using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

/// <summary>
/// The always-there game brain. Creates itself before the first scene loads (with GameUI) and survives
/// scene changes. Nothing to set up by hand.
///
///   - Hooks every DialogueRunner in each loaded scene (adds PlaybackPresenter, Yarn commands, events)
///   - Takes a checkpoint at the start of every dialogue node (what "save during dialogue" stores)
///   - Save / load / quick save / quick load / autosave
///   - Pause, play time, current chapter, hotkeys, face-mode text speed
///
/// Yarn commands it adds to every runner:
///   &lt;&lt;chapter 1&gt;&gt;   current chapter is 1 (shown in saves, decides if "skip all" is allowed) + autosave
///   &lt;&lt;autosave&gt;&gt;    autosave now
/// (&lt;&lt;chapter_end 1&gt;&gt; from YarnCommands marks chapter 1 as completed + autosave.)
/// </summary>
[DefaultExecutionOrder(-500)]
public class GameSession : MonoBehaviour
{
    /// <summary>Scene used by "Return to Title". Must be in the Build Profiles scene list.</summary>
    public const string TitleSceneName = "Title";

    private const string MenuLock = "menu";
    private const float AutosaveMinInterval = 2f;

    public static GameSession Instance { get; private set; }

    public int CurrentChapter { get; private set; }
    public float PlayTime { get; private set; }
    public bool IsOnTitle { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsLoading { get; private set; }
    public bool IsBusy => IsLoading || SceneTransitioner.IsTransitioning;
    public bool AnyDialogueRunning => runners.Any(r => r != null && r.IsDialogueRunning);

    /// <summary>Message for the small popup (e.g. "Quick saved").</summary>
    public static event Action<string> Toast;
    /// <summary>A save file was written or deleted (slot screen refreshes).</summary>
    public static event Action SlotsChanged;
    /// <summary>A load or new game is starting (UI closes its menus).</summary>
    public static event Action LoadStarted;

    private readonly List<DialogueRunner> runners = new List<DialogueRunner>();
    private readonly List<LinePresenter> linePresenters = new List<LinePresenter>();
    private readonly List<Behaviour> lineAdvancers = new List<Behaviour>();

    private SaveData checkpoint;   // state at the start of the current dialogue node
    private SaveData pendingLoad;  // save being loaded
    private float lastAutosave = -999f;
    private bool autosavePending;
    private bool saving;

    // ------------------------------------------------------------------
    // Startup
    // ------------------------------------------------------------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        Toast = null;
        SlotsChanged = null;
        LoadStarted = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (Instance != null) return;
        var go = new GameObject("[GameSession]");
        DontDestroyOnLoad(go);
        go.AddComponent<GameSession>();
        go.AddComponent<GameUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        GameInput.Init();
        GameSettings.Apply();
        DialoguePlayback.SetSpeedIndex(GameSettings.DefaultSpeedIndex);
        DialoguePlayback.CurrentChapterCompleted = () => ProgressProfile.IsChapterCompleted(CurrentChapter);

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneTransitioner.OnTransitionFinished += OnTransitionFinished;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneTransitioner.OnTransitionFinished -= OnTransitionFinished;
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f;
        }
    }

    private void OnApplicationQuit() => ProgressProfile.Flush();

    // ------------------------------------------------------------------
    // Scene hooks
    // ------------------------------------------------------------------

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            runners.Clear();
            linePresenters.Clear();
            lineAdvancers.Clear();
            checkpoint = null;
            if (IsPaused) SetPaused(false);
            DialoguePlayback.EndDialogue();
            IsOnTitle = StateCapture.Find<TitleScreen>(scene) != null;
        }

        HookScene(scene);

        if (pendingLoad != null && mode == LoadSceneMode.Single)
            StateCapture.ApplyEarly(pendingLoad, scene);
    }

    private void HookScene(Scene scene)
    {
        foreach (DialogueRunner runner in StateCapture.All<DialogueRunner>(scene))
        {
            if (runner == null || runners.Contains(runner)) continue;
            runners.Add(runner);

            // When loading a save, the save decides which dialogue runs (not the scene's auto start).
            if (pendingLoad != null) runner.autoStart = false;

            var playback = runner.gameObject.AddComponent<PlaybackPresenter>();
            playback.Init(runner);
            var presenters = runner.DialoguePresenters.ToList();
            presenters.Add(playback);
            runner.DialoguePresenters = presenters;

            DialogueRunner r = runner;
            r.onNodeStart?.AddListener(node => OnNodeStart(r, node));
            r.onDialogueComplete?.AddListener(() => OnDialogueComplete(r));

            try
            {
                r.AddCommandHandler<int>("chapter", SetChapter);       // <<chapter 1>>
                r.AddCommandHandler("autosave", AutosaveCommand);      // <<autosave>>
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameSession] Could not add Yarn commands to {r.name}: {e.Message}");
            }

            linePresenters.AddRange(presenters.OfType<LinePresenter>());
        }

        foreach (LineAdvancer advancer in StateCapture.All<LineAdvancer>(scene))
            if (!lineAdvancers.Contains(advancer)) lineAdvancers.Add(advancer);
        foreach (var keys in StateCapture.All<Yarn.Unity.LineAdvancerInput.KeyCodes>(scene))
        {
            if (lineAdvancers.Contains(keys)) continue;
            lineAdvancers.Add(keys);
            // Yarn's template uses Esc for "next line", which would fire together with the pause menu.
            if (keys.nextLineKeyCode == KeyCode.Escape) keys.nextLineKeyCode = KeyCode.None;
            if (keys.cancelDialogueKeyCode == KeyCode.Escape) keys.cancelDialogueKeyCode = KeyCode.None;
        }
    }

    private void OnNodeStart(DialogueRunner runner, string node)
    {
        if (IsOnTitle) return;
        checkpoint = StateCapture.Capture(SceneManager.GetActiveScene());
        checkpoint.dialogueRunner = runner.gameObject.name;
        checkpoint.dialogueNode = node;
        RequestAutosave();
    }

    private void OnDialogueComplete(DialogueRunner finished)
    {
        if (runners.Any(r => r != null && r != finished && r.IsDialogueRunning)) return;
        checkpoint = null;
        StartCoroutine(NextFrame(RequestAutosave));
    }

    private void OnTransitionFinished(bool wasSaveLoad)
    {
        if (!wasSaveLoad) RequestAutosave(); // entered a new area
    }

    /// <summary>True if the runner's current node is a click-to-continue ("face") node.</summary>
    public static bool IsFaceNode(DialogueRunner runner)
    {
        if (runner == null) return false;
        try
        {
            string node = runner.Dialogue.CurrentNode;
        string header = string.IsNullOrEmpty(node) ? null : runner.Dialogue.GetHeaderValue(node, "tags");
        IEnumerable<string> tags = header?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);            
        if (tags != null)
            {
                foreach (string raw in tags)
                {
                    string t = raw.Trim().TrimStart('#').ToLowerInvariant();
                    if (t == "face") return true;
                    if (t == "mhm" || t == "chat" || t == "jaycord") return false;
                }
            }
        }
        catch { /* no program loaded yet */ }

        // No mode tag: face mode if this runner shows lines with Yarn's LinePresenter.
        return runner.DialoguePresenters.OfType<LinePresenter>().Any(p => p != null && p.isActiveAndEnabled);
    }

    /// <summary>True while a face-mode node is running (the Auto button is only useful then).</summary>
    public bool FaceDialogueRunning => runners.Any(r => r != null && r.IsDialogueRunning && IsFaceNode(r));

    // ------------------------------------------------------------------
    // Update: play time, hotkeys, text speed
    // ------------------------------------------------------------------

    private float rescanTimer;

    // Safety net: also hook runners the scene-loaded event missed (the first scene in the editor,
    // or dialogue systems spawned later). Already-hooked runners are skipped.
    private void Start()
    {
        Scene active = SceneManager.GetActiveScene();
        IsOnTitle = StateCapture.Find<TitleScreen>(active) != null;
        HookScene(active);
    }

    private void Update()
    {
        rescanTimer -= Time.unscaledDeltaTime;
        if (rescanTimer <= 0f && !IsLoading)
        {
            rescanTimer = 0.5f;
            HookScene(SceneManager.GetActiveScene());
        }

        if (!IsOnTitle && !IsPaused && !IsBusy) PlayTime += Time.unscaledDeltaTime;

        if (autosavePending && Time.unscaledTime - lastAutosave >= AutosaveMinInterval) RequestAutosave();

        bool inputFree = !GameInput.IsRebinding && !IsBusy;
        bool menuOpen = GameUI.Instance != null && GameUI.Instance.AnyMenuOpen;

        DialoguePlayback.SetSkipHeld(inputFree && !IsOnTitle && !menuOpen && AnyDialogueRunning && GameInput.Skip.IsPressed());

        if (inputFree)
        {
            if (GameInput.Pause.WasPressedThisFrame())
            {
                if (GameUI.Instance != null) GameUI.Instance.HandleBack();
            }
            else if (!IsOnTitle && !menuOpen)
            {
                if (GameInput.QuickSave.WasPressedThisFrame()) QuickSave();
                else if (GameInput.QuickLoad.WasPressedThisFrame()) QuickLoad();
            }
        }

        ApplyFaceTextSpeed();
    }

    private void ApplyFaceTextSpeed()
    {
        float mult = DialoguePlayback.IsSkipping ? 1000f : GameSettings.TextSpeed * DialoguePlayback.Speed;
        foreach (LinePresenter lp in linePresenters)
        {
            if (lp == null) continue;
            switch (lp.Typewriter)
            {
                case LetterTypewriter letters: letters.CharactersPerSecond = lp.lettersPerSecond * mult; break;
                case WordTypewriter words: words.WordsPerSecond = lp.wordsPerSecond * mult; break;
            }
        }
    }

    // ------------------------------------------------------------------
    // Pause
    // ------------------------------------------------------------------

    public void SetPaused(bool paused)
    {
        if (IsPaused == paused) return;
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        DialoguePlayback.Paused = paused;
        foreach (Behaviour b in lineAdvancers) if (b != null) b.enabled = !paused;

        PlayerMover player = PlayerMover.Instance;
        if (player != null)
        {
            if (paused) player.Lock(MenuLock); else player.Unlock(MenuLock);
        }
    }

    // ------------------------------------------------------------------
    // Chapters
    // ------------------------------------------------------------------

    public void SetChapter(int chapter)
    {
        CurrentChapter = chapter;
        RequestAutosave();
    }

    public void OnChapterEnd(int chapter)
    {
        ProgressProfile.MarkChapterCompleted(chapter);
        RequestAutosave();
    }

    private void AutosaveCommand() => RequestAutosave();

    // ------------------------------------------------------------------
    // Saving
    // ------------------------------------------------------------------

    /// <summary>Save into a slot. thumbnail = null takes a screenshot now.</summary>
    public void SaveTo(string slotId, byte[] thumbnail = null, bool silent = false)
    {
        if (IsOnTitle || IsBusy) return;
        StartCoroutine(SaveRoutine(slotId, thumbnail, silent));
    }

    public void QuickSave() => SaveTo(SaveManager.QuickSlot);

    public void QuickLoad()
    {
        if (!SaveManager.Exists(SaveManager.QuickSlot)) { ShowToast("No quick save yet"); return; }
        if (GameSettings.ConfirmQuickLoad && GameUI.Instance != null)
            GameUI.Instance.Confirm("Quick load? Unsaved progress will be lost.", () => LoadFrom(SaveManager.QuickSlot));
        else
            LoadFrom(SaveManager.QuickSlot);
    }

    /// <summary>Autosave into the auto slot (at most once every 2 s; a skipped request runs a bit later).</summary>
    public void RequestAutosave()
    {
        if (IsOnTitle || IsLoading) { autosavePending = false; return; }
        if (saving || SceneTransitioner.IsTransitioning || Time.unscaledTime - lastAutosave < AutosaveMinInterval)
        {
            autosavePending = true;
            return;
        }
        autosavePending = false;
        lastAutosave = Time.unscaledTime;
        StartCoroutine(SaveRoutine(SaveManager.AutoSlot, null, true));
    }

    private IEnumerator SaveRoutine(string slotId, byte[] thumbnail, bool silent)
    {
        saving = true;

        SaveData data = AnyDialogueRunning && checkpoint != null
            ? JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(checkpoint))
            : StateCapture.Capture(SceneManager.GetActiveScene());

        DateTime now = DateTime.Now;
        data.chapter = CurrentChapter;
        data.playTime = PlayTime;
        data.savedAt = now.ToString("yyyy-MM-dd HH:mm");
        data.savedAtTicks = now.Ticks;

        byte[] jpg = thumbnail;
        if (jpg == null) yield return SaveManager.CaptureThumbnail(bytes => jpg = bytes);

        bool ok = SaveManager.Write(slotId, data, jpg);
        ProgressProfile.Flush();
        saving = false;

        if (!silent) ShowToast(ok ? "Saved: " + SaveManager.DisplayName(slotId) : "Save failed (see Console)");
        SlotsChanged?.Invoke();
    }

    public void DeleteSave(string slotId)
    {
        SaveManager.Delete(slotId);
        SlotsChanged?.Invoke();
    }

    // ------------------------------------------------------------------
    // Loading / new game / title
    // ------------------------------------------------------------------

    public bool LoadFrom(string slotId)
    {
        if (IsBusy) return false;
        SaveData data = SaveManager.Read(slotId);
        if (data == null) { ShowToast("This save can't be loaded"); return false; }
        if (!Application.CanStreamedLevelBeLoaded(data.sceneName))
        {
            ShowToast($"Scene \"{data.sceneName}\" is not in the Build Profiles scene list");
            return false;
        }

        SetPaused(false);
        LoadStarted?.Invoke();
        DialoguePlayback.EndDialogue();

        pendingLoad = data;
        IsLoading = true;
        PlayTime = data.playTime;
        CurrentChapter = data.chapter;

        if (!SceneTransitioner.Instance.LoadForSave(data.sceneName, AfterSaveSceneStarted))
        {
            pendingLoad = null;
            IsLoading = false;
            return false;
        }
        return true;
    }

    // Runs one frame after the save's scene loaded (all Start() done), before the fade-in.
    private IEnumerator AfterSaveSceneStarted(Scene scene)
    {
        SaveData d = pendingLoad;
        if (d != null)
        {
            StateCapture.ApplyLate(d, scene);

            if (!string.IsNullOrEmpty(d.dialogueNode))
            {
                DialogueRunner runner =
                    runners.FirstOrDefault(r => r != null && r.gameObject.name == d.dialogueRunner) ??
                    runners.FirstOrDefault(r => r != null && SafeNodeExists(r, d.dialogueNode));
                if (runner != null) runner.StartDialogue(d.dialogueNode);
                else Debug.LogWarning($"[GameSession] No DialogueRunner can play node \"{d.dialogueNode}\" in {scene.name}.");
            }
        }
        pendingLoad = null;
        IsLoading = false;
        yield break;
    }

    private static bool SafeNodeExists(DialogueRunner r, string node)
    {
        try { return r.Dialogue.NodeExists(node); } catch { return false; }
    }

    public void NewGame(string firstScene)
    {
        if (IsBusy) return;
        CurrentChapter = 0;
        PlayTime = 0f;
        checkpoint = null;
        pendingLoad = null;
        LoadStarted?.Invoke();
        if (!SceneTransitioner.Instance.GoTo(firstScene, ""))
            ShowToast($"Scene \"{firstScene}\" is not in the Build Profiles scene list");
    }

    public void ReturnToTitle()
    {
        if (IsBusy) return;
        SetPaused(false);
        LoadStarted?.Invoke();
        if (!SceneTransitioner.Instance.GoTo(TitleSceneName, ""))
            ShowToast($"Scene \"{TitleSceneName}\" is not in the Build Profiles scene list");
    }

    public static void QuitGame()
    {
        ProgressProfile.Flush();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------

    public static void ShowToast(string message) => Toast?.Invoke(message);

    private static IEnumerator NextFrame(Action action)
    {
        yield return null;
        action();
    }
}

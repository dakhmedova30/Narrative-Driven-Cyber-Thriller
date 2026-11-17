using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Fades to black, loads another area (Unity scene), puts the player on a SpawnPoint, fades back in.
///
/// No setup needed: it creates itself the first time it is used and survives scene loads.
/// Use it from code:  SceneTransitioner.Instance.GoTo("Street", "from_home");
/// AreaExit and the Yarn command <<goto Scene Spawn>> both call this.
///
/// Every scene you go to must be in File > Build Profiles > Scene List.
/// </summary>
public class SceneTransitioner : MonoBehaviour
{
    private const string LockReason = "scene";

    private static SceneTransitioner instance;

    public static SceneTransitioner Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("[SceneTransitioner]");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<SceneTransitioner>();
            }
            return instance;
        }
    }

    /// <summary>True from the moment the fade-out starts until the fade-in ends.</summary>
    public static bool IsTransitioning => instance != null && instance.busy;

    /// <summary>Fired after the new area is loaded and the player is placed (before the fade-in). Arg = spawn id.</summary>
    public static event System.Action<string> OnAreaEntered;

    /// <summary>Fired after the fade-in finished. Arg = true if this was loading a save (not walking to an area).</summary>
    public static event System.Action<bool> OnTransitionFinished;

    /// <summary>Fired after the fade-out, right before the old scene unloads. Arg = true if loading a save.</summary>
    public static event System.Action<bool> OnBeforeLoad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        OnAreaEntered = null;
        OnTransitionFinished = null;
        OnBeforeLoad = null;
    }

    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.4f;
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.4f;
    [SerializeField] private Color fadeColor = Color.black;

    private Image overlay;
    private bool busy;
    private string pendingSpawn;
    private bool placeAtSpawn = true;
    private PlayerMover lockedPlayer;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        BuildOverlay();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    /// <summary>Go to another scene and stand on the SpawnPoint with this id. Returns false if ignored.</summary>
    public bool GoTo(string sceneName, string spawnId = "")
    {
        if (busy) return false;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneTransitioner] No scene name given.");
            return false;
        }
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneTransitioner] Scene \"{sceneName}\" is not in File > Build Profiles > Scene List (or the name is misspelled).");
            return false;
        }
        StartCoroutine(Run(sceneName, spawnId, true, null));
        return true;
    }

    /// <summary>
    /// Used by GameSession to load a save: same fade, but the player is NOT moved to a spawn point
    /// (the save places them). afterStart runs one frame after loading, before the fade-in.
    /// </summary>
    public bool LoadForSave(string sceneName, System.Func<Scene, IEnumerator> afterStart)
    {
        if (busy) return false;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneTransitioner] Scene \"{sceneName}\" is not in File > Build Profiles > Scene List.");
            return false;
        }
        StartCoroutine(Run(sceneName, null, false, afterStart));
        return true;
    }

    private IEnumerator Run(string sceneName, string spawnId, bool useSpawn, System.Func<Scene, IEnumerator> afterStart)
    {
        busy = true;
        DialoguePlayback.StopSkip();
        overlay.raycastTarget = true; // block clicks during the transition

        lockedPlayer = PlayerMover.Instance;
        if (lockedPlayer != null) lockedPlayer.Lock(LockReason);

        yield return Fade(0f, 1f, fadeOutSeconds);

        OnBeforeLoad?.Invoke(!useSpawn);
        pendingSpawn = spawnId;
        placeAtSpawn = useSpawn;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;
        yield return null; // let Start() run in the new scene

        if (afterStart != null) yield return afterStart(SceneManager.GetActiveScene());

        yield return Fade(1f, 0f, fadeInSeconds);

        if (lockedPlayer != null) lockedPlayer.Unlock(LockReason);
        lockedPlayer = null;
        overlay.raycastTarget = false;
        busy = false;
        OnTransitionFinished?.Invoke(!useSpawn);
    }

    // Runs after the new scene's Awake/OnEnable but BEFORE its first Update/LateUpdate,
    // so the camera never sees the player at the wrong spot (no camera slide).
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        PlayerMover player = null;
        SpawnPoint chosen = null, fallbackDefault = null, fallbackFirst = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (player == null) player = root.GetComponentInChildren<PlayerMover>(true);
            foreach (SpawnPoint sp in root.GetComponentsInChildren<SpawnPoint>(true))
            {
                if (fallbackFirst == null) fallbackFirst = sp;
                if (fallbackDefault == null && sp.IsDefault) fallbackDefault = sp;
                if (chosen == null && !string.IsNullOrEmpty(pendingSpawn) && sp.Id == pendingSpawn) chosen = sp;
            }
        }

        if (chosen == null && !string.IsNullOrEmpty(pendingSpawn) && pendingSpawn != "default")
            Debug.LogWarning($"[SceneTransitioner] No SpawnPoint \"{pendingSpawn}\" in scene {scene.name}. Using the default one.");
        if (chosen == null) chosen = fallbackDefault != null ? fallbackDefault : fallbackFirst;

        if (player != null)
        {
            player.Lock(LockReason);   // keep still while fading in
            lockedPlayer = player;
            if (chosen != null && placeAtSpawn)
            {
                player.Teleport(chosen.transform.position.x);
                player.SetFacing(chosen.FaceRight);
            }
        }

        OnAreaEntered?.Invoke(chosen != null ? chosen.Id : pendingSpawn);
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        if (seconds <= 0f) { SetAlpha(to); yield break; }
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            SetAlpha(Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        Color c = fadeColor;
        c.a = a;
        overlay.color = c;
        overlay.enabled = a > 0.001f;
    }

    private void BuildOverlay()
    {
        var canvasGo = new GameObject("FadeCanvas", typeof(Canvas), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000; // above everything, including dialogue UI

        var imgGo = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        imgGo.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)imgGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        overlay = imgGo.GetComponent<Image>();
        overlay.raycastTarget = false;
        SetAlpha(0f);
    }
}

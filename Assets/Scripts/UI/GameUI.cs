using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Template UI that lives for the whole game (created by GameSession, nothing to set up):
///   - Dialogue control bar (bottom right, only while a dialogue runs):
///       Auto | 1x/2x/3x | Skip | Q.Save | Q.Load | Save | Load | Menu
///   - Pause menu (Esc): Resume, Save, Load, Settings, Return to Title, Quit
///   - Save / Load slot screen, Settings screen, confirm box, toast messages
/// Also makes sure every scene has an EventSystem so the buttons can be clicked.
/// </summary>
public class GameUI : MonoBehaviour
{
    private enum ReturnTo { Game, Pause, Title }

    public static GameUI Instance { get; private set; }

    private Canvas canvas;
    private EventSystem ownEventSystem;

    private RectTransform bar;
    private Button autoButton, speedButton, skipButton;

    private RectTransform toastBox;
    private TextMeshProUGUI toastText;
    private float toastUntil;

    private RectTransform pausePanel;
    private RectTransform confirmPanel;
    private TextMeshProUGUI confirmText;
    private Action confirmYes;

    private SlotPanel slots;
    private SettingsPanel settings;
    private ReturnTo slotsReturn, settingsReturn;

    private byte[] menuShot; // screenshot taken just before a menu opened (for save thumbnails)
    private bool opening;

    public bool AnyMenuOpen =>
        opening || pausePanel.gameObject.activeSelf || confirmPanel.gameObject.activeSelf || slots.IsOpen || settings.IsOpen;

    private static GameSession Session => GameSession.Instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------

    private void Awake()
    {
        Instance = this;

        var esGo = new GameObject("EventSystem (GameUI)", typeof(EventSystem), typeof(InputSystemUIInputModule));
        esGo.transform.SetParent(transform, false);
        ownEventSystem = esGo.GetComponent<EventSystem>();
        esGo.SetActive(false); // only switched on when the scene has no EventSystem of its own (see Update)

        canvas = UIKit.CreateCanvas("GameUI Canvas", transform, 30000);
        BuildBar();
        BuildPause();
        slots = new SlotPanel(canvas.transform, this);
        settings = new SettingsPanel(canvas.transform);
        BuildConfirm();
        BuildToast();

        GameSession.Toast += ShowToast;
        GameSession.LoadStarted += CloseAll;
        DialoguePlayback.Changed += RefreshBar;
        DialoguePlayback.SkipStopped += ShowToast;
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshBar();
    }

    private void OnDestroy()
    {
        GameSession.Toast -= ShowToast;
        GameSession.LoadStarted -= CloseAll;
        DialoguePlayback.Changed -= RefreshBar;
        DialoguePlayback.SkipStopped -= ShowToast;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        // Switch ours off; Update turns it back on only if the new scene has no EventSystem.
        ownEventSystem.gameObject.SetActive(false);
        CloseAll();
    }

    private void BuildBar()
    {
        bar = UIKit.Rect("Dialogue Control Bar", canvas.transform);
        UIKit.Place(bar, new Vector2(1f, 0f), new Vector2(0f, 52f), new Vector2(-16f, 16f));
        UIKit.Horizontal(bar.gameObject, 6f, 0, TextAnchor.MiddleRight);
        var fitter = bar.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        autoButton = UIKit.Button(bar, "Auto", () => DialoguePlayback.SetAuto(!DialoguePlayback.Auto), 90f, 44f, 20f);
        speedButton = UIKit.Button(bar, "1x", DialoguePlayback.CycleSpeed, 70f, 44f, 20f);
        skipButton = UIKit.Button(bar, "Skip", DialoguePlayback.ToggleSkip, 120f, 44f, 20f);
        UIKit.Button(bar, "Q.Save", () => Session?.QuickSave(), 90f, 44f, 20f);
        UIKit.Button(bar, "Q.Load", () => Session?.QuickLoad(), 90f, 44f, 20f);
        UIKit.Button(bar, "Save", () => OpenSlots(SlotPanel.Mode.Save, ReturnTo.Game), 80f, 44f, 20f);
        UIKit.Button(bar, "Load", () => OpenSlots(SlotPanel.Mode.Load, ReturnTo.Game), 80f, 44f, 20f);
        UIKit.Button(bar, "Menu", OpenPause, 80f, 44f, 20f);
    }

    private void BuildPause()
    {
        pausePanel = UIKit.Overlay(canvas.transform, "Pause Menu");
        Image box = UIKit.Image(pausePanel, "Box", UIKit.PanelColor);
        UIKit.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(460f, 620f));
        UIKit.Vertical(box.gameObject, 14f, 36);

        UIKit.Label(box.transform, "PAUSED", 44f, UIKit.AccentColor, TextAlignmentOptions.Center, 70f);
        UIKit.Button(box.transform, "Resume", Resume);
        UIKit.Button(box.transform, "Save", () => OpenSlots(SlotPanel.Mode.Save, ReturnTo.Pause));
        UIKit.Button(box.transform, "Load", () => OpenSlots(SlotPanel.Mode.Load, ReturnTo.Pause));
        UIKit.Button(box.transform, "Settings", () => OpenSettings(ReturnTo.Pause));
        UIKit.Button(box.transform, "Return to Title",
            () => Confirm("Return to the title screen? Unsaved progress will be lost.", () => Session?.ReturnToTitle()));
        UIKit.Button(box.transform, "Quit Game",
            () => Confirm("Quit the game? Unsaved progress will be lost.", GameSession.QuitGame));

        pausePanel.gameObject.SetActive(false);
    }

    private void BuildConfirm()
    {
        confirmPanel = UIKit.Overlay(canvas.transform, "Confirm");
        Image box = UIKit.Image(confirmPanel, "Box", UIKit.PanelColor);
        UIKit.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(640f, 280f));
        UIKit.Vertical(box.gameObject, 24f, 36);

        confirmText = UIKit.Label(box.transform, "", 28f, UIKit.TextColor, TextAlignmentOptions.Center, 110f);

        RectTransform row = UIKit.Rect("Buttons", box.transform);
        UIKit.Horizontal(row.gameObject, 20f, 0, TextAnchor.MiddleCenter);
        UIKit.Size(row.gameObject, -1f, 56f);
        UIKit.Button(row, "Yes", () =>
        {
            Action yes = confirmYes;
            CloseConfirm();
            yes?.Invoke();
        }, 200f);
        UIKit.Button(row, "No", CloseConfirm, 200f);

        confirmPanel.gameObject.SetActive(false);
    }

    private void BuildToast()
    {
        Image box = UIKit.Image(canvas.transform, "Toast", new Color(0f, 0f, 0f, 0.8f), false);
        toastBox = box.rectTransform;
        UIKit.Place(toastBox, new Vector2(0.5f, 1f), new Vector2(620f, 60f), new Vector2(0f, -24f));
        toastText = UIKit.Label(toastBox, "", 24f, UIKit.TextColor, TextAlignmentOptions.Center);
        UIKit.Stretch(toastText.rectTransform, 8f);
        toastBox.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------
    // Per frame
    // ------------------------------------------------------------------

    private void Update()
    {
        // Exactly one EventSystem: use the scene's if it has one, otherwise ours.
        EventSystem current = EventSystem.current;
        if (current == null)
        {
            if (!ownEventSystem.gameObject.activeSelf) ownEventSystem.gameObject.SetActive(true);
        }
        else if (current != ownEventSystem && ownEventSystem.gameObject.activeSelf)
        {
            ownEventSystem.gameObject.SetActive(false);
        }

        bool showBar = Session != null && !Session.IsOnTitle && !Session.IsBusy && !AnyMenuOpen && Session.AnyDialogueRunning;
        if (bar.gameObject.activeSelf != showBar) bar.gameObject.SetActive(showBar);
        if (showBar)
        {
            bool face = Session.FaceDialogueRunning;
            if (autoButton.gameObject.activeSelf != face) autoButton.gameObject.SetActive(face);
        }

        if (toastBox.gameObject.activeSelf && Time.unscaledTime > toastUntil) toastBox.gameObject.SetActive(false);
    }

    private void RefreshBar()
    {
        if (autoButton == null) return;
        UIKit.SetOn(autoButton, DialoguePlayback.Auto);
        UIKit.SetText(speedButton, DialoguePlayback.Speed.ToString("0") + "x");
        UIKit.SetOn(speedButton, DialoguePlayback.SpeedIndex > 0);
        bool skipping = DialoguePlayback.SkipToggled || DialoguePlayback.IsSkipping;
        UIKit.SetOn(skipButton, skipping);
        UIKit.SetText(skipButton, DialoguePlayback.EffectiveSkipMode == DialoguePlayback.SkipMode.All ? "Skip: All" : "Skip: Read");
    }

    public void ShowToast(string message)
    {
        toastText.text = message;
        toastBox.gameObject.SetActive(true);
        toastUntil = Time.unscaledTime + 2f;
    }

    // ------------------------------------------------------------------
    // Menus
    // ------------------------------------------------------------------

    /// <summary>Esc: close the top-most menu, or open the pause menu during gameplay.</summary>
    public void HandleBack()
    {
        if (GameInput.IsRebinding || opening) return;
        if (confirmPanel.gameObject.activeSelf) { CloseConfirm(); return; }
        if (settings.IsOpen) { settings.Close(); Return(settingsReturn); return; }
        if (slots.IsOpen) { slots.Close(); Return(slotsReturn); return; }
        if (pausePanel.gameObject.activeSelf) { Resume(); return; }
        if (Session != null && !Session.IsOnTitle && !Session.IsBusy) OpenPause();
    }

    public void OpenPause()
    {
        if (Session == null || Session.IsOnTitle || Session.IsBusy || AnyMenuOpen) return;
        StartCoroutine(OpenAfterScreenshot(() => pausePanel.gameObject.SetActive(true)));
    }

    public void Resume()
    {
        pausePanel.gameObject.SetActive(false);
        Session?.SetPaused(false);
    }

    /// <summary>Open the save/load screen. From the title screen, pass fromTitle = true.</summary>
    public void OpenSlotsFromTitle(SlotPanel.Mode mode) => OpenSlots(mode, ReturnTo.Title);
    public void OpenSettingsFromTitle() => OpenSettings(ReturnTo.Title);

    private void OpenSlots(SlotPanel.Mode mode, ReturnTo returnTo)
    {
        slotsReturn = returnTo;
        if (returnTo == ReturnTo.Game)
        {
            // From the control bar: pause and take a clean screenshot first.
            if (AnyMenuOpen) return;
            StartCoroutine(OpenAfterScreenshot(() => slots.Open(mode, menuShot)));
            return;
        }
        pausePanel.gameObject.SetActive(false);
        slots.Open(mode, returnTo == ReturnTo.Title ? null : menuShot);
    }

    private void OpenSettings(ReturnTo returnTo)
    {
        settingsReturn = returnTo;
        pausePanel.gameObject.SetActive(false);
        settings.Open();
    }

    /// <summary>Called by SlotPanel's Back button.</summary>
    public void CloseSlots()
    {
        slots.Close();
        Return(slotsReturn);
    }

    /// <summary>Called by SettingsPanel's Back button.</summary>
    public void CloseSettings()
    {
        settings.Close();
        Return(settingsReturn);
    }

    private void Return(ReturnTo to)
    {
        switch (to)
        {
            case ReturnTo.Pause: pausePanel.gameObject.SetActive(true); break;
            case ReturnTo.Game: Session?.SetPaused(false); break;
            case ReturnTo.Title: break;
        }
    }

    public void Confirm(string message, Action onYes)
    {
        confirmText.text = message;
        confirmYes = onYes;
        confirmPanel.gameObject.SetActive(true);
        confirmPanel.SetAsLastSibling();
        toastBox.SetAsLastSibling();
    }

    private void CloseConfirm()
    {
        confirmPanel.gameObject.SetActive(false);
        confirmYes = null;
    }

    public void CloseAll()
    {
        StopAllCoroutines();
        opening = false;
        pausePanel.gameObject.SetActive(false);
        CloseConfirm();
        slots.Close();
        settings.Close();
        if (Session != null && Session.IsPaused) Session.SetPaused(false);
    }

    // Screenshot WITHOUT the menu (for the save thumbnail), then pause and show the menu.
    private IEnumerator OpenAfterScreenshot(Action show)
    {
        opening = true;
        bar.gameObject.SetActive(false);
        yield return null; // let the bar disappear from the frame
        byte[] shot = null;
        yield return SaveManager.CaptureThumbnail(b => shot = b);
        menuShot = shot;
        Session?.SetPaused(true);
        opening = false;
        show();
    }

    // Lets panels rebuild their own content in the shared canvas.
    public Transform CanvasRoot => canvas.transform;
}

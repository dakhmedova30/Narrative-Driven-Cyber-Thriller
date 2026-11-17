using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// All rebindable keys of the game, built in code (no .inputactions asset needed).
/// Defaults:
///   Move Left   A / Left Arrow        Quick Save  F5
///   Move Right  D / Right Arrow       Quick Load  F9
///   Pause       Esc                   Skip (hold) Left Ctrl / Right Ctrl
/// Player changes are saved in PlayerPrefs and loaded on start.
/// Mouse clicks (click-to-move, UI) are not rebindable.
/// </summary>
public static class GameInput
{
    private const string PrefsKey = "settings.bindings";

    private static InputActionAsset asset;
    private static InputActionRebindingExtensions.RebindingOperation rebinding;

    public static InputAction MoveLeft { get; private set; }
    public static InputAction MoveRight { get; private set; }
    public static InputAction QuickSave { get; private set; }
    public static InputAction QuickLoad { get; private set; }
    public static InputAction Pause { get; private set; }
    public static InputAction Skip { get; private set; }

    /// <summary>Fired after a key was rebound or bindings were reset.</summary>
    public static event Action BindingsChanged;

    public static bool IsRebinding => rebinding != null;

    /// <summary>Actions shown in Settings &gt; Controls, in display order.</summary>
    public static InputAction[] Rebindable
    {
        get
        {
            Init();
            return new[] { MoveLeft, MoveRight, QuickSave, QuickLoad, Pause, Skip };
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        asset = null;
        rebinding = null;
        BindingsChanged = null;
    }

    public static void Init()
    {
        if (asset != null) return;

        asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.name = "GameInput";
        InputActionMap map = asset.AddActionMap("Game");

        MoveLeft = Add(map, "Move Left", "<Keyboard>/a", "<Keyboard>/leftArrow");
        MoveRight = Add(map, "Move Right", "<Keyboard>/d", "<Keyboard>/rightArrow");
        QuickSave = Add(map, "Quick Save", "<Keyboard>/f5", null);
        QuickLoad = Add(map, "Quick Load", "<Keyboard>/f9", null);
        Pause = Add(map, "Pause / Back", "<Keyboard>/escape", null);
        Skip = Add(map, "Skip (hold)", "<Keyboard>/leftCtrl", "<Keyboard>/rightCtrl");

        string json = PlayerPrefs.GetString(PrefsKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            try { asset.LoadBindingOverridesFromJson(json); }
            catch (Exception e) { Debug.LogWarning("[GameInput] Could not load saved key bindings: " + e.Message); }
        }
        asset.Enable();
    }

    // Every action gets exactly two binding slots (primary, secondary) so the settings UI can show two buttons.
    private static InputAction Add(InputActionMap map, string name, string primary, string secondary)
    {
        InputAction action = map.AddAction(name, InputActionType.Button);
        action.AddBinding(primary ?? "");
        action.AddBinding(secondary ?? "");
        return action;
    }

    /// <summary>Human-readable key name, e.g. "F5". Empty slot = "-".</summary>
    public static string Display(InputAction action, int bindingIndex)
    {
        if (action == null || bindingIndex >= action.bindings.Count) return "-";
        string s = action.GetBindingDisplayString(bindingIndex);
        return string.IsNullOrEmpty(s) ? "-" : s;
    }

    /// <summary>Wait for the next key press and bind it. Right-click cancels.</summary>
    public static void StartRebind(InputAction action, int bindingIndex, Action onFinished)
    {
        Init();
        if (rebinding != null) return;

        action.Disable();
        rebinding = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>")
            .WithControlsExcluding("<Pointer>")
            .WithCancelingThrough("<Mouse>/rightButton")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op => Finish(action, onFinished, true))
            .OnCancel(op => Finish(action, onFinished, false))
            .Start();
    }

    public static void CancelRebind()
    {
        rebinding?.Cancel();
    }

    /// <summary>Remove the key from one slot (e.g. to leave only one key for an action).</summary>
    public static void ClearBinding(InputAction action, int bindingIndex)
    {
        action.ApplyBindingOverride(bindingIndex, "");
        SaveBindings();
        BindingsChanged?.Invoke();
    }

    public static void ResetToDefaults()
    {
        Init();
        asset.RemoveAllBindingOverrides();
        SaveBindings();
        BindingsChanged?.Invoke();
    }

    private static void Finish(InputAction action, Action onFinished, bool changed)
    {
        rebinding?.Dispose();
        rebinding = null;
        action.Enable();
        if (changed)
        {
            SaveBindings();
            BindingsChanged?.Invoke();
        }
        onFinished?.Invoke();
    }

    private static void SaveBindings()
    {
        PlayerPrefs.SetString(PrefsKey, asset.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
    }
}

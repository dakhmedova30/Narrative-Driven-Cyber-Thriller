using System;
using System.Threading;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// One place for dialogue speed / skip / auto / pause. Every presenter should wait with
/// <see cref="Wait"/> instead of YarnTask.Delay, so all of these work everywhere:
///   - Speed 1x / 2x / 3x   -> waits get shorter
///   - Skip                 -> waits end immediately (stops at choices, links, scene changes, unread text)
///   - Pause menu           -> waits stop counting
/// Auto play only matters for "face" nodes (click-to-continue); PlaybackPresenter handles it.
/// </summary>
public static class DialoguePlayback
{
    public enum SkipMode { ReadOnly, All }

    public static readonly float[] Speeds = { 1f, 2f, 3f };

    private static int speedIndex;
    private static bool skipToggled;
    private static bool skipHeld;
    private static bool lineBlocksSkip;

    public static float Speed => Speeds[Mathf.Clamp(speedIndex, 0, Speeds.Length - 1)];
    public static int SpeedIndex => speedIndex;
    public static bool Auto { get; private set; }
    public static bool SkipToggled => skipToggled;
    public static bool Paused { get; set; }

    /// <summary>Skip is on (button or held key), not paused, and the current line may be skipped.</summary>
    public static bool IsSkipping => (skipToggled || skipHeld) && !Paused && !lineBlocksSkip;

    /// <summary>Set by GameSession: true when the current chapter has been finished at least once.</summary>
    public static Func<bool> CurrentChapterCompleted;

    /// <summary>The player's choice in Settings. "All" only works in chapters already completed.</summary>
    public static SkipMode PreferredSkipMode => GameSettings.SkipModeAll ? SkipMode.All : SkipMode.ReadOnly;

    public static SkipMode EffectiveSkipMode =>
        PreferredSkipMode == SkipMode.All && (CurrentChapterCompleted?.Invoke() ?? false) ? SkipMode.All : SkipMode.ReadOnly;

    /// <summary>Any button state changed (speed, auto, skip). UI listens to refresh labels.</summary>
    public static event Action Changed;
    /// <summary>Skip stopped on its own. Argument = reason for a toast.</summary>
    public static event Action<string> SkipStopped;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        speedIndex = 0; skipToggled = skipHeld = lineBlocksSkip = false;
        Auto = false; Paused = false;
        Changed = null; SkipStopped = null; CurrentChapterCompleted = null;
    }

    // ------------------------------------------------------------------
    // Buttons
    // ------------------------------------------------------------------

    public static void CycleSpeed() => SetSpeedIndex((speedIndex + 1) % Speeds.Length);

    public static void SetSpeedIndex(int index)
    {
        speedIndex = Mathf.Clamp(index, 0, Speeds.Length - 1);
        Changed?.Invoke();
    }

    public static void SetAuto(bool on)
    {
        if (Auto == on) return;
        Auto = on;
        Changed?.Invoke();
    }

    public static void ToggleSkip()
    {
        skipToggled = !skipToggled;
        Changed?.Invoke();
    }

    /// <summary>Called every frame by GameSession with the state of the Skip key.</summary>
    public static void SetSkipHeld(bool held)
    {
        if (skipHeld == held) return;
        skipHeld = held;
        Changed?.Invoke();
    }

    /// <summary>Turn the Skip button off (choice, link, scene change, dialogue end...).</summary>
    public static void StopSkip(string reason = null)
    {
        if (!skipToggled) return;
        skipToggled = false;
        Changed?.Invoke();
        if (!string.IsNullOrEmpty(reason)) SkipStopped?.Invoke(reason);
    }

    // ------------------------------------------------------------------
    // Called by PlaybackPresenter
    // ------------------------------------------------------------------

    /// <summary>A new line starts. In "read only" mode, unread lines can't be skipped.</summary>
    public static void BeginLine(bool unread)
    {
        lineBlocksSkip = unread && EffectiveSkipMode == SkipMode.ReadOnly;
        if (lineBlocksSkip) StopSkip("Skip stopped: new text");
    }

    public static void EndDialogue()
    {
        lineBlocksSkip = false;
        StopSkip();
    }

    // ------------------------------------------------------------------
    // Waiting
    // ------------------------------------------------------------------

    /// <summary>
    /// Wait for "seconds at 1x speed". Shorter at 2x/3x, ends at once while skipping,
    /// doesn't count while paused, ends early if the token is cancelled.
    /// </summary>
    public static async YarnTask Wait(float seconds, CancellationToken token = default)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (token.IsCancellationRequested || IsSkipping) return;
            if (!Paused) t += Time.unscaledDeltaTime * Speed;
            await YarnTask.Yield();
        }
    }

    /// <summary>Waits while the pause menu is open.</summary>
    public static async YarnTask WaitWhilePaused()
    {
        while (Paused) await YarnTask.Yield();
    }
}

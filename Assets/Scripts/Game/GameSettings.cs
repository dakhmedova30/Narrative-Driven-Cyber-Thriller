using UnityEngine;

/// <summary>
/// Player settings (Settings menu). Stored in PlayerPrefs, NOT in save slots.
/// Call <see cref="Apply"/> after changing a value.
/// </summary>
public static class GameSettings
{
    private const string P = "settings.";

    public static float MasterVolume { get => PlayerPrefs.GetFloat(P + "master", 1f); set => PlayerPrefs.SetFloat(P + "master", Mathf.Clamp01(value)); }
    public static float MusicVolume { get => PlayerPrefs.GetFloat(P + "music", 1f); set => PlayerPrefs.SetFloat(P + "music", Mathf.Clamp01(value)); }
    public static float SfxVolume { get => PlayerPrefs.GetFloat(P + "sfx", 1f); set => PlayerPrefs.SetFloat(P + "sfx", Mathf.Clamp01(value)); }

    /// <summary>Typewriter speed multiplier for face-mode text (1 = as set on the LinePresenter).</summary>
    public static float TextSpeed { get => PlayerPrefs.GetFloat(P + "textSpeed", 1f); set => PlayerPrefs.SetFloat(P + "textSpeed", Mathf.Clamp(value, 0.25f, 4f)); }

    /// <summary>Auto play: seconds to wait after a face-mode line is fully shown (at 1x).</summary>
    public static float AutoDelay { get => PlayerPrefs.GetFloat(P + "autoDelay", 1.5f); set => PlayerPrefs.SetFloat(P + "autoDelay", Mathf.Clamp(value, 0.2f, 6f)); }

    /// <summary>Speed the game starts with: 0 = 1x, 1 = 2x, 2 = 3x.</summary>
    public static int DefaultSpeedIndex { get => PlayerPrefs.GetInt(P + "defaultSpeed", 0); set => PlayerPrefs.SetInt(P + "defaultSpeed", value); }

    /// <summary>Skip everything (only works in chapters already completed) instead of read text only.</summary>
    public static bool SkipModeAll { get => PlayerPrefs.GetInt(P + "skipAll", 0) == 1; set => PlayerPrefs.SetInt(P + "skipAll", value ? 1 : 0); }

    /// <summary>Ask before Quick Load replaces the current progress.</summary>
    public static bool ConfirmQuickLoad { get => PlayerPrefs.GetInt(P + "confirmQuickLoad", 0) == 1; set => PlayerPrefs.SetInt(P + "confirmQuickLoad", value ? 1 : 0); }

    /// <summary>Push volumes to the audio system and save to disk.</summary>
    public static void Apply()
    {
        AudioListener.volume = MasterVolume;
        AudioManager.SetUserVolumes(MusicVolume, SfxVolume);
        PlayerPrefs.Save();
    }

    public static void ResetToDefaults()
    {
        foreach (string key in new[] { "master", "music", "sfx", "textSpeed", "autoDelay", "defaultSpeed", "skipAll", "confirmQuickLoad" })
            PlayerPrefs.DeleteKey(P + key);
        Apply();
    }
}

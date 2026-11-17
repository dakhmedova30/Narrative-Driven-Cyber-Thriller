using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Save files on disk: persistentDataPath/saves/&lt;slot&gt;.json + &lt;slot&gt;.jpg (thumbnail).
/// Slots: slot_1 … slot_6 (manual), quick, auto.
/// This class only reads/writes files. Saving/loading the GAME goes through GameSession.SaveTo / LoadFrom.
/// </summary>
public static class SaveManager
{
    public const int ManualSlotCount = 6;
    public const string QuickSlot = "quick";
    public const string AutoSlot = "auto";

    public static string ManualSlot(int number) => "slot_" + number; // 1-based

    public static IEnumerable<string> AllSlots()
    {
        for (int i = 1; i <= ManualSlotCount; i++) yield return ManualSlot(i);
        yield return QuickSlot;
        yield return AutoSlot;
    }

    public static string DisplayName(string slotId) => slotId switch
    {
        QuickSlot => "Quick Save",
        AutoSlot => "Auto Save",
        _ when slotId.StartsWith("slot_") => "Slot " + slotId.Substring(5),
        _ => slotId,
    };

    private static string Dir => Path.Combine(Application.persistentDataPath, "saves");
    private static string JsonPath(string slotId) => Path.Combine(Dir, slotId + ".json");
    private static string ThumbPath(string slotId) => Path.Combine(Dir, slotId + ".jpg");

    public static bool Exists(string slotId) => File.Exists(JsonPath(slotId));

    public static bool AnyExists()
    {
        foreach (string s in AllSlots()) if (Exists(s)) return true;
        return false;
    }

    public static SaveData Read(string slotId)
    {
        try
        {
            return Exists(slotId) ? JsonUtility.FromJson<SaveData>(File.ReadAllText(JsonPath(slotId))) : null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Save '{slotId}' is damaged and can't be read: {e.Message}");
            return null;
        }
    }

    public static bool Write(string slotId, SaveData data, byte[] thumbnailJpg)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            data.slotId = slotId;
            // Write to a temp file first so a crash mid-write never destroys the old save.
            string tmp = JsonPath(slotId) + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
            if (File.Exists(JsonPath(slotId))) File.Delete(JsonPath(slotId));
            File.Move(tmp, JsonPath(slotId));

            if (thumbnailJpg != null) File.WriteAllBytes(ThumbPath(slotId), thumbnailJpg);
            else if (File.Exists(ThumbPath(slotId))) File.Delete(ThumbPath(slotId));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Could not write save '{slotId}': {e.Message}");
            return false;
        }
    }

    public static void Delete(string slotId)
    {
        try
        {
            if (File.Exists(JsonPath(slotId))) File.Delete(JsonPath(slotId));
            if (File.Exists(ThumbPath(slotId))) File.Delete(ThumbPath(slotId));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Could not delete save '{slotId}': {e.Message}");
        }
    }

    /// <summary>Thumbnail as a texture (caller should Destroy it when done), or null.</summary>
    public static Texture2D ReadThumbnail(string slotId)
    {
        try
        {
            if (!File.Exists(ThumbPath(slotId))) return null;
            var tex = new Texture2D(2, 2);
            return tex.LoadImage(File.ReadAllBytes(ThumbPath(slotId))) ? tex : null;
        }
        catch { return null; }
    }

    /// <summary>Slot with the newest save (for "Continue"), or null.</summary>
    public static string NewestSlot()
    {
        string best = null;
        long bestTicks = long.MinValue;
        foreach (string s in AllSlots())
        {
            SaveData d = Read(s);
            if (d != null && d.savedAtTicks > bestTicks) { bestTicks = d.savedAtTicks; best = s; }
        }
        return best;
    }

    public static string FormatPlayTime(float seconds)
    {
        var t = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
        return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
    }

    /// <summary>
    /// Screenshot of the current frame, scaled to 320x180, as JPG. Must run inside a coroutine
    /// (it waits for the end of the frame). Result goes to onDone (null if it failed).
    /// </summary>
    public static IEnumerator CaptureThumbnail(Action<byte[]> onDone)
    {
        yield return new WaitForEndOfFrame();
        byte[] jpg = null;
        Texture2D full = null, small = null;
        RenderTexture rt = null;
        try
        {
            full = ScreenCapture.CaptureScreenshotAsTexture();
            rt = RenderTexture.GetTemporary(320, 180, 0);
            Graphics.Blit(full, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            small = new Texture2D(320, 180, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, 320, 180), 0, 0);
            small.Apply();
            RenderTexture.active = previous;
            jpg = small.EncodeToJPG(80);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveManager] Thumbnail failed: " + e.Message);
        }
        finally
        {
            if (rt != null) RenderTexture.ReleaseTemporary(rt);
            if (full != null) UnityEngine.Object.Destroy(full);
            if (small != null) UnityEngine.Object.Destroy(small);
        }
        onDone?.Invoke(jpg);
    }
}

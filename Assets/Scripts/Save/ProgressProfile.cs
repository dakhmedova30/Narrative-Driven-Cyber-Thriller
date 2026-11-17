using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Progress shared by ALL save slots (like most visual novels):
///   - which lines have been read  -> "skip read text only"
///   - which chapters were finished -> "skip all" is allowed there
/// Stored in persistentDataPath/profile.json.
/// </summary>
public static class ProgressProfile
{
    [Serializable]
    private class Data
    {
        public List<string> readLines = new List<string>();
        public List<int> completedChapters = new List<int>();
    }

    private static Data data;
    private static HashSet<string> read;
    private static bool dirty;

    private static string FilePath => Path.Combine(Application.persistentDataPath, "profile.json");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        data = null; read = null; dirty = false;
    }

    private static void Ensure()
    {
        if (data != null) return;
        data = new Data();
        try
        {
            if (File.Exists(FilePath)) data = JsonUtility.FromJson<Data>(File.ReadAllText(FilePath)) ?? new Data();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ProgressProfile] Could not read profile.json, starting fresh: " + e.Message);
            data = new Data();
        }
        read = new HashSet<string>(data.readLines);
    }

    private static bool Valid(string lineId) => !string.IsNullOrEmpty(lineId) && lineId != "<unknown>";

    public static bool IsRead(string lineId)
    {
        Ensure();
        return Valid(lineId) && read.Contains(lineId);
    }

    public static void MarkRead(string lineId)
    {
        Ensure();
        if (Valid(lineId) && read.Add(lineId))
        {
            data.readLines.Add(lineId);
            dirty = true;
        }
    }

    public static bool IsChapterCompleted(int chapter)
    {
        Ensure();
        return data.completedChapters.Contains(chapter);
    }

    public static void MarkChapterCompleted(int chapter)
    {
        Ensure();
        if (data.completedChapters.Contains(chapter)) return;
        data.completedChapters.Add(chapter);
        dirty = true;
        Flush();
    }

    /// <summary>Write to disk if anything changed. Called at dialogue end, on save and on quit.</summary>
    public static void Flush()
    {
        if (!dirty || data == null) return;
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data));
            dirty = false;
        }
        catch (Exception e)
        {
            Debug.LogError("[ProgressProfile] Could not write profile.json: " + e.Message);
        }
    }

    /// <summary>Forget all read lines and completed chapters (for testing).</summary>
    public static void ResetAll()
    {
        data = new Data();
        read = new HashSet<string>();
        dirty = true;
        Flush();
    }
}

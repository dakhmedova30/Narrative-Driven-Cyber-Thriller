using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

/// <summary>
/// Reads the current game state into a SaveData, and puts a SaveData back into a freshly loaded scene.
/// To save something new later: add a field to SaveData, read it in Capture, write it in ApplyEarly/ApplyLate.
/// </summary>
public static class StateCapture
{
    public static SaveData Capture(Scene scene)
    {
        var d = new SaveData { sceneName = scene.name };

        // Player
        PlayerMover player = Find<PlayerMover>(scene);
        if (player != null)
        {
            d.hasPlayer = true;
            d.playerX = player.transform.position.x;
            d.playerFacingRight = player.FacingRight;
        }

        // Yarn variables (merged from every variable storage in the scene)
        foreach (VariableStorageBehaviour storage in All<VariableStorageBehaviour>(scene))
        {
            var (floats, strings, bools) = storage.GetAllVariables();
            foreach (var kv in floats) Put(d.floats, kv.Key, new FloatVar { name = kv.Key, value = kv.Value });
            foreach (var kv in strings) Put(d.strings, kv.Key, new StringVar { name = kv.Key, value = kv.Value });
            foreach (var kv in bools) Put(d.bools, kv.Key, new BoolVar { name = kv.Key, value = kv.Value });
        }

        // Background / audio
        if (BackgroundManager.Instance != null) d.background = BackgroundManager.Instance.CurrentName;
        if (AudioManager.Instance != null)
        {
            d.music = AudioManager.Instance.CurrentMusic;
            d.ambience = AudioManager.Instance.CurrentAmbience;
        }

        // Jaycord
        JaycordApp app = Find<JaycordApp>(scene);
        if (app != null)
        {
            d.hasJaycord = true;
            d.jaycordCurrent = app.Current != null ? app.Current.name : "";
            foreach (JaycordContact c in app.contacts)
            {
                d.jaycordContacts.Add(new JaycordContactState
                {
                    name = c.name, presence = (int)c.presence, status = c.status, handle = c.handle,
                    preview = c.preview, unread = c.unread, level = c.level,
                    dossier = c.dossier != null ? c.dossier.ToList() : new List<JaycordDossierField>(),
                    trust = c.trust, tags = c.tags ?? new string[0],
                    history = c.history != null ? new List<JaycordRecord>(c.history) : new List<JaycordRecord>(),
                });
            }
        }

        // Deep copy, so later changes in the game don't change this snapshot.
        return JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(d));
    }

    /// <summary>Right after the scene loads, before any Start(): player position and Yarn variables.</summary>
    public static void ApplyEarly(SaveData d, Scene scene)
    {
        PlayerMover player = Find<PlayerMover>(scene);
        if (player != null && d.hasPlayer)
        {
            player.Teleport(d.playerX);
            player.SetFacing(d.playerFacingRight);
        }

        var floats = d.floats.ToDictionary(v => v.name, v => v.value);
        var strings = d.strings.ToDictionary(v => v.name, v => v.value);
        var bools = d.bools.ToDictionary(v => v.name, v => v.value);
        foreach (VariableStorageBehaviour storage in All<VariableStorageBehaviour>(scene))
            storage.SetAllVariables(floats, strings, bools, true);
    }

    /// <summary>One frame later (after Start()): Jaycord, background, music.</summary>
    public static void ApplyLate(SaveData d, Scene scene)
    {
        if (BackgroundManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(d.background)) BackgroundManager.Instance.Show(d.background);
            else BackgroundManager.Instance.Clear();
        }

        if (AudioManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(d.music)) AudioManager.Instance.PlayMusic(d.music, 0.5f);
            else AudioManager.Instance.StopMusic(0.5f);
            if (!string.IsNullOrEmpty(d.ambience)) AudioManager.Instance.PlayAmbience(d.ambience, 0.5f);
            else AudioManager.Instance.StopAmbience(0.5f);
        }

        JaycordApp app = Find<JaycordApp>(scene);
        if (app != null && d.hasJaycord)
        {
            foreach (JaycordContactState s in d.jaycordContacts)
            {
                JaycordContact c = app.GetOrAdd(s.name);
                c.presence = (JaycordPresence)s.presence;
                c.status = s.status; c.handle = s.handle; c.preview = s.preview; c.unread = s.unread;
                c.level = s.level; c.dossier = s.dossier.ToArray(); c.trust = s.trust; c.tags = s.tags;
                c.history = new List<JaycordRecord>(s.history);
            }
            app.BuildList();

            JaycordPresenter presenter = Find<JaycordPresenter>(scene);
            string current = !string.IsNullOrEmpty(d.jaycordCurrent) ? d.jaycordCurrent : app.startContact;
            if (!string.IsNullOrEmpty(current))
            {
                if (presenter != null) presenter.ShowConversation(current);
                else app.Select(current);
            }
        }
    }

    // ------------------------------------------------------------------

    private static void Put<T>(List<T> list, string name, T item) where T : class
    {
        int i = list.FindIndex(x => GetName(x) == name);
        if (i >= 0) list[i] = item; else list.Add(item);
    }

    private static string GetName(object v) => v switch
    {
        FloatVar f => f.name,
        StringVar s => s.name,
        BoolVar b => b.name,
        _ => null,
    };

    public static T Find<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T c = root.GetComponentInChildren<T>(true);
            if (c != null) return c;
        }
        return null;
    }

    public static List<T> All<T>(Scene scene) where T : Component
    {
        var result = new List<T>();
        if (!scene.IsValid() || !scene.isLoaded) return result;
        foreach (GameObject root in scene.GetRootGameObjects())
            result.AddRange(root.GetComponentsInChildren<T>(true));
        return result.Distinct().ToList();
    }
}

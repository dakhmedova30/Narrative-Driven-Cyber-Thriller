using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Inspector for the Game Flow (Tools > Game > Open Game Flow):
///   - drag steps to reorder, + / - to add or remove
///   - ▶ on a step: press Play starting at that step (earlier story variables are defaults)
///   - Check: finds missing scenes, scenes not in the build list, unknown Yarn nodes / spawn points
///   - Sync Scene List: flow scenes first (in order) in Build Profiles; other scenes kept after them
/// </summary>
[CustomEditor(typeof(GameFlow))]
public class GameFlowEditor : Editor
{
    private const string AssetPath = "Assets/Resources/GameFlow.asset";

    private ReorderableList list;
    private string[] sceneNames = new string[0];
    private Dictionary<string, string> scenePaths = new Dictionary<string, string>();
    private List<string> problems;

    [MenuItem("Tools/Game/Open Game Flow")]
    public static void Open()
    {
        var flow = AssetDatabase.LoadAssetAtPath<GameFlow>(AssetPath);
        if (flow == null)
        {
            Directory.CreateDirectory("Assets/Resources");
            flow = CreateInstance<GameFlow>();
            flow.steps = new List<GameFlow.Step>
            {
                new GameFlow.Step { name = "Title", scene = "Title" },
                new GameFlow.Step { name = "Chapter 0", scene = "SampleScene", startNode = "Start", nextAfterNode = "C0_End" },
                new GameFlow.Step { name = "Street", scene = "Street" },
            };
            AssetDatabase.CreateAsset(flow, AssetPath);
            AssetDatabase.SaveAssets();
        }
        Selection.activeObject = flow;
        EditorGUIUtility.PingObject(flow);
    }

    private void OnEnable()
    {
        RefreshScenes();
        list = new ReorderableList(serializedObject, serializedObject.FindProperty("steps"), true, true, true, true);
        float line = EditorGUIUtility.singleLineHeight + 2f;

        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Steps  (step 0 = title screen, last step → back to title)");
        list.elementHeightCallback = _ => line * 5 + 8f;
        list.drawElementCallback = (rect, index, active, focused) =>
        {
            SerializedProperty el = list.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2f;
            var r = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

            // Row 0: number, name, play button
            EditorGUI.LabelField(new Rect(r.x, r.y, 28f, r.height), index.ToString(), EditorStyles.boldLabel);
            EditorGUI.PropertyField(new Rect(r.x + 30f, r.y, r.width - 30f - 64f, r.height), el.FindPropertyRelative("name"), GUIContent.none);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUI.Button(new Rect(r.xMax - 60f, r.y, 60f, r.height), new GUIContent("▶ Play", "Play starting at this step")))
                    PlayFrom(index);
            }

            // Row 1: scene dropdown
            r.y += line;
            SerializedProperty sceneProp = el.FindPropertyRelative("scene");
            string current = sceneProp.stringValue;
            var options = sceneNames.ToList();
            int selected = options.IndexOf(current);
            if (selected < 0)
            {
                options.Insert(0, string.IsNullOrEmpty(current) ? "(choose a scene)" : current + "  (not found!)");
                selected = 0;
            }
            int picked = EditorGUI.Popup(r, "Scene", selected, options.ToArray());
            if (picked != selected && !options[picked].StartsWith("(choose") && !options[picked].EndsWith("(not found!)"))
                sceneProp.stringValue = options[picked];

            // Rows 2-4
            r.y += line; EditorGUI.PropertyField(r, el.FindPropertyRelative("spawn"), new GUIContent("Spawn Point"));
            r.y += line; EditorGUI.PropertyField(r, el.FindPropertyRelative("startNode"), new GUIContent("Start Node"));
            r.y += line; EditorGUI.PropertyField(r, el.FindPropertyRelative("nextAfterNode"), new GUIContent("Next After Node"));
        };
        list.onAddCallback = l =>
        {
            int i = l.serializedProperty.arraySize;
            l.serializedProperty.InsertArrayElementAtIndex(i);
            SerializedProperty el = l.serializedProperty.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("name").stringValue = "New Step";
            foreach (string f in new[] { "scene", "spawn", "startNode", "nextAfterNode" })
                el.FindPropertyRelative(f).stringValue = "";
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Main story order. Move to the next step with: \"Next After Node\" finishing, <<next>> in Yarn, " +
            "or an AreaExit with \"Advances Game Flow\" ticked.", MessageType.Info);
        list.DoLayoutList();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Check")) problems = Check((GameFlow)target);
        if (GUILayout.Button("Sync Scene List")) { SyncSceneList((GameFlow)target); problems = Check((GameFlow)target); }
        if (GUILayout.Button("Refresh Scenes")) RefreshScenes();
        EditorGUILayout.EndHorizontal();

        if (problems != null)
        {
            if (problems.Count == 0) EditorGUILayout.HelpBox("All good.", MessageType.None);
            foreach (string p in problems) EditorGUILayout.HelpBox(p, MessageType.Warning);
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ------------------------------------------------------------------

    private void RefreshScenes()
    {
        scenePaths = new Dictionary<string, string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            if (!scenePaths.ContainsKey(name)) scenePaths[name] = path;
        }
        sceneNames = scenePaths.Keys.OrderBy(n => n).ToArray();
    }

    private void PlayFrom(int index)
    {
        var flow = (GameFlow)target;
        string sceneName = flow.steps[index].scene;
        if (!scenePaths.TryGetValue(sceneName, out string path))
        {
            EditorUtility.DisplayDialog("Game Flow", $"Step {index} has no valid scene.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EditorPrefs.SetInt(GameFlow.DebugStartKey, index);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        EditorApplication.isPlaying = true;
    }

    private void SyncSceneList(GameFlow flow)
    {
        var flowPaths = flow.steps
            .Select(s => scenePaths.TryGetValue(s.scene ?? "", out string p) ? p : null)
            .Where(p => p != null).Distinct().ToList();
        var others = EditorBuildSettings.scenes.Where(s => !flowPaths.Contains(s.path));
        EditorBuildSettings.scenes = flowPaths.Select(p => new EditorBuildSettingsScene(p, true)).Concat(others).ToArray();
        Debug.Log("[GameFlow] Scene list synced: " + string.Join(" → ", flowPaths.Select(Path.GetFileNameWithoutExtension)));
    }

    private List<string> Check(GameFlow flow)
    {
        var result = new List<string>();
        if (flow.steps.Count < 2) result.Add("Add at least 2 steps: step 0 (Title) and a first game step.");

        var buildPaths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
        var nodes = new HashSet<string>();
        foreach (string p in Directory.GetFiles("Assets", "*.yarn", SearchOption.AllDirectories))
        {
            foreach (Match m in Regex.Matches(File.ReadAllText(p), @"(?m)^title:\s*(\S+)")) nodes.Add(m.Groups[1].Value);
        }

        for (int i = 0; i < flow.steps.Count; i++)
        {
            GameFlow.Step s = flow.steps[i];
            string where = $"Step {i} \"{s.name}\": ";
            if (string.IsNullOrEmpty(s.scene) || !scenePaths.TryGetValue(s.scene, out string path))
            {
                result.Add(where + "scene is missing.");
                continue;
            }
            if (!buildPaths.Contains(path)) result.Add(where + $"scene {s.scene} is not in the Build Profiles scene list (click Sync Scene List).");
            if (!string.IsNullOrEmpty(s.startNode) && !nodes.Contains(s.startNode)) result.Add(where + $"Start Node \"{s.startNode}\" not found in any .yarn file.");
            if (!string.IsNullOrEmpty(s.nextAfterNode) && !nodes.Contains(s.nextAfterNode)) result.Add(where + $"Next After Node \"{s.nextAfterNode}\" not found in any .yarn file.");
            if (!string.IsNullOrEmpty(s.spawn) && !Regex.IsMatch(File.ReadAllText(path), @"(?m)^\s+id: " + Regex.Escape(s.spawn) + @"\s*$"))
                result.Add(where + $"no SpawnPoint with Id \"{s.spawn}\" in {s.scene}.");
        }
        return result;
    }
}

/// <summary>Puts the normal "play the open scene" back after a ▶ test run.</summary>
[InitializeOnLoad]
public static class GameFlowPlayModeReset
{
    static GameFlowPlayModeReset()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorSceneManager.playModeStartScene = null;
                EditorPrefs.DeleteKey(GameFlow.DebugStartKey);
            }
        };
    }
}

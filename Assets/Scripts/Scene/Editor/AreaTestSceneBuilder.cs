using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Tools > Scenes > Create Area Test Scenes (A ⇄ B)
///
/// Copies MovementTest into AreaTest_A and AreaTest_B and wires them together:
///   AreaTest_A: right edge = Auto exit,       left edge = ClickArrow exit
///   AreaTest_B: right edge = ClickArrow exit, left edge = Auto exit
/// Walking off the right of one scene enters the other from the left, and vice versa.
/// Both scenes are added to the Build Profiles scene list. MovementTest itself is not changed.
/// </summary>
public static class AreaTestSceneBuilder
{
    private const string Title = "Area Test Scenes";
    private const string Source = "Assets/Scenes/MovementTest.unity";
    private const string PathA = "Assets/Scenes/AreaTest_A.unity";
    private const string PathB = "Assets/Scenes/AreaTest_B.unity";

    [MenuItem("Tools/Scenes/Create Area Test Scenes (A ⇄ B)")]
    public static void Create()
    {
        if (!File.Exists(Source))
        {
            EditorUtility.DisplayDialog(Title, "Can't find " + Source + ".\nThe test scenes are copied from it.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (File.Exists(PathA) || File.Exists(PathB))
        {
            if (!EditorUtility.DisplayDialog(Title, "AreaTest_A / AreaTest_B already exist. Replace them?", "Replace", "Cancel")) return;
            AssetDatabase.DeleteAsset(PathA);
            AssetDatabase.DeleteAsset(PathB);
        }

        if (!AssetDatabase.CopyAsset(Source, PathA) || !AssetDatabase.CopyAsset(Source, PathB))
        {
            EditorUtility.DisplayDialog(Title, "Copying MovementTest failed. See the Console.", "OK");
            return;
        }

        Setup(PathA, "AREA A", "AreaTest_B", new Color(0.25f, 0.65f, 0.7f), AreaExit.Mode.Auto, AreaExit.Mode.ClickArrow);
        Setup(PathB, "AREA B", "AreaTest_A", new Color(0.85f, 0.5f, 0.25f), AreaExit.Mode.ClickArrow, AreaExit.Mode.Auto);
        AddToBuildList(PathA, PathB);

        EditorSceneManager.OpenScene(PathA, OpenSceneMode.Single);
        EditorUtility.DisplayDialog(Title,
            "Created AreaTest_A and AreaTest_B (now open: A).\n\n" +
            "Press Play and walk:\n" +
            "• right edge of A → automatic\n" +
            "• left edge of A → arrow, click it\n" +
            "B is the other way round.", "OK");
    }

    private static void Setup(string path, string label, string otherScene, Color floorColor,
                              AreaExit.Mode rightMode, AreaExit.Mode leftMode)
    {
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        PlayerMover player = Object.FindAnyObjectByType<PlayerMover>();
        float min = player != null ? player.MinX : -8f;
        float max = player != null ? player.MaxX : 8f;
        Vector3 basePos = player != null ? player.transform.position : Vector3.zero;

        GameObject floor = GameObject.Find("Floor");
        if (floor != null && floor.TryGetComponent(out SpriteRenderer floorSr)) floorSr.color = floorColor;

        var root = new GameObject("Area Test");

        Spawn(root, "from_left", min + 2.5f, basePos, faceRight: true, isDefault: true);
        Spawn(root, "from_right", max - 2.5f, basePos, faceRight: false, isDefault: false);

        Exit(root, "Exit Right", max - 0.75f, basePos, rightMode, otherScene, "from_left", AreaExit.ArrowDirection.Right);
        Exit(root, "Exit Left", min + 0.75f, basePos, leftMode, otherScene, "from_right", AreaExit.ArrowDirection.Left);

        var labelGo = new GameObject("Label " + label);
        labelGo.transform.SetParent(root.transform, false);
        labelGo.transform.position = new Vector3(0f, basePos.y + 4.5f, basePos.z + 0.5f);
        var tmp = labelGo.AddComponent<TextMeshPro>();
        tmp.text = label;
        tmp.fontSize = 8f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = floorColor;
        tmp.rectTransform.sizeDelta = new Vector2(10f, 2f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void Spawn(GameObject parent, string id, float x, Vector3 basePos, bool faceRight, bool isDefault)
    {
        var go = new GameObject("Spawn " + id);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(x, basePos.y, basePos.z);
        var sp = go.AddComponent<SpawnPoint>();

        var so = new SerializedObject(sp);
        so.FindProperty("id").stringValue = id;
        so.FindProperty("faceRight").boolValue = faceRight;
        so.FindProperty("isDefault").boolValue = isDefault;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Exit(GameObject parent, string name, float x, Vector3 basePos, AreaExit.Mode mode,
                             string targetScene, string targetSpawn, AreaExit.ArrowDirection dir)
    {
        var go = new GameObject(name + " (" + mode + ")");
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(x, basePos.y, basePos.z);
        var exit = go.AddComponent<AreaExit>();

        var so = new SerializedObject(exit);
        so.FindProperty("mode").enumValueIndex = (int)mode;
        so.FindProperty("targetScene").stringValue = targetScene;
        so.FindProperty("targetSpawn").stringValue = targetSpawn;
        so.FindProperty("zoneWidth").floatValue = 1.5f;
        so.FindProperty("arrowDirection").enumValueIndex = (int)dir;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddToBuildList(params string[] paths)
    {
        var list = EditorBuildSettings.scenes.ToList();
        foreach (string p in paths)
            if (!list.Any(s => s.path == p)) list.Add(new EditorBuildSettingsScene(p, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}

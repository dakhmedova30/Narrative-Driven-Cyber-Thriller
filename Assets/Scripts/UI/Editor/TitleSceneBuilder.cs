using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Tools > Game > Create Title Scene
/// Makes Assets/Scenes/Title.unity (camera + TitleScreen) and puts it FIRST in the Build Profiles scene list,
/// so the built game starts on the title screen.
/// </summary>
public static class TitleSceneBuilder
{
    private const string Title = "Title Scene";
    private const string ScenePath = "Assets/Scenes/Title.unity";

    [MenuItem("Tools/Game/Create Title Scene")]
    public static void Create()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog(Title, "Title.unity already exists. Replace it?", "Replace", "Cancel"))
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        new GameObject("Title Screen").AddComponent<TitleScreen>();

        EditorSceneManager.SaveScene(scene, ScenePath);

        var list = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();

        EditorUtility.DisplayDialog(Title,
            "Created Assets/Scenes/Title.unity and made it the first scene of the build.\n\n" +
            "New Game starts step 1 of the Game Flow (Tools > Game > Open Game Flow).", "OK");
    }
}

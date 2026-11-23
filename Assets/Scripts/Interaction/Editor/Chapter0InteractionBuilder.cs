using System;
using System.IO;
using RoomInteraction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Yarn.Unity;

public static class Chapter0InteractionBuilder
{
    const string Fast = "Assets/Scenes/Chapter0_InteractionTest.unity";
    const string Full = "Assets/Scenes/Chapter0_InteractionFull.unity";
    const string Project = "Assets/Chapter0Interaction/Story/Chapter0Interaction.yarnproject";
    [MenuItem("Tools/Interaction/Open Chapter 0 Room Test")]
    public static void OpenTest()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Fast);
    }
    public static void PlayTest()
    {
        EditorSceneManager.OpenScene(Fast);
        EditorApplication.isPlaying = true;
    }
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Prefabs/Interaction/Chapter0");
        AssetDatabase.Refresh();
        foreach (var path in new[] { Fast, Full })
        {
            if (!File.Exists(path) && !AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", path)) throw new Exception("Scene copy failed");
            var scene = EditorSceneManager.OpenScene(path);
            var old = UnityEngine.Object.FindFirstObjectByType<RoomExploration>();
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var runner = UnityEngine.Object.FindFirstObjectByType<DialogueRunner>();
            if (runner == null) throw new Exception("Original dialogue runner missing");
            var project = AssetDatabase.LoadAssetAtPath<YarnProject>(Project);
            if (project == null || project.Program == null) throw new Exception("Test Yarn project failed to compile");
            runner.SetProject(project); runner.autoStart = true; runner.startNode = path == Fast ? "C0_GoHome" : "Start";
            EditorUtility.SetDirty(runner); PrefabUtility.RecordPrefabInstancePropertyModifications(runner);
            var root = new GameObject("Chapter 0 · Optional room exploration");
            var controller = root.AddComponent<RoomExploration>();
            controller.roomImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Scenes/room__2027.png");
            controller.items = new RoomInteractable[4];
            string[] ids = { "calendar", "computer", "bed", "door" };
            string[] labels = { "Calendar", "Computer", "Bed", "Door" };
            Rect[] rects = {new Rect(.328f,.639f,.146f,.278f),new Rect(.29f,.30f,.13f,.22f),new Rect(.01f,.10f,.22f,.22f),new Rect(.72f,.12f,.22f,.72f)};
            for (int i = 0; i < ids.Length; i++)
            {
                var item = new GameObject(labels[i]).AddComponent<RoomInteractable>(); item.transform.SetParent(root.transform);
                item.itemId = ids[i]; item.displayName = labels[i]; item.region = rects[i];
                if (i == 0) { item.inspectionImage = controller.roomImage; item.inspectionCrop = rects[i]; }
                controller.items[i] = item;
            }
            if (path == Fast) PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Interaction/Chapter0/RoomExploration.prefab");
            controller.runner = runner;
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Chapter0Interaction] Saved " + path);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Fast);
        var check = UnityEngine.Object.FindFirstObjectByType<DialogueRunner>();
        if (check.YarnProject == null || check.startNode != "C0_GoHome") throw new Exception("Saved scene failed validation");
        Directory.CreateDirectory("output/chapter0-interaction/Windows");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Fast, Full }, locationPathName = "output/chapter0-interaction/Windows/Chapter0Interaction.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Player build failed");
        Debug.Log("[Chapter0Interaction] BUILD SUCCEEDED");
    }
}

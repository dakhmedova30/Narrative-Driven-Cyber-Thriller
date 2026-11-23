using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CafeDraftBuilder
{
    const string Scene="Assets/Scenes/CafeDraft.unity";
    [MenuItem("Tools/Cafe/Open Visual Draft")]
    public static void Open()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(!File.Exists(Scene))Create();
        EditorSceneManager.OpenScene(Scene);
    }
    public static void Create()
    {
        Directory.CreateDirectory("Assets/Prefabs/Cafe");AssetDatabase.Refresh();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.gameObject.AddComponent<AudioListener>();
        var draft=new GameObject("Cafe visual draft").AddComponent<CafeDraft>();
        draft.background=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Scenes/CafeDraft/cafe-night-background.png");
        draft.visitor=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Scenes/CafeDraft/visitor-placeholder.png");
        draft.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Jaycord/ChakraPetch-Regular SDF.asset");
        PrefabUtility.SaveAsPrefabAsset(draft.gameObject,"Assets/Prefabs/Cafe/CafeConversationDraft.prefab");
        EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();
    }
    public static void Build()
    {
        try
        {
            Create();Directory.CreateDirectory("output/cafe/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},target=BuildTarget.StandaloneWindows64,locationPathName="output/cafe/Windows/CafeDraft.exe",options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Cafe build failed");
            Debug.Log("[Cafe] BUILD SUCCEEDED");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}

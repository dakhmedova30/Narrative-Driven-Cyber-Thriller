using System;
using System.IO;
using DopamineMeter;
using Exploration;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InteractionDemoBuilder
{
    const string Prefabs="Assets/Prefabs/Interaction";
    const string ScenePath="Assets/Scenes/InteractionDemo.unity";
    static T Load<T>(string path) where T:UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
    [MenuItem("Tools/Interaction/Open Demo")]
    public static void OpenDemo()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(!File.Exists(ScenePath))CreateAssets();
        EditorSceneManager.OpenScene(ScenePath);
    }
    [MenuItem("Tools/Interaction/Rebuild Demo Assets")]
    public static void Rebuild()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(!EditorUtility.DisplayDialog("Rebuild interaction demo", "This replaces the generated interaction prefabs and demo scene. Custom scenes are not changed.","Rebuild","Cancel"))return;
        CreateAssets();
    }
    public static void CreateAssets()
    {
        Directory.CreateDirectory(Prefabs);AssetDatabase.Refresh();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=4.5f;camera.backgroundColor=InteractionUI.Ink;camera.clearFlags=CameraClearFlags.SolidColor;
        camera.gameObject.AddComponent<AudioListener>();
        // Demo objects are positioned by a dedicated viewport fitter, matching the room image.
        camera.gameObject.AddComponent<InteractionDemoCamera>();
        var font=Load<TMP_FontAsset>("Assets/Fonts/Jaycord/ChakraPetch-Regular SDF.asset");
        var heading=Load<TMP_FontAsset>("Assets/Fonts/Jaycord/Orbitron-Bold SDF.asset");
        if(font==null||heading==null)throw new Exception("Demo fonts missing");
        var system=new GameObject("InteractionSystem").AddComponent<InteractionSystem>();system.font=font;
        PrefabUtility.SaveAsPrefabAsset(system.gameObject,Prefabs+"/InteractionSystem.prefab");
        system.worldCamera=camera;
        string[] names={"Terminal","Paper note","Delivery","Exit"};
        string[] files={"Terminal","PaperNote","DeliveryPackage","ExitDoor"};
        string[] categories={"01 / INCOMING MESSAGE","02 / ANALOG TRACE","03 / SEALED DELIVERY","04 / NEXT LOCATION"};
        string[] descriptions={"MAX / 02:17 AM\n\nI left something for you. Check the note before you open the package.\n\nDon't trust everything on the screen.\n\nDigital exposure: +5", "Someone has written on the back of an old receipt:\n\n\"The device is yours. The choice is still yours, too.\"\n\nFor a moment, the room feels quiet.\n\nAttention recovered: -10", "A compact device rests inside the packaging. Its status light blinks once.\n\nCROW / PERSONAL ASSISTANT\n\nYou pocket the device. The corridor door is now available.\n\nDigital exposure: +8", "You have the message, the note and the device.\n\nThere is nothing else keeping you here.\n\nStep into the corridor."};
        string[] actions={"READ MESSAGE","KEEP NOTE","COLLECT DEVICE","ENTER CORRIDOR"};
        string[] arts={"Assets/Art/Scenes/desk-2027-resized.png","Assets/Art/UI/Interaction/paper-note.png","Assets/Art/Scenes/LLM-package/4_item_show.png","Assets/Art/Scenes/hallway-resized.png"};
        Vector3[] positions={new Vector3(-2.6f,-.5f,0),new Vector3(-1.5f,2.7f,0),new Vector3(2.6f,-.6f,0),new Vector3(5.5f,1.3f,0)};
        var items=new Interactable[4];
        for(int i=0;i<4;i++)
        {
            var item=new GameObject(names[i]).AddComponent<Interactable>();items[i]=item;
            item.displayName=names[i];item.category=categories[i];item.description=descriptions[i];item.actionLabel=actions[i];item.artwork=Load<Texture2D>(arts[i]);
            if(i<3)item.gameObject.AddComponent<InteractionMeterEffect>().amount=new[]{5,-10,8}[i];
            PrefabUtility.SaveAsPrefabAsset(item.gameObject,Prefabs+"/"+files[i]+".prefab");
            UnityEngine.Object.DestroyImmediate(item.gameObject);
            items[i]=((GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(Prefabs+"/"+files[i]+".prefab"))).GetComponent<Interactable>();
            items[i].transform.position=positions[i];
            if(i>0)items[i].prerequisite=items[i-1];
            PrefabUtility.RecordPrefabInstancePropertyModifications(items[i]);
        }
        system.objects=items;
        var meter=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>("Assets/Prefabs/DopamineMeter/DopamineMeter.prefab"));
        meter.name="DopamineMeter (existing global service)";
        var demo=new GameObject("Interaction demo presentation").AddComponent<InteractionDemo>();demo.system=system;demo.steps=items;demo.font=font;demo.headingFont=heading;
        demo.roomArt=Load<Texture2D>("Assets/Art/Scenes/room__2027.png");demo.hallwayArt=Load<Texture2D>("Assets/Art/Scenes/hallway-resized.png");
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
    }
    public static void BuildAndCheck()
    {
        try
        {
            CreateAssets();
            Directory.CreateDirectory("output/interaction/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="output/interaction/Windows/InteractionDemo.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            Debug.Log("[Interaction] BUILD SUCCEEDED");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}

using System;
using System.IO;
using DopamineMeter;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MeterDemoBuilder
{
    const string Root = "Assets/DopamineMeter";
    const string Prefabs = "Assets/Prefabs/DopamineMeter";
    [MenuItem("Tools/Dopamine Meter/Open Demo")]
    public static void OpenDemo()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(Root+"/Demo/DopamineDemo.unity")) CreateAssets();
        EditorSceneManager.OpenScene(Root+"/Demo/DopamineDemo.unity");
    }
    [MenuItem("Tools/Dopamine Meter/Run Core Checks")]
    public static void CheckCore()
    {
        int checks=0;
        Action<bool,string> require=(ok,label)=>{if(!ok)throw new Exception("Meter check failed: "+label); checks++;};
        var m=new MeterModel(rate:5);int deaths=0;m.Failed+=()=>deaths++;
        require(!m.Apply(5,"dormant","ignored")&&m.Value==50,"dormant ignores input");
        m.Activate();m.Apply(5,"first","query");m.Activate();require(m.Value==55,"activation is idempotent");
        require(!m.Apply(5,"first","duplicate")&&m.Value==55,"occurrence deduplication");
        m.SetDigitalUse(true);m.Tick(19);require(m.Value==55,"no growth before 20 seconds");
        m.Tick(1);require(m.Value==60&&m.Band==MeterBand.Intrusive,"20 second step and threshold");
        m.Suspend("one",true);m.Suspend("two",true);m.Tick(99);require(m.Value==60,"pause clock");
        require(!m.Apply(-10,"paused","ignored"),"pause events");
        m.Suspend("one",false);require(m.Suspended,"multiple pause owners");
        m.Suspend("two",false);m.SetDigitalUse(false);m.Tick(20);require(m.Value==60,"no passive decay");
        m.Apply(-100,"recover","reading");require(m.Value==0,"lower clamp");
        foreach(float target in new[]{39f,40f,59f,60f,79f,80f,99f})
        {
            m.Apply(target-m.Value,"threshold"+target,"boundary");
            var expected=target>=80?MeterBand.Critical:target>=60?MeterBand.Intrusive:target>=40?MeterBand.Engaged:MeterBand.Stable;
            require(m.Band==expected,"band "+target);
        }
        m.Apply(200,"lethal","overload");require(m.Value==100&&m.Life==MeterLife.Dead&&deaths==1,"death");
        require(!m.Apply(-10,"afterdeath","ignored")&&deaths==1,"death latch");
        var snapshot=m.Capture();m.Reset();require(m.Value==50&&m.Life==MeterLife.Dormant,"reset");
        m.Restore(snapshot);require(m.Life==MeterLife.Dead&&deaths==1,"restore without death replay");
        m.Reset();m.Activate();m.Apply(5,"saved","query");snapshot=m.Capture();m.Reset();m.Restore(snapshot);
        require(!m.Apply(5,"saved","duplicate")&&m.Value==55,"snapshot preserves IDs");
        require(!m.Apply(float.NaN,"nan","invalid"),"NaN rejected");
        m.Tick(float.PositiveInfinity);require(m.Value==55,"infinite dt rejected");
        bool invalid=false;try{m.Restore(new MeterSnapshot{version=9});}catch(ArgumentException){invalid=true;}
        require(invalid,"unknown save version rejected");
        var timer=new MeterModel();timer.Activate();timer.SetDigitalUse(true);timer.Tick(10);
        timer.Tick(120,false);require(timer.Value==50&&timer.ElapsedSeconds==10,"dialogue freezes elapsed time");
        require(timer.Apply(5,"dialogue reward","story"),"dialogue allows explicit event");
        timer.Tick(10);require(timer.Value==55.5f,"resume remainder without catch-up");
        timer.SetDigitalUse(false);timer.Tick(100);require(timer.Value==55.5f,"timed growth disabled");
        timer.SetDigitalUse(true);timer.Tick(45);require(timer.Value==56.5f&&timer.ElapsedSeconds==5,"multiple intervals with remainder");
        var savedTimer=timer.Capture();timer.Reset();timer.Restore(savedTimer);
        require(timer.ElapsedSeconds==5&&!timer.DigitalUse,"snapshot remainder and disabled transient mode");
        var retry=new MeterModel(initial:35);retry.Activate();
        foreach(float expected in new[]{40f,45f,50f,50f})
        {
            retry.Apply(100,Guid.NewGuid().ToString(),"death");require(retry.RestartValue==expected,"death penalty capped");
            retry.Respawn();require(retry.Value==expected&&retry.Life==MeterLife.Active,"respawn baseline");
            retry.Respawn();require(retry.Value==expected,"respawn idempotent");
        }
        retry.Reset();require(retry.RestartValue==35&&retry.Value==35,"new game clears death penalty");
        var cap=new MeterModel();cap.Activate();cap.Apply(100,"death","death");cap.Respawn();require(cap.Value==50,"initial 50 never exceeds cap");
        var deathSave=retry.Capture();retry.Activate();retry.Apply(100,"dead","death");deathSave=retry.Capture();
        retry.Reset();retry.Restore(deathSave);retry.Respawn();require(retry.Value==40,"death baseline saved");
        Directory.CreateDirectory("output/dopamine");File.WriteAllText("output/dopamine/core-checks.txt",checks+" core checks passed.");
        Debug.Log("[Dopamine Meter] "+checks+" core checks passed.");
    }
    public static void CreateAssets()
    {
        Directory.CreateDirectory(Root+"/Demo");
        Directory.CreateDirectory(Prefabs);
        var importer=AssetImporter.GetAtPath(Root+"/Art/neural-sensor.png") as TextureImporter;
        if(importer!=null){importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.SaveAndReimport();}
        var config=AssetDatabase.LoadAssetAtPath<MeterConfig>(Root+"/Demo/MeterTuning.asset");
        if(config==null){config=ScriptableObject.CreateInstance<MeterConfig>();AssetDatabase.CreateAsset(config,Root+"/Demo/MeterTuning.asset");}
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var go=new GameObject("Dopamine Meter");var service=go.AddComponent<MeterService>();service.config=config;
        var view=go.AddComponent<MeterView>();view.service=service;
        view.sensorArt=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/neural-sensor.png");
        view.displayFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Jaycord/Orbitron-Bold SDF.asset");
        view.bodyFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Jaycord/ChakraPetch-Regular SDF.asset");
        PrefabUtility.SaveAsPrefabAsset(go,Prefabs+"/DopamineMeter.prefab"); // portable runtime only
        var bridge=go.AddComponent<MeterGameBridge>();
        PrefabUtility.SaveAsPrefabAsset(go,Prefabs+"/DopamineMeterForGame.prefab");
        UnityEngine.Object.DestroyImmediate(bridge); // Demo does not require a real player or dialogue system.
        var demo=go.AddComponent<MeterDemo>();demo.streetArt=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Scenes/street__2027.png");
        var cam=new GameObject("Main Camera",typeof(Camera));cam.tag="MainCamera";
        cam.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;cam.GetComponent<Camera>().backgroundColor=Color.black;
        EditorSceneManager.SaveScene(scene,Root+"/Demo/DopamineDemo.unity");
        // Deliberately contains a duplicate root: persistent-instance protection is exercised when switching.
        EditorSceneManager.SaveScene(scene,Root+"/Demo/DopamineDemoB.unity");
        EditorSceneManager.OpenScene(Root+"/Demo/DopamineDemo.unity");AssetDatabase.SaveAssets();
    }
    public static void BuildAndCheck()
    {
        try
        {
            CheckCore();CreateAssets();
            Directory.CreateDirectory("output/dopamine/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{Root+"/Demo/DopamineDemo.unity",Root+"/Demo/DopamineDemoB.unity"},
                locationPathName="output/dopamine/Windows/DopamineMeter.exe",target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Demo build failed: "+report.summary.result);
            ExportCurrentPackage();
            Debug.Log("[Dopamine Meter] BUILD AND CHECKS SUCCEEDED");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    [MenuItem("Tools/Dopamine Meter/Export Package")]
    public static void ExportCurrentPackage()
    {
        Directory.CreateDirectory("output/dopamine");
        AssetDatabase.ExportPackage(new[]{Root,Prefabs},"output/dopamine/DopamineMeter.unitypackage",
            ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);
    }
}

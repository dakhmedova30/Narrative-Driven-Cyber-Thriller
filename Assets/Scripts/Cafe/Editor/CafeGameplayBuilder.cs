using System;
using System.IO;
using CafeGameplay;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Yarn.Unity;

public static class CafeGameplayBuilder
{
    const string Root="Assets/CafeGameplay";
    const string Scene="Assets/Scenes/CafeGameplay.unity";
    const string Prefabs="Assets/Prefabs/Cafe";
    static T Load<T>(string path) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(path);
    [MenuItem("Tools/Cafe/Open Gameplay Demo")]
    public static void Open()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(!File.Exists(Scene))Create();EditorSceneManager.OpenScene(Scene);
    }
    [MenuItem("Tools/Cafe/Run Model Checks")]
    public static void Checks()
    {
        int count=0;Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);count++;};
        check(CoffeeStepModel.Judge(.06f,.06f,.16f)==CoffeeGrade.Perfect,"perfect inclusive");
        check(CoffeeStepModel.Judge(.16f,.06f,.16f)==CoffeeGrade.Good,"good inclusive");
        check(CoffeeStepModel.Judge(.17f,.06f,.16f)==CoffeeGrade.Miss,"miss boundary");
        var s=new CoffeeStep{input=CoffeeInput.Timing,travelSeconds=2,target=.65f};var m=new CoffeeStepModel(s);m.Tick(1.3f);m.Press();check(m.Done&&m.Grade==CoffeeGrade.Perfect,"timing perfect");m.Press();m.Tick(10);check(m.Grade==CoffeeGrade.Perfect,"result latched");
        m=new CoffeeStepModel(s);m.Tick(13);check(m.Done&&m.Grade==CoffeeGrade.Miss,"timeout");
        s=new CoffeeStep{input=CoffeeInput.Hold,travelSeconds=2,target=.65f};m=new CoffeeStepModel(s);m.Tick(1);check(m.Position==0,"hold does not fill before press");m.Press();m.Tick(1.3f);m.Release();check(m.Grade==CoffeeGrade.Perfect,"hold perfect");
        m=new CoffeeStepModel(s);m.Press();m.Tick(3);check(m.Done&&m.Grade==CoffeeGrade.Miss,"overheld miss");
        s=new CoffeeStep{input=CoffeeInput.Rhythm,beats=3,beatInterval=1};m=new CoffeeStepModel(s);for(int i=0;i<3;i++){m.Tick(1);m.Press();}check(m.Done&&m.Grade==CoffeeGrade.Perfect,"rhythm perfect");
        m=new CoffeeStepModel(s);m.Press();for(int i=0;i<3;i++){m.Tick(1);m.Press();}check(m.Grade==CoffeeGrade.Miss,"early spam not rewarded");
        m=new CoffeeStepModel(s);m.Tick(10);check(m.Done&&m.Grade==CoffeeGrade.Miss,"missed rhythm beats finish");
        var content=ScriptableObject.CreateInstance<PhoneContent>();content.notificationInterval=2;content.maxUnread=1;content.posts=new[]{new PhonePost{id="a"},new PhonePost{id="b"}};
        var inbox=new PhoneInbox(content);check(inbox.Tick(100,false)==null&&inbox.Unread==0,"notification switch off");
        check(inbox.Tick(1,true)==null,"interval waits");check(inbox.Tick(1,true)!=null&&inbox.Unread==1,"one notification");check(inbox.Tick(50,true)==null,"unread bounded");
        check(inbox.MarkRead(content.posts[0]),"first read");check(!inbox.MarkRead(content.posts[0]),"read deduplicated");check(!inbox.MarkRead(content.posts[1]),"undelivered cannot read");
        check(inbox.Tick(2,true)!=null,"delivery resumes after reading");inbox.MarkRead(content.posts[1]);check(inbox.Tick(99,true)==null,"catalog exhausted safely");
        content.posts[1].id="a";check(!content.IsValid,"duplicate ids rejected");UnityEngine.Object.DestroyImmediate(content);
        Directory.CreateDirectory("output/cafe-gameplay");File.WriteAllText("output/cafe-gameplay/model-checks.txt",count+" checks passed.");
    }
    public static void Create()
    {
        Directory.CreateDirectory(Root+"/Config");Directory.CreateDirectory(Prefabs);AssetDatabase.Refresh();
        var recipe=Load<CoffeeRecipe>(Root+"/Config/LatteRecipe.asset");
        if(recipe==null){recipe=ScriptableObject.CreateInstance<CoffeeRecipe>();AssetDatabase.CreateAsset(recipe,Root+"/Config/LatteRecipe.asset");}
        if(recipe.illustration==null){recipe.illustration=Load<Texture2D>(Root+"/Art/latte-brewing.png");EditorUtility.SetDirty(recipe);}
        var content=Load<PhoneContent>(Root+"/Config/PhoneFeed.asset");
        if(content==null)
        {
            content=ScriptableObject.CreateInstance<PhoneContent>();content.posts=new[]{
                new PhonePost{id="max-message",source="MAX",title="Are you still there?",body="I found something about the delivery. Call me when you have a moment.",meterIncrease=3},
                new PhonePost{id="ad-focus",source="FOCUS+",advertisement=true,title="Make every minute count.",body="A smarter version of you is one upgrade away. Try an assistant that never sleeps.",meterIncrease=5},
                new PhonePost{id="city-thread",source="CITY THREAD",title="Everyone is talking about this.",body="A clip from your neighbourhood is trending. You might recognise someone in it.",meterIncrease=4},
                new PhonePost{id="ad-coffee",source="NIGHT PERKS",advertisement=true,title="One more cup. One more reward.",body="Keep your streak alive. An exclusive offer is waiting for you tonight.",meterIncrease=5},
                new PhonePost{id="friend-ping",source="MHM",title="You have 3 new replies.",body="The conversation moved on while you were away. Scroll back and catch up.",meterIncrease=3},
                new PhonePost{id="ad-upgrade",source="CROW",advertisement=true,title="Let us answer for you.",body="Less effort. More connection. Your next conversation, optimised.",meterIncrease=5}};
            AssetDatabase.CreateAsset(content,Root+"/Config/PhoneFeed.asset");
        }
        if(content.notificationSound==null)
        {
            string audioPath=Root+"/Config/notification.wav";if(!File.Exists(audioPath))WriteDing(audioPath);AssetDatabase.ImportAsset(audioPath);
            content.notificationSound=Load<AudioClip>(audioPath);EditorUtility.SetDirty(content);
        }
        var project=Load<YarnProject>(Root+"/Story/CafeStory.yarnproject");if(project==null||project.Program==null)throw new Exception("Cafe Yarn project did not compile");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.backgroundColor=Color.black;camera.clearFlags=CameraClearFlags.SolidColor;camera.gameObject.AddComponent<AudioListener>();
        var font=Load<TMP_FontAsset>("Assets/Fonts/Jaycord/ChakraPetch-Regular SDF.asset");
        var stage=new GameObject("Cafe stage").AddComponent<CafeStage>();stage.background=Load<Texture2D>("Assets/Art/Scenes/CafeDraft/cafe-night-background.png");stage.visitor=Load<Texture2D>("Assets/Art/Scenes/CafeDraft/visitor-placeholder.png");stage.font=font;
        var coffee=new GameObject("CoffeeQTE").AddComponent<CoffeeQTE>();coffee.recipe=recipe;coffee.font=font;PrefabUtility.SaveAsPrefabAsset(coffee.gameObject,Prefabs+"/CoffeeQTE.prefab");
        var phone=new GameObject("CafePhone").AddComponent<CafePhone>();phone.content=content;phone.font=font;PrefabUtility.SaveAsPrefabAsset(phone.gameObject,Prefabs+"/CafePhone.prefab");phone.coffee=coffee;phone.stage=stage;
        var dialogueGO=new GameObject("Cafe Yarn dialogue");var presenter=dialogueGO.AddComponent<CafeDialoguePresenter>();presenter.font=font;presenter.coffee=coffee;presenter.phone=phone;phone.dialogue=presenter;
        var runner=dialogueGO.AddComponent<DialogueRunner>();
        // Reload after NewScene: a scene replacement can unload an importer-owned project asset.
        runner.SetProject(Load<YarnProject>(Root+"/Story/CafeStory.yarnproject"));EditorUtility.SetDirty(runner);
        runner.autoStart=true;runner.startNode="CafeStart";runner.DialoguePresenters=new[]{presenter};
        var bridge=dialogueGO.AddComponent<CafeYarnBridge>();bridge.coffee=coffee;bridge.phone=phone;bridge.stage=stage;
        PrefabUtility.InstantiatePrefab(Load<GameObject>("Assets/Prefabs/DopamineMeter/DopamineMeter.prefab"));
        var boot=new GameObject("Demo only - Meter lifecycle and verification").AddComponent<CafeDemoBoot>();boot.phone=phone;
        var smoke=boot.gameObject.AddComponent<CafeGameplaySmoke>();smoke.coffee=coffee;smoke.phone=phone;smoke.presenter=presenter;smoke.runner=runner;
        EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Scene);
        var savedRunner=UnityEngine.Object.FindFirstObjectByType<DialogueRunner>();
        if(savedRunner==null||savedRunner.YarnProject==null)throw new Exception("Saved scene lost its Yarn project reference");
    }
    static void WriteDing(string path)
    {
        const int rate=44100;int samples=(int)(rate*.32f);
        using(var writer=new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples*2);
            for(int i=0;i<samples;i++){double t=(double)i/rate;double envelope=Math.Min(1,t/.01)*Math.Exp(-t*12);double tone=Math.Sin(2*Math.PI*880*t)+.45*Math.Sin(2*Math.PI*1320*t);writer.Write((short)(tone*envelope*10000));}
        }
    }
    public static void Build()
    {
        try
        {
            Checks();Create();Directory.CreateDirectory("output/cafe-gameplay/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},target=BuildTarget.StandaloneWindows64,locationPathName="output/cafe-gameplay/Windows/CafeGameplay.exe",options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Cafe gameplay build failed");Debug.Log("[Cafe Gameplay] BUILD SUCCEEDED");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}

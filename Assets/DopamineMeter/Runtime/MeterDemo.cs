using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DopamineMeter
{
    /// <summary>Removable demo harness. This component is NOT included in the portable HUD prefab.</summary>
    public sealed class MeterDemo : MonoBehaviour
    {
        public Texture2D streetArt;
        MeterView view;
        MeterModel model;
        RectTransform lab, worldControls, failure;
        Canvas canvas;
        TextMeshProUGUI number,status,message,history,feedText,pauseText,chapter,clue,life;
        MeterDial dial;
        RawImage sensor;
        Image bar;
        readonly List<Button> actions=new List<Button>();
        readonly Queue<string> log=new Queue<string>();
        Button activate,feed;
        bool paused,world,dialogue;
        float shown;
        string sceneName="CITY / SECTOR 07";
        MeterSnapshot saved;
        int sequence;
        public string TestResult { get; private set; }
        IEnumerator Start()
        {
            view=GetComponent<MeterView>();
            yield return null;
            if (view.service != MeterService.Instance) yield break;
            model=view.Model; shown=model.Value;
            canvas=view.MakeCanvas("Dopamine / demonstration",50); canvas.transform.SetParent(transform,false);
            var es=FindFirstObjectByType<EventSystem>();
            if(es==null) new GameObject("Demo input",typeof(EventSystem),typeof(InputSystemUIInputModule)).transform.SetParent(transform,false);
            Build(); model.Changed+=Refresh; model.Failed+=OnDeath; SceneManager.sceneLoaded+=OnScene;
            AddLog("SYSTEM", "Device ready. Pair to begin."); Refresh();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-meter-smoke")>=0) StartCoroutine(Smoke());
        }
        void Build()
        {
            var stage=MeterView.Rect(canvas.transform,"Stage",0,0,1600,900);
            // Keep the composition centered when the game window changes aspect ratio.
            stage.anchorMin=stage.anchorMax=stage.pivot=new Vector2(.5f,.5f); stage.anchoredPosition=Vector2.zero;
            view.Art(stage,streetArt,0,0,1600,900,Color.white);
            MeterView.Box(stage,"City veil",0,0,1600,900,new Color(.025f,.04f,.07f,.62f));
            view.Label(stage,"RAVEN  /  NEURAL SYSTEMS",50,28,510,30,18,MeterView.Cyan,true);
            chapter=view.Label(stage,sceneName,50,69,400,22,12,MeterView.Muted);
            worldControls=MeterView.Panel(stage,"Field controls",45,683,710,174,new Color(.035f,.055f,.09f,.97f));
            view.Label(worldControls,"FIELD MODE",22,16,320,22,12,MeterView.Cyan);
            view.Label(worldControls,"The same meter. A different scene.",22,45,650,28,21,MeterView.White);
            view.Button(worldControls,"AI insight +5",22,95,205,48,Query);
            view.Button(worldControls,"Read -10",243,95,205,48,Read);
            view.Button(worldControls,"Back to console",464,95,222,48,ToggleWorld);
            worldControls.gameObject.SetActive(false);
            lab=MeterView.Rect(stage,"Calibration console",0,0,1600,900);
            MeterView.Box(lab,"Backdrop",0,110,1600,790,new Color(.025f,.035f,.055f,.96f));
            view.Label(lab,"ATTENTION IS A RESOURCE.",55,135,900,24,12,MeterView.Cyan);
            view.Label(lab,"DOPAMINE METER",52,173,950,62,44,MeterView.White,true);
            view.Label(lab,"A little convenience. A little less control.",55,237,850,30,18,MeterView.Muted);
            var body=MeterView.Panel(lab,"Instrument",55,303,940,421,MeterView.Hex("#0B121E"));
            view.Label(body,"01 / NEURAL TELEMETRY",24,18,420,25,12,MeterView.Muted);
            sensor=view.Art(body,view.sensorArt,20,52,346,346,Color.white);
            var dr=MeterView.Rect(body,"Live segmented gauge",15,47,356,356); dr.pivot=new Vector2(.5f,.5f); dr.anchoredPosition=new Vector2(193,-225);
            dial=dr.gameObject.AddComponent<MeterDial>(); dial.raycastTarget=false;
            view.Label(body,"CAPTURE INDEX",404,68,430,24,13,MeterView.Muted);
            number=view.Label(body,"50",395,103,300,117,96,MeterView.White,true);
            number.overflowMode=TextOverflowModes.Overflow;
            view.Label(body,"/ 100",700,167,135,35,25,MeterView.Muted);
            status=view.Label(body,"STANDBY",404,237,460,32,22,MeterView.Cyan,true);
            MeterView.Box(body,"Index track",404,292,490,5,MeterView.Hex("#253246"));
            bar=MeterView.Box(body,"Index fill",404,292,245,5,MeterView.Cyan);
            view.Label(body,"00          40       60       80      100",404,310,490,24,12,MeterView.Muted);
            message=view.Label(body,"Awaiting device activation",404,358,490,27,13,MeterView.Muted);
            var foot=MeterView.Panel(lab,"Session stream",55,746,940,111,MeterView.Hex("#0B121E"));
            view.Label(foot,"SESSION STREAM",20,15,170,24,11,MeterView.Cyan);
            history=view.Label(foot,"",197,15,718,80,12,MeterView.Muted); history.textWrappingMode=TextWrappingModes.Normal;
            var control=MeterView.Panel(lab,"Interaction panel",1028,196,517,661,MeterView.Hex("#101927"));
            view.Label(control,"02 / INTERACTION LAB",25,20,460,26,12,MeterView.Muted);
            view.Label(control,"Choose your next impulse.",25,58,465,38,23,MeterView.White);
            activate=view.Button(control,"PAIR COMPANION  /  ACTIVATE",25,116,467,52,()=>{model.Activate();AddLog("PAIR","Companion connected.");},true);
            actions.Add(view.Button(control,"Ask RAVEN for an insight                  +5",25,184,467,48,Query));
            actions.Add(view.Button(control,"Open a notification                           +3",25,240,467,48,()=>Apply(view.service.config.notificationCost,"NOTIFICATION","Notification opened")));
            actions.Add(view.Button(control,"Read a physical book                       -10",25,296,467,48,Read));
            actions.Add(view.Button(control,"Talk face to face                                -8",25,352,467,48,()=>Apply(-view.service.config.conversationRelief,"CONVERSATION","A moment of human connection")));
            feed=view.Button(control,"TIMED GROWTH / OFF",25,420,467,48,()=>{view.service.SetTimedGrowthEnabled(!view.service.enableTimedGrowth);Refresh();AddLog("TIMER",view.service.enableTimedGrowth ? "Enabled." : "Disabled.");});
            feedText=feed.GetComponentInChildren<TextMeshProUGUI>();
            var pause=view.Button(control,"Pause",25,485,145,42,()=>{paused=!paused;model.Suspend("demo",paused);}); pauseText=pause.GetComponentInChildren<TextMeshProUGUI>();
            view.Button(control,"Reset",186,485,145,42,ResetDemo);
            view.Button(control,"Field view",347,485,145,42,ToggleWorld);
            view.Button(control,"Switch scene",25,541,145,42,()=>StartCoroutine(SwitchScene()));
            var dialogueButton=view.Button(control,"Dialogue",186,541,145,42,null);
            dialogueButton.onClick.AddListener(()=>{dialogue=!dialogue;view.service.SetPlayerControl(!dialogue);
                dialogueButton.GetComponentInChildren<TextMeshProUGUI>().text=dialogue ? "End dialogue" : "Dialogue";
                Refresh();AddLog("CONTROL",dialogue ? "Dialogue: timer frozen; events still allowed." : "Player control resumed.");});
            view.Button(control,"Less motion",347,541,145,42,()=>{view.reduceMotion=!view.reduceMotion;AddLog("MOTION",view.reduceMotion ? "Reduced." : "Enabled.");});
            clue=view.Label(control,"Pair the device to begin.",25,608,467,34,12,MeterView.Muted);
            view.Label(lab,"RESEARCH PROTOTYPE  /  FICTIONAL INDEX, NOT A BIOLOGICAL MEASUREMENT",55,874,1100,20,10,MeterView.Muted);
            // Death overlay belongs only to the demo; hosts decide their own failure response.
            failure=MeterView.Rect(stage,"Death layer",0,0,1600,900);
            MeterView.Box(failure,"Input blocker",0,0,1600,900,new Color(.02f,.025f,.045f,1f),true);
            MeterView.Box(failure,"Signal",100,239,4,351,MeterView.Magenta);
            view.Label(failure,"SIGNAL LOST / 100",134,245,1000,38,20,MeterView.Magenta,true);
            view.Label(failure,"CONNECTION\nTERMINATED",126,311,1220,180,64,MeterView.White,true);
            life=view.Label(failure,"Neural overload. The player is dead.",134,516,1140,40,24,MeterView.Muted);
            view.Button(failure,"RESPAWN",134,605,350,58,()=>{view.service.Respawn();Refresh();AddLog("RESPAWN","Starting index "+model.Value);},true);
            view.Label(failure,"A fictional failure state. No progress in the main game has been changed.",134,703,1240,30,14,MeterView.Muted);
            failure.gameObject.SetActive(false);
        }
        void Query()
        {
            if(Apply(view.service.config.queryCost,"RAVEN","AI insight acquired")) clue.text="INSIGHT UNLOCKED / The archive entrance is on level 03.";
        }
        void Read()
        {
            if(Apply(-view.service.config.readingRelief,"READING","Reading complete")) clue.text="ARCHIVE NOTE / The city once had independent networks.";
        }
        bool Apply(float delta,string source,string cause)
        {
            bool ok=model.Apply(delta,Guid.NewGuid().ToString(),cause+" / "+delta.ToString("+0;-0;0"));
            if(ok) AddLog(source,delta.ToString("+0;-0;0")+"  /  index "+model.Value.ToString("0")); return ok;
        }
        void ResetDemo()
        {
            paused=false; model.Suspend("demo",false); view.service.NewGame();
            clue.text="Pair the device to begin."; AddLog("RESET","New session. Awaiting device.");
        }
        void ToggleWorld() { world=!world;lab.gameObject.SetActive(!world);worldControls.gameObject.SetActive(world); }
        void OnScene(Scene s,LoadSceneMode mode) { sceneName=s.name.EndsWith("B") ? "ANALOGUE DISTRICT / SECTOR 02" : "CITY / SECTOR 07"; if(chapter!=null)chapter.text=sceneName; }
        IEnumerator SwitchScene()
        {
            model.Suspend("transition",true);
            string target=SceneManager.GetActiveScene().name.EndsWith("B") ? "DopamineDemo" : "DopamineDemoB";
#if UNITY_EDITOR
            var op=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/DopamineMeter/Demo/"+target+".unity",new LoadSceneParameters(LoadSceneMode.Single));
#else
            var op=SceneManager.LoadSceneAsync(target);
#endif
            yield return op; model.Suspend("transition",false); AddLog("SCENE","State preserved across scene change.");
        }
        void AddLog(string source,string text)
        {
            log.Enqueue((++sequence).ToString("000")+"  "+source+"  /  "+text); while(log.Count>3)log.Dequeue();
            if(history!=null)history.text=string.Join("\n",log);
        }
        void Refresh()
        {
            if(status==null)return;
            status.text=MeterView.Status(model); status.color=bar.color=MeterView.Tint(model);
            message.text=model.LastCause; activate.interactable=model.Life==MeterLife.Dormant&&!model.Suspended;
            foreach(var b in actions)b.interactable=model.Life==MeterLife.Active&&!model.Suspended;
            feed.interactable=model.Life==MeterLife.Active&&!model.Suspended;
            feedText.text=!view.service.enableTimedGrowth ? "TIMED GROWTH / OFF - CLICK TO ENABLE" :
                !view.service.playerHasControl ? "TIMED GROWTH / PAUSED FOR DIALOGUE" :
                "TIMED GROWTH / +"+view.service.config.growthPerInterval.ToString("0.##")+" EVERY "+view.service.config.growthIntervalSeconds.ToString("0.##")+"s - ON";
            pauseText.text=paused ? "Resume" : "Pause";
            failure.gameObject.SetActive(model.Life==MeterLife.Dead);
            life.text="Player dead. Next starting index: "+model.RestartValue.ToString("0.##")+" / 100 (cap 50).";
        }
        void OnDeath() => AddLog("OVERLOAD","Player dead. Session locked.");
        void Update()
        {
            if(model==null||dial==null)return;
            shown=view.reduceMotion ? model.Value : Mathf.Lerp(shown,model.Value,1-Mathf.Exp(-8*Time.unscaledDeltaTime));
            number.text=model.Value.ToString("0.#"); dial.Set(shown,MeterView.Tint(model));
            bar.rectTransform.sizeDelta=new Vector2(490*shown/100,5);
            sensor.color=model.Life==MeterLife.Dormant ? new Color(.4f,.48f,.57f) : Color.white;
        }
        void OnDestroy()
        {
            if(model!=null){model.Changed-=Refresh;model.Failed-=OnDeath;}SceneManager.sceneLoaded-=OnScene;
        }
        IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(.4f); yield return new WaitForEndOfFrame();
            var tex=ScreenCapture.CaptureScreenshotAsTexture();
            string dir=Path.Combine(Application.dataPath,"../Captures");Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir,name+".png"),tex.EncodeToPNG());Destroy(tex);
        }
        IEnumerator Smoke()
        {
            // Runs in the real rendered player; exercises UI actions and scene survival.
            yield return Shot("01-standby"); model.Activate(); Query();Query();Query();
            yield return Shot("02-active");
            Read(); if(Mathf.Abs(model.Value-55)> .01f)throw new Exception("Demo reading regression");
            for(int i=0;i<5;i++)Query();
            yield return Shot("03-critical");
            saved=model.Capture(); yield return SwitchScene();
            if(model.Value!=80 || MeterService.Instance!=view.service)throw new Exception("Persistence regression");
            ToggleWorld();yield return Shot("04-field");ToggleWorld();
            for(int i=0;i<4;i++)Query();
            if(model.Life!=MeterLife.Dead)throw new Exception("Death regression");
            Read();if(model.Value!=100)throw new Exception("Dead recovery regression");
            yield return Shot("05-overload");
            model.Restore(saved);if(model.Value!=80||model.Life!=MeterLife.Active)throw new Exception("Restore regression");
            ResetDemo();if(model.Life!=MeterLife.Dormant||model.Value!=50)throw new Exception("Reset regression");
            model.Activate();view.service.SetTimedGrowthEnabled(true);view.service.SetPlayerControl(false);
            yield return new WaitForSecondsRealtime(.5f);
            if(model.ElapsedSeconds!=0||model.Value!=50)throw new Exception("Dialogue clock regression");
            Query();if(model.Value!=55)throw new Exception("Dialogue event regression");
            view.service.SetPlayerControl(true);
            yield return new WaitForSeconds(20.2f);
            if(model.Value!=55.5f)throw new Exception("Real 20-second interval regression: "+model.Value);
            view.service.SetTimedGrowthEnabled(false);
            yield return Shot("06-timed-growth");
            Directory.CreateDirectory(Path.Combine(Application.dataPath,"../Captures"));
            File.WriteAllText(Path.Combine(Application.dataPath,"../Captures/smoke-result.txt"),"PASS: activation, actions, recovery, thresholds, scene persistence, death lock, restore, reset, dialogue clock freeze, dialogue events and real 20-second growth.");
            Application.Quit();
        }
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Exploration.InteractionUI;

namespace CafeGameplay
{
    public sealed class CoffeeQTE : MonoBehaviour
    {
        public CoffeeRecipe recipe;
        public TMP_FontAsset font;
        public UnityEvent onStarted=new UnityEvent(),onFinished=new UnityEvent();
        public bool Running {get;private set;}
        public bool AwaitingServe {get;private set;}
        public CoffeeGrade Result {get;private set;}=CoffeeGrade.Cancelled;
        public CoffeeStepModel Step {get;private set;}
        public int StepIndex {get;private set;}
        public int PerfectSteps {get;private set;}
        RectTransform root,marker,goodZone,perfectZone,cupFill;
        TextMeshProUGUI title,instruction,feedback,stepLabel,actionLabel,score;
        Button serve;
        GameObject action,track;
        float feedbackTime;
        bool recorded;
        CoffeeGrade worst;
        string holdOwner;
        bool releasePending;
        PlayerMover lockedPlayer;
        void Start(){EnsureUI();}
        void EnsureUI()
        {
            if(root!=null)return;
            root=Canvas(transform,210);
            Box(root,"Backdrop",0,0,1600,900,new Color(.02f,.025f,.04f,.84f),true);
            var panel=Box(root,"Coffee workstation",170,155,1260,620,C("14202A")).rectTransform;
            Box(panel,"Amber edge",0,0,1260,3,C("E7B877"));
            Text(panel,font,"MANUAL BREW  /  STAY PRESENT",38,28,1100,28,16,C("E7B877"));
            title=Text(panel,font,"",38,78,1100,55,36,White);
            stepLabel=Text(panel,font,"",38,145,1100,30,17,Muted);
            var artFrame=Rect(panel,"Brew illustration",38,205,320,270);
            if(recipe!=null&&recipe.illustration!=null)Art(artFrame,recipe.illustration,0,0,320,270);
            Text(panel,font,"BREW PROGRESS",38,450,320,28,15,C("E7B877"));
            Box(panel,"Progress track",38,484,320,3,C("263E4B"));
            cupFill=Box(panel,"Progress fill",38,484,0,3,C("E7B877")).rectTransform;
            instruction=Text(panel,font,"",405,210,800,80,23,White);
            track=Box(panel,"Timing track",405,340,790,26,C("263E4B")).gameObject;
            goodZone=Box(track.transform,"Good range",0,0,100,26,C("446B66")).rectTransform;
            perfectZone=Box(track.transform,"Perfect range",0,0,50,26,Cyan).rectTransform;
            marker=Box(track.transform,"Marker",0,-7,4,40,White).rectTransform;
            action=Box(panel,"Brew input",405,400,790,66,C("31515B"),true).gameObject;
            action.AddComponent<CoffeePressControl>().owner=this;
            actionLabel=Text(action.transform,font,"SPACE / CLICK",24,15,740,40,22,White);
            feedback=Text(panel,font,"",405,495,780,38,26,Cyan);
            score=Text(panel,font,"",38,552,780,32,18,Muted);
            serve=Button(panel,font,"SERVE COFFEE",925,545,290,48,Serve,true);
            Button(panel,font,"CANCEL",38,495,210,40,Cancel);
            root.gameObject.SetActive(false);
        }
        public bool Begin()
        {
            if(Running||CafeRuntime.Paused)return false;
            if(recipe==null||!recipe.IsValid){Debug.LogError("Assign a valid CoffeeRecipe",this);return false;}
            EnsureUI();Running=true;AwaitingServe=false;Result=CoffeeGrade.Cancelled;worst=CoffeeGrade.Perfect;PerfectSteps=0;StepIndex=0;
            lockedPlayer=PlayerMover.Instance;lockedPlayer?.Lock("cafe-coffee");
            root.gameObject.SetActive(true);StartStep();onStarted.Invoke();return true;
        }
        void StartStep()
        {
            Step=new CoffeeStepModel(recipe.steps[StepIndex]);recorded=false;feedbackTime=0;holdOwner=null;releasePending=false;
            title.text=Step.Settings.title+" / "+recipe.drinkName;instruction.text=Step.Settings.instruction;stepLabel.text="STEP "+(StepIndex+1)+" / "+recipe.steps.Length;
            feedback.text="";action.SetActive(true);track.SetActive(true);serve.gameObject.SetActive(false);
            float target=Step.Settings.input==CoffeeInput.Rhythm?.5f:Step.Settings.target;
            float perfect=Step.Settings.input==CoffeeInput.Rhythm?Step.Settings.perfectWindow/(2*Step.Settings.beatInterval):Step.Settings.perfectWindow;
            float good=Step.Settings.input==CoffeeInput.Rhythm?Step.Settings.goodWindow/(2*Step.Settings.beatInterval):Step.Settings.goodWindow;
            Zone(goodZone,target,good);Zone(perfectZone,target,perfect);
            score.text="PERFECT STEPS / "+PerfectSteps+"   |   Bright zone: perfect. Outer zone: good.";
        }
        static void Zone(RectTransform rect,float center,float radius){float left=Mathf.Clamp01(center-radius),right=Mathf.Clamp01(center+radius);rect.anchoredPosition=new Vector2(left*790,0);rect.sizeDelta=new Vector2((right-left)*790,26);}
        void Update()
        {
            if(!Running||CafeRuntime.Paused)return;
            if(releasePending){releasePending=false;Step.Release();}
            if(Keyboard.current!=null){if(Keyboard.current.spaceKey.wasPressedThisFrame)Press("keyboard");if(Keyboard.current.spaceKey.wasReleasedThisFrame)Release("keyboard");}
            if(AwaitingServe)return;
            Step.Tick(Time.deltaTime);
            float position=Step.Settings.input==CoffeeInput.Rhythm?Mathf.Clamp01(.5f+(Step.Elapsed-Step.NextBeatTime)/(2*Step.Settings.beatInterval)):Step.Position;
            marker.anchoredPosition=new Vector2(position*786,7);
            actionLabel.text=Step.Settings.input==CoffeeInput.Hold?(Step.Holding?"RELEASE IN THE BRIGHT ZONE":"HOLD SPACE / HOLD BUTTON"):Step.Settings.input==CoffeeInput.Rhythm?"TAP / BEAT "+Mathf.Min(Step.BeatIndex+1,Step.Settings.beats)+" OF "+Step.Settings.beats:"SPACE / TAP TO STOP";
            float fill=320*(StepIndex+(Step.Done?1:position))/recipe.steps.Length;cupFill.sizeDelta=new Vector2(fill,3);
            if(!Step.Done)return;
            if(!recorded){recorded=true;holdOwner=null;if(Step.Grade==CoffeeGrade.Perfect)PerfectSteps++;if(Step.Grade<worst)worst=Step.Grade;feedback.text=Step.Grade.ToString().ToUpperInvariant();feedback.color=Step.Grade==CoffeeGrade.Miss?Pink:Cyan;}
            feedbackTime+=Time.deltaTime;
            if(feedbackTime<recipe.stepFeedbackSeconds)return;
            if(++StepIndex<recipe.steps.Length){StartStep();return;}
            Result=worst;AwaitingServe=true;action.SetActive(false);track.SetActive(false);serve.gameObject.SetActive(true);
            title.text=Result==CoffeeGrade.Perfect?"A PERFECT CUP":"YOUR CUP IS READY";instruction.text="Your result will be returned to the conversation.\nPerfect brewing can unlock a deeper story.";
            score.text=PerfectSteps+" / "+recipe.steps.Length+" PERFECT STEPS";
        }
        public void Press(string source="external")
        {
            if(!Running||AwaitingServe||CafeRuntime.Paused||Step.Done||holdOwner!=null)return;
            if(Step.Settings.input==CoffeeInput.Hold)holdOwner=source;
            Step.Press();
        }
        public void Release(string source="external")
        {
            if(!Running||AwaitingServe||Step==null||holdOwner!=source)return;
            holdOwner=null;if(CafeRuntime.Paused){releasePending=true;return;}Step.Release();
        }
        public void Serve(){if(Running&&AwaitingServe&&!CafeRuntime.Paused)Finish();}
        public void Cancel(){if(!Running)return;Result=CoffeeGrade.Cancelled;Finish();}
        void Finish(){Running=false;AwaitingServe=false;holdOwner=null;if(root!=null)root.gameObject.SetActive(false);if(lockedPlayer!=null)lockedPlayer.Unlock("cafe-coffee");lockedPlayer=null;onFinished.Invoke();}
        void OnDisable()=>Cancel();
        void OnApplicationFocus(bool focused){if(!focused&&!CafeRuntime.AutomationIgnoresFocus&&Running&&Step!=null&&Step.Holding){holdOwner=null;releasePending=true;}}
    }
}

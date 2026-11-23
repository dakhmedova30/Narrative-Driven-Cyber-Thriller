using System;
using System.Collections;
using System.IO;
using DopamineMeter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using static Exploration.InteractionUI;

namespace Exploration
{
    public sealed class InteractionDemo : MonoBehaviour
    {
        public InteractionSystem system;
        public Texture2D roomArt, hallwayArt;
        public Interactable[] steps;
        public TMP_FontAsset font, headingFont;
        TextMeshProUGUI objective, count, record;
        TextMeshProUGUI[] rows;
        Image progress, fade;
        RawImage backdrop;
        RectTransform endCard;
        bool ending;
        string[] instructions={"Read the terminal message.","Find the handwritten note.","Open the delivery package.","Leave through the unlocked door.","A new lead. A different world."};
        public bool Finished { get; private set; }
        IEnumerator Start()
        {
            var root=Canvas(transform,10);
            Box(root,"Background",0,0,1600,900,Ink);
            Text(root,font,"CHAPTER 00  /  INTERACTIVE PREVIEW",40,28,850,28,16,Cyan);
            Text(root,headingFont,"THE DELIVERY",40,65,1000,64,40,White);
            Text(root,font,"A message. A trace. Something left at your door.",42,132,1080,30,21,Muted);
            var frame=Box(root,"Room frame",40,180,1120,630,C("27384B")).rectTransform;
            backdrop=Art(frame,roomArt,0,0,1120,630);
            Box(frame,"Night tint",0,0,1120,630,new Color(.02f,.08f,.16f,.27f));
            Box(root,"Rail",1190,200,370,610,Panel);
            Text(root,font,"CURRENT OBJECTIVE",1214,224,318,28,15,Cyan);
            objective=Text(root,font,instructions[0],1214,270,318,85,26,White);
            count=Text(root,font,"00 / 04",1214,365,300,30,17,Muted);
            Box(root,"Progress track",1214,410,318,3,C("304354"));
            progress=Box(root,"Progress",1214,410,0,3,Cyan);
            rows=new TextMeshProUGUI[4];
            for(int i=0;i<4;i++) rows[i]=Text(root,font,"",1214,440+i*44,318,35,19,Muted);
            Text(root,font,"COLLECTED / SESSION",1214,642,318,24,14,Cyan);
            record=Text(root,font,"No discoveries yet.",1214,680,318,110,18,Muted);
            Text(root,font,"CLICK an object  /  E inspect nearest to cursor  /  BACKSPACE close",42,855,1100,26,16,Muted);
            Button(root,font,"REPLAY",1400,840,160,40,Replay);
            var overlay=Canvas(transform,250);
            fade=Box(overlay,"Transition",0,0,1600,900,new Color(0,0,0,0));
            endCard=Box(overlay,"End card",350,260,900,380,Panel).rectTransform;
            Box(endCard,"Line",0,0,900,3,Cyan);
            Text(endCard,font,"EXPLORATION COMPLETE",40,35,820,30,16,Cyan);
            Text(endCard,headingFont,"FOLLOW THE SIGNAL",40,93,820,72,32,White);
            Text(endCard,font,"Message decoded. Note recorded. Device collected.\nThe corridor is waiting.\n\nEnd of this interaction preview.",40,178,820,130,22,Muted);
            Button(endCard,font,"PLAY AGAIN",610,310,250,48,Replay,true);endCard.gameObject.SetActive(false);
            system.Completed+=OnCompleted;
            yield return null;
            MeterService.Instance?.Activate();Refresh();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-interaction-smoke")>=0) StartCoroutine(Smoke());
        }
        void Refresh()
        {
            int n=0;foreach(var item in steps)if(item.Completed)n++;
            objective.text=instructions[n]; count.text=n.ToString("00")+" / 04";
            progress.rectTransform.sizeDelta=new Vector2(318*n/4f,3);
            for(int i=0;i<4;i++){rows[i].text=(steps[i].Completed?"DONE  ":steps[i].Available?"NEXT  ":" - -   ")+steps[i].displayName;rows[i].color=steps[i].Completed?Cyan:steps[i].Available?White:Muted;}
            record.text=n==0?"No discoveries yet.":n==1?"Terminal message":n==2?"Terminal message\nHandwritten note":"Terminal message\nHandwritten note\nCrow device";
        }
        void OnCompleted(Interactable item)
        {
            Refresh();
            if(item==steps[3]&&!ending)StartCoroutine(EndSequence());
        }
        IEnumerator EndSequence()
        {
            ending=true;
            system.enabled=false;
            system.Root.gameObject.SetActive(false);
            for(float t=0;t<1;t+=Time.unscaledDeltaTime){fade.color=new Color(.01f,.02f,.04f,t);yield return null;}
            backdrop.texture=hallwayArt;
            var fit=backdrop.GetComponent<AspectRatioFitter>();fit.aspectRatio=(float)hallwayArt.width/hallwayArt.height;
            for(float t=1;t>0;t-=Time.unscaledDeltaTime){fade.color=new Color(.01f,.02f,.04f,t*.65f);yield return null;}
            fade.color=new Color(.01f,.02f,.04f,.4f);endCard.gameObject.SetActive(true);Finished=true;
        }
        public void Replay()
        {
            if(ending&&!Finished)return;
            StopAllCoroutines(); system.Close(); foreach(var step in steps){step.gameObject.SetActive(true);step.ResetInteraction();}
            backdrop.texture=roomArt;backdrop.GetComponent<AspectRatioFitter>().aspectRatio=(float)roomArt.width/roomArt.height;
            endCard.gameObject.SetActive(false);fade.color=Color.clear;system.enabled=true;system.Root.gameObject.SetActive(true);
            ending=Finished=false;MeterService.Instance?.NewGame();MeterService.Instance?.Activate();Refresh();
        }
        void OnDestroy(){if(system!=null)system.Completed-=OnCompleted;}
        IEnumerator Smoke()
        {
            var dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Captures");Directory.CreateDirectory(dir);
            yield return new WaitForSecondsRealtime(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"01-room.png"));
            yield return new WaitForEndOfFrame();
            string failure=null;
            Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);};
            var testPlayer=new GameObject("Smoke test player").AddComponent<PlayerMover>();
            try
            {
                steps[0].interactionDistance=1;testPlayer.transform.position=new Vector3(100,0,0);
                check(!system.Inspect(steps[0]),"distance blocks remote interaction");steps[0].interactionDistance=0;
                GameSession.Instance.SetPaused(true);check(!system.Inspect(steps[0]),"menu blocks interaction");GameSession.Instance.SetPaused(false);
                check(!system.Inspect(steps[3]),"locked exit");
                check(system.Inspect(steps[0]),"open first object");system.Close();check(!steps[0].Completed,"cancel must not collect");
                check(testPlayer.CanMove,"close unlocks player");
                check(system.Inspect(steps[0]),"reopen");
                check(!testPlayer.CanMove,"inspect locks player");
                check(MeterService.Instance.Model.Suspended,"inspect pauses meter");
            }catch(Exception e){failure=e.ToString();}
            yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"02-inspect.png"));
            yield return new WaitForEndOfFrame();
            if(failure==null)try
            {
                system.Confirm();check(steps[0].Completed,"confirm completes");check(MeterService.Instance.Model.Value==55,"terminal +5");
                check(!steps[0].Complete(),"one-shot prevents duplicate reward");
                check(system.Inspect(steps[1]),"note unlocked");
            }catch(Exception e){failure=e.ToString();}
            yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(dir,"02b-note.png"));yield return new WaitForEndOfFrame();
            if(failure==null && Keyboard.current!=null)
            {
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Backspace));
                yield return null;yield return null;
                if(system.IsInspecting)failure="Backspace must close inspection";
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.E));
                yield return null;yield return null;
                if(system.Selected!=steps[1])failure="E must inspect the available object";
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
            }
            if(failure==null)try
            {
                system.Confirm();check(MeterService.Instance.Model.Value==45,"note -10");
                check(system.Inspect(steps[2]),"package unlocked");system.Confirm();check(MeterService.Instance.Model.Value==53,"package +8");
                check(steps[3].Available,"exit unlocked");
            }catch(Exception e){failure=e.ToString();}
            yield return new WaitForSecondsRealtime(.4f);ScreenCapture.CaptureScreenshot(Path.Combine(dir,"03-collected.png"));yield return new WaitForEndOfFrame();
            if(failure==null)try{check(system.Inspect(steps[3]),"open exit");system.Confirm();}catch(Exception e){failure=e.ToString();}
            yield return new WaitForSecondsRealtime(2.5f);
            if(failure==null&&!Finished)failure="End transition did not complete";
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"04-ending.png"));yield return new WaitForEndOfFrame();
            Destroy(testPlayer.gameObject);
            File.WriteAllText(Path.Combine(dir,"smoke-result.txt"),failure??"PASS: distance gate, pause-menu gate, locked prerequisite, cancel/reopen, player movement lock/release, inspection pause, keyboard E/Backspace input, one-shot, Meter 50 -> 55 -> 45 -> 53, four-step flow, corridor transition.");
            Application.Quit(failure==null?0:1);
        }
    }
}

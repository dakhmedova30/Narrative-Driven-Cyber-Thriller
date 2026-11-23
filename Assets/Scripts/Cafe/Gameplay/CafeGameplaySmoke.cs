using System;
using System.Collections;
using System.IO;
using DopamineMeter;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace CafeGameplay
{
    // Opt-in standalone verification, never runs during normal Play.
    public sealed class CafeGameplaySmoke : MonoBehaviour
    {
        public CoffeeQTE coffee;public CafePhone phone;public CafeDialoguePresenter presenter;public DialogueRunner runner;
        string error,dir;
        void Check(bool ok,string message){if(!ok&&error==null)error=message;}
        IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-cafe-smoke")<0)yield break;
            Application.runInBackground=true;CafeRuntime.AutomationIgnoresFocus=true;
            dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Captures");Directory.CreateDirectory(dir);
            yield return new WaitForSecondsRealtime(2);
            Check(!phone.enableNotifications&&phone.Inbox.Unread==0,"phone starts quiet");
            Check(phone.Open(),"quiet phone can open");yield return new WaitForSecondsRealtime(.7f);
            Check(MeterService.Instance.Model.Value==50,"quiet phone has no Meter cost");phone.Close();yield return new WaitForSecondsRealtime(.7f);
            float deadline=Time.realtimeSinceStartup+120,nextAdvance=0;
            int served=0;bool checkedPause=false,perfectStory=false,phoneTest=false,brewing=false;
            while(Time.realtimeSinceStartup<deadline&&error==null)
            {
                if(presenter.CurrentText.Contains("My sister disappeared"))perfectStory=true;
                if(coffee.Running)
                {
                    if(!brewing){brewing=true;Check(!phone.Open(),"phone cannot interrupt QTE");}
                    if(!checkedPause)
                    {
                        checkedPause=true;float before=coffee.Step.Elapsed;
                        GameSession.Instance.SetPaused(true);yield return new WaitForSecondsRealtime(.25f);
                        Check(coffee.Step.Elapsed==before,"QTE pauses with menu");GameSession.Instance.SetPaused(false);
                        yield return new WaitForSecondsRealtime(.15f);ScreenCapture.CaptureScreenshot(Path.Combine(dir,"01-brewing.png"));
                    }
                    if(coffee.AwaitingServe)
                    {
                        Check(coffee.Result==(served==0?CoffeeGrade.Miss:CoffeeGrade.Perfect),"expected miss then perfect results");
                        if(served==1){ScreenCapture.CaptureScreenshot(Path.Combine(dir,"02-perfect.png"));yield return new WaitForEndOfFrame();}
                        coffee.Serve();served++;brewing=false;
                        Debug.Log("[Cafe smoke] Served cup "+served);
                    }
                    else
                    {
                        var model=coffee.Step;var s=model.Settings;
                        if(!model.Done)
                        {
                            if(s.input==CoffeeInput.Timing){if(served==0&&coffee.StepIndex==0)coffee.Press();else if(model.Position>=s.target)coffee.Press();}
                            if(s.input==CoffeeInput.Hold){if(!model.Holding)coffee.Press();if(model.Position>=s.target)coffee.Release();}
                            if(s.input==CoffeeInput.Rhythm&&model.Elapsed>=model.NextBeatTime)coffee.Press();
                        }
                    }
                }
                else if(presenter.Choosing)
                {
                    if(served==1)presenter.Choose(0); // Retry the failed cup.
                    else presenter.Choose(phoneTest?0:1); // Enable notifications, then choose to put the phone away.
                }
                else if(presenter.WaitingForLine&&Time.realtimeSinceStartup>=nextAdvance)
                {
                    presenter.Advance();nextAdvance=Time.realtimeSinceStartup+.25f;
                }
                if(phone.enableNotifications&&!phoneTest&&phone.Inbox.delivered.Count>=4&&!presenter.Choosing&&!coffee.Running)
                {
                    Debug.Log("[Cafe smoke] Phone phase");
                    Check(phone.Open(),"open notified phone");yield return new WaitForSecondsRealtime(.75f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(dir,"03-phone-feed.png"));yield return new WaitForEndOfFrame();
                    var scroll=phone.GetComponentInChildren<ScrollRect>();scroll.verticalNormalizedPosition=0;yield return null;
                    ScreenCapture.CaptureScreenshot(Path.Combine(dir,"04-phone-scroll.png"));yield return new WaitForEndOfFrame();
                    var post=phone.Inbox.delivered[0];float before=MeterService.Instance.Model.Value;
                    // The automation can run behind the user's editor; normal gameplay still pauses on focus loss.
                    MeterService.Instance.Model.Suspend("focus",false);
                    Check(phone.ReadPost(post),"read new message");Check(MeterService.Instance.Model.Value==before+post.meterIncrease,"reading increases global Meter");
                    Check(phone.ReadPost(post)&&MeterService.Instance.Model.Value==before+post.meterIncrease,"rereading does not repeat cost");
                    ScreenCapture.CaptureScreenshot(Path.Combine(dir,"05-reading.png"));yield return new WaitForEndOfFrame();phone.Close();phoneTest=true;
                }
                if(served==2&&phoneTest&&!runner.IsDialogueRunning)break;
                yield return null;
            }
            Check(served==2&&perfectStory&&phoneTest&&!runner.IsDialogueRunning,"real Yarn flow must finish, including retry and perfect-only story");
            Check(!phone.enableNotifications,"story can turn notifications off");
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"06-returned.png"));yield return new WaitForEndOfFrame();
            File.WriteAllText(Path.Combine(dir,"result.txt"),error??"PASS: real Yarn dialogue, failed cup/retry, perfect three-step QTE, perfect-only story, pause freeze, phone blocked while brewing, quiet phone, notifications, scrollable feed, single Meter charge, return to dialogue and notifications disabled.");
            Debug.Log("[Cafe smoke] "+(error??"PASS"));
            Application.Quit(error==null?0:1);
        }
    }
}

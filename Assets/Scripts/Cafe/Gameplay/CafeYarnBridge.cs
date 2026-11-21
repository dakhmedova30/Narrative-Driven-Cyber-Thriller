using System.Collections;
using UnityEngine;
using Yarn.Unity;
namespace CafeGameplay
{
    [RequireComponent(typeof(DialogueRunner))]
    public sealed class CafeYarnBridge : MonoBehaviour
    {
        public CoffeeQTE coffee;
        public CafePhone phone;
        public CafeStage stage;
        DialogueRunner runner;
        int phoneViewsAtPhase;
        void Awake()
        {
            runner=GetComponent<DialogueRunner>();
            runner.AddCommandHandler("cafe_arrive",Arrive);
            runner.AddCommandHandler("cafe_brew",Brew);
            runner.AddCommandHandler<bool>("cafe_notifications",Notifications);
            runner.AddCommandHandler("cafe_wait_phone",WaitForPhone);
        }
        IEnumerator Arrive(){if(stage!=null)yield return stage.Arrive();}
        IEnumerator Brew()
        {
            if(phone!=null){phone.Close();while(phone.Transitioning)yield return null;}
            runner.VariableStorage.SetValue("$coffee_perfect",false);runner.VariableStorage.SetValue("$coffee_result","Cancelled");runner.VariableStorage.SetValue("$coffee_perfect_steps",0f);
            if(coffee==null||!coffee.Begin())yield break;
            while(coffee.Running && runner!=null && runner.IsDialogueRunning)yield return null;
            if(runner==null||!runner.IsDialogueRunning){coffee.Cancel();yield break;}
            runner.VariableStorage.SetValue("$coffee_perfect",coffee.Result==CoffeeGrade.Perfect);
            runner.VariableStorage.SetValue("$coffee_result",coffee.Result.ToString());
            runner.VariableStorage.SetValue("$coffee_perfect_steps",(float)coffee.PerfectSteps);
        }
        void Notifications(bool enabled){if(phone!=null){if(enabled&&!phone.enableNotifications)phoneViewsAtPhase=phone.ViewedCount;phone.SetNotifications(enabled);}}
        IEnumerator WaitForPhone()
        {
            if(phone==null||phone.Inbox==null||!phone.enableNotifications)yield break;
            while(runner!=null&&runner.IsDialogueRunning&&(phone.ViewedCount<=phoneViewsAtPhase||phone.IsOpen||phone.Transitioning))
            {
                if(!phone.enableNotifications)yield break;
                if(phone.ViewedCount<=phoneViewsAtPhase&&phone.Inbox.Unread==0&&phone.Inbox.delivered.Count>=phone.content.posts.Length)
                {Debug.LogWarning("Phone phase has no unread or remaining content; continuing dialogue.",this);yield break;}
                yield return null;
            }
        }
        void OnDisable(){if(coffee!=null)coffee.Cancel();if(phone!=null)phone.Close();}
        void OnDestroy()
        {
            if(runner==null)return;
            foreach(var name in new[]{"cafe_arrive","cafe_brew","cafe_notifications","cafe_wait_phone"})runner.RemoveCommandHandler(name);
        }
    }
}

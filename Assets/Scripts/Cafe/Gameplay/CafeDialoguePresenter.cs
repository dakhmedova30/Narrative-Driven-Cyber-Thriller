#nullable enable
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Yarn.Unity;
using static Exploration.InteractionUI;

namespace CafeGameplay
{
    public sealed class CafeDialoguePresenter : DialoguePresenterBase
    {
        public TMP_FontAsset font=null!;
        public CoffeeQTE coffee=null!;
        public CafePhone phone=null!;
        [Min(1)] public float charactersPerSecond=48;
        public bool Choosing {get;private set;}
        public bool WaitingForLine {get;private set;}
        public string CurrentText {get;private set;}="";
        public RectTransform Panel {get;private set;}=null!;
        TextMeshProUGUI speaker=null!,body=null!;
        Button advance=null!;
        readonly List<Button> choices=new List<Button>();
        bool requested;
        DialogueOption? selected;
        public bool InputAllowed => !CafeRuntime.Paused && (coffee==null||!coffee.Running) && (phone==null||(!phone.IsOpen&&!phone.Transitioning));
        void Awake()
        {
            var root=Canvas(transform,160);
            Panel=Box(root,"Cafe conversation",42,713,1516,150,new Color(.025f,.035f,.05f,.96f),true).rectTransform;
            Box(Panel,"Accent",0,0,3,150,C("E7B877"));
            speaker=Text(Panel,font,"",26,15,1200,28,17,C("E7B877"));
            body=Text(Panel,font,"",26,54,1130,85,24,White);
            advance=Button(Panel,font,"CONTINUE",1230,75,250,48,Advance,true);
            Panel.gameObject.SetActive(false);
        }
        void Update()
        {
            if(Panel==null)return;
            bool covered=(coffee!=null&&coffee.Running)||(phone!=null&&(phone.IsOpen||phone.Transitioning));
            Panel.gameObject.SetActive((WaitingForLine||Choosing||finished)&&!covered);
            if(advance!=null)advance.interactable=InputAllowed;
            foreach(var choice in choices)if(choice!=null)choice.interactable=InputAllowed;
        }
        bool finished;
        public void Advance(){if(InputAllowed){if(finished){SceneManager.LoadScene(SceneManager.GetActiveScene().name);return;}requested=true;}}
        public void Choose(int index){if(InputAllowed&&Choosing&&index>=0&&index<available.Count)selected=available[index];}
        readonly List<DialogueOption> available=new List<DialogueOption>();
        public override YarnTask OnDialogueStartedAsync(){finished=false;return YarnTask.CompletedTask;}
        public override YarnTask OnDialogueCompleteAsync()
        {
            WaitingForLine=Choosing=false;finished=true;speaker.text="END OF PREVIEW";body.text="A cup of coffee. A conversation. Your attention is still yours.";body.maxVisibleCharacters=int.MaxValue;
            foreach(var b in choices)Destroy(b.gameObject);choices.Clear();advance.gameObject.SetActive(true);advance.GetComponentInChildren<TextMeshProUGUI>().text="REPLAY";
            return YarnTask.CompletedTask;
        }
        public override async YarnTask RunLineAsync(LocalizedLine line,LineCancellationToken token)
        {
            Panel.anchoredPosition=new Vector2(42,-713);Panel.sizeDelta=new Vector2(1516,150);
            WaitingForLine=true;requested=false;CurrentText=line.TextWithoutCharacterName.Text;speaker.text=line.CharacterName;body.text=CurrentText;body.maxVisibleCharacters=0;
            advance.gameObject.SetActive(true);advance.GetComponentInChildren<TextMeshProUGUI>().text="CONTINUE";
            float visible=0;
            while(!token.NextContentToken.IsCancellationRequested && this!=null && isActiveAndEnabled)
            {
                if(InputAllowed)
                {
                    visible+=Time.deltaTime*charactersPerSecond;
                    if(requested){requested=false;if(visible<CurrentText.Length)visible=CurrentText.Length;else break;}
                    body.maxVisibleCharacters=Mathf.FloorToInt(visible);
                }
                await YarnTask.Yield();
            }
            WaitingForLine=false;
        }
        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options,LineCancellationToken token)
        {
            Choosing=true;selected=null;available.Clear();body.text="";speaker.text="YOUR RESPONSE";advance.gameObject.SetActive(false);
            int optionCount=0;foreach(var option in options)if(option.IsAvailable)optionCount++;
            float height=60+optionCount*58;Panel.sizeDelta=new Vector2(1516,height);Panel.anchoredPosition=new Vector2(42,-(863-height));
            foreach(var option in options)
            {
                if(!option.IsAvailable)continue;
                int i=available.Count;available.Add(option);
                // Demo uses two choices; wrap additional choices upward without covering the phone controls.
                var b=Button(Panel,font,option.Line.TextWithoutCharacterName.Text,26,50+i*58,1450,48,()=>Choose(i));choices.Add(b);
            }
            while(selected==null&&!token.NextContentToken.IsCancellationRequested&&this!=null&&isActiveAndEnabled)await YarnTask.Yield();
            var result=selected;Choosing=false;foreach(var b in choices)Destroy(b.gameObject);choices.Clear();available.Clear();return result;
        }
    }
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Exploration.InteractionUI;
namespace CafeGameplay
{
    public sealed class CafeStage : MonoBehaviour
    {
        public Texture2D background,visitor;
        public TMP_FontAsset font;
        [Min(.1f)] public float arrivalSeconds=1.6f;
        public bool reduceMotion;
        RectTransform room,portrait;
        CanvasGroup visitorAlpha;
        float focus;
        void Awake()
        {
            var root=Canvas(transform,50);room=Rect(root,"Room",0,0,1600,900);
            var bg=Rect(room,"Background",0,0,1600,900).gameObject.AddComponent<RawImage>();bg.texture=background;bg.raycastTarget=false;
            portrait=Rect(room,"Visitor",580,217,440,550);Art(portrait,visitor,0,0,440,550);visitorAlpha=portrait.gameObject.AddComponent<CanvasGroup>();visitorAlpha.alpha=0;
            var counter=Rect(room,"Counter foreground",0,697,1600,203).gameObject.AddComponent<RawImage>();counter.texture=background;counter.uvRect=new Rect(0,0,1,203f/900);counter.raycastTarget=false;
            Text(root,font,"AFTER HOURS",36,30,500,38,25,C("F4D5A2"));Text(root,font,"Listen. Brew. Choose where your attention goes.",36,72,800,30,17,White);
        }
        public IEnumerator Arrive()
        {
            for(float t=0;t<1;)
            {
                if(!CafeRuntime.Paused)t+=Time.deltaTime/Mathf.Max(.1f,arrivalSeconds);
                float v=reduceMotion?1:Mathf.SmoothStep(0,1,t);visitorAlpha.alpha=v;portrait.anchoredPosition=new Vector2(Mathf.Lerp(1000,580,v),-217);yield return null;
            }
            visitorAlpha.alpha=1;portrait.anchoredPosition=new Vector2(580,-217);
        }
        public void SetPhoneFocus(float value){focus=value;}
        void LateUpdate()
        {
            if(room==null)return;
            // The camera is 2D: zoom around the top-left and translate upward to reveal the counter.
            room.localScale=Vector3.one*(1+(reduceMotion?0:.07f)*focus);
            room.anchoredPosition=new Vector2(-45*focus,80*focus);
        }
    }
}

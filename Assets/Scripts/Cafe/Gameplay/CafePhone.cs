using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DopamineMeter;
using static Exploration.InteractionUI;

namespace CafeGameplay
{
    public sealed class CafePhone : MonoBehaviour
    {
        public PhoneContent content;
        public TMP_FontAsset font;
        [Header("Notification controls")]
        public bool enableNotifications;
        public bool playNotificationSound=true;
        public bool autoOpenPhone;
        [Range(0,1)] public float soundVolume=.4f;
        [Header("Optional scene connections")]
        public CoffeeQTE coffee;
        public CafeDialoguePresenter dialogue;
        public CafeStage stage;
        public UnityEvent onOpened=new UnityEvent(),onClosed=new UnityEvent();
        public bool IsOpen {get;private set;}
        public bool Transitioning => transition>0&&transition<1;
        public PhoneInbox Inbox {get;private set;}
        public int ViewedCount {get;private set;}
        RectTransform root,panel,feed,detail,contentRoot;
        TextMeshProUGUI badge,status,empty,detailTitle,detailBody,detailSource;
        GameObject notification;
        AudioSource audioSource;
        ScrollRect scroll;
        Button desk;
        float transition;
        bool pendingAutoOpen;
        int rendered=-1;
        PlayerMover lockedPlayer;
        string sessionId;
        public bool CanOpen => isActiveAndEnabled&&!CafeRuntime.Paused&&(coffee==null||!coffee.Running)&&(dialogue==null||!dialogue.Choosing);
        void Start()=>EnsureUI();
        void EnsureUI()
        {
            if(root!=null)return;
            if(content==null||!content.IsValid){Debug.LogError("Assign valid PhoneContent with unique post IDs",this);enabled=false;return;}
            sessionId=Guid.NewGuid().ToString("N");Inbox=new PhoneInbox(content);
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;
            root=Canvas(transform,230);
            desk=Button(root,font,"PHONE",1270,626,155,76,()=>Open());desk.image.color=C("111E27");
            Text(desk.transform,font,"tap to look down",10,46,140,20,11,Muted);
            notification=Box(root,"Notification",1050,532,375,70,C("173C45"),true).gameObject;
            var nb=notification.AddComponent<Button>();nb.onClick.AddListener(()=>Open());
            badge=Text(notification.transform,font,"!",18,14,340,45,20,Cyan);
            panel=Box(root,"Phone body",880,185,480,690,C("080E17"),true).rectTransform;
            Box(panel,"Phone edge",0,0,480,3,Cyan);
            Text(panel,font,"21:47  /  NIGHT MODE",24,18,300,30,14,Muted);
            Button(panel,font,"CLOSE",355,12,110,36,Close);
            Text(panel,font,"Your attention.",24,70,430,50,31,White);
            status=Text(panel,font,"",24,123,430,52,16,Cyan);
            feed=Rect(panel,"Feed",20,185,440,475);
            var viewport=Box(feed,"Viewport",0,0,440,475,Color.clear,true).rectTransform;
            viewport.gameObject.AddComponent<RectMask2D>();
            contentRoot=Rect(viewport,"Scrollable posts",0,0,440,475);
            scroll=feed.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=contentRoot;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;
            empty=Text(contentRoot,font,"No notifications.\n\nThe phone can wait.\nThere is someone in front of you.",18,40,400,250,24,Muted);
            detail=Box(panel,"Post detail",20,180,440,480,C("142432"),true).rectTransform;
            detailSource=Text(detail,font,"",22,25,396,30,15,Cyan);
            detailTitle=Text(detail,font,"",22,76,396,90,28,White);
            detailBody=Text(detail,font,"",22,180,396,210,20,White);
            Button(detail,font,"BACK TO FEED",22,415,396,44,()=>detail.gameObject.SetActive(false));
            detail.gameObject.SetActive(false);panel.gameObject.SetActive(false);notification.SetActive(false);
        }
        void Update()
        {
            if(root==null)return;
            desk.gameObject.SetActive((coffee==null||!coffee.Running)&&!IsOpen&&!Transitioning);
            if(!CafeRuntime.Paused)
            {
                var post=Inbox.Tick(Time.deltaTime,enableNotifications);
                if(post!=null)
                {
                    pendingAutoOpen=autoOpenPhone;
                    if(playNotificationSound&&content.notificationSound!=null)audioSource.PlayOneShot(content.notificationSound,soundVolume);
                }
                if(!enableNotifications)pendingAutoOpen=false;
                if(pendingAutoOpen&&CanOpen){pendingAutoOpen=false;Open();}
                transition=Mathf.MoveTowards(transition,IsOpen?1:0,Time.deltaTime/Mathf.Max(.1f,content.lookDownSeconds));
                if(Keyboard.current?.backspaceKey.wasPressedThisFrame==true&&IsOpen)Close();
            }
            if(IsOpen && coffee!=null && coffee.Running)Close();
            float eased=transition*transition*(3-2*transition);
            panel.gameObject.SetActive(transition>0||IsOpen);
            panel.anchoredPosition=new Vector2(Mathf.Lerp(1270,880,eased),-Mathf.Lerp(626,185,eased));
            panel.localScale=Vector3.one*Mathf.Lerp(.22f,1,eased);
            if(stage!=null)stage.SetPhoneFocus(eased);
            notification.SetActive(enableNotifications&&Inbox.Unread>0&&!IsOpen);
            badge.text="!  "+Inbox.Unread+" NEW / tap to check";
            status.text=enableNotifications?Inbox.Unread+" UNREAD  /  Scroll to browse":"NOTIFICATIONS OFF  /  Quiet mode";
            if(rendered!=Inbox.delivered.Count)RenderFeed();
        }
        public void SetNotifications(bool enabled){enableNotifications=enabled;if(!enabled){pendingAutoOpen=false;Inbox?.Tick(0,false);}}
        public bool Open()
        {
            EnsureUI();if(root==null||IsOpen||!CanOpen)return false;
            IsOpen=true;lockedPlayer=PlayerMover.Instance;lockedPlayer?.Lock("cafe-phone");onOpened.Invoke();return true;
        }
        public void Close()
        {
            if(!IsOpen)return;
            IsOpen=false;if(detail!=null)detail.gameObject.SetActive(false);
            if(lockedPlayer!=null)lockedPlayer.Unlock("cafe-phone");lockedPlayer=null;onClosed.Invoke();
        }
        void RenderFeed()
        {
            // Only rebuild when new posts arrive; scrolling itself never changes the Meter.
            for(int i=contentRoot.childCount-1;i>=0;i--)if(contentRoot.GetChild(i)!=empty.transform)Destroy(contentRoot.GetChild(i).gameObject);
            rendered=Inbox.delivered.Count;empty.gameObject.SetActive(rendered==0);
            contentRoot.sizeDelta=new Vector2(440,Mathf.Max(475,rendered*182));
            for(int i=0;i<rendered;i++)
            {
                var post=Inbox.delivered[i];
                var card=Box(contentRoot,"Post "+post.id,0,i*182,430,170,C("182B39")).rectTransform;
                Text(card,font,(post.advertisement?"SPONSORED / ":"MESSAGE / ")+post.source,18,14,395,25,12,post.advertisement?Pink:Cyan);
                Text(card,font,post.title,18,50,395,55,22,White);
                var b=Button(card,font,Inbox.IsRead(post)?"READ AGAIN (NO EXTRA COST)":"VIEW  /  +"+post.meterIncrease.ToString("0.#"),18,119,395,37,()=>ReadPost(post));
                b.GetComponentInChildren<TextMeshProUGUI>().fontSize=14;
            }
        }
        public bool ReadPost(PhonePost post)
        {
            if(!IsOpen||Transitioning||CafeRuntime.Paused||post==null||!Inbox.delivered.Contains(post))return false;
            if(!Inbox.IsRead(post))
            {
                var meter=MeterService.Instance;
                // If a present Meter cannot accept this effect, leave the item unread so the cost is never silently lost.
                if(meter!=null&&post.meterIncrease>0&&!meter.Model.Apply(post.meterIncrease,"phone:"+sessionId+":"+post.id,post.title))return false;
                Inbox.MarkRead(post);ViewedCount++;RenderFeed();
            }
            detailSource.text=post.advertisement?"SPONSORED CONTENT":post.source;
            detailTitle.text=post.title;detailBody.text=post.body;detail.gameObject.SetActive(true);return true;
        }
        void OnDisable(){Close();if(root!=null)root.gameObject.SetActive(false);if(stage!=null)stage.SetPhoneFocus(0);transition=0;}
        void OnEnable(){if(root!=null)root.gameObject.SetActive(true);}
    }
}

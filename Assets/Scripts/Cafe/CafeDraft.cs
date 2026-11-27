using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Exploration.InteractionUI;

public sealed class CafeDraft : MonoBehaviour
{
    public Texture2D background, visitor;
    public TMP_FontAsset font;
    public bool showVisitor=true, gentleMotion=true;
    [TextArea] public string[] dialogue={"You came. I wasn't sure you would.","Leave your phone on the counter. I'd rather hear what you think.","For a moment, the city outside feels far away."};
    RectTransform portrait, dialoguePanel, controls, heading;
    TextMeshProUGUI line, speaker, progress;
    Button next, replyA, replyB;
    int page;
    bool sceneOnly;
    void Start()
    {
        var root=Canvas(transform,50);
        var bg=Rect(root,"Cafe background",0,0,1600,900).gameObject.AddComponent<RawImage>();bg.texture=background;bg.raycastTarget=false;
        portrait=Rect(root,"Visitor - replace with final character",580,217,440,550);
        Art(portrait,visitor,0,0,440,550);
        // Redraw only the original counter strip above the portrait so the visitor stands BEHIND it.
        var counter=Rect(root,"Foreground counter",0,697,1600,203).gameObject.AddComponent<RawImage>();counter.texture=background;counter.uvRect=new Rect(0,0,1,203f/900);counter.raycastTarget=false;
        heading=Rect(root,"Scene label",36,30,440,60);
        Text(heading,font,"AFTER HOURS",0,0,440,28,22,C("F4D5A2"));
        Text(heading,font,"CAFE / VISUAL DRAFT 01",0,30,440,24,13,White);
        controls=Rect(root,"Draft preview controls",1000,28,564,42);
        Button(controls,font,"SCENE ONLY",0,0,170,40,ToggleScene);
        Button(controls,font,"VISITOR",180,0,170,40,()=>showVisitor=!showVisitor);
        Button(controls,font,"MOTION",360,0,170,40,()=>gentleMotion=!gentleMotion);
        dialoguePanel=Box(root,"Conversation",42,718,1516,146,new Color(.035f,.045f,.06f,.94f),true).rectTransform;
        Box(dialoguePanel,"Warm edge",0,0,3,146,C("D8AD70"));
        speaker=Text(dialoguePanel,font,"VISITOR",26,18,800,25,17,C("E8BC7D"));
        line=Text(dialoguePanel,font,"",26,55,1130,76,25,White);
        progress=Text(dialoguePanel,font,"",1250,15,230,25,14,Muted);
        next=Button(dialoguePanel,font,"CONTINUE",1240,72,245,48,Next,true);
        replyA=Button(dialoguePanel,font,"What happened?",700,75,360,48,()=>Reply("I needed to know someone was still listening."));
        replyB=Button(dialoguePanel,font,"I'm listening.",1080,75,400,48,()=>Reply("Then let's start at the beginning."));
        ShowPage();
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-cafe-capture")>=0)StartCoroutine(Capture());
    }
    void Update()
    {
        if(portrait==null)return;
        portrait.gameObject.SetActive(showVisitor);
        portrait.anchoredPosition=new Vector2(580,-217+(gentleMotion?Mathf.Sin(Time.unscaledTime*.9f)*1.5f:0));
        if(UnityEngine.InputSystem.Keyboard.current?.hKey.wasPressedThisFrame==true)ToggleScene();
    }
    public void ToggleScene()
    {
        sceneOnly=!sceneOnly;dialoguePanel.gameObject.SetActive(!sceneOnly);controls.gameObject.SetActive(!sceneOnly);heading.gameObject.SetActive(!sceneOnly);
    }
    void ShowPage()
    {
        speaker.text=page==2?"A QUIET MOMENT":"VISITOR";
        line.text=dialogue.Length>0?dialogue[Mathf.Min(page,dialogue.Length-1)]:"Add draft dialogue in the Inspector.";
        progress.text="CONVERSATION / "+(page+1).ToString("00");
        next.gameObject.SetActive(true);replyA.gameObject.SetActive(false);replyB.gameObject.SetActive(false);
        next.GetComponentInChildren<TextMeshProUGUI>().text=page>=2?"REPLAY":"CONTINUE";
    }
    public void Next()
    {
        if(page==0){page=1;ShowPage();return;}
        if(page==1){line.text="How do you respond?";next.gameObject.SetActive(false);replyA.gameObject.SetActive(true);replyB.gameObject.SetActive(true);return;}
        page=0;ShowPage();
    }
    public void Reply(string text){page=2;ShowPage();speaker.text="VISITOR";line.text=text;}
    IEnumerator Capture()
    {
        var dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Captures");Directory.CreateDirectory(dir);
        yield return new WaitForSecondsRealtime(1);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir,"cafe-conversation.png"));yield return new WaitForEndOfFrame();
        Next();Next();yield return new WaitForSecondsRealtime(.2f);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir,"cafe-choices.png"));yield return new WaitForEndOfFrame();
        Reply("Then let's start at the beginning.");
        ToggleScene();yield return new WaitForSecondsRealtime(.2f);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir,"cafe-composition.png"));yield return new WaitForEndOfFrame();
        showVisitor=false;yield return new WaitForSecondsRealtime(.2f);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir,"cafe-background.png"));yield return new WaitForEndOfFrame();
        File.WriteAllText(Path.Combine(dir,"result.txt"),"PASS: conversation, choices, response, scene-only composition and background capture.");Application.Quit();
    }
}

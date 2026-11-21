using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DopamineMeter
{
    /// <summary>Portable always-visible HUD. No references to this game's session, dialogue or player.</summary>
    public sealed class MeterView : MonoBehaviour
    {
        public TMP_FontAsset displayFont, bodyFont;
        public Texture2D sensorArt;
        public bool reduceMotion;
        public Vector2 hudOffset = new Vector2(-36, -30);
        public MeterService service;
        public Canvas Canvas { get; private set; }
        public RectTransform Hud { get; private set; }
        public static readonly Color Cyan = Hex("#00E5ED"), Magenta = Hex("#FF458C"), Muted = Hex("#8A96AB"), Ink = Hex("#080D16"), White = Hex("#E7EDF6");
        TextMeshProUGUI value, state, cause;
        Image fill, stripe;
        float shown;
        public MeterModel Model => service.Model;
        public static Color Hex(string code) { ColorUtility.TryParseHtmlString(code, out var c); return c; }
        public static Color Tint(MeterModel m) => m.Life == MeterLife.Dormant ? Muted :
            m.Band >= MeterBand.Critical ? Magenta : m.Band >= MeterBand.Intrusive ? Hex("#F1CE76") : Cyan;
        public static string Status(MeterModel m) => m.Life == MeterLife.Dormant ? "STANDBY" : m.Life == MeterLife.Dead ? "OVERLOAD" : m.Suspended ? "PAUSED" : m.Band.ToString().ToUpperInvariant();
        void Start()
        {
            if (service == null) service = GetComponent<MeterService>();
            if (service == null || service != MeterService.Instance) return;
            Canvas = MakeCanvas("Dopamine / persistent HUD", 100);
            Canvas.transform.SetParent(transform, false);
            Hud = Panel(Canvas.transform,"Meter widget",0,0,350,142,Hex("#101725"));
            Hud.anchorMin = Hud.anchorMax = Hud.pivot = new Vector2(1,1); Hud.anchoredPosition = hudOffset;
            stripe = Box(Hud,"Status stripe",0,0,3,142,Cyan);
            Label(Hud,"DOPAMINE",24,15,190,22,14,Cyan,true);
            value = Label(Hud,"50",24,42,110,45,32,White,true);
            value.overflowMode = TextOverflowModes.Overflow;
            Art(Hud, sensorArt, 278, 14, 40, 40, Color.white);
            Label(Hud,"/ 100",140,61,50,20,12,Muted);
            state = Label(Hud,"STANDBY",200,67,126,24,12,Muted);
            Box(Hud,"Track",24,98,302,3,Hex("#293243"));
            fill = Box(Hud,"Fill",24,98,151,3,Cyan);
            cause = Label(Hud,"Awaiting device",24,112,306,18,10,Muted);
            shown = Model.Value;
            Model.Changed += Refresh; Refresh();
        }
        void Refresh()
        {
            if (state == null) return;
            state.text = Status(Model); state.color = stripe.color = fill.color = Tint(Model);
            cause.text = Model.LastCause;
        }
        void Update()
        {
            if (value == null) return;
            shown = reduceMotion ? Model.Value : Mathf.Lerp(shown,Model.Value,1-Mathf.Exp(-12*Time.unscaledDeltaTime));
            value.text = Model.Value.ToString("0.#"); // Exact shared value; only the bar animates.
            fill.rectTransform.sizeDelta = new Vector2(302*shown/100,3);
        }
        void OnDestroy() { if (service != null && service.Model != null) service.Model.Changed -= Refresh; }
        public Canvas MakeCanvas(string name,int order)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var c=go.GetComponent<Canvas>(); c.renderMode=RenderMode.ScreenSpaceOverlay; c.sortingOrder=order;
            var s=go.GetComponent<CanvasScaler>(); s.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution=new Vector2(1600,900); s.matchWidthOrHeight=.5f;
            return c;
        }
        public static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform; r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); return r;
        }
        public static Image Box(Transform p,string n,float x,float y,float w,float h,Color c,bool raycast=false)
        {
            var im=Rect(p,n,x,y,w,h).gameObject.AddComponent<Image>(); im.color=c; im.raycastTarget=raycast; return im;
        }
        public static RectTransform Panel(Transform p,string n,float x,float y,float w,float h,Color c)
        {
            var im=Box(p,n,x,y,w,h,c);
            Box(im.transform,"Top edge",0,0,w,1,Hex("#334052"));
            Box(im.transform,"Bottom edge",0,h-1,w,1,Hex("#202B3B")); return im.rectTransform;
        }
        public TextMeshProUGUI Label(Transform p,string text,float x,float y,float w,float h,float size,Color color,bool display=false)
        {
            var t=Rect(p,text,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();
            if ((display ? displayFont : bodyFont) != null) t.font=display ? displayFont : bodyFont;
            t.text=text; t.fontSize=size; t.color=color; t.raycastTarget=false;
            t.textWrappingMode=TextWrappingModes.NoWrap; t.overflowMode=TextOverflowModes.Ellipsis;
            return t;
        }
        public Button Button(Transform p,string text,float x,float y,float w,float h,Action action,bool primary=false)
        {
            var im=Box(p,text,x,y,w,h,primary ? Cyan : Hex("#192434"),true);
            var b=im.gameObject.AddComponent<Button>(); b.targetGraphic=im;
            var c=b.colors; c.highlightedColor=new Color(.75f,1,1); c.pressedColor=new Color(.5f,.7f,.8f); c.disabledColor=new Color(.4f,.4f,.4f); b.colors=c;
            var t=Label(im.transform,text,16,0,w-32,h,14,primary ? Ink : White); t.alignment=TextAlignmentOptions.MidlineLeft;
            if (action != null) b.onClick.AddListener(()=>action()); return b;
        }
        public RawImage Art(Transform p,Texture texture,float x,float y,float w,float h,Color tint)
        {
            var im=Rect(p,"Artwork",x,y,w,h).gameObject.AddComponent<RawImage>(); im.texture=texture; im.color=tint; im.raycastTarget=false; return im;
        }
    }
}

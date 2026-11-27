using System;
using System.Collections.Generic;
using DopamineMeter;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Exploration.InteractionUI;

namespace Exploration
{
    public sealed class InteractionSystem : MonoBehaviour
    {
        public TMP_FontAsset font;
        public Camera worldCamera;
        [Tooltip("Empty = automatically find active Interactable components in this scene at Start.")]
        public Interactable[] objects;
        public bool reduceMotion;
        public Interactable Selected { get; private set; }
        public bool IsInspecting => Selected != null;
        public event Action<Interactable> Completed;
        public RectTransform Root { get; private set; }
        readonly List<Button> markers = new List<Button>();
        readonly List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();
        RectTransform modal, card;
        TextMeshProUGUI title, category, body, toast, actionText;
        RawImage art;
        Button confirm;
        PlayerMover lockedPlayer;
        MeterService pausedMeter;
        float toastTime, openedAt;
        public static bool InputBlocked => GameSession.Instance != null && (GameSession.Instance.IsPaused || GameSession.Instance.IsBusy || GameSession.Instance.AnyDialogueRunning || GameSession.Instance.IsOnTitle) || GameInput.IsRebinding;
        void Start()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            if (objects == null || objects.Length == 0) objects = FindObjectsByType<Interactable>(FindObjectsSortMode.None);
            Root = Canvas(transform, 200);
            foreach (var obj in objects)
            {
                var captured = obj;
                var b = Button(Root, font, "", 0,0,224,52, () => Inspect(captured));
                markers.Add(b); labels.Add(b.GetComponentInChildren<TextMeshProUGUI>());
            }
            modal = Box(Root,"Inspection backdrop",0,0,1600,900,new Color(.015f,.025f,.045f,.86f),true).rectTransform;
            card = Box(modal,"Inspection",280,190,1040,530,Panel).rectTransform;
            Box(card,"Accent",0,0,1040,3,Cyan);
            category = Text(card,font,"EVIDENCE",36,30,800,30,15,Cyan);
            title = Text(card,font,"Object",36,70,960,65,34,White);
            var artFrame = Box(card,"Object preview",36,155,380,270,Ink).rectTransform;
            art = Art(artFrame,null,0,0,380,270);
            body = Text(card,font,"",452,155,548,245,22,White);
            Button(card,font,"CLOSE",36,452,180,48,Close);
            confirm = Button(card,font,"CONFIRM",650,452,350,48,Confirm,true);
            actionText = confirm.GetComponentInChildren<TextMeshProUGUI>();
            modal.gameObject.SetActive(false);
            toast = Text(Root,font,"",400,820,800,48,22,Cyan); toast.alignment=TextAlignmentOptions.Center;
        }
        void Update()
        {
            if (Root == null) return;
            if (modal.gameObject.activeSelf && Selected == null) Close();
            bool blocked = InputBlocked;
            float best = float.MaxValue; Interactable nearest = null;
            for (int i=0;i<objects.Length;i++)
            {
                var item=objects[i]; var marker=markers[i];
                bool visible = item != null && item.isActiveAndEnabled && !(item.Completed && item.hideWhenCompleted) && !IsInspecting && !blocked;
                if (visible && worldCamera != null)
                {
                    var p=worldCamera.WorldToScreenPoint(item.transform.position);
                    visible=p.z>0 && p.x>=0 && p.x<=Screen.width && p.y>=0 && p.y<=Screen.height;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(Root,p,null,out var local);
                    marker.GetComponent<RectTransform>().anchoredPosition = local + new Vector2(800-112,-450-26);
                    labels[i].text = item.Completed && item.oneShot ? "DONE / " + item.displayName : (item.Available ? "[E] " : "LOCK / ") + item.displayName;
                    marker.image.color = item.Completed && item.oneShot ? C("1C3936") : item.Available ? C("203C49") : C("242B38");
                    if (item.CanInteract && Pointer.current != null)
                    {
                        float distance=Vector2.Distance(p,Pointer.current.position.ReadValue());
                        if(distance<best){ best=distance; nearest=item; }
                    }
                }
                marker.gameObject.SetActive(visible);
            }
            if (Keyboard.current != null && !blocked)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame && !IsInspecting && nearest != null) Inspect(nearest);
                if (Keyboard.current.backspaceKey.wasPressedThisFrame && IsInspecting) Close();
            }
            if (IsInspecting)
            {
                confirm.interactable = !blocked && Selected.CanInteract;
                float t=reduceMotion ? 1 : Mathf.Clamp01((Time.unscaledTime-openedAt)*7);
                card.localScale=Vector3.one*Mathf.Lerp(.96f,1,1-Mathf.Pow(1-t,3));
            }
            toast.color = new Color(Cyan.r,Cyan.g,Cyan.b,Mathf.Clamp01(toastTime-Time.unscaledTime));
        }
        public bool Inspect(Interactable item)
        {
            if (InputBlocked || IsInspecting || item == null) return false;
            if (!item.CanInteract) { Notify(item.Completed && item.oneShot ? "Already recorded: " + item.displayName : item.BlockReason); return false; }
            Selected=item; toastTime=0; title.text=item.displayName; category.text=item.category+"  /  INSPECT"; body.text=item.description;
            actionText.text=item.actionLabel; art.texture=item.artwork; art.gameObject.SetActive(item.artwork != null);
            var fit=art.GetComponent<AspectRatioFitter>();
            if(item.artwork != null){if(fit==null)fit=art.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=(float)item.artwork.width/item.artwork.height;}
            lockedPlayer=PlayerMover.Instance; lockedPlayer?.Lock("interaction-inspect");
            pausedMeter=MeterService.Instance; pausedMeter?.Model.Suspend("interaction-inspect",true);
            openedAt=Time.unscaledTime; modal.gameObject.SetActive(true); return true;
        }
        public void Confirm()
        {
            if (InputBlocked || Selected == null || !Selected.CanInteract) return;
            var item=Selected;
            // Release the inspection pause before applying an explicit Meter event.
            Close();
            if (item.Complete()) { Notify("RECORDED / " + item.displayName); Completed?.Invoke(item); }
        }
        public void Close()
        {
            Selected=null; if(modal!=null)modal.gameObject.SetActive(false);
            if(lockedPlayer!=null)lockedPlayer.Unlock("interaction-inspect"); lockedPlayer=null;
            if(pausedMeter!=null)pausedMeter.Model.Suspend("interaction-inspect",false); pausedMeter=null;
        }
        public void Notify(string message) { if(toast==null)return; toast.text=message;toastTime=Time.unscaledTime+3.5f; }
        void OnDisable() => Close();
    }
}

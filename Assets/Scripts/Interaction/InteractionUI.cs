using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exploration
{
    // Fixed design surface, uniformly fitted to every screen (no cropped controls).
    public static class InteractionUI
    {
        public static Color Ink = C("0B111D"), Panel = C("131E2E"), Cyan = C("76F5DE"), White = C("EDF1F5"), Muted = C("91A3B8"), Pink = C("FF82B3");
        public static Color C(string s) { ColorUtility.TryParseHtmlString("#" + s, out var c); return c; }
        public static RectTransform Rect(Transform p, string n, float x, float y, float w, float h)
        {
            var r = new GameObject(n, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(p, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        public static Image Box(Transform p, string n, float x, float y, float w, float h, Color color, bool raycast = false)
        {
            var im = Rect(p, n, x, y, w, h).gameObject.AddComponent<Image>(); im.color = color; im.raycastTarget = raycast; return im;
        }
        public static TextMeshProUGUI Text(Transform p, TMP_FontAsset font, string text, float x, float y, float w, float h, float size, Color color)
        {
            var t = Rect(p, text, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>(); if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.color = color; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; return t;
        }
        public static Button Button(Transform p, TMP_FontAsset font, string text, float x, float y, float w, float h, Action action, bool primary = false)
        {
            var im = Box(p, text, x, y, w, h, primary ? Cyan : C("25364A"), true);
            var b = im.gameObject.AddComponent<Button>(); b.targetGraphic = im;
            var colors = b.colors; colors.highlightedColor = new Color(.7f, 1, .9f); colors.pressedColor = new Color(.5f, .7f, .7f); b.colors = colors;
            var label = Text(im.transform, font, text, 18, 0, w-36, h, 17, primary ? Ink : White); label.alignment = TextAlignmentOptions.MidlineLeft;
            b.onClick.AddListener(() => action?.Invoke()); return b;
        }
        public static RawImage Art(Transform p, Texture tex, float x, float y, float w, float h)
        {
            var im = Rect(p, "Art", x,y,w,h).gameObject.AddComponent<RawImage>(); im.texture = tex; im.raycastTarget = false;
            im.rectTransform.anchorMin=im.rectTransform.anchorMax=im.rectTransform.pivot=new Vector2(.5f,.5f);
            im.rectTransform.anchoredPosition=Vector2.zero;
            if (tex != null) { var fit = im.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = (float)tex.width/tex.height; }
            return im;
        }
        public static RectTransform Canvas(Transform parent, int order)
        {
            var go = new GameObject("Interaction canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); go.transform.SetParent(parent, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; go.GetComponent<Canvas>().sortingOrder = order;
            var root = Rect(go.transform,"1600 x 900 design surface",0,0,1600,900);
            root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);
            root.gameObject.AddComponent<InteractionCanvasFit>(); return root;
        }
    }
}

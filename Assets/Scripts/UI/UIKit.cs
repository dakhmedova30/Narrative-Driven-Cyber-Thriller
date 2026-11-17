using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Tiny helpers that build the TEMPLATE UI in code (pause menu, save slots, settings, title).
/// To restyle later: change the colours/sizes here, or replace a panel with your own prefab.
/// </summary>
public static class UIKit
{
    public static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.10f, 0.96f);
    public static readonly Color DimColor = new Color(0f, 0f, 0f, 0.65f);
    public static readonly Color ButtonColor = new Color(0.15f, 0.18f, 0.24f, 1f);
    public static readonly Color ButtonOnColor = new Color(0.12f, 0.45f, 0.55f, 1f);
    public static readonly Color TextColor = new Color(0.92f, 0.95f, 1f, 1f);
    public static readonly Color MutedColor = new Color(0.60f, 0.66f, 0.75f, 1f);
    public static readonly Color AccentColor = new Color(0.30f, 0.95f, 1f, 1f);
    public static readonly Color DangerColor = new Color(0.55f, 0.18f, 0.20f, 1f);

    public static Canvas CreateCanvas(string name, Transform parent, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        if (parent != null) go.transform.SetParent(parent, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static void Stretch(RectTransform rt, float margin = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);
    }

    /// <summary>Fixed-size box anchored at a point of the parent (0..1), e.g. (0.5,0.5) = centre.</summary>
    public static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 offset = default)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
    }

    public static Image Image(Transform parent, string name, Color color, bool raycast = true)
    {
        RectTransform rt = Rect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    /// <summary>Full-screen panel (dim background) that blocks clicks to the game behind it.</summary>
    public static RectTransform Overlay(Transform parent, string name)
    {
        Image dim = Image(parent, name, DimColor);
        Stretch(dim.rectTransform);
        return dim.rectTransform;
    }

    public static TextMeshProUGUI Label(Transform parent, string text, float size, Color? color = null,
                                        TextAlignmentOptions align = TextAlignmentOptions.Left, float height = -1f)
    {
        RectTransform rt = Rect("Label", parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color ?? TextColor;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        if (height > 0f) Size(rt.gameObject, -1f, height);
        return tmp;
    }

    public static Button Button(Transform parent, string label, UnityAction onClick,
                                float width = -1f, float height = 56f, float fontSize = 26f)
    {
        Image bg = Image(parent, "Button " + label, ButtonColor);
        var button = bg.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        button.colors = colors;
        if (onClick != null) button.onClick.AddListener(onClick);

        TextMeshProUGUI text = Label(bg.transform, label, fontSize, TextColor, TextAlignmentOptions.Center);
        Stretch(text.rectTransform, 6f);
        Size(bg.gameObject, width, height);
        return button;
    }

    public static void SetText(Button button, string text)
    {
        var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
    }

    public static void SetOn(Button button, bool on)
    {
        var img = button.GetComponent<Image>();
        if (img != null) img.color = on ? ButtonOnColor : ButtonColor;
    }

    public static VerticalLayoutGroup Vertical(GameObject go, float spacing, int padding = 0,
                                               TextAnchor align = TextAnchor.UpperCenter, bool stretchWidth = true)
    {
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = spacing;
        v.padding = new RectOffset(padding, padding, padding, padding);
        v.childAlignment = align;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = stretchWidth;
        v.childForceExpandHeight = false;
        return v;
    }

    public static HorizontalLayoutGroup Horizontal(GameObject go, float spacing, int padding = 0,
                                                   TextAnchor align = TextAnchor.MiddleLeft)
    {
        var h = go.AddComponent<HorizontalLayoutGroup>();
        h.spacing = spacing;
        h.padding = new RectOffset(padding, padding, padding, padding);
        h.childAlignment = align;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        return h;
    }

    public static LayoutElement Size(GameObject go, float width = -1f, float height = -1f, float flexibleWidth = -1f)
    {
        if (!go.TryGetComponent(out LayoutElement le)) le = go.AddComponent<LayoutElement>();
        if (width >= 0f) le.preferredWidth = width;
        if (height >= 0f) le.preferredHeight = height;
        if (flexibleWidth >= 0f) le.flexibleWidth = flexibleWidth;
        return le;
    }

    /// <summary>A row: label on the left, content added by the caller on the right.</summary>
    public static RectTransform Row(Transform parent, string label, float labelWidth = 340f, float height = 52f)
    {
        RectTransform row = Rect("Row " + label, parent);
        Horizontal(row.gameObject, 16f);
        Size(row.gameObject, -1f, height);
        TextMeshProUGUI l = Label(row, label, 24f, MutedColor, TextAlignmentOptions.Left);
        Size(l.gameObject, labelWidth, height);
        return row;
    }

    public static Slider Slider(Transform parent, float min, float max, float value, UnityAction<float> onChange, float width = 420f)
    {
        RectTransform root = Rect("Slider", parent);
        Size(root.gameObject, width, 40f);

        Image bg = Image(root, "Background", new Color(1f, 1f, 1f, 0.12f));
        bg.rectTransform.anchorMin = new Vector2(0f, 0.4f);
        bg.rectTransform.anchorMax = new Vector2(1f, 0.6f);
        bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;

        RectTransform fillArea = Rect("Fill Area", root);
        fillArea.anchorMin = new Vector2(0f, 0.4f);
        fillArea.anchorMax = new Vector2(1f, 0.6f);
        fillArea.offsetMin = new Vector2(8f, 0f);
        fillArea.offsetMax = new Vector2(-8f, 0f);
        Image fill = Image(fillArea, "Fill", AccentColor, false);
        fill.rectTransform.sizeDelta = Vector2.zero;

        RectTransform handleArea = Rect("Handle Area", root);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(10f, 0f);
        handleArea.offsetMax = new Vector2(-10f, 0f);
        Image handle = Image(handleArea, "Handle", TextColor);
        handle.rectTransform.sizeDelta = new Vector2(20f, 0f);

        var slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(value);
        if (onChange != null) slider.onValueChanged.AddListener(onChange);
        return slider;
    }

    /// <summary>Vertical scroll list. Add children to the returned content.</summary>
    public static RectTransform ScrollList(Transform parent, float spacing = 10f)
    {
        RectTransform root = Rect("Scroll", parent);
        var scroll = root.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.scrollSensitivity = 40f;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewport = Rect("Viewport", root);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        var vpImage = viewport.gameObject.AddComponent<Image>();
        vpImage.color = new Color(0f, 0f, 0f, 0f); // catches scroll-wheel events

        RectTransform content = Rect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = content.offsetMax = Vector2.zero;
        Vertical(content.gameObject, spacing, 8);
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;
        return content;
    }

    public static void Clear(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
    }
}

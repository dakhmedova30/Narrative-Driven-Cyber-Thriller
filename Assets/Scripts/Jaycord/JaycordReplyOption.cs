using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One reply the player can send. States match Figma "Reply Option":
/// Default / Hover / Selected / Disabled (= option's condition not met → LOCKED).
/// </summary>
public class JaycordReplyOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public enum State { Default, Hover, Selected, Disabled }

    [SerializeField] private Image fill;
    [SerializeField] private Image border;
    [SerializeField] private Image glow;
    [SerializeField] private TMP_Text indexText;
    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject lockedTag;

    private Action onSelected;
    private bool available;
    private bool interactable;

    public bool CanSelect => available && interactable;

    public void Setup(int number, string text, bool isAvailable, Action onSelectedCallback)
    {
        indexText.text = $"[{number}]";
        label.text = text;
        available = isAvailable;
        interactable = true;
        onSelected = onSelectedCallback;
        if (lockedTag != null) lockedTag.SetActive(!isAvailable);
        SetState(isAvailable ? State.Default : State.Disabled);
    }

    /// <summary>Choose this reply (mouse click or number key).</summary>
    public void Select()
    {
        if (!CanSelect) return;
        interactable = false;
        SetState(State.Selected);
        onSelected?.Invoke();
    }

    /// <summary>Stop reacting to input (another reply was chosen).</summary>
    public void Lock() => interactable = false;

    public void OnPointerEnter(PointerEventData eventData) { if (CanSelect) SetState(State.Hover); }
    public void OnPointerExit(PointerEventData eventData)  { if (CanSelect) SetState(State.Default); }
    public void OnPointerClick(PointerEventData eventData) { Select(); }

    public void SetState(State state)
    {
        // Colours follow Figma "Reply Option" (Default / Hover / Selected / Disabled).
        switch (state)
        {
            case State.Default:
                Apply(JaycordTheme.BgElevated, JaycordTheme.Cyan, 0f,
                      JaycordTheme.Cyan, JaycordTheme.TextPrimary);
                break;
            case State.Hover:
                Apply(JaycordTheme.WithAlpha(JaycordTheme.Cyan, 0.28f), JaycordTheme.Cyan, 0.35f,
                      JaycordTheme.Cyan, JaycordTheme.TextPrimary);
                break;
            case State.Selected:
                Apply(JaycordTheme.Cyan, JaycordTheme.Cyan, 0.55f,
                      JaycordTheme.TextOnAccent, JaycordTheme.TextOnAccent);
                break;
            case State.Disabled:
                Apply(JaycordTheme.BgElevated, JaycordTheme.Border, 0f,
                      JaycordTheme.TextMuted, JaycordTheme.TextMuted);
                break;
        }
    }

    private void Apply(Color fillColor, Color borderColor, float glowAlpha, Color indexColor, Color labelColor)
    {
        if (fill != null) fill.color = fillColor;
        if (border != null) border.color = borderColor;
        if (glow != null) glow.color = JaycordTheme.WithAlpha(JaycordTheme.Cyan, glowAlpha);
        indexText.color = indexColor;
        label.color = labelColor;
    }
}

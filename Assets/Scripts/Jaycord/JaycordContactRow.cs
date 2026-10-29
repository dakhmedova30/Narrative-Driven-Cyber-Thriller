using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>One row in the chat list (avatar, presence dot, name, last message, unread badge). Click to open the chat.</summary>
public class JaycordContactRow : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image selectedAccent;
    [SerializeField] private Image avatar;
    [SerializeField] private Image presenceDot;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text previewText;
    [SerializeField] private GameObject unreadBadge;
    [SerializeField] private TMP_Text unreadText;

    public string ContactName { get; private set; }

    /// <summary>Set by JaycordApp.</summary>
    public Action onClick;

    private bool selected;
    private bool hovered;

    public void Bind(JaycordContact contact)
    {
        ContactName = contact.name;
        nameText.text = contact.name;
        previewText.text = contact.preview;
        if (contact.avatar != null)
        {
            avatar.sprite = contact.avatar;
            avatar.color = Color.white;
        }
        presenceDot.color = JaycordApp.PresenceColor(contact.presence);
        unreadBadge.SetActive(contact.unread > 0);
        unreadText.text = contact.unread > 99 ? "99+" : contact.unread.ToString();
    }

    public void SetSelected(bool isSelected)
    {
        selected = isSelected;
        selectedAccent.enabled = isSelected;
        Refresh();
    }

    public void OnPointerClick(PointerEventData eventData) => onClick?.Invoke();
    public void OnPointerEnter(PointerEventData eventData) { hovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; Refresh(); }

    private void Refresh()
    {
        background.color = selected ? JaycordTheme.BgElevated
                         : hovered ? JaycordTheme.WithAlpha(JaycordTheme.BgElevated, 0.5f)
                         : new Color(0f, 0f, 0f, 0f);
    }
}

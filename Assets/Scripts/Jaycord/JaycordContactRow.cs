using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One row in the chat list (avatar, presence dot, name, last message, unread badge).</summary>
public class JaycordContactRow : MonoBehaviour
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

    public void SetSelected(bool selected)
    {
        background.color = selected ? JaycordTheme.BgElevated : new Color(0f, 0f, 0f, 0f);
        selectedAccent.enabled = selected;
    }
}

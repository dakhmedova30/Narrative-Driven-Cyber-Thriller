using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FriendView : MonoBehaviour
{
    public TMP_Text usernameText;
    public StatusIcon status;
    public Image avatar;
    public Toggle toggle;

    // public void Setup(string username, Presence p, ToggleGroup group, Sprite pic = null)
    // {
    //     usernameText.text = username;
    //     status.Set(p);
    //     toggle.group = group;
    //     if (pic != null) avatar.sprite = pic;
    // }

    public void Setup(string username, Presence p, ToggleGroup group,
                    System.Action<string> onSelected, Sprite pic = null)
    {
        usernameText.text = username;
        status.Set(p);
        if (pic != null) avatar.sprite = pic;

        toggle.SetIsOnWithoutNotify(false);   // start unselected, so no row fires an event by itself
        toggle.group = group;
        toggle.onValueChanged.AddListener(on => { if (on) onSelected?.Invoke(username); });

        SetUnread(0); 
        SetPreview("");
    }

    public TMP_Text previewText;
    public GameObject unreadBadge;
    public TMP_Text unreadText;      // leave empty for a plain dot

    public void SetPreview(string text)
    {
        if (previewText != null) previewText.text = text;
    }

    public void SetUnread(int count)
    {
        unreadBadge.SetActive(count > 0);
        if (unreadText != null) unreadText.text = count > 9 ? "9+" : count.ToString();
    }

    public void SetStatus(Presence p) { status.Set(p); }
}
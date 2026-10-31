using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FriendView : MonoBehaviour
{
    public TMP_Text usernameText;
    public StatusIcon status;
    public Image avatar;
    public Toggle toggle;

    public void Setup(string username, Presence p, ToggleGroup group, Sprite pic = null)
    {
        usernameText.text = username;
        status.Set(p);
        toggle.group = group;
        if (pic != null) avatar.sprite = pic;
    }

    public void SetStatus(Presence p) { status.Set(p); }
}
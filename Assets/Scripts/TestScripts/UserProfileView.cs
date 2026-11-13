using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserProfileView : MonoBehaviour
{
    // public TMP_Text usernameText;
    public StatusIcon status;
    // public Image avatar;
    public TMP_InputField statusInput;


    // public void Setup(string username, Presence p, ToggleGroup group, Sprite pic = null)
    // {
    //     usernameText.text = username;
    //     status.Set(p);
    //     if (pic != null) avatar.sprite = pic;
    // }


    public void SetStatus(Presence p) { status.Set(p); }

    public void SetStatusMessage(string message) { statusInput.text = message; }
}
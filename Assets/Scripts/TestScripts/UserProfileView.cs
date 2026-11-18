using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserProfileView : MonoBehaviour
{
    // public TMP_Text usernameText;
    public StatusIcon status;
    public TMP_InputField statusInput;
    public Image avatar; 

    public Sprite Avatar => avatar != null ? avatar.sprite : null;

    public void SetAvatar(Sprite pic)
    {
        if (pic != null) avatar.sprite = pic;
    }

    public void SetStatus(Presence p) { status.Set(p); }
    public void SetStatusMessage(string message) { statusInput.text = message; }
}
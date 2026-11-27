using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserProfileView : MonoBehaviour
{
    public string username = "Sage";
    public TMP_Text usernameText;
    public Image avatar;
    public TMP_InputField statusInput;
    public StatusIcon status;

    public Sprite Avatar => avatar != null ? avatar.sprite : null;

    void Awake() { Refresh(); }
    void OnValidate() { Refresh(); }          // updates in the editor too
    void Refresh() { if (usernameText != null) usernameText.text = username; }

    public void SetAvatar(Sprite pic) { if (pic != null) avatar.sprite = pic; }
    public void SetStatus(Presence p) { status.Set(p); }
    public void SetStatusMessage(string m) { statusInput.text = m; }
}
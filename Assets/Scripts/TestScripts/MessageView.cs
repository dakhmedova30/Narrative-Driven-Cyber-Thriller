using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessageView : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text timeText;
    public TMP_Text bodyText;
    public Image avatar;

    public void Setup(string who, string time, string body, Sprite pic = null)
    {
        nameText.text = who;
        timeText.text = time;
        bodyText.text = body;
        if (pic != null) avatar.sprite = pic;
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessageView : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text timeText;
    public TMP_Text bodyText;
    public Image avatar;

    public Color playerColor = new Color32(52, 148, 0, 255);
    public Color npcColor = Color.black;

    public void Setup(string who, string time, string body, bool isPlayer = false, Sprite pic = null)
    {
        nameText.text = who;
        timeText.text = time;
        bodyText.text = body;

        Color c = isPlayer ? playerColor : npcColor;
        nameText.color = c;
        timeText.color = c;

        if (pic != null) avatar.sprite = pic;
    }
}
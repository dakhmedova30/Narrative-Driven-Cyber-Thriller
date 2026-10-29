using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{
    public MessageView messagePrefab;
    public Transform content;
    public ScrollRect scrollRect;

    public void AddMessage(string who, string time, string body)
    {
        MessageView msg = Instantiate(messagePrefab, content);
        msg.Setup(who, time, body);
        StartCoroutine(ScrollToBottom());
    }

    IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // temporary, just to test
    public void TestSend()
    {
        AddMessage("MC", "00:00 AM", "here is the text message");
    }
}
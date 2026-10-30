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
    
    public OptionView optionPrefab;
    public Transform optionsContainer;
    public ToggleGroup optionsGroup;

    public void ShowOptions(string[] options)
    {
        foreach (Transform child in optionsContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < options.Length; i++)
        {
            OptionView opt = Instantiate(optionPrefab, optionsContainer);
            opt.Setup(options[i], i, optionsGroup);
            opt.toggle.isOn = (i == 0);
        }
    }

    public int GetSelectedIndex()
    {
        foreach (OptionView opt in optionsContainer.GetComponentsInChildren<OptionView>())
            if (opt.toggle.isOn) return opt.index;
        return -1;
    }

    // temporary, just to test
    public void TestSend()
    {
        Debug.Log("Send clicked!");
        AddMessage("MC", "00:00 AM", "here is the text message");
        ShowOptions(new string[] { "Text 1...", "Text 2...", "Text 3..." });
    }
}
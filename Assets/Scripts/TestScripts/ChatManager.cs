using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class StoredMessage
{
    public string who, time, body;
    public bool isPlayer;
}

public class ChatManager : MonoBehaviour
{
    public MessageView messagePrefab;
    public Transform content;
    public ScrollRect scrollRect;

    // public void AddMessage(string who, string time, string body, bool isPlayer = false)
    // {
    //     MessageView msg = Instantiate(messagePrefab, content);
    //     msg.Setup(who, time, body, isPlayer);
    //     StartCoroutine(ScrollToBottom());
    // }

    Dictionary<string, int> unread = new Dictionary<string, int>();
    Dictionary<string, string> preview = new Dictionary<string, string>();
    public event System.Action<string, string, int> RoomUpdated;   // room, preview, unread

    public int GetUnread(string room) => unread.TryGetValue(room, out int n) ? n : 0;
    public string GetPreview(string room) => preview.TryGetValue(room, out string s) ? s : "";

    public TMP_Text headerName;    // the NPC name in the chat header
    string currentRoom = "";
    Dictionary<string, List<StoredMessage>> history = new Dictionary<string, List<StoredMessage>>();

    public void AddMessage(string room, string who, string time, string body, bool isPlayer = false)
    {
        if (!history.ContainsKey(room)) history[room] = new List<StoredMessage>();
        var m = new StoredMessage { who = who, time = time, body = body, isPlayer = isPlayer };
        history[room].Add(m);

        if (!isPlayer)
        {
            preview[room] = body;
            if (room != currentRoom) unread[room] = GetUnread(room) + 1;
            RoomUpdated?.Invoke(room, GetPreview(room), GetUnread(room));
        }

        if (room == currentRoom)      // only draw it if that room is on screen
        {
            SpawnBubble(m);
            StartCoroutine(ScrollToBottom());
        }
    }

    void SpawnBubble(StoredMessage m)
    {
        MessageView msg = Instantiate(messagePrefab, content);
        msg.Setup(m.who, m.time, m.body, m.isPlayer);
    }

    public void OpenRoom(string room)
    {
        currentRoom = room;
        if (headerName != null) headerName.text = room;

        foreach (Transform child in content) Destroy(child.gameObject);

        if (history.TryGetValue(room, out var list))
            foreach (var m in list) SpawnBubble(m);

        StartCoroutine(ScrollToBottom());
        RefreshOptionsVisibility();

        unread[room] = 0;
        RoomUpdated?.Invoke(room, GetPreview(room), 0);
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

    public void ShowOptions(string room, string[] options)
    {
        optionsRoom = room;
        optionsPending = true;

        foreach (Transform child in optionsContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < options.Length; i++)
        {
            OptionView opt = Instantiate(optionPrefab, optionsContainer);
            opt.Setup(options[i], i, optionsGroup);
            opt.toggle.isOn = (i == 0);
        }

        RefreshOptionsVisibility();
    }

    public int GetSelectedIndex()
    {
        foreach (OptionView opt in optionsContainer.GetComponentsInChildren<OptionView>())
            if (opt.toggle.isOn) return opt.index;
        return -1;
    }

    public bool sendPressed;

    public void HideOptions()
    {
        optionsPending = false;
        foreach (Transform child in optionsContainer)
            Destroy(child.gameObject);
        RefreshOptionsVisibility();
    }

    public void OnSendClicked()
    {
        if (optionsPending && currentRoom == optionsRoom) sendPressed = true;
    }

    public GameObject optionsView;   // drag OptionsBody here
    string optionsRoom = "";
    bool optionsPending;

    void RefreshOptionsVisibility()
    {
        optionsView.SetActive(optionsPending && currentRoom == optionsRoom);
    }

    // temporary, just to test
    // public void TestSend()
    // {
    //     Debug.Log("Send clicked!");
    //     AddMessage("MC", "00:00 AM", "here is the text message");
    //     ShowOptions(new string[] { "Text 1...", "Text 2...", "Text 3..." });
    // }
}
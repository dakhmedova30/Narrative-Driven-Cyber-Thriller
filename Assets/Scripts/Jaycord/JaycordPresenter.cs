#nullable enable
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Yarn.Unity;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Shows Yarn dialogue as a Jaycord chat.
///
///   Max: hello        -> "Max is typing…", then Max's bubble (automatic)
///   Leo: hi           -> the player "types" it: any key lights up the text, ENTER sends it
///   -> option         -> the player picks a reply (click / 1-9), then types it the same way
///   System: ...       -> system message
///   Max: look #link   -> Max's bubble + a link card; the chat waits until OPEN LINK is pressed
///
/// Every contact has its own history. Clicking a contact in the chat list shows their history.
/// Messages for a chat that isn't on screen are stored there and counted as unread.
///
/// Add this to the DialogueRunner's "Dialogue Presenters" list.
/// </summary>
public class JaycordPresenter : DialoguePresenterBase
{
    public enum ReplyMode
    {
        [Tooltip("Player presses any keys to type the line, then ENTER to send.")]
        TypeAndPressEnter,
        [Tooltip("The line types itself and sends automatically (handy for testing the story).")]
        Automatic,
    }

    [Header("Speakers")]
    [Tooltip("Lines from this character are the player's own messages.")]
    [SerializeField] private string playerName = "Leo";
    [Tooltip("Lines from these characters (or with no character) are system messages.")]
    [SerializeField] private string[] systemSpeakers = { "System" };

    [Header("App shell (chat list, header, profile)")]
    [SerializeField] private JaycordApp? app;

    [Header("Message list")]
    [SerializeField] private ScrollRect scrollRect = null!;
    [SerializeField] private RectTransform messageContainer = null!;
    [SerializeField] private JaycordTypingIndicator typingIndicator = null!;

    [Header("Prefabs")]
    [SerializeField] private JaycordMessageView selfMessagePrefab = null!;
    [SerializeField] private JaycordMessageView otherMessagePrefab = null!;
    [SerializeField] private JaycordMessageView systemMessagePrefab = null!;
    [SerializeField] private JaycordDivider dividerPrefab = null!;
    [SerializeField] private JaycordLinkCard linkCardPrefab = null!;
    [SerializeField] private JaycordReplyOption replyOptionPrefab = null!;

    [Header("Bottom bar (reply choices replace the input bar while choosing)")]
    [SerializeField] private GameObject inputBar = null!;
    [SerializeField] private TMP_Text inputText = null!;
    [SerializeField] private TMP_Text? inputHint;
    [SerializeField] private string inputPlaceholder = "> enter message_";
    [SerializeField] private GameObject replyPanel = null!;
    [SerializeField] private RectTransform replyContainer = null!;

    [Header("Player replies")]
    [SerializeField] private ReplyMode replyMode = ReplyMode.TypeAndPressEnter;
    [Tooltip("How many characters light up per key press.")]
    [SerializeField, Range(1, 10)] private int charactersPerKey = 3;

    [Header("Link card (#link)")]
    [SerializeField] private string linkTitle = "TRANSMIGRATION LINK";
    [SerializeField] private string linkUrl = "jx://transmigrate/0x7F3A";
    [Tooltip("If on, the chat pauses until the player presses OPEN LINK.")]
    [SerializeField] private bool linkWaitsForClick = true;
    public UnityEvent<string> onLinkOpened = new UnityEvent<string>();

    [Header("Timing (seconds)")]
    [Tooltip("Global speed multiplier for the other side. 2 = twice as fast.")]
    [SerializeField, Range(0.25f, 5f)] private float speed = 1f;
    [SerializeField] private float typingBase = 0.4f;
    [SerializeField] private float typingPerCharacter = 0.035f;
    [SerializeField] private float typingMin = 0.6f;
    [SerializeField] private float typingMax = 2.5f;
    [Tooltip("Only used in Automatic reply mode.")]
    [SerializeField] private float playerCharactersPerSecond = 45f;
    [SerializeField] private float pauseAfterMessage = 0.35f;

    [Header("Layout")]
    [Tooltip("Extra space when the sender changes (Figma: 8px inside a group, 35px between groups).")]
    [SerializeField] private float groupGap = 27f;

    private readonly List<JaycordReplyOption> activeOptions = new List<JaycordReplyOption>();
    private readonly List<JaycordRecord> scratchHistory = new List<JaycordRecord>(); // used when there is no JaycordApp
    private bool waitingForChoice;
    private string? lastSender;     // for the gap between message groups
    private string? activeChat;     // the conversation Yarn is currently writing into
    private string? typingSpeaker;  // who is "typing…" in the active chat right now
    private int keysPressed;        // text-input events since last frame (TypeAndPressEnter)

    // ------------------------------------------------------------------
    // Public API (used by JaycordCommands and the chat list)
    // ------------------------------------------------------------------

    /// <summary>Yarn: &lt;&lt;jaycord_open NAME&gt;&gt; — continue the story in this chat and show it.</summary>
    public void OpenConversation(string contactName)
    {
        activeChat = contactName;
        if (app != null) app.GetOrAdd(contactName);
        ShowConversation(contactName);
    }

    /// <summary>Show a contact's history (chat-list click). Does not change where Yarn writes.</summary>
    public void ShowConversation(string contactName)
    {
        if (app != null) app.Select(contactName);
        Rerender();
    }

    public void SetStatus(string status)
    {
        if (app != null && activeChat != null) app.SetStatus(activeChat, status);
    }

    public void AddDivider(string label) => Append(new JaycordRecord { kind = JaycordRecord.Kind.Date, text = label });

    /// <summary>Yarn: &lt;&lt;jaycord_clear&gt;&gt; — empty the active chat's history.</summary>
    public void ClearMessages()
    {
        ActiveHistory().Clear();
        if (IsViewingActiveChat) Rerender();
    }

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------

    private void OnEnable()
    {
        if (app != null) app.ContactClicked += ShowConversation;
    }

    private void OnDisable()
    {
        if (app != null) app.ContactClicked -= ShowConversation;
    }

    private void Start()
    {
        if (app != null && app.Current != null) Rerender();
        ResetInputBar();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        // Number keys 1–9 pick a reply.
        if (!waitingForChoice || Keyboard.current == null) return;
        for (int i = 0; i < activeOptions.Count && i < 9; i++)
        {
            var key = (Key)((int)Key.Digit1 + i);
            if (Keyboard.current[key].wasPressedThisFrame)
            {
                activeOptions[i].Select();
                break;
            }
        }
#endif
    }

    // ------------------------------------------------------------------
    // Yarn presenter
    // ------------------------------------------------------------------

    public override YarnTask OnDialogueStartedAsync()
    {
        replyPanel.SetActive(false);
        inputBar.SetActive(true);
        typingIndicator.Hide();
        ResetInputBar();
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        replyPanel.SetActive(false);
        inputBar.SetActive(true);
        typingIndicator.Hide();
        typingSpeaker = null;
        waitingForChoice = false;
        ResetInputBar();
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        if (activeChat == null && app != null && app.Current != null) activeChat = app.Current.name;

        string speaker = line.CharacterName ?? "";
        string text = line.TextWithoutCharacterName.Text.Trim();

        if (text.Length > 0)
        {
            if (speaker.Length == 0 || systemSpeakers.Contains(speaker))
            {
                Append(Message("System", text));
            }
            else if (speaker == playerName)
            {
                await PlayerSends(text);
                Append(Message(playerName, text));
            }
            else
            {
                // The other side types for a while, then the message arrives (even if you're in another chat).
                typingSpeaker = speaker;
                if (IsViewingActiveChat) ShowTypingIndicator();
                await Wait(Mathf.Clamp(typingBase + text.Length * typingPerCharacter, typingMin, typingMax));
                typingSpeaker = null;
                typingIndicator.Hide();
                Append(Message(speaker, text));
            }
        }

        if (HasTag(line, "link"))
        {
            var record = new JaycordRecord { kind = JaycordRecord.Kind.Link, sender = speaker };
            Append(record);
            if (linkWaitsForClick)
            {
                while (!record.linkOpened) await YarnTask.Yield();
                onLinkOpened.Invoke(linkUrl);
            }
        }

        await Wait(pauseAfterMessage);
    }

    public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, LineCancellationToken token)
    {
        if (!options.Any(o => o.IsAvailable)) return null;
        if (activeChat == null && app != null && app.Current != null) activeChat = app.Current.name;

        ClearOptions();
        DialogueOption? result = null;
        bool done = false;

        for (int i = 0; i < options.Length; i++)
        {
            DialogueOption option = options[i];
            var view = Instantiate(replyOptionPrefab, replyContainer);
            view.Setup(i + 1, option.Line.TextWithoutCharacterName.Text.Trim(), option.IsAvailable, () =>
            {
                if (done) return;
                result = option;
                done = true;
            });
            activeOptions.Add(view);
        }

        // If the dialogue gets stopped while we wait, give up cleanly.
        using (token.NextContentToken.Register(() => done = true))
        {
            bool wasViewing = false;
            while (!done)
            {
                // The choices only show while the player is looking at this chat.
                bool viewing = IsViewingActiveChat;
                replyPanel.SetActive(viewing);
                inputBar.SetActive(!viewing);
                waitingForChoice = viewing;
                if (viewing && !wasViewing) await ScrollToBottom();
                wasViewing = viewing;
                await YarnTask.Yield();
            }
        }

        waitingForChoice = false;
        foreach (var view in activeOptions) view.Lock();
        await Wait(0.25f); // let the "Selected" state be seen
        replyPanel.SetActive(false);
        inputBar.SetActive(true);
        ClearOptions();

        if (result != null)
        {
            string text = result.Line.TextWithoutCharacterName.Text.Trim();
            await PlayerSends(text);
            Append(Message(playerName, text));
            await Wait(pauseAfterMessage);
        }
        return result;
    }

    // ------------------------------------------------------------------
    // Player typing
    // ------------------------------------------------------------------

    /// <summary>Waits until the player has "typed" the line into the input bar and sent it.</summary>
    private async YarnTask PlayerSends(string text)
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (replyMode == ReplyMode.TypeAndPressEnter && keyboard != null)
        {
            int revealed = 0;
            keysPressed = 0;
            keyboard.onTextInput += OnTextInput;
            try
            {
                while (true)
                {
                    if (!IsViewingActiveChat)
                    {
                        // Player is reading another chat: keep the draft, ignore keys.
                        ResetInputBar();
                        keysPressed = 0;
                        await YarnTask.Yield();
                        continue;
                    }

                    if (keysPressed > 0)
                    {
                        revealed = Mathf.Min(text.Length, revealed + keysPressed * charactersPerKey);
                        keysPressed = 0;
                    }
                    if (keyboard.backspaceKey.wasPressedThisFrame)
                        revealed = Mathf.Max(0, revealed - charactersPerKey);

                    bool complete = revealed >= text.Length;
                    ShowDraft(text, revealed, complete);

                    bool enter = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
                    if (complete && enter) break;

                    await YarnTask.Yield();
                }
            }
            finally
            {
                keyboard.onTextInput -= OnTextInput;
            }
            ResetInputBar();
            return;
        }
#endif
        // Automatic: the line types itself.
        while (!IsViewingActiveChat) await YarnTask.Yield();
        int msPerChar = Mathf.Max(1, Mathf.RoundToInt(1000f / Mathf.Max(1f, playerCharactersPerSecond * speed)));
        for (int i = 1; i <= text.Length; i++)
        {
            ShowDraft(text, i, false);
            await YarnTask.Delay(msPerChar);
        }
        await Wait(0.2f);
        ResetInputBar();
    }

    private void OnTextInput(char c)
    {
        if (!char.IsControl(c)) keysPressed++;
    }

    /// <summary>Typed part bright, the rest dim. Shows the ENTER hint when complete.</summary>
    private void ShowDraft(string text, int revealed, bool complete)
    {
        string typed = text.Substring(0, revealed);
        string rest = text.Substring(revealed);
        string bright = ColorUtility.ToHtmlStringRGB(JaycordTheme.TextPrimary);
        string dim = ColorUtility.ToHtmlStringRGB(JaycordTheme.TextMuted);
        inputText.color = Color.white;
        inputText.text = $"<color=#{dim}>> </color><color=#{bright}><noparse>{typed}</noparse></color>" +
                         $"<color=#{dim}80><noparse>{rest}</noparse></color>";
        if (inputHint != null)
        {
            inputHint.text = complete ? "ENTER ↵" : "TYPE";
            inputHint.color = complete ? JaycordTheme.Cyan : JaycordTheme.TextMuted;
        }
    }

    private void ResetInputBar()
    {
        inputText.text = inputPlaceholder;
        inputText.color = JaycordTheme.TextMuted;
        if (inputHint != null) inputHint.text = "";
    }

    // ------------------------------------------------------------------
    // History & rendering
    // ------------------------------------------------------------------

    private bool IsViewingActiveChat =>
        app == null || activeChat == null || (app.Current != null && app.Current.name == activeChat);

    private List<JaycordRecord> ActiveHistory()
    {
        if (app == null || activeChat == null) return scratchHistory;
        return app.GetOrAdd(activeChat).history;
    }

    private List<JaycordRecord> ViewedHistory()
    {
        if (app == null || app.Current == null) return scratchHistory;
        return app.Current.history;
    }

    private static JaycordRecord Message(string sender, string text) =>
        new JaycordRecord { kind = JaycordRecord.Kind.Message, sender = sender, text = text };

    /// <summary>Adds a record to the active chat; draws it if that chat is on screen, otherwise counts it unread.</summary>
    private void Append(JaycordRecord record)
    {
        ActiveHistory().Add(record);
        bool viewing = IsViewingActiveChat;

        if (viewing)
        {
            bool stick = IsAtBottom();
            Render(record);
            if (stick) ScrollToBottom().Forget();
        }

        if (record.kind == JaycordRecord.Kind.Message && app != null && activeChat != null)
            app.OnMessage(activeChat, record.text, countAsUnread: !viewing);
    }

    /// <summary>Redraws the chat that is on screen from its history.</summary>
    private void Rerender()
    {
        foreach (Transform child in messageContainer.Cast<Transform>().ToList())
        {
            if (child == typingIndicator.transform) continue;
            Destroy(child.gameObject);
        }
        lastSender = null;
        typingIndicator.Hide();

        foreach (var record in ViewedHistory()) Render(record);

        if (typingSpeaker != null && IsViewingActiveChat) ShowTypingIndicator();
        ScrollToBottom().Forget();
    }

    private void Render(JaycordRecord record)
    {
        switch (record.kind)
        {
            case JaycordRecord.Kind.Date:
                Instantiate(dividerPrefab, messageContainer).SetLabel(record.text);
                lastSender = null;
                break;

            case JaycordRecord.Kind.Link:
                StartGroup(record.sender);
                var card = Instantiate(linkCardPrefab, messageContainer);
                card.Setup(linkTitle, linkUrl, unverified: true, alreadyOpened: record.linkOpened);
                card.onOpened = () => record.linkOpened = true;
                break;

            default:
                bool isSystem = record.sender.Length == 0 || systemSpeakers.Contains(record.sender);
                bool isSelf = record.sender == playerName;
                StartGroup(isSystem ? "System" : record.sender);
                var prefab = isSystem ? systemMessagePrefab : isSelf ? selfMessagePrefab : otherMessagePrefab;
                Instantiate(prefab, messageContainer).SetText(record.text);
                break;
        }
        KeepTypingIndicatorLast();
    }

    private void ShowTypingIndicator()
    {
        if (typingSpeaker == null) return;
        bool stick = IsAtBottom();
        StartGroup(typingSpeaker);
        typingIndicator.Show(typingSpeaker);
        if (stick) ScrollToBottom().Forget();
    }

    /// <summary>Adds the bigger gap when a different person starts talking.</summary>
    private void StartGroup(string sender)
    {
        if (lastSender != null && lastSender != sender)
        {
            var spacer = new GameObject("GroupGap", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(messageContainer, false);
            spacer.GetComponent<LayoutElement>().preferredHeight = groupGap;
        }
        lastSender = sender;
        KeepTypingIndicatorLast();
    }

    private void ClearOptions()
    {
        foreach (var view in activeOptions)
        {
            if (view != null) Destroy(view.gameObject);
        }
        activeOptions.Clear();
    }

    private void KeepTypingIndicatorLast()
    {
        if (typingIndicator != null && typingIndicator.gameObject.activeSelf)
            typingIndicator.transform.SetAsLastSibling();
    }

    private bool IsAtBottom()
    {
        var viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
        if (messageContainer.rect.height <= viewport.rect.height + 1f) return true;
        return scrollRect.verticalNormalizedPosition <= 0.02f;
    }

    private async YarnTask ScrollToBottom()
    {
        await YarnTask.Yield(); // wait one frame so the layout includes the new content
        if (scrollRect == null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer);
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private YarnTask Wait(float seconds)
    {
        int ms = Mathf.RoundToInt(seconds * 1000f / Mathf.Max(0.01f, speed));
        return ms <= 0 ? YarnTask.CompletedTask : YarnTask.Delay(ms);
    }

    private static bool HasTag(LocalizedLine line, string tag)
    {
        if (line.Metadata == null) return false;
        return line.Metadata.Any(m => m.TrimStart('#').Equals(tag, System.StringComparison.OrdinalIgnoreCase));
    }
}

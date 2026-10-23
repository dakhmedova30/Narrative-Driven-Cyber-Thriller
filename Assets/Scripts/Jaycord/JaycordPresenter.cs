#nullable enable
using System;
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
/// Shows Yarn dialogue as a Jaycord chat instead of a dialogue box.
///
///   Max: hello                -> "Max is typing…", then Max's bubble (left)
///   Leo: hi                   -> typed into the input bar, then Leo's bubble (right)
///   System: Message failed.   -> centered system message
///   Max: look #link           -> Max's bubble + a link card (waits for OPEN LINK)
///   -> option                 -> reply buttons; unmet <<if>> shows as LOCKED
///
/// Add this to the DialogueRunner's "Dialogue Presenters" list.
/// </summary>
public class JaycordPresenter : DialoguePresenterBase
{
    [Header("Speakers")]
    [Tooltip("Lines from this character are shown as the player's own messages.")]
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

    [Header("Bottom bar: the reply choices replace the input bar while the player chooses")]
    [SerializeField] private GameObject inputBar = null!;
    [SerializeField] private TMP_Text inputText = null!;
    [SerializeField] private string inputPlaceholder = "> enter message_";
    [SerializeField] private GameObject replyPanel = null!;
    [SerializeField] private RectTransform replyContainer = null!;

    [Header("Link card (#link)")]
    [SerializeField] private string linkTitle = "TRANSMIGRATION LINK";
    [SerializeField] private string linkUrl = "jx://transmigrate/0x7F3A";
    [Tooltip("If on, the chat pauses until the player presses OPEN LINK.")]
    [SerializeField] private bool linkWaitsForClick = true;
    public UnityEvent<string> onLinkOpened = new UnityEvent<string>();

    [Header("Timing (seconds)")]
    [Tooltip("Global speed multiplier. 2 = twice as fast.")]
    [SerializeField, Range(0.25f, 5f)] private float speed = 1f;
    [SerializeField] private float typingBase = 0.4f;
    [SerializeField] private float typingPerCharacter = 0.035f;
    [SerializeField] private float typingMin = 0.6f;
    [SerializeField] private float typingMax = 2.5f;
    [SerializeField] private float playerCharactersPerSecond = 45f;
    [SerializeField] private float pauseAfterMessage = 0.35f;

    [Header("Layout")]
    [Tooltip("Extra space when the sender changes (Figma: 8px inside a group, 35px between groups).")]
    [SerializeField] private float groupGap = 27f;

    private readonly List<JaycordReplyOption> activeOptions = new List<JaycordReplyOption>();
    private bool waitingForChoice;
    private string? lastSender;

    // ------------------------------------------------------------------
    // Public API (used by JaycordCommands)
    // ------------------------------------------------------------------

    /// <summary>Switch to a conversation: sets the header and clears old messages.</summary>
    public void OpenConversation(string contactName)
    {
        ClearMessages();
        if (app != null) app.Select(contactName);
    }

    public void SetStatus(string status)
    {
        if (app != null) app.SetStatus(status);
    }

    public void AddDivider(string label)
    {
        var divider = Instantiate(dividerPrefab, messageContainer);
        divider.SetLabel(label);
        lastSender = null;
        KeepTypingIndicatorLast();
    }

    public void ClearMessages()
    {
        var toRemove = new List<GameObject>();
        foreach (Transform child in messageContainer)
        {
            if (typingIndicator != null && child == typingIndicator.transform) continue;
            toRemove.Add(child.gameObject);
        }
        foreach (var go in toRemove) Destroy(go);
        lastSender = null;
    }

    // ------------------------------------------------------------------
    // Yarn presenter
    // ------------------------------------------------------------------

    public override YarnTask OnDialogueStartedAsync()
    {
        replyPanel.SetActive(false);
        inputBar.SetActive(true);
        typingIndicator.Hide();
        ResetInput();
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        replyPanel.SetActive(false);
        inputBar.SetActive(true);
        typingIndicator.Hide();
        waitingForChoice = false;
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        string speaker = line.CharacterName ?? "";
        string text = line.TextWithoutCharacterName.Text.Trim();

        if (text.Length > 0)
        {
            if (speaker.Length == 0 || systemSpeakers.Contains(speaker))
            {
                await AddMessage(systemMessagePrefab, text, "System");
            }
            else if (speaker == playerName)
            {
                await TypeIntoInputBar(text);
                await AddMessage(selfMessagePrefab, text, playerName);
            }
            else
            {
                bool stick = IsAtBottom();
                StartGroup(speaker);
                typingIndicator.Show(speaker);
                if (stick) await ScrollToBottom();
                await Wait(Mathf.Clamp(typingBase + text.Length * typingPerCharacter, typingMin, typingMax));
                typingIndicator.Hide();
                await AddMessage(otherMessagePrefab, text, speaker);
            }
        }

        if (HasTag(line, "link"))
        {
            var card = Instantiate(linkCardPrefab, messageContainer);
            card.Setup(linkTitle, linkUrl, unverified: true);
            await ScrollToBottom();
            if (linkWaitsForClick)
            {
                await card.WaitUntilOpened();
                onLinkOpened.Invoke(linkUrl);
            }
        }

        await Wait(pauseAfterMessage);
    }

    public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, LineCancellationToken token)
    {
        if (!options.Any(o => o.IsAvailable)) return null;

        ClearOptions();
        var chosen = new YarnTaskCompletionSource<DialogueOption?>();

        for (int i = 0; i < options.Length; i++)
        {
            DialogueOption option = options[i];
            var view = Instantiate(replyOptionPrefab, replyContainer);
            view.Setup(i + 1, option.Line.TextWithoutCharacterName.Text.Trim(), option.IsAvailable,
                       () => chosen.TrySetResult(option));
            activeOptions.Add(view);
        }

        // If the dialogue gets stopped while we wait, give up cleanly.
        using (token.NextContentToken.Register(() => chosen.TrySetResult(null)))
        {
            inputBar.SetActive(false);
            replyPanel.SetActive(true);
            waitingForChoice = true;
            await ScrollToBottom();

            DialogueOption? result = await chosen.Task;
            waitingForChoice = false;

            foreach (var view in activeOptions) view.Lock();
            await Wait(0.25f); // let the "Selected" state be seen
            replyPanel.SetActive(false);
            inputBar.SetActive(true);
            ClearOptions();

            if (result != null)
            {
                string text = result.Line.TextWithoutCharacterName.Text.Trim();
                await TypeIntoInputBar(text);
                await AddMessage(selfMessagePrefab, text, playerName);
                await Wait(pauseAfterMessage);
            }
            return result;
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

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

    private async YarnTask AddMessage(JaycordMessageView prefab, string text, string sender)
    {
        bool stick = IsAtBottom();
        StartGroup(sender);
        var view = Instantiate(prefab, messageContainer);
        view.SetText(text);
        KeepTypingIndicatorLast();
        if (app != null) app.OnMessage(text);
        if (stick) await ScrollToBottom();
    }

    /// <summary>Adds the bigger gap when a different person starts talking.</summary>
    private void StartGroup(string sender)
    {
        if (lastSender != null && lastSender != sender)
        {
            var spacer = new GameObject("GroupGap", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(messageContainer, false);
            spacer.GetComponent<LayoutElement>().preferredHeight = groupGap;
            KeepTypingIndicatorLast();
        }
        lastSender = sender;
    }

    private async YarnTask TypeIntoInputBar(string text)
    {
        inputText.color = JaycordTheme.TextPrimary;
        int msPerChar = Mathf.Max(1, Mathf.RoundToInt(1000f / Mathf.Max(1f, playerCharactersPerSecond * speed)));
        for (int i = 1; i <= text.Length; i++)
        {
            inputText.text = "> " + text.Substring(0, i) + "_";
            await YarnTask.Delay(msPerChar);
        }
        await Wait(0.2f);
        ResetInput();
    }

    private void ResetInput()
    {
        inputText.text = inputPlaceholder;
        inputText.color = JaycordTheme.TextMuted;
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
        await YarnTask.Yield(); // wait one frame so the layout includes the new message
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
        return line.Metadata.Any(m => m.TrimStart('#').Equals(tag, StringComparison.OrdinalIgnoreCase));
    }
}

using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Jaycord-specific Yarn commands:
///   <<jaycord_open Max>>                       open the chat with Max (clears old messages)
///   <<jaycord_status "ACTIVE 20M AGO">>        text under the name in the header
///   <<jaycord_divider "NOV 30, 2026 · 11:42 PM">>  date separator
///   <<jaycord_clear>>                          remove all messages
/// Put this next to the DialogueRunner that runs Jaycord nodes.
/// </summary>
public class JaycordCommands : MonoBehaviour
{
    [SerializeField] private DialogueRunner runner;
    [SerializeField] private JaycordPresenter presenter;

    private void Awake()
    {
        if (runner == null) runner = GetComponent<DialogueRunner>();
        if (runner == null || presenter == null)
        {
            Debug.LogError("[JaycordCommands] Assign both the DialogueRunner and the JaycordPresenter.");
            return;
        }

        runner.AddCommandHandler<string>("jaycord_open", presenter.OpenConversation);
        runner.AddCommandHandler<string>("jaycord_status", presenter.SetStatus);
        runner.AddCommandHandler<string>("jaycord_divider", presenter.AddDivider);
        runner.AddCommandHandler("jaycord_clear", presenter.ClearMessages);
    }
}

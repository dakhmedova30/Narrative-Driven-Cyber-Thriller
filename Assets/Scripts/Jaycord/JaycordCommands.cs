using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Jaycord-specific Yarn commands:
///   <<jaycord_open Max>>                       open the chat with Max (clears old messages)
///   <<jaycord_status "ACTIVE 20M AGO">>        text under the name in the header
///   <<jaycord_divider "NOV 30, 2026 · 11:42 PM">>  date separator
///   <<jaycord_clear>>                          remove all messages
///   <<jaycord_link "STOP THEM" "jx://transmigrate/0x7F3A">>
///                                              link card; the story waits until OPEN LINK is pressed
///
/// Profile card (any contact):
///   <<jaycord_trust Max 70>>                   set trust (0-100)
///   <<jaycord_trust_add Max -10>>              change trust by an amount
///   <<jaycord_dossier Max STATUS "Missing">>   change (or add) a dossier line
///   <<jaycord_tag_add Max ghost>>   <<jaycord_tag_remove Max hacker>>
///   <<jaycord_level Max "LV.08">>   <<jaycord_handle Max "@max  ·  ID 0x7F3A">>
///   <<jaycord_presence Max offline>>           online / idle / offline
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
        runner.AddCommandHandler<string, string>("jaycord_link", presenter.PostLink);

        // Profile card (right panel) — can change any contact, not only the open chat
        var app = presenter.App;
        if (app == null) return;
        runner.AddCommandHandler<string, int>("jaycord_trust", app.SetTrust);
        runner.AddCommandHandler<string, int>("jaycord_trust_add", app.AddTrust);
        runner.AddCommandHandler<string, string, string>("jaycord_dossier", app.SetDossier);
        runner.AddCommandHandler<string, string>("jaycord_tag_add", app.AddTag);
        runner.AddCommandHandler<string, string>("jaycord_tag_remove", app.RemoveTag);
        runner.AddCommandHandler<string, string>("jaycord_level", app.SetLevel);
        runner.AddCommandHandler<string, string>("jaycord_handle", app.SetHandle);
        runner.AddCommandHandler<string, string>("jaycord_presence", app.SetPresence);
    }
}

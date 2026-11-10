using System;
using System.Linq;
using UnityEngine;
using Yarn.Unity;

public class ChatPresenter : DialoguePresenterBase
{
    public ChatAppRouter router;
    public string playerName = "Skye";
    public int npcDelayMs = 900;

    string lastNpc = "";

    public override YarnTask OnDialogueStartedAsync() { return YarnTask.CompletedTask; }
    public override YarnTask OnDialogueCompleteAsync() { return YarnTask.CompletedTask; }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        var chat = router.Current != null ? router.Current.chat : null;
        string who = line.CharacterName ?? "";
        string text = line.TextWithoutCharacterName.Text;

        if (chat == null)   // not an MHM node: log it so the story keeps moving
        {
            Debug.Log($"[no chat app] {who}: {text}");
            await YarnTask.Delay(600);
            return;
        }

        bool isPlayer = (who == playerName);
        if (!isPlayer) { lastNpc = who; await YarnTask.Delay(npcDelayMs); }

        chat.AddMessage(lastNpc, who, DateTime.Now.ToString("hh:mm tt"), text, isPlayer);
        await YarnTask.Delay(400);
    }

    public override async YarnTask<DialogueOption?> RunOptionsAsync(
        DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
    {
        var chat = router.Current != null ? router.Current.chat : null;
        var options = dialogueOptions.Where(o => o.IsAvailable).ToArray();

        if (chat == null)   // temporary: auto-pick the first option
        {
            Debug.Log("[no chat app] auto-picked: " + options[0].Line.TextWithoutCharacterName.Text);
            return options[0];
        }

        chat.sendPressed = false;
        chat.ShowOptions(lastNpc, options.Select(o => o.Line.TextWithoutCharacterName.Text).ToArray());
        await YarnTask.WaitUntil(() => chat.sendPressed);

        int picked = chat.GetSelectedIndex();
        chat.HideOptions();
        chat.sendPressed = false;

        chat.AddMessage(lastNpc, playerName, DateTime.Now.ToString("hh:mm tt"),
                        options[picked].Line.TextWithoutCharacterName.Text, true);
        return options[picked];
    }
}
using System;
using System.Linq;
using UnityEngine;
using Yarn.Unity;

public class ChatPresenter : DialoguePresenterBase
{
    public ChatAppRouter router;
    public UserProfileView profile;           // drag the user profile here
    string PlayerName => profile != null ? profile.username : "Sage";
    public int npcDelayMs = 900;
    public string linkUrl = "jx://transmigrate/0x7F3A";

    string lastNpc = "";

    public override YarnTask OnDialogueStartedAsync() { return YarnTask.CompletedTask; }
    public override YarnTask OnDialogueCompleteAsync() { return YarnTask.CompletedTask; }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        var chat = router.Current != null ? router.Current.chat : null;
        string who = line.CharacterName ?? "";
        string text = line.TextWithoutCharacterName.Text;

        bool isLink = line.Metadata != null &&
            line.Metadata.Any(m => m.TrimStart('#').Equals("link", StringComparison.OrdinalIgnoreCase));
        if (isLink) text = $"{text} <u><color=#3b5bdb>{linkUrl}</color></u>";

        if (chat == null)   // not an MHM node: log it so the story keeps moving
        {
            Debug.Log($"[no chat app] {who}: {text}");
            await DialoguePlayback.Wait(0.6f);
            return;
        }

        bool isPlayer = (who == PlayerName);
        if (!isPlayer) { lastNpc = who; await DialoguePlayback.Wait(npcDelayMs / 1000f); }

        chat.AddMessage(lastNpc, who, DateTime.Now.ToString("hh:mm tt"), text, isPlayer);
        await DialoguePlayback.Wait(0.4f);
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

        chat.AddMessage(lastNpc, PlayerName, DateTime.Now.ToString("hh:mm tt"),
                        options[picked].Line.TextWithoutCharacterName.Text, true);
        return options[picked];
    }
}
using System;
using System.Linq;
using UnityEngine;
using Yarn.Unity;

public class ChatPresenter : DialoguePresenterBase
{
    public ChatManager chat;
    public string playerName = "Skye";
    public int npcDelayMs = 900;
    public int mcDelayMs = 900;

    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        string who = line.CharacterName ?? "";
        string text = line.TextWithoutCharacterName.Text;

        // NPCs "type" for a moment before their message appears
        if (who != playerName) await YarnTask.Delay(npcDelayMs);
        else await YarnTask.Delay(mcDelayMs);

        chat.AddMessage(who, DateTime.Now.ToString("hh:mm tt"), text, who == playerName);
        await YarnTask.Delay(400);   // short pause, then Yarn moves on by itself
    }

    public override async YarnTask<DialogueOption?> RunOptionsAsync(
        DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
    {
        // options whose <<if>> condition is false still arrive, marked unavailable
        var options = dialogueOptions.Where(o => o.IsAvailable).ToArray();

        chat.sendPressed = false;
        chat.ShowOptions(options.Select(o => o.Line.TextWithoutCharacterName.Text).ToArray());

        await YarnTask.WaitUntil(() => chat.sendPressed);

        int picked = chat.GetSelectedIndex();
        chat.HideOptions();
        chat.sendPressed = false;

        // the chosen option shows up as the player's own message
        chat.AddMessage(playerName, DateTime.Now.ToString("hh:mm tt"),
                        options[picked].Line.TextWithoutCharacterName.Text, true);
        return options[picked];
    }
}
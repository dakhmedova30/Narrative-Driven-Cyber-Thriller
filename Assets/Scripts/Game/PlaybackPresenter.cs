#nullable enable
using System.Linq;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Invisible presenter that GameSession adds to every DialogueRunner automatically. It shows nothing; it:
///   - marks lines as read / blocks skipping unread lines (read-only skip mode)
///   - stops the Skip button at choices
///   - face-mode nodes: advances lines for Auto play and Skip
/// Do not add it by hand.
/// </summary>
public class PlaybackPresenter : DialoguePresenterBase
{
    private DialogueRunner? runner;

    public void Init(DialogueRunner owner) => runner = owner;

    public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;

    public override YarnTask OnDialogueCompleteAsync()
    {
        DialoguePlayback.EndDialogue();
        ProgressProfile.Flush();
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        bool unread = !ProgressProfile.IsRead(line.TextID);
        DialoguePlayback.BeginLine(unread);
        ProgressProfile.MarkRead(line.TextID);

        if (runner == null || !GameSession.IsFaceNode(runner)) return;

        // Face mode: the LinePresenter waits for a click. Auto / Skip click for the player.
        LinePresenter? lp = runner.DialoguePresenters.OfType<LinePresenter>().FirstOrDefault(p => p != null && p.isActiveAndEnabled);
        string text = line.TextWithoutCharacterName.Text ?? "";
        float cps = Mathf.Max(1f, (lp != null ? lp.lettersPerSecond : 60) * GameSettings.TextSpeed);
        float fade = lp != null && lp.useFadeEffect ? lp.fadeUpDuration : 0f;
        float needed = fade + text.Length / cps + GameSettings.AutoDelay; // seconds at 1x

        float autoTimer = 0f, skipTimer = 0f;
        while (!token.NextContentToken.IsCancellationRequested)
        {
            if (DialoguePlayback.IsSkipping)
            {
                skipTimer += Time.unscaledDeltaTime;
                if (skipTimer >= 0.06f) { runner.RequestNextLine(); return; }
            }
            else if (DialoguePlayback.Auto && !DialoguePlayback.Paused)
            {
                autoTimer += Time.unscaledDeltaTime * DialoguePlayback.Speed;
                if (autoTimer >= needed) { runner.RequestNextLine(); return; }
            }
            await YarnTask.Yield();
        }
    }

    public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
    {
        DialoguePlayback.StopSkip("Skip stopped: choice");
        return YarnTask<DialogueOption?>.FromResult(null); // the real options presenter picks
    }
}

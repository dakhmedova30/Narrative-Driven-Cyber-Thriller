using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Yarn commands for player movement, plus optional auto-lock while dialogue runs.
///
///   <<lock_move>>     player can't walk (keyboard and click both ignored)
///   <<unlock_move>>   player can walk again
///
/// "Lock While Dialogue Runs" (on by default): movement is locked automatically when
/// the DialogueRunner starts a node and unlocked when the dialogue finishes, so most
/// scenes need no commands at all. Use <<lock_move>> for moments outside dialogue
/// (e.g. keep the player still after a line ends until something else happens),
/// or turn the auto-lock off for a runner whose dialogue should let the player walk.
///
/// Setup: put this on the same GameObject as the DialogueRunner (like YarnCommands).
/// Player is found automatically via PlayerMover.Instance if left empty.
/// </summary>
public class PlayerCommands : MonoBehaviour
{
    private const string YarnLock = "yarn";
    private const string DialogueLock = "dialogue";

    [SerializeField] private DialogueRunner runner;
    [Tooltip("Uses PlayerMover.Instance if empty.")]
    [SerializeField] private PlayerMover player;
    [SerializeField] private bool lockWhileDialogueRuns = true;

    private PlayerMover Player => player != null ? player : PlayerMover.Instance;

    private void Awake()
    {
        if (runner == null) runner = GetComponent<DialogueRunner>();
        if (runner == null)
        {
            Debug.LogError("[PlayerCommands] No DialogueRunner assigned or found on this GameObject.");
            return;
        }

        runner.AddCommandHandler("lock_move", LockMove);       // <<lock_move>>
        runner.AddCommandHandler("unlock_move", UnlockMove);   // <<unlock_move>>
    }

    private void OnEnable()
    {
        if (runner == null || !lockWhileDialogueRuns) return;
        runner.onDialogueStart?.AddListener(OnDialogueStart);
        runner.onDialogueComplete?.AddListener(OnDialogueComplete);
    }

    private void OnDisable()
    {
        if (runner == null) return;
        runner.onDialogueStart?.RemoveListener(OnDialogueStart);
        runner.onDialogueComplete?.RemoveListener(OnDialogueComplete);
    }

    private void LockMove()
    {
        if (Player != null) Player.Lock(YarnLock);
        else Debug.LogWarning("[Yarn] <<lock_move>>: there is no PlayerMover in this scene.");
    }

    private void UnlockMove()
    {
        if (Player != null) Player.Unlock(YarnLock);
        else Debug.LogWarning("[Yarn] <<unlock_move>>: there is no PlayerMover in this scene.");
    }

    private void OnDialogueStart()
    {
        if (Player != null) Player.Lock(DialogueLock);
    }

    private void OnDialogueComplete()
    {
        if (Player != null) Player.Unlock(DialogueLock);
    }
}

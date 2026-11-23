using DopamineMeter;
using UnityEngine;

/// <summary>Project-specific adapter. Included only in DopamineMeterForGame.prefab.</summary>
[RequireComponent(typeof(MeterService))]
public sealed class MeterGameBridge : MonoBehaviour
{
    MeterService meter;
    void Awake() { meter = GetComponent<MeterService>(); meter.playerHasControl = false; }
    void Update()
    {
        var session = GameSession.Instance;
        if (meter == null || meter != MeterService.Instance) return;
        bool blocked = session == null || session.IsOnTitle || session.IsPaused || session.IsBusy;
        meter.SetPaused(blocked);
        // A dialogue blocks timed growth, NOT an explicit +5/-10 story event.
        var player = PlayerMover.Instance;
        var input = player != null ? player.GetComponent<PlayerInputHandler>() : null;
        meter.SetPlayerControl(!blocked && !session.AnyDialogueRunning &&
            player != null && player.isActiveAndEnabled && player.CanMove &&
            input != null && input.isActiveAndEnabled && !GameInput.IsRebinding);
    }
    void OnDisable()
    {
        if (meter != null && meter == MeterService.Instance) { meter.SetPlayerControl(false); meter.SetPaused(false); }
    }
}

# Player Movement (2.5D, left/right only)

Scripts: `Assets/Scripts/Player/`

| Script | Put it on | What it does |
|---|---|---|
| `PlayerMover` | Player GameObject | Moves along world X only. Bounds, facing (sprite flipX), animator params, lock API. |
| `PlayerInputHandler` | Player GameObject (same as above) | A/D, ←/→ (hold), left click / tap = walk to clicked X. Keyboard cancels click target. Clicks on UI are ignored. |
| `ClickMarker` | Its own empty GameObject (not a child of the player) | Ring on the floor at the click target. Fades out on arrival. |
| `PlayerCommands` | Same GameObject as the `DialogueRunner` | `<<lock_move>>` / `<<unlock_move>>`, and auto-locks movement while dialogue runs. |

## Setup
1. Player GameObject: SpriteRenderer + `PlayerMover` + `PlayerInputHandler`.
   - Sprite pivot at the feet (Bottom) makes floor placement easier.
   - Untick **Sprite Faces Right** if the art looks left.
2. Select the player → yellow lines in the Scene view = **Min X / Max X**. Line them up with the floor.
3. Scene needs an **EventSystem** (with Input System UI Input Module) so clicks on UI are ignored.
4. Camera: tag it `MainCamera`, or drag it into **World Camera**.
5. Empty GameObject `ClickMarker` + `ClickMarker` script. Tune **Y Offset** so it sits on the floor.
6. `PlayerCommands` next to the `DialogueRunner`.

## Animator (optional)
Float `Speed` and/or Bool `IsWalking`. Missing parameters are skipped.

## From other scripts
```csharp
PlayerMover.Instance.MoveTo(3.5f);     // walk to x = 3.5
PlayerMover.Instance.Stop();
PlayerMover.Instance.Teleport(-2f);    // e.g. on scene load
PlayerMover.Instance.SetFacing(false); // face left
PlayerMover.Instance.Lock("phone");    // each reason is separate
PlayerMover.Instance.Unlock("phone");
PlayerMover.Instance.OnArrived += ...; // for click-to-interact later
```

# Camera Follow (Cinemachine 3.1.7)

Package added to `Packages/manifest.json`: `"com.unity.cinemachine": "3.1.7"` (Unity installs it on next open).

Scripts: `Assets/Scripts/Camera/`

| Script | What it does |
|---|---|
| `CameraXLimits` | Cinemachine extension. Stops the camera at the scene's left/right edges (red lines in the Scene view). |
| `Editor/PlayerCameraBuilder` | Menu **Tools > Camera > Create Player Follow Camera**: sets up everything in one click. |

## Setup
1. Player with `PlayerMover` already in the scene, Main Camera placed at the angle you want.
2. **Tools > Camera > Create Player Follow Camera**. It:
   - adds `CinemachineBrain` to Main Camera (from now on, move the camera through "CM Player Follow", not Main Camera)
   - creates **CM Player Follow** (CinemachineCamera, follows the player, Priority 10)
   - Position Composer: dead zone = middle 30% of the screen width, damping 0.6 s
   - Camera X Limits: starts at player Min X − 2 / Max X + 2
3. Set **Left Edge / Right Edge** on Camera X Limits to the ends of the background art.

## Tuning (on CM Player Follow → Position Composer)
- **Dead Zone → Size X**: bigger = camera moves less. 0 = player always centred.
- **Damping X**: how slowly the camera catches up. 0 = locked to the player.
- **Screen Position Y**: where the player sits vertically (Cinemachine: + is DOWN).
- **Camera Distance**: zoom for perspective cameras.

## Story shots (later)
Make another CinemachineCamera for the shot and give it a higher Priority (or enable it).
The brain blends to it automatically and back to the follow camera when it is disabled.

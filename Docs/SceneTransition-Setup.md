# Scene Transitions (one area = one Unity scene)

Scripts: `Assets/Scripts/Scene/`

| Script | Put it on | What it does |
|---|---|---|
| `SceneTransitioner` | nothing (creates itself) | Fade to black → load scene → place player on spawn → fade in. Player is locked during it. |
| `AreaExit` | empty GameObject at an exit | **Auto**: walk into the zone → go. **ClickArrow**: walk into the zone → arrow appears → click it → go. |
| `SpawnPoint` | empty GameObject where the player appears | Id + facing. One per scene ticked **Is Default**. Only X is used. |
| `SceneCommands` | next to the `DialogueRunner` | Yarn `<<goto SceneName SpawnId>>` (use `default` for the default spawn). Put it last in the node. |
| `Editor/AreaTestSceneBuilder` | — | Menu **Tools > Scenes > Create Area Test Scenes (A ⇄ B)**. |

## Quick test
1. Open `MovementTest` once so it exists and works.
2. **Tools > Scenes > Create Area Test Scenes (A ⇄ B)** → creates `AreaTest_A` / `AreaTest_B` and opens A.
3. Play. A: right edge = automatic, left edge = arrow. B: the other way round.

## Making a real area
1. Scene with the usual player setup (Player, EventSystem, follow camera…).
2. Add the scene to **File > Build Profiles > Scene List** (otherwise loading fails with an error in the Console).
3. Add `SpawnPoint`s for every way in (e.g. `from_street`, `from_office`), tick **Is Default** on one.
4. Add `AreaExit`s: Target Scene = scene name, Target Spawn = spawn Id in that scene.
   - Zone must be inside the player's Min X / Max X.
   - Keep spawns outside exit zones.
5. Switch an exit between Auto and ClickArrow any time with its **Mode** field.

## Rules
- Arriving inside an exit zone never triggers it; the player has to walk out and back in.
- The arrow is UI, so clicking it never makes the player walk.
- Exits and arrows are disabled while the player is locked (dialogue, `<<lock_move>>`, a transition).
- `SceneTransitioner.OnAreaEntered` (spawn id) fires after arriving — hook for "entering this room starts a dialogue" later.

# Game Flow (main story order)

One place for the order of the whole game: **Tools > Game > Open Game Flow**
(asset: `Assets/Resources/GameFlow.asset`, created with Title → Chapter 0 → Street the first time).

| Field | Meaning |
|---|---|
| Name | Shown in save slots as the progress |
| Scene | Scene for this step (dropdown) |
| Spawn Point | SpawnPoint Id; empty = default spawn |
| Start Node | Yarn node started on arrival (replaces the scene's Auto Start); empty = none |
| Next After Node | When this node finishes → next step automatically |

- Step 0 = title screen. **New Game** = step 1. After the last step → back to the title.
- Other ways to go to the next step: `<<next>>` in Yarn, or an **AreaExit** with **Advances Game Flow** ticked.
- Normal AreaExits (not ticked) just move between areas; the step stays the same.
- Yarn variables are carried to the next scene (except new game / title / loading a save).
- Saves remember the step; the slot screen shows the step name.

Inspector buttons: **▶ Play** (start at that step; earlier variables are defaults), **Check**, **Sync Scene List**
(flow scenes first in Build Profiles, other scenes kept after them).

# Save / Load, Dialogue Controls, Pause Menu, Title

Everything is **automatic**: `GameSession` + `GameUI` create themselves when the game starts (any scene).
UI is a **template** built in code (`Assets/Scripts/UI/UIKit.cs` holds colours/sizes) — replace later.

## Quick start
1. **Tools > Game > Create Title Scene** → `Assets/Scenes/Title.unity`, first in the scene list.
   Select *Title Screen* → set **New Game Scene** (default `Home`).
2. Every scene the game can be in must be in **File > Build Profiles > Scene List**.
3. Play from `Title`, or directly from any game scene (pause/save work there too).

## Keys (rebindable in Settings > Controls)
| Action | Default |
|---|---|
| Move left / right | A, ← / D, → |
| Quick save / load | F5 / F9 |
| Pause / back | Esc |
| Skip (hold) | Left Ctrl, Right Ctrl |

## Saves
- `persistentDataPath/saves/`: `slot_1..6`, `quick`, `auto` (`.json` + `.jpg` thumbnail).
- Saved: scene, player X/facing, all Yarn variables, current dialogue node, Jaycord (contacts, trust, dossier, tags, chat history), background, music, ambience, chapter, play time.
- **Saving during a dialogue** stores the state at the **start of the current node**; loading replays that node from the top.
- **Autosave** (auto slot, max once / 2 s, last request always runs): entering an area, every dialogue node start, dialogue end, `<<chapter N>>`, `<<chapter_end N>>`, `<<autosave>>`.
- `persistentDataPath/profile.json` (shared by all slots): read lines + completed chapters.
- Settings live in PlayerPrefs (not in slots).

## Yarn commands
| Command | Effect |
|---|---|
| `<<chapter 1>>` | Current chapter = 1 (shown in saves) + autosave. Put at the start of each chapter. |
| `<<chapter_end 1>>` | Chapter 1 completed → "skip everything" allowed in it + autosave. |
| `<<autosave>>` | Autosave now. |

## Dialogue controls (bar bottom-right while a dialogue runs)
- **Speed** 1x → 2x → 3x. Default in Settings.
- **Skip**: button toggles, Ctrl held skips. Default skips **read text only**; "Everything" (Settings) only in chapters completed once.
  Stops at: choices, Jaycord links, scene changes, dialogue end, unread text (read-only mode).
- **Auto**: only for `face` nodes (shown only then). Delay in Settings.
- Node mode comes from node tags: `face` = click-to-continue; `mhm` / `chat` / `jaycord` = chat style.
  No tag → face mode if the runner uses Yarn's LinePresenter.

## For presenter code
Wait with `await DialoguePlayback.Wait(seconds)` (not `YarnTask.Delay`) so speed / skip / pause apply.
Jaycord and MHM presenters already do.

## Notes
- Yarn template's LineAdvancer uses Esc for "next line"; it is switched off at runtime (Esc = pause).
- Pause sets `Time.timeScale = 0`.
- To save something new: add a field to `SaveData`, read it in `StateCapture.Capture`, restore it in `ApplyEarly`/`ApplyLate`.

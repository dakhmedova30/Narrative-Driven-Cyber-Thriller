# Audio checklist & credits

Every sound is added to the **Sound Library** (`Assets/Audio/SoundLibrary.asset`)
with the **exact name** used in Yarn / code below. File names should match too.

- Short sound effects → `.wav`
- Loops (ambience, music) → `.ogg`, must loop seamlessly
- Record where each file came from and its licence in the **Source / Licence** column
  (CC0 is easiest; CC-BY needs a credit in the game).

## SFX — `Assets/Audio/SFX/` · Yarn `<<sfx NAME>>`

| Done | Name | Where | Description | Source / Licence |
|---|---|---|---|---|
| [ ] | `notification` | Start | 1999 chat app alert, retro and short | |
| [ ] | `static` | C0_TheJump | TV static / glitch, 1–2 s, getting stronger | |
| [ ] | `train_chime` | C0_Subway | chime before a subway announcement | |
| [ ] | `crow_chime` | C0_SubwayCrow | CrowGPT wake sound, clean and techy | |
| [ ] | `train_brake` | C0_SubwayCrow | train braking + doors opening | |
| [ ] | `knock` | C0_Knock | three normal knocks | |
| [ ] | `knock_loud` | C0_Knock | louder, impatient knocking | |
| [ ] | `door_open` | C0_Knock, C0_Outside | apartment door opening | |
| [ ] | `box_open` | C0_Knock | tape ripping, cardboard box opening | |
| [ ] | `paper` | C0_Knock | paper note unfolding | |
| [ ] | `crow_power_on` | C0_Knock | robot boot-up ending in a chime (bigger than `crow_chime`) | |
| [ ] | `robot_whir` | C0_Outside | small cleaning robot motor | |
| [ ] | `message_failed` | C0_PhoneMax | "message not delivered" error tone | |

## Ambience (loops) — `Assets/Audio/Ambience/` · Yarn `<<ambience NAME>>`

| Done | Name | Where | Description | Source / Licence |
|---|---|---|---|---|
| [ ] | `room_ambience_1999` | Start | old PC fan, CRT hum, quiet night | |
| [ ] | `subway_ambience` | C0_TheJump → C0_SubwayCrow | carriage rumble, track rhythm, murmurs | |
| [ ] | `street_ambience_2027` | C0_Outside | future city: traffic, electronic ads | |

## UI — `Assets/Audio/UI/` · used by Jaycord automatically

| Done | Name | When | Description | Source / Licence |
|---|---|---|---|---|
| [ ] | `key_press` | each key while "typing" | very short (~0.05 s). Add 3–4 variations to the same entry | |
| [ ] | `message_send` | ENTER sends a message | | |
| [ ] | `message_receive` | a message arrives | | |
| [ ] | `option_hover` | mouse over a reply option | subtle | |
| [ ] | `option_select` | reply option chosen | | |
| [ ] | `option_locked` | clicking a LOCKED option | "denied" | |
| [ ] | `link_open` | OPEN LINK pressed | can lead into `static` | |

## Music (loops) — `Assets/Audio/Music/` · Yarn `<<music NAME>>`

Not used in the script yet. Ideas: 1999 room (nostalgic), 2027 (uneasy, electronic),
CrowGPT theme (friendly but slightly off), chapter end sting.

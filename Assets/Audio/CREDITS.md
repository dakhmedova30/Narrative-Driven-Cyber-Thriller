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
| [x] | `notification` | Start | 1999 chat app alert, retro and short | Original procedural synthesis; no third-party samples |
| [x] | `static` | C0_TheJump | TV static / glitch, 1–2 s, getting stronger | Original procedural synthesis; no third-party samples |
| [x] | `train_chime` | C0_Subway | chime before a subway announcement | Original procedural synthesis; no third-party samples |
| [x] | `crow_chime` | C0_SubwayCrow | CrowGPT wake sound, clean and techy | Original procedural synthesis; no third-party samples |
| [ ] | `train_brake` | C0_SubwayCrow | train braking + doors opening | |
| [ ] | `knock` | C0_Knock | three normal knocks | |
| [ ] | `knock_loud` | C0_Knock | louder, impatient knocking | |
| [ ] | `door_open` | C0_Knock, C0_Outside | apartment door opening | |
| [ ] | `box_open` | C0_Knock | tape ripping, cardboard box opening | |
| [ ] | `paper` | C0_Knock | paper note unfolding | |
| [x] | `crow_power_on` | C0_Knock | robot boot-up ending in a chime (bigger than `crow_chime`) | Original procedural synthesis; no third-party samples |
| [ ] | `robot_whir` | C0_Outside | small cleaning robot motor | |
| [x] | `message_failed` | C0_PhoneMax | "message not delivered" error tone | Original procedural synthesis; no third-party samples |

## Ambience (loops) — `Assets/Audio/Ambience/` · Yarn `<<ambience NAME>>`

| Done | Name | Where | Description | Source / Licence |
|---|---|---|---|---|
| [ ] | `room_ambience_1999` | Start | old PC fan, CRT hum, quiet night | |
| [ ] | `subway_ambience` | C0_TheJump → C0_SubwayCrow | carriage rumble, track rhythm, murmurs | |
| [ ] | `street_ambience_2027` | C0_Outside | future city: traffic, electronic ads | |

## UI — `Assets/Audio/SFX/` · used by Jaycord automatically

| Done | Name | When | Description | Source / Licence |
|---|---|---|---|---|
| [x] | `key_press` | each key while "typing" | very short (~0.05 s). Add 3–4 variations to the same entry | Original procedural synthesis; no third-party samples |
| [x] | `message_send` | ENTER sends a message | | Original procedural synthesis; no third-party samples |
| [x] | `message_receive` | a message arrives | | Original procedural synthesis; no third-party samples |
| [x] | `option_hover` | mouse over a reply option | subtle | Original procedural synthesis; no third-party samples |
| [x] | `option_select` | reply option chosen | | Original procedural synthesis; no third-party samples |
| [x] | `option_locked` | clicking a LOCKED option | "denied" | Original procedural synthesis; no third-party samples |
| [x] | `link_open` | OPEN LINK pressed | can lead into `static` | Original procedural synthesis; no third-party samples |

## Music (loops) — `Assets/Audio/Music/` · Yarn `<<music NAME>>`

Not used in the script yet. Ideas: 1999 room (nostalgic), 2027 (uneasy, electronic),
CrowGPT theme (friendly but slightly off), chapter end sting.

## Generated electronic SFX pack

16 finished mono PCM WAV files in Assets/Audio/SFX/, 48 kHz / 16-bit, covering 13 sound names. Four key_press variations share one library entry. Original procedural synthesis using oscillators and seeded noise; no external recordings or model service. No generation script is required or included. Peak is capped at 0.8 full scale, with attack/release envelopes. These are stylized electronic effects; remaining foley and ambience entries are still pending.

SampleScene and JaycordTest already reference SoundLibrary.asset. Let Unity import the files, then enter Play mode to audition in context. File structure, samples, peaks and library references were checked; listening and Unity playback have not been verified.

Sound revision: tonal components lowered three semitones; noise high frequencies softened with a one-pole low-pass filter. Durations and Unity GUIDs preserved.

Train chime revision: extended to 2.4 seconds with a 0.70-second second-note onset and longer natural decay; retains the lowered pitch.

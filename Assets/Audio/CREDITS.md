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
| [x] | `train_brake` | C0_SubwayCrow | train braking + doors opening | [Train braking](https://freesound.org/people/jankooiker/sounds/237939/) by jankooiker + [Train Door Opening](https://freesound.org/people/iamaviolin/sounds/439499/) by iamaviolin — CC0; original project chime |
| [x] | `knock` | C0_Knock | three normal knocks | Joseph Sardin; [Knock on a light door](https://bigsoundbank.com/toc-sur-une-porte-legere-s0374.html) — CC0 |
| [x] | `knock_loud` | C0_Knock | louder, impatient knocking | Joseph Sardin; [Knock on a light door](https://bigsoundbank.com/toc-sur-une-porte-legere-s0374.html) — CC0 |
| [x] | `door_open` | C0_Knock, C0_Outside | apartment door opening | Joseph Sardin; [Apartment Door, Opening #1](https://bigsoundbank.com/apartment-door-opening-1-s1702.html) — CC0 |
| [x] | `box_open` | C0_Knock | tape ripping, cardboard box opening | Joseph Sardin + WasabiWielder; [Adhesive tape #2 + Cardboard Box Rustle 4](https://bigsoundbank.com/adhesive-tape-2-s0298.html) — CC0; [Cardboard Box Rustle 4](https://freesound.org/people/WasabiWielder/sounds/334216/) — CC0 |
| [x] | `paper` | C0_Knock | paper note unfolding | Runey; [Unfolding and crumbling paper](https://freesound.org/people/Runey/sounds/234886/) — CC0 |
| [x] | `crow_power_on` | C0_Knock | robot boot-up ending in a chime (bigger than `crow_chime`) | Original procedural synthesis; no third-party samples |
| [x] | `robot_whir` | C0_Outside | small cleaning robot motor | Joseph Sardin; [Robot vacuum](https://bigsoundbank.com/robot-vacuum-s1623.html) — CC0 |
| [x] | `message_failed` | C0_PhoneMax | "message not delivered" error tone | Original procedural synthesis; no third-party samples |

## Ambience (loops) — `Assets/Audio/Ambience/` · Yarn `<<ambience NAME>>`

| Done | Name | Where | Description | Source / Licence |
|---|---|---|---|---|
| [x] | `room_ambience_1999` | Start | old PC fan, CRT hum, quiet night | Joseph Sardin; [Computer 2 (Ventilation)](https://bigsoundbank.com/computer-2-ventilation-s0126.html) — CC0 |
| [x] | `subway_ambience` | C0_TheJump → C0_SubwayCrow | carriage rumble, track rhythm, murmurs | Joseph Sardin; [Metro, Paris #1](https://bigsoundbank.com/metro-paris-1-s0090.html) — CC0 |
| [x] | `street_ambience_2027` | C0_Outside | future city: traffic, electronic ads | Joseph Sardin; [Little Parisian Street](https://bigsoundbank.com/little-parisian-street-s1713.html) — CC0 |

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

16 finished mono PCM WAV files in Assets/Audio/SFX/, 48 kHz / 16-bit, covering 13 sound names. Four key_press variations share one library entry. Original procedural synthesis using oscillators and seeded noise; no external recordings or model service. No generation script is required or included. Peak is capped at 0.8 full scale, with attack/release envelopes. These are stylized electronic effects. The additional foley and ambience pack below completes the listed SFX and ambience entries.

SampleScene and JaycordTest already reference SoundLibrary.asset. Let Unity import the files, then enter Play mode to audition in context. File structure, samples, peaks and library references were checked; listening and Unity playback have not been verified.

Sound revision: tonal components lowered three semitones; noise high frequencies softened with a one-pole low-pass filter. Durations and Unity GUIDs preserved.

Train chime revision: extended to 2.4 seconds with a 0.70-second second-note onset and longer natural decay; retains the lowered pitch.

## Additional foley and ambience pack

Seven recorded foley WAV effects and three recorded Ogg Vorbis ambience loops have replaced their synthetic versions. All are 48 kHz. door_open is the approved first 2 seconds. Existing Unity .meta files and SoundLibrary references are preserved.

train_brake is now the approved 6.65-second game cue: recorded braking, a short arrival chime at 2.2 seconds, then recorded train door opening at 3.1 seconds. The Unity GUID is preserved.

Room ambience is an 8-second mono loop from a real Power Mac G5 fan (a stand-in, not a 1999/CRT recording). Subway ambience is a 24-second stereo Paris metro loop and may contain background French speech. Street ambience is a 30-second stereo contemporary street recording used for the 2027 setting; no electronic ads were added. Loops use circular crossfades. Music remains optional and unproduced.

All imported sources are CC0-1.0: https://creativecommons.org/publicdomain/zero/1.0/ . Cardboard and paper use the public high-quality MP3 previews; exporting to WAV does not restore lossless source quality. Other sources were original WAV downloads. Processing: cropping, level adjustment, edge fades, resampling, knock cadence assembly, tape/cardboard assembly, and loop crossfades. The mechanical recordings were not pitch-shifted. train_brake includes the original synthesized project chime; the other nine replacements use recordings only.

### Imported recording edits

| Output | Author | Source | Edit |
|---|---|---|---|
| train_brake.wav | jankooiker + iamaviolin | [Train braking](https://freesound.org/people/jankooiker/sounds/237939/) by jankooiker + [Train Door Opening](https://freesound.org/people/iamaviolin/sounds/439499/) by iamaviolin — CC0; original project chime | Braking source 30.8–34.0 s; full 3.529 s door take; first 1.6 s of project train_chime; resampling, mono mix, EQ, fades and level matching |
| knock.wav | Joseph Sardin | [Knock on a light door](https://bigsoundbank.com/toc-sur-une-porte-legere-s0374.html) — CC0 | 5.43–6.728 s; three natural knocks; gain and tiny edge fades |
| knock_loud.wav | Joseph Sardin | [Knock on a light door](https://bigsoundbank.com/toc-sur-une-porte-legere-s0374.html) — CC0 | Six real strikes edited to an impatient cadence and raised in level |
| door_open.wav | Joseph Sardin | [Apartment Door, Opening #1](https://bigsoundbank.com/apartment-door-opening-1-s1702.html) — CC0 | 0–2.00 s; level and edge fades; final 8 ms fade to prevent a cut click |
| box_open.wav | Joseph Sardin + WasabiWielder | [Adhesive tape #2 + Cardboard Box Rustle 4](https://bigsoundbank.com/adhesive-tape-2-s0298.html) — CC0 | Tape 0–2.83 s followed by cardboard recording; see second source below |
| paper.wav | Runey | [Unfolding and crumbling paper](https://freesound.org/people/Runey/sounds/234886/) — CC0 | 1.92–3.72 s; high-quality MP3 preview source; gain and edge fades |
| robot_whir.wav | Joseph Sardin | [Robot vacuum](https://bigsoundbank.com/robot-vacuum-s1623.html) — CC0 | 7.8–13.8 s of a real iRobot Roomba i7; level and edge fades |
| room_ambience_1999.ogg | Joseph Sardin | [Computer 2 (Ventilation)](https://bigsoundbank.com/computer-2-ventilation-s0126.html) — CC0 | 0.3–9.3 s; 1 s circular crossfade; real Power Mac G5 fan, used as an older-computer stand-in, not a recording from 1999 or a CRT |
| subway_ambience.ogg | Joseph Sardin | [Metro, Paris #1](https://bigsoundbank.com/metro-paris-1-s0090.html) — CC0 | 53–79 s; 2 s circular crossfade; real Paris metro, possible background French speech |
| street_ambience_2027.ogg | Joseph Sardin | [Little Parisian Street](https://bigsoundbank.com/little-parisian-street-s1713.html) — CC0 | 52–85 s; 3 s circular crossfade; contemporary street stand-in for story year 2027, no synthetic ads |

Additional box source: [Cardboard Box Rustle 4](https://freesound.org/people/WasabiWielder/sounds/334216/) by WasabiWielder, CC0.

Checked file hashes against the approved audition versions and preserved all Unity GUIDs. Audio decoding, levels and loop boundaries were checked during preparation; Unity playback has not been verified.

Train arrival revision: the external braking and door recordings use public high-quality MP3 previews, licensed CC0-1.0. WAV export does not restore lossless source quality. This is an assembled game cue, not one continuous subway recording. Avoid playing the standalone train_chime simultaneously, because this cue already contains it.

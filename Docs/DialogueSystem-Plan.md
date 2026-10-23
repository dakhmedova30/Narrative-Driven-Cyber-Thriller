# Dialogue System — Implementation Plan

Task breakdown for turning `Chapter 0.yarn` into a fully playable chapter.
Each part is split into **Code**, **Unity setup**, and **Assets**, with a **Done when** check at the end.

**Suggested order:** 0 Command registration → 1 Dialogue modes → 4 Sound → 3 Scene & props → 2 Camera → 5 Interaction

---

## 0. Command registration (foundation — do this first)

**Code**
- [ ] `YarnCommands.cs`: register `bg`, `camera`, `show`, `hide`, `sfx`, `fade_in`, `fade_out`, `chapter_end`, `explore`, `wait_for_click`. For now, each one only logs to the Console.

**Unity**
- [ ] Add `YarnCommands` to the `DialogueSystem` prefab and assign its DialogueRunner.

✅ **Done when:** Chapter 0 runs from start to end with no red "command not found" errors in the Console.

---

## 1. Dialogue modes (face / MHM / Jaycord / phone / cutscene)

**Code**
- [ ] `ModeRouter.cs`: on entering a node, read its tags and forward lines and options to the matching UI.
- [ ] `ChatPresenter.cs`: core chat logic — "typing…" indicator, delay based on message length, spawning messages, reply options, typing the chosen reply into the input box before sending, scroll behaviour.
- [ ] `ChatMessage.cs`: displays one message (avatar, name, timestamp, text; hides avatar and name for consecutive messages from the same sender).
- [ ] `ChatHistory.cs`: stores messages per conversation so the chat can be redrawn when the app is reopened or a save is loaded.
- [ ] `CharacterData.cs`: character info (name, avatar, silhouette, colour), shared by every UI.
- [ ] Special speakers: show `Note` as a paper note and `System` as a grey system message.

**Unity**
- [ ] `ChatWindow.prefab` (base), plus four Prefab Variants: MHM / Jaycord / CrowGPT / Phone.
- [ ] Shared parts: `MessageItem`, `TypingIndicator`, `ReplyOption`.
- [ ] Add `ModeRouter` to `DialogueSystem` and hook up the existing dialogue box as `face` mode.
- [ ] Recreate Leo / Max / CrowGPT in `Data/Characters/` from inside Unity (delete the three empty `.asset` files).

**Assets**
- [ ] App UI art (exported from Figma), character avatars, character silhouettes.

**Script**
- [ ] Agree on tag names. The file header lists `Jaycord`, but the script uses `MHM`; `phone` is used but not listed.

✅ **Done when:** `C0_MaxChat` plays as a chat and you can scroll back through history; the following `face` nodes switch back to the normal dialogue box.

---

## 2. Camera

**Decide first:** is the game 2D (background images) or 3D? This determines the approach below.

**Code**
- [ ] `CameraDirector.cs`: `<<camera ACTION TARGET>>` switches to the named shot and waits for the blend to finish before the dialogue continues. Each action uses its own speed and easing curve.
- [ ] `ScreenFader.cs`: `<<fade_out>>` / `<<fade_in>>` using a full-screen black overlay; the dialogue waits for it to finish.

**Unity**
- [ ] Install Cinemachine (optional if the game is 2D and only swaps backgrounds).
- [ ] Place one camera per shot, named exactly as in the script: `window_3f`, `room_2026`, `leo_desk`, `monitor`, `monitor_2027`, `room_2027`, `crow`, `hallway_2027`, `street_2027`.

✅ **Done when:** the opening smoothly pushes in on the window → pans across the room → zooms in on the desk, and the dialogue waits for each move to finish.

---

## 3. Scene & props (bg / show / hide)

**Code**
- [ ] `SceneDirector.cs`: `<<bg NAME>>` swaps the background; `<<show>>` / `<<hide>>` toggle props by name.

**Unity**
- [ ] 6 backgrounds: `apartment_exterior_night`, `room_2026`, `room_2027`, `hallway_empty`, `hallway_2027`, `street_2027`.
- [ ] 9 props: `jaycord_badge_max`, `jaycord_max_profile`, `screen_date_2027`, `door`, `delivery_box`, `box_open`, `crow`, `cleaning_robot`, `phone`. Names must match the script exactly.

**Assets**
- [ ] Art for the backgrounds and props above. Use coloured placeholder boxes with labels until the art is ready.

✅ **Done when:** every `<<bg>>` and `<<show>>` produces a visible change on screen.

---

## 4. Sound

**Check first:** who is working on `feat/SoundEffects`? Agree on a shared interface with them before starting.

**Code**
- [ ] `SoundLibrary.cs`: a name → audio clip lookup table.
- [ ] `AudioManager.cs`: `<<sfx>>` plays a sound effect; also add `<<bgm>>` and `<<bgm_stop>>`.

**Unity**
- [ ] Create `SoundLibrary.asset` in `Data/Audio/`.
- [ ] Create an Audio Mixer with Master / BGM / SFX groups.

**Assets**
- [ ] 7 sound effects: `notification`, `static`, `boot_chime`, `knock`, `knock_loud`, `door_open`, `crow_power_on`. Plus chat notification and typing sounds, and background music.

✅ **Done when:** the knock, the door opening, and Crow powering on all have sound, and incoming chat messages play a notification sound.

---

## 5. Interaction (click-to-explore, clickable links)

**Code**
- [ ] `Hotspot.cs`: a clickable object that highlights on hover and runs a given Yarn node when clicked.
- [ ] `ExplorationController.cs`: `<<explore ROOM>>` shows the clickable objects and pauses the dialogue; `<<wait_for_click OBJECT>>` waits until the player clicks that object.
- [ ] Handle `#link` in `ChatPresenter`: show the line as a clickable link and continue only after the player clicks it.

**Unity**
- [ ] Add a `Physics 2D Raycaster` to the camera (`Physics Raycaster` for 3D). The project uses the new Input System, so use `IPointerClickHandler` rather than `OnMouseDown`.
- [ ] Add Hotspots in `room_2027` for the computer, the calendar, a corner of the room, and the door.

**Script**
- [ ] Split `C0_LookAround` into `C0_LookAround` + three "look at" nodes + `C0_LookAround_Check`.
- [ ] Change the door scene to use `<<wait_for_click door>>`.

✅ **Done when:** after the player clicks any two objects in the room, the knock plays and the story continues.

---

## Notes

- Parts 0 and 1 are the backbone; everything else plugs into them.
- Art and audio can be produced in parallel with the code, so they're good tasks to split across teammates.

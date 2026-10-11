# 2026-10-11

## Done
- Fixed Yarn Spinner dependency: `manifest.json` now installs from git (`YarnSpinner-Unity.git#current`, v3.2.8) instead of a teammate's local path.
- Built `DialogueSystem` prefab from Yarn's template (`Assets/Prefabs/Dialogue/`), wired to `Story.yarnproject`, tested in `SampleScene`.
- Set up project folders (`Scripts/`, `Prefabs/`, `Art/`) and placeholder scripts for dialogue and save system.
- Wrote a Yarn Spinner cheatsheet (`Assets/Dialogue/ReadMe`).
- Set up dialogue structure: one scene per node, presentation mode set by node tags (`MHM` / `chat` / `face` / `Jaycord`).
- Visual directions mapped to commands: `<<camera>>`, `<<bg>>` / `<<show>>`, `<<sfx>>`.
- Wrote Chapter 0 story draft and converted it to `Chapter 0.yarn` (10 nodes, link choice memory, look-around menu, CrowGPT question hub).
- Added `.gitattributes` to normalize line endings (LF for text, binary for media).

## To Do
- [ ] Change look-around and door scenes to clickable exploration.
- [ ] Unity: switch UI by node tag, implement custom commands, handle `#link` and `<br>`.
- [ ] Create `CharacterData` ScriptableObject and recreate `Leo` / `Max` / `CrowGPT` assets in Unity (current `.asset` files are empty).
- [ ] Clean up folder nesting (`Chapters/Audio/...` is inside `Scripts/Data/Characters/`).
- [ ] Merge `feat/Dialogue` into `feat/Jaycord` before starting Jaycord chat UI.

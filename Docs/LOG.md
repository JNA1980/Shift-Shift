# Build log

## 2026-11-09 — D09–D10: repo, project, input classifier

**Done**
- Public repo JNA1980/Shift-Shift: Unity .gitignore, Git LFS, README, MIT LICENSE, Docs/ (PLAN, SCOPE, CREDITS, INPUT, SETUP).
- Unity 6000.6.8f1 URP project (Universal 3D template), Input System active, Web build profile (Desktop - Development).
- GestureClassifier (engine-free, Assets/_Game/Scripts/Core) + 24 edit-mode tests; ShiftInput + GestureDebugHud (Runtime); BuildScript with WebGL/Windows menu items; tools/butler-push.sh.
- InputTest scene (only scene in the build list).
- Fixed setup issues: Unity project had been created nested (Shift-Shift/Shift-Shift); moved up, stray copy deleted. Build intermediates in /Data/ git-ignored. Commit author emails rewritten to the GitHub no-reply address.

**Tested**
- 24/24 edit-mode tests pass in Unity and via `dotnet test tools/CoreTests`.
- All 7 gestures register in the editor and in the Web build (Edge, local Build And Run).
- Lane taps resolve on key-up (~60–107 ms after key-down in the overlay); John: feels fine. Keep as is.

**Uncertain / not yet tested**
- Shift+Shift > Build WebGL menu item not yet run (Build And Run was used instead).
- Firefox and Chrome not yet tested (only Edge).
- Sticky Keys behaviour with the Windows shortcut on: not recorded.
- Input timestamps use realtimeSinceStartup, not dspTime yet; frame-quantised (~16 ms).
- Playbook alignment pending: see next entry.

## 2026-11-09 — D10: input aligned with playbook prompt 1.1

**Done**
- Thresholds moved to a `GestureSettings` ScriptableObject (`Assets/_Game/Settings/GestureSettings.asset`), wired into InputTest. Live edits in Play mode rebuild the classifier.
- `IClock` interface (Core) + `DspClock` stub (dspTime, fixed BPM, default 120 on ShiftInput).
- Events now `GestureFired(type, songTime)`, `HoldStarted`, `HoldEnded`; GestureType is the seven gestures.
- Alternate is now beat-based (Option 1, John's decision): within ±0.25 beats of a beat line, alternating sides, consecutive beats; streak expires when the next beat passes.
- Overlay shows key states, clock + beat, thresholds, alternate streak and the last five gestures.
- Kept `Scripts/Core` + `Scripts/Runtime` instead of `Scripts/Input/` (Core split lets tests run without Unity; moving files risks breaking scene references).

**Tested**
- 27/27 classifier tests pass via `dotnet test tools/CoreTests` (tap/hold boundary, both-tap in/out of window, alternate on/off beat, expiry, lockout).
- Runtime scripts compile against stand-in Unity types (catches C# errors only).
- Found and fixed while testing: the alternate streak expired at key-up even when the tap's key-down was on the beat.

**Not yet run**
- Nothing above has been compiled by Unity or played. Needs: compile, Test Runner, Play InputTest, Web build.
- dspTime in WebGL: does the clock advance before the first click/key? Does it move smoothly enough for 120 ms windows?

## 2026-11-09 — D10: project setup per playbook 0.3

**Done**
- `Shift+Shift > Apply Project Setup` (Assets/_Game/Scripts/Editor/ProjectSetup.cs): creates missing folders (Materials, UI, Settings, …), Boot/Menu/Run scenes, build list Boot → Menu → Run (InputTest kept after), a neutral URP Volume profile at Assets/_Game/Materials/PostProcessProfile.asset, and Web player settings (1280x720, Brotli + fallback, data caching, explicit exceptions, strip engine code, IL2CPP optimise for size). Re-runnable; never overwrites scenes or the profile.
- Already correct before this: Active Input Handling = Input System only.

**Not yet run**
- Not compiled by Unity. Least certain line: `PlayerSettings.SetIl2CppCodeGeneration(...)` in Unity 6000.6.
- Left for the editor: Windows x64 build profile; Web build profile "Code Optimization".

## 2026-11-09 — D10: upload scripts per playbook 0.4

**Done**
- `tools/upload.ps1` (Windows) and `tools/upload.sh` push Builds/WebGL to channel `html` and a zipped Builds/Windows to `windows`; itch user/project read from `tools/itch.env` (git-ignored; template `tools/itch.env.example`). Replaces tools/butler-push.sh. Folder kept lowercase `tools/` (case-only renames are unreliable on Windows).

**Tested**
- Both scripts run against a fake butler (PowerShell 7 on Linux): correct targets and version, Windows zip contents, and clear errors for missing env file, placeholder user, missing builds, bad channel, butler failure.

**Not yet run**
- Windows PowerShell 5.1 on John's PC; real butler upload (needs itch draft project + `butler login`).

## 2026-11-09 — D10: Input System errors after script reload

- After pulling the input changes, Play mode logged InputActionState errors ("Map/Control/Binding index out of range", NullReferenceException on Key:/Keyboard/leftShift). Gestures still worked.
- None of our code uses InputActions (ShiftInput polls Keyboard directly). After restarting Unity: 0 errors. Conclusion: stale action state after the domain reload (template's project-wide actions, or an Input System 1.20.1 reload bug).
- If it recurs after pulls: restart Unity. If it becomes a nuisance: Project Settings > Input System Package > Project-wide Actions = None (nothing in the game uses them).

## 2026-11-09 — D10: dspTime clock verified in the Web build (Edge)

**Tested (John, Web build, Edge, InputTest)**
- DspClock advances before any click/key (no frozen clock before audio unlock).
- Holds decide at +255 ms against a 250 ms threshold (~5 ms frame delay); hold ends at +0 ms.
- Tap timestamps look smooth (gaps 279–328 ms, no fixed step), so no audio-buffer quantisation visible at this level.
- Alternate streak counts up. Alt+Tab while holding Shift: key not stuck.
- All 7 gestures register; no errors in Unity's console after the build.

**Uncertain**
- Chrome and Firefox not yet tested (day 28 browser pass). Browser F12 console not checked on this build.
- Input-to-audio latency not measured yet (day 13, needs Conductor + clicks).
- Info message from the template's experimental `com.unity.pipeline` package on every build: harmless; removal is out of scope for now.

## 2026-11-09 — D10: project setup applied (John)

**Done**
- Ran Shift+Shift > Apply Project Setup: Materials/UI folders, Boot/Menu/Run scenes, build list Boot → Menu → Run → InputTest, neutral Volume profile. Committed (29af827).
- Found: the player-settings changes were not on disk until File > Save Project, and in Unity 6.6 they land in the active Web build profile's own Player Settings copy (Assets/Settings/Build Profiles/Web - Desktop - Development.asset), not ProjectSettings.asset. Confirmed in the profile UI: 1280x720, IL2CPP optimise for code size, strip engine code, Input System only. Decision: keep the profile copy (it is what Web builds use).
- ProjectSetup now runs File > Save Project itself.

**Uncertain**
- Whether Shift+Shift > Build WebGL (BuildPipeline.BuildPlayer) uses the active profile's player settings or the global ones. Check the first time the menu build is used (by day 24).
- The Web profile is a Development preset (C++ config Debug, Development Build). A release profile is needed before the itch upload (day 24/28).
- Web "Code Optimization" option is not offered in 6000.6; skipped.

## 2026-11-09 — D10: closed

- Web profile player settings verified in the committed profile asset (d630369): 1280x720, Brotli, decompression fallback on, data caching on, explicit exceptions only, strip engine code, IL2CPP code generation = size, Input System only. C++ compiler configuration is Debug (Development profile); Release profile still needed before upload.
- Playbook Day 0 (0.1–0.4) and 1.1 complete. Plan "done when" for days 9–10 met: WebGL build runs in a browser and all 7 gestures log correctly. About one day ahead of plan.
- Still John's (not blocking): itch.io draft project, butler install + login, tools/itch.env.
- Next: playbook 1.2 Runner + TrackBuilder (day 11).

## 2026-11-09 — D11: Runner, TrackBuilder, chase camera (playbook 1.2)

**Done**
- RunnerModel (Core): lanes, slide/sprint/phase intervals, jump window, charges, obstacle judging. Decision A: all actions count from key-down; Runner waits on undecided presses before judging.
- Runner (forward position from song time, visuals, RunFailed/RunCompleted, debug panel top-right), Obstacle, TrackPiece, TrackBuilder, ChaseCamera (lag + sprint FOV kick).
- GestureClassifier/ShiftInput: TryGetUnresolvedPress (additive). GestureSettings: chargeTaps = 4.
- Shift+Shift > Create Greybox Scene: materials, track pieces (Straight/Tunnel/Bridge/Gap 2.5 m), obstacle prefabs, 200 m Greybox scene with one obstacle of each behaviour.
- Docs/RUNNER.md: rules and six decisions taken while building (notably: sprint does not change speed).

**Tested**
- 47/47 core tests (`dotnet test tools/CoreTests`): 17 runner rule tests + 3 unresolved-press tests added.
- Runtime and greybox editor scripts compile against stand-in Unity types.

**Cannot verify without Play mode (playbook 1.2 asks for this list)**
- Lane ease feel (120 ms) and whether the visual lag behind key-down feels responsive.
- Jump arc height/duration vs. the 2.5 m gap; slide pose; phase material swap.
- Chase camera lag, look-ahead, sprint FOV kick.
- Fail timing: whether a fail judged up to ~250 ms late (decision A wait) is noticeable.
- That obstacle positions match the rules (lane from X, time from Z) in the generated scene.
- The generated scene, prefabs and URP materials themselves (editor script not run by Unity yet).

## 2026-11-09 — D11: fix editor assembly reference

- Unity compile errors in GreyboxSetup.cs: `ShiftShift.Core` not found. Cause: ShiftShift.Editor.asmdef did not reference ShiftShift.Core (the stand-in compile builds everything as one assembly, so it cannot catch missing asmdef references). Added the reference.
- Warning CS0618 in Runner.cs: `FindObjectsByType(FindObjectsSortMode)` is deprecated in 6000.6. Harmless; left as is until the replacement overload is confirmed from the full warning text.
- John confirmed: sprint does not change forward speed.

## 2026-11-09 — D11: greybox scene, first Play

- 47/47 tests pass in Unity. Greybox scene generated (track, runner, obstacles, camera all wired) but Play failed: ShiftInput.settings was empty in the generated scene, so input disabled itself and the Runner refused to start. All other references set by the tool were fine.
- Likely cause (not confirmed): the settings asset was loaded at the start of the tool, before ~20 prefab/material assets were created; an asset reload in between left the reference stale. Fix: load it just before assigning, and every assignment is now verified and logs an error if it did not stick. John assigns it by hand in the existing scene.

## 2026-11-09 — D11: beat dot on the debug panel (John asked)

- Wall charge kept failing: with no beat cue, on-beat taps were effectively random. With Alternate Beat Tolerance temporarily at 0.45 (not committed), the charge and wall work.
- Added a beat dot to GestureDebugHud: lit for the first 20% of each beat (yellow on beat 1 of the bar, green otherwise) with the beat number 1-4. Debug aid until the Conductor's click track (day 12).
- Not run in Unity yet. Compiles against stand-in types.

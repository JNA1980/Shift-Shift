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

## 2026-11-09 — D11: greybox played (John)

**Tested (editor, Greybox scene)**
- Day 11 question answered: moving the capsule with two keys feels responsive.
- Wall charge works with a beat reference (tolerance back at 0.25; beat dot added).
- Greybox scene with settings assigned committed (bf4d5c4).

**Open bug (not yet diagnosed)**
- "Sometimes after an obstacle is passed I have to wait before changing lane, or it fails."
  Hypotheses: (1) blocks judged at their centre while they look passed ~80 ms earlier, so a tap back counts as inside the lane (decision A uses key-down); (2) phase lockout ignores taps (would not cause a fail); (3) ordering bug. Waiting on the `[Run] Failed` line plus the preceding `[Gesture]` taps to compare times.

## 2026-11-09 — D11: fix "fails after passing a block"

**Diagnosis (from John's run)**
- TapLeft key-down 2.880 s, recognised 3.051 s; Block @30m (middle) at 3.000 s. Rules (decision A, key-down) put the runner back in the middle before the block, while the screen only moves the capsule after key-up (3.05 s + 120 ms ease). The fail was then shown at 30.5 m because judging waited for the tap to resolve, so it looked like a fail after passing.

**Fix (John: go)**
- A lane change is "in between" from key-down to key-up: a one-lane obstacle passes if either lane is clear. RunnerModel records each tap's recognition time; Runner passes it in. Holds, jumps, phase unchanged.

**Tested**
- 52/52 core tests (5 new, including John's exact timings → Pass, and released-before-block → Hit).
- New check: Core, Runtime and Editor compiled as separate assemblies with only their asmdef references (non-transitive, like Unity). Verified it reproduces yesterday's missing-reference error.

**Not yet run**
- In Unity / Play mode: re-run the 30 m block the same way (tap back just before it, release after).

## 2026-11-09 — D11: wall feel note (for M1 review, day 15)

- John: after L R L R the red wall "looked like I failed but it went through". Rules are correct (charge breaks the next wall; no extra input needed). The look comes from: wall hidden only when the runner reaches its centre (capsule already touching the face), no break effect yet (day 27 juice), and a possible up-to-250 ms wait when a key was down just before the wall (decision A).
- Not changed. Option if needed: break walls at their front face. Decide at M1 review.

## 2026-11-09 — D11: 30 m block fix confirmed (John)

## 2026-11-09 — D12: Conductor and level JSON (playbook 1.3)

**Done**
- Conductor: waits for the first Shift press (browser audio unlock), schedules audio with PlayScheduled at dspTime + 1 s, SongTime = dspTime - start - offset, BPM, CurrentBeat, Beat event; becomes ShiftInput's clock. Start detected in LateUpdate so the start press is not also a gesture.
- IClock.SongTimeAtBeat. Runner: forward position = 5 m per beat x current beat; obstacle timing from beats via the clock; waits for the Conductor; run ends at the level's last beat; music stops on fail.
- LevelData + validation + type mapping (Core), LevelLoader (JsonUtility, WebGL-safe), ObstacleSpawner (beat → Z, lane → X, prefab per type).
- level00.json: 100 BPM, 32 beats, a bar every 4 beats from beat 8, alternating left/right.
- Shift+Shift > Create Level Test Scene: generates click_100.wav (if missing) and LevelTest.unity.
- Docs/LEVELS.md (format), CREDITS (generated click track).

**Deviations from the playbook (flagged to John)**
1. Click track generated by an editor menu (C#) instead of a Python script in Tools/: no Python needed on John's PC, and the WAV must be committed from his machine (LFS uploads are blocked from the cloud session).
2. Obstacle "tags" are the Obstacle component's kind/layer, not Unity tags (Unity tags need TagManager edits; the rules never read tags).
3. Chart type "ghost" = phase barrier (playbook 1.4: ghost -> phase held). Ghost-layer obstacles are not in the format yet (needed for level 4).

**Tested**
- 62/62 core tests (level validation, type mapping, lane mapping, click WAV header/length/click positions).
- Per-assembly compile check (Core / Runtime / Editor with asmdef references) passes.

**Not yet run**
- Anything in Unity: PlayScheduled timing, whether obstacles pass exactly on the click, generated scene and WAV import.
- WebGL: PlayScheduled and audio start after the key press (day 13).

## 2026-11-09 — D12: fix empty references in generated scenes (root cause)

- LevelTest generated without a track ("TrackBuilder: no fill piece assigned"). Root cause, also behind day 11's empty Settings field: the tools loaded existing assets and then called EditorSceneManager.NewScene(Single), which unloads assets nothing references yet, leaving the references empty. Freshly created prefabs survived, which is why only some fields failed.
- Fix: both tools create the new scene first, then load/create assets. Track fill piece now assigned through the verified Set(); the spawner logs any missing type prefab.
- Runner: FindObjectsByType without the deprecated sort parameter (6000.6 warning).
- Greybox scene from day 11 is unaffected. LevelTest.unity must be deleted and regenerated.

## 2026-11-09 — D12: clearer level-file errors in the level test tool

- Regenerating LevelTest failed with "level00.json is missing or invalid" (it had loaded fine on the previous run; file unchanged in git). Waiting on `git status` from John to see whether it was deleted. The tool now says which: not found (and whether the file exists on disk) vs. the list of validation errors.

## 2026-11-09 — D12: level00 sync check (John)

- Obstacles reach the runner exactly as the click sounds ("the click happens just as the obstacle passes by"): sync is correct as built. It feels early because the obstacle visibly approaches before it is level with the runner. offsetMs experiments did not help (it is not latency). John: keep A (click = obstacle level with runner); revisit at M1 review if pressing on the click feels wrong.
- level00.json had been deleted while deleting the broken LevelTest scene; restored. Laptop speakers.

## 2026-11-09 — D13: judge, scoring, run loop, calibration (playbook 1.4)

**Done**
- Rules: RunnerModel.ResolveWithAnswer (which gesture answered), Judge (Perfect 50 / Good 120 / OK / none / Miss), ScoreKeeper (100/50, x2/x3/x4 at 10/25/50), Calibration maths. Windows in GestureSettings.
- Decisions (flagged): OK grade for answers outside the window (0 points, streak reset); bars needing no action are ungraded; a gap's beat is the take-off (hole placed half a jump later).
- Runtime: Runner emits Judged per obstacle; Conductor: two-bar count-in, saved user offset, level set from code, StartNow; GameFlow (level by name, count-in pulses, flash + 60 ms hit-stop, instant restart by scene reload, results, local best); LatencyCalibration scene (F2 from the debug overlay); Boot loads Run.
- Shift+Shift > Set Up Run Scenes: fills Run, creates Calibrate, Boot → Run, build list Boot, Menu, Run, Calibrate, …
- Docs/SCORING.md.

**Tested**
- 80/80 core tests (18 new: grades per obstacle type, scoring, multiplier steps, calibration maths).
- Per-assembly compile check passes.

**Not yet run**
- Everything in Unity and in WebGL: count-in, restart time (< 1 s target), hit-stop, results, calibration, F2 in the browser.
- F1 is the browser's help key in Chrome; the debug overlay toggle may not work in WebGL (F2 should).

## 2026-11-09 — D13: fix compile error (Safe Mode)

- Unity opened in Safe Mode: `Resolution` (Core) was ambiguous with `UnityEngine.Resolution` in Runner.cs. Renamed ours to `ObstacleResolution`. The stand-in compile check did not know Unity's `Resolution`; it now includes common UnityEngine type names and reproduces the exact error (Runner.cs(200,17) CS0104) when the old name is put back.
- 80/80 tests, per-assembly check passes.

## 2026-11-09 — D13: first calibration runs (editor)

- Single tap on the last click: +111 ms. Tapping every click: only 4 of ~16 measured clicks counted (mean +7, spread 29).
- Hypotheses: (1) Windows Sticky Keys popup after 5 Shift presses (start + 4 practice taps = exactly 5) stealing focus; (2) taps beyond the ±240 ms cutoff; (3) Game view focus. Result screen now shows "counted X of Y taps" and the Console logs every tap's offset to tell them apart.

## 2026-11-09 — D13: first Web builds of the run loop (John)

**Tested**
- Build And Run failed until the Web profile's Client Browser Type was set (it pointed at a non-default browser with no executable). Then ran in **Chrome and Firefox** (same localhost server).
- Both browsers: count-in, play, fail screen ("Shift to retry"), level complete with results panel.
- Firefox fail "bar b16 lane 0" was legitimate (tapped into the bar's lane at beat 13.3).
- Score 0 in completed runs is expected: level00's bars sit in the side lanes and the runner starts in the middle, so most need no action (ungraded). Grades only appear when dodging a bar in your own lane.

**Pending from John**
- Calibration offset/spread/counted-of-taps for Editor, Chrome, Firefox (Sticky Keys shortcut checked off).
- Restart time after a fail (< 1 s target). Grades seen when dodging on the click.

## 2026-11-09 — D13: browser calibration results and M1 gesture decision

**Measured (WebGL, laptop speakers, builds without the counted-of-taps display)**

| Browser | Offset | Spread | Taps counted |
|---|---|---|---|
| Chrome | +74 ms | ±29 ms | 16 |
| Firefox | −27 ms | ±35 ms | 15 |
| Edge | −36 ms | ±63 ms | 8 (too few; redo) |

- Kill criterion 2 (beat sync off by > 60 ms in WebGL with no fix): **passes**. The offset is compensated by calibration; the jitter (spread ~±30 ms) is within budget.
- Chrome and Firefox differ by ~100 ms, so calibration has to be per browser. Flagged (not built): prompt calibration on first launch, since itch players will not find F2.
- **Decision (John): keep all 7 gestures.** Recorded in SCOPE.md.

**Done**
- Assets/_Game/Levels/test_gestures.json: one of each gesture, 4 beats apart, 100 BPM, 40 beats (fits click_100). Validated against the level rules.
- Restart after a fail feels under a second (John). Playbook 1.4 targets met: count-in, flash + hit-stop, instant restart, results, calibration; Chrome and Firefox tested.

## 2026-11-10 — D13: one answer per obstacle (phase exploit fix, all-lane low bars)

**Found (John, test level):** holding both Shift keys passed the low bar (beat 16), the gate (20) and the wall (36). Cause: the playbook 1.2 rule "phased runners ignore Solid obstacles" made phase pass everything; it also broke 1.4's "exactly one correct gesture per obstacle".

**Changed**
- New ObstacleKind.Barrier (added last; kinds are saved as numbers). Only a Barrier is passed by phasing; low/gate/gap/wall/bar need their own answer, phased or not. Ghost-layer obstacles still only hit while phased.
- Chart "ghost" → Barrier. PhaseBarrier prefab kind set to Barrier (prefab file edited).
- Low bars span all lanes (John): chart "low" is all-lane; LowBar prefab all-lane and full track width (prefab file edited).
- Wall: only a charge breaks it. A charge still lasts until the next wall (early charge = OK grade); unchanged unless John asks.
- Docs RUNNER, LEVELS, SCORING updated.

**Tested**
- 86/86 core tests (new: phase does not pass low/gate/wall/gap/lane block; barrier without phase hits; low maps to all lanes). Per-assembly compile check passes.

**Not yet run** in Unity: prefab edits (kind 5 = Barrier, LowBar scale 7), test level replay.

## 2026-11-10 — D13: gap failed while the capsule looked airborne

- John's run: jump key-down 14.113 s (half a beat early), recognised 14.348 s; gap far edge reached 14.800 s. Rules had the runner airborne 14.113–14.613 (from key-down), the screen 14.348–14.848 (arc starts on recognition), so the fail showed mid-air.
- Fix (same principle as day 11's lane rule): a jump is airborne from key-down until the visual landing (recognised + 0.5 s). Grading unchanged: that jump now passes as OK.
- 89/89 core tests (3 new, including John's exact timings). Per-assembly compile check passes. Not yet run in Unity.
- Also from the screenshot: slide (Hold L) and sprint (Hold R) through beats 16 and 20 passed on this run.

## 2026-11-10 — D13: charge shortened to two taps (tuning)

- John asked for an easier charge ("two presses of L R"); no preference among the options offered. Set GestureSettings Charge Taps 4 → 2 (setting only, no code): L, R on consecutive clicks charges; the second tap on the wall's click = Perfect. Side effect: dodging left then right on back-to-back clicks also charges (harmless; only walls use it). Revisit at M1 (2, 3 or 4).
- The plan's "four on-beat alternations" is now a tunable value, not a rule.

## 2026-11-10 — D13: charge redesign (John: option A, 4 taps)

- John: "L R L R doesn't seem to work". Alternatives offered (no-beat alternate, double jump, roll, double tap, mash); John chose **A: quick alternating taps, no beat, 4 taps**. The 2-tap tuning from earlier today is reverted (Charge Taps back to 4).
- Classifier: alternate streak continues when sides alternate and key-downs are within Alternate Max Gap (400 ms, GestureSettings; replaces Alternate Beat Tolerance). Expires 400 ms after the last tap.
- Bug fixed: a charge had to complete strictly before the wall's moment, so a last tap aimed at the click failed about half the time. Now a charge completed up to the Good window (120 ms) after the wall still breaks it; the Runner waits that long while a streak is in progress.
- CLAUDE.md, INPUT, RUNNER, LEVELS, SCORING updated.
- 92/92 core tests (alternate tests rewritten for gaps; late-charge tests; two "not a jump" tests updated because a quick L then R is now also alternate step 2). Per-assembly check passes. Not yet run in Unity.

## 2026-11-10 — D13: gate opening, wide bar, hurdle (John)

**Decisions (John)**
- Gate: one-lane opening whose position varies; you must change into it and sprint through.
- Short wall ("hurdle", **pink**) cleared by jumping; the tall **red** wall stays charge-only, so all 7 gestures keep a purpose.
- Two-lane obstacles: a new **wide bar** (one free lane) alongside the normal one-lane bars ("simple and complex"). Orange stays the colour for bars.

**Done**
- Rules: ObstacleInfo.LaneIsOpening (blocks every lane except Lane; in-between rule applies: either lane of a change in progress counts); new ObstacleKind.Hurdle (jump only). Wide bar answer = the move into its free lane.
- Chart types: `gate` now has an opening (`lane` = opening), new `wide` (`lane` = free lane) and `hurdle`. Hurdles are placed half a jump after their beat, like gaps.
- Prefabs (tool): GateOpening (yellow, Panel0-2), WideBar (orange, Panel0-2), Hurdle (pink); the spawner hides the open lane's panel. Shift+Shift > Set Up Run Scenes now refreshes the Run and LevelTest scenes' obstacle lists. Greybox scene untouched (old full-width gate still works).
- test_gestures.json: bar, wide (free right), low, gate (opening left), gap, ghost barrier, hurdle, wall.
- Docs LEVELS, RUNNER, SCORING.

**Tested**
- 102/102 core tests (10 new). Per-assembly compile check passes.

**Not yet run** in Unity: new prefabs, panel hiding, scene refresh, the updated test level.

## 2026-11-10 — D13: new obstacles verified in the editor (John)

- After Shift+Shift > Set Up Run Scenes refreshed the obstacle lists, all 8 types appear and the test level completes ("works good"). The first attempt showed "No prefab for type 'wide'/'hurdle'" and "Gate has no Panel0" because the Run scene still had its old list (the refresh had not been run after the pull).
- **For the art pass (day 17, playbook 2.2/2.3):** the wide bar and the gate read as separate bars (one block per lane with gaps). Make the wide bar one solid piece and give the gate a frame with a clearly visible opening.

## 2026-11-10 — D16: chart editor (playbook 2.1)

**Done**
- Core.Chart: place (replaces overlaps; gate/wide keep the clicked lane as the opening; all-lane types stored in lane 1), move, remove, snap 1/4 or 1/8, type counts, length in seconds, checks (errors block save; warnings: first 8 beats, different types under 1 beat apart when the spacing toggle is on), JSON writer (2-space indent, sorted by beat then lane, invariant numbers, escaped strings; same format as the existing level files).
- ChartEditorWindow (Shift+Shift > Chart Editor): level dropdown, waveform strip, beat grid with bar numbers, three lane rows, click/drag/right-click editing, play/pause/stop/scrub/loop with the playhead taken from the audio's sample position, Save/Revert, Ctrl+S, Space, 1-8 type keys, problems panel.
- Docs/CHART_EDITOR.md (usage, checks, editor features relied on and Unity 6 risks).

**Tested**
- 116/116 core tests (14 new chart tests, including exact JSON output). Per-assembly compile check now includes the window (stand-ins extended with the editor UI types it uses).

**Not yet run** in Unity: the window, edit-mode audio playback (highest risk), waveform, save/reimport round trip.

## 2026-11-10 — D16: chart editor verified (John)

**Tested (editor)**
- 116/116 tests pass in Unity. Window opens, level00 loads, obstacles drawn in lane rows, edit-mode audio playback works (the highest risk), waveform shows the click spikes exactly on the beat lines.
- Moved obstacles, saved, played: they appear where placed (spawned names like `bar b7 lane 0`).
- Playbook 2.1 question answered: **faster than editing JSON by hand; nothing clumsy.** No simplification needed.
- Day 16 "done when" met. Next per plan: 2.2 palette and post-processing (day 17), including the art-pass note on the wide bar and gate.

## 2026-11-10 — D16: palettes decided; visuals step 1 (playbook 2.2)

**Decisions (John)** — see Docs/PALETTES.md: colour = what you press (six action colours identical on every level), Option 3 skies (pale dawn/overcast, dark others), charcoal road #333333 with light edge and lane lines. Four action colours sit at 3.0-4.3:1 on the road, below the plan's 4.5:1; agreed floor 3:1.

**Done**
- Palette ScriptableObject (warns in the Inspector if an action colour drops below 3:1 on the road); PaletteApplier (camera background, linear fog, recolours runtime copies of mapped materials so assets are never modified by playing; palette picked from the level JSON's "palette" field).
- Core ColorContrast (WCAG) + tests, including the chosen colours on the charcoal road.
- Shift+Shift > Set Up Visuals: six palette assets, edge/lane lines on Straight/Tunnel/Bridge, applier wired into Run, LevelTest and Greybox (Greybox shows "night"), tracks rebuilt, fog enabled in each scene's lighting so builds keep the fog shader variants.

**Tested**
- 120/120 core tests. Per-assembly compile check passes (stand-ins extended: RenderSettings, fog, camera clear, prefab contents).

**Not yet run** in Unity: the menu, the applier, fog, how it looks. Steps 2-5 (bloom/emissive edges, camera FOV/roll + trail, phase look, solid wide bar + gate frame) not started.

## 2026-11-11 — D17: first look at the palettes; camera lowered

**Seen (John)**
- Run scene, dawn: pale sky, charcoal road, edge/lane lines; loop, grades and results work.
- The "black" run without Shift prompt, count-in or scores was the **Greybox** scene (night fallback palette, no conductor/game loop, by design), not the Run scene; the neon edit to test_gestures.json only shows in Run.
- Unity closed abnormally once (scene backup prompt). Cause unknown; kept backups. `/Assets/_Recovery/` is now git-ignored.

**Changed (John: camera too high, the overhang hides the view)**
- The camera sat at 4 m, inside the tunnel ceiling slab (3.5-4.0 m). Lowered to 3.3 m (ChaseCamera default and the three scenes' serialized offset).
- To keep every obstacle below eye level (the palette decision relies on it), both gate prefabs lowered from 3.5-3.7 m to 3.2 m (prefab files and the tool). Tunnel ceiling now passes overhead.
- Per-assembly compile check passes. Not yet seen in Unity.
- Note: a jump raises the camera by half the jump height (0.75 m); inside a tunnel it would clip the ceiling. Avoid gaps/hurdles in tunnels.

## 2026-11-11 — D17: tunnel question, count-in, gap flicker (John)

- "Tunnel gone": the Run scene's track has always been straight pieces only (tunnel exists in the Greybox track). Per-level track pieces come with playbook 2.3 (modular kit). Flagged, not built: automatic tunnel around low bars.
- Count-in read as "counting twice" (two bars of 1-2-3-4). Now: first bar "Get ready" with a pulse per beat, last bar 4-3-2-1, "GO" fading over beat 0.
- Gap "fidgety": z-fighting. The gap's black patch and the new lane lines were at the same height (0-0.02 m). Patch lifted to 0.02-0.04 m and pink (jump colour) rims added at its front and back edges. GapMarker prefab file edited for the height; rims are added by Shift+Shift > Set Up Visuals (re-runnable) and by the tool for new projects.
- Per-assembly compile check passes. Not yet seen in Unity.
- Gap rims and the new count-in confirmed in the editor (John: "all good"). "Get ready" pulsed on each beat and read as appearing 4 times: now it stays steady with four dots lighting one per beat; 4-3-2-1 still pulse.

## 2026-11-11 — D17: glowing edges and bloom (playbook 2.2 step 2)

**Done**
- Six glow materials (LaneGlow … ChargeGlow), emission keyword on in the assets so Web builds keep the emission shader variant.
- Shift+Shift > Set Up Visuals now also: adds a thin glowing top-edge strip to every obstacle block (all obstacle prefabs; slightly wider than the block so no faces coincide), makes the gap rims glow, turns bloom on in PostProcessProfile (0.8 / threshold 0.9 / scatter 0.7, only if still 0), adds a global Volume and enables camera post-processing in Run, LevelTest and Greybox.
- Palette: Glow Intensity (default 1.5) per palette; PaletteApplier sets glow copies' emission = action colour x glow.

**Tested**
- Per-assembly compile check passes (stand-ins extended with Volume, Bloom, camera data, emission).

**Uncertain / not yet run**
- The look in Unity. The Web build uses the Mobile URP asset: if HDR is off there, glow is capped and bloom weaker than in the editor. Check in a Web build.
- Bloom on the light skies (dawn, overcast): threshold 0.9 should keep the pale sky from glowing; verify.

## 2026-11-11 — D17: camera feel and runner trail (playbook 2.2 step 3)

**Done**
- CameraFeel asset (Assets/_Game/Settings/CameraFeel.asset): offset (3.3 m kept), look-ahead, lag, FOV 60 / 72 sprint / 78 phase eased over 150 ms, 2 deg roll into lane changes (proportional to sideways speed, eased 120 ms). On an asset so Play-mode tuning is kept (playbook: visual settings on ScriptableObjects).
- ChaseCamera reads CameraFeel (its own fields moved into the asset; default instance + warning if unassigned).
- Runner trail: TrailRenderer (0.25 s, tapering) with a Sprites/Default material; PaletteApplier colours it accent -> transparent.
- Set Up Visuals now also creates CameraFeel and wires it plus the trail into Run, LevelTest and Greybox.

**Tested**: per-assembly compile check passes.

**Not yet run / uncertain**
- Step 2 (glow, bloom) and step 3 not yet seen in Unity.
- Sprites/Default for the trail under URP: expected to render (unlit, vertex colours); if it shows pink, switch the Trail material to URP Particles/Unlit.

## 2026-11-11 — D17: steps 2 and 3 verified (John)
- Glowing edges, bloom, runner trail, FOV widening (sprint/phase) and lane-change roll seen in the editor: "looks good". No tuning changes requested; defaults kept (glow 1.5, bloom 0.8/0.9/0.7, FOV 60/72/78, roll 2 deg).
- Remaining in playbook 2.2: step 4 phase look (palette inversion + scanlines), step 5 solid wide bar + gate frame.

## 2026-11-11 — D17: steps 4 and 5 (phase look, obstacle shapes)

Done:
- Step 4: PaletteApplier now keeps each material copy with its role and blends every frame toward the phase look while the runner is phased (0.1 s, unscaled time): sky/fog to phase colour x phaseSkyDarken, Ghost role (new, added at the end of the enum) to near-white, other obstacle/glow roles x phaseDim, road/lines unchanged, IMGUI scanline overlay (1x4 repeating texture). New Palette fields: phaseSkyDarken 0.6, phaseDim 0.45, scanlineStrength 0.25. Set Up Visuals maps the Ghost material to the Ghost role and wires the applier's runner.
- Step 5: Set Up Visuals rebuilds WideBar and GateOpening prefabs (once; skipped if Open0 exists) into three layouts Open0/1/2. Wide bar: blocked lanes = solid 2 m slabs to the road edge. Gate: 3 m walls plus a framed doorway (posts 0.25 m, beam with top 3.1 m). Middle opening is necessarily two pieces. ObstacleSpawner activates Open{lane} and hides the others; falls back to the old Panel{lane} hiding for old prefabs. Gameplay rules unchanged.

Tested: per-assembly compile check against stand-ins (stand-in gap fixed: Texture now derives from Object); 120 Core tests pass.
Not run: nothing in Unity yet — the prefab rebuild, the edge strips on the new pieces, the phase blend, the scanlines and their draw order against the HUD are all unverified until John runs Set Up Visuals and plays.
Uncertain: whether scanlines draw over or under the GameFlow text (GUI.depth across components); whether the near-white ghost colour reads well on the dusk/night skies.

## 2026-11-11 — D17: steps 4 and 5 verified

John ran Set Up Visuals and played the Run scene: the phase look and the new wide bar and gate shapes work. Committed on his side. Still open: the scanline draw order against the HUD text and how the ghost colour reads on dusk/night (not tested; the test level is dawn).

## 2026-11-11 — D17: modular track kit (playbook 2.3)

John chose the full kit (bends, rises, drops) over a cut-down straight kit.

Done:
- Core/TrackPath.cs: 12 pieces (straight, bendLeft/Right, sLeft/Right, rise, drop, crest, dip, tunnel, bridge, pillars), each 8 beats. The path is the track's centre line; distance along it is measured on the ground plan, so distance = beat x 5 m everywhere and timing and rules are untouched. Turns and slopes ease in and out.
- Level format: optional `track` list of {beat, piece}; validated (unknown piece, overlap, out of range). Chart keeps it on save and writes it one piece per line; new warnings (gap on a slope, more than 120 degrees one way).
- TrackBuilder rewritten: builds at play time (order -150) from the playing level; road and line meshes generated along the path in 40 m chunks; TunnelShell / BridgeRails / Pillars prefabs repeated every 10 m along their piece. Point() and Rotation() turn (distance, sideways, height) into world space.
- Runner, ChaseCamera, ObstacleSpawner use the track for positions; Obstacle carries its track position (hand-placed Greybox obstacles still use world X/Z on a straight track).
- Tools: Set Up Visuals removes the pieces saved in scenes, wires the builders, creates the dressing prefabs, generates click_100_long.wav, puts test_track.json first in the Run scene's level list. Greybox keeps a gap at 100 m as a GapMarker obstacle (its tunnel is now 60-100 m, bridge 105-145 m).

Tested: 133 Core tests pass (13 new: path geometry, ground distance equals s, flat joins, validation, chart save). Per-assembly compile check passes (stand-in gaps filled: Mesh, MeshFilter, LookRotation, SetPositionAndRotation and others). test_track.json validates and round-trips through the chart writer.
Not run: nothing in Unity. Unverified: the generated road mesh (facing, shading, seams between chunks), lines flicker, camera through bends and over crests, obstacles on slopes, tunnel clearance, restart time with the track built at start, WebGL.
Uncertain: obstacles on a rise may poke above the horizon (the 3.2 m rule assumed a flat road); the camera may see under the road on a crest; fog distance on bends.
Unused now, left in place: TrackPiece.cs and the Straight/Tunnel/Bridge/Gap prefabs (old scenes still reference the script until Set Up Visuals is run).
Flagged, not built: editing track pieces in the chart editor.

## 2026-11-11 — D17: track kit played by John (Run scene, test_track, dusk)

Works: the level plays to the end on the bent and sloped track ("overall the game is good"); no underside of the road seen at the crest; the road and lines render.
Found:
- The blue phase barrier (beat 60, on the crest) reads as "the end of the game": a full-width opaque wall with nothing visible behind or beside it. John's guess: no surroundings to give depth.
- Obstacles poke above the horizon on slopes (expected; the 3.2 m rule assumed a flat road).
Open, waiting for John's choice: see-through barrier, roadside scenery (not in SCOPE.md as written), what to do about the horizon on slopes.

## 2026-11-11 — D17: scenery, see-through barrier, camera on slopes, brick wall

John's choices after playing the track kit: roadside scenery and a see-through barrier ("like car racing games"), camera adjusts on slopes, red wall made of bricks. Scenery recorded in SCOPE.md decisions.

Done:
- Scenery (TrackBuilder): RoadsidePost every 10 m on both sides at 5 m (not in tunnels); BackdropBlock every 25 m on alternating sides, 14-30 m out, width and top height varied by a fixed hash (same every run). Both use the road-detail and line materials, so palettes colour them.
- Barrier: material set to URP transparent, 30% solid; PhaseBarrier prefab gets glowing side posts and sill. PaletteApplier keeps each material's own alpha when recolouring.
- Camera: lifts by slopeLift (0.6) x the height the road gains between the camera and 30 m ahead of the runner; off inside tunnels. Both values in CameraFeel.
- Wall: the tool generates Materials/Greybox/Brick.png (64x32, running bond) and puts it on the Wall material, tiled 7 x 8; the palette's charge red tints it.

Tested: compile check and 133 Core tests.
Not run in Unity: all four. Unverified: transparent material set from script (keywords, sorting against the glow frame and the trail), brick scale, scenery density and frame rate in WebGL, how the lift feels.
Note: Brick.png is created on John's machine and committed from there (LFS does not work from the cloud session).

## 2026-11-11 — D17: scenery second pass (John's feedback after playing)

John: posts looked like floating lights; buildings repetitive; wants trees or shrubs at times; barrier a little less see-through; bricks only visible up close; camera a tad raised throughout, put it back.

Done:
- Ground: a 45 m band each side of the road, 5 cm below the surface, in the road-detail colour; left out under bridges. Posts, plants and buildings now stand on it. No posts on bridges.
- Buildings: six shapes (tower, slab, stepped, twin, spire, arch), picked per spot by a fixed hash, with varied distance (18-34 m), size, sink and quarter-turn. They and the plants stand upright on slopes (yaw only).
- Plants: two blocky trees and a shrub, at about 45% of the 12 m spots, 6.5-11 m from the centre line, random turn and size. New Foliage material, new PaletteApplier role Scenery (accent mixed 55% toward road-detail; unchanged while phased).
- Barrier 50% solid (was 30%). Bricks 2 m x 0.5 m (were 1 m x 0.19 m).
- Camera slope lift off by default (field renamed slopeLiftAmount so the saved 0.6 is dropped). So obstacles on rises poke above the horizon again; John will say if it needs a small value.

Tested: compile check, 133 Core tests. Not run in Unity. Unverified: ground meeting the road edge, the scenery mix by eye, frame rate in WebGL with the extra objects.
Unused now: BackdropBlock prefab.

## 2026-11-11 — D17: scenery third pass

John after playing: posts look planted; buildings varied but not realistic (asked if cosmetics come later); barrier to 65%; red wall "kinda jittery"; camera should lower slightly under the first bridge (read as: the tunnel, the first structure passed under; the pillars archway is fine).

Done: barrier 65% solid. Brick texture regenerated at 256 x 128 with mipmaps, trilinear and anisotropic 8 (guess at the cause: shimmer from thin high-contrast joints). Camera drops CameraFeel.tunnelDrop (0.5 m) while camera or runner is in a tunnel.
Tested: compile check only. Not run in Unity.
Uncertain: whether the jitter was texture shimmer at all (could be the edge strip against the wall face); whether "first bridge" meant the tunnel. In a tunnel the camera (2.8 m) is now below gate tops (3.1 m).
Flagged, not built: realistic buildings and trees would need imported 3D models (not in SCOPE.md; the plan's art direction is abstract shapes).

## 2026-11-11 — D17: scenery fourth pass

John: the camera rises at the pink wall (hurdle, beat 44, just after the tunnel) and should not; buildings still look alike, make 10 more; more trees, conifers.

Done:
- Camera: jump-follow is now a CameraFeel setting, 0 by default (was a fixed 0.5), so the camera stays level on every jump. The tunnel drop eases in over 12 m before a tunnel and out over 12 m after it (it used to snap back at the exit, about 13 m before the hurdle). Not known which of the two John saw; both changed.
- Buildings: 10 more shapes (L, pyramid, needle, cluster, frame, T, wall, stairs, crown, linked towers): 16 in total.
- Plants: four conifers (tiers narrowing to a point, alternate tiers turned 45 degrees); conifers are 6 of the 10 entries the builder picks from. A plant spot every 7 m with a 70% chance (was every 12 m, 45%).

Tested: compile check only. Not run in Unity.
Uncertain: wide buildings at the near distance can reach the plant band (6.5-11 m), so a tree may stand inside a building now and then; frame rate with roughly twice the plants.

## 2026-11-11 — D17: plots and compound walls

John: fourth pass "looks good". Asked for trees beside the buildings or in empty spaces, so it reads as plots (building + trees, or trees only), and a compound wall to tell plots apart.

Done (TrackBuilder scenery placement rewritten; posts unchanged):
- Both sides of the road are divided into plots 30 m long with a 4 m lane between them, from 7.5 m to 40 m out. None where a plot would touch a bridge.
- 55% of plots get one building (same 16 shapes, slightly smaller: 0.75-1.15) set back from the front wall, plus 2-5 plants placed only where the building is not. The rest get 5-9 plants and no building.
- Compound wall per plot: 1.1 m high, generated as a mesh. Front wall (with a 4 m gateway in the middle) and back wall follow the road through bends and slopes; the two end walls run straight out. Road-grey material, one shade lighter than the ground. Faces drawn from both sides.
- The loose band of trees between posts and buildings is gone (all plants are inside plots now).

Tested: compile check, 133 Core tests. Not run in Unity.
Uncertain: wall colour against the ground (#333 on #2A2A2A may be too close); walls on bends; object count (about 20 buildings and 200 plants on the test level) in WebGL.

## 2026-11-11 — D17: plot walls second pass

John: plots look good. Walls only along the road, not around the plot; lit top edge; closed gates instead of openings; no marker posts in front of gates.

Done (TrackBuilder): back and end walls removed. Each plot's road-side wall has a lit strip on top (line material), a closed gate in the middle (4 m, thinner, ground-dark, lit bar on top) between two gate posts 0.5 m taller than the wall, and closed ends. Marker posts are skipped within 3.5 m of a gate's centre.
Tested: compile check only. Not run in Unity.
Uncertain: the lit wall edges run parallel to the road's edge lines 4 m away; they may compete with the lane lines for attention. Both use the same line material, so shared-face seams between wall and end faces rely on RecalculateNormals (possible soft shading at wall ends).

## 2026-11-11 — D17: white screen fixed

John: after the wall change the Game view was white with a few black marks. Nothing lost: his commit holds the code as pushed.
Cause (from reading the code, not reproduced here): the wall end faces added in the last change shared four corners between a front-facing and a back-facing pair of triangles. RecalculateNormals averages the two opposite directions to zero; the lit shader normalises that to an invalid value (NaN); bloom spreads one invalid pixel over the whole screen.
Fix: each side of an end face has its own four corners. The Strip() calls were already safe (every strip adds its own vertices).
Tested: compile check only. Not run in Unity.
Lesson for the mesh code: never let one vertex belong to triangles facing opposite ways.

## 2026-11-11 — D17: walls, gates and white-screen fix verified in the editor

John: "now it looks good" (Run scene, test_track): walls along the road with lit edges, closed gates with taller posts, plots with buildings and trees; the white screen is gone. Next: browser Build And Run to check frame rate, glow and the see-through barrier in WebGL (not done yet).

## 2026-11-11 — D17: first browser build with scenery; gap pit, gate fix, running figure

John ran Build And Run (Chrome, development build): the track test level plays in the browser with scenery, bricks and the tunnel. No frame-rate complaint; build size not reported yet. Feedback:
- Screen "shakes" on quick left-right: that is the step-3 camera roll (2 degrees) and sideways lag; explained, not changed (CameraFeel > Roll Degrees).
- Gap needs depth, maybe water. Gate ("green wall") looks jittery. Wants a running character instead of the capsule, with white smoke from the heels.

Done:
- Gap: TrackBuilder cuts the road (surface, sides, underside, lines) at each gap, with cross-sections exactly at the edges and closed slab ends; the cut matches the spawner through one shared JumpShiftMetres(). GapMarker prefab gets a "Pit" (four dark walls 3 m deep, water surface 1.2 m down, fixed dark blue-green "Water" material). The spawner hides the flat black "Hole" where the road is cut and hides the pit on a bridge (open air). Greybox (no level, no cut) still shows the flat hole.
- Gate: layout v2 (prefabs rebuilt once, marker child "LayoutV2"): walls 2.8 m, posts 3.0 m, beam top 3.15 m, each deeper than the last, so no faces or glow strips share a plane. Guess at the cause of the jitter (coplanar glow strips), not confirmed.
- Runner: RunnerFigure, a figure of boxes (legs, arms on pivots, torso, head) built by the tool. Stride from distance along the track: one stride per beat, feet land with the music. Knees tuck in a jump, upper body leans more in a sprint, all parts take the phase material. Dust: a ParticleSystem set up by the tool, a puff of white squares per footfall. The capsule stays in the scene, switched off; Runner.body points at the figure (slide still squashes it).

Tested: compile check (stand-ins added for ParticleSystem and its modules), 133 Core tests. Not run in Unity.
Uncertain: the cut road and pit by eye, especially on slopes and where a gap crosses a 40 m chunk boundary; the particle setup from script (module properties) and its look in WebGL; limb swing directions; how the squashed figure reads as a slide.
Scope note: the figure and dust were asked for by John; particles were already on the day-27 juice list.

## 2026-11-11 — D17: stuck Shift key in the browser, bend lean, trail off

John (Chrome build): figure and gate look good. Asked if the yellow trail is needed; thought he pressed on time at the gap (beat 68) but failed; wants the runner to lean into bends; sometimes left taps do nothing, "especially in the open stretch".

Finding (from his screenshots, not reproduced here): at the gap failure the debug log's newest entry is the phase hold ending at beat 61: no gesture at all was registered near beat 68. An earlier screenshot shows "R-Shift: DOWN" at beat 54.9 with no gesture logged since the jump at beat 43.8, i.e. a key shown as held for 6 s after a both-press. Reading: on Windows, when both Shift keys are down and one is released, the browser gets no key-up for it; Unity's keyboard state keeps that key down, so its next press has no down edge and is lost. This follows every jump and phase, which matches both complaints. The editor reads the keyboard differently and was not affected.

Done:
- Assets/Plugins/WebGL/ShiftKeys.jslib + Runtime/ShiftKeys.cs: in Web builds ShiftInput reads the two Shift keys from the browser's own key events. On any Shift key-up with no Shift left down, both are cleared. A fresh key-down for a key still marked down counts as release + press. Presses are latched between frames (a tap shorter than a frame still counts). Editor and Windows builds keep the Input System path.
- RunnerFigure leans into bends: 3 degrees of lean per degree the track turns over 6 m, at most 16.
- Runner trail switched off by the tool (object kept).

Tested: compile check for both variants (normal and with UNITY_WEBGL defined); the key script simulated in Node for the lost key-up, re-press, quick tap and blur cases; 133 Core tests. Not run in Unity or a browser.
Uncertain: whether the stuck key is the whole cause; the .jslib meta is minimal (Unity should fill in the importer and include it for Web only). Conductor start, GameFlow restart and the calibration scene still read the Input System directly; a stuck key there only costs one press.
Remaining limit: releasing one Shift while the other stays held is not seen until both are up or that key is pressed again (e.g. going from phase straight into a slide).

## 2026-11-11 — D17: both-press lost in the Web build (my regression), lean reduced

John (Chrome): pressing both Shifts did not jump, three tries. His screenshot at the hurdle (beat 44) logs only "TapRight": the left key's press was never registered. Before the browser key reader, the same spot logged BothTap, so the reader introduced this.
Likely cause (not confirmed on his machine): the reader skipped key-down events flagged "repeat"; with one Shift already down, Windows flags the first press of the other Shift as a repeat. Fix: a repeat is ignored only when that same key is already marked down.
Also: bend lean reduced to 1 degree per degree of turn, at most 5 (was 3 and 16): John found the big lean read like a lane change. Fields renamed so the saved values are dropped.
Added tools/shift-key-test.html: a page that lists the browser's raw Shift key events (code, repeat, shiftKey) for four short tests, to replace guesses about the Windows behaviour with facts.
Tested: compile check; the key script simulated in Node (second Shift flagged repeat counts, true auto-repeat ignored). Not run in a browser.
Uncertain: everything about what Windows and Chrome really send for two Shift keys until John's test page results are in. In particular, tapping one Shift while holding the other (lane change during a slide) may leave the tapped key marked down.

## 2026-11-11 — D17: Web input verified; what Chrome on Windows really reports for two Shift keys

John (Chrome build): "works well now"; test_track completed in the browser with jumps, phase and taps registering.

Facts from tools/shift-key-test.html on his machine (Chrome, Windows):
1. Both together: keydown ShiftRight (repeat false), keydown ShiftLeft 4 ms later with repeat TRUE, then auto-repeat of ShiftLeft, then ONE keyup (ShiftRight, shiftKey false). No keyup for ShiftLeft.
2. Hold Left, tap Right, release Left: keydown ShiftLeft, its auto-repeats, keydown ShiftRight (repeat true), then nothing until ONE keyup (ShiftLeft, shiftKey false) when Left is released. The Right tap's release is never reported.
4. Hold both, release Left, wait, release Right: no event at Left's release; ONE keyup (ShiftRight, shiftKey false) at the end.
(Screenshot 3 was a copy of 2.)

So: the second Shift's key-down is always flagged repeat (confirms the last fix); only the LAST Shift released produces a key-up; the first release is silent. Auto-repeat moves to the most recently pressed key.

Consequence still open: tapping one Shift while holding the other (e.g. changing lane during a slide) cannot be seen as a tap in the browser: the tapped key stays marked down until the held key is released, so it is read as a hold (and both down). Proposed to John, not built: in Web builds, a second Shift pressed more than the both-window (80 ms) after the first counts as a tap straight away. Windows desktop builds are not affected.

## 2026-11-11 — D17: Web tap-during-hold workaround

John agreed to the workaround. Browser felt smooth with all scenery (his words); build size still not known. The red Console error (UnityConnectWebRequestException: Token Exchange failed) is the editor's own Unity account service, not project code.
Done: in the Web path of ShiftInput, a Shift pressed while the other has been held longer than bothWindowMs is fed to the classifier as press + release at once (a tap) and ignored until the browser shows it up. Documented in Docs/INPUT.md with the limits.
Tested: compile check for both variants; new Core test (zero-length tap during a hold fires the tap, no both gesture, the hold goes on); 134 Core tests pass. Not run in a browser.

## 2026-11-11 — D17: Web tap-during-hold verified; art block closed; level 1 drafted

John (Chrome build): "everything works" (tap during a hold, jump, phase); asked to move on. Days 16-17 block done: palettes, glow, camera feel, phase look, shapes, track kit, scenery, running figure, Web input.
Still not known: the Web build's size (limit 20 MB compressed). Not done: Edge calibration redo; Release Web profile build.

Level 1 "Warm-up" drafted a day early (plan: day 18): Assets/_Game/Levels/level01.json, see Docs/LEVELS.md. Lane changes only, quarter notes, first 8 bars one tap each, 2-bar breather before the fastest section (plan's authoring rules). Set Up Visuals now orders the Run scene's levels: level01, test_track, test_gestures, level00.
Tested: level validates, strict chart check has no warnings, a scripted walk-through needs at most two taps per obstacle with at least one beat per tap, file round-trips through the chart writer. Compile check passes.
Not run in Unity. Whether it is fun, readable and the right difficulty is John's call after playing (rule of thumb: clear within 5 attempts).

## 2026-11-11 — D17: level 1 played; level 2 drafted

John: cleared level 1 on the first attempt; visuals "look good, realistic with lighting". Asked if level 1 should have only one type of obstacle: yes by the plan (level 1 = Tap L and Tap R only; bar and wide are both lane changes, same orange).
Build folder size (development Web build): 59.4 MB uncompressed on disk. Not comparable with the 20 MB compressed limit: needs a Release profile build to measure. Flagged as a risk to check before day 24, not day 28.
Noticed, not changed: his clean run scored 50 with best streak 1 and 0% perfect. By design a dodge made away from the obstacle's beat grades "Ok" (no points, streak reset); only moves within 120 ms of the beat score. On a lane-change level that makes a clean run look like a bad one. This is the open M1 question "when should the click land"; needs John's decision.

Level 2 "Low ceiling" drafted: Assets/_Game/Levels/level02.json (Docs/LEVELS.md). Set Up Visuals generates click_110.wav and puts level02 first in the Run scene's list.
Tested: validates, strict chart check clean, scripted walk (gate openings always in the player's lane, holds at least 4 beats apart, nothing under 2 beats), round-trips through the chart writer. Compile check passes. Not played.

## 2026-11-11 — D17: scoring: every obstacle passed scores

John's decision: points for every obstacle passed, Ok 5, Good 20, Perfect 50, the on-beat grades being the bonus.
Done: ScoreKeeper values 50 / 20 / 5 (were 100 / 50 / 0), all times the streak multiplier. An OK no longer resets the streak; it neither adds to it nor breaks it (my choice, so the streak and multiplier stay a reward for on-beat play; one line to change if John wants passes to build the streak too). Best-score keys moved to ShiftShift.BestV2.* so bests under the old values are not compared with new ones. Docs/SCORING.md updated.
Tested: 136 Core tests pass (3 scoring tests rewritten, 2 added: a clean off-beat run of 27 scores 135; an OK uses the multiplier). Compile check passes. Not run in Unity.

## 2026-11-11 — D17: camera lowered to 2.8 m

John (level 2, editor): the gate's top beam (2.95-3.15 m) was level with the camera (3.3 m), looked like it hit the camera and blocked the view of the runner. Asked for the camera to come down so the beam passes overhead. His screenshot also shows the new scoring counting up (115 points at beat 41).
Done: CameraFeel offset y 3.3 -> 2.8 (asset and code default); tunnelDrop default 0.5 -> 0 (2.8 m is the height he already approved inside the tunnel). Docs/PALETTES.md notes that the day-16 rule "every obstacle top below the camera" no longer holds for the 3 m obstacles.
Tested: compile check only. Not run in Unity.

## 2026-11-11 — D17: camera closer after the height change

John (level 2, editor, camera at 2.8 m): the gate beam now passes overhead, but the runner "looks a little far ahead", so judging the distance to an obstacle is tricky. (The earlier ArgumentNullException "clock" was scripts reloading during Play mode after a pull, not a game bug; advised Recompile After Finished Playing.)
Done: CameraFeel offset z -7 -> -5.5 (closer behind the runner) and lookAhead 8 -> 6 (aims nearer, so the view tilts down about 3 degrees more and shows more road around the runner). Asset and code defaults.
Tested: compile check only. These are first guesses by geometry, not by eye; John can tune Offset Z and Look Ahead in the CameraFeel asset while playing.

## 2026-11-11 — D17: camera settled; level 3 drafted; release build requested

John: "camera feels right now" (2.8 m up, 5.5 m behind, aiming 6 m ahead). Asked for level 3 or the build; doing both: he runs Shift+Shift > Build WebGL (release, Brotli, prints the size against the 20 MB budget) while level 3 is drafted. Level 2 attempts not reported yet.
Level 3 "Gaps" drafted: Assets/_Game/Levels/level03.json (Docs/LEVELS.md). Set Up Visuals generates click_120.wav and puts level03 first in the Run scene's list.
Tested: validates, strict chart check clean (no gap-on-slope warning), scripted walk (jumps at least 4 beats apart, nothing under 2 beats, two-tap moves have 4 beats), round-trips through the chart writer. Compile check passes. Not played.

## 2026-11-11 — D17: release Web build size measured

John ran Shift+Shift > Build WebGL (release, Brotli + decompression fallback): "[Build] WebGL OK in 03:54 -> Builds/WebGL, 9.6 MB on disk (plan budget: under 20 MB)". About 10 MB of headroom. Contents at the time: levels 1-2 with placeholder clicks, all scenery, no music yet. The earlier 59.4 MB was the development build, uncompressed.
Budget note for day 23: six music tracks have to fit in roughly 10 MB together (about 1.5 MB each after Unity's compression), or fewer tracks get reused (cut order item 4).
Not yet done by John: Set Up Visuals + play for level 3 (his pull shows the level arrived; nothing to commit yet).

## 2026-11-11 — D17: levels 2 and 3 played; ghost-layer chart types; level 4 drafted

John: level 2 cleared on attempt 4, level 3 on attempt 4 (plan's limit: 5). Left as they are; tester feedback on days 25-26 decides.

The plan's level 4 needs "two overlapping obstacle layers"; the chart had no way to place a ghost-layer block. Added two chart types (ObstacleTypes.Cores): ghostbar (barrier + core in the event's lane) and ghostwide (barrier + cores in the other two lanes). They fill their beat in the chart like gate and wide; the spawner places the barrier and one GhostBlock per core, each judged on its own; the chart editor draws core lanes lighter. Ghost-layer blocks are now a lighter tint of the phase colour (were identical to the barrier), so cores can be seen before phasing. Set Up Visuals now also refreshes the spawner's type list.
Level 4 "Ghost layer" drafted: Assets/_Game/Levels/level04.json (Docs/LEVELS.md), click_125 generated by the tool, level04 first in the Run scene's list.

Tested: 137 Core tests (new: type specs, chart placement of a barrier with cores). Level validates, strict check clean, scripted walk over every lane the player could be in (gate lanes certain, at most two taps, a lane change before a phase always has 4 beats), round-trips. Compile check passes. Not played.
Uncertain: how a light core reads inside the 65%-solid barrier, in the night palette, before and during phase; each core scoring as its own obstacle (extra points per barrier); difficulty.

## 2026-11-11 — D17: "ghost block", light pink

John asked what "cores" were; chose the name "ghost block" and light pink for them.
Done: renamed in code (ObstacleTypes.GhostBlocks, spawner field ghostBlockPrefab), comments, chart editor and Docs/LEVELS.md. Ghost blocks are light pink (#FFB5D9) on every level, not palette-driven; brighter (half-way to white) while phased; the chart editor draws their lanes in the same pink.
Flagged to John: pink is the jump colour (hurdle, gap rims) under the "colour = what you press" rule; the ghost block is a paler pink inside a blue wall. To watch when playing.
Tested: compile check, 137 Core tests. Not run in Unity. The spawner's prefab field was renamed, so Set Up Visuals must be run before level 4 plays.

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

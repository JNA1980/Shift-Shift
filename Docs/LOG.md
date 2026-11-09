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
- Playbook alignment pending (see impact list in session): GestureSettings ScriptableObject, IClock, event shape, alternate-on-beat, 0.3 project config, 0.4 PowerShell upload script.

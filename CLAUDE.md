# Project rules

Project: Shift+Shift, a Unity 6 URP rhythm auto-runner for GitHub Game Off 2026 (theme SHIFT). Solo developer plus you. Deadline 1 Dec 2026. Primary target WebGL, secondary Windows x64.

Controls: Left Shift and Right Shift only. Seven gestures: Tap L, Tap R, Hold L (slide), Hold R (sprint), Both tap within 80 ms (jump), Both hold (phase shift), Alternate L R L R quickly, no beat needed (charge; changed by ELIA on day 13). Thresholds live in a ScriptableObject, never hard-coded.

Architecture: single Unity project, plain C#, no third-party frameworks. Systems: ShiftInput, Conductor, TrackBuilder, ObstacleSpawner, Runner, Judge, Scoring, GameFlow, ChartEditor (editor only). Level data is JSON in Assets/_Game/Levels. All game code under Assets/_Game/Scripts.

Rules you must follow:
- Before changing an existing script, list the scripts and scenes that depend on it and what could break.
- Never assume code works because it compiles. Say what you have not run.
- Audio timing uses AudioSettings.dspTime and PlayScheduled; never Time.time for anything rhythm-related.
- No new feature without me asking for it. If you think something is missing, say so in one line; do not build it.
- Scope is fixed in Docs/SCOPE.md. Anything outside it is refused with a one-line note.
- Commit after each completed step with a message starting with the day number, e.g. "D09: input classifier".
- Keep Docs/LOG.md: append a dated entry after each step with what was done, what was tested, what is uncertain.
- WebGL constraints: no threads, no System.IO at runtime, audio needs a user gesture to start, keep the build under 20 MB compressed.

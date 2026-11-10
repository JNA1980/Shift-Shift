# Judging, scoring and the run loop (playbook 1.4)

Code: `Assets/_Game/Scripts/Core/Judging.cs` (Judge, ScoreKeeper, Calibration), answers in `RunnerModel.ResolveWithAnswer`,
loop in `Assets/_Game/Scripts/Runtime/GameFlow.cs`. Windows in `GestureSettings` (perfect 50 ms, good 120 ms).

## Grades

Each obstacle is graded once, by the **key-down time** of the gesture that answered it, against the obstacle's beat.

| Obstacle | Answer that is timed |
|---|---|
| bar | the lane change out of its lane |
| low / gate / ghost (phase barrier) | start of the slide / sprint / phase hold covering it (phase only answers the barrier) |
| gap | the jump. **A gap's beat is the take-off**: the hole is placed half a jump after the beat, so jump on the click |
| wall | the tap that completed the charge (may land up to the Good window after the wall's click) |

| Grade | When | Points | Streak |
|---|---|---|---|
| Perfect | within 50 ms | 100 x multiplier | +1 |
| Good | within 120 ms | 50 x multiplier | +1 |
| OK | passed, answer outside 120 ms (e.g. dodged two seconds early, sprint held from far back) | 0 | reset |
| (none) | no action was needed (bar in another lane) | 0 | unchanged |
| Miss | collision; the run ends | 0 | reset |

Multiplier by streak: x2 from 10, x3 from 25, x4 from 50 (the 10th hit already scores x2).
Results: score, best streak, perfect % (perfects / graded obstacles), best score per level in PlayerPrefs
(`ShiftShift.Best.<level asset>`).

## Run loop (Run scene)

Boot → Run. "Press Shift to start" → 0.5 s → two-bar count-in with pulsing numbers → level audio starts on beat 0 →
fail: white flash + 60 ms hit-stop, then any Shift restarts (scene reload, starts immediately) → finish: results, any
Shift plays again.

## Latency calibration (F2 on the debug overlay)

20 clicks at 100 BPM, first 4 are practice. Mean of (tap − click) over the rest, stray taps ignored. Saved as
`ShiftShift.UserOffsetMs`; the Conductor delays song time by it, so visuals and judging line up with what the player
hears. It cannot separate audio delay from the player's own habit of tapping early/late; that is fine for play.

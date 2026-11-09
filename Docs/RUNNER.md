# Runner rules (Runner / RunnerModel)

Code: `Assets/_Game/Scripts/Core/RunnerModel.cs` (rules, engine-free, tested), `Assets/_Game/Scripts/Runtime/Runner.cs`
(feeds gestures, moves visuals, judges obstacles). Test scene: `Shift+Shift > Create Greybox Scene`.

## How obstacles are judged

- **By rules, not physics.** When the runner reaches an obstacle (song time = obstacle Z / speed), the state at that
  moment decides: Pass, Hit (run fails) or Break (wall). Colliders are not used.
- **Decision A (John, day 10): actions count from key-down.** A slide that was pressed before a low bar passes it,
  even though the slide is only recognised 250 ms after the press. If a press made before the obstacle is still
  undecided, the Runner waits for it (at most one tap/hold threshold), so a fail can show up to ~250 ms late.
- **Visuals start on recognition** (lane ease 120 ms, jump arc 0.5 s, slide pose), so they can lag the rules slightly.

| Obstacle | Passes when | Gesture |
|---|---|---|
| Block (one lane) | runner in another lane | Tap L / Tap R |
| Low bar | sliding | Hold L |
| Gate (all lanes) | sprinting | Hold R |
| Gap (all lanes) | in the air at both edges | Both tap (0.5 s jump) |
| Wall (all lanes) | a charge is available (used up) | Alternate × 4 on the beat |
| Solid layer | runner phased → ignored | Both hold |
| Ghost layer | only hits while phased | (let go of phase, or change lane) |

Lane comes from the obstacle's X (lanes at -2, 0, 2 m), timing from its Z.

## Decisions taken while building (flag if you disagree)

1. **Sprint does not change forward speed.** The playbook says "speed x1.5", but obstacle timing comes from song
   time; actually speeding up would make every later obstacle arrive off the beat. Sprint = state for gates + FOV
   kick (60 → 72). Confirmed by John, day 11: keep it.
2. **A charge lasts until the next wall uses it.** Every 4th on-beat alternation earns one.
3. **Walls are in the solid layer**, so a phased runner also passes them. Levels should not overlap phase and walls.
4. **Gaps are checked at both edges**, other obstacles at their centre.
5. **Charge taps also change lanes** (open question 1 in INPUT.md). Walls span all lanes, so it only looks odd.
6. Camera smoothing uses frame time (visual only, not rhythm timing).

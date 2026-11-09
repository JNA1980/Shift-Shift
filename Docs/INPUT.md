# Input rules (ShiftInput / GestureClassifier)

Code: `Assets/_Game/Scripts/Core/GestureClassifier.cs` (engine-free), `Assets/_Game/Scripts/Runtime/ShiftInput.cs`.
Tests: `Assets/_Game/Tests/EditMode/`. Thresholds: `Assets/_Game/Settings/GestureSettings.asset` (edit in the
Inspector; changes apply live in Play mode). Values are the plan's starting points; tune on day 15.

| Setting | Default | Meaning |
|---|---|---|
| Tap/hold | 250 ms | Press this long = hold; shorter = tap |
| Both window | 80 ms | Second key-down this soon after the first (first still held) = a "both" gesture |
| Alternate beat tolerance | 0.25 beats | How far from a beat line a charge tap may land |

## Events (ShiftInput)

All times are **song time** in seconds from `ShiftInput.Clock` (`IClock`). Until the Conductor exists it is
`DspClock`, a dspTime stub at the BPM set on ShiftInput.

- `GestureFired(type, songTime)`: once per gesture. Time is the **key-down**, so judge with it.
- `HoldStarted(type, songTime)` / `HoldEnded(type, songTime)`: HoldLeft, HoldRight, BothHold.
  Start time = key-down, end time = key-up.
- `AlternateStreak`: current charge count. `GestureFired(Alternate)` fires on each step from 2 up.

## How each gesture is decided

- **Tap** is decided on key-up (it might still become a hold), timed at key-down.
  Lane changes therefore show one press-length late (~60–107 ms measured). John, day 10: feels fine, keep.
- **Hold** fires when the threshold passes while still held, timed at key-down.
- **Both** is decided as soon as the second key goes down. First key-up before the threshold = BothTap;
  otherwise BothHold until the first key-up.
- **Lockout after a both-gesture:** the key still held, and any re-press, are ignored until both keys are up.
  Otherwise letting go of one key at the end of a phase would become a sprint or slide.
- **Alternate (decided day 10, "Option 1"):** a tap counts if its key-down is within the tolerance of a beat
  line (quarter notes). The streak continues when the next counting tap is on the *other* side and on the
  *next* beat. An off-beat tap, same-side tap, skipped beat, hold or both-gesture breaks it. If the next beat
  passes with no key down, the streak expires to 0. Charge = streak 4.
  Every Alternate tap is also a TapLeft/TapRight (see open question 1).
- Key repeat is ignored. Losing window focus releases everything (WebGL alt-tab safety).

## Open questions

1. **Alternate also moves lanes.** L,R,L,R taps are also lane taps, so the runner wiggles while charging.
   Options: the Runner ignores lane taps while a Wall is within N beats, or accept the wiggle (it ends in the
   start lane). Decide before level 5 is authored.
2. **Hold start vs. obstacle.** A slide is only known 250 ms after key-down. Runner collision should honour
   the key-down time or players must press 250 ms early. Decide on day 11 (Runner).
3. **Eighth-note charges.** Level 5 places walls on eighths. If charges must be played on eighths too, the beat
   grid becomes a per-level setting. Not needed yet.
4. Key timestamps are frame-quantised (~16 ms at 60 fps), and dspTime moves in audio-buffer steps.
   Measure on day 13.

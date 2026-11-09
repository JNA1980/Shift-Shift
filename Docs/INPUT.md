# Input rules (ShiftInput / GestureClassifier)

Code: `Assets/_Game/Scripts/Core/GestureClassifier.cs`. Tests: `Assets/_Game/Tests/EditMode/`.
Values are the plan's starting points (tune on day 15 in the `ShiftInput` inspector).

| Setting | Default | Meaning |
|---|---|---|
| Both window | 80 ms | Second key-down this soon after the first (first still held) = a "both" gesture |
| Hold threshold | 250 ms | Press this long = hold; shorter = tap |
| Alternate max gap | 600 ms | Max gap between alternating tap onsets for the charge streak to continue |

## How each gesture is decided

- **Tap** is emitted on key-up, but its `Time` is the key-down. The Judge should use `Time`.
- **Hold start** is emitted when the threshold passes while still held; `Time` is still the key-down.
- **Both** is decided as soon as the second key goes down. First key-up before the threshold = BothTap;
  otherwise BothHoldStart, and BothHoldEnd on the first key-up.
- **Lockout after a both-gesture:** the key still held, and any re-press, are ignored until both keys
  are up. Otherwise letting go of one key at the end of a phase would turn into a sprint or slide.
- **Alternate(n)** is emitted after each tap that switches side within the gap. n = 2 for L,R; the
  wall needs n = 4. Holds and both-gestures reset the streak. "On the beat" is the Judge's job, not this class.
- Key repeat is ignored. Losing window focus releases everything (WebGL alt-tab safety).

## Open design questions (decide by feel, day 15)

1. **Lane-change latency.** Taps only resolve on key-up, so a lane change shows up one press-length late
   (typically 60–150 ms). Judging is unaffected (uses key-down time), but it may *feel* laggy.
   Option if it does: move the lane at key-down + both window and treat a later hold as "move + slide".
2. **Alternate also moves lanes.** L,R,L,R taps are also TapLeft/TapRight, so the runner wiggles across
   lanes while charging. Options: the Runner ignores lane taps while a charge wall is within N beats, or
   accept the wiggle (it ends in the start lane). Needs a decision before level 5 is authored.
3. **Hold start vs. obstacle.** A slide is only known 250 ms after key-down. Runner collision should
   honour the onset time (key-down) or players must press 250 ms early.
4. Key timestamps are frame-quantised (~16 ms at 60 fps). Measure on day 13 with the latency test.

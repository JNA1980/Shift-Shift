# Input rules (ShiftInput / GestureClassifier)

Code: `Assets/_Game/Scripts/Core/GestureClassifier.cs` (engine-free), `Assets/_Game/Scripts/Runtime/ShiftInput.cs`.
Tests: `Assets/_Game/Tests/EditMode/`. Thresholds: `Assets/_Game/Settings/GestureSettings.asset` (edit in the
Inspector; changes apply live in Play mode). Values are the plan's starting points; tune on day 15.

| Setting | Default | Meaning |
|---|---|---|
| Tap/hold | 250 ms | Press this long = hold; shorter = tap |
| Both window | 80 ms | Second key-down this soon after the first (first still held) = a "both" gesture |
| Alternate max gap | 400 ms | Max time between alternating taps for the charge streak to continue |

## Events (ShiftInput)

All times are **song time** in seconds from `ShiftInput.Clock` (`IClock`). Until the Conductor exists it is
`DspClock`, a dspTime stub at the BPM set on ShiftInput.

- `GestureFired(type, songTime)`: once per gesture. Time is the **key-down**, so judge with it.
- `HoldStarted(type, songTime)` / `HoldEnded(type, songTime)`: HoldLeft, HoldRight, BothHold.
  Start time = key-down, end time = key-up.
- `AlternateStreak`: current charge count. `GestureFired(Alternate)` fires on each step from 2 up.

## How each gesture is decided

- **Tap** is decided on key-up (it might still become a hold), timed at key-down.
  Lane changes therefore show one press-length late (~60–107 ms measured). ELIA, day 10: feels fine, keep.
- **Hold** fires when the threshold passes while still held, timed at key-down.
- **Both** is decided as soon as the second key goes down. First key-up before the threshold = BothTap;
  otherwise BothHold until the first key-up.
- **Lockout after a both-gesture:** the key still held, and any re-press, are ignored until both keys are up.
  Otherwise letting go of one key at the end of a phase would become a sprint or slide.
- **Alternate (changed day 13, ELIA: option A):** quick alternating taps, **no beat needed**. A tap continues the
  streak when it is on the *other* side and its key-down is within 400 ms of the previous tap's. A same-side tap or
  a longer gap restarts at 1; a hold or both-gesture resets to 0; the streak expires 400 ms after the last tap.
  Charge = streak 4 (Charge Taps). Every Alternate tap is also a TapLeft/TapRight (see open question 1).
  (Day 10 to 13 it was on-beat only; it proved too hard to land without practice.)
- Key repeat is ignored. Losing window focus releases everything (WebGL alt-tab safety).

## Open questions

1. **Alternate also moves lanes.** L,R,L,R taps are also lane taps, so the runner wiggles while charging.
   Options: the Runner ignores lane taps while a Wall is within N beats, or accept the wiggle (it ends in the
   start lane). Decide before level 5 is authored.
2. **Hold start vs. obstacle.** A slide is only known 250 ms after key-down. Runner collision should honour
   the key-down time or players must press 250 ms early. Decide on day 11 (Runner).
3. ~~Eighth-note charges~~: no longer applies (charge has no beat requirement since day 13).
4. Key timestamps are frame-quantised (~16 ms at 60 fps), and dspTime moves in audio-buffer steps.
   Measure on day 13.

## Web builds: two Shift keys in the browser (day 17)

Measured on ELIA's machine (Chrome, Windows) with `tools/shift-key-test.html`:
- The second Shift's key-down is flagged "repeat" even on its first press.
- Only the last Shift released produces a key-up. The first release is silent.

So in Web builds ShiftInput reads the keys from the browser itself (`Assets/Plugins/WebGL/ShiftKeys.jslib`, `ShiftKeys.cs`) with these rules:
- A key-up with no Shift left down clears both keys.
- A repeat is ignored only when that same key is already marked down.
- A Shift pressed while the other has been held longer than the both-window (80 ms) counts as a tap at once, because its release cannot be seen.

What a Web build therefore cannot do: start a second hold while the first is still held (e.g. sprint without letting go of a slide), or notice that one of two held keys was let go before the other. Jump and phase are not affected (their presses land together). Windows desktop builds and the editor use the Input System and have neither limit.

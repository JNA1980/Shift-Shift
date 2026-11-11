# Level format (Assets/_Game/Levels/levelNN.json)

Playbook 1.3. Loaded at runtime from a TextAsset with Unity's JsonUtility (WebGL-safe). Validated on load; errors are
logged and the level does not start.

```json
{
  "name": "Level 00 - Click test",
  "bpm": 100.0,
  "audioClip": "click_100",
  "offsetMs": 0.0,
  "palette": "dawn",
  "lengthBeats": 32.0,
  "events": [ { "beat": 8.0, "lane": 0, "type": "bar" } ]
}
```

| Field | Meaning |
|---|---|
| bpm | Fixed tempo (one tempo change for level 6 comes later) |
| audioClip | Path inside a `Resources` folder, no extension. Music lives in `Assets/_Game/Audio/Resources/` |
| offsetMs | Where beat 0 is in the audio file |
| lengthBeats | Run ends at this beat |
| events | Sorted by beat. `lane` 0 = left, 1 = middle, 2 = right (ignored for all-lane types) |

| type | Obstacle | Lanes | Passed by |
|---|---|---|---|
| bar | Block | one | being in another lane |
| wide | Wide bar (two lanes) | `lane` = the **free** lane | being in the free lane |
| low | Low bar | all | slide (Hold L) |
| gate | Gate with a one-lane opening | `lane` = the **opening** | being in the opening **and** sprinting (Hold R) |
| gap | Hole in the floor (2.5 m) | all | jump (Both tap) |
| hurdle | Short **pink** wall | all | jump (Both tap); placed half a jump after its beat, like gaps |
| ghost | Phase barrier | all | phase (Both hold) |
| wall | Tall **red** wall | all | charge (L, R, L, R quickly, any rhythm; Charge Taps 4, gap 400 ms in GestureSettings) |

Position: Z = beat x the Runner's metres per beat (5), so every obstacle is reached exactly on its beat.

**Not in the format yet:** ghost-layer obstacles (hit only while phased, the purple block in the Greybox). Level 4's
"two overlapping layers" needs them; add an optional `"layer": "ghost"` field when level 4 is authored.

## Track pieces (day 17, playbook 2.3)

A level can shape its track with an optional `track` list. Everything not covered by a piece is straight road.

```json
"track": [
  { "beat": 8.0, "piece": "bendLeft" },
  { "beat": 16.0, "piece": "tunnel" }
]
```

Every piece is 8 beats (40 m) long and starts on its `beat`. Pieces must be in order and must not overlap.

| Piece | What it does |
|---|---|
| straight | Plain road (the default; no need to list it) |
| bendLeft, bendRight | Turns 20 degrees and stays turned |
| sLeft, sRight | Swings out and back: the road shifts sideways, same heading after |
| rise, drop | 4 m up or down, and stays there |
| crest, dip | 2.5 m hill or hollow, back to the same height |
| tunnel | Straight road inside a tunnel |
| bridge | Straight road with rails and piers |
| pillars | Straight road under a row of archways |

Rules:
- Bends and slopes never change timing: an obstacle on beat N is reached on beat N whatever the track does.
- Obstacles are placed and turned along the track, so they sit square across the road on bends and slopes.
- The chart editor keeps the `track` list when it saves but cannot edit it yet: edit it in the JSON file.
- The chart check warns about a gap on a sloped piece (the flat hole may not sit flush) and about more than 120
  degrees of turn one way (the track may cross itself).
- Test level: `test_track.json` uses every piece once, with one obstacle on each.

## Level 1, "Warm-up" (first draft, day 17)

`level01.json`: 100 BPM, 104 beats (62 s), dawn palette, lane changes only (bar and wide). Placeholder audio: the long click.

| Beats | What happens |
|---|---|
| 0-8 | Nothing: time to settle |
| 8-36 | One obstacle every 4 beats, one tap each: bar in the middle, then wide bars that pull you back and forth |
| 40-56 | Two taps in a row (still 4 beats apart), then two dodges 2 beats apart |
| 57-67 | Breather, inside the tunnel |
| 68-96 | One obstacle every 2 beats; one two-tap move at beat 88 |
| 96-104 | Run-out |

Track: right bend 16, left bend 32, hill 48, tunnel 60, S-bend 72, archways 88.
Checked by script: valid, no warnings in the strict chart check, never more than two taps for one obstacle, never less than one beat per tap.

## Level 2, "Low ceiling" (first draft, day 17)

`level02.json`: 110 BPM, 112 beats (61 s), overcast palette. New: slide (hold Left under a yellow low bar) and sprint (hold Right through a cyan gate). Placeholder audio: `click_110`.

| Beats | What happens |
|---|---|
| 8-20 | Slide only, every 4 beats, inside a tunnel |
| 26-38 | Sprint only, every 4 beats; the gate's opening is straight ahead |
| 42-54 | Slide and sprint in turn |
| 60-80 | Lane changes join in, 4 beats apart |
| 81-87 | Breather, inside the second tunnel |
| 88-108 | Something every 2 beats: a lane change between each slide and sprint |

Rules kept: a gate's opening is always the lane the player is already in (never a lane change and a sprint for the same obstacle); slides and sprints are never closer than 4 beats to each other; nothing is closer than 2 beats.
Track: tunnel 8, left bend 24, dip 40, S-bend 56, right bend 72, tunnel 80, archways 96.

## Level 3, "Gaps" (first draft, day 17)

`level03.json`: 120 BPM, 152 beats (76 s), dusk palette. New: jump (both Shifts together) over gaps and pink hurdles. Combines lanes, slides and jumps; no sprint gates. Placeholder audio: `click_120`.

| Beats | What happens |
|---|---|
| 8-36 | Jump only, every 4 beats: gaps and hurdles in turn |
| 40-72 | Jumps and lane changes in turn, 4 beats apart (the bridge is at 48-56, the hill at 64-72) |
| 76-100 | Slides come back, between jumps |
| 101-111 | Breather, inside the tunnel |
| 112-144 | Something every 2 beats: jump, lane change, slide |
| 144-152 | Last hurdle, then a run-out up a rise |

Rules kept: jumps are never closer than 4 beats to each other; nothing is closer than 2 beats; a two-tap lane move always has 4 beats; no gap sits on a slope or on the bridge (so every gap shows its pit and water).
Track: left bend 16, right bend 32, bridge 48, hill 64, dip 84, tunnel 102, S-bend 116, archways 132, rise 144.

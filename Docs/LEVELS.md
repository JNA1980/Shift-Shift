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

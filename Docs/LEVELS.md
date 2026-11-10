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

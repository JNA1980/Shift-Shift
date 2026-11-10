# Chart editor (playbook 2.1)

**Shift+Shift > Chart Editor.** Code: `Assets/_Game/Scripts/Editor/ChartEditorWindow.cs` (drawing, mouse, audio) and
`Assets/_Game/Scripts/Core/Chart.cs` (place/move/remove, snapping, checks, JSON writer; unit-tested).

## Use

| Action | How |
|---|---|
| Open a level | **Level** dropdown (lists `Assets/_Game/Levels/*.json`) |
| Place | pick a **Type** (or keys **1-8**), click a lane row on a beat line |
| Move | drag an obstacle to another beat/lane |
| Remove | right-click it |
| Snap | **Snap 1/4** (whole beats) / **Snap 1/8** toggle |
| Play / pause | **Play** button or **Space**; **Stop** returns to the start (or the loop start) |
| Scrub | click the ruler (top strip) |
| Loop | drag across the ruler to select a range, turn **Loop** on |
| Save | **Save** or **Ctrl+S**: writes sorted, readable JSON and reimports it. Refused while there are errors |
| Revert | reload the file, dropping unsaved edits |

- Gate / wide bar: the lane you click is the **open** lane; the other two are drawn blocked.
- All-lane types (low, gap, hurdle, ghost, wall) fill the whole beat and replace anything on it.
- The first 8 beats are tinted (warning zone). The red line is the level's end (lengthBeats).

## Checks
- **Errors** (save refused): the level's own validation (sorted, lanes 0-2, known types, inside the level, no
  duplicates) plus two obstacles on one beat where one spans the beat.
- **Warnings** (save allowed): an obstacle in the first 8 beats; with *Spacing check (levels 1-4)* on, two different
  types less than 1 beat apart (exactly 1 beat is fine).

## Editor features relied on, and Unity 6 risks
| Feature | Risk |
|---|---|
| EditorWindow + IMGUI drawing | low |
| `AudioClip.GetData` for the waveform | medium: needs the clip decompressed in memory; otherwise no waveform, editing still works |
| Hidden `AudioSource` playing **in edit mode**, positioned by `timeSamples` | highest: not verified here. If silent, check the Game view's *Mute Audio*; fallback is Unity's internal preview player |
| `EditorApplication.update` repaint while playing | low |
| `AssetDatabase.FindAssets` / `ImportAsset`; file writes (editor only) | low |

# Menu and flow (playbook 2.5)

Everything is driven by the two Shift keys, plus Esc for pause. Keys are read through ShiftInput, so Web builds use the browser key reader (Docs/INPUT.md).

| Screen | Left Shift | Right Shift | Both together | Esc |
|---|---|---|---|---|
| Menu | Previous entry | Next entry | Open the entry | |
| Credits | Back | Back | Back | |
| Playing / count-in | (gameplay) | (gameplay) | (gameplay) | Pause |
| Paused | Resume | Restart the level | Menu | Resume |
| Failed | Retry at once | Retry at once | Retry at once | Menu |
| Results | Retry | Next level (menu after the last) | Menu | Menu |

Rules of the menu keys (`Core/ShiftMenuKeys.cs`, tested): one key on its own counts when it is let go; both down at once counts the moment the second goes down; after a "both", nothing counts until every key is up. A key still held when a screen appears is ignored until it is let go.

Flow: Boot -> Menu -> Run (a level by name, started at once) -> results -> next level, retry or Menu. Calibrate is a menu entry and returns to the menu. All six levels are open from the start.

Menu entries: the levels in play order (name, length, best score), "Calibrate timing", "Credits". The credits text is on the Menu object in the Menu scene (Inspector). The menu shows the Sticky Keys tip from the plan.

Pause: the Conductor freezes the song clock and pauses the audio; on resume the song's start moves forward by the time spent paused. If the pause came during the count-in, the audio start is scheduled again.

Developer overlays (gesture log, runner state box): shown by default in the editor only; F1 toggles them anywhere.

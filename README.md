# Shift+Shift

**Two buttons. Both of them are Shift.**

A 3D rhythm auto-runner for [GitHub Game Off 2026](https://itch.io/jam/game-off-2026) (theme: SHIFT).
Play it on itch.io: _link goes here once the page is public_.

## Controls

| Input | Action |
|---|---|
| Tap Left Shift / Tap Right Shift | Move one lane left / right |
| Hold Left Shift | Slide under low bars |
| Hold Right Shift | Sprint through speed gates |
| Tap both | Jump |
| Hold both | Phase shift into the ghost layer |
| Alternate L, R, L, R on the beat | Charge through walls |

**Windows players:** pressing Shift five times can open the Sticky Keys dialog. Turn it off in
Settings > Accessibility > Keyboard > Sticky keys (or say "No" to the shortcut prompt).

## Building

- Unity 6 LTS, URP. See [Docs/SETUP.md](Docs/SETUP.md) for first-time setup.
- In the editor: **Shift+Shift > Build WebGL** or **Build Windows**. Output lands in `Builds/`.
- Upload: `ITCH_USER=<you> tools/butler-push.sh html` (or `windows`, `all`).

## Tests

Gesture logic is engine-free and covered by edit-mode tests:
- In Unity: **Window > General > Test Runner > EditMode > Run All**.
- Without Unity: `dotnet test tools/CoreTests` (.NET 8 SDK).

## Docs

[Design & build plan](Docs/PLAN.md) · [Scope lock](Docs/SCOPE.md) · [Credits](Docs/CREDITS.md) · [Input rules](Docs/INPUT.md)

## Licence

Code: MIT (see [LICENSE](LICENSE)). Assets: see [Docs/CREDITS.md](Docs/CREDITS.md).

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
| Alternate L, R, L, R quickly | Charge through the red brick walls |

**Windows players:** pressing Shift five times can open the Sticky Keys dialog. Turn it off in
Settings > Accessibility > Keyboard > Sticky keys (or say "No" to the shortcut prompt).

## Building

- Unity 6 LTS, URP. See [Docs/SETUP.md](Docs/SETUP.md) for first-time setup.
- In the editor: **Shift+Shift > Build WebGL** or **Build Windows**. Output lands in `Builds/`.
- Upload (one-time: install butler, `butler login`, copy `tools/itch.env.example` to `tools/itch.env`):
  `powershell -ExecutionPolicy Bypass -File tools\upload.ps1 html` (or `windows`, `all`); `tools/upload.sh` on macOS/Linux.

## Tests

Gesture logic is engine-free and covered by edit-mode tests:
- In Unity: **Window > General > Test Runner > EditMode > Run All**.
- Without Unity: `dotnet test tools/CoreTests` (.NET 8 SDK).

## Docs

[Design & build plan](Docs/PLAN.md) · [Scope lock](Docs/SCOPE.md) · [Credits](Docs/CREDITS.md) · [Input rules](Docs/INPUT.md)

## License

This project is dual-licensed. Copyright (c) 2026 ELIA.

- **Source code: MIT.** The C# scripts, editor tools, tests and build scripts are open source under the
  MIT License. See [LICENSE](LICENSE).
- **Game assets: All Rights Reserved.** All original game art, audio, music, sprites, textures, 3D models,
  fonts, level designs and other media assets are not open source. They may not be copied, modified,
  distributed or reused without explicit written permission. See [LICENSE-ASSETS](LICENSE-ASSETS).
- **Documentation: All Rights Reserved.** Everything in the `Docs/` folder (plan, scope, logs, guides) is
  covered by the same notice, [LICENSE-ASSETS](LICENSE-ASSETS).

Third-party material keeps its own licence; it is listed in [Docs/CREDITS.md](Docs/CREDITS.md).

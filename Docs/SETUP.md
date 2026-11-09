# First-time setup (day 9)

The scripts in `Assets/_Game` were written outside Unity. Unity has not compiled them yet, and there
are no `.meta` files, scenes or ProjectSettings in the repo yet. Do this once on your machine:

1. **Create the Unity project** in Unity Hub: Unity 6 LTS, template **Universal 3D (URP)**, add the
   **WebGL Build Support** and **Windows Build Support** modules. Create it in an empty folder.
2. **Merge this repo in:** copy this repo's contents (including `.git`, `.gitignore`, `.gitattributes`)
   into the project's root folder. Keep the project's own `Packages/` and `ProjectSettings/`.
3. **Packages** (Window > Package Manager): check that *Input System* and *Test Framework* are installed
   (the URP template normally has both). When asked to enable the new input backend, say Yes.
   Edit > Project Settings > Player > Active Input Handling = *Input System Package (New)* or *Both*.
4. Open the project. Fix any compile errors in the Console first (the Runtime and Editor scripts are
   untested in Unity).
5. **Test scene:** create `Assets/_Game/Scenes/InputTest.unity`. Add an empty GameObject `Input`
   with `ShiftInput` and `GestureDebugHud`. Add the scene to the build list.
6. Run **Window > General > Test Runner > EditMode > Run All**. Expect 24 passing.
7. Press Play and try all 7 gestures. Then **Shift+Shift > Build WebGL**, serve `Builds/WebGL`
   locally (File > Build Profiles > Build And Run does this for you) and repeat in Chrome.
   Days 9–10 "done when": the WebGL build runs in a browser and all 7 gestures log correctly.
8. **Git LFS + first push:**
   ```
   git lfs install
   git add -A && git commit -m "Unity project + scene"
   gh repo create shift-shift --public --source . --push
   ```
9. **itch.io:** link GitHub, join the jam, create a draft project (Kind: HTML), install
   [butler](https://itch.io/docs/butler/) and run `butler login`.

# Shift+Shift — Game Off 2026 Design & Build Plan

Nov 8, 2026 · @ELIA

## Overview

Shift+Shift is a 3D rhythm auto-runner controlled with only two keys:
Left Shift and Right Shift. It is a solo entry by ELIA for
GitHub Game Off 2026, built in Unity, shipped as a browser (WebGL) game
on itch.io with a Windows build as backup.

**Jam facts**

| Item         | Detail                                                                                     |
|--------------|--------------------------------------------------------------------------------------------|
| Theme        | SHIFT (announced 1 Nov 2026)                                                               |
| Deadline     | 1 Dec 2026, 13:37 PST = Dec 2, 2026 03:07 IST. Internal target: 1 Dec, 18:00 IST           |
| Voting ends  | 1 Jan 2027, 13:37 PST                                                                      |
| Requirements | Public GitHub repo with source pushed before deadline; game submitted on itch.io           |
| Engine rules | Any engine, language or library                                                            |
| Judging      | Participant voting on Overall, Gameplay, Graphics, Audio, Innovation, Theme Interpretation |
| Prizes       | None listed                                                                                |

**Team and constraints**

- One developer for design, scripting, tooling and the build
  pipeline. No artist, no composer, no dedicated tester.

- Working window: 9 Nov to 1 Dec, 23 days. Assumed availability 4 to 6
  hours per day (assumption, not confirmed).

- The two-button runner is one of the organisers' own example concepts,
  so the lane will be crowded. Differentiation comes from gesture depth,
  music sync and a consistent visual style.

Sources: [Game Off 2026 on itch.io](https://itch.io/jam/game-off-2026),
[GitHub Blog theme
announcement](https://github.blog/open-source/gaming/game-off-2026-the-theme-is-shift/)

## Game concept

The player's character runs forward automatically along a fixed 3-lane
track; the whole game is played with Left Shift and Right Shift.
Obstacles arrive on the beat of the music, and every gesture is one of
seven combinations of those two keys. The hook: a control scheme anyone
can learn in ten seconds that still produces real decisions by level
three.

**Working pitch line for the itch page:** "Two buttons. Both of them are
Shift."

**Theme interpretation**

- Literal: the Shift keys are the only controls.

- Mechanical: "phase shift" moves the runner between the solid world and
  a ghost layer, each with its own obstacles.

- Tonal: the environment shifts palette and music layer as the run
  progresses.

**The seven gestures**

| Gesture   | Input                    | Action                                         | Introduced in |
|-----------|--------------------------|------------------------------------------------|---------------|
| Tap L     | Left Shift, short press  | Move one lane left                             | Level 1       |
| Tap R     | Right Shift, short press | Move one lane right                            | Level 1       |
| Hold L    | Left Shift, held         | Slide (duck under low bars) while held         | Level 2       |
| Hold R    | Right Shift, held        | Sprint; hold through a speed gate to pass it   | Level 2       |
| Both tap  | Both keys within 80 ms   | Jump                                           | Level 3       |
| Both hold | Both keys held           | Phase shift into the ghost layer while held    | Level 4       |
| Alternate | L, R, L, R in rhythm     | Charge; four on-beat alternations break a wall | Level 5       |

Timing windows (80 ms for "both", 250 ms tap vs hold threshold) are
starting values to tune on day 15, not fixed design.

**Player fantasy**

Flow state. The game should feel like playing an instrument with two
keys: by the final level, the player is reading the track like sheet
music and inputting a sequence of gestures without thinking about each
one.

## Gameplay design

Each level is a single 60 to 90 second run, hand-authored to one music
track, with instant restart on failure. There are no lives, no
checkpoints and no text tutorials.

**Run structure**

1.  Level starts on the first downbeat after a two-bar count-in; the
    track pieces ahead pulse to the beat so the player sees the rhythm
    before the first obstacle.

2.  Obstacles sit on beat positions (quarter notes early on, eighths and
    syncopation later). Each obstacle has exactly one correct gesture.

3.  A hit stops the run with a short hit-stop and screen flash; restart
    is one key press, under one second, back to the start of the level.

4.  Reaching the end plays a short resolve chord and shows the results
    screen.

**Beat sync**

- Every level is defined by a BPM, an audio file and a list of (beat,
  lane, obstacle type) entries in a JSON file. Audio is scheduled with
  AudioSource.PlayScheduled against AudioSettings.dspTime; obstacle
  positions are derived from song time, not frame time.

- Input is judged against song time with a hit window of about 120 ms;
  the window is tuned on day 13 after the first WebGL latency test.

**Scoring**

- Per obstacle: Perfect (within 50 ms), Good (within 120 ms), Miss.

- Level score = perfects x 100 + goods x 50, plus a streak multiplier
  (x2 at 10, x3 at 25, x4 at 50 in a row).

- Results screen: score, best streak, perfect percentage, and the
  level's best score saved locally.

- Optional, post-jam: a global leaderboard. Not in jam scope.

**Difficulty curve**

- Levels 1 to 3 introduce one new gesture each on quarter notes, never
  two new things at once.

- Levels 4 to 5 combine gestures and move to eighth notes; phase-shift
  sections overlay a second obstacle layer.

- Level 6 is the showcase: everything combined, one tempo increase
  mid-track, built to be clipped for the trailer.

- Rule of thumb for solo testing: if ELIA cannot clear a level within 5
  attempts, it is too hard for a jam rater who will give it 2 attempts.

## Level plan

Six levels, roughly 7 minutes of total content. Levels 1 to 4 are
mandatory; 5 and 6 are the first to be cut if the schedule slips.

| \#  | Working title | Length | BPM        | Introduces                      | Combines                                | Visual shift                   |
|-----|---------------|--------|------------|---------------------------------|-----------------------------------------|--------------------------------|
| 1   | Warm-up       | 60 s   | 100        | Tap L, Tap R                    | Lane changes only                       | Dawn palette, static sky       |
| 2   | Low ceiling   | 60 s   | 110        | Hold L (slide), Hold R (sprint) | Lanes + slides + speed gates            | Overcast, fog rolls in         |
| 3   | Gaps          | 75 s   | 120        | Both tap (jump)                 | Lanes + slides + jumps                  | Dusk, long shadows             |
| 4   | Ghost layer   | 75 s   | 125        | Both hold (phase)               | Two overlapping obstacle layers         | Night, ghost layer glows       |
| 5   | Drumline      | 90 s   | 135        | Alternate (charge)              | Walls on eighths, phase sections        | Neon, palette inverts in phase |
| 6   | Full shift    | 90 s   | 130 to 150 | Nothing new                     | Everything; tempo rises at the midpoint | Cycles through all palettes    |

**Authoring rules**

- First 8 bars of each level use only the new gesture, with generous
  spacing, so the player learns by doing.

- No two new obstacle types within 4 beats of each other until level 5.

- Every level has a safe 2-bar breather before its hardest section.

- Level 1 gets the most polish: most raters play it, many play only it.

**Tooling**

Levels are JSON charts edited in a small in-editor tool: a timeline that
plays the track, lets ELIA place obstacles on beat positions with lane
and type, and previews the run. It is built on days 11 to
12; it is the single biggest time-saver in the project.

## Art and audio direction

One style, chosen for a solo developer with no artist: flat-shaded
low-poly geometry, a strict palette per level, and post-processing doing
the heavy lifting. Consistency beats detail in jam ratings.

**Visual style**

- Geometry: Unity primitives and ProBuilder shapes only, no sculpted
  models. The runner is a capsule with a trailing ribbon; obstacles are
  bars, arches, walls and gates.

- Shading: unlit or flat-lit with a 4 to 5 colour palette per level
  (background, track, obstacle, accent, ghost layer). Palette swaps are
  a material property, so six levels cost six palette assets, not six
  art sets.

- Post-processing (URP Volume): bloom on emissive edges, colour grading
  LUT per level, light fog for depth, vignette, chromatic aberration
  pulsed on the beat.

- Camera: chase camera with FOV kick on sprint and both-hold; subtle
  roll on lane change.

- Modular track kit: 10 to 15 pieces (straight, slight bend, rise, drop,
  tunnel, bridge, gap) assembled along a spline.

**Juice list (day 27)**

- Hit-stop (60 ms) and camera shake on collision.

- Particle burst and ribbon flare on Perfect.

- Speed lines at the screen edge during sprint.

- Ghost layer: inverted palette plus a scanline shader while phased.

**Audio**

| Need                                  | Plan                                                                                                            | Source                                                               |
|---------------------------------------|-----------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------|
| 6 music tracks, fixed BPM, 60 to 90 s | CC0 or royalty-free electronic loops; alternatively AI-generated with a tool whose licence permits distribution | freemusicarchive.org (CC0 filter), itch.io asset packs, or generated |
| Gesture SFX (7)                       | One short distinct sound per gesture, pitch-shifted to the level's key                                          | freesound.org CC0, or synthesised in a free DAW                      |
| Hit, Perfect, Good, restart, menu     | Short UI set                                                                                                    | Kenney Audio packs (CC0)                                             |
| Ambient bed per level                 | Low drone matching the palette mood                                                                             | Same sources                                                         |

Rules: every licence recorded in CREDITS.md before the asset enters the
repo; no track with an attribution-NC or no-derivatives clause; music
loops must have a steady, known BPM or the beat map cannot be authored.

## Technical plan

Unity 6 LTS with URP, WebGL as the primary build target from day 1,
plain C# with no third-party frameworks beyond what the Asset Store
gives for free. Everything is a monolithic Unity project; no services,
no backend.

**Systems (what calls what)**

| System                    | Responsibility                                                                                | Depends on                             |
|---------------------------|-----------------------------------------------------------------------------------------------|----------------------------------------|
| ShiftInput                | Reads both Shift keys each frame, classifies into the 7 gestures with timestamps in song time | Unity Input System, Conductor          |
| Conductor                 | Owns dspTime song clock, BPM, beat events, scheduled playback                                 | Unity Audio                            |
| TrackBuilder              | Builds the level's track mesh along a spline from the modular kit                             | Level JSON                             |
| ObstacleSpawner           | Places obstacles at beat positions from the level chart                                       | Level JSON, Conductor                  |
| Runner                    | Lane position, slide, jump, sprint, phase state; collision checks against obstacles           | ShiftInput, ObstacleSpawner            |
| Judge                     | Matches gestures to obstacles within the hit window; emits Perfect, Good, Miss                | ShiftInput, ObstacleSpawner, Conductor |
| Scoring                   | Streaks, multipliers, level score, local best via PlayerPrefs                                 | Judge                                  |
| GameFlow                  | Menu, level select, run, fail, results; restart in under 1 s                                  | all                                    |
| ChartEditor (editor only) | Timeline tool to author level JSON against the audio                                          | Conductor                              |

**Level data**: one JSON per level with bpm, audioClip, offsetMs,
palette, and an events array of {beat, lane, type}. Human-readable,
diffable, and editable directly as text.

**WebGL specifics**

- Compression: Brotli with decompression fallback enabled; target under
  20 MB compressed. Audio as compressed OGG/MP3, streamed where
  possible.

- Audio cannot start before a user gesture in browsers: the menu's first
  key press is the unlock.

- Keep the page in focus: Shift keys trigger browser shortcuts in some
  combos; test Chrome, Firefox and Edge on day 28. Sticky Keys on
  Windows fires after five Shift presses in a row: show a one-line tip
  on the menu to disable it, and test with it on.

- Test latency on day 13: WebGL audio scheduling is good enough for
  "feels on-beat" in most browsers, but this is the project's main
  technical unknown (assumption until measured).

**Repository layout**

- Assets/\_Game/ (Scripts, Prefabs, Levels, Palettes, Audio, Scenes),
  Assets/ThirdParty/, Docs/ (this plan, SCOPE.md, CREDITS.md),
  .github/workflows/ (optional WebGL build via GameCI; only if it costs
  under half a day).

- .gitignore from the standard Unity template; Git LFS for audio and any
  binary over 1 MB.

- Daily pushes; tag v1.0-jam at submission.

**Where the code and tooling effort goes**

- Input classifier and its unit tests (edit-mode tests for gesture
  timing are cheap and worth it).

- Conductor, Judge and the chart editor tool.

- Spline track builder and palette-swap material setup.

- Build scripts, WebGL template tweaks, itch.io butler upload script.

- Decided by playing, not by tooling: whether a level is fun, timing
  windows by feel, and the palette.

## Scope lock

This is the contract for the 23 days. Anything not in the first list
goes into SCOPE.md under "post-jam" and is not discussed again before 2
Dec.

**In scope**

- Two-key input with the 7 gestures

- 3-lane auto-runner with beat-synced obstacles

- 6 hand-authored levels (4 mandatory, 2 cuttable)

- Menu, level select, results screen, pause, restart

- Local best scores

- 6 palettes, one modular track kit, post-processing

- Music, gesture SFX, UI SFX

- WebGL build on itch.io, Windows zip as backup

- itch page with screenshots, GIF and a 30 to 45 s video

**Out of scope**

- Multiplayer of any kind, online leaderboards, accounts

- Procedural or endless mode

- Story, dialogue, characters with faces

- Gamepad, touch or mobile builds

- Custom level sharing

- Localisation

- Achievements, unlockables, cosmetics

**Cut order when behind** (cut the top item first, in this order)

1.  Level 6

2.  Level 5 (the Alternate gesture goes with it)

3.  Juice list beyond hit-stop and particles

4.  Per-level music variety (reuse 3 tracks across 4 levels)

5.  Windows build (WebGL only)

6.  Level 4's ghost layer (phase becomes a simple invulnerability dash)

Below item 6 the game no longer interprets the theme well; if that line
is reached, submit what exists rather than cut further.

## Milestones and 23-day schedule

Three phases with two hard gates. Nothing new enters after the content
lock on 24 Nov; the final week is fixes, polish and submission only.

<img src="./media/image1.png" style="width:6in;height:2.50893in"
alt="build timeline · 3 phases, 2 milestones, submission" />

build timeline · 3 phases, 2 milestones, submission

If M1 fails (the loop is not fun with placeholder art), the fix is to
simplify the gesture set, never to add features. If any task runs more
than two days late, cut it in the scope-lock order rather than catching
up.

**Day-by-day tracker**

| Days (Nov) | Work                                                                                     | Done when                                                            | Status      |
|------------|------------------------------------------------------------------------------------------|----------------------------------------------------------------------|-------------|
| 9 to 10    | Project setup, public repo, WebGL pipeline, ShiftInput with debug HUD                    | WebGL build runs in a browser; all 7 gestures log correctly          | Not started |
| 11 to 12   | Runner, camera, spline TrackBuilder, ChartEditor tool                                    | A 60 s greybox level is playable                                     | Not started |
| 13 to 14   | Conductor, Judge, placeholder music, fail and restart                                    | Obstacles land on-beat in WebGL; latency measured and hit window set | Not started |
| 15         | M1 review: play 20 runs, tune timing windows                                             | Loop is fun with placeholder art, or gesture set cut to 4            | Not started |
| 16 to 17   | Art direction, palette system, modular kit, post-processing                              | One level looks finished in a static screenshot                      | Not started |
| 18 to 20   | Author levels 1 to 4                                                                     | Four levels playable start to finish                                 | Not started |
| 21 to 22   | Levels 5 and 6, menu, level select, results, pause                                       | Full flow from boot to credits with no dead ends                     | Not started |
| 23         | Audio pass: music, gesture SFX, UI SFX, CREDITS.md                                       | No silent action anywhere                                            | Not started |
| 24         | M2 content lock; draft itch page; send build to 3 to 5 testers from the Game Off Discord | Testers have a link                                                  | Not started |
| 25 to 26   | Fix tester feedback: blockers, then confusion, then difficulty spikes                    | Level 1 is clear to a first-time player                              | Not started |
| 27         | Juice list                                                                               | Hit-stop, particles, speed lines, FOV kick in and tuned              | Not started |
| 28         | Browser pass on Chrome, Firefox, Edge; load size; Sticky Keys test                       | Under 20 MB; audio starts on first key; no shortcut conflicts        | Not started |
| 29         | Final itch page: 6 to 8 screenshots, GIF, 30 to 45 s video, description; Windows zip     | Page complete in draft                                               | Not started |
| 30         | Buffer day; if free, extra polish on level 1                                             | Nothing blocking submission                                          | Not started |
| Dec 1      | Final build by 12:00 IST; submit by 18:00 IST; tag v1.0-jam                              | Submission confirmed on itch.io                                      | Not started |

## Submission checklist

Everything here must be true by 18:00 IST on 1 Dec. Tick items as they
close.

**Accounts and setup (day 9)**

- [ ] GitHub account; public repo shift-shift created with Unity
  .gitignore and Git LFS

- [ ] itch.io account linked to GitHub; joined the Game Off 2026 jam
  page

- [ ] itch.io project created as a draft, HTML project type

- [ ] butler installed for command-line uploads

**Repository (ongoing, final check day 30)**

- [ ] Source pushed daily; last push before the deadline

- [ ] README.md: one-line pitch, controls, how to build, link to the
  itch page

- [ ] CREDITS.md: every third-party asset with licence and source URL

- [ ] SCOPE.md: scope lock and post-jam list

- [x] LICENSE chosen: MIT for code (LICENSE), All Rights Reserved for assets
  and Docs/ (LICENSE-ASSETS), copyright ELIA. Third-party assets keep their
  own licences.
- [ ] Right before the final submission: on ELIA's PC, batch-sign all commits
  with their own SSH key (commits made from the cloud session are unsigned),
  then force-push. Command and steps: Docs/LOG.md, 2026-11-13.

- [ ] Tag v1.0-jam on the submitted commit

**Builds (day 28 to 29)**

- [ ] WebGL build under 20 MB compressed, Brotli with fallback, tested
  in Chrome, Firefox, Edge

- [ ] WebGL viewport set on itch to the game's resolution (16:9, 960 x
  540 or 1280 x 720) with fullscreen button enabled

- [ ] Windows x64 zip as a downloadable backup

- [ ] Both builds uploaded via butler with channel names html and
  windows

**itch page (day 29)**

- [ ] Title, one-line tagline, 2 to 4 sentence description leading with
  the two-button hook

- [ ] Controls stated on the page (Left Shift, Right Shift; tip to
  disable Sticky Keys)

- [ ] 6 to 8 screenshots, at least two from level 4 or later

- [ ] Animated GIF cover (630 x 500) showing a gesture sequence

- [ ] 30 to 45 s gameplay video uploaded to YouTube and embedded

- [ ] Theme interpretation stated in one sentence

- [ ] Link to the GitHub repo

- [ ] Credits section mirrors CREDITS.md

**Submit (1 Dec)**

- [ ] Final WebGL build uploaded and played through once on the live
  itch page

- [ ] Game submitted to the jam from the jam page, before 18:00 IST

- [ ] Jam submission page shows the entry; post in the Game Off
  community thread

**During voting (2 Dec to 1 Jan)**

- [ ] Play and rate at least 20 other entries with written feedback;
  rating traffic is reciprocal in practice

- [ ] Fix only game-breaking bugs during voting and note any
  post-deadline change on the page (jam convention; check the jam rules
  thread before patching)

## Risks, assumptions and kill criteria

The plan's two biggest unknowns are whether seven gestures on two keys
stay interesting for seven minutes, and whether WebGL audio timing is
tight enough for a rhythm game. Both are tested in the first week on
purpose.

**Risks**

| Risk                                                    | Likelihood | Impact | Mitigation                                                                                | Fallback                                             |
|---------------------------------------------------------|------------|--------|-------------------------------------------------------------------------------------------|------------------------------------------------------|
| Gesture set is shallow; game reads as a tech demo       | Medium     | High   | M1 review on day 15 with 20 runs; design levels 4 to 6 around combinations, not new verbs | Cut to 4 gestures and lean on music sync and visuals |
| WebGL audio latency or drift breaks beat sync           | Medium     | High   | Measure on day 13; schedule audio on dspTime; wide hit window; offset calibration setting | Ship Windows build as primary, WebGL as demo         |
| Two-button runner is a crowded interpretation           | High       | Medium | Strong hook in title and first screenshot; phase-shift layer as the visible twist         | None needed; accept and compete on polish            |
| Browser eats Shift combos; Sticky Keys fires on Windows | Medium     | Medium | Test day 28; on-page tip; remap option is out of scope                                    | Note on the itch page                                |
| Solo schedule slips                                     | High       | Medium | Scope lock, cut order, 2-day-late rule, buffer day 30                                     | Submit 4 levels                                      |
| No external testers respond                             | Medium     | Medium | Ask in the Game Off Discord on day 16, not day 24                                         | Self-test with a fresh-eyes checklist                |
| Asset licence problem discovered late                   | Low        | High   | CREDITS.md entry required before an asset enters the repo                                 | Swap the asset; CC0 sources only                     |
| Level authoring takes longer than coding                | High       | Medium | Build the chart editor early (days 11 to 12); author level 1 on day 18 to calibrate pace  | Fewer, shorter levels                                |

**Assumptions (not validated)**

- 4 to 6 hours per day are available across all 23 days. If it is closer
  to 2 to 3, drop to 4 levels at scope lock.

- Unity WebGL scheduled audio is accurate enough for a 120 ms hit window
  in current Chrome and Firefox. Verified on day 13.

- Participant raters play browser builds far more than downloads. This
  matches common jam experience but is not measured for this jam.

- A rhythm runner with abstract visuals scores adequately on Graphics
  without bespoke 3D art. The day 17 screenshot test is the check.

**Kill criteria**

- Day 15: loop not fun after 20 runs and one simplification pass.
  Action: cut to 4 gestures; do not add mechanics.

- Day 14: beat sync off by more than 60 ms in WebGL with no fix in
  sight. Action: Windows becomes primary; WebGL ships as a demo with a
  note.

- Day 24: fewer than 4 levels complete. Action: content lock at whatever
  exists; move everything to polish.

- Day 29: WebGL build broken with no fix by noon. Action: submit Windows
  only.

## Post-jam: what carries forward

The jam's return is not prize money; it is a shipped, publicly rated
game, a working Unity-to-itch pipeline, and evidence of how fast one
developer can move. Each of those feeds the sub-\$10
Steam co-op plan directly.

**Reusable from this project**

- Unity project template: URP settings, post-processing stack,
  palette-swap materials, build scripts, butler upload, Git LFS setup.

- Conductor and Judge: any future game with music-timed events can reuse
  them.

- Spline track builder and modular kit workflow: a direct head start for
  the Hearse Race prototype.

- The itch.io developer page and any following gained during voting.

- A measured number for "hours of work per minute of shipped gameplay",
  which makes the next scope estimate real instead of guessed.

**Decisions to make after voting closes (1 Jan 2027)**

- If Shift+Shift rates in the top quarter on Gameplay or Innovation:
  consider a 2 to 3 week expansion (12 levels, gamepad, Steam page) as a
  sub-\$5 title. Optional; only if the ratings and comments show demand.

- Otherwise: archive it as a portfolio piece and start the Hearse Race
  prototype with the 3D resource, using this project's pipeline.

**Not to do**

- Do not expand the jam build during voting beyond bug fixes.

- Do not start the Steam prototype in parallel during the jam; the 23
  days are already full.

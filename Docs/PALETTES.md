# Palettes (proposal, day 16, for ELIA's approval)

Preview image: sent in the session chat (not in the repo: PNGs go through LFS, which the cloud session cannot upload). Contrast is WCAG 2 (the plan: obstacle vs background >= 4.5:1). Also checked:
obstacle vs track >= 3:1, and hurdle / wall / ghost / accent vs background and track >= 3:1. All pass.

| Role | Used for |
|---|---|
| background | sky, camera clear colour, fog |
| track | track pieces |
| obstacle | bars, wide bars, low bars (orange family, darker on light palettes for contrast) |
| accent | runner trail, gate, UI highlights |
| ghost | phase barrier, ghost-layer obstacles |
| hurdle / wall | ELIA's pink and red, lightness tuned per palette |

| Palette | Level | background | track | obstacle | accent | ghost | hurdle | wall | obstacle/bg |
|---|---|---|---|---|---|---|---|---|---|
| dawn | 1 | #FBD9C0 | #E7B79A | #8C2F00 | #1E5FA8 | #5B3FA8 | #A0236A | #8E0E1E | 6.3 |
| overcast | 2 | #C9D1D8 | #9EA8B2 | #7A3100 | #0E5E6F | #4B3C8F | #9C1F63 | #8B0F1F | 6.0 |
| dusk | 3 | #3B2550 | #5A3D6E | #FF9A3C | #FFD166 | #7FD1FF | #FF7AC8 | #FF7275 | 6.3 |
| night | 4 | #0B1026 | #1F2747 | #FF8C2E | #5CE1E6 | #B388FF | #FF6FB5 | #FF4D5E | 8.1 |
| neon | 5 | #07020F | #1A0B2E | #FF7A00 | #00F5D4 | #C77DFF | #FF4FD8 | #FF2E4C | 7.8 |
| fullshift | 6 | #101418 | #2A3138 | #FFA040 | #F7F7F2 | #8FB8FF | #FF7DC6 | #FF5260 | 9.1 |

Status: proposed, not yet approved.


## Proposal 2 (day 16): colour = what you press

ELIA's suggestion (one colour per obstacle type) combined with per-level skies. Obstacles answered the same way share a
colour; shape still distinguishes them (colour is a second cue, never the only one).

| Action | Hue | Obstacles |
|---|---|---|
| lane change | orange | bar, wide bar |
| slide | yellow / amber | low bar |
| sprint | teal / cyan | gate |
| jump | pink | hurdle, gap rim |
| phase | blue-violet | barrier, ghost-layer blocks |
| charge | red | wall |

Searched per sky so that every action colour is >= 4.5:1 against the background and >= 3:1 against the track, and every
pair stays >= 20 dE apart for normal vision and simulated deuteranopia and protanopia (Machado 2009), preferring vivid
colours. Light skies (dawn, overcast) force deep tones.

| Sky | background | track | lane | slide | sprint | jump | phase | charge | min contrast | min separation (dE) |
|---|---|---|---|---|---|---|---|---|---|---|
| dawn | #FBD9C0 | #E7B79A | #89573B | #7D5F00 | #004645 | #BD0B79 | #0661C4 | #60000F | 4.5 | 21 |
| overcast | #C9D1D8 | #9EA8B2 | #8D3D05 | #625335 | #215D5C | #450029 | #004998 | #470306 | 4.9 | 20 |
| dusk | #3B2550 | #5A3D6E | #F27017 | #FCC41E | #00FCF9 | #FA58AD | #6293FD | #FA6463 | 4.5 | 21 |
| night | #0B1026 | #1F2747 | #F27017 | #FCC41E | #00FCF9 | #F40E9D | #0E79F1 | #F21E3D | 4.5 | 21 |
| neon | #07020F | #1A0B2E | #F27017 | #FCC41E | #00FCF9 | #F40E9D | #0E79F1 | #EB1038 | 4.6 | 21 |
| fullshift | #101418 | #2A3138 | #F27017 | #FCC41E | #00FCF9 | #F40E9D | #217EF7 | #F92842 | 4.8 | 21 |

For comparison, the alternative palettes A-E (light backgrounds): 4-7 of 7 colours below 4.5:1, and every set has a pair
that collapses under deuteranopia or protanopia (dE 0-12).

Status: proposed, not yet approved.

## Decision (ELIA, day 16): Option 3 skies, charcoal road with lines, colour = what you press

- **Skies** (camera, fog): dawn #FBD9C0, overcast #C9D1D8, dusk #3B2550, night #0B1026, neon #07020F, fullshift #101418.
- **Road** #333333 on every level (charcoal), tunnel walls / rails #2A2A2A, **edge and lane lines** #BFBFBF.
- **Action colours, identical on every level:** lane #F27017, slide #FCC41E, sprint #00FCF9, jump #F40E9D, phase #0E79F1,
  charge #F21E3D. Accent (runner trail) per level: dawn #1E5FA8, overcast #0E5E6F, dusk #FFD166, night #5CE1E6,
  neon #00F5D4, fullshift #F7F7F2.
- **Checks:** action colours vs road 3.0-9.8:1 (lane 4.3, slide 7.9, sprint 9.8, jump 3.2, phase 3.0, charge 3.0);
  colour-blind separation >= 20 dE (normal, deuteranopia, protanopia). Road vs light skies 8-9.5:1; lines vs dark
  skies 7-11:1.
- **Known trade-off (ELIA's choice):** four action colours are below the plan's 4.5:1 against what is behind them (the
  road); 3:1 is the agreed floor. Obstacles always appear below the horizon (camera 3.3 m up since day 17; every
  obstacle top <= 3.2 m), i.e. against the road. Keep new obstacles under the camera height or this no longer holds. Shape stays the primary cue.

## Phase look (day 17, playbook 2.2 step 4)

While both Shifts are held (phased), over 0.1 s: the sky and fog shift to the palette's phase colour x Phase Sky Darken (0.6); ghost-layer blocks (the only things that can hit you while phased) turn near-white; every other obstacle and glow edge dims to x Phase Dim (0.45); road and lines stay as they are; scanlines fade in at Scanline Strength (0.25). All three settings are per palette (Palette asset, "Phase look" header).

**Camera height changed (day 17, later):** the camera is now 2.8 m up, not 3.3 m (ELIA: the gate's top beam sat at eye level, looked like it hit the camera and hid the runner). The beam now passes overhead. The "obstacles always below the horizon" note above no longer holds for the tallest obstacles: the red wall, the phase barrier and the gate (about 3 m) reach just above eye level.

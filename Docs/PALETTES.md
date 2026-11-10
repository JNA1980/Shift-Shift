# Palettes (proposal, day 16, for John's approval)

Preview image: sent in the session chat (not in the repo: PNGs go through LFS, which the cloud session cannot upload). Contrast is WCAG 2 (the plan: obstacle vs background >= 4.5:1). Also checked:
obstacle vs track >= 3:1, and hurdle / wall / ghost / accent vs background and track >= 3:1. All pass.

| Role | Used for |
|---|---|
| background | sky, camera clear colour, fog |
| track | track pieces |
| obstacle | bars, wide bars, low bars (orange family, darker on light palettes for contrast) |
| accent | runner trail, gate, UI highlights |
| ghost | phase barrier, ghost-layer obstacles |
| hurdle / wall | John's pink and red, lightness tuned per palette |

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

John's suggestion (one colour per obstacle type) combined with per-level skies. Obstacles answered the same way share a
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

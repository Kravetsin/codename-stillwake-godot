# Frost film: the cold drawn as misted glass

- Status: accepted; built
- Date: 2026-11-14
- Supersedes the look of the frost in [0050](0050-the-edge-is-made-of-oxygen.md); what it is drawn at stays
  [temperature-in-degrees](2026-11-14-temperature-in-degrees.md)'s

## Context

The frost at the screen's edges was the last pixel-era effect on screen: ragged pale ice with veins and a static grain changing
every frame. Beside the heat's haze ([heat-limits](2026-11-14-heat-limits.md)) it stood out. It also jerked: it appeared at
three tenths of its alpha the moment the cold rose past nothing, and followed each reading, taken four times a second, at a
constant rate, so every change of temperature showed as a step.

## Decision

- **The look** (the user's pick C of four on a page of candidates, 2026-11-14, "the misted film" — ice glass, frost ferns and
  thin ice plates passed over): a film creeping in from the edges as breath on cold glass. Behind it the world is blurred, more
  where the film is thicker, and paled toward a cold grey; its front is ragged and drifts slowly; motes of rime sink and sway in
  it. The user cut the candidate's six-armed crystals along the film's front: they read as snowflakes hanging in the screen.
- **The easing** (`ScreenEase`), shared by the frost and the haze: what is shown closes 1 − 1/e of its gap to what was read every
  0.6 s, the same at any frame rate, and lands on it within 0.0005, so the layer hides at nothing. The film grows from nothing:
  nothing is drawn at a fixed strength the moment the cold appears.

## Consequences

- No grain or static over the screen any more; the cold reads by the blur and the pale.
- The blur takes 17 samples of the screen per pixel, only where the film lies; the middle of the screen returns at once.
- The heat's haze eases the same way, no longer linearly over a second.

## Not handled

- The film does not differ by what made the cold (the field's edge or the degrees): one look for both, as before.

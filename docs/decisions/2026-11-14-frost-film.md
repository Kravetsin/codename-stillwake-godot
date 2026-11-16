# Frost: the cold drawn as uneven ice with no edge

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

- **The look**, found in the game with the user on 2026-11-14 from a page of four candidates (ice glass, frost ferns, a misted
  film, thin ice plates): a sheet of uneven ice over the screen's edges. Frozen bumps at three sizes bend the world behind it,
  more in some patches than others, almost still; a few soft crests catch the light; the world under it is greyed, cooled and
  milky where the ice is thick. It thins away toward the middle with no line where it ends and reaches further as the cold
  deepens.
- **What the user turned down on the way:** the misted film with motes of rime (it read as snow falling outside, not as the
  hero being cold), its six-armed crystals along the front (snowflakes hanging in the screen), the ice glass's crisp rim and the
  glints running round it (an edge to the effect), and frost ferns over the glass (looked bad).
- **The easing** (`ScreenEase`), shared by the frost and the haze: what is shown closes 1 − 1/e of its gap to what was read every
  0.6 s, the same at any frame rate, and lands on it within 0.01, so the layer hides at nothing. The ice grows from nothing:
  nothing is drawn at a fixed strength the moment the cold appears.
- **Fading out whole** (`ScreenEase.Fade`, 2026-11-15): under a quarter of its strength a layer, the frost, the haze and its
  sparks alike, fades out whole rather than shrinking to the edges, and the frost's edge stays soft however short it reaches.
  Before, a thawing frost hung on for seconds as a thin band with a hard line, and sparks kept flying after the heat; the user
  asked for them gone.

## Consequences

- No grain or static over the screen any more; the cold reads by the bending, the grey and the milk.
- The ice's work is done only where it lies; the middle of the screen returns at once.
- The heat's haze eases the same way, no longer linearly over a second.

## Not handled

- The ice does not differ by what made the cold (the field's edge or the degrees): one look for both, as before.

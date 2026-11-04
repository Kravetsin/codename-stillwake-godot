# Tool icons from large pictures; no square over a whole thing

- Status: accepted; built
- Date: 2026-11-04
- Partly supersedes [tool-icons](2026-10-27-tool-icons.md): an icon is baked from its icon picture, not the held one, with
  fewer sub-samples once fitted down. Partly supersedes [driver-at-work](2026-11-03-driver-at-work.md): the square shows over
  plans, marks and broken machines only where no whole thing is aimed at.

The user, played: the power driver's picture on the board (T) "is pixelated", and so are the other tools' and weapons'; and over
a station's blueprint the driver shows its squares, where it should do as the screwdriver does, the blueprint alone outlined.

**Why the icons broke up.** A held picture is drawn for the fist: the screwdriver's 10 × 3 world pixels, the power driver's
10 × 8 (40 × 32 baked). Turned and fitted to a 40 px slot that is several output pixels a source pixel, each a nearest sample,
and the board drew its pictures with the Nearest filter under its own zoom: hard squares.

**Icon pictures.** `hero.js` and `zfittings.js` export each held tool again as `icons/<its held picture>`, the same drawing at
`ICON_LONG` (384) px along, so every icon is fitted down, never up. `ItemIcons` bakes from it, falling back to the held picture;
`ToolIconTests` fails a tool with no icon picture or a small one.

**The bake.** Fitted down, two sub-samples a source pixel already average it, so `ToolIcon` takes `ceil(2 / scale)` of them, 2
to 8, rather than always 8; up, the ×8 nearest stays. Its texture carries mipmaps.

**The board.** A tool's picture on the board is baked three times its laid-out side (the board's most zoom, 1.5, on a screen
twice the reference) and drawn with `LinearWithMipmaps`; other pictures stay Nearest.

**A whole thing has no square.** `Aiming.IsWhole` is true for a repair, a plan to place a machine or anything wider than a cell,
and a mark to take down a machine or a structure piece of several cells; aimed at one, `WorkedSquare` gives nothing and the
driver works it at one cell, as the screwdriver does, its own outline the aim. A wall or floor plan still takes the square.

## Deliberately not here

- A square over a station next to other plans: it would work the station and its neighbours at once, the case the user turned
  down; the square starts only off a whole thing.
- Larger cards: fitted down, the icons read at the board's sizes as they are.
- Icon pictures redrawn for the icon: each is the held drawing at a larger scale, so the two never part.

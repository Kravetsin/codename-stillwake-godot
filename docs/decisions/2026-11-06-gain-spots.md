# What a hero gains, counted beside where it came from

- Status: accepted
- Date: 2026-11-06
- Supersedes where [head-tally](2026-11-06-head-tally.md) shows a gain and how it flies; its gains in the sim stay

## Context

Playtest 2026-11-06, after the head tally: dug and caught items flying home as icons hid what the hero was digging. Small sparks
flying to the chips over the head were liked but hard to see at full zoom-out. Flying up, the spring-hung column sank onto the
hero; held to the head, it shook with it again. The user tried, in the game, two more places:

- **A hologram plate on the tablet's rim** (picked on a page from four places and four flights: the plate on the rim's left,
  sparks straight to their line). Rejected: following each spark that far draws the eye off the play, and with sparks from
  several pieces landing at once it is no longer clear what came from which.
- **Counts beside their source**, the look the head-tally pick had liked but feared would get in the way while digging. Kept:
  it sometimes covers a little of the view, which the user found no real bother.

## Decision

- **A column per place that gave** (`GainSpots`): a dug or felled cell, each caught piece where it was drawn, a stack walked
  over, a body. Each line its item's icon in its own colours, so the eye finds it again in the inventory, and "+n", with a soft
  shadow and no outline (0075); drawn on the screen 12 px right of and above its place, so the place itself stays in sight.
- **Near gains merge:** a gain within `GainSpots.Near` (4 cells) of a place still showing goes onto that column, so a drill
  working along a seam keeps one growing count. Zoomed out (the user plays near full zoom-out), within an oval 80 px across and
  36 px up and down on the screen (`WidePx`, `TallPx`: a line's width, a row and a half), where the two columns would lie over
  each other. Places farther apart keep their own, so each piece says what it gave.
- **Lines keep their rows** (`GainLines`): the oldest lowest, a new item on top, a growing line staying put and flaring (a third
  larger, pale sky, about 0.9 s). A line holds 1.6 s after its last gain and fades in 0.5 s; at most 5 lines a place, 8 places.
- **A spark into the hero:** every gain also flies from its place into the chest as a small spark, no icon, on an arc sized on
  the screen so it reads alike at every zoom; at most `Fx.MostSparks` at once, the rest counted with no spark. The count does
  not wait for it.

## Deliberately not handled

- **Covering the view:** a column can stand over cells about to be dug; it goes 2 s after the last gain there.
- **Others' gains:** their heroes show nothing, as before.
- **Sound:** none.

## Consequences

- `HeadTally` and `GainTally` are gone; `GainSpots` and `GainLines` (Godot-free) and `GainSpotsView` (the HUD layer) replace them.
- `-- --sortie --manual` (and `--sentry --manual`) play in creative mode, so a check by hand is never cut short by air.

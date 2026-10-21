# 0084 — Back walls of natural materials

- Status: accepted; amends [0077](0077-a-planets-ground-grows-from-its-seed-and-is-dug-by-hits.md)
- Date: 2026-10-21

## Context

0077 made soil and stone walls blueprints but no natural back wall, "since nothing would take one down": a mark on natural
ground is refused, and digging hits walls only. The ground's back walls were drawn (0079, 0082) and the user wants to build
them too, Terraria's way, where every block has its wall: to close a dug hole's sky, to dress a shelter in the ground's own
look.

## Decision

- **Soil and stone back walls are blueprints** (`backWallSoil`, `backWallStone`), costed and timed as their walls: a soil
  or a rock. Known once soil or rock is analysed (0027), as their walls; allowed wherever back walls are (every kind's build
  list has `backWall`). Bedrock stays no blueprint, front or back.
- **A back wall a hero built is taken apart with a mark,** as any back wall, for its item. **The ground's own is never
  marked** (`building.natural`, as 0077): Grid answers `GrownBack(cell)`, whether a cell's back wall is as its ground grew,
  from what it already keeps of each written cell (0077's edits) — no new saved state. Aboard and on drawn maps nothing
  grew, so every natural back wall there is a built one.
- **Air follows the rules already made:** on grown ground a cell before a natural back wall is outside (0077: natural
  ground holds no air), a built one included, so it shuts out the sky but holds no room. Aboard it backs a room as any
  back wall does, leaking its material's `Leak` (soil 40, stone 36, above any hull's).
- **Seen as the ground's:** the build menu's card and a plan show the ground's back tile (`back_soil`, `back_stone`); built,
  it is drawn as the ground's own.

## Rejected

- **Digging back walls with weapons:** every swing at a cave's air would hit the ground's back walls, opening caves to the
  sky's light; a hammer-like tool for walls is a mechanic of its own.
- **Marking every natural back wall,** the ground's included: stripping a cave's back walls for soil at half a second each
  would light every cave.
- **A saved list of built natural back walls in Building:** Grid already tells grown cells from changed ones.

## Deliberately not handled

- **A natural back wall built on a drawn map's ground** (tests, the strip) is taken apart like a built one: no drawn map
  ships natural ground but tests.
- **Sealed rooms of soil on a planet:** natural back walls hold no air there; an aired cave base waits for vents on
  planets (0077).

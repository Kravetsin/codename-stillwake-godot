# 0106 — Steps replace slopes

- Status: accepted; supersedes [0069](0069-a-cell-has-a-shape.md); amends [0077](0077-a-planets-ground-grows-from-its-seed-and-is-dug-by-hits.md)'s slopes and [0104](0104-the-cell-is-8-px.md)'s "slopes throw"
- Date: 2026-10-22
- Design: `designs/cell-8px.md` (the session's scratchpad), "Steps replace slopes" and its amendments

## Context

Slopes (0069) let a hero walk up a hill without a jump when a cell was 16 px, 38% of its height. They cost three rounds of foot IK,
creature jerks and their own collision, and every reader of the grid had to know half cells. At 8 px (0104) a cell is a fifth of the
hero: a hill of one-cell steps reads as a slope, and a walk that climbs a step on its own needs none of that.

## Decision

- **Every cell is whole.** The grid has no shape layer: no `Shapes.cs`, `ShapeLayer`, `ShapeAt`, `GridLayers.Shape`,
  `Placement.Shape` or `grid.notShapeable`; maps have no `/ \ r 7`, and one drawn with them does not parse. Nothing reads a half cell:
  stacks, creatures, light, starlight, hits, drips and the drawing take a wall as a wall.
- **The hero steps up a cell** (`Hero.StepUp` = 1000). Walking on the ground, in parts that end as the box's leading edge reaches its
  column's end, what stops the walk is stepped onto when its top is no more than `StepUp` above the feet and the box fits there, over
  where it stands and where it goes (`Hero.StepOnto`): a ceiling too low stops it. Two cells stop the hero, which jumps or is lifted by
  the ledge assist (0092), unchanged.
- **And follows ground a cell down without a fall.** After each part the feet keep to ground up to `StepUp` below them; a hole the
  body fits that is no deeper than that is walked down into, not fallen into, so a stair down is walked at the walk's speed. Deeper, a
  walk off an edge falls as before.
- **The box is whole too:** it never overlaps a wall or a closed door (`Hero.Clear`); the low ledge rule and mixed footing of 0069 go
  with the slopes.
- **Creatures** step up and down `Fauna.StepUp` = 2 cells, as 0104 has them. Their feet no longer read the ground under them
  (`FeetOf`, `Fauna.FootSpan`, `Fauna.Teeter` are gone): a creature is drawn upright where the sim stands it, never tilted to a slope.
- **The drawing eases a step** (`game/shared/StepEase.cs`): the sim moves a body a whole step in one move, and its picture closes the
  step over `HeroMotion.StepMs` (100 ms a cell) for a hero and `FaunaLooks.StepMs` (100 ms for a creature's two cells), never slower
  than the body moves across, so a stair reads as a ramp, not a stutter. Only on the ground: a jump begun mid-step closes what is left
  at the same pace. It is read from positions frame by frame, so every peer draws every body alike.
- **The ground grows whole** (`Terrain`): no slope pass and no material without its back wall. Its hills and bumps rise at most a cell
  a column, clamped from the left, so a natural hill is a stair a hero walks; the ranges' terraces stay cliffs.
- **Saves stay format 28.** No release came between 0104 and this: a format-28 save's `shape` key is ignored, and a grown grid's edits
  hold five layers a cell, not six. A planet saved by a build between the two loads wrong; there were none but the developers'.

## Rejected

- **Keeping slopes as 8 px ramps beside steps:** two ways to walk up the same hill, and every system reading half cells for a look
  the steps give.
- **Easing in the sim** (a step over several ticks): the sim's positions are what collide; a body half up a step would stand in it.
- **A step for floors too:** a floor is climbed through from below and a walk passes it at foot height, as before; only walls and
  closed doors are stepped onto.

## Consequences

- R and Shift+R do nothing in the plan; a wall card plans whole walls. The structure tab's hint no longer names R.
- The living edge's slope pictures (the top sheets' last four columns) and the cut wall pictures are no longer made or drawn.
- Flowers grow on more of the first planet's surface: every step is whole soil where a slope was none (`FloraTests`).
- A creature over an edge no longer droops a foot over it; it stands level on what holds it.
- `-- --slopes` is gone; `-- --hills` is a room of one-cell steps, a valley, a stair with a hole at its top, a block two high and a
  stair of floating blocks.

# 0050 — The edge is made of oxygen

- Status: accepted
- Date: 2026-10-17

## Context

The world is 2048×1024 cells with a 1024×512 build zone in its middle (0048); a hero with a jetpack can
fly anywhere on it (0047). Out past the zone there was nothing to stop it but the grid's wall, and nothing
to say the far dark is no place to be. The suit's oxygen is already the hero's stamina and its leash, so
the edge of the world should be paid in it. But the sim knew a hero's cell only when it changed room:
out in space a hero could fly a thousand cells and the sim would still hold the airlock's.

## Decision

- **The field** is the grid's build zone grown by `Field.Margin` (64) cells every way. Inside it a hero
  breathes as ever.
- **The cold past it** is crew's: `EdgeCost(cell)`, public on `ICrewQueries` for its two callers, the
  breath and the client's frost. With `dx`, `dy` the cells beyond the field and each axis's span the
  cells from the field to the grid's edge on that side (448 sideways, 192 up and down on the new world),
  `depth = min(1000, max(dx·1000/spanX, dy·1000/spanY))`, rounded down: a box by the deeper axis, no
  root. The cost a breath is `ceil(Field.EdgeCost · depth² / 10⁶)`, rounded up so the first cell past the
  line costs a unit: 1 there, 12 half way, `Field.EdgeCost` (45, a full suit) at the edge. A cell off the
  grid costs 45.
- **The suit alone pays it,** at every breath, after the breath and the refill, never below nothing; a
  suit so emptied passes the hero out, and it wakes at the pod as ever. It adds to what thrust vents. On
  a map that sets no zone the field reaches past the grid, so no test or dev map is touched.
- **The hero reports where it is** with the `Enter` it already sends: besides on a change of room, air
  or the pod's reach, the client sends it whenever the sim holds its hero in another cell than its body's
  and `Crew.ReportEvery` (20) ticks have gone since its last `Enter`, never while passed out. No new
  command, payload or format. `Hero.Cell` is that reported cell, which whatever places things by where
  heroes are reads too.
- **The frost:** the client draws frost creeping in from the screen's edges, with a faint static, at
  `EdgeCost` of the body's own cell over `Field.EdgeCost`, eased out: nothing inside the field, a thin
  rim just past the line, heavy near the edge. One full-screen quad over the world, under the HUD.

## Rejected

- **A wall or a push back at the zone's edge:** space would end at a line that is not there. A cost
  the player can choose to pay, in the resource they already watch, keeps the far dark open.
- **A new command for the position, or heroes' cells sent with the replicated hero:** `Enter` already
  carries a cell into the sim; a second channel would be a second truth. Hero states reach peers
  outside the sim (0002) and must stay out of it.
- **Every frame's cell:** a command a frame per hero for a cost paid once a second. Once a second is
  what a breath reads.
- **Euclidean depth:** a root in the sim. By the deeper axis the cold is a box round the field, a
  little gentler off its corners than a round one, which no one flying out will tell.

## Consequences

- The suit is the leash out to the world's edge: a full suit lasts one breath there, three half way,
  eleven a quarter of the way out (4 a breath). Canisters and the jetpack's thrust are spent against it.
- The sim's cell is up to a second and a round trip old: a hero diving past the line and back between
  two reports pays nothing; one coming back in pays a breath at the deeper cell it reported. The frost
  reads the body's own cell, so it shows a second before the suit pays.
- A migrated save's hero off its old grid (cell -1) pays 45 on its first breath, unless its client's
  report lands first; it wakes at the pod.
- Crew's fuzz enters heroes anywhere on the grid and a little off it one draw in four, so the band and
  the edge are breathed in the desync suite.

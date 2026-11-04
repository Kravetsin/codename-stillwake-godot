# Air spreads from each vent

- Status: accepted
- Date: 2026-11-04
- Supersedes, in part, [0011](0011-air-lies-around-the-vents.md) (one order per room, from its vents together, for the
  fill), [0020](0020-walls-of-materials-front-and-back.md) (a room leaks through all its back walls and hull at once),
  [0105](0105-a-room-airs-four-cells-a-beat.md) (the first vent that can pay fills, more vents do not fill faster) and
  [liquid-oxygen](2026-10-30-liquid-oxygen.md) (the leak is free while a room fills)

## Context

A playtest: a big scrap room never got air from one vaporizer, while a smaller one filled at once, and a dry vent
joined by a door to an aired room started putting out air. The leak was the whole room's back walls and hull, so a room
over about 400 scrap cells leaked more than a vaporizer makes before a cell held air. The fill walked one order from all
fed vents together and was paid by whichever network could, so cells beside a dry vent were aired from another's gas.
The user rejected pressure and flow between rooms: air comes from a point, and walls take their share on the way.

## Decision

- **Each fed vent fills from its own order:** its room's cells breadth first from that vent alone (up, left, right,
  down). On a fill beat each fed vent of a filling room, in machine id order, airs up to `Air.CellsPerFill` of the first
  empty cells of its order, a unit each from its own network, and stops when that network holds no gas. Two working
  vents fill twice as fast; a dry vent (fed, its network holding no gas) airs nothing beside itself.
- **A room leaks only through its aired cells.** A cell leaks its back wall's `Leak` and that of each hull cell given
  to it: a hull cell goes to its first neighbour in the room, up, left, right, down, then the diagonals; a hull door
  counts as hull. A room's leak is the sum over its aired cells now. A room too big for its vaporizers fills until its
  air leaks what they make, and holds there.
- **The leak is owed exactly.** Each sealed room holding air adds its leak to a part kept in the save (`Owed`, under
  `Air.LeakEvery`), and whole units fall due, the rest kept. It is exact however the leak moves; it replaces spreading a
  fixed leak over the period by the clock.
- **The leak is paid before the fill,** by the room's vents' networks first, in vent id order, what they cannot pay
  taken from the far edge of the room's order (from its fed vents together, or its middle), as before.
- Opening a door onto an empty room: the joined room's vent orders reach through the doorway and the front carries on;
  the leak grows only as the new cells air. Closing it: a part with no working vent keeps its air and leaks it away.

## Consequences

- A big room's air grows near its vent as fast as a small room's, until it meets the walls.
- The last cells of a room near its limit fill slowly: each unit made beyond the leak airs one.
- `Owed` is a saved field added with an empty default: older saves load with nothing owed, no format bump. Saves can
  hold air no vent put there, aired by the old rules; it leaks away as any cut-off air does.

## Deliberately not handled

- **Pressure, flow, equalising between rooms:** rejected by the user.
- **Which vent aired a cell** is not kept: the leak is paid by the vents in id order, so with two networks in one room
  the lower id's tank empties first.
- **A stalled front's edge flickers by a cell** as units come and leak.
- **An inner hull corner** leaks once its neighbour holds air, a cell late.

# Air is a bubble round each source

- Status: accepted
- Date: 2026-11-05
- Supersedes, in part, [0011](0011-air-lies-around-the-vents.md) (air stays where it lay), [0016](0016-breathe-where-the-air-is.md)
  (a breath paid by the tanks behind the vents), [0020](0020-walls-of-materials-front-and-back.md) (a room leaks through all its
  back walls and hull at once), [0105](0105-a-room-airs-four-cells-a-beat.md) (the first vent that can pay fills),
  [liquid-oxygen](2026-10-30-liquid-oxygen.md) (a unit of gas makes a cell of air) and [0062](0062-drones-keep-machines-going.md)
  (drones fly only through cells holding air)

## Context

Two playtests. First, a big scrap room never got air from one vaporizer, and a dry vent beside an aired room put out
air paid by another network. A fix with per-vent orders froze in the next playtest. A room as a tank of air (its air a
count, filled by vents, emptied by everything else) was stable, but one source then filled the whole ship. The user
wants air to be scarce and valued, read at a glance: "sources are points with auras of air", more cells behind good
walls, fewer behind leaky ones, no slow spreading; a room open to space holds none; a breath costs liquid, not cells. A
first cut where a bubble never passed a doorway, open or shut, was rejected in the playtest: the air stopped dead at an
open door, and walling cells off gave the bubble nowhere to go.

## Decision

- **The bubble lives in a room** (`Rooms`): open doors pass air, closed ones stop it. Opened onto an empty room, a
  bubble grows on through the door as far as its budget goes; shut, the part past it with no vent of its own loses its air
  from the far edge. A way to space through open doors vents the room.
- **A room's air is a prefix of its order:** its cells breadth first from its live vents (machine id order), else from
  its middle.
- **A room's budget** is what its live vents' networks make, thousandths of a unit per `Air.LeakEvery`: each network
  `Air.GeneratorRate` × each vaporizer's satisfaction (the pod in full), split evenly over the live vents in sealed
  rooms on it, rounded down. A full store or a tank about to run dry does not change it; power and liveness do.
- **The bubble** is the longest prefix whose cells' leaks fit the budget. A cell leaks its back wall and the hull cells
  given to it; a closed door in the hull leaks `Air.DoorLeak`. On a fill beat a room's air steps toward its bubble by up
  to `Air.CellsPerFill` cells, growing or shrinking from the far edge; with no budget it shrinks to none.
- **Aired cells are not units.** The aired cells' leak is upkeep, owed exactly in `Owed` and drawn from the room's
  networks; a breath or a fire in an aired cell is drawn the same way and never takes a cell. What the networks cannot
  pay is forgiven: their liquid gone, the vents stop being live and the bubble shrinks.
- **Leaks are back at `main`'s:** scrap 30, iron 24, glass 6, soil 120, stone 90, bedrock 36, doors 20. The pod's 12000
  a period holds 400 scrap cells.
- **A room whose bubble stops short of it** reads `atmosphere.leaky`.
- **Drones fly in any room holding air.**

## Consequences

- One vaporizer airs one well-walled room. Scarcity is in the walls and the sources, not in a timer.
- The pod airs the airlock closet through its open inner door, as before; a room parted by a shut door needs its own vent.
- `Live` and `Owed` are saved fields defaulting to none: no format bump. An older save lists `Live` on
  its first tick and lays every room out then.

## Deliberately not handled

- **Power moves the bubble:** a solar night shrinks it at the step rate.
- **A network's making is split evenly over its vents;** a small room's unused share is wasted.
- **An owed part under a unit** is dropped when its room's cells change.
- **Pressure, flow, equalising between rooms:** rejected by the user.

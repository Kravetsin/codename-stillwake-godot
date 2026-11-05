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
walls, fewer behind leaky ones, no slow spreading. The user chose: a bubble never passes a doorway, open or not, so each
room needs its own vent; a room open to space holds none; a breath costs liquid, not cells.

## Decision

- **Air zones:** a room's cells less its doorway cells (door or hatch, open or shut), split where that divides them,
  keyed by the least cell. A doorway cell never holds air. Opening or shutting an inner door changes no zone.
- **A zone is open** when its room is: a way to space through open doors vents every zone on it. An airlock with both
  doors open still costs the base its air.
- **A zone's air is a prefix of its order:** its cells breadth first from its live vents (machine id order), else from
  its middle.
- **A zone's budget** is what its live vents' networks make, thousandths of a unit per `Air.LeakEvery`: each network
  `Air.GeneratorRate` × each vaporizer's satisfaction (the pod in full), split evenly over the live vents in sealed
  zones on it, rounded down. A full store or a tank about to run dry does not change it; power and liveness do.
- **The bubble** is the longest prefix whose cells' leaks fit the budget. A cell leaks its back wall and the hull cells
  given to it; a doorway beside a zone leaks `Air.DoorLeak`. On a fill beat a zone's air steps toward its bubble by up
  to `Air.CellsPerFill` cells, growing or shrinking from the far edge; with no budget it shrinks to none.
- **Aired cells are not units.** The aired cells' leak is upkeep, owed exactly in `Owed` and drawn from the zone's
  networks; a breath or a fire in an aired cell is drawn the same way and never takes a cell. What the networks cannot
  pay is forgiven: their liquid gone, the vents stop being live and the bubble shrinks.
- **Leaks are back at `main`'s:** scrap 30, iron 24, glass 6, soil 120, stone 90, bedrock 36, doors 20. The pod's 12000
  a period holds 400 scrap cells.
- **A room's status** from its zones: venting if one is, aired if all are, filling if one is, else vacuum. A sealed
  zone with no piped vent and no air is left out. A room whose bubble stops short of it reads `atmosphere.leaky`.
- **Drones fly in any zone holding air.**

## Consequences

- One vaporizer airs one well-walled room. Scarcity is in the walls and the sources, not in a timer.
- The wreck's airlock closet has no air of its own: a hero cycles it on the suit, and the hold needs its own vent.
- `Live` and `Owed` (now by zone key) are saved fields defaulting to none: no format bump. An older save lists `Live` on
  its first tick and lays every zone out then, its doorway cells emptied.

## Deliberately not handled

- **Power moves the bubble:** a solar night shrinks it at the step rate.
- **A network's making is split evenly over its vents;** a small zone's unused share is wasted. Build a vaporizer per zone.
- **A doorway cell holds no air:** a hero standing in one breathes from the suit.
- **An owed part under a unit** is dropped when its zone's cells change.
- **Pressure, flow, equalising between zones:** rejected by the user.

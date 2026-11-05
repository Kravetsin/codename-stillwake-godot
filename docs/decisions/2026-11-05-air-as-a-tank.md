# A room is a tank of air

- Status: accepted
- Date: 2026-11-05
- Supersedes, in part, [0011](0011-air-lies-around-the-vents.md) (air stays where it lay), [0016](0016-breathe-where-the-air-is.md)
  (a breath paid by the tanks behind the vents), [0020](0020-walls-of-materials-front-and-back.md) (a room leaks through all its
  back walls and hull at once), [0105](0105-a-room-airs-four-cells-a-beat.md) (the first vent that can pay fills),
  [liquid-oxygen](2026-10-30-liquid-oxygen.md) (the leak is free while a room fills) and [0062](0062-drones-keep-machines-going.md)
  (drones fly only through cells holding air)

## Context

Two playtests. First, a big scrap room never got air from one vaporizer: the whole room's back walls and hull leaked
more than a vaporizer makes before a cell held air, and a dry vent beside an aired room put out air paid by another
network. A fix where each vent filled from its own order, its leak paid from the networks before the fill, froze in
the next playtest: a pod paying the leak of its 399 aired cells exactly left the air unmoving, cut-off air never
leaked away, and only a door moving stirred anything. The user asked for the most stable and simple air there is, and
again rejected pressure and flow between rooms.

## Decision

- **A room's air is a prefix of its order.** The order is the room's cells breadth first from its live vents together
  (machine id order; up, left, right, down), else from its middle. Its aired cells are always the first of that order.
- **A vent is live** while it is fed and its network holds oxygen: gas in a store, a vaporizer with liquid behind it
  (power is not asked), or the pod. A dry vent has no air beside it. The live vents are saved (`Live`), so a load lays
  the air out as a world that never stopped.
- **Only vents put air in:** on a fill beat each sealed room not full airs up to `Air.CellsPerFill` of its first empty
  cells for each live vent's network, a unit each from its stores. No status gates it.
- **Everything else takes from the room, never from the tanks:** its leak, a breath, a fire, venting each take the last
  aired cells of the order. The tanks feel a use only as the vents refill it.
- **A room leaks only through its aired cells:** each its back wall's `Leak` and that of each hull cell given to it,
  owed exactly in `Owed`. A room fills until its air leaks what its vents make, and holds there; cut-off air leaks away.
- **Rooms or live vents changed:** each touched room counts the aired cells inside it and airs that many of its new
  order, emptying the rest. Only a tick does this, never a load.
- **Leaks fifth:** every material's `Leak` and the door's are a fifth of what they were, so a vaporizer holds about
  2000 scrap cells, not 400.
- **Drones fly in any room holding air,** not only through aired cells: air laid out anew never drops one mid-flight.

## Consequences

- A room's air is one number, its count; where it lies follows. Nothing can get stuck: a fill always adds the next
  cell, a take always removes the last.
- `Live` is a saved field defaulting to none: an older save gets it found on its first tick, and every room laid out
  then. No format bump.
- A room's air jumps when its live vents or cells change: a door opened onto an empty room spreads both rooms' air
  over the joined order at once.

## Deliberately not handled

- **Pressure, flow, equalising between rooms:** rejected by the user.
- **Which vent's air a cell holds** is not kept: two networks in one room refill in vent id order.
- **A hero or a fire at the far edge** of a room at its limit can lose its cell for a tick and get it back on the next
  fill; a fire goes out then, as on any airless cell.
- **A drone flies through a room's unaired cells** while the room holds any air: the overlay shows them empty.

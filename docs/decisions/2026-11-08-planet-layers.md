# A planet's layers show, and magnetite leaves a trail down

- Status: accepted; supersedes "magnetite veins" in the Deliberately not handled of [ore-pockets](2026-11-07-ore-pockets.md)
- Date: 2026-11-08

## Context

Playtesters who reached the first planet found magnetite tedious to find. It lay only in the heat's cracks, on cave floors
of the band over the core, some 1030 rows under the surface's middle, the last sixth of the depth. Nothing on the way down
said there was more below: stone was drawn alike in all four bands, only the caves' patchy growths differed, so a shaft
passed a thousand rows of the same rock; the chasms ended in the garden band; the motor's hint said "deep in a planet's
caves" and no more. Magnetite now has a sink (the motor: ten for the engine, the fuel pump, the arc bench and the grinder),
the reason ore-pockets gave for leaving its veins out.

## Decision

- **Magnetite pockets.** `magnetiteVein` (code 17, map `m`) gives a `magnetite` a cell, as hard as copper's; pockets only,
  appended after iron so the other ores' salts stay: none in bands 0 and 1, specks in the amber deep (about 0.1 % of its
  rock), more over the core (about 0.6 %). The heat's cracks stay as they were. A speck met in the amber deep says more lies
  below.
- **The rift.** One of a planet's chasms, chosen by the seed, runs down `Ground.RiftDeep` rows under the surface's middle
  (1060 on the first planet: some thirty rows into the band over the core), so the way down is seen from the top and a hero
  can drop to the magnetite. No fall hurts; the grapple and the jetpack climb back.
- **Rock by band.** _(The user's pick: to come.)_
- **The hint** says where: the deepest rock, over the lava, a rift leading down.

## Consequences

- A first motor no longer needs a dig to the core: a few specks in the amber deep, or the rift.
- Old planets regrow with the new pockets and the rift in rock nobody dug, as ore-pockets did: no format bump.

## Deliberately not handled

- A rift opening under what a player built in rock its path now cuts: rare on worlds from before this change, and the
  build stays standing on whatever of it they built.
- Magnetite in the upper bands, lode veins of it, any other new ore.
- The small test planets have no rift: their one chasm stays as the tests know it.

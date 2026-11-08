# A planet's layers show, and a rift shows the way down

- Status: accepted
- Date: 2026-11-08

## Context

Playtesters who reached the first planet found magnetite tedious to find. It lies only in the heat's cracks, on cave floors
of the band over the core, some 1030 rows under the surface's middle, the last sixth of the depth. Nothing on the way down
said the rock below differed: the stone's tint by band (`CaveTint`, 0080) was soft and blended from one band's middle to the
next, so no boundary showed and a shaft passed a thousand rows of what read as the same rock; the chasms ended in the garden
band; the motor's hint said "deep in a planet's caves" and no more.

## Decision

- **Magnetite stays at the bottom.** The user's call: no magnetite above the band over the core, no pockets of it; the
  player digs to the core for it.
- **The rift.** One of a planet's chasms, chosen by the seed, runs down `Ground.RiftDeep` rows under the surface's middle
  (1060 on the first planet: some thirty rows into the band over the core), so the way down is seen from the top and a hero
  can drop to the heat's cracks. No fall hurts; the grapple and the jetpack climb back.
- **Rock by band.** _(The user's pick, "grey stone and marks": to be ported.)_
- **The hint** says where: the glowing cracks in the deepest caves, over the lava, a rift leading down.

## Consequences

- Old planets regrow with the rift in rock nobody dug, as ore-pockets did: no format bump.

## Deliberately not handled

- A rift opening under what a player built in rock its path now cuts: rare on worlds from before this change, and the
  build stays standing on whatever of it they built.
- Magnetite anywhere but the heat's cracks; any new ore.
- The small test planets have no rift: their one chasm stays as the tests know it.

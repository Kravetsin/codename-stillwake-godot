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
- **Rock by band.** The user's pick of four, "grey stone and marks": each band's stone is the grey stone washed half towards
  its band's paint, with a mark of its own — rime on the cold band's upper edges, roots in the garden's, layered banding in the
  amber deep, and in the ember band dark seams of old heat. They glowed at first, unlit and added over the stone; in play
  the user found glowing stone with lava veins unnatural, so they are painted and give no light. Stone and its
  veins take their band's sheets, back, edges and details (`wall_stone_b2`, `tools/anime-art/subjects/caves.js`), the band
  chosen per cell by the client (`game/features/grid/logic/Strata.cs`): the sim's band of its row, the boundary wobbling up to
  three cells along the columns so it reads as rock, not a ruled line. The land under the sky keeps today's plain stone down
  to `PlanetLooks.StrataFrom` (100 rows under the surface's middle). The soft tint by band (`CaveTint`) is retired: the
  sheets carry the colour, and both together dyed it twice.
- **The hint** says where: the glowing cracks in the deepest caves, over the lava, a rift leading down.

## Consequences

- Old planets regrow with the rift in rock nobody dug, as ore-pockets did: no format bump.
- A band boundary crossed by a shaft now shows as a change of rock; the cold band's frost is the faintest of the four marks.
- The atlas holds 21 more sheets and their edges and details: some 5 MB of hi-res pictures in the repository.

## Deliberately not handled

- A cell's band follows its row only: a band's stone never bulges or pockets into its neighbour's.
- Soil, bedrock and the ruins' alloy are drawn alike at every depth.
- A rift opening under what a player built in rock its path now cuts: rare on worlds from before this change, and the
  build stays standing on whatever of it they built.
- Magnetite anywhere but the heat's cracks; any new ore.
- The small test planets have no rift: their one chasm stays as the tests know it.

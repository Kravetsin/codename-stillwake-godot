# Sulfur lies in the deep stone

- Status: accepted
- Date: 2026-11-08

## Context

Playtesters found the first planet's descent dull: nothing new between the depth bands. The design in
`docs/plans/deep-digging.md`, accepted by the user, gives two reasons to dig deep; its first is sulfur, the ore a blasting
charge will be made of. The charge must arrive while the descent is still long, not only at the bottom.

## Decision

- **Sulfur in pockets** (ore-pockets' one row): `sulfurVein` (code 18, legend `y`), Strength 10 and Armour 4 as iron's, its lump
  `sulfur`, stack 250, sold for 40 (`Market.Raw`). Its `Vein` on the first planet is pockets only, appended after iron so no
  other ore or growth moves: none in the cold band, rare in the garden band (about 0.1 % of its stone), more in the amber deep
  (0.6 %), most over the lava (1 %). A test pins the shares over 16 seeds and that only plain stone turned to sulfur.
- **Why the garden band too** (the user's call, 2026-11-08): sulfur first met some 730 rows down, in the amber deep, would make
  the charge speed up only the last 40 % of the way down.
- **Its look** is the user's pick of four drawn candidates: a pale yellow powder crusting a short dark crack in torn patches,
  matte, a thin dust round each (`sulfurVeined`, `tools/anime-art/subjects/caves.js`); the lump a crumbly pale crust with pores on
  a sliver of stone (`items.js`). It shines and glints in lamp light as every vein does, in its pale yellow fleck.
- Old planets regrow with sulfur in rock nobody dug, as ore-pockets did: no format bump.

## Deliberately not handled

- A use for sulfur: the blasting charge comes in its own change (`docs/plans/deep-digging.md`, slice 3).
- Lode veins of sulfur; sulfur on other planet types.

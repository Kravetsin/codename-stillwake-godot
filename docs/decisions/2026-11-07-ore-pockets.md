# Ore lies at every depth, in pockets, and glints now and then

- Status: accepted; supersedes "new ores, ore under the soil … a vein's glint in a beam" in the Deliberately not handled
  of [veins-and-stairs](2026-10-25-veins-and-stairs.md)
- Date: 2026-11-07

## Context

In the playtest of 2026-11-07 several players each spent about an hour on the first planet; none collected the 10 copper
ore the first crafts ask, most found none. Copper lay only in lode veins from band 1 down, some 300 cells under the
surface line, 0.9 % of the stone, and nothing shallow showed that ore existed. The user asked for ore at every depth,
small deposits near the top, richer deeper, iron besides copper, and an occasional glint so ore can be seen.

## Decision

- **Pockets.** An ore's `Vein` row may carry `Pockets(Wave, Tall, Marks)`: the ore's own noise, on a salt of its own
  (`PocketSalt + i`), and a mark per depth band; a cell is in a pocket where the noise reaches the band's mark (0: none).
  `Terrain.PaintPockets` paints them after the caves' growths, only into plain stone that is no lode vein and no
  growth's, ores in list order. Lode veins, growths and the ground's shape are as they were. The salts follow each
  ore's place in the list, so a vein inserted before an existing one would move that one's pockets on old planets:
  new ores are appended only, and a type may list no more veins than the salts keep apart (`Terrain.MostVeins`).
- **Richer deeper.** Band 0 holds rare specks, mostly 1–3 cells, 95 % at most 6 and none over 60 where neighbouring
  specks join (copper about 0.18 % of its stone, iron 0.24 %); deeper, copper's lode veins carry it (1.5–2.6 %) and
  iron's pockets grow (0.4–0.7 %). Copper's pockets are rarer below band 0 than in it, because its lode veins already
  carry it there. A test pins the shares per band over 16 seeds and at least 10 cells of each within 600 columns and
  60 rows of the spawn.
- **Iron.** `ironVein` (code 16) gives the existing `ironOre`, smelted 1:1 at the furnace; Strength 10, a little softer
  than copper's 12. Pockets only, no lode veins yet. Its look is the user's pick of four drawn candidates: kidney ore, dark red-grey bubbles with a hard highlight
  (`tools/anime-art/subjects/caves.js`).
- **Glint.** One open vein face in three (`CaveLooks.VeinGlintEvery`), picked by the planet's seed and the cell,
  glints in a helmet's beam in the vein's fleck colour, as mica does; the rest stay dark, so a glint means ore.
- **Old planets** regrow with the new ore in rock nobody dug: no format bump, as 0085 and 0091. Nothing is buried or
  moved; the growths their players saw and felled stay where they were.

## Consequences

- Shallow copper is found by digging and by glints on cave faces, not on the open surface: there are no outcrops.
- Iron now comes from a planet too; the asteroids keep their quartz and the bulk of the early iron.
- A new ore is one row: a material, its pockets or lode widths, its look.

## Deliberately not handled

- Coal, magnetite veins and other ores, until they have a sink and a reason.
- Iron lode veins; ore on the open surface; scanners, detectors, map markers.
- Pocket size drawn per pocket: the mark sets it, so pockets of a band look alike; a few neighbouring pockets join.
- Other planet types: Frost keeps no copper and gains no iron.

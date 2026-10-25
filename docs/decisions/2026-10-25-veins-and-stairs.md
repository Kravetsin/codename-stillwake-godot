# A planet's ground is a stair, with veins of ore in its lodes

- Status: accepted; supersedes the terraces' cliffs of [0077](0077-a-planets-ground-grows-from-its-seed-and-is-dug-by-hits.md)
  and the ice and copper vein growths of [0080](0080-caves-glow-by-depth.md)
- Date: 2026-10-25

## Context

The first planet's ranges rose in terraces 10 rows apart (0077), so a sheer wall 80 px tall, two heroes high, stood about
every 30 columns on every range's slope: several to a screen, cleared only by a held jump. To the player they read as seams,
as if each stretch were grown without the one before. Ore came only as 2×2 growths on cave walls, rolled slot by slot, so
copper and ice were sprinkled evenly over every cave; crystals too. The user asked for flowing ground, more varied relief,
and ore and crystals in clusters that read as veins. It is the first step of a longer reworking of the planet.

## Decision

- **A stair, never a cliff.** After the noises add up, the surface is filled so no column lies more than a row under either
  neighbour (0106: a hero steps up a row): from the left, then from the right, so the high ground keeps its shape and a
  terrace's riser becomes a run of one-row steps whichever way it faces. Terraces stay, as shelves (`TerraceStep`). Only
  chasms, cave mouths and pits still drop.
- **Plains and highlands.** A slow region noise (`RegionWave`, a swell every 1024 columns on the first planet) scales the
  ranges and hills from `RegionLeast` percent (plains) to all of them, and lifts the land up to `RegionLift` rows
  (highlands); plains are smooth swells, highlands terraced ranges.
- **The middle stays whole.** No cave mouth opens within `Clear` of the middle, and heroes beam down only within it: a spawn
  once went out past a mouth, onto a spire's alloy.
- **Veins are ground.** Each ore is a natural material run through stone (`Material.In`): `copperVein` and `iceVein`, as
  hard as stone, a lump of ore or ice a cell, never built; their back wall is the stone's, so a dug vein leaves rock behind
  and no second lump. A vein is a thin line where its own 2D noise crosses its middle (the tunnels' test), its width by depth
  band (`Vein.Widths`), painted over solid stone after the caves opened, so it shows in a cave's wall and runs on into the
  rock for the player to follow.
- **Lodes.** One smooth noise per planet (`LodeWave`, `LodeOver`) marks the rich stretches of rock. Veins run only there,
  thickest at a lode's heart; crystals and geodes (`CaveKind.Lode`) grow only there, their chances scaled by the lode up to
  `LodeMost` percent, so rich caves bristle and the caves between hold moss, caps and stalactites.
- Client: a vein's wall is its stone's picture flecked with its ore (`GroundArt.Vein`), the rest of its pictures the
  stone's; it is struck and heard as stone, its chips the ore's colour.
- Saves: format 31, no step (as 28 to 30): a planet's edits are cells, and would land on ground grown differently. Planets
  are reached only from the dev panel till the drop pod.

## Deliberately not handled

- Overhangs, arches, floating islands: the surface is one height a column.
- New ores, ore under the soil, scanners; a vein's glint in a beam (the old growths had one): a vein reads by its colour.
- Region-keyed soil, flora, sky or creatures: regions shape only the relief for now.
- A vein ending in the rock points nowhere further; the lode it lies in is the hint.
- Copper on the first planet: about 46 000 cells of vein against some 2 900 old growths of two ore each, nearly all of it
  buried; dug one cell at a time. Tuned by `Vein.Widths` and `LodeOver` when the economy asks.
- A ruin stamped over a vein overwrites it.

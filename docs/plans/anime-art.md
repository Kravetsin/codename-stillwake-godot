# Removing the pixel art

The game is drawn in [ink and watercolour](../decisions/2026-11-01-ink-and-watercolour.md), and [the switch back to pixels is gone](../decisions/2026-11-03-pixels-unsupported.md); the style's rules are in [the tool's README](../../tools/anime-art/README.md#the-style). Nothing draws a pixel picture any more: a picture missing in `art/hd` throws. What is left is the 800-odd pixel pictures still *read*, for a size or a shape, and the tools that drew them.

## Gone already

The pixel creatures (`creatures/{classic,sixleg,tapir}`, `CreatureArt.Designs`, its palettes, the coat's tones, `tools/planet-art/creatures`), the old jumpsuit, the cave veins, the closed wall edges (`wall-edge_*_0`), and every fallback that drew a pixel picture where its hi-res one was missing.

## What reads them still

Every reader below takes its numbers from the pixel file, whether or not an hd twin exists, so deleting the files breaks it at load. Each moves to the hd picture, reduced where it needs the world's pixels (`CreatureVariety.Reduced`, solid where half covered):

- **A size:** `ShipTiles` (cells, tiles, origins, the 16 px halving), `PlanShapes`, `BlueprintArt`, `FarWrecks`, `PixelArt.Far` and `Lit`, `CaveView`'s and `HomesView`'s atlases, `ShipTiles.RiseOf`. Not every hd picture is 4×: pipes and built wall edges are 2× (4× the 8 px cell), hero parts carry `HiRes.FigureMargin`, the planet's far layers are 2× and its sky another shape. Each needs its scale named.
- **A shape:** `LightView`'s door and hatch occluders, `MarkerLayer`'s pod silhouette, `FloraView`'s planting rows and glow points, `CaveView`'s glow middles and glints, the normal maps (`PixelArt.VolumeMap`), the sliding doors (`DoorShape.Slid`), torn wreck cells, whether a ground top is drawn. A reduced shape is not the pixel one: an occluder or a planting row can move by a pixel. Each is looked at in a frame.
- **The ground's sheets:** `planet/{wall,back,top,detail}_*` have no hd sheet, only hd tiles cut from them; `ShipTiles` cuts the pixel sheet. `detail_plaster` has no hd at all and is added to the atlas regardless.
- **Colours:** `PlanetSky`'s ground under a layer (a pixel's colour), `GroundArt.Vein`/`Recede`.
- **The ink export:** `tools/anime-art/export.js` reads `game/art/caves` and `game/art/planet` at start, `crystalIce*`'s boxes and `acacia_*_glow`'s pods; those numbers are frozen into the subjects first.
- **Tests:** `ArcLampLooks`, `BenchLook`, `HeroLooks`, `MachineFoot` (an ink foot against its pixel one), `MachineArt`, `DebrisLooks`, `Perches`, `WreckDress`, `DoorShape`, `HomesLooks`, `PlanetLooks`, `WaterShows`, `Charges`, `ToolIcon` read pixel files for a size, a shape or that they exist.

## Then

The pixel pictures go, and the tools that drew them: `tools/ship-art`, `tools/planet-art`, `tools/homes-art`, `tools/hero-art`, and the `tools/concepts` pages that read them. `homes-art` also writes `HomesArt.cs` and the ruins' sounds: what it generates is kept as written, no longer generated. `PlateArt`'s pixels go, its size and seat stay.

## Deliberately not here

- A shader to repaint suits: [the mark](../decisions/2026-11-02-hero-clothes.md) replaces it.
- `CreatureArt.Design`'s `Glow`, `Ramps` and `Ink`: the ink export writes them; dropping them is a change to its subject, not worth one.
- A style for each planet type beyond its palette, till one is asked for.
- Tuning for 4K.

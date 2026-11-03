# Removing the pixel art

The game is drawn in [ink and watercolour](../decisions/2026-11-01-ink-and-watercolour.md), and [the switch back to pixels is gone](../decisions/2026-11-03-pixels-unsupported.md); the style's rules are in [the tool's README](../../tools/anime-art/README.md#the-style). Nothing in the game reads a pixel picture any more: what needs one at the world's pixels, for a size or a shape, reduces its hi-res one (`HiRes.Logical`, solid where half covered; soft for a glow's weight), and a picture missing in `art/hd` throws. What is left is the 800-odd pixel files, the export and the tests that still read them, and the tools that drew them.

## Gone already

- The pixel creatures (`creatures/{classic,sixleg,tapir}`, `CreatureArt.Designs`, its palettes, the coat's tones, `tools/planet-art/creatures`), the old jumpsuit, the cave veins, the closed wall edges (`wall-edge_*_0`), and every fallback that drew a pixel picture where its hi-res one was missing.
- Every game reader: the ship's and the ground's tiles and their sheets, plans and build cards, far wrecks, far layers and the sky, lit sprites, the caves' and homes' atlases and glows, the planting rows, the door and hatch occluders, the pod's marker. A ground top or detail with no hi-res picture is not drawn (`detail_plaster`, lava's and plaster's tops), and the vein and receding back-wall colouring went with the pixel ground. The far layers are 2× their world's pixels (`PixelArt.FarScale`), the build card's pipes and built wall edges come from their 8 px cell's hi-res picture.

## What reads them still

- **The ink export:** `tools/anime-art/export.js` reads `game/art/caves` and `game/art/planet` at start, `crystalIce*`'s boxes and `acacia_*_glow`'s pods; those numbers are frozen into the subjects first.
- **Tests:** `ArcLampLooks`, `BenchLook`, `HeroLooks`, `MachineFoot` (an ink foot against its pixel one), `MachineArt`, `DebrisLooks`, `Perches`, `WreckDress`, `DoorShape`, `HomesLooks`, `PlanetLooks`, `WaterShows`, `Charges`, `ToolIcon` read pixel files for a size, a shape or that they exist.

## Then

The pixel pictures go, and the tools that drew them: `tools/ship-art`, `tools/planet-art`, `tools/homes-art`, `tools/hero-art`, and the `tools/concepts` pages that read them. `homes-art` also writes `HomesArt.cs` and the ruins' sounds: what it generates is kept as written, no longer generated. `PlateArt`'s pixels go, its size and seat stay.

## Deliberately not here

- A shader to repaint suits: [the mark](../decisions/2026-11-02-hero-clothes.md) replaces it.
- `CreatureArt.Design`'s `Glow`, `Ramps` and `Ink`: the ink export writes them; dropping them is a change to its subject, not worth one.
- A style for each planet type beyond its palette, till one is asked for.
- Tuning for 4K.

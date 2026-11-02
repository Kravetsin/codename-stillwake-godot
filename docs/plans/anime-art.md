# Removing the pixel art

The game is drawn in [ink and watercolour](../decisions/2026-11-01-ink-and-watercolour.md), and [the switch back to pixels is gone](../decisions/2026-11-03-pixels-unsupported.md); the style's rules are in [the tool's README](../../tools/anime-art/README.md#the-style). What is left is the pixel pictures, in one PR small enough to revert.

## What goes

- The 901 pictures in `game/art` outside `hd`, and the tools that drew them (`tools/ship-art`, `tools/planet-art` and the like). 817 have an ink twin and are read for a logical size or shape: light occluders, plants' rows, thumbnails, tests. Logical pictures are then made by reducing the hi-res ones, in the tool's export.
- 84 have no twin. Those of `creatures/classic`, `sixleg` and `tapir` and the `hero_jumpsuit_*` go; `planet/{back,detail,top,wall}_*`, `caves/vein*` and `wall-edge_*_0` are read by what makes the ground's sheets, which needs looking at first.
- What only pixels need: `Halved`, `CreatureArt.Designs` and its palettes, `CreatureVariety`'s tones, `CreatureParts.Of`'s index maps, `PlateArt`'s pixels (its size and its seat stay), and every fallback that grows a logical picture when `HiRes.Load` finds none.

## What stays

`MachineFootTests` compares a machine's ink foot with its logical picture's: it needs the reduced picture or a floor of its own.

## Deliberately not here

- A shader to repaint suits: [the mark](../decisions/2026-11-02-hero-clothes.md) replaces it.
- A style for each planet type beyond its palette, till one is asked for.
- Tuning for 4K.

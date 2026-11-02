# Removing the pixel art

The game is drawn in [ink and watercolour](../decisions/2026-11-01-ink-and-watercolour.md) by default ([the switch](../decisions/2026-11-02-ink-by-default.md));
its rules are in [the tool's README](../../tools/anime-art/README.md#the-style). `-- --pixel-art` still draws the old pixels. What is left is taking them out,
once the ink has held in play. One PR, small enough to revert.

## What goes

- `HiRes.On`, `--pixel-art` and every branch on them.
- The pixel pictures the ink stands in for, and the tools that drew them (`tools/ship-art`, `tools/planet-art` and the like), once nothing reads them for a logical size.
  Logical pictures are then made by reducing the hi-res ones.
- What only pixels need: `Halved`, ramps, `Atlas.Band`, `Planting.Strip`, the creatures' pixel painters and `PosedSprite`, `PlateArt`'s drawing (its size and its seat stay).

## What stays

`MachineFootTests` compares a machine's ink foot with its pixel picture's: it needs the logical picture or a floor of its own.

## Deliberately not here

- A shader to repaint suits: [the mark](../decisions/2026-11-01-player-colour-mark.md) replaces it.
- A style for each planet type beyond its palette, till one is asked for.
- Tuning for 4K.

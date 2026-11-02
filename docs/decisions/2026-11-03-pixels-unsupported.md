# The pixel art is unsupported

- Status: accepted.
- Follows [ink by default](2026-11-02-ink-by-default.md), whose `--pixel-art` it removes.

The user no longer plays or checks the old pixels, and new work had already stopped drawing them: the hero's clothes broke `-- --pixel-art` for a bare hero. So `HiRes.On` and `-- --pixel-art` are gone, with what only they reached: the tick's and the Istukan's pixel painters and their tests, the moss's stretched bands (`Planting.Strip`), the pipe's rows, the title plate's colours.

**The pixel pictures stay for now.** 817 of the 901 in `game/art` are still read for a picture's size and shape, by what no one sees: light occluders, plants' rows, thumbnails, tests. Taking them out means making them from the ink, which can move what those readers compute; it gets [its own PR](../plans/anime-art.md), and this one leaves them be.

## Deliberately not here

- **The test of a pose's pixels.** The painters' tests of a pose's size and glow go with them. `GuardInkTests` covers the rigs; what a pose looks like is checked by frames.
- **Renaming `TickArt`, `IstukanArt` and `PosedSprite`.** They now hold a pose's types and a creature's placing. A name that no longer fits costs less than a diff through every motion.

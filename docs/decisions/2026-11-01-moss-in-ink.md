# Moss in ink

- Status: accepted; under `--anime-art` until the switch ([the plan](../plans/anime-art.md)).
- Follows [ink and watercolour](2026-11-01-ink-and-watercolour.md).

The user picked on [a page](https://claude.ai/artifact/4Z4qCQyT4UtxvHHmeZToU3): «Космы», matted wet strands, hanging ragged from a ceiling, slumped down a wall, flattened one way on a floor.
The crust, the scabs and the veins were rejected, as every tuft and cushion before them had been for looking like a cartoon.

**A run of moss is its pieces, cut at their blocks' edges, not a stretched pixel.** The pixel moss drew a run by stretching one pixel of its band under its pieces: in this style, that is a ruled
line. Under the flag `Planting.Piece` cuts a block's picture at px 4 or 20 of 24 where the run goes on, and keeps its torn end where the run stops. No new pictures, no new per-piece kinds, and a
piece is one quad of the chunk's batch as before. Without the flag `Planting.Strip` still draws as it did, until the switch takes it out with `Atlas.Band`.

**Every variant has the same cross-section at those two seams,** so any joins any, whichever the cell's hash picks. Over its block a variant swells and thins by hand, some down to bare rock, so
a run does not read as a repeat of four. All of it is drawn in `tools/anime-art/subjects/caves.js`.

**Both tones are the one drawing,** green near the surface, teal deeper, in their own paint; the teal's glow is twice the green's.

## Deliberately not here

- **Long strands.** A picture leaves 4 px off its block's face, as the pixel one did, so a ceiling's reach about half a cell. A longer reach is a bigger picture for every moss; not asked for.
- **Glows cut at the seams.** A glow stays whole, fading to nothing before its edge; where two overlap it is a touch brighter, and hides in the bloom.
- **Moss that joins round a corner, or between blocks a cell out of step:** each keeps its torn end, as it always did.

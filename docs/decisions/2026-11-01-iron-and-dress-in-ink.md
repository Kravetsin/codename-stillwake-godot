# Iron and the wreck's dress in ink

- Status: accepted; under `--anime-art` until the switch ([the plan](../plans/anime-art.md)).
- Follows [ink and watercolour](2026-11-01-ink-and-watercolour.md) and [the scoop](2026-10-31-the-scoop.md).

The plan said scrap was too clean, and [a page](https://claude.ai/artifact/3svAhWkmw3xiRgQR5xADbF) offered four histories of damage. The user declined them: it is the same scrap wall, there is
nothing to invent, only iron and the temporary pixels to redraw. So `wall_scrap` is not touched and nothing here needed a pick: both follow designs already chosen.

**Iron is the scrap's kit, as made and kept.** The pixel design's plates of 32 by 16 px in running bond, a column of rivets at each end, in a colder and lighter paint than scrap, stained only a
tone off. Its back wall is the same panel of 16 px as the pixel one, with and without a grille.

**The dress is the design picked for the scoop, redrawn:** rust speckled on the marked cells, ghost chevrons pointing outward, ice-rimmed shards. Rust now grows from the seams of the wall under
it, by the wall's own plates (`AnimeScrapRows`), so it follows the plates and not a grid of cells; it takes the wall's rust colours. The map is read from `sim/Content/Wreck.cs` at export, as the
pixel generator reads it.

**A torn cell's mask takes no finish.** `WreckDress.Torn` needs pure magenta to cut, so `wreck_tears` is written straight at 4x. `ShipTiles` cuts the fine scrap wall by it exactly as it cuts the
pixel one by the pixel mask, and falls back to pixels without the flag.

## Deliberately not here

- **More damage on the scrap wall.** The user's call.
- **Rust that follows the wall in any world.** Its seams match where a new world lays the wreck (`SHEET_AT`). Should that offset change, rust stops following the seams and nothing else breaks.
- **Glass, mica and hide walls.** Not asked for; still pixels.

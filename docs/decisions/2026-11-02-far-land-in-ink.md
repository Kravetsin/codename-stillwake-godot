# The far land in ink

- Status: accepted; under `--anime-art` until the switch ([the plan](../plans/anime-art.md)).
- Follows [ink and watercolour](2026-11-01-ink-and-watercolour.md).

The planet's three layers of land (`far`, `ridge`, `hills`) are drawn in washes, and the spindly trees on the hills become far acacias: the forked trunk and flat plates of the ink acacia, lost in the haze. They follow a look already picked, so no page.

**These files are twice the logical size, not four times.** A layer is 2048 pixels across: at 4x it would be 8192 wide, about 30 MB of video memory a layer for something soft and always far. At 2x it stays clean at the zooms played, so `PixelArt.Far` draws a layer at the pixel picture's size whatever its file's. Only these go through it.

**A layer's region starts at its second row.** Smooth sampling of a repeating texture blends its bottom row into its top: a dark line across the sky. The land draws nothing on its first row; the clouds lose the faint top row of theirs, and otherwise the pixel layers draw as they did.

## Deliberately not here

- **The clouds, the sun and the giant.** Not asked for; still their pictures, soft enough that only the giant's edge shows its pixels.
- **Other planet types' backdrops.** Step 6; `PixelArt.Far` falls back to their pixels.

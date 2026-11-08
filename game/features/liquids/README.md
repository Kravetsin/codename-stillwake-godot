# liquids (client): water in a planet's cells

## Files

| File | Purpose |
| --- | --- |
| `LiquidsView.cs` | The water over the heroes: the cells in view written into a texture each frame and drawn by `WaterLook`; the nearest stream heard pouring. |
| `WaterLook.cs` | Its shader: the air overlay's watercolour wash made water, darker with depth, a pale tide line along a wandering surface, streams streaked running down. |
| `logic/WaterCells.cs` | The texture's texels: a cell's amount, whether it is in a stream, whether liquid lies over it. |
| `logic/WaterEase.cs` | Each cell's drawn amount eased toward the sim's between ticks; a dried cell fading out. |

Draws and hears what `sim/Liquids` decides (docs/plans/liquids.md); sends nothing.

- **Reads:** `ILiquidsQueries` (`Layer`, `FallNear`), `IGridQueries` (`Blocks`, the size).
- **Over the heroes** (`Layers.Water`): a hero swimming is seen through the wash. Lit as the ground is, not unshaded, so
  it is as dark as the cave and as bright as its lamps.
- **Each frame** only the cells in view, a cell round them; on a surface that never held liquid, nothing.
- **A surface** across a cell is blended with its neighbour's toward the side it is nearer, so a pool at rest, whose cells
  may differ by a unit, reads flat; the edge wanders a little as wet paint does.
- **A stream** is a lump in the air and the gap over it, nothing more. A lump is in the air over an open, empty cell, or over
  one in the air with nothing resting beside it (lumps falling one on another; a pool over the hole it drains through
  lies). Over it, the open cells up to the water it fell from (a lump over it, the pool, the cell beside the ledge it ran
  off) are its stream if they are `WaterCells.Trail` rows at most (two falls: a ledge lets one out each second tick); a
  lump with none is a drop. Under it, the cells down to the water it lands on within a fall; all as wide as the lump. Its
  ends in the air are rounded. Its motion is drawn: the width eased from cell to cell, and streaks, grain and swells running down
  at `FLOW` pixels a second. Streams side by side are drawn as one sheet.
- **Lying water is drawn settled** in its column, a cell full before the one over it: water just spread over a pool's
  surface, with nothing falling onto it, swells the pool rather than standing on it as a spike.
- **Eased:** each cell's drawn amount, and a stream's width, closes on the sim's at `WaterEase.Rate` a second, so 20 ticks a
  second glide and a stream's trail running down reads steady; a stream gone fades out as a stream. Where a stream leaves lying water beside it, its top is that surface, spilling over
  across the cell from that side.
- **Heard:** `Sound.WaterPour` loops at the stream nearest the view's middle within `Sound.PourReach` cells. The
  swimmer's splashes and strokes are the crew's (`Splashes`, played by `Figure`).

## Deliberately not handled

- No ripples round a swimmer and no splash drawn: the sound carries it.

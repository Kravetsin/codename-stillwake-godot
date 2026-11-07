# liquids (client): water in a planet's cells

## Files

| File | Purpose |
| --- | --- |
| `LiquidsView.cs` | The water over the heroes: the cells in view written into a texture each frame and drawn by `WaterLook`; the nearest stream heard pouring. |
| `WaterLook.cs` | Its shader: the air overlay's watercolour wash made water, darker with depth, a pale tide line along a wandering surface, streams streaked running down. |
| `logic/WaterCells.cs` | The texture's texels: a cell's amount, whether it falls, whether liquid lies over it. |

Draws and hears what `sim/Liquids` decides (docs/plans/liquids.md); sends nothing.

- **Reads:** `ILiquidsQueries` (`Layer`, `FallNear`), `IGridQueries` (`Blocks`, the size).
- **Over the heroes** (`Layers.Water`): a hero swimming is seen through the wash. Lit as the ground is, not unshaded, so
  it is as dark as the cave and as bright as its lamps.
- **Each frame** only the cells in view, a cell round them; on a surface that never held liquid, nothing.
- **A surface** across a cell is blended with its neighbour's toward the side it is nearer, so a pool at rest, whose cells
  may differ by a unit, reads flat; the edge wanders a little as wet paint does.
- **A stream** is a cell that falls (open under it, not full there), drawn as wide as it holds.
- **Heard:** `Sound.WaterPour` loops at the stream nearest the view's middle within `Sound.PourReach` cells. The
  swimmer's splashes and strokes are the crew's (`Splashes`, played by `Figure`).

## Deliberately not handled

- No ripples round a swimmer and no splash drawn: the sound carries it.
- A stream is drawn a cell at a time, so one falling fast may show gaps between ticks.

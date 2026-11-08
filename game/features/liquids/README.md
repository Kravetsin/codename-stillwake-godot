# liquids (client): water in a planet's cells

## Files

| File | Purpose |
| --- | --- |
| `LiquidsView.cs` | The water over the heroes: the cells in view written into a texture each frame and drawn by `WaterLook`; the nearest stream heard pouring. |
| `WaterLook.cs` | Its shader: the air overlay's watercolour wash made water, each cell filled to its level, darker with depth, a pale tide line along a wandering surface, falling water streaked running down. |
| `logic/WaterCells.cs` | The texture's texels: how high a cell's water stands, whether it falls, whether liquid lies over it. |
| `logic/WaterEase.cs` | Each cell's drawn level eased toward the sim's between ticks; a dried cell sinking out. |

Draws and hears what `sim/Liquids` decides (flowing-liquids); sends nothing.

- **Reads:** `ILiquidsQueries` (`Layer`, `FallNear`), `IGridQueries` (`Blocks`, the size).
- **Over the heroes** (`Layers.Water`): a hero swimming is seen through the wash. Lit as the ground is, not unshaded, so
  it is as dark as the cave and as bright as its lamps.
- **Each frame** only the cells in view, a cell round them; on a surface that never held liquid, nothing. A row with no water
  within `WaterCells.Gap` + 1 rows of it is skipped by its bytes alone (2026-11-08-planet-frame-rate): a dry view costs next to nothing.
- **As Terraria draws it:** each cell filled from its bottom to its level, or whole where water is over it. The sim moves
  water a cell a tick, so falling water is a whole column of cells and lands as the pool it fills: no
  stream is guessed at between cells.
- **A surface** across a cell is blended with its neighbour's toward the side it is nearer, so a pool at rest, whose cells
  may differ by a unit, reads flat; the edge wanders a little as wet paint does.
- **Falling water** (open under it, short of full there, or a full column over falling water) is streaked, its streaks
  and grain running down at `FLOW` pixels a second. Where it falls in strips (dry rows up to `WaterCells.Gap` between) or
  in trickles a column apart, the gaps are drawn as falling water, so a drained pool reads as one stream.
- **Eased:** each cell's drawn level closes on the sim's at `WaterEase.Rate` a second, so 20 ticks a second glide; a cell
  no longer covered sinks to its level. A dried one goes at once, as in Terraria: sinking out, cells drained together
  would each leave a line on their floor. Water settled in a view that stayed is not eased or uploaded again: the view
  keeps the bytes it wrote and skips a frame that would write the same (planet-frame-rate).
- **Heard:** `Sound.WaterPour` loops at the stream nearest the view's middle within `Sound.PourReach` cells. The
  swimmer's splashes and strokes are the crew's (`Splashes`, played by `Figure`).

## Deliberately not handled

- No ripples round a swimmer and no splash drawn: the sound carries it.
- A full column falls only over falling water, so whether it falls is traced from its head: one whose head lies more than
  `WaterCells.Down` (128) rows under the view, or a sheet reaching more than `WaterCells.Side` (16) columns past it, is drawn
  lying till the view nears.

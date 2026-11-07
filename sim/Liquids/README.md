# Liquids

## Files

| File | Purpose |
| --- | --- |
| `Flow.cs` | The flow: rows from the bottom up, falls then side spread; awake chunks; grid changes read each tick. |
| `Pressure.cs` | A body of full cells levelled as a whole: from its highest free surface to the lowest open cell beside it. |
| `Liquids.cs` | The module, its state and queries (`AmountAt`, `KindAt`, `Layer`, `FallNear`); init, save and load. |
| `Saved.cs` | What a save holds: the cells that differ from the base, from the cells written since. |

Liquid in the cells of a surface (docs/plans/liquids.md): water falls and spreads till it rests. Stage 1 is water only;
lava, rain and taking water out come later.

- **Depends on:** `grid` (`IGridQueries`: `Blocks`, `Zone`, `Width`, `Height`, `Version`, `ChangedSince`).
- **State** (`LiquidsState`, members internal): `Clock` — ticks run, the sweep's way turns with it; `Cells` — written only
  by a save, the cells differing from the base as `cell, byte` pairs, ascending. Derived: `Layer`, a byte a cell
  (`Content.Liquids.Of`: amount in bits 0–3, kind in 4–6), null on a surface that has held none; the awake chunks; the
  base of every cell written since; the cells liquid fell into last tick.
- **Base:** a grown surface's liquid grows again with its ground (`Layout.Liquids`, the lakes of sim/Core `Lakes.cs`), so a
  save holds only what changed; a drawn map's base is none, so a save holds all of it. A save from before the lakes gets
  them at its load, flowing into whatever was dug beside them.

## Rules

- A cell holds 0 to `Full` (8) units of one kind. Liquid enters only cells that do not block (`Blocks`: walls, closed
  doors, hatches; a ladder, floor or grate lets it through), dry or of its kind.
- **A tick** sweeps whole rows from the bottom up, along each row left to right on even `Clock`, right to left on odd.
  A cell first falls: through up to `Fall` (4) empty cells, into the last that takes it, even part-filled. What did not
  fall then spreads, the sweep's way first: to a side neighbour at least 2 lower, ⌊(a−b)/2⌋; a lone unit, which cannot
  be halved, runs whole to an empty side cell over room to fall, so a ledge keeps no film. A cell given liquid sideways
  this tick passes none sideways till the next.
- **Pressure**, after the sweep: a body, the full cells of a kind joined side to side, moves up to `Press` (8) units a
  tick, one at a time, from its highest free surface (its cells open and dry above, the part-filled cells of its kind
  beside it) to the lowest open cell beside it short of full (not under it: falling fills those), while they are 2 or
  more apart. So a U-tube rises and a pool settles within a unit. Every body is planned from the layer the sweep left,
  ties to the lower cell index, and the moves are made in the order of the bodies' first cells, one spoilt by those
  before dropped. A film one unit deep on a floor away from any drop stays.
- Liquid moved outside the zone is gone (the fog drains it).
- **The grid:** each tick reads `ChangedSince(version)`; a cell that now blocks loses its liquid, and every change wakes
  what is round it. A change too big for the log (null) looks at every cell again.
- **Awake chunks** (`Chunk` = 16 cells square) are derived: only they are swept. Any write wakes the chunks of the cell and
  its four neighbours at once, those ahead of the sweep too; after the sweep a chunk not woken this tick sleeps once none
  of its cells can move. Only bodies with a full cell in an awake chunk are pressed: whatever puts a body out of level
  wakes the chunks round it, and a body that moves keeps its own awake. So skipping chunks never changes the result, and
  the set is not saved: `Rebuild` finds it again, bodies out of level too.
- `FallNear(cell, reach)`: the nearest cell liquid fell into last tick, the lower then the left on a tie; for the stream's
  sound.

## Guarantees

- In a closed box the units are kept and the liquid comes to rest (FsCheck, `LiquidsTests`).
- A save loaded at any tick runs on as the world kept running.
- An idle surface costs nothing but the grid read; a lake draining into a chasm on 4096×1536 costs about 1 µs an awake
  chunk, under 0.7 ms a tick at most.

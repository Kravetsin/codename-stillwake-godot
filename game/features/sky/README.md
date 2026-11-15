# sky (client): frost on the hull before the ice belt

The ship's stretches of space ([stretches-of-space](../../../docs/decisions/2026-11-15-stretches-of-space.md)) are seen coming
without a word: the far picture (`render/StretchBackdrop`), the radar's band (`features/computer`, `RadarBand`) and, before the
ice belt, frost on the hull, drawn here. All read from the sky's `SkyNow` through `StretchShown` (`Content/logic/StretchLooks`),
so a peer joining mid-warning sees what the others do. The sim knows nothing of the frost.

## Files

| File | Holds |
| --- | --- |
| `FrostView.cs` | the frost drawn: one mesh of a quad per hull cell, its shader growing, holding and melting the rime |
| `logic/Frost.cs` | which sides of a wall face space, its inner corners, each cell's turn, how much shows when, the numbers |
| `logic/HullCells.cs` | the hull's cells and their sides, found as a surface loads and kept by the grid's changes |

## Rules

- **Made by `SurfaceViews`** on a surface with stretches (the ship), shown only while a stretch with `Frost` is coming, running
  or easing back (`Frost.Shows`).
- **A hull cell** is a wall with a side facing space (`Frost.SidesOf`) or an inner corner, where its two sides are walled and the
  cell between them is space (`Frost.HollowsOf`). Found once over what is built, then kept by the cells a change names and their
  eight neighbours (`HullCells`): never a walk a frame.
- **The crust** (the pick «Кромка инея»): a light rime band along every edge facing space, its inner edge scalloped
  `Shallowest` to `Deepest` (2–3.4 px) a point each `Scallop` along the hull, standing `ProudLeast` to `ProudMost` (0.5–1.4 px)
  proud of the edge, inked all round; the outer corners rounded by a disc, the inner ones filled where the two bands turn. The
  scallops follow the world's position, so the edge runs on unbroken from cell to cell. The sheet (`art/hd/frost.png`,
  `tools/anime-art/subjects/stretches.js`) holds only the paint, a tile by open sides and checker parity.
- **Its turn** (`Frost.Delays`): the outer corners at once, a cold spot one cell in `ColdEvery` at `ColdFrom`, every other cell
  `PerCell` later each cell along the hull from the nearest, at most `Latest`, in shares of the warning; each part grows over
  `Span`. It holds while the belt runs, melts back the last frozen first, darkening, and leaves the hull wet a while (`Thawing`).
- **A frame sets four numbers** (grow, melt, dark, wet), and only when one changes; the mesh is made again only when the hull's
  cells change.

## Deliberately not handled

- Frost on a planet, on inner walls or on machines: the ship's outer walls only.
- A wall built during the belt frosts at its cell's turn, at once if past it: no growth of its own.

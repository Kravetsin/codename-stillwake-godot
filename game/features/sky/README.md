# sky (client): frost on the hull before the ice belt, a planet's storms

The ship's stretches of space ([stretches-of-space](../../../docs/decisions/2026-11-15-stretches-of-space.md)) are seen coming
without a word: the far picture (`render/StretchBackdrop`), the radar's band (`features/computer`, `RadarBand`) and, before the
ice belt, frost on the hull, drawn here. A planet's storms ([surface-storms](../../../docs/decisions/2026-11-16-surface-storms.md))
are drawn and heard here too. All read from the sky's `SkyNow` through `StretchShown` (`Content/logic/StretchLooks`), so a peer
joining mid-warning sees what the others do. The sim knows nothing of the frost or the streaks.

## Files

| File | Holds |
| --- | --- |
| `StormView.cs` | a planet's weather: the streaks' shader over the open sky, the wind's or rain's loop, the wash (`Washout`) |
| `logic/Storm.cs` | how far a weather has come (`Warn`, `Share`), its wind, haze and dim, how much reaches a hero below the land |
| `logic/Streaks.cs` | the streaks' cells and clock, the smoothed ground line the grit thins from |
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

- **Storms**: `StormView` is made by `SurfaceViews` on a surface whose stretches have a `Weather` look. `Storm.Of` reads
  `StretchShown`; `Main` dims daylight and turns the sky and the land behind toward the haze by it. The hero's controller
  takes the sim's `PaceAt` (`HeroInput.Slowed`) and scales its walk and run by it.
- **Streaks** are a shader over the view: the world cut into slanting cells along the fall (`Streaks.Cells`), at most one
  streak a cell, so the pick's count over its box holds over any view; low ones thin over `LowReach` above the smoothed
  ground. Each column's sky end is a texel, read as the surface loads (`GroundLine.Top`), then only where `ChangedSince`
  names a wall or back wall. Nothing is laid out on the CPU a frame (that cost 2–3.5 ms zoomed out).
- **Under the land** the weather fades with depth to none at `Storm.Deep` (12 cells): the wash, the wind and the streaks.
- **The wash** («Блёклый мир») greys and flattens what is drawn by `Washed` times the share, under the streaks, which are
  unlit.

## Deliberately not handled

- Weather on the ship or seen from it: storms are a planet's surface only. No clear patch round the hero (rejected in play).

- Frost on a planet, on inner walls or on machines: the ship's outer walls only.
- A wall built during the belt frosts at its cell's turn, at once if past it: no growth of its own.

# backdrop (client): the land behind a planet's ground

On a planet's surface, the land behind the play field is composed when the surface is shown, from the planet type's own
plants and ground pictures by its surface's seed ([composed-backdrops](../../../docs/decisions/2026-11-08-composed-backdrops.md)):
three layers far to near, a stone range here and there, grassy hills with groves, open plain, a forest edge. In front of the
sky's pictures (`render/PlanetSky.cs`), both drawn by `render/Stars.cs`. Client only: nothing of it reaches the sim.

## Files

| File | Holds |
| --- | --- |
| `logic/BackdropPlan.cs` | the generator: roles, ridges, patterns, belts, the layers' draws in order; its rng and hash |
| `logic/GroundLine.cs` | the surface's top row per column, and its mean round the camera, wrapping |
| `BackdropLayer.cs` | one layer drawn once into its canvas item, its veil shader |
| `BackdropView.cs` | the three layers made from a planet type, look and seed, laid each frame for the camera |

## Rules

- **The page's generator, one for one** (`surface.js` of the backdrops page: `composeSurface`, `layerFrom`, `compose`):
  `BackdropPlanTests` holds the rules; a seed's draws matched the page's exactly when ported. The recipe's numbers are
  `gc/BackdropLooks.cs`; the planet's own are read where they are: `PlanetType.Plants` and `.Ground`, `FloraLooks` (pictures,
  inks, sink), its `Soil`/`Stone` (`wall_`, `back_`, `top_` pictures), `PlanetLooks.Look.Fog`.
- **Roles from numbers, never names:** a tree has `Tall` of `TreeTall` or more; a shrub `Room` of `ShrubRoom` or more; a
  small solid less room; cover none, drawn as often as its chance. With no tree, shrubs (else solids) stand in for them.
- **Seeded:** the surface's seed (`Main._seed`) through the page's mulberry32; integer ridges, so every peer sees the same land.
  The five draws the page gave its giant and sun are drawn and dropped: the sky keeps its own places.
- **Ridges** are columns a cell of the layer wide, whole cells high, neighbours at most a cell apart, wrapping at the tile.
- **Shelves:** under the mid and near crests, lower ridges of grass-topped land with their own bushes and trees, `Shelves.Drop`
  layer px apart, until one lies wholly under the layer's height (at most `Depth.Lower`), so no wide bare face shows; each a
  `Darken` step darker and `Greener` greener than the one above. A ridge's land is filled down to the next ridge's top only.
- **Drawn once:** each layer's draws go into its canvas item at the surface's showing, a 2048 px tile twice side by side and
  what crosses its edge once more; quads of one picture together in one triangle array. Pictures from `art/hd` with
  mipmaps (`PixelArt.Picture`), at their world size × the layer's scale; ground sheets repeated at that scale.
- **Veil and tint:** a shader mixes each layer toward `BackdropLooks.Recipe.Dark`, then toward the planet's fog, by its depth's
  shares, then multiplies the day's colour (`Stars.Tint`, `Daylight.Backdrop`), as the sky's pictures are multiplied.
- **Placed by the terrain:** the ground line is the surface rows (`GroundLine.Tops`, read once from the grid, the first cell
  with a wall or a back wall) averaged over `Reach` columns either side of the camera. A layer's ground row lies on the screen
  at `screen/2 + (line − camera) × zoom × Follows`, across at its share of the camera. Its land goes on `Below` under its height.

## Deliberately not handled

- Caves: every cave keeps its back wall and none can be hewn, so an underground backdrop would show nowhere.
- Digging does not move the horizon: the rows are read once a showing, a crater is small beside ±100 columns.
- No ease of the ground line over time: a column more moves the mean of 201 by a 201st of a step; an ease waits for frames that show one.
- One recipe for every planet type, the first's; the test strip takes the first planet's plants.
- A dev view zoomed out past 2048 px of the world across shows the tile end; a player's farthest zoom shows 1536.

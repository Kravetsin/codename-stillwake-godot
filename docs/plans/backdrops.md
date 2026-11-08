# Backdrops composed from the planet

Playtest 2026-11-06 asked for Terraria-like parallax backgrounds. Four rounds of picks (the page
https://claude.ai/artifact/Q4XTQZ5s7YyPCUFZ555KPw) ended here: painted backdrops all read as a child's drawing; backdrops
**assembled from the game's own pictures** were liked, and the user asked that a planet's backdrop be **composed from its seed
and its own content**, so every planet type gets one without new art. The page's `surface.js` (`composeSurface`, `layerFrom`,
`compose`) is the reference: port it, and its look, whole.

## What the player sees

- On a planet's surface: today's sky, gas giant, clouds and sun (accepted art, unchanged), then three land layers far to near,
  each assembled from the planet type's own plants and ground pictures, scaled down with distance and veiled toward its fog:
  a stone range here and there, grassy hills with groves of trees, open plain, a forest edge, mixed along the panorama by the
  planet's seed. Every peer sees the same backdrop (the seed is the world's).
- The land follows the terrain vertically ("Горизонт по рельефу"): its ground line sits at the surface's height averaged over
  ±100 cells round the camera, each layer moving by its share of the camera's height from there. In a valley
  the far land comes down with the hero instead of leaving flat fill. The near layer has two more shelves of land below its
  crest, so whatever lies under it starts below drawn land.
- At dusk and night the whole backdrop takes `Daylight.Backdrop`'s one colour, as today (`Stars.Tint`).

## How it is built

- **Data, not code** (principle 7): the recipes (three layers' scale, height, ground row, parallax share, veil, green wash, crest
  rows, which patterns they may hold) and the patterns' numbers (grove sizes, gaps, forest steps, outcrops, noise waves) live in
  `game/Content/logic/BackdropLooks.cs`, one record while there is one recipe (a key by planet type comes with a second). What is the planet's own is read from where it
  already is: its plants from `PlanetType.Plants` (`Flora.Kind`: Tall, Room, Chance) and their pictures from `FloraLooks`
  (variants, sink, inks); its ground from `PlanetType.Ground` (region, range, terrace, hill, bump); its soil and stone pictures
  from `PlanetType.Soil`/`Stone`; its fog from `PlanetLooks`. A plant's role (tree, shrub, solid, cover) is read from its numbers,
  never its name.
- **The plan** (`game/features/backdrop/logic/BackdropPlan.cs`, Godot-free, tested in `game.tests`): from those and the seed, each
  layer's ridges (column heights in whole cells of the layer's scale, a one-cell stair rule, wrapping at the tile) and its parts
  (picture, x, scale, mirrored, sink), in draw order: the generator of `surface.js` one for one, with its own seeded rng
  (client-only and cosmetic: integer ridges and a seeded integer rng, so peers see the same land; a float scale may round a pixel
  differently on another machine). The seed is the surface's own (`Main._seed`, from `Surfaces`).
- **The drawing** (`game/features/backdrop/`): a `Node2D` per layer whose `_Draw` lays its plan once (the canvas item replays
  it), the tile drawn twice side by side, the pictures from `HiRes` with mipmaps so a far tree is a soft small tree at its draw
  size. A few thousand batched quads a frame: measured with the render monitors; drawn once into a `SubViewport` (as
  `render/WorldShot.cs`) only if it misses the budget. The veil (toward a dark and toward the fog, by distance) is a small shader on the layer as drawn, so it costs no
  pixel loop; the day's tint multiplies it after. `render/PlanetSky.cs` keeps the sky, giant, clouds and sun and draws these
  layers in place of `far`, `ridge` and `hills`, repeating across.
- **The ground line**: the surface's top row per column, found once from the grid when the surface is shown (the first cell from
  the top that is not open sky; `Terrain`'s own surface array is internal and unsaved), then its mean over ±100 columns round the
  camera, wrapping at the world's width. `Stars.Show` and `PlanetSky.Follow` take that table and the camera instead of one row
  (`Main.cs` passes the spawn row today). An ease over time only if frames show the line stepping.

## Deliberate omissions

- **Caves.** Every cave keeps its natural back wall and a player cannot hew one, so an underground backdrop would show nowhere.
  The cave composer (`depths.js`) waits for a decision to grow some halls without back walls, which also needs daylight kept out
  of them (`StarField`) and surface creatures kept from spawning there (`Spawn.Stands`). Not built here.
- **Digging does not move the horizon:** the ground line is read once per landing; a dug crater is far smaller than ±100 cells.
- **The foreground walk's look is not ported**: the page's walk, its placement modes and its time buttons were for choosing.
- **One recipe for every planet type now:** the first planet's. A new type takes it until its look names its own.
- **The old painted `far`, `ridge`, `hills` pictures and their `backdrop` subject are deleted**; the sky's pictures stay.

## Steps

1. `BackdropLooks` data and `BackdropPlan` with tests: same seed same plan; different seeds differ; a planet with no tall plant
   places no tree; every part's picture exists in `FloraLooks`; ridges wrap at the tile; ridges keep the one-cell stair.
2. The layer drawing and `PlanetSky`'s use of it, the terrain-following ground line; frames of `--planet` on a plateau and
   in a valley, day and dusk, looked at beside the page.
3. Remove the old land pictures, their subject and their names in `PlanetLooks.BackdropPictures`; ADR `docs/decisions/2026-11-08-composed-backdrops.md`; render README lines.

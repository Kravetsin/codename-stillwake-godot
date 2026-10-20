# 0079 — A planet's surface has its look, and grows plants from its seed

- Status: accepted; its tiles and moss tops superseded by [0081](0081-the-grounds-chipped-look-details-and-living-edge.md)
- Date: 2026-10-20

## Context

The first planet (0077) was ground made in code — speckled placeholder tiles under a flat sky. Of nine style studies
the user picked panel 9, "Stillwake": our outlined, muted 3–4-tone tiles, the cave concepts' soil and moss, an alien
acacia with hanging fronds and glowing seed pods, glowing flowers and berries, motes drifting in the daylight, layers of
haze-lost land behind. The earlier, cartoony concepts were rejected; this one is to be matched closely. Plants need a
place in the sim: they stand on ground that is dug, they will give wood and seeds, and every peer must see the same ones.

## Decision

- **The art is the concept's own,** exported from its drawing code (the study's kit and panel 9) into `game/art/planet`,
  as the tablet's frame was: ground tiles of soil, stone and bedrock (8 × 4 cells, their noise made to repeat at the
  tile's edges so they tile seamlessly) and their back walls, darker and flatter; moss tops by shape; plants and their
  glow; the backdrop's layers. To change the look, change the drawing code and export again; the pictures are not edited
  by hand. Edges and cracks are still made at start (`GroundArt`), in each material's outline and lit line
  (`GroundLooks`).
- **Moss is a rule of drawing,** never the sim's (`features/grid/Grass.cs`): a soil wall, whole or a floor slope, under an
  empty cell with no back wall — open sky — shows moss along its top, its stems up into the cell above, one of four
  pictures a shape by a hash of the cell. Dug, built over or roofed, it is bare. Nothing is stored; a save and a peer
  never see it.
- **Plants are a sparse list grown from the seed, and Building keeps which were felled.** `Terrain` sets them with the
  ground (`Layout.Flora`: each plant's cell and kind, ascending), column by column under the sky only, on whole soil
  with room, by `Content.Flora`'s chances, spacing and room. Building derives the standing plants from that list and
  saves only `Felled`, the cells of those felled since, ascending: a fresh planet saves no plant, a felled one a number.
  Building owns them because it owns taking things apart and digging: a `Dig` on any cell a plant stands in fells it at
  a hit (`building.nothingToDig` no longer for a plant there), its kind's `Yields` going to the sender — none yet: the
  hook where trees give wood and seeds; and in derivation, a plant whose ground is gone or whose cell holds something
  now is felled the tick it happens, so a dig, a mark, a meteor and a build all fell it the same way. Save format 22:
  `felled`, empty in every older save, aboard and on every surface.
  - *Rejected: a per-cell decor layer in the grid.* It would put a seventh layer in every grid, a seventh value in every
    grown grid's edit, a new flag for every reader of `ChangedSince`, and a migration of grown saves, for a few hundred
    plants on two thousand columns. The grid stays the cells; plants are things on them.
  - *Rejected: plants as a pure function of the seed and the grid, drawn by the client only.* Free to save, but a felled
    plant would come back, and nothing could ever be given for one.
- **The client draws** (`features/flora`): a sprite a plant, lit as the world is, its glow added over it unlit; each glow
  picture's brightest squares, at most three a plant, light round them as small lamps through the light feature
  (`LightView.Glows`). A felled plant falls in leaves. Motes drift by glowing plants and over the ground in daylight —
  seven a second while one is in view, forty alive at most, none made off the screen.
- **The sky** (`render/PlanetSky.cs`) replaces the flat colour on a surface with daylight: the study's gradient, a gas
  giant, cloud, the sun with its glint, far mesas, a ridge and hills with spindly trees, the land's layers following
  0.1, 0.18 and 0.3 of the camera, their ground line on the spawn's ground, a pixel of art a pixel of the world; the sun,
  the giant and the clouds held at shares of the screen.
- **Light to match:** daylight is a soft warm white (`Surfaces` sky 1.0, 0.97, 0.9) and soaks down into the ground,
  keeping `Light.GroundSoak` (0.72) a cell going down from wall to wall, so the top cells of soil under the sun show
  fading as in the study; sideways, up and out into caves the walls keep what they kept, and caves stay dark (0077).
- **Cracks keep to a slope's solid half:** a crack on a cell cut to a shape is masked by the shape's own mask
  (`ShapeMasks`), its dent moved into the solid half.
- **The grid is drawn near the camera,** in 32 × 32 chunks, each the first time the view or a chunk round it reaches it
  (0077's freeze: drawing the planet's 1.5 million cells at once).

## Measured

On this machine (RTX 4060 Ti), 1280 × 720:

| What | Cost |
| ---- | ---- |
| the planet's view, default zoom / zoomed to 2.4 | 1050 / 890 fps (render CPU 0.36 ms) |
| its first look | 0.3 s, was 1.2 s: the grid's 965 ms now 70 ms and 11 ms of chunks the first frame; the starlight's 170 ms remains |
| a column of chunks revealed walking | 2–4 ms, every 32 cells |
| growing the first planet, plants included | 58 ms (Release) |
| the first planet's plants | 384 (seed 1): 18 acacias, 21 bushes, 14 rocks, 139 bells, 192 tufts |
| a fresh planet's save | under 16 KB, no plant in it; a felled plant adds its cell |

## Deliberately not handled

- **Felling gives nothing yet** and a tree falls to one hit, as a flower: yields and a tree's strength come with wood.
- **Plants don't grow back, spread or sway,** and none grows in caves, on stone, on slopes or on built soil walls made later.
- **Only a plant's own cell holding something fells it:** a wall built into a crown or beside a trunk leaves it standing.
- **Plants on a drawn map** (tests) are not brought back by a load: only grown ground grows again.
- **Moss on soil a player builds** under the sky grows as on the ground's own; moss in caves waits for cave flora.
- **The starlight's first look** (170 ms on the first planet) is still worked out over the whole grid; chunking it is the
  next step if the beam-down still shows a hitch.
- **One sky for every planet and every hour:** no day and night, no biomes; the backdrop's ground line is the spawn's,
  so it sits low under a high range.

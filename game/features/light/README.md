# Light (client)

Light without a word (0019, 0025): a dark ship lit by its lamps and by the stars through the holes in
its back walls. Drawn only: nothing of it reaches the sim. Numbers from `game/Content/Light.cs`.

- **Reads:** `IGridQueries` — `StructureLayer` (walls and closed doors cast shadows), `BackLayer`,
  `Machines` (lamps; machines dim starlight), `Width`/`Height`.
- **Ambient:** a `CanvasModulate` of `Light.Ambient`, so no corner is quite black.
- **Lamps:** a `PointLight2D` per lamp, soft shadows (PCF5), its texture falling off as the
  TypeScript game spread a lamp's light (`Light.Lamp`, `Light.Air` a cell, clamped per colour) out to
  `LampReach` cells, and an unlit additive glow. Brightness comes from `LightView.LampShare`, the
  power it gets; short of power a lamp flickers (`Flicker`, flicker.ts ported). A light's strength is
  its colour, its energy always 1: the lit shader reads the colour as what the light gives.
- **Shadows:** `Occluders` merges walls and closed doors into rectangles (row runs, joined down) and
  makes them one-sided, so a wall is lit on the faces light reaches and shadows what lies behind it —
  no unshadowed twin lights needed. A wall fills its cells; a closed door only the columns its
  picture fills (`door_closed_0`'s opaque pixels, a panel in the middle of its cell), so the back wall
  either side of a door takes the light of the room it is in. On a change only the rectangles that
  differ are replaced.
- **Starlight:** `Starlight` works it out cell by cell as the TypeScript game did (the stars in every
  open cell with no back wall and past the grid's edge, spread by row and column sweeps), only when
  walls, doors, back walls or machines change, and hands it to `render/Lit.cs` as a texture of a texel
  a cell, its alpha marking a wall's or closed door's cell; a directional light with no shadows
  triggers it. As lighting.ts did, a picture blends it only with cells of its own side: walls, their
  edges and doors (pictures taking `Light.WallIn`) from wall cells, everything else — back walls,
  machines, heroes — from the open cells alone, so the back wall in a door's cell is lit by the room
  either side of the door, never by the door.
- **Fire:** a shadowed light per furnace from the middle of its bottom row, `Light.Fire` falling off
  as a lamp's to `LampReach`, as the TypeScript game's fire gave its bottom row's cells `Light.Fire`;
  shown while `LightView.Burns` says its fire burns (`Main` asks the furnace module).
- **Glowing particles** (0019): a shadowed light under each spark and ember
  (`render/Particles.cs`), `Light.Particle` of its glow's colour as it fades, out to 8 cells, at most
  48 at once.
- **One colour at most whole:** Godot adds its lights one over another, so a lamp, a fire and embers
  together once lit a hero and the furnace far past their own colours. `LightField` keeps, a cell at a
  time and unshadowed, the sum of every lamp's, fire's and glow's light and the brightest of them;
  `LightView` hands both to `render/Lit.cs` as float textures, the lamps and fires worked out again only
  when one changes, the glows added each frame some glow. The lit shader lets a point show the
  brightest light on it over the stars, each colour at most what the ambient and the stars leave of
  whole — the TypeScript game's `min(1, ambient + light)` — and gives each lamp its share of that by
  what it gives of the sum, so where none is shadowed they add up to exactly that.
- **What it lights:** everything drawn with `Lit.Set` — the ship's cells and machines, cables, stacks on
  the floor, debris and every hero's figure, all by the one model; not fire, glows, particles, plans or
  overlays.
- **Kept current:** `LightView.Sync()` after every tick; lamps flicker, fires show and particles
  glow in `_Process`.

## Deliberately not handled

- Helmet lamps: the TypeScript game has none.
- Light bending round corners and through doorways the way the TypeScript sweeps let it: Godot's
  lamps cast straight shadows; only starlight keeps the sweeps. Nor do lamps leak the TypeScript
  game's `Light.WallOut` through a closed door: an airlock shut at both ends shows the ambient alone,
  where the TypeScript game gave it a few hundredths more.
- Where a wall shadows one light and not another, the light still reaching a point is shared out as
  though the wall were not there: dimmer than the TypeScript game's by the shadowed light's share. The
  field is unshadowed because Godot's shadows are worked out only on the GPU.
- A lamp's reach is cut at 16 cells for the frame rate (see `LightView.LampReach`).
- Recomputing starlight near a change only: it is redone over the whole grid, about 6 ms on the
  reference world per change. The lamps' field is likewise redone whole when a lamp's brightness
  changes — every frame while one flickers short of power, about 0.7 ms on the reference world's 103
  lamps — and copied with the glows added, about 0.15 ms, every frame some glow.

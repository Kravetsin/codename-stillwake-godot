# Light (client)

Light without a word (0019, 0029): a dark ship lit by its lamps, its furnaces' fires, whatever glows,
and by the stars through the holes in its back walls. Drawn only: nothing of it reaches the sim.
Numbers from `game/Content/Light.cs`.

- **Reads:** `IGridQueries` — `StructureLayer` (walls and closed doors stop light), `BackLayer`,
  `Machines` (lamps, furnaces; machines dim starlight), `Width`/`Height`; `Particles.Shared` (what
  glows); every `render/Glow` in the tree (a planner's screen).
- **No Godot lights.** Godot lights a drawn thing by at most 15 lights and drops the rest: a furnace's
  embers once pushed a lamp out and whole tile chunks went dark. All light is composed here instead,
  with no limit on how many lights touch a thing.
- **Lightmap** (`Lightmap.cs`): two `SubViewport`s over the view and a margin of 4 cells, HDR, cleared
  each frame, a texel a world pixel (more pixels a texel past 4096 texels across), laid with their
  `CanvasTransform` — no camera. *Main* takes lamps and fires, *Spots* embers, sparks and glows, which
  `Lit` caps apart. Each light whose reach touches the view is one additive quad of a multimesh
  (`Sources.Pack`): its colour falling off by its keep a cell out to its reach as the TypeScript game
  spread it, each colour at most its most, times its strength. The quad's shader walks the cells from
  each texel to the light through an occupancy texture, a byte a cell (`Sources.Occupy`: wall, closed
  door, open; rebuilt only when walls or doors change), and drops the texel if a wall or a closed
  door's panel stands between: shadows to the pixel. The walls a texel is in are passed until the
  line comes out of them, so a wall is lit on the faces light reaches; in a door's cell only a texel
  on the panel itself passes it, down the door's cells till the line leaves the panel, so a door is
  lit whole and the back wall beside it stays dark to a light beyond it. A lamp (`Light.LampSize`) and a
  fire (`Light.FireSize`) have a width: seen from a texel the light is a line across that wide; its two
  ends are walked, and where they disagree — a shadow's edge — seven points between, the share seen
  kept. Edges are soft over as much as the light seen from them is wide, as Godot's PCF softened them.
- **Lamps:** `Light.Lamp`, keeping `Light.Air` a cell, each colour at most `Light.LampMost`, out to
  `LampReach` (16) cells, as bright as `LightView.LampShare` says — the power it gets, flickering when
  short (`Flicker`) — and an unlit additive glow.
- **Fire:** from the middle of a furnace's bottom row, `Light.Fire` keeping `Light.FireKeep` a cell
  out to `FireReach` (8) cells, as bright as `LightView.Fire` says (`FurnaceView.BlazeOf`: a new
  brightness with each picture of the flames, past whole as a coal cracks), and a glow at the mouth.
  The furnace view runs before this one each frame, so the light follows the picture shown.
- **Spots:** each glowing particle at `Light.Particle` of its glow's colour as it fades, keeping
  `Light.ParticleKeep` a cell out to `ParticleReach` (2) cells: a bright point, not a wash, for the
  fire lights the room round a furnace anyway; and each `Glow` at its colour, keeping
  `Light.ScreenKeep` out to `ScreenReach` (3).
- **Starlight:** `Starlight` works it out cell by cell as the TypeScript game did (the stars in every
  open cell with no back wall and past the grid's edge, spread by row and column sweeps), only when
  walls, doors, back walls or machines change, and hands it to `render/Lit.cs` as a texture of a texel
  a cell, its alpha marking a wall's or closed door's cell; a picture blends it only with cells of its
  own side.
- **`render/Lit.cs`** draws everything set with `Lit.Set`, unshaded, the lightmaps blended between
  texels so a shadow's edge does not step at the world's pixels on screen: ambient, plus the stars and the
  main map — scaled down together where they would pass whole in a colour, so a fire's orange over a
  lamp stays orange — plus the spots, together at most `Light.ParticleMost` by their brightest colour;
  a wall takes `Light.WallIn` of what reaches its face. The `CanvasModulate` of `Light.Ambient` stays
  for anything drawn lit without `Lit`; unshaded things are not tinted by it.
- **What it lights:** the ship's cells and machines, cables, stacks on the floor, debris and every
  hero's figure; not fire, glows, particles, plans or overlays.
- **Cost** on the reference world with its 103 lamps lit (`--perf --lit --measure`, RTX 4060 Ti):
  1.38 ms a frame at zoom 1 and 0.96 ms at zoom 2 with soft edges (0.62 and 0.50 sharp), against 1.61
  and 0.97 ms with Godot's lights.

## Deliberately not handled

- Relief: pictures are shaded flat; their normal maps are still made but not read. Whether a light's
  side of walls and machines comes back is the team's call after seeing it flat.
- Light bending round corners and through doorways the way the TypeScript sweeps let it: lamps cast
  straight shadows; only starlight keeps the sweeps. Nor does a closed door leak `Light.WallOut`.
- Heroes and machines cast no shadows.
- Light follows the grid as the sim has it, not a door this peer has just toggled (0014).
- Cost grows with the lights on screen times their reach squared; no light is cached between frames.
  A cache of each still light's shadows is the next step if a ship ever outgrows the budget.
- Recomputing starlight near a change only: it is redone over the whole grid, about 2.7 ms on the
  reference world per change.

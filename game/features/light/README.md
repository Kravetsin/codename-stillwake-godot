# Light (client)

Light without a word (0019, 0029): a dark ship lit by its lamps, its fires, whatever glows,
and by the stars through the holes in its back walls. Drawn only: nothing of it reaches the sim.
Numbers from `game/Content/Light.cs`.

- **Reads:** `IGridQueries` — `StructureLayer` (walls and closed doors stop light), `BackLayer`,
  `Machines` (lamps, fires; machines dim starlight), `MachineAt`, `Width`/`Height`, `Version`,
  `ChangedSince` and `Built`; `Particles.Shared` (what glows); every `render/Glow` in the tree (a
  planner's screen, a charging station's light, a smelter's coils, a grow capsule's lamp, a station's cold mark).
- **Costs the cells changed, not the ship or the world** (F-7, 0051). `LightView.Sync` after a tick
  does nothing while the grid's `Version` stands; when it moved, the grid's log names the cells changed
  since its last look. A cell whose structure changed has its texel of the occupancy texture set again
  (`Lightmap.Follow`); one past the texture lays the texture out anew over `Built`, whose corner the
  shader is told (`origin`). Past the texture every cell is open, so a texture that still covers what
  was built is drawn alike. Lamps are laid out again only when the log names a machine's cell. Starlight
  is worked out again only round the cells changed in structure, back wall or machine
  (`StarField.Follow`): within `StarField.Reach` (45) cells of them, from what lies within twice that.
  Every cell crossed keeps at most `Light.Air` of the light, so past 45 cells what crossed a changed
  cell is under the faintest light a texel shows, and every texel there is as it was; and light from
  past twice the reach is as faint where it is written. The texels are those working it all out gives,
  byte for byte (`BuiltBoundsTests`, over ships and halls where the light runs far). A first look, or
  one the log no longer reaches back to, lays everything out anew, as before (F-6 slice 0): starlight
  over `Built` and 4 cells of space round it, past which every cell is open stars at full, which is what
  the texel at the texture's edge holds, and `Lit` reads that edge texel for anything beyond. A door
  toggled every tick on a fully built 1024×512 world cost the light 35 ms a frame, starlight 33 ms a
  change; now 0.9 and 1.5 ms (debug build), whatever the size of the ship.
- **No Godot lights.** Godot lights a drawn thing by at most 15 lights and drops the rest: a furnace's
  embers once pushed a lamp out and whole tile chunks went dark. All light is composed here instead,
  with no limit on how many lights touch a thing.
- **Lightmap** (`Lightmap.cs`): two `SubViewport`s over the view and a margin of 4 cells, HDR, cleared
  each frame, a texel a world pixel (more pixels a texel past 4096 texels across), laid with their
  `CanvasTransform` — no camera. *Main* takes lamps and fires, *Spots* embers, sparks and glows, which
  `Lit` caps apart. Each light whose reach touches the view is one additive quad of a multimesh
  (`Sources.Pack`): its colour falling off by its keep a cell out to its reach as the TypeScript game
  spread it, each colour at most its most, times its strength. The quad's shader walks the cells from
  each texel to the light through an occupancy texture, a byte a cell of what is built (`Sources.Occupy`: wall, closed
  door, open; a texel set again for each wall or door changed), and drops the texel if a wall or a closed
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
- **Fire:** from where a furnace's or a burner generator's fire gives its light (`LightView.FireAt`, which
  `Main` answers from `FurnaceView.LightAt`: a furnace's bottom row's middle, low in a generator's mouth), `Light.Fire` keeping `Light.FireKeep` a cell
  out to `FireReach` (8) cells, as bright as `LightView.Fire` says (`FurnaceView.BlazeOf`: a new
  brightness with each picture of the flames, past whole as a coal cracks), and a glow at the mouth.
  The furnace view runs before this one each frame, so the light follows the picture shown.
- **Spots:** each glowing particle at `Light.Particle` of its glow's colour as it fades, keeping
  `Light.ParticleKeep` a cell out to `ParticleReach` (2) cells: a bright point, not a wash, for the
  fire lights the room round a furnace anyway; and each `Glow` at its colour, keeping
  `Light.ScreenKeep` out to `ScreenReach` (3).
- **Starlight:** `StarField` (plain C#, tested) works it out cell by cell as the TypeScript game did
  (the stars in every open cell with no back wall and past the grid's edge, spread by row and column
  sweeps), only when walls, doors, back walls or machines change, and then only round them; and
  `Starlight` hands it to `render/Lit.cs` as a texture of a texel a cell laid where that region lies,
  its alpha marking a wall's or closed door's cell; a picture blends it only with cells of its own side.
- **`render/Lit.cs`** draws everything set with `Lit.Set`, unshaded, the lightmaps blended between
  texels so a shadow's edge does not step at the world's pixels on screen: ambient, plus the stars and the
  main map — scaled down together where they would pass whole in a colour, so a fire's orange over a
  lamp stays orange — plus the spots, together at most `Light.ParticleMost` by their brightest colour;
  a wall takes `Light.WallIn` of what reaches its face. A normal-mapped picture — a machine or a
  socket domed over its shape, a door rolled side to side, walls flat (`PixelArt.VolumeMap`), so the
  side toward a light is the brighter and no rivet casts a shadow — is shaded toward where the lamps' light comes from — up the main map's slope over
  half a cell each side, tilted 45° up — and the sum held at whole after it; where the light hardly
  changes the picture is shaded as facing it. Near a fire under a lamp the slope turns a little with
  the flames. The `CanvasModulate` of `Light.Ambient` stays
  for anything drawn lit without `Lit`; unshaded things are not tinted by it.
- **What it lights:** the ship's cells and machines, cables, stacks on the floor, debris and every
  hero's figure; not fire, glows, particles, plans or overlays.
- **Cost** on the reference world with its 103 lamps lit (`--perf --lit --measure`, RTX 4060 Ti):
  1.38 ms a frame at zoom 1 and 0.96 ms at zoom 2 with soft edges (0.62 and 0.50 sharp), against 1.61
  and 0.97 ms with Godot's lights.

## Deliberately not handled

- Relief from more than one light is from their sum's slope: one direction a point, not each lamp's.
- Light bending round corners and through doorways the way the TypeScript sweeps let it: lamps cast
  straight shadows; only starlight keeps the sweeps. Nor does a closed door leak `Light.WallOut`.
- Heroes and machines cast no shadows.
- Light follows the grid as the sim has it, not a door this peer has just toggled (0014).
- The multimeshes carry a fixed huge `CustomAabb`: Godot culls one by the box its quads filled when
  first drawn and keeps it, so spots first drawn with nothing glowing never showed. The shader drops
  what is out of reach anyway.
- Cost grows with the lights on screen times their reach squared; no light is cached between frames.
  A cache of each still light's shadows is the next step if a ship ever outgrows the budget.
- A change's starlight is worked out over a square of about 181 cells across (four times the reach and
  the cell), 1.5 ms in a debug build: the reach is what the faintest texel allows, not where the light
  of this ship fades out. Changes far apart in one tick are worked out apart. The texture is uploaded
  whole after a change (2 MB on a fully built 1024×512), as Godot updates no part of one.
- Starlight's texture moves with what is built, and the shader finds a texel by
  `(world − corner) / size × texels` in floats: at a texel's edge the rounding can fall the other way
  than over the whole grid. Seen once in a run of the wreck: one pixel, one step of 255 in a colour.

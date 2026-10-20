# Light (client)

Light without a word (0019, 0029, 0070): a dim ship lit by its lamps, its fires, whatever glows,
and by the stars, whole in open space and falling off within a few cells of a hole or an opening. Drawn only: nothing of it reaches the sim.
Numbers from `game/Content/Light.cs`.

- **Reads:** `IGridQueries` — `StructureLayer` (walls and closed doors stop light), `ShapeLayer` (a wall cut to a shape stops it with its solid half, 0069), `BackLayer`,
  `Machines` (lamps, fires; machines dim starlight), `MachineAt`, `Width`/`Height`, `Version`,
  `ChangedSince` and `Built`; `Glims.Shared` and `Flares.Shared` (what glows, 0075); every `render/Glow` in the tree (a
  planner's screen, a charging station's light, a smelter's coils, a grow capsule's lamp, a station's cold mark).
- **Costs the cells changed, not the ship or the world** (F-7, 0051). `LightView.Sync` after a tick
  does nothing while the grid's `Version` stands; when it moved, the grid's log names the cells changed
  since its last look. A cell whose structure changed has its texel of the occupancy texture set again
  (`Lightmap.Follow`); one past the texture lays the texture out anew over `Built`, whose corner the
  shader is told (`origin`). Past the texture every cell is open, so a texture that still covers what
  was built is drawn alike. Lamps are laid out again only when the log names a machine's cell. Starlight
  is worked out again only round the cells changed in structure, back wall or machine
  (`StarField.Follow`): within `StarField.Reach` (12) cells and a sky's reach (`Light.StarSky`, 2) of
  them, from what lies within `Reach` more. A change alters the sky of the cells within 2 of it; light
  keeps at most the brightest way through cells of any kinds (into a wall `WallIn`, on `WallDeep` or
  out `WallOut`, through the open `StarKeep`), so past 12 cells what crossed a changed cell, or came
  from a changed sky, is under the faintest light a texel shows, and every texel there is as it was; and
  light from past twice the reach is as faint where it is written. The texels are those working it all out gives,
  byte for byte (`BuiltBoundsTests`, over ships and halls where the light runs far). A first look, or
  one the log no longer reaches back to, lays everything out anew, as before (F-6 slice 0): starlight
  over `Built` and 4 cells of space round it, past which every cell is open stars at full, which is what
  the texel at the texture's edge holds, and `Lit` reads that edge texel for anything beyond. A door
  toggled every tick on a fully built 1024×512 world cost the light 35 ms a frame, starlight 33 ms a
  change; now 0.9 and 1.5 ms (debug build), whatever the size of the ship. Since starlight falls off
  (0070), a change costs it 0.2 ms; working it all out anew, 4.5 ms on 256×128 and 41 on 1024×512.
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
  door, closed hatch, open, by the doorway's shape (0067), or a wall cut to ◢ ◣ ◤ ◥, `Sources.Slope` and its shape's code
  (0069); a texel set again for each wall or door changed, its shape too,
  and those above and below it), and drops the texel if a wall or a closed door's or hatch's panel stands
  between: shadows to the pixel. A door's panel is the columns of `door_closed_0`, a hatch's the rows of
  `hatch_closed_0`, read from the pictures. The walls a texel is in are passed until the
  line comes out of them, so a wall is lit on the faces light reaches; in a door's or hatch's cell only
  a texel on the panel itself passes it, along the doorway's cells till the line leaves the panel, so a door is
  lit whole and the back wall beside it stays dark to a light beyond it. In a wall cut to a shape the solid half
  is a wall and the open half the open: the solid is a half plane in the cell, so the line's two ends there tell it
  — passed from a texel in the solid till the line comes out of it, so the diagonal is the face lit, stopping a line
  from the open that reaches it. Light goes on through an open half, and a slope's shadow runs along its diagonal. A lamp (`Light.LampSize`) and a
  fire (`Light.FireSize`) have a width: seen from a texel the light is a line across that wide; its two
  ends are walked, and where they disagree — a shadow's edge — seven points between, the share seen
  kept. Edges are soft over as much as the light seen from them is wide, as Godot's PCF softened them.
- **Lamps:** `Light.Lamp` (0.8, 0.64, 0.42), dim and warm, keeping `Light.LampKeep` (0.74) a cell, each
  colour at most `Light.LampMost`, out to `Light.LampReach` (8) cells: a room lit to walk, its corners
  dark, so a fire, a screen or a machine's glow reads in it; as bright as `LightView.LampShare` says — the power it gets, flickering when
  short (`Flicker`) — and an unlit additive glow. Short of power, in light too (`LampSparks`, 0075), the flicker's timing and
  fades kept: each time it catches, a small warm glint, more often than not a few sparks dropping to the floor under it
  and bouncing once, half the time an arc or two at its socket; rarely an arc while lit; while dark a dull ember in
  the filament cooling over a second. Nothing flashes the room white.
- **Fire:** from where a furnace's or a burner generator's fire gives its light (`LightView.FireAt`, which
  `Main` answers from `FurnaceView.LightAt`: a furnace's bottom row's middle, low in a generator's mouth), `Light.Fire` keeping `Light.FireKeep` a cell
  out to `FireReach` (8) cells, as bright as `LightView.Fire` says (`FurnaceView.BlazeOf`: a new
  brightness with each picture of the flames, which the glow breathes toward, past whole as a coal cracks: 0075), and a glow at the mouth.
  The furnace view runs before this one each frame, so the light follows the picture shown.
- **Spots:** first each flash of `Flares.Shared` (0075: a hit's, a break's, a spark burst's flicker, a
  molten spot, a jetpack's pool, a venting room's red beat), at its strength, keeping its own keep out to its own reach;
  then each electric arc (`Bolts.Shared`) at its middle in cyan, `Fx.BoltLight` of its brightness, less for a short one
  (a crawl over a coil barely, a leap the room round it); then each luminous particle
  that gives light (`Glims.Shared`, `Luminous.Light`) at `Light.Particle` times `Fx.ParticleLight` of its
  colour on its ramp as it fades,
  keeping `Light.ParticleKeep` a cell out to `ParticleReach` (2) cells: a bright point, not a wash, for the
  fire lights the room round a furnace anyway; at most `Fx.MostLights` (48) of them together, so a big
  break lights no more than a busy furnace room. Then each `Glow` at its colour, keeping
  `Light.ScreenKeep` out to `ScreenReach` (3).
- **Starlight** (0070): `StarField` (plain C#, tested) works it out cell by cell. The stars shine in every
  open cell with no back wall and past the grid's edge, and into a wall cut to a shape with no back wall,
  whose open half shows them, though it lets on no more than any wall (0069) — each as brightly as its sky
  is open: of the 5×5 cells round it (`Light.StarSky` 2), itself among them, one the stars shine into, a
  lone hole in a back wall, gives `Light.StarHole` (0.5) of them, `Light.StarFullSky` (12) or more the
  whole, so open space is whole everywhere, beside a flat hull too, and a bigger hole is brighter. From there
  row and column sweeps spread it, keeping `Light.StarKeep` (0.6) a cell, a machine's `Light.StarMachine`
  (0.48): through a doorway or a tear the room takes 0.6, 0.36, 0.22… of the stars, round a hole half that,
  and a few cells on it is dark — a hole adds a cold light near itself, not a lit room. Worked out only when
  walls, doors, back walls or machines change, and then only round them; and
  `Starlight` hands it to `render/Lit.cs` as a texture of a texel a cell laid where that region lies,
  its alpha marking a wall's or closed door's cell; a picture blends it only with cells of its own side.
- **`render/Lit.cs`** draws everything set with `Lit.Set`, unshaded, the lightmaps blended between
  texels so a shadow's edge does not step at the world's pixels on screen: ambient, plus the stars and the
  main map — scaled down together where they would pass whole in a colour, so a fire's orange over a
  lamp stays orange — plus the spots, together at most `Light.ParticleMost` by their brightest colour;
  a wall takes `Light.WallIn` of what reaches its face. A normal-mapped picture — a machine or a
  socket domed over its shape, a door rolled side to side, a hatch top to bottom, walls flat (`PixelArt.VolumeMap`), so the
  side toward a light is the brighter and no rivet casts a shadow — is shaded toward where the lamps' light comes from — up the main map's slope over
  half a cell each side, tilted 45° up — and the sum held at whole after it; where the light hardly
  changes the picture is shaded as facing it. Near a fire under a lamp the slope turns a little with
  the flames. The `CanvasModulate` of `Light.Ambient` stays
  for anything drawn lit without `Lit`; unshaded things are not tinted by it.
- **What it lights:** the ship's cells and machines, cables, stacks on the floor, debris and every
  hero's figure; not fire, glows, particles, plans or overlays.
- **Cost** on the reference world with its 103 lamps lit (`--perf --lit --measure`, RTX 4060 Ti):
  1.38 ms a frame at zoom 1 and 0.96 ms at zoom 2 with soft edges (0.62 and 0.50 sharp), against 1.61
  and 0.97 ms with Godot's lights. Walls cut to shapes (0069): 1.20–1.26 ms a frame before and 1.23–1.28 after at
  zoom 1 (`--view 128,64,1`), 0.78–0.79 both at zoom 2, three runs each — within the runs' spread; with five slopes in
  every 8×8 room of it, 1.16 and 0.76. Dim lamps of 11 cells (0070), in a 1600×900 window: 1.73–1.82 ms a frame at
  zoom 1 against 1.87–2.02 before, 0.91–0.96 at zoom 2 against 0.88–0.93.

## Deliberately not handled

- Relief from more than one light is from their sum's slope: one direction a point, not each lamp's.
- Light bending round corners and through doorways the way the TypeScript sweeps let it: lamps cast
  straight shadows; only starlight keeps the sweeps, and it falls off too fast to bend far.
- A cell's sky is counted in a square round it, not along lines of sight: how the hull round a hole is
  shaped does not change its light, and a big hole is brighter than a lone one only by its count. Nor does a closed door leak `Light.WallOut`.
- Heroes and machines cast no shadows.
- Light follows the grid as the sim has it, not a door this peer has just toggled (0014).
- The multimeshes carry a fixed huge `CustomAabb`: Godot culls one by the box its quads filled when
  first drawn and keeps it, so spots first drawn with nothing glowing never showed. The shader drops
  what is out of reach anyway.
- Cost grows with the lights on screen times their reach squared; no light is cached between frames.
  A cache of each still light's shadows is the next step if a ship ever outgrows the budget.
- A change's starlight is worked out over a square of about 53 cells across (four times the reach, twice
  the sky's and the cell), 0.2 ms in a debug build: the reach is what the faintest texel allows, not where
  the light visibly fades out (3–4 cells). Changes far apart in one tick are worked out apart. The texture is uploaded
  whole after a change (2 MB on a fully built 1024×512), as Godot updates no part of one.
- Starlight's texture moves with what is built, and the shader finds a texel by
  `(world − corner) / size × texels` in floats: at a texel's edge the rounding can fall the other way
  than over the whole grid. Seen once in a run of the wreck: one pixel, one step of 255 in a colour.

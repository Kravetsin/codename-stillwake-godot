# Light (client)

Light without a word (0019, 0029, 0070): a dim ship lit by its lamps, its fires, whatever glows,
and by the stars, whole in open space and falling off within a few cells of a hole or an opening. Drawn only: nothing of it reaches the sim. Tuned at the 16 px cell: every reach and count of cells is twice what it was and every keep a cell its square root, so light falls off over the same pixels (0104); the shader walks up to 160 cells a line.
Numbers from `game/Content/Light.cs`.

- **Reads:** `IGridQueries` — `StructureLayer` (walls and closed doors stop light, every wall whole, 0106), `BackLayer`,
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
  (`StarField.Follow`): within `StarField.Reach` (some 24) cells and a sky's reach (`Light.StarSky`, 4) of
  them, from what lies within `Reach` more. A change alters the sky of the cells within 4 of it; light
  keeps at most the brightest way through cells of any kinds (into a wall `WallIn`, on `WallDeep` or
  out `WallOut`, through the open `StarKeep`), so past some 24 cells what crossed a changed cell, or came
  from a changed sky, is under the faintest light a texel shows, and every texel there is as it was; and
  light from past twice the reach is as faint where it is written. The texels are those working it all out gives,
  byte for byte (`BuiltBoundsTests`, over ships and halls where the light runs far). A first look, or
  one the log no longer reaches back to, lays everything out anew, as before (F-6 slice 0): starlight
  over `Built` and 8 cells of space round it, past which every cell is open stars at full, which is what
  the texel at the texture's edge holds, and `Lit` reads that edge texel for anything beyond. A door
  toggled every tick on a fully built 1024×512 world cost the light 35 ms a frame, starlight 33 ms a
  change; now 0.9 and 1.5 ms (debug build), whatever the size of the ship. Since starlight falls off
  (0070), a change costs it 0.2 ms; working it all out anew, 4.5 ms on 256×128 and 41 on 1024×512.
- **No Godot lights.** Godot lights a drawn thing by at most 15 lights and drops the rest: a furnace's
  embers once pushed a lamp out and whole tile chunks went dark. All light is composed here instead,
  with no limit on how many lights touch a thing.
- **Lightmap** (`Lightmap.cs`): two `SubViewport`s over the view and a margin of 8 cells, sized from the view's size alone with a
  texel spare, so the camera moving never resizes them (a resize remakes both textures, some 4 ms; sized to where the view
  fell on the pixels, they once changed by a texel nearly every frame of a run), HDR, cleared
  each frame, a texel a world pixel (more pixels a texel past 4096 texels across), laid with their
  `CanvasTransform` — no camera. *Main* takes lamps and fires, *Spots* embers, sparks and glows, which
  `Lit` caps apart. Each light whose reach touches the view is one additive quad of a multimesh
  (`Sources.Pack`): its colour falling off by its keep a cell out to its reach as the TypeScript game
  spread it, each colour at most its most, times its strength. The quad's shader walks the cells from
  each texel to the light through an occupancy texture, a byte a cell of what is built (`Sources.Occupy`: wall, closed
  door, closed hatch, open, by the doorway's shape (0067); a texel set again for each wall or door changed,
  and those above and below it), and drops the texel if a wall or a closed door's or hatch's panel stands
  between: shadows to the pixel. A door's panel is the columns of `door_closed_0`, a hatch's the rows of
  `hatch_closed_0`, read from the pictures. The walls a texel is in are passed until the
  line comes out of them, so a wall is lit on the faces light reaches; in a door's or hatch's cell only
  a texel on the panel itself passes it, along the doorway's cells till the line leaves the panel, so a door is
  lit whole and the back wall beside it stays dark to a light beyond it. A lamp (`Light.LampSize`) and a
  fire (`Light.FireSize`) have a width: seen from a texel the light is a line across that wide; its two
  ends are walked, and where they disagree — a shadow's edge — seven points between, the share seen
  kept. Edges are soft over as much as the light seen from them is wide, as Godot's PCF softened them.
- **Lamps:** `Light.Lamp` (0.8, 0.64, 0.42), dim and warm, keeping `Light.LampKeep` (0.86) a cell, each
  colour at most `Light.LampMost`, out to `Light.LampReach` (16) cells: a room lit to walk, its corners
  dark, so a fire, a screen or a machine's glow reads in it; as bright as `LightView.LampShare` says — the power it gets, flickering when
  short (`Flicker`) — and an unlit additive glow. Short of power, in light too (`LampSparks`, 0075), the flicker's timing and
  fades kept: each time it catches, a small warm glint, more often than not a few sparks dropping to the floor under it
  and bouncing once, half the time an arc or two at its socket; rarely an arc while lit; while dark a dull ember in
  the filament cooling over a second. Nothing flashes the room white. An arc makers' lamp hung aboard (0091, `arcLamp`) is
  lit alike but in their cold `Light.ArcLamp` (0.56, 0.78, 1), three tenths stronger, keeping 0.883 out to 20 cells, its glow cyan.
  Its body (`machine_arcLamp`, a dark globe) is the grid's tile whatever its power; while it shines `LightView` lays the globe
  alight over it (`machine_arcLamp_lit`, lit by the world) and adds its glow (`machine_arcLamp_glow`), both as bright as it
  shines (`ArcLampLooks`), as a home's lit ceiling lamp draws; at full power the ceiling lamp's arc crackles across it (`Crackle`).
- **The homes' lamps** (0091): `LightView.Homes`, the light of the arc makers' lamps and hearths near the view (`HomesView`).
- **Fire:** from where a furnace's or a burner generator's fire gives its light (`LightView.FireAt`, which
  `Main` answers from `FurnaceView.LightAt`: a furnace's bottom row's middle, low in a generator's mouth), `Light.Fire` keeping `Light.FireKeep` a cell
  out to `FireReach` (16) cells, as bright as `LightView.Fire` says (`FurnaceView.BlazeOf`: a new
  brightness with each picture of the flames, which the glow breathes toward, past whole as a coal cracks: 0075), and a glow at the mouth.
  The furnace view runs before this one each frame, so the light follows the picture shown.
- **Spots:** first each flash of `Flares.Shared` (0075: a hit's, a break's, a spark burst's flicker, a
  molten spot, a jetpack's pool, a venting room's red beat), at its strength, keeping its own keep out to its own reach;
  then each electric arc (`Bolts.Shared`) but a fine one at its middle in cyan, `Fx.BoltLight` of its brightness, less for a short one
  (a lamp's spark of an arc barely, a long one the room round it; a fine one lights nothing, an arc-fitted drill's coil
  holding a pulsing light of its own); then each luminous particle
  that gives light (`Glims.Shared`, `Luminous.Light`) at `Light.Particle` times `Fx.ParticleLight` of its
  colour on its ramp as it fades,
  keeping `Light.ParticleKeep` a cell out to `ParticleReach` (2) cells: a bright point, not a wash, for the
  fire lights the room round a furnace anyway; at most `Fx.MostLights` (48) of them together, so a big
  break lights no more than a busy furnace room. Then each `Glow` at its colour, keeping
  `Light.ScreenKeep` out to `ScreenReach` (3).
- **Starlight** (0070): `StarField` (plain C#, tested) works it out cell by cell. The stars shine in every
  open cell with no back wall and past the grid's edge — each as brightly as its sky
  is open: of the 5×5 cells round it (`Light.StarSky` 2), itself among them, one the stars shine into, a
  lone hole in a back wall, gives `Light.StarHole` (0.5) of them, `Light.StarFullSky` (12) or more the
  whole, so open space is whole everywhere, beside a flat hull too, and a bigger hole is brighter. From there
  row and column sweeps spread it, keeping `Light.StarKeep` (0.6) a cell, a machine's `Light.StarMachine`
  (0.48): through a doorway or a tear the room takes 0.6, 0.36, 0.22… of the stars, round a hole half that,
  and a few cells on it is dark — a hole adds a cold light near itself, not a lit room. Worked out only when
  walls, doors, back walls or machines change, and then only round them; and
  `Starlight` hands it to `render/Lit.cs` as a texture of a texel a cell laid where that region lies,
  its alpha marking a wall's or closed door's cell; a picture blends it only with cells of its own side.
- **A planet's sky** (0076): the light through the open sky is its surface kind's (`Light.SkyOf`, from
  `Surfaces.Kind.Sky`: the test strip's a warm daylight, 0.95, 0.90, 0.80), worked out and spread as starlight is, so
  a cave of back-walled ground is dark a few cells in; the stars' where the kind names none. `Main` hands it to
  `LightView`. The faintest texel is judged by the brightest sky there is (`Light.BrightestSky`), so a change's reach
  covers every kind. Behind the ground `render/Stars` draws the sky in place of space, at `Light.SkyBackdrop` of its
  light, the stars, far wrecks and dust hidden (`Stars.Daylight`).
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
  and 0.97 ms with Godot's lights. Dim lamps of 11 cells (0070), in a 1600×900 window: 1.73–1.82 ms a frame at
  zoom 1 against 1.87–2.02 before, 0.91–0.96 at zoom 2 against 0.88–0.93.

- **Helmet lamps** (0077): a beam and a glow a lamp worn, from its bulb on the helmet (`LightView.HeadLamps`, answered by
  the crew's `HeroView.HeadLamps()`, where it is and the way the head looks): `Light.HeadLamp`, a warm (0.95, 0.82, 0.6)
  keeping `Light.HeadLampKeep` (0.8) a cell out to `Light.HeadLampReach` (9) within `Light.HeadLampHalfAngle` (30°) of
  that way, faded over a soft edge, its bulb 3 px wide; and `Light.HeadGlow` (0.4) of it out to 2 cells all round, from halfway between the bulb and the
  neck, inside the hero (`Sources.HeadLamp`): standing against a wall the bulb pokes into it, and a light in a wall lights
  nothing in the open, so the beam then shines from there too. A beam
  is a `LightSource` with a `Toward`, packed with its most as 2 and its heading as a share of a turn, which the shader
  reads back (`beam_cos`, `beam_soft`): one dot product a texel. Free, as bright anywhere, aboard too.

- **Underground on a planet is black** (0077): no ambient light there (`Lit.Ambient`, set by `LightView` from its sky; the
  ship keeps `Light.Ambient`), daylight keeps `Light.GroundKeep` (0.3) a cell of air in from the open (`StarField`'s keep), so
  it dies a cell or two round a bend, but falls down an opening keeping `Light.GroundDownKeep` (0.8: the down sweep's shares,
  `StarField`'s `down`), so a shaft is lit six to eight cells down; and lamps and beams go into solid ground keeping
  `wall_keep` (0.35) a cell of the way through it from its face (`through` in the lightmap's shader), aboard too. On a
  planet's ground (`LightView`'s `ground`, `Lightmap.SoakThrough`, 0080) they go on out into the open past it, still keeping
  `wall_keep` a cell of rock crossed: a shadow cast by rock fades in as the rock between thickens. Stopped dead, the lit rock
  met the dark open beside it along the rock's face, a step a cell's edge long: caves lit by their growths read as boxes. Aboard,
  walls stop light, so a lamp never lights the next room through one.
- **Daylight soaks into the ground** (0079): going down from a wall cell into the wall under it, a planet's daylight keeps
  `Light.GroundSoak` (0.72) a cell (`StarField`'s `soak`, the down sweep's wall-to-wall share), so the top few cells of
  ground under the sun are lit, fading, as the "Stillwake" style shows them; sideways and up a wall still keeps
  `WallDeep`, and out of a wall into a cave `WallOut`: caves stay dark. The sky's light is `Surfaces.TestStrip.Sky`, a soft
  warm white (1.0, 0.97, 0.9).
- **Plants glow** (0079): each glowing part of a plant near the view (`LightView.Glows`, answered by flora's
  `FloraView.Lights`: a seed pod, a berry, a bell) lights round it as a small lamp, in its colour at
  `FloraLooks.LightStrength`, keeping `LightKeep` (0.4) a cell out to `LightReach` (2) cells; at most three a plant.
- **Caves glow** (0080): each glowing growth of a cave near the view (`LightView.Caves`, answered by flora's `CaveView.Lights`:
  a glowcap, a crystal, threads, moss, a heat crack) is a lamp of its own on the main map, its colour, strength, keep and
  reach its kind's (`CaveLooks`), at most `CaveLooks.MostLights` (56) of them, the nearest the view's middle: a cave is not
  uniformly black, and a hall full of them costs the light what a lit room of lamps does.

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

# Light (client)

## Files

| File | Purpose |
| --- | --- |
| `Crackle.cs` | The arc makers' arcs crackling (0091, `Emits.Arc`). |
| `logic/Flicker.cs` | How brightly a lamp short of power shines, flickering. |
| `LampSparks.cs` | A lamp short of power, in light (0075): glints, sparks, arcs. |
| `LightView.cs` | Gathers every light each frame for the lightmap (0019, 0029, 0070). |
| `Lightmap.cs` | All light composed on the GPU each frame into two maps over the view (0029). |
| `logic/Sources.cs` | What the shader reads: walls and doors light stops at, and each light. |
| `logic/StarField.cs` | Starlight cell by cell (0019, 0070). |
| `Starlight.cs` | The starlight, tiles of a texel a cell, for `render/Lit.cs`. |
| `logic/StarTiles.cs` | Starlight tiles: those a change wrote. |

A dim ship lit by its lamps, fires, whatever glows, and the stars, whole in open space and falling off
within a few cells of a hole (0019, 0029, 0070). Drawn only: nothing reaches the sim. Tuned at the 8 px
cell (0104); the shader walks up to 160 cells a line. Numbers: `game/Content/logic/Light.cs`; `LightView` holds
the reaches of fire, particles, screens.

- **Reads:** `IGridQueries` (walls and closed doors stop light, every wall whole, 0106; walls' and back walls'
  materials, machines, the change log); `Glims`, `Flares`, `Bolts` (0075); every `render/Glow` in the tree.
- **Costs the cells changed, not the world** (0051). `LightView.Sync` does nothing while the grid's
  `Version` stands; else the grid's log names the changed cells. A changed structure cell resets its
  occupancy texel (`Lightmap.Follow`); one past the texture lays it out anew over `Built`. Lamps are
  relaid when a machine's cell changed. Starlight is redone only within `StarField.Reach` and the
  sky's reach of changed structure, back wall or machine cells (`StarField.Follow`), byte for byte a full
  redo (`BuiltBoundsTests`); only tiles it wrote are sent. A first look, or one past the log, redoes all over `Built` and
  `StarField.Margin`; beyond is open stars at full, and `Lit` reads the edge texel there.
- **No Godot lights** (0029): they light a thing by at most 15.
- **Lightmap:** two HDR `SubViewport`s over the view and a margin, sized from the view's size with a texel
  spare so the camera never resizes them; cleared each frame, a texel a world pixel (more past
  `MostTexels`). *Main* takes lamps and fires; *Spots* embers, sparks, glows, capped apart by `Lit`. Each
  light touching the view is one additive multimesh quad (`Sources.Pack`), falling off by its keep a cell.
- **Shadows to the pixel:** the shader walks from each texel to the light through an occupancy texture
  (`Sources.Occupy`: wall, closed door, open, by doorway shape, 0067) and drops the texel behind a wall or a
  closed door's panel: the columns of `door_closed_0` across the doorway (0104), far column in
  `Sources.DoorFar`. A closed hatch stops light across its row (0111). Walls a texel is in are passed until
  the line leaves them, so walls are lit on faces light reaches; in a door's cell only a texel on the panel
  passes, so a door is lit whole and the back wall beside it stays dark to light beyond. Soft edges: a lamp
  or fire is a line (`LampSize`, `FireSize`); its ends are walked, and seven points between if they differ.
- **Lamps:** every `Glow` machine (0113) lights by its `Light.Lamps` row (colour, strength, reach, keep,
  halo, arc glass); a new lamp is a row. The starter lamp is dim and warm. Brightness is
  `LightView.LampShare`, its power, flickering when short (`Flicker`). Short of power (`LampSparks`, 0075) a
  catch gives a glint, often sparks, sometimes an arc; dark, a cooling ember.
- **Arc lamp** (0091): cold `Light.ArcLamp`, a cyan glow. Its body (`machine_arcLamp`) is the grid's tile;
  shining, `LightView` lays `machine_arcLamp_lit` and `_glow` over it by `ArcLampLooks`; at full power its
  arc crackles (`Crackle`). **Homes' lamps** (0091): `HomesView.Lights`.
- **Fire:** at `FurnaceView.LightAt`, as bright as `FurnaceView.BlazeOf` (past whole as a coal cracks,
  0075), plus a glow at the mouth. The furnace view runs first, so light follows the flames shown.
- **Spots,** in order: `Flares` flashes (0075) at their own strength, keep, reach; arcs but fine ones, at
  their middle in cyan by `Fx.BoltLight`, less when short; luminous particles (`Luminous.Light`) at
  `Light.Particle` × `Fx.ParticleLight`, at most `Fx.MostLights`; each `Glow` by `Light.ScreenKeep`.
- **Starlight** (0070, `StarField`, plain C#, tested): stars shine in every open cell with no back wall and
  past the grid, as bright as its sky is open: of the cells within `StarSky`, a lone hole gives `StarHole`,
  `StarFullSky` or more the whole. Row and column sweeps spread it by `StarKeep` a cell (`StarMachine`
  through a machine): a hole lights near itself, not the room. `Starlight` hands it to `render/Lit.cs`,
  alpha marking walls and closed doors; a picture blends only cells of its own side.
- **Clear walls** (glass, clear-glass ADR): lamps pass them keeping `ClearKeep` a cell (`Sources.Clear`);
  starlight leaves them by `ClearOut`; a clear back wall lets `ClearBack` of the stars in.
- **A planet's sky** (0076): its surface kind's light (`Light.SkyOf`), spread as starlight; the stars'
  where the kind names none. The faintest texel is judged by `Light.BrightestSky`, so a change's reach
  covers every kind. `render/Stars` draws `PlanetSky` behind the ground, hiding stars, wrecks, dust (0079).
- **`render/Lit.cs`** draws what `Lit.Set` marks, unshaded, lightmaps blended between texels: ambient,
  stars and main scaled down together where a colour would pass whole (orange stays orange), plus spots,
  at most `ParticleMost`; a wall takes `WallIn` of what reaches it. Normal-mapped pictures
  (`PixelArt.VolumeMap`) shade toward the main map's slope, tilted 45° up, read a cell each way on their own
  side only; where it is flat, as facing it.
  `CanvasModulate` of `Light.Ambient` tints only what is drawn without `Lit`, not unshaded things.
- **Lit:** cells, machines, cables, floor stacks, debris, heroes; not fire, glows, particles, plans, overlays.
- **Helmet lamps** (0077, `LightView.HeadLamps` from `HeroView.HeadLamps()`): a `HeadLamp` beam within
  `HeadLampHalfAngle` of the head's way, plus `HeadGlow` round, both from its lens, or in a wall the open
  point next to it toward the neck (`Sources.HeadLamp`). A beam is a `LightSource` with a `Toward` (`beam_cos`, `beam_soft`).
- **Underground is black** (0077): no ambient (`Lit.Ambient` from the sky; the ship keeps `Light.Ambient`).
  Daylight keeps `GroundKeep` a cell of air in from the open, `GroundDownKeep` down a shaft. Lamps and
  beams enter solid ground by `wall_keep` a cell (`through`), aboard too; on a planet
  (`Lightmap.SoakThrough`, 0080) they go on past rock, by `wall_keep` a cell crossed. Aboard,
  walls stop light.
- **Daylight soaks into the ground** (0079): wall to wall downward it keeps `GroundSoak`, so the top cells
  under the sun are lit; sideways and up `WallDeep`, out into a cave `WallOut`: caves stay dark.
- **Views give light** (`ILightSource`, `LightView.Shining`). **Plants glow** (0079): parts near the view are lamps by
  `FloraLooks`, `MostLights` a plant at most. **Caves glow** (0080): growths near the view
  (`CaveView.Lights`) are main map lamps by `CaveLooks`, at most `MostLights`, nearest the middle first.

## Deliberately not handled

- Relief from several lights follows their sum's slope, not each lamp's.
- Lamps cast straight shadows; only starlight sweeps, and it falls off too fast to bend far.
- A cell's sky is counted in a square, not by line of sight; a closed door leaks no `WallOut`.
- Heroes and machines cast no shadows.
- Light follows the sim's grid, not a door this peer just toggled (0014).
- Multimeshes carry a fixed huge `CustomAabb`.
- Cost grows with lights on screen × reach squared; nothing is cached.
- A change's starlight is redone over about four reaches across. The texture uploads whole.
- Starlight's texel lookup is in floats: at a texel's edge rounding may differ by one colour step.

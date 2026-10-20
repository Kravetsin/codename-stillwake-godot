# effects (client): light and matter that fly

Particles without a word, first ported from `apps/game/src/features/effects/` and `render/particles.ts`
(G-08), now drawn in the luminous style (0075): what glows is light added over the world — thin streaks
white-hot at the head, stretched by speed along their own curling path, fading by brightness as their colour
cools, each lighting the room a little; a one-frame cross glint and a faint shock line on key moments; heat
shimmer behind hot things — and dust and smoke are matter, fine motes and soft translucent wisps lit by the
room. Drawn only, never part of the world: each peer emits its own from what it sees.

| File                        | Holds                                                                             | Godot |
| --------------------------- | --------------------------------------------------------------------------------- | ----- |
| `game/render/Luminous.cs`   | `Glimmer` (streak, mote, wisp, chip), `Luminous` (a look and its motion) and its presets, `Glim`, `Glims` and `Glims.Shared`, `LuminousMath` (ramps, brightness, noise, curl, a streak's points) | no |
| `game/render/Flares.cs`     | `Glint`, `Shock`, `Flash` and `Flares.Shared`: the key moments' glints, shock lines and lights | no |
| `game/render/Strokes.cs`    | every stroke of light in one multimesh, soft-edged at a screen pixel                | yes   |
| `game/render/Shimmer.cs`    | heat shimmer over a box of the world, a screen-texture shader                      | yes   |
| `game/Content/Fx.cs`        | the knobs: length, count, turbulence, glow, glint hold, shimmer; the lights' cap    | no    |
| `game/render/Particles.cs`  | the old squares, `Effect` and its presets, `Particles.Shared`: dust, air, puffs till slice 2 | no |
| `EffectsView.cs`            | the layer: steps all of it each frame and draws it                                | yes   |

- **Emitted by (luminous):** the furnace (`FurnaceView`) — embers, ash, a cracking coal's sparks and glint,
  for a furnace and a burner generator alike; the building (`BuildGlow`) — spark bursts off a screwdriver,
  glints, smoke off its molten spot, motes off a plan's cut and round a finished plan; the debris
  (`DebrisView`) — an axe's hit sparks, dust, chips, glint, shock line and flash, a drill's few sparks, a
  break's sparks, dust, cloud, glint, ring and flash, and a broken piece's embers (`BreakView`).
- **Emitted by (old squares):** the hero's figure (`crew/Figure.cs`) — dust from a boot each half stride
  and from a landing; the air (`AirView`) and a pod's hatch (`PodView`) — air; the harpoon and the jetpack
  (`HeroView`) — puffs.
- **Motion:** each look's gravity (aboard; none in space, chosen by the emitter), drag, a turn of its way by
  smooth value noise (`Spin`, keeping its speed) and a push along the noise's curl (`Turbulence`), a
  floor it may bounce off; a streak samples its path every 60th of a second whatever the frame rate, its
  last 24 points, and is drawn back along them as far as its speed times its `Stretch` says.
- **Drawn** over everything in the world, plans included (`Layers.Effects`), under an overlay, a pixel of
  the screen a unit so strokes are as fine as the screen allows over the world's pixels: matter lit by the
  room (`Lit`) — motes, wisps swelling and stretched along their way, chips tumbling; then light added —
  fine dots, halos round streak heads and glints, each matter mote again at 0.4 so dust catches a little
  light of its own; then every stroke at once in one multimesh (`Strokes`): streaks from a whitened head
  through their colour to nothing at the tail, shock rings, glints' spikes (a long one across, a shorter one
  up and down, faint diagonals, each clear at its ends), all soft over the last screen pixel either side and
  at least 1.1 px across. A glint is whole on its first 60 Hz frame and gone after `Fx.GlintHold`, drawn at
  least once at any frame rate. The old squares as before: a square of the effect's colour, unlit.
- **Light (0019):** flashes first, then luminous particles that give light, as spots of the light feature (`LightView`), at most `Fx.MostLights` (48) together.
- **Heat shimmer:** a `Shimmer` node per hot thing, under the flames and over what is behind them: the screen
  under its box sampled again, each row nudged sideways by two waves and noise rising through it, faded out
  toward its edges, a texel with nothing drawn in it adding nothing, so it never darkens. Every shimmer is drawn at
  `Layers.Shimmer` (with the fire, before its flames) whoever owns it, from one copy of the whole screen taken there
  (`Shimmer.ScreenCopy`, `Main`'s first child, shown while any shimmer is): the world drawn so far and nothing
  over it, so the aim's outline, plans, marks, the tablet and words by the pointer stay crisp and are never
  wobbled. Godot's own copy, taken at the first reader, and a copy per box both failed: the aim's outline is a
  canvas group sharing that buffer, and a shimmer drawn after it, or reading past its copied rect, showed its
  masks (a black disc over a weld, a green ghost of the furnace aimed at). `-- --testbed --near=furnace
  --shimmer-demo` records those cases (`dev/ShimmerDemo.cs`). The Esc menu's settings
  switch it off (`Shimmer.Enabled`, kept in `user://settings.cfg`, on by default).
- **Cost** (`-- --measure`, RTX 4060 Ti, 1920×1080): the testbed's furnace room 1.52 → 1.62 ms a frame,
  164 draw calls against 159; `-- --fx-bench` (eight fires with their shimmer, four plans worked, a wreck
  broken every second, and a plan welded hot under the aim's outline: some 600 particles) 2.4 ms a frame, 0.7 ms
  of it this layer's CPU, 220 draw calls.
  Godot draws each polyline as a draw call with a fresh buffer of its own; drawn so, the bench took 5.7 ms.
- At most 1600 luminous particles and 2000 old ones fly at once: a big breach thins out rather than costing frames.

`particles.test.ts` is ported (`ParticlesTests`); the luminous arithmetic is tested in `LuminousTests`.

## Deliberately not handled

- Slice 2 (0075): the jetpack's plume, boots' dust, the harpoon's puff and line, venting air, the drone, a
  door's slide, a lamp's glow, the pick-up popup and the drill's bolts are still drawn as before.
- Particles fly through walls: nothing but a floor an emitter names stops them.

## Differences from the TypeScript

- A glowing particle lights by a spot of the light feature's lightmap (0029), where the TypeScript game added
  it to the cell light it worked out every frame.

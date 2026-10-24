# effects (client): light and matter that fly

## Files

| File | Purpose |
| --- | --- |
| `EffectsView.cs` | Every particle the features emit: the luminous ones of `Glims.Shared`, the glints, shock lines … |
| `Impacts.cs` | Where a hit lands and what it strikes (0094), chosen from what the sim says is there, as `GroundSounds` chooses its … |

Particles without a word, first ported from `apps/game/src/features/effects/` and `render/particles.ts`
(G-08), now drawn in the luminous style (0075): what glows is light added over the world — thin streaks
white-hot at the head, stretched by speed along their own curling path, fading by brightness as their colour
cools, each lighting the room a little; a one-frame cross glint and a faint shock line on key moments; heat
shimmer behind hot things — and dust and smoke are matter, fine motes and soft translucent wisps lit by the
room. Drawn only, never part of the world: each peer emits its own from what it sees.

| File                        | Holds                                                                             | Godot |
| --------------------------- | --------------------------------------------------------------------------------- | ----- |
| `game/render/Luminous.cs`   | `Glimmer` (streak, mote, wisp, chip), `Luminous` (a look and its motion) and its presets, `Glim`, `Glims` and `Glims.Shared`, `LuminousMath` (ramps, brightness, noise, curl, a streak's points) | no |
| `game/render/Flares.cs`     | `Glint`, `Shock` (round or flat along a floor), `Flash`, `Trail`, `Bloom` and `Flares.Shared`: the key moments' glints, shock lines and lights; lines of light and soft glows stamped for one frame | no |
| `game/render/Bolts.cs`      | `Bolt`, `Bolts` and `Bolts.Shared`: electric arcs, folded by midpoint displacement, held two 60 Hz frames (a fine one one), on ends that move | no |
| `game/render/SoftDots.cs`   | the soft dot and puff pictures motes and wisps are drawn with | yes | no |
| `game/render/Strokes.cs`    | every stroke of light in one multimesh, soft-edged at a screen pixel                | yes   |
| `game/render/Shimmer.cs`    | heat shimmer over a box of the world, a screen-texture shader                      | yes   |
| `game/Content/Fx.cs`        | the knobs: length, count, turbulence, glow, glint hold, shimmer; the lights' cap    | no    |
| `EffectsView.cs`            | the layer: steps all of it each frame and draws it; a drill's item card has one of its own particles, light only | yes   |
| `game/Content/HitLooks.cs`  | a hit's look by what it strikes (0094): `Impact`, `Throw`, `HitLook`, the tables by material and by growth beside the sound's, each object's look | no |
| `game/render/HitBurst.cs`   | throws a `HitLook`, or a share of it, out of a struck face: `HitBurst.Shared`, or a card's own | no |
| `Impacts.cs`                | what a hit at a cell strikes, where on it (`HitPoint`: the face toward the hero), the floor its chips fall to, a tool's share | no |

- **Emitted by (luminous):** the furnace (`FurnaceView`) — embers, ash, a cracking coal's sparks and glint,
  for a furnace and a burner generator alike; the building (`BuildGlow`) — spark bursts off a screwdriver,
  glints, smoke off its molten spot, motes off a plan's cut and round a finished plan; the debris
  (`DebrisView`) — a wreck's hit (`HitBurst`, below), a break's sparks, dust, cloud, glint, ring and flash, and a
  broken piece's embers (`BreakView`); the crew (`HeroView.Struck`) — every tool's hit on the ground or a growth.
- **Hits look as what they strike (0094)**, whatever the tool, as they sound (0093), thrown where the object is struck,
  never at the tool: a wall's open face, of its four sides, turned toward the hero who struck (else its closed face turned to
  it), at the point of it nearest the hero, kept 0.15 of the face off its corners; a growth's cell's middle drawn a
  quarter cell toward the hero; a wreck where a running drill's bit is on its box, else its side facing the nearest
  hero. Chips fall to the floor under the face, within six rows. One table by the natural wall's material and one by the
  growth's kind (`HitLooks.Ground`, `HitLooks.Growths`), beside the sound's; what lies in the rock is struck as its stone.
  A whole hit is the axe's; a faster tool's throws its pace over the axe's (`Impacts.ShareOf`: the drill a fifth, the arc
  drill two fifths), so a second of drilling throws what a second of chopping does: each count by chance, rounded so
  it adds up, the glint and shock ring by the same chance, the light to the share's root. The hit that breaks or fells an
  object throws the whole, once an object (a crystal's only hit). None is drawn behind the walls: thrown out of a face,
  a chip lands on the grass and shows.

  | Struck                                                    | Its hit (a whole one, the axe's; × `Fx.Count`)                     |
  | --------------------------------------------------------- | ------------------------------------------------------------------ |
  | a wreck, out in space                                     | 16 sparks, 14 motes of metal dust, 3 dark chips; glint, ring, warm flash |
  | alloy, the ruins' metal pieces                            | 12 sparks arcing down, 6 dust, 3 alloy chips; glint, ring, warm flash |
  | stone, a rock, a stalagmite or stalactite                 | 4 short fine sparks, 5 stone chips, 5 dust; a faint flash           |
  | soil                                                      | 6 crumbs, 7 dust; no sparks                                         |
  | the homes' plaster                                        | 4 chips, 9 dust; no sparks                                          |
  | wood: ruin furniture, roots                               | 6 splinters, 2 wood chips, 3 pale dust                              |
  | a tree                                                    | 6 splinters, a wood chip, 3 leaves                                  |
  | a bush, grass, bell flowers, herbs                        | 4 leaves, 2 green motes                                             |
  | a crystal, a geode, an ice vein                           | 5 cold glitter streaks, 3 glassy shards; a cold glint and flash    |
  | a glowcap or hanging cap                                  | 7 spores drifting up                                                |
  | cloth: banners, rugs, curtains; a cobweb, a cave's threads | 6 fibres, 2 pale dust                                              |
  | glass: lamps, lanterns, windows, jars                     | 4 glassy shards, 4 glitter streaks; a cold glint                   |
  | crockery: dishes, pots, a basin, potted plants            | 4 glazed chips, 3 pale dust                                         |
- **Emitted by (slice 2):** the hero's figure (`crew/Figure.cs`) — a running boot's motes and veil, a landing's grains,
  motes, veils and flat ring; the jetpack (`JetPlume`) — its core, streaks, motes, pool and shimmer; the harpoon
  (`RodLight`) — the shot's glint and gas, the rod's trail and wake, the bite, the bead, the zip home; a drill
  (`render/DrillLight`) — an arc-fitted drill's fine arcs, crawling and leaping sparks and haze, only the tool's own; the air (`AirView`, `VentLight`) —
  breaches' jets, condensation, shimmer and frost, wind lines, the red beat, fresh air from filling vents; a pod's
  hatch (`PodView`) — vapour; drones (`DroneLight`) — rotor discs and glints, dust and veils spread along a floor close under them, the lamp's trail,
  shimmer, a pad's puff; doors (`DoorLight`) — the lit edge, the slot's motes, the seal's ring, glint, dust and hiss;
  lamps short of power (`LampSparks`) — glints, sparks, arcs, the ember; an electric smelter (`SmelterHeat`) — its
  element's pulse, its fine crackle, rising flecks, shimmer and the contacts' arc; a pick-up off the floor or out of a catch (`PickUpFlights`) — the icon's ribbon, glint, ring and flash.
- **Motion:** each look's gravity (aboard; none in space, chosen by the emitter), drag, a turn of its way by
  smooth value noise (`Spin`, keeping its speed) and a push along the noise's curl (`Turbulence`), a
  floor it may bounce off; a streak samples its path every 60th of a second whatever the frame rate, its
  last 24 points, and is drawn back along them as far as its speed times its `Stretch` says.
- **Dust along the floor** (`Luminous.Low`: a step's and a landing's motes, grains and veils, a dig's break dust and cloud, 0077) is
  drawn apart, over the back walls and behind the walls (`Layers.LowDust`), giving no light of its own, so a wall hides it.
- **Drawn** over everything in the world, plans included (`Layers.Effects`), under an overlay, a pixel of
  the screen a unit so strokes are as fine as the screen allows over the world's pixels: matter lit by the
  room (`Lit`) — motes, wisps swelling and stretched along their way, chips tumbling; then light added —
  fine dots, halos round streak heads and glints, each matter mote again at 0.4 so dust catches a little
  light of its own; then every stroke at once in one multimesh (`Strokes`): streaks from a whitened head
  through their colour to nothing at the tail, shock rings, glints' spikes (a long one across, a shorter one
  up and down, faint diagonals, each clear at its ends), all soft over the last screen pixel either side and
  at least 1.1 px across; trails and arcs in the same multimesh. A glint is whole on its first 60 Hz frame and gone after
  `Fx.GlintHold`, drawn at least once at any frame rate. A grain of matter (a streak that is `Matter`) is lit at its head as
  a mote, its smear along its way light at 0.4. Trails and blooms are stamped for one frame by what follows a moving
  thing (`Flares.Trail`, `Bloom`) and taken for drawing once a frame (`Flares.TakeFrame`): the layer runs after every
  feature (`ProcessPriority` 100), so what they stamp is drawn the frame they stamp it. Arcs (`Bolts`): a rim in their
  tint (`BoltTint`: cyan, or the arc module's violet, 0086) then a white core, a glow and a light in the tint, their shape kept along and across the line between their ends so a
  held shape stays on ends that move. A fine arc (slice 3, an arc-fitted drill's coil) is a hair of white-blue light a streak's
  width in a blue haze twice as wide, folded twice at least and jagged, rolled anew every frame, with no glow and no light.
- **Light (0019):** flashes first, then arcs, then luminous particles that give light, as spots of the light feature (`LightView`), at most `Fx.MostLights` (48) together.
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
  switch it off (`Shimmer.Enabled`, kept in `user://settings.cfg`, on by default). A shimmer whose box is off the
  screen is hidden, so a screen with none on it takes no copy. Slice 2 caps them: six breaches, three drones.
- **Cost** (`-- --measure`, RTX 4060 Ti, 1920×1080): the testbed's furnace room 1.52 → 1.62 ms a frame,
  164 draw calls against 159; `-- --fx-bench` (eight fires with their shimmer, four plans worked, a wreck
  broken every second, and a plan welded hot under the aim's outline: some 600 particles) 2.4 ms a frame, 0.7 ms
  of it this layer's CPU, 220 draw calls.
  Godot draws each polyline as a draw call with a fresh buffer of its own; drawn so, the bench took 5.7 ms.
  Slice 2 (the same machine, `PerfReadout`'s fps and frame, render CPU and GPU): `-- --fx-bench`, now with two jetpacks,
  a breach of three openings, three drones, a smelter, an arc drill biting and a lamp at half power, some 770
  particles: 603 fps (1.66 ms; 0.25 CPU, 0.19 GPU) before, 549 (1.82 ms; 0.25, 0.19) after, the layer 0.63 → 0.78 ms;
  `--testbed` 620 fps (1.61 ms; 0.24, 0.15) → 620 (1.61; 0.24, 0.15), 167 → 171 draw calls; the furnace room 605 → 625;
  `--testbed --fx-bench` 412 → 383 fps (2.61 ms) with all of it at once; `--jetpack` 593, `--asteroid --arc-drill` 927.
  Slice 3 (three runs each, 1920×1080): `-- --fx-bench`, now with a catch of five flying home every 1.5 s, 518 → 527 fps
  (1.93 ms; 0.26 CPU, 0.19 GPU), `--asteroid --arc-drill` 913 → 915 (1.09 ms), within noise.
- At most 1600 luminous particles and 48 arcs at once: a big breach thins out rather than costing frames.

The luminous arithmetic, trails, blooms, floor rings and arcs are tested in `LuminousTests`; the old squares and their
`ParticlesTests` are gone, every effect now luminous.

## Deliberately not handled

- Particles fly through walls: nothing but a floor an emitter names stops them.
- A peer is not told which tool hit a wreck: a hit under a running drill's bit is taken for that drill's, so a friend's
  axe striking a wreck this hero drills looks like a drill's hit (0094).
- A hit on a tree's crown lands in the struck cell, which may be the air between two branches; a plant's picture is the
  client's, its cells the sim's, and nothing finds the wood in the picture (0094).
- A chip keeps the one floor it was thrown over: one that flies past the edge of a step rests on the air at the step's
  height for its life. Chips are thrown slow and close for that, within about a cell (0094).
- A door's hiss of vapour comes as the panel meets the seal, the shut sound at the start of the slide.
- A breach let out through a missing back wall (no way out) jets every way and neither shimmers nor frosts.

## Differences from the TypeScript

- A glowing particle lights by a spot of the light feature's lightmap (0029), where the TypeScript game added
  it to the cell light it worked out every frame.

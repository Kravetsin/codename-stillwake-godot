# effects (client): light and matter that fly

## Files

| File | Purpose |
| --- | --- |
| `EffectsView.cs` | One stage's view: steps and draws its particles, glints, shock lines, arcs and shimmer. |
| `StagesView.cs` | The world's stages drawn: a view a stage at its layer's z, kept last among the world's views. |
| `logic/Impacts.cs` | What a hit at a cell strikes and where on it (0094): the face toward the hero, its floor, a tool's share. |

Particles without a word, in the luminous style (0075): what glows is light added over the world — streaks white-hot
at the head, stretched along their curling path, fading as they cool, lighting the room a little; a one-frame glint
and a faint shock line on key moments; heat shimmer behind hot things — and dust and smoke are matter, motes and
wisps lit by the room. Drawn only, never part of the world: each peer emits its own from what it sees.

Beside this folder: `render/logic/Luminous.cs` (`Glimmer`, `Luminous` and its presets, `Glims`, `LuminousMath`), `render/logic/Stages.cs` (a stage a layer),
`render/logic/Flares.cs` (`Glint`, `Shock`, `Flash`, `Trail`, `Bloom`), `render/logic/Bolts.cs` (arcs), `render/SoftDots.cs`,
`render/Strokes.cs` (every stroke in one multimesh), `render/Shimmer.cs`, `render/logic/HitBurst.cs` (throws a `HitLook`
or a share of it); `Content/logic/Fx.cs` (the knobs, the lights' cap) and `Content/logic/HitLooks.cs` (a hit's look by what it strikes).

## Rules

- **Emitters:** the furnace and burner (`FurnaceView`), building (`BuildGlow`), debris (`DebrisView`, `BreakView`),
  the crew's hits (`HeroView.Struck`), the hero's figure (steps, landings), the jetpack (`JetPlume`), the harpoon
  (`RodLight`), an arc-fitted drill (`render/DrillLight`), the air (`AirView`, `VentLight`), a pod's hatch (`PodView`),
  drones (`DroneLight`), doors (`DoorLight`), lamps short of power (`LampSparks`), the electric smelter (`SmelterHeat`), meteors (`MeteorView`)
  and pick-ups (`PickUpFlights`).
- **Hits look as what they strike** (0094), whatever the tool, as they sound (0093), thrown where the object is struck:
  a wall's open face toward the striking hero (else its closed face toward it), at the point nearest the hero, 0.15 of
  the face off its corners; a growth's cell's middle a quarter cell toward the hero; a wreck at a running drill's bit,
  else its side facing the nearest hero. Chips fall to the floor under the face, within `HitLooks.FloorRows`. A hit's look is never drawn behind the walls.
- **Looks by object** (0094): one table by the natural wall's material, one by the growth's kind (`HitLooks.Ground`,
  `HitLooks.Growths`), beside the sound's; what lies in the rock is struck as its stone; the ship's hull, never broken, as alloy, so a
  shot is seen to land. Metal throws sparks, glint and warm flash; stone fine sparks and chips; soil and plaster crumbs
  and dust; wood splinters; plants leaves; crystal and glass cold glitter and shards; caps spores; cloth fibres; crockery
  glazed chips.
- **A whole hit is the axe's** (0094): a faster tool throws its pace over the axe's (`Impacts.ShareOf`), so a second of
  drilling throws what a second of chopping does: each count by chance, rounded so it adds up, the glint and ring by
  the same chance, the light to the share's root. The hit that breaks or fells an object throws the whole, once.
- **Motion** (0075): each look's gravity (none in space, chosen by the emitter), drag, a turn by smooth value noise
  (`Spin`) and a push along its curl (`Turbulence`), a floor it may bounce off; a streak samples its path every 60th
  of a second whatever the frame rate, its last 24 points, drawn back along them by speed times `Stretch`.
- **Dust along the floor** (`Luminous.Low`, 0077) is drawn behind the walls (`Layers.LowDust`), giving no light, so a
  wall hides it.
- **Drawn on its maker's layer** ([effects-on-their-layer](../../../docs/decisions/2026-10-27-effects-on-their-layer.md)):
  each emitter throws on its layer's stage (`Stages.Of(Layers.Fire)`), drawn there by its own view, a screen pixel a unit
  (0075): matter lit (`Lit`), then light added (dots, halos, each mote again at 0.4), then every stroke in one multimesh
  (`Strokes`) — streaks, rings, glints' spikes, trails and arcs — soft over the last screen pixel and at least 1.1 px
  across. Over the heroes, at `Layers.Effects`: hits and what they break or kill, blows on a hero, venting air, pick-ups.
  What a hero holds (the drill's coil, the harpoon's muzzle) draws on its figure's held stage, over the torso and the
  tool, under the front arm. A particle keeps its stage for life.
- **A glint** is whole on its first 60 Hz frame and gone after `Fx.GlintHold`, drawn at least once at any frame rate.
  A `Matter` streak is lit at its head as a mote, its smear light at 0.4.
- **Trails and blooms** are stamped for one frame (`Flares.Trail`, `Bloom`) and taken once a frame
  (`Flares.TakeFrame`): the layer runs after every feature (`ProcessPriority` 100), so they draw the frame stamped.
- **Arcs** (`Bolts`, [arcs-are-hairlines](../../../docs/decisions/2026-10-27-arcs-are-hairlines.md)): in pixels of the screen
  whatever the zoom, a hair of white (`Bolts.Core`, 1.2 px) in a thin haze of their tint (`Bolts.Haze`, 4 px; `BoltTint`:
  cyan, the arc module's violet, 0086, a tick's red), their last `Bolts.Ghosts` shapes fading behind in a deeper colour, and
  a light; no glow sprites along them. Their shape is kept along and across the line between their ends. A fine arc (an
  arc-fitted drill's coil) is the finest hair in a blue haze, rolled anew every frame, with no light (no ADR).
- **Light** (0019, 0029): every stage's flashes, then arcs, then luminous particles that give light, as spots of `LightView`, at most
  `Fx.MostLights` together.
- **Heat shimmer** (0075): a `Shimmer` per hot thing samples the screen under its box again, rows nudged sideways by
  waves and rising noise, faded to its edges; a texel with nothing drawn adds nothing, so it never darkens. Every
  shimmer draws at `Layers.Shimmer` from one copy of the screen taken there (`Shimmer.ScreenCopy`, `Main`'s first child),
  so outlines, plans, marks, the tablet and words stay crisp. Off screen, hidden: no shimmer, no copy. The Esc menu
  switches it off (`Shimmer.Enabled`, `user://settings.cfg`, on by default). Breaches shimmer at most
  `VentLight.Shimmers`, drones `DroneLight.Shimmers` (no ADR). `-- --testbed --near=furnace --shimmer-demo` records the hard cases.
- **Caps** (no ADR): at most `Glims.Limit` luminous particles and `Bolts.Limit` arcs at once: a big breach thins out
  rather than costing frames. `-- --fx-bench` and `--measure` measure it.

Tested in `LuminousTests`: the luminous arithmetic, trails, blooms, floor rings and arcs.

## Deliberately not handled

- Particles fly through walls: only a floor an emitter names stops them.
- A peer is not told which tool hit a wreck: a hit under a running drill's bit is taken for that drill's (0094).
- A hit on a tree's crown lands in the struck cell, maybe air between branches: nothing finds the wood in the picture (0094).
- A chip keeps the floor it was thrown over; one flying past a step's edge rests on air at its height (0094).
- A door's hiss comes as the panel meets the seal, the shut sound at the start of the slide.
- A breach out through a missing back wall jets every way and neither shimmers nor frosts.

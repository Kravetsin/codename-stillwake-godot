# effects (client): dust, sparks, embers, air

Particles without a word, ported from `apps/game/src/features/effects/` and `render/particles.ts`
(G-08). Drawn only, never part of the world: each peer emits its own from what it sees.

| File                          | Holds                                                                             | Godot |
| ----------------------------- | --------------------------------------------------------------------------------- | ----- |
| `game/render/Particles.cs`    | `Effect` and its presets (`EFFECTS`), `Particle`, `Particles` and `Particles.Shared` | no |
| `EffectsView.cs`              | the layer: steps every particle each frame and draws it, a halo added under each that glows | yes |

- **Emitted by:** the hero's figure (`crew/Figure.cs`) — dust from a boot each half stride and from a
  landing to either side, more the harder it fell; the building (`BuildingView`) — 0.3 sparks a tick of
  work on a plan from its middle, falling aboard, drifting in space; the furnace (`FurnaceView`) — 7
  embers a second from a burning furnace; the air (`AirView`) — air streaming out of a venting room's
  openings into the space beside each (`atmosphere/Streams.cs`).
- **Drawn** unlit over everything in the world, plans included (z 21): a square of the effect's colour
  fading over its life (an ember flickering), a soft additive halo under those that glow.
- **Light (0019):** sparks and embers light what is around them: `LightView` puts a light under each,
  `Light.Particle` of its glow's colour as it fades, shadowed as a lamp's, at most 48 at once, 8 cells
  far.
- At most 2000 fly at once; a big breach thins out rather than costing frames.

`particles.test.ts` is ported (`ParticlesTests`).

## Differences from the TypeScript

- A glowing particle lights by a Godot light of its own, capped at 48, where the TypeScript game added
  it to the cell light it worked out every frame.

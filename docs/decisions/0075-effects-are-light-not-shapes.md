# 0075 — Effects are light, not shapes

- Status: accepted
- Date: 2026-10-19

## Context

The effects were squares of a world pixel or two (`Particles`, G-08): sparks, embers and dust alike, with a soft halo
under those that glow. Next to the lit pixel art they read as confetti, and a hit or a break had no moment to it. Two
review pages drew every effect in the game four ways (today, a hybrid, two luminous variants), each with sliders. The
user picked the luminous variant B of every effect — "thin small particles with a very cool effect" — at the sliders'
defaults. This decision is the style and the toolkit; slice 1 applies it to the furnace's fire (and the burner
generator's, which shares it), a screwdriver's sparks on a plan, an axe's hit and a wreck breaking.

## Decision

- **What glows is light.** Added over the world, never outlined, drawn at the screen's resolution (a pixel of the
  screen a unit) over the pixel world: thin streaks at least 1.1 screen px across, soft over the last pixel either
  side, white-hot at the head, stretched by speed along their own recent path, which curls a little, their colour
  running down a ramp (white, amber, red) as their brightness fades, each lighting the room a little.
- **What is matter is lit.** Dust and smoke are fine motes and soft translucent wisps drawn with the lit material, so
  the room's light colours them; each mote is drawn again as light at 0.4, or the dim room swallows it.
- **Key moments get a glint and a shock line.** A one-frame cross flare (a long thin spike across, a shorter one up and
  down, faint diagonals, a bloom) and a faint ring spreading fast and fading; a flash of light round them dying away.
- **Hot things shimmer.** The screen behind a fire's mouth or a weld is sampled again with its rows nudged sideways by a
  rising wobble, faded out at the box's edges.
- **Weight is drawn, never simulated.** An axe that bites is drawn stopped on contact for three 60 Hz frames, the rest
  of its swing hurried to end on time; the view nudges a pixel. The swing's clock, its hits and the sim are untouched.

## The toolkit

| Piece | What it is |
| ----- | ---------- |
| `Luminous` (`render/Luminous.cs`) | a look and its motion: kind (`Glimmer`: streak, mote, wisp, chip), colour ramp, life, brightness and fade power, gravity, drag, spin (a turn of its way by value noise, keeping its speed), turbulence (a push along the noise's curl), a streak's width and stretch, a halo, whether it lights, whether it is matter, a floor bounce; the presets of slice 1 |
| `Glims.Shared` | the particles: `Add`, `Spray`, `Step`; a streak samples its path every 60th of a second whatever the frame rate |
| `LuminousMath` | ramps, brightness, value noise and curl, a streak's points along its path: plain C#, tested |
| `Flares.Shared` (`render/Flares.cs`) | `Glint`, `Shock`, `Flash` (a light dying away by a share a 60 Hz frame) and `Hold` (a light kept while it is set each frame) |
| `Strokes` (`render/Strokes.cs`) | every stroke of light in one multimesh, one draw call: a quad a segment, its colour running end to end, its edges faded over a screen pixel with `fwidth` |
| `Shimmer` (`render/Shimmer.cs`) | heat shimmer over a box, a canvas shader reading the screen texture |
| `Fx` (`Content/Fx.cs`) | the page's sliders as knobs, all 1: length, count, turbulence, glow, glint hold (frames), shimmer; the lights' cap |

`EffectsView` steps and draws all of it; `LightView` lights flashes first, then glowing particles, at most
`Fx.MostLights` (48) together. The old `Particles` stays for dust, air and puffs until slice 2.

## Why these choices

- **One multimesh for strokes.** Godot draws each `DrawPolyline` as a draw call with a fresh buffer; a busy scene of
  some 570 particles cost 5.7 ms a frame that way, and one indexed triangle array made afresh each frame measured
  13 ms of render CPU even with a few dozen. A multimesh whose buffer is refilled is what the lightmap already does: the same scene costs 2.2 ms, the
  layer 0.63 ms of it, and a furnace room 0.1 ms more than the squares did.
- **Paths sampled at 60 Hz**, not every frame: the page's look holds at 144 Hz and at 20 (recordings).
- **Spots for light, capped.** A spark's light is a spot of the lightmap (0029); a break throws 26 sparks at once,
  whose spots summed would wash a wreck white, so their light is `Fx.ParticleLight` (0.3) of an old ember's and the
  flashes are weaker than the page drew (spots add over the room's light where the page's multiplied).
- **Every shimmer is drawn at one layer, after the world and before anything over it** (the aim's outline, plans,
  marks), from one copy of the whole screen taken there, and shows only what was drawn (its alpha): it bends the
  world and never an overlay, and never darkens. Godot's own copy (taken at the first reader) and a copy per box
  were tried first: the aim's outline is a canvas group sharing the same buffer, and they showed a black disc over a
  weld and a green ghost of a furnace aimed at. The Esc menu's settings switch it off (on by default).

## Deliberately not done

- Slice 2: the jetpack's plume, boots' dust, the harpoon's puff and line, venting air, the drone, a door's slide, a
  lamp's glow, the pick-up popup and the drill's bolts keep their look till then.
- Particles pass through walls; only a floor the emitter names stops sparks.
- Others' heroes' swings are not held on contact: a peer is not told which swing bit.
- A plan's molten spot heats per plan, so a wall dragged cell by cell is worked cool, a cell at a time.

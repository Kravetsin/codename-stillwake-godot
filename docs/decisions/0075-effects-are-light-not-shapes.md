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
`Fx.MostLights` (48) together. The old `Particles` stayed for dust, air and puffs until slice 2, which removed it.

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

## Slice 2

The rest of the game's effects in the same style, variant B of the second review page, kept restrained after the
user's note on slice 1's fire ("it was fountaining"):

- **The jetpack** is a hot thruster (`JetPlume`): a short tapered core of light, white to blue, flickering in length and
  drawn behind the heroes; hot streaks and blue motes; a blue pool of light that moves with the hero; shimmer in the plume.
- **Boots' dust** is matter: fine motes and a veil a running step; on landing, grains skittering out, motes, veils and
  one faint flat ring racing along the floor, its only light.
- **The harpoon** (`RodLight`): a muzzle glint and a jet of cold gas; a thin trail behind the rod and motes in its wake; a
  glint, sparks and a ring where it bites metal; a bead of light down the line as it goes taut; a reel over, a streak
  zipping home to the hand.
- **Air**: venting is a jet of streaks and curling condensation out of each breach (`VentLight`), shimmer and twinkling
  frost on the nearest six, motes swept toward it as wind lines, the room lit red on the beat by a light rather than
  painted over. Filling, the motes are fine soft specks and each vent breathes a faint veil now and then.
- **Drones** (`DroneLight`): a disc of light over each rotor with a glint running round its rim, downwash streaks stopped
  by the floor and the dust they creep, the lamp's trail, shimmer under the nearest three, a puff and ring at the pad.
- **Doors** slide eased; their leading edge lit while they move, the slot's motes on opening, a floor ring, a glint, dust
  and a hiss of vapour on shutting (`DoorLight`).
- **A lamp short of power** keeps its flicker's timing (`LampSparks`): a small warm glint as it catches, a few sparks more
  often than not, half the time an arc or two at its socket, an ember while dark. Nothing flashes the room white.
- **The pick-up** (`PickUpFlights`): the icon flies home to the chest on a ribbon of light and lands in a glint and a
  ring; the popup has a soft shadow and a band of light sweeps across it once.
- **The electric smelter** (`SmelterHeat`): its element breathing in its window, a faint shimmer over it, a rare blue arc
  at its contacts.
- **The drill**: where its bit bites, the axe's hit in small and without end (`DrillLight`); the electric spear's bolts
  moved from the bit to the coil on the stock, and made an upgrade: the plain drill has none, the **arc drill** has
  them. The arc drill is a weapon of its own, 3 a hit every 4 ticks, 15 damage a second to the drill's 10 (half as much
  again, damage a second being all that sets weapons apart, 0054), drawing as the drill does. It is never made: it is
  tech to be found in planet ruins (`Weapons.Found`), and till then only the testbed's supply crate and
  `--asteroid --arc-drill` hold one. Its art is the drill's with the coil tinted, a placeholder.

New pieces of the toolkit, each with several callers: `Bolts` (the arc drill's coil, a lamp short of power, the
smelter's contacts) — arcs by midpoint displacement, a shape held two 60 Hz frames, kept along and across the line
between its ends so a held arc stays on ends that move, lit in cyan by the light feature less the shorter it is;
`Flares.Trail` and `Flares.Bloom` (the rod, the ribbon, the lamp's trail, a door's edge, rotor discs, a smelter's breath,
an ember) — lines and glows stamped for one frame and drawn in the same multimesh and batch, the layer now running
after every feature; a squashed `Shock` for rings along a floor; `SoftDots`. Shimmers off the screen are hidden, so
they take no screen copy. Measured on the same machine at 1920×1080, the testbed holds 620 fps (167 → 171 draw
calls) and a bench of all of it at once, some 830 particles and eleven shimmers, 383.

## Deliberately not done

- The arc drill is not made anywhere and no planet ruin exists yet; its art is a placeholder.
- Another hero's drill hit on a wreck still shows as an axe's at the wreck: a peer is not told which tool hit.
- Particles pass through walls; only a floor the emitter names stops sparks.
- Others' heroes' swings are not held on contact: a peer is not told which swing bit.
- A plan's molten spot heats per plan, so a wall dragged cell by cell is worked cool, a cell at a time.

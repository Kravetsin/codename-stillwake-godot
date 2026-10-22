# atmosphere (client): the ship's air

Ported from `apps/game/src/features/atmosphere/` (G-08). Reads `sim/Atmosphere` (`RoomsAir`, `AiredCells`,
`OxygenIn`), `sim/Rooms` (`RoomOfCells`, `RoomAt`, `RoomById`), `sim/Grid` (`Built`, `MachinesOf`) and `sim/Crew` (`Heroes`).

| File         | Holds                                                                                   | Godot |
| ------------ | --------------------------------------------------------------------------------------- | ----- |
| `Explain.cs` | what the overlay says of a room, spans, and the rooms draining (`explain.ts`, `draining.ts`) | no    |
| `Streams.cs` | `AirStream`: where air streams out of a venting room, which way, how thick (`streams.ts`) | no    |
| `AirField.cs` | the overlay's cells eased toward the sim's, a venting room's flow toward its nearest opening, over what is built | no    |
| `AirMotes.cs` | motes gliding in all air, swept out of a venting room; a few in the lifepod's reach | no    |
| `AirLook.cs` | the shader, with O: the rooms' air dithered, grey dots where there is none, the pod's ragged aura | yes   |
| `AirView.cs` | venting rooms lit red on the beat and their air streaming out, the motes, fresh air from filling vents; with O the air overlay | yes   |
| `VentLight.cs` | the breaches in light (0075): jets, condensation, shimmer, frost | yes |
| `TankLooks.cs` | each tank's sight glass filled to its oxygen, and its light | yes   |

- The overlay draws each room's air in an ordered dither, a pixel of the world at a time, drifting like a
  gas and lit at its front; a cell eases in and out over about 220 ms (`AirField.EaseMs`) and cells blend
  into each other within a room. A venting room's air goes red and runs toward its nearest opening; a
  room without air shows a sparse grid of grey dots. It lies over the back walls, behind all that stands
  in the room. The room under the pointer is explained beside it while air is the overlay. Surveyed every 250 ms.
  Drawn under every build tab too, the Air tab's pipes and the Power tab's cables over it, which then keep the
  pointer's tip (0072).
- The field and its texture cover only what is built and a cell of space round it (the grid's
  `Built`), growing with it: past it nothing is in a room, and the shader is told where the texture
  lies (`origin`), so it draws exactly what a texture of the whole world would. A survey looks again
  only at the rows whose rooms moved, the rooms whose openings changed, and the cells whose air came
  or went (the aired cells merged with the last survey's); a frame eases and writes only the cells
  still on their way. The pulse of draining rooms is laid out again only when the rooms or those
  draining change. So a big world costs what is built, and a still ship nothing.
- With O, the lifepod's air is a ragged aura about its reach (its 6×6 and `Crew.PodReach` round it, as
  `ByPod` counts), wavering and thinning at its edge rather than marking it exactly, with a few motes.
- Always, without O: motes gliding in all air, quieter than with O (0.1 a cell, a 16 px drawing's 0.4, 1500 at most); blown
  out of a filling room's vents to the edge of its air; swept out of a venting room. Each glides on a
  heading that turns a little at a time and is drawn to a fraction of a pixel, so none jitters.
- Air streams out of each opening of a room venting while it holds air or its vents still pour some
  in, toward the space beside it (every way through a missing back wall), luminous (`VentLight`, 0075): 24 fast pale
  streaks a second an opening and 160 a room at most (`Luminous.Jet`), with condensation curling and thinning into
  space (`Condensation`, four tenths as many); over the six nearest breaches the escaping air shimmers, and frost
  on their edges against the wall twinkles. Only openings on the screen throw anything. Motes swept fast toward a
  breach are drawn as thin wind lines, stretching as they speed up (`AirView.Winds`). The venting room is lit red
  on the 900 ms beat, a light held at its middle (`Flares.Hold`), its cells only faintly painted over.
- Air filling a room (0075): the motes are fine soft specks (`SoftDots.Dot`), and each filling vent on the screen
  breathes a faint veil of fresh air now and then, drifting up and settling (`Luminous.FreshAir`, 1.6 a second).
- Each tank shows its oxygen up a sight glass in its picture, without a word: `art/tank_fill_{0..5}.png`
  drawn unlit over `machine_tank.png`, frame 1 to 5 by fifths of `Air.TankCapacity` rounded up
  (`shared/TankFill.cs`), so any oxygen shows the lowest step and only none shows the glass dark with
  a red lamp lit. Each frame lights the room a little (a `Glow`): pale blue from the glass, brighter the
  fuller, red from the lamp when empty. Read every 250 ms. Not animated: a tank changes slowly.
- The forecast under the pointer counts the heroes awake in the room as breathing its air (`crew`).
- `explain.test.ts`, `draining.test.ts` and `streams.test.ts` are ported.

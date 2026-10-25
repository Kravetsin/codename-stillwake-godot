# atmosphere (client): the ship's air

## Files

| File | Purpose |
| --- | --- |
| `logic/AirField.cs` | The air as the overlay draws it, cell by cell: how much of each cell's air shows, eased toward what the sim says so … |
| `AirLook.cs` | How the air is drawn over the rooms, behind what stands in them, a pixel of the world at a time, with the overlay … |
| `logic/AirMotes.cs` | The air made visible without the overlay: sparse motes wandering in every cell holding air, blown out of a filling … |
| `AirView.cs` | The ship's air. Without a word: every room losing oxygen into space lit red on the beat (each tank's sight glass is … |
| `logic/Explain.cs` | What the air overlay says of a room. |
| `logic/Streams.cs` | Where air streams out of a room: the middle of an opening, in cells, and the heading toward the space beside it, in … |
| `logic/Decompression.cs` | Rooms the sim turned to space at once, air and all (a launch platform opened, a floor torn open), drawn venting a while … |
| `TankLooks.cs` | What each oxygen tank holds, without a word: pale oxygen up its sight glass by fifths (`TankFill`), the glass dark … |
| `VentLight.cs` | Air escaping into space (0075), luminous: out of each opening a room vents through, a jet of fast pale streaks and … |
| `logic/Atmosphere.En.cs` | Its player text (`En`) |

Ported from `apps/game/src/features/atmosphere/` (G-08). Reads `sim/Atmosphere` (`RoomsAir`, `AiredCells`,
`OxygenIn`), `sim/Rooms` (`RoomOfCells`, `RoomAt`, `RoomById`), `sim/Grid` (`Built`, `MachinesWith`: tanks by `Gas`, vents by `Vents`, 0113) and `sim/Crew` (`Heroes`).

| File         | Holds                                                                                   | Godot |
| ------------ | --------------------------------------------------------------------------------------- | ----- |
| `logic/Explain.cs` | what the overlay says of a room, spans, and the rooms draining (`explain.ts`, `draining.ts`) | no    |
| `logic/Streams.cs` | `AirStream`: where air streams out of a venting room, which way, how thick (`streams.ts`) | no    |
| `logic/Decompression.cs` | a room turned to space at once drawn venting: its air let go over 1.5 to 6 s, farthest from the hole first | no    |
| `logic/AirField.cs` | the overlay's cells eased toward the sim's, a venting room's flow toward its nearest opening, over what is built | no    |
| `logic/AirMotes.cs` | motes gliding in all air, swept out of a venting room; a few in the lifepod's reach | no    |
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
  (`shared/logic/TankFill.cs`), so any oxygen shows the lowest step and only none shows the glass dark with
  a red lamp lit. Each frame lights the room a little (a `Glow`): pale blue from the glass, brighter the
  fuller, red from the lamp when empty. Read every 250 ms. Not animated: a tank changes slowly.
- The forecast under the pointer counts the heroes awake in the room as breathing its air (`crew`).
- `explain.test.ts`, `draining.test.ts` and `streams.test.ts` are ported.
- A room the sim turns to space at once, air and all (a launch platform opened, a floor torn open), vents as through a
  door (`Decompression`, no ADR): the sim drops its air in a tick, so the client keeps drawing it in the room it left,
  venting toward the breach (the space beside it that was not its own), and lets it go over `MsPerCell` a cell, 1.5 to
  6 s, the cells farthest from the hole first. Meanwhile the hole streams, the motes are swept, the room is lit red on
  the beat and with O its air is red. Drawn only: the sim's air is already gone. Shut again, the room stands airless and
  nothing more goes.

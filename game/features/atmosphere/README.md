# atmosphere (client): the ship's air

Ported from `apps/game/src/features/atmosphere/` (G-08). Reads `sim/Atmosphere` (`RoomsAir`, `AiredCells`,
`OxygenIn`), `sim/Rooms` (`RoomOfCells`, `RoomAt`, `RoomById`), `sim/Grid` (`Built`) and `sim/Crew` (`Heroes`).

| File         | Holds                                                                                   | Godot |
| ------------ | --------------------------------------------------------------------------------------- | ----- |
| `Explain.cs` | what the overlay says of a room, spans, and the rooms draining (`explain.ts`, `draining.ts`) | no    |
| `Streams.cs` | `AirStream`: where air streams out of a venting room, which way, how thick (`streams.ts`) | no    |
| `AirField.cs` | the overlay's cells eased toward the sim's, a venting room's flow toward its nearest opening, over what is built | no    |
| `AirMotes.cs` | motes gliding in all air, swept out of a venting room; a few in the lifepod's reach | no    |
| `AirLook.cs` | the shader, with O: the rooms' air dithered, grey dots where there is none, the pod's ragged aura | yes   |
| `AirView.cs` | tank gauges, venting rooms pulsing red and their air streaming out, the motes; with O the air overlay | yes   |

- The overlay draws each room's air in an ordered dither, a pixel of the world at a time, drifting like a
  gas and lit at its front; a cell eases in and out over about 220 ms (`AirField.EaseMs`) and cells blend
  into each other within a room. A venting room's air goes red and runs toward its nearest opening; a
  room without air shows a sparse grid of grey dots. It lies over the back walls, behind all that stands
  in the room. The room under the pointer is explained beside it. Surveyed every 250 ms.
- The field and its texture cover only what is built and a cell of space round it (the grid's
  `Built`), growing with it: past it nothing is in a room, and the shader is told where the texture
  lies (`origin`), so it draws exactly what a texture of the whole world would. A survey looks again
  only at the rows whose rooms moved, the rooms whose openings changed, and the cells whose air came
  or went (the aired cells merged with the last survey's); a frame eases and writes only the cells
  still on their way. The pulse of draining rooms is laid out again only when the rooms or those
  draining change. So a big world costs what is built, and a still ship nothing.
- With O, the lifepod's air is a ragged aura about its reach (its 3×3 and `Crew.PodReach` round it, as
  `ByPod` counts), wavering and thinning at its edge rather than marking it exactly, with a few motes.
- Always, without O: motes gliding in all air, quieter than with O (0.4 a cell, 1500 at most); blown
  out of a filling room's vents to the edge of its air; swept out of a venting room. Each glides on a
  heading that turns a little at a time and is drawn to a fraction of a pixel, so none jitters.
- Air streams out of each opening of a room venting while it holds air or its vents still pour some
  in, toward the space beside it (every way through a missing back wall), 24 specks a second an
  opening and 160 a room at most, as `Effect.Air` particles (features/effects).
- The forecast under the pointer counts the heroes awake in the room as breathing its air (`crew`).
- `explain.test.ts`, `draining.test.ts` and `streams.test.ts` are ported.

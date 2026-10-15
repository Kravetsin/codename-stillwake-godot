# atmosphere (client): the ship's air

Ported from `apps/game/src/features/atmosphere/` (G-08). Reads `sim/Atmosphere` (`RoomsAir`, `AiredAt`,
`OxygenIn`), `sim/Rooms` (`RoomOfCells`, `RoomAt`, `RoomById`) and `sim/Crew` (`Heroes`).

| File         | Holds                                                                                   | Godot |
| ------------ | --------------------------------------------------------------------------------------- | ----- |
| `Explain.cs` | what the overlay says of a room, spans, and the rooms draining (`explain.ts`, `draining.ts`) | no    |
| `Streams.cs` | `AirStream`: where air streams out of a venting room, which way, how thick (`streams.ts`) | no    |
| `AirField.cs` | the overlay's cells eased toward the sim's, a venting room's flow toward its nearest opening | no    |
| `AirMotes.cs` | motes in all air, swept out of a venting room; about the lifepod, and from its valve to a suit it fills | no    |
| `AirLook.cs` | the shader: the pod's reach always, with O the rooms' air dithered, grey dots where there is none | yes   |
| `AirView.cs` | tank gauges, venting rooms pulsing red and their air streaming out, the motes; with O the air overlay | yes   |

- The overlay draws each room's air in an ordered dither, a pixel of the world at a time, drifting like a
  gas and lit at its front; a cell eases in and out over about 220 ms (`AirField.EaseMs`) and cells blend
  into each other within a room. A venting room's air goes red and runs toward its nearest opening; a
  room without air shows a sparse grid of grey dots. It lies over the back walls, behind all that stands
  in the room. The room under the pointer is explained beside it. Surveyed every 250 ms.
- Always, without O: motes in all air, quieter than with O (0.4 a cell, 1500 at most); blown out of a
  filling room's vents to the edge of its air; swept out of a venting room. The lifepod's reach — its
  3×3 and `Crew.PodReach` round it, as `ByPod` counts — has a breathing edge and a faint fill, a few motes
  in it, and a stream from the valve on its tanks to each hero awake in it whose suit is not full.
- Air streams out of each opening of a room venting while it holds air or its vents still pour some
  in, toward the space beside it (every way through a missing back wall), 24 specks a second an
  opening and 160 a room at most, as `Effect.Air` particles (features/effects).
- The forecast under the pointer counts the heroes awake in the room as breathing its air (`crew`).
- `explain.test.ts`, `draining.test.ts` and `streams.test.ts` are ported.

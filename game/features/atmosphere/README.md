# atmosphere (client): the ship's air

Ported from `apps/game/src/features/atmosphere/` (G-08). Reads `sim/Atmosphere` (`RoomsAir`, `AiredAt`,
`OxygenIn`), `sim/Rooms` (`RoomOfCells`, `RoomAt`, `RoomById`) and `sim/Crew` (`Heroes`).

| File         | Holds                                                                                   | Godot |
| ------------ | --------------------------------------------------------------------------------------- | ----- |
| `Explain.cs` | what the overlay says of a room, spans, and the rooms draining (`explain.ts`, `draining.ts`) | no    |
| `Streams.cs` | `AirStream`: where air streams out of a venting room, which way, how thick (`streams.ts`) | no    |
| `AirView.cs` | tank gauges, venting rooms pulsing red and their air streaming out; with O the air overlay | yes   |

- The overlay colours each room cell: holding air, without it, or the whole room red while venting; the
  room under the pointer is explained beside it. Surveyed every 250 ms.
- Air streams out of each opening of a room venting while it holds air or its vents still pour some
  in, toward the space beside it (every way through a missing back wall), 24 specks a second an
  opening and 160 a room at most, as `Effect.Air` particles (features/effects).
- The forecast under the pointer counts the heroes awake in the room as breathing its air (`crew`).
- `explain.test.ts`, `draining.test.ts` and `streams.test.ts` are ported.

# atmosphere (client): the ship's air

Ported from `apps/game/src/features/atmosphere/` (G-08). Reads `sim/Atmosphere` (`RoomsAir`, `AiredAt`,
`OxygenIn`) and `sim/Rooms` (`RoomOfCells`, `RoomAt`, `RoomById`).

| File         | Holds                                                                                   | Godot |
| ------------ | --------------------------------------------------------------------------------------- | ----- |
| `Explain.cs` | what the overlay says of a room, spans, and the rooms draining (`explain.ts`, `draining.ts`) | no    |
| `AirView.cs` | tank gauges and venting rooms pulsing red, always; with O the air overlay and its words  | yes   |

- The overlay colours each room cell: holding air, without it, or the whole room red while venting; the
  room under the pointer is explained beside it. Surveyed every 250 ms.
- `explain.test.ts` and `draining.test.ts` are ported.

## Not ported yet

- Who breathes in the room: the forecast counts no heroes until the crew module lands.
- Air streaming out of openings (`streams.ts`): with the particle effects.

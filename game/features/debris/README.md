# debris (client)

The debris drifting past the ship and catching it (`apps/game/src/features/debris/`, the debris half
of `features/crew/aim.ts` and `grabbing` of `session/predicted.ts`, G-08). Drawn only: the sim
(`sim/Debris`) spawns, moves and hands out what a catch gives; the client sends `Grab` and nothing
else.

- **Reads:** `DebrisModule` — `Pieces`, `GrabProblem(peer, piece)`; `IRoomsQueries.RoomAt` — whether
  the hero is out in space; `Client.InFlight` — its own grabs; `Session.Progress`.
- **Drawn** behind the back walls (`ZIndex` −20): it never collides with the hull (0006). Each piece
  is its item's picture (`ItemIcons`), 0.8 of a cell, lit by the stars and the lamps, between ticks
  at `X − Speed × Progress` so it glides, tumbling a turn every 4 s each its own way, faded in over the
  first 8 cells of its path and out over the last (`Catching.FadeOf`, 0009). The stars behind it are
  `render/Stars.cs`.
- **Caught:** out in space, the piece nearest the pointer within `Debris.AimRadius` is aimed at
  (`Catching.AimAt`), unless E works something else under the pointer first — a door, a valve, a
  crate (`Main.HandsBusy`); within the hero's reach it is outlined, green, or red while the sim would
  refuse the grab. E or the left button sends `Grab` (`Catching.GrabOf`) unless the sim would refuse
  it.
- **Own grabs at once (0014):** a piece this peer has a grab in flight for is gone from its view and
  passed over by the aim, so E cannot send it twice (`Catching.Grabbing`).
- **Recording:** `-- --autopilot --debris` starts in the wreck with pieces already out
  (`DebrisDemo.Seed`, put in through the save) and a cargo crate drifting to where the wreck's
  autopilot leaves the hero outside the airlock; there the pointer goes to it and E catches it,
  planks and maybe a schematic landing in the hotbar. With `--host` the demo is hosted, so a guest
  sees the catch.

## Files

| File            | Holds                                                         | Godot |
| --------------- | ------------------------------------------------------------- | ----- |
| `Catching.cs`   | `FadeOf`, `Grabbing`, `AimAt`, `GrabOf`                       | no    |
| `DebrisView.cs` | the layer: pieces, the outline, E and the left button         | yes   |
| `DebrisDemo.cs` | the world and the script of `--debris`                        | yes   |

`Catching.cs` is compiled again into `game.tests/`, where `fade.test.ts`, the debris tests of
`aim.test.ts` and the debris test of `predicted.test.ts` are ported (`DebrisTests`).

## Deliberately not handled

- The prompt over a piece ("Catch it", `use.catch`) and the refusal said under the pointer: prompts
  are not ported yet (crew's README); the outline's colour says it without words.
- Other peers' grabs in flight: a piece another player is catching stays until the host's tick says
  so, as in TypeScript.

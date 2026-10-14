# debris (client)

The debris drifting past the ship (`apps/game/src/features/debris/`, G-08). Drawn only: the sim
(`sim/Debris`) spawns, moves and hands out what a catch gives. Catching is the hero's hands, aimed as
everything else E works is (`features/crew`, `Aiming.AimAt`), which send `Grab`.

- **Reads:** `DebrisModule.Pieces`; `Client.InFlight` — its own grabs (`Shared.Predicted.Grabbing`);
  `Ui.AimedPiece` — the piece the crew's E would catch; `Session.Progress`.
- **Drawn** behind the back walls (`ZIndex` −20): it never collides with the hull (0006). Each piece
  is its item's picture (`ItemIcons`), 0.8 of a cell, lit by the stars and the lamps, between ticks
  at `X − Speed × Progress` so it glides, tumbling a turn every 4 s each its own way, faded in over the
  first 8 cells of its path and out over the last (`Fading.FadeOf`, 0009). The stars behind it are
  `render/Stars.cs`.
- **Aimed:** the piece the crew sets in `Ui.AimedPiece` — out in space, within reach, the hand free —
  is outlined, green, or red while the sim would refuse the grab; the crew's prompt says "[E] Catch".
- **Own grabs at once (0014):** a piece this peer has a grab in flight for is gone from its view and
  passed over by the aim, so E cannot send it twice.
- **Recording:** `-- --autopilot --debris` starts in the wreck with pieces already out
  (`DebrisDemo.Seed`, put in through the save) and a cargo crate drifting to where the wreck's
  autopilot leaves the hero outside the airlock; there the pointer goes to it and E catches it,
  planks and maybe a schematic landing in the hotbar. With `--host` the demo is hosted, so a guest
  sees the catch.

## Files

| File            | Holds                                                         | Godot |
| --------------- | ------------------------------------------------------------- | ----- |
| `Fading.cs`     | `FadeOf`                                                      | no    |
| `DebrisView.cs` | the layer: pieces and the outline of the one aimed at         | yes   |
| `DebrisDemo.cs` | the world and the script of `--debris`                        | yes   |

`Fading.cs` is compiled again into `game.tests/`, where `fade.test.ts` and the debris test of
`predicted.test.ts` are ported (`DebrisTests`); the debris tests of `aim.test.ts` are in `AimTests`.

## Deliberately not handled

- Other peers' grabs in flight: a piece another player is catching stays until the host's tick says
  so, as in TypeScript.

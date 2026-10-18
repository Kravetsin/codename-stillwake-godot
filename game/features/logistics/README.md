# logistics (client): request slots, crate roles and drones on screen

Drones ([0062](../../../docs/decisions/0062-drones-carry-what-machines-request.md)), the client of `sim/Logistics`.
It reads `LogisticsModule` (`RoleOf`, `RequestsOf`, `RequestsMost`, `Drones`, `ZoneOf`, `InReach`), `IItemsQueries`
(`HeldFor`, `Crate`) and `IGridQueries` (`MachinesOf`, `MachineById`), and changes the sim only by sending
`SetRequests` and `SetRole`. Its controls live in other features' frames, wired by `Main`: the request row under a
fed station's slots and over a crate's grid (`InventoryPanel.Requests`), under a queued station's inputs
(`FabricatorPanel.Requests`), and the role toggle in a crate's header (`InventoryPanel.Roles`); the bay's four
slots are the items feature's. The mockup it follows: "Drone Logistics Windows".

## Files

| File               | Holds                                                                                         | Godot |
| ------------------ | --------------------------------------------------------------------------------------------- | ----- |
| `Requests.cs`      | what a click, the wheel and a right-click leave of a machine's requests; a slot's look; this peer's own `SetRequests` and `SetRole` in flight shown at once (0014); where a drone is drawn between ticks | no |
| `RequestRow.cs`    | a machine's request slots under a "Requests" heading, as many as `RequestsMost`, hidden at none   | yes   |
| `RoleToggle.cs`    | a crate's three role buttons: storage, provider (red), requester (sky)                           | yes   |
| `LogisticsView.cs` | in the world: drones in flight with their cargo, the crates' lamps and the bays' pads with their light, a bay's zone | yes |

`Requests.cs` is compiled again into `game.tests/` (`RequestsTests`). The testbed (`-- --testbed`) runs a full
bay of four through the closed door (`TestbedTests`); `--open=droneBay` shows the bay's frame and zone,
`--open=crate@0 --role=requester` makes the storage crate a requester of five circuits, and
`--card=droneBay --pick` holds a bay's blueprint over the ship.

## Guarantees

- **Request slots** (the mockup): a click with a stack on the cursor sets the slot's item at the stack's count, a slot
  past the last adding one at the end (requests are a list, not places); the wheel changes a count by one, Shift ten,
  never under one; a right-click clears. Each sends one `SetRequests` with the whole list. A slot shows the item's
  icon faint and its count: dim when the machine holds as many where a delivery goes (`HeldFor`), edged amber when
  short, a faint "+" when empty. The tooltip says the item ×N, then met, "drones bring it" or "nothing in reach has
  it" (`InReach`), and how to change it. The row hides while the machine may hold none: a crate that is no
  requester, a station without inputs. Read every 100 ms and at once after its own command.
- **Crate roles:** three buttons in a crate's frame header, the one it is pressed; a click sends `SetRole`. What
  each means is in the tooltip. In the world a provider has a red lamp on its corner post and a requester a sky
  one, each lighting round it a little (a `Glow`), drawn unlit; storage has none.
- **The bay:** its frame shows four slots, each a drone's faint silhouette while empty, its tooltip "put a drone
  here to fly". Its pads in the world light sky where drones are docked, each with a little light. Its zone, the
  sim's `ZoneOf`, is outlined dashed sky over a faint fill while its frame is open; with a bay's card in hand
  every bay's zone is outlined and the one the card would make under the pointer, clipped at the grid as the sim's.
- **Drones in flight:** each a small body drawn by code, its lens, two arms and rotors whose blades beat wide and
  narrow, out of step between drones; its cargo's icon hangs under it. Drawn where it is between ticks, carried on
  toward its next centre at the sim's speed and never past it (`Requests.Smoothed`), so it moves on every frame;
  lit by the room's lights. Read every frame from the live list; lamps and pads every 100 ms.

## Deliberately not handled

- No sound: drones, bays and role changes wait for the user's audition of candidates.
- Request slots are edged solid, not dashed as in the mockup: Godot's `StyleBoxFlat` draws no dashes.
- A docked drone is shown only by its pad and its slot. With demand that never lets up (the testbed's furnace and
  Former), a drone home is launched again within a few ticks, so they seldom show it; charge (slice 3) will keep
  drones docked a while.

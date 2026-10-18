# logistics (client): the drone badge and drones on screen

Drones ([0062](../../../docs/decisions/0062-drones-keep-machines-going.md)), the client of `sim/Logistics`.
It reads `LogisticsModule` (`Served`, `Servable`, `Reached`, `Drones`, `Low`, `ZoneOf`), `IItemsQueries` (`Crate`)
and `IGridQueries` (`MachinesOf`, `MachineById`), and changes the sim only by sending `SetServed`. Its badge lives in
other features' frames, wired by `Main`: in the header of every machine frame the items feature opens
(`InventoryPanel.Served`) and of a queued station's (`FabricatorPanel.Served`), where drones have work. The bay's four
slots, their charge bars and its frame's bolt are the items feature's. The mockup it follows: "Drones Keep It Going".

## Files

| File               | Holds                                                                                         | Godot |
| ------------------ | --------------------------------------------------------------------------------------------- | ----- |
| `Served.cs`        | the badge's look and tooltip keys; this peer's own `SetServed` in flight shown at once (0014); where a drone is drawn between ticks | no |
| `ServedBadge.cs`   | a machine's drone badge, at the right of its frame's header: shows and toggles whether drones serve it | yes |
| `LogisticsView.cs` | in the world: drones in flight with their cargo and a low one's amber light, the bays' pads with their light, a bay's zone | yes |

`Served.cs` is compiled again into `game.tests/` (`ServedTests`). The testbed (`-- --testbed`) runs two drones from a
powered bay in room one through the closed door, keeping the furnace, the Former and the grow capsule going from the
supply crate of twenty stacks of chunks by the pod, the farm's crate switched off, for ten minutes in air three
generators keep (`TestbedTests`); `--open=droneBay` shows the bay's frame and zone
(`--open-at 130` once a drone is home charging), `--open=crate@2` the farm crate's crossed badge, `--near=furnace`
starts the hero in the middle of the room, and `--card=droneBay --pick` holds a bay's blueprint over the ship.

## Guarantees

- **The badge** (the mockup): a drone's icon in a small box, 22 by 18, at the right of the header. Lit, sky behind
  it, while drones serve the machine and a bay reaches it; dim and crossed while switched off; grey where no bay
  reaches. Shown only where drones ever have work (`Servable`): crates, fed stations, a fire's machine, queued stations;
  not on a bay, the analyzer or the charging station. A click sends `SetServed` with the other state. The one tooltip
  says the state ("Drones keep this going", "Drones leave this alone", "No drone bay reaches this", and both when
  switched off out of reach) and what a click does. Read every 100 ms and at once after its own command, showing its
  own switch in flight before its tick.
- **A remembered input** (`IItemsQueries.KeptAt`, while drones serve the machine; `InventoryPanel.Kept`, set by
  `Main`): a small sky dot in its top-left corner, lit a little; empty, a faint ghost of the item's own picture. Its
  tooltip, empty, says "Drones keep this full: X", and "none in reach" in red when no crate or output a bay reaches holds
  it (`Supplied`, `InventoryPanel.Supplied`). The slot box draws both (`SlotBox.Kept`).
- **The bay:** its frame shows four slots, each a drone's faint silhouette while empty, its tooltip "put a drone here
  to fly"; a docked drone's slot draws its charge bar as a battery's does (green, amber under a quarter); the header's
  bolt shows what it draws, lit while it charges a drone (`Power.Charges`). Its pads in the world light sky where
  drones are docked, each with a little light. Its zone, the sim's `ZoneOf`, is outlined dashed sky over a faint fill
  while its frame is open; with a bay's card in hand every bay's zone is outlined and the one the card would make under
  the pointer, clipped at the grid as the sim's. A drone with no charge shows `drone_empty`, its lens dark.
- **Drones in flight:** each a small body drawn by code, its lens, two arms and rotors whose blades beat wide and
  narrow, out of step between drones; its cargo's icon hangs under it. Drawn where it is between ticks, carried on
  toward its next centre at the sim's speed and never past it (`Served.Smoothed`), so it moves on every frame; lit by
  the room's lights. One flying home low (`Low`) blinks a small amber light on its back, unlit so it reads in the dark,
  lighting round it a little. Read every frame from the live list; pads every 100 ms. No numbers and no seconds.

## Deliberately not handled

- No sound: drones, bays and the switch wait for the user's audition of candidates.
- The badge on a machine with nothing drones would do yet (a furnace with every slot empty) still reads lit: it says
  drones would serve it, not that they are.
- `Reached` floods every bay's zone each time the badge reads, ten times a second for the one frame open: cheap at a
  few bays.

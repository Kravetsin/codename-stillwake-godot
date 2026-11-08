# Salvage drones

## Files

| File | Purpose |
| --- | --- |
| `Salvage.cs` | Part of `LogisticsModule`: a salvage drone's launch, air kept, targets, catch, net, delivery and next trip. |

A salvage drone (`salvageDrone`) docks in a drone bay beside ordinary drones. On the air its bay keeps, topped up from a
canister in the bay's fuel slot when short, it flies out through doorways into space, nets up to `SalvageHolds` of the
pieces a net would catch and the stacks floating in the bay's zone, and brings them to the nearest crates with room, trip
after trip, until air or charge runs low. It serves no machine. Ordinary drones do not change and never leave the air.
Drones in general: [0062](../../../docs/decisions/0062-drones-keep-machines-going.md); salvage:
[salvage-drones](../../../docs/decisions/2026-11-03-salvage-drones.md),
[salvage-trips](../../../docs/decisions/2026-11-04-salvage-trips.md).

- **Depends on** what Logistics does, and `Debris` (`Pieces`, `PieceById`; `Catch`), `Rooms` (`RoomAt`, `Afloat`), `Items`
  (`Floor`, `Lift`); `Content.Logistics` (`TripAir`, `SalvageHolds`, `SalvageDrone`), `Content.Debris.NetCatches`.
- **State** on `Drone`, each written only when set, so an ordinary drone saves as before: `Air` (ticks left), `Piece`
  (the target piece's id), `Floor` (the target floor stack's id), `net` (what it caught, in catch order: a piece's kind
  and id, or a lifted stack's item and 0; `Caught` reads it), `load` (the stacks it carries; `Loaded` reads it). The
  crate it delivers to is `Target`. Claims are read off `Drones`, never stored. On the slice, `Air`: the air each bay
  keeps (`BayAir`), sorted by bay id, no entry at nought.

## Rules

- **Vacuum flight:** a salvage drone's floods and paths pass any cell that is not solid, and doorways shut or open (the
  door stays shut). Ordinary work and the `Reached`/`Supplied` queries use the air rule; the mode is a parameter, never a field.
- **Launch:** at a bay's look, when no ordinary drone launched: the first docked salvage drone charged to `LaunchAt`, a
  served crate with an empty slot in the vacuum flood, and a target whose trip the drone's charge and the bay's air
  cover: the air it keeps, plus `TripAir` for a canister in the fuel slot (`SlotTakes`). A canister is opened only when
  the air kept alone falls short. The drone takes all the air kept; the bay keeps none. Missing any, nothing is spent.
  A bay without power still launches a charged drone. One launch per bay per look.
- **Trip cost:** Manhattan from the drone to the target, on to the crate with an empty slot nearest it, across that
  crate, back to the bay, plus `Spare`: in ticks against air, in charge against charge. A target failing it is skipped.
- **Targets**, in the bay's zone and vacuum flood, claimed by no drone: a piece of a kind in `NetCatches`, not reeled,
  its middle in `Space`; a floor stack `Afloat`, no hero's body, lying at least `OwnPickupDelay`. Nearest by Manhattan
  first, then pieces before stacks, then the lowest id. On a planet there are none: no launch, nothing spent.
- **Each tick** it pays charge as any drone and one `Air`; short of either it drops. It turns home `Low` when charge or
  air no longer covers the way home plus `Spare`.
- **At the target**, and only there, in the cell of its middle, it catches the piece (`Catch`: the loot rolled, recorded
  found, counted obtained) or lifts the stack whole (`Lift`), into its net: one catch each, whatever the loot. A target gone, reeled, out
  of space or of the zone, out of its reach (no path from the drone in its flood: drifted under the hull, say), or taken
  first the same tick (hero, net), is dropped at the next cell centre for the next target, or home.
- **Next**, after each catch or a target dropped: while its net holds fewer than `SalvageHolds`, the nearest affordable
  target from where it is. With the net full, no target left, or none its air and charge cover, it delivers what it
  holds; after a delivery, the next target the same way; with nothing held and no target, home (`Low` when one was
  passed by for want of air or charge).
- **Delivery:** to the nearest served crate in its flood with room for any stack of the load. It puts in what fits, in
  load order, the rest to the next crate with room; with none, the rest lies at its cell beside the last crate tried,
  and the net is empty. A crate gone or switched off on the way: the next one, else the load lies where the drone is.
- **Home:** it docks as any drone; the air it has left is added to its bay's; a load still carried lies at its bay. A
  bay taken apart, or no longer a bay, loses the air it kept at the next look. A drone with no way left drops: it and
  its load become floor stacks, floating in space, for a hero or another salvage drone to fetch.

## Invariants

- No two drones go for one target; canisters spent never outnumber launches, and air is never made (bays' and drones'
  air at most the last tick's plus `TripAir` a canister); no salvage drone stands in a solid cell; air never goes
  negative; no net holds more than `SalvageHolds`; items are kept but the loot a catch rolls and stacks gone past the
  grid's edge (`SalvageInvariantTests`, random walls, doors, pieces, stacks, crate fullness and canisters).
- Saved and loaded at a random tick mid-trip, a world plays on alike; asking `Reached`/`Supplied` on one peer between
  ticks changes no hash (`SalvageInvariantTests`, `SalvageTests`).
- Four bays of a drone and two salvage drones each among forty floating stacks keep a tick under budget (`BudgetTests`).

## Deliberately not handled

- Logistics drones do not fill the fuel slot: canisters come by hand.
- Air costs one a tick inside the ship and out. A drone may fly with more than `TripAir`: the air kept and a canister.
- The air a bay keeps is not shown, and is lost with the bay: no canister back.
- A lifted floor stack takes one place in the net whatever its count; the net has no weight or volume.
- A drone turning home low lays its load at the bay rather than delivering it.
- Wreckage, asteroids and clods are not taken, and nets are not emptied: other tools' work.
- Target and crate order is Manhattan, not path length; the turn home covers detours.
- A catch shows no popup; it is recorded found only. No alert for an empty fuel slot or full crates.
- Pieces, meteors and heroes pass through drones: nothing collides but a net
  ([0006](../../../docs/decisions/0006-debris-in-the-simulation.md)).
- A dropped drone is not recalled: it drifts away like any floating stack.
- A canister opened is not given back when the target vanishes; the trip goes on to the next target.
- Before launch only an empty crate slot is checked, not room for the load; delivery handles the rest.
- A docked drone with a canister shows no live reason for staying put; the item's card says what it needs.

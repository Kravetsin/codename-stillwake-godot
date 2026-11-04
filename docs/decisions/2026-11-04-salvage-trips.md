# Salvage trips

- Status: accepted
- Date: 2026-11-04
- Supersedes: in [salvage-drones](2026-11-03-salvage-drones.md), one canister spent on every launch for 30 s, leftover air
  lost on docking, one load a trip, and decided point 3

## Context

The playtest of the salvage drones (PR 379) found three things. A drone went for a piece that had drifted under the hull,
where no path leads, and looked as if it hung or took the piece from afar. A canister bought 30 s of flight and was gone on
every launch, even after a trip of a few seconds, so a bay ate canisters faster than a hero could bring them. And a drone
flew one piece to the crates at a time, most of every trip spent going back and forth.

## Decision

- **A target out of reach is dropped.** A salvage drone takes a piece or a stack only in the cell of its middle. When its
  target has no path left in its flood (under the hull, say), or leaves the zone or space, it drops it at the next cell
  centre and picks the next by the usual order, or goes home. The cause: an unreachable target sent the drone home still
  claiming it, so it neither went on nor let it go; nothing caught from afar.
- **A canister is five minutes, and its air is kept.** `TripAir` is 6000 ticks. A drone home adds the air it has left to
  its bay's, kept in Logistics' saved state per bay (`BayAir`, sorted by bay id). A launch opens a canister only when the
  kept air cannot cover the trip by the same estimate as before; the drone takes all the air kept. A bay taken apart loses
  its air. With no target the kept air and a canister cover, nothing is spent, as before.
- **The net holds four.** `SalvageHolds` is 4; a lifted floor stack is one catch whatever its count. After each catch the
  drone goes for the next target while its net has room and its air and charge cover it; with the net full, no target
  left, or none it can afford, it delivers as before, then goes on. The drone saves what it caught, in order (`net`: the
  kind and the piece's id, or a stack's item); the client draws up to four pictures in its net in catch order, in rows of
  two, smaller the more, a lifted stack by its item's icon, and the net half shuts again at each catch.
- Save format 49 turns a drone's saved `caught` and `piece` into its `net`.

## Decided

1. Air kept is per bay, not per drone: a bay of two salvage drones shares it; the next to launch takes it all.
2. A drone may fly with more than `TripAir`: the air kept and a fresh canister.
3. A target the kept air and one canister cannot cover is skipped (replaces salvage-drones' point 3).
4. A catch whose loot rolls nothing takes no place in the net.

## Deliberately not handled

- The air a bay keeps is not shown anywhere: the drones' trips show it.
- A bay taken apart gives no canister back for its air.
- The net has no weight or volume: four catches of any size.
- A drone turning home low still lays its load at the bay rather than delivering it.

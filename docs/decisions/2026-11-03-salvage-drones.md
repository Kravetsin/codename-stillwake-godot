# Salvage drones

- Status: accepted; a canister a launch for 30 s, leftover air lost, one load a trip and point 3 superseded by
  [salvage-trips](2026-11-04-salvage-trips.md)
- Date: 2026-11-03
- Extends [0062](0062-drones-keep-machines-going.md): a second kind of drone, docked in the same bay

## Context

Debris drifts past the ship and is caught by hand, by harpoon or by a net on the hull. Ordinary drones never leave the
air, so nothing automated fetches what drifts past a bay. The next stage wants salvage to become production without a
hero out on a jetpack.

## Decision

- **A new drone kind, `salvageDrone`.** It docks in an ordinary drone bay beside logistics drones, has a battery like any
  drone (charged on its pad, its pad lamps showing it), and flies on a canister of air from the bay's new fifth slot, the
  fuel slot (`MachineSpec.SlotTakes`: that slot takes only canisters, and canisters go nowhere else). Crafted at the
  circuit assembler (nozzle 2, tube 2, plate 3, circuit 2), its block bought in the market's fourth row. Save format 48
  gives every bay its empty fuel slot.
- **Its rules** (the source of truth: `sim/Logistics/Salvage/README.md`):
  - It launches at a bay's look when no ordinary drone did, charged to half, with a canister in the slot, a served crate
    with an empty slot in reach and a target whose trip one canister (`TripAir`, 30 s) and its charge cover; then one
    canister is spent (superseded: air kept per bay, a canister opened only when short, 5 min, salvage-trips). Missing
    any, nothing is spent. A bay without power still launches a charged one.
  - It flies through doorways shut or open (they stay shut), into vacuum. Targets are the pieces a net would catch and
    stacks floating in space in the bay's zone, nearest first, pieces before stacks; no two drones go for one.
  - It catches a piece (its loot rolled, recorded found) or lifts a stack, brings each load to the nearest crate with
    room, then goes for the next target, until air or charge runs low and it turns home; leftover air is lost
    (superseded: up to four catches a load, leftover air kept by the bay, salvage-trips).
  - With no way home left it drops, it and its load floating stacks. On a planet it never launches.
- **Its look:** the bay «Хребет с кронштейнами» (`wall`: a spine bolted to the wall, four cantilever arms, the fuel slot's
  canister in a tilted breech, a valve, a vane, a lamp, a beacon) and the drone «Сетчатая рама» (`netter`: a frame with a
  net hung under it, nozzles that puff air), both picked by the user on their pages and ported whole
  (`tools/anime-art/subjects/bay.js`, `salvage.js`). The netter's half-shut grip is its open net, as picked. The client
  rules are in `game/features/logistics/README.md`.
- **Its sound:** the jetpack's thrust loop (`crew/thrust-1.ogg`), reused, quieter: −14 dB, a guess to tune by ear. No new files.
- **Its card line:** a plain item card can carry a line (`ItemCard.About`); the salvage drone's says it does not fly
  without a crate with room in reach.

## Decided

1. The salvage drone has a battery like any drone; its pad lamps show its charge (user).
2. The fuel slot's dial shows nothing: its needle is fixed, too small to read (user). Its art has three states: no
   canister, a canister in, a canister feeding.
3. A target one full canister cannot cover is skipped (user). Superseded: the air kept and one canister (salvage-trips).
4. A hero's body floating in space is left.
5. A bay without power launches a salvage drone only if it is charged, as ordinary drones (user).
6. One launch per bay per look, the ordinary one first.
7. A canister spent on a target that vanished is not refunded (user).
8. No live signal says why a docked drone with a canister stays put; the item's card says what it needs (user).

## Deliberately not handled

- Logistics drones do not top up the fuel slot: canisters come by hand, for now.
- Leftover air is not kept on docking: a field that outlives the flight costs more than it gives. Superseded: kept per
  bay (salvage-trips).
- Wreckage, asteroids and clods are not taken, and nets are not emptied: other tools' work.
- Target and crate order is Manhattan, as ordinary drones', not path length.
- A dropped drone is not recalled: it drifts away like any floating stack.
- No alert for an empty fuel slot or full crates: the slot lamp and the floor stacks show it.
- Pieces, meteors and heroes pass through drones: nothing collides but a net (0006).
- A salvage drone flies the same lift over its path as an ordinary one: its net hangs lower, and flying low over a floor it
  overlaps it.

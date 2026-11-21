# Each hero pairs once with a shard of the alien core; the bag comes with it

- Status: accepted; supersedes in part [room-upgrades](2026-10-30-room-upgrades.md) (the bag's rows open from the start)
  and [the-scoop](2026-10-31-the-scoop.md) (the landing's roof and floor)
- Date: 2026-11-21

## Context

A hero had a full bag from its first second, and the light drone ([light-drone](2026-11-21-light-drone.md)) was one more
item in a slot. The drone should be the hero's own companion, met once, and the bag something it gives. The scoop also
needed a reason to be torn off: something hit it.

## Decision

- **The alien core** (machine `alienCore`, trait `Wakes`, `Content.LightDrone.CoreMachine`) wrecked the scoop. It fell
  steeply through the roof over the landing, its torn end left hanging on the back wall, ploughed the landing and stopped
  in its floor against the outer lock (`Wreck.Rows`, the impact path's
  [pick](https://claude.ai/artifact/Pw42idMu6k7aEppWJYzTeJ)). It is fixed: never built, broken or taken apart.
- **Pairing.** E on the core sends `PairDrone(Core)`, only from almost against it: the hero's body within
  `Hero.CoreTouch` (one cell) of its footprint (`Aim.Touches`). `Crew` refuses `crew.noHero` (none, or passed out),
  `crew.hasDrone` or `crew.noCore`. Otherwise `Players.Pair` records the peer in `Drones` for good, through death,
  leaving and every surface; it is never undone, and the core stays for the next hero. Nothing is taken from the hand.
- **The core lost.** If a cell of the floor under the core is broken (`CrewModule.CoreLost`), the core is gone and every
  hero still without a drone is paired where it stands, then and later.
- **The bag is the drone's.** `Items.Own(peer)`: unpaired, the hotbar alone; paired, `Inventory.Start` plus the bag
  upgrades; creative, all. Stacks in a shut slot wait unseen. An unpaired pick-up past the hotbar is refused
  `items.full`; what digging or a delivery gives past it drops at the feet. Unpaired, the bag's window is not shown,
  only the hotbar and the equipment; paired, the bag is the drone's hologram ([drone-frame](2026-11-21-drone-frame.md)).
- **Seen in the dark.** `CoreGlow` breathes an ember halo over the core with a dim light, gone for a player once it is
  theirs or lost. The roof's hole has scorched edges (`CoreScorch`, `CoreScorchView`): an ember line breathing on each
  plate face the path cut, rare sparks off it and soot stains over the plates near it, each lasting while its plate
  stands. The pick was «Тлеющие кромки» with the soot stains of «Копоть»
  ([page](https://claude.ai/artifact/3WM3UuBhuyh35wxiiYkf2N)). A wall built there later is clean scrap.
- **Saves.** Format 68 renames `helmetLamp` to `lightDrone`; 69 turns every scanner stack into a drone and grants the
  scanner's node to a save that held one; 70 drops the `mount` slot, pairing any peer whose drone sat there.

## Deliberately not handled

- A drone is never lost, broken, unpaired or handed over, and draws no power.
- One drone per player: `crew.hasDrone` refuses a second.
- Only the starting wreck has a core. On a map with none nobody pairs: creative still opens every slot, but no drone
  floats; dev runs pair through the save (`--paired`).
- Friends see the drone, never someone's bag.

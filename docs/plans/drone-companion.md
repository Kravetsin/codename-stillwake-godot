# Design: the drone companion

By design-author, cut by design-reviewer. It continues [light-drone](light-drone.md): the item and its `mount` slot give way
to a drone each hero pairs with once. The mode wheel, the mode kept on the client and the scanner's unlock stay as they are.

## Problem

A hero has a full bag from the first second, and the light drone is one more item in a slot. The drone should be the
hero's own companion, made at the pod, and the bag something the drone gives.

## Player experience

1. A new hero wakes at the pod with the hotbar and its worn gear only. Tab opens the usual panel, its bag rows shut.
2. The hero takes scrap from the hold's piles and crafts the drone at the pod, a pod craft from the start.
3. With the drone in hand, a right click wakes it: it floats up by the head and is the hero's for good. The bag rows open.
4. Tab opens the bag as a hologram the drone projects. F switches modes, as now.

- **A friend an hour in** wakes bare and crafts their own drone at the pod, paying from the crew's crates
  (craft-from-crates, `TakeStock`). Bag upgrades and modules are crew-wide, so it has everything the crew bought.
- **Death** works as today: the body drops the stacks; the hero wakes at the pod with its drone.
- **Creative** opens every slot, as today, paired or not.

Gone wrong: before the drone the hotbar fills. A pick-up past it is refused `items.full` and stays on the ground; what digging
or a delivery gives past it drops at the hero's feet, as today's full bag does. The cure is the drone, a few scrap away.

## Out of scope

- Losing, breaking, unpairing or handing over a paired drone; its power draw; walls stopping it.
- Unlocks per player: modules and bag rows stay crew-wide.
- Friends seeing someone's bag projection: they see the drone only.
- The drone's changing look (shards, shapes per mode): after its pick page, a plan of its own.
- Keeping anything of old saves (saves break at release).

## Approach

**Who uses it.** This stage: the bag, the lamp, the scanner; next the holograms, the magnet and alarms of light-drone, then
black caves (0077), storm warnings and scanning creatures.

**Sim.** Whether a peer has paired is the player's across surfaces and deaths, so it lives in `Players` (global, while `Items`
is a slice per surface): `Drones`, the paired peers, sorted; the query `HasDrone(peer)`, the op `Pair(peer)`. `Crew` handles
`PairDrone` as it handles `Eat`: it takes one `lightDrone` from the hand (none in creative), refusing `crew.noHero` (also passed
out), `crew.hasDrone` or `items.notEnough`. `Items` reads `HasDrone`: an unpaired hero's own slots are the hotbar alone, then
`Inventory.Start` plus the bag upgrades. Equipment stays with the body. Trash, sort and quick stash work on the open slots
only. Stacks left in slots that close (creative turned off) wait unseen until pairing. The `mount` slot and the item's
`Equip` go; `lightDrone` becomes a pod craft of scrap only, the pod showing it only to an unpaired hero.

**Client.** The drone is drawn for a paired hero, not by an item worn. Once paired, the panel's bag block is drawn as a
projection from the drone with `holo.gdshader`, as `MachineHolo` beams a machine's window, its input passed through as the
panel's is now.

## Determinism and co-op

The right click sends `PairDrone`. `Drones` is saved and hashed, nothing derived from it; a joining player receives it with
the world. The projection and the mode are the client's; a friend draws a peer's drone from `HasDrone` and `Seen.Drone`. The
save format goes up one, with an empty step if the migration tests want one: an old save's equipment shifts with `mount` gone.

## Trade-offs accepted

- **Crew-wide unlocks:** a friend's first drone has everything; catching up fast is what co-op among friends wants.
- **No second drone:** `crew.hasDrone` refuses, and the pod hides the card; no gifts.

## Tests

- Scenarios: pairing takes one drone; each refusal; creative pairs for free; an unpaired pick-up past the hotbar refused
  `items.full`, anything given past it dropped; pairing opens `Start` plus the bag upgrades; pairing survives death and a
  planet trip; a friend joining pairs from the crates.
- Invariants: an unpaired hero not in creative has at most the hotbar's own slots open; a pairing is never undone.

## Build order, each step playable

3. Pairing and the hotbar-only start: `PairDrone`, `Drones`, own slots tied to pairing, the `mount` slot gone, the pod's craft,
   the format bump; the disc drawn for a paired hero.
4. The bag as the drone's hologram.
5. The look: a pick page of cores and shards moving at game scale, then its own plan.

Steps 3–5 of light-drone (holograms, magnet, alarms) follow these.

## Open questions

- **The drone's cost:** how much scrap? (Recommended: 10.)
- **Bag rows at pairing:** the two of today, or one, so bag upgrades matter more?

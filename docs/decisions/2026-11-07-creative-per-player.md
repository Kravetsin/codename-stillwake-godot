# Creative mode is each player's

- Status: accepted
- Date: 2026-11-07
- Amends: [0008](0008-items-inventories-and-crates.md), where creative mode was the world's

## Context

Creative mode was one flag of a surface's `items` slice: `SetCreative` from any peer switched it for everyone. In co-op,
one developer testing a machine turned the game creative for the other. The user asked for creative mode to be switched
per player, so that **when one player turns it on, the other does not notice.**

## Decision

- **`items` keeps the peers in creative mode**, ascending (`ItemsState.Creative`); `SetCreative(On)` puts its sender in
  or out. `Creative(peer)` is the query; there is no world-wide one. A new surface keeps the list (`Inherit`).
- **A creative peer pays as if it had the items.** `Take` and `TakeStock` take nothing from its slots or crates and
  return the whole cost; `ShortOf` and `ShortOfStock` never refuse it. Whatever records a payment records it alike:
  a pipe's or cable's `Paid`, a blueprint's `Delivered`, a trader's payment. So whoever takes apart what a creative peer
  made gets back what any teammate's would give.
- **Blueprints are shared, so a creative worker pays its parts from nothing.** `Owes` and `LackOf` are the world's, with
  no creative clause; a creative worker pays the first item the plan lacks, whatever its stock holds. Another peer working
  on after it owes only the parts after; `Delivered` always covers the parts begun.
- **Research has world truths and a peer's view.** `Knows(id)` and `Done(node)` are what the world knows;
  `Knows(peer, …)` and `Done(peer, node)` add the peer's creative mode. Commands (`PlacePlan`, `Craft`) and the client ask
  for the sender's. A creative peer no longer arms magnetic flares, which read the world's `Knows`.
- **Slots:** a creative peer's own inventory and belt open every slot (`SlotOpen(peer, place)`); a trade station's slots
  are the machine's and open by upgrades only.
- **A creative peer's hero** keeps full air and takes no room air, leaves no body, may `Spawn`, and drains no battery.
- **Saves of format 52** give a world in creative mode to every peer with an inventory on any surface
  (`Migrations`, 52 → 53).
- **The client** reads its own peer's mode everywhere: the dev panel, the inventory's label, the fabricator, the build
  menu and its ghosts. Nothing shows that another player is creative.
- The test of the requirement: `CreativeTests.APeersCreativeModeChangesNothingForAnother`, a peer's fuzzed play in two
  worlds alike but for another peer's creative mode, refusals and saves compared; it fails with the bag's slots made
  world-wide again.

## Deliberately not handled

- **Instant building is still the world's.** It follows in its own change (`docs/plans/creative-per-player.md`, C2).
- **Items a creative peer makes from nothing reach others**: it can spawn a stack and drop it, or build and let another
  take it apart. That is the creative player acting in the world, as any teammate who shares does.
- **A creative peer's refunds make items**: a craft cancelled or a cable cut gives back what it "paid". It can spawn
  anything anyway.
- **A creative hero takes no room air**, so a shared room keeps a little more; another player cannot tell it from a
  hero in a suit.
- **No permission:** any peer switches its own mode; co-op among friends (principle 6).
- **The market shows the world's purchases** to a creative player too; it no longer shows every block bought.
- **The testbed and the store world** make the host (peer 0) creative; a guest switches it on from the dev panel.

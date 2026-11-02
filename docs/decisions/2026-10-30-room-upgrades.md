# Room is bought on the tree

- Status: accepted
- Date: 2026-10-30

## Context

Several rings of the tree held only a handful of nodes, Electronics three or four, and once blueprints were bought credits had
little left to buy. The user wanted a smaller inventory at the start and room bought with credits in every ring, in a category of
its own: inventory slots, belt slots, trade station slots.

## Decision

- **A branch "Upgrades"** on both tabs. An upgrade (`Content.Upgrades`) is a tree node like any block: bought with `BuyBlock`,
  remembered in `Knowledge.Granted`, one thing a node, one slot an upgrade. Its price is its own, tuned by hand, for it has no recipe to work one out from.
- **Every ring has a bag upgrade.** The hotbar and two rows are open at first (27 slots), +1 a ring, up to 38. One slot is
  worth much now that stacks are large, and the tree is not final: later rings must not run out of room to add.
- **The belt grows with what it carries:** 1 slot at first, the second in the ring of the battery, the third in the ring of the
  accumulator. **A trade station** starts with 2 and comes to 8, from the first ring on.
- **For the whole crew**, as credits and blueprints are, not per hero: a host-owned world has one economy.
- **The slots always exist**, `Inventory.Slots` and `MachineSpec.Slots` being the most they can come to, and `Items.SlotOpen` says
  which are open now. So layouts and saves do not change with a purchase, and nothing can be stranded: upgrades are never lost. A
  trade station's closed slots work as a machine's outputs do, taking nothing in. A slot not open is not drawn.
- **A world with no `Layout.Known`** is granted every upgrade, as it is every blueprint, and creative mode opens everything.

## Consequences

- Format 41 moves the equipment up. An older world is granted what it already had open: 36 inventory slots, three belt slots, four
  station slots.
- We do not offer slots of other machines or crates yet; the next kind is a line in `Upgrades.All` and a place that reads it.
- Prices are a first guess against each ring's blocks and need the playtest, all the more with capsules paying as they do.

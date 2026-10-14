# Crafting

Making parts by hand ([0027](../../docs/decisions/0027-what-is-analysed-opens-what-is-made-of-it.md)):
each peer's queue of crafts, paid when queued, made one unit at a time into its inventory, as Rust
queues them. A module of its own rather than part of `Items`: it asks `Research` what is known, and
research depends on items.

- **Depends on:** `Items` (`IItemsQueries`: `ShortOf`, `FullFor`; `IItemsOps`: `Take`, `Give`),
  `Research` (`IResearchQueries.Knows`).
- **State** (`CraftingState`, members internal): `Queues` — each peer's that ever crafted, in peer
  order: `Peer`, `Next` (the id the next entry gets), `Progress` (ticks of work on the first entry's
  unit) and `Entries`, each an `Entry(Id, Recipe, Count)` in the order queued, `Count` the units left,
  the one at work included. A queue that empties is kept, so its ids keep counting. Saved; nothing is
  derived.
- **Files:** `Crafting.cs` — the module, its commands and system; `State.cs` — its slice and
  `Entry`; `Fuzz.cs` — its fuzz generator.
- **Runs** after `Building` in `Registry.All`.

## Rules

- A craft is named by the item it makes and costs, a unit, what `Content.Crafts` says, taking its
  `Ticks`.
- `Craft(Recipe, Count)` takes the cost of all `Count` units from the sender at once (nothing in
  creative mode) and adds an entry at the end of its queue, with the queue's next id. Refused with
  `crafting.unknown` (no craft makes it, or it is not known), `crafting.full` (the queue holds
  `Content.Crafting.QueueMost` entries) or `items.notEnough`; refused whole, nothing is queued.
- `Cancel(Entry)` takes the sender's entry of that id off its queue and gives back the cost of its
  units left, the one at work included, as content prices them now. Refused with `crafting.noEntry`
  (no such entry in the sender's queue: never queued, finished, or another's) or `items.full` (the
  refund does not fit). The first entry cancelled, the next starts from nothing.
- Each tick, in the simulation phase, in peer order, each queue's first entry works a tick; at its
  craft's `Ticks` one unit goes into the peer's inventory and the next starts. With no room for it,
  the unit waits, its progress at its ticks, counting no more, until a slot is free.
- Crafting needs no power and no station; moving does not stop it. A peer that leaves keeps its
  queue, which finishes into its inventory.

## API

No module depends on crafting, so it has no query interface: the client reads `Registry.Crafting`.

- **Queries:** `Queue(peer)` — its entries, live and read-only, the first at work; `Progress(peer)` —
  ticks of work on its first entry's unit, its ticks while that unit waits for room.
- **Commands:** `Craft(Recipe, Count)`, `Cancel(Entry)`.
- **Fuzz:** crafts of every recipe and of an item no craft makes, one to nine units, refused as
  unknown, unpaid or full, paid mostly in creative mode, which the items fuzz switches on now and
  then; cancels of the sender's entries, of finished ids and of ids never given.

## Invariants

- A queue never holds more than `QueueMost` entries; its first entry's progress stays below its
  ticks but while its unit waits for room; ids within a queue only grow.
- Items are conserved: what a craft took comes back as its units or a cancel's refund
  (`CraftingTests`, counting every item by the raw items it is made of).

## Deliberately not handled

- A count below one: the client sends one or five, and peers run one build.
- A finished unit is never dropped at the hero's feet: it waits, so crafting needs no `crew`.
- In creative mode a cancel gives back what was never taken, as taking apart does: a refund keeps no
  record of what was paid.
- The same recipe queued twice is two entries, each cancelled alone, as Rust shows it.

# Items

Every stack of items: each hero's inventory, by its peer's id; what each machine holding items (a
crate, a furnace, the analyzer) holds, by its machine id; the stacks lying on the floor; and creative
mode, the world setting in which nothing is ever taken
([0008](../../docs/decisions/0008-items-inventories-and-crates.md)). Ported from
`packages/sim/src/modules/items`; its rules hold here unchanged.

- **Depends on:** `Grid` (`IGridQueries`), for which machines hold items and what holds a stack on
  the floor up; `Rooms` (`IRoomsQueries`), for where space is.
- **State:** `Inventories` — those changed at least once, by peer id in id order; `Crates` — the
  machines holding something, by machine id in id order; `Creative`; `Floor` — stacks lying about
  (`FloorStack`: id, item, count, cell, x, y, vx, vy, by, at) in id order, with `NextFloorId`;
  `Ticks`, the module's own clock. All saved, as sorted lists; nothing is derived.
- **Files:** `Stacks.cs` — the rules below, on slots; `Floor.cs` — stacks on the floor; `Items.cs` —
  the module, its commands and interfaces; `Fuzz.cs` — its fuzz generator; `State.cs` — its types.
- **Start:** every inventory and every machine empty: what a hero has, it caught, smelted and crafted.

## Rules

- An inventory is `Inventory.Slots` slots, a machine holding items its `Slots` in `Machines`, each a
  `Stack` (`Item`, `Count` from 1 to the item's `Stack` limit in `Items`) or null. Items are known by
  their id; every material is one, the salvage items, with `Yields`, are what caught debris
  becomes, and the parts (wire, plate, gear, rotor) are crafted by `Crafting`. The inventory's first row is the hotbar.
- **Giving** puts each item onto the stacks of that item in slot order, up to their limit, then into
  the empty slots in slot order. It gives all of a cost or none of it.
- **Taking** takes each item from the last slots first, so the hotbar is the last to empty. It takes
  all of a cost or none of it.
- **Moving** takes up to `Count` from the `Src` slot to `Dst`, within the sender's inventory, between
  it and a crate, or between crates: the lesser of `Count`, what `Src` holds and what fits.
  - Onto an empty slot or a stack of the same item, as much as fits there.
  - A whole stack onto another item swaps the two; part of one is refused.
  - With no slot in `Dst`, that side as a whole: the item's stacks there in slot order, then its
    empty slots, as much as fits. Within one side a move names its slot.
  - A machine with `Inputs` (a furnace) takes items only into its first `Inputs` slots, refusing the
    rest with `items.takeOnly`; a move into it naming no slot goes into the first.
- A change of nothing (a cost of zeros) changes nothing and stores no inventory; a machine emptied
  loses its entry.
- In creative mode nothing is taken; giving still gives: what is taken apart is still there.
- **The floor.** A stack let go lies about, at a position and a speed in thousandths of a cell. A
  drop throws it from the bottom middle of the sender's cell the way its hero faces, at
  `Floor.ThrowSpeed` and up at `Floor.ThrowLift`; one with no way to face (an overflow) is let go
  still. Each tick, in id order: inside anything built (a wall, a floor, a closed door, a machine)
  it rises a cell and stops; in space it flies on, nothing slowing it, and drifts back
  `Floor.SpaceDrift` with everything outside the moving ship; inside the ship it falls by
  `Floor.Gravity` up to `Floor.MaxFall`, stops against what is built or the grid's edge, and on what
  it lands slides to a stop by `Floor.Friction`. One past the grid's edge is gone: thrown away into
  space. Then stacks of one item at rest in one cell merge into the oldest, up to the stack limit.
  Positions divide by a cell rounding down, left of and above the grid too.
- **Picking up** gives as much of a stack as fits into the picker's inventory and leaves the rest.
  The peer that dropped a stack may not take it back for `Floor.OwnPickupDelay` ticks; anyone else
  may at once.

## API

A cost is `IReadOnlyList<(string Item, int Count)>`, as content writes `Cost` and `Yields`:
`[("scrap", 2)]`. A negative amount is a bug in the caller and throws; an item named twice is
counted twice.

- **Queries** (`IItemsQueries`): `Inventory(peer)` and `Crate(id)` — their slots, live and
  read-only; `Creative`; `Totals(peer)` — how much of each item the peer carries, in the order first
  found; `ShortOf(peer, cost)` — `items.notEnough` when the peer cannot pay `cost`, never in creative
  mode; `FullFor(peer, cost)` — `items.full` when `cost` does not fit, creative mode or not. A caller
  refuses with these keys. `AfterMoves(peer, moves)` — copies of the peer's inventory and of the
  machines the `Move`s and `Drop`s touch, as the commands would leave them, refused ones skipped: what
  a client shows of its own moves before their tick runs. `Floor` — the stacks on the floor, live and
  read-only; `PickUpProblem(peer, id)` — the refusal that peer's `PickUp` would get;
  `PickUpAmount(peer, id)` — how much of it would fit.
- **Furnaces:** `CanSmelt(id)` — whether its input holds salvage whose `Yields` fit its outputs;
  the operation `Smelt(id)` turns one of it into them, the caller checking `CanSmelt` first;
  `Consume(id, slot)` uses up one of what a slot holds, a fuel as a fire is lit.
- **Operations** (`IItemsOps`): `Take(peer, cost)` — the caller checks `ShortOf` first, and taking
  past what is there throws; it returns what it took, nothing in creative mode, so a caller refunds
  exactly that. `Give(peer, cost)` — the caller checks `FullFor` first, and giving past the room
  throws. `GiveOrDrop(peer, cost, cell)` — gives what fits; the rest falls on `cell`, dropped by that
  peer. `DropAt(peer, cost, cell)` — lets all of `cost` fall on `cell`, still, dropped by that peer:
  what a cancelled blueprint held.
- **Commands:** `SetCreative(On)`, from the dev panel, with no refusals.
  `Move(Src, Dst, Count)` — `Src` a `Place(Slot, Crate?)`, `Dst` a `Target(Slot?, Crate?)`, no crate
  meaning the sender's inventory; refused with `items.noCrate` (a side names no machine holding
  items), `items.noSlot` (a slot out of range, `Src` itself, or no slot within one side),
  `items.takeOnly` (a slot of a machine's past its inputs), `items.empty` (nothing in `Src`, or
  `Count` below 1) or `items.full` (nothing fits at `Dst`, or part of a stack onto another item).
  `Drop(Src, Count, Cell, Facing)` — throws up to `Count` from a slot from `Cell`, the sender's as its
  client reports it; refused with `items.noCrate`, `items.noSlot`, `items.empty` or `items.noCell`.
  `PickUp(Id)` — refused with `items.gone`, `items.tooSoon` or `items.full` (none of it fits).

Tests put items into an inventory with `GiveItems` and into a machine with `FillCrate`, from
`sim/Tests/ItemsTesting.cs`.

## Invariants

- Every stack holds from 1 to its item's limit; every inventory and machine has its number of slots.
- A take or a give changes the peer's totals by exactly the cost, or not at all; a move, a drop and
  a pick-up change no total across inventories, machines and the floor.
- Every stack on the floor holds from 1 to its item's limit.
- A machine has an entry only while it holds something; `building` takes a crate apart only empty.

## Deliberately not handled

- Who owns items across sessions: an inventory belongs to a peer id, so after a load it goes to
  whoever joins in that slot, and a peer that rejoins mid-session starts under a new id.
- Reach: the sim moves items between a hero and any crate; the client opens only crates within reach.
- A stale pick at a shared crate moves whatever is in the slot by the time it applies.
- Who may switch creative mode: anyone; it is a dev panel switch among friends.
- The desync fuzz never fills an inventory with refunds, so `items.full` from building is proven by
  scenarios; moves draw it.

## Differences from TypeScript

- A cost is a list of pairs, not an object: order is the list's, and a fractional amount cannot be
  written, so only a negative one throws.
- Stacks are immutable records; the slots handed out are arrays typed read-only.
- `AfterMoves` takes the `Move` and `Drop` commands themselves and returns the machines touched as a
  list in first-touched order, where TypeScript returns a `Map`.

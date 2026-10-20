# Items

Every stack of items: each hero's inventory, by its peer's id; what each machine holding items (a
crate, a furnace, the analyzer) holds, by its machine id; the stacks lying on the floor; and creative
mode, the world setting in which nothing is ever taken
([0008](../../docs/decisions/0008-items-inventories-and-crates.md)). Ported from
`packages/sim/src/modules/items`; its rules hold here unchanged.

- **Depends on:** `Grid` (`IGridQueries`), for which machines hold items and what holds a stack on
  the floor up, a slope's diagonal too (`ShapeAt`); `Rooms` (`IRoomsQueries`), for where space is and which open cells are in a room.
- **State:** `Inventories` — those changed at least once, by peer id in id order; `Crates` — the
  machines holding something, by machine id in id order; `Creative`; `Floor` — stacks lying about
  (`FloorStack`: id, item, count, charge, cell, x, y, vx, vy, by, at, and the `Body` it is part of, if any) in id order, with `NextFloorId`;
  `Ticks`, the module's own clock; `Kept` — what input slots remember (`Kept(Machine, Slot, Item)`), by machine and slot, sorted (0062); `Running` — the electric tools heroes run (`Runner`: peer, item), at most one a
  peer, in peer order (0055). All saved, as sorted lists; nothing is derived.
- **Files:** `Stacks.cs` — the rules below, on slots; `Floor.cs` — stacks on the floor; `Items.cs` —
  the module, its commands and interfaces; `Fuzz.cs` — its fuzz generator; `State.cs` — its types.
- **Start:** every inventory and every machine empty: what a hero has, it caught, smelted and crafted.

## Rules

- An inventory is `Inventory.Slots` slots of its own and then its equipment, a slot for each kind in
  `Inventory.Equipment` (head, back, three on the belt), `Inventory.Total` in all
  ([0047](../../docs/decisions/0047-equipment-the-jetpack-and-the-slower-swim.md)); a machine holding items
  has its `Slots` in `Machines`. Each slot is a
  `Stack` (`Item`, `Count` from 1 to the item's `Stack` limit in `Items`, and `Charge`) or null. Items are known by
  their id; every material is one, the salvage items, with `Yields`, are what caught debris
  becomes, in stacks of 20 (hull plates) and 50 (scrap chunks, asteroid ore) since 0062, and the parts (wire, plate, gear, tube, and of them the rotor, frame and coil) are crafted by
  `Crafting` into the lifepod's outputs;
  circuits are salvaged from cable racks and derelicts (`Debris`) or made at the circuit assembler of
  silicon and wire, and go into what is electric: the battery, the charging station, the drill, the
  stations that draw power and the solar panel; solar cells, made there of silicon, glass and wire, into
  the solar panel alone. Containers and wreckage are items too, a
  stack of one, never carried but in creative mode and the testbed's crate. The inventory's first row is the hotbar.
- **What one item holds of its own** (0053) is on its stack: `Charge`, a battery's, from 0 to its item's
  `Capacity`, in the units machines draw power in (power-ticks, stored in thousandths: `Content.Charge`), only on items of a stack of one that have one, 0 on anything else. Every copy of a stack is
  made with `with`, so it goes wherever the stack goes: a move, a swap, a crate, a drop and its pick-up
  (`FloorStack.Charge`, `AsStack`). Stacks merge, onto a slot or on the floor, only when alike
  (`Stacks.Alike`: one item, one charge); a whole stack moved onto one not alike swaps with it, so two
  batteries swap. Stacks made from a cost, given, crafted or refunded, hold none: a crafted battery is
  empty. No cost names an item with a capacity (`ContentTests`), so paying never destroys charge. Saved
  only where it is not 0. A later per-item value (a tool's wear) is one more such field, copied, compared
  in `Alike` and carried by the floor alike.
- **Equipment.** An equipment slot holds only an item whose `Equip` in `Items` is its kind: the jetpack on
  the back, batteries on the belt. A move that would leave anything else in one, put there or swapped in from either side, is
  refused with `items.notEquipment` (`Stacks.Misfit`).
- **Giving** puts each item onto the stacks of that item in slot order, up to their limit, then into
  the empty slots in slot order, an inventory's own slots only, never its equipment. It gives all of a
  cost or none of it.
- **Taking** takes each item from the last of an inventory's own slots first, so the hotbar is the last
  to empty; what is worn is never taken nor counted (`Totals`). It takes all of a cost or none of it.
- **Moving** takes up to `Count` from the `Src` slot to `Dst`, within the sender's inventory, between
  it and a crate, or between crates: the lesser of `Count`, what `Src` holds and what fits.
  - Onto an empty slot or a stack alike (`Stacks.Alike`), as much as fits there.
  - A whole stack onto one not alike, another item or a battery holding other charge, swaps the two;
    part of one is refused.
  - With no slot in `Dst`, that side as a whole: the item's stacks there in slot order, then its
    empty slots, as much as fits; an inventory's own slots only. Within one side a move names its slot.
  - A machine with `Inputs` (a furnace, the Former's two, the assembler's three) takes items only into its first
    `Inputs` slots, refusing the rest with `items.takeOnly`; a move into it naming no slot goes into the first. One
    with no inputs (the lifepod) takes nothing in, refused with `items.takeOnly` whichever slot is named.
  - A charging station (a machine with `Charges` set) holds only items with a `Capacity`: a move that would
    leave anything else in it, put or swapped in, is refused with `items.notBattery` (`Stacks.Unfit`).
- **Sorting** (0008, Terraria's): `Sort(Crate)` sorts the sender's bag, its own slots past the hotbar
  (`Inventory.Hotbar`, the first row) up to its equipment, or with `Crate` every slot of a crate (`Machines` kind
  `crate`). Stacks alike (`Stacks.Alike`: one item, one charge) merge up to their limit, the full ones first, so two
  batteries holding other charge stay two; then they are laid from the first slot in order of the item's `Group`
  (`ItemGroup`: tools, wearables, materials and parts, salvage, the rest), its id (ordinal), charge, fullest first, and
  count, largest first; the empty slots after (`Stacks.Sorted`). The hotbar and the equipment stay as they are; every
  item and every charge is kept. Only a crate sorts: a machine's slots have their places (inputs, fuel, outputs). Two
  sorts of one crate in one tick apply in the host's order, the second finding it sorted. In creative mode too.
- **Charge** is filled at a charging station (the charger, a drone bay) by `Power`, through `Charge(id, slot, amount)`, never past full,
  and spent from the belt by an electric tool through `Drain(peer, amount)`, `amount` in power-ticks (a
  running tool's `Draw` for a tick, the drill's 3, 0055): the first battery on the belt, in slot order, holding at least that pays it whole, so the belt drains left to right; false
  when none can. In creative mode nothing is taken and it answers true.
- **Running an electric tool** (0055): `Run(Item, On)` says the sender's hero runs an item with a `Draw` it carries,
  in any slot (which one is in hand is the client's word), or has stopped; its client sends it while its own run
  differs from the sim's, as `Thrust` is. A hero runs one thing: on replaces what it ran, off stops whatever it runs.
  Every tick, before the floor settles, each run in peer order draws its item's `Draw` through `Drain`, touching
  anything or nothing; one whose belt cannot pay the tick, or whose item its peer no longer carries, stops by itself,
  so a run never outlives a belt that cannot pay. The draw is whole power-ticks a tick, so nothing is left over to
  carry. Creative mode draws nothing and never stops a run for want of charge. `Crew` stops a run as its hero passes
  out and while it stays out, and as it leaves (`StopRunning`). `Debris` takes an electric weapon's hit only while
  its sender runs it; the hit itself spends nothing.
- A change of nothing (a cost of zeros) changes nothing and stores no inventory; a machine emptied
  loses its entry.
- In creative mode nothing is taken; giving still gives: what is taken apart is still there.
- **Surfaces** (0076): a peer's own goes with its hero (`ICarries`): `Depart` takes its inventory, equipment and belt
  with their charge, and the electric item it runs, off this surface, and `Arrive` puts them on the next, the run going
  on there, its belt paying from that tick. Floor stacks, bodies and what machines hold stay. A seat's inventory and
  run are on its surface only (`SurfacesTests`, FsCheck, which also counts every item across a transfer). Creative mode
  is a surface's, copied to a surface opened from it (`Inherit`).
- **The floor.** A stack let go lies about, at a position and a speed in thousandths of a cell. A
  drop throws it from the bottom middle of the sender's cell the way its hero faces, at
  `Floor.ThrowSpeed` and up at `Floor.ThrowLift`; one with no way to face (an overflow) is let go
  still. Each tick, in id order: inside anything built (a wall, a floor, a closed door)
  it is **pushed out**, still, to the bottom middle of an open cell found breadth-first from its
  cell over the four neighbours, through built cells only, up to `Floor.PushReach` steps: the
  nearest open cell in a room; with none in reach, the nearest open cell of any kind, which is
  space; ties in both by the lowest cell index (up, then left, then right, then down). So a stack
  comes out into the room even where space is nearer. With nothing open in reach (deep in a block
  of walls) it rises a cell instead, and stops, and is pushed out once in reach; in the top row it
  stays. Afloat in space (`IRoomsQueries.Afloat`) it flies on, nothing slowing it, and drifts back
  `Floor.SpaceDrift` with everything outside the moving ship; elsewhere — inside the ship, and everywhere on a planet,
  its open sky too (0076) — it falls by `Floor.Gravity` scaled by its surface's kind (`Surfaces.Kind.Fall`: two thirds
  on the test strip, rounded down) up to `Floor.MaxFall`, stops against what is built or the grid's edge, and on what
  it lands slides to a stop by `Floor.Friction`. One past the grid's edge is gone: thrown away into
  space. Then stacks alike at rest in one cell merge into the oldest, up to the stack limit.
  Positions divide by a cell rounding down, left of and above the grid too.
- **Machines hold no stack.** A machine is not built to a stack: one falling onto it falls through to the floor it
  stands on, one thrown at it flies on past, and one laid in its cells (a delivery, a refund, a drop in front of it)
  is not pushed out but falls from there. So a stack ends where a hero walks, never on top of a station. Ladders
  hold none either, as before. A stack an older save left on a machine's top falls off on the first tick after the
  load, with no migration: a position is saved, never where it rests, and every tick reads the rule anew.
- **Slopes** ([0069](../../docs/decisions/0069-a-cell-has-a-shape.md)): a stack is a point, its bottom middle, and
  a floor slope (◢ ◣, `Grid`'s `Shape`) holds it on its diagonal, not its cell. Inside means the point is in
  something built: a whole cell, or the slope's solid half (`Shape.Solid`); in its open half a stack is not
  pushed out. Falling, it lands on the diagonal at its x (`Shape.TopAt`); moving into a slope's solid half no
  steeper than it moves (a rise no more than its speed across, a 45° slope taken by anything moving), it skids up
  onto the diagonal, else stops as against a wall; resting on the diagonal it slides to a stop. When the wall
  goes, so does its shape, and the stack falls. A ceiling slope (◤ ◥) is whole to a stack, and a slope's open
  half is no room's, so a stack lying in one falls as aboard even out in space. Pushed out of something built,
  a stack still goes to an open cell, never onto a slope's open half.
- **Picking up** gives as much of a stack as fits into the picker's own slots and leaves the rest.
  The peer that dropped a stack may not take it back for `Floor.OwnPickupDelay` ticks; anyone else
  may at once.
- **Bodies** ([0061](../../docs/decisions/0061-death-leaves-a-body.md)). A hero that dies (`Crew` passes it out)
  leaves its body: `LeaveBody(peer, cell)` lets every stack in its own slots whose item is not `Gear` (the tools,
  the jetpack, the battery) fall on `cell`, still, in slot order, by `FloorStack.Nobody`, each tagged with one
  `Body(Peer, Died)`, `Died` the module's tick. Gear and everything worn stays (equipment slots hold only worn
  items, all gear). Creative mode, or nothing but gear, leaves no body. A body's stacks are floor stacks like any
  other: anyone, its owner too, picks them up at once, as much as fits; they lie still inside the ship and drift
  in space and leave the world at its edge. Each is gone `Floor.BodyLasts` ticks after its death (ten minutes),
  wherever it is, before it moves that tick. Stacks merge only within one body or outside any, so two deaths
  never become one body. A body is gone once its last stack is.

## API

A cost is `IReadOnlyList<(string Item, int Count)>`, as content writes `Cost` and `Yields`:
`[("scrap", 2)]`. A negative amount is a bug in the caller and throws; an item named twice is
counted twice.

- **Queries** (`IItemsQueries`): `Inventory(peer)` and `Crate(id)` — their slots, live and
  read-only, an inventory's equipment after its own; `Creative`; `Totals(peer)` — how much of each item the
  peer carries in its own slots, in the order first found; `Equipped(peer, item)` — whether it wears that
  item in an equipment slot (crew reads the jetpack so); `BeltCharge(peer)` — what the batteries on its belt
  hold, summed, and what they would hold full: the energy bar; `CanDrain(peer, amount)` — whether `Drain`
  would take that many power-ticks now, always in creative mode: `Debris` refusing the drill `items.flat`, and
  the client's drill; `WeaponProblem(peer, weapon, running)` — why a hit of that weapon is refused, the same on debris and
  ground (0077): `items.flat` for an electric one the belt cannot pay a tick of, and with `running`, as a hit applies,
  `items.notRunning` for one its hero does not run; `Debris` and `Building` refuse hits and digs with it;
  the client's drill; `Running(peer)` — the electric item that peer's hero runs, or null: `Debris` taking its hits,
  the client comparing its own run and drawing others' drills spinning; `ShortOf(peer, cost)` — `items.notEnough` when the peer cannot pay `cost`, never in creative
  mode; `FullFor(peer, cost)` — `items.full` when `cost` does not fit, creative mode or not. A caller
  refuses with these keys. `AfterMoves(peer, moves)` — copies of the peer's inventory and of the
  machines the `Move`s, `Drop`s and `Sort`s touch, as the commands would leave them, refused ones skipped: what
  a client shows of its own moves before their tick runs. `Floor` — the stacks on the floor, live and
  read-only; `PickUpProblem(peer, id)` — the refusal that peer's `PickUp` would get;
  `PickUpAmount(peer, id)` — how much of it would fit.
- **Fed stations:** `CanSmelt(id)` — whether its input holds what has `Yields` that fit its outputs, with the
  most of itself it may give back (`ItemSpec.Returns`: a crop's two seeds); the operation `Smelt(id, back)` turns
  one of it into them and `back` of itself, a crop's seeds, the caller checking `CanSmelt` first and drawing
  `back` (0060);
  `Consume(id, slot)` uses up one of what a slot holds, a fuel as a fire is lit.
- **What an input remembers** (0062): each input slot of a fed station or of a machine with a fire (its fuel slot too)
  remembers the item last in it, through every write of the machine's slots (`SetMachine`): what it holds now, or,
  emptied by that write, what it held, so a slot burnt, smelted or grown down to nothing remembers its item. A `Move` or
  `Drop` that takes all of a slot out, a hand's, makes it forget. `KeptAt(id, slot)` — the item, or null: what drones
  refill (`Logistics`). Kept for a machine taken apart, never read: ids are never reused.
- **Machine to machine,** for drones (`Logistics`, [0062](../../docs/decisions/0062-drones-keep-machines-going.md)), no
  hero's inventory in between: `HeldFor(id, item)` — what a machine holds of an item where a delivery of it goes;
  `RoomFor(id, stack)` — how many of a stack would go in; the operations `TakeFrom(id, slot, count)` — up to `count` from
  any slot, outputs too, the stack taken with all it holds of its own, null for an empty slot; `PutIn(id, stack)` — as
  much as fits, as a whole, onto stacks alike in slot order and then into empty slots, returning how many did not fit;
  `LayDown(stack, cell)` — a stack on the floor, still, by `FloorStack.Nobody`, its charge with it. **Where a delivery
  goes:** a fuel (`BurnTicks`) only into a machine's `FuelSlot`; anything else into its inputs but the fuel slot, so a
  furnace's fuel and what it smelts never mix; a machine with no `Inputs` given (a crate) all its slots;
  a charging station (the charger, a drone bay) nothing without a capacity: a bay takes drones, and batteries, to charge. In creative mode too: these only move what is there. Moves by hand
  keep their own rules (`Move`: a slot named, or the first input).
- **Crafting machines:** `OutputFull(id, cost)` — `items.full` when `cost` does not fit into the
  machine's outputs, its slots past its inputs; the operation `Output(id, cost)` puts it there, the
  caller checking `OutputFull` first: what `Crafting` makes. `InputsLack(id, cost)` — what of `cost` the machine's
  inputs lack, item by item, none when they hold it all; the operation `TakeInputs(id, cost)` takes it from them,
  each item from the first input slot first, in creative mode too, the caller checking `InputsLack` first: a queued
  station's unit fed from its inputs (0057).
- **Operations** (`IItemsOps`): `Take(peer, cost)` — the caller checks `ShortOf` first, and taking
  past what is there throws; it returns what it took, nothing in creative mode, so a caller refunds
  exactly that. `Give(peer, cost)` — the caller checks `FullFor` first, and giving past the room
  throws. `GiveOrDrop(peer, cost, cell)` — gives what fits; the rest falls on `cell`, dropped by that
  peer; it returns what it gave, by item: what a caught piece showed its catcher (`Debris`). `DropAt(peer, cost, cell)` — lets all of `cost` fall on `cell`, still, dropped by that peer:
  what a cancelled blueprint held; or by `FloorStack.Nobody`, for anyone to pick up at once: what a
  broken wreck held. `Charge(id, slot, amount)` — adds up to
  `amount` of charge, stored in thousandths of a power-tick, to what slot `slot` of machine `id` holds, never past its capacity: `Power`'s
  charging stations, a share for each slot short of full; it throws for a slot holding nothing with a capacity. `Drain(peer, amount)` — takes
  `amount` power-ticks from the first belt battery that holds that much, as above: a run's tick. `StopRunning(peer)` —
  stops whatever that peer's hero runs: `Crew`, as the hero passes out or leaves. `LeaveBody(peer, cell)` — leaves
  that peer's body on `cell`, as above: `Crew`, as its hero passes out; the caller passes a cell on the grid.
- **Commands:** `SetCreative(On)`, from the dev panel, with no refusals.
  `Move(Src, Dst, Count)` — `Src` a `Place(Slot, Crate?)`, `Dst` a `Target(Slot?, Crate?)`, no crate
  meaning the sender's inventory; refused with `items.noCrate` (a side names no machine holding
  items), `items.noSlot` (a slot out of range, `Src` itself, or no slot within one side),
  `items.takeOnly` (a slot of a machine's past its inputs), `items.notEquipment` (an equipment slot left
  holding what is not worn there), `items.notBattery` (a charging station left holding what holds no charge), `items.empty` (nothing in `Src`, or
  `Count` below 1) or `items.full` (nothing fits at `Dst`, or part of a stack onto another item).
  `Drop(Src, Count, Cell, Facing)` — throws up to `Count` from a slot from `Cell`, the sender's as its
  client reports it; refused with `items.noCrate`, `items.noSlot`, `items.empty` or `items.noCell`.
  `PickUp(Id)` — refused with `items.gone`, `items.tooSoon` or `items.full` (none of it fits).
  `Sort(Crate?)` — no crate meaning the sender's bag; refused with `items.noCrate` (no machine holding items) or
  `items.notCrate` (a machine holding items that is no crate: a furnace, a bay). Reach is the client's, as for a move.
  `Run(Item, On)` — refused with `items.notElectric` (the item has no `Draw`, on or off), and turning on with
  `items.notHeld` (the sender carries none) or `items.flat` (no belt battery holds a tick of its draw; never in
  creative mode). Off with nothing running is no refusal.
Tests put items into an inventory with `GiveItems`, onto a hero with `Wear`, into a machine with `FillCrate` and a
stack holding charge into any slot with `Put`, from
`sim/Tests/ItemsTesting.cs`.

## Invariants

- Every stack holds from 1 to its item's limit; every inventory and machine has its number of slots.
- Every equipment slot holds only an item of its kind (`EquipmentTests`, FsCheck).
- A take or a give changes the peer's totals by exactly the cost, or not at all; a move, a drop, a
  pick-up and a sort change no total across inventories, machines and the floor.
- A sort keeps every item and charge, leaves the hotbar and the equipment as they are, lays the bag from its first
  slot with no gaps, in order, at most one stack of a kind short of full, and a second sort changes nothing (`SortTests`, FsCheck).
- Every stack on the floor holds from 1 to its item's limit.
- Charge is between 0 and the item's capacity, and only on items with one, a stack of one; moves, drops
  and pick-ups keep every item's total and the total charge (`ChargeTests`, FsCheck).
- A run draws exactly its `Draw` a tick it goes on, and one its belt cannot pay stops that tick, paying nothing;
  charge never goes below nothing; a peer runs only what it carries (`RunTests`, FsCheck).
- A machine has an entry only while it holds something; `building` takes a crate apart only empty.
- Death makes and loses nothing: what both heroes carry and what lies about changes only by what leaves, stacks in
  space past the grid's edge and a body's at its limit; no body's stack is gear (`DeathTests`, FsCheck).

## Deliberately not handled

- A body belongs to a peer id, as an inventory does, and lasts its ten minutes whoever is aboard; one whose
  owner left is still anyone's to pick up.
- A body inside a cell built over later is pushed out of it as any stack is.
- Who owns items across sessions: an inventory belongs to a peer id, so after a load it goes to
  whoever joins in that slot, and a peer that rejoins mid-session starts under a new id.
- Reach: the sim moves items between a hero and any crate; the client opens only crates within reach.
- Nothing in space collides: a stack let go still out there that drifts back into the hull is pushed
  out of it like any other, into the room behind when one is in reach, not stopped against it.
- A stack pushed out of a wall with space on both sides and no room within `Floor.PushReach` goes
  into space and drifts away: what was delivered to or refunded at a wall standing out in space.
- A stale pick at a shared crate moves whatever is in the slot by the time it applies.
- A drone on its way to a crate that a sort has since rearranged takes what of its item is there, in whichever slots,
  or with none looks for its next job (`Logistics`), as after a move by hand: it is sent for an item, not a slot.
- Who may switch creative mode: anyone; it is a dev panel switch among friends.
- A battery's charge never leaks, in a crate, on the floor or on a belt: only a tool spends it.
- A `Run` landing on a hero already passed out pays one tick before `Crew` stops it: items run before crew in a
  tick, and the client of a hero out sends none of its own; 3 power-ticks, accepted.
- What is left in a battery short of a tick's draw stays there until it is charged: one battery pays a tick whole
  (0053), and a run moves on to the next.
- A run needs no hero: a peer whose hero has not come in yet may run what it carries; nothing a player does sends
  one before its first `Enter`.
- `Drain` in creative mode answers true with no battery worn: nothing is ever taken there, and refusing
  a tool for want of a battery would be the only payment creative mode still asks for.
- The desync fuzz never fills an inventory with refunds, so `items.full` from building is proven by
  scenarios; moves draw it.

- Equipment is new here, with no TypeScript original: its rules are 0047's. A worn item counts toward no
  payment, so a hero can never spend the jetpack on its back on a recipe.

## Differences from TypeScript

- A cost is a list of pairs, not an object: order is the list's, and a fractional amount cannot be
  written, so only a negative one throws.
- Stacks are immutable records; the slots handed out are arrays typed read-only.
- `AfterMoves` takes the `Move` and `Drop` commands themselves and returns the machines touched as a
  list in first-touched order, where TypeScript returns a `Map`.

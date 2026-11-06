# Items

## Files

| File | Purpose |
| --- | --- |
| `Deliveries.cs` | Where deliveries go in a machine (`Receiving`). |
| `Floor.cs` | Stacks on the floor: laid down, moved a tick, merged. |
| `Fuzz.cs` | Commands for the desync suite. |
| `Gains.cs` | `Gain` and the journal of gains. |
| `Items.cs` | The module. |
| `Modules.cs` | Fits a module into a machine (0086). |
| `Stacks.cs` | Slot rules: give, take, move. |
| `Stash.cs` | The bag into crates in reach holding its items. |
| `State.cs` | The saved slice and its records. |
| `Spray.cs` | A kill's loot dealt into pieces. |
| `Stock.cs` | A peer's stock: its own slots, then every crate. |
| `ToolStats.cs` | What a tool works with (0098): damage, pace, draw. |

Every stack of items; creative mode takes nothing (0008).

- **Depends on:** `Grid` (machines, what holds a stack up), `Rooms` (space, open room cells), `Knowledge` (`Obtain`).
- **State** (all saved, as sorted lists; derived only `Gains`): `Inventories`, those changed at least once, by peer;
  `Crates`, machines holding something, by id; `Creative`; `Floor` (`FloorStack`, id order) and `NextFloorId`; `Ticks`;
  `Kept` (0062); `Running`, one `Runner` a peer (0055); `Fitted`, by machine then module (0086), saved only when set.
- **Start:** all empty; `Kit` gives `Inventory.Kit` (the screwdriver; 0107) to a peer's first hero.

## Rules

- **Slots:** an inventory is `Inventory.Slots`, then one equipment slot per `Inventory.Equipment` kind (0047); `SlotOpen`
  says which are open (upgrades). A `Stack` is `Item`, `Count`, `Charge`, `Module`. The first row is the hotbar.
- **Per-item values** (0053, 0098): `Charge` (thousandths of a power-tick) and `Module` (a module on a
  tool among its `Hosts`) live on a stack of one and go with it, the floor included. Stacks merge only
  when `Alike` (item, charge, module); a whole stack onto one not alike swaps. Stacks made from a cost hold none, and no
  cost names an item with a capacity or a drill (`ContentTests`). Saved only when set.
- **A tool works with its stack** (0098): `ToolStack(peer, item)` is the first carried, a fitted one first.
  `ToolStats.Of(stack)`, shared with the client, folds the modules' trades (`Boost.Plus`, 0100): damage a gain rounded
  down, draw a cost rounded up, pace the weapon's. `Damage(peer, tool)` is that stack's, the weapon's own if none.
- **Equipment** holds only an item whose `Equip` is its kind: `items.notEquipment` (0047). Worn items
  are never taken nor counted (`Totals`). `WornIn(peer, kind)`: what is worn (2026-10-27-suits-are-worn).
- **Obtained** (the-board): `Output`, `Smelt`, `GiveOrDrop`, `Spray`, `Caught`, `DropAt` for `Nobody` count, `GiveWhatFits`
  what it gave; the rest moves or refunds.
- **Gains** (2026-11-06-head-tally): `GiveOrDrop` notes what went in at its cell in the unsaved `Gains`, for the client.
  A grab passes `noted: false`.
- **Give and take** all of a cost or none (0008): giving fills stacks of the item in slot order, then empty
  own slots; taking empties own slots from the last, the hotbar last. `Spawn` (creative) gives what
  fits, batteries full. `TakeStock`/`GiveStock`: the same over own slots, then each crate (craft-from-crates).
- **Move** (0008) takes the least of `Count`, what `Src` holds and what fits, onto an empty slot or a stack alike; part
  of a stack onto one not alike is refused. With no `Dst` slot, that side as a whole. A machine with `Inputs` takes items
  only into its first `Inputs` slots (`items.takeOnly`, 0057); one with none takes nothing. A charger holds only
  items with a `Capacity` (`items.notBattery`, 0053); an arc bench's vise only a tool a module fits (`items.notTool`,
  0099); one that `Takes` an item only it; a `SlotTakes` slot (a bay's fuel) only with its item, checked first, deliveries
  too (`items.notTaken`).
- **Sort** (0008): the bag past the hotbar, or every slot of a `crate` (`items.notCrate` for other machines). Alike
  stacks merge, then lay by `ItemGroup`, id, module, charge (fullest first), count; hotbar and equipment stay.
- **Stash** (2026-11-06-quick-stash): bag stacks not in `Keep` move into each `Container` within `StashReach` of
  `Cell`, through walls, that holds the item, nearest then by id.
- **Charge** (0053): `Power` fills it through `Charge`, never past full; `Drain(peer, amount)` takes it whole from the
  first belt battery holding it; creative takes nothing.
- **Running an electric tool** (0055): `Run(Item, On)`; a hero runs one thing. Each tick, each
  run in peer order draws its stack's boosted `Draw` through `Drain`; one its belt cannot pay or its peer no longer
  carries stops. `Crew` stops a run as its hero passes out or leaves (`StopRunning`).
- **Modules** (0086): `Fit` takes a module out of every slot into one of its `Hosts`, one of each kind a machine;
  `Unfit` returns it. `Boost(machine)` sums their trades in thousandths over 1000 (`Gain` rounds down, `Cost` up).
  At an arc bench (0099) `Fit`/`Unfit` act on the vise's tool stack; its `Fitted` stays empty.
- **Inputs remember** their last item (`KeptAt`) till a hand empties one (0062).
- **Deliveries** (0062): a fuel only into a `FuelSlot`, else inputs, a crate's all slots, a charger only what
  has a capacity, a `Takes` only its item. Outputs are the slots past the inputs (0057).
- **Surfaces** (0076): `Depart`/`Arrive` (`ICarries`) carry a peer's inventory, charge and run to the next surface;
  the rest stays. Creative mode is a surface's (`Inherit`).
- **The floor**: a drop is thrown the way its hero faces. Each tick, by id: a stack
  inside anything built is pushed out breadth-first through built cells to `PushReach`, a room's open cell
  first, ties by index, else rises a cell. Afloat it flies on, drifting only outside with no back wall; else it falls by gravity times its surface's `Fall` and slides to rest. Gone past the grid's edge or `Lasts`
  after a drop. Alike stacks resting in a cell merge, the later time kept.
  `Spray` (2026-10-29-kill-feel): a kill's loot, a unit a piece up to `SprayMost`, thrown away from the striker.
- **Machines and ladders hold no stack**: one falls through. A stack is a point on a cell's top, never climbing (0106).
- **Picking up** gives as much as fits; the dropper waits `OwnPickupDelay` ticks; a drone's `Lift` takes one whole.
- **Bodies** (0061): `LeaveBody(peer, cell)` drops every non-`Gear` stack of the peer's own slots, still, by
  `FloorStack.Nobody`, tagged `Body(Peer, Died)`. Gear and worn items stay; creative mode leaves none. Each is gone
  `BodyLasts` after death; stacks merge only within one body or outside any.

## Commands

`Move` (`items.noCrate`, `noSlot`, `takeOnly`, `notEquipment`, `notBattery`, `notTool`, `notTaken`, `empty`, `full`); `Drop`
(`noCrate`, `noSlot`, `empty`, `noCell`); `PickUp` (`gone`, `tooSoon`, `full`); `Sort` (`noCrate`, `notCrate`); `Run`
(`notElectric`; on: `notHeld`, `flat`); `Fit` (`noMachine`, `noCrate`, `noSlot`, `empty`, `notModule`, `fitted`,
`noTool`); `Unfit` (`notFitted`, `noTool`, `noSlot`, `full`); `SetCreative`; `Stash`, never refused; `Spawn` (`notCreative`, `full`). A negative
cost throws.

## Invariants

- Stacks hold 1 to their limit; equipment only its kind (`EquipmentTests`). A machine has an entry only while it holds.
- Take and give change totals by exactly the cost or not at all; move, drop, pick-up, sort and stash change none (`StashTests`); a sort is
  idempotent (`SortTests`). Charge stays within capacity on stacks of one (`ChargeTests`); a run draws exactly its
  `Draw` or stops (`RunTests`). Fits and deaths make and lose nothing (`ArcModuleTests`, `DeathTests`).

## Deliberately not handled

- An inventory or body is a peer id's: after a load, whoever joins in it; a body outlasts its owner.
- Reach is the client's, a stash's `Cell` too. A stale pick, or a drone after a sort, takes what is there.
- A stack pushed out of a wall in space with no room in reach drifts away.
- A module in a machine gone but by `Building` stays in `Fitted`: no player causes it.
- A fit shows once its tick runs (0014). Of two drills the fitted works (0098).
- Charge never leaks; short of a tick's draw, it waits (0053).

# Items

## Files

| File | Purpose |
| --- | --- |
| `Deliveries.cs` | Part of `ItemsModule`: how much of `item` machine `id` holds where a delivery of it goes (`Receiving`): its fuel slot for a fuel, its … |
| `Floor.cs` | Stacks on the floor: laid down, moved a tick, merged. |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `Items.cs` | Every stack of items: inventories, what machines hold, the floor, creative mode. |
| `Modules.cs` | Fits the module in `Module`, a slot of the sender's or of a machine, into machine `Machine` (0086): one of each … |
| `Stacks.cs` | The rules on slots: giving, taking and moving. |
| `State.cs` | The saved slice and its records: `Stack`, `Place`, `Target`, `Body`, `FloorStack`, … |
| `ToolStats.cs` | What a tool works with (0098): its damage a hit, the ticks between its hits and what it draws a tick it runs … |

Every stack of items: each hero's inventory, by peer id; what each machine holds, by id; the
stacks on the floor; and creative mode, where nothing is ever taken (0008).

- **Depends on:** `Grid` (which machines hold items, what holds a stack up), `Rooms` (space, open cells in a
  room).
- **State** (all saved, as sorted lists; nothing derived): `Inventories`, those changed at least once, by peer;
  `Crates`, machines holding something, by id; `Creative`; `Floor` (`FloorStack`, id order) and `NextFloorId`; `Ticks`;
  `Kept` (0062); `Running`, one `Runner` a peer (0055); `Fitted`, by machine then module (0086), saved only when set.
- **Start:** every inventory and machine empty; `Kit` gives `Inventory.Kit` (the screwdriver; 0107)
  to a peer's first hero.

## Rules

- **Slots:** an inventory is `Inventory.Slots` of its own, then one equipment slot per `Inventory.Equipment` kind
  (0047); a machine has its `Slots` in `Machines`. A `Stack` is `Item`, `Count` (1 to the item's limit), `Charge`,
  `Module`. The first row is the hotbar.
- **Per-item values** (0053, 0098): `Charge` (0 to `Capacity`, thousandths of a power-tick) and `Module` (a module on a
  tool among its `Hosts`) live on a stack of one and go wherever the stack goes, the floor included. Stacks merge only
  when `Alike` (item, charge, module); a whole stack onto one not alike swaps. Stacks made from a cost hold none, and no
  cost names an item with a capacity or a drill (`ContentTests`). Saved only when set.
- **A tool works with its stack** (0098): `ToolStack(peer, item)` is the first carried, a fitted one first.
  `ToolStats.Of(stack)`, shared with the client, folds the modules' trades (`Boost.Plus`, 0100): damage a gain rounded
  down, draw a cost rounded up, pace the weapon's. `Damage(peer, tool)` is that stack's, the weapon's own if none.
- **Equipment** holds only an item whose `Equip` is its kind: `items.notEquipment` (`Stacks.Misfit`, 0047). Worn items
  are never taken nor counted (`Totals`).
- **Give and take** all of a cost or none (0008; order no ADR): giving fills stacks of the item in slot order, then empty
  own slots; taking empties own slots from the last, so the hotbar goes last. `Spawn` (creative) gives what
  fits, a battery full.
- **Move** (0008) takes the least of `Count`, what `Src` holds and what fits, onto an empty slot or a stack alike; part
  of a stack onto one not alike is refused. With no `Dst` slot, that side as a whole. A machine with `Inputs` takes items
  only into its first `Inputs` slots (`items.takeOnly`, 0057); one with none takes nothing. A charging station holds
  only items with a `Capacity` (`items.notBattery`, 0053); an arc bench's vise only a tool a module fits
  (`items.notTool`, 0099); one that `Takes` an item only it (`items.notTaken`).
- **Sort** (0008): the bag past the hotbar, or every slot of a `crate` (`items.notCrate` for other machines). Alike
  stacks merge, then lay by `ItemGroup`, id, module, charge (fullest first), count; hotbar and equipment stay.
- **Charge** (0053): `Power` fills it through `Charge`, never past full; `Drain(peer, amount)` takes it whole from the
  first belt battery holding that much, so the belt drains left to right. Creative mode takes nothing, answers true.
- **Running an electric tool** (0055): `Run(Item, On)`; a hero runs one thing. Each tick, each
  run in peer order draws its stack's boosted `Draw` through `Drain`; one its belt cannot pay or its peer no longer
  carries stops itself. `Crew` stops a run as its hero passes out or leaves (`StopRunning`).
- **Modules** (0086): `Fit` takes a module out of every slot into one of its `Hosts`, one of each kind a machine;
  `Unfit` returns it. `Boost(machine)` sums their trades in thousandths over 1000 (`Gain` rounds down, `Cost` up).
  At an arc bench (0099) `Fit`/`Unfit` act on the vise's tool stack; its `Fitted` stays empty.
- **Inputs remember** their last item through machine writes (`KeptAt`); a hand emptying one forgets (0062).
- **Deliveries** (0062): a fuel only into a `FuelSlot`, else inputs, a crate's all slots, a charging station only
  what has a capacity, a `Takes` only its item. Outputs are the slots past the inputs (0057).
- **Surfaces** (0076): `Depart`/`Arrive` (`ICarries`) carry a peer's inventory, charge and run to the next surface;
  floor stacks, bodies and machines stay. Creative mode is a surface's, copied by `Inherit`.
- **The floor** (no ADR; numbers in `Floor`): a drop is thrown the way its hero faces. Each tick, in id order: a stack
  inside anything built is pushed out breadth-first through built cells up to `PushReach`, a room's open cell before
  space, ties by lowest index, else rises a cell. Afloat it flies on, drifting only with no back wall;
  elsewhere it falls by gravity times its surface's `Fall` and slides to a stop. Gone past the grid's edge. Alike stacks
  at rest in a cell merge.
- **Machines and ladders hold no stack** (no ADR): one falls through to the floor; one laid in their cells stays.
- **Whole cells** (0106): a stack is a point; it lands on a cell's top and stops against a built cell, never climbing.
- **Picking up** gives as much as fits; the dropper waits `Floor.OwnPickupDelay` ticks (no ADR).
- **Bodies** (0061): `LeaveBody(peer, cell)` drops every non-`Gear` stack of the peer's own slots, still, by
  `FloorStack.Nobody`, tagged `Body(Peer, Died)`. Gear and worn items stay; creative mode leaves none. Each is gone
  `Floor.BodyLasts` after death; stacks merge only within one body or outside any.

## Commands

`Move` (`items.noCrate`, `noSlot`, `takeOnly`, `notEquipment`, `notBattery`, `notTool`, `notTaken`, `empty`, `full`); `Drop`
(`noCrate`, `noSlot`, `empty`, `noCell`); `PickUp` (`gone`, `tooSoon`, `full`); `Sort` (`noCrate`, `notCrate`); `Run`
(`notElectric`; on: `notHeld`, `flat`); `Fit` (`noMachine`, `noCrate`, `noSlot`, `empty`, `notModule`, `fitted`,
`noTool`); `Unfit` (`notFitted`, `noTool`, `noSlot`, `full`); `SetCreative`; `Spawn` (`notCreative`, `full`). A
negative cost throws.

## Invariants

- Stacks hold 1 to their limit; equipment only its kind (`EquipmentTests`). A machine has an entry only while it holds.
- Take and give change totals by exactly the cost or not at all; move, drop, pick-up and sort change none; a sort is
  idempotent (`SortTests`). Charge stays within capacity on stacks of one (`ChargeTests`); a run draws exactly its
  `Draw` or stops (`RunTests`). Fits and deaths make and lose nothing (`ArcModuleTests`, `DrillArcTests`, `DeathTests`).

## Deliberately not handled

- Ownership across sessions: an inventory or body is a peer id's, after a load whoever joins in that slot. A body
  outlasts its owner leaving and is anyone's.
- Reach is the client's. A stale pick, or a drone after a sort, takes what is there then.
- A stack pushed out of a wall in space with no room in reach drifts away.
- Anyone may switch creative mode (dev panel).
- A module in a machine gone other than by `Building` would stay in `Fitted`: nothing a player does causes it.
- A fit shows once its tick runs (0014). Of two drills carried the fitted one works (0098).
- Charge never leaks; short of a tick's draw, it waits to be charged (0053).
- A `Run` landing on a hero already out pays one tick before `Crew` stops it.

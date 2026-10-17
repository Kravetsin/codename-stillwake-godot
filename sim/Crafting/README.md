# Crafting

Stations ([0057](../../docs/decisions/0057-stations-one-model-for-every-crafting-machine.md)): every
crafting machine on one model. A **queued** station (the lifepod's Fabricator, the Former) makes crafts
from a queue shared by the crew, paid when queued, one unit at a time into its outputs
([0028](../../docs/decisions/0028-the-lifepod-and-crafting-by-machine-only.md)); a **fed** station (the
furnace, the electric smelter) smelts what its input holds into that item's yields, where it is hot
enough. A module of its own rather than part of `Items`: it asks `Research` what is known, and research
depends on items.

- **Depends on:** `Grid` (`IGridQueries`: `Machines`, `MachineById`: which machines are stations),
  `Items` (`IItemsQueries`: `Crate`, `CanSmelt`, `ShortOf`, `FullFor`, `OutputFull`; `IItemsOps`: `Take`,
  `Give`, `Output`, `Smelt`), `Research` (`IResearchQueries.Knows`), `Power` (`IPowerQueries.Satisfaction`,
  a powered station's share; `IPowerOps.Rest`, an idle one draws nothing).
- **Depended on by:** `Building` (`ICraftingQueries.Queue`: a station with crafts queued is not taken
  apart), `Furnace` (`ICraftingQueries.CanSmelt`, `TooCold`, `Progress`: what lights a furnace's fire and
  what its panel shows; `ICraftingOps.Heat`: a furnace's fire burns this tick).
- **State** (`CraftingState`, members internal): `Queues` — each queued station's ever crafted at, in
  machine id order: `Machine`, `Next` (the id the next entry gets), `Progress` (work on the first entry's
  unit, in thousandths of a tick at full speed) and `Entries`, each an `Entry(Id, Recipe, Count, By)` in the
  order queued, `Count` the units left, the one at work included, `By` the peer that queued it. A queue
  that empties is kept, so its ids keep counting. `Smelts` — each fed station part-way through what its
  input holds, `Smelt(Machine, Item, Progress)` in machine id order, its work in thousandths of a tick.
  Both saved. Derived: `Heated`, the fed stations whose fire burns this tick, told by `Furnace` in the
  construction phase and cleared once crafting has worked, so empty between ticks; never saved.
- **Files:** `Crafting.cs` — the module, its commands, system and interfaces; `State.cs` — its slice,
  `Entry` and `Smelt`; `Fuzz.cs` — its fuzz generator.
- **Runs** after `Research` and before `Building` in `Registry.All`.

## Rules

- **A station** is a machine with an entry in `Content.Stations`: its `Speed` in thousandths of full
  speed, its `QueueMost`, its `Rows` (what a queued one makes, as its panel lays them out) and its `Heat`
  (a fed one's; above 0 makes it fed). `StationOf` is the one place these are read, so a machine upgrade
  folds in there. Its slots are its machine's (`MachineSpec.Slots`, `Inputs`), read by `Items`.
- **Energy** is the machine's: one that `Draws` works at its network's satisfaction; one with a `FuelSlot`
  at full while its fire burns (`Heat`) and not at all while it is out; one with neither (the pod) at full.
  **Work a tick** is `Speed × share / 1000`, rounded down. Stations work in the simulation phase, after
  power's derivation, in machine id order.
- **A queued station:** a craft is named by the item it makes and costs, a unit, what `Content.Crafts`
  says; a unit takes its `Ticks` × 1000 of work. Crafts come in layers: parts of materials (wire, plate,
  gear, tube), components of parts (rotor, frame, coil), and products, tools as builds, of either (0046).
  Electronics need circuits besides, salvaged from cable racks until silicon makes them.
  - The pod is the most basic bench, slow on purpose (speed 200, a fifth): a wire takes 5 s there, a plate
    10, a rotor 15, a frame 20, the harpoon 30, while the hero is out salvaging. It needs no power and no air.
  - **The Former** is the first powered station: 2×2, it draws 3 and works at speed 1000, five times the
    pod, with a queue of eight; it makes the parts and components (wire, plate, gear, tube, frame, coil,
    rotor), not tools. It costs 2 frames, a rotor, 2 gears and a circuit, so it opens once those four are
    analysed (0027). A plate takes 2 s at full power, about 2.8 s at 70%. Idle, it rests and draws nothing.
  - `Craft(Machine, Recipe, Count)` takes the cost of all `Count` units from the sender at once (nothing in
    creative mode) and adds an entry, by the sender, at the end of that station's queue, with the queue's
    next id. Refused with `crafting.noStation` (no station by that id), `crafting.unknown` (no craft makes
    it, or it is not known), `crafting.notHere` (not in this station's rows, or the station is fed),
    `crafting.full` (the queue holds the station's `QueueMost` entries, whoever queued them) or
    `items.notEnough`; refused whole, nothing is queued.
  - `Cancel(Machine, Entry)` takes the entry of that id off the station's queue, whoever queued it, and
    gives the sender the cost of its units left, the one at work included, as content prices them now.
    Refused with `crafting.noStation`, `crafting.notHere` (a fed station has no queue),
    `crafting.noEntry` (no such entry: never queued, or finished) or `items.full` (the refund does not
    fit). The first entry cancelled, the next starts from nothing.
  - Each tick the first entry adds its work, at most its craft's; at its work one unit goes into the
    station's outputs (`IItemsOps.Output`: its slots past its inputs, all three of the pod's) and the next
    starts. With no room there the unit waits, its progress at its work, until a slot is taken from. The
    queue runs whoever is near.
  - **Idle** — nothing queued, or a unit done waiting for room — a station that draws power is told to
    rest (`IPowerOps.Rest`, as research rests an analyzer), so it draws nothing from the next tick; told to
    work again on the tick it has work, it works at once at its network's share.
- **A fed station:** its input slot is its queue. Each tick, work on another item than its input holds,
  or none, is dropped; then, if its input holds an item with `Yields` whose `Heat` is at most the
  station's and whose yields fit its outputs (`IItemsQueries.CanSmelt`), it adds its work, and at the
  item's `SmeltTicks` × 1000 one of it becomes its yields (`IItemsOps.Smelt`). Work stopped — the fire out,
  the outputs full, the power cut — is kept while the item stays. The furnace: speed 1000, heat 1; a scrap
  chunk smelts in 60 ticks of fire into 4 scrap, a hull plate in 100 into 2 iron and 1 scrap, rock in 160
  into 2 silicon.
  - **The electric smelter** is the second fed station: 2×2, it draws 4 and works at speed 2000, twice the
    furnace, at heat 2; an input and three outputs, no fuel. It costs a frame, 2 coils, 4 plates and a
    circuit, so it opens once those four are analysed. Rock takes 4 s there at full power; silicon, which
    needs heat 2, smelts into glass in 3 s, there only. Like a queued station that draws, it rests while it
    has nothing it can smelt or no room for what it makes, drawing nothing from the next tick.
  - **Heat** is the station's `Heat`, read in `StationOf` alone, so a machine upgrade folds in there.
    **Too cold:** an item with `Yields` hotter than the station is taken into its input (`Items` refuses
    nothing for it) and kept there, never worked, a furnace never lit for it: `TooCold(machine)` says so,
    for the station's panel and its mark in the world. Replaced by what it can work, it works.
- **A furnace smelts while its fire burns at the tick's start:** `Furnace` tells crafting in its
  construction phase, from the fire as the last tick left it. So a furnace starts smelting the tick after
  it lights, and smelts on the tick its fire goes out for want of air; a fire fed in time relights within
  its tick and never misses one (0057).

## API

- **Queries** (`ICraftingQueries`): `StationOf(machine)` — its station as it works now, null for none;
  `Queue(machine)` — a queued station's entries, live and read-only, the first at work; `Progress(machine)`
  — work on its unit in thousandths of a tick, a queued one's first entry (its full work while that unit
  waits for room), a fed one's input; `CanSmelt(machine)` — whether a fed station has what it can smelt;
  `TooCold(machine)` — whether a fed station's input holds what smelts, but hotter than the station.
  On the module only, for the client: `Works(machine)` — whether a station works now with a share of its
  energy, a queued one on a unit not done, a fed one on what it can smelt: what its picture's motion, its
  screen and its bolt show. A furnace's fire reaches crafting only within a tick, so between ticks a
  furnace reads as not working here; its fire is the furnace module's to show.
- **Operations** (`ICraftingOps`): `Heat(machine)` — that fed station's fire burns this tick.
- **Commands:** `Craft(Machine, Recipe, Count)`, `Cancel(Machine, Entry)`.
- **Fuzz:** mostly at the map's stations, queued and fed alike (a fed one, the furnace or the smelter,
  refuses both as not here), now and then at another machine or an id that is none (no station); crafts of every recipe and of an item
  no craft makes, one to nine units, refused as unknown, not here, unpaid or full, paid mostly in creative
  mode, which the items fuzz switches on now and then; cancels of the station's entries, of finished ids
  and of ids never given.

## Invariants

- A queue never holds more than its station's `QueueMost` entries; its first entry's progress stays
  below its work but while its unit waits for room; ids within a queue only grow.
- Items are conserved across both feeds: what a craft took comes back as its units in the outputs or a
  cancel's refund, whoever cancels, and a smelt turns one item into its yields and nothing else
  (`CraftingTests`, `StationsTests`, counting every item by the salvage it is made of).
- Every item is reachable from the wreck: salvage, or made at a reachable station from reachable items
  (`ContentTests`).

## Deliberately not handled

- A count below one: the client sends one or five, and peers run one build.
- A finished unit is never dropped at anyone's feet: it waits in the station, so crafting needs no
  `crew`.
- A queue paid by one and cancelled by another refunds the canceller: friends share a ship, and no
  record of who paid is kept. `By` only marks whose order an entry is.
- In creative mode a cancel gives back what was never taken, as taking apart does.
- The same recipe queued twice is two entries, each cancelled alone, as Rust shows it.
- A queued station taken apart with crafts queued: `Building` refuses it (`building.notEmpty`); the pod is
  `Fixed`. An empty queue left by a station taken apart stays in the slice: machine ids are never reused.
- A smelt's work past the item's on its last tick is dropped, not carried to the next item.
- Nothing stops a too-hot item going into a cold station: the player sees the station cold and moves it
  (the user's decision, 0057). Only one item is too hot for anything yet, silicon for the furnace.
- No machine upgrades: heat and speed are content's alone until modules come.

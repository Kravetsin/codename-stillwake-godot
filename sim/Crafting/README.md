# Crafting

## Files

| File | Purpose |
| --- | --- |
| `Crafting.cs` | Stations (0057): every crafting machine on one model, its work counted in thousandths of a tick at its speed times … |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `State.cs` | The saved slice and its records: `Entry`, `Smelt`, `CraftingState`. |

Stations ([0057](../../docs/decisions/0057-stations-one-model-for-every-crafting-machine.md)): every
crafting machine on one model. A **queued** station (the lifepod's Fabricator, the Former, the circuit
assembler) makes crafts
from a queue shared by the crew, one unit at a time into its outputs, each entry paid when queued or, at the Former
and the assembler, fed from their input slots a unit at a time
([0028](../../docs/decisions/0028-the-lifepod-and-crafting-by-machine-only.md)); a **fed** station (the
furnace, the electric smelter) smelts what its input holds into that item's yields, where it is hot
enough, and a grow capsule grows a seed into biomass the same way: each fed station has a process, "smelt" or
"grow" ([0060](../../docs/decisions/0060-the-farm-a-grow-capsule-is-a-fed-station.md)). A module of its own
rather than part of `Items`: it asks `Research` what is known, and research depends on items.

- **Depends on:** `Grid` (`IGridQueries`: `Machines`, `MachineById`: which machines are stations),
  `Items` (`IItemsQueries`: `Crate`, `CanSmelt`, `ShortOf`, `FullFor`, `OutputFull`, `InputsLack`, `Boost`, what the
  modules fitted into a station add; `IItemsOps`:
  `Take`, `Give`, `Output`, `Smelt`, `TakeInputs`), `Research` (`IResearchQueries.Knows`), `Power` (`IPowerQueries.Satisfaction`,
  a powered station's share; `IPowerOps.Rest`, an idle one draws nothing); the world's `Rng`, for a crop's seeds.
- **Depended on by:** `Building` (`ICraftingQueries.Queue`: a station with crafts queued is not taken
  apart), `Furnace` (`ICraftingQueries.CanSmelt`, `TooCold`, `WorkOf`, `Progress`: what lights a furnace's fire
  and what its panel shows; `ICraftingOps.Heat`: a furnace's fire burns this tick).
- **State** (`CraftingState`, members internal): `Queues` — each queued station's ever crafted at, in
  machine id order: `Machine`, `Next` (the id the next entry gets), `Progress` (work on the first entry's
  unit, in thousandths of a tick at full speed) and `Entries`, each an `Entry(Id, Recipe, Count, By, FromInputs,
  Endless)` in the order queued, `Count` the units left, the one at work included, `By` the peer that queued it,
  `FromInputs` whether it is fed from the inputs, `Endless` whether it repeats; the last two saved only when set. A queue
  that empties is kept, so its ids keep counting. `Smelts` — each fed station part-way through what its
  input holds, `Smelt(Machine, Item, Progress)` in machine id order, its work in thousandths of a tick.
  Both saved. Derived: `Heated`, the fed stations whose fire burns this tick, told by `Furnace` in the
  construction phase and cleared once crafting has worked, so empty between ticks; never saved.
- **Files:** `Crafting.cs` — the module, its commands, system and interfaces; `State.cs` — its slice,
  `Entry` and `Smelt`; `Fuzz.cs` — its fuzz generator.
- **Runs** after `Research` and before `Building` in `Registry.All`.

## Rules

- **A station** is a machine with an entry in `Content.Stations`: its `Speed` in thousandths of full
  speed, its `QueueMost`, its `Rows` (what a queued one makes, as its panel lays them out), its `Heat`
  (a fed one's; above 0 makes it fed) and its `Process` (a fed one's, "smelt" or "grow"). `StationOf`, an instance
  method, is the one place these are read, so a machine upgrade folds in there: **the modules fitted into it**
  (`IItemsQueries.Boost`, [0086](../../docs/decisions/0086-modules-one-socket-for-machines-and-tools.md)) make its speed
  `Speed × (1000 + their Speed) / 1000`, rounded down, a gain: the arc module doubles it. A fed one's heat gains theirs
  (no module adds any yet: the crucible to come); a queued one's stays 0, which says it is not fed. The pod and the
  furnace take no module. Its slots are its machine's (`MachineSpec.Slots`, `Inputs`), read by `Items`.
- **Energy** is the machine's: one that `Draws` works at its network's satisfaction; one with a `FuelSlot`
  at full while its fire burns (`Heat`) and not at all while it is out; one with neither (the pod) at full.
  **Work a tick** is `Speed × share / 1000`, rounded down. Stations work in the simulation phase, after
  power's derivation, in machine id order.
- **A queued station:** a craft is named by the item it makes and costs, a unit, what `Content.Crafts`
  says; a unit takes its `Ticks` × 1000 of work. Crafts come in layers: parts of materials (wire, plate,
  gear, tube), components of parts (rotor, frame, coil), and products, tools as builds, of either (0046).
  Electronics need circuits besides, salvaged from cable racks and derelicts, or made at the circuit assembler.
  - The pod is the most basic bench, slow on purpose (speed 200, a fifth): a wire takes 5 s there, a plate
    10, a rotor 15, a frame 20, the harpoon 30, while the hero is out salvaging. It needs no power and no air.
  - **The Former** is the first powered station: 4×4, it draws 3 and works at speed 1000, five times the
    pod, with a queue of eight; it makes the parts and components (wire, plate, gear, tube, frame, coil,
    rotor), not tools; and alone, soil (0060) and the motor, a rotor, 2 of a planet's copper and a magnetite in 120 ticks (0080). It costs 2 frames, a rotor, 2 gears and a circuit, so it opens once those four are
    analysed (0027). A plate takes 2 s at full power, about 2.8 s at 70%. Idle, it rests and draws nothing.
  - **The circuit assembler** makes the electronics (no longer the arc module, 0099): 4×4, it draws 2 and works at speed 1000, a queue of eight;
    a circuit of a silicon and 2 wire in 80 ticks, a solar cell of a silicon, a glass and a wire in 100, and on a
    row of its own a drone of a rotor, 2 plates, a circuit and a battery in 160 (0062; a drone holds charge of its own,
    made empty, and the battery's is lost there). It
    costs a frame, a rotor, a glass, 2 wire and a circuit, salvaged, so it opens once those five are analysed.
    Idle, it rests as the Former does. The pod and the Former make neither; the pod's list stops growing
    (0057).
  - **The arc bench** ([0099](../../docs/decisions/0099-the-arc-bench.md)) is the arc branch's station: every craft of a sky
    shard, the arc module (moved from the assembler, same cost) and the accumulator (a shard, a battery, 2 copper and a
    circuit in 200 ticks: three batteries' charge). 4×2, no power, speed 1000, a queue of four, **paid by hand only**: its
    one input slot is its vise, which holds a tool (`Items`), so a craft from the inputs is refused `crafting.noInputs`, as
    at the pod. Its two outputs follow the vise. It costs a shard, a frame, 2 plates and a gear, so analysing a shard opens
    it with the arc module (0027).
  - **Inputs** (0057): the Former has 3 input slots and the assembler 4, before their 3 outputs: as many as the
    most distinct items any of their crafts needs (a motor's rotor, copper and magnetite; a drone's rotor, plates, circuit
    and battery, `ContentTests`; the assembler had 3, a solar cell's, until the drone, 0062; the Former 2, a rotor's, until
    the motor, 0080, save format 23). The pod has none, paid by hand by design. Items go in by hand as into any machine with inputs
    (`Items`: a move naming no slot into the first; the outputs only taken from).
  - `Craft(Machine, Recipe, Count, FromInputs)`, **paid** (the default), takes the cost of all `Count` units from the
    sender at once (nothing in creative mode); **fed from the inputs** it takes nothing and is never refused as
    unpaid. Either adds an entry, by the sender, at the end of that station's queue, with the queue's next id.
    Refused with `crafting.noStation` (no station by that id), `crafting.unknown` (no craft makes it, or it is not
    known), `crafting.notHere` (not in this station's rows, or the station is fed), `crafting.full` (the queue
    holds the station's `QueueMost` entries, whoever queued them), `items.notEnough` (paid) or `crafting.noInputs`
    (fed from the inputs at the pod); refused whole, nothing is queued.
  - `Cancel(Machine, Entry)` takes the entry of that id off the station's queue, whoever queued it, and
    gives the sender what was paid for it and not made, as content prices it now: a paid one's units left, the one
    at work included; one fed from the inputs, its unit begun (the first entry with progress above 0), else
    nothing; what its inputs hold stays there. Refused with `crafting.noStation`, `crafting.notHere` (a fed station
    has no queue), `crafting.noEntry` (no such entry: never queued, or finished) or `items.full` (the refund does
    not fit). The first entry cancelled, the next starts from nothing.
  - `SetEndless(Machine, Entry, On)` makes an entry fed from the inputs **endless** (∞), or not: a unit made leaves
    its count as it is, so it repeats while its inputs last and waits when they run out; a cancel takes it off.
    Set rather than toggled, so a click sent twice before its tick sets it once. Refused with
    `crafting.noStation`, `crafting.notHere`, `crafting.noEntry` or `crafting.paid` (a paid entry: what it makes
    is paid for).
  - Each tick the first entry adds its work, at most its craft's; at its work one unit goes into the
    station's outputs (`IItemsOps.Output`: its slots past its inputs, all three of the pod's) and the next
    starts. With no room there the unit waits, its progress at its work, until a slot is taken from. The
    queue runs whoever is near.
  - **A unit fed from the inputs** takes its cost from them (`IItemsOps.TakeInputs`), each item from the first
    input slot first, on its first tick with work above 0, so a station without power takes nothing in; creative
    mode does not make it free, as it does not a smelt. While its inputs lack the cost (`IItemsQueries.InputsLack`)
    the entry **waits** at progress 0, and the whole queue waits behind it, a paid entry too: first in, first out,
    no skipping; cancelling it is the way out. `Lacks(machine)` says what it waits for.
  - **Idle** — nothing queued, a unit done waiting for room, or a first entry waiting for its inputs — a station
    that draws power is told to rest (`IPowerOps.Rest`, as research rests an analyzer), so it draws nothing from
    the next tick; told to work again on the tick it has work, it works at once at its network's share.
- **A fed station:** its input slot is its queue. Each tick, work on another item than its input holds,
  or none, is dropped; then, if its input holds an item with `Yields` of the station's `Process`, whose
  `Heat` is at most the station's and whose yields fit its outputs (`IItemsQueries.CanSmelt`), it adds its
  work, and at the item's `SmeltTicks` × 1000 one of it becomes its yields (`IItemsOps.Smelt`). An item of
  another process sits in the input, never worked, the station resting: a seed in a furnace, rock in a capsule. Work stopped — the fire out,
  the outputs full, the power cut — is kept while the item stays. The furnace: speed 1000, heat 1; a scrap
  chunk smelts in 60 ticks of fire into 4 scrap, a hull plate in 100 into 2 iron and 1 scrap, rock in 160
  into 2 silicon, biomass in 160 into a biofuel, a planet's copper ore in 160 into a copper (an ingot an ore, so a
  vein's 2 ore are 2 copper, a motor's worth with a magnetite).
  - **The electric smelter** is the second fed station: 4×4, it draws 4 and works at speed 2000, twice the
    furnace, at heat 2; an input and three outputs, no fuel. It costs a frame, 2 coils, 4 plates and a
    circuit, so it opens once those four are analysed. Rock takes 4 s there at full power; silicon, which
    needs heat 2, smelts into glass in 3 s, there only. Like a queued station that draws, it rests while it
    has nothing it can smelt or no room for what it makes, drawing nothing from the next tick; told so on the tick
    its last item smelts, as its empty input used to say it.
  - **The grow capsule** is the third fed station, of process "grow" (0060): 2×3, it draws 2 and works at speed
    1000, heat 1; an input and two outputs. It costs a frame, 2 glass, 4 soil, a circuit and 2 wire, so it opens
    once those five are analysed. A seed grows in 9600 ticks, eight minutes, at a full share, slower short of
    power; unpowered it pauses keeping its growth; it rests while it has nothing to grow or no room. It needs no air.
    **A crop** uses its seed and gives 4 biomass and none, one or two seeds: the seed's `Returns` weights (35, 50,
    15), drawn from the world's `Rng` as the crop completes, stations in machine id order (`Returned`), passed to
    `IItemsOps.Smelt(id, back)`. An item without `Returns` draws nothing. It grows only with room in its outputs for
    the yields and the most seeds it may give back, so no draw ever overflows a slot.
  - **Heat** is the station's `Heat`, read in `StationOf` alone, so a module's heat folds in there.
    **Too cold:** an item of its process with `Yields` hotter than the station is taken into its input (`Items`
    refuses nothing for it) and kept there, never worked, a furnace never lit for it: `TooCold(machine)` says so,
    for the station's panel and its mark in the world. Replaced by what it can work, it works.
- **A furnace smelts while its fire burns at the tick's start:** `Furnace` tells crafting in its
  construction phase, from the fire as the last tick left it. So a furnace starts smelting the tick after
  it lights, and smelts on the tick its fire goes out for want of air; a fire fed in time relights within
  its tick and never misses one (0057).

## API

- **Queries** (`ICraftingQueries`): `StationOf(machine)` — its station as it works now, null for none;
  `Queue(machine)` — a queued station's entries, live and read-only, the first at work; `Progress(machine)`
  — work on its unit in thousandths of a tick, a queued one's first entry (its full work while that unit
  waits for room), a fed one's input; `CanSmelt(machine)` — whether a fed station has what it can work;
  `TooCold(machine)` — whether a fed station's input holds what is of its process, but hotter than the station;
  `WorkOf(machine)` — the work one of a fed station's input takes there, in thousandths of a tick, null for
  nothing of its process: what its bar and a capsule's plant are measured against.
  On the module only, for the client: `Works(machine)` — whether a station works now with a share of its
  energy, a queued one on a unit not done and not waiting for its inputs, a fed one on what it can smelt: what its
  picture's motion, its screen and its bolt show; `Lacks(machine)` — what a queued station's inputs lack for its
  first entry's next unit while that entry, fed from them, waits: what dims it in the queue and its tip names. A furnace's fire reaches crafting only within a tick, so between ticks a
  furnace reads as not working here; its fire is the furnace module's to show. Static: `Processes(station, item)` —
  whether a fed station processes an item at all, room aside: of its process and no hotter than it; what the
  client marks in the inventory while one is open, and what `CanSmelt` asks before room.
- **Operations** (`ICraftingOps`): `Heat(machine)` — that fed station's fire burns this tick.
- **Commands:** `Craft(Machine, Recipe, Count, FromInputs)`, `Cancel(Machine, Entry)`, `SetEndless(Machine, Entry, On)`.
- **Fuzz:** mostly at the map's stations, queued and fed alike (a fed one, the furnace, the smelter or the grow
  capsule, refuses them all as not here), now and then at another machine or an id that is none (no station);
  crafts of every recipe and of an item no craft makes, one to nine units, paid or fed from the inputs, refused
  as unknown, not here, unpaid, full or, at the pod, with no inputs, paid mostly in creative mode, which the items
  fuzz switches on now and then; cancels and endless switches of the station's entries (a paid one refusing the
  switch), of finished ids and of ids never given.

## Invariants

- A queue never holds more than its station's `QueueMost` entries; its first entry's progress stays
  below its work but while its unit waits for room; ids within a queue only grow.
- Items are conserved across both feeds, both kinds of entry and both processes: what a craft took, paid when
  queued or a unit at a time from the inputs, endless or not, comes back as its units in the outputs or a cancel's
  refund, whoever cancels, a smelt turns one item into its yields and nothing else, and a
  crop turns a seed into its yields and none, one or two seeds (`CraftingTests`, `StationsTests`, counting every
  item by the salvage it is made of, seeds apart).
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
- A seed in a furnace, or rock in a capsule, is taken in and does nothing: no refusal, no mark; the player moves
  it. Only two processes yet, each item of one.
- A crop is not grown under the stars without power: the capsule is powered only for now (the user's decision).
- First in, first out: a first entry waiting for its inputs holds up a paid one behind it. No skip rule; the player
  cancels it (the user's decision). An endless one at the head holds up the queue for good, likewise.
- What a cancelled entry fed from the inputs left in them stays there, crowding the next entry's items if they
  differ: the player takes it out by hand.
- The pod takes no inputs: a craft from them there is refused (`crafting.noInputs`) rather than left waiting.
- Drones are `Logistics`': they bring a head entry fed from the inputs what it needs, read through `Queue` and
  `Progress` (0062); crafting knows nothing of them.

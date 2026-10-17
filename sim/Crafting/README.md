# Crafting

Making parts by machine only ([0028](../../docs/decisions/0028-the-lifepod-and-crafting-by-machine-only.md)):
each crafting machine's queue, shared by the crew, paid when queued, made one unit at a time into the
machine's outputs. Today the one crafting machine is the lifepod's Fabricator. A module of its own
rather than part of `Items`: it asks `Research` what is known, and research depends on items.

- **Depends on:** `Grid` (`IGridQueries.MachineById`: which machines craft), `Items` (`IItemsQueries`:
  `ShortOf`, `FullFor`, `OutputFull`; `IItemsOps`: `Take`, `Give`, `Output`), `Research`
  (`IResearchQueries.Knows`).
- **State** (`CraftingState`, members internal): `Queues` — each machine's ever crafted at, in machine
  id order: `Machine`, `Next` (the id the next entry gets), `Progress` (work on the first entry's unit,
  in thousandths of a tick at full speed) and `Entries`, each an `Entry(Id, Recipe, Count, By)` in the
  order queued, `Count` the units left, the one at work included, `By` the peer that queued it. A queue
  that empties is kept, so its ids keep counting. Saved; nothing is derived.
- **Files:** `Crafting.cs` — the module, its commands and system; `State.cs` — its slice and
  `Entry`; `Fuzz.cs` — its fuzz generator.
- **Runs** after `Building` in `Registry.All`.

## Rules

- A craft is named by the item it makes and costs, a unit, what `Content.Crafts` says; a unit takes
  its `Ticks` × 1000 of work. Crafts come in layers: parts of materials (wire, plate, gear, tube),
  components of parts (rotor, frame, coil), and products, tools as builds, of either (0046). Electronics need
  circuits besides, salvaged from cable racks until silicon makes them: the battery one, the drill two; the
  charging station, a build, one.
- A machine crafts when it is a lifepod: every craft is the pod's while it is the only crafting
  machine (principle 2). It needs no power and no air.
- The pod is the most basic bench, slow on purpose: a wire takes 5 s there, a plate 10, a rotor 15, a
  frame 20, the harpoon 30, while the hero is out salvaging. Machines for parts (a former and the
  like), faster and with longer queues, are planned
  ([0046](../../docs/decisions/0046-recipes-show-what-a-thing-is-made-of.md)).
- `Craft(Machine, Recipe, Count)` takes the cost of all `Count` units from the sender at once (nothing
  in creative mode) and adds an entry, by the sender, at the end of that machine's queue, with the
  queue's next id. Refused with `crafting.noStation` (no crafting machine by that id),
  `crafting.unknown` (no craft makes it, or it is not known), `crafting.full` (the queue holds
  `Content.Crafting.QueueMost` entries, whoever queued them) or `items.notEnough`; refused whole,
  nothing is queued.
- `Cancel(Machine, Entry)` takes the entry of that id off the machine's queue, whoever queued it, and
  gives the sender the cost of its units left, the one at work included, as content prices them now.
  Refused with `crafting.noStation`, `crafting.noEntry` (no such entry: never queued, or finished) or
  `items.full` (the refund does not fit). The first entry cancelled, the next starts from nothing.
- Each tick, in the simulation phase, in machine order, each queue's first entry adds
  `Content.Crafts.PodSpeed` (200, a fifth of full speed) to its progress, at most its work; at its
  work one unit goes into the machine's outputs (`IItemsOps.Output`: its slots past its inputs, all
  three of the pod's) and the next starts. With no room there the unit waits, its progress at its
  work, counting no more, until a slot is taken from. The queue runs whoever is near.

## API

No module depends on crafting, so it has no query interface: the client reads `Registry.Crafting`.

- **Queries:** `Queue(machine)` — its entries, live and read-only, the first at work;
  `Progress(machine)` — work on its first entry's unit, in thousandths of a tick, its full work while
  that unit waits for room; `Crafts(machine)` — whether that machine crafts.
- **Commands:** `Craft(Machine, Recipe, Count)`, `Cancel(Machine, Entry)`.
- **Fuzz:** mostly at the map's pod, now and then at another machine or an id that is none (no
  station); crafts of every recipe and of an item no craft makes, one to nine units, refused as
  unknown, unpaid or full, paid mostly in creative mode, which the items fuzz switches on now and then;
  cancels of the machine's entries, of finished ids and of ids never given.

## Invariants

- A queue never holds more than `QueueMost` entries; its first entry's progress stays below its work
  but while its unit waits for room; ids within a queue only grow.
- Items are conserved: what a craft took comes back as its units in the outputs or a cancel's refund,
  whoever cancels (`CraftingTests`, counting every item by the raw items it is made of).

## Deliberately not handled

- A count below one: the client sends one or five, and peers run one build.
- A finished unit is never dropped at anyone's feet: it waits in the machine, so crafting needs no
  `crew`.
- A queue paid by one and cancelled by another refunds the canceller: friends share a ship, and no
  record of who paid is kept. `By` only marks whose order an entry is.
- In creative mode a cancel gives back what was never taken, as taking apart does.
- The same recipe queued twice is two entries, each cancelled alone, as Rust shows it.
- A crafting machine that goes: the lifepod is `Fixed`, never taken apart, so its queue never loses
  its machine.

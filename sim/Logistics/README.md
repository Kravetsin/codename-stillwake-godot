# Logistics

Drones ([0062](../../docs/decisions/0062-drones-keep-machines-going.md)): with nothing set, the drones of a bay keep
the machines in its zone going. An input holding an item is refilled with it, a queued station's head entry fed from
its inputs gets what it needs, and what machines make goes to crates; drones fly through air and doors, take job after
job from where they are, and fly home with no work left or their charge low. A machine switched off is left alone.

- **Depends on:** `Grid` (`IGridQueries`: machines, their kinds and cells, door cells), `Atmosphere`
  (`IAtmosphereQueries.AiredAt`: where drones may fly), `Items` (`IItemsQueries`: `Crate`, `HeldFor`, `RoomFor`;
  `IItemsOps`: `TakeFrom`, `PutIn`, `LayDown`), `Crafting` (`ICraftingQueries.Queue`, `Progress`: a queued station's
  head entry and whether its unit has begun). Stations and machines are read from `Content`.
- **State** (`LogisticsState`, members internal): `Off` — the machines switched off, by id, sorted; `Drones` — every
  drone in flight (`Drone`: id, bay, its own `Stack` with its charge, `X`/`Y` in thousandths of a cell, `Cell` it last
  reached the centre of, `To` it flies to, `Leg`, `Low`, and its job: `Source`, `Slot`, `Target`, `Item`, `Count`, with
  its `Cargo`) in id order; `NextId`; `Ticks`, its own clock. All saved. Derived and never saved: reservations, read
  from the drones in flight; the floods and paths, found when needed in scratch arrays sized to one zone box; the
  lists one look fills.
- **Files:** `Logistics.cs` — the module, its command and queries; `Jobs.cs` — what is wanted, matching, and what a
  job costs in charge; `Flight.cs` — looking, launching, flying, the floods and paths; `State.cs` — its slice and types;
  `Fuzz.cs` — its fuzz generator.
- **Runs** last in `Registry.All`, after `Furnace`: drones read the air of the tick.

## Rules

- **What is wanted** (`Wants`), of a served machine in the flood:
  - a fed station (the furnace, the smelter, the grow capsule) or one with a fire (the burner generator): each item its
    inputs hold, its fuel slot included, once, kept at its stack: wanted when what it holds where a delivery goes
    (`HeldFor`) and what drones bring is under half that, the rest of a stack;
  - a queued station with inputs (the Former, the assembler): when its head entry is fed from its inputs, each item of
    its craft's cost × its units not begun (endless: no end), at most a stack: wanted under half that or under one
    unit's cost. A paid head entry, or none, wants nothing;
  - an empty input wants nothing. Crates, bays, the analyzer and the charging station want nothing.
- **What may be taken** (offers): every stack in a served crate in the flood, and every stack in a served machine's
  outputs (its slots past its inputs, the pod's and queued stations' too), that no drone is on its way to take.
- **A job,** from where a drone is: each wanted item from an offer of it in another machine, carrying the least of
  what is wanted, the stack and the room at the target (`RoomFor` a stack); and each output offered to a crate
  (`Storage`): a served one in the flood with room, one already holding the item first, then the nearest to the
  output's machine, then the lowest id, carrying up to that room. Of all of them, the source nearest by Manhattan
  distance between the drone's cell and the source's top-left, then the lowest source id, then slot, a refill before
  an output, then the lowest target id.
- **What a job costs** (`Affords`): `Draw` × ticks a cell × the Manhattan lengths from where the drone is to the source,
  on to the target and home to the bay's top-left, and `Spare` (2) cells; its charge must hold at least that.
- **A bay** (`droneBay`, 2×2, four slots, a charging station drawing 4) looks when `(Ticks + id) % LookEvery == 0`,
  bays in id order, each tick after the drones have flown. Its first slot holding a drone charged to `LaunchAt` floods
  its zone — the cells within `Reach` of its footprint — breadth first from its passable cells through passable cells
  (aired, or a door's, closed too), finds the job from the bay, and launches that drone on it when it affords it.
- **Next:** a drone free in the air — its delivery made, its source found empty, its source or target taken apart —
  floods the zone from its cell and takes the job found from there if it affords it; with none it flies home, and
  with one it cannot afford it flies home marked `Low`.
- **Flight:** each tick a drone pays `Draw` power-ticks from its charge, or drops if it cannot, then moves `Speed`
  thousandths of a cell, cell centre to cell centre. At each centre: in a cell not passable it drops; in its goal (any
  cell of the machine) it arrives; else one breadth-first search from there inside its bay's zone, over neighbours up,
  left, right, down, finds its first step to its goal and its distance home. No way home: it drops. On a job with no
  way to its goal, it flies home with its cargo; holding less than the way home and `Spare` cells, it flies home
  marked `Low`. A cell ahead no longer passable turns it back to the centre it left. A bay gone drops its drones.
- **Arriving:** at its source it takes what the slot holds of its item up to its job, any of it, and with none looks
  for the next job; at its target it puts in what fits and lays the rest down there, then looks for the next job; a
  target taken apart has its cargo laid where the drone is. Home, it docks in its bay's first free slot (or lies down
  where it is with none) and lays any cargo it brought back where it is.
- **Dropping** lays the drone's stack, its charge with it, and its cargo where it is, still, for anyone to pick up.
- **The switch:** `SetServed(Machine, On)`; refused `logistics.noMachine` for an id that is no machine and
  `logistics.bay` for a bay. Switching a machine as it already is changes nothing. A job under way when its machine is
  switched off is finished.

## API

- **Queries:** `Served(machine)` — not switched off; `Servable(machine)` — whether drones ever work a machine of its
  kind, served or not: where the badge shows; `Reached(machine)` — whether some bay's flood reaches it; `Drones` — live
  and read-only, for drawing; `Low(drone)` — flying home marked low; `ZoneOf(bay)` — its zone box, null for no bay.
- **Commands:** `SetServed(Machine, On)`.
- **Fuzz:** the switch mostly at the map's machines, on or off, now and then at a bay or at an id that is none.

## Invariants

- Items are kept, drones included, across machines, the floor, drones in flight and their cargo; each drone is once,
  docked, flying or on the floor; no two drones go for one stack (`LogisticsInvariantTests`, FsCheck, with walls built
  and taken down, the door worked and machines switched mid-flight).
- No drone in flight ever holds no charge, and none drops, however far round walls its jobs take it, while nothing
  changes where drones may fly (`LogisticsInvariantTests`).
- Saved, loaded and played on mid-flight, a world hashes the same (`LogisticsTests`).

## Deliberately not handled

- An input emptied is left alone even once its supply is back: nothing remembers what it held. The player puts one in.
- An output is carried off as soon as a drone is free, a stack of four scrap as soon as of four hundred: while drones
  are busy outputs gather, so carries grow by themselves.
- The pod's outputs go to crates like any machine's; switch the pod off to keep what it makes at hand.
- Distance is Manhattan between top-left cells, not path length; a job's cost is checked against Manhattan lengths,
  and the turn home in flight covers what a detour adds.
- A wall built across a drone's way home after it set out can leave it short: it drops when it cannot pay a tick.
- Room at the target is read as the job is found: two drones bound for one crate may bring more than fits, and the
  rest lies there; cargo a drone turned home with lies at its bay. Neither is taken back to a crate.
- A drone checks the air only at cell centres: one may fly half a cell into a cell vented that tick before turning.
- A wall built on the cell a drone stands in drops it there; its stacks are pushed out as any floor stack is.
- `Off` keeps a machine taken apart: ids are never reused, and it is never read.
- No drones in vacuum, opening doors, colliding or flying outside the hull; reach is fixed.
- A bay's zone is a square box, clipped at the grid's edge.

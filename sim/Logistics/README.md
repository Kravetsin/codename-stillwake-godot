# Logistics

## Files

| File | Purpose |
| --- | --- |
| `Flight.cs` | Part of `LogisticsModule`: a bay's numbers; looking, launching, flying, floods, paths. |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `Jobs.cs` | A job: `Count` of `Item` from `Source` to `Target`; wants, matching, cost. |
| `Logistics.cs` | Drone logistics (0062): the module, its command and queries. |
| `State.cs` | The saved slice and its records: `Leg`, `Drone`, `LogisticsState`. |

Drones ([0062](../../docs/decisions/0062-drones-keep-machines-going.md)): with nothing set, a bay's drones keep the
machines in its zone going: inputs refilled, queued stations fed, outputs carried to crates; they fly through air and
doors, job after job, home with no work or low charge. A machine switched off is left alone. Rules are 0062's
unless marked.

- **Depends on:** `Grid` (machines, door cells), `Atmosphere` (`AiredAt`), `Items` (`Crate`, `HeldFor`, `RoomFor`,
  `KeptAt`; `TakeFrom`, `PutIn`, `LayDown`), `Crafting` (`Queue`, `Progress`); stations from `Content`.
- **State** (`LogisticsState`, all saved): `Off` — machines switched off, sorted; `Drones` — in flight, in id
  order (`Drone`: bay, `Stack` with charge, `X`/`Y` in thousandths of a cell, `Cell` last centred, `To`, `Leg`,
  `Low`, job, `Cargo`); `NextId`; `Ticks`. An old save's drone `slot` is ignored. Derived: reservations, floods
  and paths in scratch arrays sized to one zone box.
- **Runs** last in `Registry.All`, after `Furnace`: drones read the tick's air (no ADR).

## Rules

- **Never the arc bench** (0099): a machine with `Tunes` is neither target nor source (`Takes`, `OutputsOf`), so
  `Servable` says no: its vise and crafts are by hand.
- **What is wanted** (`Wants`), of a served machine in the flood:
  - a fed station (furnace, smelter, grow capsule) or one with a fire (burner generator): each item its inputs
    hold, fuel slot included, or remember (`KeptAt`) once burnt down, kept at its stack: wanted when what it holds
    where a delivery goes (`HeldFor`) plus what drones bring is under half that; the rest of a stack;
  - a queued station with inputs (Former, assembler), when its head entry is fed from inputs: each item of the
    craft's cost × units not begun (endless: no end), at most a stack, wanted under half that or under one unit's
    cost. A paid head entry, or none, wants nothing;
  - an empty input remembering nothing, crates, bays, the analyzer and the charging station want nothing.
- **Offers**, by item: what served crates in the flood and served machines' outputs (slots past inputs, the pod's
  and queued stations' too) hold, less **reservations** by drones coming: at a crate their counts, so drones share
  it; at outputs all of it, so one drone at a time takes them.
- **A job,** from where a drone is: each wanted item from an offer in another machine, the least of want, offer
  and room at the target (`RoomFor` a stack); and each output to a crate (`Storage`): served, in the flood, with
  room; one holding the item first, then nearest the output, then lowest id. Of all: the source nearest (Manhattan,
  drone's cell to its top-left), then lowest source id, first slot holding the item, refill before output, lowest
  target id.
- **A job's cost** (`Affords`): `Draw` × the ticks, rounded up, at the bay's speed over the Manhattan lengths from
  the drone to the source, to the target and home to the bay's top-left, the legs from source and target each less
  its machine's width and height minus two, plus `Spare` cells (0104); its charge must hold that.
- **A bay** (`droneBay`) looks when `(Ticks + id) % LookEvery == 0`, bays in id order, after drones fly. Its first
  slot holding a drone charged to `LaunchAt` floods its zone — cells within `Reach` of its footprint — breadth first
  through passable cells (aired, or a door's, closed too), finds the job from the bay and launches if it affords it.
- **Next:** a drone free in the air (delivered, its source found empty, source or target taken apart) floods from
  its cell and takes the job found if it affords it; with none it flies home; with one it cannot afford, home `Low`.
- **Flight:** each tick a drone pays `Draw` or drops, then moves `Speed` thousandths of a cell, centre to centre.
  At each centre: not passable, it drops; in its goal (any machine cell) it arrives; else one breadth-first search
  in its zone (up, left, right, down) finds its next step and distance home. No way home: drops. No way to the
  goal: home with cargo. Charge under the way home plus `Spare`: home `Low`. A cell ahead turned impassable sends it
  back to the centre it left. A bay gone drops its drones.
- **Arriving:** at its source it takes what its offered slots hold of the item now, up to its job, last slot first,
  as one stack; with none it looks again. At its target it puts in what fits, the rest to a crate with room
  (`Storage`) or laid down there, then looks again; a target gone has the cargo laid where the drone is. Home, it
  docks in the first free slot (or lies down with none) and lays any cargo there.
- **Dropping** lays the drone's stack, charge and all, and its cargo where it is, for anyone.
- **The switch:** `SetServed(Machine, On)`; refused `logistics.noMachine` (no such machine) or `logistics.bay`.
  Switching to the same state changes nothing; a job under way when its machine is switched off is finished.

## API

- **Queries:** `Served(machine)` — not switched off; `Servable(machine)` — whether drones ever work its kind (where
  the badge shows); `Reached(machine)` — some bay's flood reaches it; `Supplied(machine, item)` — a served crate or
  output in the reach of a bay reaching it holds some (a remembered slot's tooltip); `Drones` — live, read-only;
  `Low(drone)`; `ZoneOf(bay)` — its zone box, null for no bay.
- **Commands:** `SetServed(Machine, On)`.
- **Fuzz:** the switch mostly at the map's machines, now and then at a bay or an id that is none.

## Invariants

- Items are kept across machines, floor, drones and cargo; each drone is once, docked, flying or on the floor; a
  crate holds all drones will take from it while only drones take; a machine's outputs of an item have at most one
  drone coming; no input passes a stack (`LogisticsInvariantTests`, walls, doors, switches changed mid-flight).
- No drone in flight holds no charge or drops while nothing changes where drones fly (`LogisticsInvariantTests`).
- Saved, loaded and played on mid-flight, a world hashes the same (`LogisticsTests`).

## Deliberately not handled

- An input forgets its item only when a hand takes all of it out, or the machine is switched off.
- An output is carried off as soon as a drone is free, however small: while drones are busy, carries grow.
- The pod's outputs go to crates; switch the pod off to keep what it makes at hand.
- Distance is Manhattan between top-left cells, not path length; the turn home covers what a detour adds.
- A wall built across a drone's way home can leave it short: it drops when it cannot pay a tick.
- Room at the target is read as the job is found: two drones may bring more than fits; the rest goes to another
  crate, or lies there with none. Cargo a drone turned home with lies at its bay.
- Air is checked only at cell centres: a drone may fly half a cell into a cell vented that tick.
- A wall built on a drone's cell drops it there; its stacks are pushed out as any floor stack.
- A hand or sort taking from a crate leaves drones coming less: the first to arrive takes its count.
- A machine's outputs of an item wait for the drone on its way, though another is free.
- Stacks of one item holding other charge are not gathered: only single-item stacks hold charge.
- `Off` keeps a machine taken apart: ids are never reused, and it is never read.
- No drones in vacuum, opening doors, colliding or flying outside the hull; reach is fixed.
- A bay's zone is a square box, clipped at the grid's edge.

# Logistics

Drones ([0062](../../docs/decisions/0062-drones-carry-what-machines-request.md)): crate roles, what machines
request, and the drones in flight. A drone bay launches a docked drone to bring what a requester lacks from a
provider or storage, or to carry what a fed station made to storage; drones fly through air and doors within
their bay's zone and drop where they stand with no way on. Factorio's supply and demand, inside the hull.

- **Depends on:** `Grid` (`IGridQueries`: machines, their kinds and cells, door cells), `Atmosphere`
  (`IAtmosphereQueries.AiredAt`: where drones may fly), `Items` (`IItemsQueries`: `Crate`, `HeldFor`, `RoomFor`;
  `IItemsOps`: `TakeFrom`, `PutIn`, `LayDown`). Stations are read from `Content.Stations`, not `Crafting`.
- **State** (`LogisticsState`, members internal): `Roles` — the crates that are not storage, `CrateRole(Machine,
  Role)` in machine id order; `Requests` — each machine requesting anything, `Requesting(Machine, Requests)` in
  machine id order, its `Request(Item, Count)`s in the order set; `Drones` — every drone in flight (`Drone`: id,
  bay, its own `Stack`, `X`/`Y` in thousandths of a cell, `Cell` it last reached the centre of, `To` it flies to,
  `Leg`, and its job: `Source`, `Slot`, `Target`, `Item`, `Count`, with its `Cargo`) in id order; `NextId`;
  `Ticks`, its own clock. All saved. Derived and never saved: reservations, read from the drones in flight; the
  floods and paths, found when needed in scratch arrays sized to one zone box.
- **Files:** `Logistics.cs` — the module, its commands and queries; `Flight.cs` — looking, matching and flying;
  `State.cs` — its slice and types; `Fuzz.cs` — its fuzz generator.
- **Runs** last in `Registry.All`, after `Furnace`: drones read the air of the tick.

## Rules

- **Roles:** a crate is storage, a provider or a requester. `SetRole(Crate, Role)`; refused `logistics.notCrate`
  for anything not a crate. Leaving requester clears the crate's requests.
- **Requests:** `SetRequests(Machine, Requests)` replaces what a machine requests; none clears them. Allowed on
  machines that take items in to work them, as many as their inputs (`Inputs`, or the slots of one with a
  `FuelSlot`): the furnace 2, the smelter 1, the burner generator 1, the grow capsule 1, the Former 2, the assembler
  4; and requester crates, `Content.Logistics.RequestsMost`. Refused `logistics.cannotRequest` elsewhere (the pod,
  the analyzer, the charging station, bays, storage and provider crates, a machine that is not there) and
  `logistics.tooMany` past the most. Requests on a machine taken apart stay and are skipped.
- **Where a delivery goes** (`Items.HeldFor`, `RoomFor`, `PutIn`): a fuel into the fuel slot; anything else into the
  inputs but the fuel slot, as a whole; a crate or a bay across all its slots.
- **A bay** (`droneBay`, 2×2, four slots, no power yet) looks when `(Ticks + id) % LookEvery == 0`, bays in id
  order, each tick after the drones have flown. With a drone in a slot (the first, in slot order) it floods its
  zone — the cells within `Reach` of its footprint — breadth first from its passable cells through passable cells
  (aired, or a door's, closed too), and launches that drone on the first job:
  1. **Requests:** requesters in id order, each one's requests in order, the requester in the flood: missing is
     the count less what it holds where a delivery goes and less what drones bring (jobs on the way to their source,
     cargo after). For one short, the **source:** providers (every machine's outputs past its inputs, provider
     crates) before storage crates; the nearest by Manhattan distance between top-left cells, then the lowest id,
     then the lowest slot; in the flood; only stacks no drone is on its way to take. It carries the least of what
     is missing, the stack, the stack limit and the room at the target.
  2. **Push**, with no request served: the first unreserved output of a fed station in the flood (id, slot) to a
     storage crate in the flood with room for it — one already holding the item first, then the nearest, then the
     lowest id — the least of the stack and that room. Queued stations' outputs are never pushed.
- **Flight:** a drone moves `Speed` thousandths of a cell a tick, cell centre to cell centre. At each centre: in a
  cell not passable it drops; in its goal (any cell of the machine) it arrives; else it finds a path from there to
  its goal inside its bay's zone, breadth first over neighbours up, left, right, down, and flies to its first step;
  none, it drops. A cell ahead no longer passable turns it back to the centre it left. A bay gone drops its drones
  wherever they are.
- **Arriving:** at its source it takes what the slot holds of its item up to its job, any of it, and with none goes
  home; a source taken apart sends it home. At its target it puts in what fits and lays the rest down there; a
  target taken apart has its cargo laid where the drone is. Home, it docks in its bay's first free slot, or lies
  down where it is with none.
- **Dropping** lays the drone's stack and its cargo where it is, still, for anyone to pick up (`LayDown`).

## API

- **Queries:** `RoleOf(crate)`; `RequestsOf(machine)` — live and read-only; `RequestsMost(machine)` — 0 where none
  may be set; `Drones` — live and read-only, for drawing; `ZoneOf(bay)` — its zone box, null for no bay;
  `InReach(machine, item)` — whether a bay reaches it and a source in its flood holds the item, taken or not: an
  unmet request's tooltip.
- **Commands:** `SetRole(Crate, Role)`, `SetRequests(Machine, Requests)`.
- **Fuzz:** roles mostly of the map's crates, now and then of another machine or an id that is none; requests
  mostly on machines that may hold them, as many as they may or one too many, of any item, now and then none, and
  now and then on a machine that may not.

## Invariants

- Items are kept, drones included, across machines, the floor, drones in flight and their cargo; each drone is
  once, docked, flying or on the floor; no request is overfilled by drones (`LogisticsInvariantTests`, FsCheck,
  with walls built and taken down and the door worked mid-flight).
- Saved, loaded and played on mid-flight, a world hashes the same (`LogisticsTests`).

## Deliberately not handled

- No power: drones fly for nothing until slice 3 gives them charge.
- Requests on a machine taken apart stay in the slice forever: ids are never reused, and they are skipped.
- Scarcity favours low ids: requesters are served in id order, no round robin.
- Distance is Manhattan between top-left cells, not path length.
- A drone goes home after every job.
- Room at the target is read as the job is found: two drones bound for one machine for different requests may
  bring more than fits, and the rest lies at the target.
- Leftover cargo lies on the floor at the target, never taken back to storage.
- A drone checks the air only at cell centres: one may fly half a cell into a cell vented that tick before turning.
- A wall built on the cell a drone stands in drops it there; its stacks are pushed out as any floor stack is.
- No drones in vacuum, opening doors, colliding or flying outside the hull.
- A bay's zone is a square box, clipped at the grid's edge.

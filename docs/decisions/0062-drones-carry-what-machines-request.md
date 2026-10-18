# 0062 — Drones carry what machines request

- Status: accepted; supersedes [0002](0002-coop-model.md)'s line on drones as host-only physics
- Date: 2026-10-18
- Design: "drone logistics" (approved with its cuts and the user's answers, 2026-10-18), slice 2; mockup "Drone
  Logistics Windows"

## Context

Every item moved between machines by hand. While the crew was out salvaging, production stopped as soon as an
input ran dry or an output filled. Slice 1 gave the Former and the assembler input slots and entries fed from them
(0057); nothing filled those inputs but a hero. 0002 had drones as bodies in the host's physics, outside the hull;
the drones wanted now fly inside it, between machines, and what they carry is the economy itself.

## Decision

- **Drones are sim entities,** in a module of their own, `Logistics`, not host physics: what they carry changes
  inventories, so every peer must fly them alike. Positions are integers, thousandths of a cell; order is by id,
  slot and request order. This supersedes 0002's "Rapier simulates … drones".
- **Supply and demand, Factorio's:**
  - **Requests** are set per machine, a list of (item, count), by `SetRequests`: on the machines that take items in
    to work them — the furnace, the smelter, the burner generator, the grow capsule, the Former and the assembler,
    as many as their inputs — and on requester crates, up to `Content.Logistics.RequestsMost`. Not on the pod
    (paid by hand), the analyzer, the charging station or bays. A machine requests nothing until set.
  - **Crate roles,** `SetRole`: storage (the default, not saved), provider, requester. Leaving requester clears the
    crate's requests.
  - **Sources:** providers first — every machine's outputs past its inputs, provider crates — then storage; the
    nearest by Manhattan distance between top-left cells, then id, then slot, among stacks no drone is on its way to.
  - **Missing** is the count less what the machine holds where a delivery goes and less what drones bring. A
    delivery carries the least of what is missing, the source's stack, the stack limit and the room at the target.
  - **Push:** with no request served, the first unreserved output of a fed station goes to a storage crate with
    room: one holding the item first, then the nearest, then the lowest id. Queued stations' outputs only provide.
- **A drone bay** is a machine and a crate of four slots; docked drones are items in them (a drone, a stack of one,
  made at the assembler). A bay looks every `LookEvery` ticks, spread by its id, and launches one drone at most a
  look. Its **zone** is the square of cells within `Reach` of its footprint; one flood from the bay through
  passable cells inside it says what the bay reaches; source and target must both be in it. Two zones may overlap;
  what drones bring counts for every bay, so a machine in both gets one delivery.
- **Flight:** `Speed` thousandths of a cell a tick, cell centre to cell centre, four neighbours. Passable is a cell
  holding air or a door's, closed doors included; doors are never opened. Each time a drone reaches a centre it
  finds a path from there to its goal inside its zone (breadth first, up, left, right, down) and takes the first
  step; nothing about a path is kept. A cell ahead that stops being passable turns it back to the centre it left.
  **With no way on** — its cell airless, no path, its bay gone — it drops where it is, itself and its cargo as
  floor stacks. That covers a breach, a wall built across, an airless airlock and a bay taken apart.
- **At the source** it takes what is there up to its job, less if a hero took some, and goes home with nothing if
  nothing is left. **At the target** it puts in what fits — a fuel into the fuel slot, anything else into the
  inputs but the fuel slot, a crate across its slots — and lays the rest on the floor there. **Home,** it docks in
  a free slot of its bay, or lies down where it is with none.
- **Items gains machine-to-machine operations** for this: `TakeFrom(id, slot, count)`, `PutIn(id, stack)`,
  `LayDown(stack, cell)`, and the queries `HeldFor` and `RoomFor`. Moves by hand keep their rules.
- **Reservations are derived** from the drones in flight, never saved. Requests on a machine taken apart stay in
  the slice and are skipped: ids are never reused.
- **A bay's numbers are read in one place** (`BayOf`), so machine modules fold in there.
- **The assembler gets a fourth input,** as the content rule asks of a drone's four parts; a drone's battery is the
  one item with a capacity in a cost: its charge is lost there until drones hold charge (slice 3).
- **Format 17:** an empty logistics slice, its clock the world's tick; the assembler's fourth input, empty, after
  its three.
- **The client:** request slots under a fed station's slots, under the Former's and the assembler's inputs, above
  a requester crate's grid: a click with a stack on the cursor sets the item at the stack's count, the wheel ±1
  (Shift ±10), a right-click clears; a met request is dim, an unmet one edged amber, an empty slot a faint "+"; the
  tooltip says the item and count, met, or "nothing in reach has it". A crate's frame has a three-icon role toggle;
  a provider shows a red lamp on the crate and a requester a sky one, each casting a small light; storage none.
  A bay's frame shows four drone slots; its pads light where drones dock; its zone is outlined, dashed sky, while
  its frame is open or a bay blueprint is in hand. A flying drone is a small body with two rotors, drawn by code,
  its cargo's icon hanging under it, smoothed between ticks. No sound yet: it waits for the user's audition.

## Rejected

- **The bay's connected aired region as its zone:** the whole ship, shifting with every door and breach, and no
  way to split networks.
- **Saved paths, replanning by the grid's log, a cap on paths a tick:** a drone finds a short path at every cell
  for little; nothing kept means nothing to keep right across a load.
- **A grace period in vacuum before a drone falls:** a drone with no way on drops at once; one rule covers every
  case.
- **A round-robin cursor over requesters:** id order is readable; scarcity favours low ids, accepted.
- **Requests derived from a queued station's queue:** the player sets them like any machine's.
- **Requests on the charging station and the analyzer:** not now.

## Consequences

- A furnace's planks are brought again as they burn: a request is what a machine keeps, not what it gets once.
- Leftovers lie at the target; a full storage leaves fed stations' outputs where they are.
- A bay with docked drones cannot be taken apart, as no crate holding items can.
- Drones carry through closed doors and never let air through: an airless airlock between rooms stops them.

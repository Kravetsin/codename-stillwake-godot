# Atmosphere

## Files

| File | Purpose |
| --- | --- |
| `Air.cs` | The rules of air: tanks pour, vaporizers make, vents air their rooms in order. |
| `Atmosphere.cs` | The module, its interfaces and command: tanks, generators, filling, use and venting. |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `Order.cs` | The order of a room's cells, nearest its live vents first. |
| `State.cs` | The saved slice and its records: `RoomStatus`, `RoomAir`, `AtmosphereState`. |

Oxygen: liquid in tanks, turned into air by vaporizers (liquid-oxygen), and how it fills rooms, is used in them and vents into space.

- **Depends on:** `Grid` (machines, walls by material), `Items` (canisters), `Rooms` (cells, vents, hull,
  openings), `Pipes` (which stores a vent reaches, which tanks a vaporizer), `Power` (a vaporizer's share).
- **State** (`AtmosphereState`, internal, all saved): `Tanks` — liquid in each tank, gas in each vaporizer's store, holding any, by
  machine id, in id order; `Making` — each vaporizer's work toward its next unit; `Aired` — cells holding air,
  sorted; `Clock` — for the fill beats; `Owed` — each sealed aired room's part of a leak unit, by room id; `Live` —
  the live vents' cells, machine id order (null until a tick lists them). Derived: `Orders` by room id and what they
  were found from (`Ordered*`); in the module, the survey and each cell's leak. A tank over an `o` on the map starts with
  `Air.Canister`; an `o` under anything else does not load (`FormatException`).

## Rules

- **Air lives in cells, a unit each** (0011). A room's `RoomStatus`, read from its cells each tick: **Venting** — open
  to space; **Aired** — all aired; **Filling** — not all, a vent supplied (below); **Vacuum** — otherwise.
- **A room is a tank** (air-as-a-tank): its air is always the first cells of its order, its count all that matters.
  The order is its cells breadth first from its **live** vents together (`Live`, machine id order), neighbours up, left,
  right, down; a room with none from the cell nearest the mean of its cells (rounded down, smallest on a tie).
- **Only vents put air in;** a leak, a breath, a fire and venting each take the last aired cells of the order, never
  the tanks: the tanks feel a use only as the vents refill it. A hero breathes room air only in a cell holding it (0016).
- **When rooms or the live vents change,** each touched room counts the aired cells inside it and airs that many of
  its new order, emptying the rest; cells no longer in a room lose theirs. A door opened adds two rooms' air, closed
  splits it by where it lay. Only a tick lays air out; `Rebuild` finds the orders from `Live` and nothing else.
- **Vaporizers** (liquid-oxygen; kind `generator`, `AirSource`) turn the liquid of the tanks (`Gas`) whose output is on
  their input's network into gas in their store, up to `Air.GeneratorStore`: with room and liquid behind, `Making` grows by
  `Air.GeneratorRate` × satisfaction, a unit each `Air.LeakEvery × 1000` taken from those tanks in id order; the rest
  is kept, else it stands. A source with no input (the pod, 2026-10-31-the-scoop) makes air from nothing. A vent is
  **fed** when a vaporizer's output is on its network; **live** when fed and its network holds oxygen: gas in a store,
  a vaporizer with liquid behind (power not asked) or the pod; **supplied** when a store feeding it holds gas or a
  vaporizer feeding it is powered with liquid behind (read after the vaporizers).
- **Tanks in a line** (tanks-in-a-line): each tank holding liquid, in id order, pours up to `Air.TankFlow` a tick into the
  tanks whose input is on its output's network, in id order, as each has room; a line fills from its last tank.
- **A tick of air,** in the simulation phase, in order (no ADR):
  1. Rooms `ChangedSince` names (or whose cells differ) get new orders and are laid out; then the live vents are
     listed and the rooms of those that changed laid out. A machine's cell in the grid's log (0050) redoes the survey.
  2. Tanks pour on; each vaporizer, in id order, makes its share; tanks and stores are reread.
  3. A room open to space loses `Air.VentLoss` cells from the far edge, and every vent in it or in space drains
     its network by `Air.VentDrain`.
  4. Every sealed room holding air leaks through its aired cells (0020, air-as-a-tank), per `Air.LeakEvery` ticks: each
     cell its back wall's `Leak` and each hull cell's given to it (its first neighbour in the room, up, left, right,
     down, then diagonals; `Air.DoorLeak` a hull door). A back wall behind a wall or closed door leaks nothing. The
     leak adds to the room's `Owed`; whole units fall due and are taken from its far edge, the rest kept (64-bit).
  5. Every `Air.TicksPerCell` ticks each sealed room not full airs, for each of its live vents' networks (vent id,
     then port order), up to `Air.CellsPerFill` of its first empty cells (0105), a unit each from that network's stores.
- **Draws** take only from the stores of the vaporizers feeding the network, in id order; emptied ones leave `Tanks`
  at the tick's end.
- **Canisters** (no ADR): a tank holds up to `Air.TankCapacity` of liquid; `LoadCanister` takes a canister from
  the sender (free in creative) and adds `Air.Canister`, refused if the tank would spill. A tank taken apart takes its oxygen
  with it (construction phase).
- **The survey** (no ADR) — vents, fills, stores, tanks, aired cells and their leak, by room and network — is kept
  between ticks, redone for changed rooms (all, once the pipes' `Version` moved); what is held is reread after a
  canister, a tank taken apart or the tick's start. Machines are walked by trait (0113), ports in spec order.
- **`Aired` is edited in place** for up to 32 cells changed (a binary search each), merged into a new list for more.

## API

- **Queries** (`IAtmosphereQueries`): `OxygenIn(tank)`; `MakesAt(source)` — a vaporizer's or the pod's rate while its store has
  room, thousandths of a unit a tick; `CanisterProblem(tank, peer)`; `Fed(vent)`; `Supplied(vent)`;
  `StatusOf(room)`; `AiredAt(cell)`; `HoldsAir(cell)` — its room holds any (where drones fly); `AiredCells` — sorted, live; `RoomsAir()` — each room's `RoomAir` for the
  overlay (see `State.cs`), with why a vacuum gets none (`atmosphere.noVent`, `.notPiped`, `.dry`, `.unpowered`); on a timer.
- **Operations** (`IAtmosphereOps`): `Breathe(cell, amount)` — up to `amount` for a hero's head or a fire in `cell`,
  if it holds air, after atmosphere's tick (crew, furnace); returns how much. `Fill(tank, amount)` — liquid, up to capacity.
- **Commands:** `LoadCanister(Tank)`, refused with `atmosphere.noTank` (a vaporizer too), `atmosphere.tankFull`
  or `items.notEnough`.
- **Fuzz:** mostly a canister into a tank the grid has, else into ids 0–5, mostly no tank.

## Invariants

- A tank holds between nothing and its capacity; a vaporizer, its store. Only those holding any are listed. Liquid,
  gas and aired cells together grow only by a canister.
- Aired cells are sorted, distinct, and each in a room; a fill airs only cells joined through air to a
  vent that paid; every room's air is the first cells of its order.
- Orders, survey and leaks kept equal those found afresh, as after a load: a loaded world goes on
  with the same air.

## Deliberately not handled

- Air does not thin or flow between rooms (air-as-a-tank); it jumps when a room is laid out anew: a door opened onto an
  empty room spreads both rooms' air over the joined order at once.
- A hero or fire at the far edge of a room at its limit can lose its cell for a tick, back on the next fill.
- A vent in a venting room drains its whole network, aired rooms on it included: cut it with a valve.
- A wall facing a one-cell gap is no hull: the gap is a tiny room open to space (`Rooms`), so it leaks nothing.
- A wall's material changed without its code (no player can) reaches the leaks once its room is found again.
- A vaporizer cannot be switched off; one fills a room a cell a tick: build more.
- A valve under a tank is not worked with E; a tank taken apart refunds no canisters.

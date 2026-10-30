# Atmosphere

## Files

| File | Purpose |
| --- | --- |
| `Air.cs` | The rules of air: air stays where it is; vents air the first empty cells of their room's order. |
| `Atmosphere.cs` | The module, its interfaces and command: tanks, generators, filling, use and venting. |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `Order.cs` | The order of a room's cells: where its air lies, nearest its fed vents (`Fed`) first. |
| `State.cs` | The saved slice and its records: `RoomStatus`, `RoomAir`, `AtmosphereState`. |

Oxygen: liquid in tanks, turned into air by vaporizers (liquid-oxygen), and how it fills rooms, is used in them and vents into space.

- **Depends on:** `Grid` (machines, walls by material), `Items` (canisters), `Rooms` (cells, vents, hull,
  openings), `Pipes` (which stores a vent reaches, which tanks a vaporizer), `Power` (a vaporizer's share).
- **State** (`AtmosphereState`, internal, all saved): `Tanks` — liquid in each tank, gas in each vaporizer's store, holding any, by
  machine id, in id order; `Making` — each vaporizer's work toward its next unit; `Aired` — cells holding air,
  sorted; `Clock` — for the fill and leak beats. Derived: `Orders` by room id and what they were found from
  (`Ordered*`); in the module, the survey and each room's leak. A tank over an `o` on the map starts with
  `Air.Canister`; an `o` under anything else does not load (`FormatException`).

## Rules

- **Air lives in cells, a unit each** (0011). Room ids change when a door moves, so air is kept per cell and a
  room's `RoomStatus` is read from its cells every tick: **Venting** — open to space; **Aired** — every cell
  aired; **Filling** — not all aired, and a vent supplied (below);
  **Vacuum** — otherwise, a half-aired room with no supply included.
- **Air stays where it is** (0011). Each room's cells are ordered breadth first from its vents (piped or not),
  in vent id order, neighbours up, left, right, down; a room with no vent from the cell nearest the mean of its
  cells (rounded down, smallest on a tie). Vents air the first empty cells of the order, and whatever takes air
  takes the last aired one, so air gathers round the vents and never jumps.
- **When rooms, a room's vents or their order change,** the touched rooms get new orders and the air stays put;
  cells no longer in a room lose theirs. A door opened onto an empty room lets the vents fill it through the
  doorway; closed again, each part keeps its cells.
- **What uses air** — a breath, a leak — is paid by the room's vents' networks, in vent id order; what they
  cannot pay is taken from the far edge. A hero breathes room air only in a cell holding it (0016).
- **Vaporizers** (liquid-oxygen; kind `generator`, `AirSource`) turn the liquid of the tanks (`Gas`) on their input's
  network into gas in their store, up to `Air.GeneratorStore`: with room and liquid behind, `Making` grows by
  `Air.GeneratorRate` × satisfaction, a unit each `Air.LeakEvery × 1000` taken from those tanks in id order; the rest
  is kept, else it stands. A vent is **fed** when a vaporizer's output is on its network, **supplied** when a store
  feeding it holds gas or a vaporizer feeding it is powered with liquid behind (read after the vaporizers).
- **A tick of air,** in the simulation phase, in order (no ADR):
  1. The survey is gathered; each vaporizer, in id order, makes its share; tanks and stores are read again.
  2. If the grid's or rooms' `Version` moved, rooms `IRoomsQueries.ChangedSince` names (or whose cells differ)
     and any whose vents in order changed get new orders; vents are relisted only when the grid's log (0050)
     names a machine's cell or no longer reaches back. Leaks and survey are redone round a change.
  3. A room open to space loses `Air.VentLoss` cells from the far edge, and every vent in it or in space drains
     its network by `Air.VentDrain`.
  4. Every `Air.TicksPerCell` ticks each filling room airs the next `Air.CellsPerFill` cells of its order (0105),
     a unit each from the first of its vents (id order) whose network holds any. More vents do not fill faster.
  5. Every other aired room leaks through what its air touches (0020): the `Leak` of each cell's back wall and
     each hull wall, `Air.DoorLeak` a hull door, per `Air.LeakEvery` ticks, spread exactly over the period
     (each tick due-by-end less due-by-start, rounded down, 64-bit). A back wall behind a wall or closed door
     leaks nothing. Leaks are kept per room id while the rooms' `Version` stays.
- **Draws** take only from the stores of the vaporizers feeding the network, in id order; emptied ones leave `Tanks`
  at the tick's end.
- **Canisters** (no ADR): a tank holds up to `Air.TankCapacity` of liquid; `LoadCanister` takes a canister from
  the sender (free in creative) and adds `Air.Canister`, refused if the tank would spill. A tank taken apart takes its oxygen
  with it, in the construction phase, looked for only when the grid's log names a machine's cell.
- **The survey** (no ADR) — vents by room, stores by network, tanks behind each vaporizer, aired cells by room — is
  kept between ticks, gathered afresh only when the pipes' `Version` moved or every room was found again; else only changed
  rooms and the vents in space are redone. Satisfaction and tanks holding oxygen are reread on the first call after
  a canister, a tank taken apart, or the tick's start. Machines are walked by trait (`MachinesWith`,
  0113), a vaporizer's ports by its spec's `Ports` order. Walks over cells cover all rooms at once
  (`IRoomsQueries.RoomOfCells`).
- **`Aired` is edited in place** for up to 32 cells changed (a binary search each), merged into a new list for more.

## API

- **Queries** (`IAtmosphereQueries`): `OxygenIn(tank)`; `CanisterProblem(tank, peer)`; `Fed(vent)`; `Supplied(vent)`;
  `StatusOf(room)`; `AiredAt(cell)`; `AiredCells` — sorted, live; `RoomsAir()` — each room's `RoomAir`: status,
  aired cells, leak, the liquid behind the vaporizers feeding it (each tank once; null for none), ticks it lasts
  leaving at the lesser of the room's draw and what they make, what they make a period, and why a vacuum gets none
  (`atmosphere.noVent`, `.notPiped`, `.dry`, `.unpowered`). Read on a timer; breathing and other rooms not counted.
- **Operations** (`IAtmosphereOps`): `Breathe(cell, amount)` — takes up to `amount` for a hero's head or a
  furnace's fire in `cell`, if it holds air; returns how much. Emptied tanks leave the list at once. Call it in the
  simulation phase after atmosphere (crew and furnace do).
- **Commands:** `LoadCanister(Tank)`, refused with `atmosphere.noTank` (a vaporizer too), `atmosphere.tankFull`
  or `items.notEnough`.
- **Fuzz:** mostly a canister into a tank the grid has, else into ids 0–5, mostly no tank.

## Invariants

- A tank holds between nothing and its capacity; a vaporizer, its store. Only those holding any are listed. Liquid,
  gas and aired cells together grow only by a canister.
- Aired cells are sorted, distinct, and each in a room.
- Orders, survey and leaks kept room by room equal those found afresh, as after a load: a loaded world goes on
  with the same air.

## Deliberately not handled

- Air does not thin or flow: a cell holds air or not. A door opened onto an empty room spills nothing; the vents
  fill it, nearest first.
- A vent in a venting room drains its whole network, aired rooms on it included: cut it with a valve.
- A wall facing a one-cell gap is no hull: the gap is a tiny room open to space (`Rooms`), so it leaks nothing.
- A wall's material changed without its code (taken apart and rebuilt in one tick, which no player can do)
  reaches the leaks only once its room is found again.
- A vaporizer cannot be switched off; one vaporizer fills a room at a cell a tick: build more.
- Oxygen in a tank taken apart is lost, not refunded as canisters. A valve under a tank is not worked with E.
- A cell aired by a fill and taken by a leak on one tick stays aired, so that cell of the leak is free: one cell a
  room a tick at most, only on the tick its network runs dry.

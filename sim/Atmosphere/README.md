# Atmosphere

## Files

| File | Purpose |
| --- | --- |
| `Air.cs` | The rules of air: tanks pour, vaporizers make, vents air their rooms in order. |
| `Atmosphere.cs` | The module, its interfaces and command: tanks, generators, filling, use and venting. |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `Order.cs` | The order of a room's cells, nearest its fed vents first, and of each vent's alone. |
| `State.cs` | The saved slice and its records: `RoomStatus`, `RoomAir`, `AtmosphereState`. |

Oxygen: liquid in tanks, turned into air by vaporizers (liquid-oxygen), and how it fills rooms, is used in them and vents into space.

- **Depends on:** `Grid` (machines, walls by material), `Items` (canisters), `Rooms` (cells, vents, hull,
  openings), `Pipes` (which stores a vent reaches, which tanks a vaporizer), `Power` (a vaporizer's share).
- **State** (`AtmosphereState`, internal, all saved): `Tanks` — liquid in each tank, gas in each vaporizer's store, holding any, by
  machine id, in id order; `Making` — each vaporizer's work toward its next unit; `Aired` — cells holding air,
  sorted; `Clock` — for the fill beats; `Owed` — each sealed aired room's part of a leak unit, by room id. Derived:
  `Orders` by room id, `VentOrders` by vent cell, and what they were found from
  (`Ordered*`); in the module, the survey and each cell's leak. A tank over an `o` on the map starts with
  `Air.Canister`; an `o` under anything else does not load (`FormatException`).

## Rules

- **Air lives in cells, a unit each** (0011), as room ids change when a door moves. A room's `RoomStatus` is read
  from its cells every tick: **Venting** — open to space; **Aired** — every cell aired; **Filling** — not all
  aired, a vent supplied (below); **Vacuum** — otherwise, a half-aired room with no supply included.
- **Air stays where it is** (0011). Each fed vent orders its room's cells breadth first from itself alone, neighbours
  up, left, right, down, and fills from that order (air-from-vents). The take order is the room's: breadth first from
  its fed vents together, in vent id order; a room with no fed vent from the cell nearest the mean of its cells
  (rounded down, smallest on a tie). Whatever takes air takes the last aired cell of it, so air never jumps.
- **When rooms, a room's vents or their order change,** the touched rooms get new orders and the air stays put;
  cells no longer in a room lose theirs. A door opened: the vents fill on through it; closed: each part keeps its air.
- **What uses air** — a breath, a leak — is paid by the room's vents' networks, in vent id order; what they
  cannot pay is taken from the far edge. A hero breathes room air only in a cell holding it (0016).
- **Vaporizers** (liquid-oxygen; kind `generator`, `AirSource`) turn the liquid of the tanks (`Gas`) whose output is on
  their input's network into gas in their store, up to `Air.GeneratorStore`: with room and liquid behind, `Making` grows by
  `Air.GeneratorRate` × satisfaction, a unit each `Air.LeakEvery × 1000` taken from those tanks in id order; the rest
  is kept, else it stands. A source with no input (the pod, 2026-10-31-the-scoop) makes air from nothing. A vent is
  **fed** when a vaporizer's output is on its network, **dry** when fed but its network holds no gas, **supplied** when
  a store feeding it holds gas or a vaporizer feeding it is powered with liquid behind (read after the vaporizers).
- **Tanks in a line** (tanks-in-a-line): each tank holding liquid, in id order, pours up to `Air.TankFlow` a tick into the
  tanks whose input is on its output's network, in id order, as each has room; a line fills from its last tank.
- **A tick of air,** in the simulation phase, in order (no ADR):
  1. The survey is gathered; tanks pour on; each vaporizer, in id order, makes its share; tanks and stores are reread.
  2. If the grid's or rooms' `Version` moved, rooms `ChangedSince` names (or whose cells differ) and any whose
     vents changed get new orders; vents are relisted when the grid's log (0050) names a machine's cell.
  3. A room open to space loses `Air.VentLoss` cells from the far edge, and every vent in it or in space drains
     its network by `Air.VentDrain`.
  4. Every sealed room holding air leaks through its aired cells (0020, air-from-vents), per `Air.LeakEvery` ticks: each
     cell its back wall's `Leak` and each hull cell's given to it (its first neighbour in the room, up, left, right,
     down, then diagonals; `Air.DoorLeak` a hull door). A back wall behind a wall or closed door leaks nothing. The
     leak adds to the room's `Owed`; whole units fall due, the rest kept (64-bit). Cell leaks follow the rooms' `Version`.
  5. Every `Air.TicksPerCell` ticks each fed vent of a filling room, in machine id order, airs up to
     `Air.CellsPerFill` empty cells of its own order (0105), a unit each from its own network, while it holds gas.
- **Draws** take only from the stores of the vaporizers feeding the network, in id order; emptied ones leave `Tanks`
  at the tick's end.
- **Canisters** (no ADR): a tank holds up to `Air.TankCapacity` of liquid; `LoadCanister` takes a canister from
  the sender (free in creative) and adds `Air.Canister`, refused if the tank would spill. A tank taken apart takes its oxygen
  with it (construction phase).
- **The survey** (no ADR) — vents and fills by room, stores by network, tanks behind each vaporizer and each tank pours
  into, aired cells and their leak by room — is kept between ticks: found afresh when the pipes' `Version` moved or
  every room was found again, else redone for changed rooms and space. Satisfaction and what is held are reread after
  a canister, a tank taken apart, or the tick's start. Machines are walked by trait (0113), ports in spec order.
- **`Aired` is edited in place** for up to 32 cells changed (a binary search each), merged into a new list for more.

## API

- **Queries** (`IAtmosphereQueries`): `OxygenIn(tank)`; `MakesAt(source)` — a vaporizer's or the pod's rate while its store has
  room, thousandths of a unit a tick; `CanisterProblem(tank, peer)`; `Fed(vent)`; `Supplied(vent)`;
  `StatusOf(room)`; `AiredAt(cell)`; `AiredCells` — sorted, live; `RoomsAir()` — each room's `RoomAir`: status,
  aired cells, leak, the liquid behind the vaporizers feeding it (each tank once; null for none), ticks it lasts
  leaving at the lesser of the room's draw and what they make, what they make a period, and why a vacuum gets none
  (`atmosphere.noVent`, `.notPiped`, `.dry`, `.unpowered`). Read on a timer; breathing and other rooms not counted.
- **Operations** (`IAtmosphereOps`): `Breathe(cell, amount)` — up to `amount` for a hero's head or a fire in `cell`,
  if it holds air, after atmosphere's tick (crew, furnace); returns how much. `Fill(tank, amount)` — liquid, up to capacity.
- **Commands:** `LoadCanister(Tank)`, refused with `atmosphere.noTank` (a vaporizer too), `atmosphere.tankFull`
  or `items.notEnough`.
- **Fuzz:** mostly a canister into a tank the grid has, else into ids 0–5, mostly no tank.

## Invariants

- A tank holds between nothing and its capacity; a vaporizer, its store. Only those holding any are listed. Liquid,
  gas and aired cells together grow only by a canister.
- Aired cells are sorted, distinct, and each in a room; a tick airs only cells joined through air to a vent that paid.
- Orders, survey and leaks kept equal those found afresh, as after a load: a loaded world goes on
  with the same air.

## Deliberately not handled

- Air does not thin or flow between rooms (air-from-vents): a door opened onto an empty room spills nothing.
- A vent in a venting room drains its whole network, aired rooms on it included: cut it with a valve.
- A wall facing a one-cell gap is no hull: the gap is a tiny room open to space (`Rooms`), so it leaks nothing.
- A wall's material changed without its code (no player can) reaches the leaks once its room is found again.
- A vaporizer cannot be switched off; one fills a room a cell a tick: build more.
- A valve under a tank is not worked with E; a tank taken apart refunds no canisters.

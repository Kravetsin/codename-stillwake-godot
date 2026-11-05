# Atmosphere

## Files

| File | Purpose |
| --- | --- |
| `Air.cs` | The rules of air: tanks pour, vaporizers make, each zone holds the bubble its vents pay for. |
| `Atmosphere.cs` | The module, its interfaces and command: tanks, generators, filling, use and venting. |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `Order.cs` | The order of a zone's cells, nearest its live vents first. |
| `State.cs` | The saved slice and its records: `RoomStatus`, `RoomAir`, `AtmosphereState`. |
| `Zones.cs` | The air zones: each room's cells less its doorways, followed from the rooms. |

Oxygen: liquid in tanks, turned into gas by vaporizers (liquid-oxygen), and the bubble of air each vent holds round itself.

- **Depends on:** `Grid` (machines, walls by material, doorways), `Items` (canisters), `Rooms` (cells, hull,
  openings), `Pipes` (which stores a vent reaches, which tanks a vaporizer), `Power` (a vaporizer's share).
- **State** (`AtmosphereState`, internal, all saved): `Tanks` — liquid in each tank, gas in each vaporizer's store, holding any, by
  machine id, in id order; `Making` — each vaporizer's work toward its next unit; `Aired` — cells holding air,
  sorted; `Clock` — for the fill beats; `Owed` — each sealed aired zone's part of a leak unit, by zone key; `Live` —
  the live vents' cells, machine id order (null until a tick lists them). Derived: the zones, `Orders` by zone key; in
  the module, the survey, each cell's leak, the budgets. A tank over an `o` on the map starts with `Air.Canister`; an `o`
  under anything else does not load (`FormatException`).

## Rules

- **Air zones** (air-bubbles): a room's cells less its doorway cells (door or hatch, open or shut), split where that
  divides them (up, left, right, down), keyed by the least cell. A doorway never holds air; an inner door moved changes
  no zone's cells. A zone is **open** when its room is: a way to space through open doors vents it.
- **A zone's air is the first cells of its order** (0011): its cells breadth first from its **live** vents together
  (`Live`, machine id order), else from the cell nearest the mean of its cells (rounded down, smallest on a tie).
- **Vaporizers** (liquid-oxygen; kind `generator`, `AirSource`) turn the liquid of the tanks (`Gas`) whose output is on
  their input's network into gas in their store, up to `Air.GeneratorStore`: with room and liquid behind, `Making` grows by
  `Air.GeneratorRate` × satisfaction, a unit each `Air.LeakEvery × 1000` taken from those tanks in id order. A source
  with no input (the pod, 2026-10-31-the-scoop) makes gas from nothing. A vent is **fed** when a vaporizer's output is
  on its network; **live** when fed and its network holds oxygen: gas in a store, a vaporizer with liquid behind (power
  not asked) or the pod; **supplied** when a store feeding it holds gas or a vaporizer feeding it is powered with liquid.
- **A zone's budget**, thousandths of a unit per `Air.LeakEvery`: for each live vent's network, `Air.GeneratorRate` ×
  each vaporizer's satisfaction on it (the pod's in full), split evenly over the live vents in sealed zones on it,
  rounded down; summed. **Its bubble**: the longest prefix of its order whose cells' leak × 1000 fits the budget.
- **A cell's leak**, per `Air.LeakEvery` (0020): its back wall's `Leak` and each hull cell given to it (its first
  neighbour in the zone, up, left, right, down, then diagonals); a doorway beside the zone is hull, `Air.DoorLeak`. A
  back wall behind a wall leaks nothing.
- **Tanks in a line** (tanks-in-a-line): each tank holding liquid, in id order, pours up to `Air.TankFlow` a tick into the
  tanks whose input is on its output's network, in id order, as each has room.
- **A tick of air,** in the simulation phase, in order:
  1. Zones follow the rooms (`ChangedSince`, or all); touched zones get new orders, laid out by count; then the live
     vents are listed and their zones laid out. Cells in no zone lose their air.
  2. Tanks pour on; each vaporizer, in id order, makes its share; tanks and stores are reread.
  3. An open zone loses `Air.VentLoss` cells from the far edge, and every vent in it or in space drains its network by
     `Air.VentDrain`.
  4. Every `Air.TicksPerCell` ticks each sealed zone steps toward its bubble by up to `Air.CellsPerFill` cells (0105):
     the next cell of its order while it fits, the last aired one while over; none at all with no budget.
  5. Each sealed zone holding air adds its aired cells' leak to its `Owed`; whole units fall due and are drawn from its
     vents' networks (vent, then port order), the rest kept (64-bit). What they lack is forgiven.
- **A breath or a fire** (0016) in an aired cell is drawn from its zone's networks the same way, never a cell; it always
  succeeds there. A hero breathes room air only in an aired cell.
- **A room's status** from its zones: **Venting** if one is open; **Aired** if every zone counted is full; **Filling** if
  one has a live vent with making; else **Vacuum**. A sealed zone with no piped vent and no air is not counted.
- **Draws** take only from the stores of the vaporizers feeding the network, in id order; emptied ones leave `Tanks`
  at the tick's end.
- **Canisters:** a tank holds up to `Air.TankCapacity` of liquid; `LoadCanister` takes a canister from the sender (free
  in creative) and adds `Air.Canister`, refused if the tank would spill. A tank taken apart takes its oxygen with it.
- **The survey** — vents, fills, stores, tanks, aired cells and their leak, by zone and network — is kept between
  ticks, redone for changed zones (all, once the pipes' `Version` moved); what is held is reread after a canister, a
  tank taken apart or the tick's start. Machines are walked by trait (0113), ports in spec order.
- **`Aired` is edited in place** for up to 32 cells changed (a binary search each), merged into a new list for more.

## API

- **Queries** (`IAtmosphereQueries`): `OxygenIn(tank)`; `MakesAt(source)` — a vaporizer's or the pod's rate while its store has
  room, thousandths of a unit a tick; `CanisterProblem(tank, peer)`; `Fed(vent)`; `Supplied(vent)`;
  `StatusOf(room)`; `AiredAt(cell)`; `HoldsAir(cell)` — its zone holds any (where drones fly); `AiredCells` — sorted,
  live; `RoomsAir()` — each room's `RoomAir` for the overlay (`State.cs`), with why a room falls short
  (`atmosphere.noVent`, `.notPiped`, `.dry`, `.unpowered`; `.leaky` when a bubble stops short of its zone); on a timer.
- **Operations** (`IAtmosphereOps`): `Breathe(cell, amount)` — `amount` for a hero's head or a fire in `cell` if it
  holds air, drawn from its zone's networks, else none (crew, furnace). `Fill(tank, amount)` — liquid, up to capacity.
- **Commands:** `LoadCanister(Tank)`, refused with `atmosphere.noTank` (a vaporizer too), `atmosphere.tankFull`
  or `items.notEnough`.
- **Fuzz:** mostly a canister into a tank the grid has, else into ids 0–5, mostly no tank.

## Invariants

- A tank holds between nothing and its capacity; a vaporizer, its store. Only those holding any are listed. Liquid and
  gas together grow only by a canister; an aired cell holds no unit.
- Aired cells are sorted, distinct, each in a zone, never a doorway; every zone's air is the first cells of its order,
  and grows only with a live vent.
- Zones, orders, survey and leaks kept equal those found afresh, as after a load: a loaded world goes on alike.

## Deliberately not handled

- Air does not flow between zones (air-bubbles): each room needs its own vent; a doorway cell holds no air.
- Power moves the bubble: a solar night shrinks it at the step rate. A network's making is split evenly over its vents.
- An owed part under a unit is dropped when its zone's cells change.
- A vent in a venting zone drains its whole network, aired zones on it included: cut it with a valve.
- A wall facing a one-cell gap is no hull: the gap is a tiny room open to space (`Rooms`), so it leaks nothing.
- A vaporizer cannot be switched off. A valve under a tank is not worked with E; a tank taken apart refunds no canisters.

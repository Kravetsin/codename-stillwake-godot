# Atmosphere

## Files

| File | Purpose |
| --- | --- |
| `Air.cs` | The rules of air: tanks pour, vaporizers make, each room holds the bubble its vents pay for. |
| `Atmosphere.cs` | The module, its interfaces and command: tanks, generators, filling, use and venting. |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `Order.cs` | The order of a room's cells, nearest its live vents first. |
| `State.cs` | The saved slice and its records: `RoomStatus`, `RoomAir`, `AtmosphereState`. |

Oxygen: liquid in tanks, turned into gas by vaporizers (liquid-oxygen), and the bubble of air each vent holds round itself.

- **Depends on:** `Grid` (machines, walls by material), `Items` (canisters), `Rooms` (cells, hull,
  openings), `Pipes` (which stores a vent reaches, which tanks a vaporizer), `Power` (a vaporizer's share).
- **State** (`AtmosphereState`, internal, all saved): `Tanks` — liquid in each tank, gas in each vaporizer's store, holding any, by
  machine id, in id order; `Making` — each vaporizer's work toward its next unit; `Aired` — cells holding air,
  sorted; `Clock` — for the fill beats; `Owed` — each sealed aired room's part of a leak unit, by room id; `Live` —
  the live vents' cells, machine id order (null until a tick lists them). Derived: `Orders` by room id; in
  the module, the survey, each cell's leak, the budgets. A tank over an `o` on the map starts with `Air.Canister`; an `o`
  under anything else does not load (`FormatException`).

## Rules

- **A bubble lives in a room** (air-bubbles): open doors pass it, closed ones stop it; a room parted by a door shut,
  the part with no vent fades from its far edge. Air is never laid out anew: it stays where it lies and flows a few
  cells a beat. Open to space, through open doors too, a room vents.
- **A room's order** (0011): its cells breadth first from its **live** vents together (`Live`, machine id order),
  else from the cell nearest the mean of its cells (rounded down, smallest on a tie). Air grows at its first empty cell
  (the gap) and goes from its last aired one.
- **Vaporizers** (liquid-oxygen; kind `generator`, `AirSource`) turn the liquid of the tanks (`Gas`) whose output is on
  their input's network into gas in their store, up to `Air.GeneratorStore`: with room and liquid behind, `Making` grows by
  `Air.GeneratorRate` × satisfaction, a unit each `Air.LeakEvery × 1000` taken from those tanks in id order. A source
  with no input (the pod, 2026-10-31-the-scoop) makes gas from nothing at `Air.PodRate` (leaks-tripled). A vent is **fed** when a vaporizer's output is
  on its network; **live** when fed and its network holds oxygen: gas in a store, a vaporizer with liquid behind (power
  not asked) or the pod; **supplied** when a store feeding it holds gas or a vaporizer feeding it is powered with liquid.
- **A room's budget**, thousandths of a unit per `Air.LeakEvery`: for each live vent's network, each source's rate ×
  its satisfaction on it (the pod's in full), split evenly over the live vents in sealed rooms on it,
  rounded down; summed. **Its bubble**: aired cells whose leak × 1000 fits the budget, settling round the vents.
- **A cell's leak**, per `Air.LeakEvery` (0020): its back wall's `Leak` and each hull cell given to it (its first
  neighbour in the room, up, left, right, down, then diagonals); a closed door in the hull, `Air.DoorLeak`. A
  back wall behind a wall leaks nothing.
- **Tanks in a line** (tanks-in-a-line): each tank holding liquid, in id order, pours up to `Air.TankFlow` a tick into the
  tanks whose input is on its output's network, in id order, as each has room.
- **A tick of air,** in the simulation phase, in order:
  1. Rooms `ChangedSince` names (or all) get new orders; then the live vents are listed. Air stays where it lies; cells
     in no room lose theirs.
  2. Tanks pour on; each vaporizer, in id order, makes its share; tanks and stores are reread.
  3. An open room loses `Air.VentLoss` cells from the far edge, and every vent in it or in space drains its network by
     `Air.VentDrain`.
  4. Every `Air.TicksPerCell` ticks each sealed room steps toward its bubble by up to `Air.CellsPerFill` cells (0105):
     the gap while it fits, the last aired cell while over; at the limit, the last aired cell past the gap moves into
     it while the budget still pays; all of it goes with no budget.
  5. Each sealed room holding air adds its aired cells' leak to its `Owed`; whole units fall due and are drawn from its
     vents' networks (vent, then port order), the rest kept (64-bit). What they lack is forgiven.
- **A breath or a fire** (0016) in an aired cell is drawn from its room's networks the same way, never a cell; it always
  succeeds there. A hero breathes room air only in an aired cell. A room's **status**: Venting if open, Aired if full, Filling while a live vent makes, else Vacuum.
- **Draws** take only from the stores of the vaporizers feeding the network, in id order; emptied ones leave `Tanks`
  at the tick's end.
- **Canisters:** a tank holds up to `Air.TankCapacity` of liquid; `LoadCanister` takes a canister from the sender (free
  in creative) and adds `Air.Canister`, refused if the tank would spill. A tank taken apart takes its oxygen with it.
- **The survey** — vents, fills, stores, tanks, aired cells and their leak, by room and network — is kept between
  ticks, redone for changed rooms (all, once the pipes' `Version` moved); what is held is reread after a canister, a
  tank taken apart or the tick's start. Machines are walked by trait (0113), ports in spec order.
- **`Aired` is edited in place** for up to 32 cells changed (a binary search each), merged into a new list for more.

## API

- **Queries** (`IAtmosphereQueries`): `OxygenIn(tank)`; `MakesAt(source)` — a vaporizer's or the pod's rate while its store has
  room, thousandths of a unit a tick; `CanisterProblem(tank, peer)`; `Fed(vent)`; `Supplied(vent)`;
  `StatusOf(room)`; `AiredAt(cell)`; `HoldsAir(cell)` — its room holds any (where drones fly); `AiredCells` — sorted,
  live; `RoomsAir()` — each room's `RoomAir` for the overlay (`State.cs`), its `Budget` its share of the making, with why a room falls short
  (`atmosphere.noVent`, `.notPiped`, `.dry`, `.unpowered`; `.leaky` when a bubble stops short of its room); on a timer.
- **Operations** (`IAtmosphereOps`): `Breathe(cell, amount)` — `amount` for a hero's head or a fire in `cell` if it
  holds air, drawn from its room's networks, else none (crew, furnace). `Fill(tank, amount)` — liquid, up to capacity.
- **Commands:** `LoadCanister(Tank)`, refused with `atmosphere.noTank` (a vaporizer too), `atmosphere.tankFull`
  or `items.notEnough`.
- **Fuzz:** mostly a canister into a tank the grid has, else into ids 0–5, mostly no tank.

## Invariants

- A tank holds between nothing and its capacity; a vaporizer, its store. Only those holding any are listed. Liquid and
  gas together grow only by a canister; an aired cell holds no unit.
- Aired cells are sorted, distinct, each in a room; a beat airs and empties at most `Air.CellsPerFill` cells a room
  (a door shut takes its own cells' air at once); air grows only with a live vent.
- Orders, survey and leaks kept equal those found afresh, as after a load: a loaded world goes on alike.

## Deliberately not handled

- Air does not thin or equalise (air-bubbles); a far cell leaking less than the gap would stays where it is at the limit.
- Power moves the bubble: a solar night shrinks it at the step rate. A network's making is split evenly over its vents.
- An owed part under a unit is dropped when its room's cells change.
- A vent in a venting room drains its whole network, aired rooms on it included: cut it with a valve.
- A wall facing a one-cell gap is no hull: the gap is a tiny room open to space (`Rooms`), so it leaks nothing.
- A vaporizer cannot be switched off. A valve under a tank is not worked with E; a tank taken apart refunds no canisters.

# Atmosphere

Oxygen: what the ship's tanks and generators hold, and how it fills rooms, is used in them and vents
into space. Ported from `packages/sim/src/modules/atmosphere`; its rules hold here unchanged.

- **Depends on:** `Grid` (`IGridQueries`: tanks, generators and vents are its machines; walls and back
  walls by material), `Items` (`IItemsQueries`, `IItemsOps`: canisters), `Rooms` (`IRoomsQueries`:
  where cells and vents are, hull, openings), `Pipes` (`IPipesQueries`: which tanks a vent reaches),
  `Power` (`IPowerQueries`: how much of its draw a generator gets).
- **State** (`AtmosphereState`, members internal): `Tanks` — the oxygen in each tank or generator
  holding any, by machine id, in id order; `Making` — each generator's work toward its next unit, by
  machine id; `Aired` — the cells holding air, sorted; `Clock` — ticks run, for the fill and leak
  beats. All saved. Derived: `Orders` — each room's cells in the order its air lies in, by room id
  (only looked up), the rooms per cell (`OrderedRooms`) and vents' cells (`OrderedVents`) they
  were found from, and the grid's and the rooms' versions they were found at (`OrderedGrid`,
  `OrderedVersion`). In the module, also derived: the survey (below) and each room's leak, kept
  between ticks. A tank drawn over an `o` on the map under the map starts with `Air.Canister`; an
  `o` under anything else is a broken map and does not load (`FormatException`).
- **Files:** `Air.cs` — the rules below; `Order.cs` — the order of a room's cells; `Atmosphere.cs` —
  the module, its interfaces and command; `State.cs` — its slice and records; `Fuzz.cs` — its fuzz
  generator.

## Rules

- **Air lives in cells, a unit of oxygen each** (ADR 0011). Room ids change whenever a door moves, so
  air is kept per cell, and a room's status (`RoomStatus`) is read from its cells every tick:
  - **Venting** — the room has openings to space;
  - **Aired** — every cell of it is aired;
  - **Filling** — not all aired, and one of its vents stands on a network holding oxygen, or with a
    powered generator on it;
  - **Vacuum** — otherwise, a half-aired room with no supply included.
- **Air stays where it is.** Each room's cells are ordered breadth first through the room from the
  vents standing in it (piped or not), in vent id order, neighbours up, left, right, down; a room with
  no vent is ordered from its middle, the cell nearest the mean of its cells (rounded down; the
  smallest such cell on a tie). Vents air the first empty cell of the order, and whatever takes air
  takes the last aired one, so a room's air gathers around its vents and never jumps.
- **When the rooms change, or a room's vents or their order,** the orders of the rooms they touch are found again
  and the air stays put; cells no longer in a room lose theirs. So a door opened onto an empty room
  lets the vents fill it through the doorway, and closed again each part keeps the cells it holds.
- **What uses air** — a hero's breath, a room's leak — is paid by the networks of the room's vents,
  in vent id order, as if they put the air back at once; what they cannot pay is taken as the last
  aired cells of the order, the far edge. A hero breathes room air only standing in a cell holding it
  (ADR 0016).
- **Generators** make `Air.GeneratorRate` units every `Air.LeakEvery` ticks at full power, each into a
  store of its own in `Tanks` holding up to `Air.GeneratorStore`; what does not fit is lost. Each tick
  a generator adds the rate times its power's satisfaction (thousandths) to its remainder in `Making`,
  saved, and makes a unit each time that reaches `Air.LeakEvery × 1000`: exactly the rate a period in
  full, proportionally less when short, nothing unpowered. Draws take from a store as from a tank. A
  vent on a network with a powered generator counts as supplied: its room is filling, not dry, while
  the store sits empty between units.
- **A tick of air,** in the simulation phase, in this order:
  1. Each generator, in id order, makes its share for the tick.
  2. If the grid's or the rooms' `Version` moved, the rooms and vents are followed: the rooms
     `IRoomsQueries.ChangedSince` names, or, when every room was found again, those whose cells
     differ from what the orders were found from, and any whose vents in order changed, get new
     orders; only cells they held can lose their air. Around a change their leaks and survey counts
     are redone and the rest kept; when every room was found again, both are found afresh.
  3. A room open to space loses `Air.VentLoss` cells from the far edge, and every vent in it — or
     standing in space — drains its network by `Air.VentDrain`.
  4. Every `Air.TicksPerCell` ticks, each filling room airs the next cell of its order, paying one unit
     from the first of its vents (in id order) whose network holds any. More vents do not fill it
     faster.
  5. Every other room holding air uses its leak, through what its air touches (0020) — the material
     `Leak` of the back wall behind each of its cells and of each hull wall, or `Air.DoorLeak` for a
     door in its hull, every `Air.LeakEvery` ticks — spread over the period: on each tick what is due
     by its end less what was by its start (rounded down, in 64 bits), so its total is exact. A back
     wall behind a wall or a closed door touches no air and leaks nothing. Leaks are kept per room
     while the rooms' `Version` stays: rooms are found again whenever walls or back walls change.
- **Draws** go tank by tank in tank id order along the network. Tanks a tick empties leave `Tanks` at
  its end.
- **Canisters:** a tank holds up to `Air.TankCapacity`; `LoadCanister` takes a canister from the
  sender's inventory (free in creative mode) and adds `Air.Canister`, refused when the tank would
  spill. A tank taken apart takes its oxygen with it, in the construction phase, looked for only when
  the grid's `Version` moved: only a machine taken apart leaves a tank's oxygen with no machine.
- **The survey** — vents by room, tanks and generators by network, aired cells by room — is kept
  between ticks and gathered again in one pass only when the pipes' `Version` moved or every room
  was found again; around a change, only the changed rooms' counts and vents are redone.
  Generators' satisfaction and the tanks holding oxygen are read again on the first call after they
  may have changed: a canister loaded, a tank taken apart, or atmosphere's system starting a tick,
  with power found again in derivation and generators' new stores yet to come. A tank emptied since
  stays read, at none, so it draws and counts as a missing one would. Vents, tanks and generators are walked by kind (`MachinesOf`). The aired
  cells it counts are kept in step with every change of `Aired`. Every walk over cells covers all
  rooms at once (`IRoomsQueries.RoomOfCells`).
- **`Aired` is edited in place** when a call changes up to 32 cells (a binary search each), and
  merged into a new list for more: copying a long list costs more than a few shifts of it.

## API

- **Queries** (`IAtmosphereQueries`): `OxygenIn(tank)`; `CanisterProblem(tank, peer)` — why that
  peer's `LoadCanister` would be refused, for the client; `StatusOf(room)` (TypeScript's
  `roomStatus`; a method cannot share the `RoomStatus` enum's name); `AiredAt(cell)`; `AiredCells` —
  every aired cell, sorted, live; `RoomsAir()` — every room's `RoomAir`: status, aired cells, leak,
  the oxygen on its vents' networks (each counted once), how many ticks that lasts at what the room
  alone draws (its leak if aired, its vents' drain if venting; null when a generator feeds it), what
  the generators on those networks make a period (`Air.LeakEvery` ticks), and why a room in vacuum gets
  no air (`atmosphere.noVent`, `atmosphere.notPiped`, `atmosphere.unpowered` — only unpowered
  generators behind it — or `atmosphere.dry`). One survey for all rooms, so the air overlay reads it
  four times a second, never every tick. It counts neither breathing nor other rooms on the same
  network.
- **Operations** (`IAtmosphereOps`): `Breathe(cell, amount)` — takes up to `amount` of air for a hero
  whose head is in `cell`, or a furnace's fire there, as above, if that cell holds air; returns how
  much, none anywhere else. Emptied tanks leave the list at once. Call it from the simulation phase,
  after atmosphere (crew and furnace run after it).
- **Commands:** `LoadCanister(Tank)`, refused with `atmosphere.noTank` (a generator too),
  `atmosphere.tankFull` or `items.notEnough`.
- **Fuzz:** mostly a canister into a tank the grid has, otherwise into ids 0–5, mostly no tank (the
  wreck's machines). It reads the grid's tanks, where the TypeScript generator, which cannot see the
  grid, aims at machine 1.

## Invariants

- A tank holds between nothing and its capacity; a generator, between nothing and its store. Only
  tanks holding oxygen are listed.
- Aired cells are sorted, distinct, and each in a room.
- The orders kept room by room are those found afresh for the whole ship, as after a load; so are
  the survey and the leaks kept: a world loaded from a save goes on with the same air.

## Differences from TypeScript

- TypeScript caches each room's leak in a `WeakMap` keyed by the room object; here the leaks are kept
  by room id while the rooms' `Version` stays, and found again for the ids `ChangedSince` names.
- TypeScript gathers the survey every tick; here it is kept between ticks (above). The orders'
  per-cell scratch is reused, as orders are found on every change of rooms.
- TypeScript's `changedRooms` compares the vents as sets, so a room whose vents only changed order
  (one taken apart and built again in the same cell) kept its old order; here the vents are compared
  room by room in order.

## Deliberately not handled

- Air does not thin or flow: a cell holds air or not. A door opened onto an empty room does not
  spill air through by itself; the vents fill it, nearest first.
- A vent in a venting room drains its whole network, aired rooms on it included: cut it with a valve.
- A wall facing a one-cell gap is no hull: the gap is a tiny room open to space, not space (see
  `Rooms`), so two modules built one cell apart do not leak through the walls they face.
- A wall's or back wall's material changed without its code (taken apart and built again in one
  tick, which no player can do: a plan claims its slot) reaches the leaks only once its room is
  found again.
- A generator needs nothing but power, cannot be switched off, and its surplus is lost even with a
  tank on its network: spilling into tanks comes with processing, before which no tank can be built.
- Oxygen in a tank taken apart is lost, not refunded as canisters. A valve under a tank is not worked
  with E.
- A cell aired by a fill and taken by a leak on the same tick stays aired (TypeScript's
  `applyEdits` keeps what was added), so that cell of the leak costs nothing: kept for parity; one
  cell a room a tick at most, only on the tick its network runs dry.

## Tests waiting for other modules

From `atmosphere.test.ts` and `air.test.ts`, ported with the test builder in place of `building`
(heroes breathe through `Crew`): the back wall built through a plan and the refund of one taken apart
wait for `building`.

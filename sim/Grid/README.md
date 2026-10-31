# Grid

## Files

| File | Purpose |
| --- | --- |
| `ChangeLog.cs` | Every write to the grid's layers, oldest first, as the version it was made under, the cell, the layer and what the … |
| `Doorways.cs` | What a doorway is and how its cells join and toggle, from its structure spec (0111, 0112). |
| `StructureRuns.cs` | Cells of one structure side by side along a row, one run: a launch platform's pieces, one deck with one lamp. |
| `Grid.cs` | The ship's cells in layers. |
| `Grown.cs` | What a save holds of a grown surface's grid (0077): its size and zone, its machines, and the cells changed since … |

The ship as cells in ship-local coordinates, layer by layer; rooms, pipes and air are read from here. Depends on nothing.

- **Size and zone:** the starting map's; the grid never grows. A new world is `Content.World`, 2048×1024, the wreck centred;
  the zone (`Layout.Zone`, 0048) is where anything may be built, the whole grid for a map that sets none.
- **Cells:** row by row from the top-left, `cell = y * width + x`. A cell is 8 px (0104); every cell is whole, no slopes (0106).
- **State** (`GridState`, members internal): the layers as `int[]` codes (`Structure`, `Material`, `Back`, `BackMaterial`),
  `Zone`, `Machines` (id order), `NextMachineId`. Saved as runs (`Runs`), so empty space costs nothing. Derived, rebuilt after
  a load: `Occupancy` (machine id per cell), `ById`, `ByKind`, `Version`, `Built` (the tight box of everything), `Log`.

## Layers

| Layer     | Holds |
| --------- | ----- |
| structure | a code of `Structure` (`sim/Content/Structures.cs`), a wall's with a material |
| machine   | machines from `Data.Machines`, each covering its footprint |
| back      | none, back wall (with a material): the wall behind a cell (0020) |

**Structures are data (0112).** Each code has a `StructureSpec`: its `Trait`s (`Blocks`, `Supports`, `Climbable`, `Seals`,
`BlocksLight`, `Terrain`, `Opening`, `BlocksFromBelow`), its leak, toggle, doorway group and join, build, map character and
looks. Rules ask the traits, never the code; `StructureCodeGuardTests` fails on a concrete code outside the table.

A ladder, a door segment and a hatch are pieces (0104): a 2×2, a 2×2, a 2×1 (`Builds.PieceOf`), every cell free, filled at
once. A machine needs every footprint cell free of structure and machines; a back wall only a free back slot.

## API

- **Queries** (`IGridQueries`): `Width`, `Height`, `Zone`, `Built`; `Version`, bumped on every change, 0 after a load;
  `ChangedSince(version)` — the cells and `GridLayers` that differ since then, ascending, or null when the log no longer
  reaches back (the reader then looks at everything, 0051); the layers live and read-only (`StructureLayer`, `MaterialLayer`,
  `BackLayer`, `BackMaterialLayer`); `StructureAt`, `WallAt` / `BackAt` (the `Material`, null for none), `GrownBack` (0084).
- **Traits per cell:** `Blocks`, `Supports`, `Climbable`, `Seals`, `BlocksLight`, and `Solid` (blocks or supports); past
  the grid everything blocks and is solid, nothing else. Code-only callers read `Structures.Is(code, trait)`.
- **Machines:** `MachineAt`, `MachineById`, `Machines`, `MachinesWith(trait)` (one `MachineTrait`, id order: what a system walks, 0113), `MachinesOf(kind)` (id order: one kind's own rule, the analyzer's), `PortsOf(machine)` (0109), `PortAt(cell)` (its way and fluid, typed-ports).
- **Pieces:** `Footprint(kind, cell)` — what a machine or piece would cover from that top-left, null when it does not fit;
  `PieceOf(cell)` — the cells taken apart with it: its build's piece, columns paired from its row's run of the same (a
  doorway's group), rows from its column's top; any other structure its own cell.
- `PlacementProblem(cell, placement)`: a machine with an `Exhaust` (the engine) also needs, in every row of its footprint, one
  wall or more left of it, then space up to its nozzle's tip or a cell past the wall: else `grid.noExhaust`
  ([the-engine-through-the-hull](../../docs/decisions/2026-10-25-the-engine-through-the-hull.md)); `AfterToggles(commands)` — the codes the `SetDoor`s among them would set, for a
  client's own toggles in flight (0014); not a copy of the layer.
- **Operations** (`IGridOps`): `Place(cell, placement)` (the build's code, `Structures.Placed`; a problem throws),
  `Clear(cell, layer)` (a whole machine), `SetDoorway(cell, open)` (a whole doorway as `SetDoor` sets it: Landing's
  platforms). Only in commands or construction.
- **Command:** `SetDoor(cell, open)` — sets the whole doorway the cell is in to the state its sender saw it should take
  (0071), each cell its `Toggle` within its group; already so, nothing changes, not even the version. Refused
  `grid.noDoor`, and on a launch platform, which only Landing opens (power gates it).
- **Refusals:** `grid.outOfBounds`, `grid.outsideZone`, `grid.occupied`, `grid.machineInTheWay`, `grid.blocked`,
  `grid.noSuchMaterial`, `grid.noSuchMachine`, `grid.noExhaust`, `grid.noPlatform` (a machine that `Lands` with the row
  under it not all launch platform), `grid.noGround` (off the ship, where only Landing places a pod, the row under it not all
  solid), `grid.underPlatform` (a structure or machine in a launch platform's shaft: under it, to the grid's edge),
  `grid.shaftBlocked` (a platform over a shaft not empty). `Taken(cell)`, `ClearBelow(cell)`: the shaft's test; `OnRun`.

## Maps

A structure's map character is its spec's `Char` (`#` wall, `=` floor, `H` ladder, `D`/`d` door, `h`/`o` hatch, `_`/`~` launch
platform, `%` the grate
fixture); `-` pipe, `v`/`x` valve, a machine by its spec's `Char`. A machine is drawn as its whole footprint. Every character
but a space has a back wall of the default hull. Machines take ids by their top-left cells in reading order.

Maps are drawn a character a 16 px cell and doubled (`Layout.Doubled()`): a character fills its 2×2 cells, a `Flat` structure
(floor, hatch) its top row only, `S` its bottom-left, a conduit its top-left toward its neighbours. A test of a rule per cell
reads its map a character a cell. A map may carry its cells' materials (`Layout.Materials`, 0077): a grown planet's or a
test's (`Legend.Materials`); code 0 is open sky with no back wall. A vein's cell (`Material.In`) has the stone it runs
through as its back wall, so digging it out leaves no second lump of ore.

**A grown grid saves its edits** (0077, `Grown.cs`): the cells that differ from the ground grown from the seed, ascending,
`cell, structure, material, back, backMaterial`; loading lays them on the ground grown again. A drawn map saves every layer.

## Invariants

- Machines never overlap and stand only on cells without structure; `Occupancy`, `ById` and `ByKind` match `Machines`.
- Machine ids only grow and are never reused.
- Every write goes through `Set`, which logs it and bumps `Version`; `ChangedSince(v)` names exactly what differs since `v`.
- Nothing is placed outside `Zone`; `Built` is the tight box of everything after any change and any load.
- Stored codes are the save format: never renumber them.
- A doorway is a door or a hatch (0111), its shape derived (`Doorways`): a door cell joins the door cells above and below
  (`Join.Column`) and the cell beside it within its segment; a hatch cell (`Join.Row`) only the cell beside it within its
  piece. Groups never join one another. The client draws, hears and lights doorways by the same joins.

## Deliberately not handled

- A grid that grows; taking apart what a map put outside the zone is allowed.
- Machines need no floor under them.
- An engine's exhaust is checked only when placed: what is later built in its nozzle's cells or taken from them leaves it standing.
- Door kinds with rules of their own (an airlock) would need a layer (0067).
- The log holds the last 65,536 writes (`ChangeLog.DefaultCapacity`); a tick writing more makes readers look at everything.


# Grid

The ship as cells in ship-local coordinates: what is built in each cell, layer by layer. Everything
else that stands on the ship — rooms, pipe networks, air — is read from here. Ported from
`packages/sim/src/modules/grid`; its rules hold here unchanged.

- **Depends on:** nothing.
- **Size:** set by the starting map; the grid does not grow.
- **Cells:** numbered row by row from the top-left, `cell = y * width + x`.
- **State** (`GridState`, members internal): the layers as `int[]` codes (`Structure`, `Conduit`,
  `Back`, and the materials of walls and back walls), `Machines`, `NextMachineId`. Derived:
  `Occupancy`, the id of the machine covering each cell, rebuilt after a load.

## Layers

| Layer     | Holds                                                                                     |
| --------- | ----------------------------------------------------------------------------------------- |
| structure | empty, hull wall (with a material), floor, ladder, door (open or closed)                  |
| conduit   | none, pipe, valve (open or closed); runs through walls and under machines                 |
| machine   | machines from `Data.Machines`, each covering its footprint                                |
| back      | none, back wall (with a material): the wall behind a cell, closing a room behind (0020)   |

Walls, floors, ladders and doors are one structure per cell. A machine needs every footprint cell free
of structure and of other machines. Conduits need only a free conduit slot, and a back wall only a
free back slot: it stands behind anything, and in space.

## API

- **Queries** (`IGridQueries`): `Width`, `Height`, `StructureLayer` and `BackLayer` (live, read-only),
  `StructureAt(cell)`, `MachineAt(cell)`, `Machines`, `PlacementProblem(cell, placement)`.
- **Operations** (`IGridOps`): `Place(cell, placement)` — the caller checks `PlacementProblem` first, a
  problem here throws; `Clear(cell, layer)` — for machines, removes the whole machine covering the
  cell. Only in commands or construction: modules reading the grid in derivation trust that nothing
  changes it after.
- **Commands:** `ToggleDoor(cell)` — opens or closes the whole doorway, every door cell 4-connected
  to `cell` taking the opposite of its state; refused with `grid.noDoor`. `ToggleValve(cell)`,
  refused with `grid.noValve`.
- **Refusals from `PlacementProblem`:** `grid.outOfBounds`, `grid.occupied`, `grid.machineInTheWay`,
  `grid.blocked`, `grid.noSuchMaterial`, `grid.noSuchMachine`.
- **Not yet ported** (added with the module that needs them): `layers`/`codes` for every layer,
  `backAt`, `conduitAt`, `portsOf`, `footprint`, `afterToggles`.

## Maps

`#` wall of `Data.DefaultHull`, `=` floor, `H` ladder, `D`/`d` door closed/open, `-` pipe, `v`/`x`
valve open/closed, machines by `Data.MachineOf`. A machine is drawn as its whole footprint; a map that
draws part of one does not load. Every character but a space has a back wall of the default hull,
hull walls included. A map's machines take ids 1, 2, … by their top-left cells in reading order.

## Invariants

- Machines never overlap, and stand only on cells without structure.
- `Occupancy` names exactly the machine covering each cell.
- Machine ids only grow; a removed machine's id is never reused.
- Stored codes are part of the save format: never renumber them.

## Deliberately not handled

- A grid that grows: its size is the map's.
- Machines need no floor under them.
- Doorways have no shape: any run of door cells is one.

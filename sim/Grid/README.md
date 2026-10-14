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

- **Queries** (`IGridQueries`): `Width`, `Height`; every layer as codes, a cell each, live and
  read-only, as TypeScript's `layers()`: `StructureLayer`, `MaterialLayer` (a wall's material, 0 for
  none), `ConduitLayer`, `BackLayer`, `BackMaterialLayer`; `StructureAt(cell)`, `ConduitAt(cell)`,
  `MachineAt(cell)`, `Machines`, `PortsOf(machine)` — the cells where conduits meet it, from its
  kind's port offsets —, `WallAt(cell)` and `BackAt(cell)` — the `Material` of the wall or back
  wall there, null for none —, `Footprint(kind, cell)` — the cells a machine of that kind would cover
  from that top-left, null when it is no machine or does not fit —, `PlacementProblem(cell, placement)`,
  `AfterToggles(commands)` — copies of the structure and conduit layers after the `ToggleDoor`s and
  `ToggleValve`s among the commands, each as it would apply and skipped where refused: what a client
  shows of its own toggles before their tick runs (0014).
- **Operations** (`IGridOps`): `Place(cell, placement)` — the caller checks `PlacementProblem` first, a
  problem here throws; `Clear(cell, layer)` — for machines, removes the whole machine covering the
  cell. Only in commands or construction: modules reading the grid in derivation trust that nothing
  changes it after.
- **Commands:** `ToggleDoor(cell)` — opens or closes the whole doorway, every door cell 4-connected
  to `cell` taking the opposite of its state; refused with `grid.noDoor`. `ToggleValve(cell)`,
  refused with `grid.noValve`.
- **Refusals from `PlacementProblem`:** `grid.outOfBounds`, `grid.occupied`, `grid.machineInTheWay`,
  `grid.blocked`, `grid.noSuchMaterial`, `grid.noSuchMachine`.
- TypeScript's `codes()`
  is the public `Structure`, `Conduit` and `Back` constants.

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

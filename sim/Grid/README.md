# Grid

The ship as cells in ship-local coordinates: what is built in each cell, layer by layer. Everything
else that stands on the ship — rooms, pipe networks, air — is read from here. Ported from
`packages/sim/src/modules/grid`; its rules hold here unchanged.

- **Depends on:** nothing.
- **Size:** set by the starting map; the grid does not grow. A new world is `Content.World`, 2048×1024,
  the wreck centred in it (`Layout.InWorld`, [0048](../../docs/decisions/0048-a-big-world-a-build-zone-inside-it.md)).
- **Zone:** the box where anything may be built, saved with the grid: the map's `Layout.Zone`, the
  world's middle 1024×512 for a new world, the whole grid for a map that sets none (tests, dev maps).
- **Cells:** numbered row by row from the top-left, `cell = y * width + x`.
- **State** (`GridState`, members internal): the layers as `int[]` codes (`Structure`, `Conduit`,
  `Back`, and the materials of walls and back walls), `Zone` (a `Box`), `Machines` (in id order), `NextMachineId`.
  Derived, rebuilt after a load and kept current by every machine placed or cleared: `Occupancy`, the
  id of the machine covering each cell; `ById`, every machine by its id; `ByKind`, each kind's
  machines in id order; `Version`; `Built`, the smallest box (`Box`, edges included) holding
  every cell with anything in any layer or under a machine: grown by every placement, and moved in
  past each edge line a removal leaves empty, so it stays tight; and `Log` (`ChangeLog.cs`), every
  write to a layer or a machine's cells, as the version it was made under, the cell, the layer and
  what the cell held before, empty after a load. A save
  holds each layer as runs, `[value, count, …]` (`Runs` in Core), so empty space costs a save and
  its hash nothing.
- **Files:** `Grid.cs` — the module, its state, queries and operations; `ChangeLog.cs` — the log of
  changed cells (0050).

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

- **Queries** (`IGridQueries`): `Width`, `Height`; `Version` — bumped on every change to any layer or
  machine, by a command or an operation, never saved and 0 again after a load: a module deriving from
  the grid (`rooms`, `pipes`, `atmosphere`) looks again only when it moved; `ChangedSince(version)` —
  the cells whose layers differ now from what they held at that version, ascending, each with the
  `GridLayers` that do (`Structure`, `Material`, `Conduit`, `Back`, `BackMaterial`, `Machine` — the
  machine covering the cell, by id): a cell changed and changed back is not named, and none while the
  version is current. Null when the log no longer reaches back to the version, or it is not one this
  grid has had (later than now; any version from before a load, which brings a new grid): the reader
  then looks at the whole grid again, as on its first look (0050). Every reader of the grid follows
  it — rooms, pipes, atmosphere; the client's tile view, light, starlight and pipes overlay — and
  keeps the version it last saw, and the grid with it; `Built` — the box past which every cell is
  empty space, never saved: `rooms` floods within it after a load, `rooms` walks space until it leaves
  it, and a reader the log let go of looks again within it; `Zone` — the build zone, nothing placed
  outside it; every layer as codes, a
  cell each, live and read-only, as TypeScript's `layers()`: `StructureLayer`, `MaterialLayer` (a
  wall's material, 0 for none), `ConduitLayer`, `BackLayer`, `BackMaterialLayer`; `StructureAt(cell)`, `ConduitAt(cell)`,
  `MachineAt(cell)`, `MachineById(id)` — null for none or one taken apart —, `Machines` — every
  machine in id order —, `MachinesOf(kind)` — one kind's, in id order, for the systems that work one
  kind (furnaces, analyzers, vents, tanks, generators) —, `PortsOf(machine)` — the cells where conduits meet it, from its
  kind's port offsets —, `WallAt(cell)` and `BackAt(cell)` — the `Material` of the wall or back
  wall there, null for none —, `Footprint(kind, cell)` — the cells a machine of that kind would cover
  from that top-left, null when it is no machine or does not fit —, `PlacementProblem(cell, placement)`,
  `AfterToggles(commands)` — the structure and conduit codes of just the cells the `ToggleDoor`s and
  `ToggleValve`s among the commands would change, each as it would apply and skipped where refused,
  every other cell as the world has it: what a client shows of its own toggles before their tick runs
  (0014). Not copies of the layers: those were 16 MB a tick on a 2048×1024 world while a toggle was in
  flight.
- **Operations** (`IGridOps`): `Place(cell, placement)` — the caller checks `PlacementProblem` first, a
  problem here throws; `Clear(cell, layer)` — for machines, removes the whole machine covering the
  cell. Only in commands or construction: modules reading the grid in derivation trust that nothing
  changes it after.
- **Commands:** `ToggleDoor(cell)` — opens or closes the whole doorway, every door cell 4-connected
  to `cell` taking the opposite of its state; refused with `grid.noDoor`. `ToggleValve(cell)`,
  refused with `grid.noValve`.
- **Refusals from `PlacementProblem`:** `grid.outOfBounds`, `grid.outsideZone` (the cell, or any cell of a
  machine's footprint, outside the zone), `grid.occupied`, `grid.machineInTheWay`,
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
- Machine ids only grow; a removed machine's id is never reused. `Machines` is in id order, and
  `ById` and `ByKind` hold exactly what a scan of it finds.
- Every change to a layer or a machine bumps `Version`, and every write to a layer or to `Occupancy`
  goes through one place that logs it (`Set`): nothing else writes them, so no change goes unlogged.
- `ChangedSince(v)` names exactly the cells and layers that differ between the grid at `v` and now, for
  every `v` the log still reaches; null for every other (`GridLogTests`, across logs trimmed to a write).
- Nothing is placed outside `Zone`: `Built` lies within it, unless the map drew something outside.
- `Built` is the smallest box holding every cell with a structure, conduit, back wall or machine, as
  a scan of them all finds it, after any change and after a load; `Box.None` when there is none.
- Stored codes are part of the save format: never renumber them.

## Deliberately not handled

- A grid that grows: its size is the map's. A world keeps the size it was made with when
  `Content.World` changes.
- Taking apart what stands outside the zone is allowed: only a map could put it there.
- Machines need no floor under them.
- Doorways have no shape: any run of door cells is one.
- The log holds the last 65,536 writes (`ChangeLog.DefaultCapacity`), a ring: a door, a breach or a
  hero's build writes a handful, so the sim's readers, which look every tick, and the client's, which
  look every frame or a few times a second, never come near it. A tick that writes more (no player
  can) leaves them to look at everything again, as after a load.
- A version names a state of one grid only: a load starts versions over in a new grid, and a reader
  keeping a version keeps the grid it read it from (the client's views are made anew with the world).

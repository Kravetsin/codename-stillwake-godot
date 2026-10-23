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
- **State** (`GridState`, members internal): the layers as `int[]` codes (`Structure`,
  `Back`, the materials of walls and back walls), `Zone` (a `Box`), `Machines` (in id order), `NextMachineId`.
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
  changed cells (0051).

## Layers

| Layer     | Holds                                                                                     |
| --------- | ----------------------------------------------------------------------------------------- |
| structure | empty, hull wall (with a material), floor, ladder, door (open or closed)                  |
| machine   | machines from `Data.Machines`, each covering its footprint                                |
| back      | none, back wall (with a material): the wall behind a cell, closing a room behind (0020)   |

Every cell is whole: there are no slopes ([0106](../../docs/decisions/0106-steps-replace-slopes.md), superseding
0069). Ground rises in steps of whole cells, which heroes and creatures step up.

Walls, floors, ladders and doors are one structure per cell. A ladder, a door and a hatch are placed as pieces (0104), a
ladder's 2×2, a door's 2×6, a hatch's 6×2 (`Builds.PieceOf`), every cell of one free of structure and machines, all of them
filled at once. A machine needs every footprint cell free
of structure and of other machines. A back wall needs only a free back slot: it stands behind anything, and in space.

## API

- **Queries** (`IGridQueries`): `Width`, `Height`; `Version` — bumped on every change to any layer or
  machine, by a command or an operation, never saved and 0 again after a load: a module deriving from
  the grid (`rooms`, `pipes`, `atmosphere`) looks again only when it moved; `ChangedSince(version)` —
  the cells whose layers differ now from what they held at that version, ascending, each with the
  `GridLayers` that do (`Structure`, `Material`, `Conduit`, `Back`, `BackMaterial`, `Machine` — the
  machine covering the cell, by id): a cell changed and changed back is not named, and none while the
  version is current. Null when the log no longer reaches back to the version, or it is not one this
  grid has had (later than now; any version from before a load, which brings a new grid): the reader
  then looks at the whole grid again, as on its first look (0051). Every reader of the grid follows
  it — rooms, pipes, atmosphere; the client's tile view, light, starlight and pipes overlay — and
  keeps the version it last saw, and the grid with it; `Built` — the box past which every cell is
  empty space, never saved: `rooms` floods within it after a load, `rooms` walks space until it leaves
  it, and a reader the log let go of looks again within it; `Zone` — the build zone, nothing placed
  outside it; every layer as codes, a
  cell each, live and read-only, as TypeScript's `layers()`: `StructureLayer`, `MaterialLayer` (a
  wall's material, 0 for none), `BackLayer`, `BackMaterialLayer`; `StructureAt(cell)`,
  `MachineAt(cell)`, `MachineById(id)` — null for none or one taken apart —, `Machines` — every
  machine in id order —, `MachinesOf(kind)` — one kind's, in id order, for the systems that work one
  kind (furnaces, analyzers, vents, tanks, generators) —, `PortsOf(machine)` — the cells where pipes meet it (`pipes`, 0109), from its
  kind's port offsets —, `WallAt(cell)` and `BackAt(cell)` — the `Material` of the wall or back
  wall there, null for none —, `GrownBack(cell)` — whether that back wall is a grown ground's own, unchanged since it
  grew, false aboard, on drawn maps and for one a hero built (0084) —, `Footprint(kind, cell)` — the cells a machine or a piece of that
  kind would cover from that top-left, null when it is neither or does not fit —, `PieceOf(cell)` — the cells of the piece the
  structure there is part of, taken apart as one (0104), ascending: a door cell's whole doorway, a ladder cell's two by two (its
  columns paired from the left end of its row's run of ladder, its rows from the top of its column's), any other structure its
  own cell, none for none —, `PlacementProblem(cell, placement)`,
  `AfterToggles(commands)` — the structure codes of just the cells the `SetDoor`s among the commands
  would change, each as it would apply and skipped where refused,
  every other cell as the world has it: what a client shows of its own toggles before their tick runs
  (0014). Not copies of the layers: those were 16 MB a tick on a 2048×1024 world while a toggle was in
  flight.
- **Operations** (`IGridOps`): `Place(cell, placement)` — the caller checks `PlacementProblem` first, a
  problem here throws; `Clear(cell, layer)` — for machines, removes the whole machine covering the
  cell. Only in commands or construction: modules reading the grid in derivation trust that nothing
  changes it after.
- **Commands:** `SetDoor(cell, open)` — opens or shuts the whole doorway, the straight run of door cells
  `cell` is in (`Doorways`, 0067), every cell of it, a part-open one too; refused with `grid.noDoor`. It says the state its sender saw it should take, not a flip (0071): a door
  already so is left alone, the grid's version too, so two peers opening one door at once leave it open.
- **Refusals from `PlacementProblem`:** `grid.outOfBounds`, `grid.outsideZone` (the cell, or any cell of a
  machine's footprint, outside the zone), `grid.occupied`, `grid.machineInTheWay`,
  `grid.blocked`, `grid.noSuchMaterial`, `grid.noSuchMachine`.
- TypeScript's `codes()`
  is the public `Structure`, `Conduit` and `Back` constants.

## Maps

`#` wall of `Data.DefaultHull`, `=` floor, `H` ladder, `D`/`d` door closed/open, `-` pipe, `v`/`x`
valve open/closed, machines by `Data.MachineOf`. The old slopes' `/` `\` `r` `7` are gone (0106): a map using them does
not parse. A machine is drawn as its whole footprint; a map that
draws part of one does not load. Every character but a space has a back wall of the default hull,
hull walls included. A map's machines take ids 1, 2, … by their top-left cells in reading order.

**A cell is 8 px** ([0104](../../docs/decisions/0104-the-cell-is-8-px.md)); maps are drawn a character a 16 px cell and doubled
(`Layout.Doubled()`): a character's 2×2 cells all its wall, back wall, machine, ladder or door, so a doorway is two cells thick
as its 16 px door was one; a floor
its top row; `S` its bottom-left; a conduit its top-left and the cells toward neighbouring conduits, one cell wide, meeting the
doubled ports. The wreck, the test strip, the ruins' prefabs and test scenarios are doubled; a test
of a rule per cell reads its map a character a cell.

A map may come with its cells' materials (`Layout.Materials`, 0077): a planet's ground grown from its seed
(`Terrain`), or a test's material map (`Layout.WithMaterials`, `Legend.Materials`: `s` soil, `t` stone, `b` bedrock, `a` the ruins' alloy, 0085, `p` the homes' plaster, 0091).
A wall and the back wall behind a cell are then of that cell's material; a code of 0 is open sky, with no back wall.
No code is negative (0106).
Natural materials (`Material.Natural`) are walls like any other to the grid: what digs them is building's (0077).

**A grown grid saves its edits** (0077, `Grown.cs`): on a layout grown from a seed (`Layout.Grown`), the grid notes what each
cell held as grown on its first write (derived: `_base`, never saved) and a save holds, in place of its layers, a
`GrownGrid`: its size, zone, machines, next machine id and `edits`, the cells that differ now from the ground as grown,
ascending, each `cell, structure, material, back, backMaterial` (four layers after the cell since 0109, pipes being spans). Loading, it is handed the ground grown
again from the surface's seed and lays the edits on it. A cell changed and changed back is no edit, so every peer saves
and hashes the same edits whatever its history (`GrownSaveTests`). A drawn map (the ship, the strip, tests) saves every
layer as runs, as ever.

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
- A doorway is one straight run, `Doorways.Thick` (2) cells thick (0104), its shape derived, never stored (`Doorways`, 0067): a
  door cell in a run of more than two door cells up and down stands in a door; every other lies in a hatch. A cell joins the
  door cells above and below it and those either side that stand as it does, so a door's two columns are one doorway, a
  hatch's two rows another, and a hatch run stops beside a door. The client draws, hears and lights doorways by the same joins.

## Deliberately not handled

- A grid that grows: its size is the map's. A world keeps the size it was made with when
  `Content.World` changes.
- Taking apart what stands outside the zone is allowed: only a map could put it there.
- Machines need no floor under them.
- Door kinds: one with rules of its own (an airlock, a blast door) would need a layer here (0067).
- The log holds the last 65,536 writes (`ChangeLog.DefaultCapacity`), a ring: a door, a breach or a
  hero's build writes a handful, so the sim's readers, which look every tick, and the client's, which
  look every frame or a few times a second, never come near it. A tick that writes more (no player
  can) leaves them to look at everything again, as after a load.
- A version names a state of one grid only: a load starts versions over in a new grid, and a reader
  keeping a version keeps the grid it read it from (the client's views are made anew with the world).

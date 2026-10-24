# Pipes

## Files

| File | Purpose |
| --- | --- |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `PipeNetworks.cs` | How pipe networks are found from the spans, and how a map's drawn pipes become spans (0109). |
| `Pipes.cs` | Pipes as spans (0109): straight pipes between machines' ports and joints, laid and taken away by hand with the … |

Pipes as spans (ADR 0109): straight pipes between machines' ports and joints, laid and taken away by hand with the
wrench, valves on joints, and the networks they join, along which air flows between the machines on them. Shaped as
`power`'s cables are; the per-cell conduit layer of the grid is gone.

- **Depends on:** `grid` (`IGridQueries`: `Machines`, `PortsOf`, `MachineAt`, `Zone`, `ChangedSince`), `items`
  (`IItemsQueries`, `IItemsOps`: what a span or a valve costs, given back).
- **State** (`PipesState`, members internal): `Spans` — every `PipeSpan(A, B, Paid)`, its node cells the lower first,
  ordered by its ends; `Valves` — every `Valve(Cell, Open, Paid)`, by cell. Saved. Derived: `NetworkOf` (node cell → its
  network), `Networks` in id order, the grid's version last looked at, `Version`, and `Stale` (a span or valve changed).
- **Files:** `Pipes.cs` — the module, its records, queries and commands; `PipeNetworks.cs` — the networks from the spans,
  and a map's drawn pipes as spans; `Fuzz.cs` — its fuzz generator.

## Rules

- **A node** is a cell a span ends at: a machine's port (`IGridQueries.PortsOf`: a vent's own cell, a tank's bottom-left)
  or a **joint**, any other cell of the build zone, a wall, a back wall, open space or a machine's other cell alike. A
  joint comes with the first span laid to it and goes with the last cut. A port whose machine is taken apart stays a
  joint; a machine built with a port on a node joins it.
- **A span** joins two different cells of the zone straight, at any angle, at most `Content.Piping.MaxSpan` (12) long;
  two cells take one span at most. Its length is the square root of the cells' squared distance, an integer, rounded up
  (`LengthOf`: 3 by 4 is 5, 1 by 1 is 2). It costs one `Content.Piping.Item` (a tube) per started
  `CellsPerItem` (4) cells, taken from the sender as it is laid, given back as it is cut; in creative mode it costs
  nothing and gives nothing back (`Paid`). It takes no cell: it passes through walls and seals nothing.
- **A valve** is a joint that opens and closes (`MakeValve(cell, true)`, `Content.Piping.ValveCost`, open as made;
  `MakeValve(cell, false)` turns it back, refunded). Only a joint becomes one, never a port. `SetValve(cell, open)` says
  the state its sender saw it should take (0071): one already so is left alone. Its last span cut, a valve goes with it,
  refunded.
- **A network** is a component of the nodes that carry, joined by spans; a closed valve carries nothing, so it splits its
  spans apart and is in no network. Its id is its smallest node cell; its cells are its nodes in order; its machines,
  every machine with a port among them, by id. Found again in the derivation phase when a span or valve changed, or a
  machine came or went (the grid's log, 0051); never in commands.
- **Maps** draw pipes as cells (`-`, `v` open, `x` closed, `-` under machines): every drawn pipe cell that is a valve, a
  machine's port, an end, a corner or a branch is a node, every straight run between two nodes a span, cut by a joint every
  12 cells from its start (`PipeNetworks.FromLayout`). A lone pipe joining nothing is dropped. They count as paid. The
  machines a drawing connected are connected the same (`DoubledTests`).

## API

- **Queries** (`IPipesQueries`): `NetworkAt(cell)`, `NetworksOf(machineId)`, `Network(id)`, `Networks`, `Version` as
  before; `Spans`, `Valves`, `ValveAt(cell)`; `LayProblem(peer, a, b)` and `ValveProblem(peer, cell, valve)` — why that
  command would be refused, for the wrench's ghost; `AfterToggles(commands)` — whether each valve the `SetValve`s among
  them name would be open, for what a client shows before its tick runs (0014). Static: `LengthOf(width, a, b)`,
  `CostOf(length)`.
- **Commands:** `LayPipe(A, B)` — `pipes.notAboard` (away from the ship, 0110), `pipes.offGrid`, `pipes.sameCell`, `pipes.outsideZone`, `pipes.tooLong`, `pipes.twice`,
  then the items' `items.notEnough`; `CutPipe(A, B)` — `pipes.noPipe`, then whether the refund fits; `MakeValve(Cell,
  Valve)` — making one, `pipes.notAboard`, `pipes.noJoint`, `pipes.already`, `pipes.noValve`, then the items; `SetValve(Cell, Open)` — `pipes.noValve`.

## Invariants

- Every node not a closed valve is in exactly one network, shared with every carrying node a span joins it to.
- Every valve stands on a joint some span ends at.
- What the networks are depends on the spans, valves and machines alone: the same after a load as before it.

## Deliberately not handled

- **Identity.** Network ids are smallest cells, so shutting a valve splits a network and opening one merges two.
- **Meteors** break walls, not pipes: a span passes through a wall and stays (0109).
- **Spans crossing** do not join: only shared ends do.
- **Taking a machine apart** with spans on its ports is allowed: its ports stay joints.

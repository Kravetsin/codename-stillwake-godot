# Pipes

Pipe networks: the runs of pipes that carry oxygen between tanks and vents, found from the grid's
conduit layer and machines. Ported from `packages/sim/src/modules/pipes`; its rules hold here
unchanged.

- **Depends on:** `Grid`, through `IGridQueries` (`ConduitLayer`, `Machines`, `PortsOf`).
- **State:** nothing saved. Derived: `NetworkOf` per cell (a network's id, or `PipeNetworks.None`), the
  networks in id order, and the copies of the conduit layer and of the machines (id and cell each)
  they were found from.
- **Files:** `PipeNetworks.cs` — the rules below, on plain arrays; `Pipes.cs` — the module.

## Rules

- Pipes and open valves carry; a closed valve does not, so it splits a run in two and belongs to no
  network. Conduits join side by side, never corner to corner.
- A network is a component of the cells that carry; its id is its smallest cell.
- A machine joins every network one of its ports stands on (`IGridQueries.PortsOf`): a vent through
  its own cell, a tank through its bottom-left. A pipe under another cell of a machine does not
  connect it.
- Networks are found again on the tick a conduit or machine changes: a system in the derivation phase
  compares the conduit layer and the machines with its copies every tick. Read networks in systems at
  or after derivation, never in commands or construction.

## API

- **Queries** (`IPipesQueries`): `NetworkAt(cell)` — a network's id, or null; `NetworksOf(machineId)`
  — the networks a machine has a port on, in id order; `Network(id)`; `Networks` in id order, each a
  `Network(Id, Cells, Machines)` with cells in cell order and machines by id. `Networks` is the same
  list until a conduit or machine changes: a reader may compare it by reference.
- **Commands, operations, events:** none.

## Invariants

- Every pipe and open valve is in exactly one network, shared with every pipe and open valve beside it;
  closed valves and empty cells are in none.
- What the networks are depends on the grid alone: the same after a load as before it.

## Deliberately not handled

- **Identity.** Ids are smallest cells, so closing a valve splits a network and opening one merges two;
  nothing links networks across a rebuild. Atmosphere keys oxygen on what it owns (tanks), not on
  network ids.
- Pipes only: wires join this when they arrive and make it generic.
- A rebuild is a full pass over the grid, on each conduit or machine change.

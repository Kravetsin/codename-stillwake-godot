# Building

Plans, and the work that turns them into the ship. A plan is a blueprint, which puts something in a
cell (a wall, a pipe, a tank) once what it costs has been delivered to it and heroes have worked on
it, or a mark on what a layer of a cell holds, taken out by heroes' work. Both complete through
`Grid`'s operations, paid from and refunded to the inventories in `Items`. Ported from
`packages/sim/src/modules/building`; its rules hold here unchanged.

- **Depends on:** `Grid` (`IGridQueries`, `IGridOps`) and `Items` (`IItemsQueries`, `IItemsOps`);
  `Power` (`IPowerQueries.Cabled`), to keep a cabled machine standing; `Research`
  (`IResearchQueries.Knows`), to refuse a blueprint not known (ADR 0024).
- **State** (`BuildingState`, members internal): `Plans` in the order they were made, each a `Plan`
  with its `Progress` in ticks of work — a blueprint (`Place` set) with what was `Delivered` to it so
  far, as `Stack`s in the order first given, or a mark (`Clear`, the layer, set); `NextPlanId`. All
  saved; nothing is derived.
- **Files:** `Rules.cs` — which plans may be made and worked; `Costs.cs` — what things cost, from
  content, and what a blueprint still lacks; `Building.cs` — the module, its commands and queries;
  `Fuzz.cs` — its fuzz generator; `State.cs` — its types.

## Rules

- A blueprint is checked against what is known first (`building.unknown`, but in creative mode), then
  against the grid (`IGridQueries.PlacementProblem`, whose keys pass through unchanged), then against
  the other plans: in each cell, structure and machines share one solid slot, and a conduit and a
  back wall each have their own, as in the grid. A machine's blueprint covers its whole footprint.
  Placing one costs nothing.
- A mark needs something in that layer of that cell, and one thing takes one mark. A machine holding
  items (a crate, a furnace) is marked only empty, refused with `building.notEmpty`; a machine a cable
  ends on, only with its cables cut, refused with `building.cabled`. Both are checked again when the
  mark is worked. A machine's mark is kept at the machine's top-left, whichever of its cells was
  marked. A marked thing is still built, so the grid refuses a plan for that slot until it is gone.
- **`Work` is the left button held on a plan.**
  - On a blueprint still lacking something it takes from the sender, item by item, the lesser of
    what the blueprint lacks and what the sender carries, and adds it to `Delivered`; no work is
    done. Refused with `items.notEnough` when the sender carries none of what is lacking. Several
    heroes may deliver to one, each paying only what is still lacking. The client sends one per
    blueprint per hold of E, so sweeping the pointer delivers to every blueprint it passes.
  - On a blueprint with all it costs, or on a mark, it is one tick of one hero's work, sent every
    tick the player holds E on it within reach. Two heroes on one plan work it twice as fast, and
    work stopped keeps its progress. The tick that reaches its ticks completes it: a blueprint
    takes its build's `BuildTicks` (`Materials` for walls and back walls, `Builds`, `Machines`) and is
    built; a mark takes those of what it names divided by `Builds.DeconstructSpeedup`, rounded up, and
    takes it apart, refunding its whole cost to the sender, what does not fit falling on the floor at
    it (`IItemsOps.GiveOrDrop`).
  - In creative mode a blueprint lacks nothing: it is worked as it is, taking nothing.
- **Cancelling** a blueprint lets what was delivered fall on the floor at it, dropped by whoever
  cancels (`IItemsOps.DropAt`); cancelling a mark drops nothing.
- What a thing costs and takes: its material's for a wall or a back wall (`Materials`: scrap for
  scrap, plates for iron), `Builds` and `Machines` otherwise.
- Anyone may cancel or work on any plan.
- A mark whose thing is gone (a meteor broke it) is dropped in the derivation phase of that tick,
  refunding nothing; worked on that tick, it completes with nothing to refund.

## API

No module depends on building, so it has no query interface: the client reads `Registry.Building`.
A cost is `IReadOnlyList<(string Item, int Count)>`, as `Items` takes it.

- **Queries:** `Plans` — live, read-only; `PlansAt(cell)` — every plan covering the cell;
  `Problem(cell, placement)` and `Problem(cell, layer)` — the refusal a `PlacePlan` or a
  `Deconstruct` would get, for the client's ghost; `ClearAt(cell, layers?)` — what a removal takes
  from a cell next, of the layers given or all (a machine, then a conduit, then the structure, then
  the back wall, passing over what is marked already; when all is marked, the top one) and where its
  mark is kept, as `(Layer, Cell)`, null for nothing there; `WorkProblem(planId, peer)` — the refusal
  that peer's `Work` would get; `Lacking(planId)` — what a blueprint still lacks before work may
  begin; `TicksFor(planId)` — the ticks a plan takes in all, null for no plan; `CostOf(planId)` —
  what a blueprint costs in all, null for a mark or no plan; `CostFor(placement)` — what placing it
  costs, for the build menu.
- **Commands:** `PlacePlan(Cell, Placement)`, refused with `building.unknown`, grid's keys or
  `building.planned`; `Deconstruct(Cell, Layer)`, refused with `building.nothingThere`,
  `building.planned`, `building.notEmpty` or `building.cabled`; `CancelPlan(Id)`, refused with
  `building.noPlan`; `Work(Plan)`, refused with `building.noPlan`, `items.notEnough`,
  `building.notEmpty` or `building.cabled`.
- **Fuzz:** every command, refused ones too: placements of every kind at any cell or one off the
  grid; marks mostly on the wreck's built cells (cells drawn from the wreck as drawn, as in
  TypeScript, which cannot see the grid); cancels of ids that are and are not there; `Work` mostly on
  the oldest plan or a mark, so plans complete within a run.

## Invariants

- No two plans share a slot of a cell; no two marks share a thing.
- Plan ids only grow and are never reused.
- A blueprint's `Delivered` never exceeds its cost, and it is worked only with all of it delivered.
- A plan's progress stays below its ticks: the tick that reaches them completes it.
- Items are conserved across inventories, the floor, blueprints and what is built.

## Deliberately not handled

- Plans left stale by a grid change: plans claim their slots, so nothing can fill one first.
- No reach is checked in the sim: the client sends E only on plans within its hero's reach.
- A blueprint finished on the cell its hero stands in puts the hero inside what was built.
- Taking apart what is built needs no knowledge of it (ADR 0024): its refund is its cost.

## Differences from TypeScript

- `problem(cell, action)` is two overloads, `Problem(cell, placement)` and `Problem(cell, layer)`;
  a plan is one record with `Place` or `Clear` set, where TypeScript has a union.
- `Delivered` is a list of `Stack`s, where TypeScript has an object; a cost is a list of pairs.
- `Deconstruct` is a record with a constructor, not a positional one: a positional record would
  generate a `Deconstruct` method, which the type's own name forbids.

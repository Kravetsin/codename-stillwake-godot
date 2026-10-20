# Building

Plans, and the work that turns them into the ship. A plan is a blueprint, which puts something in a
cell (a wall, a pipe, a tank) as heroes work on it, paying its cost an item a part as they go
(ADR 0063), or a mark on what a layer of a cell holds, taken out by heroes' work. Both complete through
`Grid`'s operations, paid from and refunded to the inventories in `Items`. Ported from
`packages/sim/src/modules/building`; its rules hold here but for the payment a part at a time.

- **Depends on:** `Grid` (`IGridQueries`, `IGridOps`) and `Items` (`IItemsQueries`, `IItemsOps`; `WeaponProblem` for a dig); `Crew`
  (`ICrewQueries.Heroes`), so a wall does not finish on a hero;
  `Power` (`IPowerQueries.Cabled`), to keep a cabled machine standing; `Research`
  (`IResearchQueries.Knows`), to refuse a blueprint not known (ADR 0024); `Crafting`
  (`ICraftingQueries.Queue`), to keep a station with crafts queued standing (0057).
- **State** (`BuildingState`, members internal): `Plans` in the order they were made, each a `Plan`
  with its `Progress` in ticks of work — a blueprint (`Place` set) with what was `Delivered` to it so
  far, an item a part begun, as `Stack`s in the order first given, or a mark (`Clear`, the layer, set);
  `NextPlanId`; `Cracks` (0077), the natural walls dug part way, each a `Crack(Cell, Damage)`, sorted by cell. All saved;
  nothing is derived. Paying a part at a time needed no new field: what was paid
  and the work done were saved already, and an older save's blueprint, paid in full or in part before any
  work, works on.
- **Files:** `Rules.cs` — which plans may be made and worked; `Costs.cs` — what things cost, from
  content, what a blueprint still lacks and the parts its work is paid in; `Building.cs` — the module, its
  commands and queries; `Digging.cs` — `Dig` and the cracks (0077); `Fuzz.cs` — its fuzz generator; `State.cs` — its types.

## Rules

- **A surface's build list** (0076): a blueprint for a placement its surface's kind does not allow
  (`Surfaces.Kind.Builds`, by the placement's kind: `wall`, `ladder`, `furnace`) is refused `building.notHere`, after
  `building.fixed` and before anything else, creative mode or not. The ship allows every one; the test strip hull
  walls, back walls, floors, ladders and doors. Marks and work are not asked: nothing else can be built there.
- A blueprint is checked against what is known first (`building.unknown`, but in creative mode), then
  against the grid (`IGridQueries.PlacementProblem`, whose keys pass through unchanged), then against
  the other plans: in each cell, structure and machines share one solid slot, and a conduit and a
  back wall each have their own, as in the grid. A machine's blueprint covers its whole footprint.
  Placing one costs nothing.
- **No blueprint builds bedrock or a natural back wall** (0077): either is refused `building.unknown`, creative mode or
  not (`Blueprints.Has`). Soil and stone walls are blueprints like any wall, known once soil or rock is analysed.
- A machine built into the ship (`Fixed` in `Machines`: the lifepod) is never planned nor marked,
  refused with `building.fixed`, creative mode or not; it is no blueprint.
- **Natural ground is dug, never marked** (0077): a mark on a natural wall or back wall is refused `building.natural`.
- A mark needs something in that layer of that cell, and one thing takes one mark. A machine holding
  items (a crate, a furnace) or a station with crafts queued is marked only empty, refused with
  `building.notEmpty`; a machine a cable
  ends on, only with its cables cut, refused with `building.cabled`. Both are checked again when the
  mark is worked. A machine's mark is kept at the machine's top-left, whichever of its cells was
  marked. A marked thing is still built, so the grid refuses a plan for that slot until it is gone.
- **`Work` is the left button held on a plan:** one tick of one hero's work, sent every tick the player
  holds E on it within reach. Two heroes on one plan work it twice as fast, and work stopped keeps its
  progress. The tick that reaches its ticks completes it: a blueprint takes its build's `BuildTicks`
  (`Materials` for walls and back walls, `Builds`, `Machines`) and is built; a mark takes those of what
  it names divided by `Builds.DeconstructSpeedup`, rounded up, and takes it apart, refunding its whole
  cost to the sender, what does not fit falling on the floor at it (`IItemsOps.GiveOrDrop`).
- **A blueprint is paid a part at a time** (ADR 0063). Its work is split into as many parts as its cost
  has items: a scrap wall's 2, a vent's 2 (a plate, a tube), a tank's 5. Part k of N begins at tick
  `BuildTicks * k / N`, rounded down, so the first begins with the first tick and a scrap wall's 13
  ticks split 6 and 7; content gives every part a tick at least (`ContentTests`).
  - The tick that begins a part not paid for yet first takes one item from the sender into
    `Delivered`: the first item in the cost's order that the blueprint still lacks and the sender
    carries. So a vent is paid its plate, then its tube; a hero carrying only the tube pays it first.
  - A hero carrying none of what is still lacking is refused there with `items.notEnough`, the work
    stopping at the part's start and what was put in staying. Within a part paid for, anyone works on,
    carrying nothing. Each hero pays from their own inventory, whoever reaches the part.
  - In creative mode a blueprint lacks nothing: it is worked as it is, taking nothing.
- **Cancelling** a blueprint, however far it was built, lets exactly what was put in fall on the floor at
  it, dropped by whoever cancels (`IItemsOps.DropAt`); cancelling a mark drops nothing.
- **A wall may be planned cut to a shape** (0069, part 2): the placement carries it (`Placement.Shape`), the grid
  refuses one on anything but a wall (`grid.notShapeable`), and the wall is built cut to it. It costs, takes and gives
  back what a whole wall of its material does. A wall's shape is changed by taking it apart and building it again.
- What a thing costs and takes: its material's for a wall or a back wall (`Materials`: scrap for
  scrap, plates for iron), `Builds` and `Machines` otherwise.
- Anyone may cancel or work on any plan.
- A mark whose thing is gone (a meteor broke it) is dropped in the derivation phase of that tick,
  refunding nothing; worked on that tick, it completes with nothing to refund.
- **A wall does not finish on a hero** (0077): `Work` on a wall plan whose next tick would complete it is refused
  `building.heroInside` while a hero's reported cell (`ICrewQueries.Heroes`, the body's middle), or the one above or under
  it, is the plan's cell. Planning there is allowed; it finishes once the hero steps out. Building reads crew only as
  commands apply, so it keeps its place in the run order before crew.
- **Cracks heal** (0077): each keeps the tick of its last hit by Building's own clock (`Clock`, saved, ticked in
  derivation); one not hit for `Builds.CrackHeals` ticks is dropped, the wall whole again.
- **`Dig(Cell, Tool)` is a weapon's hit on natural ground** (0077), sent as the axe strikes or at the drill's pace: its
  damage (`Weapons`) is added to the cell's crack, and the hit that reaches the wall's `Strength` clears the wall —
  only the wall: its back wall stays — drops its crack and gives what it is made of (its material's `Cost`: soil, rock)
  to the sender, what does not fit falling at the cell (`GiveOrDrop`). Refused `building.nothingToDig` with no natural
  wall there (open ground, hull, off the grid), `building.bedrock` for one that never breaks, then as the weapon says
  (`IItemsQueries.WeaponProblem`: `items.flat`, and `items.notRunning` for an electric one its hero does not run). Two
  digs on one cell in a tick apply in the host's order: the one that breaks it gets the yield, the next finds nothing.
  A crack whose wall is gone (a meteor) is dropped in derivation. A tool that deals no damage is our bug and throws.

## API

No module depends on building, so it has no query interface: the client reads `Registry.Building`.
A cost is `IReadOnlyList<(string Item, int Count)>`, as `Items` takes it.

- **Queries:** `Plans` — live, read-only; `PlansAt(cell)` — every plan covering the cell;
  `Problem(cell, placement)` and `Problem(cell, layer)` — the refusal a `PlacePlan` or a
  `Deconstruct` would get, for the client's ghost; `ClearAt(cell, layers?)` — what a removal takes
  from a cell next, of the layers given or all (a machine, then a conduit, then the structure, then
  the back wall, passing over what is marked already; when all is marked, the top one) and where its
  mark is kept, as `(Layer, Cell)`, null for nothing there; `WorkProblem(planId, peer)` — the refusal
  that peer's `Work` would get; `Lacking(planId)` — what a blueprint still lacks of its cost, nothing in
  creative mode; `Parts(planId)` — `(In, Of)`: the parts of a blueprint put in, of all its parts, those paid
  for or, in creative mode, those its work began, null for a mark or no plan;
  `TicksFor(planId)` — the ticks a plan takes in all, against which the client draws a blueprint built as far as its `Progress`, null for no plan; `CostOf(planId)` —
  what a blueprint costs in all, null for a mark or no plan; `RefundOf(planId)` — what taking apart what a mark
  names gives back, the same `Costs.BuiltAt` its last tick refunds, for its tip: null for a blueprint or no plan; `CostFor(placement)` — what placing it
  costs, for the build menu; `Cracks` — live, read-only, by cell; `DigProblem(peer, cell, tool)` — the refusal a `Dig`
  would get, leaving out whether an electric weapon runs, as the client asks before it sends.
- **Commands:** `PlacePlan(Cell, Placement)`, refused with `building.fixed`, `building.notHere`, `building.unknown`,
  grid's keys or `building.planned`; `Deconstruct(Cell, Layer)`, refused with `building.nothingThere`,
  `building.fixed`, `building.planned`, `building.notEmpty` or `building.cabled`; `CancelPlan(Id)`, refused with
  `building.noPlan`; `Work(Plan)`, refused with `building.noPlan`, `items.notEnough`,
  `building.notEmpty` or `building.cabled`; `Dig(Cell, Tool)`, refused with `building.nothingToDig`, `building.bedrock`,
  `items.flat` or `items.notRunning`. A mark is refused `building.natural` besides.
- **Fuzz:** every command, refused ones too: placements of every kind, walls cut to every shape and a shape on
  what may have none, mostly within 16 cells of
  what the grid has built, as players build, now and then at any cell of the grid or one off it, so
  the build zone's edge is drawn too; marks mostly within what is built (TypeScript draws from the
  wreck as drawn, which in the big world of 0048 would miss the ship); cancels of ids that are and
  are not there; `Work` mostly on the oldest plan or a mark, so plans complete within a run; `Dig` with every weapon,
  half the time on a crack, so walls break within a run, else anywhere, which on a small planet is mostly its ground; on
  grown ground half of all its draws are digs.

## Invariants

- No two plans share a slot of a cell; no two marks share a thing.
- Plan ids only grow and are never reused.
- A blueprint's `Delivered` never exceeds its cost, and, but in creative mode, its work never passes the
  end of the parts paid for: with k of N parts paid, `Progress <= BuildTicks * k / N`.
- A plan's progress stays below its ticks: the tick that reaches them completes it.
- A crack lies only on a natural wall that breaks, its damage above 0 and below the wall's strength; the cracks are
  sorted by cell, one a cell. A wall broken gives exactly what it is made of, once.
- Items are conserved across inventories, the floor, blueprints and what is built, but for what
  drifts off the grid from space (Items' floor rules): a delivery cancelled, or a refund that does not
  fit, out in space, or in a wall with no room within `Floor.PushReach`. One in the hull is pushed
  out into the room beside it.

## Deliberately not handled

- Half-dug cells never heal (0077): a crack stays until its wall breaks. Reach and which cell is dug are the
  client's word, as for work.
- Plans left stale by a grid change: plans claim their slots, so nothing can fill one first. Only
  building's own work places anything in the grid (hazards only clear), so a blueprint finds its slot
  as it left it, and completing one over something throws: our bug. The tests' instant `Builder`
  (`sim/Tests/Scenario.cs`) refuses a planned slot too, with `building.planned`.
- No reach is checked in the sim: the client sends E only on plans within its hero's reach.
- A blueprint finished on the cell its hero stands in puts the hero inside what was built.
- Taking apart what is built needs no knowledge of it (ADR 0024): its refund is its cost.
- A blueprint part built is nothing in the grid until its last tick: a half-built wall holds no air,
  blocks no light and stops no one (accepted, 0063).
- Creative mode, a dev panel switch, turned off on a blueprint worked past what was paid: each tick
  after pays an item until the parts paid catch up with the work; no rule makes up the difference.

## Differences from TypeScript

- `problem(cell, action)` is two overloads, `Problem(cell, placement)` and `Problem(cell, layer)`;
  a plan is one record with `Place` or `Clear` set, where TypeScript has a union.
- `Delivered` is a list of `Stack`s, where TypeScript has an object; a cost is a list of pairs.
- A blueprint is paid an item a part as it is worked (0063); TypeScript took all it lacked on the first
  press, before any work.
- `Deconstruct` is a record with a constructor, not a positional one: a positional record would
  generate a `Deconstruct` method, which the type's own name forbids.

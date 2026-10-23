# Building

Plans, and the work that turns them into the ship. A plan is a blueprint, which puts something in a
cell (a wall, a ladder, a tank) as heroes work on it, paying its cost an item a part as they go
(ADR 0063), or a mark on what a layer of a cell holds, taken out by heroes' work. Both complete through
`Grid`'s operations, paid from and refunded to the inventories in `Items`. Ported from
`packages/sim/src/modules/building`; its rules hold here but for the payment a part at a time.

- **Depends on:** `Grid` (`IGridQueries`, `IGridOps`) and `Items` (`IItemsQueries`, `IItemsOps`; `WeaponProblem` for a dig); `Crew`
  (`ICrewQueries.Heroes`), so a wall does not finish on a hero, and `ICrewOps.Shock`, the resonator's zap (0091) and a relic's cradle's (0096);
  `Power` (`IPowerQueries.Cabled`), to keep a cabled machine standing; `Research`
  (`IResearchQueries.Knows`), to refuse a blueprint not known (ADR 0024); `Crafting`
  (`ICraftingQueries.Queue`), to keep a station with crafts queued standing (0057).
- **State** (`BuildingState`, members internal): `Plans` in the order they were made, each a `Plan`
  with its `Progress` in ticks of work — a blueprint (`Place` set) with what was `Delivered` to it so
  far, an item a part begun, as `Stack`s in the order first given, or a mark (`Clear`, the layer, set);
  `NextPlanId`; `Cracks` (0077), the natural walls dug part way and the plants of some strength hit part way (by
  their own cells, 0080), each a `Crack(Cell, Damage, At)`, sorted by cell; `Felled`
  (0079), the cells of the plants felled since the ground grew, ascending — the caves' growth among them (0080). All saved.
  Derived (`Flora.cs`): the plants the layout grew with (`Layout.Flora`; on a load, those of the ground grown again), the
  ones standing — every one not felled — each by its own cell, and each cell a standing plant stands in. Paying a part at a time needed no new field: what was paid
  and the work done were saved already, and an older save's blueprint, paid in full or in part before any
  work, works on.
- **Files:** `Rules.cs` — which plans may be made and worked; `Costs.cs` — what things cost, from
  content, what a blueprint still lacks and the parts its work is paid in; `Building.cs` — the module, its
  commands and queries; `Digging.cs` — `Dig` and the cracks (0077); `Flora.cs` — what grows on a planet's surface (0079) and in its caves (0080); `Fuzz.cs` — its fuzz generator; `State.cs` — its types.

## Rules

- **A surface's build list** (0076): a blueprint for a placement its surface's kind does not allow
  (`Surfaces.Kind.Builds`, by the placement's kind: `wall`, `ladder`, `furnace`) is refused `building.notHere`, after
  `building.fixed` and before anything else, creative mode or not. The ship allows every one; the test strip hull
  walls, back walls, floors, ladders and doors. Marks and work are not asked: nothing else can be built there.
- A blueprint is checked against what is known first (`building.unknown`, but in creative mode), then
  against the grid (`IGridQueries.PlacementProblem`, whose keys pass through unchanged), then against
  the other plans: in each cell, structure and machines share one solid slot, and a back wall has its
  own, as in the grid. A machine's blueprint covers its whole footprint, and a piece's (0104): a
  ladder's 2×2, a door's 2×6, a hatch's 6×2 (`Builds.Hatch`, built as a door is, of its blueprint and at its cost).
  Placing one costs nothing.
- **No blueprint builds bedrock** (0077), front or back: refused `building.unknown`, creative mode or not
  (`Blueprints.Has`). Soil and stone walls and back walls (0084) are blueprints like any, known once soil or rock is analysed.
- A machine built into the ship (`Fixed` in `Machines`: the lifepod) is never planned nor marked,
  refused with `building.fixed`, creative mode or not; it is no blueprint.
- **Natural ground is dug, never marked** (0077): a mark on a natural wall, or on the ground's own back wall
  (`IGridQueries.GrownBack`), is refused `building.natural`. A soil or stone back wall a hero built is marked and taken
  apart for its item as any back wall (0084).
- A mark needs something in that layer of that cell, and one thing takes one mark. A machine holding
  items (a crate, a furnace), a module fitted (`IItemsQueries.FittedTo`, 0086) or a station with crafts queued is marked
  only empty, refused with
  `building.notEmpty`; a machine a cable
  ends on, only with its cables cut, refused with `building.cabled`. Both are checked again when the
  mark is worked. A machine's mark is kept at the machine's top-left, whichever of its cells was
  marked; a ladder's or a door's at the first cell of its piece (`IGridQueries.PieceOf`, 0104), and taking it apart takes the
  whole piece: a doorway whole, a ladder's two by two. It gives back its build's cost for as many pieces as its cells make, each
  item rounded down: two doors side by side give back two, a drawn map's lone door (2×2) a third of one, a ladder piece a
  meteor broke nothing. A marked thing is still built, so the grid refuses a plan for that slot until it is gone.
- **`Work` is the left button held on a plan with the screwdriver in hand** (0107): one tick of one hero's work, sent
  `Screwdriver.Work` times a tick the player holds it on the plan within reach, the client's pace (the sim does not check the
  screwdriver, as it does not a weapon, 0034). Two heroes on one plan work it twice as fast, and work stopped keeps its
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
  damage for its sender (`IItemsQueries.Damage`: the weapon's, boosted by its tool's module, 0098) is added to the cell's crack, and the hit that reaches the wall's `Strength` clears the wall —
  only the wall: its back wall stays — drops its crack and gives what it is made of (its material's `Cost`: soil, rock)
  to the sender, then the `Yields` of every growth the wall held — the one in its own stone, the one standing on it, the
  one hanging from it — felled there and then, before derivation's sweep would fell them for nothing; what does not fit
  falls where each was (`GiveOrDrop`). Refused `building.builtWall` for a wall of a ship's material (not `Natural`: scrap,
  iron; 0108), taken apart only by a mark; `building.nothingToDig` with no wall there (open ground, off the grid), `building.bedrock` for one that never breaks, then as the weapon says
  (`IItemsQueries.WeaponProblem`: `items.flat`, and `items.notRunning` for an electric one its hero does not run). Two
  digs on one cell in a tick apply in the host's order: the one that breaks it gets the yield, the next finds nothing.
  A crack whose wall or plant is gone (a meteor) is dropped in derivation. A tool that deals no damage is our bug and throws.
- **`Dig(Cell, Tool, Back: true)` hits the back wall** (0107, the right button): a natural one (0108), a hero's soil
  or the ground's own, cracks by its material's `Strength` in a crack of its own (`Crack.Back`, kept after the front's of its cell,
  healed and swept alike, never saved when false) and at its strength is cleared, the wall in front staying, its material's `Cost`
  given to the sender, what hangs on it (`Flora.Anchor.Back`) felled for them. Refused `building.nothingToDig` with no back wall
  (off the grid too), `building.builtWall` for a ship's (scrap, iron: a ship comes apart only by marks and the screwdriver),
  `building.bedrock` for one of a material nothing breaks (bedrock, lava), then as the weapon says.
- **What grows on a planet's surface** (0079): plants and stones (`Content.Flora`), grown from the seed with the ground
  (`Terrain`), each standing in its cell over the soil it grows from, `Across` columns (2 at the least, 0104) and `Tall` cells up
  from it; one in the rock a block of two by two walls. A `Dig` on any of those
  cells, open (no structure), adds the weapon's damage to the plant's crack, kept by the plant's own cell whichever of its
  cells was hit, healing as a wall's does; reaching its kind's `Strength` (an acacia 12, a rock 8, a bush 4; 0 for flowers,
  grass and every cave growth, which fall to any hit) fells it, its crack dropped, giving its kind's `Yields` to the
  sender (an acacia 10 planks, a bush a seed, a rock 2 rock; flowers and grass nothing). Felled any other way, its crack
  goes with it, so a wall built in its cell never takes it over. A plant whose ground is gone — no wall under its cell, dug, taken apart, a
  meteor — or whose cell holds something now is felled in derivation, the tick it happens, looked at only when the grid's
  version moved. A felled plant never grows back; only `Felled` is saved, so a planet's save grows by a number a plant.
- **What grows in a planet's caves** (0080): the caves' kinds of `Content.Flora.Cave`, grown with the ground too, each holding on
  to something (`Flora.Anchor`): on a floor, standing in the open cell over the wall and `Tall` cells up; from a ceiling,
  hanging in the open cell under the wall and `Tall` cells down; in the rock, lying in a stone wall's own cell beside a cave
  (moss, mica, veins, fossils, a geode, the heat's cracks). One in open cells falls to a hit on any of them, as a plant does; one
  in the rock is not hit itself — a `Dig` there digs the stone, as on any wall — and goes the tick the stone breaks. Any whose
  hold is gone (`HoldOf`: the wall under it, over it, or its own) is felled in derivation; looked at only round the cells the
  grid's log names since the last look (`ChangedSince`), all of them when it names none (a first look, a load). They are many —
  some thirty-eight thousand on the first planet — and their cells go in `Felled` like a plant's. Felled by a hit or by
  digging out their hold, they give fixed `Yields`: teal and hanging caps 2 biomass, violet caps a spore, ice crystals an
  ice and ice veins 2, the amber, rose and ember crystals a crystal and a geode 6, copper veins 2 copper ore, mica a mica,
  the heat's cracks 2 magnetite.
- **A growth felled by nobody lets its yields fall** (0085), every kind alike, the surface's and the caves': built over (a floor
  or a ladder in its cell), its hold gone to a meteor — whatever the sweep fells — drops its `Yields` at its cell for anyone
  (`DropAt(FloorStack.Nobody, …)`). Its cell is in `Felled` from then, so a load never fells or drops it again.
- **The ruins' growths** (0085): a spire's cache, standing on its alloy floor (alloy scrap 4, wire 6, silicon 2), and its lens in
  its tip's top cell (nothing), are cave kinds no band grows (`Content.Ruins`), set by `Terrain` where it stamps a spire, and felled
  as any. Its relic's cradle is a piece (0096).
- **The homes' pieces** (0091): `Content.Ruins.Pieces`, growths (`Flora.Piece`) set by `Terrain` where it stamps a home, felled,
  saved and dropped as any. What they need every growth has: `Across` columns from its own cell (the bottom-left on a floor, the
  top-left else) beside its `Tall` rows, a hit on any of them hitting it, its crack kept by its own cell, the wall under (over)
  each of its columns holding it, any of them dug felling it for the digger; `Anchor.Back`, hung on the back wall of its cells,
  felled when its back wall goes or its own cell is built over; `Behind` (a back wall's pieces, a rug), kept in a layer of their
  own, so a cell holds one piece in front and one behind, `PlantAt` and a hit taking the front one first; `Fixed` (the mural, the
  crack), which no hit takes and `PlantAt` never names. A floor's piece may stand on the front piece under it, a table or a
  stool: felling that one fells what stands on it, its yields to the same hero or let fall for anyone. The fallen beam and the
  rubble lie in two walls each of the home's alloy and stone, which block the way; any of them dug fells the piece.
- **The resonator zaps** (0091): every breath (`Crew.BreathEvery` of this module's clock, in the simulation phase, before crew
  breathes) each hero not out whose reported cell lies within `Ruins.ZapReach` (1) of a standing resonator's cells pays
  `Ruins.Zap` (5) of its suit through `ICrewOps.Shock`. The resonators standing are derived (`_resonators`), by own cell.
- **The relic** (0096): a spire's cradle (`Ruins.Cradle`, three across and up, 8 hit points) gives four `arcShard`, a home's niche
  (`Ruins.Niche`) one. Every `Dig` on a standing cradle, the felling one too, first shocks its sender through `ICrewOps.Shock`:
  `Ruins.CradleShock` (5) for the axe's hit, a faster tool's its pace's share of the axe's, rounded up (`ShockOf`: the drill 1, a module
  in it or not), 10 units a second whatever strikes. Its floor dug out from under it fells it for the digger, unshocked. `Relics` reads,
  for every relic the ground grew (`Ruins.Relics`), whether it is taken from `Felled` alone, so a load and a friend joining agree.

## API

No module depends on building, so it has no query interface: the client reads `Registry.Building`.
A cost is `IReadOnlyList<(string Item, int Count)>`, as `Items` takes it.

- **Queries:** `Plans` — live, read-only; `PlansAt(cell)` — every plan covering the cell;
  `Problem(cell, placement)` and `Problem(cell, layer)` — the refusal a `PlacePlan` or a
  `Deconstruct` would get, for the client's ghost; `ClearAt(cell, layers?)` — what a removal takes
  from a cell next, of the layers given or all (a machine, then the structure, then
  the back wall, passing over what is marked already; when all is marked, the top one) and where its
  mark is kept, as `(Layer, Cell)`, null for nothing there; `WorkProblem(planId, peer)` — the refusal
  that peer's `Work` would get; `Lacking(planId)` — what a blueprint still lacks of its cost, nothing in
  creative mode; `Parts(planId)` — `(In, Of)`: the parts of a blueprint put in, of all its parts, those paid
  for or, in creative mode, those its work began, null for a mark or no plan;
  `TicksFor(planId)` — the ticks a plan takes in all, against which the client draws a blueprint built as far as its `Progress`, null for no plan; `CostOf(planId)` —
  what a blueprint costs in all, null for a mark or no plan; `RefundOf(planId)` — what taking apart what a mark
  names gives back, the same `Costs.BuiltAt` its last tick refunds, for its tip: null for a blueprint or no plan; `CostFor(placement)` — what placing it
  costs, for the build menu; `Cracks` — live, read-only, by cell; `Plants` — the plants standing, `Plant(Cell, Kind)`,
  ascending by cell, live and read-only, only ever fewer; `PlantAt(cell)` — the plant a hit there takes, the one in front first, then the one behind, never a fixed one (one in the rock in
  its stone's), null for none; `HoldOf(plant)` — the cell it holds on to in its own column (0080); `Zaps(cell)` — whether a hero
  reported there stands right by a resonator (0091); `Relics` — every relic grown, `Relic(Cell, Kind, Taken)`, by cell (0096);
  `ShockOf(tool)` — what a hit of it on a cradle costs its striker; `DigProblem(peer, cell, tool, back)` — the refusal a `Dig`
  would get, leaving out whether an electric weapon runs, as the client asks before it sends.
- **Commands:** `PlacePlan(Cell, Placement)`, refused with `building.fixed`, `building.notHere`, `building.unknown`,
  grid's keys or `building.planned`; `Deconstruct(Cell, Layer)`, refused with `building.nothingThere`,
  `building.fixed`, `building.planned`, `building.notEmpty` or `building.cabled`; `CancelPlan(Id)`, refused with
  `building.noPlan`; `Work(Plan)`, refused with `building.noPlan`, `items.notEnough`,
  `building.notEmpty` or `building.cabled`; `Dig(Cell, Tool, Back)`, refused with `building.builtWall` (a ship's wall or back wall), `building.nothingToDig` (no wall nor plant there; no back wall, `Back`), `building.bedrock`,
  `items.flat` or `items.notRunning`. A mark is refused `building.natural` besides.
- **Fuzz:** every command, refused ones too: placements of every kind, mostly within 32 cells of
  what the grid has built, as players build, now and then at any cell of the grid or one off it, so
  the build zone's edge is drawn too; marks mostly within what is built (TypeScript draws from the
  wreck as drawn, which in the big world of 0048 would miss the ship); cancels of ids that are and
  are not there; `Work` mostly on the oldest plan or a mark, so plans complete within a run; `Dig` with every weapon, a quarter of them on the back layer,
  a quarter of the time on a plant (one in an open cell sought over three more draws, for most of a cave's lie in rock) or on
  what holds it (0079, 0080), half the rest on a crack, so walls break within a run, else anywhere, which on a small planet is mostly its ground; on
  grown ground half of all its draws are digs.

## Invariants

- No two plans share a slot of a cell; no two marks share a thing.
- Plan ids only grow and are never reused.
- A blueprint's `Delivered` never exceeds its cost, and, but in creative mode, its work never passes the
  end of the parts paid for: with k of N parts paid, `Progress <= BuildTicks * k / N`.
- A plan's progress stays below its ticks: the tick that reaches them completes it.
- A crack lies only on a natural wall that breaks, its damage above 0 and below the wall's strength; the cracks are
  sorted by cell, one a cell. A wall broken gives exactly what it is made of, once.
- Every standing plant's hold is a wall and, but for one in the rock, its cell is open, after derivation; `Plants` and
  `Felled` are disjoint and together the plants the ground grew with (`FloraTests`, `CaveFloraTests`).
- Items are conserved across inventories, the floor, blueprints and what is built, but for what
  drifts off the grid from space (Items' floor rules): a delivery cancelled, or a refund that does not
  fit, out in space, or in a wall with no room within `Floor.PushReach`. One in the hull is pushed
  out into the room beside it.

## Deliberately not handled

- Plants on a drawn map (only tests draw them) are not brought back by a load: only a grown surface's ground grows again.
- A plant's crown over a wall built into it, or a tree's trunk cells above its own: only its own cell holding something fells
  it. A hit plant shows no cracks: the client shakes it and drops a few leaves instead (0080).
- A cave growth's other cells, past its own, holding something now (a wall built into hanging threads) leave it standing, as a
  plant's crown does. Moss shows on whichever faces of its stone are open now: dig beside it and it shows there too (0080).
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

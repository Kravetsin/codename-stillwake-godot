# Building

## Files

| File | Purpose |
| --- | --- |
| `Building.cs` | Plans and the work turning them into the ship. |
| `Costs.cs` | Costs, ticks to build or repair; what a blueprint lacks. |
| `Digging.cs` | A weapon's hits on a planet's ground (`Hew`, `HewCells`). |
| `Flora.cs` | Plants, stones, cave growths and ruins' pieces (0079, 0080, 0091). |
| `Fuzz.cs` | The commands the desync suite draws. |
| `Repairs.cs` | Repairing a machine a magnetic flare broke (`Repair`). |
| `Rules.cs` | Which plans may be made and worked. |
| `State.cs` | The saved slice: `Plan`, `Crack`, `Repairing`, `BuildingState`. |

A blueprint puts something in a cell, a mark takes something apart.
Also digging, what grows (0077, 0079, 0080), the ruins' pieces (0085, 0091, 0096) and repairs (magnetic-flare).

- **Depends on:** `Grid` (queries, ops); `Items` (queries, ops); `Crew` (`Heroes`; `Shock`, 0091, 0096); `Power`
  (`Unplug`, `Plug`, 0110; `IsBroken`, `Mend`); `Research` (`Knows`, 0024); `Crafting` (`Queue`, 0057); `Pipes` (`SeatProblem`). Creatures reads it (`ShotTakes`, `ShotKey`) and changes it (`Hit`).
- **State** (`BuildingState`, all saved): `Plans` in the order made, each with `Progress` in ticks and, for a blueprint,
  `Delivered` stacks in the order first given; `NextPlanId`; `Cracks`, sorted by cell (`Crack(Cell, Damage, At, Back)`);
  `Felled`, cells of growths felled since the ground grew, ascending; `Clock`; `Instant`; `Repairs` paid, by machine. Derived (`Flora.cs`): the growths grown
  and standing, by cell; the resonators standing.

## Rules

- **Blueprint checks, in order:** `building.fixed` (the lifepod, 0028); `building.needsAxe` (a pile, marked); `building.notHere`
  (`Surfaces.Kind.Builds`, 0076); `building.unknown` (unknown but to a creative sender, 0024; bedrock and machines with no card always, 0077); the
  grid's `PlacementProblem` keys; plans' shaft keys too; `building.noFloor` (machines-stand: `Footing` under a
  standing machine, built or, not instant, planned); `building.planned`; ports (`SeatProblem`, `pipes.*`). Structure
  and machines share a slot, back walls their own. A machine covers its footprint, a ladder or door segment 2×2, a hatch
  2×1 (0104, 0111). Soil and stone walls are blueprints too (0084).
- **Marks** (no ADR): one thing takes one mark and needs something in that layer; none at all where the surface's tools
  break things (`ToolsBreak`, a planet: `building.notHere`, its old marks not worked, fan-of-rays). A
  machine holding items, a fitted module (0086) or queued crafts (0057) is marked only empty, `building.notEmpty`,
  checked again when worked, as is `building.holdsMachine` (under a standing machine). A machine's mark is kept at its top-left, a piece's at its first cell (`PieceOf`, 0104);
  taking it apart takes the whole piece. Its cables stay on a peg (`Unplug`); a machine built is `Plug`ged in (0110).
- **`Work`** is one tick of one hero's work, sent `Screwdriver.Work` times a tick while a builder is held on a
  plan (0107, power-driver); no tool checked. A blueprint takes its `BuildTicks`; a mark takes those
  divided by `Builds.DeconstructSpeedup`, rounded up (no ADR), and refunds the whole cost to the sender, the rest
  falling at it (`GiveOrDrop`, 0008); a piece's cost goes per piece, each item rounded down.
- **Paid a part at a time** (0063): N parts for N cost items, part k starting at tick `BuildTicks * k / N`, rounded
  down. The tick that starts an unpaid part takes from the sender's stock (own slots, then crates) the first item in cost
  order still lacking, else is refused `items.notEnough`. Within a paid part anyone works; a creative worker pays from nothing.
- **Repair** (free-repair): a tick of work as `Work`, free; the last `Mend`s. A broken machine refunds in full.
- **Instant** (`SetInstant`, dev): blueprints built free, removals refunded, at once; new surfaces keep it.
- **Cancelling** a blueprint drops exactly what was put in (`DropAt`); a mark drops nothing (0063). Anyone may cancel
  or work any plan; a mark whose thing a meteor broke is dropped in derivation, refunding nothing.
- **A wall does not finish on a hero** (0077): the `Work` that would complete a wall (instant: placing) is refused `building.heroInside`
  while its cell is in a standing body round a hero's reported middle (3 rows up, 2 down, 1 aside). Building runs before crew.
  So does a last tick the grid, `SeatProblem` or a floor still unlaid refuses.
- **`Hew(Tool, X, Y, Heading)`** (fan-of-rays), only where `ToolsBreak` (else `building.notHere`): the weapon's fan
  (`Rays`) from the hand; a ray stops at a wall or what stops a body, striking it if it breaks (not bedrock or lava; a
  piece by its first cell), and passes ladders, open doors and plants, striking them (`Struck`).
  `HewCells(Tool, Cells)` (tool-edge) strikes each listed cell as a ray does, once.
  Each takes `BlockDamage` (0098) once (0: none, no shock), a pile `Damage` at its `Hp` (pile-hit-points): at its strength a wall
  goes for its `Cost` and held growths, a piece whole.
  `building.nothingToDig`, then `items.flat` / `items.notRunning`. `Hit`, a shot's, strikes one cell `ShotTakes` allows,
  true if it broke, its yield the shooter's here, else anyone's. No tool hits back walls.
- **Cracks heal** (0077): one not hit for `Builds.CrackHeals` ticks of Building's `Clock` is dropped, as, in derivation,
  one whose wall or plant is gone.
- **Growths** (0079, 0080) grow from the seed with `Terrain`, each held by an `Anchor` (floor, ceiling, rock, back).
  A hit on any open cell of one cracks it by its own cell; at its kind's `Strength` it is felled for its `Yields`; one in
  the rock goes with its stone. Hold gone or own cell built over: felled in derivation (0051). Nothing grows back.
- **Felled by nobody** (0085): built over or lost to a meteor, a growth drops its `Yields` at its cell, anyone's.
- **Ruins** (0085, 0091): caches, lenses and the homes' pieces are growths set by `Terrain`. `Behind` pieces have a
  layer of their own, the front hit first; `Fixed` ones no hit takes; a piece standing on another falls with it.
- **The resonator** (0091): every breath, before crew breathes, a hero within `Ruins.ZapReach` pays `Ruins.Zap` (`Shock`).
- **The relic** (0096): every `Hew` striking a cradle first shocks its sender once (`ShockOf`, a faster tool its pace's
  share); its floor dug out fells it unshocked. `Relics` reads taken-ness from `Felled` alone.
- **Posts** (`Flora.Post`): rock pieces a guard is tied to; `FellPost` for a kill.

## Commands

`PlacePlan(Cell, Placement)`, `Deconstruct(Cell, Layer)` (`building.nothingThere` besides), `CancelPlan(Id)`
(`building.noPlan`), `Work(Plan)` (`building.noPlan`, `items.notEnough` and the keys above),
`Hew(Tool, X, Y, Heading)`, `HewCells(Tool, Cells)`, `SetInstant(On)`, `Repair(Machine)` (`building.notBroken`). Previews ask
`Problem`, `WorkProblem`, `HewProblem`, `HewCellsProblem`, `Struck`, `RepairProblem`.

## Invariants

- No two plans share a slot of a cell; no two marks share a thing. Plan ids only grow.
- `Delivered` never exceeds the cost; with k of N parts paid, `Progress <= BuildTicks * k / N`.
  Progress stays below its ticks: the tick that reaches them completes it.
- A crack lies only on something that breaks, damage above 0 and below its strength, one a cell and layer, by cell.
  A wall broken gives what it is made of, once.
- After derivation every standing growth's hold is there; `Plants` and `Felled` are disjoint and together what grew.
- Items are conserved across inventories, floor, blueprints and what is built, but for what drifts off the grid.

## Deliberately not handled

- Growths on a drawn map are not brought back by a load: only a grown surface's ground grows again.
- Only a growth's own cell built over fells it, not its crown's (0080).
- Reach, a fan's hand, a shot's muzzle and which plan is worked are the client's.
- A blueprint finished on its hero's cell holds the hero.
- Taking apart needs no knowledge (0024).
- A part-built blueprint is nothing in the grid till its last tick (0063).
- An old save's plan past what was paid: each later tick pays an item.

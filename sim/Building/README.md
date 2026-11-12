# Building

## Files

| File | Purpose |
| --- | --- |
| `Building.cs` | Plans and the work turning them into the ship. |
| `Costs.cs` | Costs, ticks to build or repair; what a blueprint lacks. |
| `Bursts.cs` | `Burst`: a burst's wave and what it breaks. |
| `Digging.cs` | Hits of tools and shots (`Hew`, `HewCells`, `Hit`). |
| `Flora.cs` | Plants, stones, cave growths and ruins' pieces. |
| `Fuzz.cs` | The commands the desync suite draws. |
| `Repairs.cs` | Repairing a machine a magnetic flare broke (`Repair`). |
| `Rules.cs` | Which plans may be made and worked. |
| `Targets.cs` | `TargetAt`: what a hit strikes; the one damage rule. |
| `State.cs` | The saved slice: `Plan`, `Crack`, `Repairing`, `BuildingState`. |

- **Depends on:** `Grid`, `Items` (queries, ops); `Crew` (`Heroes`; `Shock`, `Loot`, `Works`); `Power`
  (`Unplug`, `Plug`; `IsBroken`, `Mend`); `Research` (`Knows`); `Crafting` (`Queue`, `Scrap`); `Pipes` (`SeatProblem`). Creatures uses `ShotTakes`, `Surplus`, `Hit`, `Burst`, Hazards `Burst`; Places, `Wipe`.
- **State** (`BuildingState`, all saved): `Plans` in order made, each with `Progress` ticks and, for a blueprint,
  `Delivered` stacks in first-given order; `NextPlanId`; `Cracks`, sorted by cell (`Crack(Cell, Damage, At, Back)`);
  `Felled` growths' cells, ascending; `Clock`; `Instant`; `Repairs` paid, by machine. Derived (`Flora.cs`): growths grown and
  standing, by cell; resonators.

## Rules

- **Blueprint checks, in order:** `building.fixed` (the lifepod); `building.needsAxe` (a pile, marked); `building.notHere`
  (`Surfaces.Kind.Builds`, 0076); `building.unknown` (but to a creative sender; bedrock, cardless machines always); the
  grid's `PlacementProblem` keys; plans' shaft keys too; `building.noFloor` (machines-stand: `Footing` under a
  standing machine, built or, not instant, planned); `building.planned`; ports (`SeatProblem`, `pipes.*`).
  A machine covers its footprint, a ladder or door segment 2×2, a hatch 2×1. Soil and stone walls are blueprints too.
- **Marks**: one thing takes one mark and needs something in that layer; none where
  `ToolsBreak` (`building.notHere`, old ones not worked). A
  machine holding items, a fitted module or queued crafts is marked only empty, `building.notEmpty`,
  checked again when worked, as is `building.holdsMachine` (under a standing machine). A mark is kept at a machine's top-left, a piece's first cell (`PieceOf`);
  taking it apart takes the piece. Its cables stay on a peg (`Unplug`); a machine built is `Plug`ged in.
- **`Work`**, sent `Screwdriver.Work` a tick while a builder is held on it (power-driver), no tool checked, is worth the
  sender's `Works` (food-and-the-body), 0 to 2 ticks; a second never finishes a plan nor begins an unpaid part. A blueprint takes its `BuildTicks`; a mark those
  over `Builds.DeconstructSpeedup`, rounded up, refunding the whole cost to the sender, the rest
  falling at it (`GiveOrDrop`); a piece's cost goes per piece, each item rounded down.
- **Paid a part at a time** (0063): N parts for N cost items, part k starting at tick `BuildTicks * k / N`, rounded
  down. The tick that starts an unpaid part takes from the sender's stock (own slots, then crates) the first item in cost
  order still lacking, else is refused `items.notEnough`. Within a paid part anyone works; a creative worker pays from nothing.
- **Repair** (free-repair): worth what a `Work` is, free; the last tick `Mend`s. A broken machine refunds in full.
- **Instant** (`SetInstant`, dev): blueprints built free, removals refunded.
- **Cancelling** a blueprint drops exactly what was put in (`DropAt`); a mark drops nothing. Anyone may cancel
  or work any plan; a mark on what broke drops in derivation, refunding nothing.
- **A wall does not finish on a hero** (0077): its last `Work` (instant: placing) is refused `building.heroInside` while its
  cell is in a body round a hero's reported middle (building runs before crew); so is one the grid, `SeatProblem` or an unlaid floor refuses.
- **A hit's target** (hit-targets) is `TargetAt` alone: wall, machine, piece, plant; `Key`, `Strength` (null: never), `Wreck`.
- **One damage rule** (one-damage-rule): a player's hit on their ship (`OwnShip`: `Aboard`, never a pile) takes a tenth
  (`OwnShipTakes`), all else in full: tools, shots, carries, bursts.
- **`Hew(Tool, X, Y, Heading)`** (fan-of-rays): the weapon's
  `Rays` from the hand; a ray stops at a wall or what stops a body, striking it if it breaks, passing and striking
  ladders, open doors, machines and plants (`Struck`). `HewCells(Tool, Cells)`: each cell once.
  Each takes `BlockDamage` once (0: none, no shock), a pile `Damage`: at its strength a wall goes for its
  `Cost` and held growths, a piece whole; `Carries`% of the surplus to the next cell on the line from the hand (`HewCells`: the
  hero's), the larger, once. `building.nothingToDig`, then `items.flat` / `items.notRunning`. `Hit`, a shot's, strikes a cell
  `ShotTakes` allows, returning its surplus if it broke, the yield the shooter's here, else anyone's. No tool hits back walls.
- **`Burst(peer, cell, power, reach, Spoils)`** (damage-scale): a `Wave`, steps `Damage.Step`; a cell costs its
  strength left (a pile's read as tenths), a piece or machine once, by the one rule; bedrock, lava, fog, the lifepod stop
  it. Paid ones break, unpaid crack at their head by what was left.
  `Drop` (a charge): all for anyone, `Crushed` if set; `Lost` (a meteor): costs gone, and back walls entered where what is
  left pays. A machine broken by a hit `Spill`s and `Scrap`s (`Unbuild`), cables on a peg; a net lays its cost down; a felled cradle
  shocks `peer` once.
- **Cracks heal** (0077): dropped once `Builds.CrackHeals` ticks unhit, or in derivation once their wall or plant is gone.
- **Growths** (0079, 0080) grow from the seed with `Terrain`, each held by an `Anchor`.
  A hit on any open cell of one cracks it by its own cell, felled at its kind's `Strength` for its `Yields`; one in
  the rock goes with its stone. Hold gone or own cell built over: felled in derivation. Nothing grows back.
- **Felled by nobody** (0085): built over or burst, its `Yields` drop at its cell.
- **Ruins** (0085, 0091): caches, lenses and the homes' pieces are growths set by `Terrain`. `Behind` pieces have a
  layer of their own, the front hit first; `Fixed` ones no hit takes; a piece standing on another falls with it.
- **The resonator** (0091): each breath, before crew, a hero in `Ruins.ZapReach` pays `Ruins.Zap` (`Shock`).
- **The relic** (0096): a `Hew` striking a cradle first shocks its sender once (`ShockOf`, by pace);
  its floor dug out fells it unshocked. `Relics` reads taken-ness from `Felled` alone.
- **Posts** (`Flora.Post`): rock pieces tied to a guard; `FellPost` on a kill.

## Commands

`PlacePlan(Cell, Placement)`, `Deconstruct(Cell, Layer)` (`building.nothingThere` besides), `CancelPlan(Id)`
(`building.noPlan`), `Work(Plan)` (`building.noPlan`, `items.notEnough` and the keys above),
`Hew`, `HewCells`, `SetInstant(On)`, `Repair(Machine)` (`building.notBroken`). Previews:
`Problem`, `WorkProblem`, `HewProblem`, `HewCellsProblem`, `Struck`, `RepairProblem`.

## Invariants

- No two plans share a slot of a cell; no two marks share a thing. Plan ids only grow.
- `Delivered` never exceeds the cost; with k of N parts paid, `Progress <= BuildTicks * k / N`.
  Progress stays below its ticks: the tick that reaches them completes it.
- A crack lies only on what breaks, damage above 0 and below its strength, one a cell and layer, by cell.
  A wall broken yields at most once.
- After derivation every standing growth's hold is there; `Plants` and `Felled` are disjoint and together what grew.
- Items are conserved, but what drifts off the grid or a `Wipe` takes.

## Deliberately not handled

- A load brings back no drawn map's growths.
- Reach, a fan's hand, a muzzle, the plan: the client's.
- A blueprint finished on its hero holds it.
- A machine whose floor a burst breaks stands on.
- Taking apart needs no knowledge.
- A part-built blueprint is nothing in the grid till done.
- An old save's plan past what was paid: each later tick pays an item.

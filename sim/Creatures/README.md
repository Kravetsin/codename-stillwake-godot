# Creatures

## Files

| File | Purpose |
| --- | --- |
| `Creatures.cs` | The commands (`Strike`, `HitBy`, `Release`), the queries, and the hit path every hit takes. |
| `Drives.cs` | The drives' rules: `Graze`, `Flee`, `Retaliate`, `Escape`. |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `Live.cs` | The system: the killed swept out; each creature kept or gone, then its tick; the brain's shared steps. |
| `Lunge.cs` | The lunge's rule: a ram along the ground in phases (0103). |
| `Rules.cs` | The rules' contracts (drive, body, move) and `RuleBook`, which finds them by reflection. |
| `Spawn.cs` | Spawning (0102): round each hero, on the beat, out of every hero's view, where a habitat of the surface's type is. |
| `State.cs` | The saved slice and its records: `Creature`, `Act`, `CreaturesState`; the derived `Posture`. |
| `Walker.cs` | The walker's rule: steering, walking, stepping, falling and leaping (0102, 0104, 0106); `Leaps`. |

Every creature, composed ([2026-10-26-creatures-are-composed](../../docs/decisions/2026-10-26-creatures-are-composed.md)):
a species (`Content.Creatures`) names a body, a life, a faction, a lifetime, a brain of drives ranked from the top, the
moves it may make and its yields, and each of those records is served by one rule here. Living on a planet's surface
([0102](../../docs/decisions/0102-creatures-live-in-the-sim.md)), spawned round heroes, grazing, struck by any weapon, fleeing,
ramming a hero ([0103](../../docs/decisions/0103-a-ram-costs-the-suit-air.md)), dying into their yields. The ship has none.

- **Depends on:** `Grid` (size, solid, back walls); `Crew` (heroes' reported cells; `Shock`, a ram's cost to the suit);
  `Items` (`Damage` (0098), `WeaponProblem`; `DropAt` for yields).
- **State** (`CreaturesState`, saved and hashed): `Creatures` in id order, each `{ Id, Species, X, Y, VX, VY, Damage, Drive,
  DriveLeft, Step, Act, SeenAt }`, position the bottom middle of its body in thousandths of a cell (`CreaturesModule.Unit`);
  `Drive` the drive in charge by its place in the brain, `Step` where that drive is within itself, `Act` the move under way
  (`{ Move, Phase, Left }`) or none; `NextId`, never reused; `Clock`, ticks run on a surface with life or creatures.
  **Never saved:** `Dead`, a creature killed this tick. **Derived:** `Posture`, from the drive or act in charge; facing (sign
  of `VX`); the beat (`Clock % Creatures.Beat`).
- **Runs** last in `Registry.All`, after `Crew` in the simulation phase, so heroes' cells are this tick's; saved last.

## Adding a species or a rule

- **A species from rules there are:** a record in `Content.Creatures`, its art, its name. Its brain ends with a drive that
  always wants it (`Graze`); one with moves lists `Escape` (`CreatureContractTests`).
- **A rule:** a record (`Drive`, `Shape` or `Move`) in `Content.Creatures` and a class here deriving `DriveRule`, `BodyRule` or
  `MoveRule`, serving it. `RuleBook` finds it; no list is edited. Rules hold no state; a creature's own lives on it.

## Rules

- **Nothing aboard** (0102): a surface whose kind has no habitats and no creature let out runs nothing and draws nothing.
- **A tick,** in id order: kept or gone; its body freed of what was built into it since, which spends its tick; else an
  act's clock counts down (a lunge's wind-up only once grounded) and its phase moves on, or the drives above the one in
  charge are asked `Wants` and the drive's clock counts down, its `Next` run at none: yielding, the first drive from the top
  that wants it takes over. Then its body steers by the act or drive and moves. Then, on the beat, spawns.
- **The walker** (0102, 0104, 0106): integer, per axis, under the surface's gravity (`Kind.Fall`, capped `Creatures.MaxFall`).
  Solid is a wall, floor, closed door and past the grid's edge, not a machine. It steps up and down `Creatures.StepUp`,
  moving in parts under a cell. A drive's gait says how it goes: still, walking (`Walk`) or running (`Run`). Walking or running into a wall it cannot step up,
  or a drop of `Creatures.Drop` rows, it first tries a leap flown through the grid (`Lands`): running, the lowest of three
  that lands two cells on (`WanderLedge`, a step higher, its whole `Jump`); walking, only up a wall at its feet. No leap,
  its drive may act instead (`Blocked`); else it turns.
- **Drives:** `Graze` idles and wanders by turns, each length drawn, a wander's way first. `Flee` runs its ticks, then yields;
  cornered — the nearest hero within `CorneredReach` on the side it would turn to — it draws `CorneredChance` and lunges.
  `Retaliate`, struck by a hero within `Reach`, draws `Chance`, then flees a drawn `FleeLeast..FleeMost` and lunges (at once
  where it must turn). `Escape` runs its ticks after a lunge, never acting, then yields.
- **The lunge** (0103): wind-up, standing; then a charge at the hero nearest as it ends, along the ground at `Speed`, never
  leaping, till `Past` cells past it, a wall or drop, or `Ticks`; a hero farther than that run, or none, is let be. However it
  ends, the drive that began it hands the creature to `Escape`.
- **The hit path** (`Strike(Creature, Tool)`, 0102): takes `Items.Damage` off it; at its hit points it is dead, its `Yields`
  rolled as loot (`Loots.Roll`) and dropped by nobody on the cell under its middle. Else knocked by its `Life.Knock` away from
  the striker's reported cell (none: back against its way), never slower than it already goes, `Hop` up unless rising, a
  tool faster than `Creatures.KnockPace` its pace's share; its act cut short; its drives told in brain order till one claims
  it (`Retaliate`, else `Flee`, each bolting at its run or faster). Reach, pace and holding are the client's word.
- **The dead** are gone to every command, query and cap from the blow that killed them; swept out as the system starts.
- **`HitBy(Creature)`** (0103), from the hero a charge touched, as its client saw it (0034): the sender's suit pays the
  move's `Damage` (`Shock`: nothing in creative mode or for a hero out); the creature recoils into `Escape`, hopping. One hit
  per charge.
- **`Release(Species, Cell)`** (0102), a dev command (`--creatures`): one born with its body's bottom left in the cell.
- **Kept or gone** (`Lifetime.Wild`, 0102), by the cell under its middle: in some hero's keep box (`Creatures.Keep`) it is
  seen; out of every one it is gone `Creatures.Linger` ticks after last seen, and at once out of every hard box.
- **Spawning** (0102, `Spawn.cs`), on the beat: per hero under `Creatures.PerKeep` in its keep box, the surface under
  `Creatures.Max`: three draws, always taken (side, columns out, habitat by weight), then the first row down that column
  where the body stands in the habitat's place; none where any hero sees it. Born to its grazing, seen.

## Commands

`Strike(Creature, Tool)`: `creatures.gone`, `items.flat`, `items.notRunning` (0055). `HitBy(Creature)`: `creatures.gone`,
`creatures.notAttacking`. `Release(Species, Cell)`: `creatures.notHere` aboard, `creatures.full` at `Creatures.Max`,
`creatures.blocked` off the grid or in anything solid. A tool dealing no damage or an unknown species throws: our bug.

## Guarantees

- Every creature is on the grid and, but the ticks after a wall is built into it, out of everything solid; damage below
  hit points; ids only grow; never more than `Creatures.Max`; what is left of a drive or act never exceeds its length; a
  charge hurts at most once; every suit stays between empty and full (`CreaturesInvariantTests`).
- A surface with no habitats and no creatures leaves the slice and the Rng untouched.

## Deliberately not handled

- **Creatures do not collide** with each other or with heroes (0006). **Senses through rock.**
- **Walled in at the grid's top** it stays in the wall. **Despawning loses damage.**
- **A ram aims where the hero was reported** as the wind-up ended (0103). A second hero a charge touched pays nothing.
- **The drill held on a rammer** draws anew at every hit, so it turns back only once the drilling stops.
- **No hit-stun** (a `Flinch` drive) until a species needs one.

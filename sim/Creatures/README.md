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

Every creature, composed ([2026-10-26-creatures-are-composed](../../docs/decisions/2026-10-26-creatures-are-composed.md)): a
species (`Content.Creatures`) names a body, a life, a brain of drives ranked from the top, its moves and yields, each record
served by one rule here; its `Lifetime` is not read before the sentry. Living on planets
([0102](../../docs/decisions/0102-creatures-live-in-the-sim.md)), grazing, struck, fleeing, ramming
([0103](../../docs/decisions/0103-a-ram-costs-the-suit-air.md)), dying into yields. The ship has none.

- **Depends on:** `Grid` (size, solid, back walls); `Crew` (heroes' reported cells; `Shock`); `Items` (`Damage` (0098),
  `WeaponProblem`; `DropAt` for yields).
- **State** (`CreaturesState`, saved and hashed): `Creatures` in id order, each `{ Id, Species, X, Y, VX, VY, Damage, Drive,
  DriveLeft, Step, Act, SeenAt }`, its bottom middle in thousandths of a cell (`Unit`); `Drive` the drive in charge by its
  place in the brain, `Step` where that drive is within itself, `Act` the move under way (`{ Move, Phase, Left }`) or none;
  `NextId`, never reused; `Clock`. **Never saved:** `Dead`, killed this tick. **Derived:** `Posture`, `Fleeing`, from the
  drive or act in charge.
- **Runs** after `Crew` in the simulation phase, so heroes' cells are this tick's.

## Adding a species or a rule

- **A species from rules there are:** a record in `Content.Creatures`, its art, its name. Its brain ends with `Graze`,
  holds a `Flee` to claim hits, and `Escape` if it has moves; its order is pinned (`CreatureContractTests`).
- **A rule:** a record (`Drive`, `Shape` or `Move`) and a class here deriving `DriveRule`, `BodyRule` or `MoveRule`. `RuleBook`
  finds it; no list is edited. Rules hold no state. Their context is the module's internal members; a narrower one comes with
  the sentry.
- **A brain's order is saved** (a creature's `Drive` and `Act.Move` are places in it): change it only with a format bump.

## Rules

- **Nothing aboard** (0102): a surface with no habitats and no creature let out runs nothing and draws nothing.
- **A tick,** in id order: kept or gone; its body freed of what was built into it since, spending the tick; else an act's
  clock counts down (a lunge's wind-up once grounded) and its phase moves on, or a drive above the one in charge that
  `Wants` it takes over, or the drive's clock counts down and at none its `Next` runs or it yields to the first that wants
  it. Then its body steers by the act or drive and moves. Then, on the beat, spawns.
- **The walker** (0102, 0104, 0106): integer, under the surface's gravity (capped `MaxFall`). Solid is a wall, floor, closed
  door and past the grid's edge, not a machine. It steps up and down `StepUp`, in parts under a cell. Its drive's gait: still,
  walking (`Walk`) or running (`Run`). Into a wall it cannot step up, or a drop of `Drop` rows, it first tries a leap flown
  through the grid (`Lands`): running, the lowest of three that lands; walking, only up a wall at its feet. No leap, its
  drive may act instead (`Blocked`); else it turns.
- **Drives:** `Graze` idles and wanders by turns, lengths drawn, a wander's way first. `Flee` runs its ticks, then yields;
  cornered — the nearest hero within `CorneredReach` on the side it would turn to — it draws `CorneredChance` and lunges.
  `Retaliate`, struck by a hero within `Reach`, draws `Chance`, flees a drawn `FleeLeast..FleeMost`, then lunges (at once
  where it must turn). `Escape` runs its ticks after a lunge, never acting, then yields.
- **The lunge** (0103): wind-up, standing; then a charge at the hero nearest as it ends, along the ground at `Speed`, never
  leaping, till `Past` cells past it, a wall or drop, or `Ticks`; a hero farther than that run, or none, is let be, and it
  escapes away from the nearest. However it ends, the drive that began it hands the creature to `Escape`.
- **The hit path** (`Strike`, 0102): takes `Items.Damage` off it; at its hit points it is dead, its `Yields` rolled and
  dropped by nobody under its middle. Else knocked its `Life.Knock` away from the striker (a quick tool its pace's share of
  `KnockPace`), never slowed, hopped unless rising; its act cut short; its drives told in brain order, the first claim winning
  (`Retaliate`, else `Flee`, each bolting at its run or faster). Reach, pace and holding are the client's word.
- **The dead** are gone to every command and cap from the blow that killed them; the `Creatures` list holds them till the
  system sweeps them, first thing.
- **`HitBy(Creature)`** (0103), from the hero a charge touched, as its client saw it (0034): its suit pays the move's
  `Damage` (`Shock`); the creature recoils into `Escape`, hopping. One hit per charge.
- **`Threatens(peer)`**: a creature within `ThreatReach` of its hero acts, retaliates or hunts the crew; its client then
  reports every tick it moves (Terraria's way).
- **`Release(Species, Cell)`** (0102), a dev command (`--creatures`): one born with its body's bottom left in the cell.
- **Kept or gone** (wild, 0102), by the cell under its middle: in some hero's keep box it is seen; out of every one it is
  gone `Linger` ticks after, and at once out of every hard box.
- **Spawning** (0102, `Spawn.cs`), on the beat: per hero under `PerKeep` in its keep box, the surface under `Max`: three
  draws, always taken (side, columns out, habitat by weight), then the first row down where the body stands in the
  habitat's place; none where any hero sees it. Born to its grazing.

## Commands

`Strike(Creature, Tool)`: `creatures.gone`, `items.flat`, `items.notRunning` (0055). `HitBy(Creature)`: `creatures.gone`,
`creatures.notAttacking`. `Release(Species, Cell)`: `creatures.notHere` aboard, `creatures.full` at `Max`, `creatures.blocked`
off the grid or in anything solid. A tool dealing no damage or an unknown species throws: our bug.

## Guarantees

- Every creature is on the grid and, but the ticks after a wall is built into it, out of everything solid; damage below
  hit points; ids only grow; never more than `Max`; what is left of a drive or act never exceeds its length; a charge
  hurts at most once; every suit stays between empty and full (`CreaturesInvariantTests`). A world saved and loaded every
  tick plays on as one never saved.
- A surface with no habitats and no creatures leaves the slice and the Rng untouched.

## Deliberately not handled

- **Creatures do not collide** with each other or with heroes (0006). **Senses through rock.** **Despawning loses damage.**
- **Walled in at the grid's top** it stays in the wall.
- **A ram aims where the hero was reported** as the wind-up ended (0103). A second hero a charge touched pays nothing.
- **The drill held on a rammer** draws anew at every hit, so it turns back only once the drilling stops.
- **No hit-stun** (a `Flinch` drive) until a species needs one.
- **Walkers only:** drives and the lunge read the walker's speeds; a second body moves them to its rule.
- **Taking over:** only grazing wants a creature, so asking the drives above it is untested; one taking over counts from the
  next tick.

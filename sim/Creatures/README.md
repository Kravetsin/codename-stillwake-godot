# Creatures

## Files

| File | Purpose |
| --- | --- |
| `Clinger.cs` | The clinger's rule (the sentry): clamped to a wreck's top, riding it, flying straight off it. |
| `Creatures.cs` | The commands (`Strike`, `HitBy`, `Release`), the queries, and the hit path every hit takes. |
| `Drives.cs` | The grazers' drives' rules: `Graze`, `Flee`, `Retaliate`, `Escape`. |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `Guard.cs` | The sentry's drives' rules: `Guard`, `Return`, and the `CutTether` reflex. |
| `Live.cs` | The system: the killed swept out; each creature kept or gone, then its tick; the brain's shared steps. |
| `Lunge.cs` | The lunge's rule: a ram along the ground in phases (0103). |
| `Pounce.cs` | The pounce's rule: a straight flight at a hero, never cut short. |
| `Rules.cs` | The rules' contracts (drive, body, move) and `RuleBook`, which finds them by reflection. |
| `Spawn.cs` | Spawning (0102): round each hero, on the beat, out of every view, where a habitat is; guards on new derelicts. |
| `State.cs` | The saved slice and its records: `Creature`, `Act`, `Ref`, `Note`, `CreaturesState`; the derived `Posture`. |
| `Walker.cs` | The walker's rule: steering, walking, stepping, falling and leaping (0102, 0104, 0106); `Leaps`. |

Every creature, composed ([2026-10-26-creatures-are-composed](../../docs/decisions/2026-10-26-creatures-are-composed.md)): a
species (`Content.Creatures`) names a body, a life, a faction, a brain of drives ranked from the top, its moves and yields,
each record served by one rule here; its `Lifetime` is not read yet. Grazers live on planets
([0102](../../docs/decisions/0102-creatures-live-in-the-sim.md), [0103](../../docs/decisions/0103-a-ram-costs-the-suit-air.md));
the sentry clings to wrecks in space, aboard's surface: creatures run wherever one is let out or a habitat spawns.

- **Depends on:** `Grid`; `Crew` (heroes' reported cells; `Shock`); `Items` (`Damage` (0098), `WeaponProblem`, `DropAt`);
  `Debris` (`IDebrisQueries`, `IDebrisOps.Unreel`: the sentry's wreck).
- **State** (`CreaturesState`, saved): `Creatures` in id order, each `{ Id, Species, X, Y, VX, VY, Damage, Drive, DriveLeft,
  Step, Act, Target, Mem, SeenAt }`, its bottom middle in thousandths of a cell; `Drive` the drive in charge by its place in
  the brain, `Step` where that drive is within itself, `Act` the move under way (`{ Move, Phase, Left }`), `Target` its
  grudge (a `Ref`), `Mem` its rules' numbers by key in ordinal order (never a dictionary: a load keeps the order); `NextId`;
  `Clock`. Empty `Target` and `Mem` are not written. **Never saved:** `Dead`. **Derived:** `Posture`, `Fleeing`.
- **Runs** after `Crew` and `Debris` in the simulation phase: heroes' cells and wrecks are this tick's.

## Adding a species or a rule

- **A species:** a record in `Content.Creatures`, its art, its name. Some drive always wants it, one claims hits, and a drive
  that acts has its follow-up (`Escape`, `Return`); its order is pinned (`CreatureContractTests`): a brain's order is saved,
  change it only with a format bump.
- **A rule:** a record (`Drive`, `Shape` or `Move`) and a class deriving `DriveRule`, `BodyRule` or `MoveRule`; `RuleBook`
  finds it. Rules hold no state; their context is the module's internal members.

## Rules

- **A tick,** in id order: kept or gone; its body freed of what was built into it since; every drive's `Reflex`; then an
  act's clock (a lunge's wind-up once grounded), or a drive above the one in charge that `Wants` it takes over, or the drive
  in charge `Run`s (false yields; an act it begins counts from the next tick) and its clock counts down, at none its `Next`
  running or yielding to the first that wants it. Then its body steers and moves. Then, on the beat, spawns.
- **The walker** (0102, 0104, 0106): integer, under the surface's gravity; solid is a wall, floor, closed door and the grid's
  edge. It steps `StepUp` up and down, in parts under a cell. Its gait still, walking or running; at a wall it cannot step or a
  drop of `Drop` rows it tries a leap flown through the grid (`Lands`), else its drive may act (`Blocked`), else it turns.
- **The clinger:** clamped to the top of the wreck it was let out on (`Wreckage.SizeOf`), `along` from its middle, riding
  its drift; off it, it flies its speed straight, through walls, kept on the grid. Its wreck gone, it holds the last point.
- **Grazers:** `Graze` idles and wanders by turns, lengths drawn. `Flee` runs its ticks; cornered with a hero near it may
  lunge. `Retaliate`, struck by a hero within `Reach`, draws `Chance`, flees a drawn while, then lunges. `Escape` runs after.
- **The lunge** (0103): wind-up, then a charge along the ground at the hero nearest, never leaping, till past it, a wall or
  drop, or `Ticks`; a hero beyond its run is let be. It ends into `Escape`.
- **The sentry:** `Guard` watches; an awake hero within `Notice` alerts it, `AlertTicks` (from the tick it noticed) of it
  pounce. A blow on it, or on its wreck since it last answered (`Debris.Piece.Struck`, by the debris clock), and it pounces
  at once at the striker (`Target`); a pounce answers every blow up to it, a blow while away is answered home, blows before
  it was let out never. The pounce crouches `WindUp`, then flies straight at its grudge, else the nearest awake hero within
  its run, at `Speed` on the longer axis: `Ticks` moves, never cut short, then it hangs. Unclamped, `Return` (above `Guard`)
  flies it home and clamps it on, its grudge spent. `CutTether` lets go of any reel on its wreck the tick it is seen (the
  reel moved it a step) and stamps the cut (`LastCut`). Body and home stay wholly on the grid. Queries: `Alerted`, `Clamped`.
- **The hit path** (`Strike`): damage; dead at its hit points, its `Yields` dropped by nobody; else knocked by a `Life.Knock`
  and `Hop` above none, its act cut if interruptible, its drives told in brain order, the first claim winning.
- **The dead** are gone to every command and cap from the blow; the list holds them till the sweep, first thing.
- **`HitBy(Creature)`** (0103), as the hit hero's client saw it: its suit pays the move's `Damage`; the act ends.
- **`Release(Species, Cell)`**, dev (`--creatures`, `--sentry`): its body's `ReleaseProblem` and `Place`.
- **`Threatens(peer)`**: a creature within `ThreatReach` acts, retaliates or hunts the crew; its client reports each tick.
- **Kept or gone** (0102): in some hero's keep box it is seen; out of every one gone `Linger` after; out of every hard box at
  once. **Spawning** (`Spawn.cs`): on the beat round heroes under `PerKeep`, the surface under `Max`. **Guards**
  (`Kind.Guards`): each piece past `Watermark` draws once, in id order: sentries on half the derelicts.

## Commands

`Strike`: `creatures.gone`, `items.flat`, `items.notRunning`. `HitBy`: `creatures.gone`, `creatures.notAttacking`. `Release`:
`creatures.blocked` off the grid, `creatures.full`, then the body's: `creatures.notHere` (a walker with no gravity),
`creatures.blocked` (in anything solid), `creatures.noPiece` (a clinger with no wreck).

## Guarantees

- Every creature is on the grid, a walker out of everything solid; damage below hit points; ids only grow; never more than
  `Max`; a drive's or act's ticks never past its length; a charge hurts at most once; suits within range
  (`CreaturesInvariantTests`). A world saved and loaded every tick plays on as one never saved.
- No habitats, creatures or new guarded piece: nothing drawn; only `Watermark` moves.

## Deliberately not handled

- **No collisions** (0006); senses through rock; despawning loses damage; walled in at the grid's top it stays.
- **A ram aims where the hero was reported** (0103); a second hero touched pays nothing.
- **No hit-stun** until a species needs one. **Lifetime** is the keep box's for all: a wreck goes before its sentry.
- **A derelict escorted onto the hull** brings its sentry over the ship; its pounce passes walls.
- **The sentry sits on its wreck's box**, not its picture's outline, and does not crawl yet. A first blow that breaks its
  wreck goes unanswered: the wreck is gone before it looks.

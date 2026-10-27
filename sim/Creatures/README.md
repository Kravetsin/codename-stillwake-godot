# Creatures

## Files

| File | Purpose |
| --- | --- |
| `Clinger.cs` | The clinger's rule (the sentry): on a perch of a wreck's top, crawling, riding it, flying off it. |
| `Creatures.cs` | The commands (`Strike`, `HitBy`, `Release`), the queries, and the hit path every hit takes. |
| `Drives.cs` | The grazers' drives' rules: `Graze`, `Flee`, `Retaliate`, `Escape`. |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `Guard.cs` | The sentry's drives' rules: `Guard`, `Return`, and the `CutTether` reflex. |
| `Live.cs` | The system: the killed swept out; each creature kept or gone, then its tick; the brain's shared steps. |
| `Lunge.cs` | The lunge's rule: a ram along the ground in phases (0103). |
| `Pounce.cs` | The pounce's rule: a straight flight at a hero, never cut short. |
| `Rules.cs` | The rules' contracts (drive, body, move) and `RuleBook`, which finds them by reflection. |
| `Shots.cs` | Shots: `Fire`, the flight system, what a shot hits; `Shots`, `Ended`. |
| `Spawn.cs` | Spawning (0102): round each hero, on the beat, out of every view, where a habitat is; guards on new derelicts. |
| `State.cs` | The saved slice and its records: `Creature`, `Act`, `Ref`, `Note`, `Shot`, `CreaturesState`; the derived `Posture`. |
| `Walker.cs` | The walker's rule: steering, walking, stepping, falling and leaping (0102, 0104, 0106); `Leaps`. |

Every creature, composed ([2026-10-26-creatures-are-composed](../../docs/decisions/2026-10-26-creatures-are-composed.md)): a
species (`Content.Creatures`) names a body, a life, a faction, a brain of drives ranked from the top, its moves and yields,
each record served by one rule here; its `Lifetime` is not read yet. Grazers live on planets (0102, 0103); the sentry clings
to wrecks in space: creatures run wherever one is let out or a habitat spawns. Shots fly here too
([shots-are-the-sims](../../docs/decisions/2026-10-26-shots-are-the-sims.md)).

- **Depends on:** `Grid`; `Crew` (heroes' cells; `Shock`); `Items` (`Damage`, `WeaponProblem`, `ToolStack`, `ShortOf`,
  `Take`, `DropAt`); `Debris` (queries, `Wrecks`, `Unreel`, `Hit`); `Building` (`IBuildingOps.Hit`).
- **State** (`CreaturesState`, saved): `Creatures` in id order, each `{ Id, Species, X, Y, VX, VY, Damage, Drive, DriveLeft,
  Step, Act, Target, Mem, SeenAt }`, its bottom middle in thousandths of a cell; `Drive` the drive in charge by its place in
  the brain, `Step` where that drive is within itself, `Act` the move under way (`{ Move, Phase, Left }`), `Target` its
  grudge (a `Ref`), `Mem` its rules' numbers by key in ordinal order; `NextId`; `Clock`; `Shots`, `Fired`. Empty `Target`,
  `Mem` and `Shots` are not written. **Never saved:** `Dead`. **Derived:** `Posture`, `Fleeing`, `Ended`.
- **Runs** after `Crew` and `Debris` in the simulation phase: heroes' cells and wrecks are this tick's; shots fly after.

## Adding a species or a rule

- **A species:** a record in `Content.Creatures`, its art, its name. Some drive always wants it, one claims hits, a drive that
  acts has its follow-up; a brain's order is saved and pinned (`CreatureContractTests`): change it with a format bump.
- **A rule:** a record and a class deriving `DriveRule`, `BodyRule` or `MoveRule`, found by `RuleBook`; rules hold no state.

## Rules

- **A tick,** in id order: kept or gone; its body freed of what was built into it since; every drive's `Reflex`; then an
  act's clock (a lunge's wind-up once grounded), or a drive above the one in charge that `Wants` it takes over, or the drive
  in charge `Run`s (false yields; an act it begins counts from the next tick) and its clock counts down, at none its `Next`
  running or yielding to the first that wants it. Then its body steers and moves. Then, on the beat, spawns.
- **The walker** (0102, 0104, 0106): integer, under the surface's gravity; solid is a wall, floor, closed door, the grid's
  edge and on a planet the fog past its zone. It steps `StepUp` up and down, in parts under a cell. Its gait still, walking
  or running; at a wall it cannot step or a drop of `Drop` rows it tries a leap (`Lands`), else its drive may act
  (`Blocked`), else it turns.
- **The clinger:** clamped to the top of the wreck it was let out on, `along` from its middle, riding its drift; off it, it
  flies its speed straight, through walls, kept on the grid. Its wreck gone, it holds the last point.
- **Grazers:** `Graze` idles and wanders by turns, lengths drawn. `Flee` runs its ticks; cornered with a hero near it may
  lunge. `Retaliate`, struck by a hero within `Reach`, draws `Chance`, flees a drawn while, then lunges. `Escape` runs after.
- **The lunge** (0103): wind-up, then a charge along the ground at the hero nearest, never leaping, till past it, a wall or
  drop, or `Ticks`; a hero beyond its run is let be. It ends into `Escape`.
- **The sentry:** `Guard` watches; a hero within `Notice` alerts it; `AlertTicks` on, one within `Strike` and it pounces.
  A blow on it, or on its wreck since it last answered (`Piece.Struck` against a hit's `Answered`), and it pounces at the
  striker (`Target`); a blow while away is answered home. The pounce crouches `WindUp`, then flies straight at its grudge,
  else the nearest awake hero within its run, `Ticks` moves at `Speed`, then hangs. Unclamped, `Return` flies it home.
  `CutTether` lets go of a reel on its wreck `Ticks` after it bites. Its wreck gone, it dies. Queries: `Alerted`, `Clamped`.
- **The hit path** (`Strike`, a shot): damage; dead at its hit points, its `Yields` dropped by nobody; else knocked by a
  `Life.Knock` and `Hop` above none, its act cut if interruptible, its drives told in brain order, the first claim winning.
- **Shots** (the ADR): `Fire` takes one ammo and adds a shot; each tick, in id order, each `ShotFlight.Step`s, and the
  first cell met takes its hit: solid (`IBuildingOps.Hit`), else the lowest-id creature, else a wreck (`IDebrisOps.Hit`).
- **The dead** are gone to every command and cap from the blow; the list holds them till the sweep.
- **`HitBy(Creature)`** (0103), as the hit hero's client saw it: its suit pays the move's `Damage`; the act ends.
- **`Release(Species, Cell)`**, dev: its body's `ReleaseProblem` and `Place`.
- **`Threatens(peer)`**: one within `ThreatReach` acts, retaliates or hunts the crew; its client reports each tick.
- **Kept or gone** (0102): in some hero's keep box it is seen; out of every one gone `Linger` after; out of every hard box at
  once. **Spawning:** on the beat round heroes under `PerKeep`, the surface under `Max`. **Guards** (`Kind.Guards`): each
  piece past `Watermark` with a perch draws once: sentries on half.

## Commands

`Strike`: `creatures.gone`, `items.flat`, `items.notRunning`. `HitBy`: `creatures.gone`, `creatures.notAttacking`. `Release`:
`creatures.blocked` off the grid, `creatures.full`, then the body's: `creatures.notHere` (a walker with no gravity),
`creatures.blocked` (in anything solid), `creatures.noPiece` (a clinger with no wreck). `Fire`: `creatures.noGun` (none
carried), `creatures.blocked` (the muzzle off the grid), `items.notEnough`.

## Guarantees

- Every creature is on the grid, a walker out of everything solid; damage below hit points; ids only grow; never more than
  `Max`; a drive's or act's ticks never past its length; a charge hurts at most once; suits within range
  (`CreaturesInvariantTests`). Every shot on the grid, within its reach, never through a solid cell (`ShotsInvariantTests`).
  A world saved and loaded every tick plays on as one never saved.
- No habitats, creatures or new guarded piece: nothing drawn; only `Watermark` moves.

## Deliberately not handled

- **No collisions** (0006); senses through rock; despawning loses damage; walled in at the top it stays.
- **A ram aims where the hero was reported** (0103); a second hero pays nothing.
- **No hit-stun** until a species needs one. **Lifetime** is the keep box's for all: a wreck goes before its sentry.
- **A derelict escorted aboard** brings its sentry; its pounce passes walls. A blow that breaks its wreck goes unanswered.
- **A shot's muzzle and pace are the client's** (0002, 0034); aboard, shots fly straight.

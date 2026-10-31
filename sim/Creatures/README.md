# Creatures

## Files

| File | Purpose |
| --- | --- |
| `Arcs.cs` | Arcs: `Bolted`, `StrikeArc`, `Linger`; `Arcs`. |
| `Clinger.cs` | The clinger's rule (the sentry): perched on a wreck's top, crawling, riding, flying off. |
| `Creatures.cs` | The commands (`Strike`, `HitBy`, `Release`), the queries, and the hit path every hit takes. |
| `Drives.cs` | The grazers' drives' rules: `Graze`, `Flee`, `Retaliate`, `Escape`. |
| `Fuzz.cs` | The `Fuzz` override: commands the desync suite draws. |
| `Guard.cs` | The sentry's drives' rules: `Guard`, `Return`, and the `CutTether` reflex. |
| `Live.cs` | The system: the killed swept out; each creature kept or gone, then its tick; the brain's shared steps. |
| `Lunge.cs` | The lunge's rule: a ram along the ground in phases (0103). |
| `Pounce.cs` | The pounce's rule: a straight flight at a hero, never cut short; a walker's stops at a wall. |
| `Rules.cs` | The rules' contracts (drive, body, move) and `RuleBook`, which finds them by reflection. |
| `Shoot.cs` | The arc's rule (the sentry's): crouched, its aim held, an arc struck at once, a recovery. |
| `Shots.cs` | Shots: `Fire`, the flight, hits, pierces; `Shots`, `Ended`, `Hits`, `StepOf`. |
| `Spawn.cs` | Spawning (0102): posts; round each hero, on the beat, out of view, by habitat; guards on derelicts. |
| `State.cs` | The saved slice and its records: `Creature`, `Act`, `Ref`, `Note`, `Shot`, `CreaturesState`; the derived `Posture`. |
| `Ward.cs` | The spire guards' drive: asleep, waking, waiting, its pounce, stuck; turns; calm. |
| `Walker.cs` | The walker's rule: steering, walking, stepping, falling, leaping (0102, 0104, 0106). |

Every creature, composed ([creatures-are-composed](../../docs/decisions/2026-10-26-creatures-are-composed.md)): a
species (`Content.Creatures`) names a body, life, faction, a brain of ranked drives, moves and yields, each record served
by one rule here. Grazers live on planets (0102, 0103), the sentry on wrecks, the warden in spires. Shots fly here (shots-are-the-sims,
shots-pierce, shots-fall-aboard).

- **Depends on:** `Grid`; `Crew` (heroes' cells; `Blow`); `Items` (queries, ops: weapons, ammo, drops, charge); `Debris`
  (queries, `Unreel`, `Hit`); `Building` (`ShotTakes`, `Hit`, `Relics`, `Posts`, `PostAt`, `FellPost`); `Rooms` (`Afloat`).
- **State** (`CreaturesState`, saved): `Creatures` in id order, each `{ Id, Species, X, Y, VX, VY, Damage, Drive, DriveLeft,
  Step, Act, Target, Mem, SeenAt }`, its bottom middle in thousandths of a cell; `Drive` its brain place, `Act` `{ Move, Phase, Left }`,
  `Mem` its rules' numbers by key, ordinal; `NextId`; `Clock`; `Shots`, `Arcs`, `Fired`. **Never saved:** `Dead`. **Derived:** `Posture`, `Fleeing`, `Ended`, `Hits`.
- **Runs** after `Crew` and `Debris` (simulation); shots fly after.

## Adding a species or a rule

- **A species:** a record in `Content.Creatures`, its art and name. A drive always wants it, one claims hits, an acting drive
  has a follow-up; a brain's order is saved and pinned (`CreatureContractTests`): change it with a format bump.
- **A rule:** a record and a class deriving `DriveRule`, `BodyRule` or `MoveRule`, found by `RuleBook`; rules hold no state.

## Rules

- **A tick,** in id order: kept or gone; its body freed; every drive's `Reflex`; then an act's clock, or a drive above the
  one in charge that `Wants` it takes over, or the drive in charge `Run`s (false yields; an act it begins counts from the
  next tick), at none left its `Next`. Then its body steers and moves. Then, on the beat, spawns.
- **The walker** (0102, 0104, 0106): under the surface's gravity; solid is a wall, floor, closed door, the grid's
  edge and a planet's fog. It steps `StepUp`; at a wall it cannot step or a drop of `Drop` it tries a leap (`Lands`),
  else its drive may act (`Blocked`), else it turns.
- **The clinger:** clamped to its wreck's top, riding its drift; off it, it flies straight through walls.
- **Grazers:** `Graze` idles and wanders by turns, lengths drawn. `Flee` runs its ticks; cornered with a hero near it may
  lunge. `Retaliate`, struck by a hero within `Reach`, draws `Chance`, flees a drawn while, then lunges. `Escape` runs after.
- **The lunge** (0103): wind-up, then a charge along the ground at the hero nearest, never leaping, till past it, a wall or
  drop, or `Ticks`; a hero beyond its run is let be. It ends into `Escape`.
- **The sentry:** `Guard` watches; one within `Notice` alerts it; `AlertTicks` on, one within `Strike` and it pounces,
  one farther it strikes an arc. A blow on it or its wreck: it answers so. The pounce crouches `WindUp`, then flies at
  its grudge, else the nearest awake hero within its run, `Ticks` at `Speed`; `Return` flies it home. The arc, from its perch: aimed
  `Lock` before the wind-up ends, struck to the first wall, lingering `Lingers`. `CutTether` lets go of a reel. Its wreck gone, it dies.
- **The warden** (`Lifetime.Posted`, `Ward`): on the beat one appears at a standing post (`Flora.Post`) with a standing relic in its
  arena, a hero in its keep box, none tied to it; no draw; a full cap or blocked niche waits. Woken by an awake hero in the arena, a
  blow or a sibling. It pounces if no sibling acts; a walker's (`Flies`) has no gravity and ends at the first solid cell; then `Stuck`
  from its landing. **Goes in the kept loop, no drop, never by `Dead`:** post gone, relic taken, or `Calm` (back whole). Killed, it fells its post.
- **The hit path** (`Strike`, a shot): damage; dead at its hit points, its `Yields` sprayed; else knocked
  (`Life.Knock`, `Hop`), its act cut if interruptible, its drives told in brain order till one claims it.
- **Shots:** `Fire` takes an ammo and its gun's `Energy`; each tick, in id order, it steps, hitting a cell's targets in turn (solid,
  the lowest-id creature, a wreck), skipping `Passed`: through what its gun `Pierces`, else ending. It falls by `ShotFlight.Gravity`.
- **The dead** are gone to every command and cap from the blow, swept within the tick: the mark is not saved.
- **`HitBy(Creature)`** (0103), **`Bolted(Arc)`**, as the hit hero's client saw it: a `Blow` of the move's `Damage`, the act ends; or
  the arc's projectile's, once a hero.
- **`Release`** (dev): `Place`.
- **`Threatens(peer)`**: one within `ThreatReach` acts, retaliates or hunts the crew.
- **Kept or gone** (0102): in a hero's keep box; out of all, gone `Linger` after; out of every hard box, at once. **Spawning:** on the beat, under `PerKeep` and `Max`. **Guards** (`Kind.Guards`): each
  piece past `Watermark` with a perch draws once: sentries on half.

## Commands

`Strike`: `creatures.gone`, `items.flat`, `items.notRunning`. `HitBy`: `creatures.gone`, `creatures.notAttacking`. `Release`:
`creatures.notHere` (a posted one), `creatures.blocked` off the grid, `creatures.full`, then the body's: `creatures.notHere` (a walker with no gravity),
`creatures.blocked` (in anything solid), `creatures.noPiece` (a clinger with no wreck). `Fire`: `creatures.noGun` (none
carried), `creatures.blocked` (the muzzle off the grid), `items.notEnough`, `items.flat`. `Bolted`: `creatures.gone`.

## Guarantees

- Every creature is on the grid, a walker out of everything solid; damage below hit points; ids only grow; never more than
  `Max`; a drive's or act's ticks never past its length; a charge, or an arc a hero, hurts at most once; suits in range
  (`CreaturesInvariantTests`). Every shot on the grid, within reach, never through a cell left solid (`ShotsInvariantTests`).
  A world saved and loaded every tick plays on as one never saved.
- No habitats, creatures or new guarded piece: nothing drawn; only `Watermark` moves.

## Deliberately not handled

- **No collisions** (0006); senses through rock; despawning loses damage.
- **A ram aims where the hero was reported** (0103); a second hero pays nothing.
- **No hit-stun** yet. **Lifetime** is the keep box's for all: a wreck goes before its sentry.
- **A derelict escorted aboard** brings its sentry; its pounce passes walls. A blow that breaks its wreck goes unanswered.
- **Shots** (0034, the ADRs): muzzle, pace, charge the client's; a guest's copy stops at blocks the nailer breaks.

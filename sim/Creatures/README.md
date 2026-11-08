# Creatures

## Files

| File | Purpose |
| --- | --- |
| `Arcs.cs` | Arcs: `Bolted`, `StrikeArc`, `Linger`; `Arcs`. |
| `Clinger.cs` | The clinger (the sentry): perched on its wreck, crawling, riding, flying off. |
| `Creatures.cs` | The commands, the queries, the hit path. |
| `Drives.cs` | The grazers' drives: `Graze`, `Flee`, `Retaliate`, `Escape`. |
| `Fuzz.cs` | The `Fuzz` override: commands the desync suite draws. |
| `Guard.cs` | `Guard` (sentry, lurkers), `Return`, the `CutTether` reflex. |
| `Live.cs` | The system: the dead swept, patches aged, each creature kept or ticked. |
| `Lunge.cs` | The lunge: a ram along the ground in phases (0103). |
| `Patches.cs` | Patches (gas, slime): laid, aged, paid by the heroes in them. |
| `Pounce.cs` | The pounce: a straight flight at a hero, never cut; a walker's stops at a wall. |
| `Rules.cs` | The rules' contracts (drive, body, move); `RuleBook` finds them. |
| `Shell.cs` | The shell (the snail): open near a hero; shut it shields, open from shots. |
| `Shoot.cs` | The arc: crouched, its aim held, struck at once, a puddle left, a recovery. |
| `Shots.cs` | Shots: `Fire`, the flight, hits, pierces. |
| `Spawn.cs` | Spawning: posts; herds round each hero by habitat; guards on derelicts. |
| `State.cs` | The saved slice: `Creature`, `Act`, `Ref`, `Note`, `Shot`, `CreaturesState`; `Posture`. |
| `Ward.cs` | The spire guards' drive: asleep, waking, its pounce, stuck, calm. |
| `Vent.cs` | The vent (the capback): its gas laid at once, then still; a cooldown. |
| `Walker.cs` | The walker: steering, walking, stepping, falling, leaping (0102, 0104, 0106). |

Every creature, composed (creatures-are-composed): a
species (`Content.Creatures`) names a body, life, faction, a brain of ranked drives, moves and yields, each served
by one rule here. Grazers on planets, lurkers in caves, the sentry on wrecks, the warden in spires. Shots fly here.

- **Depends on:** `Grid`; `Crew` (heroes' cells; `Blow`, `Shock`); `Items` (weapons, ammo, drops, charge); `Debris`
  (queries, `Unreel`, `Hit`); `Building` (`ShotTakes`, `Hit`, `Relics`, `Posts`, `PostAt`, `FellPost`); `Rooms` (`Afloat`).
- **State** (`CreaturesState`, saved): `Creatures` in id order, each `{ Id, Species, X, Y, VX, VY, Damage, Drive, DriveLeft,
  Step, Act, Target, Mem, SeenAt }`, its bottom middle in thousandths of a cell; `Drive` its brain place, `Act` `{ Move, Phase, Left }`,
  `Mem` its rules' numbers by key, ordinal; `NextId`; `Clock`; `Shots`, `Arcs`, `Patches`, `Fired`. **Never saved:** `Dead`. **Derived:** `Posture`, `Fleeing`, `Ended`, `Hits`.
- **Runs** after `Crew` and `Debris` (simulation); shots fly after.

## Adding a species or a rule

- **A species:** a record in `Content.Creatures`, its art and name. A drive always wants it, one claims hits (or is answered
  in its `Mem`); a brain's order is saved and pinned (`CreatureContractTests`): a change bumps the format.
- **A rule:** a record and a `DriveRule`, `BodyRule` or `MoveRule`, found by `RuleBook`; rules hold no state.

## Rules

- **A tick,** in id order: kept or gone; its body freed; every drive's `Reflex`; then an act's clock, or a drive above the
  one in charge that `Wants` it takes over, or the drive in charge `Run`s (false yields; an act it begins counts from the
  next tick), at none left its `Next`. Then its body steers and moves. Then, on the beat, spawns.
- **The walker** (0102, 0104, 0106): under gravity; solid: walls, floors, shut doors, the grid's edge, a planet's fog.
  It steps `StepUp`; at a higher wall or a `Drop` it tries a leap (`Lands`), else its drive may act (`Blocked`), else turns.
- **The clinger:** clamped to its wreck's top, riding its drift; off it, it flies straight through walls.
- **Grazers:** `Graze` idles and wanders by turns, lengths drawn; past `Herd` from its kind, toward it. `Flee` runs its
  ticks; cornered with a hero near it may lunge. `Retaliate`, struck by a hero within `Reach`, draws `Chance`, flees a drawn
  while, then lunges. `Escape` runs after.
- **The lunge** (0103): wind-up, then a charge along the ground at the awake hero nearest, never leaping, till past it, a wall
  or drop, or `Ticks`; one beyond its run, or none, is let be. A `Steady` one no blow cuts.
- **The sentry:** `Guard`: one within `Notice` alerts it; `AlertTicks` on it pounces at one within `Strike`, strikes an arc at
  one farther; a blow on it or its wreck it answers so. The pounce crouches `WindUp`, flies `Ticks` at `Speed` at its grudge or the
  nearest awake hero in its run; `Return` flies it home. The arc: aimed `Lock` before its wind-up ends, to the first wall, `Lingers`.
  `CutTether` lets a reel go. Its wreck gone, it dies.
- **Lurkers** (lurker-attacks): still walkers on `Guard`, seeing only in line. The capback `Vent`s at one within `Strike`
  (`Cooldown`); provoked `Provokes` ticks by answering, it lobs at one `From` to `Reach`, a puddle (`Leaves`) laid as it lands
  `Lingers` on, harmless. The snail's `Shell` opens `Hold` past a hero within `Reach`; it bites, a short lunge.
- **Patches** (`PatchKind`) last `Lasts`; each breath an awake hero in one seen from its middle pays `Damage` (`Shock`, `Stings`).
- **The warden** (`Posted`, `Ward`): on the beat at a standing post with a standing relic in its arena, a hero in its keep box,
  none tied to it. Woken by an awake hero in the arena, a blow or a sibling, it pounces if no sibling acts (`Flies`: to the first
  solid cell), then `Stuck`. **Kept loop, no drop, never `Dead`:** post or relic gone, or `Calm`. Killed, it fells its post.
- **The hit path** (`Strike`, a shot): unless a drive `Shields` (a shut shell, a shot), damage; dead at hit points, `Yields` sprayed; else knocked
  (`Life.Knock`, `Hop`), its act cut if interruptible, its drives told in brain order till one claims it.
- **Shots:** `Fire` takes an ammo and its gun's `Energy`; each tick, in id order, one steps, hitting a cell's solid, lowest-id
  creature, wreck (not `Passed`), on through what its gun `Pierces`. It falls (`ShotFlight.Gravity`).
- **The dead** are gone to all from the blow, swept within the tick.
- **`HitBy(Creature)`** (0103), **`Bolted(Arc)`**, as the hero's client saw it: a `Blow` of the move's `Damage`, the act ends; or
  the arc's projectile's, once a hero.
- **`Release`** (dev): `Place`.
- **`Threatens(peer)`**: one within `ThreatReach` acts or hunts the crew.
- **Kept or gone** (0102): in a hero's keep box; out of all, gone `Linger` after; out of every hard box, at once. **Spawning:** on the beat a herd of the hero's band's kind, `Spacing` apart outward, all or none (`PerKeep`, `Max`, `Apart`). **Guards** (`Kind.Guards`): each
  piece past `Watermark` with a perch draws once: sentries on half.

## Commands

`Strike`: `creatures.gone`, `items.flat`, `items.notRunning`. `HitBy`: `creatures.gone`, `creatures.notAttacking`. `Release`:
`creatures.notHere` (posted; no gravity), `creatures.blocked` (off the grid; in solid), `creatures.full`, `creatures.noPiece`
(a clinger, no wreck). `Fire`: `creatures.noGun`,
`creatures.blocked` (muzzle off the grid), `items.notEnough`, `items.flat`. `Bolted`: `creatures.gone`.

## Guarantees

- Every creature is on the grid, a walker out of everything solid; damage below hit points; ids only grow; never more than
  `Max`; a drive's or act's ticks never past its length; a charge, or an arc a hero, hurts at most once; suits in range; a
  shut shell never hurt; a patch on the grid within `Lasts` (`CreaturesInvariantTests`). Every shot on the grid, in reach, never through solid (`ShotsInvariantTests`).
- No habitats, creatures or new guarded piece: nothing drawn; only `Watermark` moves.

## Deliberately not handled

- **No collisions** (0006); a clinger senses through rock; despawn loses damage.
- **A ram aims where the hero was reported** (0103); a second hero pays nothing.
- **No hit-stun.** **Lifetime** is the keep box's: a wreck goes before its sentry.
- **A derelict escorted aboard** brings its sentry; its pounce passes walls. A blow that breaks its wreck goes unanswered.
- **Shots** (0034, the ADRs): muzzle, pace, charge the client's; a guest's copy stops at blocks the nailer breaks.

# Creatures

## Files

| File | Purpose |
| --- | --- |
| `Arcs.cs` | `Bolted`, `StrikeArc`, `Linger`; `Arcs`, `BeamStop`. |
| `Bounty.cs` | Catchers for hunted heroes. |
| `Clinger.cs` | The sentry clinging to its wreck. |
| `Creatures.cs` | Commands, queries, the hit path. |
| `Drives.cs` | Grazers: `Graze`, `Flee`, `Retaliate`, `Escape`. |
| `Flyer.cs` | The catcher's body: stops at solid. |
| `Fuzz.cs` | Commands the desync suite draws. |
| `Guard.cs` | `Guard` (sentry, lurkers), `Return`, `CutTether`. |
| `Hunt.cs` | The catcher's hunt. |
| `Live.cs` | Dead swept, patches aged, ticks. |
| `Lunge.cs` | A ram along the ground in phases. |
| `Paths.cs` | Weighted A*: runs, flights. |
| `Patches.cs` | Gas, slime: laid, aged, paid by heroes in them. |
| `Pounce.cs` | A straight flight at a hero, never cut. |
| `Rules.cs` | Rule contracts; `RuleBook` finds them. |
| `Shell.cs` | The snail's shell: open near a hero, shut it shields. |
| `Shoot.cs` | The arc: aimed, struck, recovered. |
| `Shots.cs` | Shots: `Fire`, flight, hits, pierces, bursts. |
| `Spawn.cs` | Spawning: posts, herds, guards. |
| `State.cs` | The saved slice: `Creature`, `Shot`. |
| `Ward.cs` | The spire guards' drive. |
| `Vent.cs` | The capback's vent: its gas, a cooldown. |
| `Walker.cs` | The walker: steering, stepping, leaping (0102, 0106). |

Every creature composed (creatures-are-composed): a species names a body, life, faction, ranked
drives, moves and yields, each a rule here. The dead are gone to all from the blow.

- **Depends on:** `Grid`; `Crew` (heroes' cells, `Hunted`; `Blow`, `Shock`, `CallOff`, `Loot`); `Items` (weapons, ammo, drops, charge); `Debris`
  (queries, `Unreel`, `Hit`); `Building` (`TargetAt`, `ShotTakes`, `Surplus`, `Hit`, `Burst`, `Relics`, `Posts`, `PostAt`, `FellPost`); `Rooms` (`Afloat`, `RoomAt`); `Knowledge` (`Earn`).
- **State** (`CreaturesState`, saved): `Creatures` in id order, each `{ Id, Species, X, Y, VX, VY, Damage, Drive, DriveLeft,
  Step, Act, Target, Mem, SeenAt, Path }`, bottom middle in thousandths; `Drive` its brain place, `Act` `{ Move, Phase, Left }`,
  `Mem` rule numbers by key, ordinal; `NextId`; `Clock`; `Shots`, `Arcs`, `Patches`, `Fired`. **Unsaved:** `Dead`. **Derived:** `Posture`, `Fleeing`, `Ended`, `Hits`.
- **Runs** after `Crew` and `Debris`; shots fly after.

## Adding a species or a rule

- **A species:** a record in `Content.Creatures`, art, name. A drive always wants it, one claims hits (or `Mem` answers);
  a brain's order is saved and pinned (`CreatureContractTests`): a change bumps the format.
- **A rule:** a record and a `DriveRule`, `BodyRule` or `MoveRule` (`RuleBook`), holding no state.

## Rules

- **A tick,** in id order: kept or gone; freed; each drive's `Reflex`; an act's clock, or a higher drive that `Wants` it takes
  over, or the one in charge `Run`s (false yields), at none left its `Next`. Its body moves. On the beat, spawns.
- **The walker** (0102, 0106): under gravity; solid: walls, floors, shut doors, the edge, fog.
  It steps `StepUp`; at a higher wall or a `Drop` it tries a leap (`Lands`), else its drive may act (`Blocked`), else turns.
- **The clinger:** clamped to its wreck's top, riding it; off it, it flies through walls.
- **Grazers:** `Graze` idles and wanders, past `Herd` from its kind toward it. `Flee` runs its ticks; cornered, may lunge.
  `Retaliate`, struck in `Reach`, draws `Chance`, flees a while, lunges. `Escape` runs after.
- **The lunge** (0103): wind-up, a charge along the ground at the nearest awake hero till past it, a wall, a drop, `Ticks`.
  A `Steady` one no blow cuts.
- **The sentry:** `Guard`: one within `Notice` alerts it; `AlertTicks` on it pounces within `Strike`, an arc farther; a blow
  it answers so. The pounce crouches `WindUp`, flies `Ticks` at `Speed` at its
  grudge or the nearest hero; `Return` flies it home. The arc: aimed `Lock` before its wind-up ends, to a wall, `Lingers`.
  `CutTether` lets a reel go. Its wreck gone, it dies.
- **Lurkers** (lurker-attacks): walkers on `Guard`, seeing in line. The capback `Vent`s at one within `Strike`
  (`Cooldown`); provoked `Provokes` ticks by answering, it lobs at one `From` to `Reach`, a puddle (`Leaves`) laid as it lands
  `Lingers` on, harmless. The snail's `Shell` opens `Hold` past a hero in `Reach`; it bites, a short lunge.
- **The catcher** (tracker-catchers): a `Hunted` hero gets one
  `RingFrom..RingTo` columns off, unseen. `Hunt` runs a `Path`, beams along its gun (`Gunfire`); lagging or short, `Blink`s
  ahead or near; a `Salvo`; shut out, lobs a charge (`Lob`), held `Tell`. Pounces in `Strike`, answers blows. Out of `Ticks`,
  killed or quarryless: `CallOff`, up. Afloat, all in `Space`, no salvo: it keeps its `Range` at `Accel`, round a flown path,
  leads its beam, fires a `Barrage`; its quarry in a room, thrown `Standoff` off.
- **Patches** (`PatchKind`) last `Lasts`; each breath an awake hero in one seen from its middle pays `Damage` (`Shock`, `Stings`).
- **The warden** (`Posted`, `Ward`): on the beat at a standing post with a standing relic in its arena, a hero in its keep box,
  none tied to it. Woken by an awake hero in the arena, a blow or a sibling, it pounces if no sibling acts (`Flies`: to the first
  solid cell), then `Stuck`. **Kept loop, no drop, never `Dead`:** post or relic gone, or `Calm`. Killed, it fells its post.
- **The hit path** (`Strike`, a shot): unless a drive `Shields` (a shut shell, a shot), damage; dead at hit points, `Yields` sprayed, its `Purse` earned; else knocked
  (`Life.Knock`, `Hop`), its act cut if it may be, drives told in order till one claims it.
- **Shots:** `Fire` takes ammo and its gun's `Energy`; each tick, in id order, one steps, hitting a cell's solid, lowest-id
  creature, machine (the lifepod stops it), wreck (not `Passed`), through bodies its gun `Pierces`, broken cells while it `Carries` some. Falls (`ShotFlight`). One that `Bursts`
  `Burst`s at the first it meets (`Spoils.Drop`; a relic shocks the thrower); wrecks in `Reach` take `WreckDamage`, a creature's heroes it sees its `Damage`.
- **`HitBy(Creature)`** (0103), **`Bolted(Arc)`**, as the hero's client saw it: a `Blow` of the move's `Damage`, the act ends; or
  the arc's projectile's, once a hero.
- **`Release`** (dev): `Place`.
- **`Threatens(peer)`**: one in `ThreatReach` acts or hunts.
- **Kept or gone** (0102): out of every keep box, gone `Linger` after; out of every hard box, at once. **Spawning:** on the beat a
  herd of the hero's band's kind, `Spacing` apart, all or none (`PerKeep`, `Max`, `Apart`). **Guards** (`Kind.Guards`): each
  piece past `Watermark` with a perch draws once: sentries on half.

## Commands

`Strike`: `creatures.gone`, `items.flat`, `items.notRunning`. `HitBy`: `creatures.gone`, `creatures.notAttacking`. `Release` (a catcher: its sender):
`creatures.notHere` (posted; no gravity), `creatures.blocked` (off the grid; in solid), `creatures.full`, `creatures.noPiece`
(a clinger, no wreck). `Fire`: `creatures.noGun`,
`creatures.blocked` (muzzle off the grid), `items.notEnough`, `items.flat`. `Bolted`: `creatures.gone`.

## Guarantees

- Every creature on the grid, walker or catcher out of solid, one afloat all in `Space`; damage below hit points; ids only grow; at most `Max`;
  ticks never past a length; a charge, or an arc a hero, hurts at most once; suits in range; a shut
  shell never hurt; a patch on the grid within `Lasts` (`CreaturesInvariantTests`). Every shot on the grid, in reach, never through solid (`ShotsInvariantTests`).
- Nothing to spawn: nothing drawn; only `Watermark` moves.

## Deliberately not handled

- **No collisions** (0006); a clinger senses through rock; despawn loses damage; no hit-stun.
- **A ram aims where the hero was reported** (0103); a second hero pays nothing. **Lifetime:** the keep box's.
- **A burst hurts no creature; a hero's, no hero.**
- **A catcher:** none on a planet under twice `RingFrom` wide.
- **A derelict escorted aboard** brings its sentry; its pounce passes walls. A blow breaking its wreck: unanswered.
- **Shots** (0034, the ADRs): muzzle, pace, charge the client's; a guest's copy stops at blocks the nailer breaks.

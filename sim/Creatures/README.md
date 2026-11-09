# Creatures

## Files

| File | Purpose |
| --- | --- |
| `Arcs.cs` | Arcs: `Bolted`, `StrikeArc`, `Linger`; `Arcs`. |
| `Bounty.cs` | Catchers for hunted heroes. |
| `Clinger.cs` | The clinger (the sentry) on its wreck. |
| `Creatures.cs` | The commands, the queries, the hit path. |
| `Drives.cs` | The grazers' drives: `Graze`, `Flee`, `Retaliate`, `Escape`. |
| `Flyer.cs` | The catcher's body: stopped by solid, cutting grown rock. |
| `Fuzz.cs` | The `Fuzz` override: commands the desync suite draws. |
| `Guard.cs` | `Guard` (sentry, lurkers), `Return`, the `CutTether` reflex. |
| `Hunt.cs` | The catcher's hunt. |
| `Live.cs` | The system: dead swept, patches aged, creatures ticked. |
| `Lunge.cs` | The lunge: a ram along the ground in phases (0103). |
| `Patches.cs` | Patches (gas, slime): laid, aged, paid by heroes in them. |
| `Pounce.cs` | The pounce: a straight flight at a hero, never cut. |
| `Rules.cs` | The rules' contracts (drive, body, move); `RuleBook` finds them. |
| `Shell.cs` | The shell (the snail): open near a hero, shut it shields. |
| `Shoot.cs` | The arc: crouched, its aim held, struck, a recovery. |
| `Shots.cs` | Shots: `Fire`, flight, hits, pierces, bursts. |
| `Spawn.cs` | Spawning: posts, herds by habitat, guards on derelicts. |
| `State.cs` | The saved slice: `Creature`, `Act`, `Ref`, `Shot`, `CreaturesState`. |
| `Ward.cs` | The spire guards' drive. |
| `Vent.cs` | The vent (the capback): its gas, a cooldown. |
| `Walker.cs` | The walker: steering, stepping, falling, leaping (0102, 0106). |

Every creature composed (creatures-are-composed): a species (`Content.Creatures`) names a body, life, faction, ranked
drives, moves and yields, each a rule here. The dead are gone to all from the blow.

- **Depends on:** `Grid`; `Crew` (heroes' cells, `Hunted`; `Blow`, `Shock`, `CallOff`); `Items` (weapons, ammo, drops, charge); `Debris`
  (queries, `Unreel`, `Hit`); `Building` (`ShotTakes`, `Surplus`, `Hit`, `Burst`, `Wears`, `Wear`, `Relics`, `Posts`, `PostAt`, `FellPost`); `Rooms` (`Afloat`); `Knowledge` (`Earn`).
- **State** (`CreaturesState`, saved): `Creatures` in id order, each `{ Id, Species, X, Y, VX, VY, Damage, Drive, DriveLeft,
  Step, Act, Target, Mem, SeenAt }`, bottom middle in thousandths; `Drive` its brain place, `Act` `{ Move, Phase, Left }`,
  `Mem` rule numbers by key, ordinal; `NextId`; `Clock`; `Shots`, `Arcs`, `Patches`, `Fired`. **Unsaved:** `Dead`. **Derived:** `Posture`, `Fleeing`, `Ended`, `Hits`.
- **Runs** after `Crew` and `Debris`; shots fly after.

## Adding a species or a rule

- **A species:** a record in `Content.Creatures`, art, name. A drive always wants it, one claims hits (or `Mem` answers);
  a brain's order is saved and pinned (`CreatureContractTests`): a change bumps the format.
- **A rule:** a record and a `DriveRule`, `BodyRule` or `MoveRule` (`RuleBook`), holding no state.

## Rules

- **A tick,** in id order: kept or gone; freed; each drive's `Reflex`; an act's clock, or a higher drive that `Wants` it takes
  over, or the one in charge `Run`s (false yields), at none left its `Next`. Its body moves. On the beat, spawns.
- **The walker** (0102, 0106): under gravity; solid: walls, floors, shut doors, the grid's edge, fog.
  It steps `StepUp`; at a higher wall or a `Drop` it tries a leap (`Lands`), else its drive may act (`Blocked`), else turns.
- **The clinger:** clamped to its wreck's top, riding it; off it, it flies through walls.
- **Grazers:** `Graze` idles and wanders, past `Herd` from its kind toward it. `Flee` runs its ticks; cornered, may lunge.
  `Retaliate`, struck in `Reach`, draws `Chance`, flees a while, lunges. `Escape` runs after.
- **The lunge** (0103): wind-up, a charge along the ground at the nearest awake hero till past it, a wall, a drop or `Ticks`.
  A `Steady` one no blow cuts.
- **The sentry:** `Guard`: one within `Notice` alerts it; `AlertTicks` on it pounces within `Strike`, an arc farther; a blow
  it answers so. The pounce crouches `WindUp`, flies `Ticks` at `Speed` at its
  grudge or the nearest hero; `Return` flies it home. The arc: aimed `Lock` before its wind-up ends, to a wall, `Lingers`.
  `CutTether` lets a reel go. Its wreck gone, it dies.
- **Lurkers** (lurker-attacks): walkers on `Guard`, seeing in line. The capback `Vent`s at one within `Strike`
  (`Cooldown`); provoked `Provokes` ticks by answering, it lobs at one `From` to `Reach`, a puddle (`Leaves`) laid as it lands
  `Lingers` on, harmless. The snail's `Shell` opens `Hold` past a hero within `Reach`; it bites, a short lunge.
- **The catcher** (tracker-catchers): on the beat a `Hunted` hero without one gets one `RingFrom..RingTo` columns off,
  unseen. `Hunt` holds `Warn`, flies at `Speed` at its quarry, pounces within `Strike`, answers blows (`AnswerEvery`).
  Its `Flyer` stops at solid, `Wear`ing its box every `CutEvery` if all `Wears`. Out of `Ticks`, killed or quarry gone: `CallOff`, up.
- **Patches** (`PatchKind`) last `Lasts`; each breath an awake hero in one seen from its middle pays `Damage` (`Shock`, `Stings`).
- **The warden** (`Posted`, `Ward`): on the beat at a standing post with a standing relic in its arena, a hero in its keep box,
  none tied to it. Woken by an awake hero in the arena, a blow or a sibling, it pounces if no sibling acts (`Flies`: to the first
  solid cell), then `Stuck`. **Kept loop, no drop, never `Dead`:** post or relic gone, or `Calm`. Killed, it fells its post.
- **The hit path** (`Strike`, a shot): unless a drive `Shields` (a shut shell, a shot), damage; dead at hit points, `Yields` sprayed, its `Purse` earned; else knocked
  (`Life.Knock`, `Hop`), its act cut if interruptible, drives told in brain order till one claims it.
- **Shots:** `Fire` takes an ammo and its gun's `Energy`; each tick, in id order, one steps, hitting a cell's solid, lowest-id
  creature, wreck (not `Passed`), through bodies its gun `Pierces`, broken cells while it `Carries` some. It falls (`ShotFlight.Gravity`). One that `Bursts`
  `Burst`s where it ends (`Spoils.Drop`; a relic shocks the thrower); wrecks in `Reach` take `WreckDamage`.
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

- Every creature on the grid, a walker or catcher out of solid; damage below hit points; ids only grow; at most `Max`;
  ticks never past a length; a charge, or an arc a hero, hurts at most once; suits in range; a shut
  shell never hurt; a patch on the grid within `Lasts` (`CreaturesInvariantTests`). Every shot on the grid, in reach, never through solid (`ShotsInvariantTests`).
- Nothing to spawn: nothing drawn; only `Watermark` moves.

## Deliberately not handled

- **No collisions** (0006); a clinger senses through rock; despawn loses damage. **No hit-stun.**
- **A ram aims where the hero was reported** (0103); a second hero pays nothing. **Lifetime:** the keep box's.
- **A burst hurts no hero or creature.**
- **A catcher** cuts only grown rock. A planet under twice `RingFrom` wide gets none.
- **A derelict escorted aboard** brings its sentry; its pounce passes walls. A blow breaking its wreck: unanswered.
- **Shots** (0034, the ADRs): muzzle, pace, charge the client's; a guest's copy stops at blocks the nailer breaks.

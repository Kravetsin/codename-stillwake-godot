# Creatures

## Files

| File | Purpose |
| --- | --- |
| `Arcs.cs` | Arcs: `Bolted`, `StrikeArc`, `Linger`; `Arcs`. |
| `Bounty.cs` | The bounty: a catcher for each hunted hero, gone once left. |
| `Clinger.cs` | The clinger (the sentry) on its wreck. |
| `Creatures.cs` | The commands, the queries, the hit path. |
| `Drives.cs` | The grazers' drives: `Graze`, `Flee`, `Retaliate`, `Escape`. |
| `Flyer.cs` | The flyer (the catcher): stopped by solid, cutting grown rock. |
| `Fuzz.cs` | The `Fuzz` override: commands the desync suite draws. |
| `Guard.cs` | `Guard` (sentry, lurkers), `Return`, the `CutTether` reflex. |
| `Hunt.cs` | The hunt: warning, chase, pounce, answer, leaving. |
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
drives, moves and yields, each served by a rule here. The dead are gone to all from the blow.

- **Depends on:** `Grid`; `Crew` (heroes' cells, `Hunted`; `Blow`, `Shock`, `CallOff`); `Items` (weapons, ammo, drops, charge); `Debris`
  (queries, `Unreel`, `Hit`); `Building` (`ShotTakes`, `Hit`, `Blast`, `Wear`, `Relics`, `Posts`, `PostAt`, `FellPost`); `Rooms` (`Afloat`); `Knowledge` (`Earn`).
- **State** (`CreaturesState`, saved): `Creatures` in id order, each `{ Id, Species, X, Y, VX, VY, Damage, Drive, DriveLeft,
  Step, Act, Target, Mem, SeenAt }`, bottom middle in thousandths; `Drive` a brain place, `Act` `{ Move, Phase, Left }`, `Mem` by key; `NextId`; `Clock`; `Shots`, `Arcs`, `Patches`, `Fired`. **Unsaved:** `Dead`. **Derived:** `Posture`, `Fleeing`, `Ended`, `Hits`.
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
  (`Cooldown`); provoked `Provokes` ticks, it lobs at one `From` to `Reach`, a harmless puddle (`Leaves`) `Lingers`. The snail's `Shell` opens `Hold` past a hero within `Reach`; it bites, a short lunge.
- **The catcher** (`Hunter`, tracker-catchers): on the beat a `Hunted` hero here without one gets one `RingFrom..RingTo` columns to
  a drawn side (else the other), on the row nearest its own where it fits unseen; none, a later beat. `Hunt` holds `Warn`,
  flies at `Speed` at its quarry, pounces within `Strike` in line; another hero's blow it answers by its shot, once per
  `AnswerEvery`. Its `Flyer` body stops at solid and every `CutEvery` `Wear`s its box's cells if all `Wears` (grown rock).
  Out of `Ticks`, killed or its quarry out or gone, it calls off (`CallOff`); leaving, it flies up through all till unseen.
- **Patches** (`PatchKind`) last `Lasts`; each breath an awake hero in one pays `Damage` (`Shock`, `Stings`).
- **The warden** (`Posted`, `Ward`): on the beat at a standing post with its relic, a hero in its keep box, none tied to it.
  Woken by a hero in the arena, a blow or a sibling, it pounces if no sibling acts (`Flies`), then `Stuck`. **Kept loop, no drop, never `Dead`:** post or relic gone, or `Calm`. Killed, it fells its post.
- **The hit path** (`Strike`, a shot): unless a drive `Shields`, damage; dead at hit points, `Yields` sprayed, its `Purse` earned; else knocked
  (`Life.Knock`, `Hop`), its act cut if interruptible, its drives told in order till one claims it.
- **Shots:** `Fire` takes ammo and `Energy`; each tick, in id order, one steps, hitting a solid cell, the lowest-id creature, a
  wreck (not `Passed`), on through what it `Pierces`, falling (`ShotFlight.Gravity`). One that `Bursts` spreads a `Wave`:
  `Blast`s each wall `Blasts` lets in; wrecks in `Reach` take `WreckDamage`.
- **`HitBy(Creature)`** (0103), **`Bolted(Arc)`**, as the hero's client saw it: a `Blow` of the move's `Damage`, the act
  ends; or the arc's, once a hero.
- **`Threatens(peer)`**: one in `ThreatReach` acts or hunts.
- **Kept or gone** (0102): out of every keep box, gone `Linger` after; out of every hard box, at once. **Spawning:** on the beat a
  herd of the hero's band's kind, `Spacing` apart, all or none (`PerKeep`, `Max`, `Apart`). **Guards** (`Kind.Guards`): each
  piece past `Watermark` with a perch draws once: sentries on half.

## Commands

`Strike`: `creatures.gone`, `items.flat`, `items.notRunning`. `HitBy`: `creatures.gone`, `creatures.notAttacking`. `Release` (a catcher hunts its sender):
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
- **A burst hurts no hero or creature**, only a hit.
- **A derelict escorted aboard** brings its sentry; its pounce passes walls. **Shots** (0034): muzzle, pace, charge the client's.
- **A catcher** cuts only grown rock: a hero walled in by built walls outlasts it. A planet under twice `RingFrom` wide gets none.

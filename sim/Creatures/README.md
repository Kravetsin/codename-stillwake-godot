# Creatures

## Files

| File | Purpose |
| --- | --- |
| `Aboard.cs` | Guards aboard: spawn, rooms. |
| `Arcs.cs` | The arc's hits. |
| `Bounty.cs` | Catchers sent. |
| `Clinger.cs` | The sentry's grip. |
| `Creatures.cs` | Commands, queries, the hit path. |
| `Dock.cs` | A guard asleep in its dock. |
| `Drives.cs` | Grazers' drives. |
| `Flyer.cs` | A flying body. |
| `Fuzz.cs` | For the desync suite. |
| `Guard.cs` | `Guard`, `Return`, `CutTether`. |
| `Hunt.cs` | The catcher's hunt. |
| `Live.cs` | The tick. |
| `Lunge.cs` | A ram along the ground. |
| `Nests.cs` | Packs on wrecks. |
| `Paths.cs` | A*: runs, flights. |
| `Patches.cs` | Gas, slime. |
| `Patrol.cs` | The escort's hunt. |
| `Pounce.cs` | A flight at a hero. |
| `Prowl.cs` | The spider's hunt; a pry. |
| `Rules.cs` | Contracts, `RuleBook`. |
| `Shell.cs` | The snail's shell. |
| `Shoot.cs` | The arc. |
| `Shots.cs` | Shots, bursts. |
| `Spawn.cs` | Posts, herds, guards. |
| `State.cs` | The saved slice. |
| `Swarm.cs` | A pack's drive. |
| `Ward.cs` | The spire guards' drive. |
| `Vent.cs` | The capback's vent. |
| `Walker.cs` | Steps, leaps, climbs. |

Every creature composed (creatures-are-composed): a species (`Content.Creatures`) names a body, life, faction, ranked drives,
moves and yields, each a stateless `DriveRule`, `BodyRule` or `MoveRule` (`RuleBook`); a drive always wants it, one claims
hits. A brain's order is pinned (`CreatureContractTests`). The dead are gone from the blow.

- **Depends on:** `Grid` (ops: `SetDoorway`, a guard's pry); `Crew` (heroes' cells, `Hunted`; `Blow`, `Shock`, `CallOff`, `Loot`); `Items` (weapons, ammo, drops, charge); `Debris`
  (queries, `Unreel`, `Hit`, `TakeBursting`); `Building` (`TargetAt`, `ShotTakes`, `Surplus`, `Hit`, `Burst`, relics and posts); `Rooms` (`Afloat`, `RoomAt`); `Knowledge` (`Earn`).
- **State** (`CreaturesState`, saved): `Creatures` in id order, each `{ Id, Species, X, Y, VX, VY, Damage, Drive, DriveLeft,
  Step, Act, Target, Mem, SeenAt, Path }`, bottom middle in thousandths; `Drive` its brain place, `Act` `{ Move, Phase, Left }`,
  `Mem` rule numbers by key, ordinal; `NextId`; `Clock`; `Shots`, `Arcs`, `Patches`, `Nests`, `Fired`. **Unsaved:** `Dead`. **Derived:** `Posture`, `Fleeing`, `Ended`, `Hits`, `Docks`.
- **Runs** after `Crew` and `Debris`; shots fly after.

## Rules

- **A tick,** in id order: kept or gone; freed; drives' `Reflex`; an act's clock, or a higher drive that `Wants` it takes
  over, or the one in charge `Run`s (false yields), at none left `Next`. Its body moves. On the beat, spawns.
- **The walker** (0102, 0106): under gravity; solid: walls, floors, shut doors, the edge, fog.
  It steps `StepUp`; at a higher wall or a `Drop` it tries a leap (`Lands`), else its drive may act (`Blocked`), else turns.
- **The clinger:** clamped to its wreck's top, riding and turning with it (`StanceOf`, `BoxOf`); off it, it flies through walls.
- **Grazers:** `Graze` idles, wanders, toward its kind past `Herd`. `Flee` runs; cornered, may lunge. `Retaliate`, struck in
  `Reach`, draws `Chance`, flees, lunges. `Escape` after.
- **The lunge** (0103): wind-up, a charge along the ground at the nearest awake hero till past it, a wall, a drop, `Ticks`.
  A `Steady` one no blow cuts.
- **The sentry:** `Guard`: one within `Notice` alerts it; `AlertTicks` on it pounces within `Strike`, an arc farther; a blow
  it answers so. The pounce crouches `WindUp`, flies `Ticks` at `Speed` at its grudge or the nearest hero's middle; `Return`
  flies it home. The arc: aimed `Lock` before its wind-up ends, to a wall, `Lingers`. `CutTether` lets a reel go. Its wreck gone, it dies.
- **Lurkers** (lurker-attacks): walkers on `Guard`, seeing in line. The capback `Vent`s within `Strike` (`Cooldown`); provoked
  (`Provokes`), lobs a harmless puddle `From`..`Reach` (`Leaves`, `Lingers`). The snail's `Shell` opens `Hold` past a hero in
  `Reach`; it bites, a short lunge.
- **The catcher** (tracker-catchers): a `Hunted` hero gets one `RingFrom..RingTo` columns off, unseen. `Hunt` runs a `Path`,
  beams (`Gunfire`), `Blink`s, a `Salvo`; shut out, a `Lob`. Pounces in `Strike`. Out of `Ticks`, killed or quarryless: `CallOff`.
  Afloat (`Space`): keeps its `Range` at `Accel`, a `Barrage`, a `Missile`; its quarry in a room, thrown `Standoff` off.
- **Packs** (companion-fights): a far piece may draw a `Nest`, which wakes struck or neared, all out or waits, gone with it.
  `Swarm` rings the nearest hero out; one `Every` pounces (`AtTarget`, `Cuttable`). A flyer afloat is knocked along a blow.
- **Patches** (`PatchKind`) last `Lasts`; each breath an awake hero in one seen from its middle pays `Damage` (`Shock`, `Stings`).
- **The warden** (`Posted`, `Ward`): on the beat at a post, its arena's relic standing, a hero in its keep box.
  Woken by a hero in the arena, a blow or a sibling, it pounces if no sibling acts (`Flies`), then `Stuck`. **Kept loop, no
  drop, never `Dead`:** post or relic gone, or `Calm`. Killed, it fells its post.
- **Vessel guards** (derelict-guards, `*AboardTests`): spawned first, asleep at `Shipyard.Guards`' docks. `Lifetime.Aboard`:
  never swept. `Dock`: a hero's cell in its room or a blow wakes it for good. The spider `Prowl`s by decks, hatch well and walls;
  the escort `Patrol`s its deck, claws near, fires the `gauss` afoot. A shut doorway: `Pry` ticks, then `SetDoorway`; never the way in.
- **The hit path** (`Strike`, a shot): unless a drive `Shields`, damage; dead at hit points, `Yields` sprayed, `Purse` earned;
  else knocked (`Life.Knock`, `Hop`), its act cut if it may be, drives told till one claims it.
- **Shots:** `Fire` takes ammo and `Energy`; each tick, in id order, one steps to a solid cell, the lowest-id creature, a
  machine (the lifepod stops it), a wreck (not `Passed`), on through bodies it `Pierces`, cells it `Carries`; falls
  (`ShotFlight`). `Homes`: turns `Turn` a tick, bursts in `Reach`; `Bursts`: at the first it meets. **A burst** (fuel-drums,
  debris-bursts): wrecks in `Wave` take `WreckDamage`, awake heroes and live creatures it sees `Body`, each once.
- **`HitBy(Creature)`** (0103), **`Bolted(Arc)`**, as a hero's client saw it: a `Blow` of the move's `Damage`, the act ends; or
  the arc's projectile's, once a hero. **`Release`** (dev): `Place`. **`Threatens(peer)`**: one in `ThreatReach` acts or hunts.
- **Kept or gone** (0102): out of every keep box, gone `Linger` after; out of a hard box, at once. **Spawning:** on the beat a
  herd of the hero's band's kind, `Spacing` apart, all or none (`PerKeep`, `Max`, `Apart`). **Guards** (`Kind.Guards`): each
  piece past `Watermark` with a perch draws once: sentries on half.

## Commands

`Strike`: `creatures.gone`, `items.flat`, `items.notRunning`. `HitBy`: `creatures.gone`, `creatures.notAttacking`. `Release` (a catcher: its sender):
`creatures.notHere` (posted, aboard; no gravity), `creatures.blocked` (off the grid; in solid), `creatures.full`, `creatures.noPiece`
(a clinger, no wreck). `Fire`: `creatures.noGun`, `creatures.blocked` (muzzle off the grid), `items.notEnough`, `items.flat`. `Bolted`: `creatures.gone`.

## Guarantees

- Every creature on the grid, walker or flyer out of solid, a catcher afloat all in `Space`; damage below hit points; ids only grow; at most `Max`;
  ticks never past a length; a charge, or an arc a hero, hurts once at most; suits in range; a shut
  shell never hurt; a patch on the grid within `Lasts` (`CreaturesInvariantTests`). Every shot on the grid, in reach, never through solid (`ShotsInvariantTests`).
- Nothing to spawn: nothing drawn; only `Watermark` moves.

## Deliberately not handled

- **No collisions**; a clinger senses through rock; despawn loses damage; no stun.
- **A ram aims where the hero was reported** (0103); a second hero pays nothing.
- **A catcher:** none on a planet under twice `RingFrom` wide. **A derelict escorted aboard** brings its sentry; its pounce
  passes walls; a blow breaking its wreck: unanswered. **Guards:** no stealth, never re-docked; none on wrecks or stations.
- **Shots** (0034): muzzle, pace, charge the client's; a guest's copy stops at blocks the nailer breaks.

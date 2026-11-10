# Plan: The catcher's homing missile

Status: reviewed (design, completeness, clarity, determinism) and agreed with the user (2026-11-10). Step 4 of
[catchers-space](catchers-space.md), redone for the fight that [catcher-motion](../decisions/2026-11-10-catcher-motion.md)
built since: the catcher keeps a range round its hero, fires its laser every 2.5 s and a barrage every 25 s. Changed from
step 4 there: the missile's beat is fitted to the barrage; the fuse is the burst's reach; its flight is the hunt's number.

## Decided with the user (catchers-space)

- In space, beside the laser, a homing missile on a regular beat: it turns slowly after a hero and bursts on the first
  thing it meets; escaped by dodging or by leading it into a wreck; it cannot be shot down.
- A creature's burst breaks hull at full strength; the room vents as for any breach (one damage rule).
- Not on planets; no other creature fires one; no hero carries one.

## Out of scope (deliberate omissions)

- Shooting the missile down: a hero's shot passes it, as shots never meet shots.
- Missiles on planets; homing for anything else; leading its target (it turns toward where a hero is, so a hero who keeps
  turning inside its circle escapes).
- The missile meeting loose pieces (not wrecks): it stops at cells, creatures and wrecks, as every shot.
- A laser shot during the missile's tell is allowed; a play pass holds the laser off then if the launch reads badly.
- A barrage starting while a missile flies is allowed: a harder moment, not a bug; a play pass may forbid it.
- A missile launched against a wreck or a wall beside its catcher bursts at once. The catcher keeps 20–35 cells off its
  hero, past the burst's reach, so only the wreck takes it.
- A missile hurts a creature it meets as any shot does. In space a hunt is the only creature that fights, one at a time.
- An incoming-missile alert off screen: the catcher keeps within its range, so its missiles start on screen.

## Approach

**Content (catcher).**
- `Hunt.Missile(Every, Tell, Flight, Projectile)`: `Every: 300` (15 s), `Tell: 20` (1 s), `Flight: 160` (8 s),
  `Projectile: "missile"`. `ContentTests`: `Flight < Every`, so a catcher never has two in flight.
- `Projectile` gains `Homes`, a `Homing(Speed, Turn)`, null for none. The missile: `Damage: 6`, `BlockDamage: 0`, weight 0,
  `HitsBodies: true`, bursting as `Blasting.Missile(Power: 450, Reach: 4, WreckDamage: 60)` (a scrap hull's cell struck breaks, iron cracks); `Speed: 1000` thousandths of a
  cell a tick (20 cells a second at 20 ticks a second; the jetpack's top is 30), `Turn: 24` fine headings a tick (some 42°
  a second: a circle some 27 cells across). Every number a placeholder for a play pass.

**The beat (`HuntDrive`, space only).** New `Mem` keys: `MissileAt`, the clock the catcher's next missile tell may start
(its first a whole `Every` after the hunt's first look, as `BarrageAt`); `LaunchAt`, the clock its missile tell ends, 0 for
none.
- The tell starts in `Afloat`, beside `BarrageDue`, when: `Clock >= MissileAt`, the quarry not shut in a room, no barrage
  running (`Volley` 0), the quarry in sight within `Range.Far` (the gate `BarrageDue` uses, shared). Then
  `LaunchAt = Clock + Tell`. Through the tell the catcher keeps its range: the range is what makes the fight.
- The launch is checked in `Run` before every early return (blink, volley, standoff, hurt): when `LaunchAt != 0` and
  `Clock >= LaunchAt`, the missile leaves from wherever the catcher is, headed at its quarry's middle, even out of sight
  or shut in a room by then (a tell is a promise; the hull takes it and the room vents). Then `LaunchAt = 0`,
  `MissileAt = Clock + Every`.
- `BarrageDue` also asks `LaunchAt == 0`. `EndBarrage` sets `MissileAt = Clock + Every`, as it restarts the laser's wait.
  `Leave` clears `LaunchAt`; a leaving catcher's missile flies on.
- `FightOf` gains `LaunchLeft`, the ticks left of the missile tell (0 for none), as `ThrowLeft` is.

**The flight (`CreaturesModule`, `Shots.cs`).**
- `Shot` gains `int? Heading`, a fine heading, written only when not null (as `Carry`): rotating `VX/VY` in integers
  would shrink its speed. Null is a shot that does not home; 0 is east.
- `Launch(c, projectile, x, y, heading, flight)` beside `Lob`: `By = Ref.None` (a creature's), `VX/VY` from
  `Rays.FineDirection(heading)` times `Speed` over 1000, truncated toward zero; `Left = flight`; the catcher in `Passed`,
  as a lob's thrower (saved as `passed`, so a load keeps it).
- Each tick, before its common step, a shot with a `Heading`:
  - Its target is the nearest awake hero's middle (`Middle` of its reported cell), by squared distance in thousandths,
    walking `_crew.Heroes` in order and taking only a strictly nearer one: in co-op a nearer friend draws it off (a
    decoy works); a hero gone into a room draws it onto the hull.
  - Within its burst's `Reach` cells of that point, it bursts where it is (`ShotEnd`, `Met: false`).
  - Else it turns toward that point by `Rays.FineApart` clamped to ±`Turn` and sets `VX/VY` from its heading. No awake
    hero: it flies on straight.
- Then the common step: it bursts on a cell, a creature or a wreck it meets, or when `Left` runs out (`Burst`): cells and
  hull by `Power` at full strength, wrecks in reach by `WreckDamage`, heroes in reach and in sight by its `Damage`.

**Client.** The missile drawn along its heading each tick from the sim's shot (no straight-line extrapolation); its trail;
the tell from `LaunchLeft`; its burst reuses the shell's puff and sound. Picked by the user first: the missile and its trail,
the tell's look and tone, its flight sound.

## Determinism and co-op

No new input: heroes' reported cells only. Headings are integer fine headings; `FineApart` decides a turn's side, its
half-turn tie going the positive way; speeds truncate toward zero; distances are squared `long`s. Shots fly in id order;
the nearest hero is taken in `_crew.Heroes` order, strictly nearer only. `Heading` (null for none) and the two `Mem` keys
(0 for none) need no format bump. A guest's dodge is judged about one round trip late, as the shell's.

## Tests (`CatchersTests`)

- The beat: the first tell some `Every` after the hunt's first look; none during a barrage; no barrage during a tell; none
  at a hero shut in a room; a tell ending mid-blink still launches; a barrage's end restarts the wait.
- The flight: it turns at most `Turn` a tick; it reaches a still hero and bursts within `Reach`; a hero flying a circle
  wider than its turn escapes; a wreck between takes `WreckDamage` and stops it; on the hull the room vents; it bursts when
  its flight runs out; it never meets its own catcher, also when saved on its launch tick; a nail flies through it; with two
  heroes it turns to the nearer.
- Saved mid-tell and mid-flight with a heading of 0 and loaded, two worlds play on alike.

## Steps

1. Sim: content, `Heading`, `Launch`, the homing step, the beat, `FightOf`; tests; README lines.
2. Pick pages: the missile and its trail; the tell; its flight sound.
3. Client: the picks; a dev room in space; the user plays it.
4. ADR (`catcher-missile`), catchers-space's step 4 marked done, glossary.

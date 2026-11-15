# Plan: Catchers in space, and a silent bracelet

Status: designed and agreed with the user (2026-11-10); step 1 built as [catcher-loot](../decisions/2026-11-10-catcher-loot.md), which wins where they differ; step 2 built. Slice 2 of [catchers](catchers.md), less its kits. The
planet fight is [catcher-combat](../decisions/2026-11-09-catcher-combat.md); the meter is
[tracker-catchers](../decisions/2026-11-09-tracker-catchers.md).

## Decided with the user

- The aim: light tension, so a player cannot farm forever unpunished. Hunts read as random encounters, never as a timer or
  a slowdown; short trips usually pass without one.
- **The meter, one mechanic everywhere:** hidden; it grows by the worth (`Market.WorthOf`) of what a hero breaks or kills,
  credited to the one who broke it (a bomb's thrower) when it yields, picked up or not: soil and rock are worth 1, an opal
  150, so digging barely stirs it and greed does. Everything counts but taking apart by plan or screwdriver. Moving counts
  for nothing. A random need per hunt, sized to 10–20 minutes of greedy mid-game salvage. One catcher at a time; a surplus
  past full is lost. Leaving a surface still empties it (the user: the reset keeps it less predictable).
- **The starting belt:** in space, a fixed box round the starting wreck (the stream's at the start: the scoop and 128
  columns either side, 24 rows above and below; it does not grow with the ship), the meter drains a little every breath,
  so short trips and work at home never bring a catcher (the user: punish only the greedy, not a 99% meter and one more
  piece). Out of it, it never drains.
- **The bracelet:** no blinking from two thirds, anywhere. Silent till the meter fills; then an alarm and 30–60 s before a
  catcher comes.
- **In space** the same catcher flies (no runs), blinks ahead when lagging, fires the laser. The laser stops at the first
  wreck it meets and damages it (the user: a fight that chews up the debris looks more epic). Instead of the salvo, a homing
  missile on a regular beat: it turns slowly after the hero and bursts on the first thing it meets; escaped by dodging or by
  leading it into a wreck; it cannot be shot down.
- **A hero hiding aboard:** the catcher throws its charge at the hull; the breach vents the room like any other.
- **Rejected:** a meter with decay everywhere and a cap (a regular cadence a player learns); dice per action; a
  fast-filling, fast-cooling visible meter; any visible meter, signal strength or floating blink threshold; a meter from
  moving and working (weights per action); a safe zone measured from the hull (it grows with the ship, so a big ship's
  stream is never hunted); counting what enters a pack (chests, furnaces, trades and friends' gifts would fill it).
- **Skipped for now:** several full meters at once bringing as many catchers.

## Out of scope (deliberate omissions)

- Kits (varied weapons and habits per hunt): the next slice.
- Shooting the missile down; the missile meeting loose (non-wreck) pieces: it stops at cells, creatures and wrecks, as every shot.
- A catcher using doors or airlocks, a path search in space, a blink into a room or to a spot that sees into one.
- A hunt following its hero to another surface; the salvo in space; the missile on planets; the laser breaking cells.
- A homing missile for any other creature or for heroes: no named user.
- Loot no hero's hand broke (nets, drones, machines) fills no meter: the hunt answers a hero's greed, not the ship's.

## Approach

**The meter and the bracelet (Crew).**
- A new op `ICrewOps.Loot(peer, worth)` adds to the hero's meter, up to its need. Its callers are the modules that already
  hand a hero what it broke (`GiveOrDrop`/`DropAt`: Building's breaking, bursts, digging and flora; Debris's salvage) and
  Creatures for a kill, each summing `Market.WorthOf` of the yield. Building's taking apart by plan or
  screwdriver does not call it. All of them already depend on Crew (Debris through queries now: it gains the ops).
- `Stirring`, `Wanted.Open`, `Wanted.Under` and `Wanted.ActiveFor` go. `NeedLeast..NeedMost` becomes a worth (placeholders
  till a play pass measures 10–20 minutes of greedy salvage).
- In space, a hero whose cell lies in `Wanted.Belt` (a half-box round the grid's middle, where the scoop is placed) loses
  `Wanted.Drain` a breath, not below 0. A full meter does not drain: its catcher is coming.
- Full, the meter draws `Coming`, `Wanted.ComingLeast..ComingMost` (30..60) breaths, from the world's `Rng`, saved in the
  hero, default 0, counted down a breath at a time whatever the hero does.
- `Signal` becomes `Calm`, `Coming`, `Hunted` (full and `Coming` 0). `Signal.Blinking` and `Wanted.Blink` go. The bounty
  sends a catcher only for a `Hunted` hero, planet and space alike. The alarm sounds on the `Calm`→`Coming` edge, for the
  hunted player only; no new event.

**The catcher in space (Creatures).**
- The bounty runs where things float too: the same ring out of view, every body cell in `Space`, never in a room.
- No path in space (`Plan` does not run): it flies straight at its quarry's reported cell at `Speed`, sliding along what
  blocks it. Superseded: it keeps its range by a flown path, leads its laser and fires a barrage
  ([catcher-motion](../decisions/2026-11-10-catcher-motion.md)); the missile below still comes on its own beat. `Short` is set in space when the quarry's cell lies in a room (redefined, not the path search's flag).
- The lag blink is the planet's, its spot needing no ground and lying wholly in `Space`.
- Short, it throws the charge (`LobAtShelter`) at the first solid on the line to its quarry. A creature's burst breaks hull
  at full strength; Rooms and Atmosphere vent the room as for any breach.

**The laser.** A burning arc (`Through`) also ends where it enters the first wreck box and takes `Laser.WreckDamage` off that
wreck through `IDebrisOps`, once per beam. Cells still take nothing. The sentry's arcs are unchanged.

**The missile.**
- `Hunt.Missile(Every, Tell, Flight, Projectile)` takes the salvo's place where things float: every `Every` ticks (about
  15 s), its quarry within the arc's reach and none of its own missiles flying, it holds still `Tell` ticks (shown through
  `FightOf`), then fires.
- The projectile carries `Homing(Speed, Turn, Fuse)`. The shot saves one new field, `Heading` (default none; rotating
  `VX/VY` in integers would shrink its speed). Each tick it turns at most `Turn` heading steps toward the nearest awake
  hero's reported cell (in co-op it may follow another hero) and flies at `Speed` from `Headings.Sines`.
- It bursts on a cell, a creature or a wreck, within `Fuse` cells of an awake hero's reported cell, or when `Flight` runs out,
  as `Blasting.Shell`: wrecks and hull take damage, heroes are hurt as by any creature burst. Judged by the sim, like the
  shell; a guest's dodge is judged about one round trip late (heroes near a creature report each changed cell, `Threatens`).

**Client.** The bracelet shows `Signal`, without the blink (`TrackerBlink` goes). For the user's pick pages: the missile and
its trail, its launch tell, a lock tone, the alarm (today's beep or a new sound), the laser striking a wreck.

## Determinism and co-op

Every input is an existing command. `Coming` is drawn in the breath, in hero id order. A missile turns by the sign of an
integer cross product, clockwise on a tie; speeds truncate toward zero; missiles launch in creature id order and fly in shot
id order. A joiner gets the meter, `Coming` and the shots in the save. New saved fields have defaults: no format bump.

## Tests

- Scenarios: breaking a cell, salvaging a wreck, a bomb's burst (to its thrower) and a kill each add their yield's worth to
  the one who did it, picked up or not; taking apart by screwdriver adds nothing; walking adds nothing; in the belt the meter
  drains, out of it it does not, a full one never; the signal stays `Calm` at two thirds, `Coming` counts down, then
  `Hunted`, and only then a catcher; leaving a surface empties a filling meter.
- A space catcher spawns in `Space` out of view, slides along the hull, blinks never into a room; the beam stops at a wreck
  and damages it once; the missile turns at most `Turn` a tick, bursts at a wreck, at the hull (the room vents), within
  `Fuse` of a hero, at the end of its flight; a hero's shot passes through it; a charge breaches a sealed room.
- Invariants (FsCheck): the meter stays within 0..need and grows only by a `Loot`; every body cell of a space catcher is in
  `Space`. Save, load and fuzz are the generic suites'.

## Steps

1. Done (catcher-loot): `Loot` and its callers, `Coming`, the `Signal` change, the alarm; `TrackerBlink` kept for the urgent blink.
2. Done: space opens: `Loot` fills where things float, the belt's drain; a test that taking apart fills nothing (only the ship has
   `Deconstruct`). Creatures: the bounty and the flight in space, the space blink, short → charge; `--catcher` alone aboard.
3. The laser meeting and damaging wrecks.
4. Done (catcher-missile): the missile, its beat, its flight and burst; its look, tell and sounds picked and in the client.
5. Pick pages (alarm, laser on a wreck), then the client; ADR, READMEs, glossary; a dev room in space.

# Plan: Catchers in space, and a silent bracelet

Status: designed and agreed with the user (2026-11-10); not built. Slice 2 of [catchers](catchers.md), less its kits. The
planet fight is [catcher-combat](../decisions/2026-11-09-catcher-combat.md); the meter is
[tracker-catchers](../decisions/2026-11-09-tracker-catchers.md).

## Decided with the user

- The aim: light tension, so a player cannot farm forever unpunished. Hunts read as random encounters, never as a timer or
  a slowdown; short trips usually pass without one.
- **The meter, one mechanic everywhere:** hidden; grows only from activity (`Stirring`); never decays while out; a random
  need per hunt of 10–20 minutes of activity (`Need` 1200..2400: under rock, at half the rate, 20–40). In space it grows only
  while the hero is far from the ship. Leaving a surface still empties it (the user: the reset keeps it less predictable).
- **The bracelet:** no blinking from two thirds, anywhere. Silent till the meter fills; then an alarm and 30–60 s before a
  catcher comes.
- **In space** the same catcher flies (no runs), blinks ahead when lagging, fires the laser. The laser stops at the first
  wreck it meets and damages it (the user: a fight that chews up the debris looks more epic). Instead of the salvo, a homing
  missile on a regular beat: it turns slowly after the hero and bursts on the first thing it meets; escaped by dodging or by
  leading it into a wreck; it cannot be shot down.
- **A hero hiding aboard:** the catcher throws its charge at the hull; the breach vents the room like any other.
- **Rejected:** a salvage-weighted meter with decay and a cap (a regular cadence a player learns); dice per action; a
  fast-filling, fast-cooling visible meter; any visible meter, signal strength or floating blink threshold.

## Out of scope (deliberate omissions)

- Kits (varied weapons and habits per hunt): the next slice.
- Shooting the missile down; the missile meeting loose (non-wreck) pieces: it stops at cells, creatures and wrecks, as every shot.
- A catcher using doors or airlocks, a path search in space, a blink into a room or to a spot that sees into one.
- A hunt following its hero to another surface; the salvo in space; the missile on planets; the laser breaking cells.
- A homing missile for any other creature or for heroes: no named user.

## Approach

**The meter and the bracelet (Crew).**
- In space an active, awake hero adds `Wanted.Open` per breath only while no ship structure lies within `Wanted.Near` (24)
  cells of it: a scan of the square round its cell through `IGridQueries` (`StructureLayer`), stopping at the first hit,
  skipping `Reserved` cells as Debris's `IsHull` does, so a vessel being cut is not the ship. Once a breath per hero; no cache.
  Aboard or near the hull, ship structure is in reach: nothing.
- `NeedLeast..NeedMost` becomes 1200..2400 for both places.
- Full, the meter draws `Coming`, `Wanted.ComingLeast..ComingMost` (30..60) breaths, from the world's `Rng`, saved in the
  hero, default 0, counted down a breath at a time whatever the hero does.
- `Signal` becomes `Calm`, `Coming`, `Hunted` (full and `Coming` 0). `Signal.Blinking` and `Wanted.Blink` go. The bounty
  sends a catcher only for a `Hunted` hero, planet and space alike. The alarm sounds on the `Calm`→`Coming` edge, for the
  hunted player only; no new event.

**The catcher in space (Creatures).**
- The bounty runs where things float too: the same ring out of view, every body cell in `Space`, never in a room.
- No path in space (`Plan` does not run): it flies straight at its quarry's reported cell at `Speed`, sliding along what
  blocks it. `Short` is set in space when the quarry's cell lies in a room (redefined, not the path search's flag).
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

- Scenarios: the meter grows beyond `Near`, not within it, not aboard, not idle, not with no ship, not by a reserved vessel;
  a tethered hero at full line (test, not assumption); the signal stays `Calm` at two thirds, `Coming` counts down, then
  `Hunted`, and only then a catcher; leaving a surface empties a filling meter.
- A space catcher spawns in `Space` out of view, slides along the hull, blinks never into a room; the beam stops at a wreck
  and damages it once; the missile turns at most `Turn` a tick, bursts at a wreck, at the hull (the room vents), within
  `Fuse` of a hero, at the end of its flight; a hero's shot passes through it; a charge breaches a sealed room.
- Invariants (FsCheck): the meter never grows with ship structure within `Near`; every body cell of a space catcher is in
  `Space`. Save, load and fuzz are the generic suites'.

## Steps

1. Crew: `Coming`, the `Signal` change, the space rule, `Need` 1200..2400; delete `Blinking`, `Wanted.Blink`, `TrackerBlink`
   and their tests; the alarm on the edge. Playable alone: planets lose the blink.
2. Creatures: the bounty and the flight in space, the space blink, short → charge.
3. The laser meeting and damaging wrecks.
4. The missile: `Homing`, `Heading`, the beat, the burst.
5. Pick pages (missile, tell, lock tone, alarm, laser on a wreck), then the client; ADR, READMEs, glossary; a dev room in space.

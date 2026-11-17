# Plan: Guards aboard intact vessels

## Why

An intact vessel puts up no fight today: a hero walks room to room and empties it, so a liner is only a longer freighter.
This plan puts guards aboard: a **spider** asleep in a capsule on a cabin's back wall, and a **guard robot** hung in a stand
in a big hall. Both wake when a hero walks into their room and then hunt the crew through the vessel.

The design and its review are in `derelict-enemies-design.md`. Its conclusions, after the reviewer's cuts: no new module
(Creatures owns the guards, Core's Shipyard places them); no new path search (the catcher's `Paths` stays the flyer's, and
both guards route by the vessel's simple shape: decks, doorways, one hatch well); no re-docking; no stealth.

**Where it is heading** (0049): guards placed from a vessel's description and woken by its rooms. Users now: intact
vessels. Next: wrecked vessels (the vessel-wrecks plan) and enemy vessels (`vessels.md`). Placement lives in Core's
Shipyard so those reuse it.

**Words.** A *guard* is a creature placed aboard a vessel at a *dock*: a spider's *capsule* or a robot's *stand*. A *room*
is a `Vessel.Compartment` (not a `Rooms` room); a *small* room is one deck tall, a *big* one more (liner and tanker blocks).
The *arrival room* is where a hero comes aboard; the *way-in door* is its door to space (`Shipyard.Door`). The *sentry* is
the space-debris creature (look «Зонд»); the *warden* the spire guard, whose sleep-and-wake the guards borrow; *Istukan* is
the warden's rig. `Escort` is the robot's species, look A1 «Конвоир». `Mem` is a creature's saved memory by key (Creatures
README). Lengths in cells (8 px), times in ticks (20 a second); every number named below lives in `Content`.

## Rules (confirmed with the user, 2026-11-17)

- Only intact vessels. Wrecked ones get no guards for now.
- **Spiders:** one per 5 small rooms, rounded up (1–5 → 1, 6 → 2), each in a distinct random small room.
- **Robots:** one per 3 big rooms, rounded up, each in a distinct random big room. No big rooms, no robots.
- Never a guard in the arrival room.
- A guard wakes when a hero **enters** its room. Seeing the room through an open door does not wake it; a hero standing
  in a doorway's columns is in neither room. A blow on a sleeping guard, from anywhere, wakes it (as the warden).
- Once awake it never sleeps again. It hunts while any awake hero is aboard, whether or not it can reach one.
- **Spider:** the sentry and its own combat (arc strike, pounce). Indoors it runs on the floor and on solid walls inside
  the hull, never on back walls, ceilings or the outside of the hull. It jumps. It opens doors and hatches by itself.
  Asleep it sits folded in capsule G1 «Зрачок — впадина» on its room's back wall (a background object, no solid cell);
  waking, the hazard lamp flashes, the iris opens, it unfolds and drops to the floor.
- **Robot** A1 «Конвоир»: at most 5.5 cells tall with its antenna (a doorway is 6), walks, never jumps, opens doors only.
  Awake, it patrols and searches its deck. Hero far and in line → stops and fires a gauss shot (straight, fast, no
  explosion). Hero close → dashes and claws. Hero above, out of the claw's reach but in line → aims up and shoots. Hero out
  of line and reach → it walks off through a door into the next rooms of its deck. Asleep it hangs in stand S1 «Люлька»
  0.4 cells off the floor (the stand's yoke holds it by the shoulders; the spider's capsule sits on the wall, so the two
  differ); waking, the clamp swings up, the drum lowers it to the floor, the yoke opens, its lamp lights red, it steps off.
- **Loot:** a killed spider gives the sentry's yields; a killed robot gives more circuits and scrap.
- No guard ever opens the way-in door. A killed guard's dock stays open and empty, also when it was killed asleep.

## Design

### Placement: `Shipyard.Guards(Vessel)` (sim/Core `Vessels/`)
A pure function of the vessel, drawing from Shipyard's stream **3** (`Draws(cls, seed, 3)`; 0–2 are taken), never the
world's Rng. Candidates: small and big compartments (`Decks == 1` / `> 1`) in compartment index order, the arrival room left
out, and only rooms where the dock fits: a capsule (4¾ cells wide) on a stretch of back wall no furniture covers, a stand on
floor columns no fitting covers. Count = `min((n + per - 1) / per, candidates)`, `per` 5 for spiders and 3 for robots, `n`
counted over all rooms of that size (arrival room included, so the rule reads as the user said it). Rooms are drawn by a
partial Fisher–Yates over the candidate list; each dock's column by one `Rng.Next(lo, hi)` over the fitting columns. The
result is in a fixed order: spiders then robots, each by compartment index. The rules (`per`, species, dock sizes) are
`Content/Vessels.cs` `Aboard`.

### Guards in Creatures (sim/Creatures)
- **Spawn:** in `Opened()`, before any other spawn on that surface, one sleeping guard per dock in `Guards` order, so
  creature ids follow it. It reads only `Shipyard.Guards` (re-derived from `Surfaces.Kind` and `Surfaces.Seed`), never the
  grid: Places writes the vessel after Creatures runs. Guards count toward `Creatures.Max`; a liner's largest count is
  pinned under it by a test. A guard remembers only its dock's index (`Mem("dock")`); the dock list, the rooms and the
  way-in door are `[JsonIgnore]`, rebuilt in `Rebuild()`.
- **Never swept:** a new `Lifetime.Aboard`: not culled by keep boxes (a vessel is wider than one); gone only when killed or
  when the vessel world closes (it never reopens, Places).
- **Waking:** the warden's wake counter (`Wake` ticks), with one more trigger: an awake hero's **feet cell** (the crew's
  reported cell) inside the dock's room. A hit wakes it and then applies as usual (damage, knock, a kill from sleep). The
  waking stage, a pry's start tick and the wall a spider holds are saved in its `Act`/`Mem`; `Posture` stays derived.
- **Quarry:** the nearest awake hero aboard by squared cell distance, ties to the first in the crew's order (as `Shots`'s
  homing does).
- **Spider:** the sentry's species with a walker body that may climb a wall cell inside the vessel's hull box, and the
  sentry's Notice/Strike moves (arc, pounce). Route, no graph search: on the quarry's deck it walks and jumps toward him; on
  another deck it walks to the hatch well (vessels have one, aft), opens the hatch and climbs; a wall in the way it
  climbs. Ties between equal targets go to the lower column.
- **Robot (`Escort`):** a walker 4 wide, 5 tall (5.5 with the antenna, which is drawn only), never jumps, bound to its
  deck's floor rows. Moves: `Lunge` (dash and claw) within `Strike`, and a `Shoot` move firing a new `gauss` shot within
  `Notice`, aimed along the line to the hero (up too). The gauss is an ordinary creature shot through `Shots.Fire` (the
  sim's, shots-are-the-sims), with a speed that crosses its range in one tick: it stops at the first solid cell and hits a
  hero as the catcher's shots do, no new command. "In line" is the existing sight check (`Sees`). With no hero in line or
  reach it walks to the nearest doorway out of its room (ties to the lower column) and on along its deck.
- **Doors:** the spider opens doors and hatches, the robot doors only, never the way-in door. At a shut doorway on its way
  a guard waits `Pry` ticks (`Content`), then opens it through `IGridOps.SetDoorway`; Creatures takes Grid's ops. If the
  doorway changes while it waits (a hero opens or shuts it), the wait restarts. Creatures do not collide with doors (0006):
  a door shut on a guard in the doorway leaves it there.
- **No hero aboard:** a guard stands where it is until one comes back.
- No new commands or events. `Posture` gains `Docked`, `Waking`, `Climbing`.
- **Saves:** guards live only in vessel worlds, which a load closes, so no saved world holds one. The new species, rules and
  `Lifetime` value still change the pinned contract (`CreatureContractTests`): updated there, with the `SaveFormat` bump and
  its `Migrations` step if that test demands one (G-1 decides and says which).

### Client (game/features/creatures)
- `DocksView`: capsules G1 and stands S1 from `Shipyard.Guards`, drawn with the back-wall objects (behind bodies, under
  the unseen-room fog), listed once when the vessel world opens. A dock shows asleep while its guard is `Docked`, opening
  while `Waking`, open after.
- The hazard lamp (capsule) and the robot's red head lamp are 2D lights; the gauss trace is a shot streak.
- The tick rig gains folded, unfolding and climbing poses. A new A1 rig, built as the Istukan's (parts on sprites).
- Art: `tools/anime-art` subjects ported from the mockups' draw code (capsule G1, stand S1, robot A1), in ink and
  watercolour per `tools/anime-art/README.md`.

### Budgets
The largest liner (its room count pinned by the G-1 test) gives about a dozen spiders and two robots. No path search: an
awake guard does a few cell probes a tick; a sleeping one checks up to 4 heroes against one room box.

## Tasks

Each is a try the user plays (`--vessel=freighter`, `--vessel=liner`), committed on `feat/derelict-enemies`; nothing is
pushed until all are played and the user says yes.

### G-1 — Docks aboard
`Shipyard.Guards`, `Aboard` rules, sleeping guards spawned, `Lifetime.Aboard`; `DocksView` with the exported G1 capsule and
S1 stand art, asleep only; the contract test updated. Tests: counts follow the formula and never exceed the candidates
(property over classes and seeds); docks inside their rooms and clear of furniture; none in the arrival room; a full
world's guards still spawn and the max per class is under `Creatures.Max`; `ModulesTests`' fuzz and two-world suites open
a vessel world with guards. Play: capsules and stands where they belong.

### G-2 — The spider wakes and hunts
Wake by entering or by a hit; unfold and drop; climbs solid walls inside the hull only; the sentry's arc and pounce; doors
and hatches after `Pry`; route by deck; keeps hunting; the sentry's loot. Client: tick poses, capsule iris and hazard
lamp. Tests: seen through a door or in the doorway → asleep, one step in → awake; a hit wakes it; killed asleep → dock
empty; never on a back wall, ceiling or the hull's outside (invariant); opens a door and a hatch, never the way-in door;
a door changed mid-pry restarts the wait; follows a hero to another deck; two worlds kept alike aboard a freighter while it
wakes, pries, is hit and hunts, plus a join mid-hunt, hash the same. Play: a freighter's cabins.

### G-3 — The robot
`Escort` species, stand wake and release, deck-bound walk, doors only, gauss far and up, dash and claw near, leaving
through a door when out of line, its loot. Client: A1 rig, stand animation, red lamp, gauss streak. Tests: one per robot
rule; never takes a hatch; stays on its deck's rows (invariant); two worlds alike aboard a liner through a fight. Play: a
liner's hall.

### G-4 — Ship
Full suite, ADR (`derelict-guards`) with the picks and their pages, Creatures and Places READMEs, map, glossary
(паук / охранная капсула, робот-охранник / стенд), the mockups and the design deleted with this plan, PR.

## Deliberately not handled

- Guards on wrecks, stations or the player's ship; enemy vessels.
- Ceilings, back walls and the outside hull for the spider; ladders and hatches for the robot.
- Stealth: a guard knows where every awake hero aboard is.
- Guards waking each other, alarms, guards shutting doors, guards following a hero into space.
- Re-docking: an awake guard never sleeps again.
- The robot's sounds: silent until its sound is picked on a page; the spider keeps the tick's.
- Toughness and every distance (`Strike`, `Notice`, `Pry`, `Wake`) are content, set in G-2/G-3 and tuned in play: the
  spider as the sentry, the robot about twice.

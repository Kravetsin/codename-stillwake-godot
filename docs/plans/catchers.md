# Design: Catchers — hunters on the hero's tracker

Status: designed (design-author, cut by design-reviewer, revised after /review-plan), slice 1 not built.

**Decided with the user** (2026-11-09):
- The hero still wears its former owners' tracker on the shin (the player's colour bracelet). The owners hire catchers, a
  third party neutral to the two warring factions, to bring it down.
- A hidden "wanted" meter per hero fills only while the hero is active: an idle hero's never fills, anywhere. On a planet
  nothing is safe, but under rock it fills slower (a weak signal). In space it fills away from the ship, not near it. Even the
  busiest hero meets a hunt no more often than once in 10 minutes, and never knows exactly when.
- A full meter starts a hunt for that hero: its own catcher, one per hunted hero. The hunt is timed (about 90 s): the
  catcher knows where its hero is and comes by any way, cutting soil and rock. Out of time, it leaves. The hero downed: the
  usual death (0061). Any end resets the meter.
- One species with a jetpack for space and planet, looking almost alike, but with weapons and habits varied per hunt, so no
  single strategy beats it.
- It hunts its own hero. Another hero striking it gets a short answer (a stun, a quick shot), then it turns back.
- Proposed by Claude, not objected to: the ship and anything built are not broken in slice 1 (they wait for the agreed
  unified damage); the bracelet hints without a number, blinking from two thirds full; a short warning as it comes.

## Problem
Every hostile waits for the hero to come near: walking round them is always safe, and a hero far from home never feels hunted.

## Player experience
1. On a planet the hero digs, runs, fights. Minutes in, the bracelet on its shin blinks now and then.
2. A beep, and a mark at the screen's edge; ten seconds later a hunter on a jetpack drops from the sky and comes straight
   at the hero, cutting through rock to one underground, heard digging closer.
3. It crouches and flies fists first; the hero sidesteps, strikes it while it recovers. A friend shoots it: it answers with an
   arc at the friend, then turns back.
4. It dies (scrap and a circuit), or its 90 s pass and it flies up and away, or the hero falls. The bracelet goes quiet.

Goes wrong: the hero walls itself in with walls it placed. The catcher holds outside the wall it may not cut, its time runs
out, it leaves; the meter resets. Only placed walls hold: a room dug into rock with a built door is cut into through the rock
beside the door. Building a shelter is a real answer, and it costs materials and the trip home to fetch them.

## Out of scope (deliberate omissions)
- **Breaking the ship or anything a player built**, machines, bedrock, lava, the fog: waits for unified damage.
- **Space** (slice 2): the meter does not fill aboard, and no catcher comes off a planet.
- **Nails or any projectile hitting a hero**: a hero's own client judges hits on it (0034); the catcher fights with the
  sentry's pounce and arc, whose hits already reach the sim (`HitBy`, `Bolted`).
- **Paths**: it steers straight at its hero and cuts or slides along what blocks it.
- **A number for the meter**, decay while idle, contracts, a bounty board, lore text.
- **`Stir` from every module**: a hero hewing in one spot for a minute reads as idle; its meter fills a little slower.
- **A `SpawnRule<P>` base** (creatures-are-composed): it would have one user; the bounty spawner is plain code until the raid.
- **Pockets a cut opens** (air, water, lava behind rock): a cut is a hit, so they open as the hero's own digging opens them.

## Scale
A breath is `Crew.BreathEvery` (20 ticks, 1 s); a client reports a moving hero's cell every `Crew.ReportEvery` (20 ticks).
- One catcher per hunted hero; on four heroes, four.
- A hunt per hero at least `NeedLeast` / `Wanted.Open` breaths of activity apart: 10 minutes under the open sky, up to 30
  under rock.
- No new command: activity rides the hero's `Enter` reports and `Exert`.

## Approach (slice 1: the planet)
**The meter, on Crew's `Hero`** (not in Creatures: it travels with the hero between surfaces, and Crew already charges
place costs each breath). Saved fields with "none yet" defaults, omitted while default: no format step.
- `Stirred`, the last tick the hero acted: an `Enter` moving its cell, or running (`Exerting`). Thrust would count, but Crew
  stops it on a planet (0076), so in slice 1 it never does. Active while within `Wanted.ActiveFor` ticks (60: three reports).
- Each breath an awake, active hero on a planet adds `Wanted.Open` (2) under the open sky, `Wanted.Under` (1) with a back
  wall behind its reported cell: the night chill's shelter test, our proxy for "under rock" (caves have grown back walls).
  `Wanted` is clamped to `Need`. Aboard the ship: nothing in slice 1.
- `Need`: 0 means "not drawn". A reset sets `Wanted` and `Need` to 0; every draw happens in the breath, in hero id order,
  for each hero at `Need` 0, from the world's Rng of the surface the hero is on: `NeedLeast..NeedMost` inclusive (1200..1800).
- Hunted: `Need > 0 && Wanted >= Need`, derived, never saved apart.
- Reset: passing out, departing the surface (so boarding the ship or launching home ends a hunt), `Leave`, or
  `ICrewOps.CallOff(peer)` from Creatures. Each sets the two fields only; the draw waits for the breath.
- `Signal(peer)`: `Calm` (also at `Need` 0), `Blinking` (`Wanted * 3 >= Need * 2`), `Hunted`; what the bracelet shows.

**The catcher, in Creatures:**
- A `Flyer` shape (a jetpack needs no gravity; the embermoth and driftbell of cave-hostiles fly next): moves on the longer
  axis at `Speed`, horizontal first on a tie; solid cells stop it.
- Species `catcher`, faction `catchers`: no relations either way, it ignores creatures and they ignore it. `Target` its
  hero. Kept while its target is on this surface and awake and its time lasts, keep boxes never removing it: a flag on its
  lifetime. Counts toward `Creatures.Max`.
- **The bounty spawner** (on the beat, after the habitats): per hero in id order, hunted, awake, on a planet, with no catcher
  here. It draws a side, then a column `RingFrom..RingTo` from the hero on that side, and places the catcher at the column's
  first open row from the top, out of every hero's `View` as the habitats' spawns are. No such place (off the grid, no open
  row, in a view): the other side the same beat; neither: it waits for the next beat, the meter staying full. At
  `Creatures.Max` it waits too. Then it draws the kit.
- **The `Hunt` drive:** `Warn` (200 ticks, 10 s) holding where it spawned, then `Ticks` (1800, 90 s) hunting. It steers at the
  target's reported cell, its kit's moves acting as a guard's do. Blocked, it wears the leading cells of its body in
  ascending order every `CutEvery` ticks through Building's new op `Wear(cell, damage)`; a blocked axis it cannot wear it
  slides along; both blocked, it holds.
- **`Wear`** strikes a cell as a hit does (a crack, then the wall gone, Rooms and the rest deriving as from digging) but gives
  nothing, and only where Grid's new `GrownWall(cell)` holds: the cell's layers equal the grown ones, as `GrownBack` tests
  the back wall, so a host and a joiner from the save agree. A wall rebuilt exactly as it grew counts as grown (accepted).
  Built cells, pieces, machines, bedrock, lava, the fog and the ship are refused.
- **The end:** out of time or dead, Creatures calls `CallOff`. Its hero passed out, departed or left has been reset by Crew;
  the catcher, its target gone, ends without a call. Either way it flies straight up, passing rock as a clinger does, and
  is removed once out of every hero's `View`.
- **Kits**, a content table drawn per hunt: slice 1 holds one, the Brawler: the sentry's `Pounce`, closing in; anyone in its
  way when it pounces is hit, as by a ram. Slice 2 adds the Marksman (the sentry's arc, holding about 30 cells in line) and
  the Gasser (the capback's lob and vent, holding 14), with the one new behaviour they need, holding a range.
- **The answer** (the catcher's own arc, whatever its kit): a hit from a hero not its target, if `AnswerEvery` (60 ticks)
  has passed and no move it cannot break off runs. Of several strikers in a tick, the first command in stamped order. A
  wind-up of `AnswerWindUp` (6 ticks), then an arc at the striker's reported cell, then the hunt resumes. The striker gone
  from the surface, or the hunt ended, during the wind-up: no arc.
- **Saved on the creature** (`Mem` and its fields, as every drive's): target, kit, the tick its warning ends, the tick its hunt
  ends, the next cut's tick, the last answer's tick. A new species adds its own pinned brain order (`CreatureContractTests`)
  and changes no other: no format step.
- Yields 2–4 scrap, a circuit. Hp 40, never knocked.
- **Dev:** `--catcher` sends `Release`; a released catcher targets its sender.

**Numbers** (in `sim/Content`, placeholders until played): `Speed` 250 thousandths of a cell a tick; `CutEvery` 10 ticks;
`Wear` damage 3 a strike (soil's `Strength` 4 in two strikes, stone's 12 in four: 2 s a cell); `RingFrom..RingTo` 70..90 columns.

**Client:** the catcher's look (a rig with a jet, the kit's part swapped in; picked by the user), the bracelet's light by
`Signal` on every hero, a beep and the edge pointer to its catcher for the hunted hero alone (sounds picked by the user), the
planet's dig sound when a crack appears or deepens in front of a catcher (read from Building's saved cracks, as it draws
them; no event list).

## Determinism and co-op
Every input is a command the sim has. Integers; steering truncates toward zero, horizontal first on a tie. Rng draws: `Need`
in the breath for heroes at 0, in id order; side, column, then kit at spawn, in hero id order on the beat. Cuts in ascending
cell order. `GrownWall` compares saved layers with the grown ones, never derived membership. A joiner gets the meter and the
catchers in the save. Heroes' cells and hits stay client-authoritative (0002, 0103).

## Tests
- Scenarios: the meter fills while moving and not idle, half under a back wall, never past `Need`; at `Need` 0 no hero is
  hunted and the next breath draws one; a full meter spawns one catcher out of view targeting that hero; no place on either
  side waits a beat; it cuts grown rock into a sealed cave and into a dug room beside its built door; it never wears a built
  wall, a machine or bedrock; each end resets (time, death with its yield, the hero passing out, launching, leaving); the
  answer at a second hero, once per `AnswerEvery`, none once the striker is gone; two hunted heroes, two catchers; an old
  save with `Need` 0; `Release` of a catcher; a world saved during `Warn` and again mid-cut, loaded and played on alike.
- `GrownWall` on a played world (a cell dug and refilled) agrees with the same world saved and loaded.
- Invariants: at most one catcher per hero per surface; `Wanted` never over `Need`, never growing without activity;
  `Wear` never changes an edited cell, a piece or a machine; a catcher never inside a solid cell.
- Creatures' `Fuzz` draws a catcher `Release`, so the generic suites reach cutting and answering.
- Budget: four catchers cutting stay inside the 2 ms tick on the reference planet.

## Slices
1. The planet, the Brawler: the meter, the bracelet, the flyer, the hunt, `Wear`, the answer, the look and sounds picked.
2. The Marksman and Gasser kits with range-holding; space (the meter away from the ship, a catcher holding at the ship's edge).
3. Later: nail kits (creature shots hitting heroes), wearing built walls with unified damage, `Stir` from other modules.

## Open (the body follows each recommendation until the user says otherwise)
- Slice 1 with the Brawler alone (recommended: the first playable soonest, slice 2 right after), or all three kits.
- Launching home ends the hunt and resets the meter (recommended), or the catcher follows into space.
- The beep and edge mark for the hunted hero only, the bracelet blinking for all (recommended), or the beep for everyone.

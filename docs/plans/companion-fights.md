# Companion fights: design and the plan of the first try

Written by `design-author`, cut by `design-reviewer`, revised by `/review-plan` (completeness, clarity, determinism) on
2026-11-22. Paths are from the repo root. Every number is a placeholder for `sim/Content`, to be tuned in play.
**Confirmed** marks the user's word; the rest is proposed.

Words used below: a hero is **out** when it is awake, on the grid and `RoomAt(cell) == Space` (never `Rooms.Afloat`,
which takes a room with no back wall too). A **blow** is a player's hit; a breaker's attack is its **pounce**. Distances
are cells, speeds thousandths of a cell a tick, 20 ticks a second.

## Problem

There is nothing to mow down: sentries are rare single duels, and the drone, the stronger of the pair, takes no part in
a fight.

## What is confirmed

- **Confirmed:** simple dummies in groups that are fun to mow down, not enemies worked out like the catcher.
- **Confirmed:** they are breakers («разборщики»), the dead fleet's service machines still on their last order: take
  apart whatever drifts, the hero included.
- **Confirmed:** the left button is the weapon, the right the companion's ability with what is in hand. The first is
  the axe thrown and brought back by the drone, the same damage.
- **Confirmed, later:** a palm shot (threads round the arm, the core a charge generator) that is fun, not a bare wait;
  later still the drone strengthens weapons and is one.
- **Confirmed, later:** the shot is learned from a sentry. Its first arc hits and the drone shows a question mark; at a
  later arc the drone catches it, draws it into the core and sends it back, killing the sentry with its wreck.
- **Proposed, tried here:** where breakers live, and every number.

## Player experience of the first try

1. Out by jetpack past the stream, the player sees a wreck with small machines folded on it.
2. They come near or chop it. Four to eight breakers spill out and ring the hero.
3. A swing catches two or three. Each flies off along the blow, comes back, and bursts at the second hit.
4. A right click: the axe leaves the hand through the line of them, and the drone hauls it home on a thread.
5. Retreating ends it: a jetpack outruns a pack, and in a room the hero is let be, the pack idling till it lingers out.

## The mechanics

| Mechanic, owner | Users now | Users next |
|---|---|---|
| Pack: nests, release, the `Swarm` drive on the flyer body (`sim/Creatures`) | breakers on far wrecks | vessels, raids |
| Knock along the blow for a flyer afloat (`Hurt`, the one hit path) | breakers, by any weapon or shot | every flying dummy |
| Companion abilities, one table by the item in hand (`sim/Content`) | the axe's throw | spear and sledgehammer throws, the palm shot, strengthened weapons |
| One lookup along a segment (`ToolRays`, lifted out of `Fan`) | the swing, both legs of a throw | every later throw |

### Nests

- Only on the ship's surface, on the far field's `hullSection`, `cargoContainer`, `machineBlock` and `shieldSlab`
  (`Debris.Far`). A piece of the stream, of a delivery or of a stretch never has one.
- Drawn as a guard is (`GuardNewPieces`, the same watermark), so an old world gets them on new pieces only. Per piece in
  id order: the guard's draws, then for a piece not yet struck the nest's chance (100‰) and, if it has one, its size
  (4 to 8), whatever `Max` says.
- `Nests`: a list ascending by piece id of `{ Piece, Size }`, saved null when empty as `Patches` is. Read by the client
  as a read-only list.
- A nest wakes when its piece has been struck (any later `Struck` stays a request, whoever struck) or a hero out is
  within 32 cells of the piece's middle. Its hero: the striker if out, else the hero out nearest the piece (ties by
  id); with none it waits. It waits too while the surface would pass `Max` (40 creatures of every kind) or that hero's
  keep box would hold more than 8 breakers.
- Nests are walked in piece order after the creatures behave, before `Posted` and `Bounty`. Breakers appear at a fixed
  ring of eight places round the piece where `InSpace` holds. None free: it waits. Some free: that many come and the
  nest is spent. A piece gone takes its sleeping nest with it, with nothing given.

### A breaker

- The existing `Flyer`, 2×2 cells, no path search. `Swarm` steers at the nearest hero out, to one of eight fixed places
  4 cells round it (id % 8), easing to its speed; solids stop it as they stop the catcher. With no hero out it eases to
  rest. It goes as every wild creature does (keep box, linger, hard box).
- Within 9 cells, in line, it may pounce: the existing `Pounce` with two new flags, both false by default, so the
  sentry, the warden and the catcher stay as they are. `Cuttable`: a blow cuts the tell and the dash. `AtTarget`: it
  aims at its `Target` alone and lets be a target out of run or no longer out. Contact is today's `HitBy` (`Rams`); it
  recoils the way it came.
- At one hero one breaker begins a pounce each 30 ticks: beginning sets `Target` and keeps `beganAt` (clock + 1) and
  `beganFor` (peer + 1) in `Mem`. Only a breaker's last pounce counts; killing it frees the next sooner: accepted.
- A flyer afloat with a knock is knocked along the blow: from the middle of the striker's reported cell to its middle
  (a throw's too, so both legs push it away from the hero), a shot along its speed. The longer axis gets
  `Knock * Pace / KnockPace`, the shorter its share of that, toward zero. No line (no hero, the same point): as today,
  across only. `Hit.Away` stays for every walker.
- Knocked is saved in `Step`: `Swarm` only sheds speed (`Slow`) till it is at a quarter of its own, then steers again.
  A blow and a pounce's end clear `Target`.
- It dies by the existing `Kill`, spraying its yield.

### The throw

- Content: `Abilities`, a row by item, the axe's `Throw(Range, Out, Home)`. A row exists only with a user.
- No sim state and no new command: the axe never leaves the inventory. A right click starts one with a drone paired and
  not scripted, the axe wielded, the building plan away, the hands able to act and no throw out. Otherwise the click
  does nothing, silently. Food, the canister, the driver, guns and the plan keep theirs.
- The client flies it along the aim to its range or the first cell that stops a ray, then home to the hero as it is
  that frame. Out it sends today's `Strike`, `HewPieces` and `HewCells`; home `Strike` and `HewPieces` only, nothing
  past a solid cell. All through the one lookup, each target once a throw, each command validated as it applies: a
  wreck's hit refused to a thrower who went aboard is accepted.
- No cooldown: the next leaves once this one is home, and no sooner than the axe's `Weapons` pace after the last.
- While it is out the hand is drawn empty and swings nothing. It ends at once, hits and drawing, when the axe is no
  longer wielded (a switch, a drop, death), the building plan comes out, the hero passes out, is held by a scene, sits,
  flies or changes surface. A wheel or a window opened stops only new throws.
- The drone flies to the axe from where it is and stays with it on the tow's thread, the lamp too; the axe never
  waits. Its tow, scan and moods wait.
- Friends: `Handwork.Throw`, appended last, outranks other work and gestures while out: the point and the throw's
  number. They stop drawing when it is absent. Only the thrower sends hits.

## Co-op and determinism

- Nests, wakes, the swarm, the gate and the knock are sim: id order, integers, divisions toward zero. The world's Rng
  for the nests' draws and a kill's yield. Ties go to the first hero in id order, as `Nearest` does.
- The throw's path and a breaker's contact are the client's word, as swings and rams are (0002). A `HitBy` after a cut
  is refused `notAttacking`, the local shove without loss accepted. Friends show no ram for a pounce a blow cut (the
  creature's `Damage` rose).
- `Nests` starts empty and the species is appended to `All`, so no format bump. The added draws move what later draws
  of a seed give: pinned worlds change once. A world the try saved with breakers in it does not open on main's build:
  the try is played in a new world.

## Numbers

| Placeholder | Value |
|---|---|
| Nest | 100‰ of the four kinds; 4–8; wakes within 32 cells |
| Breaker | 4 HP: axe 2 hits, spear 2, sledgehammer 1, nail 1 |
| Flight | 700 (14 cells/s), gaining 150 a tick on each axis |
| Pounce | within 9 cells; tell 12 ticks, dash 6 ticks at 1500, 1 unit; one begun a hero each 30 ticks |
| Knock | 1200, shed a quarter a tick down to 175: about 4 cells |
| Yield | one scrap chunk with chance 1/2, a breaker |
| Limits | `Max` 40 a surface; 8 breakers in a hero's keep box |
| Throw | 18 cells; 45 cells/s out, 60 home: about 0.7 s |

Budget: at most 40 creatures and some 60 sleeping nests among 640 pieces. A tick stays under 2 ms with 40 breakers and
four heroes.

## Deliberate omissions

- No tiers by distance, no cap of breakers apart from `Max`, no give-up drive, no path search, no gnawing that harms a
  wreck or takes loot. Breakers ignore each other, wrecks as obstacles, and bursts.
- A pack may wait outside an airlock, or where a hero comes back to life, up to the linger. Revisit if play calls it a
  nuisance.
- A pack may wake unseen, by a wreck struck far off. A wreck broken by the blow that would have woken its nest takes
  the nest with it.
- A hit afloat without a jetpack masks the keys and shoves nobody (`Hero.Step`), as today.
- No throw shown to one joining mid-flight, no axe lost or stuck, no aim assist, no prediction, no drone health.
- No shake, rings or blade particles; the 50 ms arm stop stays.

## Plan: the try

Branch `feat/companion-fights`, branched from `fix/no-see-through` as the throw needs the right button freed. A commit
a step, filtered tests, no push. Steps 1–5 are one try, played in one window.

### 1. The breaker (sim)

- `sim/Content/Creatures.cs`: `Breaker` (look `breaker`, `Flyer(2, 2)`, `Life`, faction `Machines`, wild, brain
  `[Swarm]`, moves `[Pounce]`), appended to `All`; the `Swarm` record; `Pounce`'s two flags.
- `sim/Creatures/Swarm.cs` (new): the drive. `Pounce.cs`: the flags. `Creatures.cs`, `Shots.cs`: the line of a hit and
  the knock; a test holds the catcher's knock unchanged.
- Tests `sim/Tests/BreakerTests.cs`, breakers put out by state where things float: steers and rings; stops at a wall;
  cut by a blow while a sentry's is not; knocked up, down, across and from afar; the gate with two heroes and six
  breakers; aims at no other hero; dies to two axe hits; idles with every hero in a room, one with no back wall too, or
  passed out. `CreatureContractTests`: its pinned row, and let out by state, a saved knock counting as an answer.

### 2. Nests (sim)

- `sim/Content/Creatures.cs`: `Nest` (species, zone, kinds, per mille, least, most, wake, per keep); `Surfaces.Ship`
  names it as it names `Guards`.
- `sim/Creatures/State.cs`: `Nests`. `Spawn.cs`: the draws. `sim/Creatures/Nests.cs` (new): wake, release, drop with
  the piece, the query. `Live.cs`: the early return lets a surface with nests run.
- Tests `sim/Tests/NestTests.cs`: only the four kinds and only the far field; wakes by nearing and by a strike, not
  for one not out; never aboard; waits at both limits and with no place; save and load before and after waking.
  A desync test: two worlds seeded with nests and breakers, fed random commands, one loaded every tick, the same hash,
  a wake, a cut and a kill seen. FsCheck: never past `Max`, never in solid, ids only grow. A budget test, 40 breakers.

### 3. The breaker seen (client)

- `game/Content/logic/CreatureLooks.cs`: breakers join the `Ticks` group of looks at a size of their own, 0.45 (the
  sentry is 0.65); `TickBodies` takes the size by species. `HitLooks` and `Sound.Ticks`: the sentry's rows, one shots
  only. A sleeping nest: three folded ones on its piece, drawn from the `Nests` query in view only. No friend's ram
  for a cut pounce.
- Names in `Creatures.En.cs` and `Creatures.Ru.cs`. These looks and sounds are stand-ins till the pick pages.

### 4. The throw (sim content, client)

- `sim/Content/Abilities.cs` (new), the table. `game/features/crew/logic/ToolRays.cs`: the segment lookup lifted out of
  `Fan`, the fan unchanged by its tests. `game/features/crew/logic/AxeThrow.cs` (new, Godot-free): legs, once a
  target, the cancels.
- `HeroView.Hands.cs`: the right click and the hits sent; `LightDrone.cs`: the drone with the axe, the thread and the
  axe drawn; the arm by today's overhand.
- Tests `game.tests/AxeThrowTests.cs`: range and stop at a wall; once over two legs; nothing past a wall and no cells
  coming home; every cancel; none mid-flight; never sooner than the pace.

### 5. Friends, then the user plays

- `Remote.cs`: `Handwork.Throw`; `HeroView.Others.cs` draws it; a test of the packing.
- `dotnet build game`, headless import, the log checked, then the plain game launched from this worktree, the user
  told to start a new world.

### What the play answers

1. Is it fun to hit: the knock, the two-hit kill, the pack's rhythm?
2. Is the throw a move one wants, or a worse swing?
3. Is a pack met within a couple of minutes, and is it too many?
4. In survival: is a pack a fight or a tax?

## After the try, not planned yet

- **Ship of the try:** pick pages (breaker with nest, tell and death; its sounds; the throw and catch), then the ADR,
  READMEs, map, glossary and the full suite.
- **The scene and the palm shot,** one slice, as one teaches the other. The question is a deed counted at `Bolted`.
  Once earned, the next arc aimed at that player is caught on the tick the aim is taken, in the sim: the form granted
  by a deed, the hero spared, the arc never struck, and at the act's end the sentry and its wreck destroyed. A perched
  sentry that would pounce at a hero with the question shoots instead, or a curious player who flies closer would
  never get the second arc.
- **Open then:** how the shot shares the right button with the throw (recommended: tap for the item's ability, hold
  to charge the palm); its charge (recommended: by hits, and slowly by itself so one shot always comes).
- **Later, named only:** breakers aboard vessels, other throws, strengthened weapons.

# Companion fights: design and the plan of the first try

Written by `design-author`, cut by `design-reviewer` on 2026-11-22. Paths are from the repo root. Every number is a
placeholder for `sim/Content`, to be tuned in play. **Confirmed** marks the user's word; the rest is proposed.

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
5. With little air they leave: a jetpack outruns a pack, and nobody is attacked in a room.

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
- Each such new piece draws once, as a guarded piece does (`GuardNewPieces`, the same watermark): 100‰ have a nest,
  and a second draw gives its size, 4 to 8. The size is saved in `Nests`, by piece id.
- A nest is no creature until it wakes: when its piece is struck after the nest came (`Piece.Struck`), or an awake hero
  afloat is within 32 cells. It wakes whole or not at all. It waits while the surface would pass `Max` (40) or that
  hero's keep box would hold more than 8 breakers.
- Breakers appear round the piece, in id order, at the free places of a fixed ring; one with no free place is not
  spawned. A piece gone takes its sleeping nest with it, with nothing given.

### A breaker

- The existing `Flyer`, 2×2 cells, no path search. `Swarm` steers at the nearest awake hero afloat, to a place on a
  ring of 4 cells picked by its id, easing to its speed; solids stop it as they stop the catcher. With no hero afloat
  it eases to rest. It goes as every wild creature does (keep box, linger, hard box).
- Its blow is the existing `Pounce` with a new `Cuttable` flag, default false, so the sentry and the warden stay as
  they are: a blow cuts the breaker's tell and its dash. Contact is today's `HitBy` (`Rams`).
- At one hero one breaker begins a blow each 1.5 s: before beginning, it reads when the living breakers last began one
  at that hero (kept in their `Mem`). Killing the one that began frees the next sooner: accepted.
- A flyer afloat with a knock is knocked along the line from the striker's hero to its middle, both axes, on the longer
  axis the species' knock, and sheds it as `Slow` does. `Hit` keeps its one `Away` for every walker.
- It dies by the existing `Kill`, spraying its yield.

### The throw

- Content: `Abilities`, a row by item, the axe's `Throw(Range, Out, Home)`. A row exists only with a user.
- No sim state and no new command: the axe never leaves the inventory. Right click, only with a drone paired, the
  plan away, the hand free and the item having a row. Food, the canister, the driver, guns and the plan keep theirs.
- The client flies it along the aim to its range or the first cell that stops a ray, then home to the hero as it is
  that frame. Both legs send today's `Strike`, `HewPieces` and `HewCells` through the one lookup, each target once a
  throw. The way home strikes nothing past a solid cell and no cells.
- No cooldown: the next leaves once this one is home, and no sooner than the axe's pace.
- While it is out the hands swing nothing. Anything that takes the axe from the hand (a switch, a drop, death, a seat,
  another surface) ends the hits at once and the drawing with them.
- The drone flies with the axe on the tow's thread and the lamp with it; its tow, scan and moods wait.
- Friends: one new `Handwork.Throw` in `Busy`, the end point and the throw's number. They draw both legs; only the
  thrower sends hits.

## Co-op and determinism

- Nests, wakes, the swarm, the gate and the knock are sim: id order, integers, divisions toward zero, the world's Rng
  for the two draws alone. Ties go to the first hero in id order, as `Nearest` does.
- The throw's path and a breaker's contact are the client's word, as swings and rams are (0002).
- `Nests` starts empty and the species is appended to `All`, so no format bump. The added draws move what later draws
  of a seed give: pinned worlds change once.

## Numbers

| Placeholder | Value |
|---|---|
| Nest | 100‰ of the four kinds; 4–8; wakes within 32 cells |
| Breaker | 4 HP: axe 2 hits, spear 2, sledgehammer 1, nail 1 |
| Flight | 700 a tick (14 cells/s), gaining 150 a tick |
| Blow | tell 12 ticks, dash 6 ticks at 1500, 1 unit; one begun a hero each 30 ticks |
| Knock | 1200 a tick, shed a quarter a tick: about 5 cells |
| Yield | scrap chunk, one in two |
| Limits | `Max` 40 a surface; 8 breakers in a hero's keep box |
| Throw | 18 cells; 45 cells/s out, 60 home: about 0.7 s |

A pack of six is 24 HP: about 7 s as played, costing a typical 6 air beyond breathing (2 blows, 4 thrust), of the basic
45. In creative blows cost nothing, so air is judged in survival.

Budget: at most 40 creatures and some 60 sleeping nests among 640 pieces. A tick stays under 2 ms with 40 breakers and
four heroes.

## Deliberate omissions

- No tiers by distance, no cap of breakers apart from `Max`, no give-up drive, no path search, no gnawing that harms a
  wreck or takes loot. Breakers ignore each other, wrecks as obstacles, and bursts.
- A pack may wait outside an airlock up to the linger. Revisit if play calls it a nuisance.
- A wreck broken in the blow that would have woken its nest takes the nest with it.
- A hit afloat without a jetpack masks the keys and shoves nobody (`Hero.Step`), as today.
- No throw shown to one joining mid-flight, no axe lost or stuck, no aim assist, no prediction, no drone health.
- No shake, rings or blade particles; the 50 ms arm stop stays.

## Plan: the try

Branch `feat/companion-fights`, on `fix/no-see-through`. A commit a step, filtered tests, no push. Steps 1–5 are one
try, played in one window.

### 1. The breaker (sim)

- `sim/Content/Creatures.cs`: `Breaker` (look `breaker`, `Flyer(2, 2)`, `Life`, faction `Machines`, wild, brain
  `[Swarm]`, moves `[Pounce]`), appended to `All`; the `Swarm` record; `Pounce.Cuttable`, default false.
- `sim/Creatures/Swarm.cs` (new): the drive and its rule as above. `Pounce.cs`: `Interruptible` reads the flag.
  `Creatures.cs` `Hurt`: the knock along the blow for a flyer where things float with `Knock > 0`; the catcher's
  knock is checked unchanged.
- Tests `sim/Tests/BreakerTests.cs`: steers and rings; stops at a wall; cut by a blow while a sentry's is not; knocked
  up, down and across; the gate with two heroes and six breakers; dies to two axe hits and gives its yield; idles with
  every hero in a room or passed out.

### 2. Nests (sim)

- `sim/Content/Creatures.cs`: `Nest` (species, kinds, per mille, least, most, wake, per keep);
  `Surfaces.Ship` names it as it names `Guards`.
- `sim/Creatures/State.cs`: `Nests`, saved only when there are any. `Spawn.cs`: the draws with the guards' watermark,
  telling a far-field piece by its zone. `sim/Creatures/Nests.cs` (new): wake, release, drop with the piece, the query
  `Nests` for the client. `Live.cs`: the early return lets a surface with nests run.
- Tests `sim/Tests/NestTests.cs`: only the four kinds and only the far field; wakes by nearing and by a blow, not for
  one in a room or passed out; waits at both limits; save and load before and after waking; two worlds alike. FsCheck:
  never past `Max`, never in solid, ids only grow. A budget test with 40 breakers.

### 3. The breaker seen (client)

- `game/Content/logic/CreatureLooks.cs`: breakers join `Ticks` at a size of their own, 0.45 (the sentry is 0.65), with
  a tint of their own; `TickBodies` takes the size by species. `HitLooks` and `Sound.Ticks`: the sentry's rows, one
  shots only. A sleeping nest: three folded ones on its piece, drawn from the `Nests` query in view only.
- Names in `Creatures.En.cs` and `Creatures.Ru.cs`. These looks and sounds are stand-ins till the pick pages.

### 4. The throw (sim content, client)

- `sim/Content/Abilities.cs` (new), the table. `game/features/crew/logic/ToolRays.cs`: the segment lookup lifted out of
  `Fan`, the fan unchanged by its tests. `game/features/crew/logic/AxeThrow.cs` (new, Godot-free): legs, once a
  target, the cancels.
- `HeroView.Hands.cs`: the right click and the hits sent; `LightDrone.cs`: the drone with the axe, the thread and the
  axe drawn; the arm by today's overhand.
- Tests `game.tests/AxeThrowTests.cs`: range and stop at a wall; once over two legs; nothing past a wall coming home;
  every cancel; no throw unpaired, with the plan out or mid-flight; never sooner than the pace.

### 5. Friends, then the user plays

- `Remote.cs`: `Handwork.Throw`; `HeroView.Others.cs` draws it; a test of the packing.
- `dotnet build game`, headless import, the log checked, then the plain game launched from this worktree.

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

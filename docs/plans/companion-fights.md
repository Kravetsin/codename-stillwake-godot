# Design: companion fights

Paths are from the repo root. Every number is a placeholder for `sim/Content`. "Not checked" marks what was not read or run.

## Problem

There is nothing to mow down: sentries are rare single duels (`sim/Content/Creatures.cs`), and the drone, the stronger of the pair, takes no part in a fight.

## Player experience

1. Out by jetpack, the player sees a wreck with red eyes folded on it and something gnawing.
2. They come close or chop it. The eyes open for half a second, then six small breakers spill out and ring the hero.
3. A swing catches two or three. Each flies off along the blow, comes back, and bursts into its parts at the second hit.
4. Right click: the axe leaves the hand and goes through the line of them, and the drone hauls it home on a thread.
5. The first sentry arc hurts as today, and the drone forms a question mark. At a later arc it catches the discharge, draws it into its core and sends it back through the sentry and its wreck.
6. The palm shot is learned: threads wind round the arm and the charge leaves the hand. Fighting refills it.

Going wrong: with little air the player stays in a pack, each blow takes a unit and the suit reddens. A jetpack outruns them and a room ends the chase.

## Lore

Confirmed: breakers («разборщики») are the dead fleet's service machines, still on their last order, which is to take apart whatever drifts, the hero included. Sentries guard that fleet's derelicts. They drop scrap. The spire wardens are the arc makers' and unrelated.

## Where and how often

Proposal: nests on the far field's small wrecks (`hullSection`, `cargoContainer`, `machineBlock`, `shieldSlab`). Derelicts keep their sentries.

| Cells from the hull (`Debris.Far` tiers) | Wrecks with a nest | Pack | Nests spawned a minute of flight (not measured) |
|---|---|---|---|
| the stream, a tether's reach | none | none | none |
| far, under 256 | 20‰ | 4 | about 1 |
| 256–511 | 30‰ | 6 | 1–3 |
| 512 and on | 40‰ | 8 | 2–4 |

- A piece draws once as it spawns out of view, as sentries do (`GuardNewPieces`, `sim/Creatures/Spawn.cs`), so a nest is seen before it is hit.
- It wakes whole when its wreck is struck (`Piece.Struck`) or an awake hero afloat is within 16 cells. With no room under the limits it stays asleep.
- It does not wake on breaking: a piece gone does not say broken or drifted off. A wreck broken in one blow takes its nest with it.
- Aboard generated vessels is a later user of the same release. Their developer decides where.

## The modules shared

| Mechanic, owner | Users now | Users next |
|---|---|---|
| Pack: nests, release, drive `Swarm` on the flyer body (`sim/Creatures`) | breakers on wrecks | vessels, raids from wrecks (`docs/concept.md`, Open) |
| Two-axis knock for a body afloat (the one hit path, `Hurt`) | breakers, by any weapon or shot | every flying dummy |
| Companion abilities, one table by the item in hand (`sim/Content`) | axe: throw | spear and sledgehammer throws, the palm shot, strengthened weapons |
| One lookup along a segment (`ToolRays`, lifted out of `Fan`) | the swing, both legs of a throw | every later throw |
| A lesson by a caught projectile (`Content.Lessons`, a new source) | the sentry's bolt teaches the palm shot | none named, so one row |

No new module: Creatures owns what moves and hurts, Crew a hero's charge, Knowledge what a player learned.

## State, commands, what is client

**Breakers.** A species record with the existing `Flyer`, a `Swarm` drive and the existing `Pounce`, which gains "a blow cuts it". There is no path search: a breaker steers at the nearest awake hero afloat, to a place on a ring of 4 cells picked by its id, and slides along solids. With none within 48 cells for 10 s it flies off and goes once unseen, so packs do not wait at airlocks holding the limit. Saved: `Nests` (piece, size).

**The throw.** No sim state and no new command. The axe never leaves the inventory.
- The client flies it from today's overhand release along the aim, to its full range or the first thing that stops a ray, then home to the hero, faster than any hero moves.
- What it crosses gets today's `Strike`, `HewPieces` and `HewCells`, each target once a throw over both legs, at the axe's damage and knock. Out, it passes creatures, wrecks and stations as a swing's rays do. Home, it strikes nothing behind a wall from the hero.
- A wreck loses 2, as from a swing. The cell that stops it takes the axe's 40, a tenth on one's own ship, by `TargetAt` (one-damage-rule). Wrecks are hit only by a hero outside (`debris.notOutside`).
- `Strike` already trusts reach and pace (`sim/Creatures/Creatures.cs`), and `HewPieces` trusts reach (`sim/Debris/README.md`).
- No cooldown, with one floor: the next release is no sooner than the axe's pace, or an adjacent wall would make throwing the faster attack.
- The hands are empty meanwhile: no swing, while moving and E work. A hotbar switch, drop, trash, death, seating or surface change ends the hits at once. The drone comes back, and the axe is drawn home only if still owned and on this surface.
- The lamp flies with the drone. The scanner, tow and moods wait. Unpaired, the right button does nothing.

**The question and the catch.** The first `Bolted` by a sentry's bolt on a hero with a drone counts a deed in Knowledge (saved, the player's, kept through death). The blow is today's.
- The question mark exists (`game/shared/logic/DroneInk.cs:284`), but `DroneThoughts` shows it only for teaching machines, once each, two minutes apart. A sentry becomes a second thing it is curious at: at once after that hit, then once per sentry within 40 cells, until the form is known.
- On the tick a sentry takes its aim (`Shoot.cs`), the sim looks at whom it aimed at. If that hero has the question, it grants the form, spares the hero by Crew's `Meets` for `VisionTicks`, and marks the arc caught: it hurts nobody. No client's word, crossing or dodge is asked.
- 2.4 s later the sim kills the sentry and breaks its wreck by `Debris.Hit`, both credited to that player, with the usual loot, worth and deed. This is a scripted act, not the palm shot's damage.
- The client holds the hero and plays it in the world: threads out, the arc caught, the core drawing it in, the beam back. The lesson's 3.6 s runs under it, then the form vision shows the threads on the arm and the palm, and `Envision` renews the spare.

**The sentry's bands.** Unprovoked it shoots at 21–40 cells, pounces nearer and calms farther (`Guard.cs`). A hero staying in the band gets an arc every 3.65 s, which confirms the lead's figure. A hero coming closer would never get the second, so one rule changes: a perched sentry that would pounce at a hero with the question shoots instead, the drone's core drawing the discharge. Coming to study it, hovering and hitting it from afar (already answered by an arc) all lead to the scene. Fleeing loses nothing, as any sentry later will do.

**Palm shot.** Crew holds the charge. Creatures adds the damage the hero really took off creatures, and it creeps up by itself. It fires through `Fire` as a gun row with no item or ammo, adding refusals `creatures.notLearned` and `creatures.notCharged`. It starts full after the scene.

## Co-op and determinism

- Packs, wakes, the knock and the catch are sim: id order, integers, divisions toward zero. Ties go to the first hero in id order, as `Nearest` does.
- The spare begins on the lock tick on every peer. The 0.2 s of `Lock` need not cover a round trip: a guest sees the same beat later. A blow it sent in between costs nothing (`Crew.Blow` returns on `Meets`), though it may have flinched.
- Two heroes with questions: only the one aimed at learns. A friend's sentry serves as well. Watchers see the catch from that hero's drone, read from the sim. A friend on the line is not hurt.
- Today's replicated state cannot show a throw. `Busy` (`game/features/crew/logic/Remote.cs`) already carries a point and a count, so one new `Handwork` value suffices: the end fixed at release and the throw's number. Friends play out both legs to the hero as drawn, 150 ms behind, and may see a flinch before the axe arrives.
- A joiner gets nests, packs, the deed, charge and a catch under way in the snapshot, and no throw in flight. Added fields start at "none", so no format bump.
- A save and load during the catch finishes it and keeps the spare and the form. The vision is lost, as form-vision already accepts.
- Client-authoritative: the throw's path and a breaker's contact (`HitBy`), as swings and rams are among friends (0002).

## Numbers

| Placeholder | Value |
|---|---|
| Breaker | 2×2 cells, 4 HP: axe 2 hits, spear 2, sledgehammer 1, nail 1, drill 4 |
| Its flight | 14 cells/s, between a swim's 3.6 and a jetpack's 30 |
| Its blow | 1 unit, 0.6 s tell, 0.3 s dash, cut by a hit; at one hero one begins each 1.5 s, all breakers counted |
| Knock | about 5 cells along the blow, home again in under a second |
| Yield | scrap chunk, 1 in 2 |
| Limits | 24 breakers a surface, leaving 16 of `Max` 40 to sentries and catchers; 8 in a keep box for each hero in it |
| Despawn afloat | today's rule: keep box 160×96, 30 s linger, hard box 220×128, no drop |
| Throw | 18 cells, 45 cells/s out, 60 home, about 0.7 s: 2.9 damage/s on one target against a swing's 4 |
| Palm shot | 30 through every body; full at 48 charge, 1 a damage point and 1 a second: two packs, or 48 s alone |

**A pack of six:** 24 HP, 6 s of single-target swings, 3 s at two a swing, about 7 s as played.

| Air in those 7 s | Breath | Thrust | Blows | Total |
|---|---|---|---|---|
| typical | 7 | 4 | 2 | 13 |
| worst (thrusting throughout, every blow lands) | 7 | 14 | 5 | 26 |

Breath is paid anyway, so a typical fight adds 6: a seventh of the basic 45, under a third of a canister's 20. Bare lungs (20) live through one typical pack and should run from a second. Nothing has to give back at these numbers. `PerKeep` 6 governs planet herds only and stays.

**Scale and budget:** at most 40 creatures, four heroes, about 10 sleeping nests that cost one lookup each a tick. Forty lopers take 0.12 ms (`sim/Tests/BudgetTests.cs`). Within budgets, to be proven with 24 breakers among 640 pieces. Commands: up to 2 strike batches a second a hero, and reports every tick near a pack, as near sentries today.

## Slices

1. **Is it fun to hit?** Nests at a flat rate with packs of six on far wrecks, the drive, the two-axis knock, today's left-button axe, a death into parts. Played in the plain game: a jetpack and a minute upward. Creative shows the feel, survival the air, as blows cost nothing in creative.
2. **The throw,** with the table, the segment lookup and the friends' view. Also playable on wrecks alone.
3. **The place:** tiers and sizes, the picked looks and sounds. Ship.
4. **The scene and the palm shot** together, since one teaches the other.
5. Named, not designed here: other throws, the drone strengthening weapons, the drone as a weapon.

**Before picks:** the sentry's pictures at two thirds (scaling not checked), three folded as a nest, flung apart at death; its one-shot hit and death sounds; the axe's picture, the tow's picked thread, today's whoosh and one arm pose delta. The stop on contact stays 50 ms. No dev scene.
**Pick pages:** the breaker with nest, tell and death; its sounds; the throw and catch, with sounds; the catch scene, arm threads and shot, with sounds.

**Tests:** wake by nearing, by a blow, refused at a limit; give up and leave; knock on both axes; blow gate with two packs; throw hits once over two legs and stops on every cancel; question, close-range arc, catch on lock, two heroes, load mid-catch, both refusals. FsCheck: limits hold, bodies out of solid, no hero pays twice inside the gate, a form is caught once.
**Cost:** changed Creatures, Content, Debris (a tier query), and for slice 4 Crew, Knowledge, Lessons' content. Client: creatures, crew, research. About 700, 550, 250 and 900 lines by slice, art aside.

## Deliberate omissions

- A sentry killed before it ever fires teaches nothing. The user says it does not happen.
- A pounce earns no question. The lesson is the arc.
- The scene cannot pause the world or promise the first sentry ever met: co-op runs on, and old saves have met many. The next two arcs serve.
- A first arc that kills still earns the question, shown again at the next sentry.
- A friend killing the sentry or wreck in the catch's 2.4 s: the form stands and whatever is left is not blown up.
- Each player gets one derelict's loot free, four in a full crew: the price of the scene, once.
- In creative the question and catch read Knowledge, so they play. The shot is open at once, as every form is, and the vision may not play (not checked).
- Breakers do not really damage wrecks or take loot: gnawing is drawn. Competition would need an economy rule.
- They ignore bursts (a burst hurts no creature today), each other and wrecks as obstacles.
- A hit afloat without a jetpack masks the keys 0.4 s and shoves nobody (`Hero.Step`). Left until a play pass calls it a stun.
- No throw on a joiner's screen mid-flight, no axe lost or stuck, no aim assist, no drone health, no prediction.
- No shake, rings or blade particles. Cards state damage and range only.
- Not checked: the lamp's light following the drone away from the head, vessel interiors as a home for a flyer, the feel of any number.

## Open questions for the user

1. **Where are they met?** Recommended: nests on far wrecks, none in a tether's reach, so the first hour stays calm. Or a rare nest of three in the stream too. Or derelicts only, which is rarer and nearer today's "one sentry in all space".
2. **How does the palm shot share the right button with the throw?** Recommended: tap for the item's ability, hold to wind the threads and release to fire, wherever the right button is free. The tap then fires on release once the shot is learned. Or the shot with an empty hand only. Or a charged shot takes the next right click.
3. **May a sentry shoot at close range at a player with the question?** Recommended: yes, it answers the doubt, as curiosity then leads to the scene. Or keep the bands and let the scene wait for a player who happens to hover.
4. **If packs still feel like a tax after slice 1?** Recommended: change nothing before playing in survival. Then either fewer blows, or a canister now and then from a nest. Air for kills is not proposed: it turns packs into an air farm.

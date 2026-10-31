# Plan: spire guards

A spire's chamber is an empty room: a hero drops in and takes the shard unopposed. Nothing on the first planet offers a fight
worth choosing. Two machines of the arc makers now guard each shard, in a hall at the spire's bottom, and the hero chooses: kill
them for circuits, or take the shard at once and get only that.

Written by `design-author`, cut down by `design-reviewer`, its choices made by the user. Its claims about the code were read,
not run. Numbers and sizes are placeholders for a play pass.

## Words

A *guard* here is the Istukan, species `Warden`, never the sentry's drive `Guard` or `Kind.Guards`. Its drive is `Ward`. *Siblings* are
guards tied to the same relic. The *arena box* is a post's own (hall interior plus the hole); the *keep box* is `Creatures.Keep`, round each
hero, far bigger. A *woken* guard has left its sleep; an *awake* hero has not passed out. Times are in ticks (20 a second), speeds in
thousandths of a cell a tick, lengths in cells unless a row or column of a prefab is named. A suit holds 45 units.

## What the player meets

1. A hero drops through the shaft's hole into a wide hall: the cradle under the light, a dark Istukan in a niche at each end.
2. Eyes go teal, then yellow. One guard crouches red for 0.8 s and flies straight, stopping at the first wall. It lies stuck for
   a second while the other starts its crouch.
3. The hero dodges and shoots or chops whichever is stuck. Each kill sprays circuits and a little alloy scrap; then the hero takes
   the shard.
4. Or the hero runs to the cradle and fells it in four swings (20 suit units). The hall's light dies and the living guards
   collapse into blocks, dropping nothing.
5. The way back is the step under the hole, then the shaft as today.

**Going wrong:** the suit empties and the hero falls. With no one awake in the hall, the guards are back in their niches whole
after 5 s. A friend finds the body and the same two guards, minus any already killed.

**Sequences:**
- **Nailer.** 24 HP takes 4 nails, so about 24 s for both: about 14 pounces at 5 units against about 40 units left, so the hero
  must dodge about 6 of 14. Tight.
- **Sprint.** About 2 s before the first blow, 2 s of swings: the shard costs about 20 to 30 units.
- **Kill one and leave.** The dead guard's post is felled; the other is whole on return.
- **Digging.** A side tunnel goes unnoticed until it breaks into the arena box, then they wake. A cradle dug from under fells as
  today and the guards drop the same tick. A post's wall dug out: that guard falls lootless.
- **Two heroes.** The fight lasts while anyone awake is in the box. Bodies are ignored.
- **Ledge.** Guards sleep until struck. A nail wakes both and the struck one flies at the shooter, stopping at the lip if it hits it.
- **Save and join.** Guards are ordinary creatures in the save, mid-flight included.

## Decided with the user

- Guards live only at a spire's bottom; melee only on the first planet, so a hero can dodge. No weapon comes with this: a better gun
  waits for the first play pass.
- **Look:** «Истукан», the guard page's candidate 4: a folded statue that reads as a column in its niche, flies as one block fists
  first, and falls apart into blocks ([Стражи шпилей](https://claude.ai/artifact/Lp99qu7tzLVfCphA1xLkT3)).
- **Sounds,** from the [sound page](https://claude.ai/artifact/29qL2G5VjURhUQdTtv5jMw), each Freesound's HQ preview of a CC0 recording:

| Moment | Sound | Author | Id |
| --- | --- | --- | --- |
| Wake | Robot Awakening, Power On | GammaGool | 746988 |
| Alert | Synthetic monster growl | Wax_vibe | 556613 |
| Wind-up | hydraulic office chair moving effect 01 | rubberduck9999 | 678452 |
| Pounce | burst3 | ReadeOnly | 186926 |
| Blow on a hero | PUNCH-BOXING-03 | newagesoup | 348242 |
| Killed | Computer Breaking Sound | thehorriblejoke | 505751 |
| Shut down | MachinePowerOff | Skylar1146 | 395378 |
| Landing, stuck | a hero's landing on alloy, `Sound.LandAlloy` | ours | none new |
| A tool's blow on it | a tool's blow on alloy, the alloy row of `Sound.Ground` | ours | none new |

The user also liked Big robot footstep 002 (813053) for the landing and took the hero's as "also fine": a one-line swap after the play pass.

## Out of scope

Day and night, new weapons, nests, other enemies, boss logic, scaling by number of players (four heroes find it easier: accepted).
Guards on the surface, in the shaft or in homes. Changes to the sentry, the shard's split, homes, or spire count.

## Approach

**One prefab.** `Ruins.Spire` is redrawn 25 characters wide (50 cells; a character is 2×2 cells), with blank flanks (`Ruins.Keep`)
above the hall, so the stamp and what clears a box work unchanged. Rows 0 to 17 are today's, at columns 8 to 16. The interior is
46 × 18 cells:

```
#########....############  18 ceiling; the hole is the shaft's
#.......................#  19
#.......................#  20
#.......................#  21
#.......................#  22
###......###..........###  23 step under the hole, the flue's column open; the niches' lintels, three rows over a guard so it can fly out rising
#.......................#  24
#.......................#  25
P.......................P  26 P a post, in the wall (a piece, not the prefab)
#........c.CCC...........#  27 c cache, C cradle under the light, both as today's, eight columns in
#########################  28
#########################  29
```

**Where it stands.** The anchor stays over the tip's 18 columns (`SpireTip`). Inside its band, scan from the seed's pick for the
nearest column where the hall's ceiling lies `HallCover` rows under the lowest surface across the hall, as `Homes` finds a place. If
none qualifies, use the pick and accept a hall that shows, as 0085 accepted for spires (a hero then walks in from a valley and wakes
them as anywhere). The shaft never changes length. Bands, `RuinClear` and the spawn invariants all take the hall's columns, so no two halls meet.

**The small test planet splits in two,** for a spire's hall and a home cannot both fit (home 68 + margins 8 + hall 50 = 126, against
110 usable): `Small` keeps its home and holds no spire; a new `SmallSpire` (`Small` 192 wide, with one spire and no home) is grown the same way.
Guard scenarios and the fuzz use `SmallSpire`: on `TestStrip` a load grows no flora, so its posts and relic would vanish and a test of "no
respawn after a load" would pass for nothing. The fuzz opens it beside `Small`, so its draws shift once: accepted, as the sentry's did.

**The post, felled for good.** A `Growth` may carry `Post(Species, Stand, Arena)`: where the body stands and the arena box, both
relative to the post. It is a rock piece in the end wall (`Anchor.Rock`, no yields), so "dead for good" is one int in
`Felled`, and nests can use the same record with a hittable piece. Digging out a post's wall fells it through today's sweep.
- *Rejected, a saved list in Creatures:* it duplicates what Building keeps.
- *Rejected, saved guards from world start:* eight creatures ticking with no one near.

**New rules, and why existing ones do not cover them:**
- `Lifetime.Posted`: on the beat, before `Spawn`, a creature appears at a standing post with a standing relic, a hero in its keep box
  and none bound to it. Wild spawning draws a random ring by habitat and forgets. No draw. A full cap or a blocked `Stand` (something
  built in the niche) waits for a later beat. The binding is the guard's: `Mem` keys `post` and `relic`, their cells, set as it appears.
  Building keeps the standing posts as a derived list, as it does the resonators. `Release` refuses a posted species `creatures.notHere`, so
  no guard exists without a post.
- **Body: `Walker`, not a new one.** It has gravity, overlap, flush and a still gait. A new flag on the move's rule, `Flies`, set by the
  `Pounce` only when a walker makes it, gives: no gravity while it is active, no stepping up (`grounded: false`), and the act ending at the
  first solid cell on its path, wall, floor or ceiling. It aims from the guard's middle at the hero's middle, so a level flight does not
  touch the floor on its first tick. A lunge sets no flag, so lopers walk as they did. About 10 lines.
- `Pounce` as a record is reused unchanged.
- `Ward(Wake, Stuck, Calm)` drive. `Guard` does not fit: it senses by distance through rock, crawls, shoots and reads its wreck. Asleep →
  waking → waiting → pounce → stuck → waiting, stuck on the floor so an axe reaches it.
- **Waking:** an awake hero's reported cell in the arena box (hall interior plus the hole), a blow, or a sibling awake. A hero in rock
  outside the box is not sensed.
- **Turns:** a waiting guard goes if no sibling is crouching or flying. A stuck sibling does not block, so one's window is the other's
  crouch. Ties go by id order. Accepted: once in a while one goes twice running.
- **Quiet removal** (new path, `Kill` always rolls yields), checked each tick in `Live`'s kept loop and dropping the creature there, never
  through the `Dead` mark: `Dead` is not saved, and `Fly` may return before its sweep, so a save would bring it back. Three triggers: its relic
  taken; its post gone (a dug wall); and `Calm` on a woken guard (no awake hero in the arena box, no blow, for 5 s; `Posted` then brings it
  back whole on the beat, so it pops back rather than flying home). A sleeping guard is left alone till the keep box drops it, so nothing
  flickers beside a spire.
- `FellPost` on `IBuildingOps`, only for a kill, doing nothing to a post already gone (a wall break fells one at once, and `Fell` throws on a
  second felling). `Relics` joins `IBuildingQueries`.

**Same tick.** Commands and Building's sweep come before `Live`, so a cradle hit or dug in commands or derivation drops the guards that tick. A
wall a shot breaks (`Fly`, after `Live`) or a meteor's fell (next derivation) gives them one tick more: accepted. A strike landed in the
commands of the tick the cradle falls, on a guard with its last hit points, still pays its loot: accepted.

**Lifetime.** The keep box still removes guards, forgetting damage: that is "whole again".

**Tunables, `sim/Content`:**
- `Warden` species (id `warden`; `Guard` is the sentry's drive), Hp 24, never knocked.
- `Pounce(WindUp 16, Speed 1800, Ticks 28, Damage 5)`: 0.8 s, 36 cells a second, 1.4 s at most, 5 units. `Ward(Wake 10, Stuck 20, Calm 100)`, in ticks.
- Yields `circuit` 2 to 3, `alloyScrap` 1 to 2: 4 to 6 circuits a spire.
- `HallCover` 4, the prefab, the posts.

## Determinism and co-op

Hero positions enter as commands; hits on a hero are judged by that hero's client through `HitBy`. Integers only: flight shares round
toward zero, as the sentry's. Creatures iterate in id order, posts by cell; spawning draws nothing. Saved: `Felled`, creature fields
and `Mem` keys. **No save-format bump:** no field is removed or changes meaning, no existing species' brain changes order, a new species
adds none, and ground regrows from the seed with edits applied by cell (0085).

## Trade-offs accepted

- Old saves: a spire moved or redrawn keeps old edits by cell, so holes may sit in new walls and a felled cradle may stand anew with
  four more shards. Not worth a migration.
- A long straight tunnel or a lucky ledge angle snipes for loot: it costs 15 s per alloy cell and 12 s per guard.
- Sneaking under the floor: cradle felled, guards never wake, the loot given up.
- Walls built in the hall stop flights: a friends' game, no guard.
- Fluffs may spawn in the hall: harmless, as in homes.
- Guards count toward `PerKeep` and `Max`: a post waits for room at the cap.
- Spires are slightly less random in where they stand.

## Tasks

Each is its own branch and PR, merged before the next starts.

### G-1: The hall

The redrawn prefab, its placement, `Small` and `SmallSpire`, `SpireFurnishing`'s cells moved with the hall. The `P` cells are plain alloy
walls till G-2. No creatures: the hero meets an empty hall. The ruins' README says so in the same PR.

**Done when**
- over seeds on `First`, the ceiling lies under cover, the hall stays in the zone and clear of homes and other halls, and tips are
  anchored as before (FsCheck);
- a climb test goes from the hall's floor to the tip;
- a frame of `--ground=spire-in` shows the hall lit under its flue.

### G-2: The guards in the sim

`Post`, `Lifetime.Posted`, `Ward`, the walker's flight, turns, quiet removal, `FellPost`, `Relics` on the query interface, the posts in
the spire, drawn as a placeholder box. Guards meet the fuzz on `SmallSpire`. `sim/Creatures/README.md` and Building's say so in the same PR.

**Done when** scenario tests cover, on `SmallSpire`:
- sleeps with no hero; a hero digging beside the wall does not wake it; entering the box does; a blow from outside does, and the sibling too;
- crouch, straight flight, flush stop at a wall;
- `HitBy` costs 5 and leaves it stuck; refused `creatures.notAttacking` while stuck;
- turns alternate; a lone guard goes on;
- a kill yields and fells its post; no respawn after leaving or a load;
- calm removes it and it returns whole; a second hero keeps it; a body does not;
- hit and dug-out cradles, and a post's wall dug out: guards gone that tick, no drops;
- a sleeping guard with a hero only in its keep box stays and does not flicker; a blocked niche waits; a second `FellPost` does nothing;
- saved and loaded in the tick of a quiet removal, it stays gone;
- `Release` of a guard refused;
- the sentry's contract and a lunging walker's run unchanged, pinned by a trace (the sentry's alone never walks);

and invariants hold: every live guard's post and relic stand after `Live`, but for the one tick a later fell gives (Same tick); never two
siblings acting; at most one guard per post; the body on the grid, not in solid.

### G-3: Its look, its sounds and the docs

- **Art:** «Истукан» drawn from the page's own code, as the tick is (`TickArt`, `TickBodies`), in every state: asleep as a column, its eye
  dark, teal, yellow and red, the crouch, the flight, stuck, a blow's flash. The fall is the client's: the eye fades, the blocks fall and lie
  3 s, when a guard goes with its relic taken or its post gone, a kill's post among them. One gone with both standing is a calm: it simply is
  no longer there, with no hero in the arena to see it.
- **Sound:** the seven files cut, levelled and credited as `game/audio/CREDITS.md` says for the sentry's; the two reused cues keyed on the
  species. All on the World bus: a planet has air.
- **Dev:** one demo, `--ground=spire-guards`, recorded by movie.
- **Docs:** the ADR, `game/features/creatures/README.md`, `docs/concept.md` (Dangers), `docs/map.md`, `docs/glossary.md`.

**Done when** frames show every state, a recording walks the kill and the cradle sequences, and the user has played it.

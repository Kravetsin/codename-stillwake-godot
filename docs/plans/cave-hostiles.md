# Design: hostile creatures in the first planet's caves

Status: accepted (the user, 2026-11-07); slice 1 built ([cave-hostiles](../decisions/2026-11-07-cave-hostiles.md)), slices 2 and 3 to come. Source concepts: the user's page «Жители пещер»
(https://claude.ai/artifact/62VbzFvuwAMw7p86qjbEGp), seven cave creatures drawn as peaceful; the user (2026-11-07):
take them and turn some hostile.

## Problem

The caves have nothing living in them. A hero can go down to the lava with no threat on the way, so depth costs nothing
and gives nothing a hero couldn't get by digging.

## Player experience

1. A hero walks through the teal garden. One clump of glowcaps pulses a little slower than the rest. When the lamp beam
   crosses it, an eye catches the light.
2. The hero comes within 14 cells. The clump stops pulsing and goes dim: a **capback** has noticed. At 10 cells its shell
   rises, the caps flare violet and it chitters. 0.6 s later it springs straight at where the hero is.
3. The hero steps back or jumps. The capback lands short, and the hero has about a second of blows before it crouches
   again. Dead, it drops biomass and sometimes a spore.
4. Deeper, in the amber deep, a grey stone with an amber slit pushes out eye stalks. The stalks snap back, the lid clacks
   shut, and the **geode snail** rolls along the floor at 24 cells a second. The hero jumps over it. A blow during its
   crouch does not stop it. Dead, it drops crystal.
5. **Going wrong:** a hero stands and swings without watching the crouch. They take 4 or 6 units of suit air per hit, with
   today's flinch and 50 ms stop. An empty suit passes them out, and lurkers ignore a hero who is out.

## Which of the seven, and when

| Creature | Band | Verdict | When |
| --- | --- | --- | --- |
| Capback (Шляпоспин) | garden | **hostile**: a mimic ambusher | slice 1 |
| Geode snail (Жеодник) | amber deep | **hostile**: a rolling stone | slice 1 |
| Threadling (Нитевик) | garden ceiling | hostile: drops on a hero passing below | slice 2, with a ceiling-hanger body the icicler shares |
| Embermoth (Тлянка) | lava core | hostile: dives at the head | slice 3, with a flyer body |
| Driftbell (Звонница) | garden | stays peaceful | slice 3, on the same flyer body |
| Glimmite (Глиммит) | cold | stays peaceful: follows the lamp | later: a back-wall body and the lamp in the sim |
| Quillmole (Иглорой) | cold | not in this design | waits for the lava worm's burrowing body |

Only these two fit today's `Walker` body; every other needs a new body. Danger grows with depth: the cold band stays calm,
the garden ambushes, the amber deep hits harder.

## Out of scope

- Chasing. Lurkers hold their ground; the pack hunter comes with paths.
- The snail's shell armour (damage ÷4): it comes with wards, its first user.
- Light changing anything in the sim.
- A new hit path, health or hit-stun. Ranged hostile attacks.
- Any change to grazers, the sentry, wardens or the tick loop.
- Night spawns and liquids: other sessions own those.

## Scale

- World: at most `PerKeep` (6) creatures in each hero's keep box, 40 per surface, as today.
- Players: up to 4, each with their own spawn ring.
- Frequency: spawns on the 2 s beat; a lurker strikes at most every ~1.1–1.8 s while a hero stays in its reach; at most one
  `HitBy` per strike.

## Approach

**`Creatures` owns everything.** No new module, no new command.

**`Guard` (notice → alert → strike) serves walkers too.** It already does what an ambush needs: a hero within `Notice`
alerts it; after `AlertTicks`, a hero within `Strike` gets its first move; a blow is answered at once. Two changes:

- `ClingerBody.Crawl` returns at once for a body that is not a clinger (today it casts and would throw); a walker on guard
  stands still. Blows on a wreck need nothing: a walker has no piece, so none resolves.
- A walker notices and stays alerted only while the line through the cells to the hero is clear, walked by the cell walk the
  shots already use (`Shots.cs`), not a new one. A walker's move stops at rock, so without this a capback would spring at
  the ceiling under a hero digging in the tunnel above, which looks broken. A clinger's pounce passes walls, so the sentry
  keeps sensing through rock.

The later lurkers use the same rule: the threadling and the icicler (they drop on a hero in line below), the angler.

**Not a chase drive:** walker steering cannot find a way through caves, and a chaser stuck at every ledge looks broken.
Chasing comes with the carapace pack and paths.

**Moves are the existing ones:**

- Capback: `Pounce`. It is never cut short and a walker's flies without gravity until it meets rock.
- Snail: `Lunge` with one new field, `Steady`: a blow cuts neither its crouch nor its roll. Today a blow cuts any lunge
  and `Guard` answers it with a new wind-up, so the drill or an axe every 10 ticks keeps the snail crouching forever (0103
  records this lock) and it never attacks. `Interruptible` is per rule today; it becomes `Interruptible(Move)`.
- A lunge aims only at an awake hero (`Nearest(…, awake: true)`, as the pounce already does): lurkers leave a passed-out hero be.

**A new faction `beasts`** that hunts the crew, for the cave hostiles now and the carapace pack and the loper's predator
next. Through `Threatens`, heroes near a lurker report their cell every tick, so strikes aim at where the hero is now.

**Spawning:** each hero's herd is drawn only from habitats in that hero's own depth band; surface habitats count as the
cold band. Without the filter a hero draws a habitat of another band, finds no place and loses the beat: cave habitats
would thin surface spawns, and surface weights would starve the caves. Weights alone cannot fix both. Herds stay the global
2–4 for every kind: no herd size per habitat (cut in review), so a snail comes two to four.

**Species (placeholder numbers):**

| | Capback | Geode snail |
| --- | --- | --- |
| Body | Walker 4×3, walk 100, never jumps | Walker 4×3, walk 30, never jumps |
| Life | 10 hp (5 axe hits), knock 300, hop 200 | 24 hp, knock 0 |
| Brain | `Guard(Notice 14, Strike 10, AlertTicks 10)` | `Guard(Notice 24, Strike 16, AlertTicks 20)` |
| Move | `Pounce(WindUp 12, Speed 1600, Ticks 8, Damage 4)`: about 12 cells | `Lunge(WindUp 16, Speed 1200, Ticks 30, Past 6, Damage 6, Steady)`: 24 c/s, under a sprint |
| Drops | biomass 2–3, spore 1 in 2 | crystal 2–3, biomass 1 |
| Spawns | garden band | amber band |

**How they are fought:** the capback — bait the spring, step back, hit during its ~1.1 s recovery. The snail — jump the roll,
hit during its alert and crouch, ~1.8 s.

**The lamp stays client-only.** A worn lamp is always on (`Items.Equipped`), so "lit" would mean "a hero near", which the
sim knows; the beam's aim is a client float changing every frame, a new stream of reports; nothing in slice 1 moves by
light. The lamp is flavour from every lamp in `HeroView.HeadLamps`: the capback's eye glints, the snail's slit narrows.
Revisit with the first creature whose body moves by light (the glimmite or the embermoth).

**Client** (`game/features/creatures`, the parts rig): a lurking pose (the capback hunkered, head tucked; the snail's lid
shut); the alert read from `Alerted`, the crouch and strike from `WindUp`/`Charge`; glowing caps and the amber slit give
light as glowing parts do; the snail struck as stone. Art and three sounds (chitter, clack, grind) through pick pages.

**Saves:** no format bump: new species need none, content records are not saved, no new `Mem` keys.
`CreatureContractTests` pins the two new brains.

## Determinism and co-op

- Everything enters through commands that exist: hero reports, `Strike`, `HitBy`.
- The line check walks grid cells in integers from the creature's head cell to the hero's reported cell, alike on every peer.
- Ties: nearest by box distance, then by id, as today.
- A player joining mid-session gets the creatures in the snapshot.
- Hits are judged by the hit hero's client (0103); the lamp flavour is per screen. Fine, for no rule reads them.

## Trade-offs accepted

- Ranged weapons (the nailer) kill lurkers from beyond their reach unanswered: ammo costs, and this planet's hostiles are melee.
- Walking away is always safe: lurkers don't chase, till paths.
- Capbacks spawn on any garden floor, not only among glowcaps: garden floors are thick with them.
- The snail rolls only along the floor; a hero on a ledge above is missed.
- A strike aims a round trip behind; a second hero it touches pays nothing (0103).

## Tests

- Scenarios: no hero near, it stays still; a hero in line within `Notice` alerts it, rock between does not; alerted, it
  strikes a hero within `Strike` and waits for one farther; a passed-out hero is ignored; struck, it answers; a blow does
  not cut the snail's crouch (`Steady`); the roll stops at a wall or drop; `HitBy` costs 4 or 6; a hero in the garden spawns
  only capbacks, one on the surface as today; a snail's lunge passes a passed-out hero by; the sentry still crawls and
  answers blows on its wreck.
- Invariants: a lurking walker moves only when knocked or striking; every spawned herd's band is its hero's band; the
  existing creature invariants hold.
- Existing tests to change: `…NoneInTheCaves` (`CreaturesTests`) asserts empty caves; `CreatureContractTests`' `LetOut`
  and its sentry-guards case assume a guard is a clinger.

## Cost

- Modules: `Creatures` (Guard on walkers with the line check, `Lunge.Steady`, the awake lunge, the band filter).
- Content: two species, a faction, two habitats.
- Client: two designs, a lurking pose, the tells, sounds, names (Шляпоспин, Жеодник), a dev room.
- About 800 lines with tests and client, art aside.

## Review (design-reviewer) and what was done with it

- Kept against the reviewer: both creatures in slice 1 (the snail is cheap: `Steady` and data), the per-hero band filter
  (the mechanic every cave kind needs), the line check (springs at rock look broken).
- Taken: herd size per habitat cut; `Guard` on walkers stated as gating `Crawl` plus the line check; the shots' cell walk
  reused; the awake lunge; `Steady` as `Interruptible(Move)`, cutting both phases; the faction's next users named; the
  existing tests to change listed.

## Open questions

1. Ship the capback and the snail first, and leave the cold band calm for now?
2. 4 and 6 units a hit against a full suit: how many hits should one cave trip absorb?
3. Art: both redrawn in ink on a pick page before the PR (picked art ships whole). Sounds: reuse the grazers' squish and
   ram sounds in slice 1, a sound page later?
4. Should a lurker hit from range come out after the shooter? That needs chasing and paths.
5. Capbacks only next to glowcaps? A new spawn place rule.
6. Do the glimmite and the driftbell stay peaceful, and does the quillmole wait for the worm?

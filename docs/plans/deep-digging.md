# Deep digging: sulfur, the blasting charge, deep caches (design and plan)

Status: design for the user's review (2026-11-08), drafted by `design-author`, cut by `design-reviewer`. Nothing built.
The open questions at the end are the user's to answer before slice 1.

## Problem

Playtesters found the first planet's descent dull: a thousand rows of the same rock, no tool that speeds the way down,
nothing to find between the depth bands (`PlanetType.Bands` = [250, 600, 850]: 0 cold, 1 garden, 2 amber deep, 3 ember
over the lava). On the first planet band 1 starts some 300 rows under the surface's middle, band 2 at about 726, band 3 at
about 1028, the bedrock near 1210; the rift ([planet-layers](../decisions/2026-11-08-planet-layers.md), on its branch) runs
to about 1060. Harder rock deeper was rejected by the user: digging would be even longer.

Two answers: an ore that turns into a faster way down, and finds worth digging to in every band.

## Rules as the player meets them

### Sulfur

1. **Where.** Yellow pockets in the stone, richer deeper: rare specks in the garden band (band 1), more in the amber deep
   (band 2), most over the lava (band 3); none in the cold band. The rift crosses some 300 rows of band 2, so its walls show
   pockets on the way down. A pocket shines in lamp light like every vein (ore-pockets' shine and glint, no new code).
   Target shares, pinned by a test over 16 seeds: about 0.1 % of band 1's stone, 0.6 % of band 2's, 1 % of band 3's.
2. **What it is.** Material `sulfurVein` (`In: "stone"`, Strength 10, Armour 4, cost `("sulfur", 1)`) and item `sulfur`
   (stack 250, `Market.Raw` 40). It goes into nothing but the charge.
3. **Why band 1 too:** the garden band is where the player is when digging starts to drag; sulfur first met at 726 rows
   would make the charge speed up only the last 40 % of the way. Its Marks keep band 1 rare, so the bulk is still deep.

### The blasting charge

4. **Learned** by the rule that grants (`Research.Understood`): 5 sulfur reach `EnoughWorth` 200, and the workbench shows
   it. Made at the workbench: 2 sulfur and 1 tube, 40 ticks; stack 50; sold at its worked-out worth.
5. **Thrown.** A charge in the hand is a gun that fires itself (`Gun` with `Ammo: ["blastCharge"]`): it lobs on an arc
   (Weight 3), at most 40 cells, a throw a second (Pace 20), and bursts where its shot ends: on the first wall, creature or
   wreck it meets, or in the air at the end of its reach. A creature it strikes takes its direct damage (10); nothing else is
   hurt. Refusals are the gun's own (`creatures.noGun`, `creatures.blocked`, `items.notEnough`): no new keys.
6. **The burst** is the meteor's wave ([meteor-wave](../decisions/2026-10-29-meteor-wave.md)) from the shot's end:
   `Blasting.Power` (about 100, tuned so solid stone gives a crater some 20 cells across) spent at `Meteors.Step` a cell
   (`Diagonal` aslant) and `Meteors.PerStrength × Armour` for each wall, inside a box of `Blasting.Reach` (10) cells. Stone
   costs 10 a cell, soil 4, ruin alloy 16: soft ground opens wider, alloy soaks it. It starts at cost 0 where the shot ended
   in the air, at the wall's cost where it ended on a wall.
7. **It breaks natural ground only** (`Material.Natural`): stone, soil, veins, ruin walls. A built wall, back walls, a
   door, ladder, machine, bedrock, lava or fog stops it untouched. Aboard the ship it breaks nothing; the charge is spent.
8. **What drops:** an ore's cost (a material with `In`: copper, iron, ice, sulfur ore) and the yields of every growth a broken
   cell held go to the thrower as digging gives them (`GiveOrDrop`). Plain ground and ruin walls go to dust: a crater of
   300 stone cells would otherwise give 300 rock. It is a way down, not a way to mine rock.
9. **Wrecks.** Every wreck whose box lies within `Blasting.Reach` of the burst takes `Blasting.WreckDamage` (120) through
   `IDebrisOps.Hit`, in id order: a charge thrown into a drifting cluster breaks most of it at once. The thrower is the hitter.
10. **Co-op.** The aim is the client's word (0034), among friends. A friend's base is safe because built walls stop the
    blast; a friend or creature standing in the crater is untouched, though the ground under them may go (no fall hurts).

### Deep caches

11. **One per band kind.** Four `Flora.Cave` kinds anchored in rock (`Anchor.Rock`, as the geode), appended last:
    `frostCache` (band 0), `bloomCache` (1), `amberCache` (2), `emberCache` (3), each with a chance in its band only,
    `Apart` 200; about 16 / 16 / 12 / 8 a planet, tuned by a count test.
12. **What it holds:** one trade good, `opal` (stack 50, `Market.Raw` 150, no craft use, sold only), 1 / 2 / 4 / 7 of it
    by band: 150, 300, 600, 1050 credits. An amber cache is worth about a geode's six crystals. Dug or blasted, it gives
    its opals like any growth.
13. **Seen from afar:** each kind glows in its band's colour through the dark, as the heat's cracks do; a glow means a
    cache. Its look and colour are the user's pick.

## Why the existing ones do not cover it

- **A new ore:** the charge needs a raw input no current ore spares; copper and iron already carry the early crafts, and
  magnetite stays at the core by the user's call.
- **The charge:** every tool hits a fan of a few cells next to the hero; none breaks an area from afar.
- **A burst helper in Core:** the meteor's wave is the area break we want (cost spread, walls soaking); moved out of
  `Hazards` once, both use it instead of a second, square radius.
- **Caches as growths:** none of today's growths yields what only sells, and none lives in every band. The spire's cache
  is stamped with a ruin; these need to be scattered, which growths already are.
- **One `opal`, not one per band:** the cache kinds differ by look and count; four items would add three icons, prices and
  translations for nothing the player does differently.

## Determinism, saves, budgets

- Every input is the existing `Fire`. The wave is integer, no division; cells break in ascending order, wrecks in id order,
  bursts in shot id order within a tick.
- Nothing new is saved: shots in flight already are. Old planets grow sulfur and caches in rock nobody dug, as ore-pockets
  did: the vein and the growth kinds are appended after every existing one, so no existing ore or growth moves. No format bump.
- A burst walks a 21×21 box once. Nothing per frame on the client but the burst's own look.

## Deliberately not handled

- Harm to heroes from a burst; area harm to creatures (a direct hit only).
- Built walls, back walls, doors, machines: never broken by a burst (see open question 1).
- A placed charge, a timed fuse, chain reactions between charges.
- Sulfur in anything but the charge; gunpowder, ammunition, fertiliser.
- Rock from a crater; ruin walls' alloy from a crater (lost, the price of blasting a ruin instead of cutting it).
- Credits paid straight out of a cache; a cache that refills; caches on other planet types (Frost: later, by its own rows).
- Detectors, map markers or a sound through rock for caches.
- Harder rock deeper (rejected), temperature by band and new creatures (other sessions' work).
- Principle 7: the charge needs a planet, so nothing up to the drop pod uses it; `ContentTests`' list of what needs a planet
  gains it.

## Open questions for the user

1. **Built walls.** Should a burst break built walls too (a friend's base, your own hull next to a wreck)? Recommended:
   no, natural ground only: in space charges go off next to the ship's hull.
2. **Sulfur in the garden band.** Rare specks from band 1, or only from band 2 near the heat (your first idea)? Recommended:
   band 1 rare, so the charge arrives while the descent is still long.
3. **One opal or four goods.** One `opal`, the caches differing by look and how many they hold; or each band its own good?
   Recommended: one.
4. **Thrown or placed.** Thrown on an arc, bursting where it lands; or placed with a fuse? Recommended: thrown: heroes take no
   harm, so a fuse buys nothing.

## Plan

Each slice its own PR; numbers in `sim/Content`, every rule and refusal a scenario test.

1. **Sulfur.** `sulfurVein` in `Materials.cs` (code 17, or the next free one if `feat/planet-layers` takes it first),
   `sulfur` in `Items.cs`, `Market.Raw`, a `Vein` with `Pockets` appended last in `PlanetTypes.First.Veins`. Tests: shares
   per band over 16 seeds, none in band 0, pockets on the rift's walls in band 2, existing ores identical before and after.
   Its look: the user's pick (an Artifact page), then `tools/anime-art`.
2. **The wave moved.** `Hazards.Strike`'s wave to a pure function in `sim/Core` over a cost-per-cell callback (null: stops)
   and a starting cost; Hazards calls it with its rules. A pure move in its own commit: meteor tests unchanged and green.
3. **The charge.** `Content/Blasting.cs` (`Power`, `Reach`, `WreckDamage`), the `blastCharge` item and gun in `Shots.cs`,
   its craft in `Crafts.cs` and the workbench's row in `Stations.cs`. In `Creatures`, a `blastCharge` shot's end calls the
   wave with natural walls only; `Digging.Blast(peer, cell)` (on `IBuildingOps`) clears a cell, yielding an ore's cost and
   its growths' yields; wrecks through `IDebrisOps.Hit`. Scenarios: a crater in stone and in soil; a burst in an empty cave;
   alloy soaks; a built wall and a door stop it; aboard the ship nothing breaks; ore and a geode go to the thrower, rock
   does not; wrecks within reach take 120, beyond it nothing; a direct hit hurts a creature; no charge refused. The
   generic fuzz draws `Fire` with the charge. The understood-time check in `ContentTests.Production` lists it.
4. **Caches.** `opal` in `Items.cs` and `Market.Raw`; the four kinds appended in `Flora.cs` with their chances and yields.
   Tests: counts per band over 16 seeds; existing growths identical before and after; a cache gives its opals when dug and
   when blasted.
5. **Looks and sounds** (after the user's picks on Artifact pages): sulfur's vein, the charge in the hand and its lob, the
   burst's flash, dust and sound, four caches and their glow, the opal's icon. Client in `HeroView.Guns`, `ShotEnds`, the
   cave view; En and Ru names.
6. **Docs.** An ADR (`YYYY-MM-DD-blasting-charge`), the Creatures, Building and Hazards READMEs, the economy ledger, the
   glossary (сера, заряд, тайник, опал), the map. This plan deleted once done.

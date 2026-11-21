# Plan: planets differ — types, ores and the crafts that need them

All six system planets are the same green planet with the same ores, and the deep ores (sulfur, magnetite, crystal) only
gate the way off planet 1: no reason to fly anywhere else. This plan gives each system planet a type and its own set of
ores, shown on the radar, and re-costs the crafts so the varying ores are worth the flight. Designed by `design-author`,
cut by `design-reviewer`, the user's decisions of 2026-11-16 over both.

Two slices, each played before the next: **slice 1** (`feat/planet-variety`) types, ore sets, caves by temperature and the
re-costed recipes; **slice 2** the three planet-stage crafts, their icons picked first on a pick page.

## Rules (the user's, 2026-11-16)

- Every craft is provisional (only the space stage was worked out), so recipes change where a mechanic needs it.
- **Ores.** Starting ores: copper, iron, ice and mica. Varying ores: sulfur, magnetite, crystal. Planet 1 always has
  copper, iron and ice; mica is on every planet. Planets 2–6 each get 2–4 ores of copper, iron, ice and the varying three,
  and may lack copper, iron or ice. Every varying ore lies on at least one planet.
- **Ores lean to the type**: icy usually sulfur and ice, scorched usually crystal and magnetite; others still possible.
- **Types**: First (today's green planet), Scorched and Icy, in a fixed order: 1 First, 2–3 Scorched, 4 First, 5–6 Icy.
  Every type keeps the Arc spires and homes. A type differs by temperatures, plants, creatures and a tint of soil and stone.
- **Caves by temperature and depth** (the user's idea): a cave growth grows where its temperature suits it; a planet
  without an ore grows none of the growths that yield it. Its place stays bare rock.
- **The radar's planet callout** lists the planet's ores as a row of names.
- Solar panels are never crafted: they come from derelicts and events (the user's plan).
- What does not change: the unlock rule (a craft is understood once each part was obtained); the flight and the landing
  (solar-system); space ores (asteroids, catchers) and creature drops (a geode snail drops crystal anywhere).

## Player sequences

1. First flight: the engine is built from planet 1's ores alone. The radar's planet 2: "Scorched · Ruins: Arc · Ores: iron,
   magnetite, crystal, mica". A course, a landing: red-brown ground, dry scrub, sixlegs, ember crystals and heat cracks in the
   caves; the basic suit near its limit. Magnetite and crystal dug; the crew shuttle and the railgun become buildable.
2. A planet missing copper: planet 5 (Icy) lists "ice, sulfur, mica"; the player goes there for blast charges, not copper.
3. A revisit: new ground, the same type, the same ores.
4. Save and load on a planet: the same ground grows again. A guest joining, even mid-descent, gets the sets and the open
   planet's ores in the save and grows the same ground.

## Slice 1 — shipped

Types, ore sets, caves by temperature and the re-costed recipes: [planet-variety](../decisions/2026-11-16-planet-variety.md).
The icy planet's own weather and art come next, before slice 2.

## Slice 2 — three planet-stage crafts (after slice 1 is played)

Icons picked first on a pick page (one each, the searchlight's look on the helmet too).
- **Heat pack** (sulfur 2, plate 1, workbench): eaten from the hotbar in a meal slot like the stim. `Rates.Warmth`
  (degrees, `Diet.Caps` 20): 20 for 12 000 ticks (10 min), subtracted in `LowOf` as the cape's is. No effect on heat.
- **Loot magnet** (magnetite 4, coil 2, plate 1): worn on the belt (against the battery); while worn the client's `Reach`
  pulls loose stacks from `Floor.MagnetReach` 6 instead of `PickUpReach` 2.
- **Searchlight** (crystal 2, light drone 1, wire 2): in the helmet mount, switched by `Run(item, On)` (a run's stack the
  hand's, or the mount's for a worn electric item); draws 1 from the belt every 3 ticks (the user: not to rival the drill's
  1 a tick); a narrow cone about 3× the lamp's reach with the lamp's light code; off or with an empty belt a plain lamp.

## Deliberately not handled

- New ores, tile art, backdrops, plants or creatures; a technology per type (Thermal forges, a Cryo vault, dungeons): the
  heading. Ore richness per planet; other systems; types drawn per world.
- Space ores, creature drops and ruins' loot ignore the sets (the Arc resonator gives crystal anywhere). The black market
  sells no ores (it would remove the reason to fly).
- The radar says which ores, not how deep: an ore found only in a hot band asks for the heat suit.
- An old save: its sets are dealt on its next tick; the planet it has open keeps every ore until left; `Planets[At]` may
  now be another type than the one it landed on. Saves break at release anyway.
- Tuning the weights changes nothing in a world already dealt.
- Lean, not certainty: an icy planet with crystal happens.

## Consequences

- The deal draws from a fork of the world's Rng (`Rng.Fork`, its state xored with a salt, nothing drawn from the world's),
  so no seeded test pinned to later draws moves.

## Supersedes in part

`2026-11-08-computer-after-planet` (magnetite in the computer and antenna), `2026-10-25-the-engine-through-the-hull` (the
motor's magnetite and the igniter's crystal gating the engine), `0080` (cave chances by band become by temperature).

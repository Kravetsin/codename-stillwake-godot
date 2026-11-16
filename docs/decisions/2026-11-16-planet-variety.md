# Planets differ: types and ore sets

- Status: accepted
- Date: 2026-11-16
- Supersedes: in part [computer-after-planet](2026-11-08-computer-after-planet.md) (magnetite in the computer and the
  antenna), [the-engine-through-the-hull](2026-10-25-the-engine-through-the-hull.md) (the motor's magnetite and the igniter's
  crystal gating the engine), [0080](0080-caves-glow-by-depth.md) (cave chances by band become by temperature)

## Context

All six system planets ([solar-system](2026-11-16-solar-system.md)) were the same green planet with the same ores, and the deep
ores only gated the way off planet 1: no reason to fly anywhere else. The user (2026-11-16) wanted planets to differ as in No
Man's Sky: a pool of ores, each planet holding some, shown on the radar, the player flying to gather all of them to open the
crafts; types leaning to their ores; cave growths spawned by temperature and depth; the simplest version first. Every craft was
called provisional (only the space stage was worked out), so recipes may be re-costed. Planned in `docs/plans/planet-variety.md`.

## Decision

- **Types.** `PlanetTypes.Scorched` (`scorched`, 45/30/50/70 °C by band) and `Icy` (`icy`, −35/−30/−10/40), each the first
  planet's ground with its own temperatures, plants, creatures and ore weights. The system in a fixed order: 1 First, 2–3
  Scorched, 4 First, 5–6 Icy; Arc ruins on every one.
- **Ores.** The pool `Landing.Ores` (copper ore, iron ore, ice, sulfur, magnetite, crystal, mica). Planet 1 always holds the
  starting three (copper, iron, ice); each varying ore (sulfur, magnetite, crystal) lies on one of planets 2–6 at least;
  planets 2–6 hold 2 to 4 of the six; mica is on every planet. Landing deals them aboard on the first tick
  (`LandingState.Ores`, saved), from a fork of the world's Rng (`Rng.Fork`) so no later draw moves; each step draws by the
  type's `OreWeights` by cumulative sum, integers only. `OresOf(planet)` reads them.
- **A planet's ground holds its ores only.** `Surface.Ores` (saved in the surface's entry, null for all: tests, dev opens,
  old saves); `World.Write`, `Load` and `Open` carry it; `Terrain.Generate(type, seed, ores)` grows it. An absent ore's veins,
  pockets and cave growths still claim their cells and leave them bare stone, so every other cell, growth, lake and ruin
  stands as with every ore. Ruins' pieces, space ores and creature drops ignore the sets.
- **Caves by temperature.** Each cave kind's `Chances` stand at `Flora.Degrees` (−30, −15, 15, 60, the first planet's bands,
  so its caves are unchanged); a type's band reads its chance at its own temperature, straight between points, rounded
  down, held past the ends. The opal caches stay by depth (`ByDepth`).
- **Re-costed.** Motor: rotor, copper 6, coil 2. Igniter: copper 2, coil 1, circuit 2, wire 4. The computer and the radar's
  antenna lose their magnetite; the arc module takes mica 4. The engine, computer, antenna and heat suit are made of planet
  1's ores; the crew shuttle (magnetite), railgun (crystal) and blast charges (sulfur) need a flight.
- **Seen.** The radar's planet callout lists its ores (rows wrap), each type its own colours. On the surface each type has
  its own daylight multiplying sky and backdrop, a ground tint and fog: stand-ins until the types get their own art.

## Deliberately not handled

- New art, plants, creatures, weather or a technology per type; other systems; types drawn per world; ore richness.
- The radar says which ores, not how deep: magnetite on a green planet lies only in its ember band, past the basic suit
  and inside the heat suit.
- An old save is dealt on its next tick; the planet it has open keeps every ore; `Planets[At]` may now be of another type.
- Tuning the weights changes no world already dealt. The black market sells no ores.

## Tests

`PlanetVarietyTests` (the deal's property, proven to bite without its second step; an absent ore moves nothing; cave chances by
temperature; every ore a type may hold reachable in the basic or heat suit), `CourseTests` (a descent's type and ores, a
revisit, save and load on a planet, a guest joining mid-descent, an old save mid-descent), `ContentTests` (the way off
planet 1 needs no varying ore).

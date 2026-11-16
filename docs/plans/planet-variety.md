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

## Slice 1 — sim

**Content (`sim/Content`).**
- `PlanetTypes.Icy` (id `icy`) and `Scorched` (id `scorched`), each the C# `First with { … }` of its numbers, in `All` (so
  `Surfaces.Get` finds them by id). Temperatures by band: Icy −35, −30, −10, 40 (inside the basic suit's −40, so slice 1
  plays without the heat pack); Scorched 45, 30, 50, 70. Icy: no acacia or bells, rock and tuft weighted
  up; tapir 3, loper 1, capback in band 1; ice veins in bands 0–2. Scorched: no teal or violet bells, rock and amber bells
  weighted up; sixleg 3, loper 1, geode snail in bands 2–3; veins as First's.
- `Landing.Planets` in the order above; `Landing.Planet` stays, as `Planets[0].Type`.
- `Landing.Starting` (copper, iron, ice, mica), `Landing.Varying` (sulfur, magnetite, crystal), `OresLeast` 2,
  `OresMost` 4; `PlanetType.OreWeights` by ore:

| Type     | copper | iron | ice | sulfur | magnetite | crystal |
| -------- | ------ | ---- | --- | ------ | --------- | ------- |
| First    | 4      | 4    | 4   | 2      | 2         | 2       |
| Scorched | 2      | 3    | 1   | 2      | 6         | 6       |
| Icy      | 2      | 2    | 6   | 6      | 1         | 1       |

- An ore is an item id (`copperOre`, `ironOre`, `ice`, `sulfur`, `magnetite`, `crystal`, `mica`). What grows it is
  derived, not tagged: a vein's material's `Cost` item, a pocket's, a cave kind's `Yields`.
- **Re-costed recipes:** motor (rotor 1, copper 6, coil 2); igniter (copper 2, coil 1, circuit 2, wire 4); the computer and
  radar antenna lose their magnetite, keep their copper; the arc module takes mica 4 instead of crystal 2 + mica 2. Kept:
  blast charge (sulfur 2), crew shuttle (magnetite 6), railgun (crystal 2).

**Cave chances by temperature.** Each `Flora.Cave` row's per-band `Chances` become points `(degrees, chance)`; a band's
chance is interpolated linearly from the band's temperature, clamped at the ends, rounded down (floor division, not C#'s
toward zero), once per type per band where `CaveFlora` builds its `chances` table today (no cost per cell). Depth enters
only where it is the reason: `Shallow` for roots as today, and `CaveKind.Depth` (`From`, `To` band, inclusive; null =
any) for the four opal caches (frost 0–0 … ember 3–3, as today). Every type grows from `Flora.Cave`; the per-type `Caves`
lists go. Each row's points sit at First's temperatures (−30, −15, 15, 60) with today's values, so planet 1's caves are
unchanged: a test pins the derived table to today's. Other types shift as intended — the point of the idea: heat cracks
in Scorched's band 2 (50°), amber growths in Icy's band 3 (40°), ice crystals higher on Icy.

**Ore filter (Core).** `Surface.Ores`: a list of item ids in pool order (copper, iron, ice, sulfur, magnetite, crystal,
mica), null = everything the type has (tests, dev opens, `World.Fuzz`, old saves). It reaches the ground on three paths,
all named: `World.Write` writes `ores` in each surface's entry (left out when null); `World.Load` reads it
(`TryGetProperty`) and passes it to `Terrain.Generate`; the `_opening` tuple and `World.Open` carry it to the new
`Surface` and its `Generate`. `Surface with { Closed = true }` keeps it. `Terrain.Generate(type, seed, ores)`:
- an absent ore's **vein** still claims its cells in `PaintVeins` and leaves them plain stone, so no other vein takes them;
- its **pockets** claim their cells and leave them plain stone too;
- an absent ore's **cave kind** grows as it would, holds its cells and moves `last`/`lastOn`, but is not set: its cells
  stay bare, so every other growth stands where it would.
Entries stay in their lists, so salts (`Veins + 2·i`, `PocketSalt + i`) never shift. The tests hold it exactly: every cell,
growth, lake and ruin as with every ore, but the absent ore's cells plain stone. Ruins' pieces (the Arc device's crystal) stay.
Mica is in the set on every dealt planet, so its growths are never skipped but for a hand-picked set in a test.

**Landing.** `LandingState.Ores`: one set per system planet, in `Planets` order, each a list of item ids in pool order,
mica included; null (not written) = not dealt yet. A ship-only construction system (`Surfaces.Here == 0`), registered
before the flights' count, deals them while null. `OresOf(planet)` reads them (empty while not dealt).
`ISurfaces.OpenPlanet()` becomes `OpenPlanet(string kind, IReadOnlyList<string>? ores)`: an open planet is returned as
today (one at most: a course closes it, `LeavePlanet`), else a new one is asked for with its seed from the world's Rng as
today. The descent passes `Planets[At].Type` and `OresOf(At)`, or null ores while not dealt (an old save loaded
mid-descent). The four test stubs and the dev's `OpenSurface` (null ores) follow.

**The deal**, from the world's Rng, integers only, nothing divided:
1. Planet 1 takes copper, iron and ice.
2. Each varying ore, in pool order, goes to one of planets 2–6 drawn by cumulative weight (that planet's type weight for
   the ore, `Below(sum)`).
3. Each of planets 2–6, in order, draws a count `OresLeast + Below(OresMost − OresLeast + 1)`, takes the larger of it and
   what it holds already (step 2 gives at most 3, so the cap of 4 holds), and fills up with ores it lacks: candidates the
   six of copper, iron, ice, sulfur, magnetite, crystal in pool order (no mica), drawn by its type's weights by cumulative
   sum, without replacement.
4. Mica goes to every planet.

No new command. Both new saved fields default to "none yet": no format bump. A descent before the deal is impossible
(Board, Launch, then `DescentTicks` 100).

## Slice 1 — client

- `RadarScreen.CardNow`: a row "Ores: …" of the items' names from `OresOf`. `RadarScreen.Looks` gets the two types' colours.
- `PlanetLooks`: per type a soil and stone tint (a `Modulate` on the ground layers) and a fog colour; the backdrop reused.
- i18n: the two types' names (`computer.radar.type.*`), the row's label.

## Slice 1 — tests

- Scenarios: a `[Theory]` over each pool ore missing — its veins, pockets and growths never appear, the others' salts
  unmoved; a descent opens `Planets[At]`'s type with `OresOf(At)`; a revisit: a new seed, the same type and ores; save and
  load on a planet opened by a real descent after the deal regrows the same ground (a hash of the grid); a guest joining
  mid-descent grows the same ground; an old save loaded with a flight at `TicksLeft` 1 opens a planet with every ore;
  planet 1's derived cave chances equal today's table; both new types pass `PlanetTypeTests.Wrong`; the re-costed way off
  planet 1 needs no varying ore (`ContentTests`); on every type, every ore it can be dealt has a source in a band the
  basic suit reaches (−40..50°) — else its points are tuned.
- Invariant (FsCheck): for any seed, planet 1 holds copper, iron, ice and mica; planets 2–6 each 2–4 dealt ores plus mica;
  every varying ore on some planet; the same seed deals the same sets. Proven to bite by skipping step 2.

## Slice 2 — three planet-stage crafts (after slice 1 is played)

Icons picked first on a pick page (one each, the searchlight's look on the helmet too).
- **Heat pack** (sulfur 2, plate 1, workbench): eaten from the hotbar in a meal slot like the stim. `Rates.Warmth`
  (degrees, `Diet.Caps` 20): 20 for 12 000 ticks (10 min), subtracted in `LowOf` as the cape's is. No effect on heat.
- **Loot magnet** (magnetite 4, coil 2, plate 1): worn on the belt (against the battery); while worn the client's `Reach`
  pulls loose stacks from `Floor.MagnetReach` 6 instead of `PickUpReach` 2.
- **Searchlight** (crystal 2, helmet lamp 1, wire 2): in the helmet mount, switched by `Run(item, On)` (a run's stack the
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

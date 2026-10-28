# The crusher and the first cost pass

- Status: accepted; built
- Date: 2026-10-28
- Builds on [smelt-chances](2026-10-28-smelt-chances.md) (byproducts, smelts of several) and
  [the-workbench](2026-10-28-the-workbench.md); amends [0054](0054-asteroids-and-the-drill.md)'s rock and
  [0057](0057-stations-one-model-for-every-crafting-machine.md)'s smelting of rock

## Context

Rock smelted two for one into silicon, so a pile of a planet's stone was an endless supply of circuits and the asteroids
were worth nothing the stone did not give. A machine cost 4–12 caught pieces; the whole first-stage ship about 500 iron,
1000 scrap and 100 silicon and glass, two hours of gathering at most. The target the team set is about five hours solo
from the wreck to the drop pod.

## Decision

- **The crusher** (`crusher`, 4×4, draws 4): a fed station of its own process, `crush`, after the Former (two frames).
  Asteroid ore → 1 iron ore + 2 gravel, a quartz one time in two; rock → 2 gravel, a quartz one time in twelve.
- **Asteroids drop asteroid ore**, a new item; rock is a planet's alone, bulk worth little. Iron ore smelts at the
  furnace, so asteroids are iron's second source after hull plates.
- **Silicon only of quartz**, at the electric smelter (heat 2); **glass only of gravel, four to one**, there too. Rock and
  silicon no longer smelt. Rock's other uses stay: stone walls, soil (2 gravel and a plank).
- **The asteroid band beats every 2 to 3 minutes, at most 3 out** (was 3–5, 2): space's only silicon comes of it.
- **Costs and times, a first pass:** parts stay conversions; what machines take of them rises two to four times; craft
  times rise by half. On the calculator's first-stage ship (every machine, 180 wall cells, the tools, two drones) the
  whole costs about 960 iron, 1930 scrap, 110 silicon and 100 glass, about 230 asteroid ore with the iron its ore gives,
  and some 70 minutes of crafting at the Former's speed: near the five hours with gathering and building.
- **Canisters of ice** at the Former (3 ice): air for spacewalks after the first landing that no longer waits on salvage.

## Consequences

- Stone walls and the canister's craft need a planet: `EverythingButAPlanetsOwnOpens…` lists them.
- The numbers are a first pass, to be tuned at the playtest; every one is in `sim/Content`.

## Rejected

- **Rock still smelting into silicon at a low yield:** any yield makes the bulk a circuit mine again.
- **One gravel an ore:** glass then needs some 380 ores a stage, four hours of the band; two halve it.

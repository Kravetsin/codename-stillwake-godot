# Fruit, not biomass

- Status: accepted; amends [0060](0060-the-farm-a-grow-capsule-is-a-fed-station.md): a crop gives fruit, its seeds come
  back from the fruit
- Date: 2026-11-12
- Built as step B1 of [food and the body](2026-11-12-food-and-the-body.md)

## Context

[Food and the body](2026-11-12-food-and-the-body.md) made eating matter, but only salvage fed a hero: rations and canned
meat out of pods and fridges. The farm grew biomass, which no one eats, and gave its seeds back as it grew. A crew with a
farm should eat from it, and the galley to come (B2) cooks what the farm grows. Fuel must not get dearer for it: the
jump burns biofuel, and the farm is its source.

## Decision

- **A crop is fruit.** A seed grows into 4 fruit, a spore into 3 glowberries, in the same 9600 ticks; nothing else
  comes out of the capsule. `ItemSpec.Returns`, the weights of seeds a crop gave back, goes: it had no other user.
- **Fruit crushes into biomass and its seed by chance.** At the crusher, 40 ticks: 1 biomass, and the crop back as a
  byproduct (`Byproducts`, a loot roll as ore's quartz): a fruit a seed one time in 5, a glowberry a spore one time in 3
  and another one time in 30. So a seed still makes 4 biomass and 0.8 seeds on average, a spore 3 biomass and 1.1
  spores: fuel and the farm's slow appetite for seeds are what they were. Biomass stays the wild drop of creatures and
  cave caps.
- **Eaten raw, it draws the same rolls.** `Eat` of a food with `Byproducts` rolls them from the world's `Rng` in the
  apply only (never in `EatProblem`, which the client calls), giving what comes or dropping it at the hero's feet. So
  eating the crop costs no seeds the crusher would have given back. Fruit lasts 12 000 ticks and gives work 100;
  a glowberry lasts 12 000 and gives calm 120.
- **No market bypass.** Fruit and glowberries are worth the biomass they crush into (20, hand-priced as grown things
  are) and never sold, as rations are: the crush-to-fuel economy keeps its one way out.

## Rejected

- **Seeds back at the capsule and fruit with none:** eating the crop would cost nothing in seeds, and a fruit crushed
  for fuel would be a second, seedless path to the same biomass.
- **A seed inside every fruit:** 4 seeds a crop, a farm that grows itself, which 0060 refused on purpose.
- **Fruit fermented directly:** one step fewer, but the crusher is where a crop's seeds come back; fermenting would
  lose them or need a second draw at the fermenter.

## Consequences

- A farm needs a crusher for fuel; the crusher opens before the capsule, so no stage is shut.
- The farm is the one loop in the production chains (seed, fruit, seed); `ContentTests` names it.
- A cooked fruit (B2) gives no seed back: cooking draws none.
- No save format change: capsules holding biomass from before keep it; a seed part-grown grows into fruit.

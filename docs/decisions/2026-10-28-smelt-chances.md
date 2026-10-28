# Byproducts by chance and smelts of several

- Status: accepted; amends the fed stations of [0057](0057-stations-one-model-for-every-crafting-machine.md) and
  [0060](0060-the-farm-a-grow-capsule-is-a-fed-station.md)
- Date: 2026-10-28
- Design: the economy rework's crusher ("The crusher", "Code this needs"), on `feat/economy`

## Context

The crusher to come is a fed station: asteroid ore into an iron ore and a gravel, and one time in two a quartz; rock
into two gravel, one time in twelve a quartz; and at the smelter four gravel into one glass. A fed station's yields
were fixed, its only draw a crop's seeds (`Returns`, copies of the input), and every smelt worked one of its input.
Both are rules of the fed-station model, not of the crusher, so they go there before any content uses them.

## Decision

- **Byproducts** (`ItemSpec.Byproducts`): loot entries (`LootEntry`, the containers' own: `Min`, `Max`, `OneIn`), each
  rolled as loot is (`Loots.Roll`) as the smelt completes, from the world's Rng, stations in machine id order. A crop's
  seeds are drawn first, then the byproducts entry by entry, so an item without byproducts draws exactly as before.
- **Room for the most:** a fed station works only while its outputs take its yields, the most seeds its `Returns` give
  and every byproduct at its `Max` (`ItemsModule.MostMade`), as 0060 decided for seeds: no draw ever overflows.
- **Smelts of several** (`ItemSpec.SmeltTakes`, 1 by default, at most a stack): a fed station gains work only while its
  input holds that many, and takes them all as the smelt completes. With fewer it rests, a furnace's fire is not lit,
  and its panel says so (`ICraftingQueries.TooFew`, `crafting.tooFew`), not that its outputs are full.
- **Drones need nothing new:** they keep a fed station's input at a stack, refilling under half of it; under one smelt's
  `SmeltTakes` too, so an input short of a smelt is always wanted, whatever the stack.
- **The item card** says what one smelt takes, "×4 →", and each byproduct with its chance, "1 in 2".

## Rejected

- **Yields with a one-in of their own**, every yield an entry: one list instead of two, but every recipe's data and
  every reader of `Yields` changes for what two items use. Byproducts reuse the loot entry and its roll.
- **Seeds as byproducts**: a crop's 0, 1 or 2 are one weighted draw; as independent rolls their odds and their place
  in the Rng's sequence would change every existing world's farm.
- **Checking room for the byproducts actually drawn**: the draw is made as the smelt completes; checking for the most
  first keeps a station from working a smelt it could not put out (0060).

## Deliberately not handled

- **Too few of an item part-way**: one taken out of the input mid-smelt keeps its work and waits, as outputs full do.
- **No byproduct of another process**: an item still has one process (the crusher's design keeps scrap chunks at the
  furnace).

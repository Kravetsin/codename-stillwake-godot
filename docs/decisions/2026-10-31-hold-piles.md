# The hold's piles

- Status: accepted
- Date: 2026-10-31
- Extends [the-scoop](2026-10-31-the-scoop.md), and changes its panel on the roof; the design is in [scoop.md](../plans/scoop.md)

## Context

The scoop started a player with nothing but the debris stream. Its hold was to hold what the last crew had gathered, and it was too dark to see anything in. The design had a mark take a pile apart, and in play that looked strange: a user wants to hit a heap of junk, not plan its removal.

## Decision

- **A pile is a machine row with no menu card and a `Strength`:** the axe's or the drill's `Hew` cracks it, in the same cracks walls keep, and at its strength it goes and its `Cost` lands in the bag. It heals if left, as a crack does. No new command or saved state. A mark on one is refused (`building.needsAxe`).
- **A blueprint now follows from a menu card,** not from not being `Fixed`, and a machine with no blueprint is refused even in creative mode (`building.unknown`). `Fixed` keeps its meaning: never taken apart, and only the lifepod.
- **The panel leaves the roof, at the user's word; the cargo pile holds its photosensitive panel,** still a world's only one, which the black market's solar panel needs.
- **Nothing is reached by piles first.** The hero starts with a screwdriver, and an axe needs the workbench and iron, so the way is: catch debris, build the trade station, sell, buy the furnace and the workbench, make the axe. No dead end: the stream never stops and the trade station is known from the start. The piles are the axe's first reward: 16 chunks, 8 plates and 6 planks, about 760 credits.
- **A pile holds its cells' build slot.** The one in the hold's throat must be broken before the throat can be walled.
- **Two emergency lamps light the hold in red, each on a bracket from the right wall,** a glowing machine row that draws nothing and so is always lit, though a flare dims it as it does any lamp. Without them the piles cannot be seen. A lamp is still taken apart by a mark, which can leave the player in the dark by their own hand.

## Deliberately not handled

- **A pile shrinking as it is hit:** cracks show the damage and it goes whole on the last hit. Stages would need more art for looks alone.
- **The floor under a pile being cut away:** it stays where it is, as every machine does.
- **On the testbed:** a pile is only ever in the starting ship, so `TestbedTests` asks only for machines a player builds, and the pod.

## Consequences

- To watch in the playtest: whether the first stretch by catching alone is too long, and whether "Needs an axe" tells a player what to do. The first ten minutes have not been played through.
- Old saves keep their wreck, with no piles and no lamps.

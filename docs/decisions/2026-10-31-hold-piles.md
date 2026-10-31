# The hold's piles

- Status: accepted
- Date: 2026-10-31
- Extends [the-scoop](2026-10-31-the-scoop.md), and changes its panel on the roof; the design is in [scoop.md](../plans/scoop.md)

## Context

The scoop started a player with nothing but the debris stream. Its hold was to hold what the last crew had gathered, enough to build the first machines by hand, and it was too dark to see anything in.

## Decision

- **A pile is a machine row with no menu card,** taken apart by the ordinary mark: its `Cost` is what it holds, its `BuildTicks` its work. A mark already keeps its progress, pays once, and goes faster with two heroes, so a pile needs no command or state of its own.
- **A blueprint now follows from a menu card,** not from not being `Fixed`, and a machine with no blueprint is refused even in creative mode (`building.unknown`). `Fixed` keeps its meaning: never taken apart, and only the lifepod.
- **The panel leaves the roof, at the user's word; the cargo pile holds its photosensitive panel,** still a world's only one, which the black market's solar panel needs.
- **Four piles hold 16 chunks, 8 plates and 6 planks,** which pay for the trade station, the furnace and the workbench with a little over, worth less than the first ring's 400 credits. A player still has to catch.
- **A pile holds its cells' build slot.** The one in the hold's throat must come out before the throat can be walled.
- **Two emergency lamps light the hold in red, each on a bracket from the right wall,** a glowing machine row that draws nothing and so is always lit, though a flare dims it as it does any lamp. Without them the piles cannot be seen. They too can be taken apart, leaving the player in the dark by their own hand.

## Deliberately not handled

- **A pile shrinking as it is worked:** it goes whole on the last tick. Stages would need saved progress for looks alone.
- **The floor under a pile being cut away:** it stays where it is, as every machine does.
- **On the testbed:** a pile is only ever in the starting ship, so `TestbedTests` asks only for machines a player builds, and the pod.

## Consequences

- The first ten minutes can now be played rather than worked out. To watch in the playtest: whether a player finds the hold, and whether the piles are read as something to take apart.
- Old saves keep their wreck, with no piles and no lamps.

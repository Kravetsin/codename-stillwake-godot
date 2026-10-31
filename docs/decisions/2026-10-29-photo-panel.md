# The photosensitive panel

- Status: accepted; built
- Date: 2026-10-29
- Amended by [first-hour-pace](2026-10-31-first-hour-pace.md): the panel is known once one is taken apart
- Amends [0041](0041-wreckage-of-many-kinds-and-circuits.md), [0057](0057-stations-one-model-for-every-crafting-machine.md)
  and [tech-graph](2026-10-30-tech-graph.md): the solar panel's cost, and how it opens

## Context

The team wants the solar panel out of the economy for now: no new panels, but the wreck's own must never be lost to a
player who takes it apart.

## Decision

- **A photosensitive panel** (`photoPanel`, a part) joins the solar panel's cost, one a panel. No craft makes it, and no
  debris, loot, smelt, growth or creature gives it: a solar panel taken apart gives it back, with the rest of its cost.
- **The wreck starts knowing the solar panel** (`Content.Research.StartKnown`), and no block of the tech tree
  ([tech-graph](2026-10-30-tech-graph.md)) opens it: the solar cell's block opens the cell alone. Bought mid-game, the
  wreck's panel taken apart early could not be built again for hours.
- **It has no price** (`Market`): no recipe gives it a worth, so the trade station never takes the one a world has.
  The testbed keeps one in the farm's crate.

## Deliberately not handled

- A panel's photosensitive panel lost (dropped in space, a dead hero's) is gone for good: we accept a world with
  fewer panels than it began with.

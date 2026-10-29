# The photosensitive panel

- Status: accepted; built
- Date: 2026-10-29
- Amends [0041](0041-wreckage-of-many-kinds-and-circuits.md) and
  [0057](0057-stations-one-model-for-every-crafting-machine.md): the solar panel's cost, and how it opens

## Context

The team wants the solar panel out of the economy for now: no new panels, but the wreck's own must never be lost to a
player who takes it apart.

## Decision

- **A photosensitive panel** (`photoPanel`, a part) joins the solar panel's cost, one a panel. No craft makes it, and no
  debris, loot, smelt, growth or creature gives it: a solar panel taken apart gives it back, with the rest of its cost.
- **The wreck starts knowing the solar panel** (`Content.Research.StartKnown`). Taking apart the wreck's only panel
  leaves the analyzer unpowered, so a panel opened by analysis could never be built again.
- The photosensitive panel is in no other cost, so it needs no analysis. The testbed keeps one in the farm's crate.

## Deliberately not handled

- A panel's photosensitive panel lost (dropped in space, a dead hero's) is gone for good: we accept a world with
  fewer panels than it began with.

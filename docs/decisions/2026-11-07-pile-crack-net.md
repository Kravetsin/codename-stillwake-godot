# A pile cracks as a wreck

- Status: accepted
- Date: 2026-11-07
- Amends [pile-hit-points](2026-11-07-pile-hit-points.md)

## Context

A pile in the hold cracked as a wall does, a fault drawn in every cell of its footprint, so a pile of 4 by 4 cells showed
sixteen small cracks in a grid. The user asked for cracks that run across it.

## Decision

- **One network over a pile's picture,** the wreck's (cracks-to-end): out of a spot of its middle chosen by its cell, running on
  with the share of its hit points lost, over the picture's own pixels and clipped to them.
- `WreckCracks` and `WreckInk` moved from `features/debris` to `game/shared`, for the debris and the building to share.

## Deliberately not handled

- A pile's blows are not placed where they land: the sim's crack is a number, and the network starts at its middle.

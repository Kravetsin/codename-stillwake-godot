# The crusher takes the arc module

- Status: accepted; supersedes, of [0100](0100-tech-branches.md), the arc module's list of stations
- Date: 2026-11-07

## Context

The arc module fitted the electric smelter, the Former and the circuit assembler (0100). The crusher, an electric fed
station like the smelter, took none: its window had no socket, and a player feeding it asteroid ore had no way to speed
it up.

## Decision

The crusher is one of the arc module's stations (the user's call): +1000 speed and +1000 draw, as the others, so it
crushes twice as fast and draws 8 instead of 4. Its window gets the socket by the same rule as the smelter's
(`Modules.For`); nothing else changes. Pinned by `ContentTests` and `CrusherTests`.

## Consequences

- Two solar panels feed a crusher with the arc; on one (5) it runs at 5/8 of its doubled speed, as a smelter does.

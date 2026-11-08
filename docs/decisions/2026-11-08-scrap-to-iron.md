# Scrap to iron: four scrap smelt into an iron at any furnace

- Status: accepted
- Date: 2026-11-08

## Context

Smelted scrap piles up: a chunk smelts into four, a supply pod holds six, and it goes only into wire, nails, floors, ladders,
lamps, cast scrap walls and one burner generator, two or so at a time. Iron is what every part past wire needs and what a
player waits on ([first-hour-pace](2026-10-31-first-hour-pace.md)). The user asked for a way to turn the pile into iron, to be
judged by playtest.

## Decision

- **Four scrap smelt into one iron at any furnace** (the user's pick of ratio and station): `scrap` gets `Yields: iron 1`,
  `SmeltTakes: 4`, `SmeltTicks: 80` (the ore's), heat 1. Three wait in the input for a fourth, as gravel does.
- **At a loss on purpose:** four scrap sell for 32 credits, an iron for 19. It is a way out for what piles up, not a cheaper
  iron; a hull plate stays the better source. The market's rule that every step pays exempts this one smelt.
- **An item smelted from several sources is worth its cheapest** (`Market`): iron stays priced by the hull plate, not by the
  four scrap. Every other item with several sources was already priced by its cheapest, so no price moved.
- Cast scrap walls give back their own scrap and hull plates their iron and scrap, so no smelt or dismantle makes a loop.

## Consequences

- From the first furnace on, scrap past what wire and walls take becomes iron, slowly: the early gates may open sooner. The
  playtest decides whether the ratio stays, goes up to five or six, or moves to the electric smelter.
- Players who feed a furnace a mixed crate now smelt its scrap too.
- A larger scrap sink in building (outer armour against meteors) is left for a design of its own, if scrap still piles up.

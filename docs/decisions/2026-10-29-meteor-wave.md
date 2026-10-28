# A meteor's blast is a wave walls soak up

- Status: accepted
- Date: 2026-10-29
- Supersedes the radius of [meteor-blast](2026-10-28-meteor-blast.md); its strike, flight and look stand

## Context

A blast broke every wall within a radius set by power against each wall's own strength. A wall stood or fell alone: iron in
front spared nothing behind it, and the same meteor tore the same disc out of any hull. The user wanted walls to dampen the
blast, so the hull's make decides how much a meteor takes, and back walls in reach to break too. Damage adding up over hits
was weighed and left for a place of many meteors.

## Decision

- **The strike stays:** a meteor whose power is below the struck wall's strength breaks nothing; the rock shatters on it.
- **A wave with a charge:** else a blast of `Charge + power × PerPower` (10 and 6 in the starting space: 22 to 52) spreads from
  the struck wall, cheapest way first (a bucket queue over whole numbers, cells in order). Reaching a cell costs `Step` 2, or
  `Diagonal` 3 aslant; a wall costs `PerStrength` 2 a point of its strength more, and is broken: what is left goes on. So
  walls soak the blast, iron (12 a cell) two and a half times as fast as scrap (6), and a strong layer spares what lies behind.
- **It goes through** open cells and breakable walls; a door, window, ladder, floor, machine or a wall that never breaks stops it.
- **Back walls** behind any cell it reached break where what is left pays for their strength, spending nothing: they are
  behind, not in the way. A room without its back wall vents to space as a breached one does.
- **Content:** `Meteors.Shower(…, Charge, PerPower, Powers)`, `Meteors.Step`, `Diagonal`, `PerStrength`. A scrap hull is torn
  7 cells along at power 2, 17 at 7; the starting wreck loses at least five wall cells to any meteor.
- **The build card says it:** a wall's card shows its leak a minute a cell, its strength against meteors and the share of the
  meteors here that break it; a back wall's its strength alone.

## Deliberately not handled

- No damage adds up: a wall stands whole or goes. A shower of many weak meteors, a later event, may break by chance instead.
- The wave slips between two walls meeting at a corner, aslant; a hull two cells thick, as built, never shows it.
- Doors, windows and machines in the blast are untouched; heroes take no harm.

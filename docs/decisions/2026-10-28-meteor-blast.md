# A meteor breaks an area

- Status: accepted
- Date: 2026-10-28
- Amends [hull-meteors](2026-10-28-hull-meteors.md): a meteor broke one wall cell, now every wall within its blast

## Context

A cell is 8 px (0104), a fifth of the hero's height. A meteor that broke one cell made a pinhole hardly seen and cheaply
mended, and the thin streak before it was hardly seen either. The user asked for a meteor that flies in as a projectile
and damages walls over an area.

## Decision

- **A blast by power against strength:** if the meteor's power reaches the struck wall's strength, every wall within
  `(power − strength + 1) × Blast` cells of it (Euclidean, compared squared: integers) breaks, each wall by its own
  strength. `Blast` is the shower's (`Content/Meteors.cs`), 2 in the starting space: scrap's hole is 3×3 at power 2 and
  about 19 cells across at 6; iron's 3×3 at 5, 7 across at 6. Walls inside the area that are stronger resist more.
- **A wall that holds stops it whole:** a struck wall stronger than the meteor's power leaves the area untouched.
- **Every broken wall is breached** (`IBuildingOps.Breach`) as one was: lost, its hole planned as itself. All are found
  before any breaks, then breached in cell order.
- **Seen as a projectile:** the warning is three seconds; the client draws a rock, bigger the stronger it is, its front
  white-hot, trailing light, embers and smoke from up the drift's side onto the wall; at the strike, a shock ring as wide as
  what broke and hot chunks flung on its way out of every wall gone.
- The dev panel's `MeteorHit` still breaks the one wall it names.

## Deliberately not handled

- The blast passes through what stands between: no line of sight, a door or machine inside the area is untouched, and it
  breaks floors and inner walls in reach as it breaks the hull.
- No knock-back or harm to heroes; nothing thrown by the blast lands as items.

# A hit on a pile is seen and heard

- Status: accepted
- Date: 2026-11-07
- Amends [pile-hit-points](2026-11-07-pile-hit-points.md)

## Context

A pile in the hold took its hits but showed none: a tool's rays pass through a pile, which stops no body, and only a cell
that stops them or a growth was touched, so the sim was told and nothing was felt. Without the scanner a player could not
tell a pile was there to break.

## Decision

- **A ray touches a pile** it passes (`Shared.Piles.At`), as it touches a growth: the drill's edge and every fan of rays.
- **A pile is struck as a wreck:** the wreck's sparks, glint and flash (`Impact.Wreck`), the wreck's ring under the tool
  (`Sound.WreckHitOf`: the chop, the spear's point, the sledgehammer's blow), by the weapon's damage, the breaking blow throwing
  the whole look.
- The plasma cutter, which never hits a pile, shows nothing on one.

## Deliberately not handled

- A wreck's sparks in the hold float as they do in space, as the user asked for the same look; they do not fall.

# A meteor's hole is not planned

- Status: accepted
- Date: 2026-10-29
- Supersedes the planned hole of [hull-meteors](2026-10-28-hull-meteors.md) and [meteor-blast](2026-10-28-meteor-blast.md)

## Context

A wall a meteor broke was planned again as a blueprint of itself, so repair was the screwdriver on the plan. The user
saw the catch: a hull of costly walls, iron now and better later, leaves a hole the player may not have the material for,
and would rather patch it for now with scrap. A ghost of the old wall is in the way of that.

## Decision

- **A meteor only breaks walls:** each wall it breaks is cleared through the grid's operations, lost, refunding nothing,
  and nothing is planned in its place. The player plans the patch they can afford, as for any hole.
- Hazards depends on `Grid` alone again; Building's `Breach` operation, which had no other user, is gone. A mark on a
  broken wall is still dropped by Building in derivation.

## Deliberately not handled

- Nothing marks where a hole was once its wall is gone: the room venting, its air view and the meteor's strike show it.

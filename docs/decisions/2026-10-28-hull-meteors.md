# Meteors from the start

- Status: accepted; a meteor breaks an area since [meteor-blast](2026-10-28-meteor-blast.md), its hole unplanned since [holes-not-planned](2026-10-29-holes-not-planned.md)
- Date: 2026-10-28
- Changes the concept's "the starting space is calm"; meteors by hand stay as the dev panel's

## Context

The economy plan (`feat/economy`) found that once the ship stands, nothing costs materials again: automation has nothing
to feed. Meteors were meant for later places, rings round planets. Moved to the starting space, they make repair a
steady sink of scrap and plates, and give a material's strength its meaning for the hull.

## Decision

- **A shower belongs to a place:** `Surfaces.Kind.Meteors` (`Content/Meteors.cs`), the ship's `StartingSpace`, a planet's
  none. A meteor every 4–6 minutes, drawn evenly; its power 2–7 by weight, mostly weak (1–6 before meteor-blast).
- **From the drift's side:** the target is, among the rows of what is built inside the zone, the first thing met
  walking left from the right, kept only when it is a breakable wall (a door, window, machine or ladder first: no
  candidate). One drawn evenly among the candidates.
- **Seen before it strikes:** the target and power are drawn `Warning` ticks (2 s) before the hit and kept in the
  Hazards slice, so every peer, one joining too, draws the same streak.
- **Power against strength:** at the hit, a wall still standing there breaks iff power ≥ its material's `Strength`:
  scrap (2) seven times in ten, iron (5) three in twenty, concrete (9) never. A hit breaks or does nothing.
- **The hole stays planned:** `IBuildingOps.Breach` clears the wall and plans a blueprint of the same wall in it,
  asking no knowledge (a repair of what stood), unless a blueprint already holds that slot. Repair is the screwdriver
  paid part by part as any blueprint. The broken material is lost. The dev panel's `MeteorHit` breaches too, always.
- **Opted into by the world:** `Layout.Meteors`, set on the new game's wreck, saved as Hazards' `Calm` (its opposite,
  written only when true), so test and dev maps are calm and old saves get meteors without a format bump.

## Deliberately not handled

- Meteors from the left, above or below; through open doors; at machines, doors, windows, solar panels or heroes.
- Damage that adds up over hits; a refund of the broken wall; showers changing with where the ship goes.
- A client joining in the moment between the hit and its next frame misses the impact's look.
- Instant building does not build the ghost at once: it stays a blueprint.

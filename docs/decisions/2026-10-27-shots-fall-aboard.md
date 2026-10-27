# Shots fall aboard

- Status: accepted; built. Amends [shots are the sim's](2026-10-26-shots-are-the-sims.md) and [the railgun](2026-10-27-the-railgun.md):
  a shot no longer flies straight on the ship's surface
- Date: 2026-10-27

## Context

A shot's gravity was its surface's: the ship's floats, so a nail flew level everywhere on it, in its rooms too, where the hero
walks on the floor and a dropped stack falls. The player's words: since the hero walks rather than flies, the shot must be
pulled down too, "maybe harder than on a planet, since gravity on the ship is stronger".

## Decision

- **A shot falls by the cell it starts each tick in** (`ShotFlight.Gravity(kind, projectile, gun, afloat)`): none where a hero
  would float, `Rooms.Afloat` (open space on the ship's surface, a room open to it); elsewhere `Creatures.Gravity` times its
  weight times its gun's `Fall`, scaled by the surface's gravity. The ship has no scale, so aboard it falls by the whole: 420 a
  tick for the nailer's nail, where the first planet gives 294; the railgun's tenth, 42.
- **One rule for the sim's shot and a guest's copy:** both step by `CreaturesModule.StepOf`'s path, which asks the rooms, so
  the copy still ends where the sim's does. `Creatures` now depends on `Rooms` for the query.
- Nothing is saved anew; the rooms are derived, the same on every peer.

## Consequences

- A nailer fired level across a room drops some ten cells over twenty: aim up to reach the far side. The railgun barely drops.
- A shot leaving the hull into space flies on level from the speed it fell with; one coming in from space starts to fall.
- A creature's arc has no weight and never falls.

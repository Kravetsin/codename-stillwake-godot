# Debris tumbles in the sim, and is hit by its turned shape

- Status: accepted
- Date: 2026-11-23

## Context

Debris moves to Godot physics on the host (`docs/plans/debris-physics.md`, step 1 of 4). Small pieces tumbled only in the
client's drawing and wrecks rocked; the sim met every wreck by an upright box, so a turned picture and its hit box disagreed.
The user rejected having to step into a box's corner to hit, and chose that every piece tumbles, wrecks too.

## Decision

- **Angle and spin are the sim's:** each piece keeps a saved `Angle` and `Spin` in 65536ths of a turn; each tick its angle
  turns by its spin on every peer. A spawn's angle and spin come from a splitmix mix of its id (`TumbleOf`), never the world's
  Rng: pieces up to `TumbleSide` (2 cells) spin up to `TumbleMax` (160, about 0.3 rad/s, the prototype's), longer ones slower
  in proportion, never under `TumbleMin` (24, about 0.05 rad/s). A cargo hold turns once in some 80 s.
- **One turned shape:** `TurnedBox` (`Shape(piece)`), integer maths on Core's sine table, is what every hit, hew, shot, burst,
  aim, edge, scan and harpoon rod meets. Nets, grabs, salvage drones and despawn still go by the middle cell.
- **Creatures on a wreck stand on it:** clingers, sentries and nests ride their perch turned with the wreck, drawn turned about
  their feet, their box turned the same way (`StanceOf`, `BoxOf`). Their own moves still start along the world's axes.
- **Saves:** format 73; the migration gives old pieces their id's angle and spin.

## Deliberately not handled

- **The harpoon rod stuck in a piece** keeps a world-axis offset and does not turn with it: the rope of step 2 replaces it.
- **A sentry leaving its wreck** snaps upright at once.
- **The aim tests the box at the last tick's pose,** not the one drawn between ticks, as before.

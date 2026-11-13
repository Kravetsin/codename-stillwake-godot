# One answer to what a hit strikes

- Status: accepted; built; how much each hit takes, and machines struck by tools and shots, in [one-damage-rule](2026-11-10-one-damage-rule.md)
- Date: 2026-11-10

## Context

[unified-damage](2026-11-09-unified-damage.md) gave every hit one formula, a strength, a crack and a surplus carried on,
but not one target. Bursts, tool rays, carries, shots, the crack sweep and six client views each decided for themselves
what stood at a cell, which cell kept its crack and how strong it was. They disagreed:

- A door's crack was never drawn.
- A station struck by a tool showed nothing.
- A machine had two numbers, `Hp` for tools on a pile and `HitPoints` for bursts, in different units.

## Decision

1. **`BuildingModule.TargetAt(cell)`** (`IBuildingQueries`, `sim/Building/Targets.cs`) is the only place a hit's target
   is chosen. It returns a `HitTarget(Kind, Key, Strength, Wreck)`, or none for open air, a grate and what is off the grid.
   - `Kind` is a wall, else a machine, else a built piece, else a plant in an open cell. A plant a wall holds is the wall's.
   - `Key` is the cell its crack is kept at: a wall's own cell, a machine's top-left, a piece's head, a plant's own cell.
   - `Strength` is null for what nothing breaks: bedrock, lava, the lifepod.
   - `Wreck` marks a pile, whose strength is in a weapon's `Damage` as a wreck's; everything else is in tenths.
2. **Every hitter and view reads it:**
   - in the sim: bursts, `Hew`, carries, a shot's `ShotTakes`, `ShotKey`, `Surplus` and `Hit`, the crack sweep, and
     `building.needsAxe`;
   - in the client: sparks, flash, sound, cracks and touches. `game/shared/logic/Piles.cs` is gone.
3. **What stays with each hitter:**
   - whether a ray stops (a wall, or `Grid.Blocks`);
   - the fog;
   - for bursts, a plant counts as open air and a grate stops them.
4. **A machine has one number:** `MachineSpec.Strength` (`Fixed` → null, else `Hp ?? Damage.MachineHp`), with `Wreck`.
   `HitPoints` is gone.

No save step: no saved field changed, and cracks keep their keys.

## Accepted findings

- A burst reads a pile's strength as tenths (12): any charge or meteor passes a pile almost free. Piles are cleared at
  the start, long before the first charge, which is found on planets.
- A plant overlapping a machine's cell is the machine's, never struck by a ray. No map or player places one so.

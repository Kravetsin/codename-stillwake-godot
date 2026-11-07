# Machines stand on a floor

- Status: accepted
- Date: 2026-11-07

## Context

A machine asked only that its own cells be free, so a tank, a furnace or an electrolyser could be built hanging in a room's
air or out in space. Only the drop pod had footing, a launch platform's run. The user asked for stations to be built on a
floor or a platform alone, the launch platform kept for the pod, and space allowed by the same rule.

## Decision

- **Standing machines** (`Machines.Stands`): every machine but the hung ones (`MachineTrait.Hangs`: the lamp, the arc lamp,
  the emergency lamp, the vent) and those with a rule of their own (the debris net, which `Catches` on its hull; the drop
  pod, which `Lands`). The solar panel and the radar antenna stand: they go on the hull's top.
- **Footing** (`Structures.Footing`, `IGridQueries.Footing`): a solid structure that is no doorway, so a wall (any
  material, the ground too), a floor or a grate. Not a hatch, which a machine would block, nor a launch platform, the pod's.
- A standing machine is planned only with footing under every cell of the row below its footprint, built or a blueprint of
  one (`building.noFloor`); building instantly, built only. A whole row, as the pod's: half over an edge reads as hanging.
- Its last tick of work waits, refused `building.noFloor`, till the footing planned under it is built, as a wall's waits on
  a hero.
- What a standing machine stands on is not marked for taking apart, and a mark made before the machine is not worked
  (`building.holdsMachine`).
- Checked in Building, not in the grid's `PlacementProblem`: the wreck's piles, the lifepod and a landing pod are placed by
  the world, not the player, and keep the rules they had.

## Deliberately not handled

- Machines already built in the air, in a save or on the wreck, stay where they are: the rule is for what is built next,
  so no save step.
- A meteor breaking a machine's footing leaves the machine standing on nothing. Falling machines are a mechanic of their
  own; no player action but waiting for a meteor reaches it.
- Tools breaking a footing: they break only on a planet, where no machine is built.
- A machine on another machine (crates stacked): machines are no footing.
- Hung machines may still be hung anywhere, a wall or ceiling beside them or not.

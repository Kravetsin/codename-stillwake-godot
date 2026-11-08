# Leaks a minute

- Status: accepted; supersedes the leaks of [leaks-tripled](2026-11-06-leaks-tripled.md) and the pod's rate of
  [scarce-oxygen](2026-11-07-scarce-oxygen.md)
- Date: 2026-11-08

## Context

After the industrial-oxygen playtest the user set the leaks of the walls by what a player reads, units a minute a cell:
scrap 3, iron 2, alloy 1.8, glass 1.5. Glass no longer seals five times better than scrap, only twice.

## Decision

- **Leaks a period of ten minutes:** scrap 30, iron 20, alloy 18, glass 15. Those the user did not name, by where they
  stood against scrap: mica and hide 15, as glass; soil 120; stone, plaster and the veins 90; bedrock and lava 36;
  `Air.DoorLeak` 20.
- **The pod holds room A and its first furnace:** `Air.PodRate` = 6000, room A's leak of 3840 and a lit fire's
  `Furnace.Reserve` of 1800. At 4000, A alone, a furnace in A pulled its air back past its own cell and went out, and
  the first scrap could not be smelted.

## Consequences

- A vaporizer holds three times the scrap cells it did, some 400. The shortage comes back through canister scarcity,
  fires' reserves and stacks of five, not leaks.
- The pod airs the lock with its doors open and small second rooms whole, as it did before leaks were tripled.
- Tests sized for the old leaks were refitted; the fire's shrinking of a room at its limit is shown in the deep room
  (`AirTests`), the generator's room no longer reaching its limit.

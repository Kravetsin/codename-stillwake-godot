# Building, pipes and cables pay from every crate on the ship

- Status: accepted; extends [craft-from-crates](2026-11-05-craft-from-crates.md) to everything the build menu pays for
- Date: 2026-11-05

## Context

Crafting took from every crate on the ship ([craft-from-crates](2026-11-05-craft-from-crates.md)); building still took from
the hand, so a wall planned beside a full crate meant a walk to fetch its plates. The user asked to build from the crates too.

## Decision

- **The stock pays for everything the build menu prices**, the same stock as a craft's (`TakeStock`, own slots, then
  every crate on the surface by id, however far):
  - a blueprint's part, as its tick of work starts: the first item in cost order the plan lacks and the stock holds (0063);
  - a pipe span and a valve (`LayPipe`, `MakeValve`), and a cable (`Lay`).
- **What a cut gives back goes into the stock** (`GiveStock`): a cut pipe, an unmade valve, a cut cable, own slots first,
  the rest into each crate; refused `items.full` only when none of it has room.
- **The build menu, its counts and a plan's tip weigh the stock** (`Ui.Carried` reads `StockTotals`).

## Rejected

- **A taken-apart thing's refund into the crates:** it keeps going to the hand and falling at the plan (`GiveOrDrop`, 0008):
  the pieces are where it stood.
- **A cancelled blueprint's delivered items into the crates:** they fall where it was, as before.

## Consequences

- Carrying building material is never needed aboard; drones (0062) still carry outputs into crates, which now feed the build.
- Away from the ship a surface's crates pay there, and pipes and cables are not laid at all (0110).

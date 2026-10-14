# Hazards

Meteors breaking the hull. For now struck by hand, from the dev panel, to try a breach and its
repair: random meteors arrive with locations, rings around planets whose rocks hit hard enough to
break some hulls and not others (see `docs/concept.md`). Ported from
`packages/sim/src/modules/hazards`; its rules hold here unchanged.

- **Depends on:** `Grid`, through `IGridQueries` (reads walls) and `IGridOps` (clears cells).
- **State:** none (`HazardsState` is empty).

## Rules

- `MeteorHit(cell)` breaks the wall at `cell`: its structure and the conduit through it are cleared,
  with no refund. The hole is an opening of its room: `atmosphere` vents it and drains its vents'
  networks until a valve cuts them off or a wall is built on it. A deconstruction mark on the broken
  wall is dropped by `building`.

## API

- **Commands:** `MeteorHit(cell)` — refused with `hazards.noWall` unless `cell` holds a wall.
- **Fuzz:** a hit one turn in 64, mostly at a standing wall, else at any cell or one off the grid. It
  reads the grid's walls when it draws, where the TypeScript generator, which cannot see the grid,
  aims at the wreck's walls as drawn.

## Invariants

- A meteor only ever turns a wall into an empty cell; it never adds anything.

## Deliberately not handled

- Meteors hit walls only, never doors, windows or machines.
- No meteor falls by itself, and a hit ignores the wall's `strength`: both come with locations, where
  a meteor's power set against a material's strength decides whether it breaks.

## Tests not yet ported

`hazards.test.ts` checks what a breach does to the air, waiting for `atmosphere`: the broken room
vents and drains its network by `Air.VentDrain` a tick; it stops draining once a valve cuts the vent
off; it fills again once a wall planned on the hole is built (the wall built and the room closed are
tested already, with `building`).

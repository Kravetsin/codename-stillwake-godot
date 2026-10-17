# 0048 — A big world, a build zone inside it

- Status: accepted
- Date: 2026-10-17

## Context

Every world was the wreck's own map, 192×96 cells: the ship could never grow past it, and the grid's
edge, a wall, was a few dozen cells from the hull. The foundations plan (F-1 to F-5) made what is
built, not the area, set the cost of a save, a hash, rooms and the air, so a much bigger world is now
cheap. The ship needs room to grow, and space round it to fly out into.

## Decision

- **One size for every new world:** `Content.World`, 2048×1024 cells, with a **build zone** of
  1024×512 in its very middle. Nothing is placed outside the zone: `Grid.PlacementProblem` refuses
  `grid.outsideZone` for any cell of a footprint outside it, and the placers (blueprints, the red
  ghost) read that refusal as they read every other.
- **The zone is saved with the grid** (`GridState.Zone`, a `Box`) and offered as `IGridQueries.Zone`.
  A map carries it as `Layout.Zone`; a map that sets none, as every test and dev map does, has the
  whole map as its zone, so nothing there is refused and nothing else changes.
- **The wreck is centred:** `Layout.InWorld()` pads a parsed map with space to the world's size, its
  under-map and cables moved with it, and sets the world's zone. `Registry.Wreck`, what the title's new
  world is made from, is the wreck so padded; `Registry.WreckMap` keeps the wreck as drawn for the dev
  runs and tests whose cells are the drawing's.
- **Debris still spawns by the hull,** its band and lead found from the hull's bounds, now walked only
  within the grid's `Built` box: the same pieces as before for the same world.
- **Old saves move in (format 10):** the migration centres the save's grid in the world by the offset
  its own size gives, (928, 464) for the wreck, and moves every cell and every position in thousandths
  of a cell by it: the five layers, machines, floor stacks, cables, plans, aired cells, the crew's
  spawn, pod and heroes, and every piece of debris with its reel. Rooms, their volumes and the air are
  as they were.

## Consequences

- A ship can grow to 1024×512 cells, and the edge of what may be built is far from the grid's edge.
- Changing `Content.World` later does not resize worlds already made: their size is saved.
- Leaving the zone costs the suit's oxygen past a margin round it, the hero reporting its cell and the
  frost showing the cold ([0050](0050-the-edge-is-made-of-oxygen.md)); the build frame (slice c) comes next.
- Client views that still walk whole layers (the grid's sync, the light's compare) cost more on the
  big world, some 2 ms on a change; bounding them to `Built` is the plan's slice 0.
- A cell off the old grid, which only the fuzz ever reports for a hero, becomes -1 in a migrated save.

# Hazards

## Files

| File | Purpose |
| --- | --- |
| `Hazards.cs` | Meteors breaking the hull: the place's shower falling by itself, and struck by hand. |

Meteors breaking the hull ([hull-meteors](../../docs/decisions/2026-10-28-hull-meteors.md), [meteor-blast](../../docs/decisions/2026-10-28-meteor-blast.md)): a place's shower
(`Surfaces.Kind.Meteors`, `Content/Meteors.cs`) falls by itself from the start, a steady cost of repair; the dev panel
strikes one by hand, to try a breach and its repair. Ported from `packages/sim/src/modules/hazards`; its rules hold
here unchanged.

- **Depends on:** `Grid`, through `IGridQueries` (reads walls, what is built); `Building`, through `IBuildingOps`
  (`Breach`: the wall cleared, its hole planned). Registered after Building.
- **State** (`HazardsState`, all saved, each written only when set): `Calm`, a world whose map asked for no meteors
  (`Layout.Meteors` false: tests, dev worlds); `NextIn`, ticks till the next is seen coming, 0 before one is drawn;
  `Incoming`, the meteor seen coming (`Meteor(Cell, Power, Left)`).

## Rules

- **A shower** falls where the surface's kind has one (the ship's `StartingSpace`; a planet none) and the world is not
  `Calm`. A system in construction, as a command would apply: building and the rooms see the hole on its tick.
- **Every `EveryMin` to `EveryMax` ticks**, drawn evenly from the world's `Rng`, a meteor is seen coming: its power by
  the shower's weights (2–7, mostly weak), its target one of the walls open to the drift, drawn evenly. None open: the
  interval is drawn again.
- **Open to the drift** (debris drifts right to left): in each row of what is built, inside the zone, the first thing
  met walking left from the right, when it is a wall that breaks (`HitProblem` none). A door, window, ladder or machine
  met first shields its row.
- **`Warning` ticks later it strikes** (3 s): if a wall still stands there and power ≥ its material's `Strength` (scrap 2,
  iron 5), every wall nearer than its `Reach`, `Hole + (power − its strength) × Blast` cells, breaks, each by its own
  strength (Euclidean, compared squared); all found first, then breached in cell order. A struck wall that holds stops it
  whole. Then the next interval is drawn. `Farthest(power, shower)` bounds the area, for the client too.
- **A wall broken** (`Breach`) is lost, refunding nothing, its pipes left whole (0109), and its hole planned as a
  blueprint of the same wall, known or not: repair is the screwdriver, paid part by part. The hole is an opening of its
  room: `atmosphere` vents it and drains its vents' networks until a valve cuts them off or the wall is built again. A
  deconstruction mark on it is dropped by `building`.
- `MeteorHit(cell)` (dev) breaches the one wall at `cell` whatever its strength.

## API

- **Queries:** `Incoming`, the meteor seen coming, for its flight; `Reach` and `Farthest`, how far a blast breaks a wall and any.
- **Commands:** `MeteorHit(cell)` — refused with `hazards.noWall` unless `cell` holds a wall, and `hazards.neverBreaks` for one that
  never breaks (bedrock, lava) or out of the grid's zone (a planet's fog).
- **Fuzz:** a hit one turn in 64, mostly at a standing wall, else at any cell or one off the grid. It
  reads the grid's walls when it draws, where the TypeScript generator, which cannot see the grid,
  aims at the wreck's walls as drawn.

## Invariants

- A meteor only ever turns a wall into an empty cell, planned as the wall it was; it adds nothing else.

## Deliberately not handled

- Meteors hit walls only, never doors, windows, machines, solar panels or heroes; they come only from the right, never
  through an open door.
- A hit breaks or does nothing: no damage adds up. A blast ignores what stands between, breaking floors and inner walls in
  reach, and leaves doors and machines in it whole. Showers do not change with where the ship goes yet.
- A client joining between a strike and its next frame misses the strike's look.

## Tests not yet ported

Every test of `hazards.test.ts` is ported, with `atmosphere`, `building` and `items`.

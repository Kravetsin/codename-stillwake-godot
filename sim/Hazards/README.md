# Hazards

## Files

| File | Purpose |
| --- | --- |
| `Hazards.cs` | Meteors breaking the hull: the place's shower falling by itself, and struck by hand. |

Meteors breaking the hull ([meteor-wave](../../docs/decisions/2026-10-29-meteor-wave.md), [hull-meteors](../../docs/decisions/2026-10-28-hull-meteors.md), [meteor-blast](../../docs/decisions/2026-10-28-meteor-blast.md),
[holes-not-planned](../../docs/decisions/2026-10-29-holes-not-planned.md), [weak-meteors-till-landing](../../docs/decisions/2026-10-29-weak-meteors-till-landing.md)): a place's shower
(`Surfaces.Kind.Meteors`, `Content/Meteors.cs`) falls by itself, a steady cost of repair; the dev panel
strikes one by hand, to try a breach and its repair. Ported from `packages/sim/src/modules/hazards`; its rules hold
here unchanged.

- **Depends on:** `Grid`, through `IGridQueries` (reads walls, what is built) and `IGridOps` (clears them).
- **State** (`HazardsState`, all saved, each written only when set): `Calm`, a world whose map asked for no meteors
  (`Layout.Meteors` false: tests, dev worlds); `NextIn`, ticks till the next is seen coming, 0 before one is drawn;
  `Incoming`, the meteor seen coming (`Meteor(Cell, Power, Left)`).

## Rules

- **A shower** falls where the surface's kind has one and the world is not `Calm`: `Shower`, the kind's
  `MeteorsBeforeLanding` (the ship's `BeforeLanding`) till a planet has been opened (`ISurfaces.Landed`), its `Meteors`
  (`StartingSpace`) after; a planet has none. A system in construction, as a command would apply: building and the rooms see the hole on its tick.
- **Every `EveryMin` to `EveryMax` ticks**, drawn evenly from the world's `Rng`, the first at `First` when the shower
  gives one (a quarter of an hour), a meteor is seen coming: its power by
  the shower's weights (2–7, mostly weak), its target one of the walls open to the drift, drawn evenly. None open: the
  interval is drawn again.
- **Open to the drift** (debris drifts right to left): in each row of what is built, inside the zone, the first thing
  met walking left from the right, when it is a wall that breaks (`HitProblem` none). A door, window, ladder or machine
  met first shields its row.
- **`Warning` ticks later it strikes** (3 s): if a wall still stands there and power ≥ its material's `Armour` (wall-roles: scrap 2,
  iron 5), a blast of `Charge(power)` spreads from it as a wave, cheapest way first: a cell costs `Meteors.Step` (aslant
  `Diagonal`), a wall `PerStrength` a point of its armour more, and breaks; what is left goes on, so walls soak it. It goes
  through open cells and breakable walls; anything else stops it. A back wall behind a cell reached breaks where what is left
  pays for its armour, spending nothing. All found, then broken in cell order. A struck wall that holds stops it whole.
  Then the next interval is drawn. `Farthest(power, shower)` bounds the area, for the client too.
- **A wall broken** is cleared, lost, refunding nothing, its pipes left whole (0109), and nothing is planned in its place:
  the player patches it with what they have. The hole is an opening of its room: `atmosphere` vents it and drains its
  vents' networks until a valve cuts them off or a wall is built again. A deconstruction mark on it is dropped by `building`.
- `MeteorHit(cell)` (dev) breaks the one wall at `cell` whatever its armour.

## API

- **Queries:** `Incoming`, the meteor seen coming, for its flight; `Shower`, the shower falling now; `Charge` and `Farthest`, a blast's charge and bound.
- **Commands:** `MeteorHit(cell)` — refused with `hazards.noWall` unless `cell` holds a wall, and `hazards.neverBreaks` for one that
  never breaks (bedrock, lava) or out of the grid's zone (a planet's fog).
- **Fuzz:** a hit one turn in 64, mostly at a standing wall, else at any cell or one off the grid. It
  reads the grid's walls when it draws, where the TypeScript generator, which cannot see the grid,
  aims at the wreck's walls as drawn.

## Invariants

- A meteor only ever turns a wall into an empty cell and takes back walls away; it adds nothing, a plan neither.

## Deliberately not handled

- Meteors hit walls only, never doors, windows, machines, solar panels or heroes; they come only from the right, never
  through an open door.
- A hit breaks or does nothing: no damage adds up. A blast slips aslant between two walls meeting at a corner, and leaves
  doors and machines it meets whole. Showers do not change with where the ship goes yet.
- A client joining between a strike and its next frame misses the strike's look.

## Tests not yet ported

Every test of `hazards.test.ts` is ported, with `atmosphere`, `building` and `items`.

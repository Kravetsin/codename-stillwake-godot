# Hazards

## Files

| File | Purpose |
| --- | --- |
| `Flares.cs` | Magnetic flares breaking machines: drawn, counted down, striking; `FlareNow` by hand. |
| `Hazards.cs` | Meteors breaking the hull: the place's shower falling by itself, and struck by hand. |

Meteors breaking the hull ([meteor-wave](../../docs/decisions/2026-10-29-meteor-wave.md), [hull-meteors](../../docs/decisions/2026-10-28-hull-meteors.md), [meteor-blast](../../docs/decisions/2026-10-28-meteor-blast.md),
[holes-not-planned](../../docs/decisions/2026-10-29-holes-not-planned.md), [weak-meteors-till-landing](../../docs/decisions/2026-10-29-weak-meteors-till-landing.md)): a place's shower
(`Surfaces.Kind.Meteors`, `Content/Meteors.cs`) falls by itself, a steady cost of repair; the dev panel
strikes one by hand, to try a breach and its repair. Ported from `packages/sim/src/modules/hazards`; its rules hold
here unchanged. Magnetic flares break machines ([magnetic-flare](../../docs/decisions/2026-10-30-magnetic-flare.md),
`Surfaces.Kind.Flares`, `Content/MagneticFlares.cs`).

- **Depends on:** `Grid`, through `IGridQueries` (reads walls, what is built) and `IGridOps` (clears them); `Power`
  (`IsBroken`, `NetworkOf`; `Break`); `Research` (`Knows`, to arm the flares).
- **State** (`HazardsState`, all saved, each written only when set): `Calm`, a world whose map asked for no hazards
  (`Layout.Meteors` false: tests, dev worlds); `NextIn`, ticks till the next is seen coming, 0 before one is drawn;
  `Incoming`, the meteor seen coming (`Meteor(Cell, Power, Left)`); `FlareIn`, ticks till the flare drawn strikes, 0 for none.

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
- **A flare**, each tick in construction after the shower: a `FlareIn` above 0 counts down, armed or not, and strikes on
  reaching 0. Then, with none drawn, where the kind has `Flares`, the world is not `Calm` and `Knows("generator")`, the next
  is drawn evenly in `EveryMin..EveryMax`, not counted down that tick. Its last `Warning` ticks are its warning.
- **The strike** takes the powered machines a cable ends on now (not last derivation's networks) and not broken, in id
  order (`n`), draws a percent evenly in `PercentMin..PercentMax`, then `ceil(n × percent / 100)` picks, each
  `Rng.Below(left)` and taken out, and `Break`s them. None: nothing drawn.
- `FlareNow(ticks)` (dev) sets `FlareIn`, replacing a flare drawn and not yet coming, in any world; a place with no
  `Flares` strikes with `StartingSpace`'s.

## API

- **Queries:** `Incoming`, the meteor seen coming, for its flight; `Shower`, the shower falling now; `Charge` and `Farthest`,
  a blast's charge and bound; `FlareRules`; `FlareComing` and `FlareLeft`, the warning and its ticks left.
- **Commands:** `MeteorHit(cell)` — refused with `hazards.noWall` unless `cell` holds a wall, and `hazards.neverBreaks` for one that
  never breaks (bedrock, lava) or out of the grid's zone (a planet's fog). `FlareNow(ticks)` — `hazards.badTicks` below 1,
  `hazards.flareComing` during a warning.
- **Fuzz:** a hit one turn in 64, mostly at a standing wall, else at any cell or one off the grid. It
  reads the grid's walls when it draws, where the TypeScript generator, which cannot see the grid,
  aims at the wreck's walls as drawn. `FlareNow(1)` one turn in 64; `MagneticFlareTests` loads a world struck and repaired every tick.

## Invariants

- A meteor only ever turns a wall into an empty cell and takes back walls away; it adds nothing, a plan neither.
- A strike breaks between `ceil(n × PercentMin / 100)` and `ceil(n × PercentMax / 100)` of its `n` candidates.

## Deliberately not handled

- Meteors hit walls only, never doors, windows, machines, solar panels or heroes; they come only from the right, never
  through an open door.
- A hit breaks or does nothing: no damage adds up. A blast slips aslant between two walls meeting at a corner, and leaves
  doors and machines it meets whole. Showers do not change with where the ship goes yet.
- A client joining between a strike and its next frame misses the strike's look.
- Nothing shields a machine from a flare yet; fuses and shielded walls will shrink the candidates. No grace before the
  first flare, and a machine just repaired may break in the next.

## Tests not yet ported

Every test of `hazards.test.ts` is ported, with `atmosphere`, `building` and `items`.

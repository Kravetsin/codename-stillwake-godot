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

- **Depends on:** `Grid`, through `IGridQueries` (reads walls, what is built); `Power` (`IsBroken`, `NetworkOf`; `Break`);
  `Research` (`Knows`, to arm the flares); `Building`, through `IBuildingOps` (`Burst`, what a meteor breaks, unified-damage).
- **State** (`HazardsState`, all saved, each written only when set): `Calm`, no meteors nor flares drawn: a world whose map
  asked for no hazards (`Layout.Meteors` false: tests, dev worlds), or one calmed from the dev panel (`SetCalm`); `NextIn`, ticks till the next is seen coming, 0 before one is drawn;
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
  met first shields its row; a debris net (`Catches`) does not: the aim passes it to the wall it hangs on. A vessel's
  box is passed over: no vessel shields the ship (generated-vessels).
- **`Warning` ticks later it strikes** (3 s), if a breakable wall still stands there: Building's `Burst` from it of
  `Charge(power)` (`PerPower` 170 a point: 340 to 1190), within the shower's `Reach` (4), `Spoils.Lost`. The one burst rule
  (sim/Building README): a cell costs `Damage.Step` and its strength left, in full (no player's
  share); what it pays for breaks and is lost, what it reaches unpaid cracks, the wall struck first. A scrap hull two
  thick (400 a cell): 2 cracks it, 3–4 hole its outer cell, 5 and up breach it; iron holes at 3, breaches at 6; a second within the heal time on a cracked
  patch breaches too. A back wall behind a cell entered breaks where what is left pays for it. Then the next interval is drawn.
- **What breaks** is lost, refunding nothing, its pipes left whole (0109), nothing planned in its place: the player patches
  it with what they have. A machine spills what it held; a debris net, struck or with its wall, lays its cost at its cell
  for anyone, once, and `debris` drops what it held. A hole is an opening of its room: `atmosphere` vents it and drains its
  vents' networks until a valve cuts them off or a wall is built again. A deconstruction mark on it is dropped by `building`.
- `MeteorHit(cell)` (dev) bursts with the wall's full `Strength`, reach 0 (damage-scale): that wall breaks, its back wall
  stands but behind a deep crack. `MeteorHit(cell, power)` strikes as the shower's meteor would.
- **A flare**, each tick in construction after the shower: a `FlareIn` above 0 counts down, armed or not, and strikes on
  reaching 0. Then, with none drawn, where the kind has `Flares`, the world is not `Calm` and `Knows("generator")`, the next
  is drawn evenly in `EveryMin..EveryMax`, not counted down that tick. Its last `Warning` ticks are its warning.
- **The strike** takes the powered machines a cable ends on now (not last derivation's networks) and not broken, in id
  order (`n`), draws a percent evenly in `PercentMin..PercentMax`, then `ceil(n × percent / 100)` picks, each
  `Rng.Below(left)` and taken out, and `Break`s them. None: nothing drawn.
- `FlareNow(ticks)` (dev) sets `FlareIn`, replacing a flare drawn and not yet coming, in any world; a place with no
  `Flares` strikes with `StartingSpace`'s.
- `SetCalm(on)` (dev) sets `Calm` and drops the meteor seen coming, the flare drawn (its warning too) and both countdowns, on or
  off: calmed, nothing falls or strikes; stirred, they start over as in a new world, the first meteor at the shower's `First`,
  a flare drawn on that tick. `FlareNow` still works while calm.

## API

- **Queries:** `Incoming`, the meteor seen coming, for its flight; `Shower`, the shower falling now; `Charge`, a meteor's
  burst; `FlareRules`; `FlareComing` and `FlareLeft`, the warning and its ticks left.
- **Commands:** `MeteorHit(cell)` — refused with `hazards.noWall` unless `cell` holds a wall, and `hazards.neverBreaks` for one that
  never breaks (bedrock, lava) or out of the grid's zone (a planet's fog). `FlareNow(ticks)` — `hazards.badTicks` below 1,
  `hazards.flareComing` during a warning. `SetCalm(on)`, never refused. Query `Calm`.
- **Fuzz:** a hit one turn in 64, mostly at a standing wall, else at any cell or one off the grid; half of them of a power
  0–7, so bursts that crack and break run in the generic suites. It
  reads the grid's walls when it draws, where the TypeScript generator, which cannot see the grid,
  aims at the wreck's walls as drawn. `FlareNow(1)` one turn in 64, `SetCalm` either way as often; `MagneticFlareTests` loads a world struck and repaired every tick.

## Invariants

- A meteor only ever turns what it breaks, a wall, piece, machine or back wall, into an empty cell; it adds nothing but what
  a machine held and a net's cost on the floor, a plan neither.
- A strike breaks between `ceil(n × PercentMin / 100)` and `ceil(n × PercentMax / 100)` of its `n` candidates.

## Deliberately not handled

- Meteors aim at walls only, never heroes; they come only from the right, never through an open door. What the burst
  reaches round the wall, doors and machines too, it cracks or breaks. Showers do not change with where the ship goes yet.
- A client joining between a strike and its next frame misses the strike's look.
- Nothing shields a machine from a flare yet; fuses and shielded walls will shrink the candidates. No grace before the
  first flare, and a machine just repaired may break in the next.

## Tests not yet ported

Every test of `hazards.test.ts` is ported, with `atmosphere`, `building` and `items`.

# Design: body temperature

Status: second draft, 2026-11-17, after the user's call: the sim stays stateless, and the body's slow change is the
client's. Reviewed by `design-reviewer` and its fixes applied. Lands after `feat/mild-climate`, whose degrees it reads.

## Problem

There are two complaints, and both come from the HUD thermometer showing the place's degrees.

- **It stops at −40°.** In space it stops at −40° past the field's line while the frost keeps growing. The field's edge
  costs air through a second rule, `EdgeCost`, that never reads as degrees.
- **"−20° in space" reads oddly.** It is an environment number where the player expects their own state.

## Player experience

The thermometer shows the hero's own body, 37.0° when nothing is felt. It sinks below that in the cold and rises above it
in the heat, and the farther past the suit's limit, the farther it goes. The number never jumps. It slides to its new
reading over a few seconds: fast when the gap is wide, slowly as it closes. So the body seems to cool and warm, while the
sim knows only where the hero is.

1. **Bare hero out of the airlock** (by the hull, nothing worn). The body slides to about 35.9° within a few seconds, with
   light frost. Air drains as it does today.
2. **Suit in space, inside the field:** 37.0°, nothing felt, as today.
3. **Toward the edge:** past the field's line the cold deepens without end. The body falls, sliding faster the deeper the
   hero goes, and the frost thickens with it. Halfway it reads about 31.8°. At the world's edge it reads 30.0° and air
   drains at today's 45 units a breath.
4. **Back into a room** (+20°): the cost stops at once. The number climbs back to 37.0° over a few seconds, and the frost
   melts with it.
5. **Night on a planet** (mild climate, −8°): a bare hero reads about 36.5° and pays a trickle; a suited one reads 37.0°.
6. **Deep hot band:** past the suit's heat limit the body rises toward 42.0°, with the haze and an ember number.
7. **The cape:** both limits are 5° lower, as today. The body falls less in the cold and rises more in the heat.
8. **The hover over the thermometer:**
   - the body;
   - where the hero stands: «Здесь −8°» on a planet or in a room, «Вакуум» in open space. Open space is the test the
     gusts already use, `Rooms.Afloat(c) && !Atmosphere.AiredAt(c)` (`HeroView.Feel.cs`), so there is no new sim API;
   - both limits, the one crossed in red.

## The rule

**One source of cold.** A place's degrees are as today, with one change: past the field's line, space keeps getting colder.

- **Degrees past the line.** `Afloat` runs −20° by the hull to −40° at the line, as today. Past it the degrees fall
  linearly with the depth `EdgeCost` measures today, by the deeper axis, from −40° to `Temperature.Coldest` (−273°) at
  the world's edge. They are rounded toward the colder, so the first cell past the line is felt at once.
- **Off the grid:** `Coldest`, on every surface, planets too, where it was `Space`.
- **What goes:** `EdgeCost`, `Field.EdgeCost`, `Field.OffGrid` and the edge's line in the breath.

**Each degree costs more than the last.** The felt degrees (`Felt` + `Heat`, past either limit, as today) cost:

`Bite × felt + Steep × felt² / 1000` thousandths of a unit a breath.

`Bite` stays 20; `Steep` is set so a basic suit at the edge (233 felt) pays today's 45. The cost is paid on today's
`Chills` schedule, after the regen suit's give-back. The last-breath rule is unchanged: "nothing felt" now covers the edge
too.

| Felt | Case | Units a breath, today → new |
| --- | --- | --- |
| 8 | bare hero, mild night | 0.16 → 0.21 |
| 20 | bare hero by the hull | 0.4 → 0.7 |
| 116 | suit, halfway to the edge | 12 (edge) → 12.3 |
| 233 | suit at the edge | 45 → 45 |
| 273 | bare hero at the edge | 45.8 → 60.8 |

**The body shown** is client-only:

- below a limit: `37.0 − Drop × Eased(felt / ColdFull)`;
- above one: `37.0 + Rise × Eased(heat / HeatFull)`;
- `Eased` is today's 2s − s²;
- `ColdFull` is derived, not tuned: `Temperature.Space − Temperature.Coldest` (233), the felt of a basic suit at the edge,
  the same fact `Steep` is set on;
- `HeatFull` is today's `Report.FeltFull` (30);
- `Drop` is 7.0° and `Rise` is 5.0°: the two ends, 30.0° and 42.0°, are separate choices.

**The glide is today's.** `Thermometers.Glide` already closes `1 − e^(−dt/GlideS)` of the gap each frame, so the wider
the gap, the faster it moves. It now glides one signed share (cold below 0, heat above), so a hero from the hot band into
the cold passes through 37.0°. `GlideS` goes from 0.3 s to about 1.5 s, and the snap from half a degree to 0.02 of the
share, or the last tenth crawls. The frost and the haze are drawn from the same glided share, so the number and the frost
always agree. `Report.Cold`'s second, edge share goes.

## Users of the mechanic

- **This stage:** space by the hull and the edge, the first planet's bands, night and storms (mild climate), the hot band,
  rooms, the suits' `Low`/`High`, the cape.
- **Next:** hotter planet types (the third runs 45° to 70°), and later perhaps a hand-held thermometer (below).

## Determinism and saves

- **Integers only in the sim.** The degrees past the line are rounded toward the colder. `Steep` is in millionths of a
  unit per degree², and the cost is computed in `long`, rounded down to thousandths. `Chills` turns thousandths into whole
  units across breaths, as today.
- **Nothing saved changes:** no new field, no format bump. The body shown is the client's, floats allowed.
- **Inputs:** the hero's reported cell and gear, as today. Nothing new reaches the sim.

## Cards and text

- **Cards:** unchanged. A body «Выдерживает до −40°» and «жар до +50°», the cape «Тепло +5°».
- **HUD:** the number gets a tenth, «36.4°».
- **New keys:** «Здесь {n}°», «Вакуум», with their Russian.

## Trade-offs accepted

- **No inertia in the sim.** A step into the cold costs air at once, and a room saves at once. The slow change is only on
  screen, so the frost can trail the cost by about τ. We accept it: the user prefers the simple rule that looks
  complex.
- **The edge keeps its strength:** 12.3 against 11.3 halfway, the same 45 at the edge. The cost grows with felt
  squared, the felt with depth, so the curve keeps today's shape. These are placeholders for the coming air retune.
- **Mild cold costs a little more** (the squared term). A bare hero by the hull pays 0.7 instead of 0.4.
- **The frost is lighter in mild cold.** Today a bare hero by the hull sees heavy frost (20 felt over 30). Now it sees
  light frost, because the full frost is at the edge. The frost shows how bad it is, not merely that it is cold.
- **Space shows no degrees.** «Вакуум» stands where «−20°» did. A suit's −40° matters there only at the edge, which the
  body shows.
- **The line itself is gentler.** Today the first cell past it costs a whole unit, because `EdgeCost` rounds up. Now it
  costs about 0.02 a breath: one or two degrees felt. The frost shows at once; the cost builds.
- **Off the grid is no longer instant death.** It costs 45 units a breath to a basic suit rather than 100. Only a hero
  pushed off the map's edge is ever there. The heavy helmet's guarantee that no tank outlasts off-grid air goes with
  `Field.OffGrid`.
- **The frost caps at the edge.** A bare hero past 233 felt sees full frost while the cost goes on rising to 60.8. The
  frost overstates everywhere before that (0.75 halfway against 12.3 of 45), so it never hides danger.

## Out of scope

- **A hand-held thermometer.** The user's idea: a hotbar item that reads the place's degrees. It is deferred, because the
  hover reads them already. It wants its own picture and design.
- **Food or heat sources warming the body.** The body is not a state to warm.
- **Shivering, slowing or any effect beyond air.**
- **Other heroes' bodies on your screen.**
- **Time-left projections.**

## Tests

- **Scenarios** (Crew, Field):
  - degrees: −40° at the line, a degree colder one cell past it, −273° at the edge, `Coldest` off the grid;
  - the cost per breath at felt 8, 20 and 233;
  - a Content test pinning `Bite × 233 + Steep × 233² / 1000 = 45 000`, so `Steep` cannot drift from its edge;
  - a room ends the cost at once;
  - the regen suit's give-back comes first;
  - the last breath ends in a room after the edge;
  - the cape shifts `Felt` by 5.

  Port `FieldTests`' edge cases onto `TemperatureAt`; `RegenSuitTests`, `FogBandsTests` and `PlanetRulesTests` stop
  naming `EdgeCost`. Also update `TemperatureTests`' off-grid cases (`Space` → `Coldest`), and `ContentTests` and
  `ContentTests.Hero`' `Field.OffGrid` guards.
- **Invariants (FsCheck):**
  - the degrees never get warmer farther past the line;
  - the cost never falls as felt rises, and is 0 exactly at 0 felt.
- **Client (`game.tests`):**
  - the shown body from felt and heat: 37.0 at 0, 30.0 at `ColdFull`, 42.0 at `HeatFull`;
  - the glide never overshoots and moves faster for a wider gap;
  - the hover reads «Вакуум» in open space and degrees in a room.

## Cost

- **Content:** `Temperature.Coldest` −273 and `Temperature.Steep` 743 (millionths). `Field.EdgeCost` and `Field.OffGrid`
  go.
- **Client tunables** in `Thermometers`: `Drop` 7.0, `Rise` 5.0, `GlideS` 1.5 s.
- **Code:**
  - Crew: `Afloat`/`TemperatureAt` past the line; `Chills` takes the curve; `EdgeCost` goes.
  - Client: `Thermometers`, `Thermometer`, `Report.Cold`/`Heat`, the frost overlay.
  - Docs: the Crew README, and an ADR superseding 0050's oxygen edge, the degrees ADR's "edge stays apart" and the
    heavy helmet's off-grid guarantee.
- **Size:** about 250 lines of diff.

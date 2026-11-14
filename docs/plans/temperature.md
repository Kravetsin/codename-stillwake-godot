# Plan: temperature in degrees

Status: designed (`design-author`, cut by `design-reviewer`, reviewed by `/review-plan`); T-1 and T-2 built, T-3 open. Supersedes the
unmerged depth-band design of PR 502; this plan keeps PR 502's bands, room air, night in band 0 only, unsaved payment and heat
suit.

## Why

Item cards state only what an item guarantees (capacities, rates, resistances, price), never a projection such as "lasts
2:00". Today's cold has three sources (the bare body, night's chill, and in part the field's edge) and the cape spares a fifth
of all of them, so no card can say what a suit or the cape does against it: "which cold?". Until this lands the cards say only
«Защита от вакуума» for a body and «Сопротивление холоду 20%» for the cape.

The user's idea, weighed and kept: every place has a temperature in degrees; gear withstands down to a limit and costs nothing
inside it; each degree past it costs air; the cape adds degrees; the temperature is on screen; heat later goes through the
same rule. The user will revisit air and cold numbers, so every number below is a placeholder; the shape is the decision.

## The rule

Crew owns it: the breath is its only payer, and Crew already reads gear, `Sky` and air. A new module would own no state.

**A cell's temperature**, `TemperatureAt(cell)`, whole degrees, a pure function of content, cell, sky and air (nothing stored,
nothing per frame):

- off the grid: `Temperature.Space`, on any surface;
- an aired cell (`AiredAt`): `Temperature.Room`, at any depth and on any surface;
- on a planet (`Rules` is a `PlanetType`): its `Temperatures[Terrain.BandOf(type, row)]`; in band 0 only, a cell with no back
  wall is `SkyNow.Chill` degrees colder (0 by day). Deeper bands never feel the night;
- aboard: `Temperature.Space` everywhere on the grid (since: `Temperature.Hull`, −20°, in the zone, to −40° at the field's line,
  and a planet's bands smooth between their middles: [temperature-gradients](../decisions/2026-11-14-temperature-gradients.md)).

The pod's reach is not warm: it gives the breath air, not heat, as today, where a bare hero by it pays the bare cold.

**A hero's limit**, `LowOf(peer)`: the worn body's `Item.Low`, or `Temperature.BareLow` with none; `Cape.Warmth` degrees lower while
the cape is worn.

**Felt degrees**, `Felt(peer, cell)` = `max(0, LowOf(peer) − TemperatureAt(cell))`. Each felt degree costs `Temperature.Bite`
thousandths of a unit of air (a unit is 1000 thousandths) a breath, flat: every degree costs the same, which a player can read
off the thermometer and a card.

**Paid** every breath, on its own schedule and not on thin air's beat; thin air's clamp of the breath's shortfall is unchanged.
It comes from the hero's own air alone, after the breath and the regen suit's give-back (which never covers it, as today),
before lava. With `r = Bite × felt` and `n = Clock / BreathEvery` read after the tick's `Clock += 1`, a breath pays
`⌊(n+1)·r/1000⌋ − ⌊n·r/1000⌋` (in `long`): rounded down, the rest paid on a later breath, nothing saved. Every hero pays on the
same breaths, as the chill's beat does today. It does not reuse `Hero.Owed`: that remainder is paid through room air and moves
only on covered breaths, while the cold is the hero's own air alone. Creative and passed-out heroes pay nothing: the payment
sits after both of the breath's early exits.

**The last breath** (the-last-breath): cold past an empty air starts it and cuts none of its ticks, as today. A covered breath
ends it only where `felt == 0`, not where the breath happened to pay nothing: otherwise a bare hero by the pod, paying on four
breaths of five, would have its last breath reset on the fifth and never pass out.

**Stays apart:**

- **The field's edge** (0050): `EdgeCost` unchanged, `Field.OffGrid` too, paid after the cold, and no longer softened by the
  cape. It is a leash on the build field, not weather: a better suit must not lengthen it, and a card claims only weather. A
  flat cost per degree could not reach its 45 units a breath without thousands of degrees.
- **Lava** burns as today; the cape never spared it.
- **Blows:** `Soften` and `Hero.Spared` stay for `Cape.Guard` alone.

## Numbers (placeholders, all in `sim/Content`)

| Name | Value | Today |
| --- | --- | --- |
| `Temperature.Room` | +20° | — |
| `Temperature.Space` | −40° | — |
| `Temperature.Bite` | 20 thousandths a degree a breath | — |
| `Sky.Cycle.Chill` | 20 degrees | 1 unit every `Every` (5) breaths; `Every` goes |
| `Temperature.BareLow` | 0° | `Crew.BareCold`, 1 unit a breath, goes |
| `Item.Low`: `suit`, `regenSuit` | −40° | — |
| `Cape.Warmth` | 5 degrees, not the edge | 200‰ of all cold, the edge's too |

`PlanetType.Temperatures` is required, one per band (`Bands.Count + 1`, a ContentTests check). `Small` and `SmallSpire` take
the first planet's through `with`. The first planet's bands (`Terrain.BandOf`):

| Band | Name | Where | Temperature |
| --- | --- | --- | --- |
| 0 | cold, ice-blue | the surface and the caves under it | −30° (night −50° under the open sky) |
| 1 | garden | caves | −15° |
| 2 | amber deep | caves | +15° |
| 3 | ember | caves over the lava | +60° |

Day on the first planet sits inside the suit's limit; night pushes past it. An aired room (+20°) is warmer than the amber deep
on purpose: a sealed base is a shelter.

## Walk-throughs (a suited hero's basic helmet holds 45 units; a hero with no body, its lungs' 20)

| Case | Felt | Today | New |
| --- | --- | --- | --- |
| Aboard in vacuum, suited | 0 | 45 s | 45 s |
| Aboard in vacuum, no body | 40 | 10 s | 11 s |
| Planet by day, suited, cape or not | 0 | 225 s | 225 s |
| Planet at night, open sky, suited | 10 | 112 s | 112 s |
| … with the cape | 5 | 125 s | 150 s |
| … under a back wall, or in the garden | 0 | 225 s | 225 s |
| Planet by day, no body | 30 | 112 s | 56 s |
| Planet at night, open sky, no body | 50 | 75 s | 37 s |
| Amber deep or ember, any gear | 0 | 225 s | 225 s (heat costs from step T-3) |
| Edge only: half the depth from the field to the grid's edge, caped | 0 | 10.6 a breath | 13 a breath |

Aboard a breath costs a unit; on the planet thin air costs 0.2 a second. At half the depth `EdgeCost` is 12: today the cape
spares a fifth (12 × 0.8 + 1 = 10.6), from now none (12 + 1 = 13).

## Accepted trade-offs

- **No body costs twice as much on a planet** (felt 30 by day). The pod prints a body for nothing.
- **The cape spares none of the edge**, so its frost there is heavier than today.
- **An aired room is +20° at any depth**, the ember band too.
- **The ember band reads +60° and costs nothing** until heat (T-3); the thermometer shows the number only, no heat colour.
- **A unit may shift to a neighbouring breath** when felt changes between breaths, or when a hero changes surfaces (each
  surface has its own `Clock`); too small to matter.
- **Night no longer reaches below band 0**, where today a cave cell without a back wall felt it at any depth.
- **`Clock` is an `int`:** it wraps after 2³¹ ticks, over three years of play on one surface.

## On screen

- **The thermometer:** a piece of the vitals block, bottom-left, in its light-and-water look: the reading against the hero's
  limit, calm inside it, frost-blue past it and deeper the further past, with the small number in degrees (the user's "shown in
  a corner"). Hover: the reading and «Выдерживает до −40°». Hidden with no hero or passed out; in creative it reads, at no cost.
  Its look is the user's pick, on a published page of candidates drawn at the game's scale through a day, a night and a flight
  out.
- **The frost:** `min(1, max(felt / 30, EdgeCost / Field.EdgeCost))`, eased as `Report.Cold` eases today (`s·(2−s)`); the cape
  lightens it only by what it changes in felt. `Report.BareFrost`'s floor goes: the smallest felt (5) already draws about 0.3.
- **The cell:** the thermometer and the frost read the body's own cell, as the frost does today (0050): they show the cold a
  report before the sim pays it. Display only.
- **Cards:** a body «Выдерживает до −40°» from `Item.Low`; the cape «Тепло +5°» from `Cape.Warmth`, beside its 20% guard. Keys
  in `Items.En.cs` and `Items.Ru.cs`.

## Saves, co-op, determinism

- No new input: the cell arrives by `Enter`, gear by `Items`' commands, the hour from `Sky`. Integers only; nothing stored.
- Nothing saved changes. `Hero.Spared` keeps its meaning for blows; a remainder an old save holds from cold is under 1000 and
  valid for them. `SkyNow.Chill` changes from units to degrees but is derived, never saved. No `SaveFormat` bump.
- A load pays no catch-up: the schedule is read from the saved `Clock`, which every surface's Crew saves and counts alike.
- A peer joining gets nothing new: temperature comes from content, cell, sky and air.

## Steps, one PR each, in order

### T-1: the cold in degrees (sim, frost, cards)

- **Content:** `Temperature` (`Room`, `Space`, `Bite`); `PlanetType.Temperatures`, required, and the first planet's row;
  `Item.Low` on the two bodies; `Temperature.BareLow`; `Cape.Warmth` in degrees; `Sky.Cycle.Chill` in degrees, `Every` gone;
  `Crew.BareCold` gone.
- **Crew:** `TemperatureAt`, `LowOf`, `Felt` on `ICrewQueries` (replacing `Cold` and `Warmth`); the payment in the breath
  replacing `ColdAt`'s bare and chill parts; the last breath's end keyed on felt; `EdgeCost` kept, unsoftened. **Sky:** `Chill`
  in degrees, no beat.
- **Client:** `Report.Cold` from `Felt` and `EdgeCost` as above; `SuitGauge`'s big-drop threshold without `BareCold`; the card
  lines in `ItemCards`.
- **Tests, failing first:** one scenario per walk-through row; the cape spares none of the edge nor lava; a band-1 cell with
  no back wall feels no night; an aired planet cell costs a bare hero nothing; off the grid reads `Space`; a bare hero with no
  air by the pod aboard passes out; cold leaves `Spared` alone. ContentTests: every `PlanetType` has a temperature per band.
  Invariants (FsCheck), each broken once to see it bite: a lower `Low` never costs more; over k breaths at a steady felt f,
  k·Bite·f/1000 is paid within one unit. Rewritten where they assert `BareCold`, `Chill`, `Every`, `Cold`, `Warmth`,
  `BareFrost` or the cape's share of cold: `FieldTests`, `SuitsTests`, `LavaTests`, `PlanetRulesTests`, `LastBreathTests`,
  `FogBandsTests`, `ContentTests.Hero`, `CapeTests`, `SkyTests`, `RegenSuitTests`, and in `game.tests` `ReportTests` and the
  item card tests. Machines' `TooCold` is another rule and stays.
- **Docs:** ADR [temperature-in-degrees](../decisions/2026-11-14-temperature-in-degrees.md), superseding the cold parts of night-chills, suits-are-worn and the-cape-is-worn;
  Crew's and Sky's READMEs; the concept's cape line; `docs/glossary.md` (температура, градусы).

### T-2: the thermometer

The pick page (CLAUDE.md, art picks), then the piece in the vitals block reading `TemperatureAt`, `LowOf` and `Felt` on the
panels' timer; a dev room the user plays (day, night, cave, flight out).

### T-3: heat (its own short design first)

Bodies gain `Item.High`; felt adds `max(0, t − High)` at the same `Bite`; the heat suit, PR 502's (Low −40°, High +40°),
made at the workbench; the thermometer's and the frost's heat side, a haze picked on a page. Open there, from PR 502: whether
the amber deep (+15°) already asks for gear, and how the heat suit is unlocked.

This plan is deleted when T-3 lands, what stays true moved into the ADRs, Crew's README and the concept.

## Deliberately not handled

- Per-cell simulated or stored temperatures; heat from lava, furnaces, the galley or machines; rooms heated or cooled.
- Temperature for machines, plants, creatures or liquids.
- Wind, clothing layers, a cold suit, coolant consumables.
- Storms: they deepen `Chill` later, under [day-night](day-night.md).
- Spore gas in the garden: not temperature; a later Creatures slice, as PR 502 planned.
- Caves of the cold band colder than its surface by day (PR 502's Q3): the band is one number until a player sequence needs two.

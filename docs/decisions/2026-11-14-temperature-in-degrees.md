# Temperature in degrees

- Status: accepted; the cold in degrees built (step T-1 of [docs/plans/temperature.md](../plans/temperature.md)), the
  thermometer and heat to come
- Date: 2026-11-14
- Supersedes the cold of [suits-are-worn](2026-10-27-suits-are-worn.md) (`Crew.BareCold`), of
  [night-chills](2026-11-07-night-chills.md) (a unit every five breaths) and of
  [the-cape-is-worn](2026-10-27-the-cape-is-worn.md) (a fifth of all cold); and the unmerged depth-band design of PR 502

## Context

Item cards state only what an item guarantees: capacities, rates, resistances, price, never a projection such as "lasts
2:00". The cold had three sources, the bare body, night's chill and the field's edge, and the cape spared a fifth of all of
them, so no card could say what a suit or the cape did against it. The user proposed degrees: every place has a
temperature, gear withstands down to a limit, the cape adds degrees, the temperature is on screen, heat later goes through
the same rule. PR 502 had designed temperature by a planet's depth band, in degrees from comfort and never shown; this
keeps its bands, its room air, its night in the top band only and its unsaved payment.

## Decision

- **A cell's temperature** (`Crew.TemperatureAt`), whole degrees, a pure function of content, cell, sky and air:
  `Temperature.Space` (−40°) off the grid; `Temperature.Room` (+20°) aired, at any depth; else its kind's
  `Temperatures[band]` (a planet's `Terrain.BandOf`, else the one), the top band `Cycle.Chill` (20°) colder at night where the
  cell has no back wall. The ship is −40° everywhere; the first planet −30°, −15°, +15°, +60° down its four bands.
- **A hero's limit** (`LowOf`): its body's `Low` (−40° for both), or `Temperature.BareLow` (0°) with none, the cape's
  `Cape.Warmth` (5°) lower.
- **Felt degrees** (`Felt`), below the limit, each cost `Temperature.Bite` (20) thousandths of a unit a breath, flat. Every
  breath, not on thin air's beat; paid from the hero's own air after the regen suit's give-back, as
  `⌊(n+1)·r/1000⌋ − ⌊n·r/1000⌋` with `n` the crew's breaths counted on its surface: nothing saved, every hero on the same
  breaths. A covered breath ends the last breath only with nothing felt, so a regen suit that covers every breath does not
  keep a hero at no air alive in the cold.
- **The field's edge stays apart** (0050): `EdgeCost` unchanged, paid after the cold, and the cape no longer spares it. It is
  a leash on the build field, not weather; a flat cost per degree could not reach its 45 units a breath.
- **The frost** draws the felt degrees over 30 or the edge's share, whichever is more, eased out; the bare floor is gone.
- **Cards:** a body «Выдерживает до −40°», the cape «Тепло +5°» beside its fifth off blows.
- **Saves:** nothing saved changes; `Hero.Spared` keeps blows' remainder alone. No format bump.

Numbers are placeholders for the coming air and cold retune. Day sits inside a suit's limit, night 10° past it, so the night
costs a suit what it did; a bare hero pays more on a planet (felt 30 by day) and about the same aboard.

## Consequences

- The cape makes the night half as dear and spares no edge; its frost there is heavier than before.
- A bare hero on a planet lasts about 56 s by day, 37 s at night; the pod prints a body for nothing.
- An aired room is +20° at any depth, the ember band too: a sealed base is a shelter.
- The ember band reads +60° and costs nothing until heat: bodies gain a `High` then, with the heat suit.
- A unit may move to a neighbouring breath when felt changes between breaths or a hero changes surfaces.

## Deliberately not handled

- Per-cell simulated temperatures; heat from lava, furnaces or machines; rooms heated or cooled.
- Temperature for machines, plants, creatures or liquids; wind, clothing layers, a cold suit.
- The cold band's caves colder than its surface: the band is one number until a player sequence needs two.

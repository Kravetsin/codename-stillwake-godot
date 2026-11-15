# Temperature gradients: colder away from the ship, smooth down the bands

- Status: accepted; built with the thermometer ([the-thermometer](2026-11-14-the-thermometer.md))
- Date: 2026-11-14
- Amends [temperature-in-degrees](2026-11-14-temperature-in-degrees.md): the ship's −40° everywhere and a band's one number
  all through it

## Context

With the thermometer on screen, the user found the readings odd: a step out of the ship read −40° at once, and a planet's
band one round number from its top to its bottom. They asked for space in more pieces, colder the farther from the ship,
and chose on 2026-11-14 −20° by the hull falling to −40° at the field's line, and a planet's degrees running smoothly with
depth.

## Decision

- **Afloat** (a kind that `Floats`): an airless cell in the build zone or a vessel's box is `Temperature.Hull` (−20°). Across
  the field it falls straight to the kind's own (`Temperatures[0]`, space's −40°) by its cells past the zone on the farther
  axis over `Field.Margin` (128), rounded toward the warmer, and holds it from the field's line on. Off the grid stays
  `Temperature.Space`.
- **On a planet** (`Terrain.TemperatureOf`): a band's `Temperatures[band]` holds at its middle, in thousandths of the way
  down; between two middles it runs straight, rounded toward the upper band's; above the top band's middle and below the
  last's it holds. The first planet's surface still reads −30°; no band's edge is a step.
- The night still reaches only the top band (`BandOf`) under the open sky; an aired cell is still `Room`.
- Still a pure function of content, cell, sky and air: nothing saved, no format bump.

## Consequences

- A bare hero by the ship feels 20°, not 40°: half the cold it paid. A suit's −40° feels nothing anywhere in the field.
- Past the field's line the degrees stop at −40°; the edge's cold (`EdgeCost`, 0050) is still the leash there.
- A reading is rarely a round number, and the thermometer glides through the degrees between.

## Deliberately not handled

- The distance is to the zone's box, not to the hull's own walls: the zone is what the field grows from, a box query; a
  distance to walls would be a walk on every change.

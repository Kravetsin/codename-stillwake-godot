# The first planet's surface is mild; its storms are the threat

- Status: accepted; supersedes [surface-storms](2026-11-16-surface-storms.md)'s 20° storm chill and
  [storm-lightning](2026-11-16-storm-lightning.md)'s one bolt per storm for the rain
- Date: 2026-11-18

## Context

With the body's temperature ([body-temperature](2026-11-18-body-temperature.md)) the first planet's −30° surface would have
chilled even a suit's wearer, and its storms only added cold. The user wanted the surface mild, the caves cold, and each storm
a danger of its own: dig in from the dust, take cover from the lightning.

## Decision

- **Mild air over cold caves:** a planet type may set `PlanetType.Air`, the degrees at its surface's middle row and above. From
  there `Terrain.TemperatureOf` runs straight to the top band's middle, so the first planet is +12° up top (−8° at night under
  the open sky, `Cycle.Chill` 20) and −30° in its cold band's ice caves. The landing surface takes the same air.
- **The dust storm grinds the suit:** a stretch's `Grit` (2 for the dust storm) is units of a hero's own air a breath under the
  open sky, unaired (`SkyNow.Grit`, Crew's `GritAt`), past what it breathes; it starts the last breath as a shortfall does and
  keeps a covered breath from ending it. Its chill is 10°.
- **The rain strikes again and again:** a `BoltRule` may draw `Again`, the wait after each strike. The rain's first bolt is
  180–300 ticks in and each next one as far after the last struck: a bolt every 12–18 s, glow included. Its chill is 5°, no
  grit. The dust storm keeps its one bolt.

## Consequences

- A suit feels nothing on the first planet's surface, day, night or storm; a bare hero feels the night, and a storm at night more.
- A storm's danger is its own: grit empties a suit left out in the dust; the rain's bolts shock whoever stays under the sky.

## Deliberately not handled

- Grit ignores the suit worn: no filter or tighter suit spares it yet.
- The other planet types keep their top band's degrees throughout (`Air` null) and have no weather.

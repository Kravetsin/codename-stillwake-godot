# Surface storms

- Status: accepted; supersedes [stretches-of-space](2026-11-15-stretches-of-space.md)'s one warning, stretches only aboard and
  the world's Rng
- Date: 2026-11-16

## Context

Planet 1's surface held nothing to stay up top for. Weather makes it a place to watch and shelter from, and the icy planet's
blizzard and the scorched one's ash storm are its next users. Designed, cut and reviewed as `docs/plans/surface-storms.md`,
its slice 1 played by the user and tuned on what they saw.

## Decision

- **A storm is a stretch** on a planet's kind (`Kind.Stretches`, `Kind.StretchOf`): Sky's schedule, `SkyNow` and `StretchNow`
  serve the ship and the planets alike. Each stretch has its own `Warning`; an empty `Unlock` opens it from the start; `Zone`
  is optional (none on a planet). Icy and Scorched list none until their own weather comes.
- **Planet 1 has two storms**, dust and rain, one at a time: about three in a 20-minute day (first in 6000–9600 ticks, a gap
  of 5700–7300 from the last end), each 1200–1800 ticks, warned 800 ahead. The hour is not read.
- **Under the open sky** (the top band, no back wall; Crew's `OpenSky`) a running storm is 20° colder (`Chill`, through the
  existing path) and a hero moves at half pace (`Stretch.Pace` 500‰, `SkyNow.Pace`, `ICrewQueries.PaceAt`; the controller
  scales its walk and run tops by it). The warning neither chills nor slows. Rain is a look, not water.
- **Draws come from the sky's own fork** of the world's Rng, salted by the surface and a saved count `Drawn`, so two planets
  drawing on one tick differ and nothing else's draws move. No format bump: every field added has a "none" initializer.
- **Seen and heard without words**, all from `SkyNow` through `StretchShown`, so a guest joining mid-storm sees the others'
  share: the sway grows, the sky and the land behind turn toward the haze, daylight dims, then streaks blow and the wind or
  the rain plays. The world under the open sky is washed out (`Washout`, a screen shader). It all fades with depth under
  the land's line, gone 12 cells down (`Storm.Reach`), so a cave is quiet.
- **Streaks are drawn by a shader**: the world cut into slanting cells along the fall, at most one streak a cell, so the
  pick's count over its view holds over any view. Only the columns' sky end goes to the GPU, read once a surface loads and
  then per changed column. Laid out on the CPU they cost 2–3.5 ms a frame zoomed out; the shader costs 0.01 ms.
- **The picks** (the user's, 2026-11-16): the dust storm «A» (grit, no clouds of dust: wisps were dropped after play), the
  rain storm «B»; the wash «Блёклый мир» (grey 0.8, flat 0.42); the wind `wind_F` (Fission9, "Polar Wind") and the rain
  `rain_A` (felix.blume, "Rain and wind."), CC0, credited in `game/audio/CREDITS.md`. No clear patch round the hero.

## Consequences

- A new weather is a stretch and a look (`StretchLooks.Weather`), no code; a slower or faster one sets its `Pace`.
- Every suite meeting planet 1 may meet a storm after 6000 ticks; one reading temperature, air or speed states its storm.
- Lightning (slice 2) and surface plants (slice 3) follow in the plan.

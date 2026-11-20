# The sun moves by the hour, and what is far stays behind

- Status: accepted; supersedes [night-chills](2026-11-07-night-chills.md)'s omission of a moving sun, and
  [stretches-of-space](2026-11-15-stretches-of-space.md)'s far picture drawn plain at full size
- Date: 2026-11-18

## Context

A planet's sun hung at one place on the screen through day and night, and the sky and land behind were as sharp as the play
plane. Aboard, the contraband drift's hulk filled the screen once the farthest zoom came nearer, and read as a wreck by the
ship rather than one far off.

## Decision

- **The sun by the hour** (`Daylight.Sun`, `PlanetSky`): it rises halfway through the dawn on the left and sets halfway through
  the dusk on the right, so it is low in their warm light; down at night. It climbs an arc from about the far mountains' tops
  (a fifth of the screen down) to noon's height, steep at its ends, and comes in and goes out wholly past the screen's sides,
  its glow too, so it is never seen to vanish. The dev panel's noon button sets the hour.
- **The backdrop out of focus** (`FocusView`): a planet's sky and the land behind are blurred by its look's `Far`, 0.4 mip
  levels at 1080 rows (the user's, tried with the moods); the play plane stays sharp. Hidden while sharp, aboard and in space.
- **Far pictures sink into space** (`StretchLooks.Deep`, the shader in `StretchBackdrop`): the user's pick «Дымка глубины», A on
  [Дальний корабль](https://claude.ai/artifact/JivmshrpFTLuhhcG5U5BuM): greyed 0.6, contrast flattened 0.5 toward the paint's
  mean, then hazed 0.6 toward the colour of space. No blur: a blurred hulk read as a poor picture. The ice belt's field keeps
  its own veil.
- **The hulk smaller:** 480 pixels of its plane across, not 1100, with a gap of 8 keeping one in a drift.

## Consequences

- The hour is read off the sky as well as off the light.
- The sky's pictures are soft at every zoom; a screen is copied for the blur only on a planet.

## Deliberately not handled

- No seasons, moons or a sun that differs by planet; the giant and the clouds keep their places.
- The sun's light on the ground does not follow it: daylight still comes from the open sky alike.

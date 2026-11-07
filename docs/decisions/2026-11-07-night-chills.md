# Night chills a planet's open sky

- Status: accepted; supersedes [0076](0076-a-world-is-made-of-surfaces.md) slice 4's omission "no gradient, sun, clouds or day
  and night" for day and night
- Date: 2026-11-07

## Context

The planet stage is being filled after playtests, and the user picked day, night and weather among the next big things. A
planet's daylight was one colour for ever. Day and night had to change something a player does, not only the look, and lay
the ground for storms, rain once liquids exist, hostile creatures that may act at night and further planet types.

## Decision

- **A module, `Sky`**, counts each planet's hour in its slice: a day of `Cycle.Length` ticks (the first planet's 24 000, 20
  minutes), its last `Cycle.Night` (7 200, 6 minutes) night. A planet opens at 0, the start of daylight, so the first landing
  never meets the dark. The ship has no day. It sits right after `Grid` in the modules' order, so Crew reads it; Hazards,
  where flares are scheduled, runs after Crew, and the world's tick is no clock (a planet opens at a different tick in each).
- **The chill.** At night a hero whose cell has no back wall pays `Cycle.Chill` (1) more air every `Cycle.Every` (5) breaths,
  suit or none, thin air or not; the cape spares a fifth as of all cold. Shelter is any back wall: caves, tunnels, ground
  under an overhang, back walls built. Walls and a roof without back walls shelter nothing: they hold no air either. Outside,
  a basic helmet lasts about 112 s at night against 225 s by day. The frost already drawn shows it; nothing new is said.
- **Looks** are the client's (`Daylight`): over the minute before the night line the light through the open sky fades
  through dusk to night, about a tenth of day, so the light warns before the chill; dawn fades back over the night's last
  minute. One `sky` uniform scales the baked starlight, lamps untouched; the backdrop and the fog at a planet's sides are
  multiplied by one colour, `Daylight.Backdrop`, which the backdrop's redraw consumes.
- **Dev:** `SetTime` sets the hour, refused `sky.noDay` aboard; the dev panel's "dusk" and "day", and `--dusk` with
  `--planet` or `--strip`.
- **Saves: format 54** adds the slice, `{"time": 0}`, right after `grid` in every slices object.

The user's answers to the design's questions: the chill goes through the suit; a 20-minute day with a 6-minute night, retuned
after a playtest; night first, storms after it is played; no planet hour shown aboard.

## Consequences

- Dusk sends a hero underground, behind back walls or aboard; the cape, of hunted hide, makes night cheaper.
- Storms (slice 2, docs/plans/day-night.md) reuse the chill: a storm adds to it under the open sky, scheduled as flares are.
- Rain needs no interface from a liquids module: it reads `ISkyQueries.Now` and pours into open-sky columns itself.

## Deliberately not handled

- **Wind pushing anything:** stacks, bodies, creatures and nails are sim; pushing only heroes reads wrong.
- **Fog banks** would read as the world's edge; **a separate cold snap** would be a storm without grit.
- **A clock anywhere,** the planet's hour aboard included; **seasons, moons, a moving sun** (the backdrop's call).
- **Flora, grazers or creatures' senses reacting to night;** `SkyNow.Night` is the hook when a rule names one.
- **Night's sound:** the planet has no ambience to change yet.
- **The ship's sky:** space has no day; solar panels are unaffected.

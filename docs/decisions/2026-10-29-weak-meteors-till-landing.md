# Weak meteors till landing

- Status: accepted
- Date: 2026-10-29
- Amends the starting space's shower of [hull-meteors](2026-10-28-hull-meteors.md) and [meteor-wave](2026-10-29-meteor-wave.md)

## Context

A softlock from play. At the start the only wall the player can afford is scrap, and it took smelted scrap. A meteor
holed the first room, the one with the furnace in it. The furnace's fire breathes its room's air, so it went out, and
no scrap could be smelted to patch the hole. Even with scrap at hand, a strike tore 7 to 17 cells of a scrap hull every
four to six minutes. Debris drifts by too slowly to catch that much before the next one, so the holes only grew.

Holding meteors back till the landing was weighed and rejected. The space phase, about five hours, would lose its
steady cost of repair, and meteors would first fall while the crew is down on the planet.

## Decision

- **Two showers on the ship.** `Surfaces.Kind.MeteorsBeforeLanding` (`Meteors.BeforeLanding`) falls till a planet has
  been opened, and `Kind.Meteors` (`Meteors.StartingSpace`, unchanged) after. `HazardsModule.Shower` is the shower
  falling now, and the client reads it for the streak and the wall card.
  - The weak shower comes at the same four to six minutes, with power 2–5, weighted low (40/35/20/5).
  - Its blast is `1 + 3 × power`: one scrap cell at power 2, about three at 3 and 4, five at 5. A corner loses a few
    more.
  - Iron breaks only at power 5, one strike in twenty, and only the cell struck.
- **The first meteor waits a quarter of an hour** (`Shower.First`). That is long enough to build the furnace and
  smelt some scrap.
- **A planet opened** is the stage, read through `ISurfaces.Landed`: a hero landing opens one, and so does a dev's
  `OpenSurface`. It is derived from the world's surfaces, so nothing is saved for it.
- **A scrap wall is patched of a raw scrap chunk**, one a cell, not of smelted scrap. Debris brings chunks from the
  first minute, and they are caught by hand. A repair then never needs the furnace or its air, at any stage. A wall
  now costs a whole chunk (four scrap smelted) instead of two scrap: the price of skipping the furnace.

## Deliberately not handled

- The shower does not grow by degrees between the start and the landing: two settings are easier to tune and explain.
- Floors and ladders still take smelted scrap: a meteor breaks walls only.
- After the landing, eight or so unrepaired strong strikes can leave every row's first thing a door or a machine, and
  then no meteor finds a target. That is as before.

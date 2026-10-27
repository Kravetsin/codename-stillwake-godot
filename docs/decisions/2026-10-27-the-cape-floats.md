# The cape floats

- Status: accepted; built
- Date: 2026-10-27
- Builds on [2026-10-27-the-cape-is-worn](2026-10-27-the-cape-is-worn.md)

## Context

The cape's cloth (`game/features/crew/logic/Cape.cs`) hung by gravity and blew in air everywhere, so out in space it
hung to the feet as aboard. The user wants it to move as cloth would in no gravity.

## Decision

- **Afloat is the figure's pose:** `Pose.Float` or `Pose.Tumble`, out in space or gliding in a planet's fog. `Figure`
  passes it to `Cape.Step`; the cape swaps the cloth's `ClothStyle` (now settable) on the step it changes.
- **No gravity, no air:** gravity 0, nearly all speed kept (`Keep` 0.985, the hood 0.97), no push, lift or flutter. A
  thrust leaves it behind and a stop carries it on past the hero, both from its own inertia.
- **The wool remembers its cut:** each point is drawn weakly toward its cut swung back and up from its pin, more toward
  the hem (`Cape.AfloatRest`), so at rest it opens out behind the hero rather than crumpling.
- **A slow wave** runs down it from the shoulders to the hem, so it ripples as in water and never quite stops.
- **No floor:** afloat it may drift below the feet; it still never comes more than `Give` forward of its cut.
- `Cloth.Update` takes a force per point (by index and position) instead of one wind for every point.

## Deliberately not handled

- **Fog is airless to the cape:** gliding in a planet's fog it floats as in vacuum, without the air's push.
- **Nothing collides with it:** walls, the jetpack's plume and other heroes pass through, as aboard.
- **Its afloat cut is an aim, not a shape:** its links keep it some pixels forward of `AfloatRest`; tests check it
  opens back and up, not where exactly.

## Consequences

- Tests in `game.tests/CapeTests.cs`: afloat it opens back and up and keeps rippling, unturned; a push-off leaves it
  behind and a stop carries it on; back in gravity it hangs as cut. Proven to bite with afloat ignored.
- `-- --jetpack --caped` flies the jetpack demo in a cape, to see it.

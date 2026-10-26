# fog (client): a planet's fog at its sides

A planet ends in a band of fog at each side ([fog-edges](../../../docs/decisions/2026-10-26-fog-edges.md)): the hero glides
into it, is moved at its seam to the other band and drops out of it, as if the planet wrapped. This feature draws the fog;
the bands are the grid's (`Terrain`, outside the zone), the glide and the move the hero's (`features/crew`, `FogMove`), the
muffle the sound's. What they share lives in `game/shared/logic`: `FogEdges` (the bands, seams, period and drop, read from
the grid) and `FogShare` (how thick the fog is by column and over the screen, and `FogScreen`, the flash at a move).

## Files

| File | Holds |
| --- | --- |
| `FogView.cs` | the fog drawn: one full-screen quad on its screen layer, thick by `FogShare.Column`, flashed by `FogScreen` |

## Rules

- **Over everything in the world** (`ScreenLayers.Fog`, under the frost and all that is read): the ground, the sky's
  backdrop, creatures, friends and this player's own hero. Made by `SurfaceViews` only where `FogEdges.Of` finds bands: a
  planet whose zone leaves the same columns at each side; never aboard, on a test map or on a planet saved before it had
  bands.
- **By column** (`FogShare.Column`, worked out again in the shader): none from `FogLooks.Lead` columns inside the zone,
  easing to `FogLooks.Edge` at the zone's side, so the hollow band is never seen clear, and to whole at the seam and past
  the grid. Wisps drift through it where it is thin, never where it is whole.
- **Over the screen** (`FogScreen`): none while the screen's middle is in the zone, whole at a seam. When the middle moves
  more than half the grid's width in a frame with fog on either side — the hero moved across — it holds whole for
  `FogLooks.FlashMs` and eases back over `FadeMs`. A beam-down, a load or a wake at the spawn flash nothing.
- **Shade:** the planet type's `PlanetLooks.Look.Fog`; under each band's ground line it darkens over a few rows as
  underground is, the less the thicker the screen's fog, so a move from a tunnel to the sky shows no change of shade.
- Hidden while no fog is on screen: nothing is drawn in the zone's middle.

## Deliberately not handled

- Light: the fog is unshaded, its own colour by day and by night alike.

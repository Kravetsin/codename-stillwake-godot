# Arcs are hairlines

- Status: accepted; built
- Date: 2026-10-27
- Supersedes the arcs' look of [0075](0075-effects-are-light-not-shapes.md)

## Context

[0075](0075-effects-are-light-not-shapes.md) draws effects as light at the screen's resolution, thin streaks over the
pixel world. Its arcs did not keep to that: a cyan rim 2.5 and a core 1 pixel of the world wide, times the zoom (5 and 2
screen pixels at zoom 2, 7.5 and 3 at zoom 3), and a glow sprite every other point along them. Beside the sparks and
trails they read as pixel art. The player's word, on the sentry's arc: the effects are fine lines, high resolution over
the pixel art, and that contrast is the look; every lightning effect is to follow it. The player picked variant D of five
on the arc page: a thin thread whose last shapes fade behind it.

## Decision

- **Every arc is drawn in pixels of the screen, whatever the zoom:** a white core of 1.2 px (`Bolts.Core`) in a haze of its
  tint 4 px across at a third of its strength (`Bolts.Haze`), branches 0.7 as wide and dimmer, never under the 1.1 px of
  every streak. An arc's `Width` still scales both.
- **An arc leaves an afterimage:** its last four shapes (`Bolts.Ghosts`, a tenth of a second at a roll every two 60 Hz
  frames), its own line without its branches, each a 1.1 px line in its tint's deeper `Ghost` colour, fainter the older.
- **No glow sprites along an arc.** The haze is its glow; the light feature still lights the world from it.
- **The sentry's arc is one chain**, its links 40 px of the world, so its folds stay fine on a long arc.

## Deliberately not handled

- **Arc shapes are still folded in shares of their length** in pixels of the world: a long arc that is not chained folds
  wide. The sentry's is chained; the others are short.
- **Ghosts end with their arc**: they show only while it lives, never after.

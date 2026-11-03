# The hero's own lights light what is round them

- Status: accepted
- Date: 2026-11-03
- Adds light to [the-regen-suit](2026-11-03-the-regen-suit.md)'s bands and [hero-clothes](2026-11-02-hero-clothes.md)'s tracker; their pictures stay as they were

## Context

The tracker on the shin and the regen suit's lit bands are drawn unlit, so they show in a dark room, but nothing round them caught their light. The user
asked for a light glow on them, and only on lights that are really there: no new lit parts on the basic suit or the helmets.

## Decision

- **Their light falls on what is near**, through the lightmap, as the planner's screen's does: a `render/Glow` on each light. The user picked it from five
  treatments on a page drawing the hero at the game's scale in a dark room and a lit one: a tight halo, a wide wash behind the figure, this one, a halo
  pulsing with the wave and a halo only in the dark. In the air nothing glows; the floor under the boot, the other shin, the suit and the wall behind take
  the colour.
- **The tracker** gives the player's colour, `Light.Tracker` of it, out to `TrackerReach` cells, always.
- **The regen suit's bands** each give `SuitLight` at `SuitBand` times their wave's strength, so the wave climbs the wall behind as it climbs the
  suit; its arcs at `SuitArcs` of their flicker, its empty belt's pip in red. Switched off, nothing gives light.
- **Each light gives off from the middle of its picture** by brightness (`HiRes.MiddleOf`), riding its part as it moves.
- **A glow says how far it lights** (`Glow.Reach`, `Glow.Keep`); the planner's screen keeps its own.
- **The look editor's figure gives none** (`Figure.GivesLight`): it stands in a window of its own, out of the world.

## Deliberately not handled

- **A halo in the air:** none, as picked. In a lit room the lights barely show beyond themselves.
- **Shadows:** the lights are points, as glows are, and cast none of their own.
- **A cap on them:** none, as on glows; a hero gives a dozen at most, so four heroes add about fifty spots.

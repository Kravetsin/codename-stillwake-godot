# Clear glass

- Status: accepted
- Date: 2026-10-29
- Builds on [wall-roles](2026-10-29-wall-roles.md), whose "no wall is see-through" it replaces

## Context

Glass walls were drawn frosted and opaque, and stopped light as any wall. The user wants glass half see-through: stars
showing through it, and by the same logic light passing it. Mica and hide stay opaque, so glass is the one seal
that lets a ship's rooms see out.

## Decision

- **A material says it:** `Material.Clear` (glass only). Drawn only: the sim gives it no rule; air, meteors and building
  treat glass as the other seals.
- **The picture:** a glass wall's and back wall's panes are half transparent (alpha 150 of 255, `tools/ship-art/walls`),
  so the stars drawn behind the ship show through; the frames are solid.
- **Light ignores it:** lamps and starlight pass a glass wall as air (`Sources.IsClear`: the lightmap's occupancy open
  there, `StarField` air), and the stars shine through a glass back wall as through none. The simplest rule: dimming
  light by the glass crossed was tried and drew stepped streaks; the frosted picture already reads as glass.
- **Changes follow:** a wall's material or a back wall's changing redoes the light round it, as a structure change does.

## Deliberately not handled

- Glass neither dims nor tints light; a room behind glass is lit as one open to it.
- A hero's helmet lamp still backs out of glass as out of any wall.

# Clear glass

- Status: accepted
- Date: 2026-10-29
- Builds on [wall-roles](2026-10-29-wall-roles.md), whose "no wall is see-through" it replaces

## Context

Glass walls were drawn frosted and opaque, and stopped light as any wall. The user wants glass half see-through: stars
showing through it, and by the same logic light passing it, weak. Mica and hide stay opaque, so glass is the one seal
that lets a ship's rooms see out.

## Decision

- **A material says it:** `Material.Clear` (glass only). Drawn only: the sim gives it no rule; air, meteors and building
  treat glass as the other seals.
- **The picture:** a glass wall's and back wall's panes are half transparent (alpha 150 of 255, `tools/ship-art/walls`),
  so the stars drawn behind the ship show through; the frames are solid.
- **Lamps:** the lightmap's occupancy marks a clear wall apart (`Sources.Clear`); the shader lets light through it,
  keeping `Light.ClearKeep` (0.5) a cell crossed, where a wall stops it. A hero's helmet lamp still backs out of glass
  as out of any wall.
- **Starlight:** a clear wall is a kind of its own in `StarField`: light goes in as into a wall, on through glass as
  through air, and out keeping `Light.ClearOut` (0.5) where a wall keeps `WallOut` (0.06). A cell with a clear back wall
  counts as open sky, the stars in it at `Light.ClearBack` (0.3) of their light: a room backed with glass is lit dimly
  blue, never as the open.
- **Changes follow:** a wall's material or a back wall's changing redoes the light round it, as a structure change does.

## Deliberately not handled

- Glass does not tint light passing it; it only dims it.
- Lamps cast straight shadows through glass as past the open: no blur for the frost.
- Light through glass costs as much as through the open; nothing bounds how much glass a lamp's walk crosses.

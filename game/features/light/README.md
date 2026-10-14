# Light (client)

Light without a word (0019, 0025): a dark ship lit by its lamps and by the stars through the holes in
its back walls. Drawn only: nothing of it reaches the sim. Numbers from `game/Content/Light.cs`.

- **Reads:** `IGridQueries` — `StructureLayer` (walls and closed doors cast shadows), `BackLayer`,
  `Machines` (lamps; machines dim starlight), `Width`/`Height`.
- **Ambient:** a `CanvasModulate` of `Light.Ambient`, so no corner is quite black.
- **Lamps:** a `PointLight2D` per lamp, soft shadows (PCF5), its texture falling off as the
  TypeScript game spread a lamp's light (`Light.Lamp`, `Light.Air` a cell, clamped per colour) out to
  `LampReach` cells, and an unlit additive glow. Brightness comes from `LightView.LampShare`, full
  until the power module exists; short of power a lamp flickers (`Flicker`, flicker.ts ported).
- **Shadows:** `Occluders` merges walls and closed doors into rectangles (row runs, joined down) and
  makes them one-sided, so a wall is lit on the faces light reaches and shadows what lies behind it —
  no unshadowed twin lights needed. On a change only the rectangles that differ are replaced.
- **Starlight:** `Starlight` works it out cell by cell as the TypeScript game did (the stars in every
  open cell with no back wall and past the grid's edge, spread by row and column sweeps), only when
  walls, doors, back walls or machines change, and hands it to `render/Lit.cs` as a texture of a texel
  a cell; a directional light with no shadows triggers it. Where a lamp and the stars both reach, the
  brighter shows, not their sum, as in the TypeScript game.
- **Kept current:** `LightView.Sync()` after every tick; lamps flicker in `_Process`.

## Deliberately not handled

- Fire and glowing particles as light sources, and helmet lamps: with the furnace, effects and hero
  (G-07, G-08).
- Light bending round corners and through doorways the way the TypeScript sweeps let it: Godot's
  lamps cast straight shadows; only starlight keeps the sweeps.
- A lamp's reach is cut at 16 cells for the frame rate (see `LightView.LampReach`).
- Recomputing starlight near a change only: it is redone over the whole grid, about 6 ms on the
  reference world per change.

# 0088 — Back walls recede, the ground's edges catch the light

- Status: accepted; amends [0082](0082-the-grounds-chipped-look-details-and-living-edge.md)'s edges
- Date: 2026-10-21

## Context

From the playtest: on a planet the back walls blend into the solid ground, and a player often cannot tell what blocks the way
until the pointer's green square shows it. A natural back wall is the ground's own picture at about half its colour, cut from
the same 8 × 4 tile, so its pattern runs on from the solid's; and a solid takes only `Light.WallIn` (0.7) of its face's light
while a back wall takes all of it, so on screen the back wall was only 15–30 % darker, and in the helmet lamp's beam a block
in a passage nearly vanished. Of eight looks drawn with the game's own tiles and light on a pick page, the user picked the
seventh, «Сочетание»: the back wall receding, a soft shadow on it under the solid's edge, and a lit edge on the solid.

## Decision

- **A natural back wall recedes** (`GroundArt.Recede`, once at load, on `back_*`): paled 30 % toward its grey, cooled (red
  −8 %, green −3 %, blue +10 %), then darkened to 0.62. Its pattern stays, so the ground still reads as one material.
- **Walls cast a soft shadow on it** (`GroundShadow`, plain C#, tested): the walls' pixels round a cell blurred twice by a
  box three pixels each way, times 2.1, in three tones, the darkest taking half the back wall's light — about four pixels
  deep along an open edge. It reaches less than a cell, so a cell's shadow hangs on its own shape and its eight neighbours'.
  Each such three by three is made once into a tile of a shared atlas (`ShadowTiles`): a sprint over the planet's surface
  made 166, a deep cave 102. The tiles are drawn on a layer of their own just over the back walls (`Layers.BackShade`),
  multiplying them as lit, unlit themselves; a dig redraws its cell's eight neighbours' shadows.
- **The ground's edges catch the light** (`GroundArt.Edge`): inside the dark outline a top shows two lit lines (its lit
  colour × 1.28, then × 1.02), the other open sides one (× 0.96) where they had a shaded one. A floor slope's diagonal is
  lit as a top; a ceiling slope's keeps its outline and one lit line.
- **Tunables** are `GroundLooks.Back*`, `Shadow*` and `Rim*`.
- **The ship is left as it was.** Its hull walls are light steel plates in a bright outline, its back walls dark panels of
  another picture; a frame of the testbed shows no such confusion. The look follows the material, so a natural back wall
  built aboard (0084) recedes and takes the shadow too, and any wall, hull included, casts it on one.

## Why tiles, not a shader

A shader over the back walls would read the walls' shapes round each pixel, 169 samples for the blur, on every back-wall
pixel every frame. The shadow is the same wherever the walls round a cell are, so it is worked out once per arrangement and
drawn as tiles, chunked as the rest of the grid. The atlas's slots (1024) are made at start, white: adding a tile to the tile
set later, or updating its texture through `ImageTexture.Update`, tells every layer drawing it to rebuild — 8–12 ms a time
while running into new ground. A new shadow goes to the renderer with `RenderingServer.Texture2DUpdate` instead.

## Measured

On this machine (RTX 4060 Ti), 1280 × 720, against main:

| What | Before | After |
| ---- | ------ | ----- |
| `--ground=sprint --autopilot --frames --for=60`, avg / p99 / max | 1.38 / 2.71 / 22.9 ms | 1.38 / 2.66 / 4.17 ms |
| frames over 4.17 ms | 6 | 0 |
| reveal, worst frame | 0.97 ms | 2.01 ms (new shadows worked out) |
| the ship's views made at start | 115–117 ms | 124–128 ms (the atlas's slots) |

## Deliberately not handled

- **Past 1024 different shadows** a cell shows none, with a warning once: a planet was seen to need a few hundred.
- **Doors, machines and plants cast no shadow;** only walls block the way, which is what the shadow tells.
- **The shadow is not lit by its own side:** in a lamp's beam it darkens the back wall by the same share as in daylight.

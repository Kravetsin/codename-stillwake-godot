# 0029 — Light composed in lightmaps

- Status: accepted
- Date: 2026-10-15
- Supersedes: the "lit by Godot's 2D lights" part of [0025](0025-godot-and-csharp.md)

## Context

Godot lights a drawn canvas item by at most 15 2D lights and silently drops the rest. Ship tiles are
drawn in chunks; a furnace's embers, each a light, pushed a lamp out of a chunk and the whole chunk
went dark with a hard edge. Ten furnaces on screen, or dense lamps, would do it again. The team will
not accept a hard limit. 0026 had rejected light by cells: the team wants sharp shadows from walls and
open doors and an evenly shaded ship.

## Decision

- No Godot lights. Lamps and fires, and apart from them embers, sparks and glows, are drawn each frame
  into two HDR lightmaps over the view, a texel a world pixel, one additive quad a light over its reach.
- Shadows are traced per texel: the quad's shader walks the cells to the light through a byte-a-cell
  occupancy texture and drops the texel behind a wall or a closed door's panel. Walls are lit on the
  faces light reaches. Lamps and fires have a width, so a shadow's edge is soft: rays to more points of
  the light are walked only where the two ends disagree.
- The lit shader reads the lightmaps unshaded: ambient, the stars and the main map scaled together to
  keep hue, the spots capped apart. Starlight stays the cell-by-cell sweeps of 0019,
  falling off within a few cells of where it comes in since [0070](0070-starlight-falls-off-where-it-comes-in.md).
- Relief by normal maps comes from the main lightmap's slope: a picture is shaded toward where the light
  grows, one direction a point for all lights together.

## Consequences

- Any number of lights may touch a thing.
- No dearer than Godot's lights, which drew every wall's shadow for every light each frame: 1.38 against
  1.61 ms a frame at zoom 1 on the reference world with 103 lamps lit, 0.96 against 0.97 at zoom 2.
- Cost grows with lights on screen times reach squared; caching still lights' shadows is the next step
  if that ever binds.
- A new light is a `LightSource` in `LightView`, or a `render/Glow` node for a small one.

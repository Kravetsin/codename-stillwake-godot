# wreck (client): a wrecked vessel's looks and what still moves on it

A wrecked vessel ([vessel-wrecks](../../../docs/decisions/2026-11-18-vessel-wrecks.md)) is drawn from its record (`VesselWreck`)
and the built vessel alone; the damage is never run again here. Namespace `Features.Wrecked` (`Features.Wreck` would hide
`Content.Wreck`).

## Files

| File | Holds |
| --- | --- |
| `WreckView.cs` | the looks painted a picture per 32-cell chunk and layer as the surface loads, a changed chunk painted again |
| `WreckLifeView.cs` | the motion: motes as multimeshes swaying in their shaders, drifters, sparks, vents, hot edges; embers' light |
| `logic/WreckScene.cs` | the vessel as the looks read it: cells, doors, machines, compartments, kept as the grid changes |
| `logic/WreckPaint.cs` | a Godot-free canvas the looks paint on: fills by coverage, strokes, clips to cells, tinted pictures |
| `logic/WreckLooks.cs` | each event's looks (the mockup's looks.js): soot, burnt rooms, smoke, rime, frost, petals, cables, pits |
| `logic/WreckLife.cs` | each event's motion (the mockup's fates.js): motes, drifters, sparkers, vents, glows |

## Rules

- **Looks by event:** an explosion's soot and streaks in the rooms the air left, heat tint, scorch and beads on the hull,
  petals and rib stubs at its edges, two cables; a fire's burnt rooms (tongues of smoke, the V at its seat, the floor charred,
  blisters; smoke hanging while fresh), plumes and warped doors where it passed, scorch on the shut doors that stopped it, sag
  and char on soft machines, burnt machines' pictures darkened, melted glass; icing first (power lost) rime in every room, every
  window frosted, every machine paled, pipes burst in frozen streams; a breach's icing (and every asteroid's own) an ice fan,
  hull frost, rime and icicles in its frozen rooms; an asteroid's petals and stubs; a break's petals, cables and pipes at its
  faces, folds and stress cracks (overload) or heat (an explosion's); scavenged, pits on the skin, patches, the livery faded.
- **Age:** an old group's soot, streaks, heat, scorch, burnt rooms, plumes and beads at 0.55; its frost in old colours; no smoke.
  An old group keeps a sixth of its motes (rivets two in five), twice as spread, ice dull, no embers; a third of its drifters,
  farther out; no sparks, vents or glows.
- **Motion:** motes by the mockup's clouds, fans, room dots and trails; dust in a lit lamp's or ember's reach warm; ice and fuel
  glinting; embers flickering; drifters (shards, bits, stones, thrown machines) sway on their scatter line and turn, decoration
  only; fresh events spark at torn edges and cut cables; vents from burst pipes, torn pipes and a tanker's broken bay.
- **Lamps** are the light feature's (`VesselLights.ByWreck`): dark when the last group is old or the power went first; dark in
  burnt and vented rooms, a few stuttering; after a break the stern blinks red, the bow dark but one sealed room lit.
- **Layers:** room looks at `WreckRooms`, hull looks at `WreckOver`, motes and drifters at `WreckMotes`, sparks, embers and
  glints added at `WreckSparks`. All lit as the hull but the added ones.
- **Costs:** looks painted once as the surface loads (about 70 ms on a liner with three events); a chunk whose cells changed
  painted again, one every 100 ms; motes cost the GPU only, by the chunks in view; drifters, sparks and vents walk the
  chunks in view each frame.

## Deliberately not handled

- Drifters are flat shapes and pictures, not inked art; a repainted chunk can cost a frame about 11 ms.
- The body in a suit is a placeholder picture till its pick.

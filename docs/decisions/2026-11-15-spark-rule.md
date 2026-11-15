# A tool's haze in matter throws that matter's look, whatever was hit

- Status: accepted; supersedes where sparks start and how many a running tool throws in
  [grinder-contact](2026-11-14-grinder-contact.md), [drill-contact](2026-11-14-drill-contact.md),
  [blade-sparks](2026-11-15-blade-sparks.md), [cutter-contact](2026-11-15-cutter-contact.md),
  [tool-contact](2026-11-05-tool-contact.md) and [damage-looks](2026-11-05-damage-looks.md)
- Date: 2026-11-15

## Context

Each tool had its own spark code: the grinder's rim exits, the drill's helix exits, a struck blade's trail on a wreck, the
cutter's slag, the ground's face throw. Each tied the sparks to the cell or wreck the rays damaged. The rays come from the
shoulder and the blade is drawn by its swing, so the two part: striking down, the floor took the hit 20–25 px above where the
spear's tip was drawn, and the sparks came off the floor, not the blade. Every fix was a patch: a 16 px reach round the hit, the
haze's nearest point, a kept sweep. The user asked for one rule, simple and the same for every tool, and pointed out it is only
the client's picture.

## Decision

- **A haze is points with a way** (`HazePoint`): a struck blade's edge swept frame to frame (the spear's thrust too), the
  grinder's rim along its spin, the drill's two strands back to the chuck, the cutter's jet. Each knows the next point of its
  path (`Next`). A blade has a haze only while it is swung, a running tool while it runs: one carried as the hero runs throws
  nothing.
- **Where it lies in matter it throws** (`ToolHaze.Touches`): every frame, every point of every haze in use is tested against
  the wrecks' pictures as drawn and the cells a tool would hit. A point inside throws the look of what it is in
  (`HitLooks`): metal sparks, stone sparks and crumbs, soil only crumbs. Nothing asks which cell or wreck the rays damaged.
- **How much**: `WorkLooks.HazeRate`, 10 of the axe's hits a second while a haze lies in matter, shared among what it lies in
  by how many of its points lie in each, from three of them picked afresh each frame. One number for every tool: a blade
  throws while it sweeps through, a running tool all along.
- **How they fly** (`ToolHaze.Ridden`, `Glim.Rides`): each runs along the haze's path through the matter to where it comes
  out, its end or 48 px, then flies off the way the path ran, glanced off the surface if that leads back in. Streaks run all
  of it, chips 8 px, motes and wisps 3 (`WorkLooks.RideOf`). Fanned by kind, not material (`WorkLooks.HazeSpread`).
- **A hit is heard and cracks** where the rays land, as before; it throws nothing of its own. A wreck a haze lay in within a
  trail's life (`Ui.HazeTouched`) throws nothing on its hit either; anything without a haze still throws from the face.

## Consequences

- The grinder's sparks run round its rim and leave where it comes out, the drill's spiral back out of the hole, a blade's and
  the spear's come off the edge all the way it passes through, the cutter's off its jet's end.
- About 800 particles cost 0.85 ms of CPU a frame on the effects bench, a microsecond each; the cap stays 1600.
- Removed: `ToolSparks`, `BladeBites`, `ToolContact`, the hit's face throw, the 16 px reach, the nearest-point fallback.

### Deliberately not handled

- A blade drawn into rock past the face its rays hit sparks from inside the rock, glancing out; the drawn swing is not stopped
  at the face.
- A haze that never touches what the rays hit throws nothing; the hit is still heard and cracks.
- The path is taken when a particle is thrown: it does not follow the tool for its fraction of a second.

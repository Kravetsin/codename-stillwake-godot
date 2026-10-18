# 0064 — Plans and marks are drawn alike

- Status: accepted
- Date: 2026-10-18
- Supersedes in part: [0022](0022-one-hand.md) ("Marks stay as they are")
- Design: "plans and removal marks in one style" (the user's decisions, 2026-10-18)

## Context

Plans had become pale ghosts in marching dashes that turn real as they are built (0063), while a thing
marked for taking down was still a red projection over itself with a white work bar: two languages for
one act, building and unbuilding. The dashes went round the cell box, so a vent, a lamp or a tank's legs
were boxed rather than outlined, and the ghost at 70% was faint in a lit room and against the stars.

## Decision

- **Marks look like plans, in red.** A marked thing keeps its own picture, as the grid draws it, inside
  red marching dashes. As it is worked it turns into its red ghost from the top down (a floor or a
  conduit from the right), the mirror of a blueprint turning real from the bottom up; the cut is at what
  is left of its work. The work bar goes: the ghost shows how far it has gone. A bar was kept only if
  nothing else showed progress; the ghost does, in the thing's own shape, and reads the same as building.
- **A mark's tip** shows what it is (picture and name) and what taking it down gives back, "+2" with
  the item's icon, in green, as the sim refunds it (`BuildingModule.RefundOf`).
- **Plans are more opaque:** 88% (back walls 65%), against 70% and 45%. The real part of a half-built plan
  is lit and coloured; the ghost stays pale and unlit, so the two still read apart.
- **The dashes follow the silhouette:** a strip just outside the opaque pixels of what is drawn, as
  wide as the aim's outline: a screen pixel at 1080 lines whatever the zoom, not a world pixel (2 screen
  pixels at zoom 2, too heavy beside it; changed after the co-op playtest of 2026-10-18). Dashes 3 line
  widths on in 5, marching 5 line widths a second clockwise, so they keep their proportions to the line.
  A group's shapes join, the sides they share dropped: a row of walls is one outline. Holes smaller than a
  cell (a ladder's rungs, a grille) are part of the silhouette; a room walled round is outlined inside too.
- **Found on the CPU, per picture, and kept.** Masks come from each picture's own image, never from the
  ship's atlas, which packs pictures with no padding. A shader outline over the atlas would sample
  neighbouring regions; bounds of each region passed to a shader would take an instance uniform or a node a
  plan, which 0063 keeps out; and neither could drop the sides a group shares. Pre-baked outline masks per
  tile would still need the merging. So: rims per picture and way of laying it, cached; merged and traced
  per group when plans change; machines the same way, one method for all.
- **Drawn as one mesh a layer,** a quad a side, each pixel carrying its place round its loop; a shader marches
  the dashes with the time. Built again only when the zoom or the screen's height changes, so the strip stays
  a whole number of screen pixels wide and crisp; one draw for all plans' dashes.

## Consequences

- Building and unbuilding read alike: dashes say "this will change", blue for coming, red for going; the
  ghost grows or shrinks with the work.
- Outlines are found again whenever the plans or the grid change: about 3 ms for 578 walls in one group
  on the reference machine, nothing between changes. Work alone never finds them again.
- An outline's dashes start from its top-left corner: a plan added to a group may shift the phase of that
  group's dashes, never another's.
- Two plans of one group overlapping would not merge cleanly; plans claim their cells, so they never do.

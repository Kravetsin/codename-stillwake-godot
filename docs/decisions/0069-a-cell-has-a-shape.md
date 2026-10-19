# 0069 — A cell has a shape

- Status: accepted
- Date: 2026-10-19

## Context

Cells were full or empty. A hero could not walk up a hill without jumping, which planet sorties will need,
and hulls could only be stair-stepped boxes where the ship-looks mockups had bevelled corners and diagonal
bows. The design (slopes) proposed shapes that seal by their sides — air, rooms, leaks and light reading
which side of a cell is open. That is most of its cost and most of its risk, and walking on slopes is
worth having without it. So this is part 1: shapes that only feet and the drawing read.

## Decision

- **A wall has a shape**, a layer of the grid (`Shape`, saved, a change-log flag `GridLayers.Shape`): none,
  or the solid half-cell triangle in one corner, cut at 45° — ◢ `BottomRight`, ◣ `BottomLeft`,
  ◤ `TopLeft`, ◥ `TopRight`. It is not none only on a wall, and clears when the wall breaks or is taken
  apart (`Clear`). Saves go from format 18 to 19, the step giving every cell none. A shape is a property of
  the wall, not a new structure: material, cost, refund, strength and leak are the wall's.
- **Maps write shapes:** `/` ◢, `\` ◣, `r` ◤, `7` ◥, walls of the default hull, so tests, dev maps and a
  future generator can draw them. Nothing builds one yet.
- **One geometry, integers, shared by the sim and the hero** (`Shape.Solid(shape, fx, fy)`,
  `Shape.TopAt(shape, fx)`, `Shape.ClosedSides(shape)`, thousandths of a cell): a point stands for the
  thousandth square below and right of it and is solid when that square's middle is, so ◣ is exactly ◢ in
  a mirror and a ramp of ◢ rises a thousandth a thousandth, with no step between its cells or onto the
  plateau it leads to. No division anywhere.
- **A shaped cell is a full wall for everything but feet and the drawing**: rooms, air, atmosphere, leaks,
  drones, light and starlight read it as a wall, permanently — air by sides is dropped, not deferred. Its
  open half is no room's and holds no air; a stack lying in it falls as aboard, even out in space.
- **The hero** (`game/features/crew/Hero.cs`): whole cells hold the whole box; a floor slope (◢ ◣) holds
  the middle of the feet on its diagonal, and beside the middle the box at its point nearest the middle.
  - A walk moves in parts that end where the box's leading edge or its middle crosses into another column,
    the feet stepping up onto ground no more than half a width above them, the head permitting, and
    sticking to ground that fell by no more than the part walked: up and down a ramp without a jump, and
    never a frame in the air going down.
  - A slope's closed sides (◢'s right, ◣'s left, both bottoms) stop the box like a wall; through its open
    side the box goes in and the feet climb.
  - **The low ledge rule** (standing, a wall whose top is no more than half a width above the feet does not
    stop a walk) is `≤`, not `<`: at 45° the box's leading edge meets the next column exactly half a width
    above the feet, so with `<` every ramp would stop at its top.
  - **Mixed footing** stands on the higher, but a corner no more than half a width above the ground under
    the middle gives way to it: walking off a plateau down a slope the feet follow the slope, rather than
    hang on the plateau's corner until the box clears it and then drop half a cell.
  - Only the lowest half width of the box may overlap anything — the uphill side of a slope, a ledge being
    stepped onto; a move that would end with more overlapped, or the feet in anything, is not made
    (`Hero.Clear`).
  - Ceiling slopes (◤ ◥) are whole walls to the hero; swimming and being pulled, every shape is.
  - Down on a slope does nothing. The one-cell-hole catch and down through floors are unchanged.
- **Stacks** (`Items`' `FloorRules`) read the point, not the cell: one lies on a floor slope's diagonal at
  its x and is pushed out only from the slope's solid half; thrown into one no steeper than it moves, it
  skids up onto it. A ceiling slope is whole to them.
- **Drawing:** the existing wall pictures cut by four masks at load (`render/ShapeMasks.cs`), no picture per
  material and shape; the diagonal edged in the material's own edge colours; edges on a shaped wall's closed
  sides only, and on a neighbour only where the shaped wall's facing side is open. The aim outline traces
  the solid half.

## Consequences

- Part 2 adds the build UI (the plan's R cycles the four shapes, `PlacePlan` carrying one, the ghost
  showing the cut), ceiling slopes the hero's head follows, and diagonal light (occupancy codes per shape,
  shadows along the diagonal). Each is a new decision; none needs the geometry or the save to change.
- A hull bevel's open half is part of the wall: a room never reaches into it, and a corner cut to a slope
  seals as the whole wall did. Stars do not shine into it; lamps cast square shadows from it.
- The box overlaps a slope's uphill side by up to half a cell, and a ledge's top as it is stepped onto: a
  rectangle on a 45° ground. The figure is drawn by its feet, so it reads as standing on the diagonal.
- A floor slope with nothing under it, entered from its open side at head height, lets the box's lowest
  half width in only: higher up it stops the hero like a wall. Maps draw slopes on ground, and no player can
  build one yet.

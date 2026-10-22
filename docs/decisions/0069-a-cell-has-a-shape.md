# 0069 — A cell has a shape

- Status: superseded by [0106](0106-steps-replace-slopes.md)
- Date: 2026-10-19

## Context

Cells were full or empty. A hero could not walk up a hill without jumping, which planet sorties will need,
and hulls could only be stair-stepped boxes where the ship-looks mockups had bevelled corners and diagonal
bows. The design (slopes) proposed shapes that seal by their sides — air, rooms, leaks and light reading
which side of a cell is open. That is most of its cost and most of its risk, and walking on slopes is
worth having without it. So this is part 1: shapes that only feet and the drawing read. Part 2, below, builds
them, lets the head follow ceiling slopes and the light follow the diagonal; a shaped cell stays a whole wall
for air, rooms, leaks and drones.

## Decision

- **A wall has a shape**, a layer of the grid (`Shape`, saved, a change-log flag `GridLayers.Shape`): none,
  or the solid half-cell triangle in one corner, cut at 45° — ◢ `BottomRight`, ◣ `BottomLeft`,
  ◤ `TopLeft`, ◥ `TopRight`. It is not none only on a wall, and clears when the wall breaks or is taken
  apart (`Clear`). Saves go from format 18 to 19, the step giving every cell none. A shape is a property of
  the wall, not a new structure: material, cost, refund, strength and leak are the wall's.
- **Maps write shapes:** `/` ◢, `\` ◣, `r` ◤, `7` ◥, walls of the default hull, so tests, dev maps and a
  future generator can draw them. Players build them too since part 2.
- **One geometry, integers, shared by the sim and the hero** (`Shape.Solid(shape, fx, fy)`,
  `Shape.TopAt(shape, fx)`, `Shape.ClosedSides(shape)`, thousandths of a cell): a point stands for the
  thousandth square below and right of it and is solid when that square's middle is, so ◣ is exactly ◢ in
  a mirror and a ramp of ◢ rises a thousandth a thousandth, with no step between its cells or onto the
  plateau it leads to. No division anywhere.
- **A shaped cell is a full wall for everything but feet and the drawing**: rooms, air, atmosphere, leaks,
  drones, light and starlight read it as a wall, permanently — air by sides is dropped, not deferred (part 2
  lets light and starlight read its halves; to air, rooms, leaks and drones it stays whole). Its
  open half is no room's and holds no air; a stack lying in it falls as aboard, even out in space.
- **The hero** (`game/features/crew/Hero.cs`): whole cells hold the whole box; a floor slope (◢ ◣) holds
  the middle of the feet on its diagonal, and beside the middle the box at its point nearest the middle.
  - A walk moves in parts that end where the box's leading edge or its middle reaches the last thousandth of
    its column, then crosses a thousandth into the next, the feet stepping up onto ground no more than half a
    width above them, the head permitting, and sticking to ground that fell by no more than the part walked:
    up and down a ramp without a jump, and never a frame in the air going down. Crossing a thousandth at a
    time puts the feet a thousandth into the next step's diagonal, so a stair of slopes with air under its
    steps, as players build them, is walked like a ramp on solid ground (fix of 2026-10-21).
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
  - Ceiling slopes (◤ ◥) are whole walls to the hero (part 2: the head follows them); swimming and being
    pulled, every shape is.
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
  seals as the whole wall did. Until part 2, stars did not shine into it and lamps cast square shadows from it.
- The box overlaps a slope's uphill side by up to half a cell, and a ledge's top as it is stepped onto: a
  rectangle on a 45° ground. The figure is drawn by its feet, so it reads as standing on the diagonal.
- A floor slope with nothing under it, entered from its open side at head height, lets the box's lowest
  half width in only: higher up it stops the hero like a wall. Maps draw slopes on ground; a player who builds
  one in mid-air meets that edge.

## Part 2: building slopes, ceilings and light (2026-10-19)

### Decision

- **A blueprint carries the shape.** `Placement` has a `Shape` (none by default, so older saves' plans and every
  other caller read as before); `PlacePlan` sends it, the plan keeps it, and the wall is built cut to it
  (`IGridOps.Place`). The grid refuses a shape on anything but a hull wall — a floor, ladder, door, back wall,
  conduit or machine — or a code that is no shape, with `grid.notShapeable`, asked before anything else. A shaped
  wall costs, takes and gives back what a whole one of its material does; a shape is changed by taking the wall
  apart and building it again (no hammer). The fuzz draws walls of every shape and shapes where none may be.
- **R in the plan.** With a hull wall's card in hand, R turns the shape walls are planned in — whole, ◢, ◣, ◤, ◥,
  round again — and Shift+R back. It sticks for every wall card until turned again, so a whole bevelled hull is
  dragged without pressing R each time; a drag plans every cell of its line in it. The ghost, the card thumbnails,
  the plan, its marching dashes and its filling as it is built all use the cut pictures the grid draws with, so they
  follow the cut with no code of their own. The pipette picks a wall with its shape. A wall planned beside a slope
  runs on into it only where the slope's facing side is whole, as built walls are edged.
- **The head follows ceiling slopes** (`Shape.BottomAt`, the mirror of `TopAt`): rising, the box's top stops where
  it first meets a ◤'s or ◥'s diagonal, in the row the head is in too; through a ceiling slope's open side the box
  goes in under it till the head meets the diagonal. So a hero stands and walks under a bevelled corner as far as
  its open half gives the head room. It stops there: no slide along the diagonal, as there is none under a flat
  ceiling. Only the box's lowest half width may still overlap anything; the head never does.
- **Light along the diagonal.** The lightmap's occupancy texture tells each shape apart (`Sources.Slope` and the
  shape's code, 33 to 36 of 255), and the shader treats a shaped cell's solid half as a wall and its open half as
  the open: the solid is a half plane in the cell, so the side of the diagonal the line's two ends are on decides
  it. Light passes through an open half, a slope's shadow runs along its diagonal, and its lit face is the
  diagonal (taking `Light.WallIn`, as any wall's face). Starlight is still worked out a cell at a time: a shaped
  wall with no back wall takes the stars, as its open half shows them, but is a wall to what crosses it.

### Consequences

- The reference world's light costs what it did (1.20–1.26 ms a frame at zoom 1 before, 1.23–1.28 after, within
  the runs' spread; the same at zoom 2); with five slopes in every room, 1.16 ms.
- Starlight has one texel a cell, so a shaped cell's texel is its wall's face: a picture drawn in its open half
  (a hero, a back wall) takes the light of the open cells beside it, not of the stars in the half.
- A diagonal of ◤ or ◢ is a straight wall to light on its diagonal side and a sawtooth on the other, as drawn.
- `-- --slopes` (`game/dev/SlopesDemo.cs`) shows it all: a bevelled hull being built with R, a lamp under a strut
  of slopes, a corridor whose top corner the hero walks in under.

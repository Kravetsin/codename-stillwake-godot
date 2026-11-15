# A hero's near arm and what it holds over the grass

- Status: accepted; amends [0082](0082-the-grounds-chipped-look-details-and-living-edge.md) for the hero's hands
- Date: 2026-11-15

## Context

The tufts of a planet's living edge are drawn before the heroes' feet (`Layers.GroundFront`, 18), so boots stand in them. The
whole hero was one layer under them (`Hero`, 17), the weapon in its hand too. Running on a planet, the user saw the weapon
drawn over the ground but under the grass.

## Decision

- **A new layer, `HeldFront` (19), over `GroundFront`.** A standing hero's near arm and everything drawn over its body is on it:
  - the plan, the tool and the held item, the near arm, the collar and the air at the shoulder;
  - the held stage's effects between the tool and the arm;
  - the cape's outside.

  The body and the legs stay under the tufts.
- **Set by the views that stand heroes in the world** (`Figure.HandsLayer`): this player's and the others'. A body, a seated hero
  and the title's figures keep their figure whole on their own layer.
- **What must stay over the tool moves up with it:**
  - `ToolWork` (the haze's sparks) draws at `HeldFront`, after the figure.
  - `Drones`, `Gas`, `Water` and `Dug` move from 18 to 20. Water still covers a swimming hero's tool, and the dig's cracks stay
    over the drill that bites them.
  - Every layer above them moves up by two.

## Not handled

- Another player's near arm draws over this player's body where they stand together; before, the whole other hero was under.
- The back hand's item (`_offHeld`) stays behind the body, so under the tufts.

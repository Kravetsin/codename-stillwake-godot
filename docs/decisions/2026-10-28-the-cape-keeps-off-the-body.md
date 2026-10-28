# The cape keeps off the body

- Status: accepted; built
- Date: 2026-10-28
- Builds on [2026-10-27-the-cape-is-worn](2026-10-27-the-cape-is-worn.md); amends how its cloth is kept off the hero

## Context

The cape's cloth (`game/features/crew/logic/Cape.cs`) was kept from wrapping the hero by one rule: no point more than
`Give` (1 px) forward of where it hangs as cut. The cut's front edge runs from the throat to the heel, through the
torso and the front leg, so that line passes through the body. Walking backward, the air pressed the cape against it and the
cape stood still as a flat plate cut across the hero; stopping after a run it could not swing forward; a point lifted
to the waist was held by its own column's line at the feet, not by the body there. The user found it digging into the hero.

## Decision

- **A body zone, not a line per point.** `Cape.BackOf(y, front, back)` is the body's back at a height of the figure's
  frame: the torso from the neck to the hips, narrowing into the legs over `Seat` (6 px) so cloth slides off it, and
  each leg, thigh and shin, as posed. `Figure` passes the shown legs to `Cape.Step`.
- **Points cut behind the body** come forward only to the body's back at the height they are, keeping `Fullness` (0.3)
  of how far behind they were cut, plus `Give`. Walking or running backward the cape folds forward against the back and
  legs; stopping, it swings on forward; a leg swung back kicks the hem.
- **Points cut beside the body** (its near side: the front edge, the top rows) keep the old rule, `Give` forward of the
  cut. Tied to the body's back they jumped as they moved up and down past the hips and crumpled the front edge.
- **The run's lift only trails a run the way the hero faces:** running backward the air presses the cape against the
  body rather than lifting it. `Push` 0.1 → 0.05 and `Lift` 0.055 → 0.035: a sprint trailed it near horizontal, now
  about 45°, the drag of `Keep` already pushing it back.
- **The `--cape` demo walks and runs backward** facing on; with no pointer, `HeroView` lets a script's `Face` stand.

## Deliberately not handled

- **Only horizontally:** a point is moved straight back out of the body, not along the body's surface.
- **The arms, the pack and held items** do not push it; the pack was already none of its business.
- **No walls or steps:** only the floor under the feet, as before.
- **Its back corner at the hem** may turn over a pixel or two running backward, showing the lining at the fold.

## Consequences

- Hanging still, it hangs as cut within 1.5 px rather than 1.
- Tests in `game.tests/CapeTests.cs`: running backward it folds forward but never into the body nor past the near
  side's cut; a stop swings it forward; a leg swung back pushes the hem back. Each fails with the old rule.

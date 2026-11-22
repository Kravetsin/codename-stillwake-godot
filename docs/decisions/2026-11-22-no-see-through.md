# The right button held fades nothing

- Status: accepted; supersedes in part [0107](0107-left-is-front-right-is-back.md) (the fade round the pointer with the
  right button held) and [0110](0110-the-plan-has-a-focused-layer.md) (that fade kept for the hand tools, `SeesThrough`)
- Date: 2026-11-22

## Context

The right button held aboard faded the walls round the pointer. It served digging back walls with that button (0107),
which it no longer does. The user could not tell what it was for and asked to cut it, and the companion's abilities
need the button ([companion-fights](2026-11-22-companion-fights.md)).

## Decision

- The right button held fades nothing, with any item or none. The fade's rule (`PlanHand.SeesThrough`) is gone.
- The plan with the back walls focused still clears the front round the pointer
  ([back-focus-faint](2026-11-12-back-focus-faint.md)).

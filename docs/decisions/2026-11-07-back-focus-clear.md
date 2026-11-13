# The back walls' focus hides the front

- Status: accepted, superseded in part by [back-focus-faint](2026-11-12-back-focus-faint.md); supersedes in part [0110](0110-the-plan-has-a-focused-layer.md) (the front drawn at 35% with the back focused)
- Date: 2026-11-07

## Context

With the back walls focused (0110) the front was drawn at 35% over them, lit as ever. Played, the front still covered them
and a dark room's back walls were too dim to tell their material.

## Decision

- **With the back walls focused the front is hidden** (`Tools.FrontInBack`, 0): walls, doors, machines and their looks,
  cables, pipes, and the front's plans and their dashes. Heroes, items and marks stay.
- **The back walls are drawn as painted, out of the light** (`Tools.BackUnlit`, `GridView.Fade`), the ground's shade off.
- With the front focused nothing changes: the back walls at 35% (`Tools.Faded`).

## Accepted limitations

- Where a door or a machine stands is not shown while the back walls are focused: the back walls themselves draw the rooms.
- A mark on something of the front stays drawn over the back walls.

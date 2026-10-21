# 0090 — The figure leans, skids and lands by its motion

- Status: accepted
- Date: 2026-10-21

## Context

After the controller's feel (0089) the figure still showed the old poses: upright whatever the acceleration, the same
220 ms landing from a hop or a roof, and in the air two fixed poses swapped at the top of a jump. The research page that
proposed 0089 drew these too, in its JS copy of `Pose.cs` and `Figure.cs`, and the user approved items 4, 5 and 6 with
"numbers: as proposed": the lean and skid, the landing by the fall, the air poses; and the dust of item 15 only where these
need it.

## Decision

Drawn only, in the client: `game/features/crew/Motion.cs` remembers the motion frame by frame and hands `Poses.LimbsOf` a
`Momentum` (lean, skid, landing depth, time since a jump, time falling); every number is in `game/Content/HeroMotion.cs`,
the rig's poses stay limb angles in `Pose.cs` (`Poses.Airborne`, `Poses.Skidding`), so a redesigned hero on the same rig
keeps them.

- **Lean:** on the floor, 0.006 rad per thousandth of a cell a step gained a step, the change smoothed by 0.3 a step,
  clamped to 0.25 forward and 0.22 back. The smoothing goes by time (0.7 to the power of the steps elapsed), so a friend's
  speed that changes a send (three steps) at a time leans its figure about as far as its own peer draws it.
- **Skid:** a step on the floor that loses at least 3 with over 50 left starts it; held 7 steps past the last such step,
  blended in and out over 45 ms; braking if the hero faces the way it slides, turned (leaning into its new way, soles
  dragging) if it slides backwards after a reversal. While it slides over 50, 0.7 streaks a step. Stopped dead from over
  50 to nothing, a wall took the speed: no skid.
- **Landing:** the sink of old times the fall's speed over 420, from 0.35 to 1.8, ×0.6 landing at over 60 across; it
  springs back in 150 ms plus up to 110 by the fall over 600. From 480 it is heavy.
- **Air:** blended from the rising pose to the falling one as the speed goes from -150 to 300, smoothstepped; within 60
  of still the knees tuck toward the top's pose by up to half; for 110 ms after a jump the legs push straight; falling
  faster than 300 for over 450 ms the arms flail, fully 300 ms later; faster than 100 across, the body tips 0.1 into its way.
- **The effects, in 0075's style:** a skid's streaks are thin pale streaks of light in the dust's colours, dragged to a
  stop in a fifth of a second, every second one with a mote of matter dust. A landing's flat ring now goes out by the
  fall (3 + 26 × fall/1200, at least a fifth of it); a heavy one's to 34 px, brighter (0.45) and longer (260 ms), with two
  pale streaks of light racing out along the floor to either side.
- **Friends see it:** all of it is read from what a hero sends (`X`, `Y`, `Vx`, `Vy`, `Facing`, the pose); no new field.

## Why these choices

- **The page's numbers, two readings made explicit.** Its code flailed after 0.45 s in the air; its text said "after
  0.45 s of falling". Counted in the air, a plain held jump aboard flails for its last steps; counted falling it does not,
  and the brief asked for flailing on long falls, so it counts falling. Its skid read the speed after the step; kept, so
  a hero running into a wall does not skid for the step it is stopped by.
- **The skid streaks are light, not matter.** 0075 makes dust matter, lit by the room; the boots' motes and a landing's
  grains all but vanish in a dim room (recorded in the poses hall), and the skid's must read. Light at 0.7 of a spark's
  brightness, in the dust's pale ramp, keeps them as fine as the rest.
- **The camera's 2 px dip on a heavy landing is left out:** the camera is another change (0089's left-out list). It can
  call `Motion.Heavy(fall)` on the landing `Figure` already detects; no hook is added before it has a caller.
- **The tilt settles in 60 ms on the floor** where it settled in 30, as the page drew it: a lean that snaps reads as a twitch.

## Deliberately left out

- **Looking down on a fast fall**, on the page: the head follows the pointer in the game, which the page did not have.
- **The jump's push-off dust** (item 15's six grains and two streaks): only the skid and the landing needed effects.
- **A friend's walk stopping** may show no skid: seen a send at a time, its speed can drop from 120 to 30 in one sample,
  under the 50 a skid needs. Its run's stop and its reversal skid as their own.
- **The hold's hit test** still assumes the body upright (0078): leaning into a start or a skid turns the drawn drill by up
  to a quarter radian round the body's middle for a moment, as the run's own lean already did.
- **The arms' flail is small** (0.35 rad on the front arm, 0.3 on the back), as proposed; it reads on a fall of twenty cells,
  barely on one of eight.
- **A head bumped on a ceiling** leaves the hero no speed for a step, which reads as standing for that frame, as before.

Recorded with `-- --poses --autopilot --lit --zoom 4` (`HeroAutopilot.Hall`, `PosesRun`); a friend with `--host` and a guest
`--join --aside`. Tested in `MotionTests`.

# Debris moves by the host player's physics, and the harpoon tows it on a rope

- Status: accepted
- Date: 2026-11-23
- Supersedes: [0006](0006-debris-in-the-simulation.md) (debris drifts in straight lines, nothing collides); the reel to a
  point of [0044](0044-the-harpoon-reels-a-piece-to-a-point.md)

## Context

Debris drifted in straight lines in the sim, and the harpoon pulled a piece to a point at a fixed speed. A prototype of rigid
bodies that collide and a rope with inertia was loved; the plan (`docs/plans/debris-physics.md`) moves debris onto Godot
physics on the host in four steps. Step 1 gave every piece a turned shape that tumbles ([debris-tumble](2026-11-23-debris-tumble.md)).
This is step 2: bodies and the rope, for a host without guests.

## Decision

- **The sim keeps poses, the host player's client moves them.** Each piece keeps a saved motion `Vx`, `Vy` in thousandths of
  a cell a tick, `(-Speed, 0)` as it spawns; every peer runs `X += Vx`, `Y += Vy` each tick. A piece off the grid on any side
  is gone. The sim eases and collides nothing.
- **Bodies** (`features/debris/DebrisBodies`): on the host player's client one rigid body a piece in its own PhysicsServer2D
  space, its box `ShapeShare` (800‰) of its picture, mass its area times `Density`, no gravity, eased back to its own drift
  and tumble by `Drag` (25‰ a tick). Bodies meet only each other, never the hull.
- **The feed:** after each tick, `Moved(ForTick, Bodies, Lines)` carries every body whose worst corner strays more than
  `FeedSlack` (an eighth of a cell) from the sim's pose, at most `FeedMost` (64) a tick, the farthest first, with each line's
  length. The sim sets each body and runs it on from `ForTick`, the debris clock. Anyone but the host player is refused
  `debris.notHost`.
- **Lines** replace `Reel`: a saved `Line { By, Piece, HookX, HookY, Length, Winds }`, one a hero and one a piece, the hook in
  the piece's own frame. `Hook` starts it as long as the span, at most `Harpoon.Range`; the last hook on a piece wins. Holding
  the left button sends `Wind(true)`, a fresh press needed after a hook; E stays the grab. `LetGo` drops it, and so does the
  holder leaving, passing out or being in a room, its piece going for any reason, or a sentry's cut (`Unreel`).
- **The rope** (`logic/PieceRope`, the prototype's `rope.gd`): pays out under load up to `PayOut` (10 cells) past the hooked
  length, winds in at `ReelSpeed` to `Stop` while `Winds`, braking by `Brake` over `BrakeRamp` ticks.
- **Drawn as the physics has it:** the host player draws its bodies between physics steps and its lines' lengths straight from
  the physics (`Shared.LiveDebris`); a line hangs by its slack, the rope's length past the span from the gun to the hook, so it
  sags while slack and is straight once taut.
- **Saves:** format 74; old reels are dropped.

## Deliberately not handled

- **Guests:** nothing of theirs is fed yet; co-op is step 3 (a surface-addressed `Moved`, guests' anchors, `FeedSlack` under
  lag). Meanwhile, a host player off the debris surface feeds nothing.
- **Bursts push no pieces** until step 4.
- **A spawn on another piece** may pop apart once; contacts are not saved, rebuilt from the poses after a load.
- **A delivery knocked off the grid is lost,** as one past its end.
- **The wire trusts the host player's bodies,** as 0002 trusts the host among friends.

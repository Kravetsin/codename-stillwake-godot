# The grinder's sparks leave its haze's rim where it comes out of a wreck

- Status: accepted; supersedes the grinder's haze size in [tool-reach](2026-11-05-tool-reach.md) and its sparks' start in
  [damage-looks](2026-11-05-damage-looks.md); its no-exit fallback superseded by [drill-contact](2026-11-14-drill-contact.md)
- Date: 2026-11-14

## Context

A running grinder threw its sparks from one point, where a ray of its fan first met each wreck, always back and down. With the
wheel already inside a wreck they sprayed from inside it. Its haze was drawn 11 px round the hub, smaller than both its zone
(`DiscRadius`, never drawn) and the fan's reach. The user:

> Предлагаю эффект показывать именно на краю марева. То есть по кругу марева будут летать частицы и вылетать уже где-то внизу
> этого круга. Тоже самое для любых других эффектов. Та часть, которая коснулась обломка, будет высекать искры.

Then chose to grow the haze to the tool's real reach.

## Decision

- **The working circle** is round the hub as the figure draws it, out to the fan's `Range` from the shoulder
  (`WorkLooks.WheelRadius`); none when that leaves under `WheelLeast` px. The haze draws it, feathered over half its radius.
- **Spin:** clockwise on screen facing right, mirrored facing left, as the figure flips.
- **Exits** (`ToolContact.Exits`): for each wreck the fan touches, the rim is walked `RimSteps` points along the spin against the
  wreck's drawn pixels, the test the fan uses. Each stretch inside ends at an exit, the last point inside. A stretch across
  the walk's start is one stretch. At most `WheelExits` a wreck, the longest stretches first.
- **Sparks:** a wreck throws at the same rate as before, whatever its exits. Each spark starts anywhere on the rim, rides it
  along the spin at `WheelTurns` turns a second to the next exit, up to a whole turn, and flies on along the rim's way there,
  spread `GrinderSpread`. So the whole haze's rim runs with sparks, and they leave only where it comes out of the wreck.
  Facing right into a plate ahead, they leave low on the front and fly down and back, as before; a plate below throws them up
  and back.
- **No bead.** The user, seeing the first build: «надо убрать эту точку, с которой летят искры… инструмент как будто должен
  сам генерировать искры». The grinder draws no white bead, at an exit or at a touch; the drill and the cutter keep theirs.
- **No exit** (the rim all inside, or a wreck inside the circle the rim misses): sparks come from the fan's touch, as before.

## Deliberately not handled

- A fan ray past the circle that touches a wreck cracks it without sparks off the rim: the fan is wider than the disc, and
  narrowing the sim's fan is a balance change of its own.
- The drill has no drawn haze, and its sparks already start where it enters matter; it joins once it has a drawn zone. Struck
  tools and the cutter are unchanged.
- The ground's chips stay where they were, still skipped while a wreck is touched.
- A riding spark follows the rim it was thrown on: a wheel moved mid-ride leaves it on the old circle for under a quarter
  turn. Sparks share a wreck's rate round the whole rim, so they are sparser at an exit than when they started near it. A friend's sparks may differ by a frame between screens; a friend's first frame falls back to the touch.

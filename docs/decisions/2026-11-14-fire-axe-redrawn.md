# The fire axe traced from a picked reference, its trail following the rounded edge

- Status: accepted; amends [blade-trail](2026-11-10-blade-trail.md): the trail is taken along the edge's shape, not only its ends
- Date: 2026-11-14

## Context

The fire axe was still the pixel art's shape inked over: a tall head, a ragged straight edge, five thin wraps. A first
redraw by eye from a generated reference kept the reference's thick haft and small head: it read as a throwing hatchet
(the user). Its edge is rounded, and a trail drawn from the edge's two ends cut it straight.

## Decision

- **Picked from five generated references, traced.** GPT drew five fire axes (long, pick-head, bearded from hull plate,
  rescue, heavy-headed); the user picked «Пожарный, тяжёлая головка» on a pick page. It is traced as the quilted helmet
  ([traced art](2026-11-14-quilted-helmet-and-traced-art.md)): `refs/fireAxe.png` → `traced/fireAxe.json`, drawn in
  `zfittings.js` by `traced.js` with our pen, paint and finish. Its paint meets bare steel with no ink line, so a palette
  may now cut parts by colour (`"segment": "colour"`): each pixel takes the nearest material, the ink still parting pieces.
- **34 × 13 pixels**, the pickaxe's length, at three quarters (tool-size): a big red head with a squared steel poll and a
  broad bare bevel, a dark collar with two rivets, a slim wooden haft, three wraps and a cap. Its grip, edge and clamp
  are measured from the trace.
- **An edge may bow.** `HeldLook.Bow` is how far the middle of an edge stands out of the line between its ends; the axe's
  is 1.23 pixels, down. The figure samples the edge at `HeldLook.EdgePoints` points along that curve every frame, and
  `BladeTrail` sweeps the band from the edge as it was to the edge as it is: the trail's ends are the blade's own curve.
- **Reach stays the ends'.** The bow is for the trail; what a swing hits is still set by the edge's ends and the weapon's
  range.

## Not handled

- The other swung tools have straight edges and no bow; a new blade with a rounded edge sets its own.
- The bow is a parabola through the ends, its peak at their middle, as the axe's is. An edge of another shape (a hook, a
  notch) would need its points listed.
- The traced drawing is data in the subject: changing the axe means tracing again, not editing its paths by hand.

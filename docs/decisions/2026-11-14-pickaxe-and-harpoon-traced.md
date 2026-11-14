# The pickaxe and the harpoon traced from picked references

- Status: accepted; redraws the harpoon of [0044](0044-the-harpoon-reels-a-piece-to-a-point.md) and the pickaxe of [the pickaxe](2026-11-07-the-pickaxe.md)
- Date: 2026-11-14

## Context

Weapons move one by one to pictures traced from a picked reference, as the [fire axe](2026-11-14-fire-axe-redrawn.md) and
the [drill](2026-11-14-drill-redrawn.md) did. For both, the user picked the reference that redraws the current shape.

## Decision

- **The pickaxe**, «Нынешняя, перерисованная»: a pipe haft taped at the butt, two welded collars, a rusted plate head with
  its hole, a steel spike down. Traced whole (`refs/pickaxe.png`, the hole kept by `holes`), 34 × 19 pixels. Its grip, the
  spike's point and the clamp are measured from the trace; on the arc bench it is clamped by its head, since its spike hangs
  too far under the haft to clamp it by the pipe.
- **The harpoon**, «Нынешний, перерисованный»: a wooden grip, a steel frame with the spring seen through a long window, a
  canvas-wrapped bottle in two clamps. The harpoon is mended out of the reference and drawn in `hero.js`: the gun is 26 × 11
  empty, 34 × 11 loaded with the shaft and two tiers of barbs out past the muzzle, and the same head flies on the rod. The
  spring's window is too dark for the trace to keep, so it is drawn over the trace too.
- **The line ends at the gun only while the rod's eye is in it**: the gun on the rod, between its eye and its point.
  Before, a hero who flew past a stuck rod, behind its point as it flew, saw the line shrink into the gun and vanish.
- **The rod sticks in a wreck where it is drawn.** A wreck's box with its margin still says what the rod may meet, but
  it is met only where its picture is opaque, as drawn and turned (`WreckPixels`, as a tool's rays meet it), the rod sunk
  a bite past that point. Before, it stuck in the empty corner of a rock's box, in the air beside it. Small pieces keep
  their box, which makes them easier to catch.

## Not handled

- The pickaxe's spike reaches farther from the fist than the old one's: its edge is the trace's point, so its swing reaches
  about a fifth farther. If that plays too long, the picture shrinks, not the edge.
- The grapple and the nailer still share the old plank stock; each gets its own pick.

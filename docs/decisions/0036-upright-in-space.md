# 0036 — Upright in space

- Status: accepted; its last consequence, others showing no axe, superseded by [0037](0037-others-see-the-hands.md)
- Date: 2026-10-16

## Context

Afloat, the hero swam as a swimmer does (ported from the TypeScript `pose.ts`): the body turned head
first the way it swam, flat along the floor and head down sinking, a breaststroke slow and a crawl
fast, treading water still and now and then windmilling in a panic. With anything in the front hand,
the fire axe (0034), the plan's sheet or the screwdriver (0030), it read wrong: the body lay sideways or
on the diagonal and the item stuck out stiffly from an outstretched arm. Small pixel art also reads
worse turned than upright.

## Decision

- **Always upright afloat, leaning into the motion.** Swimming along the floor the body leans toward
  the way it swims, 0.22 rad (about 13°) and 0.38 rad (about 22°) swimming fast; sinking it tucks
  forward 0.06 rad more; rising it stays upright. Never more than about 25°. The lean eases in and out
  over about 450 ms, and still the body only rocks a few degrees. No swimming head first, no somersaults.
- **Legs trail, hands paddle.** The legs trail behind a swim along the floor, are drawn up sinking and
  hang rising; the free hand paddles, both with nothing in the front one; the hand holding the axe keeps
  calm. Still, the hero drifts weightless: arms floating a little up, knees loose.
- **Held items as on the floor.** The axe, the sheet and the screwdriver use the floor's arm and hand
  logic on the floating body; at work the paddling stops and the hero drifts upright, as before.
- **The same for every hero.** It is all in the client's figure (`Pose.cs`, `Figure.cs`): nothing in
  the sim or on the wire changes, and others' heroes float the same way.

## Consequences

- The swimming feel is traded for a lean and trailing legs: the breaststroke, the crawl, the turn head
  first and the arms reaching ahead of a turning body are gone.
- The panic bout adrift (arms windmilling now and then) is gone with treading water: whirling the axe
  read as wrong as the stroke did, and a still hero now drifts calmly.
- Passing out in space still leaves the body turned as it was (`Pose.Tumble`), now at most the lean.
- Others' heroes do not show the axe (0034), so their front arm paddles like an empty one.

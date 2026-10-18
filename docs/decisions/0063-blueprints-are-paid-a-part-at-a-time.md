# 0063 — Blueprints are paid a part at a time

- Status: accepted
- Date: 2026-10-18
- Design: "gradual building" (the user's decisions, 2026-10-18)

## Context

A blueprint took everything it cost from the hero on the first press, before any work: a sweep of the
pointer over a row of plans emptied the pockets into them, then holding on built them. Nothing on screen
told the two apart but an amber bar, then a white one. Raft, Valheim and Grounded spend as they build, and
a plan that fills in as it is built shows its progress without a bar.

## Decision

- **Parts.** A blueprint's work is split into as many parts as its cost has items: a scrap wall's two, a
  vent's two (a plate, a tube), a tank's five. Part k of N begins at tick `BuildTicks * k / N`, rounded
  down: the first with the first tick, and the remainder falls to the later parts (a scrap wall's 13 ticks
  split 6 and 7). Content gives every build a tick an item at least, so no part is empty.
- **Paying.** The tick of work that begins a part not paid for first takes one item from the working hero:
  the first in the cost's order that the blueprint still lacks and the hero carries. The order is fixed;
  to the player it reads as any order. A hero carrying none of what is lacking is refused at the part's
  start (`items.notEnough`), the work and what was put in staying. Each hero pays from their own
  inventory, whoever reaches the part; within a part paid for, anyone works, carrying nothing.
- **Unchanged.** Creative mode is free. Cancelling a blueprint drops exactly what was put in where it
  stood. What was paid (`Delivered`) and the work done (`Progress`) were saved already: no save format
  change, and an older save's blueprint, paid in full or in part, works on.
- **Seen.** The plan turns into the real thing as it is worked: walls, back walls, ladders, doors and
  machines from the bottom up; floors and conduits, thin, from the left. The real share is drawn and lit
  as what is built is; the rest stays the unlit ghost in its dashes, which stay round the whole plan until
  it is built. The cut follows the work done (`Progress / BuildTicks`), a row or a column of pixels at a
  time: the first touch of a part pays its item, then the part fills smoothly under the screwdriver, and a
  build stalled for want of an item sits at that part's boundary. Stepping a whole part at each payment was
  tried first: it showed the part built before it was worked. The delivery and work bars on blueprints go.
  A mark is taken down the same way in mirror, turning into its red ghost from the top ([0064](0064-plans-and-marks-are-drawn-alike.md)).
- **Drawn** by splitting each picture at its cut into two regions of the ship's atlas: the real one in a
  lit canvas item per tile layer, the ghost in an unlit one; no node, material or instance uniform per
  plan (Godot's global buffer, which instance uniforms fill per item, is what `Lit` ran out of).

## Consequences

- Heroes no longer empty their pockets into plans they pass; what is in a plan is what its work used.
- A half-built thing is nothing in the grid until its last tick: a half-built wall holds no air and
  stops no one. Accepted.
- Future drone deliveries to blueprints (0062) only let work run on past part boundaries.
- Creative mode switched off on a blueprint worked past what was paid: each later tick pays an item until
  the parts paid catch up with the work.

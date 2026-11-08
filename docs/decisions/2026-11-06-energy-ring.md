# The energy tab's ring and beating bolt

- Status: accepted; amends [0081](0081-the-wrist-is-one-column-of-gauges-and-keys.md)'s energy tab and the suit gauge's label, and [0053](0053-charge-belongs-to-the-item.md)'s energy bar; its discharges amended by [air-feedback](2026-11-07-air-feedback.md)
- Date: 2026-11-06

## Context

The playtest found the energy tab dull while a tool drained the belt: the number fell in jumps four times a second, each
read flaring white. With tools now drawing a third as much ([lighter-tools](2026-11-06-lighter-tools.md)), a drain lasts
long enough to watch. Two pick pages were shown: six ways for the number in the first, then, from the user's notes (the
smooth count and the beating bolt liked, per-battery cells liked but feared to crowd as the belt grows), four layouts of
cells for any number of batteries and five effects round the bolt.

## Decision

The user's picks, combined (`game/features/energy/`):

- **A smooth count**: a fall glides to each read over one read's span, a rise eases out; no flare and no white flash.
- **The pulse bolt**: it beats with the tool, ten beats a second a power unit of draw, a current running down it; going
  flat it sputters out, a store put on kindles it from its tip.
- **The ring of cells** (`cellsRing`): an arc round the bolt for each open belt slot, the circle shared alike, so any
  number of slots fits; the drained store's arc lit with a glint, a spark hopping to the next when it is spent.
- **Discharges onto the frame** (`boltArcs`): now and then a beat throws a thin jagged discharge from the bolt to the
  tab's edge; the grinder's more often, wider and forked.
- The tab grows from 70 to 78 px so "16 200", three accumulators, clears the ring; the suit's gauge keeps more than four
  times its width at every slot size.
- The tab moves out of `features/items` into its own feature, whose README holds its rules.
- **The suit's gauge is labelled by the oxygen sign** rooms are read by ([tip-signs](2026-11-05-tip-signs.md)) in place of the
  canister, the user's call: a sign beside a sign, as tall as the bolt.

Rejected on the pages: odometer drums, a smouldering fuse strip, a phosphor sweep, a split strip of cells, the drained
battery as a bar beside dots, level columns at the edge; and round the bolt, a current into the digits, an overloaded
lamp's flicker, sparks off its tip.

## Consequences

- The bolt's beat follows the running tool's content draw: a fitted drill beats as a plain one.
- A drain without a tool (the regen suit, the railgun) moves the number and the ring, not the bolt.

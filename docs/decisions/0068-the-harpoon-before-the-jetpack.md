# 0068 — The harpoon before the jetpack

- Status: accepted
- Supersedes: of [0046](0046-recipes-show-what-a-thing-is-made-of.md), the harpoon's recipe; of
  [0047](0047-equipment-the-jetpack-and-the-slower-swim.md), the jetpack's; of
  [0044](0044-the-harpoon-reels-a-piece-to-a-point.md), the harpoon's 12-cell range and 5-cell reel
- Date: 2026-10-18

## Context

In the first co-op playtest the harpoon opened after the jetpack. Its cost held a plank and a frame, the
jetpack's did not need a coil, so a jetpack came first, and once a hero flies, a 12-cell harpoon that reels at
5 cells a second does nothing the flight does not do better. It felt useless.

## Decision

- **The harpoon:** a tube, a coil and 4 wire, in 100 ticks.
- **The jetpack:** a frame, 2 tubes, a coil, a circuit, a canister and 2 wire, in 160 ticks.
- **The order follows from the opening rule** (0027): a craft is known once every item of its cost is
  analysed. The harpoon's items (tube, coil, wire) are all in the jetpack's, and neither is granted, so the
  harpoon is known no later than the jetpack in any order of analysis. In play it comes well before: iron opens
  the tube and wire the coil, both made at the lifepod, while the jetpack also waits on a frame (plates), a
  canister (salvage) and a circuit, which needs silicon smelted from asteroid ore and the circuit assembler.
  This holds as well if analysis is narrowed to items in the cost of something not granted: both are ungranted.
- **The harpoon tows:** its rod flies 20 cells (`Harpoon.Range`), and a reeled piece comes in at 8 cells a second
  (`Harpoon.ReelSpeed`, 400). The asteroid band stays where it is (0054).

## Consequences

- No save format change: costs and the harpoon's numbers are content, and a reel in flight keeps the `Left`
  it was given.
- The harpoon has no plank stock in its recipe any more, though its picture keeps one.
- A jetpack now needs electronics: flight comes after the circuit assembler.

## Also from the playtest

- **Rock is called asteroid ore** where players read it; its id stays `rock`, so saves need no migration.
- **The axe's swing is heard,** every swing, a hit or a miss: a staff's whoosh (four CC0 recordings, the user's
  pick), its pitch varied, its loudest moment in the middle of the downstroke, on the Suit bus as the chop is.
- **Idle tool sounds share one volume,** `Sound.ToolIdleDb`: the drill running free and the axe's swing. Their files
  are levelled to the same momentary loudness, so the one value moves them together and no cue gets a level picked
  by hand.

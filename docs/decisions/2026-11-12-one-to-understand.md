# One of each part is enough to understand a thing

- Status: accepted
- Date: 2026-11-12
- Supersedes in part: [the-board](2026-11-05-the-board.md) (how many of a part are enough, and the pencil's counts),
  [board-thoughts](2026-11-02-board-thoughts.md) (a shape's "2 more"), [trade-station-later](2026-11-06-trade-station-later.md)
  (two circuits understanding the trade station)

## Context

The board's rule asked for `Understanding.Enough` of each part, up to ten of a common one, and the board showed how many more
were still needed. In the playtests most players never looked at those counts to make sense of a craft; once told, they took them
as a formality, and the numbers confused more than they helped. A design of food, dishes learned at a galley by the board, assumes
the simpler rule too.

## Decision

- **The rule.** A blueprint or craft is understood once one of each of its parts has ever been obtained
  (`Knowledge.ObtainedOf(item) > 0`) and, for a craft, a machine that makes it stands on some surface. `Understanding`,
  `EnoughWorth` and `EnoughMost` are gone. Everything else of [the-board](2026-11-05-the-board.md)'s rule stands: known stays
  known, the walk, what the market opens and upgrades left outside it. The wreck's first walls come at the first chunk.
- **`Knowledge.Obtained` keeps its counts.** The board's slots read its order and tests read the counts; nothing in the saved
  state changes, so no format bump.
- **The board shows no count.** A shape's pencil is the share of its parts ever had, a standing station one more part for a craft.
  A shape's box says "needs" and the picture of the first part never had, or of the machine to stand; the strip ticks a part
  once had and dims one not had yet, under "To work out how to make it, get one of each". The pile says "get some X first".

## Deliberately not handled

- **A rare part is no slower to understand than a common one.** The worth-scaled count made a common part take longer; the
  pace now comes from finding each part at all and from the machines that make a craft.
- **No migration.** A world saved under the old rule grants, on its first tick, whatever one of each now allows.

# 0067 — One door, many shapes

- Status: accepted
- Supersedes: of [0005](0005-terraria-scale.md), "toggleDoor on any door cell sets every door cell 4-connected"
  and "Touching doorways toggle together ... accepted"
- Date: 2026-10-18

## Context

A door laid along a deck already sealed, opened as one doorway and let a ladder climb through it when
open: the sim's doorway was every door cell 4-connected to the one toggled, with no shape. But the client
drew it as three one-cell doors standing side by side and played its sound three times, so a friend in
playtest 1 read it as broken (point 10). Then a hatch with a door hanging from it — an L — opened as one,
both legs at once, which read wrong too. More door kinds will come; each should ship as one object that
takes every shape, not as a door and a separate hatch.

## Decision

- **A doorway is one straight run** (`Doorways`, in the grid module): a door cell with a door cell above or
  below stands in a door and joins its doorway up and down only; every other door cell — in a run across,
  or alone — lies in a hatch and joins the hatch cells either side of it. Where a door meets a hatch the
  cell they share is the door's and the hatch stops beside it, so an L or a T is two or three doorways, each
  toggled alone. A cell alone is a hatch because no hero fits through a door one cell tall, and one-wide
  shafts are where 0005 put hatches.
- **One rule, the sim's.** `ToggleDoor` floods along `Doorways.LinksOf`, and the client reads the same
  joins for everything it draws or hears: the grid's pictures, plan and card ghosts and the dashes round
  them (`Blueprint.LinksOf`), the aim outline (the whole run), the lightmap (a closed door's panel spans
  columns, a closed hatch's rows, each read from its picture), and the open and close sound, once a
  doorway at the cell joined neither above nor to its left (`DoorShape.Leads`). The shape is derived from
  the structure layer: no new code, no save format change (still 17). Rooms, air leaks, drones passing
  (0062), stacks falling and the hero standing, falling and climbing never read doorways.
- **Pictures by shape, swapped as files.** A door's pieces are `door_{closed|open}_{mask & (up|down)}`, as
  before; a hatch's `hatch_{closed|open}_{mask & (left|right)}`: 0 alone, 2 the left end, 10 the middle, 8
  the right end. The hatch is the art picked as concept B: a dark steel slider with an edge stripe, a grip
  and an arrow the way it slides, its panel flush with the top of its cell, where a hero stands; open, the
  panel is slid into its frame at the left end. New art replaces the eight files and nothing else. The
  build card still shows a whole door; the prompt still says "Open door".

## Consequences

- A future door kind (an airlock, a blast door) needs a kind stored per cell, a layer in the sim, since
  kinds differ in rules; its shapes need nothing more than its own two picture sets.
- A deck two cells thick draws its hatch as a row of short doors, and each column toggles alone: it
  works, and decks are one wall thick.
- A door and a hatch touching only at a corner are two doorways, as they always were.
- Down on a closed hatch does nothing (it is solid); on an open one over a ladder the hero climbs down, and
  over open floor falls through. A hero below a hatch with no ladder cannot get up through it.
- A door cell coming or going changes how the hatch cells beside the cell above or below it join, so the
  grid view redraws a changed door cell's corners as well as its sides.

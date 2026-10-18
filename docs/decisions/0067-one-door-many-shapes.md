# 0067 — One door, many shapes

- Status: accepted
- Date: 2026-10-18

## Context

A door laid along a deck already sealed, opened as one doorway and let a ladder climb through it when
open: the sim's doorway is every door cell 4-connected to the one toggled, with no shape (grid README).
But the client drew it as three one-cell doors standing side by side and played its sound three times,
so a friend in playtest 1 read it as broken (point 10). More door kinds will come; each should ship as
one object that takes every shape, not as a door and a separate hatch.

## Decision

- **The sim stays shapeless.** Rooms, air leaks, `ToggleDoor`, drones passing (0062), stacks falling and the
  hero standing, falling and climbing already ignore shape. No new structure code, command or save format
  (still 17).
- **One client rule, `DoorShape`** (`game/render/DoorShape.cs`): a door cell joined to a door cell above or
  below is part of a door; every other door cell — a run across, or a cell alone — is part of a hatch. A
  cell alone is a hatch because no hero fits through a door one cell tall, and one-wide shafts are where
  0005 put hatches. The rule reads only the cell's own joins, `linked`, the sides with a door cell on them.
- **Everything that draws or hears a doorway asks it:** the grid's pictures, plan and card ghosts and the
  dashes round them (`BlueprintArt.PiecesOf`), the aim outline (the whole doorway, flooded as the sim
  toggles it), the lightmap (a closed door's panel spans columns, a closed hatch's rows, each read from its
  picture), and the open and close sound, once a doorway at the cell with no door above it or to its left
  (`DoorShape.Leads`). The build card still shows a whole door; the prompt still says "Open door".
- **Pictures by shape, swapped as files.** A door's pieces are `door_{closed|open}_{mask & (up|down)}`, as
  before; a hatch's `hatch_{closed|open}_{mask & (left|right)}`: 0 alone, 2 the left end, 10 the middle, 8
  the right end. A closed hatch's panel lies flush with the top of its cell, where a hero stands; open, the
  panel is slid into its frame at the left end. The placeholders are the door pieces turned a quarter and
  raised flush; real art replaces the eight files and nothing else.

## Consequences

- A future door kind (an airlock, a blast door) needs a kind stored per cell, a layer in the sim, since
  kinds differ in rules; its shapes need nothing more than its own two picture sets.
- A deck two cells thick draws its hatch as a row of short doors: it works, and decks are one wall thick.
- A door touching a hatch at a corner keeps both shapes; one sharing a side with a hatch's cell makes that
  cell a door's, and the hatch's end joins into it. Both are one doorway in the sim, as before (0005).
- A hatch running left from the foot of a door (a doorway shaped like ┘) is heard twice: at the door's top and
  at the hatch's left end, both with nothing above or to their left.
- Down on a closed hatch does nothing (it is solid); on an open one over a ladder the hero climbs down, and
  over open floor falls through. A hero below a hatch with no ladder cannot get up through it.

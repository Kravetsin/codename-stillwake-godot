# 0111 — Doors and hatches are two things

- Status: accepted; supersedes [0067](0067-one-door-many-shapes.md)'s guessing of a doorway's kind from the shape of its run
- Date: 2026-10-24

## Context

0067 read one structure code, a door cell, and decided from the run it stood in whether it was a door or a hatch: a
cell with a door above or below was a door, every other cell a hatch. The guess was right for the shapes it was
written for and wrong for many others. A door laid across a deck became a row of hatches, a hatch beside a door
changed the door's shape, a lone segment became a hatch no hero could walk through, and a plan could not keep the
kind its card promised. The client drew, lit, heard and ghosted the guess, so a player could not tell what a card
would build until it was built, and building one kind where the guess expected the other broke the doorway in two.

The kinds differ in rules, not only in shape: a door stands in a wall, two cells thick and two tall, and shuts a
room; a hatch lies in a deck, two cells wide and one tall, is a floor when shut and a ladder when open. Guessing
one from the other cannot be made right.

## Decision

- **A door is built of 2×2 segments.** A segment is two cells wide and two tall (`Doorways.Thick`, `Builds.PieceOf`).
  Segments stacked one above another are one doorway, opening and shutting whole; segments side by side are
  separate doorways. A door never joins a hatch, so a hatch beside or under a door is a doorway of its own.
- **A hatch is one 2×1 piece, a doorway of its own.** It does not stack or run: the cell beside it within its pair is
  its only join. Shut, it is a floor: it holds a hero, a stack or a creature from above and stops a body from below,
  and, like a shut door, it seals its room and stops light (amended 2026-10-24: a hatch differs from a door only in being climbed through when open). Open, it is climbed through as a ladder is, but only
  where a ladder lies past it, so a hole beside a ladder is still a hole (amended by [0112](0112-structure-traits.md): an open hatch is a ladder of its own).
- **The kind is stored, not guessed.** `Structure` gains `HatchClosed` and `HatchOpen` beside `DoorClosed` and
  `DoorOpen` (`sim/Grid/Grid.cs`). `Doorways` reads the codes: `IsDoor`, `IsHatch`, `IsDoorway`, `IsOpen`, `IsClosed`,
  and `LinksOf` joins a door cell up and down and the cell beside it within its segment, a hatch cell only the cell
  beside it within its piece. `SetDoor` keeps the kind through `DoorCode`, so a doorway opens and shuts as what it is.
- **A hatch is a build of its own.** `Builds.Hatch` is 2×1, its own blueprint id, at the door's cost and build ticks;
  `Builds.BuiltAs` is gone. The plan menu has a hatch card beside the door's (`Tools`), and a drag lays a piece along
  its own longer axis (a square piece along the drag's longer axis, a hatch along its row), with no orientation guessed
  from the card (`Drag.Oriented` removed). A plan carries the code it is planned as (`Tools.CodeOf`, `Tools.DoorLinks`),
  so a planned door stays a door and a planned hatch a hatch while it is a ghost.
- **Everything the client does reads the codes.** Drawing and its atlas (`DoorShape.SpotOf(..., hatch)`), the outline,
  the aim, hover, the lightmap, the sound and the slides all take the kind from the cell, not from the run. A hatch's
  pictures are always mask 0 (`hatch_closed_0`, `hatch_open_0`): adjacent hatches are separate doorways, so the old
  `_2/_8/_10` pieces stay in the atlas unused. A closed hatch stops light across its row, as a wall does; a closed door's panel stops it where drawn.
- **Maps and saves.** The map legend gains `h` and `o` (hatch shut and open) beside `D` and `d`; in a doubled map a
  hatch character fills the top row of its 2×2 block, as a floor does, so the hatch is one cell thick in the 8 px
  world. The save format is 29 and unreleased: the new codes need no migration.

## Rejected

- **Keeping 0067's guess and fixing its edge cases:** every fix was another shape the guess read wrong, and the
  client and the sim had to agree on the same guess.
- **Doors joining side by side into one doorway:** a wall of doors would open as one, and the pieces would no longer
  be the 2×2 segments they are built and paid as.
- **Hatches stacking:** a stack of hatches is not a hatch; the deck between two decks is a wall.
- **A hatch that joins a door's run:** the two have different rules, so a shared doorway could not open as both.
- **A separate structure layer for the kind:** the four codes already say it, and a layer would need a save format
  change and a second thing to keep in step with the structure.
- **A card that guesses its orientation from the drag:** the card says what it builds; the drag says where.

## Accepted limitations

- A doubled map cannot draw a 2×1 hatch flush against a ladder a block below: the hatch fills the top row, leaving the
  deck's lower row empty, and a hero climbing up bridges that one cell with its body. A cell-scale map draws it flush.
- The atlas keeps the old hatch mask pictures; nothing produces them.
- Door kinds with rules of their own (an airlock, a blast door) still need a layer of their own; a door and a hatch
  do not, since their kind is their code.

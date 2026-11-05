# The board has edges, and what is bought stands by its parts

- Status: accepted
- Date: 2026-11-02
- Changes [the-board](2026-11-05-the-board.md): it is no longer endless, and the market's goods hold slots

## Context

A note could be carried anywhere and the camera always held all that stood, so one note dragged away shrank the whole board to specks. A bought solar panel
got a note, but in the row above the first note, far from its parts, where its ties ran across the board.

## Decision

- **The board reaches 1200 either way across and 600 up or down from the first note** (`BoardBox`), about 2500 by 1300 with its margin. At 720p the camera then goes no
  smaller than about a third of life size, where a note is some 25 pixels and its picture still reads. All the board's things fit at any order of finding: a test holds it.
- **A note carried past an edge stops at it.** One kept past it, from before this, comes back to the edge when its world loads.
- **A slot is never past an edge.** Its arcs go on as before; those spent, it looks the whole way round its note. Finding no room at all throws, for that is our bug: it takes
  several times the things there are.
- **What the market opens holds its slot from its first part noted,** hidden, as any thing holds one before it is known. Bought, it inks in there, among its parts, and nothing
  else moves. A click on it shows its ties, as on any note. It still shows no shape or thread before it is bought.
- **The row stays** for what is known with no part noted: bought before any part was found, or an old world's. A row past the edge goes on round the first note.

## Deliberately not handled

- **Upgrades** are still the market page's rows, not notes: they have no parts and tie to nothing.
- **Threads to a bought note:** none, as to any note; threads run to shapes. Its ties show on a click.
- **Boards of worlds from before this** shift by the hidden slots, once, as they load. What the player moved stays where it was moved.
- **The empty side:** a board grows outward from its first note, so a full one may crowd one edge with room left at another. The player can carry things across.

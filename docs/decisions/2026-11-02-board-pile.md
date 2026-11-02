# New finds come to the board as a pile

- Status: accepted
- Date: 2026-11-02
- Amends [tablet-key](2026-10-31-tablet-key.md): how new notes are marked

## Context

In a playtest the board dumped a heap of icons on the player at once. The user wants each thing noticed, and to see at
once where to go next, with the board's logic as it is. Four ways were drawn on the same finds in the real tablet: a
pile, a camera tour, notes warming up in place, and a screen of the next goals.

## Decision

- **The user picked the pile.** Unread entries wait at the screen's edge with a count; the top one is large; a click
  lands it at its place with its thread drawing; "Take all" lands the rest.
- **The player sets the pace.** Nothing flips by itself, however many wait. The candidate turned three by itself past six
  entries; the user asked for the opposite: clicking through is the point, and "Take all" is the way out.
- **Only the showing changes.** What is on the board and when stays `BoardView`'s.

The design is in [board-reveal](../plans/board-reveal.md).

## Deliberately not handled

- **The notes' own look:** picked apart. The page's notes were a sketch.

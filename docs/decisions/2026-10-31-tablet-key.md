# The tablet's key and the mark of a new note

- Status: accepted
- Date: 2026-10-31

## Context

The empty board ([board](../plans/board.md)) writes notes while the tablet is away, so the HUD needs a key that says something new has
been written, and the board has to show which notes those are. Both are looks, so the user picked them, each from four candidates:
[the key](https://claude.ai/artifact/TMyDwPENqzkJg6AAiBAgzL) and [the mark](https://claude.ai/artifact/ReujzZTQyoPgnJyAV8ZSik).

## Decision

- **The glyph is three cards and threads** (`TabletKey.Cards`): the board itself rather than a tablet's outline, in the sort key's inks and
  glow, so it says what it opens.
- **The signal is a lamp in the key's metal**, above the rubber, always there: dark when nothing is new, amber when something is. The face
  stays free for the glyph, and a dark lamp teaches where to look before it first lights.
- **A new note gets a loop drawn round it by hand** (`BoardPage.LoopPoints`), in the same amber, round the card and its word, its ends
  overlapping. The scavenger circles what they just wrote down, so the border stays the note's own and a tie's colour never fights it. It
  dims with the note while something else is picked.

## Deliberately not handled

- The loop is the same on every note, a stroke drawn once in code: no jitter per note, for nothing has asked for it.
- It reaches a little past a note's slot, so it can lie over a close neighbour's edge; it stays readable as a pencil line would.

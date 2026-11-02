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

- **The pile is not stored.** It is the notes less what was seen, so the file format stands, and a close mid-pile resumes.
- **Mouse only.** Space would have to be kept from the hero, who still jumps under the open tablet.
- **A find with the board whole lands at once**, for a pile would take the player's view from what they are doing.
- **The board says where next.** Once a pile empties, the closest shape is framed with what it lacks, what was never had unnamed.

## Deliberately not handled

- **The notes' own look:** picked apart. The page's notes were a sketch. Until then the entry's name is in the game's own font.
- **Pencil from its old value:** counts at the last close are not kept.
- **The player's camera after a pile:** they just gained something to look at.
- **A note again after the host loads an older save:** `Seen` only grows.
- **Flips after the last write, on a crash:** they replay, harmlessly.
- **A find arriving on the market's tab:** no signal; the pile plays when the board is chosen.
- **A fade for a thread pushed out of a note's top three:** it goes with the landing.
- **An unread entry whose slot a dragged note stands on:** it lands on top, and the player can drag either away.
- **The whole card shrinking in flight:** only its picture in a box flies, as the other needs the card drawn to a texture.
- **What is second in the pile:** the order is worked out per flip, and the outlines are only a count.

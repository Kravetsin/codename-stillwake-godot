# New finds on the board, as a pile

The look was picked on a page of candidates: [the pile](../decisions/2026-11-02-board-pile.md).

## Problem

A tablet opened after a burst shows every new note at once, each with only a loop. The player notices none in
particular and cannot see where to go next.

## Player experience

1. The lamp lights and the player presses T. With unread entries, the tablet opens on the board.
2. The board stands left. Beside it the top entry is large: picture, name, made from, made at, used in, next step. The
   rest wait at the edge as outlines with a count.
3. A click on the entry or the pile sends it down, shrinking, to its place. The board glides there as it lands, and its
   thread draws itself. A click never waits for a landing.
4. Nothing flips by itself, however many wait. Only the "Take all" button lands the rest, at once and without flights.
5. Once the pile is empty, the board opens to the whole view, with what the closest shape lacks written beside it.

Going wrong: the player closes on entry 4 of 9. Three are read, six wait, the lamp stays lit, and the next opening
resumes there.

| Player sequence                                 | Result                                                                  |
| ----------------------------------------------- | ----------------------------------------------------------------------- |
| Opens the board or chooses its tab, with unread | Reading starts, whichever tab the tablet was left on                    |
| A find arrives while reading                    | The count rises, the entry joins under the top; the top never changes   |
| A find arrives with the board whole             | Goes straight onto the board with today's loop, so a drag is left alone |
| Closes mid-pile                                 | A flip counts at the click; the rest stay unread                        |
| Leaves and returns                              | The pile is worked out again from the file, same order                  |
| Guest                                           | Own file, own pace. A guest with no file, late or lost: an empty pile   |

While reading, drag and wheel move the board as today, and the next flip brings it back. A click on the board does
nothing, as the entry already says what the strip would; the strip holds only a hint. T, Esc, Tab, E and B close, as today.

## Out of scope

- What is on the board and when.
- The notes' redraw, picked apart. Until then the entry's name is in the game's own font.
- Keyboard flips: Space would have to be kept from the hero, who still jumps under the open tablet.
- Paper, or a watercolour wash on screen.
- A count on the HUD key.
- Sound.

## Scale

- World: about 80 notes at most, recomputed per flip, not per frame.
- Players: each reads alone.
- Frequency: mostly 1 to 6 entries (`board.md`); 15 or more after a long absence.

## Approach

Client only.

- **Pile:** not stored. It is notes minus `BoardSeen.Seen`, so `BoardFile` is unchanged.
- **`BoardSeen`:** advances per entry, not on `Closed`. `Marked` becomes "landed since opening".
- **`BoardPile` (new, `logic/`):** order, top, count, flip, take all. `BoardSeen` knows no recipes.
- **`BoardPage`:** draws the entry large with the builders it has (`Fill`, `PartOf`, `TechLinks`), so an unseen part stays
  a pale picture with no name. Only "next step" is new. Flights and the thread's fraction are `Tween`s. The old picture
  stays drawn until a flight ends, so a thread that goes vanishes as its shape inks.
- **`TechCamera`:** none new. `Fit` takes the narrowed width and each flip calls `Centre` on the entry's spot, so every
  landing is seen however large the pile.
- **Unchanged:** `BoardView`, `BoardThreads`, `BoardPencil`, `BoardSlots`. `BoardView` gets the read without unread
  entries, over full slots.
- **Order:** repeatedly, the first in slot order with no unread part or station; if none, the first left, so a recipe
  cycle cannot stall the pile. A hull plate lands, its thread reaches the pale furnace, and the next flip inks the
  furnace there.
- **The pile's controls** take no keyboard focus, or Space presses "Take all".

## Determinism and co-op

Nothing enters the sim: no commands, and `BoardPage` still cannot send any. Floats are client-side. Knowledge is
shared, reading is per machine.

## Trade-offs accepted

- We do not animate pencil from its old value, because counts at last close are not kept.
- We do not restore the player's camera after a pile, because they just gained something to look at.
- We do not re-show a note after the host loads an older save, because `Seen` only grows.
- We do not protect flips after the last write from a crash; they replay, harmlessly.
- We do not signal a find arriving on the market tab; the pile plays when the board is chosen.
- We do not fade a thread pushed out of a note's top three: it goes with the landing.
- We do not move an unread entry off a note the player dragged onto its slot: it lands on top, as `BoardSlots` ignores
  moved spots, and the player can drag either away.

## Tests

- Scenarios (`game.tests`): order, parts first; close and resume; arrival under the top; take all; relaunch; a guest
  with no file; the lamp while unread remain.
- Rewrite the `BoardSeenTests` that pin marks and seen-on-close.
- Invariant (FsCheck): every entry lands exactly once, for any flips and arrivals.
- Frames: `--board=burst`, `--board=late`.

## Cost

No sim module, no content; timings are client constants. An ADR amends the tablet key's and `board.md`'s "no landing
animation", and the README. Client about +350, tests about +200.

## Decisions taken by default

- The lack written by the closest shape stays until something is picked or the tablet closes: it answers "where next".

# The camera stops at a size, and the player has room for heaps

- Status: accepted
- Date: 2026-11-02
- Changes [board-edges](2026-11-02-board-edges.md): what stopped the camera shrinking was the size of the board, which left no room to sort notes into heaps

## Context

Edges of 1200 by 600 kept a dragged note from shrinking the board to specks, but they did it by making the board small. In play it was too tight to sort notes into heaps of
one's own, which is what the player is likely to want.

## Decision

- **The camera has a floor of its own:** a zoom of 0.33, where a note is some 25 pixels and its picture still reads, however large the board has become. Past it the wheel does nothing.
- **Things can be carried three times as far:** 3600 either way across and 1800 up or down from the first note. At the floor that is about two views each way. Past that a carried
  thing still stops at the edge, so a note cannot be lost for good.
- **Slots keep to the old 1200 by 600,** so everything that stands by itself fits in one view at the floor, and the rest is the player's room.
- A board larger than the view at the floor is dragged across, as any map larger than the view is.

## Deliberately not handled

- **The view does not follow a carried thing.** It goes as far as the pointer can in the view: to take it further, let go, drag the board and carry it again.
- **Seeing a whole spread-out board at once** is not possible at the floor. That is the trade for notes that always read.

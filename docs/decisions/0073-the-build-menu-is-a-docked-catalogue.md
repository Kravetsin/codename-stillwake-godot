# 0073 — The build menu is a docked catalogue

- Status: accepted
- Date: 2026-10-19

## Context

The build menu was a panel at the bottom middle as wide as its hint line, its cards flowing in rows of 48 px: it
changed size between tabs (659×122 to 507×226 px with decor added), and every new card made it taller. Decor (24+
pieces), more machines and more materials are coming. The right side of the screen is the inventory's and the
equipment's; a window in the middle would cover the ship and need closing after every pick. Three layouts were drawn
over the ship (the build-ux mockups): A, a fixed catalogue docked at the bottom; B, a drawer at the right edge; C, a
quick bar with the whole catalogue on a key. A was picked.

## Decision

- **One fixed frame.** The open menu is 638×204 px at the normal slot size (862×252 at the large), docked 12 px above
  the bottom edge in the middle, whatever the tab, the search or how many cards there are (`Catalogue`). Only the slot
  size setting changes it.
- **Cards are the inventory's slots**, 40 or 56 px with the 32 or 48 px picture, 14 across and 3 down: 42 before it
  scrolls. The wheel anywhere over the panel scrolls a row a notch; a thin bar beside the grid shows where, its thumb
  only when there is more. The card in hand is edged sky blue 2 px; under the pointer, a slot's hover look.
- **Tabs carry their counts** of known cards; they stay the mode (0030, 0072). A tab clicked starts at its top row; a
  card taken by Q is scrolled into view.
- **A search across every tab** (/ or a click on the field): the cards whose names hold the words, case ignored, no tab
  lit while it shows. Typing takes every key, so the hero stands still and B, Q and the digits type. Enter or a click
  elsewhere ends the typing and keeps the words; Esc clears them, the menu staying open, and a second Esc puts the plan
  away. A tab clicked, or a card taken by Q that the words do not name, ends the search. Put away, the menu forgets it.
- **The line under the cards** is the card in hand, its picture and name, and the tab's hint, at 11 px so the longest
  hint beside the longest name fits; a hint too long for the frame is cut with an ellipsis, never the frame widened.
  The hints are written "key: action", as the mockup's, a few words shorter.
- Picking a card leaves the menu open, as before; B toggles it; the tooltip by the pointer is the one tooltip (cost,
  carried against needed, use), found by where the mouse is each frame, so it follows a scroll under a still pointer.

## Deliberately not handled

- Cards not analysed yet stay hidden (0024), not shown as "?": the mockup drew them, but nobody asked to change that.
- No dragging the scrollbar or clicking its track: the wheel, and Q's scroll into view, reach every card.
- Tabs past the strip's width are not wrapped or shrunk: today's five, and decor as a sixth, leave the search about
  180 px. A seventh may need shorter names or icons.
- No recent or pinned cards (layout C's bar).

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

## Look: a rugged planning tablet

The catalogue is the screen of a handheld tablet (the tablet-menu concepts; variant A, rugged industrial, picked). Its
frame is pixel art at two screen pixels a pixel round the unchanged panel: a scuffed bezel with rubber corners, a worn
warning stripe, screws, an ID plate, signal and battery pips, a status lamp and a stylus clipped on the right; the
screen square and borderless, a recess shadow and a faint glare over it. The 9-slice (`art/tablet_frame.png`) holds
the corners, the top and bottom strips and one rivet period of the sides, tiled rather than stretched, so the large
slot's 862 px frame is as crisp as the normal one; the plate (centred) and the stylus (at the right) are laid over it.
The screen stays where the panel was, the bezel running 8 px past the bottom edge, as held from below.

- **B lifts it** from below the bottom edge in 130 ms, easing out, and puts it away in 100 ms, easing in; turned back
  halfway it goes on from where it is (`TabletMotion`). It takes clicks from the start of its slide, and none from
  the moment it is put away, though it shows while it slides down;
  the closed chip ("[B] Build") shows once it is gone.
- **The screen comes on gently**: its content fades in from 60 to 140 ms, never brighter than lit, the lamp turning
  green with it. Taken out again within 1.5 s of being put away it is lit at once, so B pressed often never blinks.
  The concept's power-on (a bright line, an overbright picture and a flicker) was dropped as hard on the eyes.
- **Its light**: a faint wash of the screen's blue round the tablet and the status lamp's glow, both added over what is
  under them and dimmed with the screen. In the world, the planner in the hero's hand already lights what is round
  it in the same blue (`crew`'s `Figure`); the tablet on the screen is not a light of the lightmap.

## Deliberately not handled

- Cards not analysed yet stay hidden (0024), not shown as "?": the mockup drew them, but nobody asked to change that.
- No dragging the scrollbar or clicking its track: the wheel, and Q's scroll into view, reach every card.
- Tabs past the strip's width are not wrapped or shrunk: today's five, and decor as a sixth, leave the search about
  180 px. A seventh may need shorter names or icons.
- No recent or pinned cards (layout C's bar).
- The frame's art keeps two screen pixels a pixel at both slot sizes and every resolution; the UI is not scaled for
  4K, where the large slot is the setting to use.
- The bezel takes the clicks and the wheel as the screen does, its rect's transparent strip beside the stylus too.

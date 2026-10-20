# 0081 — The Handbook is a tablet

- Status: accepted
- Date: 2026-10-20
- Supersedes: the window of [0066](0066-the-handbook.md) (a rounded panel with a side list of sections)

## Context

The Handbook (0066) was a plain rounded window in the middle of the screen, a header with the book and a J keycap,
the sections in a side list, sans and semibold text. The build menu had meanwhile become the screen of a rugged
planning tablet (0073), and the inventory a wrist computer in the same metal (0074). The Handbook was the one window
left in the old look. The user asked for it in the plan menu's style, copied whole.

## Decision

- **The same tablet.** `shared/Tablet` is the tablet itself, taken out of the build menu: its 9-slice frame at two
  screen pixels a pixel, the plate, the stylus, the status lamp and its glow, the screen's blue wash, the glass's
  shadow and glare, its slide and its screen coming on (`TabletMotion`), its tabs, its scrollbar, its monospaced
  text. The build menu and the Handbook both use it; neither draws a frame of its own.
- **Larger, not another look.** The Handbook's screen is 838×376 at the normal slot, 950×440 at the large
  (`HandbookScreen`, from `Content/Handbook`), the same frame tiled round it, so the bezel is as crisp and as thick
  as the build menu's. With its bezel it fits 1280×720 at both sizes. Fixed, as the build menu is: only the slot size
  changes it; the kinds' slots are an item slot and 16 px, the loot slots item slots.
- **In the middle, lifted from below.** J lifts it from below the bottom edge to the middle of the screen, as B
  lifts the plan to its dock, in the same 130 ms out and 100 ms away; its screen fades in from 60 to 140 ms, lit at
  once when taken out again within 1.5 s. Put away, it takes no input at once.
- **Tabs for sections.** The sections are the build menu's tabs along the top; what the chosen one says sits at the
  strip's right, where the build menu's search is (the debris tally, "23/81" and its green bar). Along the foot, as
  the build menu's card in hand and hint: the book, "Handbook" and the keys, at 11 px.
- **The wheel scrolls the body**, anywhere over the tablet, a row of kinds a notch, a thin bar beside it showing
  where (its thumb only while there is more); it never reaches the hotbar. Today every entry fits at both sizes
  (`BodyOver` leaves room for an entry whose loot takes a second row at the large size), so nothing scrolls yet:
  the room is for the sections to come.

## Deliberately not handled

- No closed chip ("[J] Handbook") at the screen's edge, as the build menu's "[B] Build": the game menu's line says J.
- The stylus and the plate are the build menu's, though the Handbook is no plan: one tablet, one look.
- The section's icon (the cargo crate in the side list) is gone: the build menu's tabs carry no icons.
- No dragging the scrollbar or clicking its track, as in the build menu.

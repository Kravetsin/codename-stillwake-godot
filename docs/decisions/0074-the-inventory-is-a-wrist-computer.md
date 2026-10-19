# 0074 — The inventory is a wrist computer

- Status: accepted
- Date: 2026-10-19

## Context

The build menu became a rugged planning tablet (0073); the hotbar, the bag, the open machine and the equipment were
still flat slate boxes with a 1 px border. Four looks were drawn over the ship (the inventory-look concepts): A, the
suit's own storage; B, a wrist computer in the tablet's metal; C, today's panels in a thin rugged bezel; D, a
canvas backpack. B was picked. The concept kept every slot's size and place, and named its own problems: its
always-on HUD is the heaviest of the four, the bag's and a crate's slots looked alike, and the large slot (56 px)
does not fit art drawn round the normal one.

## Decision

- **Frames only.** Slot sizes, places, gaps and the layout (0058) are unchanged, and so are the slots themselves
  (`SlotLooks`), the digits, counts, "?" marks, smelt and sprout marks, battery bars, the stack in hand and the
  tooltip: they are drawn on the screens. Each panel's box is now a screen (flat navy, the recess's shadow on its top
  and left edges and a faint glare, all under what it shows) in a piece of the tablet's scuffed metal.
- **The pieces** are pixel art at two screen pixels a pixel, exported from the concept's drawing code and cut into
  9-slices whose corners are kept whole and edges tiled (`WristPieces`, `art/wrist_*.png`), as the tablet's frame:
  the large slot's frames are as crisp as the normal ones, and any panel's size fits.
  - The **cuff** round the hotbar, always shown, is kept quiet: a thin rim, two small bumpers, a dim status light
    of one art pixel and no glow. The suit's gauge hangs under it in a tab sharing its bottom outline, and the energy
    bar, while it shows, in another tab whose edge covers the first's: one seam between the two screens.
  - The **forearm display** round the bag, shown with the inventory: bumpers, a worn warning stripe, screws,
    signal pips and a lamp. The **equipment's unit** at the right edge, in the same metal.
  - The open machine's **module** under the bag (`MachineFrame`, so the Fabricator's too): square on top, where it
    meets the display, which then squares its own bottom edge (`DisplayDocked`); the two share one outline, a dark
    seam. A module narrower or wider than the display shows as a stepped unit, each part edged in full.
- **Yours or a crate's, without words**: the machine's screen is greyer and greener than the hero's navy, and the
  seam lies between them. No label plate: the machine's name is already in its header.
- **Opening**: the metal shows at once and the screens' content fades in by the tablet's rule (`TabletMotion`,
  moved to `game/shared/`): from 60 to 140 ms, never brighter than lit, at once when opened again within 1.5 s.
  Closing is at once. No slide: the slots stay where clicks and scripted runs find them.
- **Light**: the display and the equipment's unit wash what is round them faintly in the screen's blue, and their
  lamps glow (`ScreenGlow`, shared with the tablet), dimmed with the screen. The cuff casts none.
- The bezels take clicks: a stack let go over the metal is not thrown, and the crew's markers keep out of it.

## Deliberately not handled

- The Fabricator's panel is the crafting feature's own: it shows its module's metal and screen but does not fade in
  with the bag's content; it opens at once, as before.
- The concept's sockets (slots pressed into the metal) were variant A's; B draws today's slots on the screen, and so
  do we. Slot textures stay flat style boxes.
- The concept's chest unit for the equipment kept its lamp at the top middle; ours sits at the top right, as on the
  display, so it lives in the 9-slice's corner.
- The art is two screen pixels a pixel at both slot sizes and every resolution; the UI is not scaled for 4K (0073).

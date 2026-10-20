# 0081 — The wrist is one column: segmented gauges that never step, and hardware keys

- Status: accepted
- Date: 2026-10-20
- Supersedes: 0053's "the energy bar shows only while an electric tool is held"

## Context

The wrist computer (0074) kept two things from the flat panels before it: the suit's and the energy gauges, a word or a
bolt over a 6 px bar, read four times a second and so jumping in steps, with no sign of what was just lost; and the sort
button, a slate rounded square that sat in its own column right of the bag, making the bag's display 26 px wider than
the hotbar. The energy bar came and went with an electric tool in the hand, so the row under the hotbar changed width.
The wrist-gauges mockup offered sort keys in four places, four gauge looks, three loss trails and three ways to show
energy. The user picked: the bag's sort key on the display's right edge (A); a crate's header keys restyled as metal
keys where they are; segmented gauges with the game's canister and bolt for labels (A); a trail that flashes and drains
(T3); energy as a number and a bar (N2). And a layout note: every wrist window lines up in one column as wide as the
hotbar.

## Decision

- **One column.** The hotbar's screen, the gauge row and the bag's screen are one width (`Slots.Column`): 400 px at
  the normal slot, 544 large. The suit's gauge and the energy gauge fill it between them, a seam apart, their cells of
  about one width (`Slots.GaugeRowOf`). Only two things stand on the column's right rail, one seam out: the bag's sort
  key and the energy readout.
- **Segmented gauges** (`WristGauge`, shared by the crew's and the items' features): the suit's fifteen cells, three
  suit units each, after the canister; the energy's four cells for each battery the belt can wear, a wider gap between
  batteries, after the bolt. Lit cells glow; a partly filled cell is dim. Low, the suit's turn red and the energy's
  amber, pulsing as the gauge pulsed before, the last lit cell blinking faster. The danger vignette, the dark of
  passing out and the alarm stay as they were; the energy gauge is added under them, so they cover it as the rest of
  the HUD.
- **Never in steps** (`GaugeMotion`): a small change slides at an even pace over a second, a breath's interval, so a
  steady drain or a slow refill moves steadily and leaves no trail. A drop of 5 % of full or more between two reads is
  a hit: the fill falls at once, the lost part flashes white and the tab brightens for 0.3 s, it holds 0.15 s, then
  drains. A big refill eases out, light running along the lit cells and, for oxygen, motes rising.
- **Energy always shown**, as the belt's total (`Charges.BeltOf`), whatever is in the hand, dim with no battery worn,
  so the row never changes width. Its share is of a belt of full batteries, so the cells say how many batteries are on.
  Low is under a quarter of what the worn batteries hold full, as a slot's bar. The readout gives the same charge in
  whole power units, a thin space between thousands ("4 300"), never seconds (0053); it counts the lost part as it
  drains and flashes with it.
- **Hardware keys** (`WristKey`): a housing of the wrist's metal round a rubber face that sinks while held; the glyph
  lights sky under the pointer, white while held, and glows a moment after. The bag's tall sort key is mounted on the
  display's right edge, outside the screen, never dimmed with it (`WristScreen.Mount`); a crate's square sort key and
  the drone badge (0062) stay in the header, as keys. A sort sends light across the sorted grid (`SortSweep`), and the
  bag's flares the display's lamp.
- Every number is in `WristLook` (the client's look constants).

## Deliberately not handled

- **The hit threshold is 5 %, not the 4 % asked for.** A running hero out of air breathes 2 of the suit's 45 units a
  breath, 4.4 %: at 4 % every running breath would flash. 5 % keeps breathing a slow drain.
- The hotbar's "creative" label, shown only in creative mode, still widens the cuff past the column.
- The gauges take no clicks and have no tooltip: the one tooltip stays the inventory's (the user's rule). What they
  measure is said by their pictures.
- The canister is drawn at its art's own size (16 px), one screen pixel a pixel, as the mockup drew it; the bolt at two.
- A small change lags the sim by up to a second as it slides. Accepted: the danger, the alarm and the vignette come
  from the sim's read at once, not from what the gauge shows.

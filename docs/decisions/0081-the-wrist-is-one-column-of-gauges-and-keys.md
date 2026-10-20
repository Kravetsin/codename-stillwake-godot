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
hotbar. Seen in the game, the user dropped the energy bar for the bolt and the number alone, so nothing but the bag's
sort key stands out of the column, and asked for refilling oxygen to be felt: a player barely noticed it.

## Decision

- **One column.** The hotbar's screen, the gauge row and the bag's screen are one width (`Slots.Column`): 400 px at
  the normal slot, 544 large. The suit's gauge and the energy tab fill it between them, a seam apart: the energy tab
  just wide enough for its bolt and "5 400", the suit's gauge the rest (`Slots.GaugeRowOf`). Only the bag's sort key
  stands on the column's right rail, one seam out. Every frame's metal spans the same pixels as the hotbar's cuff
  (checked at x 6 to 417 at the normal slot), though the display's bright rim lies a pixel pair further in on its right.
- **A segmented gauge** (`WristGauge`): the suit's fifteen cells, three suit units each, after the canister. Lit cells
  glow; a partly filled cell is dim. Low, they turn red, pulsing as the gauge pulsed before, the last lit cell
  blinking faster. The danger vignette, the dark of
  passing out and the alarm stay as they were; the energy gauge is added under them, so they cover it as the rest of
  the HUD.
- **Never in steps** (`GaugeMotion`): a small change slides at an even pace over a second, a breath's interval, so a
  steady drain or a slow refill moves steadily and leaves no trail. A drop of 5 % of full or more between two reads is
  a hit: the fill falls at once, the lost part flashes white and the tab brightens for 0.3 s, it holds 0.15 s, then
  drains. A big refill eases out, light running along the lit cells and, for oxygen, motes rising.
- **Energy always shown**, as the belt's total (`Charges.BeltOf`), whatever is in the hand, dim with no battery worn,
  so the row never changes width: the bolt and the charge in whole power units, a thin space between thousands
  ("4 300"), never seconds (0053), green, amber and pulsing when low. Low is under a quarter of what the worn batteries
  hold full, as a slot's bar. The number moves as the gauge did: after a hit it flashes white and counts the lost part
  down as it drains.
- **Air coming back is felt.** A canister breathed (sent once, `Report.RefillOf`): its slot pulses as a shifted
  stack's landing, longer; the suit gauge surges, a sky wash, a white sweep along the lit cells and motes rising thick;
  fine velocity-stretched streaks, motes and pale wisps spiral in to the visor over 0.6 to 0.9 s (`AirSwirl`, the
  luminous toolkit of 0075); air filling the suit is heard (`Sound.AirFill`). A friend's canister, seen in its suit
  jumping more than a breath of air can add (`CanistersSeen`), swirls at its visor and is heard where it is. Coming
  out of thin air or vacuum into room air with a suit to refill (`AirCues`): one soft cyan wave across the cells, a
  few pale motes off the visor and a soft inhale in the helmet (`Sound.Inhale`), once an entry, never again within
  four seconds. A low suit leaving danger without passing out lets the red edges go in 0.15 s to a pale cyan edge that
  fades over 0.7 s, never the whole screen. Both sounds are synthesised placeholders until the audition's picks.
- **Hardware keys** (`WristKey`): a housing of the wrist's metal round a rubber face that sinks while held; the glyph
  lights sky under the pointer, white while held, and glows a moment after. The bag's tall sort key is mounted on the
  display's right edge, outside the screen, never dimmed with it (`WristScreen.Mount`); a crate's square sort key and
  the drone badge (0062) stay in the header, as keys. A sort sends light across the sorted grid (`SortSweep`), and the
  bag's flares the display's lamp.
- Every number is in `WristLook` (the client's look constants).

## Deliberately not handled

- **The cells no longer say how many batteries are worn.** The number says how much is there; the equipment shows the
  batteries.
- **A friend's canister into a suit within 5 units of full shows nothing** on other screens: nothing tells a client a
  friend sent `Refill`, and so small a jump is a breath of room air and the pod's together. The sim has no events.
- **The item card covers the gauge** while the pointer stays on the canister just breathed from the inventory: the
  slot's pulse and the swirl show it, and the gauge shows it once the pointer moves. The one tooltip stays as it is.

- **The hit threshold is 5 %, not the 4 % asked for.** A running hero out of air breathes 2 of the suit's 45 units a
  breath, 4.4 %: at 4 % every running breath would flash. 5 % keeps breathing a slow drain.
- The hotbar's "creative" label, shown only in creative mode, still widens the cuff past the column.
- The gauges take no clicks and have no tooltip: the one tooltip stays the inventory's (the user's rule). What they
  measure is said by their pictures.
- The canister is drawn at its art's own size (16 px), one screen pixel a pixel, as the mockup drew it; the bolt at two.
- A small change lags the sim by up to a second as it slides. Accepted: the danger, the alarm and the vignette come
  from the sim's read at once, not from what the gauge shows.

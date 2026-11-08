# Every loss shows white

- Status: accepted
- Date: 2026-11-03
- Supersedes in part: [0081](0081-the-wrist-is-one-column-of-gauges-and-keys.md) (a steady drain leaves no trail; only a hit's lost part holds, 0.15 s)

## Context

0081 let a steady drain slide down with no trail. Only a drop of 5 % of full in one read counted as a hit, and only a hit
left a lost part, flashing, holding 0.15 s and then draining. Breathing never reaches 5 %, so in play the white part
almost never showed, and when it did it was gone before it read. The user asked for the bar other games have: the loss
turns white first, and only after a delay does the white drain away.

## Decision

- **Every loss falls at once and stays lit white** for `TrailHoldS` (0.6 s), then drains: `TrailDrain` of full a second
  and `TrailDrainPerGap` more for each of full still white. `GaugeMotion` keeps what it showed over the last hold, and the
  white drains only down to the most of it. Under a steady drain, such as a battery spent every read, that leaves a
  white band about a hold wide.
- **A hit still flashes:** a drop of `BigDrop` in one read washes the tab as before. A loss's size changes only the flash.
- **The white is its own lamp tone** (`gauge` subject, `white`), not the pale light that runs over the lit lamps.
- **A capacity change is no loss.** When a helmet goes on or off, the share changes with no air gone, so `SuitGauge`
  shows the new share at once (`GaugeMotion.Show`).
- Rises are unchanged: a small rise slides over `SlideS`, a big one eases out.

## Consequences

- The energy tab's number counts the white part down too, so it lags a loss by the hold.
- We do not wait for a run of losses to stop before draining, as some games do. Under the suit's steady drain, the white
  would grow without end.

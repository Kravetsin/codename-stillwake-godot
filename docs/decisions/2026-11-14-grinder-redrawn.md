# The grinder drawn raw from a picked reference, its wheel spun in code

- Status: accepted; redraws the grinder of [salvage-tools](2026-11-04-salvage-tools.md), raw as [raw tool pictures](2026-11-14-raw-tool-pictures.md) says
- Date: 2026-11-14

## Context

The grinder was the last salvage tool still drawn in code, «Заводская» of 2026-11-04. It got a page of GPT references like
the others, and the user picked «Нынешняя, перерисованная»: the current look, redrawn. It has a long motor tube in worn ochre
enamel with vents at its back, a pistol grip, a cast head, a cut-off wheel under a half guard and a lamp on its end cap.

## Decision

- **Shipped raw:** `refs/grinder.png`, a transparent reference cut by its alpha, `traced/grinder.png`. It is **30 × 12** at
  three quarters (tool-size); the old one was 29 × 15 with a battery under it, and the pick has none.
- **The wheel never turns as a picture.** The head hides its back, so a turned wheel would bring a hole round from behind
  the head. In the three spin frames, code draws the turning instead:
  - light streaks sweep the wheel's face, a third of their turn a frame, only where the wheel is in sight;
  - the washer and nut are cut from the picture and laid over themselves turned through a sixth of a turn (`Traced.picture`):
    the hex smears round as a spinning nut does.
- The grip, the edge (the hub to the rim ahead), the clamp under the tube before the head and the coil's mount over the vents
  are measured from the picture's fit.

## Not handled

- The lamp on the end cap is lit in the reference and stays lit, running or not: nothing in the game reads the grinder's
  lamp.
- The streaks are drawn over the picture's own wheel. A wheel of another size would need its streaks measured again.

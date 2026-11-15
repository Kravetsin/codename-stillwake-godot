# The vitals: air, charge and meals as frameless light at the bottom-left

- Status: accepted
- Date: 2026-11-13
- Supersedes: [0081](0081-the-wrist-is-one-column-of-gauges-and-keys.md)'s gauge row under the hotbar, and
  [energy-ring](2026-11-06-energy-ring.md)'s ring and number

## Context

The gauge row under the hotbar showed air as 15 lamps and charge as a small ring and a number. These lamps were a pixel-art echo.
A heavy helmet looked the same as a basic one, and the row was cramped. Food's meals sat there as a stand-in. On 2026-11-12, over six
rounds on a pick page, the user rejected several things:

- pictures of objects in the gauges;
- rings round the signs;
- one bar without separate pieces;
- thick frames;
- a hologram's scan lines and flicker;
- seconds on the bar (they hold only at rest);
- food tinting the bars;
- rate signs in the meal slots.

The design was cut down on the page and in a plan, now done.

## Decision

- **Place:** a block at the screen's bottom-left, its foot 24 px above the screen's, scaled with the slots (×1.4 large).
  The hotbar stays alone top-left, and the bag opens 18 px under it.
- **Look D, "light and water":** each piece is a wash baked by `gauge.js`, with a slightly darker wandering tide line. A glow
  lies under the lit part. There is no frame. Pieces stand apart with real gaps.
- **Signs:** the game's own oxygen sign and bolt, 1.5× larger, never tinted. The bolt keeps its beat, current, discharges and
  flat blink. The oxygen sign keeps its red picture when short, and the canister ring round it.
- **Air** is one piece as long as the suit holds, lungs and helmet together (`CapacityOf`), at 2.3 px a unit. It is never in
  seconds. Its effects are spans of the piece:
  - the leading edge;
  - low red with its last 5 units blinking;
  - the hit's white trail and chips;
  - the refill's run and rising motes;
  - the canister's white sweep, pale glow and shake;
  - the room air's wave;
  - the fog dimming the last units.

  Hover names the helmet, or «Лёгкие» bare, with the units.
- **Charge:** a piece per store on the belt, its length its capacity (a battery 40 px), in reverse slot order. The store a
  tool drains first therefore stands at the right end. There is no number; hover gives the store's charge and capacity.
- **Fit:** a row keeps its scale until it would pass 300 px, then shrinks alike. Gaps stay.
- **Meals:** three slots under the charge row, each the dish's own picture (the hotbar's) over a thin time light that drains
  right to left.
  - Its last minute pulses.
  - When eaten, the picture pops in. When it ends, the picture greys and fades.
  - A refused meal blinks red with a jolt: all three slots when full, the dish's own slot when too soon.
  - When the hero is hungry, the empty lights breathe ochre.

  What a meal does is shown on hover.
- **No food on the bars.** Bar colour is kept for damage and poison.

## Consequences

- The block is taller than the old row (about 94 px against 30) and covers the world's bottom-left corner. The build catalogue
  covers a long row's tail while it is open.
- A fitted row's length no longer says its total; hover does.
- What a meal does is learnt by its picture or read on hover.
- `WristGauge`, the lamp pictures, the gauge tab's frame and the energy number are gone.

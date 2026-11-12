# Plan: HUD vitals, air and charge as frameless light, meals as rate signs

## Why

The wrist's gauge row under the hotbar ([0081](../decisions/0081-the-wrist-is-one-column-of-gauges-and-keys.md)) shows air as 15 pixel-echo lamps and charge as a small ring and a
number. A heavy helmet looks the same as a basic one, the row is cramped, and food's meals (food plan A2) sit there as
a stand-in (`MealSlots`). The vitals move to the bottom-left corner, larger, drawn in the game's ink and watercolour.

**Decided with the user** (2026-11-12, five rounds on the pick page `2QwfBBTq9K8MbmUe3ZbBuq`, picks in its `db`):
- **Look `look3` = D, "light and water":** frameless pieces of light, each a wash baked by `wash.js` with a slightly
  darker wandering rim (the tide line), an even glow under the lit part. No frame, channel or plate; no rings; no scan
  lines or flicker; no pictures of objects. Separate pieces with real gaps.
- **Icons:** the game's own `sign_oxygen` and `sign_bolt`, in their own colours, never tinted, 1.5× today's size. The
  bolt keeps every effect it has (`BoltPulse`, `FlatBlink`); the O₂ sign keeps its red picture when short and the
  canister ring (PR 542). Hit, surge and refusal show as light behind the sign and a jolt.
- **Air is volume in units, never seconds** (seconds hold only at rest). The bar is pieces by source; this stage has
  one: lungs and helmet together, `CapacityOf` (20 bare, 45 basic, 100 heavy). Later sources of volume (gear, maybe
  food) add pieces after it.
- **Charge:** one piece per belt store; an accumulator is one piece three batteries long. **No number** (`number` = N2);
  the amount is on hover.
- **Hover** on a piece names its source: «Шлем: 45», «Тяжёлый шлем: 100», «Аккумулятор: 3 200 / 5 400».
- **Meals `food2` = D:** three light pieces under the charge row, each showing the meal's rates as signs, its light
  draining as time passes; the food's name on hover.
- **Food is not shown on the bars.** The tint (`tint` = T2) and round five's bubbles were tried and dropped: the bars'
  colour is kept for what matters, damage and poison. A poison damage-over-time is the named next user of a bar tint.
- **Fit:** a row keeps its scale until it would pass its maximum width, then the whole row shrinks; gaps stay.

## Layout

At 1280×720, normal slot size; large multiplies every size by 1.4. The block's left edge is `Slots.Margin` (12), its
bottom 24 px above the screen's, clear of the tablet's rim. Top down:

| Row | What | Box |
| --- | --- | --- |
| air | O₂ sign 33 px, its centre 20 px in; the canister ring round it (radius 12.5 × 1.5); the pieces from x 44 | pieces 10 px tall |
| charge | the bolt 21 × 27, same centre x; the pieces from x 44 | 38 px under the air row |
| meals | three pieces 30 × 12, 34 px apart, from x 44 | 17 px under the charge row |

- Air scale 2.3 px per unit (heavy: 230 px); charge 40 px a battery, 126 px an accumulator; 4 px between pieces.
- **Maximum row width 300 px** (420 large). Past it, px per unit = (300 − gaps) / units. Today only three accumulators
  pass it (386 px), so fit is needed now, at large size especially.
- The block ends left of x 540 at both sizes (the edge keys start at ~550); its top is at y ~603 (569 large).
- **The hotbar stays alone top-left.** The gauge row goes: `Slots.GaugeTop`, `GaugeRowOf`, `GaugeRow` go with it, and
  `InventoryTop` becomes the hotbar's bottom + 18: the bag and equipment open 48 px higher.
- The build catalogue opens bottom-centre (x ~329–951, 249–1031 large) and covers the long rows' tails while it is
  open. Accepted: the vitals do not move.
- `Covers` (markers keep clear): one rectangle for the block.

## Air row

- **One piece now:** length `CapacityOf × scale`. Its name on hover: the worn helmet's item name, or «Лёгкие» bare.
  A helmet put on or off changes the piece's length with an ease (0.45 s); the lit share is shown at once, no flash.
- **Fill:** sky (`#38bdf8`) left to right; the dark wash of the same tone where empty.
- **Kept from `WristGauge`, as spans of the pieces instead of lamps:** the moving fill's bright leading edge; Low
  (< 25 %, `Danger`) red with a pulse, the last 5 units blinking; a hit's white trail that drains, its chips, the hurt
  blink in the kind's colour (`HurtLook`); the room air's wave and the slow refill sweep with rising pale motes; the
  canister surge (white sweep, pale gained span, motes, a shake); the visor fog dimming the last part (`LastDim`);
  `Greyed` sign while the canister cools; `Bright`.
- Rising pale motes stay the one sign of "air coming in"; nothing else on the bar rises.

## Charge row

- **Pieces in reverse slot order:** the sim drains the first store in slot order (`EnergyRing.DrainingOf`), so the
  drained store sits at the right end and the row empties from the right. One reversal in the layout, nothing in the sim.
- Green (`#7ee36b`), amber when low, as today. The drained piece lit brighter with its glint running back; a store put on
  fills in; the spark hops to the next store; a flat press: three red blinks (`FlatBlink`) over the pieces and the bolt.
- No battery: no pieces, the bolt dim.
- `EnergyRing` keeps its logic (shares, draining, glint, hop, fill-in) and loses its arcs: the ring becomes the row.

## Meals row

Replaces the stand-in `MealSlots` (food plan A2, "as built").

- **Slot:** a light piece 30 × 12 in a neutral slate light; empty: its dark wash only.
- **Signs:** the meal's rates as small light glyphs centred in the piece: Calm a wave, Deep an arrow down onto a line,
  Work a double chevron; pod paste (no rates) a dot. Each rate has a colour, on its glyph and on its tip line: Calm
  violet `#b49ae6`, Deep sea-teal `#3ccfb6`, Work wheat `#e2c27e`. The glyphs are drawn by `tools/anime-art` (subject
  `signs`), white masks tinted in code.
- **Time left:** the piece's light drains right to left (`Left / Lasts`). Its last minute (`RefreshWithin`): the piece
  pulses (`WristGauge.Pulse` rate × 2.4).
- **Eating:** the piece flashes in (0.6 s glow). **Refused** (`crew.mealsFull` on all three, `crew.sameFood` on that
  meal's): `FlatBlink` red light behind and its jolt.
- **Hungry** (all three empty): the empty pieces breathe a muted ochre (`#d2a866`, 2.6 s), never red.
- Hover: `MealTips.Of` / `MealTips.Empty` as built, with each rate line in its colour.

## Hover

The game's one tooltip (`Ui.Tip` through `TipSpots`), not a new card: the page's dark-glass card is not built. A
piece's spot is its rectangle; a tiny piece keeps a hover target of at least 8 px. Keys, En and Ru:
`hud.lungs` («Лёгкие: {n}»), the helmet's own item name with `: {n}`, `hud.store` («{name}: {charge} / {capacity}»,
thousands spaced).

## Art

- `tools/anime-art/subjects/gauge.js` stops drawing `gauge_lamp*`; it draws the D strips instead: per tone (sky, red,
  pale, white, green, amber, slate, hunger ochre, and their dark washes) a long strip 304 × 10 cut to length in code
  (its start, then its own 8 px end), and the meal piece 34 × 12, two variants each so neighbours differ. The game
  restores a light's saturation before the finish greys it, as `gauge.js` does today.
- The glow under a lit part is drawn in code (a soft add), not baked.
- The three rate glyphs and the dot join `signs.js`.
- `gauge_lamp*` pictures and their code are deleted.

## Steps, one PR each

### H1: the vitals redrawn and moved
`WristGauge` draws pieces from the strips at the new place; `SuitGauge`, `EnergyBar` move into the bottom-left block;
the gauge row and `GaugeRowOf` go; `InventoryTop` follows the hotbar; fit; hover tips; the number goes. Art from
`gauge.js`. ADR `hud-vitals` (supersedes the gauge row of 0081): the rules above. Crew, energy and items READMEs.

### H2: the meals row
`MealSlots` redrawn as the meals row with the glyphs from `signs.js`, under the charge row; rate colours on its tips.
Food plan A2's stand-in note removed.

## Tests

- **Layout (plain C#, `game.tests`):** the block's top at both sizes stays under the open bag's bottom; every row of the
  largest content (heavy helmet; three accumulators) ends left of x 540 at both sizes; fit keeps a row at or under its
  maximum and its pieces in proportion; the charge pieces are in reverse slot order.
- **Hover:** the tip of each piece names its source; a meal's tip as `MealTips`.
- **Frames:** `tools/snap.ps1` at both sizes: bare, basic, heavy; three accumulators fitted; low air; a canister cooling;
  three meals, one in its last minute; hungry. Each looked at once (in creative mode, frames-in-creative-mode).

## Not done

- Food on the bars (tint, bubbles, currents): tried, dropped. Poison damage-over-time tinting the air bar is the next
  user of a bar colour, not designed.
- A buff area for effects other than meals: decided with the first such effect.
- Item cards stating what actions cost in air units (jetpack per second, sprint): the user's idea instead of seconds on
  the bar; its own task.
- Pieces by source beyond lungs-and-helmet: no second source of volume exists yet.
- The page's dark-glass tooltip: the game's tooltip is restyled game-wide or not at all.

**Accepted:**
- The block is taller than today's row (~117 px vs 30) and sits over the world's bottom-left corner.
- The build catalogue covers a long row's tail while open.
- A meal's identity is on hover; two foods with the same rates look alike in their slots.
- A fitted row's length no longer says its total: the hover does.

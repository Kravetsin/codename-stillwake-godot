# Air segments

- Status: accepted; supersedes [suits are worn](2026-10-27-suits-are-worn.md)'s helmet as the whole capacity and
  `Crew.Refill`, and [food tuning](2026-11-15-food-tuning.md)'s stimulant air as a plain clamp
- Date: 2026-11-15

## Context

The stimulant's units were drawn past a gap at the end of the air gauge. The rest was one bar, the helmet's or the lungs'.
The user wanted one general system rather than one exception: the parts of the air shown in a fixed order and spent in it.
A big helmet also took long to fill aboard, three units a breath whatever it held.

## Decision

- **The air is parts in order:** the lungs (`Crew.Lungs`, 20), the worn helmet's `Tank`, then what food lends. A future
  upgrade goes between the helmet and food. `PartsOf(peer)` lists them; `CapacityOf` is their sum.
- **A helmet adds past the lungs:** basic +25, quilted +50, heavy +80, so the totals stay 45, 70 and 100.
- **One number, cut by parts:** `Hero.Air` stays one number. Parts fill the first first and are spent the last first.
  `Hero.Shape` saves the parts as last cut. When they change, the air is laid into the old parts, the lungs first, and
  each part keeps its fill only if it is still there, capped at its new size. A part taken away takes what it held, with
  no exceptions: under a stimulant with air 50 (20, 25, 5), taking the helmet off leaves 25, not 40.
- **Every helmet fills alike:** an aired room fills a hero's air from empty in `Crew.FillBreaths` (15) breaths, each breath
  giving `ceil(capacity / 15)`. The pod's and the canister's amounts are unchanged.
- **The gauge** draws each part as its own segment of one scale, past a gap, every effect running across them all. Each
  segment's tip names it (the lungs, the helmet, the stimulant) and its units. A helmet's card says `+25 O₂`.
- `Shape` is a saved field whose null means "take the parts as they are now", so saves need no format bump. A hero
  printed after passing out starts with none, full.

## Deliberately not handled

- Putting a helmet on under a stimulant packs the air to the left: the stimulant's units move into the helmet. Nothing is
  lost and the total is the same, so it is left as it is.
- The gauge shows the parts' sizes, not how much each holds apart from the fill; the fill runs across them in order.

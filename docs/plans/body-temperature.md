# Design: body temperature, phases 2 and 3: weight and clothes under the suit

Status: open, 2026-11-18. Phase 1, the body, shipped: [body-temperature](../decisions/2026-11-18-body-temperature.md) holds
what it made true. What is left builds on it: a suit's price becomes its weight, and clothes under it reach the cold.
Each phase is its own branch and its own play.

## The model

- **One limit per suit, the same everywhere.** A suit's price is its weight, not a second number.
- **One slowness.** The body (`BodyHeat.SpeedOf`, the client's) and the weight slow the hero through the same factor: the walk,
  run, climb, a jump's height and the swim in water. Afloat nothing weighs.

## The quilted suit

- `Low` −150°, `High` +50°: free to some 64 cells past the zone; its body settles short of death until about 121 cells out
  (−195°), just short of the field's line. Later suits reach farther.
- Its picture is drawn in another session by the user (2026-11-17); this sets its numbers and its card («Выдерживает до −150°»).
- **No ramp of its own past −200°:** the deep's cold (`Temperature.Deep`) already ends every suit's reach.

## Phase 2: weight and gravity

- **Weight.** An item may have a `Weight`: thousandths of movement it takes at the ship's gravity.
  - the basic suit 100 (−10%), the quilted 150;
  - the jetpack and the helmets small;
  - the card: «Тяжесть: −10% к движению».
- **Move.** The client's `HeroInput.Move` multiplies in `1000 − Σweight × gravity / 1000`, with gravity in thousandths of
  the ship's:
  - aboard: 1000;
  - the first planet: 700 (−7% in the basic suit);
  - open space: 0, so weight is free afloat.
- **Weight never slows work.** Gravity touches neither tools nor hands (the user's call, "for now").
- **Is −10% noticed?** Run drops from 440 to 396 px/s. Taking the suit off in a room or on the planet feels lighter, yet
  the start is not sluggish.
- **Where it lives:** the weight is content (`ItemSpec.Weight`), the sum a query of what is worn (`Items`), gravity the
  surface kind's (`Surfaces.Kind.Gravity`); the client multiplies, as it does the body's.

## Phase 3: clothes under the suit

Two layers, as in 7 Days to Die: clothes always, the suit over them. The choice a player makes is "take the suit off or
keep it on", never a second set to swap.

- **A clothes slot.** A tab «Одежда» in the gear, under the suit's slot; one item, worn with a suit or without.
- **Warmth.** A clothing item has a `Warmth` in degrees. It lowers `Low` only, the hero's with a suit or bare, added the
  way the cape's is; `High` stays. Warm clothes never make heat worse, and never help with it.
- **Leather.** The first clothes, for the first planet: about 15° of warmth, little weight. A bare hero then holds down to
  about −15°, so the planet's nights and storms stop biting; under the basic suit (−110) it reaches −125.
- **Weight** (phase 2) counts clothes like any worn item.
- **No loadout button.** Keeping the suit on or off is the swap; the clothes stay on either way.
- **Assets.** The clothes are painted already, worn as outfits for now ([clothes-painted](../decisions/2026-11-17-clothes-painted.md));
  this phase sets their numbers (`Warmth`, `Weight`) and wires the slot.
- **Not here:** a back slot (the air tank lives in the helmet), clothes against heat.

## Users of the mechanic

- **Phase 2:** every worn item, and every surface's gravity, which already exists (`Surfaces.Kind.Gravity`).
- **Phase 3:** the first planet's nights and storms, for a hero out of its suit.
- **Next:** hotter and heavier planets, and suits past −150°.

## Trade-offs accepted

- **Suits make planet cold trivial.** On purpose: the price is weight, and the bare hero's limits are what planets test.
- **The client applies the weight.** The sim never checks it, as it never checks the body's slowness (0002).

## Out of scope

- **Gravity on work speed:** the drill is mechanical.
- **Food or heat sources warming the body.**
- **A hand-held thermometer:** the user's idea, deferred; the dial's hover reads the place.
- **Wounds or loads slowing a hero:** no user yet.

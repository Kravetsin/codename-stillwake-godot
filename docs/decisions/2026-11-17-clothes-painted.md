# Eleven warm clothes, painted, worn as outfits for now

- Status: accepted; amends [outfits-painted](2026-11-17-outfits-painted.md)'s "no new outfits"
- Date: 2026-11-17

## Context

Temperature is being reworked: the first planet sits at +12 to −8 °C, where a suit is not needed and slows the hero. The
hero needs clothes to wear there and aboard with no suit on: sturdier and warmer than the robes, far lighter than a suit, no
tank. Their rules (slot, warmth, how they meet the suit) are being decided apart; this decision only draws them.

## Decision

- **Painted raw, the robes' way.** GPT painted twelve sets on the robes' outlines (side view, skin keyed green), then
  detailed them in ink and watercolour with sleeves down to the wrist. Each row of a piece is stretched onto the mean of the
  robe's span and the basic suit's in that row: between the two in size.
- **Eleven kept.** The user dropped the mechanic's overall, its straps sat wrong. Kept, in order: quilted jacket, waxed coat,
  knitted sweater, leather jacket with fur, fur-lined parka, rags and foil, rubberised dock coat, felt coat, tool vest, crew
  uniform, stitched hides.
- **Outfits 4 to 14.** They are `hero_outfit_<n>_*` after the four robes (`HeroLooks.Outfits` is 15), chosen in the look
  editor and seen with no suit on, as the robes are: the cheapest way to wear them before their rules exist.

## Deliberately not handled

- No clothing item, slot, warmth or recipe: the temperature rework decides them and may move the pictures behind an item.
- Every set wears the bare hand: the only glove picture is the Трюмный's gauntlet, which fits none of them.
- The torsos keep the robes' bare neck under the head.

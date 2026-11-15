# Food tuning: whole-unit canisters, longer dishes, a stimulant, a purge

Agreed with the user on 2026-11-15, after the jet-cruise playtest. Food stays what food-and-the-body made it: the body's
rates, gear the shell. This pass makes the numbers readable, the dishes worth cooking ahead, and adds two things: a
stimulant that widens the lungs, and a way to empty the stomach.

## Rules

1. **Everything adds, nothing multiplies.** Meals' rates are summed and capped, as today. The tooltip and card say so by
   showing plain sums.
2. **Air amounts are units, not shares.** `Deep` becomes units a canister gives more (`+10`, not `+30%`). `Calm` and `Work`
   stay thousandths: they scale every breath and every work tick, which a unit cannot say.
3. **A new rate, `Lungs`:** units added to the hero's capacity while the meal lasts. Capacity is the helmet's `Tank` (or
   `Crew.Lungs`) plus it. The hero's air is one number, so the extra is spent first and filled last by itself; when the meal
   ends, capacity drops and the existing clamp cuts what is above it. Nothing below the old capacity is ever lost.
4. **The stimulant** (`stim`): a food with `Lungs 20` for 20 minutes, nothing else. Never made: the black market sells it
   as an item (`Research.GearSold`), delivered in a wreck and bought again and again, from its first order's row. It takes a meal slot and, like any meal, keeps hunger off.
5. **The purge**: a command `Purge` empties every meal slot at once. Refused with `crew.noHero` and `crew.noMeals`. The
   hero is then hungry until it eats. `Hero.Purges` counts purges (saved, like `Hurts`) so every peer plays it.
6. **Duration grows with the recipe.** Raw finds and crops as today; galley dishes 25 minutes, the stew (three
   ingredients) 40.

## Numbers (first guess for the playtest)

| Food | Lasts | Rates |
|---|---|---|
| pod paste | 20 min | — |
| seed | 5 min | — |
| ration | 15 min | calm 5%, work 30% |
| canned meat | 15 min | canister +10 |
| fruit | 10 min | calm 5%, work 10% (calm new) |
| glowberry | 10 min | calm 12% |
| stimulant | 20 min | lungs +20 (new) |
| porridge | 25 min | calm 5%, work 40% |
| hash | 25 min | canister +10, work 20% |
| broth | 25 min | calm 30% |
| roast | 25 min | canister +20 |
| stew | 40 min | calm 15%, canister +10 |

Caps: calm 40%, work 50% (as today), canister +25 (a canister fills the basic helmet's 45), lungs +20.

## Client

- **The air gauge** draws the stimulant's units as their own segment at the top of the bar, a different tint; it shrinks
  away when the meal ends.
- **The purge button**: a fourth spot right of the meal slots, bottom left. Held 3 seconds it purges; a ring fills round it
  as round the oxygen sign while the canister cools; let go early, nothing. Dim while there is nothing to purge. On hover,
  its name.
- **The purge itself**: a short retch. In a helmet the visor fills with a sick tint and clears in a few seconds; bare, a
  plume from the mouth. Only the hero's figure changes: the player's view is never covered. Look and sound are picked on a
  page; the try ships a first draft.

## Deliberately not handled

- A stimulant ending while its units are spent costs nothing; ending while full loses those units. That is the price.
- A hero eating only stimulants is never hungry. Stimulants are rare finds; not worth a rule.
- Purging has no cooldown and no cost beyond the meals: nothing is gained by doing it twice.
- No recipe for the stimulant: what it is made of is not designed.
- The other rates the food ADR names (legs, strike, harvest yield) stay for later.

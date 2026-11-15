# Food tuning: whole-unit canisters, longer dishes, a stimulant, a purge

- Status: accepted; supersedes [food and the body](2026-11-12-food-and-the-body.md)'s deep breath in shares and its dish durations
- Date: 2026-11-15

## Context

After the jet-cruise playtest the user found the food numbers hard to read: a canister's "+30%" left a player to work out
what it gave, and two such rates could be read as multiplying. The fruit did too little, cooked dishes lasted no longer
than raw finds, and there was no way to get rid of a meal eaten by mistake. The user also wanted more air for a hard stretch,
taken like food rather than worn like gear.

## Decision

- **Everything adds, nothing multiplies.** Meals' rates are summed and capped, as before.
- **Air amounts are units.** `Deep` is units a canister gives more (+10, not +30%), capped at +25: a canister then fills the
  basic helmet's 45. `Calm` and `Work` stay thousandths: they scale every breath and every work tick.
- **A new rate, `Lungs`:** units added to capacity while the meal lasts (`CapacityOf` = the helmet's `Tank` or `Crew.Lungs`,
  plus it; capped at +20). Air is one number, so the extra is spent first and filled last by itself; when the meal ends the
  existing clamp cuts what is above the old capacity. Nothing below it is ever lost.
- **The stimulant** (`stim`): a food with `Lungs 20` for 20 minutes, nothing else. Never made: the black market sells it as
  an item (`Research.GearSold`), delivered in a wreck and bought again, from its first order's row, at 80. It takes a meal
  slot and keeps hunger off as any meal does.
- **The purge:** `Purge` empties every meal slot at once, refused with `crew.noHero` and `crew.noMeals`; the hero is hungry
  until it eats. `Hero.Purges` counts purges (saved with a default, no format bump) so every peer shows it.
- **Duration grows with the recipe,** as in Valheim, where a complex dish is eaten ahead of an event: raw finds and crops
  as before, galley dishes 25 minutes, the stew (three ingredients) 40.

| Food | Lasts | Rates |
|---|---|---|
| pod paste | 20 min | — |
| seed | 5 min | — |
| ration | 15 min | calm 5%, work 30% |
| canned meat | 15 min | canister +10 |
| fruit | 10 min | calm 5%, work 10% |
| glowberry | 10 min | calm 12% |
| stimulant | 20 min | lungs +20 |
| porridge | 25 min | calm 5%, work 40% |
| hash | 25 min | calm 5%, canister +10, work 20% (calm so it is never weaker than its fruit) |
| broth | 25 min | calm 30% |
| roast | 25 min | canister +20 |
| stew | 40 min | calm 15%, canister +10 |

Caps: calm 40%, work 50%, canister +25, lungs +20.

### Client

- **The stimulant's air** is the top of the same air gauge, drawn past a gap as its own segment by the same rules; no
  effect plays on it. Its tip names the stimulant and its units; the main piece's names the helmet or lungs and theirs.
- **The purge spot:** a fourth spot right of the meal slots, bottom left. Held 3 s while a ring fills round it, as round the
  O₂ sign while a canister cools, it sends `Purge`; let go early, nothing; refused, it blinks red. Dim with nothing eaten.
- **The retch** (bile-slide, picked from four on a page: bile sliding, chunks sticking, mist rising, sludge sloshing): a gag,
  then three gushes of thin olive bile as the body hunches, forearms to the belly, head down (`Retching`). In a helmet the
  bile splashes the glass, slides down it in streaks and gathers on the rim, gone in about 5 s, laid on any helmet's glass
  by its picture's bounds. Bare, it sprays from the mouth (`RetchSpray`): on a deck it falls into puddles with a dark tide
  line that spread and dry after 9 s; afloat its drops wobble, drift and fade after 4 s. Only the figure changes; the
  player's view is never covered.
- **The retch's sound** (picked from six CC0 recordings on a page): "20 Throwing Up" by D.jones, a groan building to two
  heaves and a long wet tail, its first heave on the first gush. This player's own plays in the helmet as the chewing does;
  another's on World where it stands, 8 dB quieter through a helmet.
- **The stimulant's picture** is a code-drawn auto-injector, a draft; it is not yet picked on a page.

## Deliberately not handled

- A stimulant ending with its units spent costs nothing; ending full loses them. That is the price.
- A hero eating only stimulants is never hungry: they are bought, not found in numbers.
- Purging has no cooldown and no cost beyond the meals: nothing is gained by doing it twice.
- No recipe for the stimulant: what it is made of is not designed.
- A drop of bile lands where the feet stood as it left the mouth: a floor falling away under it is not looked for.
- The bile is not grained like the paper of the pick page.
- The other rates [food and the body](2026-11-12-food-and-the-body.md) names (legs, strike, harvest yield) stay for later.

# Food and the body

- Status: accepted
- Date: 2026-11-12

## Context

The game grew in length, one craft chain after another, but not in breadth. A hero with a stack of canisters breathed
them back to back and never ran dry, so air, the only health, stopped mattering short of the farthest ring. The
canister upgrade of [the heavy helmet](2026-11-03-the-heavy-helmet.md) sold a body effect, more air a breath, for gear
money. The user wanted a semi-mandatory side loop like Valheim's food: a crew can play on without it but plays better
with it. It was built in six steps the same day, the plan since retired into this record.

## Decision

- **Gear owns the shell, food the body.** Capacities and protection are gear's: helmets, suits, the cape. The body's
  rates are food's: how much air a breath costs, how much a canister gives, how fast a hero works. Legs, strike and
  harvest yield are named next.
- **A canister has a cooldown.** A hero breathes another only `Crew.CanisterCooldown` ticks (25 s) after the last;
  creative skips it, passing out clears it. Kept as ticks left, never a tick of the clock: each surface keeps its own,
  and a hero carries it across.
- **The cooldown is a ring round the O₂ sign** (canister-mark, picked from four on a page: the ring, a fill from below, a
  strip under the cells, pips; the user's words: the energy sign has the same ring, so a player will notice it). Cooling,
  the sign greys and a sky arc sweeps clockwise from the top; a canister asked for too soon blinks the sign and ring red as
  a flat belt blinks the bolt; ready, a pale ring flashes. The canister's slots grey and get their colour back clockwise.
- **The canister upgrade (`canister1`) is gone.** More air a canister comes from food (deep breath).
- **Three food slots, hunger when all are empty.** Hunger slows building and makes sprinting dearer; it never kills or
  hurts. The pod prints a hero fed. Dishes are named, their rates set by hand; a dish beats the raw foods of its recipe.
- **The first foods' pictures** (food-icons, each picked from four on a page): the ration a pressed bar in waxed paper
  torn back off its end, with a rust band and an ink stamp; canned meat a tall tin with an oxblood label and a cream band,
  rust at its foot; the pod paste a crimped squeeze tube with the pod's teal band, paste curling from its nozzle.
- **The farm's crops' pictures** (picked on a page, step B1): fruit a ribbed gourd, olive going ochre, on a dry woody
  stalk with a curled tendril (fruit_B); the glowberry a cluster of small dusky berries, their light showing through,
  brightest at each heart (glowberry_A).
- **Space's food pictures** (picked on a page, step A4, two of four each, mixed for variety): two fridges, the galley
  fridge in chipped bone enamel (fridge_A) and the bar cooler, dark iron with a glass door and an oxblood header
  (fridge_D); two vending machines, the enamel one on the galley fridge's cabinet (vendingMachine_A) and the glass case
  of bottles (vendingMachine_D), their sign and screen still faintly lit. A piece is one or the other at random: a fridge by
  its id (`Content.Looks`), as every drifting piece; a vending machine by the vessel's seed as it is stamped, as consoles
  and main engines are.
- **The galley's look and sounds** (picked on pages, step B2): the galley a cast-iron range, its stock pot on the hob
  rattling its lid under steam, the fire in its box flickering through the draught slots, the flue up through the deck
  (galley_A); the dishes galley ware seen from the side, each its own vessel (dishes_A), their fruit and berries
  recoloured to the picked crops; its pot boiling hard while it cooks (galleyLoop_C), a wooden spoon knocked on a tin
  lid as a dish is done (galleyDone_D), and three soft chews whenever a hero eats (eat_A).

## As built

- Every number is a first guess for the playtest, in `Content.Diet` and `Content.Foods`: the raw foods' rates, the
  dishes' (each lasts 18 000 ticks; no loadout fills every cap), the caps, hunger's sprint and work.
- The HUD's meal slots were a stand-in (`MealSlots`) until [hud-vitals](2026-11-13-hud-vitals.md) placed food under the charge row.
- A repair takes both ticks of a `Work` worth two, its last mending: it has no part to pay nor last-tick check.
- A vending machine stands 4 × 5 cells in derelicts' quarters (`mess`), gives the fridge's mean five times over and
  glows as the emergency lamp does, no power. Seeds stay sold: they are the farm's input; every other food is unsold.
- The galley is 4 × 5 like the workbench, works at full speed, a dish in 200 ticks, roast and stew in 300. A hero's own
  eating is heard, a friend's not.

## Not done

- Death or damage from hunger; a hunger meter; walking slower.
- Free mixing or computed dishes; black-market recipes; feeding another hero.
- Legs, strike and harvest yield: the rates named next.
- The jetpack's cost, rest breath, base air: playtest tuning.
- Deep breath on tank canisters or drone trips.

Accepted: water cooked is oxygen not made; paste takes a slot; a cooked fruit gives no seed back; with canned meat and a
stack of canisters a resting hero breathes indefinitely, as canisters stay scarce
([scarce-oxygen](2026-11-07-scarce-oxygen.md)); a fed hero's extra `Work` tick is lost where it would finish a plan or
start a part; leaving and rejoining wipes meals and the cooldown and prints paste.

## Consequences

- Canisters stop being an endless health bar; between them a hero lives on room air, the pod and its suit.
- The board's `order4` loses a node. Old saves holding `canister1` read it as an unknown granted id; saves break at
  release anyway.
- Crew no longer reads Knowledge.

## Superseded

The canister upgrade of [the heavy helmet](2026-11-03-the-heavy-helmet.md); the helmet itself stands.

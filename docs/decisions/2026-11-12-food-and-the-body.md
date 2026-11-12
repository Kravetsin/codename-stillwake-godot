# Food and the body

- Status: accepted
- Date: 2026-11-12

## Context

The game grew in length, one craft chain after another, but not in breadth. A hero with a stack of canisters breathed
them back to back and never ran dry, so air, the only health, stopped mattering short of the farthest ring. The
canister upgrade of [the heavy helmet](2026-11-03-the-heavy-helmet.md) sold a body effect, more air a breath, for gear
money. The user wanted a semi-mandatory side loop like Valheim's food: a crew can play on without it but plays better
with it. The plan is [food](../plans/food.md).

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

## Consequences

- Canisters stop being an endless health bar; between them a hero lives on room air, the pod and its suit.
- The board's `order4` loses a node. Old saves holding `canister1` read it as an unknown granted id; saves break at
  release anyway.
- Crew no longer reads Knowledge.

## Superseded

The canister upgrade of [the heavy helmet](2026-11-03-the-heavy-helmet.md); the helmet itself stands.

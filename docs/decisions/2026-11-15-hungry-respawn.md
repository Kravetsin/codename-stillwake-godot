# Hungry respawn: no pod paste, a hunger sign, one icon size

- Status: accepted; supersedes [food and the body](2026-11-12-food-and-the-body.md)'s printed paste and [the vitals](2026-11-13-hud-vitals.md)' sign sizes
- Date: 2026-11-15

## Context

The pod printed every hero holding pod paste, a 20-minute meal, so a fresh hero was never hungry. In play it got in the way:
a player usually carries food, and the paste held a slot, so after a death they had to throw up before they could eat. A
first try let the first meal push the paste out; the user preferred no paste at all and a hero printed hungry, with hunger
shown by a sign and its costs on hover. The vitals' signs had grown each its own size; the user asked for one size and even
spacing.

## Decision

- **No pod paste.** The pod prints a hero (on `Enter` and on waking) with no meal: hungry until it eats. Hunger still never
  hurts: a running breath costs `Diet.HungrySprint`, work goes at `Diet.HungryWork`. Creative heroes are never hungry.
- **Saves:** format 65 drops every `podPaste` meal; a hero with nothing else wakes hungry. A closed surface has no slices
  and is skipped.
- **The hunger sign** (the user's pick B of five, crossed fork and spoon in the meal slots' muted ochre): `sign_hunger` from
  `tools/anime-art/subjects/signs.js`, in the signs' column on the meals row. Lit while hungry, breathing with the empty
  slots' ochre, faded out on eating. Hover, and an empty slot's while hungry: "Air use when running +25%", "Work speed −25%",
  "Eat something", the percents computed from `Diet`.
- **One icon size, even rows.** `Vitals.Icon` (22 block units) is every vitals icon's side: the oxygen sign, the bolt by its
  height, the thermometer's sign, hunger's, each meal's picture and the purge ring. The air, charge, thermometer and meals rows
  centre `Vitals.Pitch` (30) apart; the meals stand a `Gap` apart like the rows' pieces.

## Consequences

A new player starts hungry, so the first minutes run and build slower until they eat; the starting wreck has food to find.
The oxygen sign reads a little smaller than its box, its picture being a ring and a small 2: accepted, the boxes match.

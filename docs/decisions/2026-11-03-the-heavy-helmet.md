# The heavy helmet and richer canisters

- Status: accepted; the canister upgrade superseded by [food-and-the-body](2026-11-12-food-and-the-body.md)
- Date: 2026-11-03

## Context

Once the jetpack flies, the suit's air runs short: thrust costs two units a second and the breath one, so the basic helmet's 45
last some 15 seconds of flight and a canister's 20 some seven more. The user wanted a bigger suit to buy on the black market
about then, and an upgrade making canisters give 40 instead of 20. Since [suits-are-worn](2026-10-27-suits-are-worn.md) the air is
the helmet's, so the bigger suit is a helmet; the body stays as it is.

## Decision

- **The heavy helmet** (`Helmet.Heavy`, `heavyHelmet`) holds 100: not twice 45, so it reads as more than a doubling (the user's
  word). It is worn as any helmet: put on, it keeps the air the hero has and fills from there; taken off, the air is cut to the next.
- **Its blueprint only the black market sells**, on the row of `order3`, whose order asks for the frames the jetpack is made of.
  Made at the workbench beside the jetpack, of a frame, plates, tubes, three glass, two canisters and wire. Its block costs 5000,
  the user's price: four times its worth, which the recipe is tuned to (`ResearchTests`). A craft's block is priced by its worth as
  a blueprint's is (`Prices.WorthOf`).
- **The canister upgrade** (`canister1`, kind `Upgrades.Canister`) for 10000, a row later, on `order4`: each one bought adds
  `Crew.CanisterUp` (20) to what a canister breathed gives, for the whole crew. One step only for now. It changes the breath alone,
  never what a canister loads into a tank, or the ship's whole oxygen economy would double with it.
- **Off the grid any helmet ends in a breath.** `Field.EdgeCost` stays the basic helmet's 45 and the cold's curve with it, so the
  basic suit pays what it paid before; off the grid a breath costs `Field.OffGrid`, at least any tank (ContentTests).

## Consequences

- A canister is still nearly half a helmet with both bought: 20 of 45, 40 of 100.
- The upgrade is the stronger buy, for flight is paid in canisters, which stack by twenty: it halves the ice a flight costs. Hence
  its price and its later row; both are first guesses for the playtest.
- At the world's last cells, short of off the grid, the heavy helmet outlasts the basic one a breath or two. Nothing is there.
- **Looks, as picked** from eight helmets and four icons: a diver's steel bell, a barred port in front and one at the temple, bolted
  to a breastplate, its hose to the pack ("a bit jokey, but it fits"); the upgrade, two canisters strapped, their valves joined by a
  tee. The others were rejected as the basic helmet with bottles stuck on: a later helmet is designed whole. A helmet other than the
  basic one has its own `hero_<id>_shell` and `_visor` and its lamp's place (`HeadLampArt.OnOf`).

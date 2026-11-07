# Scarce oxygen

- Status: accepted; supersedes the pod's rate of [leaks-tripled](2026-11-06-leaks-tripled.md)
- Date: 2026-11-07

## Context

After the leaks were tripled, air was still never short. The pod made three vaporizers' worth from nothing, enough for
the starting ship, its lock and its hold, for ever. The stream past the ship brought some 65 canisters an hour, about
585,000 units, while a vaporizer at full power burns 72,000. A fire breathed a unit a second, 5% of a vaporizer. Four
canisters filled a tank for half an hour, and the electrolyser had no reason to be built.

## Decision

- **The pod holds room A alone:** `Air.PodRate` = 12000 a period, a vaporizer's rate, just over room A's leak of 11520.
  The lock, the hold and every room built after need a vaporizer and liquid.
- **A canister is a third of what it was:** `Air.Canister` = 3000, so a tank takes twelve. What a hero breathes from one
  (`Crew.CanisterRefill`) is unchanged.
- **Every other supply pod holds a canister:** its loot's canister is one in two.
- **A fire breathes three units a second** (`Furnace.Breath` = 3): a furnace or generator alight costs 1800 a period,
  15% of a vaporizer.
- **A bottle of water stays 4500** (`Water.Bottle`), no longer half a canister but one and a half, so ice melted and
  split weighs more than what is salvaged.

## Consequences

- Salvaged canisters now cover about one vaporizer if every one is caught, so a growing ship needs ice and the
  electrolyser.
- A fire empties a room's tanks three times as fast as before, but only while its room leaks less than its vaporizers
  make: in a room at its budget the store stays near empty, and what a breath or a fire lacks is forgiven.
- A canister still sells for 117 credits, a third of the oxygen for the same price; the market is left for the playtest.

## Not done

- Oxygen as an input to production (an oxygen-blown furnace, refuelling from tanks, hydrogen) is its own design.
- Breathing is unchanged: the shortage comes from building and burning, not from standing still.
- Steady consumers do not shrink a room's budget; they only empty its tanks sooner.

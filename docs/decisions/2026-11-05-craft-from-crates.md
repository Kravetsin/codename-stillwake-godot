# A paid craft takes from every crate on the ship

- Status: accepted; changes 0008's "paid from the sender's inventory" for crafts only
- Date: 2026-11-05

## Context

A paid craft took its cost from the sender's inventory alone (0008), so a craft at the workbench meant a walk to each crate
for its parts first, then back. The user asked to craft from the ship's crates at any distance. 0008 had turned away from
P1's ship-wide stock ("a magic store with no place leaves nothing to carry or automate"); that stays true of building,
pipes, cables and canisters, which still pay from the hand. Crafting is what the walk wore down most.

## Decision

- **The stock** is a peer's own slots, then every crate (`MachineTrait.Container`) on its surface by machine id, however
  far, behind any wall. A station's slots are not in it, nor the floor.
- **A paid `Craft` takes its whole cost from the stock** (`ShortOfStock`, `TakeStock`): from the sender's own slots as
  `Take` does, the rest from each crate in turn, its last slots first. Refused `items.notEnough` only when all of it together
  holds too little. Nothing in creative mode, as before.
- **A `Cancel` gives back into the stock** (`FullForStock`, `GiveStock`): the canceller's own slots first, the rest into
  each crate by id; refused `items.full` only when none of it has room. So a full bag no longer blocks a cancel.
- **The station's panel counts the stock** (`StockTotals`): a card is lit when the crates hold what it needs.

## Rejected

- **Crates in the station's room only:** keeps the ship's layout in play, but the user chose the whole ship.
- **A radius around the station:** a number to tune with no rule of the game behind it.
- **Building and pipes from the stock as well:** not asked for; plans are fed by hand and by drones (0062).

## Consequences

- Drones (0062) matter less for paid crafts; they still feed entries made from a station's inputs, furnaces and outputs.
- A refund may land in another crate than the one it came from: the first by id with room.
- A drone heading for a crate a craft emptied finds less there, as after a hand's take.
- On a planet the stock is that surface's crates: a station there does not reach the ship's.

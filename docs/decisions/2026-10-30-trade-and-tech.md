# Trade and a tech tree

- Status: partly superseded by [tech-graph](2026-10-30-tech-graph.md): the tree's shape and the price list; by [trade-capsule](2026-10-30-trade-capsule.md): selling a unit at a time
- Date: 2026-10-30
- Supersedes [0024](0024-blueprints-are-learned-by-analysis.md) and
  [0027](0027-what-is-analysed-opens-what-is-made-of-it.md): the analyzer is gone

## Context

The economy had no sink the player wants to feed. Mega-builds cost much, but once. Every other idea was a chore: upkeep,
decay, fees. Inventing a craft round every resource would never end. Analysis opened things by what the player had
already touched, so it gated nothing and gave no reason to process anything further.

Selling is a sink the player feeds willingly, if credits buy what the player wants. Blueprints are what the player wants
first. Satisfactory's tiers control when each thing can be had without a story to pace it.

## Decision

- **Two stations, both of what the start can afford.** The trade station (`tradeStation`) sells what is put in it. The tech
  station (`techStation`) opens the tree. Neither needs power. Both are known from the start, and one tech station stands
  in the wreck's torn room.
- **The tree** (`Content.Research.Tiers`). A tier holds branches, a branch a chain of blocks, a block a few blueprints and
  crafts bought together. A block needs only the one before it in its branch; there are no edges across branches.
- **A tier opens by its deposit:** resources put in, for good, by any peer, part by part. A tier is open when it and every
  tier before it hold their full deposits; nothing about opening is saved but what was deposited.
- **A block is bought with credits.** First blocks are cheap: 5 credits, a few chunks' worth. The engine is the last and
  the dearest, 25,000.
- **Selling** (`sim/Trade`). A station sells one unit every `Market.SellEvery` ticks, from its first slot holding what has a
  price. A slot remembers its item, so drones keep it stocked: automation is what makes credits flow.
- **Prices never fall.** The market is steered by the price list: refined always pays more than what it is made of, a test
  says so. In a world alone nothing is gained by punishing the player for selling what they are good at making.
- **Credits, deposits and what is known are the ship's** (`Knowledge`, one a world), shared by the crew on every surface.
- **Softlocks are ruled out by a test.** `ContentTests.Reachable` walks the tree over what the stream gives and what
  stations make, and pins what opens only with a planet's goods. Credits are not walked: any priced good earns them, and
  raw salvage has a price.
- Saves: format 37. Older worlds no longer load: nothing is released, and nothing in an old save can say what it would
  have bought.

## Deliberate omissions

- Falling prices, and any market state: see above.
- Credits for anything but blueprints. Coordinates, inventory upgrades and cosmetics are the next users.
- A discovery list: the whole tree shows plainly, greyed out where it is not open yet.
- The drone switch stops drones, not selling.
- No machines stand on a planet, so a planet's goods are carried up to sell. A dead hero's bag stays in the body, not in a
  deposit.
- Taking a trade station apart with goods in it: emptied by hand first, as a crate is.
- Raising a deposit in a later build shuts a tier already opened in a save. We do not ship saves across such a change yet.
- Who buys goods when the ship is alone in space is an open question of fiction. It changes no mechanic.
- The tech station takes no power, so the first cable is met at the Former, not at the start.

## Consequences

- `Analysable`, the violet "?", lessons and the analyzer's panel are gone. An item's card shows what it sells for.
- `Research` holds the tree's rules and has no slice of its own to speak of; `Trade` owns selling and will own the next
  uses of credits. The balance (prices, deposits, block prices) is data, `Content/Research.cs` and `Content/Market.cs`, to
  be tuned by playtest.

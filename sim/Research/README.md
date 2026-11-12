# Research

## Files

| File | Purpose |
| --- | --- |
| `Blueprints.cs` | Every blueprint: what a card of the build menu places, by the name the tree knows it by. |
| `Prices.cs` | What a block costs, worked out from what it opens. |
| `Research.cs` | Knowing blueprints and crafts: what the world knows, what the rule grants it, and the market's rings it buys more from. |

Knowing blueprints and crafts ([the-board](../../docs/decisions/2026-11-05-the-board.md), [tablet-tree](../../docs/decisions/2026-10-30-tablet-tree.md)). The market's rings are data,
`Content/Research.cs`. A blueprint not known cannot be planned (`building.unknown`), a craft not known not queued
(`crafting.unknown`).

- **Depends on:** `Grid` (`MachinesOf`, `Version`, for the rule), `Items` (`Creative(peer)`), `Knowledge` (what is known, the credits and the counts).
- **State** (`ResearchState`): empty; all of it is the world's `Knowledge`, so a surface shares it.

## Rules

- A blueprint is named as the build menu names its card: a wall or back wall by kind and material (`wallIron`), anything
  else by the grid's name (`floor`, `furnace`). A craft by the item it makes. Cables are no blueprint and always known.
- **The black market** is all there is to buy, its first ring open from the start; every upgrade stands there. Its later rings open by the **mafia's
  reputation** ([mafia-reputation](../../docs/decisions/2026-11-08-mafia-reputation.md)): ring `r` is open at `Knowledge.Level >= r`, a gate done when its ring is.
  A gate's `Order` is what its level wants. In sevenths, with `w` an item's worth: the target is `Σ count × w × 7`; the points `Σ (min(sold, count) × w × 7 +
  max(sold − count, 0) × w)` over the wanted goods, `sold` counted from the level's `Base`, plus everything else sold since at `× 1`, never below 0.
  `Walk` raises the level, every tick, once the points reach the target, the next counted from what is sold then: one level a tick, so one capsule
  opens one. Every surface's research calls `Open`, which Knowledge keeps once.
- What is known at the start is the map's `Layout.Known` (the wreck's `Content.Research.StartKnown`, empty); a map with none knows
  everything. A creative peer knows everything and sees every node done (`Knows(peer, …)`, `Done(peer, node)`); the world does not, so nothing it arms (magnetic flares) changes.
- **The rule** ([the-board](../../docs/decisions/2026-11-05-the-board.md), [one-to-understand](../../docs/decisions/2026-11-12-one-to-understand.md)): a blueprint or craft is understood, and granted, once one of each
  part was ever obtained (`Knowledge.ObtainedOf` above 0) and, for a craft, a machine that makes it stands on some surface. `Walk`, in
  the derivation phase, checks when a count or that surface's grid has moved, and on a load's first tick. What only the market
  opens (`Content.Research.Market`) and upgrades stay outside it. Known stays known, its machine gone or not.
- **A node is reached** when everything it `Needs` is done: a block needs its ring's gate, a gate the gate before it. A block is done when everything it opens is known or on its way (`Knowledge.InFlight`); gear and the confiscated container
  never are (`Repeats`): sold again and again, gear one to a hero.
- **A block is bought** with `BuyBlock` once reached, from anywhere. `Prices.Of` leaves the ship's credits: what it opens
  is worth (a blueprint its cost, a craft its item, gear its hand worth) times its `Times`, to two figures. An upgrade
  (`Content.Upgrades`) is bought the same way, and `Items` and `Crew` read what it opens.
- **A purchase is delivered** ([mafia-deliveries](../../docs/decisions/2026-11-08-mafia-deliveries.md)): `BuyBlock` spends and calls `Knowledge.Deliver`, granting
  nothing; `Debris` brings it along a trade station's row and grants it when caught or broken. Gear (`Content.Research.GearSold`) is an item,
  never a blueprint or a craft: its worth is a hand worth in `Content.Market`, its `Times` its old blueprint's.

## Refusals

| Key | When |
| --- | --- |
| `research.bought` | the block is bought already, or on its way |
| `research.locked` | no such node; a buy of a gate |
| `research.reputation` | the block's ring is past the mafia's level |
| `research.noCredits` | short of its price |

## API

- **Queries** (`IResearchQueries`): `Knows(placement)`, `Knows(id)`, the world's; `Knows(peer, …)`, a peer's; `Recipes()`, every blueprint then every craft as
  `Recipe(Id, Placement, Cost, Known)`, creative mode aside; `Credits`; `Done(node)`, `Done(peer, node)`; `Reached(node)`; `BuyProblem(block)`;
  `Standing()`, the next level, what it wants and its points and target.
- **Commands:** `BuyBlock(Block)`, the block named by what it opens. `Fuzz` draws it at any node, refused ones too.

## Invariants

- Credits move here only by a block's price, and never go negative.
- A node's state is worked out from what is known and what is sold, never saved.
- After a walk nothing the rule allows is ungranted, and a world loaded before it grants the same (`UnderstandingTests`).

## Deliberately not handled

- A level's goods sold before it is the next count at a seventh: an auto station sells everything priced.
- An order asked for more by a later build leaves a level reached as it is.

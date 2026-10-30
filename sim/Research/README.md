# Research

## Files

| File | Purpose |
| --- | --- |
| `Blueprints.cs` | Every blueprint: what a card of the build menu places, by the name the tree knows it by. |
| `Research.cs` | Knowing blueprints and crafts: what the world knows, the tree it buys more of at tech stations, and the tiers' deposits. |

Knowing blueprints and crafts ([trade-and-tech](../../docs/decisions/2026-10-30-trade-and-tech.md)). The tree is data,
`Content/Research.cs`. A blueprint not known cannot be planned (`building.unknown`), a craft not known not queued
(`crafting.unknown`).

- **Depends on:** `Grid` (`IGridQueries`: which machine is a tech station), `Items` (`IItemsQueries.Totals`, `Creative`;
  `IItemsOps.Take`, for a deposit), `Knowledge` (what is known, the credits and the deposits).
- **State** (`ResearchState`): empty; all of it is the world's `Knowledge`, so a surface shares it.

## Rules

- A blueprint is named as the build menu names its card: a wall or back wall by kind and material (`wallIron`), anything
  else by the grid's name (`floor`, `furnace`). A craft by the item it makes. Cables are no blueprint and always known.
- What is known at the start is the map's `Layout.Known` (the wreck's `Content.Research.StartKnown`); a map with none knows
  everything. In creative mode everything is known and every tier open.
- **A tier is open** when it and every tier before it hold their full deposits. The first asks nothing.
- **A block is bought** with `BuyBlock` at a tech station when its tier is open and the block before it in its branch is
  bought. Bought means everything it opens is known. The price leaves the ship's credits.
- **A deposit** (`Deposit`) takes from the sender's bag what it carries of what the tier still lacks, for good. Part of it
  is enough. Only the next tier shut takes one.

## Refusals

| Key | When |
| --- | --- |
| `research.noMachine` | the machine is no tech station |
| `research.bought` | the block is bought already |
| `research.locked` | its tier is shut, the block before it unbought, no such block; a deposit past the next shut tier, no such tier |
| `research.noCredits` | short of its price |
| `research.tierOpen` | the deposit's tier is open already |
| `research.nothingToDeposit` | the sender carries nothing it lacks |

## API

- **Queries** (`IResearchQueries`): `Knows(placement)`, `Knows(id)`; `Recipes()`, every blueprint then every craft as
  `Recipe(Id, Placement, Cost, Known)`, creative mode aside; `Credits`; `Bought(block)`; `Open(tier)`;
  `Deposited(tier, item)`; `BuyProblem(block)`, the refusal leaving the machine aside.
- **Commands:** `BuyBlock(Machine, Block)`, the block named by the first thing it opens; `Deposit(Machine, Tier)`. `Fuzz`
  draws both, refused ones too.

## Invariants

- Credits move here only by a block's price, and never go negative.
- A tier's state is worked out from `Deposits`, never saved.

## Deliberately not handled

- Edges across branches: order within a branch and tiers are enough.
- Refunds of a deposit: it is spent, as a sink should be.
- A tier opened in a save and then asked for more by a later build shuts again.

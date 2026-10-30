# Research

## Files

| File | Purpose |
| --- | --- |
| `Blueprints.cs` | Every blueprint: what a card of the build menu places, by the name the tree knows it by. |
| `Prices.cs` | What a block costs, worked out from what it opens. |
| `Research.cs` | Knowing blueprints and crafts: what the world knows, the tree it buys more of at tech stations, and its gates' deposits. |

Knowing blueprints and crafts ([tech-graph](../../docs/decisions/2026-10-30-tech-graph.md)). The tree is data,
`Content/Research.cs`. A blueprint not known cannot be planned (`building.unknown`), a craft not known not queued
(`crafting.unknown`).

- **Depends on:** `Grid` (`IGridQueries`: which machine is a tech station), `Items` (`IItemsQueries.Totals`, `Creative`;
  `IItemsOps.Take`, for a deposit), `Knowledge` (what is known, the credits and the deposits).
- **State** (`ResearchState`): empty; all of it is the world's `Knowledge`, so a surface shares it.

## Rules

- A blueprint is named as the build menu names its card: a wall or back wall by kind and material (`wallIron`), anything
  else by the grid's name (`floor`, `furnace`). A craft by the item it makes. Cables are no blueprint and always known.
- What is known at the start is the map's `Layout.Known` (the wreck's `Content.Research.StartKnown`); a map with none knows
  everything. In creative mode everything is known and every node done.
- **A node is reached** when everything it `Needs` is done. A block is done when everything it opens is known, a gate when
  its deposit is full.
- **A block is bought** with `BuyBlock` at a tech station once reached. `Prices.Of` leaves the ship's credits.
- **A deposit** (`Deposit`) takes from the sender's bag what it carries of what a gate still lacks, for good. Part of it is
  enough. Only a gate reached takes one.

## Refusals

| Key | When |
| --- | --- |
| `research.noMachine` | the machine is no tech station |
| `research.bought` | the block is bought already |
| `research.locked` | what it needs is not done; no such node; a buy of a gate, a deposit at a block |
| `research.noCredits` | short of its price |
| `research.gateOpen` | the gate is full already |
| `research.nothingToDeposit` | the sender carries nothing it lacks |

## API

- **Queries** (`IResearchQueries`): `Knows(placement)`, `Knows(id)`; `Recipes()`, every blueprint then every craft as
  `Recipe(Id, Placement, Cost, Known)`, creative mode aside; `Credits`; `Done(node)`; `Reached(node)`;
  `Deposited(gate, item)`; `BuyProblem(block)`, the refusal leaving the machine aside.
- **Commands:** `BuyBlock(Machine, Block)`, the block named by the first thing it opens; `Deposit(Machine, Gate)`. `Fuzz`
  draws both, refused ones too.

## Invariants

- Credits move here only by a block's price, and never go negative.
- A node's state is worked out from what is known and `Deposits`, never saved.

## Deliberately not handled

- Refunds of a deposit: it is spent, as a sink should be.
- A gate filled in a save and then asked for more by a later build shuts again.

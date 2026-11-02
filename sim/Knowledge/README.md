# Knowledge

## Files

| File | Purpose |
| --- | --- |
| `Knowledge.cs` | What the crew knows and has earned, wherever it did (0076): the blueprints and crafts known, the credits, the gates' deposits, what it has obtained and sold. |

What the crew knows and has earned, wherever it did ([0076](../../docs/decisions/0076-a-world-is-made-of-surfaces.md),
[trade-and-tech](../../docs/decisions/2026-10-30-trade-and-tech.md)). A global: one per world, made by the ship's
registry and shared by every surface's, saved second in the ship's `slices`. Records only: no commands, no systems.
`Research` and `Trade`, one a surface, hold the rules.

- **Depends on:** nothing; it is made with the id of every blueprint and craft (`ResearchModule.Ids`), what a map
  with no `Layout.Known` is granted.
- **State** (`KnowledgeState`, members internal): `Granted` — the blueprints and crafts known, by id, sorted ordinally,
  from the map's `Layout.Known` and what is bought since; `Credits`; `Deposits` — `Deposited(Tier, Item, Count)`, sorted
  ordinally by gate, then item; `Obtained` — `Tally(Item, Count)`, in the order first obtained; `Sold` — `Tally`s, sorted
  ordinally by item. Saved, those two omitted when empty. Derived, never saved: where each item stands in `Obtained`, looked up and never iterated; `Version`.
- **Files:** `Knowledge.cs` — the module, its state, query and operation interfaces.

## API

- **Queries** (`IKnowledgeQueries`): `Granted(id)`, `GrantedIds`, `Credits`, `DepositOf(gate, item)`, `Obtained`,
  `ObtainedOf(item)`, `SoldOf(item)`; `Version`, which moves whenever something is obtained or newly granted.
- **Operations** (`IKnowledgeOps`): `Earn(credits)`; `Spend(credits)`, which throws when short, the caller having checked;
  `Grant(id)`, again changes nothing; `AddDeposit(gate, item, count)`; `Obtain(item, count)` (`Items`) and
  `Sell(item, count)` (`Trade`), a count of 0 or less counting for nothing.

## Invariants

- `Granted` only grows, holds each id once, sorted. `Deposits` holds a gate and item once, sorted.
- `Credits` never go negative.
- `Obtained` holds each item once, its counts above zero and only growing; `Sold` likewise, sorted.
- Whatever lies in a slot or on the floor has a count or is known from the start (`CountsTests`), creative mode aside.

# Knowledge

## Files

| File | Purpose |
| --- | --- |
| `Knowledge.cs` | What the crew knows and has earned, wherever it did (0076): the blueprints and crafts known, the credits, the gates' deposits. |

What the crew knows and has earned, wherever it did ([0076](../../docs/decisions/0076-a-world-is-made-of-surfaces.md),
[trade-and-tech](../../docs/decisions/2026-10-30-trade-and-tech.md)). A global: one per world, made by the ship's
registry and shared by every surface's, saved second in the ship's `slices`. Records only: no commands, no systems.
`Research` and `Trade`, one a surface, hold the rules.

- **Depends on:** nothing; it is made with the id of every blueprint and craft (`ResearchModule.Ids`), what a map
  with no `Layout.Known` is granted.
- **State** (`KnowledgeState`, members internal): `Granted` — the blueprints and crafts known, by id, sorted ordinally,
  from the map's `Layout.Known` and what is bought since; `Credits`; `Deposits` — `Deposited(Tier, Item, Count)`, sorted
  ordinally by gate, then item. Saved.
- **Files:** `Knowledge.cs` — the module, its state, query and operation interfaces.

## API

- **Queries** (`IKnowledgeQueries`): `Granted(id)`, `Credits`, `DepositOf(gate, item)`.
- **Operations** (`IKnowledgeOps`): `Earn(credits)`; `Spend(credits)`, which throws when short, the caller having checked;
  `Grant(id)`, again changes nothing; `AddDeposit(gate, item, count)`.

## Invariants

- `Granted` only grows, holds each id once, sorted. `Deposits` holds a gate and item once, sorted.
- `Credits` never go negative.

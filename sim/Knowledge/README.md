# Knowledge

What the crew knows, wherever it learned it ([0076](../../docs/decisions/0076-a-world-is-made-of-surfaces.md)): the
blueprints and crafts the world was granted and the items analysed. A global: one per world, made by the ship's
registry and shared by every surface's, saved second in the ship's `slices`. Records only: no commands, no systems.
`Research`, one a surface, keeps what each analyzer is working on and works out what is known from this.

- **Depends on:** nothing; it is made with the id of every blueprint and craft (`ResearchModule.Ids`), what a map
  with no `Layout.Known` is granted.
- **State** (`KnowledgeState`, members internal): `Granted` — the blueprints and crafts known from the start, by id,
  sorted ordinally, from the map's `Layout.Known`; `Analysed` — the items analysed, by id, each once, sorted
  ordinally. Saved.
- **Files:** `Knowledge.cs` — the module, its state, query and operation interfaces.

## API

- **Queries** (`IKnowledgeQueries`): `Granted(id)`, `IsAnalysed(item)`, `Analysed` — live, sorted.
- **Operations** (`IKnowledgeOps`): `Learn(item)` — an analyzer has finished it; again, nothing changes.

## Invariants

- `Analysed` only grows, holds each item once, sorted.

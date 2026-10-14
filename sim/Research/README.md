# Research

Knowing blueprints (ADR 0024): which blueprints the world knows, and analyzers learning more from
what is put in them. A blueprint not known cannot be planned. Ported from
`packages/sim/src/modules/research`; its rules hold here unchanged.

- **Depends on:** `Grid` (`IGridQueries`: which machines are analyzers), `Items` (`IItemsQueries`:
  what their slot holds, creative mode; `IItemsOps`: `Consume`), `Power` (`IPowerQueries`: their
  satisfaction; `IPowerOps`: `Rest`).
- **State** (`ResearchState`, members internal): `Known` — the blueprints known, by id, sorted
  ordinally; `Analyses` — each analyzer at work, `Analysis(Analyzer, Progress)` by machine id in id
  order, its work in thousandths of a tick at full power. Saved; nothing is derived.
- **Files:** `Research.cs` — the module, its state, records and query interface; `Blueprints.cs` —
  what blueprints there are and their names.

## Rules

- A blueprint is named as the build menu names its card: a wall or a back wall by kind and material
  (`wallIron`, `backWallScrap`), anything else by the grid's name for it (`floor`, `furnace`)
  (`Blueprints.Of`). Cables are no blueprint.
- A map's `Layout.Known` is what its world knows at the start; a map without one knows every
  blueprint. The wreck knows `Content.Research.StartKnown`, through `Registry.Wreck`.
- Each tick, in the simulation phase, in machine id order, an analyzer whose slot holds something it
  can teach adds its satisfaction to its progress; at `Content.Research.Ticks × 1000` the world
  learns it:
  - a material (`Content.Research.Unlocks`): the blueprints it unlocks but those known; it stays in
    the slot;
  - a schematic: the first machine of `Content.Research.Schematics` not known; it is used up
    (`IItemsOps.Consume`).
- An empty slot, or one holding what teaches nothing new, drops the work done; so does taking it out.
  Such an analyzer rests (`IPowerOps.Rest`), drawing no power from the next tick, until it has work
  again.
- `building` refuses a plan for a blueprint not known with `building.unknown`, but in creative mode:
  it asks `IResearchQueries.Knows`.

## API

- **Queries** (`IResearchQueries`): `Knows(placement)` — whether its blueprint is known, every one in
  creative mode; `Known` — live, sorted; `Blueprints` — every blueprint with what it places
  (`Blueprint(Id, Placement)`); `At(id)` — what analyzer `id` holds, how far, out of how much work,
  and why it stands (`Analyzing(Item, Progress, Work, Problem)`, `Problem` `research.unpowered` or
  `research.nothingNew`); null for an empty slot.
- No commands, so no fuzz generator: a hero works an analyzer by moving items in and out
  (`Items.Move`).

## Invariants

- `Known` only grows, holds only blueprints there are, each once, sorted.

## Deliberately not handled

- Two analyzers may finish the same thing on one tick; the world learns it once.
- Taking apart what is built needs no knowledge of it: its refund is its cost.

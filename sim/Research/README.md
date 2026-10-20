# Research

Knowing blueprints and crafts ([0024](../../docs/decisions/0024-blueprints-are-learned-by-analysis.md),
[0027](../../docs/decisions/0027-what-is-analysed-opens-what-is-made-of-it.md)): what the world was
granted, the items analyzers have analysed, and what that opens. A blueprint not known cannot be
planned, a craft not known cannot be queued. Ported from `packages/sim/src/modules/research`; since
0027 a blueprint opens by the rule below, not by a table or schematics.

- **Depends on:** `Grid` (`IGridQueries.MachinesOf`: which machines are analyzers), `Items` (`IItemsQueries`:
  what their slot holds, creative mode), `Power` (`IPowerQueries`: their satisfaction; `IPowerOps`:
  `Rest`); `Knowledge` (`IKnowledgeQueries`, `IKnowledgeOps.Learn`: what the world was granted and has analysed,
  the world's on every surface, 0076).
- **State** (`ResearchState`, members internal): `Analyses`
  — each analyzer at work, `Analysis(Analyzer, Progress)` by machine id in id order, its work in
  thousandths of a tick at full power. Saved; nothing is derived. What is known is not stored: it is
  worked out from costs when asked, some 25 entries.
- **Files:** `Research.cs` — the module, its state, records and query interface; `Blueprints.cs` —
  what blueprints there are, their names and costs; `Analysable.cs` — which items need analysis.

## Rules

- A blueprint is named as the build menu names its card: a wall or a back wall by kind and material
  (`wallIron`, `backWallScrap`), anything else by the grid's name for it (`floor`, `furnace`)
  (`Blueprints.Of`); a machine built into the ship (`Fixed`: the lifepod) is no blueprint. Nor is a wall of bedrock or a back wall of
  natural ground (0077, `Blueprints.Has`); soil and stone walls are, known once soil or rock is analysed. A craft is named by the item it makes (`wire`, `Content.Crafts`). Cables are no
  blueprint and always known.
- **The opening rule:** a blueprint or a craft is known when it is granted, or when every item of its
  cost that needs analysis (below) is analysed. Nothing is held back: the solar panel opens as the rest do, once a frame, a solar
  cell, wire and a circuit are analysed (0041, 0057). In creative mode everything is known.
- What is granted and analysed is kept by the world's `Knowledge` (0076), one for every surface: an item analysed on
  a planet opens what it opens aboard. Research, one a surface, keeps what each of its analyzers is doing and reads the
  rest; `IResearchQueries` is answered as before, so building and crafting ask it alone.
- A map's `Layout.Known` is what its world is granted; a map without one is granted every blueprint
  and craft. The wreck is granted `Content.Research.StartKnown`, through `Registry.Wreck`.
- Each tick, in the simulation phase, in machine id order, an analyzer whose slot holds an item that
  needs analysis and is not yet analysed adds its satisfaction to its progress; at
  `Content.Research.Ticks × 1000` the item is analysed. It stays in the slot. Every item that needs
  analysis is analysed alike, whether it opens anything yet or not.
- **What needs analysis** (`Analysable`, 0027 amended twice): an item in the cost of a blueprint or a
  craft **not known from the start**, that is not in `Content.Research.StartKnown` (the cable, always
  known, counts as granted), worked out from content, never listed. Any other item is known from the start:
  the tools, the jetpack, the drone, the seed, biomass, biofuel, and the furnace's salvage, the scrap chunk and
  the hull plate, which go into nothing else; an analyzer holding one does no work and says
  `research.nothingToLearn`. Made items in such a cost (wire, a frame) stay analysable. The rule reads the
  wreck's grants, not the world's `Layout.Known`: a test map granting less opens what costs only items
  with nothing to learn (the furnace) from the start.
- An empty slot, or one holding what needs no analysis or is analysed already, drops the work done;
  so does taking it out. Such an analyzer rests (`IPowerOps.Rest`), drawing no power from the next
  tick, until it has work again.
- `building` refuses a plan for a blueprint not known with `building.unknown`, and `crafting` a craft
  not known with `crafting.unknown`, but in creative mode: they ask `IResearchQueries.Knows`.

## API

- **Queries** (`IResearchQueries`): `Knows(placement)` — whether its blueprint is known, every one in
  creative mode; `Knows(id)` — the same for a blueprint or a craft by its id, false for an id that is
  neither; `Analysed` — live, sorted; `Recipes()` — every blueprint, then every craft, each a
  `Recipe(Id, Placement, Cost, Granted, Unanalysed)` with `Known` worked out by the opening
  rule (creative mode aside), `Placement` null for a craft and `Unanalysed` the items of its cost that
  need analysis and are not analysed yet, in the cost's order; `At(id)` — what analyzer `id` holds, how far,
  out of how much work, and why it stands (`Analyzing(Item, Progress, Work, Problem)`, `Problem`
  `research.unpowered`, `research.nothingNew`, which means done: the item is analysed, or
  `research.nothingToLearn`, for an item that needs none); null for an empty slot.
  `Analysable.NeedsAnalysis(item)` — whether an item needs analysis; static, from content, so the client
  asks it for the "?" without a world.
- What an analysed item goes into, opened or not, is the client's filter of `Recipes()`.
- No commands, so no fuzz generator: a hero works an analyzer by moving items in and out
  (`Items.Move`).

## Invariants

- `Analysed` only grows, holds only items there are, each once, sorted; an analyzer adds only
  items that need analysis (a save from before 0027's amendment may hold others, harmlessly).

## Deliberately not handled

- Two analyzers may finish the same item on one tick; it is analysed once.
- Taking apart what is built needs no knowledge of it: its refund is its cost. Taking apart the
  wreck's machines refunds parts and opens blueprints early; the cost is the room's power or air.
- Items in a cost that open nothing yet are still analysed. An item in no cost of something not known
  from the start is never analysed, even a made one (the jetpack): it teaches nothing, so the analyzer
  says so and rests. Its analyzer panel lists nothing either, though the item may go into what is granted
  (the scrap chunk into the furnace).

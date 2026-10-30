# 0027 — What is analysed opens what is made of it

- Status: superseded by [trade-and-tech](2026-10-30-trade-and-tech.md); was accepted; supersedes the unlocks, the schematics and the "moment is shown" of
  [0024](0024-blueprints-are-learned-by-analysis.md); the `Locked` list superseded by
  [0041](0041-wreckage-of-many-kinds-and-circuits.md): the solar panel costs circuits and opens by the rule;
  "every item is analysed alike" amended 2026-10-18: only what goes into some cost, and again: only what goes
  into something not known from the start (below)
- Date: 2026-10-14
- Design: `docs/plans/crafting.md` (deleted once built)

## Context

Under 0024 a blueprint opened from a hand-written table (`UNLOCKS`) or from a schematic found in a
crate, the next machine of a fixed list. How fast the ship grew was luck, and nothing told the player
why a blueprint was closed. Every build was paid in raw materials, so there was nothing to make, carry
or later automate between the furnace and the hull.

## Decision

- **One rule, Grounded's:** a blueprint or a craft is known when it is granted, or when every item of
  its cost is analysed. No table and no schematics: the cost is the lesson. A `Locked` list in content
  holds what analysis cannot open yet (the solar panel, until electronics exist).
- **Parts are crafted by hand** — wire of scrap, plates and gears of iron, a rotor of a gear and wire —
  and most builds cost parts. Each part opens as its materials are analysed; each build as its parts
  are. Every item in some cost is analysed alike, raw or made, whether it opens anything yet or not
  (amended: see the end).
- **Research stores what is analysed,** sorted, and what was granted; what is known is worked out from
  costs when asked. A map grants `Layout.Known`, now naming crafts too; one that names nothing grants
  everything. The wreck grants the furnace, the analyzer, the crate, the scrap walls and the wire.
- **Crafting is a module of its own,** a queue per hero, Rust's way: the whole cost is taken when a
  craft is queued, units come one at a time into the inventory, a cancel gives back what is left, the
  unit at work included. It is not part of `items`: it asks `research` what is known, and research
  depends on items.
- **Nothing pops up over the hero.** With many blueprints opening at once a list of them would cover
  the screen. The analyzer's screen says it is done; its panel shows every blueprint and craft the
  item in it goes into, opened or still closed, and in a tooltip what each closed one waits for.

## Rejected

- **Keeping schematics beside the rule:** two ways to learn one thing, and the luck stays.
- **Opening by the materials a build is made of, recursively:** iron would open everything made of
  plates; a part then teaches nothing on its own.
- **Crafting inside `items`:** it needs `research`, which depends on `items`.
- **Paying per unit as it starts:** the same scrap could be counted twice by two queued crafts, and a
  refund would need a record of what was paid.

## Consequences

- Saves from before this change are refused (save format 2).
- Taking apart the wreck's machines refunds parts and opens blueprints early; the cost is the room's
  power or air.
- The burner generator of the design's second task is the first build that needs a rotor and opens by
  this rule alone.

## Amendment, 2026-10-18: only what goes into something is analysed

Tools, the jetpack, the battery and the like showed the violet "?" and went into the analyzer, which
then opened nothing. In Grounded the analyzer is for resources: what goes into recipes, to learn what
can be made of it. Finished things are not analysed.

- **An item needs analysis only when it is in some cost:** a blueprint's, a craft's or the cable's.
  Worked out from content (`Research.Analysable`), never listed: an item that later joins a cost, the
  engine's biofuel say, needs analysis from then on.
- **Every other item is known from the start:** no "?" on it, no band on its card. An analyzer holding
  one rests, draws no power and says why (`research.nothingToLearn`); its screen waits as an empty
  one's does.
- **Made items stay analysable** when they are in a cost (wire, plates, frames, circuits): analysing
  one still says what it goes into.
- Nothing saved changes: `Analysed` keeps what it holds, and an old save naming a tool there is
  harmless. A recipe's `Unanalysed` is unaffected, since what is in a cost always needs analysis.

Known from the start today: the fire axe, the harpoon, the drill, the jetpack, the drone, the seed,
biomass and biofuel, and the containers, wreckage, asteroids and derelicts, which are never carried.
The battery joined a cost with the drone (0062), and so waits for an analyzer again: the rule works it out.

## Second amendment, 2026-10-18: only what goes into something still to open

The first playtest caught a hull plate and a scrap chunk, saw the violet "?" on them, and spent two
minutes analysing them, though the only thing they go into, the furnace, is open from the start. The
analysis taught nothing.

- **An item needs analysis only when it is in the cost of something not known from the start:** a
  blueprint or a craft not in `Content.Research.StartKnown`; the cable, always known, counts as granted.
  Still worked out from content (`Research.Analysable`), never listed.
- **The opening rule reads only those items:** a blueprint or a craft is known when granted, or when every
  item of its cost that needs analysis is analysed; a recipe's `Unanalysed` lists only those. In the
  wreck's world nothing changes for what is not granted, since every item in its cost needs analysis.
  A world granted less than the wreck (a test map's `Layout.Known`) opens from the start what costs only
  items with nothing to learn: the rule follows the wreck's grants, not each world's.
- **Creative mode is unchanged:** no "?" at all.

Today this takes the scrap chunk and the hull plate out of analysis: they go into the furnace alone.
Iron, scrap and planks stay: iron opens the plate and the gear, the wreck's first lesson; scrap the floor,
the ladder and the lamp; planks the harpoon and soil. An old save's `Analysed` holding the chunk or the
plate is harmless.

# 0027 — What is analysed opens what is made of it

- Status: accepted; supersedes the unlocks, the schematics and the "moment is shown" of
  [0024](0024-blueprints-are-learned-by-analysis.md)
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
  are. Every item is analysed alike, raw or made, whether it opens anything yet or not.
- **Research stores what is analysed,** sorted, and what was granted; what is known is worked out from
  costs when asked. A map grants `Layout.Known`, now naming crafts too; one that names nothing grants
  everything. The wreck grants the furnace, the analyzer, the crate, the scrap walls and the wire.
- **Crafting is a module of its own,** a queue per hero, Rust's way: the whole cost is taken when a
  craft is queued, units come one at a time into the inventory, a cancel gives back what is left, the
  unit at work included. It is not part of `items`: it asks `research` what is known, and research
  depends on items.
- **Nothing pops up over the hero.** With many blueprints opening at once a list of them would cover
  the screen. The analyzer's screen says it is done; its panel shows what the item in it opened, what
  is analysed so far and what is one item short.

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

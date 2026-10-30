# A tech tree drawn as a graph, priced by a formula

- Status: accepted
- Date: 2026-10-30
- Supersedes [trade-and-tech](2026-10-30-trade-and-tech.md) on the tree's shape and on the price list; its stations, shared
  credits and prices that never fall stand

## Context

The tree was tiers of branches of chains: nothing showed why a thing came after another, the player clicked through tier
buttons, and forty-five prices and forty-six block prices were set by hand, each to be retuned whenever a recipe moved. The
wreck also knew most first parts, so the first purchases opened little.

## Decision

- **Prices are worked out** (`Content.Market`). Only what no recipe makes has a worth by hand. Anything else is worth its
  inputs and its time, `(inputs + seconds) x 1.25 / units`, rounded up, a recipe's first source naming it. A chunk is worth 20,
  not 4, so rounding at the bottom of a chain stays small. A recipe changed reprices everything made of it. A test still says
  every step pays by every source.
- **A block's price is worked out** (`Research.Prices`): what it opens is worth, a blueprint what it is built of, a craft
  `CraftTimes` units, times its category's multiple, up to two figures. `Times` on a node overrides it; the engine uses this to
  stay the dearest. The multiples are a first pass for the playtest to correct.
- **The tree is a graph** (`Content.Research.Categories`). A node lists the nodes it `Needs`, any number: `Reached` when all are
  done. A test says each line is true: a block's crafts and blueprints are made only of what stands behind it or wreckage gives.
- **Gates replace tiers.** A gate is a node asking for resources, for good, by any peer, part by part. Without them raw
  salvage alone would buy the whole tree; a gate makes the player actually run what came before it.
- **The wreck knows little:** both stations, scrap walls, the fire axe and what the pod makes. Both stations are of raw
  salvage, so the first sale needs nothing bought, a test says so, and taking one apart cannot end a world.
- **The window is one map** (`TechPanel`, `logic/TechMap.cs`): a band a category, a column by the longest way to the node, so
  lines always run left to right and a new node needs no coordinates. It is built once and restyled, so a read never
  rebuilds a control under the pointer.
- Saves: format 38. A deposit is named by its gate, not its tier. Older worlds no longer load.

## Deliberate omissions

- Lines drawn from recipes: wire would run to nearly everything. Needs are written by hand, one to three a node.
- Tabs per category: they would hide what leads across categories.
- Power and heat in a price: time stands for the effort; a machine's own cost is paid in its blueprint.
- A payback figure on a card: it says nothing a player can act on.

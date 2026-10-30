# The tech tree is rings round a hub

- Status: accepted
- Date: 2026-10-30
- Supersedes: the layered graph of [tech-graph](2026-10-30-tech-graph.md) and the click-to-buy of [tablet-tree](2026-10-30-tablet-tree.md)

## Context

A layered graph drawn on the tablet read as a diagonal smear: a player could not tell where to go, and block-to-block needs made
one line of the tree out of what are many ways. Nothing showed why a block mattered. A click bought at once, so looking cost credits.

## Decision

- **A tab is rings round a hub.** The hub in the middle asks for resources; filling it opens the next ring all round, as Satisfactory's
  HUB opens milestones. A block needs its ring's gate and nothing else, so a ring is bought in any order. The form is Path of Exile's: branches
  go out in all directions, far rings visible and dim.
- **Nothing opens before what it is made of.** A block may take what its own ring opens or what is further in, never further out:
  `NothingIsOpenedBeforeWhatItIsMadeOf` holds the data to it, so credits never buy a blueprint that cannot yet be made.
- **Two tabs, each its own hub.** Ship, for what space gives; Technologies, for what a planet gives, opened by the ship's last gate.
- **A click picks; a separate button buys.** What is picked lights up what it takes (teal), where it is made (pink) and what it goes
  into (sky), known or not, so a player can plan ahead; the strip below says the same and holds the button, with the price.
  These ties are worked out from the recipes, never written down (`TechLinks`). A gate picked lights the ring it opens.
- **A node is one thing.** Two pictures in a circle under one name read as one thing: a block opens one blueprint or craft, so a wall and its
  back wall are bought apart.
- **Words stay sharp.** The tree does not scale them with the map: nodes and text are set at the size they are seen, in an unhinted font.
  From far, under a zoom where words cannot be read, a node is a plain dot coloured by its state, under its branch's name.
- **The hub says what you carry against what is still needed**, so a glance tells what can go in now.
- **Credits have a coin.** Written with its picture everywhere, and always shown by the bag, not only at a station.

## Deliberate omissions

- Weapons and tools share one branch, Gear, until there are enough of each to part them. There is no cryo tab: cryo does not exist yet.
- No cost for looking and no hidden nodes: spoilers cost nothing here, and planning is the point.
- Ties are straight lines over the map, not routed: a starburst from a basic material is what it is, and the strip lists them.
- Rings hold a block's place by position only: there is no moving a block by hand; a new block needs a ring and a branch, not coordinates.

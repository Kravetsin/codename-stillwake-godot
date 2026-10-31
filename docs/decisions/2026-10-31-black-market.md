# The black market

- Status: accepted
- Date: 2026-10-31
- Amends [photo-panel](2026-10-29-photo-panel.md): how the solar panel is known

## Context

The solar panel should neither be known from the start nor open at a stage: it is meant to be rare and dear. The user wants a tab where
some things are simply sold for a lot of money, whenever a player can afford them.

## Decision

- **A third tab of the tree, "Black market"**, with one ring open from the start, no gate. Its listings are blocks: bought with `BuyBlock`,
  kept in `Knowledge.Granted`, priced as any block with a high multiple. It needs no module, command or panel of its own.
- **The solar panel's blueprint is its first listing**, at `Dear(5)` of what the panel is built of: about 12 000 credits, below only the last bag.
- **Its photosensitive panel stays unmade.** The derelicts that will hold them are still to come; until then the one in a world is the
  wreck's own, got by taking it apart.

## Deliberately not handled

- **Selling items:** a block opens a blueprint or a craft. Selling goods takes a purchase that gives items, and comes with the first goods
  to sell.
- **A warning on a blueprint nothing can yet be built from:** its card lists the panel in its cost, and a buyer can read that.
- **A market of its own look:** it is drawn as the tree's other tabs are, one node round the hub.

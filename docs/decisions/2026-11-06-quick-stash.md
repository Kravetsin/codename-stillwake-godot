# The bag stashed into nearby crates with one key

- Status: accepted
- Date: 2026-11-06

## Context

Playtest 2026-11-06, item 5: back aboard with a full bag, the player opens each crate and shift-clicks stack by stack.

## Decision

- **One key, `]`** (`Bind.Stash`, rebindable; R is the canister's), or the stash key under the bag's sort key. Every bag
  stack (own slots past the hotbar; never the hotbar, equipment or belt) goes into the crates that already hold its item,
  topping up their stacks of it, then their empty slots. What fits nowhere stays; nothing to stash does nothing, silently.
- **A radius through walls**, not the room or the whole ship: crates within `Inventory.StashReach` (12 cells, squared
  distance to the nearest cell of the footprint) of the hero's cell, nearest first, ties by id. The user's call: a store
  beside the airlock but behind a wall is reached, a store across the ship is not.
- **One command, `Stash(Cell, Keep)`**, never refused, planned when it applies: not a batch of moves planned on the
  client's view, which would leave stacks a crate filled meanwhile, the next crate had room for. `Cell` is the client's
  (0002); the plan is the same code for the world, the client's fold (0014) and `StashPlan`, which drives the flights.
- **Favourites** (`Keep`), per kind of item, this machine's for every world (`settings.cfg`): Alt held in the open inventory
  shows a star by the pointer, Alt+click marks or unmarks; a marked item's stacks carry the picked **outline** star in
  their top-left corner. The **fan** glyph (a box, three arrows fanning out) on the key. Both picked on the Artifact page.
- **Icons fly from the chest** to each crate, the inventory open or closed, landing with the pick-ups' glint.
- Free for now: a whole-ship stash or wider reach is a black-market wrist upgrade later.

## Not done

- Taking from crates; favourites in throwing, sorting, death or crafting; a mark per stack.
- A sound, a refusal or a tally chip. Others' flights.
- Flights show the client's plan: a crate filled by a teammate in the same moment takes less, and the bag corrects.

# crafting (client): the crafts beside the inventory

Making parts by hand ([0027](../../../docs/decisions/0027-what-is-analysed-opens-what-is-made-of-it.md)),
the client of `sim/Crafting`. It reads `Registry.Crafting` (`Queue`, `Progress`), `IResearchQueries`
(`Recipes`, `Knows`) and `IItemsQueries` (`Totals`, `Creative`), handed in every frame from the current
registry, and changes the sim only by sending `Craft` and `Cancel`.

## Files

| File             | Holds                                                                              | Godot |
| ---------------- | ---------------------------------------------------------------------------------- | ----- |
| `Crafts.cs`      | which crafts show, a unit's cost against what is carried, the unit's bar           | no    |
| `CraftsPanel.cs` | the panel: a card per craft, the queue under them                                  | yes   |

`Crafts.cs` is compiled again into `game.tests/` (`CraftsTests`).

## Guarantees

- Docked beside the open inventory, wherever `items` says it ends (`Ui.InventoryEdge`): shown with
  it, on Tab or E on a crate, and gone when it closes.
- A card per known craft, every one in creative mode: the item made and, per item a unit takes, what
  is carried against what is needed, red where it is short (never in creative mode). A click queues
  one, a shift-click five; the sim refuses the whole lot when it is unpaid.
- Under the cards, this player's queue in order: each entry's item and its units left, the one at work
  with a bar, full while its unit waits for room. A click on an entry cancels it.
- Read every 100 ms and at once after a click; cards and entries are rebuilt only when what they show
  changes, the bar every read.

## Deliberately not handled

- A craft sent shows in the queue once its tick has run, a round trip later, not at once as moves do
  (0014): queueing is not a hand's motion, and nothing in the world waits on it.

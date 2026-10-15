# crafting (client): the crafts window beside the inventory

Making parts by hand ([0027](../../../docs/decisions/0027-what-is-analysed-opens-what-is-made-of-it.md)),
the client of `sim/Crafting`. It reads `Registry.Crafting` (`Queue`, `Progress`), `IResearchQueries`
(`Knows`) and `IItemsQueries` (`Totals`, `Creative`), handed in every frame from the current registry,
and changes the sim only by sending `Craft` and `Cancel`. Where each craft sits is content:
`Content.Crafts.Tabs`.

## Files

| File             | Holds                                                                                        | Godot |
| ---------------- | -------------------------------------------------------------------------------------------- | ----- |
| `Crafts.cs`      | a craft's fixed place, how its slot looks, where a click on a short one goes, pins, the bar | no    |
| `CraftsPanel.cs` | the window: pinned row, tabs, the grid, the queue; pins kept in `user://crafts.cfg`          | yes   |

`Crafts.cs` is compiled again into `game.tests/` (`CraftsTests`).

## Guarantees

- Docked beside the open inventory, its top level with the inventory's, wherever `items` says it ends
  (`Ui.InventoryEdge`): shown with it, on Tab or E on a crate, and gone when it closes.
- Over its top edge, outside it, the tabs holding any craft, each an item's icon, the wheel over them
  switching tab. In it, from the top: the tab's name, how many of its crafts are opened and, with
  more than one, its page and arrows; the grid, 9 columns by 3 rows a page, a family per row, empty
  slots dim, the wheel over it turning pages; the pinned row, up to 9; the queue, 9 slots
  (`Content.Crafting.QueueMost`). Every slot is 36 px, the inventory's.
- Every craft has one place, a tab, a page, a row and a column, that never moves: one not known yet is a dark silhouette in it.
  One known but short of an item for a unit is dimmed and grey; in creative mode every one is known
  and ready.
- A click on a ready craft queues one, a shift-click five; the sim refuses the whole lot when it is
  unpaid. A click on a short one shows the tab and page of the first item it lacks that is itself crafted and
  pulses that slot amber twice; with only raw items lacking it does nothing. A silhouette does nothing.
- A right-click pins a known craft at the end of the pinned row, or unpins a pinned one; pinned crafts
  have a sky dot in the grid. A pin acts on a click as its craft does.
- The one tooltip (`Shared.PointerTip`), found by a hit test (`TipSpots`): a craft's name; per item a
  unit takes, its icon, what is needed and what is carried, green or red; for a raw item short, where
  it comes from (`crafts.from.<item>`); its seconds a unit; "click: go to what is missing" when a
  click goes somewhere. A silhouette says "???" and, in violet, that analysing what it is made of
  opens it. A tab says its name; a queued entry its item.
- The queue in order: each entry's item and its units left, the one at work filling its slot from the
  bottom up, behind its item and count, full while its unit waits for room. A click on an entry cancels it.
- Read every 100 ms and at once after a click; slots are rebuilt only when the tab, the pins, a
  craft's look or the queue change, the bar every read. Hover and the pulse are painted every frame.

## Deliberately not handled

- A craft sent shows in the queue once its tick has run, a round trip later, not at once as moves do
  (0014): queueing is not a hand's motion, and nothing in the world waits on it.
- The window is taller than the inventory beside it by its pinned row and queue: the two share their
  top, not their height.
- A shift-click on a craft ready for one unit but short of five is sent: the sim refuses it whole,
  as it would any unpaid lot, and nothing is jumped to.
- Pins are this machine's, not the world's: the same in every world played here.

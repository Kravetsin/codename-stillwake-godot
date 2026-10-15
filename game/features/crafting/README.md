# crafting (client): the lifepod and its Fabricator

Making parts by machine only ([0028](../../../docs/decisions/0028-the-lifepod-and-crafting-by-machine-only.md)),
the client of `sim/Crafting`. It reads `Registry.Crafting` (`Queue`, `Progress`, `Crafts`),
`IResearchQueries` (`Knows`), `IItemsQueries` (`Totals`, `Creative`, `Crate`, `AfterMoves`),
`IGridQueries` (`Machines`) and `ICrewQueries` (`Heroes`, `Spawn`), and changes the sim only by
sending `Craft`, `Cancel` and `Move`. Where each recipe sits is content: `Content.Crafts.Rows`. The
mockups it follows: "Fabricator panel" and "Lifepod", variant C.

## Files

| File                  | Holds                                                                                   | Godot |
| --------------------- | --------------------------------------------------------------------------------------- | ----- |
| `Crafts.cs`           | a recipe's fixed place, how its slot looks, where a click on a short one goes, the bar, what the pod is doing | no |
| `FabricatorPanel.cs`  | the panel beside the inventory: header and screen, recipes, the shared queue, outputs, status | yes |
| `FabricatorScreen.cs` | the panel's small screen: lines running while it works, READY idle                      | yes   |
| `PodView.cs`          | over the pod's picture: its screen, its tray, its glass glowing while a hero is made    | yes   |
| `PodDemo.cs`          | `-- --pod`: the wreck's pod opened and crafting, for recordings                        | yes   |

`Crafts.cs` is compiled again into `game.tests/` (`CraftsTests`). The pod's pictures are
`art/machine_lifepod.png`, laid by the grid feature like every machine's and normal-mapped with
them, and `art/lifepod_glass.png`, its glass lit from inside with a hero's shape; both were drawn from
the mockup's pixel recipe (`PODS[2].draw`) in the ship art's palette.

## Guarantees

- **The panel** shows while E has a machine that crafts open (`Ui.OpenCrate`, `Crafts(id)`) and the
  inventory is shown, docked beside it, its top level with the inventory's (`Ui.InventoryEdge`); gone
  when either closes. The items feature gives the pod no crate panel of its own.
- From the top: "Fabricator" and its screen; "Recipes", how many are opened; every recipe in its place
  from content, four a row, two rows at least, empty places dim. One not known yet is a dark
  silhouette; one known but short of an item for a unit is dimmed and grey; in creative mode every one
  is known and ready. A click on a ready one queues one, a shift-click five, refused whole by the sim
  when unpaid; a click on a short one pulses the first item it lacks that is itself crafted, amber,
  twice; a silhouette does nothing.
- "Queue · shared": `Content.Crafting.QueueMost` slots, each entry its item and units left, a mark in
  the top-left corner in its orderer's suit colour, the one at work filling its slot from the bottom,
  full while its unit waits for room. A click cancels it; its cost goes to whoever cancels.
- "Output": the pod's slots; a click takes a stack into the inventory as a whole (`Move` to the
  inventory), shown at once as the inventory's own moves are (0014).
- The status line: "Ready" in green, "Making X…", or "Output full: take what is made" in amber. The
  pod needs no power, so it never says so.
- The one tooltip (`Shared.PointerTip`), found by a hit test (`TipSpots`): a recipe's name; per item a
  unit takes, its icon, what is needed and what is carried, green or red; for a raw item short, where
  it comes from (`crafts.from.<item>`); its seconds a unit at the pod's speed; "click: go to what is
  missing" when a click goes somewhere. A silhouette says "???" and, in violet, that analysing what it
  is made of opens it. A queued entry: its item and units, whose it is and that a click cancels it;
  an output: its item and that a click takes it.
- **In the world**, over the pod's picture, unlit: its screen runs green while it works or waits full
  and shows a steady green READY light idle; a line in its tray while its outputs hold something.
  While a hero is passed out, being made, the glass glows from inside with a hero's shape, brighter as
  it comes round; when a hero is made (coming round, or coming aboard) a puff of air leaves the hatch
  and the glow fades over a second.
- Read every 100 ms and at once after a click; slots are rebuilt only when a recipe's look, the queue
  or the outputs change, the bar every read. Hover and the pulse are painted every frame.

## Deliberately not handled

- A craft sent shows in the queue once its tick has run, a round trip later, not at once as moves do
  (0014): queueing is not a hand's motion, and nothing in the world waits on it.
- A shift-click on a recipe ready for one unit but short of five is sent: the sim refuses it whole,
  as it would any unpaid lot, and nothing is pulsed.
- The pod's glow lights nothing around it: it is drawn, not a lamp in the light field.
- The hero climbing out is the glow fading behind the hero standing at the hatch, not an animation of
  its own.

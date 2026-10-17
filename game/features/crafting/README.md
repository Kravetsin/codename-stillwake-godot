# crafting (client): the lifepod's Fabricator, the Former, every queued station

Making parts by machine only ([0028](../../../docs/decisions/0028-the-lifepod-and-crafting-by-machine-only.md)),
the client of `sim/Crafting`. It reads `Registry.Crafting` (`Queue`, `Progress`, `StationOf`),
`IResearchQueries` (`Knows`), `IItemsQueries` (`Totals`, `Creative`, `Crate`, `AfterMoves`),
`IGridQueries` (`Machines`) and `ICrewQueries` (`Heroes`, `Spawn`), and changes the sim only by
sending `Craft`, `Cancel` and `Move`. Where each recipe sits is content: the station's `Rows` in
`Content.Stations`; the panel serves every queued station (0057), the pod's Fabricator first. The
mockups it follows: "Fabricator panel" and "Lifepod", variant C.

## Files

| File                  | Holds                                                                                   | Godot |
| --------------------- | --------------------------------------------------------------------------------------- | ----- |
| `Crafts.cs`           | a recipe's fixed place, how its slot looks, where a click on a short one goes, its block on the card, the bar, what a station is doing | no |
| `FabricatorPanel.cs`  | any queued station's panel beside the inventory: header, bolt and screen, recipes, the shared queue, outputs | yes |
| `FabricatorScreen.cs` | the panel's small screen: lines running while it works, READY idle, dark with no power | yes   |
| `StationView.cs`      | over a station's picture: its moving parts, the Former's flywheel, gears and press head, shown moving while it works | yes |
| `PodView.cs`          | over the pod's picture: its screen, its tray, its glass glowing while a hero is made    | yes   |
| `PodDemo.cs`          | `-- --pod`: the wreck's pod opened and crafting, the axe's and the rotor's cards shown, for recordings | yes |

`Crafts.cs` is compiled again into `game.tests/` (`CraftsTests`). The pod's pictures are
`art/machine_lifepod.png`, laid by the grid feature like every machine's and normal-mapped with
them, and `art/lifepod_glass.png`, its glass lit from inside with a hero's shape; both were drawn from
the mockup's pixel recipe (`PODS[2].draw`) in the ship art's palette.
The Former's are placeholders matching its recipe (0057), drawn by a script in the ship art's palette:
`art/machine_former.png`, two frame posts, a rotor flywheel on top, gears and a press head over a die at rest,
and `art/former_work_0..3.png`, the moving parts in four steps, each the machine's size and opaque over what
it covers. `-- --testbed --open=former` (or `lifepod`, `furnace`) opens a machine for a recording.

## Guarantees

- **The panel** shows while E has a queued station open (`Ui.OpenCrate`, `StationOf(id)`) and the inventory
  is shown, docked beside it, its top level with the inventory's (`Ui.InventoryEdge`); gone when either
  closes. The items feature gives a queued station no crate panel of its own. It lays out that station:
  its name (`station.<kind>`), its rows, its queue size, its speed in the card's seconds.
- From the top: the station's name, for one that draws power its bolt and what it draws (lit while it
  works, dark while it rests or has no power), and its screen (dark while a unit has no power);
  "Recipes", how many are opened; every recipe in its place from content, four a row, two rows at
  least, empty places dim. One not known yet is a dark silhouette; one known but short of an item for
  a unit is dimmed and grey; in creative mode every one is known and ready. A click on a ready one queues one, a shift-click five, refused whole by the sim
  when unpaid; a click on a short one pulses the first item it lacks that is itself crafted, amber,
  twice; a silhouette does nothing. A tool's recipe (the fire axe) is tinted amber, its edge
  amber-ish, amber under the pointer; a silhouette is not.
- No status line: the screen says whether it works, the queue's slot filling and full says the rest,
  and the outputs show what can be taken.
- "Queue · shared": the station's `QueueMost` slots, each entry its item and units left, a mark in
  the top-left corner in its orderer's suit colour, the one at work filling its slot from the bottom,
  full while its unit waits for room. A click cancels it; its cost goes to whoever cancels.
- "Output": the station's slots; a click takes a stack into the inventory as a whole (`Move` to the
  inventory), shown at once as the inventory's own moves are (0014).
- The one tooltip (`Shared.PointerTip`), found by a hit test (`TipSpots`): over a recipe, its item's
  card, as a stack's (`Shared.ItemCards`), with the craft block the station adds at its bottom
  (`Crafts.Block`): "Craft" and a unit's seconds at the station's speed; a row per item a unit takes, its
  icon, name, how many are carried in dim brackets and ×N, green when enough, red when short; for a raw
  item short, where it comes from (`crafts.from.<item>`); then the keys: LMB craft · Shift ×5 when a
  unit is carried, LMB go to what is missing when a click goes somewhere, none else. A silhouette says
  "???" and, in violet, that analysing what it is made of opens it. An output: its item's card with
  LMB take. A queued entry: its item and units, whose it is and that a click cancels it.
- **In the world**, over the pod's picture, unlit: its screen runs green while it works or waits full
  and shows a steady green READY light idle; a line in its tray while its outputs hold something.
  While a hero is passed out, being made, the glass glows from inside with a hero's shape, brighter as
  it comes round; when a hero is made (coming round, or coming aboard) a puff of air leaves the hatch
  and the glow fades over a second.
- **The Former** in the world: its flywheel turns, its gears with it, and its press head beats on the
  die while it works (`CraftingModule.Works`: a unit at work with power), four pictures over its own;
  idle, waiting full or without power it stands still. No sound of its own yet.
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
- A queued entry keeps its line tip rather than its item's card: it is an order, whose and how many,
  and a click on it cancels rather than uses the item.

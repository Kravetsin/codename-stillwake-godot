# crafting (client): the lifepod's Fabricator, the Former, the circuit assembler, every station at work

Making parts by machine only ([0028](../../../docs/decisions/0028-the-lifepod-and-crafting-by-machine-only.md)),
the client of `sim/Crafting`. It reads `Registry.Crafting` (`Queue`, `Progress`, `StationOf`),
`IResearchQueries` (`Knows`), `IItemsQueries` (`Totals`, `Creative`, `Crate`, `AfterMoves`),
`IGridQueries` (`Machines`) and `ICrewQueries` (`Heroes`, `Spawn`), and changes the sim only by
sending `Craft`, `Cancel`, `SetEndless` and `Move`; a queued station's input slots are the items feature's
(`InventoryPanel.Lend`, wired by `Main`), laid out here. Where each recipe sits is content: the station's `Rows` in
`Content.Stations`; the panel serves every queued station (0057), the pod's Fabricator first. The
mockups it follows: "Fabricator panel" and "Lifepod", variant C.

## Files

| File                  | Holds                                                                                   | Godot |
| --------------------- | --------------------------------------------------------------------------------------- | ----- |
| `Crafts.cs`           | a recipe's fixed place, how its slot looks, where a click on a short one goes, where a short item comes from, its block on the card, the bar, what a station is doing | no |
| `FabricatorPanel.cs`  | any queued station's panel under the inventory, in the machine frame: header, bolt and screen; recipes on the left, the shared queue and outputs on the right | yes |
| `FabricatorScreen.cs` | the panel's small screen: lines running while it works, READY idle, dark with no power | yes   |
| `StationView.cs`      | over a station's picture: the Former's flywheel, gears and press head moving, the smelter's coils glowing and lighting the room, the assembler's arm dipping and its screen lighting the room green, while it works; a cold-blue mark on a fed station too cold for its input | yes |
| `ArcCrackle.cs`       | a station with the arc module fitted, at work (0086): violet-white arcs crawling along its body's outline every 70–260 ms, now and then a burst or one leaping down a side, and a flickering violet-white light; nothing stopped | yes |
| `SmelterHeat.cs`      | an electric smelter at work in light (0075): its element's glow pulsing, a fine crackle along its top and bottom edges every 0.3–1 s (now and then two or three), a hot fleck drifting up every 0.6–1.6 s, a blue arc at its contacts every 1–2.5 s, a faint shimmer; nothing stopped | yes |
| `StationSounds.cs`    | a station heard at work where it stands (0059): the Former's strokes in step with its head and its motor, the smelter's hum and its melt's pops; told by `StationView` every frame | yes |
| `PodView.cs`          | over the pod's picture: its screen, its tray, its glass glowing while a hero is made    | yes   |
| `PodDemo.cs`          | `-- --pod`: the wreck's pod opened and crafting, the axe's and the rotor's cards shown, for recordings | yes |

`Crafts.cs` is compiled again into `game.tests/` (`CraftsTests`). The pod's pictures are
`art/machine_lifepod.png`, laid by the grid feature like every machine's and normal-mapped with
them, and `art/lifepod_glass.png`, its glass lit from inside with a hero's shape; both were drawn from
the mockup's pixel recipe (`PODS[2].draw`) in the ship art's palette.
The Former's are placeholders matching its recipe (0057), drawn by a script in the ship art's palette:
`art/machine_former.png`, two frame posts, a rotor flywheel on top, gears and a press head over a die at rest,
and `art/former_work_0..3.png`, the moving parts in four steps, each the machine's size and opaque over what
it covers. The electric smelter's too (0057): `art/machine_smelter.png`, a plated box on a frame, two copper coils
cold behind a grate, a small control board and two insulators, and `art/smelter_work_0..2.png`, only the coils'
pixels between the bars, glowing, the heat running up them a winding a picture. `-- --testbed --open=former` (or
`smelter`, `assembler`, `lifepod`, `furnace`) opens a machine for a recording. The circuit assembler's too (0057):
`art/machine_assembler.png`, a frame bench with two spools of wire under it, a cabinet over it with a rotor joint
and its arm parked, a glass magnifier over a green board, a dark screen; `art/assembler_work_0..3.png`, the
cabinet's inside with the arm dipping to the board and the rotor turned, lit, opaque over it; and
`art/assembler_screen_0..3.png`, only the screen's green trace and the head's spark, drawn unlit. The solar cell's
icon is `art/items/solarCell.png`. The grow capsule's (0060): `art/machine_growCapsule.png`, a glass dome on a
plated base, a soil bed inside, a grow lamp under its cap, dark; `art/growCapsule_work_0.png`, only the lamp's lens
lit pink, drawn unlit; and `art/growCapsule_plant_0..3.png`, the plant at each quarter of its crop, a sprout to a
budding bush, with the glass's glints kept over it. The farm's icons are `art/items/seed.png`, `soil.png`,
`biomass.png` and `biofuel.png`. The script that drew them is not kept: they are placeholders. So are what a planet's
caves give (0080), drawn as ASCII in the icons' outlined palette till the user picks art: `copperOre.png`, `copper.png`,
`ice.png`, `crystal.png`, `spore.png`, `mica.png`, `magnetite.png` and the Former's `motor.png`; the ruins' `alloyScrap.png` and `arcCoil.png` (0085).

## Guarantees

- **The panel** shows while E has a queued station open (`Ui.OpenCrate`, `StationOf(id)`) and the inventory
  is shown, docked under its grid, level with its left edge, where every machine opens (`Ui.InventoryFoot`, 0058); gone when either
  closes. The items feature gives a queued station no crate panel of its own. It lays out that station:
  its name (`station.<kind>`), its rows, its queue size, its speed in the card's seconds.
- In the machine frame (`Shared.MachineFrame`, 0058; the wrist computer's module under the bag, 0074), its header: the station's name, for one that draws power its bolt and what it draws (lit while it
  works, dark at 0 while it rests or has no power), and its screen (dark while a unit has no power). On the left
  "Recipes", how many are opened; every recipe in its place from content, four a row, two rows at
  least, empty places dim. One not known yet is a dark silhouette; one known but short of an item for
  a unit is dimmed and grey; in creative mode every one is known and ready. A click on a ready one queues one, a shift-click five, refused whole by the sim
  when unpaid; a click on a short one pulses the first item it lacks that this station makes itself (`Crafts.MissingCraft`), amber,
  twice; a silhouette does nothing. At a station with inputs (the Former, the assembler; not the pod) a right-click on
  a known recipe queues it **from the inputs** (shift ×5), whatever is carried. A tool's recipe (the fire axe) is tinted amber, a worn item's (the jetpack, the battery) green,
  each edged in its colour, full under the pointer, as their slots are everywhere (`Shared.SlotLooks`); a silhouette is not. Every slot is `Ui.Slot` wide, built again when the slot size changes.
- No status line: the screen says whether it works, the queue's slot filling and full says the rest,
  and the outputs show what can be taken: a click takes the stack into the inventory through the items feature, as a
  shift-click on its slot, flying there (`ShiftOut`, wired by `Main`; every shifted stack flies, 0075).
- On the right, inputs, queue, outputs (the logistics mockup). "Inputs", at a station that has them: its input
  slots, drawn and worked by the items feature (`InventoryPanel.Lend`) as any machine's, so a stack goes in and out
  by its clicks, the hand, drags and shift-clicks; laid out again only for another station or slot size. A
  shift-click from the inventory goes onto its own kind there, else the first empty input (`Clicks`). The header
  carries the logistics feature's drone badge (`FabricatorPanel.Served`, wired by `Main`, 0062): drones bring a head
  entry fed from the inputs what it needs.
- "Queue · shared": the station's `QueueMost` slots, each entry its item and units left, a mark in
  the top-left corner in its orderer's suit colour, the one at work filling its slot from the bottom,
  full while its unit waits for room. One fed from the inputs has a small blue inlet in its top-right corner, and
  endless, a blue ∞ in place of its count; a first entry waiting for its inputs (`CraftingModule.Lacks`) is dimmed,
  and the screen shows READY. A click cancels any entry, what was paid going to whoever cancels; a right-click on
  one fed from the inputs makes it repeat, or stop (`SetEndless`), so the click that cancels stays what it was.
- "Output": the station's slots; a click takes a stack into the inventory as a whole (`Move` to the
  inventory), shown at once as the inventory's own moves are (0014).
- The one tooltip (`Shared.PointerTip`), found by a hit test (`TipSpots`): over a recipe, its item's
  card, as a stack's (`Shared.ItemCards`), with the craft block the station adds at its bottom
  (`Crafts.Block`): "Craft" and a unit's seconds at the station's speed; a row per item a unit takes, its
  icon, name, how many are carried in dim brackets and ×N, green when enough, red when short; for an item
  short that this station does not make, where it comes from (`Crafts.From`): its own words when it is salvaged,
  smelted or made besides elsewhere (`crafts.from.<item>`), else the stations that make it ("made at: Fabricator,
  Former", wire at the assembler); then the keys: LMB craft when a unit is carried, LMB go to what is missing when
  a click goes somewhere; RMB craft from the inputs at a station with inputs; Shift ×5 with either craft. A silhouette says
  "???" and, in violet, that analysing what it is made of opens it. An output: its item's card with
  LMB take. A queued entry: its item and units (∞ endless); "from the inputs", "· repeats"; "waiting for: Iron ×2"
  in red while it waits; whose it is and that a click cancels it; for one fed from the inputs, that a right-click
  makes it repeat or stop.
- **In the world**, over the pod's picture, unlit: its screen runs green while it works or waits full
  and shows a steady green READY light idle; a line in its tray while its outputs hold something.
  While a hero is passed out, being made, the glass glows from inside with a hero's shape, brighter as
  it comes round; when a hero is made (coming round, or coming aboard) a puff of air leaves the hatch
  and the glow fades over a second.
- **The Former** in the world: its flywheel turns, its gears with it, and its press head beats on the
  die while it works (`CraftingModule.Works`: a unit at work with power), four pictures over its own;
  idle, waiting full or without power it stands still. Heard where it stands (0059): a press stroke each time its
  head comes down (the third picture), over its motor spinning up, dipping at each stroke and running down after it
  stops (`StationSounds`, the sound feature's README).
- **The electric smelter** in the world: while it works (`Works`: what it can smelt, with power) its
  coils glow behind the grate, three pictures drawn unlit, 160 ms each, and each coil lights the room round
  it in a hot orange (a `Glow`), a little brighter as the heat passes; idle, full or without power they
  stand dull copper and give no light. Heard where it stands (0059): its coils' hum, risen and fallen, and its melt
  popping at random moments while it works.
- **The circuit assembler** in the world: while it works (`Works`) its arm dips to the board under the glass
  pad by pad and its rotor joint turns, four pictures lit as the machine is, 140 ms each; over them, drawn unlit
  and in step, its screen runs a green trace and a spark shows where the head touches the board, and the screen
  lights the room round it green (a `Glow`), flickering a little. Idle, full or without power the arm stays
  parked, the screen dark, no light. Silent until the user picks its sound.
- **The grow capsule** in the world (0060): while it grows (`Works`) its lamp is lit pink, drawn unlit, and lights the
  room round it pink (a `Glow`); idle, full or without power the lamp is dark and gives no light. Its plant shows
  whatever of its process the input holds (`WorkOf`), at the quarter of its crop it has grown to (`Progress`), lit as
  the machine is, working or paused; an empty capsule, or one holding rock, shows none. Silent: its sound waits for
  the user's audition.
- **Too cold** (0057): a fed station whose input holds what needs more heat than it has
  (`ICraftingQueries.TooCold`) shows a five-pixel star of ice where it would work it — the furnace's
  mouth, the smelter's grate — drawn unlit, pulsing slowly between a dim and a cold blue, lighting a
  little round it. Its panel (the items feature) says the rest.
- **The arc module** (0086): a station with it fitted (`IItemsQueries.FittedTo`) crackles while it works (`Works`): crisp
  violet-white arcs (`Bolts` in `BoltTint.Violet`) crawl along its body's outline, its footprint below the top third,
  bowed out from it, every 70–260 ms, now and then a burst of two or three or a longer one leaping from a top corner
  down a side, and the room round it is lit violet-white, flickering (a `Glow` and a bloom; `ArcCrackle`). One look for
  every station, no art of its own. Idle, without power or without the arc: no arcs, no light. `-- --testbed --arc`
  plays it: the smelter opened, its empty socket and the module's card pointed at, the module shifted in, out and in
  again, flying each way, the smelter
  crackling, then its rock taken out and the smelter dark.
- A queued station's module socket sits at its inputs' right end, under the word "Module" (`LendSockets`, the items
  feature's), so the panel is no taller; its bolt says what it draws with its modules (`Draw`, set by `Main`).
- Read every 100 ms and at once after a click; slots are rebuilt only when a recipe's look, the queue
  or the outputs change, the bar every read. Hover and the pulse are painted every frame.

## Deliberately not handled

- The arcs are the same on every station: no picture of the module on the machine, no per-machine spot (cut from the
  design; a `ModuleLook` per machine waits for the user's picks). Silent: a crackle waits for the user's audition.

- A craft sent shows in the queue once its tick has run, a round trip later, not at once as moves do
  (0014): queueing is not a hand's motion, and nothing in the world waits on it.
- A shift-click on a recipe ready for one unit but short of five is sent: the sim refuses it whole,
  as it would any unpaid lot, and nothing is pulsed.
- The pod's glow lights nothing around it: it is drawn, not a lamp in the light field.
- A plant does not sway or grow smoothly between its four pictures: a quarter of an eight-minute crop is two minutes.
- The hero climbing out is the glow fading behind the hero standing at the hatch, not an animation of
  its own.
- A queued entry keeps its line tip rather than its item's card: it is an order, whose and how many,
  and a click on it cancels rather than uses the item.
- A first entry waiting for its inputs is only dimmed, its lack named in its tip: no ghosts of what it lacks in the
  inputs (cut from the logistics design).
- At the large slot size the Former's panel, now a row taller for its inputs, ends some 24 px above the window's
  foot at 1280×720: it fits, with little to spare.

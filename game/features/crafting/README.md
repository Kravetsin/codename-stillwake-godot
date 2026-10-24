# crafting (client): the lifepod's Fabricator, the Former, the circuit assembler, every station at work

## Files

| File | Purpose |
| --- | --- |
| `logic/Crafts.cs` | A recipe's fixed place and slot look, where a click on a short one goes, where a short item comes from, its block on the card, what a station is doing (`CraftsTests`) |
| `FabricatorPanel.cs` | Any queued station's panel under the inventory, in the machine frame: header, bolt and screen; recipes left, inputs, queue and outputs right |
| `FabricatorScreen.cs` | The panel's small screen: lines running while it works, READY idle, dark with no power |
| `BenchView.cs` | The arc bench (0099): the tool clamped in its vise with its modules, the shard alight, a module going on as a burst of arcs |
| `StationView.cs` | Over a station's picture while it works: the Former's moving parts, the smelter's coils, the assembler's arm and screen, the capsule's lamp and plant; the too-cold mark |
| `ArcCrackle.cs` | A station with the arc module fitted, at work (0086): violet-white arcs along its outline and a flickering light |
| `SmelterHeat.cs` | An electric smelter at work in light (0075): its element's glow, crackles, flecks and arcs |
| `StationSounds.cs` | A station heard at work where it stands (0059); told by `StationView` every frame |
| `PodView.cs` | Over the pod's picture: its screen, its tray, its glass glowing while a hero is made |
| `PodDemo.cs` | `-- --pod`: the wreck's pod opened and crafting, cards shown, for recordings |
| `logic/Crafting.En.cs` | Its player text (`En`) |
| `logic/Crafting.Sound.cs` | Its sound cues (`Sound`) |

Making parts by machine only (0028), the client of `sim/Crafting`. It reads `Registry.Crafting` (`Queue`, `Progress`,
`StationOf`), `IResearchQueries` (`Knows`), `IItemsQueries` (`Totals`, `Creative`, `Crate`, `AfterMoves`, `FittedTo`),
`IGridQueries` (`Machines`) and `ICrewQueries` (`Heroes`, `Spawn`), and sends only `Craft`, `Cancel`, `SetEndless`
and `Move`. A queued station's input slots and module socket are the items feature's (`InventoryPanel.Lend`,
`LendSockets`, wired by `Main`), laid out here. Where each recipe sits is content (`Content.Stations` `Rows`).
Mockups: "Fabricator panel" and "Lifepod", variant C. Pictures in `art/` (machines, work frames, item icons) are
placeholders drawn by a script not kept, or ASCII, till the user picks art. `-- --testbed --open=<former|smelter|
assembler|lifepod|furnace>` opens a machine for a recording; `--arc` plays the arc module going in and out.

## The panel

- **Shown** while E has a queued station open (`Ui.OpenCrate`, `StationOf`) and the inventory is shown, docked under
  its grid (`Ui.InventoryFoot`, 0058); it serves every queued station, the pod's first (0057). The items feature gives
  a queued station no crate panel of its own.
- **Header** in the machine frame (`Shared.MachineFrame`, 0058, 0074): the station's name, for one that draws power its
  bolt and draw with its modules (`Draw`), lit while it works and dark at 0 otherwise, and its screen, dark without
  power (0057). No status line: the screen, the queue and the outputs say it (no ADR).
- **Recipes** (no ADR), four a row, two rows at least, in their content places: one not known is a dark silhouette
  that does nothing, its tip "???" and that analysing opens it (0024); one short for a unit is dimmed grey; in creative
  mode all are known and ready. A click queues one, shift five, refused whole by the sim when unpaid; a click on a
  short one pulses amber, twice, the first lacking item this station makes (`Crafts.MissingCraft`). A tool's recipe
  is tinted amber, a worn item's green (`Shared.SlotLooks`). Slots are `Ui.Slot` wide, rebuilt on a size change.
- **From the inputs** (0057): at a station with inputs (not the pod, nor the arc bench) a right-click on a known recipe
  queues it from them (shift ×5). A shift-click from the inventory goes onto its own kind, else the first empty input.
  The header carries the drone badge (`FabricatorPanel.Served`, 0062).
- **Queue · shared** (0028, 0057): `QueueMost` slots, each its item, units left and its orderer's suit colour; the one
  at work fills from the bottom, full while its unit waits for room. Fed from the inputs: a blue inlet; endless: a blue
  ∞; a first entry waiting for inputs (`Lacks`) is dimmed and the screen shows READY. A click cancels any entry, what
  was paid going to whoever cancels; a right-click on one fed from the inputs toggles endless (`SetEndless`).
- **Output** (0014, 0075): a click takes a stack into the inventory (`Move`), shown at once, flying (`ShiftOut`).
- **The one tooltip** (`Shared.PointerTip`, hit test `TipSpots`): a recipe shows its item's card with the craft block
  (`Crafts.Block`): a unit's seconds at the station's speed, a row per item taken (carried, ×N, green or red), for a
  short item this station does not make where it comes from (`Crafts.From`: `crafts.from.<item>`, else "made at: …",
  0057), then the keys. An output: its card, LMB take. A queue entry: units, "from the inputs", "· repeats", what it
  waits for in red, whose it is, and its clicks.
- **Read** every 100 ms and at once after a click (no ADR); slots rebuilt only when a recipe's look, the queue or the
  outputs change; hover and the pulse painted every frame.

## In the world

- **The pod** (0028), unlit: its screen runs green while it works or waits full, a steady READY idle; a line in its
  tray while outputs hold something. While a hero is made its glass glows with a hero's shape, brighter as it comes
  round; when made, a puff leaves the hatch and the glow fades (no ADR).
- **While it works** (`CraftingModule.Works`: a unit at work with power), else still and dark (0057): the Former's
  flywheel, gears and press head over four pictures; the smelter's coils glowing unlit and lighting the room orange
  (`Glow`); the assembler's arm dipping, its screen's trace drawn unlit and lighting the room green. The grow capsule
  (0060): its lamp lit pink and lighting the room; its plant shows what of its process the input holds (`WorkOf`) at
  the quarter grown (`Progress`), none for an empty or rock-holding capsule.
- **Heard** (0059): the Former and the smelter, through `StationSounds` (the sound feature's README). The assembler
  and the grow capsule are silent till the user's audition.
- **Too cold** (0057): a fed station whose input needs more heat than it has (`TooCold`) shows a pulsing cold-blue
  star of ice where it would work it, unlit, lighting a little; its panel says the rest.
- **The arc module** (0086): a station with it fitted (`FittedTo`) crackles while it works: violet-white `Bolts`
  along its outline, now and then a burst or one leaping down a side, and a flickering violet-white light
  (`ArcCrackle`). One look for every station; no arcs and no light idle, unpowered or without it.

## Deliberately not handled

- The arcs are the same on every station: no module picture, no per-machine spot (a `ModuleLook` per machine waits for
  the user's picks). Silent: a crackle waits for its audition.
- A craft sent shows a round trip later, not at once as moves do (0014): queueing is not a hand's motion.
- A shift-click on a recipe ready for one but short of five is sent and refused whole; nothing pulses.
- The pod's glow lights nothing: it is drawn, not a lamp. The hero climbing out is the glow fading, no animation.
- A plant does not sway or grow between its four pictures.
- A queue entry keeps its line tip rather than its item's card: a click on it cancels.
- A first entry waiting for inputs is only dimmed: no ghosts of what it lacks in the inputs.

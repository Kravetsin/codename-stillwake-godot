# Design: Crafting, and recipes opened by analysed items

## Problem

Recipes open from hand-written tables and random schematics, so how fast you progress is luck, and
nothing tells the player why a machine is locked. Every build is paid in raw materials: there are no
parts to make, carry or later automate.

## Player experience

1. In the wreck the hero smelts scrap, opens the inventory (Tab) and clicks the wire craft: a bar
   fills for a second, a wire lands in the inventory.
2. The wire lays the cable that powers the analyzer in room B.
3. Scrap goes in the analyzer: its screen runs stripes, then shows a green check; floor, ladder and
   pipe appear in the build menu.
4. Opened, the analyzer lists what is analysed, what each opened, and recipes one item short: the
   door's thumbnail beside a gear's.
5. The hero crafts a plate and a gear, analyses both, and the door opens.

Goes wrong: ten wires queued with scrap for six. The job stops after six, its bar empties, nothing is
lost. A full inventory: the finished wire waits in the bar until a slot is free.

## Out of scope

- Workbench (faster basic crafts, complex ones), auto-assembler, crafting speed.
- Tools such as the screwdriver; faster building.
- Recipes that make more than one item; recipes needing a station.
- Any notice of learning besides the analyzer's screen and its panel: no popup.
- Loading saves from before this change.

## Scale

- World: one craft job per hero, a few analyzers, about 25 recipes.
- Players: each crafts alone, all in parallel.
- Frequency: a craft command every few seconds at most.

## Approach

Map-granted at start: furnace, analyzer, crate, scrap wall, scrap back wall, wire, solar panel. The
cable is always known. The solar panel is granted so that a player who takes the wreck's panel apart
can build it again from its refund: without it the analyzer would stay dark for good.

| Recipe                     | Cost                               | Opens with                 |
| -------------------------- | ---------------------------------- | -------------------------- |
| wire (craft, 20 ticks)     | 2 scrap                            | granted                    |
| plate (craft, 40)          | 2 iron                             | iron                       |
| gear (craft, 40)           | 1 iron                             | iron                       |
| circuit (craft, 100)       | 1 brokenElectronics, 2 wire        | brokenElectronics, wire    |
| cable                      | 1 wire                             | always                     |
| scrap / iron wall and back | 2 scrap / 3 iron                   | granted / iron             |
| floor, ladder, pipe        | 1 scrap                            | scrap                      |
| lamp                       | 2 wire, 1 scrap                    | wire, scrap                |
| door, vent                 | 1 plate, 1 gear                    | plate, gear                |
| valve                      | 1 scrap, 1 gear                    | scrap, gear                |
| tank                       | 4 plate, 1 gear                    | plate, gear                |
| solarPanel                 | 2 plate, 4 wire, 1 circuit         | granted                    |
| generator                  | 4 plate, 2 gear, 2 wire, 1 circuit | plate, gear, wire, circuit |
| analyzer                   | 2 plate, 4 wire, 1 circuit         | granted                    |
| crate / furnace            | unchanged                          | granted                    |

Supply pods give 1–2 `brokenElectronics` in place of schematics.

**research** stores `analysed` items (sorted) and `granted` recipes (`Layout.known`, now naming crafts
too; a map that sets none knows all). A recipe is known in creative mode, when granted, or when every
ingredient of its cost is analysed. Queries compute this; it is not derived state: 25 recipes, read
on validation and panel timers. Queries: `knows(recipe)`, `recipes()` (each with its cost, whether it
is known, and which ingredients are not analysed yet; it replaces `blueprints()`), `at(analyzer)`:
working, done or unpowered. "Almost open" and "what an item opened" are the client's filters of
`recipes()`, no queries of their own.

An analyzer works only while its item is not yet analysed; empty or done, it rests. Items stay in the
slot. `UNLOCKS`, `SCHEMATICS` and the schematic item go.

**crafting** is a new module. `items` cannot own it: it needs to know what is known, and `research`
already depends on `items`.

- State: one job per peer, `{ peer, recipe, left, progress }`.
- `craft { recipe, count }` is refused with `crafting.unknown`, `crafting.busy` or `items.notEnough`.
- `cancel {}` is refused with `crafting.idle`: the unit in progress finishes, no more start. Nothing
  is refunded, so nothing needs to remember what was paid.
- Each unit's cost is taken as that unit starts. The unit finishes after its ticks and goes into the
  inventory; with no room for it, it waits there, the bar full, until there is. Short for the next
  unit, the job ends.
- Runs in the simulation phase; moving does not stop it. Depends on `items` and `research` only.

Content: `CRAFTS`, keyed by the item made, `{ cost, ticks }`. `CABLE_COST` becomes `{ wire: 1 }`.
`paid` and refunds work as before: a refund is the current cost.

Client:

- A crafting feature docked beside the inventory: known crafts only (creative mode shows all). A
  click crafts 1; shift-click crafts as many as can be paid. The running job's bar; a click on it
  cancels.
- The analyzer's screen: stripes while working, a check when done, dark when unpowered.
- The analyzer panel: analysed items, what each opened, and recipes one analysed item short.
- `Learned` goes.
- Art: wire coil, plate, gear, circuit and broken electronics, each visibly made of its source.

## Determinism and co-op

Only `craft` and `cancel` enter the sim, per `ctx.from`. Progress is whole ticks per job and needs no
power. Jobs run in peer order. A peer that joins gets the jobs and `analysed` in the snapshot. Nothing
new is client-authoritative.

## Trade-offs accepted

- A peer leaves mid-craft → the job finishes into that peer's inventory → items already belong to the
  peer id.
- The inventory is full at the finish → the job waits with the item → no item dropped where the
  hero is, so no dependency on `crew`.
- A bulk craft started by mistake stops after its running unit and is never refunded → no `paid` to
  keep.
- Planks, canisters, chunks and hull plates open nothing yet → they are still analysed and get the
  check → one rule, no exceptions. Containers are never carried, so they never reach the slot.
- Taking apart a wreck machine refunds parts, a circuit included, early, and analysing them opens
  door, valve, tank before iron → the cost is the room's power or air → everything is made of
  something.
- The panel feeds 5 to a generator 4, a lamp 1 and an analyzer 2: the analyzer works at 714‰ in the
  first lesson → a first, gentle look at a short network.
- Two analyzers finish the same item on one tick → it is learned once.
- Saves from before this change are refused → the format bump.

## Tests

Scenarios:

- A wire is made in its ticks.
- A count stops when the scrap runs out.
- Each refusal: unknown, busy, notEnough, idle.
- Cancel lets the unit in progress finish and starts no more.
- A full inventory holds the finished item in the job until a slot is free.
- Creative mode crafts for free and knows every recipe.
- Analysed scrap makes the floor known but not the door; the door opens only after plate and gear.
- An analyzer rests empty and when its item is done; it stalls unpowered.
- The wreck: furnace → wire → cable → scrap analysed.

Invariants:

- `analysed` only grows; it holds real items, each once, sorted.
- A job's progress stays below its ticks.

Existing tests:

- The `UNLOCKS`, `SCHEMATICS` and loot scenarios become analysed-item scenarios.
- Cable costs change to wire.
- The `news` tests and the popup's e2e go.
- The e2e "analyse iron, the iron wall card appears" stays.

## Cost

- Modules: new `crafting` (sim and client). Changed: `research`, `building`, `power`, `debris` loot,
  `items` content, the analyzer art. ADR 0025 supersedes parts of 0024; the concept is updated.
- Content: `CRAFTS`; the wire, plate, gear, circuit and brokenElectronics items; new costs.
- Estimated diff: 1000–1300 lines with tests.

## Open questions

- Circuits from supply pods gate the panel, the generator and the analyzer: a pod drifting by is luck,
  though each always gives electronics. Keep, or build those from plates, gears and wire for now?
- Craft ticks, and 1–2 electronics a pod: are these first numbers fine?
- Should iron walls cost plates? Recommended no: there are many cells, and a craft step per cell is
  tedious.

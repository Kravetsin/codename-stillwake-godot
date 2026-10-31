# Plan: The scavenger's board

## Why

[board.md](board.md) replaces the tech rings with one endless board of notes on the tablet. A player learns by obtaining
things, and credits buy only on a black market whose rings open by what is sold. This plan lands that in steps, and every
merge leaves the game playable. Costs, recipes, worths, prices, the capsule and the scoop start do not change.

**Where this plan and board.md differ, the plan wins.** It checked the design against the code: the wreck knows nothing from its start; the layout is keyed by a world id, not
the seed; a market gate also needs its ring reached; ring 5 has a first order; slots also read what is granted; and a newly met order
lights the dot. File lines are at 22d3a36a and drift: find a thing by its name, and `TechPanel`'s lines live in `ResearchTablet` or
`RingsPage` after B-3.

**Words.** A *part* is an item in a recipe's `Cost`. A *count* is `Knowledge.Obtained`'s number for an item. A part is *enough* at
`Understanding.Enough(item)`. The *rule*: a thing is understood when each of its parts is enough and, for a craft, a station that
makes it stands. The *walk* is `Research` granting what the rule allows. A *thing* is an item, blueprint or craft. A *note* is a thing
obtained or known, drawn in ink. A *silhouette* is a pale shape of a recipe not yet known, and *pencil* is how far it has come. A
*thread* joins a note to a silhouette. A *page* is what a tablet tab shows. An *order* is what a market ring wants sold.

**Order and PRs.** Each task is its own branch and PR, merged before the next starts.

| Task | What lands | What a player sees |
| --- | --- | --- |
| B-1 | counts and sales recorded | nothing |
| B-2 | the rule grants | blocks of the tree turn green by themselves |
| B-3 | `TechPanel` split | nothing |
| B-4 | the board's logic, unwired | nothing |
| B-5 | the board, a tab beside the tree | a new first tab |
| B-6 | world id, dragging, the layout's file | notes move and stay moved |
| B-7 | the tablet's icon and dot | a key on the HUD |
| B-8 | the market's rings by sales | upgrades move to the market |
| B-9 | **the tree goes, format 42** | `ship` and `tech` gone, no deposits |
| B-10 | docs; this plan deleted | nothing |

- **Why the tabs go last.** From B-2 to B-9 the tree and the rule both work, so each merge only adds a way to learn. Recipes
  stay visible and knowledge stays reachable throughout. When the tabs go, the board and the rule have been on `main` for
  several merges, and `Reachable` has walked the rule since B-2.
- **Why B-8 comes before B-9.** An upgrade can stand on one tab only (`ById`, `Content/Research.cs:173`, throws on a repeat),
  and removing the tabs before the market had its rings would leave bags unbuyable.
- **Why B-9 is one PR.** `Deposit` has no gate to act on once the tabs go. Removing `Deposits` is what bumps the format. The
  `Reachable` leftovers, the tests, the text and the demo all name the gates.
- **The other developer.** Agree a window for B-3 (`game/features/research`), and for B-8 and B-9 (`sim/Content/Research.cs`,
  `ResearchTests`). Edits to `game/Main.cs` stay small.

**Beside this plan.**
- **[economy.md](economy.md)** is independent. `Enough` is worked from `Market.WorthOf`, so a recipe change moves it. B-1's pinned
  table and B-2's 540 s test fail if a change moves the pace. Items it adds later need no node: the walk finds them.
- **`feat/scoop-piles`** (read only through `scoop.md`) can land before or after any task.
  - A pile's refund passes `Building.cs:247`, so it counts with no code.
  - Its `Blueprints.cs` line (`Menu != null`) keeps piles out of the walk, and B-2's test `EveryWalkedBlueprintHasACard` fails
    if that line is lost.
  - Its stock's "8 plates" must be hull plates to meet the furnace. That is its author's call.
  - B-2 and B-9 edit `ContentTests.Production` and `WreckTests`, as that branch does: merge `main` often.
- **Flares** stay on `generator` (`MagneticFlares.cs:14`). B-2 pins the behaviour with a scenario, and the question goes to the
  ADR's list for the playtest.

## Tasks

### B-1 — Counts and sales (nothing a player sees)

- **Knowledge:**
  - `Obtained` (`Tally(Item, Count)`, appended in the order first obtained) and `Sold` (sorted by item, ordinally).
  - Ops `Obtain(item, count)` and `Sell(item, count)`, which ignore a count of 0 or less, as ops are passed zero entries (`ItemsTests.cs:517`).
  - Queries `Obtained` (read-only list), `GrantedIds` (read-only list), `ObtainedOf(item)`, `SoldOf(item)`, `Version`.
  - Derived, never saved: an item-to-index map, looked up and never iterated, rebuilt in `Rebuild()`; `Version`, bumped in
    `Obtain` and `Grant`.
- **Items:** `ItemsModule(grid, rooms, knowledge, knowledgeOps)` (`Registry.cs:74`). What counts:
  - `Output` (`Items.cs:450`).
  - `Smelt` (`:431`): what it puts into the outputs, its yields and then what it draws besides.
  - `GiveOrDrop` (`:473`): the whole cost, before it splits. Its inner `DropAt` names a real peer, so nothing counts twice.
  - `Spray` (`:501`): the whole loot.
  - `DropAt` (`:490`) only when the peer is `FloorStack.Nobody`.
  - Nothing else. Every caller in board.md's table was re-read at 22d3a36a and its line stands.
- **Research:** `sim/Research/Understanding.cs`, static and public like `Prices`: `Enough(item)` is
  `min(EnoughMost, (EnoughWorth + worth - 1) / worth)`, with `worth` from `Market.WorthOf`, and 1 for worth 0. `EnoughWorth = 200` and `EnoughMost = 10` go in
  `Content.Research`.
- **Trade:** `Launch` (`Trade.cs:73`) calls `Sell` for each priced stack, in its existing loop.
- **Save:** both lists are added with an initializer, so no bump (`Sim.cs:102-108`).
- **Tests:** `sim/Tests/CountsTests.cs`.
  - One scenario per source: catch, catch with a full bag, take apart, dig, fell, a kill, a wreck broken, what a dig or a fall
    drops for nobody (`DropAt`), a craft paid from the queue and one fed from inputs, a smelt with a byproduct.
  - One per non-source: craft cancelled, plan cancelled, pod set down, pipe, valve and cable refunds, the kit, move, drop,
    pick-up, sort, death, transfer, `Spawn`.
  - `Obtained`'s order; a count on a second surface reaching the shared list.
  - A capsule counts once by hand and once from an auto station; `Sold` stays sorted.
  - `Enough` pinned: `scrapChunk`, `scrap`, `wire`, `planks`, `iron` 10; `gear`, `tube` 8; `hullPlate`, `glass` 5; `plate` 4;
    `rotor` 3; `coil`, `canister`, `circuit` 2; `frame`, `solarCell`, `photoPanel` 1.
  - `GiveItems` writes the slice directly (`ItemsTesting.cs:11`) and never counts, so existing tests do not notice.
- **Invariants:**
  - `EverythingHeldHasACountOrIsKnownFromTheStart` (FsCheck) draws these acts itself, because `ItemsModule.Fuzz` switches on
    creative and spawns (`Items/Fuzz.cs:19-23`).
  - `ObtainedHoldsEachItemOnce`.
  - **Proven to bite:** delete the `Obtain` in `GiveOrDrop` once, watch `EverythingHeldHasACount…` fail, and say so in the PR.
- **Docs:** Knowledge, Trade and Items READMEs, and a row for `Understanding.cs` in Research's Files table (`DocsGuardTests.cs:77`).
  Items' README is at 8 189 of 8 192 bytes (`DocsGuardTests.cs:17`), so a line must leave it first.

**Done when** the tests above pass, a save lacking both lists loads (the pattern at `MigrationsTests.cs:62`), the full
`tools/test.ps1` passes, `dotnet format --verify-no-changes` is clean, and `DocsGuardTests` passes. No frames: nothing shows.

### B-2 — The rule grants, the tree still sells

- **Research:** gains `IGridQueries` (`Registry.cs:76`) and `System(Phase.Derivation, Walk)`.
- **The id set:** `Walked`, a static list of `Every` (`Research.cs:52`) in its order, with a blueprint's or craft's parts and a
  craft's station kinds from `Stations.All`.
- **What is outside the rule:** `Content.Research.Node(id) is { Tab: Content.Research.Market }` (a new constant). Only
  `solarPanel` matches today, and the test stays true after B-9. Upgrades are not in `Every`, so they are never walked.
- **The walk:**
  - It returns when `Knowledge.Version` and `grid.Version` have not moved (the idiom of `Flora.cs:198`). Seen versions are private
    fields, as `Flora.cs:246`, not in `ResearchState`, which is saved; they start at -1 in `Rebuild()`.
  - For each ungranted id: all parts `ObtainedOf >= Enough`, and for a craft `MachinesOf(kind).Count > 0` for some station kind.
    Then `Grant`.
  - One pass is enough, as the rule reads counts and the grid, never `Granted`. It allocates nothing.
- **Determinism:**
  - `Grant` inserts sorted (`Knowledge.cs:78`), so the saved list cannot show the order of grants.
  - Commands apply in stamped order, then surfaces by index, each running all its phases, modules in order (`Sim.cs:154`, `:171`), on
    every peer. So a count on surface 1 reaches surface 0's walk a tick later, the same everywhere.
  - A surface checks its own grid, and `Knowledge` is one object shared across surfaces (`Registry.cs:71`). A station on a
    planet grants there, on the same tick on every peer.
  - A loaded world walks on its first tick. The walk is idempotent, because every input of the rule moves a version, so a
    loaded world and a running one match.
- **No parts:** `screwdriver`, `helmet`, `suit` (`Crafts.cs:25-34`) need only their station. All are `StartKnown`. A test map with
  a pod and `Known = []` would learn them: fix such a test, not the rule.
- **Client:** no game code. The demo changes: `TreeDemo.Seed` gains `obtained`, 10 `scrapChunk` and 5 `hullPlate`, so the furnace
  shows done unbought.
- **Tests:**
  - `sim/Tests/UnderstandingTests.cs`: a blueprint at its counts; one short by one; a craft waiting for its station; a station
    on another surface; a station gone keeps the knowledge; a market block never granted by counts; save, load and same grants;
    flares arming once `generator` is known by the rule; `EveryWalkedBlueprintHasACard`.
  - Invariant `AfterAWalkNothingTheRuleAllowsIsUngranted`, proven to bite by skipping the last entry once.
  - `ModulesTests.cs:105` uses a map with `Known` null, where the walk has nothing to grant: add a case with `Known = StartKnown` and
    counts one short of enough, and a desync run with `joinAt` that asserts a grant fired.
- **`Reachable`** (`ContentTests.Production.cs:359-364`) walks the rule: market nodes as today, walked ids when their cost is had
  and a craft's station is built. All its users stay, unchanged, so the fixpoint is proven before the tree goes:
  `EveryCraftAndBlueprintOpens…`, `WithoutAStation…`, the solar-cell test, the farm test, and the pinned list at `:618`. New:
  `TheFormerAndBurnerAreUnderstoodWithin540SecondsOfTheBench` (the old gate test stays until B-9).
- **Budget:** the walk goes into `KeepsAThousandFurnacesAndTradeStationsAtWorkUnderBudget` (`BudgetTests.cs:329`).
  - Today's map has `Known` null, so nothing would be walked. Give it `Known = StartKnown`; the furnaces move `Version` each tick.
  - Time it both ways and hold the difference under 0.2 ms, a first cut. Write the measured figure in the PR.

**Done when**
- the tests above and the full suite pass; format and docs guard are clean;
- `tools/snap.ps1 tree.png -At 6 -- --tree` shows the furnace done with credits unspent for it, against the workbench the same run
  bought at 4.55 s (`TreeDemo.cs:57`) as reference;
- Research README says the rule; `Fuzz` is unchanged.

### B-3 — `TechPanel` split (nothing a player sees)

- **`ResearchTablet.cs`** (the shell, `TechPanel.cs` renamed) keeps the tablet, the strip, head, view, detail, `LayOut`,
  `Open`/`Close`/`Shown`, T, Tab, E and B (`:759-773`), the read timer, tips and the wheel.
- **`RingsPage.cs`** takes the rings' drawing: `TabView`, `NodeView`, `Place`, `SetSizes`, `OpenEdge`, `BuildTab`, `NodeOf`, `Click`,
  `Pick`, `Restyle`, `Tie`, `RestyleHub`, `DrawBack`, `ShowAction`, `Act`, `TipOf`. One instance per tab of `Content.Research.Tabs`.
- **`StripParts.cs`** holds what both pages will use: `Title`, `Fact`, `Flow`, `Named`, `Pictures`, `Picture`, `NameOf`, `Text`.
- **`ITabletPage`:** `Show(bool)`, `Step(delta)`, `Read(Registry)`, `Pointer(InputEvent)`; `Zoom(...)`, taking what the wheel passes
  today; `Fill()`, which fills the strip for what is picked.
- **Switching** stays as it is: one `Tablet.Tab` per page in the strip (`TechPanel.cs:119`), `Choose` (`:478`), `PaintTab` (`:498`).
- **Dev:** `Main.cs:256`, `TreeDemo` and `MachineScenarios` follow; `ScreenOf`, `ActionAt`, `HubAt`, `TabAt` forward to the page.
- **Docs:** client README's Files table, `docs/map.md:27`, `docs/glossary.md:60`.

**Done when** `TechRingsTests`, `TechCameraTests` and `TechTreeTextTests` pass untouched; a sheet of `tools/snap.ps1 tree.png
-Movie 11 -- --tree` matches by eye one recorded on `main` before the task, with exactly the same script lines and credits printed; the release
build passes; format and docs guard are clean.

### B-4 — The board's logic (nothing a player sees)

In `game/features/research/logic/`, with no Godot; floats are fine.

- **`BoardSlots.Of(found, recipes, granted)`**, where `found` is `Obtained`, `recipes` each thing's id and parts, and `granted` the
  known ids:
  - It replays `found` in order. The first note stands at the middle.
  - A thing takes its slot when its first part gets a note, or when it is itself obtained if that is sooner: the next free slot on an
    arc round that part, pushing outward. A raw thing takes one round the note before it.
  - The wreck knows nothing from its start (`StartKnown` is empty). What takes nothing, the pod's helmet, suit and screwdriver, is
    granted on the first tick and shows only once obtained: it has no slot, not even in the row.
  - Market blocks take none until bought. Then they stand, with what an old world knows with no part noted, in a row by the first
    note, by id. Only this row reads `granted`.
  - The radius, spacing and arc are client constants chosen by eye. The tests pin only the properties below.
- **`BoardThreads.Of(note, ...)`:** at most 3 (a constant), closest to done, ties in content order, never to a market block.
- **`BoardPencil.Of(parts, station)`:** the mean of `min(count, enough) / enough`, a standing station as one more part.
- **`BoardView`:** decides, for each thing with a slot, whether it shows as a note, shows as a silhouette (at a thread's end or linked to
  what is picked) or stays unseen.
- **Tests** (`BoardSlotsTests`, `BoardThreadsTests`, `BoardPencilTests`):
  - same input, same slots; no two slots closer than a note's size;
  - property: appending to `found` moves no thing already placed, leaving out the row;
  - five things understood at once keep the slots they held; nothing shown with an empty `found`;
  - a plate's 24 uses show 3 threads; the engine shows only by a click; the solar panel has no silhouette;
  - a craft with counts met and no bench is short of 1 only by its station.

**Done when** these pass under `tools/test.ps1 Board game.tests`; format and docs guard are clean; README's Files table lists the files.

### B-5 — The board, a tab beside the tree

- **`BoardPage.cs`**, an `ITabletPage`, is the first tab (`research.tab.board`), and the tablet opens on it.
- **Reads:** `m.Knowledge.Obtained`, `GrantedIds` and `m.Research.Recipes()` at 4 Hz, behind a key of the `Knowledge` instance, its
  `Version` and the hero's surface's `grid.Version`. A bench built moves no count, and a load or a resync brings a new instance at
  version 0 (precedent: `Grid.cs:70`), so the key starts empty. It takes no `send`, so it cannot issue a command.
- **Blank** until the first find.
- **Drawing:**
  - notes from `ItemIcons` and `BlueprintArt`; threads on one back canvas, redrawn on change;
  - pencil as a shader on the picture, copied from the `Silhouette` material (`FabricatorPanel.cs:61`);
  - `TechCamera` bounded by the notes' box plus a margin. When the box grows left or up, `Drag` by the difference so nothing jumps.
  - positions set only when the camera moves, unlike `TechPanel.cs:254`.
- **Strip:** takes, made at, goes into. Known parts show as named have/enough, unseen ones as pale pictures with no name. A craft
  with no station shows a pale bench. The buy button is hidden on this page.
- **`TechLinks`** becomes `Of(string id)`, over every recipe, item and node (`TechLinks.cs:24`). The solar panel shows under "goes
  into". `TechRingsTests.Link` follows.
- **Demo** (new-demo recipe): `[Dev("--board", …)]` in `MachineScenarios.cs`, `game/dev/BoardDemo.cs`.
  - Seeded through the save as `TreeDemo.Seed` is, one still each; nothing is obtained during the run:

    | Value | Seeded | Shows |
    | --- | --- | --- |
    | bare | nothing | a blank board |
    | `=first` | 3 `scrapChunk` | one note, threads to three pale shapes |
    | `=near` | 9 `scrapChunk`, 4 `hullPlate` | two notes, the same shapes darker, the plate's threads adding the furnace and workbench |
    | `=burst` | enough for wire, nail, plate, gear and tube, a workbench standing | five things inked at their slots |
    | `=circuit` | the five parts of a wreck's panel | five notes, a found circuit pale in what it is made of |
    | `=late` | everything in space | about 60 notes in view |

  - Script: T, click a note, click a silhouette, wheel out. Shared input helpers move to `game/dev/demos/TabletHands.cs`.
  - `TreeDemo`'s `TabAt` indices shift by one.
- **Text:** new keys; `TechTreeTextTests` names the tab.

**Done when**
- sheets from `tools/snap.ps1 b.png -Movie 6 -- --board=<value>` show what the table says, and the shapes darker on `=near` than on
  `=first`; the tablet's frame and strip matching `--tree`'s at 3.6 s; pencil reading like the bench's locked rows (`--bench`);
- `DevModesTests` and the release build pass; format and docs guard are clean.

### B-6 — World id, dragging, the layout's file

- **The seed cannot key a layout.** The ship is `new Surface(0, kind, 0)` (`Sim.cs:62`), `Sim.cs:303` writes only the other surfaces,
  and every new world is `World.Create(1, …)` (`Main.Sessions.cs:392`).
- **Smallest fix:** `World.Create(seed, layout, modules, uint id = 0)` and `World.Id`.
  - Written at the save's root when not 0 and read with `TryGetProperty`. Root keys are not under the unmapped check (`Sim.cs:108`), so
    no bump, and today's saves and hashes stay byte-alike.
  - `NewWorld` draws it on the client, a random non-zero number. The sim never reads it. It does not reuse the world index's id, as a
    guest has no index.
  - A guest has it from the Welcome save, so the protocol does not change. A resync keeps it. It reaches the hash through the shared
    `Write` (`Sim.cs:285`), alike on all peers, as they all load the host's save.
  - Id 0 (old saves, every dev world) keeps no file, so demos leave nothing behind.
- **Client:** a left press on a note drags it past `ClickSlack`; elsewhere it drags the camera. `logic/BoardFile.cs` holds what was moved,
  in `user://boards/<id>.json`, written on `Close`, in `ExitToTitle`, in `Quit` (`Main.cs:589`) and before an update's restart (`Main.cs:246`).
  Closing the tablet ends a drag. A failed write never stops the closing. A lost or bad file means notes return to their slots.
- **Tests:** `CoreTests` for id saved, loaded and hashed alike; `net/Tests` for a guest's id equalling the host's; `BoardFileTests` for
  the round trip, a lost file and a bad file.

**Done when** a `--board=first` run with a scripted drag shows the note moved, its threads following and the rest unmoved; a real new world
relaunched by hand keeps a moved note and a second new world does not; format and docs guard are clean.

### B-7 — The tablet's icon and dot

- **What is on screen:**
  - the hotbar at `(Margin, Margin)`, the gauge row under it at `Slots.GaugeRowOf`/`GaugeTop`, filling the wrist's column;
  - bottom left the overlay chip (`OverlayBar`), bottom right the version (`VersionCorner`), the build menu at the bottom;
  - the equipment at the right, from `InventoryTop` down.
- **Where it goes:** the key stands one seam right of the gauge row: x = `EnergyX + EnergyWidth + TabGap`, y = `GaugeTop`, side
  `WristLook.TabHeight`. The bag's sort key starts lower, at `InventoryTop`.
- **What it copies:** look and press from `SortButton` over `WristKey` (`game/shared/WristKey.cs`); placing and `SlotScale.Subscribe` from
  `EnergyBar.cs:28-34`; its layer as `InventoryPanel._over` (`:59`), with `ScreenLayers.Hud`.
- **`TabletKey.cs`:** glyph drawn in code, a wordless dot, a tip by the pointer. A press calls `Open`, as T and the menu's line do
  (`GameMenu.cs:37`, wired at `Main.cs:311`). `Hud()` (`Main.cs:222`) yields its rect for the markers.
- **The dot reads for itself on a 0.25 s timer behind `Knowledge.Version`,** because the panel returns early while away (`TechPanel.cs:201`).
- **`logic/BoardSeen.cs`** compares `Obtained` and `GrantedIds` with what this hero last saw, taken when the tablet closes and kept in
  `BoardFile`. Things new since then get a mark until the tablet closes. Client only.
  - With no file, what was seen starts as things stand at the first read, so a new world has no dot on a blank board and a joining guest
    has no marks.
  - A granted id with no note is ignored, so buying a bag lights nothing.
- **Tests:** `BoardSeenTests`: dot on a new item and on a new grant with a note, off for more of a known item, off after opening; marks
  until close; a bare world has no dot; a joined guest has no marks; buying a bag lights nothing.

**Done when**
- `--board=first` frames show the dot before T and none after, with the mark on the new notes;
- the key is compared by eye with the sort key in `--items` (`SortKeyAt`, `ItemsDemo.cs:148`), at `--slots large` too;
- frames with `--open=crate` show it clear of the crate's panel;
- a press on it does not reach the hand; format and docs guard are clean.

### B-8 — The market's rings, opened by sales

- **Content:**
  - `blackMarket` gets branches `[ForSale, Better]` and rings per board.md's table, gates `order1` to `order5`.
  - Every `Up(…)` leaves `ship` and `tech`, and `Better` leaves their branches.
  - Ring 5's items are not in the table, and an empty order means no gate (`Content/Research.cs:17`). First cut: today's `ground` order
    (20 copper, 10 mica, 5 hide).
- **Research:**
  - A market gate is done when `SoldOf` meets its order **and** it is `Reached`. Without that, a player selling 20 circuits first would
    open ring 4 with ring 1 shut, as a block needs only its own gate (`Content/Research.cs:186-190`).
  - `Deposit` at a market gate is `research.locked`.
  - The order sits in the field `Deposit` until B-9 renames it. The branch on `Tab` lasts one merge; B-9 removes it.
- **Client (`RingsPage`):** the hub and strip say sold over ordered, not carried. No button on a market gate. Text: `research.gate.order*`,
  `research.sold`. `TreeDemo.Seed` adds `sold` part way, and the script clicks the hub.
  - `Sell` moves no `Version`, so the page's change string (`TechPanel.cs:336-339`) gains what is sold of each market gate's order.
  - A ring opens as a side effect of a launch, about five times a game, so a newly met order lights the dot, and the market's tab in the
    strip carries one until it is chosen. The key's timer reads `Done` of the market gates for this, and `BoardSeen` keeps how many it saw.
- **Tests:**
  - a ring opens by sales alone, with no command; what was sold before it came up counts; its order met with the ring before short stays
    shut; a later ring may open at once;
  - `EveryOrderedItemHasAPrice`;
  - `UpgradesTests.cs:119-122` reads the market tab; `:128-135` (`TheBeltGrows…`) goes;
  - `ResearchTests.Fill` and the property skip market gates;
  - `BoardSeenTests`: a newly met order lights the dot, and an order already seen does not.
- **Save:** none: upgrades granted stay granted.

**Done when** a `--tree` sheet shows the market's hub at sold/ordered, its open rings lit as the `ship` tab's are in the same sheet, and
upgrades gone from the other tabs; the full suite passes; format and docs guard are clean; Research and client READMEs say it.

### B-9 — The tree goes (format 42)

- **Content:** `ship` and `tech` tabs removed. `TechNode.Deposit` and `TechRing.Deposit` become `Order`; `Gate => Order.Count > 0`. `CraftTimes` goes.
- **Research:** these go: `Deposit`, `Taken`, `IItemsOps`, `Deposited`, the refusals `research.gateOpen` and `research.nothingToDeposit`, the
  craft branch of `Prices.WorthOf`, B-8's branch on `Tab`. `Fuzz` draws `BuyBlock` alone, and its draws shift: re-tune what a seed pinned,
  never loosen.
- **Knowledge:** these go: `Deposits`, `Deposited`, `AddDeposit`, `DepositOf`.
- **Docs:** the READMEs of `sim/Research`, `sim/Knowledge`, `sim/Registry` (the step) and `game/features/research`.
- **Client:** tabs are the board and the market. `_carried` leaves the page; the deposit branch of `ShowAction` and `Act` goes. `menu.tree`
  reads "Tablet (T)". `TreeDemo` shrinks to the one tab.
- **Save:**
  - `SaveFormat = 42` (`Sim.cs:18`). The step removes `slices.knowledge.deposits` and nothing else, and reads no `Content`. It does not touch
    `surfaces`, as knowledge lives in the ship's slices (`Registry.cs:94`).
  - `Granted`, credits and every upgrade stay, so slots stay open. `Obtained` and `Sold` read as empty and nothing closes.
  - `Layout.Known` is read only at create (`Knowledge.cs:52`, never in `World.Load`), so a map with none still gets every id.
  - `Oldest` stays 39.
  - Write `format-42.json` with `STILLWAKE_WRITE_FIXTURE=1`. `MigrationsTests` then loads 39, 40, 41 and 42 through the steps (`:42`) and
    plays 600 ticks.
  - New test: format 41's upgrade has no `deposits`, the same `granted` and `credits`, and a bag upgrade's slot still open.
- **Tests naming what goes** (by grep):

| File | What happens |
| --- | --- |
| `sim/Tests/ResearchTests.cs` | rewritten: gates, `Reach`, `Fill`, deposits, the property, `TheWholeTree…`; kept: `KnowsWhatIsGranted…`, `TakesTheSolarPanelApart…`, `ListsEvery…`, `KnowsEvery…`, `RefusesAPlan…`, `KnowsAPlacement…` |
| `sim/Tests/ContentTests.Production.cs` | `:391` exactly-one-block, `:405` gate seconds, `:461` nothing-opened-early, `:516` first-blocks prices go; `:489` needs and `:730` stay |
| `sim/Tests/ContentTests.Tools.cs` | `:113-115` the harpoon's rings go |
| `sim/Tests/UpgradesTests.cs` | `:106-112` stays, on `bag1` |
| `sim/Tests/SurfacesTests.cs` | `:73-76` picks the first open block, which should still be there; edit only if it is not |
| `sim/Tests/WreckTests.cs` | `:240-245` sells a chunk, buys `bag1`; `:379` lamp's ring goes |
| `net/Tests/DesyncTests.cs` | `:600-608` blocks become `bag1`, `tradeSlots1` |
| `game.tests/TechRingsTests.cs` | the one tab; ties through `TechLinks.Of(id)` |
| `game.tests/TechTreeTextTests.cs` | `:16` loses the two refusals, `:20-23` asks of orders |

- A new `sim/Tests/FirstMinutesTests` plays board.md's first steps through commands: catch, smelt, furnace known, bench built, wire known.

**Done when**
- `grep -rnE "Deposit|DepositOf|CraftTimes|gateOpen|nothingToDeposit"` finds only `Migrations`, fixtures and `docs/`, READMEs having
  changed in this task;
- the full suite passes, the release build passes, format and docs guard are clean;
- frames: `--board=burst`, `--tree` on the market page with two tabs in the strip, and a real run of `--debris` showing a catch lighting the dot;
- a dev save of format 41 loads by hand, its bag still large.

### B-10 — Docs, then this plan is gone

- **ADR** `docs/decisions/<date>-the-board.md`: supersedes `2026-10-30-tech-rings.md` and amends these, each with a status line:
  - `2026-10-31-black-market.md`: rings by sales, its "one ring".
  - `2026-10-30-room-upgrades.md`: upgrades in the market's rings.
  - `2026-10-31-first-hour-pace.md`: gate numbers gone; the panel at 12 000 stands.
  - `2026-10-30-tablet-tree.md`: the tablet stands, the tree does not.
  - `2026-10-31-the-scoop.md`: its line 23 on the `machinery` ring.
- **The ADR holds** the rule, the counting table, slots, the world id, the omissions and risks from board.md, and its list for the playtest:
  flares on `generator`, 3 threads, ring numbers, whether a player finds the goal.
- **`docs/concept.md`:** lines 38 and 41-45 (room, the tree) and 53 (market "a tab of the tech tree").
- **READMEs** change in the task that changes their code, as CLAUDE.md asks; here only `game/dev/demos` if `--board` and `--tree` are not
  yet in it.
- **`docs/map.md:27`; `docs/glossary.md:26`, `:60-63`** (deposit gone; note, count, enough, order, sold added).
- **`docs/plans/scoop.md`:** step 4's reason and the lamp. `tools/adr-index.ps1` rewrites the index.
- **Delete** `docs/plans/board.md` and this plan.

**Done when** `DocsGuardTests` passes, `grep -rn "TechPanel\|board.md"` finds only ADR history, and format is clean.

## Not in this plan

Contraband goods, any tuning of orders beyond the first cut, moving the flares, a landing stagger, notes for upgrades, search or reset on the board.

## Deliberately not handled

- **Old worlds and dev worlds keep no layout file** (id 0): only our own test worlds are old.
- **A known thing in an old world's row may move** when its first part is noted. Only old worlds have one.
- **Creative and the testbed show a blank board:** `Spawn` does not count, and nothing is noted.
- **A content update can shift notes nobody moved,** as slots follow the recipes, and so bring them over moved ones. A player drags them apart.
- **`Migrations.cs:29` throws on a save with no surfaces** in the steps from 39 and 40. It affects only old test saves and is not touched here.

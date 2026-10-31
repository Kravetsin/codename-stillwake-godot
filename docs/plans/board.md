# The scavenger's board: learning by obtaining

Replaces the tech rings ([tech-rings](../decisions/2026-10-30-tech-rings.md)) with one endless board of notes on the tablet. Today a player learns by paying gates and credits; after this, by finding and making things. Credits buy only on the black market.

Written by `design-author`, cut down by `design-reviewer`. Design only, no plan yet. Paths are relative to the repo; each file:line was read at e383be01. Of the numbers, only the worths and thread counts come from a run over the current recipes; no play has been timed.

## What the player meets

Timings are arithmetic from `Content`: a piece every 3 to 5 s (`Debris.cs:18`), a workbench at a fifth of full speed (`Stations.cs:41`).

1. **First open.** T shows seven inked notes, the wreck's `StartKnown` (`Content/Research.cs:38`), and two pale shapes far off: the engine and the radar. There are no words.
2. **First catch.** A chunk goes into the bag. A note lands with a pencil stroke, and threads run off it to two pale shapes, the furnace and the workbench. A hull plate lands its own note, and the same shapes darken.
3. **Furnace.** At 10 chunks (a scrap wall gives one in 4 ticks) and 5 hull plates, the furnace inks in and its card joins the build menu.
4. **Workbench.** It waits on 10 planks: 2 or 3 crates, one crate in about 33 s of stream. Its 4-plank cost is not the wait. The planks are what sends a player out to catch once the scoop's piles land (6 planks in stock; `feat/scoop-piles`, not on main yet).
5. **First smelts.** A hull plate gives 2 iron and 1 scrap in 5 s; a chunk gives 4 scrap in 3 s. At 10 scrap, floor and ladder ink in.
6. **First burst.** The bench goes up with scrap and iron at 10: its last stroke opens wire, nail, plate, gear and tube at once. Five notes ink in at slots they already hold, so nothing lands in a heap (see Slots below).
7. **Bench work.** A plate, gear or tube takes 15 s, a wire 7.5 s.
   - 4 plates: iron wall and back wall.
   - 8 gears: door and hatch.
   - 10 wires: coil and lamp, then rotor.
   - 8 tubes: harpoon, nailer, vent, cistern and tank, if the rest is met.
   Queued first, the harpoon is about 195 s of bench, so about minute 7 or 8.
8. **A circuit early.** The wreck's panel (`Wreck.cs:18`) taken apart gives 2 frames, 6 solar cells, 4 wire, 1 circuit and 1 photosensitive panel (`Machines.cs:144`). Five notes land together, and circuit is 1 of 2. A cable rack's circuit is the second, whichever comes first. A found thing's picture is inked; what it is made of stays pale until it is understood. It opens nothing yet: wire is 4 of 10.
9. **Second circuit, with the rest met.** Smelter, charger, capacitor bank, auto trade station, drone bay and drill all open from one count.
10. **Former and burner.** Understanding both is about 520 s of one bench (plate 60, gear 120, tube 120, wire 105, rotor 67, coil 45). Today's press and machinery gates are 5 and 16 minutes of it (`first-hour-pace`).

**Going wrong:**
- **Craft counts met, no bench:** the shape is nearly done, its last stroke missing. A click shows a pale bench.
- **Bench taken apart later:** what it opened stays known.
- **Everything sold:** the count stands.
- **Full bag in space:** the rest floats off, and it still counted.
- **The wreck's panel taken apart:** it stays down until 12 000 credits buy its blueprint, as today (`first-hour-pace`).

## Decisions

**The rule, as data.** `Enough(item)` is `(EnoughWorth + worth - 1) / worth`, rounded up and capped at `EnoughMost`. Both tunables (200, 10) live in `Content.Research`. The worked table matches the user's numbers. The ones the user did not list: scrap 10, wire 10, planks 10, hull plate 5, gear 8, tube 8, glass 5, coil 2, canister 2, solar cell 1. A part worth 0, as `photoPanel` (`Market.cs:57`, and the wreck's panel gives one in minute one), needs 1: the division never sees a zero.

**A saved latch, not a live answer.** A craft stays known when its bench goes, so knowing must be saved. `Knowledge.Granted` is already that (`Knowledge.cs:12`). It keeps its readers unchanged: `Knows` (`Research.cs:84`), the upgrades (`Items.cs:117`), the build menu (`Tools.cs:207`), a station's rows (`FabricatorPanel.cs:273`), and both refusals (`Building/Rules.cs:123`, `Crafting.cs:166`). A machine's card and a station's row follow knowledge exactly as today.

**Where "obtained" is counted: `Items`, where every stack is born.** There is no single point today, but the call sites sort cleanly, and that sorting decides it.

| Op | Callers | Counts |
| --- | --- | --- |
| `Output`, `Smelt` | `Crafting.cs:269`, `:311` (hand and machine alike; a smelt's yields and what it draws besides) | yes |
| `GiveOrDrop` | catch `Debris.cs:289`, take apart `Building.cs:247`, dig `Digging.cs:140`, fell `Flora.cs:177` | yes, all four |
| `Spray` | a kill's loot, `Creatures.cs:219` | yes |
| `DropAt`, nobody's | wreck broken `Debris.cs:148`, `Digging.cs:141`, `Flora.cs:178` | yes: `DropAt` counts when the peer is `Nobody` |
| `DropAt`, a peer's | plan cancelled `Building.cs:204`, pod with nowhere to stand `Landing.cs:290` | no: paid before |
| `Give` | craft cancelled `Crafting.cs:202`, `Pipes.cs:339`, `:356`, `Power.cs:478`, the kit `Items.cs:580` | no |
| `Move`, `Drop`, `PickUp`, `Sort`, `LeaveBody`, `Take*`, `PutIn`, `LayDown`, `Unfit`, `Depart`/`Arrive` | hands, drones, deaths, surfaces | no: moves or spending |
| `Spawn` | creative | no |

- A machine holding anything cannot be taken apart (`Building/Rules.cs:74`), so a crate's contents never pass through a refund.
- `Items` gains `IKnowledgeOps` beside the `IKnowledgeQueries` it already holds (`Items.cs:134`). Inside the op it calls `Knowledge.Obtain(item, count)`.
- We do not have each source call `Knowledge`: that is seven modules with a new dependency, and the next source forgets it.
- We do not split `Give` into refund and gain; its callers already mean one thing.
- It counts when the stack is born, on the roll, as `Debris.cs:287` already records. Counting at pick-up would need a saved "fresh" flag on floor stacks, surviving merges, to tell loot from what was dropped.

**Who decides.** `Research` holds the rule and `Knowledge` the record, as with credits. `Research` gains a system in derivation and `IGridQueries` (a station stands: `MachinesOf`, `Grid.cs:114`). It walks the ungranted blueprints and crafts in `Every` order (`Research.cs:52`) when `Knowledge.Version` or `grid.Version` (`Grid.cs:64`) has moved, the idiom of `Flora.cs:198`, and grants what the rule allows. `Knowledge` is shared by every surface (`Registry.cs:71`), and each surface's `Research` checks its own grid. A station on a planet counts.

**Black market: a second page of the tablet, beside the board.** `tablet-tree` already settled this: an order placed by radio, naming no machine and working on any surface, while goods leave physically through a trade station. So a player on a planet with credits can still buy a bag slot.
- `BuyBlock` becomes `Buy(Id)` in `Research`. It keeps `research.bought`, `research.noCredits` and `research.locked` (an id that is no listing), which have text and tests.
- A listing is an `Upgrades.All` row at its own price, or `solarPanel` at 12 000, today's worked price written down in `Content.Research`.
- The page shows the next unbought of each kind. The sim does not enforce order; any order adds one slot.
- Contraband later is more rows, with a purchase that gives items (`black-market`). Nothing is built for it.

**Solar panel.** A listing is outside the rule: its counts never ink it. It shows no silhouette or threads on the board. Any note it uses lists it under "goes into". Once bought it gets a note. Building it still needs a `photoPanel`, as today (`black-market`).

**What cannot be had in space.** Copper, mica, magnetite, crystal, alloy, hide and cloth have no note until found. A silhouette appears only when one of its parts has a note, so planet walls do not clutter a ship's board. A motor stays part drawn for as long as a player stays in space; nothing says "locked". Click a silhouette and the strip shows its parts: known ones named with have/enough, unseen ones as pale pictures with no name.

**Threads.** At most 3 per note, to the shapes closest to done, ties in content order. A silhouette is seen only at the end of a thread or while a click shows its links, so a plate's 24 uses do not fill the board: on the current recipes, one known part would otherwise show about half the tree at once. Pencil progress is the mean of `min(count, enough) / enough` over the parts, with a standing station as one more part for a craft.

**Slots.** Worked out in plain C# in `logic/`, a pure function of `Obtained` and the recipes, so a late guest gets the same board.
- A thing takes its slot when its first part gets a note, or when it is itself obtained if that is sooner: the next free slot on an arc round that part, pushing outward. A raw thing with no recipe takes one round the note before it.
- What is known with no part noted, the seven of `StartKnown` above all, stands in a row at the start.
- Nothing moves on its own. A thing inks in where it stands, so a burst of five is five shapes darkening, not five things looking for room.

**Engine and radar at the far edge: kept.** The concept ends the stage on them (`concept.md:19`). Their parts are noted within minutes, but threads go to what is closest to done, so without this they would stay unseen for most of the stage. They cost two ids, `engine` and `radarAntenna`, in `Content.Research.Far` and no state, and they are the only wordless statement of the goal. They are silhouettes like any other, drawing in as counts rise, draggable. This is the one hand placement on the board, and it is listed as such.

**Readouts stay.** The strip and its colours (takes, made at, goes into) stay; the buy button goes. `TechLinks` keeps working from recipes, keyed by id over every recipe and item rather than `TechNode` (`TechLinks.cs:24`). `TechCamera` stays, its bounds the notes' box plus a margin.

## Saved, derived, determinism

- **Saved:** `Granted` as is, `Credits`, and one new list, `Obtained: (Item, Count)`, appended in the order first obtained. The list index is the order found. No separate notes list.
- **Derived:** an item-to-index map (lookups only), `Knowledge.Version`, bumped in `Obtain` and `Grant`, and each `Research`'s last seen versions, set so the first tick walks. The walk is idempotent, so a loaded world and a running one grant on the same tick.
- **Removed:** `Deposits`, `AddDeposit`, `DepositOf`. Removing a saved field bumps the format: 41 to 42 (`Sim.cs:18`), and a save with `deposits` would otherwise be refused (`Sim.cs:108`).
- **An existing world on load:** the step only removes `deposits`. It keeps everything `Granted` and its credits, and `Obtained` starts empty, so nothing closes and its counts begin again. Only our own test worlds have one (`first-hour-pace`), so neither gates nor blocks are refunded, and the step reads no mutable `Content`.
- **Determinism:** integers, rounded up as above. `Obtained` and `Every` are lists; the map is never iterated. Within a tick the order is the sim's own: commands in stamped order, then surfaces by index, then modules (`Sim.cs:154`, `:171`). All peers get the same list and hash, as the slice is saved. Only `Obtain` and `Grant` write it, inside the tick.
- **Inputs:** only existing commands, plus `Buy`. Drags are never commands.

## Co-op

- **Two heroes dragging:** each client keeps its own positions and sends nothing. Neither sees the other's arrangement; two people with one tablet each is the point. Knowledge is shared; layout is not.
- **Late guest:** gets `Obtained` and `Granted` in the Welcome save (`Protocol.cs:46`), and from them the same slots as anyone.
- **Persistence:** only the notes a player has moved, written when the tablet closes and on leaving, in `user://boards/<seed>.json` for host and guest alike. The seed is the surface's, already in the save (`Sim.cs:303`), so the protocol does not change. Unverified: that a world's seed differs from another's; if it does not, this needs one id on `WelcomeMessage`. The reviewer would have cut persistence for session-only positions. We keep it: arranging notes is what the user asked for, and it is not worth doing if a relaunch undoes it. If the file is lost, notes return to their slots.

## Budget

- **Sim:** a count is one map lookup and an add, a few a tick at most. A walk is at most about 150 ids times at most 7 parts, about 1000 lookups, at worst once a tick on a busy ship, and it gets shorter as things are granted. An estimate of tens of µs, not measured; it goes into the existing budget test.
- **Client:** the panel reads at 4 Hz behind a change check (`TechPanel.cs:30`, `:339`) and returns early when away (`:201`). About 80 notes and their silhouettes are about the same pieces as today's 94 blocks. Unlike today (`:254`), positions are set only when the camera moves or a note is dragged. Threads are one back canvas redrawn on change. Pencil is a shader on the note's existing picture (`BlueprintArt`, `ItemIcons`).

## What goes, stays, changes

- **Goes:** `Deposit`, the refusals `research.gateOpen` and `research.nothingToDeposit` and their text, `Prices.cs`, `CraftTimes`, `Tabs`, `TechNode`/`TechRing`/`TechTab`, `Done`, `Reached`, `Deposited`, `BuyProblem`, `TechRings`, `TechPanel` (replaced by a board panel), `TreeDemo`, `TechRingsTests`, `TechTreeTextTests`.
- **Stays:** `Knows`, `Recipes`, `Credits`, `Blueprints`, `Layout.Known` (a map with none knows everything, so most tests do not notice), `StartKnown` unchanged, `Upgrades`, `Market` worths and prices, the trade capsule, `TechCamera`.
- **Changes:**
  - `ResearchTests` (44 hits) is rewritten; `UpgradesTests`, `SurfacesTests.cs:73`, `WreckTests.cs:245`, `net/Tests/DesyncTests.cs:608` and `ContentTests.Tools.cs` move off `BuyBlock` and `Nodes`. `Research.Fuzz` draws `Buy`.
  - The `Reachable` walk (`ContentTests.Production.cs:339`) becomes the rule's fixpoint, with `WithoutAStation` and `SolarCellsAreReachable` kept. `EveryBlueprintAndCraftIsInExactlyOneBlock` and the nothing-opened-early test go, since the rule makes them true.
  - The gate-seconds test (`:405`) becomes "the former is understood within 540 s of the bench".

**Tests.**
- **Scenarios:** each source counts once; each non-source leaves counts alone; a craft waits for a station; a station gone keeps the knowledge; a listing is never granted by counts; each `Buy` refusal.
- **Invariants:** whatever lies in a slot or on the floor has a count or is known from the start (proven to bite by removing one source's count); `Obtained` holds each item once; after a walk, no rule-true non-listing is ungranted.
- **Client:** layout and pencil in `logic/`.

## Deliberately not handled

- **Build and take apart counts as taken apart.** We keep no record of what the crew built. Knowing a node means its parts are already at `Enough`, so the loop only helps through `StartKnown`: a trade station for chunks and hull plates, about 9 s a cycle, scrap walls for chunks. It saves about a minute and hurts nobody.
- **Loot that drifts off counts.** It was seen coming out, as the handbook's record already holds.
- **No landing animation:** no stagger for a burst, no replay when the tablet opens, no picture over the hero beyond what a catch already shows. It is the first thing to add if the playtest finds that new knowledge passes unnoticed.
- **Ring order on upgrades is gone.** `belt1` can be bought before any battery exists. Price is the only pace.
- **Heroes do not see each other's boards,** and no note is shared, pointed at or written on.
- **No search, no zoom to fit, no undo of a drag, no reset layout.**
- **Two worlds with one seed** share a layout. Harmless.
- **Notes for upgrades:** none. They are the market page's rows.

## Risks

- **Flares arm earlier and unchosen.** `ArmedBy = "generator"` (`MagneticFlares.cs:14`) was a purchase in the processing ring. Now it arrives with rotor 3, tube 8, wire 10 and any frame, which the wreck's panel gives in the first minute, so before minute 10 is possible. Flares need a cabled machine (`Flares.cs:53`), and repair is free (`free-repair`), so this is pressure, not a softlock. We keep the line and watch it; moving it is one constant.
- **The ship pays too well.** Four door segments give 12 plates and 8 gears in about 2 s of work (`Structures.cs:142`), skipping about 3 minutes of bench and opening the door blueprint itself; the player pays with a lock. It extends the scoop's listed risk; the same dial answers it.
- **Scoop text goes stale.** Its step 4 reason, 400 credits of blocks forcing catching, is gone. Its stock still works: 16 chunks and 8 plates meet the furnace if those plates are hull plates (the design does not say), and 6 planks leave the bench short.
- **Credits run out of uses:** about 74 000 in all (bags 39 300, belts 10 000, trade slots 12 800, panel 12 000) against more salvage all game. Contraband is the answer later; until then a late game has idle credits.
- **The first playtest's grind may just move:** the bench is 520 s to the former against today's 21 minutes, and real time is unmeasured.

## Cost

- **Modules changed:** Knowledge, Items, Research, Registry (migration). None new.
- **Content:** `EnoughWorth`, `EnoughMost`, `Far`, the panel's price; tree data removed.
- **Client:** research feature: the board and the market page.
- **Estimated diff** (the author's, before the cuts): sim +250/-350, client +700/-900, tests +300/-500.
- **Docs when accepted:** an ADR superseding tech-rings and amending black-market, room-upgrades and first-hour-pace; the concept's lines 38 to 45.

## Open questions

- Should flares stay on `generator`, or move to a later knowing such as `smelter`? Default: stay, decide at the playtest.
- Are 3 threads a note enough for a board that invites wandering? Default: 3, a client constant, judged by eye.
- Is the seed unique enough to key a layout by? To be checked in the plan.

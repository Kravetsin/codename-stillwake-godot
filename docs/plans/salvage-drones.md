# Salvage drones

A new drone kind, the salvage drone (`salvageDrone`), docks in an ordinary drone bay beside logistics drones. On one
canister of air from the bay's new fuel slot it flies out through doorways into vacuum. It takes debris pieces the net
would catch and loose stacks floating in space, and brings each load to the nearest crate with room. It keeps doing trips
until its air runs low, then flies home. Ordinary drones do not change, and they still never leave the air. The sim adds a
per-slot filter on `MachineSpec`, two ops (`IDebrisOps.Catch`, `IItemsOps.Lift`), five saved `Drone` fields and save
format 48. The client adds the bay's fifth slot, the redrawn bay (`wall`, «Хребет с кронштейнами»), the salvage drone
(`netter`, «Сетчатая рама») with its puffs and held piece, and a quieter jetpack loop. The rules go into an ADR written
with this work (see Docs); drones in general: [0062](../decisions/0062-drones-keep-machines-going.md).

## Player sequences

**A. First flight**
1. Sell the fourth order, then buy the salvage drone block in the market's order4 row → the assembler's drone row shows it.
2. Craft it at the assembler (nozzle 2, tube 2, plate 3, circuit 2) → its card says "No crate with room in reach: it does not fly".
3. Open a drone bay (E) → four drone slots, then the fuel slot with a faint canister in it.
4. Put the salvage drone in a drone slot → it sits folded on that slot's pad, eye lit.
5. Shift-click canisters → they go into the fuel slot; a canister lies in the tilted breech; the slot lamp turns amber.
   The salvage drone charges on its pad like any drone; its pad lamps show the charge.
6. A scrap chunk drifts into the bay's zone and a served crate inside has an empty slot → at the bay's next look the fuel
   count drops by one, the valve opens and the vane turns. The drone lifts on puffs and passes the shut door (it stays shut).
   Outside, its net is open and a quiet jetpack hiss plays while it is out.
7. It reaches the piece → the net closes (open, mid, shut) over the piece's own picture. It flies in to the crate, the
   crate holds the loot, and the net opens again.

**B. Several trips on one canister**
1. Several pieces and floating stacks are in the zone → after each delivery it goes straight to the next target, nearest first, pieces before stacks.
2. Its air gets low (the way home plus the spare) → the drone's own lamp turns red, it flies home and docks; leftover air is lost.
3. More targets come → the next launch takes another canister.

**Failure cases**
- **No canister:** the drone stays docked, the bay's slot lamp is red, nothing is spent.
- **No crate:** no served crate with an empty slot in the flood (full, switched off, or behind a wall) → it stays docked
  and nothing is spent. The item's card says it needs one ("No crate with room in reach: it does not fly"); the bay
  gives no live signal for this case (accepted, the user's call).
- **Crates fill up mid-trip:** the drone tries the nearest crate, then the next with room. If none has room, it lays the
  rest on the floor beside the last crate it tried, flies home and does not launch again until a crate has an empty slot.
- **The target crate is taken apart or switched off mid-trip:** it re-picks the nearest crate with room, else lays the
  load where it is and goes on.
- **A hero grabs the target, a net catches it or a harpoon reels it:** the hero or net wins on the same tick. The drone
  takes another target from where it is, or flies home. Nothing extra is spent.
- **A wall is built across the way:** if no way home is left, it drops at once. If only a longer way is left, it flies
  until its air runs out and then drops. Either way the drone and its load become floor stacks where it was, floating in
  space. They drift away like any stack; a hero on a jetpack or a second salvage drone may fetch them first.
- **Low air while carrying a load:** it turns home with the red lamp and lays the load at its bay.
- **On a planet:** a docked salvage drone never launches and no canister is spent (sim step 7).

## Sim

Terms, as Logistics uses them today:
- **flood**: the cells a drone can reach from its bay, by `Passable`;
- **served crate**: a storage machine switched on, inside the bay's zone (`ZoneBox`) and the flood;
- **look**: a bay's periodic check for work (`Look`), which launches at most one drone;
- **Leg.Source / Leg.Target / Home**: a drone's trip legs (to what it fetches, to where it delivers, back to its bay);
- **Next**: what a drone does after a delivery (today: the next job, or home);
- **Spare**: the margin of ticks the turn-home check keeps (`Content.Logistics`, the one charge uses);
- **Across(crate)**: the ticks to cross a crate's footprint, as the charge estimate counts it.

1. **Content** (`sim/Content`):
   - `Items.cs`: add `salvageDrone` beside `drone` (:185), with the same stack, group and `Capacity` as the drone. It has
     a battery like any drone: it charges on its pad and spends charge in flight; the canister's air only drives its nozzles.
   - `Crafts.cs`: add `new("salvageDrone", [("nozzle", 2), ("tube", 2), ("plate", 3), ("circuit", 2)], Ticks: 240)` beside `drone` (:61).
   - `Stations.cs:51`: add it to the assembler row `["battery", "drone", "salvageDrone", "deckDrive"]`.
   - `Market.cs:51`: add it to `Unsold`. Also add it to `ContentTests.Production.cs:433` `Unpriced`.
   - `Research.cs:78`: add the block `("salvageDrone", 4)` to order4, in that table's format. The number is a hand
     estimate from `Market`'s formula: worth about 3300 (the nozzles dominate), price about 14000, against the solar
     panel's about 13000. The content test holds the price within a fifth of the solar panel's; adjust the number until it passes.
   - `Logistics.cs`: add `TripAir = 600` (ticks of flight one canister gives: 30 s) and `SalvageDrone = "salvageDrone"`.
     Reuse `Content.Crew.Canister`.
   - Why: no item, craft or block exists for it.
2. **Per-slot filter:**
   - `MachineSpec` (`Machines.cs:64`) gets `(int Slot, string Item)? SlotTakes = null`: that slot holds only that item,
     and that item goes nowhere else. Why: `Takes` filters the whole machine, and `Charges` rejects anything without a `Capacity`.
   - The droneBay row (`Machines.cs:205`) becomes `Slots: 5, SlotTakes: (4, "canister")`.
   - `Stacks.Unfit` (`sim/Items/Stacks.cs:302`): the `SlotTakes` check goes first. A wrong item in that slot, or that
     item in another slot, is refused with `items.notTaken`. After it comes the `Charges` check (`items.notBattery`),
     which skips the fuel slot; the salvage drone passes it unchanged, since it has a `Capacity`.
   - `Receiving` (`Deliveries.cs:64`) gives the same answers. The canister goes only to its slot. Everything else goes to
     every slot but that one, so `PutIn` docks a `salvageDrone` and never docks a drone into the fuel slot.
   - `Power` and charging need no change: the bay charges a salvage drone on its pad as it charges a drone, and canisters (no `Capacity`) draw nothing.
3. **Save format 48:**
   - `World.SaveFormat` 47 → 48 (`sim/Core/Sim.cs:18`).
   - New step in `Migrations.Steps` (`sim/Registry/Migrations.cs`, after the 46→47 step ending at :94): in the ship's
     `slices` and on every surface, each `droneBay` crate's `slots` is padded with nulls to 5, as the charger's step does.
   - Write `sim/Tests/Fixtures/format-48.json` once with `STILLWAKE_WRITE_FIXTURE=1`. `format-47.json` already exists.
   - Why: the slot count changed what an old save means. The new `Drone` fields need no bump.
4. **Ops:**
   - `IDebrisOps.Catch(int piece) : IReadOnlyList<(string Item, int Count)>?` (`Debris.cs:66`). It removes the piece,
     rolls `LootOf(item, Random)` as the net does (`Nets.cs:78`), records it as found with `Record`, and reports no
     `CaughtFrom`. A gone piece returns null. Rename the private net `Catch(Piece)` to `CatchInNet`.
     Why: only `Grab` takes a piece, and it needs a hero.
   - `IItemsOps.Lift(int floorId) : Stack?` (`sim/Items/Items.cs:79`; body beside `ApplyPickUp` at :724) takes a floor
     stack whole, `AsStack()` with its charge and module; a gone stack returns null. Why: `PickUp` puts a stack into a peer's inventory.
5. **Logistics refactor, behaviour unchanged** (`Flight.cs`):
   - `Search` (:372) and the arrive check take a goal `Box` instead of a `Machine`; a machine passes its footprint box.
     Why: a piece or stack goal is a single cell that moves.
   - `Passable` (:242), `Flood`, `Search` and `StartOf` take a `bool vacuum` parameter, passed from the drone's kind
     (or `false` for ordinary work and for the public `Reached`/`Supplied` queries). Never a field: the queries run
     between ticks on one peer only, and a field would carry the last search's value into the next. In vacuum mode
     `Passable` is `!Structures.Solid(s) || Doorways.IsDoorway(s)`. Why: today's rule is "aired or doorway" only.
   - All `LogisticsTests` stay green.
6. **State and wiring:**
   - `Drone` (`State.cs`) gets, all written only when set, so ordinary drones save as before:
     - `int Air` (ticks left);
     - `int Piece` (the target piece's id, 0 none, kept while it carries the loot so the client can pick the piece's look; ids start at 1);
     - `int Floor` (the target floor stack's id, 0 none);
     - `string? Caught` (the piece's kind; an unknown kind after a content change shows the default look);
     - `Load` (`internal List<Stack>`, saved as `"load"`, null while empty, the `NetEntry.Stacks` pattern; public
       `IReadOnlyList<Stack> Loaded` marked `[JsonIgnore]`, as `NetEntry.Items` is, so it is never saved or hashed).
   - The crate it delivers to is kept in the existing `Target`.
   - Why a new `Air` field, not `Stack.Charge`: the charge is its battery, spent and checked as an ordinary drone's;
     air is a second, separate supply.
   - Claims are derived by a scan over `State.Drones`, as `Spoken` does. Nothing is stored, so nothing needs rebuilding.
   - `LogisticsModule` constructor adds `IDebrisQueries, IDebrisOps, IRoomsQueries`. Add `../Debris/Debris.csproj` to
     `Logistics.csproj`. `Registry.cs:89` becomes `new LogisticsModule(Grid, Atmosphere, Items, Items, Crafting, Debris, Debris, Rooms)`.
     Debris is registered before Logistics, so a same-tick grab or net catch wins.
7. **Launch** (new `sim/Logistics/Salvage/Salvage.cs`, a partial of `LogisticsModule`; Phase.Simulation inside today's `Look`):
   - When a bay's look comes, it tries an ordinary launch first, as today. If it launched none, it tries a salvage
     launch. Like an ordinary launch it needs the drone charged to `LaunchAt` and the trip affordable on its charge
     (`Affords`), not the bay's power: a bay without power still launches a charged drone, it just stops charging.
     - a docked `salvageDrone` charged to `LaunchAt` in the first slot holding one;
     - a canister in `SlotTakes`;
     - a vacuum flood from the bay;
     - a target (rules below);
     - a served crate in the flood with an empty slot (a cheap stand-in for "room for the load", which is unknown
       before the catch; what does not fit is handled by delivery);
     - `AffordsAir(TripAir, …)`: `TicksOver(bay, Manhattan bay→target + target→nearest crate + Across(crate) + crate→bay + Spare)`,
       and the same trip affordable on its charge.
     A target that fails either is skipped, and the next one is tried.
   - If all hold, it takes one canister with `TakeFrom` (spent), then the drone (`Air = TripAir`, `Leg.Source`, `Piece` or `Floor` set).
   - Targets, all inside `ZoneBox` and in the vacuum flood, not claimed by any drone:
     - a piece whose kind is in `Debris.NetCatches`, with `Reel == null` and `_rooms.RoomAt(middle) == Space`; its cell is `Debris.CellOf(middle)`;
     - a floor stack with `_rooms.Afloat(stack.Cell)`, `Body == null` (a hero's body is left), and older than
       `OwnPickupDelay` (`Items.cs:300`), so a stack a hero just put down or threw is not snatched at once; its cell is `FloorStack.Cell`.
   - Order: Manhattan from the drone's cell (`StartOf(bay, vacuum: true)` at launch, `d.Cell` in flight), then pieces
     before stacks, then the lowest id. Iterate `_debris.Pieces` and `_items.Floor` lists only.
   - Planets fall out of this: no debris zone spawns without `Surfaces.Kind.Debris`, and `Afloat` is false on every planet. With no targets there is no launch.
8. **Flight** (`Fly`, :97, branches on the drone's kind):
   - Each tick a salvage drone pays charge as an ordinary drone does, and air: it checks first, as the charge code does
     (`Air < 1` → `Drop`, :229), then pays one `Air` and moves.
   - Every place that lays `Cargo` today also lays `Load`: `GoalOf` (:164, goal gone), the `Arrive` Home case (:204) and `Drop` (:233).
   - At each cell centre:
     - Leg.Source: the goal is the target's cell. A target gone, reeled, out of space or out of the zone sends it to `Next`.
       Arriving, it calls `Catch` or `Lift`; a null result (a hero grabbed it in the commands phase) sends it to `Next`,
       and the canister is not refunded. `Load` gets the stacks and `Caught` the kind. The goal (`Target`) becomes the
       nearest served crate in the flood with room for any stack of the load, else home.
     - Leg.Target: it puts in what fits, walking `Load` in list order. The rest goes to the next nearest crate with room;
       with none, it is laid at `d.Cell`, beside the last crate it tried. Then `Next`. A target crate gone or switched off
       on the way: re-pick as above, else lay the load at `d.Cell`.
     - Salvage `Next`: the next target from `d.Cell` by step 7's order whose trip `AffordsAir` over the vacuum flood,
       else Home (`Low` if a target exists but is not affordable).
   - Turn home: low charge (today's rule) or `d.Air < TicksOver(bay, home + Spare)`, whichever comes first, gives Home `Low`.
   - Docking: as today; air is lost and any load is laid at the bay. A bay gone drops the drone (existing rule).
   - Why: today's `Fly` pays charge, moves only through air, and serves machines only.

## Client

9. **The bay panel's fifth slot** (`game/features/items`):
   - `InventoryPanel.Machine.cs:65` `BayBody`: the four drone slots, a gap, then the fuel slot.
   - `Reading.GhostOf` (:65): the `SlotTakes` slot gets a new `SlotMark.Breathes`, a canister ghost.
   - `Lights` (:81): the Docks ghost lights for `drone` or `salvageDrone`; Breathes for `canister`.
   - `Click.Shifted` (`logic/Click.cs:168`): a stack of the `SlotTakes` item goes to that slot.
   - Tooltip `logistics.fuel`. A salvage drone's slot shows its charge bar, as a drone's does.
10. **Card line:** `ItemCard.About` (`game/shared/logic/ItemCard.cs`) is set in `ItemCards.Card` (:31) when the table
    has `item.<id>.about`, and `ItemCardView` shows it under the kind on a plain card. Why: plain-item cards show no words today.
11. **i18n, En and Ru:**

    | Key | En | Ru |
    | --- | --- | --- |
    | `item.salvageDrone` | Salvage drone | Дрон-сборщик |
    | `item.salvageDrone.kind` | Logistics | Логистика |
    | `item.salvageDrone.about` | Flies out on a canister's air, takes drifting debris and floating stacks, brings them to crates. No crate with room in reach: it does not fly. | Вылетает на воздухе из баллона, забирает плавающие обломки и вещи, привозит их в ящики. Нет ящика со свободным местом в зоне — не полетит. |
    | `logistics.fuel` | canisters here: a salvage drone takes one a flight | сюда баллоны: дрон-сборщик берёт один на вылет |

    `tool.droneBay.about` (Building, En and Ru) gains a sentence on the fuel slot and the salvage drone.
12. **Logic** (`game/features/logistics/logic/`, Godot-free):
    - `Puffs.cs` mirrors the art page's `firing` and `puffsAt`: nozzle groups L, R, U, D by acceleration over `FIRE`,
      a hold-up puff in air, born every `PUFF_EVERY`, living `LIFE_IN` in air and `LIFE_OUT` in vacuum.
    - `Grip.cs`: open on Leg.Source; mid, then shut over 0.2 s when `Loaded` appears; open again when it empties.
    - Their numbers go in `game/Content/logic/SalvageLook.cs`: the puff timing, and a motion with no rotors (a small bob
      in air, tilt clamped at ±0.06).
13. **Sound** (`game/Content/logic/Sound.cs:224`):
    - New cue `SalvageThrust = new(["res://audio/crew/thrust-1.ogg"], -14, Loops: true, Heard: StationReach)` on World; the file is reused.
    - It plays where the drone is while it is out, with `ThrustAttackS` and `ThrustReleaseS` through the `Strikes` envelope.
    - −14 dB is a guess; it needs an ear check. A salvage drone gets no `DroneWhir` loop.

## Art (exported by the drawing code; `tools/anime-art/README.md`, "Add a subject")

14. **`subjects/salvage.js`:**
    - Port from the pick page's `salvage.js` (the session scratchpad today; copied into the repo in this step), whole:
      `make`, `bell`, `mesh`, `rust`, `lampWell`, the `netter` block, `eye`, `lamp`, `puffs`. Drop the other candidates.
    - Pictures: `salvage_fly_{open,mid,shut}` (21×30), `salvage_front` (the net's near side over a held piece),
      `salvage_dock` (14×14), `salvage_eye`, `salvage_dock_eye`, `salvage_lamp_{work,low}`, `salvage_puff_{0..2}`, `items/salvageDrone`.
    - It writes the generated `game/features/logistics/logic/SalvageInk.cs`, as net.js writes `NetInk.cs`: margin 2,
      anchor, eye, lamp, nozzles `(x, y, dx, dy, s, group)`, `HoldX`, `HoldBottom`, `DockFeet`.
15. **`subjects/bay.js`:**
    - Port `wall` from the pick page's `bays.js` (scratchpad today, copied in this step), whole: body, front, pad lamps, fuel parts (dial, vane, lever, slot lamp), beacon, the canister in the tilted breech.
    - Remove `droneBay` from `machines.js` (:259 and the table at :601) and `droneBay_pad`/`droneBay_charge_*` from `zfittings.js:189-191`.
    - Pictures:
      - `machine_droneBay`, `droneBay_front`;
      - `droneBay_lamps_{0..4,low,off}`;
      - `droneBay_fuel_*`: off, empty, a canister in (still, feeding a, feeding b), 5 in all; the art's dial stays on
        the body with its needle fixed (too small to read, the user's call);
      - `droneBay_beacon_{on,off}`, `droneBay_canister`.
    - It writes `logistics/logic/BayInk.cs`: `Pads` [(-1,1),(19,4),(-1,15),(19,18)], `Feet` 11.4, the lamp, fuel and beacon boxes, and the breech's point, angle and slide-in offset.
    - Add both subjects to the README's table. Run `node tools/anime-art/export.js salvage bay`, then `tools/snap.ps1` to import.
16. **`LogisticsView.cs`:**
    - `DroneLook.Pads`, `PadGlows`, `ChargeLamps` and `Beacon*` give way to `BayInk`.
    - `ReadBays` (:151) tracks either drone kind per slot.
    - `DrawDocks` (:448) draws a salvage drone `DockFeet − Feet` higher; a new `_fronts` node draws `droneBay_front` over docked drones.
    - `DrawGlowing` (:462) draws the pad lamps from `Charges.Share` and the fuel state:
      - can: the fuel slot holds a canister;
      - feeding: 1 s after its count drops, by `FLOW_STEP`;
      - off: no power.
    - `DrawFlying` (:502): a salvage drone draws its fly picture by `Grip`, then the held piece (`DebrisLooks.NameOf(Caught,
      Piece)`, or the stack's `ItemIcons.Icon`) at `Hold`, then `salvage_front`, then eye and lamp (red while `Low`).
      Its puffs come from `Puffs`, and it is heard through `SalvageThrust`.
    - `DroneLight` skips rotor discs and dust for it.
    - Pass `IAtmosphereQueries` from `SurfaceViews.cs:87`, for the air or vacuum puff life.
    - Add a demo `-- --salvage` with the `new-demo` skill (a bay by a door to space, a crate, pieces drifting) for the screenshots.

## Tests

Scenario tests (`sim/Tests/SalvageTests.cs`). The map is a sealed aired room with a bay, a crate and a door in the right
wall to space. The stream is quiet and pieces are drifted in, as `NetTests.Quiet`/`Drift` do.
- Slots, power and passage:
  - `ACanisterGoesOnlyInTheFuelSlotAndTheSalvageDroneOnlyInADroneSlot` (refusals `items.notTaken`)
  - `ASalvageDroneChargesOnItsPadAndCanistersDrawNoPower`
  - `AnUnchargedSalvageDroneDoesNotLaunchAndAChargedOneLaunchesFromAnUnpoweredBay`
  - `LowChargeTurnsItHomeAsLowAirDoes`
  - `ASalvageDroneFliesOutThroughAShutDoorTakesAScrapChunkAndPutsItInTheCrate` (the door stays shut)
  - `OrdinaryDronesStillNeverLeaveTheAirAndSalvageDronesServeNoMachine`
- What it takes:
  - `TakingACargoCrateRollsItsLootAtOnceAndRecordsItFound`
  - `AStackFloatingInSpaceIsFetchedAndOneOnTheShipsFloorIsLeft`
  - `AHerosBodyAndAStackJustThrownAreLeft`
  - `WreckageAsteroidsClodsAndReeledPiecesAreNeverTargets`
  - `APieceOverTheHullOrOutOfTheZoneIsNoTarget`
  - `TheNearestTargetGoesFirstPiecesBeforeStacksThenTheLowestId`
  - `TwoSalvageDronesNeverGoForOneTarget`
- Launch and refusals:
  - `NoCanisterNoLaunchNothingSpent`
  - `NoTargetNoLaunchNothingSpent`
  - `NoCrateWithAnEmptySlotNoLaunchNothingSpent` (full, switched off, out of the flood)
  - `ATargetNotReachableFromInsideLaunchesNothing`
  - `ATargetOneCanisterCannotCoverIsSkipped`
- Air:
  - `OneCanisterALaunchGivesTripAirTicks`
  - `OnOneCanisterItMakesTripAfterTripThenTurnsHomeLowWithTheWayHomeAndSpareLeft`
  - `LeftoverAirIsLostOnDocking`
- Delivery:
  - `WhatDoesNotFitGoesToTheNextCrateAndTheRestLiesBesideTheLastTried`
  - `CratesFullTheLootLiesBesideTheLastCrateTriedAndTheDroneFliesHome`
  - `ATargetCrateTakenApartMidTripSendsTheLoadToAnother`
  - `ALowDroneWithALoadLaysItAtItsBay`
- Races:
  - `AHerosGrabOnTheArrivingTickWinsAndTheCanisterIsNotRefunded`
  - `ANetCatchingTheTargetWins`
  - `AReelOnTheTargetLetsTheDroneGo`
- Walls, drops, planets, saves:
  - `AWallCutAcrossTheWayHomeDropsItWithItsLoadAsFloatingStacks`
  - `ADroppedSalvageDroneIsFetchedByAnother`
  - `OnAPlanetASalvageDroneNeverLaunches` (`PlanetTesting.First(seed)`)
  - `ASalvageDroneMidTripWithItsLoadSavesLoadsAndHashesTheSame`
  - `AskingReachedAndSuppliedOnOnePeerBetweenTicksKeepsTheHashes` (two worlds, the queries on one only)

Content and migration tests:
- `ContentTests.Production`: `TheSalvageDroneIsAnAssemblerCraftOnTheMarketsFourthRowWithinAFifthOfTheSolarPanelsPrice`
- `ContentTests.Machines`: `TheBayHasFourDroneSlotsAndAFuelSlotTakingOnlyCanisters`
- `MigrationsTests`: `GivesADroneBayItsFuelSlotEmpty` (a bay on the ship and one on a surface)
- `DebrisTests`: `CatchTakesThePieceRollsItsLootAndRecordsItFound`
- Beside `PickUp`'s tests (file guessed): `LiftTakesAFloorStackWholeAndAGoneOneIsNull`

Invariants (`SalvageInvariantTests`, FsCheck, random walls, doors, pieces, stacks, crate fullness and canisters):
- `NoTwoDronesEverShareATarget`
- `EveryCanisterSpentLaunchedOneSalvageDrone`
- `NoSalvageDroneIsInASolidCellAndAirNeverGoesNegative`
- `ItemsAreKeptWhenOnlyStacksAreTaken`
- `SavingAndLoadingAtARandomTickMidTripPlaysOnAlike` (the generic suites never fly a salvage drone)
- Prove one bites: drop the claim check once and watch it fail.

Budget (`BudgetTests`): `KeepsFourBaysOfTwelveDronesSalvagingAmongFloatingStacksUnderBudget`. The hall with a door to
space has four bays of one drone and two salvage drones each, a full stream and 40 floating stacks. The tick mean stays
under the budget through the suite's `Within(4)` helper, as the other budget tests do.

Client tests (`game.tests`):
- `SlotMarksTests` (update :82: slot 4 is Breathes)
- `ACanisterShiftClickedIntoAnOpenBayGoesToItsFuelSlot`
- `ItemCardTests.TheSalvageDronesCardSaysNoCrateNoFlight`
- `PuffsTests`, `GripTests`
- `BayInkTests` (pads = drone slots; the fuel box matches `SlotTakes`)
- `DroneSoundTests.ASalvageDroneOutHissesQuieterThanTheJetpack`
- `EnTests` and `RuFilesTests` cover the new keys.

## Docs

- `sim/Logistics/README.md` is 8184 B against the 8192 cap, so the rules go to a new `sim/Logistics/Salvage/README.md`
  (Files: `Salvage.cs`; Rules; Invariants; Not handled). The parent README gets one `Files` row and Depends on `Debris`
  (`Catch`) and `Rooms` (`RoomAt`, `Afloat`). Make room by shortening its `## API` Queries line to bare names.
- `sim/Debris/README.md` (8169 B) and `sim/Items/README.md` (8182 B): one line each, for `Catch` (in Found: "every
  catch: grab, break, net, salvage drone") and for `Lift` and `SlotTakes`. Trim the same bytes; `DocsGuardTests` decides.
- `game/features/logistics/README.md` (7239 B): Files rows for `SalvageInk.cs`, `BayInk.cs`, `Puffs.cs`, `Grip.cs`, and Guarantees for the bay's fuel parts and the salvage drone in flight.
- ADR `docs/decisions/2026-11-03-salvage-drones.md`: the rules above, the art and sound picks, the resolved open
  questions. Then run `tools/adr-index.ps1`.
- `docs/map.md:26` adds `Salvage/Salvage.cs` and `SalvageInk.cs`.
- `docs/glossary.md`: "дрон-сборщик / salvage drone → item `salvageDrone`, `Drone.Air`, `Salvage.cs` → Logistics".
  Extend the canister line (a salvage drone's air) and the drone bay line (fuel slot, `SlotTakes`).

## Deliberately not handled

- We do not top up the fuel slot by logistics drones, because that is later work and canisters come by hand.
- We do not keep leftover air on docking, because a field that outlives the flight costs more than it gives.
- We do not deliver a load to a crate when the drone turns home low; it lays it at the bay, as an ordinary drone does its cargo.
- We do not charge air at a different rate inside the ship, because one flat cost a tick is enough.
- We do not take wreckage, asteroids or clods, and we do not empty nets, because those are other tools' work.
- We do not use path length for the target or crate order, only Manhattan, as ordinary drones do; the turn home covers detours.
- We do not show a popup for a drone's catch; it is recorded as found only.
- We do not raise an alert for an empty fuel slot or full crates; the slot lamp and the floor stacks show it.
- We do not let pieces, meteors or heroes collide with drones; nothing collides but a net (0006).
- We do not recall a dropped drone: it drifts away like any floating stack, because a lost-and-found state is heavy
  for a rare case that follows the player's own act.
- We do not refund a canister when the target vanishes before the drone gets there; the trip goes on to the next target.
- We do not check that the load fits before launch, only that a crate has an empty slot; delivery handles the rest.
- We do not show a live reason why a docked drone with a canister stays put; the item's card says what it needs.

## Decided (were open questions)

1. The salvage drone has a battery like any drone; its pad lamps show its charge (user).
2. The fuel dial shows nothing: too small to read (user).
3. A target one full canister cannot cover is skipped (step 7; user).
4. A hero's body floating in space is left (step 7).
5. A bay without power launches a salvage drone only if it is charged, as ordinary drones (user).
6. One launch per bay per look, the ordinary one first (step 7).
7. A canister spent on a target that vanished is not refunded (user).

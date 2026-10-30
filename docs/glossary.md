# Glossary

Game words → code. One line a term: Russian / English → identifiers → module (`sim/X`, `f/x` = `game/features/x`).
**≠** marks a term whose code name differs from the game word. Ids are strings in `sim/Content`; names in each feature's `logic/<Name>.En.cs`
(`item.<id>`, `tool.<build>`). Where the code lives: [map.md](map.md).

## Ship and building

- люк / hatch **≠** → `Structure.HatchClosed`, `Structure.HatchOpen`, build `Builds.Hatch` = `"hatch"`, a lying doorway (`StructureGroup.Hatch`); art `hatch_open_*`, `hatch_closed_*` → Grid, Building
- свойство конструкции / structure trait **≠** → `Trait` (`Blocks`, `Supports`, `Climbable`, `Seals`, `BlocksLight`, `Terrain`, `Opening`, `BlocksFromBelow`) in c/`Structures.cs`, `Structures.Of(code)`, `IGridQueries.Solid(cell)` and the like; material traits `Hurts`, `Grows`, `Footing`, `Look`; machine traits `Container`, `Gas`, `Vents`, `Zone`, `Glow` (0112) → Grid
- дверь / door → `Structure.DoorClosed`, `Structure.DoorOpen`, command `SetDoor`; `Doorways`, r/`DoorShape`, f/grid `DoorSlides` → Grid
- лестница / ladder → `Structure.Ladder`, `tool.ladder`; climbing in f/crew `Hero` → Grid, Crew
- бортовой компьютер / ship computer → machine `computer` (`MachineTrait.Computer`), f/computer `ComputerPanel`; радарная антенна / radar antenna → `radarAntenna`; программа / program, радар / radar → c/`Programs.cs`, f/computer `RadarScreen`; их детали / their parts → items `processor`, `display`, `transceiver` (circuit assembler) → Crafting
- двигатель / engine → machine `engine` (`Exhaust`, `Takes`: biofuel), its nozzle out through the stern's wall (`grid.noExhaust`); сопло / nozzle, топливный насос / fuel pump, запал / igniter → items `nozzle`, `fuelPump` (Former), `igniter` (circuit assembler) → Crafting
- платформа / platform **≠** → `Structure.Floor`, `tool.floor`, build `"floor"` → Grid
- открывающаяся (посадочная) платформа / launch platform **≠** → `Structure.PlatformClosed`, `Structure.PlatformOpen`, build `Builds.Platform` = `"launchPlatform"`, a run doorway (`StructureGroup.Platform`, `Join.Run`), command `SetPlatform`; f/power `PlatformLamps`; item `deckDrive` → Landing, Grid
- капсула / посадочная капсула / колокол / drop pod **≠** → machine `"dropPod"` (`MachineTrait.Seats`, `Lands`), commands `Board`, `Alight`, `Launch` (descend / return), `LandingModule.Seats`, `Flights`, `Owners`; art `machine_dropPod.png`, `dropPod_porthole.png`, `dropPod_lit.png`; f/landing `PodSeats`, f/crew `SeatedHeroes` → Landing
- шахта под платформой / platform shaft → the column under a launch platform to the grid's edge, kept empty: `grid.underPlatform`, `grid.shaftBlocked`, `IGridQueries.ClearBelow`, `Taken`, `landing.shaftBlocked`; f/building `Shafts` → Grid, Building
- стена / wall → `Structure.Wall` + a material (`c/Materials.cs`), `tool.wall`, `tool.wallScrap` … → Grid
- задняя стена / back wall → `Back.Wall`, layer `"back"`, `tool.backWall` → Grid
- обшивка / hull **≠** → the ship's walls (`Structure.Wall`; "hull wall" in `tool.wallScrap.about`); item `hullPlate`, `hullSection` are salvage → Grid, Debris
- слой / layer → `GridLayers` (`sim/Grid/ChangeLog.cs`), f/building `PlanKey`, `Placement` layer → Grid, Building
- план / plan → `Plan`, `PlacePlan`, `CancelPlan`, `Work` (`sim/Building`); f/building `PlanHand`, `BuildMenu`; план мод / plan modes (Z X C) `PlanMode`, `PlanHand.Mode`; r/`PlanTiles`, `PlanOutline` → Building
- мгновенная постройка / instant building (dev panel) → `SetInstant`, `BuildingModule.Instant`; f/dev-panel → Building
- меню строительства / build menu → f/building `BuildMenu`, `Tools.Tabs`; cards and order from content: `MenuSlot` on machine, build and material rows, `MenuTabs` (c/`Menu.cs`, 0114) → Building
- планшет / tablet → `game/shared/Tablet.cs`, `TabletLayout`, `TabletMotion` (build menu and Handbook) → UI
- кисть / brush → `game/shared/logic/Brush.cs`, `Spec.Builds.Brush` (cells a strike covers; Ctrl = one cell) → Building
- ступенька / step → step-up in f/crew `Hero` (`StepUp`, `StepOnto`), drawn by `game/shared/logic/StepEase.cs` (0106) → Crew
- отвёртка / screwdriver → item `screwdriver`, `c/Screwdrivers.cs`; works plans → Building
- комната / room → `Room`, `RoomsModule.RoomAt` → Rooms

## Air, power, pipes

- баллон / canister **≠** (oxygen canister) → item `canister`, `LoadCanister` (into a tank), `Refill` (the hero's air) → Atmosphere, Crew
- провод / cable **≠** → `Cable`, `tool.cable` (laid by the plan, wiring); item `wire` is the crafted part → Power
- проводка / wiring (plan mode, X) → `PlanMode.Wiring`, f/power `PowerHands` → Power
- труба / pipe → spans: `LayPipe`, `CutPipe`, `PipeNetworks`, `tool.pipe` → Pipes
- клапан / valve → `Valve`, `MakeValve`, `SetValve`, `tool.valve` → Pipes
- порт, вход, выход / port, input, output → `Port`, `Way.In`, `Way.Out` (`c/Fluids.cs`), `PortAt` (2026-10-29-typed-ports) → Pipes, Grid
- среда / fluid **≠** (what a pipe carries) → `Fluids` (`air`, `water`); a line's: `FluidAt` → Pipes
- трубы (режим) / piping (plan mode, C) → `PlanMode.Piping`, f/pipes `PipeHands` → Pipes
- генератор / generator **≠** → `"generator"` is the oxygen generator (Atmosphere); `"burnerGenerator"` burns fuel for power (Furnace, Power)
- панель / solar panel → machine `solarPanel` → Power (also: a UI panel, `*Panel.cs`)
- аккумулятор / accumulator → item `accumulator` (belt equipment); the machine is `capacitorBank` "Capacitor bank" **≠** → Items, Power
- батарея / battery → item `battery` (belt), charged at `charger` "Charging station" → Items, Power
- танк, бак / tank → machine `tank`, `TankAir`; f/atmosphere `TankLooks` → Atmosphere

## Items, crafting, research

- верстак / arc bench **≠** → machine `arcBench`; f/crafting `BenchView` → Crafting
- модуль / machine module **≠** → `ModuleSpec` (`c/Modules.cs`), `Fit`, `Unfit` (`sim/Items/Modules.cs`), item `arcModule` → Items. Not a sim module (`Module<T>`).
- дробилка / crusher → machine `crusher` (process `crush`), its items `asteroidOre` (руда астероида), `ironOre`, `gravel` (щебень), `quartz` (кварц) → Crafting
- плавильня / electric smelter → machine `smelter` (station in `c/Stations.cs`), f/crafting `SmelterHeat` → Crafting
- печь / furnace → machine `furnace`, `FurnaceModule`, `Fire` → Furnace
- побочный продукт, шанс / byproduct → `ItemSpec.Byproducts` (`LootEntry`, its `OneIn`), drawn as a fed station's smelt completes; several a smelt → `ItemSpec.SmeltTakes`, `TooFew` → Crafting
- капсула / grow capsule → machine `growCapsule` → Crafting. Lifepod **≠**: machine `lifepod`, its station "Fabricator" (`station.lifepod`)
- станция технологий / tech station → machine `techStation`, `TechPanel` → Research
- дерево технологий, категория, блок, ворота / tech tree, category, block, gate → `Content.Research.Categories`, `TechCategory`, `TechNode` → Research
- взнос / deposit → `Deposit`, `Knowledge.Deposits` → Research
- ценность, наценка / worth, markup → `Content.Market.WorthOf`, `Markup`; цена блока → `Research.Prices` → Research
- торговая станция / trade station → machine `tradeStation`, `TradeModule` → Trade
- кредиты, цены / credits, prices → `Knowledge.Credits`; `Content.Market` → Trade
- чертёж / blueprint → `Blueprint`, `Blueprints` → Research
- ящик / crate → machine `crate`; item `cargoCrate` is salvage → Items
- дрон / drone → item `drone`, `Drone`, `LogisticsModule`, machine `droneBay` → Logistics
- небесный осколок / sky shard **≠** → item `arcShard` → Items, Ruins
- стойка электроники / electronics rack **≠** → item `cableRack` → Debris
- кислородные баки / oxygen tanks (wreckage) **≠** → item `tankCluster` → Debris
- верстак / workbench → machine `workbench` (paid by hand, no power) → Crafting
- формовщик / former → machine `former` → Crafting; circuit assembler **≠** → `assembler`

## Crew and tools

- герой / hero → `Hero` (sim/Crew `State.cs`; client f/crew `Hero.cs`) → Crew
- шлем / helmet → helmet lamp: item `helmetLamp`, `c/HelmetLamp.cs`; the helmet is drawn in f/crew `Figure` → Crew
- джетпак / jetpack → item `jetpack`, `Thrust`, `c/Jetpack.cs` → Crew
- гарпун / harpoon → item `harpoon`, `c/Harpoon.cs`, f/crew `HarpoonShot`, `HarpoonLine`; `Reel` → Crew, Debris
- топор / fire axe → item `fireAxe`, f/crew `AxeSwing`, `HeldLook.FireAxe` → Crew
- бур / drill → item `drill`, `c/Weapons.cs`, f/crew `DrillPace`, `Motor` → Crew
- трос / tether → f/crew `Tether` → Crew
- скафандр / suit → helmet `helmet` (`Content.Helmet`, slot `head`, `Tank`) + body `suit` (`Content.Suit`, slot `suit`), `Hero.Air`, `CapacityOf`; `SuitGauge`, `Suits` (colours), `Danger` (f/crew) → Crew, Items
- голый, без скафандра / bare → no helmet: `Crew.Lungs`; no body: `Crew.BareCold` → Crew
- внешность, облик, редактор персонажа / look → `Look`, `ChooseLook`, `LookOf`, `HeroLooks`, `LookEditor` (f/crew) → Players, Crew
- плащ / cape → `cape` (`Content.Cape`, slot `cape`, `Guard`, `Warmth`), `Hero.Spared`, `ICrewOps.Blow`; `Figure.WearsCape`, `Cape`, `CapeView` (f/crew) → Crew, Items
- фонарик на шлем / helmet lamp → `helmetLamp`, slot `mount` (`Helmet.Mount`), for whatever sits on a helmet → Items
- смерть, тело / death, body → `Bodies`, `BodyView` (0061) → Crew, Items

## World

- обломок / debris piece **≠** → `Piece`, `Hit`, `Reel` (sim/Debris); a wreck is debris of kind wreckage → Debris
- метеор / meteor → `Meteor`, `MeteorHit`, `Meteors.Shower` → Hazards
- планета / planet → `PlanetType` (`c/PlanetTypes.cs`), `Surface`, `Terrain` (sim/Core) → Core, Building
- копать / dig → `Dig`, `Crack` (sim/Building `Digging.cs`) → Building
- жила, руда / vein, ore → `Vein` (`c/Veins.cs`), materials `copperVein`, `iceVein` (`Material.In`), drawn in `Terrain` → Core
- рудная зона / lode → `PlanetType.LodeWave`, `LodeOver`, `CaveKind.Lode` (crystals by the veins) → Core
- равнина, нагорье / plain, highland → `Ground.RegionWave`, `RegionLeast`, `RegionLift` → Core
- мох / moss **≠** → `Grass` (f/grid `Grass.cs`, the living edge); `Sound.StepMoss` → Grid (client only)
- растение / plant → `Plant` (sim/Building `Flora.cs`), `c/Flora.cs` → Building
- существо / creature **≠** → `Creature`, `CreaturesModule` → Creatures
- лопер / loper → `Creatures.Loper` = `"loper"` → Creatures
- сентри / sentry → `Creatures.Sentry` = `"sentry"`: a `Clinger` on a wreck, `Guard`, `Return`, `CutTether`, `Pounce`; dev `--sentry` (`=fight`: a line sawn, struck, its wreck broken; `=crawl`) → Creatures
- клещ / tick → the sentry's look (its pick page's «Клещ»): `CreatureLooks.Ticks`, f/creatures `TickBodies`, `TickArt`, `TickMotion`; sounds `Sound.Ticks`
- вид / species → `Content.Creatures.Species`, composed of a body (`Shape`: `Walker`), `Life`, a faction, a brain and moves → Creatures
- побуждение / drive **≠** → `Drive` (`Graze`, `Flee`, `Retaliate`, `Escape`), its rule a `DriveRule`; a creature's `Drive`, `DriveLeft`, `Step` → Creatures
- действие / act **≠** → `Act` (`Move`, `Phase`, `Left`), a move's rule a `MoveRule` (`Lunge`, the ram) → Creatures
- фракция / faction → `Species.Faction`, `Content.Creatures.Factions`, `Relations` (`Stance`) → Creatures
- поза / posture **≠** → `Posture`, derived from the drive or act in charge, what the client draws by → Creatures
- руины / ruins → `c/Ruins.cs`, f/ruins `HomesView` (ruin pieces as plants) → Building, Ruins
- шпиль / spire → f/ruins `SpiresView`, its cradle a `Relic` → Ruins
- реликвия / relic → `Relic` (sim/Building `Flora.cs`), `RelicArt`, `RelicSparks` → Ruins
- лава / lava → `c/Lava.cs`, f/grid `LavaView` → planets
- вылазка / sortie → `OpenSurface`, `Transfer` (sim/Core `Surface.cs`) → Core

## Game

- сохранение / save → `World.Save()`, `World.Load` (sim/Core `Sim.cs`), `Migrations` (sim/Registry), `game/platform/logic/Saves.cs`, `WorldIndex` → Core
- хост / host, co-op → net/Net `Host`, `Client`, `Session`; game/menu `Together`; `game/Main.Sessions.cs` → net
- справочник / Handbook → f/handbook `HandbookPanel`, `game/Content/logic/Handbook.cs` → UI
- подсказка / tooltip → `game/shared/logic/Tip.cs`, `PointerTip` → UI
- дев-аргумент, демо / dev argument, demo → `-- --<name>`: one `DevMode` each in `game/dev/logic/DevModes.cs`, wired in `game/dev/Main.Dev.cs` → Dev tools

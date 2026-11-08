# Glossary

Game words → code. One line a term: Russian / English → identifiers → module (`sim/X`, `f/x` = `game/features/x`).
**≠** marks a term whose code name differs from the game word. Ids are strings in `sim/Content`; names in each feature's `logic/<Name>.En.cs` and `.Ru.cs`
(`item.<id>`, `tool.<build>`). Where the code lives: [map.md](map.md).

## Ship and building

- люк / hatch **≠** → `Structure.HatchClosed`, `Structure.HatchOpen`, build `Builds.Hatch` = `"hatch"`, a lying doorway (`StructureGroup.Hatch`); art `hatch_open_*`, `hatch_closed_*` → Grid, Building
- свойство конструкции / structure trait **≠** → `Trait` (`Blocks`, `Supports`, `Climbable`, `Seals`, `BlocksLight`, `Terrain`, `Opening`, `BlocksFromBelow`) in c/`Structures.cs`, `Structures.Of(code)`, `IGridQueries.Solid(cell)` and the like; material traits `Hurts`, `Grows`, `Footing`, `Look`; machine traits `Container`, `Gas`, `Vents`, `Zone`, `Glow` (0112) → Grid
- дверь / door → `Structure.DoorClosed`, `Structure.DoorOpen`, command `SetDoor`; `Doorways`, r/`DoorShape`, f/grid `DoorSlides` → Grid
- лестница / ladder → `Structure.Ladder`, `tool.ladder`; climbing in f/crew `Hero` → Grid, Crew
- бортовой компьютер / ship computer → machine `computer` (`MachineTrait.Computer`), f/computer `ComputerPanel`; радарная антенна / radar antenna → `radarAntenna`; программа / program, радар / radar → c/`Programs.cs`, f/computer `RadarScreen`; точка на радаре, контакт / radar contact, заброшенный корабль / derelict ship → `Contact`, kind `wreck` (sim/Contacts, c/`Contacts.cs`), its selection `Ui.RadarContact`; их детали / their parts → items `processor`, `display`, `transceiver` (circuit assembler) → Crafting
- двигатель / engine → machine `engine` (`Exhaust`, `Takes`: biofuel), its nozzle out through the stern's wall (`grid.noExhaust`); сопло / nozzle, топливный насос / fuel pump, запал / igniter → items `nozzle`, `fuelPump` (Former), `igniter` (circuit assembler) → Crafting
- платформа / platform **≠** → `Structure.Floor`, `tool.floor`, build `"floor"` → Grid
- опора, «стоит на полу» / footing **≠** → `Structures.Footing`, `IGridQueries.Footing` (wall, floor, grate; no doorway); standing machines `Machines.Stands`, hung ones `MachineTrait.Hangs` (lamps, vent); `building.noFloor`, `building.holdsMachine` → Building, Grid
- открывающаяся (посадочная) платформа / launch platform **≠** → `Structure.PlatformClosed`, `Structure.PlatformOpen`, build `Builds.Platform` = `"launchPlatform"`, a run doorway (`StructureGroup.Platform`, `Join.Run`), command `SetPlatform`; f/power `PlatformLamps`; item `deckDrive` → Landing, Grid
- капсула / посадочная капсула / колокол / drop pod **≠** → machine `"dropPod"` (`MachineTrait.Seats`, `Lands`), commands `Board`, `Alight`, `Launch` (descend / return), `LandingModule.Seats`, `Flights`, `Owners`; art `machine_dropPod.png`, `dropPod_porthole.png`, `dropPod_lit.png`; f/landing `PodSeats`, f/crew `SeatedHeroes` → Landing
- беговое колесо / колесо / treadmill wheel → machine `"treadWheel"` (`Seats` without `Lands`, `MachineSpec.Anywhere`): makes 4 while its rider runs (`Exert`), rested by `LandingModule.Turning`; f/crew `SeatedHeroes` → Landing, Power
- шахта под платформой / platform shaft → the column under a launch platform to the grid's edge, kept empty: `grid.underPlatform`, `grid.shaftBlocked`, `IGridQueries.ClearBelow`, `Taken`, `landing.shaftBlocked`; f/building `Shafts` → Grid, Building
- стена / wall → `Structure.Wall` + a material (`c/Materials.cs`), `tool.wall`, `tool.wallScrap` … → Grid
- задняя стена / back wall → `Back.Wall`, layer `"back"`, `tool.backWall` → Grid
- литой лом / cast scrap → material `castScrap` (`c/Materials.cs`), walls of smelted `scrap`; лом в стене / patched scrap is `scrap` → Grid
- обшивка / hull **≠** → the ship's walls (`Structure.Wall`; "hull wall" in `tool.wallScrap.about`); item `hullPlate`, `hullSection` are salvage → Grid, Debris
- слой / layer → `GridLayers` (`sim/Grid/ChangeLog.cs`), f/building `PlanKey`, `Placement` layer → Grid, Building
- план / plan → `Plan`, `PlacePlan`, `CancelPlan`, `Work` (`sim/Building`); f/building `PlanHand`, `BuildMenu`; план мод / plan modes (Z X C) `PlanMode`, `PlanHand.Mode`; r/`PlanTiles`, `PlanOutline` → Building
- мгновенная постройка / instant building (dev panel) → `SetInstant`, `BuildingModule.Instant`; f/dev-panel → Building
- меню строительства / build menu → f/building `BuildMenu`, `Tools.Tabs`; cards and order from content: `MenuSlot` on machine, build and material rows, `MenuTabs` (c/`Menu.cs`, 0114) → Building
- кромка, верхушка планшета / tablet's edge, rim → `game/shared/TabletEdge.cs`, `TabletPeek` (the keys along the foot) → UI
- планшет / tablet → `game/shared/Tablet.cs`, `TabletLayout`, `TabletMotion` (build menu, Handbook, the board and the black market) → UI
- кисть / brush → `game/shared/logic/Brush.cs`, `Spec.Builds.Brush` (cells a strike covers; Ctrl = one cell) → Building
- ступенька / step → step-up in f/crew `Hero` (`StepUp`, `StepOnto`), drawn by `game/shared/logic/StepEase.cs` (0106) → Crew
- отвёртка / screwdriver → item `screwdriver`, `c/Screwdrivers.cs`; works plans → Building
- шуруповёрт / power driver **≠** бур → item `powerDriver`, `Screwdrivers.PowerDriver` (`Area` 2); f/crew `SquareAt`, `WorkTargets`, `Ui.DriverSquare` → Building, Crew
- комната / room → `Room`, `RoomsModule.RoomAt` → Rooms

## Air, power, pipes

- баллон / canister **≠** (oxygen canister) → item `canister`, `LoadCanister` (into a tank), `Refill` (the hero's air), a salvage drone's air (`TripAir`) → Atmosphere, Crew, Logistics
- провод / cable **≠** → `Cable`, `tool.cable` (laid by the plan, wiring); item `wire` is the crafted part → Power
- проводка / wiring (plan mode, X) → `PlanMode.Wiring`, f/power `PowerHands` → Power
- труба / pipe → spans: `LayPipe`, `CutPipe`, `PipeNetworks`, `tool.pipe` → Pipes
- клапан / valve → `Valve`, `MakeValve`, `SetValve`, `tool.valve` → Pipes
- порт, вход, выход / port, input, output → `Port`, `Way.In`, `Way.Out` (`c/Fluids.cs`), `PortAt` (2026-10-29-typed-ports) → Pipes, Grid
- среда / fluid **≠** (what a pipe carries) → `Fluids` (`air`, `water`); a line's: `FluidAt` → Pipes
- цистерна / cistern → machine `cistern`: water poured in by the bottle, given on to an electrolyser (2026-11-02-water-to-oxygen) → Water
- вода, бутылка / water, bottle → item `water` (`Content.Water.Item`), melted of ice → Water
- электролизёр / electrolyser → machine `electrolyser`: a cistern's water into a tank's liquid oxygen (2026-11-02-water-to-oxygen) → Water
- трубы (режим) / piping (plan mode, C) → `PlanMode.Piping`, f/pipes `PipeHands` → Pipes
- генератор / generator **≠** → `"generator"` is the oxygen vaporizer (Atmosphere); `"burnerGenerator"` burns fuel for power, `"biofuelGenerator"` (биотопливный генератор) biofuel alone, 16 a tick (Furnace, Power)
- испаритель / vaporizer → machine `generator`: a tank's liquid oxygen into air on power (2026-10-30-liquid-oxygen) → Atmosphere
- жидкий кислород / liquid oxygen → fluid `liquidOxygen`: what a tank holds and a liquid pipe carries → Atmosphere, Pipes
- чёрный рынок / black market → tab `blackMarket` of `Content.Research.Tabs` → Research; `MarketPage` drawn in f/research
- товар, плитка рынка / a good, a market tile → `MarketGood`, `MarketGoods` (client): a kind of upgrade's blocks as one ladder, or a blueprint
- расшифровка, каша / the decipher, garbage → `MarketReveal`, `Cipher` (client)
- панель / solar panel → machine `solarPanel` → Power (also: a UI panel, `*Panel.cs`)
- светочувствительная панель / photosensitive panel → item `photoPanel`, in the solar panel's cost, had only of one taken apart
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
- ферментёр / fermenter → machine `fermenter` (process `ferment`): biomass and fat into biofuel, the only way to it → Crafting
- капсула / grow capsule → machine `growCapsule` → Crafting. Lifepod **≠**: machine `lifepod`, its station "Fabricator" (`station.lifepod`)
- доска / the board **≠** → `BoardPage` in `ResearchTablet` on the tablet (T); `BoardSlots`, `BoardView`, `BoardPile` (client) → Research. The tech tree is gone (the-board): the board learns, the black market sells
- заметка, силуэт, нить / note, silhouette (shape), thread → `BoardView`, `BoardThreads`, `BoardPencil` (how far a shape has come) (client)
- найдено, счёт / obtained, count → `Knowledge.Obtained` (`Tally`), `ObtainedOf`, counted in `Items` → Knowledge, Items
- достаточно / enough → `Understanding.Enough`, `Content.Research.EnoughWorth`, `EnoughMost`; понять / understood (the rule) → `ResearchModule`'s walk, `Knowledge.Granted` → Research
- продано / sold → `Knowledge.Sold`, `SoldOf`, counted at a capsule's launch → Knowledge, Trade
- строка рынка, блок, заказ / market ring, block, order → `Content.Research.Tabs` (the one `blackMarket`), `TechRing`, `TechNode` (`Gate`, `Order`: what a ring wants sold) → Research
- связи ноды / a node's ties → `TechLinks` (client)
- ценность, наценка / worth, markup → `Content.Market.WorthOf`, `Markup`; цена блока → `Research.Prices` → Research
- улучшения, слоты инвентаря, пояса, терминала, баллонов / upgrades → `Content.Upgrades`, `Items.SlotOpen`, `Crew.CanisterRefill` → Items, Crew, Research
- торговая станция / trade station → machine `tradeStation`, `TradeModule` → Trade; авто / auto trade station → `autoTradeStation`
- капсула с товаром / trade capsule → `SendCapsule`, `Flight`, `Market.CapsuleFlight` → Trade; `CapsuleFlights` drawn in f/research
- кредиты, цены / credits, prices → `Knowledge.Credits`; `Content.Market` → Trade
- чертёж / blueprint → `Blueprint`, `Blueprints` → Research
- ящик / crate → machine `crate`; item `cargoCrate` is salvage → Items; запас, крафт из ящиков / stock → `TakeStock`, `StockTotals` (craft-from-crates, build-from-crates) → Items, Crafting, Building
- дрон / drone → item `drone`, `Drone`, `LogisticsModule`, machine `droneBay` (its fuel slot of canisters: `SlotTakes`) → Logistics, Items
- дрон-сборщик / salvage drone → item `salvageDrone`, `Drone.Air`, `Salvage.cs` → Logistics
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
- движение удара / swing motion **≠** → `SwingMotion` (keys `MotionKey`), a weapon's `Stroke.Motions`, played as `MotionPlay` with `Overhand`, `Jab` (f/crew `MotionPlay.cs`), its `MotionKind` in gc/`SwingMotions.cs`; the offset the hero carries `AxeSwing.Aim` → Crew
- сектор удара / aim sector **≠** → `Sector`, `Sectors` (up, straight, down, under) in gc/`SwingMotions.cs` → Crew
- бур / drill → item `drill`, `c/Weapons.cs`, f/crew `DrillPace`, `Motor` → Crew
- трос / tether → f/crew `Tether` → Crew; подтягивание по тросу / haul → `Hero.Haul`, `Hero.Wraps` (tether-haul)
- скафандр / suit → helmet `helmet` or `heavyHelmet` (`Content.Helmet`, slot `head`, `Tank`) + body `suit` or `regenSuit` (`Content.Suit`, slot `suit`; its mode `SuitMode`, `Hero.SuitOn`), `Hero.Air`, `CapacityOf`; `SuitGauge`, `Suits` (colours), `Danger` (f/crew) → Crew, Items
- голый, без скафандра / bare → no helmet: `Crew.Lungs`; no body: `Crew.BareCold` → Crew
- внешность, облик, редактор персонажа / look → `Look`, `ChooseLook`, `LookOf`, `HeroLooks`, `LookEditor` (f/crew) → Players, Crew
- плащ / cape → `cape` (`Content.Cape`, slot `cape`, `Guard`, `Warmth`), `Hero.Spared`, `ICrewOps.Blow`; `Figure.WearsCape`, `Cape`, `CapeView` (f/crew) → Crew, Items
- фонарик на шлем / helmet lamp → `helmetLamp`, slot `mount` (`Helmet.Mount`), for whatever sits on a helmet → Items
- смерть, тело / death, body → `Bodies`, `BodyView` (0061) → Crew, Items

## World

- обломок / debris piece **≠** → `Piece`, `Hit`, `Reel` (sim/Debris); a wreck is debris of kind wreckage → Debris
- сеть, сеть для обломков / debris net → machine `net`, trait `Catches`, `Collect`, `NetEntry`, `DebrisModule.Held` (sim/Debris `Nets.cs`) → Debris
- метеор / meteor → `Meteor`, `MeteorHit`, `Meteors.Shower` → Hazards
- вспышка, магнитная буря / magnetic flare → `MagneticFlares.Flare`, `FlareNow`, `FlareComing` (sim/Hazards `Flares.cs`) → Hazards
- сломан, поломка / broken → `IPowerQueries.IsBroken`, `IPowerOps.Break` → Power
- ремонт, починить / repair → `Repair`, `RepairProgress`, `RepairRules` (sim/Building `Repairs.cs`) → Building
- планета / planet → `PlanetType` (`c/PlanetTypes.cs`), `Surface`, `Terrain` (sim/Core) → Core, Building
- фон, задний план, параллакс / backdrop, the land behind → `PlanetSky` (r/, the sky), `BackdropView`, `BackdropPlan` (f/backdrop, the land), `BackdropLooks` (gc) → client only
- сутки, день, ночь, время суток / day, night, hour → `Sky.Cycle` (`c/Sky.cs`), `SkyNow`, `SetTime` (sim/Sky); fades `Daylight` (gc) → Sky
- ночной холод, мороз / the night's chill → `Cycle.Chill`, `ColdAt` (sim/Crew), frost `Report.Cold` → Crew
- копать / dig → `Dig`, `Crack` (sim/Building `Digging.cs`) → Building
- жила, руда / vein, ore → `Vein` (`c/Veins.cs`), materials `copperVein`, `iceVein`, `ironVein` (`Material.In`), drawn in `Terrain` → Core
- залежь, рудный карман / ore pocket → `Pockets` (`c/Veins.cs`), `Terrain.PaintPockets` → Core
- рудная зона / lode → `PlanetType.LodeWave`, `LodeOver`, `CaveKind.Lode` (crystals by the veins) → Core
- равнина, нагорье / plain, highland → `Ground.RegionWave`, `RegionLeast`, `RegionLift` → Core
- мох / moss **≠** → `Grass` (f/grid `Grass.cs`, the living edge); `Sound.StepMoss` → Grid (client only)
- растение / plant → `Plant` (sim/Building `Flora.cs`), `c/Flora.cs` → Building
- существо / creature **≠** → `Creature`, `CreaturesModule` → Creatures
- лопер / loper → `Creatures.Loper` = `"loper"` → Creatures
- сентри / sentry → `Creatures.Sentry` = `"sentry"`: a `Clinger` on a wreck, `Guard`, `Return`, `CutTether`, `Pounce`; dev `--sentry` (`=fight`: a line sawn, struck, its wreck broken; `=crawl`) → Creatures
- страж шпиля / spire guard → `Creatures.Warden` = `"warden"`: a `Walker` at a `Post`, `Ward`, `Pounce`; dev `--ground=spire-guards`
- истукан / Istukan → the warden's look: `CreatureLooks.Istukans`, f/creatures `IstukanBodies`, `IstukanArt`, `IstukanMotion`; sounds `Sound.Istukans`
- клещ / tick → the sentry's look (its pick page's «Клещ»): `CreatureLooks.Ticks`, f/creatures `TickBodies`, `TickArt`, `TickMotion`; sounds `Sound.Ticks`
- вид / species → `Content.Creatures.Species`, composed of a body (`Shape`: `Walker`), `Life`, a faction, a brain and moves → Creatures
- побуждение / drive **≠** → `Drive` (`Graze`, `Flee`, `Retaliate`, `Escape`), its rule a `DriveRule`; a creature's `Drive`, `DriveLeft`, `Step` → Creatures
- действие / act **≠** → `Act` (`Move`, `Phase`, `Left`), a move's rule a `MoveRule` (`Lunge`, the ram) → Creatures
- фракция / faction → `Species.Faction`, `Content.Creatures.Factions`, `Relations` (`Stance`) → Creatures
- шляпоспин / capback → `Creatures.Capback` = `"capback"`: a cave lurker of the garden, a `Walker` on `Guard`; `Vent` (gas), `Shoot` (slime) → Creatures
- жеодник / geode snail → `Creatures.GeodeSnail` = `"geodeSnail"`: a cave lurker of the amber deep, a `Walker` on `Guard` and `Shell`, a steady `Lunge` (its bite) → Creatures
- засадник / lurker → a cave hostile on `Guard` (cave-hostiles), faction `beasts` → Creatures
- облако газа, лужа слизи / gas cloud, slime puddle → a `Patch` of `Creatures.Gas`, `Creatures.Slime` (`PatchKind`), lurker-attacks → Creatures
- раковина / shell → `Creatures.Shell`: the snail shut, hurt by nothing; open, by melee only → Creatures
- поза / posture **≠** → `Posture`, derived from the drive or act in charge, what the client draws by → Creatures
- руины / ruins → `c/Ruins.cs`, f/ruins `HomesView` (ruin pieces as plants) → Building, Ruins
- шпиль / spire → f/ruins `SpiresView`, its cradle a `Relic` → Ruins
- реликвия / relic → `Relic` (sim/Building `Flora.cs`), `RelicArt`, `RelicSparks` → Ruins
- вода в пещерах, озеро / water, lake **≠** → liquid in the cells (`Content.Liquids.Water`), `LiquidsModule`, `ILiquidsQueries.AmountAt`; not the item `water` → Liquids
- лава / lava → `c/Lava.cs`, f/grid `LavaView` → planets
- вылазка / sortie → `OpenSurface`, `Transfer` (sim/Core `Surface.cs`) → Core

## Game

- сохранение / save → `World.Save()`, `World.Load` (sim/Core `Sim.cs`), `Migrations` (sim/Registry), `game/platform/logic/Saves.cs`, `WorldIndex` → Core
- хост / host, co-op → net/Net `Host`, `Client`, `Session`; game/menu `Together`; `game/Main.Sessions.cs` → net
- справочник / Handbook → f/handbook `HandbookPanel`, `game/Content/logic/Handbook.cs` → UI
- подсказка / tooltip → `game/shared/logic/Tip.cs`, `PointerTip` → UI
- дев-аргумент, демо / dev argument, demo → `-- --<name>`: one `DevMode` each in `game/dev/logic/DevModes.cs`, wired in `game/dev/Main.Dev.cs` → Dev tools

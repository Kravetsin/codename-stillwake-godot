# Glossary

Game words → code. One line a term: Russian / English → identifiers → module (`sim/X`, `f/x` = `game/features/x`).
**≠** marks a term whose code name differs from the game word. Ids are strings in `sim/Content`; names in each feature's `logic/<Name>.En.cs` and `.Ru.cs`
(`item.<id>`, `tool.<build>`). Where the code lives: [map.md](map.md).

## Ship and building

- люк / hatch **≠** → `Structure.HatchClosed`, `Structure.HatchOpen`, build `Builds.Hatch` = `"hatch"`, a lying doorway (`StructureGroup.Hatch`); art `hatch_open_*`, `hatch_closed_*` → Grid, Building
- свойство конструкции / structure trait **≠** → `Trait` (`Blocks`, `Supports`, `Climbable`, `Seals`, `BlocksLight`, `Terrain`, `Opening`, `BlocksFromBelow`) in c/`Structures.cs`, `Structures.Of(code)`, `IGridQueries.Solid(cell)` and the like; material traits `Hurts`, `Grows`, `Footing`, `Look`; machine traits `Container`, `Gas`, `Vents`, `Zone`, `Glow` (0112) → Grid
- дверь / door → `Structure.DoorClosed`, `Structure.DoorOpen`, command `SetDoor`; `Doorways`, r/`DoorShape`, f/grid `DoorSlides` → Grid
- лестница / ladder → `Structure.Ladder`, `tool.ladder`; climbing in f/crew `Hero` → Grid, Crew
- бортовой компьютер / ship computer → machine `computer` (`MachineTrait.Computer`), f/computer `ComputerPanel`; радарная антенна / radar antenna → `radarAntenna`; программа / program, радар / radar → c/`Programs.cs`, f/computer `RadarScreen`; точка на радаре / radar contact, «заброшенный корабль» / "derelict ship" → places' `Contact` (its `Offset`, `Left`; c/`Contacts.cs`), its selection `Ui.RadarContact`, its callout's «Проложить маршрут» the shuttle's route; их детали / their parts → items `processor`, `display`, `transceiver` (circuit assembler) → Crafting
- солнечная система / solar system, планета системы / system planet → `Content.Landing.Planets` (`SystemPlanet`: type, technology), `LandingModule.At`; курс / перелёт / the ship's course, flight → `Landing.Course`, command `PlotCourse` (the radar's «Проложить курс»), `JumpFuel`, `FlightTicks`; the call home → `landing.crewAway`, f/landing `JumpCall`; карта радара / radar map → f/computer `RadarScreen`, `logic/NavMap.cs`; выноска / callout → its callout → Landing
- двигатель / engine → machine `engine` (`Exhaust`, a 20 l fuel store, its fuel input on its right), its nozzle out through the stern's wall (`grid.noExhaust`); сопло / nozzle, топливный насос / fuel pump, запал / igniter → items `nozzle`, `fuelPump` (Former), `igniter` (circuit assembler) → Crafting
- платформа / platform **≠** → `Structure.Floor`, `tool.floor`, build `"floor"` → Grid
- опора, «стоит на полу» / footing **≠** → `Structures.Footing`, `IGridQueries.Footing` (wall, floor, grate; no doorway); standing machines `Machines.Stands`, hung ones `MachineTrait.Hangs` (lamps, vent); `building.noFloor`, `building.holdsMachine` → Building, Grid
- открывающаяся (посадочная) платформа / launch platform **≠** → `Structure.PlatformClosed`, `Structure.PlatformOpen`, build `Builds.Platform` = `"launchPlatform"`, a run doorway (`StructureGroup.Platform`, `Join.Run`), command `SetPlatform`; f/power `PlatformLamps`; item `deckDrive` → Landing, Grid
- капсула / посадочная капсула / колокол / drop pod **≠** → machine `"dropPod"` (`MachineTrait.Seats`, `Lands`, a 20 l fuel store), commands `Board`, `Alight`, `Launch` (descend / return), `LandingModule.Seats`, `Flights`, `Owners`; art `machine_dropPod.png`, `dropPod_porthole.png`, `dropPod_lit.png`; f/landing `PodSeats`, f/crew `SeatedHeroes` → Landing
- беговое колесо / колесо / treadmill wheel → machine `"treadWheel"` (`Seats` without `Lands`, `MachineSpec.Anywhere`): makes 4 while its rider runs (`Exert`), rested by `LandingModule.Turning`; f/crew `SeatedHeroes` → Landing, Power
- шатл / шаттл / челнок / «Ледокол» / crew shuttle **≠** → machine `"crewShuttle"` (`Seats`, `Lands`, `Riders` 4, `Seat.Place`, a 200 l fuel store piped at its stern): the crew's craft to the vessels, on a launch platform its whole length; `LandingModule.Voyages`, `Parked`; f/landing `ShuttleKey`, `ShuttleLights`; art `machine_crewShuttle.png` (anime-art `shuttle`). Not the vessel class `shuttle` (a derelict) → Landing
- маршрут / course → `Places.Course`, command `SetCourse` (the radar's «Проложить маршрут»), its fuel `PlacesModule.CostOf`, burnt from the shuttle's store on setting off → Places
- отправиться / set off → command `SetOff` (places'), `ILandingOps.SetOff`, `SetOffHome`; the shuttle's «Отправиться» key → Places, Landing
- рейс / voyage → `Voyage` (sim/Landing): the shuttle in the air with its riders and its fuel, `Content.Shuttle.FlightTicks`; client `Ride` → Landing
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
- топливный бак / fuel tank → machine `fuelTank` (a 100 l fuel store, «Бочка на ложементе») → Fuel
- топливо, биотопливо / fuel, biofuel **≠** → item `biofuel` (a 1 l can) and fluid `fuel` (`Content.Fuel`); the litres a machine holds are its store: `FuelHolds`, `MachineTrait.HoldsFuel`, `IFuelQueries.FuelIn`, `DrawFuel`, `Lasts` (how long it lasts) → Fuel
- бункер, приёмник / hopper → a store's slot that takes its one item and pours it in (`SlotTakes`): a tank's canisters, a cistern's bottles, a fuel store's cans → Atmosphere, Water, Fuel
- смотровое стекло, уровень / sight glass, level → `GlassLevel`, `SightGlass` (game/shared), pictures `{kind}_level`, `{kind}_level_top` → Atmosphere, Water, Fuel (client)

## Items, crafting, research

- верстак / arc bench **≠** → machine `arcBench`; f/crafting `BenchView` → Crafting
- корзина / trash → `Inventory.Trash`, `Stacks.Trashed`; f/items `TrashBurst` → Items (creative only)
- модуль / machine module **≠** → `ModuleSpec` (`c/Modules.cs`), `Fit`, `Unfit` (`sim/Items/Modules.cs`), item `arcModule` → Items. Not a sim module (`Module<T>`).
- дробилка / crusher → machine `crusher` (process `crush`), its items `asteroidOre` (руда астероида), `ironOre`, `gravel` (щебень), `quartz` (кварц) → Crafting
- плавильня / electric smelter → machine `smelter` (station in `c/Stations.cs`), f/crafting `SmelterHeat` → Crafting
- печь / furnace → machine `furnace`, `FurnaceModule`, `Fire` → Furnace
- побочный продукт, шанс / byproduct → `ItemSpec.Byproducts` (`LootEntry`, its `OneIn`), drawn as a fed station's smelt completes; several a smelt → `ItemSpec.SmeltTakes`, `TooFew` → Crafting
- ферментёр, перегонный аппарат / fermenter → machine `fermenter` (process `ferment`): biomass and fat into its 20 l fuel store, the only way to biofuel → Crafting, Fuel
- капсула / grow capsule → machine `growCapsule` → Crafting. Lifepod **≠**: machine `lifepod`, its station "Fabricator" (`station.lifepod`)
- доска / the board **≠** → `BoardPage` in `ResearchTablet` on the tablet (T); `BoardLayout`, `BoardDistricts`, `BoardView` (client) → Research. Fixed district islands (board-islands); the board learns, the black market sells
- район, силуэт, находки, цель / district, silhouette, finds, goal → `BoardDistricts`, `BoardView` (almost), `BoardSeen` (batches as cards), `BoardChain` (a pinned goal), `BoardPencil` (how far a silhouette has come) (client)
- найдено, счёт / obtained, count → `Knowledge.Obtained` (`Tally`), `ObtainedOf`, counted in `Items` → Knowledge, Items
- понять / understood (the rule: one of each part, a machine for a craft) → `ResearchModule`'s walk, `Knowledge.Granted` → Research
- своё знание, найдено игроком / own knowledge, found → `Learner` (`Peer`, `Found`, `Granted`), `Knowledge.Learners`, `Knows(peer, id)`, `KnowsAny`, `HasFound`, `Find`, `Grant(peer, id)`; Items' `Finds`: each player learns what they hold, purchases are everyone's (own-knowledge) → Knowledge, Items, Research
- продано / sold → `Knowledge.Sold`, `SoldOf`, counted at a capsule's launch → Knowledge, Trade
- строка рынка, блок, заказ / market ring, block, order → `Content.Research.Tabs` (the one `blackMarket`), `TechRing`, `TechNode` (`Gate`, `Order`: what a level wants sold) → Research
- репутация, уровень мафии / reputation, level → `Knowledge.Level`, `Research.Standing()` (2026-11-08-mafia-reputation) → Knowledge, Research
- связи ноды / a node's ties → `TechLinks` (client)
- ценность, наценка / worth, markup → `Content.Market.WorthOf`, `Markup`; цена блока → `Research.Prices` → Research
- улучшения, слоты инвентаря, пояса, терминала / upgrades → `Content.Upgrades`, `Items.SlotOpen` → Items, Research
- откат баллона / canister cooldown → `Crew.CanisterCooldown`, `Hero.CanisterIn`, `crew.canisterCooling` → Crew
- еда, блюдо, голод / food, meal, hunger → `Content.Foods`, `Content.Diet`, `Hero.Meals`, `Eat`, `Hungry`; паёк `ration`, тушёнка `cannedMeat`, фрукт `fruit`, светоягода `glowberry` (crops, crushed or eaten for their seed back), семечко `seed` (a snack); блюда (dishes, the galley's): каша `porridge`, рагу `hash`, бульон `broth`, жаркое `roast`, похлёбка `stew` → Crew
- холодильник, торговый автомат / fridge, vending machine → debris `fridge` (`Content.Loot`), salvage `salvageVending` (`Vessels.Salvage`) → Debris
- спокойствие, глубокий вдох, работа / calm, deep, work (a food's rates) → `Content.Rates`, `RatesOf` → Crew
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
- камбуз / galley → machine `galley` (the dishes, paid by hand, no power) → Crafting
- формовщик / former → machine `former` → Crafting; circuit assembler **≠** → `assembler`

## Crew and tools

- герой / hero → `Hero` (sim/Crew `State.cs`; client f/crew `Hero.cs`) → Crew
- шлем / helmet → `Content.Helmet`, slot `head`: `helmet`, `heavyHelmet`, `heatedHelmet` (утеплённый, the ice belt), `heatHelmet` (жаростойкий); each adds degrees to `LowOf`, `HighOf` (gear-roles); drawn bolted still in f/crew `Figure`, `BodyFit` → Crew, Items
- джетпак / jetpack → item `jetpack`, `Thrust`, `c/Jetpack.cs` → Crew
- гарпун / harpoon → item `harpoon`, `c/Harpoon.cs`, f/crew `HarpoonShot`, `HarpoonLine`; `Hook`, `Wind` → Debris
- топор / fire axe → item `fireAxe`, f/crew `AxeSwing`, `HeldLook.FireAxe` → Crew
- движение удара / swing motion **≠** → `SwingMotion` (keys `MotionKey`), a weapon's `Stroke.Motions`, played as `MotionPlay` with `Overhand`, `Jab` (f/crew `MotionPlay.cs`), its `MotionKind` in gc/`SwingMotions.cs`; the offset the hero carries `AxeSwing.Aim` → Crew
- сектор удара / aim sector **≠** → `Sector`, `Sectors` (up, straight, down, under) in gc/`SwingMotions.cs` → Crew
- метка, пинг / ping → `Ping`, `PingSeen`, `PingView`, `MarkerIcon.Ping` (f/crew), a tap of H (crew-pings) → Crew
- жест, эмоция, круговое меню / gesture, emote, wheel **≠** → `Gesture` (`Point`, `Wave`, `Yes`, `No`), `GestureMotions` (gc), `Gestures`, `GesturePlay`, `Wheel<T>`, `WheelView` (f/crew), bind `Gesture` (H), sent as `Handwork` (gestures-for-words) → Crew
- режим дрона, сканер / drone mode, scanner → `DroneMode` (`Lamp`, `Scan`), `DroneModes`, the same `Wheel<T>` (f/crew), bind `Drone` (F), sent as `Seen.Drone`; the scanner opened by node `scanner` (c/`LightDrone.ScannerMode`, light-drone) → Crew, Research
- кисть руки, перчатка / hand, glove → `Rig.Wrist`, `hero_hand_glove`, `hero_hand_skin`, `HeroLooks.OutfitGloved`, frames in `GestureMotions` (hand-at-wrist) → Crew
- бур / drill → item `drill`, `c/Weapons.cs`, f/crew `DrillPace`, `Motor` → Crew
- трос / tether → f/crew `Tether` → Crew; подтягивание по тросу / haul → `Hero.Haul`, `Hero.Wraps` (tether-haul)
- скафандр / suit → a helmet (`Content.Helmet`, slot `head`, `Tank`) + body `suit`, `regenSuit` or `heatSuit` (`Content.Suit`, slot `suit`; its mode `SuitMode`, `Hero.SuitOn`), `Hero.Air`, `CapacityOf`; `SuitGauge`, `Suits` (colours), `Danger` (f/crew) → Crew, Items
- голый, без скафандра / bare → no helmet: `Crew.Lungs`; no body: `Temperature.BareLow` → Crew
- внешность, облик, редактор персонажа / look → `Look`, `ChooseLook`, `LookOf`, `HeroLooks`, `LookEditor` (f/crew) → Players, Crew
- роба, одежда без скафандра, тёплая одежда / outfit, clothes → `Look.Outfit`, `HeroLooks.Outfits` (0–3 robes, 4–14 clothes), pictures `hero_outfit_<n>_<part>` and `_skin` under them, painted raw (outfits-painted, clothes-painted) → Crew
- скин, внешний вид (вкладка) / skin, Appearance (tab) → `Skin`, `Skins` (kinds `head`, `suit`, `cape`, `effect`; sets such as `brassHelmet` + `brassSuit`), `Dressed`, `Suits.DressedAs`, `AppearanceView`, `SkinSlot` (f/crew), `GearTabs` (f/items); кукла (снаряжения) / doll → `HeroDoll` (f/crew), `DollLayout`, `DollSpot`, `DollLeads` (game/shared); `ChooseSkins`, `SkinsOf`, `PeerSkins`; `Owned` (game/platform) → Players, Crew; эффект оружия, «Пламя», «Разлом» / weapon effect, Fire, Rift → `BladeFx`, `FireFx`, `RiftFx` (f/crew), `SkinEffects` (gc)
- плащ / cape → `cape` (`Content.Cape`, slot `cape`, `Guard`, `Warmth`), `Hero.Spared`, `ICrewOps.Blow`; `Figure.CapeWorn`, `Cape`, `CapeView` (f/crew); skins `tarpCape`, `blanketCape`, `woolCape` → Crew, Items
- световой дрон, спутник, связать дрона / light drone, Companion, pair → item `lightDrone` («Спутник»), f/crew `LightDrone`, `DroneSwarm` (drone-threads); E on the core sends `PairDrone(Core)` (`PairProblem`; `CoreLost` pairs all), `Drones`, `HasDrone`, `Pair` (Players): the bag rows open once paired (drone-companion) → Crew, Players, Items
- цвет спутника, соли; буксир спутника / drone colour, salts; the tug → `Salts` (game/shared), `Suits.All` (drone-colour); `DroneTow`, `DroneIntent.Free` (f/crew, drone-tow) → Crew
- ядро, инопланетное ядро / alien core → `Wreck.CorePath`, `Hero.CoreTouch`, f/crew `CoreGlow`, f/grid `CoreScorch`, `CoreScorchView` (drone-companion) → Crew, Grid
- встреча, видение / the meeting, vision → `Hero.Meets`, `Crew.MeetingTicks`, `Spared`; f/crew `Meeting`, `MeetingView`, `HeroView.Meeting`, `CutsceneView` (drone-meeting) → Crew
- проекция дрона, голограмма / drone projection, hologram window → `HoloWindow`, `HoloPen`, `HoloSway` (game/shared), `DroneFrame` (its ember corners: `DroneWindows`, `CornerClock`, drone-windows), `DroneFlurry`, `SheetHoloLook`, `PlanPoint` (f/crew), sent as `Seen.Projects` (drone-frame) → Crew, Items
- формы дрона, урок, научился / the drone's forms, lesson, learned → c/`Lessons.cs` (`Lesson`, `Deed`, `Near`), sim/Lessons (`Teacher`), `Knowledge.Did` (drone-lessons); фонарь `droneLamp`, скан `scanner` (прицел, scan-form), тревоги `droneAlarms` → Lessons, Knowledge
- мысли, настроения спутника / the drone's thoughts, moods → f/crew `DroneThoughts`, `DroneThought`, c/`Lessons.Wonder` (drone-thoughts) → Crew, Lessons
- видение формы / form vision → sends `Envision` (`Crew.VisionTicks`, `Hero.Meets`); f/crew `FormVision`, `FormVisionView`, `HeroView.Vision`, `FormsOpen`; f/research `LessonStage.Vision` (form-vision) → Crew, Research
- скан находки, изучения / find scan, the research key → f/research `FindScan`, `FindScanView` (find-scan), `TabletKey` → Research
- значки нитями / thread icons → game/shared `ThreadIcon`, `IconInk` (thread-icons) → UI
- смерть, тело / death, body → `Bodies`, `BodyView` (0061) → Crew, Items
- трекер, браслет, розыск / tracker, wanted → `Hero.Wanted`, `Need`, `Coming`, `Signal`, `SignalOf`, `ICrewOps.Loot`, c/`Wanted.cs`; начальный пояс / starting belt → `Wanted.Belt`, `Wanted.Drain`; f/crew `TrackerBlink` → Crew
- ловец / catcher → species `catcher` (`Lifetime.Hunter`, `Hunt`, `Flyer`, `Purse`), `Bounty`, `HunterOf`; drawn as a hero, f/crew `HunterView` → Creatures; телепорт / blink → `Blink`, залп / salvo → `Salvo` (`shell`), стрельба на бегу → `Gunfire`, лазер / laser → `laser`, `Shoot.Through`, `Arc.Burns`, `FightOf`, `MarksOf` → Creatures; ракета, захват / missile, lock → `Hunt.Missile` (`missile`, `Homing`, `Shot.Heading`, `Launch`, `LaunchLeft`), c/`ShotLooks.Missile`, f/crew `CatcherFx.LockOf` → Creatures

## World

- обломок / debris piece **≠** → `Piece`, `Hit`, `Line` (sim/Debris); a wreck is debris of kind wreckage → Debris
- сеть, сеть для обломков / debris net → machine `net`, trait `Catches`, `Collect`, `NetEntry`, `DebrisModule.Held` (sim/Debris `Nets.cs`) → Debris
- метеор / meteor → `Meteor`, `MeteorHit`, `Meteors.Shower` → Hazards
- вспышка, магнитная буря / magnetic flare → `MagneticFlares.Flare`, `FlareNow`, `FlareComing` (sim/Hazards `Flares.cs`) → Hazards
- сломан, поломка / broken → `IPowerQueries.IsBroken`, `IPowerOps.Break` → Power
- ремонт, починить / repair → `Repair`, `RepairProgress`, `RepairRules` (sim/Building `Repairs.cs`) → Building
- планета / planet → `PlanetType` (`c/PlanetTypes.cs`), `Surface`, `Terrain` (sim/Core) → Core, Building
- тип планеты: выжженная, ледяная / planet type: scorched, icy → `PlanetTypes.Scorched`, `Icy`, `PlanetType.OreWeights`; its look → `PlanetLooks` (gc) → Core
- набор руд / a planet's ores → `Content.Landing.Ores`, `Starting`, `Varying`, `LandingModule.OresOf`, `Surface.Ores` → Landing, Core
- фон, задний план, параллакс / backdrop, the land behind → `PlanetSky` (r/, the sky; солнце / sun by the hour, `Daylight.Sun`), размытие / out of focus `FocusView`, `PlanetLooks.Look.Far` (moving-sun), `BackdropView`, `BackdropPlan` (f/backdrop, the land), `BackdropLooks` (gc) → client only
- сутки, день, ночь, время суток / day, night, hour → `Sky.Cycle` (`c/Sky.cs`), `SkyNow`, `SetTime` (sim/Sky); fades `Daylight` (gc) → Sky
- буря, пыльная буря, ливень, непогода / storm, dust storm, rain storm, weather **≠** → a planet's stretch: `Stretches.DustStorm`, `RainStorm` (`"dustStorm"`, `"rainStorm"`), `SkyNow.Pace`, `ICrewQueries.PaceAt`; песок / grit `Stretch.Grit`, `GritAt` (mild-surface); looks `StretchLooks.Weather`, f/sky `StormView` → Sky, Crew
- полоса космоса, ледяной пояс, дрейф контрабанды / stretch of space, ice belt, contraband drift → `Stretches.IceBelt`, `ContrabandDrift`, `StretchShown` (gc/`StretchLooks`); its far picture r/`StretchBackdrop`, дымка глубины / depth haze `StretchLooks.Deep` (moving-sun) → Sky
- молния, удар молнии, гроза / lightning, bolt, strike → `Stretch.Bolt` (`BoltRule`, `Again`: the rain's every 12–18 s, c/`Stretches.cs`), `Strike`, `BoltNow` (sim/Hazards `Bolts.cs`), `Harm.Lightning`; f/hazards `BoltView` → Hazards, Crew
- ночной холод, мороз / the night's chill → `Cycle.Chill` (degrees), the screen's frost `Report.Cold` → Crew
- температура, градусы, предел / temperature, degrees, a hero's limit → `TemperatureAt`, `LowOf`, `Felt`, `HighOf`, `Heat` (sim/Crew), `Temperature`, `Kind.Temperatures`, `PlanetType.Air`, `ItemSpec.Low`, `High`, `Cape.Warmth` (c/) → Crew
- температура тела, замёрз, перегрелся / body temperature, frozen, overheated → the client's `BodyHeat` (f/crew; `Temperature.Normal`, `ColdDeath`, `HeatDeath`, `Drift`), its slowness `SpeedOf`, the death `Succumb` (sim/Crew), the suit frosted or scorched `SuitFx` (body-temperature) → Crew
- копать / dig → `Dig`, `Crack` (sim/Building `Digging.cs`) → Building
- жила, руда / vein, ore → `Vein` (`c/Veins.cs`), materials `copperVein`, `iceVein`, `ironVein`, `sulfurVein` (сера, item `sulfur`; `Material.In`), drawn in `Terrain` → Core
- залежь, рудный карман / ore pocket → `Pockets` (`c/Veins.cs`), `Terrain.PaintPockets` → Core
- подрывной заряд, взрывчатка, бомба / blasting charge → item and gun `blastCharge` (`Shots.Thrown`, `Shots.BlastCharge`), `Blasting.Charge` (c/`Blasting.cs`); its burst `Wave` (sim/Core), `IBuildingOps.Blast` → Creatures, Building
- тайник, клад / deep cache **≠** the spire's cache → growths `frostCache`, `bloomCache`, `amberCache`, `emberCache` (`Flora.Cave`, in the rock) → Building
- опал / opal → item `opal`, a cache's yield, only sold (`Market`) → Items, Trade
- рудная зона / lode → `PlanetType.LodeWave`, `LodeOver`, `CaveKind.Lode` (crystals by the veins) → Core
- расщелина, разлом / chasm, rift → `Ground.Chasms`, `ChasmDeep`, `RiftDeep` (one chasm down to the band over the core), `Terrain.Landmarks` → Core
- слой планеты, пласт / depth band, stratum **≠** a grid layer → `PlanetType.Bands`, `Terrain.BandOf` → Core
- равнина, нагорье / plain, highland → `Ground.RegionWave`, `RegionLeast`, `RegionLift` → Core
- мох / moss **≠** → `Grass` (f/grid `Grass.cs`, the living edge); `Sound.StepMoss` → Grid (client only)
- растение / plant → `Plant` (sim/Building `Flora.cs`), `c/Flora.cs` → Building
- существо / creature **≠** → `Creature`, `CreaturesModule` → Creatures
- лопер / loper → `Creatures.Loper` = `"loper"` → Creatures
- сентри / sentry → `Creatures.Sentry` = `"sentry"`: a `Clinger` on a wreck, `Guard`, `Return`, `CutTether`, `Pounce`; dev `--sentry` (`=fight`: a line sawn, struck, its wreck broken; `=crawl`) → Creatures
- разборщик, болванчик, стая, гнездо / breaker, pack, nest → `Creatures.Breaker` = `"breaker"` (`Flyer`, `Swarm`, `Pounce`), c/`Nest`, `Nests.cs`, f/creatures `WorkingNests`, `BreakerBodies` (companion-fights, breakers-look) → Creatures
- клубок / knot (a breaker's kind) → `KnotMoves`, `CreatureArt.BreakerKnot`, `CreatureLooks.Breakers` (breakers-look) → Creatures
- бросок оружия, умение спутника / throw, the companion's ability → c/`Abilities` (`Throw`, `ThrowOf`), f/crew `WeaponThrow`, `ThrownDrawing` (the catch), sent as `Handwork.Throw`, `Haul` (companion-fights) → Crew
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
- корабль, корабль-находка / vessel **≠** → `Vessel`, `Shipyard` (sim/Core `Vessels/`), `Content.Vessels`; a world of its own (vessel-worlds) → Places
- лайнер, танкер / liner, tanker → the big classes (big-vessels), `VesselLayout.Liner`/`Tanker`, `Shipyard.Blocks`/`Liner`/`Tanker`/`Fit`; высокий отсек / a room several decks tall → `Vessel.Compartment.Decks`; бассейн, ресторан, трюм, ангар / pool, restaurant, hold, hangar → `VesselRole`; шаттлы в ангаре / hangar shuttles GULL, MULE, BEETLE, WHALE, HERON; цистерна, топливная колонна / tank, fuel column → `vesselCapsule`, `vesselTankTall`, `vesselFuelColumn*`; забыть корабль, убрать маршрут / forget a contact, clear the route → `Forget`, `ClearCourse` → Places
- заброшенный корабль, его вид / derelict, its looks → the client's from class and seed: `VesselDress` (обшивка / skin, ливрея / livery, имя и код / name and code), `VesselLights` (аварийные лампы / emergency lamps, умирающая лампа / the dying lamp, точки над дверями / door dots, заклиненная дверь / jammed door), `VesselSigns` (следы заброшенности / signs: иней, искрящий кабель, копоть, упавшая панель, перевёрнутый ящик, газ) → Places, Light, Grid
- скрытые отсеки, тьма внутри, комната открылась / hidden rooms, unseen, seen → the client's f/unseen `Sight` (seen, dark), `UnseenView` (hidden-rooms) → Unseen
- детали снаружи корабля / outside parts → `Vessels.Outside`: `vesselNozzle` (сопло), `vesselRcs` (маневровый блок), `vesselMast` (антенна, маячок / beacon), `vesselDish` (тарелка), `vesselSolar`, `vesselTank`, `vesselRadiator`, `vesselCollar` (стыковочный узел), `vesselTruss` and `vesselPylon` (килевая ферма), `vesselContainer` (контейнер под килем), `vesselBoom` (сенсорная штанга) → Places
- обломок, разбитый корабль / wreck → `VesselWreck` (sim/Core `Vessels/Wreck.cs`), `Shipyard.Build`, `Hulk`, c/`Wrecks.cs` (`WreckPercent`), `Built`; событие / event (взрыв, пожар, обледенение, удар астероида, разлом) → `WreckEvent`, `WreckKind` (`Explosion`, `Fire`, `Icing`, `Asteroid`, `Break`); цепочка / chain, группа, давнее и свежее / group, old and fresh → `WreckStep`, `Find(Class, Wreck, Wrecked)`, `Shipyard.IsWreck` (the radar's mark), dev `--wrecked` and the panel's abandoned/wrecked button; комната прибытия / arrival room; камень / stone → `pileRock`; вид и движение / looks and motion → f/wreck `WreckView`, `WreckLifeView` → Places
- мебель корабля / a vessel's furniture → `Vessels.Furniture` (койки, шкафчик, стол, баки, пульт, кресло, блок механизмов, кабельная стойка, `vesselCargo`); лампа корабля / vessel lamp → `vesselLamp` → Places
- находка на корабле / salvage aboard → `Vessels.Salvage`, `VesselSalvage`, a pile (`salvageTanks`, …) copied from a debris piece, five times its loot → Places, Building
- главный двигатель, пульт корабля / main engine, console → `salvageMainEngine` (one a lower deck, by the stern), `salvageConsole` (the bridge) → Places
- отсек, комната корабля / a vessel's room → `Vessel.Compartment`, placed by `VesselRoomRule`; doors between, hatches down the well → Places
- разбитый корабль / wrecked vessel → a vessel after `Shipyard.Damage` (`VesselCause.Breach`); not the starting wreck, nor wreckage → Places
- контакт, прыжок / contact, jump → `Contact`, `Find`, `Jump` → Places

## Game

- сохранение / save → `World.Save()`, `World.Load` (sim/Core `Sim.cs`), `Migrations` (sim/Registry), `game/platform/logic/Saves.cs`, `WorldIndex` → Core
- хост / host, co-op → net/Net `Host`, `Client`, `Session`; game/menu `Together`; `game/Main.Sessions.cs` → net
- справочник / Handbook → f/handbook `HandbookPanel`, `game/Content/logic/Handbook.cs` → UI
- подсказка / tooltip → `game/shared/logic/Tip.cs`, `PointerTip` → UI
- показатели, циферблаты, полоса воздуха / vitals, dials, air bar → `Vitals`, `RingLight`, `DialNumber`, `AirRow`, `AirMotion` (game/shared), `EnergyBar` (f/energy), `SuitGauge`, `MealSlots` (f/crew) (hud-dials) → UI
- термометр, «Ртуть» / thermometer **≠** the temperature dial → `Thermometer`, `Thermometers` (f/crew), the body to a tenth → UI
- дев-аргумент, демо / dev argument, demo → `-- --<name>`: one `DevMode` each in `game/dev/logic/DevModes.cs`, wired in `game/dev/Main.Dev.cs` → Dev tools

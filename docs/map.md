# Map

Where each mechanic's code lives. A sim module is `sim/X/` (class `XModule`, queries `IXQueries`, ops `IXOps`);
its client is `game/features/x/`. Each has a `README.md`: read its `## Files`, then only the section you need.
Game words → code names: [glossary.md](glossary.md). ADRs: [decisions/index.md](decisions/index.md).
Below, `f/x` = `game/features/x`, `r/` = `game/render`, `gc/` = `game/Content` (client looks), `c/` = `sim/Content`.

## Domains

**Ship building**
- Grid — sim/Grid · f/grid: `Grid.cs` GridModule, `Placement`, `SetDoor`, trait queries, `MachinesWith`; c/`Structures.cs` (`Structure` codes, `StructureSpec`, `Trait`, 0112); c/`Machines.cs` (`MachineSpec`, `MachineTrait`, 0113); `Doorways.cs`; `ChangeLog.cs` `GridLayers`; `GridView.cs`, `DoorSlides.cs`
- Building — sim/Building · f/building: `Building.cs` BuildingModule, `PlacePlan`, `IBuildingOps`; `Rules.cs`; `Costs.cs`; `Repairs.cs` (`Repair`); `Bursts.cs` (`Burst`: what a charge or meteor breaks, unified-damage), sim/Core `Wave.cs` (its spread), c/`Damage.cs`; `BuildMenu.cs`, `PlanHand.cs`, `Tools.cs`, `Drag.cs`, `WallStats.cs` (a wall card's leak); the hints: `Hints.cs`, `HintWing.cs`, `HintChip.cs`, game/shared `KeyCaps.cs` (presses drawn), `logic/KeyHint.cs`, `logic/HintFlow.cs`; c/`Builds.cs`, c/`Menu.cs` (`MenuSlot`, `MenuTabs`: the menu from content, 0114)
- Rooms — sim/Rooms: `Rooms.cs` RoomsModule (rooms from walls and doors). Drawn: r/`ShipTiles.cs`, `BlueprintArt.cs`, `DoorShape.cs`

**Air** — Atmosphere — sim/Atmosphere · f/atmosphere: `Atmosphere.cs` AtmosphereModule, `LoadCanister`; `Air.cs` (vaporizers, draws); `Order.cs`; `AirView.cs`, `TankLooks.cs`, `VentLight.cs`; c/`Air.cs`

**Power** — Power — sim/Power · f/power: `Power.cs` PowerModule; `PowerNodes.cs`; `PowerView.cs`, `PowerHands.cs`, `Wiring.cs`, `StoreGauge.cs`; c/`Power.cs`, `Charge.cs`

**Pipes** — Pipes — sim/Pipes · f/pipes: `Pipes.cs` PipesModule, `Valve`, `SetValve`; `PipeNetworks.cs`; `PipesView.cs`, `PipeHands.cs`; c/`Piping.cs`, `Fluids.cs`
**Water** — Water — sim/Water · f/water: `Water.cs` WaterModule, `PourWater`, the cisterns' water and the electrolysers; `Water.En.cs`, `WaterLooks.cs`, `WaterTip.cs`; c/`Water.cs`

**Items and crafting**
- Items — sim/Items · f/items: `Items.cs` ItemsModule; `Stacks.cs`; `Floor.cs`; `Modules.cs` (machine modules, `Fit`); `ToolStats.cs`; `InventoryPanel.cs`, `Hotbar.cs`, `Click.cs`, `Equipment.cs`; f/energy: `EnergyBar.cs` (the wrist's energy tab); c/`Items.cs`
- Crafting — sim/Crafting · f/crafting: `Crafting.cs` CraftingModule (stations); `StationView.cs`, `FabricatorPanel.cs`, `BenchView.cs`; c/`Crafts.cs`, `Stations.cs`, `Machines.cs`
- Furnace — sim/Furnace · f/furnace: `Furnace.cs` FurnaceModule; `FurnaceView.cs`, `Flames.cs`; c/`Furnace.cs`
- Drones — sim/Logistics · f/logistics: `Logistics.cs` LogisticsModule; `Jobs.cs`; `Flight.cs`; `Salvage/Salvage.cs` (salvage drones); `LogisticsView.cs`, `Served.cs`, `Puffs.cs`, `BayInk.cs` and `SalvageInk.cs` (generated); c/`Logistics.cs`

**Research and trade** — sim/Research · f/research: `Research.cs` ResearchModule (the rule that grants, the market's rings); `Understanding.cs` (how many are enough); `Blueprints.cs`; `Prices.cs`; `ResearchTablet.cs`, `BoardPage.cs` (the board; its slots, threads and pile in `logic/Board*.cs`), `TabletKey.cs`, `FindFlashView.cs` (a find's flash along the edge), `MarketPage.cs`, `StripParts.cs`, `logic/Cipher.cs`, `logic/MarketGoods.cs`, `logic/MarketReveal.cs`, `logic/TechLinks.cs`, `StationScreens.cs`; c/`Research.cs` (the market's rings, what is enough). sim/Trade: `Trade.cs` TradeModule (selling); c/`Market.cs` (worth, worked out from recipes). sim/Knowledge: `Knowledge.cs` (what the crew knows, credits, what it obtained and sold)

**Crew and hero movement**
- Crew — sim/Crew · f/crew: `Crew.cs` CrewModule (where heroes are, breathing); `Hero.cs` (movement, ladders, steps), `Motion.cs`, `Pose.cs`, `Rig.cs`, `Aim.cs`, `ToolEdge.cs` (what a running tool hits), `AxeSwing.cs`, `MotionPlay.cs` (a stroke's motion as played: `Overhand`, `Jab`), `HarpoonShot.cs`; gc/`SwingMotions.cs` (motions and aim sectors, data); c/`Hero.cs`, `Weapons.cs`
- Players — sim/Players: `Players.cs` PlayersModule (what is the player's, not a surface's)

**Creatures** — sim/Creatures · f/creatures: `Creatures.cs` CreaturesModule; `Live.cs`, `Rules.cs`, `Drives.cs`, `Lunge.cs`, `Walker.cs`, `Clinger.cs`, `Guard.cs`, `Ward.cs`, `Pounce.cs`, `Spawn.cs`, `Bounty.cs`, `Hunt.cs`, `Flyer.cs` (the catcher), `Paths.cs` (its path search), `Shots.cs` (shots: `Fire`, the flight, where the charge bursts); sim/Core `ShotFlight.cs`; c/`Blasting.cs`; `CreaturesView.cs`, `ShotsView.cs`, `TickBodies.cs`, `CreatureBody.cs`, `CreaturePose.cs`; f/crew `HeroView.Guns.cs`, `GunHand.cs`, `HunterView.cs`; game/shared `ShotStreaks.cs`, `ShotEnds.cs`, `ShotDraws.cs`, `PixelBursts.cs`; c/`Creatures.cs`, `Shots.cs`; gc/`CreatureLooks.cs`

**Planets and terrain** — sim/Core `Terrain.cs`, `Lakes.cs`, `Surface.cs`; sim/Grid `Grown.cs`; sim/Building `Digging.cs`, `Flora.cs` · f/flora: `FloraView.cs`, `CaveView.cs`, `logic/Topple.cs`, `logic/Shedding.cs`; plants stirred by bodies: r/`logic/Stirs.cs`, `Bend.cs`; f/grid `Grass.cs`, `LavaView.cs`, `Strata.cs` (stone by band); the fog at a planet's sides: f/fog `FogView.cs`, game/shared `FogEdges.cs`, `FogShare.cs`, f/crew `FogMove.cs`, gc/`FogLooks.cs`; c/`PlanetTypes.cs`, `Ground.cs`, `Flora.cs`, `Veins.cs`, `Lava.cs`; r/`GroundArt.cs`, `PlanetSky.cs` (the sky); the land behind, composed by the seed: f/backdrop `BackdropView.cs`, `logic/BackdropPlan.cs`, `logic/GroundLine.cs`, gc/`BackdropLooks.cs`

**Liquids** — sim/Liquids: `Liquids.cs` LiquidsModule (water in a surface's cells); `Flow.cs` (the flow, awake chunks); `Pressure.cs` (bodies levelled); `Puddles.cs` (runs levelled, thin ones dried); `Saved.cs`; c/`Liquids.cs`; drawn in a map by `Layout.WithLiquids`, `Legend.LiquidOf`; lakes grown by sim/Core `Lakes.cs` · f/liquids: `LiquidsView.cs` (drawn over the heroes, a stream heard), `WaterLook.cs` (its shader)

**Day and night** — sim/Sky: `Sky.cs` SkyModule, `SetTime`, `ISkyQueries.Now`; the chill in sim/Crew `ColdAt`; gc/`Daylight.cs`, `PlanetLooks.cs` (dusk and night colours); r/`Lit.cs` (`sky` uniform), `Stars.cs` (`Tint`, also the land's veil in f/backdrop); c/`Sky.cs`

**Ruins** — sim/Building `Flora.cs` (`Relic`, ruin pieces as plants) · f/ruins: `HomesView.cs`, `SpiresView.cs`, `RelicSparks.cs`; c/`Ruins.cs`; gc/`HomesLooks.cs`, `RelicLooks.cs`

**Debris and wrecks**
- Debris — sim/Debris · f/debris: `Debris.cs` DebrisModule, `Hit`; `Drift.cs` (zones, spawns); `Nets.cs` (the debris net: `Collect`, `NetEntry`); `DebrisView.cs`, `BreakView.cs`, `Finds.cs`; c/`Debris.cs`, `Wreck.cs`, `Wreckage.cs`
- Scanner — f/scanner: `Scan.cs` (`Readout`: a wreck's hit points left under the pointer), `Phosphor.cs` (the picked readout's digits), `ScannerView.cs`; game/shared `WreckAim.cs` (the box a wreck is pointed at by), `ScannerInk.cs` (generated: the crest on each helmet); c/`Suit.cs` `Helmet.Scanner`
- Hazards — sim/Hazards · f/hazards: `Hazards.cs` HazardsModule (the shower falling, `Incoming`), `MeteorHit`, struck through Building's `Burst`; `Flares.cs` (magnetic flares, `FlareNow`); `MeteorView.cs`, `logic/MeteorWarn.cs` (the weak alarm at a crack); c/`Meteors.cs`, c/`MagneticFlares.cs`
- Landing — sim/Landing: `Landing.cs` LandingModule, `SetPlatform` (the launch platform opened with power), `Board`, `Alight` (the drop pod's seats), `Launch` (its descent and return, `Flights`, `Owners`); `Content.Landing`; client game/features/landing: `PodSeats.cs`, `LaunchKey.cs` (Descend / Return), `FlightOverlay.cs` (the white-out); the seated hero: f/crew `SeatedHeroes.cs`
- Places — sim/Places: `Places.cs` PlacesModule, `Find` (a contact), the radar's own contacts on a timer (c/`Contacts.cs`, drawn by f/computer `RadarScreen.cs`, `logic/RadarOrbit.cs`), `Jump` (a vessel written beside the ship, the last wiped); the vessel itself sim/Core `Vessels/`: `Shipyard.cs` (describe, stamp, damage), `Vessel.cs`; c/`Vessels.cs` (classes, interiors, loot); f/places (its text only); dev `--vessel` (`VesselScenario.cs`)

**Light** — f/light: `LightView.cs`, `Lightmap.cs`, `Sources.cs`, `StarField.cs`; f/effects: `EffectsView.cs`, `Impacts.cs`; r/`Lit.cs`, `Luminous.cs`, `Flares.cs`; gc/`Light.cs`, `Fx.cs`

**Sound** — f/sound: `SoundView.cs`, `Hearing.cs`, `Motors.cs`, `GroundSounds.cs`; gc/`Sound.cs` (every cue); game/shared `Cues.cs`; files in `game/audio`

**Ship computer** — f/computer: `ComputerPanel.cs`, `RadarScreen.cs`, `ComputerScreens.cs`, `Readout.cs` (programs brought by modules on its network); c/`Programs.cs`

**UI and tablet** — game/shared: `Tablet.cs`, `TabletEdge.cs`, `EdgeKey.cs` (a hologram key), `logic/EdgeRow.cs`, `logic/TabletPeek.cs`, `holo.gdshader`, `CreditHolo.cs` (the credits on the edge), `TabletLayout.cs`, `CreditLine.cs`, `MachineFrame.cs` and `MachineHolo.cs` (a machine's window and its hologram, `logic/HoloShape.cs`, `logic/HoloMotion.cs`, gc/`HoloLook.cs`), `Ui.cs` (`Store`), `StoreWatch.cs` (a node hears a store while in the tree), `Slots.cs`, `ItemCard.cs`, `Brush.cs`; f/handbook: `HandbookPanel.cs`; game/menu: `TitleScreen.cs`, `GameMenu.cs`, `SettingsView.cs` (Settings), `BindRows.cs` (the keys); game/shared `logic/Binds.cs`, `BindKeys.cs`, `logic/TextMatch.cs` (what a typed search names); game/platform: `Settings.cs`; game/overlays; r/`Style.cs`

**Net and co-op** — net/Net: `Host.cs`, `Client.cs`, `Session.cs`, `Playout.cs`; net/Tests `Desync.cs`; game/net: `EnetLink.cs`, `RelayLink.cs`; game/`Main.Sessions.cs` (hosting, joining, worlds on disk, the title); sim/Core: `Sim.cs` World, `Module.cs` Command, `Rng.cs`; sim/Registry: `Registry.cs`, `Migrations.cs`; game/platform: `Saves.cs`

**Dev tools** — game/dev: `DevModes.cs` (every `-- --arg`, its doc), `DevArgs.cs`, `Main.Dev.cs` (the tools that are not scenarios), `DevScenario.cs`, `DevScenarios.cs`, `Testbed.cs`, `Autopilot.cs`, `*Demo.cs`; game/dev/demos: a class per demo argument, `[Dev("--name", …)]` (its README: adding one); f/dev-panel: `DevPanel.cs`, `CueNames.cs`; sim/Analyzers (determinism rules); sim/Tests (`Scenario.cs`); `tools/` (art pipelines, node)

## Conventions

- **Content by id**: `grep -rn '"<id>"' sim/Content`. Systems ask a machine's `MachineTrait` (0113) or a structure's `Trait` (0112), never its kind. Items `Items.cs`, machines `Machines.cs`, materials `Materials.cs`,
  creatures `Creatures.cs`, planet types `PlanetTypes.cs`, builds `Builds.cs`, recipes `Crafts.cs`, stations `Stations.cs`.
  ASCII map characters: `Legend.cs`. A content file is named after its module or catalogue.
- **Art**: `game/art/hd/machine_<kind>.png`, `<kind>_work_<n>.png` (a station at work), `items/<itemId>.png` (none for a tool, 2026-10-27-tool-icons),
  `debris/<look>_<nn>.png`, `creatures/<design>/<part>.png`, `planet/`, `caves/`, `homes/<piece>_<n>.png`;
  `_glow`/`_lit` suffixes for light. Drawn at four times the world's pixels (`PixelArt.Picture(name)`), and
  reduced to them for a size or shape (`HiRes.Logical`); the only pictures (2026-11-03-pixel-art-removed).
- **i18n** (core `game/i18n/logic/En.cs`; a feature's strings are in its `logic/<Name>.En.cs`, Russian in `.Ru.cs`): `item.<itemId>` (+ `.kind`, `.key`, `.hint`), `tool.<build>` (+ `.about`),
  `material.<id>`, refusals `<moduleId>.<why>`, UI by area (`menu.`, `use.`, `handbook.`, `build.`). Read with `T(key)`.
- **Tests**: `sim/Tests/<Module>Tests.cs` or `<Rule>Tests.cs`, helpers `<Module>Testing.cs`; client `game.tests/<Class>Tests.cs`
  (Godot-free client code lives in its folder's `logic/`; `Game.Tests.csproj` compiles every `logic/` by glob); `net/Tests/<Topic>Tests.cs`. Docs guard: `game.tests/DocsGuardTests.cs`.
- **Dev args**: a demo's on its scenario class in `game/dev/demos/` (`[Dev("--name"`), every other in `game/dev/logic/DevModes.cs` (`DevModesTests` keeps both exact); `grep -rn '"--<name>' game` for the wiring,
  `grep -rn '<c>-- --<name>' game` for its class. `game/Main.cs` keeps startup and the frame loop; `game/SurfaceViews.cs` lists the views of a surface, in draw order.

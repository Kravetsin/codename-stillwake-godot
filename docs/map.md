# Map

Where each mechanic's code lives. A sim module is `sim/X/` (class `XModule`, queries `IXQueries`, ops `IXOps`);
its client is `game/features/x/`. Each has a `README.md`: read its `## Files`, then only the section you need.
Game words → code names: [glossary.md](glossary.md). ADRs: [decisions/index.md](decisions/index.md).
Below, `f/x` = `game/features/x`, `r/` = `game/render`, `gc/` = `game/Content` (client looks), `c/` = `sim/Content`.

## Domains

**Ship building**
- Grid — sim/Grid · f/grid: `Grid.cs` GridModule, `Placement`, `SetDoor`, trait queries, `MachinesWith`; c/`Structures.cs` (`Structure` codes, `StructureSpec`, `Trait`, 0112); c/`Machines.cs` (`MachineSpec`, `MachineTrait`, 0113); `Doorways.cs`; `ChangeLog.cs` `GridLayers`; `GridView.cs`, `DoorSlides.cs`
- Building — sim/Building · f/building: `Building.cs` BuildingModule, `PlacePlan`, `IBuildingOps`; `Rules.cs`; `Costs.cs`; `BuildMenu.cs`, `PlanHand.cs`, `Tools.cs`, `Drag.cs`; c/`Builds.cs`, c/`Menu.cs` (`MenuSlot`, `MenuTabs`: the menu from content, 0114)
- Rooms — sim/Rooms: `Rooms.cs` RoomsModule (rooms from walls and doors). Drawn: r/`ShipTiles.cs`, `BlueprintArt.cs`, `DoorShape.cs`

**Air** — Atmosphere — sim/Atmosphere · f/atmosphere: `Atmosphere.cs` AtmosphereModule, `LoadCanister`; `Air.cs`; `Order.cs`; `AirView.cs`, `TankLooks.cs`, `VentLight.cs`; c/`Air.cs`

**Power** — Power — sim/Power · f/power: `Power.cs` PowerModule; `PowerNodes.cs`; `PowerView.cs`, `PowerHands.cs`, `Wiring.cs`, `StoreGauge.cs`; c/`Power.cs`, `Charge.cs`

**Pipes** — Pipes — sim/Pipes · f/pipes: `Pipes.cs` PipesModule, `Valve`, `SetValve`; `PipeNetworks.cs`; `PipesView.cs`, `PipeHands.cs`; c/`Piping.cs`

**Items and crafting**
- Items — sim/Items · f/items: `Items.cs` ItemsModule; `Stacks.cs`; `Floor.cs`; `Modules.cs` (machine modules, `Fit`); `ToolStats.cs`; `InventoryPanel.cs`, `Hotbar.cs`, `Click.cs`, `Equipment.cs`; c/`Items.cs`
- Crafting — sim/Crafting · f/crafting: `Crafting.cs` CraftingModule (stations); `StationView.cs`, `FabricatorPanel.cs`, `BenchView.cs`; c/`Crafts.cs`, `Stations.cs`, `Machines.cs`
- Furnace — sim/Furnace · f/furnace: `Furnace.cs` FurnaceModule; `FurnaceView.cs`, `Flames.cs`; c/`Furnace.cs`
- Drones — sim/Logistics · f/logistics: `Logistics.cs` LogisticsModule; `Jobs.cs`; `Flight.cs`; `LogisticsView.cs`, `Served.cs`; c/`Logistics.cs`

**Research** — sim/Research · f/research: `Research.cs` ResearchModule; `Blueprints.cs`; `Analysable.cs`; `AnalyzerScreens.cs`, f/items/`AnalyzerPanel.cs`, `Lessons.cs`. sim/Knowledge: `Knowledge.cs` (what the crew knows)

**Crew and hero movement**
- Crew — sim/Crew · f/crew: `Crew.cs` CrewModule (where heroes are, breathing); `Hero.cs` (movement, ladders, steps), `Motion.cs`, `Pose.cs`, `Rig.cs`, `Aim.cs`, `AxeSwing.cs`, `HarpoonShot.cs`; c/`Hero.cs`, `Weapons.cs`
- Players — sim/Players: `Players.cs` PlayersModule (what is the player's, not a surface's)

**Creatures** — sim/Creatures · f/creatures: `Creatures.cs` CreaturesModule; `Live.cs`, `Rules.cs`, `Drives.cs`, `Lunge.cs`, `Walker.cs`, `Clinger.cs`, `Guard.cs`, `Pounce.cs`, `Spawn.cs`, `Shots.cs` (shots: `Fire`, the flight); sim/Core `ShotFlight.cs`; `CreaturesView.cs`, `ShotsView.cs`, `TickBodies.cs`, `CreatureBody.cs`, `CreaturePose.cs`; f/crew `HeroView.Guns.cs`, `GunHand.cs`; game/shared `ShotStreaks.cs`, `ShotEnds.cs`, `ShotDraws.cs`; c/`Creatures.cs`, `Shots.cs`; gc/`CreatureLooks.cs`

**Planets and terrain** — sim/Core `Terrain.cs`, `Surface.cs`; sim/Grid `Grown.cs`; sim/Building `Digging.cs`, `Flora.cs` · f/flora: `FloraView.cs`, `CaveView.cs`; f/grid `Grass.cs`, `LavaView.cs`; the fog at a planet's sides: f/fog `FogView.cs`, game/shared `FogEdges.cs`, `FogShare.cs`, f/crew `FogMove.cs`, gc/`FogLooks.cs`; c/`PlanetTypes.cs`, `Ground.cs`, `Flora.cs`, `Veins.cs`, `Lava.cs`; r/`GroundArt.cs`, `PlanetSky.cs`

**Ruins** — sim/Building `Flora.cs` (`Relic`, ruin pieces as plants) · f/ruins: `HomesView.cs`, `SpiresView.cs`, `RelicSparks.cs`; c/`Ruins.cs`; gc/`HomesLooks.cs`, `RelicLooks.cs`

**Debris and wrecks**
- Debris — sim/Debris · f/debris: `Debris.cs` DebrisModule, `Hit`; `Drift.cs` (zones, spawns); `DebrisView.cs`, `BreakView.cs`, `Finds.cs`; c/`Debris.cs`, `Wreck.cs`, `Wreckage.cs`
- Hazards — sim/Hazards · f/hazards: `Hazards.cs` HazardsModule (the shower falling, `Incoming`), `MeteorHit`; `MeteorView.cs`; c/`Meteors.cs`
- Landing — sim/Landing: `Landing.cs` LandingModule, `SetPlatform` (the launch platform opened with power), `Board`, `Alight` (the drop pod's seats), `Launch` (its descent and return, `Flights`, `Owners`); `Content.Landing`; client game/features/landing: `PodSeats.cs`, `LaunchKey.cs` (Descend / Return), `FlightOverlay.cs` (the white-out); the seated hero: f/crew `SeatedHeroes.cs`

**Light** — f/light: `LightView.cs`, `Lightmap.cs`, `Sources.cs`, `StarField.cs`; f/effects: `EffectsView.cs`, `Impacts.cs`; r/`Lit.cs`, `Luminous.cs`, `Flares.cs`; gc/`Light.cs`, `Fx.cs`

**Sound** — f/sound: `SoundView.cs`, `Hearing.cs`, `Motors.cs`, `GroundSounds.cs`; gc/`Sound.cs` (every cue); game/shared `Cues.cs`; files in `game/audio`

**Ship computer** — f/computer: `ComputerPanel.cs`, `RadarScreen.cs`, `ComputerScreens.cs`, `Readout.cs` (programs brought by modules on its network); c/`Programs.cs`

**UI and tablet** — game/shared: `Tablet.cs`, `TabletLayout.cs`, `Ui.cs`, `Slots.cs`, `ItemCard.cs`, `Brush.cs`; f/handbook: `HandbookPanel.cs`; game/menu: `TitleScreen.cs`, `GameMenu.cs`; game/overlays; r/`Style.cs`

**Net and co-op** — net/Net: `Host.cs`, `Client.cs`, `Session.cs`, `Playout.cs`; net/Tests `Desync.cs`; game/net: `EnetLink.cs`, `RelayLink.cs`; game/`Main.Sessions.cs` (hosting, joining, worlds on disk, the title); sim/Core: `Sim.cs` World, `Module.cs` Command, `Rng.cs`; sim/Registry: `Registry.cs`, `Migrations.cs`; game/platform: `Saves.cs`

**Dev tools** — game/dev: `DevModes.cs` (every `-- --arg`, its doc), `DevArgs.cs`, `Main.Dev.cs` (the tools that are not scenarios), `DevScenario.cs`, `DevScenarios.cs`, `Testbed.cs`, `Autopilot.cs`, `*Demo.cs`; game/dev/demos: a class per demo argument, `[Dev("--name", …)]` (its README: adding one); f/dev-panel: `DevPanel.cs`, `CueNames.cs`; sim/Analyzers (determinism rules); sim/Tests (`Scenario.cs`); `tools/` (art pipelines, node)

## Conventions

- **Content by id**: `grep -rn '"<id>"' sim/Content`. Systems ask a machine's `MachineTrait` (0113) or a structure's `Trait` (0112), never its kind. Items `Items.cs`, machines `Machines.cs`, materials `Materials.cs`,
  creatures `Creatures.cs`, planet types `PlanetTypes.cs`, builds `Builds.cs`, recipes `Crafts.cs`, stations `Stations.cs`.
  ASCII map characters: `Legend.cs`. A content file is named after its module or catalogue.
- **Art**: `game/art/machine_<kind>.png`, `<kind>_work_<n>.png` (a station at work), `items/<itemId>.png` (none for a tool, 2026-10-27-tool-icons),
  `debris/<look>_<nn>.png`, `creatures/<design>/<part>.png`, `planet/`, `caves/`, `homes/<piece>_<n>.png`;
  `_glow`/`_lit` suffixes for light. Loaded by `PixelArt.Picture(name)` = `res://art/<name>.png`.
- **i18n** (core `game/i18n/logic/En.cs`; a feature's strings are in its `logic/<Name>.En.cs`): `item.<itemId>` (+ `.kind`, `.key`, `.hint`), `tool.<build>` (+ `.about`),
  `material.<id>`, refusals `<moduleId>.<why>`, UI by area (`menu.`, `use.`, `handbook.`, `build.`). Read with `T(key)`.
- **Tests**: `sim/Tests/<Module>Tests.cs` or `<Rule>Tests.cs`, helpers `<Module>Testing.cs`; client `game.tests/<Class>Tests.cs`
  (Godot-free client code lives in its folder's `logic/`; `Game.Tests.csproj` compiles every `logic/` by glob); `net/Tests/<Topic>Tests.cs`. Docs guard: `game.tests/DocsGuardTests.cs`.
- **Dev args**: a demo's on its scenario class in `game/dev/demos/` (`[Dev("--name"`), every other in `game/dev/logic/DevModes.cs` (`DevModesTests` keeps both exact); `grep -rn '"--<name>' game` for the wiring,
  `grep -rn '<c>-- --<name>' game` for its class. `game/Main.cs` keeps startup and the frame loop; `game/SurfaceViews.cs` lists the views of a surface, in draw order.

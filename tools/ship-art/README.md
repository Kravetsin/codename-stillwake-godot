| `engine/engine.gd` (Godot) | `machine_engine`; `items/` `nozzle`, `fuelPump`, `igniter` | none | `godot --headless --script tools/ship-art/engine/engine.gd -- <artDir> [sheet]` | exact 4/4 |
﻿# Ship art painters

These scripts paint the ship's pictures in `game/art`: debris, machines, hero tools, drones, hatches, the tank, the
wrist, the tablet, the farm and item icons. Each writes into `game/art` or an output dir it takes. The planet's painters are in [`tools/planet-art`](../planet-art).

Plain Node unless noted (`fs`, `path`, `zlib`). **Reproduces**, against the art at 85f408a: **exact** вЂ” identical
bytes; **pixel-exact** вЂ” same pixels, other bytes (the PNG encoder was rewritten); **drifted** вЂ” the art
was redrawn later, and the painter is the source of the earlier look.

| Folder | Makes in `game/art` | Inputs | Command | Reproduces |
| ------ | ------------------- | ------ | ------- | ---------- |
| `machines/assembler.js` | `machine_assembler`, `assembler_work_0-3`, `assembler_screen_0-3`, `items/solarCell` | none | `node tools/ship-art/machines/assembler.js [artDir]` | exact 10/10 |
| `machines/former.js` | `machine_former`, `former_work_0-3` | none | `node tools/ship-art/machines/former.js [artDir]` | pixel-exact 5/5 |
| `machines/smelter.js` | `machine_smelter`, `smelter_work_0-2`, `items/glass`, `items/silicon` | none | `node tools/ship-art/machines/smelter.js [artDir]` | pixel-exact 6/6 |
| `machines/lifepod.js` | `machine_lifepod`, `lifepod_glass` | none | `node tools/ship-art/machines/lifepod.js [artDir]` | pixel-exact 2/2 |
| `machines/charger.js` | `machine_charger` | none | `node tools/ship-art/machines/charger.js [file]` | pixel-exact 1/1 |
| `machines/capacitor-bank.js` | `machine_capacitorBank` (0101) | none | `node tools/ship-art/machines/capacitor-bank.js [file]` | exact 1/1 |
| `machines/parts.js` | `items/rotor`; also `wire`, `plate`, `gear` | none | `node tools/ship-art/machines/parts.js [itemsDir]` | `rotor` exact; others drifted (`resource-icons`) |
| `machines/make.js` | `items/asteroid`; also the first `drill` icon and `hero_drill` | none; writes `.raw`, then `ffmpeg -f rawvideo -pix_fmt rgba -s WxH -i x.raw x.png` | `node tools/ship-art/machines/make.js <dir>` | `asteroid` exact; drill ones drifted (`drill/`) |
| `burner/make.cjs` | `machine_burnerGenerator`, `burner_wheel_0-3` | none | `node tools/ship-art/burner/make.cjs [file] [zoom]` | exact 5/5 |
| `weapons/gen.js` | candidates; `held_fireAxe` is `hero_axe`, `icon_fireAxe` is `items/fireAxe` | hero parts | `node tools/ship-art/weapons/gen.js [outDir]` | exact 2/2 |
| `drill/gen.js` (uses `weapons/`) | candidates; `icon_tube` is `items/drill`, `held_tube(_spin1-3)` was `hero_drill(_spin1-3)` | scenes | `node tools/ship-art/drill/gen.js [outDir]` | `items/drill` exact; `hero_drill*` drifted (re-posed) |
| `harpoon/gen.js` (uses `weapons/`) | candidates; `rod_spring` is `hero_harpoon_rod`; `held_spring(_loaded)`, `icon_spring` were `hero_harpoon(_loaded)`, `items/harpoon` | scenes | `node tools/ship-art/harpoon/gen.js [outDir]` | `hero_harpoon_rod` exact; others drifted |
| `screwdriver/screwdriver.js` | `hero_screwdriver`, `tool_screwdriver` | none | `node tools/ship-art/screwdriver/screwdriver.js <outDir>` | exact 2/2 |
| `suit/suit.gd` (Godot) | `items/helmet`, `suit` | none | as jetpack's | exact |
| `jetpack/jetpack.gd` (Godot) | `items/jetpack`, `hero_jetpack` | none | `godot --headless --script tools/ship-art/jetpack/jetpack.gd -- <root>` (writes `<root>/art/...`; dirs must exist) | exact 2/2 |
| `computer/computer.gd` (Godot) | `*computer*`, `*radarAntenna*`; `items/` `processor`, `display`, `transceiver` | none | `godot --headless --script tools/ship-art/computer/computer.gd -- <artDir> [preview]` | exact 29/29 |
| `debris-v1/gen.js` | `debris/` `crate_01-08`, `hull_01-12`, `pod_01-04`, `scrap_01-12` | none | `node tools/ship-art/debris-v1/gen.js [outDir]` | exact 36/36 |
| `debris/gen.js` | `debris/` `asteroid_01-06`, `smallrock_01-06`, `bigrock_01-06`, `orerock_01-06`, `hold_01-04`, `pods_01-04`, `engine_01-04` (styles A, B) | `game/art/debris` | `node tools/ship-art/debris/gen.js [finalDir]` | exact 36/36 |
| `wreck-section/section.js` | `debris/section_01`, `items/hullSection` | `debris-v1/gen.js` | `node tools/ship-art/wreck-section/section.js [outDir]` | exact 2/2 |
| `wrecks/` | `debris/` `container_01`, `machine_01`, `rack_01`, `slab_01`, `tanks_01`; `items/` `cargoContainer`, `machineBlock`, `cableRack`, `shieldSlab`, `tankCluster`, `circuit` | `debris-v1/gen.js`, `debris/section_01.png` | in `tools/ship-art/wrecks/`: `node gen-wrecks.js`, `node icons.js 16` (not the default 14), `node install.js [artDir]` | exact 11/11 |
| `resource-icons/` | `items/` `gear`, `hullPlate`, `iron`, `planks`, `plate`, `scrap`, `scrapChunk`, `wire` | `game/art/items` | `node build.js [outDir]`, then `node pick.js [outDir] [itemsDir]` | exact 8/8 |
| `part-icons/` | `items/` `rock`, `battery`, `battery_empty`, `coil`, `frame`, `tube` | `game/art/items` | `node build.js [outDir]`, then `node pick.js [outDir] [itemsDir]` | exact 6/6 |
| `drones/tools/build.js` | drone-A: `drone_fly_0-3`, `drone_dock`, `drone_dock_eye`, `drone_eye`, `drone_lamp_work`, `drone_lamp_low`, `items/drone`, `items/drone_empty`; bay-C: `machine_droneBay`, `droneBay_pad`, `droneBay_charge_0-4`, `droneBay_charge_low` | scenes | `node tools/ship-art/drones/tools/build.js [outDir]`, then copy `drone-A/*`, `bay-C/*` | exact 19/19 |
| `hatch/tools/build.js` | variant B: `hatch_{closed,open}_{0,2,8,10}` | `game/art` pieces | `node tools/ship-art/hatch/tools/build.js [outDir]`, then copy `B/*` | exact 8/8 |
| `tank/tools/build.js` | variant A: `machine_tank`, `tank_fill_0-5` | scenes | `node tools/ship-art/tank/tools/build.js [outDir]`, then copy `A/*` | exact 7/7 |
| `farm/tools/build.js` | `items/seed` (A), `items/soil` and `items/biofuel` (B), `debris/soilclod_01-03` | scenes, icons | `node tools/ship-art/farm/tools/build.js [outDir]` | exact 6/6 |
| `grow-capsule/farm.js` | `machine_growCapsule`, `growCapsule_work_0`, `growCapsule_plant_0-3`, `items/biomass`; seed, soil, biofuel placeholders | none | `node tools/ship-art/grow-capsule/farm.js <outDir>` (`<outDir>/items` must exist) | 7/10 exact; seed, soil, biofuel drifted (`farm/`) |
| `grow-capsule/drones.js` | the first drone bay and drone icon placeholders | none | `node tools/ship-art/grow-capsule/drones.js <outDir>` | 0/2, drifted (`drones/`) |
| `clod-icon/icon.js` | `items/soilClod` | `game/art/items`, `farm/tools` lib | `node tools/ship-art/clod-icon/icon.js game/art/items <outDir>` | exact 1/1 |
| `tablet/export.cjs` (headless Chrome) | `tablet_frame`, `tablet_plate`, `tablet_stylus` | Chrome | `node tools/ship-art/tablet/export.cjs <outDir>` | exact 3/3 |
| `arc-bench/paint.js` | six candidate benches; the pick, `vise/`: `machine_arcBench`, `arcBench_alight`, `arcBench_front`, `arcBench_glow`, `arcBench_fit_glow` (0099) | `hero_drill`, `module_arc_coil`, previews | `node tools/ship-art/arc-bench/paint.js [outDir]`, then copy `vise/`'s five | exact 5/5 |
| `arc-bench/accumulator.js` | `items/accumulator`, `items/accumulator_empty` (placeholder, 0099) | none | `node tools/ship-art/arc-bench/accumulator.js [itemsDir]` | exact 2/2 |
| `wrist/export.cjs` (headless Chrome) | `wrist_body`, `wrist_cuff`, `wrist_display`, `wrist_display_docked`, `wrist_dock`, `wrist_gauge` | Chrome | `node tools/ship-art/wrist/export.cjs <outDir>` | exact 6/6 |

`machines/comp.js` lays one PNG over another (`node comp.js a.png b.png out.png <png.js>`). Painters of
candidates write pick pages and previews beside themselves, ignored by git. "Scenes" means
`game/art` scenes or previews.

## Not painted here

No painter for:

- The ship's tiles, doors, pipes, wall edges, back walls, `floor`, `ladder`, `fire_0-2`, valves, sockets, `wall_iron` and `wall_scrap`.
- `machine_generator`, `vent`, `solarPanel`, `furnace`, `crate`, `lamp` and `analyzer`.
- The hero's body parts and `hero_sheet(_screen)`.
- `items/cargoCrate`, `supplyPod`, `canister`, `asteroidLarge`, `asteroidRich`, `asteroidSmall`, `cargoHold`,
  `engineBlock` and `podCluster`.

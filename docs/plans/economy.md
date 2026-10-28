# Economy rework (design draft)

Status: draft for discussion, before `design-reviewer`. Numbers are a first pass, to be checked with the
cost calculator once they are in `sim/Content`.

## Problem

Measured on `main` (c7c0b020): a machine costs 4–12 caught pieces of salvage (the Former 8, the computer 35);
the whole first-stage ship about 270 hull plates, 220 scrap chunks and 48 rock; a planet is needed for 16 units
in all. Ice, mica and spores go into nothing; crystal, magnetite, cloth and alloy scrap into one recipe each.
Nothing is spent continuously but nails, canisters and fuel, so production never needs automating. Rock turns
into silicon two for one, so a pile of planet stone is an endless supply of circuits, and the asteroids are worth
nothing a planet's floor does not give.

## Principles

1. **Every source has a sink that grows with the ship.** One-off builds are not enough: a sink that recurs
   (repairs, fuel, air, ammunition) is what makes a factory worth building.
2. **No new flood.** A conversion that consumes a bulk item may only produce items that have a sink of their
   own. Every item in the ledger below names its sinks; one without is cut.
3. **Value density falls with volume.** Bulk items (scrap, rock, gravel, soil) come in hundreds and are worth
   little each; every step up the chain is several-to-one. Value never comes from bulk alone.
4. **Scarcity belongs to places.** Space gives scrap and iron; the asteroids iron and quartz (the only silicon);
   the planet copper, magnetite, ice, mica, crystal, spores; the ruins alloy, shards, cloth. Plain stone gives
   gravel and, rarely, a trace of something better.
5. **Cost is processing, not catching.** Catching stays manual, so raw amounts rise moderately (×2) and the
   parts machines take rise more (×3–5), with longer craft times: the work moves to stations and drones.
6. **Stacks by value.** Bulk 999, ores and refined 250, parts 250, assemblies 100, components 50, gear 1.
7. **The way down is had in space.** Everything up to the drop pod and its platform is made of what space gives;
   a planet's items go only into what comes after the first landing. `EverythingButAPlanetsOwnOpens…` in
   `ContentTests.Production` walks the recipes from space alone and pins the list of what needs a planet, so a
   softlock (a parachute of the ruins' cloth on the pod that reaches them) fails the build.
8. **New stations and tools are fair game.** A sink may be a new machine or tool, not only a new use of an
   existing one: what exists is not the limit.

## Recurring sinks

| Sink | Takes | Trigger |
| --- | --- | --- |
| Hull repair after meteors | scrap, plates, gravel + iron | meteors break walls weaker than their power |
| Fuel | planks, cloth, biofuel | burner generator, furnace, drop pod trips |
| Air | canisters (of ice) | jetpack thrust, spacewalks, refilling the tank |
| Ammunition | nails (scrap) | wrecks, creatures, sentries |

### Meteors from the start

The starting space gets light meteors; harder places stronger ones (the concept's "meteors belong to places",
moved earlier). Every few minutes (tunable, about one in 4–6) a meteor of power 1–6, weighted low, strikes a wall
cell exposed to space on the ship's side facing the drift. It breaks the wall if its power reaches the material's
`Strength`: scrap (2) often, iron (5) seldom, concrete (9) never in the starting space. The broken wall is lost,
its material too, and its room vents: the existing air systems make it urgent. The client shows a streak a moment
before the hit.

- The wall's cell stays planned, so repair is the screwdriver on the plan (or a builder drone later).
- Material strength gains a hull meaning: today it is "unused for hull".
- Deliberately not handled: meteors hitting machines, doors or solar panels (a later step: panels break and need
  glass); damage that accumulates on a wall. A hit breaks or does nothing.

## The crusher

A fed station on power, process `crush`, like the furnace (0057, 0060 name it as the model's next user).
4×4, draws 4, its input, then three outputs. It comes before the electric smelter: silicon and glass both start
here.

| Input | Output | Instead of |
| --- | --- | --- |
| asteroid ore (new item, what asteroids now give) | 1 iron ore + 1 gravel, 1 in 2 a quartz | rock → 2 silicon |
| rock (planet stone, boulders, pots) | 2 gravel, 1 in 12 a quartz | rock → 2 silicon |

Then at the furnace: iron ore → 1 iron. At the electric smelter: quartz → 1 silicon; **4 gravel → 1 glass**.

Rock stops being smeltable. It stays what it is good for: stone walls, gravel. 500 rock is 1000 gravel, which is
250 glass or 250 cells of concrete wall, and some 40 quartz: a bonus, not a circuit mine. Circuits need asteroid
ore, so the asteroids keep their worth.

Scrap chunks stay at the furnace: an item has one process, and giving the crusher scrap too means yields per
process, code we skip for now.

## Ledger: every item, its sources and sinks

Bulk and raw:

| Item | Stack | Sources | Sinks (recurring in bold) |
| --- | --- | --- | --- |
| scrap | 999 | chunks at the furnace, supply pods | wire, **nails**, **scrap walls**, floors, ladders |
| scrapChunk, hullPlate | 250 | debris, wreckage | furnace |
| iron | 250 | hull plates, iron ore | plate, gear, tube, **concrete walls** |
| asteroidOre (new) | 250 | asteroids | crusher |
| ironOre (new) | 250 | crusher | furnace |
| rock | 999 | planet stone, boulders | crusher, stone walls |
| gravel (new) | 999 | crusher | glass 4:1, **concrete walls** 4 a cell, soil |
| quartz (new) | 250 | crusher (ore 1 in 2, rock 1 in 12) | silicon |
| silicon | 250 | quartz | circuit, solar cell, processor |
| glass | 250 | gravel at the smelter, ruins | solar cell, display, transceiver, grow capsule, platform, drop pod, assembler |
| planks | 200 | crates, holds, trees, furniture | **fuel**, crate, soil, axe |
| soil | 999 | Former (3 gravel + 1 plank → 2), clods, ground | grow capsule, soil walls |

Planet and ruins:

| Item | Stack | Sources | Sinks |
| --- | --- | --- | --- |
| copperOre → copper | 250 | veins; ruins | motor, nozzle, accumulator, railgun |
| ice | 250 | ice veins, ice crystals | **canister** (Former: 3 ice) |
| mica | 250 | cave walls | accumulator, arc module, railgun |
| magnetite | 250 | heat cracks | motor |
| crystal | 100 | cave crystals, geodes | igniter, arc module, railgun |
| spore | 100 | glowcaps, ruins | **grows** in a capsule into biomass, giving spores back |
| alloyScrap | 250 | ruin walls, caches | alloy plate (Former: 3 → 1) |
| alloyPlate (new) | 100 | Former | nozzle, arc bench, arc module, railgun |
| cloth | 100 | ruins | cape, **fuel** |
| biomass → biofuel | 100 | capsules, glowcaps, creatures | **fuel**, engine tank, drop pod |
| canister | 20 | Former, supply pods, tank clusters | **breathed**, tank |

## Recipes (first pass)

Parts stay conversions; machines take several times more of them. A frame grows to plates and tubes.

| Item | Now | New |
| --- | --- | --- |
| frame | 2 plate | 3 plate, 2 tube |
| coil | 3 wire | 4 wire |
| circuit | 1 silicon, 2 wire | 1 silicon, 3 wire |
| solarCell | 1 silicon, 1 glass, 1 wire | 1 silicon, 2 glass, 2 wire |
| processor | 4 circuit, 2 silicon, 1 coil | 6 circuit, 3 silicon, 2 coil |
| display | 2 glass, 1 circuit, 4 wire | 4 glass, 2 circuit, 6 wire |
| transceiver | 3 coil, 2 circuit, 1 tube, 1 glass | 4 coil, 3 circuit, 2 tube, 2 glass |
| battery | 1 plate, 1 coil, 2 wire, 1 circuit | 2 plate, 2 coil, 4 wire, 1 circuit |
| helmetLamp | 1 plate, 2 wire, 1 silicon | 1 plate, 2 wire, 1 coil (before any silicon) |
| drone | 1 rotor, 2 plate, 1 circuit, 1 battery | 2 rotor, 3 plate, 2 circuit, 1 battery |
| deckDrive | 2 circuit, 1 rotor, 1 coil | 3 circuit, 2 rotor, 2 coil |
| motor | 1 rotor, 2 copper, 1 magnetite | 1 rotor, 4 copper, 2 magnetite |
| nozzle | 4 alloyScrap, 2 copper | 4 alloyPlate, 6 copper |
| fuelPump | 1 motor, 2 tube, 1 plate | 2 motor, 4 tube, 2 plate |
| igniter | 1 crystal, 1 circuit, 2 wire | 2 crystal, 2 circuit, 4 wire |
| arcModule | shard, 2 coil, 2 circuit, frame | shard, 2 crystal, 2 mica, 4 coil, 4 circuit, 1 alloyPlate |
| accumulator | shard, battery, 2 copper, circuit | shard, 2 battery, 4 copper, 4 mica, 2 circuit |
| railgun | shard, 4 copper, 2 coil, tube, frame, 2 circuit | shard, 8 copper, 4 coil, 2 crystal, 2 alloyPlate, 3 circuit |
| canister (new craft) | — | 3 ice, at the Former |
| alloyPlate (new) | — | 3 alloyScrap, at the Former |
| soil | 1 rock, 1 planks | 3 gravel, 1 planks → 2 |

Machines:

| Machine | Now | New |
| --- | --- | --- |
| furnace | 4 chunk, 2 hull plate | unchanged: the first build |
| analyzer | 2 plate, 4 wire | 4 plate, 8 wire, 1 coil |
| crate | 6 planks, 2 iron | 8 planks, 2 plate |
| tank | 4 plate, 1 tube | 8 plate, 2 tube, 2 gear |
| vent | 1 plate, 1 tube | 2 plate, 2 tube |
| burnerGenerator | frame, rotor, coil, 6 scrap | 2 frame, 2 rotor, 2 coil, 4 plate |
| crusher (new) | — | 3 frame, 8 gear, 6 plate, 2 rotor, 1 circuit |
| smelter | frame, 2 coil, 4 plate, circuit | 3 frame, 4 coil, 8 plate, 4 tube, 2 circuit |
| assembler | frame, rotor, glass, 2 wire, circuit | 3 frame, 2 rotor, 4 glass, 2 coil, 10 wire, 2 circuit |
| former | 2 frame, rotor, 2 gear, circuit | 4 frame, 3 rotor, 8 gear, 4 plate, 3 circuit |
| solarPanel | frame, 4 cell, 2 wire, circuit | 2 frame, 6 cell, 4 wire, 1 circuit |
| charger | frame, coil, 4 wire, circuit | frame, 2 coil, 6 wire, 2 circuit |
| capacitorBank | frame, 2 coil, 2 plate, circuit | 2 frame, 6 coil, 6 plate, 2 circuit |
| growCapsule | frame, 2 glass, 4 soil, circuit, 2 wire | 2 frame, 6 glass, 8 soil, 2 circuit, 4 wire |
| droneBay | 2 frame, 4 plate, 2 circuit, 2 wire | 4 frame, 8 plate, 4 rotor, 4 circuit, 10 wire |
| computer | 2 frame, 4 plate, 2 processor, display, 2 coil, 6 wire | 4 frame, 12 plate, 3 processor, 2 display, 6 coil, 20 wire |
| radarAntenna | frame, 3 plate, 2 tube, rotor, 2 gear, transceiver, cell | 3 frame, 8 plate, 4 tube, 2 rotor, 6 gear, 2 transceiver, 2 cell |
| launchPlatform | deckDrive, 2 plate, glass | deckDrive, 4 plate, 2 glass |
| dropPod | 4 frame, 8 plate, 2 glass, 3 tube, processor, display | 6 frame, 16 plate, 8 glass, 6 tube, processor, display |
| engine | nozzle, pump, igniter, 2 frame, 4 plate | nozzle, pump, igniter, 4 frame, 12 plate, 1 motor |
| arcBench | shard, frame, 2 plate, gear | shard, 2 frame, 4 alloyPlate, 1 motor |

Walls: scrap 2 scrap a cell (was 1); iron 1 plate; **concrete** (new material, strength 9, leak 16, build 20
ticks) 4 gravel + 1 iron a cell. Craft times double across the board.

## The workbench: the pod stops growing

The pod's Fabricator makes every part and tool today and keeps gaining rows. It goes back to what a lifepod
would give: the helmet, the suit and the screwdriver (free or of salvage, so a lost screwdriver never strands a
hero before the furnace).

A **workbench** takes the rest of the start: no power, slow (the pod's fifth of speed), built of raw salvage with
no smelting (scrap chunks, hull plates, planks), as the furnace is. It makes the basic parts (wire, plate, gear,
tube, nails) and the first tools (axe, harpoon, nailer, helmet lamp). Everything else moves to the **Former**:
assemblies (frame, coil, rotor, motor) and the advanced gear (jetpack, battery, drill).

So power comes before the jetpack, and the Former is the first real decision. That forces two costs down a tier:
the burner generator and the Former itself are built of bench parts and a salvaged circuit, never of frames,
rotors or coils, which only the Former makes. The reachability test catches the loop if a cost slips.

## Order of play

Pod (suit, screwdriver) → furnace and workbench (raw salvage) → burner generator (bench parts) → Former (bench
parts, a salvaged circuit) → crusher → electric smelter → circuit assembler (glass): the electronics loop closes
after about five circuits from cable racks.

## Creatures drop what they are made of

Grazers drop hide and fat besides biomass; sentries stay scrap. **Hide** goes with cloth into the cape (2 cloth,
2 hide) and the insulated suit; **fat** burns longer than planks and smelts into biofuel at the furnace, so hunting
is a fuel source. Both are a planet's, so neither goes into anything before the first landing.

## More sinks: stations and tools to come

- Helmet tiers: a bigger tank of glass and a canister.
- Suit tiers: an insulated suit (hide, cloth, coils) for the cold; a heat suit (alloy plate, mica) for the band
  over the lava.
- An electrolyser: ice into the oxygen network, a later step once the starting air is settled.

## Code this needs

- **Crusher:** a station and a machine row, items with `Process: "crush"`; the fed-station code is shared.
- **Chance byproducts:** a fed item's yields with a one-in (quartz from ore). Today only `Returns` draws, and only
  copies of the input; generalise it to yields with a `OneIn`, drawn from the world's Rng.
- **Several units per smelt:** `4 gravel → 1 glass`, a count on the input (`ItemSpec.SmeltTakes`).
- **Random meteors:** a system in `Hazards` on the world's Rng, by zone, reading `Material.Strength`; the plan kept
  on a broken cell; a client streak.
- **Spores as a crop:** data only (`Process: "grow"`, yields biomass, returns spores).
- **Content:** new items (asteroidOre, ironOre, gravel, quartz, alloyPlate), material concrete, asteroid loot,
  stacks, costs, times; rock no longer smelts. Tests that pin costs follow.
- Saves: items and a material added, none renamed. Existing worlds keep their rock and silicon.

## Deliberately not handled

- Catching stays manual: no automatic debris collector this pass.
- Meteors hit walls only; machines and panels later.
- No tool wear, no water for the farm, no ice in the oxygen generator.
- An item has one process: scrap chunks do not go into the crusher.

## Steps, simplest and most rewarding first

1. Stacks raised, spores a crop, the space-only reachability test. Done (#255).
2. The workbench, the pod cut to the suit, the costs of the burner generator and the Former moved to bench parts. Done.
3. The crusher with asteroid ore, iron ore, gravel and quartz; chance yields and several units a smelt in code. Done.
4. Costs and craft times across the tree, checked with the calculator against about five hours solo. First pass done (the-crusher).
5. Meteors from the start, walls kept planned. Done.
6. Hide and fat (done, #256), ice canisters (done); alloy plates, concrete walls, suit and helmet tiers to come.

## Open

- Target time from the start to the drop pod, solo and with four: it sets the multipliers.
- Meteor rate and power in the starting space.

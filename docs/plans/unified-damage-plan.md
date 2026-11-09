# Unified damage: implementation plan

The plan for [unified-damage.md](unified-damage.md). One step, one commit; the build stays green after each.

## What the code already gives

- Hero position for `HewCells`: `BuildingModule` already depends on `ICrewQueries` (`_crew.HeroOf(peer)`); Crew is
  registered before Building and never names it. `Hero.Cell` is the body's middle cell as last reported (at most every
  `Crew.ReportEvery` ticks): enough for a direction. `Hew` has its own origin (`X`/`Y` in thousandths). No hero, no carry.
- The thrower at burst time: `Shot.By` in `sim/Creatures/Shots.cs`, already passed as `peer`. `ICrewOps.Shock` works
  wherever the hero stands on the surface; one who boards during the throw's flight (under 2 s) is not shocked (accepted).
- `Material.Armour` is content only: walls are saved by material code. Cracks are saved in strength units, so only the
  tenths step (0) bumps the save format.

## Steps

### 0. `refactor(content)`: strength in tenths
- ×10: every `Material.Strength`, `Build.Strength` (pieces, default 8), `Flora` strength (plants, ruins furniture, the
  cradle), `Weapon.BlockDamage`, `Gun` block damage. `Armour` untouched (it goes in 7). Doc comments say tenths; item
  cards show block damage /10.
- Piles stay as they are: tools hit them with wreck damage, their `Hp` in its points (moving them to block damage would
  make the pickaxe the best pile tool and the grinder the worst). Bursts convert in step 6 (pile `Hp` × 10).
- `World.SaveFormat` 60 → 61; its step clears saved cracks (they heal in seconds; scaling would need each crack's kind).
  A saved incoming meteor's power is step 7's to rescale, with its own bump.
- Tests: tests hard-coding a strength or block damage read it from Content or ×10; swings to break a wall, a piece, a
  plant unchanged; `Migrations` test that a 60 save loads with no cracks.
- Why now: at whole units the carry, the built share and machine hit points have no room (see "Accepted findings").

### 1. `feat(creatures)`: shots carry their surplus on cells
- `IBuildingOps.Hit`, `HitWall`, `HitPiece` return `int?`: null when the cell held (only cracked or nothing there to
  break), else the surplus, 0 included (a wall broken by exactly its remaining strength counts as broken).
  `IBuildingQueries.Surplus(cell, damage)` → `int?`, changing nothing, so the shooter's copy (`StepOf`, which gains the
  carry as a parameter) judges a cell as the sim does.
- `sim/Content/Shots.cs`: `Gun` gains `Pierce` (percent, on cells): railgun 65, nailer 0. `PierceKeep` and the `Pierce`
  enum stay, for bodies only. Railgun block damage rises so soil leaves a surplus (at today's 40 it equals soil's strength).
- `Shot` gains a saved `int? Carry` (null = no cell broken yet; an added field, no bump). A cell's damage is `Carry ??`
  the gun's block damage. A shot stops on a cell it only cracks and when its carry rounds to 0; on cells `Pierce.All` no
  longer flies through. Creature and wreck damage keep today's fold. `ShotTick` gains `Carry`.
- Tests (`RailgunTests`, `ShotsTests`): `RailgunIntoSoilBreaksTwoCellsAndCracksTheThird`,
  `AShotFliesOnWithItsSurplusTimesItsPierceRoundedDown`, `ANailStopsInTheWallItBreaks`,
  `ACreatureBeforeAWallLeavesTheBlockDamageWhole`, `TheShootersCopyMeetsCellsAsTheSimDoes`,
  `AShotSavedMidFlightKeepsItsCarry`. `PierceTests`, `RailgunTests` (through cells), `ItemCardTests` change.
- README: Creatures `## Rules`.

### 2. `feat(building)`: tools carry one cell behind
- `Weapon` gains `Carries` (percent, as `Gun`): pickaxe 70 at block damage 75 (at half, below a bush's 80, the carry settles
  under what fells stone at one blow; at 75/70 straight digging goes 2, 2, 2, 1 blows a cell), others 0. Tests in `CarryTests`.
- `HitAll` keeps each broken wall or piece's surplus; a second pass, once per command after all primary hits, sends
  surplus × pierce / 100, rounded down, to the neighbour one step further along the line from the origin through that
  cell. dx, dy run from the origin to the broken cell's centre, in thousandths of a cell (integer octant test: x steps
  when 5·|dx| ≥ 2·|dy|, likewise y); dx = dy = 0 carries nothing. Targets in a list sorted by cell, each taking its
  largest carry, through the same rule as `Stops` (breakable wall or piece, in the zone or a vessel's box, `ToolsBreak`).
  Carries are not carried again; piles and plants take none. Origin: `Hew` from X/Y; `HewCells` from the centre of
  `_crew.HeroOf(From)?.Cell`, only when the hero is on the surface the command hits (aboard, the vessel's box rule).
- Tests (`PickaxeTests`, `HewTests`, `ToolEdgeTests`): `APickaxeSwingBreaksSoilAndCracksTheCellBehindByHalfItsSurplus`,
  `ACarryGoesAlongTheLineFromTheHeroThroughTheBrokenCellAslantToo`, `ACellTwoCarriesReachTakesTheLargestOnce`,
  `ACarryIsNotCarriedAgain`, `HewCellsWithNoHeroCarriesNothing`, `ACarryStopsAtBedrockLavaAndTheFog`,
  `AboardTheShipNoCarryLeavesAVesselsBox`, `DiggingStraightTakesFewerSwingsThanDiggingAcross`.
- README: Building `## Rules`.

### 3. `feat(building)`: cracks heal after a minute
- `Builds.CrackHeals` 200 → 1200. Test `ACrackHealsTwelveHundredTicksAfterItsLastHit`; tests running 200 ticks to heal change.
- Cracks live 6× longer and a charge leaves a rim of dozens, so `Cracks` grows to hundreds: its per-tick `RemoveAll` and
  the cell lookups are measured in the step-5 budget test; if lookups show, a derived cell → index map (rebuilt, not saved).

### 4. `feat(core)`: the wave reports what it cannot pay for
- New `sim/Content/Damage.cs`: `Step` (falloff, `Meteors.Step` moved here, its users with it), `BuiltTakes` (permille, about 37: 1/27),
  `MachineHp` (tenths, the default hit points of a built machine, about 20 — a scrap wall's).
- `Wave.Spread` gains an optional `unpaid(cell, left)` callback, for a cell the wave reached the edge of but could not pay to
  enter: `left` is the most left on the cheapest path to it after the step, over the step's length (so in its entry cost's units), reported only when ≥ 1
  (clamped before any division, so truncation never meets a negative). A start too dear gets the whole charge. Called
  after the spread, ascending, once per cell, never for an entered one.
- Tests (`WaveTests.cs`): reported once with the most left on its cheapest path; a cell entered later never reported;
  nothing reported when the step ate it all; a start too dear gets the whole charge; ascending order; `[Property]`: every
  unpaid cell is unentered, borders an entered one, and 1 ≤ left < its entry cost (proven to bite by reporting an entered
  cell once).

### 5. `feat(building)`: `Burst` owns the wave; the charge cracks, drops and crushes
- `IBuildingOps.Burst(peer, cell, power, reach, spoils)` → whether it went off (false from a cell that stops it), replacing
  `Blast` (the `Blasts` query goes); the shot reads nothing back. `Spoils { Drop }`, `Lost` added in step 7: `Drop` (the
  charge) drops what breaks and leaves back walls; `Lost` (meteors) loses it and also breaks the back wall behind each
  entered cell, paid from what was left there, as Hazards does today.
- Entering: open cell or plant 0 (a `Fixed` ruins piece is a plant: never struck, it falls only with its hold, as when
  dug); natural ground its remaining strength; a built wall or piece `ceil(remaining·1000/BuiltTakes)`; bedrock, lava,
  fog stop it; machines and other structures stop it until step 6. `ToolsBreak` no longer gates bursts. A piece (later a machine) pays once, at the cheapest of its cells the wave reaches; its other
  cells cost the same and then cost nothing, so it never pays twice.
- Entered cells break once each (a piece by its head), ascending; unpaid cells crack once by `left` (built:
  ·BuiltTakes/1000, rounded down, nothing when 0), a piece at its head with its largest `left`.
- What breaks with a wall goes with it: a debris net hung on it is handled by `Burst` as Hazards' `Break` does today
  (moved into Building, so a charge never leaves a net in the air).
- `Drop`: what breaks falls at its cell for `FloorStack.Nobody`, from `Material.Crushed` when set (stone: 1 gravel) or
  `Cost`; held plants felled with `Fell(plant, null)`. A felled `Ruins.Cradle` shocks `peer` by `CradleShock` once; two
  bursts in one tick are applied in command order, so the first to break it is shocked.
- Creatures loses `_wave`, `Enter`, `_blasted` and calls `_building.Burst(..., Spoils.Drop)`. Charge power 500, reach 8:
  stone 4 out, soil the whole circle of 8, alloy cracked to 496. `BuiltTakes` tuned so one charge cracks scrap and a second
  breaks it (a derelict's iron hull then takes more: see "Accepted findings").
- Tests (`BlastChargeTests`, rewritten): crater four out with its rim cracked and gravel and ore in it; a second
  goes further; soil the whole circle, stone as far aslant as down (within 1.5 cells); alloy cracks and a second within
  the heal time opens it; scrap cracks and a second breaks it; aboard it cracks the hull. `BurstTests` (the op called
  directly): charges under the hall floor drop the relic and shock the thrower once; a warden whose post breaks stands
  down; a built piece cracks at its head once; bedrock, lava, fog stop it; a door it pays for once across both cells;
  `[Property]`: nothing it could not pay for breaks (proven to bite by costing built things as natural). `NetTests`: a
  net on a wall it breaks goes with it. `BudgetTests`: three charges bursting in one tick in soil stay under the 2 ms tick. Old cases ("giving no rock", "a built wall stops it", "ore to
  the thrower", "aboard breaks nothing", the crater radii) rewritten.
- `--charges` still works (`tools/snap.ps1 -- --planet --charges`). READMEs: Building, Creatures.

### 6. `feat(building)`: machines break
- `MachineTrait.Pile` on the four pile rows; built machines get `Hp` in tenths (per kind where it matters, else
  `Damage.MachineHp`); the lifepod none.
  `PileAt`, `Rules.MarkProblem` (`building.needsAxe`), client `Piles.At` and `PilesScenario` read the trait, not `Hp != null`.
- Bursts enter machines: built `ceil(remainingHp·100/BuiltTakes)`, a pile its remaining Hp, `Fixed` stops it. A short
  machine cracks at `machine.Cell`; `Crackable` accepts any machine head with `Hp`.
- `BreakMachine`: new `IItemsOps.Spill(machine, cell)` drops crate slots, a bench's tool and fitted modules; new
  `ICraftingOps.Scrap(machine)` removes its queue and returns the refund, dropped at the cell; then `_powerOps.Unplug`
  (cables stay on a peg, 0110) and `_gridOps.Clear(cell, "machine")`; its cost drops with `Drop`, lost with `Lost`.
  Gas and fluid held in a tank, cistern or pipe machine are lost with it, through the same removal deconstruction uses,
  so the room's air and the pipe net recount; a belt or inserter aimed at the cell finds nothing there, as after
  deconstruction. Building gains `ICraftingOps` and `IItemsOps` through the Registry only: the csproj references exist,
  and nothing they reference names Building, so no cycle.
- Client: an open panel of a machine that is gone closes, as after deconstruction (check it does; fix if not).
- Client: `Piles.Cracked(grid, cell)` for any machine with `Hp`; `DigView` draws the existing crack net over it.
- Tests (`MachineBreakTests`): a charge cracks a furnace and a second breaks it, its cost, ore and modules on the floor;
  a station's queue refunded at its cell; cables stay on a peg; the lifepod never breaks; tools aboard still break piles
  only; no module keeps anything of a broken machine (crate, modules, queue, cisterns); a broken tank leaves no gas in the
  room's count. `PilesTests`, `ImpactsTests`,
  `GroundSoundsTests` change where piles are built with `Hp`.
- READMEs: Items, Crafting (`## API`), Building, `game/features/building`.

### 7. `feat(hazards)`: meteors through `Burst`; armour goes
- Hazards depends on `IBuildingOps` (csproj + Registry; registered after Building, which references only Crafting, Crew,
  Pipes and Items: no cycle). `Strike` becomes `Burst(Nobody, cell, Charge(power), Farthest(power), Spoils.Lost)`; nets
  and back walls are `Burst`'s (step 5). `HitProblem`, `Targets` read `Strength`. `MeteorHit` still clears at once.
- Remove `Material.Armour` and every `Armour:` argument, `Meteors.PerStrength`, `Meteors.Step`. Retune `Shower` so a
  meteor's power is spread: most crack scrap, the largest (about one in four) breach a scrap hull on their own, so the
  hull hazard stays in play before the shower event; a second small one on a cracked patch breaches too. A machine's cost
  is lost, its slots, modules and queue still spill.
- What the player sees of a crack aboard: the crack drawn on the hull (the existing crack net over walls), and a meteor
  that cracks a wall of a sealed room shows the same edge flash the breach alarm uses, weaker; the alarm itself stays for
  breaches.
- `Fuzz`: now and then it moves an incoming meteor to strike next tick, so `Burst` with `Lost` runs in the generic suites.
- Client: `WallStats.Of` shows the leak only (`BuildMenu.cs` drops the argument); `wall.meteors`, `wall.resists`,
  `wall.meteorsNever` leave `Building.En.cs`/`.Ru.cs`; `WallStatsTests`. `MeteorScenario --meteors=N` strikes the same
  cell again ten seconds later, so sequence 6 can be seen.
- Tests (`HazardsTests.Meteors`): a small meteor cracks a scrap hull; a second within the heal time breaches it and the
  room vents; the largest of the shower breaches scrap alone; a crack in a sealed room's wall raises the weak flash; what it breaks is lost; a machine it breaks spills its slots and loses its cost; a door takes its share and no
  longer stops it; back walls break to meteors alone. The hole-per-strike cases are rewritten to crack-or-breach;
  `ReadAWallsArmourNotItsHardnessToDig` deleted; `NetTests` reworded to strength; `WreckScrapTests` loses `Armour`.
- README: Hazards.

### 8. `docs`: the ADR and the map
- `docs/decisions/2026-11-09-unified-damage.md`: rules 1–9, one scale in tenths, `Spoils`, numbers and rounding,
  omissions, the accepted findings. Supersedes in part: `2026-11-08-blasting-charge` (crater to dust, ore to the thrower),
  `2026-10-27-shots-pierce` (share per pierce on cells; bodies stay), `2026-11-09-round-blasts` (wall cost by armour),
  `2026-10-29-meteor-wave` (armour). `tools/adr-index.ps1`; `docs/map.md` (Wave, Damage.cs, WallStats). Delete both plan
  files. Full `dotnet test Stillwake.sln`, `dotnet format --verify-no-changes`.

## Checking by hand

1. `--pickaxe`: dig soil straight, then across: straight takes fewer swings.
2. `--planet --charges`: at a stone face, a round crater, rim cracked, gravel and ore in it; again, further.
3. `--planet --charges` at a spire: deep cracks; a second within a minute opens the first layer.
4. `--vessel=shuttle` with charges: the hull and your own scrap wall crack; scrap breaks to a second.
5. Railgun into soil: two cells break, the third cracks.
6. `--meteors=4`: the first strike cracks with a weak flash, the second breaches, alarm as today.
7. `--furnace`: ore in, two charges beside: cracks drawn on it, then its cost, ore and modules on the floor.
8. Three charges into the spire from under the cradle: the hall opens, the relic falls, the suit takes the shock.

## Accepted findings

1. One `BuiltTakes` cannot make both scrap (strength 20) and iron hull (50) fall to the second charge: tuned for scrap,
   a derelict's hull takes 4–5 charges.
2. Two small meteors on one patch within a minute are rare at one each 4–6 min: until the shower event, breaches come
   mostly from the largest meteors alone; `--meteors` shows the two-strike case.
3. The carry alone never breaks fresh soil; its effect is a head start: in stone, straight on, every fourth cell at one blow.
4. Charges from the side open the spire's hall and leave the relic standing; it falls when the floor under it breaks.
5. `HewCells` aims its carry from `Hero.Cell`, up to `Crew.ReportEvery` ticks old and the body's middle: a hero digging
   on the run may carry a step off their aim.
6. Building, Creatures, Items, Crafting and `game/features/building` READMEs are at the 8 KB cap: each edit cuts what it adds.

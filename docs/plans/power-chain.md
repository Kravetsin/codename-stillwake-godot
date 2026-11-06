# Plan: the power chain (fermenter, biofuel generator, treadmill wheel)

Status: planned 2026-11-06 from the reviewed design (playtest of 2026-11-06: the planet stage needs more power than
hand-fed burners give). Two PRs: **A** the fermenter and the biofuel generator, **B** the wheel. What stays true goes into
ADR `2026-11-06-the-power-chain` and the READMEs of Crafting, Furnace, Power, Landing and Crew (its `Exert` is no longer
only "a change of running by speed"); this file is deleted once both land.

**Decided by the user:**
- Biomass and fat become biofuel **only at the fermenter**: the furnace and the smelter stop making it.
- The wheel needs no stamina: a hero must stay in it, so it cannot devalue the burner.
- The pictures of all three are the user's pick on a published Artifact page (CLAUDE.md, Art and sound picks), in ink and
  watercolour (`tools/anime-art/README.md`). Their sounds reuse existing cues; new sounds are a later pick.

## Rules as the player meets them

### A1. The fermenter (`fermenter`, "fermenter" / «ферментёр»; the user may rename)

1. A fed station, the grow capsule's sibling: `Stations` row `new("fermenter", Speed: 1000, QueueMost: 0, Rows: [], Heat: 1,
   Process: "ferment")`. Machine row: 4×4, `Draws: 2`, `Slots: 2, Inputs: 1` (biomass or fat in, biofuel out), cost frame 2,
   glass 4, tube 4, plate 4, circuit 1, `BuildTicks: 200`, `Menu: new(MenuTabs.Production, 65)`, `Char: 'u'`, `Plug` from its
   picture. No trait: drones serve it as every fed station (`Jobs.Takes`: `station.Fed`).
2. Biomass and fat get `Process: "ferment"`; their yields (one biofuel), `SmeltTicks` 160 and `Heat` 1 stay. At a full share
   (its network covering its draw) it makes a biofuel every 160 ticks (8 s), drawing 2 a tick: 320 power-ticks a biofuel. Fat keeps its `BurnTicks`: it still burns in a fuel slot.
3. **Nothing else changes in Crafting**: `Worked` already matches an item's `Process` to the station's, so biomass put in a
   furnace or smelter, by hand or by drone, is taken and sits unworked as a seed does, the furnace never lit for it (0057,
   "stations: one model for every crafting machine": the wrong process). Drones keep topping up an input with what it holds,
   worked or not, as they do a seed in a furnace today. An old save's biomass in a furnace sits there too, its part-done work kept and never finished: the player takes it out
   (accepted).
4. Its blueprint is understood as every machine's is (Research's rule): once its parts are obtained enough. Its parts are
   the grow capsule's tier, so the farm and its fermenter open together.
5. Client: `StationView` lights it while it works (an entry like the grow capsule's, colour from the picked art); the panel's
   progress bar and slot mark treat `"ferment"` as their own case beside `"grow"` (`InventoryPanel.Machine`, `Reading`), else
   the smelt look. Name and about in `Building.En/Ru`, `station.fermenter` in `Crafting.En/Ru`, `use.open.fermenter` in
   `Crew.En/Ru`. Its hum is the grow capsule's cue.

### A2. The biofuel generator (`biofuelGenerator`, "biofuel generator" / «биотопливный генератор»)

1. A fuel burner, data only: 6×4, `Makes: 16`, `Slots: 1, FuelSlot: 0, Takes: "biofuel"`, cost fuel pump 1, frame 3,
   plate 8, coil 4, tube 4, `BuildTicks: 240`, `Menu: new(MenuTabs.Power, 25)`, `Char: 'q'`, `Plug` from its picture.
2. Furnace runs it as the burner: lit only with fuel and air, breathing `Furnace.Breath` while it burns, rested while out,
   lighting its next fuel only while its network wants a fire (`Needed`, 0101). A biofuel burns 1600 ticks: 25 600
   power-ticks here, 9 600 in the burner generator (6 × 1600).
3. `Takes: "biofuel"` keeps planks, fat and everything else out of its slot by hand and by drone (as the engine's and the
   pod's). The engine ADR's "`Takes`, not `FuelSlot`" stays true for the engine; this machine has both `Takes` and `FuelSlot`.
4. The fuel pump (motor of magnetite) is a planet's part, so it is built at the planet stage: it joins the planetary list of
   `EverythingButAPlanetsOwnOpensFromWhatSpaceGivesAloneTheDropPodAmongThem`.
5. Client: `FurnaceView` draws its fire where the picked picture's window is (`FireAt`), its shimmer and light as the burner's;
   no flywheel unless the picture has one. Its hum is the burner's cue.

### A3. The early path, before the first landing

Before: seeds (supply pods, soil clods) → grow capsule → biomass → furnace → 5 biofuel → drop pod.
After: the same, **the fermenter in place of the furnace**, on power. Its parts are the grow capsule's, so the step costs one
machine more and power the capsule already needs; a player who built a furnace for the descent's fuel builds a fermenter
instead. The reachability test (`TheFarmIsReachableSeedsFromSalvageSoilAtTheFormerBiomassInTheCapsuleBiofuelSmelted`) walks it: without the
fermenter no biofuel, so no descent. `Landing.DescentFuel` stays 5. The PR says this in its description.

Market: biofuel's worth is worked out from biomass's recipe (`Market.Worked`), which does not change: no price moves.

### B. The treadmill wheel (`treadWheel`, "treadmill wheel" / «беговое колесо»)

1. Machine row: 6×6, `Makes: 4`, `Has: MachineTrait.Seats`, `Anywhere: true`, cost planks 10, gear 6, coil 1, scrap 8,
   `BuildTicks: 160`, `Menu: new(MenuTabs.Power, 15)`, `Char: 'w'`, `Plug` from its picture. Of the workbench's parts and
   the furnace's scrap: the earliest power there is.
2. **`MachineSpec.Anywhere`** (new content flag): a maker that makes power wherever it stands, not only under the stars.
   Not keyed on `Seats`: a crank or a water wheel would share it. Power's supply rule, for a maker not resting and not broken:

   | Maker | In `Supply` | In `Steady` |
   | --- | --- | --- |
   | with a `FuelSlot` (burner, biofuel generator) | always | never |
   | `Anywhere` (the wheel) | always | always |
   | any other (solar panel) | only under the stars | when in `Supply` |

   So a wheel's 4 is weighed against demand before a fire lights (`Needed`: demand + intake > `Steady`): a wheel covering its
   network keeps the burner generators and biofuel generators cold.
3. **Boarding:** E on it sends `Board(wheel)`, as on the pod; `Alight` as on the pod; one rider. `BoardProblem` already reads
   `Seats`. `LaunchProblem` refuses a seat in a machine without `Lands` with `landing.notSeated`, checked right after the
   seat itself, before the off-ship branch (a wheel may stand on a planet). `Fuzz` boards wheels too.
4. **It turns only under an awake, exerting rider:** each tick in construction, after Building, Landing calls
   `IPowerOps.Rest(id, !turning)` for every standing machine with `Seats` and `Makes`, in id order, where `turning` is: its
   rider (`RiderOf`) has a hero on this surface, awake (`Out == 0`) and `Exerting`. Landing gains `IPowerOps` in its
   constructor. A wheel built this tick is rested before power's derivation, so it never makes a tick unridden. A rider
   passing out stops it from the next construction; one leaving the game (`Leave`) loses its hero, so its seat and its
   `Exerting` go with it.
   - **One writer a rest:** a `Seats` maker has no `Draws` and no `FuelSlot`, so Crafting and Furnace never rest it; a
     `ContentTests` check holds every `Seats` machine with `Makes` to that.
   - A world made with a wheel in its layout counts it for its first derivation (`Rebuild` after create), before the first
     construction rests it: one tick, on every peer alike (accepted, as for burners).
5. **`Exert` from a seat:** the chain is the client's `Report.Exerting` (decides) → the `Exert` command (sends) →
   `Hero.Exerting` (Crew's saved flag, read by Landing). While seated `HeroView` zeroes the hero's input, so `Vx` stays 0;
   the held move key is read before that (`taken.X != 0` from `Take()`, the autopilot's likewise) and passed to
   `Report.Exerting` with whether its seat is in a machine without `Lands`: then it exerts whatever its speed. Exerting, the
   hero breathes `Crew.Exerted` (2) units a breath instead of 1, as a runner does. No new Crew query: `HeroOf(peer).Exerting`
   is public. Trusting the client's word is 0002's (hero movement is client-authoritative).
6. **`FloorOf(kind, cell, width)`**: where a rider is held, the middle of the machine's bottom row, by its own spec. The pod's
   callers pass `dropPod`: `Seating.SeatOf`, `FlightTimeline` (its inverse) and `DescentTests`.
7. Client: where the client means the drop pod it asks `Lands`, not `Seats` (`Main.cs` pods and `LaunchOf`, `PodFlights`);
   `SeatedHeroes` draws a pod's rider behind its porthole and a wheel's in the drum, running in place, its gait's frames
   advancing only while it exerts; the drum turns on the same reading (its rider in `Seats`, awake, `Exerting`), the rule
   the sim rests it by, so picture and power agree. Name in `Building.En/Ru`. **The prompts say what to do:** E on it
   "Get in the wheel" (`use.board.treadWheel`), and seated, in place of the pod's fixed `use.alight`, a per-kind
   `use.alight.treadWheel`: "[A/D] Run · [E] Get out". Seated and still, the drum stands and the rider stands in it; the
   power overlay shows the wheel making nothing. Its sound is a reused creak or motor cue.
8. **Tests and the pod:** existing pod tests find the pod by `Seats` (`Assert.Single`); a map with a wheel finds it by `Lands`.
   The wreck has no wheel, so the generic suites' fuzz never meets one: `TreadWheelTests`' twin and mid-ride save tests are
   its coverage, and a wheel is added to the testbed (client only).

## Save, determinism, co-op

- No save format bump: three machines, saved by kind; `Anywhere` is content; Landing's new rule writes Power's existing
  `Resting`; biomass's process is content. Old saves load unchanged (biomass in a furnace sits).
- Every input is already a command: `Board`, `Alight`, `Exert`. Rests are written in construction from saved state (seats,
  heroes), in machine id order (`MachinesWith`), so twins agree. Generic suites cover save/load and the Fuzz.

## Tests

**A:** `FermenterTests` (scenario): biomass and fat ferment into biofuel at 160 ticks a unit at full power, half speed at half
power; it rests idle and draws nothing then; a seed or a scrap chunk sits unworked; biomass and fat in a furnace and a
smelter sit unworked, the furnace never lit for them. `BiofuelGeneratorTests`: only biofuel goes into its slot (a plank
refused by hand, `items.notTaken` or the engine's refusal, as `Takes` says); a biofuel burns 1600 ticks making 16 a tick; rests
while out; lights only while `Needed`; goes out without air. Updated: `FurnaceTests` (fat smelts no more; it still burns),
`FarmTests` (biomass ferments, not smelts), `ContentTests.Production` (reachability: the fermenter gates biofuel; the
generator planetary), `LogisticsTests` servable list, `MachineTraitsTests`, `BuildMenuDataTests`, `TestbedTests` (both on
the testbed), `MachineArtTests`.

**B:** `TreadWheelTests` (scenario): a ridden wheel whose rider exerts makes 4 on its network; empty, ridden but still, or
its rider passed out, it rests; it makes inside a room, where a solar panel would not; it counts in `Steady`, so a burner
on its network stays unlit while the wheel covers the demand and lights once demand passes it; `Launch` from it is
`landing.notSeated`; one rider (`landing.taken`); twin worlds fed the same commands hash alike and a save mid-ride loads
and plays on alike. A property (FsCheck): over drawn boards, exerts and alights, a wheel supplies only on ticks whose
construction saw an awake, exerting rider; proven to bite by dropping the exerting check once.

## Deliberately not handled

- A rider pausing lets a burner on its network light, and that fuel burns to its end (0101: a fuel lit burns out).
- Running in a wheel in vacuum drains the suit at `Crew.Exerted`, twice a resting breath: the hero is exerting.
- `Exert` reaches the host a round trip late: a hero boarding at a run turns the wheel a few ticks before its `Exert(false)`.
- No stamina, no output scaled by speed: on or off.
- Biomass left in a furnace from an older save sits there till taken out.
- Fat burns in a fuel slot and ferments in the fermenter; nothing prefers one use.
- No ferment mark: the fermenter's slot shows the crucible and "Smelts here" as the crusher's does; its own icon is a later pick.
- No new sounds: reused cues until the user picks.

"Burner generator" is the `burnerGenerator` machine; "a fire" or "a fuel burner" is any machine with a `FuelSlot` (the furnace,
the burner generator, the biofuel generator). "Plug" is where a cable meets a machine's picture (`MachineSpec.Plug`, pixels).

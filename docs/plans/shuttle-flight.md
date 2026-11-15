# Plan: The crew shuttle flies to a derelict and back

Status: plan, 2026-11-15, from the reviewed design (`design-author`, `design-reviewer`, the user's answers), `/review-plan`
folded in. Three slices, each a branch, worktree and PR, in CLAUDE.md's two stages: **Try** (built, played by the user),
then **Ship** (full suite, docs, PR) once the user says yes.

## Goal

Players pick a contact on the radar and set a course; up to four board the shuttle and Set off, arrive together beside the
vessel, explore it, board again and fly home, the shuttle docking on its own platform. The fuel cost is shown, never checked.

**Asked by the user** (2026-11-09, 2026-11-15): the radar shows distance and fuel (10 km = 1 l, the round trip paid at
departure, the return free); the radar's button sets a route, then the text "Route set, board the shuttle" («Маршрут
выбран, садитесь в шатл»), a course line and a lamp on the shuttle; board, Set off, a flight, the shuttle stops in space 30
cells left of the vessel; the jetpack crosses the gap (without one, fly home for it); fly home and dock. **Fuel is a stub**
until [fuel-liquid](fuel-liquid.md) lands.

**Where it is heading** (principle 2): now the trip out and back, four riders; next, fuel-liquid's store on `crewShuttle`,
checked and burnt in `SetOffProblem`/`SetOff`, the one place the stub leaves. The mechanic: a craft with several seats
carrying its riders to another surface and back; the pod keeps its single-rider `Flight` (merging needs a migration).

## Decided with the user (2026-11-15)

1. **Flying home while a friend is still out on the vessel is allowed.** The shuttle goes; the friend gets home only by
   dying or quitting, losing what they carry. An away friend never blocks the others.
2. **The radar's button reads «Проложить маршрут» / "Set course"**; the shuttle's reads «Отправиться» / "Set off".

## Open for the user

- **The lit slits and lamp:** the «Ледокол» pick page already draws them (crew-shuttle ADR, "not exported yet"). Export
  them in slice 1, or keep placeholders until slice 3's pick page? This plan assumes placeholders.

## Words

- *course* (маршрут): `Places.Course`, the contact the shuttle will fly to, one per world. *set off* (отправиться): the
  command `SetOff`. *voyage* (рейс): `Landing.Voyage`, the shuttle in the air with its riders.
- *place*: a seat's index in its machine, `0..Riders−1`. *slit*: one of the shuttle's four frosted windows; slit i is place i.
- *home*: the ship cell (its top-left) the shuttle stood at before it left, where it docks. *deck*: the ship's floor. *run*:
  the launch platform pieces in the deck under it, opened and shut whole. *shaft*: the cells under a run to the grid's edge,
  kept clear.
- *`Rules.Floats`*: a surface kind with no gravity in its open space: the ship's and a vessel world's, never a planet's.
- *out*: a rider whose hero is on this surface but passed out (`Out > 0`). *gone*: a rider with no hero on this surface
  (left the game, woke aboard, dev-transferred).
- *placeholder*: a plain look drawn in code until slice 3's pick: a warm rectangle over slit i, a small round lamp lit or
  dark, a thin line in the radar's green.

## Rules as the player meets them

1. **Radar.** A click on a contact shows `{n} km` and `{l} l`: `2 × ceil(|Offset| × KmPerUnit / KmPerLitre)`, each leg
   rounded up (today 250–1000 km, so 50–200 l). Its button «Проложить маршрут» / "Set course" sets the course: every peer's
   radar shows a line from the ship to it and the route text; every shuttle aboard lights its course lamp. Another pick
   replaces it (the last command in a tick wins); it clears when its contact runs out or is used. No cancel.
2. **Boarding.** E boards a shuttle: 4 seats, a fifth `landing.taken`. A rider takes the lowest free place; place i lights
   slit i warm; the rider is never seen. E again alights. No owner: anyone boards it, aboard or on a vessel.
3. **Launch.** Any rider presses «Отправиться» / "Set off" on the shuttle's panel. Aboard it needs a course, its run powered
   and its shaft clear. The contact is used up and the course cleared; the run opens, the riders fly, the run shuts after
   `FallTicks`.
4. **Flight.** `Shuttle.FlightTicks`, fixed. Riders fly on whatever happens to them: one out flies on and arrives out, then
   wakes aboard by the existing rule; one gone is not sent.
5. **Arrival.** One vessel world opens for the voyage; riders are sent in seat order, each to the spawn: an awake one is
   seated at the lowest free place, its client holding it at that place's floor; one out lies at the spawn, unseated. The
   shuttle stands in open space, its right edge `Shuttle.Gap` cells left of the frame's left edge, its bottom row on the
   spawn's row.
6. **Return.** Seated on the vessel, Set off flies home, free. It docks at its home platform, riders seated; its home built
   over or its run gone, it is laid there as its build cost.
7. **The shuttle never dies with the vessel.** When a vessel world closes (gone home, died and woke aboard, quit, a load), a
   shuttle there, standing or flying home, docks home empty, as in 6. While anyone is on the vessel it stays. Flying home
   while a friend is on the vessel takes the shuttle; the friend gets home only by dying (waking aboard) or quitting, and
   the vessel then closes as usual.

## Settled from the code

- **C-1. The home is a saved record** on the vessel's Landing, `Parked(Shuttle, Home)`, not read from the surface's parent:
  once the shuttle leaves, its home cell exists nowhere else, and the parent is always the ship (courses are set aboard).
- **C-2. `ISurfaces.Open(kind, seed)`, no parent:** the opener (whose `Inherit` the new surface takes) is the caller's
  surface, `Here`, as `Visit` uses the sender's; voyages count down on the ship.
- **C-3. Rule 7's hook is in `World.Close`** (Sim.cs:204), which runs after the sends: before a surface is marked closed,
  each of its modules implementing a new `IOutlives` gives up what outlives it (`object? Closing()`), handed to its twin on
  the ship (`Outlived(object)`); surfaces in id order as `Close` walks them, modules in run order, Landing's `Parked` before
  its voyages, each list in order. `ICarries` does not fit: it moves a peer's own with that peer; the shuttle is no peer's.
- **C-4. After a load:** the host releases every hero with `Leave` (Main.Sessions.cs:403), which sends each aboard; the
  vessel, empty, closes at that tick's end and C-3 docks its `Parked` shuttle on the ship.
- **C-5. "Launch flies pods only"** is read as `Riders == 1` (a new `MachineSpec.Riders`, default 1), not by kind (0113).
- **C-6. Who carries the shuttle:** only a voyage at `TicksLeft == 0` hands it over. There `Count` drops the gone riders and
  sends the rest in seat order; the voyage stays in the slice until they depart. `Depart` of its first rider in seat order
  carries the shuttle, of the others a seat, and the last removes the voyage. Before zero `Depart` of a rider returns null
  and leaves the voyage untouched; the next `Count` drops that rider as gone. Later riders find the shuttle (`Parked` on a
  vessel, the shuttle at `Home` aboard). No step-local state.
- **C-7. Riders keep their surface open until zero:** they stay on the surface they left, held at their place's floor, as a
  pod's rider does (`Flight.Floor`), so Core's `_where` lists them and `Close` (Sim.cs:209) keeps that surface. Core changes
  nothing for it.
- **C-8. The gap spot cannot fail:** the vessel world is open space round its frame (`Vessels.Margin` 128 > `Gap` + 24, a
  content test) and is opened that tick with nothing placed before the first rider arrives. `Grid.Place` throwing there is
  our bug.
- **C-9. Laid as its cost:** a dock with a rider lays it with `DropAt(firstRider, …)`, as the pod's (Landing.cs:325); an
  empty dock (C-3, an outbound voyage left riderless) lays each cost entry with `IItemsOps.LayDown` (Deliveries.cs:70), which
  counts nothing obtained. `DropAt` with `FloorStack.Nobody` would count it (Items.cs:563).

## Steps

### Slice 1 — seats (`feat/shuttle-seats`)

**S-1. Content.** `MachineSpec` gains `int Riders = 1` (Machines.cs:76, beside `Has`); `crewShuttle` (Machines.cs:281) gets
`Has: Lands | Seats, Riders: 4`. `ContentTests`: `Riders ≥ 1`, above 1 only on a `Seats | Lands` machine.
`CrewShuttleTests.IsTwentyFourCellsByTen…` expects the new traits.

**S-2. Landing seats with places** (Landing.cs):
- `Seat(int Pod, int Peer, int Place = 0)` (line 11; an added field meaning the first place: no bump); `Seats` sorted by
  (pod, place), which for old saves (every place 0) is their saved pod order; `Sit` (198) takes the lowest free place.
- `BoardProblem` (115): `landing.taken` when the machine's seats number `Riders`, not when it has a rider.
- `LaunchProblem` (132): a seat on a machine with `Riders > 1` is `landing.notSeated`, as the wheel's (line 137; C-5).
- Queries: `PlaceOf(peer)`, `RidersOf(machine)` (its seats, by place). `RiderOf` stays for one-seat machines.
- `FloorOf(kind, cell, width, place = 0)` (176): place i's column is `(2i+1) × Width / (2 × Riders) − 1` from the left,
  integer division rounding down; the pod's middle unchanged.

**S-3. Client.**
- `Seating.SeatOf` (crew/logic/Seating.cs:13) passes the rider's place; `SeatedHeroes.cs:64` draws no figure in a
  machine with `Riders > 1` (never seen).
- Every `MachineTrait.Lands` read that means "a pod" also checks `Riders == 1`: `PodSeats.cs:44`, `PodFlights.cs:67,91`,
  `Main.cs:130`, `PlatformScenario.cs:85,108`.
- `ShuttleLights.cs` (game/features/landing): slit i lit warm while place i is taken (placeholder); compared every frame,
  laid again on change, as `PodSeats`.
- `ShuttleKey.cs`: the shuttle's panel, returned by `LaunchOf` (Main.cs:236) for a `Lands` machine with `Riders > 1`
  (check the items panel opens for a machine with no slots on `Board`, Aim.cs:511; else open it on `SeatOf`): four seat
  marks, the destination line `No route`, the Set off key greyed with `places.noCourse` as its tooltip, built on `WristKey`
  as `LaunchKey`. Read on a timer.
- Text: `landing.setOff` Set off / Отправиться, `landing.noRoute` No route / Нет маршрута, `landing.home` Home / Домой
  (`logic/Landing.En.cs`, `.Ru.cs`); `places.noCourse` No route set / Маршрут не выбран (`features/places/logic`).
- **Play:** `-- --testbed --near=crewShuttle`, four windows with `--host` and `--join`: four friends board and alight.

**Tests** (`CrewShuttleTests`): four board, a fifth `landing.taken`; one alights from place 1, the next boarder takes 1;
`Launch` from a shuttle `landing.notSeated`; a seat freed when its hero passes out; `FloorOf` columns per place; a save
written before `Place` (seats without it) loads, keeps its seat order and plays on. Pod and wheel tests stay green.

**Ship:** full suite; README lines in sim/Landing (seats with places, `Riders`, the shuttle boarded) and
game/features/landing (`ShuttleLights`, `ShuttleKey`, Files table); `docs/map.md` Landing line.

### Slice 2 — the trip (`feat/shuttle-trip`)

**S-4. Content** `sim/Content/Shuttle.cs`: `KmPerLitre = 10`, `Gap = 30`, `FlightTicks = 200`, `FallTicks = 20`.
`ContentTests`: `Vessels.Margin > Shuttle.Gap + crewShuttle.Width` (C-8).

**S-5. Core** (Sim.cs, Module.cs):
- `ISurfaces.Open(string kind, uint seed)` (Module.cs:26; C-2): deferred like `OpenPlanet`, returns the id it will take.
  `_opening` (Sim.cs:42) becomes a list of (kind, `uint?` seed, opener), opened in the order asked after the phases (line
  191); a null seed (`OpenPlanet`'s) is drawn from the world's Rng as it opens, in list order, so planets keep today's
  draws. `OpenPlanet` (428) finds a planet already in the list first, so two calls a tick still give one id
  (SurfacesTests.cs:192); `Send`'s bounds check (401) counts the list.
- `IOutlives` (Module.cs) and its call in `Close` (204), as C-3.
- Test fakes of `ISurfaces` gain `Open` (CreaturesTesting.cs:14, DescentTests.cs:165, DropPodTests.cs:90,
  TemperatureTests.cs:50).

**S-6. Grid** (Grid.cs:678-679): off the ship, where `Rules.Floats`, a `Lands` machine needs no ground: an explicit branch
returning null before the `noGround` check. A planet keeps `grid.noGround` (`DropPodTests`).

**S-7. Places** (Places.cs; it already references Landing, line 46):
- State `int? Course` (null: no bump). Queries `Course`; `CostOf(Contact)`, litres for the round trip, each leg the integer
  ceiling `(a + b − 1) / b` of `a = |Offset| × KmPerUnit`, `b = KmPerLitre`, never `Math`; `SetOffProblem(peer)`: aboard
  `places.noCourse` first (no course, or its contact gone), then Landing's. So a second `SetOff` aboard in the tick of the
  first is `places.noCourse` (the course was just cleared); on a vessel it is `landing.notSeated` (the seats went).
- `SetCourse(int Contact)`: `places.notAboard`, `places.noContact`.
- `SetOff`: refused by `SetOffProblem`. Aboard: the contact removed, the course cleared,
  `ILandingOps.SetOff(peer, kind, seed)` with the class's vessel kind (as `ApplyJump`, line 127); elsewhere
  `ILandingOps.SetOffHome(peer)`.
- `Watch` (95), after contacts run out: the course clears when its contact is gone (a dev `Jump` too).
- `Fuzz`: `SetCourse` to a held contact or not, `SetOff`.

**S-8. Landing voyages** (Landing.cs):
- `Voyage(int From, List<int> Riders, int TicksLeft, int Home, string? Out = null, uint Seed = 0)`: `From` the cell it stood
  at on this surface, `Riders` in seat order, `Out` the vessel's kind outbound. `Parked(int Shuttle, int Home)`. Both lists
  added with initializers: no bump. `Voyages` kept in launch order, appended; a voyage's `Riders` replaced through `with`,
  never mutated in place.
- `ILandingQueries.SetOffProblem(peer)`: `landing.notSeated` (in no `Riders > 1` seat, or out); aboard `landing.noPlatform`,
  `landing.shaftBlocked`, `landing.unpowered`, `landing.cannotLeave`, as `LaunchProblem`'s lines 140–146. `ILandingOps`:
  `SetOff`, `SetOffHome`. `Rides` (158) counts voyage riders; `VoyageOf(peer)` for the client.
- **Leaving** (a `Fly` for shuttles): riders are its seats held (here, awake), in place order; aboard the run under it
  opens; the machine is cleared and its `Parked` record dropped; `Under`/`Shut` (209–270) take the machine's own height,
  not the pod's.
- **Count** (246), voyages after flights: gone riders are dropped (out ones stay); an outbound voyage shuts its run at
  `FlightTicks − FallTicks` and, left with no rider, docks home empty at once (C-9), run shut, contact still used. At zero
  outbound: `Surfaces.Open(Out, Seed)`, then `Send` each rider; homeward, `Send(rider, 0)`. One passing out on the zero tick
  is sent all the same and arrives out. A homeward voyage with no rider at zero waits there until its world closes (C-3).
- **Depart/Arrive** (334, 356) as C-6. Arriving on a vessel, the first rider places the shuttle at
  `(spawnRow − Height + 1) × width + Vessels.Margin − Gap − Width` (C-8) and adds `Parked`. Aboard it docks at `Home` when
  `PlacementProblem` there is null (the cells free and on a run; no power or shaft check), else it is laid there (C-9).
  Each awake rider is seated at the lowest free place; an out one is not.
- **`IOutlives`** (C-3): `Closing()` returns the homes of every `Parked` shuttle, then every voyage here; the ship's
  `Outlived` docks each home empty.

**S-9. Client.**
- Radar (ComputerPanel.cs, RadarScreen.cs): the button reads `computer.radar.setCourse` (the old `setOff` key goes) and
  sends `SetCourse` (ComputerPanel.cs:74); `{l} l` under the km (RadarScreen.cs:164, `computer.radar.litres`, from
  `CostOf`); the course line from the ship to the course contact, redrawn with the sweep (placeholder colour); the route
  text while a course is set (`computer.radar.route`). The panel reads `Course` with the contacts every quarter second.
  `Jump` stays on the dev panel, where it already is (DevPanel.cs:302, 317).
- `ShuttleLights`: the course lamp lit on every shuttle aboard while `Course` is set (placeholder).
- `ShuttleKey`: destination `{n} km · {l} l`, `No route`, or `Home` on a vessel; Set off sends `SetOff`, greyed with
  `Places.SetOffProblem`.
- Riders hidden and held at their place's floor of `From` during a voyage: `Seating` (SeatOf, Held, InPod),
  `HeroView.Others.cs:52`, `HeroView.Hands.cs:31`, `Main.cs:127`.
- Plain pictures: the shuttle vanishes and appears (the grid's); the rider sees the pod's flight screen, star lines both
  ways: `FlightOverlay`, `Whiteout` and `FlightTimeline.Total` (logic/FlightTimeline.cs:43) take a voyage as a descent of
  `Shuttle.FlightTicks` (a small `logic/` record both map to).
- Text, En/Ru: `computer.radar.setCourse` Set course / Проложить маршрут; `computer.radar.route` Route set, board the
  shuttle / Маршрут выбран, садитесь в шатл; `computer.radar.litres` {l} l / {l} л.
- **Play:** a new demo `--trip` (`new-demo` skill, game/dev/demos/ShuttleScenario.cs): the testbed, the hero by the
  shuttle; as it starts the host sends `Find("freighter")` and then `SetCourse` to it as commands, never writing state;
  `--trip=auto` boards, sets off, alights 3 s after arriving, boards again and flies home, for `tools/snap.ps1 -Movie`.
  Check the hangar's run (Testbed.cs:128) is cabled to power; cable it if not. Co-op with `--host`/`--join`.

**Tests** (`sim/Tests/ShuttleTripTests.cs`, a powered hangar map as `DescentTests`'; `SurfacesTests` for Core):
- Course: `SetCourse` refusals; replaced by a later one; cleared on expiry and on Set off; `CostOf` a property against
  the formula, never below the exact cost.
- `SetOff` refusals: `places.noCourse`, `landing.notSeated`, `landing.unpowered`, `landing.noPlatform`,
  `landing.shaftBlocked`, state untouched; a second `SetOff` in the same tick `places.noCourse`.
- Arrival: four riders in one world, all seated, the shuttle at the gap spot; a rider out mid-flight arrives out and wakes
  aboard; a rider passing out on the arrival tick arrives out, unseated; every rider gone before zero: docked home empty,
  no world opened, contact used.
- Gone mid-flight: a homeward rider wakes aboard, or is dev-transferred, before zero: exactly one shuttle after. A flies
  home with B, B quits during the flight: A still arrives seated.
- Return: docks home, riders seated aboard, the world closes; home built over, or its run gone: laid as its cost. Rule 7:
  the last hero off by `Leave`, by death, by a load: docked home empty; riders quitting mid-return: docked home.
- Core: `Open` deferred, ids in the order asked; a descent and a voyage ending on one tick; `Send` to an opening id; a
  planet's seed drawn as before. Grid: a `Lands` machine placed in a vessel's open space; a planet's `noGround` stays.
- Saves mid-flight, outbound and homeward, hash alike with a twin. A guest joining mid-voyage hashes alike through the
  arrival (`net/Tests/JoinTests.cs`).
- Invariant (FsCheck, `[Property]`): over boards, alights, courses, set-offs, leaves and runs with 1–5 peers, every shuttle
  built stands on one open surface or flies in one voyage; seats per machine ≤ `Riders`; a voyage rider is seated nowhere.
  It runs a twin fed the same commands and saves and loads one mid-voyage, the hashes compared, so voyages, the `Open` list
  and `IOutlives` are exercised (the generic suites' worlds hold no shuttle). Proven to bite by skipping the `Close` hook.

**Ship:** full suite; ADR `YYYY-MM-DD-shuttle-flight.md` (course, voyages, gap, rule 7's hook, fuel stub; supersedes
crew-shuttle's "next slices" line and vessel-worlds' radar Set off sending `Jump`); READMEs: sim/Places, sim/Landing,
Core's `Open`/`Close`, game/features/computer (its "No text on the radar" omission changes), game/features/landing;
`docs/map.md`; `docs/glossary.md` (маршрут, рейс, отправиться, шатл); `docs/concept.md`; `tools/adr-index.ps1`.

### Slice 3 — pictures and sounds (`feat/shuttle-pictures`), after the user's pick

Outline only. A pick page (CLAUDE.md "Art and sound picks"): the lit states (lamp, slits, flame), the drift-in, the
lift-off, the course line's colour, at the game's scale through every state. Once picked, exported through
`tools/anime-art`: the deck slides and the shuttle drops through; it drifts in from the left on the vessel and lifts off to
the left; docking heard as the pod's, sounds borrowed from `PodSounds`. Ship: the pick in its ADR, README, plan deleted.

## Deliberately not handled

- Fuel is never checked or burnt; the cost is shown only (the stub). Distance changes the cost only, not the flight time.
- No air or hold in the shuttle: suits cover the crossing, loot comes home in hands.
- No other way to the vessel: a friend who missed the launch cannot follow. No jetpack: fly home for one.
- The gap is measured from the frame's left edge, whichever side the door is on. Nothing checks the gap spot at runtime:
  the content test keeps the margin wide enough (C-8).
- A contact is used even if nobody arrives; no revisit. The pod's `Flight` and the shuttle's `Voyage` stay two records.
- Nothing takes the shuttle apart off the ship, as with a pod.
- The course is set only aboard (`places.notAboard`), from a computer's radar, by anyone, while riders sit too; they fly to
  the latest. A `SetCourse` applied before `SetOff` in the same tick changes where it flies.
- A course cleared by its contact running out shows only by the lamp going out and the route text going.
- Riders who all quit mid-return while a friend stays on the vessel: the shuttle docks home only once that world closes.
- Returning to a home built over by another shuttle, later riders sit in that one.

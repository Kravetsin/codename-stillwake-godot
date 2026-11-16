# Landing

## Files

| File | Purpose |
| --- | --- |
| `Landing.cs` | The launch platform opened and shut with power; the drop pod boarded, left, flown down to the planet and back; the crew shuttle seated four and flown to a vessel and home; the ship's course between its system's planets; the treadmill wheel rested. |

The launch platform (ADR `2026-10-24-the-launch-platform`), the drop pod (`2026-10-25-the-drop-pod`), its descent and return
(`2026-10-26-the-descent`), the crew shuttle (`2026-11-15-crew-shuttle`) and its flight (`2026-11-16-shuttle-flight`).

- **Depends on:** `Grid` (platform cells, machines; opening a run, placing), `Power` (`Feeds`: a run's power; `Rest`: a wheel
  no one runs), `Crew` (a hero here, awake, exerting; the spawn), `Items` (a hopper's load; a cost laid), `Fuel` (a store).
- **State** (`LandingState`, saved): `Seats` — `Seat(Pod, Peer, Place)` by (machine, place); `Flights` — `Flight(Peer, Up,
  TicksLeft, Home, Floor, Load, Fuel)` by peer; `Owners` — `Owner(Pod, Peer, Home)`, pods standing off the ship; `Voyages` —
  `Voyage(From, Riders, TicksLeft, Home, Out, Seed, Load, Fuel)` in launch order; `Parked` — `Parked(Shuttle, Home)`, shuttles standing
  off the ship. Voyages and parked are written only when any.

## Rules

- A platform piece (`_`, open `~`) is a doorway of its own group: the pieces along a row are one run, opened and shut
  whole by `SetPlatform(cell, open)`. Opening needs the run's plug, its left end, fed; shutting needs none. Open, the
  deck is a hole: space comes in.
- **Seats.** A machine with `Seats` holds `Riders` heroes (1 but the shuttle's 4), each at a place `0..Riders−1`;
  `Board` takes the lowest free place, `Alight` leaves. A seat holds while its machine stands and its hero is here and
  awake. `FloorOf(kind, cell, width, place)`: place i's column is `(2i+1) × Width / (2 × Riders) − 1` from the left.
- **The drop pod** (`dropPod`, one seat): aboard only on a platform, off the ship only on ground only this module places it
  on. Its fuel is a 20 l store (`sim/Fuel`, fuel-liquid), its slot the store's hopper. **The treadmill wheel** (`treadWheel`, no `Lands`): Landing rests it each
  construction but while its rider is here, awake and exerting (`Turning`).
- **`Launch`** flies a pod (`Riders == 1`): from the ship down, burning `Content.Fuel.Descent` (5 l) from its store, its run opened; from anywhere
  else back home, free. Flights count down in construction; a descent shuts its run `FallTicks` in, and one whose hero is
  gone or out ends at once, docked home empty. At zero it sends its hero to `OpenPlanet()`, a return aboard.
- **A pod goes where its hero goes** (`ICarries`): aboard it docks at home (home built over or run gone: its cost, load and fuel
  laid there); off the ship it lands by the spawn, outward from its column, left first, on level ground, its rider's
  (`Owners`). Seated again when it rode and its hero is awake. Off the ship only its owner boards or flies it.
- **The crew shuttle** (`crewShuttle`, 24 × 10, four seats, a whole run under it). Its course is Places'; `SetOff` (Places)
  calls `ILandingOps.SetOff(peer, kind, seed, fuel)` aboard, `SetOffHome(peer)` off the ship. Its riders are the seats held, in
  place order; aboard its run opens and the round trip (Places' `CostOf`, in mL) is taken from its 200 l store, short of it
  `landing.noFuel`; its hopper and the rest of its fuel fly with it (`Load`, `Fuel`), put back wherever it stands again;
  its machine, seats and `Parked` go; a `Voyage` of `Shuttle.FlightTicks` starts.
- Voyages count after flights: riders gone from this surface are dropped, out ones fly on. Outbound the run shuts
  `Shuttle.FallTicks` in; left riderless it docks home empty at once, the contact still used. At zero outbound
  `Surfaces.Open(Out, Seed)` opens one vessel world and every rider is sent there in seat order; homeward aboard. A
  homeward voyage with no rider waits at zero till its world closes.
- `Depart` at zero: the first rider brings the shuttle, the rest a seat, the last ends the voyage; before zero nothing.
  On a vessel the shuttle stops in open space, its right edge `Shuttle.Gap` cells left of the frame, its bottom row on the
  spawn's (`Parked`); aboard it docks at home if it fits, else its cost is laid there (to its first rider, or to nobody).
  Each awake rider sits at the lowest free place.
- **Rule 7** (`IOutlives`): a vessel world closing hands the ship the homes of its parked shuttles, then of its voyages;
  each docks home empty, its load and fuel in it. Flying home while a friend is on the vessel is allowed.
- **The ship's course** (2026-11-16-solar-system), aboard: `At` (from 0) and `Course(To, TicksLeft)`. `PlotCourse(planet)` burns
  `JumpFuel` (8 l) from the `Engine`'s store (the lowest-id machine with an `Exhaust`), closes the planet below for good
  (`LeavePlanet`) and counts `FlightTicks` down in construction; at zero `At = To`, and the next descent opens a new seed.
- Riders and seated heroes stay where they were sent; the client holds them at their place's floor.

## API

- **Queries:** `Problem(cell, open)`, `Powered(cell)`, `RunOf` (static); `BoardProblem`, `LaunchProblem`, `SetOffProblem`
  (`ILandingQueries`); `SeatOf`, `PlaceOf`, `RiderOf`, `RidersOf(machine)`, `Seats`; `FlightOf`, `VoyageOf`, `Voyages`,
  `Parked`; `At`, `Course`, `CourseProblem`, `Engine`; `Rides` (seated, flying or voyaging); `Returns`; `FloorOf` (static); `PodSpec`, `CraftSpec`; `Turning`.
- **Refusals:** `SetPlatform` — `landing.noPlatform`, `landing.unpowered`. `Board` — `landing.noPod`, `landing.seated`,
  `landing.taken` (every place taken), `landing.notYours`. `Alight` — `landing.notSeated`. `Launch` — `landing.notSeated`
  (no pod, or a shuttle's seat); aboard `landing.noPlatform`, `landing.shaftBlocked`, `landing.unpowered`,
  `landing.noFuel`, `landing.cannotLeave`, `landing.inFlight`; off the ship `landing.notYours`. `PlotCourse` — `landing.hereAlready`,
  `landing.inFlight`, `landing.noEngine`, `landing.engineUnpowered`, `landing.noFuel`, `landing.noExhaust`, last `landing.crewAway`
  (a hero off the ship or a pod flying from it). `SetOffProblem(peer, fuel)` — `landing.notSeated` (no
  shuttle seat, or out); aboard `landing.noPlatform`, `landing.shaftBlocked`, `landing.unpowered`, `landing.noFuel`, `landing.cannotLeave`;
  off the ship `landing.notYours` (a shuttle with no home, a dev's).
- **Fuzz:** one turn in 8: a launch, else a board, a leave or a platform set; one in 12 a course. `SetOff` is Places'.

## Invariants

- A run is all shut or all open; a shut run never opens without power. Its shaft stays empty (Grid, Building).
- A course starts only with every hero and pod aboard and no planet open (`CourseTests`' property).
- No machine seats more than its `Riders`; a hero sits in one place at most; every seat's machine stands, its hero awake.
- Every pod stands on one surface or flies in one flight. Every shuttle stands on one open surface or flies in one voyage,
  a voyage's rider sits nowhere, and its fuel is what it had less each round trip set off on (`ShuttleTripTests`' property,
  with a twin saved and loaded mid-voyage).

## Deliberately not handled

- A run open when its power fails stays open; a piece added to a run's left moves its plug. Boarding asks no reach (0002).
- No picture here: the client draws the deck, the fall, the flight and the touchdown from the ticks.
- A launch shuts a run opened by hand; a hero on a run that opens falls out of the ship.
- The shuttle's return burns nothing; laid as its cost, its fuel's part of a litre is lost, as a pod's.
- Distance changes the cost only, not the flight. A contact is used even when nobody arrives.
- No air or hold in the shuttle; loot comes home in hands. Nothing takes a shuttle or pod apart off the ship.
- The gap is measured from the frame's left edge, whatever side the door is on; nothing checks the spot at runtime.
- Riders who all quit mid-return while a friend stays: the shuttle docks only when that world closes. A home built over
  by another shuttle: later riders sit in that one.

# Landing

## Files

| File | Purpose |
| --- | --- |
| `Landing.cs` | The launch platform opened and shut with power; the drop pod boarded, left, flown down to the planet and back. |

The launch platform (ADR `2026-10-24-the-launch-platform`), the drop pod (ADR `2026-10-25-the-drop-pod`) and its descent and
return (ADR `2026-10-26-the-descent`); later the landers that drop with it.

- **Depends on:** `Grid` (`IGridQueries`: the platform cells and the pods; `IGridOps`: opening a run, a pod lifted and placed),
  `Power` (`IPowerQueries.Feeds`: whether a run has power enough), `Crew` (`ICrewQueries`: whether a hero is here and awake,
  the spawn), `Items` (the pod's slot: fuel burnt, its load lifted, put back or laid).
- **State** (`LandingState`, saved): `Seats` — `Seat(Pod, Peer)` by pod id; `Flights` — `Flight(Peer, Up, TicksLeft, Home,
  Floor, Load)` by peer, in the slice of the surface the pod left; `Owners` — `Owner(Pod, Peer, Home)` by pod id, the pods
  standing off the ship. Seats new to format 32; flights and owners added later with initializers, none.

## Rules

- A platform piece (`Structure.PlatformClosed`, `_`; open `PlatformOpen`, `~`) is a doorway of its own group
  (`StructureGroup.Platform`): the pieces side by side along a row are one doorway (`Join.Run`), opened and shut whole.
- `SetPlatform(cell, open)` sets the run at `cell` as asked, whole. Opening needs its plug fed (`Feeds`); every cable to a run
  plugs in at its left end (`StructureSpec.Plug`, `PowerNodes.NodeAt`). Shutting needs none. `SetDoor` refuses a platform.
- Open, the deck is a hole (no trait): space under a hull's floor comes in and its room turns to space, air and gravity
  gone. Heroes pass it both ways. A stack on the deck stays.
- **The drop pod** (`dropPod`, `Seats` and `Lands`): aboard only on a platform (`grid.noPlatform`), off the ship only on
  solid ground, where only this module places one (`grid.noGround`). `Board(pod)` seats the sender's hero; `Alight` takes it
  out. Its one slot takes biofuel only, by hand.
- A seat holds while its pod stands and its hero is here and awake.
- **`Launch`** flies the sender's pod: from the ship down, burning `Content.Landing.DescentFuel` biofuel and opening its run
  under it; from anywhere else back to its home aboard, free. Its machine and seat go; its slot's stack flies as its load.
  A hero gone or out on the tick it launches launches nothing (its seat lasts till derivation); a flying hero boards none.
- Flights count down in the construction phase (the grid changes only in commands or construction). A descent shuts its
  run `FallTicks` after its launch, whether or not it was open before; one whose hero is gone or out ends at once, docked
  home empty, no planet opened; one passing out after it on its last tick is sent all the same. At zero a descent sends its
  hero to `ISurfaces.OpenPlanet()` (an open planet, else the first, opened once every surface ran), a return aboard. A dev
  transfer mid-fall shuts the run the descent opened.
- **A pod goes where its hero goes** (`ICarries`, after Crew and Items): its flight, or, off the ship, the pod it owns,
  whatever moved the hero — a flight ending, `Leave`, a wake aboard, a load, a dev transfer.
  - Aboard it **docks** at home, its load put back; home built over or its run gone, its build cost and load are laid there.
  - Off the ship it **lands** by the spawn: from the spawn's column outward, left first, the first anchor whose four column
    tops are level and where it fits; it is its rider's (`Owners`). Nowhere, it is laid at the spawn as its cost and load.
  - Its hero is seated in it when it rode (a flight, not a pod following its owner) and is here and awake.
  - It minds the grid, not plans: a plan under it waits on its last tick (Building) till the pod is gone.
- Off the ship only its owner boards or launches a pod (`landing.notYours`). Nothing takes a pod apart there: Building marks
  nothing where tools break.
- The hero arrives at the surface's spawn (`Send` carries no cell, and the pod is placed after Crew arrives); its client
  holds it at the pod's floor (`Floor`, `FloorOf`) and reports it there within a round trip. The sim keeps no seated position.

## API

- **Queries:** `Problem(cell, open)`, `Powered(cell)`, `RunOf(grid, cell)` (static); `BoardProblem(peer, pod)`,
  `LaunchProblem(peer)` — what the pod's panel greys its button with; `SeatOf(peer)`, `RiderOf(pod)`, `Seats`;
  `FlightOf(peer)`; `Returns` — the key's Return; `FloorOf(cell, width)` (static) — where a pod's rider sits.
- **Commands:** `SetPlatform(cell, open)` — `landing.noPlatform`, `landing.unpowered`. `Board(pod)` — `landing.noPod`,
  `landing.seated` (in a pod or flying), `landing.taken`, `landing.notYours`. `Alight` — `landing.notSeated`. `Launch` — `landing.notSeated`;
  aboard `landing.noPlatform`, `landing.unpowered`, `landing.noFuel`, `landing.cannotLeave` (a world that cannot open
  surfaces); off the ship `landing.notYours` (another's pod, or one nobody came down in).
- **Fuzz:** one turn in 8: a launch, half of them from a seated hero, an eighth from others; else a board or a leave a third,
  a set of a platform the rest.

## Invariants

- A run is all shut or all open after any `SetPlatform`; a shut run never opens without power enough.
- A pod has at most one rider and a hero at most one pod; every seat's pod stands and its hero is awake after a tick.
- Every pod is in one place: standing on one surface or in one flight; a hero flies one at most, seated in none meanwhile.
  Every owned pod stands by its owner, or its owner flies. Biofuel goes only by a descent's five; a twin fed the same, saved
  and loaded, hashes alike (`DescentTests`).

## Deliberately not handled

- A run open when its power fails stays open; a piece added to a run's left moves its plug.
- No slide as it opens; no picture of a flight yet (slice 2): the rider's screen whites out.
- Boarding asks no reach: peers are trusted (0002).
- A launch shuts a run opened by hand before it. A descent leaves its room without air: crewmates in it breathe their
  suits; a hero standing on the run when it opens falls out of the ship.
- A pod whose home was built over while it was down is laid as its cost on return; one that finds nowhere to land likewise.
- A pod over ground dug away under it stays standing. Opening the first planet freezes every peer for its tick (~1.7 s).
- A flying hero breathes the room it was held in, its suit covering the flight.

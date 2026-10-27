# The drop pod flies its rider down to the planet and back

- Status: accepted
- Date: 2026-10-26

## Context

Heroes reached a planet only by the dev panel (0076: "dev commands until the drop pod"). The drop pod
(`2026-10-25-the-drop-pod`) seated a hero on a launch platform and did nothing more. The user asked for the descent by play:
sit in the pod, confirm, the platform opens, the pod flies down while the planet is made, lands, and the hero gets out; and
back again, only in the pod one came down in. Designed and cut down in the session's design (`docs/plans/descent.md`); this
is its first slice, the sim with a button and a white-out, the pictures of the flight to come.

## Decision

- **One command both ways.** `Launch` flies the sender's pod: from the ship down, from anywhere else back aboard. Aboard it needs
  the pod on a run (`landing.noPlatform`), the run powered (`landing.unpowered`), five biofuel in its slot (`landing.noFuel`,
  `Content.Landing.DescentFuel`) and a world that can open surfaces (`landing.cannotLeave`); off the ship, the sender's own pod
  (`landing.notYours`). A return burns nothing. The pod's panel carries a Descend or Return key, not a dialog.
- **A launch opens the run and a flight shuts it** `FallTicks` later, whether it was open before or not; the room vents as
  when opened by hand. A pod is no machine while it flies: its machine, seat and slot go into a `Flight` in the slice of the
  surface it left.
- **A pod goes where its hero goes.** Landing is `ICarries`: its flight, or off the ship the pod its hero came down in, crosses
  with the hero by whatever moved it — a flight ending, `Leave`, a wake aboard, a load, a dev transfer — so no new trigger
  exists and the own-pod rule never strands anyone. A hero rides in only at a flight's end; a pod following its owner
  arrives empty.
- **The planet is shared.** `ISurfaces.OpenPlanet()` gives the lowest open planet, else opens `Content.Landing.Planet` once every
  surface has run, before the sends, by the ship, from the next tick on: two descents ending together share it. One planet
  kind is queued a tick (`_opening`), as only descents open one.
- **Landing and docking.** Off the ship a pod lands by the spawn: from its column outward, left first, four level column tops
  it fits over (Grid's `Lands` reads the surface: off the ship solid ground, `grid.noGround`). Aboard it docks at home. Either
  failing (home built over, no level ground), it is laid as its build cost and load. Nothing takes a pod apart off the ship:
  Building marks nothing where tools break.
- **Flights count down in the construction phase,** as the grid changes only in commands or construction; a descent whose
  hero left or passed out ends there, docked home, before any planet opens. A hero passing out later on that very last tick
  is sent all the same: its pod lands, owned, and follows it home when it wakes aboard.
- **Its rider is one hero, awake.** A flying hero boards no other pod (`landing.seated`), and a hero gone or out on the tick
  it launches launches nothing (`landing.notSeated`): its seat lasts till the derivation phase, its fuel unburnt.
- **A platform keeps its shaft clear.** The column under each platform cell, to the grid's edge, is the pod's way out: a
  platform is placed only over one empty of anything solid (`grid.shaftBlocked`), and no structure or machine goes in one
  (`grid.underPlatform`), built or planned (Building checks plans as the grid checks what is built). Back walls, cables and
  pipes are behind the fall and stay allowed. A platform taken apart frees its shaft. While building, every shaft shows a
  red hatch and a platform's ghost its own, green where empty and red where not (`Shafts`). A run built before this, over
  something, launches nothing (`landing.shaftBlocked`).
- **A pod minds the grid, not plans.** A plan a pod docks or lands over waits on its last tick while the grid refuses it
  (Building), as a wall's does on a hero, rather than building into the pod.
- **The rider's client** holds its hero where the pod stood on the surface it is on (`Flight.Floor`), hidden for every peer,
  its hands idle; its screen whites out over the flight's last second, so the frame frozen while a planet opens is white,
  and fades in on arrival.

## Deliberately not handled

- The pictures of a flight (the fall, streaks, re-entry, touchdown, docking): the next slice. Peers aboard see the pod vanish.
- Opening the first planet freezes every peer for its tick, about 1.7 s; growing its ground ahead on a thread would save a
  third of it and was turned down.
- The hero arrives at the surface's spawn; its client reports it at the pod within a round trip (`Send` carries no cell).
- A launch shuts a run opened by hand; a descent leaves its room without air; a hero on the run as it opens falls out.
- Closing an empty planet, choosing a planet or a landing spot, landers: later, with the jump.

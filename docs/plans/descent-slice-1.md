# Plan: the descent, slice 1 — down and back by playing

Design: [descent.md](descent.md). This slice is the sim, the panel button and the white-out: a player goes down to a
planet and back with no dev panel, every co-op case handled; the fall, streaks, touchdown and docking pictures are slice 2.
Branch `feat/drop-pod-descent`, one PR. Time is ticks, 20 a second (`Sim.TicksPerSecond`).

## What the player can do after it

1. Sit in a pod on a powered run with 5+ biofuel; press **Descend** in its slot panel. The run opens (the room vents),
   the pod is gone from the deck, the run shuts a second later. The rider's screen fades to white over the flight's last
   second and fades in on the planet, seated in the pod beside the spawn. E gets out.
2. Board the same pod (E): the panel shows **Return**. Pressed, white again, and the hero is seated in the pod back on its
   run aboard.
3. Refusals the player meets: a friend's pod on the planet ("Not your pod", `landing.notYours`); a slot of 4 biofuel greys
   Descend ("Needs 5 biofuel", `landing.noFuel`); a pod on a planet cannot be taken apart (`building.podStays`).

## How the pieces fit (read before the tasks)

- **A flight ends by crossing.** A descent or return ending calls `Where.Send(peer, target)`. Core's `Move` then calls
  `Depart` on every carrying module of the surface left and `Arrive` on the twin, in run order: Crew (the hero), Items (the
  inventory), then Landing — last in the registry — which gives up what it holds of the peer and, on arrival, places the
  pod. So a pod goes with its hero by any cause (an ending flight, Leave, a wake aboard, a load, the dev panel): no new
  trigger.
- **A flight lives in the slice of the surface it left:** a descent in the ship's Landing, a return in the planet's.
- **The pod is not a machine while it flies:** `Launch` clears its machine and removes its seat itself (not left to the
  derivation rule), and the flight remembers the rider. Arrival places a new machine, with a new id.
- **"Aboard" is `Where.Here == 0`; "off the ship" is any other surface,** a planet or the test strip alike. Nothing in
  this slice branches on the surface's kind except `Planet()`.
- **Deviation from the design: the hero arrives at the surface's spawn, not at the pod.** `Send` carries no cell, and when
  Crew's `Arrive` runs (before Landing's) the pod is not placed yet. The rider's client holds it at the pod's floor from
  its first frame there (`Seating`) and reports that position by the usual crew commands, so the sim has the hero at the
  spawn for the round trip of that first report: one tick on the host, a few on a guest. The pod lands a few columns
  from the spawn, in the open; air and creatures reading the spawn for those ticks change nothing a player sees.

## Tasks

### D1-1 — Content and Core

- `sim/Content/Landing.cs`: `DescentFuel = 5`, `FallTicks = 20`, `DescentTicks = 160`, `AscentTicks = 100`,
  `Planet = PlanetTypes.First.Id`; a `///` on each.
- `ISurfaces.CanOpen` (sim/Core/Module.cs, `Where` in sim/Core/Sim.cs): whether this world can open surfaces (`World.CanOpen`).
- `ISurfaces.Planet()`: the lowest-id open surface whose kind is a `PlanetType`, else one queued to open this tick, else
  queues an open of `Content.Landing.Planet` and returns the id it will have (`_surfaces.Count` plus those queued before
  it). Called only where `CanOpen`; otherwise our bug, it throws.
- `World.Open` is split: `Open(kind, opener)` does today's work, the opener's surface giving `Inherit` its twins;
  `Open(OpenSurface c)` calls it with `SurfaceOf(c.From)`; queued opens with opener 0, the ship (its creative flag
  copied). The seed is drawn from the world's Rng when the open is made.
- The step makes queued opens after every surface has run, before `_sends`, in the order queued, then clears the queue. The
  queue is never saved (it is empty between steps). A surface opened there runs its systems from the next tick on, where
  one opened by a command runs on its own tick: write it in 0076's slice table note or the Core README.
- `Where.Send` accepts an id queued to open this tick; past that it still throws (our bug).
- Tests (`SurfacesTests`): `Planet()` twice in a tick opens one surface; with `Small` open it returns that and opens none;
  a send to a queued id lands there; the queued open inherits the ship's creative flag.

### D1-2 — Grid and Building: a pod off the ship

- Grid `PlacementProblem` for `Lands`: aboard as today (`grid.noPlatform`); off the ship every cell of the row under the
  footprint `Supports` (`grid.noGround`). Players cannot plan machines off the ship (`building.notHere`), so only Landing
  reaches the second branch.
- Building `MarkProblem` for `Deconstruct`: a `Lands` machine off the ship is refused `building.podStays`. Without it a
  friend takes apart someone's pod on a planet and strands them, the one way back gone. (`Hew` strikes walls, pieces and
  plants, never machines: checked.)
- Keys: `grid.noGround` in `game/features/grid/logic/Grid.En.cs`, `building.podStays` in
  `game/features/building/logic/Building.En.cs`. README lines in Grid and Building.
- Tests: on `Small`, a pod over ground places, over a gap refuses `grid.noGround` (`DropPodTests`); `Deconstruct` of a
  landed pod refuses `building.podStays`, aboard it is allowed (`BuildingTests`).

### D1-3 — Landing: launch, fly, land, return

Constructor gains `IItemsQueries`, `IItemsOps` (Registry.cs wiring). State, new fields with empty initializers, no format
bump:

- `Flights` — `Flight(Peer, Up, TicksLeft, Home, Floor, Stack? Load)`, by peer ascending. `Up` is true for a return.
  `TicksLeft` starts at `DescentTicks` or `AscentTicks`. `Home` is the ship cell the pod stood at; `Floor` is the cell its
  rider is held at while flying, on the surface the flight left (the pod's floor where it stood: home aboard for a
  descent, its planet cell for a return). `Load` is the slot's stack lifted out.
- `Owners` — `Owner(Pod, Peer, Home)`, by pod ascending: pods standing off the ship, each placed only by `Arrive`.

Command `Launch` (no payload: the sender's seat says which pod; the surface says which way). Refusals, in order:

1. `landing.notSeated`.
2. Aboard: `landing.noPlatform` (the row under the pod no longer a run's), `landing.unpowered` (the run's `Powered`),
   `landing.noFuel` (fewer than `DescentFuel` biofuel in its slot), `landing.cannotLeave` (`!CanOpen`: a world that cannot
   open surfaces, as the generic suites build).
3. Off the ship: `landing.notYours` when `Owners` has no entry for the pod naming the sender — a pod drawn on a map or put
   there by the dev panel has no home to return to.

`LaunchProblem(peer)` is the public query of the same refusals; `FlightOf(peer)` the public query of a flight (`Up`,
`TicksLeft`, `Floor`) or null.

Apply: take `DescentFuel` (descent only); lift the rest of the slot (`TakeFrom`) into `Load`; clear the machine and remove
its seat; descent: `SetDoorway(run, true)`; return: `Home` from its `Owners` entry, the entry removed. Add the flight.

Systems:

- Derivation: a **descent** whose hero is gone (`HeroOf` null) or out ends here, docked home (`Dock`, below), its run shut
  if not yet. A **return** goes on regardless: its hero is sent aboard at zero, out or not.
- Simulation: each flight counts down. A descent shuts its run (`SetDoorway(home run, false)`) when `TicksLeft` reaches
  `DescentTicks - FallTicks`, 20 ticks after its launch, whether or not the run was open before (design trade-off); a
  second launch from the run in those ticks shuts it at its own count, its pod already through. At zero, the descent's
  hero is checked again (out on this very tick → `Dock`, not sent, so no planet opens for a hero waking aboard); else a
  descent → `Send(peer, Planet())`, a return → `Send(peer, 0)`.

`Dock(flight)`, aboard, shared by the derivation system and `Arrive`: place `dropPod` at `Home`; placed, `PutIn` the load.
Refused there (built over, run gone), `DropAt` its build cost and `LayDown` the load at `Home`. The burnt biofuel is never
given back.

`ICarries`:

- `Depart(peer)`: the peer's flight, given up; else, off the ship, the pod it owns — machine cleared, slot lifted into a
  load, entry removed — as a `Flight` with `TicksLeft` 0 (a pod following its owner, not a flight ending); null for
  neither.
- `Arrive(peer, carried, cell)`:
  - Aboard: `Dock`.
  - Off the ship: the landing search. The pod is `dropPod`'s width, 4 columns, anchored by its left column. From the
    spawn's column outward, left first on a tie, each anchor is tried: a column's top is its first cell from row 0 down
    that `Supports` (a pod already down counts, so pods side by side never share columns); the four tops must be equal
    and the footprint above them clear (`PlacementProblem` null). Found: place it, `PutIn` the load, add
    `Owners(pod, peer, Home)`. None found (a planet dug flat to nothing, a strip too narrow): `DropAt` the build cost and
    `LayDown` the load at the spawn, no entry — the hero is down without a way back but by dying, as on a planet reached by
    the dev panel.
  - Seat the rider only when the carried flight ended (`TicksLeft` 0 by counting, not `Depart`'s pod) and its hero is
    here and awake. A pod following its owner (Leave, a wake aboard, a load, a dev transfer) docks empty. A flight carried
    by a dev transfer mid-way arrives as one ending: placed, its rider seated.

`Fuzz`: one turn in 8 as today; a quarter of them `Launch`, mostly when the drawn peer sits in a pod.

README: Rules, API (`Launch`, `LaunchProblem`, `FlightOf`), Invariants, and Deliberately not handled — the design's
trade-offs plus: a descent leaves its room without air, and crewmates in it breathe their suits; a hero standing on the run
when it opens falls out of the ship; a pod that finds nowhere to land is laid as its cost. Glossary line for `Launch`; map
line.

Tests (`DropPodTests`, a scenario per rule and refusal):

- Each refusal: `notSeated`, `noPlatform`, `unpowered`, `noFuel` with 4 biofuel, `cannotLeave`, `notYours` (a friend's pod
  and a pod with no owner).
- The launch burns 5, the rest of the stack comes down and back; the run opens at once and shuts 20 ticks later, also
  after a hero leaves mid-fall; two descents from one run on nearby ticks, the run shut, both pods gone.
- With `Small` open, a descent lands beside its spawn, seated; a second lands beside the first; two descents ending on one
  tick share one planet; the slow one with nothing open opens `First` once.
- A return docks home seated; with home built over, the cost and load laid there.
- Leave mid-descent, Leave on the planet, a wake aboard after passing out on the planet, Leave mid-return, passing out on
  a descent's last tick: the pod is home each time, nobody seated, no planet opened in the last case.
- `Launch` twice in one tick (the second `notSeated`); `Launch` and a friend's `Board` of the same pod in one tick.
- Save and load mid-descent and mid-return play on alike (`Reload`); two worlds fed the same descent and return hash alike
  on the tick the planet opens and on the tick the pod docks.

Invariants (FsCheck, `DropPodTests`): every pod in one place (standing on one surface or in one flight, never both); every
`Owners` entry's machine stands and its owner is on that surface or flying; over a run of launches and returns with no
hand edits to slots, biofuel in pod slots, loads and laid stacks falls only by `DescentFuel` a descent. Prove one bites by
breaking the rule once.

### D1-4 — Client: the button, the held hero, the white-out

- `game/features/landing/LaunchButton.cs`: a `Func<int, Control?>` like logistics' `Served` badge, on a new
  `InventoryPanel` property beside `Served`, set by `Main`, shown in a pod's frame: **Descend** aboard, **Return** off the
  ship, greyed with `T(LaunchProblem(peer))` read on the panel's timer; pressing sends `Launch`. The panel opens with
  boarding (E) as today; closed while seated, or on arrival (`Follow` closes it on a surface switch), E, E brings it back:
  accepted, written in the README.
- `Seating.SeatOf` also holds a flying hero: at `FlightOf(peer).Floor` on the surface it is on, so a return holds it at
  the pod's planet floor, a descent at its home floor aboard. A flying hero is hidden and takes no keys at all.
- `HeroView.Others` hides a peer's hero while `FlightOf(id)` is non-null, as it does while seated.
- `game/features/landing/logic/Whiteout.cs` (Godot-free, tested in `game.tests`): the white's opacity from the own
  flight's `TicksLeft` and the frames since the client's surface last switched — rising over the flight's last
  `FadeTicks` (20, a client constant: the sim never reads it), full at zero and while the switch freezes the frame,
  falling over half a second after the switch; and clear at once whenever there is no own flight and no recent switch,
  so a flight ended any other way (a docked descent, a load mid-flight) never leaves the screen white.
- `game/features/landing/FlightOverlay.cs`: a `CanvasLayer` over everything, Main's, surviving `Follow`, drawing
  `Whiteout`.
- `logic/Landing.En.cs`: `landing.noFuel`, `landing.notYours`, `landing.cannotLeave`, the two button labels.
- README (landing client): files, rules.

### D1-5 — Seeing it

- A dev scenario (`new-demo` skill): `--descent` — a hero seated in a fuelled pod on a powered run, `Launch` sent, the
  return sent once seated on the planet. `tools/snap.ps1 -Movie` sheets of: the deck opening and the room venting, the
  white, the hero seated on the planet, the Return panel, home again. Look at every frame.
- ADR `docs/decisions/2026-10-26-the-descent.md`: what stays true (one command both ways, a pod crosses with its hero,
  own-pod return, a pod off the ship stays, docking or landing nowhere lays its cost, the opening hitch); the design's
  trade-offs.

## Done when

- `tools/test.ps1` green, `dotnet format Stillwake.sln --verify-no-changes` clean, the docs guard green.
- The three player sequences above walked in the game (movie sheets looked at), host alone and host plus a guest.
- Landing, Grid, Building and client landing READMEs, map and glossary current; the ADR written.

## Risks to watch

- **Fuzz cost:** a fuzzed `Launch` that opens `First` costs ~1.7 s. Launches need a seated hero on a powered run with
  5 biofuel, rare in fuzz maps, and the world's own fuzz opens `Small` first, which `Planet()` reuses. If the generic
  suites slow down, measure before changing anything.
- **A flying hero breathes the vented room:** held at its home floor, its suit drains for the flight's 8 s of its 45 s
  (`Content.Crew.Suit`, a unit a second); if a playtest shows otherwise, a flying hero breathes nothing, a later change.

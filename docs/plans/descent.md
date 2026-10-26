# Design: the drop pod's descent and return

Status: slice 1 built (the sim, the Descend/Return key, the white-out; ADR `2026-10-26-the-descent`, which records where it departs from this design); slice 2, the cinematics, next.

## Problem

The only way to a planet is the dev panel (`OpenSurface`, `Transfer`). A player can build a drop pod, fuel it and sit in
it, and nothing happens; there is no way down by playing, and none back but dying.

## Rules (confirmed with the user)

- If a planet is open (a hero is down), the pod lands on it, near the pods already there; otherwise a new planet opens
  (`PlanetTypes.First`).
- A descent burns 5 biofuel from the pod's slot; the return burns none. Short of 5, the descent is refused.
- The launch opens the platform run itself (the room vents) and shuts it after the pod has gone. A dark run cannot launch.
- The confirmation is a button in the pod's slot panel, not a dialog.
- A hero goes back only in the pod they came down in; it stays on the planet as a machine they board again.

## Player experience

**Descent**

1. A hero boards a pod on a powered run (E). The slot panel opens: biofuel and **Descend**.
2. Pressed, the run opens, the room vents, the pod drops through the hole. Peers aboard see it fall and the deck shut
   behind it.
3. The rider's screen turns to a flight at huge speed: streaks, a re-entry glow. At its end the screen whites out while
   the planet is made, and fades in on it.
4. The pod settles on flat ground near the planet's spawn (legs, dust), beside any pods already down. The hero is
   seated; E (`Alight`) gets out.

**Return**

5. Back at the pod, E boards it and the panel shows **Return** (no fuel). It lifts off, flies, and docks rising through
   its home run, drawn opening and shutting under it (the sim keeps it shut: nothing vents). The hero is seated aboard.

**Goes wrong:** the slot holds 4 biofuel: the button is greyed, "Needs 5 biofuel" (`landing.noFuel`); nothing opens.

**Co-op sequences**

- **Two descents end on one tick, no planet open:** the first opens it, the second finds it queued; one planet, two pods.
- **A rider's peer leaves, or the hero passes out, mid-descent:** the flight ends at once, the run shuts if it has not,
  the pod docks home empty; the burnt fuel stays burnt.
- **Mid-return:** the hero is sent aboard anyway, the pod with it.
- **The owner leaves, dies or is loaded while the pod stands on the planet:** every one of these already sends the hero
  to the ship (Crew's `Leave` and waking, `Main.Sessions` on load); the pod goes with them through `Depart`, home and
  empty. No new trigger. A death leaves the body on the planet; the pod is aboard to go and fetch it.
- **A hero boards a friend's pod on a planet:** refused `landing.notYours`. "Own" is the peer the pod came down with,
  saved per pod. Aboard, pods have no owner.
- **Two launches from one run, the second while the first's hole is still open:** each flight shuts the run at its own
  `FallTicks`; the second pod is already through by then (it fell in the first's hole or opened it again).
- **A friend builds over a pod's home spot while it is down:** the return docks it there if it still fits; otherwise it
  falls apart at home into its build cost, its stack laid beside. Accepted (see Trade-offs).
- **A save mid-flight** holds the flight; it resumes after loading.

## Out of scope

- Landers (oxygen tank, cargo hold) and the jump.
- Closing a planet: deferred. Pods reuse the open planet, so they only ever make one; the jump is the first caller.
- Choosing a planet or a landing spot; steering the flight.
- Growing the ground ahead on a client thread (an `IGrounds` seam): it saves ~0.6 s of a ~1.7 s opening tick that the
  white-out already covers. Measured again only if playtests complain.
- Prediction; the dev panel's `open planet` and transfer stay as dev tools.

## Approach

Landing owns all of it; no new module. Landing's constructor gains Items (`IItemsQueries`, `IItemsOps`: `TakeFrom`,
`PutIn`, `LayDown`) beside Grid, Power and Crew.

**Command `Launch`:** the pod the sender sits in flies — from the ship it descends, from a planet it returns. One command,
not two: the surface says which way. Refused `landing.notSeated`, `landing.noPlatform` (the run under it taken apart),
`landing.unpowered` (reused), `landing.noFuel` (descent only). `Board` gains `landing.notYours` off the ship.
`LaunchProblem(peer)` is the query the panel reads.

**State** — new saved fields with empty initializers, so no format bump:

- `Flights`, by peer, in the slice of the surface the flight left (a descent in the ship's, a return in the planet's):
  up or down, ticks left, home cell (where the pod stood aboard), the pod's slot stack. Flights hold no target: it is
  resolved when they end.
- `Owners`, by pod, on a planet: owner peer and home cell.

**Launch.** Take 5 biofuel, lift the rest of the stack out (`TakeFrom`), clear the pod's machine, and for a descent open
the run. A flight shuts its run when it reaches `FallTicks` or ends early, whichever comes first, whether or not it was
open before.

**Flights** (construction phase, as built): ticks left count down. A descent ending asks `ISurfaces.OpenPlanet()` for its surface, then
`Send`s the rider there; a return ends with `Send(peer, 0)`. In the derivation phase, a descent whose rider is gone or
passed out ends at once and docks home.

**Crossing.** Landing becomes `ICarries`; it is last in the registry, so it runs after Crew and Items as `Move` needs.
`Depart` gives up the peer's flight, or on a planet the pod it owns, lifted out of the grid. `Arrive` places the pod and
seats the rider if here and awake:

- On a planet: columns outward from the spawn column, left first on a tie; at each the pod drops to its highest standing
  row; the first column where it can be placed wins. Pods already down take their cells, so a new one lands beside them.
  None found: the cost and stack are laid at the spawn (a throw would crash every peer).
- Aboard: its home cell; if that no longer fits, it falls apart there into its build cost, its stack laid beside.
- The hero arrives at the surface's spawn (`Send` carries no cell, and the pod is placed after Crew arrives); its
  client holds it at the pod's floor and reports it there within a round trip. A pod off the ship cannot be taken apart
  (Building marks nothing where tools break, as built), so nobody strands its owner.

**Placement.** Grid's `Lands` rule reads the surface kind: aboard a run under the pod (`grid.noPlatform`, shut or open),
elsewhere standing ground under every column (`grid.noGround`). Players cannot build machines on planets, so only
Landing reaches it.

**Core.** `ISurfaces` gains `OpenPlanet()`: the lowest-id planet surface, opening `PlanetTypes.First` if there is none,
including one queued earlier this tick, so two descents ending together share it. The open is queued and made once every
surface has run, before the sends; its seed is drawn from the world's Rng then; the opener is the ship (`Inherit` copies
the ship's creative flag). `Where.Send` today throws for a surface id that does not exist yet (Sim.cs ~347): it checks
against the surfaces queued to open as well.

**Making the planet.** Its tick costs ~1.7 s, ~0.6 s of it growing the ground (0104), on every peer. The open tick is the
descent's last, so the rider's overlay is full white while the frame is frozen. Ship peers see a hitch once a session.

**Client**

- Slot panel: the landing feature gives the items panel a control for a pod, as logistics gives it `Served`: Descend or
  Return, greyed with the reason from `LaunchProblem(peer)`, read on the panel's timer.
- Landing feature: `PodFlights` draws the pod falling out of the run for ship peers and lifting off the planet; the flight
  overlay (streaks, re-entry glow, the white-out) is a `CanvasLayer` that survives the view switch; the touchdown (legs,
  dust) and the docking (the run drawn opening under the rising pod, then shutting) play when the client sees its rider
  arrive with the transfer — no derived state in the sim for them.
- Seating holds a flying hero at its home floor, hidden. The camera follows the pod until the overlay covers the screen.

## Determinism and co-op

- Inputs: `Launch`, `Board`, `Alight`, `Leave`, `Transfer` only. Time is whole ticks; the seed comes from the world's Rng
  when the planet opens.
- Order: flights by peer, pods by id, queued opens before sends, in the order asked; the landing search breaks ties left.
- A peer joining mid-flight gets the flight in the save and draws it.
- Client-authoritative only where the seated hero is (0002).

## Trade-offs accepted

- A launch shuts its run even if it was opened by hand before.
- A pod over ground dug away under it stays standing, as one does aboard.
- A pod whose home spot was built over while it was down falls apart into its build cost on return.
- Ship peers see a ~1.7 s hitch whenever a descent opens the planet; the rider sees white.

## Tests

- Scenarios: one per refusal (`notSeated`, `noFuel` with 4, `unpowered`, `noPlatform`, `notYours`); the launch burns 5,
  the rest of the stack crosses and comes back; the run opens and shuts at `FallTicks`, and shuts on a mid-fall Leave;
  the descent opens `First` once and the hero arrives seated at the pod on flat ground near the spawn (the only slow
  test; others open `Small` first); a second descent lands beside the first; two descents ending on one tick give one
  planet and two pods; a return docks at home, and with home built over falls apart, its stack laid; Leave mid-descent,
  Leave on a planet, death on a planet, Leave mid-return — the pod comes home each time; save and load mid-descent and
  mid-return play on alike.
- Invariants: every pod is in exactly one place (standing on one surface or in one flight); biofuel in slots and flights
  falls only by 5 per descent; every planet pod's owner is on that surface or in a flight.
- Fuzz: Landing draws `Launch`, mostly from seated heroes.

## Cost

- Landing (changed); Grid's `Lands` rule; Core's `ISurfaces.Planet` and `Where.Send`; client `landing`, the items panel
  slot, `crew` Seating.
- `Content.Landing`: `DescentFuel` 5, `FallTicks` 20, `DescentTicks` 160, `AscentTicks` 100, `Planet` =
  `PlanetTypes.First`.
- Slices: 1 the sim, the panel button and the white-out (playable end to end); 2 the cinematics (fall, streaks, glow,
  touchdown, docking).
- About 400 lines of sim, 500–700 of client.

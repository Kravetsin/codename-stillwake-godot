# The shuttle's trip is seen: «A — Под днищем»

- Status: accepted; built
- Date: 2026-11-17
- Supersedes: the "plain pictures" line of [shuttle-flight](2026-11-16-shuttle-flight.md)

## Context

The shuttle vanished from its platform and appeared at the vessel; its riders saw the drop pod's flight screen. The user
drew the trip's storyboard and picked A of four variants on its pick page (2026-11-16): the run opens, the shuttle sinks
through it, flies off under the ship's belly, the flight, a white flash, a far approach on its retros, the camera closing in.

## Decision

- **All on the client** (`game/features/landing`), the voyage's ticks and the arrivals' frame time driving
  `logic/ShuttleTimeline`; one number reaches the sim: `Shuttle.FallTicks` 20 → 60, the run open till the shuttle is under the
  belly. `FlightTicks` stays 200, the lines running about 4 s.
- **Leaving** (ship or vessel): it lifts off its spot or sinks through its run slid open (`PodFlights.Deck`), its skids fold in
  three frames (`crewShuttle_fold_1`, `_2`), its stern fires and it flies off to the right; the rider's camera follows and pulls
  out. Always facing right: no turn, no mirror.
- **The rider's screen**: the shuttle, skids up, among sideways star lines over the whole window, then the white (`Whiteout`).
- **Arriving**, held by `Arrivals` (a shuttle's own hold, aboard and off the ship, let go when a rider gets out): from the far
  frame (`0.8 · FarthestZoom`) it comes in from the left on its nose retros; at the vessel it stops at the gap, home it rises
  through its run drawn sliding open and shut over the grid's shut one. Its skids unfold and the camera closes back to the
  follow's own view and zoom, with no jump.
- **Moving, it lights itself** a little, faded in as it sets off and out as it settles, so it reads in an unlit hangar. Its
  canopy never changes ([shuttle-desant](2026-11-16-shuttle-desant.md)).
- **Who sees what**: riders see everything and only their cameras move; a crewmate aboard sees it leave and come home; a peer at
  the vessel sees it arrive and leave.

## Deliberately not handled

- New sounds, and onlookers hearing it: riders keep `FlightOverlay`'s, retimed.
- Shake and tilt; the course line stays a placeholder.
- An arrival in a world loaded or joined mid-way: it stands, as a pod does. A shuttle laid as its cost: no arrival.
- The far frame is only 1.25 × the follow's at the closest zoom level: wider would draw debris it never loaded.

# The drop pod's flight is drawn by the client from the flight's ticks

- Status: accepted
- Date: 2026-10-27

## Context

Slice 1 of the descent (`2026-10-26-the-descent`) made the pod vanish at a launch and the rider's screen white. The user
approved a storyboard (the "Drop Pod Descent" artifact): the deck opens, the pod falls through, a fast flight, a white flash
over the planet's opening, the touchdown. After seeing it built the user cut the flight to its first stage: star lines alone,
no re-entry fire or clouds, and a shorter descent.

## Decision

- **Client only.** No sim, save or command changes but one tunable: `Content.Landing.DescentTicks` 160 → 100 (5 s), as the
  return. Every picture reads what slice 1 exposes: `FlightOf(peer)` (`Up`, `TicksLeft`, `Home`, `Floor`).
- **Time from the ticks, then from frames.** A flight's seconds are its ticks gone plus the tick's drawn share
  (`FlightTimeline`), so a save loaded or a guest joining mid-flight shows the right moment. After an arrival, time counts in
  frames from when it was first seen, a frame at most 0.05 s (`Arrivals.MostDelta`): the planet's opening freezes the ticks
  the touchdown would play in.
- **Who sees what.** Aboard, everyone: the deck's halves sliding apart (0–0.3 s), the pod falling through on a thruster's kick
  (0.3–1.0 s) and on, gathering speed, till 2.4 s far under the ship, the halves sliding shut by `FallTicks`; the rider's
  camera shakes through `ShipCamera.Nudge` (`PodFlights`). The
  rider: a dip to black, then the flight at the world's scale, the pod in the middle, stars torn into lines ever faster, to the
  white (`FlightScreen` under `Whiteout` in `FlightOverlay`). On the planet, everyone: the pod lowered from eighteen cells over
  3 s on its
  retro flame into blown dust, with a bounce, held out of the grid, the porthole and the lamps till it stands (`Arrivals`,
  `GridView.HeldBack`). A return: a lift-off on its flame out of dust, seen rising for 3 s, the rider's climb out of a blue sky
  with clouds into
  black, the white falling over the pod already home.
- **Drawn in code, not an AnimationPlayer:** the project has none, and every mark is a function of time, ported from the
  storyboard. The flight screen is one `_Draw` of at most 400 quads, for the rider alone.
- **Arrivals are off the ship only** and never in a world just made (a new `client.World`: a load or a resync); the rider's
  own pod arrives the frame its flight ends. Leaving the pod or flying off inside the hold lets it go at once.

- **Heard by everyone on the surface.** A moving pod's engine, clamps and touchdown are placed on it on the World bus (as
  far as `Sound.BreakReach`), so the host and every guest there hear a drop, a touchdown, a lift-off or a docking aboard; as the
  flame reaches the ground the engine swells and the blasted grit roars, briefly. The rider hears the engine from inside, the
  hull's rumble and a thump at the white on the Suit bus, and hears as in air while in a pod: it holds its own. No new files:
  the jetpack's thrust, a drone's clunk, a hard landing, the vent's hiss and lava's bed, retuned (`Sound.Pod`). A moment's sound
  plays only if it passes while seen, never late after a load.

## Deliberately not handled

- The docking aboard: no rising pod, no deck drawn opening over the shut one.
- Light cast by the flames; the hero behind the porthole while its pod moves.
- A touchdown in a world loaded or joined during it: the pod shows standing.
- A run opened by hand before the launch: its pieces show shut then slide; a friend toggling the run in the first second
  disagrees with the picture. Two pods launched off one run within a second: the second falls through a deck drawn shut.
- A mate under a landing pod is not pushed: the touchdown is a picture.

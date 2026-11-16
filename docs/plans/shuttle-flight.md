# Plan: The crew shuttle's flight — pictures and sounds

Status: slices 1 (seats) and 2 (the trip) are built and shipped, 2026-11-16, the trip's fuel checked and burnt from the
shuttle's store since fuel-liquid; what stays true of them is in the
[shuttle-flight ADR](../decisions/2026-11-16-shuttle-flight.md), sim/Landing, sim/Places and game/features/landing. Slice 3
is left, after the user's pick. Delete this plan once it ships.

## Words

- *course lamp*: the shuttle's lamp, lit while a course is set. *course line*: the radar's line from the ship to the
  course's contact.
- *placeholder*: the plain look drawn in code now: a thin line in the radar's green (`RadarScreen`). The shuttle vanishes
  and appears; its riders see the pod's flight screen (`Ride`).
- *deck*: the ship's floor; *run*: the launch platform pieces under the shuttle, opened and shut whole.

## Slice 3 — the trip as «A — Под днищем» (`feat/shuttle-pictures`)

Picked 2026-11-16 on the flight pick page (A of four), the user's storyboard: the run opens; the shuttle sinks through it,
skids folding; it flies off to the right under the ship's belly, the rider's camera following and pulling out; the rider's
flight screen, stars torn into lines; a white flash, and the vessel's world; from far out it comes in from the left, brakes on
its nose retros, skids unfolding, and stops at the gap; the camera closes back to normal focus. Reference timing: the pick
page's `VARIANTS[0]` (beats 1.2 / 1.5 / 2.8 / 2.2 / 1.0 / 2.8 / 1.8 s).

All on the client in `game/features/landing`, plus one number:

- **Sim:** `Content.Shuttle.FallTicks` 20 → 60, so the run stays open till the shuttle is under the belly (beats 1–2).
  `FlightTicks` stays 200: beats 1–5 fit its 10 s, the lines running about 4 s. Nothing else reaches the sim.
- **Art:** `shuttle.js` exports `crewShuttle_fold_1`, `crewShuttle_fold_2`: the whole craft, skids half up and up, 192 × 80.
  Flames drawn as the pod's (`PodFlights.Thrust`) at the stern nozzles and, braking, the nose retros (the pick's `NOZ`, `RETRO`).
- **`logic/ShuttleTimeline`** (Godot-free, beside `FlightTimeline`): from seconds, each beat's offset from the parked spot, skid
  frame, which flames burn and how hard, and the rider's camera blend (0 follow … 1 far frame). Leaving runs on the voyage's
  ticks and progress; arriving on `Arrivals`' frame time, as a pod's does (a world opening freezes the ticks).
- **`PodFlights`** draws a leaving shuttle as it draws a pod: the run slid open by its `Deck`, the craft sinking through and
  flying off right under the belly; and an arriving shuttle held by `Arrivals`, at the vessel (in from the left, retros, skids
  out, stop at the gap) and home (the far frame, in from the left under the belly, braking, rising through the run drawn sliding
  open by `Deck` as an overlay, skids out, the run sliding shut under it, the camera closing in; the sim's run is already shut).
  Leaving the vessel it lifts off its spot, skids folding, and flies off right past the vessel, as it leaves the ship. Moving, it
  is drawn by its own light too, faded in as it sets off and out as it settles, so it reads in an unlit hangar. Its canopy never
  changes, no lit panes; `ShuttleLights` skips a held shuttle's course lamp.
- **`Arrivals`** holds a shuttle too — new state, not the pod's: aboard (home) and off the ship (at the vessel), each till its
  camera stands (5.6 s / 5.1 s), let go at once when a rider gets out.
- **Rider's screen:** `Ride` knows its craft; `FlightScreen` draws the shuttle (skids up) among sideways lines, over the whole
  window whatever the zoom; `Whiteout` as is.
- **Camera:** one far frame, `0.8 · FarthestZoom`, absolute as `Fit` is; `ShipCamera` blends from the follow to it and back by
  the timeline's blend, centred between the follow and the shuttle, so every move ends where the follow stands. Riders only.
- **Tests (`game.tests`):** `ShuttleTimeline` at every beat's edges (offset, skid frame, flames, blend); `Arrivals` holds a
  shuttle aboard and off the ship and lets it go when a rider gets out; the blend ends at 0. `ShuttleTripTests` read `FallTicks`.
- **Dev:** `--trip=auto` waits out the hold before getting out, so one run shows the whole trip both ways.

Deliberately not handled: new sounds (riders keep `FlightOverlay`'s, retimed) and onlookers hearing it; turning or mirroring
(it always faces right); shake, tilt; an arrival in a world loaded or joined mid-way (it stands, as a pod does); a shuttle laid
as its cost (no arrival). Ship: the pick in an ADR, the landing README, this plan deleted.

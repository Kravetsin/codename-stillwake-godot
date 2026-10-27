# Design: the drop pod's flight pictures (descent, slice 2)

Storyboard approved by the user: the "Drop Pod Descent" artifact (a canvas page whose `ship`, `flight`, `planet` and `white`
functions draw each phase from time alone). Slice 1: [descent.md](descent.md), ADR `2026-10-26-the-descent`.

## Problem

A launch makes the pod vanish and the rider sees a white screen. Nobody sees the deck open, the pod drop, the flight, the
touchdown or the docking.

## Player experience

1. The rider presses Descend. The deck's two halves slide apart, the room vents through the hole, the lamp goes dark; the
   pod drops with a thruster kick, the rider's view shaking. A friend aboard sees the same, without the shake.
2. A dip to black, then the flight fills the rider's screen: stars torn into upward lines, the air catching fire under the
   hull with plasma streaming up both sides, then the sky hard blue and cloud sheets ripped past. No planet in view.
3. White rises and holds through the planet's freeze. On the planet the pod comes down on its retro flame, blows dust
   sideways, bounces and lights its lamps. Friends on the planet see the same.
4. The return runs backwards: a lift-off with flame and dust, a fast climb from sky to stars with no fire, white, then the pod
   rises through a deck drawn opening and shutting under it.

Goes wrong: the rider passes out mid-fall: every picture stops that frame and the pod stands at home, unlit.

## Out of scope

- Sound; light cast by the flames; the hero behind the porthole while the pod moves.
- A touchdown or docking in a world loaded or joined during it: the pod shows standing.
- A planet seen from above; landing legs; dust coloured per planet.
- Any change to the simulation.

## Scale

1–4 pods at once, each one sprite and one deck run; the flight screen about 350 strokes and 15 quads; each rider sees only
their own flight screen; a few flights a session.

## Approach

> Sections above "Decided after review" are the first draft: where they differ (Shake, `PlatformRuns.Lit`, the docking rows,
> the aboard return, Cost, the trade-off on alighting), the decisions at the end win.

The landing client feature owns all of it.

- **`logic/FlightTimeline`** — every time window as a client constant; the tick counts from `Content.Landing`. A flight's
  seconds are `(Descent|AscentTicks − TicksLeft + Session.Progress) / 20`. Pure curves give the deck's openness, the drop,
  heat, air, shake, touchdown and docking. The flight screen's windows are fractions of the flight's length, so retuned
  ticks stretch them.
- **`logic/Arrivals`** — one per session, kept by `Main` across surface switches as `FlightOverlay` is. A pod has arrived
  when it is seated and was not standing the previous frame on the same surface, or when it is your own pod right after
  your flight ended. It reports seconds since arrival; `Holds(pod)` is true until the pod settles. A load or a resync clears it.
- **`PodFlights`** — a new line in `SurfaceViews`: one `_Draw` node plus one `Strokes`. It draws this surface's flights
  (`Crew.Heroes` × `FlightOf`) and its arrivals: the deck halves sliding within the run (`RunOf`), the pod's sprite, the
  flame, dust puffs. `PodSeats` cannot: it draws only standing pods.
- **`GridView`, `SeatedHeroes` and `PodSeats`** take a `Held` predicate through `SurfaceContext`, so a pod still arriving is
  hidden while it is drawn moving.
- **`FlightOverlay`** gets a `FlightScreen` under its white, drawn in code from the storyboard's 320×180 frame scaled to the
  screen. Code, not an AnimationPlayer: the project has none, and every mark is already a function of time.
- **`ShipCamera.Shake`** — new, because `Nudge` gives one knock, not a rumble.
- **`PlatformRuns.Lit`** — an open run's lamp is dark.

Landers later reuse the deck, the fall curves and `Arrivals` as they are: the code works per body and per run, nothing
reserved. No new art: `machine_dropPod`, `platform_*`, `platform_open_*`.

## Determinism and co-op

Nothing enters the sim. Each peer draws from its own copy of the flights, so a guest who joins, a world loaded mid-flight
or a hitch shows the right moment. After arrival, time counts in frame seconds from when the arrival was first seen: the
planet's freeze would otherwise skip the touchdown.

| Seconds | Ticks | Seen by | What |
| --- | --- | --- | --- |
| 0–0.3 | 0–6 | everyone aboard | deck slides open; lamp out; venting (existing `Decompression`) |
| 0.3–1.0 | 6–20 | everyone aboard | pod falls 150 px with a kick; the rider's camera shakes |
| 0.75–1.0 | 15–20 | everyone aboard | deck slides shut, the pod already below it (62 px ≥ the pod's 56); the sim shuts it at 20 |
| 1.0–1.3 | 20–26 | rider | dip to black; the flight screen from 1.15 |
| 1.15–8.0 | 23–160 | rider | star lines; heat ≈2.3–6.9 s; air from 4.0 s |
| 7.0–8.0 | 140–160 | rider | white (`Whiteout`, built) |
| +0–2.0 | arrival | everyone on the planet | white falls over 0.5 s; pod down from 9 cells over 1.5 s, dust in the last quarter; bounce 1.5–1.85; released at 2.0 |

The return (100 ticks): lift-off 0–1.0; dip 1.0–1.3; sky to stars with lines rushing down, no heat; white 4.0–5.0. Aboard
after arrival: deck drawn opening +0–0.3, pod rising +0.3–1.2, deck shutting +1.2–1.45, released at +1.5.

## Trade-offs accepted

- The rider leaves or passes out mid-flight: the flight is gone and the pod pops back home empty, not animated.
- Two pods on one run launched within a second: the second falls through a deck drawn shut.
- Two pods arriving on one tick: each rider sees only their own pod come down.
- Docking draws an open hole over a shut deck (no venting, lamp lit) for 1.2 s.
- A rider who gets out during touchdown stands beside a pod still descending, for under 2 s.
- The camera holds still during the fall, as the storyboard shows (the slice 1 design said it follows the pod).
- The world keeps drawing under the opaque flight screen: within budgets.

## Tests

- No sim scenarios (no sim change).
- `FlightTimelineTests`: the deck shut at 0, open by 0.3, shut by `FallTicks`; the pod below the deck when it starts to shut;
  the ascent has no heat; the windows scale with `DescentTicks`.
- `ArrivalsTests`: nothing arrives on the first frame after a load or join; a new seated pod on the same surface arrives; an
  empty one does not; boarding a standing pod does not; your own pod arrives after a surface switch; the hold ends at release.
- Invariants (FsCheck): every curve in range for any ticks and progress; the drop never decreases; the touchdown height
  never increases before the bounce.
- Client: `--descent` with `tools/snap.ps1 -Movie`, a sheet per phase; render time during re-entry on the reference world.

## Cost

No sim modules. New in the landing feature: `PodFlights`, `FlightScreen`, `FlightTimeline`, `Arrivals`. Changed:
`FlightOverlay`, `Main`, `SurfaceViews`, `GridView`, `SeatedHeroes`, `PodSeats`, `ShipCamera`, `PlatformRuns`, READMEs, an
ADR. About 900 lines: 2a the world pictures and the hold-back (~500), 2b the flight screen (~400).

## Open questions (answered below)

- Sound: reuse the jetpack's `Sound.Thrust` loop, or silent until a sound pass?
- Should a run opened by hand also turn its lamp dark?
- Scale: the storyboard's frame scaled to the screen draws the pod about 6× at 1080p, larger than the world's zoom.

## Decided after review (overrides the above where they differ)

- **No docking picture.** The return is a lift-off on the planet (flame, dust), the ascent flight screen (the descent's run
  backwards: blue sky to stars, lines rushing down, no fire), white, and the white falling over the pod standing at home.
  Arrivals are seen off the ship only: a pod docking aboard is not animated.
- **No sound** in this slice; a sound pass later.
- **The flight screen at the world's scale:** drawn in screen pixels at the camera's zoom, so the pod is the size it is in the
  world, with more room round it for the speed lines. Not the storyboard's 320×180 frame stretched.
- **Three steps, each a commit that stands:** 2a the departure aboard (deck sliding, the pod falling, `FlightTimeline`);
  2b the rider's flight screen, down and up; 2c the touchdown and the lift-off on the planet (`Arrivals`, the hold-back).
- **Shake through `ShipCamera.Nudge`,** called every few ticks of the fall, not a second shaker.
- **The run's lamp is left as it is** (green while powered, open or shut).
- **Drawing:** the flight screen in one `_Draw` node, ported from the storyboard; render time measured on re-entry, and its
  lines and clouds moved to a shader only if it misses the budget.
- **Invariants:** FsCheck only for "every curve in range"; monotone shapes by a table of sample points.
- **Facts the review added:** the deck halves are new drawing (platforms are joined pieces, no door slide to reuse): a run
  splits at its middle cell, each half's shut pieces sliding outward clipped to the run. `FlightOverlay` reads the whole
  flight (`Up`, `TicksLeft`), not ticks alone. `Arrivals` drops a pod the moment its seat or machine goes (arrive, E, E,
  Return inside the hold). A mate under the landing pod is not pushed: the touchdown is a picture.
- **Shortened by the user (after slice 2b):** the descent's flight screen is star lines alone, no re-entry fire, bow shock,
  plasma or clouds; `DescentTicks` 160 → 100 (5 s: the fall, three seconds of lines, the white). The return keeps its blue sky
  and clouds leaving the planet.

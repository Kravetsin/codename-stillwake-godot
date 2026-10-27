# Plan: the descent's flight pictures (slice 2)

Design: [descent-cinematics.md](descent-cinematics.md) (its "Decided after review" section wins). Client only: no sim, save or
command changes. Branch `feat/drop-pod-descent`, three commits that each stand: 2a, 2b, 2c. Time is the flight's ticks, 20 a
second; "s" below is seconds since the launch tick.

## Shared: `game/features/landing/logic/FlightTimeline.cs` (Godot-free, tested)

- `Seconds(Flight f, double progress)` = `(Total(f) − f.TicksLeft + progress) / 20`, `Total` = `DescentTicks` or `AscentTicks`.
- Windows as client constants, in seconds: `OpenS = 0.3`, `DropFrom = 0.3`, `DropTo = 1.0` (= `FallTicks / 20`, asserted in
  a test), `ShutFrom = 0.75`, `DipFrom = 1.0`, `DipTo = 1.3`, `ScreenFrom = 1.15`; the flight screen's heat and air windows
  as fractions of the flight (`HeatFrom 0.22`, `HeatTo 0.95`, `AirFrom 0.5`, as the storyboard); arrival: `FadeIn 0.5`,
  `Descend 1.5`, `Bounce 1.5–1.85`, `Release 2.0`; lift-off `0–1.0`.
- Pure curves, each 0..1 (or px) of seconds: `Open(s)` (0 shut, 1 open, closing over `ShutFrom..DropTo`), `Drop(s)`
  (px fallen, 150 at `DropTo`, eased in), `Dip(s)`, `Heat(k)`, `Air(k)` (k = fraction of the flight), `Touchdown(a)`
  (cells above the ground: 9 → 0 eased out over `Descend`, then the bounce), `Lift(s)` (cells risen, 0 → 9 eased in).
- Tests (`game.tests/FlightTimelineTests.cs`): the deck shut at 0, open by `OpenS`, shut by `DropTo`; the pod's drop at
  `ShutFrom` ≥ its 56 px (below the deck when it starts shutting); `DropTo × 20 == FallTicks`; the ascent has no heat; a
  table of sample points that `Drop` never falls back and `Touchdown` never rises before the bounce; FsCheck: every curve
  in range for any ticks and progress.

## 2a — The departure, aboard

- `game/features/landing/PodFlights.cs`, a new line in `game/SurfaceViews.cs` after `PodSeats`: one `Node2D` with `_Draw`,
  lit (`Lit.Set`), at the machines' z. Each frame (`_Process`): for every hero on this surface (`Crew.Heroes`) with a
  `FlightOf` that is a descent (`!Up`), its seconds `s`:
  - **The deck:** the run under `Home` (`LandingModule.RunOf`), split at its middle cell; while `Open(s)` < 1 draw each
    half's shut pieces (`platform_*`, by their joins as the grid draws them) slid outward by `(1 − Open) × half`, clipped to the
    run's rect (`DrawSetTransform` + a clip `Rect2` via a child `Control`'s `ClipContents`, or drawing only the part inside).
    The grid itself shows the run open from the launch tick to tick 20 (the sim), so the slide covers the open sprites.
  - **The pod:** `machine_dropPod` at `Home`, `Drop(s)` px lower, clipped to the run's bottom edge and below; its thruster
    kick (two flickering quads, as the storyboard) while `DropFrom ≤ s < DropTo`. Before `DropFrom` it stands (the grid no
    longer draws it: its machine is gone).
  - Redraw every frame while any flight is in its first second; nothing otherwise.
- **The rider's shake:** `HeroView` or `Main` calls `_camera.Nudge` with a small random vector every 3 ticks while the
  client's own descent is within `DropFrom..DropTo` (no new camera member).
- Venting: nothing to do (`Decompression` already plays when a run opens).
- README (landing client): `PodFlights` row and rules; `SurfaceViews` line.
- Check: `--descent` with `tools/snap.ps1 -Movie 3 -- --descent --zoom 4`, the sheet looked at (deck sliding, pod falling,
  deck shutting).

## 2b — The rider's flight screen

- `FlightOverlay` reads `(int Surface, Flight? Own)` instead of ticks: Main passes `_m.Landing.FlightOf(id)`, and a
  `Func<float>` for the camera's zoom and one for `session.Progress`.
- `game/features/landing/FlightScreen.cs`: a `Control` (full rect, under the white `ColorRect` in `FlightOverlay`) with
  `_Draw`, shown while the own flight's `s ≥ ScreenFrom`. Drawn in screen pixels, the world's scale (`zoom`) for every
  size: the pod (`machine_dropPod`, a texture) at the centre; port of the storyboard's `flight(t)`:
  - 220 speed lines (seeded per index, positions from `s` only), near ones longer/brighter; heat tints them orange;
  - the sky gradient black → fire → blue (`Heat`, `Air`);
  - re-entry: the glow under the hull, 120 plasma tongues up both sides, the bow arc, the hull's glow; shake and buffeting
    by `Heat`; the vignette;
  - clouds in the air phase, some in front of the pod;
  - the ascent (`Up`): the same with no heat, blue sky to black, the lines rushing down instead of up.
  - The dip to black over `DipFrom..DipTo` (a black `ColorRect` alpha, under the white).
- Godot-free: the per-index seeds and the line/cloud positions in `logic/FlightScreenMath.cs` if they hold any rule worth a
  test; otherwise in the node. A `Rng`-free hash (`sin`-based as the storyboard, or `HashCode`-free integer hash).
- Performance: the screen draws only for the rider; measure `RenderingServer.ViewportGetMeasuredRenderTimeCpu` on re-entry
  on the reference world (`--perf`-like or the testbed) and note it; over budget → move the lines and clouds into a
  `ShaderMaterial` on a `ColorRect`.
- README (landing client): `FlightScreen` row and rule.
- Check: `--descent` movie sheets of the flight (space, re-entry, air) and of the return's ascent.

## 2c — Touchdown and lift-off, on the planet

- `game/features/landing/logic/Arrivals.cs` (Godot-free, tested), one per session, kept by `Main` across `Follow`, cleared on
  a load or a resync. `Frame(int surface, bool aboard, IReadOnlyList<(int Pod, int Cell)> seatedPods, double delta)`:
  off the ship only (`aboard` false), a pod seated now and not standing (seated or not) on this surface last frame, and not
  on the first frame after a load or switch-in of a world, has arrived; each arrival keeps its seconds since seen;
  `Holds(pod)` while seconds < `Release`; a pod whose seat or machine is gone is dropped at once.
  Your own pod after your flight ended counts as arrived (the surface switch frame is not "first frame" for it).
- Hold-back: `SurfaceContext` gains `Func<int, bool> Held`; `GridView.DrawMachines` skips held machines and redraws when the
  held set changes (a key compared each frame while any is held); `SeatedHeroes` and `PodSeats` skip held pods.
- `PodFlights` (2a) also draws, on a planet: each arrival — the pod `Touchdown(a)` cells above its cell, its retro flame
  shrinking as it slows, dust puffs sideways in the last quarter and settling after (a fixed number of quads from `a`);
  each ascent flight (`Up`) on this surface in its first `1.0 s`: the pod rising `Lift(s)` cells with flame and dust.
- The white after arrival already falls (`Whiteout`); aboard after a return nothing more.
- Tests (`game.tests/ArrivalsTests.cs`): nothing arrives on the first frame of a world; a new seated pod off the ship arrives;
  an empty pod does not; boarding a pod that stood does not; your own pod after a switch arrives; aboard nothing arrives;
  the hold ends at `Release`; a pod launched inside its hold is dropped.
- README rows; ADR `docs/decisions/2026-10-27-the-descent-pictures.md`: what stays true (client only, time from ticks then
  frames, who sees what, no docking picture, the world's scale).
- Check: `--descent` movie sheets of the touchdown and of the lift-off; `--host`/`--join` pair once by hand.

## Done when

- `tools/test.sh` green; `dotnet format --verify-no-changes` clean; docs guard green; README sections current.
- The three movie sheets looked at; the re-entry render time noted in the ADR.
- `descent.md` and this plan deleted once what stays true is in the ADR and READMEs.

## Risks

- `GridView` redraws machines on a version change only: the held check must not cost a frame when nothing is held.
- A guest's `FlightOf` lags the host by its playout: the pictures follow its own world, which is the point.
- The first-frame rule must not swallow the rider's own arrival (the switch happens on the tick the pod lands).

## Review folded in (wins over the tasks above where they differ)

- **Time names:** `s` seconds since the launch tick (`Seconds`); `T` the flight's length in seconds (`Total / 20`: 8 down,
  5 up); `k = clamp((s − DipFrom) / (T − 2), 0, 1)` the flight screen's fraction; `a` seconds since an arrival was first seen.
  Heat rises over `k` 0.22–0.40 and falls over 0.80–0.98; air rises over 0.5–0.9 (the storyboard's numbers). Arrival windows
  are in `a`: `Descend 1.5`, `Bounce 1.5–1.85`, `Release 2.0`. Client constants, all, with the 9-cell start and the 150 px
  drop; `FadeIn` dropped (`Whiteout.FadeOutS` does it). Tests add "windows scale with `DescentTicks`"; the drop at `ShutFrom`
  is 62 px (≥ the pod's 56).
- **Where a flight is drawn:** on the surface it left until it ends (a descent aboard, a return's lift-off on the planet): that
  surface's `Crew.Heroes` × `FlightOf`. A pod's cell during a return comes from `Flight.Floor` by the inverse of `FloorOf`
  (`Floor − (Height − 1) × Width − (Width / 2 − 1)`), a client helper in `FlightTimeline`.
- **2a drawing:** the pod first, the deck's shut halves on top, slid outward by `Open × half` (0 in place, 1 fully apart);
  no clip on the pod. Accepted: a run opened by hand before the launch shows its pieces pop shut-then-slide; a friend toggling
  the run in the first second disagrees with the picture.
- **Shake:** `Main`, on each new tick that is a multiple of 3 while its own descent's `s` is in `DropFrom..DropTo`, calls
  `_camera.Nudge` with a random direction (client `Random`); the size judged on the sheet.
- **2b layers,** bottom to top inside `FlightOverlay`: `FlightScreen`, the dip's black (a tent: 0 at `DipFrom`, 1 at 1.15, 0 at
  `DipTo`), the white. `FlightOverlay` reads `(int Surface, Flight? Own)`, `Whiteout` gets `Own?.TicksLeft`; progress is a
  `Func<double>`. `FlightScreen` `MouseFilter = Ignore`. Lines and tongues try `Strokes` before any shader. The hash is the
  storyboard's `sin`-based one.
- **2c `Arrivals`:** `Frame(bool aboard, IReadOnlyList<(int Pod, bool Seated)> pods, bool ownLanded, bool newWorld, double
  delta)`, called by `Main._Process` before the views draw; `delta` clamped to 0.05 s (the freeze and the planet's first
  `ShowWorld` would eat the touchdown). Arrived: off the ship, seated now, absent from last frame's pods (seated or not) — or
  `ownLanded` (the client's own flight was there last frame and is gone, and its hero now sits in a pod here). `newWorld`
  (the `client.World` reference changed: a load or a resync, never a surface switch) clears it and counts nothing. A pod whose
  seat or machine goes is dropped at once: alighting during the touchdown snaps it to the ground (the design's trade-off line
  corrected). `SurfaceContext` gains `Arrivals`; `PodFlights` reads `Held(pod)` and `SecondsSince(pod)`; its line `Fades: true`.
  Tests add: boarding a standing pod does not arrive; alighting inside the hold drops it; a long delta is clamped.
- **Docs:** `sim/Landing/README.md` "no picture of a flight yet (slice 2)" and the client README's slice 2 line updated.
- **Checks:** `--descent` movies of 5 s (2a) and 16 s (2b, 2c); the scenario's Return 60 ticks after landing; one host/join
  pair by hand with the friend on the planet, in "Done when".

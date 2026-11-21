# Plan: the drone's motion and alarms

By design-author, cut by design-reviewer, choices by the user (2026-11-21), reviewed by `/review-plan`. Replaces
`drone-alarms.md`. Continues [light-drone](../decisions/2026-11-21-light-drone.md) and
[drone-companion](../decisions/2026-11-21-drone-companion.md).

## Problem

Running and turning at once makes the drone jump. `DroneFloat` (`game/features/crew/logic/`) places it at
`neck - facing * Behind` (6 px behind the head), so each turn moves its target 12 px across, and its 14 px `Leash` drags it
there. It is pinned to the hero's facing and does nothing of its own. Nothing near the hero warns of a meteor, a bolt or a
catcher out of view.

## Player experience

1. Running and turning: it carries on past the head, curves over it and settles. It never jumps.
2. Standing still: it sits by a shoulder, then drifts to a new perch every few seconds, its lens looking off into the dark.
3. Lamp or scan: the lens follows the pointer whatever the drone does, the beam with it.
4. Bag or plans open: it flies before the waist and projects, in any pose; it does not wander meanwhile.
5. A meteor `Hazards.Incoming` whose cell is within `Notice` of the anchor: it darts toward the cell, flashes red until the
   meteor lands, then returns. With a window open it stays and flashes in place, its lens toward the danger; no lean then.
6. A bolt `Hazards.Strike` whose column is within its `Spread` + 2 columns (8 px each) of the anchor: the same dart, toward
   the strike's column at the anchor's height. Hunted (`Crew.Hunted(peer)` with `Creatures.HunterOf(peer)` not null): it
   leans toward the hunter the whole time, under a dart too.
7. It pulses red while its hero's air is `Low` (`Danger.Read`); a friend's drone does all this too.

## Decided (the user)

- The lens follows the pointer in lamp and scan; elsewhere the drone looks where it wants (2026-11-21).
- It does not fly to watch the work: tried, it chased the pointer and all but jumped when zoomed out (2026-11-21).
- Window open during an alarm: it flashes in place (2026-11-21).
- The red is the lens and the soot tinted, no light node (2026-11-21).
- Alarms come with the drone, no unlock; its habits win over the mode: a dart in lamp mode carries the beam (2026-11-18).

## Out of scope

- Walls: it flies through them (light-drone). A dart through a wall shows where the danger is, not the way to it.
- A bolt under a roof still darts: checking open sky costs a query nobody else needs.
- Its own air, cold, venting, storms, flares, fire, hunger, lava, blows: the HUD, the room flash and the sky show them.
- Sounds, text, a mute setting; anything sent or saved.
- The work, pings, loot and creatures as things it watches: they come with the magnet and its habits.
- The scanner's own look (an animated scan, the readout hologram redrawn): a separate art pick, not motion.

## Approach

`DroneFlight.cs` (the body) and `DroneWill.cs` (the mind, with `DroneSenses`) in `game/features/crew/logic/` replace
`DroneFloat`. Godot-free, unit-tested in game.tests. Floats are fine: nothing reaches the sim.

- **Body.** Position and velocity, pixels of the world. The anchor is the hero's middle column `Above` (16 px) over the
  neck, or over the body's middle lying down: a turn moves nothing. Each frame, dt capped at 33 ms so a stall never flings it:
  - its wanted velocity is the hero's plus `(goal - pos) / ArriveMs`, that second term at most `Cruise` long (`DartSpeed`
    darting);
  - its velocity moves toward the wanted one by at most `Accel`·dt, which gives the overshoot on a turn;
  - outside a window it stays within `Reach` of the anchor: past it, the position is put back on the circle and the outward
    part of the velocity dropped. A window's goal has no reach: it is always before the waist;
  - the first frame, the first after being hidden, and any anchor jump over `Lost` in one frame (a respawn, a late joiner)
    put it at its goal at rest;
  - `Off` is its distance to its goal (`Figure` reads it for `_droneThere`, the window's arrival).
- **Will.** `Pick(senses, ms)` gives the goal, the look and the tint; a `Random` comes in through its constructor (a seed in
  tests, a new one per figure in the game). The goal by rank: window (before the waist, `SheetHoloLook`) > dart >
  settle.
  - **Dart:** one per meteor or bolt the sim reports, held while it reports it, then the drone returns; the same meteor or
    bolt never darts it twice; a new one replaces a dart under way.
  - **Settle:** a perch inside the box `Home` (half-sizes) round the anchor with a look of its own, picked again after a
    random `BoredMs`.
  - **Lean:** `Lean` toward the hunter added to any goal but the window's.
  - **Sign** (a gesture, `DroneSign`): the goal held where the drone is while it plays.
  - **Tint**, apart from the goal: red while a dart's danger lasts, and while its hero's air is `Low` a red pulse every
    `PulseMs`. A passed-out hero's drone keeps to the anchor and still tints.
- **Senses.** `DroneSenses`, a record `HeroView` fills per drawn hero each frame: the anchor, the hero's velocity, the window,
  the meteor, the bolt, the hunter's place, `Low`, the surface. A hazard of another surface than
  the hero's is not sensed. Each read is O(1) or one `List.Find`.
- **Lens.** `LightDrone` keeps its easing. Toward the pointer in lamp and scan (the look for a friend), up in a window, at the
  danger in a dart, else the perch's own look.

Tunables, `DroneFlight.Tune`, in the client: they never enter the sim, so not `sim/Content`. `Above` 16 px, `Cruise`
0.12 px/ms, `DartSpeed` 0.6 px/ms, `Accel` 0.002 px/ms², `ArriveMs` 250, `Reach` 40 px, `Lost` 64 px, `Home` 10 × 5 px
half-sizes, `BoredMs` random 3000–8000, `Notice` 160 px, `Lean` 6 px,
`PulseMs` 1200.

## Co-op

Nothing enters the sim, nothing new is sent. A friend's client computes the drone from `Seen` (hero, look, drone mode,
`Projects`) and the shared sim's read-only queries. Decoration may differ between screens: the perches (own `Random`),
the overshoot, and a dart at the edge of `Notice` (a friend's anchor is 20 Hz). Accepted: syncing decoration is not worth
traffic.

## Steps

1. `DroneFlight` with its tests: a run reversed at 30 and 144 fps never moves it more than
   `(hero speed + Cruise)·dt + 0.1 px` a frame; a facing flip alone moves nothing; within `Reach` at any hero motion up to the
   hero's fastest fall; a window goal reached in every pose; a jump over `Lost`, the first frame and a re-show put it at its goal.
2. `DroneWill` and `DroneSenses` with their tests, seeded: ranks (window > dart > settle); a dart held while its danger
   lasts, then returning within `Home`, never twice for the same one, replaced by a new one; the lean under a dart, none with a
   window or no hunter; a meteor beyond `Notice`, a bolt outside its spread, a hazard on another surface ignored; low air
   pulses; standing 60 s it re-perches at most `BoredMs` max apart and
   stays within `Home`; the lens on the pointer in lamp and scan.
3. Callers: `Figure` passes the anchor and senses, keeps `Off` for `_droneThere`; `LightDrone.Follow` takes the goal, look
   and tint; `HeroView` and `HeroView.Others` fill the senses; `HeroView.Meeting` takes `Above` from `DroneFlight`, its drone
   put at the anchor as it takes over and gives back, untinted; `Figure.DroneTo`, never set but to null, goes with its branch
   and the two lines clearing it; `DroneFloat` is deleted. Readers of the drone's place keep their contract: loot flying into
   the lens (`Ui.DroneAt`, `ItemsView`) and the window frames (`Ui.DroneLens`).
4. Try: build, the crew tests, launch the plain game for the user.
5. Ship, after the user's yes: the full suite, the light-drone ADR's "Alarms were designed and cut" line and a drone-motion
   ADR, this plan and `drone-alarms.md` deleted.

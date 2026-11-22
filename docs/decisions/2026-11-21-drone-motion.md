# The drone's motion and alarms: its own will, trailing its hero

- Status: accepted; continues [light-drone](2026-11-21-light-drone.md) (its alarms, designed and cut there) and
  [drone-companion](2026-11-21-drone-companion.md); superseded in part by [drone-thoughts](2026-11-22-drone-thoughts.md)
- Date: 2026-11-21

## Context

The drone sat 6 px behind the head on the side the hero faced, dragged there by a 14 px leash. Running and turning at once
moved its target 12 px across every turn, so it jumped. It was pinned to the facing and did nothing of its own, and nothing
near the hero warned of a meteor or a bolt out of view.

## Decision

The drone is a body and a mind, both Godot-free client code in `game/features/crew/logic/`; nothing reaches the sim.

- **Body** (`DroneFlight`): a position and a velocity of its own. Its anchor is 16 px over the neck in the feet's column, so a
  turn moves nothing. It flies at its own pace, never the hero's: a slow drift to a goal near (`Drift`, easing in over
  `ArriveMs`) and a catch-up past `Slack` that grows with the square of the gap. So it falls a few cells behind a running
  hero and catches up when the hero stops; it does not ride the shoulder. Its velocity turns at a bounded acceleration,
  which carries it on and round a turn. A `Reach` circle is a net for the longest falls; an anchor jumping over `Lost`
  (respawn, late join) or a re-show puts it at its goal.
- **Mind** (`DroneWill`, by rank): a window's place before the waist; held at the anchor while its hero is passed out; a
  dart at a meteor within `Notice` or a bolt near its column, flashing red until the danger ends, once per danger; held
  still while its hero signs; else a perch of its own in a 10 × 5 px box by the anchor, picked again every 8–20 s with a
  look of its own. A hunter leans it his way. Its hero's low air pulses it red, a friend's drone too.
- **Lens:** at the pointer in lamp and scan, up in a window, at the danger in a dart, else the perch's look.
- The tunables are `DroneFlight.Tune`, in the client, not `sim/Content`: they never enter the sim.

## Consequences

- Each client computes every drone from what it already has (`Seen`, the shared sim's queries). Decoration differs between
  screens: the perches (a `Random` per figure), the overshoot, a dart at the edge of `Notice`. Syncing it is not worth
  traffic.
- The alarms ride the motion: no sound, text or light node; the lens and the soot are tinted.

## Rejected

- **Watching the work** (flying to where the hero drills or cuts): it chased the pointer and all but jumped when zoomed
  out.
- **Matching the hero's velocity plus a pull** (the first cut): it rode the shoulder, and perches every 3–8 s at a
  brisk pace read as jerky.

## Deliberately not handled

- Walls: it flies through them. A dart through a wall shows where the danger is, not the way to it.
- A bolt under a roof still darts it: checking open sky costs a query nobody else needs.
- Its own warnings of cold, venting, storms, flares, fire, hunger or lava: the HUD, the room flash and the sky show them.

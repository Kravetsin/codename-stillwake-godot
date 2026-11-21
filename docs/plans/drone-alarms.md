# Design: the drone's alarms

By design-author, cut by design-reviewer; cut from the light drone before it shipped, not built. It continues
[light-drone](../decisions/2026-11-21-light-drone.md) and [drone-companion](../decisions/2026-11-21-drone-companion.md):
a paired hero's drone warns, by light and motion, of danger the screen does not show at the hero.

## Sources, checked against the code

- **Meteors:** `Incoming`, 3 s before the hit.
- **Lightning:** Hazards `Strike` (cell, ticks left, spread), a 3 s glow.
- **Hunted:** `Hunted(peer)`. The tracker's beep and bracelet blink reach the ear and the wrist, not the eye.
- **Air:** `SuitDanger.Low` (`Danger.cs`), for friends' drones only; one's own air is on the HUD bar and `TunnelView`.
- **Already shown, so no alarm:** venting (the room's flash, 0075), storm (the sky), flare (the aurora; it breaks machines, not
  heroes), hunger (meal slots), fire (no sim state), lava and blows (shown as they land).

## Problem

A meteor's wall, a lightning glow overhead, a catcher behind are out of view, and nothing by the hero says "now" or "there".

## Player experience

1. A meteor is coming at a near wall: the drone darts to it, glows red there 3 s, comes back.
2. A bolt's glow near the hero's columns under open sky: the drone darts at it the same way.
3. Hunted by a catcher: the drone leans toward it, on screen or off.
4. Two at once: the latest dart replaces the one before; the hunted lean goes on underneath.
5. Co-op: a friend's drone does the same for their surroundings, and pulses slow red while their air is `Low`: trouble seen
   across the ship.

## Out of scope

Own air and cold, venting, storm, flares, fire, hunger, lava, blows; pointing the way to air; new sounds, text, a mute
setting. An unpaired hero has no drone: only the HUD warns.

## Approach

A client class, Godot-free, in the crew feature's `logic/`, read on a timer. It gives each drone one motion and target:
dart (meteor, bolt), lean (hunted) or pulse (a friend's air `Low`), in that fixed priority. The flash is a `PointLight2D` on
the drone in the breach alarm's red (`Fx`), lighting the dark in scanner mode too. Reach, timings, colours in `game/Content`.
Nothing in the sim; nothing new sent or saved.

## Trade-offs accepted

- A dart flies straight through walls: it shows where, not the way.
- One's own air and cold get no drone signal: the HUD and the tunnel already speak.
- Venting and storms are not echoed on the drone.

## Tests

- Client units: dart beats lean beats pulse; a meteor beyond reach and a bolt outside its spread ignored; the pulse only
  for friends.
- A creative frame run with `BoltNow` and a meteor, read frame by frame.

## Decided

- **No unlock:** alarms come with the drone (the user, 2026-11-18).
- **Its habits come first:** the drone is a companion with a mind of its own, so an alarm's motion wins over the chosen
  mode: a dart in lamp mode carries the lamp with it, and it comes back after.

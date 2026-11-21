# The light drone: the hero's companion in place of the helmet lamp and the scanner

- Status: accepted; supersedes in part [the-scanner](2026-11-07-the-scanner.md) (an item worn on the helmet's mount),
  [0077](0077-a-planets-ground-grows-from-its-seed-and-is-dug-by-hits.md) (the helmet lamp, an item),
  [gestures-for-words](2026-11-16-gestures-for-words.md) and [crew-pings](2026-11-16-crew-pings.md) (their key, F)
- Date: 2026-11-21

## Context

Once helmets were bolted still ([gear-roles](2026-11-21-gear-roles.md)), nothing could mount on them, so the lamp and the
scanner lost their place. They were also two items fighting over one slot. Gestures took the hand off the held item and
were hard to read from far away.

## Decision

- **One drone, the Companion** («Спутник», item `lightDrone`, `Content.LightDrone`). It floats by a paired hero's head,
  its lens turned where it shines. How a hero comes to have one is [drone-companion](2026-11-21-drone-companion.md). The
  `mount` slot, the `helmetLamp` item and the `scanner` item are gone.
- **Modes, the client's** (`DroneMode`: `Lamp`, `Scan`; `DroneModes`). A tap of F goes on to the next open mode; held, F
  opens a wheel (`Wheel<T>`, shared with gestures). The mode is never a command and never saved: a reload starts on the
  lamp. It is sent with the hero (`Seen.Drone`, nothing for the lamp), so friends see the beam or the scan.
- **It comes dark.** Each mode opens with a market node, bought once for the crew: the lamp `droneLamp` in the first ring
  (draws no power), the scanner `scanner` in its old place. With no mode open, the drone rests on its orbits and gives no
  light. Scanning puts the light out and shows a wreck's hit points (features/scanner).
- **Gestures move to H.** A tap points and pings; held, H opens their wheel. The drone shows the gesture as a cyan
  hologram over itself (`DroneSign`, `DroneHolo`), so friends far off read it.
- **The look** is the pick «Пепел в латах» (ash in armour), after «Диск»
  ([page](https://claude.ai/artifact/U15TWJa1rX5Bpwi7yEfGRo)) and «Рой» ([page](https://claude.ai/artifact/3EPsnP1hpmhUKpdofvg9Bx)):
  an iron core and its lens, a cloud of soot, four scrap plates riding it as armour (`drone_core`, `drone_plates`,
  tools/anime-art `drone.js`). Each shape re-forms both (`DroneSwarm`): idle orbits like an atom; the lamp a soot mirror
  with the plates in a dish before it; the scan a spyglass, two draws of plates along the aim, the front one sliding to
  focus. Plates fly home late by their index and turn over on the way; one mote in thirteen is an ember. Orange arcs run
  from the core to each plate and crackle now and then.

## Deliberately not handled

- **The mode is not saved or hashed.** It changes nothing in the sim; if a mode ever does, it becomes a command then.
- The drone ignores walls; its place is drawn by the client and never reaches the sim.
- Not built: the magnet mode, drone skins, power draw, scanning creatures, sounds for modes, rebinding the wheel's sides.
- Alarms were designed and cut; the design waits in `docs/plans/drone-alarms.md`.

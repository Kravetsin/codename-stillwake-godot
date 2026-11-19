# Plan: the drone's swarm look

The user's pick on the [look page](https://claude.ai/artifact/3EPsnP1hpmhUKpdofvg9Bx), 2026-11-19: «Рой», an iron core holding scrap
shards by its field; per mode the shards fly along field lines into a new shape. It replaces the «Диск» of
[light-drone](light-drone.md) (step 5 of [drone-companion](drone-companion.md)). The user also liked «Пыль» (dots that re-form into
anything) and «Оригами» (plates flipping edge-on): borrowings from both are later, not here.

## What the player sees

- **Idle and lamp** (today's lamp mode): ten shards circle the core lazily, each turning on its own; in lamp mode they fly into a
  dish behind the core, open toward the pointer, and the lens glows warm.
- **Scan:** they fly into a comb in front of the core that sweeps; the shards glow teal at their ends.
- **A gesture playing** (the hologram, light-drone step 3): they fan out toward the sign, as the pick's hologram mode, then go back to
  the mode's shape when it ends.
- **Changing shape:** each shard leaves late by its index, flies in an arc to its new place with an overshoot, and a faint thread of
  the field's light runs from the core to it while it travels. This is the point of the look.
- A friend's drone does the same from their mode (`Seen.Drone`) and their gestures.

## Out of scope

- The magnet mode's ring (no magnet yet) and the grown version with more shards (no drone upgrades yet): their formations wait for
  their mechanics. The shard count is a tunable.
- Dust and origami borrowings; the drone's habits (its own will, alarms' darts); the pen hologram («Перо», its own step).
- Icons: the item `lightDrone` and the pod's card show the swarm idle, exported once.

## Approach

- **Art** (`tools/anime-art/subjects/drone.js`, the pick's own pen and paint): `drone_core`, the core with its rust crust, one
  picture (it is round; the lens is drawn in code as today); `drone_shards`, each of the ten shards at 8 turns in a row, lit from
  above as the pick paints them. A shard is drawn at its nearest turn, turned the rest of the way, as the disc is now.
  `items/lightDrone`, the swarm idle. `drone_disc` goes.
- **Formation logic** (`game/features/crew/logic/DroneSwarm.cs`, Godot-free, tested): per shard, its pose in a shape (`Idle`, `Lamp`,
  `Scan`, `Sign`) from the aim's angle and the time, and the pick's `morph` between two shapes by the time since the change, its
  delay per shard, the overshoot and the arc. It reports position, turn, in front of the core or behind, glow, and how far along
  the flight (for the thread). Numbers (radii, delays, speeds) are the pick's, a quarter of its canvas pixels, in a `Tune` block.
- **Drawing** (`LightDrone.cs`): the shards behind, the threads, the core, the lens, the shards in front. The band and its marks go.
  The lamp's light keeps coming from the lens, where it is now.
- **Sign:** the hologram's state (`DroneSign`) drives the `Sign` shape while a gesture plays.

## Tests

- The logic: every shape puts every shard within its reach of the core; a change ends exactly in the new shape after its last
  delay plus the flight; no shard is mid-flight once done; the lamp's dish opens toward the aim on both sides; idle shards never
  stop moving; a shape held stays put up to its own motion.
- Frames, by the lead: the dress sheet or a creative run, idle, lamp, scan, a gesture, and each change.

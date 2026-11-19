# Plan: the drone's ash look

The user's pick on the hybrid look page, 2026-11-19: «Пепел в латах» (ash in armour). It replaces the «Рой» picked the same day on
the [first page](https://claude.ai/artifact/3EPsnP1hpmhUKpdofvg9Bx) (and the «Диск» of [light-drone](light-drone.md) before it,
step 5 of [drone-companion](drone-companion.md)): the iron core and its lens stay; its body is now a dense cloud of soot, and four
big scrap plates ride it as armour. Each mode re-forms both.

## What the player sees

- **Idle** (before its first mode): the soot runs round three tilted orbits about the core like an atom, the middle one the other
  way; the plates circle wide and slowly tumble over, showing their rusty backs.
- **Lamp** (today's lamp mode): the soot forms a thick mirror behind the core, its inner face lit warm; the plates stand in a dish
  before it, open toward the pointer.
- **Scan:** the soot forms comb teeth reaching ahead that sweep and flicker, lit teal at the tips; the plates line its back.
- **A gesture playing** (the hologram, light-drone step 3): the plates fan up toward the sign and glow; the soot keeps its idle
  orbits (the pick's own hologram soot lays out the bag's grid, a later step). Back to the mode's shape when it ends.
- **Changing shape:** each plate leaves late by its index, flies an arc to its new place with an overshoot and turns over once on
  the way, its back showing mid-flight, a faint thread of the field's light running to it; each mote is drawn into the core along
  its old thread and out along the new one to its place.
- **Embers:** about one mote in thirteen is red-hot, flickering orange with a small glow, whatever the shape.
- No glints: a plate turned edge-on does not flash (the pick page's white line and cross were rejected as artificial).
- A friend's drone does the same from their mode (`Seen.Drone`) and their gestures.

## Out of scope

- The magnet mode (no magnet yet), the grown level (six plates, 260 motes; no drone upgrades yet) and the bag's grid of soot (the
  bag hologram, a later step of drone-companion): their shapes wait for their mechanics. The counts are tunables.
- The drone's habits (its own will, alarms' darts); the pen hologram («Перо», its own step).
- Later user, named by the user: a shield module formed from the plates.
- Icons: the item `lightDrone` and the pod's card show the drone idle, exported once.

## Approach

- **Art** (`tools/anime-art/subjects/drone.js`, the pick's own pen and paint): `drone_core`, the core with its rust crust, unchanged;
  `drone_plates`, a row per plate, its face at 8 turns, then its rusty back (the shape mirrored, rusty paint, no rust blob) at 8,
  lit from above as the pick paints them. A plate is drawn at its nearest turn, turned the rest of the way and squashed across its
  long axis by how far it is turned over, as the pick's `flipY`. `items/lightDrone`, the drone idle. `drone_shards` goes.
- **Logic** (`game/features/crew/logic/DroneSwarm.cs`, Godot-free, tested): per plate, its pose in a shape (`Idle`, `Lamp`, `Scan`,
  `Sign`) from the aim and the time, the pick's `morph` between two shapes and its `flipK` turn; per mote, its home in the soot's
  shape (the sign's is idle) and the pick's `flowDust` re-forming along the threads, its delay its own; the hot motes' flicker. Its
  seeds are the pick's (its xorshift, ported). A change mid-flight starts from where everything is. Numbers are the pick's, a
  quarter of its canvas pixels, in a `Tune` block.
- **Drawing** (`LightDrone.cs`), in the pick's order: soot behind, plates behind, the core, the lens, soot before, plates before; the
  threads behind everything. The soot is one triangle array per side (a dark rim, its tone, a lit top a mote); the field's light on
  it and the embers (a rect and a soft fan each) one more, added on the glow node with the lens's haze and the plates' halos.
  The lamp's light keeps coming from the lens.

## Tests

- The logic: every shape keeps every plate and mote within reach of the core, re-forming too; a change ends exactly in the new
  shape after its delays; no plate is mid-flight once done; a flying plate's turn passes through edge-on to its back and again;
  held shapes face on, idle plates tumble; the dish and mirror sit behind toward the aim; the comb's teeth glow at their tips; idle
  orbits never stop; the sign's soot keeps its orbits; a change mid-flight never jumps; the hot share is the pick's and only those
  glow.
- Frames, by the lead: the dress sheet or a creative run, idle, lamp, scan, a gesture, and each change.

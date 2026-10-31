# The first hour's pace

- Status: accepted
- Date: 2026-10-31
- Amends [photo-panel](2026-10-29-photo-panel.md): how the solar panel is known; [0068](0068-the-harpoon-before-the-jetpack.md):
  the harpoon's recipe; [0041](0041-wreckage-of-many-kinds-and-circuits.md): the stream's weights

## Context

The first solo playtest of the rings: paying the gates before the jetpack was a grind, mostly of hand work at a workbench
(the machinery gate alone took 28 minutes of it); the nailer opened before the harpoon although nothing it answers is met before the
jetpack; a harpoon outran the stream, a piece every 10 s, of which a fifth gave iron, so a player waited and could not grow the ship;
and the build menu offered a solar panel from the first minute, which nobody can build. The user would rather a player can lose than
keep it for the sake of one who takes the wreck's own apart.

## Decision

- **The gates up to the jetpack ask 40 % less:** first metal 12 scrap and 6 iron, the press 12 wire, 8 plates and 6 gears, machinery
  18 plates, 8 coils and 5 rotors, processing 6 frames, 12 gears and 12 coils. The press is 5 minutes of a workbench's work, machinery 16.
  The gates after, on machines by then, stay.
- **The harpoon opens in the first metal ring, the nailer and nails in the press's.** The harpoon loses its coil, which opens a ring
  later: a tube and 8 wire, the same worth.
- **The stream brings a piece every 4 s** (60 ticks plus up to 40, at most 60 out, 20 at the start), about all a harpoon can reel in.
  What it adds is plain salvage: scrap chunks, hull plates, and twice the crates and half again the supply pods. Wrecks,
  racks and soil clods come about as often as before, so found circuits and seeds stay as rare.
- **The solar panel is out of the crafts** (`Research.NeverKnown`): no block opens it, and taking one apart does not either. The wreck's
  own taken apart is gone for good, its parts in hand. We accept that a player can hurt themself: it is a loss, not a dead end, as
  the lifepod's air and a death at the pod ask for no power, the furnace and the workbench neither, and the burner generator opens by
  the machinery gate.

## Consequences

- A world saved earlier keeps the panel known, as it keeps all it knows; no format change. We do not take it back: only our own
  test worlds have one.
- More salvage is more credits all game, not only in the first hour. We do not scale the stream down later on: the far field and
  the machines soon outrun it anyway, and the 5-hour target is for the next playtest to check.
- We do not touch how long a craft takes: the workbench stays slow, which is what makes the Former worth building.

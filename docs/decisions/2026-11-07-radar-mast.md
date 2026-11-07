# The radar antenna is a guyed mast

- Status: accepted
- Date: 2026-11-07

## Context

The radar antenna's picture was a dish 17 px wide on a thin pole over a small base: it filled half of its 4×4 footprint
and read as something two cells wide and three tall. The user asked for it redrawn so that its footprint shows. Three
pictures filling the 4×4 cells (a big dish on a turret, a lattice tower, a ship's bar array) were too square for the user,
who asked for a taller antenna, 4×6. Of three tall ones (a guyed mast, a lattice tower, a telescopic mast) the user picked
the guyed mast, with its dish made as wide as the footprint.

## Decision

- **The radar antenna is 4×6 cells**: a lattice mast tapering up from a bolted foot, two faded red bands near its head,
  four guy wires to poured anchors at the footprint's corners and sides, and a dish four cells across turning at its head.
  A red light sits on top. The cable comes down the mast to a junction box, its plug at (13, 42).
- **The picture is split as before**: `radarAntenna_mast` (all that stands still), `radarAntenna_dish_<f>` (the dish in
  eight frames, edge on at 0 and 4, its face at 2 and its ribbed back at 6, the light's dark glass on its axis) and
  `radarAntenna_beacon` (the light lit). `machine_radarAntenna` is the mast and frame 2, the light dark.
  `tools/anime-art/subjects/machines.js` (`radarMast`, `radarDish`, `radarBeacon`).
- **Saves of format 51 move every radar antenna up two rows**, built or planned (`Migrations`, 51 → 52): each keeps the
  roof it stood on and grows over the two cells above.
- The pick page: https://claude.ai/artifact/MXK8VJ65HsN1BSwgozYf4E.

## Deliberately not handled

- **The dish does not turn in the game yet** and the light does not blink: the game draws `machine_radarAntenna` alone, as
  it did the old picture. The frames are drawn for the view that will play them.
- **Something an old save built in the two cells above an antenna** overlaps the mast once upgraded; nothing is released.
  The antenna stands on the roof, where only open space is above it in every ship played so far. Take one of the two apart.

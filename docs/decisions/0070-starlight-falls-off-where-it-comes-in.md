# 0070 — Starlight falls off where it comes in

- Status: accepted
- Date: 2026-10-19
- Amends: the starlight of [0019](0019-light-by-cells-in-colour.md) and [0029](0029-light-composed-in-lightmaps.md)

## Context

Starlight came from every open cell with no back wall at full strength and spread by row and column
sweeps keeping 0.88 a cell, so it still held a quarter of itself ten cells on. One missing back wall lit
a whole room cold blue; patching it made the room go suddenly dark. A torn room was as bright at its far
wall as at the tear. Starter lamps were bright too, out to 16 cells, so a furnace's fire, a screen and a
machine's glow hardly showed.

## Decision

- **Where the stars shine, as much as the sky is open.** A cell the stars shine into (no back wall, open
  or a shaped wall's open half, or past the grid) is lit by how many of the 5×5 cells round it, itself
  among them, the stars shine into (`Light.StarSky` = 2): one alone, a hole in a back wall, gives
  `Light.StarHole` (0.5); `Light.StarFullSky` (12) or more give the stars whole. Open space is whole
  everywhere, beside a flat hull too (15 of 25 open); a hole of a few cells is between.
- **Falling off from there.** The sweeps stay, but crossing a cell keeps `Light.StarKeep` (0.6), a
  machine's `Light.StarMachine` (0.48). Through a doorway or a tear the room takes 0.6, 0.36, 0.22… of
  the stars; round a hole half that. Walls keep `WallIn`, `WallDeep`, `WallOut` as before.
- **Weighed: only the cells on the border with space give light**, faint, into the room. This gives the
  same where it matters — light enters at the border and falls off inward — without making a border cell
  faint itself: the open space just outside a doorway or a tear would then show a dim stripe where open
  space must stay whole.
- **Starter lamps dim and warm:** `Light.Lamp` (0.8, 0.64, 0.42), keeping `Light.LampKeep` (0.84) a cell
  out to `Light.LampReach` (11), against (1.05, 0.91, 0.665), 0.88 and 16. Ambient (0.04, 0.045, 0.06),
  from (0.05, 0.055, 0.07).

## Consequences

- A hole lights a few cells round it and leaves the room as it was; patching it changes only those.
  A torn or open room is lit near the opening and dark at its far end: the wreck's room B shows its
  analyzer by its mark, not by starlight.
- Still worked out only around a change: a cell's sky reaches 2 cells, so a change is written within
  `Reach` + 2 of it, from what lies within 2 × `Reach` + 2. `Reach` is now the brightest way through cells
  of any kinds, step by step (12 cells), not the largest share to a power (45). A change costs 0.2 ms
  instead of 1.7 (debug build, any ship size); laying everything out anew costs about as much as before
  (4.5 ms on 256×128, 41 ms on 1024×512, against 4.5 and 37), for counting the sky.
- Shorter lamps cost a little less: 1.73–1.82 ms a frame at zoom 1 on the reference world with every lamp
  lit, against 1.87–2.02; 0.91–0.96 at zoom 2, against 0.88–0.93 — within the runs' spread.
- Not handled: the sky is counted in a square, not along lines of sight, so a hole's light does not
  depend on how the hull round it is shaped; and a big hole is brighter than a lone one only by its count.

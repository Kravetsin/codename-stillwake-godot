# A tool hits by a fan of rays from the shoulder and shows its reach in use

- Status: accepted; supersedes the swept blade of [melee-edge](2026-11-05-melee-edge.md), the drawn edge of
  [tool-edge](2026-11-05-tool-edge.md) for the drill and grinder, and the swing's "flicks" of [damage-looks](2026-11-05-damage-looks.md)
- Date: 2026-11-05

## Context

The drawn edge hit too little. The drill's bit starts about three cells from the shoulder, so on a planet it broke a far cell
and left the one beside the hero standing, walling it in; a swing tested only its downstroke's last stretch, so a hammer drawn
through a wreck did nothing. The older fan of rays hit well but showed no reach, so its sparks looked magnetised. The user asked
for rays from the shoulder again, a longer reach, and the reach shown by a look that suits each tool, only while it is used.

## Decision

- **A hit is the weapon's fan of rays from the shoulder** toward the pointer (`ToolRays.Fan`): `Arc`, `Rays`, `Range` from
  `Weapons`, each heading from the sim's `Rays.HeadingOf`, walked by `Rays.Cast`, middle ray first. A ray stops at the first
  cell that stops a body and strikes it; cells it passes before are struck too (plants). One ground touch a hit, where the
  first ray stops.
- **Wrecks by their pixels**: along a ray, each wreck's opaque pixels as drawn (`WreckPixels`) sampled every `ToolEdges.Step`;
  the first one met stops the ray. One wreck a ray, as many as the fan meets.
- **Creatures by their box** on the walked stretch; none behind a wall or wreck.
- **When is unchanged**: a swing as its blade crosses the aim, a thrust at full stretch, a running tool every `Pace` ticks.
  The commands are tool-edge's: `HewCells`, `HewPieces`, `Strike`. The cutter keeps its one-ray beam (`ToolEdges.Hits`).
- **The reach is shown in use only, never clipped by walls** (`AirCuts`, drawn by `ReachAir` in the arms' frame): it shows how
  far and how wide; the hits show what. The user picked both looks on Artifact pages:
  - axe, sledgehammer, spear, «Пыль по ходу удара» (`dustDrift`): motes torn off where the blade passes, carried along the arc
    the way the stroke goes, each at its own radius out to `Range`, stopping by the arc's end; a thrust's carried forward to
    `Range`. Seen `CutFadeOf` ms after, the sledgehammer's longer;
  - drill and grinder, «Сухая кисть» (`dryBlur`): the working part grown, in a few broken dry-brush strokes stepping round as it
    turns. The drill's bit point is a cone from the bit's tip to `Range`, its base `ConeBase` either side; the grinder's forward
    half of its disc, radius `DiscRadius` round the hub. Eased in and out (`ZoneIn`, `ZoneOut`).
- **Numbers**, so the shown zone and the fan agree: the drill's fan 32 steps (an eighth of a turn) wide, 7 rays, 8 cells; the
  grinder's 62 steps, 11 rays, 6.6 cells. The axe, sledgehammer and spear keep theirs.

## Consequences

- The face beside the hero is dug first, so a tunnel opens where the hero stands; a hammer drawn through a wreck hits it.
- The drill digs about the hero's height of face a hit, faster than before; `BlockDamage` is not retuned here.
- `SwingSmear` and `SwingTrail` are gone; `Aiming.StrokeEdges` and the grinder's round bite with them.
- The sim's `Hew`, `HewWrecks` and `Struck` stay: shots and the sim's tests use them.

### Deliberately not handled

- The look shows the whole fan where a wall cuts rays short: reach, not targets.
- A wreck drifting a pixel between frames may be missed by one ray of one hit.
- A friend's look plays from its sent swing and busy state, as its swing does; its exact pointer is not sent.
- At the game's zoom the looks are small; their sizes are data in `WorkLooks`.

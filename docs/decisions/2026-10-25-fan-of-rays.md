# A tool's hit is a fan of rays

- Status: accepted; supersedes, for wrecks, the blade's contact of [0039](0039-melee-by-contact.md) and
  [0040](0040-a-melee-weapon-is-held-by-its-handle.md) and the bit's touch of [0054](0054-asteroids-and-the-drill.md), the
  dig brush and its one-cell mode of [0077](0077-a-planets-ground-grows-from-its-seed-and-is-dug-by-hits.md) and the back
  wall struck behind its wall of [0107](0107-left-is-front-right-is-back.md)
- Date: 2026-10-25

## Context

A hit landed wherever the pointer was: the 2×2 brush dug cells deep in the rock and behind walls, and Ctrl struck one cell
anywhere within the hero's twelve cells of reach. In space a wreck was hit by whatever the drawn blade or bit touched,
judged by the client. The player could not feel the tool strike the surface in front of them. The user asked for the hit to
be a fan of rays from the hand, each stopping at the first thing it meets, computed in the sim.

## Decision

- **A fan of rays.** A weapon (`Content.Weapons`) has an arc in steps of a turn (`Headings.Turn`, 256), a ray count and a
  range in thousandths of a cell. Its rays spread evenly over the arc round the aim, each offset truncated toward zero. The
  axe: a quarter turn, 9 rays, 3.5 cells, a wave that takes a band of a wall's face. The drill: 8 steps (11°), 3 rays, 5.5
  cells, one or two cells.
- **Cast in the sim** (`Rays`, Core): an integer walk over the cells (DDA), directions from a quarter table of sines in
  thousandths (`Headings.Sines`), mirrored to the whole turn. A ray asks each cell it crosses, its start's first, and stops at
  the first that stops it; a cell it only touches at its end is not asked. Through a cell's exact corner it asks both cells
  beside it and stops if either stops it: two walls touching at a corner close the gap, and the walk is the same mirrored.
- **What a ray meets in front** (`Hew(Tool, X, Y, Heading, Back)`, Building): a wall stops it, a natural one that breaks is
  struck, a ship's, bedrock and lava are not; whatever stops a body stops it; a plant in an open cell is struck and passed
  through. Each wall and plant takes the weapon's block damage once, however many rays meet it: walls by cell, then plants, a plant
  its wall took already left out; a relic's cradle among them shocks its striker once (0096).
- **Behind** (`Back`, the right button): a ray strikes the natural back wall of every open cell it crosses, a ship's passed,
  until a wall stops it. A back wall behind a wall is no longer reached.
- **In space** (`HewWrecks(Tool, X, Y, Heading)`, Debris): each ray strikes the first wreck whose box (its size in cells,
  `Wreckage.Sizes`) covers part of a cell it crosses, the first spawned of those in one cell, and stops there or at a wall. A
  wreck whose middle is not in space is passed; the hero must be in space. Two commands, not one: ground and wrecks share no
  surface in play, and one command would have Building read Debris against the run order.
- **Refusals:** `building.nothingToDig` and `debris.nothingToHit` for a fan that strikes nothing, then the weapon's
  (`items.flat`, `items.notRunning`). The hand point is the client's word, among friends (0002); the range bounds the walk.
- **The client** casts from the shoulder toward the pointer, one `Hew` and in space one `HewWrecks` a hit, each sent only when
  the sim's own query (`Struck`) strikes something. Creatures stay on the blade and the bit (0102).
- **The preview:** every cell and plant the hit would strike is outlined, a square each, green or red as the aim (the same
  query, so it never lies), over the hero; nothing while the fan strikes nothing. No reach shown, no particles, no words.
- **Block damage apart** (`Weapon.BlockDamage`): walls, back walls and plants take it, wrecks and creatures `Damage`. A
  weapon made to fight will hit hard and break blocks slowly, so fights do not end in ruins. A module's damage gain (0098)
  scales both.
- **Numbers.** The axe 2 and 4 on blocks, the drill 1 and 4 (its arc module 2 and 8); wrecks as before. Plants and the
  ruins' pieces doubled in strength, so they fall to as many swings as before. Measured by seconds to cut a 4×20 tunnel of
  stone (80 cells, strength 12): the axe about 4 face cells a swing, stone at three swings, 30 s; the drill about 1.5 cells
  at 10 hits a second, stone at three hits, 16 s. Soil goes at one hit of either. To be tuned by feel.

## Deliberately not handled

- Creatures are still struck by the drawn blade and bit.
- A fan may skip a face cell between its outer rays; ray count is content.
- A wreck that drifts between the preview and its tick may be missed by a hit: one swing lost.
- A wreck is struck by the cells a ray crosses, not its picture's outline: up to a cell of slack.
- A machine does not stop a ray, as it does not stop a body.
- A cheating client may hew from anywhere: no anti-cheat (principle 6).

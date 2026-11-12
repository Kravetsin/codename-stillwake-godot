# The grow capsule grows a gourd vine

- Status: accepted
- Date: 2026-11-13

## Context

The grow capsule showed a green stick with diamond leaves and yellow dots for fruit. It read as a placeholder, and it did
not match the pear-shaped fruit a seed now gives ([fruit-not-biomass](2026-11-12-fruit-not-biomass.md)).

This was also the first pick made from generated references. An image model (gpt-image, through the Figma connector) drew
five really different plants, each in four stages bearing the fruit. The user picked the gourd vine on a stake, and Claude
redrew it in code. Generated pictures are references only: the game's pictures stay drawn by `tools/anime-art`, by its
rules.

## Decision

- **A rusted stake** stands in the capsule's soil at every stage, part of the capsule, not of the plant.
- **A vine winds up the stake** (`plantStage` in `tools/anime-art/subjects/machines.js`). It is drawn as a helix: the runs
  behind the stake first, then the stake, then the runs in front. It reaches higher at each stage, and from the second
  stage it ends in a brown tendril.
- **Heart leaves on stalks**, drooping outward and alternating sides: 2, 3, 4, then 5.
- **The last stage carries four pear gourds**, olive like the fruit's icon, each with a curled brown stem, hanging from stalks
  under a leaf.
- The leaves are muted greens, darker than the old plant's on the dark glass. The user saw them in play at the testbed and
  kept them.

## Deliberately not handled

- Spores grow the same vine, though they give glowberries. A plant of their own is a later pick.
- The plant still does not sway or grow between its four pictures, as before.

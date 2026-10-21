# 0082 — The ground's chipped look: details over it and a living edge on top

- Status: accepted; supersedes 0079's moss tops; its edges amended by [0085](0085-back-walls-recede-solid-edges-catch-light.md)
- Date: 2026-10-20

## Context

The first planet's ground (0079) read as flat noise: one 8 × 4 tile a material, moss only on soil under the sky. Of three
directions drawn for the ground the user picked D1, «Сколы» (chips): angular chunks with facets, soil in rounded clods,
bedrock in big slabs, for both the texture and the edge on top.

## Decision

- **The tiles are D1's**, exported from its painter into `game/art/planet`: `wall_*` and `back_*` replace the old ones
  and are cut 8 × 4 as before (`ShipTiles.CutAt`), cut to slopes by `ShapeMasks` as before.
- **Details over the ground** (`render/GroundDetails.cs`, drawn by `GridView` on a layer with the walls', after them and
  before the edges): `detail_*` holds eight single-cell pictures (a pebble, a root, a crack, a hole) and three features —
  two 2 × 2, one 3 × 3. About 40 % of whole cells show a single; a block of 4 × 4 cells holds at most one feature, 7 % a
  3 × 3 and 16 % a 2 × 2, laid only where every cell it covers is a whole wall of one natural material. Slopes show none.
  A hash of the position alone, client-side: nothing saved, every peer alike. A feature lies within its block, so a dig
  redraws the details of that block only, and a half-dug boulder goes whole.
- **A living edge on top** (`top_*`, 16 × 32 a picture laid from the cell above, as moss was): four flat, two ◢, two ◣ a
  material, each in two rows. The back row is drawn behind the heroes, with the edges; the front row — a few tufts or a
  chip, at most two pixels over the ground — before them (`Layers.GroundFront`). The ground a hero stands on does not move:
  soil's moss cushion lies in the back row, so boots stand on the ground and only thin tufts overlap them.
- **Where the edge shows** (`Grass.Edged`): soil's moss where it grew, under open sky; stone and bedrock show their chips
  under any empty cell — a cave's floor and a dug hole's too, as rubble would lie there.
- The painter is the pick page's (`ground2/paint.js`); one change from the pick: soil's moss cushion moved from the front
  row to the back, and lowered to two pixels, so feet do not sink into it.

## Measured

On this machine (RTX 4060 Ti), 1280 × 720, the planet against #121's:

| What | Before | After |
| ---- | ------ | ----- |
| `--planet --measure` | 1016–1042 fps | 817–980 fps |
| `--ground=band1 --measure` | 796 fps | 720–733 fps |
| `--ground=sprint --autopilot --frames`, avg / p99 | 1.07 / 1.52 ms | 1.32 / 2.36 ms |
| chunk reveal, worst frame | 0.50 ms | 0.86 ms |

## Deliberately not handled

- **Slopes show no details:** a pebble cut by the diagonal reads as a mistake; their living edge is enough.
- **Stone chips lie on every open stone top,** caves included; in the dark they read as the floor's silhouette.
- **Built walls of soil or stone** show details and edges as the ground's own do: the rule reads the material, not who
  placed it.

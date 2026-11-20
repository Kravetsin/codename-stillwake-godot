# The title is a hero on a tether against a sun

- Status: accepted; supersedes the scene of [title-scene](2026-10-25-title-scene.md)
- Date: 2026-11-21

## Context

The title's scene, a hero hunched on a drawn chunk of hull with shards and astronauts drifting by, looked poor: the
sadness was acted by the pose and read as a cartoon, and the plate, drawn up close, read as cheap. Before it, a freighter
and a raider drawn in code had read as cheap too. Detailed things drawn large are what fails here; light, gradients and
silhouettes are what code draws well. The user asked for something simple that is sure to look good.

## Decision

- **Vast space, a tiny hero, no ship.** The hero, about 6.5% of the frame's height, limp and spread, turns slowly (a
  whole turn in about 46 s) on a tether that leaves the frame at its foot. What it is tied to is never shown.
- **Against a small distant sun.** A hard warm-white disc in a soft falloff; the hero sits partly inside it, a dark
  silhouette with a warm rim on the sun's side, a glint on the visor, a tiny red lamp blinking on the chest. The tether is a
  thin dark line, its edge toward the sun lit. Sparse stars of varied brightness drift left in two layers.
- **Lit by shifting the silhouette.** The hero is drawn flat into its own buffer; a shader dims it to shade, then lays a
  lit step and a rim where the body is and the body moved toward the sun is not. The way to the sun follows the hero, so
  the rim slides over the limbs as it turns, and it reads as a body in the round.
- **Soft light, by exception.** The ink-and-watercolour rule wants hard steps and no painted glow; the sun's falloff and
  the hero's light are soft here. The user compared both on the pick page and chose soft for this scene alone.
- **Picked on a page, ported whole.** Three candidates (a planet's crescent, a cut tether, the sun) in both lights; the
  page (`tools/pick-kit/pages/title-tether.js`) is the source of every number, and the game draws it in its 1280 × 720
  units, scaled to the window.
- Kept from [title-scene](2026-10-25-title-scene.md): the scene first, then the name, then the choices; words over the
  darkened left; the left third calm for them.

## Consequences

- Gone: the plate and its picture, the sitting hero, the drifting shards and astronauts, the red emergency lamp.
- The hero is seen from the front, unlike the game's side view: spread, a side view reads as a blob.
- Not done: the scene changing with progress, the player's own suit on the title, a second scene.

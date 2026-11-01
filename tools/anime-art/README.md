# Anime art

Draws the anime art's pictures in code and writes them into `game/art/hd/`, where `game/render/HiRes.cs` picks
them up in place of the pixel pictures of the same names, with `-- --anime-art` or an `--anime` demo. Off by default until
the switch ([the plan](../../docs/plans/anime-art.md)).

## The style

Ink and watercolour ([why](../../docs/decisions/2026-11-01-ink-and-watercolour.md)), neutral to slightly grim, **never a cartoon**. The
finish is `wash.js`, one for every subject; these are the rules a drawing must follow for it to work.

- **Mood before readability.** A lone scavenger's world: judge a picture by that first, and beside what it replaces.
- **Colour:** clean, slightly grey paint, neither dusty nor faded. Metals are cool greys; suit, copper and wood are warm and worn. Full
  saturation is only for what works or warns: fire, lamps, screens, hazard stripes.
- **Light:** hard steps of tone, one shade and one deep, no gradients and no painted glow. The hero is lit from straight above, so
  turning changes nothing; the world from the side. The game's own light does the rest.
- **Line:** a thin pen that wanders, swells and lifts off. Heavier round an object, lighter inside, never an even border like a sticker's.
  Paint may miss the line a little, not so much that it looks careless.
- **Water:** a trace of tide lines, pooling and grain. It is the finish's, one strength for all: never tune it per picture. It darkens or
  lightens paint by a factor and never mixes in paper: on a dark colour that reads as dust.
- **Parting from the wall:** back walls stay well darker than anything before them, and every outlined shape has a thin lit edge at the
  top, drawn by the finish.
- **Wear:** rust grows from seams and rivets where water sat, with a torn edge and a darker heart. No long drips, no rings. Cracks are
  short pen lines; dents and patches sit where something hit or was mended.
- **Detail:** objects are composed, each part placed by hand: about 20 calls for a 16 × 16 icon. Walls are quieter than what stands
  before them, back walls dark and quietest. Random scatter is for the edge of a placed thing, never for placing it.
- **Ground:** one body, not paving: large masses of close tone, a hard shadow at their foot, only some cracks drawn. Grass lies over the
  block's top as part of it, thick, a deep muted green.
- **Do not:** lay a filter or an outline over finished or pixel art, or draw a wall as a texture of noise.

## What it draws

| Subject   | Pictures                                                                                                  |
| --------- | --------------------------------------------------------------------------------------------------------- |
| `hero`    | `hero_<part>`: the rig's parts, suit and bare, helmet shell and visor apart, top-lit so facing changes nothing; `hero_mark`, `hero_mark_stripe`: the player's colour, white, tinted by Modulate |
| `machines` | `machine_<kind>` and their working overlays, fire, tank fill; exposes `AnimeKit` |
| `zfittings` | pipes, valves, sockets, hatches, platforms, ladder, floor, drone, lamps, charge bars, held axe and drill; sorts after `machines` to read `AnimeKit` |
| `items`   | `items/<id>`: the ten items of the slice's hotbar and bag                                                 |
| `room`    | `wall_` and `wall-edge_` of scrap and iron, `back_scrap`, `back_iron_*`, `door_closed_*`, `door_open_*`   |
| `wreck`   | `wreck_dress`, `wreck_tears` (a mask, no finish), from `sim/Content/Wreck.cs`; its rust grows from `wall_scrap`'s seams |
| `piles`   | `machine_pile*`: the hold's heaps                                                                         |
| `smelter` | `machine_smelter`, `smelter_work_*`                                                                       |
| `caves`   | stone, ice vein and soil (`wall_`, `back_`, `wall-edge_`, `top_`, `detail_`), `caves/moss_*`, `caves/mossGreen_*`, `caves/crystalIce*` |
| `creatures` | `creatures/<id>/<part>`, and `Content/logic/CreatureInk.cs`, their numbers: the picked designs of the rigged creatures |
| `tick`, `warden` | `creatures/inkTick/*`, `creatures/inkIstukan/*`, and `Content/logic/TickInk.cs`, `IstukanInk.cs`: the picked guards, posed by `TickRig` and `IstukanRig` |
| `flora`   | `planet/acacia_*` and their `_glow`: the pods stand where the logical glow picture has them, for the game lights from there |

## Run

```
node tools/anime-art/export.js              # every subject
node tools/anime-art/export.js room caves   # some
```

Needs Node 22 or later and Edge or Chrome (`ANIME_BROWSER` for another path). Then `tools/snap.ps1` imports the pictures.

## Add a subject

A file `subjects/<name>.js` setting `Subjects.<name>`, which calls `out('<picture>', canvas)` for each picture, the
canvas from `Anime.bake({ w, h, draw })`: `w` and `h` the logical picture's pixels, drawn at 4 times. Line weights
and the light are the engine's, one for every subject (`engine.js`), per world pixel; use them, not numbers of your own.
`wash.js` loads after the engine and replaces `bake`; a picture drawn straight on a canvas skips the finish.

- A picture must be exactly 4 times the logical one; the game reports the logical size and lays out by it.
- A sheet that repeats takes `tile: true`. A door's shut and open pictures differ only by the panel, or pixels stay behind.
- A `top_*` picture lays its ground line on row 16; rows above it stand on the cell above, rows below lie over the block, in its `front` picture.
- A cave picture keeps the rows its logical one uses (`LOGICAL` in the page), or its plant floats. Moss is cut at its block's edges along a
  run, px 4 and 20 of 24: every variant has the same cross-section there, its torn end past them.

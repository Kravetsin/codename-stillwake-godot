# Anime art

Draws the anime slice's pictures in code and writes them into `game/art/hd/`, where `game/render/HiRes.cs` picks
them up in place of the pixel pictures of the same names (`-- --pixel` leaves them out).

## What it draws

| Subject   | Pictures                                                                                                  |
| --------- | --------------------------------------------------------------------------------------------------------- |
| `hero`    | `hero_<part>`: the rig's parts, suit and bare, helmet shell and visor apart, top-lit so facing changes nothing; `hero_mark`, `hero_mark_stripe`: the player's colour, white, tinted by Modulate |
| `machines` | `machine_<kind>` and their working overlays, fire, tank fill; exposes `AnimeKit` |
| `zfittings` | pipes, valves, sockets, hatches, platforms, ladder, floor, drone, lamps, charge bars, held axe and drill; sorts after `machines` to read `AnimeKit` |
| `items`   | `items/<id>`: the ten items of the slice's hotbar and bag                                                 |
| `room`    | `wall_scrap`, `back_scrap`, `wall-edge_scrap_*`, `door_closed_*`, `door_open_*`                           |
| `smelter` | `machine_smelter`, `smelter_work_*`                                                                       |
| `caves`   | stone, ice vein and soil (`wall_`, `back_`, `wall-edge_`, `top_`, `detail_`), `caves/mossGreen_*`, `caves/crystalIce*` |

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

- A picture must be exactly 4 times the logical one; the game reports the logical size and lays out by it.
- A sheet that repeats takes `tile: true`. A door's shut and open pictures differ only by the panel, or pixels stay behind.
- A cave picture keeps the rows its logical one uses (`LOGICAL` in the page), or its plant floats. Moss has its band
  solid on the rows `CaveView` stretches.

# Anime art

Draws the game's pictures in code into `game/art/hd/`, where `game/render/HiRes.cs` picks them up: its only pictures, reduced to the world's pixels where a size is needed ([the pixels gone](../../docs/decisions/2026-11-03-pixel-art-removed.md)).

## The style

Ink and watercolour ([why](../../docs/decisions/2026-11-01-ink-and-watercolour.md)), neutral to slightly grim, **never a cartoon**. The
finish is `wash.js`, one for every subject; these are the rules a drawing must follow for it to work.

- **Mood before readability.** A lone scavenger's world: judge a picture by that first, by what it replaces.
- **Colour:** clean, slightly grey paint, neither dusty nor faded. Metals are cool greys; suit, copper and wood are warm and worn. Full
  saturation is only for what works or warns: fire, lamps, screens, hazard stripes.
- **Light:** hard steps of tone, one shade and one deep, no gradients and no painted glow. The hero is lit from straight above, so
  turning changes nothing; the world from the side. The game's own light does the rest.
- **Line:** a thin pen that wanders, swells and lifts off. Heavier round an object, lighter inside, never an even border like a sticker's.
  Paint may miss the line a little, never carelessly.
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
- **Do not:** lay a filter or an outline over finished art, or draw a wall as a texture of noise.

## What it draws

| Subject | Pictures |
| --- | --- |
| `hero` | `hero_<part>`: the suit, helmet shell and visor apart, later helmets' and bodies' (lights `_lit`, `_arcs`, `_pip`); `hero_outfit_<n>_<part>`, `_skin` under bare skin; hair, faces, beards by `LookTint`; cape, jetpack, lamp, sheet, tools; `hero_tracker`, `_light`: white, the player's colour by Modulate. Top-lit |
| `catchergear` | catchers' outfits: `hero_catcher<Body>_<part>`, `_upperArm_lit`; `hero_catcher<Helmet>_shell`, `_visor`, `_lit`, by `HeroKit` |
| `machines` | `machine_<kind>`, working overlays (fire, fills, bubbles); a `fitting` on its port (`Machines.Ports`); exposes `AnimeKit` |
| `zfittings` | `pipe_run`, `pipe_<arms>`, valves, sockets, hatches, platforms, ladder, floor, drone, lamps, held axe and drill; after `machines`, for `AnimeKit` |
| `items` | `items/<id>`; an `_empty` state is the full picture's calls, only the changing part dark; `tool_screwdriver` |
| `room` | `wall_`, `wall-edge_` and `back_` of scrap (rusted by `wreck`'s `AnimeSheetRust`), cast scrap (clean) and iron, `door_*` |
| `wreck` | `wreck_dress`, `wreck_tears` (a mask, no finish), from `Wreck.cs`; its rust grows from `wall_scrap`'s seams |
| `piles` | `machine_pile*`: the hold's heaps |
| `smelter` | `machine_smelter`, `smelter_work_*` |
| `shipwalls`, `grounds`, `doors` | `wall_`, `back_`, `wall-edge_`, `detail_`, `top_` of glass, mica, hide, alloy, bedrock, lava, plaster; `door_*_0` and the links `room` leaves |
| `caves` | stone, ice, copper veins and soil (`wall_`, `back_`, `wall-edge_`, `top_`, `detail_`), `caves/moss*_*`, `caves/crystalIce*` |
| `creatures` | `creatures/<id>/<part>`, their numbers in `CreatureInk.cs`: the rigged creatures |
| `tick`, `warden`, `cavehostiles`, `lurkerattacks` | `creatures/ink<Kind>/*`, `<Rig>Ink.cs`: guards, lurkers, a capback's gas |
| `tufts` | `planet/tuft_*`, `tuftReed_*`, `tuftFungus_*`: one plant of three to a tuft, by `FloraLooks.Drawn` |
| `flora` | `planet/acacia_*`, `_glow`: pods where the logical glow picture has them, as the game lights from there |
| `shrubs` | `planet/bush_*`, `rock_*`, `bell*_*`: dead thorn, boulders, hooded bells; light only in `_glow` |
| `leaves` | `planet/leaf_<look>_<plant>_<n>[_back]`: four falling leaves |
| `heavens` | `planet/sky`, `clouds` (far layers, any size), `giant`, `sun` |
| `cavelife` | `caves/` crystals, dripstone, fungi, threads, roots, cracks, geodes, fossils, opal and spire caches, lamp; dim `_glow` |
| `homes`, `homeskeep`, `homeslights`, `homeswall`, `homesruin` | `homes/*`, `_dark`, `_fed`, `_glow`; `ruins/spireLens_*`; `homes.js` exposes `HomeKit`, the others sort after it |
| `debris` | `debris/*`: rocks lit hard from a side, crates, plates, pods, scrap, derelicts; unlettered, they turn |
| `aboard` | `machine_salvage*`: salvage whole on the deck; numbered main engines and consoles, one per vessel |
| `net` | `machine_net` (booms), `net_mount`, `net_front_<n>` (holding n); `NetInk.cs` |
| `bay`, `salvage` | `machine_droneBay`, `droneBay_*`, `salvage_*`, `items/salvageDrone`; `BayInk.cs`, `SalvageInk.cs` |
| `scanner` | `hero_scanner`, `hero_<helmet>_scanner`, `_lit_<n>`, `items/scanner`; `ScannerInk.cs` |
| `quilted` | `hero_quiltedHelmet_shell`, `_visor`, `items/quiltedHelmet`: traced; `QuiltedKit` |
| `charge`, `missile` | `hero_blastCharge` (`icons/` lit), `shot_blastCharge_spark`: its fuse in flight; `shot_missile` |
| `edgekeys` | `holo_<key>`, `_glow`: the edge keys' white masks, glows |
| `signs`, `credit`, `capsule`, `gauge` | `sign_*`: bolts, plug, wrench, tips; `credit`, the chip; `capsule`, `capsule_flame_<n>`; `gauge_*`, the vitals' light |
| `upgrades` | `upgrade_bag`, `_belt`, `_tradeSlots`, `_canister`: wares, no item |
| `zleftovers` | the arc bench's and lamp's glows, `machine_emergencyLamp`, `module_arc_coil`, the pod's lit state and porthole, `wrist_*`, `tablet_*` (9-slice) |

## Run

```
node tools/anime-art/export.js [subject...]   # every subject, or some
```

Needs Node 22+ and Edge or Chrome (`ANIME_BROWSER` for another). Then `tools/snap.ps1` imports the pictures.

## Traced subjects

A picked AI-made reference is traced for its **form**; the line, paint and water stay ours.

- `refs/<name>.png` and `.palette.json`: its colours (`ref`) and our paint (`mat`); `single`: one part, `overlay`: patches.
- `node tools/anime-art/trace.js <name>` (ffmpeg) cuts it by its own ink into beziers, `traced/<name>.json`, committed: export
  draws from that, never the reference. `traced.js` places and paints them.
- AI-made work ships: say so in Steam's AI disclosure.

## Add a subject

A file `subjects/<name>.js` setting `Subjects.<name>`, which calls `out('<picture>', canvas)` for each picture, the
canvas from `Anime.bake({ w, h, draw })`: `w` and `h` the logical picture's pixels, drawn at exactly 4 times, as the game lays out. Line weights
and the light are the engine's (`engine.js`), per world pixel; use them, not numbers of your own.
`wash.js` loads after the engine and replaces `bake`; a picture drawn straight on a canvas skips the finish.

- A sheet that repeats takes `tile: true`. A door's shut and open pictures differ only by the panel, or pixels stay behind.
- A `top_*` picture has its ground line on row 16: rows above stand on the cell above, rows below lie over the block (`front`).
- A cave picture keeps the rows its logical one uses (`LOGICAL` in the page), or its plant floats. Moss is cut at its block's edges along a
  run, px 4 and 20 of 24: every variant has the same cross-section there, its torn end past them.
- A tool in the fist (about 4 px) is drawn to it: `hero.js` shrinks the drivers' paths by `Hand` (0.6: 10 px long), the pen
  kept. A held tool's `icons/` copy is 384 px long, for its icon (driver-icons).

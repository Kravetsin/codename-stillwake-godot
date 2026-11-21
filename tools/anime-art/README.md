# Anime art

Draws the game's pictures in code into `game/art/hd/`; `game/render/HiRes.cs` picks them up (pixel-art-removed).

## The style

Ink and watercolour ([why](../../docs/decisions/2026-11-01-ink-and-watercolour.md)), neutral to slightly grim, **never a cartoon**. The
finish is `wash.js`, one for every subject.

- **Mood before readability.** A lone scavenger's world: judge a picture by that.
- **Colour:** clean, slightly grey paint, neither dusty nor faded. Metals are cool greys; suit, copper and wood are warm and worn. Full
  saturation is only for what works or warns: fire, lamps, screens, hazard stripes.
- **Light:** hard steps of tone, one shade and one deep, no gradients or painted glow. The hero is lit from straight above, so
  turning changes nothing; the world from the side; the game's light does the rest.
- **Line:** a thin pen that wanders, swells and lifts off. Heavier round an object, lighter inside, never a sticker's even border.
  Paint may miss the line a little, never carelessly.
- **Water:** a trace of tide lines, pooling and grain. It is the finish's, one strength for all: never tune it per picture. It scales
  paint's tone and never mixes in paper, which reads as dust on a dark colour.
- **Parting from the wall:** back walls stay well darker than what stands before them; the finish gives every outlined shape a thin lit
  top edge.
- **Wear:** rust grows from seams and rivets where water sat, torn-edged and darker at heart; no long drips or rings. Cracks are
  short pen lines; dents and patches sit where something hit or was mended.
- **Detail:** objects are composed, each part placed by hand: about 20 calls for a 16 × 16 icon. Walls are quieter than what stands
  before them, back walls quietest. Random scatter is for the edge of a placed thing, never for placing it.
- **Ground:** one body of large close-toned masses, a hard shadow at their foot, few cracks; grass lies thick and deep muted green over
  the block's top.
- **Do not:** lay a filter or an outline over finished art (a raw tool's ink aside), or draw a wall as noise.

## What it draws

| Subject | Pictures |
| --- | --- |
| `hero` | `hero_<part>`: suits' parts (the regen's lights `_lit`, `_arcs`, `_pip`), limbs by `limbs.js`; not the outfits, painted raw (outfits-painted); hair, faces, beards by `LookTint`; cape, jetpack, lamp, sheet, tools; `hero_tracker`, `_light`: white, tinted by Modulate. Top-lit |
| `catchergear` | catchers' outfits: `hero_catcher<Body>_<part>`, `_arm_lit`; `hero_catcher<Helmet>_shell`, `_visor`, `_lit`, by `HeroKit` |
| `machines` | `machine_<kind>`, working overlays (fire, fills, bubbles); a `fitting` on its port (`Machines.Ports`); exposes `AnimeKit` |
| `zfittings` | `pipe_run`, `pipe_<arms>`, valves, sockets, hatches, platforms, ladder, floor, drone, lamps, held tools; after `machines`, for `AnimeKit` |
| `items` | `items/<id>`; an `_empty` state is the full picture's calls, only the changing part dark; `tool_screwdriver` |
| `room` | `wall_`, `wall-edge_` and `back_` of scrap (rusted by `wreck`'s `AnimeSheetRust`), cast scrap (clean) and iron, `door_*` |
| `wreck` | `wreck_dress`, `wreck_tears` (a mask, no finish), from `Wreck.cs`; its rust grows from `wall_scrap`'s seams |
| `piles`, `smelter` | `machine_pile*`: the hold's heaps; `machine_smelter`, `smelter_work_*` |
| `shipwalls`, `grounds`, `doors` | `wall_`, `back_`, `wall-edge_`, `detail_`, `top_` of glass, mica, hide, alloy, bedrock, lava, plaster; `door_*_0` and the links `room` leaves |
| `caves` | stone, ice, copper veins and soil (`wall_`, `back_`, `wall-edge_`, `top_`, `detail_`), `caves/moss*_*`, `caves/crystalIce*` |
| `creatures` | `creatures/<id>/<part>`, numbers in `CreatureInk.cs`: traced rigs |
| `tick`, `warden`, `cavehostiles`, `lurkerattacks` | `creatures/ink<Kind>/*`, `<Rig>Ink.cs`: guards, lurkers, a capback's gas |
| `tufts` | `planet/tuft_*`, `tuftReed_*`, `tuftFungus_*`: one of three plants a tuft (`FloraLooks.Drawn`) |
| `flora` | `planet/acacia_*`, `_glow`: pods where the logical glow picture has them, as the game lights there |
| `shrubs` | `planet/bush_*`, `rock_*`, `bell*_*`: dead thorn, boulders, hooded bells; light only in `_glow` |
| `leaves` | `planet/leaf_<look>_<plant>_<n>[_back]`: falling leaves |
| `heavens` | `planet/sky`, `clouds` (far layers, any size), `giant`, `sun` |
| `cavelife` | `caves/` crystals, dripstone, fungi, threads, roots, cracks, geodes, fossils, opal and spire caches, lamp; dim `_glow` |
| `homes`, `homeskeep`, `homeslights`, `homeswall`, `homesruin` | `homes/*`, `_dark`, `_fed`, `_glow`; `ruins/spireLens_*`; `homes.js` exposes `HomeKit`, the others sort after it |
| `debris` | `debris/*`: rocks lit from a side, crates, plates, pods, scrap, derelicts; unlettered, they turn |
| `aboard`, `shuttle` | `machine_salvage*`: salvage on the deck, numbered engines and consoles, one per vessel; `machine_crewShuttle` |
| `net` | `machine_net` (booms), `net_mount`, `net_front_<n>` (holding n); `NetInk.cs` |
| `bay`, `salvage` | `machine_droneBay`, `droneBay_*`, `salvage_*`, `items/salvageDrone`; `BayInk.cs`, `SalvageInk.cs` |
| `scanner` | `hero_scanner`, `hero_<helmet>_scanner`, `_lit_<n>`, `items/scanner`; `ScannerInk.cs` |
| `helmets`, `quilted`, `heatSuit` | traced: `hero_<helmet>_shell`, `_visor` (`_collar`; bell `_seal`; `hero_visor`, card `hero_helmet`), `items/<id>`; `hero_heatSuit_<part>` |
| `charge`, `missile` | `hero_blastCharge` (`icons/` lit), `shot_blastCharge_spark`: its fuse in flight; `shot_missile` |
| `signs`, `credit`, `capsule`, `gauge`, `titlename` | `sign_*`: bolts, plug, wrench, tips; `credit`, the chip; `capsule`, `capsule_flame_<n>`; `gauge_*`, the vitals' light; `menu/title_name*`, the title's name (Oswald, `fonts/`) |
| `upgrades` | `upgrade_bag`, `_belt`, `_tradeSlots`, `_canister`: wares, no item |
| `zleftovers` | the arc bench's and lamp's glows, `machine_emergencyLamp`, `module_arc_coil`, the pod's lit state and porthole, `wrist_*`, `tablet_*` |

## Run

```
node tools/anime-art/export.js [subject...]   # every subject, or some
```

Needs Node 22+, Edge or Chrome (`ANIME_BROWSER` for another); `tools/snap.ps1` imports.

## Traced subjects

A picked AI-made reference is traced for its **form**; the line, paint and water stay ours.

- `refs/<name>.png` and `.palette.json`: its colours (`ref`) and our paint (`mat`); `single`: one part, `overlay`: patches,
  `mend`: repainted first, `holes`: left open; no `background`: cut by alpha. `pieces`: boxes or
  `{ keep, ... }` polygons with their own keys (`materials` added), each to `traced/<name>.<piece>.json`.
- `node tools/anime-art/trace.js <name>` (ffmpeg) cuts it by its ink into beziers, `traced/<name>.json`, drawn by `traced.js`, never
  the reference.
- `"raw": true`, held tools ([raw](../../docs/decisions/2026-11-14-raw-tool-pictures.md)): the trace cuts out the
  reference's own paint, `traced/<name>.png`; `Traced.raw` inks round it, no water. Code draws what moves or lights. `over`: a part, an edited ref's pixels unlike `refs/<over>.png` in `clip`.
- AI-made work ships: say so in Steam's AI disclosure.

## Add a subject

A file `subjects/<name>.js` setting `Subjects.<name>`, which calls `out('<picture>', canvas)` for each picture from
`Anime.bake({ w, h, draw })`, `w` and `h` in world pixels, drawn 4 times over. Line weights and the light are the
engine's (`engine.js`), per world pixel. `wash.js` replaces `bake`; a picture drawn straight on a canvas skips the finish.

- A sheet that repeats takes `tile: true`. A door's frame and panel are washed apart (`DoorKit`): shut and open differ only by the panel.
- A `top_*` picture has its ground line on row 16: rows above stand on the cell above, rows below lie over the block (`front`).
- A cave picture keeps the rows its logical one uses (`LOGICAL` in the page), or its plant floats. Moss is cut at its block's edges along a
  run, px 4 and 20 of 24: every variant's cross-section matches there, its torn end past them.
- Every held tool is raw (builders-raw); its `icons/` copy is 384 px long
  (driver-icons).

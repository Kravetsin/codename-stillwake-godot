# Planet art painters

The scripts that paint the first planet's pictures in `game/art`, kept here so the planet-variety work (palette swaps,
new planet types) can repaint them. They were written in session scratchpads and are copied as they were; only their
paths changed (inputs from `tools/`, output into `game/art` relative to the repo root by default). The homes, the plaster
and the arc lamp aboard are painted by [`tools/homes-art`](../homes-art), which was already here.

Plain Node, no packages: `fs`, `path`, `zlib`. Run from anywhere; verified with Node 24.15.

| Painter | Writes | Inputs | Command | Reproduces the committed PNGs |
| ------- | ------ | ------ | ------- | ----------------------------- |
| `surface/export.js` (with `kit.js`) | `planet/`: the flora — `acacia_0-2`, `bush_0-1`, `rock_0-1`, `bell{Teal,Violet,Amber}_0-1`, `tuft_0-2`, each with `_glow` but `tuft_2` — and the backdrop — `sky`, `sun`, `clouds`, `far`, `giant`, `hills`, `ridge` (38 files) | none | `node tools/planet-art/surface/export.js [artDir]` | **38 / 38 exact.** It also writes the first surface pass's `wall_`/`back_` soil, stone and bedrock and 12 `grass_*`, all since replaced by the ground painter: run `ground` after it and delete `game/art/planet/grass_*.png` |
| `ground/paint.js` | `planet/{wall,back,detail,top}_{soil,stone,bedrock}` (12), the D1 direction («Сколы») picked on the ground page; D2 and D3 and a preview into the sheets dir | none | `node tools/planet-art/ground/paint.js [sheetsDir]` | **12 / 12 exact** |
| `caves/export.js` (with `engine.js`, `objects.js`, the cave page's drawing code) | `caves/`: crystals, hanging crystals, glowcaps, hangcaps, moss and green moss by face, threads, roots, geodes, fossils, veins, heat cracks, stalactites and stalagmites (181 files); `caves-sizes.json` beside the script | none (the page's hero for its scenes is read from `game/art`, not used by the export) | `node tools/planet-art/caves/export.js [artDir]` | **181 / 181 exact** |
| `lava/lava.js` | `planet/{wall,back,top,detail}_lava` (4) | none | `node tools/planet-art/lava/lava.js [planetDir]` | **4 / 4 exact** |
| `ruins/make.js` | placeholders of the spires (ADR 0085): `planet/{wall,back,top,detail}_alloy`, `caves/{spireCache,relicShrine,spireLamp}_0` and their `_glow`, `items/arcCoil`, `items/alloyScrap` | none | `node tools/planet-art/ruins/make.js [artDir] [previewDir]` | **11 / 12 exact**; its `alloyScrap` is the placeholder the planet icons replaced: run `icons` after it |
| `icons/build.js` + `icons/pick.js` (with `engine2.js`, candidates in `items1-3.js`) | every candidate of the planet icons page into `icons/out/<id>/<id>_<key>.png`; `pick.js` copies the picked ones (`picks.json`) to `items/`: `copperOre`, `copper`, `ice`, `crystal`, `spore`, `magnetite`, `motor`, `alloyScrap`, `cloth`, `arcLamp`, `helmetLamp` | `game/art/items/iron.png` (one candidate recolours it), `game/art/machine_arcLamp*.png` (one composites them) | `node tools/planet-art/icons/build.js tools/planet-art/icons/out items1.js items2.js items3.js` then `node tools/planet-art/icons/pick.js` | **11 / 11 exact** (`arcLamp` B and D are the same picture) |
| `creatures/export.js` (with `loper-parts.js`, the loper page's painter) | `creatures/{classic,tapir,sixleg,fluff}/`: every part of the four looks the user picked as an index map (R ramp, G tone) and its `_glow` map (60 files), and the generated `game/Content/logic/CreatureArt.cs` (sizes, pivots, rig, pose numbers, eye, the three palettes' ramps) the game recolours them by at load (ADR 0102) | none | `node tools/planet-art/creatures/export.js [artDir] [contentFile]` | **60 / 60 exact**, the page's own exports byte for byte. `creatures/paint.js` is the page's exporter as it was: every design, colour previews and manifests into a folder given (`node tools/planet-art/creatures/paint.js <outDir>`) |
| [`../homes-art/export.js`](../homes-art) | `homes/` (82), `planet/{wall,back,top,detail}_plaster`, `machine_arcLamp{,_glow,_lit}`, `game/Content/logic/HomesArt.cs`, `game/audio/ruins/*-1.wav` | `game/art/items/arcLamp.png` | `node tools/homes-art/export.js` | **exact**, every file unchanged |

`surface/s9-stillwake.js` is the surface study the export cut its pictures from, kept as the reference for its palette
and light; `node tools/planet-art/surface/gen.js s9` renders it into `surface/out/`.

## Repainting everything

In this order, each later step overwriting what an earlier one wrote and was since replaced:

```sh
node tools/planet-art/surface/export.js
node tools/planet-art/ground/paint.js
rm game/art/planet/grass_*.png
node tools/planet-art/caves/export.js
node tools/planet-art/ruins/make.js
node tools/planet-art/lava/lava.js
node tools/planet-art/creatures/export.js
node tools/planet-art/icons/build.js tools/planet-art/icons/out items1.js items2.js items3.js
node tools/planet-art/icons/pick.js
node tools/homes-art/export.js
```

Run on 2026-10-21 against `main` at 85f408a, this leaves `git status` clean: every planet, cave, home and planet item
picture in `game/art` comes back byte for byte. What it writes beside itself (`ground/sheets/`, `icons/out/`,
`surface/out/`, `caves/caves-sizes.json`) is ignored by git.

## Not painted here

- `items/mica.png` and `items/arcModule.png`: drawn by one-off commands while their features were built; no script was
  kept. The planet icons page's candidates for them (`items2.js`, `items3.js`) were not picked.
- The relic (`relicShrine`, `spireLamp`, `arcCoil` redrawn, the shard): PR #150 ports the relic page's drawing code into
  `tools/homes-art/relic.js`; until it merges, `ruins/make.js` paints the committed placeholders. The page's sources are
  in [`tools/concepts/arc-artifact`](../concepts/arc-artifact).
- The arc technology's icon is drawn live by `game/render/TechCanvas.cs` from `game/Content/logic/TechLooks.cs`, ported by hand
  from the arc icon page ([`tools/concepts/arc-icon`](../concepts/arc-icon)); nothing generates it.
- The back walls' contrast (ADR 0088, picked on [Задние стены пещер](https://claude.ai/artifact/5VFpDpLrYrDRR393JxCSXj)):
  `GroundArt.Recede` (`game/render/GroundArt.cs`, numbers in `game/Content/logic/GroundLooks.cs`) pales, cools and darkens a
  natural back wall's picture at load; no picture is painted for it.
- The ship's art (debris, machines, the hero, drones, hatches, the tank, the wrist, the early item icons, the farm's)
  is painted by [`tools/ship-art`](../ship-art).

## The pages the pictures were picked on

Found by their titles among the published artifacts.

- Surface: [First Planet Styles](https://claude.ai/artifact/WvQw1CvcosGyXRdMZvoT46), panel 9
- Ground: [Грунт планеты](https://claude.ai/artifact/AkmY2jXaMQZDjC4QYvhBvX), D1
- Caves and lava: [Stillwake Cave Contents](https://claude.ai/artifact/FAciAoQgLwdA7L2t62n8iD)
- Icons: [Иконки планет](https://claude.ai/artifact/8pCiPXBcUbLuEKae6sW1Ev)
- Creatures: [Лопер: облик](https://claude.ai/artifact/Rt6NAdUCtPCGR64REJWkkL), «Классика», «Тапирчик», «Шестиног», «Пушок»

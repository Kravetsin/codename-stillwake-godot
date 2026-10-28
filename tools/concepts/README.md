# Approved concept pages

The sources of concept pages the user approved and later work ports into the game. They are design references, not
shipped code: each was built in a session scratchpad into one self-contained page and published as an artifact. Only
their sources are kept (html, js, json); what the builds inline (pictures rendered by the page's scripts, copies of
`game/art`, screenshots) and the built pages are left out, as the published artifacts hold those. Requires that pointed
into the scratchpads now point into the repo: the cave page's engine is
[`tools/planet-art/caves`](../planet-art/caves), its PNG codec [`tools/planet-art/png.js`](../planet-art/png.js).

The artifacts were found by the pages' titles among the published ones; no page source names its own link.

| Folder | Page | What it settles | Build |
| ------ | ---- | --------------- | ----- |
| [`planet-bg`](planet-bg) | [Фон первой планеты](https://claude.ai/artifact/NLPpUbYt74ZkFiJd1GNknt) | the planet's backdrop system: the surface's sky layers, what lies behind caves and halls, the spires seen far | `node build.js` joins `src/*.js` into `src/page.html` with the pictures of `assets/` (not kept: game art crops and screenshots) |
| [`ruins-decor`](ruins-decor) | [Жилища творцов дуги](https://claude.ai/artifact/QmKMqoqFFqazvfdwewzfnK) | the arc makers' homes, their pieces and scenes (ADR 0091) | `node scenes.js`, `node cards.js` render `out/`, `node build.js` inlines them into `site/ruins-homes.html`. `decor.js` is the drawing code [`tools/homes-art`](../homes-art) exports from |
| [`arc-artifact`](arc-artifact) | [Дуговая реликвия](https://claude.ai/artifact/WbHME68Hh5tUQz5m8Mqyvk) | the arc relic's designs; design 1, the sky shard, is ported in PR #150 (`tools/homes-art/relic.js`) | `node scenes.js` renders `out/`, `node build.js` inlines into `site/arc-relic.html`; uses `ruins-decor` and the caves' engine |
| [`movement`](movement) | [Движение героя Stillwake](https://claude.ai/artifact/F6x7njvfvVJLPZVxV7LKfz) | the hero's movement research: livelier, as responsive | `node build.js` from the folder, with the game's pictures copied into `art/` |
| [`zero-g`](zero-g) | [Руки в невесомости](https://claude.ai/artifact/PHHDuwuvs9hokvf548c7mw) | the hands in zero gravity | as `movement`, whose `engine.js` and `render.js` it shares |
| [`hero-redesign`](hero-redesign) | [Новый облик героя](https://claude.ai/artifact/SmAXrGwUdzqqTyzqys9FG3) | the hero's new look | `node tools/build.js` reads `game/art` and four screenshots (`shot-deck.png`, `shot-planet.png`, `cmp-real.png`, `cmp-mine.png`, not kept) |
| [`arc-icon`](arc-icon) | [Значок дуги](https://claude.ai/artifact/M3Ua1GzFic9MpMqz4ptZdz) | the arc technology's icon, ported by hand into `game/Content/logic/TechLooks.cs` and `game/render/TechCanvas.cs` | `node tools/build.js` fills `template.html` with `tools/icons.txt` and `tools/crops.txt` (not kept: game screenshots as base64) |
| [`hero-looks`](hero-looks) | [Герой без скафандра](https://claude.ai/artifact/2vevr2KwPjQGcaur2dvi7r) | the first trial of the hero without a suit, superseded by `hero-styles`; its build now draws today's `tools/hero-art` pictures | `node build.js <out.html> [artDir]` inlines `game/art`, `parts.js`, `compose.js`, `hd.js` (the trial head at 2×), `page.js` and `live.js` (hair and cape on chains) into `page.html`; `node check.js <out.png> <scale> <looks> [dark]` and `node check-live.js <out.png> [style]` draw without a browser |
| [`hero-styles`](hero-styles) | [Стили облика героя](https://claude.ai/artifact/28GcsHnv8brH9np9k5hnCB) | the hero without a suit: five styles, then style E with one head for everyone at the body's pixel (1× against 2×), its hairs, faces and colours, painted by [`tools/hero-art`](../hero-art) | `node render.js out` draws `out/` from `styles/*.js` (F is `parts.js` itself) with `hero-looks/compose.js`, `node build.js out/hero-styles.html` inlines them into `template.html`; `gen.js` rasterises the 2× trial from outlines |

The screenshot scripts (`shoot.cjs`, `dbg.cjs`) load Playwright from the old TypeScript repo's `node_modules`; point them
at any Playwright to use them.

## Left out

| Page | Skipped |
| ---- | ------- |
| planet-bg | `assets/` and the shots (3 webp, 8 jpg, 69 png, about 12 MB: game art, crops and screenshots), the built `planet-bg.html` and `check.html` (0.8 MB each), the shot jobs `j1-j7.json` and `cdp.js`, the one-off edits `patch-*.js` beside `src/` (applied to `src/` already), `import.log`, a Chrome profile |
| ruins-decor | the built `site/ruins-homes.html` (0.96 MB), 121 PNGs (12.7 MB: the renders in `out/` and shots) |
| arc-artifact | the built `site/arc-relic.html` and `site/test.html` (3.4 MB each), 97 PNGs (12.3 MB: the renders in `out/` and shots) |
| movement | the built `index.html`, `local.html`, `probe.html` (0.11 MB each), `art/` (copies of game art), 50 shots (2.3 MB), `shot.sh`, `strip.sh` |
| zero-g | the built `index.html`, `local.html`, `probe.html` (0.13 MB each), `dom.txt` (a DOM dump), `art/`, 66 shots (3.4 MB), a Chrome profile, `shot.sh`, `mont.sh` |
| hero-redesign | the built `hero-redesign.html`, `local.html`, `probe.html` (0.27 MB each) and `sheet.html`, `game-copy/` (a copy of `game/art`), 693 PNGs (109 MB: shots, frames, the screenshots its build inlines), 4 WAVs (12 MB), `real/*.log`, `tools/*.sh` |
| arc-icon | the built `arc-icon.html`, `tools/crops.txt` (64 KB of base64 screenshots), the check pages in `tools/`, `tools/shot.sh`, 12 PNGs |
| hero-looks | the built page (46 KB) |
| hero-styles | the built page (0.2 MB), the renders in `out/` |

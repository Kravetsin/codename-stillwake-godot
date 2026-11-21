# Pick kit

The shared part of an art-pick page: the page on which the user picks a new look (CLAUDE.md, «Art and sound picks»). A page is one
page file of its candidates, toggles and checks; `build.js` turns it into one self-contained `index.html` and runs its stub check.

## Files

| File | Purpose |
| --- | --- |
| `kit.js` | the page's shared script: helpers, the drone «Пепел в латах», the rig hero and its run-and-jump loop, the scene, the bag, the ghost and real pointers, the open schedule, the tiles, toggles, frame loop and pick |
| `check.js` | the stub check: a stub DOM and a canvas that throws on a non-finite number, a bad colour or a source rectangle outside its image, and reads back solid white; the plan of clicks and forced turns |
| `build.js` | `node tools/pick-kit/build.js <page.js> <out-dir> [--break <name>] [--fonts <dir>]`: writes `<out-dir>/index.html`, then checks it |
| `pages/drone-holo.js` | the worked example: the drone's hologram in the planner sheet's place |
| `pages/title-tether.js` | a whole-scene page (`scene`): the title screen's scene, a limp hero on a tether |
| `pages/drone-vision.js` | a page without the bag (`window: false`): the drone's meeting as a vision before the giant core |
| `pages/title-logo.js` | a page with its own font (`fonts`): the name CASTOFF on the title, turning to dust and gathering again in a loop |

## Rules

- **One file.** The page is one HTML file with one inline script and no external URL: the artifact's frame blocks other hosts.
  The bag's item pictures are embedded from `game/art/hd/items/<id>.png` (the ids in `BAG_ITEMS`), so they are the game's own.
- **The pick** is a db doc (`pick.doc`), `{ pick, <every saved toggle's key>, at }`, read back on load within 10 s; without a db
  the page says so and «Скопировать выбор» gives the text. Page text is Russian.
- **The game's scale.** A cell is 8 world px, drawn ×4 (`Z`); the hero is `Rig.cs`'s, with `Reach`, `PlanHand`, `SheetTurn` and
  the `HeadTurn` clamp (`HEAD`); he faces the pointer as `Pose.FaceOf` does. The window opens at once, on the schedule.
- **Nothing is invented for a page that does not exist yet.** A hook goes in when a page needs it.
- The page is written into the scratchpad and published by the main session; nothing here touches `game/` or `sim/`.

## A page file

A CommonJS module. Its data goes into the page as `PAGE`; its `page` function is pasted whole into the kit's closure, so it calls
the kit's names (`rr`, `css`, `halo`, `SIGN`, `drawDrone`, `plateIdle`, `newHero`, `poseOf`, `toS`, `turnHead`,
`scheduledOn`, `lensAt`, `OPT`, …) directly; it may not use anything from Node.

| Field | What |
| --- | --- |
| `title`, `heading` | the tab's title, the `h1` |
| `window` | `false` for a page without the bag: no open schedule, ghost, Tab key or bag pictures, no open-mode or hero buttons; its step places the heroes, who do not run |
| `lead` | HTML: `<p class="lead">` paragraphs, then `<p class="miss">` saying what the page does not show |
| `toggles` | `{ id, key, opts: [[label, value], …], label?, bar?, saved?, short? }`: buttons setting `OPT[key]`, its first value the default; `bar: 1` sits beside the kit's own (open mode, hero, speed, pause), others on a second bar; a `saved` one goes into the pick under `key`, named `short` in its text |
| `pick` | `{ doc, subject, what: [accusative, instrumental] }`: the db doc, the copy text's subject, the candidate's word in the hints |
| `tile` | `[w, h]`: the tile's logical size, `[1000, 540]` when left out; «Крупно 1:1» shows it `w` wide |
| `fonts` | `{ family: file }`: OFL TTFs inlined as `@font-face` data, the names in `PAGE.fonts` to wait for; files from `--fonts <dir>`, by default `fonts/` beside the out dir, each with its `OFL.txt`; never committed |
| `kitBars` | the kit's own toggles shown (`openMode`, `heroMode`, `speedMode`, `playMode`); all when left out |
| `page()` | returns `{ cands, expose?, …hooks }` |
| `breaks` | `{ name: [[from, to], …] }`: text swaps that break one rule, to see the check fail (`--break name`) |
| `check(K)` | plans the run, runs it, prints its lines, returns whether its own rules held |

A candidate is `{ id, name, idea, time, … }`: `idea` and `time` are shown above its tile; the rest is the page's (`T.cand`).

## Hooks

Each is optional; the kit calls it with the tile `T` (`T.c` the canvas, `T.S` device px per page px, `T.time`, `T.win.on`,
`T.me`, `T.heroes`).

| Hook | When |
| --- | --- |
| `init(T)` | a tile is made: add its own state and heroes (`T.heroes`, drawn in order) |
| `onScale(T)` | the tile's device scale changed: remake its buffers |
| `step(T, dt)` | after the hero moved and the window opened or closed, before poses: set `h.e` and `h.hl` (how far each hero holds the planner sheet, at which look), the page's own clocks; a pose of its own on the rig's joints through `h.kn` (how far to the kneel, `KNEEL`), `h.lean` (the body forward about the hip, rad) and `h.rk` / `h.rp` (how far the front hand goes to a point, world px from the feet) |
| `back(T)` | in place of the kit's starry floor: the page's own world, under the heroes |
| `aim(T, h)` | a screen point the hero's drone turns its lens to, or null for its lamp's way |
| `ownDrone(T, h)` | true while the page draws this hero's drone and lamp itself; the kit's spring keeps following `h.d.p` |
| `overHero(T, h)` | drawn over a hero, under its drone |
| `platesAway(T, h)` | true when the drone's plates are away and the page draws them |
| `overDrone(T, h)` | drawn over a hero's drone |
| `screen(T)` | over the whole finished picture, under the HUD: a screen effect (it may read `T.cv`) |
| `hud(T)` | the line of text beside the Tab key |
| `scene(T, dt)` | each frame, in place of the kit's world (floor, hero loop, drone, window, Tab key): the page draws its whole tile |

## Build and check

```
node tools/pick-kit/build.js tools/pick-kit/pages/drone-holo.js <scratchpad>/drone-holo
node tools/pick-kit/build.js tools/pick-kit/pages/drone-holo.js <scratchpad>/x --break still
```

The second must print `CHECK FAILED`: a check is proven by breaking its rule once. `check(K)` gets the tiles, `OPT`, the page's
`expose` (`K.X`) and:

- `K.at(frame, fn)`, `K.seg(barId, i)` (the i-th button), `K.click(tile, selector)`, `K.ptrAll([x, y] | null)` (the real pointer
  on every tile), `K.forceSeq(frame)` (forced open and close turns at every moment of both);
- `K.run(frames, (tile, frame) => …)` at 60 fps; `K.fail(text)` fails the check; `K.clicks` holds when the ghost took a stack;
  `K.picked()` is the db doc the current pick writes (without its time), or null before a pick.

The kit adds the errors thrown, the canvas calls, the ghost's clicks and the pick's status line; `CHECK OK` needs no error, no
`fail`, no external URL, and the page's `true`.

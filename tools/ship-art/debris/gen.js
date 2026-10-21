// node tools/ship-art/debris/gen.js [finalDir, default game/art/debris] — draws every style's pictures into out/<style>/ with the game's file names
// (art/debris/<look>_NN.png), and an "in context" scene per style and for the current art into out/scenes/.
const fs = require('fs'), path = require('path');
const { ALL } = require('./styles.js');
const { ROCKS, drawRock } = require('./rocks.js');
const { WRECKS, drawWreck } = require('./wrecks.js');
const { read, write } = require('./png.js');
const { hash } = require('./lib.js');

const OUT = path.join(__dirname, 'out');
const GAME = path.join(path.join(__dirname, '..', '..', '..', 'game', 'art'), 'debris');
const LOOK = { asteroid: 'asteroid', asteroidSmall: 'smallrock', asteroidLarge: 'bigrock', asteroidRich: 'orerock', engineBlock: 'engine', cargoHold: 'hold', podCluster: 'pods' };
// the concept page shows the first three of each; every style draws them all
const COUNT = 3;
// what was picked (2026-10-18): asteroids, hold and pods in A, the engine block in B
const PICKED = { asteroid: 'A', asteroidSmall: 'A', asteroidLarge: 'A', asteroidRich: 'A', engineBlock: 'B', cargoHold: 'A', podCluster: 'A' };

const manifest = {};
for (const st of ALL) {
  const dir = path.join(OUT, st.id); fs.mkdirSync(dir, { recursive: true });
  for (const f of fs.readdirSync(dir)) fs.unlinkSync(path.join(dir, f));
  for (const kind of Object.keys(LOOK)) for (let i = 0; i < (ROCKS[kind] || WRECKS[kind]).list.length; i++) {
    const p = ROCKS[kind] ? drawRock(st, kind, i) : drawWreck(st, kind, i);
    const name = `${LOOK[kind]}_${String(i + 1).padStart(2, '0')}.png`;
    fs.writeFileSync(path.join(dir, name), p.png());
    (manifest[st.id] ??= []).push(name);
  }
}

// ---- the scenes: a slice of starfield, one of each kind and a few of the game's finished pieces for scale ----
const SW = 420, SH = 150;
function starfield() {
  const d = new Uint8Array(SW * SH * 4);
  for (let i = 0; i < SW * SH; i++) d.set([9, 12, 18, 255], i * 4);
  for (let i = 0; i < 260; i++) {
    const x = Math.floor(hash(i, 1, 9) * SW), y = Math.floor(hash(i, 2, 9) * SH), b = hash(i, 3, 9);
    const c = b > 0.97 ? [230, 236, 255] : b > 0.85 ? [150, 160, 185] : b > 0.5 ? [80, 88, 108] : [48, 54, 70];
    d.set([...c, 255], (y * SW + x) * 4);
    if (b > 0.985) for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) { const X = x + dx, Y = y + dy; if (X >= 0 && Y >= 0 && X < SW && Y < SH) d.set([90, 100, 130, 255], (Y * SW + X) * 4); }
  }
  return d;
}
function blit(d, im, ox, oy) {
  for (let y = 0; y < im.h; y++) for (let x = 0; x < im.w; x++) {
    const s = (y * im.w + x) * 4, a = im.data[s + 3] / 255; if (!a) continue; const X = ox + x, Y = oy + y; if (X < 0 || Y < 0 || X >= SW || Y >= SH) continue;
    const o = (Y * SW + X) * 4; for (let c = 0; c < 3; c++) d[o + c] = Math.round(im.data[s + c] * a + d[o + c] * (1 - a));
  }
}
const PLACE = [
  ['hold', 2, 8, 8], ['engine', 1, 14, 92], ['bigrock', 1, 118, 10], ['asteroid', 2, 124, 84], ['smallrock', 3, 170, 118],
  ['orerock', 1, 172, 58], ['pods', 1, 226, 8], ['asteroid', 3, 300, 52], ['smallrock', 1, 104, 64],
];
const OLD = [['section_01', 300, 104], ['hull_04', 304, 14], ['crate_01', 342, 30], ['scrap_03', 386, 74], ['pod_02', 360, 120], ['hull_09', 214, 112], ['crate_05', 392, 16]];
const sceneDir = path.join(OUT, 'scenes'); fs.mkdirSync(sceneDir, { recursive: true });
for (const id of ['current', ...ALL.map(s => s.id)]) {
  const d = starfield();
  for (const [name, x, y] of OLD) blit(d, read(path.join(GAME, name + '.png')), x, y);
  for (const [look, n, x, y] of PLACE) {
    const f = `${look}_${String(n).padStart(2, '0')}.png`;
    const src = id === 'current' ? path.join(GAME, fs.existsSync(path.join(GAME, f)) ? f : `${look}_01.png`) : path.join(OUT, id, f);
    blit(d, read(src), x, y);
  }
  write(path.join(sceneDir, `${id}.png`), { w: SW, h: SH, data: d });
}
// the chosen set, named as art/debris wants it
const FINAL = process.argv[2] || GAME; fs.mkdirSync(FINAL, { recursive: true });
const counts = {};
for (const [kind, id] of Object.entries(PICKED)) {
  const n = (ROCKS[kind] || WRECKS[kind]).list.length; counts[kind] = n;
  for (let i = 1; i <= n; i++) { const f = `${LOOK[kind]}_${String(i).padStart(2, '0')}.png`; fs.copyFileSync(path.join(OUT, id, f), path.join(FINAL, f)); }
}
console.log('final', counts);
fs.writeFileSync(path.join(OUT, 'manifest.json'), JSON.stringify({ count: COUNT, looks: LOOK, picked: PICKED, final: counts, styles: manifest }, null, 1));
console.log('drawn', Object.values(manifest).flat().length, 'pictures');

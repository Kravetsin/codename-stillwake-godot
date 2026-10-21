// Placeholder art for the farm (0060): the grow capsule, its plant's four stages, its lamp lit, and four item icons.
const { encode, scale } = require('./png.js');
const out = process.argv[2];
const hex = h => [parseInt(h.slice(0, 2), 16), parseInt(h.slice(2, 4), 16), parseInt(h.slice(4, 6), 16), 255];

function canvas(w, h) { return { w, h, data: Buffer.alloc(w * h * 4) }; }
function set(img, x, y, c) { if (x < 0 || y < 0 || x >= img.w || y >= img.h) return; img.data.set(c, (y * img.w + x) * 4); }
function get(img, x, y) { const i = (y * img.w + x) * 4; return [...img.data.slice(i, i + 4)]; }
function rect(img, x0, y0, x1, y1, c) { for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) set(img, x, y, c); }
function grid(rows, pal) {
  const img = canvas(rows[0].length, rows.length);
  rows.forEach((r, y) => [...r].forEach((ch, x) => { if (ch !== '.') set(img, x, y, hex(pal[ch])); }));
  return img;
}
const same = (a, b) => a.every((v, i) => v === b[i]);

// Items, 16x16, outlined as the others (14171e).
const icons = {
  seed: grid([
    '................',
    '........Gg......',
    '........g.......',
    '.......ooo......',
    '......oLLMo.....',
    '.....oLLMMMo....',
    '.....oLMMMMo....',
    '....oLMMMMMDo...',
    '....oMMMMMMDo...',
    '....oMMMMMDDo...',
    '.....oMMMDDo....',
    '.....oMMDDDo....',
    '......oDDDo.....',
    '.......ooo......',
    '................',
    '................',
  ], { o: '14171e', L: 'efcf98', M: 'bd8449', D: '8f5b2d', g: '3f8f4f', G: '6fbf73' }),
  soil: grid([
    '................',
    '................',
    '................',
    '................',
    '................',
    '......oooo......',
    '....ooLLMMoo....',
    '...oLLMMfMMMo...',
    '..oLMMDMMMDMMo..',
    '..oMMMMMMMMfMo..',
    '.oMMDMMfMMDMMDo.',
    '.oMMMMMDMMMMMMo.',
    '.oDMMMMMMMMDMDo.',
    '.oDDDDDDDDDDDDo.',
    '..oooooooooooo..',
    '................',
  ], { o: '14171e', L: '8a5a3a', M: '6a4430', D: '4a2e1e', f: 'a8a092' }),
  biomass: grid([
    "................",
    "................",
    ".....oo..oo.....",
    "....oGGooGGo....",
    "...oGGgooGggo...",
    "..ooGggGGgggoo..",
    ".oGGoggGgggoGGo.",
    ".oGggogggggoggo.",
    "..oggGgDggGggo..",
    "..ogggDDgDgggo..",
    "...oggDggDggo...",
    "...oDgggggDDo...",
    "....oDDDDDDo....",
    ".....oooooo.....",
    "................",
    "................",
  ], { o: "14171e", G: "6fbf73", g: "3f8f4f", D: "2c6a3a" }),
  biofuel: grid([
    '................',
    '................',
    '......oooo......',
    '......oSSo......',
    '.....oooooo.....',
    '....oWWWWWWo....',
    '....oWLLLLWo....',
    '....oFFFFFFo....',
    '....oFHFFFFo....',
    '....oFHFFFDo....',
    '....oFFFFFDo....',
    '....oFFFFDDo....',
    '....oFFFFDDo....',
    '....oDDDDDDo....',
    '.....oooooo.....',
    '................',
  ], { o: '14171e', S: '6b7385', W: 'c6e9f2', L: '8fc4d2', F: 'b5c23a', H: 'e6ee8a', D: '7f8f26' }),
};

// The grow capsule, 32x48 (2x3 cells): a glass dome on a metal base, a soil bed inside, a lamp under its cap.
const C = {
  o: hex('1a1e25'), m1: hex('8f99ad'), m2: hex('6b7385'), m3: hex('5b6170'), m4: hex('474c57'), mx: hex('9aa0ab'),
  g0: hex('22303c'), g1: hex('2a3d4f'), g2: hex('3a536a'), g3: hex('5f86a8'), g4: hex('9fd8ff'),
  s1: hex('6a4430'), s2: hex('8a5a3a'), s3: hex('4a2e1e'),
  lamp: hex('5b3a52'), lampEdge: hex('3a2a36'), pink: hex('ff7ad1'), pinkCore: hex('ffd0ef'),
  led: hex('1c4626'), screw: hex('d8d2c4'),
};
const W = 32, H = 48;
const body = canvas(W, H);
// The dome's edge, rounded at its shoulders: [y, x0, x1] of the glass between its outline.
const dome = [];
for (let y = 7; y <= 34; y++) {
  const inset = y === 7 ? 3 : y === 8 ? 1 : 0;
  dome.push([y, 5 + inset, 26 - inset]);
}
for (const [y, x0, x1] of dome) {
  set(body, x0 - 1, y, C.o);
  set(body, x1 + 1, y, C.o);
  rect(body, x0, y, x1, y, C.g1);
}
// Darker deep in the glass, toward the back.
for (const [y, x0, x1] of dome) for (let x = x0 + 12; x <= x1 - 2; x++) set(body, x, y, C.g0);
// The left highlight and a few diagonal glints.
for (let y = 10; y <= 29; y++) set(body, 6, y, C.g4);
for (let y = 10; y <= 29; y++) set(body, 7, y, C.g3);
const glints = [[20, 11], [23, 16], [19, 21], [22, 26]];
for (const [x, y] of glints) for (let i = 0; i < 4; i++) set(body, x + i, y - i, C.g3);
for (const [x, y] of glints) set(body, x - 1, y + 1, C.g2);
// The cap over the dome, and the lamp under it.
rect(body, 9, 2, 22, 2, C.o);
set(body, 8, 3, C.o); set(body, 23, 3, C.o); rect(body, 9, 3, 22, 3, C.m1);
set(body, 7, 4, C.o); set(body, 24, 4, C.o); rect(body, 8, 4, 23, 4, C.m2); set(body, 9, 4, C.m1);
set(body, 5, 5, C.o); set(body, 6, 5, C.o); set(body, 25, 5, C.o); set(body, 26, 5, C.o); rect(body, 7, 5, 24, 5, C.m3);
set(body, 11, 3, C.screw); set(body, 20, 3, C.screw);
rect(body, 4, 6, 27, 6, C.o);
rect(body, 9, 7, 22, 7, C.lampEdge);
rect(body, 10, 7, 21, 7, C.lamp);
rect(body, 9, 8, 22, 8, C.o);
// The soil bed at the dome's foot.
rect(body, 5, 31, 26, 31, C.s2);
rect(body, 5, 32, 26, 33, C.s1);
rect(body, 5, 34, 26, 34, C.s3);
for (const x of [8, 13, 19, 24]) set(body, x, 32, C.s2);
for (const x of [10, 17, 22]) set(body, x, 33, C.s3);
// The base: a rim over a plated box with a small panel, on two feet.
rect(body, 2, 35, 29, 35, C.o);
rect(body, 2, 36, 29, 45, C.o);
rect(body, 3, 36, 28, 36, C.m1);
rect(body, 3, 37, 28, 44, C.m2);
rect(body, 3, 44, 28, 44, C.m3);
rect(body, 3, 37, 3, 43, C.mx);
for (const x of [5, 26]) set(body, x, 38, C.screw);
// A slatted vent and a panel with its light.
for (const y of [39, 41, 43]) rect(body, 7, y, 14, y, C.m4);
rect(body, 18, 38, 25, 42, C.o);
rect(body, 19, 39, 24, 41, C.m4);
set(body, 20, 40, C.led); set(body, 21, 40, C.led);
rect(body, 22, 40, 23, 40, C.m3);
rect(body, 3, 46, 7, 47, C.o); rect(body, 24, 46, 28, 47, C.o);
rect(body, 4, 46, 6, 46, C.m3); rect(body, 25, 46, 27, 46, C.m3);

// The lamp lit: only its lens, pink, its middle near white.
const lamp = canvas(W, H);
rect(lamp, 10, 7, 21, 7, C.pink);
rect(lamp, 13, 7, 18, 7, C.pinkCore);
set(lamp, 9, 7, C.pink); set(lamp, 22, 7, C.pink);

// The plant's four stages, from a sprout to a bush filling the dome; glass glints stay over it.
const P = { st: hex('2c6a3a'), lf: hex('3f8f4f'), hi: hex('6fbf73'), bud: hex('c9d14a'), budHi: hex('f2e98a') };
function leaf(img, x, y, dir, len) {
  // A leaf from (x, y) out to one side, rising a little: dark edge under, light on top.
  for (let i = 1; i <= len; i++) {
    const lx = x + dir * i, ly = y - Math.floor(i / 2);
    set(img, lx, ly, i === len ? P.lf : P.hi);
    set(img, lx, ly + 1, P.lf);
  }
}
function plant(stage) {
  const img = canvas(W, H);
  const top = [28, 22, 15, 10][stage];
  for (let y = top; y <= 30; y++) { set(img, 15, y, P.st); set(img, 16, y, P.lf); }
  const leaves = [
    [[27, 2]],
    [[27, 3], [24, 4], [21, 2]],
    [[28, 4], [25, 6], [22, 5], [19, 4], [16, 3], [14, 2]],
    [[28, 5], [26, 7], [23, 8], [20, 7], [17, 6], [14, 5], [11, 3], [9, 2]],
  ][stage];
  leaves.forEach(([y, len], i) => {
    leaf(img, 15, y, -1, len);
    leaf(img, 16, y - (i % 2), 1, len);
  });
  if (stage === 3) {
    for (const [x, y] of [[11, 20], [21, 17], [13, 13], [19, 24], [17, 10]]) {
      rect(img, x, y, x + 1, y + 1, P.bud);
      set(img, x, y, P.budHi);
    }
  }
  if (stage === 0) { set(img, 14, 27, P.hi); set(img, 17, 27, P.hi); }
  // Glass in front of the plant: wherever the dome shows a glint, the glint stays.
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const b = get(body, x, y);
    if (get(img, x, y)[3] && (same(b, C.g3) || same(b, C.g4))) set(img, x, y, b);
  }
  return img;
}

const fs = require('fs');
fs.mkdirSync(out + '/items', { recursive: true });
for (const [name, img] of Object.entries(icons)) encode(`${out}/items/${name}.png`, img);
encode(`${out}/machine_growCapsule.png`, body);
encode(`${out}/growCapsule_work_0.png`, lamp);
for (let s = 0; s < 4; s++) encode(`${out}/growCapsule_plant_${s}.png`, plant(s));

// Previews: each item at x12; the capsule at rest, lit, and at its four stages, at x8, on the ship's dark.
const bg = [37, 41, 49];
for (const name of Object.keys(icons)) encode(`${out}/../preview_${name}.png`, scale(icons[name], 12, bg));
function over(a, b) {
  const img = canvas(a.w, a.h); a.data.copy(img.data);
  for (let y = 0; y < a.h; y++) for (let x = 0; x < a.w; x++) if (get(b, x, y)[3]) set(img, x, y, get(b, x, y));
  return img;
}
const strip = canvas(W * 6 + 10, H);
const frames = [body, over(body, lamp), ...[0, 1, 2, 3].map(s => over(over(body, plant(s)), lamp))];
frames.forEach((f, i) => { for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) set(strip, i * (W + 2) + x, y, get(f, x, y)); });
encode(`${out}/../preview_capsule.png`, scale(strip, 6, bg));
console.log('drawn');

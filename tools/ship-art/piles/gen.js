// The hold's piles (the-scoop): pieces of the debris art heaped into a machine's footprint, a dark mound behind them, flat on the floor.
// And its emergency lamp: a red glass on a bracket from the wall on its right.
const path = require('path');
const { read, write } = require('../part-icons/png.js');
const ART = path.join(__dirname, '../../../game/art');
const OUT = process.argv[2] || ART;
const CELL = 8;

const piece = (name, i) => read(path.join(ART, `debris/${name}_${String(i).padStart(2, '0')}.png`));
const PILES = [
  { kind: 'pileHull', w: 4, h: 4, seed: 11, pieces: [['hull', 3], ['hull', 5], ['hull', 8], ['hull', 12], ['scrap', 2], ['hull', 1], ['hull', 7], ['scrap', 11]] },
  { kind: 'pileScrap', w: 3, h: 3, seed: 23, pieces: [['scrap', 10], ['scrap', 7], ['scrap', 3], ['scrap', 9], ['scrap', 12], ['hull', 10]] },
  { kind: 'pileCargo', w: 3, h: 3, seed: 37, pieces: [['crate', 2], ['crate', 5], ['crate', 1], ['scrap', 6], ['hull', 3]] },
  { kind: 'pileBits', w: 2, h: 2, seed: 41, pieces: [['scrap', 9], ['crate', 5], ['hull', 7]] },
];

for (const pile of PILES) {
  let seed = pile.seed;
  const rnd = () => { seed = (seed + 0x6d2b79f5) >>> 0; let t = seed; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
  const W = pile.w * CELL, H = pile.h * CELL, px = new Uint8Array(W * H * 4);
  const top = (x) => { const q = (x + 0.5 - W / 2) / (W / 2); return Math.round(H - (H - 3) * Math.pow(1 - q * q, 0.55) * (0.88 + 0.12 * rnd())); };

  const tops = Array.from({ length: W }, (_, x) => top(x));
  const set = (x, y, r, g, b) => { const i = (y * W + x) * 4; px[i] = r; px[i + 1] = g; px[i + 2] = b; px[i + 3] = 255; };
  for (let x = 0; x < W; x++) for (let y = tops[x]; y < H; y++) set(x, y, 26, 27, 32);

  // From the top of the mound down, so what lies low lies in front; each turned a little and cut to the mound's outline.
  pile.pieces.forEach(([name, n], i) => {
    const img = piece(name, n), row = i / Math.max(1, pile.pieces.length - 1);
    const cx = W * (0.18 + 0.64 * rnd()), cy = H * (0.3 + 0.62 * row), turn = (rnd() - 0.5) * 1.6, flip = rnd() < 0.5;
    const cos = Math.cos(turn), sin = Math.sin(turn);
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
      if (y < tops[x]) continue;
      const dx = x + 0.5 - cx, dy = y + 0.5 - cy;
      let u = Math.floor(cos * dx + sin * dy + img.w / 2), v = Math.floor(-sin * dx + cos * dy + img.h / 2);
      if (flip) u = img.w - 1 - u;
      if (u < 0 || v < 0 || u >= img.w || v >= img.h) continue;
      const at = (v * img.w + u) * 4;
      if (img.data[at + 3] > 128) set(x, y, img.data[at], img.data[at + 1], img.data[at + 2]);
    }
  });
  write(path.join(OUT, `machine_${pile.kind}.png`), { w: W, h: H, data: px });
}

const LAMP = [
  '................',
  '................',
  '................',
  '................',
  '.....######...dd',
  '.....#RRRR#...dd',
  '....#RRoRRR#..dd',
  '....#RooRRR#mmmd',
  '....#RRRRRR#mmmd',
  '.....#RRRR#...dd',
  '.....######...dd',
  '..............dd',
  '................',
  '................',
  '................',
  '................',
];
const COLOURS = { '#': [24, 22, 26], R: [226, 44, 34], o: [255, 176, 150], m: [88, 96, 110], d: [44, 48, 58] };
const glass = new Uint8Array(16 * 16 * 4);
LAMP.forEach((row, y) => [...row].forEach((c, x) => { if (COLOURS[c]) { glass.set([...COLOURS[c], 255], (y * 16 + x) * 4); } }));
write(path.join(OUT, 'machine_emergencyLamp.png'), { w: 16, h: 16, data: glass });

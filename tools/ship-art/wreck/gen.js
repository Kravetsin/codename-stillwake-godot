// The starting wreck's dressing, as the user picked it: speckled rust, ghost chevrons, ice-rimmed shards (2026-10-31-the-scoop).
// Writes art/wreck_dress.png (rust and stripes, drawn over the wall) and art/wreck_tears.png (a torn cell's mask: nothing keeps
// the wall, magenta cuts it, any other colour paints it), a cell of the drawing each. Usage: node tools/ship-art/wreck/gen.js [artDir]
const fs = require('fs'), path = require('path');
const { write } = require('../part-icons/png.js');
const ROOT = path.join(__dirname, '../../..');
const OUT = process.argv[2] || path.join(ROOT, 'game/art');

const mapOf = (source, name, next) => {
  const text = source.slice(source.indexOf(`${name} = Placed(`), source.indexOf(next));
  return text.split('\n').map(l => /^\s*"(.*)",\s*$/.exec(l)).filter(Boolean).map(m => m[1]);
};
const source = fs.readFileSync(path.join(ROOT, 'sim/Content/Wreck.cs'), 'utf8');
const ROWS = mapOf(source, 'Rows', 'Materials = Placed(');
const DRESS = mapOf(source, 'Dress', 'The pipe inside the pod');
const COLS = 74;

const charAt = (x, y) => (ROWS[y] === undefined || ROWS[y][x] === undefined ? ' ' : ROWS[y][x]);
const isWall = (x, y) => charAt(x, y) === '#';
const isTorn = (x, y) => (DRESS[y] || '')[x] === 'T';

/** Which sides of a torn cell are open: to space, to a room, or none (a wall). */
function sidesOf(x, y) {
  const one = (nx, ny) => (isWall(nx, ny) ? null : charAt(nx, ny) === ' ' ? 'space' : 'interior');
  return { up: one(x, y - 1), right: one(x + 1, y), down: one(x, y + 1), left: one(x - 1, y) };
}

function hash(a, b, s) {
  let t = Math.imul(a | 0, 73856093) ^ Math.imul(b | 0, 19349663) ^ Math.imul((s | 0) + 7, 83492791);
  t = Math.imul(t ^ (t >>> 15), t | 1);
  t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
  return (t ^ (t >>> 14)) >>> 0;
}
const roll = (a, b, s, n) => (hash(a, b, s) >>> 7) % n;
const chance = (a, b, s, percent) => roll(a, b, s, 100) < percent;

function eachPixel(cx, cy, fn) {
  for (let py = 0; py < 8; py++) for (let px = 0; px < 8; px++) fn(px, py, cx * 8 + px, cy * 8 + py);
}

// Old, dull rust: browns, no orange that could pass for heat. The wall's plate lines stay faintly visible through it.
const RUST_LIGHT = [150, 98, 70], RUST_MID = [126, 80, 58], RUST_DARK = [96, 62, 50], RUST_BLACK = [70, 50, 46], RUST_ALPHA = 222;

function rust(cx, cy, heavy, put) {
  eachPixel(cx, cy, (px, py, wx, wy) => {
    // Patches from coarse blocks, staggered so no grid shows; loose speckle round them.
    const coarse = roll((wx + (wy >> 3) * 5) >> 3, wy >> 3, 11, 100);
    const fine = roll(wx >> 1, wy >> 1, 12, 100);
    const level = (coarse * 3 + fine) >> 2;
    const solid = heavy ? 54 : 74, loose = heavy ? 36 : 56;
    const on = level >= solid ? chance(wx, wy, 13, 92) : level >= loose ? chance(wx, wy, 14, 26) : chance(wx, wy, 15, 3);
    if (!on) return;
    const tone = roll(wx, wy, 16, 10);
    put(px, py, level >= solid + 18 ? (tone < 5 ? RUST_DARK : RUST_BLACK) : tone < 2 ? RUST_LIGHT : tone < 7 ? RUST_MID : RUST_DARK, RUST_ALPHA);
  });
}

const OCHRE = [176, 150, 84], BONE = [196, 180, 128];
// Where a chevron's point lies, in pixels down the drawing: the upper jaw's tip and the lower jaw's; the landing's rows, above it, slant one way, as picked.
const tipMiddle = cy => (cy <= 13 ? 104 : 200);

function stripes(cx, cy, put) {
  eachPixel(cx, cy, (px, py, wx, wy) => {
    // Point outward: left on the jaws, right on the landing. Broad, 6 px a band, most of the paint gone.
    const slant = Math.abs(wy - tipMiddle(cy));
    const v = cx > 40 ? wx + slant : wx - slant + 256;
    if (Math.floor(v / 6) & 1) return;
    const blotch = roll(wx >> 2, wy >> 2, 61, 100);
    if (blotch < 34 ? chance(wx, wy, 62, 80) : chance(wx, wy, 63, 14)) return;
    put(px, py, chance(wx, wy, 64, 25) ? OCHRE : BONE, 255);
  });
}

const ICE = [150, 190, 214], ICE_DIM = [112, 146, 170], SHADOW = [20, 22, 30], CUT = [255, 0, 255];
const SIDES = ['up', 'right', 'down', 'left'], OPPOSITE = { up: 'down', down: 'up', left: 'right', right: 'left' };

// How deep a side may cut. Facing sides share 5 px, so at least 3 px of metal stay across the cell.
function capOf(sides, side) {
  const state = sides[side], other = sides[OPPOSITE[side]];
  if (!other) return state === 'space' ? 6 : 3;
  if (state === 'space' && other === 'interior') return 4;
  if (state === 'interior' && other === 'space') return 1;
  return side === 'up' || side === 'left' ? 3 : 2;
}

function tear(cx, cy, sides, put) {
  const cut = new Array(64).fill(null);
  for (const side of SIDES) {
    if (!sides[side]) continue;
    const cap = capOf(sides, side), vertical = side === 'up' || side === 'down';
    const line = (vertical ? cy : cx) * 2 + (side === 'down' || side === 'right' ? 1 : 0);
    for (let py = 0; py < 8; py++) for (let px = 0; px < 8; px++) {
      const along = vertical ? cx * 8 + px : cy * 8 + py;
      const d = side === 'up' ? py : side === 'down' ? 7 - py : side === 'left' ? px : 7 - px;
      // Saw teeth of uneven pitch: each tooth climbs to a spike of metal, then drops.
      const pitch = 3 + roll(along >> 3, line, 91, 2);
      const tooth = Math.floor(along / pitch), phase = along - tooth * pitch;
      const tall = 1 + roll(tooth, line, 92, cap);
      const depth = Math.min(tall, Math.floor((tall * (phase + 1)) / pitch) + (chance(along, line, 93, 18) ? 1 : 0));
      if (d < Math.min(cap, depth)) cut[py * 8 + px] = cut[py * 8 + px] === 'space' ? 'space' : sides[side];
    }
  }
  const result = cut.map((c, i) => {
    if (c) return c;
    const px = i % 8, py = i >> 3;
    const rim = (py > 0 ? cut[i - 8] : sides.up) || (py < 7 ? cut[i + 8] : sides.down) || (px > 0 ? cut[i - 1] : sides.left) || (px < 7 ? cut[i + 1] : sides.right);
    return rim ? 'rim' : 'keep';
  });
  // Nothing floats: a pixel that does not reach a closed side of its cell goes.
  const seen = new Array(64).fill(false), queue = [];
  result.forEach((v, i) => {
    const px = i % 8, py = i >> 3;
    if (v !== 'space' && v !== 'interior' && ((py === 0 && !sides.up) || (py === 7 && !sides.down) || (px === 0 && !sides.left) || (px === 7 && !sides.right))) { seen[i] = true; queue.push(i); }
  });
  while (queue.length) {
    const i = queue.pop(), px = i % 8, py = i >> 3;
    for (const j of [py > 0 ? i - 8 : -1, py < 7 ? i + 8 : -1, px > 0 ? i - 1 : -1, px < 7 ? i + 1 : -1])
      if (j >= 0 && !seen[j] && result[j] !== 'space' && result[j] !== 'interior') { seen[j] = true; queue.push(j); }
  }
  result.forEach((v, i) => {
    const px = i % 8, py = i >> 3;
    // A cut into a room is a shadow: the cell draws no back wall, and stars at a room's edge would read as a hole.
    if (v === 'space' || (!seen[i] && v !== 'interior')) put(px, py, CUT, 255);
    else if (v === 'interior') put(px, py, SHADOW, 255);
    else if (v === 'rim') put(px, py, chance(cx * 8 + px, cy * 8 + py, 94, 22) ? ICE_DIM : ICE, 255);
  });
}

const W = COLS * 8, H = ROWS.length * 8;
const dress = new Uint8Array(W * H * 4), tears = new Uint8Array(W * H * 4);
// The picks were judged as they look lit on the hull, whose scrap art is this much brighter than the frame they were painted on: the mask's cut stays as it is.
const LIT = 95 / 62;
const lit = c => (c === CUT ? c : c.map(v => Math.min(255, Math.round(v * LIT))));
const putter = (data, cx, cy) => (px, py, c, a) => data.set([...lit(c), a], ((cy * 8 + py) * W + cx * 8 + px) * 4);
DRESS.forEach((row, y) => [...row].forEach((c, x) => {
  if (c === 'R' || c === 'r') rust(x, y, c === 'R', putter(dress, x, y));
  else if (c === 'H') stripes(x, y, putter(dress, x, y));
  else if (c === 'T') tear(x, y, sidesOf(x, y), putter(tears, x, y));
}));
write(path.join(OUT, 'wreck_dress.png'), { w: W, h: H, data: dress });
write(path.join(OUT, 'wreck_tears.png'), { w: W, h: H, data: tears });

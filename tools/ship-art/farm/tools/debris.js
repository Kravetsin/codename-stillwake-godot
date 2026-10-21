// A concept for a new debris kind: a clod of earth torn from some ship's farm, drifting, a plant still in it.
// 32x32, three of them. The silhouette is hand-drawn as a polygon; it is lit as a lumpy body from the upper
// left, clods placed by hand, then roots, the plant and the farm's scrap laid on; outline #14171e as all debris.
const { Pic } = require('./lib.js');
const { ITEM_OUTLINE } = require('./pal.js');

const EARTH = ['#22150d', '#3a2418', '#553624', '#734b31', '#946644', '#b5875c'];
const G = { 1: '#1c4626', 2: '#2c6a3a', 3: '#3f8f4f', 4: '#6fbf73', 5: '#a3dc8c' };
const ROOT = ['#7a6448', '#a88c64', '#cdb488'];
const PEB = ['#423f3a', '#837f76', '#aaa69c'];
const METAL = { k: '#2c3138', s: '#59616d', t: '#7a8390', u: '#a8b1bd', v: '#d5dce4' };
const TEAL = { W: '#1d4f57', X: '#2a7f88', Y: '#4fb3b5', Z: '#a6e6e0' };
const POD = { G: '#4f7a1c', H: '#86ad2c', I: '#bcd84c', J: '#e6f39a' };

const hash = (x, y, s) => { let h = (x * 374761393 + y * 668265263 + s * 2246822519) >>> 0; h = ((h ^ (h >>> 13)) * 1274126177) >>> 0; return ((h ^ (h >>> 16)) >>> 0) / 4294967296; };

function inside(pts, x, y) {
  let c = false; const px = x + 0.5, py = y + 0.5;
  for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
    const [xi, yi] = pts[i], [xj, yj] = pts[j];
    if ((yi > py) !== (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi) c = !c;
  }
  return c;
}

// the body: a slot per pixel from where it lies on the clod and how near its lit or shadowed edge it is
function clod(p, pts, seed, clods) {
  const cells = []; for (let y = 0; y < p.h; y++) for (let x = 0; x < p.w; x++) if (inside(pts, x, y)) cells.push([x, y]);
  const set = new Set(cells.map(([x, y]) => y * p.w + x)), isIn = (x, y) => set.has(y * p.w + x);
  let cx = 0, cy = 0; for (const [x, y] of cells) { cx += x; cy += y; } cx /= cells.length; cy /= cells.length;
  const span = Math.sqrt(cells.length / Math.PI);
  for (const [x, y] of cells) {
    let s = 2.3 + 1.6 * ((cx - x) * 0.6 + (cy - y) * 0.8) / span;
    const lit = !isIn(x - 1, y) || !isIn(x, y - 1), lit2 = !isIn(x - 2, y) || !isIn(x, y - 2);
    const dark = !isIn(x + 1, y) || !isIn(x, y + 1), dark2 = !isIn(x + 2, y + 1) || !isIn(x + 1, y + 2);
    if (lit) s += 1.1; else if (lit2) s += 0.4;
    if (dark) s -= 1.1; else if (dark2) s -= 0.5;
    s += (hash(x, y, seed) - 0.5) * 1.5 + (hash(x >> 1, y >> 1, seed + 7) - 0.5) * 0.8;
    p.px(x, y, EARTH[Math.max(0, Math.min(5, Math.round(s)))]);
  }
  // clods: a lit top-left, a dark lower-right lip
  for (const [x, y, w] of clods || []) {
    for (let i = 0; i < w; i++) { if (isIn(x + i, y)) p.px(x + i, y, EARTH[4]); if (isIn(x + i + 1, y + 1)) p.px(x + i + 1, y + 1, EARTH[1]); }
    if (isIn(x, y + 1)) p.px(x, y + 1, EARTH[3]);
  }
  return { isIn };
}
// a crack: dark, its lower lip lit
function crack(p, pts) { for (let i = 1; i < pts.length; i++) p.line(...pts[i - 1], ...pts[i], EARTH[0]); for (const [x, y] of pts) if (p.get(x, y + 1) && p.get(x, y + 1) !== EARTH[0]) p.px(x, y + 1, EARTH[3]); }
// crumbs drifting off it
function crumb(p, x, y, big) { p.px(x, y, EARTH[4]); if (big) { p.px(x + 1, y, EARTH[3]); p.px(x, y + 1, EARTH[2]); p.px(x + 1, y + 1, EARTH[1]); } }
// moss on the top, where it grew
function moss(p, x0, x1, y) { for (let x = x0; x <= x1; x++) if (p.get(x, y)) p.px(x, y, hash(x, y, 5) < 0.5 ? G[3] : G[2]); }
function pebble(p, x, y) { p.px(x, y, PEB[2]); p.px(x + 1, y, PEB[1]); p.px(x, y + 1, PEB[1]); p.px(x + 1, y + 1, PEB[0]); }
// a root: pale thread with a dark side, trailing off
function root(p, pts) { for (let i = 1; i < pts.length; i++) p.line(...pts[i - 1], ...pts[i], ROOT[1]); p.px(...pts[pts.length - 1], ROOT[0]); p.px(...pts[0], ROOT[2]); }
const rows = (p, x, y, r, map) => p.stamp(x, y, r, map);

// 1: a round clod, a sprout on top, roots trailing out of its torn underside
function chunk1() {
  const p = new Pic(32, 32);
  clod(p, [[5, 13], [9, 11], [12, 12], [16, 10], [20, 11], [24, 10], [27, 13], [28, 17], [26, 19], [27, 22], [23, 25], [20, 24], [17, 27], [13, 25], [10, 26], [7, 23], [4, 20], [5, 17], [3, 15]], 11, [[9, 12, 3], [15, 11, 2], [20, 12, 3], [8, 18, 2], [13, 16, 3], [19, 17, 2], [23, 15, 2], [12, 21, 2]]);
  pebble(p, 17, 20); pebble(p, 8, 16);
  crack(p, [[10, 18], [13, 20], [15, 19]]); crack(p, [[21, 15], [23, 18]]); moss(p, 13, 19, 11);
  crumb(p, 25, 27, true); crumb(p, 6, 27); crumb(p, 28, 8, true);
  root(p, [[12, 25], [11, 27], [12, 29]]); root(p, [[17, 26], [18, 28], [20, 29]]); root(p, [[22, 24], [24, 26]]); root(p, [[7, 22], [5, 24]]);
  // the sprout
  rows(p, 13, 2, [
    '..45..45',
    '.4433443',
    '..2.3.2.',
    '....3...',
    '....2...',
    '...32...',
    '....2...',
  ], G);
  return p;
}

// 2: still in the broken corner of its tray, a bigger plant leaning out
function chunk2() {
  const p = new Pic(32, 32);
  clod(p, [[5, 14], [8, 12], [12, 12], [16, 10], [21, 11], [25, 12], [27, 15], [28, 19], [26, 21], [27, 24], [22, 25], [16, 25], [8, 24], [5, 21]], 23, [[8, 14, 2], [13, 13, 3], [19, 12, 2], [22, 16, 3], [10, 19, 2], [16, 18, 2]]);
  pebble(p, 20, 20);
  crack(p, [[17, 15], [20, 19], [19, 22]]); moss(p, 12, 18, 11); crumb(p, 28, 27, true); crumb(p, 25, 29);
  // the tray's corner: a teal rim along the bottom and up the left, snapped off
  p.hl(4, 18, 25, TEAL.Y); p.hl(4, 19, 26, TEAL.X); p.hl(5, 18, 27, TEAL.W);
  p.vl(3, 16, 27, TEAL.Y); p.vl(4, 17, 24, TEAL.X); p.px(3, 15, TEAL.Z); p.px(4, 25, TEAL.Z);
  p.px(19, 25, TEAL.X); p.px(20, 26, TEAL.W); p.px(18, 27, TEAL.W);
  root(p, [[21, 25], [23, 27], [24, 29]]); root(p, [[25, 21], [28, 23]]);
  // the plant: two stems leaning right, leaves, a pod
  p.line(15, 11, 18, 4, G[3]); p.line(16, 11, 19, 5, G[2]);
  p.line(17, 7, 22, 4, G[3]);
  rows(p, 18, 1, ['.445', '44332', '.32..'], G);
  rows(p, 21, 2, ['..45', '.4433', '322..'], G);
  rows(p, 12, 5, ['54..', '4433', '.322'], G);
  rows(p, 13, 8, ['..44', '.433', '2.2.'], G);
  rows(p, 22, 5, ['G.', 'JH', 'IH', '.G'], POD);
  return p;
}

// 3: a long clod, its plant turned sideways as it drifts, a scrap of the tray's steel in it
function chunk3() {
  const p = new Pic(32, 32);
  clod(p, [[3, 16], [5, 13], [9, 13], [11, 11], [16, 13], [20, 11], [24, 12], [28, 14], [29, 18], [26, 20], [27, 22], [22, 22], [19, 24], [14, 22], [10, 24], [6, 22], [4, 20]], 37, [[7, 15, 2], [12, 14, 3], [21, 13, 2], [17, 16, 2], [9, 19, 3], [24, 17, 2]]);
  pebble(p, 14, 18);
  crack(p, [[8, 16], [11, 18]]); crack(p, [[19, 15], [21, 17], [24, 16]]); moss(p, 11, 16, 12); crumb(p, 29, 8, true); crumb(p, 25, 27);
  // a bent strip of steel stuck in its right end
  p.line(24, 20, 29, 23, METAL.u); p.line(24, 21, 29, 24, METAL.s); p.px(29, 23, METAL.v); p.px(25, 20, METAL.v);
  root(p, [[8, 22], [6, 25], [7, 27]]); root(p, [[15, 23], [15, 26]]); root(p, [[4, 19], [2, 21]]);
  // the plant, sideways: its stem runs up-left out of the clod's top
  p.line(14, 12, 10, 6, G[3]); p.line(15, 12, 11, 6, G[2]);
  rows(p, 6, 3, ['..45.', '.4443', '3432.', '.2...'], G);
  rows(p, 11, 3, ['45.', '443', '.32'], G);
  rows(p, 13, 6, ['.45', '443', '32.'], G);
  rows(p, 7, 8, ['.44', '443', '2..'], G);
  return p;
}

const CHUNKS = [chunk1, chunk2, chunk3];
function soilChunk(i) {
  const p = CHUNKS[i]();
  p.each((x, y) => { if (x === 0 || y === 0 || x === 31 || y === 31) throw new Error(`soil chunk ${i + 1}: fill on the border at ${x},${y}`); });
  return p.outline(ITEM_OUTLINE);
}
module.exports = { soilChunk };

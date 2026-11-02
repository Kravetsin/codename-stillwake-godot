// The starting wreck's dress, as picked in 2026-10-31-the-scoop, over today's scrap wall: speckled rust grown from its seams, ghost chevrons, ice-rimmed shards.
// wreck_dress goes over the wall; wreck_tears is a torn cell's mask (nothing keeps the wall, pure magenta cuts it, any other colour paints it), so it takes no finish.
(function () {
'use strict';
const { bake, canvas } = Anime;
const CELL = 8, ART = 4, Q = 4, SHEET_W = 128, SHEET_H = 64, GW = SHEET_W * Q, GH = SHEET_H * Q, BIG = 1 << 14;
// Which cell of the wall's sheet lies on the drawing's first in a new world: Wreck.Left + Layout.OffsetInWorld, modulo the sheet.
const SHEET_AT = [2, 4];
const COLS = 74, SEED = 21, TEAR_SEED = 31, RIM_BREAK = .34, PITCH = [2.2, 3.8];
const INK = '#12131a', SHADOW = [31, 34, 46], CUT = [255, 0, 255];
// The wall's own rust, a tone lighter and a pit darker; the chevrons' old paint; the ice on a torn edge.
const RUST = ['#94664a', '#8a4f2e', '#5c3220', '#2c1c16'], STRIPE = ['#c0b07c', '#a28a52'], ICE = '#b6d6e6', ICE_DIM = '#86a9be';

const mod = (a, n) => ((a % n) + n) % n;
const sm = (t) => t * t * (3 - 2 * t);
const hex = (c) => [parseInt(c.substr(1, 2), 16), parseInt(c.substr(3, 2), 16), parseInt(c.substr(5, 2), 16)];
const h2 = (x, y, s) => { let t = Math.imul(x, 374761393) + Math.imul(y, 668265263) + Math.imul(s, 2147483647 - 18); t = Math.imul(t ^ (t >>> 13), 1274126177); return ((t ^ (t >>> 16)) >>> 0) / 4294967296; };
function pn(x, y, s) {
  const X = Math.floor(x), Y = Math.floor(y), u = sm(x - X), v = sm(y - Y), a = h2(X, Y, s), b = h2(X + 1, Y, s), c = h2(X, Y + 1, s), d = h2(X + 1, Y + 1, s);
  return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
}
const lumps = (x, y, s) => (pn(x / 15, y / 15, s) * 4 + pn(x / 7.5, y / 7.5, s + 1) * 2 + pn(x / 3.7, y / 3.7, s + 2)) / 7;
const flecks = (x, y, s) => pn(x * 1.5, y * 1.5, s);

/** Painted tones from a function over a grid: a path of whole runs per tone, on the output's own pixels, so no seam shows inside one. */
function raster(a, tones, gw, gh, at) {
  const parts = tones.map(() => []), n2 = (v) => +v.toFixed(2);
  for (let y = 0; y < gh; y++) {
    let start = 0, cur = -1;
    for (let x = 0; x <= gw; x++) {
      const t = x < gw ? at(x, y) : -1;
      if (t === cur) continue;
      if (cur >= 0) parts[cur].push(`M${n2(start / Q)} ${n2(y / Q)}L${n2(x / Q)} ${n2(y / Q)}L${n2(x / Q)} ${n2((y + 1) / Q)}L${n2(start / Q)} ${n2((y + 1) / Q)}Z`);
      cur = t; start = x;
    }
  }
  tones.forEach((c, t) => { if (parts[t].length) a.flat(parts[t].join(''), c); });
}

const charOf = (rows, x, y) => (rows[y] === undefined || rows[y][x] === undefined ? ' ' : rows[y][x]);
const isWall = (x, y) => charOf(WRECK.rows, x, y) === '#';
const lookOf = (x, y) => charOf(WRECK.dress, x, y);
const WIDTH = COLS * CELL, HEIGHT = () => WRECK.rows.length * CELL;

/** Distance in logical pixels to the nearest seam of the scrap sheet, over its torus. */
let seamMemo;
function seams() {
  if (seamMemo) return seamMemo;
  const id = new Int16Array(GW * GH).fill(-1);
  let n = 0;
  for (const row of AnimeScrapRows()) for (const p of row) {
    for (const dx of [-SHEET_W, 0]) {
      const x0 = Math.max(0, (p.x + dx) * Q), x1 = Math.min(GW, (p.x + p.w + dx) * Q);
      if (x1 > x0) for (let y = p.y * Q; y < (p.y + p.h) * Q; y++) id.fill(n, y * GW + x0, y * GW + x1);
    }
    n++;
  }
  const d = new Float32Array(GW * GH).fill(1e6), at = (x, y) => mod(y, GH) * GW + mod(x, GW);
  for (let y = 0; y < GH; y++) for (let x = 0; x < GW; x++) if (id[at(x, y)] !== id[at(x + 1, y)] || id[at(x, y)] !== id[at(x, y + 1)]) d[at(x, y)] = 0;
  const ahead = [[-1, 0, 1], [0, -1, 1], [-1, -1, 1.414], [1, -1, 1.414]], back = [[1, 0, 1], [0, 1, 1], [1, 1, 1.414], [-1, 1, 1.414]];
  for (let pass = 0; pass < 3; pass++) {
    for (let y = 0; y < GH; y++) for (let x = 0; x < GW; x++) { const i = y * GW + x; for (const [a, b, c] of ahead) { const v = d[at(x + a, y + b)] + c; if (v < d[i]) d[i] = v; } }
    for (let y = GH - 1; y >= 0; y--) for (let x = GW - 1; x >= 0; x--) { const i = y * GW + x; for (const [a, b, c] of back) { const v = d[at(x + a, y + b)] + c; if (v < d[i]) d[i] = v; } }
  }
  for (let i = 0; i < d.length; i++) d[i] /= Q;
  return (seamMemo = d);
}
const seamAt = (X, Y) => seams()[Math.min(GH - 1, (mod(Y + SHEET_AT[1] * CELL, SHEET_H) * Q) | 0) * GW + Math.min(GW - 1, (mod(X + SHEET_AT[0] * CELL, SHEET_W) * Q) | 0)];

/** How heavy the map's rust is at each logical pixel, smoothed so no cell shows, and how far each is from a wall that takes none. */
let wearMemo;
function wear() {
  if (wearMemo) return wearMemo;
  const w = WIDTH, h = HEIGHT(), raw = new Float32Array(w * h), near = new Float32Array(w * h).fill(1e4);
  for (let Y = 0; Y < h; Y++) for (let X = 0; X < w; X++) {
    const cx = X >> 3, cy = Y >> 3, l = lookOf(cx, cy), i = Y * w + X;
    raw[i] = l === 'R' ? 1 : l === 'r' ? .55 : l === 'T' ? .7 : 0;
    if (isWall(cx, cy) && l !== 'R' && l !== 'r' && l !== 'T') near[i] = 0;
  }
  const blur = (a, rad) => {
    const b = new Float32Array(a.length), c = new Float32Array(a.length);
    for (let Y = 0; Y < h; Y++) { let s = 0; for (let X = -rad; X <= rad; X++) s += a[Y * w + Math.max(0, Math.min(w - 1, X))]; for (let X = 0; X < w; X++) { b[Y * w + X] = s / (2 * rad + 1); s += a[Y * w + Math.min(w - 1, X + rad + 1)] - a[Y * w + Math.max(0, X - rad)]; } }
    for (let X = 0; X < w; X++) { let s = 0; for (let Y = -rad; Y <= rad; Y++) s += b[Math.max(0, Math.min(h - 1, Y)) * w + X]; for (let Y = 0; Y < h; Y++) { c[Y * w + X] = s / (2 * rad + 1); s += b[Math.min(h - 1, Y + rad + 1) * w + X] - b[Math.max(0, Y - rad) * w + X]; } }
    return c;
  };
  const smooth = blur(blur(raw, 4), 4);
  for (let Y = 0; Y < h; Y++) { for (let X = 1; X < w; X++) near[Y * w + X] = Math.min(near[Y * w + X], near[Y * w + X - 1] + 1); for (let X = w - 2; X >= 0; X--) near[Y * w + X] = Math.min(near[Y * w + X], near[Y * w + X + 1] + 1); }
  for (let X = 0; X < w; X++) { for (let Y = 1; Y < h; Y++) near[Y * w + X] = Math.min(near[Y * w + X], near[(Y - 1) * w + X] + 1); for (let Y = h - 2; Y >= 0; Y--) near[Y * w + X] = Math.min(near[Y * w + X], near[(Y + 1) * w + X] + 1); }
  return (wearMemo = { smooth, near });
}

/** The rust field at a point: from the seams under it, the map's weight setting how far, the map's edge ending it raggedly. */
function rustField(X, Y) {
  const { smooth, near } = wear(), i = (Y | 0) * WIDTH + (X | 0), weight = smooth[i], seam = seamAt(X, Y);
  const reach = 3 + 2.6 * Math.max(0, Math.min(1, (weight - .5) * 2.2)) + 3.4 * (lumps(X, Y, SEED + 3) - .5);
  const bloom = (pn(X / 19, Y / 19, SEED + 11) * 2 + pn(X / 9, Y / 9, SEED + 12)) / 3;
  const f = 1 - seam / Math.max(1.5, reach * (.5 + bloom * 1.5)) + (lumps(X, Y, SEED) - .5) * 1.05 + (bloom - .5) * 2.4 - (1 - Math.min(1, near[i] / 6)) * 1.5;
  return { f, seam };
}
/** A point's rust tone by how far it lies inside its patch: specks past the edge, masses of two tones, a dark heart. */
function rustTone(X, Y) {
  const { f } = rustField(X, Y), fine = flecks(X, Y, SEED + 5), fine2 = flecks(X, Y, SEED + 6), mass = pn(X * .5, Y * .5, SEED + 7);
  if (f < 0) return f > -.55 && fine > .6 - f * (.34 / .55) ? (fine2 > .55 ? 2 : 1) : -1;
  if (f < .09) return fine > .4 ? 1 : fine2 > .55 ? 2 : -1;
  if (f > .8) return fine2 > .6 ? 3 : 2;
  if (f > .58) return mass > .42 ? 2 : 1;
  if (fine > .84) return 2;
  return mass > .58 ? 0 : mass < .4 ? 2 : 1;
}
/** Worn chevrons, the same as the pixel picture's: point outward, broad bands, most of the paint gone. */
function stripeTone(X, Y) {
  const cx = X >> 3, cy = Y >> 3, slant = Math.abs(Y - (cy <= 13 ? 104 : 200)), v = (cx > 40 ? X + slant : X - slant + 256) + (lumps(X, Y, 91) - .5) * 1.1;
  if (Math.floor(v / 6) & 1) return -1;
  if (lumps(X, Y, 92) * .55 + flecks(X, Y, 93) * .45 < .33) return -1;
  return lumps(X, Y, 95) > .56 ? 1 : 0;
}
/** Whether a point lies in the outer pixel of a side of its cell that faces no wall: paint does not run to an edge. */
function byOpenSide(X, Y) {
  const cx = X >> 3, cy = Y >> 3, u = X - cx * 8, v = Y - cy * 8, m = 1.25;
  return (u < m && !isWall(cx - 1, cy)) || (u > 8 - m && !isWall(cx + 1, cy)) || (v < m && !isWall(cx, cy - 1)) || (v > 8 - m && !isWall(cx, cy + 1));
}
const dress = () => ({
  w: WIDTH, h: HEIGHT(),
  draw(a) {
    const gw = WIDTH * Q, gh = HEIGHT() * Q;
    raster(a, RUST, gw, gh, (gx, gy) => {
      const l = lookOf(gx >> 5, gy >> 5);
      return (l === 'R' || l === 'r') && seamAt(gx / Q, gy / Q) >= .28 ? rustTone(gx / Q, gy / Q) : -1;
    });
    raster(a, STRIPE, gw, gh, (gx, gy) => (lookOf(gx >> 5, gy >> 5) === 'H' && !byOpenSide(gx / Q, gy / Q) ? stripeTone(gx / Q, gy / Q) : -1));
  },
});

const SIDES = ['up', 'right', 'down', 'left'], OPPOSITE = { up: 'down', down: 'up', left: 'right', right: 'left' };
function sidesOf(x, y) {
  const one = (nx, ny) => (isWall(nx, ny) ? null : charOf(WRECK.rows, nx, ny) === ' ' ? 'space' : 'interior');
  return { up: one(x, y - 1), right: one(x + 1, y), down: one(x, y + 1), left: one(x - 1, y) };
}
/** How deep a side may cut: facing sides share 5 px, so at least 3 px of metal stay across the cell. */
function capOf(sides, side) {
  const state = sides[side], other = sides[OPPOSITE[side]];
  if (!other) return state === 'space' ? 6 : 3;
  if (state === 'space' && other === 'interior') return 4;
  if (state === 'interior' && other === 'space') return 1;
  return side === 'up' || side === 'left' ? 3 : 2;
}
/** The depth of a torn line at a point along it, 0 to 1: each tooth climbs to its spike of metal, then drops. */
function shard(along, line) {
  const [lo, hi] = PITCH, bin = Math.floor(along / hi);
  let d = .12 + .1 * h2(bin, line, TEAR_SEED + 5);
  for (const b of [bin - 1, bin, bin + 1]) {
    const w = lo + h2(b, line, TEAR_SEED) * (hi - lo), start = hi * b + (hi - w) * h2(b, line, TEAR_SEED + 4), deep = .4 + h2(b, line, TEAR_SEED + 3) * .6, u = (along - start) / w;
    if (u >= 0 && u < 1) d = Math.max(d, u < .78 ? deep * (1 - u / .78 * .86) : deep * .14 + (u - .78) / .22 * (deep * .86));
  }
  return d;
}
/** A cell's cuts into the sheet's grid, 1 to space, 2 to a room, 3 kept. Nothing floats: what does not reach a closed side of its cell goes. */
function tearCell(cx, cy, grid, mapW) {
  const sides = sidesOf(cx, cy), S = CELL * ART, cut = new Int8Array(S * S);
  for (const side of SIDES) {
    if (!sides[side]) continue;
    const vertical = side === 'up' || side === 'down', cap = capOf(sides, side), line = (vertical ? cy : cx) * 2 + (side === 'down' || side === 'right' ? 1 : 0) + (vertical ? 0 : 1000);
    // What lies along this side: a torn cell open the same way meets it at the shallower depth, a whole wall at nearly none, space at no limit.
    const endCap = (nx, ny) => {
      if (!isWall(nx, ny)) return cap;
      if (lookOf(nx, ny) !== 'T') return .35;
      const o = sidesOf(nx, ny);
      return o[side] ? Math.min(cap, capOf(o, side)) : .6;
    };
    const capA = vertical ? endCap(cx - 1, cy) : endCap(cx, cy - 1), capB = vertical ? endCap(cx + 1, cy) : endCap(cx, cy + 1);
    for (let py = 0; py < S; py++) for (let px = 0; px < S; px++) {
      const u = vertical ? px : py, along = (vertical ? cx : cy) * CELL + (u + .5) / ART, local = (u + .5) / ART, ramp = 2.6;
      const d = (side === 'up' ? py : side === 'down' ? S - 1 - py : side === 'left' ? px : S - 1 - px) / ART;
      const capHere = local < ramp ? capA + (cap - capA) * sm(local / ramp) : local > CELL - ramp ? capB + (cap - capB) * sm((CELL - local) / ramp) : cap;
      if (d < Math.min(capHere, cap * .96 * shard(along, line))) { const i = py * S + px; cut[i] = cut[i] === 1 ? 1 : sides[side] === 'space' ? 1 : 2; }
    }
  }
  const seen = new Uint8Array(S * S), q = [];
  for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) {
    if (!cut[y * S + x] && ((y === 0 && !sides.up) || (y === S - 1 && !sides.down) || (x === 0 && !sides.left) || (x === S - 1 && !sides.right))) { seen[y * S + x] = 1; q.push(y * S + x); }
  }
  while (q.length) {
    const i = q.pop(), x = i % S, y = (i / S) | 0;
    for (const j of [y > 0 ? i - S : -1, y < S - 1 ? i + S : -1, x > 0 ? i - 1 : -1, x < S - 1 ? i + 1 : -1]) if (j >= 0 && !seen[j] && !cut[j]) { seen[j] = 1; q.push(j); }
  }
  for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) {
    const i = y * S + x;
    grid[(cy * S + y) * mapW + cx * S + x] = cut[i] === 2 ? 2 : cut[i] === 1 || !seen[i] ? 1 : 3;
  }
}
/** The torn cells' mask at 4 x: pure magenta and solid colours, no finish over it. */
function tears() {
  const S = CELL * ART, w = WIDTH * ART, h = HEIGHT() * ART, grid = new Int8Array(w * h), list = [];
  WRECK.dress.forEach((row, y) => { for (let x = 0; x < row.length; x++) if (row[x] === 'T') { tearCell(x, y, grid, w); list.push([x, y]); } });
  // How near every kept pixel of a torn cell lies to a cut, in 4 x pixels; a cell's open side counts as one.
  const near = new Int16Array(w * h).fill(99);
  let wave = [];
  for (const [cx, cy] of list) {
    const sd = sidesOf(cx, cy);
    for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) {
      const g = (cy * S + y) * w + cx * S + x;
      if (grid[g] === 1 || grid[g] === 2) { near[g] = 0; wave.push(g); }
      else if ((y === 0 && sd.up) || (y === S - 1 && sd.down) || (x === 0 && sd.left) || (x === S - 1 && sd.right)) near[g] = 1;
    }
  }
  for (let step = 1; step <= 12; step++) {
    const next = [];
    for (const g of wave) for (const j of [g - 1, g + 1, g - w, g + w]) if (j >= 0 && j < w * h && grid[j] === 3 && near[j] > step) { near[j] = step; next.push(j); }
    wave = next;
  }
  const C = canvas(w, h), X = C.getContext('2d'), img = X.createImageData(w, h), d = img.data;
  const put = (i, col) => { d[i * 4] = col[0]; d[i * 4 + 1] = col[1]; d[i * 4 + 2] = col[2]; d[i * 4 + 3] = 255; };
  const ice = hex(ICE), dim = hex(ICE_DIM), inked = hex(INK), tones = RUST.map(hex);
  for (const [cx, cy] of list) for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) {
    const GX = cx * S + x, GY = cy * S + y, g = GY * w + GX, LX = GX / ART, LY = GY / ART;
    if (grid[g] === 1) { put(g, CUT); continue; }
    if (grid[g] === 2) { put(g, SHADOW); continue; }
    const m = near[g], wide = pn(LX * .62, LY * .62, TEAR_SEED + 7), rim = wide < RIM_BREAK || flecks(LX, LY, TEAR_SEED + 9) > .74 ? 0 : 1 + Math.round((wide - RIM_BREAK) * 5);
    if (rim && m <= rim) { put(g, flecks(LX, LY, TEAR_SEED + 8) > .56 ? dim : ice); continue; }
    if (m <= rim + 2) { put(g, inked); continue; }
    const t = rustTone(LX, LY);
    if (t >= 0) put(g, tones[t].map((v) => Math.round(v * .94)));
  }
  X.putImageData(img, 0, 0);
  return C;
}

Subjects.wreck = function () {
  out('wreck_dress', bake(dress()));
  out('wreck_tears', tears());
};
})();

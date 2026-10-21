// Round 2 ground tiles, painted as pixel art: per direction and material
//   wall_<mat>.png          128x64 seamless base, a drop-in for game/art/planet/wall_<mat>.png (cut 8x4 by CutAt)
//   back_<mat>.png          128x64 the same, darker, for the back wall behind caves
//   detail_<mat>.png        128x64 overlays with alpha: 8 single cells (row 0), two 2x2 (x0,x32 y16), one 3x3 (x64 y16)
//   top_<mat>.png           128x64 the living edge, 16x32 tiles laid from the cell above (like grass_*):
//                           flat v0-3 (x0-63), slope ◢ v0-1 (x64-95), slope ◣ v0-1 (x96-127); back layer y0-31, front y32-63
// Light from the top-left. Ramps of 6, darkest first; no lone pixels of noise: everything is a cluster, a line or an object.
const P = require('../png');
const fs = require('fs'), path = require('path');
// node tools/planet-art/ground/paint.js [sheetsDir]: every direction into sheetsDir (default ground/sheets, ignored by git),
// then the picked one, D1, into game/art/planet as <kind>_<mat>.png.
const OUT = process.argv[2] || path.join(__dirname, 'sheets');
const PLANET = path.join(__dirname, '..', '..', '..', 'game', 'art', 'planet');

const hex = h => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)];
function hash(x, y, s) { let h = (Math.imul(x | 0, 0x85EBCA77) ^ Math.imul(y | 0, 0xC2B2AE3D) ^ Math.imul(s | 0, 0x27D4EB2F)) >>> 0; h ^= h >>> 15; h = Math.imul(h, 0x2C1B3C6D) >>> 0; h ^= h >>> 12; h = Math.imul(h, 0x297A2D39) >>> 0; h ^= h >>> 15; return (h >>> 0) / 4294967296; }
const clamp = (v, a, b) => v < a ? a : v > b ? b : v;
const R = (s) => { let k = 0; return () => hash(k++, 77, s); };

const PAL = {
  soil: ['#22170f', '#322219', '#453023', '#5a412f', '#72563f', '#8c6d52'].map(hex),
  stone: ['#1d2029', '#2c313d', '#3d4351', '#525a69', '#6b7383', '#8d95a3'].map(hex),
  bedrock: ['#121016', '#1b1820', '#25212b', '#312c39', '#413a4b', '#575066'].map(hex),
  pebble: ['#25221f', '#3e3a35', '#59534c', '#787068', '#9a9187', '#bdb4a7'].map(hex),
  tan: ['#2a231c', '#4a3e31', '#6a5a46', '#8a7760', '#a8957a', '#c7b597'].map(hex),
  root: ['#1a110a', '#2e2015', '#4a3524', '#6a4f38'].map(hex),
  moss: ['#16261b', '#24402b', '#33593b', '#4a7a4c', '#6c9e62', '#9cc584'].map(hex),
  grass: ['#1b2c1e', '#2a4630', '#3b6340', '#558650', '#7aa868', '#a8cc8a'].map(hex),
  violet: ['#1e1828', '#352a4a', '#55437a', '#7d68ad', '#ab97d8', '#e2d8ff'].map(hex),
  quartz: ['#2a2e38', '#5b6372', '#8c94a2', '#b5bcc8', '#d9dee6', '#f2f4f7'].map(hex),
  glow: ['#5fd6bf', '#c8fff0'].map(hex),
};

class Cv {
  constructor(w, h, wrap = false) { this.w = w; this.h = h; this.wrap = wrap; this.d = new Uint8ClampedArray(w * h * 4); }
  i(x, y) { x = Math.round(x); y = Math.round(y); if (this.wrap) { x = ((x % this.w) + this.w) % this.w; y = ((y % this.h) + this.h) % this.h; } else if (x < 0 || y < 0 || x >= this.w || y >= this.h) return -1; return (y * this.w + x) * 4; }
  set(x, y, c) { const i = this.i(x, y); if (i < 0) return; this.d[i] = c[0]; this.d[i + 1] = c[1]; this.d[i + 2] = c[2]; this.d[i + 3] = 255; }
  has(x, y) { const i = this.i(x, y); return i >= 0 && this.d[i + 3] > 0; }
  get(x, y) { const i = this.i(x, y); return i < 0 ? null : [this.d[i], this.d[i + 1], this.d[i + 2], this.d[i + 3]]; }
  blitTo(img, ox, oy) { for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) { const s = (y * this.w + x) * 4; if (!this.d[s + 3]) continue; const o = ((oy + y) * img.w + ox + x) * 4; img.data[o] = this.d[s]; img.data[o + 1] = this.d[s + 1]; img.data[o + 2] = this.d[s + 2]; img.data[o + 3] = this.d[s + 3]; } }
}

// ---------- the base field: chunks on a torus ----------
function seedsOn(W, H, cw, ch, seed, jit = 0.75) {
  const nx = Math.max(1, Math.round(W / cw)), ny = Math.max(1, Math.round(H / ch)), gw = W / nx, gh = H / ny, s = [];
  for (let j = 0; j < ny; j++) for (let i = 0; i < nx; i++) {
    const off = (j % 2) * 0.5 * (hash(j, 9, seed) < 0.6 ? 1 : 0);
    s.push({ x: (i + 0.5 + off + (hash(i, j, seed) - 0.5) * jit) * gw, y: (j + 0.5 + (hash(i, j, seed + 1) - 0.5) * jit) * gh, row: j, r: hash(i, j, seed + 2), r2: hash(i, j, seed + 3), r3: hash(i, j, seed + 4) });
  }
  return s;
}
function chunkMap(W, H, S, sx = 1, sy = 1) {
  const id = new Int16Array(W * H), DX = new Float32Array(W * H), DY = new Float32Array(W * H);
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    let best = 1e9, bi = 0, bdx = 0, bdy = 0;
    for (let k = 0; k < S.length; k++) { let dx = x + 0.5 - S[k].x, dy = y + 0.5 - S[k].y; dx -= W * Math.round(dx / W); dy -= H * Math.round(dy / H); const d = dx * dx * sx * sx + dy * dy * sy * sy; if (d < best) { best = d; bi = k; bdx = dx; bdy = dy; } }
    const i = y * W + x; id[i] = bi; DX[i] = bdx; DY[i] = bdy;
  }
  const at = (x, y) => id[(((y % H) + H) % H) * W + (((x % W) + W) % W)];
  return { id, DX, DY, at };
}
// periodic value noise sampled at a point, for patches of lighter or darker chunks
function pnoise(x, y, W, H, gx, gy, seed) {
  const fx = ((x / W) * gx % gx + gx) % gx, fy = ((y / H) * gy % gy + gy) % gy, ix = Math.floor(fx), iy = Math.floor(fy), tx = fx - ix, ty = fy - iy;
  const v = (i, j) => hash(((i % gx) + gx) % gx, ((j % gy) + gy) % gy, seed);
  const s = t => t * t * (3 - 2 * t), a = v(ix, iy), b = v(ix + 1, iy), c = v(ix, iy + 1), d = v(ix + 1, iy + 1);
  return a + (b - a) * s(tx) + (c - a) * s(ty) + (a - b - c + d) * s(tx) * s(ty);
}
// shade every pixel from its chunk: a crack on the shadow side, a rim on the lit side, the body flat, faceted or round
function shadeChunks(cv, M, S, ramp, o) {
  const { w: W, h: H } = cv;
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const i = y * W + x, k = M.id[i], s = S[k], t0 = s.t0;
    const on = (k2, h) => { const sh = h ? (o.shareH ?? o.share ?? 1) : (o.shareV ?? o.share ?? 1); return hash(Math.min(k, k2), Math.max(k, k2), h ? 17 : 19) < sh; };
    const sameH = (a, b) => { const k2 = M.at(a, b); return k2 === k || !on(k2, false); }, sameV = (a, b) => { const k2 = M.at(a, b); return k2 === k || !on(k2, true); };
    const same = (a, b) => (a !== x ? sameH(a, b) : sameV(a, b));
    let t;
    if (o.crack !== null && (!same(x + 1, y) || !same(x, y + 1))) t = o.crack;
    else if (o.crack === null && (!same(x + 1, y) || !same(x, y + 1))) t = t0 - (o.softEdge ? 0 : 1);
    else if (o.rim && (!same(x - 1, y) || !same(x, y - 1))) t = t0 + 1;
    else if (o.inner && (!same(x + 2, y) || !same(x, y + 2))) t = t0 - 1;
    else {
      const dx = M.DX[i], dy = M.DY[i];
      if (o.mode === 'facet') { const a = s.r2 * Math.PI * 2, nx = Math.cos(a), ny = Math.sin(a), side = (dx - 0.8) * nx + (dy - 0.8) * ny; const litSide = (-nx - ny) > 0; t = (side > 0) === litSide ? t0 : t0 - 1; }
      else if (o.mode === 'round') { const Rr = o.R || 4, r = Math.hypot(dx, dy) / Rr, l = -(dx + dy) / (Rr * 1.5); t = t0 + (l > 0.38 ? 1 : 0) - (r > 0.8 && l < -0.1 ? 1 : 0); }
      else t = t0;
    }
    cv.set(x, y, ramp[clamp(t, 0, ramp.length - 1)]);
  }
}
function toneSeeds(S, W, H, pick, macro) {
  for (const s of S) { let t = pick(s); if (macro) { const m = pnoise(s.x, s.y, W, H, macro.gx, macro.gy, macro.seed); t += m < 0.32 ? -1 : m > 0.7 ? 1 : 0; } s.t0 = clamp(t, macro && macro.min || 1, macro && macro.max || 4); }
}
const pickW = (arr) => (s) => arr[Math.floor(s.r * arr.length)];

// ---------- objects ----------
function ellipsePts(cx, cy, rx, ry, seed, rough = 0.12, angular = 0) {
  const pts = new Set(), ph = hash(seed, 1, 3) * 6.28, ph2 = hash(seed, 2, 3) * 6.28;
  const verts = angular ? Array.from({ length: angular }, (_, k) => 0.78 + hash(seed, k, 5) * 0.32) : null;
  for (let y = Math.floor(cy - ry - 2); y <= cy + ry + 2; y++) for (let x = Math.floor(cx - rx - 2); x <= cx + rx + 2; x++) {
    const dx = (x + 0.5 - cx) / rx, dy = (y + 0.5 - cy) / ry, a = Math.atan2(dy, dx);
    let lim = 1 + rough * Math.sin(3 * a + ph) + rough * 0.6 * Math.sin(5 * a + ph2);
    if (verts) { const n = angular, f = ((a + Math.PI) / (2 * Math.PI)) * n, k0 = Math.floor(f) % n, k1 = (k0 + 1) % n, t = f - Math.floor(f); lim = verts[k0] + (verts[k1] - verts[k0]) * t; lim /= Math.cos((t - 0.5) * (2 * Math.PI / n)) || 1; lim = Math.min(lim, 1.25); }
    if (dx * dx + dy * dy <= lim * lim) pts.add(x + ',' + y);
  }
  return pts;
}
// a rock or pebble: outline, rim light top-left, shadow bottom-right; facets for angular rock, a ball for round
function rock(cv, pts, ramp, o = {}) {
  const P2 = [...pts].map(k => k.split(',').map(Number)), inn = (x, y) => pts.has(x + ',' + y);
  let minx = 1e9, maxx = -1e9, miny = 1e9, maxy = -1e9; for (const [x, y] of P2) { minx = Math.min(minx, x); maxx = Math.max(maxx, x); miny = Math.min(miny, y); maxy = Math.max(maxy, y); }
  const cx = (minx + maxx + 1) / 2, cy = (miny + maxy + 1) / 2, rx = (maxx - minx + 1) / 2, ry = (maxy - miny + 1) / 2;
  const out = o.outline || PAL.stone[0];
  if (o.shadow) for (const [x, y] of P2) for (const [dx, dy] of [[1, 1], [0, 1], [1, 0]]) { const a = x + dx, b = y + dy; if (!inn(a, b) && !inn(a - 1, b) && !inn(a, b - 1)) cv.set(a + 1, b, o.shadow); }
  for (const [x, y] of P2) for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) if (!inn(x + dx, y + dy)) cv.set(x + dx, y + dy, out);
  let facets = null;
  if (o.facets) { facets = []; const n = o.facets; for (let k = 0; k < n; k++) { const a = hash(k, o.seed || 0, 11) * 6.28, d = 0.25 + hash(k, o.seed || 0, 12) * 0.5; facets.push({ x: cx + Math.cos(a) * rx * d, y: cy + Math.sin(a) * ry * d, n: [Math.cos(a), Math.sin(a)] }); } }
  const base = o.base ?? 3;
  const facetOf = (x, y) => { let b = 0, bd = 1e9; for (let k = 0; k < facets.length; k++) { const d = (x + .5 - facets[k].x) ** 2 + (y + .5 - facets[k].y) ** 2; if (d < bd) { bd = d; b = k; } } return b; };
  for (const [x, y] of P2) {
    const nx = (x + 0.5 - cx) / rx, ny = (y + 0.5 - cy) / ry; let t;
    if (facets) { const f = facetOf(x, y), n = facets[f].n, l = -(n[0] + n[1]) * 0.7 - (f === facetOf(Math.round(cx), Math.round(cy)) ? -0.4 : 0); t = base + (l > 0.45 ? 1 : l < -0.35 ? -1 : 0); if (facetOf(x + 1, y) !== f || facetOf(x, y + 1) !== f) t -= 1; }
    else { const l = -(nx + ny) * 0.75 - (nx * nx + ny * ny) * 0.35; t = base + (l > 0.3 ? 1 : l < -0.45 ? -1 : 0) + (l > 0.75 ? 1 : 0); }
    if (!inn(x, y - 1) || !inn(x - 1, y)) t += 1;
    if (!inn(x + 1, y) || !inn(x, y + 1)) t -= 1;
    if (o.embed !== undefined && y >= o.embed) t -= 1;
    cv.set(x, y, ramp[clamp(t, 1, ramp.length - 1)]);
  }
  if (o.glint && rx >= 3) { const gx = Math.round(cx - rx * 0.35), gy = Math.round(cy - ry * 0.4); if (inn(gx, gy) && inn(gx + 1, gy)) { cv.set(gx, gy, ramp[5]); cv.set(gx + 1, gy, ramp[5]); } }
}
// a root: a wandering line, darker on its shadow side, a lit pixel now and then on its left
function root(cv, x, y, len, seed, o = {}) {
  const r = R(seed); let dx = o.dx ?? 0; const pts = [];
  for (let k = 0; k < len; k++) { pts.push([Math.round(x), Math.round(y)]); y += o.dy ?? 1; x += dx; if (r() < 0.35) dx = clamp(dx + (r() < 0.5 ? -0.5 : 0.5), -0.8, 0.8); }
  for (const [px, py] of pts) { cv.set(px, py, PAL.root[o.thick ? 2 : 1]); if (o.thick) { cv.set(px + 1, py, PAL.root[1]); cv.set(px - 1, py, PAL.root[3]); } }
  for (let k = 1; k < pts.length; k += 3) { const [px, py] = pts[k]; if (!o.thick) cv.set(px - 1, py, PAL.root[0]); }
  return pts;
}
// a crack: a dark wandering line with the lit lip below it
function crack(cv, x, y, len, seed, ramp, o = {}) {
  const r = R(seed); let dir = o.dir ?? (r() < 0.5 ? 0 : 1); const pts = [];
  for (let k = 0; k < len; k++) { pts.push([x, y]); const turn = r(); if (turn < 0.28) y += 1; else if (turn < 0.42) y -= 1; x += dir === 0 ? 1 : -1; if (o.down && r() < 0.5) y += 1; }
  const set = new Set(pts.map(p => p.join(',')));
  for (const [px, py] of pts) cv.set(px, py, ramp[0]);
  for (const [px, py] of pts) if (!set.has(px + ',' + (py + 1))) cv.set(px, py + 1, ramp[o.lip ?? 4]);
  return pts;
}
function crystals(cv, cx, cy, n, seed, ramp = PAL.violet) {
  const r = R(seed);
  for (let k = 0; k < n; k++) {
    const x = cx + Math.round((r() - 0.5) * 6), h = 3 + Math.floor(r() * 4), y = cy - h + Math.round(r() * 2);
    for (let j = 0; j < h; j++) { cv.set(x - 1, y + j, ramp[0]); cv.set(x + 2, y + j, ramp[0]); cv.set(x, y + j, ramp[j < 1 ? 5 : 4]); cv.set(x + 1, y + j, ramp[j < 1 ? 3 : 2]); }
    cv.set(x, y - 1, ramp[0]); cv.set(x + 1, y - 1, ramp[0]); cv.set(x, y + h, ramp[0]); cv.set(x + 1, y + h, ramp[0]);
  }
}

// ---------- directions ----------
// each returns the base painter, the detail painters and the top painters for a material
const DIRS = {};

// D1 «Сколы»: angular chunks with facets; soil in rounded clods; bedrock in big slabs
DIRS.d1 = {
  base(mat, cv) {
    const W = cv.w, H = cv.h;
    if (mat === 'stone') { const S = seedsOn(W, H, 7.5, 6.5, 101); toneSeeds(S, W, H, pickW([2, 3, 3, 4]), { gx: 4, gy: 2, seed: 102 }); shadeChunks(cv, chunkMap(W, H, S), S, PAL.stone, { mode: 'facet', crack: 0, rim: true, inner: true, shareV: 0.5, shareH: 0.6 }); }
    if (mat === 'soil') { const S = seedsOn(W, H, 5.6, 5, 201); toneSeeds(S, W, H, pickW([2, 3, 3, 3]), { gx: 4, gy: 2, seed: 202 }); shadeChunks(cv, chunkMap(W, H, S), S, PAL.soil, { mode: 'round', R: 3.4, crack: 1, rim: true, share: 0.32 });
      for (let k = 0; k < 7; k++) { const x = 6 + hash(k, 1, 203) * 116, y = 4 + hash(k, 2, 203) * 56; rock(cv, ellipsePts(x, y, 1.6 + hash(k, 3, 203) * 1.6, 1.2 + hash(k, 4, 203), k + 30), PAL.pebble, { outline: PAL.soil[0], base: 3 }); }
      for (let k = 0; k < 5; k++) root(cv, 4 + hash(k, 1, 204) * 120, hash(k, 2, 204) * 50, 5 + Math.floor(hash(k, 3, 204) * 8), 900 + k); }
    if (mat === 'bedrock') { const S = seedsOn(W, H, 12, 9, 301); toneSeeds(S, W, H, pickW([1, 2, 2, 3]), { gx: 4, gy: 2, seed: 302, min: 1, max: 3 }); shadeChunks(cv, chunkMap(W, H, S), S, PAL.bedrock, { mode: 'facet', crack: 0, rim: true, inner: true, share: 0.62 }); }
  },
  rockStyle: { facets: 4 },
};
// D2 «Пласты»: beds of flat slabs with seams; soil in horizons with rootlets; bedrock laminated
DIRS.d2 = {
  base(mat, cv) {
    const W = cv.w, H = cv.h;
    if (mat === 'stone') { const S = seedsOn(W, H, 15, 5.4, 111, 0.55); const band = (s) => { const h = hash(s.row, 0, 113); return h < 0.18 ? 4 : h < 0.55 ? 3 : h < 0.85 ? 2 : 1; }; for (const s of S) s.t0 = clamp(band(s) + (s.r < 0.15 ? -1 : s.r > 0.88 ? 1 : 0), 1, 4); shadeChunks(cv, chunkMap(W, H, S, 0.42, 1.15), S, PAL.stone, { mode: 'flat', crack: 0, rim: true, inner: false, shareH: 0.9, shareV: 0.28 });
      for (let k = 0; k < 4; k++) { const x = hash(k, 1, 114) * 128, y = hash(k, 2, 114) * 64; rock(cv, ellipsePts(x, y, 3 + hash(k, 3, 114) * 2, 1.4, k + 50), PAL.tan, { outline: PAL.stone[0], base: 3 }); } }
    if (mat === 'soil') { const S = seedsOn(W, H, 8.5, 4, 211, 0.6); const band = (s) => { const h = hash(s.row, 0, 213); return h < 0.3 ? 2 : h < 0.8 ? 3 : 4; }; for (const s of S) s.t0 = clamp(band(s) + (s.r < 0.2 ? -1 : 0), 1, 4); shadeChunks(cv, chunkMap(W, H, S, 0.55, 1.1), S, PAL.soil, { mode: 'flat', crack: 1, rim: true, shareH: 0.45, shareV: 0.12 });
      for (let k = 0; k < 9; k++) root(cv, 3 + hash(k, 1, 214) * 122, hash(k, 2, 214) * 40, 8 + Math.floor(hash(k, 3, 214) * 12), 950 + k);
      for (let k = 0; k < 5; k++) { const x = hash(k, 1, 215) * 128, y = hash(k, 2, 215) * 64; rock(cv, ellipsePts(x, y, 2.4 + hash(k, 3, 215) * 1.5, 1.2, k + 70), PAL.pebble, { outline: PAL.soil[0], base: 3 }); } }
    if (mat === 'bedrock') { const S = seedsOn(W, H, 21, 4.2, 311, 0.5); for (const s of S) s.t0 = clamp([1, 2, 2, 3][Math.floor(hash(s.row, 0, 313) * 4)] + (s.r < 0.15 ? -1 : 0), 1, 3); shadeChunks(cv, chunkMap(W, H, S, 0.33, 1.2), S, PAL.bedrock, { mode: 'flat', crack: 0, rim: true, shareH: 0.85, shareV: 0.2 }); }
  },
  rockStyle: { flat: true },
};
// D3 «Валуны»: rounded stones set in a matrix (conglomerate); soil full of pebbles; bedrock in rounded boulders
DIRS.d3 = {
  base(mat, cv) {
    const W = cv.w, H = cv.h;
    if (mat === 'stone') { const S = seedsOn(W, H, 4, 3.6, 121); toneSeeds(S, W, H, () => 2, { gx: 8, gy: 4, seed: 122, min: 1, max: 3 }); shadeChunks(cv, chunkMap(W, H, S), S, PAL.stone, { mode: 'flat', crack: null, softEdge: true, rim: false });
      for (let k = 0; k < 26; k++) { const x = hash(k, 1, 123) * 128, y = hash(k, 2, 123) * 64, rr = 2 + hash(k, 3, 123) ** 1.6 * 4; rock(cv, ellipsePts(x, y, rr * (1.1 + hash(k, 4, 123) * .4), rr, k + 90, 0.1), hash(k, 5, 123) < 0.25 ? PAL.tan : PAL.stone, { outline: PAL.stone[0], base: 3, glint: true }); } }
    if (mat === 'soil') { const S = seedsOn(W, H, 3.6, 3.4, 221); toneSeeds(S, W, H, () => 3, { gx: 8, gy: 4, seed: 222, min: 2, max: 4 }); shadeChunks(cv, chunkMap(W, H, S), S, PAL.soil, { mode: 'flat', crack: null, softEdge: true, rim: false });
      for (let k = 0; k < 20; k++) { const x = hash(k, 1, 223) * 128, y = hash(k, 2, 223) * 64, rr = 1.3 + hash(k, 3, 223) ** 1.5 * 2.6; rock(cv, ellipsePts(x, y, rr * 1.2, rr, k + 130, 0.1), PAL.pebble, { outline: PAL.soil[0], base: 3, glint: true }); }
      for (let k = 0; k < 4; k++) root(cv, 3 + hash(k, 1, 224) * 122, hash(k, 2, 224) * 50, 6 + Math.floor(hash(k, 3, 224) * 8), 980 + k); }
    if (mat === 'bedrock') { const S = seedsOn(W, H, 10, 8.5, 321); toneSeeds(S, W, H, pickW([1, 2, 2, 3]), { gx: 4, gy: 2, seed: 322, min: 1, max: 3 }); shadeChunks(cv, chunkMap(W, H, S), S, PAL.bedrock, { mode: 'round', R: 5, crack: 0, rim: true, share: 0.6 }); }
  },
  rockStyle: { round: true },
};

// details: overlays with alpha, own outlines, so they sit on any base cell
function rockFor(dir, cv, cx, cy, rx, ry, ramp, outline, seed, extra = {}) {
  const st = DIRS[dir].rockStyle;
  const pts = st.facets ? ellipsePts(cx, cy, rx, ry, seed, 0, 7) : st.flat ? ellipsePts(cx, cy, rx * 1.25, ry * 0.8, seed, 0.06) : ellipsePts(cx, cy, rx, ry, seed, 0.1);
  rock(cv, pts, ramp, { outline, base: 3, facets: st.facets && rx > 3 ? st.facets : 0, seed, shadow: extra.shadow, glint: !st.facets });
}
function details(dir, mat, sheet) {
  const ramp = PAL[mat], out = ramp[0], sh = ramp[1];
  const cell = (k, fn) => { const c = new Cv(16, 16); fn(c); c.blitTo(sheet, k * 16, 0); };
  const big = (ox, oy, n, fn) => { const c = new Cv(16 * n, 16 * n); fn(c); c.blitTo(sheet, ox, oy); };
  const pebR = mat === 'soil' ? PAL.pebble : mat === 'stone' ? PAL.tan : PAL.bedrock;
  if (mat === 'soil') {
    cell(0, c => rockFor(dir, c, 8, 9, 3.2, 2.4, PAL.pebble, out, 1, { shadow: sh }));
    cell(1, c => { rockFor(dir, c, 5, 6, 2, 1.6, PAL.pebble, out, 2); rockFor(dir, c, 10.5, 11, 2.4, 1.8, PAL.pebble, out, 3); });
    cell(2, c => root(c, 7, 1, 14, 21));
    cell(3, c => { const p = root(c, 5, 1, 13, 22); root(c, p[5][0] + 1, p[5][1], 7, 23, { dx: 0.6 }); });
    cell(4, c => { for (let y = 0; y < 3; y++) for (let x = 0; x < 5; x++) { const e = ((x - 2) / 2.6) ** 2 + ((y - 1) / 1.6) ** 2; if (e < 1) c.set(5 + x, 7 + y, y === 0 ? PAL.soil[1] : PAL.soil[0]); } c.set(5, 6, PAL.soil[4]); c.set(6, 6, PAL.soil[4]); c.set(7, 6, PAL.soil[4]); });
    cell(5, c => rockFor(dir, c, 8, 8, 5, 3.6, PAL.pebble, out, 5, { shadow: sh }));
    cell(6, c => { root(c, 11, 0, 9, 24); rockFor(dir, c, 5.5, 11, 2.4, 2, PAL.pebble, out, 6); });
    cell(7, c => { rockFor(dir, c, 4, 4, 1.6, 1.3, PAL.pebble, out, 7); rockFor(dir, c, 11, 7, 1.8, 1.4, PAL.tan, out, 8); rockFor(dir, c, 7, 12, 1.6, 1.3, PAL.pebble, out, 9); });
    big(0, 16, 2, c => { rockFor(dir, c, 16, 17, 10, 7.5, PAL.pebble, out, 31, { shadow: sh }); root(c, 26, 2, 10, 32); });
    big(32, 16, 2, c => { const p = root(c, 15, 0, 26, 33, { thick: true }); root(c, p[6][0] + 1, p[6][1], 13, 34, { dx: 0.7 }); root(c, p[10][0] - 1, p[10][1], 12, 35, { dx: -0.7 }); root(c, p[16][0] + 1, p[16][1], 9, 36, { dx: 0.5 }); });
    big(64, 16, 3, c => { rockFor(dir, c, 22, 26, 15, 11, PAL.pebble, out, 37, { shadow: sh }); rockFor(dir, c, 38, 11, 4, 3, PAL.pebble, out, 38); rockFor(dir, c, 8, 40, 3, 2.4, PAL.tan, out, 39); root(c, 40, 20, 22, 40); root(c, 6, 4, 14, 41); });
  }
  if (mat === 'stone') {
    cell(0, c => crack(c, 2, 7, 12, 51, ramp));
    cell(1, c => { const p = crack(c, 3, 4, 8, 52, ramp, { down: true }); crack(c, p[4][0], p[4][1], 6, 53, ramp); });
    cell(2, c => rockFor(dir, c, 8, 8, 3.4, 2.6, PAL.tan, out, 54, { shadow: sh }));
    cell(3, c => { rockFor(dir, c, 6, 6, 2.2, 1.8, PAL.quartz, out, 55); rockFor(dir, c, 10, 10, 1.8, 1.4, PAL.quartz, out, 56); });
    cell(4, c => { crack(c, 1, 11, 9, 57, ramp); rockFor(dir, c, 11, 6, 2.4, 2, ramp, out, 58); });
    cell(5, c => { for (const [x, y] of [[6, 7], [7, 7], [7, 8], [10, 9]]) c.set(x, y, PAL.quartz[4]); c.set(6, 8, PAL.quartz[2]); c.set(11, 9, PAL.quartz[2]); });
    cell(6, c => crack(c, 0, 9, 16, 59, ramp));
    cell(7, c => rockFor(dir, c, 8, 9, 4.2, 3.2, PAL.stone.map((v, i) => PAL.stone[clamp(i + 1, 0, 5)]), out, 60, { shadow: sh }));
    big(0, 16, 2, c => rockFor(dir, c, 16, 16, 11, 8.5, PAL.stone.map((v, i) => PAL.stone[clamp(i + 1, 0, 5)]), out, 61, { shadow: sh }));
    big(32, 16, 2, c => { // a quartz vein across the rock
      const pts = new Set(); for (let x = 3; x < 29; x++) { const y0 = Math.round(8 + x * 0.55 + Math.sin(x * 0.5) * 1.5), th = x < 6 || x > 25 ? 1 : 2 + (Math.sin(x * 0.9) > 0.3 ? 1 : 0); for (let k = 0; k < th; k++) pts.add(x + ',' + (y0 + k)); }
      rock(c, pts, PAL.quartz.map((v, i) => mix(PAL.quartz[clamp(i - 1, 0, 5)], PAL.stone[i], 0.35)), { outline: out, base: 3 }); });
    big(64, 16, 3, c => { // a lens of a darker stratum, its top lit
      const pts = new Set(); for (let x = 2; x < 46; x++) { const t = (x - 2) / 43, th = Math.round(Math.sin(t * Math.PI) * 9), yc = 22 + Math.round(Math.sin(x * 0.18) * 2.5); for (let y = yc - Math.floor(th / 2); y < yc + Math.ceil(th / 2); y++) pts.add(x + ',' + y); }
      const lens = new Cv(48, 48); const S = seedsOn(48, 48, 7, 3.5, 66, 0.5); for (const s of S) s.t0 = 1 + Math.floor(s.r * 2); const tmp = new Cv(48, 48); shadeChunks(tmp, chunkMap(48, 48, S, 0.5, 1.2), S, PAL.bedrock.map((v, i) => mix(PAL.bedrock[i], PAL.stone[i], 0.45)), { mode: 'flat', crack: 0, rim: true });
      for (const k of pts) { const [x, y] = k.split(',').map(Number); c.set(x, y, tmp.get(x, y)); if (!pts.has(x + ',' + (y - 1))) c.set(x, y - 1, PAL.stone[0]); if (!pts.has(x + ',' + (y - 2)) && !pts.has(x + ',' + (y - 1))) c.set(x, y - 2, PAL.stone[4]); if (!pts.has(x + ',' + (y + 1))) c.set(x, y + 1, PAL.stone[0]); }
      void lens; });
  }
  if (mat === 'bedrock') {
    cell(0, c => crack(c, 1, 6, 14, 71, ramp, { lip: 3 }));
    cell(1, c => crack(c, 3, 10, 10, 72, ramp, { lip: 3, down: true }));
    cell(2, c => crystals(c, 8, 11, 2, 73));
    cell(3, c => rockFor(dir, c, 8, 8, 3.6, 2.8, ramp, ramp[0], 74, { shadow: ramp[0] }));
    cell(4, c => { crack(c, 0, 4, 9, 75, ramp, { lip: 3 }); crack(c, 7, 11, 9, 76, ramp, { lip: 3 }); });
    cell(5, c => rockFor(dir, c, 7, 9, 2.6, 2, PAL.violet.map((v, i) => mix(PAL.violet[i], PAL.bedrock[i], 0.45)), ramp[0], 77));
    cell(6, c => crack(c, 0, 8, 16, 78, ramp, { lip: 3 }));
    cell(7, c => { crack(c, 2, 3, 7, 79, ramp, { lip: 3, down: true }); });
    big(0, 16, 2, c => rockFor(dir, c, 16, 16, 12, 9, ramp.map((v, i) => ramp[clamp(i + 1, 0, 5)]), ramp[0], 81, { shadow: ramp[0] }));
    big(32, 16, 2, c => { rockFor(dir, c, 16, 18, 9, 6, ramp, ramp[0], 82); crystals(c, 14, 19, 4, 83); });
    big(64, 16, 3, c => { const p = crack(c, 2, 8, 44, 84, ramp, { lip: 3, down: true }); for (const k of [10, 22, 33]) crack(c, p[k][0], p[k][1], 8, 85 + k, ramp, { lip: 3, dir: 1 }); const q = crack(c, 4, 12, 40, 99, ramp, { lip: 3, down: true }); void q; });
  }
}
const mix = (a, b, t) => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t].map(Math.round);

// ---------- the living edge ----------
// sy(x): the surface's first solid row in a 16x32 tile laid from the cell above (16 for a flat top)
const SURF = { flat: () => 16, br: x => 16 + 15 - x, bl: x => 16 + x };
function profile(seed, lo, hi, ends) { const r = R(seed), bumps = []; for (let k = 0; k < 3; k++) bumps.push([2 + r() * 12, 1.6 + r() * 2.6, lo + r() * (hi - lo)]); return x => { if (x <= 0 || x >= 15) return ends; let v = 0; for (const [c, w, h] of bumps) v = Math.max(v, h * Math.exp(-(((x - c) / w) ** 2))); return clamp(Math.round(Math.max(ends * (1 - Math.min(x, 15 - x) / 3), v)), 0, hi); }; }
function tops(dir, mat, sheet) {
  const kinds = [['flat', 0], ['flat', 1], ['flat', 2], ['flat', 3], ['br', 0], ['br', 1], ['bl', 0], ['bl', 1]];
  kinds.forEach(([kind, v], col) => {
    const back = new Cv(16, 32), front = new Cv(16, 32), sy = SURF[kind], seed = 1000 + col * 17 + (mat.length * 3) + dir.charCodeAt(1) * 101;
    topPainters[dir][mat](back, front, sy, seed, kind);
    back.blitTo(sheet, col * 16, 0); front.blitTo(sheet, col * 16, 32);
  });
}
// moss cushion along the surface, its ends two pixels high so neighbours join
function cushion(front, sy, seed, ramp, lo, hi, outline = true) {
  const hc = profile(seed, lo, hi, 2);
  for (let x = 0; x < 16; x++) {
    const s = sy(x), h = hc(x);
    for (let k = -1; k <= h; k++) { const y = s - k; if (k === -1) front.set(x, y, ramp[1]); else if (k === h) front.set(x, y, ramp[h >= 2 && hc(x - 1) < h ? 5 : 4]); else if (k === h - 1) front.set(x, y, ramp[3]); else front.set(x, y, ramp[2]); }
    if (outline) { front.set(x, s - h - 1, ramp[0]); if (x > 0 && hc(x - 1) < h - 1) for (let y = s - h; y < s - hc(x - 1); y++) front.set(x - 1, y, ramp[0]); if (x < 15 && hc(x + 1) < h - 1) for (let y = s - h; y < s - hc(x + 1); y++) front.set(x + 1, y, ramp[0]); }
  }
  return hc;
}
function blades(back, x, base, n, seed, ramp, glow) {
  const r = R(seed);
  for (let k = 0; k < n; k++) { const bx = x + k - Math.floor(n / 2), h = 2 + Math.floor(r() * 4), lean = r() < 0.4 ? (r() < 0.5 ? -1 : 1) : 0;
    for (let j = 0; j < h; j++) back.set(bx + (j > h / 2 ? lean : 0), base - j, ramp[j < h / 2 ? 2 : j === h - 1 ? 4 : 3]);
    if (glow && k === 0 && h >= 3) { back.set(bx + lean, base - h, PAL.glow[1]); back.set(bx + lean, base - h + 1, PAL.glow[0]); } }
}
function sitRock(cv, sy, cx, rx, ry, ramp, outline, seed, style, sinkIn = 1) {
  const s = sy(Math.round(cx)), cy = s - ry + sinkIn + 0.5;
  const pts = style.facets ? ellipsePts(cx, cy, rx, ry, seed, 0, 6) : style.flat ? ellipsePts(cx, cy, rx * 1.3, ry * 0.7, seed, 0.05) : ellipsePts(cx, cy, rx, ry, seed, 0.08);
  for (const k of [...pts]) { const [x, y] = k.split(',').map(Number); if (y > sy(clamp(x, 0, 15)) + sinkIn || x < 0 || x > 15) pts.delete(k); }
  rock(cv, pts, ramp, { outline, base: 3, facets: style.facets && rx > 3 ? 3 : 0, seed, embed: sy(Math.round(cx)) });
}
const topPainters = { d1: {}, d2: {}, d3: {} };
// the cushion behind the hero's feet, so boots stand on the ground; in front only a few thin tufts, at most two pixels over it
topPainters.d1.soil = (back, front, sy, seed) => { const hc = cushion(back, sy, seed, PAL.moss, 0, 2); const r = R(seed + 5); for (let k = 0; k < 3; k++) { const x = 2 + Math.floor(r() * 12); blades(back, x, sy(x) - hc(x) - 1, 2 + Math.floor(r() * 2), seed + k, PAL.moss, r() < 0.3); }
  for (let k = 0; k < 3; k++) { const x = 1 + Math.floor(r() * 14), h = r() < 0.5 ? 2 : 1, s = sy(x); for (let j = 0; j < h; j++) front.set(x, s - 1 - j, PAL.moss[j === h - 1 ? 5 : 3]); if (r() < 0.5) front.set(x + 1, s - 1, PAL.moss[4]); } };
topPainters.d1.stone = (back, front, sy, seed) => { const r = R(seed); sitRock(back, sy, 3 + r() * 4, 2.6 + r() * 1.4, 2 + r(), PAL.stone, PAL.stone[0], seed, { facets: 1 }, 1); sitRock(front, sy, 10 + r() * 3, 1.6 + r() * 0.8, 1.3, PAL.stone, PAL.stone[0], seed + 1, { facets: 1 }, 1); };
topPainters.d1.bedrock = (back, front, sy, seed) => { const r = R(seed); for (let k = 0; k < (r() < 0.45 ? 1 : 2); k++) { if (r() < 0.25) continue; const x = 3 + k * 7 + Math.floor(r() * 4), h = 2 + Math.floor(r() * 3), s = sy(x); for (let j = 0; j < h; j++) { back.set(x, s - 1 - j, PAL.bedrock[j === h - 1 ? 5 : 4]); if (j < h - 1) back.set(x + 1, s - 1 - j, PAL.bedrock[2]); } back.set(x, s - 1 - h, PAL.bedrock[0]); back.set(x - 1, s - 1, PAL.bedrock[0]); back.set(x - 1, s - 2, PAL.bedrock[0]); back.set(x + 2, s - 1, PAL.bedrock[0]); if (h > 2) back.set(x + 1, s - h, PAL.bedrock[0]); } };
topPainters.d2.soil = (back, front, sy, seed) => { const r = R(seed); for (let x = 0; x < 16; x++) { const s = sy(x); front.set(x, s, PAL.grass[1]); front.set(x, s - 1, PAL.grass[2]); if (r() < 0.55) front.set(x, s - 2, PAL.grass[3]); }
  for (let x = 0; x < 16; x++) { if (r() < 0.45) { const h = 3 + Math.floor(r() * 5), lean = r() < 0.5 ? 0 : (r() < 0.5 ? -1 : 1), s = sy(x) - 2; for (let j = 0; j < h; j++) back.set(x + (j > h * 0.6 ? lean : 0), s - j, PAL.grass[j < 2 ? 2 : j === h - 1 ? 5 : (x + j) % 2 ? 3 : 4]); if (r() < 0.06) back.set(x + lean, s - h, PAL.glow[1]); } } };
topPainters.d2.stone = (back, front, sy, seed) => { const r = R(seed); const x0 = 1 + Math.floor(r() * 5), len = 6 + Math.floor(r() * 5); for (let x = x0; x < Math.min(15, x0 + len); x++) { const s = sy(x); back.set(x, s - 1, PAL.stone[x === x0 ? 4 : 3]); back.set(x, s - 2, PAL.stone[x < x0 + 2 ? 5 : 4]); back.set(x, s - 3, PAL.stone[0]); } back.set(x0 - 1, sy(x0 - 1) - 1, PAL.stone[0]); back.set(x0 - 1, sy(x0 - 1) - 2, PAL.stone[0]); const xe = Math.min(15, x0 + len); back.set(xe, sy(xe) - 1, PAL.stone[0]); back.set(xe, sy(xe) - 2, PAL.stone[0]);
  if (r() < 0.8) sitRock(front, sy, 12 + r() * 2, 1.5, 1.1, PAL.pebble, PAL.stone[0], seed + 3, { flat: 1 }, 1); };
topPainters.d2.bedrock = (back, front, sy, seed) => { const r = R(seed); for (let k = 0; k < 2; k++) { const x0 = k * 8 + Math.floor(r() * 3), len = 3 + Math.floor(r() * 4); for (let x = x0; x < Math.min(16, x0 + len); x++) { const s = sy(x); back.set(x, s - 1, PAL.bedrock[x === x0 ? 5 : 4]); back.set(x, s - 2, PAL.bedrock[0]); } back.set(x0 - 1, sy(x0 - 1) - 1, PAL.bedrock[0]); } };
topPainters.d3.soil = (back, front, sy, seed) => { const hc = cushion(front, sy, seed, PAL.moss, 0, 2); const r = R(seed + 7); sitRock(front, sy, 3 + r() * 9, 1.6 + r() * 1.2, 1.3 + r() * 0.7, PAL.pebble, PAL.soil[0], seed + 2, { round: 1 }, 1);
  if (r() < 0.6) { const x0 = 2 + Math.floor(r() * 6), len = 5 + Math.floor(r() * 5); for (let k = 0; k <= len; k++) { const x = x0 + k; if (x > 15) break; const y = sy(x) - hc(x) - Math.round(1.8 * Math.sin(Math.PI * k / len)); back.set(x, y, PAL.root[2]); back.set(x, y + 1, PAL.root[1]); } } void hc; };
topPainters.d3.stone = (back, front, sy, seed) => { const r = R(seed); sitRock(back, sy, 4 + r() * 6, 2.6 + r() * 1.6, 2.2 + r(), PAL.stone, PAL.stone[0], seed, { round: 1 }, 1); sitRock(front, sy, 12 + r() * 2, 1.4 + r() * 0.6, 1.2, r() < 0.4 ? PAL.tan : PAL.stone, PAL.stone[0], seed + 1, { round: 1 }, 1); };
topPainters.d3.bedrock = (back, front, sy, seed) => { const r = R(seed); sitRock(back, sy, 5 + r() * 6, 3.2 + r() * 1.5, 2.2, PAL.bedrock, PAL.bedrock[0], seed, { round: 1 }, 1); };

// ---------- write ----------
fs.mkdirSync(OUT, { recursive: true });
const preview = [];
for (const dir of ['d1', 'd2', 'd3']) for (const mat of ['soil', 'stone', 'bedrock']) {
  const base = new Cv(128, 64, true); DIRS[dir].base(mat, base);
  const wall = P.create(128, 64); base.blitTo(wall, 0, 0); P.encode(wall, path.join(OUT, `${dir}_wall_${mat}.png`));
  const back = P.create(128, 64); for (let i = 0; i < 128 * 64; i++) { const r = wall.data[i * 4], g = wall.data[i * 4 + 1], b = wall.data[i * 4 + 2]; back.data[i * 4] = r * 0.5; back.data[i * 4 + 1] = g * 0.52; back.data[i * 4 + 2] = b * 0.6; back.data[i * 4 + 3] = 255; } P.encode(back, path.join(OUT, `${dir}_back_${mat}.png`));
  const det = P.create(128, 64); details(dir, mat, det); P.encode(det, path.join(OUT, `${dir}_detail_${mat}.png`));
  const top = P.create(128, 64); tops(dir, mat, top); P.encode(top, path.join(OUT, `${dir}_top_${mat}.png`));
  preview.push([dir, mat, wall, det, top]);
}
fs.writeFileSync(path.join(OUT, 'palette.json'), JSON.stringify(Object.fromEntries(Object.entries(PAL).map(([k, v]) => [k, v.map(c => '#' + c.map(n => n.toString(16).padStart(2, '0')).join(''))])), null, 1));
// a preview for checking: per row wall | detail on wall | top over a flat strip, x4
const K = 4, rowH = 64 * K + 8, img = P.create((128 * 3 + 16) * K, preview.length * rowH);
for (let i = 0; i < img.w * img.h; i++) { img.data[i * 4] = 18; img.data[i * 4 + 1] = 20; img.data[i * 4 + 2] = 24; img.data[i * 4 + 3] = 255; }
preview.forEach(([dir, mat, wall, det, top], r) => {
  const put = (src, ox, alphaOnly) => { for (let y = 0; y < 64; y++) for (let x = 0; x < 128; x++) { const s = (y * 128 + x) * 4; if (alphaOnly && !src.data[s + 3]) continue; for (let j = 0; j < K; j++) for (let i = 0; i < K; i++) { const o = (((r * rowH) + y * K + j) * img.w + ox + x * K + i) * 4; img.data[o] = src.data[s]; img.data[o + 1] = src.data[s + 1]; img.data[o + 2] = src.data[s + 2]; } } };
  put(wall, 0); put(wall, (128 + 8) * K); put(det, (128 + 8) * K, true);
  // tops over a strip of the wall: the lower half of each tile is ground
  const strip = P.create(128, 64); for (let y = 0; y < 64; y++) for (let x = 0; x < 128; x++) { const s = (y * 128 + x) * 4, o = s; const tileY = y % 32, col = Math.floor(x / 16), lx = x % 16; const sy = col < 4 ? 16 : col < 6 ? 16 + 15 - lx : 16 + lx; if (tileY >= sy) { strip.data[o] = wall.data[s]; strip.data[o + 1] = wall.data[s + 1]; strip.data[o + 2] = wall.data[s + 2]; } else { strip.data[o] = 120; strip.data[o + 1] = 160; strip.data[o + 2] = 200; } strip.data[o + 3] = 255; }
  for (let y = 0; y < 64; y++) for (let x = 0; x < 128; x++) { const s = (y * 128 + x) * 4; const ty = y % 32; const ss = (ty * 128 + x) * 4, fs2 = ((ty + 32) * 128 + x) * 4; for (const src of [ss, fs2]) if (top.data[src + 3]) { strip.data[s] = top.data[src]; strip.data[s + 1] = top.data[src + 1]; strip.data[s + 2] = top.data[src + 2]; } }
  put(strip, (256 + 16) * K);
});
P.encode(img, path.join(OUT, 'preview.png'));
for (const kind of ['wall', 'back', 'detail', 'top']) for (const mat of ['soil', 'stone', 'bedrock']) fs.copyFileSync(path.join(OUT, `d1_${kind}_${mat}.png`), path.join(PLANET, `${kind}_${mat}.png`));
console.log('written', fs.readdirSync(OUT).length, 'files');

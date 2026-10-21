// Shared bits: the engines of the earlier pages, palettes of the arc relic, a small sprite buffer with shapes and an outline.
// The earlier pages' engines, now in the repo: the caves' (tools/planet-art/caves) and the homes' (ruins-decor).
const RD = '../';
const E = require('../../planet-art/caves/engine');
const O = require('../../planet-art/caves/objects');
const ROOM = require('../ruins-decor/room');
const D = require('../ruins-decor/decor');
const { hex, mix, mul, hash, vnoise } = E;
const pal = (...h) => h.map(hex);

// the relic's light: violet-white, as the arc tech's mark
const VC = { core: hex('#fdfaff'), hi: hex('#e4d6ff'), mid: hex('#b493ff'), lo: hex('#7552e0'), dk: hex('#2e1d78'), light: [0.74, 0.62, 1] };
const VG = pal('#07060c', '#151027', '#241c44', '#372c66', '#5848a0', '#a090e0'); // void glass: ol .. sheen
const AM = pal('#07070b', '#14131c', '#22202d', '#353244', '#55506c', '#9a92b8'); // alien metal
const IRON = pal('#0b0908', '#1e1814', '#302720', '#463a30', '#625142', '#8a7460'); // meteor crust
const CHIT = pal('#0e0b10', '#261d26', '#3c2f3c', '#574656', '#7c687c', '#a894a8'); // the seed's husk
const CU = D.PAL.copper; // ol, dk, sh, base, lt
const CER = pal('#262119', '#5a5244', '#8e8470', '#c4b99e', '#ebe2c8'); // porcelain of their insulators
const A = D.PAL.alloy;
const ST = D.PAL.stone;

class Spr {
  constructor(w, h) { this.w = w; this.h = h; this.c = new Array(w * h).fill(null); this.g = new Float32Array(w * h); this.m = new Array(w * h).fill(null); }
  in(x, y) { return x >= 0 && y >= 0 && x < this.w && y < this.h; }
  set(x, y, col, mat = 'x', glow = 0) { x = Math.round(x); y = Math.round(y); if (!this.in(x, y)) return; const i = y * this.w + x; this.c[i] = col; this.m[i] = mat; this.g[i] = glow; }
  glow(x, y, col, k = 1, mat = 'glow') { this.set(x, y, col, mat, k); }
  has(x, y) { return this.in(x, y) && this.c[y * this.w + x] !== null; }
  mat(x, y) { return this.in(x, y) ? this.m[y * this.w + x] : null; }
  // an outline outside every filled pixel of the given materials, in that material's darkest colour
  outline(ols, only) {
    const add = [];
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      if (this.has(x, y)) continue;
      for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) { const m = this.mat(x + dx, y + dy); if (m && ols[m] && (!only || only.includes(m))) { add.push([x, y, ols[m]]); break; } }
    }
    for (const [x, y, c] of add) this.set(x, y, c, 'ol');
  }
  blit(pic, ox, oy, o = {}) {
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      const i = y * this.w + x, c = this.c[i]; if (!c) continue; const X = ox + x, Y = oy + y;
      if (pic.inb(X, Y)) pic.unlit[Y * pic.w.PW + X] = 0;
      if (this.g[i] > 0) { pic.glow(X, Y, c, this.g[i]); if (pic.art && pic.inb(X, Y)) pic.art[Y * pic.w.PW + X] = 1; }
      else pic.set(X, Y, c);
    }
  }
  png(P) {
    const img = P.create(this.w, this.h);
    for (let i = 0; i < this.w * this.h; i++) { const c = this.c[i], o = i * 4; if (!c) { img.data[o + 3] = 0; continue; } img.data[o] = Math.round(Math.min(1, c[0]) * 255); img.data[o + 1] = Math.round(Math.min(1, c[1]) * 255); img.data[o + 2] = Math.round(Math.min(1, c[2]) * 255); img.data[o + 3] = 255; }
    return img;
  }
}

// polygon fill with a colour function of the pixel (returns [col, glow] or col or null)
function inPoly(pts, x, y) {
  let ins = false;
  for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
    const [xi, yi] = pts[i], [xj, yj] = pts[j];
    if ((yi > y) !== (yj > y) && x < ((xj - xi) * (y - yi)) / (yj - yi) + xi) ins = !ins;
  }
  return ins;
}
function poly(s, pts, f, mat = 'x') {
  const xs = pts.map((p) => p[0]), ys = pts.map((p) => p[1]);
  for (let y = Math.floor(Math.min(...ys)); y <= Math.ceil(Math.max(...ys)); y++) for (let x = Math.floor(Math.min(...xs)); x <= Math.ceil(Math.max(...xs)); x++) {
    if (!inPoly(pts, x + 0.5, y + 0.5)) continue; const r = f(x, y); if (!r) continue;
    if (Array.isArray(r[0])) s.set(x, y, r[0], mat, r[1] || 0); else s.set(x, y, r, mat);
  }
}
function ell(s, cx, cy, rx, ry, f, mat = 'x') {
  for (let y = Math.floor(cy - ry - 1); y <= Math.ceil(cy + ry + 1); y++) for (let x = Math.floor(cx - rx - 1); x <= Math.ceil(cx + rx + 1); x++) {
    const nx = (x + 0.5 - cx) / rx, ny = (y + 0.5 - cy) / ry, d = nx * nx + ny * ny; if (d > 1) continue;
    const r = f(x, y, nx, ny, Math.sqrt(d)); if (!r) continue;
    if (Array.isArray(r[0])) s.set(x, y, r[0], mat, r[1] || 0); else s.set(x, y, r, mat);
  }
}
function line(x0, y0, x1, y1, f) {
  x0 = Math.round(x0); y0 = Math.round(y0); x1 = Math.round(x1); y1 = Math.round(y1);
  const dx = Math.abs(x1 - x0), dy = -Math.abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1; let e = dx + dy, k = 0;
  for (;;) { f(x0, y0, k++); if (x0 === x1 && y0 === y1) break; const e2 = 2 * e; if (e2 >= dy) { e += dy; x0 += sx; } if (e2 <= dx) { e += dx; y0 += sy; } }
}
// a lit tone from a normal (light from the upper left), into a palette's steps 1..n-1
function tone(palette, nx, ny, lo = 1) {
  const nz = Math.sqrt(Math.max(0, 1 - nx * nx - ny * ny)), l = -0.55 * nx - 0.6 * ny + 0.58 * nz;
  const t = Math.max(0, Math.min(0.999, (l + 0.25) / 1.15)); return palette[lo + Math.floor(t * (palette.length - 1 - lo))];
}
// a copper rod, one pixel wide, lit on its upper-left side
function rod(s, x0, y0, x1, y1, o = {}) { line(x0, y0, x1, y1, (x, y, k) => s.set(x, y, o.col || (k % 4 === 0 && o.wound ? CU[4] : CU[3]), 'cu')); }
// a stack of porcelain discs, centred at x, from y down n discs
function insulator(s, cx, y, n, w = 5) {
  for (let k = 0; k < n; k++) { const yy = y + k * 2; for (let i = 0; i < w; i++) s.set(cx - (w >> 1) + i, yy, i === 0 ? CER[4] : i === w - 1 ? CER[2] : CER[3], 'cer'); for (let i = 1; i < w - 1; i++) s.set(cx - (w >> 1) + i, yy + 1, CER[1], 'cer'); }
}
const OLS = { x: null, cu: CU[0], cer: CER[0], al: A[0], vg: VG[0], am: AM[0], iron: IRON[0], chit: CHIT[0], st: ST[0], glow: null };

module.exports = { RD, E, O, ROOM, D, hex, mix, mul, hash, vnoise, pal, VC, VG, AM, IRON, CHIT, CU, CER, A, ST, Spr, poly, ell, line, tone, rod, insulator, OLS, inPoly };

// The arc relic (ADR 0095), from the relic concept page's own drawing code (its lib.js and designs.js, design 1 "the sky shard",
// unchanged but for its requires): the shard held upright in its copper collar on three legs over porcelain; a chip of one in a
// home's wall niche (the page's home A); the spire's lens (its light-well spire); the shard's 16 px icon. Each piece is drawn
// into the export's Pic as decor.js's are, and says where its arcs leave from, the metal they jump to and its heart.
const { hex, mix } = require('./engine');
const D = require('./decor');

const pal = (...h) => h.map(hex);
// The relic's light: violet-white, as the arc tech's mark.
const VC = { core: hex('#fdfaff'), hi: hex('#e4d6ff'), mid: hex('#b493ff'), lo: hex('#7552e0'), dk: hex('#2e1d78'), light: [0.74, 0.62, 1] };
const VG = pal('#07060c', '#151027', '#241c44', '#372c66', '#5848a0', '#a090e0'); // void glass: ol .. sheen
const CU = D.PAL.copper; // ol, dk, sh, base, lt
const CER = pal('#262119', '#5a5244', '#8e8470', '#c4b99e', '#ebe2c8'); // porcelain of their insulators
const A = D.PAL.alloy;

class Spr {
  constructor(w, h) { this.w = w; this.h = h; this.c = new Array(w * h).fill(null); this.g = new Float32Array(w * h); this.m = new Array(w * h).fill(null); }
  in(x, y) { return x >= 0 && y >= 0 && x < this.w && y < this.h; }
  set(x, y, col, mat = 'x', glow = 0) { x = Math.round(x); y = Math.round(y); if (!this.in(x, y)) return; const i = y * this.w + x; this.c[i] = col; this.m[i] = mat; this.g[i] = glow; }
  glow(x, y, col, k = 1, mat = 'glow') { this.set(x, y, col, mat, k); }
  has(x, y) { return this.in(x, y) && this.c[y * this.w + x] !== null; }
  mat(x, y) { return this.in(x, y) ? this.m[y * this.w + x] : null; }
  outline(ols, only) {
    const add = [];
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      if (this.has(x, y)) continue;
      for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) { const m = this.mat(x + dx, y + dy); if (m && ols[m] && (!only || only.includes(m))) { add.push([x, y, ols[m]]); break; } }
    }
    for (const [x, y, c] of add) this.set(x, y, c, 'ol');
  }
  /** Into the export's Pic: what glows as glow, the rest as albedo. */
  blit(p, ox, oy) {
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      const i = y * this.w + x, c = this.c[i]; if (!c) continue;
      if (this.g[i] > 0) p.glow(ox + x, oy + y, c, this.g[i]); else p.set(ox + x, oy + y, c);
    }
  }
}
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
function line(x0, y0, x1, y1, f) {
  x0 = Math.round(x0); y0 = Math.round(y0); x1 = Math.round(x1); y1 = Math.round(y1);
  const dx = Math.abs(x1 - x0), dy = -Math.abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1; let e = dx + dy, k = 0;
  for (;;) { f(x0, y0, k++); if (x0 === x1 && y0 === y1) break; const e2 = 2 * e; if (e2 >= dy) { e += dy; x0 += sx; } if (e2 <= dx) { e += dx; y0 += sy; } }
}
function rod(s, x0, y0, x1, y1, o = {}) { line(x0, y0, x1, y1, (x, y, k) => s.set(x, y, o.col || (k % 4 === 0 && o.wound ? CU[4] : CU[3]), 'cu')); }
function insulator(s, cx, y, n, w = 5) {
  for (let k = 0; k < n; k++) { const yy = y + k * 2; for (let i = 0; i < w; i++) s.set(cx - (w >> 1) + i, yy, i === 0 ? CER[4] : i === w - 1 ? CER[2] : CER[3], 'cer'); for (let i = 1; i < w - 1; i++) s.set(cx - (w >> 1) + i, yy + 1, CER[1], 'cer'); }
}
const OLS = { x: null, cu: CU[0], cer: CER[0], al: A[0], vg: VG[0], glow: null };
const sc = (S) => (v) => v * S;
const P2 = (S, pts) => pts.map(([x, y]) => [x * S, y * S]);
function veins(s, pts, S, o = {}) {
  // a glowing crack: a polyline of points; nodes brighter; the matter beside it lit faintly from within
  for (let i = 0; i < pts.length - 1; i++) line(pts[i][0] * S, pts[i][1] * S, pts[i + 1][0] * S, pts[i + 1][1] * S, (x, y) => {
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) { const X = x + dx, Y = y + dy; if (s.has(X, Y) && s.mat(X, Y) !== 'glow') { const c = s.c[Y * s.w + X]; s.set(X, Y, mix(c, VC.lo, 0.45), 'glow', 0.28); } }
    s.glow(x, y, o.col || VC.hi, 0.95); if (S >= 1.6) s.glow(x + 1, y, VC.mid, 0.8);
  });
  for (const [x, y] of pts.slice(1, -1)) s.glow(x * S, y * S, VC.core, 1.25);
}
function plate(s, x0, y0, w, h) { for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) s.set(x0 + i, y0 + j, j === 0 ? A[4] : i === 0 ? A[4] : j === h - 1 ? A[1] : A[3], 'al'); }

/** The page's design 1 at scale S (1: 32 px, two cells): its sprite, its heart, where its arcs leave from and the metal they reach. */
function shard(S = 1) {
  const k = sc(S), W = Math.ceil(32 * S), s = new Spr(W, W);
  const sh = P2(S, [[12, 27], [11, 19], [12.5, 10], [15, 1.5], [17.5, 6], [19.5, 13], [21, 21], [20, 27]]);
  poly(s, sh, (x, y) => {
    const xm = k(15) + (k(16) - k(15)) * (y - k(1.5)) / k(25.5), left = x + 0.5 < xm;
    const edgeL = !inPoly(sh, x - 0.5, y + 0.5), edgeR = !inPoly(sh, x + 1.5, y + 0.5);
    if (left) return edgeL ? VG[4] : (y < k(9) && x + 0.5 > xm - 1.5) ? VG[5] : VG[3];
    return edgeR ? VG[1] : VG[2];
  }, 'vg');
  veins(s, [[16, 26], [15, 20], [17, 15], [15.5, 10], [16, 4.5]], S);
  veins(s, [[15, 20], [13, 17]], S); veins(s, [[17, 15], [19, 18]], S);
  // the collar, its legs and the porcelain under it
  const y0 = Math.round(k(19)), hC = Math.max(3, Math.round(k(3.5)));
  for (let j = 0; j < hC; j++) for (let x = Math.round(k(9)); x <= Math.round(k(23)); x++) s.set(x, y0 + j, j === 0 ? CU[4] : j === hC - 1 ? CU[1] : (x % Math.round(k(4)) === 0 && j === 1) ? CU[4] : CU[3], 'cu');
  rod(s, k(10), y0 + hC, k(5), W - 2); rod(s, k(22), y0 + hC, k(27), W - 2);
  insulator(s, Math.round(k(16)), Math.round(k(27)), Math.max(2, Math.round(2 * S)), Math.round(k(7)) | 1);
  plate(s, Math.round(k(3)), W - 2, Math.round(k(5)), 2); plate(s, Math.round(k(24.5)), W - 2, Math.round(k(5)), 2);
  plate(s, Math.round(k(12)), W - 1, Math.round(k(9)), 1);
  s.outline(OLS);
  return { spr: s, core: [k(16), k(15)], src: P2(S, [[15, 2], [19.5, 13], [11.5, 15], [21, 21], [12.5, 10]]), metal: P2(S, [[9, 20], [23, 20], [7, 27], [25, 27], [5, 30], [27, 30]]) };
}

/** The cradle with its shard taken: the copper, the porcelain and the plates, outlined again. */
function emptied(s) {
  const keep = new Set(['cu', 'cer', 'al']), t = new Spr(s.w, s.h);
  for (let y = 0; y < s.h; y++) for (let x = 0; x < s.w; x++) { const i = y * s.w + x; if (keep.has(s.m[i])) t.set(x, y, s.c[i], s.m[i]); }
  t.outline(OLS);
  return t;
}

// The spire's cradle: the shard at one and a half, three cells across and up, as the page's lighthouse holds it.
const CRADLE_S = 1.5;
const cradle = shard(CRADLE_S);
const CRADLE = {
  w: 48, h: 48, anchor: 'floor', glow: 'relic',
  draw(p, x, y, o) {
    if (o.lit === false) { emptied(cradle.spr).blit(p, x, y); return; }
    // Its light is the spires' view's, which breathes with its hum and dies with it once it is taken (RelicLooks).
    cradle.spr.blit(p, x, y);
  },
  points: { core: cradle.core, src: cradle.src, metal: cradle.metal },
};

// A chip of a shard in a niche cut into a home's wall (the page's home A): a dark recess under an alloy lintel, the chip on a
// small collar on its sill.
const chip = shard(0.62);
const NICHE = {
  w: 24, h: 32, anchor: 'wall', glow: 'relic',
  draw(p, x, y, o) {
    const rx = x + 2, ry = y + 3;
    for (let j = 0; j < 26; j++) for (let i = 0; i < 20; i++) { const e = i === 0 || j === 0 || i === 19 || j === 25; p.set(rx + i, ry + j, e ? A[0] : j === 1 ? hex('#0a0b10') : i < 3 ? hex('#15161d') : hex('#0e0f15')); }
    D.abox(p, x, ry + 24, 24, 4); D.abox(p, x, y, 24, 3);
    const s = chip.spr, cx = rx + 10 - Math.round(s.w / 2), cy = ry + 24 - s.h;
    if (o.lit !== false) {
      s.blit(p, cx, cy);
      p.light({ x: rx + 10, y: ry + 24 - s.h * 0.55, col: VC.light, r: 6, k: 1.1 });
    }
  },
};
const nicheCore = [2 + 10, 3 + 24 - chip.spr.h * 0.55];
NICHE.points = {
  core: nicheCore,
  src: [[nicheCore[0], nicheCore[1] - 6], [nicheCore[0] - 3, nicheCore[1]], [nicheCore[0] + 3, nicheCore[1] + 2]],
  metal: [[3, 7], [20, 9], [6, 25], [18, 25], [12, 2]],
  // where its copper cable leaves: from its right side toward the hearth, from its lintel up to the ceiling
  side: [24, 15], top: [12, 0],
};

/** The lens in a spire's tip (the light-well spire), 24 px round its cell as a cave's growth in the rock: its body, alight or dark. */
function lens(p, x, y, lit) {
  const cx = x + 12, top = y + 4;
  for (let i = -5; i <= 5; i++) { p.set(cx + i, top + 1, A[4]); p.set(cx + i, top + 14, A[1]); }
  for (let j = 2; j < 14; j++) for (let i = -4; i <= 4; i++) {
    const d2 = (i * i) / 20 + ((j - 8) * (j - 8)) / 40; if (d2 > 1) continue;
    if (lit) p.glow(cx + i, top + j, d2 < 0.3 ? VC.core : d2 < 0.7 ? VC.hi : VC.mid, d2 < 0.3 ? 1.2 : 0.9);
    else p.set(cx + i, top + j, d2 > 0.75 ? VG[1] : i < 0 && j < 7 ? VG[4] : i < 0 ? VG[3] : VG[2]);
  }
}

// The shard's icon, the page's 16 px (design 1).
const ICON = [
  '...........H....', '.......o........', '......o5o.......', '......o4Ho......', '.....o4H2o......', '.M...o3M2o......', '.....o4H2o......', '....o43W2o......',
  '....o4M22o......', '...ooo3H2ooo....', '..oCCCCCCCCCo...', '..okkkkkkkkqo...', '...ooo3M2ooo....', '..oco.o22o.oco..', '.oco..oppo..oco.', '.ooo..oooo..ooo.',
];
const ICOL = {
  o: hex('#100c18'), C: CU[4], c: CU[3], k: CU[2], q: CU[1], W: VC.core, H: VC.hi, M: VC.mid, L: VC.lo,
  1: VG[1], 2: VG[2], 3: VG[3], 4: VG[4], 5: VG[5], P: CER[4], p: CER[3], r: CER[2],
};

module.exports = { VC, VG, CU, CER, CRADLE, NICHE, CRADLE_S, lens, ICON, ICOL };

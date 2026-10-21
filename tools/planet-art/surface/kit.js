// Shared toolkit for the surface style studies: one terrain slice, a canvas, noise, tree skeletons,
// foliage with leaf cells, outlines, bloom, fog, and the game's own hero and crate.
const P = require('../png'), path = require('path');
const ART = path.join(__dirname, '..', '..', '..', 'game', 'art');
const C = 16, CW = 60, CH = 24, W = CW * C, H = CH * C;

// ---------- colour + noise ----------
const hex = (h) => [parseInt(h.slice(1, 3), 16) / 255, parseInt(h.slice(3, 5), 16) / 255, parseInt(h.slice(5, 7), 16) / 255];
const H_ = (v) => (typeof v === 'string' ? hex(v) : v);
const mix = (a, b, t) => { a = H_(a); b = H_(b); return [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t]; };
const mul = (a, k) => (a = H_(a), Array.isArray(k) ? [a[0] * k[0], a[1] * k[1], a[2] * k[2]] : [a[0] * k, a[1] * k, a[2] * k]);
const add3 = (a, b) => [a[0] + b[0], a[1] + b[1], a[2] + b[2]];
const clamp = (v, a = 0, b = 1) => Math.max(a, Math.min(b, v));
const lum = (c) => 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2];
const smooth = (a, b, v) => { const t = clamp((v - a) / (b - a)); return t * t * (3 - 2 * t); };
function hash(x, y, s = 0) {
  let h = Math.imul(x | 0, 374761393) + Math.imul(y | 0, 668265263) + Math.imul(s | 0, 1442695041);
  h = Math.imul(h ^ (h >>> 13), 1274126177); h ^= h >>> 16; return (h >>> 0) / 4294967296;
}
function vnoise(x, y, cell, s) {
  const gx = Math.floor(x / cell), gy = Math.floor(y / cell), fx = x / cell - gx, fy = y / cell - gy;
  const sx = fx * fx * (3 - 2 * fx), sy = fy * fy * (3 - 2 * fy);
  const a = hash(gx, gy, s), b = hash(gx + 1, gy, s), c = hash(gx, gy + 1, s), d = hash(gx + 1, gy + 1, s);
  return a + (b - a) * sx + (c - a) * sy + (a - b - c + d) * sx * sy;
}
function fbm(x, y, cell, s, oct = 4) { let v = 0, a = 0.5, n = 0; for (let k = 0; k < oct; k++) { v += vnoise(x, y, cell, s + k * 17) * a; n += a; a *= 0.5; cell /= 2; } return v / n; }
const BAYER = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5].map((v) => (v + 0.5) / 16);
const bayer = (x, y) => BAYER[(((y % 4) + 4) % 4) * 4 + (((x % 4) + 4) % 4)];
// a ramp of colours indexed by t in [0,1], optionally dithered between steps
function ramp(cols, t, x, y, dither = 0) {
  cols = cols.map(H_); const n = cols.length - 1; let f = clamp(t) * n;
  if (dither) f += (bayer(x, y) - 0.5) * dither; f = clamp(f, 0, n);
  return cols[Math.round(f)];
}
function rampSmooth(cols, t) { cols = cols.map(H_); const n = cols.length - 1, f = clamp(t) * n, i = Math.min(n - 1, Math.floor(f)); return mix(cols[i], cols[i + 1], f - i); }
function nearest(pal, c) { let b = pal[0], bd = 1e9; for (const p of pal) { const d = (p[0] - c[0]) ** 2 * 0.3 + (p[1] - c[1]) ** 2 * 0.59 + (p[2] - c[2]) ** 2 * 0.11; if (d < bd) { bd = d; b = p; } } return b; }

// ---------- canvas ----------
class Cv {
  constructor(w = W, h = H) { this.w = w; this.h = h; this.c = new Float32Array(w * h * 3); this.m = new Uint8Array(w * h); }
  inb(x, y) { return x >= 0 && y >= 0 && x < this.w && y < this.h; }
  set(x, y, col, tag = 0) { x |= 0; y |= 0; if (!this.inb(x, y)) return; const i = y * this.w + x; col = H_(col); this.c[i * 3] = col[0]; this.c[i * 3 + 1] = col[1]; this.c[i * 3 + 2] = col[2]; if (tag) this.m[i] = tag; }
  blend(x, y, col, a, tag = 0) { x |= 0; y |= 0; if (!this.inb(x, y) || a <= 0) return; if (a >= 1) return this.set(x, y, col, tag); const i = (y * this.w + x) * 3; col = H_(col); for (let k = 0; k < 3; k++) this.c[i + k] += (col[k] - this.c[i + k]) * a; if (tag) this.m[y * this.w + x] = tag; }
  add(x, y, col, k = 1) { x |= 0; y |= 0; if (!this.inb(x, y)) return; const i = (y * this.w + x) * 3; col = H_(col); this.c[i] += col[0] * k; this.c[i + 1] += col[1] * k; this.c[i + 2] += col[2] * k; }
  get(x, y) { x = clamp(x | 0, 0, this.w - 1); y = clamp(y | 0, 0, this.h - 1); const i = (y * this.w + x) * 3; return [this.c[i], this.c[i + 1], this.c[i + 2]]; }
  tag(x, y) { return this.inb(x, y) ? this.m[(y | 0) * this.w + (x | 0)] : 0; }
  each(fn) { for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) { const i = (y * this.w + x) * 3; const r = fn(x, y, [this.c[i], this.c[i + 1], this.c[i + 2]], this.m[y * this.w + x]); if (r) { this.c[i] = r[0]; this.c[i + 1] = r[1]; this.c[i + 2] = r[2]; } } }
  copy() { const o = new Cv(this.w, this.h); o.c.set(this.c); o.m.set(this.m); return o; }
  toPng(k = 1) { const img = P.create(this.w, this.h); for (let i = 0; i < this.w * this.h; i++) { for (let q = 0; q < 3; q++) img.data[i * 4 + q] = Math.round(clamp(this.c[i * 3 + q]) * 255); img.data[i * 4 + 3] = 255; } return k > 1 ? P.scale(img, k) : img; }
}
// a sparse sprite layer: alpha mask + colour, composited onto a canvas
class Layer {
  constructor(w = W, h = H) { this.w = w; this.h = h; this.c = new Float32Array(w * h * 3); this.a = new Float32Array(w * h); this.d = new Float32Array(w * h * 4); }
  set(x, y, col, a = 1) { x |= 0; y |= 0; if (x < 0 || y < 0 || x >= this.w || y >= this.h) return; const i = y * this.w + x; col = H_(col); if (a >= 1 || !this.a[i]) { this.c.set(col, i * 3); this.a[i] = Math.max(this.a[i], a); return; } for (let k = 0; k < 3; k++) this.c[i * 3 + k] += (col[k] - this.c[i * 3 + k]) * a; this.a[i] = a + this.a[i] * (1 - a); }
  has(x, y) { return x >= 0 && y >= 0 && x < this.w && y < this.h && this.a[y * this.w + x] > 0.5; }
  get(x, y) { const i = y * this.w + x; return [this.c[i * 3], this.c[i * 3 + 1], this.c[i * 3 + 2]]; }
  onto(cv, tag = 0) { for (let i = 0; i < this.w * this.h; i++) if (this.a[i] > 0) cv.blend(i % this.w, (i / this.w) | 0, [this.c[i * 3], this.c[i * 3 + 1], this.c[i * 3 + 2]], this.a[i], tag); }
  // a 1px outline around the opaque mask; colour fn(x,y,inner) lets styles colour it from the fill
  outline(colFn, sides = 'all') {
    const add = [];
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      if (this.has(x, y)) continue;
      const nb = [[1, 0], [-1, 0], [0, 1], [0, -1]].filter(([dx, dy]) => this.has(x + dx, y + dy));
      if (!nb.length) continue; if (sides === 'nottop' && nb.length === 1 && nb[0][1] === -1) continue;
      add.push([x, y, colFn(x, y, this.get(x + nb[0][0], y + nb[0][1]))]);
    }
    for (const [x, y, c] of add) this.set(x, y, c);
  }
  inlineEdge(colFn) { // darken the opaque pixels that touch air (an outline drawn inside)
    const add = [];
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) if (this.has(x, y) && [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => !this.has(x + dx, y + dy))) add.push([x, y, colFn(x, y, this.get(x, y))]);
    for (const [x, y, c] of add) this.set(x, y, c);
  }
}

// ---------- the terrain slice (identical in every panel) ----------
const TOP = []; for (let x = 0; x < CW; x++) TOP[x] = x <= 4 ? 16 : x <= 20 ? 17 : x <= 24 ? 18 : x <= 38 ? 17 : x <= 45 ? 16 : x <= 52 ? 15 : 16;
const SHAPE = new Map(); // "x,y" -> 1 rises to the right, 2 falls to the right
for (let x = 0; x < CW; x++) {
  const t = TOP[x], l = x > 0 ? TOP[x - 1] : t, r = x < CW - 1 ? TOP[x + 1] : t;
  const lLow = l > t, rLow = r > t; if (lLow && !rLow) SHAPE.set(x + ',' + t, 1); else if (rLow && !lLow) SHAPE.set(x + ',' + t, 2);
}
function solidPx(x, y) {
  if (y >= H) return true; if (y < 0) return false; x = clamp(x | 0, 0, W - 1);
  const cx = (x / C) | 0, cy = (y / C) | 0; if (cy < TOP[cx]) return false;
  const s = SHAPE.get(cx + ',' + cy), lx = x % C, ly = y % C;
  return s === 1 ? lx + ly >= 15 : s === 2 ? lx <= ly : true;
}
const SURF = new Int32Array(W); for (let x = 0; x < W; x++) { let y = 0; while (!solidPx(x, y)) y++; SURF[x] = y; }
const surfY = (x) => SURF[clamp(x | 0, 0, W - 1)];
const cellIsSlope = (x, y) => SHAPE.has(((x / C) | 0) + ',' + ((y / C) | 0));
// layout every panel shares
const SPOT = { tree: 200, bush: 284, hero: 456, crate: 500, rock: 690, sun: [790, 66], pond: [336, 400] };

// ---------- tree skeleton ----------
// returns segments (tapered capsules) and tips; angles in radians from straight up
function treeSkeleton(o) {
  const segs = [], tips = [];
  const R = (k) => hash(o.seed, k, 991);
  let n = 0;
  function grow(x, y, ang, len, w, depth) {
    const steps = Math.max(2, Math.round(len / (o.step || 10)));
    let cx = x, cy = y, a = ang, cw = w;
    for (let s = 0; s < steps; s++) {
      a += (R(n++) - 0.5) * (o.wiggle || 0.25) + (o.lift || 0) * (0 - a) * 0.15; // bend back toward up a little
      const sl = len / steps, nx = cx + Math.sin(a) * sl, ny = cy - Math.cos(a) * sl, nw = Math.max(o.minW || 1, cw * (o.taper || 0.88));
      segs.push({ x0: cx, y0: cy, x1: nx, y1: ny, w0: cw, w1: nw, depth });
      cx = nx; cy = ny; cw = nw;
      // side shoots along the trunk
      if (depth < o.depth && s >= (o.firstSplit != null && depth === 0 ? o.firstSplit : 1) && R(n++) < (depth === 0 ? (o.trunkShoot || 0.35) : (o.shoot || 0.3))) {
        const side = R(n++) < 0.5 ? -1 : 1;
        grow(cx, cy, a + side * (o.spread || 0.7) * (0.7 + R(n++) * 0.6), len * (o.childLen || 0.55) * (0.7 + R(n++) * 0.5), cw * (o.childW || 0.6), depth + 1);
      }
    }
    if (depth < o.depth) {
      const k = o.fork || 2;
      for (let i = 0; i < k; i++) {
        const sp = (i - (k - 1) / 2) * (o.spread || 0.7) * (1.4 / Math.max(1, k - 1)) + (R(n++) - 0.5) * 0.3;
        grow(cx, cy, a + sp, len * (o.childLen || 0.6) * (0.8 + R(n++) * 0.4), cw * (o.childW || 0.65), depth + 1);
      }
    } else tips.push({ x: cx, y: cy, a, depth });
  }
  grow(o.x, o.y, o.ang || 0, o.len, o.w, 0);
  return { segs, tips };
}
// rasterise segments into a buffer: per pixel the across-coordinate u in [-1,1], along v, depth
function rasterSegs(segs, w = W, h = H) {
  const U = new Float32Array(w * h).fill(NaN), V = new Float32Array(w * h), D = new Int8Array(w * h).fill(-1);
  let vacc = 0;
  for (const s of segs) {
    const dx = s.x1 - s.x0, dy = s.y1 - s.y0, L2 = dx * dx + dy * dy, L = Math.sqrt(L2) || 1, mw = Math.max(s.w0, s.w1) / 2 + 1;
    const x0 = Math.floor(Math.min(s.x0, s.x1) - mw), x1 = Math.ceil(Math.max(s.x0, s.x1) + mw), y0 = Math.floor(Math.min(s.y0, s.y1) - mw), y1 = Math.ceil(Math.max(s.y0, s.y1) + mw);
    for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
      if (x < 0 || y < 0 || x >= w || y >= h) continue;
      const px = x + 0.5 - s.x0, py = y + 0.5 - s.y0, t = clamp((px * dx + py * dy) / (L2 || 1));
      const qx = px - dx * t, qy = py - dy * t, d = Math.sqrt(qx * qx + qy * qy), hw = (s.w0 + (s.w1 - s.w0) * t) / 2;
      if (d > hw + 0.15) continue;
      const i = y * w + x;
      if (D[i] > s.depth) continue;
      U[i] = clamp((qx * -dy + qy * dx) / L / Math.max(0.6, hw), -1, 1); V[i] = vacc + t * L; D[i] = s.depth;
    }
    vacc += L;
  }
  return { U, V, D };
}
// foliage: clumps -> per-pixel front clump id, sphere normal and a leaf-cell normal
function foliage(clumps, o = {}) {
  const w = W, h = H, id = new Int16Array(w * h).fill(-1), N = new Float32Array(w * h * 3), Lf = new Float32Array(w * h * 3);
  const order = clumps.map((c, i) => i).sort((a, b) => (clumps[a].z || 0) - (clumps[b].z || 0));
  const lsz = o.leaf || 4;
  for (const k of order) {
    const c = clumps[k], R = c.r * 1.35;
    for (let y = Math.floor(c.y - R); y <= c.y + R; y++) for (let x = Math.floor(c.x - R); x <= c.x + R; x++) {
      if (x < 0 || y < 0 || x >= w || y >= h) continue;
      const dx = x + 0.5 - c.x, dy = (y + 0.5 - c.y) * (c.sq || 1), d = Math.sqrt(dx * dx + dy * dy), ang = Math.atan2(dy, dx);
      // ragged rim: big lobes plus leaf-sized notches
      const rim = c.r * (1 + (vnoise(ang * 3 + 10, k * 7, 1, 11) - 0.5) * (o.lobes || 0.35)) + (hash(Math.round(x / (lsz * 0.75)), Math.round(y / (lsz * 0.75)), k + 5) - 0.5) * (o.ragged || 3.5);
      if (d > rim) continue;
      if (o.holes && d < rim - 3 && vnoise(x, y, 5, 33 + k) < o.holes) continue;
      const i = y * w + x; id[i] = k;
      const nx = dx / rim, ny = dy / rim, nz = Math.sqrt(Math.max(0, 1 - nx * nx - ny * ny));
      N[i * 3] = nx; N[i * 3 + 1] = ny; N[i * 3 + 2] = nz;
    }
  }
  // leaf cells: nearest jittered point on a grid, each leaf a tiny dome
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const i = y * w + x; if (id[i] < 0) continue;
    const gx = Math.floor(x / lsz), gy = Math.floor(y / lsz); let bd = 1e9, bx = 0, by = 0, bd2 = 1e9;
    for (let j = -1; j <= 1; j++) for (let q = -1; q <= 1; q++) {
      const px = (gx + q + 0.15 + hash(gx + q, gy + j, 71) * 0.7) * lsz, py = (gy + j + 0.15 + hash(gx + q, gy + j, 72) * 0.7) * lsz;
      const d = (x + 0.5 - px) ** 2 + ((y + 0.5 - py) * 1.25) ** 2; if (d < bd) { bd2 = bd; bd = d; bx = px; by = py; } else if (d < bd2) bd2 = d;
    }
    const r = lsz * 0.75, lx = (x + 0.5 - bx) / r, ly = (y + 0.5 - by) / r;
    Lf[i * 3] = clamp(lx, -1, 1); Lf[i * 3 + 1] = clamp(ly, -1, 1); Lf[i * 3 + 2] = Math.sqrt(bd2) - Math.sqrt(bd); // gap: small = between leaves
  }
  return { id, N, Lf, clumps };
}
// light direction toward the sun (upper right), screen y down
const SUN = (() => { const v = [0.55, -0.62, 0.56], l = Math.hypot(...v); return v.map((a) => a / l); })();
const dot3 = (a, b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

// ---------- background helpers ----------
const ridge = (x, base, amp, cell, seed, oct = 4) => base - fbm(x, 0, cell, seed, oct) * amp;
// a treeline: many crowns of a given shape along a base line; returns top y per x
function treeline(base, o) {
  const top = new Float32Array(W).fill(1e9);
  for (let k = -2; k < W / o.gap + 2; k++) {
    const cx = k * o.gap + hash(k, 1, o.seed) * o.gap, hh = o.h * (0.6 + hash(k, 2, o.seed) * 0.6), ww = o.w * (0.7 + hash(k, 3, o.seed) * 0.5), by = base(cx);
    for (let x = Math.floor(cx - ww); x <= cx + ww; x++) {
      if (x < 0 || x >= W) continue; const t = Math.abs(x - cx) / ww; let y;
      if (o.kind === 'pine') y = by - hh * (1 - t) - (hash(x, k, o.seed) < 0.3 && t > 0.1 ? 1 : 0) - (Math.floor((1 - t) * 6) % 2 ? 0 : 1.5) * (t > 0.15 ? 1 : 0);
      else y = by - hh * 0.25 - Math.pow(Math.max(0, 1 - t * t), 0.6) * hh * 0.75 + (vnoise(x, k, 3, o.seed) - 0.5) * 2.5;
      top[x] = Math.min(top[x], y);
    }
  }
  return top;
}
function cloudField(x, y, o) { // returns density 0..1 for cumulus-like clouds
  const n = fbm(x * (o.sx || 1), y * (o.sy || 2.2), o.cell || 90, o.seed || 3, 5);
  const band = 1 - Math.abs((y - o.y) / o.th); return clamp((n - (o.cut || 0.5)) * 3.2) * clamp(band * 1.5);
}

// ---------- post ----------
function gblur(src, w, h, rad) {
  const tmp = new Float32Array(src.length), dst = new Float32Array(src.length), ws = []; let sum = 0;
  for (let k = -rad * 3; k <= rad * 3; k++) { const v = Math.exp(-(k * k) / (2 * rad * rad)); ws.push(v); sum += v; }
  const R = rad * 3; for (let k = 0; k < ws.length; k++) ws[k] /= sum;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) { let a = 0, b = 0, c = 0; for (let k = -R; k <= R; k++) { const xx = clamp(x + k, 0, w - 1), j = (y * w + xx) * 3, q = ws[k + R]; a += src[j] * q; b += src[j + 1] * q; c += src[j + 2] * q; } const o = (y * w + x) * 3; tmp[o] = a; tmp[o + 1] = b; tmp[o + 2] = c; }
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) { let a = 0, b = 0, c = 0; for (let k = -R; k <= R; k++) { const yy = clamp(y + k, 0, h - 1), j = (yy * w + x) * 3, q = ws[k + R]; a += tmp[j] * q; b += tmp[j + 1] * q; c += tmp[j + 2] * q; } const o = (y * w + x) * 3; dst[o] = a; dst[o + 1] = b; dst[o + 2] = c; }
  return dst;
}
// bloom from an emissive buffer (or from pixels brighter than a threshold)
function bloom(cv, emis, k1 = 0.8, k2 = 1.2, r1 = 2, r2 = 7) {
  if (!emis) { emis = new Float32Array(cv.c.length); for (let i = 0; i < cv.w * cv.h; i++) { const l = lum([cv.c[i * 3], cv.c[i * 3 + 1], cv.c[i * 3 + 2]]); if (l > 0.82) for (let q = 0; q < 3; q++) emis[i * 3 + q] = cv.c[i * 3 + q] * (l - 0.82) * 2; } }
  const a = gblur(emis, cv.w, cv.h, r1), b = gblur(emis, cv.w, cv.h, r2);
  for (let i = 0; i < cv.c.length; i++) cv.c[i] += a[i] * k1 + b[i] * k2;
}
function mote(cv, x, y, col, a = 1, big = false) { // a crisp luminous dot with a small halo, as the cave motes
  x = Math.round(x); y = Math.round(y); col = H_(col);
  cv.add(x, y, mix(col, [1, 1, 1], 0.45), a);
  for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) cv.add(x + dx, y + dy, col, a * 0.3);
  for (const [dx, dy] of [[1, 1], [-1, 1], [1, -1], [-1, -1], [2, 0], [-2, 0], [0, 2], [0, -2]]) cv.add(x + dx, y + dy, col, a * (big ? 0.16 : 0.08));
}
function glint(cv, x, y, col, a = 1, arm = 3) { cv.add(x, y, [1, 1, 1], a); for (let k = 1; k <= arm; k++) { const f = a * (k === 1 ? 0.7 : k === 2 ? 0.32 : 0.14); cv.add(x + k, y, col, f); cv.add(x - k, y, col, f); cv.add(x, y + k, col, f); cv.add(x, y - k, col, f); } }

// ---------- the game's own sprites, drawn unchanged ----------
const loadArt = (n) => P.decode(path.join(ART, n));
const HERO_PARTS = { hero_shin: [1, -11], hero_thigh: [3, -21], hero_torso: [1, -34], hero_upperArm: [5, -32], hero_forearm: [5, -25], hero_helmet: [0, -43] };
let ART_CACHE = null;
function art() { if (!ART_CACHE) { ART_CACHE = {}; for (const n of [...Object.keys(HERO_PARTS), 'machine_crate']) ART_CACHE[n] = loadArt(n + '.png'); } return ART_CACHE; }
function blit(cv, img, x0, y0, tag = 9) { for (let y = 0; y < img.h; y++) for (let x = 0; x < img.w; x++) { const o = (y * img.w + x) * 4; if (img.data[o + 3] < 128) continue; cv.set(x0 + x, y0 + y, [img.data[o] / 255, img.data[o + 1] / 255, img.data[o + 2] / 255], tag); } }
function heroAndCrate(cv) {
  const A = art(), fy = surfY(SPOT.hero + 6);
  for (const n of Object.keys(HERO_PARTS)) blit(cv, A[n], SPOT.hero + HERO_PARTS[n][0], fy + HERO_PARTS[n][1]);
  blit(cv, A.machine_crate, SPOT.crate, surfY(SPOT.crate + 16) - 16);
}
// a soft contact shadow under hero and crate so they sit on any ground
function contactShadow(cv, col, k = 0.35) {
  for (const [x0, x1] of [[SPOT.hero - 1, SPOT.hero + 13], [SPOT.crate - 2, SPOT.crate + 33]]) for (let x = x0; x <= x1; x++) { const y = surfY(x); const t = 1 - Math.abs((x - (x0 + x1) / 2) / ((x1 - x0) / 2 + 2)); cv.blend(x, y, col, k * t); cv.blend(x, y - 1, col, k * 0.4 * t); }
}

module.exports = { P, C, CW, CH, W, H, hex, H_, mix, mul, add3, clamp, lum, smooth, hash, vnoise, fbm, bayer, ramp, rampSmooth, nearest, Cv, Layer, TOP, SHAPE, solidPx, surfY, cellIsSlope, SPOT, treeSkeleton, rasterSegs, foliage, SUN, dot3, ridge, treeline, cloudField, gblur, bloom, mote, glint, heroAndCrate, contactShadow, loadArt };

// ---------- tree drawing shared by the styles ----------
// clumps around each tip (and optionally along branches); z orders them back to front
function clumpsFor(skel, o) {
  const out = []; let k = 0;
  for (const t of skel.tips) {
    const n = o.n || 4;
    out.push({ x: t.x + (o.dx || 0), y: t.y - (o.r || 12) * 0.2, r: (o.r || 12) * 1.15, z: -1, core: true, sq: o.sq });
    for (let i = 0; i < n; i++) {
      const a = hash(k, 1, o.seed) * Math.PI * 2, d = (o.spread || 10) * Math.sqrt(hash(k, 2, o.seed));
      out.push({ x: t.x + Math.cos(a) * d + (o.dx || 0), y: t.y + Math.sin(a) * d * 0.7 - (o.lift || 4), r: (o.r || 12) * (0.6 + hash(k, 3, o.seed) * 0.5), z: hash(k, 4, o.seed), sq: o.sq }); k++;
    }
  }
  if (o.extra) for (const c of o.extra) out.push(c);
  return out;
}
// paint a tree: branches through bark(x,y,u,v,depth), leaves through leaf(x,y,info) (info: n, leaf, gap, clump)
function paintTree(layer, skel, fol, bark, leaf) {
  const r = rasterSegs(skel.segs);
  for (let i = 0; i < W * H; i++) if (!Number.isNaN(r.U[i])) { const x = i % W, y = (i / W) | 0, c = bark(x, y, r.U[i], r.V[i], r.D[i]); if (c) layer.set(x, y, c); }
  if (fol) for (let i = 0; i < W * H; i++) if (fol.id[i] >= 0) {
    const x = i % W, y = (i / W) | 0, cl = fol.clumps[fol.id[i]];
    const c = leaf(x, y, { n: [fol.N[i * 3], fol.N[i * 3 + 1], fol.N[i * 3 + 2]], leaf: [fol.Lf[i * 3], fol.Lf[i * 3 + 1]], gap: fol.Lf[i * 3 + 2], clump: cl, k: fol.id[i] });
    if (c) layer.set(x, y, c);
  }
  return r;
}
// a standard light term for a foliage pixel: clump sphere + leaf dome + crevices
function leafLight(info, o = {}) {
  const s1 = dot3(info.n, SUN), lx = info.leaf[0], ly = info.leaf[1], lz = Math.sqrt(Math.max(0.05, 1 - lx * lx - ly * ly));
  const s2 = dot3([lx, ly, lz], SUN);
  let s = s1 * (o.clump || 0.65) + s2 * (o.leafK || 0.45) + (o.bias || 0.1);
  if (info.gap < (o.gapW || 0.8)) s -= o.gapK || 0.35;
  if (info.clump.core) s -= o.coreK || 0.25;
  return s;
}
// pixel depth below the surface at this x
const depthAt = (x, y) => y - surfY(x);
const airAt = (x, y) => !solidPx(x, y);
function edgeAir(x, y) { return airAt(x, y - 1) || airAt(x - 1, y) || airAt(x + 1, y) || airAt(x, y + 1); }
Object.assign(module.exports, { clumpsFor, paintTree, leafLight, depthAt, airAt, edgeAir });
// a cumulus cloud: puffs along a flat base; fn(x, y, s, t) paints (s: light 0..1 toward the sun, t: height 0 base..1 top)
function cumulus(cx, base, halfW, R, seed, fn, step = 1) {
  const puffs = [], n = Math.max(3, Math.round(halfW / (R * 0.55)));
  for (let k = 0; k <= n; k++) { const t = k / n * 2 - 1, r = R * (1 - t * t * 0.55) * (0.75 + hash(k, 1, seed) * 0.5); puffs.push({ x: cx + t * halfW * 0.85, y: base - r * 0.55 - (1 - t * t) * R * 0.25, r }); }
  for (let k = 0; k < 2; k++) puffs.push({ x: cx + (hash(k, 5, seed) - 0.5) * halfW * 0.6, y: base - R * 1.05, r: R * (0.7 + hash(k, 6, seed) * 0.3) });
  const top = Math.min(...puffs.map((p) => p.y - p.r));
  for (let y = Math.floor(top); y <= base; y += step) for (let x = Math.floor(cx - halfW - R); x <= cx + halfW + R; x += step) {
    let best = null, bd = 0;
    for (const p of puffs) { const dx = x + 0.5 - p.x, dy = y + 0.5 - p.y, d = Math.sqrt(dx * dx + dy * dy), e = p.r - d + (vnoise(x, y, 4, seed) - 0.5) * 2.5; if (e > 0 && (!best || p.y < best.y || e > bd + 6)) { best = p; bd = e; } }
    if (!best) continue;
    const dx = (x + 0.5 - best.x) / best.r, dy = (y + 0.5 - best.y) / best.r, nz = Math.sqrt(Math.max(0, 1 - dx * dx - dy * dy));
    const s = clamp(dot3([dx, dy, nz], SUN) * 0.75 + 0.35 - smooth(base - R * 0.5, base, y) * 0.3);
    fn(x, y, s, clamp((base - y) / (base - top)));
  }
}
module.exports.cumulus = cumulus;

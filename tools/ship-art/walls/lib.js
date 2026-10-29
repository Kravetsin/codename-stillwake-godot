// What every wall painter here shares: a 128x64 sheet that wraps round its edges, seeded Rng, wrapping noise, dithered
// ramps, the back wall's recession and the hull's edges. Colours are [r, g, b], or [r, g, b, a] where see-through.
const { encode } = require('../part-icons/png.js');

const W = 128, H = 64;
const mod = (a, n) => ((a % n) + n) % n;
const hex = (v) => [v >> 16, (v >> 8) & 255, v & 255];
const mix = (a, b, t) => a.map((v, i) => Math.round(v + (b[i] - v) * t));
const shade = (c, k) => c.map((v) => Math.max(0, Math.min(255, Math.round(v * k))));
const lum = (c) => 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2];

function rng(seed) {
  let s = seed >>> 0;
  return () => { s = (s + 0x6d2b79f5) >>> 0; let t = s; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
}

/** Value noise in [0, 1] that wraps on the sheet: its lattice of cx by cy pixels must divide W and H. */
function noise(r, cx, cy = cx) {
  const gw = W / cx, gh = H / cy, g = Array.from({ length: gw * gh }, r), s = (t) => t * t * (3 - 2 * t);
  const v = (i, j) => g[mod(j, gh) * gw + mod(i, gw)];
  return (x, y) => {
    const fx = mod(x, W) / cx, fy = mod(y, H) / cy, i = Math.floor(fx), j = Math.floor(fy), tx = s(fx - i), ty = s(fy - j);
    const a = v(i, j) + (v(i + 1, j) - v(i, j)) * tx, b = v(i, j + 1) + (v(i + 1, j + 1) - v(i, j + 1)) * tx;
    return a + (b - a) * ty;
  };
}

const BAYER = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5].map((v) => (v + 0.5) / 16);
/** A colour of ramp for t in [0, 1], dithered between its steps by `dither` (0 none, 1 full). */
const ramp = (pal, t, x, y, dither = 0.5) => {
  const f = Math.max(0, Math.min(0.999, t)) * pal.length + (BAYER[mod(y, 4) * 4 + mod(x, 4)] - 0.5) * dither;
  return pal[Math.max(0, Math.min(pal.length - 1, Math.floor(f)))];
};

class Sheet {
  constructor(w = W, h = H) { this.w = w; this.h = h; this.c = new Array(w * h).fill([255, 0, 255]); }
  i(x, y) { return mod(Math.round(y), this.h) * this.w + mod(Math.round(x), this.w); }
  set(x, y, c) { if (c) this.c[this.i(x, y)] = c; }
  get(x, y) { return this.c[this.i(x, y)]; }
  each(f) { for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) this.set(x, y, f(x, y)); return this; }
  rect(x0, y0, x1, y1, c) { for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) this.set(x, y, c); }
  map(f) { this.c = this.c.map(f); return this; }
  rgba() { const b = Buffer.alloc(this.w * this.h * 4); this.c.forEach((c, i) => { b[i * 4] = c[0]; b[i * 4 + 1] = c[1]; b[i * 4 + 2] = c[2]; b[i * 4 + 3] = c[3] ?? 255; }); return b; }
  png() { return encode(this.w, this.h, this.rgba()); }
}

/**
 * The back wall: lower in contrast, paler, cooler and darker, as the ground's back walls recede (GroundLooks: pale 0.3,
 * cool 0.6), darkened till its mean brightness is about `target`, near scrap's back wall, whatever the material's own.
 */
function recede(s, target = 64) {
  const mean = s.c.reduce((a, c) => a + lum(c), 0) / s.c.length, dim = Math.min(0.62, target / mean);
  const cs = [-0.14, -0.05, 0.16], k = cs.map((v) => (1 + v * 0.6) * dim);
  return s.map((c) => {
    const flat = c.slice(0, 3).map((v) => mean + (v - mean) * 0.75), g = lum(flat);
    return [...flat.map((v, i) => Math.max(0, Math.min(255, Math.round((v + (g - v) * 0.3) * k[i])))), ...c.slice(3)];
  });
}


/** Where each trim marks a side's rim, as positions along it, and in which colour: the accent or the outline. */
const TRIMS = {
  plain: [],
  rivet: [[4, 'accent']],
  notch: [[5, 'outline'], [2, 'accent']],
  stitch: [[2, 'outline'], [5, 'outline']],
  glint: [[3, 'accent']],
};

/**
 * The hull's edge of a wall on its sides open to air (open: 1 up, 2 right, 4 down, 8 left), drawn at the 8 px cell and
 * doubled to the 16 px the tileset halves: an outline, a lit rim up and left, a shadowed one down and right, the
 * material's trim on the rim. Returns { w, h, data } RGBA.
 */
function edge(open, look) {
  const g = new Array(64).fill(null), put = (x, y, c) => (g[y * 8 + x] = c), c = (k) => hex(look[k]);
  for (let i = 0; i < 8; i++) {
    if (open & 1) put(i, 1, c('lit'));
    if (open & 8) put(1, i, c('lit'));
    if (open & 2) put(6, i, c('shade'));
    if (open & 4) put(i, 6, c('shade'));
  }
  for (const [at, k] of TRIMS[look.trim || 'plain']) {
    if (open & 1) put(at, 1, c(k));
    if (open & 8) put(1, 7 - at, c(k));
    if (open & 2) put(6, at, c(k));
    if (open & 4) put(7 - at, 6, c(k));
  }
  for (let i = 0; i < 8; i++) {
    if (open & 1) put(i, 0, c('outline'));
    if (open & 8) put(0, i, c('outline'));
    if (open & 2) put(7, i, c('outline'));
    if (open & 4) put(i, 7, c('outline'));
  }
  const data = Buffer.alloc(16 * 16 * 4);
  for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
    const p = g[(y >> 1) * 8 + (x >> 1)], o = (y * 16 + x) * 4;
    if (p) { data[o] = p[0]; data[o + 1] = p[1]; data[o + 2] = p[2]; data[o + 3] = 255; }
  }
  return { w: 16, h: 16, data };
}

module.exports = { W, H, mod, hex, mix, shade, lum, rng, noise, ramp, Sheet, recede, edge };

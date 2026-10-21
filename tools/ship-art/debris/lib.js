// A picture is a grid of (material, slot) pairs: a material is a 6-step ramp from the style
// (0 deep, 1 dark, 2 shade, 3 base, 4 light, 5 highlight), so one drawing renders in every style.
// Light comes from the upper left, as on all of the game's art; the outline is added on render.
const { encode } = require('./png.js');

const hex = c => [parseInt(c.slice(1, 3), 16), parseInt(c.slice(3, 5), 16), parseInt(c.slice(5, 7), 16)];
const clamp = (v, a, b) => (v < a ? a : v > b ? b : v);
const L = (() => { const v = [-0.62, -0.78, 0.9], n = Math.hypot(...v); return v.map(x => x / n); })();

function hash(x, y, s) { let h = (x * 374761393 + y * 668265263 + s * 2246822519) >>> 0; h = ((h ^ (h >>> 13)) * 1274126177) >>> 0; return ((h ^ (h >>> 16)) >>> 0) / 4294967296; }
function vnoise(x, y, s) {
  const xi = Math.floor(x), yi = Math.floor(y), fx = x - xi, fy = y - yi, u = fx * fx * (3 - 2 * fx), v = fy * fy * (3 - 2 * fy);
  const a = hash(xi, yi, s), b = hash(xi + 1, yi, s), c = hash(xi, yi + 1, s), d = hash(xi + 1, yi + 1, s);
  return (a * (1 - u) + b * u) * (1 - v) + (c * (1 - u) + d * u) * v - 0.5;
}

class Pic {
  constructor(w, h, style, { flip = false, seed = 1 } = {}) {
    this.w = w; this.h = h; this.st = style; this.flip = flip; this.seed = seed;
    this.m = new Array(w * h).fill(null); this.k = new Int8Array(w * h); this.over = [];
  }
  X(x) { return this.flip ? this.w - 1 - x : x; }
  in(x, y) { return x >= 0 && y >= 0 && x < this.w && y < this.h; }
  // raw (output-space) access
  get(x, y) { return this.in(x, y) ? this.m[y * this.w + x] : null; }
  slot(x, y) { return this.k[y * this.w + x]; }
  setRaw(x, y, mat, k) { if (!this.in(x, y)) return; const i = y * this.w + x; this.m[i] = mat; if (k !== undefined) this.k[i] = clamp(Math.round(k), 0, 5); }
  // design-space drawing (x flipped when the picture is mirrored)
  px(x, y, mat, k) { this.setRaw(this.X(Math.round(x)), Math.round(y), mat, k); }
  clear(x, y) { x = this.X(Math.round(x)); y = Math.round(y); if (this.in(x, y)) this.m[y * this.w + x] = null; }
  has(x, y) { return this.get(this.X(Math.round(x)), Math.round(y)) !== null; }
  matAt(x, y) { return this.get(this.X(Math.round(x)), Math.round(y)); }
  slotAt(x, y) { const X = this.X(Math.round(x)); return this.in(X, y) ? this.k[Math.round(y) * this.w + X] : 0; }
  rect(x0, y0, x1, y1, mat, k) { for (let y = Math.min(y0, y1); y <= Math.max(y0, y1); y++) for (let x = Math.min(x0, x1); x <= Math.max(x0, x1); x++) this.px(x, y, mat, k); }
  // all pixels whose centre is inside a polygon, in design space
  polyPts(pts) {
    const out = []; let y0 = Infinity, y1 = -Infinity, x0 = Infinity, x1 = -Infinity;
    for (const [x, y] of pts) { x0 = Math.min(x0, x); x1 = Math.max(x1, x); y0 = Math.min(y0, y); y1 = Math.max(y1, y); }
    for (let y = Math.floor(y0); y <= Math.ceil(y1); y++) for (let x = Math.floor(x0); x <= Math.ceil(x1); x++) {
      let inside = false; const px = x + 0.5, py = y + 0.5;
      for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
        const [xi, yi] = pts[i], [xj, yj] = pts[j];
        if ((yi > py) !== (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi) inside = !inside;
      }
      if (inside) out.push([x, y]);
    }
    return out;
  }
  poly(pts, mat, k) { for (const [x, y] of this.polyPts(pts)) this.px(x, y, mat, typeof k === 'function' ? k(x, y) : k); }
  erasePoly(pts) { for (const [x, y] of this.polyPts(pts)) this.clear(x, y); }
  ell(cx, cy, rx, ry, mat, k) {
    for (let y = Math.floor(cy - ry - 1); y <= cy + ry + 1; y++) for (let x = Math.floor(cx - rx - 1); x <= cx + rx + 1; x++) {
      const dx = (x + 0.5 - cx) / rx, dy = (y + 0.5 - cy) / ry; if (dx * dx + dy * dy <= 1) this.px(x, y, mat, typeof k === 'function' ? k(x, y, dx, dy) : k);
    }
  }
  line(x0, y0, x1, y1, mat, k) {
    x0 = Math.round(x0); y0 = Math.round(y0); x1 = Math.round(x1); y1 = Math.round(y1);
    const dx = Math.abs(x1 - x0), dy = -Math.abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1; let e = dx + dy; const pts = [];
    for (;;) { pts.push([x0, y0]); if (x0 === x1 && y0 === y1) break; const e2 = 2 * e; if (e2 >= dy) { e += dy; x0 += sx; } if (e2 <= dx) { e += dx; y0 += sy; } }
    if (mat !== undefined) for (const [x, y] of pts) this.px(x, y, mat, typeof k === 'function' ? k(x, y) : k);
    return pts;
  }
  path(pts, mat, k) { const all = []; for (let i = 1; i < pts.length; i++) { const seg = this.line(...pts[i - 1], ...pts[i]); if (i > 1) seg.shift(); all.push(...seg); } if (mat !== undefined) for (const [x, y] of all) this.px(x, y, mat, typeof k === 'function' ? k(x, y) : k); return all; }
  // light value 0..1 to a ramp slot, by the style's thresholds
  q(s) { s += this.st.shadeBias || 0; const t = this.st.th; let k = 0; while (k < t.length && s > t[k]) k++; return k; }

  // ---- shaded solids (shading computed in output space so mirrored pictures stay lit from the left) ----
  sphere(cx, cy, r, mat, { gain = 1, bias = 0 } = {}) {
    this.ell(cx, cy, r, r, mat, (x, y, dx, dy) => {
      if (this.flip) dx = -dx; const nz = Math.sqrt(Math.max(0, 1 - dx * dx - dy * dy));
      const d = dx * L[0] + dy * L[1] + nz * L[2]; return this.q(clamp(0.15 + 0.85 * d * gain + bias, 0, 1));
    });
  }
  // a cylinder lying along x (shade varies with y) or standing along y (shade varies with x)
  cyl(x0, y0, x1, y1, mat, axis, { bias = 0 } = {}) {
    const [a0, a1] = axis === 'x' ? [y0, y1] : [x0, x1];
    for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
      const t = axis === 'x' ? y : x; let u = ((t + 0.5 - a0) / (a1 - a0 + 1)) * 2 - 1;
      if (axis !== 'x' && this.flip) u = -u;
      const n = [axis === 'x' ? 0 : u, axis === 'x' ? u : 0, Math.sqrt(Math.max(0, 1 - u * u))];
      this.px(x, y, mat, this.q(clamp(0.18 + 0.82 * (n[0] * L[0] + n[1] * L[1] + n[2] * L[2]) + bias, 0, 1)));
    }
  }
  // a flat plate: base slot, lit top and left edge, dark bottom and right edge (in output space)
  panel(x0, y0, x1, y1, mat, { base = 3, rivets = 0, inset = 2 } = {}) {
    base = Math.max(1, Math.min(4, base + (this.st.panelShift || 0)));
    for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) this.px(x, y, mat, base);
    const L0 = this.flip ? x1 : x0, R0 = this.flip ? x0 : x1;
    for (let x = x0; x <= x1; x++) { this.px(x, y0, mat, base + 1); this.px(x, y1, mat, base - 1); }
    for (let y = y0; y <= y1; y++) { this.px(L0, y, mat, base + 1); this.px(R0, y, mat, base - 1); }
    this.px(L0, y1, mat, base); this.px(R0, y0, mat, base);
    if (rivets) for (const [x, y] of [[x0 + inset, y0 + inset], [x1 - inset, y0 + inset], [x0 + inset, y1 - inset], [x1 - inset, y1 - inset]]) this.rivet(x, y, mat);
  }
  rivet(x, y, mat) { this.px(x, y, mat, 5); }
  // a seam: dark line with a lit line on its lower or right side (as cracks and panel joints read)
  seamH(x0, x1, y, mat, k = 1) { for (let x = x0; x <= x1; x++) { this.px(x, y, mat, k); if (this.has(x, y + 1) && this.matAt(x, y + 1) === mat) this.px(x, y + 1, mat, Math.min(5, this.slotAt(x, y + 1) + 1)); } }
  seamV(x, y0, y1, mat, k = 1) { const r = this.flip ? -1 : 1; for (let y = y0; y <= y1; y++) { this.px(x, y, mat, k); if (this.has(x + r, y) && this.matAt(x + r, y) === mat) this.px(x + r, y, mat, Math.min(5, this.slotAt(x + r, y) + 1)); } }
  stripes(x0, y0, x1, y1, { w = 2, dir = 1 } = {}) {
    for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
      const on = Math.floor((x + dir * y + 64) / w) % 2 === 0;
      this.px(x, y, on ? 'yellow' : 'ink', on ? (y === y0 ? 4 : 3) : 0);
    }
  }
  // the pixels of this picture with a given material (output space)
  each(fn) { for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) { const m = this.m[y * this.w + x]; if (m !== null) fn(x, y, m, this.k[y * this.w + x]); } }

  // lone pixels of one slot among four agreeing neighbours of another take the neighbours' slot
  tidy(mats) {
    const ch = [];
    this.each((x, y, m, k) => {
      if (mats && !mats.includes(m)) return;
      const ns = [[1, 0], [-1, 0], [0, 1], [0, -1]].map(([dx, dy]) => (this.get(x + dx, y + dy) === m ? this.k[(y + dy) * this.w + x + dx] : null));
      if (ns.some(n => n === null)) return;
      if (ns.every(n => n !== k) && ns.filter(n => n === ns[0]).length >= 3) ch.push([x, y, ns[0]]);
      else if (ns.every(n => n !== k)) { const avg = Math.round(ns.reduce((a, b) => a + b) / 4); ch.push([x, y, avg]); }
    });
    for (const [x, y, k] of ch) this.k[y * this.w + x] = k;
  }
  // cel lines: where two parts meet, the lower or right one gets an ink edge (style C)
  inkSeams(thin) {
    const ch = [];
    this.each((x, y, m) => {
      if (m === 'ink' || m === 'void' || thin.includes(m)) return;
      for (const [dx, dy] of [[-1, 0], [0, -1]]) { const n = this.get(x + dx, y + dy); if (n && n !== m && n !== 'ink' && n !== 'void' && !thin.includes(n)) { ch.push([x, y]); break; } }
    });
    for (const [x, y] of ch) this.m[y * this.w + x] = 'ink';
  }
  // single pixels floating free in space go
  despeck() { const gone = []; this.each((x, y) => { if (![[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => this.get(x + dx, y + dy) !== null)) gone.push(y * this.w + x); }); for (const i of gone) this.m[i] = null; }
  // the edge facing the light gets the style's rim
  rim(mats, { k = 5, min = 2, dark = false, skip = [] } = {}) {
    const ch = [];
    this.each((x, y, m, s) => {
      if (mats && !mats.includes(m)) return; if (skip.includes(m)) return;
      const up = this.get(x, y - 1) === null, lf = this.get(x - 1, y) === null, ul = this.get(x - 1, y - 1) === null;
      if ((up || lf) && s >= min) ch.push([x, y, k]);
      else if (ul && s >= min && (this.get(x, y - 2) === null || this.get(x - 2, y) === null)) ch.push([x, y, k]);
      if (dark) { const dn = this.get(x, y + 1) === null, rt = this.get(x + 1, y) === null; if ((dn || rt) && !(up || lf)) ch.push([x, y, Math.max(0, Math.min(s, 1))]); }
    });
    for (const [x, y, k] of ch) this.k[y * this.w + x] = k;
  }

  render() {
    const { w, h } = this, st = this.st, data = new Uint8Array(w * h * 4);
    const ink = hex(st.ink);
    for (let i = 0; i < w * h; i++) {
      const m = this.m[i]; if (m === null) continue;
      const ramp = st.mats[m]; if (!ramp) throw new Error('no material ' + m + ' in style ' + st.id);
      const c = hex(ramp[this.k[i]]); data.set([...c, 255], i * 4);
    }
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
      if (this.m[y * w + x] !== null) continue;
      const near = [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => this.get(x + dx, y + dy) !== null);
      if (near) data.set([...ink, 255], (y * w + x) * 4);
    }
    for (const [x, y, c] of this.over) if (this.in(x, y)) data.set([...hex(c), 255], (y * w + x) * 4);
    // nothing may touch the picture's border: the outline must fit inside it
    for (let x = 0; x < w; x++) for (const y of [0, h - 1]) if (this.m[y * w + x] !== null) throw new Error(`${this.name}: fill on the border at ${x},${y}`);
    for (let y = 0; y < h; y++) for (const x of [0, w - 1]) if (this.m[y * w + x] !== null) throw new Error(`${this.name}: fill on the border at ${x},${y}`);
    return { w, h, data };
  }
  png() { const im = this.render(); return encode(im.w, im.h, im.data); }
}

// ---------- rocks: a hand-drawn silhouette lit as a body of flat facets, craters cut in ----------
// o: { pts, R, craters: [[x,y,r,depth]], bumps: [[x,y,r,h]], mat, facets }
function rock(p, o) {
  const st = p.st, mat = o.mat || 'rock';
  const cells = p.polyPts(o.pts).map(([x, y]) => [p.X(x), y]); // output space from here on
  const inside = new Uint8Array(p.w * p.h); for (const [x, y] of cells) inside[y * p.w + x] = 1;
  const isIn = (x, y) => x >= 0 && y >= 0 && x < p.w && y < p.h && inside[y * p.w + x];
  const edge = []; for (let y = -1; y <= p.h; y++) for (let x = -1; x <= p.w; x++)
    if (!isIn(x, y) && [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => isIn(x + dx, y + dy))) edge.push([x, y]);
  const R = o.R, F = x => (p.flip ? p.w - 1 - x : x);
  let cx = 0, cy = 0; for (const [x, y] of cells) { cx += x; cy += y; } cx /= cells.length; cy /= cells.length;
  // the body's height: a dome by distance from the edge, with knobs
  const H = new Float32Array(p.w * p.h);
  for (const [x, y] of cells) {
    let d = Infinity; for (const [ex, ey] of edge) d = Math.min(d, Math.hypot(ex - x, ey - y));
    const dd = Math.min(d - 0.5, R); let h = Math.sqrt(Math.max(0, R * R - (R - dd) * (R - dd)));
    for (const [bx, by, br, bh] of o.bumps || []) { const q = Math.hypot(x - F(bx), y - by) / br; if (q < 1) h += bh * (1 - q * q) * (1 - q * q); }
    H[y * p.w + x] = h;
  }
  const at = (x, y) => (isIn(x, y) ? H[y * p.w + x] : -1.2);
  const normal = (x, y) => { const hx = (at(x + 1, y) - at(x - 1, y)) / 2, hy = (at(x, y + 1) - at(x, y - 1)) / 2; const n = [-hx * st.relief, -hy * st.relief, 1], l = Math.hypot(...n); return n.map(v => v / l); };
  // facets: seeded points, each a flat plane tilted like the body under it, a little askew
  const area = cells.length, nF = Math.max(4, Math.round(area / (st.chunky ? 70 : 34)));
  const seeds = []; let tries = 0; const minD = Math.sqrt(area / nF) * 0.8;
  while (seeds.length < nF && tries++ < 4000) {
    const c = cells[Math.floor(hash(tries, seeds.length, p.seed) * cells.length)];
    if (seeds.every(s => Math.hypot(s[0] - c[0], s[1] - c[1]) >= minD)) {
      const n = normal(c[0], c[1]), j = st.facetTilt;
      const t = [n[0] + (hash(c[0], c[1], p.seed + 3) - 0.5) * j, n[1] + (hash(c[1], c[0], p.seed + 5) - 0.5) * j, n[2]], l = Math.hypot(...t);
      seeds.push([c[0], c[1], t.map(v => v / l)]);
    }
  }
  const planesIn = (o.planes || []).map(([pts, n]) => ({ n, set: new Set(p.polyPts(pts).map(([x, y]) => y * p.w + p.X(x))) }));
  const span = Math.max(p.w, p.h) * 0.5, dot = n => n[0] * L[0] + n[1] * L[1] + n[2] * L[2];
  for (const [x, y] of cells) {
    let best = null, bd = Infinity; for (const s of seeds) { const d = (s[0] - x) ** 2 + (s[1] - y) ** 2; if (d < bd) { bd = d; best = s; } }
    const ns = normal(x, y), nf = best[2], w = st.facetMix;
    let inBowl = false; let n = [ns[0] * (1 - w) + nf[0] * w, ns[1] * (1 - w) + nf[1] * w, ns[2] * (1 - w) + nf[2] * w];
    // craters: a bowl whose far (lower right) wall catches the light, a raised lip round it
    for (const [kx, ky, kr] of o.craters || []) {
      const dx = (x + 0.5 - F(kx) - 0.5) / kr, dy = (y + 0.5 - ky - 0.5) / (kr * 0.9), q = Math.hypot(dx, dy);
      if (q < 1) { n = [-dx * 1.3 + n[0] * 0.3, -dy * 1.3 + n[1] * 0.3, 1]; inBowl = true; }
      else if (q < 1.45) { const f = 0.9 * (1 - (q - 1) / 0.45); n = [n[0] + dx / q * f, n[1] + dy / q * f, n[2]]; }
    }
    for (const pl of planesIn) if (pl.set.has(y * p.w + x)) n = [pl.n[0] * (p.flip ? -1 : 1), pl.n[1], 1];
    const l = Math.hypot(...n); n = n.map(v => v / l);
    let s = 0.5 + 0.6 * (dot(n) - 0.55) / 0.45 + st.bodyGrad * (((cx - x) * 0.62 + (cy - y) * 0.78) / span);
    if (inBowl) s -= 0.14;
    p.setRaw(x, y, mat, Math.min(st.maxShade ?? 5, p.q(clamp(s + st.rockBias, 0, 1))));
  }
  // the floor of a crater lies in its up-left wall's shadow
  for (const [kx, ky, kr] of o.craters || []) for (const [x, y] of cells) {
    const dx = x + 0.5 - F(kx) - 0.5, dy = y + 0.5 - ky - 0.5, q = Math.hypot(dx, dy / 0.9) / kr;
    if (q < 0.8 && dx * 0.62 + dy * 0.78 < kr * 0.1) p.k[y * p.w + x] = Math.min(p.k[y * p.w + x], st.craterFloor + (q > 0.55 ? 1 : 0));
  }
  p.tidy([mat]); p.tidy([mat]);
  return { cells, cx, cy };
}

// a crack in design space: dark line, its lower lip lit (the shader's own crack style)
function crack(p, pts, { w = 1, mat } = {}) {
  const line = p.path(pts);
  for (const [x, y] of line) for (let i = 0; i < w; i++) {
    const X = x, Y = y + i; if (!p.has(X, Y)) continue; const m = mat || p.matAt(X, Y);
    p.px(X, Y, m, 0);
  }
  for (const [x, y] of line) { const Y = y + w; if (p.has(x, Y) && !line.some(([a, b]) => a === x && b === Y) && p.slotAt(x, Y) > 0) p.px(x, Y, p.matAt(x, Y), Math.min(5, p.slotAt(x, Y) + 1)); }
}

// ore: veins follow the rock's own shading a step brighter; nuggets; glints
function wiggle(pts, seed) {
  const out = [pts[0]];
  for (let i = 1; i < pts.length; i++) {
    const [x0, y0] = pts[i - 1], [x1, y1] = pts[i], len = Math.hypot(x1 - x0, y1 - y0), n = Math.max(1, Math.round(len / 3));
    for (let j = 1; j <= n; j++) { const t = j / n, o = j === n ? 0 : Math.round((hash(i, j, seed) - 0.5) * 2.4); out.push([Math.round(x0 + (x1 - x0) * t - (y1 - y0) / len * o), Math.round(y0 + (y1 - y0) * t + (x1 - x0) / len * o)]); }
  }
  return out;
}
function vein(p, pts, { w = 1 } = {}) {
  const line = p.path(wiggle(pts, pts[0][0] * 7 + pts[0][1]));
  for (const [x, y] of line) for (let i = 0; i < w; i++) {
    if (!p.has(x, y + i)) continue; const k = p.slotAt(x, y + i);
    p.px(x, y + i, 'ore', clamp(k + 2 - i, 2, 4));
  }
  // the vein sits in a groove: a dark pixel under it where rock follows
  for (const [x, y] of line) { const Y = y + w; if (p.has(x, Y) && p.matAt(x, Y) !== 'ore' && p.slotAt(x, Y) > 1) p.px(x, Y, p.matAt(x, Y), Math.max(0, p.slotAt(x, Y) - 2)); }
}
function nugget(p, x, y, s = 2) {
  for (let j = 0; j < s; j++) for (let i = 0; i < s; i++) p.px(x + i, y + j, 'ore', 3);
  if (s >= 3) { p.px(x + 1, y - 1, 'ore', 3); p.px(x - 1, y + 1, 'ore', 2); p.px(x + s - 1, y + s - 1, 'ore', 1); p.px(x + s - 2, y + s - 1, 'ore', 2); }
  else p.px(x + s - 1, y + s - 1, 'ore', 1);
  p.px(x, y, 'ore', 5); if (s >= 3) p.px(x + 1, y, 'ore', 4);
}
function glint(p, x, y, r = 2) {
  // drawn over the outline, so a glint may cross the rock's edge into space
  const g = p.st.glint, X = p.X(x), put = (a, b, c) => p.over.push([a, b, c]);
  put(X, y, g[0]);
  for (let i = 1; i <= r; i++) { const c = i === 1 ? g[1] : g[2]; put(X + i, y, c); put(X - i, y, c); put(X, y + i, c); put(X, y - i, c); }
}

module.exports = { wiggle, Pic, rock, crack, vein, nugget, glint, clamp, hash, vnoise, L };

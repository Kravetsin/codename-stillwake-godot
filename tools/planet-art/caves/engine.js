// Cave renderer: cells of 16 px, front tiles with autoshaped slopes, back wall, decor objects,
// per-pixel light with occlusion (helmet cone + point glows), emissive bloom, beam glints, luminous motes.
const P = require('../png'), path = require('path');
const ART = path.join(__dirname, '..', '..', '..', 'game', 'art');
const C = 16;

const hex = (h) => [parseInt(h.slice(1, 3), 16) / 255, parseInt(h.slice(3, 5), 16) / 255, parseInt(h.slice(5, 7), 16) / 255];
const mix = (a, b, t) => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];
const mul = (a, k) => Array.isArray(k) ? [a[0] * k[0], a[1] * k[1], a[2] * k[2]] : [a[0] * k, a[1] * k, a[2] * k];
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
const loadArt = (n) => P.decode(path.join(ART, n));

// ---------- materials ----------
const MP = {
  soil: { a: hex('#46332a'), b: hex('#543e30'), dk: hex('#35271f'), lt: hex('#644a37'), ol: hex('#24190f'), hi: hex('#6a523c') },
  rock: { a: hex('#525764'), b: hex('#5e6472'), dk: hex('#434753'), lt: hex('#737a88'), ol: hex('#22242b'), hi: hex('#636874') },
  cold: { a: hex('#55606e'), b: hex('#606d7c'), dk: hex('#424c59'), lt: hex('#7d8a9a'), ol: hex('#20262e'), hi: hex('#6c7888') },
  deep: { a: hex('#4b4858'), b: hex('#56526a'), dk: hex('#3a3846'), lt: hex('#6a6680'), ol: hex('#1f1d27'), hi: hex('#5f5b72') },
  basalt: { a: hex('#4a3a36'), b: hex('#56423b'), dk: hex('#352725'), lt: hex('#6e4e44'), ol: hex('#1e1514'), hi: hex('#5e463e') },
  bedrock: { a: hex('#24232a'), b: hex('#2b2a32'), dk: hex('#1a191f'), lt: hex('#36343e'), ol: hex('#111015'), hi: hex('#302e37') },
};
const ORE = {
  copper: { v: hex('#b9783a'), vl: hex('#e3a656'), vd: hex('#7a4a26'), glint: hex('#ffd08a') },
  ice: { v: hex('#9cc8de'), vl: hex('#d8f2ff'), vd: hex('#5e86a0'), glint: hex('#e8faff') },
  gold: { v: hex('#b8963a'), vl: hex('#f0d070'), vd: hex('#7a6226'), glint: hex('#fff0b0') },
};
function baseColor(m, x, y) {
  const p = MP[m] || MP.rock;
  if (m === 'soil') {
    let c = mix(p.a, p.b, vnoise(x, y, 7, 1)); const h = hash(x, y, 2);
    if (h < 0.07) c = p.dk; else if (h > 0.955) c = p.lt;
    const bx = Math.floor(x / 5), by = Math.floor(y / 5);
    if (hash(bx, by, 3) < 0.09) { const ox = bx * 5 + 1 + Math.floor(hash(bx, by, 4) * 2), oy = by * 5 + 1;
      if (x >= ox && x < ox + 2 && y >= oy && y < oy + 2) c = (x === ox && y === oy) ? hex('#6b6359') : hex('#4e4943'); }
    return c;
  }
  let c = mix(p.a, p.b, vnoise(x, y, 9, 4));
  const strata = ((y + 4 * vnoise(x, y, 14, 5)) % 9 + 9) % 9;
  if (strata < 0.9) c = p.dk;
  const h = hash(x, y, 6);
  if (h < 0.05) c = p.dk; else if (h > 0.965) c = p.lt;
  if (m === 'basalt' && hash(x, y, 7) < 0.012) c = hex('#8a3a22');
  return c;
}

// ---------- shapes (as in the ruin study) ----------
function inShape(s, x, y) {
  switch (s) { case 1: return x + y >= 15; case 2: return x <= y; case 3: return x >= y; case 4: return x + y <= 15;
    case 5: return y >= 8; case 6: return y < 8; default: return true; }
}

// ---------- world ----------
class World {
  constructor(W, H) {
    Object.assign(this, { W, H, PW: W * C, PH: H * C });
    this.F = new Array(W * H).fill('rock'); this.B = new Array(W * H).fill('earth'); this.ore = new Array(W * H).fill(null);
    this.backMat = null; // optional fn(cx, cy) -> material for the back wall
  }
  i(x, y) { return y * this.W + x; }
  solidCell(x, y) { return x < 0 || y < 0 || x >= this.W || y >= this.H ? true : !!this.F[this.i(x, y)]; }
  open(x, y) { if (x >= 0 && y >= 0 && x < this.W && y < this.H) this.F[this.i(x, y)] = null; }
  shapes() {
    const { W, H } = this; this.S = new Array(W * H).fill(0);
    for (let y = 1; y < H - 1; y++) for (let x = 1; x < W - 1; x++) {
      const m = this.F[this.i(x, y)]; if (!m || m === 'bedrock' || m === 'geode') continue;
      const U = !this.solidCell(x, y - 1), D = !this.solidCell(x, y + 1), L = !this.solidCell(x - 1, y), R = !this.solidCell(x + 1, y);
      let s = 0;
      if (U && L && !R && !D) s = 1; else if (U && R && !L && !D) s = 2; else if (D && L && !R && !U) s = 3; else if (D && R && !L && !U) s = 4;
      else if (U && L && R && !D) s = 5; else if (D && L && R && !U) s = 6;
      this.S[this.i(x, y)] = s;
    }
    const { PW, PH } = this; this.solid = new Uint8Array(PW * PH);
    for (let y = 0; y < PH; y++) for (let x = 0; x < PW; x++) {
      const c = this.i((x / C) | 0, (y / C) | 0); if (this.F[c] && inShape(this.S[c], x % C, y % C)) this.solid[y * PW + x] = 1;
    }
  }
  sp(x, y) { return x < 0 || y < 0 || x >= this.PW || y >= this.PH ? 1 : this.solid[y * this.PW + x]; }
}

// ---------- the picture ----------
class Pic {
  constructor(w) {
    this.w = w; const n = w.PW * w.PH;
    this.alb = new Float32Array(n * 3); this.emis = new Float32Array(n * 3); this.isE = new Uint8Array(n);
    this.back = new Uint8Array(n); this.water = new Uint8Array(n); this.glint = []; this.motes = []; this.drips = []; this.lights = [];
    this.glintAt = new Map(); this.waterTop = new Int32Array(w.PW).fill(-1); this.unlit = new Uint8Array(n);
    this.emitters = []; // for the page's live overlay
    this.nob = new Uint8Array(n); // emissive pixels that glow but do not bloom (fine detail such as a hologram)
  }
  inb(x, y) { return x >= 0 && y >= 0 && x < this.w.PW && y < this.w.PH; }
  set(x, y, c) { x |= 0; y |= 0; if (!this.inb(x, y)) return; const i = y * this.w.PW + x; this.alb.set(c, i * 3); this.isE[i] = 0; this.back[i] = 0; }
  glow(x, y, c, k = 1) { x |= 0; y |= 0; if (!this.inb(x, y)) return; const i = y * this.w.PW + x; this.alb.set(c, i * 3); this.emis.set(mul(c, k), i * 3); this.isE[i] = 1; this.back[i] = 0; }
  get(x, y) { const i = (y * this.w.PW + x) * 3; return [this.alb[i], this.alb[i + 1], this.alb[i + 2]]; }
  solid(x, y) { return this.w.sp(x, y); }
  light(l) { this.lights.push(l); }
}

function paintTerrain(pic) {
  const w = pic.w, { PW, PH } = w;
  const matAtPx = (x, y) => w.F[w.i((x / C) | 0, (y / C) | 0)];
  for (let y = 0; y < PH; y++) for (let x = 0; x < PW; x++) {
    const i = y * PW + x, cx = (x / C) | 0, cy = (y / C) | 0;
    if (!w.solid[i]) {
      const b = w.B[w.i(cx, cy)];
      if (b === null) { pic.alb.set(w.sky ? w.sky(x, y) : [0, 0, 0], i * 3); pic.unlit[i] = 1; continue; }
      let m = w.backMat ? w.backMat(cx, cy, x, y) : (w.F[w.i(cx, cy)] || 'rock');
      if (m === 'geode' || !MP[m]) m = 'rock';
      let c = mul(baseColor(m, x, y), [0.42, 0.42, 0.5]);
      if (w.sp(x - 1, y - 1) || w.sp(x, y - 1) || w.sp(x - 1, y)) c = mul(c, 0.55);
      else if (w.sp(x - 2, y - 2) || w.sp(x, y - 2) || w.sp(x - 2, y)) c = mul(c, 0.78);
      pic.alb.set(c, i * 3); pic.back[i] = 1; continue;
    }
    let m = matAtPx(x, y); const ore = w.ore[w.i(cx, cy)];
    const pm = m === 'geode' ? 'rock' : m;
    let c = baseColor(pm, x, y);
    if (m === 'geode') {
      const lx = x % C - 7.5, ly = y % C - 7.5, r = Math.sqrt(lx * lx + ly * ly);
      if (r < 7.2) c = r > 6 ? hex('#2c2a36') : r > 5 ? mix(hex('#6d6a7e'), hex('#8b87a0'), hash(x, y, 9)) : mix(hex('#4a4760'), hex('#5a5674'), vnoise(x, y, 3, 8));
      if (r < 5 && hash(x, y, 31) < 0.06) pic.glint.push({ x, y, col: w.geodeGlint || hex('#f0d8ff'), need: 0.25, size: 1 });
    }
    if (ore) {
      const o = ORE[ore], n = vnoise(x, y, 3, 7 + cx * 3);
      if (n > 0.7) { c = n > 0.84 ? o.vl : o.v; if (hash(x, y, 33) < 0.07) pic.glint.push({ x, y, col: o.glint, need: 0.22, size: n > 0.8 ? 2 : 1 }); }
      else if (n > 0.65) c = o.vd;
    } else if (pm !== 'soil' && hash(x, y, 34) < (w.mica || 0)) pic.glint.push({ x, y, col: hex('#f4f0ff'), need: 0.42, size: 1, dim: 0.6 });
    const p = MP[pm] || MP.rock;
    const airU = !w.sp(x, y - 1), airD = !w.sp(x, y + 1), airL = !w.sp(x - 1, y), airR = !w.sp(x + 1, y);
    const sky = (xx, yy) => !w.sp(xx, yy) && w.B[w.i(Math.max(0, Math.min(w.W - 1, (xx / C) | 0)), Math.max(0, Math.min(w.H - 1, (yy / C) | 0)))] === null;
    if (pm === 'soil' && (sky(x, y - 1) || sky(x, y - 2) || sky(x, y - 3))) {
      const d = sky(x, y - 1) ? 0 : sky(x, y - 2) ? 1 : 2; c = d === 0 ? hex('#78b48a') : d === 1 ? hex('#55906a') : hash(x, y, 90) < 0.6 ? hex('#3f6f52') : c;
    } else if (airU || airD || airL || airR) c = p.ol;
    else if (!w.sp(x, y - 2) || !w.sp(x - 2, y)) c = p.hi;
    else if (!w.sp(x, y + 2) || !w.sp(x + 2, y)) c = mul(c, 0.78);
    pic.alb.set(c, i * 3);
  }
}

// ---------- light ----------
function lightUp(pic, opt = {}) {
  const w = pic.w, { PW, PH } = w, n = PW * PH, AMB = opt.amb == null ? 0.03 : opt.amb;
  const L = new Float32Array(n * 3).fill(AMB), beam = new Float32Array(n);
  for (const l of pic.lights) {
    const R = l.r * C, x0 = Math.max(0, Math.floor(l.x - R)), x1 = Math.min(PW - 1, Math.ceil(l.x + R)), y0 = Math.max(0, Math.floor(l.y - R)), y1 = Math.min(PH - 1, Math.ceil(l.y + R));
    const cone = l.cone, cosI = cone ? Math.cos(cone.half - 0.09) : 0, cosO = cone ? Math.cos(cone.half + 0.05) : 0;
    const pen = l.pen || 0.86;
    for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
      const dx = x + 0.5 - l.x, dy = y + 0.5 - l.y, d = Math.sqrt(dx * dx + dy * dy); if (d > R) continue;
      let f = l.k * (cone ? 1 : (opt.glowK || 1.45)) * Math.pow(1 - d / R, l.p || 1.6);
      if (cone && d > 2) {
        const cs = (dx * cone.dx + dy * cone.dy) / d; if (cs < cosO) continue;
        const t = Math.min(1, (cs - cosO) / (cosI - cosO)); f *= t * t * (3 - 2 * t);
        f *= 0.85 + 0.15 * (cs - cosO) / (1 - cosO);
      }
      if (f < 0.004) continue;
      const steps = Math.ceil(d); let att = 1;
      for (let s = 1; s < steps; s++) { if (w.sp((l.x + dx * s / steps) | 0, (l.y + dy * s / steps) | 0)) { att *= pen; if (att < 0.03) break; } }
      f *= att; if (f < 0.004) continue;
      const i = y * PW + x;
      L[i * 3] += l.col[0] * f; L[i * 3 + 1] += l.col[1] * f; L[i * 3 + 2] += l.col[2] * f;
      if (cone) beam[i] += f;
    }
  }
  const out = new Float32Array(n * 3), E = new Float32Array(n * 3);
  for (let i = 0; i < n; i++) {
    let r = pic.alb[i * 3], g = pic.alb[i * 3 + 1], b = pic.alb[i * 3 + 2];
    if (pic.isE[i]) { const nb = pic.nob && pic.nob[i] ? 0.12 : 1; E[i * 3] = pic.emis[i * 3] * nb; E[i * 3 + 1] = pic.emis[i * 3 + 1] * nb; E[i * 3 + 2] = pic.emis[i * 3 + 2] * nb;
      // emissive pixels still take light on their matter part
      r = Math.max(pic.emis[i * 3], r * Math.min(1.4, L[i * 3])); g = Math.max(pic.emis[i * 3 + 1], g * Math.min(1.4, L[i * 3 + 1])); b = Math.max(pic.emis[i * 3 + 2], b * Math.min(1.4, L[i * 3 + 2]));
    } else if (!pic.unlit[i]) { const k = pic.back[i] ? 0.85 : 1; r *= Math.min(1.4, L[i * 3]) * k; g *= Math.min(1.4, L[i * 3 + 1]) * k; b *= Math.min(1.4, L[i * 3 + 2]) * k; }
    out[i * 3] = r; out[i * 3 + 1] = g; out[i * 3 + 2] = b;
  }
  // water: dark, takes a mirrored copy of what glows above its surface
  for (let y = 0; y < PH; y++) for (let x = 0; x < PW; x++) {
    const i = y * PW + x; if (!pic.water[i]) continue; const top = pic.waterTop[x], d = y - top;
    const my = top - d - 1; let ref = [0, 0, 0];
    if (my >= 0) { const j = my * PW + x; ref = [out[j * 3], out[j * 3 + 1], out[j * 3 + 2]]; }
    const ripple = (d % 3 === 1 && hash(x >> 2, y, 41) < 0.5) ? 0.55 : 1;
    const k = Math.max(0, 0.62 - d * 0.035) * ripple, base = pic.get(x, y), lv = [L[i * 3], L[i * 3 + 1], L[i * 3 + 2]];
    for (let c = 0; c < 3; c++) out[i * 3 + c] = base[c] * Math.min(1.6, lv[c]) * 1.25 + ref[c] * k * 1.3;
    if (d === 0) for (let c = 0; c < 3; c++) out[i * 3 + c] += 0.05 + lv[c] * (hash(x >> 1, 7, 3) < 0.7 ? 0.55 : 0.15);
  }
  // bloom of emissive pixels: a tight halo and a wide soft one
  const blur = (src, rad) => {
    const tmp = new Float32Array(n * 3), dst = new Float32Array(n * 3), wts = [];
    let sum = 0; for (let k = -rad * 3; k <= rad * 3; k++) { const v = Math.exp(-(k * k) / (2 * rad * rad)); wts.push(v); sum += v; }
    for (let k = 0; k < wts.length; k++) wts[k] /= sum; const R = rad * 3;
    for (let y = 0; y < PH; y++) for (let x = 0; x < PW; x++) { let a = 0, b = 0, c = 0;
      for (let k = -R; k <= R; k++) { const xx = x + k; if (xx < 0 || xx >= PW) continue; const j = (y * PW + xx) * 3, wv = wts[k + R]; a += src[j] * wv; b += src[j + 1] * wv; c += src[j + 2] * wv; }
      const o = (y * PW + x) * 3; tmp[o] = a; tmp[o + 1] = b; tmp[o + 2] = c; }
    for (let y = 0; y < PH; y++) for (let x = 0; x < PW; x++) { let a = 0, b = 0, c = 0;
      for (let k = -R; k <= R; k++) { const yy = y + k; if (yy < 0 || yy >= PH) continue; const j = (yy * PW + x) * 3, wv = wts[k + R]; a += tmp[j] * wv; b += tmp[j + 1] * wv; c += tmp[j + 2] * wv; }
      const o = (y * PW + x) * 3; dst[o] = a; dst[o + 1] = b; dst[o + 2] = c; }
    return dst;
  };
  const b1 = blur(E, 2), b2 = blur(E, 7), bk = opt.bloom == null ? 1 : opt.bloom;
  for (let i = 0; i < n * 3; i++) out[i] += (b1[i] * 0.9 + b2[i] * 1.6) * bk;
  // the beam's dust: a faint veil in the air of the cone
  for (let i = 0; i < n; i++) if (beam[i] > 0 && !w.solid[i]) { const v = beam[i] * 0.035; out[i * 3] += v; out[i * 3 + 1] += v * 0.96; out[i * 3 + 2] += v * 0.88; }
  // glints: only where the light is strong enough (the beam, mostly)
  const add = (x, y, c, a) => { if (x < 0 || y < 0 || x >= PW || y >= PH) return; const o = (y * PW + x) * 3; out[o] += c[0] * a; out[o + 1] += c[1] * a; out[o + 2] += c[2] * a; };
  for (const g of pic.glint) {
    const i = g.y * PW + g.x, lv = opt.glintAny ? (L[i * 3] + L[i * 3 + 1] + L[i * 3 + 2]) / 3 - AMB : beam[i];
    if (lv < g.need) continue; const a = Math.min(1, (lv - g.need) * 2.2 + 0.35) * (g.dim || 1);
    add(g.x, g.y, [1, 1, 1], a * 1.1);
    const arm = g.size === 2 ? 3 : 2;
    for (let k = 1; k <= arm; k++) { const f = a * (k === 1 ? 0.75 : k === 2 ? 0.35 : 0.15); add(g.x + k, g.y, g.col, f); add(g.x - k, g.y, g.col, f); add(g.x, g.y + k, g.col, f); add(g.x, g.y - k, g.col, f); }
    for (const [dx, dy] of [[1, 1], [-1, 1], [1, -1], [-1, -1]]) add(g.x + dx, g.y + dy, g.col, a * 0.12);
  }
  // motes: a crisp core and a small soft halo
  for (const m of pic.motes) {
    const a = m.a == null ? 1 : m.a, x = Math.round(m.x), y = Math.round(m.y);
    add(x, y, mix(m.col, [1, 1, 1], 0.45), a);
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) add(x + dx, y + dy, m.col, a * 0.32);
    for (const [dx, dy] of [[1, 1], [-1, 1], [1, -1], [-1, -1], [2, 0], [-2, 0], [0, 2], [0, -2]]) add(x + dx, y + dy, m.col, a * 0.1);
  }
  // drips: thin falling streaks, bright head below, gone at the tail
  for (const d of pic.drips) {
    for (let k = 0; k < d.len; k++) add(d.x, d.y - k, k === 0 ? mix(d.col, [1, 1, 1], 0.5) : d.col, d.a * Math.pow(1 - k / d.len, 1.5) * (k === 0 ? 1.2 : 0.8));
    add(d.x, d.y + 1, d.col, d.a * 0.15);
  }
  const img = P.create(PW, PH);
  for (let i = 0; i < n; i++) {
    const o = i * 4;
    img.data[o] = Math.round(Math.min(1, Math.max(0, out[i * 3])) * 255); img.data[o + 1] = Math.round(Math.min(1, Math.max(0, out[i * 3 + 1])) * 255);
    img.data[o + 2] = Math.round(Math.min(1, Math.max(0, out[i * 3 + 2])) * 255); img.data[o + 3] = 255;
  }
  return img;
}

module.exports = { C, P, hex, mix, mul, hash, vnoise, loadArt, MP, ORE, World, Pic, paintTerrain, lightUp, baseColor };

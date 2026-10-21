'use strict';
// ---------- math, noise, colour (the surface study's kit, made periodic across a layer's width) ----------
const mod = (a, n) => ((a % n) + n) % n;
const clamp = (v, a = 0, b = 1) => (v < a ? a : v > b ? b : v);
const smooth = (a, b, v) => { const t = clamp((v - a) / (b - a)); return t * t * (3 - 2 * t); };
function hash(x, y, s = 0) {
  let h = Math.imul(x | 0, 374761393) + Math.imul(y | 0, 668265263) + Math.imul(s | 0, 1442695041);
  h = Math.imul(h ^ (h >>> 13), 1274126177); h ^= h >>> 16; return (h >>> 0) / 4294967296;
}
function vn1(x, cell, s, P) {
  const n = Math.max(1, Math.round(P / cell)), g = Math.floor(x / cell), f = x / cell - g, t = f * f * (3 - 2 * f);
  const a = hash(mod(g, n), 0, s), b = hash(mod(g + 1, n), 0, s); return a + (b - a) * t;
}
function fbm1(x, cell, s, oct, P) { let v = 0, a = 0.5, n = 0; for (let k = 0; k < oct && cell >= 1; k++) { v += vn1(x, cell, s + k * 17, P) * a; n += a; a *= 0.5; cell /= 2; } return v / n; }
function vn2(x, y, cell, s, P) {
  const n = Math.max(1, Math.round(P / cell)), gx = Math.floor(x / cell), gy = Math.floor(y / cell), fx = x / cell - gx, fy = y / cell - gy;
  const sx = fx * fx * (3 - 2 * fx), sy = fy * fy * (3 - 2 * fy), x0 = mod(gx, n), x1 = mod(gx + 1, n);
  const a = hash(x0, gy, s), b = hash(x1, gy, s), c = hash(x0, gy + 1, s), d = hash(x1, gy + 1, s);
  return a + (b - a) * sx + (c - a) * sy + (a - b - c + d) * sx * sy;
}
function fbm2(x, y, cell, s, oct, P) { let v = 0, a = 0.5, n = 0; for (let k = 0; k < oct && cell >= 1; k++) { v += vn2(x, y, cell, s + k * 17, P) * a; n += a; a *= 0.5; cell /= 2; } return v / n; }
const BAYER = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5].map((v) => (v + 0.5) / 16);
const bayer = (x, y) => BAYER[(y & 3) * 4 + (x & 3)];
const hex = (h) => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)];
const C = (v) => (typeof v === 'string' ? hex(v) : v);
const mix = (a, b, t) => { a = C(a); b = C(b); return [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t]; };
const mul = (a, k) => { a = C(a); return Array.isArray(k) ? [a[0] * k[0], a[1] * k[1], a[2] * k[2]] : [a[0] * k, a[1] * k, a[2] * k]; };
const css = (c, a = 1) => { c = C(c); return `rgba(${c[0] | 0},${c[1] | 0},${c[2] | 0},${a})`; };
function rampD(cols, t, x, y, dz = 1) { const n = cols.length - 1; let f = clamp(t) * n + (bayer(x, y) - 0.5) * dz; return cols[Math.round(clamp(f, 0, n))]; }
function rampS(cols, t) { const n = cols.length - 1, f = clamp(t) * n, i = Math.min(n - 1, Math.floor(f)); return mix(cols[i], cols[i + 1], f - i); }

// ---------- a pixel buffer that wraps across its width ----------
class Buf {
  constructor(w, h, wrap = true) { this.w = w; this.h = h; this.wrap = wrap; this.d = new Uint8ClampedArray(w * h * 4); }
  idx(x, y) { x = Math.floor(x); y = Math.floor(y); if (this.wrap) x = mod(x, this.w); else if (x < 0 || x >= this.w) return -1; if (y < 0 || y >= this.h) return -1; return (y * this.w + x) * 4; }
  set(x, y, c, a = 1) {
    const i = this.idx(x, y); if (i < 0 || a <= 0) return; const d = this.d; c = C(c);
    if (a >= 1) { d[i] = c[0]; d[i + 1] = c[1]; d[i + 2] = c[2]; d[i + 3] = 255; return; }
    const da = d[i + 3] / 255, na = a + da * (1 - a);
    for (let k = 0; k < 3; k++) d[i + k] = (c[k] * a + d[i + k] * da * (1 - a)) / na;
    d[i + 3] = na * 255;
  }
  add(x, y, c, a = 1) { // emissive: brightest wins, alpha grows
    const i = this.idx(x, y); if (i < 0) return; const d = this.d; c = C(c);
    const da = d[i + 3] / 255, na = Math.min(1, da + a);
    for (let k = 0; k < 3; k++) d[i + k] = Math.max(d[i + k], c[k]);
    d[i + 3] = na * 255;
  }
  has(x, y) { const i = this.idx(x, y); return i >= 0 && this.d[i + 3] > 127; }
  get(x, y) { const i = this.idx(x, y); return i < 0 ? [0, 0, 0] : [this.d[i], this.d[i + 1], this.d[i + 2]]; }
  outline(col, onlyAir = true) {
    const add = [];
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      if (this.has(x, y)) continue;
      if (this.has(x + 1, y) || this.has(x - 1, y) || this.has(x, y + 1) || this.has(x, y - 1)) add.push(x, y);
    }
    for (let k = 0; k < add.length; k += 2) this.set(add[k], add[k + 1], col);
  }
  canvas(blur = 0) {
    const cv = document.createElement('canvas'); cv.width = this.w; cv.height = this.h;
    const ctx = cv.getContext('2d'); ctx.putImageData(new ImageData(this.d, this.w, this.h), 0, 0);
    if (!blur) return cv;
    // a glow: its sharp pixels plus a soft bloom of them, baked once
    const out = document.createElement('canvas'); out.width = this.w; out.height = this.h;
    const o = out.getContext('2d'); o.filter = `blur(${blur}px)`; o.globalAlpha = 0.9;
    o.drawImage(cv, -this.w, 0); o.drawImage(cv, 0, 0); o.drawImage(cv, this.w, 0);
    o.filter = `blur(${blur * 3}px)`; o.globalAlpha = 0.55; o.drawImage(cv, -this.w, 0); o.drawImage(cv, 0, 0); o.drawImage(cv, this.w, 0);
    o.filter = 'none'; o.globalAlpha = 1; o.globalCompositeOperation = 'lighter'; o.drawImage(cv, 0, 0);
    return out;
  }
}

// thick pixel line
function line(b, x0, y0, x1, y1, w, col, a = 1) {
  const n = Math.max(1, Math.ceil(Math.max(Math.abs(x1 - x0), Math.abs(y1 - y0))));
  const r = (w - 1) / 2;
  for (let k = 0; k <= n; k++) {
    const t = k / n, x = x0 + (x1 - x0) * t, y = y0 + (y1 - y0) * t;
    for (let dy = -Math.floor(r); dy <= Math.ceil(r); dy++) for (let dx = -Math.floor(r); dx <= Math.ceil(r); dx++) b.set(x + dx, y + dy, col, a);
  }
}
function ellipseFill(b, cx, cy, rx, ry, fn) {
  for (let y = Math.floor(cy - ry); y <= Math.ceil(cy + ry); y++) for (let x = Math.floor(cx - rx); x <= Math.ceil(cx + rx); x++) {
    const dx = (x + 0.5 - cx) / rx, dy = (y + 0.5 - cy) / ry, e = dx * dx + dy * dy; if (e <= 1) fn(x, y, dx, dy, e);
  }
}

// ---------- art from the game, decoded once ----------
const IMG = {}, TEX = {};
function loadArt() {
  return Promise.all(Object.entries(window.__A).map(([k, v]) => new Promise((res) => {
    const im = new Image(); im.onload = () => { IMG[k] = im; res(); }; im.onerror = () => res(); im.src = v;
  })));
}
function tex(name) {
  if (TEX[name]) return TEX[name];
  const im = IMG[name], cv = document.createElement('canvas'); cv.width = im.width; cv.height = im.height;
  const c = cv.getContext('2d'); c.drawImage(im, 0, 0);
  return (TEX[name] = { w: im.width, h: im.height, d: c.getImageData(0, 0, im.width, im.height).data, cv });
}
function texAt(t, x, y) { const i = (mod(y, t.h) * t.w + mod(x, t.w)) * 4; return [t.d[i], t.d[i + 1], t.d[i + 2], t.d[i + 3]]; }

// ---------- canvas helpers ----------
function mkCanvas(w, h) { const c = document.createElement('canvas'); c.width = w; c.height = h; return c; }
function drawWrap(ctx, img, sx, dy, W, alpha = 1) {
  if (alpha <= 0.001) return;
  const LW = img.width; let s = mod(Math.round(sx), LW), x = 0; dy = Math.round(dy);
  ctx.globalAlpha = alpha;
  while (x < W) { const w = Math.min(LW - s, W - x); ctx.drawImage(img, s, 0, w, img.height, x, dy, w, img.height); x += w; s = 0; }
  ctx.globalAlpha = 1;
}
// a wrapped layer's sprite positions on the screen
function wrapX(px, off, LW, W, margin) { const s = mod(px - off, LW); const out = []; if (s < W + margin) out.push(s); if (s - LW > -margin) out.push(s - LW); return out; }

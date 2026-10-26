// The loper's designs as parts painted in palette ramps: every pixel is (ramp, tone), never a colour. Shared by the
// pick page (inlined) and paint.js (the export). Plain JS, no packages; deterministic.
(function (root, factory) {
  if (typeof module === 'object' && module.exports) module.exports = factory();
  else root.LOPER = factory();
})(typeof self !== 'undefined' ? self : this, function () {
  'use strict';

  // ---------- ramps ----------
  // R channel of an index map: creatures ramps take the top of the 0-31 range, free beside the ~20 of the ground and flora.
  const RAMP_INDEX = { horn: 23, furA: 24, furB: 25, belly: 26, mark: 27, skin: 28, eye: 29, frond: 30, glow: 31 };
  const TONES = ['контур', 'глубокая тень', 'тень', 'основа', 'свет', 'блик'];
  const clamp = (v, a = 0, b = 1) => Math.max(a, Math.min(b, v));
  const hexRgb = (h) => [parseInt(h.slice(1, 3), 16) / 255, parseInt(h.slice(3, 5), 16) / 255, parseInt(h.slice(5, 7), 16) / 255];
  const rgbHex = (c) => '#' + c.map((v) => Math.round(clamp(v) * 255).toString(16).padStart(2, '0')).join('');
  function rgbHsl([r, g, b]) {
    const mx = Math.max(r, g, b), mn = Math.min(r, g, b), l = (mx + mn) / 2;
    if (mx === mn) return [0, 0, l];
    const d = mx - mn, s = l > 0.5 ? d / (2 - mx - mn) : d / (mx + mn);
    let h = mx === r ? (g - b) / d + (g < b ? 6 : 0) : mx === g ? (b - r) / d + 2 : (r - g) / d + 4;
    return [h * 60, s, l];
  }
  function hslRgb(h, s, l) {
    h = ((h % 360) + 360) % 360 / 360;
    if (!s) return [l, l, l];
    const q = l < 0.5 ? l * (1 + s) : l + s - l * s, p = 2 * l - q;
    const f = (t) => { t = (t + 1) % 1; return t < 1 / 6 ? p + (q - p) * 6 * t : t < 0.5 ? q : t < 2 / 3 ? p + (q - p) * (2 / 3 - t) * 6 : p; };
    return [f(h + 1 / 3), f(h), f(h - 1 / 3)];
  }
  const toward = (h, t, amt) => { const d = ((t - h + 540) % 360) - 180; return h + Math.sign(d) * Math.min(Math.abs(d), amt); };
  // six tones from one base: shadows lean cool, lights warm, the outline dark and desaturated (the crate's and the pod's way)
  function ramp(base, o = {}) {
    const [h, s, l] = rgbHsl(hexRgb(base)), cool = o.cool ?? 245, warm = o.warm ?? 50;
    const L = [l * 0.25, l * 0.52, l * 0.76, l, l + (1 - l) * 0.34, l + (1 - l) * 0.66];
    const H = [toward(h, cool, 28), toward(h, cool, 18), toward(h, cool, 9), h, toward(h, warm, 7), toward(h, warm, 14)];
    const pale = l > 0.78 ? 0.4 : 1, Sx = [0.6 * pale, 0.9 * pale, pale, 1, 0.95, 0.75];
    return L.map((li, i) => rgbHex(hslRgb(H[i], clamp(s * Sx[i] * (o.sat ?? 1)), clamp(li))));
  }
  // a ramp from a biome's five leaf tones (dark to light) and an outline below them
  const leafRamp = (five) => { const [h, s, l] = rgbHsl(hexRgb(five[0])); return [rgbHex(hslRgb(toward(h, 245, 12), s * 0.7, l * 0.55)), ...five]; };
  const eyeRamp = (white, ink) => { const w = hexRgb(white), k = hexRgb(ink); const m = (t) => rgbHex(w.map((v, i) => k[i] + (v - k[i]) * t)); return [ink, m(0.3), m(0.72), m(0.86), m(0.95), '#ffffff']; };

  const PALETTES = {
    savanna: {
      name: 'Саванна', sky: ['#3e6fb0', '#5f93c8', '#93bfdc', '#cfe0dc', '#f1e3c2'], soil: ['#45342a', '#5a4434', '#6a5040'], grass: ['#3f6f52', '#55906a', '#78b48a'],
      ramps: {
        furA: ramp('#c79a6e'), furB: ramp('#8f86b4'), belly: ramp('#eee0c6'), mark: ramp('#6a4c42'), skin: ramp('#d69a8c'),
        eye: eyeRamp('#f6f1e2', '#1f1a24'), frond: leafRamp(['#3f6f52', '#55906a', '#78b48a', '#9ccf9c', '#b8e2b0']), glow: ramp('#6fdcc4', { warm: 170 }), horn: ramp('#6e5249'),
      },
    },
    badlands: {
      name: 'Бэдлендс', sky: ['#5a7aa8', '#8aa0b8', '#c0b8a8', '#e8d0a0', '#f6e0b0'], soil: ['#4a2c22', '#6a4030', '#83503a'], grass: ['#6a3a22', '#9a5a2a', '#c8803a'],
      ramps: {
        furA: ramp('#c9784a'), furB: ramp('#a6946e'), belly: ramp('#f2d8ae'), mark: ramp('#5a3428'), skin: ramp('#dc9a7c'),
        eye: eyeRamp('#fbf0dc', '#24160f'), frond: leafRamp(['#3a2418', '#6a3a22', '#9a5a2a', '#c8803a', '#e8b060']), glow: ramp('#ffc85e', { warm: 60 }), horn: ramp('#4e3228'),
      },
    },
    crystal: {
      name: 'Кристальный', sky: ['#3a3a7a', '#5c5aa0', '#8e88c4', '#c8b8dc', '#f0dcd4'], soil: ['#2e2a44', '#423c5e', '#544c72'], grass: ['#3a2e6a', '#5a4a9a', '#8a78d0'],
      ramps: {
        furA: ramp('#9b90d2'), furB: ramp('#5d86b6'), belly: ramp('#e8e2fb'), mark: ramp('#3a2f6c'), skin: ramp('#cf9ad0'),
        eye: eyeRamp('#f4f2ff', '#18142c'), frond: leafRamp(['#1e1a3e', '#3a2e6a', '#5a4a9a', '#8a78d0', '#c8b8ff']), glow: ramp('#a6f2f6', { warm: 190 }), horn: ramp('#433c78'),
      },
    },
  };

  // ---------- painting: ellipsoids and tubes, lit from the upper left, quantised to tones ----------
  const LIGHT = (() => { const v = [-0.62, -0.86, 0.5], n = Math.hypot(...v); return v.map((x) => x / n); })();
  const E = (x, y, rx, ry, o = {}) => ({ k: 'e', x, y, rx, ry, ...o });
  const T = (pts, o = {}) => ({ k: 't', pts, ...o });
  const inE = (x, y, cx, cy, rx, ry) => ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1;
  function segDist(x, y, a, b) {
    const dx = b[0] - a[0], dy = b[1] - a[1], L2 = dx * dx + dy * dy || 1e-9, t = clamp(((x - a[0]) * dx + (y - a[1]) * dy) / L2);
    return [Math.hypot(x - a[0] - dx * t, y - a[1] - dy * t), a[2] + (b[2] - a[2]) * t];
  }
  const inTube = (x, y, pts) => { for (let i = 0; i + 1 < pts.length; i++) { const [d, r] = segDist(x, y, pts[i], pts[i + 1]); if (d <= r) return true; } return false; };
  function hash(x, y, s = 0) {
    let h = Math.imul(x | 0, 374761393) + Math.imul(y | 0, 668265263) + Math.imul(s | 0, 1442695041);
    h = Math.imul(h ^ (h >>> 13), 1274126177); h ^= h >>> 16; return (h >>> 0) / 4294967296;
  }
  // tubes become chains of spheres
  function expand(prims) {
    const out = [];
    for (const p of prims) {
      if (p.k === 'e') { out.push(p); continue; }
      for (let i = 0; i + 1 < p.pts.length; i++) {
        const a = p.pts[i], b = p.pts[i + 1], n = Math.max(2, Math.ceil(Math.hypot(b[0] - a[0], b[1] - a[1]) / 0.3));
        for (let k = 0; k <= n; k++) { const t = k / n, r = a[2] + (b[2] - a[2]) * t; out.push({ ...p, k: 'e', x: a[0] + (b[0] - a[0]) * t, y: a[1] + (b[1] - a[1]) * t, rx: r, ry: r, rz: r * (p.flat ?? 1), cz: p.cz ?? 0 }); }
      }
    }
    return out;
  }
  function paint(def) {
    const sph = expand(def.prims);
    let x0 = 1e9, y0 = 1e9, x1 = -1e9, y1 = -1e9;
    for (const e of sph) { const r = Math.max(e.rx, e.ry); x0 = Math.min(x0, e.x - r); y0 = Math.min(y0, e.y - r); x1 = Math.max(x1, e.x + r); y1 = Math.max(y1, e.y + r); }
    const ox = Math.floor(x0) - 1, oy = Math.floor(y0) - 1, w = Math.ceil(x1) + 1 - ox, h = Math.ceil(y1) + 1 - oy;
    const N = w * h, role = new Array(N).fill(null), tone = new Uint8Array(N), z = new Float32Array(N).fill(-1e9), grp = new Int16Array(N).fill(-1), prim = new Int32Array(N).fill(-1);
    sph.forEach((e, ei) => {
      const rz = e.rz ?? Math.min(e.rx, e.ry), c = Math.cos(e.rot || 0), s = Math.sin(e.rot || 0), r = Math.max(e.rx, e.ry);
      for (let j = Math.floor(e.y - r - oy); j <= Math.ceil(e.y + r - oy); j++) for (let i = Math.floor(e.x - r - ox); i <= Math.ceil(e.x + r - ox); i++) {
        if (i < 0 || j < 0 || i >= w || j >= h) continue;
        const dx = i + ox + 0.5 - e.x, dy = j + oy + 0.5 - e.y, u = (dx * c + dy * s) / e.rx, v = (-dx * s + dy * c) / e.ry, d = u * u + v * v;
        if (d > 1) continue;
        const nz = Math.sqrt(1 - d), zz = (e.cz || 0) + rz * nz, q = j * w + i;
        if (zz <= z[q]) continue;
        let gx = u / e.rx, gy = v / e.ry, gz = nz / rz; const nx = gx * c - gy * s, ny = gx * s + gy * c, nl = Math.hypot(nx, ny, gz) || 1;
        const lam = (nx * LIGHT[0] + ny * LIGHT[1] + gz * LIGHT[2]) / nl + (e.bias || 0) + (def.bias || 0);
        let t = lam > 0.93 && e.hi !== false && def.hi !== false ? 5 : lam > 0.7 ? 4 : lam > 0.38 ? 3 : lam > 0.06 ? 2 : 1;
        if (t === 1 && ny / nl > 0.55) t = 2; // light bounced up from the ground keeps the underside off black
        z[q] = zz; role[q] = e.role || 'fur'; tone[q] = t; grp[q] = e.grp ?? 0; prim[q] = ei;
      }
    });
    const P = {
      name: def.name, w, h, ox, oy, role, tone, z,
      idx: (X, Y) => { const i = X - ox, j = Y - oy; return i < 0 || j < 0 || i >= w || j >= h ? -1 : j * w + i; },
      has(X, Y) { const q = this.idx(X, Y); return q >= 0 && role[q] !== null; },
      get(X, Y) { const q = this.idx(X, Y); return q >= 0 && role[q] !== null ? { role: role[q], tone: tone[q] } : null; },
      set(X, Y, r, t) { const q = this.idx(X, Y); if (q < 0) return; role[q] = r; tone[q] = t; if (z[q] < -1e8) z[q] = 99; },
      clear(X, Y) { const q = this.idx(X, Y); if (q >= 0) role[q] = null; },
      each(fn) { for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) { const q = j * w + i; if (role[q] !== null) fn(i + ox, j + oy, q); } },
      recolor(test, r, dt = 0) { this.each((X, Y, q) => { if (test(X + 0.5, Y + 0.5, role[q], tone[q])) { role[q] = r; tone[q] = clamp(tone[q] + dt, 1, 5); } }); },
      shift(test, dt) { this.each((X, Y, q) => { if (test(X + 0.5, Y + 0.5, role[q], tone[q])) tone[q] = clamp(tone[q] + dt, 1, 5); }); },
    };
    smooth(P);
    if (def.fur) furTexture(P, def.fur);
    if (def.post) def.post(P);
    cleanup(P);
    if (def.lines !== false) zLines(P, grp, prim, sph);
    if (def.outline !== false) outline(P, def.outlineSkip);
    if (def.after) def.after(P);
    return P;
  }
  // tone bands made clean: a pixel that agrees with at most one neighbour takes the tone most of them have
  function smooth(P) {
    const fix = [];
    P.each((X, Y, q) => {
      const cnt = [0, 0, 0, 0, 0, 0]; let same = 0;
      for (let b = -1; b <= 1; b++) for (let a = -1; a <= 1; a++) { if (!a && !b) continue; const c = P.get(X + a, Y + b); if (!c || c.role !== P.role[q]) continue; cnt[c.tone]++; if (c.tone === P.tone[q]) same++; }
      if (same > 1) return; let bt = -1, bn = 4; cnt.forEach((n, t) => { if (n > bn) { bn = n; bt = t; } }); if (bt > 0) fix.push([q, bt]);
    });
    for (const [q, t] of fix) P.tone[q] = t;
  }
  // short darker strands in the fur's base and lit tones, leaning down and back
  function furTexture(P, o) {
    const marks = [];
    P.each((X, Y, q) => {
      if (!o.roles.includes(P.role[q]) || P.tone[q] !== 3) return;
      if (hash(X, Y, o.seed || 7) < o.density && P.get(X - 1, Y + 1)) marks.push([X, Y], [X - 1, Y + 1]);
    });
    for (const [X, Y] of marks) { const c = P.get(X, Y); if (c && o.roles.includes(c.role) && c.tone > 1) P.set(X, Y, c.role, c.tone - 1); }
  }
  // a lone pixel of one tone amid another takes its neighbours' tone
  function cleanup(P) {
    const fix = [];
    P.each((X, Y, q) => {
      const t = P.tone[q]; if (t === 0 || t === 5) return;
      const n = [[1, 0], [-1, 0], [0, 1], [0, -1]].map(([a, b]) => P.get(X + a, Y + b));
      if (n.some((c) => !c || c.role !== P.role[q])) return;
      if (n.every((c) => c.tone === n[0].tone) && n[0].tone !== t && n[0].tone !== 0) fix.push([q, n[0].tone]);
    });
    for (const [q, t] of fix) P.tone[q] = t;
  }
  // a line where one group of a part passes in front of another
  function zLines(P, grp, prim, sph) {
    const put = [];
    P.each((X, Y, q) => {
      for (const [a, b] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
        const r = P.idx(X + a, Y + b); if (r < 0 || P.role[r] === null) continue;
        if (grp[r] !== grp[q] && P.z[q] - P.z[r] > 1.2) put.push(r);
      }
    });
    for (const r of put) P.tone[r] = 0;
  }
  // a 1px outline outside the shape, coloured from the ramp of the pixel it hugs (4-connected: rounder at this size)
  function outline(P) {
    const add = [];
    for (let j = 0; j < P.h; j++) for (let i = 0; i < P.w; i++) {
      const q = j * P.w + i; if (P.role[q] !== null) continue;
      let best = null, bz = -1e9;
      for (const [a, b] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) { const ii = i + a, jj = j + b; if (ii < 0 || jj < 0 || ii >= P.w || jj >= P.h) continue; const r = jj * P.w + ii; if (P.role[r] !== null && P.tone[r] !== 0 && P.z[r] > bz) { bz = P.z[r]; best = P.role[r]; } }
      if (best) add.push([q, best]);
    }
    for (const [q, r] of add) { P.role[q] = r; P.tone[q] = 0; P.z[q] = -1e7; }
  }
  // a part made of another's pixels: markings keep the body's light, in their own ramp
  function derive(src, test, r, dt = 0) {
    const N = src.w * src.h, P = { name: '', w: src.w, h: src.h, ox: src.ox, oy: src.oy, role: new Array(N).fill(null), tone: new Uint8Array(N) };
    for (let j = 0; j < src.h; j++) for (let i = 0; i < src.w; i++) {
      const q = j * src.w + i, X = i + src.ox, Y = j + src.oy;
      if (src.role[q] === null || src.tone[q] === 0) continue;
      const got = test(X + 0.5, Y + 0.5, src.tone[q], src.role[q]);
      if (got) { P.role[q] = typeof got === 'string' ? got : r; P.tone[q] = clamp(src.tone[q] + dt, 1, 5); }
    }
    return P;
  }

  // ---------- the eye: sclera, a pupil that moves, a lid over both; four states ----------
  function eyeSet(o) {
    const { rx, ry } = o, wx = rx * 1.22, wy = ry * 1.32, lidRole = o.lid || 'fur';
    const sclera = (ax, ay, thick) => paint({ name: 'sclera', lines: false, hi: false, prims: [E(0, 0, ax, ay, { role: 'eye', rz: 0.2 })], post(P) {
      P.each((X, Y, q) => { const x = X + 0.5, y = Y + 0.5; let t = 5; if (y > ay * 0.35 && x > -ax * 0.1) t = 4; if (y > ay * 0.62) t = 3; if (y < -ay + 1.6) t = 3; P.tone[q] = t; });
    }, after(P) { if (thick) P.each((X, Y, q) => { if (P.tone[q] && !P.has(X, Y - 1)) { /* outline row above stays */ } if (P.tone[q] && P.tone[q] !== 0 && P.get(X, Y - 1) && P.get(X, Y - 1).tone === 0 && Y + 0.5 < -ay * 0.45) P.tone[q] = 0; }); } });
    const open = sclera(rx, ry, true), wide = sclera(wx, wy, false);
    // lids: the lid's fur over the top of the sclera, its edge a dark line
    const lid = (ax, ay, cover) => {
      const P = paint({ name: 'lid', lines: false, outline: false, prims: [E(0, 0, ax + 0.6, ay + 0.6, { role: lidRole, rz: ay })] });
      const edge = -ay + cover * 2 * ay;
      P.each((X, Y, q) => { const x = X + 0.5, y = Y + 0.5; const curve = edge - 0.35 * cover * (1 - (x / ax) ** 2) * ay * 0.6; if (y > curve + 1) P.role[q] = null; else if (y > curve) { P.role[q] = 'eye'; P.tone[q] = 0; } });
      if (cover >= 0.99) { P.each((X, Y, q) => { if (Y + 0.5 > 0.3 * ay && Y + 0.5 < 0.3 * ay + 1 && Math.abs(X + 0.5) < ax - 0.5) { P.role[q] = 'eye'; P.tone[q] = 0; } }); P.set(Math.round(-ax) - 1, Math.round(0.3 * ay), 'eye', 0); }
      return P;
    };
    const pupil = (ri, rp, glint) => paint({ name: 'pupil', lines: false, outline: false, hi: false, prims: [E(0, 0, ri, ri * 1.1, { role: o.iris || 'glow', rz: 0.3 })], post(P) {
      P.each((X, Y, q) => { const x = X + 0.5, y = Y + 0.5; P.tone[q] = y > ri * 0.25 ? 2 : 1; if (inE(x, y, 0, 0.2, rp, rp * 1.15)) { P.role[q] = 'eye'; P.tone[q] = 0; } });
      for (const [gx, gy] of glint) P.set(gx, gy, 'eye', 5);
    } });
    const ri = Math.min(rx, ry) * 0.7;
    return {
      sclera: { open, wide, half: open, blink: open },
      lid: { open: null, wide: null, half: lid(rx, ry, 0.5), blink: lid(rx, ry, 1) },
      pupil: { calm: pupil(ri, ri * 0.5, [[-Math.ceil(ri * 0.55), -Math.ceil(ri * 0.65)]]), wide: pupil(ri * 0.78, ri * 0.32, [[-Math.ceil(ri * 0.45), -Math.ceil(ri * 0.6)]]) },
      travel: [Math.max(0.6, rx - ri - 0.4), Math.max(0.6, ry - ri - 0.6)], travelWide: [wx - ri * 0.78 - 0.4, wy - ri * 0.78 - 0.6],
    };
  }

  // ---------- the designs ----------
  // Coordinates in pixels of the world, origin the middle of the feet, facing right, y down. The sim's body is 32 x 32:
  // x -16..16, y -32..0. Every node hangs from its parent at `at`, turns about its part's pivot (the part's 0,0).
  const leg = (pts, paw, far) => ({ prims: [T(pts), paw], bias: far ? -0.2 : 0 });
  const DESIGNS = [
    {
      id: 'classic', name: 'Классика', tag: 'Тот же лопер, перерисованный: круглый, одноглазый, уши-лепестки с тёмными кисточками.',
      ramps: { fur: 'furA', belly: 'belly', mark: 'mark', skin: 'skin', eye: 'eye', glow: 'glow' },
      gait: 'walk', headless: true,
      eye: { rx: 4, ry: 4.6, at: [6, -18] },
      parts: {
        body: { fur: { roles: ['fur'], density: 0.05 }, prims: [
          E(0, -15, 13.5, 11, { rz: 10 }), T([[-3, -23, 2.6], [-4.6, -26.6, 1.1]]), T([[-8, -21.2, 2.4], [-10.6, -23.8, 1]]), T([[-12, -17, 2.2], [-14.8, -18.6, 0.9]]),
          T([[10, -8, 2.2], [14, -6.4, 0.5]]), T([[5, -5, 2], [7, -2.4, 0.5]]),
        ], post(P) {
          P.recolor((x, y, r) => r === 'fur' && inE(x, y, 4, -7.5, 9.5, 6), 'belly', 1);
          P.set(13, -13, 'skin', 3); P.set(12, -13, 'skin', 2); P.set(11, -10, 'mark', 1); P.set(12, -11, 'mark', 1);
        } },
        marks: { from: 'body', test: (x, y) => inTube(x, y, [[-1, -25.5, 1.3], [-1.5, -20, 0.7]]) || inTube(x, y, [[-5.5, -24.5, 1.3], [-6.5, -19, 0.7]]) || inTube(x, y, [[-9.5, -22, 1.2], [-10.5, -17, 0.6]]), role: 'mark' },
        ear: { prims: [T([[0, 0, 2.3], [0.3, -4.5, 2.1], [0, -9, 1.2]]), T([[0, -8.5, 1.6], [0, -11.8, 0.4]], { role: 'mark' })], post(P) { P.recolor((x, y, r, t) => r === 'fur' && Math.abs(x - 0.4) < 1.1 && y > -7.5 && y < -1.5, 'skin'); } },
        earFar: { prims: [T([[0, 0, 2.3], [0.3, -4.5, 2.1], [0, -9, 1.2]]), T([[0, -8.5, 1.6], [0, -11.8, 0.4]], { role: 'mark' })], bias: -0.22 },
        leg: leg([[0, 0, 2.6], [0, 3.6, 2.4]], E(0.6, 4, 2.8, 1.6, { role: 'mark' })),
        legFar: leg([[0, 0, 2.6], [0, 3.6, 2.4]], E(0.6, 4, 2.8, 1.6, { role: 'mark' }), true),
        tail: { prims: [E(-1.5, 0, 3, 2.8, { role: 'mark' }), T([[-2, 0, 2], [-6.2, -1.5, 0.5]], { role: 'mark' }), T([[-1.5, -1, 1.8], [-4.5, -4.8, 0.5]], { role: 'mark' }), T([[-1.5, 1, 1.8], [-4.8, 3.6, 0.5]], { role: 'mark' })] },
      },
      rig: [
        { id: 'earFar', part: 'earFar', parent: 'body', at: [1, -24.5], drive: 'ear', base: -0.4 },
        { id: 'legFH', part: 'legFar', parent: 'body', at: [-3, -5.5], drive: 'leg', phase: 0 },
        { id: 'legFF', part: 'legFar', parent: 'body', at: [8, -5.5], drive: 'leg', phase: 0.5 },
        { id: 'tail', part: 'tail', parent: 'body', at: [-12.5, -16], drive: 'tail' },
        { id: 'body', part: 'body', parent: null, at: [0, 0], drive: 'body' },
        { id: 'marks', part: 'marks', parent: 'body', at: [0, 0] },
        { id: 'legNH', part: 'leg', parent: 'body', at: [-6, -5], drive: 'leg', phase: 0.5 },
        { id: 'legNF', part: 'leg', parent: 'body', at: [5, -5], drive: 'leg', phase: 0 },
        { id: 'eye', part: 'eye', parent: 'body', at: [6, -18], drive: 'eye' },
        { id: 'ear', part: 'ear', parent: 'body', at: [6, -23.5], drive: 'ear', base: -0.12 },
      ],
      pose: { earUp: 0, earBack: -1.1, earPerk: 0.12, legAmp: 0.55, lift: 1.4, tailAmp: 0.18, tailFlee: -0.7, graze: 0.16 },
    },
    {
      id: 'hopper', name: 'Прыгун', tag: 'Длинноухий скакун: тяжёлые задние лапы, хвост-кисточка, светящиеся кончики ушей.',
      ramps: { fur: 'furA', belly: 'belly', mark: 'mark', skin: 'skin', eye: 'eye', glow: 'glow' },
      gait: 'hop',
      eye: { rx: 3.5, ry: 4, at: [3, -5] },
      parts: {
        body: { fur: { roles: ['fur'], density: 0.07 }, prims: [E(-4, -10.5, 8.5, 8, { rz: 8 }), E(4, -13, 6, 6, { rz: 6, cz: 0.5 }), T([[-6, -17.5, 2], [-9.2, -20.6, 0.5]]), T([[-10.5, -13.5, 1.8], [-13.8, -15, 0.4]])],
          post(P) { P.recolor((x, y, r) => r === 'fur' && inE(x, y, 3, -7.5, 7, 4.5), 'belly', 1); } },
        marks: { from: 'body', test: (x, y) => inE(x, y, -3.5, -17, 7.5, 3.3) && x < 5, role: 'mark' },
        head: { fur: { roles: ['fur'], density: 0.05 }, prims: [E(3, -4, 6, 5.2, { rz: 5.5 }), E(8, -2.5, 3, 2.6, { rz: 3, cz: 1 }), T([[1.5, 0, 1.8], [-0.5, 2.6, 0.4]])],
          post(P) { P.recolor((x, y, r) => r === 'fur' && inE(x, y, 8.3, -1.4, 3, 1.9), 'belly', 1); P.set(10, -4, 'skin', 3); P.set(10, -3, 'skin', 2); P.set(9, -1, 'mark', 1); } },
        ear: { prims: [T([[0, 0, 1.7], [-0.4, -6, 2.2], [-0.8, -11, 1.7], [-0.6, -13.6, 0.7]]), E(-0.6, -14.2, 1.3, 1.3, { role: 'glow', cz: 1.5 })], post(P) { P.recolor((x, y, r) => r === 'fur' && Math.abs(x + 0.2) < 1 && y > -11.5 && y < -2, 'skin'); } },
        earFar: { prims: [T([[0, 0, 1.7], [-0.4, -6, 2.2], [-0.8, -11, 1.7], [-0.6, -13.6, 0.7]]), E(-0.6, -14.2, 1.3, 1.3, { role: 'glow', cz: 1.5 })], bias: -0.22 },
        hind: leg([[0, 0, 2.6], [-1, 4, 1.7], [-1.4, 4.9, 1.5], [4.6, 5.6, 1.2]], E(5, 5.6, 1.5, 1.1, { role: 'mark' })),
        hindFar: leg([[0, 0, 2.6], [-1, 4, 1.7], [-1.4, 4.9, 1.5], [4.6, 5.6, 1.2]], E(5, 5.6, 1.5, 1.1, { role: 'mark' }), true),
        fore: leg([[0, 0, 1.5], [0.6, 6.4, 1.2]], E(1, 7, 1.6, 1.1, { role: 'mark' })),
        foreFar: leg([[0, 0, 1.5], [0.6, 6.4, 1.2]], E(1, 7, 1.6, 1.1, { role: 'mark' }), true),
        tail: { prims: [T([[0, 0, 1.4], [-3, -0.6, 1.1], [-6, -2, 0.9]]), E(-7.6, -3.2, 2.6, 2.1, { role: 'mark' }), T([[-7.6, -3.2, 1.8], [-11, -4.4, 0.4]], { role: 'mark' })] },
      },
      rig: [
        { id: 'earFar', part: 'earFar', parent: 'head', at: [0.5, -8.3], drive: 'ear', base: -0.5 },
        { id: 'hindF', part: 'hindFar', parent: 'body', at: [-2, -7], drive: 'leg', hind: true },
        { id: 'foreF', part: 'foreFar', parent: 'body', at: [7, -8], drive: 'leg' },
        { id: 'tail', part: 'tail', parent: 'body', at: [-12, -11], drive: 'tail' },
        { id: 'body', part: 'body', parent: null, at: [0, 0], drive: 'body' },
        { id: 'marks', part: 'marks', parent: 'body', at: [0, 0] },
        { id: 'hind', part: 'hind', parent: 'body', at: [-5, -6.5], drive: 'leg', hind: true },
        { id: 'head', part: 'head', parent: 'body', at: [6, -17], drive: 'head' },
        { id: 'fore', part: 'fore', parent: 'body', at: [5, -8], drive: 'leg' },
        { id: 'eye', part: 'eye', parent: 'head', at: [3, -5], drive: 'eye' },
        { id: 'ear', part: 'ear', parent: 'head', at: [2.5, -8.6], drive: 'ear', base: -0.28 },
      ],
      pose: { earUp: 0, earBack: -1.3, earPerk: 0.16, legAmp: 0.5, lift: 1, tailAmp: 0.14, tailFlee: -0.55, graze: 0.75, hop: 5 },
    },
    {
      id: 'sixleg', name: 'Шестиног', tag: 'Шестиногий пастбищник: бочка на шести копытцах, висячие уши, хвост-ветвь с листьями-подвесками.',
      ramps: { fur: 'furB', belly: 'belly', mark: 'mark', skin: 'skin', eye: 'eye', glow: 'glow', frond: 'frond', horn: 'horn' },
      gait: 'tripod',
      eye: { rx: 3.3, ry: 3.7, at: [2.2, -1] },
      parts: {
        body: { fur: { roles: ['fur'], density: 0.06 }, prims: [E(-7, -12.5, 6.5, 6.5, { rz: 6.5 }), E(0, -13.5, 7.5, 7, { rz: 7 }), E(6.5, -13, 5.5, 6, { rz: 6 }),
          T([[-8, -18.5, 1.4], [-9.8, -21, 0.3]]), T([[-4, -19.6, 1.4], [-5.8, -22.2, 0.3]]), T([[0, -19.8, 1.4], [-1.6, -22.6, 0.3]]), T([[4, -18.4, 1.3], [2.8, -21, 0.3]])],
          post(P) { P.recolor((x, y, r) => r === 'fur' && inE(x, y, 0, -7, 11, 3.6), 'belly', 1); } },
        marks: { from: 'body', test: (x, y, t, r) => r === 'fur' && (inE(x, y, -6, -14, 3.6, 3) || inE(x, y, 2.5, -16, 3, 2.2) || inE(x, y, 5.5, -10.8, 2.2, 1.8) || inE(x, y, -10.5, -10.5, 2, 2.6)), role: 'mark' },
        head: { bias: 0.12, fur: { roles: ['fur'], density: 0.05 }, prims: [E(2.5, 1, 5.1, 4.7, { rz: 5 }), E(6.6, 3.3, 2.9, 2.5, { role: 'skin', rz: 2.6, cz: 0.6 }), T([[1, -3, 1.7], [-1.4, -6, 0.4]])],
          post(P) { P.set(8, 2, 'skin', 1); P.set(7, 5, 'skin', 1); P.set(6, 5, 'skin', 1); } },
        ear: { prims: [T([[0, 0, 1.4], [-2.6, 1, 1.7], [-4.8, 2.2, 0.8]])], post(P) { P.recolor((x, y, r) => r === 'fur' && x < -1 && x > -4 && y > 0.6, 'skin'); } },
        earFar: { prims: [T([[0, 0, 1.4], [-2.6, 1, 1.7], [-4.8, 2.2, 0.8]])], bias: -0.22 },
        leg: leg([[0, 0, 1.7], [0, 4.6, 1.5]], E(0.2, 5.3, 1.9, 1.1, { role: 'mark' })),
        legFar: leg([[0, 0, 1.7], [0, 4.6, 1.5]], E(0.2, 5.3, 1.9, 1.1, { role: 'mark' }), true),
        tail: { prims: [T([[0, 0, 1.1], [-2.5, -2, 0.9], [-5, -2.2, 0.8], [-7, -0.5, 0.7]], { role: 'horn' }),
          T([[-1.5, -1.4, 0.8], [-2, 2.6, 0.35]], { role: 'frond', cz: 0.5 }), T([[-3.5, -2.2, 0.85], [-4.2, 3.2, 0.35]], { role: 'frond', cz: 0.5 }), T([[-5.5, -2, 0.85], [-6.2, 4, 0.35]], { role: 'frond', cz: 0.5 }), T([[-7, -0.5, 0.85], [-7.6, 4.8, 0.35]], { role: 'frond', cz: 0.5 }),
          E(-7.6, 5.4, 1, 1, { role: 'glow', cz: 1 }), E(-4.2, 3.7, 0.9, 0.9, { role: 'glow', cz: 1 })] },
      },
      rig: [
        { id: 'earFar', part: 'earFar', parent: 'head', at: [0, -2.8], drive: 'ear', base: -0.25 },
        { id: 'l4', part: 'legFar', parent: 'body', at: [-6.5, -6.5], drive: 'leg', phase: 0.5 },
        { id: 'l5', part: 'legFar', parent: 'body', at: [1.5, -6.5], drive: 'leg', phase: 0 },
        { id: 'l6', part: 'legFar', parent: 'body', at: [9.5, -6.5], drive: 'leg', phase: 0.5 },
        { id: 'tail', part: 'tail', parent: 'body', at: [-13, -15.5], drive: 'tail' },
        { id: 'body', part: 'body', parent: null, at: [0, 0], drive: 'body' },
        { id: 'marks', part: 'marks', parent: 'body', at: [0, 0] },
        { id: 'l1', part: 'leg', parent: 'body', at: [-9, -6], drive: 'leg', phase: 0 },
        { id: 'l2', part: 'leg', parent: 'body', at: [-1, -6], drive: 'leg', phase: 0.5 },
        { id: 'l3', part: 'leg', parent: 'body', at: [7, -6], drive: 'leg', phase: 0 },
        { id: 'head', part: 'head', parent: 'body', at: [11, -13.5], drive: 'head' },
        { id: 'eye', part: 'eye', parent: 'head', at: [2.2, -1], drive: 'eye' },
        { id: 'ear', part: 'ear', parent: 'head', at: [1, -2.8], drive: 'ear', base: 0 },
      ],
      pose: { earUp: -0.35, earBack: -0.95, earPerk: 0.25, legAmp: 0.45, lift: 1.1, tailAmp: 0.16, tailFlee: -0.5, graze: 0.7 },
    },
    {
      id: 'tapir', name: 'Тапирчик', tag: 'Круглый тапир: светлое седло на тёмной шерсти, хоботок с семенной коробочкой, что светится.',
      ramps: { fur: 'furB', belly: 'belly', mark: 'mark', skin: 'skin', eye: 'eye', glow: 'glow', frond: 'frond' },
      gait: 'walk',
      eye: { rx: 3.2, ry: 3.7, at: [2.6, -1.2] },
      parts: {
        body: { fur: { roles: ['fur'], density: 0.05 }, prims: [E(-1, -12.5, 12, 9.5, { rz: 9 }), E(-8, -14, 6.5, 7.5, { rz: 7 }), T([[-4, -21, 1.6], [-6, -23.2, 0.4]]), T([[1, -21.6, 1.5], [-0.6, -24, 0.4]])] },
        marks: { from: 'body', test: (x, y, t, r) => r === 'fur' && x > -6 + (y + 13) * 0.3 && x < 1.5 + (y + 13) * 0.3 && y < -6.5, role: 'belly' },
        head: { fur: { roles: ['fur'], density: 0.05 }, prims: [E(3, 0.5, 5.5, 5.2, { rz: 5.5 }), T([[6, 0.5, 2.6], [8.5, 2.6, 2.1], [10, 5, 1.6]])],
          post(P) { P.set(8, 5, 'mark', 1); P.set(7, 5, 'mark', 1); } },
        pod: { prims: [E(0, 0.9, 2.1, 2.1, { role: 'glow', rz: 2 }), T([[-1.7, -0.8, 0.8], [1.7, -0.8, 0.8]], { role: 'frond', cz: 1.6 })] },
        ear: { prims: [E(0, -2.2, 2, 2.6, { rz: 1.5 })], post(P) { P.set(0, -3, 'skin', 2); P.set(0, -2, 'skin', 2); } },
        earFar: { prims: [E(0, -2.2, 2, 2.6, { rz: 1.5 })], bias: -0.22 },
        leg: leg([[0, 0, 2.8], [0, 4.4, 2.6]], E(0.5, 5, 2.9, 1.3, { role: 'mark' })),
        legFar: leg([[0, 0, 2.8], [0, 4.4, 2.6]], E(0.5, 5, 2.9, 1.3, { role: 'mark' }), true),
        tail: { prims: [E(-1, 0, 1.8, 1.6)] },
      },
      rig: [
        { id: 'earFar', part: 'earFar', parent: 'head', at: [-1, -3.8], drive: 'ear', base: -0.3 },
        { id: 'legFH', part: 'legFar', parent: 'body', at: [-5, -6.5], drive: 'leg', phase: 0 },
        { id: 'legFF', part: 'legFar', parent: 'body', at: [8.5, -6.5], drive: 'leg', phase: 0.5 },
        { id: 'tail', part: 'tail', parent: 'body', at: [-13.5, -15], drive: 'tail' },
        { id: 'body', part: 'body', parent: null, at: [0, 0], drive: 'body' },
        { id: 'marks', part: 'marks', parent: 'body', at: [0, 0] },
        { id: 'legNH', part: 'leg', parent: 'body', at: [-8, -6], drive: 'leg', phase: 0.5 },
        { id: 'legNF', part: 'leg', parent: 'body', at: [5.5, -6], drive: 'leg', phase: 0 },
        { id: 'head', part: 'head', parent: 'body', at: [8.5, -14.5], drive: 'head' },
        { id: 'pod', part: 'pod', parent: 'head', at: [10.2, 6.4], drive: 'pod' },
        { id: 'eye', part: 'eye', parent: 'head', at: [2.6, -1.2], drive: 'eye' },
        { id: 'ear', part: 'ear', parent: 'head', at: [1.2, -4.3], drive: 'ear', base: 0 },
      ],
      pose: { earUp: 0, earBack: -1.1, earPerk: 0.12, legAmp: 0.4, lift: 1.2, tailAmp: 0.3, tailFlee: -0.6, graze: 0.55 },
    },
    {
      id: 'frondhorn', name: 'Ветверог', tag: 'Олень-ветка: тонкие ноги, пятна-звёзды, рога как ветви акации — с листьями и светящимися семенами.',
      ramps: { fur: 'furA', belly: 'belly', mark: 'mark', skin: 'skin', eye: 'eye', glow: 'glow', frond: 'frond', horn: 'horn' },
      gait: 'walk',
      eye: { rx: 2.8, ry: 3.2, at: [3, -6.8] },
      parts: {
        body: { fur: { roles: ['fur'], density: 0.05 }, prims: [E(-2, -16.5, 9.5, 5.6, { rz: 5.5 }), E(5, -17, 5, 5.6, { rz: 5.5 }), E(-8, -17, 4.5, 5, { rz: 5 })],
          post(P) { P.recolor((x, y, r) => r === 'fur' && y > -13.2, 'belly', 1); } },
        marks: { from: 'body', test: (x, y, t, r) => r === 'fur' && [[-7, -19.5], [-4, -20.6], [-0.8, -20.6], [2.4, -20], [-5.5, -17.6], [-2, -18.2], [1.5, -17.4], [-8.6, -17]].some(([a, b]) => inE(x, y, a, b, 0.9, 0.9)), role: 'belly' },
        head: { fur: { roles: ['fur'], density: 0.04 }, prims: [T([[0, 0, 2.6], [1.5, -4, 2.2]]), E(3.2, -6.2, 4.4, 3.9, { rz: 4, cz: 0.5 }), E(7, -4.8, 2.6, 2.2, { rz: 2.4, cz: 0.8 })],
          post(P) { P.recolor((x, y, r) => r === 'fur' && inE(x, y, 7, -3.8, 2.6, 1.5), 'belly', 1); P.set(9, -5, 'mark', 1); P.set(8, -3, 'mark', 1); } },
        antlers: { prims: [T([[0, 0, 1], [-0.8, -4.5, 0.8], [-2.5, -8.5, 0.6]], { role: 'horn' }), T([[-0.6, -3.5, 0.7], [2.5, -6.5, 0.55]], { role: 'horn' }), T([[-1.8, -6.8, 0.6], [-5, -9, 0.5]], { role: 'horn' }),
          T([[2.6, -6.4, 0.75], [3, -3.2, 0.35]], { role: 'frond', cz: -0.5 }), T([[-5, -8.8, 0.75], [-5.6, -5.4, 0.35]], { role: 'frond', cz: -0.5 }), T([[-2.6, -8.4, 0.7], [-3.2, -5.4, 0.35]], { role: 'frond', cz: -0.5 }),
          E(2.7, -7.2, 1, 1, { role: 'glow', cz: 1 }), E(-5.2, -9.6, 1, 1, { role: 'glow', cz: 1 }), E(-2.6, -9.2, 0.9, 0.9, { role: 'glow', cz: 1 })] },
        ear: { prims: [T([[0, 0, 1.1], [-2.5, -1.2, 1.5], [-5, -1.8, 0.5]])], post(P) { P.recolor((x, y, r) => r === 'fur' && x < -1 && x > -4 && y > -1.6, 'skin'); } },
        earFar: { prims: [T([[0, 0, 1.1], [-2.5, -1.2, 1.5], [-5, -1.8, 0.5]])], bias: -0.22 },
        leg: leg([[0, 0, 2], [0.4, 5, 1.3], [-0.3, 9.5, 1], [0, 12, 0.9]], E(0.2, 12.6, 1.2, 0.9, { role: 'mark' })),
        legFar: leg([[0, 0, 2], [0.4, 5, 1.3], [-0.3, 9.5, 1], [0, 12, 0.9]], E(0.2, 12.6, 1.2, 0.9, { role: 'mark' }), true),
        tail: { prims: [T([[0, 0, 1.6], [-2, -1.5, 1.4], [-3, -2.2, 0.8]])], post(P) { P.recolor((x, y, r) => r === 'fur' && y > -1.2 + (x + 0) * -0.6, 'belly', 1); } },
      },
      rig: [
        { id: 'earFar', part: 'earFar', parent: 'head', at: [1, -8], drive: 'ear', base: -0.2 },
        { id: 'legFH', part: 'legFar', parent: 'body', at: [-5, -13.5], drive: 'leg', phase: 0 },
        { id: 'legFF', part: 'legFar', parent: 'body', at: [7, -13.5], drive: 'leg', phase: 0.5 },
        { id: 'tail', part: 'tail', parent: 'body', at: [-11.5, -19], drive: 'tail' },
        { id: 'body', part: 'body', parent: null, at: [0, 0], drive: 'body' },
        { id: 'marks', part: 'marks', parent: 'body', at: [0, 0] },
        { id: 'legNH', part: 'leg', parent: 'body', at: [-7, -13], drive: 'leg', phase: 0.5 },
        { id: 'legNF', part: 'leg', parent: 'body', at: [5, -13], drive: 'leg', phase: 0 },
        { id: 'head', part: 'head', parent: 'body', at: [7, -19.5], drive: 'head' },
        { id: 'antlers', part: 'antlers', parent: 'head', at: [1.5, -9.5], drive: 'antler' },
        { id: 'eye', part: 'eye', parent: 'head', at: [3, -6.8], drive: 'eye' },
        { id: 'ear', part: 'ear', parent: 'head', at: [2, -8.3], drive: 'ear', base: 0 },
      ],
      pose: { earUp: 0.1, earBack: -0.8, earPerk: 0.3, legAmp: 0.42, lift: 1.6, tailAmp: 0.1, tailFlee: -1.1, graze: 1.25 },
    },
    {
      id: 'fluff', name: 'Пушок', tag: 'Пушистый шар на крошечных лапках: светящиеся пятна по спине, усики-фонарики вместо ушей.',
      ramps: { fur: 'furB', belly: 'belly', mark: 'mark', skin: 'skin', eye: 'eye', glow: 'glow' },
      gait: 'waddle', headless: true,
      eye: { rx: 4, ry: 4.6, at: [6.5, -14] },
      parts: {
        body: { fur: { roles: ['fur'], density: 0.06 }, prims: (() => {
          const p = [E(0, -13, 12, 11.3, { rz: 11 })];
          for (let k = 0; k < 15; k++) {
            const a = Math.PI * (0.62 + k * 0.125), ca = Math.cos(a), sa = Math.sin(a), long = sa > 0.6 ? 1.12 : 1;
            if (ca > 0.55 && sa > -0.5 && sa < 0.55) continue; // the face stays smooth
            p.push(T([[ca * 9.8, -13 + sa * 9.3, 2.6], [ca * 12.6 * long - sa * 1.6, -13 + sa * 12 * long + ca * 1.6, 0.7]]));
          }
          return p;
        })(), post(P) { P.recolor((x, y, r) => r === 'fur' && inE(x, y, 7.5, -12, 5, 6), 'belly', 1); P.set(10, -7, 'mark', 1); P.set(11, -8, 'mark', 1); P.set(12, -11, 'skin', 3); } },
        marks: { from: 'body', test: (x, y, t, r) => r === 'fur' && [[-8, -16], [-6, -19.5], [-2.5, -21.8], [-9.5, -11.5], [-6, -13], [-3, -16.5], [0.5, -19]].some(([a, b], i) => inE(x, y, a, b, i % 3 ? 0.9 : 1.3, i % 3 ? 0.9 : 1.3)), role: 'glow', dt: 1 },
        ear: { prims: [T([[0, 0, 0.8], [0.3, -4, 0.7], [1.5, -7, 0.6]], { role: 'mark' }), E(1.8, -8.2, 1.7, 1.7, { role: 'glow', cz: 1 })] },
        earFar: { prims: [T([[0, 0, 0.8], [0.3, -4, 0.7], [1.5, -7, 0.6]], { role: 'mark' }), E(1.8, -8.2, 1.7, 1.7, { role: 'glow', cz: 1 })], bias: -0.22 },
        leg: { prims: [E(0, -0.8, 2.5, 1.6, { role: 'mark' })] },
        legFar: { prims: [E(0, -0.8, 2.5, 1.6, { role: 'mark' })], bias: -0.22 },
        tail: { prims: [E(-1.5, 0, 3, 3), T([[-2, 0, 2], [-6, -1, 0.5]]), T([[-2, -1, 1.8], [-4.5, -4.6, 0.5]]), T([[-2, 1, 1.8], [-5, 3.4, 0.5]])], fur: { roles: ['fur'], density: 0.06 } },
      },
      rig: [
        { id: 'earFar', part: 'earFar', parent: 'body', at: [0, -22.8], drive: 'ear', base: -0.35 },
        { id: 'legFH', part: 'legFar', parent: 'body', at: [-2, -1.2], drive: 'leg', phase: 0 },
        { id: 'legFF', part: 'legFar', parent: 'body', at: [7, -1.2], drive: 'leg', phase: 0.5 },
        { id: 'tail', part: 'tail', parent: 'body', at: [-11.5, -11], drive: 'tail' },
        { id: 'legNH', part: 'leg', parent: 'body', at: [-5, -0.8], drive: 'leg', phase: 0.5 },
        { id: 'legNF', part: 'leg', parent: 'body', at: [4, -0.8], drive: 'leg', phase: 0 },
        { id: 'body', part: 'body', parent: null, at: [0, 0], drive: 'body' },
        { id: 'marks', part: 'marks', parent: 'body', at: [0, 0] },
        { id: 'eye', part: 'eye', parent: 'body', at: [6.5, -14], drive: 'eye' },
        { id: 'ear', part: 'ear', parent: 'body', at: [4.5, -23], drive: 'ear', base: -0.1 },
      ],
      pose: { earUp: 0, earBack: -1.35, earPerk: 0.15, legAmp: 0, lift: 2, tailAmp: 0.2, tailFlee: -0.4, graze: 0.18 },
    },
  ];

  // ---------- building: every part painted once, the eye's states beside them ----------
  function build(d) {
    if (d.built) return d.built;
    const parts = {};
    for (const [name, def] of Object.entries(d.parts)) {
      if (def.from) continue;
      parts[name] = paint({ name, ...def });
    }
    for (const [name, def] of Object.entries(d.parts)) {
      if (!def.from) continue;
      const P = derive(parts[def.from], def.test, def.role, def.dt ?? (def.role === 'mark' ? -1 : 0)); P.name = name; parts[name] = P;
    }
    const eye = eyeSet(d.eye);
    const add = (n, P) => { if (P) { P.name = n; parts[n] = P; } };
    for (const s of ['open', 'wide']) add('eye_' + s, eye.sclera[s]);
    add('lid_half', eye.lid.half); add('lid_blink', eye.lid.blink);
    add('pupil', eye.pupil.calm); add('pupil_wide', eye.pupil.wide);
    return (d.built = { parts, eye });
  }
  // the part's pixels as colours of one palette; tone shift for far parts is already painted in
  function colours(d, P, pal, mode) {
    const out = new Uint8ClampedArray(P.w * P.h * 4);
    for (let q = 0; q < P.w * P.h; q++) {
      const r = P.role[q]; if (r === null) continue;
      let c;
      if (mode === 'white') c = [255, 255, 255];
      else if (mode === 'glow') { if (r !== 'glow' || P.tone[q] < 3) continue; c = hexRgb(pal.ramps[d.ramps.glow][Math.min(5, P.tone[q] + 1)]).map((v) => v * 255); }
      else c = hexRgb(pal.ramps[d.ramps[r] || r][P.tone[q]]).map((v) => v * 255);
      out[q * 4] = c[0]; out[q * 4 + 1] = c[1]; out[q * 4 + 2] = c[2]; out[q * 4 + 3] = 255;
    }
    return out;
  }
  // the index map the game bakes at load: R ramp index, G tone 0-5, B 0, A 255 where painted
  function indexMap(d, P) {
    const out = new Uint8ClampedArray(P.w * P.h * 4);
    for (let q = 0; q < P.w * P.h; q++) { const r = P.role[q]; if (r === null) continue; out[q * 4] = RAMP_INDEX[d.ramps[r] || r]; out[q * 4 + 1] = P.tone[q]; out[q * 4 + 3] = 255; }
    return out;
  }

  // ---------- motion: CreatureMotion.cs, line for line ----------
  class Motion {
    constructor() { Object.assign(this, { _flash: 0, _squash: 0, _flinch: 0, _startle: 0, _hold: 0, _land: 0, _clock: 0, _blink: 0, _nextBlink: 2400, Facing: 1, Away: 1, Phase: 0 }); }
    Hit(away, share) {
      this.Away = away === 0 ? this.Away : Math.sign(away);
      const whole = clamp(0.45 + 0.55 * share, 0, 1);
      this._flash = Math.max(this._flash, whole); this._squash = Math.max(this._squash, whole); this._flinch = Math.max(this._flinch, whole);
      this._startle = 1; this._hold = Math.max(this._hold, 70 * whole);
    }
    Step(ms, dx, grounded, landed, watch, stride) {
      this._clock += ms;
      this._flash = Math.max(0, this._flash - ms / 150); this._squash = Math.max(0, this._squash - ms / 380); this._flinch = Math.max(0, this._flinch - ms / 320);
      this._startle = Math.max(0, this._startle - ms / 900); this._hold = Math.max(0, this._hold - ms);
      this._land = landed ? 1 : Math.max(0, this._land - ms / 220);
      if (Math.abs(dx) > 0.05) this.Facing = Math.sign(dx); else if (watch && this._flinch === 0) this.Facing = watch;
      if (grounded) this.Phase += Math.abs(dx) / (stride || 9);
      this._blink = Math.max(0, this._blink - ms);
      if (this._clock >= this._nextBlink) { this._blink = 120; this._nextBlink = this._clock + 2200 + (this._clock * 7919) % 2600; }
    }
    get Held() { return this._hold > 0; }
    get Flash() { return this._flash * this._flash; }
    Scale(fleeing) {
      const t = 1 - this._squash, wob = this._squash > 0 ? Math.exp(-4 * t) * Math.cos(t * Math.PI * 2.6) : 0, land = this._land * this._land * 0.08, br = 0.015 * Math.sin(this._clock / 520);
      return [1 + 0.22 * wob + land + (fleeing ? 0.08 : 0), 1 - 0.2 * wob - land + br - (fleeing ? 0.06 : 0)];
    }
    Tilt(fleeing) { return this.Away * 0.38 * this._flinch * this._flinch + (fleeing ? this.Facing * 0.12 : 0); }
    get Shake() { return this._hold > 0 ? 1.2 * Math.sin(this._clock * 0.9) * (this._hold / 70) : 0; }
    get Eye() { return this._blink > 0 ? 0.1 : 1 + 0.45 * this._startle; }
    Ears(fleeing) { return Math.max(fleeing ? 1 : 0, this._startle); }
  }

  // ---------- pose: what each node does this frame, from the motion and what the creature is about ----------
  // s: { mode: 'graze'|'idle'|'walk'|'flee'|'notice', grounded, clock (ms), look: [x,y] body space or null, grazeT }
  function pose(d, m, s) {
    const P = d.pose, out = {}, ph = m.Phase, fle = s.mode === 'flee', ears = m.Ears(fle), moving = s.mode === 'walk' || fle;
    const tau = Math.PI * 2, clk = s.clock;
    // grazing: the head down, nibbling, up for a look every few seconds
    const g = s.mode === 'graze' ? 1 - Math.max(0, Math.sin(clk / 1400) - 0.75) * 4 : 0;
    const graze = clamp(g) * (s.grazeIn ?? 1), nib = graze * Math.max(0, Math.sin(clk / 70)) * 0.07;
    let bob = 0, roll = 0;
    if (d.gait === 'hop' && moving && s.grounded) bob = -Math.abs(Math.sin(ph * Math.PI)) * P.hop * (fle ? 1.2 : 1);
    if (d.gait === 'walk' || d.gait === 'tripod') bob = moving ? -Math.abs(Math.cos(ph * tau)) * 0.7 : 0;
    if (d.gait === 'waddle') { bob = moving ? -Math.abs(Math.sin(ph * Math.PI * 2)) * 1.6 : 0; roll = moving ? Math.sin(ph * tau) * 0.09 : 0; }
    const bodyA = (d.headless ? P.graze * graze + nib * 0.5 : 0) + roll;
    for (const n of d.rig) {
      let a = 0, x = 0, y = 0;
      if (n.drive === 'body') { a = bodyA; y = bob; }
      else if (n.drive === 'head') a = P.graze * graze + nib + (s.mode === 'notice' ? -0.16 + (s.look ? s.look[1] * 0.25 : 0) : 0) + (fle ? 0.22 : 0) - (m.Eye > 1.2 ? 0.08 : 0);
      else if (n.drive === 'ear') {
        const flick = Math.max(0, Math.sin(clk / 330 + n.at[0]) - 0.96) * 6;
        a = (n.base || 0) + P.earUp + (P.earBack - P.earUp) * ears - (s.mode === 'notice' ? P.earPerk : 0) - flick * 0.3 + (fle ? Math.sin(clk / 45) * 0.06 : 0) + graze * 0.15;
      } else if (n.drive === 'tail') a = Math.sin(clk / 650) * P.tailAmp + (fle ? P.tailFlee : 0) + (m.Eye > 1.2 ? P.tailFlee * 0.6 : 0);
      else if (n.drive === 'pod') a = Math.sin(clk / 380) * 0.12 + nib * 2;
      else if (n.drive === 'antler') a = Math.sin(clk / 900) * 0.03;
      else if (n.drive === 'leg') {
        if (!s.grounded) { a = n.hind ? -0.6 : (n.at[0] > 0 ? 0.55 : -0.45); y = -1; }
        else if (d.gait === 'hop') {
          const k = moving ? Math.sin(ph * Math.PI) : 0;
          a = n.hind ? -0.75 * k : 0.5 * k; y = n.hind ? 0 : -k * 1.5;
        } else if (d.gait === 'waddle') {
          const c = Math.cos((ph + (n.phase || 0)) * tau);
          y = moving ? -Math.max(0, c) * P.lift : 0; x = moving ? Math.sin((ph + (n.phase || 0)) * tau) * 1.2 : 0;
        } else {
          const amp = P.legAmp * (fle ? 1.35 : 1), w = (ph + (n.phase || 0)) * tau;
          a = moving ? Math.sin(w) * amp : 0; y = moving ? -Math.max(0, -Math.cos(w)) * P.lift : 0;
        }
        a -= bodyA;
      }
      out[n.id] = { a, x, y };
    }
    const eyeState = m.Eye < 0.3 ? 'blink' : m.Eye > 1.2 ? 'wide' : graze > 0.5 ? 'half' : 'open';
    return { nodes: out, eye: eyeState, look: s.look };
  }

  return { RAMP_INDEX, TONES, PALETTES, DESIGNS, build, colours, indexMap, Motion, pose, ramp, hexRgb };
});

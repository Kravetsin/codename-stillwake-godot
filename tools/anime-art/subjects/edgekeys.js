// The tablet edge's keys as frosted lenses (the edge-lenses ADR), as picked: the research key's glass (keylens.js), each motif a shade
// in it, lit sky from within while what it switches is on. Motifs are vector paths in a 16-unit square, rasterised 8 by 8 samples to a
// face pixel and boxed down to the 22-pixel face. No finish: at 22 pixels it would not show.
(function () {
'use strict';
const { canvas } = Anime;
const F = 22, RES = 8, N = F * RES, BOX = 16;
const clamp = (v) => Math.min(1, Math.max(0, v));
const mix = (a, b, t) => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];

// ---------- the motifs, in a 16-unit square, y down ----------
const rr = (x, y, w, h, r) => `M ${x + r} ${y} L ${x + w - r} ${y} Q ${x + w} ${y} ${x + w} ${y + r} L ${x + w} ${y + h - r} Q ${x + w} ${y + h} ${x + w - r} ${y + h} L ${x + r} ${y + h} Q ${x} ${y + h} ${x} ${y + h - r} L ${x} ${y + r} Q ${x} ${y} ${x + r} ${y} Z`;
const ring = (cx, cy, r) => { let d = ''; for (let k = 0; k < 48; k++) { const a = (k / 48) * Math.PI * 2; d += `${k ? 'L' : 'M'} ${cx + r * Math.cos(a)} ${cy + r * Math.sin(a)} `; } return d + 'Z'; };
const fill = (d) => ({ fill: d }), stroke = (d, w, thin = 0.3) => ({ stroke: d, w, thin });

/**
 * Each key's parts: 'solid' the shade, 'band' a paler part of it, 'lamp' clear cuts the light comes through while on.
 * The suit (pick `vest`): shoulders and short sleeves, a collar, a waist a little in, a round lamp port on the chest.
 * The air: three strokes that swell and taper, the top one curling up, the lowest down.
 * The build: three offset courses of bricks with mortar between, a corner knocked off one.
 * The journal (pick `open`): an open book, its pages curving up from the gutter, the leaves fanning at the foot, two lines a page.
 * The market (pick `onion`): a bulb from a pointed neck, two inner layers paler.
 */
const MOTIFS = {
  suit: {
    solid: [fill('M 5.5 1.7 Q 8 5.3 10.5 1.7 L 12.9 2.2 Q 15.5 2.9 15.7 5.6 L 15.6 9.3 Q 14.6 9.7 13.6 9.4 L 13.25 6.9 '
      + 'Q 12.3 7.5 12.4 9.2 L 11.95 12.4 Q 12.5 14.0 12.35 15.2 Q 8 15.9 3.65 15.2 Q 3.5 14.0 4.05 12.4 L 3.6 9.2 Q 3.7 7.5 2.75 6.9 '
      + 'L 2.4 9.4 Q 1.4 9.7 0.4 9.3 L 0.3 5.6 Q 0.5 2.9 3.1 2.2 Z')],
    lamp: [fill(ring(8, 7.7, 1.95))],
  },
  air: {
    solid: [
      stroke('M 0.5 5.9 C 4 5.3 8 6.1 11.4 5.5 C 14.3 5.0 14.5 1.4 12.0 1.3 C 10.5 1.25 9.9 2.7 10.9 3.4', 1.95),
      stroke('M 1.6 8.7 C 5.5 8.1 10.5 9.1 15.5 8.3', 1.9, 0.25),
      stroke('M 0.5 11.5 C 3.8 10.9 7.2 11.7 9.6 11.6 C 12.5 11.5 12.8 15.1 10.4 15.25 C 8.9 15.35 8.3 13.9 9.3 13.2', 1.8),
    ],
  },
  build: {
    solid: [
      fill(rr(0.4, 1.8, 7.1, 3.5, 0.55)), fill('M 8.4 1.8 L 14.6 1.8 Q 15.6 1.8 15.6 3.0 L 15.6 4.7 Q 15.6 5.3 15.0 5.3 L 8.95 5.3 Q 8.4 5.3 8.4 4.75 L 8.4 2.35 Q 8.4 1.8 8.95 1.8 Z'),
      fill(rr(0.4, 6.2, 3.1, 3.5, 0.5)), fill(rr(4.4, 6.25, 7.2, 3.5, 0.55)), fill(rr(12.5, 6.2, 3.1, 3.5, 0.5)),
      fill(rr(0.4, 10.6, 7.1, 3.5, 0.55)), fill('M 8.4 10.75 L 15.05 10.75 Q 15.6 10.75 15.6 11.3 L 15.6 13.75 Q 15.6 14.25 15.05 14.25 L 8.95 14.25 Q 8.4 14.25 8.4 13.7 L 8.4 12.2 L 9.6 10.75 Z'),
    ],
  },
  journal: {
    solid: [fill('M 7.75 4.6 Q 4.8 2.7 0.6 3.3 L 0.4 13.4 Q 4.6 12.7 7.75 14.7 Z'), fill('M 8.25 4.6 Q 11.2 2.7 15.4 3.3 L 15.6 13.4 Q 11.4 12.7 8.25 14.7 Z'),
      fill('M 0.4 12.6 Q 4.6 12.1 8 14.2 Q 11.4 12.1 15.6 12.6 L 15.6 14.2 Q 11.6 13.8 8 15.6 Q 4.4 13.8 0.4 14.2 Z')],
    band: [fill('M 7.6 4.9 Q 4.8 3.3 1.4 3.9 L 1.3 12.3 Q 4.8 11.9 7.6 13.4 Z'), fill('M 8.4 4.9 Q 11.2 3.3 14.6 3.9 L 14.7 12.3 Q 11.2 11.9 8.4 13.4 Z')],
    lamp: [fill('M 2.6 6.0 Q 4.6 5.6 6.6 6.4 L 6.6 7.2 Q 4.6 6.4 2.6 6.8 Z'), fill('M 2.6 8.6 Q 4.6 8.2 6.6 9.0 L 6.6 9.8 Q 4.6 9.0 2.6 9.4 Z'),
      fill('M 9.4 6.4 Q 11.4 5.6 13.4 6.0 L 13.4 6.8 Q 11.4 6.4 9.4 7.2 Z'), fill('M 9.4 9.0 Q 11.4 8.2 13.4 8.6 L 13.4 9.4 Q 11.4 9.0 9.4 9.8 Z')],
  },
  market: {
    solid: [fill('M 8 0.8 Q 8.5 2.6 9.7 3.7 Q 14.9 6.5 14.3 11.0 Q 13.5 15.2 8 15.4 Q 2.5 15.2 1.7 11.0 Q 1.1 6.5 6.3 3.7 Q 7.5 2.6 8 0.8 Z')],
    band: [fill('M 8 4.6 Q 4.0 7.4 4.6 11.2 Q 5.2 14.0 8 14.4 Q 5.9 13.4 5.8 11.0 Q 5.6 7.6 8 4.6 Z'), fill('M 8 4.6 Q 12.0 7.4 11.4 11.2 Q 10.8 14.0 8 14.4 Q 10.1 13.4 10.2 11.0 Q 10.4 7.6 8 4.6 Z')],
  },
};
/** Seeds per key, so the glass's grain differs between keys but never between runs. */
const SEED = { air: 11, build: 23, suit: 37, journal: 59, market: 71 };
/** Two short scratches per lens, placed by hand: x, y, length, slope. */
const SCRATCHES = {
  air: [[2, 17, 6, 0.4], [15, 2, 4, 0.6]],
  build: [[3, 3, 5, 0.5], [14, 18, 5, -0.3]],
  suit: [[2, 15, 5, 0.5], [16, 4, 4, 0.7]],
  journal: [[14, 17, 5, -0.4], [2, 3, 5, 0.5]],
  market: [[3, 18, 5, 0.3], [15, 3, 4, 0.6]],
};

// ---------- rasterising: Uint8Array N×N, RES samples to a face pixel ----------
/** A path string (M L Q C Z, absolute) flattened into polygons. */
function flatten(d) {
  const tok = d.match(/[MLQCZ]|-?\d*\.?\d+(?:e-?\d+)?/g), polys = [];
  let i = 0, cmd = 'M', x = 0, y = 0, cur = null;
  const num = () => parseFloat(tok[i++]);
  while (i < tok.length) {
    if (/[MLQCZ]/.test(tok[i])) { cmd = tok[i++]; if (cmd === 'Z') { if (cur) polys.push(cur); cur = null; continue; } }
    if (cmd === 'M') { if (cur) polys.push(cur); x = num(); y = num(); cur = [[x, y]]; cmd = 'L'; }
    else if (cmd === 'L') { x = num(); y = num(); cur.push([x, y]); }
    else if (cmd === 'Q') { const cx = num(), cy = num(), ex = num(), ey = num(); for (let k = 1; k <= 14; k++) { const t = k / 14, u = 1 - t; cur.push([u * u * x + 2 * u * t * cx + t * t * ex, u * u * y + 2 * u * t * cy + t * t * ey]); } x = ex; y = ey; }
    else if (cmd === 'C') { const ax = num(), ay = num(), bx = num(), by = num(), ex = num(), ey = num(); for (let k = 1; k <= 24; k++) { const t = k / 24, u = 1 - t; cur.push([u * u * u * x + 3 * u * u * t * ax + 3 * u * t * t * bx + t * t * t * ex, u * u * u * y + 3 * u * u * t * ay + 3 * u * t * t * by + t * t * t * ey]); } x = ex; y = ey; }
    else i++;
  }
  if (cur) polys.push(cur);
  return polys;
}
/** Fills polygons (in samples) into m by the nonzero rule, a sample in when its centre is. */
function fillPolys(m, polys) {
  let y0 = Infinity, y1 = -Infinity;
  for (const p of polys) for (const [, y] of p) { y0 = Math.min(y0, y); y1 = Math.max(y1, y); }
  for (let j = Math.max(0, Math.floor(y0)); j <= Math.min(N - 1, Math.ceil(y1)); j++) {
    const sy = j + 0.5, xs = [];
    for (const p of polys) for (let k = 0; k < p.length; k++) {
      const a = p[k], b = p[(k + 1) % p.length];
      if ((a[1] <= sy) !== (b[1] <= sy)) xs.push([a[0] + ((sy - a[1]) / (b[1] - a[1])) * (b[0] - a[0]), b[1] > a[1] ? 1 : -1]);
    }
    xs.sort((u, v) => u[0] - v[0]);
    let w = 0;
    for (let k = 0; k < xs.length - 1; k++) {
      w += xs[k][1];
      if (!w) continue;
      for (let i = Math.max(0, Math.ceil(xs[k][0] - 0.5)); i < Math.min(N, Math.ceil(xs[k + 1][0] - 0.5)); i++) m[j * N + i] = 1;
    }
  }
}
/** A pen stroke (in samples): discs stamped along the line, swelling to w in the middle and tapering to `thin` of it at the ends. */
function strokeLine(m, pts, w, thin) {
  const len = [0];
  for (let k = 1; k < pts.length; k++) len.push(len[k - 1] + Math.hypot(pts[k][0] - pts[k - 1][0], pts[k][1] - pts[k - 1][1]));
  const L = len[len.length - 1], steps = Math.ceil(L / 0.5);
  let k = 0;
  for (let s = 0; s <= steps; s++) {
    const at = (s / steps) * L;
    while (k < pts.length - 2 && len[k + 1] < at) k++;
    const t = (at - len[k]) / (len[k + 1] - len[k] || 1), cx = pts[k][0] + (pts[k + 1][0] - pts[k][0]) * t, cy = pts[k][1] + (pts[k + 1][1] - pts[k][1]) * t;
    const u = at / L, r = (w / 2) * (thin + (1 - thin) * Math.pow(Math.sin(Math.PI * u), 0.6)) * (1.08 - 0.16 * u);
    for (let j = Math.max(0, Math.floor(cy - r)); j <= Math.min(N - 1, Math.ceil(cy + r)); j++)
      for (let i = Math.max(0, Math.floor(cx - r)); i <= Math.min(N - 1, Math.ceil(cx + r)); i++)
        if ((i + 0.5 - cx) ** 2 + (j + 0.5 - cy) ** 2 < r * r) m[j * N + i] = 1;
  }
}
/** The share of each face pixel a key's part covers, its 16-unit square at x, y, s pixels wide: Float32Array 22×22. */
function cover(key, part, box) {
  const o = new Float32Array(F * F);
  if (!MOTIFS[key][part]) return o;
  const m = new Uint8Array(N * N), k = (box.s / BOX) * RES, T = ([u, v]) => [box.x * RES + u * k, box.y * RES + v * k];
  for (const item of MOTIFS[key][part]) {
    if (item.fill) fillPolys(m, flatten(item.fill).map((p) => p.map(T)));
    else strokeLine(m, flatten(item.stroke)[0].map(T), item.w * k, item.thin);
  }
  for (let j = 0; j < N; j++) for (let i = 0; i < N; i++) if (m[j * N + i]) o[((j / RES) | 0) * F + ((i / RES) | 0)] += 1 / (RES * RES);
  return o;
}

// ---------- the lens ----------
/** The lens of one key in one state, as a 22-pixel canvas. */
function lens(key, state) {
  const down = state === 'down', on = state === 'on', hover = state === 'hover', dim = state === 'dim';
  const box = { x: 4, y: 4 + (down ? 1 : 0), s: 14 }; // the shade a little smaller than the glass, sunk a pixel while held
  const solid = cover(key, 'solid', box), hollow = cover(key, 'lamp', box), band = cover(key, 'band', box);
  const k = on ? 1 : 0, gain = down ? 0.8 : dim ? 0.9 : 1;
  let seed = 90173 + SEED[key];
  const rnd = () => { seed = (seed * 1664525 + 1013904223) >>> 0; return seed / 4294967296; };
  const marks = new Set();
  for (const [x, y, n, slope] of SCRATCHES[key]) for (let t = 0; t < n; t++) marks.add((x + t) + ',' + (y + Math.round(t * slope)));
  const made = canvas(F, F), c = made.getContext('2d'), img = c.createImageData(F, F);
  const at = (a, i, j) => (i < 0 || j < 0 || i >= F || j >= F ? 0 : a[j * F + i]);
  for (let j = 0; j < F; j++) for (let i = 0; i < F; i++) {
    const p = j * F + i;
    // Frost: the shade a little soft, as a thing seen through ground glass.
    const soft = (a) => 0.6 * a[p] + 0.1 * (at(a, i - 1, j) + at(a, i + 1, j) + at(a, i, j - 1) + at(a, i, j + 1));
    const m = clamp(soft(solid) - soft(hollow) - 0.45 * soft(band));
    const x = i + 0.5 - 11, y = j + 0.5 - 11 - (down ? 1 : 0), radial = clamp(Math.hypot(x, y + 1.5) / 14);
    const glow = (1 - radial) * (dim ? 0.7 : 1);
    let dark0 = [38, 43, 52], dark1 = [52, 58, 69];
    if (hover) { dark0 = [50, 58, 73]; dark1 = [78, 91, 112]; } // brighter, and cooler
    if (dim) { dark0 = [30, 32, 36]; dark1 = [44, 46, 51]; } // greyed, its shade still there
    let col = mix(mix(dark0, dark1, glow), mix([14, 44, 64], [118, 204, 244], glow), k);
    col = mix(col, mix([12, 13, 17], [5, 26, 40], k), m * 0.88);
    if (on) col = mix(col, [214, 242, 255], clamp(soft(hollow)) * 0.85);
    const v = 1 - 0.28 * radial * radial, add = (rnd() * 2 - 1) * (2.5 + 5 * k) + (marks.has(i + ',' + j) ? 10 + 16 * k : 0);
    let [r, g, b] = col.map((n) => (n * v + add) * gain);
    if (j === 0) { if (down) { r *= 0.45; g *= 0.45; b *= 0.45; } else { r += 14; g += 14; b += 16; } }
    if (j === F - 1) { r *= 0.7; g *= 0.7; b *= 0.7; }
    for (const [o, n] of [[0, r], [1, g], [2, b]]) img.data[p * 4 + o] = Math.max(0, Math.min(255, Math.round(n)));
    img.data[p * 4 + 3] = 255;
  }
  c.putImageData(img, 0, 0);
  return made;
}

Subjects.edgekeys = function () {
  for (const key of Object.keys(MOTIFS)) for (const state of ['idle', 'hover', 'down', 'on', 'dim']) out(`key_${key}_${state}`, lens(key, state));
};
})();

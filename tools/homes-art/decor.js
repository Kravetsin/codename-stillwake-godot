// The decor of an arc-civilisation home, drawn pixel by pixel into a Pic: outlined 3-4 tone pieces, glowing parts emit,
// every glowing thing adds a light, every moving thing adds an emitter for the page's live layer.
const { hex, mix, mul, hash } = require('./engine');

const pal = (...h) => h.map(hex);
const PAL = {
  alloy: [[20, 29, 36], [38, 53, 62], [56, 76, 87], [78, 103, 114], [110, 138, 147], [166, 194, 198]].map((c) => c.map((v) => v / 255)),
  wood: pal('#22140d', '#3e271a', '#5c3a25', '#7c5234', '#a0704a'),
  woodOld: pal('#231c17', '#40342a', '#5e4c3c', '#7c6650', '#9c8468'),
  red: pal('#220b10', '#4a1820', '#74262c', '#9c3a36', '#c45c48'),
  indigo: pal('#11132a', '#22284e', '#333c76', '#4b589e', '#7385c4'),
  ochre: pal('#3a2810', '#6a4a18', '#a07a2c', '#cca048', '#ecd088'),
  ceramic: pal('#2a1912', '#5a3626', '#86543a', '#ac7452', '#cc9a70'),
  glaze: pal('#0d2726', '#1b4b49', '#2a716c', '#459c94', '#7cc8bc'),
  copper: pal('#2e1608', '#5c3218', '#8a5228', '#b9783a', '#e3a656'),
  stone: pal('#1c1c22', '#33333b', '#494953', '#61616d', '#7e7e8a'),
  paper: pal('#3a3226', '#6a5e48', '#9a8a68', '#c0ae86', '#ddd0aa'),
  linen: pal('#29272a', '#4d4a4a', '#77726c', '#a19a90', '#c8c0b4'),
  plant: pal('#0e2216', '#1c4028', '#2c6038', '#46844a', '#78b064'),
  dry: pal('#241a10', '#4a3420', '#6e5030', '#8e6c44', '#ae8c60'),
};
const A = PAL.alloy; // [ol, dk, sh, base, lt, shine]
const ARC = { core: hex('#f4feff'), hi: hex('#c4ecff'), mid: hex('#70c6ff'), lo: hex('#3a7ad8'), dk: hex('#16305a'), light: [0.56, 0.78, 1] };
const HOT = { hi: hex('#fff2c4'), mid: hex('#ff9c46'), lo: hex('#c44c1a'), dk: hex('#5a1e0c'), light: [1, 0.6, 0.3] };
const HOLO = { hi: hex('#d6fbff'), mid: hex('#62e4f4'), lo: hex('#2a8ea8'), light: [0.4, 0.9, 1] };
const TEAL = { hi: hex('#b4fff0'), mid: hex('#4fe6c8'), lo: hex('#1c8a7e'), dk: hex('#0e3f3c'), light: [0.35, 1, 0.85] };
const VIO = { hi: hex('#ecdcff'), mid: hex('#b48cff'), lo: hex('#6a46c8'), light: [0.7, 0.5, 1] };
const DUST = hex('#8e877a'), WEB = hex('#b8b4ac'), GLASS = hex('#26384a'), GLASS_D = hex('#141e2a');

// ---- primitives ----
const S = (p, x, y, c) => p.set(x, y, c);
const G = (p, x, y, c, k = 1) => p.glow(x, y, c, k);
function R(p, x, y, w, h, c) { for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) p.set(x + i, y + j, c); }
function blend(p, x, y, c, a) {
  x |= 0; y |= 0; if (!p.inb(x, y)) return; const i = y * p.w.PW + x;
  const cur = [p.alb[i * 3], p.alb[i * 3 + 1], p.alb[i * 3 + 2]]; p.alb.set(mix(cur, c, a), i * 3);
}
function line(p, x0, y0, x1, y1, f) {
  x0 |= 0; y0 |= 0; x1 |= 0; y1 |= 0; const dx = Math.abs(x1 - x0), dy = -Math.abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1; let e = dx + dy, k = 0;
  for (;;) { f(x0, y0, k++); if (x0 === x1 && y0 === y1) break; const e2 = 2 * e; if (e2 >= dy) { e += dy; x0 += sx; } if (e2 <= dx) { e += dx; y0 += sy; } }
}
// a bevelled box: outline, lit top and left, shaded bottom and right
function box(p, x, y, w, h, c, o = {}) {
  for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) {
    const edge = i === 0 || j === 0 || i === w - 1 || j === h - 1;
    if (o.round && (i === 0 || i === w - 1) && (j === 0 || j === h - 1)) continue;
    let col = edge ? c[0] : j === 1 ? c[3] : j === h - 2 ? c[1] : i === 1 ? c[3] : i === w - 2 ? c[1] : c[2];
    if (!edge && o.grain && j > 1 && j < h - 2 && hash((x + i + (y + j) * 3) >> 2, (y + j) * 3, 7) < 0.14) col = c[1];
    S(p, x + i, y + j, col);
  }
  if (!o.nohi && w > 3 && h > 3) S(p, x + 1, y + 1, c[4]);
}
// alloy box: the alloy's six steps
function abox(p, x, y, w, h, o) { box(p, x, y, w, h, [A[0], A[1], A[3], A[4], A[5]], o); }
function dust(p, x, y, w, seed = 0) { for (let i = 0; i < w; i++) if (hash(x + i, y, 91 + seed) < 0.7) blend(p, x + i, y, DUST, 0.55); }
function arcE(p, a, b, o = {}) { p.emitters.push({ kind: 'arc', a, b, rate: o.rate || 0.02, life: o.life || 6, w: o.w || 0.8, pal: o.pal || 'cyan', light: o.light || 0 }); }
function globe(p, cx, cy, r, lit, o = {}) {
  for (let y = -r; y <= r; y++) for (let x = -r; x <= r; x++) {
    const d = Math.sqrt(x * x + y * y); if (d > r + 0.3) continue;
    const X = cx + x, Y = cy + y, rim = d > r - 0.9;
    if (lit) {
      const col = rim ? ARC.lo : d < r * 0.35 ? ARC.core : d < r * 0.7 ? ARC.hi : ARC.mid;
      G(p, X, Y, col, rim ? 0.55 : d < r * 0.35 ? 1.1 : 0.85);
    } else S(p, X, Y, rim ? A[0] : (x < 0 && y < 0 && d < r * 0.6) ? GLASS : GLASS_D);
  }
  if (!lit) { S(p, cx - Math.ceil(r / 2), cy - Math.ceil(r / 2), hex('#5a7890')); }
  // the electrodes: two pins meeting near the middle
  if (r >= 3 && !lit) { S(p, cx, cy - 1, A[4]); S(p, cx, cy + 1, A[4]); }
  if (lit) { p.light({ x: cx + 0.5, y: cy + 0.5, col: ARC.light, r: o.r || 7, k: o.k || 1.05, p: 1.5 }); p.emitters.push({ kind: 'pulse', x: cx + 0.5, y: cy + 0.5, r: (o.r || 7) * 4, col: [120, 200, 255], amp: 0.08 }); arcE(p, [cx - r + 1.5, cy + 0.5], [cx + r - 0.5, cy + 0.5], { rate: o.rate || 0.03, life: 4, w: 0.6 }); }
}

// ================= LIGHT =================
const PIECES = {};
PIECES.ceilLamp = { w: 15, h: 18, anchor: 'ceiling', glow: 'arc', draw(p, x, y, o) {
  R(p, x + 6, y, 3, 5, A[0]); S(p, x + 7, y + 1, A[4]); S(p, x + 7, y + 3, A[4]);
  abox(p, x + 2, y + 5, 11, 4, { nohi: false }); S(p, x + 1, y + 8, A[0]); S(p, x + 13, y + 8, A[0]);
  for (const i of [3, 6, 9, 11]) S(p, x + i, y + 7, A[2]);
  globe(p, x + 7, y + 13, 4, o.lit, { r: 8 });
  S(p, x + 7, y + 17, A[0]); dust(p, x + 3, y + 5, 9);
} };
PIECES.sconce = { w: 10, h: 16, anchor: 'wall', glow: 'arc', draw(p, x, y, o) {
  abox(p, x + 3, y + 3, 4, 11); S(p, x + 4, y + 8, ARC.lo);
  // the tube on a short arm
  R(p, x + 6, y + 9, 2, 1, A[0]);
  for (let j = 0; j < 8; j++) { const lit = o.lit; const yy = y + 2 + j;
    if (lit) { G(p, x + 7, yy, j === 0 || j === 7 ? ARC.mid : ARC.hi, 0.9); G(p, x + 8, yy, j % 3 === 1 ? ARC.core : ARC.mid, 0.8); }
    else { S(p, x + 7, yy, GLASS); S(p, x + 8, yy, GLASS_D); } }
  R(p, x + 6, y + 1, 4, 1, A[0]); R(p, x + 6, y + 10, 4, 1, A[0]); S(p, x + 7, y + 1, A[4]);
  if (o.lit) { p.light({ x: x + 8, y: y + 6, col: ARC.light, r: 5.5, k: 0.85 }); arcE(p, [x + 7.5, y + 2.5], [x + 8.5, y + 9.5], { rate: 0.025, life: 4, w: 0.5 }); p.emitters.push({ kind: 'pulse', x: x + 8, y: y + 6, r: 20, col: [120, 200, 255], amp: 0.07 }); }
} };
PIECES.floorLamp = { w: 13, h: 42, anchor: 'floor', glow: 'arc', draw(p, x, y, o) {
  // tripod foot, pole, a cage of ribs round the globe
  R(p, x + 1, y + 41, 11, 1, A[0]); abox(p, x + 3, y + 38, 7, 3); line(p, x + 1, y + 41, x + 4, y + 37, (a, b) => S(p, a, b, A[0])); line(p, x + 11, y + 41, x + 8, y + 37, (a, b) => S(p, a, b, A[0]));
  R(p, x + 5, y + 12, 3, 26, A[0]); for (let j = 13; j < 38; j++) { S(p, x + 6, y + j, j % 6 === 0 ? A[5] : A[3]); }
  R(p, x + 4, y + 24, 5, 2, A[0]); S(p, x + 5, y + 24, A[4]);
  globe(p, x + 6, y + 6, 4, o.lit, { r: 8 });
  for (const dx of [-5, 5]) for (let j = 1; j < 12; j++) S(p, x + 6 + dx + (j < 3 ? -Math.sign(dx) * (3 - j) : j > 9 ? -Math.sign(dx) * (j - 9) : 0), y + j, A[1]);
  R(p, x + 3, y, 7, 1, A[0]); R(p, x + 3, y + 12, 7, 1, A[0]); dust(p, x + 3, y - 1, 7);
} };
PIECES.lantern = { w: 9, h: 13, anchor: 'floor', glow: 'arc', draw(p, x, y, o) {
  for (const [a, b] of [[3, 0], [4, 0], [5, 0], [2, 1], [6, 1], [2, 2], [6, 2]]) S(p, x + a, y + b, A[0]);
  abox(p, x + 1, y + 3, 7, 2, { nohi: true });
  for (let j = 5; j < 11; j++) for (let i = 1; i < 8; i++) {
    const rib = i === 1 || i === 7;
    if (rib) S(p, x + i, y + j, i === 4 ? A[2] : A[0]);
    else if (o.lit) { const d = Math.abs(i - 4) + Math.abs(j - 7.5); G(p, x + i, y + j, d < 1.6 ? ARC.core : d < 3 ? ARC.hi : ARC.mid, d < 1.6 ? 1.1 : 0.8); }
    else S(p, x + i, y + j, i < 4 ? GLASS : GLASS_D);
  }
  abox(p, x, y + 11, 9, 2, { nohi: true });
  if (o.lit) { p.light({ x: x + 4.5, y: y + 8, col: ARC.light, r: 5, k: 0.8 }); arcE(p, [x + 2.5, y + 8], [x + 6.5, y + 7], { rate: 0.03, life: 4, w: 0.45 }); }
} };
PIECES.lightWindow = { w: 30, h: 32, anchor: 'wall', glow: 'arc', draw(p, x, y, o) {
  // an arched niche cut in the rock with a pale arc panel behind the slats: a window onto a sky that is not there
  const inArch = (i, j) => { const cx = 14.5, r = 12; if (j < 12) { const dx = i - cx, dy = j - 12; return dx * dx + dy * dy <= r * r; } return i >= 2.5 && i <= 26.5 && j <= 29; };
  for (let j = 0; j < 31; j++) for (let i = 0; i < 30; i++) {
    const inn = inArch(i, j), out = inArch(i, j) || inArch(i - 1, j) || inArch(i + 1, j) || inArch(i, j - 1) || inArch(i, j + 1);
    if (!out) continue;
    if (!inn) { S(p, x + i, y + j, PAL.stone[0]); continue; }
    const t = j / 30;
    if (o.lit) G(p, x + i, y + j, mix(hex('#b4dcf6'), hex('#4a78b8'), t), 0.3 + 0.2 * (1 - t));
    else S(p, x + i, y + j, mix(hex('#1a2430'), hex('#10161e'), t));
  }
  // shutters: left one open against the wall, right one half shut, slats casting stripes
  for (let j = 10; j < 30; j++) { S(p, x + 3, y + j, PAL.wood[1]); }
  for (let j = 8; j < 30; j++) for (let i = 16; i < 27; i++) {
    const slat = (j - 8) % 3;
    if (i === 16 || i === 26 || j === 8 || j === 29) S(p, x + i, y + j, PAL.wood[0]);
    else if (slat === 0) S(p, x + i, y + j, PAL.wood[3]); else if (slat === 1) S(p, x + i, y + j, PAL.wood[2]);
  }
  for (let j = 10; j < 30; j += 7) S(p, x + 17, y + j, A[4]);
  // the sill
  box(p, x, y + 29, 30, 3, PAL.stone, { nohi: true }); dust(p, x + 1, y + 28, 14);
  if (o.lit) { p.light({ x: x + 10, y: y + 18, col: [0.66, 0.84, 1], r: 6.5, k: 0.75 }); p.emitters.push({ kind: 'motes', x0: x + 4, y0: y + 6, x1: x + 15, y1: y + 28, col: [210, 236, 255], n: 6 }); }
} };
PIECES.brokenLamp = { w: 15, h: 17, anchor: 'ceiling', glow: 'arc', draw(p, x, y, o) {
  R(p, x + 6, y, 3, 5, A[0]); S(p, x + 7, y + 2, A[4]);
  abox(p, x + 2, y + 5, 11, 4); line(p, x + 2, y + 8, x + 5, y + 6, (a, b) => S(p, a, b, A[0]));
  // half a globe: jagged glass, bare electrodes
  for (let yy = -4; yy <= 4; yy++) for (let xx = -4; xx <= 4; xx++) {
    const d = Math.sqrt(xx * xx + yy * yy); if (d > 4.3) continue;
    const gone = xx > -1 + (yy % 2 === 0 ? 1 : 0) && yy > -2;
    if (gone) continue; S(p, x + 7 + xx, y + 13 + yy, d > 3.3 ? A[0] : xx < -1 && yy < 0 ? GLASS : GLASS_D);
  }
  R(p, x + 7, y + 10, 1, 5, A[4]); R(p, x + 9, y + 12, 1, 3, A[4]); S(p, x + 9, y + 15, A[0]);
  for (const [a, b] of [[3, 16], [9, 16], [12, 16]]) S(p, x + a, y + b + 0, GLASS); // shards fell off
  G(p, x + 8, y + 13, ARC.mid, o.lit ? 0.6 : 0.25);
  p.emitters.push({ kind: 'sputter', x: x + 8, y: y + 13, r: 46, a: [x + 7.5, y + 14.5], b: [x + 9.5, y + 14.5] });
} };
PIECES.garland = { w: 64, h: 12, anchor: 'ceiling', glow: 'arc', draw(p, x, y, o) {
  // a string of little arc beads sagging under the ceiling; a few dead
  const n = 64;
  for (let k = 0; k <= n; k++) {
    const t = k / n, X = x + k, Y = Math.round(y + 1 + 9 * 4 * t * (1 - t));
    S(p, X, Y, A[1]);
    if (k % 6 === 3) {
      const dead = hash(k, 7, 3) < 0.25;
      if (o.lit && !dead) { G(p, X, Y + 1, ARC.core, 1); G(p, X - 1, Y + 1, ARC.mid, 0.6); G(p, X + 1, Y + 1, ARC.mid, 0.6); G(p, X, Y + 2, ARC.hi, 0.8); }
      else { S(p, X, Y + 1, GLASS); S(p, X, Y + 2, GLASS_D); }
      if (o.lit && !dead && k % 18 === 3) p.light({ x: X, y: Y + 2, col: ARC.light, r: 2.6, k: 0.45 });
    }
  }
  if (o.lit) p.emitters.push({ kind: 'twinkle', pts: Array.from({ length: 11 }, (_, i) => i * 6 + 3).filter((k) => hash(k, 7, 3) >= 0.25).map((k) => [x + k, y + 2 + Math.round(9 * 4 * (k / n) * (1 - k / n))]) });
} };

// ================= FURNITURE =================
PIECES.table = { w: 30, h: 13, anchor: 'floor', draw(p, x, y, o) {
  box(p, x, y, 30, 4, PAL.wood, { grain: true }); R(p, x + 1, y + 4, 28, 1, PAL.wood[0]);
  for (const lx of [2, 25]) { R(p, x + lx, y + 5, 3, 8, A[0]); for (let j = 5; j < 12; j++) S(p, x + lx + 1, y + j, j === 6 ? A[5] : A[3]); R(p, x + lx - 1, y + 12, 5, 1, A[0]); }
  R(p, x + 5, y + 8, 20, 1, A[1]); S(p, x + 14, y + 8, ARC.lo);
  if (!o.noDust) dust(p, x + 1, y - 1, 28);
} };
PIECES.stool = { w: 9, h: 9, anchor: 'floor', draw(p, x, y, o) {
  box(p, x, y, 9, 3, PAL.wood, { nohi: false });
  for (let j = 3; j < 9; j++) { S(p, x + 1 + (j > 6 ? -1 : 0), y + j, PAL.wood[0]); S(p, x + 7 + (j > 6 ? 1 : 0), y + j, PAL.wood[0]); S(p, x + 2 + (j > 6 ? -1 : 0), y + j, PAL.wood[2]); S(p, x + 6 + (j > 6 ? 1 : 0), y + j, PAL.wood[1]); }
  R(p, x + 2, y + 5, 5, 1, PAL.wood[1]); if (!o.noDust) dust(p, x + 1, y - 1, 7);
} };
PIECES.chair = { w: 11, h: 17, anchor: 'floor', draw(p, x, y, o) {
  const f = o.flip, X = (i) => f ? x + 10 - i : x + i;
  for (let j = 0; j < 17; j++) for (let i = 0; i < 3; i++) { if (j > 9 && j < 17) {} S(p, X(i), y + j, i === 0 || j === 0 ? PAL.wood[0] : i === 1 ? PAL.wood[3] : PAL.wood[1]); }
  for (const j of [3, 6]) for (let i = 2; i < 4; i++) S(p, X(i), y + j, PAL.wood[1]);
  S(p, X(1), y + 1, PAL.wood[4]);
  for (let i = 0; i < 11; i++) { S(p, X(i), y + 9, PAL.wood[0]); S(p, X(i), y + 10, i === 10 ? PAL.wood[0] : PAL.wood[3]); S(p, X(i), y + 11, PAL.wood[0]); }
  // a cushion, red, worn
  for (let i = 3; i < 10; i++) { S(p, X(i), y + 8, i === 3 || i === 9 ? PAL.red[0] : PAL.red[3]); S(p, X(i), y + 9, PAL.red[1]); }
  for (let j = 12; j < 17; j++) { S(p, X(1), y + j, PAL.wood[0]); S(p, X(9), y + j, PAL.wood[0]); S(p, X(10), y + j, PAL.wood[1]); }
  if (!o.noDust) { dust(p, Math.min(X(4), X(9)), y + 7, 6); dust(p, Math.min(X(0), X(2)), y - 1, 3); }
} };
PIECES.bench = { w: 28, h: 9, anchor: 'floor', draw(p, x, y, o) {
  box(p, x, y, 28, 3, PAL.woodOld, { grain: true });
  for (const lx of [2, 23]) { R(p, x + lx, y + 3, 3, 6, PAL.woodOld[0]); R(p, x + lx + 1, y + 3, 1, 5, PAL.woodOld[2]); }
  R(p, x + 5, y + 5, 18, 1, PAL.woodOld[1]); dust(p, x + 1, y - 1, 26);
  // a folded red throw left on it
  box(p, x + 16, y - 3, 9, 3, PAL.red, { nohi: true }); S(p, x + 17, y - 2, PAL.red[4]);
} };
PIECES.bed = { w: 34, h: 18, anchor: 'floor', draw(p, x, y, o) {
  // headboard with a reading light still alive in its crest
  box(p, x, y, 4, 18, PAL.wood, { grain: true }); abox(p, x - 1, y, 6, 2, { nohi: true });
  if (o.lit) { G(p, x + 1, y + 3, ARC.hi, 0.9); G(p, x + 2, y + 3, ARC.mid, 0.7); p.light({ x: x + 2, y: y + 4, col: ARC.light, r: 3, k: 0.6 }); }
  else { S(p, x + 1, y + 3, GLASS); S(p, x + 2, y + 3, GLASS_D); }
  box(p, x + 30, y + 7, 4, 11, PAL.wood);
  // frame and legs
  box(p, x + 3, y + 12, 28, 3, PAL.wood, { nohi: true }); R(p, x + 4, y + 15, 2, 3, PAL.wood[0]); R(p, x + 28, y + 15, 2, 3, PAL.wood[0]);
  // mattress, then the blanket with an ochre band, then the pillow
  for (let i = 4; i < 30; i++) { S(p, x + i, y + 11, PAL.linen[1]); S(p, x + i, y + 10, PAL.linen[2]); }
  for (let j = 6; j < 12; j++) for (let i = 11; i < 31; i++) {
    const edge = j === 6 || i === 30 || i === 11; let c = edge ? PAL.indigo[0] : j === 7 ? PAL.indigo[3] : PAL.indigo[2];
    if (j === 9 && !edge) c = (i % 4 === 0) ? PAL.ochre[4] : PAL.ochre[2];
    if (i === 29 && j > 7) c = PAL.indigo[1];
    if (j === 11 && i >= 30) continue; S(p, x + i, y + j, c);
  }
  S(p, x + 12, y + 7, PAL.indigo[4]);
  box(p, x + 4, y + 6, 8, 5, PAL.linen, { round: true }); S(p, x + 6, y + 7, PAL.linen[4]);
  dust(p, x + 12, y + 5, 18); dust(p, x + 30, y + 6, 4);
} };
PIECES.cradle = { w: 18, h: 26, anchor: 'floor', glow: 'arc', draw(p, x, y, o) {
  // rockers
  for (let i = 0; i < 18; i++) { const yy = y + 25 - Math.round(Math.pow((i - 8.5) / 8.5, 2) * 3); S(p, x + i, yy, PAL.wood[0]); S(p, x + i, yy - 1, PAL.wood[2]); }
  R(p, x + 4, y + 20, 2, 3, PAL.wood[0]); R(p, x + 12, y + 20, 2, 3, PAL.wood[0]);
  // a woven basket
  for (let j = 13; j < 21; j++) for (let i = 1; i < 17; i++) {
    const e = j === 13 || j === 20 || i === 1 || i === 16; S(p, x + i, y + j, e ? PAL.woodOld[0] : ((i + j) % 3 === 0 ? PAL.woodOld[1] : (i + (j >> 1)) % 2 ? PAL.woodOld[3] : PAL.woodOld[2]));
  }
  for (let i = 3; i < 15; i++) { S(p, x + i, y + 12, i % 2 ? PAL.indigo[3] : PAL.indigo[2]); }
  S(p, x + 4, y + 11, PAL.linen[3]); S(p, x + 5, y + 11, PAL.linen[3]);
  // the mobile: a post, an arm, three arc beads that once turned
  R(p, x + 15, y + 1, 1, 12, A[0]); S(p, x + 15, y + 4, A[4]); R(p, x + 5, y + 1, 11, 1, A[0]);
  for (const [bx, len] of [[6, 4], [9, 6], [12, 3]]) {
    for (let j = 2; j < 2 + len; j++) S(p, x + bx, y + j, A[1]);
    if (o.lit) { G(p, x + bx, y + 2 + len, ARC.core, 1); G(p, x + bx - 1, y + 2 + len, ARC.mid, 0.55); G(p, x + bx + 1, y + 2 + len, ARC.mid, 0.55); }
    else S(p, x + bx, y + 2 + len, GLASS);
  }
  if (o.lit) { p.light({ x: x + 9, y: y + 7, col: ARC.light, r: 3.4, k: 0.6 }); p.emitters.push({ kind: 'mobile', x: x + 9, y: y + 6, rx: 4, ry: 1.5 }); }
  dust(p, x + 1, y + 12, 3); dust(p, x + 14, y + 12, 3);
} };
PIECES.wardrobe = { w: 20, h: 34, anchor: 'floor', draw(p, x, y, o) {
  box(p, x, y + 3, 20, 29, PAL.wood, { grain: true }); abox(p, x - 1, y, 22, 3, { nohi: true });
  for (let j = 4; j < 31; j++) S(p, x + 10, y + j, PAL.wood[0]);
  // left door shut, right door ajar: dark inside, a hanging red cloak
  for (let j = 5; j < 30; j++) for (let i = 11; i < 19; i++) S(p, x + i, y + j, i < 13 ? hex('#0d0a08') : hex('#16100c'));
  for (let j = 7; j < 25; j++) { S(p, x + 13, y + j, PAL.red[1]); S(p, x + 14, y + j, j < 20 ? PAL.red[2] : PAL.red[1]); S(p, x + 15, y + j, PAL.red[1]); }
  R(p, x + 12, y + 6, 6, 1, A[1]);
  for (let j = 4; j < 31; j++) S(p, x + 19 + (j < 6 || j > 28 ? 0 : 1), y + j, j % 9 === 0 ? PAL.wood[1] : PAL.wood[3]);
  for (let j = 6; j < 30; j += 7) { S(p, x + 2, y + j, PAL.wood[1]); }
  R(p, x + 8, y + 15, 1, 4, A[4]); R(p, x + 1, y + 32, 3, 2, PAL.wood[0]); R(p, x + 16, y + 32, 3, 2, PAL.wood[0]);
  dust(p, x, y - 1, 20);
} };
PIECES.chest = { w: 18, h: 12, anchor: 'floor', glow: 'arc', draw(p, x, y, o) {
  box(p, x, y + 4, 18, 8, PAL.wood, { grain: true }); box(p, x, y, 18, 5, PAL.wood, { round: true });
  for (const bx of [3, 14]) for (let j = 0; j < 12; j++) S(p, x + bx, y + j, j === 0 || j === 11 ? A[0] : j < 5 ? A[4] : A[3]);
  abox(p, x + 7, y + 4, 4, 4, { nohi: true });
  if (o.lit) { G(p, x + 8, y + 5, ARC.hi, 0.9); G(p, x + 9, y + 5, ARC.mid, 0.7); p.light({ x: x + 9, y: y + 6, col: ARC.light, r: 2, k: 0.4 }); } else S(p, x + 8, y + 5, GLASS);
  dust(p, x + 1, y - 1, 16);
} };
PIECES.shelfJars = { w: 32, h: 18, anchor: 'wall', glow: 'teal', draw(p, x, y, o) {
  box(p, x, y + 13, 32, 3, PAL.wood, { nohi: true });
  for (const bx of [3, 27]) { line(p, x + bx, y + 16, x + bx + 2, y + 17, (a, b) => S(p, a, b, A[0])); S(p, x + bx, y + 17, A[0]); }
  const base = y + 12;
  // an amphora, a bowl, a glazed jug, a glass jar of something that still glows, two cups
  const jar = (cx, w, h, c, neck) => { for (let j = 0; j < h; j++) { const t = j / h, hw = neck && j < 2 ? 1 : Math.round(w / 2 * Math.sin(Math.min(1, (t * 1.1 + 0.15)) * Math.PI * 0.95)); for (let i = -hw; i <= hw; i++) S(p, x + cx + i, base - j, Math.abs(i) === hw ? c[0] : i < 0 ? (i === -hw + 1 ? c[3] : c[2]) : c[1]); } S(p, x + cx, base - h, c[0]); };
  jar(4, 7, 11, PAL.ceramic, true); S(p, x + 2, base - 7, PAL.ceramic[4]);
  jar(11, 5, 6, PAL.glaze, false); S(p, x + 10, base - 4, PAL.glaze[4]);
  // the glass jar
  for (let j = 0; j < 9; j++) for (let i = -3; i <= 3; i++) { const e = Math.abs(i) === 3 || j === 0; const X = x + 18 + i, Y = base - j;
    if (e) S(p, X, Y, hex('#3a5060')); else if (j < 7 && o.lit !== 'never') { const blob = (hash(i, j, 5) < 0.4) ? TEAL.hi : TEAL.mid; G(p, X, Y, j < 4 ? blob : TEAL.lo, j < 4 ? 0.8 : 0.5); } else S(p, X, Y, GLASS_D); }
  R(p, x + 15, base - 9, 7, 1, PAL.copper[2]); S(p, x + 15, base - 9, PAL.copper[0]); S(p, x + 21, base - 9, PAL.copper[0]);
  p.light({ x: x + 18, y: base - 4, col: TEAL.light, r: 3, k: 0.55 });
  for (const cx of [25, 29]) { box(p, x + cx - 2, base - 3, 4, 4, PAL.ceramic, { nohi: true }); }
  dust(p, x + 1, base - 0, 1); dust(p, x + 8, base - 12, 6);
} };

// ================= KITCHEN =================
PIECES.hearth = { w: 34, h: 44, anchor: 'floor', glow: 'hot', draw(p, x, y, o) {
  // a hood up to the ceiling, a hob, an open belly with the heating coil
  for (let j = 0; j < 22; j++) { const hw = 5 + Math.floor(j / 5); for (let i = -hw; i <= hw; i++) S(p, x + 17 + i, y + j, Math.abs(i) === hw ? A[0] : i === -hw + 1 ? A[4] : j % 8 === 7 ? A[1] : A[2]); }
  for (let j = 2; j < 20; j += 6) S(p, x + 17, y + j, A[5]);
  // makers' emblem on the hood
  for (const [a, b] of [[-2, 10], [-1, 9], [0, 9], [1, 9], [2, 10], [-2, 11], [2, 11], [-1, 12], [0, 12], [1, 12], [0, 10], [1, 11], [-1, 11]]) S(p, x + 17 + a, y + b + 4, a === 0 && b === 10 ? PAL.ochre[4] : PAL.ochre[2]);
  abox(p, x + 2, y + 22, 30, 3);
  box(p, x + 3, y + 25, 28, 19, PAL.stone);
  // the belly
  for (let j = 28; j < 40; j++) for (let i = 7; i < 27; i++) {
    const e = j === 28 || i === 7 || i === 26; S(p, x + i, y + j, e ? PAL.stone[0] : hex('#100a08'));
  }
  // the coil: a zigzag that glows hot; arcs crawl on it
  const coil = [];
  for (let i = 9; i < 25; i++) { const yy = y + 33 + ((i % 4 === 0) ? -2 : (i % 4 === 2) ? 2 : 0) + (i % 4 === 1 ? -1 : i % 4 === 3 ? 1 : 0); coil.push([x + i, yy]); }
  for (const [cx, cy] of coil) { if (o.lit) { G(p, cx, cy, HOT.hi, 1.05); G(p, cx, cy + 1, HOT.mid, 0.8); } else { S(p, cx, cy, PAL.copper[1]); S(p, cx, cy + 1, PAL.copper[0]); } }
  R(p, x + 8, y + 33, 1, 3, A[4]); R(p, x + 25, y + 33, 1, 3, A[4]);
  if (o.lit) {
    for (let i = 8; i < 26; i++) G(p, x + i, y + 38, HOT.lo, 0.5);
    p.light({ x: x + 17, y: y + 34, col: HOT.light, r: 8, k: 1.1, p: 1.4 });
    arcE(p, [x + 9, y + 31], [x + 24, y + 31], { rate: 0.04, life: 5, w: 0.7, light: 0.4 });
    p.emitters.push({ kind: 'pulse', x: x + 17, y: y + 34, r: 34, col: [255, 150, 70], amp: 0.12 });
    p.emitters.push({ kind: 'embers', x: x + 17, y: y + 30, w: 16, col: [255, 170, 90] });
  }
  for (let i = 4; i < 30; i += 5) S(p, x + i, y + 42, PAL.stone[1]);
} };
PIECES.kettle = { w: 12, h: 10, anchor: 'floor', glow: 'arc', draw(p, x, y, o) {
  for (let j = 2; j < 9; j++) { const hw = j < 3 ? 2 : j < 4 ? 3 : 4; for (let i = -hw; i <= hw; i++) S(p, x + 5 + i, y + j, Math.abs(i) === hw ? PAL.copper[0] : i < -hw + 2 ? PAL.copper[4] : i > hw - 2 ? PAL.copper[1] : PAL.copper[3]); }
  R(p, x + 4, y + 1, 3, 1, PAL.copper[0]); S(p, x + 5, y, PAL.copper[0]);
  for (const [a, b] of [[9, 5], [10, 4], [11, 3]]) { S(p, x + a, y + b, PAL.copper[0]); S(p, x + a, y + b + 1, PAL.copper[2]); }
  for (const [a, b] of [[2, 1], [1, 2], [1, 3], [1, 4]]) S(p, x + a, y + b, A[0]);
  // the base ring that heats it: one glyph still lit
  abox(p, x + 1, y + 8, 9, 2, { nohi: true });
  if (o.lit) { G(p, x + 4, y + 8, ARC.hi, 0.9); G(p, x + 6, y + 8, ARC.mid, 0.7); arcE(p, [x + 2, y + 9], [x + 8, y + 9], { rate: 0.012, life: 4, w: 0.4 }); }
} };
PIECES.dishes = { w: 28, h: 9, anchor: 'floor', draw(p, x, y, o) {
  // a meal laid for four that nobody finished: plates, bowls with something dark in them, a jug, a cup on its side, a spoon
  const b = y + 8;
  for (const px0 of [0, 20]) { R(p, x + px0, b, 6, 1, PAL.linen[0]); R(p, x + px0 + 1, b - 1, 4, 1, PAL.linen[3]); S(p, x + px0 + 1, b - 1, PAL.linen[4]); }
  for (const bx of [7, 15]) { R(p, x + bx, b - 2, 5, 3, PAL.glaze[0]); R(p, x + bx + 1, b - 2, 3, 2, PAL.glaze[2]); S(p, x + bx + 1, b - 2, hex('#2a1c14')); S(p, x + bx + 2, b - 2, hex('#3a2818')); S(p, x + bx + 3, b - 2, hex('#2a1c14')); S(p, x + bx + 1, b - 1, PAL.glaze[4]); }
  // the jug
  for (let j = 0; j < 8; j++) { const hw = j > 6 ? 1 : j > 5 ? 2 : 2; for (let i = -hw; i <= hw; i++) S(p, x + 13 + i, b - j, Math.abs(i) === hw ? PAL.ceramic[0] : i < 0 ? PAL.ceramic[3] : PAL.ceramic[2]); }
  S(p, x + 16, b - 5, PAL.ceramic[0]); S(p, x + 16, b - 4, PAL.ceramic[0]); S(p, x + 15, b - 6, PAL.ceramic[0]); S(p, x + 11, b - 7, PAL.ceramic[0]);
  // a cup on its side, a spoon
  R(p, x + 23, b - 2, 4, 3, PAL.ceramic[0]); R(p, x + 24, b - 1, 2, 1, PAL.ceramic[3]); S(p, x + 27, b - 1, PAL.ceramic[0]);
  R(p, x + 3, b - 2, 4, 1, A[4]); S(p, x + 2, b - 2, A[5]);
  dust(p, x + 7, b - 3, 5); dust(p, x + 20, b - 2, 6);
} };
PIECES.pots = { w: 20, h: 16, anchor: 'floor', draw(p, x, y, o) {
  const b = y + 15;
  const pot = (cx, w, h, c, lip) => { for (let j = 0; j < h; j++) { const t = j / (h - 1); const hw = Math.max(1, Math.round(w / 2 * (0.55 + 0.45 * Math.sin(Math.min(1, t * 1.15) * Math.PI)))); for (let i = -hw; i <= hw; i++) S(p, x + cx + i, b - j, Math.abs(i) === hw || j === 0 ? c[0] : i === -hw + 1 ? c[3] : i > hw - 2 ? c[1] : c[2]); } if (lip) R(p, x + cx - 2, b - h, 5, 1, c[0]); };
  pot(5, 9, 15, PAL.ceramic, true); S(p, x + 2, b - 9, PAL.ceramic[4]);
  for (let i = 2; i < 9; i += 2) S(p, x + i, b - 6, PAL.ochre[2]);
  pot(13, 6, 9, PAL.glaze, true); S(p, x + 11, b - 5, PAL.glaze[4]);
  pot(18, 3, 5, PAL.ceramic, false);
  // shards of a broken one
  for (const [a, c] of [[9, PAL.ceramic[2]], [10, PAL.ceramic[0]], [16, PAL.ceramic[3]]]) S(p, x + a, b, c);
  dust(p, x + 3, b - 15, 5);
} };
PIECES.basin = { w: 30, h: 22, anchor: 'floor', draw(p, x, y, o) {
  // a stone tub, a tap of alloy on the wall, a white crust where the water line was
  box(p, x, y + 10, 30, 12, PAL.stone, { round: true });
  for (let i = 2; i < 28; i++) { S(p, x + i, y + 11, hex('#0e0e12')); S(p, x + i, y + 12, i % 3 ? hex('#b8b4a4') : hex('#8e8a7c')); }
  for (let i = 3; i < 27; i += 4) S(p, x + i, y + 17, PAL.stone[1]);
  R(p, x + 22, y, 3, 8, A[0]); S(p, x + 23, y + 1, A[4]); R(p, x + 19, y + 2, 4, 2, A[0]); R(p, x + 19, y + 2, 1, 4, A[0]); S(p, x + 19, y + 5, ARC.lo);
  abox(p, x + 21, y - 1, 5, 2, { nohi: true });
  // a ladle hanging on the rim
  R(p, x + 4, y + 7, 1, 4, PAL.wood[3]); R(p, x + 3, y + 11, 3, 2, PAL.wood[1]);
  dust(p, x + 1, y + 9, 28);
} };

// ================= CLOTH AND MEMORY =================
function emblem(p, cx, cy, c1, c2) {
  // the makers' sign: a ring broken by a bolt
  for (let a = 0; a < 360; a += 15) { const r = a * Math.PI / 180; if (a > 30 && a < 80) continue; S(p, Math.round(cx + 3.6 * Math.cos(r)), Math.round(cy + 3.6 * Math.sin(r)), c1); }
  for (const [a, b] of [[1, -5], [1, -4], [0, -3], [0, -2], [-1, -1], [0, -1], [1, -1], [0, 0], [-1, 1], [-1, 2], [-2, 3]]) S(p, cx + a, cy + b, c2);
}
PIECES.banner = { w: 14, h: 34, anchor: 'wall', draw(p, x, y, o) {
  abox(p, x, y, 14, 2, { nohi: true }); S(p, x - 1, y, A[0]); S(p, x + 14, y, A[0]); S(p, x - 1, y + 1, A[5]); S(p, x + 14, y + 1, A[5]);
  const C = o.col || PAL.red;
  for (let j = 2; j < 34; j++) for (let i = 1; i < 13; i++) {
    const tail = j > 27 && Math.abs(i - 6.5) < (j - 27) * 0.9; if (tail) continue;
    const torn = j > 20 && i > 9 && hash(i, j, 3) < (j - 20) / 20; if (torn) continue;
    const edge = i === 1 || i === 12 || (j >= 27 && Math.abs(Math.abs(i - 6.5) - (j - 27) * 0.9) < 1);
    let c = edge ? C[0] : i === 2 || i === 11 ? PAL.ochre[2] : (i + j) % 7 === 0 ? C[1] : i < 5 ? C[3] : C[2];
    if (j === 4 || j === 25) c = edge ? C[0] : PAL.ochre[2];
    S(p, x + i, y + j, c);
  }
  emblem(p, x + 6, y + 14, PAL.ochre[3], PAL.ochre[4]);
  p.emitters.push({ kind: 'sway', x: x + 6.5, y: y + 2 });
} };
PIECES.tapestry = { w: 48, h: 30, anchor: 'wall', draw(p, x, y, o) {
  abox(p, x - 1, y, 50, 2, { nohi: true });
  for (let j = 2; j < 28; j++) for (let i = 0; i < 48; i++) {
    if (i > 38 && j > 18 && (i - 38) + (j - 18) > 12) continue; // a torn corner
    const edge = i === 0 || i === 47 || j === 2 || j === 27;
    let c = edge ? PAL.indigo[0] : (i === 2 || i === 45 || j === 4 || j === 25) ? PAL.ochre[2] : hash(i, j, 41) < 0.1 ? PAL.indigo[1] : PAL.indigo[2];
    S(p, x + i, y + j, c);
  }
  for (let i = 1; i < 39; i += 2) S(p, x + i, y + 28, PAL.ochre[1]);
  // a woven scene: a spire with its lamp, an arc from it, three figures holding hands under it
  const ox = x + 24, oy = y + 24;
  for (let j = 0; j < 16; j++) { const hw = j < 3 ? 0 : j < 8 ? 1 : 2; for (let i = -hw; i <= hw; i++) S(p, ox + i, oy - j, PAL.linen[i < 0 ? 3 : 2]); }
  S(p, ox, oy - 17, hex('#9fe8ff')); S(p, ox, oy - 18, hex('#d8f6ff'));
  for (const [a, b] of [[1, -18], [2, -19], [3, -18], [4, -19], [5, -20], [-1, -18], [-2, -17], [-3, -18], [-4, -19]]) S(p, ox + a, oy + b, hex('#7cc8ff'));
  for (const fx of [-14, -9, 9, 14]) { R(p, ox + fx, oy - 6, 1, 4, PAL.red[3]); S(p, ox + fx, oy - 7, PAL.ochre[4]); R(p, ox + fx - 1, oy - 2, 3, 1, PAL.red[2]); }
  for (let i = -14; i <= -9; i++) S(p, ox + i, oy - 5, PAL.red[3]); for (let i = 9; i <= 14; i++) S(p, ox + i, oy - 5, PAL.red[3]);
  for (let i = 3; i < 45; i++) S(p, x + i, oy + 0, PAL.ochre[1]);
  p.emitters.push({ kind: 'sway', x: x + 24, y: y + 2 });
} };
PIECES.rug = { w: 46, h: 3, anchor: 'floor', draw(p, x, y, o) {
  for (let i = 0; i < 46; i++) {
    const fringe = i < 2 || i > 43;
    if (fringe) { if (i % 2 === 0) S(p, x + i, y + 2, PAL.ochre[3]); continue; }
    S(p, x + i, y + 1, PAL.red[0]);
    S(p, x + i, y + 2, (i % 6 < 2) ? PAL.ochre[3] : (i % 6 === 3) ? PAL.indigo[3] : PAL.red[3]);
  }
  dust(p, x + 2, y + 1, 42);
} };
PIECES.mural = { w: 72, h: 34, anchor: 'wall', glow: 'violet', draw(p, x, y, o) {
  // painted on the plaster, faded and flaking: a tree of lightning over the makers, and at the right a stair going down,
  // figures carrying scroll cases into the dark, one faint violet glyph at its foot
  const paint = (X, Y, c, a = 0.55) => { if (hash(X, Y, 77) < 0.12) return; blend(p, X, Y, c, Math.min(0.9, a * 1.35)); };
  for (let i = 0; i < 72; i++) { paint(x + i, y + 25, PAL.ochre[2], 0.4); if (i % 3) paint(x + i, y + 26, PAL.ochre[1], 0.35); }
  // the tree: a trunk and branching bolts
  const T = [x + 20, y + 25];
  line(p, T[0], T[1], T[0], T[1] - 12, (a, b) => { paint(a, b, PAL.paper[3], 0.6); paint(a + 1, b, PAL.paper[2], 0.5); });
  const br = (sx, sy, dx, n, d) => { let X = sx, Y = sy; for (let k = 0; k < n; k++) { X += dx + (hash(k, sx, d) < 0.3 ? -dx : 0); Y -= 1; paint(X, Y, hex('#8ad8ff'), 0.6); if (d < 2 && k === (n >> 1)) br(X, Y, -dx, n - 3, d + 1); } };
  br(T[0], T[1] - 12, 1, 9, 0); br(T[0], T[1] - 12, -1, 9, 0); br(T[0], T[1] - 14, 0, 6, 1);
  // the makers under it, a ring of figures
  for (const fx of [6, 10, 14, 26, 30, 34]) { for (let j = 0; j < 5; j++) paint(x + fx, y + 24 - j, PAL.red[3], 0.6); paint(x + fx, y + 18, PAL.ochre[4], 0.6); paint(x + fx - 1, y + 21, PAL.red[2], 0.5); paint(x + fx + 1, y + 21, PAL.red[2], 0.5); }
  // the stair down and its carriers
  for (let k = 0; k < 9; k++) for (let i = 0; i < 4; i++) paint(x + 44 + k * 3 + i, y + 8 + k * 3, PAL.paper[2], 0.5);
  for (const k of [1, 3, 5, 7]) { const fx = x + 46 + k * 3, fy = y + 7 + k * 3; for (let j = 0; j < 4; j++) paint(fx, fy - j, PAL.indigo[3], 0.65); paint(fx, fy - 5, PAL.ochre[4], 0.6); paint(fx + 1, fy - 2, PAL.copper[3], 0.7); paint(fx + 2, fy - 2, PAL.copper[3], 0.7); }
  for (let j = 0; j < 8; j++) for (let i = 0; i < 10; i++) paint(x + 62 + i, y + 28 + (j >> 1) - 2, hex('#0a0810'), 0.45);
  G(p, x + 66, y + 30, VIO.hi, 0.85); G(p, x + 65, y + 30, VIO.mid, 0.5); G(p, x + 67, y + 30, VIO.mid, 0.5); G(p, x + 66, y + 29, VIO.mid, 0.5); G(p, x + 66, y + 31, VIO.lo, 0.4);
  p.light({ x: x + 66, y: y + 30, col: VIO.light, r: 2.2, k: 0.4 });
  p.emitters.push({ kind: 'pulse', x: x + 66, y: y + 30, r: 10, col: [190, 150, 255], amp: 0.35, slow: 1 });
} };
PIECES.holoPortrait = { w: 18, h: 20, anchor: 'wall', glow: 'holo', draw(p, x, y, o) {
  // a frame whose bottom bar projects the family: two grown-ups and a child, cut by scanlines
  abox(p, x, y + 16, 18, 4); R(p, x + 3, y + 16, 12, 1, A[1]);
  for (const ex of [5, 9, 13]) G(p, x + ex, y + 17, o.lit ? HOLO.hi : HOLO.lo, o.lit ? 0.9 : 0.35);
  if (o.lit) {
    const bust = (cx, top, r, sh) => { for (let j = 0; j < 15 - top; j++) { const yy = top + j; let hw = j < r * 2 ? Math.round(Math.sqrt(Math.max(0, r * r - (j - r) * (j - r)))) : sh; if (j >= r * 2 && j < r * 2 + 1) hw = 1; for (let i = -hw; i <= hw; i++) { const scan = yy % 2 === 0; G(p, x + cx + i, y + yy, Math.abs(i) === hw ? HOLO.mid : HOLO.lo, scan ? 0.55 : 0.25); } } };
    bust(5, 2, 2, 3); bust(13, 3, 2, 3); bust(9, 7, 1.6, 2);
    G(p, x + 4, y + 4, HOLO.hi, 0.9); G(p, x + 12, y + 5, HOLO.hi, 0.9);
    p.light({ x: x + 9, y: y + 9, col: HOLO.light, r: 3.6, k: 0.5 });
    p.emitters.push({ kind: 'holo', x: x + 1, y: y + 1, w: 16, h: 15 });
  }
} };
PIECES.toys = { w: 24, h: 9, anchor: 'floor', glow: 'arc', draw(p, x, y, o) {
  const b = y + 8;
  // a wooden beast on wheels, a cloth ball, a spinning top with a spark inside, fallen over
  box(p, x + 1, b - 6, 9, 4, PAL.woodOld); R(p, x + 8, b - 8, 3, 3, PAL.woodOld[0]); S(p, x + 9, b - 7, PAL.woodOld[3]); S(p, x + 10, b - 7, hex('#0a0a0a'));
  S(p, x + 1, b - 7, PAL.woodOld[0]);
  for (const wx of [2, 7]) { S(p, x + wx, b - 1, A[0]); S(p, x + wx + 1, b - 1, A[0]); S(p, x + wx, b, A[0]); S(p, x + wx + 1, b, A[3]); }
  R(p, x - 2, b - 4, 3, 1, PAL.linen[2]);
  box(p, x + 12, b - 3, 4, 4, PAL.red, { round: true }); S(p, x + 13, b - 2, PAL.ochre[3]);
  for (const [a, c] of [[0, A[0]], [1, A[3]], [2, A[4]], [3, A[3]], [4, A[0]]]) { S(p, x + 18 + a, b - 1, c); S(p, x + 18 + a, b, A[0]); }
  S(p, x + 20, b - 2, A[0]); S(p, x + 23, b, A[1]);
  if (o.lit) { G(p, x + 20, b - 1, ARC.core, 1); p.emitters.push({ kind: 'arc', a: [x + 19, b - 1.5], b: [x + 21.5, b - 2], rate: 0.01, life: 3, w: 0.35, pal: 'cyan' }); }
} };
PIECES.clock = { w: 16, h: 26, anchor: 'wall', draw(p, x, y, o) {
  box(p, x + 1, y + 13, 14, 13, PAL.wood, { grain: true });
  for (let yy = -7; yy <= 7; yy++) for (let xx = -7; xx <= 7; xx++) { const d = Math.sqrt(xx * xx + yy * yy); if (d > 7.3) continue; S(p, x + 8 + xx, y + 7 + yy, d > 6.3 ? PAL.copper[0] : d > 5.4 ? PAL.copper[3] : PAL.paper[3]); }
  for (let k = 0; k < 12; k++) { const a = k * Math.PI / 6; S(p, Math.round(x + 8 + 4.4 * Math.cos(a)), Math.round(y + 7 + 4.4 * Math.sin(a)), k % 3 ? PAL.paper[1] : PAL.paper[0]); }
  // hands stopped at the same moment as everything else
  line(p, x + 8, y + 7, x + 6, y + 4, (a, b) => S(p, a, b, PAL.wood[0])); line(p, x + 8, y + 7, x + 11, y + 8, (a, b) => S(p, a, b, PAL.wood[0])); S(p, x + 8, y + 7, PAL.copper[4]);
  line(p, x + 4, y + 3, x + 10, y + 11, (a, b) => { if (hash(a, b, 4) < 0.6) S(p, a, b, PAL.paper[4]); });
  // the pendulum hangs askew in its glass
  for (let j = 15; j < 24; j++) for (let i = 3; i < 13; i++) S(p, x + i, y + j, hex('#14100e'));
  line(p, x + 8, y + 15, x + 6, y + 21, (a, b) => S(p, a, b, PAL.copper[2])); box(p, x + 4, y + 21, 4, 3, PAL.copper, { round: true });
  dust(p, x + 2, y + 12, 3); dust(p, x + 11, y + 12, 3);
} };
PIECES.curtain = { w: 12, h: 46, anchor: 'ceiling', draw(p, x, y, o) {
  abox(p, x - 1, y, 14, 2, { nohi: true });
  for (let j = 2; j < 46; j++) { const pull = j > 30 ? Math.round((j - 30) * 0.35) : 0;
    for (let i = 0; i < 10 - pull; i++) { const fold = (i + (j >> 3)) % 4; S(p, x + i, y + j, i === 9 - pull ? PAL.ochre[0] : fold === 0 ? PAL.ochre[0] : fold === 1 ? PAL.ochre[2] : PAL.ochre[1]); } }
  for (let i = 0; i < 9; i++) if (hash(i, 3, 3) < 0.6) S(p, x + i, y + 46, PAL.ochre[1]);
  R(p, x - 1, y + 30, 6, 2, PAL.red[2]);
  p.emitters.push({ kind: 'sway', x: x + 5, y: y + 2 });
} };

// ================= KNOWLEDGE =================
PIECES.desk = { w: 34, h: 26, anchor: 'floor', glow: 'arc', draw(p, x, y, o) {
  const top = y + 12;
  box(p, x, top, 34, 3, PAL.wood, { grain: true });
  box(p, x + 20, top + 3, 13, 11, PAL.wood); for (const dy of [3, 7]) { R(p, x + 21, top + 3 + dy, 11, 1, PAL.wood[0]); S(p, x + 26, top + 1 + dy, A[4]); }
  R(p, x + 2, top + 3, 3, 11, PAL.wood[0]); R(p, x + 3, top + 3, 1, 10, PAL.wood[2]);
  // the slanted screen: dead glass, a ghost glyph burnt in, one pixel that never went out
  for (let j = 0; j < 11; j++) for (let i = 0; i < 16; i++) { const X = x + 4 + i + (j >> 2), Y = y + j; const e = i === 0 || i === 15 || j === 0 || j === 10;
    S(p, X, Y, e ? A[0] : (j === 1 || i === 1) ? A[2] : (i + j) % 11 === 0 ? hex('#1b2a33') : hex('#0f171d')); }
  for (const [a, b] of [[4, 3], [5, 3], [6, 3], [9, 3], [4, 5], [7, 5], [8, 5], [10, 5], [4, 7], [5, 7]]) S(p, x + 4 + a + (b >> 2), y + b, hex('#1f3640'));
  R(p, x + 11, y + 11, 3, 1, A[0]);
  if (o.lit) { G(p, x + 15, y + 6, ARC.hi, 0.9); p.emitters.push({ kind: 'pixel', x: x + 15, y: y + 6 }); }
  // an open scroll case and a sheet on the desk
  R(p, x + 23, top - 2, 7, 2, PAL.copper[2]); S(p, x + 23, top - 2, PAL.copper[0]); S(p, x + 29, top - 2, PAL.copper[4]); R(p, x + 30, top - 1, 3, 1, PAL.paper[3]);
  dust(p, x + 1, top - 1, 20);
} };
PIECES.bookshelf = { w: 32, h: 40, anchor: 'floor', glow: 'violet', draw(p, x, y, o) {
  box(p, x, y, 32, 40, PAL.wood);
  for (let j = 2; j < 38; j++) for (let i = 2; i < 30; i++) S(p, x + i, y + j, hex('#140e0a'));
  for (const sy of [12, 24, 36]) box(p, x + 1, y + sy, 30, 3, PAL.wood, { nohi: true });
  // scroll cases lying in rows; most slots stand empty, one case still glows faint violet
  const caseAt = (cx, cy, col, glowing) => { for (let i = 0; i < 9; i++) { S(p, x + cx + i, y + cy, col[0]); S(p, x + cx + i, y + cy + 1, i === 0 || i === 8 ? col[4] : col[3]); S(p, x + cx + i, y + cy + 2, col[2]); S(p, x + cx + i, y + cy + 3, col[0]); }
    if (glowing) { G(p, x + cx + 4, y + cy + 1, VIO.hi, 0.9); G(p, x + cx + 3, y + cy + 2, VIO.mid, 0.6); G(p, x + cx + 5, y + cy + 2, VIO.mid, 0.6); } };
  caseAt(3, 8, PAL.copper); caseAt(3, 4, PAL.copper); caseAt(19, 8, A.slice(1));
  caseAt(13, 20, PAL.copper, true); caseAt(3, 32, PAL.copper);
  // loose scrolls and a fallen one
  for (const [cx, cy] of [[22, 30], [25, 32]]) { R(p, x + cx, y + cy, 5, 4, PAL.paper[2]); R(p, x + cx, y + cy, 5, 1, PAL.paper[4]); S(p, x + cx + 4, y + cy + 1, PAL.paper[0]); }
  for (let i = 0; i < 4; i++) S(p, x + 4 + i, y + 22 - i, PAL.paper[3]);
  p.light({ x: x + 17, y: y + 22, col: VIO.light, r: 2.4, k: 0.45 });
  p.emitters.push({ kind: 'pulse', x: x + 17, y: y + 22, r: 12, col: [190, 150, 255], amp: 0.35, slow: 1 });
  dust(p, x, y - 1, 32);
} };
PIECES.arcDevice = { w: 22, h: 44, anchor: 'floor', glow: 'arc', draw(p, x, y, o) {
  // a resonator: a coil on insulator rings under a polished sphere, a grounding rod beside it; it still talks to itself
  const cx = x + 11;
  abox(p, x + 1, y + 38, 20, 6); for (let i = 3; i < 19; i += 4) S(p, x + i, y + 40, A[1]);
  for (let j = 14; j < 38; j++) { const ring = j % 4 === 0; for (let i = -3; i <= 3; i++) S(p, cx + i, y + j, Math.abs(i) === 3 ? (ring ? PAL.glaze[0] : PAL.copper[0]) : ring ? PAL.glaze[i < 0 ? 3 : 2] : (j % 2 ? PAL.copper[i < 0 ? 4 : 3] : PAL.copper[1])); }
  for (let j = 10; j < 14; j++) { S(p, cx - 1, y + j, A[0]); S(p, cx, y + j, A[4]); S(p, cx + 1, y + j, A[0]); }
  for (let yy = -5; yy <= 5; yy++) for (let xx = -5; xx <= 5; xx++) { const d = Math.sqrt(xx * xx + yy * yy); if (d > 5.3) continue; const X = cx + xx, Y = y + 5 + yy;
    if (d > 4.4) S(p, X, Y, A[0]); else if (xx < -1 && yy < -1) S(p, X, Y, A[5]); else if (xx + yy < 2) S(p, X, Y, A[4]); else S(p, X, Y, A[2]); }
  S(p, cx - 2, y + 2, hex('#ffffff'));
  // the rod
  R(p, x + 1, y + 20, 2, 18, A[0]); for (let j = 21; j < 38; j++) S(p, x + 2, y + j, A[3]); box(p, x, y + 17, 4, 4, A.slice(1), { round: true });
  if (o.lit) {
    G(p, cx - 1, y + 4, ARC.hi, 0.5); G(p, cx + 2, y + 6, ARC.mid, 0.4);
    p.light({ x: cx, y: y + 6, col: ARC.light, r: 9, k: 1.1, p: 1.4 });
    p.emitters.push({ kind: 'arc', a: [cx - 4, y + 7], b: [x + 2, y + 18], rate: 0.06, life: 6, w: 1, pal: 'cyan', light: 1 });
    p.emitters.push({ kind: 'arc', a: [cx + 3, y + 2], b: 'air', rate: 0.05, life: 5, w: 0.8, pal: 'cyan', light: 0.6, reach: 12 });
    p.emitters.push({ kind: 'arc', a: [cx - 3, y + 16], b: [cx + 3, y + 30], rate: 0.03, life: 4, w: 0.5, pal: 'cyan' });
    p.emitters.push({ kind: 'pulse', x: cx, y: y + 6, r: 44, col: [120, 200, 255], amp: 0.1 });
  }
} };

// ================= PLANTS =================
PIECES.plantAlive = { w: 16, h: 26, anchor: 'floor', glow: 'teal', draw(p, x, y, o) {
  // a glazed pot; the plant outlived its keepers and glows where its bulbs are
  const b = y + 25;
  for (let j = 0; j < 8; j++) { const hw = j > 5 ? 5 : 4; for (let i = -hw; i <= hw; i++) S(p, x + 8 + i, b - j, Math.abs(i) === hw || j === 0 ? PAL.glaze[0] : i < -2 ? PAL.glaze[3] : i > 2 ? PAL.glaze[1] : PAL.glaze[2]); }
  R(p, x + 2, b - 8, 13, 1, PAL.glaze[0]); R(p, x + 4, b - 8, 9, 1, hex('#2a1c14'));
  const stem = (sx, len, bend) => { let X = sx; for (let k = 0; k < len; k++) { if (k > 3 && k % 4 === 0) X += bend; S(p, x + X, b - 9 - k, PAL.plant[k % 5 === 0 ? 3 : 2]); if (k % 5 === 2) { S(p, x + X - bend, b - 9 - k, PAL.plant[3]); S(p, x + X - 2 * bend, b - 10 - k, PAL.plant[4]); } } return X; };
  const tips = [[stem(8, 15, 0), 15], [stem(6, 11, -1), 11], [stem(10, 9, 1), 9]];
  for (const [tx, len] of tips) { const ty = b - 9 - len; G(p, x + tx, ty, TEAL.hi, 1); G(p, x + tx - 1, ty, TEAL.mid, 0.8); G(p, x + tx + 1, ty, TEAL.mid, 0.8); G(p, x + tx, ty - 1, TEAL.mid, 0.8); G(p, x + tx, ty + 1, TEAL.lo, 0.6); }
  p.light({ x: x + 8, y: b - 20, col: TEAL.light, r: 3.6, k: 0.7 });
  p.emitters.push({ kind: 'spore', x: x + 8, y: b - 20, col: [80, 230, 200], n: 4 });
} };
PIECES.plantDead = { w: 14, h: 18, anchor: 'floor', draw(p, x, y, o) {
  const b = y + 17;
  for (let j = 0; j < 7; j++) { const hw = j > 4 ? 4 : 3; for (let i = -hw; i <= hw; i++) S(p, x + 7 + i, b - j, Math.abs(i) === hw || j === 0 ? PAL.ceramic[0] : i < -1 ? PAL.ceramic[3] : PAL.ceramic[2]); }
  R(p, x + 2, b - 7, 11, 1, PAL.ceramic[0]);
  const droop = (sx, len, dir) => { let X = sx, Y = b - 8; for (let k = 0; k < len; k++) { if (k < len * 0.6) Y--; else { X += dir; if (k % 2) Y++; } S(p, x + X, Y, PAL.dry[k % 3 === 0 ? 1 : 3]); } S(p, x + X + dir, Y + 1, PAL.dry[4]); };
  droop(7, 12, 1); droop(6, 9, -1); droop(8, 7, 1);
  for (const [a, c] of [[0, PAL.dry[3]], [12, PAL.dry[2]], [13, PAL.dry[4]]]) S(p, x + a, b, c);
} };

// ================= AGE =================
PIECES.cobweb = { w: 14, h: 14, anchor: 'ceiling', draw(p, x, y, o) {
  // a corner web: spokes from the corner, sagging rings between them
  const spokes = [0.05, 0.4, 0.8, 1.2, 1.5].map((a) => [Math.cos(a), Math.sin(a)]);
  for (const [dx, dy] of spokes) for (let r = 0; r < 14; r++) blend(p, Math.round(x + dx * r), Math.round(y + dy * r), WEB, 0.42);
  for (const r of [4, 8, 12]) for (let k = 0; k < 40; k++) { const a = 0.05 + 1.45 * k / 40, rr = r - 0.9 * Math.sin((a * 3.3) % 1 * Math.PI); blend(p, Math.round(x + Math.cos(a) * rr), Math.round(y + Math.sin(a) * rr), WEB, 0.3); }
  p.emitters.push({ kind: 'motes', x0: x, y0: y, x1: x + 14, y1: y + 14, col: [200, 196, 188], n: 2 });
} };
PIECES.crack = { w: 16, h: 24, anchor: 'wall', draw(p, x, y, o) {
  let X = x + 8; for (let j = 0; j < 24; j++) { if (hash(j, 3, 9) < 0.45) X += hash(j, 4, 9) < 0.5 ? -1 : 1; S(p, X, y + j, hex('#0c0a0a')); blend(p, X + 1, y + j, hex('#9a8a7a'), 0.25);
    if (j === 9 || j === 15) { let Z = X; for (let k = 0; k < 5; k++) { Z += j === 9 ? 1 : -1; S(p, Z, y + j + (k >> 1), hex('#140f0e')); } } }
} };
PIECES.fallenBeam = { w: 50, h: 22, anchor: 'floor', draw(p, x, y, o) {
  // an alloy beam fallen from the ceiling, one end on the floor, the other caught on rubble
  for (let k = 0; k < 50; k++) { const cy = y + 3 + Math.round(k * 0.34);
    for (let d = 0; d < 5; d++) { const c = d === 0 || d === 4 ? A[0] : d === 1 ? A[4] : d === 3 ? A[1] : (k % 8 === 2 ? A[5] : A[3]); S(p, x + k, cy + d, c); } }
  for (let d = 0; d < 6; d++) S(p, x + 49, y + 20 - d, A[0]);
  for (let d = 0; d < 5; d++) S(p, x, y + 3 + d, A[0]);
  // torn end
  for (const [a, b] of [[1, 1], [2, 2], [0, 2], [2, 0]]) S(p, x + a, y + b, A[2]);
  dust(p, x + 2, y + 2, 46);
} };
PIECES.rubble = { w: 22, h: 8, anchor: 'floor', draw(p, x, y, o) {
  const chunk = (cx, cy, w, h, c) => box(p, x + cx, y + cy, w, h, c, { round: true });
  chunk(0, 4, 6, 4, PAL.stone); chunk(5, 2, 7, 6, PAL.stone); chunk(11, 4, 5, 4, A.slice(1)); chunk(15, 5, 4, 3, PAL.stone); chunk(18, 6, 4, 2, PAL.stone);
  S(p, x + 13, y + 3, A[4]); S(p, x + 14, y + 3, A[0]);
  for (let i = 0; i < 22; i++) if (hash(i, 8, 1) < 0.5) S(p, x + i, y + 7, PAL.stone[hash(i, 9, 1) < 0.5 ? 1 : 2]);
} };

PIECES.musicBox = { w: 12, h: 10, anchor: 'floor', glow: 'arc' };
PIECES.musicBox.draw = function (p, x, y, o) {
  box(p, x, y + 5, 12, 5, PAL.wood, { grain: true });
  for (let j = 0; j < 6; j++) { S(p, x + j, y + 5 - j, PAL.wood[0]); S(p, x + j + 1, y + 5 - j, PAL.wood[3]); }
  R(p, x + 3, y + 6, 6, 1, PAL.copper[3]); for (let i = 3; i < 9; i += 2) S(p, x + i, y + 7, PAL.copper[1]);
  S(p, x + 10, y + 6, PAL.copper[4]); S(p, x + 10, y + 7, PAL.copper[2]); S(p, x + 6, y + 8, PAL.ochre[4]);
  if (o.lit) { G(p, x + 10, y + 6, ARC.hi, 0.9); p.emitters.push({ kind: 'arc', a: [x + 10, y + 5.5], b: [x + 11.5, y + 3.5], rate: 0.03, life: 3, w: 0.35, pal: 'cyan', beat: 1 }); p.light({ x: x + 10, y: y + 6, col: ARC.light, r: 2.2, k: 0.4 }); }
};

// ================= SIZED TO THE HERO (44 px в‰€ 1.8 m, so about 24 px a metre) =================
PIECES.table = { w: 32, h: 18, anchor: 'floor', draw(p, x, y, o) {
  box(p, x, y, 32, 4, PAL.wood, { grain: true }); R(p, x + 1, y + 4, 30, 2, PAL.wood[0]); R(p, x + 2, y + 4, 28, 1, PAL.wood[1]);
  for (const lx of [2, 26]) { R(p, x + lx, y + 6, 4, 11, A[0]); for (let j = 6; j < 17; j++) { S(p, x + lx + 1, y + j, j === 7 ? A[5] : A[4]); S(p, x + lx + 2, y + j, A[2]); } R(p, x + lx - 1, y + 17, 6, 1, A[0]); }
  R(p, x + 6, y + 12, 20, 2, A[0]); R(p, x + 6, y + 12, 20, 1, A[2]); S(p, x + 16, y + 12, ARC.lo);
  if (!o.noDust) dust(p, x + 1, y - 1, 30);
} };
PIECES.stool = { w: 10, h: 11, anchor: 'floor', draw(p, x, y, o) {
  box(p, x, y, 10, 3, PAL.wood);
  for (let j = 3; j < 11; j++) { const s = j > 8 ? 1 : 0; S(p, x + 1 - s, y + j, PAL.wood[0]); S(p, x + 2 - s, y + j, PAL.wood[3]); S(p, x + 7 + s, y + j, PAL.wood[2]); S(p, x + 8 + s, y + j, PAL.wood[0]); }
  R(p, x + 3, y + 6, 4, 1, PAL.wood[1]); if (!o.noDust) dust(p, x + 1, y - 1, 8);
} };
PIECES.chair = { w: 12, h: 22, anchor: 'floor', draw(p, x, y, o) {
  const f = o.flip, X = (i) => f ? x + 11 - i : x + i;
  for (let j = 0; j < 22; j++) for (let i = 0; i < 3; i++) S(p, X(i), y + j, i === 0 || j === 0 ? PAL.wood[0] : i === 1 ? PAL.wood[3] : PAL.wood[1]);
  for (const j of [3, 6]) for (let i = 3; i < 5; i++) { S(p, X(i), y + j, PAL.wood[0]); S(p, X(i), y + j + 1, PAL.wood[2]); }
  S(p, X(1), y + 1, PAL.wood[4]);
  for (let i = 0; i < 12; i++) { S(p, X(i), y + 11, PAL.wood[0]); S(p, X(i), y + 12, i === 11 ? PAL.wood[0] : PAL.wood[3]); S(p, X(i), y + 13, PAL.wood[0]); }
  for (let i = 3; i < 11; i++) { S(p, X(i), y + 9, i === 3 || i === 10 ? PAL.red[0] : PAL.red[i < 6 ? 4 : 3]); S(p, X(i), y + 10, i === 10 ? PAL.red[0] : PAL.red[1]); }
  for (let j = 14; j < 22; j++) { S(p, X(1), y + j, PAL.wood[0]); S(p, X(2), y + j, PAL.wood[2]); S(p, X(9), y + j, PAL.wood[2]); S(p, X(10), y + j, PAL.wood[0]); }
  for (let i = 3; i < 9; i++) S(p, X(i), y + 18, PAL.wood[1]);
  if (!o.noDust) { dust(p, Math.min(X(4), X(9)), y + 8, 6); dust(p, Math.min(X(0), X(2)), y - 1, 3); }
} };
PIECES.bench = { w: 30, h: 11, anchor: 'floor', draw(p, x, y, o) {
  box(p, x, y, 30, 3, PAL.woodOld, { grain: true });
  for (const lx of [2, 25]) { R(p, x + lx, y + 3, 3, 8, PAL.woodOld[0]); R(p, x + lx + 1, y + 3, 1, 7, PAL.woodOld[2]); }
  R(p, x + 5, y + 6, 20, 1, PAL.woodOld[1]); dust(p, x + 1, y - 1, 15);
  box(p, x + 17, y - 3, 10, 3, PAL.red, { nohi: true }); S(p, x + 18, y - 2, PAL.red[4]); R(p, x + 26, y, 1, 4, PAL.red[1]);
} };
PIECES.bed = { w: 46, h: 22, anchor: 'floor', glow: 'arc', draw(p, x, y, o) {
  box(p, x, y + 1, 5, 21, PAL.wood, { grain: true }); abox(p, x - 1, y, 7, 2, { nohi: true });
  for (let j = 5; j < 9; j++) S(p, x + 2, y + j, PAL.wood[0]);
  if (o.lit) { G(p, x + 2, y + 6, ARC.core, 1); G(p, x + 2, y + 5, ARC.mid, 0.7); G(p, x + 2, y + 7, ARC.mid, 0.7); p.light({ x: x + 4, y: y + 6, col: ARC.light, r: 3.2, k: 0.6 }); }
  else { S(p, x + 2, y + 6, GLASS); }
  box(p, x + 41, y + 8, 5, 14, PAL.wood); abox(p, x + 40, y + 7, 7, 2, { nohi: true });
  box(p, x + 4, y + 15, 38, 3, PAL.wood, { nohi: true }); R(p, x + 5, y + 18, 3, 4, PAL.wood[0]); R(p, x + 37, y + 18, 3, 4, PAL.wood[0]);
  for (let i = 5; i < 41; i++) { S(p, x + i, y + 14, PAL.linen[1]); S(p, x + i, y + 13, PAL.linen[2]); S(p, x + i, y + 12, PAL.linen[3]); }
  // the blanket, thrown back at the head as if someone just got up
  for (let j = 9; j < 17; j++) for (let i = 16; i < 41; i++) {
    if (j > 14 && i < 38) continue;
    const edge = j === 9 || i === 40 || i === 16 || (j === 16) ; let c = edge ? PAL.indigo[0] : j === 10 ? PAL.indigo[3] : PAL.indigo[2];
    if (j === 12 && !edge) c = (i % 4 === 0) ? PAL.ochre[4] : PAL.ochre[2];
    if (i === 39 && j > 10 && !edge) c = PAL.indigo[1];
    S(p, x + i, y + j, c);
  }
  for (let j = 10; j < 14; j++) { S(p, x + 17, y + j, PAL.indigo[4]); S(p, x + 18, y + j, PAL.indigo[3]); }
  box(p, x + 5, y + 8, 10, 5, PAL.linen, { round: true }); S(p, x + 7, y + 9, PAL.linen[4]); S(p, x + 13, y + 11, PAL.linen[1]);
  dust(p, x + 17, y + 8, 22); dust(p, x + 41, y + 6, 5);
} };
PIECES.cradle = { w: 20, h: 30, anchor: 'floor', glow: 'arc', draw(p, x, y, o) {
  for (let i = 0; i < 20; i++) { const yy = y + 29 - Math.round(Math.pow((i - 9.5) / 9.5, 2) * 3); S(p, x + i, yy, PAL.wood[0]); S(p, x + i, yy - 1, PAL.wood[2]); }
  R(p, x + 4, y + 24, 2, 4, PAL.wood[0]); R(p, x + 14, y + 24, 2, 4, PAL.wood[0]);
  for (let j = 15; j < 25; j++) for (let i = 1; i < 19; i++) {
    const e = j === 15 || j === 24 || i === 1 || i === 18;
    S(p, x + i, y + j, e ? PAL.woodOld[0] : ((i + j) % 3 === 0 ? PAL.woodOld[1] : (i + (j >> 1)) % 2 ? PAL.woodOld[3] : PAL.woodOld[2]));
  }
  for (let i = 3; i < 17; i++) S(p, x + i, y + 14, i % 2 ? PAL.indigo[3] : PAL.indigo[2]); S(p, x + 3, y + 13, PAL.linen[3]); S(p, x + 4, y + 13, PAL.linen[4]); S(p, x + 5, y + 13, PAL.linen[3]);
  // the mobile: a bent post, an arm, four little arc stars on threads
  R(p, x + 18, y + 2, 1, 13, A[0]); S(p, x + 18, y + 6, A[4]); R(p, x + 5, y + 1, 14, 1, A[0]); S(p, x + 5, y + 2, A[0]);
  for (const [bx, len] of [[7, 3], [10, 6], [13, 4], [16, 7]]) {
    for (let j = 2; j < 2 + len; j++) S(p, x + bx, y + j, A[1]);
    const by = y + 3 + len;
    if (o.lit) { G(p, x + bx, by, ARC.core, 1); for (const [a, b] of [[-1, 0], [1, 0], [0, -1], [0, 1]]) G(p, x + bx + a, by + b, ARC.mid, 0.6); }
    else { S(p, x + bx, by, GLASS); S(p, x + bx - 1, by, A[1]); S(p, x + bx + 1, by, A[1]); }
  }
  if (o.lit) { p.light({ x: x + 11, y: y + 8, col: ARC.light, r: 3.4, k: 0.6 }); p.emitters.push({ kind: 'mobile', x: x + 11.5, y: y + 8, rx: 5, ry: 1.5 }); }
  dust(p, x + 1, y + 14, 3); dust(p, x + 16, y + 14, 3);
} };
PIECES.wardrobe = { w: 24, h: 46, anchor: 'floor', draw(p, x, y, o) {
  box(p, x, y + 3, 24, 40, PAL.wood, { grain: true }); abox(p, x - 1, y, 26, 3, { nohi: true });
  for (let j = 4; j < 42; j++) S(p, x + 12, y + j, PAL.wood[0]);
  for (let j = 5; j < 41; j++) for (let i = 13; i < 23; i++) S(p, x + i, y + j, i < 15 ? hex('#0d0a08') : hex('#16100c'));
  for (let j = 8; j < 34; j++) { S(p, x + 15, y + j, PAL.red[1]); S(p, x + 16, y + j, j < 28 ? PAL.red[3] : PAL.red[2]); S(p, x + 17, y + j, PAL.red[2]); S(p, x + 18, y + j, PAL.red[1]); }
  R(p, x + 14, y + 7, 8, 1, A[2]);
  for (let j = 4; j < 42; j++) S(p, x + 23 + (j < 6 || j > 39 ? 0 : 1), y + j, j % 9 === 0 ? PAL.wood[1] : PAL.wood[3]);
  for (let j = 8; j < 40; j += 10) { S(p, x + 2, y + j, PAL.wood[1]); S(p, x + 2, y + j + 1, PAL.wood[1]); }
  R(p, x + 10, y + 20, 1, 5, A[4]); S(p, x + 10, y + 19, A[5]);
  R(p, x + 1, y + 43, 4, 3, PAL.wood[0]); R(p, x + 19, y + 43, 4, 3, PAL.wood[0]);
  dust(p, x, y - 1, 24);
} };
PIECES.chest = { w: 20, h: 14, anchor: 'floor', glow: 'arc', draw(p, x, y, o) {
  box(p, x, y + 5, 20, 9, PAL.wood, { grain: true }); box(p, x, y, 20, 6, PAL.wood, { round: true });
  for (const bx of [3, 16]) for (let j = 0; j < 14; j++) S(p, x + bx, y + j, j === 0 || j === 13 ? A[0] : j < 6 ? A[5] : A[4]);
  abox(p, x + 8, y + 5, 4, 5, { nohi: true });
  if (o.lit) { G(p, x + 9, y + 7, ARC.hi, 0.9); G(p, x + 10, y + 7, ARC.mid, 0.7); p.light({ x: x + 10, y: y + 7, col: ARC.light, r: 2, k: 0.4 }); } else S(p, x + 9, y + 7, GLASS);
  dust(p, x + 1, y - 1, 18);
} };
PIECES.desk = { w: 36, h: 32, anchor: 'floor', glow: 'arc', draw(p, x, y, o) {
  const top = y + 14;
  box(p, x, top, 36, 4, PAL.wood, { grain: true });
  box(p, x + 22, top + 4, 13, 14, PAL.wood); for (const dy of [4, 9]) { R(p, x + 23, top + 4 + dy, 11, 1, PAL.wood[0]); S(p, x + 28, top + 2 + dy, A[5]); }
  R(p, x + 2, top + 4, 4, 14, PAL.wood[0]); R(p, x + 3, top + 4, 2, 13, PAL.wood[2]); S(p, x + 3, top + 5, PAL.wood[4]);
  // the slanted screen on its stand: dead glass, a glyph burnt in, one pixel that never went out
  R(p, x + 11, top - 3, 6, 3, A[0]); R(p, x + 12, top - 3, 4, 1, A[4]);
  for (let j = 0; j < 12; j++) for (let i = 0; i < 18; i++) { const X = x + 4 + i + (j >> 2), Y = y + j; const e = i === 0 || i === 17 || j === 0 || j === 11;
    S(p, X, Y, e ? A[0] : (j === 1 || i === 1) ? A[3] : (i + j) % 13 === 0 ? hex('#1b2a33') : hex('#0e161c')); }
  for (const [a, b] of [[4, 3], [5, 3], [6, 3], [9, 3], [10, 3], [4, 5], [7, 5], [8, 5], [11, 5], [4, 7], [5, 7], [8, 7]]) S(p, x + 4 + a + (b >> 2), y + b, hex('#22404c'));
  if (o.lit) { G(p, x + 18, y + 7, ARC.hi, 0.9); p.emitters.push({ kind: 'pixel', x: x + 18, y: y + 7 }); }
  // an open scroll case and a sheet on the desk
  R(p, x + 24, top - 2, 8, 2, PAL.copper[2]); S(p, x + 24, top - 2, PAL.copper[0]); S(p, x + 31, top - 2, PAL.copper[4]); R(p, x + 32, top - 1, 3, 1, PAL.paper[3]); R(p, x + 18, top - 1, 4, 1, PAL.paper[4]);
  dust(p, x + 1, top - 1, 9);
} };
PIECES.bookshelf = { w: 32, h: 48, anchor: 'floor', glow: 'violet', draw(p, x, y, o) {
  box(p, x, y, 32, 48, PAL.wood);
  for (let j = 2; j < 46; j++) for (let i = 2; i < 30; i++) S(p, x + i, y + j, hex('#140e0a'));
  for (const sy of [14, 29, 44]) box(p, x + 1, y + sy, 30, 3, PAL.wood, { nohi: true });
  const caseAt = (cx, cy, col, glowing) => { for (let i = 0; i < 10; i++) { S(p, x + cx + i, y + cy, col[0]); S(p, x + cx + i, y + cy + 1, i === 0 || i === 9 ? col[4] : col[3]); S(p, x + cx + i, y + cy + 2, col[2]); S(p, x + cx + i, y + cy + 3, col[0]); }
    if (glowing) { G(p, x + cx + 5, y + cy + 1, VIO.hi, 0.9); G(p, x + cx + 4, y + cy + 2, VIO.mid, 0.6); G(p, x + cx + 6, y + cy + 2, VIO.mid, 0.6); } };
  caseAt(3, 10, PAL.copper); caseAt(4, 6, PAL.copper); caseAt(18, 10, A.slice(1));
  caseAt(12, 25, PAL.copper, true); caseAt(3, 40, PAL.copper); caseAt(14, 40, A.slice(1));
  for (const [cx, cy] of [[24, 38], [26, 40]]) { R(p, x + cx, y + cy, 4, 4, PAL.paper[2]); R(p, x + cx, y + cy, 4, 1, PAL.paper[4]); S(p, x + cx + 3, y + cy + 1, PAL.paper[0]); }
  for (let i = 0; i < 5; i++) S(p, x + 4 + i, y + 27 - i, PAL.paper[3]);
  p.light({ x: x + 17, y: y + 27, col: VIO.light, r: 2.4, k: 0.45 });
  p.emitters.push({ kind: 'pulse', x: x + 17, y: y + 27, r: 12, col: [190, 150, 255], amp: 0.35, slow: 1 });
  dust(p, x, y - 1, 32);
} };
PIECES.arcDevice = { w: 22, h: 50, anchor: 'floor', glow: 'arc', draw(p, x, y, o) {
  const cx = x + 11;
  abox(p, x + 1, y + 44, 20, 6); for (let i = 4; i < 19; i += 5) S(p, x + i, y + 46, A[1]);
  for (let j = 16; j < 44; j++) { const ring = j % 7 === 2 || j === 43; for (let i = -3; i <= 3; i++) S(p, cx + i, y + j, Math.abs(i) === 3 ? (ring ? A[0] : PAL.copper[0]) : ring ? (i < 0 ? A[4] : A[2]) : (j % 2 ? PAL.copper[i < -1 ? 4 : i > 1 ? 2 : 3] : PAL.copper[1])); if (j % 7 === 2) { S(p, cx - 4, y + j, A[0]); S(p, cx + 4, y + j, A[0]); } }
  for (let j = 12; j < 16; j++) { S(p, cx - 1, y + j, A[0]); S(p, cx, y + j, A[4]); S(p, cx + 1, y + j, A[0]); }
  for (let yy = -6; yy <= 6; yy++) for (let xx = -6; xx <= 6; xx++) { const d = Math.sqrt(xx * xx + yy * yy); if (d > 6.3) continue; const X = cx + xx, Y = y + 6 + yy;
    if (d > 5.4) S(p, X, Y, A[0]); else if (xx < -1 && yy < -1 && d > 2) S(p, X, Y, A[5]); else if (xx + yy < 2) S(p, X, Y, A[4]); else S(p, X, Y, A[2]); }
  S(p, cx - 3, y + 3, hex('#ffffff')); S(p, cx - 2, y + 3, A[5]);
  R(p, x + 1, y + 24, 2, 20, A[0]); for (let j = 25; j < 44; j++) S(p, x + 2, y + j, A[3]); box(p, x, y + 20, 4, 4, A.slice(1), { round: true });
  if (o.lit) {
    G(p, cx - 1, y + 5, ARC.hi, 0.45); G(p, cx + 2, y + 8, ARC.mid, 0.35);
    p.light({ x: cx, y: y + 6, col: ARC.light, r: 9, k: 1.1, p: 1.4 });
    p.emitters.push({ kind: 'arc', a: [cx - 5, y + 8], b: [x + 2, y + 21], rate: 0.06, life: 6, w: 1, pal: 'cyan', light: 1 });
    p.emitters.push({ kind: 'arc', a: [cx + 4, y + 3], b: 'air', rate: 0.05, life: 5, w: 0.8, pal: 'cyan', light: 0.6, reach: 12 });
    p.emitters.push({ kind: 'arc', a: [cx - 3, y + 18], b: [cx + 3, y + 36], rate: 0.03, life: 4, w: 0.5, pal: 'cyan' });
    p.emitters.push({ kind: 'pulse', x: cx, y: y + 6, r: 44, col: [120, 200, 255], amp: 0.1 });
  }
} };
PIECES.basin = { w: 34, h: 24, anchor: 'floor', draw(p, x, y, o) {
  box(p, x, y + 10, 34, 14, PAL.stone, { round: true });
  for (let i = 2; i < 32; i++) { S(p, x + i, y + 11, hex('#0e0e12')); S(p, x + i, y + 12, i % 3 ? hex('#b8b4a4') : hex('#8e8a7c')); S(p, x + i, y + 13, hex('#3a3a40')); }
  for (let i = 3; i < 31; i += 5) S(p, x + i, y + 19, PAL.stone[1]);
  R(p, x + 26, y, 3, 8, A[0]); S(p, x + 27, y + 1, A[4]); R(p, x + 22, y + 2, 5, 2, A[0]); R(p, x + 22, y + 2, 1, 5, A[0]); S(p, x + 22, y + 6, ARC.lo);
  abox(p, x + 25, y - 1, 5, 2, { nohi: true });
  R(p, x + 5, y + 6, 1, 5, PAL.wood[3]); R(p, x + 4, y + 11, 3, 2, PAL.wood[1]);
  dust(p, x + 1, y + 9, 32);
} };
PIECES.plantAlive = { w: 18, h: 30, anchor: 'floor', glow: 'teal', draw(p, x, y, o) {
  const b = y + 29;
  for (let j = 0; j < 9; j++) { const hw = j > 6 ? 6 : 5; for (let i = -hw; i <= hw; i++) S(p, x + 9 + i, b - j, Math.abs(i) === hw || j === 0 ? PAL.glaze[0] : i < -2 ? PAL.glaze[3] : i > 2 ? PAL.glaze[1] : PAL.glaze[2]); }
  R(p, x + 2, b - 9, 15, 1, PAL.glaze[0]); R(p, x + 4, b - 9, 11, 1, hex('#2a1c14')); S(p, x + 5, b - 5, PAL.glaze[4]);
  const stem = (sx, len, bend) => { let X = sx; for (let k = 0; k < len; k++) { if (k > 3 && k % 4 === 0) X += bend; S(p, x + X, b - 10 - k, PAL.plant[k % 5 === 0 ? 3 : 2]); if (k % 5 === 2) { S(p, x + X - (bend || 1), b - 10 - k, PAL.plant[3]); S(p, x + X - 2 * (bend || 1), b - 11 - k, PAL.plant[4]); } } return X; };
  const tips = [[stem(9, 17, 0), 17], [stem(7, 12, -1), 12], [stem(11, 10, 1), 10]];
  for (const [tx, len] of tips) { const ty = b - 10 - len; G(p, x + tx, ty, TEAL.hi, 1); G(p, x + tx - 1, ty, TEAL.mid, 0.8); G(p, x + tx + 1, ty, TEAL.mid, 0.8); G(p, x + tx, ty - 1, TEAL.mid, 0.8); G(p, x + tx, ty + 1, TEAL.lo, 0.6); }
  p.light({ x: x + 9, y: b - 22, col: TEAL.light, r: 3.6, k: 0.7 });
  p.emitters.push({ kind: 'spore', x: x + 9, y: b - 22, col: [80, 230, 200], n: 4 });
} };
PIECES.fallenBeam = { w: 56, h: 30, anchor: 'floor', draw(p, x, y, o) {
  // an alloy I-beam fallen from the ceiling: one end on the floor, the other still high
  for (let k = 0; k < 56; k++) { const cy = y + Math.round(k * 0.4);
    for (let d = 0; d < 8; d++) { const c = d === 0 || d === 7 ? A[0] : d === 1 ? A[5] : d === 2 ? A[3] : d === 6 ? A[1] : d === 5 ? A[2] : (k % 9 === 4 ? A[4] : A[2]); S(p, x + k, cy + d, c); } }
  for (let k = 4; k < 56; k += 9) { S(p, x + k, y + Math.round(k * 0.4) + 3, A[5]); S(p, x + k, y + Math.round(k * 0.4) + 4, A[0]); }
  for (let d = 0; d < 8; d++) S(p, x, y + d, A[0]);
  for (const [a, b] of [[1, -1], [2, -2], [3, -1], [0, 8], [1, 9]]) S(p, x + a, y + b, A[2]);
  dust(p, x + 3, y + 1, 50);
} };
PIECES.hearth = { w: 36, h: 50, anchor: 'floor', glow: 'hot', draw(p, x, y, o) {
  const cx = x + 18;
  if (o.flue) { for (let j = -o.flue; j < 0; j++) for (let i = -3; i <= 3; i++) S(p, cx + i, y + j, Math.abs(i) === 3 ? A[0] : i === -2 ? A[4] : (j % 10 === 0 ? A[1] : A[2])); }
  for (let j = 0; j < 22; j++) { const hw = 4 + Math.floor(j / 4); for (let i = -hw; i <= hw; i++) S(p, cx + i, y + j, Math.abs(i) === hw ? A[0] : i === -hw + 1 ? A[4] : j % 7 === 6 ? A[1] : A[2]); }
  emblem(p, cx, y + 13, PAL.ochre[2], PAL.ochre[4]);
  abox(p, x + 1, y + 22, 34, 4);
  box(p, x + 3, y + 26, 30, 24, PAL.stone);
  for (let j = 30; j < 45; j++) for (let i = 8; i < 28; i++) { const e = j === 30 || i === 8 || i === 27; S(p, x + i, y + j, e ? PAL.stone[0] : hex('#100a08')); }
  const coil = [];
  for (let i = 10; i < 26; i++) { const ph = i % 4; coil.push([x + i, y + 37 + (ph === 0 ? -2 : ph === 1 ? -1 : ph === 2 ? 0 : -1) + (ph === 2 ? 1 : 0)]); }
  for (const [px, py] of coil) { if (o.lit) { G(p, px, py, HOT.hi, 1.05); G(p, px, py + 1, HOT.mid, 0.8); } else { S(p, px, py, PAL.copper[1]); S(p, px, py + 1, PAL.copper[0]); } }
  R(p, x + 9, y + 34, 1, 5, A[4]); R(p, x + 26, y + 34, 1, 5, A[4]);
  if (o.lit) {
    for (let i = 9; i < 27; i++) G(p, x + i, y + 43, HOT.lo, 0.5);
    p.light({ x: cx, y: y + 38, col: HOT.light, r: 8, k: 1.15, p: 1.4 });
    arcE(p, [x + 10, y + 33], [x + 25, y + 33], { rate: 0.05, life: 5, w: 0.7, light: 0.4 });
    p.emitters.push({ kind: 'pulse', x: cx, y: y + 38, r: 36, col: [255, 150, 70], amp: 0.12 });
    p.emitters.push({ kind: 'embers', x: cx, y: y + 33, w: 16, col: [255, 170, 90] });
  }
  for (let i = 5; i < 31; i += 5) S(p, x + i, y + 47, PAL.stone[1]);
} };

PIECES.holoPortrait = { w: 18, h: 20, anchor: 'wall', glow: 'holo', draw(p, x, y, o) {
  // a frame whose bottom bar projects the family: two grown-ups and a child, cut by scanlines
  abox(p, x, y + 16, 18, 4); R(p, x + 3, y + 16, 12, 1, A[1]);
  for (const ex of [5, 9, 13]) { if (o.lit) G(p, x + ex, y + 17, HOLO.hi, 0.9); else S(p, x + ex, y + 17, GLASS); }
  if (!o.lit) return;
  const shape = [];
  const bust = (cx, hy, hr, sy, sw) => { for (let j = -4; j <= 15; j++) for (let i = -6; i <= 6; i++) { const X = cx + i, Y = hy + j; const inHead = (i * i) / (hr * hr) + (j * j) / ((hr + 0.6) * (hr + 0.6)) <= 1; const inBody = Y >= sy && Y <= 15 && Math.abs(i) <= sw - Math.max(0, sy + 1 - Y); if (inHead || inBody) shape.push([X, Y]); } };
  bust(5, 5, 2.4, 9, 4); bust(13, 6, 2.4, 10, 4); bust(9, 10, 1.6, 13, 2);
  const has = new Set(shape.map(([a, b]) => a + ',' + b));
  for (const [X, Y] of shape) {
    if (Y < 1 || Y > 15 || X < 1 || X > 16) continue;
    const edge = !has.has((X - 1) + ',' + Y) || !has.has((X + 1) + ',' + Y) || !has.has(X + ',' + (Y - 1)) || !has.has(X + ',' + (Y + 1));
    const scan = Y % 2 === 0;
    if (edge) G(p, x + X, y + Y, HOLO.mid, scan ? 0.6 : 0.4); else if (scan) G(p, x + X, y + Y, HOLO.lo, 0.22);
  }
  for (const [X, Y] of [[4, 4], [12, 5], [9, 9]]) G(p, x + X, y + Y, HOLO.hi, 0.7);
  p.light({ x: x + 9, y: y + 9, col: HOLO.light, r: 3.4, k: 0.45 });
  p.emitters.push({ kind: 'holo', x: x + 1, y: y + 1, w: 16, h: 15 });
} };
PIECES.tapestry = { w: 48, h: 30, anchor: 'wall', draw(p, x, y, o) {
  abox(p, x - 1, y, 50, 2, { nohi: true });
  const base = (i, j) => mix(PAL.indigo[1], PAL.indigo[2], 0.35 + 0.5 * (hash(i >> 1, j, 41) * 0.4 + 0.6 * ((i + j * 2) % 9 < 5 ? 1 : 0.6)));
  const cloth = (i, j) => !(i > 38 && j > 18 && (i - 38) + (j - 18) > 12);
  for (let j = 2; j < 28; j++) for (let i = 0; i < 48; i++) {
    if (!cloth(i, j)) continue;
    const edge = i === 0 || i === 47 || j === 2 || j === 27 || !cloth(i + 1, j) || !cloth(i, j + 1);
    let c = edge ? PAL.indigo[0] : (i === 2 || i === 45 || j === 4 || j === 25) ? PAL.ochre[1] : base(i, j);
    if (!edge && (j === 4 || j === 25) && i % 4 === 0) c = PAL.ochre[2];
    S(p, x + i, y + j, c);
  }
  for (let i = 1; i < 39; i += 2) S(p, x + i, y + 28, PAL.ochre[1]);
  // a woven scene, faded: a spire with its lamp sending an arc into the sky, the makers in a ring under it
  const ox = x + 24, oy = y + 23;
  for (let j = 0; j < 15; j++) { const hw = j < 3 ? 0 : j < 8 ? 1 : 2; for (let i = -hw; i <= hw; i++) S(p, ox + i, oy - j, i < 0 ? PAL.linen[2] : PAL.linen[1]); }
  S(p, ox, oy - 15, hex('#9fdcf0')); S(p, ox, oy - 16, hex('#cdeef8'));
  for (const [a, b] of [[1, -17], [2, -18], [3, -17], [4, -18], [5, -19], [6, -18], [-1, -17], [-2, -18], [-3, -17], [-4, -18], [-5, -19]]) S(p, ox + a, oy + b, hex('#6fb4d8'));
  for (let i = 3; i < 45; i++) S(p, x + i, oy + 1, PAL.ochre[1]);
  const figs = [-16, -12, -8, 8, 12, 16], cols = [PAL.red, PAL.ochre, PAL.red, PAL.red, PAL.ochre, PAL.red];
  figs.forEach((fx, n) => { const c = cols[n]; R(p, ox + fx, oy - 3, 2, 4, c[2]); S(p, ox + fx, oy - 3, c[3]); R(p, ox + fx, oy - 6, 2, 2, PAL.paper[3]); });
  for (const [a, b] of [[-14, -11], [-10, -9], [10, 11], [14, 15]]) for (let i = a; i <= b; i++) S(p, ox + i, oy - 2, PAL.paper[2]);
  for (let k = 0; k < 7; k++) S(p, x + 5 + k * 6, y + 7 + (k % 2), PAL.ochre[2]);
  p.emitters.push({ kind: 'sway', x: x + 24, y: y + 2 });
} };

// ================= LIVED IN =================
PIECES.herbs = { w: 11, h: 16, anchor: 'ceiling', draw(p, x, y, o) {
  // bundles of something that grew here once, hung head down to dry, tied with red thread
  for (const [bx, len, c] of [[2, 6, PAL.dry], [5, 9, PAL.plant], [8, 5, PAL.dry]]) {
    for (let j = 0; j < len; j++) S(p, x + bx, y + j, A[1]);
    const ty = y + len; S(p, x + bx, ty, PAL.red[3]); S(p, x + bx - 1, ty, PAL.red[1]);
    for (let j = 1; j < 7; j++) { const hw = j < 2 ? 1 : j < 5 ? 2 : 1; for (let i = -hw; i <= hw; i++) S(p, x + bx + i, ty + j, Math.abs(i) === hw ? c[0] : (i + j) % 2 ? c[2] : c[3]); }
    S(p, x + bx - 1, ty + 7, c[1]); S(p, x + bx + 1, ty + 7, c[2]);
  }
  p.emitters.push({ kind: 'sway', x: x + 5, y });
} };
PIECES.utensils = { w: 28, h: 18, anchor: 'wall', draw(p, x, y, o) {
  // a rail of alloy over the hearth: a ladle, a pan, a knife, a whisk of arc wire
  R(p, x, y + 1, 28, 2, A[0]); R(p, x + 1, y + 1, 26, 1, A[4]); S(p, x - 1, y + 1, A[0]); S(p, x + 28, y + 1, A[0]);
  const hook = (hx) => { S(p, x + hx, y + 3, A[0]); S(p, x + hx, y + 4, A[3]); };
  hook(3); R(p, x + 3, y + 5, 1, 7, PAL.wood[3]); box(p, x + 1, y + 11, 5, 4, PAL.copper, { round: true, nohi: true });
  hook(10); R(p, x + 10, y + 5, 1, 3, PAL.wood[2]);
  for (let yy = -4; yy <= 4; yy++) for (let xx = -4; xx <= 4; xx++) { const d = Math.sqrt(xx * xx + yy * yy); if (d > 4.3) continue; S(p, x + 10 + xx, y + 12 + yy, d > 3.4 ? PAL.copper[0] : xx < 0 && yy < 0 ? PAL.copper[4] : PAL.copper[2]); }
  S(p, x + 9, y + 10, PAL.copper[4]);
  hook(18); R(p, x + 18, y + 5, 1, 3, PAL.wood[1]); for (let j = 8; j < 15; j++) { S(p, x + 18, y + j, A[5]); S(p, x + 19, y + j, j < 14 ? A[3] : A[0]); }
  hook(24); for (let j = 5; j < 13; j++) { S(p, x + 23 + ((j >> 1) % 2), y + j, PAL.copper[3]); S(p, x + 25 - ((j >> 1) % 2), y + j, PAL.copper[2]); }
  R(p, x + 23, y + 13, 3, 1, A[0]);
  dust(p, x, y, 28);
} };
PIECES.frame = { w: 24, h: 18, anchor: 'wall', draw(p, x, y, o) {
  // a painting of the sky they had: two moons over a ridge and a spire with its lamp lit
  box(p, x, y, 24, 18, PAL.wood, { nohi: true });
  for (let j = 2; j < 16; j++) for (let i = 2; i < 22; i++) {
    const t = (j - 2) / 13; let c = mix(hex('#5a4a78'), hex('#1e3a44'), t);
    const ridge = 11 + Math.round(1.5 * Math.sin(i * 0.6)) - (i > 13 && i < 17 ? 1 : 0); if (j >= ridge) c = j === ridge ? hex('#2a2a30') : hex('#1a1a20');
    if (hash(i, j, 61) < 0.12) c = mul(c, 0.85);
    S(p, x + i, y + j, c);
  }
  for (const [mx, my, r, c] of [[6, 5, 2, hex('#e8dcc0')], [11, 4, 1, hex('#b8c8d8')]]) for (let a = -r; a <= r; a++) for (let b = -r; b <= r; b++) if (a * a + b * b <= r * r + 0.5) S(p, x + mx + a, y + my + b, c);
  for (let j = 6; j < 12; j++) S(p, x + 17, y + j, hex('#2a2a30')); S(p, x + 16, y + 10, hex('#2a2a30')); S(p, x + 18, y + 10, hex('#2a2a30')); S(p, x + 17, y + 5, hex('#9fe8ff'));
  S(p, x + 1, y + 1, PAL.wood[4]); dust(p, x + 1, y - 1, 22);
} };
PIECES.drawings = { w: 26, h: 14, anchor: 'wall', draw(p, x, y, o) {
  // a child's drawings pinned low on the wall: the family, the spire with a bolt, a sun
  const sheet = (sx, sy, w, h) => { R(p, x + sx, y + sy, w, h, PAL.paper[3]); R(p, x + sx, y + sy + h - 1, w, 1, PAL.paper[2]); R(p, x + sx + w - 1, y + sy, 1, h, PAL.paper[2]); S(p, x + sx + (w >> 1), y + sy, A[5]); };
  sheet(0, 2, 8, 10); sheet(9, 0, 8, 11); sheet(18, 3, 8, 10);
  for (const fx of [2, 4, 6]) { const h = fx === 6 ? 2 : 3; S(p, x + fx, y + 10 - h - 1, PAL.red[3]); for (let j = 0; j < h; j++) S(p, x + fx, y + 10 - j, PAL.red[2]); }
  for (let j = 3; j < 10; j++) S(p, x + 13, y + j, PAL.indigo[3]); S(p, x + 12, y + 9, PAL.indigo[3]); S(p, x + 14, y + 9, PAL.indigo[3]);
  for (const [a, b] of [[14, 2], [15, 3], [14, 4], [15, 5]]) S(p, x + a, y + b, PAL.ochre[4]);
  for (const [a, b] of [[22, 7], [21, 7], [23, 7], [22, 6], [22, 8]]) S(p, x + a, y + b, PAL.ochre[3]);
  for (const [a, b] of [[20, 5], [24, 5], [20, 9], [24, 9]]) S(p, x + a, y + b, PAL.ochre[2]);
} };
PIECES.heightMarks = { w: 7, h: 40, anchor: 'wall', draw(p, x, y, o) {
  // notches cut in a door post as a child grew, each with a little glyph; the last one is low
  for (const [ny, g] of [[30, 1], [24, 2], [19, 3], [15, 4]]) {
    R(p, x + 1, y + ny, 5, 1, hex('#120e0c')); R(p, x + 1, y + ny + 1, 5, 1, mix(PAL.paper[2], PAL.stone[3], 0.5));
    for (let k = 0; k < g; k++) S(p, x + 1 + k, y + ny - 2, PAL.ochre[2]);
  }
} };
PIECES.wallShelf = { w: 26, h: 13, anchor: 'wall', draw(p, x, y, o) {
  box(p, x, y + 10, 26, 3, PAL.wood, { nohi: true });
  for (const bx of [3, 21]) { S(p, x + bx, y + 13, A[0]); S(p, x + bx + 1, y + 14, A[0]); }
  // three codices, one leaning, a scroll case lying, a cup
  const spine = (sx, h, c) => { for (let j = 0; j < h; j++) { S(p, x + sx, y + 9 - j, c[0]); S(p, x + sx + 1, y + 9 - j, j === 2 ? PAL.ochre[3] : c[3]); S(p, x + sx + 2, y + 9 - j, c[2]); } };
  spine(2, 8, PAL.red); spine(5, 7, PAL.indigo); spine(8, 9, PAL.glaze);
  for (let j = 0; j < 7; j++) { S(p, x + 12 + (j >> 1), y + 9 - j, PAL.woodOld[0]); S(p, x + 13 + (j >> 1), y + 9 - j, PAL.woodOld[3]); }
  R(p, x + 16, y + 7, 7, 3, PAL.copper[2]); R(p, x + 16, y + 7, 7, 1, PAL.copper[4]); S(p, x + 16, y + 8, PAL.copper[0]); S(p, x + 22, y + 8, PAL.copper[0]);
  box(p, x + 23, y + 6, 3, 4, PAL.ceramic, { nohi: true });
  dust(p, x + 1, y + 9, 24);
} };
PIECES.basket = { w: 14, h: 10, anchor: 'floor', draw(p, x, y, o) {
  for (let j = 3; j < 10; j++) for (let i = 1; i < 13; i++) { const e = j === 3 || j === 9 || i === 1 || i === 12; S(p, x + i, y + j, e ? PAL.woodOld[0] : (i + (j >> 1)) % 2 ? PAL.woodOld[3] : PAL.woodOld[2]); }
  for (let i = 2; i < 9; i++) S(p, x + i, y + 2, i % 2 ? PAL.indigo[3] : PAL.indigo[2]); R(p, x + 1, y + 3, 2, 4, PAL.indigo[1]);
  box(p, x + 8, y, 5, 4, PAL.ochre, { round: true }); S(p, x + 9, y + 1, PAL.ochre[4]); line(p, x + 12, y + 3, x + 15, y + 9, (a, b) => S(p, a, b, PAL.ochre[2]));
} };
PIECES.coatPegs = { w: 20, h: 30, anchor: 'wall', draw(p, x, y, o) {
  abox(p, x, y, 20, 3, { nohi: true }); for (const px of [3, 10, 16]) { S(p, x + px, y + 3, A[0]); S(p, x + px, y + 4, A[4]); }
  // a long red scarf, a small coat, a satchel
  for (let j = 4; j < 28; j++) { S(p, x + 2, y + j, PAL.red[j % 5 === 0 ? 4 : 3]); S(p, x + 3, y + j, PAL.red[2]); S(p, x + 4, y + j, j > 6 ? PAL.red[1] : PAL.red[2]); }
  for (let i = 1; i < 6; i++) if (i % 2) S(p, x + i, y + 28, PAL.red[3]);
  for (let j = 5; j < 20; j++) { const hw = j < 7 ? 2 : 3; for (let i = -hw; i <= hw; i++) S(p, x + 10 + i, y + j, Math.abs(i) === hw ? PAL.indigo[0] : i < 0 ? PAL.indigo[3] : PAL.indigo[2]); }
  S(p, x + 10, y + 9, PAL.ochre[3]); S(p, x + 10, y + 13, PAL.ochre[3]);
  line(p, x + 16, y + 5, x + 14, y + 12, (a, b) => S(p, a, b, PAL.woodOld[1])); line(p, x + 16, y + 5, x + 18, y + 12, (a, b) => S(p, a, b, PAL.woodOld[1]));
  box(p, x + 13, y + 12, 7, 6, PAL.woodOld, { round: true }); S(p, x + 16, y + 14, A[4]);
} };
PIECES.arcCandle = { w: 6, h: 11, anchor: 'floor', glow: 'arc', draw(p, x, y, o) {
  // a table light: two pins on a little stand with a spark held between them, like a candle's flame
  abox(p, x, y + 9, 6, 2, { nohi: true }); R(p, x + 2, y + 5, 2, 4, A[0]); S(p, x + 2, y + 5, A[4]);
  S(p, x + 1, y + 1, A[0]); S(p, x + 1, y + 2, A[0]); S(p, x + 4, y + 1, A[0]); S(p, x + 4, y + 2, A[0]); R(p, x + 1, y + 3, 4, 2, A[1]);
  if (o.lit) { G(p, x + 2, y + 1, ARC.core, 1.1); G(p, x + 3, y + 1, ARC.hi, 1); G(p, x + 2, y + 2, ARC.mid, 0.8); G(p, x + 3, y, ARC.mid, 0.7);
    p.light({ x: x + 3, y: y + 1, col: ARC.light, r: 4.2, k: 0.85 }); arcE(p, [x + 1.5, y + 1.5], [x + 4.5, y + 1.5], { rate: 0.05, life: 3, w: 0.35 }); }
} };
PIECES.toolRack = { w: 32, h: 22, anchor: 'wall', draw(p, x, y, o) {
  // a board with painted outlines for every tool; one outline stands empty
  box(p, x, y, 32, 22, PAL.woodOld, { nohi: true });
  const out = (pts) => pts.forEach(([a, b]) => S(p, x + a, y + b, PAL.paper[1]));
  // hammer
  R(p, x + 4, y + 4, 2, 13, PAL.wood[3]); R(p, x + 2, y + 3, 6, 3, A[0]); R(p, x + 3, y + 4, 4, 1, A[4]);
  // tongs
  line(p, x + 10, y + 3, x + 12, y + 17, (a, b) => S(p, a, b, A[3])); line(p, x + 14, y + 3, x + 12, y + 17, (a, b) => S(p, a, b, A[2])); S(p, x + 12, y + 10, A[5]);
  // the empty outline of a drill with a coil
  for (let i = 16; i < 24; i++) { S(p, x + i, y + 6, PAL.paper[1]); S(p, x + i, y + 9, PAL.paper[1]); } for (let j = 6; j < 10; j++) S(p, x + 16, y + j, PAL.paper[1]); for (let j = 9; j < 15; j++) { S(p, x + 18, y + j, PAL.paper[1]); S(p, x + 20, y + j, PAL.paper[1]); }
  out([[24, 7], [25, 7], [26, 7], [24, 8], [25, 8]]);
  // a spool of copper wire
  box(p, x + 23, y + 13, 7, 7, PAL.copper, { round: true }); for (let j = 14; j < 19; j += 2) R(p, x + 24, y + j, 5, 1, PAL.copper[4]); R(p, x + 25, y + 16, 3, 1, A[0]);
  dust(p, x + 1, y - 1, 30);
} };

PIECES.holoPortrait = { w: 20, h: 22, anchor: 'wall', glow: 'holo', draw(p, x, y, o) {
  // a frame whose bottom bar projects the family: two grown-ups, a child between them, cut by scanlines
  abox(p, x + 1, y + 18, 18, 4); R(p, x + 4, y + 18, 12, 1, A[1]);
  for (const ex of [5, 10, 15]) { if (o.lit) G(p, x + ex, y + 19, HOLO.hi, 0.9); else S(p, x + ex, y + 19, GLASS); }
  if (!o.lit) return;
  const ADULT = ['..###..', '.#####.', '.#####.', '..###..', '...#...', '.#####.', '#######', '#######', '#######', '#######', '#######', '#######'];
  const LONG = ['..###..', '.#####.', '######.', '######.', '##.#...', '.#####.', '#######', '#######', '#######', '#######', '#######', '#######'];
  const CHILD = ['.###.', '#####', '#####', '.###.', '.###.', '#####', '#####', '#####'];
  const fig = (bm, ox, oy) => {
    const at = (i, j) => bm[j] && bm[j][i] === '#';
    for (let j = 0; j < bm.length; j++) for (let i = 0; i < bm[j].length; i++) {
      if (!at(i, j)) continue; const Y = oy + j, edge = !at(i - 1, j) || !at(i + 1, j) || !at(i, j - 1) || !at(i, j + 1);
      const X = x + ox + i, YY = y + Y; if (edge) G(p, X, YY, HOLO.mid, Y % 2 ? 0.6 : 0.95); else if (Y % 2 === 0) G(p, X, YY, HOLO.lo, 0.3); else continue; if (p.inb(X, YY)) p.nob[YY * p.w.PW + X] = 1;
    }
  };
  fig(LONG, 0, 6); fig(ADULT, 13, 5); fig(CHILD, 8, 10);
  // the beam the bar throws up, faint
  
  for (const [X, Y] of [[2, 7], [15, 6], [9, 11]]) G(p, x + X, y + Y, HOLO.hi, 0.6);
  p.light({ x: x + 10, y: y + 11, col: HOLO.light, r: 3.6, k: 0.5 });
  p.emitters.push({ kind: 'holo', x: x + 1, y: y + 2, w: 18, h: 16 });
} };

// place a piece by its anchor: floor (x, floorY), ceiling (x, ceilY), wall (x, top)
function put(p, id, x, y, o = {}) {
  const P = PIECES[id]; const oo = { lit: true, ...o };
  const top = P.anchor === 'floor' ? y - P.h : y;
  P.draw(p, Math.round(x), Math.round(top), oo);
  return { x, top, w: P.w, h: P.h };
}

module.exports = { PIECES, PAL, ARC, HOT, HOLO, TEAL, VIO, put, dust, blend, line, box, abox, R, emblem, globe };

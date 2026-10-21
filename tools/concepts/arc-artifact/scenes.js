// Bakes every picture of the page: per relic a dark cave card; three spires; two homes. Each scene is baked three times
// (lo: the hum's trough, hi: its crest, fx: a big discharge) so the page can breathe and flash the real light; plus 'off'
// (the relic taken) for the spires. node scenes.js [only]
const fs = require('fs'), path = require('path');
const L = require('./lib');
const { E, O, ROOM, D, hex, mix, mul, hash, VC, VG, AM, CU, CER, A, ST, Spr, line, rod, insulator, OLS, poly, ell, tone } = L;
const { C, P, Pic, lightUp } = E;
const { Room, paint } = ROOM;
const { put } = D;
const { DESIGNS, icon, veins, plate } = require('./designs');
const OUT = path.join(__dirname, 'out'); fs.mkdirSync(OUT, { recursive: true });
const only = process.argv[2];
const manifest = fs.existsSync(path.join(OUT, 'meta.json')) && only ? JSON.parse(fs.readFileSync(path.join(OUT, 'meta.json'), 'utf8')) : {};

function newPic(r) { r.shapes(); const p = new Pic(r); paint(p); p.art = new Uint8Array(r.PW * r.PH); return p; }
const artLight = (p, l) => p.light({ ...l, col: l.col || VC.light, art: true });
// place a relic: its sprite, its light, and its emitter for the live layer
function relic(p, d, S, x, bottom, o = {}) {
  const r = d.piece(S), s = r.spr, ox = Math.round(x - s.w / 2), oy = Math.round(bottom - s.h);
  if (o.off) {
    // taken: the cradle stays, the relic is gone
    const keep = new Set(['cu', 'cer', 'al', 'st']), t = new Spr(s.w, s.h);
    for (let y = 0; y < s.h; y++) for (let x = 0; x < s.w; x++) { const i = y * s.w + x; if (keep.has(s.m[i])) t.set(x, y, s.c[i], s.m[i]); }
    t.outline(OLS); t.blit(p, ox, oy);
  } else s.blit(p, ox, oy);
  const sh = ([a, b]) => [a + ox, b + oy], core = sh(r.core);
  if (!o.off) {
    artLight(p, { x: core[0], y: core[1], r: (o.r || r.r) * (o.rk || 1) * 1.15, k: (o.k || 1.15) * 1.35, p: 1.35 });
    artLight(p, { x: core[0], y: core[1], r: 2.4 * S, k: 0.9 });
    p.emitters.push({ kind: 'relic', core, src: r.src.map(sh), near: r.metal.map(sh), far: o.far || [], hero: o.hero || null, floor: o.floor || bottom, r: (o.r || r.r) * C * 0.9, S,
      rate: o.rate || 0.55, big: o.big || [2.6, 5.5], orbit: r.orbit ? { x: r.orbit.x + ox, y: r.orbit.y + oy, r: r.orbit.r } : null, hover: !!r.hover });
  }
  return { ox, oy, core, w: s.w, h: s.h };
}
function bake(p, id, o = {}) {
  const crop = o.crop || [0, 0, p.w.PW, p.w.PH], [cx0, cy0, cw, ch] = crop;
  const lights = p.lights, emis = p.emis, n = p.w.PW * p.w.PH;
  const run = (lk, rk, ek, suffix) => {
    p.lights = lights.map((l) => (l.art ? { ...l, k: l.k * lk, r: l.r * rk } : l));
    const e2 = new Float32Array(emis); for (let i = 0; i < n; i++) if (p.art[i]) { e2[i * 3] *= ek; e2[i * 3 + 1] *= ek; e2[i * 3 + 2] *= ek; }
    p.emis = e2;
    const full = lightUp(p, { amb: o.amb == null ? 0.03 : o.amb, bloom: o.bloom || 0.9 }), img = P.create(cw, ch);
    for (let y = 0; y < ch; y++) full.data.copy(img.data, y * cw * 4, ((y + cy0) * full.w + cx0) * 4, ((y + cy0) * full.w + cx0 + cw) * 4);
    P.encode(img, path.join(OUT, `${id}-${suffix}.png`));
  };
  if (o.off) run(1, 1, 1, 'off');
  else { run(0.62, 0.92, 0.7, 'lo'); run(1, 1, 1, 'hi'); run(2.3, 1.45, 1.35, 'fx'); }
  p.lights = lights; p.emis = emis;
  if (o.off) return;
  const sh = (pt) => [pt[0] - cx0, pt[1] - cy0];
  const em = p.emitters.map((e) => {
    const q = JSON.parse(JSON.stringify(e));
    for (const k of ['core', 'hero', 'flare', 'a', 'b']) if (q[k] && Array.isArray(q[k])) q[k] = sh(q[k]);
    for (const k of ['src', 'near', 'far', 'pts']) if (q[k]) q[k] = q[k].map(sh);
    if (q.floor != null) q.floor -= cy0; if (q.orbit) { q.orbit.x -= cx0; q.orbit.y -= cy0; }
    if (q.x != null) q.x -= cx0; if (q.y != null) q.y -= cy0; if (q.y0 != null) { q.y0 -= cy0; q.y1 -= cy0; } if (q.x0 != null) { q.x0 -= cx0; q.x1 -= cx0; }
    if (q.col && q.col.every((v) => v <= 1)) q.col = q.col.map((v) => Math.round(v * 255));
    return q;
  });
  manifest[id] = { size: [cw, ch], emitters: em };
}

// ---------------- the cards: each relic alone in a dark cave, a ruin's alloy wall at its right, the hero at its left ----------------
function card(d) {
  const r = new Room(14, 7);
  r.carve(1, 1, 12, 5, 'rock');
  for (const [x, y] of [[1, 1], [2, 1], [12, 1], [1, 5]]) r.F[r.i(x, y)] = 'rock';
  r.fill(11, 1, 3, 5, 'alloy'); r.fill(5, 6, 4, 1, 'alloy');
  const p = newPic(r), FL = 6 * C;
  const v = O.hero(p, 14, FL); O.helmetBeam(p, v.x, v.y, 6);
  const far = []; for (let y = 22; y <= 90; y += 6) far.push([176, y]); for (let x = 82; x <= 140; x += 8) far.push([x, FL]);
  far.push([60, 17], [90, 16], [130, 18]);
  relic(p, d, 1, 104, FL, { far, hero: [v.x - 1, v.y - 2], floor: FL });
  bake(p, 'card-' + d.id, { amb: 0.025 });
}

// ---------------- the spires ----------------
const SW = 25, SH = 44, AX = 8, ROOF = 15; // scene size in cells; the spire's left column; ground row at its roof
const surf = (x) => (x >= AX - 1 && x <= AX + 9 ? ROOF : ROOF - 1 + Math.round(1.3 * Math.sin(x * 0.55 + 0.6) + 0.7 * Math.sin(x * 1.37)) + (x < AX ? -1 : 0));
const SPIRE0 = [
  '    l    ', '    #    ', '   ###   ', '   ###   ', '   #.#   ', '  ##.##  ', '  #...#  ', '  s...s  ', '  s...s  ', '  s...s  ',
  '###...###', '#.......#', '#.......#', '####....#', '#.......#', '#.......#', '#....####', '#.......#', '#.......#', '####....#',
  '#.......#', '#.......#', '#....####', '#.......#', '#.......#', '####....#', '#.......r', '#....c..#', '#########', '#########',
];
function ground(extra) {
  const r = new Room(SW, SH);
  for (let x = 0; x < SW; x++) for (let y = 0; y < SH; y++) {
    const s = surf(x), i = r.i(x, y);
    if (y < s) { r.F[i] = null; r.B[i] = null; } else { r.F[i] = y < s + 3 ? 'soil' : 'rock'; r.B[i] = y < s + 3 ? 'soil' : 'rock'; }
  }
  // two side caves, dark, so the shell reads against open rock
  r.carve(1, 24, 5, 3, 'rock'); r.carve(2, 27, 3, 1, 'rock'); r.carve(19, 31, 5, 3, 'rock'); r.carve(18, 33, 2, 1, 'rock');
  if (extra) extra(r);
  return r;
}
function stamp(r, rows, top) {
  const at = {};
  rows.forEach((row, j) => { for (let i = 0; i < row.length; i++) { const ch = row[i], x = AX + i, y = top + j, k = r.i(x, y); if (ch === ' ') continue;
    if (ch === '#' || ch === 'r' || ch === 'l') { r.F[k] = 'alloy'; r.B[k] = 'alloy'; } else if (ch === 's') { r.F[k] = 'soil'; r.B[k] = 'soil'; } else { r.F[k] = null; r.B[k] = ch === 'g' ? 'basalt' : 'alloy'; }
    if ('clrg'.includes(ch)) (at[ch] = at[ch] || []).push([x, y]); } });
  return at;
}
function sky(p) {
  const { PW, PH } = p.w;
  for (const mx of [-0.2, 0.5, 1.2]) p.light({ x: PW * mx, y: -40, col: [0.55, 0.62, 0.85], r: 40, k: 1.05, p: 0.6, pen: 0.9 });
  for (let y = 0; y < PH; y++) for (let x = 0; x < PW; x++) {
    const i = y * PW + x; if (!p.unlit[i]) continue;
    const t = y / (ROOF * C); let c = mix(hex('#04060f'), hex('#16213a'), Math.min(1, t * t * 1.1));
    const hill = 13.2 * C - 10 * Math.sin(x * 0.021 + 1) - 6 * Math.sin(x * 0.057);
    if (y > hill) c = mix(hex('#0d1424'), hex('#111a2c'), (y - hill) / 40);
    if (y < hill - 4 && hash(x, y, 77) < 0.0035) c = mix(c, hex('#c8d4ff'), 0.35 + hash(x, y, 78) * 0.5);
    p.alb.set(c, i * 3);
  }
  // grass where soil meets the sky
  for (let y = 1; y < PH; y++) for (let x = 0; x < PW; x++) {
    const i = y * PW + x; if (!p.w.solid[i] || p.w.F[p.w.i((x / C) | 0, (y / C) | 0)] !== 'soil') continue;
    const up = (k) => y - k >= 0 && p.unlit[(y - k) * PW + x];
    if (up(1)) p.alb.set(hash(x, y, 5) < 0.5 ? hex('#4f8a63') : hex('#5f9a70'), i * 3); else if (up(2)) p.alb.set(hex('#3b6a4e'), i * 3); else if (up(3) && hash(x, y, 6) < 0.6) p.alb.set(hex('#2c5040'), i * 3);
  }
}
function cache(p, cx, floor) {
  const x = cx - 7, y = floor - 10; D.abox(p, x, y, 14, 10); D.R(p, x + 2, y + 3, 10, 1, A[1]);
  p.glow(cx, y + 6, hex('#7cf0dc'), 1); p.glow(cx - 1, y + 6, hex('#3cb8a4'), 0.7);
  p.light({ x: cx, y: y + 6, col: [0.35, 1, 0.85], r: 2.2, k: 0.7 });
}
function heroAt(p, x, floor, ang) { const v = O.hero(p, x, floor); O.helmetBeam(p, v.x, v.y, ang); return v; }

// V1: the lighthouse over the crater. The shard fell here and scorched the rock; they sank the tower round it, ran a copper
// bus up its shaft, and crowned the tip with porcelain and two horns so its overflow climbs and lights the night.
function spireV1(off) {
  const rows = SPIRE0.slice(); rows[26] = '#.......#'; rows[27] = '#c......#'; rows[28] = '###ggg###'; rows[29] = '##ggggg##';
  const top = ROOF - 10, cx = (AX + 4) * C + 8;
  const r = ground((r) => {
    // the crater: scorched glass under the chamber
    for (let y = top + 28; y < SH; y++) for (let x = 0; x < SW; x++) { const dx = x - (AX + 4), dy = (y - (top + 29)) * 1.25; if (dx * dx + dy * dy < 44 && r.F[r.i(x, y)]) { r.F[r.i(x, y)] = 'basalt'; r.B[r.i(x, y)] = 'basalt'; } }
  });
  stamp(r, rows, top);
  const p = newPic(r); sky(p);
  const floor = (top + 28) * C, bottom = (top + 30) * C;
  // fulgurites: veins of fused glass from the impact, through the rock
  const imp = [cx, bottom - 6];
  if (!off) for (let n = 0; n < 9; n++) {
    let a = Math.PI * (0.05 + 0.9 * n / 8) + (hash(n, 1, 2) - 0.5) * 0.3, x = imp[0], y = imp[1], len = 60 + hash(n, 3, 4) * 60;
    for (let s = 0; s < len; s++) { a += (hash(n, s, 9) - 0.5) * 0.5; x += Math.cos(a); y += Math.sin(a) * 0.9 + 0.15; if (!p.w.sp(x | 0, y | 0)) continue; const f = 1 - s / len;
      p.glow(x, y, f > 0.6 ? VC.mid : VC.lo, 0.25 + 0.5 * f); p.art[(y | 0) * p.w.PW + (x | 0)] = 1;
      if (hash(n, s, 11) < 0.05) { let b = a + 1.2, bx = x, by = y; for (let t = 0; t < 10; t++) { bx += Math.cos(b); by += Math.sin(b); if (p.w.sp(bx | 0, by | 0)) p.glow(bx, by, VC.lo, 0.25 * f); } } }
  }
  // the copper bus up the shaft's back wall, and the crown on the tip
  const busTop = (top + 4) * C + 2;
  for (let y = busTop; y < floor - 30; y++) { if (p.w.sp(cx, y)) continue; p.set(cx - 1, y, y % 12 === 0 ? CU[4] : CU[3]); p.set(cx, y, CU[1]); if (y % 24 === 0) { p.set(cx - 2, y, A[4]); p.set(cx + 1, y, A[1]); } }
  const crown = new Spr(25, 40), m = 12;
  for (const side of [-1, 1]) { line(m + side * 2, 38, m + side * 10, 6, (x, y) => crown.set(x, y, side < 0 ? CU[4] : CU[3], 'cu')); line(m + side * 3, 38, m + side * 11, 7, (x, y) => crown.set(x, y, CU[1], 'cu')); }
  insulator(crown, m, 30, 4, 7);
  ell(crown, m + 0.5, 22.5, 4.2, 4.2, (x, y, nx, ny, d) => off ? (d > 0.75 ? VG[1] : tone(VG, nx, ny, 2)) : [d < 0.4 ? VC.core : d < 0.75 ? VC.hi : VC.mid, d < 0.4 ? 1.3 : 1], off ? 'vg' : 'cry');
  crown.outline({ ...OLS, cry: VC.dk });
  const crY = top * C - 38; crown.blit(p, cx - m, crY);
  const d = DESIGNS.find((q) => q.id === 'shard');
  const far = []; for (let y = (top + 26) * C + 2; y < floor; y += 6) { far.push([(AX + 1) * C, y]); far.push([(AX + 8) * C, y]); } far.push([cx - 1, floor - 34], [cx - 1, floor - 50]);
  relic(p, d, 1.5, cx, bottom, { off, far, floor: bottom - 2, r: 9, k: 1.3 });
  cache(p, (AX + 1) * C + 10, floor);
  if (!off) {
    artLight(p, { x: cx, y: crY + 22, r: 10, k: 1.25, p: 1.3 }); artLight(p, { x: cx, y: crY + 22, r: 2.6, k: 1 });
    for (let y = busTop + 20; y < floor - 30; y += 48) artLight(p, { x: cx, y, r: 2.6, k: 0.45 });
    p.emitters.push({ kind: 'climb', pts: [[cx - 0.5, floor - 30], [cx - 0.5, busTop]], every: [2.2, 4.5], flare: [cx + 0.5, crY + 22.5] });
    p.emitters.push({ kind: 'ladder', a: [cx - 2.5, crY + 36], b: [cx + 2.5, crY + 36], a1: [cx - 10.5, crY + 7], b1: [cx + 10.5, crY + 7], period: 1.4 });
    p.emitters.push({ kind: 'orb', x: cx + 0.5, y: crY + 22.5, r: 5, far: [[cx - 10, crY + 7], [cx + 10, crY + 7], [cx - 6, crY + 18], [cx + 6, crY + 18]] });
  }
  heroAt(p, 3 * C, surf(3) * C, 2);
  return p;
}

// V2: the light well. The core lies in a tall chamber at the bottom; they opened the tower into one straight flue to a lens at
// its tip, so its light climbs the shaft and stands over the hills as a pillar.
function spireV2(off) {
  const rows = SPIRE0.slice();
  rows[0] = '   #l#   '; rows[1] = '   #.#   '; rows[2] = '   #.#   '; rows[3] = '   #.#   ';
  rows[22] = '#....####'; rows[23] = '#.......#'; rows[24] = '#.......#'; rows[25] = '#.......#'; rows[26] = '#.......#'; rows[27] = '#c......#';
  const top = ROOF - 10, cx = (AX + 4) * C + 8, r = ground(); stamp(r, rows, top);
  const p = newPic(r); sky(p);
  const floor = (top + 28) * C;
  // the lens in the tip, ringed
  for (let i = -5; i <= 5; i++) { p.set(cx + i, top * C + 1, A[4]); p.set(cx + i, top * C + 14, A[1]); }
  for (let j = 2; j < 14; j++) for (let i = -4; i <= 4; i++) { const d2 = (i * i) / 20 + ((j - 8) * (j - 8)) / 40; if (d2 > 1) continue; if (off) p.set(cx + i, top * C + j, i < 0 ? VG[3] : VG[2]); else { p.glow(cx + i, top * C + j, d2 < 0.3 ? VC.core : d2 < 0.7 ? VC.hi : VC.mid, d2 < 0.3 ? 1.2 : 0.9); p.art[(top * C + j) * p.w.PW + cx + i] = 1; } }
  // a ring of mirrors up the flue: small alloy brackets with a glint each
  for (let y = (top + 5) * C; y < floor - 40; y += 40) { p.set(cx - 7, y, A[4]); p.set(cx - 6, y, A[3]); p.set(cx + 6, y, A[2]); p.set(cx + 7, y, A[1]); }
  const d = DESIGNS.find((q) => q.id === 'core');
  const far = []; for (let y = (top + 23) * C + 2; y < floor; y += 6) { far.push([(AX + 1) * C, y]); far.push([(AX + 8) * C, y]); } for (let x = (AX + 1) * C; x < (AX + 8) * C; x += 8) far.push([x, (top + 23) * C]);
  const rl = relic(p, d, 1.75, cx, floor, { off, far, floor, r: 8, k: 1.3 });
  cache(p, (AX + 1) * C + 10, floor);
  if (!off) {
    // the column of light: lights up the flue, so the ledges catch it
    for (let y = rl.core[1] - 30; y > top * C; y -= 26) artLight(p, { x: cx, y, r: 3.2, k: 0.62, p: 1.2 });
    artLight(p, { x: cx, y: top * C + 6, r: 7, k: 1.1, p: 1.3 });
    p.emitters.push({ kind: 'beam', x: cx, y0: rl.core[1] - 12, y1: top * C + 2, w: 11, sky: { y1: 0, w: 46 } });
    p.emitters.push({ kind: 'climb', pts: [[cx, rl.core[1] - 30], [cx, top * C + 8]], every: [1.6, 3.2], flare: [cx, top * C + 8], soft: true });
  }
  heroAt(p, 3 * C, surf(3) * C, 2);
  return p;
}

// V3: the pierced tower. A crystal spear fell point-up and stuck; they built the tower round it as a sheath. Its tip stands
// over the alloy as the spire's lamp; its foot, in the chamber, they chipped and cradled: that heart is what you take.
function spireV3(off) {
  const rows = SPIRE0.slice();
  rows[0] = '         '; rows[1] = '   #.#   '; rows[2] = '   #.#   '; rows[3] = '   #.#   ';
  rows[22] = '#....####'; rows[23] = '#.......#'; rows[24] = '#.......#'; rows[25] = '#.......#'; rows[26] = '#.......#'; rows[27] = '#c......#';
  const top = ROOF - 10, cx = (AX + 4) * C + 8, r = ground(); stamp(r, rows, top);
  const p = newPic(r); sky(p);
  const floor = (top + 28) * C, base = floor + 3 * C, tip = top * C - 3 * C;
  // the spear: drawn into what is behind (back walls, sky), so it stands in the shaft and pokes out of the tip
  for (let y = tip; y < base; y++) {
    const t = (y - tip) / (base - tip), hw = 1.5 + t * 10;
    for (let x = Math.floor(cx - hw); x <= Math.ceil(cx + hw); x++) {
      const i = y * p.w.PW + x; if (p.w.solid[i]) continue; const X = x + 0.5 - cx, u = X / hw; if (Math.abs(u) > 1) continue;
      if (off) { p.set(x, y, Math.abs(u) > 0.85 ? VG[1] : u < -0.2 ? VG[3] : u < 0.35 ? VG[2] : VG[1]); p.unlit[i] = 0; continue; }
      const edge = Math.abs(u) > 0.82, ridge = Math.abs(u + 0.15) < 0.12;
      const col = edge ? VG[1] : u < -0.45 ? VG[5] : u < -0.15 ? VG[4] : u < 0.4 ? VG[3] : VG[2];
      const facet = (y + Math.round(X * 1.7)) % 29 === 0 || (y + Math.round(-X * 2.3)) % 41 === 0; p.unlit[i] = 0; if (ridge || facet) { p.glow(x, y, facet ? VC.hi : VC.mid, facet ? 0.75 : 0.5 + 0.3 * (1 - t)); p.art[i] = 1; } else p.set(x, y, col);
    }
  }
  // copper rings clamping the spear where each ledge meets it
  for (const ry of [top + 4, top + 11, top + 16, top + 22]) { const y = ry * C + 3, t = (y - tip) / (base - tip), hw = Math.round(2.5 + t * 10);
    for (let x = cx - hw - 1; x <= cx + hw + 1; x++) { if (p.w.sp(x, y)) continue; p.set(x, y, CU[4]); p.set(x, y + 1, CU[3]); p.set(x, y + 2, CU[1]); } }
  const d = DESIGNS.find((q) => q.id === 'heart');
  const far = []; for (let y = (top + 23) * C + 2; y < floor; y += 6) { far.push([(AX + 1) * C, y]); far.push([(AX + 8) * C, y]); }
  relic(p, d, 1.25, cx - 30, floor, { off, far, floor, r: 6, k: 1.1 });
  cache(p, (AX + 1) * C + 6, floor);
  if (!off) {
    for (let y = tip + 10; y < floor; y += 34) artLight(p, { x: cx, y, r: 3.4 + 2 * (y - tip) / (base - tip), k: 0.55, p: 1.3 });
    artLight(p, { x: cx, y: tip + 8, r: 8, k: 1.1, p: 1.3 });
    p.emitters.push({ kind: 'climb', pts: [[cx - 4, floor], [cx, tip + 2]], every: [1.8, 3.6], flare: [cx, tip + 1], spear: true });
    p.emitters.push({ kind: 'orb', x: cx, y: tip + 2, r: 3, far: [[cx - 7, top * C + 2], [cx + 7, top * C + 2], [cx - 4, tip - 10], [cx + 5, tip - 9]] });
  }
  heroAt(p, 3 * C, surf(3) * C, 2);
  return p;
}

// ---------------- the homes: the hearth room of the earlier page, with a harnessed chip powering part of it ----------------
const FLOOR = 10 * C, CEIL = 5 * C, HH = 13, CROP = 2 * C;
function shell(W, x0, x1) {
  const r = new Room(W, HH);
  r.carve(x0, 5, x1 - x0 + 1, 5, (x, y) => (y === 9 ? 'alloy' : 'plaster'));
  r.fill(x0 - 1, 4, x1 - x0 + 3, 1, 'alloy'); r.fill(x0 - 1, 10, x1 - x0 + 3, 1, 'alloy');
  r.fill(x0 - 1, 5, 1, 5, 'alloy'); r.fill(x1 + 1, 5, 1, 5, 'alloy');
  r.carve(0, 7, x0, 3, 'rock'); r.carve(x0 - 1, 7, 1, 3, 'alloy');
  return r;
}
// a chip of the shard in a wall niche, its cables to the hearth and along the ceiling to the lamp over the table
function niche(p, x, y, cableTo) {
  for (let j = 0; j < 26; j++) for (let i = 0; i < 20; i++) { const e = i === 0 || j === 0 || i === 19 || j === 25; p.set(x + i, y + j, e ? A[0] : j === 1 ? hex('#0a0b10') : i < 3 ? hex('#15161d') : hex('#0e0f15')); }
  D.abox(p, x - 2, y + 24, 24, 4); D.abox(p, x - 2, y - 3, 24, 3);
  const s = DESIGNS[0].piece(0.62).spr; s.blit(p, x + 10 - Math.round(s.w / 2), y + 24 - s.h);
  const core = [x + 10, y + 24 - s.h * 0.55];
  artLight(p, { x: core[0], y: core[1], r: 6, k: 1.1, p: 1.4 }); artLight(p, { x: core[0], y: core[1], r: 1.8, k: 0.8 });
  const pts = []; const cab = (x0, y0, x1, y1) => line(x0, y0, x1, y1, (X, Y) => { p.set(X, Y, CU[3]); p.set(X, Y + 1, CU[0]); pts.push([X, Y]); });
  cab(x + 20, y + 12, cableTo.hearth[0], y + 12); cab(x + 10, y - 3, x + 10, CEIL + 1); cab(x + 10, CEIL + 1, cableTo.lamp[0], CEIL + 1);
  const src = [[core[0], core[1] - 6], [core[0] - 3, core[1]], [core[0] + 3, core[1] + 2]], near = [[x + 1, y + 4], [x + 18, y + 6], [x + 4, y + 22], [x + 16, y + 22], [x + 10, y - 1]];
  p.emitters.push({ kind: 'relic', core, src, near, far: [[x + 20, y + 12], [x + 10, y - 3]], hero: null, floor: y + 24, r: 70, S: 0.62, rate: 0.5, big: [3, 6.5], small: true });
  p.emitters.push({ kind: 'crawl', pts: pts.filter((_, i) => i % 3 === 0), rate: 0.35 });
}
// a chip of the heart in a copper cage hung from the ceiling over the table
function chandelier(p, cx) {
  for (let y = CEIL; y < CEIL + 12; y++) p.set(cx, y, y % 3 ? A[3] : A[1]);
  const s = new Spr(19, 22), m = 9;
  for (let a = 0; a < Math.PI * 2; a += 0.02) { const x = m + Math.cos(a) * 8, y = 11 + Math.sin(a) * 9; s.set(x, y, Math.cos(a) < -0.2 ? CU[4] : CU[2], 'cu'); }
  for (const bx of [-4, 0, 4]) for (let y = 3; y <= 19; y++) { const w = Math.sqrt(Math.max(0, 1 - ((y - 11) / 9) ** 2)) * 8; if (Math.abs(bx) <= w) s.set(m + bx * w / 8, y, CU[3], 'cu'); }
  for (let i = -3; i <= 3; i++) s.set(m + i, 1, CU[4], 'cu'); for (let i = -2; i <= 2; i++) s.set(m + i, 21, CU[2], 'cu');
  const cr = [[m, 6], [m + 4, 11], [m, 16], [m - 4, 11]];
  poly(s, cr.map(([x, y]) => [x + 0.5, y + 0.5]), (x, y) => [Math.abs(x - m) + Math.abs(y - 11) < 2 ? VC.core : y < 11 ? VC.hi : VC.mid, Math.abs(x - m) + Math.abs(y - 11) < 2 ? 1.3 : 0.95], 'cry');
  s.outline({ ...OLS, cry: VC.dk }, ['cu']);
  const ox = cx - m, oy = CEIL + 12; s.blit(p, ox, oy);
  const core = [cx + 0.5, oy + 11.5];
  artLight(p, { x: core[0], y: core[1], r: 7.5, k: 1.15, p: 1.35 }); artLight(p, { x: core[0], y: core[1], r: 2, k: 0.9 });
  p.emitters.push({ kind: 'relic', core, src: [[cx, oy + 6], [cx + 4, oy + 11], [cx, oy + 16], [cx - 4, oy + 11]], near: [[ox + 1, oy + 11], [ox + 17, oy + 11], [cx, oy + 2], [cx, oy + 20], [cx - 4, oy + 4], [cx + 4, oy + 18], [ox + 5, oy + 18], [ox + 13, oy + 4]], far: [[cx, CEIL + 2], [cx, CEIL + 8]], hero: null, floor: FLOOR, r: 90, S: 0.6, rate: 0.6, big: [3.5, 7], small: true });
}
function home(kind) {
  const r = shell(36, 4, 33); r.fill(19, 5, 1, 1, 'alloy');
  const p = newPic(r), lit = (keep) => ({ lit: !!keep });
  put(p, 'cobweb', 65, CEIL); put(p, 'heightMarks', 66, 120); put(p, 'coatPegs', 76, 86); put(p, 'basket', 74, FLOOR);
  put(p, 'lightWindow', 100, 86, lit()); put(p, 'bench', 98, FLOOR); put(p, 'rug', 138, FLOOR); put(p, 'chair', 140, FLOOR);
  put(p, 'table', 154, FLOOR); put(p, 'dishes', 156, FLOOR - 17); put(p, 'chair', 188, FLOOR, { flip: true });
  if (kind === 'lamp') chandelier(p, 170); else put(p, 'ceilLamp', 163, CEIL, lit(true));
  put(p, 'frame', 206, 90); put(p, 'stool', 210, FLOOR); put(p, 'holoPortrait', 234, 88, lit()); put(p, 'plantAlive', 230, FLOOR);
  put(p, 'banner', 262, 84); put(p, 'clock', 282, 88); put(p, 'pots', 276, FLOOR);
  put(p, 'herbs', 338, CEIL);
  put(p, 'hearth', 358, FLOOR, { lit: kind === 'niche', flue: FLOOR - 50 - CEIL });
  put(p, 'utensils', 400, 88); put(p, 'shelfJars', 434, 100, lit()); put(p, 'stool', 404, FLOOR); put(p, 'kettle', 403, FLOOR - 11, lit());
  put(p, 'sconce', 466, 92, lit(kind === 'niche')); put(p, 'basin', 472, FLOOR); put(p, 'wallShelf', 500, 90); put(p, 'pots', 516, FLOOR);
  if (kind === 'niche') niche(p, 312, 112, { hearth: [362], lamp: [170] });
  const v = O.hero(p, kind === 'niche' ? 270 : 110, FLOOR); O.helmetBeam(p, v.x, v.y, kind === 'niche' ? 8 : -4);
  for (const l of p.lights) if (l.col === D.ARC.light) { l.k *= 1.3; l.r *= 1.2; }
  return p;
}

// ---------------- run ----------------
const want = (id) => !only || id.startsWith(only);
for (const d of DESIGNS) {
  if (want('icon')) { const s = icon(d); P.encode(s.png(P), path.join(OUT, `icon-${d.id}.png`)); }
  if (want('card')) { card(d); console.log('card', d.id); }
}
for (const [id, fn] of [['spire-v1', spireV1], ['spire-v2', spireV2], ['spire-v3', spireV3]]) if (want(id) || want('spire')) {
  bake(fn(false), id, { amb: 0.05 }); bake(fn(true), id, { amb: 0.05, off: true }); console.log(id);
}
for (const k of ['niche', 'lamp']) if (want('home')) { bake(home(k), 'home-' + k, { amb: 0.03, crop: k === 'niche' ? [8 * C, 3 * C, 28 * C, 8 * C] : [2 * C, 3 * C, 20 * C, 8 * C] }); console.log('home', k); }
fs.writeFileSync(path.join(OUT, 'meta.json'), JSON.stringify(manifest));

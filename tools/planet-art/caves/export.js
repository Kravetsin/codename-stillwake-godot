// node tools/planet-art/caves/export.js [artDir, default game/art]: the cave concept's decor (cave-content/objects.js, direction C with A's moss and threads) cut into
// the game's pictures, `{kind}_{variant}.png` and what of it glows in `{kind}_{variant}_glow.png`, under <artDir>/caves.
// Floor kinds: the picture's bottom row is the first row of the floor under the cell, its middle the cell's middle.
// Ceiling kinds: its top row is the last row of the ceiling over the cell. Rock kinds: 24 × 24, the cell in its middle.
const path = require('path'), fs = require('fs');
const E = require('./engine'), O = require('./objects');
const { C, P, hex, mix, mul, hash, vnoise, MP } = E;
const { PAL } = O;
const OUT = path.join(process.argv[2] || path.join(__dirname, '..', '..', '..', 'game', 'art'), 'caves');
fs.mkdirSync(OUT, { recursive: true });
const clamp = (v, a = 0, b = 1) => Math.max(a, Math.min(b, v));

function gblur(src, w, h, rad) {
  const tmp = new Float32Array(src.length), dst = new Float32Array(src.length), ws = []; let sum = 0;
  for (let k = -rad * 3; k <= rad * 3; k++) { const v = Math.exp(-(k * k) / (2 * rad * rad)); ws.push(v); sum += v; }
  const R = rad * 3; for (let k = 0; k < ws.length; k++) ws[k] /= sum;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) { let a = 0, b = 0, c = 0; for (let k = -R; k <= R; k++) { const xx = clamp(x + k, 0, w - 1), j = (y * w + xx) * 3, q = ws[k + R]; a += src[j] * q; b += src[j + 1] * q; c += src[j + 2] * q; } const o = (y * w + x) * 3; tmp[o] = a; tmp[o + 1] = b; tmp[o + 2] = c; }
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) { let a = 0, b = 0, c = 0; for (let k = -R; k <= R; k++) { const yy = clamp(y + k, 0, h - 1), j = (yy * w + x) * 3, q = ws[k + R]; a += tmp[j] * q; b += tmp[j + 1] * q; c += tmp[j + 2] * q; } const o = (y * w + x) * 3; dst[o] = a; dst[o + 1] = b; dst[o + 2] = c; }
  return dst;
}
// The glow: the emissive pixels and a soft halo round each, as the study's bloom, tighter (the light does the wide part).
function bloomed(Em, w, h) {
  const a = gblur(Em, w, h, 1), b = gblur(Em, w, h, 3), o = new Float32Array(Em.length);
  for (let i = 0; i < Em.length; i++) o[i] = Em[i] * 0.9 + a[i] * 0.9 + b[i] * 1.1;
  return o;
}

function save(w, h, at, name) { // at(x, y) -> [r, g, b, a] in 0..1
  const o = P.create(w, h);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) { const c = at(x, y), i = (y * w + x) * 4; for (let q = 0; q < 4; q++) o.data[i + q] = Math.round(clamp(c[q]) * 255); }
  P.encode(o, path.join(OUT, name + '.png'));
}

// ---------- a stage for the concept's drawing functions: a few cells, floor or ceiling solid ----------
const SW = 10, SH = 8, AX = 4; // the anchor cell's column
function stage(solid) {
  const w = new E.World(SW, SH);
  for (let y = 0; y < SH; y++) for (let x = 0; x < SW; x++) if (!solid(x, y)) w.open(x, y);
  w.shapes();
  const pic = new E.Pic(w), drawn = new Uint8Array(w.PW * w.PH);
  const set0 = pic.set.bind(pic), glow0 = pic.glow.bind(pic);
  const mark = (x, y) => { x |= 0; y |= 0; if (pic.inb(x, y)) drawn[y * w.PW + x] = 1; };
  pic.set = (x, y, c) => { set0(x, y, c); mark(x, y); };
  pic.glow = (x, y, c, k = 1) => { glow0(x, y, c, k); mark(x, y); };
  return { w, pic, drawn };
}

// Cuts what was drawn round the anchor: `ax` the cell's middle (between two pixels), rows `top`..`bottom` given or found.
function cut(st, name, ax, from, to) {
  const { w, pic, drawn } = st, PW = w.PW, PH = w.PH, n = PW * PH;
  const Em = new Float32Array(n * 3); for (let i = 0; i < n; i++) if (pic.isE[i]) for (let q = 0; q < 3; q++) Em[i * 3 + q] = pic.emis[i * 3 + q];
  const B = bloomed(Em, PW, PH);
  let x0 = 1e9, x1 = -1, y0 = 1e9, y1 = -1;
  for (let y = 0; y < PH; y++) for (let x = 0; x < PW; x++) {
    const i = y * PW + x, on = drawn[i] || Math.max(B[i * 3], B[i * 3 + 1], B[i * 3 + 2]) > 0.03;
    if (on) { x0 = Math.min(x0, x); x1 = Math.max(x1, x); y0 = Math.min(y0, y); y1 = Math.max(y1, y); }
  }
  const top = from ?? y0, bottom = to ?? y1, half = Math.max(ax - x0, x1 + 1 - ax), W = 2 * half, H = bottom - top + 1, left = ax - half;
  let glowing = false;
  const albedo = (x, y) => { const sx = left + x, sy = top + y; if (sx < 0 || sy < 0 || sx >= PW || sy >= PH) return [0, 0, 0, 0]; const i = sy * PW + sx; return drawn[i] ? [pic.alb[i * 3], pic.alb[i * 3 + 1], pic.alb[i * 3 + 2], 1] : [0, 0, 0, 0]; };
  const glow = (x, y) => {
    const sx = left + x, sy = top + y; if (sx < 0 || sy < 0 || sx >= PW || sy >= PH) return [0, 0, 0, 0];
    const i = sy * PW + sx, e = [B[i * 3], B[i * 3 + 1], B[i * 3 + 2]], m = Math.max(...e);
    if (m <= 0.01) return [0, 0, 0, 0]; glowing = true; return [e[0] / m, e[1] / m, e[2] / m, clamp(m)];
  };
  save(W, H, albedo, name);
  // The glow is written only when something glows.
  const o = P.create(W, H);
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) { const c = glow(x, y), i = (y * W + x) * 4; for (let q = 0; q < 4; q++) o.data[i + q] = Math.round(clamp(c[q]) * 255); }
  if (glowing) P.encode(o, path.join(OUT, name + '_glow.png'));
  return { W, H, glowing };
}

const FLOOR_ROW = 6, CEIL_ROW = 1; // the floor's first row of cells; the ceiling's last
const floorStage = () => stage((x, y) => y >= FLOOR_ROW);
const ceilStage = () => stage((x, y) => y <= CEIL_ROW);
const AXP = AX * C + C / 2, SURF = FLOOR_ROW * C, CEIL = (CEIL_ROW + 1) * C; // anchor x; the floor's top pixel row; the first open row under the ceiling
const onFloor = (name, draw) => { const st = floorStage(); draw(st.pic); return cut(st, name, AXP, null, SURF); };
const onCeiling = (name, draw) => { const st = ceilStage(); draw(st.pic); return cut(st, name, AXP, CEIL - 1, null); };
const sizes = {};
const note = (name, r) => { sizes[name] = [r.W, r.H, r.glowing]; };

// ---------- floor ----------
const capSets = [[3, 1.0], [4, 1.15], [5, 1.3]];
capSets.forEach(([n, sc], v) => note(`glowcapTeal_${v}`, onFloor(`glowcapTeal_${v}`, (pic) => O.glowcaps(pic, AXP, SURF, PAL.teal, 11 + v * 7, n, { scale: sc }))));
capSets.forEach(([n, sc], v) => note(`glowcapViolet_${v}`, onFloor(`glowcapViolet_${v}`, (pic) => O.glowcaps(pic, AXP, SURF, PAL.violet, 41 + v * 5, n, { scale: sc }))));
const crystalSets = [[3, 0.75, -0.15], [4, 0.95, 0.1], [5, 1.15, -0.05]];
for (const [kind, pal] of [['crystalIce', PAL.blue], ['crystalAmber', PAL.amber], ['crystalRose', PAL.rose], ['crystalEmber', PAL.ember]])
  crystalSets.forEach(([n, size, ang], v) => note(`${kind}_${v}`, onFloor(`${kind}_${v}`, (pic) => O.crystals(pic, AXP, SURF, ang, pal, 21 + v * 3 + kind.length, n, size))));
// Rock-coloured spikes in each band's stone: cold, the garden's, the deep's, the basalt over the bedrock.
const BAND_ROCK = ['cold', 'rock', 'deep', 'basalt'];
const spikeSets = [[8, 2.2], [12, 2.6], [17, 3.2]];
BAND_ROCK.forEach((mat, band) => spikeSets.forEach(([len, wd], v) => {
  note(`stalagmite_${band}_${v}`, onFloor(`stalagmite_${band}_${v}`, (pic) => O.spike(pic, AXP, SURF - 1, len - 2, wd, mat, false)));
  note(`stalactite_${band}_${v}`, onCeiling(`stalactite_${band}_${v}`, (pic) => O.spike(pic, AXP, CEIL, len, wd, mat, true)));
}));

// ---------- ceiling ----------
for (const [kind, pal] of [['crystalIceHang', PAL.blue], ['crystalAmberHang', PAL.amber]])
  crystalSets.forEach(([n, size, ang], v) => note(`${kind}_${v}`, onCeiling(`${kind}_${v}`, (pic) => O.crystals(pic, AXP, CEIL, Math.PI + ang, pal, 61 + v * 3 + kind.length, n, size * 0.9))));
[[2, 1], [3, 1], [3, 1.15]].forEach(([n, sc], v) => note(`hangcap_${v}`, onCeiling(`hangcap_${v}`, (pic) => O.glowcaps(pic, AXP, CEIL, PAL.violet, 71 + v * 3, n, { hang: true, scale: sc }))));
for (const [kind, pal] of [['threadViolet', PAL.violet], ['threadTeal', PAL.teal]])
  [3, 4, 5].forEach((n, v) => note(`${kind}_${v}`, onCeiling(`${kind}_${v}`, (pic) => {
    // The strands centred on the cell, each as long as the concept's but within the two cells they hang in.
    for (let i = 0; i < n; i++) {
      const x = AXP - Math.round((n - 1) * 1.5) + Math.round(i * 3 + hash(i, v + kind.length, 1) * 2);
      O.thread(pic, x, CEIL, 7 + Math.floor(hash(i, 81 + v + kind.length, 3) * 20), pal, 81 + v * 9 + i + kind.length);
    }
  })));
[18, 24, 30].forEach((len, v) => note(`roots_${v}`, onCeiling(`roots_${v}`, (pic) => {
  O.roots(pic, AXP - 3, CEIL, len, 91 + v, PAL.green);
  O.roots(pic, AXP + 3, CEIL, Math.round(len * 0.6), 95 + v, PAL.green);
})));

// ---------- in the rock: 24 × 24, the cell at 4..19 ----------
const R = 24, M = 4;
function rock(name, draw) {
  const alb = new Float32Array(R * R * 3), a = new Float32Array(R * R), Em = new Float32Array(R * R * 3);
  const set = (x, y, c, al = 1) => { x |= 0; y |= 0; if (x < 0 || y < 0 || x >= R || y >= R) return; const i = y * R + x; alb.set(c, i * 3); a[i] = Math.max(a[i], al); };
  const glow = (x, y, c, k = 1) => { set(x, y, c); x |= 0; y |= 0; if (x < 0 || y < 0 || x >= R || y >= R) return; const i = y * R + x; for (let q = 0; q < 3; q++) Em[i * 3 + q] += c[q] * k; };
  draw({ set, glow });
  const B = bloomed(Em, R, R);
  let glowing = false;
  save(R, R, (x, y) => { const i = y * R + x; return [alb[i * 3], alb[i * 3 + 1], alb[i * 3 + 2], a[i]]; }, name);
  const o = P.create(R, R);
  for (let i = 0; i < R * R; i++) { const e = [B[i * 3], B[i * 3 + 1], B[i * 3 + 2]], m = Math.max(...e); if (m <= 0.01) continue; glowing = true; for (let q = 0; q < 3; q++) o.data[i * 4 + q] = Math.round(clamp(e[q] / m) * 255); o.data[i * 4 + 3] = Math.round(clamp(m) * 255); }
  if (glowing) P.encode(o, path.join(OUT, name + '_glow.png'));
  note(name, { W: R, H: R, glowing });
}

// Glow moss on one face of the cell: its top (a floor, tufts rising), its bottom (a ceiling, strands hanging), its left side.
const MOSS = { teal: PAL.teal, green: PAL.green };
for (const [tone, pal] of Object.entries(MOSS)) for (let v = 0; v < 4; v++) {
  const s = 7 + v * 13 + tone.length * 101;
  rock(`moss${tone === 'teal' ? '' : 'Green'}_top_${v}`, ({ glow }) => {
    for (let x = M; x < M + C; x++) {
      if (vnoise(x, v * 9, 5, s) < 0.32) continue;
      const y = M; glow(x, y, pal.lo, 0.55);
      const h = hash(x, s, 2); if (h < 0.55) glow(x, y - 1, h < 0.2 ? pal.mid : pal.lo, h < 0.2 ? 0.9 : 0.5);
      if (h < 0.14) glow(x, y - 2, pal.hi, 1);
      if (hash(x, s, 3) < 0.45) glow(x, y + 1, mul(pal.lo, 0.7), 0.35);
    }
  });
  rock(`moss${tone === 'teal' ? '' : 'Green'}_bottom_${v}`, ({ glow }) => {
    for (let x = M; x < M + C; x++) {
      if (vnoise(x, v * 9, 6, s + 1) < 0.4) continue;
      const h = hash(x, s, 4), len = h < 0.15 ? 4 : h < 0.45 ? 2 : 1, y = M + C - 1;
      glow(x, y, mul(pal.lo, 0.8), 0.4);
      for (let k = 1; k <= len; k++) glow(x, y + k, k === len && len > 1 ? pal.hi : pal.lo, k === len && len > 1 ? 1 : 0.5);
    }
  });
  // The left face, and the right as its mirror.
  for (const [face, x, out] of [['left', M, -1], ['right', M + C - 1, 1]])
    rock(`moss${tone === 'teal' ? '' : 'Green'}_${face}_${v}`, ({ glow }) => {
      for (let y = M; y < M + C; y++) {
        if (vnoise(v * 9, y, 5, s + 2) < 0.4) continue;
        const h = hash(y, s, 5);
        glow(x, y, h > 0.85 ? pal.mid : pal.lo, h > 0.85 ? 0.9 : 0.5);
        if (h < 0.3) glow(x + out, y, pal.lo, 0.45);
        if (h > 0.93) glow(x + 2 * out, y, pal.hi, 0.9);
      }
    });
}

// Veins of ice and copper through the cell, kept off its outlined edges.
for (const [kind, o] of [['veinIce', E.ORE.ice], ['veinCopper', E.ORE.copper]]) for (let v = 0; v < 3; v++)
  rock(`${kind}_${v}`, ({ set }) => {
    for (let y = M + 2; y < M + C - 2; y++) for (let x = M + 2; x < M + C - 2; x++) {
      const n = vnoise(x + v * 40, y + kind.length * 17, 3, 7 + v * 3);
      if (n > 0.66) set(x, y, n > 0.82 ? o.vl : o.v); else if (n > 0.61) set(x, y, o.vd);
    }
  });

// Fossils sunk in the stone: an ammonite's spiral, and a few ribs of something big.
const BONE = hex('#c8bc9c'), BONE_D = hex('#8a8068'), BONE_L = hex('#e4dac0'), SUNK = 0.62;
rock('fossil_0', ({ set }) => {
  const cx = M + 7.5, cy = M + 7.5, r = 5.6;
  for (let y = -6; y <= 6; y++) for (let x = -6; x <= 6; x++) {
    const d = Math.sqrt(x * x + y * y); if (d > r + 0.3) continue;
    let a = Math.atan2(y, x); if (a < 0) a += Math.PI * 2;
    const turn = (d / r) * 3 - a / (Math.PI * 2), f = turn - Math.floor(turn);
    let col = f < 0.22 ? BONE_D : f > 0.75 ? BONE_L : BONE; if (d > r - 0.8) col = BONE_D;
    set(Math.round(cx + x), Math.round(cy + y), mul(col, SUNK));
  }
});
rock('fossil_1', ({ set }) => {
  for (let k = 0; k < 14; k++) { const x = M + 1 + k, y = M + 4 + Math.round(Math.sin(k / 14 * Math.PI) * -2); set(x, y, mul(k % 4 === 0 ? BONE_D : BONE_L, SUNK)); set(x, y + 1, mul(BONE, SUNK));
    if (k % 4 === 1) for (let j = 0; j < 7; j++) { const rx = x + Math.round(j * 0.3 - (j * j) / 30), ry = y + 2 + j; set(rx, ry, mul(j % 5 === 0 ? BONE_D : BONE, SUNK)); } }
});
rock('fossil_2', ({ set }) => {
  // a small skull and jaw
  for (let y = -3; y <= 3; y++) for (let x = 0; x < 10; x++) if ((x - 4.5) * (x - 4.5) / 22 + y * y / 9 < 1) set(M + 3 + x, M + 7 + y, mul((x === 6 && y === -1) ? hex('#2a2620') : y === 3 ? BONE_D : BONE, SUNK));
  for (let x = 0; x < 8; x++) set(M + 4 + x, M + 11 + (x > 5 ? 1 : 0), mul(BONE_D, SUNK));
});

// A geode cracked open: a rough shell, a dark hollow, teeth glowing toward their tips (the concept's, small).
for (let v = 0; v < 2; v++) rock(`geode_${v}`, ({ set, glow }) => {
  const pal = v === 0 ? PAL.violet : PAL.amber, cx = M + 7.5, cy = M + 7.5, r = 7.2, seed = 5 + v;
  const teeth = []; for (let k = 0; k < 9; k++) teeth.push({ a: (k / 9) * Math.PI * 2 + hash(k, seed, 1) * 0.4, len: 0.4 + hash(k, seed, 2) * 0.35, w: 0.22 + hash(k, seed, 3) * 0.1 });
  for (let y = -8; y <= 8; y++) for (let x = -8; x <= 8; x++) {
    const d = Math.sqrt(x * x + y * y), an = Math.atan2(y, x), Rr = r * (0.9 + 0.1 * vnoise(an * 6 + 10, 0, 1, seed));
    if (d > Rr) continue; const X = Math.round(cx + x), Y = Math.round(cy + y);
    if (d > Rr - 1.2) { set(X, Y, hex('#262430')); continue; }
    if (d > Rr - 2.4) { set(X, Y, mix(hex('#7d7990'), hex('#a29eb6'), hash(X, Y, seed))); continue; }
    const inner = Rr - 2.4; let tooth = null;
    for (const t of teeth) { let da = Math.abs(an - t.a); if (da > Math.PI) da = Math.PI * 2 - da; const reach = inner * (1 - t.len), hw = t.w * (d - reach) / (inner - reach + 0.01); if (d > reach && da < hw) { tooth = { f: (inner - d) / (inner - reach), side: an - t.a }; break; } }
    if (!tooth) { if (d > inner - 1.2) glow(X, Y, mix(pal.lo, pal.dk, 0.4), 0.45); else set(X, Y, mix(pal.dk, hex('#100c18'), 0.6)); continue; }
    const f = tooth.f; glow(X, Y, f > 0.7 ? pal.hi : f > 0.35 ? pal.mid : pal.lo, f > 0.7 ? 1 : f > 0.35 ? 0.8 : 0.5);
  }
});

// The heat's cracks: a seam through the cell, in at the left at the cell's middle row and out at the right at it, so a
// run of them reads as one; a branch now and then.
for (let v = 0; v < 4; v++) rock(`heatCrack_${v}`, ({ glow }) => {
  const pal = PAL.ember, s = 71 + v * 11;
  let y = M + 8;
  for (let x = M; x < M + C; x++) {
    const left = M + C - 1 - x; // steps left to come back to the middle row
    const h = hash(x, s, 1); if (Math.abs(y - (M + 8)) < left) { if (h < 0.28) y++; else if (h < 0.56) y--; } else y += Math.sign(M + 8 - y);
    y = Math.max(M + 3, Math.min(M + 12, y));
    const core = x > M && x < M + C - 1;
    glow(x, y, core ? pal.mid : pal.lo, core ? 0.95 : 0.7);
    if (hash(x, s, 2) < 0.3) glow(x, y, pal.hi, 1.1);
    if (core && hash(x, s, 4) < 0.5) glow(x, y + 1, pal.lo, 0.5);
    if (v % 2 === 1 && x === M + 6) { let by = y; for (let k = 1; k < 5; k++) { by += hash(k, s, 6) < 0.5 ? -1 : 1; glow(x + k, by + (v > 1 ? k : -k) * 0, pal.lo, 0.55); } }
  }
});

fs.writeFileSync(path.join(process.argv[2] ? path.join(OUT, '..') : __dirname, 'caves-sizes.json'), JSON.stringify(sizes, null, 1));
console.log(Object.keys(sizes).length, 'pictures');

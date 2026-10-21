// node tools/planet-art/surface/export.js [artDir, default game/art]: the "Stillwake" surface style (s9) cut into the game's pictures: ground tiles that tile, grass
// tops by cell shape, plants with their glow, and the backdrop's layers, each tileable across.
const path = require('path'), fs = require('fs');
const K = require('./kit');
const { P, hex, mix, mul, clamp, hash, smooth, ramp, rampSmooth, Layer } = K;
const OUT = process.argv[2] || path.join(__dirname, '..', '..', '..', 'game', 'art');
fs.mkdirSync(path.join(OUT, 'planet'), { recursive: true });

// ---------- images ----------
const img = (w, h) => ({ w, h, c: new Float32Array(w * h * 3), a: new Float32Array(w * h) });
const put = (im, x, y, col, a = 1) => { x |= 0; y |= 0; if (x < 0 || y < 0 || x >= im.w || y >= im.h) return; const i = y * im.w + x; col = hex2(col); if (a >= 1 || !im.a[i]) { im.c.set(col, i * 3); im.a[i] = Math.max(im.a[i], a); return; } for (let k = 0; k < 3; k++) im.c[i * 3 + k] += (col[k] - im.c[i * 3 + k]) * a; im.a[i] = a + im.a[i] * (1 - a); };
const hex2 = (c) => (typeof c === 'string' ? hex(c) : c);
function save(im, name) {
  const o = P.create(im.w, im.h);
  for (let i = 0; i < im.w * im.h; i++) { for (let q = 0; q < 3; q++) o.data[i * 4 + q] = Math.round(clamp(im.c[i * 3 + q]) * 255); o.data[i * 4 + 3] = Math.round(clamp(im.a[i]) * 255); }
  P.encode(o, path.join(OUT, 'planet', name + '.png'));
}

// ---------- noise that tiles: the lattice wraps every `period` pixels ----------
function pvnoise(x, y, cell, s, px, py) {
  const gx = Math.floor(x / cell), gy = Math.floor(y / cell), fx = x / cell - gx, fy = y / cell - gy;
  const nx = px ? px / cell : 1e9, ny = py ? py / cell : 1e9, w = (v, n) => ((v % n) + n) % n;
  const sx = fx * fx * (3 - 2 * fx), sy = fy * fy * (3 - 2 * fy);
  const a = hash(w(gx, nx), w(gy, ny), s), b = hash(w(gx + 1, nx), w(gy, ny), s), c = hash(w(gx, nx), w(gy + 1, ny), s), d = hash(w(gx + 1, nx), w(gy + 1, ny), s);
  return a + (b - a) * sx + (c - a) * sy + (a - b - c + d) * sx * sy;
}
function pfbm(x, y, cell, s, oct, px, py) { let v = 0, a = 0.5, n = 0; for (let k = 0; k < oct; k++) { v += pvnoise(x, y, cell, s + k * 17, px, py) * a; n += a; a *= 0.5; cell /= 2; } return v / n; }

// ---------- ground tiles: 8 × 4 cells, as the game cuts them ----------
const TW = 128, TH = 64;
const soil = (x, y) => {
  let c = mix('#5a4434', '#6a5040', pvnoise(x, y, 8, 1, TW, TH)); const h = hash(x, y, 2);
  if (h < 0.07) c = hex('#45342a'); else if (h > 0.955) c = hex('#7d604a');
  const bx = Math.floor(x / 8), by = Math.floor(y / 8);
  if (hash(bx, by, 3) < 0.11) { const ox = bx * 8 + 1 + Math.floor(hash(bx, by, 4) * 4), oy = by * 8 + 1 + Math.floor(hash(bx, by, 5) * 4); if (x >= ox && x < ox + 3 && y >= oy && y < oy + 2) c = (y === oy) ? hex('#8a8074') : hex('#5e574e'); }
  return c;
};
const stone = (x, y) => {
  let c = mix('#5b6170', '#676e7e', pvnoise(x, y, 8, 4, TW, TH));
  const st = ((y + 4 * pvnoise(x, y, 16, 5, TW, TH)) % 8 + 8) % 8; if (st < 0.9) c = hex('#4a4f5c');
  if (hash(x, y, 6) > 0.965) c = hex('#808898');
  return c;
};
const bedrock = (x, y) => {
  let c = mix('#33313d', '#3d3a48', pvnoise(x, y, 8, 14, TW, TH));
  const st = ((y + 6 * pvnoise(x, y, 16, 15, TW, TH)) % 8 + 8) % 8; if (st < 1.6) c = hex('#24222c');
  if (hash(x, y, 16) > 0.975) c = hex('#5a566a');
  return c;
};
for (const [id, fn, backK] of [['soil', soil, 0.5], ['stone', stone, 0.45], ['bedrock', bedrock, 0.6]]) {
  const wall = img(TW, TH), back = img(TW, TH);
  for (let y = 0; y < TH; y++) for (let x = 0; x < TW; x++) {
    const c = fn(x, y); put(wall, x, y, c);
    // A back wall: darker and flatter, its grain softened toward its own mean.
    const m = fn(x & ~3, y & ~3); put(back, x, y, mul(mix(c, m, 0.45), backK));
  }
  save(wall, 'wall_' + id); save(back, 'back_' + id);
}

// ---------- grass: moss over a soil cell open to the sky, its stems up into the cell above (16 × 32) ----------
// Shapes as the game numbers them: 0 whole, 1 ◢ (rises to the right), 2 ◣ (rises to the left).
const topOf = (shape, lx) => (shape === 1 ? 15 - lx : shape === 2 ? lx : 0);
const GRASS_VARIANTS = 4;
for (let shape = 0; shape <= 2; shape++) for (let v = 0; v < GRASS_VARIANTS; v++) {
  const g = img(16, 32), s = 40 + v * 13 + shape * 101;
  for (let lx = 0; lx < 16; lx++) {
    const sy = 16 + topOf(shape, lx);
    // Moss 4 deep at a cell's sides, so any two cells meet; between, 3 to 6 deep.
    const mid = Math.sin(Math.PI * (lx + 0.5) / 16), mossD = Math.round(4 + (hash(lx >> 2, v, s) * 2.6 - 0.8) * mid) + (hash(lx, v, s + 1) < 0.12 ? 1 : 0);
    for (let d = 0; d < mossD + 3 && sy + d < 32; d++) {
      const y = sy + d;
      if (d < mossD) put(g, lx, y, d === 0 ? '#b8e2b0' : d === 1 ? '#78b48a' : d < mossD - 1 ? '#55906a' : '#3f6f52');
      else if (d === mossD && hash(lx, v, s + 2) < 0.5) put(g, lx, y, '#3f6f52');
      else put(g, lx, y, '#000000', 0.26); // shade under the moss
    }
    // Stems: thin, darker below, a few with a pale glowing tip.
    if (hash(lx, v, s + 3) < 0.4) continue;
    const h = 1 + Math.floor(hash(lx, v, s + 4) ** 1.6 * 7), lean = hash(lx, v, s + 5) < 0.5 ? 0 : (hash(lx, v, s + 6) < 0.5 ? -1 : 1);
    for (let k = 0; k < h; k++) {
      const xx = clamp(lx + (k > h / 2 ? lean : 0), 0, 15), y = sy - 1 - k;
      if (k === h - 1 && h > 3 && hash(lx, v, s + 7) < 0.35) put(g, xx, y, '#bff8e0'); else put(g, xx, y, k < h / 2 ? '#5f9c74' : '#86c492');
    }
  }
  save(g, `grass_${shape}_${v}`);
}

// ---------- plants: drawn on the study's canvas, then cut out round their footing ----------
// Each picture's bottom row lies 2 pixels under the ground, its footing in the middle of it (column w >> 1).
const BX = 480, GY = 300; // where a plant is drawn: footing x, ground line y
function cut(layer, glowE, name, sink = 2) {
  let x0 = 1e9, x1 = -1, y0 = 1e9;
  for (let y = 0; y < K.H; y++) for (let x = 0; x < K.W; x++) {
    const i = y * K.W + x; const on = layer.a[i] > 0 || (glowE && (glowE[i * 3] + glowE[i * 3 + 1] + glowE[i * 3 + 2]) > 0.02);
    if (on && y <= GY + sink) { x0 = Math.min(x0, x); x1 = Math.max(x1, x); y0 = Math.min(y0, y); }
  }
  const half = Math.max(BX - x0, x1 - BX) + 1, w = 2 * half + 1, h = GY + sink - y0 + 1, left = BX - half;
  const out = img(w, h), glow = img(w, h);
  let glowing = false;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const sx = left + x, sy = y0 + y; if (sx < 0 || sx >= K.W) continue; const i = sy * K.W + sx;
    if (layer.a[i] > 0) put(out, x, y, [layer.c[i * 3], layer.c[i * 3 + 1], layer.c[i * 3 + 2]], layer.a[i]);
    if (glowE) { const e = [glowE[i * 3], glowE[i * 3 + 1], glowE[i * 3 + 2]], m = Math.max(...e); if (m > 0.01) { glowing = true; put(glow, x, y, e.map((q) => q / m), clamp(m)); } }
  }
  save(out, name);
  if (glowing) save(glow, name + '_glow');
  return { w, h };
}
// The glow: the emissive pixels blurred into a soft halo round each, as the study's bloom.
function bloomed(E) {
  const a = K.gblur(E, K.W, K.H, 1), b = K.gblur(E, K.W, K.H, 3), o = new Float32Array(E.length);
  for (let i = 0; i < E.length; i++) o[i] = E[i] * 0.9 + a[i] * 1.1 + b[i] * 1.6;
  return o;
}
const shift = (o) => Object.assign({}, o);

// The acacia: a dark trunk that forks, wide flat crowns, fronds hanging with seed lights.
function acacia(seed, len, w, name) {
  const T = new Layer(), E = new Float32Array(K.W * K.H * 3);
  const glow = (x, y, c, k) => { x |= 0; y |= 0; c = hex(c); T.set(x, y, c); const i = (y * K.W + x) * 3; E[i] += c[0] * k; E[i + 1] += c[1] * k; E[i + 2] += c[2] * k; };
  const sk = K.treeSkeleton({ x: BX, y: GY + 2, len, w, seed, depth: 3, fork: 2, spread: 1.15, childLen: 0.62, childW: 0.62, step: 9, wiggle: 0.16, lift: 1.2, taper: 0.9, trunkShoot: 0.12, shoot: 0.2, minW: 2, firstSplit: 5 });
  const clumps = []; let n = 0;
  for (const t of sk.tips) { const r = (13 + hash(n, 1, seed) * 5) * len / 84; clumps.push({ x: t.x, y: t.y - 2, r: r * 1.15, sq: 1.7, z: -1, core: true }); for (let i = 0; i < 3; i++) clumps.push({ x: t.x + (hash(n, i, seed + 6) - 0.5) * 26 * len / 84, y: t.y - 3 - hash(n, i, seed + 7) * 6, r: r * (0.55 + hash(n, i, seed + 8) * 0.35), sq: 1.6, z: hash(n, i, seed + 9) }); n++; }
  const fol = K.foliage(clumps, { leaf: 4, lobes: 0.4, ragged: 3, holes: 0.12 });
  const trunkPal = ['#2a2026', '#3c2f33', '#54444a', '#6e5e60'];
  K.paintTree(T, sk, fol, (x, y, u, v) => {
    const s = u * 0.8 + 0.15 + (hash(Math.round(v / 3), Math.round(u * 3), 4) - 0.5) * 0.25;
    let c = ramp(trunkPal, (s + 1) / 2); if (Math.abs(u) < 0.5 && hash(Math.round(v / 5), Math.round(u * 2), 8) < 0.12) c = hex('#2a2026');
    return c;
  }, (x, y, info) => ramp(['#1f3d3a', '#2e5a50', '#3f7a62', '#5c9c72', '#95c88a', '#d6ecaa'], (K.leafLight(info, { clump: 0.7, leafK: 0.4, gapK: 0.3, coreK: 0.3 }) + 0.55) / 1.5));
  const lights = [];
  for (const [k, c] of clumps.entries()) {
    if (c.core) continue;
    for (let j = 0; j < 7; j++) {
      const fx = Math.round(c.x + (hash(k, j, seed + 40) - 0.5) * c.r * 1.6); let fy = 0; for (let y = Math.round(c.y); y < Math.round(c.y + c.r); y++) if (T.has(fx, y)) fy = y; if (!fy) continue;
      const flen = 5 + Math.floor(hash(k, j, seed + 41) * 16), sway = hash(k, j, seed + 42) < 0.5 ? 1 : -1;
      for (let q = 1; q <= flen; q++) { const xx = fx + Math.round(Math.sin(q / flen * 1.6) * sway * 1.5); T.set(xx, fy + q, q < flen * 0.5 ? '#2e5a50' : '#3f7a62'); }
      if (hash(k, j, seed + 43) < 0.11) lights.push([fx + Math.round(Math.sin(1.6) * sway * 1.5), fy + flen + 1]);
    }
  }
  T.outline(() => hex('#141a1c'), 'all');
  for (const [x, y] of lights) { glow(x, y, '#c8fff0', 0.9); glow(x, y + 1, '#7fe8d4', 0.5); }
  return cut(T, bloomed(E), name);
}
// A bush with luminous berries.
function bush(seed, name) {
  const B = new Layer(), E = new Float32Array(K.W * K.H * 3), bb = GY + 1, s = seed;
  const glow = (x, y, c, k) => { x |= 0; y |= 0; c = hex(c); B.set(x, y, c); const i = (y * K.W + x) * 3; E[i] += c[0] * k; E[i + 1] += c[1] * k; E[i + 2] += c[2] * k; };
  const j = (k, a) => (hash(k, s, 77) - 0.5) * a;
  const bf = K.foliage([{ x: BX - 10 + j(1, 4), y: bb - 7, r: 9 + j(2, 3), sq: 1.2 }, { x: BX + 8 + j(3, 4), y: bb - 6, r: 9 + j(4, 3), sq: 1.2 }, { x: BX - 1 + j(5, 3), y: bb - 12, r: 11 + j(6, 3), sq: 1.3, z: 1 }], { leaf: 4, lobes: 0.4, ragged: 3 });
  for (let i = 0; i < K.W * K.H; i++) if (bf.id[i] >= 0 && (i / K.W | 0) < bb) { const x = i % K.W, y = (i / K.W) | 0; const l = K.leafLight({ n: [bf.N[i * 3], bf.N[i * 3 + 1], bf.N[i * 3 + 2]], leaf: [bf.Lf[i * 3], bf.Lf[i * 3 + 1]], gap: bf.Lf[i * 3 + 2], clump: bf.clumps[bf.id[i]] }); B.set(x, y, ramp(['#1f3d3a', '#2e5a50', '#3f7a62', '#5c9c72', '#95c88a'], (l + 0.55) / 1.4)); }
  B.outline(() => hex('#141a1c'), 'nottop');
  const berries = seed % 2 ? [[-12, -8], [-4, -15], [6, -10], [12, -5], [0, -4]] : [[-10, -6], [-2, -13], [8, -9], [3, -3], [13, -7], [-6, -11]];
  for (const [dx, dy] of berries) { glow(BX + dx, bb + dy, '#ffd0f0', 0.9); glow(BX + dx + 1, bb + dy, '#ff7ad0', 0.6); glow(BX + dx, bb + dy + 1, '#ff7ad0', 0.6); glow(BX + dx + 1, bb + dy + 1, '#c04a9a', 0.3); }
  return cut(B, bloomed(E), name, 1);
}
// A bell flower on a stem, glowing.
function bell(seed, col, hi, name) {
  const L = new Layer(), E = new Float32Array(K.W * K.H * 3);
  const glow = (x, y, c, k) => { x |= 0; y |= 0; c = hex(c); L.set(x, y, c); const i = (y * K.W + x) * 3; E[i] += c[0] * k; E[i + 1] += c[1] * k; E[i + 2] += c[2] * k; };
  const y = GY - 1, h = 6 + Math.floor(hash(seed, 1, 3) * 6), x = BX;
  for (let k = 0; k < h; k++) L.set(x + (k > h - 3 ? 1 : 0), y - k, '#4f8a66');
  L.set(x - 1, y - 1, '#5f9c74'); L.set(x - 2, y - 2, '#5f9c74'); L.set(x + 1, y - 2, '#5f9c74');
  const hx = x + 1, hy = y - h;
  glow(hx, hy, hi, 0.9); glow(hx - 1, hy, col, 0.6); glow(hx + 1, hy, col, 0.6); glow(hx, hy - 1, col, 0.5); glow(hx - 1, hy - 1, col, 0.4); glow(hx + 1, hy - 1, col, 0.4); glow(hx - 2, hy + 1, col, 0.45); glow(hx + 2, hy + 1, col, 0.45); glow(hx, hy + 1, hi, 0.5);
  L.set(hx - 1, hy + 2, '#3f6f52'); L.set(hx + 2, hy + 2, '#3f6f52');
  return cut(L, bloomed(E), name, 0);
}
// A tuft of grass: a clump of the stems the moss grows, taller.
function tuft(seed, name) {
  const L = new Layer(), E = new Float32Array(K.W * K.H * 3);
  for (let i = -4; i <= 4; i++) {
    if (hash(i, seed, 31) < 0.25) continue;
    const h = 4 + Math.floor(hash(i, seed, 32) ** 1.3 * 9 * (1 - Math.abs(i) / 6)), lean = i < -1 ? -1 : i > 1 ? 1 : 0;
    for (let k = 0; k < h; k++) { const xx = BX + i + (k > h * 0.6 ? lean : 0); L.set(xx, GY - 1 - k, k < h / 3 ? '#3f6f52' : k < h * 0.7 ? '#5f9c74' : '#86c492'); }
    if (h > 7 && hash(i, seed, 33) < 0.3) { L.set(BX + i + lean, GY - 1 - h, '#bff8e0'); const j = ((GY - 1 - h) * K.W + BX + i + lean) * 3; E[j] += 0.5; E[j + 1] += 0.7; E[j + 2] += 0.6; }
  }
  return cut(L, bloomed(E), name, 0);
}
// A rock of the planet's stone, outlined, mica catching the sun.
function rock(seed, name) {
  const R = new Layer(), E = new Float32Array(K.W * K.H * 3), rb = GY + 2, rw = 15 + (seed % 3) * 2, rh = 14 + (seed % 2) * 4;
  for (let y = rb - rh - 4; y <= rb; y++) for (let x = BX - rw; x <= BX + rw; x++) {
    const dx = (x - BX) / rw, dy = (y - rb + 3) / rh, e = dx * dx + dy * dy + (K.vnoise(x, y, 5, 3 + seed) - 0.5) * 0.25; if (e > 1) continue;
    const facet = K.vnoise(x, y, 7, 8 + seed) > 0.55 ? 0.25 : 0, s = dx * 0.55 - dy * 0.75 + facet;
    R.set(x, y, ramp(['#3f4450', '#525866', '#687080', '#8a92a0', '#b6bcc4'], (s + 1) / 2.1));
  }
  R.outline(() => hex('#1a1c24'));
  for (const [x, y] of [[BX + 6, rb - rh + 1], [BX - 3, rb - rh + 6]]) { const i = (y * K.W + x) * 3; E[i] += 0.9; E[i + 1] += 0.85; E[i + 2] += 0.7; for (const [ax, ay] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) { const j = ((y + ay) * K.W + x + ax) * 3; E[j] += 0.35; E[j + 1] += 0.32; E[j + 2] += 0.25; } }
  return cut(R, bloomed(E), name);
}
const sizes = {};
sizes.acacia_0 = acacia(50, 84, 13, 'acacia_0');
sizes.acacia_1 = acacia(71, 66, 11, 'acacia_1');
sizes.acacia_2 = acacia(93, 74, 12, 'acacia_2');
sizes.bush_0 = bush(1, 'bush_0'); sizes.bush_1 = bush(2, 'bush_1');
sizes.rock_0 = rock(0, 'rock_0'); sizes.rock_1 = rock(1, 'rock_1');
for (const [id, c, h] of [['bellTeal', '#7fe8d4', '#e6fff8'], ['bellViolet', '#b49aff', '#f2ecff'], ['bellAmber', '#ffd27a', '#fff6e0']])
  for (let v = 0; v < 2; v++) sizes[`${id}_${v}`] = bell(v * 7 + id.length, c, h, `${id}_${v}`);
for (let v = 0; v < 3; v++) sizes[`tuft_${v}`] = tuft(v + 1, `tuft_${v}`);
console.log(JSON.stringify(sizes));

// ---------- the backdrop: each layer tiles across every BW pixels; rows as the study's, the ground line at y 272 ----------
const BW = 2048, BH = 384;
// The sky: a strip of its gradient, top to the horizon's warm haze.
{ const s = img(1, BH); for (let y = 0; y < BH; y++) put(s, 0, y, rampSmooth(['#3e6fb0', '#5f93c8', '#93bfdc', '#cfe0dc', '#f1e3c2'], y / 290)); save(s, 'sky'); }
// High thin cloud streaks, white, tiling.
{ const s = img(BW, 160); for (let y = 0; y < 160; y++) for (let x = 0; x < BW; x++) { const n = pfbm(x * 0.35, y * 3, 60, 41, 4, BW * 0.35, 0), band = smooth(0.58, 0.75, n) * smooth(150, 60, y); if (band > 0.02) put(s, x, y, '#fbf7ec', band * 0.55); } save(s, 'clouds'); }
// The sun: a white core, a warm halo, the thin cross glint of our effects; added over the sky.
{ const R = 90, s = img(2 * R + 1, 2 * R + 1);
  const add = (x, y, c, k) => { const i = y * s.w + x; for (let q = 0; q < 3; q++) s.c[i * 3 + q] += c[q] * k; };
  for (let y = 0; y < s.h; y++) for (let x = 0; x < s.w; x++) { const d = Math.hypot(x - R, y - R); if (d < 9) add(x, y, d < 7 ? [1, 1, 1] : hex('#fff6d8'), 1); else add(x, y, [1, 0.92, 0.72], 0.35 * Math.exp(-d / 22)); }
  for (let k = 10; k < 46; k++) { const f = 0.5 * (1 - k / 46) ** 2; add(R + k, R, [1, 0.96, 0.85], f); add(R - k, R, [1, 0.96, 0.85], f); add(R, R + k, [1, 0.96, 0.85], f * 0.7); add(R, R - k, [1, 0.96, 0.85], f * 0.7); }
  for (let i = 0; i < s.w * s.h; i++) { const m = Math.max(s.c[i * 3], s.c[i * 3 + 1], s.c[i * 3 + 2]); if (m > 0.004) { for (let q = 0; q < 3; q++) s.c[i * 3 + q] /= Math.max(1, m); s.a[i] = clamp(m); } }
  // Stored for adding: colour full, its strength in alpha.
  for (let i = 0; i < s.w * s.h; i++) if (s.a[i] > 0) { const m = Math.max(s.c[i * 3], s.c[i * 3 + 1], s.c[i * 3 + 2]); for (let q = 0; q < 3; q++) s.c[i * 3 + q] /= m; }
  save(s, 'sun'); }
// A gas giant with a ring, pale in the day sky.
{ const gr = 52, s = img(232, 150), gx = 116, gy = 75;
  for (let y = 0; y < s.h; y++) for (let x = 0; x < s.w; x++) {
    const dx = x - gx, dy = y - gy, d = Math.hypot(dx, dy);
    const rx = dx * Math.cos(-0.25) - dy * Math.sin(-0.25), ry = (dx * Math.sin(-0.25) + dy * Math.cos(-0.25)) / 0.22, rd = Math.hypot(rx, ry);
    const ringFront = rd > gr * 1.32 && rd < gr * 1.95 && (ry > 0 || d > gr) && !((rd > gr * 1.55 && rd < gr * 1.6));
    if (d < gr) { const band = Math.floor((dy + 6 * K.vnoise(dx, dy, 18, 3)) / 9) % 3, lit = clamp(0.5 + (dx * 0.5 - dy * 0.6) / gr * 0.6); put(s, x, y, mix(band === 0 ? '#c9d8d4' : band === 1 ? '#b3c8cc' : '#dbe3d6', '#7f9cb4', 1 - lit), 0.55 - (d > gr - 1.5 ? 0.15 : 0)); }
    if (ringFront) put(s, x, y, rd < gr * 1.6 ? '#e8efe8' : '#c4d6dc', 0.32);
  }
  save(s, 'giant'); }
// Far mesas lost in the bright haze, rim-lit toward the sun; then a nearer ridge; both down to the bottom.
{ const top = 96, s = img(BW, BH - top), far = (x) => 168 - pfbm(x, 0, 128, 7, 4, BW, 0) * 64;
  for (let x = 0; x < BW; x++) { const f = far(x), sl = far((x + 2) % BW) - far((x - 2 + BW) % BW);
    for (let y = Math.max(top, Math.floor(f)); y < BH; y++) { const t = clamp((y - f) / 90); let c = mix('#9bb7cc', '#c6d6d6', t); c = mix(c, sl < 0 ? '#dce6e2' : '#8aa6bc', clamp(Math.abs(sl) * 0.12) * 0.5 * Math.exp(-(y - f) / 30)); put(s, x, y - top, mix(c, '#efe6cc', smooth(150, 240, y) * 0.6)); } }
  save(s, 'far'); }
{ const top = 140, s = img(BW, BH - top), farB = (x) => 196 - pfbm(x, 0, 64, 13, 3, BW, 0) * 40;
  for (let x = 0; x < BW; x++) { const b = farB(x), sb = farB((x + 2) % BW) - farB((x - 2 + BW) % BW);
    for (let y = Math.max(top, Math.floor(b)); y < BH; y++) { let c = mix('#7d9fb2', '#a8bfc4', clamp((y - b) / 60)); if (y - b < 1.5 && sb < -0.6) c = mix(c, '#cddcd8', 0.6); put(s, x, y - top, mix(c, '#e9e2c8', smooth(170, 250, y) * 0.55)); } }
  save(s, 'ridge'); }
// Near hills with spindly branching alien trees in silhouette, a warm haze where the land meets the sky.
{ const top = 150, s = img(BW, BH - top), hill = (x) => 236 - pfbm(x, 0, 128, 19, 3, BW, 0) * 26;
  const HL = { w: BW, h: BH, a: new Uint8Array(BW * BH), c: new Array(BW * BH) };
  const set = (x, y, c) => { x = ((x | 0) % BW + BW) % BW; y |= 0; if (y < 0 || y >= BH) return; HL.a[y * BW + x] = 1; HL.c[y * BW + x] = c; };
  for (let x = 0; x < BW; x++) for (let y = Math.floor(hill(x)); y < BH; y++) set(x, y, y - hill(x) < 1.5 ? '#86a8a0' : '#5f8684');
  for (let k = 0; k < 24; k++) {
    const tx = 20 + k * 85 + hash(k, 1, 61) * 50, sk = K.treeSkeleton({ x: 480, y: hill(tx) + 2, len: 12 + hash(k, 2, 61) ** 2 * 34, lift: 1, w: 3, seed: 600 + k, depth: 2, spread: 0.8, childLen: 0.62, step: 6, wiggle: 0.3, minW: 1 });
    const r = K.rasterSegs(sk.segs); for (let i = 0; i < K.W * K.H; i++) if (!Number.isNaN(r.U[i])) set(i % K.W - 480 + tx, (i / K.W) | 0, '#5f8684');
    for (const t of sk.tips) for (let y = -4; y <= 2; y++) for (let x = -6; x <= 6; x++) if ((x * x) / 36 + (y * y) / 9 < 1 && K.vnoise(t.x + x, t.y + y, 2, k) > 0.3) set(t.x - 480 + tx + x, t.y + y, y < -1 ? '#7fa29a' : '#5f8684');
  }
  for (let y = top; y < BH; y++) for (let x = 0; x < BW; x++) {
    const i = y * BW + x; if (!HL.a[i]) continue;
    let c = mix(HL.c[i], '#e6dfc4', smooth(200, 280, y) * 0.5 + 0.12);
    c = mix(c, '#f4e8c8', 0.18 * smooth(200, 260, y) * (0.7 + 0.3 * pvnoise(x, y, 32, 77, BW, 0)));
    put(s, x, y - top, c);
  }
  save(s, 'hills'); }
console.log('backdrop done');

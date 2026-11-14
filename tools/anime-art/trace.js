// node tools/anime-art/trace.js <name> [debug.png]: traces refs/<name>.png by refs/<name>.palette.json into traced/<name>.json.
// Traces an AI-made reference for its form only (README, Traced subjects): the reference's own ink strokes cut its silhouette into
// parts, each part is named by the palette material nearest its mean colour, and every part's outline is traced (marching squares,
// RDP, beziers) in the reference's pixels. Writes the draw ops (the body, every part its own shape, overlays after their part, the
// reference's loose strokes) and the fit into the icon box; traced.js draws them with the engine's own line, paint and finish.
// Needs ffmpeg on the PATH to read the PNG.
'use strict';
const fs = require('fs'), path = require('path'), { spawnSync } = require('child_process');
const [name, debugPng] = process.argv.slice(2);
if (!name) { console.error('usage: node tools/anime-art/trace.js <name> [debug.png]'); process.exit(2); }
const refPng = path.join(__dirname, 'refs', name + '.png'), outFile = path.join(__dirname, 'traced', name + '.json');
const P = JSON.parse(fs.readFileSync(path.join(__dirname, 'refs', name + '.palette.json'), 'utf8'));
const hex = (h) => [1, 3, 5].map((i) => parseInt(h.substr(i, 2), 16));
const lum = (r, g, b) => .3 * r + .59 * g + .11 * b;

// ---------- Decode ----------
const png = fs.readFileSync(refPng);
const W = png.readUInt32BE(16), H = png.readUInt32BE(20), N = W * H;
const dec = spawnSync('ffmpeg', ['-loglevel', 'error', '-i', refPng, '-f', 'rawvideo', '-pix_fmt', 'rgba', '-'], { maxBuffer: N * 4 + 1024 });
if (dec.status !== 0 || dec.stdout.length !== N * 4) throw new Error('ffmpeg decode failed: ' + dec.stderr);
const px = dec.stdout;

// ---------- Mask helpers (Uint8Array W*H) ----------
const idx = (x, y) => y * W + x;
function slide(src, r, isMax) {
  // A square kernel, separable: the max (dilate) or min (erode) over (2r+1)².
  const tmp = new Uint8Array(N), out = new Uint8Array(N);
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    let v = isMax ? 0 : 1;
    for (let k = Math.max(0, x - r), e = Math.min(W - 1, x + r); k <= e; k++) { const s = src[y * W + k]; if (isMax ? s : !s) { v = isMax ? 1 : 0; break; } }
    tmp[y * W + x] = v;
  }
  for (let x = 0; x < W; x++) for (let y = 0; y < H; y++) {
    let v = isMax ? 0 : 1;
    for (let k = Math.max(0, y - r), e = Math.min(H - 1, y + r); k <= e; k++) { const s = tmp[k * W + x]; if (isMax ? s : !s) { v = isMax ? 1 : 0; break; } }
    out[y * W + x] = v;
  }
  return out;
}
const dilate = (m, r) => (r > 0 ? slide(m, r, true) : m), erode = (m, r) => (r > 0 ? slide(m, r, false) : m);
const open = (m, r) => dilate(erode(m, r), r), close = (m, r) => erode(dilate(m, r), r);
const and = (a, b) => a.map((v, i) => v & b[i]), not = (a) => a.map((v) => 1 - v);
/** Connected components (4-neighbour) of a mask: [labels Int32Array (-1 off), list of {id, n, x0,y0,x1,y1}]. */
function components(m) {
  const lab = new Int32Array(N).fill(-1), comps = [], st = new Int32Array(N);
  for (let i = 0; i < N; i++) {
    if (!m[i] || lab[i] >= 0) continue;
    const c = { id: comps.length, n: 0, x0: W, y0: H, x1: 0, y1: 0 };
    let sp = 0; st[sp++] = i; lab[i] = c.id;
    while (sp) {
      const j = st[--sp], x = j % W, y = (j / W) | 0;
      c.n++; if (x < c.x0) c.x0 = x; if (x > c.x1) c.x1 = x; if (y < c.y0) c.y0 = y; if (y > c.y1) c.y1 = y;
      if (x > 0 && m[j - 1] && lab[j - 1] < 0) { lab[j - 1] = c.id; st[sp++] = j - 1; }
      if (x < W - 1 && m[j + 1] && lab[j + 1] < 0) { lab[j + 1] = c.id; st[sp++] = j + 1; }
      if (y > 0 && m[j - W] && lab[j - W] < 0) { lab[j - W] = c.id; st[sp++] = j - W; }
      if (y < H - 1 && m[j + W] && lab[j + W] < 0) { lab[j + W] = c.id; st[sp++] = j + W; }
    }
    comps.push(c);
  }
  return [lab, comps];
}
function fillHoles(m) {
  const [lab, comps] = components(not(m)), outside = new Set(comps.filter((c) => c.x0 === 0 || c.y0 === 0 || c.x1 === W - 1 || c.y1 === H - 1).map((c) => c.id));
  return m.map((v, i) => (v || !outside.has(lab[i]) ? 1 : 0));
}
/** Every unlabelled pixel of `within` takes the label of the nearest labelled one (multi-source BFS, 4-neighbour). */
function spread(labels, within) {
  const out = Int32Array.from(labels), q = new Int32Array(N);
  let h = 0, t = 0;
  for (let i = 0; i < N; i++) if (out[i] >= 0) q[t++] = i;
  while (h < t) {
    const j = q[h++], x = j % W, l = out[j];
    for (const k of [x > 0 ? j - 1 : -1, x < W - 1 ? j + 1 : -1, j - W, j + W]) if (k >= 0 && k < N && within[k] && out[k] < 0) { out[k] = l; q[t++] = k; }
  }
  return out;
}

// ---------- Segment ----------
const bg = hex(P.background), bgTol = P.backgroundTolerance || 30;
let sil = new Uint8Array(N);
for (let i = 0; i < N; i++) sil[i] = Math.hypot(px[i * 4] - bg[0], px[i * 4 + 1] - bg[1], px[i * 4 + 2] - bg[2]) > bgTol && px[i * 4 + 3] > 127 ? 1 : 0;
{ const [lab, comps] = components(sil), big = comps.reduce((a, c) => (c.n > a.n ? c : a)); sil = sil.map((v, i) => (lab[i] === big.id ? 1 : 0)); }
sil = fillHoles(open(close(sil, 2), 2));
const silArea = sil.reduce((a, v) => a + v, 0);

// The reference's ink: thin dark ridges, pixels black outright or well darker than the paint round them (a seam between two rolls
// is dark olive, not black). A dark area wider than a stroke (smoked glass, a shadow patch) is paint, not ink.
const inkL = P.ink.maxLum, inkW = P.ink.width, rel = P.ink.relative || 0.65, R0 = P.ink.around || 12;
const L = new Float32Array(N);
for (let i = 0; i < N; i++) L[i] = lum(px[i * 4], px[i * 4 + 1], px[i * 4 + 2]);
const boxBlur = (src, r) => {
  const t = new Float32Array(N), o = new Float32Array(N);
  for (let y = 0; y < H; y++) { let s = 0; for (let x = -r; x < W + r; x++) { if (x + r < W) s += src[y * W + x + r]; if (x - r - 1 >= 0) s -= src[y * W + x - r - 1]; if (x >= 0) t[y * W + x] = s; } }
  for (let x = 0; x < W; x++) { let s = 0; for (let y = -r; y < H + r; y++) { if (y + r < H) s += t[(y + r) * W + x]; if (y - r - 1 >= 0) s -= t[(y - r - 1) * W + x]; if (y >= 0) o[y * W + x] = s; } }
  return o;
};
const around = boxBlur(L.map((v, i) => v * sil[i]), R0), weight = boxBlur(Float32Array.from(sil), R0);
/** The ink at a strength: how much darker than its surroundings a pixel must be, and how wide a gap in a stroke is bridged. */
function inkAt(relative, bridge) {
  const dark = new Uint8Array(N);
  for (let i = 0; i < N; i++) dark[i] = sil[i] && (L[i] < inkL || L[i] < relative * (around[i] / Math.max(1, weight[i]))) ? 1 : 0;
  return and(close(and(dark, not(open(dark, inkW))), bridge), sil);
}
const ink = inkAt(rel, P.ink.bridge || 2);

// Parts: what the ink encloses; small crumbs go back to their neighbours. A large part is split again by a stronger ink (fainter
// lines, wider gaps bridged) when that cuts it into large pieces: two quilted rolls whose seam fades out, a frame on its plate.
// The stronger ink is not used everywhere, for it would fill a buckle's thin ring.
const minPart = (P.minPart || 0.0015) * silArea;
let [partLab, partsAll] = components(and(sil, not(ink)));
if (P.ink.coarse) {
  const coarse = inkAt(P.ink.coarse.relative, P.ink.coarse.bridge), [cl] = components(and(sil, not(coarse))), splitMin = (P.ink.coarse.min || 0.02) * silArea;
  const next = new Int32Array(N).fill(-1); let id = 0;
  for (const c of partsAll) {
    const counts = new Map();
    for (let y = c.y0; y <= c.y1; y++) for (let x = c.x0; x <= c.x1; x++) { const i = idx(x, y); if (partLab[i] === c.id && cl[i] >= 0) counts.set(cl[i], (counts.get(cl[i]) || 0) + 1); }
    const big = [...counts].filter(([, n]) => n >= splitMin).map(([k]) => k);
    const map = new Map(big.length >= 2 ? big.map((k) => [k, id++]) : []), whole = big.length >= 2 ? -1 : id++;
    for (let y = c.y0; y <= c.y1; y++) for (let x = c.x0; x <= c.x1; x++) { const i = idx(x, y); if (partLab[i] !== c.id) continue; next[i] = whole >= 0 ? whole : map.has(cl[i]) ? map.get(cl[i]) : -1; }
  }
  // Re-label by the split ids: components of each id apart, so touching pieces stay distinct.
  const lab2 = new Int32Array(N).fill(-1), comps2 = [];
  const st = new Int32Array(N);
  for (let i = 0; i < N; i++) {
    if (next[i] < 0 || lab2[i] >= 0) continue;
    const c = { id: comps2.length, n: 0, x0: W, y0: H, x1: 0, y1: 0 }, v = next[i]; let sp = 0; st[sp++] = i; lab2[i] = c.id;
    while (sp) {
      const j = st[--sp], x = j % W, y = (j / W) | 0; c.n++; if (x < c.x0) c.x0 = x; if (x > c.x1) c.x1 = x; if (y < c.y0) c.y0 = y; if (y > c.y1) c.y1 = y;
      for (const k of [x > 0 ? j - 1 : -1, x < W - 1 ? j + 1 : -1, j >= W ? j - W : -1, j + W < N ? j + W : -1]) if (k >= 0 && next[k] === v && lab2[k] < 0) { lab2[k] = c.id; st[sp++] = k; }
    }
    comps2.push(c);
  }
  partLab = lab2; partsAll = comps2;
}
const keep = partsAll.filter((c) => c.n >= minPart), keepId = new Map(keep.map((c, k) => [c.id, k]));
let lab0 = new Int32Array(N).fill(-1);
for (let i = 0; i < N; i++) if (partLab[i] >= 0 && keepId.has(partLab[i])) lab0[i] = keepId.get(partLab[i]);
const parts = keep.map((c, k) => ({ k, n: c.n, box: [c.x0, c.y0, c.x1, c.y1] }));

// Name each part by the material nearest its mean colour.
const MAT = Object.entries(P.materials).filter(([, m]) => !m.overlay);
const sums = parts.map(() => [0, 0, 0]);
for (let i = 0; i < N; i++) if (lab0[i] >= 0) { const s = sums[lab0[i]]; s[0] += px[i * 4]; s[1] += px[i * 4 + 1]; s[2] += px[i * 4 + 2]; }
for (const p of parts) {
  p.mean = sums[p.k].map((v) => v / p.n);
  let best = null, bd = 1e9;
  for (const [name, m] of MAT) for (const r of m.ref) { const c = hex(r), d = Math.hypot(p.mean[0] - c[0], p.mean[1] - c[1], p.mean[2] - c[2]); if (d < bd) { bd = d; best = name; } }
  p.mat = best;
}
// A material marked single (a visor's glass) is one part: its smaller pieces, a shadow under a frame read as glass, go to their
// neighbours with the ink.
{
  const drop = new Set();
  for (const [mname, m] of Object.entries(P.materials)) {
    if (!m.single) continue;
    const of = parts.filter((p) => p.mat === mname).sort((a, b) => b.n - a.n);
    for (const p of of.slice(1)) drop.add(p.k);
  }
  if (drop.size) {
    const kept = parts.filter((p) => !drop.has(p.k)), next = new Map(kept.map((p, k) => [p.k, k]));
    for (let i = 0; i < N; i++) if (lab0[i] >= 0) lab0[i] = next.has(lab0[i]) ? next.get(lab0[i]) : -1;
    parts.length = 0;
    kept.forEach((p, k) => parts.push({ ...p, k }));
  }
}
// The partition: ink pixels go to the nearest part.
const lab = spread(lab0, sil);
// Each part's box grows with the ink it took.
for (const p of parts) p.inner = p.box.slice();
for (let i = 0; i < N; i++) { const k = lab[i]; if (k < 0) continue; const b = parts[k].box, x = i % W, y = (i / W) | 0; if (x < b[0]) b[0] = x; if (x > b[2]) b[2] = x; if (y < b[1]) b[1] = y; if (y > b[3]) b[3] = y; }

// ---------- Trace ----------
/** Marching squares on a mask blurred by a few pixels, iso 0.5: the longest closed loop, in pixel centres. */
function contour(mask, box, blur) {
  const pad = blur * 3 + 2, x0 = Math.max(0, box[0] - pad), y0 = Math.max(0, box[1] - pad), x1 = Math.min(W - 1, box[2] + pad), y1 = Math.min(H - 1, box[3] + pad);
  const w = x1 - x0 + 1, h = y1 - y0 + 1;
  let f = new Float32Array(w * h);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) f[y * w + x] = mask[idx(x0 + x, y0 + y)];
  for (let pass = 0; pass < 2; pass++) {
    const g = new Float32Array(w * h);
    for (let y = 0; y < h; y++) { let s = 0; for (let x = -blur; x < w + blur; x++) { const a = x + blur, b = x - blur - 1; if (a < w && a >= 0) s += f[y * w + a]; if (b >= 0 && b < w) s -= f[y * w + b]; if (x >= 0 && x < w) g[y * w + x] = s / (2 * blur + 1); } }
    const o = new Float32Array(w * h);
    for (let x = 0; x < w; x++) { let s = 0; for (let y = -blur; y < h + blur; y++) { const a = y + blur, b = y - blur - 1; if (a < h && a >= 0) s += g[a * w + x]; if (b >= 0 && b < h) s -= g[b * w + x]; if (y >= 0 && y < h) o[y * w + x] = s / (2 * blur + 1); } }
    f = o;
  }
  const v = (x, y) => (x < 0 || y < 0 || x >= w || y >= h ? 0 : f[y * w + x]);
  const ends = new Map(), segs = [];
  const key = (p) => p[0].toFixed(3) + ',' + p[1].toFixed(3);
  const lerp = (xa, ya, va, xb, yb, vb) => { const t = (0.5 - va) / (vb - va); return [xa + (xb - xa) * t, ya + (yb - ya) * t]; };
  for (let y = -1; y < h; y++) for (let x = -1; x < w; x++) {
    const a = v(x, y), b = v(x + 1, y), c = v(x + 1, y + 1), d = v(x, y + 1);
    const code = (a > .5 ? 8 : 0) | (b > .5 ? 4 : 0) | (c > .5 ? 2 : 0) | (d > .5 ? 1 : 0);
    if (code === 0 || code === 15) continue;
    const T = () => lerp(x, y, a, x + 1, y, b), R = () => lerp(x + 1, y, b, x + 1, y + 1, c), B = () => lerp(x, y + 1, d, x + 1, y + 1, c), L = () => lerp(x, y, a, x, y + 1, d);
    // Segments oriented with the inside on one side; saddles split by the centre's value.
    const m = { 1: [[L, B]], 2: [[B, R]], 3: [[L, R]], 4: [[R, T]], 5: (a + b + c + d) / 4 > .5 ? [[L, T], [R, B]] : [[L, B], [R, T]], 6: [[B, T]], 7: [[L, T]], 8: [[T, L]], 9: [[T, B]],
      10: (a + b + c + d) / 4 > .5 ? [[T, R], [B, L]] : [[T, L], [B, R]], 11: [[T, R]], 12: [[R, L]], 13: [[R, B]], 14: [[B, L]] }[code];
    for (const [p, q] of m) segs.push([p(), q()]);
  }
  for (const s of segs) { const k = key(s[0]); if (!ends.has(k)) ends.set(k, []); ends.get(k).push(s); }
  const used = new Set(), loops = [];
  for (const s of segs) {
    if (used.has(s)) continue;
    const loop = [s[0]]; let cur = s;
    while (cur && !used.has(cur)) { used.add(cur); loop.push(cur[1]); cur = (ends.get(key(cur[1])) || []).find((n) => !used.has(n)); }
    loops.push(loop);
  }
  let best = loops[0], bl = 0;
  for (const l of loops) { let s = 0; for (let i = 1; i < l.length; i++) s += Math.hypot(l[i][0] - l[i - 1][0], l[i][1] - l[i - 1][1]); if (s > bl) { bl = s; best = l; } }
  return best.map(([x, y]) => [x + x0 + .5, y + y0 + .5]);
}
function rdp(pts, eps) {
  if (pts.length < 3) return pts;
  const [a, b] = [pts[0], pts[pts.length - 1]], dx = b[0] - a[0], dy = b[1] - a[1], L = Math.hypot(dx, dy) || 1;
  let im = 0, dm = 0;
  for (let i = 1; i < pts.length - 1; i++) { const d = L > 1e-9 ? Math.abs((pts[i][0] - a[0]) * dy - (pts[i][1] - a[1]) * dx) / L : Math.hypot(pts[i][0] - a[0], pts[i][1] - a[1]); if (d > dm) { dm = d; im = i; } }
  if (dm <= eps) return [a, b];
  return rdp(pts.slice(0, im + 1), eps).slice(0, -1).concat(rdp(pts.slice(im), eps));
}
/** A closed loop simplified: split at its two farthest points so RDP has fixed ends. */
function simplifyLoop(loop, eps) {
  const pts = loop.slice(0, -1);
  let far = 0, fd = 0;
  for (let i = 0; i < pts.length; i++) { const d = Math.hypot(pts[i][0] - pts[0][0], pts[i][1] - pts[0][1]); if (d > fd) { fd = d; far = i; } }
  const A = rdp(pts.slice(0, far + 1), eps), B = rdp(pts.slice(far).concat([pts[0]]), eps);
  return A.slice(0, -1).concat(B.slice(0, -1));
}
const f1 = (v) => +v.toFixed(1);
/** Points to cubic beziers through them (Catmull-Rom); a vertex turning sharper than `corner` degrees stays a corner. */
function bez(pts, closed, corner = 50, tension = 1) {
  const n = pts.length, at = (i) => pts[closed ? (i + n) % n : Math.max(0, Math.min(n - 1, i))];
  const sharp = (i) => {
    if (!closed && (i === 0 || i === n - 1)) return true;
    const p = at(i - 1), q = at(i), r = at(i + 1), a1 = Math.atan2(q[1] - p[1], q[0] - p[0]), a2 = Math.atan2(r[1] - q[1], r[0] - q[0]);
    let d = Math.abs(a2 - a1) * 180 / Math.PI; if (d > 180) d = 360 - d; return d > corner;
  };
  const tan = (i) => { if (sharp(i)) return [0, 0]; const p = at(i - 1), r = at(i + 1); return [(r[0] - p[0]) / 6 * tension, (r[1] - p[1]) / 6 * tension]; };
  let d = `M${f1(pts[0][0])} ${f1(pts[0][1])}`;
  const segs = closed ? n : n - 1;
  for (let i = 0; i < segs; i++) {
    const p = at(i), q = at(i + 1), tp = tan(i), tq = tan(i + 1);
    d += `C${f1(p[0] + tp[0])} ${f1(p[1] + tp[1])} ${f1(q[0] - tq[0])} ${f1(q[1] - tq[1])} ${f1(q[0])} ${f1(q[1])}`;
  }
  return closed ? d + 'Z' : d;
}
const maskOf = (pred, box) => { const m = new Uint8Array(N); for (let y = box[1]; y <= box[3]; y++) for (let x = box[0]; x <= box[2]; x++) { const i = idx(x, y); if (pred(i)) m[i] = 1; } return m; };
const boxOf = (m) => { let x0 = W, y0 = H, x1 = 0, y1 = 0, n = 0; for (let i = 0; i < N; i++) if (m[i]) { n++; const x = i % W, y = (i / W) | 0; if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; } return n ? [x0, y0, x1, y1] : null; };
const tracePath = (m, box, eps, blur = 2) => bez(simplifyLoop(contour(m, box, blur), eps), true);

/** An open stroke from a thin pixel set: its two farthest ends by walking it, the walk between them. */
function strokeOf(cells) {
  const set = new Set(cells), nb = (j) => { const x = j % W, o = []; for (const dy of [-W, 0, W]) for (const dx of [-1, 0, 1]) { if (!dx && !dy) continue; if ((x === 0 && dx < 0) || (x === W - 1 && dx > 0)) continue; const k = j + dy + dx; if (set.has(k)) o.push(k); } return o; };
  const bfs = (s) => { const prev = new Map([[s, -1]]), q = [s]; let last = s; for (let h = 0; h < q.length; h++) { last = q[h]; for (const k of nb(q[h])) if (!prev.has(k)) { prev.set(k, q[h]); q.push(k); } } return [last, prev]; };
  const [a] = bfs(cells[0]), [b, prev] = bfs(a);
  const pts = []; for (let j = b; j !== -1; j = prev.get(j)) pts.push([j % W + .5, ((j / W) | 0) + .5]);
  // A little smoothing along the walk, then the fewest points that keep it.
  const sm = pts.map((p, i) => { let sx = 0, sy = 0, c = 0; for (let k = Math.max(0, i - 4); k <= Math.min(pts.length - 1, i + 4); k++) { sx += pts[k][0]; sy += pts[k][1]; c++; } return [sx / c, sy / c]; });
  if (sm.length > 2) { sm[0] = pts[0]; sm[sm.length - 1] = pts[pts.length - 1]; }
  return sm;
}

// ---------- Build the ops ----------
const out = { reference: path.basename(refPng), size: [W, H], parts: [] };
const sbox = boxOf(sil), bw = sbox[2] - sbox[0] + 1, bh = sbox[3] - sbox[1] + 1, u = Math.max(bw, bh) / (P.fit || 15.4);
out.fit = { units: +u.toFixed(4), shift: [f1((16 * u - bw) / 2 - sbox[0]), f1((16 * u - bh) / 2 - sbox[1])], box: sbox };
const crescent = (m, box) => { const e = Math.min(box[2] - box[0], box[3] - box[1]) / u; return +Math.max(.22, Math.min(.9, e * (m.shade ?? .16))).toFixed(3); };

const silD = tracePath(sil, sbox, 1.6, 2);
const baseName = Object.entries(P.materials).find(([, m]) => m.base)[0];
// Each part's filled outline, its material, its nesting depth (how many other parts' filled outlines hold most of it).
const filled = parts.map((p) => fillHoles(maskOf((i) => lab[i] === p.k, p.box)));
for (const p of parts) {
  p.depth = 0;
  for (const q of parts) {
    if (q === p) continue;
    let inside = 0; const F = filled[q.k];
    for (let y = p.box[1]; y <= p.box[3]; y++) for (let x = p.box[0]; x <= p.box[2]; x++) { const i = idx(x, y); if (lab[i] === p.k && F[i]) inside++; }
    if (inside > p.n * .8) p.depth++;
  }
}
const layer = (p) => P.materials[p.mat].layer || 0;
const order = parts.slice().sort((a, b) => a.depth - b.depth || layer(a) - layer(b) || b.n - a.n);
for (const p of order) {
  const m = P.materials[p.mat];
  p.d = tracePath(filled[p.k], p.box, P.epsilon || 1.6, 2);
  p.shade = m.shade === 0 ? 0 : crescent(m, p.box);
  // The material's lit sub-areas the reference paints (a streak on glass): its pixels well above the part's median light.
  if (m.lite) {
    const ls = []; for (let i = 0; i < N; i++) if (lab0[i] === p.k) ls.push(lum(px[i * 4], px[i * 4 + 1], px[i * 4 + 2]));
    ls.sort((a, b) => a - b); const med = ls[ls.length >> 1];
    let lm = maskOf((i) => lab0[i] === p.k && lum(px[i * 4], px[i * 4 + 1], px[i * 4 + 2]) > med + m.lite, p.box);
    lm = open(close(lm, 3), 3);
    const [ll, lc] = components(lm);
    p.lite = lc.filter((c) => c.n > p.n * .03).map((c) => tracePath(maskOf((i) => ll[i] === c.id, [c.x0, c.y0, c.x1, c.y1]), [c.x0, c.y0, c.x1, c.y1], 1.5, 2));
  }
  out.parts.push({ k: p.k, mat: p.mat, n: p.n, depth: p.depth, mean: p.mean.map(Math.round), box: p.box });
}

// Loose strokes: ink with one part on every side (a crease, a stitch), long enough to read.
const loose = [];
{
  const R = inkW + 2, strokeMask = new Uint8Array(N);
  for (let i = 0; i < N; i++) {
    if (!ink[i] || !sil[i]) continue;
    const x = i % W, y = (i / W) | 0; let first = -2, mixed = false;
    for (let dy = -R; dy <= R && !mixed; dy += 2) for (let dx = -R; dx <= R; dx += 2) {
      const X = x + dx, Y = y + dy, l = X < 0 || Y < 0 || X >= W || Y >= H || !sil[idx(X, Y)] ? -1 : lab0[idx(X, Y)];
      if (l === -1 && (X < 0 || Y < 0 || X >= W || Y >= H || !sil[idx(X, Y)])) { mixed = true; break; }
      if (l < 0) continue; if (first === -2) first = l; else if (l !== first) { mixed = true; break; }
    }
    if (!mixed && first >= 0 && P.materials[parts[first].mat].loose !== false) strokeMask[i] = 1;
  }
  const [sl, sc] = components(strokeMask);
  for (const c of sc) {
    if (Math.max(c.x1 - c.x0, c.y1 - c.y0) < (P.looseMin || 0.8) * u) continue;
    const cells = []; for (let y = c.y0; y <= c.y1; y++) for (let x = c.x0; x <= c.x1; x++) if (sl[idx(x, y)] === c.id) cells.push(idx(x, y));
    loose.push(bez(rdp(strokeOf(cells), 1.5), false));
  }
}

// Overlays (rust): their pixels on the parts they grow on, as flat patches drawn after those parts.
const overlays = [];
for (const [mname, m] of Object.entries(P.materials)) {
  if (!m.overlay) continue;
  const refs = m.ref.map(hex), others = m.on.flatMap((n) => P.materials[n].ref.map(hex)); // rust or the plate it grows on
  let om = new Uint8Array(N);
  for (let i = 0; i < N; i++) {
    if (lab[i] < 0 || !m.on.includes(parts[lab[i]].mat)) continue;
    const c = [px[i * 4], px[i * 4 + 1], px[i * 4 + 2]], dm = Math.min(...refs.map((r) => Math.hypot(c[0] - r[0], c[1] - r[1], c[2] - r[2]))), dn = Math.min(...others.map((r) => Math.hypot(c[0] - r[0], c[1] - r[1], c[2] - r[2])));
    if (dm < dn && dm < (m.tolerance || 30)) om[i] = 1;
  }
  // Kept inside its host by the pen's width, so a patch never breaks the line it grows from.
  { const r = Math.round((m.inset ?? .2) * u); om = and(dilate(om, r), erode(lab.map((k) => (k >= 0 && m.on.includes(parts[k].mat) ? 1 : 0)), r)); }
  om = close(open(om, 1), 3);
  const [ol, oc] = components(om);
  for (const c of oc) {
    if (c.n < (m.minArea || 0.0004) * silArea) continue;
    let host = new Map(); for (let y = c.y0; y <= c.y1; y++) for (let x = c.x0; x <= c.x1; x++) { const i = idx(x, y); if (ol[i] === c.id) host.set(lab[i], (host.get(lab[i]) || 0) + 1); }
    const on = [...host].sort((a, b) => b[1] - a[1])[0][0];
    overlays.push({ name: mname, on, d: tracePath(maskOf((i) => ol[i] === c.id, [c.x0, c.y0, c.x1, c.y1]), [c.x0, c.y0, c.x1, c.y1], 1.2, 1) });
  }
}

// The body under everything, every part its own shape with a deep step, overlays after their part, loose strokes on top.
out.ops = [{ op: 'shape', mat: baseName, d: silD, shade: 0, deep: 0, lite: [] }];
for (const p of order) {
  out.ops.push({ op: 'shape', mat: p.mat, d: p.d, shade: p.shade, deep: p.shade ? +(p.shade * .4).toFixed(3) : 0, lite: p.lite || [] });
  for (const o of overlays.filter((o) => o.on === p.k)) out.ops.push({ op: 'flat', mat: o.name, d: o.d });
}
for (const d of loose) out.ops.push({ op: 'line', d, w: 1 });
out.counts = { parts: parts.length, loose: loose.length, overlays: overlays.length, ops: out.ops.length };
fs.writeFileSync(outFile, JSON.stringify(out));

if (debugPng) {
  // Each part in a colour of its own, ink black: to check the segmentation by eye.
  const dbg = Buffer.alloc(N * 4), rnd = (k, s) => (Math.imul((k + 1) * 7919 + s * 104729, 2654435761) >>> 24);
  for (let i = 0; i < N; i++) {
    const q = i * 4; dbg[q + 3] = 255;
    if (!sil[i]) { dbg.set([20, 24, 40], q); continue; }
    if (ink[i]) { dbg.set([0, 0, 0], q); continue; }
    const k = lab0[i]; if (k < 0) { dbg.set([255, 0, 255], q); continue; }
    dbg.set([60 + rnd(k, 1) % 190, 60 + rnd(k, 7) % 190, 60 + rnd(k, 13) % 190], q);
  }
  spawnSync('ffmpeg', ['-loglevel', 'error', '-y', '-f', 'rawvideo', '-pix_fmt', 'rgba', '-s', `${W}x${H}`, '-i', '-', debugPng], { input: dbg });
}
console.log(JSON.stringify({ counts: out.counts, fit: out.fit, parts: out.parts.map((p) => `${p.k}:${p.mat}@${p.depth} n${p.n} [${p.mean}]`) }, null, 0));

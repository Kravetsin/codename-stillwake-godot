// ---------- the backdrop's drawing code: sky things, clouds, land layers ----------
const LW = 2048; // every repeating layer is this many pixels of art across

function bakeSky(ramp, W, rows = 300) {
  const b = new Buf(W, rows, false), cols = ramp.map(C);
  for (let y = 0; y < rows; y++) for (let x = 0; x < W; x++) {
    const t = y / (rows - 1) + (bayer(x, y) - 0.5) * 0.025;
    b.set(x, y, rampS(cols, clamp(t)));
  }
  return b.canvas();
}

function bakeGiant(o) {
  const r = o.r, w = Math.ceil(r * 4.3), h = Math.ceil(r * 2.6), gx = w / 2, gy = h / 2;
  const b = new Buf(w, h, false), cs = Math.cos(o.tilt), sn = Math.sin(o.tilt);
  const bands = o.bands.map(C), shade = C(o.shade);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const dx = x + 0.5 - gx, dy = y + 0.5 - gy, d = Math.hypot(dx, dy);
    const rx = dx * cs - dy * sn, ry = (dx * sn + dy * cs) / 0.22, rd = Math.hypot(rx, ry);
    const inRing = rd > r * 1.32 && rd < r * 1.95 && !(rd > r * 1.55 && rd < r * 1.6);
    const front = o.ring !== false && inRing && (ry > 0 || d > r);
    if (d < r) {
      const band = Math.floor((dy + 6 * vn2(dx + 500, dy + 500, r * 0.35, 3, 4096)) / (r * 0.17)), bi = mod(band, 3);
      const lit = clamp(0.5 + (dx * 0.5 - dy * 0.6) / r * 0.6);
      let c = mix(bands[bi], shade, (1 - lit) * 0.9);
      if (lit < 0.22) c = mix(c, shade, 0.4);
      // the ring's shadow across the disc
      if (o.ringShadow) { const sy = (dx * sn + (dy - r * 0.18) * cs) / 0.22; const sd = Math.hypot(dx * cs - (dy - r * 0.18) * sn, sy); if (sd > r * 1.32 && sd < r * 1.95 && sy < 0) c = mul(c, 0.82); }
      b.set(x, y, c, d > r - 1.2 ? o.alpha * 0.75 : o.alpha);
    }
    if (front) b.set(x, y, rd < r * 1.6 ? o.ringA || '#e8efe8' : o.ringB || '#c4d6dc', o.alpha * (rd < r * 1.6 ? 0.55 : 0.42));
  }
  return b.canvas();
}

function bakeMoon(r, tone, alpha, seed) {
  const s = Math.ceil(r * 2 + 4), b = new Buf(s, s, false), c0 = s / 2;
  ellipseFill(b, c0, c0, r, r, (x, y, dx, dy) => {
    const lit = dx * 0.7 - dy * 0.5 + 0.2;
    let c = lit > 0 ? C(tone) : mix(tone, '#6d86a6', 0.55);
    if (hash(x >> 1, y >> 1, seed) > 0.86) c = mul(c, 0.9);
    b.set(x, y, c, alpha);
  });
  return b.canvas();
}

function bakeCirrus(seed, col, aMax, H = 150) {
  const b = new Buf(LW, H);
  for (let y = 0; y < H; y++) for (let x = 0; x < LW; x++) {
    const n = fbm2(x * 0.35, y * 3, 64, seed, 4, LW * 0.35), band = smooth(0.58, 0.76, n) * smooth(H, H * 0.35, y) * smooth(0, 14, y);
    const q = rampD([0, 0.33, 0.66, 1], band, x, y, 0.8);
    if (q > 0) b.set(x, y, col, q * aMax);
  }
  return b.canvas();
}

function bakeCumulus(seed, count, tones, H = 130, flat = 1) {
  const b = new Buf(LW, H), [lit, mid, sh] = tones.map(C);
  for (let k = 0; k < count; k++) {
    const cx = (k + hash(k, 1, seed) * 0.7) * (LW / count), base = H - 12 - hash(k, 2, seed) * 30;
    const halfW = 26 + hash(k, 3, seed) * 70, R = (9 + hash(k, 4, seed) * 14) * flat, n = Math.max(3, Math.round(halfW / (R * 0.55)));
    const puffs = [];
    for (let i = 0; i <= n; i++) { const t = (i / n) * 2 - 1, r = R * (1 - t * t * 0.55) * (0.75 + hash(i, k, seed + 9) * 0.5); puffs.push({ x: cx + t * halfW * 0.85, y: base - r * 0.55 - (1 - t * t) * R * 0.25, r }); }
    const top = Math.min(...puffs.map((p) => p.y - p.r));
    for (let y = Math.floor(top); y <= base; y++) for (let x = Math.floor(cx - halfW - R); x <= cx + halfW + R; x++) {
      let best = null;
      for (const p of puffs) { const e = p.r - Math.hypot(x + 0.5 - p.x, y + 0.5 - p.y) + (vn2(x, y, 4, seed + k, LW) - 0.5) * 2.5; if (e > 0 && (!best || p.y < best.y)) best = p; }
      if (!best) continue;
      const dx = (x + 0.5 - best.x) / best.r, dy = (y + 0.5 - best.y) / best.r;
      const s = clamp(dx * 0.35 - dy * 0.75 + 0.15 - smooth(base - R * 0.6, base, y) * 0.5);
      b.set(x, y, s > 0.55 ? lit : s > 0.2 ? mid : sh, 0.92);
    }
  }
  return b.canvas();
}

function bakeDust(seed, col, aMax, H = 110) {
  const b = new Buf(LW, H);
  for (let y = 0; y < H; y++) for (let x = 0; x < LW; x++) {
    const n = fbm2(x * 0.12, y * 2.2, 64, seed, 4, LW * 0.12), env = Math.sin((y / H) * Math.PI);
    const band = smooth(0.5, 0.78, n) * env;
    const q = rampD([0, 0.5, 1], band, x, y, 0.9);
    if (q > 0) b.set(x, y, col, q * aMax);
  }
  return b.canvas();
}

function bakeHazeNoise(col) {
  const W = 1024, H = 56, b = new Buf(W, H);
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const n = fbm2(x * 0.5, y * 2, 64, 71, 3, W * 0.5), env = Math.sin((y / H) * Math.PI);
    const q = rampD([0, 0.5, 1], smooth(0.45, 0.75, n) * env, x, y, 0.8);
    if (q > 0) b.set(x, y, col, q * 0.5);
  }
  return b.canvas();
}

// ---------- land: shapes ----------
function profile(shape, x, cell, seed) {
  const f = fbm1(x, cell, seed, 4, LW);
  if (shape === 'mesa') { const q = clamp(f * 1.25 - 0.1) * 3.2, fl = Math.floor(q), fr = q - fl; return clamp((fl + smooth(0.28, 0.72, fr)) / 3.2); }
  if (shape === 'jag') { const saw = 1 - Math.abs(mod(x / (cell / 5), 2) - 1); return clamp(f * 0.75 + saw * vn1(x, cell / 2, seed + 5, LW) * 0.45); }
  if (shape === 'soft') return f * 0.85 + fbm1(x, cell * 2, seed + 3, 2, LW) * 0.15;
  if (shape === 'cone') { // volcanoes: steep cones with a notch at the top, over low rubble
    const n = 5, w = LW / n, k = Math.floor(x / w), cx = (k + 0.3 + hash(k, 1, seed) * 0.4) * w, hw = w * (0.22 + hash(k, 2, seed) * 0.12);
    const t = clamp(1 - Math.abs(x - cx) / hw), cone = Math.pow(t, 1.25) * (0.7 + hash(k, 3, seed) * 0.3) - (t > 0.94 ? (t - 0.94) * 2 : 0);
    return clamp(Math.max(cone, f * 0.35));
  }
  return f;
}

// trees and things on a layer
function mushroomSil(b, g, x, by, size, T, seed, col) {
  // a cluster: one big fat-stemmed cap and up to two small ones leaning out
  const n = 1 + Math.floor(hash(seed, 9, 4) * 3);
  for (let m = 0; m < n; m++) {
    const sz = size * (m ? 0.35 + hash(seed, 30 + m, 4) * 0.25 : 1), ox = m ? (m === 1 ? -1 : 1) * size * (0.25 + hash(seed, 40 + m, 4) * 0.15) : 0;
    const h = sz * (0.5 + hash(seed, 1 + m, 4) * 0.25), sw = Math.max(1.5, sz * 0.11), lean = (hash(seed, 2 + m, 4) - 0.5) * sz * 0.3 + ox * 0.3;
    for (let y = 0; y < h; y++) { const t = y / h, cx = x + ox + lean * t * t, w = sw * (1.25 - t * 0.35); for (let q = -w; q <= w; q++) b.set(cx + q, by - y, q > w - 1.2 ? T.rim2 : T.sil); }
    const cx = x + ox + lean, cy = by - h, rx = sz * (0.36 + hash(seed, 3 + m, 4) * 0.14), ry = rx * 0.62;
    ellipseFill(b, cx, cy, rx, ry, (px, py, dx, dy) => { if (dy > 0.18) return; b.set(px, py, dy < -0.62 ? T.rim : dx > 0.45 && dy < -0.2 ? T.rim2 : T.sil); });
    for (let q = -rx + 1; q < rx - 1; q++) b.set(cx + q, cy + ry * 0.18 + 1, T.shade);
    if (g) {
      for (let q = -rx + 2; q < rx - 2; q++) if (hash(Math.round(q), m, seed) < 0.7) g.add(cx + q, cy + ry * 0.18 + 1, col, 0.7);
      for (let s = 0; s < 2 + sz / 10; s++) g.add(cx + (hash(seed, 10 + s + m * 9, 4) - 0.5) * rx * 1.3, cy - ry * 0.2 - hash(seed, 20 + s + m * 9, 4) * ry * 0.6, col, 0.6);
    }
  }
}
function acaciaSil(b, g, x, by, size, T, seed, glowCol, bare) {
  const R = (k) => hash(seed, k, 77);
  const h = size * 0.62, lean = (R(1) - 0.5) * size * 0.16, w = Math.max(1, Math.round(size / 16));
  line(b, x, by + 2, x + lean, by - h, w, T.sil);
  const tx = x + lean, ty = by - h, nb = R(2) < 0.55 ? 2 : 3;
  for (let i = 0; i < nb; i++) {
    const ang = (-0.95 + (1.9 * i) / (nb - 1)) * (0.75 + R(3 + i) * 0.35), len = size * (0.26 + R(10 + i) * 0.14);
    const ex = tx + Math.sin(ang) * len * 1.25, ey = ty - Math.cos(ang) * len * 0.85;
    line(b, tx, ty, ex, ey, Math.max(1, w - (size > 40 ? 0 : 1)), T.sil);
    if (bare) { // a dead or frozen tree: twigs, no crown
      for (let q = 0; q < 3; q++) { const a2 = ang + (R(30 + i * 3 + q) - 0.5) * 1.4, l2 = len * (0.35 + R(40 + q) * 0.3); line(b, ex, ey, ex + Math.sin(a2) * l2, ey - Math.cos(a2) * l2 * 0.8, 1, q === 0 ? T.rim2 : T.sil); }
      continue;
    }
    const rx = size * (0.17 + R(20 + i) * 0.12), ry = Math.max(2.2, size * 0.075);
    ellipseFill(b, ex, ey - 1, rx, ry, (px, py, dx, dy) => {
      if (vn2(px, py, 2, seed + i, LW) < 0.22 && Math.abs(dx) > 0.7) return;
      b.set(px, py, dy < -0.35 ? T.rim : T.sil);
    });
    for (let fx = Math.floor(ex - rx * 0.85); fx <= ex + rx * 0.85; fx += 2) {
      if (hash(fx, i, seed) < 0.45) continue;
      const fl = 1 + Math.floor(hash(fx, 2, seed) * size * 0.14);
      for (let q = 0; q < fl; q++) b.set(fx, ey + ry - 0.5 + q, T.sil);
      if (g && size > 30 && hash(fx, 3, seed) < 0.12) g.add(fx, ey + ry + fl, glowCol, 0.9);
    }
  }
}
function hoodooSil(b, x, by, size, T, seed) {
  // a pinnacle of layered rock; some keep a cap stone, most are worn to a point
  const h = size * (0.8 + hash(seed, 1, 3) * 0.6), w0 = Math.max(3, size * 0.24), capped = hash(seed, 2, 3) < 0.4;
  for (let y = 0; y < h; y++) {
    const t = y / h, wob = 0.18 * Math.sin(t * 7 + seed) + (vn1(y, 6, seed, 4096) - 0.5) * 0.3;
    const w = w0 * (capped ? 0.75 + wob - (t > 0.8 ? 0.25 : 0) : (1 - t * 0.85) + wob * 0.6), yy = by - y;
    for (let dx = -w; dx <= w; dx++) {
      const band = mod(Math.floor(y / 3 + vn1(x + dx, 8, seed, LW) * 2), 4) === 0;
      b.set(x + dx, yy, dx > w * 0.4 ? T.rim2 : band && T.strata ? T.strata : T.sil);
    }
  }
  if (capped) { const cw = w0 * 1.35, cy = by - h; ellipseFill(b, x + (hash(seed, 3, 3) - 0.5) * 2, cy, cw, Math.max(2, w0 * 0.5), (px, py, dx, dy) => b.set(px, py, dy < -0.3 ? T.rim : T.shade)); }
}
function crystalSil(b, g, x, by, size, T, seed, glows, twinkles) {
  const n = 2 + Math.floor(hash(seed, 1, 5) * 3);
  for (let k = 0; k < n; k++) {
    const off = (hash(seed, k + 2, 5) - 0.5) * size * 0.5, h = size * (0.45 + hash(seed, k + 9, 5) * 0.6), hw = Math.max(1.5, size * (0.06 + hash(seed, k + 13, 5) * 0.06)), tilt = (hash(seed, k + 17, 5) - 0.5) * 0.55;
    const col = glows[k % glows.length];
    for (let py = 0; py < h; py++) {
      const t = py / h, w = hw * (1 - t) + 0.4, cx = x + off + tilt * py;
      for (let px = Math.floor(cx - w); px <= cx + w; px++) b.set(px, by - py, px >= cx ? mix(T.rim, col, 0.25) : T.sil);
    }
    const tipX = x + off + tilt * h, tipY = by - h;
    if (g) { g.add(tipX, tipY, col, 1); g.add(tipX, tipY + 1, col, 0.6); for (let q = 2; q < h * 0.6; q += 2) g.add(Math.round(x + off + tilt * (h - q)), tipY + q, col, 0.25); }
    if (twinkles) twinkles.push([tipX, tipY, col]);
  }
}
function floraSil(b, g, x, by, size, T, seed, col) {
  // a giant bell plant: an arching stem with bells hanging along it, broad blades at its foot
  const side = hash(seed, 1, 9) < 0.5 ? -1 : 1, w = size > 36 ? 2 : 1, rise = size * 0.7, reach = size * (0.45 + hash(seed, 2, 9) * 0.2);
  const pts = [], n = Math.ceil(size * 1.6);
  for (let k = 0; k <= n; k++) {
    const t = k / n, px = x + side * reach * t * t, py = by - rise * Math.sin(Math.min(1, t * 1.25) * Math.PI * 0.5) + (t > 0.8 ? (t - 0.8) * size * 0.9 : 0);
    pts.push([px, py]); for (let q = 0; q < w; q++) b.set(px + q, py, T.sil);
  }
  const bells = 3 + Math.floor(hash(seed, 3, 9) * 3);
  for (let k = 0; k < bells; k++) {
    const [px, py] = pts[Math.floor(n * (0.42 + (0.58 * k) / (bells - 1)))] || pts[n];
    const s = size * (0.06 + 0.05 * (k / bells)), drop = 1 + Math.floor(hash(seed, 10 + k, 9) * size * 0.08);
    for (let q = 0; q < drop; q++) b.set(px, py + q, T.sil);
    const top = py + drop, bh = Math.max(2, s * 1.5);
    for (let y = 0; y < bh; y++) { const ww = Math.max(0.5, s * (0.4 + 0.6 * (y / bh))); for (let q = -ww; q <= ww; q++) b.set(px + q, top + y, y < 1 ? T.rim2 : T.sil); }
    if (g) { for (let q = -s; q <= s; q++) g.add(px + q, top + bh, col, 0.9); g.add(px, top + bh + 1, mix(col, '#ffffff', 0.4), 0.6); }
  }
  for (let s = 0; s < 4; s++) { const dir = (s - 1.5) * 0.7, len = size * (0.16 + hash(seed, 5 + s, 9) * 0.12); for (let q = 0; q <= len; q++) { const t = q / len; b.set(x + dir * len * t * t * 0.9, by - q * (1 - t * 0.3), T.sil); if (t < 0.5) b.set(x + dir * len * t * t * 0.9 + (dir > 0 ? 1 : -1), by - q * (1 - t * 0.3), T.sil); } }
}
function spireSil(b, g, x, by, s, T, pillars) {
  // the real spire's outline (Content.Ruins.Spire, rows 0-10), s pixels a cell
  const rows = [1, 1, 3, 3, 3, 5, 5, 5, 5, 5, 9, 9, 9];
  const top = by - rows.length * s;
  rows.forEach((wc, r) => { for (let yy = 0; yy < s; yy++) for (let xx = -wc * s / 2; xx < wc * s / 2; xx++) b.set(x + xx, top + r * s + yy, xx >= wc * s / 2 - Math.max(1, s / 2) ? T.rim : T.spire); });
  if (g) { const lx = x, ly = top + s / 2; g.add(lx, ly, '#e6fffb', 1); g.add(lx - 1, ly, '#7fe8f0', 0.8); g.add(lx + 1, ly, '#7fe8f0', 0.8); g.add(lx, ly - 1, '#7fe8f0', 0.8); }
  if (pillars) pillars.push({ x, y: top + s / 2, s });
}
function archSil(b, cx, by, span, ah, th, T) {
  const rx = span / 2;
  for (let y = Math.floor(by - ah - th); y <= by; y++) for (let x = Math.floor(cx - rx - th); x <= cx + rx + th; x++) {
    const dx = x + 0.5 - cx, dy = by - (y + 0.5);
    const outer = (dx * dx) / ((rx + th) * (rx + th)) + (dy * dy) / ((ah + th) * (ah + th)) <= 1;
    const inner = (dx * dx) / (rx * rx) + (dy * dy) / (ah * ah) < 1;
    if (outer && !inner) { const nearTop = (dx * dx) / ((rx + th - 1.2) ** 2) + (dy * dy) / ((ah + th - 1.2) ** 2) > 1; b.set(x, y, nearTop && dx > -rx * 0.2 ? T.rim : T.sil); }
  }
}

// a floating rock, as its own sprite so it can bob
function bakeFloater(r, T, Z, seed, glowCol, nearish) {
  const w = Math.ceil(r * 2 + 8), h = Math.ceil(r * 2.4 + 24), b = new Buf(w, h, false), g = new Buf(w, h, false), cx = w / 2, topY = Math.ceil(r * 0.5) + 10;
  for (let x = 0; x < w; x++) {
    const dx = (x + 0.5 - cx) / r; if (Math.abs(dx) >= 1) continue;
    const ty = topY - r * 0.12 * (1 - dx * dx) + (vn1(x, 4, seed, 4096) - 0.5) * 2;
    const spike = hash(x >> 2, 1, seed) * 0.5 + 0.6, by = topY + r * 1.25 * Math.pow(1 - dx * dx, 0.8) * spike;
    for (let y = Math.floor(ty); y < by; y++) {
      const d = y - ty, t = (y - ty) / (by - ty);
      let c = d < 1.5 ? Z.mossC : d < 3 ? mix(Z.mossC, T.sil, 0.5) : t < 0.35 ? (dx > 0.2 ? T.rim2 : T.sil) : t < 0.7 ? T.shade : T.under;
      if (mod(y + Math.floor(vn1(x, 16, seed, 4096) * 3), 5) === 0 && d > 3 && t < 0.7) c = mul(c, 0.92);
      b.set(x, y, c);
    }
  }
  // hanging fronds and pods under it
  for (let x = Math.floor(cx - r * 0.7); x < cx + r * 0.7; x += 2) {
    if (hash(x, 4, seed) < 0.55) continue;
    let y = h - 1; while (y > 0 && !b.has(x, y)) y--; if (!y) continue;
    const fl = 2 + Math.floor(hash(x, 5, seed) * r * 0.35);
    for (let q = 1; q <= fl; q++) b.set(x + (q > fl / 2 && hash(x, 6, seed) < 0.5 ? 1 : 0), y + q, T.shade);
    if (hash(x, 7, seed) < 0.3) g.add(x, y + fl + 1, glowCol, 1);
  }
  if (nearish) b.outline(T.line);
  return { cv: b.canvas(), glow: g.canvas(1.2), w, h, top: topY };
}

// ---------- land: one layer, for a zone ----------
// spec: { kind:'range'|'hills'|'floaters', follows, peak, shape, haze, cell, trees, flora, spires, arches, outline, count, size, ys }
function layerTones(spec, Z, P) {
  const n = clamp((spec.follows - 0.06) / 0.3);
  let base = mix(Z.far, Z.near, n);
  if (P.dusk) base = mix(mul(base, [0.66, 0.55, 0.7]), '#4a3658', 0.22);
  base = mix(base, P.haze, spec.haze * 0.62);
  const rim = mix(base, P.dusk ? mix(Z.rim, '#ffb48a', 0.5) : Z.rim, 0.28 + (1 - n) * 0.12);
  return {
    n, base, rim, rim2: mix(base, rim, 0.5), shade: mul(base, 0.88), under: mul(base, 0.78), sil: mul(base, 0.86),
    low: mix(base, P.haze, 0.45), lower: mix(base, P.haze, 0.75), line: mul(base, 0.62), spire: mix(mul(base, 0.8), '#3a4656', 0.25),
    strata: Z.strata ? mix(base, P.dusk ? mul(Z.strata, [0.7, 0.55, 0.7]) : Z.strata, 0.35 * (0.6 + n * 0.6)) : null,
  };
}

function buildLand(spec, Z, P, seed) {
  const T = layerTones(spec, Z, P);
  if (spec.kind === 'floaters') {
    const sprites = [];
    for (let k = 0; k < spec.count; k++) {
      const r = spec.size[0] + hash(k, 1, seed) * (spec.size[1] - spec.size[0]);
      const f = bakeFloater(r, T, { mossC: mix(T.rim, Z.moss[2], 0.45) }, seed + k * 31, Z.glow[k % Z.glow.length], T.n > 0.5);
      sprites.push({ ...f, x: (k + 0.2 + hash(k, 2, seed) * 0.6) * (LW / spec.count), y: spec.ys[0] + hash(k, 3, seed) * (spec.ys[1] - spec.ys[0]), ph: hash(k, 4, seed) * 6.28, amp: 1.2 + hash(k, 5, seed) * 2 });
    }
    return { kind: 'floaters', sprites, T };
  }
  const below = 110, H = Math.ceil(spec.peak * 1.18 + below + 12), B = H - below;
  const b = new Buf(LW, H), g = new Buf(LW, H), pillars = [], twinkles = [];
  const shape = spec.shape === 'auto' || !spec.shape ? (spec.kind === 'range' ? Z.shapeFar : 'soft') : spec.shape === 'mesa' ? Z.shapeMesa : spec.shape;
  const cell = spec.cell || (spec.kind === 'range' ? 256 : 128);
  const lo = spec.kind === 'range' ? 0.32 : 0.25;
  const top = (x) => B - spec.peak * (lo + (1 - lo) * profile(shape, x, cell, seed));
  const tops = new Float32Array(LW); for (let x = 0; x < LW; x++) tops[x] = top(x);
  const tAt = (x) => tops[mod(Math.round(x), LW)];
  const r1 = new Float32Array(LW), r2 = new Float32Array(LW);
  for (let x = 0; x < LW; x++) { r1[x] = tops[x] + spec.peak * 0.14 + vn1(x, 64, seed + 20, LW) * spec.peak * 0.22; r2[x] = tops[x] + spec.peak * 0.38 + vn1(x, 128, seed + 21, LW) * spec.peak * 0.3; }
  const rAt = (r, x) => r[mod(Math.round(x), LW)];
  for (let x = 0; x < LW; x++) {
    const y0 = tops[x], sl = tAt(x + 3) - tAt(x - 3), s1 = rAt(r1, x + 3) - rAt(r1, x - 3), s2 = rAt(r2, x + 3) - rAt(r2, x - 3);
    for (let y = Math.floor(y0); y < H; y++) {
      const d = y - y0;
      let c = d < 1.3 ? (sl > 0.3 ? T.rim : sl < -0.6 ? T.base : T.rim2) : d < 7 && sl > 1.4 ? T.rim2 : d < 10 && sl < -1.6 ? T.shade : T.base;
      if (spec.kind === 'range' && d > 2) {
        if (y >= r2[x]) c = y - r2[x] < 1 ? (s2 > 0.2 ? T.rim2 : T.base) : bayer(x, y) < 0.35 ? T.shade : T.under;
        else if (y >= r1[x]) c = y - r1[x] < 1 ? (s1 > 0.2 ? T.rim2 : T.base) : y - r1[x] < 6 && s1 > 1 ? T.rim2 : T.shade;
        if (Math.abs(sl) > 2.2 && d > 3 && d < 22 && bayer(x, y) < 0.3 && sl < 0) c = T.shade;
      }
      if (spec.kind === 'hills' && d > 3 && vn2(x, y, 9, seed + 30, LW) > 0.66 - bayer(x, y) * 0.04) c = T.shade;
      if (T.strata && d > 2 && shape !== 'cone') { const sy = y + vn2(x, y, 48, seed + 3, LW) * 6; if (mod(Math.floor(sy), 9) < 1.5) c = T.strata; else if (mod(Math.floor(sy), 9) < 2.5 && bayer(x, y) < 0.5) c = T.strata; }
      if (spec.kind === 'hills' && T.n > 0.55 && d > 2 && hash(x >> 1, y >> 1, seed) > 0.985) c = T.rim2; // a few pebbles of light
      const t = smooth(B - spec.peak * 0.25, B + 50, y);
      c = rampD([c, T.low, T.lower], t, x, y, 0.9);
      b.set(x, y, c);
    }
    if (spec.kind === 'hills' && vn1(x, 6, seed + 40, LW) > 0.62) { const hh = 1 + Math.floor((vn1(x, 6, seed + 40, LW) - 0.62) * 14 * (0.5 + T.n)); for (let q = 1; q <= hh; q++) b.set(x, Math.floor(y0) - q, q === hh ? T.rim2 : T.sil); }
    if (spec.outline && tAt(x) <= tAt(x - 1) + 1 && tAt(x) <= tAt(x + 1) + 1) b.set(x, Math.floor(y0) - 1, T.line);
  }
  const R = (k, s) => hash(k, s, seed + 101);
  // arches
  for (let k = 0; k < (spec.arches || 0); k++) {
    const cx = (k + 0.3 + R(k, 1) * 0.4) * (LW / spec.arches), span = 16 + R(k, 2) * (spec.kind === 'range' ? 22 : 30), ah = span * (0.7 + R(k, 3) * 0.5);
    archSil(b, cx, tAt(cx) + 3, span, ah, Math.max(3, span * 0.22), T);
  }
  // spires with their pillars
  for (let k = 0; k < (spec.spires || 0); k++) {
    const x = Math.round((k + 0.35 + R(k, 4) * 0.3) * (LW / spec.spires));
    let hi = 1e9; for (let q = -6; q <= 6; q++) hi = Math.min(hi, tAt(x + q));
    spireSil(b, g, x, hi + 3, spec.spireScale || 2, T, pillars);
  }
  // standing props from the library, each kind by the biome's weight, placed by the seed
  const tr = spec.props, kinds = Object.entries(Z.props).filter(([, w]) => w > 0), total = kinds.reduce((s, [, w]) => s + w, 0);
  if (tr && total > 0) for (let k = 0; k < tr.n; k++) {
    const x = Math.round((k + 0.15 + R(k, 6) * 0.7) * (LW / tr.n)), size = tr.size[0] + R(k, 7) ** 1.4 * (tr.size[1] - tr.size[0]), by = tAt(x) + 1;
    let pick = R(k, 10) * total, kind = kinds[0][0]; for (const [kk, w] of kinds) { if (pick < w) { kind = kk; break; } pick -= w; }
    const sd = seed + k * 13;
    if (kind === 'acacia') acaciaSil(b, g, x, by, size, T, sd, Z.glow[0]);
    else if (kind === 'bare') acaciaSil(b, null, x, by, size * 0.85, T, sd, null, true);
    else if (kind === 'hoodoo') hoodooSil(b, x, by + 1, size * 0.8, T, sd);
    else if (kind === 'crystal') crystalSil(b, g, x, by + 1, size * 0.85, T, sd, Z.glow, twinkles);
    else if (kind === 'mushroom') mushroomSil(b, g, x, by + 1, size, T, sd, Z.glow[k % Z.glow.length]);
    else if (kind === 'flora') floraSil(b, g, x, by + 1, size * 0.8, T, sd, Z.flora[k % Z.flora.length]);
  }
  // a cone's crater glows and breathes smoke
  const plumes = [];
  if (shape === 'cone' && Z.crater) for (let x = 2; x < LW - 2; x++) if (tops[x] < tops[x - 1] && tops[x] <= tops[x + 1] && tops[x] < B - spec.peak * 0.8) for (let r = 0; r < 2; r++) { let rx = x + (r ? 2 : -2), ry = tops[x] + 2; for (let s = 0; s < spec.peak * 0.5; s++) { rx += (r ? 1 : -1) * (hash(s, r, seed + x) < 0.55 ? 1 : 0); ry = Math.max(ry + 1, tAt(rx) + 1); if (ry > B) break; g.add(rx, ry, s < 6 ? '#ffd27a' : Z.crater, 0.5 * (1 - s / (spec.peak * 0.5))); } }
  if (shape === 'cone' && Z.crater) for (let x = 2; x < LW - 2; x++) {
    if (tops[x] < tops[x - 1] && tops[x] <= tops[x + 1] && tops[x] < B - spec.peak * 0.8) {
      for (let q = -2; q <= 2; q++) { g.add(x + q, tops[x] + 1, Z.crater, 1); g.add(x + q, tops[x] + 2, Z.crater, 0.6); }
      plumes.push({ x, y: tops[x] });
    }
  }
  for (let k = 0; k < (spec.flora || 0); k++) {
    const x = Math.round((k + 0.5 + (R(k, 8) - 0.5) * 0.8) * (LW / spec.flora)), size = (spec.floraSize || [14, 30])[0] + R(k, 9) * ((spec.floraSize || [14, 30])[1] - (spec.floraSize || [14, 30])[0]);
    floraSil(b, g, x, tAt(x) + 1, size, T, seed + k * 7, Z.flora[k % Z.flora.length]);
  }
  if (spec.outline) {
    // a soft line round what stands on the nearest layer, its fill's own dark, never black
    const add = [];
    for (let y = 1; y < B; y++) for (let x = 0; x < LW; x++) if (!b.has(x, y) && (b.has(x + 1, y) || b.has(x - 1, y) || b.has(x, y - 1))) add.push(x, y);
    for (let k = 0; k < add.length; k += 2) b.set(add[k], add[k + 1], T.line);
  }
  return { kind: 'land', cv: b.canvas(), glow: g.canvas(1.3), B, H, pillars, twinkles, plumes, under: T.lower, T };
}

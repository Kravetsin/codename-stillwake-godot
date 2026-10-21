// ---------- under the ground: a backdrop a depth band, seen where a great hall has no back wall ----------
const BANDS = {
  cold: { key: 'cold', name: 'Холодные пещеры', depth: '0–25 % глубины', bg: '#070b12', rock: ['#101822', '#16202c', '#1d2a38'], rim: '#4f6f8f', glow: '#8fd0ff', glow2: '#c8ecff', haze: [143, 200, 255], tint: [0.86, 0.92, 1.04], amb: '#5a6270', fx: 'drips' },
  garden: { key: 'garden', name: 'Бирюзовый сад', depth: '25–60 %', bg: '#051112', rock: ['#0b1a1c', '#102426', '#163032'], rim: '#2f7a72', glow: '#7fe8d4', glow2: '#b49aff', haze: [90, 220, 200], tint: [0.92, 0.98, 0.98], amb: '#56625f', fx: 'spores' },
  amber: { key: 'amber', name: 'Янтарная глубь', depth: '60–85 %', bg: '#100a06', rock: ['#1a110b', '#22170f', '#2c1e14'], rim: '#8a5a2a', glow: '#ffc46a', glow2: '#ff8aa8', haze: [255, 180, 100], tint: [1.04, 0.94, 0.84], amb: '#625850', fx: 'dust' },
  lava: { key: 'lava', name: 'Лавовое ядро', depth: '85–100 %, над бедроком', bg: '#120504', rock: ['#1a0807', '#240c09', '#30110c'], rim: '#a8401c', glow: '#ff8a3a', glow2: '#ffd27a', haze: [255, 110, 50], tint: [1.04, 0.8, 0.72], amb: '#664c40', fx: 'embers' },
};
const UH = 560, UB = 280; // a cave layer's height and the row that lies at the hall's middle

function caveLayer(band, idx) {
  const b = new Buf(LW, UH), g = new Buf(LW, UH), seed = 300 + idx * 41 + band.key.length * 7;
  const tone = C(band.rock[idx]), rim = mix(tone, band.rim, 0.5 - idx * 0.08), dark = mul(tone, 0.75);
  const open = [235, 180, 130][idx];
  const ceil = (x) => UB - open * (0.55 + 0.5 * fbm1(x, 256 >> idx, seed, 4, LW));
  const floor = (x) => UB + open * (0.42 + 0.42 * fbm1(x, 256 >> idx, seed + 9, 4, LW));
  const cs = new Float32Array(LW), fs = new Float32Array(LW);
  for (let x = 0; x < LW; x++) { cs[x] = ceil(x); fs[x] = floor(x); }
  const at = (a, x) => a[mod(Math.round(x), LW)];
  for (let x = 0; x < LW; x++) for (let y = 0; y < UH; y++) {
    const c0 = cs[x], f0 = fs[x];
    if (y > c0 && y < f0) continue;
    const d = y <= c0 ? c0 - y : y - f0;
    let c = d < 1.5 ? rim : d < 4 && bayer(x, y) < 0.5 ? mix(tone, rim, 0.4) : tone;
    const sy = y + vn2(x, y, 40, seed, LW) * 8; if (mod(Math.floor(sy), 11) < 1) c = dark;
    if (hash(x >> 2, y >> 2, seed) > 0.992) c = mix(tone, rim, 0.3);
    b.set(x, y, c);
  }
  const R = (k, s) => hash(k, s, seed);
  const k = band.key;
  if (idx === 0) {
    // faint veins through the far rock
    for (let v = 0; v < 26; v++) {
      let x = R(v, 1) * LW, y = R(v, 2) < 0.5 ? at(cs, x) - 10 - R(v, 3) * 80 : at(fs, x) + 10 + R(v, 3) * 80, a = R(v, 4) * 6.28;
      for (let s = 0; s < 70; s++) { a += (hash(v, s, seed) - 0.5) * 0.9; x += Math.cos(a) * 1.4; y += Math.sin(a) * 0.8; if (!(y > at(cs, x) && y < at(fs, x))) g.add(x, y, s % 3 ? band.glow : band.glow2, 0.35); }
    }
    if (k === 'lava') for (let v = 0; v < 7; v++) { const x = (v + R(v, 5)) * (LW / 7), y0 = at(cs, x), y1 = at(fs, x); for (let y = y0 - 4; y < y1 + 2; y++) { const w = 1 + (y > y1 - 12 ? 1 : 0); for (let q = -w; q <= w; q++) g.add(x + q + Math.round(Math.sin(y * 0.05 + v) * 1.5), y, q === 0 ? '#ffd27a' : '#ff7a2a', 0.8); } }
    if (k === 'amber') for (let v = 0; v < 3; v++) { // a geode's mouth, lined with points
      const cx = (v + 0.5) * (LW / 3), cy = UB - 20, rx = 60 + R(v, 6) * 40, ry = 40 + R(v, 7) * 20;
      for (let a = 0; a < 6.28; a += 0.02) { const x = cx + Math.cos(a) * rx, y = cy + Math.sin(a) * ry; for (let q = 0; q < 12; q++) b.set(x + Math.cos(a) * q, y + Math.sin(a) * q, q < 2 ? rim : q < 5 ? mix(tone, rim, 0.3) : tone); if (hash(Math.round(a * 30), v, seed) < 0.35) { const tl = 3 + hash(Math.round(a * 30), v, 9) * 9, c = hash(v, Math.round(a * 30), 3) < 0.7 ? band.glow : band.glow2; for (let q = 0; q < tl; q++) { b.set(x - Math.cos(a) * q, y - Math.sin(a) * q, mix(c, tone, 0.45)); g.add(x - Math.cos(a) * q, y - Math.sin(a) * q, c, 0.5 * (1 - q / tl)); } g.add(x - Math.cos(a) * tl, y - Math.sin(a) * tl, c, 1); } }
    }
  }
  if (idx === 1) {
    if (k === 'cold') for (let r = 0; r < 18; r++) { // giant roots from the ceiling
      let x = (r + R(r, 1)) * (LW / 18), y = at(cs, x) + 1, w = 6 + R(r, 2) * 7, a = Math.PI / 2 + (R(r, 3) - 0.5) * 0.6; const len = 70 + R(r, 4) * 160;
      for (let s = 0; s < len; s++) { a += (hash(r, s, seed) - 0.5) * 0.12; x += Math.cos(a); y += Math.sin(a); const ww = w * (1 - 0.75 * s / len) + 0.6; for (let q = -ww; q <= ww; q++) b.set(x + q, y, q > ww - 1.4 ? rim : q < -ww + 1 ? mul(tone, 0.6) : mix(tone, rim, 0.22)); if (s > 20 && hash(r, s, seed + 1) < 0.03) { let tx = x, ty = y, ta = a + (hash(r, s, 2) < 0.5 ? -0.8 : 0.8); for (let q = 0; q < 26; q++) { ta += (hash(r, q + s, 4) - 0.5) * 0.3; tx += Math.cos(ta); ty += Math.sin(ta); b.set(tx, ty, dark); } } }
      if (R(r, 9) < 0.5) g.add(x, y + 1, band.glow2, 0.7);
    }
    if (k === 'garden') for (let m = 0; m < 9; m++) { // giant glowcaps
      const x = (m + 0.2 + R(m, 1) * 0.6) * (LW / 9), base = at(fs, x) + 2, h = 60 + R(m, 2) * 110, rx = 20 + R(m, 3) * 30, ry = rx * 0.42, sw = 3 + rx * 0.12, lean = (R(m, 4) - 0.5) * 20;
      for (let y = 0; y < h; y++) { const cx = x + lean * (y / h) ** 2; for (let q = -sw; q <= sw; q++) b.set(cx + q, base - y, q > sw - 1.5 ? rim : dark); }
      const cx = x + lean, cy = base - h;
      ellipseFill(b, cx, cy, rx, ry, (px, py, dx, dy) => { if (dy > 0.25) return; b.set(px, py, dy < -0.6 ? rim : tone); });
      for (let q = -rx + 2; q < rx - 2; q++) { const c = R(m, 5) < 0.6 ? band.glow : band.glow2; g.add(cx + q, cy + ry * 0.25 + 1, c, 0.9); if (hash(m, Math.round(q), 1) < 0.5) g.add(cx + q, cy + ry * 0.25 + 2, c, 0.5); }
      for (let s = 0; s < 6; s++) g.add(cx + (R(m, 10 + s) - 0.5) * rx * 1.4, cy - ry * 0.3 + R(m, 20 + s) * ry * 0.5, '#e6fff8', 0.6);
    }
    if (k === 'amber') for (let c = 0; c < 12; c++) { // crystal columns
      const x = (c + R(c, 1)) * (LW / 12), base = at(fs, x) + 3, T = { sil: dark, rim: mix(rim, band.glow, 0.3) };
      crystalSil(b, g, x, base, 90 + R(c, 2) * 140, T, seed + c, [band.glow, band.glow2, band.glow], null);
      if (R(c, 3) < 0.5) { const y0 = at(cs, x + 40) - 2; for (let s = 0; s < 3; s++) { const hx = x + 40 + s * 7, hh = 20 + R(c, 4 + s) * 40; for (let y = 0; y < hh; y++) for (let q = -(3 * (1 - y / hh)); q <= 3 * (1 - y / hh); q++) b.set(hx + q, y0 + y, q > 0 ? T.rim : dark); g.add(hx, y0 + hh, band.glow, 0.8); } }
    }
    if (k === 'lava') { // basalt columns and the lava's surface
      let x = 0; while (x < LW) { const w = 5 + Math.floor(hash(x, 1, seed) * 4), cl = smooth(0.42, 0.6, vn1(x, 256, seed + 5, LW)); if (cl <= 0) { x += w; continue; } const top = at(fs, x) - 6 - cl * (20 + fbm1(x, 96, seed + 6, 3, LW) * 120) - hash(x, 2, seed) * 6; for (let px = x; px < x + w; px++) { const e = Math.min(px - x, x + w - 1 - px), tY = top + (e < 1 ? 2 : 0); for (let y = Math.floor(tY); y < UH; y++) { const crack = mod(Math.floor(y + hash(x, 3, seed) * 20), 17) === 0; b.set(px, y, px === x ? mul(tone, 0.7) : y - tY < 1.5 ? rim : crack ? mul(tone, 0.8) : px - x > w - 2 ? mix(tone, rim, 0.25) : tone); } } x += w; }
      for (let px = 0; px < LW; px++) { const ly = UB + 150 + Math.sin(px * 0.02) * 3; for (let y = Math.floor(ly); y < UH; y++) { const d = y - ly; b.set(px, y, d < 1 ? '#ffe2a0' : d < 3 ? '#ffa040' : d < 8 ? '#d0501c' : '#7a200e'); } g.add(px, ly, '#ffd27a', 1); g.add(px, ly + 1, '#ff8a3a', 0.8); }
    }
  }
  if (idx === 2) {
    const T = { sil: dark, rim };
    if (k === 'cold') for (let c = 0; c < 22; c++) { const x = (c + R(c, 1)) * (LW / 22); crystalSil(b, g, x, at(fs, x) + 2, 18 + R(c, 2) * 34, T, seed + c, [band.glow, band.glow2], null); }
    if (k === 'garden') for (let th = 0; th < 60; th++) { const x = (th + R(th, 1)) * (LW / 60), y0 = at(cs, x), len = 20 + R(th, 2) * 90; for (let y = 0; y < len; y++) b.set(x + Math.round(Math.sin(y * 0.08 + th) * 1.2), y0 + y, dark); for (let q = 6; q < len; q += 9 + Math.floor(R(th, q) * 8)) g.add(x + Math.round(Math.sin(q * 0.08 + th) * 1.2), y0 + q, R(th, 3) < 0.7 ? band.glow : band.glow2, 0.8); }
    if (k === 'amber') for (let h = 0; h < 4; h++) { // far homes of the arc makers: alloy, a teal lamp
      const x = (h + 0.5) * (LW / 4), base = at(fs, x) + 2, w = 40 + R(h, 1) * 30, hh = 28 + R(h, 2) * 18, al = mix('#2a3440', tone, 0.4);
      for (let px = x - w / 2; px < x + w / 2; px++) for (let y = base - hh; y < base; y++) { const ax = (px - x) / (w * 0.18), ay = (base - y) / (hh * 0.6); const door = ax * ax + ay * ay < 1, win = !door && mod(Math.floor(px - x + w / 2), 12) < 3 && mod(Math.floor(base - y), 14) > 8 && base - y < hh - 4; b.set(px, y, door ? mul(al, 0.5) : y < base - hh + 2 || px > x + w / 2 - 2 ? mix(al, '#7fa0b0', 0.45) : al); if (win) g.add(px, y, '#7fe8f0', 0.55); }
      for (let q = -2; q <= 2; q++) g.add(x + q, base - hh * 0.75, '#7fe8f0', 0.9); g.add(x, base - hh * 0.75 - 1, '#e6fffb', 1);
    }
    if (k === 'lava') for (let c = 0; c < 40; c++) { // the heat's cracks in the near rock
      let x = (c + R(c, 1)) * (LW / 40), y = at(fs, x) + 6 + R(c, 2) * 40, a = -Math.PI / 2 + (R(c, 3) - 0.5); for (let s = 0; s < 30; s++) { a += (hash(c, s, seed) - 0.5) * 0.8; x += Math.cos(a); y += Math.sin(a) * 0.7; if (y > at(fs, x) + 2) g.add(x, y, s % 4 ? '#ff7a2a' : '#ffd27a', 0.8); }
    }
  }
  return { cv: b.canvas(), glow: g.canvas(idx === 0 ? 2 : 1.3), topCol: tone, botCol: tone };
}

// the hall in front: our stone tiles, back walls kept round its rim, its middle open to the deep
const HW = 110, HHt = 44;
function buildHall(band) {
  const open = new Uint8Array(HW * HHt);
  for (let cy = 0; cy < HHt; cy++) for (let cx = 0; cx < HW; cx++) {
    const end = Math.max(0, Math.abs(cx - 55) - 36);
    const fl = 35 + Math.round((fbm1(cx, 32, 61, 2, 4096) - 0.5) * 5) - Math.round(end * 0.7), ce = 6 + Math.round((fbm1(cx, 32, 62, 2, 4096) - 0.5) * 7) + Math.round(end * 0.8);
    if (cx > 4 && cx < HW - 5 && cy > ce && cy < fl) open[cy * HW + cx] = 1;
  }
  const isOpen = (x, y) => x >= 0 && y >= 0 && x < HW && y < HHt && open[y * HW + x] === 1;
  const win = new Uint8Array(HW * HHt);
  for (let cy = 0; cy < HHt; cy++) for (let cx = 0; cx < HW; cx++) {
    if (!isOpen(cx, cy)) continue;
    const rimW = 1 + Math.round(vn2(cx, cy, 6, 63, 4096) * 2); let near = false;
    for (let dy = -rimW; dy <= rimW && !near; dy++) for (let dx = -rimW; dx <= rimW; dx++) if (!isOpen(cx + dx, cy + dy)) { near = true; break; }
    win[cy * HW + cx] = near ? 0 : 1;
  }
  const W = HW * CELL, H = HHt * CELL, b = new Buf(W, H, false), g = new Buf(W, H, false), lights = [];
  const stone = tex('wall_stone'), back = tex('back_stone'), tint = band.tint;
  const openPx = (px, py) => isOpen(Math.floor(px / CELL), Math.floor(py / CELL));
  for (let py = 0; py < H; py++) for (let px = 0; px < W; px++) {
    const cx = Math.floor(px / CELL), cy = Math.floor(py / CELL);
    if (isOpen(cx, cy)) { if (!win[cy * HW + cx]) { const lx = mod(px, CELL), ly = mod(py, CELL), edge = (lx === 0 && win[cy * HW + cx - 1]) || (lx === CELL - 1 && win[cy * HW + cx + 1]) || (ly === 0 && win[(cy - 1) * HW + cx]) || (ly === CELL - 1 && win[(cy + 1) * HW + cx]); b.set(px, py, edge ? mul(mul([120, 128, 140], tint), 0.9) : mul(mul(texAt(back, px, py), tint), 0.8)); } continue; }
    let c = mul(texAt(stone, px, py), tint);
    if (openPx(px - 1, py) || openPx(px + 1, py) || openPx(px, py + 1)) c = [11, 13, 18];
    else if (openPx(px, py - 1)) c = mix(c, '#9aa2b0', 0.5);
    else if (openPx(px, py - 2) || openPx(px - 2, py) || openPx(px + 2, py)) c = mul(c, 1.18);
    b.set(px, py, c);
  }
  // what grows, by band, with its light
  for (let cx = 6; cx < HW - 6; cx++) for (let cy = 1; cy < HHt - 1; cy++) {
    if (!isOpen(cx, cy)) continue;
    const floor = !isOpen(cx, cy + 1), ceil = !isOpen(cx, cy - 1), h = hash(cx, cy, 71), x = cx * CELL + 8;
    if (floor && h < 0.3) {
      const y = (cy + 1) * CELL - 1, k = band.key;
      if (k === 'garden') { const col = h < 0.18 ? band.glow : band.glow2, sh = 3 + Math.floor(h * 20); for (let q = 0; q < sh; q++) b.set(x, y - q, '#3a5a54'); for (let q = -3; q <= 3; q++) { b.set(x + q, y - sh - 1, mix(col, '#163032', 0.35)); g.add(x + q, y - sh, col, 0.9); } b.set(x, y - sh - 2, mix(col, '#163032', 0.35)); lights.push([x, y - sh, col]); }
      else { const col = k === 'cold' ? band.glow2 : k === 'amber' ? (h < 0.15 ? band.glow : band.glow2) : '#ff7a3a'; crystalSil(b, g, x, y + 1, 8 + h * 30, { sil: mul(col, 0.45), rim: mix(col, '#ffffff', 0.3) }, cx * 7 + cy, [col], null); lights.push([x, y - 6, col]); }
    }
    if (ceil && h > 0.78) {
      const y = cy * CELL;
      if (band.key === 'garden') { const len = 8 + Math.floor((h - 0.78) * 120); for (let q = 0; q < len; q++) b.set(x + (q > len / 2 ? 1 : 0), y + q, '#24403c'); g.add(x + 1, y + len, band.glow, 1); lights.push([x, y + len, band.glow]); }
      else { const len = 6 + Math.floor((h - 0.78) * 60); for (let q = 0; q < len; q++) { const w = Math.round(2 * (1 - q / len)); for (let dx = -w; dx <= w; dx++) b.set(x + dx, y + q, mul(mul([92, 98, 112], tint), 0.8)); } if (band.key !== 'amber') g.add(x, y + len, band.key === 'lava' ? '#ff9a4a' : band.glow2, 0.7); }
    }
  }
  return { cv: b.canvas(), glow: g.canvas(1.2), lights, W, H, floorAt: (cx) => { let y = 20; while (y < HHt && isOpen(cx, y)) y++; return y * CELL; } };
}

class UnderView {
  constructor(el) {
    this.el = el; this.cv = el.querySelector('canvas.stage'); this.ctx = this.cv.getContext('2d');
    this.W = 960; this.H = 540; this.cv.width = this.W; this.cv.height = this.H;
    this.band = 'garden'; this.cache = {}; this.keys = {}; this.cam = { x: 55 * CELL, y: 22 * CELL }; this.t0 = performance.now(); this.tFix = null; this.visible = true; this.gray = false;
    this.tmp = [mkCanvas(this.W, this.H), mkCanvas(this.W, this.H), mkCanvas(this.W, this.H)];
    this.parts = Array.from({ length: 70 }, (_, k) => k);
  }
  built() {
    const k = this.band; if (!this.cache[k]) { const band = BANDS[k]; this.cache[k] = { band, layers: [0, 1, 2].map((i) => caveLayer(band, i)), hall: buildHall(band) }; }
    return this.cache[k];
  }
  time() { return this.tFix != null ? this.tFix : (performance.now() - this.t0) / 1000; }
  step(dt) {
    const dx = (this.keys.ArrowRight ? 1 : 0) - (this.keys.ArrowLeft ? 1 : 0), dy = (this.keys.ArrowDown ? 1 : 0) - (this.keys.ArrowUp ? 1 : 0), sp = (this.keys.Shift ? 2.2 : 1) * 14 * CELL;
    this.cam.x = clamp(this.cam.x + dx * sp * dt, this.W / 2, HW * CELL - this.W / 2); this.cam.y = clamp(this.cam.y + dy * sp * dt, this.H / 2, HHt * CELL - this.H / 2);
  }
  draw() {
    const { ctx, W, H } = this, t = this.time(), B = this.built(), band = B.band, cx = this.cam.x, cy = this.cam.y, refY = 21 * CELL;
    ctx.imageSmoothingEnabled = false; ctx.globalCompositeOperation = 'source-over'; ctx.globalAlpha = 1;
    ctx.fillStyle = band.bg; ctx.fillRect(0, 0, W, H);
    // the deep's own faint light
    const hz = band.haze; ctx.globalCompositeOperation = 'lighter';
    if (band.key === 'lava') { const g = ctx.createLinearGradient(0, H, 0, H * 0.2); g.addColorStop(0, css(hz, 0.42 + 0.06 * Math.sin(t * 0.8))); g.addColorStop(1, css(hz, 0)); ctx.fillStyle = g; ctx.fillRect(0, 0, W, H); }
    else { const g = ctx.createRadialGradient(W * 0.5 - cx * 0.03 + 30, H * 0.55, 10, W * 0.5, H * 0.55, W * 0.7); g.addColorStop(0, css(hz, band.key === 'cold' ? 0.16 : 0.2)); g.addColorStop(1, css(hz, 0)); ctx.fillStyle = g; ctx.fillRect(0, 0, W, H); }
    ctx.globalCompositeOperation = 'source-over';
    const fol = [0.12, 0.22, 0.34];
    B.layers.forEach((L, i) => {
      const f = fol[i], y = Math.round(H / 2 + (refY - cy) * f - UB), off = cx * f - W / 2;
      if (band.key === 'lava' && i === 0) { for (let s = 0; s < UH; s += 8) { const o = Math.round(Math.sin(t * 2.1 + s * 0.09) * 1.2); drawWrapStrip(ctx, L.cv, off + o, y, W, s, 8); } }
      else drawWrap(ctx, L.cv, off, y, W);
      ctx.fillStyle = css(L.topCol); ctx.fillRect(0, 0, W, Math.max(0, y + 1)); ctx.fillRect(0, y + UH - 1, W, H);
      ctx.globalCompositeOperation = 'lighter'; drawWrap(ctx, L.glow, off, y, W, 0.95 + 0.05 * Math.sin(t * 0.7 + i)); ctx.globalCompositeOperation = 'source-over';
      if (i === 1) this.particles(ctx, W, H, t, band, off);
    });
    // the hall: lit by the helmet and what grows, its back walls round the rim
    const [fg, mask, lm] = this.tmp, fc = fg.getContext('2d'), mc = mask.getContext('2d'), lc = lm.getContext('2d');
    fc.imageSmoothingEnabled = false; fc.clearRect(0, 0, W, H);
    const wl = cx - W / 2, wt = cy - H / 2;
    fc.drawImage(B.hall.cv, Math.round(-wl), Math.round(-wt));
    const hx = 50 * CELL + 8, hy = B.hall.floorAt(50);
    fc.drawImage(HERO, Math.round(hx - 8 - wl), Math.round(hy - 44 - wt));
    mc.clearRect(0, 0, W, H); mc.drawImage(fg, 0, 0);
    lc.globalCompositeOperation = 'source-over'; lc.fillStyle = band.amb; lc.fillRect(0, 0, W, H); lc.globalCompositeOperation = 'lighter';
    const light = (x, y, r, c, a) => { const g = lc.createRadialGradient(x, y, 0, x, y, r); g.addColorStop(0, css(c, a)); g.addColorStop(1, css(c, 0)); lc.fillStyle = g; lc.fillRect(x - r, y - r, r * 2, r * 2); };
    light(hx - wl + 50, hy - 36 - wt, 230, [235, 228, 210], 0.8); light(hx - wl, hy - 24 - wt, 60, [255, 250, 235], 0.5);
    for (const [x, y, c] of B.hall.lights) { const X = x - wl, Y = y - wt; if (X > -60 && X < W + 60 && Y > -60 && Y < H + 60) light(X, Y, 44, C(c), 0.55); }
    fc.globalCompositeOperation = 'multiply'; fc.drawImage(lm, 0, 0); fc.globalCompositeOperation = 'destination-in'; fc.drawImage(mask, 0, 0); fc.globalCompositeOperation = 'source-over';
    ctx.drawImage(fg, 0, 0);
    ctx.globalCompositeOperation = 'lighter'; ctx.drawImage(B.hall.glow, Math.round(-wl), Math.round(-wt));
    ctx.fillStyle = css('#fff6d8', 0.9); ctx.fillRect(Math.round(hx - wl + 4), Math.round(hy - 39 - wt), 1, 1);
    ctx.globalCompositeOperation = 'source-over';
    if (this.gray !== this._gray) { this.cv.style.filter = this.gray ? 'grayscale(1)' : 'none'; this._gray = this.gray; }
  }
  particles(ctx, W, H, t, band, off) {
    ctx.globalCompositeOperation = 'lighter';
    for (const k of this.parts) {
      const sp = band.fx === 'embers' ? 0.12 : band.fx === 'drips' ? 0.5 : 0.05, life = mod(t * sp + k / 70, 1), cyc = Math.floor(t * sp + k / 70);
      let x = mod(hash(k, cyc, 5) * LW - off * 0.6, LW), y, a, col = band.glow;
      if (x > W) continue;
      if (band.fx === 'drips') { y = H * 0.12 + hash(k, cyc, 6) * H * 0.2 + life * life * H * 0.7; a = 0.6 * (1 - life); col = band.glow2; }
      else if (band.fx === 'embers') { y = H * (1 - life * 0.9); x += Math.sin(t + k) * 8 * life; a = (1 - life) * 0.9; col = life < 0.3 ? '#ffd27a' : '#ff7a2a'; }
      else if (band.fx === 'spores') { y = H * (0.9 - life * 0.8); x += Math.sin(t * 0.5 + k) * 10; a = Math.sin(life * Math.PI) * 0.7; col = hash(k, 1, 1) < 0.7 ? band.glow : band.glow2; }
      else { y = H * (0.2 + hash(k, cyc, 7) * 0.6) + Math.sin(t * 0.3 + k) * 6; x += life * 30; a = Math.sin(life * Math.PI) * 0.45; }
      ctx.fillStyle = css(col, a); ctx.fillRect(Math.round(x), Math.round(y), 1, band.fx === 'drips' ? 2 : 1);
      ctx.fillStyle = css(col, a * 0.25); ctx.fillRect(Math.round(x) - 1, Math.round(y), 3, 1);
    }
    ctx.globalCompositeOperation = 'source-over';
  }
}
function drawWrapStrip(ctx, img, sx, dy, W, sy, sh) {
  const L = img.width; let s = mod(Math.round(sx), L), x = 0;
  while (x < W) { const w = Math.min(L - s, W - x); ctx.drawImage(img, s, sy, w, sh, x, dy + sy, w, sh); x += w; s = 0; }
}

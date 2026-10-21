// ---------- the ground in front: our tiles, moss, plants, a spire and the hero ----------
const ROW0 = 132, ROW1 = 184, CHW = 64;
function spireCell(cx, cy) {
  for (const s of SPIRES) if (cx >= s.x && cx < s.x + 9 && cy >= s.top && cy < s.top + SPIRE.length) return { ch: SPIRE[cy - s.top][cx - s.x], s };
  return null;
}
function groundSolid(px, py) {
  const cx = Math.floor(px / CELL), cy = Math.floor(py / CELL);
  if (cx < 1 || cx >= WORLD_W - 1) return cy >= SURF;
  const t = TOPS[cx]; if (cy < t) return false; if (cy > t) return true;
  const l = TOPS[cx - 1], r = TOPS[cx + 1], lLow = l > t, rLow = r > t, lx = mod(px, CELL), ly = mod(py, CELL);
  if (lLow && !rLow) return lx + ly >= 15; if (rLow && !lLow) return lx <= ly; return true;
}
function surfacePx(px) { let y = (TOPS[clamp(Math.floor(px / CELL), 0, WORLD_W - 1)] - 1) * CELL; while (!groundSolid(px, y) && y < ROW1 * CELL) y++; return y; }

// recolour: a prop's leaf pixels re-mapped onto a biome's ramp by their brightness rank — indexed tones
const RECOL = {};
function recolour(name, biomeKey) {
  const Bm = BIOMES[biomeKey], key = name + '|' + biomeKey;
  if (RECOL[key]) return RECOL[key];
  const t = tex(name), glow = /_glow$/.test(name);
  if (!glow && !Bm.leaf) return (RECOL[key] = IMG[name]);
  const cv = mkCanvas(t.w, t.h), c = cv.getContext('2d'), out = c.createImageData(t.w, t.h), d = t.d, o = out.data;
  if (glow) {
    const col = C(Bm.glow[0]);
    for (let i = 0; i < d.length; i += 4) { const v = Math.max(d[i], d[i + 1], d[i + 2]) / 255; o[i] = col[0] * v + (255 - col[0]) * v * v * 0.5; o[i + 1] = col[1] * v + (255 - col[1]) * v * v * 0.5; o[i + 2] = col[2] * v + (255 - col[2]) * v * v * 0.5; o[i + 3] = d[i + 3]; }
  } else {
    const lums = [];
    const leafy = (i) => d[i + 3] > 0 && d[i + 1] > d[i] + 6 && d[i + 1] >= d[i + 2] - 12;
    for (let i = 0; i < d.length; i += 4) if (leafy(i)) lums.push(d[i] * 0.3 + d[i + 1] * 0.59 + d[i + 2] * 0.11);
    lums.sort((a, b) => a - b);
    const q = [0.12, 0.35, 0.62, 0.88].map((f) => lums[Math.floor(f * (lums.length - 1))] || 0), ramp = Bm.leaf.map(C);
    for (let i = 0; i < d.length; i += 4) {
      if (leafy(i)) { const l = d[i] * 0.3 + d[i + 1] * 0.59 + d[i + 2] * 0.11; let k = 0; while (k < 4 && l > q[k]) k++; const c2 = ramp[k]; o[i] = c2[0]; o[i + 1] = c2[1]; o[i + 2] = c2[2]; }
      else { const sl = Bm.soil; o[i] = d[i] * sl[0]; o[i + 1] = d[i + 1] * sl[1]; o[i + 2] = d[i + 2] * sl[2]; }
      o[i + 3] = d[i + 3];
    }
  }
  c.putImageData(out, 0, 0);
  return (RECOL[key] = cv);
}

const chunkCache = new Map();
function chunk(P, ix) {
  const key = P.id + '#' + ix;
  if (chunkCache.has(key)) { const c = chunkCache.get(key); chunkCache.delete(key); chunkCache.set(key, c); return c; }
  const W = CHW * CELL, H = (ROW1 - ROW0) * CELL, x0 = ix * W, y0 = ROW0 * CELL, b = new Buf(W, H, false), g = new Buf(W, H, false);
  const soil = tex('wall_soil'), stone = tex('wall_stone'), alloy = tex('wall_alloy'), backA = tex('back_alloy');
  const surf = new Int32Array(W); for (let x = 0; x < W; x++) surf[x] = surfacePx(x0 + x);
  const biomeCol = [], mossCol = [];
  for (let x = 0; x < W; x++) { const Bm = biomeAt(P, Math.floor((x0 + x) / CELL)); biomeCol[x] = Bm; mossCol[x] = Bm.moss.map(C); }
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const px = x0 + x, py = y0 + y, cx = Math.floor(px / CELL), cy = Math.floor(py / CELL), sc = spireCell(cx, cy);
    if (sc && sc.ch !== ' ') {
      const anchor = (sc.s.top + 10) * CELL, k = py < anchor ? 1 : Math.pow(0.72, (py - anchor) / CELL);
      if (sc.ch === '#' || sc.ch === 's') {
        const t = sc.ch === '#' ? alloy : soil; let c = texAt(t, px, py);
        const air = (qx, qy) => { const s2 = spireCell(Math.floor(qx / CELL), Math.floor(qy / CELL)); return s2 ? s2.ch === ' ' || s2.ch === 'l' : !groundSolid(qx, qy); };
        if (air(px - 1, py) || air(px + 1, py) || air(px, py - 1)) c = [26, 31, 38];
        b.set(x, y, mul(c, k));
      } else if (sc.ch === '.') b.set(x, y, mul(texAt(backA, px, py), 0.5 * k));
      continue;
    }
    if (!groundSolid(px, py)) continue;
    const M = mossCol[x], sl = biomeCol[x].soil, d = py - surf[x], rockLine = 5 * CELL + vn1(px, 40, 21, 1 << 20) * 26;
    let c = d > rockLine ? texAt(stone, px, py) : mul(texAt(soil, px, py), sl);
    const mossD = 3 + Math.round(vn1(px, 5, 8, 1 << 20) * 3) + (hash(px, 3, 9) < 0.15 ? 2 : 0);
    if (d < mossD) c = d === 0 ? M[1] : d === 1 ? M[2] : d < mossD - 1 ? M[3] : M[4];
    else if (d < mossD + 3) c = mul(c, 0.72);
    const air = !groundSolid(px - 1, py) || !groundSolid(px + 1, py);
    if (air && d > 1) c = [36, 25, 15];
    else if (d === 0 && !groundSolid(px, py - 1)) c = M[0];
    b.set(x, y, mul(c, Math.pow(0.72, Math.max(0, d) / CELL)));
  }
  for (let x = 0; x < W; x++) {
    const px = x0 + x, sy = surf[x] - y0 - 1; if (sy < 0 || hash(px, 1, 30) < 0.42) continue;
    if (spireCell(Math.floor(px / CELL), Math.floor((surf[x] - 1) / CELL))) continue;
    const M = mossCol[x], tip = biomeCol[x].glow[0];
    const h = 1 + Math.floor(hash(px, 2, 30) ** 1.6 * 7), lean = hash(px, 3, 30) < 0.5 ? 0 : hash(px, 4, 30) < 0.5 ? -1 : 1;
    for (let k = 0; k < h; k++) {
      const xx = x + (k > h / 2 ? lean : 0);
      if (k === h - 1 && h > 3 && hash(px, 5, 30) < 0.35) { b.set(xx, sy - k, mix(tip, '#ffffff', 0.4)); g.add(xx, sy - k, tip, 0.5); }
      else b.set(xx, sy - k, k < h / 2 ? M[3] : M[2]);
    }
  }
  const out = { cv: b.canvas(), glow: g.canvas(1), x: x0, y: y0 };
  chunkCache.set(key, out); if (chunkCache.size > 10) chunkCache.delete(chunkCache.keys().next().value);
  return out;
}

const SPRITE_CACHE = {}; let HERO = null;
const ANCH = {};
function anchorOf(im) { if (ANCH[im] != null) return ANCH[im]; const t = tex(im); let s = 0, n = 0; for (let y = t.h - 3; y < t.h; y++) for (let x = 0; x < t.w; x++) if (t.d[(y * t.w + x) * 4 + 3] > 128) { s += x; n++; } return (ANCH[im] = n ? s / n : t.w / 2); }
function bellFor(Bm, h) { const f = C(Bm.flora[Math.floor(h * Bm.flora.length)]); const opts = [['bellTeal_0', '#7fe8d4'], ['bellViolet_0', '#b49aff'], ['bellAmber_0', '#ffd27a']]; let best = opts[0], bd = 1e9; for (const o of opts) { const c = C(o[1]), dd = (c[0] - f[0]) ** 2 + (c[1] - f[1]) ** 2 + (c[2] - f[2]) ** 2; if (dd < bd) { bd = dd; best = o; } } return best[0]; }
function spritesFor(P) {
  if (SPRITE_CACHE[P.id]) return SPRITE_CACHE[P.id];
  const list = []; let lastTree = -99;
  for (let cx = 3; cx < WORLD_W - 3; cx++) {
    if (SPIRES.some((s) => cx > s.x - 4 && cx < s.x + 13)) continue;
    const t = TOPS[cx]; if (TOPS[cx - 1] !== t || TOPS[cx + 1] !== t) continue;
    const zi = zoneIndex(P, cx), bk = P.biomes[zi], Bm = BIOMES[bk], h = hash(cx, 1, 555 + P.seed), h2 = hash(cx, 2, 555 + P.seed), f = Bm.fg;
    let name = null;
    if (h < f.tree && cx - lastTree > 11 && Math.abs(cx - SPAWN) > 2) { name = 'acacia_' + Math.floor(h2 * 3); lastTree = cx; }
    else if (h < f.tree + f.bush) name = 'bush_0';
    else if (h < f.tree + f.bush + f.rock) name = 'rock_0';
    else if (h < f.tree + f.bush + f.rock + f.bell) name = bellFor(Bm, h2);
    else if (h < f.tree + f.bush + f.rock + f.bell + 0.16) name = h2 < 0.5 ? 'tuft_0' : 'tuft_1';
    if (!name || !IMG[name]) continue;
    const px = cx * CELL + 8, glow = IMG[name + '_glow'] ? name + '_glow' : null;
    list.push({ name, glow, bk, x: px, ax: anchorOf(name), y: surfacePx(px) + (name.startsWith('acacia') ? 2 : 1), w: IMG[name].width, h: IMG[name].height, recol: name.startsWith('acacia') || name.startsWith('bush') || name.startsWith('tuft') });
  }
  return (SPRITE_CACHE[P.id] = list);
}
function buildHero() {
  const parts = { hero_shin: [1, -11], hero_thigh: [3, -21], hero_torso: [1, -34], hero_upperArm: [5, -32], hero_forearm: [5, -25], hero_helmet: [0, -43] };
  HERO = mkCanvas(16, 44); const hc = HERO.getContext('2d');
  for (const [n, [dx, dy]] of Object.entries(parts)) hc.drawImage(IMG[n], dx, 44 + dy);
}

// ---------- built backdrops, shared by the viewer and the gallery ----------
const BUILT = new Map();
function builtFor(P, dirKey, tod) {
  const k = `${P.id}|${dirKey}|${tod}`;
  if (BUILT.has(k)) { const v = BUILT.get(k); BUILT.delete(k); BUILT.set(k, v); return v; }
  const dir = DIRS[dirKey]; let B;
  if (dir.key === 'now') B = { now: true };
  else {
    const sky = skyFor(dir, P, tod), dusk = tod === 'dusk', W = P.weather;
    const cloudTones = W.low === 'ash' ? ['#8a7e7e', '#5e5454', '#3e3638'] : dusk ? ['#ffcaa0', '#d98a8a', '#7a5a7e'] : [mix('#ffffff', sky.haze, 0.25), mix(sky.ramp[3], '#ffffff', 0.35), mix(sky.ramp[2], sky.ramp[1], 0.35)];
    const clouds = [];
    for (const c of dir.clouds) {
      const kind = c.kind === 'low' ? W.low : c.kind;
      let cv;
      if (kind === 'cirrus') cv = bakeCirrus(c.seed + P.seed, dusk ? '#ffd9c0' : mix('#fbf7ec', sky.haze, 0.2), c.a);
      else if (kind === 'dust') cv = bakeDust(c.seed + P.seed, dusk ? '#e7a07a' : W.dust, c.a || 0.3);
      else if (kind === 'ash') cv = bakeCumulus(c.seed + P.seed, c.n || 7, cloudTones, 130, 1.2);
      else cv = bakeCumulus(c.seed + P.seed, c.n || 7, cloudTones, 130, c.flat || 1);
      clouds.push({ ...c, kind, cv });
    }
    const giant = bakeGiant({ ...sky.giant, bands: dusk ? ['#e8c0b0', '#c9a0a8', '#f0d0b8'] : P.giantBands, shade: dusk ? '#6a4a78' : mix(sky.ramp[1], '#7f9cb4', 0.5), ringA: dusk ? '#ffe0c8' : null, ringB: dusk ? '#e0b0b0' : null });
    const moons = sky.moons.map((m, i) => ({ ...m, cv: bakeMoon(m.r, dusk ? '#f0d8c8' : mix('#e4ecec', P.giantBands[0], 0.3), m.a, i + 3 + P.seed) }));
    B = { sky, P: { haze: sky.haze, dusk }, clouds, giant, moons, land: dir.land.map((spec) => ({ spec, z: [null, null, null] })), haze: bakeHazeNoise(sky.haze), skyCv: {} };
  }
  BUILT.set(k, B); while (BUILT.size > 6) BUILT.delete(BUILT.keys().next().value);
  return B;
}
function zoneLayer(B, P, i, zi, dirKey) {
  const slot = B.land[i];
  if (!slot.z[zi]) slot.z[zi] = buildLand(slot.spec, BIOMES[P.biomes[zi]], B.P, 1000 + i * 97 + dirKey.charCodeAt(0) * 7 + zi * 3 + P.seed * 13);
  return slot.z[zi];
}

// ---------- the surface viewer ----------
const STARS = Array.from({ length: 170 }, (_, k) => ({ fx: hash(k, 1, 3), fy: hash(k, 2, 3) ** 1.5 * 0.5, ph: hash(k, 3, 3) * 6.28, f: 0.6 + hash(k, 4, 3) * 2, big: hash(k, 5, 3) > 0.92 }));
const MOTES = Array.from({ length: 46 }, (_, k) => ({ k }));

class SurfaceView {
  constructor(el) {
    this.el = el; this.cv = el.querySelector('canvas.stage'); this.ctx = this.cv.getContext('2d');
    this.mini = el.querySelector('canvas.mini'); this.info = el.querySelector('.info');
    this.s = { dir: 'A', tod: 'day', zoom: 'near', gray: false, life: true, beacon: true, trans: 'T2', hidden: new Set() };
    this.planet = PRESETS.savanna;
    this.hero = { x: SPAWN * CELL + 8, face: 1, v: 0 }; this.look = 0; this.free = 0; this.keys = {}; this.fly = 0; this.lead = 0;
    this.t0 = performance.now(); this.tFix = null; this.visible = true;
    this.tmp = [mkCanvas(8, 8), mkCanvas(8, 8), mkCanvas(8, 8), mkCanvas(8, 8), mkCanvas(8, 8)];
    this.resize();
  }
  resize() {
    const [W, H] = this.s.zoom === 'far' ? [1536, 864] : [960, 540];
    if (this.cv.width !== W) { this.cv.width = W; this.cv.height = H; for (const t of this.tmp) { t.width = W; t.height = H; } }
    this.W = W; this.H = H;
  }
  built() { return builtFor(this.planet, this.s.dir, this.s.tod); }
  weights(cxCell, f) {
    const hw = this.s.trans === 'T1' ? 40 : 40 + (1 - clamp(f / 0.36)) * 260, [b1, b2] = this.planet.borders;
    const t1 = smooth(b1 - hw, b1 + hw, cxCell), t2 = smooth(b2 - hw, b2 + hw, cxCell);
    return [1 - t1, t1 * (1 - t2), t2];
  }
  prefetch() {
    const B = this.built(); if (!B.land) return false;
    for (let i = 0; i < B.land.length; i++) for (let zi = 0; zi < 3; zi++) if (!B.land[i].z[zi]) { zoneLayer(B, this.planet, i, zi, this.s.dir); return true; }
    return false;
  }
  time() { return this.tFix != null ? this.tFix : (performance.now() - this.t0) / 1000; }
  step(dt) {
    const run = this.keys.Shift ? 2.2 : 1, dirn = (this.keys.ArrowRight ? 1 : 0) - (this.keys.ArrowLeft ? 1 : 0);
    const speed = (this.fly ? 90 : 7.5 * run) * CELL;
    if (dirn) { this.hero.face = dirn; this.free = 0; }
    this.hero.v += (dirn * speed - this.hero.v) * Math.min(1, dt * 12);
    this.hero.x = clamp(this.hero.x + this.hero.v * dt, 40 * CELL, (WORLD_W - 40) * CELL);
    this.lead += ((Math.abs(this.hero.v) > 1 ? Math.sign(this.hero.v) * 2 * CELL * Math.min(1, Math.abs(this.hero.v) / (14 * CELL)) : 0) - this.lead) * Math.min(1, dt / 0.65);
    const lk = (this.keys.ArrowUp ? -1 : 0) + (this.keys.ArrowDown ? 1 : 0);
    if (lk) this.look = clamp(this.look + lk * dt * 260, -260, 200);
  }

  draw() {
    this.resize();
    const { ctx, W, H } = this, t = this.time(), B = this.built(), P = this.planet;
    const heroFoot = surfacePx(this.hero.x);
    const camX = this.hero.x + this.lead + this.free, camY = heroFoot - 22 + this.look;
    const groundY = TOPS[SPAWN] * CELL, lineOf = (f) => H / 2 + (groundY - camY) * f;
    const hid = this.s.hidden, show = (id) => !hid.has(id), cxCell = camX / CELL;
    ctx.imageSmoothingEnabled = false; ctx.globalCompositeOperation = 'source-over'; ctx.globalAlpha = 1;
    if (B.now) this.drawNow(ctx, W, H, t, camX, lineOf, show);
    else this.drawDir(ctx, W, H, t, camX, lineOf, show, B, cxCell);
    const wl = camX - W / 2, wt = camY - H / 2, boost = this.s.tod === 'dusk' && !B.now ? 1.5 : 1;
    if (show('fg')) {
      for (const sp of SPIRES) {
        const lx = (sp.x + 4) * CELL + 8 - wl, ly = sp.top * CELL + 8 - wt;
        if (lx > -60 && lx < W + 60) this.beam(ctx, lx, ly, 1, t, boost, 1);
        else if (this.s.beacon && !B.now && Math.abs(lx - W / 2) < 420 * CELL) {
          const dx = lx - W / 2, bx = W / 2 + Math.sign(dx) * Math.min(W * 0.47, Math.abs(dx) * 0.06 + W * 0.25);
          this.beam(ctx, bx, lineOf(0.06) - 30, 0.35 * (1 - Math.abs(dx) / (420 * CELL)), t, boost, 0.6);
        }
      }
      this.drawGround(ctx, W, H, t, wl, wt, heroFoot, boost, B.now ? PRESETS.savanna : P);
    }
    if (this.s.gray !== this._gray) { this.cv.style.filter = this.s.gray ? 'grayscale(1)' : 'none'; this._gray = this.s.gray; }
    if (this.mini) this.drawMini(cxCell, B.now);
    if (this.info) {
      const w = this.weights(cxCell, 0.36), zs = [0, 1, 2].filter((k) => w[k] > 0.01).map((k) => `${BIOMES[P.biomes[k]].name} ${Math.round(w[k] * 100)} %`).join(' · ');
      const txt = `клетка ${Math.round(this.hero.x / CELL)} из 2048 · ${B.now ? 'одна картинка на всю планету' : zs}`;
      if (txt !== this._txt) { this.info.textContent = txt; this._txt = txt; }
    }
  }
  beam(ctx, x, y, a, t, boost, scale) {
    const sh = (0.86 + 0.14 * Math.sin(t * 1.7 + x * 0.01)) * a * boost;
    ctx.globalCompositeOperation = 'lighter';
    for (const [w, c, al] of [[34 * scale, '#5fd8e8', 0.05], [12 * scale, '#7fe8f0', 0.14], [4 * scale, '#e6fffb', 0.34]]) {
      const g = ctx.createLinearGradient(0, y, 0, 0); g.addColorStop(0, css(c, al * sh)); g.addColorStop(0.55, css(c, al * sh * 0.6)); g.addColorStop(1, css(c, 0));
      ctx.fillStyle = g; ctx.fillRect(Math.round(x - w / 2), 0, Math.round(w), Math.max(0, Math.round(y)));
    }
    for (let k = 0; k < 5; k++) { const p = mod(t * 0.12 + k / 5, 1), yy = y - p * y; ctx.fillStyle = css('#e6fffb', 0.5 * (1 - p) * sh); ctx.fillRect(Math.round(x + Math.sin(k * 3 + t) * 3 * scale), Math.round(yy), 1, 1); }
    ctx.globalCompositeOperation = 'source-over';
  }
  drawNow(ctx, W, H, t, camX, lineOf, show) {
    const sky = IMG.sky, skyTop = lineOf(0.04) - 272, top = tex('sky');
    ctx.fillStyle = css([top.d[0], top.d[1], top.d[2]]); ctx.fillRect(0, 0, W, H);
    if (show('sky')) ctx.drawImage(sky, 0, 0, 1, sky.height, 0, Math.round(skyTop), W, sky.height);
    if (show('cel')) {
      const gi = IMG.giant; ctx.drawImage(gi, Math.round(W * 0.17 - gi.width / 2), Math.round(H * 0.22 - gi.height / 2));
      const su = IMG.sun; ctx.globalCompositeOperation = 'lighter'; ctx.drawImage(su, Math.round(W * 0.8 - su.width / 2), Math.round(H * 0.16 - su.height / 2)); ctx.globalCompositeOperation = 'source-over';
    }
    if (show('clouds')) drawWrap(ctx, IMG.clouds, camX * 0.06 + t * 1.5 - W / 2, H * 0.02, W);
    [['far', 96, 0.1], ['ridge', 140, 0.18], ['hills', 150, 0.3]].forEach(([n, tp, f], i) => {
      if (!show('L' + i)) return;
      const im = IMG[n], y = lineOf(f) + (tp - 272), tt = tex(n), bi = ((tt.h - 1) * tt.w) * 4;
      drawWrap(ctx, im, camX * f - W / 2, y, W);
      ctx.fillStyle = css([tt.d[bi], tt.d[bi + 1], tt.d[bi + 2]]); ctx.fillRect(0, Math.round(y + im.height - 1), W, H);
    });
  }
  drawDir(ctx, W, H, t, camX, lineOf, show, B, cxCell) {
    const sky = B.sky, dusk = !!sky.dusk, boost = dusk ? 1.55 : 1, P = this.planet;
    if (!B.skyCv[W]) B.skyCv[W] = bakeSky(sky.ramp, W);
    const horizon = lineOf(0.04), skyTop = horizon - 290;
    ctx.fillStyle = css(sky.ramp[0]); ctx.fillRect(0, 0, W, H);
    if (show('sky')) { ctx.drawImage(B.skyCv[W], 0, Math.round(skyTop)); ctx.fillStyle = css(sky.ramp[sky.ramp.length - 1]); ctx.fillRect(0, Math.round(skyTop + 299), W, H); }
    if (show('cel')) {
      if (sky.stars) for (const s of STARS) {
        const a = sky.stars * (0.35 + 0.65 * (0.5 + 0.5 * Math.sin(t * s.f + s.ph))) * clamp(1 - s.fy / 0.5) * (dusk ? 1 : 0.55);
        const x = Math.round(s.fx * W), y = Math.round(s.fy * H); ctx.fillStyle = css('#f4f6ff', a); ctx.fillRect(x, y, 1, 1);
        if (s.big) { ctx.fillStyle = css('#cfe0ff', a * 0.45); ctx.fillRect(x - 1, y, 3, 1); ctx.fillRect(x, y - 1, 1, 3); }
      }
      if (sky.aurora) this.aurora(ctx, W, H, t, (dusk ? 0.85 : 0.5) * sky.auroraA);
      const g = B.giant; ctx.drawImage(g, Math.round(W * sky.giant.fx - g.width / 2), Math.round(H * sky.giant.fy - g.height / 2));
      for (const m of B.moons) ctx.drawImage(m.cv, Math.round(W * m.fx - m.cv.width / 2), Math.round(H * m.fy - m.cv.height / 2));
      if (sky.ship) this.ship(ctx, W, H, t, dusk);
      this.sun(ctx, W, H, t, sky, horizon);
    }
    if (show('clouds')) for (const c of B.clouds) drawWrap(ctx, c.cv, camX * c.follows + t * c.drift - W / 2, H * c.y, W);
    B.land.forEach((slot, i) => {
      const f = slot.spec.follows, w = this.weights(cxCell, f), y0 = lineOf(f), off = camX * f - W / 2;
      if (show('L' + i)) {
        const zs = [0, 1, 2].filter((k) => w[k] > 0.004);
        for (const zi of zs) zoneLayer(B, P, i, zi, this.s.dir);
        if (zs.length === 1) this.drawZoneLayer(ctx, slot.z[zs[0]], W, H, y0, off, t, boost);
        else {
          const [a, b, slotCv] = this.tmp, sc = slotCv.getContext('2d');
          sc.clearRect(0, 0, W, H);
          zs.forEach((zi, j) => {
            const tc = (j ? b : a).getContext('2d'); tc.clearRect(0, 0, W, H); tc.imageSmoothingEnabled = false;
            this.drawZoneLayer(tc, slot.z[zi], W, H, y0, off, t, boost, true);
            sc.globalAlpha = w[zi]; sc.globalCompositeOperation = j ? 'lighter' : 'source-over'; sc.drawImage(j ? b : a, 0, 0);
          });
          sc.globalAlpha = 1; sc.globalCompositeOperation = 'source-over'; ctx.drawImage(slotCv, 0, 0);
          zs.forEach((zi) => this.drawGlows(ctx, slot.z[zi], W, y0, off, t, w[zi] * boost));
        }
      }
      if (slot.spec.hazeBand && show('haze')) this.hazeBand(ctx, B, W, y0, camX * f + t * 3, slot.spec.hazeBand * (dusk ? 1.1 : 1));
      if (i === 1 && this.s.life && show('life')) this.flock(ctx, W, t, camX, lineOf(0.15), dusk);
    });
  }
  drawZoneLayer(ctx, L, W, H, y0, off, t, boost, noGlow) {
    if (L.kind === 'floaters') {
      for (const s of L.sprites) { const y = Math.round(y0 + s.y + Math.sin(t * 0.35 + s.ph) * s.amp - s.top); for (const x of wrapX(s.x, off, LW, W, s.w)) ctx.drawImage(s.cv, Math.round(x - s.w / 2), y); }
      if (!noGlow) this.drawGlows(ctx, L, W, y0, off, t, boost);
      return;
    }
    const y = y0 - L.B; drawWrap(ctx, L.cv, off, y, W);
    ctx.fillStyle = css(L.under); ctx.fillRect(0, Math.round(y + L.H - 1), W, H);
    // smoke from the craters, a few puffs rising and leaning with the wind
    for (const p of L.plumes) for (const x of wrapX(p.x, off, LW, W, 60)) for (let k = 0; k < 7; k++) {
      const q = mod(t * 0.06 + k / 7, 1), r = 2 + q * 9, px = Math.round(x + q * q * 40), py = Math.round(y + p.y - 3 - q * 90);
      ctx.fillStyle = css(mix(L.T.base, '#2a2224', 0.5), 0.55 * (1 - q)); ctx.beginPath(); ctx.arc(px, py, r, 0, 6.29); ctx.fill();
    }
    if (!noGlow) this.drawGlows(ctx, L, W, y0, off, t, boost);
  }
  drawGlows(ctx, L, W, y0, off, t, a) {
    ctx.globalCompositeOperation = 'lighter';
    if (L.kind === 'floaters') {
      ctx.globalAlpha = Math.min(1, a);
      for (const s of L.sprites) { const y = Math.round(y0 + s.y + Math.sin(t * 0.35 + s.ph) * s.amp - s.top); for (const x of wrapX(s.x, off, LW, W, s.w)) ctx.drawImage(s.glow, Math.round(x - s.w / 2), y); }
      ctx.globalAlpha = 1;
    } else {
      const y = y0 - L.B; drawWrap(ctx, L.glow, off, y, W, Math.min(1, a));
      ctx.globalCompositeOperation = 'lighter';
      for (const p of L.pillars) for (const x of wrapX(p.x, off, LW, W, 40)) {
        const sh = (0.8 + 0.2 * Math.sin(t * 1.3 + p.x)) * a, py = y + p.y;
        const g = ctx.createLinearGradient(0, py, 0, 0); g.addColorStop(0, css('#9ff4f4', 0.22 * sh)); g.addColorStop(1, css('#9ff4f4', 0));
        ctx.fillStyle = g; ctx.fillRect(Math.round(x - p.s * 1.5), 0, Math.max(2, Math.round(p.s * 3)), Math.round(py));
        const g2 = ctx.createLinearGradient(0, py, 0, 0); g2.addColorStop(0, css('#e6fffb', 0.4 * sh)); g2.addColorStop(1, css('#e6fffb', 0));
        ctx.fillStyle = g2; ctx.fillRect(Math.round(x - 0.5), 0, 1, Math.round(py));
      }
      for (const [tx, ty, col] of L.twinkles) {
        const ph = mod(t * 0.35 + hash(tx | 0, ty | 0, 7), 1); if (ph > 0.1) continue;
        const k = Math.sin((ph / 0.1) * Math.PI) * a;
        for (const x of wrapX(tx, off, LW, W, 6)) { const X = Math.round(x), Y = Math.round(y + ty); ctx.fillStyle = css('#ffffff', k); ctx.fillRect(X, Y, 1, 1); ctx.fillStyle = css(col, k * 0.7); ctx.fillRect(X - 2, Y, 5, 1); ctx.fillRect(X, Y - 2, 1, 5); }
      }
    }
    ctx.globalCompositeOperation = 'source-over'; ctx.globalAlpha = 1;
  }
  hazeBand(ctx, B, W, y0, xoff, a) {
    const y = Math.round(y0 - 34), h = 76, hz = B.sky.haze;
    const g = ctx.createLinearGradient(0, y, 0, y + h); g.addColorStop(0, css(hz, 0)); g.addColorStop(0.55, css(hz, a)); g.addColorStop(1, css(hz, 0));
    ctx.fillStyle = g; ctx.fillRect(0, y, W, h);
    drawWrap(ctx, B.haze, xoff, y + 8, W, Math.min(1, a * 2.2));
  }
  sun(ctx, W, H, t, sky, horizon) {
    const s = sky.sun, k = s.size || 1, x = W * s.fx, y = s.horizon ? horizon - 34 : H * s.fy;
    ctx.globalCompositeOperation = 'lighter';
    const col = s.horizon ? [255, 170, 110] : [255, 236, 190];
    const g = ctx.createRadialGradient(x, y, 0, x, y, 90 * k); g.addColorStop(0, css(col, 0.55)); g.addColorStop(0.25, css(col, 0.18)); g.addColorStop(1, css(col, 0));
    ctx.fillStyle = g; ctx.fillRect(x - 90 * k, y - 90 * k, 180 * k, 180 * k);
    if (sky.rays) for (let i = 0; i < 9; i++) {
      const ang = Math.PI * (0.18 + i * 0.08) + Math.sin(t * 0.05 + i) * 0.02, a = 0.045 + 0.03 * Math.sin(t * 0.23 + i * 1.7);
      const L = H * 1.4, x1 = x + Math.cos(ang) * L, y1 = y + Math.sin(ang) * L, x2 = x + Math.cos(ang + 0.035) * L, y2 = y + Math.sin(ang + 0.035) * L;
      const gr = ctx.createLinearGradient(x, y, (x1 + x2) / 2, (y1 + y2) / 2); gr.addColorStop(0, css(col, a)); gr.addColorStop(1, css(col, 0));
      ctx.fillStyle = gr; ctx.beginPath(); ctx.moveTo(x, y); ctx.lineTo(x1, y1); ctx.lineTo(x2, y2); ctx.closePath(); ctx.fill();
    }
    const r = Math.round(6 * k); ctx.fillStyle = s.horizon ? '#ffd8a8' : '#ffffff';
    for (let dy = -r; dy <= r; dy++) { const w = Math.round(Math.sqrt(r * r - dy * dy)); ctx.fillRect(Math.round(x - w), Math.round(y + dy), w * 2 + 1, 1); }
    for (let q = 8; q < 46 * k; q++) { const f = 0.5 * (1 - q / (46 * k)) ** 2; ctx.fillStyle = css(col, f); ctx.fillRect(Math.round(x + q), Math.round(y), 1, 1); ctx.fillRect(Math.round(x - q), Math.round(y), 1, 1); ctx.fillStyle = css(col, f * 0.7); ctx.fillRect(Math.round(x), Math.round(y + q), 1, 1); ctx.fillRect(Math.round(x), Math.round(y - q), 1, 1); }
    ctx.globalCompositeOperation = 'source-over';
  }
  ship(ctx, W, H, t, dusk) {
    const p = mod(t / 120, 1), x = Math.round((p * 1.3 - 0.15) * W), y = Math.round(H * (0.09 + 0.06 * Math.sin(p * Math.PI)));
    ctx.globalCompositeOperation = 'lighter';
    ctx.fillStyle = css('#e6fffb', dusk ? 0.95 : 0.7); ctx.fillRect(x, y, 1, 1);
    ctx.fillStyle = css('#7fe8f0', dusk ? 0.5 : 0.3); ctx.fillRect(x - 1, y, 3, 1); ctx.fillRect(x, y - 1, 1, 3);
    if (mod(t, 1.6) < 0.18) { ctx.fillStyle = css('#ffb070', 0.8); ctx.fillRect(x + 2, y, 1, 1); }
    ctx.globalCompositeOperation = 'source-over';
  }
  aurora(ctx, W, H, t, a) {
    const w = 240, h = 70; if (!this.au) { this.au = mkCanvas(w, h); this.auD = new ImageData(w, h); }
    const d = this.auD.data; d.fill(0);
    for (let x = 0; x < w; x++) {
      const base = 22 + 9 * Math.sin(x * 0.035 + t * 0.07) + 6 * Math.sin(x * 0.011 - t * 0.04);
      const k = Math.pow(0.5 + 0.5 * Math.sin(x * 0.09 - t * 0.35 + Math.sin(x * 0.02 + t * 0.1) * 2), 2) * (0.5 + 0.5 * Math.sin(x * 0.013 + t * 0.05));
      const len = 26 + 14 * Math.sin(x * 0.05 + t * 0.2);
      for (let y = 0; y < h; y++) {
        const dy = y - base; if (dy < -len || dy > 6) continue;
        const f = dy > 0 ? 1 - dy / 6 : 1 + dy / len, q = Math.round(clamp(f * k) * 3) / 3; if (q <= 0) continue;
        const c = mix('#7fe8d4', '#b49aff', clamp(-dy / len)), i = (y * w + x) * 4;
        d[i] = c[0]; d[i + 1] = c[1]; d[i + 2] = c[2]; d[i + 3] = q * 255 * 0.55;
      }
    }
    this.au.getContext('2d').putImageData(this.auD, 0, 0);
    ctx.globalCompositeOperation = 'lighter'; ctx.globalAlpha = a; ctx.drawImage(this.au, 0, 0, w, h, 0, Math.round(H * 0.02), W, Math.round(h * (W / w) * 0.5)); ctx.globalAlpha = 1; ctx.globalCompositeOperation = 'source-over';
  }
  flock(ctx, W, t, camX, y0, dusk) {
    const off = camX * 0.15;
    ctx.fillStyle = css(dusk ? '#2a2038' : '#3e5868', 0.75);
    for (let g = 0; g < 3; g++) {
      const gx = mod(g * 700 + t * (9 + g * 3) - off, LW + 400) - 200, gy = y0 - 120 - g * 34 + Math.sin(t * 0.3 + g) * 8;
      if (gx < -100 || gx > W + 100) continue;
      for (let k = 0; k < 6; k++) {
        const x = Math.round(gx + k * 9 + (k % 2) * 4 + Math.sin(t * 0.7 + k) * 2), y = Math.round(gy + (k % 3) * 5 + Math.sin(t * 0.9 + k * 2) * 2), up = mod(t * 3 + k * 0.37, 1) < 0.5;
        ctx.fillRect(x - 2, y + (up ? -1 : 1), 2, 1); ctx.fillRect(x, y, 1, 1); ctx.fillRect(x + 1, y + (up ? -1 : 1), 2, 1);
      }
    }
  }
  drawGround(ctx, W, H, t, wl, wt, heroFoot, boost, P) {
    const dusk = this.s.tod === 'dusk', fg = this.tmp[3], fc = fg.getContext('2d'), mask = this.tmp[4], mc = mask.getContext('2d');
    fc.imageSmoothingEnabled = false; fc.clearRect(0, 0, W, H);
    const i0 = Math.floor(wl / (CHW * CELL)), i1 = Math.floor((wl + W) / (CHW * CELL)), chunks = [];
    for (let ix = i0; ix <= i1; ix++) { const c = chunk(P, ix); chunks.push(c); fc.drawImage(c.cv, Math.round(c.x - wl), Math.round(c.y - wt)); }
    fc.fillStyle = '#000'; fc.fillRect(0, Math.round(ROW1 * CELL - wt), W, H);
    const vis = spritesFor(P).filter((s) => s.x - wl > -120 && s.x - wl < W + 120);
    for (const s of vis) fc.drawImage(s.recol ? recolour(s.name, s.bk) : IMG[s.name], Math.round(s.x - s.ax - wl), Math.round(s.y - s.h - wt));
    const hx = Math.round(this.hero.x - wl), hy = Math.round(heroFoot - wt);
    fc.save(); fc.translate(hx - 8, hy - 44); if (this.hero.face < 0) { fc.translate(16, 0); fc.scale(-1, 1); } fc.drawImage(HERO, 0, 0); fc.restore();
    if (dusk) {
      mc.clearRect(0, 0, W, H); mc.drawImage(fg, 0, 0);
      fc.globalCompositeOperation = 'multiply'; fc.fillStyle = '#c0a0b4'; fc.fillRect(0, 0, W, H);
      fc.globalCompositeOperation = 'destination-in'; fc.drawImage(mask, 0, 0); fc.globalCompositeOperation = 'source-over';
    }
    ctx.drawImage(fg, 0, 0);
    ctx.globalCompositeOperation = 'lighter'; ctx.globalAlpha = Math.min(1, 0.9 * boost);
    for (const c of chunks) ctx.drawImage(c.glow, Math.round(c.x - wl), Math.round(c.y - wt));
    for (const s of vis) if (s.glow) ctx.drawImage(recolour(s.glow, s.bk), Math.round(s.x - s.ax - wl), Math.round(s.y - s.h - wt));
    ctx.globalAlpha = 1;
    for (const sp of SPIRES) {
      const lx = Math.round((sp.x + 4) * CELL + 8 - wl), ly = Math.round(sp.top * CELL + 8 - wt);
      if (lx < -30 || lx > W + 30) continue;
      const g = ctx.createRadialGradient(lx, ly, 0, lx, ly, 26); g.addColorStop(0, css('#7fe8f0', 0.55)); g.addColorStop(1, css('#7fe8f0', 0)); ctx.fillStyle = g; ctx.fillRect(lx - 26, ly - 26, 52, 52);
      for (const [dx, dy, c] of [[0, -3, '#7fe8f0'], [-1, -2, '#7fe8f0'], [0, -2, '#e6fffb'], [1, -2, '#7fe8f0'], [-2, -1, '#5fc8e0'], [-1, -1, '#e6fffb'], [0, -1, '#ffffff'], [1, -1, '#e6fffb'], [2, -1, '#5fc8e0'], [-1, 0, '#7fe8f0'], [0, 0, '#e6fffb'], [1, 0, '#7fe8f0'], [0, 1, '#5fc8e0']]) { ctx.fillStyle = c; ctx.fillRect(lx + dx, ly + dy, 1, 1); }
    }
    if (this.s.life && !this.s.hidden.has('life')) this.motes(ctx, W, H, t, wl, wt, dusk, P);
    ctx.globalCompositeOperation = 'source-over';
  }
  motes(ctx, W, H, t, wl, wt, dusk, P) {
    const cols = P.weather.motes.map(C);
    for (const m of MOTES) {
      const life = mod(t * 0.09 + m.k / MOTES.length, 1), cyc = Math.floor(t * 0.09 + m.k / MOTES.length);
      const sx = hash(m.k, cyc, 91) * W, gy = surfacePx(wl + sx) - wt;
      const y = gy - 6 - hash(m.k, cyc, 92) * 70 - life * 26, x = sx + Math.sin(t * 0.6 + m.k) * 6 + life * 10;
      if (y > gy - 2) continue;
      const a = Math.sin(life * Math.PI) * (0.35 + hash(m.k, 3, 90) * 0.45) * (dusk ? 1.4 : 1), col = cols[Math.floor(hash(m.k, cyc, 93) * cols.length)];
      ctx.fillStyle = css(mix(col, [255, 255, 255], 0.45), a); ctx.fillRect(Math.round(x), Math.round(y), 1, 1);
      ctx.fillStyle = css(col, a * 0.3); ctx.fillRect(Math.round(x) - 1, Math.round(y), 3, 1); ctx.fillRect(Math.round(x), Math.round(y) - 1, 1, 3);
    }
  }
  drawMini(cxCell, now) {
    const m = this.mini, c = m.getContext('2d'), w = m.width, h = m.height, P = now ? null : this.planet, key = P ? P.id : 'now';
    if (this.miniKey !== key) {
      this.miniKey = key; this.miniBase = mkCanvas(w, h); const b = this.miniBase.getContext('2d');
      const ed = P ? [0, P.borders[0], P.borders[1], WORLD_W] : [0, WORLD_W];
      for (let z = 0; z < ed.length - 1; z++) { b.fillStyle = P ? BIOMES[P.biomes[z]].map : '#5f9c74'; b.fillRect(Math.round((ed[z] / WORLD_W) * w), 0, Math.round(((ed[z + 1] - ed[z]) / WORLD_W) * w) + 1, h); }
      b.fillStyle = 'rgba(10,14,18,.55)';
      for (let x = 0; x < w; x++) { const cx = Math.floor((x / w) * WORLD_W), y = Math.round(h * 0.3 + (TOPS[cx] - SURF + 18) * 0.55); b.fillRect(x, y, 1, h - y); }
      b.fillStyle = '#9ff4f4'; for (const s of SPIRES) b.fillRect(Math.round(((s.x + 4) / WORLD_W) * w) - 1, 0, 2, Math.round(h * 0.6));
      if (this.onMini) this.onMini(P);
    }
    c.clearRect(0, 0, w, h); c.drawImage(this.miniBase, 0, 0);
    const vx = (cxCell / WORLD_W) * w, vw = (this.W / CELL / WORLD_W) * w;
    c.strokeStyle = '#f4f1e6'; c.lineWidth = 2; c.strokeRect(Math.round(vx - vw / 2) + 0.5, 1, Math.max(4, Math.round(vw)), h - 2);
  }
}

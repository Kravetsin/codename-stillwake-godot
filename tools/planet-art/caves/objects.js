// Cave decor and light sources, drawn pixel by pixel into a Pic. Every glowing thing also adds a light.
const { C, hex, mix, mul, hash, vnoise, MP, loadArt } = require('./engine');

const PAL = {
  teal: { hi: hex('#b4fff0'), mid: hex('#4fe6c8'), lo: hex('#1c8a7e'), dk: hex('#0e3f3c'), stem: hex('#c9dcd4'), light: [0.35, 1, 0.85] },
  violet: { hi: hex('#f0d8ff'), mid: hex('#b48cff'), lo: hex('#6a46c8'), dk: hex('#2c1e58'), stem: hex('#d0c8e0'), light: [0.7, 0.5, 1] },
  blue: { hi: hex('#e8f8ff'), mid: hex('#8cccff'), lo: hex('#3c78d0'), dk: hex('#1a3266'), stem: hex('#c8d8e8'), light: [0.5, 0.78, 1] },
  amber: { hi: hex('#fff2c0'), mid: hex('#ffbe5a'), lo: hex('#c87028'), dk: hex('#5a2c10'), stem: hex('#e0d0b0'), light: [1, 0.7, 0.32] },
  rose: { hi: hex('#ffe0dc'), mid: hex('#ff9a8c'), lo: hex('#c04c5c'), dk: hex('#561c2c'), stem: hex('#e6d0cc'), light: [1, 0.5, 0.5] },
  ember: { hi: hex('#fff0b0'), mid: hex('#ff8a30'), lo: hex('#c83a18'), dk: hex('#4a140a'), stem: hex('#d8b8a0'), light: [1, 0.42, 0.16] },
  green: { hi: hex('#e4ffc8'), mid: hex('#9cf070'), lo: hex('#4a9a3a'), dk: hex('#1e3e1a'), stem: hex('#d4dcc0'), light: [0.6, 1, 0.45] },
  ochre: { hi: hex('#fff0c8'), mid: hex('#e8c060'), lo: hex('#a07030'), dk: hex('#4a3010'), stem: hex('#ddd4bc'), light: [1, 0.82, 0.45] },
};

// ---- glow mushrooms: a cluster of caps on a floor (or hanging from a ceiling) ----
const CAPS = { 11: [3, 4, 5, 5], 9: [2, 3, 4, 4], 7: [2, 3, 3], 5: [1, 2], 3: [1] };
function glowcaps(pic, cx, surfY, pal, seed, n = 4, opt = {}) {
  const hang = !!opt.hang, dir = hang ? 1 : -1, sc = opt.scale || 1;
  const caps = [];
  for (let i = 0; i < n; i++) {
    const role = i === Math.floor(n / 2) ? 'big' : hash(i, seed, 3) < 0.45 ? 'mid' : 'small';
    const x = Math.round(cx + (i - (n - 1) / 2) * (opt.spread || 5.5) + (hash(i, seed, 1) - 0.5) * 3);
    const h = role === 'big' ? Math.round((8 + hash(i, seed, 2) * 4) * sc) : role === 'mid' ? 4 + Math.floor(hash(i, seed, 2) * 4) : 2 + Math.floor(hash(i, seed, 2) * 2);
    const w = role === 'big' ? (sc > 1.1 ? 11 : 9) : role === 'mid' ? (hash(i, seed, 5) < 0.5 ? 5 : 7) : (hash(i, seed, 6) < 0.5 ? 3 : 5);
    caps.push({ x, h, w: hang ? Math.max(5, w) : w, role, lean: role === 'small' ? 0 : Math.round((hash(i, seed, 4) - 0.5) * 2.4) });
  }
  caps.sort((a, b) => a.h - b.h);
  for (const c of caps) {
    for (let k = 0; k < c.h; k++) {
      const sx = c.x + (k > c.h * 0.55 ? c.lean : 0), y = surfY + dir * (k + 1) - (hang ? 1 : 0);
      pic.set(sx, y, k === 0 ? mul(pal.stem, 0.55) : pal.stem);
      if (c.role === 'big') pic.set(sx + 1, y, mul(pal.stem, 0.6));
      if (c.role === 'big' && k === 0) { pic.set(sx - 1, y, mul(pal.stem, 0.5)); pic.set(sx + 2, y, mul(pal.stem, 0.4)); }
    }
    const sx = c.x + c.lean + (c.role === 'big' ? 0.5 : 0), capY = surfY + dir * (c.h + 1), rows = CAPS[c.w], H = rows.length;
    for (let r = 0; r < H; r++) {
      const hw = rows[r], y = hang ? capY + r : capY - (H - 1 - r), last = r === H - 1;
      for (let dx = -hw; dx <= hw; dx++) {
        const X = Math.floor(sx + dx), edge = Math.abs(dx) === hw;
        let col = pal.mid, k = 0.85;
        if (hang && last && !edge) { col = mix(pal.hi, [1, 1, 1], 0.3); k = 1.1; }
        else if (r === 0) { col = hang ? pal.mid : pal.hi; k = hang ? 0.8 : 1; }
        else if (edge) { col = pal.lo; k = 0.6; }
        else if (dx < 0 && r === 1) { col = pal.hi; k = 1; }
        else if (dx > hw - 2) { col = mix(pal.lo, pal.mid, 0.5); k = 0.7; }
        if (!last && r > 0 && !edge && hash(X, y, seed + 5) < 0.18) { col = mix(pal.hi, [1, 1, 1], 0.4); k = 1.05; }
        if (!hang && last && !edge && c.role !== 'small') { col = mix(pal.lo, pal.mid, 0.4); k = 0.7; }
        pic.glow(X, y, col, k);
      }
    }
    if (hang) { const X = Math.floor(sx); pic.glow(X, capY + H + 1, pal.hi, 1); if (c.role === 'big') pic.glow(X, capY + H + 2, pal.mid, 0.7); }
    if (c.w >= 5 && !hang) { const gy = hang ? capY + H : capY + 1, hw = rows[H - 1] - 1; for (let dx = -hw; dx <= hw; dx++) if (Math.floor(sx + dx) !== c.x + c.lean) pic.set(Math.floor(sx + dx), gy, pal.dk); }
  }
  const tallest = Math.max(...caps.map((c) => c.h));
  const ly = surfY + dir * (tallest * 0.7 + 2);
  pic.light({ x: cx + 0.5, y: ly, col: pal.light, r: opt.r || 3.2 + n * 0.25, k: opt.k || 0.75, p: 1.7 });
  const ns = opt.spores == null ? 6 : opt.spores;
  for (let s = 0; s < ns; s++) pic.motes.push({ x: cx + (hash(s, seed, 7) - 0.5) * 30, y: surfY + dir * (8 + hash(s, seed, 8) * 26), col: pal.mid, a: 0.3 + hash(s, seed, 9) * 0.6 });
  pic.emitters.push({ kind: 'spore', x: cx, y: surfY + dir * 8, col: pal.mid, up: !hang, n: ns + 2 });
}

// ---- glow moss: tufts on the tops of floors, along a span ----
function moss(pic, x0, x1, pal, seed, dens = 0.55) {
  const w = pic.w; let lit = [];
  for (let x = x0; x <= x1; x++) {
    let y = -1; for (let yy = 1; yy < w.PH; yy++) if (!w.sp(x, yy - 1) && w.sp(x, yy) && yy > (pic.mossMinY || 0)) { y = yy; if (pic.mossFirst) break; }
    if (y < 0) continue;
    if (vnoise(x, 0, 7, seed) < 1 - dens) continue;
    pic.glow(x, y, pal.lo, 0.55);
    const h = hash(x, seed, 2); if (h < 0.55) pic.glow(x, y - 1, h < 0.2 ? pal.mid : pal.lo, h < 0.2 ? 0.9 : 0.5);
    if (h < 0.12) pic.glow(x, y - 2, pal.hi, 1);
    if (hash(x, seed, 3) < 0.4 && w.sp(x, y + 1)) pic.glow(x, y + 1, mul(pal.lo, 0.7), 0.35);
    lit.push([x, y]);
  }
  for (let k = 0; k < lit.length; k += 14) { const [x, y] = lit[Math.min(lit.length - 1, k + 6)]; pic.light({ x, y: y - 3, col: pal.light, r: 3, k: 0.4 }); }
}
// moss that clings to a wall face or ceiling run: given explicit pixel coordinates of air next to rock
function mossOnSurface(pic, pts, pal, seed) {
  for (const [x, y] of pts) { const h = hash(x, y, seed); if (h < 0.4) continue; pic.glow(x, y, h > 0.9 ? pal.mid : pal.lo, h > 0.9 ? 0.9 : 0.5); }
}

// ---- alien threads: strands from a ceiling with glowing beads; a sagging web between two points ----
function thread(pic, x, ceilY, len, pal, seed) {
  let xx = x;
  for (let k = 0; k < len; k++) {
    const y = ceilY + k; if (k > 2 && hash(k, seed, 1) < 0.12) xx += hash(k, seed, 2) < 0.5 ? -1 : 1;
    if (pic.solid(xx, y)) break;
    const bead = k > 2 && (k % (4 + (seed % 3)) === 0);
    if (bead) pic.glow(xx, y, pal.hi, 0.95); else pic.glow(xx, y, mul(pal.lo, 0.8), 0.22);
    if (k === len - 1) { pic.glow(xx, y + 1, pal.hi, 1); pic.glow(xx - 1, y + 1, pal.mid, 0.55); pic.glow(xx + 1, y + 1, pal.mid, 0.55); pic.glow(xx, y + 2, pal.mid, 0.6); }
  }
}
function web(pic, xa, ya, xb, yb, sag, pal, seed) {
  const n = Math.abs(xb - xa);
  for (let k = 0; k <= n; k++) {
    const t = k / n, x = Math.round(xa + (xb - xa) * t), y = Math.round(ya + (yb - ya) * t + sag * 4 * t * (1 - t));
    const bead = k % 5 === 2; pic.glow(x, y, bead ? pal.hi : mul(pal.lo, 0.8), bead ? 0.9 : 0.22);
  }
}
function threads(pic, x0, ceilY, pal, seed, n = 5) {
  for (let i = 0; i < n; i++) {
    const x = x0 + Math.round(i * 3 + hash(i, seed, 1) * 2);
    let cy = ceilY; while (cy > 0 && !pic.solid(x, cy - 1)) cy--; // stick to the actual ceiling
    thread(pic, x, cy, 8 + Math.floor(hash(i, seed, 3) * 22), pal, seed + i);
  }
  pic.light({ x: x0 + n * 1.5, y: ceilY + 14, col: pal.light, r: 3.4, k: 0.55 });
  pic.emitters.push({ kind: 'bead', x: x0 + n * 1.5, y: ceilY + 12, col: pal.hi, n: 3 });
}

// ---- crystals: shards from a surface; normal angle in radians (0 = up) ----
function crystals(pic, bx, by, ang, pal, seed, n = 4, size = 1, opt = {}) {
  const shards = [];
  for (let i = 0; i < n; i++) {
    const big = i === 0;
    const a = ang + (big ? 0 : (hash(i, seed, 1) - 0.5) * 1.5);
    const L = Math.round((big ? 15 : 6 + hash(i, seed, 2) * 9) * size), w = (big ? 3 : hash(i, seed, 3) < 0.5 ? 2 : 1.5) * Math.max(1, 0.55 + size * 0.45);
    const off = big ? 0 : (hash(i, seed, 4) - 0.5) * 12 * size;
    shards.push({ a, L, w, ox: bx + Math.cos(ang) * off, oy: by + Math.sin(ang) * off });
  }
  shards.reverse();
  for (const s of shards) {
    const ux = Math.sin(s.a), uy = -Math.cos(s.a), vx = -uy, vy = ux; // axis and its perpendicular
    const R = s.L + 4;
    for (let y = Math.floor(s.oy - R); y <= s.oy + R; y++) for (let x = Math.floor(s.ox - R); x <= s.ox + R; x++) {
      const px = x + 0.5 - s.ox, py = y + 0.5 - s.oy, u = px * ux + py * uy, v = px * vx + py * vy;
      if (u < -1 || u > s.L) continue;
      const tip = s.w * 1.6, hw = u > s.L - tip ? s.w * (s.L - u) / tip : s.w;
      if (Math.abs(v) > hw + 0.35) continue;
      const edge = Math.abs(v) > hw - 0.65 || u > s.L - 1.2;
      let col, k;
      if (edge) { col = pal.lo; k = 0.55; }
      else if (Math.abs(v) < 0.6 && u > 1) { col = pal.hi; k = 1; }
      else if (v < 0) { col = pal.mid; k = 0.85; }
      else { col = mix(pal.lo, pal.mid, 0.45); k = 0.6; }
      if (u < 1.5) { col = mix(col, pal.dk, 0.5); k *= 0.5; }
      if (opt.dim) k *= opt.dim;
      pic.glow(x, y, col, k);
    }
    if (hash(s.L, seed, 9) < 0.7) pic.glint.push({ x: Math.round(s.ox + ux * s.L * 0.7), y: Math.round(s.oy + uy * s.L * 0.7), col: pal.hi, need: 0.15, size: 2 });
  }
  const lx = bx + Math.sin(ang) * 8 * size, ly = by - Math.cos(ang) * 8 * size;
  pic.light({ x: lx, y: ly, col: pal.light, r: opt.r || 3.6 * size + 0.6, k: opt.k || 0.85, p: 1.6 });
  pic.motes.push({ x: lx + 6, y: ly - 4, col: pal.hi, a: 0.6 });
  pic.emitters.push({ kind: 'glint', x: lx, y: ly, col: pal.hi, n: 2 });
}

// ---- stalactites and stalagmites, rock-coloured, lit like the walls ----
function spike(pic, x, baseY, len, w, mat, down, opt = {}) {
  const p = MP[mat] || MP.rock, dir = down ? 1 : -1;
  for (let k = 0; k < len; k++) {
    const t = k / len, hw = Math.max(0.5, w * (1 - t) * (1 - t * 0.3)), y = baseY + dir * k;
    const sway = Math.round(Math.sin(k * 0.35 + x) * 0.6 * t);
    for (let dx = Math.floor(-hw); dx <= Math.ceil(hw); dx++) {
      if (Math.abs(dx) > hw + 0.25) continue;
      const X = x + dx + sway, edge = Math.abs(dx) >= hw - 0.6;
      let col = edge ? p.ol : dx < 0 ? p.hi : mix(p.a, p.b, hash(X, y, 3));
      if (!edge && dx > hw * 0.4) col = mul(col, 0.8);
      if (!edge && hash(X, y, 5) < 0.06) col = p.dk;
      pic.set(X, y, col);
    }
  }
  if (down && opt.drip) {
    const tipY = baseY + len, col = opt.dripCol || hex('#cfe8ff');
    pic.glow(x, tipY, col, 0.55);
    pic.drips.push({ x, y: tipY + 6 + (opt.dripFall || 0), len: 5, col, a: 0.85 });
    pic.emitters.push({ kind: 'drip', x, y: tipY + 1, to: opt.dripTo || tipY + 60, col });
  }
}

// ---- hanging roots near the surface, with pale glowing tips ----
function roots(pic, x, ceilY, len, seed, tipPal) {
  const brown = hex('#6b4a32'), dark = hex('#3e2a1c'), hair = hex('#8a6a4a');
  const grow = (sx, sy, L, depth) => {
    let xx = sx;
    for (let k = 0; k < L; k++) {
      const y = sy + k; if (pic.solid(xx, y) && k > 1) return;
      if (k > 1 && hash(k, sx * 7 + depth, seed) < 0.22) xx += hash(k, sx, seed + 1) < 0.5 ? -1 : 1;
      pic.set(xx, y, depth === 0 && k < L * 0.5 ? brown : hair);
      if (depth === 0 && k < L * 0.6) pic.set(xx + 1, y, k < L * 0.3 ? brown : dark);
      if (depth < 1 && k > 3 && hash(k, sx, seed + 2) < 0.1) grow(xx + (hash(k, sx, seed + 3) < 0.5 ? -1 : 1), y, Math.floor(L * 0.45), depth + 1);
      if (k === L - 1 && tipPal) { pic.glow(xx, y + 1, tipPal.hi, 0.9); if (depth === 0) pic.glow(xx, y + 2, tipPal.mid, 0.6); }
    }
  };
  grow(x, ceilY, len, 0);
  if (tipPal) pic.light({ x, y: ceilY + len, col: tipPal.light, r: 1.6, k: 0.3 });
}

// ---- fossils in the back wall: an ammonite spiral, and the ribs of something big ----
const BONE = hex('#c8bc9c'), BONE_D = hex('#8a8068'), BONE_L = hex('#e4dac0');
function ammonite(pic, cx, cy, r) {
  for (let y = -r; y <= r; y++) for (let x = -r; x <= r; x++) {
    const d = Math.sqrt(x * x + y * y); if (d > r + 0.3) continue;
    let a = Math.atan2(y, x); if (a < 0) a += Math.PI * 2;
    const turn = (d / r) * 3 - a / (Math.PI * 2); const f = turn - Math.floor(turn);
    let col = f < 0.22 ? BONE_D : f > 0.75 ? BONE_L : BONE;
    if (d > r - 0.8) col = BONE_D;
    if ((Math.floor(a * 9 + d) % 4 === 0) && f > 0.3 && f < 0.7) col = mix(BONE, BONE_D, 0.5);
    setBack(pic, cx + x, cy + y, col);
  }
}
function ribs(pic, x0, y0, len, seed) {
  // a curved spine with ribs hanging off it, half sunk in the stone
  let prev = null;
  for (let k = 0; k < len; k++) {
    const x = x0 + k, y = y0 + Math.round(Math.sin(k / len * Math.PI) * -4);
    setBack(pic, x, y, k % 4 === 0 ? BONE_D : BONE_L); setBack(pic, x, y + 1, BONE);
    if (k % 6 === 3 && k < len - 3) {
      const rl = 9 + Math.round(Math.sin(k / len * Math.PI) * 9 + hash(k, seed, 1) * 3);
      for (let j = 0; j < rl; j++) { const rx = x + Math.round(j * 0.35 - (j * j) / 40), ry = y + 2 + j; if (hash(j, k, seed) < 0.08) continue; setBack(pic, rx, ry, j % 5 === 0 ? BONE_D : BONE); setBack(pic, rx + 1, ry, BONE_D); }
    }
  }
  // the skull end
  for (let y = -3; y <= 3; y++) for (let x = 0; x < 9; x++) if ((x - 4) * (x - 4) / 20 + y * y / 9 < 1) setBack(pic, x0 + len + x, y0 + y, (x === 6 && y === -1) ? hex('#2a2620') : y === 3 ? BONE_D : BONE);
}
function setBack(pic, x, y, col) {
  if (!pic.inb(x, y)) return; const i = y * pic.w.PW + x; if (pic.solid(x, y) || !pic.back[i]) return;
  // bones sit in the back wall: they keep its depth shading
  const cur = pic.get(x, y), lum = (cur[0] + cur[1] + cur[2]) / 3; pic.alb.set(mul(col, 0.5 + lum * 1.6), i * 3);
}

// ---- a pool: fills the air of a floor dip below a level ----
function pool(pic, x0, x1, level, col = hex('#1e3c44')) {
  const w = pic.w;
  for (let x = x0; x <= x1; x++) for (let y = level; y < w.PH; y++) {
    if (w.sp(x, y)) break; const i = y * w.PW + x; pic.water[i] = 1; pic.waterTop[x] = level;
    pic.alb.set(mix(col, hex('#0a1418'), Math.min(1, (y - level) / 14)), i * 3); pic.back[i] = 0;
  }
}

// ---- heat cracks: glowing seams through rock, deep near the core ----
function heatCrack(pic, x, y, len, pal, seed, branch = true, dirY = 0) {
  let xx = x, yy = y;
  for (let k = 0; k < len; k++) {
    const h = hash(k, seed, 1);
    xx += 1; if (h < 0.3) yy += 1; else if (h < 0.6) yy -= 1; if (dirY && hash(k, seed, 5) < 0.3) yy += dirY;
    const t = k / len, core = t > 0.08 && t < 0.85; if (!pic.solid(xx, yy)) continue;
    pic.glow(xx, yy, core ? pal.mid : pal.lo, core ? 0.95 : 0.55);
    if (core && hash(k, seed, 2) < 0.3) pic.glow(xx, yy, pal.hi, 1.1);
    if (core && t > 0.2 && t < 0.7) pic.glow(xx, yy + 1, pal.lo, 0.5);
    if (branch && k > 4 && hash(k, seed, 3) < 0.03) heatCrack(pic, xx, yy, Math.floor(len * (0.2 + hash(k, seed, 4) * 0.25)), pal, seed + k, false, hash(k, seed, 6) < 0.5 ? 1 : -1);
  }
  if (branch) { for (let k = 0; k < len; k += 40) pic.light({ x: x + k + 12, y, col: pal.light, r: 3.2, k: 0.7, pen: 0.97 }); pic.emitters.push({ kind: 'ember', x: x + len * 0.5, y, col: pal.mid, n: 3 }); }
  if (branch) for (let s2 = 0; s2 < 3; s2++) pic.motes.push({ x: x + hash(s2, seed, 8) * len, y: y - 6 - hash(s2, seed, 9) * 18, col: pal.mid, a: 0.6 });
}

// ---- the hero, from the game's own parts; returns the visor ----
let HERO = null;
function hero(pic, x0, floorY) {
  if (!HERO) HERO = ['hero_shin', 'hero_thigh', 'hero_torso', 'hero_upperArm', 'hero_forearm', 'hero_helmet'].map((n) => [n, loadArt(n + '.png')]);
  const at = { hero_shin: [1, -11], hero_thigh: [3, -21], hero_torso: [1, -34], hero_upperArm: [5, -32], hero_forearm: [5, -25], hero_helmet: [0, -43] };
  for (const [n, img] of HERO) for (let y = 0; y < img.h; y++) for (let x = 0; x < img.w; x++) {
    const o = (y * img.w + x) * 4; if (img.data[o + 3] < 128) continue;
    pic.set(x0 + at[n][0] + x, floorY + at[n][1] + y, [img.data[o] / 255, img.data[o + 1] / 255, img.data[o + 2] / 255]);
  }
  return { x: x0 + 11, y: floorY - 38 };
}
function helmetBeam(pic, vx, vy, angDeg = 10) {
  const a = angDeg * Math.PI / 180;
  pic.light({ x: vx, y: vy, col: [1, 0.95, 0.84], r: 9, k: 1.15, p: 0.6, cone: { dx: Math.cos(a), dy: Math.sin(a), half: Math.PI / 6 }, pen: 0.9 });
  pic.light({ x: vx - 4, y: vy + 10, col: [1, 0.95, 0.84], r: 2.2, k: 0.6 }); // the spill on the suit
}

// ---- a geode that has been cracked open: a rough shell, a dark hollow, teeth that glow toward their tips ----
function openGeode(pic, cx, cy, r, pal, seed) {
  const teeth = []; for (let k = 0; k < 11; k++) teeth.push({ a: (k / 11) * Math.PI * 2 + hash(k, seed, 1) * 0.4, len: 0.35 + hash(k, seed, 2) * 0.4, w: 0.16 + hash(k, seed, 3) * 0.1 });
  for (let y = -r - 1; y <= r + 1; y++) for (let x = -r - 1; x <= r + 1; x++) {
    const d = Math.sqrt(x * x + y * y), a = Math.atan2(y, x), R = r * (0.9 + 0.1 * vnoise(a * 6 + 10, 0, 1, seed));
    if (d > R) continue;
    const X = cx + x, Y = cy + y;
    if (d > R - 1.2) { pic.set(X, Y, hex('#262430')); continue; }
    if (d > R - 2.6) { pic.set(X, Y, mix(hex('#7d7990'), hex('#a29eb6'), hash(X, Y, seed))); continue; }
    const inner = R - 2.6; let tooth = null;
    for (const t of teeth) { let da = Math.abs(a - t.a); if (da > Math.PI) da = Math.PI * 2 - da; const reach = inner * (1 - t.len); const hwid = t.w * (d - reach) / (inner - reach + 0.01); if (d > reach && da < hwid) { tooth = { t, f: (inner - d) / (inner - reach), side: a - t.a }; break; } }
    if (!tooth) { if (d > inner - 1.5) pic.glow(X, Y, mix(pal.lo, pal.dk, 0.4), 0.45); else pic.set(X, Y, mix(pal.dk, hex('#100c18'), 0.6)); continue; }
    const f = tooth.f, col = f > 0.75 ? pal.hi : f > 0.35 ? (tooth.side > 0 ? pal.mid : mix(pal.mid, pal.hi, 0.4)) : pal.lo;
    pic.glow(X, Y, col, f > 0.75 ? 1 : f > 0.35 ? 0.8 : 0.5);
  }
  for (let k = 0; k < 3; k++) { const t = teeth[(k * 4 + seed) % teeth.length], rr = (r - 2.6) * (1 - t.len); pic.glint.push({ x: Math.round(cx + Math.cos(t.a) * rr), y: Math.round(cy + Math.sin(t.a) * rr), col: pal.hi, need: 0.1, size: 2 }); }
  pic.light({ x: cx, y: cy, col: pal.light, r: 3.6, k: 0.85, pen: 0.97 });
}

// moss that hangs from a ceiling run
function ceilMoss(pic, x0, x1, fromY, pal, seed, dens) {
  const w = pic.w, E2 = { vnoise, hash };
  for (let x = x0; x <= x1; x++) {
    let y = fromY + 4; while (y > 0 && !w.sp(x, y - 1)) y--; if (w.sp(x, y)) continue;
    if (E2.vnoise(x, 0, 6, seed) < 1 - dens) continue;
    const h = E2.hash(x, seed, 2), len = h < 0.15 ? 4 : h < 0.45 ? 2 : 1;
    for (let k = 0; k < len; k++) pic.glow(x, y + k, k === len - 1 && len > 1 ? pal.hi : pal.lo, k === len - 1 && len > 1 ? 1 : 0.5);
  }
  for (let x = x0 + 10; x < x1; x += 28) pic.light({ x, y: fromY + 6, col: pal.light, r: 2.6, k: 0.38 });
}


module.exports = { ceilMoss, PAL, glowcaps, moss, mossOnSurface, thread, web, threads, crystals, spike, roots, ammonite, ribs, pool, heatCrack, hero, helmetBeam, openGeode };

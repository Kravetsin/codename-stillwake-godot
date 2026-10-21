// 9. Stillwake: our tiles (dark-outlined, muted, 3-4 tones like the crate and the pod), the cave's
// moss and luminous life, under a soft alien daylight; thin glowing particles carry the joy.
module.exports = function (K) {
  const { Cv, Layer, W, H, hex, mix, mul, clamp, hash, vnoise, fbm, ramp, rampSmooth, smooth, surfY, solidPx, SPOT, SUN, dot3 } = K;
  const cv = new Cv(), E = new Float32Array(W * H * 3);
  const glow = (x, y, c, k = 1) => { x |= 0; y |= 0; if (x < 0 || y < 0 || x >= W || y >= H) return; c = hex(c); cv.set(x, y, c); const i = (y * W + x) * 3; E[i] += c[0] * k; E[i + 1] += c[1] * k; E[i + 2] += c[2] * k; };
  const [sx, sy] = SPOT.sun;
  // sky: deep teal-blue overhead to a warm pale horizon, finely dithered
  cv.each((x, y) => { const t = y / 290 + (K.bayer(x, y) - 0.5) * 0.02; return rampSmooth(['#3e6fb0', '#5f93c8', '#93bfdc', '#cfe0dc', '#f1e3c2'], t); });
  // a gas giant with a ring, pale in the day sky
  const gx = 170, gy = 88, gr = 52;
  for (let y = gy - 70; y <= gy + 70; y++) for (let x = gx - 110; x <= gx + 110; x++) {
    const dx = x - gx, dy = y - gy, d = Math.hypot(dx, dy);
    const rx = dx * Math.cos(-0.25) - dy * Math.sin(-0.25), ry = (dx * Math.sin(-0.25) + dy * Math.cos(-0.25)) / 0.22, rd = Math.hypot(rx, ry);
    const ringFront = rd > gr * 1.32 && rd < gr * 1.95 && (ry > 0 || d > gr) && !((rd > gr * 1.55 && rd < gr * 1.6));
    if (d < gr) {
      const band = Math.floor((dy + 6 * vnoise(dx, dy, 18, 3)) / 9) % 3, lit = clamp(0.5 + (dx * 0.5 - dy * 0.6) / gr * 0.6);
      let c = mix(band === 0 ? '#c9d8d4' : band === 1 ? '#b3c8cc' : '#dbe3d6', '#7f9cb4', 1 - lit);
      cv.blend(x, y, c, 0.55 - (d > gr - 1.5 ? 0.15 : 0));
    }
    if (ringFront) cv.blend(x, y, rd < gr * 1.6 ? '#e8efe8' : '#c4d6dc', 0.32);
  }
  // high thin cloud streaks
  for (let y = 20; y < 150; y++) for (let x = 0; x < W; x++) { const n = fbm(x * 0.35, y * 3, 60, 41, 4), band = smooth(0.58, 0.75, n) * smooth(150, 60, y); if (band > 0.02) cv.blend(x, y, '#fbf7ec', band * 0.55); }
  // the sun: a white core, a warm halo and the thin cross glint of our fx
  for (let y = sy - 90; y <= sy + 90; y++) for (let x = sx - 90; x <= sx + 90; x++) { const d = Math.hypot(x - sx, y - sy); if (d < 9) glow(x, y, d < 7 ? '#ffffff' : '#fff6d8', 0.9); else cv.add(x, y, [1, 0.92, 0.72], 0.35 * Math.exp(-d / 22)); }
  for (let k = 10; k < 46; k++) { const f = 0.5 * (1 - k / 46) ** 2; cv.add(sx + k, sy, [1, 0.96, 0.85], f); cv.add(sx - k, sy, [1, 0.96, 0.85], f); cv.add(sx, sy + k, [1, 0.96, 0.85], f * 0.7); cv.add(sx, sy - k, [1, 0.96, 0.85], f * 0.7); }
  // far range: layered mesas lost in the bright haze, rim-lit toward the sun
  const far = (x) => 168 - fbm(x, 0, 150, 7, 4) * 64 - smooth(560, 760, x) * smooth(900, 780, x) * 26;
  const farB = (x) => 196 - fbm(x, 0, 90, 13, 3) * 40;
  for (let x = 0; x < W; x++) {
    const f = far(x), sl = far(x + 2) - far(x - 2);
    for (let y = Math.floor(f); y < H; y++) { const t = clamp((y - f) / 90); let c = mix('#9bb7cc', '#c6d6d6', t); c = mix(c, sl < 0 ? '#dce6e2' : '#8aa6bc', K.clamp(Math.abs(sl) * 0.12) * 0.5 * Math.exp(-(y - f) / 30)); cv.set(x, y, mix(c, '#efe6cc', smooth(150, 240, y) * 0.6)); }
    const b = farB(x), sb = farB(x + 2) - farB(x - 2);
    for (let y = Math.floor(b); y < H; y++) { let c = mix('#7d9fb2', '#a8bfc4', clamp((y - b) / 60)); if (y - b < 1.5 && sb < -0.6) c = mix(c, '#cddcd8', 0.6); cv.set(x, y, mix(c, '#e9e2c8', smooth(170, 250, y) * 0.55)); }
  }
  // near hills with spindly branching alien trees in silhouette, their seed lights faint
  const hill = (x) => 236 - fbm(x, 0, 110, 19, 3) * 26;
  const HL = new Layer();
  for (let x = 0; x < W; x++) for (let y = Math.floor(hill(x)); y < H; y++) HL.set(x, y, y - hill(x) < 1.5 ? '#86a8a0' : '#5f8684');
  for (let k = 0; k < 11; k++) {
    const tx = 20 + k * 88 + hash(k, 1, 61) * 50, sk = K.treeSkeleton({ x: tx, y: hill(tx) + 2, len: 12 + hash(k, 2, 61) ** 2 * 34, lift: 1, w: 3, seed: 600 + k, depth: 2, spread: 0.8, childLen: 0.62, step: 6, wiggle: 0.3, minW: 1 });
    const r = K.rasterSegs(sk.segs); for (let i = 0; i < W * H; i++) if (!Number.isNaN(r.U[i])) HL.set(i % W, (i / W) | 0, '#5f8684');
    for (const t of sk.tips) { for (let y = -4; y <= 2; y++) for (let x = -6; x <= 6; x++) if ((x * x) / 36 + (y * y) / 9 < 1 && vnoise(t.x + x, t.y + y, 2, k) > 0.3) HL.set(t.x + x, t.y + y, y < -1 ? '#7fa29a' : '#5f8684'); }
  }
  for (let i = 0; i < W * H; i++) if (HL.a[i]) { const y = (i / W) | 0; HL.c.set(mix(HL.get(i % W, y), '#e6dfc4', smooth(200, 280, y) * 0.5 + 0.12), i * 3); }
  HL.onto(cv);
  // a band of warm haze where the land meets the sky
  for (let y = 200; y < 290; y++) for (let x = 0; x < W; x++) cv.blend(x, y, '#f4e8c8', 0.18 * smooth(200, 260, y) * (0.7 + 0.3 * vnoise(x, y, 40, 77)));

  // ---- ground: the cave's soil and moss, daylit, with a dark outline where it meets the air ----
  const soil = (x, y) => {
    let c = mix('#5a4434', '#6a5040', vnoise(x, y, 7, 1)); const h = hash(x, y, 2);
    if (h < 0.07) c = hex('#45342a'); else if (h > 0.955) c = hex('#7d604a');
    const bx = Math.floor(x / 6), by = Math.floor(y / 6);
    if (hash(bx, by, 3) < 0.09) { const ox = bx * 6 + 1 + Math.floor(hash(bx, by, 4) * 3), oy = by * 6 + 1; if (x >= ox && x < ox + 3 && y >= oy && y < oy + 2) c = (y === oy) ? hex('#8a8074') : hex('#5e574e'); }
    return c;
  };
  const rock = (x, y) => { let c = mix('#5b6170', '#676e7e', vnoise(x, y, 9, 4)); const st = ((y + 4 * vnoise(x, y, 14, 5)) % 9 + 9) % 9; if (st < 0.9) c = hex('#4a4f5c'); if (hash(x, y, 6) > 0.965) c = hex('#808898'); return c; };
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    if (!solidPx(x, y)) continue;
    const d = y - surfY(x), rockLine = 78 + vnoise(x, 0, 40, 21) * 26;
    let c = d > rockLine ? rock(x, y) : soil(x, y);
    const mossD = 3 + Math.round(vnoise(x, 0, 5, 8) * 3) + (hash(x, 3, 9) < 0.15 ? 2 : 0);
    if (d < mossD) c = d === 0 ? hex('#9ccf9c') : d === 1 ? hex('#78b48a') : d < mossD - 1 ? hex('#55906a') : hex('#3f6f52');
    else if (d === mossD && hash(x, 0, 12) < 0.5) c = hex('#3f6f52');
    else if (d < mossD + 3) c = mul(c, 0.72); // shade under the moss
    const air = !solidPx(x - 1, y) || !solidPx(x + 1, y);
    if (air && d > 1) c = hex('#24190f');
    else if (!solidPx(x, y - 1) && !solidPx(x - 1, y - 1) && !solidPx(x + 1, y - 1) && d === 0) c = hex('#b8e2b0');
    // daylight soaks in from the top, the way the game's light fades into ground
    const k = 1.08 - smooth(10, 120, d) * 0.62;
    cv.set(x, y, mul(c, k));
  }
  // moss filaments: thin stems with luminous tips, denser on flats
  for (let x = 0; x < W; x++) {
    const y0 = surfY(x) - 1; if (hash(x, 1, 30) < 0.4) continue;
    const h = 1 + Math.floor(hash(x, 2, 30) ** 1.6 * 7), lean = hash(x, 3, 30) < 0.5 ? 0 : (hash(x, 4, 30) < 0.5 ? -1 : 1);
    for (let k = 0; k < h; k++) { const xx = x + (k > h / 2 ? lean : 0); if (k === h - 1 && h > 3 && hash(x, 5, 30) < 0.35) glow(xx, y0 - k, '#bff8e0', 0.35); else cv.set(xx, y0 - k, k < h / 2 ? '#5f9c74' : '#86c492'); }
  }

  // ---- the tree: a dark trunk that forks, wide flat crowns, hanging fronds with seed lights ----
  const T = new Layer(), tx = SPOT.tree, base = surfY(tx) + 2;
  const sk = K.treeSkeleton({ x: tx, y: base, len: 84, w: 13, seed: 50, depth: 3, fork: 2, spread: 1.15, childLen: 0.62, childW: 0.62, step: 9, wiggle: 0.16, lift: 1.2, taper: 0.9, trunkShoot: 0.12, shoot: 0.2, minW: 2, firstSplit: 5 });
  const tips = sk.tips;
  const clumps = []; let n = 0;
  for (const t of tips) { const r = 13 + hash(n, 1, 5) * 5; clumps.push({ x: t.x, y: t.y - 2, r: r * 1.15, sq: 1.7, z: -1, core: true }); for (let i = 0; i < 3; i++) { clumps.push({ x: t.x + (hash(n, i, 6) - 0.5) * 26, y: t.y - 3 - hash(n, i, 7) * 6, r: r * (0.55 + hash(n, i, 8) * 0.35), sq: 1.6, z: hash(n, i, 9) }); } n++; }
  const fol = K.foliage(clumps, { leaf: 4, lobes: 0.4, ragged: 3, holes: 0.12 });
  const trunkPal = ['#2a2026', '#3c2f33', '#54444a', '#6e5e60'];
  K.paintTree(T, sk, fol, (x, y, u, v, d) => {
    const s = u * 0.8 + 0.15 + (hash(Math.round(v / 3), Math.round(u * 3), 4) - 0.5) * 0.25;
    let c = ramp(trunkPal, (s + 1) / 2); if (Math.abs(u) < 0.5 && hash(Math.round(v / 5), Math.round(u * 2), 8) < 0.12) c = hex('#2a2026');
    return c;
  }, (x, y, info) => {
    const s = K.leafLight(info, { clump: 0.7, leafK: 0.4, gapK: 0.3, coreK: 0.3 });
    return ramp(['#1f3d3a', '#2e5a50', '#3f7a62', '#5c9c72', '#95c88a', '#d6ecaa'], (s + 0.55) / 1.5);
  });
  // fronds: thin strands hanging from the crowns' undersides, a seed light at some tips
  const lights = [];
  for (const [k, c] of clumps.entries()) {
    if (c.core) continue;
    for (let j = 0; j < 7; j++) {
      const fx = Math.round(c.x + (hash(k, j, 40) - 0.5) * c.r * 1.6); let fy = 0; for (let y = Math.round(c.y); y < Math.round(c.y + c.r); y++) if (T.has(fx, y)) fy = y; if (!fy) continue;
      const len = 5 + Math.floor(hash(k, j, 41) * 16), sway = hash(k, j, 42) < 0.5 ? 1 : -1;
      for (let q = 1; q <= len; q++) { const xx = fx + Math.round(Math.sin(q / len * 1.6) * sway * 1.5); T.set(xx, fy + q, q < len * 0.5 ? '#2e5a50' : '#3f7a62'); }
      if (hash(k, j, 43) < 0.16) lights.push([fx + Math.round(Math.sin(1.6) * sway * 1.5), fy + len + 1]);
    }
  }
  T.outline(() => hex('#141a1c'), 'all'); T.onto(cv);
  for (const [x, y] of lights) { glow(x, y, '#c8fff0', 0.7); glow(x, y + 1, '#7fe8d4', 0.35); }

  // ---- bush with luminous berries ----
  const B = new Layer(), bx = SPOT.bush, bb = surfY(bx) + 1;
  const bf = K.foliage([{ x: bx - 10, y: bb - 7, r: 9, sq: 1.2 }, { x: bx + 8, y: bb - 6, r: 9, sq: 1.2 }, { x: bx - 1, y: bb - 12, r: 11, sq: 1.3, z: 1 }], { leaf: 4, lobes: 0.4, ragged: 3 });
  for (let i = 0; i < W * H; i++) if (bf.id[i] >= 0 && (i / W | 0) < bb) { const x = i % W, y = (i / W) | 0; const s = K.leafLight({ n: [bf.N[i * 3], bf.N[i * 3 + 1], bf.N[i * 3 + 2]], leaf: [bf.Lf[i * 3], bf.Lf[i * 3 + 1]], gap: bf.Lf[i * 3 + 2], clump: bf.clumps[bf.id[i]] }); B.set(x, y, ramp(['#1f3d3a', '#2e5a50', '#3f7a62', '#5c9c72', '#95c88a'], (s + 0.55) / 1.4)); }
  B.outline(() => hex('#141a1c'), 'nottop'); B.onto(cv);
  for (const [dx, dy] of [[-12, -8], [-4, -15], [6, -10], [12, -5], [0, -4]]) { glow(bx + dx, bb + dy, '#ffd0f0', 0.9); glow(bx + dx + 1, bb + dy, '#ff7ad0', 0.6); glow(bx + dx, bb + dy + 1, '#ff7ad0', 0.6); glow(bx + dx + 1, bb + dy + 1, '#c04a9a', 0.3); }

  // ---- glow flowers and tufts ----
  const bell = (x, col, hi) => { const y = surfY(x) - 1, h = 6 + Math.floor(hash(x, 1, 3) * 6); for (let k = 0; k < h; k++) cv.set(x + (k > h - 3 ? 1 : 0), y - k, '#4f8a66'); const hx = x + 1, hy = y - h; glow(hx, hy, hi, 0.9); glow(hx - 1, hy, col, 0.6); glow(hx + 1, hy, col, 0.6); glow(hx, hy - 1, col, 0.5); glow(hx - 1, hy - 1, col, 0.4); glow(hx + 1, hy - 1, col, 0.4); glow(hx - 2, hy + 1, col, 0.45); glow(hx + 2, hy + 1, col, 0.45); glow(hx, hy + 1, hi, 0.5); cv.set(hx - 1, hy + 2, '#3f6f52'); cv.set(hx + 2, hy + 2, '#3f6f52'); };
  for (const [x, c, h] of [[70, '#7fe8d4', '#e6fff8'], [96, '#b49aff', '#f2ecff'], [318, '#7fe8d4', '#e6fff8'], [372, '#ffd27a', '#fff6e0'], [420, '#b49aff', '#f2ecff'], [598, '#7fe8d4', '#e6fff8'], [742, '#ffd27a', '#fff6e0'], [790, '#7fe8d4', '#e6fff8'], [870, '#b49aff', '#f2ecff']]) bell(x, c, h);

  // ---- rock: our rock material, outlined, with mica that catches the sun ----
  const R = new Layer(), rx = SPOT.rock, rb = surfY(rx) + 2;
  for (let y = rb - 20; y <= rb; y++) for (let x = rx - 17; x <= rx + 17; x++) {
    const dx = (x - rx) / 17, dy = (y - rb + 3) / 17, e = dx * dx + dy * dy + (vnoise(x, y, 5, 3) - 0.5) * 0.25; if (e > 1) continue;
    const facet = vnoise(x, y, 7, 8) > 0.55 ? 0.25 : 0, s = dx * 0.55 - dy * 0.75 + facet;
    R.set(x, y, ramp(['#3f4450', '#525866', '#687080', '#8a92a0', '#b6bcc4'], (s + 1) / 2.1));
  }
  R.outline(() => hex('#1a1c24')); R.onto(cv);
  for (const [x, y] of [[rx + 6, rb - 15], [rx - 3, rb - 10], [rx + 11, rb - 7]]) K.glint(cv, x, y, hex('#fff2c8'), 0.8, 2);

  // the game's own hero and crate, unchanged
  K.contactShadow(cv, hex('#1d2a22'), 0.4);
  // bloom from what glows, then luminous motes drifting in the light
  K.bloom(cv, E, 0.9, 1.4, 2, 7);
  for (let k = 0; k < 46; k++) {
    const x = hash(k, 1, 90) * W, y = 120 + hash(k, 2, 90) * 160; if (solidPx(x, y)) continue;
    const near = Math.abs(x - SPOT.tree) < 90 || Math.abs(x - SPOT.bush) < 40;
    K.mote(cv, x, y, near && hash(k, 3, 90) < 0.6 ? hex('#9ff0dc') : hex('#fff0c0'), 0.35 + hash(k, 4, 90) * 0.45, hash(k, 5, 90) < 0.2);
  }
  K.heroAndCrate(cv); // last, so nothing touches it
  return cv;
};

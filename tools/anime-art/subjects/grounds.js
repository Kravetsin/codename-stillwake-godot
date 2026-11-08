// The ground that is not stone or soil: the ruins' alloy, bedrock, the lava's rock and the homes' plaster, under the names the ship's atlas reads (wall_, back_, wall-edge_, top_, detail_).
(function () {
'use strict';
const { ell, poly, rng, bake } = Anime;
const W = 128, H = 64;
const box = (x, y, w, h) => poly(x, y, x + w, y, x + w, y + h, x, y + h);
const pts = (list, dx, dy) => poly(list.flatMap(([x, y]) => [x + (dx || 0), y + (dy || 0)]));

/** Facets of rock on the sheet's torus, as caves.js cuts stone: a jittered grid of points, each one's cell by half-planes, its neighbours' copies included so it tiles. */
function cellsOf(n, seed) {
  const r = rng(seed), seeds = [];
  const across = Math.round(Math.sqrt((n * W) / H)), down = Math.round(n / across);
  for (let j = 0; j < down; j++) for (let i = 0; i < across; i++) {
    if (r() < .1) continue;
    seeds.push([(i + .15 + r() * .7) * W / across, (j + .15 + r() * .7) * H / down]);
  }
  return seeds.map((p, k) => {
    let cell = [[p[0] - 40, p[1] - 40], [p[0] + 40, p[1] - 40], [p[0] + 40, p[1] + 40], [p[0] - 40, p[1] + 40]];
    seeds.forEach((q0, m) => {
      for (const dx of [-W, 0, W]) for (const dy of [-H, 0, H]) {
        if (m === k && !dx && !dy) continue;
        const q = [q0[0] + dx, q0[1] + dy], mx = (p[0] + q[0]) / 2, my = (p[1] + q[1]) / 2, nx = q[0] - p[0], ny = q[1] - p[1];
        if (nx * nx + ny * ny > 6400) continue;
        const out = [];
        for (let i = 0; i < cell.length; i++) {
          const a = cell[i], b = cell[(i + 1) % cell.length], da = (a[0] - mx) * nx + (a[1] - my) * ny, db = (b[0] - mx) * nx + (b[1] - my) * ny;
          if (da <= 0) out.push(a);
          if (da * db < 0) { const t = da / (da - db); out.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]); }
        }
        cell = out;
      }
    });
    return cell;
  });
}
const grown = (cell, k) => {
  const cx = cell.reduce((s, p) => s + p[0], 0) / cell.length, cy = cell.reduce((s, p) => s + p[1], 0) / cell.length;
  return poly(cell.flatMap((p) => [cx + (p[0] - cx) * k, cy + (p[1] - cy) * k]));
};
/** An irregular patch round a point: where a coat has come away, or a mass of molten rock. Its place is given, only its edge is torn. */
function torn(r, x, y, rx, ry) {
  const p = [];
  for (let i = 0; i < 12; i++) { const t = (i / 12) * Math.PI * 2, k = .68 + r() * .5; p.push(x + Math.cos(t) * rx * k, y + Math.sin(t) * ry * k); }
  return poly(p);
}

// ---- Bedrock: the deep, one dark body, its fractures long.
const BEDROCK = [
  { base: '#39363f', shade: '#28252d', lite: '#47444e', spec: '#57545e', ink: '#0d0b10' },
  { base: '#34313a', shade: '#24222a', lite: '#413e48', spec: '#514e58', ink: '#0d0b10' },
  { base: '#3e3b45', shade: '#2c2932', lite: '#4c4954', spec: '#5c5964', ink: '#0d0b10' },
];
const AMETHYST = { base: '#7c6ea6', shade: '#4e4278', deep: '#362c5a', lite: '#b4a8d8', spec: '#ece6ff', ink: '#120e20' };
const wallBedrock = {
  w: W, h: H, tile: true, K: 2,
  draw(a) {
    const r = rng(0xbe01), cells = cellsOf(24, 0xbe02);
    for (const cell of cells) a.shape(grown(cell, 1.03), BEDROCK[Math.floor(r() * 3)], { outer: false, edge: false, shade: 1.3 });
    for (const cell of cells) for (let i = 0; i < cell.length; i++) {
      const p = cell[i], q = cell[(i + 1) % cell.length];
      if (r() < .24) a.line(`M${p[0]} ${p[1]}L${q[0]} ${q[1]}`, { w: 2.4, c: BEDROCK[0].ink });
    }
    // Long fractures through the masses, placed by hand: pressure, not paving.
    for (const d of ['M8 6L19 13L24 22L37 28', 'M60 40L71 36L78 44L96 47L102 58', 'M92 4L100 12L98 19L113 26', 'M30 46L38 52L37 60']) a.line(d, { w: 3, c: BEDROCK[0].ink });
  },
};
const backBedrock = {
  w: W, h: H, tile: true, K: 2,
  draw(a) {
    const r = rng(0xbe11);
    for (const cell of cellsOf(16, 0xbe12)) a.flat(grown(cell, 1.02), ['#141319', '#16151b', '#121116'][Math.floor(r() * 3)]);
  },
};

// ---- Alloy: the ruins' metal. Long plates made true and laid in courses, pitted in rows; a dead line of inlay on a few.
const ALLOY = [
  { base: '#4c6570', shade: '#374b56', lite: '#6c868e', spec: '#a6bcbf', ink: '#141d24' },
  { base: '#445d68', shade: '#31444e', lite: '#627c85', spec: '#9cb2b5', ink: '#141d24' },
  { base: '#546e79', shade: '#3e525d', lite: '#748e96', spec: '#aec2c4', ink: '#141d24' },
];
// A course of plates: its start, then each plate's width, tone, and what it carries: 'r' a row of pits along its top, 'f' along its foot, 'i' a line of inlay, 'c' a crack, 'd' a dent.
const COURSES = [
  [0, [32, 0, 'r'], [28, 1, 'f'], [36, 2, 'r'], [32, 1, '']],
  [-14, [30, 1, 'f'], [34, 0, 'c'], [26, 2, 'rf'], [38, 1, 'r']],
  [-4, [36, 2, ''], [24, 1, 'r'], [40, 0, 'f'], [28, 2, 'r']],
  [-20, [28, 0, 'r'], [38, 1, 'i'], [30, 2, 'd'], [32, 0, 'f']],
  [0, [34, 1, 'f'], [30, 2, 'r'], [32, 0, 'c'], [32, 1, 'rf']],
  [-10, [26, 2, 'r'], [36, 0, 'f'], [34, 1, 'i'], [32, 2, '']],
  [-2, [38, 0, 'rf'], [28, 2, ''], [30, 1, 'r'], [32, 0, 'd']],
  [-16, [30, 2, 'f'], [32, 1, 'r'], [36, 0, ''], [30, 2, 'r']],
];
function alloyPlate(a, x, y, w, h, m, marks, teal) {
  a.shape(box(x, y, w, h), m, { outer: false, edge: false, lite: box(x, y, w, .7), shade: [box(x, y + h - 1.2, w, 1.2), box(x + w - .8, y, .8, h)], spec: box(x + 1.2, y + .15, Math.min(w * .3, 9), .4) });
  const row = (py) => { for (let k = 0, n = Math.floor((w - 6) / 3); k < n; k++) { if (k % 7 === 5) continue; a.flat(box(x + 3 + k * 3, py, .8, .55), m.ink); a.flat(box(x + 3 + k * 3, py + .55, .8, .22), m.lite); } };
  if (marks.includes('r')) row(y + 1.6);
  if (marks.includes('f')) row(y + h - 3);
  if (marks.includes('i')) {
    // Its inlay, gone dark but for a stretch or two.
    a.flat(box(x + 3, y + h / 2 - .3, w - 6, .6), '#22383c');
    a.flat(box(x + 6, y + h / 2 - .22, 7, .44), teal);
    a.flat(box(x + w - 12, y + h / 2 - .22, 4, .44), teal);
  }
  if (marks.includes('c')) a.line(`M${x + 8} ${y + 1}L${x + 11} ${y + 3.4}L${x + 10} ${y + 5}L${x + 14} ${y + 7}`, { w: 2.2 });
  if (marks.includes('d')) { a.flat(pts([[x + 9, y + 2.4], [x + 15, y + 2], [x + 17, y + 4.4], [x + 11, y + 5.4]]), m.shade); a.flat(pts([[x + 11, y + 5.4], [x + 17, y + 4.4], [x + 16.4, y + 5.2], [x + 11.4, y + 6]]), m.lite); }
}
const seamsOf = (a, courses, h, ink) => courses.forEach((c, i) => {
  a.flat(box(-2, i * h - .3, W + 4, .6), ink);
  let x = c[0];
  for (const p of c.slice(1)) { a.flat(box(x - .28, i * h, .56, h), ink); x += p[0]; }
});
const wallAlloy = {
  w: W, h: H, tile: true, K: 2,
  draw(a) {
    COURSES.forEach((c, i) => { let x = c[0]; for (const [w, t, marks] of c.slice(1)) { alloyPlate(a, x, i * 8, w, 8, ALLOY[t], marks, '#5cb4a8'); x += w; } });
    seamsOf(a, COURSES, 8, ALLOY[0].ink);
  },
};
const BACK_ALLOY = [
  { base: '#1f2c31', shade: '#182328', lite: '#28373d', spec: '#28373d', ink: '#0b1216' },
  { base: '#1c282d', shade: '#162025', lite: '#253339', spec: '#253339', ink: '#0b1216' },
];
const BACK_COURSES = [[0, [32, 0, 1], [32, 1, 0], [34, 0, 0], [30, 1, 1]], [-14, [30, 1, 0], [34, 0, 1], [32, 1, 0], [32, 0, 0]], [-2, [36, 0, 0], [28, 1, 1], [32, 0, 0], [32, 1, 0]], [-18, [28, 1, 1], [36, 0, 0], [32, 1, 1], [32, 0, 0]]];
const backAlloy = {
  w: W, h: H, tile: true, K: 2,
  draw(a) {
    BACK_COURSES.forEach((c, i) => {
      let x = c[0];
      for (const [w, t, pits] of c.slice(1)) {
        a.shape(box(x, i * 16, w, 16), BACK_ALLOY[t], { outer: false, edge: false, lite: box(x, i * 16, w, .6), shade: [box(x, i * 16 + 14.9, w, 1.1), box(x + w - .7, i * 16, .7, 16)] });
        if (pits) for (let k = 0, n = Math.floor((w - 8) / 4); k < n; k++) a.flat(box(x + 4 + k * 4, i * 16 + 2.4, .7, .5), '#111a1e');
        x += w;
      }
    });
    seamsOf(a, BACK_COURSES, 16, BACK_ALLOY[0].ink);
  },
};

// ---- Lava: the rock's molten body, in hard patches of the lava view's colours; its back, a dark crust with the same fire in its cracks.
const MOLTEN = { base: '#ff7a14', shade: '#e44c10', lite: '#ffb030', spec: '#ffe08a', ink: '#5a1c08' };
const GOLD = '#ffa426', PALE = '#ffdc6c';
// Masses of gold and of deep orange, placed: x, y, half width, half height.
const LAVA_GOLD = [[14, 12, 13, 8], [52, 8, 9, 6], [92, 14, 16, 9], [122, 34, 9, 7], [30, 40, 15, 9], [72, 34, 8, 6], [100, 52, 12, 7], [58, 56, 11, 6], [8, 56, 8, 6]];
const LAVA_DEEP = [[34, 18, 8, 6], [70, 14, 7, 5], [112, 26, 6, 5], [12, 32, 7, 5], [52, 36, 8, 5], [88, 40, 6, 5], [38, 58, 7, 4], [80, 58, 6, 4], [120, 54, 5, 5]];
// The pale cracks, running as far as they do in the logical sheet, each by hand.
const LAVA_CRACKS = [
  'M0 22L10 20L16 26L28 24L34 30L44 28L50 22L62 24L68 20L74 26L74 38L82 42L92 38L104 40L110 34L122 38L128 22',
  'M28 24L26 12L32 4L28 0M26 64L30 56L24 48L30 42L34 30',
  'M62 24L60 10L66 4L64 0M64 64L60 54L68 48L74 38',
  'M104 40L108 50L100 58L104 64M104 0L100 8L108 16L110 34',
  'M16 26L8 34L12 44L4 50L10 58',
];
const wallLava = {
  w: W, h: H, tile: true, K: 2,
  draw(a) {
    const r = rng(0x1a7a);
    a.flat(box(-2, -2, W + 4, H + 4), MOLTEN.base);
    for (const [x, y, rx, ry] of LAVA_DEEP) a.flat(torn(r, x, y, rx, ry), MOLTEN.shade);
    for (const [x, y, rx, ry] of LAVA_GOLD) a.flat(torn(r, x, y, rx, ry), GOLD);
    for (const d of LAVA_CRACKS) a.line(d, { w: 5, c: PALE });
  },
};
const backLava = {
  w: W, h: H, tile: true, K: 2,
  draw(a) {
    const r = rng(0x1a7b);
    a.flat(box(-2, -2, W + 4, H + 4), '#2c0f08');
    for (const [x, y, rx, ry] of LAVA_GOLD) a.flat(torn(r, x, y, rx * .9, ry * .9), '#3e150a');
    for (const [x, y, rx, ry] of LAVA_DEEP) a.flat(torn(r, x, y, rx, ry), '#20100a');
    for (const d of LAVA_CRACKS) a.line(d, { w: 4.5, c: '#e4641c' });
  },
};
// The bright cooling splashes over a lava cell: patches in place, by the logical sheet's cells. Each a list of outlines, in the picture's own pixels.
const SPLASH = [
  [[[4, 0], [10, 0], [9.6, 2.6], [6, 3], [4.4, 1.6]], [[4, 10], [8, 9], [10, 11], [9.4, 14.4], [6, 15.6], [4, 13]]],
  [[[0, 0], [5, 0], [5, 1.6], [1, 2.2]]],
  [[[3, 0], [8, 0], [7, 2], [5, 2.4]], [[8, 13], [12, 10], [13, 12], [11, 15.4], [9, 16]]],
  [[[0, 5], [6, 5], [6, 8], [0, 8]]],
  [[[12, 0], [15, 0], [14.4, 3], [12.4, 2.8]]],
  [[[0, 0], [6, 0], [5, 2.2], [2, 2.6]], [[0, 11], [8.6, 11], [8.8, 16], [7, 18], [5, 15], [3, 13.4], [0, 13]]],
  [[[4, 3], [7, 3], [8, 12], [8, 16], [6, 16], [4, 13]]],
  [[[0, 2], [3, 2], [3, 8], [0, 8]]],
];
const SPLASH_FEATURES = [
  [[[0, 2], [3, 3], [2, 8], [3, 14], [0, 15]], [[14, 12], [19, 10], [21, 13], [21, 16], [14, 16]], [[4, 18], [9, 18], [9, 22], [4, 22]], [[9, 19], [13, 18], [15, 21], [12, 22], [9, 22]], [[11, 25], [13, 26], [12, 28], [10, 27]], [[16, 28], [20, 28], [21, 32], [16, 32]], [[18, 6], [22, 2], [24, 4], [24, 8], [20, 8]]],
  [[[24, 8], [28, 8], [29, 12], [24, 12]], [[9, 13], [13, 13], [13, 17], [9, 16]], [[8, 19], [11, 19], [11, 26], [8, 26]], [[14, 18], [22, 17], [23, 20], [14, 21]], [[4, 2], [8, 2], [8, 5], [4, 5]], [[0, 22], [5, 22], [4, 25], [0, 24]]],
  [[[2, 0], [6, 0], [8, 3], [14, 7], [13, 13], [20, 17], [24, 12], [28, 14], [30, 18], [24, 22], [18, 26], [8, 26], [6, 22], [10, 18], [6, 12], [3, 8]], [[34, 0], [38, 2], [40, 10], [48, 12], [48, 24], [40, 24], [36, 18], [34, 8]], [[30, 26], [38, 26], [38, 30], [30, 30]], [[2, 32], [6, 32], [7, 38], [3, 38]], [[10, 34], [18, 36], [20, 42], [14, 44], [10, 40]], [[40, 32], [44, 32], [44, 40], [40, 42]]],
];
function splash(list, side) {
  return { w: side, h: side, draw(a) { for (const o of list) { a.flat(pts(o), GOLD); } } };
}

// ---- Plaster: smoothed stone under lime wash. One body, its coat worn through at the courses and the damp foot; warm.
const PLASTER = { base: '#6b5c50', shade: '#625448', lite: '#716256', spec: '#8c7c6d', ink: '#241c16' };
const plasterSheet = (look, seed) => ({
  w: W, h: H, tile: true, K: 2,
  draw(a) {
    const r = rng(seed);
    a.shape(box(-2, -2, W + 4, H + 4), look, { outer: false, edge: false });
    // Large masses of the coat, a tone apart, laid with a broad float.
    for (const [x, y, rx, ry] of [[26, 11, 30, 12], [94, 14, 30, 11], [34, 38, 28, 11], [100, 38, 26, 10]]) a.flat(torn(r, x, y, rx, ry), look.lite);
    for (const [x, y, rx, ry] of [[58, 31, 18, 6], [116, 58, 18, 6], [16, 59, 20, 5]]) a.flat(torn(r, x, y, rx, ry), look.shade);
    // Where the coat has come away: the darker stone under it, torn at the edge.
    for (const [x, y, rx, ry] of [[86, 25, 7, 2.4], [18, 49, 8, 2.6], [110, 12, 4, 2], [44, 8, 3, 1.6]]) { a.flat(torn(r, x, y, rx, ry), look.ink2); a.line(`M${x - rx * .7} ${y - ry * .4}L${x} ${y - ry * .7}L${x + rx * .6} ${y - ry * .3}`, { w: 1.4, c: look.ink }); }
    // The courses of the stone showing as long hairlines, broken where the wash held.
    for (const d of ['M-2 25L30 24.6', 'M42 25.4L76 25', 'M96 24.6L130 25', 'M-2 50L16 49.8', 'M26 50.4L70 50', 'M74 49.6L112 50', 'M120 50L130 50']) a.line(d, { w: 2.2, c: look.ink });
    for (const d of ['M60 6L63 11L61 15', 'M101 36L104 40L103 44', 'M28 32L32 36']) a.line(d, { w: 1.6, c: look.ink });
  },
});

/** A rivet's pair at a place: its head lit on top, a dark foot. */
const rivet = (a, x, y) => { a.flat(ell(x, y + .35, .7), '#141d24'); a.flat(ell(x, y - .1, .55), '#9fb6ba'); };
const TEAL = '#4fae9f', TEAL_DARK = '#1e5a5a', STEEL = '#7d99a0', SLATE = '#2c3f48', DARK = '#141d24';
const ring = (x, y, r0, r1) => { const p = (r, f) => { const o = []; for (let i = 0; i <= 28; i++) { const t = (f ? 1 : -1) * i * Math.PI / 14; o.push(x + Math.cos(t) * r, y + Math.sin(t) * r); } return 'M' + o.join(' ').replace(/(\S+ \S+) /g, '$1L') + 'Z'; }; return p(r0, true) + p(r1, false); };

const ALLOY_DETAILS = [
  (a) => { rivet(a, 4.4, 5.6); rivet(a, 11.4, 5.4); rivet(a, 4.4, 10.8); rivet(a, 11.5, 10.6); },
  (a) => {
    // A vent: slats in a dark throat.
    a.shape(box(3, 3.8, 10.2, 8.4), { base: DARK, shade: DARK, lite: DARK, spec: DARK, ink: DARK }, { outer: false, edge: false });
    for (const [y, w] of [[4.4, 10], [6, 10], [7.6, 9.6], [9.2, 10], [10.8, 10]]) { a.flat(box(3.1, y, w, .9), STEEL); a.flat(box(3.1, y + .6, w, .3), '#425a64'); }
    a.line('M8 7.8L12.6 8.6', { w: 1.2 });
  },
  (a) => {
    // A run of wire, come loose from its plate.
    a.tube('M.6 4.4L3.6 4.2L4.2 6.8L6.8 6.6L7.2 9L9.8 9.2L10 10.4L12.2 10.6', .9, { base: TEAL, shade: '#2c7a78', spec: '#a6e4d8' }, { bare: true });
    a.flat(ell(12.4, 10.6, .55), DARK);
  },
  (a) => {
    a.shape(box(4, 4, 8, 8), { base: '#71909a', shade: '#3e5560', lite: '#9cb6ba', spec: '#d0dee0', ink: DARK }, { lite: box(4, 4, 8, 1), shade: [box(4, 11, 8, 1), box(11, 4, 1, 8)], lines: 'M5.6 5.6L10.4 5.6L10.4 10.4L5.6 10.4Z' });
    a.flat(box(7.3, 7.3, 1.2, 1.2), '#d6e4e4');
  },
  (a) => { for (const [x, y, w] of [[3, 9.4, 3.6], [6.2, 8.5, 2.8], [8.6, 7.4, 3]]) { a.flat(box(x, y, w, .9), '#a8c0c2'); a.flat(box(x, y + .7, w, .3), '#3e5560'); } },
  (a) => { a.flat(ring(8.6, 8.4, 3.3, 2.1), DARK); a.flat(ring(8.6, 8.4, 2.9, 2.4), TEAL_DARK); a.flat(box(8, 7.8, 1.2, 1.2), TEAL); },
  (a) => a.line('M6 2L7.4 4L9.4 7L9.4 8.6L8.2 10.8', { w: 3, c: DARK }),
  (a) => { a.flat(box(5, 6, 6.2, 1.1), SLATE); a.flat(box(5, 7.1, 6.2, .3), DARK); a.flat(box(5, 9, 6.2, 1.1), STEEL); },
  (a) => {
    // A dial of the ruins: two dark rings, a live core.
    a.flat(ring(16, 16, 10.8, 9.4), DARK);
    for (const [from, to] of [[.2, 1.2], [1.7, 2.9], [3.3, 4.2], [4.7, 5.9]]) { const o = []; for (let i = 0; i <= 8; i++) { const t = from + (to - from) * i / 8; o.push(16 + Math.cos(t) * 8.8, 16 + Math.sin(t) * 8.8); } for (let i = 8; i >= 0; i--) { const t = from + (to - from) * i / 8; o.push(16 + Math.cos(t) * 8, 16 + Math.sin(t) * 8); } a.flat(poly(o), TEAL_DARK); }
    a.flat(ring(16, 16, 5.4, 4.3), DARK);
    a.flat(ell(16, 16, 4.3), '#30454e');
    a.flat(box(14, 14, 4, 4), TEAL);
    a.flat(box(14, 14, 4, 1), '#92d8cc');
    a.flat(box(14.8, 14.6, 1, 1), '#e4f6f0');
    a.line('M21 8L23.4 10.6', { w: 1.4 });
  },
  (a) => {
    // A slate of its writing, most of the lines dead.
    a.shape(box(4, 4, 24, 24), { base: '#2a3f48', shade: '#22333a', lite: '#37505a', spec: '#476470', ink: DARK }, { lite: box(4, 4, 24, 1.2), shade: [box(4, 26.6, 24, 1.4), box(26.8, 4, 1.2, 24)] });
    for (const [y, runs] of [[7.4, [[8, 1], [10.5, 2.4, 1], [14, 2.6], [17.6, 1, 1], [19.6, .9], [21.6, 3]]], [11.4, [[7, 1.4], [9.5, 2, 1], [13, 3.4], [17, 3, 1], [21.6, 1]]], [15.4, [[8, 1], [10, 4.6], [16, 1.6, 1], [18.6, 1], [21, 3.4]]], [19.4, [[7, 2, 1], [10.4, 1.2], [13, 1.4], [15.6, 4.2], [21, 2]]], [23.4, [[10, 1], [12.2, 1, 1], [15, 2.2, 1], [18.2, 5]]]])
      for (const [x, w, on] of runs) a.flat(box(x, y, w, 1), on ? TEAL : '#2f6a6c');
    a.line('M6 27L11 22L10 18', { w: 1.6 });
  },
  (a) => {
    // The great wheel: rim, eight spokes, a hub with a live core.
    const X = 24.6, Y = 24.4;
    a.flat(ring(X, Y, 14.6, 13), DARK);
    a.flat(ring(X, Y, 13, 12), STEEL);
    for (let i = 0; i < 8; i++) {
      const t = i * Math.PI / 4, dx = Math.cos(t), dy = Math.sin(t), nx = -dy * .4, ny = dx * .4;
      a.flat(poly(X + dx * 7 + nx, Y + dy * 7 + ny, X + dx * 12 + nx, Y + dy * 12 + ny, X + dx * 12 - nx, Y + dy * 12 - ny, X + dx * 7 - nx, Y + dy * 7 - ny), i % 2 ? '#3f8f88' : TEAL);
    }
    a.flat(ell(X, Y, 7.2), DARK);
    a.flat(ell(X, Y, 6), TEAL_DARK);
    a.flat(ell(X, Y, 4.8), '#2c414a');
    a.flat(box(X - 2, Y - 2, 4, 4), TEAL);
    a.flat(box(X - 2, Y - 2, 4, 1), '#92d8cc');
    a.flat(box(X - 1.2, Y - 1.4, 1, 1), '#e4f6f0');
    a.line(`M${X + 5} ${Y + 10}L${X + 8} ${Y + 8}L${X + 9} ${Y + 11}`, { w: 1.6 });
  },
];

const BED_INK = '#0d0b10';
const shard = (a, x, foot, w, tip, lean) => a.shape(poly(x - w / 2, foot, x - w * .38 + lean * .6, tip + (foot - tip) * .32, x + lean, tip, x + w * .5 + lean * .6, tip + (foot - tip) * .3, x + w / 2, foot), AMETHYST, {
  lite: poly(x - w / 2, foot, x - w * .38 + lean * .6, tip + (foot - tip) * .32, x + lean, tip, x - w * .08 + lean * .7, tip + (foot - tip) * .34, x - w * .1, foot),
  shade: poly(x + w * .14, foot, x + w * .14 + lean * .6, tip + (foot - tip) * .36, x + w * .5 + lean * .6, tip + (foot - tip) * .3, x + w / 2, foot),
  outer: false,
});
const BEDROCK_DETAILS = [
  (a) => a.line('M1 6L5 6.2L6.6 8L10 8.8L13 11.4', { w: 3.2, c: BED_INK }),
  (a) => a.line('M.6 12L1.4 10L3 9.6L4 11', { w: 2.2, c: BED_INK }),
  (a) => { shard(a, 7, 13, 3.8, 4.6, 0); },
  (a) => a.shape(poly(3.2, 8, 5, 5, 8, 4, 11, 5.4, 13.4, 8.2, 12, 10.6, 8, 11.4, 4.8, 10.8), { base: '#1c1a20', shade: '#141216', lite: '#2a282f', spec: '#34323a', ink: BED_INK }, { shade: .7, lines: 'M6 7L9 8L11 7' }),
  (a) => { a.line('M.4 13L1 11L3.6 10.4L5.4 11L7.6 12.4', { w: 2.2, c: BED_INK }); a.line('M.4 4L1.2 6', { w: 1.8, c: BED_INK }); },
  (a) => a.shape(poly(3.2, 9.6, 5, 6.4, 9, 6, 11, 8.6, 9.4, 11.6, 5, 12), AMETHYST, { lite: poly(4, 9, 5.4, 7, 8.4, 6.8, 6.8, 9.4), shade: poly(5, 12, 7.6, 10.2, 11, 8.6, 9.4, 11.6), spec: ell(6.4, 8, .6, .4) }),
  (a) => a.line('M0 8.2L4 9.4L9.4 10.4L13 11L15.8 13', { w: 3, c: BED_INK }),
  (a) => a.line('M.4 6L1.4 4.6L3 3', { w: 2, c: BED_INK }),
  (a) => {
    // A boulder fallen in the deep: a big slumped mass, cut by one or two faces.
    const d = poly(3, 16, 7, 10, 14, 7, 23, 8, 29, 11, 29.6, 19, 26, 24, 16, 25.4, 8, 24, 4.6, 21);
    a.shape(d, { base: '#4a4553', shade: '#332f3b', deep: '#26232d', lite: '#5e5868', spec: '#716b7c', ink: BED_INK }, {
      lite: poly(6.6, 12, 14, 8.4, 22, 9, 19, 14, 9, 16),
      shade: poly(19, 14, 29, 11, 29.6, 19, 26, 24, 16, 25.4, 17.4, 19),
      deep: poly(24, 16, 29.4, 13, 29.6, 19, 26, 24, 22.4, 24.6),
      lines: ['M9 16L19 14L17.4 19', 'M19 14L22 9'],
    });
  },
  (a) => {
    // Rock split open on a seam of crystal.
    const d = poly(5, 17, 9, 12, 16, 10.4, 21, 12, 25, 16, 25, 21, 21, 24.6, 13, 25.2, 8, 23);
    a.shape(d, { base: '#3f3a48', shade: '#2c2833', deep: '#211e27', lite: '#544e5e', spec: '#68616f', ink: BED_INK }, { lite: poly(7, 15, 15, 11.4, 20, 13, 12, 17), shade: poly(19, 17, 25, 16, 25, 21, 21, 24.6, 15, 25) });
    a.flat(poly(10.6, 24, 12, 19, 19.6, 17.6, 20.4, 23.4), '#16141a');
    shard(a, 17.2, 23.6, 3, 16.4, .2);
    shard(a, 13.4, 23.8, 1.8, 19.4, -.3);
  },
  (a) => a.line('M0 8.6L4 9L6 11.6L9 12.4L13 16L16 20L19 24L24 27L27 30L32 33L35 38L40 42L43.6 46', { w: 3.8, c: BED_INK }),
];

// Tops: what lies at the ground's line, row 16, in the cells the logical sheet has it. [variant, half, front]: an alloy's fleck, a bedrock's spike.
const ALLOY_TOPS = [[0, 0, 0, 6.6, 1], [0, 1, 0, 1.2, 2.8], [1, 1, 0, 2, 3.6], [2, 0, 0, 4, 2.2], [2, 1, 0, 3, 1.8], [3, 0, 0, 2, 1.2], [3, 1, 0, 2.2, 2],
  [0, 1, 1, 2.2, 2], [1, 1, 1, 2.4, 3.4], [2, 0, 1, 4, 2], [3, 1, 1, 2.2, 1.8]];
const BEDROCK_TOPS = [[0, 1, 2.6, 12.2, -.3], [1, 0, 4.6, 11, .3], [1, 1, 3.4, 12, -.2], [2, 0, 5.6, 11.2, .4], [3, 0, 6.2, 13, -.2], [3, 1, .5, 15, .1]];

function alloyTop(x, w) {
  return { w: 8, h: 32, draw(a) {
    const d = poly(x, 16, x + w * .1, 15, x + w * .55, 14.4, x + w, 15.2, x + w, 16);
    a.shape(d, { base: '#5a7682', shade: '#2e414b', lite: '#a4bcc0', spec: '#d0dee0', ink: DARK }, { lite: poly(x + w * .1, 15, x + w * .55, 14.4, x + w, 15.2, x + w * .6, 15.4), shade: poly(x, 15.7, x + w, 15.7, x + w, 16, x, 16) });
  } };
}
function bedrockTop(x, tip, lean) {
  return { w: 8, h: 32, draw(a) {
    const w = (16 - tip) * .85;
    a.shape(poly(x - w / 2, 16.2, x - w * .2 + lean * .6, tip + (16 - tip) * .4, x + lean, tip, x + w * .3 + lean * .6, tip + (16 - tip) * .5, x + w / 2, 16.2), BEDROCK[2], {
      lite: poly(x - w / 2, 16.2, x - w * .2 + lean * .6, tip + (16 - tip) * .4, x + lean, tip, x - w * .05, 16.2),
      shade: poly(x + w * .08, 16.2, x + lean, tip, x + w * .3 + lean * .6, tip + (16 - tip) * .5, x + w / 2, 16.2),
    });
  } };
}

Subjects.grounds = function () {
  out('wall_bedrock', bake(wallBedrock));
  out('back_bedrock', bake(backBedrock));
  out('wall_alloy', bake(wallAlloy));
  out('back_alloy', bake(backAlloy));
  out('wall_lava', bake(wallLava));
  out('back_lava', bake(backLava));
  // A wall of plaster stands nowhere yet, its back wall is the homes': warm, well darker than what stands before it.
  out('wall_plaster', bake(plasterSheet({ ...PLASTER, ink2: '#4a3e35' }, 0x91a)));
  out('back_plaster', bake(plasterSheet({ base: '#40342b', shade: '#3a2f27', lite: '#43372e', spec: '#4c3f35', ink: '#1c1510', ink2: '#2e251e' }, 0x91b)));
  const edges = {
    bedrock: { lite: '#4e4a5c', shade: '#25222b', ink: '#141218' },
    alloy: { lite: '#a6c2c6', shade: '#2d3e47', ink: '#141d24' },
    lava: { lite: '#fbbe50', shade: '#8a2c10', ink: '#280a04' },
    plaster: { lite: '#827060', shade: '#3a2e26', ink: '#241c16' },
  };
  for (const [name, look] of Object.entries(edges)) for (let open = 1; open < 16; open++) out(`wall-edge_${name}_${open}`, AnimeEdge(open, look));
  ALLOY_DETAILS.forEach((draw, k) => { const side = k < 8 ? 16 : k < 10 ? 32 : 48; out(`detail_alloy_${k}`, bake({ w: side, h: side, draw })); });
  BEDROCK_DETAILS.forEach((draw, k) => { const side = k < 8 ? 16 : k < 10 ? 32 : 48; out(`detail_bedrock_${k}`, bake({ w: side, h: side, draw })); });
  SPLASH.forEach((list, k) => out(`detail_lava_${k}`, bake(splash(list, 16))));
  SPLASH_FEATURES.forEach((list, k) => out(`detail_lava_${8 + k}`, bake(splash(list, k < 2 ? 32 : 48))));
  for (const [v, half, front, x, w] of ALLOY_TOPS) out(`top_alloy_${v}_${front ? 'front' : 'back'}_${half}`, bake(alloyTop(x, w)));
  for (const [v, half, x, tip, lean] of BEDROCK_TOPS) out(`top_bedrock_${v}_back_${half}`, bake(bedrockTop(x, tip, lean)));
};
})();

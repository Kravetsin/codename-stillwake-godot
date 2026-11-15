// The leaves plants shed, picked in 2026-11-10-plant-stirs: four looks, each with an acacia's and a bush's three shapes, front
// and back (seen as a leaf flips). All kept, for plants to come: the acacia sheds blots (B), the thorn bush sprigs (D). Each look's
// flight is FloraLooks.Flights: the leaflet swings like a pendulum, the blot flutters, the dry leaf spins, the sprig tumbles.
(function () {
'use strict';
const { poly, rng, bake } = Anime;
const f2 = (v) => +v.toFixed(2);
const path = (pts) => 'M' + pts.map(([x, y]) => `${f2(x)} ${f2(y)}`).join('L');
const flat = (pts) => poly(pts.flat());

/** A blade along x through (cx, cy): len long, wid at its widest (belly: where along), arched up by bend; its edges and axis, a point a step. */
function blade(cx, cy, len, wid, o) {
  const N = 16, belly = o.belly ?? .76, sharp = o.sharp ?? .8, bend = o.bend ?? 0, up = [], lo = [], ax = [];
  for (let i = 0; i <= N; i++) {
    const t = i / N, x = cx - len / 2 + len * t, y = cy - bend * Math.sin(Math.PI * t), hw = (wid / 2) * Math.pow(Math.sin(Math.PI * Math.pow(t, belly)), sharp);
    up.push([x, y - hw]); lo.push([x, y + hw]); ax.push([x, y]);
  }
  return { up, lo, ax, N, outline: flat([...up, ...lo.slice().reverse()]), under: flat([...ax, ...lo.slice().reverse()]) };
}

/** A wet round of paint: a wobbling oval, drawn out to a soft point toward +x by tip. */
function blobPts(cx, cy, rx, ry, tip, r) {
  const pts = [], n = 24, p1 = r() * 6.3, p2 = r() * 6.3;
  for (let k = 0; k < n; k++) {
    const t = (k / n) * Math.PI * 2, c = Math.cos(t), s = Math.sin(t), wob = 1 + .07 * Math.sin(t * 3 + p1) + .05 * Math.sin(t * 5 + p2), pt = 1 + tip * Math.pow(Math.max(0, c), 5);
    pts.push([cx + c * rx * wob * pt, cy + s * ry * wob]);
  }
  return pts;
}

/** A dry leaf rolled along its length, seen from the side: a crescent, its middle lifted by cu, wid thick. */
function curlPts(cx, cy, len, wid, cu) {
  const N = 16, up = [], lo = [], foot = cy + (wid + cu) / 2;
  for (let i = 0; i <= N; i++) {
    const t = i / N, x = cx - len / 2 + len * t, s = Math.sin(Math.PI * t), yl = foot - cu * Math.pow(s, .9), yu = yl - wid * Math.pow(s, .7);
    up.push([x, yu]); lo.push([x, yl]);
  }
  return { up, lo, N };
}

/** A dab of leaf on a stem, as the acacia's crown is painted: from its foot (x, y) along angle a, len long. */
function dab(x, y, a, len, wk) {
  const dx = Math.cos(a), dy = Math.sin(a), w = len * wk, mx = x + dx * len * .45, my = y + dy * len * .45;
  return { all: [[x, y], [mx - dy * w / 2, my + dx * w / 2], [x + dx * len, y + dy * len], [mx + dy * w / 2, my - dx * w / 2]], half: [[x, y], [mx - dy * w / 2, my + dx * w / 2], [x + dx * len, y + dy * len]] };
}

// The plants' own paints (flora.js, shrubs.js), and what a leaf off them turns to.
const INK = '#091412', WOOD_INK = '#1a1714';

/* ---- A: a narrow leaflet, an ink midrib, the pen round it; its lower half in shadow, one hard step. Falls like a pendulum. ---- */
const LEAFLET = {
  id: 'leaflet',
  acacia: [
    { len: 4.6, wid: 1.4, bend: .12, m: { base: '#4a7c60', shade: '#37644f', lite: '#689873', ink: INK }, b: { base: '#7b9a84', lite: '#93ae98', vein: '#5a7a66' } },
    { len: 4.0, wid: 1.25, bend: -.1, m: { base: '#335f4d', shade: '#244537', lite: '#4a785f', ink: INK }, b: { base: '#5e8070', lite: '#77957f', vein: '#43604f' } },
    { len: 5.0, wid: 1.5, bend: .22, m: { base: '#64794c', shade: '#4b5c38', lite: '#82915e', ink: INK }, b: { base: '#8c9670', lite: '#a3aa82', vein: '#6a7553' } },
  ],
  bush: [
    { len: 3.1, wid: .95, bend: .06, m: { base: '#2d4638', shade: '#1c2e25', lite: '#3e5a48', ink: INK }, b: { base: '#4b5f50', lite: '#5f7262', vein: '#33463a' } },
    { len: 2.7, wid: .9, bend: -.08, m: { base: '#213529', shade: '#16241c', lite: '#33493a', ink: INK }, b: { base: '#3f5244', lite: '#526457', vein: '#2b3b30' } },
    { len: 3.3, wid: 1.0, bend: .12, m: { base: '#4f5032', shade: '#3a3a22', lite: '#666644', ink: INK }, b: { base: '#6b6a4a', lite: '#7e7c59', vein: '#4f4e33' } },
  ],
  pic(v, back) {
    const w = Math.ceil(v.len + 1.4), h = Math.ceil(v.wid + Math.abs(v.bend) + 1.2);
    return { w, h, margin: 1, draw(a) {
      const B = blade(w / 2 + .35, h / 2 + v.bend / 2, v.len, v.wid, { bend: v.bend, belly: .62, sharp: .62 }), s = B.ax[0];
      const rib = path([[s[0] - .75, s[1] + .2], ...B.ax.slice(0, Math.round(B.N * .86))]);
      if (back) { a.shape(B.outline, { base: v.b.base, shade: v.b.base, lite: v.b.lite, ink: INK }, { edge: false }); a.line(rib, { w: 1, c: v.b.vein }); }
      else { a.shape(B.outline, v.m, { edge: false, shade: B.under }); a.line(rib, { w: 1.15, c: v.m.ink }); }
    } };
  },
};

/* ---- B: a round blot of wet paint, no outline: a darker tide where it pooled, a lighter bloom, and a pen stroke along one edge that lifts off. Flutters wide and slow. ---- */
const BLOT = {
  id: 'blot',
  acacia: [
    { rx: 1.7, ry: 1.3, tip: .25, bite: false, m: { base: '#4f8068', shade: '#3a6450', deep: '#2b4b3c', lite: '#74a084', ink: INK }, b: { base: '#7f9e89', shade: '#688772', lite: '#9ab49f' } },
    { rx: 2.1, ry: 1.15, tip: .45, bite: false, m: { base: '#3c6a55', shade: '#2c5141', deep: '#1f3b2f', lite: '#5f8c72', ink: INK }, b: { base: '#6a8b78', shade: '#577563', lite: '#86a491' } },
    { rx: 1.85, ry: 1.3, tip: .3, bite: true, m: { base: '#5f7d55', shade: '#486240', deep: '#354a30', lite: '#7f9a70', ink: INK }, b: { base: '#8b9c7e', shade: '#748568', lite: '#a2b094' } },
  ],
  bush: [
    { rx: 1.3, ry: .95, tip: .3, bite: false, m: { base: '#33452f', shade: '#263423', deep: '#1a2519', lite: '#4a5f44', ink: INK }, b: { base: '#55654f', shade: '#45543f', lite: '#6a7a62' } },
    { rx: 1.15, ry: 1.0, tip: .15, bite: true, m: { base: '#263a2e', shade: '#1b2a21', deep: '#121d17', lite: '#3a5142', ink: INK }, b: { base: '#485a4d', shade: '#3a4a3f', lite: '#5b6c5f' } },
    { rx: 1.4, ry: .95, tip: .4, bite: false, m: { base: '#57553a', shade: '#43412b', deep: '#302f1e', lite: '#706d4c', ink: INK }, b: { base: '#77745a', shade: '#626047', lite: '#8b876b' } },
  ],
  pic(v, back) {
    const w = Math.ceil(v.rx * 2 * (1 + v.tip * .6) + 1.4), h = Math.ceil(v.ry * 2 + 1);
    return { w, h, margin: 1, draw(a) {
      const r = rng(Math.round(v.rx * 97 + v.ry * 31)), cx = w / 2 + .3, cy = h / 2, pts = blobPts(cx, cy, v.rx, v.ry, v.tip, r);
      const lite = blobPts(cx - v.rx * .3, cy - v.ry * .3, v.rx * .38, v.ry * .3, 0, r);
      if (back) a.shape(flat(pts), { base: v.b.base, shade: v.b.shade, lite: v.b.lite, ink: INK }, { outer: false, edge: false, shade: .35 });
      else a.shape(flat(pts), v.m, { outer: false, edge: false, shade: .6, deep: .22, lite: flat(lite) });
      if (v.bite) a.cut(Anime.ell(cx + v.rx * .45, cy + v.ry * 1.0, v.rx * .42, v.ry * .4));
      // The pen along the upper left only, lifting off; the stalk.
      const n = pts.length, from = Math.round(n * .55), to = Math.round(n * .86);
      if (!back) a.line(path(pts.slice(from, to).map(([x, y]) => [x, y - .12])), { w: 1.05, c: INK });
      a.line(path([[cx - v.rx * .95, cy + .05], [cx - v.rx - .75, cy + .3]]), { w: .95, c: back ? v.b.shade : INK });
    } };
  },
};

/* ---- C: a dry leaf rolled along its length: a crescent, the dark of its hollow underneath, a lit rolled edge, the pen heavy round it. Spins as it falls, and falls faster. ---- */
const DRY = {
  id: 'dry',
  acacia: [
    { len: 4.6, wid: 1.2, cu: .9, m: { base: '#7a7442', shade: '#5a5530', deep: '#3d3820', lite: '#9a9160', ink: '#17120f' }, b: { base: '#8f8858', shade: '#716b42', lite: '#a8a070' } },
    { len: 4.0, wid: 1.1, cu: 1.1, m: { base: '#7d6238', shade: '#5c4628', deep: '#3c2c1a', lite: '#9a7d4f', ink: '#17120f' }, b: { base: '#937852', shade: '#765e3c', lite: '#ab9166' } },
    { len: 4.8, wid: 1.3, cu: .7, m: { base: '#5f6a45', shade: '#474f33', deep: '#323722', lite: '#7c8559', ink: '#17120f' }, b: { base: '#7c8462', shade: '#62694b', lite: '#959b77' } },
  ],
  bush: [
    { len: 3.1, wid: .9, cu: .7, m: { base: '#4f483c', shade: '#38322a', deep: '#221d18', lite: '#6a604f', ink: WOOD_INK }, b: { base: '#655d4f', shade: '#4e473b', lite: '#7a7160' } },
    { len: 2.8, wid: .85, cu: .9, m: { base: '#4b4c30', shade: '#37371f', deep: '#25250f', lite: '#64643f', ink: WOOD_INK }, b: { base: '#636446', shade: '#4d4e34', lite: '#77785a' } },
    { len: 3.3, wid: .95, cu: .55, m: { base: '#3e3529', shade: '#2c251c', deep: '#1a1510', lite: '#574b3a', ink: WOOD_INK }, b: { base: '#574d40', shade: '#43392e', lite: '#6b604f' } },
  ],
  pic(v, back) {
    const w = Math.ceil(v.len + 1.2), h = Math.ceil(v.wid + v.cu + 1.2);
    return { w, h, margin: 1, draw(a) {
      const C = curlPts(w / 2 + .3, h / 2, v.len, v.wid, v.cu), N = C.N, out = flat([...C.up, ...C.lo.slice().reverse()]);
      const mid = (k0, k1) => [Math.round(N * k0), Math.round(N * k1)];
      const [h0, h1] = mid(.14, .86), hollow = [], rim = [];
      for (let i = h0; i <= h1; i++) { const [x, yl] = C.lo[i], yu = C.up[i][1]; hollow.push([x, yl - (yl - yu) * .42]); }
      const [l0, l1] = mid(.08, .62);
      for (let i = l0; i <= l1; i++) { const [x, yu] = C.up[i], yl = C.lo[i][1]; rim.push([x, yu + (yl - yu) * .26]); }
      const veins = [.36, .58].map((t) => { const i = Math.round(N * t), j = Math.min(N, i + 3); return path([[C.lo[i][0], C.lo[i][1] - .1], [C.up[j][0] - .2, (C.up[j][1] + C.lo[j][1]) / 2]]); });
      if (back) a.shape(out, { base: v.b.base, shade: v.b.shade, lite: v.b.lite, ink: v.m.ink }, { edge: false, shade: .45, lines: veins.slice(0, 1) });
      else a.shape(out, v.m, {
        edge: false, shade: .35, lines: veins,
        deep: flat([...C.lo.slice(h0, h1 + 1), ...hollow.slice().reverse()]),
        lite: flat([...C.up.slice(l0, l1 + 1), ...rim.slice().reverse()]),
      });
      const s = C.lo[0];
      a.line(path([[s[0] + .15, s[1] - .1], [s[0] - .7, s[1] + .1]]), { w: 1, c: v.m.ink });
    } };
  },
};

/* ---- D: a sprig: a dark stem with the crown's own dabs of leaf in pairs; off the bush, a snapped thorn twig with a leaf or two. Tumbles end over end, falls fastest. ---- */
const SPRIG = {
  id: 'sprig',
  acacia: [
    { len: 5.4, pairs: [[.3, 1.7], [.55, 1.9], [.8, 1.8]], end: 1.6, tones: ['#4a7c60', '#335f4d', '#689873'], stem: '#33443a' },
    { len: 4.8, pairs: [[.35, 1.9], [.68, 2.0]], end: 1.8, tones: ['#335f4d', '#4a7c60', '#3e6f58'], stem: '#2f3f35' },
    { len: 5.8, pairs: [[.28, 1.8], [.52, 1.9, 'up'], [.76, 1.7]], end: 0, tones: ['#4a7c60', '#64794c', '#335f4d'], stem: '#3a4436' },
  ],
  bush: [
    { len: 4.2, thorn: .45, leaves: [[.78, -1, 1.7, '#2d4638'], [.62, 1, 1.5, '#213529']] },
    { len: 3.8, thorn: .35, leaves: [[.8, 1, 1.8, '#4f5032']] },
    { len: 4.6, thorn: .55, leaves: [[.7, -1, 1.6, '#2d4638'], [.9, 1, 1.4, '#4f5032']] },
  ],
  pic(v, back, plant) {
    const end = v.end || 0, w = Math.ceil(v.len + end + 1.4), h = plant === 'bush' ? 4 : 5;
    return { w, h, margin: 1, draw(a) {
      const x0 = (w - v.len - end) / 2 + .2, y0 = h / 2 + .5, at = (t) => [x0 + v.len * t, y0 - .7 * Math.sin(Math.PI * t * .9) + .2 * t], dir = (t) => { const p = at(Math.max(0, t - .02)), q = at(Math.min(1, t + .02)); return Math.atan2(q[1] - p[1], q[0] - p[0]); };
      const stem = [];
      for (let k = 0; k <= 10; k++) stem.push(at(k / 10));
      const pale = (c) => { const n = parseInt(c.slice(1), 16), m = (s) => Math.round(((n >> s) & 255) * .55 + 150 * .45); return '#' + [m(16), m(8), m(0)].map((x) => x.toString(16).padStart(2, '0')).join(''); };
      const leafOf = (base, a0, len, tone, shadeTone) => { const d = dab(base[0], base[1], a0, len, .42); a.flat(flat(d.all), tone); a.flat(flat(d.half), shadeTone); };
      if (plant === 'bush') {
        a.tube(path(stem), .42, { base: '#4a443a', shade: '#35302a', ink: WOOD_INK }, { spec: false });
        const p = at(v.thorn), d = dir(v.thorn);
        a.flat(flat([[p[0] - .3, p[1]], [p[0] + Math.cos(d - 1.2) * 1.1, p[1] + Math.sin(d - 1.2) * 1.1], [p[0] + Math.cos(d - .6) * 1.3, p[1] + Math.sin(d - .6) * 1.3], [p[0] + .3, p[1]]]), WOOD_INK);
        for (const [t, side, len, tone] of v.leaves) leafOf(at(t), dir(t) + side * .75, len, back ? pale(tone) : tone, back ? pale(tone) : '#16241c');
        a.line(path([at(0), [at(0)[0] - .5, at(0)[1] + .25]]), { w: 1.2, c: WOOD_INK });
        return;
      }
      a.tube(path(stem), .3, { base: v.stem, shade: v.stem, ink: INK }, { spec: false });
      v.pairs.forEach(([t, len, up], i) => {
        const base = at(t), d = dir(t), tone = v.tones[i % 3], sh = '#244537';
        for (const side of up === 'up' ? [-1] : [-1, 1]) leafOf(base, d + side * .78, len * (side > 0 ? .95 : 1), back ? pale(tone) : tone, back ? pale(sh) : sh);
      });
      if (v.end) leafOf(at(.97), dir(.97), v.end, back ? pale(v.tones[0]) : v.tones[0], back ? pale('#244537') : '#244537');
    } };
  },
};

const CANDIDATES = [LEAFLET, BLOT, DRY, SPRIG];
const PLANTS = ['acacia', 'bush'];

/** planet/leaf_<look>_<plant>_<n> and its _back. */
Subjects.leaves = function () {
  for (const c of CANDIDATES) for (const plant of PLANTS) c[plant].forEach((v, n) => {
    out(`planet/leaf_${c.id}_${plant}_${n}`, bake(c.pic(v, false, plant)));
    out(`planet/leaf_${c.id}_${plant}_${n}_back`, bake(c.pic(v, true, plant)));
  });
};
})();

// The cave hostiles (docs/plans/cave-hostiles.md): the capback and the geode snail, three candidates each, drawn for a pick page that
// loads this file as it is. World pixels: a body's origin is the middle of its feet, facing right, y down; a part's origin is its pivot.
// A part with `light` is a white mask the game adds over it unlit, in its colour that moment. Exports only the picked candidates.
(function () {
'use strict';
const { ell, poly, rng } = Anime;

const cr = (a, b, c, d, t) => .5 * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (3 * b - a - 3 * c + d) * t * t * t);
/** A smooth run through the points, about `step` pixels a step; a point with 'c' as its last item is a hard corner. */
function spline(pts, closed, step) {
  const n = pts.length, out = [], last = closed ? n : n - 1, dims = pts[0].filter((v) => typeof v === 'number').length;
  const at = (i) => (closed ? pts[(i + n) % n] : pts[Math.max(0, Math.min(n - 1, i))]);
  for (let i = 0; i < last; i++) {
    const p1 = at(i), p2 = at(i + 1), p0 = p1[p1.length - 1] === 'c' ? p1 : at(i - 1), p3 = p2[p2.length - 1] === 'c' ? p2 : at(i + 2);
    const k = Math.max(2, Math.round(Math.hypot(p2[0] - p1[0], p2[1] - p1[1]) / (step || .6)));
    for (let s = 0; s < k; s++) { const t = s / k, q = []; for (let d = 0; d < dims; d++) q.push(cr(p0[d], p1[d], p2[d], p3[d], t)); out.push(q); }
  }
  if (!closed) out.push(pts[n - 1].slice(0, dims));
  return out;
}
/** A closed outline through the points, moved along its normal a little, so an edge is worn and not a ruled curve. */
function hide(pts, r, amp) {
  const s = spline(pts, true), n = s.length, out = [];
  s.forEach((p, i) => {
    const a = s[(i + n - 1) % n], b = s[(i + 1) % n], dx = b[0] - a[0], dy = b[1] - a[1], l = Math.hypot(dx, dy) || 1, k = r ? (r() - .5) * (amp === undefined ? .4 : amp) : 0;
    out.push(p[0] - (dy / l) * k, p[1] + (dx / l) * k);
  });
  return poly(out);
}
/** A limb along [x, y, half width] points. */
function limb(pts) {
  const s = spline(pts, false), L = [], R = [];
  s.forEach((p, i) => {
    const a = s[Math.max(0, i - 1)], b = s[Math.min(s.length - 1, i + 1)], dx = b[0] - a[0], dy = b[1] - a[1], l = Math.hypot(dx, dy) || 1;
    L.push(p[0] - (dy / l) * p[2], p[1] + (dx / l) * p[2]);
    R.unshift(p[1] - (dx / l) * p[2]); R.unshift(p[0] + (dy / l) * p[2]);
  });
  return poly(L.concat(R));
}
/** A pen line through the points. */
const run = (...pts) => 'M' + spline(pts, false, .8).map((p) => `${p[0].toFixed(2)} ${p[1].toFixed(2)}`).join('L');
const mul = (hex, k) => '#' + [1, 3, 5].map((i) => Math.min(255, Math.round(parseInt(hex.substr(i, 2), 16) * k)).toString(16).padStart(2, '0')).join('');
const mat = (base, shade, deep, lite, ink) => ({ base, shade, deep, lite, spec: lite, ink: ink || '#1a120d' });
const farOf = (m) => mat(mul(m.base, .66), mul(m.shade, .68), mul(m.deep, .74), mul(m.base, .76), m.ink);
/** A point on a circle, for crystals and teeth set round a rim. */
const on = (cx, cy, rx, ry, a) => [cx + Math.cos(a) * rx, cy + Math.sin(a) * ry];

/** A crystal: a prism from (x, y) along angle a, len long, half w wide, its tip cut at a slant. Returns [outline, shaded face]. */
function prism(x, y, a, len, w) {
  const dx = Math.cos(a), dy = Math.sin(a), nx = -dy * w, ny = dx * w, t = [x + dx * len, y + dy * len];
  const l = [x - nx + dx * len * .7, y - ny + dy * len * .7], r = [x + nx + dx * len * .82, y + ny + dy * len * .82];
  return [poly(x - nx, y - ny, l[0], l[1], t[0], t[1], r[0], r[1], x + nx, y + ny), poly(x, y, t[0], t[1], r[0], r[1], x + nx, y + ny)];
}
/** A cluster of crystals set round a mouth, pointing in at odd angles and lengths: never a ring of even teeth. */
function cluster(seed, cx, cy, rx, ry, n, from, to) {
  const r = rng(seed), out = [];
  for (let i = 0; i < n; i++) {
    const a = from + (to - from) * (i + .2 + r() * .6) / n, [x, y] = on(cx, cy, rx * (.92 + r() * .12), ry * (.92 + r() * .12), a);
    out.push({ x, y, a: a + Math.PI + (r() - .5) * .9, len: Math.min(rx, ry) * (.45 + r() * .55), w: .45 + r() * .4 });
  }
  return out;
}
function crystals(a, list, m) { for (const q of list) { const [o, face] = prism(q.x, q.y, q.a, q.len, q.w); a.shape(o, m, { outer: false, edge: true }); a.flat(face, m.shade); } }
function crystalGlow(c, list, al) { for (const q of list) { const [o] = prism(q.x, q.y, q.a, q.len * .9, q.w * .6); fillP(c, o, al === undefined ? .75 : al); } }

/* Plain masks for the parts that shine: the page and the game tint them. */
const fillP = (c, d, al) => { c.globalAlpha = al === undefined ? 1 : al; c.fillStyle = '#fff'; c.fill(new Path2D(d)); c.globalAlpha = 1; };
const strokeP = (c, d, w, al) => { c.globalAlpha = al === undefined ? 1 : al; c.strokeStyle = '#fff'; c.lineWidth = w; c.stroke(new Path2D(d)); c.globalAlpha = 1; };

const STEM = mat('#8e897b', '#6c685d', '#4f4c44', '#a7a292');
const STONE = mat('#6f6d68', '#55534f', '#3d3c39', '#8a8781');
const EYE = '#16120f';

/** A glowcap: a thin stem and a bell that hangs a little to one side, its lip torn. Returns the cap's outline for its light mask. */
function capOutline(x, y, w, h, lean, seed) {
  const r = rng(seed), l = lean || 0;
  return hide([[x + l, y - h, 'c'], [x + l + w * .2, y - h * .78], [x + w * .36, y - h * .36], [x + w * .47, y + h * .1, 'c'], [x + w * .22, y - h * .04],
    [x + w * .02, y + h * .06], [x - w * .2, y - h * .08], [x - w * .45, y + h * .12, 'c'], [x - w * .34, y - h * .38], [x + l - w * .18, y - h * .8]], r, .18);
}
/** A glowcap's light: the cap faint, its torn lip bright, as gills shine through it. */
function capGlow(c, q) {
  fillP(c, capOutline(q.x, q.y, q.w, q.h, q.lean, q.seed), .2);
  fillP(c, limb([[q.x - q.w * .42, q.y + q.h * .02, .14], [q.x, q.y - q.h * .02, .32], [q.x + q.w * .42, q.y + q.h * .02, .14]]), .9);
}
function glowcap(a, o) {
  const { x, y, w, h, lean, stem, m, seed } = o;
  a.flat(limb([[stem[0], stem[1], .36], [(stem[0] + x) / 2 + (lean || 0) * .3, (stem[1] + y) / 2, .3], [x, y - .2, .24]]), STEM.shade);
  a.line(run([stem[0] - .36, stem[1]], [(stem[0] + x) / 2 - .3 + (lean || 0) * .3, (stem[1] + y) / 2], [x - .25, y]), { w: 1 });
  const d = capOutline(x, y, w, h, lean, seed);
  a.shape(d, m, {
    shade: poly(x - w, y - h * .35, x + w, y - h * .55, x + w, y + 2, x - w, y + 2),
    deep: poly(x - w, y - h * .1, x + w, y - h * .2, x + w, y + 2, x - w, y + 2),
    lines: [run([x + (lean || 0) * .8, y - h * .9], [x + w * .14, y - h * .1]), run([x + (lean || 0) * .6 - w * .1, y - h * .85], [x - w * .24, y - h * .06])],
  });
  return d;
}

/* ===================== The capback ===================== */

/** «Кочка»: a dome of chitin under a mat of moss, glowcaps on its crown; hunkered, a mossy hump with nothing to tell it from the floor. */
function capHummock() {
  const SHELL = mat('#3b332d', '#2a241f', '#1c1714', '#51473f');
  const MOSS = mat('#3c5446', '#2b4034', '#1e2e25', '#516c5a');
  const TEAL = mat('#557f78', '#3f625d', '#2c4643', '#6f9b93'), VIOLET = mat('#64547a', '#4b3f5d', '#342b42', '#7c6c94');
  const r = () => rng(0xca1);
  const leg = (m) => (a) => {
    a.shape(limb([[0, -3.5, 1.1], [.2, -.4, 1], [2.2, 2.4, .8], [1.8, 4.6, .55], [1.2, 6.2, .22]]), m, { shade: poly(.4, -5, 4, -5, 4, 7, .4, 7) });
    a.line(run([1.6, 2.6], [2.6, 2.2]), { w: 1 });
  };
  const caps = [
    { x: -1.6, y: -5.8, w: 3.6, h: 3.4, lean: -.7, stem: [-.2, 0], m: TEAL, seed: 0x31 },
    { x: 2.8, y: -3.6, w: 2.8, h: 2.6, lean: .6, stem: [2, 0], m: TEAL, seed: 0x32 },
    { x: -4.6, y: -2.8, w: 2.4, h: 2.6, lean: -.5, stem: [-3.8, 0], m: VIOLET, seed: 0x33 },
    { x: 5.4, y: -1.8, w: 1.8, h: 1.8, lean: .4, stem: [5, 0], m: TEAL, seed: 0x35 },
  ];
  return {
    id: 'hummock', name: 'А', title: 'Кочка',
    tag: 'Купол хитина под толстым ковром мха, на макушке пучок светляков. Затаившись, ложится брюхом на пол и становится моховой кочкой: ни ног, ни головы. Вся угроза в том, как этот холмик поднимается.',
    centre: [0, -8],
    parts: {
      body: { w: 36, h: 20, px: 18, py: 19, draw(a) {
        const rr = r();
        a.shape(hide([[15.6, -4], [15.6, -7.6], [13.4, -11.4], [8.6, -15], [1.6, -16.6], [-5.6, -16.4], [-11.6, -14], [-15.6, -10], [-17, -6], [-16.4, -3.2], [-12, -2.6], [-4, -2.2], [5, -2.2], [12, -2.7]], rr, .3), SHELL, {
          shade: poly(-18, -6.4, -8, -5.2, 4, -5, 17, -6.6, 17, 0, -18, 0),
          deep: poly(-18, -3.4, 17, -3.6, 17, 0, -18, 0),
          lines: [run([-15.4, -6], [-8, -4.4], [2, -4.2], [10, -4.8], [14.8, -6.4]), run([-13.8, -8.6], [-12.8, -5]), run([12.4, -8.8], [11.8, -5.4]), run([-3, -7], [-2.6, -4.2])],
        });
        a.shape(hide([[-16.4, -8.4], [-15.2, -11.6], [-11.4, -14.6], [-5.6, -17], [1.6, -17.2], [8.8, -15.4], [13.6, -11.6], [15.2, -8.4], [13.2, -8.4], [12.4, -6.6], [10.4, -8], [7, -7.4],
          [5.6, -5.6], [3.8, -7.6], [0, -7.2], [-2.2, -5.4], [-3.8, -7.6], [-7.6, -7.8], [-9.4, -5.8], [-10.8, -8.2], [-14, -7.6]], rr, .6), MOSS, {
          shade: poly(-17, -9.6, -9, -10.8, 0, -10.4, 9, -11.2, 16, -10.2, 16, -5, -17, -5),
          deep: poly(-17, -8.4, 16, -8.8, 16, -5, -17, -5),
          lite: poly(-12, -15, -6, -17.6, 0, -17.8, -2, -15.8, -8, -14.6),
          lines: [run([-9, -12.6], [-7.8, -11.4]), run([-4, -14.2], [-2.6, -13]), run([3, -13.8], [4.4, -12.8]), run([8, -12.2], [9.2, -11]), run([-13, -10.8], [-12, -9.6]), run([11.4, -10.4], [12.2, -9.2])],
        });
        a.shape(hide([[8.2, -12.6], [9.6, -13.6], [11.2, -12.8], [10.8, -11.4], [9, -11.2]], rr, .2), STONE, { shade: poly(8, -12.2, 12, -12.6, 12, -10, 8, -10) });
        a.shape(hide([[-12.6, -11.4], [-11.4, -12.4], [-10.2, -11.6], [-10.8, -10.6]], rr, .15), STONE, { shade: poly(-13, -11.2, -10, -11.6, -10, -10, -13, -10) });
        a.flat(poly(-16, -3.4, -4, -2.8, 6, -2.8, 14.6, -3.6, 13, -2.4, 4, -1.9, -8, -2, -15, -2.4), SHELL.deep);
      } },
      mossLit: { w: 36, h: 20, px: 18, py: 19, light: true, draw(c) {
        for (const [x, y, rx] of [[-9.6, -8.4, .45], [-2.4, -6.4, .35], [5.6, -6.2, .4], [-7.4, -12.2, .3], [4.2, -13, .3]]) fillP(c, ell(x, y, rx, rx * .6), .7);
      } },
      caps: { w: 16, h: 13, px: 8, py: 11, draw(a) {
        a.flat(hide([[-7, .6], [-6, -1.2], [-3, -1.6], [0, -.8], [3, -1.4], [6, -.6], [6.4, .8]], rng(0x34), .5), MOSS.base);
        for (const q of caps) glowcap(a, q);
        a.shape(ell(1, -.7, .7, .55), TEAL, { shade: poly(0, -.7, 3, -.7, 3, 1, 0, 1) });
      } },
      capsLitT: { w: 16, h: 13, px: 8, py: 11, light: true, draw(c) {
        for (const q of caps.filter((q) => q.m === TEAL)) capGlow(c, q);
        fillP(c, ell(1, -.6, .45, .3), .7);
      } },
      capsLitV: { w: 16, h: 13, px: 8, py: 11, light: true, draw(c) {
        capGlow(c, caps[2]);
      } },
      head: { w: 14, h: 9, px: 2.5, py: 4.5, draw(a) {
        const rr = rng(0xca2);
        a.shape(limb([[5.4, .2, .55], [8, -.1, .42], [9.6, -.9, .26], [10, -1.8, .06]]), farOf(SHELL), { edge: false });
        a.shape(hide([[-2.4, -2.6], [1.6, -3.4], [4.8, -3], [6.8, -1.6], [7.6, .4], [6.8, 2], [3.6, 2.8], [-2.4, 2.6]], rr, .2), SHELL, {
          shade: poly(-3, .2, 8, -.4, 8, 4, -3, 4), deep: poly(-3, 1.6, 8, 1.2, 8, 4, -3, 4), lite: poly(0, -3.2, 3.6, -3.4, 4.2, -2.6, .4, -2.4),
          lines: [run([1.4, -3], [2, 2.4]), run([4.4, -2.8], [5, -1.4])],
        });
        a.shape(limb([[5.6, 1.4, .6], [8.4, 1.9, .46], [10.2, 1.2, .28], [10.8, .1, .06]]), SHELL, { edge: false, shade: poly(5, 1.9, 12, 1.6, 12, 4, 5, 4) });
        a.line(run([7.6, 1.2], [8.2, .7]), { w: .9 });
        a.flat('M3.6 -1.1Q4.6 -1.6 5.6 -1.2Q4.6 -.7 3.6 -1.1Z', EYE);
      } },
      eyeLit: { w: 14, h: 9, px: 2.5, py: 4.5, light: true, draw(c) { fillP(c, ell(4.8, -1.2, .3, .16)); } },
      leg: { w: 7, h: 12, px: 2, py: 4.5, draw: leg(SHELL) },
      legFar: { w: 7, h: 12, px: 2, py: 4.5, draw: leg(farOf(SHELL)) },
    },
    rig: [
      { id: 'l4', part: 'legFar', parent: 'body', x: -9, y: -6, drive: 'leg', phase: .5 },
      { id: 'l5', part: 'legFar', parent: 'body', x: -1, y: -6, drive: 'leg', phase: 0 },
      { id: 'l6', part: 'legFar', parent: 'body', x: 7.4, y: -6, drive: 'leg', phase: .5 },
      { id: 'l1', part: 'leg', parent: 'body', x: -12, y: -6, drive: 'leg', phase: 0 },
      { id: 'l2', part: 'leg', parent: 'body', x: -4, y: -6, drive: 'leg', phase: .5 },
      { id: 'l3', part: 'leg', parent: 'body', x: 4.6, y: -6, drive: 'leg', phase: 0 },
      { id: 'head', part: 'head', parent: 'body', x: 13.4, y: -5.6, drive: 'head', lit: [['eyeLit', 'eye']] },
      { id: 'body', part: 'body', parent: null, x: 0, y: 0, drive: 'body', lit: [['mossLit', 'teal']] },
      { id: 'caps', part: 'caps', parent: 'body', x: -2, y: -15.6, drive: 'caps', lit: [['capsLitT', 'teal'], ['capsLitV', 'violet']] },
    ],
    lights: [{ node: 'caps', x: -1, y: -6, r: 24, col: 'teal', k: .9 }, { node: 'caps', x: -4.6, y: -3, r: 13, col: 'violet', k: .6 }, { node: 'body', x: 0, y: -7, r: 10, col: 'teal', k: .25 }],
    pose: { drop: 3.2, rise: 3.4, pitch: .16, legUp: 1, tuckX: 7.4, tuckY: .2, tuckRot: .3, headUp: .25, headCrouch: -.2, legFly: .9, legSplay: .5, sagCaps: .3, capsLean: [0, 0] },
  };
}

/** «Гребень»: long and low in banded plates, a crest of thin caps standing along its spine; a scoop of a head with hooks. */
function capCrest() {
  const PLATE = mat('#3d3944', '#2c2932', '#1d1b22', '#544f5e');
  const LICHEN = '#363e35';
  const TEAL = mat('#53786f', '#3e5c55', '#2b403c', '#6b948a'), VIOLET = mat('#62527a', '#4a3e5c', '#332b40', '#7a6a93');
  const r = () => rng(0xcb1);
  const leg = (m) => (a) => {
    a.shape(limb([[0, -3, .9], [.4, -.2, .85], [2.8, 2.2, .62], [2.4, 4.4, .42], [3.4, 6.2, .16]]), m, { shade: poly(.4, -4, 5, -4, 5, 7, .4, 7) });
    a.line(run([2.6, 3.4], [1.6, 3.1]), { w: .9 });
    a.line(run([1.2, .6], [.2, .9]), { w: .9 });
  };
  // The crest: thin caps along the ridge, tallest over the middle, violet among the teal.
  // Short-stemmed, swept back like a mane, tallest over the shoulders.
  const crest = [[-15.4, 1.4, 'T'], [-13, 1.8, 'T'], [-10.6, 2.2, 'V'], [-8.2, 2.6, 'T'], [-5.8, 2.9, 'T'], [-3.4, 3.2, 'T'], [-1, 3.3, 'V'], [1.4, 3.2, 'T'], [3.8, 3, 'T'], [6.2, 2.7, 'V'], [8.6, 2.3, 'T'], [11, 1.9, 'T'], [13.2, 1.5, 'T']];
  const ridge = (x) => -11.4 + Math.pow(x / 18, 2) * 5.4;
  const capOf = ([x, h, k], i) => ({ x: x - h * .25, y: ridge(x) + 11.4 - .8 - h * .55, w: 1.3 + h * .32, h: h * .8, lean: -.35 - h * .08, stem: [x, ridge(x) + 11.4 + .5], m: k === 'T' ? TEAL : VIOLET, seed: 0x40 + i, k });
  const caps = crest.map(capOf);
  return {
    id: 'crest', name: 'Б', title: 'Гребень',
    tag: 'Длинный и низкий, в налезающих пластинах, как мокрица в броне. Вдоль хребта гребень тонких светляков, будто ряд грибов на корне. Плоская голова-лопата с крюками; в замахе гребень встаёт дыбом и вспыхивает.',
    centre: [0, -6],
    parts: {
      body: { w: 40, h: 16, px: 20, py: 15, draw(a) {
        const rr = r();
        const out = [[17.6, -4], [17, -7.4], [13.8, -9.8], [8, -11.4], [0, -11.8], [-8, -11.4], [-14, -9.8], [-18.2, -6.8], [-19, -3.6], [-15, -2.4], [0, -2], [12, -2.2]];
        a.shape(hide(out, rr, .2), PLATE, {
          shade: poly(-20, -5.4, -6, -4.6, 8, -4.6, 18, -6, 18, 0, -20, 0), deep: poly(-20, -3, 18, -3.2, 18, 0, -20, 0),
          lite: poly(-12, -10.4, -4, -11.8, 6, -11.6, 4, -10.4, -6, -10.4),
          lines: [-12.6, -7.2, -1.8, 3.6, 9, 13.8].map((x) => run([x - 1.4, ridge(x) + .4], [x - .2, -7], [x + .8, -3.4])).concat([run([-18, -4.4], [-8, -3.6], [8, -3.6], [17, -4.6])]),
        });
        // The back edge of each plate is a lip that throws a shadow on the next.
        for (const x of [-12.6, -7.2, -1.8, 3.6, 9, 13.8]) a.flat(limb([[x - 1.6, ridge(x) + .6, .15], [x - .5, -7, .45], [x + .6, -3.6, .2]]), PLATE.deep);
        a.flat(hide([[-10, -9.6], [-7.6, -10.6], [-5.4, -9.4], [-6.6, -8], [-9, -8.2]], rr, .4), LICHEN);
        a.flat(hide([[5, -9.6], [7.4, -10], [8.8, -8.6], [7, -7.6]], rr, .4), LICHEN);
        a.flat(hide([[-17, -5.6], [-15.4, -6.6], [-14, -5], [-15.6, -4.4]], rr, .3), LICHEN);
        a.flat(poly(-18, -3, 0, -2.6, 16, -3, 15, -2, 0, -1.6, -17, -2), PLATE.deep);
      } },
      crest: { w: 36, h: 12, px: 18, py: 7, draw(a) {
        for (const q of caps) glowcap(a, q);
      } },
      crestLitT: { w: 36, h: 12, px: 18, py: 7, light: true, draw(c) { for (const q of caps.filter((q) => q.k === 'T')) capGlow(c, q); } },
      crestLitV: { w: 36, h: 12, px: 18, py: 7, light: true, draw(c) { for (const q of caps.filter((q) => q.k === 'V')) capGlow(c, q); } },
      head: { w: 17, h: 9, px: 3, py: 4.5, draw(a) {
        const rr = rng(0xcb2);
        a.shape(limb([[7, -.2, .5], [10, -.6, .4], [12.4, -.2, .26], [13.2, .8, .06]]), farOf(PLATE), { edge: false });
        a.shape(hide([[-2.6, -2.2], [2, -2.8], [6, -2.4], [8.8, -1.2], [9.6, .2], [7, 1.2], [2, 1.8], [-2.6, 1.8]], rr, .15), PLATE, {
          shade: poly(-3, -.2, 10, -.6, 10, 3, -3, 3), lite: poly(-1, -2.4, 5, -2.6, 6, -1.8, 0, -1.6), lines: [run([1.8, -2.2], [2.4, 1.4]), run([4.6, -2.1], [6.6, -.6])],
        });
        a.shape(limb([[7.6, .8, .55], [10.6, 1.4, .42], [12.8, 1, .26], [13.6, -.2, .06]]), PLATE, { edge: false, shade: poly(7, 1.4, 15, 1.2, 15, 4, 7, 4) });
        a.line(run([10.2, .8], [10.8, .2]), { w: .9 });
        a.flat('M4 -1.1Q4.8 -1.5 5.8 -1.1Q4.8 -.7 4 -1.1Z', EYE);
        a.line(run([5.6, -2.4], [7.6, -3.6], [10, -3.4]), { w: .9 });
      } },
      eyeLit: { w: 17, h: 9, px: 3, py: 4.5, light: true, draw(c) { fillP(c, ell(5.1, -1.15, .28, .15)); } },
      leg: { w: 8, h: 12, px: 2, py: 4.5, draw: leg(PLATE) },
      legFar: { w: 8, h: 12, px: 2, py: 4.5, draw: leg(farOf(PLATE)) },
    },
    rig: [
      { id: 'l5', part: 'legFar', parent: 'body', x: -13, y: -5, drive: 'leg', phase: .5 },
      { id: 'l6', part: 'legFar', parent: 'body', x: -5, y: -5, drive: 'leg', phase: 0 },
      { id: 'l7', part: 'legFar', parent: 'body', x: 3, y: -5, drive: 'leg', phase: .5 },
      { id: 'l8', part: 'legFar', parent: 'body', x: 10.6, y: -5, drive: 'leg', phase: 0 },
      { id: 'l1', part: 'leg', parent: 'body', x: -15, y: -5, drive: 'leg', phase: 0 },
      { id: 'l2', part: 'leg', parent: 'body', x: -7, y: -5, drive: 'leg', phase: .5 },
      { id: 'l3', part: 'leg', parent: 'body', x: 1, y: -5, drive: 'leg', phase: 0 },
      { id: 'l4', part: 'leg', parent: 'body', x: 8.6, y: -5, drive: 'leg', phase: .5 },
      { id: 'head', part: 'head', parent: 'body', x: 15.6, y: -4.8, drive: 'head', lit: [['eyeLit', 'eye']] },
      { id: 'crest', part: 'crest', parent: 'body', x: 0, y: -11.4, drive: 'caps', lit: [['crestLitT', 'teal'], ['crestLitV', 'violet']] },
      { id: 'body', part: 'body', parent: null, x: 0, y: 0, drive: 'body' },
    ],
    lights: [{ node: 'crest', x: -7, y: -2, r: 18, col: 'teal', k: .7 }, { node: 'crest', x: 6, y: -2, r: 18, col: 'teal', k: .6 }, { node: 'crest', x: -1, y: -3, r: 12, col: 'violet', k: .5 }],
    pose: { drop: 2.6, rise: 2.4, pitch: .2, legUp: 1, tuckX: 6.6, tuckY: 0, tuckRot: .2, headUp: .2, headCrouch: -.35, legFly: 1, legSplay: .6, sagCaps: .2, crestRise: 1.8 },
  };
}

/** «Трутовик»: a high hump of bark-like chitin, shelves of bracket fungus stepping down its back, glowing from beneath; long folded legs and a pale face. */
function capBracket() {
  const BARK = mat('#3e3128', '#2c231c', '#1d1712', '#54443a');
  const SHELF = mat('#5e5443', '#473f32', '#312b22', '#73684f');
  const BONE = mat('#878275', '#67635a', '#4b4841', '#9b968a');
  const VIOLET = mat('#5f4f77', '#483c5a', '#312a3f', '#776790');
  const r = () => rng(0xcc1);
  const leg = (m) => (a) => {
    a.shape(limb([[0, -1.6, 1.05], [1.6, -4.2, .85], [3.6, -5.4, .72, 'c'], [4.6, -2, .58], [5.4, 2.4, .42], [5.6, 5.8, .16]]), m, { shade: poly(3.6, -7, 7, -7, 7, 7, 3.6, 7) });
    a.line(run([3.4, -5.2], [4.2, -4.4]), { w: 1 });
  };
  const bcaps = [{ x: -1.2, y: -4.6, w: 2.2, h: 3, lean: -.5, stem: [-.4, 0], m: VIOLET, seed: 0x61 }, { x: 1.6, y: -2.6, w: 1.7, h: 2.2, lean: .3, stem: [1, 0], m: VIOLET, seed: 0x62 }];
  const shelves = [[-2.4, -1.6, 8], [-4.8, 2.2, 6.8], [-6, 6, 5.4], [-6.4, 9.4, 3.6]];
  const shelf = ([x, y, w]) => hide([[x + 2, y - 1.2], [x - w * .3, y - 1.5], [x - w * .7, y - 1.1], [x - w, y - .1, 'c'], [x - w * .82, y + .35], [x - w * .55, y + .2], [x - w * .3, y + .45], [x, y + .3], [x + 2, y + .6]], rng(0x50 + Math.round(w * 10)), .3);
  return {
    id: 'bracket', name: 'В', title: 'Трутовик',
    tag: 'Высокий горб, как старый пень в коре; по спине уступами растут полки трутовика, светятся снизу. Ноги длинные, сложены коленями вверх; бледное лицо-кость под передним краем. Затаившись, это пень с грибами.',
    centre: [0, -10],
    parts: {
      body: { w: 34, h: 24, px: 17, py: 23, draw(a) {
        const rr = r();
        a.shape(hide([[14, -4.6], [13.6, -8.6], [10.4, -13.2], [5.6, -17.6], [0, -20.8], [-5, -21.6], [-9.6, -19.6], [-13.6, -15], [-15.4, -9.6], [-15.4, -4.4], [-11, -3], [0, -2.6], [9, -2.8]], rr, .35), BARK, {
          shade: poly(-16, -8, -4, -7.4, 8, -7.2, 15, -8.6, 15, 0, -16, 0), deep: poly(-16, -4.4, 15, -4.6, 15, 0, -16, 0),
          lite: poly(-8, -20.4, -3, -21.8, 1, -20.6, -2, -19, -7, -18.6),
          lines: [run([-11.4, -16], [-12.6, -10], [-12, -5.6]), run([-6.6, -19.4], [-8, -12], [-7.4, -5]), run([-1.6, -20], [-2.4, -13], [-1.6, -5]), run([3.6, -17.6], [3.4, -10], [4.4, -5]), run([8.4, -13.4], [9, -6.6])],
        });
        a.flat(poly(-15, -4.4, 0, -3.6, 13.6, -4.6, 12, -2.8, 0, -2.4, -13, -2.8), BARK.deep);
      } },
      shelves: { w: 14, h: 15, px: 10, py: 3, draw(a) {
        for (const s of shelves) {
          const [x, y, w] = s;
          a.shape(shelf(s), SHELF, { shade: poly(x - w - 1, y - .3, x + 3, y - .6, x + 3, y + 2, x - w - 1, y + 2), deep: poly(x - w - 1, y + .15, x + 3, y + .1, x + 3, y + 2, x - w - 1, y + 2),
            lines: [run([x + 1, y - .9], [x - w * .4, y - 1.1], [x - w * .85, y - .4]), run([x + .6, y - .4], [x - w * .45, y - .5], [x - w * .75, y - .1])] });
        }
      } },
      shelvesLit: { w: 14, h: 15, px: 10, py: 3, light: true, draw(c) {
        for (const [x, y, w] of shelves) { fillP(c, limb([[x - w * .8, y + .3, .08], [x - w * .45, y + .3, .2], [x + 1.4, y + .5, .12]]), .9); fillP(c, limb([[x - w * .7, y + 1, .2], [x - w * .4, y + 1.2, .35], [x + 1, y + 1.1, .2]]), .18); }
      } },
      caps: { w: 10, h: 10, px: 5, py: 9, draw(a) {
        for (const q of bcaps) glowcap(a, q);
      } },
      capsLitV: { w: 10, h: 10, px: 5, py: 9, light: true, draw(c) {
        for (const q of bcaps) capGlow(c, q);
      } },
      head: { w: 14, h: 9, px: 2, py: 4, draw(a) {
        const rr = rng(0xcc2);
        a.shape(limb([[5.4, 1.6, .55], [7.8, 1.2, .42], [9.6, .2, .26], [9.8, -.8, .06]]), farOf(BARK), { edge: false });
        a.shape(hide([[-1.6, -2.8], [2.4, -3.2], [5.4, -2.4], [7, -.4], [7, 1.8], [5.2, 3], [2, 3.2], [-1.6, 2.6]], rr, .2), BONE, {
          shade: poly(-2, .8, 9, .2, 9, 5, -2, 5), deep: poly(-2, 2.2, 9, 1.8, 9, 5, -2, 5), lines: [run([1, -2.8], [1.4, 2.6]), run([4, -2.6], [3.4, -1.4])],
        });
        a.flat(hide([[3.8, -1.4], [5.2, -1.8], [6.2, -.8], [5.4, .2], [4.2, -.2]], rr, .1), BONE.deep);
        a.shape(limb([[5.4, 2.6, .6], [8, 3, .46], [9.8, 2.2, .28], [10.4, 1, .06]]), BARK, { edge: false, shade: poly(4, 2.8, 12, 2.6, 12, 6, 4, 6) });
        a.line(run([7.4, 2.4], [8, 1.8]), { w: .9 });
        a.flat('M4.4 -.9Q5.1 -1.3 5.9 -.8Q5.2 -.4 4.4 -.9Z', EYE);
      } },
      eyeLit: { w: 14, h: 9, px: 2, py: 4, light: true, draw(c) { fillP(c, ell(5.3, -1, .26, .16)); } },
      leg: { w: 9, h: 15, px: 1.6, py: 7, draw: leg(BARK) },
      legFar: { w: 9, h: 15, px: 1.6, py: 7, draw: leg(farOf(BARK)) },
    },
    rig: [
      { id: 'l4', part: 'legFar', parent: 'body', x: -8, y: -5.8, drive: 'leg', phase: .5 },
      { id: 'l5', part: 'legFar', parent: 'body', x: -1, y: -5.8, drive: 'leg', phase: 0 },
      { id: 'l6', part: 'legFar', parent: 'body', x: 6, y: -5.8, drive: 'leg', phase: .5 },
      { id: 'head', part: 'head', parent: 'body', x: 12.2, y: -6.4, drive: 'head', lit: [['eyeLit', 'eye']] },
      { id: 'body', part: 'body', parent: null, x: 0, y: 0, drive: 'body' },
      { id: 'shelves', part: 'shelves', parent: 'body', x: -9.4, y: -15.6, drive: 'caps', lit: [['shelvesLit', 'teal']] },
      { id: 'caps', part: 'caps', parent: 'body', x: -3.6, y: -21, drive: 'caps', lit: [['capsLitV', 'violet']] },
      { id: 'l1', part: 'leg', parent: 'body', x: -10.6, y: -5.6, drive: 'leg', phase: 0 },
      { id: 'l2', part: 'leg', parent: 'body', x: -3.4, y: -5.6, drive: 'leg', phase: .5 },
      { id: 'l3', part: 'leg', parent: 'body', x: 3.8, y: -5.6, drive: 'leg', phase: 0 },
    ],
    lights: [{ node: 'shelves', x: -5, y: 3, r: 24, col: 'teal', k: .9 }, { node: 'caps', x: 0, y: -4, r: 14, col: 'violet', k: .7 }],
    pose: { drop: 3.4, rise: 2.6, pitch: .14, legUp: 1, legFold: .5, tuckX: 6, tuckY: -.4, tuckRot: .3, headUp: .2, headCrouch: -.25, legFly: .9, legSplay: .4, sagCaps: .25 },
  };
}

/* ===================== The geode snail ===================== */

const FLESH = mat('#4d4540', '#39322e', '#28221f', '#655a52');
const AMBER = mat('#a87430', '#83581f', '#5c3d15', '#c9913f');

/** The soft parts every snail shares in shape: a tail behind the shell, a neck and head before it, eye stalks. */
function snailSoft(F, opt) {
  const o = opt || {};
  const stalk = (m, len) => (a) => {
    a.shape(limb([[0, .6, .7], [.5, -len * .45, .52], [1.1, -len * .9, .4]]), m, { shade: poly(.4, 1, 3, 1, 3, -len - 2, .7, -len - 2) });
    a.shape(ell(1.15, -len - .2, .62, .5), mat('#221c18', '#191512', '#110e0c', '#3a332e'), { edge: false });
  };
  const L = o.stalk || 8;
  return {
    tail: { w: 16, h: 6, px: 14, py: 5, draw(a) {
      const rr = rng(0xd01);
      a.shape(hide([[1.4, -3.4], [-4, -2.8], [-9, -1.8], [-12.6, -.6], [-13.2, 0, 'c'], [-6, 0], [1.4, 0, 'c']], rr, .15), F, {
        shade: poly(-14, -1.2, 2, -1.6, 2, 1, -14, 1), lines: [run([-3, -2.4], [-2.4, -.6]), run([-7, -1.6], [-6.6, -.4]), run([-10.2, -.8], [-9.8, -.2])],
      });
    } },
    front: { w: 18, h: 13, px: 3, py: 11, draw(a) {
      const rr = rng(0xd02);
      a.shape(hide([[-2.4, 0, 'c'], [-2.6, -4], [1, -5.4], [5, -6.6], [8.4, -8.6], [11.4, -9.2], [13.6, -7.8], [14.2, -5.4], [13, -3.6], [10.6, -2.4], [9.4, -.4], [10.6, 0, 'c']], rr, .15), F, {
        shade: poly(-3, -2.6, 6, -3.4, 15, -5.4, 15, 1, -3, 1), deep: poly(-3, -.9, 15, -1.2, 15, 1, -3, 1),
        lite: poly(4, -6.4, 8, -8.6, 11, -9.2, 9, -8, 5, -5.8),
        lines: [run([2, -4.6], [2.6, -1.2]), run([5.4, -5.6], [6, -2]), run([8.2, -7.2], [8.8, -3.4]), run([12.4, -4.4], [13.8, -4.8])],
      });
      a.flat(limb([[11.8, -3.4, .1], [12.8, -3.8, .25], [13.8, -4.6, .1]]), F.deep);
    } },
    stalk: { w: 5, h: Math.ceil(L + 4), px: 1.5, py: L + 2, draw: stalk(F, L) },
    stalkFar: { w: 5, h: Math.ceil(L + 4), px: 1.5, py: L + 2, draw: stalk(farOf(F), L - .6) },
    eyeLit: { w: 5, h: Math.ceil(L + 4), px: 1.5, py: L + 2, light: true, draw(c) { fillP(c, ell(1.35, -L - .42, .2, .14)); } },
  };
}
const softRig = (shellAt, front, stalks) => [
  { id: 'tail', part: 'tail', parent: 'body', x: shellAt[0] + 1, y: 0, drive: 'tail' },
  { id: 'stalkFar', part: 'stalkFar', parent: 'front', x: stalks[0][0], y: stalks[0][1], drive: 'stalk', base: .5, lit: [['eyeLit', 'eye']] },
  { id: 'front', part: 'front', parent: 'body', x: front, y: 0, drive: 'front' },
];

/** «Жеода»: a round grey stone, pocked and crusted, its mouth a ring of amber teeth; a stone lid swings down over it. */
function snailGeode() {
  const SH = mat('#615f5a', '#4b4945', '#363532', '#787570');
  const LID = mat('#57554f', '#42403c', '#2f2e2b', '#6c6964');
  const R = 11, mouth = [5.4, 5.6, 4.6, 2.2];
  const teeth = cluster(0xd13, mouth[0], mouth[1], mouth[2], mouth[3], 7, -3, -.1).concat(cluster(0xd14, mouth[0], mouth[1], mouth[2], mouth[3], 3, .5, 2.6));
  const soft = snailSoft(FLESH, { stalk: 8.6 });
  return {
    id: 'geode', name: 'А', title: 'Жеода',
    tag: 'Круглый серый камень в оспинах и корке, устье — кольцо янтарных зубьев, что светятся изнутри. Каменная створка падает сверху и закрывает устье с щелчком. Катится как валун.',
    centre: [-1, -12],
    parts: {
      ...soft,
      shell: { w: 26, h: 26, px: 13, py: 13, draw(a) {
        const rr = rng(0xd11);
        const pts = []; for (let i = 0; i < 16; i++) { const t = i / 16 * Math.PI * 2, k = 1 + (rr() - .5) * .1; pts.push([Math.cos(t) * R * k, Math.sin(t) * R * k]); }
        a.shape(hide(pts, rr, .5), SH, {
          shade: [poly(-13, 2, -4, 4, 6, 1, 13, -3, 13, 13, -13, 13)], deep: poly(-13, 7, 13, 4, 13, 13, -13, 13),
          lite: poly(-8, -7.6, -3, -10.4, 2, -10.6, -2, -8.4, -6.4, -5.6),
          lines: [run([-7.4, -3.6], [-4.6, -4.8], [-4, -7.6]), run([1.2, -1.6], [3.6, -3.8], [3.2, -6.4], [5.2, -8]), run([-9.6, 2.4], [-7.4, 4.6]), run([2, -10.4], [3.2, -8.2])],
        });
        for (const [x, y, s] of [[-8.6, -5.4, .9], [-6.8, -6.6, .5], [-7.4, -7.4, .3], [-2.4, 2.8, .7], [-1, 3.8, .4], [4.8, -7.8, .55], [-4.4, 8.4, .8], [7.6, -1.6, .45], [8.4, -.6, .3]]) a.flat(hide([[x - s, y], [x, y - s * .8], [x + s, y], [x, y + s * .7]], rr, .2), SH.deep);
        a.flat(hide([[-9.4, -1.8], [-7.6, -3], [-6.4, -1.4], [-7.8, -.4]], rr, .3), '#6d6c5c');
        a.shape(ell(mouth[0], mouth[1], mouth[2], mouth[3]), mat('#2a1d13', '#1d140d', '#140e09', '#3a2a1c'), { shade: poly(0, 6, 11, 5, 11, 10, 0, 10) });
        crystals(a, teeth, AMBER);
      } },
      mouthLit: { w: 26, h: 26, px: 13, py: 13, light: true, draw(c) {
        fillP(c, ell(mouth[0], mouth[1], mouth[2] * .5, mouth[3] * .45), .16);
        crystalGlow(c, teeth);
      } },
      cracksLit: { w: 26, h: 26, px: 13, py: 13, light: true, draw(c) {
        for (const d of [run([-2, -10.6], [-1.2, -6], [-3, -2.6], [-1.6, 1.6]), run([-10.4, 1], [-6.4, 1.6], [-3, -2.6]), run([4.6, -9.6], [3, -5], [5.2, -1.8]), run([-1.6, 1.6], [.4, 6], [-1, 10.4])]) strokeP(c, d, .5);
      } },
      lid: { w: 14, h: 14, px: 2.6, py: 4.6, draw(a) {
        const rr = rng(0xd12);
        a.shape(hide([[-1.2, -.6], [2.6, -1], [6.4, -.4], [8, 1.4], [7.2, 3.8], [4, 4.8], [.6, 4.4], [-1.4, 2.2]], rr, .3), LID, {
          shade: poly(-3, 2, 9, 1.2, 9, 6, -3, 6), deep: poly(-3, 3.6, 9, 3, 9, 6, -3, 6), lite: poly(-.6, -.4, 3, -.8, 4, -.2, 0, .4), lines: [run([.4, 1.2], [3, 2], [6, 1.6])],
        });
        a.line(run([-1.4, 1.6], [.4, 4], [4, 4.8], [7.4, 3.4]), { w: 1.6 });
      } },
    },
    rig: [
      ...softRig([-1, -12], 4, [[9.6, -8.4]]),
      { id: 'stalk', part: 'stalk', parent: 'front', x: 11.2, y: -8.8, drive: 'stalk', base: .25, lit: [['eyeLit', 'eye']] },
      { id: 'shell', part: 'shell', parent: 'body', x: -1, y: -12, drive: 'shell', lit: [['mouthLit', 'amber'], ['cracksLit', 'crack']] },
      { id: 'lid', part: 'lid', parent: 'shell', x: 1.6, y: 3.6, drive: 'lid' },
    ],
    lights: [{ node: 'shell', x: 5.4, y: 5.6, r: 26, col: 'amber', k: .9, mouth: true }],
    pose: { tailIn: 9, frontIn: 11, lidOpen: -2.1, stalkBack: 1.5, radius: 11, drop: 1, rock: .12 },
  };
}

/** «Жернов»: a flat coiled stone, a wheel of sandstone strata; amber crystal veins glow along its coil even shut, and a stone plug seals its mouth. */
function snailWheel() {
  const SH = mat('#5f584f', '#4a443d', '#35312c', '#736b61');
  const PLUG = mat('#625b52', '#4b453e', '#35312c', '#7a7268');
  const BODY = mat('#6a6560', '#504c48', '#383532', '#827c76');
  const R = 11.6;
  const spiral = (t0, t1, k) => { const pts = []; for (let t = t0; t <= t1; t += .18) { const r = R * (.18 + .82 * t / (Math.PI * 4.2)) * (k || 1); pts.push([Math.cos(t) * r, Math.sin(t) * r]); } return pts; };
  const coil = spiral(.8, Math.PI * 4.2, .97);
  const mouthC = cluster(0xd23, 7.6, 7.4, 3.6, 1.8, 6, -3, -.1).concat(cluster(0xd24, 7.6, 7.4, 3.6, 1.8, 2, .6, 2.4));
  // Crystals grown through the coil's seam: a few short runs, each with specks of crystal along it.
  const veins = [[14, 31], [44, 66]].map(([i, j]) => coil.slice(i, j));
  const specks = veins.flatMap((v) => v.filter((_, i) => i % 3 === 1).map((p, i) => ({ x: p[0], y: p[1], a: Math.atan2(p[1], p[0]) + (i % 2 ? .4 : -.4), len: .7 + (i % 3) * .25, w: .26 })));
  const soft = snailSoft(BODY, { stalk: 6.2 });
  return {
    id: 'millstone', name: 'Б', title: 'Жернов',
    tag: 'Плоская свёрнутая раковина, колесо из песчаника слоями. По шву завитка проросли янтарные жилы и светятся даже закрытой, а в прокате спираль вертится огнём. Устье затыкает каменная пробка.',
    centre: [-1, -12.4],
    parts: {
      ...soft,
      shell: { w: 27, h: 27, px: 13.5, py: 13.5, draw(a) {
        const rr = rng(0xd21);
        const pts = []; for (let i = 0; i < 18; i++) { const t = i / 18 * Math.PI * 2, k = 1 + (rr() - .5) * .06; pts.push([Math.cos(t) * R * k, Math.sin(t) * R * k]); }
        a.shape(hide(pts, rr, .35), SH, {
          shade: poly(-14, 1, 0, 3, 14, -2, 14, 14, -14, 14), deep: poly(-14, 7, 14, 4, 14, 14, -14, 14), lite: poly(-9, -6, -4, -10, 1, -11, -3, -8, -7, -4),
          lines: [run(...coil.slice(12, 40)), run(...coil.slice(39)), run([-10, -2], [-6, -1.4]), run([-9, 4], [-5.6, 4.4]), run([4, 8], [7.4, 6.4])],
        });
        a.shape(hide([[3.8, 7.4], [6.2, 5.8], [9.4, 5.4], [11.2, 6.8], [10.4, 8.6], [7.4, 9.2], [4.6, 8.6]], rr, .15), mat('#2a1d13', '#1d140d', '#140e09', '#3a2a1c'), {});
        crystals(a, mouthC, AMBER);
        for (const v of veins) a.flat(limb(v.map((p, i) => [p[0], p[1], .12 + .14 * Math.sin(Math.PI * i / (v.length - 1))])), AMBER.deep);
        crystals(a, specks, AMBER);
      } },
      veinsLit: { w: 27, h: 27, px: 13.5, py: 13.5, light: true, draw(c) {
        for (const v of veins) fillP(c, limb(v.map((p, i) => [p[0], p[1], .06 + .1 * Math.sin(Math.PI * i / (v.length - 1))])), .7);
        crystalGlow(c, specks, .6);
      } },
      mouthLit: { w: 27, h: 27, px: 13.5, py: 13.5, light: true, draw(c) {
        fillP(c, ell(7.6, 7.4, 2, .9), .14);
        crystalGlow(c, mouthC);
      } },
      cracksLit: { w: 27, h: 27, px: 13.5, py: 13.5, light: true, draw(c) {
        for (const d of [run([-3, -11], [-2, -5.6], [-4.2, -1], [-2, 3], [-3.2, 11]), run([-11, -2.4], [-6.6, -1.6], [-4.2, -1]), run([5.4, -10], [3.6, -4.4], [6.4, .4])]) strokeP(c, d, .5);
      } },
      plug: { w: 10, h: 10, px: 5, py: 5, draw(a) {
        const rr = rng(0xd22);
        a.shape(hide([[-3.8, .2], [-2.2, -1.8], [1.4, -2.2], [3.8, -1], [4, 1.2], [1.6, 2.6], [-2, 2.4]], rr, .25), PLUG, { shade: poly(-5, .6, 5, -.2, 5, 4, -5, 4), lines: [run([-2.2, -.6], [1.4, .2])] });
        a.line(run([-3.6, .8], [-1.8, 2.4], [1.8, 2.6], [3.9, 1]), { w: 1.6 });
      } },
    },
    rig: [
      ...softRig([-1, -12.4], 3, [[10.2, -8]]),
      { id: 'stalk', part: 'stalk', parent: 'front', x: 11.8, y: -8.4, drive: 'stalk', base: .15, lit: [['eyeLit', 'eye']] },
      { id: 'shell', part: 'shell', parent: 'body', x: -1, y: -12.4, drive: 'shell', lit: [['veinsLit', 'vein'], ['mouthLit', 'amber'], ['cracksLit', 'crack']] },
      { id: 'plug', part: 'plug', parent: 'shell', x: 7.6, y: 7.2, drive: 'plug' },
    ],
    lights: [{ node: 'shell', x: 7.6, y: 7.4, r: 22, col: 'amber', k: .8, mouth: true }, { node: 'shell', x: -2, y: -2, r: 18, col: 'amber', k: .45 }],
    pose: { tailIn: 9, frontIn: 10, plugOut: [-3.4, -2.6], stalkBack: 1.6, radius: 11.6, drop: .8, rock: .14 },
  };
}

/** «Пасть»: a split geode, two crusted stone halves on a hinge at the back; open, a maw of amber crystal teeth and banded agate, the soft body between. */
function snailMaw() {
  const CRUST = mat('#5c544b', '#47413a', '#332e29', '#706659');
  const AGATE = mat('#9b9184', '#7a7166', '#5a534b', '#b2a899');
  const BODY = mat('#4a3f45', '#372f34', '#272125', '#61545b');
  const soft = snailSoft(BODY, { stalk: 9.4 });
  // The two halves meet along y = 0 of the shell node; the hinge is at its back.
  const lower = [[-11.6, 0, 'c'], [-11, 4], [-8, 8.4], [-3, 10.6], [3, 10.6], [8, 8.4], [11, 4], [11.6, 0, 'c']];
  const upper = [[11.6, 0, 'c'], [11, -4.4], [8, -8.6], [3, -10.8], [-3, -10.8], [-8, -8.6], [-11, -4.4], [-11.6, 0, 'c']];
  const row = (seed, y0, dir, n) => { const r = rng(seed), out = []; for (let i = 0; i < n; i++) { const x = -8.6 + (17.6 * (i + .2 + r() * .6)) / n, edge = Math.abs(x) / 10.4; out.push({ x, y: y0 + dir * (1.2 - edge * edge) * 1.6, a: dir > 0 ? -Math.PI / 2 + (r() - .5) * .9 : Math.PI / 2 + (r() - .5) * .9, len: 1.8 + r() * 2.4 * (1 - edge * .5), w: .35 + r() * .4 }); } return out; };
  const teethLow = row(0xd35, .3, 1, 8), teethUp = row(0xd36, -.3, -1, 7).map((q) => ({ ...q, x: q.x + 11.6 }));
  return {
    id: 'maw', name: 'В', title: 'Пасть',
    tag: 'Расколотая жеода: две половины в каменной корке на петле сзади. Открыта — пасть из янтарных зубьев и полос агата, между ними мягкое тело. Захлопывается с щелчком и катится круглым камнем.',
    centre: [-1, -11.6],
    parts: {
      ...soft,
      shell: { w: 26, h: 14, px: 13, py: 1.5, draw(a) {
        const rr = rng(0xd31);
        a.shape(hide(lower, rr, .45), CRUST, {
          shade: 1.8, deep: poly(-13, 8.4, 13, 7, 13, 13, -13, 13), lines: [run([-8.4, 4.4], [-5.6, 6.6]), run([3, 6.8], [6.6, 5]), run([-10.6, 1.4], [0, 2.6], [10.6, 1.4])],
        });
      } },
      // The cut faces: seen only while the halves are apart.
      gums: { w: 26, h: 14, px: 13, py: 1.5, draw(a) {
        const rr = rng(0xd33);
        a.shape(hide([[-10.4, 0, 'c'], [-9.6, 2.4], [-5, 4.4], [5, 4.4], [9.6, 2.4], [10.4, 0, 'c']], rr, .15), AGATE, { outer: false, shade: poly(-11, 2.2, 11, 2.2, 11, 6, -11, 6), lines: [run([-9, 1], [0, 2.8], [9, 1])] });
        crystals(a, teethLow, AMBER);
      } },
      lowerLit: { w: 26, h: 14, px: 13, py: 1.5, light: true, draw(c) { crystalGlow(c, teethLow); strokeP(c, run([-9, 1], [0, 2.8], [9, 1]), .4, .45); } },
      upper: { w: 26, h: 14, px: 1.5, py: 12.5, draw(a) {
        const rr = rng(0xd32);
        const up = upper.map((p) => [p[0] + 11.6, p[1], p[2]]);
        a.shape(hide(up, rr, .45), CRUST, {
          shade: 1.6, lite: poly(5, -9, 11, -11.2, 17, -10.4, 12, -9, 7, -7.4),
          lines: [run([6, -6], [10, -8.4], [15, -8]), run([18.6, -6.6], [20.8, -3.6]), run([3, -3.6], [4.4, -1.4])],
        });
        for (const [x, y, s] of [[4.2, -5.6, .9], [5.8, -4.4, .55], [6.4, -6.8, .4], [16.6, -7.2, .35]]) a.flat(hide([[x - s, y], [x, y - s * .8], [x + s, y], [x, y + s * .7]], rr, .2), CRUST.deep);
      } },
      upperGums: { w: 26, h: 14, px: 1.5, py: 12.5, draw(a) {
        const rr = rng(0xd34);
        a.shape(hide([[1.4, 0, 'c'], [2.2, -1.6], [6.6, -3], [16.6, -3], [21, -1.6], [21.8, 0, 'c']], rr, .15), AGATE, { outer: false, shade: poly(0, -1.4, 24, -1.4, 24, 1, 0, 1), lines: [run([2.6, -.8], [11.6, -2.4], [20.6, -.8])] });
        crystals(a, teethUp, AMBER);
      } },
      upperLit: { w: 26, h: 14, px: 1.5, py: 12.5, light: true, draw(c) { crystalGlow(c, teethUp); strokeP(c, run([2.6, -.8], [11.6, -2.4], [20.6, -.8]), .4, .45); } },
      cracksLit: { w: 26, h: 14, px: 13, py: 1.5, light: true, draw(c) { for (const d of [run([-6, .4], [-4.6, 4], [-6, 7.6]), run([2.4, .4], [3.6, 5], [1.6, 9.8]), run([7, 2], [9.6, 5.4])]) strokeP(c, d, .5); } },
      upperCracksLit: { w: 26, h: 14, px: 1.5, py: 12.5, light: true, draw(c) { for (const d of [run([7, -.4], [8.4, -4.6], [6.6, -8.4]), run([15, -.6], [14, -5.4], [16.4, -9.2])]) strokeP(c, d, .5); } },
    },
    rig: [
      { id: 'tail', part: 'tail', parent: 'body', x: 0, y: 0, drive: 'tail' },
      { id: 'shell', part: 'shell', parent: 'body', x: -1, y: -11.6, drive: 'shell', lit: [['cracksLit', 'crack']] },
      { id: 'gums', part: 'gums', parent: 'shell', x: 0, y: 0, drive: 'gums', lit: [['lowerLit', 'amber']] },
      { id: 'stalkFar', part: 'stalkFar', parent: 'front', x: 9.6, y: -8.2, drive: 'stalk', base: .55, lit: [['eyeLit', 'eye']] },
      { id: 'front', part: 'front', parent: 'body', x: 2, y: -1.4, drive: 'front' },
      { id: 'stalk', part: 'stalk', parent: 'front', x: 11.2, y: -8.6, drive: 'stalk', base: .3, lit: [['eyeLit', 'eye']] },
      { id: 'upper', part: 'upper', parent: 'shell', x: -11.6, y: 0, drive: 'jaw', lit: [['upperCracksLit', 'crack']] },
      { id: 'upperGums', part: 'upperGums', parent: 'upper', x: 0, y: 0, drive: 'gums', lit: [['upperLit', 'amber']] },
    ],
    lights: [{ node: 'shell', x: 2, y: 0, r: 30, col: 'amber', k: 1, mouth: true }],
    pose: { tailIn: 8, frontIn: 9, frontDown: 3, jawOpen: -.62, stalkBack: 1.6, radius: 11.4, drop: .6, rock: .14 },
  };
}

const DESIGNS = { capback: [capHummock(), capCrest(), capBracket()], geodeSnail: [snailGeode(), snailWheel(), snailMaw()] };
/** The candidate each creature's pick page chose, by id (the user, 2026-11-07): the export writes only these. */
const PICKED = { capback: 'hummock', geodeSnail: 'millstone' };
const FOLDER = { capback: 'inkCapback', geodeSnail: 'inkGeodeSnail' };
const MARGIN = 1.5;

/** A part that shines: plain white paths, no water. */
function plain(q) {
  const cv = Anime.canvas((q.w + MARGIN * 2) * 4, (q.h + MARGIN * 2) * 4), c = cv.getContext('2d');
  c.setTransform(4, 0, 0, 4, (q.px + MARGIN) * 4, (q.py + MARGIN) * 4);
  c.lineCap = c.lineJoin = 'round';
  q.draw(c);
  return cv;
}
/** A part's finished picture: a light part plain, the rest through the finish. */
const picture = (q) => (q.light ? plain(q) : Anime.bake({ w: q.w, h: q.h, margin: MARGIN, shift: [q.px, q.py], draw: q.draw }));

const num = (v) => String(+v.toFixed(3));
const up = (s) => s[0].toUpperCase() + s.slice(1);
/** The pose numbers the game's rig reads (LurkerRig), by their names there. */
const POSE = ['drop', 'rise', 'pitch', 'legFold', 'tuckX', 'tuckY', 'tuckRot', 'headUp', 'headCrouch', 'legFly', 'legSplay', 'sagCaps', 'tailIn', 'frontIn', 'stalkBack', 'radius', 'rock'];

/** A picked design as the game's record: its nodes in draw order, a snail's partless body first, parents by place. */
function record(look, d) {
  const I = '            ';
  const rig = d.rig.some((n) => n.id === 'body') ? d.rig : [{ id: 'body', parent: null, x: 0, y: 0, drive: 'body' }, ...d.rig];
  const at = (id) => rig.findIndex((n) => n.id === id);
  const parts = Object.entries(d.parts).map(([name, q]) => `${I}    ["${name}"] = new(${q.w + MARGIN * 2}, ${q.h + MARGIN * 2}, ${num(q.px + MARGIN)}f, ${num(q.py + MARGIN)}f, []),`);
  const nodes = rig.map((n) => {
    const more = [n.phase ? `Phase: ${num(n.phase)}` : '', n.base ? `Base: ${num(n.base)}` : '', n.lit ? `Lit: [${n.lit.map(([p, k]) => `("${p}", LurkerShine.${up(k)})`).join(', ')}]` : ''].filter(Boolean).map((s) => ', ' + s).join('');
    return `${I}    new("${n.id}", ${n.part ? `"${n.part}"` : 'null'}, ${n.parent ? at(n.parent) : -1}, ${num(n.x)}, ${num(n.y)}, LurkerDrive.${up(n.drive)}${more}),`;
  });
  const lights = d.lights.map((l) => `${I}    new(${at(l.node)}, ${num(l.x)}, ${num(l.y)}, ${num(l.r)}, LurkerShine.${up(l.col)}, ${num(l.k)}${l.mouth ? ', Mouth: true' : ''}),`);
  const p = d.pose, pose = POSE.filter((k) => p[k]).map((k) => `${up(k)}: ${num(p[k])}`);
  if (p.plugOut) pose.push(`PlugOut: (${num(p.plugOut[0])}, ${num(p.plugOut[1])})`);
  return `        ["${look}"] = new("${FOLDER[look]}", Snail: ${!!p.radius}, Centre: (${num(d.centre[0])}, ${num(d.centre[1])}),
            Parts: new Dictionary<string, Part>
            {
${parts.join('\n')}
            },
            Pose: new(${pose.join(', ')}),
            Nodes:
            [
${nodes.join('\n')}
            ],
            Lights:
            [
${lights.join('\n')}
            ]),`;
}

Subjects.cavehostiles = function () {
  const picked = Object.keys(DESIGNS).map((kind) => [kind, DESIGNS[kind].find((c) => c.id === PICKED[kind])]).filter(([, d]) => d);
  for (const [kind, d] of picked) for (const [name, q] of Object.entries(d.parts)) out(`creatures/${FOLDER[kind]}/${name}`, picture(q));
  writeText('../../Content/logic/LurkerInk.cs', `// Generated by tools/anime-art/subjects/cavehostiles.js: export again, never edit.
using System.Collections.Generic;

namespace Stillwake.Game;

public static partial class CreatureArt
{
    /// <summary>The cave lurkers drawn in ink and watercolour (cave-hostiles), by the look they stand in for: their parts in <c>art/hd/creatures/{id}</c>.</summary>
    public static readonly IReadOnlyDictionary<string, LurkerDesign> Lurkers = new Dictionary<string, LurkerDesign>
    {
${picked.map(([kind, d]) => record(kind, d)).join('\n')}
    };
}
`);
};
window.CaveHostiles = { DESIGNS, PICKED, MARGIN, picture, kit: { hide, run, limb, mat, mul, poly, ell, rng, glowcap, capOutline, fillP, strokeP, STONE, AMBER } };
})();

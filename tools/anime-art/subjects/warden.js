// The Istukan, picked on a page («Латник», 2026-11-01-guards-in-ink): each part placed by hand, posed in the game by game/features/creatures/logic/IstukanRig.cs.
// World pixels: a body's origin is the middle of its feet, facing right, y down; a part's origin is its pivot. Also writes its numbers and key poses into Content/logic/IstukanInk.cs.
(function () {
'use strict';
const { poly, rng } = Anime;

/* ---------- The kit: as the loper's ---------- */

const cr = (a, b, c, d, t) => .5 * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (3 * b - a - 3 * c + d) * t * t * t);
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
function hide(pts, r, amp) {
  const s = spline(pts, true), n = s.length, out = [];
  s.forEach((p, i) => {
    const a = s[(i + n - 1) % n], b = s[(i + 1) % n], dx = b[0] - a[0], dy = b[1] - a[1], l = Math.hypot(dx, dy) || 1, k = r ? (r() - .5) * (amp === undefined ? .4 : amp) : 0;
    out.push(p[0] - (dy / l) * k, p[1] + (dx / l) * k);
  });
  return poly(out);
}
function limb(pts) {
  const s = spline(pts, false), L = [], R = [];
  s.forEach((p, i) => {
    const a = s[Math.max(0, i - 1)], b = s[Math.min(s.length - 1, i + 1)], dx = b[0] - a[0], dy = b[1] - a[1], l = Math.hypot(dx, dy) || 1;
    L.push(p[0] - (dy / l) * p[2], p[1] + (dx / l) * p[2]);
    R.unshift(p[1] - (dx / l) * p[2]); R.unshift(p[0] + (dy / l) * p[2]);
  });
  return poly(L.concat(R));
}
const run = (...pts) => 'M' + spline(pts, false, .8).map((p) => `${p[0].toFixed(2)} ${p[1].toFixed(2)}`).join('L');
const mul = (hex, k) => '#' + [1, 3, 5].map((i) => Math.round(parseInt(hex.substr(i, 2), 16) * k).toString(16).padStart(2, '0')).join('');
const mat = (base, shade, deep, lite, ink) => ({ base, shade, deep, lite, spec: lite, ink: ink || '#0e0d10' });
const farOf = (m) => mat(mul(m.base, .68), mul(m.shade, .7), mul(m.deep, .74), mul(m.base, .8), m.ink);
/** A plate: straight sides between hard corners, its edge a little off the rule. */
const pl = (r, pts, amp) => hide(pts.map((p) => [p[0], p[1], 'c']), r, amp === undefined ? .1 : amp);
/** A part's picture from its bounds. */
const box = (l, t, r, b, draw) => ({ w: r - l, h: b - t, px: -l, py: -t, draw });
const glowBox = (l, t, r, b, draw) => ({ w: r - l, h: b - t, px: -l, py: -t, light: true, draw });
/** Rust at a seam: a torn edge and a darker heart. */
function rust(a, r, x, y, w, h, col, heart) {
  a.flat(hide([[x - w, y - h * .3], [x - w * .2, y - h], [x + w * .6, y - h * .6], [x + w, y + h * .2], [x + w * .2, y + h], [x - w * .7, y + h * .6]], r, .55), col);
  if (heart) a.flat(hide([[x - w * .4, y], [x, y - h * .45], [x + w * .4, y], [x, y + h * .45]], r, .25), heart);
}
const stud = (a, x, y, col, s) => { const q = s || .42; a.flat(poly(x - q, y - q * .8, x + q, y - q, x + q * .9, y + q, x - q * .9, y + q * .8), col); };
/** The makers' script: a few short strokes in a cell, cut in a plate. */
const SCRIPT_CELLS = [
  [[[0, 0], [1, 0]], [[.5, 0], [.5, 1]]], [[[0, 0], [0, 1], [1, 1]]], [[[0, 1], [.5, 0], [1, 1]]], [[[0, 0], [1, 1]], [[0, .55], [.5, .55]]],
  [[[0, .5], [1, .5]], [[.3, 0], [.3, 1]], [[.8, .2], [.8, .8]]], [[[0, 0], [.5, .5], [0, 1]], [[1, 0], [1, 1]]],
];
function script(a, x, y, size, across, cells, w) {
  cells.forEach((c, i) => {
    const ox = across ? x + i * size * 1.45 : x, oy = across ? y : y + i * size * 1.45;
    for (const stroke of SCRIPT_CELLS[c]) a.line('M' + stroke.map((p) => `${(ox + p[0] * size).toFixed(2)} ${(oy + p[1] * size).toFixed(2)}`).join('L'), { w: w || 1.2 });
  });
}

/* ---------- Light: plain paths, no water, for it is the part that shines ---------- */

const ARC = '#70c6ff', ARC_HOT = '#d8f1ff', VIOLET = '#b493ff', VIOLET_LOW = '#7552e0', VIOLET_HOT = '#fdfaff';
const stroke = (c, d, col, wd) => { c.strokeStyle = col; c.lineWidth = wd; c.stroke(new Path2D(d)); };
const fill = (c, d, col) => { c.fillStyle = col; c.fill(new Path2D(d)); };
const seams = (l, t, r, b, paths, col, hot, wd) => glowBox(l, t, r, b, (c) => { for (const d of paths) { stroke(c, d, col, wd || .55); stroke(c, d, hot, (wd || .55) * .4); } });
/** The lamp, white: the game colours it by what it is about. */
const lampsOf = (l, t, r, b, shape, heart) => ({ lamp: glowBox(l, t, r, b, (c) => fill(c, shape, '#fff')), lamp_heart: glowBox(l, t, r, b, (c) => fill(c, heart, '#fff')) });
const coreOf = (l, t, r, b, outer, mid, heart) => glowBox(l, t, r, b, (c) => { fill(c, outer, VIOLET_LOW); fill(c, mid, VIOLET); fill(c, heart, VIOLET_HOT); });

/* ---------- «Латник»: the plated warrior, today's idea redrawn ---------- */

function candidateA() {
  const STEEL = mat('#5e717a', '#44545d', '#2f3b43', '#7a8e97'), FRAME = mat('#2a2d32', '#1f2226', '#15171a', '#3b4046');
  const BRASS = mat('#8c7545', '#65532f', '#443721', '#a58f5e'), RUST = '#65402d', RUSTD = '#432a1e';
  const r = () => rng(0xa51);
  const seamT = [run([-6.8, -11], [-2.8, -11.8], [-.8, -10.4]), run([1.8, -6], [4.6, -6.8], [6.8, -8]), run([.8, -17], [.2, -11], [1.4, -6])];
  const seamF = [run([-2.8, 1.6], [-3, 6], [-3.2, 10])];
  const seamS = [run([2.1, .6], [2.3, 6.4])];

  const arm = (S, B, F) => (a) => {
    const rr = r();
    a.shape(pl(rr, [[-2.1, 2], [2.1, 2], [1.9, 11.4], [-1.9, 11.4]]), F, { shade: poly(.2, 1, 3, 1, 3, 12, .2, 12), lines: [run([-1.6, 7], [1.6, 7])] });
    // The flange: a shoulder plate rising past the helm, as a temple guardian's; three lames under it.
    a.shape(pl(rr, [[-5, 3], [-5.8, -2], [-5.6, -7], [-4.2, -12.4], [-1.8, -8.4], [1.6, -7], [4.6, -4], [5.2, .8], [3, 3.4], [-1.6, 4.4]]), S, {
      shade: poly(.6, -8, 6, -5, 6, 5, -4, 5, -1.4, 1, .6, -2.4), deep: poly(-6, 2, 6, 1, 6, 5, -6, 5), lite: poly(-5, -7, -4, -11, -2.6, -8, -3.4, -3, -5, -2),
      lines: [run([-5.4, -2.6], [-1.4, -3.6], [4.4, -3.4]), run([-5.2, .8], [-.6, -.2], [5, .2]), run([-3.8, -10.6], [-3, -6])],
    });
    a.flat(limb([[-4.8, 3, .35], [-1.6, 4.1, .42], [2.8, 3.2, .4], [5, .8, .3]]), B.base);
    rust(a, rr, 3, 1.8, 1.4, .8, RUST, RUSTD);
    stud(a, -3.8, -5, F.base); stud(a, .4, -5.2, F.base); stud(a, 3.2, -3.4, F.base);
  };
  const fore = (S, B, F) => (a) => {
    const rr = r();
    // A long plated bone that ends in a block with no fingers.
    a.shape(pl(rr, [[-2.6, -1.4], [2.6, -1.4], [3.1, 5], [3.3, 10.4], [-3.4, 10.4], [-3.3, 5]]), S, {
      shade: poly(.5, -2, 4, -2, 4, 11, -.6, 11), lines: [run([-3, 4], [.4, 4.6], [3, 4])],
    });
    a.shape(pl(rr, [[-3.6, 9.8], [3.6, 9.6], [4.5, 11.4], [4.6, 15], [2.4, 16.6], [-2.6, 16.6], [-4.7, 15], [-4.6, 11.4]]), F, {
      shade: poly(-.2, 9, 5, 9, 5, 17, -.2, 17), lines: [run([-2.4, 11.4], [-2.4, 16]), run([.2, 11.2], [.2, 16.2]), run([2.6, 11], [2.5, 15.6])],
    });
    a.flat(limb([[-3.5, 9.9, .35], [0, 10.2, .4], [3.5, 9.8, .3]]), B.base);
    a.flat(poly(-4.5, 12.6, 4.5, 12.4, 4.5, 13.2, -4.5, 13.4), B.shade);
    rust(a, rr, -1.2, 7.6, 1.2, 1, RUST, RUSTD);
    script(a, .4, 5.8, .8, false, [1], .9);
    a.line(seamF[0], { w: 1 });
  };
  const thigh = (S, F) => (a) => {
    const rr = r();
    a.shape(pl(rr, [[-3, -2.2], [3, -2.2], [3.1, 4], [2.4, 11.8], [-2.4, 11.8], [-3.1, 4]]), F, { shade: poly(.2, -3, 4, -3, 4, 12, .2, 12) });
    a.shape(pl(rr, [[-.4, -1.8], [3, -1.8], [3.1, 4], [2.5, 9], [.4, 8]]), S, { shade: poly(1.7, -3, 4, -3, 4, 10, 1, 10), lines: [run([.1, 3], [3, 3.4])] });
  };
  const shin = (S, B, F) => (a) => {
    const rr = r();
    a.shape(pl(rr, [[-2.6, -1.4], [2.8, -1.8], [3.2, 3], [2.5, 10.6], [-2.2, 10.6], [-2.8, 3]]), S, { shade: poly(.5, -3, 4, -3, 4, 11, -.2, 11), deep: poly(-3, 7.4, 4, 7.2, 4, 11, -3, 11), lines: [run([-2.4, 3], [2.9, 2.6])] });
    a.flat(limb([[-2.4, -1, .3], [.2, -.6, .36], [2.8, -1.2, .28]]), B.base);
    a.line(seamS[0], { w: 1 });
  };
  const foot = (S, F) => (a) => {
    const rr = r();
    a.shape(pl(rr, [[-3.6, 0], [-3.4, -2], [-2, -3.4], [2, -3.4], [4.6, -1.6], [6, 0]]), S, { shade: poly(-4, -1.3, 7, -1.3, 7, 1, -4, 1), lines: [run([1.6, -3.2], [2.4, -.4])] });
    a.flat(poly(-3.5, -.5, 5.8, -.5, 6, 0, -3.6, 0), F.base);
  };
  const NF = { S: farOf(STEEL), B: farOf(BRASS), F: farOf(FRAME) };
  return {
    parts: {
      thigh: box(-4, -3.5, 4, 13, thigh(STEEL, FRAME)), thighFar: box(-4, -3.5, 4, 13, thigh(NF.S, NF.F)),
      shin: box(-4, -3, 4, 11.5, shin(STEEL, BRASS, FRAME)), shinFar: box(-4, -3, 4, 11.5, shin(NF.S, NF.B, NF.F)),
      shin_seam: seams(-4, -3, 4, 11.5, seamS, ARC, ARC_HOT),
      foot: box(-5, -4.5, 7, 1.5, foot(STEEL, FRAME)), footFar: box(-5, -4.5, 7, 1.5, foot(NF.S, NF.F)),
      arm: box(-7, -13.5, 6.5, 12.5, arm(STEEL, BRASS, FRAME)), armFar: box(-7, -13.5, 6.5, 12.5, arm(NF.S, NF.B, NF.F)),
      fore: box(-5.5, -2.5, 5.5, 17.5, fore(STEEL, BRASS, FRAME)), foreFar: box(-5.5, -2.5, 5.5, 17.5, fore(NF.S, NF.B, NF.F)),
      fore_seam: seams(-5.5, -2.5, 5.5, 17.5, seamF, ARC, ARC_HOT),
      hips: box(-6.5, -3, 6.5, 8, (a) => {
        const rr = r();
        a.shape(pl(rr, [[-4.2, -2], [4.4, -2], [5.2, 2.2], [4.4, 5.6], [1.8, 6.8], [-1.4, 6.2], [-4, 5.8], [-5.2, 2.2]]), STEEL, {
          shade: poly(.8, -3, 6, -3, 6, 8, -6, 8, -6, 4, -.6, 2.6), deep: poly(-6, 5, 6, 4.4, 6, 8, -6, 8),
          lines: [run([-4.8, .8], [0, 1.6], [4.8, .8]), run([-5, 3.4], [-.4, 4.2], [4.8, 3.4]), run([.6, 4], [1, 6.4])],
        });
        a.flat(limb([[-4, -1.6, .4], [0, -1, .45], [4.2, -1.6, .4]]), BRASS.base);
        rust(a, rr, 3, 2.8, 1.2, .9, RUST, RUSTD);
      }),
      torso: box(-9.5, -20.5, 9.5, 3, (a) => {
        const rr = r();
        // A wedge narrowing hard to a waist no wider than a man's.
        a.shape(pl(rr, [[-3.8, 1.6], [-5, -5], [-8, -12], [-7.8, -17], [-3.8, -19.4], [3, -19.4], [7, -17], [8.6, -11.6], [6.4, -5], [3.8, 1.6]]), FRAME, { shade: poly(0, -20, 10, -20, 10, 3, 0, 3), lines: [run([-3, -2.4], [3, -2.4]), run([-3.8, -.2], [3.2, -.2])] });
        a.shape(pl(rr, [[-7.8, -16.4], [-3.6, -19], [.2, -19], [-.7, -10.6], [-2, -5.4], [-5, -5.6], [-7.8, -11.6]]), STEEL, {
          shade: poly(-9, -10, -.8, -12, -.8, -4, -9, -4), deep: poly(-6, -7.2, -1.6, -7.4, -1.6, -4, -6, -4),
          lines: [run([-7.4, -13.2], [-3.6, -14.2], [-.4, -14]), run([-6.6, -8.4], [-1.4, -9])],
        });
        script(a, -5.6, -17.4, .75, true, [0, 4, 2], .85);
        rust(a, rr, -4.8, -6.4, 1.4, .9, RUST, RUSTD);
        a.shape(pl(rr, [[1.4, -19], [6.6, -16.8], [8.2, -11.6], [6.2, -6], [2.2, -5], [.5, -11]]), STEEL, {
          shade: poly(4.6, -15, 9, -13, 9, -3, 1, -3, 1.8, -6.4, 5, -8), deep: poly(1, -5.8, 7, -7, 7, -3, 1, -3),
        });
        a.flat(limb([[1.4, -19, .4], [.5, -11, .45], [2.2, -5.1, .35]]), BRASS.base);
        a.flat(limb([[2.4, -5.2, .3], [6.3, -6.2, .34]]), BRASS.base);
        // The core's socket, cut in the breast: glass over nothing until it wakes.
        a.shape(poly(4.4, -14, 6.4, -11.4, 4.5, -8.4, 2.5, -11.2), FRAME, { outer: false, shade: poly(4.4, -14, 6.4, -11.4, 4.5, -11, 2.5, -11.2) });
        stud(a, 2.2, -16, FRAME.base); stud(a, 6.4, -14.8, FRAME.base); stud(a, 6.6, -8, FRAME.base); stud(a, 2.6, -7, FRAME.base);
        for (const d of seamT) a.line(d, { w: 1 });
      }),
      torso_seam: seams(-9.5, -20.5, 9.5, 3, seamT, ARC, ARC_HOT),
      core: coreOf(-3.5, -4, 3.5, 4, poly(0, -2.6, 1.8, 0, 0, 2.6, -1.8, 0), poly(0, -1.8, 1.2, 0, 0, 1.8, -1.2, 0), poly(0, -.8, .55, 0, 0, .8, -.55, 0)),
      collar: box(-7, -8.5, 4, 4, (a) => {
        const rr = r();
        a.shape(pl(rr, [[-5.8, 3], [-6, -3], [-4.4, -7], [-2, -6.6], [-1.4, -2], [1.4, 3]]), FRAME, { shade: poly(-3.2, -8, 3, -8, 3, 4, -3.2, 4), lines: [run([-5.6, -1.8], [-2.4, -2.2])] });
        a.flat(limb([[-4.4, -6.9, .3], [-2, -6.5, .34], [-1.4, -2, .3]]), BRASS.shade);
      }),
      head: box(-5, -8.5, 6.5, 3, (a) => {
        const rr = r();
        // Smaller than a man's head: a block of a helm with one slit under a hard brow, the chin cut square.
        a.shape(pl(rr, [[-3.4, 2], [-3.8, -3.6], [-3, -6.6], [1.2, -7.2], [3.8, -6.2], [4.8, -3.6], [5.2, -1.2], [3.6, -.3], [3.2, 2]]), STEEL, {
          shade: poly(-5, -1, 6, -1.6, 6, 3, -5, 3), deep: poly(-5, .6, 6, .4, 6, 3, -5, 3), lines: [run([-3.6, -2.4], [.2, -1.8], [.8, 1.6]), run([-.6, -7], [-.4, -4])],
        });
        a.flat(poly(.6, -5.2, 4.8, -4.2, 5.1, -2.8, .8, -3.2), '#0a0d10');
        a.line(run([.1, -5.6], [2.6, -5.8], [4.8, -4.6]), { w: 2.6 });
        rust(a, rr, -2, -.6, 1, .8, RUST, RUSTD);
        a.line(run([2.4, -1.4], [3.6, -.8], [3.2, .8]), { w: 1 });
      }),
      ...lampsOf(-4, -4, 6, 3, poly(.1, -1.2, 3.9, -.2, 4.2, .9, .3, .3), poly(1.2, -.6, 3.6, 0, 3.8, .5, 1.4, 0)),
    },
    rig: [
      { id: 'foreF', part: 'foreFar', parent: 'armF', x: 0, y: 11, shiver: .1 },
      { id: 'armF', part: 'armFar', parent: 'body', x: -.4, y: -15, drift: .03, shiver: .05 },
      { id: 'thighF', part: 'thighFar', ik: { hip: [-1, 2], foot: 'footF', L: [11, 10], ankle: [0, -2.6], bend: 1 } },
      { id: 'shinF', part: 'shinFar', ikLower: 'thighF' },
      { id: 'footF', part: 'footFar', parent: null, x: -4, y: 0 },
      { id: 'thighN', part: 'thigh', ik: { hip: [1.2, 2], foot: 'footN', L: [11, 10], ankle: [0, -2.6], bend: 1 } },
      { id: 'shinN', part: 'shin', ikLower: 'thighN', seam: 'shin_seam' },
      { id: 'footN', part: 'foot', parent: null, x: 4, y: 0 },
      { id: 'hips', part: 'hips', parent: 'body', x: 0, y: 0 },
      { id: 'collar', part: 'collar', parent: 'body', x: 0, y: -18 },
      { id: 'body', part: 'torso', parent: null, x: 0, y: -23, seam: 'torso_seam' },
      { id: 'core', core: true, parent: 'body', x: 4.4, y: -11.2 },
      { id: 'foreN', part: 'fore', parent: 'armN', x: 0, y: 11, seam: 'fore_seam', shiver: .12 },
      { id: 'armN', part: 'arm', parent: 'body', x: -1.6, y: -14.5, drift: .03, shiver: .05 },
      { id: 'head', part: 'head', parent: 'body', x: 2.2, y: -18 },
      { id: 'lamp', lamp: true, parent: 'head', x: 0, y: -3.4 },
    ],
    middle: 'body', streakY: -21,
    keys: {
      stand: { body: { x: 0, y: -23, a: .03 }, head: { a: .04 }, armN: { a: -.04 }, foreN: { a: -.2 }, armF: { a: .2 }, foreF: { a: -.26 }, footN: { x: 3.8, y: 0, a: 0 }, footF: { x: -3.4, y: 0, a: 0 }, hips: { a: -.03 } },
      fold: { body: { x: 0, y: -21.6, a: 0 }, head: { x: 2, y: -16.8, a: .2 }, armN: { x: -.6, a: .08 }, foreN: { a: -3.04 }, armF: { a: .12 }, foreF: { a: -3.04 }, footN: { x: 2, y: 0, a: 0 }, footF: { x: -2.2, y: 0, a: 0 } },
      crouch: { body: { x: -3, y: -14.4, a: .4 }, head: { a: -.34 }, armN: { a: 1.05 }, foreN: { a: -.26 }, armF: { a: 1.3 }, foreF: { a: -.34 }, footN: { x: 5, y: 0, a: 0 }, footF: { x: -8, y: 0, a: 0 }, hips: { a: -.3 } },
      fly: { body: { x: 3, y: -21, a: 1.32 }, head: { a: -.55 }, armN: { a: -2.95 }, foreN: { a: -.06 }, armF: { a: -3.2 }, foreF: { a: -.04 }, footN: { x: -21, y: -15, a: 1.25 }, footF: { x: -25, y: -21, a: 1.4 }, hips: { a: -.1 } },
      stuck: { body: { x: -2, y: -12.5, a: .95 }, head: { a: -.5 }, armN: { a: -.9 }, foreN: { a: -1.08 }, armF: { a: -.62 }, foreF: { a: -1.33 }, footN: { x: -12, y: -3.2, a: .5 }, footF: { x: -16, y: -3.2, a: .5 }, hips: { a: -.4 } },
      dead: { body: { x: -5, y: -8.8, a: 1.55 }, head: { x: 2.6, y: -18.2, a: .5 }, armN: { x: -2.4, y: -13, a: -2.65 }, foreN: { a: -.4 }, armF: { a: -2.55 }, foreF: { a: -.35 }, footN: { x: -24, y: -6, a: 1.45 }, footF: { x: -21, y: -6, a: 1.5 }, hips: { a: -.1 } },
    },
  };
}

const MARGIN = 1.5;
const num = (v) => (Number.isInteger(v) ? String(v) : `${+v.toFixed(3)}f`);
const dbl = (v) => String(+v.toFixed(3));
/** A part with whole bounds and a whole pivot, as a picture is whole pixels: its drawing keeps its place. */
const snapped = (q) => { const px = Math.ceil(q.px), py = Math.ceil(q.py); return { ...q, px, py, w: Math.ceil(q.w + px - q.px), h: Math.ceil(q.h + py - q.py) }; };
/** A part that shines, as it is: plain paths, no water. */
function plain(q) {
  const cv = Anime.canvas((q.w + MARGIN * 2) * 4, (q.h + MARGIN * 2) * 4), c = cv.getContext('2d');
  c.setTransform(4, 0, 0, 4, (q.px + MARGIN) * 4, (q.py + MARGIN) * 4);
  c.lineCap = c.lineJoin = 'round';
  q.draw(c);
  return cv;
}
const partsOf = (parts, I) => Object.entries(parts).map(([name, q]) => `${I}["${name}"] = new(${q.w + MARGIN * 2}, ${q.h + MARGIN * 2}, ${num(q.px + MARGIN)}, ${num(q.py + MARGIN)}, []),`).join('\n');
function bakeAll(id, parts) {
  for (const [name, q] of Object.entries(parts)) out(`creatures/${id}/${name}`, q.light ? plain(q) : Anime.bake({ w: q.w, h: q.h, margin: MARGIN, shift: [q.px, q.py], draw: q.draw }));
}
const pair = (p) => `(${dbl(p[0])}, ${dbl(p[1])})`;

const ORDER = ['fold', 'crouch', 'fly', 'stuck', 'dead'];

Subjects.warden = function () {
  const d = candidateA(), I = '        ', index = (id) => d.rig.findIndex((n) => n.id === id);
  const parts = {};
  for (const [name, q] of Object.entries(d.parts)) parts[name] = snapped(q);
  bakeAll('inkIstukan', parts);
  const keyRow = (n) => {
    const st = d.keys.stand[n.id] || {}, base = { x: st.x !== undefined ? st.x : n.x || 0, y: st.y !== undefined ? st.y : n.y || 0, a: st.a || 0 };
    return [base, ...ORDER.map((name) => ({ ...base, ...(d.keys[name][n.id] || {}) }))].map((k) => `${dbl(k.x)}, ${dbl(k.y)}, ${dbl(k.a)}`).join(', ');
  };
  const row = (n) => {
    const kind = n.ik ? 'Upper' : n.ikLower ? 'Lower' : n.lamp ? 'Lamp' : n.core ? 'Core' : 'Plain';
    const ik = n.ik ? `, Ik: new(${dbl(n.ik.hip[0])}, ${dbl(n.ik.hip[1])}, ${index(n.ik.foot)}, ${dbl(n.ik.L[0])}, ${dbl(n.ik.L[1])}, ${dbl(n.ik.ankle[0])}, ${dbl(n.ik.ankle[1])}, ${dbl(n.ik.bend)})` : '';
    const more = [n.ikLower ? `Upper: ${index(n.ikLower)}` : '', n.seam ? `Seam: "${n.seam}"` : '', n.drift ? `Drift: ${dbl(n.drift)}` : '', n.shiver ? `Shiver: ${dbl(n.shiver)}` : ''].filter(Boolean).map((s) => ', ' + s).join('');
    return `${I}    new("${n.part || ''}", ${n.parent ? index(n.parent) : -1}, Part.${kind}, [${keyRow(n)}]${ik}${more}),`.replace('Part.', 'IstukanNode.Kinds.');
  };
  writeText('../../Content/logic/IstukanInk.cs', `// Generated by tools/anime-art/subjects/warden.js: export again, never edit.
using System.Collections.Generic;

namespace Stillwake.Game;

public static partial class CreatureArt
{
    /// <summary>The Istukan drawn in ink and watercolour, its parts in <c>art/hd/creatures/inkIstukan</c>: «Латник», a dry plated guard.</summary>
    public static readonly IstukanDesign InkIstukan = new("inkIstukan",
        new Dictionary<string, Part>
        {
${partsOf(parts, I + '    ')}
        },
        Body: ${index('body')},
        Nodes:
        [
${d.rig.map(row).join('\n')}
        ]);
}
`);
};
})();

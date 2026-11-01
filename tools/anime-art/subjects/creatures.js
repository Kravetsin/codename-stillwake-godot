// The creatures, picked on a page (their ADR): each part placed by hand, posed in the game by game/features/creatures/logic/CreaturePose.cs. Coordinates are
// world pixels: the body's origin is the middle of its feet, facing right, y down; a part's origin is its pivot. Also writes the designs' numbers for the game.
(function () {
'use strict';
const { ell, poly, rng } = Anime;

const cr = (a, b, c, d, t) => .5 * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (3 * b - a - 3 * c + d) * t * t * t);
/** A smooth run through the points, about `step` pixels a step; a point with 'c' as its last item is a hard corner. Keeps every number of a point. */
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
/** A closed outline through the points, moved along its normal a little, so the edge of the placed thing is hide and not a curve. */
function hide(pts, r, amp) {
  const s = spline(pts, true), n = s.length, out = [];
  s.forEach((p, i) => {
    const a = s[(i + n - 1) % n], b = s[(i + 1) % n], dx = b[0] - a[0], dy = b[1] - a[1], l = Math.hypot(dx, dy) || 1, k = r ? (r() - .5) * (amp === undefined ? .4 : amp) : 0;
    out.push(p[0] - (dy / l) * k, p[1] + (dx / l) * k);
  });
  return poly(out);
}
/** A limb along [x, y, half width] points, its ends left open flat. */
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
const mul = (hex, k) => '#' + [1, 3, 5].map((i) => Math.round(parseInt(hex.substr(i, 2), 16) * k).toString(16).padStart(2, '0')).join('');
const mat = (base, shade, deep, lite, ink) => ({ base, shade, deep, lite, spec: lite, ink: ink || '#1a120d' });
const farOf = (m) => mat(mul(m.base, .7), mul(m.shade, .72), mul(m.deep, .75), mul(m.base, .82), m.ink);
/** A part that grows out of what it hangs from: painted with no line round it, its sides penned, so no box shows where it joins. */
function grown(a, d, m, o, sides, w) {
  a.shape(d, m, { ...o, outer: false, edge: false });
  for (const side of sides) a.line(run(...side), { w: w || 2.3 });
}

const HOOF = mat('#322824', '#221b18', '#171210', '#4a3d36');
const INNER = mat('#6a4842', '#4c322f', '#382422', '#7f5b53');
const DARK = mat('#3c2d27', '#2b201c', '#1e1613', '#54423a');
const EYE = mat('#a39a7c', '#6f6853', '#4c4739', '#b5ac8e');
const PIT = '#14100e';

/** An almond in the eye's place, pointed at both ends, flatter above than a circle, so it never reads as a round cartoon eye. */
const almond = (rx, ry) => `M${-rx} ${ry * .25}Q${-rx * .35} ${-ry} ${rx * .2} ${-ry * .85}Q${rx * .8} ${-ry * .55} ${rx} ${ry * .05}Q${rx * .3} ${ry} ${-rx * .35} ${ry * .75}Q${-rx * .8} ${ry * .55} ${-rx} ${ry * .25}Z`;

/** The six pictures of an eye: its whites calm and wide, its pupils, its half and shut lids. A flat pupil: a goat's, not a pet's. No shine at all. */
function eyeParts(rx, ry, lid, o) {
  const W = o && o.wide || 1.3, T = o && o.tall || 1.35, R = Math.ceil(rx * W + 2.5), S = R * 2;
  const pr = rx * .55, pq = ry * .5;
  const white = (x, y) => (a) => { a.shape(almond(x, y), EYE, { outer: false, edge: true, shade: poly(-x - 1, -y - 1, x + 1, -y - 1, x + 1, -y * .25, -x - 1, -y * .1) }); };
  return {
    eye_open: { w: S, h: S, px: R, py: R, draw: white(rx, ry) },
    eye_wide: { w: S, h: S, px: R, py: R, draw: white(rx * W, ry * T) },
    pupil: { w: S, h: S, px: R, py: R, draw: (a) => { a.flat(`M${-pr} 0Q0 ${-pq * 1.1} ${pr} 0Q0 ${pq * 1.1} ${-pr} 0Z`, PIT); } },
    pupil_wide: { w: S, h: S, px: R, py: R, draw: (a) => { a.flat(ell(0, 0, pr * .5, pq * .75), PIT); } },
    lid_half: { w: S, h: S, px: R, py: R, draw: (a) => {
      a.flat(`M${-rx - .6} ${-ry - .7}L${rx + .6} ${-ry - .7}L${rx + .6} ${ry * .1}Q0 ${-ry * .05} ${-rx - .6} ${ry * .4}Z`, lid.base);
      a.line(`M${-rx - .4} ${ry * .4}Q0 ${-ry * .05} ${rx + .4} ${ry * .1}`, { w: 1.6 });
    } },
    lid_blink: { w: S, h: S, px: R, py: R, draw: (a) => {
      a.flat(`M${-rx - .7} ${-ry - .7}L${rx + .7} ${-ry - .7}L${rx + .7} ${ry + .6}L${-rx - .7} ${ry + .6}Z`, lid.base);
      a.line(`M${-rx - .4} ${ry * .35}Q0 ${ry * .8} ${rx + .4} ${ry * .05}`, { w: 2 });
    } },
  };
}

/* ---------- The loper: gaunt, wary, hungry ---------- */

function loper() {
  const HIDE = mat('#8b9198', '#646b72', '#454a51', '#a4a9af');
  const MANE = mat('#8e4630', '#663222', '#472318', '#a65a40');
  const THROAT = mat('#bdb49c', '#8f8875', '#686254', '#cec6af');
  const SHIN = '#3b3f46';
  const FAR = farOf(HIDE);
  const r = () => rng(0xb1);
  const leg = (m) => (a) => {
    const rr = r();
    grown(a, hide([[-1.9, -2.8], [0, -3.6], [1.9, -2.8], [1.8, 1.6], [.95, 5], [1.25, 6.5], [.75, 8], [.6, 11.6], [.95, 12.8], [-.75, 12.8], [-.6, 11.6], [-.75, 8], [-1.35, 6.5], [-1.05, 5], [-1.7, 1.6]], rr, .1), m, {
      shade: poly(.25, -4, 3, -4, 3, 13, .05, 13),
      lines: [run([-1, 6], [.2, 6.9], [1.1, 6.2])],
    }, [[[-1.8, .4], [-1.05, 5], [-1.35, 6.5], [-.75, 8], [-.6, 11.6], [-.75, 12.8]], [[1.8, .4], [.95, 5], [1.25, 6.5], [.75, 8], [.6, 11.6], [.95, 12.8]]], 2);
    // Dark from the knee down, like a stocking worn through.
    a.flat(limb([[0, 7.1, 1], [0, 8, .75], [0, 11.6, .6], [0, 12.8, .85]]), m === FAR ? mul(SHIN, .75) : SHIN);
    a.line(run([-1.1, 7], [-.75, 8], [-.6, 11.6], [-.75, 12.8]), { w: 2 });
    a.line(run([1.1, 7], [.75, 8], [.6, 11.6], [.95, 12.8]), { w: 2 });
    a.shape('M-.9 12.3L1 12.2L2.2 13.2L2.3 14L-1.1 14L-1.2 13.1Z', HOOF, { edge: false, shade: poly(-2, 13.3, 3, 13.3, 3, 15, -2, 15) });
  };
  const ear = (m, inner) => (a) => {
    grown(a, hide([[-1.1, 1.2], [-1.5, -2.4], [-1.2, -4.8], [-.4, -7.4, 'c'], [.8, -4.8], [1.3, -2.4], [1, 1.2]], rng(0xb2), .06), m, { shade: poly(.2, 2, 2, 2, 2, -6, .1, -6), deep: poly(-2, -5.4, 2, -4.9, 2, -9, -2, -9) }, [[[-1.2, .4], [-1.5, -2.4], [-1.2, -4.8], [-.4, -7.4]], [[-.4, -7.4], [.8, -4.8], [1.3, -2.4], [1, .4]]], 1.8);
    if (inner) a.flat('M-.6 -.3Q-.7 -2.6 -.4 -4.4Q.5 -2.6 .5 -.3Z', INNER.base);
  };
  return {
    eye: { travel: [.25, .15], travelWide: [.3, .2] },
    pose: { earUp: 0, earBack: -.5, earPerk: -.6, legAmp: .32, lift: 2, tailAmp: .12, tailFlee: -.5, graze: 1.2, legLen: 14, footW: 2 },

    parts: {
      body: { w: 32, h: 20, px: 16, py: 28, draw(a) {
        const rr = r();
        a.shape(hide([[-13.6, -18], [-13.4, -22.6], [-11.8, -24.4], [-10, -26.8, 'c'], [-7.6, -24.8], [-3, -23.2], [2, -23.2], [6.4, -25.2], [9.4, -25.6], [12.4, -22.6], [13, -18.5], [11.4, -14.6], [8.4, -12.2], [3.6, -13], [-1, -15], [-5.4, -17.6], [-8, -15.2], [-10.4, -12.4], [-12.8, -13.6]], rr, .25), HIDE, {
          shade: poly(-14, -18.4, -9, -18.6, -6.4, -20.2, -2, -18.8, 3, -16.8, 8, -16.6, 13, -19.4, 14, -10, -14, -10),
          deep: [poly(-6, -21, -2.8, -19.4, -.6, -17.2, -6, -15, -8.4, -18.4), poly(1, -14.6, 6, -13.8, 10, -14, 10, -10, 1, -10)],
          lite: [poly(-11.6, -25.6, -9.4, -26, -7.4, -23.4, -9.2, -22.4), poly(4.6, -24.4, 8.6, -24.6, 10.4, -21.2, 8.8, -18.6, 6.8, -21.6)],
          lines: [
            run([-12.8, -21.6], [-10.6, -21.4], [-8.2, -22.6]), run([-11.4, -16], [-10.2, -19], [-8.2, -19.6]),
            run([6.2, -19], [6.8, -22.6], [8.6, -24.8]), run([9.4, -17.8], [10.6, -20.4]),
          ],
        });
        // Pale bone from the brisket along the tucked belly.
        a.shape(hide([[12.2, -20], [12.7, -18.5], [11.2, -14.8], [8.4, -12.5], [3.6, -13.3], [-1, -15.3], [-4.8, -17.6], [-2.6, -18.2], [1, -16.6], [5, -15.8], [9, -16.8], [11, -19]], rr, .15), THROAT, { outer: false, edge: false, shade: poly(-5, -15.6, 2, -14.4, 8, -14, 13, -16.6, 13, -11, -5, -11) });
        const rib = (x0, y0, x1, y1, x2, y2) => {
          a.flat(limb([[x0, y0, .15], [x1, y1, .6], [x2, y2, .12]]), HIDE.deep);
          a.line(run([x0 - .5, y0], [x1 - .6, y1], [x2 - .5, y2]), { w: 1.4 });
        };
        rib(-3.2, -22.6, -2.7, -20, -1.6, -17.2);
        rib(-.4, -22.4, .2, -19.6, 1.2, -15.6);
        rib(2.4, -22.4, 3, -19.2, 4, -14.8);
        rib(5, -22.8, 5.4, -19, 6.2, -14.6);
        // The mane runs along the spine, on every one of them: the marks are what differs.
        a.flat(limb([[-10, -26, .4], [-7, -23.9, 1.15], [-2.5, -22.4, 1], [2, -22.4, 1.05], [6.5, -24.3, 1.2], [9.2, -25, .4]]), MANE.base);
      } },
      marks: { w: 32, h: 20, px: 16, py: 28, draw(a) {
        // What one wears and the next does not: a dark hip, a rust scuff on the ribs, one on the shoulder, each apart from the rest.
        a.flat(limb([[-13.2, -20, .15], [-12, -19, .7], [-10.8, -17.6, .15]]), DARK.shade);
        a.flat(poly(-1.6, -20.2, -.2, -20.8, 1.2, -19.8, 1.8, -18.2, .6, -17.6, -.6, -18.6), MANE.shade);
        a.flat(poly(6.8, -21.2, 8.6, -21.6, 9.8, -20, 9.4, -18.6, 8, -19, 7, -19.8), MANE.shade);
      } },
      // The neck and the skull as one: it swings down from the shoulder to graze.
      head: { w: 26, h: 18, px: 4, py: 14, draw(a) {
        const rr = r();
        // Carried low and forward: a wedge of a skull, the bone of the brow and cheek showing, the mouth a straight hard line.
        const top = [[-2.4, -1.2], [1.2, -4.6], [4.4, -7.6], [6.8, -9.2], [9, -9.4], [12, -7.6], [15.4, -5.6], [18.6, -3.6, 'c'], [19.2, -2]];
        const under = [[19.2, -1.7], [18.4, -.6, 'c'], [15.4, -1], [12.4, -2], [9.8, -3.4], [7.8, -3.8], [5.8, -2.2], [4.2, 0]];
        a.shape(hide([[-2.4, 1.8], ...top, ...under, [3.2, 2.4]], rr, .1), HIDE, {
          outer: false, edge: false,
          shade: [poly(3.6, 3, 2, -1.4, 4.2, -4.6, 7.2, -5.6, 10.6, -5.2, 14, -3.6, 20, -2.6, 20, 0, 4, 4)],
          deep: [poly(5, -2.4, 7.8, -4.2, 10.4, -3.6, 7, -1, 5, 2), poly(9.2, -5.2, 12.2, -4.6, 13, -2.8, 10.2, -3.4)],
          lite: poly(4.6, -6.8, 6.8, -8.6, 8.8, -8.8, 8.8, -8, 6.4, -6.6),
          lines: [run([1.2, -2.6], [2.6, -4.4]), run([18.8, -1.8], [15.6, -2], [13, -2.6])],
        });
        // The bone of the throat under the jaw and down the neck; the mane going on up the neck.
        a.flat(hide([[3.3, 2], [4.5, -.2], [5.9, -2.3], [7.8, -3.9], [9.8, -3.5], [12.4, -2.1], [15.4, -1.1], [18, -.9], [15, -2.4], [11, -4], [8, -5], [5.2, -4], [3, -.8]], rr, .1), THROAT.base);
        a.flat(limb([[1.4, -4.1, .25], [3, -5.7, .55], [4.8, -7.2, .5], [6.8, -8.6, .2]]), MANE.base);
        a.line(run(...top.slice(1)), { w: 2.4 });
        a.line(run(...under), { w: 2.4 });
        a.flat(poly(8, -7.8, 10.4, -7.8, 12.2, -6.2, 11.8, -5.4, 10, -6.6, 8.4, -6.4), HIDE.deep);
        a.line(run([7.8, -7.6], [10.2, -8.2], [12.4, -6]), { w: 2.2 });
        a.flat(ell(18, -2.8, .36, .26), PIT);
      } },
      ear: { w: 6, h: 14, px: 3, py: 12, draw: ear(HIDE, true) },
      earFar: { w: 6, h: 14, px: 3, py: 12, draw: ear(FAR, false) },
      leg: { w: 8, h: 18, px: 3, py: 3.6, draw: leg(HIDE) },
      legFar: { w: 8, h: 18, px: 3, py: 3.6, draw: leg(FAR) },
      tail: { w: 9, h: 14, px: 6, py: 2, draw(a) {
        a.flat(limb([[.6, -.6, .75], [-1.2, 3, .5], [-2.2, 6.4, .35], [-2.4, 8, .3]]), HIDE.shade);
        a.line(run([.1, -.2], [-1.6, 3], [-2.6, 6.4]), { w: 1.6 });
        a.shape(hide([[-3.1, 6.8], [-1.7, 6.8], [-1.6, 8.6], [-2.5, 10.6, 'c'], [-3.2, 8.6]], rng(0xb3), .1), MANE, { shade: poly(-2.5, 6, 0, 6, 0, 12, -2.5, 12) });
      } },
      ...eyeParts(1, .55, HIDE, { wide: 1.25, tall: 1.6 }),
    },
    rig: [
      { id: 'earFar', part: 'earFar', parent: 'head', x: 5.8, y: -8.4, drive: 'ear', base: -1 },
      { id: 'legFH', part: 'legFar', parent: 'body', x: -6.5, y: -14, drive: 'leg', phase: 0 },
      { id: 'legFF', part: 'legFar', parent: 'body', x: 9.5, y: -14, drive: 'leg', phase: .5 },
      { id: 'tail', part: 'tail', parent: 'body', x: -13, y: -22, drive: 'tail' },
      { id: 'body', part: 'body', parent: null, x: 0, y: 0, drive: 'body' },
      { id: 'marks', part: 'marks', parent: 'body', x: 0, y: 0 },
      { id: 'legNH', part: 'leg', parent: 'body', x: -9.5, y: -14, drive: 'leg', phase: .5 },
      { id: 'legNF', part: 'leg', parent: 'body', x: 7, y: -14, drive: 'leg', phase: 0 },
      { id: 'head', part: 'head', parent: 'body', x: 8, y: -21, drive: 'head' },
      { id: 'eye', part: 'eye', parent: 'head', x: 10.2, y: -6.1, drive: 'eye' },
      { id: 'ear', part: 'ear', parent: 'head', x: 7, y: -8.6, drive: 'ear', base: -.72 },
    ],
  };
}

/* ---------- The tapir: thick plated hide, low and heavy ---------- */

function tapir() {
  const HIDE = mat('#505a60', '#3a4247', '#282e32', '#677278');
  const PLATE = mat('#3d4128', '#2c2f1d', '#1e2014', '#555a39');
  const UNDER = mat('#88867a', '#65645a', '#494840', '#9b998c');
  const EDGE = '#bcaa6c', LICHEN = '#8d7e44';
  const FAR = farOf(HIDE);
  const LEG = mat(HIDE.shade, HIDE.deep, HIDE.deep, HIDE.base), LEGFAR = farOf(LEG);
  const SCAR = '#c4b478';
  const POD = mat('#2f8f88', '#1e6a66', '#144a48', '#6fdcc4');
  const r = () => rng(0xc1);
  const leg = (m) => (a) => {
    const rr = r();
    grown(a, hide([[-3.4, -3.2], [0, -4.4], [3.4, -3.2], [3.2, 0], [2.4, 4.4], [-2.2, 4.4], [-3.1, 0]], rr, .15), m, { shade: poly(.7, -5, 4, -5, 4, 5, 1, 5), lines: [run([-2.6, 1.4], [0, 2], [2.6, 1.2])] }, [[[-3.3, -1.4], [-3.1, 0], [-2.2, 4.4]], [[3.3, -1.4], [3.2, 0], [2.4, 4.4]]]);
    a.shape('M-2.3 4.5L2.4 4.4L3.4 5.5L3.6 6.5L-2.5 6.5L-2.6 5.6Z', HOOF, { edge: false, shade: poly(-3, 5.9, 5, 5.9, 5, 7, -3, 7) });
  };
  const ear = (m) => (a) => {
    // Short and stiff, a bite out of it.
    grown(a, hide([[-1.2, 1.6], [-1.5, -1.2], [-1.2, -3, 'c'], [-.5, -3.2, 'c'], [-.7, -5.8, 'c'], [.5, -3.2], [1, -.8], [1, 1.6]], rng(0xc2), .04), m, { shade: poly(.1, 2, 2, 2, 2, -6, .1, -6) }, [[[-1.3, .8], [-1.5, -1.2], [-1.2, -3]], [[-1.2, -3], [-.5, -3.2], [-.7, -5.8]], [[-.7, -5.8], [.5, -3.2], [1, -.8], [1, .8]]], 1.8);
  };
  /** A plate of the hide: its own outer line, a hard shade along its foot, one crack. */
  const plate = (a, rr, pts, foot, lines) => a.shape(hide(pts, rr, .18), PLATE, { shade: foot, lines });
  return {
    eye: { travel: [.2, .12], travelWide: [.25, .15] },
    pose: { earUp: 0, earBack: -.75, earPerk: .1, legAmp: .46, lift: 1.1, tailAmp: .1, tailFlee: -.4, graze: .62, legLen: 6.5, footW: 3.2 },
    parts: {
      body: { w: 34, h: 30, px: 18, py: 29, draw(a) {
        const rr = r();
        a.shape(hide([[-16.4, -9], [-16.8, -14], [-14.6, -19], [-8, -22], [0, -25], [6, -25], [10.4, -22.6], [12.6, -17], [12.6, -11], [9, -6.4], [2, -5], [-6, -5], [-13, -5.4]], rr, .3), HIDE, {
          shade: poly(-18, -9.6, -8, -9, 2, -9.4, 8, -11.6, 14, -13, 14, -3, -18, -3),
          deep: poly(-14, -6.6, -4, -6.6, 4, -6.8, 10, -8, 10, -3, -14, -3),
          lines: [run([-15, -11], [-12, -8.2]), run([3, -9], [5.6, -7]), run([-5, -8.2], [-3, -6.4]), run([11, -12.4], [9, -9.4])],
        });
        // The paler underside, between the legs.
        a.shape(hide([[-15, -8.6], [-6, -7.8], [2, -8], [8, -9.8], [11, -11.6], [9, -6.9], [2, -5.4], [-6, -5.4], [-13, -5.8]], rr, .2), UNDER, { outer: false, edge: false, shade: poly(-16, -6.6, 12, -7.4, 12, -4, -16, -4) });
        // Rear to front, each over the last: the ridge of the back steps up to the shoulder.
        plate(a, rr, [[-17, -13.4, 'c'], [-15.6, -19.2], [-11.6, -22, 'c'], [-9.2, -17.4], [-10, -12.2, 'c'], [-14, -10.6]], poly(-18, -14, -9, -15, -9, -9, -18, -9), [run([-14.6, -17.4], [-13.2, -15.6], [-13.8, -13.8])]);
        plate(a, rr, [[-12.4, -21], [-7.6, -24.8, 'c'], [-4, -19.8], [-4.2, -14.2], [-8.2, -11.4, 'c'], [-11, -14.4]], poly(-13, -16, -3, -17, -3, -10, -13, -10), [run([-7.8, -20.6], [-8.6, -17.4])]);
        plate(a, rr, [[-7.2, -23.6], [-1.4, -27.8, 'c'], [2, -21.6], [1.4, -15.4], [-2.6, -12.4, 'c'], [-5.6, -15.8]], poly(-7, -17.6, 3, -18.6, 3, -11, -7, -11), [run([-3, -22.4], [-1.8, -19.6], [-2.8, -17.4])]);
        plate(a, rr, [[-1.2, -26.6], [5, -29, 'c'], [9, -23.4], [8.8, -17], [4.6, -12.8, 'c'], [.8, -15.6]], poly(0, -18.8, 10, -19.8, 10, -12, 0, -12), [run([3.6, -23.4], [5, -20.6])]);
        plate(a, rr, [[4.8, -27.4], [9.8, -27, 'c'], [13.2, -21.6], [13.4, -15.6], [10.4, -11.6, 'c'], [7.8, -14.8], [7.8, -21.6]], poly(7, -18, 14, -18.6, 14, -11, 7, -11), [run([10.4, -22.4], [11.2, -19.6])]);
        // Each plate's top and fore edge worn to old bone.
        for (const edge of [
          [[-15.6, -19.2], [-11.6, -22]], [[-12.4, -21], [-7.6, -24.8]], [[-7.2, -23.6], [-1.4, -27.8]], [[-1.2, -26.6], [5, -29]], [[5.4, -28], [9.8, -27]],
          [[-11.4, -21.6], [-9.2, -17.4], [-10, -12.6]], [[-7.4, -24.4], [-4, -19.8], [-4.2, -14.6]], [[-1.2, -27.2], [2, -21.6], [1.4, -15.8]], [[5.2, -28.4], [9, -23.4], [8.8, -17.2]],
        ]) a.line(run(...edge), { c: EDGE, w: 1.7 });
        // Scars: pale where they cut, each a mended blow.
        a.line(run([-8, -19.6], [-6, -15.4]), { c: SCAR, w: 2.2 });
        a.line(run([-7.2, -18.6], [-5.4, -19.2]), { c: SCAR, w: 1.2 });
        a.line(run([1.8, -24.4], [4.2, -18.4], [3.6, -15.6]), { c: SCAR, w: 2.4 });
        a.line(run([-14.2, -13.4], [-11.6, -15.6]), { c: SCAR, w: 1.8 });
      } },
      marks: { w: 34, h: 30, px: 18, py: 29, draw(a) {
        // Lichen on three plates, in dull ochre patches.
        a.flat(poly(-15.6, -15.2, -14.6, -16.6, -13.2, -16.2, -12.4, -14.8, -13, -14.2, -13.8, -14.6, -14.6, -13.6, -15.4, -14), LICHEN);
        a.flat(poly(-12, -17, -11, -17.4, -10.6, -16.4, -11.6, -16.2), LICHEN);
        a.flat(poly(-9.6, -20.4, -8.6, -21.6, -7.2, -21.2, -6.2, -19.8, -6.8, -19, -7.8, -19.6, -8.6, -18.6, -9.4, -19), LICHEN);
        a.flat(poly(-7.4, -18, -6.4, -18.2, -6, -17.2, -7, -17), LICHEN);
        a.flat(poly(2.8, -24.8, 4, -25.8, 5.6, -25.4, 6.8, -23.6, 6.2, -22.6, 5.2, -23.2, 4.6, -21.8, 3.6, -22, 3.2, -23.4), LICHEN);
        a.flat(poly(5.6, -21.4, 6.6, -21.6, 6.8, -20.6, 5.8, -20.4), LICHEN);
        // Dark where the old plates have grown over each other.
        a.flat(poly(-9.6, -16.6, -9.2, -13.2, -10.6, -14.2), DARK.base);
        a.flat(poly(-13.6, -19.6, -12, -20.6, -12.4, -18.4), DARK.base);
        a.flat(poly(-4.6, -21.2, -4, -17.4, -5.6, -18.6, -6.2, -22), DARK.base);
        a.flat(poly(1.6, -24, 2, -19.4, .7, -20.4, .2, -25), DARK.base);
        a.flat(poly(8.4, -25.8, 8.4, -21.6, 7.4, -23), DARK.base);
      } },
      head: { w: 21, h: 19, px: 6, py: 9, draw(a) {
        const rr = r();
        const top = [[-4.6, -2.8], [-1, -5.2], [3, -5, 'c'], [6.4, -2.2], [9, .8], [11.2, 4], [12.4, 6.8, 'c']];
        const under = [[12.4, 7], [11.6, 8.4, 'c'], [8.6, 8.2], [5.6, 6.6], [2, 6], [-2.4, 6.6]];
        a.shape(hide([[-5, 3], ...top, ...under], rr, .12), HIDE, {
          outer: false, edge: false,
          shade: poly(-6, 3.6, -2, 2.6, 3, 2, 6.4, 3, 9.4, 4.8, 14, 5.4, 14, 10, -6, 10),
          deep: poly(1.6, 3.8, 5.6, 4.2, 8.4, 6.2, 8.4, 9.6, 1.6, 9.6),
          lines: [run([12, 6.4], [9.2, 5.8], [6.4, 4.8]), run([-3, 1], [-1, 3.6]), run([.4, -1.6], [1.6, .4])],
        });
        a.flat(hide([[-2, 6.2], [2, 5.6], [5.6, 6.2], [8.6, 7.7], [11.4, 8], [11.8, 7], [9, 6], [6, 4.8], [2, 4.4], [-2, 4.8]], rr, .1), UNDER.base);
        a.line(run(...top), { w: 2.8 });
        a.line(run(...under), { w: 2.8 });
        // The brow plate, thick, the eye under it in shadow.
        a.shape(hide([[-2.8, -5.6], [3.2, -6.4, 'c'], [7.4, -3.2], [9.4, -.4, 'c'], [6, -1.6], [2.2, -2.8], [-2, -2.4]], rr, .08), PLATE, { shade: poly(-3, -3.6, 10, -2.6, 10, 0, -3, 0), lines: [run([3.4, -5], [4.2, -3.4])] });
        a.flat(poly(4.6, -1.4, 7.8, -.6, 8.2, .4, 5.6, .2), HIDE.deep);
        a.flat(ell(11.2, 5.6, .5, .36), PIT);
        a.line(run([3.2, 1.2], [5.6, 2.8]), { c: SCAR, w: 1.8 });
      } },
      // Hung under its jaw on a dry stalk: the one thing about it that works, so the one bright thing.
      pod: { w: 8, h: 9, px: 4, py: 1, glow: [[-.2, 4.4, 1.5]], draw(a) {
        a.flat(limb([[0, -.6, .4], [.4, 1.2, .3], [-.2, 2.8, .35]]), DARK.base);
        a.line(run([-.3, -.6], [.1, 1.2], [-.5, 2.8]), { w: 1.3 });
        a.shape(hide([[-1.7, 3.8], [-.2, 2.7], [1.3, 3.7], [1.5, 5.2], [-.2, 6.4, 'c'], [-1.8, 5.2]], rng(0xc4), .08), POD, { shade: poly(.2, 2, 3, 2, 3, 7, .2, 7) });
      } },
      ear: { w: 7, h: 9, px: 4, py: 7, draw: ear(HIDE) },
      earFar: { w: 7, h: 9, px: 4, py: 7, draw: ear(FAR) },
      leg: { w: 10, h: 11, px: 5, py: 4.5, draw: leg(LEG) },
      legFar: { w: 10, h: 11, px: 5, py: 4.5, draw: leg(LEGFAR) },
      tail: { w: 9, h: 10, px: 6, py: 2, draw(a) {
        a.shape(hide([[1.4, -1.6], [-1.6, -1.6], [-3.6, 1.6], [-4.6, 5.6, 'c'], [-2.2, 4.6], [-.6, 2.8], [1.4, 1.8]], rng(0xc3), .2), HIDE, { shade: poly(-5, 2.4, 2, 1, 2, 7, -5, 7), lines: [run([-3.2, 2.2], [-.8, 2.6]), run([-1.8, .2], [.2, .6])] });
      } },
      ...eyeParts(.95, .45, HIDE, { wide: 1.15, tall: 1.5 }),
    },
    rig: [
      { id: 'earFar', part: 'earFar', parent: 'head', x: .2, y: -4.8, drive: 'ear', base: -.75 },
      { id: 'legFH', part: 'legFar', parent: 'body', x: -7.5, y: -6.5, drive: 'leg', phase: 0 },
      { id: 'legFF', part: 'legFar', parent: 'body', x: 8.5, y: -6.5, drive: 'leg', phase: .5 },
      { id: 'tail', part: 'tail', parent: 'body', x: -15.6, y: -14, drive: 'tail' },
      { id: 'body', part: 'body', parent: null, x: 0, y: 0, drive: 'body' },
      { id: 'marks', part: 'marks', parent: 'body', x: 0, y: 0 },
      { id: 'legNH', part: 'leg', parent: 'body', x: -11, y: -6.5, drive: 'leg', phase: .5 },
      { id: 'legNF', part: 'leg', parent: 'body', x: 5, y: -6.5, drive: 'leg', phase: 0 },
      { id: 'head', part: 'head', parent: 'body', x: 11, y: -15.6, drive: 'head' },
      { id: 'pod', part: 'pod', parent: 'head', x: 8, y: 7.2, drive: 'pod' },
      { id: 'eye', part: 'eye', parent: 'head', x: 6.4, y: -.1, drive: 'eye' },
      { id: 'ear', part: 'ear', parent: 'head', x: -1.4, y: -4.6, drive: 'ear', base: -.95 },
    ],
  };
}

/* ---------- The sixleg: alien, read through its ears and marks ---------- */

function sixleg() {
  const HIDE = mat('#3a2d44', '#2a2033', '#1c1624', '#4e4158');
  const SMOKE = mat('#77727f', '#57535f', '#3e3b45', '#8a8592');
  const BLOOD = '#823029', NIGHT = '#14131e';
  const FAR = farOf(HIDE);
  const PALE = '#c6bb9f';
  const r = () => rng(0xd1);
  const leg = (m) => (a) => {
    const rr = r();
    grown(a, hide([[-2, -2.8], [0, -3.6], [2, -2.8], [1.7, 1.4], [.8, 4.4], [.7, 8.6], [-.7, 8.6], [-.9, 4.4], [-1.7, 1.4]], rr, .1), m, { shade: poly(.2, -4, 3, -4, 3, 9, .05, 9) }, [[[-1.9, -.4], [-1.7, 1.4], [-.9, 4.4], [-.7, 8.6]], [[1.9, -.4], [1.7, 1.4], [.8, 4.4], [.7, 8.6]]], 2);
    // A splayed foot, two blunt toes: not a hoof, not a paw.
    a.shape('M-.9 8.2L.8 8.2L2.8 9.2L3.2 10L1 10L.1 9.6L-.5 10L-2.4 10L-1.8 9Z', HOOF, { edge: false });
  };
  /** A long blade swept back, one tear in its trailing edge, banded dark and tipped pale: what the eye reads instead of a face. */
  const ear = (m, bands) => (a) => {
    const back = [[-1.5, .4], [-2, -4], [-2.5, -7.4], [-1.5, -8.4, 'c'], [-2.9, -9.4, 'c'], [-3.1, -13], [-3, -17.4, 'c']], front = [[-3, -17.4], [-1.2, -12.8], [.1, -8], [.9, -3.5], [.9, .4]];
    grown(a, hide([[-1.4, 1.4], ...back, ...front.slice(1), [.9, 1.4]], rng(0xd2), .05), m, { shade: poly(-.5, 2, 3, 2, 3, -18, -2, -18) }, [back, front], 1.9);
    if (bands) {
      a.flat(poly(-2.4, -6.2, 1, -5.2, .8, -4, -2.2, -5), mul(PALE, .72));
      a.flat(poly(-3.1, -11.4, -.4, -10.6, -.8, -9.4, -3, -10.2), mul(PALE, .72));
    }
    a.flat(poly(-3.2, -14, -1.5, -13.4, -3, -17.2), bands ? BLOOD : mul(BLOOD, .75));
  };
  return {
    eye: { travel: [0, 0], travelWide: [0, 0] },
    pose: { earUp: 0, earBack: -.42, earPerk: -.65, legAmp: .38, lift: 1.7, tailAmp: .16, tailFlee: .5, graze: 1.02, legLen: 10, footW: 2.6 },
    parts: {
      body: { w: 32, h: 28, px: 16, py: 29, draw(a) {
        const rr = r();
        a.shape(hide([[-14.4, -13], [-14.2, -16.8], [-11, -19.8], [-6, -21.2], [-1.4, -23.4], [-.2, -25.2, 'c'], [1.4, -24.8], [2.6, -27, 'c'], [4.2, -26], [5.6, -28.2, 'c'], [7.4, -26.4], [9.8, -25.6], [13, -21.8], [13.2, -16.4], [10.6, -11.4], [6, -9.6], [0, -11], [-5, -11.4], [-8.6, -8.8], [-11.4, -7.8], [-13.6, -9.4]], rr, .2), HIDE, {
          shade: poly(-15, -13, -9, -14.4, -3, -15, 3, -14.6, 8, -15.4, 14, -18, 14, -6, -15, -6),
          deep: poly(-6, -13, -1, -12.8, 3, -11.6, 8, -11, 8, -6, -6, -6),
          lines: [run([-12.6, -13.4], [-10, -11.6], [-8.8, -9.4]), run([9.4, -16.4], [10.8, -19.6]), run([.2, -22.8], [.8, -21]), run([3, -24.6], [3.6, -22.6]), run([5.8, -25.8], [6.4, -23.6])],
        });
        // The smoky belly, up between the forelegs.
        a.shape(hide([[-12, -11], [-7, -11.8], [-4, -13.2], [1, -13.4], [7, -12.6], [11.6, -14.6], [10.4, -11.7], [6, -9.9], [0, -11.2], [-5, -11.6], [-8.6, -9.1], [-11.4, -8.1]], rr, .2), SMOKE, { outer: false, edge: false, shade: poly(-13, -9.6, -6, -10.2, 2, -11.4, 8, -11.6, 12, -12.6, 12, -7, -13, -7) });
        a.flat(limb([[-13, -17.4, .3], [-7, -20.6, .55], [-1, -23.4, .7], [3, -25.4, .8], [7, -25.8, .7], [9.8, -25.2, .2]]), NIGHT);
      } },
      marks: { w: 32, h: 28, px: 16, py: 29, draw(a) {
        // Pale bars torn off short at uneven lengths, each its own.
        a.flat(poly(6.6, -24.6, 8.2, -24.2, 7.4, -19.6, 6.6, -15.6, 5.4, -15.4, 6, -20), PALE);
        a.flat(poly(2.6, -23.8, 4, -24, 3.2, -20.4, 2.4, -17.8, 1.4, -18.2, 2, -21), PALE);
        a.flat(poly(-1.6, -22, -.4, -22.4, -.8, -18.4, -1.6, -14.6, -2.6, -14.8, -2.2, -18.6), PALE);
        a.flat(poly(-5.6, -20.2, -4.4, -20.6, -5, -17.4, -5.8, -16.6), PALE);
        a.flat(poly(-9.4, -18.8, -8.4, -19.2, -9, -15, -9.8, -14.6), PALE);
      } },
      head: { w: 26, h: 16, px: 5, py: 7, draw(a) {
        const rr = r();
        const top = [[-3.2, -2.2], [-.4, -4.4], [3.6, -4.8], [7.4, -4.2], [11.6, -2.2], [15.6, .2], [18.2, 1.9], [18.8, 3]], under = [[18.8, 3.2], [18.2, 4.2, 'c'], [16.2, 4.4], [12.4, 3.8], [8, 2.6], [4.4, 2.2], [2.4, 4.6]];
        a.shape(hide([[-3, 3], ...top, ...under, [.6, 6]], rr, .1), HIDE, {
          outer: false, edge: false,
          shade: poly(-4, 2.6, 2, .8, 7, .8, 12, 1.6, 20, 2.6, 20, 7, -4, 7),
          lines: [run([18.2, 3.4], [15, 3], [12, 2.3])],
        });
        a.flat(hide([[2.6, 4.2], [4.4, 1.9], [8, 2.3], [12.4, 3.5], [16.2, 4.1], [18, 3.9], [15, 2.5], [11, 1.3], [7, .7], [3, 1.1]], rr, .1), SMOKE.base);
        a.line(run(...top), { w: 2.4 });
        a.line(run(...under), { w: 2.4 });
        a.line(run([5, -3.4], [7.8, -3], [10.4, -1.8]), { w: 2 });
        // A pale blaze toward the nose, where a face would be.
        a.flat(limb([[10.6, -2.2, .2], [13.4, -.6, .5], [17, 1.5, .4], [18.2, 2.5, .15]]), PALE);
      } },
      ear: { w: 8, h: 21, px: 5, py: 19, draw: ear(HIDE, true) },
      earFar: { w: 8, h: 21, px: 5, py: 19, draw: ear(FAR, false) },
      leg: { w: 9, h: 15, px: 4, py: 4, draw: leg(HIDE) },
      legFar: { w: 9, h: 15, px: 4, py: 4, draw: leg(FAR) },
      tail: { w: 21, h: 12, px: 19, py: 6, draw(a) {
        a.flat(limb([[1, .2, .9], [-3.4, 1.6, .72], [-8, 2.4, .52], [-12.6, 1, .34], [-16.2, -1.8, .1]]), HIDE.base);
        a.line(run([1, -.7], [-3.4, .9], [-8, 1.9], [-12.6, .7], [-16.2, -1.8]), { w: 1.6 });
        a.line(run([.4, 1.1], [-3.4, 2.3], [-8, 2.9], [-12.2, 1.4]), { w: 1.6 });
        a.flat(poly(-6, 1.5, -4.8, 1.4, -5, 3, -6.2, 3), mul(PALE, .72));
        a.flat(poly(-10, 1.3, -9, 1.6, -9.2, 2.9, -10.2, 2.7), mul(PALE, .72));
        a.flat(limb([[-12.6, 1, .34], [-14.6, -.6, .24], [-16.2, -1.8, .1]]), NIGHT);
      } },
      // No white and no pupil: a pit that narrows and widens.
      eye_open: { w: 8, h: 6, px: 4, py: 3, draw: (a) => { a.flat('M-1.4 .25Q-.3 -.4 1.4 0Q.2 .4 -1.4 .25Z', '#120d0b'); } },
      eye_wide: { w: 8, h: 6, px: 4, py: 3, draw: (a) => { a.flat('M-1.8 .3Q-.6 -1 1.8 -.2Q.4 1 -1.8 .3Z', '#120d0b'); } },
      lid_half: { w: 8, h: 6, px: 4, py: 3, draw: (a) => { a.flat(poly(-2.2, -1, 2.2, -1, 2.2, .05, -2.2, .25), HIDE.base); a.line(run([-2, .3], [2, .05]), { w: 1.4 }); } },
      lid_blink: { w: 8, h: 6, px: 4, py: 3, draw: (a) => { a.flat(poly(-2.2, -1, 2.2, -1, 2.2, 1, -2.2, 1), HIDE.base); a.line(run([-2, .3], [2, .05]), { w: 1.8 }); } },
    },
    rig: [
      { id: 'earFar', part: 'earFar', parent: 'head', x: 1.2, y: -4.2, drive: 'ear', base: -.55 },
      { id: 'l4', part: 'legFar', parent: 'body', x: -6.5, y: -10, drive: 'leg', phase: .5 },
      { id: 'l5', part: 'legFar', parent: 'body', x: 1.5, y: -10.4, drive: 'leg', phase: 0 },
      { id: 'l6', part: 'legFar', parent: 'body', x: 9.5, y: -10, drive: 'leg', phase: .5 },
      { id: 'tail', part: 'tail', parent: 'body', x: -13.4, y: -14.6, drive: 'tail' },
      { id: 'body', part: 'body', parent: null, x: 0, y: 0, drive: 'body' },
      { id: 'marks', part: 'marks', parent: 'body', x: 0, y: 0 },
      { id: 'l1', part: 'leg', parent: 'body', x: -10, y: -10, drive: 'leg', phase: 0 },
      { id: 'l2', part: 'leg', parent: 'body', x: -1.8, y: -10.4, drive: 'leg', phase: .5 },
      { id: 'l3', part: 'leg', parent: 'body', x: 6.5, y: -10, drive: 'leg', phase: 0 },
      { id: 'head', part: 'head', parent: 'body', x: 10, y: -21, drive: 'head' },
      { id: 'eye', part: 'eye', parent: 'head', x: 7.8, y: -1.4, drive: 'eye' },
      { id: 'ear', part: 'ear', parent: 'head', x: -.4, y: -3.6, drive: 'ear', base: -.92 },
    ],
  };
}

/* ---------- Out ---------- */

/** Each design by the look a species names in sim/Content, its id its folder under art/hd/creatures. */
const DESIGNS = {
  classic: { id: 'inkLoper', gait: 'Walk', ...loper() },
  tapir: { id: 'inkTapir', gait: 'Walk', ...tapir() },
  sixleg: { id: 'inkSixleg', gait: 'Tripod', ...sixleg() },
};
const MARGIN = 1.5;
const up = (s) => s[0].toUpperCase() + s.slice(1);
const num = (v) => (Number.isInteger(v) ? String(v) : `${+v.toFixed(3)}f`);

/** A design as the game's record: a part's box and pivot take in the margin its picture is drawn with. */
/** What a part gives light from, for the game to add over it: a soft halo and a hard heart at each of its [x, y, r], in its picture's pixels. */
function glowOf(q) {
  const c = Anime.canvas((q.w + MARGIN * 2) * 4, (q.h + MARGIN * 2) * 4), x = c.getContext('2d');
  const at = ([gx, gy, r], k, fill) => { x.fillStyle = fill; x.beginPath(); x.arc((q.px + MARGIN + gx) * 4, (q.py + MARGIN + gy) * 4, r * 4 * k, 0, 7); x.fill(); };
  x.filter = 'blur(3px)';
  for (const g of q.glow) at(g, 1, 'rgba(150,235,215,.55)');
  x.filter = 'none';
  for (const g of q.glow) at(g, .55, '#d8fff2');
  return c;
}

function record(look, d) {
  const eye = d.rig.find((n) => n.drive === 'eye'), p = d.pose, I = '            ';
  const parts = Object.entries(d.parts).map(([name, q]) => `${I}    ["${name}"] = new(${q.w + MARGIN * 2}, ${q.h + MARGIN * 2}, ${num(q.px + MARGIN)}, ${num(q.py + MARGIN)}, []${q.glow ? ', Glow: true' : ''}),`);
  const rig = d.rig.map((n) => {
    const more = [n.phase ? `Phase: ${num(n.phase)}` : '', n.base ? `Base: ${num(n.base)}` : ''].filter(Boolean).map((s) => ', ' + s).join('');
    return `${I}    new("${n.id}", "${n.part}", ${n.parent ? `"${n.parent}"` : 'null'}, ${num(n.x)}, ${num(n.y)}, Drive.${up(n.drive || 'none')}${more}),`;
  });
  return `        ["${look}"] = new("${d.id}", Gait.${d.gait}, Headless: false,
            Glow: 0,
            Eye: new(${num(eye.x)}, ${num(eye.y)}, (${num(d.eye.travel[0])}, ${num(d.eye.travel[1])}), (${num(d.eye.travelWide[0])}, ${num(d.eye.travelWide[1])})),
            Pose: new(EarUp: ${num(p.earUp)}, EarBack: ${num(p.earBack)}, EarPerk: ${num(p.earPerk)}, LegAmp: ${num(p.legAmp)}, Lift: ${num(p.lift)}, TailAmp: ${num(p.tailAmp)}, TailFlee: ${num(p.tailFlee)}, Graze: ${num(p.graze)}, Hop: 0, LegLen: ${num(p.legLen)}, FootW: ${num(p.footW)}),
            Parts: new Dictionary<string, Part>
            {
${parts.join('\n')}
            },
            Rig:
            [
${rig.join('\n')}
            ],
            Ink: true),`;
}

Subjects.creatures = function () {
  for (const d of Object.values(DESIGNS))
    for (const [name, q] of Object.entries(d.parts)) {
      out(`creatures/${d.id}/${name}`, Anime.bake({ w: q.w, h: q.h, margin: MARGIN, shift: [q.px, q.py], draw: q.draw }));
      if (q.glow) out(`creatures/${d.id}/${name}_glow`, glowOf(q));
    }
  writeText('../../Content/logic/CreatureInk.cs', `// Generated by tools/anime-art/subjects/creatures.js: export again, never edit.
using System.Collections.Generic;

namespace Stillwake.Game;

public static partial class CreatureArt
{
    /// <summary>The designs drawn in ink and watercolour, by the look they stand in for: their parts are in <c>art/hd/creatures/{id}</c>.</summary>
    public static readonly IReadOnlyDictionary<string, Design> InkDesigns = new Dictionary<string, Design>
    {
${Object.entries(DESIGNS).map(([look, d]) => record(look, d)).join('\n')}
    };
}
`);
};
})();

// The tick, picked on a page («Зонд», 2026-11-01-guards-in-ink): each part placed by hand, posed in the game by game/features/creatures/logic/TickRig.cs.
// World pixels: the body's origin is the middle of its feet, facing right, y down; a limb's part lies along +x from its pivot. Also writes its numbers into Content/logic/TickInk.cs.
(function () {
'use strict';
const { poly, rng } = Anime;

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
/** A closed outline through the points, moved along its normal a little, so the edge of a placed thing is worn and not a ruled line. */
function hide(pts, r, amp) {
  const s = spline(pts, true), n = s.length, out = [];
  s.forEach((p, i) => {
    const a = s[(i + n - 1) % n], b = s[(i + 1) % n], dx = b[0] - a[0], dy = b[1] - a[1], l = Math.hypot(dx, dy) || 1, k = r ? (r() - .5) * (amp === undefined ? .4 : amp) : 0;
    out.push(p[0] - (dy / l) * k, p[1] + (dx / l) * k);
  });
  return poly(out);
}
/** A hard-cornered plate: straight edges a little worn. */
const plate = (pts, r, amp) => hide(pts.map((p) => [p[0], p[1], 'c']), r, amp === undefined ? .1 : amp);
/** A pen line through the points. */
const run = (...pts) => 'M' + spline(pts, false, .8).map((p) => `${p[0].toFixed(2)} ${p[1].toFixed(2)}`).join('L');
const mul = (hex, k) => '#' + [1, 3, 5].map((i) => Math.min(255, Math.round(parseInt(hex.substr(i, 2), 16) * k)).toString(16).padStart(2, '0')).join('');
const mat = (base, shade, deep, lite, ink) => ({ base, shade, deep, lite, spec: lite, ink: ink || '#1a120d' });
const farOf = (m) => mat(mul(m.base, .62), mul(m.shade, .66), mul(m.deep, .72), mul(m.base, .74), m.ink);
/** An eight-sided boss, a machine's joint: never a circle. */
const oct = (x, y, r) => { const k = r * .41; return poly(x - r, y - k, x - k, y - r, x + k, y - r, x + r, y - k, x + r, y + k, x + k, y + r, x - k, y + r, x - r, y + k); };
const riv = (a, x, y, c, r) => a.flat(oct(x, y, r || .34), c);
/** Rust from a seam: a torn edge and a darker heart. */
function rust(a, pts, seed, base, heart) {
  const r = rng(seed), cx = pts.reduce((s, p) => s + p[0], 0) / pts.length, cy = pts.reduce((s, p) => s + p[1], 0) / pts.length;
  a.flat(hide(pts, r, .5), base || '#7c4a2a');
  a.flat(hide(pts.map((p) => [cx + (p[0] - cx) * .48, cy + (p[1] - cy) * .48]), r, .3), heart || '#54301c');
}
/** A joint where a limb turns: a boss with a pin, over the end of what it joins, so no box shows. */
function joint(a, x, y, r, m, pin) {
  a.shape(oct(x, y, r), m, { shade: poly(x - r - 1, y + r * .15, x + r + 1, y - r * .1, x + r + 1, y + r + 1, x - r - 1, y + r + 1) });
  riv(a, x, y, pin || '#191716', r * .36);
}
/** A straight limb along +x, w0 half wide at its pivot, w1 at its end. */
const bar = (L, w0, w1, m, more) => (a) => {
  a.shape(poly(-.3, -w0, L + .3, -w1, L + .3, w1, -.3, w0), m, { shade: poly(-1, w0 * .2, L + 1, w1 * .2, L + 1, w1 + 1, -1, w0 + 1), lines: more && more.lines });
  if (more && more.after) more.after(a);
};
/** A limb's picture: its size and pivot for a limb L long and up to `half` wide about its axis. */
const limbPart = (L, half, draw) => ({ w: L + half * 2 + 2, h: half * 2 + 2, px: half + 1, py: half + 1, draw });
/** A rig entry for a leg from its row: id, hip x and y, foot x, knee's bend, gait's phase, folded angles in degrees. */
function legOf(up, low, L1, L2, foot, ankle) {
  return (l) => ({ id: l[0], leg: { hip: [l[1], l[2]], foot: l[3], bend: l[4], phase: l[5], fold: [l[6][0] * Math.PI / 180, l[6][1] * Math.PI / 180], L: [L1, L2], up, low, footPart: foot, ankle: ankle || 0 } });
}

/* ---------- «Зонд»: an old probe, a bare truss on stilts ---------- */

function candidateB() {
  const ZINC = mat('#a8adab', '#7a7f7f', '#555a5b', '#c4c8c4');
  const FOIL = mat('#b28f46', '#86692f', '#5a4620', '#cdad64');
  const BLACK = mat('#2a2c2f', '#1d1f22', '#131416', '#42454a');
  const FARZ = farOf(ZINC), BAND = '#b23b24';
  const up = (m) => limbPart(17.5, 2.4, (a) => {
    bar(17.5, 1.05, .8, m, { lines: [run([2, 0], [15, 0])] })(a);
    joint(a, 0, 0, 1.05, m);
    joint(a, 17.5, 0, 1.15, m);
    a.flat(poly(13.8, -.95, 15.4, -.9, 15.4, .9, 13.8, .95), m === ZINC ? BAND : mul(BAND, .62));
  });
  const low = (m) => limbPart(29, 2.2, (a) => {
    a.shape(poly(-.3, -.95, 8, -.85, 8.2, -.62, 27, -.32, 29, 0, 27, .32, 8.2, .62, 8, .85, -.3, .95), m, { shade: poly(-1, .05, 30, 0, 30, 2, -1, 2) });
    a.line(run([8.1, -.85], [8.1, .85]), { w: 1.8 });
    a.flat(poly(25, -.35, 27, -.32, 29, 0, 27, .32, 25, .35), mul(BLACK.lite, 1.1));
    a.line(run([14, -.5], [14, .5]), { w: 1.2 });
  });
  const legs = [
    ['LF1', -4, -15, -12, 1, .25, [204, -3], 'far'], ['LF2', 3.5, -15, 11, -1, .75, [196, -6], 'far'],
    ['LN1', -6.5, -14, -18, 1, 0, [208, -2], 'near'], ['LN2', 5, -14, 16.5, -1, .5, [192, -8], 'near'],
  ];
  return {
    // 'Списанный зонд на ходулях: голая ферма, колени выше спины, живот висит низко. Бледный цинк, рваная золотая обшивка на ядре, чёрные кабели, сопло больше всего тела. Ни панциря, ни морды, сквозь него видно космос: давно забыли выключить.
    centre: [0, -13],
    pose: { crouch: 5.5, stride: 6, lift: 2.6, lookAmp: .28, headUp: -.3, headSaw: -.1, jawOpen: .6, alertTilt: 0, sawTilt: -.05, flyLift: 0 },
    nozzle: { node: 'nozzle', x: -6.4, y: 0, r: 3.1, len: 1.15 },
    lamp: { node: 'head', d: poly(2.6, -2.8, 3.6, -2.8, 3.6, .3, 2.6, .3), core: [3.1, -1.3], travel: [.1, .8] },
    cut: [18, -13], cutLeg: 'arm',
    emit: { node: 'head', x: 4.2, y: -1.4 },
    arcs: [[-8.5, -19.5], [8.5, -19.5], [-8.5, -8], [8.5, -8], [-3, -33], [1.5, -29]],
    splits: [-3, 4], shatter: ['body'],
    crackNode: 'body',
    cracks: [
      [[-1, -13], [-2.4, -14.6], [-1.8, -16.2], [-3.4, -17.4]],
      [[-1, -13], [1, -14.2], [1.8, -16], [4, -16.4]],
      [[-1, -13], [-3.6, -12.6], [-4.4, -10.8], [-6.2, -10.2]],
      [[-1, -13], [.8, -11.4], [3, -11], [4.6, -9.4]],
    ],
    parts: {
      upN: up(ZINC), upF: up(FARZ), lowN: low(ZINC), lowF: low(FARZ),
      body: { w: 26, h: 38, px: 13, py: 36, draw(a) {
        const r = rng(0xb1);
        // The far side of the cage first, dark.
        const F = farOf(ZINC);
        a.tube(run([-9, -19], [9, -19]), .55, F); a.tube(run([-9, -8], [9, -8]), .55, F);
        // The whips: what the arcs jump from.
        a.tube(run([-5.5, -19.4], [-3, -33]), .42, ZINC, { spec: false }); a.tube(run([3, -19.4], [1.5, -29]), .42, ZINC, { spec: false });
        a.flat(poly(-3.5, -33.4, -2.5, -33.4, -2.6, -32.2, -3.4, -32.2), BAND); a.flat(poly(1, -29.4, 2, -29.4, 1.9, -28.2, 1.1, -28.2), BAND);
        // The core, wrapped in foil once gold; the wrap has torn.
        a.shape(plate([[-6.6, -17.6], [2, -18.4], [6.4, -16.4], [7, -11], [4.8, -8.4], [-3, -7.8], [-7.2, -10]], r, .22), FOIL, {
          shade: [poly(-8, -12, -2, -11, 3, -12.4, 8, -14, 8, -7, -8, -7), poly(-5.4, -18, -3.4, -18, -4.6, -12, -6.4, -12)],
          deep: [poly(-8, -9.6, 0, -9.2, 5, -10.4, 5, -7, -8, -7), poly(.8, -18.4, 1.8, -18.4, .8, -13, -.2, -13)],
          lite: [poly(-2.6, -17.6, 0, -17.8, -1.2, -13.6, -3, -13.4), poly(3, -17.6, 5.4, -16.2, 4.2, -14, 2.6, -15)],
          lines: [run([-5, -16], [-3.2, -14.4], [-3.6, -12]), run([-1, -17], [.6, -14.8]), run([2.6, -13], [5.4, -12.4]), run([-6, -10.6], [-3, -9.8]), run([1, -10], [3.2, -9])],
        });
        // Where the foil has gone: bare black box.
        a.flat(hide([[1.4, -11.8], [4.6, -12.4], [5.8, -10.2], [3.6, -8.8], [1.6, -9.6]], r, .4), BLACK.base);
        a.line(run([2.2, -11], [4.8, -10.6]), { w: 1.2 }); a.line(run([2.4, -10], [4, -9.8]), { w: 1.2 });
        // Hanging cables.
        a.tube(run([-6.8, -9], [-8, -6.6], [-5.4, -5.4], [-3, -7.6]), .42, BLACK, { spec: false });
        a.tube(run([5.6, -9], [6.8, -6], [8.4, -7.8]), .36, BLACK, { spec: false });
        // The near side of the cage over it: tubes and braces, so space shows through.
        for (const d of [run([-9, -19.4], [9, -19.4]), run([-9, -8], [9, -8]), run([-9, -19.4], [-9, -8]), run([9, -19.4], [9, -8]), run([-9, -19.4], [0, -8]), run([0, -8], [9, -19.4])]) a.tube(d, .72, ZINC);
        for (const [x, y] of [[-9, -19.4], [9, -19.4], [-9, -8], [9, -8], [0, -8]]) a.flat(oct(x, y, .6), BLACK.base);
        a.flat(poly(-9.5, -14.6, -8.5, -14.6, -8.5, -12.4, -9.5, -12.4), BAND);
        rust(a, [[8.6, -13], [9.6, -12.6], [9.5, -10.6], [8.6, -10.2]], 0xb7);
      } },
      nozzle: { w: 13, h: 12, px: 9, py: 6, draw(a) {
        // The bell, its throat bolted to the cage, two fuel lines to it.
        a.shape(plate([[2.4, -1.4], [-.8, -1.5], [-2.8, -2.2], [-5, -3.6], [-6.8, -4.6], [-6.8, 4.6], [-5, 3.6], [-2.8, 2.2], [-.8, 1.5], [2.4, 1.4]], rng(0xb3), .06), mat('#6c645c', '#4d4640', '#332e2a', '#887e72'), { shade: poly(-8, .9, 3, .4, 3, 6, -8, 6), deep: poly(-7, 2.6, -2.6, 1.2, -2.6, 6, -7, 6), lite: poly(-5.6, -3.8, -4, -2.8, -4.6, -2.2, -6, -3.2) });
        a.flat(poly(-6.9, -3.6, -6, -3.3, -6, 3.3, -6.9, 3.6), '#0b0a0a');
        for (const x of [-1.8, -3.4, -5]) a.line(run([x, -2.8 - (-1.8 - x) * .5], [x, 2.8 + (-1.8 - x) * .5]), { w: 1.2 });
        a.tube(run([3, -3.4], [.8, -3.2], [-1.2, -2]), .36, BLACK, { spec: false });
        a.flat(poly(-5, -3.5, -3.4, -2.6, -3.4, -1.4, -5, -2.2), '#7b4f2e');
      } },
      head: { w: 9, h: 9, px: 3, py: 4.5, draw(a) {
        // A sensor box on a boom: one thin slit, no face.
        a.tube(run([-3, 2.2], [.4, 0]), .7, ZINC);
        a.shape(plate([[.2, -3.6], [4.4, -3.8], [5, -1.6], [4.6, 1.2], [.2, 1.6]], rng(0xb2), .05), BLACK, { shade: poly(-1, -.2, 6, -.4, 6, 3, -1, 3), lite: poly(.8, -3.3, 4, -3.4, 3.8, -2.8, 1, -2.7) });
        a.flat(poly(2.3, -3.1, 3.9, -3.1, 3.9, .6, 2.3, .6), '#070708');
        a.flat(poly(.2, -4.6, 4.7, -4.8, 4.8, -3.8, .2, -3.6), ZINC.base);
        a.line(run([.2, -3.7], [4.7, -3.8]), { w: 2 });
      } },
      jaw: { w: 8, h: 6, px: 2, py: 2, draw(a) {
        // A gripper's two fingers under the box.
        a.shape(plate([[-1, -.8], [2, -.8], [5, .4], [5.6, 2.4], [3.8, 1.2], [1.6, .8], [-1, .8]], rng(0xb4), .04), ZINC, { shade: poly(-2, 0, 6, .2, 6, 3, -2, 3) });
        a.flat(poly(4.4, .1, 5.6, 2.4, 3.8, 1.2, 3.6, .3), BLACK.base);
      } },
      armU: limbPart(8, 1.6, (a) => { bar(8, .62, .5, ZINC)(a); joint(a, 0, 0, .7, BLACK); }),
      armL: limbPart(11, 2.4, (a) => {
        // A cutter: a thin arm ending in a hooked blade.
        a.shape(plate([[-.2, -.5], [6, -.45], [6.6, -1.5], [9.2, -1.3], [11, 0], [8.8, .2], [7.4, 1.1], [6.4, .55], [-.2, .5]], rng(0xb5), .03), ZINC, { shade: poly(-1, .1, 7, .1, 12, .1, 12, 2, -1, 2) });
        a.flat(poly(7.4, -1.4, 9.2, -1.3, 11, 0, 8.8, .2, 7.6, 1), BAND);
        joint(a, 0, 0, .85, BLACK);
      }),
    },
    rig: [
      ...legs.filter((l) => l[7] === 'far').map(legOf('upF', 'lowF', 17.5, 29)),
      { id: 'nozzle', part: 'nozzle', parent: 'body', x: -10, y: -13.6 },
      { id: 'arm', leg: { arm: true, hip: [9, -9.5], rest: [15, -4.5], bend: -1, fold: [60 * Math.PI / 180, -170 * Math.PI / 180], L: [8, 11], up: 'armU', low: 'armL' } },
      { id: 'body', part: 'body', parent: null },
      { id: 'jaw', part: 'jaw', parent: 'head', x: 1.8, y: 1.4, drive: 'jaw' },
      { id: 'head', part: 'head', parent: 'body', x: 11, y: -17.2, drive: 'head' },
      ...legs.filter((l) => l[7] === 'near').map(legOf('upN', 'lowN', 17.5, 29)),
    ],
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

const WHITE = '#fff';
const bodyBox = { w: 26, h: 38, px: 13, py: 36 }, headBox = { w: 9, h: 9, px: 3, py: 4.5 };
const legRow = (l) => `new("${l.up}", "${l.low}", ${dbl(l.hip[0])}, ${dbl(l.hip[1])}, ${dbl(l.foot === undefined ? l.rest[0] : l.foot)}, ${dbl(l.rest ? l.rest[1] : 0)}, ${dbl(l.bend)}, ${dbl(l.phase || 0)}, ${dbl(l.fold[0])}, ${dbl(l.fold[1])}, ${dbl(l.L[0])}, ${dbl(l.L[1])})`;

Subjects.tick = function () {
  const d = candidateB(), node = (id) => d.rig.find((n) => n.id === id), I = '        ';
  const parts = {};
  for (const [name, q] of Object.entries(d.parts)) parts[name] = snapped(q);
  // What the game adds over it unlit, in the colours it has that moment: white here.
  parts.lamp = snapped({ ...headBox, light: true, draw: (c) => { c.fillStyle = WHITE; c.fill(new Path2D(d.lamp.d)); } });
  parts.pupil = snapped({ w: 3, h: 4, px: 1.5, py: 2, light: true, draw: (c) => { c.fillStyle = WHITE; c.fill(new Path2D(poly(-.5, -.7, .5, -.7, .5, .7, -.5, .7))); } });
  parts.cracks = snapped({ ...bodyBox, light: true, draw: (c) => {
    c.strokeStyle = WHITE; c.lineWidth = .62;
    for (const run of d.cracks) { c.beginPath(); run.forEach((p, i) => (i ? c.lineTo(p[0], p[1]) : c.moveTo(p[0], p[1]))); c.stroke(); }
  } });
  bakeAll('inkTick', parts);
  const legs = (near) => d.rig.filter((n) => n.leg && !n.leg.arm && (n.id[1] === 'N') === near).map((n) => legRow(n.leg)).join(', ');
  const p = d.pose, head = node('head'), jaw = node('jaw'), nozzle = node('nozzle');
  writeText('../../Content/logic/TickInk.cs', `// Generated by tools/anime-art/subjects/tick.js: export again, never edit.
using System.Collections.Generic;

namespace Stillwake.Game;

public static partial class CreatureArt
{
    /// <summary>The tick drawn in ink and watercolour, its parts in <c>art/hd/creatures/inkTick</c>: «Зонд», an old probe on stilts.</summary>
    public static readonly TickDesign InkTick = new("inkTick",
        new Dictionary<string, Part>
        {
${partsOf(parts, I + '    ')}
        },
        Centre: ${pair(d.centre)}, Crouch: ${dbl(p.crouch)}, Stride: ${dbl(p.stride)}, Lift: ${dbl(p.lift)}, LookAmp: ${dbl(p.lookAmp)}, HeadUp: ${dbl(p.headUp)}, HeadSaw: ${dbl(p.headSaw)},
        JawOpen: ${dbl(p.jawOpen)}, SawTilt: ${dbl(p.sawTilt)},
        FarLegs: [${legs(false)}],
        NearLegs: [${legs(true)}],
        Arm: ${legRow(node('arm').leg)},
        NozzleAt: ${pair([nozzle.x, nozzle.y])}, NozzleEnd: ${pair([d.nozzle.x, d.nozzle.y])}, HeadAt: ${pair([head.x, head.y])}, JawAt: ${pair([jaw.x, jaw.y])},
        Eye: ${pair(d.lamp.core)}, EyeTravel: ${pair(d.lamp.travel)}, Cut: ${pair(d.cut)},
        Arcs: [${d.arcs.map(pair).join(', ')}]);
}
`);
};
})();

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

/* ---------- Traced creatures: their pictures cut from a picked reference, legs, necks and tails bent whole ---------- */

/**
 * What places a creature traced from refs/<name>.png (2026-11-14-creatures-bend): S world pixels a reference pixel; points are the
 * reference's. part(piece, pivot, o) is a part of that piece turning about pivot, its box round the trace; o: only (which ops), flat
 * (their colour, no line), materials, fade (clear at its first point, whole from its second, world pixels from the pivot), bend, glow
 * and its tint.
 */
function tracer(name, S) {
  const piece = (p) => name + '.' + p;
  const at = (p, from) => [(p[0] - from[0]) * S, (p[1] - from[1]) * S];
  /** The turn from straight down to the way from a to b, clockwise: a bent part's axis. */
  const axis = (a, b) => Math.atan2(-(b[0] - a[0]), b[1] - a[1]);
  /** k world pixels from a toward b. */
  const along = (a, b, k) => { const l = Math.hypot(b[0] - a[0], b[1] - a[1]); return [(b[0] - a[0]) / l * k, (b[1] - a[1]) / l * k]; };
  const dark = (M) => Object.fromEntries(Object.entries(M).map(([k, m]) => [k, { ...m, mat: farOf(m.mat) }]));
  const xy = ([x, y]) => ({ x: +x.toFixed(2), y: +y.toFixed(2) });
  function part(p, pivot, o = {}) {
    const t = TRACED[piece(p)], [x0, y0, x1, y1] = t.fit.box, shift = at(t.origin, pivot);
    const px = Math.ceil(-(x0 * S + shift[0]) + .5), py = Math.ceil(-(y0 * S + shift[1]) + .5);
    const ops = Traced.placed(piece(p), S, shift).filter(o.only || (() => true));
    const draw = o.flat ? (a) => ops.forEach((op) => a.flat(op.d, o.flat)) : Traced.drawer(piece(p), ops, t.fit.units * S, o.materials);
    return { w: Math.ceil(px + (x1 + 1) * S + shift[0] + .5), h: Math.ceil(py + (y1 + 1) * S + shift[1] + .5), px, py, draw, fade: o.fade, bend: o.bend, glow: o.glow, glowTint: o.glowTint };
  }
  return { at, axis, along, dark, xy, part, materials: (p) => TRACED[piece(p)].materials };
}

/**
 * The loper as picked (2026-11-14-creatures-bend): a round grey grazer on long legs, traced from refs/loper.png for its form only and
 * cut there into its rig's parts (its palette's pieces). Each part is placed by points of the reference: its pivot, where it fades
 * into what it grows from, where it bends; the feet's middle at FEET. One picture of a foreleg and one of a hind leg serve the near
 * and far pairs, the far ones darker.
 */
function loper() {
  // Its trunk as long as the hand-drawn loper's was.
  const S = .039, FEET = [715, 985];
  const { at, axis, along, dark, xy, part, materials } = tracer('loper', S);
  // Pivots: the neck's root by its throat (bent about its middle, the wide throat inside a grazing bend folds over),
  // the ear's base, each near leg's hip, the tail's root.
  const NECK = [1067, 473], POLL = [1258, 172], EAR = [1275, 140], EYE = [1358, 155], FORE = [900, 605], HIND = [450, 605], TAIL = [455, 462];
  // Its knee and hock, how far below each leg's pivot; the far pair this much ahead of the near. Each leg's picture rises into
  // its shoulder or haunch, fading in from TOP and whole by WHOLE: on the body however it swings (creatures-bend).
  const KNEE = (745 - FORE[1]) * S, HOCK = (738 - HIND[1]) * S, FAR = 67 * S, FARF = 40 * S, TOP = 570, WHOLE = 588;
  const M = materials('body'), FARM = dark(M);
  const neckFade = [along(NECK, POLL, -44 * S), along(NECK, POLL, 20 * S)], earFade = [at([1276, 158], EAR), at([1272, 128], EAR)];
  const leg = (name, pivot, joint, materials) =>
    part(name, pivot, { materials, fade: [[0, (TOP - pivot[1]) * S], [0, (WHOLE - pivot[1]) * S]], bend: { joint, band: 75 * S, axis: 0 } });
  const fore = at(FORE, FEET), hind = at(HIND, FEET), neck = at(NECK, FEET), ear = at(EAR, NECK);
  return {
    eye: { travel: [.25, .15], travelWide: [.3, .2] },
    pose: { earUp: 0, earBack: -.5, earPerk: -.6, legAmp: .32, lift: +(44 * S).toFixed(2), tailAmp: .12, tailFlee: -.5, graze: 1.6, legLen: +((990 - FORE[1]) * S).toFixed(2), footW: +(33 * S).toFixed(2), knee: .55 },
    parts: {
      body: part('body', FEET, { only: (op) => op.mat !== 'dark' }),
      // Its dark flank stripes, each a marking one may or may not wear.
      marks: part('body', FEET, { only: (op) => op.op === 'shape' && op.mat === 'dark', flat: M.dark.mat.base }),
      // The neck and the skull as one, bending along the neck so the skull nods and grazes on it and its root stays on the shoulders.
      head: part('neck', NECK, { fade: neckFade, bend: { joint: 90 * S, band: 270 * S, axis: axis(NECK, POLL) } }),
      ear: part('ear', EAR, { fade: earFade }),
      earFar: part('ear', EAR, { materials: FARM, fade: earFade }),
      foreLeg: leg('foreLeg', FORE, KNEE),
      foreLegFar: leg('foreLeg', FORE, KNEE, FARM),
      hindLeg: leg('hindLeg', HIND, HOCK),
      hindLegFar: leg('hindLeg', HIND, HOCK, FARM),
      tail: part('tail', TAIL, { bend: { joint: (TAIL[0] - 320) * S, band: 222 * S, axis: Math.PI / 2 } }),
      ...eyeParts(1, .55, M.hide.mat, { wide: 1.25, tall: 1.6 }),
    },
    rig: [
      { id: 'earFar', part: 'earFar', parent: 'head', x: ear[0] - 27 * S, y: ear[1] + 4 * S, drive: 'ear', base: -.28 },
      { id: 'legFH', part: 'hindLegFar', parent: 'body', x: hind[0] + FAR, y: hind[1], drive: 'leg', phase: 0 },
      { id: 'legFF', part: 'foreLegFar', parent: 'body', x: fore[0] + FARF, y: fore[1], drive: 'leg', phase: .5 },
      { id: 'tail', part: 'tail', parent: 'body', ...xy(at(TAIL, FEET)), drive: 'tail' },
      { id: 'body', part: 'body', parent: null, x: 0, y: 0, drive: 'body' },
      { id: 'marks', part: 'marks', parent: 'body', x: 0, y: 0 },
      { id: 'legNH', part: 'hindLeg', parent: 'body', x: hind[0], y: hind[1], drive: 'leg', phase: .5 },
      { id: 'legNF', part: 'foreLeg', parent: 'body', x: fore[0], y: fore[1], drive: 'leg', phase: 0 },
      { id: 'head', part: 'head', parent: 'body', x: neck[0], y: neck[1], drive: 'head' },
      { id: 'eye', part: 'eye', parent: 'head', ...xy(at(EYE, NECK)), drive: 'eye' },
      { id: 'ear', part: 'ear', parent: 'head', x: ear[0], y: ear[1], drive: 'ear', base: 0 },
    ],
  };
}

/**
 * The tapir as picked (creatures-bend): a round grazer under a domed shell of three plates, traced from refs/tapir.png. Its short neck
 * bends low by the throat, so the head nods and grazes on it under a still shell; its four short legs fold a little at the knee; its
 * stubby tail turns whole. The seed pod is today's, hung from the snout.
 */
function tapir() {
  // As long and as tall as the hand-drawn tapir was, shell and snout; the feet's middle at FEET.
  const S = .03, FEET = [650, 965];
  const { at, axis, dark, xy, part, materials } = tracer('tapir', S);
  const POD = mat('#2f8f88', '#1e6a66', '#144a48', '#6fdcc4');
  // The neck's root low by the throat, its bone forward along the head: it bends JOINT ahead of it, past the shoulder and before the ear.
  const NECK = [930, 700], SNOUT = [1300, 700], JOINT = 75, EAR = [1085, 468], EYE = [1215, 503], SEED = [1448, 634];
  const FORE = [865, 668], HIND = [290, 668], TAIL = [168, 572];
  // Its knee and hock below each leg's pivot, at the reference's creases; the far legs ahead of the near by the reference's. Each
  // leg's picture rises into the body, fading in from TOP and whole by WHOLE, as the loper's.
  const KNEE = (775 - FORE[1]) * S, HOCK = (765 - HIND[1]) * S, FARF = 112 * S, FARH = 135 * S, TOP = 612, WHOLE = 640;
  const M = materials('body'), FARM = dark(materials('foreLeg'));
  const leg = (name, pivot, joint, materials) =>
    part(name, pivot, { materials, fade: [[0, (TOP - pivot[1]) * S], [0, (WHOLE - pivot[1]) * S]], bend: { joint, band: 70 * S, axis: 0 } });
  const fore = at(FORE, FEET), hind = at(HIND, FEET), neck = at(NECK, FEET), ear = at(EAR, NECK);
  const earFade = [at([1085, 480], EAR), at([1085, 452], EAR)];
  return {
    eye: { travel: [.2, .12], travelWide: [.25, .15] },
    pose: { earUp: 0, earBack: -.6, earPerk: .1, legAmp: .46, lift: 1.1, tailAmp: .1, tailFlee: -.4, graze: .62, legLen: +((965 - FORE[1]) * S).toFixed(2), footW: +(72 * S).toFixed(2), knee: .35 },
    parts: {
      body: part('body', FEET, { only: (op) => op.mat !== 'spot' }),
      // The ochre spots on its shell, each a marking one may or may not wear.
      marks: part('body', FEET, { only: (op) => op.op === 'shape' && op.mat === 'spot', flat: M.spot.mat.base }),
      // The neck and the head as one, bending low by the throat, so the head nods and grazes while the shell over it stays.
      head: part('head', NECK, { fade: [[-28 * S, 0], [4 * S, 0]], bend: { joint: JOINT * S, band: 160 * S, axis: axis(NECK, SNOUT) } }),
      // Hung from the snout on a dry stalk: the one thing about it that works, so the one bright thing.
      pod: { w: 8, h: 9, px: 4, py: 1, glow: [[-.2, 4.4, 1.5]], draw(a) {
        a.flat(limb([[0, -.6, .4], [.4, 1.2, .3], [-.2, 2.8, .35]]), DARK.base);
        a.line(run([-.3, -.6], [.1, 1.2], [-.5, 2.8]), { w: 1.3 });
        a.shape(hide([[-1.7, 3.8], [-.2, 2.7], [1.3, 3.7], [1.5, 5.2], [-.2, 6.4, 'c'], [-1.8, 5.2]], rng(0xc4), .08), POD, { shade: poly(.2, 2, 3, 2, 3, 7, .2, 7) });
      } },
      ear: part('ear', EAR, { fade: earFade }),
      earFar: part('ear', EAR, { materials: dark(materials('ear')), fade: earFade }),
      foreLeg: leg('foreLeg', FORE, KNEE),
      foreLegFar: leg('foreLeg', FORE, KNEE, FARM),
      hindLeg: leg('hindLeg', HIND, HOCK),
      hindLegFar: leg('hindLeg', HIND, HOCK, FARM),
      tail: part('tail', TAIL),
      ...eyeParts(.95, .45, M.hide.mat, { wide: 1.15, tall: 1.5 }),
    },
    rig: [
      { id: 'legFH', part: 'hindLegFar', parent: 'body', x: hind[0] + FARH, y: hind[1], drive: 'leg', phase: 0 },
      { id: 'legFF', part: 'foreLegFar', parent: 'body', x: fore[0] + FARF, y: fore[1], drive: 'leg', phase: .5 },
      { id: 'tail', part: 'tail', parent: 'body', ...xy(at(TAIL, FEET)), drive: 'tail' },
      { id: 'body', part: 'body', parent: null, x: 0, y: 0, drive: 'body' },
      { id: 'marks', part: 'marks', parent: 'body', x: 0, y: 0 },
      // Its far ear behind its head and before the shell, a little ahead of the near one.
      { id: 'earFar', part: 'earFar', parent: 'head', x: ear[0] + 26 * S, y: ear[1] - 6 * S, drive: 'ear', base: -.2 },
      { id: 'legNH', part: 'hindLeg', parent: 'body', x: hind[0], y: hind[1], drive: 'leg', phase: .5 },
      { id: 'legNF', part: 'foreLeg', parent: 'body', x: fore[0], y: fore[1], drive: 'leg', phase: 0 },
      { id: 'head', part: 'head', parent: 'body', x: neck[0], y: neck[1], drive: 'head' },
      { id: 'pod', part: 'pod', parent: 'head', ...xy(at(SEED, NECK)), drive: 'pod' },
      { id: 'eye', part: 'eye', parent: 'head', ...xy(at(EYE, NECK)), drive: 'eye' },
      { id: 'ear', part: 'ear', parent: 'head', x: ear[0], y: ear[1], drive: 'ear', base: 0 },
    ],
  };
}

/**
 * The sixleg as picked (creatures-bend): a long indigo barrel in bold cream stripes, spines down its back, traced from refs/sixleg.png.
 * Its neck bends by the throat; its legs are the reference's hind, middle and fore, the far three darker, each folding at its hock or
 * knee and growing out of the body from inside it; its tail arches over its back hung with glowing berries and bends where it rises,
 * so the whole arch lifts and sways.
 */
function sixleg() {
  // As long in the trunk as the hand-drawn sixleg was; the feet's middle at FEET.
  const S = .032, FEET = [676, 965];
  const { at, axis, along, dark, xy, part, materials } = tracer('sixleg', S);
  const NECK = [1090, 500], MUZZLE = [1350, 400], EAR = [1290, 370], EYE = [1352, 398];
  // Each near leg's hip inside the body above the belly, over its hock or knee (a bend far off its bone folds over), its picture
  // fading in above it; the bend at the reference's bump. The far legs as far ahead of the near as their hoofs are, the fore pair's
  // kept under the chest.
  const HIND = [285, 570], MID = [632, 570], FORE = [960, 570], TOP = 512, WHOLE = 545, JOINT_Y = 695, FAR = [137, 121, 10];
  // The tail's pivot on its stem where it turns up behind the rump; it bends JOINT above, the arch over the back past the bend.
  const TAIL = [50, 420], JOINT = 80, BERRIES = [[215, 230], [245, 240], [310, 175], [445, 152], [487, 173], [530, 127], [632, 143], [690, 158]];
  const M = materials('body'), FARM = dark(materials('midLeg'));
  // Its stripes stay the body's marks: a leg's top over one is hide.
  const leg = (name, pivot, materials) => part(name, pivot, {
    materials, only: (op) => op.mat !== 'stripe', fade: [[0, (TOP - pivot[1]) * S], [0, (WHOLE - pivot[1]) * S]], bend: { joint: (JOINT_Y - pivot[1]) * S, band: 70 * S, axis: 0 },
  });
  const neck = at(NECK, FEET), ear = at(EAR, NECK), hip = (x) => at([x, HIND[1]], FEET);
  const earFade = [at([1290, 380], EAR), at([1288, 350], EAR)];
  // Today's tripods: the near hind and fore with the far middle, then the rest. The four behind fold as hocks, the fore pair as knees.
  const node = ([id, p, x, phase, hind]) => ({ id, part: p, parent: 'body', ...xy(hip(x)), drive: 'leg', phase, hind });
  const far = [['l4', 'hindLegFar', HIND[0] + FAR[0], .5, true], ['l5', 'midLegFar', MID[0] + FAR[1], 0, true], ['l6', 'foreLegFar', FORE[0] + FAR[2], .5, false]].map(node);
  const near = [['l1', 'hindLeg', HIND[0], 0, true], ['l2', 'midLeg', MID[0], .5, true], ['l3', 'foreLeg', FORE[0], 0, false]].map(node);
  return {
    eye: { travel: [0, 0], travelWide: [0, 0] },
    pose: { earUp: 0, earBack: -.42, earPerk: -.65, legAmp: .38, lift: 1.7, tailAmp: .07, tailFlee: -.25, graze: 1.02, legLen: +((965 - HIND[1]) * S).toFixed(2), footW: +(35 * S).toFixed(2), knee: .5 },
    parts: {
      body: part('body', FEET, { only: (op) => op.mat !== 'stripe' }),
      // Its cream stripes, each a marking one may or may not wear.
      marks: part('body', FEET, { only: (op) => op.op === 'shape' && op.mat === 'stripe', flat: M.stripe.mat.base }),
      // The neck and the long head as one, bending by the throat.
      head: part('head', NECK, { fade: [along(NECK, MUZZLE, -25 * S), along(NECK, MUZZLE, 15 * S)], bend: { joint: 100 * S, band: 180 * S, axis: axis(NECK, MUZZLE) } }),
      ear: part('ear', EAR, { fade: earFade }),
      earFar: part('ear', EAR, { materials: dark(materials('ear')), fade: earFade }),
      hindLeg: leg('hindLeg', HIND),
      hindLegFar: leg('hindLeg', HIND, FARM),
      midLeg: leg('midLeg', MID),
      midLegFar: leg('midLeg', MID, FARM),
      foreLeg: leg('foreLeg', FORE),
      foreLegFar: leg('foreLeg', FORE, FARM),
      // Bent where its stem rises: the arch and its berries above turn whole about the bend.
      tail: part('tail', TAIL, {
        bend: { joint: JOINT * S, band: 80 * S, axis: Math.PI },
        glow: BERRIES.map((b) => [...at(b, TAIL), 15 * S]), glowTint: ['rgba(255,170,70,.55)', '#ffe2a8'],
      }),
      // No white and no pupil: a pit that narrows and widens.
      eye_open: { w: 8, h: 6, px: 4, py: 3, draw: (a) => { a.flat('M-1.4 .25Q-.3 -.4 1.4 0Q.2 .4 -1.4 .25Z', '#120d0b'); } },
      eye_wide: { w: 8, h: 6, px: 4, py: 3, draw: (a) => { a.flat('M-1.8 .3Q-.6 -1 1.8 -.2Q.4 1 -1.8 .3Z', '#120d0b'); } },
      lid_half: { w: 8, h: 6, px: 4, py: 3, draw: (a) => { a.flat(poly(-2.2, -1, 2.2, -1, 2.2, .05, -2.2, .25), M.hide.mat.base); a.line(run([-2, .3], [2, .05]), { w: 1.4 }); } },
      lid_blink: { w: 8, h: 6, px: 4, py: 3, draw: (a) => { a.flat(poly(-2.2, -1, 2.2, -1, 2.2, 1, -2.2, 1), M.hide.mat.base); a.line(run([-2, .3], [2, .05]), { w: 1.8 }); } },
    },
    rig: [
      { id: 'tail', part: 'tail', parent: 'body', ...xy(at(TAIL, FEET)), drive: 'tail' },
      ...far,
      { id: 'body', part: 'body', parent: null, x: 0, y: 0, drive: 'body' },
      { id: 'marks', part: 'marks', parent: 'body', x: 0, y: 0 },
      { id: 'earFar', part: 'earFar', parent: 'head', x: ear[0] - 30 * S, y: ear[1] + 4 * S, drive: 'ear', base: -.25 },
      ...near,
      { id: 'head', part: 'head', parent: 'body', x: neck[0], y: neck[1], drive: 'head' },
      { id: 'eye', part: 'eye', parent: 'head', ...xy(at(EYE, NECK)), drive: 'eye' },
      { id: 'ear', part: 'ear', parent: 'head', x: ear[0], y: ear[1], drive: 'ear', base: 0 },
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
  const [halo, heart] = q.glowTint || ['rgba(150,235,215,.55)', '#d8fff2'];
  for (const g of q.glow) at(g, 1, halo);
  x.filter = 'none';
  for (const g of q.glow) at(g, .55, heart);
  return c;
}

/**
 * A part that grows out of what it is drawn over, faded in along its fade: clear at its first point, whole from its second, in its own
 * pixels from its pivot. Its cut edge there, outlined as any traced shape is, never shows on what it grows from.
 */
function fade(c, q) {
  const g = c.getContext('2d'), k = c.width / (q.w + MARGIN * 2), [[ax, ay], [bx, by]] = q.fade.map(([x, y]) => [(q.px + MARGIN + x) * k, (q.py + MARGIN + y) * k]);
  const ramp = g.createLinearGradient(ax, ay, bx, by);
  ramp.addColorStop(0, 'rgba(0,0,0,0)');
  ramp.addColorStop(1, 'rgba(0,0,0,1)');
  g.globalCompositeOperation = 'destination-in';
  g.fillStyle = ramp;
  g.fillRect(0, 0, c.width, c.height);
  g.globalCompositeOperation = 'source-over';
}

function record(look, d) {
  const eye = d.rig.find((n) => n.drive === 'eye'), p = d.pose, I = '            ';
  const parts = Object.entries(d.parts).map(([name, q]) => `${I}    ["${name}"] = new(${q.w + MARGIN * 2}, ${q.h + MARGIN * 2}, ${num(q.px + MARGIN)}, ${num(q.py + MARGIN)}, []${q.glow ? ', Glow: true' : ''}${q.bend ? `, Bend: new(${num(q.bend.joint)}, ${num(q.bend.band)}, ${num(q.bend.axis)})` : ''}),`);
  const rig = d.rig.map((n) => {
    const more = [n.phase ? `Phase: ${num(n.phase)}` : '', n.hind ? 'Hind: true' : '', n.base ? `Base: ${num(n.base)}` : ''].filter(Boolean).map((s) => ', ' + s).join('');
    return `${I}    new("${n.id}", "${n.part}", ${n.parent ? `"${n.parent}"` : 'null'}, ${num(n.x)}, ${num(n.y)}, Drive.${up(n.drive || 'none')}${more}),`;
  });
  return `        ["${look}"] = new("${d.id}", Gait.${d.gait}, Headless: false,
            Glow: 0,
            Eye: new(${num(eye.x)}, ${num(eye.y)}, (${num(d.eye.travel[0])}, ${num(d.eye.travel[1])}), (${num(d.eye.travelWide[0])}, ${num(d.eye.travelWide[1])})),
            Pose: new(EarUp: ${num(p.earUp)}, EarBack: ${num(p.earBack)}, EarPerk: ${num(p.earPerk)}, LegAmp: ${num(p.legAmp)}, Lift: ${num(p.lift)}, TailAmp: ${num(p.tailAmp)}, TailFlee: ${num(p.tailFlee)}, Graze: ${num(p.graze)}, Hop: 0, LegLen: ${num(p.legLen)}, FootW: ${num(p.footW)}${p.knee ? `, Knee: ${num(p.knee)}` : ''}),
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
      const c = Anime.bake({ w: q.w, h: q.h, margin: MARGIN, shift: [q.px, q.py], draw: q.draw });
      if (q.fade) fade(c, q);
      out(`creatures/${d.id}/${name}`, c);
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

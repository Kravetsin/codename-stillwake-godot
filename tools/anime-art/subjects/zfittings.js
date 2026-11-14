// What the ship is fitted with: pipes, valves, sockets, hatches, launch decks, floor and ladder; the drone; the hero's tools.
(function () {
'use strict';
const { ell, rr, poly, bake, canvas } = Anime;
const { STEEL, GUN, DARK, COPPER, PIPE, band, rivets } = window.AnimeKit;

/** The pipe's run, a sheet repeated along a span: riveted seam, a rust patch at a rivet, a collar every sheet. It is 4 px thick, rows 2 to 6 of 8. */
const pipeRun = (a) => {
  const w = 24, y0 = 2, y1 = 6, clip = band(0, y0, w, y1);
  a.flat(clip, PIPE.base);
  a.flat(band(0, y0, w, y0 + 1), PIPE.lite);
  a.flat(band(0, y1 - 1.4, w, y1), PIPE.shade);
  a.flat(band(0, y1 - .5, w, y1), PIPE.deep);
  a.flat(poly(15.1, 4.5, 17.2, 4.3, 17.7, 5, 16.8, 5.95, 16.2, 5.5, 15.4, 5.9, 15.2, 5.2), '#6e5646', { clip });
  a.flat(poly(15.8, 4.7, 16.8, 4.6, 16.9, 5.3, 16, 5.4), '#4f3d36', { clip });
  for (const y of [y0, y1]) a.line(`M-1 ${y}L${w} ${y}`, { w: 3 });
  a.line(`M-1 4.7L${w} 4.7`, { w: 1.1 });
  rivets(a, [3.5, 4], [8, 4], [16.2, 4], [20.5, 4]);
  a.shape(rr(10.6, y0 - .6, 2.8, 5.2, .3), GUN, { lite: band(10.6, y0 - .6, 13.4, y0 + .2), shade: band(10.6, y1 - .5, 13.4, y1 + .6), lines: ['M12 1.6L12 6.4'] });
};
/** A joint's bolted flange, its middle the cell's. */
const flange = (a) => {
  a.shape(ell(4, 4, 3.7), GUN, { shade: .6, lite: ell(3.5, 3.4, 2.6) });
  a.flat(ell(4, 4, 2), PIPE.shade);
  a.flat(ell(3.8, 3.7, 1.2), PIPE.base);
  for (let i = 0; i < 6; i++) rivets(a, [4 + Math.cos(.26 + i * Math.PI / 3) * 2.85, 4 + Math.sin(.26 + i * Math.PI / 3) * 2.85]);
};
/** A pipe by its arms (up 1, right 2, down 4, left 8), one cell, what a build card lays together: the run's body, the flange where a run turns or ends. */
function pipe(open) {
  return bake({ w: 8, h: 8, draw: (a) => {
    const arm = (x0, y0, x1, y1, lit, dark) => { a.flat(band(x0, y0, x1, y1), PIPE.base); a.flat(lit, PIPE.lite); a.flat(dark, PIPE.shade); };
    if (open & 8) arm(0, 2, 4, 6, band(0, 2, 4, 3), band(0, 4.6, 4, 6));
    if (open & 2) arm(4, 2, 8, 6, band(4, 2, 8, 3), band(4, 4.6, 8, 6));
    if (open & 1) arm(2, 0, 6, 4, band(2, 0, 3, 4), band(4.6, 0, 6, 4));
    if (open & 4) arm(2, 4, 6, 8, band(2, 4, 3, 8), band(4.6, 4, 6, 8));
    if (open & 10) for (const y of [2, 6]) a.line(`M${open & 8 ? -1 : 4} ${y}L${open & 2 ? 9 : 4} ${y}`, { w: 3 });
    if (open & 5) for (const x of [2, 6]) a.line(`M${x} ${open & 1 ? -1 : 4}L${x} ${open & 4 ? 9 : 4}`, { w: 3 });
    if (open !== 5 && open !== 10) flange(a);
  } });
}
const valve = (open) => (a) => {
  const m = open ? { base: '#3fae5c', shade: '#256a3c', deep: '#1a4c2c', lite: '#86e08c', spec: '#e0ffe0' } : { base: '#d0483c', shade: '#8a2c30', deep: '#5c1c22', lite: '#f08c74', spec: '#ffe0d0' };
  const turn = open ? .4 : .4 + Math.PI / 4, corners = [];
  for (let i = 0; i < 8; i++) corners.push(5 + Math.cos(Math.PI / 8 + i * Math.PI / 4) * 4.7, 5 + Math.sin(Math.PI / 8 + i * Math.PI / 4) * 4.7);
  a.shape(poly(corners), GUN, { shade: .6, lite: ell(4.4, 4.2, 3) });
  a.shape(ell(5, 5, 3.8), m, { shade: .6, lite: ell(4.6, 4.5, 2.7) });
  a.flat(ell(5, 5, 2.4), '#14151c');
  for (let i = 0; i < 3; i++) {
    const t = turn + (i / 3) * Math.PI * 2, c = Math.cos(t), s = Math.sin(t), w = .5, R = 2.8;
    a.flat(poly(5 - s * w, 5 + c * w, 5 + c * R - s * w, 5 + s * R + c * w, 5 + c * R + s * w, 5 + s * R - c * w, 5 + s * w, 5 - c * w), m.base);
  }
  a.flat(ell(5, 5, 1.1), m.lite);
  a.flat(ell(4.75, 4.7, .45), m.spec);
};
const socket = (a) => { a.shape(ell(4, 4, 3.2), DARK, { outer: true, lite: .0001 }); a.flat(ell(4, 4, 1.6), '#12131e'); a.flat(band(2.6, 3.6, 5.4, 4.4), '#4f9cbc'); };
const powerSocket = (a) => { a.shape(ell(2.5, 2.5, 2), { base: '#868fa2', shade: '#474c68', lite: '#c4ccd8', spec: '#ffffff' }, { shade: .4 }); a.flat(ell(2.5, 2.5, 1), '#12131e'); };

/** A hatch segment by what it joins (right 2, left 8): its frame, the same open and shut; the plate between, only shut, so FineSlid finds exactly it. */
function hatch(links, shut) {
  const left = links & 8 ? 0 : 1, right = links & 2 ? 16 : 15;
  const frame = bake({ w: 16, h: 16, draw: (a) => {
    a.shape(band(0, 0, 16, 1), DARK, { outer: false, lite: band(0, .2, 16, .5) });
    a.shape(band(0, 7, 16, 8), DARK, { outer: false, lite: band(0, 7.2, 16, 7.5) });
    if (!(links & 8)) a.shape(band(0, 0, 1, 8), DARK, { outer: false, lite: band(.2, .2, .5, 7.8) });
    if (!(links & 2)) a.shape(band(15, 0, 16, 8), DARK, { outer: false });
  } });
  const c = canvas(64, 64), x = c.getContext('2d');
  if (shut) x.drawImage(bake({ w: 16, h: 16, draw: (a) => {
    a.shape(band(left, 1, right, 7), STEEL, { outer: false, lite: band(left, 1, right, 1.5), shade: band(left, 5.4, right, 7), lines: [`M${left} 5.4L${right} 5.4`] });
    // Hazard across its leading edge: a plate that shuts on someone says so; the only bright thing on it.
    for (const px of [2, 6, 10, 14]) a.flat(poly(px, 1.7, px + 1.8, 1.7, px + 1.1, 3, px - .7, 3), '#c8a030', { clip: band(left, 1, right, 7) });
    if (!(links & 8)) { a.flat(band(3, 4, 7, 4.9), '#1a1c28'); a.flat(band(3, 4, 7, 4.2), '#0f111b'); }
    if (!(links & 2)) { a.shape(band(10.6, 3.6, 13.8, 5.6), { base: '#c8a030', shade: '#8e7028', deep: '#685428', lite: '#e4c45c', spec: '#f4e49c', ink: '#1e1808' }, { outer: false, lite: band(10.6, 3.6, 13.8, 4), shade: band(10.6, 5, 13.8, 5.6) }); }
  } }), 0, 0);
  x.drawImage(frame, 0, 0);
  return c;
}
/** A launch deck piece: studs along its top, ended where it joins nothing; open, its middle gapes. */
const deck = (links, open) => (a) => {
  a.shape(band(0, 1.2, 16, 7), GUN, { outer: false, lite: band(0, 1.2, 16, 2), shade: band(0, 5.4, 16, 7) });
  a.flat(band(0, 7, 16, 8), '#14162a');
  a.flat(band(0, .8, 16, 1.2), '#14162a');
  for (const x of [2.4, 7, 11.6]) { a.flat(band(x, .2, x + 2.4, 1.2), '#f0c43c'); a.flat(band(x, .2, x + 2.4, .5), '#fff0b0'); }
  a.flat(band(0, 3.2, 16, 3.8), '#868fa2');
  if (open) a.flat(band(0, 2.2, 16, 6), '#12131e');
  if (!(links & 8)) { a.shape(rr(0, 0, 1.8, 8, .2), DARK, { outer: false, lite: band(0, 0, .6, 8) }); }
  if (!(links & 2)) { a.shape(rr(14.2, 0, 1.8, 8, .2), DARK, { outer: false, shade: band(15.4, 0, 16, 8) }); }
};
const ladder = (a) => {
  for (const x of [3, 11.6]) a.flat(band(x, 0, x + 1.4, 16), '#6e5436');
  for (const x of [3, 11.6]) a.flat(band(x, 0, x + .5, 16), '#d4b878');
  for (const y of [2, 6, 10, 14]) { a.flat(band(3, y, 13, y + 1.1), '#a98a4c'); a.flat(band(3, y, 13, y + .35), '#f6e6b4'); a.flat(band(3, y + 1.1, 13, y + 1.5), '#2c2018'); }
};
const floor = (a) => { a.flat(band(0, 0, 16, 4), '#14162a'); a.flat(band(0, .4, 16, 3.4), '#5c6474'); a.flat(band(0, .4, 16, 1), '#a7afbc'); a.flat(band(0, 2.6, 16, 3.4), '#383c58'); };

// The drone: rotors out on arms, a rounded hull with one eye.
const flyBlur = ['#ffffff', '#dfe7f2', '#ffffff', '#c4ccd8'];
const droneFly = (f) => (a) => {
  const span = [5, 3.6, 5, 4.2][f];
  a.tube('M3 2.4L7 6', 1.1, GUN);
  a.tube('M15 2.4L11 6', 1.1, GUN);
  for (const x of [3, 15]) { a.flat(band(x - span, .6, x + span * .5, 1.7), flyBlur[f]); a.flat(rr(x - .7, .6, 1.4, 2.2, .2), '#383c58'); }
  a.shape(rr(4.6, 4.4, 8.8, 5.4, 2), STEEL, { lite: band(5.4, 4.5, 11, 5.6), spec: band(6.2, 4.7, 8.6, 5.1), shade: band(4.6, 8, 13.4, 9.8), lines: ['M5 7.2L13 7.2'] });
  a.shape(rr(11, 4.8, 3, 2.4, .4), DARK, { outer: false });
  a.tube('M9 9.6L9 11', .9, GUN);
};
const droneDock = (a) => {
  a.shape(rr(2, 1, 10, 1.6, .3), STEEL, { lite: band(2.2, 1, 11.8, 1.5) });
  a.tube('M4 2.6L4 4.6', .8, GUN); a.tube('M10 2.6L10 4.6', .8, GUN);
  a.shape(rr(2.2, 3.6, 10, 5, 1.6), STEEL, { lite: band(3, 3.7, 9, 4.7), shade: band(2.2, 7, 12.2, 8.6) });
  a.shape(rr(9.2, 4.4, 3, 2.4, .4), DARK, { outer: false });
  for (const x of [2, 9.4]) a.shape(rr(x, 8.8, 2.6, 2.6, .3), GUN, { lite: band(x, 8.8, x + 1, 11.4) });
};
const eye = (x, y) => (a) => { a.flat(rr(x - .5, y - .5, 3, 3, .5), '#2ca4d8'); a.flat(rr(x, y, 1.6, 1.6, .3), '#a8ecff'); };
const lampOf = (c) => (a) => { a.flat(rr(6.6, 2.7, 2.8, 1.2, .3), c); };

// The tools in the hand.
// The fire axe, the user's pick of five drawn references («Пожарный, тяжёлая головка»), shipped raw (refs/fireAxe.png).
/** The fire axe's picture, 34 × 13 (the pickaxe's length): its picture's box fitted along it, half a pixel round it. */
const AXE_W = 34, AXE_H = 13;
const axe = (a) => {
  const [x0, y0, x1, y1] = TRACED.fireAxe.fit.box, s = (AXE_W - 1) / (x1 - x0 + 1);
  const at = [.5 - x0 * s, (AXE_H - (y1 - y0 + 1) * s) / 2 - y0 * s];
  Traced.raw('fireAxe', s, at)(a);
};
// The drill, the user's pick of five drawn references («Из трубы»), shipped raw (refs/drill.png) without its bit: the bit is drawn here,
// so its flutes can move a third of a groove a frame while it spins.
/** The drill's picture, 33 × 16 (the old one's length): its picture and the bit fitted along it, half a pixel round them. */
const DRILL_W = 33, DRILL_H = 16, DRILL_TIP = 1500;
const BIT = { base: '#b4b8c0', shade: '#767c8c', deep: '#4e5464', lite: '#dde0e6', spec: '#f6f7f8', ink: '#14162a' };
const drill = (spin) => (a) => {
  const [x0, y0, , y1] = TRACED.drill.fit.box, s = (DRILL_W - 1) / (DRILL_TIP - x0 + 1);
  const at = [.5 - x0 * s, (DRILL_H - (y1 - y0 + 1) * s) / 2 - y0 * s], X = (x) => x * s + at[0], Y = (y) => y * s + at[1];
  // The bit, placed by the reference's pixels: a shank out of the chuck, the fluted body, its point; under the chuck's nose.
  const cy = Y(298.5), r = 23.5 * s, sx0 = X(1100), sx1 = X(1148), bx1 = X(1466), tip = X(DRILL_TIP);
  a.shape(rr(sx0, cy - r * .55, sx1 - sx0 + .3, r * 1.1, .15), BIT, { lite: band(sx0, cy - r * .5, sx1, cy - r * .15), shade: band(sx0, cy + r * .2, sx1, cy + r * .55) });
  const body = poly(sx1, cy - r, bx1, cy - r, tip, cy, bx1, cy + r, sx1, cy + r);
  a.shape(body, BIT, { lite: band(sx1, cy - r * .75, bx1, cy - r * .3), shade: poly(sx1, cy + r * .3, bx1, cy + r * .3, tip, cy, bx1, cy + r, sx1, cy + r) });
  const pitch = 2.1, shift = spin === null ? 0 : (spin % 3) * pitch / 3;
  for (let x = sx1 - pitch + shift; x < tip; x += pitch) a.flat(poly(x, cy - r, x + .5, cy - r, x + 1.2, cy + r, x + .7, cy + r), BIT.deep, { clip: body });
  Traced.raw('drill', s, at)(a);
};

// The salvage tools, each the user's pick of drawn references, shipped raw: their helpers, where they differ from the ones above.
const DEG = Math.PI / 180;
// Full saturation only for what works: the jet's violet.
const VIOLET_ON = '#b49aff';
/** Points along a circle from a0 to a1, in degrees (+y down: 0 ahead, 90 down, 270 up). */
const arcPts = (cx, cy, r, a0, a1, n) => {
  const p = [];
  for (let i = 0; i <= n; i++) { const t = (a0 + (a1 - a0) * i / n) * DEG; p.push(cx + Math.cos(t) * r, cy + Math.sin(t) * r); }
  return p;
};
const steps = (a0, a1) => Math.max(4, Math.ceil(Math.abs(a1 - a0) / 9));
/** A band of a ring between radii r0 and r1, from a0 to a1 degrees. */
const arcBand = (cx, cy, r0, r1, a0, a1) => { const n = steps(a0, a1); return poly(arcPts(cx, cy, r1, a0, a1, n).concat(arcPts(cx, cy, r0, a1, a0, n))); };
/** Several pictures of one size laid in order, each finished alone, as the pick page lays a tool's front over it. */
const layered = (w, h, draws, px = 4) => {
  const c = canvas(w * px, h * px), x = c.getContext('2d');
  for (const draw of draws) x.drawImage(bake({ w, h, px, draw }), 0, 0);
  return c;
};

// The grinder, the user's pick of five drawn references («Нынешняя, перерисованная»), shipped raw (refs/grinder.png): a long motor
// tube in worn ochre enamel, vents at its back, a pistol grip, a cast head, the cut-off wheel under a half guard, a lamp on its end cap.
// Running, f 1..3: the wheel's nut and washer smeared round, light streaks sweeping its face a third of their turn a frame. The head
// hides the wheel's back, so the wheel itself never turns; the streaks show only where it is seen.
/** The grinder's pictures, 30 × 12; the wheel's middle and radius, and its washer's radius, in the reference's pixels. */
const GRINDER_W = 30, GRINDER_H = 12, WHEEL = [1247, 497, 245], WASHER = 92, STREAK = '#bab6b0';
/** The part of an arc from a0 to a1 degrees (0 ahead, +y down) the head leaves in sight, within 115 degrees of ahead. */
const seen = (a0, a1) => {
  const s0 = ((a0 + 180) % 360 + 360) % 360 - 180;
  return [Math.max(s0, -115), Math.min(s0 + a1 - a0, 115)];
};
/** The washer and nut cut from the picture, turned through a sixth of a turn and laid over each other: a nut spinning. */
let smeared = null;
const smear = () => {
  if (smeared) return smeared;
  const img = Traced.picture('grinder'), [ax, ay] = TRACED.grinder.raw.at, d = WASHER * 2, n = 12;
  smeared = document.createElement('canvas');
  smeared.width = smeared.height = d;
  const x = smeared.getContext('2d');
  x.beginPath(); x.arc(WASHER, WASHER, WASHER, 0, Math.PI * 2); x.clip();
  for (let i = 0; i < n; i++) {
    x.save(); x.globalAlpha = 1 / (i + 1);
    x.translate(WASHER, WASHER); x.rotate(i * Math.PI / 3 / n);
    x.drawImage(img, ax - WHEEL[0], ay - WHEEL[1]);
    x.restore();
  }
  return smeared;
};
const grinder = (f) => (a) => {
  const { s, at, X, Y } = Traced.fit('grinder', GRINDER_W, GRINDER_H);
  Traced.raw('grinder', s, at)(a);
  if (f === null) return;
  const [cx, cy, r] = [X(WHEEL[0]), Y(WHEEL[1]), WHEEL[2] * s];
  for (let k = 0; k < 3; k++) {
    const t = f * 40 + k * 120;
    for (const [r0, r1, from, span] of [[.5, .56, t - 20, 60], [.76, .82, t + 15, 75]]) {
      const [a0, a1] = seen(from, from + span);
      if (a1 - a0 > 4) a.flat(arcBand(cx, cy, r * r0, r * r1, a0, a1), STREAK);
    }
  }
  a.image(smear(), X(WHEEL[0] - WASHER), Y(WHEEL[1] - WASHER), WASHER * 2 * s, WASHER * 2 * s);
};

// The plasma cutter, the user's pick of five drawn references («Самодельный»), shipped raw (refs/plasmaCutter.png): a rusted
// tube on a pistol grip, two copper coils over it, a blue gas bottle under, green insulators and a copper tip the jet leaves from.
// Running, the tip glows white-hot in violet, drawn here.
/** The plasma cutter's pictures, 32 × 15; the tip's middle in the reference's pixels. */
const CUTTER_W = 32, CUTTER_H = 15, CUTTER_TIP = [1488, 494];
const cutter = (on) => (a) => {
  const { s, at, X, Y } = Traced.fit('plasmaCutter', CUTTER_W, CUTTER_H);
  Traced.raw('plasmaCutter', s, at)(a);
  if (!on) return;
  const [x, y] = [X(CUTTER_TIP[0]), Y(CUTTER_TIP[1])];
  a.flat(ell(x, y, 34 * s, 30 * s), VIOLET_ON);
  a.flat(ell(x + 4 * s, y, 20 * s, 16 * s), '#efe9da');
};

// The spear, the user's pick of five drawn references («Телескопическое»), shipped raw (refs/spear.png): a three-part aluminium pole
// with twist locks, a foam grip, a long blade lashed on with cord and tape. Its arc module is its own picture (refs/spearArc.png, an
// edit of the spear by the same model, cut where it differs): a coil box between the locks, a cable run to the blade.
const SPEAR_W = 64, SPEAR_H = 10;
const spear = (a) => { const { s, at } = Traced.fit('spear', SPEAR_W, SPEAR_H); Traced.raw('spear', s, at)(a); };
const spearArc = (a) => { const { s, at } = Traced.fit('spear', SPEAR_W, SPEAR_H); Traced.raw('spearArc', s, at)(a); };

// The sledgehammer, the user's pick of five drawn references («Нынешняя, перерисованная»), shipped raw (refs/sledgehammer.png): five
// cut plates bolted into a block, a wooden handle through a tube ferrule, black tape round its end.
const SLEDGE_W = 36, SLEDGE_H = 13;
const sledgehammer = (a) => { const { s, at } = Traced.fit('sledgehammer', SLEDGE_W, SLEDGE_H); Traced.raw('sledgehammer', s, at)(a); };

// The pickaxe, the user's pick of five drawn references («Нынешняя, перерисованная»), shipped raw (refs/pickaxe.png): a pipe haft taped
// at the butt, two welded collars, a rusted plate head with its hole, the steel spike down, what meets the stone.
/** The pickaxe's picture, 34 × 19: its picture's box fitted along it, half a pixel round it. */
const PICK_W = 34, PICK_H = 19;
const pickaxe = (a) => {
  const [x0, y0, x1, y1] = TRACED.pickaxe.fit.box, s = (PICK_W - 1) / (x1 - x0 + 1);
  const at = [.5 - x0 * s, (PICK_H - (y1 - y0 + 1) * s) / 2 - y0 * s];
  Traced.raw('pickaxe', s, at)(a);
};

Subjects.zfittings = function () {
  out('pipe_run', bake({ w: 24, h: 8, tile: true, draw: pipeRun }));
  for (let n = 0; n < 16; n++) out(`pipe_${n}`, pipe(n));
  out('valve_open', bake({ w: 10, h: 10, draw: valve(true) }));
  out('valve_closed', bake({ w: 10, h: 10, draw: valve(false) }));
  out('socket', bake({ w: 8, h: 8, draw: socket }));
  out('power-socket', bake({ w: 5, h: 5, draw: powerSocket }));
  for (const links of [0, 2, 8, 10]) {
    out(`hatch_closed_${links}`, hatch(links, true));
    out(`hatch_open_${links}`, hatch(links, false));
    out(`platform_${links}`, bake({ w: 16, h: 8, draw: deck(links, false) }));
    out(`platform_open_${links}`, bake({ w: 16, h: 8, draw: deck(links, true) }));
  }
  out('platform', bake({ w: 16, h: 8, draw: deck(10, false) }));
  out('platform_open', bake({ w: 16, h: 8, draw: deck(10, true) }));
  out('ladder', bake({ w: 16, h: 16, tile: true, draw: ladder }));
  out('floor', bake({ w: 16, h: 4, draw: floor }));
  for (let f = 0; f < 4; f++) out(`drone_fly_${f}`, bake({ w: 18, h: 12, draw: droneFly(f) }));
  out('drone_dock', bake({ w: 14, h: 12, draw: droneDock }));
  out('drone_eye', bake({ w: 18, h: 12, draw: eye(12.2, 5.2) }));
  out('drone_dock_eye', bake({ w: 14, h: 12, draw: eye(10.2, 5.2) }));
  out('drone_lamp_work', bake({ w: 18, h: 12, draw: lampOf('#a8ecff') }));
  out('drone_lamp_low', bake({ w: 18, h: 12, draw: lampOf('#ff5040') }));
  out('hero_axe', bake({ w: AXE_W, h: AXE_H, draw: axe }));
  out('hero_drill', bake({ w: DRILL_W, h: DRILL_H, draw: drill(null) }));
  // Their icons (icons/<picture>), as hero.js draws the hand tools': the same drawing at many pixels a world pixel.
  for (const [name, pic] of [['hero_axe', { w: AXE_W, h: AXE_H, draw: axe }], ['hero_drill', { w: DRILL_W, h: DRILL_H, draw: drill(null) }]])
    out(`icons/${name}`, bake({ ...pic, px: Math.ceil(Anime.ICON_LONG / pic.w) }));
  for (let f = 1; f <= 3; f++) out(`hero_drill_spin${f}`, bake({ w: DRILL_W, h: DRILL_H, draw: drill(f) }));
  // The salvage tools: each still, then its running states (the grinder's three turns of the disc, the cutter lit), and its icon.
  const salvage = [
    ['hero_grinder', GRINDER_W, GRINDER_H, [grinder(null)]],
    ['hero_plasmaCutter', CUTTER_W, CUTTER_H, [cutter(false)]],
    ['hero_spear', SPEAR_W, SPEAR_H, [spear]],
    ['hero_sledgehammer', SLEDGE_W, SLEDGE_H, [sledgehammer]],
    ['hero_pickaxe', PICK_W, PICK_H, [pickaxe]],
  ];
  for (const [name, w, h, draws] of salvage) {
    out(name, layered(w, h, draws));
    out(`icons/${name}`, layered(w, h, draws, Math.ceil(Anime.ICON_LONG / w)));
  }
  for (let f = 1; f <= 3; f++) out(`hero_grinder_spin${f}`, layered(GRINDER_W, GRINDER_H, [grinder(f)]));
  out('hero_plasmaCutter_on', layered(CUTTER_W, CUTTER_H, [cutter(true)]));
  out('hero_spear_arc', layered(SPEAR_W, SPEAR_H, [spearArc]));
};
})();

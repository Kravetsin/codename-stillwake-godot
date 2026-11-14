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

// The salvage tools, each the user's pick drawn whole (salvage-tools): the grinder «Заводская», the plasma cutter «Самодельный», the spear
// «Труба и лист», the sledgehammer «Пакет плит». Their pick pages' own materials and helpers, where they differ from the ones above.
const DEG = Math.PI / 180, f2 = (n) => +n.toFixed(3);
const RUBBER = { base: '#2e2c2a', shade: '#1d1c1b', deep: '#121212', lite: '#45423e' };
const RED_PAINT = { base: '#a63c2e', shade: '#742a28', deep: '#541e20', lite: '#c8604a', spec: '#e8a890' };
const STUD = { base: '#b4bcc8', shade: '#6c7486', deep: '#474c68', lite: '#d8dee6', spec: '#f0f2f4' };
const SLOT = '#14172c', CHIP = '#8e96a2';
// Full saturation only for what works: the running lamp the game's own green, the charge lights' steady green, the jet's violet.
const LAMP_ON = '#a0f0a0', LAMP_OFF = '#3d5a46', CHG = '#4f9a58', ON = '#9dff8a', VIOLET_ON = '#b49aff';
/** Points along a circle from a0 to a1, in degrees (+y down: 0 ahead, 90 down, 270 up). */
const arcPts = (cx, cy, r, a0, a1, n) => {
  const p = [];
  for (let i = 0; i <= n; i++) { const t = (a0 + (a1 - a0) * i / n) * DEG; p.push(cx + Math.cos(t) * r, cy + Math.sin(t) * r); }
  return p;
};
const steps = (a0, a1) => Math.max(4, Math.ceil(Math.abs(a1 - a0) / 9));
/** A band of a ring between radii r0 and r1, from a0 to a1 degrees. */
const arcBand = (cx, cy, r0, r1, a0, a1) => { const n = steps(a0, a1); return poly(arcPts(cx, cy, r1, a0, a1, n).concat(arcPts(cx, cy, r0, a1, a0, n))); };
/** An open arc, for a pen line. */
const arcPath = (cx, cy, r, a0, a1) => {
  const p = arcPts(cx, cy, r, a0, a1, steps(a0, a1));
  let s = `M${f2(p[0])} ${f2(p[1])}`;
  for (let i = 2; i < p.length; i += 2) s += `L${f2(p[i])} ${f2(p[i + 1])}`;
  return s;
};
/** A band between two ellipses round one middle, from a0 to a1 degrees: a ring seen near edge-on. */
const ellBand = (cx, cy, rx0, ry0, rx1, ry1, a0, a1) => {
  const n = steps(a0, a1) * 2, p = [];
  for (let i = 0; i <= n; i++) { const t = (a0 + (a1 - a0) * i / n) * DEG; p.push(cx + Math.cos(t) * rx1, cy + Math.sin(t) * ry1); }
  for (let i = n; i >= 0; i--) { const t = (a0 + (a1 - a0) * i / n) * DEG; p.push(cx + Math.cos(t) * rx0, cy + Math.sin(t) * ry0); }
  return poly(p);
};
const stud = (a, x, y, r = .42) => a.shape(ell(x, y, r), STUD, { outer: false, edge: false, shade: band(x - r, y + .05, x + r, y + r), lines: [`M${f2(x - r * .7)} ${f2(y + r * .3)}L${f2(x + r * .7)} ${f2(y - r * .3)}`] });
/** Rust round a seam or stud: a torn patch, its darker heart. */
const rustAt = (a, x, y, s) => {
  a.flat(poly(x - s, y - .2 * s, x - .3 * s, y - .7 * s, x + .5 * s, y - .5 * s, x + s, y + .1 * s, x + .4 * s, y + .45 * s, x - .5 * s, y + .4 * s), '#6e4630');
  a.flat(poly(x - .4 * s, y - .1 * s, x + .1 * s, y - .3 * s, x + .4 * s, y + .05 * s, x, y + .3 * s), '#3f2a24');
};
/** Rust where water sat: a torn patch with a darker heart, kept inside its part. */
const rust = (a, x, y, s, clip) => {
  a.flat(poly(x, y, x + 1.3 * s, y - .25 * s, x + 1.9 * s, y + .4 * s, x + 1.3 * s, y + 1.1 * s, x + .6 * s, y + .8 * s, x - .1 * s, y + .9 * s), '#6e5646', { clip });
  a.flat(poly(x + .5 * s, y + .15 * s, x + 1.3 * s, y + .1 * s, x + 1.3 * s, y + .7 * s, x + .6 * s, y + .6 * s), '#4f3d36', { clip });
};
/** Several pictures of one size laid in order, each finished alone, as the pick page lays a tool's front over it. */
const layered = (w, h, draws, px = 4) => {
  const c = canvas(w * px, h * px), x = c.getContext('2d');
  for (const draw of draws) x.drawImage(bake({ w, h, px, draw }), 0, 0);
  return c;
};

// The grinder «Заводская»: a long motor tube in worn ochre enamel, vents at its back, a paddle under it; a cast head, the wheel under a
// half guard. A state: f null standing, 1..3 the disc's turn while it spins; on, its lamp and charge lights lit.
const DISC = { base: '#57524e', shade: '#37332f', deep: '#24211f', lite: '#79726b', spec: '#a8a098' };
const OCHRE = { base: '#9a7c44', shade: '#6a5432', deep: '#4a3b28', lite: '#bc9e62', spec: '#e0cc98' };
/** A cut-off wheel seen face on: grit, its paper label, the print and the nut turning with it; spinning, the marks smear round. */
const wheel = (a, cx, cy, r, f, o = {}) => {
  const th = f === null ? 0 : f * 40, spin = f !== null;
  a.shape(ell(cx, cy, r), DISC, { shade: .7, lines: [arcPath(cx, cy, r * .84, 35, 145)] });
  for (let k = 0; k < 3; k++) {
    const t = th + 60 + k * 120;
    if (spin) { a.flat(arcBand(cx, cy, r * .72, r * .78, t - 30, t + 50), DISC.lite); a.flat(arcBand(cx, cy, r * .86, r * .9, t + 10, t + 80), DISC.lite); }
    else a.line(`M${f2(cx + Math.cos(t * DEG) * r * .68)} ${f2(cy + Math.sin(t * DEG) * r * .68)}L${f2(cx + Math.cos(t * DEG) * r * .93)} ${f2(cy + Math.sin(t * DEG) * r * .93)}`, { w: .9 });
  }
  a.flat(ell(cx, cy, r * .56), o.label || '#7a6c4e');
  a.flat(arcBand(cx, cy, r * .46, r * .56, 15, 165), o.labelShade || '#5a4e3a');
  for (let k = 0; k < 3; k++) { const t = th + k * 120; a.flat(arcBand(cx, cy, r * .33, r * .49, t, t + (spin ? 80 : 46)), o.print || '#3a2e22'); }
  a.shape(ell(cx, cy, r * .27), STEEL, { outer: false, shade: .25 });
  const hex = [];
  for (let k = 0; k < 6; k++) { const t = (th + 30 + k * 60) * DEG; hex.push(cx + Math.cos(t) * r * .17, cy + Math.sin(t) * r * .17); }
  a.shape(poly(hex), GUN, { outer: false, shade: .12 });
  for (const t of [th + 90, th + 270]) a.flat(ell(cx + Math.cos(t * DEG) * r * .22, cy + Math.sin(t * DEG) * r * .22, .18), '#14172c');
};
/** A guard's lip round the wheel from a0 to a1 degrees, over its rim: lit along the top, rolled at the edge. */
const guard = (a, cx, cy, r, a0, a1, m, o = {}) => {
  const r0 = r - (o.inset ?? .5), r1 = r + (o.out ?? 1);
  a.shape(arcBand(cx, cy, r0, r1, a0, a1), m, { shade: .3, lite: arcBand(cx, cy, r1 - .5, r1, Math.max(a0, 200), Math.min(a1, 340)), lines: [arcPath(cx, cy, r1 - .32, a0 + 3, a1 - 3), ...(o.lines || [])] });
};
const battery = (a, x, y, w, h, on) => {
  a.shape(rr(x, y, w, h, .5), GUN, { lite: band(x + .2, y + .1, x + w - .2, y + .6), shade: band(x, y + h - .7, x + w, y + h), lines: [`M${x + w - 1.4} ${y + .1}L${x + w - 1.4} ${y + h - .1}`] });
  for (let i = 0; i < 3; i++) a.glow(rr(x + .8 + i * 1.3, y + h * .45, .9, .55, .15), on ? ON : CHG);
};
const grinder = (s) => (a) => {
  const cx = 22.6, cy = 7.2, R = 4.6;
  battery(a, 1.4, 11.9, 7, 2.6, s.on);
  a.shape('M3.4 7.2L8 7.2L7.4 9.6C7.2 10.6 7.2 11.4 7.4 12.2L3 12.2C2.8 11 2.8 9.6 3.4 7.2Z', RUBBER, { shade: band(6.4, 7, 8.2, 12.4), lines: ['M3.3 8.6L7.7 8.6', 'M3.1 9.6L7.4 9.6', 'M3 10.6L7.3 10.6', 'M3 11.5L7.4 11.5'] });
  a.shape('M7.8 7.4L12.6 7.6L12.2 8.8L8.4 8.6Z', DARK, { lite: band(8, 7.5, 12.4, 7.95) });
  a.shape('M7.4 7.3L8.4 7.4L8.3 8.3L7.5 8Z', RED_PAINT, { outer: false });
  a.shape(rr(1.2, 2.4, 15.4, 5.4, 2.2), OCHRE, { lite: band(1.6, 2.5, 16.2, 3.4), spec: band(7.2, 2.75, 12.6, 3.05), shade: band(1.2, 6.6, 16.6, 7.8), lines: ['M6.6 2.5L6.6 7.7', 'M13.6 2.5L13.6 7.7'] });
  for (const x of [2.7, 3.7, 4.7, 5.7]) a.flat(poly(x, 3.7, x + .5, 3.7, x + .3, 6.4, x - .2, 6.4), SLOT);
  a.shape(rr(8.2, 4, 4.2, 1.8, .3), DARK, { outer: false, lite: band(8.3, 4.05, 12.3, 4.4) });
  a.line('M8.8 4.95L11.6 4.95', { w: .8, c: '#8a8070' });
  a.flat(poly(14.6, 2.5, 15.9, 2.7, 15.8, 3.6, 15, 3.3), CHIP);
  a.flat(poly(9.4, 7.3, 10.8, 7.2, 10.6, 7.75, 9.6, 7.75), CHIP);
  rustAt(a, 13.4, 6.9, .7);
  a.shape(rr(.3, 3, 1.6, 4.2, .7), DARK, { lite: band(.4, 3.1, 1.8, 3.6) });
  a.glow(ell(1.1, 4.3, .42), s.on ? LAMP_ON : LAMP_OFF, s.on ? { core: ell(1.05, 4.2, .16) } : {});
  a.shape('M16.2 2.3L21.2 1.6C22.6 1.5 23.6 2.4 23.6 3.8L23.6 8.2L16.2 8.2Z', GUN, { lite: band(16.4, 1.7, 23.4, 2.6), shade: band(16.2, 7.2, 23.8, 8.4), lines: ['M17.6 2.2L17.6 8.1', 'M18.8 2.1L18.8 4'] });
  stud(a, 21.6, 2.6);
  wheel(a, cx, cy, R, s.f, { label: '#4e6478', labelShade: '#3a4a5a', print: '#d8d2c2' });
  guard(a, cx, cy, R, 150, 350, STEEL);
  stud(a, cx + Math.cos(205 * DEG) * (R + .3), cy + Math.sin(205 * DEG) * (R + .3), .36);
  rustAt(a, cx + Math.cos(165 * DEG) * (R + .3), cy + Math.sin(165 * DEG) * (R + .3), .45);
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
    ['hero_grinder', 29, 15, [grinder({ f: null, on: false })]],
    ['hero_plasmaCutter', CUTTER_W, CUTTER_H, [cutter(false)]],
    ['hero_spear', SPEAR_W, SPEAR_H, [spear]],
    ['hero_sledgehammer', SLEDGE_W, SLEDGE_H, [sledgehammer]],
    ['hero_pickaxe', PICK_W, PICK_H, [pickaxe]],
  ];
  for (const [name, w, h, draws] of salvage) {
    out(name, layered(w, h, draws));
    out(`icons/${name}`, layered(w, h, draws, Math.ceil(Anime.ICON_LONG / w)));
  }
  for (let f = 1; f <= 3; f++) out(`hero_grinder_spin${f}`, bake({ w: 29, h: 15, draw: grinder({ f, on: true }) }));
  out('hero_plasmaCutter_on', layered(CUTTER_W, CUTTER_H, [cutter(true)]));
  out('hero_spear_arc', layered(SPEAR_W, SPEAR_H, [spearArc]));
};
})();

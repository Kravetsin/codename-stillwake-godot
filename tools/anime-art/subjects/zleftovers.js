// What was left of the ship and the interface: the bench's and the arc lamp's glows, the emergency lamp, the arc module's coil, the pod's lit and dim states; the wrist computer and the tablet. Sorts after machines to read AnimeKit.
(function () {
'use strict';
const { ell, rr, poly, bake, canvas } = Anime;
const { GUN, STEEL, DARK, band } = window.AnimeKit;

const TAU = Math.PI * 2;
const INK = '#14162a', LIT = '#7f879b', LIP = '#6f778a', BASE = '#596173', SHADE = '#444a5e', DEEP = '#2c3044';
const RUBBER = { base: '#282a3a', shade: '#1a1b28', deep: '#11121b', lite: '#40445a', spec: '#8a94a6', ink: '#0c0d14' };
const PLATE = { base: '#596173', shade: '#444a5e', deep: '#2c3044', lite: '#8a92a6', spec: '#c4ccd8', ink: '#14162a' };
const GO = '#4ade80', GO_OFF = '#1f3b2a', HAZ = '#d99a2c';

// ---------- a frame's runs: straight metal that repeats, its edges swelling with a period of the sheet, so a tiled run has no seam ----------
const wave = (T, seed, amp) => {
  const k1 = Math.max(1, Math.round(T / 11)), k2 = k1 * 2 + 1;
  return (t) => amp * (.6 * Math.sin(TAU * k1 * t / T + seed) + .4 * Math.sin(TAU * k2 * t / T + seed * 2.7));
};
/** A run along x, from y0 to y1, its two edges swelling apart. */
function alongX(T, y0, y1, seed, amp) {
  const a = wave(T, seed, amp), b = wave(T, seed + 1.9, amp), top = [], foot = [];
  for (let t = -1; t <= T + 1; t += .5) { top.push(t, y0 + a(t)); foot.unshift(t, y1 + b(t)); }
  return poly(top.concat(foot));
}
/** A run along y, from x0 to x1. */
function alongY(T, x0, x1, seed, amp) {
  const a = wave(T, seed, amp), b = wave(T, seed + 1.9, amp), left = [], right = [];
  for (let t = -1; t <= T + 1; t += .5) { left.push(x0 + a(t), t); right.unshift(x1 + b(t), t); }
  return poly(left.concat(right));
}
/** Layers of a run, back to front: [from, to, colour]. */
const runX = (T, rows, marks, amp = .07) => (a) => { rows.forEach(([y0, y1, c], i) => a.flat(alongX(T, y0, y1, 1.3 + i * 2.1, amp), c)); if (marks) marks(a); };
const runY = (T, cols, marks, amp = .07) => (a) => { cols.forEach(([x0, x1, c], i) => a.flat(alongY(T, x0, x1, .7 + i * 2.3, T < 4 ? 0 : amp), c)); if (marks) marks(a); };
/** A run's sheet, repeating along it. Across it, two clear pixels past what is used: the finish wraps a repeating sheet both ways, and would carry its foot onto its head. */
const sheetX = (T, h, draw) => ({ along: 'x', used: h, picture: bake({ w: T, h: h + 2, tile: true, draw }) });
const sheetY = (w, T, draw) => ({ along: 'y', used: w, picture: bake({ w: w + 2, h: T, tile: true, draw }) });

// Wear, by hand: a chip of bare metal, a dent, a rust stain with a darker heart, a short scratch.
const chip = (a, x, y, l, c = '#969eb0') => a.flat(poly(x, y, x + l, y - .06, x + l - .22, y + .34, x + .14, y + .4), c);
const dent = (a, x, y, l) => { a.flat(poly(x, y, x + l, y + .05, x + l - .3, y + .5, x + .2, y + .46), DEEP); a.flat(poly(x + .2, y + .46, x + l - .3, y + .5, x + l - .4, y + .62, x + .3, y + .58), LIT); };
const rust = (a, x, y, s) => {
  a.flat(poly(x - s, y - .2 * s, x - .3 * s, y - .7 * s, x + .5 * s, y - .5 * s, x + s, y + .1 * s, x + .4 * s, y + .4 * s, x + .1 * s, y + .75 * s, x - .5 * s, y + .45 * s), '#70482f');
  a.flat(poly(x - .4 * s, y - .1 * s, x + .1 * s, y - .3 * s, x + .45 * s, y + .05 * s, x, y + .3 * s), '#3f2a24');
};
/** All a short run may carry, since it repeats every 48 screen pixels: a patch of close tone, no edge to count. */
const quiet = (a, x, y, l) => a.flat(poly(x, y + .1, x + l * .4, y, x + l, y + .2, x + l * .8, y + .9, x + .2, y + .8), '#555d6f');
const screw = (a, x, y, turn = .5) => {
  a.flat(ell(x, y, 1), INK);
  a.flat(ell(x, y, .72), '#7d8598');
  a.flat(ell(x - .14, y - .16, .34), '#b0b8c8');
  const c = Math.cos(turn) * .62, s = Math.sin(turn) * .62;
  a.line(`M${x - c} ${y - s}L${x + c} ${y + s}`, { w: 1.1 });
};
/** A rubber bumper round a corner: its outline, where the light falls on it, and the wear only this corner has. */
const bumper = (a, pts, lite, shade, lines, wear) => {
  a.shape(poly(pts), RUBBER, { lite, shade, lines });
  if (wear) wear(a);
};
const pip = (a, x, on) => {
  a.flat(band(x - .3, 2.7, x + 2.3, 5), INK);
  a.flat(rr(x, 3, 2, 1.9, .25), on ? GO : GO_OFF);
  if (on) a.flat(band(x + .2, 3.15, x + 1.1, 3.6), '#c8f8d4');
};
const lamp = (a, x, y) => {
  a.flat(ell(x, y, 1.45), INK);
  a.flat(ell(x, y, 1.05), GO);
  a.flat(ell(x - .3, y - .32, .42), '#d4fbe0');
};

/** Lays a frame's runs and what stands over them on one picture, each run repeating from where the 9-slice cuts it, so a stretch finds it continuous. */
function lay(W, H, cut, runs, over) {
  const c = canvas(W * 4, H * 4), x = c.getContext('2d'), across = W - cut.l - cut.r, down = H - cut.t - cut.b;
  for (const { run, at, from, to } of runs) {
    x.save();
    x.beginPath();
    if (run.along === 'x') {
      x.rect(from * 4, at * 4, (to - from) * 4, run.used * 4);
      x.clip();
      for (let X = cut.l - across * Math.ceil(cut.l / across); X < W; X += across) x.drawImage(run.picture, X * 4, at * 4);
    } else {
      x.rect(at * 4, from * 4, run.used * 4, (to - from) * 4);
      x.clip();
      for (let Y = cut.t - down * Math.ceil(cut.t / down); Y < H; Y += down) x.drawImage(run.picture, at * 4, Y * 4);
    }
    x.restore();
  }
  x.drawImage(bake({ w: W, h: H, draw: over }), 0, 0);
  return c;
}

// The rails of the wrist computer: a thin pen's dark outside, then metal; the recess dark along its top and left, lit along its foot and right.
const topRail = (T, y, marks) => runX(T, [[y, y + 5, INK], [y + .5, y + 1.5, LIT], [y + 1.35, y + 3.9, BASE], [y + 3.15, y + 3.9, SHADE]], marks);
const footRail = (T, y, marks) => runX(T, [[y, y + 4, INK], [y, y + 1, LIP], [y + .95, y + 2, BASE], [y + 1.9, y + 3.35, SHADE]], marks);
const leftRail = (T, x, marks) => runY(T, [[x, x + 4, INK], [x + .5, x + 1.5, LIT], [x + 1.4, x + 3, BASE]], marks);
const rightRail = (T, x, marks) => runY(T, [[x, x + 4, INK], [x, x + 1, LIP], [x + .95, x + 2, BASE], [x + 1.9, x + 3.4, SHADE]], marks);

// ---------- the forearm display and its kin: bumpers at four corners, a top rail carrying what the device shows ----------
const cornerTL = (a, x0, wear) => bumper(a, [x0 + 1, .1, x0 + 7.9, .1, x0 + 8.1, 1, x0 + 8.1, 4, x0 + 4.1, 4.1, x0 + 4.1, 8.9, x0 + 1, 9, x0 + .1, 8, x0 + .1, 1], band(x0 + 1, .2, x0 + 7.8, 1.1), [band(x0 + 4.4, 3, x0 + 8.2, 4.2), band(x0 + 3, 4, x0 + 4.2, 9)], [`M${x0 + 1.6} 2.2L${x0 + 7.4} 2.3`, `M${x0 + 2.1} 2.4L${x0 + 2.2} 8`], wear);
const cornerTR = (a, W, wear) => bumper(a, [W - 8.1, .1, W - 1, .1, W - .1, 1, W - .1, 8, W - 1, 9, W - 4.1, 8.9, W - 4.1, 4.1, W - 8.1, 4, W - 8.1, 1], band(W - 7.8, .2, W - 1, 1.1), [band(W - 8.2, 3, W - 4.4, 4.2), band(W - 1.4, 1, W, 9)], [`M${W - 7.4} 2.3L${W - 1.6} 2.2`, `M${W - 2.2} 2.4L${W - 2.1} 8`], wear);
const cornerBL = (a, y0, wear) => bumper(a, [1, y0 + .1, 5, y0, 5.1, y0 + 4, 9.1, y0 + 5, 9.1, y0 + 8, 8, y0 + 8.9, 1, y0 + 8.9, .1, y0 + 8, .1, y0 + 1], band(.8, y0 + .1, 4.8, y0 + 1), [band(.1, y0 + 7.4, 9.1, y0 + 9), band(3.8, y0, 5.1, y0 + 4)], [`M2.1 ${y0 + 1.6}L2.2 ${y0 + 6}`, `M2.4 ${y0 + 6.3}L8.2 ${y0 + 6.2}`], wear);
const cornerBR = (a, W, y0, wear) => bumper(a, [W - 5, y0, W - 1, y0 + .1, W - .1, y0 + 1, W - .1, y0 + 8, W - 1, y0 + 8.9, W - 8, y0 + 8.9, W - 9.1, y0 + 8, W - 9.1, y0 + 5, W - 5.1, y0 + 4], band(W - 4.8, y0 + .1, W - .8, y0 + 1), [band(W - 9.1, y0 + 7.4, W, y0 + 9), band(W - 1.3, y0, W, y0 + 8.9)], [`M${W - 2.1} ${y0 + 1.6}L${W - 2.2} ${y0 + 6}`, `M${W - 8.2} ${y0 + 6.2}L${W - 2.4} ${y0 + 6.3}`], wear);

/** The display's top: hazard paint worn back, a screw each end, three signal pips of four, its lamp. */
const displayTop = (a) => {
  a.shape(band(8, 1.5, 16, 5), PLATE, { outer: false, lite: band(8, 1.5, 16, 2.5), shade: band(8, 4, 16, 5) });
  a.flat(band(15, 1.6, 51.4, 5.2), INK);
  const worn = [[0, null], [1, [18.6, 3.6]], [2, null], [3, [29.4, 3.2]], [4, null], [5, null], [6, [41, 3.8]], [7, [46.2, 2.8]], [8, null]];
  for (const [k, fleck] of worn) {
    const x = 16 + k * 3.95;
    a.flat(poly(x + 1.9, 2, x + 3.9, 2, x + 2, 4.9, x, 4.9), k % 3 === 1 ? '#b98526' : HAZ);
    if (fleck) a.flat(poly(fleck[0], fleck[1], fleck[0] + .9, fleck[1] - .1, fleck[0] + .6, fleck[1] + .6, fleck[0] - .1, fleck[1] + .5), INK);
  }
  screw(a, 11.2, 3.4, .9);
  rust(a, 12.8, 4.3, .7);
  screw(a, 100.6, 3.4, 2.1);
  for (const x of [81, 84, 87]) pip(a, x, true);
  pip(a, 90, false);
  lamp(a, 96, 4);
  a.line('M93.2 2.6L93.2 4.8', { w: .8 });
  chip(a, 69, 2.9, 4.4);
  a.flat(band(50.6, 2.2, 51.6, 4.8), SHADE);
};

function wristDisplay() {
  const W = 112, H = 26;
  const top = sheetX(24, 9, topRail(24, 1, (a) => quiet(a, 5, 2.6, 6)));
  const foot = sheetX(24, 9, footRail(24, 4, (a) => quiet(a, 13, 5.2, 5)));
  const left = sheetY(6, 8, leftRail(8, 1)), right = sheetY(6, 8, rightRail(8, 1, null));
  return lay(W, H, { l: 54, t: 9, r: 34, b: 9 }, [
    { run: left, at: 0, from: 5, to: 19 }, { run: right, at: 106, from: 5, to: 22 },
    { run: top, at: 0, from: 4, to: 108 }, { run: foot, at: 17, from: 4, to: 108 },
  ], (a) => {
    a.flat(band(106.1, 5, 107, 6), LIP);
    cornerTL(a, 0, (q) => { chip(q, 1.4, 1, 1.8, '#4c5066'); q.line('M5.6 3L7 3.5', { w: .8 }); });
    cornerTR(a, W, (q) => { chip(q, 108, 5.4, 1.2, '#4c5066'); });
    cornerBL(a, 17, (q) => { q.flat(poly(.1, 22.4, .9, 22.1, 1.2, 23.4, .1, 23.9), '#11121b'); });
    cornerBR(a, W, 17, (q) => { chip(q, 106, 23.5, 2, '#4c5066'); q.line('M109 19.6L109.6 21.2', { w: .8 }); });
    displayTop(a);
    chip(a, 96, 22.2, 2.4);
    rust(a, 10.4, 23.1, .8);
  });
}

function wristBody() {
  const W = 56, H = 26;
  const top = sheetX(24, 9, topRail(24, 1, (a) => quiet(a, 15, 2.8, 7)));
  const foot = sheetX(24, 9, footRail(24, 4, (a) => quiet(a, 7, 5.2, 6)));
  const left = sheetY(6, 8, leftRail(8, 1, null)), right = sheetY(6, 8, rightRail(8, 1));
  return lay(W, H, { l: 13, t: 9, r: 19, b: 9 }, [
    { run: left, at: 0, from: 5, to: 19 }, { run: right, at: 50, from: 5, to: 22 },
    { run: top, at: 0, from: 4, to: 52 }, { run: foot, at: 17, from: 4, to: 52 },
  ], (a) => {
    a.flat(band(50.1, 5, 51, 6), LIP);
    cornerTL(a, 0, (q) => { q.line('M2.4 5L3.2 6.6', { w: .8 }); });
    cornerTR(a, W, (q) => { chip(q, 49.6, 1, 2.2, '#4c5066'); });
    cornerBL(a, 17, (q) => { chip(q, 2.4, 24, 1.8, '#4c5066'); });
    cornerBR(a, W, 17, (q) => { q.flat(poly(55.9, 19, 55.1, 19.4, 55.2, 21, 55.9, 21.4), '#11121b'); });
    screw(a, 11.2, 3.4, 2.4);
    screw(a, 45, 3.4, .4);
    lamp(a, 40, 4);
    a.flat(band(35.5, 3, 37.8, 3.5), SHADE);
    chip(a, 45.5, 22.2, 2.6);
    rust(a, 46.6, 3.9, .55);
  });
}

function wristCuff() {
  const W = 43, H = 19;
  const top = sheetX(24, 7, runX(24, [[1, 4, INK], [1.55, 2.95, LIT], [2.5, 2.95, LIP]], null));
  const foot = sheetX(24, 4, runX(24, [[0, 3, INK], [0, 1, LIP], [.95, 2.3, SHADE]], null));
  const left = sheetY(4, 8, runY(8, [[1, 4, INK], [1.55, 2.95, LIT]]));
  const right = sheetY(4, 8, runY(8, [[0, 3, INK], [0, 1, LIP], [.95, 2.3, SHADE]]));
  return lay(W, H, { l: 7, t: 7, r: 12, b: 4 }, [
    { run: left, at: 0, from: 5, to: 18 }, { run: right, at: 39, from: 4, to: 18 },
    { run: top, at: 0, from: 5, to: 39 }, { run: foot, at: 15, from: 3, to: 40 },
  ], (a) => {
    bumper(a, [1, .1, 6, .1, 7.1, 1, 7.1, 4, 4.1, 4.1, 4.1, 6.9, 1, 7, .1, 6, .1, 1], band(1, .2, 6, 1.1), [band(4.4, 3, 7.1, 4.2), band(3, 4, 4.2, 7)], ['M1.6 2.2L6 2.3', 'M2 2.4L2.1 6']);
    bumper(a, [37, .1, 42, .1, 42.9, 1, 42.9, 6, 42, 7, 40.1, 6.9, 40.1, 4.1, 36, 4, 36, 1], band(37, .2, 42, 1.1), [band(36, 3, 39.8, 4.2), band(41.6, 1, 43, 7)], ['M37 2.3L41.4 2.2', 'M41.2 2.4L41.1 6'], (q) => { chip(q, 38, .9, 1.7, '#4c5066'); });
    a.flat(band(39, 4, 40, 7), LIP);
    // The status light, dim.
    a.flat(band(31.6, 1.7, 33.4, 3), INK);
    a.flat(rr(32, 2, 1, 1, .15), '#2f8050');
    a.flat(poly(1.2, 17, 2.2, 16.6, 3, 16.6, 3, 18, 2, 18), INK);
    a.flat(poly(40, 16.6, 41.9, 16.6, 41.9, 17, 41, 18, 40, 18), INK);
  });
}

function wristGauge() {
  const W = 32, H = 8;
  const top = sheetX(24, 3, runX(24, [[1, 3, INK], [1.5, 1.9, DEEP]], null, .05));
  const foot = sheetX(24, 4, runX(24, [[0, 3, INK], [0, 1, LIP], [.95, 2.3, SHADE]], null));
  const left = sheetY(4, 1, runY(1, [[1, 4, INK], [1.55, 2.95, LIT]]));
  const right = sheetY(4, 1, runY(1, [[0, 3, INK], [0, 1, LIP], [.95, 2.3, SHADE]]));
  return lay(W, H, { l: 4, t: 3, r: 4, b: 4 }, [
    { run: left, at: 0, from: 2, to: 6 }, { run: right, at: 28, from: 2, to: 6 },
    { run: top, at: 0, from: 1, to: 31 }, { run: foot, at: 4, from: 3, to: 29 },
  ], (a) => {
    a.flat(band(2, 2, 3, 4), LIT);
    a.flat(band(28, 2, 29, 3), LIP);
    a.flat(band(29, 2, 30, 3), LIT);
    a.flat(poly(1.2, 5, 3, 5, 3, 7, 2, 7), INK);
    a.flat(poly(29, 5, 30.9, 5, 30.8, 6, 30, 7, 29, 7), INK);
  });
}

// ---------- the tablet: a long bar over and under its screen, a dark armoured corner at each end ----------
const armour = (a, pts, lite, shade, ridges, wear) => {
  a.shape(poly(pts), RUBBER, { lite, shade, lines: ridges });
  if (wear) wear(a);
};
function tabletFrame() {
  const W = 345, H = 61;
  const top = sheetX(194, 21, runX(194, [[8, 16, INK], [8.5, 9.6, LIT], [9.5, 14.1, BASE], [13.1, 14.8, SHADE]], (a) => {
    chip(a, 12, 11.6, 2.2); dent(a, 21, 12, 3.1); chip(a, 28, 10.3, 1.4); a.line('M30 12.6L33.4 11.4', { w: .8 });
    dent(a, 58, 12.4, 1.6); chip(a, 89, 11, 3.4);
    rust(a, 90.4, 14, .9);
    a.line('M118 10.6L120.6 12', { w: .8 });
    chip(a, 141, 12.2, 1.2); dent(a, 164, 10.6, 3.2);
    chip(a, 177, 12.6, 1.6);
  }));
  const foot = sheetX(194, 14, runX(194, [[4, 14, INK], [4, 5, LIP], [4.95, 6, SHADE], [5.9, 12, BASE], [11.2, 13.3, SHADE]], (a) => {
    chip(a, 6, 7.2, 1.8); dent(a, 13, 9, 2.1); chip(a, 31, 7, 3); a.line('M33.4 8.4L37 9.8', { w: .8 });
    rust(a, 40, 12.2, 1);
    dent(a, 62, 9.4, 3.4); chip(a, 76, 8, 1.4); chip(a, 98, 8.8, 2.4);
    a.line('M110 7L111.6 10', { w: .8 });
    dent(a, 126, 8, 1.8); chip(a, 148, 9.8, 3.2);
    a.line('M163 9.6L167 8.4', { w: .8 });
    chip(a, 180, 7.4, 1.6);
  }));
  const left = sheetY(10, 26, runY(26, [[2, 9, INK], [2.5, 3.6, LIT], [3.5, 7.1, BASE], [6.3, 7.8, SHADE]], null));
  const right = sheetY(10, 26, runY(26, [[2, 9, INK], [2, 3, LIP], [2.9, 3.8, SHADE], [3.7, 7.1, BASE], [6.3, 7.8, SHADE]], null));
  return lay(W, H, { l: 74, t: 21, r: 77, b: 14 }, [
    { run: left, at: 0, from: 19, to: 49 }, { run: right, at: 326, from: 19, to: 49 },
    { run: top, at: 0, from: 12, to: 325 }, { run: foot, at: 47, from: 10, to: 326 },
  ], (a) => {
    const ridgesTL = ['M2.4 12.6L7.6 7.6', 'M2.6 16.2L11.4 7.8', 'M4.8 18L13.2 9.6', 'M8 18.4L13.4 13'];
    armour(a, [3, 7.1, 12.9, 7.1, 14, 8.1, 14, 14.2, 8.9, 19.4, 8.9, 20.1, 2.1, 20.1, 2.1, 19, 1.1, 18, 1.1, 9], band(3, 7.2, 13, 8.2), [poly(9, 19.5, 14, 14.4, 14, 13, 8, 19), band(2, 18.8, 9, 20.1)], ridgesTL, (q) => { q.flat(poly(1.2, 10, 2.2, 9.6, 2.4, 11.4, 1.2, 11.8), '#11121b'); });
    const ridgesTR = ['M325.6 8L331 13.2', 'M324.4 9.6L333.4 18.2', 'M327 8L334.4 15', 'M330.6 7.8L335 12'];
    armour(a, [324, 7.1, 334, 7.1, 335.9, 9, 335.9, 18, 334.9, 19, 334.9, 20.1, 328.1, 20.1, 328.1, 19.4, 323, 14.2, 323, 8.1], band(324, 7.2, 334, 8.2), [poly(323, 13, 323, 14.2, 328.1, 19.4, 329, 19), band(328, 18.8, 335, 20.1), band(334.6, 9, 336, 19)], ridgesTR, (q) => { chip(q, 331, 15.8, 1.8, '#4c5066'); });
    const ridgesBL = ['M2.6 52L7 56', 'M2 55.4L6.4 60', 'M4.4 50L14.4 60', 'M8 50.4L14.8 57'];
    armour(a, [2.1, 48, 9, 48, 9.1, 50, 15.1, 56, 15.1, 60.9, 2.6, 60.9, 1.1, 59.4, 1.1, 50, 2.1, 49], band(2.1, 48.1, 9, 49), [band(1, 59.4, 15.1, 61), band(14, 56, 15.1, 61)], ridgesBL, (q) => { chip(q, 2, 56.4, 1.4, '#4c5066'); });
    const ridgesBR = ['M334 52L329.6 56.4', 'M335 55.4L330 60.4', 'M332 49.6L322.6 59', 'M328.6 50L322.4 56.2'];
    armour(a, [328.1, 48, 334.9, 48, 335.9, 49, 335.9, 59.4, 334.5, 60.9, 323, 60.9, 322, 59.9, 322, 55, 328.1, 49.9], band(328.1, 48.1, 334.9, 49), [band(322, 59.4, 336, 61), band(334.6, 49, 336, 60)], ridgesBR, (q) => { q.flat(poly(335.9, 53, 335, 53.4, 335.1, 55, 335.9, 55.4), '#11121b'); q.line('M326 52.6L327.2 54', { w: .8 }); });
    chip(a, 4.4, 24, 1.5); dent(a, 4.2, 44, 1.6); a.line('M4.6 29L5.6 32', { w: .8 });
    chip(a, 330.6, 25, 1.3); rust(a, 331.2, 45.6, .8);
    // Its head: the plate and screw, hazard paint worn back.
    a.shape(band(14, 9.1, 22.4, 15), PLATE, { outer: false, edge: false, lite: band(14, 9.1, 22.4, 10), shade: band(14, 13.8, 22.4, 15) });
    screw(a, 18.4, 11.5, .8);
    rust(a, 20, 13, .7);
    a.flat(band(22.4, 8.9, 73.4, 15.2), INK);
    const flecks = [[], [[36, 11.6]], [], [[52, 10.8], [50.6, 12.6]], [], [[66.4, 10.2], [64, 12.6]]];
    for (let k = 0; k < 6; k++) {
      const x = 23 + k * 8.05, l = k === 4 ? 3.3 : 4;
      a.flat(poly(x + 3, 10, x + 3 + l, 10, x + l, 14, x, 14), k === 2 ? '#b98526' : HAZ);
      for (const [fx, fy] of flecks[k]) a.flat(poly(fx, fy, fx + 1.3, fy - .1, fx + .8, fy + .8, fx - .2, fy + .7), '#454a5e');
    }
    // Its foot of signal, the battery, the status lamp the game dims over, a screw.
    a.flat(band(269.4, 9.6, 277, 14.4), SHADE);
    for (const [x, h, c] of [[270, 1, GO], [272, 2, GO], [274, 3, GO], [276, 4, GO_OFF]]) a.flat(band(x, 14 - h, x + 1, 14), c);
    a.shape(band(282, 9.8, 296, 15), RUBBER, { outer: false, deep: band(282, 13.6, 296, 15) });
    a.flat(band(296, 11, 298, 14), INK);
    for (const [x, c] of [[283.2, GO], [286.2, GO], [289.2, GO], [292.2, GO_OFF]]) { a.flat(rr(x, 11, 2, 3, .2), c); if (c === GO) a.flat(band(x + .2, 11.15, x + 1.2, 11.6), '#c8f8d4'); }
    a.flat(rr(301.7, 10.7, 2.6, 2.6, .35), INK);
    a.flat(rr(302, 11, 2, 2, .25), GO);
    a.flat(band(302.2, 11.15, 303, 11.6), '#d4fbe0');
    screw(a, 318.4, 11.5, 2.3);
  });
}

const tabletPlate = (a) => {
  a.shape(band(.3, .3, 33.7, 5.7), { base: '#6f7789', shade: '#565d70', deep: '#444a5e', lite: '#8a92a6', spec: '#c4ccd8', ink: '#14162a' }, { lite: band(.5, .4, 33.5, 1.1), shade: band(.5, 4.6, 33.5, 5.7) });
  // Stamped, not written: its dashes are all that is left of a serial number.
  for (const [x0, y, x1] of [[5, 2.5, 12], [13.5, 2.5, 16], [18, 2.5, 19.8], [21.5, 2.5, 28]]) a.flat(band(x0, y, x1, y + .9), '#454a5e');
  a.flat(band(5, 3.6, 14.2, 4.2), '#9aa2b4');
  for (const x of [2.4, 31.6]) { a.flat(ell(x, 2.8, .75), '#2c3044'); a.flat(ell(x - .1, 2.65, .34), '#a8b0c0'); }
  a.line('M16 3.8L17 4.9', { w: .8 });
};

const tabletStylus = (a) => {
  const shaft = { base: '#9aa2b4', shade: '#5c6474', deep: '#444a5e', lite: '#d0d6e0', spec: '#ffffff', ink: '#14162a' };
  const cap = { base: '#c88a32', shade: '#8c5a24', deep: '#5c3a20', lite: '#e8ae58', spec: '#ffe0a8', ink: '#1c1210' };
  a.shape('M3.6 74L8 74L7.4 76L6.5 78L5.5 78L4.5 76Z', { ...DARK, lite: '#585c70' }, { shade: band(6, 74, 8, 78) });
  a.shape(rr(3.2, 6.6, 4.6, 67.8, .2), shaft, { lite: band(3.8, 6.6, 4.9, 74.4), shade: band(6.1, 6.6, 7.8, 74.4), lines: ['M4.9 31L4.9 44', 'M6 62L6 70'] });
  a.shape(rr(3.2, .4, 4.6, 5.8, .5), cap, { lite: band(3.8, .6, 4.8, 6), shade: band(6.2, .4, 7.8, 6.2), spec: band(4, 1, 4.5, 3.4) });
  a.flat(band(3.5, 6, 7.5, 7), '#3c4056');
  // The two clips that hold it to the frame.
  a.shape(rr(.2, 22.2, 8.6, 3.6, .3), PLATE, { lite: band(.5, 22.4, 8.5, 23.4), shade: band(.5, 24.2, 8.5, 25.8) });
  a.shape(rr(.2, 54.2, 8.6, 3.6, .3), PLATE, { lite: band(.5, 54.4, 8.5, 55.4), shade: band(.5, 56.2, 8.5, 57.8), lines: ['M1.6 55.8L3 57'] });
  chip(a, 6.4, 23.4, 1.5);
  rust(a, 1.8, 57, .55);
};

// ---------- the leftovers ----------
const glowDisc = (w, h, cx, cy, r, rgb, most) => {
  const c = canvas(w * 4, h * 4), x = c.getContext('2d'), g = x.createRadialGradient(cx * 4, cy * 4, 0, cx * 4, cy * 4, r * 4);
  g.addColorStop(0, `rgba(${rgb},${most})`);
  g.addColorStop(.35, `rgba(${rgb},${most * .42})`);
  g.addColorStop(.7, `rgba(${rgb},${most * .08})`);
  g.addColorStop(1, `rgba(${rgb},0)`);
  x.fillStyle = g;
  x.fillRect(0, 0, w * 4, h * 4);
  return c;
};
/** The lamp's glow, added round its glass: from the ink lantern's body outward, its middle clear, for the glass alight is drawn there. */
function lampGlow() {
  const c = canvas(32 * 4, 32 * 4), x = c.getContext('2d'), body = new Path2D(Anime.rr(11.6 * 4, 10.2 * 4, 8.8 * 4, 11 * 4, 2.6 * 4));
  x.filter = 'blur(7px)';
  x.fillStyle = 'rgba(136,203,255,.62)';
  x.fill(body);
  x.filter = 'blur(2.5px)';
  x.fillStyle = 'rgba(150,212,255,.34)';
  x.fill(body);
  x.filter = 'none';
  x.globalCompositeOperation = 'destination-out';
  x.fillStyle = '#000';
  x.fill(body);
  return c;
}

const emergencyLamp = (a) => {
  const LENS = { base: '#b82a22', shade: '#7e1a20', deep: '#52131a', lite: '#d84a38', spec: '#f4b8a4', ink: '#260a0e' };
  // Its bracket on the wall, the arm, the lens in a dark collar.
  a.shape(rr(14, 4, 2, 8, .2), GUN, { lite: band(14.1, 4.1, 14.7, 11.9), shade: band(15.3, 4, 16, 12) });
  for (const y of [5.2, 10.8]) { a.flat(ell(15, y, .36), '#262a42'); a.flat(ell(14.9, y - .1, .15), '#a7afbc'); }
  a.shape(rr(11, 7, 3.2, 2, .2), GUN, { lite: band(11, 7.1, 14.2, 7.7), shade: band(11, 8.3, 14.2, 9) });
  a.shape(rr(10.6, 6.4, 1.3, 3.2, .3), DARK, { outer: false, lite: band(10.7, 6.5, 11.8, 7) });
  a.shape(ell(8, 7.5, 3.9, 3.6), DARK, { shade: .5 });
  a.shape(ell(7.9, 7.5, 3.1, 2.85), LENS, { outer: false, shade: .75, deep: poly(8.6, 10.3, 11, 8.4, 11, 7.8, 9.8, 9.2), lite: ell(6.9, 6.4, 1.5, 1), spec: poly(5.9, 6, 6.8, 5.5, 7, 5.8, 6.1, 6.4) });
  // Its guard, one bent wire across the lens; a crack it did not stop.
  a.line('M5 5.8C7 7.2 9 7.6 10.9 6.8', { w: 1.3 });
  a.line('M8.8 9.2L9.5 8.4L9.3 7.8', { w: .7 });
};

/** The arc module's coil, four turns of blue wire over the stock, each at its own lean: each lies where the pixel one lay, its foot in the stock's shadow. */
const arcCoil = (a) => {
  const leans = [.06, -.05, .08, -.03];
  [0, 2, 4, 6].forEach((x, i) => {
    const l = leans[i];
    a.flat(poly(x + l, 0, x + 1 + l, 0, x + 1 - l, 3, x - l, 3), '#2496d8');
    a.flat(poly(x + .1 + l, 0, x + .5 + l, 0, x + .5 - l * .3, 2, x + .1 - l * .3, 2), '#8ad8ff');
    a.flat(poly(x - l * .3, 2.1, x + 1 - l * .3, 2.1, x + 1 - l, 3, x - l, 3), '#1a4c70');
    a.line(`M${x + .98 + l} .1L${x + .98 - l} 2.9`, { w: .7 });
  });
};

// The pod's states: over machine_dropPod in machines.js, where its porthole, glint and band's lamps are drawn there.
const podLit = (a) => {
  a.flat(poly(12, 12.4, 14.4, 10.6, 15, 11, 12.6, 13.2), '#eafcff');
  a.flat(poly(11.2, 14.6, 11.7, 13.8, 12.2, 14, 11.6, 15.6), '#b4e4f8');
  for (const x of [7, 14, 21, 25]) { a.flat(ell(x, 38, .55), '#14131c'); a.flat(ell(x, 38, .42), '#62e08c'); a.flat(ell(x - .1, 37.9, .16), '#d8ffe4'); }
};
const podPorthole = (a) => {
  const IN = { base: '#2c5e6c', shade: '#1f4554', deep: '#183642', lite: '#3a7280', spec: '#7cc0c0', ink: '#10242c' };
  a.shape(ell(16, 18.8, 7, 9.4), IN, { outer: false, edge: false, shade: band(6, 19.4, 26, 29), deep: band(6, 25.4, 26, 29), lite: poly(9.4, 15, 12, 11, 15, 10, 11.4, 16) });
};

Subjects.zleftovers = function () {
  // The shard's light, centred where the ink bench's arc burns: the bench's corner, less its margin, at BenchLook.LightAt.
  out('arcBench_glow', glowDisc(64, 64, 33.5, 30, 10.5, '180,150,255', .56));
  out('arcBench_fit_glow', glowDisc(64, 64, 33.5, 30, 16, '180,150,255', 1));
  out('machine_arcLamp_glow', lampGlow());
  out('machine_emergencyLamp', bake({ w: 16, h: 16, draw: emergencyLamp }));
  out('module_arc_coil', bake({ w: 7, h: 3, draw: arcCoil }));
  out('dropPod_lit', bake({ w: 32, h: 56, draw: podLit }));
  out('dropPod_porthole', bake({ w: 32, h: 56, draw: podPorthole }));
  out('wrist_display', wristDisplay());
  out('wrist_body', wristBody());
  out('wrist_cuff', wristCuff());
  out('wrist_gauge', wristGauge());
  out('tablet_frame', tabletFrame());
  out('tablet_plate', bake({ w: 34, h: 6, draw: tabletPlate }));
  out('tablet_stylus', bake({ w: 9, h: 78, draw: tabletStylus }));
};
})();

// The crew's shuttle, as picked: «Десант — Салазки» (G4 of the shuttle pick page, round six), a dropship of grey over dark gun-grey,
// a tinted canopy along its upper bow that nobody shows through, a wide sliding door in yellow edging, a far engine pod high and a near
// one low at the stern, two long skids on swinging links. The craft is 9 cells tall in its footprint of 24 × 10 (192 × 80): drawn in
// its own 72-tall frame and shifted 8 px down, so its skids stand on the platform at row 80 and the top 8 px stay empty.
// `machine_crewShuttle`, unlit; its fuel input at the stern and the fuel up its sight glass (`crewShuttle_level`, fuel-liquid);
// `crewShuttle_course`, its course lamp lit. Its canopy never changes, whoever sits behind it (the user's rule).
(function () {
'use strict';
const { ell, rr, poly, star } = Anime;

const STEEL = { base: '#a7afbc', shade: '#6a708c', deep: '#474c68', lite: '#d4dae2', spec: '#ffffff', rim: '#86d2e8' };
const GUN = { base: '#5c6474', shade: '#383c58', deep: '#262a42', lite: '#868fa2', spec: '#dfe7f2', rim: '#5fb2d0' };
const DARK = { base: '#2a2d3c', shade: '#1a1c2a', deep: '#12131e', lite: '#40445a', spec: '#a8b4c6', rim: '#4f9cbc' };
const LGREY = { base: '#b4b8bc', shade: '#868b94', deep: '#5e636e', lite: '#d4d8dc', spec: '#f4f6f8' };
const MGREY = { base: '#7e848e', shade: '#5a606c', deep: '#40454f', lite: '#9ea4ae', spec: '#e0e4ea' };
const INSET = { base: '#8d929b', shade: '#6d727c', deep: '#50555e', lite: '#a9adb4', spec: '#dfe3e8' };
const PIPE = { base: '#7f8796', shade: '#555b70', deep: '#3a3e50', lite: '#a9b1be', spec: '#dfe7f2' };
/** Tinted glass: dark enough that nothing behind it shows. */
const TINT = { base: '#262f3b', shade: '#1a212b', deep: '#10151c', lite: '#46566a', spec: '#d0dcea' };
const RUST = ['#70482f', '#3f2a24'];
const ON = { course: '#6cd8f4' };
const OFF = { green: '#2a4a36', amber: '#5e4a2c', red: '#5a2a28', course: '#23414e' };
/** Biofuel: «Ржавый оранжевый», the user's pick (fuel-liquid), as the fuel tank's. */
const FUEL = { base: '#b0501c', lite: '#f0965c', glint: '#d46a30' };

/** How far the craft's own frame sits down the picture's: its 72 rows end on the platform at row 80. */
const DROP = 8;
/** Round five's drawing, 80 tall, squashed to the craft's 72 (round six). */
const K9 = .9;

const f3 = (v) => +v.toFixed(3);
const band = (x0, y0, x1, y1) => poly(x0, y0, x1, y0, x1, y1, x0, y1);
const rivets = (a, ...pts) => { for (const [x, y] of pts) { a.flat(ell(x, y, .4), '#474c68'); a.flat(ell(x - .1, y - .12, .19), '#d4dae2'); } };
const row = (x0, x1, step, y) => { const o = []; for (let x = x0; x <= x1 + 1e-6; x += step) o.push([x, y]); return o; };
const hazard = (a, x, y, w, h) => {
  a.flat(band(x, y, x + w, y + h), '#f0c43c');
  for (let k = -h; k < w; k += h * 1.6) {
    a.flat(poly(x + Math.max(0, k), y + h, x + Math.min(w, k + h * .8), y + h, x + Math.min(w, k + h * 1.8), y, x + Math.min(w, k + h), y), '#1a1820', { clip: band(x, y, x + w, y + h) });
  }
};
/** Wear where water sat: a torn edge and a darker heart (credit.js). */
const wear = (a, x, y, s, [edge, heart]) => {
  a.flat(poly(x - s, y - .2 * s, x - .3 * s, y - .7 * s, x + .5 * s, y - .5 * s, x + s, y + .1 * s, x + .4 * s, y + .4 * s, x + .1 * s, y + .75 * s, x - .5 * s, y + .45 * s), edge);
  a.flat(poly(x - .4 * s, y - .1 * s, x + .1 * s, y - .3 * s, x + .45 * s, y + .05 * s, x, y + .3 * s), heart);
};
/** A lamp as built: a dark socket and its glass, unlit. */
const lampOff = (a, x, y, c, r) => { const q = r || .6; a.flat(ell(x, y, q), '#14131c'); a.flat(ell(x, y, q * .68), c); };
/** The same lamp lit, for an overlay. */
const lampOn = (a, x, y, c, r) => { const q = r || .6; a.flat(ell(x, y, q), '#14131c'); a.glow(ell(x, y, q * .72), c, { core: ell(x - q * .18, y - q * .2, q * .26) }); };
/** A slanted streak of reflection laid on glass, clipped to it: x at its top, its width, its lean over its height. */
const streak = (a, clip, x, y0, y1, w, lean, c) => a.flat(poly(x, y0, x + w, y0, x + w - lean, y1, x - lean, y1), c, { clip });
/** A stern bell opening astern (to the left) from its throat at x0 to its mouth at x1, centred on y. */
function bellAft(a, x0, x1, y, r0, r1, m) {
  m = m || STEEL;
  a.shape(poly(x0, y - r0, x0, y + r0, x1, y + r1, x1, y - r1), m, { lite: poly(x0, y - r0, x1, y - r1, x1, y - r1 + 1.4, x0, y - r0 + 1.2), shade: poly(x0, y + r0 - 1.4, x0, y + r0, x1, y + r1, x1, y + r1 - 2.2) });
  a.shape(ell(x1 + .2, y, .8, r1 - .5), DARK, { outer: false });
}
/** A recessed panel: its top in shadow, its bottom edge catching the light. */
const inset = (a, x, y, w, h, m) => a.shape(rr(x, y, w, h, .6), m || INSET, { outer: false, deep: band(x, y, x + w, y + .8), lite: band(x, y + h - .7, x + w, y + h) });
/** A row of belly armour plates, each riveted at its top corners. */
const armour = (a, x0, x1, y, h, step, m) => {
  for (let x = x0; x + step <= x1 + .01; x += step) {
    a.shape(rr(x + .3, y, step - .6, h, .6), m || MGREY, { lite: band(x, y, x + step, y + .8), shade: band(x, y + h - 1, x + step, y + h) });
    rivets(a, [x + 1.6, y + 1.6], [x + step - 1.8, y + 1.6]);
  }
};
/** A chunky thruster block: two dark ports. */
const bigRcs = (a, x, y) => {
  a.shape(rr(x - 3.2, y - 2.4, 6.4, 4.8, .8), GUN, { lite: band(x - 3.2, y - 2.4, x + 3.2, y - 1.6), shade: band(x - 3.2, y + 1.6, x + 3.2, y + 2.4) });
  for (const dx of [-1.6, 1.6]) a.flat(ell(x + dx, y + .4, .8), '#14131c');
};
/** The dark canopy: one tinted body, its mullions, hard streaks of sheen and a glint. Never clear. */
function canopy(a, d, [x0, y0, x1, y1], mullions) {
  a.shape(d, TINT, { lite: band(x0 - 2, y1 - 1.4, x1 + 2, y1 + 2), deep: band(x0 - 2, y0 - 2, x1 + 2, y0 + .9), lines: (mullions || []).map((x) => `M${x} ${y0 - 2}L${x} ${y1 + 2}`) });
  const W = x1 - x0;
  streak(a, d, x0 + W * .28, y0 - 2, y1 + 2, Math.max(1.2, W * .07), 3, '#3e4c60');
  streak(a, d, x0 + W * .4, y0 - 2, y1 + 2, .8, 3, '#3e4c60');
  streak(a, d, x0 + W * .68, y0 - 2, y1 + 2, Math.max(1, W * .05), 3, '#56687e');
  a.flat(star(x0 + W * .6, y0 + (y1 - y0) * .32, 1.3, .2), '#dfe8f2', { clip: d });
}
/** A path's y scaled by k about the top: every number pair is x y, as rr, ell and poly write them. */
const scaleD = (d, k) => { let i = 0; return d.replace(/-?\d*\.?\d+(?:e-?\d+)?/g, (m) => (i++ % 2 ? String(f3(+m * k)) : m)); };
/** A recorder that squashes everything drawn through it by k in height; widths stay. */
function scaled(a, k) {
  const S = (d) => (typeof d === 'string' ? scaleD(d, k) : d);
  const SO = (o) => { if (!o) return o; const r = { ...o }; for (const key of ['lite', 'shade', 'deep', 'spec', 'lines', 'clip', 'core']) if (key in r) r[key] = Array.isArray(r[key]) ? r[key].map(S) : S(r[key]); return r; };
  const b = {
    shape: (d, m, o) => (a.shape(S(d), m, SO(o)), b), line: (d, o) => (a.line(S(d), o), b), flat: (d, c, o) => (a.flat(S(d), c, SO(o)), b),
    tube: (d, w, m, o) => (a.tube(S(d), w, m, o), b), glow: (d, c, o) => (a.glow(S(d), c, SO(o)), b), cut: (d, o) => (a.cut(S(d), o), b),
  };
  return b;
}
const P2 = (p) => `${f3(p[0])} ${f3(p[1])}`;
const bar = (a, p, q, w, m) => a.tube(`M${P2(p)}L${P2(q)}`, w, m);
const joint = (a, p, r, m) => a.shape(ell(p[0], p[1], r), m || DARK, {});

// ---------- the hull, in round five's 80-tall drawing, squashed through scaled() ----------
const hull = 'M16 14L150 14Q170 14 184 26L190 42L186 62Q182 70 170 70L16 70Z';
const topCanopy = 'M142 15.6L150 15.6Q168 15.6 182 26.8L187.6 38.6L174 38.6Q166 28 148 24.4L142 24.4Z';
function desant(a) {
  // The far pod, behind and high; the stern's own small nozzle.
  a.tube('M40 16L34 13', 4, GUN);
  a.shape(rr(6, 6, 50, 13, 6.5), GUN, { lite: band(6, 6, 56, 7.2), shade: band(6, 16.4, 56, 19) });
  bellAft(a, 7, 2, 12.5, 4.2, 5.4, MGREY);
  bellAft(a, 16.6, 11, 38, 4, 5.4);
  // The hull: grey over dark gun-grey, a big sliding door, armour on the belly.
  a.shape(hull, MGREY, { lite: band(16, 14, 160, 15.4), spec: band(30, 14.8, 140, 15.3), shade: ['M180 22L194 36L194 72L174 72Z'] });
  a.flat(band(0, 44, 192, 72), GUN.base, { clip: hull });
  a.flat(band(0, 44, 192, 44.8), GUN.lite, { clip: hull });
  a.flat(band(0, 64, 192, 72), GUN.shade, { clip: hull });
  for (const d of ['M16.4 44L188 44', 'M60 14.4L60 69.6', 'M140 14.4L140 69.6']) a.line(d);
  a.shape(rr(78, 22, 44, 40, 1.2), DARK, { outer: false, lite: band(78, 22, 122, 22.8), lines: ['M100 22.4L100 61.6', 'M78.4 42L121.6 42'] });
  a.tube('M74 21L126 21', 1.2, STEEL);
  hazard(a, 78, 62.4, 44, 1.6);
  for (const x of [79, 121]) a.flat(band(x - .7, 22, x + .7, 62), '#d8ac3a');
  inset(a, 126, 20, 12, 12, LGREY); inset(a, 24, 48, 30, 10, MGREY);
  inset(a, 152, 44, 26, 12, MGREY);
  a.shape(ell(183.6, 52, 1.8, 3.6), DARK, { outer: false });
  armour(a, 126, 156, 62, 7.4, 10, DARK);
  bigRcs(a, 30, 14); bigRcs(a, 130, 14); bigRcs(a, 180, 66);
  // The canopy along the upper bow, its mullions parting the four seats' panes.
  canopy(a, topCanopy, [142, 15.6, 187.6, 38.6], [156, 166, 176]);
  a.tube('M140 14L140 9.6', 1, GUN);
  // The near pod, low, on its pylon.
  a.shape(poly(40, 62, 60, 62, 56, 54, 44, 54), LGREY, { lite: band(40, 54, 60, 55) });
  a.shape(rr(4, 52, 54, 16, 8), LGREY, { lite: band(4, 52, 58, 53.6), shade: [band(4, 64.4, 58, 68), band(54, 52, 58, 68)], lines: ['M22 52.4L22 67.6', 'M42 52.4L42 67.6'] });
  hazard(a, 26, 53.4, 12, 1.4);
  a.shape(ell(56, 60, 1.6, 5.2), DARK, { outer: false });
  // Its mouth kept off the picture's edge: the fuel port's fitting alone meets it there.
  bellAft(a, 6.4, 2.8, 60, 5.6, 7);
  rivets(a, ...row(64, 176, 8, 46.6));
  wear(a, 60, 40, 1.1, RUST); wear(a, 140, 56, 1.2, RUST); wear(a, 100, 66, 1, RUST); wear(a, 70, 18, .8, RUST); wear(a, 160, 30, 1, RUST);
  a.line('M150 40L152.4 42.6');
}

// ---------- from here on, the craft's own 72-tall frame ----------
/** The course lamp on the mast over the canopy's back: x, y, radius (shuttle-flight). */
const COURSE = [140, 7.6, 1.6];
/** Its running lamps, unlit: red aft, green at the nose, amber on the belly. */
const LAMPS = [[46, 16.4, OFF.red, .8], [186.2, 41.4, OFF.green, .7], [140, 53.6, OFF.amber, .7]];

/** The sight glass's liquid on the stern quarter: the empty glass is in the body, its fuel drawn over it (crewShuttle_level). */
const GLASS = [27, 18.4, 4, 20.2];
function glassBody(a) {
  a.shape(rr(24.6, 16.4, 9, 24.6, 1.4), GUN, { lite: band(24.6, 16.4, 33.6, 17.2), shade: band(31.8, 16.4, 33.6, 41) });
  a.shape(rr(26.6, 18, 4.8, 21, 1.8), DARK, { outer: false });
  a.flat(rr(...GLASS, .8), '#26363a');
  for (let k = 0; k <= 4; k++) a.line(`M34 ${f3(18.4 + k * 5.05)}L36.4 ${f3(18.4 + k * 5.05)}`, { w: .8 });
  rivets(a, [29, 17.2], [29, 40.2]);
  // Its filler cap beside it, under a mark of the fuel's colour.
  a.shape(ell(43, 21, 2.8), GUN, { lite: ell(42.5, 20.4, 2) });
  a.flat(band(41.4, 20.6, 44.6, 21.4), '#1a1820');
  a.flat(rr(39.6, 25, 6.8, 2, .4), FUEL.base);
}
/** The glass full of biofuel, under its surface (sight-glass): the client shows as much of it as the tank holds. */
const level = (a) => { const [x, y, w, h] = GLASS; a.flat(rr(x, y, w, h, .8), FUEL.base); a.flat(band(x + .4, y, x + 1.4, y + h), FUEL.glint); };
const levelTop = (a) => { const [x, y, w] = GLASS; a.flat(band(x, y, x + w, y + .6), FUEL.lite); };

/**
 * Its fuel input (fuel-liquid), its port fixed at cell 0, 8 (pixels 0–8 × 64–72 of the picture, 56–64 of the craft): a bolted plate on
 * the port's middle (4, 60) under the near pod's bell, its pipe running in along the pod's belly and up into the hull.
 */
function fuelPipe(a) {
  a.tube('M6 62.6L27 62.6Q30 62.6 30 59.8', 2.2, PIPE);
  a.shape(rr(26, 58.6, 5.4, 2.4, .6), GUN, { lite: band(26, 58.6, 31.4, 59.2) });
}
function fuelPlate(a) {
  // Flush with the picture's edge, where a pipe laid to the port meets it.
  a.shape(rr(0, 56.9, 7.1, 6.2, .4), GUN, { lite: band(0, 56.9, 7.1, 57.6), shade: band(0, 62.3, 7.1, 63.1) });
  a.flat(ell(4, 60, 2), '#14151c');
  a.flat(ell(4, 60, 1.3), PIPE.shade);
  a.flat(ell(3.8, 59.75, .65), PIPE.base);
  rivets(a, [1.6, 57.6], [6.4, 57.6], [1.6, 62.4], [6.4, 62.4]);
}

/**
 * The skids on their swinging links, in three frames: 0 down, 1 half, 2 up. Only frame 0 is drawn (into the body): the craft has no
 * launch in the world yet, so the others are kept for when it does.
 */
function skids(a, f) {
  const E = [[-3, 70], [-6, 67.6], [-8.4, 64.4]][f], sy = [69.6, 67.2, 64][f], dx = [0, -3, -5.4][f];
  for (const [x0, x1] of [[34, 100], [112, 178]]) {
    for (const hx of [x0 + 12, x1 - 14]) { bar(a, [hx, 62.6], [hx + E[0], E[1]], 2.2, GUN); joint(a, [hx, 62.6], 1.4); }
    const X0 = x0 + dx, X1 = x1 + dx;
    a.shape(rr(X0, sy, X1 - X0, 2.4, 1), DARK, { lite: band(X0, sy, X1, sy + .6) });
    a.tube(`M${f3(X1 - 1.2)} ${f3(sy + 1.2)}Q${f3(X1 + 3.4)} ${f3(sy + 1.2)} ${f3(X1 + 4.4)} ${f3(sy - 2.6)}`, 1.8, DARK);
    for (const hx of [x0 + 12, x1 - 14]) joint(a, [hx + E[0], E[1]], 1.2, GUN);
  }
}

function shuttle(a) {
  desant(scaled(a, K9));
  glassBody(a);
  skids(a, 0);
  fuelPipe(a);
  for (const [x, y, c, r] of LAMPS) lampOff(a, x, y, c, r);
  lampOff(a, COURSE[0], COURSE[1], OFF.course, COURSE[2]);
  fuelPlate(a);
}

/** A picture of the footprint, the craft's frame dropped onto the platform. */
const picture = (draw) => Anime.bake({ w: 192, h: 80, shift: [0, DROP], draw });

Subjects.shuttle = function () {
  out('machine_crewShuttle', picture(shuttle));
  out('crewShuttle_level', picture(level));
  out('crewShuttle_level_top', picture(levelTop));
  out('crewShuttle_course', picture((a) => lampOn(a, COURSE[0], COURSE[1], ON.course, COURSE[2])));
};
})();

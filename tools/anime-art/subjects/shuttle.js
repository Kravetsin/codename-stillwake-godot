// The crew's shuttle, as picked: «Ледокол» (H of the shuttle pick page), a heavy prow-shaped hull of worn red plates over a grey
// ice belt, a row of four frosted slits nobody shows through, a stack of three stubby thrusters at the stern, standing on two keel
// blocks on its launch platform. `machine_crewShuttle`, 192 × 80, its footprint of 24 × 10 cells; built only, unlit and empty.
(function () {
'use strict';
const { ell, rr, poly } = Anime;

const STEEL = { base: '#a7afbc', shade: '#6a708c', deep: '#474c68', lite: '#d4dae2', spec: '#ffffff', rim: '#86d2e8' };
const GUN = { base: '#5c6474', shade: '#383c58', deep: '#262a42', lite: '#868fa2', spec: '#dfe7f2', rim: '#5fb2d0' };
const DARK = { base: '#2a2d3c', shade: '#1a1c2a', deep: '#12131e', lite: '#40445a', spec: '#a8b4c6', rim: '#4f9cbc' };
const OXRED = { base: '#7e5040', shade: '#583630', deep: '#3e2624', lite: '#a06e58', spec: '#d6a88c' };
/** Frosted glass: milky and deep enough that nothing behind it shows, its sheen laid on in hard steps. */
const FROST = { base: '#76868f', shade: '#56646e', deep: '#3c4852', lite: '#a2b2ba', spec: '#e6eef0' };
const RUST = ['#70482f', '#3f2a24'];
const OFF = { green: '#2a4a36', red: '#5a2a28', white: '#5e5c52' };

const band = (x0, y0, x1, y1) => poly(x0, y0, x1, y0, x1, y1, x0, y1);
function box(a, x, y, w, h, m, o) {
  o = o || {};
  const s = Math.min(1.25, w * .12), b = Math.min(1.4, h * .14), l = Math.min(.8, h * .2);
  a.shape(rr(x, y, w, h, .4), m, { outer: o.outer, lite: band(x, y, x + w, y + l), shade: [band(x + w - s, y, x + w, y + h), band(x, y + h - b, x + w, y + h)] });
}
const rivets = (a, ...pts) => { for (const [x, y] of pts) { a.flat(ell(x, y, .4), '#474c68'); a.flat(ell(x - .1, y - .12, .19), '#d4dae2'); } };
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
/** A slanted streak of reflection laid on glass, clipped to it. */
const streak = (a, clip, x, y0, y1, w, lean, c) => a.flat(poly(x, y0, x + w, y0, x + w - lean, y1, x - lean, y1), c, { clip });

const hull = 'M14 18L130 16.4Q166 15.4 190 8L183 30L167 63Q164 68 156 68L24 68Q15.6 68 15 62L12.6 22Q12.6 18 14 18Z';
/** The prow's x at a row: raked back to its foot. */
const prowX = (y) => (y < 30 ? 190 - (y - 8) * 7 / 22 : 183 - (y - 30) * 16 / 33);
/** The four slits' left edges, one a seat. */
const SLITS = [99.4, 115.8, 132.2, 148.6];

function frostSlit(a, x) {
  const d = rr(x, 23.6, 13, 4.6, 1.6);
  a.shape(d, FROST, { outer: false, deep: band(x, 23.6, x + 13, 24.6), lite: band(x, 26.8, x + 13, 28.2) });
  streak(a, d, x + 3.4, 23.6, 28.2, 2, 1.6, '#8e9ea8');
  streak(a, d, x + 6.6, 23.6, 28.2, .7, 1.6, '#8e9ea8');
  a.flat(ell(x + 2.6, 25.4, .45), '#e6eef0');
}

function shuttle(a) {
  // The keel blocks it stands on, their feet on the platform.
  for (const x of [36, 122]) {
    a.shape(poly(x, 66, x + 24, 66, x + 21, 80, x + 3, 80), GUN, { lite: band(x, 66, x + 24, 67.4), shade: band(x + 18, 66, x + 25, 80), deep: band(x, 78.6, x + 25, 80) });
    hazard(a, x + 4, 73, 16, 1.6);
  }
  // The mast and its searchlight.
  a.tube('M150 15.6L150 7', 1.4, GUN);
  a.shape(rr(145.4, 3, 8.6, 4.8, 1.2), DARK, { lite: band(145.4, 3, 154, 3.8) });
  a.flat(ell(154, 5.4, .9, 1.9), OFF.white);
  // The hull: heavy plates of worn red over a grey ice belt, riveted seams.
  a.shape(hull, OXRED, {
    lite: 'M14 18L130 16.4Q166 15.4 190 8L189 11.2Q166 18.6 130 19.4L14 21Z', spec: band(20, 18.4, 110, 19),
    shade: ['M178 0L192 0L192 70L156 70Z', band(12, 18, 15.6, 70)],
    lines: ['M12 32L184 32', 'M44 17.6L44 32', 'M90 17L90 32', 'M136 16.4L136 32', 'M30 32L30 48', 'M70 32L70 48', 'M116 32L116 48', 'M156 32L156 48'] });
  a.flat(band(0, 48, 192, 70), '#5c6474', { clip: hull });
  a.flat(band(0, 48, 192, 49.2), '#868fa2', { clip: hull });
  a.flat(band(0, 63.6, 192, 70), '#383c58', { clip: hull });
  a.flat('M172 47L192 47L192 70L160 70Z', '#40465a', { clip: hull });
  a.line('M14 48.2L175 48.2', { w: 1.4 });
  for (const x of [50, 98, 140]) a.line(`M${x} 49L${x} 67`);
  for (const y of [30.4, 33.6, 46.6, 50]) rivets(a, ...Array.from({ length: 26 }, (_, k) => [18 + k * 6.4, y]).filter(([x]) => x < prowX(y) - 3));
  // The slits: four frosted windows in an armoured band.
  a.shape(rr(96, 21.6, 69, 8.6, 2), DARK, { outer: false, lite: band(96, 21.6, 165, 22.4) });
  for (const x of SLITS) frostSlit(a, x);
  // The stack of stubby thrusters at the stern.
  for (const y of [27, 41, 55]) {
    a.shape(rr(9, y - 5.6, 7, 11.2, 1.6), GUN, { lite: band(9, y - 5.6, 16, y - 4.4), shade: band(9, y + 4, 16, y + 5.6) });
    a.shape(poly(10, y - 4.4, 10, y + 4.4, 2.6, y + 6, 2.6, y - 6), STEEL, { lite: poly(10, y - 4.4, 2.6, y - 6, 2.6, y - 4.2, 10, y - 2.8), shade: band(2, y + 3, 11, y + 7) });
    a.shape(ell(2.8, y, 1, 5.6), DARK, { outer: false });
  }
  // The feed slot on the stern quarter, its lamp and the sight glass, empty.
  box(a, 20, 35, 14, 10, GUN, { outer: false });
  a.shape(rr(22, 37.2, 10, 2.4, .5), DARK, { outer: false, deep: band(22, 37.2, 32, 38) });
  lampOff(a, 27, 42.2, OFF.green, .9);
  a.shape(rr(36.6, 34, 4.6, 12.6, 1.4), DARK, { outer: false });
  a.flat(rr(37.5, 35, 2.8, 10.6, .8), '#26363a');
  for (const y of [37.6, 40.2, 42.8]) a.flat(band(36.6, y, 37.6, y + .3), '#a7afbc');
  lampOff(a, 20.4, 21.8, OFF.red, .9);
  lampOff(a, 182.8, 13.4, OFF.green, .9);
  wear(a, 44, 31.4, 1.2, RUST); wear(a, 116, 33, 1, RUST); wear(a, 70, 48.6, 1.1, RUST); wear(a, 160, 30.6, .9, RUST); wear(a, 26, 64, .9, RUST);
  a.line('M60 40L62.6 42.4'); a.line('M134 56L136 59');
}

Subjects.shuttle = function () {
  out('machine_crewShuttle', Anime.bake({ w: 192, h: 80, draw: shuttle }));
};
})();

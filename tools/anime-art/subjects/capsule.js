// The trade capsule, as picked (trade-capsule-ink): a squat freight drum on two swept fins, a keep-clear stripe, an amber lamp and
// the eye it is hoisted by. `capsule`, 18 × 26, at its size in the world by its station; `capsule_flame_<n>`, 9 × 12, three frames of
// its ink flame, whose top middle hangs at the drum's bell, (9, 24.2) of the capsule (CapsuleFlights.Nozzle).
(function () {
'use strict';
const { ell, rr, poly } = Anime;

const WORN = { base: '#8e96a3', shade: '#5f6577', deep: '#40445a', lite: '#b4bbc6', spec: '#e6eaf0' };
const GUN = { base: '#5c6474', shade: '#383c58', deep: '#262a42', lite: '#868fa2', spec: '#dfe7f2' };
const DARK = { base: '#2a2d3c', shade: '#1a1c2a', deep: '#12131e', lite: '#40445a', spec: '#a8b4c6' };
const RUST = ['#70482f', '#3f2a24'];
const AMBER = '#f0b44c';
/** The fire's hard steps, outside in: the furnace's (machines.js), the game's old #ffb066 in the middle. */
const FIRE = ['#c63a14', '#f06a22', '#ffb066', '#fff4c8'];

const f3 = (v) => +v.toFixed(3);
const band = (x0, y0, x1, y1) => poly(x0, y0, x1, y0, x1, y1, x0, y1);
const rivets = (a, ...pts) => { for (const [x, y] of pts) { a.flat(ell(x, y, .38), '#474c68'); a.flat(ell(x - .09, y - .11, .18), '#d4dae2'); } };
const led = (a, x, y, c, r) => { const q = r || .55; a.flat(ell(x, y, q), '#14131c'); a.glow(ell(x, y, q * .66), c); };
const hazard = (a, x, y, w, h) => {
  a.flat(band(x, y, x + w, y + h), '#f0c43c');
  for (let k = -h; k < w; k += h * 1.6) {
    a.flat(poly(x + Math.max(0, k), y + h, x + Math.min(w, k + h * .8), y + h, x + Math.min(w, k + h * 1.8), y, x + Math.min(w, k + h), y), '#1a1820',
      { clip: band(x, y, x + w, y + h) });
  }
};
/** Wear where water sat: a torn edge and a darker heart (credit.js). */
const wear = (a, x, y, s, [edge, heart]) => {
  a.flat(poly(x - s, y - .2 * s, x - .3 * s, y - .7 * s, x + .5 * s, y - .5 * s, x + s, y + .1 * s, x + .4 * s, y + .4 * s, x + .1 * s, y + .75 * s, x - .5 * s, y + .45 * s), edge);
  a.flat(poly(x - .4 * s, y - .1 * s, x + .1 * s, y - .3 * s, x + .45 * s, y + .05 * s, x, y + .3 * s), heart);
};
/** A tongue of fire hanging from (x, y): four hard steps of tone, its tip swayed by `sway`; no glow painted round it. */
function tongue(a, x, y, w, len, sway) {
  [[1, 1], [.72, .82], [.46, .6], [.22, .34]].forEach(([kw, kl], k) => {
    const ww = w * kw / 2, l = len * kl, tip = x + sway * kl;
    a.flat(`M${f3(x - ww)} ${f3(y)}Q${f3(x - ww)} ${f3(y + l * .55)} ${f3(tip)} ${f3(y + l)}Q${f3(x + ww)} ${f3(y + l * .55)} ${f3(x + ww)} ${f3(y)}Z`, FIRE[k]);
  });
}

function canister(a) {
  for (const s of [-1, 1]) {
    const x = 9 + s * 5.8, o = 9 + s * 8.4;
    a.shape(poly(x, 15.6, x, 21.4, o, 25.2, o, 22.4), GUN, { shade: s > 0 ? band(x, 15, o + .5, 26) : band(o - .5, 23.6, x, 26) });
  }
  a.shape(poly(6.8, 21, 11.2, 21, 12.4, 24.2, 5.6, 24.2), DARK, { lite: band(5.6, 21, 7.4, 24.2), deep: band(5.6, 23.5, 12.4, 24.2) });
  // The drum, its two hoops, the stripe that says keep clear; the lid and the eye it is hoisted by.
  a.shape(rr(2.6, 5, 12.8, 16.4, 1), WORN, { lite: band(2.6, 5, 5.6, 21.4), spec: band(3.8, 6.4, 4.5, 19.6), shade: band(12.4, 5, 15.4, 21.4), deep: band(2.6, 20.3, 15.4, 21.4), lines: ['M9 10.4L9 15.6'] });
  hazard(a, 3, 18, 12, 1.5);
  for (const y of [8.4, 15.8]) {
    a.shape(rr(2, y, 14, 1.6, .4), GUN, { lite: band(2, y, 16, y + .5), shade: band(14.6, y, 16, y + 1.6) });
    rivets(a, [4, y + .85], [9, y + .85], [14, y + .85]);
  }
  a.shape(ell(9, 1.4, 1.1, .9), GUN, {});
  a.cut(ell(9, 1.4, .45, .35));
  a.shape('M3.4 5.2C3.8 2.8 6.2 2 9 2C11.8 2 14.2 2.8 14.6 5.2Z', GUN, { lite: 'M4.6 4.4C5.2 3.1 6.8 2.6 9 2.6C7.2 3 6.2 3.6 5.7 4.6Z', shade: band(12.2, 1.8, 15, 5.4) });
  led(a, 12.4, 6.7, AMBER);
  wear(a, 4.8, 17.3, .7, RUST);
  wear(a, 13.4, 11, .5, RUST);
  a.line('M6 12.4L7.2 13.2');
  a.line('M15.4 6.2L15.4 20.4', { w: 2 });
}

const flame = (f) => (a) => tongue(a, 4.5, 0, 5.2, [10.4, 8.6, 9.6][f], [0, -.5, .4][f]);

Subjects.capsule = function () {
  out('capsule', Anime.bake({ w: 18, h: 26, draw: canister }));
  for (let f = 0; f < 3; f++) out(`capsule_flame_${f}`, Anime.bake({ w: 9, h: 12, draw: flame(f) }));
};
})();

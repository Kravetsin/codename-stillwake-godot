// The hold's piles (2026-10-31-hold-piles), the pixel heaps' pieces placed by hand: packed, torn at their edges, the shape of the heap made by what lies on it. Flat on the floor.
(function () {
'use strict';
const { ell, poly, bake, rng } = Anime;
const SOOT = { base: '#20222b', shade: '#16171f', deep: '#101118', lite: '#2e313c', spec: '#3a3d48', ink: '#0c0d13' };
const HULL = { base: '#566b80', shade: '#3a4a60', deep: '#293548', lite: '#7c92a6', spec: '#a8bac8', ink: '#0e1018' };
const PLATE = { base: '#868d99', shade: '#585e72', deep: '#3e4256', lite: '#a8aeb8', spec: '#ccd0d6', ink: '#0e1018' };
const OLD = { base: '#61646c', shade: '#44464f', deep: '#30323a', lite: '#83858a', spec: '#a2a3a6', ink: '#0e1018' };
const PLANK = { base: '#75553b', shade: '#50392b', deep: '#382821', lite: '#957350', spec: '#ad8c68', ink: '#16100c' };
const RUST = { base: '#7c4e34', shade: '#553425', deep: '#3a241c', lite: '#986644', spec: '#ae8058', ink: '#16100c' };
const STRIPE = ['#ab8b36', '#8c7230'];

/** A torn plate lying turned by t: its edges kinked, lit along its upper edge, shaded along its foot and far side. */
function piece(a, r, cx, cy, w, h, t, m, tear) {
  const c = Math.cos(t), s = Math.sin(t), at = (u, v) => [cx + u * c - v * s, cy + u * s + v * c], k = tear ?? .3 * Math.min(w, h), pts = [], E = [[-w / 2, -h / 2], [w / 2, -h / 2], [w / 2, h / 2], [-w / 2, h / 2]];
  E.forEach(([u0, v0], i) => {
    const [u1, v1] = E[(i + 1) % 4], L = Math.hypot(u1 - u0, v1 - v0), n = Math.max(1, Math.round(L / 3.4)), nu = (v1 - v0) / L, nv = -(u1 - u0) / L;
    // A corner chipped in along both edges; a side's teeth go in and out alike, so no piece pinches at its waist.
    pts.push(...at(u0 + (r() - .5) * k * .9 * Math.sign(-u0), v0 + r() * k * .7 * Math.sign(-v0)));
    for (let j = 1; j < n; j++) { const q = (j + (r() - .5) * .5) / n, o = (r() - .5) * k; pts.push(...at(u0 + (u1 - u0) * q + nu * o, v0 + (v1 - v0) * q + nv * o)); }
  });
  const l = Math.min(.7, h * .2), b = Math.min(1.1, h * .24), e = Math.min(.9, w * .14);
  a.shape(poly(...pts), m, {
    lite: poly(...at(-w / 2, -h / 2), ...at(w / 2, -h / 2), ...at(w / 2, -h / 2 + k * .4 + l), ...at(-w / 2, -h / 2 + k * .4 + l)),
    shade: [poly(...at(-w / 2, h / 2 - b), ...at(w / 2, h / 2 - b), ...at(w / 2, h / 2), ...at(-w / 2, h / 2)), poly(...at(w / 2 - e, -h / 2), ...at(w / 2, -h / 2), ...at(w / 2, h / 2), ...at(w / 2 - e, h / 2))],
  });
  return { at, d: poly(...pts) };
}
/** The dark under the pieces: a heap's shape, its top broken, wide on the floor. */
const hill = (a, ...pts) => a.shape(poly(...pts), SOOT, {});
const laid = (a, r, list) => list.map(([x, y, w, h, t, m, tear]) => piece(a, r, x, y, w, h, t, m, tear));
const rivetsOn = (a, p, ...pts) => { for (const [u, v] of pts) { const [x, y] = p.at(u, v); a.flat(ell(x, y, .3), '#2c2f3e'); a.flat(ell(x - .07, y - .09, .13), '#bcc2ca'); } };
/** Hazard paint on a piece, most of it gone: slanting bands broken by the bare metal. */
function hazardOn(a, p, h, bands) {
  for (const [u0, u1, v0, v1, tone] of bands) a.flat(poly(...p.at(u0, v1), ...p.at(u1, v1), ...p.at(u1 + (v1 - v0) * .7, v0), ...p.at(u0 + (v1 - v0) * .7, v0)), STRIPE[tone], { clip: p.d });
}
const planksOn = (a, p, w, h, n) => { for (let i = 1; i < n; i++) { const v = -h / 2 + (i * h) / n; a.line(`M${p.at(-w / 2 + .4, v).join(' ')}L${p.at(w / 2 - .4, v).join(' ')}`, { w: 1, c: PLANK.deep }); } };
/** Crushed mesh over its dark: bent wire, close and uneven, bright only where a strand stands up. */
function mesh(a, r, x0, y0, x1, y1) {
  const clip = poly(x0, y1, x0 + 1.4, y0 + 3.4, x0 + 5, y0 + .6, (x0 + x1) / 2, y0 + 1.4, x1 - 4, y0, x1 - 1, y0 + 3, x1, y1);
  a.flat(clip, SOOT.shade);
  for (let i = 0; i < (x1 - x0 + y1 - y0) / 1.3; i++) {
    const u = x0 - (y1 - y0) + i * 1.3 + r() * .5, kink = r() * .9;
    a.flat(poly(u, y1, u + .4, y1, u + (y1 - y0) * .55 + kink + .4, (y0 + y1) / 2, u + (y1 - y0) + .4, y0, u + (y1 - y0), y0, u + (y1 - y0) * .55 + kink, (y0 + y1) / 2), i % 4 ? OLD.shade : OLD.base, { clip });
    a.flat(poly(u + (y1 - y0), y1, u + (y1 - y0) + .4, y1, u + (y1 - y0) * .45 - kink + .4, (y0 + y1) / 2, u + .4, y0, u, y0, u + (y1 - y0) * .45 - kink, (y0 + y1) / 2), i % 3 ? OLD.deep : OLD.lite, { clip });
  }
}

const pileHull = (a) => {
  const r = rng(0x11);
  hill(a, .6, 32, 1.6, 26, 3, 21.4, 2.6, 18, 5.4, 13, 7.6, 11, 8, 7.4, 11, 5.6, 12, 2.4, 15, 2.8, 18, 3.4, 21.6, 6, 26, 9, 27.4, 13.4, 29.4, 17, 29, 22, 31, 25, 31.4, 32);
  laid(a, r, [[14.4, 3.8, 6.4, 3.6, .5, HULL], [19.6, 6.2, 7.6, 3.4, .32, HULL], [9.8, 8.4, 5, 3.4, -.75, OLD], [24.2, 10.4, 7, 3, .85, HULL], [16.4, 9, 7.4, 3, -.15, OLD], [21.4, 12.4, 7, 2.2, .2, PLATE]]);
  const big = piece(a, r, 10.4, 15.4, 13.6, 7.8, -.6, HULL);
  rivetsOn(a, big, [-5, 2.4], [-1.6, 2.4], [1.8, 2.4]);
  laid(a, r, [[27, 16, 5.4, 3.6, 1.1, OLD], [17.4, 17, 5.4, 1.6, 1.25, OLD, .15]]);
  const plate = piece(a, r, 22.6, 24.4, 14, 15, .05, PLATE, .55);
  rivetsOn(a, plate, [-5.2, -5.6], [-2, -5.6], [1.4, -5.6], [4.8, -5.6], [-5.2, 5.2], [-2, 5.2], [1.4, 5.2], [4.8, 5.2]);
  a.line('M21 23.6L23.2 25.2L25.6 24.8', { w: 1 });
  const front = piece(a, r, 9.6, 26.8, 14, 9.4, -.1, OLD, .5);
  hazardOn(a, front, 10.8, [[-7, -4.4, -4.6, 5.4, 0], [-1.4, .6, -2, 5.4, 1], [3.6, 4.8, -4.4, 1.4, 0]]);
  laid(a, r, [[16.6, 30.4, 5.6, 3, .12, OLD], [27.6, 30.6, 4.6, 2.8, -.3, HULL], [2.8, 30.8, 3.4, 2.4, .3, RUST]]);
};

const pileScrap = (a) => {
  const r = rng(0x24b);
  hill(a, .4, 24, 1, 19, 2.4, 15, 2.6, 12, 5, 9.4, 5.6, 6.6, 8.4, 4.6, 10, 1.8, 13, 1.6, 14.6, 4.2, 17.4, 5.6, 18.6, 8.6, 21, 11.4, 21.4, 15, 23, 19, 23.6, 24);
  mesh(a, r, 2, 14, 22.6, 24);
  laid(a, r, [[11.6, 3.4, 4.2, 3.2, .7, OLD], [8.4, 6, 5.4, 3.4, -.4, PLATE], [14.8, 6.6, 5, 3, .5, OLD], [5.4, 8.4, 2.6, 2, .3, RUST], [18.2, 9.8, 5.4, 2.6, .25, RUST], [11.4, 9.2, 6.4, 3.8, .2, OLD]]);
  laid(a, r, [[5.2, 12, 5.6, 3, -.5, PLATE], [9.6, 12.4, 6.6, 1.3, 1.1, PLATE, .1], [15.6, 12.6, 4, 2, -.2, RUST], [19.2, 14.2, 5.6, 2.8, .55, OLD], [12.2, 14.4, 6, 2, .1, OLD]]);
  laid(a, r, [[3.4, 21.8, 4.2, 3.4, .3, OLD], [17.4, 22.4, 5, 2.6, -.2, PLATE], [10.6, 22.8, 3.2, 1.8, .15, RUST]]);
};

const pileCargo = (a) => {
  const r = rng(0x37);
  hill(a, .4, 24, 1, 18, 1.6, 13, 3.4, 10, 3.4, 5.4, 6, 2.8, 10, 1.4, 15, 1, 19.6, 2, 21, 6, 21.6, 11, 22.8, 15, 23.6, 24);
  const crate = piece(a, r, 12.2, 6.8, 14.6, 7.6, -.1, PLANK, .35);
  planksOn(a, crate, 14.6, 7.6, 3);
  hazardOn(a, crate, 7.6, [[-7, -5.6, -3.6, 3.6, 0]]);
  laid(a, r, [[17.4, 2.8, 4.4, 1.6, .14, PLANK, .15], [9.6, 2.8, 3, 1.4, -.2, RUST, .15], [13, 9.2, 3.4, 1.8, -.1, RUST], [20.4, 9.6, 3.6, 3.4, .4, OLD], [4, 10.6, 3.6, 3, -.5, OLD]]);
  laid(a, r, [[11, 13.4, 5.4, 2.2, .9, PLATE, .2], [16.8, 13.8, 5.8, 1.6, -1.05, OLD, .15], [7, 14.8, 4.6, 2.6, .3, RUST], [19.8, 15.4, 4.6, 3, .2, OLD]]);
  laid(a, r, [[10.4, 18.2, 8.6, 2.8, -.5, OLD]]);
  laid(a, r, [[18.6, 20.4, 8, 6.4, -.7, HULL, .6], [5.4, 21.2, 7.6, 5, .1, PLATE, .55], [12.6, 22.6, 4.4, 2.6, .1, OLD]]);
};

const pileBits = (a) => {
  const r = rng(0x41);
  hill(a, .3, 16, .8, 11, 2, 8, 2.6, 4.4, 5.8, 2.6, 6.6, .6, 9, .6, 12.6, 2.4, 14, 5.4, 14.8, 9.4, 15.7, 16);
  laid(a, r, [[7.6, 1.8, 2.4, 2.2, .25, PLATE, .3]]);
  const crate = piece(a, r, 8.2, 5.8, 9.8, 5.2, -.2, PLANK, .3);
  planksOn(a, crate, 9.8, 5.2, 2);
  laid(a, r, [[6, 7.2, 3, 2.4, .2, RUST], [13.4, 8.6, 2.8, 2.2, .5, OLD]]);
  const plate = piece(a, r, 7.4, 12.8, 12.6, 5.8, -.08, OLD, .4);
  hazardOn(a, plate, 6.6, [[-6.6, -4.6, -2.4, 3.2, 0], [-1, .8, -1, 3.2, 1], [3.2, 4.2, -2.6, .6, 0]]);
  laid(a, r, [[13.4, 14.2, 3.6, 2.4, -.25, HULL]]);
};

Subjects.piles = function () {
  for (const [kind, size, draw] of [['pileHull', 32, pileHull], ['pileScrap', 24, pileScrap], ['pileCargo', 24, pileCargo], ['pileBits', 16, pileBits]])
    out(`machine_${kind}`, bake({ w: size, h: size, draw }));
};
})();

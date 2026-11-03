// The credit, as picked (the ink-signs ADR): a slab of gunmetal with brass teeth and a working lamp, credits held as charge is in a
// cell, lying on a diagonal. 16 by 16, the finish's; written before every amount of credits.
(function () {
'use strict';
const { ell, poly } = Anime;
const n3 = (v) => +v.toFixed(3);

/** Rust where water sat, as upgrades.js places it: a torn edge and a darker heart. */
const rust = (a, x, y, s) => {
  a.flat(poly(x - s, y - .2 * s, x - .3 * s, y - .7 * s, x + .5 * s, y - .5 * s, x + s, y + .1 * s, x + .4 * s, y + .4 * s, x + .1 * s, y + .75 * s, x - .5 * s, y + .45 * s), '#70482f');
  a.flat(poly(x - .4 * s, y - .1 * s, x + .1 * s, y - .3 * s, x + .45 * s, y + .05 * s, x, y + .3 * s), '#3f2a24');
};
const rivet = (a, x, y, r) => { a.flat(ell(x, y, r), '#262a3a'); a.flat(ell(x - r * .2, y - r * .22, r * .55), '#8f97a6'); };

const GUN = { base: '#596173', shade: '#40465a', deep: '#2a2e40', lite: '#7f879b', spec: '#c4ccd8' };
const CONTACT = { base: '#b0914f', shade: '#76603a', deep: '#4c3d2a', lite: '#d8bd82', spec: '#f2e2b4', ink: '#17120f' };

function chip(a) {
  const th = -38 * Math.PI / 180, cs = Math.cos(th), sn = Math.sin(th);
  const at = (u, v) => [n3(7.7 + u * cs - v * sn), n3(8.3 + u * sn + v * cs)];
  const slab = (list) => poly(list.flatMap(([u, v]) => at(u, v)));
  const seg = (u0, v0, u1, v1) => `M${at(u0, v0).join(' ')}L${at(u1, v1).join(' ')}`;
  // The teeth, behind the body.
  a.shape(slab([[4.1, -2], [6.8, -2], [6.8, 2], [4.1, 2]]), CONTACT, { shade: slab([[4, 1.2], [6.9, 1.2], [6.9, 2.1], [4, 2.1]]), lite: slab([[4, -2.1], [6.9, -2.1], [6.9, -1.5], [4, -1.5]]) });
  for (const v of [-1, 0, 1]) a.flat(slab([[4.6, v - .17], [6.9, v - .17], [6.9, v + .17], [4.6, v + .17]]), '#2b2118');
  // The body: lit along its top, its thickness in shade and deep along the bottom.
  a.shape(slab([[-5.3, -2.7], [4.4, -2.7], [4.4, 2.7], [-5.3, 2.7], [-6.2, 1.8], [-6.2, -1.8]]), GUN, {
    lite: slab([[-6.3, -2.8], [4.5, -2.8], [4.5, -2.15], [-6.3, -2.15]]),
    shade: slab([[-6.3, 1.6], [4.5, 1.6], [4.5, 2.8], [-6.3, 2.8]]),
    deep: slab([[-6.3, 2.25], [4.5, 2.25], [4.5, 2.8], [-6.3, 2.8]]),
    lines: [seg(3.05, -2.7, 3.05, 2.7)],
  });
  // The window: sunk, its lower lip lit; the lamp that says it holds, and the bars of what it holds.
  a.flat(slab([[-4.4, -1.45], [1.9, -1.45], [1.9, .95], [-4.4, .95]]), '#15171f');
  a.flat(slab([[-4.4, .95], [1.9, .95], [1.9, 1.25], [-4.4, 1.25]]), '#7f879b');
  a.glow(ell(...at(-3.35, -.25), .62), '#f2b84b');
  [-.85, -.35, -.65, -.15].forEach((top, i) => a.flat(slab([[-2.2 + i, top], [-1.62 + i, top], [-1.62 + i, .5], [-2.2 + i, .5]]), '#3c4256'));
  // The lanyard hole, the cap's two rivets, rust from the seam, a scratch across the lit edge, a dented back corner.
  a.cut(ell(...at(-5.2, 0), .55));
  a.line(ell(...at(-5.2, 0), .62), { w: 1 });
  rivet(a, ...at(3.75, -1.75), .36); rivet(a, ...at(3.75, 1.65), .36);
  rust(a, ...at(3.0, 2.05), .5);
  a.line(seg(-1.2, -2.35, 1.4, -1.85), { w: .7 });
  a.line(seg(-5.9, -1.2, -5.5, -.2), { w: .8 });
  // The pen swells along the shadow side.
  a.line(`M${at(-5.3, 2.72).join(' ')}L${at(4.4, 2.72).join(' ')}L${at(6.8, 2.02).join(' ')}`, { w: 2.1 });
}

Subjects.credit = function () {
  out('credit', Anime.bake({ w: 16, h: 16, draw: chip }));
};
})();

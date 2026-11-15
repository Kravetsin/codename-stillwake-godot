// The outside parts of abandoned vessels, as picked on the derelict-looks mockup (docs/plans/derelict-looks-mockup.html, drawPart).
// Each stands outside a hull, against space: lit from the upper left by the star, worn and scorched, never repaired.
(function () {
'use strict';
const { ell, poly, rng } = Anime;

const INK = '#0e1018';
const STEEL = { base: '#555a63', shade: '#3b3f47', deep: '#282a31', lite: '#757b86', spec: '#a0a6ae', ink: INK };
const GUN = { base: '#474c5a', shade: '#2e3140', deep: '#1e202c', lite: '#6a7080', spec: '#a0a8b8', ink: INK };
const OLD = { base: '#61646c', shade: '#44464f', deep: '#30323a', lite: '#83858a', spec: '#a2a3a6', ink: INK };
const COPPER = { base: '#7e5a3e', shade: '#563c2c', deep: '#3a2820', lite: '#a07a54', spec: '#c49e76', ink: INK };
/** The heat tint a burn left on the bell: brass by the throat, violet-blue further out. */
const BRONZE = { base: '#8a693f', shade: '#5e4630', deep: '#3e2e22', lite: '#9c7c52', spec: '#c9a878', ink: INK };
const BRONZE2 = { base: '#6e5a3f', shade: '#4c3e2e', deep: '#322a22', lite: '#8a7454', spec: '#ab9470', ink: INK };
const VIOLET = { base: '#4f4a6b', shade: '#36334c', deep: '#252336', lite: '#625e80', spec: '#8f8aac', ink: INK };
const VIOLET2 = { base: '#433f58', shade: '#2e2b3e', deep: '#1f1d2b', lite: '#5b5674', spec: '#7f7a98', ink: INK };
const SCORCH = ['#393b41', '#2a2b30'];
const RUST = ['#6a4630', '#3f2a24'];

const band = (x0, y0, x1, y1) => poly(x0, y0, x1, y0, x1, y1, x0, y1);
const rivets = (a, ...pts) => { for (const [x, y] of pts) { a.flat(ell(x, y, .42), '#2c2f3e'); a.flat(ell(x - .08, y - .1, .2), '#c2c8d0'); } };
/** A torn patch with a darker heart, its edge by chance: rust where a seam held water, soot where the burn licked back. */
function patch(a, x, y, rx, ry, [edge, heart], clip, seed) {
  const r = rng(seed), blob = (k) => { const p = []; for (let i = 0; i < 11; i++) { const t = (i / 11) * Math.PI * 2, q = k * (.66 + r() * .58); p.push(x + Math.cos(t) * rx * q, y + Math.sin(t) * ry * q); } return poly(p); };
  a.flat(blob(1), edge, clip ? { clip } : undefined);
  a.flat(blob(.52), heart, clip ? { clip } : undefined);
}

/** machine_vesselNozzle: the main bell of a dead vessel, 8 × 7 cells, its throat bolted to the hull on the right, its mouth on the left. */
function nozzle(a) {
  const cy = 28, N = 53, E = 5.2, H0 = 10, H1 = 26.8, RX = 4.4;
  // The mockup's profile, h = 1.25 + 2.15·t^1.7 cells, from the neck (t 0) to the mouth (t 1).
  const h = (x) => H0 + (H1 - H0) * Math.pow(Math.max(0, Math.min(1, (N - x) / (N - E))), 1.7);
  /** A strip of the bell between x0 and x1, from k0 to k1 of its half-height (−1 its top, 1 its foot). */
  const zone = (x0, x1, k0, k1) => {
    const top = [], foot = [];
    for (let i = 0; i <= 16; i++) { const x = x0 + (x1 - x0) * i / 16; top.push(x, cy + k0 * h(x)); foot.unshift(x, cy + k1 * h(x)); }
    for (let i = 0; i < foot.length; i += 2) top.push(foot[i], foot[i + 1]);
    return poly(top);
  };
  const lit = (x0, x1, m, o) => a.shape(zone(x0, x1, -1, 1), m, { outer: false, edge: false, lite: zone(x0, x1, -1.1, -.52), shade: zone(x0, x1, .24, 1.1), deep: zone(x0, x1, .68, 1.1), ...o });

  // The collar the throat bolts through, flaring to the hull; its bracket and a feed line into the hull.
  a.shape(poly(52.6, cy - 10.6, 64, cy - 12.6, 64, cy + 12.6, 52.6, cy + 10.6), GUN, {
    lite: poly(52.6, cy - 10.6, 64, cy - 12.6, 64, cy - 9.4, 52.6, cy - 8.2), shade: band(52, cy + 5, 64, cy + 13), deep: band(52, cy + 9, 64, cy + 13) });
  a.tube(`M59.4 ${cy - 15}L59.4 ${cy - 18.4}Q59.4 ${cy - 20.4} 61.4 ${cy - 20.4}L64.6 ${cy - 20.4}`, 1.5, COPPER);
  a.shape(band(56.8, cy - 15.2, 61.8, cy + 15.2), OLD, {
    lite: band(56.8, cy - 15.2, 61.8, cy - 13.8), shade: [band(60.6, cy - 15.2, 61.8, cy + 15.2), band(56.8, cy + 8, 61.8, cy + 15.2)], deep: band(56.8, cy + 13.4, 61.8, cy + 15.2),
    lines: [`M56.8 ${cy - 10.8}L61.8 ${cy - 10.8}`, `M56.8 ${cy + 10.8}L61.8 ${cy + 10.8}`] });
  rivets(a, [58.8, cy - 13], [58.8, cy + 13], [58.8, cy - 4.6], [58.8, cy + 4.6]);
  patch(a, 59.6, cy + 12.4, 1.3, .9, RUST, band(56.8, cy - 15.2, 61.8, cy + 15.2), 7);

  // The bell, its outline round the whole, then the bands the heat left on it.
  const bell = zone(E, N, -1, 1);
  a.shape(bell, STEEL, { lite: zone(E, N, -1.1, -.52), shade: zone(E, N, .24, 1.1), deep: zone(E, N, .68, 1.1) });
  // The mockup's bands, scaled from its 7.7-cell bell to this one.
  lit(40.6, 51.8, BRONZE);
  lit(35, 40.6, BRONZE2);
  lit(25.7, 35, VIOLET);
  lit(21.3, 25.7, VIOLET2);
  // The tubes it was brazed from, along its length, and the hoops round it.
  for (const k of [-.4, .5]) a.line('M' + Array.from({ length: 17 }, (_, i) => { const x = E + 4 + (N - 6 - E) * i / 16; return `${x.toFixed(2)} ${(cy + k * h(x)).toFixed(2)}`; }).join('L'), { w: .5 });
  for (const x of [13.8, 33]) a.line(`M${x} ${cy - h(x) * .98}L${x + .2} ${cy + h(x) * .98}`, { w: 1.2 });
  // The ring at the neck, against the collar.
  a.shape(poly(50.6, cy - 11.4, 54.2, cy - 11.4, 54.2, cy + 11.4, 50.6, cy + 11.4), OLD, {
    lite: band(50.6, cy - 11.4, 54.2, cy - 6.4), shade: band(50.6, cy + 3, 54.2, cy + 11.4), deep: band(50.6, cy + 8, 54.2, cy + 11.4) });
  rivets(a, [52.4, cy - 9], [52.4, cy + 9]);

  // Soot where the burn licked back over the lip, a crack or two, worn bare at the rim.
  for (const [x, y, rx, ry, s] of [[12.6, cy + 19, 5.6, 2.4, 3], [11.4, cy - 20.6, 4.6, 1.6, 5], [10.6, cy + 7, 3.4, 1.4, 11]]) patch(a, x, y, rx, ry, SCORCH, bell, s);
  a.line(`M21.6 ${cy - 17.6}L23.4 ${cy - 15.4}L23 ${cy - 13.8}`);
  a.line(`M33 ${cy + 13.2}L34.4 ${cy + 14.6}`);

  // The mouth: its lip, then the dark inside, a thin light along the lit rim.
  a.shape(ell(E, cy, RX, H1), STEEL, { lite: band(E - RX - 1, cy - H1 - 1, E + RX + 1, cy - H1 * .45), shade: .9 });
  a.flat(ell(E + .6, cy + .3, RX - 1.5, H1 - 1.6), '#121419');
  a.flat(ell(E + 1.4, cy + 1, RX - 3, H1 - 6.4), '#0b0c10');
  a.line(`M${E - 1.6} ${cy - H1 + 2.4}Q${E - 2.6} ${cy - 12} ${E - 2.4} ${cy + 2}`, { c: '#7d838c', w: .8 });
}

Subjects.vessel = function () {
  out('machine_vesselNozzle', Anime.bake({ w: 64, h: 56, draw: nozzle }));
};
})();

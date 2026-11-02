// What outlasted them, and what is falling: the arc device, the shard in its cradle and its niche, the spire's lens; a plant still alive, one dead; web, crack, beam, rubble. Sorts after homes.js for its kit.
(function () {
'use strict';
const { ell, rr, poly, bake } = Anime;
const { band, board, post, dust, stud, shine, WOOD, IRON, SOOT, STONE, COPPER, CLAY, TEAL, LINEN, BURN, BURNHOT } = window.HomeKit;
const SHARD = { base: '#3d2c74', shade: '#2a1e54', deep: '#1b1338', lite: '#5e48a0', spec: '#a48ce0' };
const LEAF = { base: '#3f6b42', shade: '#2b4d32', deep: '#1c3424', lite: '#5a8a54', spec: '#7fae6e' };
const BLOOM = '#58d8bc', BLOOMHOT = '#c8fff0';
const TWIG = { base: '#7a5a38', shade: '#7a5a38' };
const GRIT = { base: '#4c4852', shade: '#37343d', deep: '#262429', lite: '#68646e', spec: '#85818a' };
/** Draws through <paramref name="a"/> only if <paramref name="there"/>; else as many empty calls, so the pen of what follows does not move between a picture's states. */
const maybe = (a, there) => (there ? a : { shape() { a.flat('M0 0Z', '#000'); }, flat() { a.flat('M0 0Z', '#000'); }, tube() { a.flat('M0 0Z', '#000'); }, line() { a.flat('M0 0Z', '#000'); } });
/** A crack in a wall: a black line that thins from where it started. */
function rift(a, pts, w0, w1, c) {
  const L = [], R = [], n = pts.length - 1;
  pts.forEach((p, i) => {
    const q = pts[Math.min(n, i + 1)], o = pts[Math.max(0, i - 1)], dx = q[0] - o[0], dy = q[1] - o[1], l = Math.hypot(dx, dy) || 1, w = (w0 + (w1 - w0) * (i / n)) / 2;
    L.push(p[0] - (dy / l) * w, p[1] + (dx / l) * w); R.unshift(p[0] + (dy / l) * w, p[1] - (dx / l) * w);
  });
  a.flat(poly(...L, ...R), c);
}

// The resonator: a dull steel ball on a neck, a column wound in copper, a thin return rod to its knob, a heavy bolted foot. It still works.
const arcDevice = (a) => {
  a.tube('M3 24L3 46', .9, IRON, { spec: false });
  a.shape(ell(3, 23, 1.3), IRON, { lite: ell(2.6, 22.6, .5) });
  a.shape(band(2, 45, 22, 51), IRON, { lite: band(2, 45, 22, 45.7), shade: band(2, 49.4, 22, 51), lines: ['M4 47.4L10 47.4', 'M15 47.4L20 47.4'] });
  for (const x of [5, 9, 15, 19]) stud(a, x, 48.2);
  a.shape(band(9.2, 16.8, 15.8, 45.2), SOOT, { lite: band(9.2, 16.8, 10, 45.2) });
  // The windings, by hand: thick bands with dark gaps, one slipped.
  for (const [y, h, k] of [[18, 2.2, 0], [21, 2.2, .1], [24, 2.2, 0], [27.2, 2.2, -.2], [30.4, 1.4, .2], [32.6, 2.2, 0], [35.6, 2.2, .1], [38.6, 2.2, 0], [41.6, 2.2, -.1]]) {
    a.shape(poly(9.4 + k, y, 15.6 + k, y + .1, 15.8 + k, y + h, 9.4 + k, y + h - .1), COPPER, { outer: false, edge: false, lite: band(9, y, 11, y + h), shade: band(14, y, 16.4, y + h), deep: band(9, y + h - .5, 16.4, y + h + .2) });
  }
  a.shape(band(11, 13, 13.8, 17), IRON, { lite: band(11, 13, 11.6, 17), shade: band(13, 13, 13.8, 17) });
  // The ball.
  a.shape(ell(12.4, 7.2, 6.2), IRON, { shade: 1.6, deep: 'M8 11.6C10 13 15 13 17 11L17.4 13.6L8 13.6Z', lite: 'M7.8 5.6C8.4 3.8 10 2.4 11.8 2.2L11.8 3.4C10.4 3.6 9.2 4.6 8.8 6Z', spec: ell(10, 4.2, 1.1, .7), lines: ['M13.4 2L14.6 4.4'] });
  a.flat(ell(15.4, 10.4, .55), '#5e9cd0');
  a.flat(ell(13, 9.6, .4), '#3f6c94');
};
const arcDeviceGlow = shine(23, 52, (g) => { g.halo(ell(12.6, 9.4, 1.6), 'rgba(120,190,255,.28)', 4); g.halo(ell(15.4, 10.4, 1.2), 'rgba(120,190,255,.3)', 3); });

// A plant nobody tends, grown thin on arc light: long vines out of a teal pot, three lit blooms.
const BLOOMS = [[8.5, 4], [4.5, 9], [12.5, 11]];
const plantAlive = (a) => {
  a.tube('M8.6 23C7.4 19 9.6 14 8.6 4.6', .6, LEAF, { spec: false });
  a.tube('M8 22.6C5.6 19 5.4 13 4.6 9.6', .55, LEAF, { spec: false });
  a.tube('M9.2 23C11 19.6 10 15 12.4 11.6', .55, LEAF, { spec: false });
  a.tube('M8 16.6C9.6 15 10.2 13.4 10 11.6', .4, LEAF, { spec: false });
  for (const [x, y, t] of [[7.4, 19, -1], [10.4, 18, 1], [5.6, 15.4, -1], [9.6, 12.4, 1], [6.2, 12.6, 1], [8.2, 8.4, -1], [11, 15, 1], [7, 21.2, 1]]) a.shape(poly(x, y, x + t * 1.5, y - 1, x + t * 2.4, y - .3, x + t * 1.2, y + .6), LEAF, { edge: false, outer: true, shade: poly(x, y, x + t * 2.4, y - .3, x + t * 1.2, y + .6) });
  // The pot: rim, a chip out of it.
  a.shape('M3 24L14 24L13.4 30.6C13.2 31.6 12.6 32 12 32L5 32C4.4 32 3.8 31.6 3.6 30.6Z', TEAL, { lite: band(3, 24, 5, 32), shade: [band(11.8, 24, 14, 32), band(3, 30.6, 14, 32)], lines: ['M6 26L6.4 29'] });
  a.shape(poly(1.6, 22.2, 15.8, 22.2, 15.6, 24.4, 1.8, 24.4), TEAL, { lite: band(1, 22.2, 16, 22.8), shade: band(1, 23.6, 16, 24.6) });
  a.flat(band(2.8, 22.3, 14.6, 22.9), '#141514');
  for (const [x, y] of BLOOMS) {
    a.flat(poly(x, y - 1.8, x + .55, y - .55, x + 1.8, y, x + .55, y + .55, x, y + 1.8, x - .55, y + .55, x - 1.8, y, x - .55, y - .55), BLOOM);
    a.flat(ell(x, y, .5), BLOOMHOT);
  }
};
const plantAliveGlow = shine(17, 33, (g) => { for (const [x, y] of BLOOMS) { g.halo(ell(x, y, 2.4), 'rgba(90,255,220,.42)', 7); g.core(poly(x, y - 1.6, x + .5, y - .5, x + 1.6, y, x + .5, y + .5, x, y + 1.6, x - .5, y + .5, x - 1.6, y, x - .5, y - .5), '#70f0d4'); g.core(ell(x, y, .5), '#d8fff4'); } });

// The same thing that nobody tended, without the arc: sticks, a clay pot, what fell off it on the floor.
const plantDead = (a) => {
  for (const d of ['M8 10.4L8.2 7L7.4 4.4L8.6 2', 'M8.2 7.4L5.6 5.2L3.6 5.6', 'M5.6 5.2L5 3.4', 'M8.2 6.6L11 4.6L13.4 5.4', 'M11 4.6L11.4 2.8', 'M7.6 4.8L6 3.2', 'M8 9L10.4 8L12 8.8', 'M8 9.4L5.6 8.6']) a.tube(d, .55, TWIG, { bare: false, spec: false });
  a.flat(poly(3.4, 5.2, 4.8, 6, 3.4, 7), '#6a4c30');
  a.flat(poly(12.8, 4.6, 14, 5.8, 12.6, 6.4), '#6a4c30');
  a.shape('M4.4 11.2L13 11.2L12.2 17.2C12 17.8 11.6 18 11 18L6.4 18C5.8 18 5.4 17.8 5.2 17.2Z', CLAY, { lite: band(4, 11.2, 6, 18), shade: [band(11, 11.2, 13.4, 18), band(4, 16.6, 14, 18)], lines: ['M10 11.4L9.4 13.4L10 15'] });
  a.shape(poly(3.4, 10, 14, 10, 13.8, 11.6, 3.6, 11.6), CLAY, { lite: band(3, 10, 14, 10.4), shade: band(3, 11, 14, 11.8) });
  a.flat(band(4.6, 10.1, 12.8, 10.5), '#1a1413');
  a.flat(poly(1, 18, 2.4, 17, 3, 18), '#7a5a38');
  a.flat(poly(13.8, 18, 15.2, 17.2, 15.6, 18), '#6a4c30');
};

// A web in a corner, grey with what it caught: spokes from the corner, sagging rings, a torn side.
const cobweb = (a) => {
  const T = { base: '#8a867c', shade: '#8a867c' }, F = { base: '#6c6960', shade: '#6c6960' };
  for (const d of ['M1 1L14.6 2', 'M1 1L13 7', 'M1 1L9.4 11', 'M1 1L4.6 14', 'M1 1L1.4 11']) a.tube(d, .32, T, { bare: true, spec: false });
  for (const d of ['M4.8 1.4C4.6 2.4 4.2 3 3.6 3.6C3 4.2 2.4 4.6 1.2 4.6', 'M8.4 1.6C8.2 3 7.6 4 6.6 5.4C5.2 6.8 3 7.6 1.3 7.8', 'M12 1.8C11.8 3.6 11 5.4 9.4 7.2C7.4 9.4 5 10.6 3.8 11', 'M14.2 2C13.8 4 13 5.8 12.6 7']) a.tube(d, .3, F, { bare: true, spec: false });
  // What it caught: thick grey where threads meet.
  a.flat(poly(1, 1, 3.6, 1.2, 1.4, 3.4), T.base);
  a.flat(poly(5.6, 5, 7, 5, 7, 6.4, 5.6, 6.8), '#7a766c');
  a.flat(poly(9, 2.4, 10.4, 2.2, 10.2, 3.4, 9.2, 3.6), F.base);
  a.flat(poly(3, 8.6, 4.4, 8.4, 4.4, 9.8, 3.2, 10), F.base);
};

// A crack climbing the wall, branching where the wall gave.
const crack = (a) => {
  rift(a, [[9.2, 1], [9.6, 5], [9, 7.6], [9.8, 9.2], [8.8, 11.4], [8, 14], [6.6, 15.4], [6.8, 18], [5.8, 21], [6, 24.6]], 1.5, .4, '#14100e');
  rift(a, [[9.6, 9], [11.6, 10.2], [12.4, 12], [13.4, 13]], 1, .3, '#14100e');
  rift(a, [[6.8, 15.2], [4.4, 16.4], [3, 18.2], [1.4, 18.8]], 1, .3, '#14100e');
  rift(a, [[6.8, 18], [4.6, 18.6]], .7, .25, '#14100e');
  a.line('M10.2 3L10.6 7.4', { w: 1, c: '#6e6a62' });
  a.line('M7.4 15.8L7.6 19', { w: 1, c: '#6e6a62' });
};

// A roof beam that came down: one end still in the wall, the other in the floor, its rivets sprung. Marks left on the ceiling where it ran.
const fallenBeam = (a) => {
  for (const [x0, x1] of [[6, 8], [9.6, 13], [14.4, 16.6], [22, 24.2], [26, 30], [31.4, 32.6], [34, 37], [41, 43.6], [47.6, 54]]) a.flat(poly(x0, 4.4, x1, 4.2, x1, 5, x0, 5.2), '#5f5a54');
  a.shape('M1.2 3.6L2.6 3.2L3.6 4.6L12 8L24 12.6L36 17.2L47 21.4L57.6 25.4L57.8 30.6L57 33.6L56 32L44 27.8L32 23.4L20 18.8L8 14.2L2.8 13L1.4 10.2L2.4 7.4Z', IRON, {
    lite: 'M3.6 4.6L12 8L24 12.6L36 17.2L47 21.4L57.6 25.4L57.6 26.6L47 22.6L36 18.4L24 13.8L12 9.2L3.2 5.8Z',
    shade: 'M2.8 11L8 12.2L20 16.6L32 21.2L44 25.8L57.6 30.2L57 33.6L56 32L44 27.8L32 23.4L20 18.8L8 14.2L2.8 13Z',
    lines: ['M9 9L16 11.8', 'M26 15.2L31 17', 'M40 20.4L47 23', 'M52 25.4L55 26.6', 'M2.6 4.4L1.8 7'] });
  a.flat(poly(1.2, 3.8, 2.6, 3.4, 3.2, 5, 2.4, 7.2, 1.6, 6), SOOT.base);
  for (const [x, y] of [[12, 10.2], [21, 13.6], [30, 17.2], [39, 20.6], [48, 24], [54, 26.2]]) stud(a, x, y);
  // Dust slid down its upper edge and stayed in the seams.
  a.flat(poly(14, 8.8, 19, 10.8, 19, 11.3, 14, 9.3), '#7c7c78');
  a.flat(poly(32, 15.8, 35.6, 17.2, 35.6, 17.7, 32, 16.3), '#7c7c78');
  a.flat(poly(45, 20.6, 50, 22.5, 50, 23, 45, 21.1), '#7c7c78');
};

// A low heap of what fell.
const rubble = (a) => {
  a.shape('M1 7.4L1.4 5.6L3.4 4.2L6 4.6L7 2L9.2 1L11.8 1.6L12.6 3.8L15 3.8L16.4 5L19 5.2L21.6 6.2L22.4 7.4Z', GRIT, { shade: band(0, 6, 23, 7.6) });
  a.shape(poly(7, 2.2, 9.2, 1, 11.8, 1.6, 12.4, 4.8, 8.2, 5.4), GRIT, { lite: poly(7.4, 2.2, 9.2, 1.2, 11.4, 1.8, 9, 2.4), shade: poly(11, 2, 11.8, 1.6, 12.4, 4.8, 10.6, 5) });
  a.shape(poly(2.2, 5.2, 3.8, 4.2, 6.4, 4.8, 6.6, 6.4, 2.8, 6.6), GRIT, { lite: poly(2.8, 5, 3.8, 4.4, 5.4, 4.8, 3.4, 5.4) });
  a.shape(poly(13.2, 4.6, 15.6, 4, 16.8, 5.4, 15.2, 6.6, 13.2, 6.4), { ...GRIT, base: '#3f3c45' }, {});
  a.shape(poly(18, 5.8, 20, 5.4, 21.4, 6.8, 18.4, 7.2), GRIT, { lite: poly(18.4, 5.8, 20, 5.5, 20.4, 6, 18.6, 6.2) });
  a.line('M9.4 2.8L8.6 4.6', { w: 1 });
  dust(a, 1.3, [8.4, 11, .3]);
};

const clampM = COPPER, SPRING = { base: '#958b73', shade: '#6e6654', deep: '#4c4639', lite: '#b4aa90', spec: '#ccc4ac' };
const FEET = { ...IRON };

/** The shard: a long dark crystal in facets, its old lightning frozen in it. Its pale lines by hand, where the game lights. */
const shardBig = (a) => {
  a.shape('M19.6 .8L21.4 4L23.4 9L25.4 15L27 21L28 27L27.6 33L26.4 37L24.6 40L21 41L17.6 40L15.4 36.4L14 31L13.6 26L14.4 20L15.8 14L17.6 7Z', SHARD, {
    lite: 'M19.6 .8L20 7L17 19L15.2 30L15.8 37L14 31L13.6 26L14.4 20L15.8 14L17.6 7Z', shade: 'M21.4 4L23.4 9L25.4 15L27 21L28 27L27.6 33L26.4 37L24.6 40L24.4 31L23.6 20Z', deep: 'M23.6 31L26.8 28L27 33L24.6 40L22 41Z',
    lines: ['M20 7L21.8 18L21 30L20.4 41', 'M21.8 18L25.4 15', 'M21 30L15 30', 'M17 19L21.8 18'] });
  const P = { base: '#d8c8f4', shade: '#d8c8f4' };
  a.tube('M21.6 7L21.6 11.6L20.6 12.2L20.6 16.6L21.6 17.2L21.6 19.6L22.6 20.2L22.6 21.6L24 23.6', .7, P, { bare: true, spec: false });
  a.tube('M24 23.6L22.6 25L22.4 26.4L21.6 28.4', .55, P, { bare: true, spec: false });
  a.tube('M24 23.6L25.6 25.4L26.6 27.4', .55, P, { bare: true, spec: false });
  a.tube('M22.6 25L18.6 27.6L17.6 26.2', .5, P, { bare: true, spec: false });
  a.tube('M20.6 34L21.6 35.4L21.6 40', .6, P, { bare: true, spec: false });
};
// The cradle a spire keeps its shard in: a copper yoke on two struts, a pale spring under, iron feet. Taken, only the yoke and what held it remain.
const shardCradle = (has) => (a) => {
  // Struts and feet.
  a.tube('M12 34L6.6 45.6', 1.4, clampM, { spec: false });
  a.tube('M32 34L37.4 45.6', 1.4, clampM, { spec: false });
  a.shape(poly(1.2, 45.4, 11, 45.4, 11.4, 48, 1, 48), FEET, { lite: band(1, 45.4, 12, 46), shade: band(1, 47.2, 12, 48.2) });
  a.shape(poly(33.6, 45.4, 43, 45.4, 43.4, 48, 33.2, 48), FEET, { lite: band(33, 45.4, 44, 46), shade: band(33, 47.2, 44, 48.2) });
  // The spring.
  for (const y of [41, 42.5, 44, 45.5, 47]) a.shape(poly(15.4, y, 27.6, y, 27.8, y + 1.1, 15.2, y + 1.1), SPRING, { outer: false, edge: true, lite: band(15, y, 28, y + .35), shade: band(25.6, y, 28, y + 1.2) });
  // The shard goes in behind the yoke.
  const g = maybe(a, has);
  shardBig(g);
  // The yoke, last, so it stands the same with or without.
  a.shape(rr(10.2, 28.8, 23.8, 5.4, .6), clampM, { lite: band(10.2, 28.8, 34, 29.6), shade: band(10.2, 32.8, 34, 34.2), lines: ['M12.6 31.4L18 31.4', 'M25.6 31.4L31.6 31.4'] });
  for (const x of [12, 17.4, 26.2, 31.6]) a.flat(ell(x, 30.2, .35), clampM.spec);
};
const shardCradleGlow = shine(44, 49, (g) => {
  const line = (pts, w) => { const L = [], R = []; pts.forEach(([x, y]) => { L.push(x - w, y); R.unshift(x + w, y); }); return poly(...L, ...R); };
  const parts = [line([[21.6, 7], [21.6, 11.6], [20.6, 12.2], [20.6, 16.6], [21.6, 17.2], [21.6, 19.6], [22.6, 20.2], [22.6, 21.6], [24, 23.6]], .5), line([[24, 23.6], [22.6, 25], [22.4, 26.4], [21.6, 28.4]], .42), line([[24, 23.6], [25.6, 25.4], [26.6, 27.4]], .42), poly(22.6, 24.7, 22.8, 25.3, 18.6, 28, 17.3, 26.3, 17.8, 26.2, 18.7, 27.2), line([[20.6, 34], [21.6, 35.4], [21.6, 40]], .45)];
  for (const d of parts) g.halo(d, 'rgba(190,150,255,.65)', 6);
  for (const d of parts) g.core(d, '#f8ecff');
});

// A home's own small shard, in a niche in the wall with the same copper yoke: the heart that feeds its hearth.
const shardNiche = (a) => {
  a.shape(band(2, 3.6, 24, 28.6), SOOT, { lite: band(2, 3.6, 3, 28.6), shade: band(23, 3.6, 24, 28.6) });
  a.flat(band(4, 4.4, 22, 28), '#0e1216');
  a.flat(poly(4, 4.4, 22, 4.4, 22, 6.6, 15, 7.4, 8, 6.8, 4, 7.6), '#090c0f');
  // Struts, spring, then the small shard and its yoke.
  a.tube('M9.8 22.6L7 26.6', .8, COPPER, { spec: false });
  a.tube('M17.6 22.6L20.4 26.6', .8, COPPER, { spec: false });
  for (const y of [24.8, 25.9, 27]) a.shape(poly(11, y, 16.4, y, 16.6, y + .8, 10.8, y + .8), SPRING, { outer: false, edge: false, lite: band(10, y, 17, y + .3) });
  a.shape('M13.4 8.6L14.6 11L15.8 15L16.4 19L16.4 22L15.6 25L13.4 25.6L11.6 25L10.8 22L10.8 19L11.6 14L12.6 11Z', SHARD, {
    lite: 'M13.4 8.6L13.2 13L12 20L11.8 25L10.8 22L10.8 19L11.6 14L12.6 11Z', shade: 'M14.6 11L15.8 15L16.4 19L16.4 22L15.6 25L15 19Z', lines: ['M13.2 13L14 20L13.4 25'] });
  const P = { base: '#d8c8f4', shade: '#d8c8f4' };
  a.tube('M13.5 11L13.5 15.6L14.5 16.2L14.5 17.8L13.5 18.4L13.5 20', .55, P, { bare: true, spec: false });
  a.tube('M14.5 17.6L15.5 18.4L15.5 20', .45, P, { bare: true, spec: false });
  a.tube('M13.5 18.4L11.6 19.6', .4, P, { bare: true, spec: false });
  a.shape(rr(9, 20.4, 9.4, 2.8, .4), COPPER, { lite: band(9, 20.4, 18.4, 21), shade: band(9, 22.4, 18.4, 23.2) });
  // The frame: lintel, base, what the cable runs out of.
  a.shape(poly(1, 1.2, 25, 1.2, 25, 4, 1, 4), IRON, { lite: band(1, 1.2, 25, 1.8), shade: band(1, 3.2, 25, 4), lines: ['M4 2.6L11 2.6', 'M16 2.6L22 2.6'] });
  a.shape(poly(6, 26.4, 11, 26.4, 11, 28.4, 6, 28.4), IRON, { outer: false, lite: band(6, 26.4, 11, 26.9) });
  a.shape(poly(16.4, 26.4, 21.4, 26.4, 21.4, 28.4, 16.4, 28.4), IRON, { outer: false, lite: band(16.4, 26.4, 21.4, 26.9) });
  a.shape(poly(1, 28.4, 25, 28.4, 25, 32, 1, 32), IRON, { lite: band(1, 28.4, 25, 29), shade: band(1, 31, 25, 32), lines: ['M4 30L9 30', 'M14 30.2L21 30.2'] });
  dust(a, 1.3, [4, 9, .35], [15, 21, .35]);
};
const shardNicheGlow = shine(26, 33, (g) => {
  const parts = [poly(13, 11, 14, 11, 14, 15.6, 15, 16.2, 15, 17.8, 14, 18.6, 14, 20, 13, 20, 13, 18.2, 14, 17.6, 14, 16.4, 13, 15.8), poly(14.6, 17.6, 15.9, 18.2, 15.9, 20, 15.1, 20, 15.1, 18.6, 14.4, 18), poly(13.3, 18.2, 13.6, 18.6, 11.8, 20, 11.4, 19.4), poly(13, 23, 14, 23, 14, 25, 13, 25)];
  for (const d of parts) g.halo(d, 'rgba(190,150,255,.65)', 6);
  for (const d of parts) g.core(d, '#f6eaff');
});

// The spire's lens: a violet-white egg of glass under a thin cap.
const spireLens = (a) => {
  a.flat(poly(7.4, 5.2, 17.8, 5.2, 17.8, 5.9, 7.4, 5.9), IRON.lite);
  a.flat(poly(11.8, 5.4, 13, 5.4, 13, 6.6, 11.8, 6.6), SOOT.lite);
  a.flat(ell(12.5, 12, 5, 6.5), '#7a5cc4');
  a.flat(ell(12.5, 12, 4.2, 5.7), '#b09ae4');
  a.flat(ell(12.4, 12.2, 3, 4.3), '#e6dcf8');
  a.flat(ell(12.2, 12, 1.6, 2.6), '#fbf8ff');
  a.flat(poly(9, 18.2, 16, 18.2, 16, 18.8, 9, 18.8), SOOT.lite);
};
const spireLensGlow = shine(24, 24, (g) => {
  g.halo(ell(12.5, 12, 6.8, 8.4), 'rgba(190,120,255,.6)', 8);
  g.core(ell(12.5, 12, 4.8, 6.4), '#f0d6ff');
  g.core(ell(12.5, 12, 3.8, 5.2), '#ffffff');
});

Subjects.homesruin = function () {
  const pic = (name, w, h, draw, up) => out(`homes/${name}`, bake({ w, h, draw, shift: up ? [0, -up] : undefined }));
  pic('arcDevice_0', 23, 52, arcDevice); out('homes/arcDevice_0_glow', arcDeviceGlow);
  pic('plantAlive_0', 17, 33, plantAlive); out('homes/plantAlive_0_glow', plantAliveGlow);
  pic('plantDead_0', 16, 19, plantDead);
  pic('cobweb_0', 16, 16, cobweb);
  pic('crack_0', 14, 26, crack);
  pic('fallenBeam_0', 58, 34, fallenBeam);
  pic('rubble_0', 24, 8, rubble, .6);
  pic('shardCradle_0', 44, 49, shardCradle(true)); pic('shardCradle_0_dark', 44, 49, shardCradle(false)); out('homes/shardCradle_0_glow', shardCradleGlow);
  pic('shardNiche_0', 26, 33, shardNiche); out('homes/shardNiche_0_glow', shardNicheGlow);
  out('ruins/spireLens_lit', bake({ w: 24, h: 24, draw: spireLens })); out('ruins/spireLens_glow', spireLensGlow);
};
})();

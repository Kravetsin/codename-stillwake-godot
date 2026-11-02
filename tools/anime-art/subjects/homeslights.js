// The makers' lights: hearth, globes, sconce, lantern, window, garland, each drawn once, burning or dead, so only the part that burns changes. Sorts after homes.js for its kit.
(function () {
'use strict';
const { ell, rr, poly, bake } = Anime;
const { band, board, post, dust, stud, shine, WOOD, DARKW, IRON, SOOT, STONE, BRASS, BURN, BURNHOT } = window.HomeKit;
const GLOBE = { lit: ['#2f5fb0', '#5f9ee6', '#bfe4ff'], fed: ['#5a3fa8', '#9a78e0', '#e6d8ff'] };
const DEAD = { base: '#1d262d', shade: '#141a20', deep: '#0d1115', lite: '#2f3b45', spec: '#51616c' };

/** The makers' mark, in thin metal: a ring and a cross, with two fins. */
function mark(a, cx, cy, s, c) {
  const L = (x0, y0, x1, y1, w) => a.tube(`M${cx + x0 * s} ${cy + y0 * s}L${cx + x1 * s} ${cy + y1 * s}`, w * s, { base: c, shade: c }, { bare: true, spec: false });
  L(0, -4.4, 0, 4.4, .75); L(-2.8, -.8, 2.8, -.8, .7);
  L(-1.9, -3, -3.6, .4, .6); L(-3.6, .4, -1.6, 3.6, .6); L(1.9, -3, 3.6, .4, .6); L(3.6, .4, 1.6, 3.6, .6);
}
window.HomeKit.mark = mark;

/** The burning heart of a globe: a hard, uneven shape, for rings would read as a target. */
const blot = (x, y, r) => poly(x - r, y - r * .2, x - r * .55, y - r * .86, x + r * .2, y - r, x + r * .9, y - r * .44, x + r, y + r * .3, x + r * .34, y + r, x - r * .4, y + r * .9, x - r * .92, y + r * .44);

/** A globe, lit in steps of tone with no gradient, or dead: clouded, one crack, a black bite. Always the same four calls. */
function globe(a, x, y, r, tone) {
  if (tone) {
    const [o, m, h] = GLOBE[tone];
    a.flat(ell(x, y, r), o);
    a.flat(blot(x, y, r * .86), m);
    a.flat(blot(x + r * .04, y - r * .04, r * .56), h);
    a.flat(blot(x - r * .03, y + r * .02, r * .26), BURNHOT);
  } else {
    a.shape(ell(x, y, r), DEAD, { shade: r * .3, lite: ell(x - r * .3, y - r * .4, r * .32) });
    a.flat(poly(x + r * .1, y + r * .15, x + r * .75, y + r * .05, x + r * .55, y + r * .55, x + r * .2, y + r * .6), '#0a0d10');
    a.line(`M${x - r * .4} ${y - r * .1}L${x} ${y + r * .15}L${x - r * .1} ${y + r * .6}`, { w: 1, c: '#46555f' });
    a.line(`M${x + r * .1} ${y - r * .6}L${x + r * .3} ${y - r * .1}`, { w: 1, c: '#46555f' });
  }
}
const globeGlow = (w, h, x, y, r, tone) => shine(w, h, (g) => {
  const [halo, body, hot] = tone === 'fed' ? ['rgba(160,110,255,.55)', '#9a7cf0', '#ece0ff'] : ['rgba(90,160,255,.55)', '#5fa4f4', '#d0ecff'];
  g.halo(ell(x, y, r * 1.5), halo, 9);
  g.core(ell(x, y, r * .98), body);
  g.core(blot(x, y, r * .8), hot);
});

// The hearth: the makers' iron flue and its mark, a stone box, a coil across the mouth. A heart's chip still feeds some of them.
const hearth = (lit) => (a) => {
  // The flue: a long iron pipe with its seams, widening into the hood.
  a.shape(poly(15.4, 1, 21.8, 1, 21.8, 31, 15.2, 31), IRON, { lite: band(15, 1, 16.6, 32), shade: band(20, 1, 22, 32), lines: ['M15.6 9.2L21.6 9.2', 'M15.6 20.2L21.6 20.2', 'M18 3L18 7.4', 'M19.4 13L19.4 18'] });
  a.flat(band(15.6, 8.8, 21.6, 9.6), IRON.deep); a.flat(band(15.6, 19.8, 21.6, 20.6), IRON.deep);
  a.shape('M15.2 31L21.8 31L23 35.6L24.4 41L26 47.4L27.6 53.4L8.8 53.4L10.4 47.4L12 41L13.6 35.6Z', IRON, {
    lite: 'M15.2 31L16.4 31L13.2 45L10.8 53.4L8.8 53.4L10.4 47.4L12 41L13.6 35.6Z', shade: 'M21 31L21.8 31L23 35.6L24.4 41L26 47.4L27.6 53.4L24 53.4L23.2 47Z', deep: band(8, 51.6, 28, 53.6),
    lines: ['M13.4 36L23.2 36', 'M11.8 41.4L24.6 41.4', 'M10.2 47.6L14 47.6', 'M22.6 47.6L26.2 47.6', 'M19.6 32.4L20 34.8'] });
  stud(a, 13.8, 38.6); stud(a, 23.2, 38.6); stud(a, 11.8, 44.6); stud(a, 25, 44.6);
  mark(a, 18.6, 43.6, 1, BRASS.lite);
  // The box of stone, its mouth black from long use.
  a.shape(band(3, 56.6, 33, 80.8), STONE, { lite: band(3, 56.6, 33, 57.4), shade: [band(31.6, 56.6, 33.2, 81), band(3, 77, 33, 81)], lines: ['M3.4 62L7 62', 'M29 66L32.4 66', 'M3.4 71L7 71', 'M29 74L32.4 74', 'M9 58.8L16 58.8', 'M21 59L27 59', 'M5 58.4L5 61.4', 'M30.6 58L30.6 62'] });
  for (const x of [5, 9, 13, 17, 21, 25, 29]) a.flat(ell(x + .3, 79.2, .32), STONE.deep);
  a.shape(rr(6.6, 60.2, 23, 17, .3), { base: '#433e49', shade: '#332f39', deep: '#26222a', lite: '#5b5560', spec: '#6f6974' }, { outer: false, edge: true, lite: band(6.6, 60.2, 29.6, 60.8) });
  a.flat(band(8.4, 62, 27.6, 76), '#141010');
  a.flat(poly(8.4, 62, 27.6, 62, 27.6, 63.6, 21, 64.4, 13, 63.8, 8.4, 64.6), '#0b0908');
  // The mantel over it all, chipped; dust along it.
  a.shape(poly(1, 53.4, 35, 53.2, 35, 56.8, 1, 57), IRON, { lite: band(1, 53.2, 35, 54), shade: band(1, 55.8, 35, 57), lines: ['M4 55L12 55.1', 'M24 55L32 54.9'] });
  dust(a, 53.4, [2, 7, .6], [28.6, 34, .7]);
  // The coil's posts, and last, what burns or does not.
  a.shape(rr(8.8, 64.6, 1.2, 6.4, .2), IRON, { outer: false, lite: band(8.8, 64.6, 9.3, 71) });
  a.shape(rr(26.2, 64.6, 1.2, 6.4, .2), IRON, { outer: false, lite: band(26.2, 64.6, 26.7, 71) });
  const coil = 'M10 70.4L12.1 66.4L14.1 70.4L16.1 66.4L18.1 70.4L20.1 66.4L22.1 70.4L24.1 66.4L26 70.4';
  a.tube(coil, 1.2, lit ? { base: '#d07a30', shade: '#d07a30' } : { base: '#49281c', shade: '#49281c' }, { bare: true, spec: false });
  a.tube(coil, .55, lit ? { base: '#f6e4b0', shade: '#f6e4b0' } : { base: '#5d3524', shade: '#5d3524' }, { bare: true, spec: false });
  a.flat(band(9, 74, 27, 75), lit ? '#c0502a' : '#33201a');
};
const hearthGlow = shine(36, 82, (g) => {
  const coil = 'M10 70.4L12.1 66.4L14.1 70.4L16.1 66.4L18.1 70.4L20.1 66.4L22.1 70.4L24.1 66.4L26 70.4L26 71L24.1 67.4L22.1 71L20.1 67.4L18.1 71L16.1 67.4L14.1 71L12.1 67.4L10 71Z';
  g.halo(band(8, 64, 28, 73), 'rgba(255,170,90,.32)', 12);
  g.halo(band(9, 73.6, 27, 75.4), 'rgba(235,100,50,.5)', 5);
  g.core(coil, '#ffc070');
  g.core('M10.8 70.2L12.1 66.8L14 70.4L16.1 66.8L18 70.4L20.1 66.8L22 70.4L24.1 66.8L25.3 70.2L24.1 67.2L22.1 70.6L20.1 67.2L18.1 70.6L16.1 67.2L14.1 70.6L12.1 67.2Z', '#fff4d0');
  g.core(band(9.4, 74.2, 26.6, 74.8), '#d8603a');
});

// The ceiling lamp: stem, a shallow shade, a globe under it on a spike.
const ceilLamp = (tone) => (a) => {
  a.shape(band(7, 1, 10, 6.4), SOOT, { lite: band(7, 1, 7.6, 6.4) });
  stud(a, 8.5, 2.2); stud(a, 8.5, 4.2);
  a.line('M8.5 18.8L8.5 20.4', { w: 3, c: IRON.lite });
  // Last the globe, then the shade over its top: the picture's only change is the first.
  globe(a, 8.5, 14.5, 4.5, tone);
  a.shape(poly(5.2, 6, 11.8, 6, 14.6, 9.8, 2.4, 9.8), SOOT, { lite: band(2.4, 6, 14.6, 6.5), lines: ['M6.4 7.2L10.8 7.2'] });
  a.flat(poly(3.8, 8.2, 13.2, 8.2, 13.6, 9, 3.4, 9), IRON.base);
  dust(a, 6.1, [6, 8, .4], [9.2, 11, .4]);
};

// The sconce: a bracket on the wall, a slot of light in a casing beside it.
const sconce = (lit) => (a) => {
  a.shape(poly(1.2, 5.2, 4.8, 5, 5, 15, 1.4, 15), SOOT, { lite: band(1, 5, 5, 5.5) });
  a.flat(band(1.8, 5.8, 2.5, 14.2), IRON.base);
  a.flat(ell(3, 10.2, .45), '#2f5c8c');
  a.shape(rr(4, 2.8, 4.4, 10.6, .5), SOOT, { lite: band(4, 2.8, 8.4, 3.3), shade: band(7.4, 2.8, 8.4, 13.4) });
  a.flat(rr(4.9, 3.8, 2.3, 8.6, .4), lit ? BURN : DEAD.lite);
  a.flat(rr(5.4, 4.3, 1.2, 7.6, .3), lit ? BURNHOT : DEAD.base);
  dust(a, 5.2, [1.6, 3.6, .4]);
};
const sconceGlow = shine(12, 17, (g) => { g.halo(rr(4.4, 3.4, 3.4, 9.4, 1), 'rgba(120,200,255,.5)', 8); g.core(rr(4.9, 3.9, 2.3, 8.3, .5), BURN); g.core(rr(5.4, 4.4, 1.2, 7.3, .3), '#e0f4ff'); });

// The floor lamp: a thin column with a knuckle, a tripod foot, a globe in a ring; two little ears over it.
const floorLamp = (tone) => (a) => {
  a.shape('M3.6 43.8L6.6 41.2L7.8 39.4L9.4 39.4L10.6 41.2L13.8 43.8Z', SOOT, { lite: 'M4.6 43.4L7 41.2L7.6 41.2L6 43.4Z', lines: ['M5.6 43L12 43'] });
  a.shape(band(7.6, 12.6, 9.6, 39.8), IRON, { lite: band(7.6, 12.6, 8.2, 39.8), shade: band(9, 12.6, 9.8, 39.8) });
  for (const y of [16, 21, 33, 37]) a.flat(band(8.2, y, 9, y + 1), IRON.spec);
  a.shape(rr(6.4, 26.4, 4.4, 2.6, .5), SOOT, { lite: band(6.4, 26.4, 10.8, 26.8) });
  a.shape(rr(5, .8, 2.2, 2, .3), SOOT, {}); a.shape(rr(10, .8, 2.2, 2, .3), SOOT, {});
  // The ring, then what is in it.
  a.shape(ell(8.5, 8.5, 5.7), SOOT, { lite: 'M3.8 5.6C4.6 4 6.2 3 8 2.9L8 3.4C6.4 3.5 5 4.4 4.4 5.8Z' });
  a.flat(ell(8.5, 8.5, 5), '#0f1418');
  globe(a, 8.5, 8.5, 4.5, tone);
};

// The lantern: a ring to carry it by, a cap, a little house of glass, a heavy foot.
const lantern = (lit) => (a) => {
  a.line('M6 3.2C6 .8 9 .8 9 3.2', { w: 3.2, c: SOOT.lite });
  a.shape('M4.4 6C4.4 4.2 5.8 2.8 7.5 2.8C9.2 2.8 10.6 4.2 10.6 6Z', SOOT, { lite: 'M5.2 5C5.4 4 6.2 3.4 7 3.2L7 3.8C6.4 4 6 4.6 5.8 5.2Z', lines: ['M5 5.4L10 5.4'] });
  a.shape(band(3.2, 12, 11.8, 13.8), SOOT, { lite: band(3.2, 12, 11.8, 12.5), shade: band(3.2, 13.1, 11.8, 13.9) });
  a.shape(band(4.2, 5.6, 10.8, 12.2), SOOT, {});
  a.flat(rr(5, 6, 5, 6, .3), lit ? '#7cc4f4' : '#1a2b38');
  a.flat(rr(5.8, 6.8, 3.4, 4.4, .3), lit ? '#d4f0ff' : '#233846');
  a.line('M5.4 11L7 8.4', { w: 1, c: lit ? '#a0d8ff' : '#32495a' });
  dust(a, 5.9, [5, 7, .35]);
};
const lanternGlow = shine(15, 17, (g) => { g.halo(rr(4.4, 5.4, 6.2, 7.2, 1), 'rgba(120,205,255,.55)', 9); g.core(rr(5, 6, 5, 6, .3), '#8cdcff'); g.core(rr(5.8, 6.8, 3.4, 4.4, .3), '#e0f6ff'); });

// A ceiling lamp that fell in on itself: its shade knocked askew, its globe a few black teeth; now and then it sputters.
const brokenLamp = (a) => {
  a.shape(band(5.2, 1, 7.8, 6.4), SOOT, { lite: band(5.2, 1, 5.7, 6.4) });
  stud(a, 6.5, 2.4);
  a.shape(poly(2, 10, 3, 7.2, 8.6, 5.8, 12, 7.2, 12.4, 8.6, 6, 10), SOOT, { lite: 'M3 7.2L8.6 5.8L8.8 6.4L3.2 7.8Z', lines: ['M4 8.6L10 7.6'] });
  a.flat(poly(3.6, 8.8, 9.6, 7.6, 10, 8.2, 4, 9.4), IRON.base);
  // What is left of the globe: jagged, hung from its collar, one tooth over the empty.
  a.shape('M3 10L8.6 9.4L9.4 11.6L8.4 13L9 15.2L7.2 14.4L6.8 17.6L5.2 15.4L3.4 18L3.2 14.6L2 13.4Z', DEAD, { shade: band(2, 15, 10, 18.4), lite: 'M3.6 10.4L5.4 10.2L4.4 13L3.2 12.6Z', lines: ['M5.6 11L6.4 13.4'] });
  a.flat(poly(6.6, 13.6, 7.6, 13.4, 7.8, 14.8, 6.8, 15), '#6aaee6');
  a.flat(poly(11, 17, 12, 16.8, 12.2, 17.8, 11.2, 18), DEAD.lite);
  a.flat(poly(8.6, 18.2, 9.2, 18, 9.4, 18.8, 8.8, 18.8), DEAD.lite);
};
const brokenLampGlow = shine(13, 20, (g) => { g.halo(ell(7.2, 14.4, 2), 'rgba(110,190,255,.3)', 4); g.core(poly(6.8, 13.8, 7.6, 13.6, 7.7, 14.8, 6.9, 15), '#8ccaff'); });

const SKY = [['M5 34C4 34 3.4 33.4 3.4 32.4L3.4 12C3.6 7.4 8 3 16 3C24 3 28.4 7.4 28.6 12L28.6 32.4C28.6 33.4 28 34 27 34Z'], ['M3.4 12C3.6 7.4 8 3 16 3C24 3 28.4 7.4 28.6 12L28.6 16C22 15 10 15 3.4 16Z'], ['M8 5.4C11 3.6 21 3.6 24 5.4C25.4 6.4 26 7.6 26.2 8.8C20 8 12 8 5.8 8.8C6 7.6 6.6 6.4 8 5.4Z']];
const ARCH = SKY[0][0];
// The light window: a false sky let into the wall, in an arch, one louvred shutter still hanging. It shows what the world once looked like, or it shows nothing.
const lightWindow = (lit) => (a) => {
  a.shape('M2.4 34L2.4 12C2.6 6.6 8 2 16 2C24 2 29.4 6.6 29.6 12L29.6 34Z', DARKW, { lite: 'M3 11C3.6 6.6 9 2.6 16 2.6L16 3.2C9.6 3.2 4.4 7 3.8 11.4Z', shade: band(28, 10, 30, 34) });
  a.flat(ARCH, lit ? '#6ea4cc' : '#121c28');
  a.flat(SKY[1][0], lit ? '#94c2e0' : '#172434');
  a.flat(SKY[2][0], lit ? '#c0dcee' : '#1c2c3e');
  a.flat(poly(4, 31, 8, 30.4, 13, 31, 17, 30.6, 17, 33.8, 4, 33.8), lit ? '#5c8cb0' : '#0e1620');
  // The shutter, its slats fallen out here and there.
  a.shape(poly(17, 11, 27.6, 11, 27.6, 33.8, 17, 33.8), DARKW, { lite: band(17, 11, 27.6, 11.5), lines: ['M17.6 11.4L17.6 33.6'] });
  const rows = [12.6, 15, 17.4, 19.8, 22.2, 24.6, 27, 29.4, 31.8];
  for (const y of rows) a.flat(band(18.4, y, 26.8, y + 1.2), lit ? '#8fb4cc' : '#0f1720');
  rows.forEach((y, i) => { const w = [[18.4, 26.8], [18.4, 26.8], [20.2, 26.8], [18.4, 25], [18.4, 26.8], [18.4, 26.8], [18.4, 23], [18.4, 26.8], [18.4, 26.8]][i]; a.flat(poly(w[0], y + .7, w[1], y + .5, w[1], y + 1.8, w[0], y + 2), WOOD.base); a.flat(poly(w[0], y + .7, w[1], y + .5, w[1], y + 1, w[0], y + 1.2), WOOD.lite); });
  // The sill of stone.
  a.shape(poly(1.2, 32.4, 30.8, 32.4, 30.8, 34.8, 1.2, 34.8), STONE, { lite: band(1, 32.4, 31, 33), shade: band(1, 34.2, 31, 35.4), lines: ['M4 33.8L14 33.8', 'M19 33.8L28 33.8'] });
  dust(a, 32.5, [4, 7, .5], [9, 15, .6]);
};
// The false sky gives the light, the shutter's slats do not: what is lit is the arch beside it and the gaps between them.
const OPEN = 'M3.4 34L3.4 12C3.6 7.4 8 3 16 3C24 3 28.4 7.4 28.6 12L28.6 11L17 11L17 34Z';
const lightWindowGlow = shine(32, 36, (g) => {
  g.halo(OPEN, 'rgba(170,215,255,.35)', 8);
  g.core(OPEN, 'rgba(150,195,235,.5)');
  g.core('M3.4 16L3.4 12C3.6 7.4 8 3 16 3C24 3 28.4 7.4 28.6 11L17 11L17 15.2C12 15 7 15.4 3.4 16Z', 'rgba(195,228,250,.3)');
  for (const y of [12.6, 15, 17.4, 19.8, 22.2, 24.6, 27, 29.4, 31.8]) g.core(band(18.4, y, 26.8, y + .7), 'rgba(150,195,235,.45)');
});

// The garland: one sagging wire and its beads, one dead in the middle.
const BEADS = [[10, 7.6], [16, 9.6], [22.4, 11.6], [28.4, 12.8], [40.4, 12.8], [46.4, 11.6], [52.4, 9.6], [58.4, 7.6], [64.4, 4.8]];
const garland = (a) => {
  a.line('M2.2 2C7 5 15 8.6 22 10.8C28 12.8 36 13.6 42 12.4C50 10.8 58 7.4 66.4 3', { w: 5, c: SOOT.lite });
  a.line('M2.2 2.4C7 5.4 15 9 22 11.2C28 13.2 36 14 42 12.8C50 11.2 58 7.8 66.4 3.4', { w: 3, c: SOOT.base });
  a.flat(ell(34.4, 13.8, .8, .9), SOOT.deep);
  for (const [x, y] of BEADS) {
    a.flat(poly(x - 1.4, y - 1.1, x + 1.4, y - 1.1, x + .9, y + .1, x, y + 1.2, x - .9, y + .1), BURN);
    a.flat(ell(x, y - .2, .6), BURNHOT);
    a.flat(band(x - .5, y - 1.7, x + .5, y - 1), SOOT.base);
  }
};
const garlandGlow = shine(69, 17, (g) => { for (const [x, y] of BEADS) { g.halo(ell(x, y, 1.9), 'rgba(120,200,255,.5)', 6); g.core(poly(x - 1.1, y - 1, x + 1.1, y - 1, x + .7, y + .1, x, y + .9, x - .7, y + .1), '#a4dcff'); } });

// A lamp as small as a candle, on the table where they sat.
const arcCandle = (a) => {
  a.shape('M2 13.8L3 12.6L4.4 12.2L5.8 12.2L7 12.6L8 13.8Z', SOOT, { lite: 'M3 13.4L4.4 12.5L4.8 12.5L3.8 13.6Z' });
  a.shape(band(4.3, 8, 5.9, 12.4), IRON, { lite: band(4.3, 8, 4.8, 12.4), shade: band(5.4, 8, 6, 12.4) });
  a.shape('M3.2 3.6L7 3.6L6.8 7.2L5.6 8.4L4.4 8.4L3.2 7.2Z', SOOT, { lite: band(3.2, 3.6, 7, 4) });
  a.flat(poly(4.2, 3.4, 6.2, 3, 5.8, 5.8, 4.4, 6), BURN);
  a.flat(poly(4.8, 3.6, 5.6, 3.4, 5.4, 5.2, 4.8, 5.4), BURNHOT);
};
const arcCandleGlow = shine(10, 15, (g) => { g.halo(ell(5, 4.6, 2.8), 'rgba(120,200,255,.5)', 8); g.core(poly(4.2, 3.4, 6.2, 3, 5.8, 5.8, 4.4, 6), '#a8e0ff'); g.core(poly(4.8, 3.6, 5.6, 3.4, 5.4, 5.2, 4.8, 5.4), '#e8f8ff'); });

Subjects.homeslights = function () {
  const pic = (name, w, h, draw, up) => out(`homes/${name}`, bake({ w, h, draw, shift: up ? [0, -up] : undefined }));
  pic('hearth_0', 36, 82, hearth(true)); pic('hearth_0_dark', 36, 82, hearth(false)); out('homes/hearth_0_glow', hearthGlow);
  pic('ceilLamp_0', 17, 23, ceilLamp('lit')); pic('ceilLamp_0_dark', 17, 23, ceilLamp(null)); pic('ceilLamp_0_fed', 17, 23, ceilLamp('fed'));
  out('homes/ceilLamp_0_glow', globeGlow(17, 23, 8.5, 14.5, 4.5, 'lit')); out('homes/ceilLamp_0_fed_glow', globeGlow(17, 23, 8.5, 14.5, 4.5, 'fed'));
  pic('sconce_0', 12, 17, sconce(true)); pic('sconce_0_dark', 12, 17, sconce(false)); out('homes/sconce_0_glow', sconceGlow);
  pic('floorLamp_0', 17, 45, floorLamp('lit')); pic('floorLamp_0_dark', 17, 45, floorLamp(null)); out('homes/floorLamp_0_glow', globeGlow(17, 45, 8.5, 8.5, 4.5, 'lit'));
  pic('lantern_0', 15, 17, lantern(true)); pic('lantern_0_dark', 15, 17, lantern(false)); out('homes/lantern_0_glow', lanternGlow);
  pic('brokenLamp_0', 13, 20, brokenLamp); out('homes/brokenLamp_0_glow', brokenLampGlow);
  pic('lightWindow_0', 32, 36, lightWindow(true)); pic('lightWindow_0_dark', 32, 36, lightWindow(false)); out('homes/lightWindow_0_glow', lightWindowGlow);
  pic('garland_0', 69, 17, garland); out('homes/garland_0_glow', garlandGlow);
  pic('arcCandle_0', 10, 15, arcCandle); out('homes/arcCandle_0_glow', arcCandleGlow);
};
})();

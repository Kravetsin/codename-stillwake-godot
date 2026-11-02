// What a home kept and worked with: books, jars, the desk with its dead screen, kettle, pots, the sink. Sorts after homes.js for its kit.
(function () {
'use strict';
const { ell, rr, poly, bake } = Anime;
const { band, board, post, dust, scar, stud, shine, WOOD, DARKW, PALEW, IRON, SOOT, STONE, COPPER, BRASS, CLAY, TEAL, CRIMSON, NAVY, OCHRE, LINEN, PAPER, BURN, BURNHOT } = window.HomeKit;
const SLATE = { base: '#56686f', shade: '#3c4a51', deep: '#29343a', lite: '#73868b', spec: '#98a9ac' };
const JAR = '#2f5d5c', LIQ = '#58c4ae', LIQHOT = '#b8f0dc';

// One book lying flat: its boards, its pages' edge, a wear at the spine.
const lying = (a, x0, y0, x1, y1, m) => {
  a.shape(poly(x0, y0 + .1, x1, y0, x1 - .1, y1, x0, y1), m, { lite: band(x0, y0, x1, y0 + .45), shade: band(x0, y1 - .5, x1, y1), lines: [`M${x0 + 1.2} ${y0 + .2}L${x0 + 1.2} ${y1 - .1}`] });
};

// The bookcase: three shelves still holding what nobody came back for. The little box of the middle shelf keeps its violet eye.
const bookshelf = (a) => {
  a.shape(band(1, 2.4, 33.2, 50.6), DARKW, { lite: band(1, 2.4, 33.2, 3), shade: band(32, 2.4, 33.2, 50.6) });
  a.flat(band(3.2, 5, 31, 17.2), '#171110'); a.flat(band(3.2, 19.2, 31, 32.4), '#171110'); a.flat(band(3.2, 34.4, 31, 47.2), '#171110');
  a.line('M12 5.4L12.2 17', { w: .9, c: DARKW.shade }); a.line('M22 34.6L21.8 47', { w: .9, c: DARKW.shade });
  // The shelves, the middle one bowed.
  for (const [y, k] of [[17, 0], [32.2, .4], [47, 0]]) a.shape(`M2.8 ${y}C12 ${y + k} 22 ${y + k} 31.4 ${y}L31.4 ${y + 2}C22 ${y + 2 + k} 12 ${y + 2 + k} 2.8 ${y + 2}Z`, WOOD, { outer: false, lite: band(2, y - .1, 32, y + .4), shade: band(2, y + 1.4, 32, y + 2.6), lines: [`M6 ${y + 1}L10 ${y + 1}`, `M20 ${y + 1}L27 ${y + 1}`] });
  a.shape(band(1, 2.2, 33.2, 4.8), WOOD, { lite: band(1, 2.2, 33.2, 2.8), shade: band(1, 4, 33.2, 5) , lines: ['M5 3.5L14 3.5', 'M21 3.6L29 3.5'] });
  // Top shelf: two lying books, one fallen off the other; a long grey one.
  lying(a, 5, 9.3, 15, 11.5, COPPER);
  lying(a, 4, 13.3, 14.2, 15.2, COPPER); a.flat(band(4.6, 15.2, 13.6, 17), DARKW.deep);
  a.line('M5 11.6L4.2 13.2', { w: .9 });
  lying(a, 19, 13.4, 29, 15.6, SLATE); a.flat(band(19.2, 15.6, 28.8, 17), DARKW.deep);
  dust(a, 9.5, [6, 8, .5]); dust(a, 13.6, [20, 27, .5]);
  // Middle: a stick left leaning; the box with its eye.
  a.tube('M5.2 31.4L10.2 25.2', .7, PALEW, { bare: false, spec: false });
  a.shape(rr(13, 27.6, 10, 4.6, .3), COPPER, { lite: band(13, 27.6, 23, 28.1), shade: band(13, 31.2, 23, 32.2), lines: ['M13.4 29.2L16.4 29.2', 'M20.6 29.4L22.6 29.4'] });
  a.flat(poly(18.5, 28.6, 20.3, 31, 16.7, 31), '#7c60b4');
  a.flat(ell(18.5, 30.1, .5), '#d8c4ff');
  // Bottom: a book, a heavy grey one, loose papers curling at the end of the shelf.
  lying(a, 4, 43.2, 14.2, 45.4, COPPER); a.flat(band(4.6, 45.4, 13.6, 47), DARKW.deep);
  lying(a, 15, 42.2, 25.4, 46, SLATE); a.line('M16.4 44L24.6 43.9', { w: .9 }); a.flat(band(15.4, 46, 25, 47), DARKW.deep);
  a.shape('M25.4 40.6L30.6 40.2L30.8 42.2L28.8 42.4L31 43L31 47L25.6 47L25.8 44.2L25 42.6Z', PAPER, { shade: [band(25, 45.6, 31.2, 47.2), 'M28.4 42.4L31 42.8L31 43.6L28.4 43.2Z'], lines: ['M26 41.8L29.6 41.5', 'M26.2 44.6L30.4 44.5'] });
  dust(a, 2.3, [2.4, 6, .7], [8.2, 11, .6], [14, 16.6, .7], [19, 20.4, .5], [23, 26.6, .7], [28.8, 31, .6]);
};
const bookshelfGlow = shine(34, 51, (g) => { g.halo(ell(18.5, 29.3, 2.4), 'rgba(170,130,255,.4)', 5); g.core(poly(18.5, 28, 19.7, 30, 17.3, 30), '#ccb4ff'); g.core(ell(18.5, 28.8, .5), '#eadcff'); });

// The desk, with a dead screen that still shows one lit pixel, and what was on it the last day.
const desk = (a) => {
  // The screen on its stand.
  a.shape(band(12, 12.4, 21, 15.4), SOOT, { outer: true, lite: band(12, 12.4, 21, 12.8) });
  a.shape(rr(5, 1, 20, 12.4, .5), SOOT, { lite: band(5, 1, 25, 1.5), shade: band(5, 12.4, 25, 13.4) });
  a.shape(rr(6.4, 2.2, 17.4, 10, .3), { base: '#14222a', shade: '#0d171d', deep: '#0a1116', lite: '#1c2e38', spec: '#2c4450' }, { outer: false, edge: true, shade: ['M6.4 8L23.8 6L23.8 12.2L6.4 12.2Z'] });
  for (const d of ['M9 4.2L12.4 4.2L12.4 5L9 5Z', 'M14 3.6L16 3.6L16 4.4L14 4.4Z', 'M9.8 6.4L11.6 6.4L11.6 7.2L9.8 7.2Z', 'M13.2 6L16.2 6L16.2 6.6L13.2 6.6Z', 'M11.4 8.6L15 8.6L15 9.3L11.4 9.3Z', 'M17 9L19.4 9L19.4 9.8L17 9.8Z', 'M19.8 4.6L21.6 4.6L21.6 5.2L19.8 5.2Z']) a.flat(d, '#233a44');
  a.line('M7.4 10.4L10 7.6L10.6 9', { w: .9, c: '#2e4a54' });
  a.flat(ell(19.5, 8.5, .55), '#bfe6ff');
  // The leg and the drawers under the top.
  post(a, 3.2, 18.4, 7, 32.8, WOOD, { lines: ['M4.4 21L4.4 30', 'M5.8 24L5.8 31'] });
  a.shape(band(23, 18.6, 36.2, 32.9), DARKW, { shade: band(23, 31.8, 36.2, 33) });
  board(a, 24, 19.4, 35.4, 25.6, WOOD, { outer: false, edge: true, lines: ['M25 21.5L29 21.6', 'M31 23.8L34.2 23.7'] });
  board(a, 24.4, 26.2, 35.6, 32.2, WOOD, { outer: false, edge: true, lines: ['M25.4 28.2L28 28.2', 'M30 30.8L34.4 30.8'], kink: [0, .3, 0, 0] });
  stud(a, 29.6, 22.4); stud(a, 29.8, 29.6);
  // The top and what lies on it: a keyboard's strip, a page, a book left face down.
  a.shape(poly(1, 15.4, 37, 15.2, 37, 18.8, 1, 19), WOOD, { lite: band(1, 15.2, 37, 15.9), shade: band(1, 17.8, 37, 19), lines: ['M3 16.8L14 16.9', 'M18 16.9L34 16.8', 'M9 18L12 18'] });
  a.shape(poly(2.2, 14.2, 11.4, 14.1, 11.6, 15.4, 2, 15.5), SLATE, { lite: band(2, 14, 12, 14.5) });
  a.flat(poly(19, 14.6, 23.4, 14.4, 23.6, 15.3, 18.8, 15.4), PAPER.lite);
  a.shape('M25 15.4C25.2 14.2 27 13.4 29 13.8C31 13.4 33 14.2 33.2 15.4Z', COPPER, { shade: band(25, 14.8, 34, 15.6), lines: ['M29 13.9L29 15.3'] });
  dust(a, 15.3, [12.4, 14.2, .5], [15, 18, .6], [34, 36, .5]);
  dust(a, 1.1, [7, 12, .5], [16, 19, .5]);
};
const deskGlow = shine(38, 34, (g) => { g.halo(ell(19.5, 8.5, 1.6), 'rgba(150,215,255,.35)', 4); g.core(ell(19.5, 8.5, .55), '#d6f0ff'); });

// A shelf of jars: a tall clay bottle, a small stoppered one, the lit jar with what still lives in it, a box.
const shelfJars = (a) => {
  a.shape(poly(4.4, 17, 6.8, 17, 6.8, 19, 4.4, 19), SOOT, {}); a.shape(poly(28, 17, 30.4, 17, 30.4, 19, 28, 19), SOOT, {});
  a.shape('M5.2 2.2L6.6 2.2L6.8 5C8.6 6.2 9.2 8 9.2 10.2C9.2 12 8.8 13.4 8.2 14.2L3.6 14.2C3 13.4 2.6 12 2.6 10.2C2.6 8 3.2 6.2 5 5Z', CLAY, { shade: .9, lite: 'M3.6 7.6L4.4 6.8L4.2 12.6L3.5 12Z', lines: ['M3 10.6C4.6 11 7.2 11 8.8 10.6'] });
  a.flat(rr(5, 1.8, 1.8, 1, .2), CLAY.deep);
  a.shape('M11.8 9.4L13.2 9.4L13.2 10.4C14.6 11 15 12.2 15 14.2L10 14.2C10 12.2 10.4 11 11.8 10.4Z', TEAL, { shade: .7, lite: 'M11 11.6L11.8 11L11.6 13L10.8 13Z' });
  a.shape(rr(11.8, 7.8, 1.5, 1.6, .2), CLAY, { outer: false });
  // The jar: glass over what glows, its lid dark; what changes in it lies under the light.
  a.shape(rr(16.4, 6.2, 6.8, 8, .5), { base: '#2c4048', shade: '#1e2d34', deep: '#141e24', lite: '#4a626a', spec: '#b0cccc' }, { lite: band(16.4, 6.2, 17, 14.2) });
  a.flat(rr(17.2, 9.4, 5.2, 4.2, .3), JAR);
  a.flat(poly(17.2, 9.6, 22.4, 9.2, 22.4, 12, 17.2, 12.4), LIQ);
  a.flat(poly(18, 10.4, 21.4, 10.2, 21.2, 11.4, 18, 11.6), LIQHOT);
  a.shape(rr(16, 3.8, 7.6, 2.6, .3), SOOT, { lite: band(16, 3.8, 23.6, 4.2), lines: ['M18 5.2L21 5.2'] });
  a.shape(poly(24.4, 10.2, 31.8, 10, 32, 14.2, 24.4, 14.2), DARKW, { lite: band(24, 10, 32, 10.5), lines: ['M28.2 10.4L28.2 14'] });
  a.flat(ell(26.4, 12.2, .55), COPPER.base); a.flat(ell(30, 12.1, .55), COPPER.base);
  // The shelf, bowed under its load, its nose chipped.
  a.shape('M.8 14.2C10 14.5 24 14.5 33.2 14.1L33.2 16.6C24 17 10 17 1.2 16.8Z', WOOD, { lite: band(0, 14.1, 34, 14.8), shade: band(0, 16, 34, 17), lines: ['M4 15.5L14 15.6', 'M20 15.6L30 15.5'] });
  dust(a, 14.3, [9.4, 10, .4], [15, 16.2, .5], [23.6, 24.4, .4]);
  dust(a, 3.9, [17, 20.4, .5]);
  dust(a, 10.3, [25, 28, .5]);
};
const shelfJarsGlow = shine(34, 20, (g) => { g.halo(rr(16.4, 8, 7, 6.6, .8), 'rgba(90,255,220,.42)', 7); g.core(poly(17.3, 9.8, 22.3, 9.4, 22.3, 13.4, 17.3, 13.5), 'rgba(90,240,205,.7)'); g.core(poly(18, 10.5, 21.4, 10.2, 21.2, 11.8, 18, 12), '#a8f8e2'); });

// A wall shelf: books slumped against each other, a tool's handle, a rolled thing, its end turned up to stop them.
const wallShelf = (a) => {
  a.shape(band(3.4, 12.6, 5.4, 15), SOOT, {}); a.shape(band(22.2, 12.6, 24, 15), SOOT, {});
  // The books, each leaning a little more than the last.
  a.shape(poly(3, 1.4, 5.2, 1.2, 5.4, 10.2, 3.2, 10.2), CRIMSON, { lite: band(3, 1, 3.5, 10.4), shade: band(4.7, 1, 5.6, 10.4), lines: ['M3.2 3L5.2 3', 'M3.3 8.4L5.3 8.4'] });
  a.shape(poly(5.4, 3.4, 8.2, 3.2, 8.2, 10.2, 5.4, 10.2), NAVY, { lite: band(5.4, 3, 5.9, 10.4), shade: band(7.4, 3, 8.4, 10.4), lines: ['M5.6 5L8 5'] });
  a.flat(poly(6, 7, 7.4, 7, 7.4, 7.6, 6, 7.6), OCHRE.base);
  a.shape(poly(8.8, 1.2, 11, 1.4, 12.6, 10.2, 10.2, 10.2), TEAL, { lite: poly(8.8, 1.2, 9.3, 1.2, 10.7, 10.2, 10.2, 10.2), shade: poly(10.3, 1.3, 11, 1.4, 12.6, 10.2, 11.8, 10.2), lines: ['M9.2 3.4L11.3 3.4'] });
  // The handle against the wall, its head dark; the bundle lying on the shelf.
  a.tube('M14.4 10L16.6 3', .9, PALEW, { spec: false });
  a.shape('M15.6 3.4L17.6 2.2L18.2 3.2L16.6 4.6Z', SOOT, {});
  a.shape('M17.2 7.4C19 6.8 23 6.8 25.4 7.4C25.8 8.2 25.8 9.4 25.4 10.2L17.2 10.2C16.8 9.4 16.8 8.2 17.2 7.4Z', COPPER, { shade: band(16, 9.2, 26, 10.4), lite: 'M18 7.4L23 7.2L23 7.8L18 8Z', lines: ['M19.6 7.2L19.6 10.1', 'M22.8 7.2L22.8 10.1'] });
  // The shelf, its stop.
  a.shape(poly(1, 10.2, 27, 10, 27, 12.8, 1, 13), WOOD, { lite: band(1, 10, 27, 10.7), shade: band(1, 12, 27, 13), lines: ['M3 11.4L11 11.5', 'M15 11.5L24 11.4'] });
  a.shape(band(25.2, 8, 27, 10.2), DARKW, {});
  dust(a, 10.3, [12.8, 14, .4], [15.8, 16.6, .4]);
};

// What was on the table: a plate, a bowl, a tall cup, a chipped bowl, a plate on its edge's shadow. Laid low.
const dishes = (a) => {
  a.shape('M1 8.2C4 7.4 6 7.4 8.6 8.2L8.2 9.4L1.6 9.4Z', SLATE, { lite: 'M2 8.1L7.4 8L7.4 8.5L2 8.6Z', shade: band(1, 9, 9, 9.6) });
  a.shape('M8.2 6.8L12.6 6.8C12.6 8 12 9 11.2 9.4L9.6 9.4C8.8 9 8.2 8 8.2 6.8Z', TEAL, { shade: .5, lite: band(8.2, 6.8, 12.6, 7.1) });
  a.flat(poly(8.8, 7, 12, 7, 12, 7.3, 8.8, 7.3), TEAL.deep);
  a.shape('M16.8 9.4L12.8 9.4C12.2 7 12.4 4 13 1.8L16.6 1.6C17.2 4 17.4 7 16.8 9.4Z', CLAY, { lite: 'M13.4 1.8L13.9 1.8L13.5 9.2L13 9.2Z', shade: 'M15.8 1.6L16.8 1.6L17.2 9.5L15.8 9.5Z', lines: ['M13 4.2C14.2 4.5 15.6 4.5 16.8 4.2'] });
  a.flat(ell(14.8, 1.8, 1.8, .4), CLAY.deep);
  a.shape('M17.2 7.2L21.2 7.2C21.2 8.2 20.6 9.1 19.8 9.4L18.6 9.4C17.8 9.1 17.2 8.2 17.2 7.2Z', TEAL, { shade: .45, lite: band(17.2, 7.2, 21.2, 7.5) });
  a.flat(poly(20, 7.1, 21.3, 7.1, 21, 8, 20.4, 7.8), '#2b2d30');
  a.shape('M21.4 8.4C23 7.6 26 7.4 28.2 8.2L27.6 9.4L22 9.4Z', SLATE, { lite: 'M22.4 8.2L27 8L27 8.5L22.4 8.6Z', shade: band(21, 9, 29, 9.6) });
  a.line('M24 8.6L25.6 9.2', { w: .9 });
  dust(a, 8.1, [3, 5.4, .4]);
};

// The tall jar and its neighbour, and the piece broken off a third, against them.
const pots = (a) => {
  a.shape('M3.6 2.2L7.2 2.2L7.4 4.6C9.2 6 9.8 8.6 9.6 11.2C9.4 13.6 8.8 15.8 8 17.4L3 17.4C2 15.8 1.4 13.6 1.2 11.2C1 8.6 1.6 6 3.4 4.6Z', CLAY, {
    shade: 1.2, deep: band(1, 16.4, 10, 17.6), lite: 'M2.6 7L3.4 6L3 13L2.2 12Z', lines: ['M3.4 4.7C4.6 5 6.2 5 7.4 4.7', 'M7.4 13L6.2 14.4L6.8 15.4'] });
  a.flat(rr(3.2, 1.8, 4.4, .9, .2), CLAY.deep);
  for (const x of [2.8, 4.4, 6, 7.6]) a.flat(ell(x, 10.6, .4, .32), OCHRE.lite);
  a.line('M1.4 11.4C4 11.9 7 11.9 9.5 11.4', { w: .9 });
  a.shape('M11 8.4L15 8.4L15.2 9.6C15.8 10.4 16 11.4 16 12.6L15.8 17.4L10.2 17.4L10 12.6C10 11.4 10.2 10.4 10.8 9.6Z', TEAL, { shade: .9, deep: band(10, 16.6, 16, 17.6), lite: 'M11 10L11.5 9.6L11.3 15.4L10.8 15.4Z', lines: ['M10.8 9.7C12 10 14 10 15.2 9.7'] });
  a.flat(rr(10.8, 7.6, 4.4, .9, .2), TEAL.deep);
  a.shape('M16.4 12.6L19.4 13.6C20 14.8 20.2 16 20 17.4L16.4 17.4L17 15.8Z', CLAY, { shade: band(16, 16.6, 21, 17.6), deep: 'M17 15.8L19 14.6L18.6 15.8L17.4 16.4Z' });
  dust(a, 4.8, [8, 8.6, .3]);
};

// The kettle on its coil: when it burns two points show under it. A dent, a crooked spout.
const kettle = (lit) => (a) => {
  a.shape(rr(2.2, 9.6, 8, 1.3, .3), SOOT, { lite: band(2.2, 9.6, 10, 10) });
  // The handle rises from its shoulder to the lid, on the side away from the spout.
  a.tube('M3.2 6.6C1.4 5.8 1.6 2.6 3.6 2.2C4.6 2 5.4 2.4 5.8 3', .8, SOOT, { spec: false });
  a.shape('M9.4 8L11.2 5.2L12.6 5.4L12.2 6.2L10.6 9.2L9.4 9.6Z', COPPER, { shade: 'M10.8 6.4L12.2 6.2L10.6 9.2L9.6 9.6Z', lite: 'M11.2 5.2L12.6 5.4L12.4 5.8L11 5.8Z' });
  a.shape('M2.6 9.8C2.2 8.6 2.2 7.2 3 6C3.8 4.6 5 3.8 6.2 3.7C7.4 3.8 8.6 4.6 9.4 6C10.2 7.2 10.2 8.6 9.8 9.8Z', { ...COPPER, base: '#8e5f32', lite: '#ab7f4a' }, { shade: [band(2, 8.2, 10.4, 10), 'M8.2 4.6L9.6 6.2L10 9.8L8.4 9.8Z'], deep: band(2, 9.1, 10.4, 10), lite: 'M3.6 7.4C4 6.2 4.8 5.2 5.6 4.8L5.9 5.3C5.2 5.9 4.7 6.7 4.3 7.8Z', lines: ['M7 6.4L7.9 7.6', 'M3 5.9C4.6 5.5 7.6 5.5 9.4 5.9'] });
  a.shape(ell(6.2, 3.2, 1, .7), SOOT, { lite: ell(5.9, 3, .4, .25) });
  for (const x of [4.5, 6.5]) { a.flat(rr(x - .6, 9.9, 1.2, 1, .2), lit ? BURN : '#1e3546'); a.flat(ell(x, 10.3, .25), lit ? BURNHOT : '#26404f'); }
};
const kettleGlow = shine(13, 13, (g) => { g.halo(band(3.6, 9.6, 7.4, 11), 'rgba(120,200,255,.35)', 4); g.core(rr(4, 9.9, 1, .9, .2), '#cfeeff'); g.core(rr(6, 9.9, 1, .9, .2), '#9fd8ff'); });

// The stone sink: its rim, its tap with one drop that never fell, a brush left standing in it.
const basin = (a) => {
  // The tap, on its bracket.
  a.shape('M26.4 1.4L30.4 1.4L30.6 5.6L29.6 9.8L26.6 9.8L26.4 5.6L23.8 5.6L23.8 8.6L22.8 8.6L22.8 4L26.4 4Z', SOOT, { lite: band(26.4, 1.4, 30.4, 1.9), lines: ['M26.6 4L30 4'] });
  a.flat(ell(23.3, 9.5, .45, .7), '#3f7cb0');
  // The brush.
  a.tube('M6.4 7.6L6.6 13', .9, WOOD, { spec: false });
  a.flat(poly(5, 12, 8.2, 12, 8.4, 14, 4.8, 14), DARKW.deep);
  // The sink.
  a.shape('M1 14.6C1 13.4 1.8 12.6 3 12.6L33 12.6C34.2 12.6 35 13.4 35 14.6L35 24C35 25.2 34.2 26 33 26L3 26C1.8 26 1 25.2 1 24Z', STONE, { lite: band(1, 12.6, 35, 13.2), shade: [band(1, 24.4, 35, 26.4), band(33.8, 12, 35.4, 26)], lines: ['M8 17.6L12 18.8', 'M26.4 22L24 23.8', 'M3 19.4L3.6 22'] });
  a.shape(poly(2.8, 13.5, 33.4, 13.5, 33.4, 14.8, 2.8, 14.8), { ...LINEN, base: '#8c8676', lite: '#a49e8c', shade: '#6c6759' }, { outer: false, shade: band(2, 14.3, 34, 15) });
  for (const x of [4.6, 10, 15.4, 20.8, 26, 31.4]) { a.flat(ell(x, 20.7, .38), STONE.deep); a.flat(ell(x - .08, 20.6, .16), STONE.lite); }
  dust(a, 12.7, [6, 9, .5], [11, 13, .4], [15, 20, .5], [23, 28, .5]);
};

// The rail and what still hangs from it: a ladle, the pan, a knife, a tongs. Each hook in its place; each thing a little off plumb.
const utensils = (a) => {
  for (const [x, y1] of [[5.4, 6], [13, 8.4], [21.1, 6], [26.4, 7]]) a.tube(`M${x} 3.4L${x} ${y1}`, .55, SOOT, { spec: false });
  // The ladle.
  a.tube('M5.4 5.4L5.6 12', .8, COPPER, { bare: true, spec: false });
  a.shape('M3.6 12.4C4.4 11.8 6.6 11.8 7.4 12.4C7.6 14 6.8 15.2 5.6 15.4C4.4 15.2 3.4 14 3.6 12.4Z', COPPER, { shade: .6, lite: band(3.6, 12, 7.4, 12.5) });
  // The pan, its bottom burnt.
  a.shape(ell(13, 13, 4, 4.2), COPPER, { shade: 1.1, deep: 'M10.4 15.4C11.6 16.6 14.6 16.6 15.8 15.2L15.8 17.4L10.4 17.4Z', lite: 'M10.2 11.4C10.8 10.2 11.8 9.6 13 9.5L13 10.2C12 10.4 11.2 11 10.8 12Z', lines: ['M12.4 12.4L14.4 13.2', 'M11.4 14.4L12.2 14.8'] });
  a.shape(rr(12.4, 7.8, 1.2, 2, .2), SOOT, {});
  // The knife, its blade worn narrow.
  a.shape(rr(20.3, 5.6, 1.6, 3.4, .3), SOOT, { lite: band(20.3, 5.6, 20.8, 9) });
  a.shape('M20.4 9L21.8 9L21.8 14.4L20.8 16L20.4 15.6Z', { base: '#8b9ca0', shade: '#5f7075', deep: '#465459', lite: '#b4c2c4', spec: '#d4dee0' }, { shade: band(21.2, 9, 22, 16), lines: ['M20.8 11L21 13'] });
  // The tongs, two arms crossed at a rivet.
  a.tube('M25.2 6L27.6 11L25.4 15', .75, COPPER, { spec: false });
  a.tube('M27.4 6L25.4 11L27.6 15', .75, COPPER, { spec: false });
  stud(a, 26.4, 8.6, COPPER.lite);
  // The rail.
  a.shape(poly(1, 1.6, 31, 1.4, 31, 3.8, 1, 4), SLATE, { lite: band(1, 1.4, 31, 2), shade: band(1, 3.2, 31, 4), lines: ['M6 2.7L15 2.7', 'M19 2.8L27 2.7'] });
  stud(a, 2.6, 2.8); stud(a, 29.4, 2.7);
  dust(a, 1.6, [4, 7, .5], [10, 14, .5], [20, 24, .5]);
};

Subjects.homeskeep = function () {
  const pic = (name, w, h, draw, up) => out(`homes/${name}`, bake({ w, h, draw, shift: up ? [0, -up] : undefined }));
  pic('bookshelf_0', 34, 51, bookshelf, .8); out('homes/bookshelf_0_glow', bookshelfGlow);
  pic('desk_0', 38, 34, desk); out('homes/desk_0_glow', deskGlow);
  pic('shelfJars_0', 34, 20, shelfJars); out('homes/shelfJars_0_glow', shelfJarsGlow);
  pic('wallShelf_0', 28, 16, wallShelf);
  pic('dishes_0', 30, 10, dishes, .6);
  pic('pots_0', 21, 18, pots, .6);
  pic('kettle_0', 13, 13, kettle(true)); pic('kettle_0_dark', 13, 13, kettle(false)); out('homes/kettle_0_glow', kettleGlow);
  pic('basin_0', 36, 27, basin);
  pic('utensils_0', 32, 19, utensils);
};
})();

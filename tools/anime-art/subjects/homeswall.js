// What people hang up: cloth, the mural of their faith, a clock that stopped, a family's hologram, a child's drawings and the notches of their height. Sorts after homes.js for its kit.
(function () {
'use strict';
const { ell, rr, poly, bake } = Anime;
const { band, board, post, dust, scar, stud, shine, mark, WOOD, DARKW, PALEW, IRON, SOOT, STONE, COPPER, BRASS, CLAY, TEAL, CRIMSON, NAVY, OCHRE, LINEN, PAPER, WICKER, BURN, BURNHOT } = window.HomeKit;
const LEAF = { base: '#3f5c3a', shade: '#2b422c', deep: '#1c2d20', lite: '#587a4e', spec: '#7c9c6c' };

/** A paint stroke of a wall painting: flat, no pen, faded where it broke. */
const paint = (a, d, c) => a.flat(d, c);

// The mural, painted straight on the wall: the tree of light and those who stood before it, the ones who went down the stair, and the black where the stair ends. Paint has fallen off in places.
const mural = (a) => {
  const TRUNK = '#8a8268', BOUGH = '#5f94ae', BODY = '#7a3a35', SKIN = '#a8986c', STEP = '#6e6650', FLAG = '#a4693a', WALKER = '#3f4a7c', LAND = '#6c5a34';
  // The ground, broken.
  for (const [x0, x1, y] of [[1, 3.6, 25.2], [4.8, 8, 25.4], [8.8, 18.6, 25.2], [19.6, 23, 25.3], [24, 32.6, 25.2], [33.4, 37, 25.4], [38.6, 51, 25.2], [52.6, 62, 25.3], [63, 71.8, 25]]) paint(a, poly(x0, y, x1, y - .15, x1 - .2, y + 1.05, x0 + .2, y + 1.15), LAND);
  for (const [x, y] of [[2.4, 27], [8, 27.1], [28, 27.2], [46, 27]]) paint(a, ell(x, y, .5, .25), LAND);
  // The tree.
  paint(a, poly(21, 13, 22.6, 13, 22.8, 25.4, 20.8, 25.4), TRUNK);
  paint(a, poly(22, 15, 22.6, 15, 22.6, 22, 22, 22.4), '#5a5442');
  for (const d of ['M21.8 13L18 8.4L15.2 5.8', 'M18 8.4L18.2 3', 'M18.2 8.8L15 9', 'M21.8 13L21.8 1.6', 'M21.8 6.4L20 3.4', 'M21.8 6.4L23.6 3.4', 'M21.8 13L25.8 8.4L28.6 5.8', 'M25.8 8.4L25.6 3', 'M25.6 8.8L28.6 9.2', 'M19.6 11.4L18 13', 'M24 11.4L25.6 12.8']) a.tube(d, .8, { base: BOUGH, shade: BOUGH }, { bare: true, spec: false });
  // Those who stood before it: three and three, the second on the right flaked away below.
  for (const [x, k] of [[7.5, 0], [11.5, 1], [15.5, 0], [27.5, 0], [31.5, 2], [35.5, 1]]) {
    paint(a, ell(x, 18.3, .6, .7), SKIN);
    paint(a, poly(x - .55, 19.6, x + .55, 19.6, x + .4, k === 2 ? 22 : 25, x - .4, k === 2 ? 22.6 : 25), BODY);
    if (k === 1) paint(a, poly(x - 1.5, 20.6, x + 1.5, 20.6, x + 1.5, 21.2, x - 1.5, 21.2), BODY);
    else if (k === 0) paint(a, poly(x - 1.2, 20.8, x - .3, 20.4, x - .3, 21.2, x - 1.2, 21.6), BODY);
  }
  // The stair going down to the right, three walkers on it with their flags.
  for (const [x0, x1, y] of [[46, 50.6, 8.4], [48, 52, 11.2], [51, 55, 14.2], [54, 58, 17.2], [58, 62, 20.2], [61, 64, 23.2]]) paint(a, poly(x0, y, x1, y - .1, x1 - .2, y + .95, x0 + .2, y + 1), STEP);
  for (const [x, y] of [[50.2, 7.6], [56.2, 13.6], [62.2, 19.6]]) {
    paint(a, ell(x, y - 1.9, .5, .6), SKIN);
    paint(a, poly(x - .5, y - 1.1, x + .5, y - 1.1, x + .4, y + 2.6, x - .4, y + 2.6), WALKER);
    paint(a, poly(x + .5, y - .9, x + 2.8, y - .7, x + 2.8, y + .1, x + .5, y + .1), FLAG);
  }
  // The black where it ends, and the violet point in it.
  a.flat('M63.4 26.2L73 26.2L73.2 28.4L71 29.6L69.8 30.8L65.4 31.4L63 29.2Z', '#120e16');
  a.flat(poly(66, 28.4, 72, 27.4, 72, 28.6, 66.6, 29.6), '#231c2a');
  paint(a, poly(69, 32.4, 73, 32.2, 73, 33.2, 69, 33.4), STEP);
  a.flat(poly(67.5, 28.4, 68, 29.5, 69.2, 30, 68, 30.5, 67.5, 31.6, 67, 30.5, 65.8, 30, 67, 29.5), '#9c78d8');
  a.flat(ell(67.5, 30, .5), '#e6d8ff');
};
const muralGlow = shine(74, 35, (g) => { g.halo(ell(67.5, 30, 3), 'rgba(170,130,255,.4)', 6); g.core(poly(67.5, 28.6, 68, 29.5, 69, 30, 68, 30.5, 67.5, 31.4, 67, 30.5, 66, 30, 67, 29.5), '#c8acff'); g.core(ell(67.5, 30, .5), '#f0e6ff'); });

// The rug, seen on its edge: a worn band of bars, frayed at both ends.
const rug = (a) => {
  a.shape(poly(2, 1.2, 45, 1, 45.2, 3, 1.8, 3.1), { base: '#4f3a30', shade: '#3a2a26', deep: '#2a1e1c', lite: '#66503e', spec: '#7c6650' }, { lite: band(1, 1, 46, 1.4) });
  for (const [x0, x1, c] of [[3, 5, OCHRE.base], [6, 8, CRIMSON.base], [9.6, 11, NAVY.lite], [12, 14.4, CRIMSON.lite], [16, 17, OCHRE.base], [18.6, 21.4, CRIMSON.base], [23, 24.2, NAVY.lite], [25.4, 27.4, OCHRE.base], [29, 31, CRIMSON.base], [32.6, 33.8, OCHRE.lite], [35, 37.6, CRIMSON.lite], [39, 40.6, NAVY.lite], [41.4, 43.8, OCHRE.base]]) a.flat(poly(x0, 1.5, x1, 1.45, x1, 2.8, x0, 2.85), c);
  for (const [x, y] of [[1.2, 2.3], [.6, 3], [1.4, 3.2], [45.8, 2.2], [46.4, 3], [45.6, 3.2]]) a.flat(ell(x, y, .35, .25), OCHRE.shade);
};

// The tapestry: their story again, woven: the tree on its grey pillar, the six before it, on navy gone dull. A corner torn away; moth at the hem.
const TAPE = 'M2.2 3.4L49.6 3.4L49.8 25.4L48.8 26.4L47.6 26L46.4 28.6L44 29.2L3 29.4L2 29Z';
const tapestry = (a) => {
  a.shape(TAPE, NAVY, { lite: band(2, 3.4, 50, 4), shade: [band(47.4, 3, 50, 30), band(2, 27.4, 50, 30)], lines: ['M12 8L11.6 15', 'M38 6.6L38.4 11', 'M7 20L7.4 25', 'M43.6 16.4L44 22'] });
  // The border, a ragged double line of old gold.
  const G = OCHRE.base;
  a.flat(poly(3.4, 4.8, 48.6, 4.8, 48.6, 5.6, 3.4, 5.6), G);
  a.flat(poly(3.4, 25.8, 46.6, 25.8, 46, 26.6, 3.4, 26.6), G);
  a.flat(band(3.4, 5, 4.2, 26.4), G); a.flat(poly(47.8, 5, 48.6, 5, 48.6, 24.8, 47.8, 25), G);
  for (const [x, y] of [[8, 8], [14, 7.6], [20, 8.2], [36, 8], [42, 7.6], [45.4, 9.4], [6.4, 11.6], [45, 13.6]]) a.flat(ell(x, y, .35), OCHRE.lite);
  // The tree on its pillar.
  a.flat(poly(24.8, 9.8, 28.6, 9.8, 28, 20, 27, 22.8, 26, 22.8, 25.4, 20), STONE.lite);
  a.flat(poly(27.2, 10, 28.6, 9.8, 28, 20, 27, 22.8), STONE.shade);
  a.flat(poly(26.2, 22.6, 27, 22.6, 27, 25.8, 26.2, 25.8), STONE.shade);
  for (const d of ['M26.6 9.6L26.6 5.8', 'M26.6 9.4L23.6 6.4', 'M26.6 9.4L29.8 6.4', 'M25 7.8L22.4 7.2', 'M28.2 7.8L31 7.2']) a.tube(d, .7, { base: '#6fa4bc', shade: '#6fa4bc' }, { bare: true, spec: false });
  // The six: pale heads, red and old-gold robes, linked by the arm.
  for (const [x, m] of [[11, CRIMSON.lite], [15, OCHRE.lite], [19, CRIMSON.lite], [35, CRIMSON.lite], [39, OCHRE.lite], [43, CRIMSON.lite]]) {
    a.flat(ell(x, 18.8, 1, 1.1), PAPER.lite);
    a.flat(poly(x - 1, 20.2, x + 1, 20.2, x + 1.2, 25.6, x - 1.2, 25.6), m);
  }
  a.flat(band(12, 21.8, 18, 22.4), PAPER.shade); a.flat(band(36, 21.8, 42, 22.4), PAPER.shade);
  // The hem's fringe, last threads; the rod over it all.
  for (const x of [4, 6.4, 9, 11.2, 14, 16.6, 19, 21.4, 24, 26.6, 29, 31.4, 34, 36.6, 39, 41.6]) a.flat(band(x, 29.2, x + .6, 30.4), OCHRE.shade);
  a.shape(poly(1, 1, 51, 1, 51, 3.4, 1, 3.4), SOOT, { lite: band(1, 1, 51, 1.5) });
  dust(a, 1.1, [6, 12, .4], [22, 30, .4], [40, 46, .4]);
};

// A banner of the house: its colour, a border, the mark in old gold; the swallowtail hem torn, and torn differently each.
const banner = (m, hem, wear) => (a) => {
  a.shape(hem, m, { lite: band(3, 3, 4.2, 34), shade: band(13.6, 3, 15.4, 34), lines: wear });
  const G = OCHRE.lite;
  a.flat(poly(4.2, 4.2, 14, 4.2, 14, 5, 4.2, 5), G);
  a.flat(band(4.2, 4.6, 5, 26), G); a.flat(band(13.2, 4.6, 14, 26), G);
  a.flat(poly(4.4, 25.6, 13.8, 25.6, 13.8, 26.4, 4.4, 26.4), G);
  mark(a, 9, 14.2, 1.15, G);
  a.shape(poly(1, 1, 17, 1, 17, 3.2, 1, 3.2), SOOT, { lite: band(1, 1, 17, 1.4) });
  a.flat(ell(1.8, 3.2, .5), IRON.lite); a.flat(ell(16.2, 3.2, .5), IRON.lite);
  dust(a, 1.1, [4, 8, .35], [11, 14, .35]);
};
const banner0 = banner(CRIMSON, 'M3 3L15 3L14.8 27L15 31L12.6 28.6L11 31.4L9 29L6.8 31L3.4 34L3.2 27Z', ['M6.4 8L7.6 11', 'M12 16L11 21', 'M5.6 19L7 23']);
const banner1 = banner(NAVY, 'M3 3L15 3L15 26.6L14.4 33.4L12.6 30L9.8 33L8 30.4L5.4 32.6L3.4 34.4L3 27L4.2 22L3.2 20.4Z', ['M11.6 7L10.6 10.4', 'M6.2 16L5.4 20', 'M13 22L12 25']);

// The curtain: heavy old-gold cloth in long folds, tied back by a strip of red; the bottom pooled and frayed.
const curtain = (a) => {
  a.shape('M2 3L12.4 3C12.6 12 12.2 22 12 31.2C11.6 33 10.4 35 9.6 38C9.2 40.4 9 42.6 9.6 44.6L11 46.8L9 46.4L7.6 47L6 46.6L4.6 46.8C4.6 44 3.6 42 3 39C2.6 36 2.4 34 2.4 31.2Z', { ...OCHRE, base: '#7a5c2a', lite: '#a08446' }, {
    lite: ['M2.6 3C2.8 12 2.5 22 2.7 31L3.5 31C3.5 22 3.9 12 3.6 3Z', 'M6.2 3C6.5 12 6 22 6.2 31L7 31C7 22 7.3 12 7 3Z', 'M3.4 33.2C3.5 37 4.2 41 4.6 46L5.2 46C4.9 41 4.2 37 4.2 33.2Z', 'M6.2 33.2C6.4 37 6.2 42 6.4 46.4L7 46.4C6.9 42 7.2 37 7 33.2Z'],
    shade: ['M4 3C3.8 10 4.5 20 4.1 31L5.8 31C6.2 20 5.4 10 5.8 3Z', 'M7.8 3C8.2 12 7.5 22 7.9 31L9.6 31C9.2 22 9.8 12 9.5 3Z', 'M10.6 3C10.9 12 10.6 22 10.7 31L12.4 31C12.6 22 12.8 12 12.6 3Z', 'M4.6 33.2C4.6 37 5.3 41 5.6 46.8L6.2 46.8C6 41 5.4 37 5.4 33.2Z', 'M7.4 33.2C7.7 37 7.3 42 7.5 46.8L9.2 46.4C8.6 42 9.4 37 10 33.2Z'],
    deep: ['M5 3C4.8 10 5.4 20 5.1 31L5.8 31C6.2 20 5.4 10 5.8 3Z', 'M8.8 3C9 12 8.6 22 8.9 31L9.6 31C9.2 22 9.8 12 9.5 3Z', 'M8.2 40C8 42 8 44 8.2 46.6L9.2 46.4C8.8 44 8.8 42 9.2 40Z'] });
  a.shape('M1.6 31.2L12.4 31L12.2 33L1.8 33.2Z', CRIMSON, { lite: band(1, 31, 13, 31.5), lines: ['M6.4 31.2L6.6 33'] });
  a.shape(poly(1, 1, 15, 1, 15, 3.2, 1, 3.2), SOOT, { lite: band(1, 1, 15, 1.4) });
  dust(a, 1.1, [3, 7, .35], [10, 13, .35]);
};

// The hologram of a family: two grown and a child, drawn in light, over a low dark base that keeps three points. When it dies only the base is left.
// Each: a head, shoulders that come off its neck and stop at the base, and the scan's rows across the chest.
const FIGS = [
  { head: [4.6, 4.3, 1.9, 2.2], neck: 'M4 6.4L4 7.6M5.2 6.4L5.2 7.6', body: 'M1.8 14L2 10.6C2.2 9 3 8 4 7.6L5.2 7.6C6.2 8 7.2 9 7.4 10.6L7.8 14Z', rows: ['M2.3 10.4L7.1 10.4', 'M2.1 12.2L7.5 12.2'] },
  { head: [11.7, 8.2, 1.5, 1.7], neck: 'M11.2 9.8L11.2 10.6M12.2 9.8L12.2 10.6', body: 'M9.6 14L9.8 12.2C10 11.2 10.6 10.8 11.2 10.6L12.2 10.6C12.8 10.8 13.4 11.2 13.6 12.2L13.8 14Z', rows: ['M10 12.6L13.4 12.6'] },
  { head: [18, 3.2, 2, 2.3], neck: 'M17.3 5.4L17.3 6.6M18.7 5.4L18.7 6.6', body: 'M15.2 14L15.4 9.6C15.6 8 16.4 7 17.3 6.6L18.7 6.6C19.6 7 20.4 8 20.6 9.6L21 14Z', rows: ['M15.7 9.4L20.3 9.4', 'M15.5 11.2L20.5 11.2', 'M15.4 12.8L20.7 12.8'] },
];
const holoBase = (a) => {
  a.shape(rr(2, 14.2, 18, 4, .4), SOOT, { lite: band(2, 14.2, 20, 14.7), shade: band(2, 17, 20, 18.2) });
  a.flat(poly(3.6, 15, 18.6, 14.9, 18.6, 15.7, 3.6, 15.8), IRON.base);
  dust(a, 14.4, [4, 7, .3], [13, 17, .3]);
};
const holoDraw = (a) => {
  const C = { base: '#58d4e6', shade: '#58d4e6' }, S = { base: '#3aa0b4', shade: '#3aa0b4' };
  for (const f of FIGS) {
    a.tube(ell(...f.head), .6, C, { bare: true, spec: false });
    a.tube(f.neck, .4, C, { bare: true, spec: false });
    a.tube(f.body, .6, C, { bare: true, spec: false });
    for (const d of f.rows) a.tube(d, .45, S, { bare: true, spec: false });
  }
};
const holoPortrait = (lit) => (a) => {
  holoBase(a);
  for (const x of [7.5, 12.5, 17.5]) a.flat(ell(x, 15.5, .4), lit ? '#d4f8ff' : IRON.shade);
  if (lit) holoDraw(a);
};
const holoGlow = shine(23, 19, (g) => {
  for (const f of FIGS) {
    g.halo(ell(...f.head), 'rgba(100,235,250,.14)', 4);
    g.halo(f.body, 'rgba(100,235,250,.14)', 4);
    g.pen(ell(...f.head), '#68eeff', .55);
    g.pen(f.neck, '#68eeff', .4);
    g.pen(f.body, '#68eeff', .55);
    for (const d of f.rows) g.pen(d, 'rgba(104,238,255,.6)', .4);
  }
  for (const x of [7.5, 12.5, 17.5]) g.core(ell(x, 15.5, .45), '#e2fbff');
});

// What a child left on the floor: a wooden animal on wheels, a ball, a little winding thing with one point of life in it.
const toys = (a) => {
  for (const x of [6.4, 11]) { a.shape(ell(x, 8, 1.3), SOOT, { lite: ell(x - .3, 7.6, .45) }); a.flat(ell(x, 8, .35), IRON.lite); }
  a.tube('M4.2 3.8L2.6 3.6L1.4 4.6', .6, { base: LINEN.shade, shade: LINEN.deep }, { bare: true, spec: false });
  a.shape(poly(4, 3, 10.6, 2.9, 10.8, 7, 4.2, 7.1), DARKW, { lite: band(4, 2.9, 11, 3.5), shade: band(4, 6, 11, 7.2), lines: ['M5 4.4L9 4.3'] });
  a.flat(poly(5.4, 4.8, 8.6, 4.7, 8.8, 5.8, 5.2, 5.9), PALEW.base);
  a.shape(poly(10, 7, 10.2, 3, 10.8, .9, 13.8, .7, 14.2, 2.8, 12.6, 3.6, 12.4, 7), DARKW, { lite: poly(10.8, .9, 13.8, .7, 13.8, 1.3, 10.8, 1.5), shade: poly(12.6, 3.6, 14.2, 2.8, 13.8, 1.8, 12.4, 2.6, 11.8, 7, 12.4, 7) });
  a.flat(poly(11.2, .9, 11.5, 0, 12, .8), DARKW.shade);
  a.flat(ell(12.7, 1.8, .3), '#0e0a08');
  stud(a, 10.9, 5);
  dust(a, 3, [5.4, 8.4, .35]);
  // The ball, its paint gone on top.
  a.shape(ell(17, 7.1, 2), CRIMSON, { shade: .7, lite: ell(16.4, 6.3, .7, .5), lines: ['M15.4 7.9C16.4 8.6 17.8 8.6 18.6 7.9'] });
  // The winding toy.
  a.shape('M21 9.1L21.2 7.2L22.8 6.2L26 6.4L27 7.4L27 9.1Z', SOOT, { lite: band(21, 6.2, 27, 6.7), lines: ['M24.4 6.6L24.4 9'] });
  a.flat(poly(25, 5, 26, 5, 25.8, 6.4, 25.2, 6.4), IRON.lite);
  a.flat(ell(23.5, 8.5, .45), '#e8f6ff');
};
const toysGlow = shine(28, 12, (g) => { g.halo(ell(23.5, 8.5, 1.5), 'rgba(190,225,255,.35)', 4); g.core(ell(23.5, 8.5, .45), '#fdffff'); });

// The tall clock: a round face in copper on a dark case, stopped; the case's door is split.
const clock = (a) => {
  a.shape(rr(1.2, 13.4, 14.6, 12, .4), DARKW, { lite: band(1.2, 13.4, 15.8, 14), shade: band(14.4, 13.4, 15.8, 25.4), lines: ['M3.2 16L3.4 23'] });
  a.flat(band(3.8, 15, 13.2, 23.8), '#1a1211');
  // The pendulum, off its hook, lying in the bottom of the case.
  a.tube('M8.6 14.6L8.4 15.8', .5, COPPER, { bare: true, spec: false });
  a.tube('M7.8 16.2L6 22', .5, COPPER, { bare: true, spec: false });
  a.shape(ell(5.8, 22.4, 1.5, 1.3), COPPER, { outer: false, edge: true, shade: .4, lite: ell(5.4, 22, .5, .35) });
  a.line('M12 16L11.2 18.6', { w: 1.2, c: DARKW.lite });
  a.shape(rr(1, 24.8, 15, 2, .3), DARKW, { lite: band(1, 24.8, 16, 25.3) });
  // The face.
  a.shape(ell(8.5, 7.6, 7.3), COPPER, { shade: 1 , lite: 'M2.4 6C3 3.4 5.4 1.2 8.4 1L8.4 1.8C5.8 2 3.8 3.8 3.2 6.2Z' });
  a.flat(ell(8.5, 7.6, 5.6), LINEN.base);
  a.flat(poly(3.4, 8.4, 8, 10, 13.6, 7.8, 12.6, 11, 9, 13.2, 5, 12), LINEN.shade);
  a.line('M4 9.6L6 11.2', { w: 1, c: LINEN.deep });
  for (const [x, y] of [[8.5, 3], [12.8, 7.6], [8.5, 12.2], [4.2, 7.6], [11.6, 4.4], [11.6, 10.8], [5.4, 10.8], [5.4, 4.4]]) a.flat(ell(x, y, .32), SOOT.base);
  // Its hands, as they stopped.
  a.tube('M8.7 7.8L6.6 4.4', .75, SOOT, { bare: true, spec: false });
  a.tube('M8.7 7.8L11.4 8.8', .9, SOOT, { bare: true, spec: false });
  a.flat(ell(8.7, 7.8, .5), SOOT.deep);
};

// The music box, its lid propped open, the comb still in it; it plays when struck.
const musicBox = (a) => {
  a.shape('M1.4 5.2L7.8 .8L8.6 1.6L2.6 6Z', DARKW, { lite: 'M1.8 4.8L7.6 1L7.8 1.3L2 5.2Z' });
  a.shape(rr(1, 5.2, 12, 5.2, .3), DARKW, { lite: band(1, 5.2, 13, 5.6), shade: band(1, 9.4, 13, 10.4) });
  a.shape(band(2.4, 6.2, 11.8, 8.6), COPPER, { outer: false, lite: band(2.4, 6.2, 11.8, 6.7), shade: band(2.4, 7.8, 11.8, 8.6), lines: ['M4 7.2L9.4 7.2'] });
  for (const x of [8.6, 9.4, 10.2]) a.flat(band(x, 6.4, x + .35, 7.4), BRASS.spec);
  a.flat(ell(11, 7, .4), '#d6f0ff');
};
const musicBoxGlow = shine(14, 12, (g) => { g.halo(ell(11, 7, 1.5), 'rgba(150,215,255,.35)', 4); g.core(ell(11, 7, .4), '#d8f2ff'); });

// Herbs hung to dry and forgotten: three bunches on strings, gone the colour of the wall, a little green left in the heart of one.
const herbs = (a) => {
  const string = (d) => a.tube(d, .5, { base: '#2c3841', shade: '#2c3841' }, { bare: true, spec: false });
  // Stalks and dried heads standing out of the bunches, for a bunch is not a bag.
  for (const d of ['M2.6 12.4L1.8 14.4', 'M4.6 13.4L5 15', 'M1.6 9.4L.6 8.4', 'M8 12L7.2 13.8', 'M11 12.4L11.8 14.4', 'M12 8.6L12.8 7.4']) a.tube(d, .3, WICKER, { bare: true, spec: false });
  for (const d of ['M5.4 15.6L4.6 17.6', 'M7.4 15.6L8.4 17.4', 'M6.4 16.6L6.4 18.6']) a.tube(d, .3, LEAF, { bare: true, spec: false });
  string('M3.4 .4L3.4 6.4'); string('M6.2 .4L6.2 9.6'); string('M9.4 .4L9.6 5.6');
  a.shape('M2.8 6.2L4.2 6.2L6 9L6 12.4L4.6 13.8L3.6 12.8L2.6 13.6L1.2 12L1.2 9Z', WICKER, { shade: .7, lines: ['M2.4 8.6L3 12', 'M4.2 8.4L4.6 12.4', 'M3.4 7L3.5 10'] });
  a.flat(band(2.8, 6, 4.2, 6.9), CRIMSON.base);
  a.shape('M9 5.4L10.4 5.4L12 8L12 11.4L10.8 13L9.8 12.2L8.8 13.2L7.4 11.8L7.4 8.4Z', WICKER, { shade: .7, lines: ['M8.6 8L9.2 11.6', 'M10.4 8L10.6 11.4'] });
  a.flat(band(9, 5.2, 10.4, 6), CRIMSON.base);
  a.shape('M5.6 9.6L6.8 9.6L8.2 12L8.2 14.6L7.4 16.4L6.4 17L5.4 16.2L4.4 14.4L4.4 12Z', LEAF, { shade: .6, lines: ['M5.6 12L5.8 15.2', 'M7 12L6.8 15'] });
  a.flat(band(5.6, 9.4, 6.8, 10.2), CRIMSON.base);
};

// A framed picture of home as it was: a sky of two stars over a town of spires. The frame's corner has parted.
const frame = (a) => {
  a.shape(band(1, 1.4, 25, 19), DARKW, { lite: band(1, 1.4, 25, 2), shade: [band(23.8, 1.4, 25, 19), band(1, 17.8, 25, 19)], lines: ['M4 2.6L12 2.6', 'M16 18.2L22 18.2', 'M2.2 7L2.2 13', 'M24 5L24 11', 'M22.2 1.6L23.6 3.2'] });
  a.flat(band(3, 3.4, 23, 17), '#2b2a4c');
  a.flat(poly(3, 3.4, 23, 3.4, 23, 7, 16, 8, 8, 7, 3, 8), '#3d3560');
  a.flat(poly(3, 10, 12, 9.4, 23, 10.4, 23, 12.4, 3, 12.4), '#3e3a5e');
  // The town, dark along the foot.
  a.flat(poly(3, 17, 3, 13.4, 5, 13, 5.6, 11.4, 6.2, 13, 8.6, 13.2, 9.2, 12, 10, 13.2, 12.6, 12.6, 13.4, 13.4, 16, 13.2, 16.6, 10.8, 17.2, 13, 19, 13.4, 20, 12.2, 21, 13.4, 23, 13, 23, 17), '#191a26');
  a.flat(poly(3, 17, 3, 15.4, 9, 15, 15, 15.6, 23, 15, 23, 17), '#111219');
  // The stars, hard points of pale paint.
  a.flat(poly(8.4, 5.2, 9.1, 7.1, 11, 7.8, 9.1, 8.5, 8.4, 10.4, 7.7, 8.5, 5.8, 7.8, 7.7, 7.1), '#d4c8a4');
  a.flat(poly(13.6, 5.8, 14, 7, 15.2, 7.4, 14, 7.8, 13.6, 9, 13.2, 7.8, 12, 7.4, 13.2, 7), '#a8a4b0');
  a.flat(ell(19.4, 6.4, .4), '#a8a4b0');
  a.line('M3.4 16.6L6.4 12', { w: .8, c: '#4a4866' });
  dust(a, 1.5, [3, 8, .4], [11, 15, .4], [18, 23, .4]);
};

// A child's drawings, pinned up: three of them, curling, in wax colour. A family under a lamp; a tower; a sun.
const sheet = (a, pts, shadeAt) => {
  a.shape(poly(...pts), PAPER, { shade: shadeAt, outer: true });
};
const drawings = (a) => {
  // The family: three red figures, the small one between; a pin.
  sheet(a, [.4, 3.4, 8, 3.1, 8.4, 13.2, .6, 13.5], poly(.4, 12.2, 8.4, 12, 8.4, 13.4, .4, 13.6));
  for (const [x, y0] of [[2.2, 7.6], [4.2, 9.2], [6.2, 7.4]]) { a.flat(ell(x, y0 - .8, .55), CRIMSON.base); a.tube(`M${x} ${y0}L${x + .1} 11.6`, .75, { base: CRIMSON.base, shade: CRIMSON.base }, { bare: true, spec: false }); }
  a.flat(ell(4.2, 2.9, .4), IRON.lite);
  // The tower, with its yellow window lights; it leans, as hers would.
  sheet(a, [9.4, 1.6, 17, 1.4, 16.8, 12, 9.6, 12.3], poly(9.4, 11.2, 17, 11, 17, 12.2, 9.4, 12.4));
  a.tube('M13.4 3.2L13 10.2', 1, { base: NAVY.lite, shade: NAVY.lite }, { bare: true, spec: false });
  a.tube('M11 10.4L15.2 10.2', .9, { base: NAVY.lite, shade: NAVY.lite }, { bare: true, spec: false });
  a.flat(ell(15, 3.6, .45), OCHRE.lite); a.flat(ell(15.4, 5.4, .4), OCHRE.lite); a.flat(ell(14.8, 7, .45), OCHRE.lite);
  a.flat(ell(13.2, 1.3, .4), IRON.lite);
  // The sun, four arms, four dots; the paper's corner has come loose.
  sheet(a, [18.8, 4, 26.4, 3.8, 26.8, 14, 19, 14.3], poly(18.8, 13, 26.8, 12.8, 26.8, 14.2, 18.8, 14.4));
  a.tube('M22.8 6.4L22.8 11.4', .9, { base: OCHRE.lite, shade: OCHRE.lite }, { bare: true, spec: false });
  a.tube('M20.4 8.9L25.2 8.9', .9, { base: OCHRE.lite, shade: OCHRE.lite }, { bare: true, spec: false });
  for (const [x, y] of [[20.6, 6.6], [25, 6.4], [20.8, 11.4], [25.2, 11.6]]) a.flat(ell(x, y, .35), OCHRE.base);
  a.flat(poly(26.4, 3.8, 26.6, 5.4, 25.2, 4.4), PAPER.deep);
  a.flat(ell(22.6, 3.6, .4), IRON.lite);
};

// Notches on the wall where somebody's height was marked, and the small pencil ticks of years between. The last one stops well short of a grown one.
const heightMarks = (a) => {
  const cut = (y, x1) => { a.flat(poly(.2, y, x1, y - .1, x1 - .2, y + .9, .2, y + 1), '#1d1815'); a.flat(poly(.2, y + 1, x1 - .2, y + .9, x1 - .4, y + 1.6, .4, y + 1.7), '#6f6a62'); };
  const tick = (y, x1) => a.flat(poly(.2, y, x1, y + .1, x1 - .2, y + .9, .2, y + .9), OCHRE.base);
  tick(.9, 4.2); cut(3, 6); tick(5.2, 3.4); cut(7.2, 6.2); tick(10.4, 3.8); cut(12.6, 6); tick(16, 1.4); cut(18.2, 6);
};

// A basket with a cloth left half over it, a little brass thing on top, a rope trailing off its side.
const basket = (a) => {
  const shell = 'M1.2 5.4L13.2 5.2L13 10C12.8 10.8 12.4 11.2 11.8 11.2L2.4 11.2C1.8 11.2 1.4 10.8 1.2 10Z';
  a.tube('M13 4.8C14.6 5.4 14.8 7 15 8.4L16 11.2', .6, BRASS, { spec: false });
  a.shape(shell, WICKER, { shade: band(1, 9.6, 14, 11.4), lite: band(1, 5.2, 14, 5.8) });
  for (let row = 0; row < 3; row++) for (let i = 0; i < 6; i++) { const x = 1.8 + i * 2 + (row % 2), y = 6.1 + row * 1.6; a.flat(poly(x, y, x + 1.3, y - .15, x + 1.4, y + .7, x + .1, y + .85), row === 2 ? WICKER.shade : WICKER.lite, { clip: shell }); }
  a.shape('M1 3.6L8.2 3.2L8.6 5.6L6.6 6.4L4.4 8L2.8 7.2L1 8Z', NAVY, { lite: band(1, 3.2, 9, 3.8), shade: 'M3 6.4L6.6 5.6L4.4 8L2.8 7.2Z', lines: ['M3.4 4.4L4 6.6'] });
  a.shape(rr(8.2, 1.2, 4.8, 3.6, .4), BRASS, { lite: band(8.2, 1.2, 13, 1.7), shade: band(8.2, 3.8, 13, 4.8), lines: ['M10.6 1.4L10.6 4.6'] });
};

// The pegs by the door: a long red coat, a shorter blue one with its buttons, a bag on a strap. Nobody came back for their coats.
const coatPegs = (a) => {
  a.tube('M18.6 4.4L17 10.6L16.4 14', .45, DARKW, { bare: true, spec: false });
  a.tube('M18.6 4.4L20 10L20.4 14', .45, DARKW, { bare: true, spec: false });
  a.shape(rr(15.2, 14, 6, 5.2, .5), DARKW, { lite: band(15.2, 14, 21.2, 14.5), shade: band(15.2, 18, 21.2, 19.2), lines: ['M15.6 15.8L20.8 15.8'] });
  a.flat(ell(18.2, 16.4, .4), BRASS.lite);
  a.shape('M3.2 5.2L6.2 5.2L6.4 29.4L5.4 29.2L5 30.4L4 29.4L3 30L2.8 29Z', CRIMSON, { lite: band(3, 5, 3.8, 30), shade: band(5.4, 5, 6.6, 30), lines: ['M4.6 8L4.7 16', 'M4.5 19L4.6 28'] });
  a.shape('M9 6.2L14.4 6.2C15 8 15 11 15 14L15.2 21.2L8.4 21.2L8.4 14C8.4 11 8.4 8 9 6.2Z', NAVY, { lite: 'M9 6.4L9.8 6.4L9.4 21L8.6 21Z', shade: 'M13.4 6.4L14.4 6.2L15.2 21.2L13.4 21.2Z', lines: ['M11.8 7.4L11.8 20.8', 'M9.4 16L10.6 16.4'] });
  for (const y of [9, 12.2, 15.4]) a.flat(ell(12.6, y, .35), OCHRE.lite);
  // The rail, its three pegs.
  for (const x of [4.6, 11.6, 18.6]) a.flat(rr(x - .6, 3.4, 1.2, 2.2, .3), SOOT.base);
  a.shape(poly(1, 1.2, 21, 1, 21, 3.8, 1, 4), SOOT, { lite: band(1, 1, 21, 1.5) });
  a.flat(poly(2, 2, 20, 1.9, 20, 2.6, 2, 2.7), IRON.base);
};

// The tool board: a hammer and pliers still on it, the clean shape on the board where the third was taken, a small toolbox.
const BOARD = { base: '#5f4a38', shade: '#4a3a2e', deep: '#352a23', lite: '#725a44', spec: '#8a7058' };
const toolRack = (a) => {
  a.shape(band(1, 2.2, 33, 24), DARKW, { lite: band(1, 2.2, 33, 2.8), shade: [band(31.8, 2.2, 33, 24), band(1, 23, 33, 24)] });
  a.shape(band(2.6, 3.8, 31.4, 22.6), BOARD, { outer: false, edge: true, shade: band(2.6, 20.6, 31.4, 22.6), lines: ['M3 9.6L10 9.6', 'M16 12.2L18 12.2', 'M3 16.4L9 16.4', 'M22 8L30.6 8', 'M16.4 20L23 20'] });
  // The shape of what was taken: paler where the wall kept out the years.
  a.flat('M17.4 8.4L26.8 8.4L27 9.8L23 10L22.6 11.4L20.8 11.4L20.8 15L19.4 15L19.4 11.4L17.4 10.8Z', BOARD.lite);
  // The hammer.
  a.shape(band(5.4, 8, 7.2, 19.2), COPPER, { lite: band(5.4, 8, 5.9, 19.2), shade: band(6.6, 8, 7.2, 19.2), lines: ['M6.2 11L6.3 16'] });
  a.shape(poly(3.2, 5.2, 9.4, 5.2, 9.4, 8.2, 3.2, 8.2), SOOT, { lite: band(3.2, 5.2, 9.4, 5.7) });
  a.flat(band(4.2, 6.4, 8.4, 7), IRON.lite);
  // The pliers.
  a.tube('M11.8 18.8L13 12.6L14.2 5.4', .95, IRON, { spec: false });
  a.tube('M14.6 18.8L13.2 12.6L11.8 5.4', .95, IRON, { spec: false });
  stud(a, 13.1, 12.7);
  // The small box.
  a.shape(rr(24, 14.2, 6.2, 7, .4), COPPER, { lite: band(24, 14.2, 30.2, 14.7), shade: band(24, 20, 30.2, 21.2) });
  a.flat(band(25.2, 16.6, 29.2, 18.6), SOOT.base);
  a.flat(band(25.8, 17.3, 28.6, 17.9), IRON.lite);
  dust(a, 2.4, [3, 8, .5], [11, 13, .4], [15, 20, .5], [23, 25, .4], [27, 31, .5]);
};

Subjects.homeswall = function () {
  const pic = (name, w, h, draw, up) => out(`homes/${name}`, bake({ w, h, draw, shift: up ? [0, -up] : undefined }));
  pic('mural_0', 74, 35, mural); out('homes/mural_0_glow', muralGlow);
  pic('rug_0', 47, 4, rug, .4);
  pic('tapestry_0', 52, 31, tapestry);
  pic('banner_0', 18, 36, banner0); pic('banner_1', 18, 36, banner1);
  pic('curtain_0', 16, 49, curtain);
  pic('holoPortrait_0', 23, 19, holoPortrait(true)); pic('holoPortrait_0_dark', 23, 19, holoPortrait(false)); out('homes/holoPortrait_0_glow', holoGlow);
  pic('toys_0', 28, 12, toys); out('homes/toys_0_glow', toysGlow);
  pic('clock_0', 17, 28, clock);
  pic('musicBox_0', 14, 12, musicBox); out('homes/musicBox_0_glow', musicBoxGlow);
  pic('herbs_0', 13, 19, herbs);
  pic('frame_0', 26, 21, frame);
  pic('drawings_0', 28, 15, drawings);
  pic('heightMarks_0', 7, 21, heightMarks);
  pic('basket_0', 17, 12, basket, .6);
  pic('coatPegs_0', 22, 31, coatPegs);
  pic('toolRack_0', 34, 25, toolRack);
};
})();

// The arc makers' homes (ADR 0091), left as they were: what they sat on, slept in and kept. Every piece placed by hand, dust where it settled; each state differs only by the part that changes.
(function () {
'use strict';
const { ell, rr, poly, bake, canvas, P } = Anime;
const band = (x0, y0, x1, y1) => poly(x0, y0, x1, y0, x1, y1, x0, y1);

const WOOD = { base: '#6f4b31', shade: '#4b3225', deep: '#30201a', lite: '#8d6b49', spec: '#a68a66' };
const DARKW = { base: '#4e3324', shade: '#35231b', deep: '#231714', lite: '#6a4a33', spec: '#86684a' };
const PALEW = { base: '#8d7355', shade: '#66503c', deep: '#46372b', lite: '#a78f70', spec: '#bda888' };
const IRON = { base: '#4d626c', shade: '#34454f', deep: '#222e36', lite: '#71878f', spec: '#a3b6bc' };
const SOOT = { base: '#232c33', shade: '#171e24', deep: '#0f1418', lite: '#384550', spec: '#5d6d78' };
const STONE = { base: '#5b5560', shade: '#433e49', deep: '#2d2933', lite: '#756f79', spec: '#928d94' };
const COPPER = { base: '#a06a35', shade: '#6d4527', deep: '#472d1e', lite: '#c08c50', spec: '#dcb27c' };
const BRASS = { base: '#94773a', shade: '#67512a', deep: '#45361f', lite: '#b39755', spec: '#d0b87c' };
const CLAY = { base: '#8a5c42', shade: '#624031', deep: '#422b24', lite: '#a67a5a', spec: '#bd9674' };
const TEAL = { base: '#3e7a78', shade: '#2a5658', deep: '#1c3c40', lite: '#5f9c96', spec: '#a0cfc4' };
const CRIMSON = { base: '#73302f', shade: '#511f23', deep: '#361419', lite: '#8f4a44', spec: '#a8685c' };
const NAVY = { base: '#34406f', shade: '#252e54', deep: '#181e3a', lite: '#4c5a8b', spec: '#7886a8' };
const OCHRE = { base: '#8a6a30', shade: '#624a24', deep: '#42311b', lite: '#a88a48', spec: '#c4a868' };
const LINEN = { base: '#a59a84', shade: '#7a7162', deep: '#575146', lite: '#bdb29c', spec: '#d2c8b4' };
const PAPER = { base: '#a89a78', shade: '#7f735a', deep: '#5a5240', lite: '#c0b290', spec: '#d4c8a8' };
const WICKER = { base: '#6f5a40', shade: '#4d3e2e', deep: '#332921', lite: '#8f7856', spec: '#a8926c' };
const DUST = '#807c76', DUSTLITE = '#9a968e';
const BURN = '#9ad6ff', BURNHOT = '#e8f6ff';

/** A board: a kinked four-sided piece, lit along its top, shaded along its foot; grain and scars by hand. */
function board(a, x0, y0, x1, y1, m, o) {
  o = o || {};
  const k = o.kink || [0, 0, 0, 0], l = o.l ?? Math.min(.8, (y1 - y0) * .22), b = o.b ?? Math.min(1, (y1 - y0) * .26);
  a.shape(poly(x0 + k[0], y0, x1, y0 + k[1], x1 - k[2], y1, x0, y1 - k[3]), m, { outer: o.outer, lite: o.lite ?? band(x0, y0, x1, y0 + l), shade: o.shade ?? band(x0, y1 - b, x1, y1), deep: o.deep, lines: o.lines, spec: o.spec });
}
/** A post: lit down its left, shaded down its right, as the light falls from the upper left. */
function post(a, x0, y0, x1, y1, m, o) {
  o = o || {};
  const w = x1 - x0, k = o.kink || [0, 0, 0, 0];
  a.shape(poly(x0 + k[0], y0, x1 + k[1], y0, x1 + k[2], y1, x0 + k[3], y1), m, { outer: o.outer, lite: band(x0 - 1, y0, x0 + w * .28, y1), shade: band(x1 - w * .34, y0, x1 + 1, y1), deep: o.deep, lines: o.lines });
}
/** Dust settled on a top: thin uneven lies of it, only where something could hold it, each [x0, x1] by hand on line y. */
function dust(a, y, ...runs) {
  for (const [x0, x1, h] of runs) {
    // Lying flat along the surface, its far edge wavering a little: a heap of it reads as snow.
    const t = (h || .7) * .5, m = (x0 + x1) / 2, q = (x1 - x0) * .22;
    a.flat(poly(x0, y + .25, x0 + q, y - t * .7, m - q * .4, y - t * .45, m + q * .5, y - t, x1 - q, y - t * .55, x1, y + .25), DUST);
  }
}
const scar = (a, ...lines) => { for (const l of lines) a.line(l, { w: .9 }); };
/** A nail or small fitting. */
const stud = (a, x, y, c) => { a.flat(ell(x, y, .42), '#1c2125'); a.flat(ell(x - .08, y - .1, .2), c || '#8b9a9f'); };

/** A picture's own light, added over it unlit: a hard shape where the pixel glow stood, a short breath round it. */
function shine(w, h, draw) {
  const c = canvas(w * 4, h * 4), x = c.getContext('2d');
  const put = (d, col, blur) => { x.save(); x.setTransform(4, 0, 0, 4, 0, 0); x.filter = blur ? `blur(${blur}px)` : 'none'; x.fillStyle = col; x.fill(P(d)); x.restore(); };
  const pen = (d, col, w) => { x.save(); x.setTransform(4, 0, 0, 4, 0, 0); x.strokeStyle = col; x.lineWidth = w; x.lineJoin = 'round'; x.lineCap = 'round'; x.stroke(P(d)); x.restore(); };
  draw({ halo: (d, col, blur) => put(d, col, blur ?? 5), core: (d, col) => put(d, col, 0), pen });
  return c;
}

window.HomeKit = { band, board, post, dust, scar, stud, shine, WOOD, DARKW, PALEW, IRON, SOOT, STONE, COPPER, BRASS, CLAY, TEAL, CRIMSON, NAVY, OCHRE, LINEN, PAPER, WICKER, DUST, DUSTLITE, BURN, BURNHOT };

// The bed: a head post with a lamp let into it, a sagging rail, the blanket kicked to one side, the pillows still dented.
const bed = (a) => {
  // The rail and legs, under everything.
  board(a, 6.4, 16.4, 43, 19.6, WOOD, { kink: [0, .3, 0, 0], lines: ['M12 17.6L19 17.8', 'M24 18.2L33 18', 'M37 17.5L41 17.7'] });
  post(a, 6, 19.4, 10.2, 23, DARKW, { kink: [0, 0, -.2, .2] });
  post(a, 38.8, 19.4, 42.4, 23, DARKW, { kink: [0, 0, .2, 0] });
  // The foot post, with its dusty cap.
  post(a, 43, 10, 48.2, 23, WOOD, { kink: [0, -.2, 0, 0], lines: ['M45 13L45.2 19', 'M46.4 15L46.3 21.6'] });
  board(a, 41.8, 8.2, 49.6, 10.6, DARKW, { kink: [.2, 0, .2, 0] });
  dust(a, 8.2, [43, 45.4, .7], [46.4, 48.6, .8]);
  // The mattress and its ticking, flat under the pillows.
  a.shape(poly(7, 13.4, 19, 13, 19, 16.6, 7, 16.8), LINEN, { lite: band(7, 13, 19, 13.8), shade: band(7, 15.8, 19, 17), lines: ['M8.6 15.2L17.4 15', 'M8.2 16.1L12 16'] });
  // Two pillows, the upper fallen in where a head lay.
  a.shape('M8 13.6C7.6 12.2 8 10.9 9.2 10.8C11.6 10.4 13 10.2 15.2 10.6C17.4 10.8 18.8 11.2 18.6 12.6C18.6 13.2 18.4 13.4 18 13.6Z', LINEN, { shade: [band(8, 12.8, 19, 14)], lite: 'M9 11.2L13 10.7L13 11.3L9 11.8Z', lines: ['M11.2 11.4C12.4 12.2 14 12.2 15.4 11.5'] });
  a.shape('M8.6 10.9C8.6 10 9.4 9.4 10.6 9.4L15 9.4C16.2 9.4 16.6 10 16.4 10.8L12.4 11.2Z', { ...LINEN, base: '#8e8473', lite: '#a59a84' }, { shade: band(8, 10.5, 17, 11.2) });
  // The blanket: heavy navy, thrown back, one fold over the foot, its woven stripe gone in places.
  a.shape('M18.6 11.2C22 10.4 28 10.6 33 10.3C37 10.2 40.4 10.3 42.6 10.6C43.2 12.6 43.3 15 42.8 17.4L18.2 17.2C17.8 15.2 18 13 18.6 11.2Z', NAVY, {
    lite: 'M19.2 11.3C24 10.8 32 10.8 41 10.7L41 11.5C32 11.5 24 11.6 19.2 12Z', shade: ['M18.2 15.4L43 15.7L42.8 17.6L18.2 17.4Z', 'M39.6 10.4L43.4 10.4L43.2 17.6L40.4 17.6C41 15 40.6 12.4 39.6 10.4Z'],
    deep: 'M25 16.4C26.4 15.9 29 15.8 31 16.3L31 17.4L25 17.4Z',
    lines: ['M21.4 11.8C21 13.4 21.2 15 21.8 16.8', 'M30.2 11.2C30.6 12 30.8 12.6 30.4 13.4', 'M36 11C36.4 12.6 36.2 15 35.6 16.6'] });
  for (const [x0, x1] of [[21.6, 24.4], [25.4, 28.2], [29, 31.2], [32.4, 35], [36, 37.4], [38.2, 40]]) a.flat(poly(x0, 13.5, x1, 13.4, x1, 14.2, x0, 14.3), OCHRE.base);
  dust(a, 10.6, [20, 23, .7], [24.6, 26, .6], [27.8, 30.6, .8], [33, 35.4, .7]);
  // The head post last, in front of the pillows: the lamp's slot, its cap with a crust of dust.
  post(a, 1.8, 3, 7.2, 23, WOOD, { lines: ['M2.8 11L3 19', 'M5.6 12.4L5.4 22', 'M3.6 21L4.6 22.6'] });
  board(a, .8, 1, 8.2, 3.6, SOOT, { kink: [.2, 0, 0, 0] });
  a.shape(rr(3.5, 5.3, 2, 4.4, .3), SOOT, { outer: false, edge: true });
  a.flat(rr(3.9, 5.8, 1.1, 3.4, .25), BURN);
  a.flat(band(4.25, 6.1, 4.7, 8.8), BURNHOT);
};
const bedGlow = shine(50, 24, (g) => { g.halo(rr(3.4, 5.2, 2.2, 4.6, .4), 'rgba(120,200,255,.5)', 8); g.core(rr(4, 5.9, 1, 3.2, .3), '#bfe6ff'); });

// The table: heavy planks on the makers' iron legs, its stretcher's little lamp long out but for a speck.
const table = (a) => {
  // The iron under it.
  post(a, 3.4, 7, 6.8, 19.4, IRON, { lines: ['M4.4 9L4.4 18'] });
  post(a, 27.4, 7, 30.8, 19.4, IRON, { lines: ['M28.6 9.4L28.6 17'] });
  board(a, 6.8, 13.9, 27.4, 15.2, SOOT, { outer: false, lite: band(6.8, 13.9, 27.4, 14.2) });
  a.flat(band(16.8, 14, 18.2, 15), '#35648e');
  board(a, 2.2, 19, 8, 20.6, SOOT, { kink: [.3, 0, 0, 0] });
  board(a, 26.2, 19, 32, 20.6, SOOT, { kink: [0, 0, .3, 0] });
  // The skirt, the top; a corner of it chipped, its planks shrunk apart.
  board(a, 3.2, 6.6, 31, 8.8, DARKW, {});
  board(a, 1, 2.2, 33, 6.8, WOOD, {
    kink: [.5, 0, 0, 0], lite: band(1, 2.2, 33, 3), shade: band(1, 5.6, 33, 7),
    lines: ['M1.4 4.3L9 4.4', 'M11.2 4.3L21.6 4.2', 'M23.2 4.4L32.6 4.3', 'M12.6 2.5L12.3 4.2', 'M24 4.5L24.4 6.6', 'M5.8 3.2L8 3.3', 'M16 5.3L19.6 5.3', 'M27 3.2L30 3.1'] });
  dust(a, 2.3, [2, 5.4, .8], [9, 11.4, .7], [12.8, 14.2, .6], [16, 17.6, .7], [18.8, 22.6, .9], [24, 25.6, .6], [27.2, 28.8, .7], [30, 32.4, .8]);
};

// A chair by which way it faces: its back on the side away from the table. One sat in and left pushed out, its pad worn through.
const chair = (flip, torn) => (a) => {
  const X = (x) => (flip ? 14 - x : x), R = (x0, y0, x1, y1) => (flip ? [14 - x1, y0, 14 - x0, y1] : [x0, y0, x1, y1]);
  const P4 = (x0, y0, x1, y1, m, o) => post(a, ...R(x0, y0, x1, y1), m, o);
  // The far leg and stretcher first.
  P4(10.2, 15, 12.6, 24.6, DARKW, { kink: flip ? [.2, 0, -.2, 0] : [0, -.2, 0, .2] });
  a.shape(band(...R(3.6, 20, 10.4, 21.2)), DARKW, { outer: false, lite: band(0, 20, 14, 20.3) });
  // The back: its post, two rungs, one of the second chair's sprung loose at its end.
  P4(1.2, 2, 4.2, 24.6, WOOD, { lines: [`M${X(2.2)} 5L${X(2.3)} 11`, `M${X(3.2)} 16L${X(3.1)} 22.4`] });
  if (torn) {
    a.shape(poly(X(4), 5, X(7), 5.5, X(7), 6.9, X(4), 6.7), DARKW, {});
    a.shape(poly(X(4), 8.4, X(6.4), 9.4, X(6.2), 10.5, X(4), 9.8), DARKW, {});
  } else {
    a.shape(poly(X(4), 5, X(6.6), 5, X(6.6), 6.8, X(4), 6.8), DARKW, {});
    a.shape(poly(X(4), 8.2, X(6.6), 8.2, X(6.6), 10, X(4), 10), DARKW, {});
  }
  dust(a, 2.2, flip ? [10, 13, .8] : [1.2, 4, .8]);
  // The seat and its pad, sunk where someone sat.
  a.shape(poly(...R(1, 13.2, 13, 13.2).slice(0, 2), R(1, 13.2, 13, 13.2)[2], 13.2, R(1, 13.2, 13, 13.2)[2], 15.6, R(1, 13.2, 13, 13.2)[0], 15.6), WOOD, { lite: band(0, 13.2, 14, 13.9), shade: band(0, 14.9, 14, 15.8), lines: [`M${X(5)} 14.5L${X(11)} 14.5`] });
  a.shape(`M${X(4.2)} 13.2C${X(4)} 11.8 ${X(5)} 11 ${X(6.4)} 11.2C${X(8)} 11.7 ${X(9)} 11.7 ${X(10)} 11.2C${X(11)} 11 ${X(11.4)} 12 ${X(11.3)} 13.2Z`, CRIMSON, { shade: band(0, 12.4, 14, 13.4), lite: `M${X(5)} 11.4L${X(6.6)} 11.5L${X(6.6)} 12L${X(5)} 11.9Z` });
  if (torn) { a.flat(poly(X(7.6), 12, X(9.6), 11.8, X(9.8), 13, X(8), 13.2), LINEN.shade); a.line(`M${X(7.6)} 12L${X(8.4)} 13.2`, { w: .9 }); } else a.line(`M${X(6)} 12.3L${X(9.6)} 12.2`, { w: .9 });
  dust(a, 11.4, flip ? [4.4, 6.2, .6] : [8.6, 10.8, .6]);
};

const stool = (a) => {
  // Splayed legs, a rung, the top's edge worn to a curve where it was carried.
  a.shape(poly(2.4, 4.6, 4.2, 4.6, 3.6, 12.8, 1.4, 12.8), WOOD, { lite: poly(2.4, 4.6, 3, 4.6, 2, 12.8, 1.4, 12.8), shade: poly(3.6, 4.6, 4.2, 4.6, 3.6, 12.8, 3, 12.8) });
  a.shape(poly(7.8, 4.6, 9.6, 4.6, 10.6, 12.8, 8.4, 12.8), WOOD, { lite: poly(7.8, 4.6, 8.3, 4.6, 8.9, 12.8, 8.4, 12.8), shade: poly(9, 4.6, 9.6, 4.6, 10.6, 12.8, 10, 12.8), lines: ['M8.8 7L9.2 10'] });
  a.shape(poly(3.7, 7.6, 8.3, 7.6, 8.4, 8.6, 3.6, 8.7), DARKW, { outer: false });
  a.shape('M1.4 2.3L10.6 2.2C11 2.8 11 4 10.5 4.9L1.6 5C1 4.1 1 3 1.4 2.3Z', WOOD, { lite: band(1, 2.2, 11, 3), shade: band(1, 4.1, 11, 5.2), lines: ['M3 3.5L7.6 3.6'] });
  dust(a, 2.3, [2, 4.8, .8], [6.4, 8, .6]);
};

// The bench and the folded cloth left on it, one end slid off and hanging.
const bench = (a) => {
  post(a, 3.2, 6.4, 6, 15.4, DARKW, { kink: [0, 0, -.2, .2] });
  post(a, 26, 6.4, 28.8, 15.4, DARKW, { kink: [0, 0, .3, 0] });
  a.shape(poly(6, 10.1, 26, 9.9, 26, 11, 6, 11.2), DARKW, { outer: false });
  a.shape(poly(.8, 4.4, 31.2, 4.2, 31.2, 6.8, .8, 7), PALEW, { lite: band(0, 4.2, 32, 4.9), shade: band(0, 6, 32, 7), lines: ['M2 5.5L12 5.6', 'M14.6 5.5L19 5.4', 'M23 5.6L29.8 5.5'] });
  a.shape('M17.8 1.6L28.2 1.2C28.8 2 28.8 3.4 28.4 4.3L18 4.4C17.6 3.4 17.4 2.4 17.8 1.6Z', SOOT, { lite: band(17, 1.2, 29, 1.7) });
  a.shape('M18.8 2.4L27.4 2.1L27.5 3.3L18.8 3.5Z', CRIMSON, { outer: false, lite: band(18, 2.1, 28, 2.5), shade: band(18, 3, 28, 3.6) });
  a.shape('M26.4 4.1L28.2 4.1L28.1 7.6L27.2 8.2L26.5 7.4Z', CRIMSON, { lite: band(26.4, 4, 26.9, 8), shade: band(27.7, 4, 28.4, 8) });
  dust(a, 4.5, [2.4, 6.6, .8], [8.8, 10.2, .6], [11.8, 14.2, .8]);
};

// The wardrobe: one door shut and warped, the other hanging open on the dark, and the one coat they left.
const wardrobe = (a) => {
  post(a, 3, 44.6, 7.2, 48.6, SOOT, {});
  post(a, 21, 44.6, 25.2, 48.6, SOOT, {});
  // The carcass, the dark inside it, the rail.
  a.shape(band(2, 4.6, 26.2, 45.2), DARKW, { lite: band(2, 4.6, 26, 5.2), shade: band(2, 43.6, 26.2, 45.2) });
  a.flat(band(15, 6.6, 25.2, 43.6), '#14100f');
  a.flat(band(15, 6.6, 18, 43.6), '#0d0a0a');
  a.line('M15.6 9.2L25 9', { w: 2.6, c: IRON.shade });
  // The coat, on its hanger, one shoulder fallen.
  a.tube('M18.6 9.2L19 10.4', .5, IRON, { bare: true, spec: false });
  a.shape('M16.9 12.2C17.4 10.9 18.2 10.4 19 10.3C19.8 10.4 20.6 11 21 12C21.5 18 21.5 26 21.2 32.4C20.4 33 18 33 17 32.2C16.5 26 16.5 18 16.9 12.2Z', CRIMSON, {
    lite: 'M17.4 12L18 12L17.7 31L17 31Z', shade: 'M19.8 11L21.2 12L21.5 32.6L20.2 32.8Z', lines: ['M19.1 13L19.2 31.6', 'M18.2 19.4L18.5 22', 'M20.4 25L20.2 28'] });
  // The open door, seen on its edge.
  post(a, 25.4, 6.8, 27.4, 42.4, WOOD, { kink: [0, 0, 0, 0], lines: ['M26.4 12L26.4 18'] });
  // The shut door: boards, a warped foot, a pale handle.
  a.shape(poly(3, 6.2, 15, 6.2, 15, 43.8, 3.4, 44), WOOD, {
    lite: band(3, 6.2, 15, 7), shade: [band(13.8, 6.2, 15.2, 44), band(3, 42.8, 15, 44.2)],
    lines: ['M6.8 6.6L6.9 22', 'M7 25L6.8 43.6', 'M10.8 6.6L10.6 16', 'M10.9 19L10.7 43.6', 'M4 10.5L6.2 10.6', 'M8 15L10 15.1', 'M4 29.6L6 29.7', 'M8 33.5L10.2 33.4', 'M11.4 12L13.4 12.1', 'M11.4 38L13.2 38'] });
  a.shape(rr(12, 21, 1.1, 5.8, .3), IRON, { outer: false, lite: band(12, 21, 12.4, 27) });
  // The cornice, iron, with its pale edge; the dust on top is thick.
  board(a, 1, 2.2, 27, 4.9, SOOT, { kink: [0, 0, 0, 0], lite: band(1, 2.2, 27, 2.6) });
  a.flat(poly(2.4, 3, 25.6, 2.9, 25.6, 3.5, 2.4, 3.6), IRON.lite);
  dust(a, 2.3, [3, 7, 1], [10, 11.6, .7], [13.6, 15.2, .8], [17.6, 20, 1], [21.4, 25.2, .8]);
};

// The chest: an iron-strapped box, lid shut; the lock's little light is all that is different when it dies.
const chest = (lit) => (a) => {
  a.shape(band(1.2, 7, 20.8, 15.6), WOOD, { lite: band(1, 7, 21, 7.5), shade: band(1, 14.2, 21, 15.8), lines: ['M1.6 10.2L8.4 10.3', 'M13.4 10.2L20.4 10.1', 'M1.6 12.8L20.4 12.7', 'M5.8 8.4L8 8.4', 'M15 11.4L16.4 11.4'] });
  a.shape('M1 7.2C1 4.6 1.6 3 3 2.4L19 2.4C20.4 3 21 4.6 21 7.2Z', DARKW, { lite: 'M3 2.5L19 2.5L19.6 3.2L2.4 3.2Z', shade: band(1, 6.2, 21, 7.4), lines: ['M1.6 4.8L20.4 4.7', 'M8 3.8L10.6 3.8'] });
  for (const x of [4, 17]) a.shape(poly(x, 2.2, x + 1.2, 2.2, x + 1.2, 15.6, x, 15.6), IRON, { outer: false, edge: true, lite: band(x, 2, x + .4, 16), lines: [`M${x} 7.2L${x + 1.2} 7.2`] });
  stud(a, 4.6, 9.6); stud(a, 4.6, 13.6); stud(a, 17.6, 9.6); stud(a, 17.6, 13.6);
  a.shape(rr(9, 6.8, 4, 4.4, .4), SOOT, { lite: band(9, 6.8, 13, 7.2) });
  a.flat(rr(10, 8.5, 2, 1.2, .2), lit ? BURN : '#1c3346');
  if (lit) a.flat(band(10.3, 8.8, 11.6, 9.3), BURNHOT); else a.flat(band(10.3, 8.8, 11.6, 9.3), '#25425a');
  dust(a, 2.5, [2, 3.8, .7], [6, 9, .8], [11, 12.4, .6], [14, 16.6, .8], [18.4, 20, .6]);
};
const chestGlow = shine(22, 17, (g) => { g.halo(rr(9.6, 8, 2.8, 2.2, .4), 'rgba(120,200,255,.42)', 5); g.core(rr(10.1, 8.7, 1.8, .9, .2), '#cfeeff'); });

// The cradle: a woven basket on rockers; over it a frame that hung a mobile, which is still lit, or not. Someone's folded cloth lies in it.
const cradle = (lit) => (a) => {
  // The hanging frame, behind.
  a.shape(poly(6, .2, 20.2, .2, 20.2, 1.6, 7.4, 1.6, 7.4, 2.6, 6, 2.6), SOOT, { lite: band(6, .2, 20, .6) });
  post(a, 18.8, 1.4, 20.2, 14, SOOT, {});
  // The strings, whole or cut.
  for (const [x, y] of [[8, 5], [14, 6], [11, 8], [17, 9]]) a.line(`M${x + .3} 1.5L${x} ${y}`, { w: .9, c: IRON.lite });
  // The rockers and legs.
  a.shape('M.6 25.6C2 26 3.6 27.6 5.4 28.2L16.4 28.2C18.2 27.6 20 26 21.4 25.6L21.6 26.8C20 27.4 18.4 29 16.6 29.6L5.2 29.6C3.6 29 2 27.4 .4 26.8Z', DARKW, { lite: 'M5.4 28.2L16.4 28.2L16.2 28.6L5.6 28.6Z', lines: ['M8 29L12 29'] });
  post(a, 5, 22.6, 7.2, 28.4, DARKW, {});
  post(a, 14.8, 22.6, 17, 28.4, DARKW, {});
  // The basket, hand woven, sagging; a broken withe at its lip.
  a.shape('M2 14C7 14.3 14 14.3 19.8 14L19.5 21.2C19.3 22.4 18.8 23.2 18 23.4L4 23.4C3 23.2 2.5 22.4 2.3 21.2Z', WICKER, { shade: 'M2 21L20 21L19 23.6L3 23.6Z', lite: band(2, 14, 20, 14.8) });
  const clip = 'M2 14C7 14.3 14 14.3 19.8 14L19.5 21.2C19.3 22.4 18.8 23.2 18 23.4L4 23.4C3 23.2 2.5 22.4 2.3 21.2Z';
  for (let row = 0; row < 5; row++) for (let i = 0; i < 9; i++) {
    const x = 2.2 + i * 2 + (row % 2) * 1, y = 14.8 + row * 1.7;
    a.flat(poly(x, y, x + 1.4, y - .2, x + 1.5, y + .8, x + .1, y + 1), row > 2 ? WICKER.shade : WICKER.lite, { clip });
  }
  a.line('M17.4 16.6L18.6 15.4', { w: 1.1 });
  // The lip: its rim, the little pillow, the blanket's edge.
  a.shape('M2 13.2L20 13.1L20 14.5L2 14.6Z', STONE, { lite: band(2, 13.1, 20, 13.5) });
  a.flat(poly(4.2, 13.3, 19, 13.2, 19, 13.9, 4.2, 14), NAVY.base);
  a.shape('M4 13.2C4 12.2 4.8 11.8 5.8 11.8C6.8 11.8 7.4 12.3 7.4 13.2Z', LINEN, { shade: band(4, 12.8, 8, 13.4) });
  // The mobile's four pieces, hanging last, so only they change.
  for (const [x, y] of [[8, 6], [14, 7], [11, 9], [17, 10]]) {
    if (lit) { a.flat(poly(x, y - 1.6, x + .55, y - .55, x + 1.6, y, x + .55, y + .55, x, y + 1.6, x - .55, y + .55, x - 1.6, y, x - .55, y - .55), BURN); a.flat(ell(x, y, .5), BURNHOT); }
    else { a.flat(poly(x, y - 1.3, x + .5, y - .5, x + 1.3, y, x + .5, y + .5, x, y + 1.3, x - .5, y + .5, x - 1.3, y, x - .5, y - .5), IRON.shade); a.flat(ell(x, y, .4), SOOT.base); }
  }
};
const cradleGlow = shine(22, 31, (g) => {
  for (const [x, y] of [[8, 6], [14, 7], [11, 9], [17, 10]]) {
    g.halo(ell(x, y, 2), 'rgba(120,200,255,.5)', 6);
    g.core(poly(x, y - 1.5, x + .5, y - .5, x + 1.5, y, x + .5, y + .5, x, y + 1.5, x - .5, y + .5, x - 1.5, y, x - .5, y - .5), '#bfe6ff');
  }
});

Subjects.homes = function () {
  const pic = (name, w, h, draw, up) => out(`homes/${name}`, bake({ w, h, draw, shift: up ? [0, -up] : undefined }));
  pic('bed_0', 50, 24, bed); out('homes/bed_0_glow', bedGlow);
  pic('table_0', 34, 21, table, .8);
  pic('chair_0', 14, 25, chair(false, false), .8); pic('chair_1', 14, 25, chair(true, true), .8);
  pic('stool_0', 12, 14, stool);
  pic('bench_0', 32, 16, bench, .6);
  pic('wardrobe_0', 28, 49, wardrobe, .8);
  pic('chest_0', 22, 17, chest(true)); pic('chest_0_dark', 22, 17, chest(false)); out('homes/chest_0_glow', chestGlow);
  pic('cradle_0', 22, 31, cradle(true)); pic('cradle_0_dark', 22, 31, cradle(false)); out('homes/cradle_0_glow', cradleGlow);
};
})();

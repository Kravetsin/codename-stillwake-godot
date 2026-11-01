// What the ship is fitted with: pipes, valves, sockets, hatches, launch decks, floor and ladder; the drone; the hero's tools.
(function () {
'use strict';
const { ell, rr, poly, bake, canvas } = Anime;
const { STEEL, GUN, DARK, band } = window.AnimeKit;
const G = { lite: '#868fa2', base: '#5c6474', shade: '#383c58', ink: '#14162a' };

/** A pipe by its arms (up 1, right 2, down 4, left 8), one cell, in crisp final pixels so runs meet exactly at cell borders. */
function pipe(open) {
  const S = 32, c = canvas(S, S), x = c.getContext('2d'), R = 10, r = 6, m = S / 2;
  const across = (y0, h) => { x.fillStyle = G.ink; x.fillRect(y0.x, y0.y, y0.w, y0.h); };
  const horiz = (x0, x1) => { x.fillStyle = G.ink; x.fillRect(x0, m - r - 2, x1 - x0, 2 * r + 4); x.fillStyle = G.base; x.fillRect(x0, m - r, x1 - x0, 2 * r); x.fillStyle = G.lite; x.fillRect(x0, m - r, x1 - x0, 3); x.fillStyle = G.shade; x.fillRect(x0, m + r - 4, x1 - x0, 4); };
  const vert = (y0, y1) => { x.fillStyle = G.ink; x.fillRect(m - r - 2, y0, 2 * r + 4, y1 - y0); x.fillStyle = G.base; x.fillRect(m - r, y0, 2 * r, y1 - y0); x.fillStyle = G.lite; x.fillRect(m - r, y0, 3, y1 - y0); x.fillStyle = G.shade; x.fillRect(m + r - 4, y0, 4, y1 - y0); };
  if (open & 8) horiz(0, m);
  if (open & 2) horiz(m, S);
  if (open & 1) vert(0, m);
  if (open & 4) vert(m, S);
  // The joint over the arms, only where a run turns or ends: a straight run stays one line.
  if (open !== 5 && open !== 10) {
    x.fillStyle = G.ink; x.fillRect(m - R - 2, m - R - 2, 2 * R + 4, 2 * R + 4);
    x.fillStyle = G.base; x.fillRect(m - R, m - R, 2 * R, 2 * R);
    x.fillStyle = G.lite; x.fillRect(m - R, m - R, 2 * R, 4);
    x.fillStyle = G.shade; x.fillRect(m - R, m + R - 4, 2 * R, 4); x.fillRect(m + R - 4, m - R, 4, 2 * R);
  }
  return c;
}
const valve = (open) => (a) => {
  const m = open ? { base: '#3fae5c', shade: '#256a3c', lite: '#86e08c', spec: '#e0ffe0', ink: '#0d2a18' } : { base: '#d0483c', shade: '#8a2c30', lite: '#f08c74', spec: '#ffe0d0', ink: '#2c0c10' };
  a.shape(ell(5, 5, 4), m, { shade: .7, lite: ell(4.5, 4.4, 2.6) });
  a.flat(ell(5, 5, 2.2), m.shade);
  a.flat(ell(5, 5, 1.2), m.base);
  a.flat(ell(4.8, 4.8, .5), m.spec);
};
const socket = (a) => { a.shape(ell(4, 4, 3.2), DARK, { outer: true, lite: .0001 }); a.flat(ell(4, 4, 1.6), '#12131e'); a.flat(band(2.6, 3.6, 5.4, 4.4), '#4f9cbc'); };
const powerSocket = (a) => { a.shape(ell(2.5, 2.5, 2), { base: '#868fa2', shade: '#474c68', lite: '#c4ccd8', spec: '#ffffff' }, { shade: .4 }); a.flat(ell(2.5, 2.5, 1), '#12131e'); };

/** A hatch segment by what it joins (right 2, left 8): its frame, the same open and shut; the plate between, only shut, so FineSlid finds exactly it. */
function hatch(links, shut) {
  const c = canvas(64, 64), x = c.getContext('2d');
  x.setTransform(4, 0, 0, 4, 0, 0);
  const R = (a, y, b, h, col) => { x.fillStyle = col; x.fillRect(a, y, b - a, h); };
  const left = links & 8 ? 0 : 1, right = links & 2 ? 16 : 15;
  if (shut) {
    R(left, 1, right, 6, '#d97f3a');
    R(left, 1, right, 1.25, '#f6b06a');
    R(left, 5.25, right, 1.75, '#a9542a');
    for (const px of [3, 8, 13]) R(px, 3, px + 1.5, 1, '#6c3a30');
  }
  R(0, 0, 16, 1, '#160c08');
  R(0, 7, 16, 1, '#160c08');
  R(0, .25, 16, .5, '#7a5236');
  if (!(links & 8)) { R(0, 0, 1, 8, '#160c08'); R(.25, .25, .5, 7.5, '#7a5236'); }
  if (!(links & 2)) { R(15, 0, 1, 8, '#160c08'); R(15.25, .25, .5, 7.5, '#3d2519'); }
  return c;
}
/** A launch deck piece: studs along its top, ended where it joins nothing; open, its middle gapes. */
const deck = (links, open) => (a) => {
  a.shape(band(0, 1.2, 16, 7), GUN, { outer: false, lite: band(0, 1.2, 16, 2), shade: band(0, 5.4, 16, 7) });
  a.flat(band(0, 7, 16, 8), '#14162a');
  a.flat(band(0, .8, 16, 1.2), '#14162a');
  for (const x of [2.4, 7, 11.6]) { a.flat(band(x, .2, x + 2.4, 1.2), '#f0c43c'); a.flat(band(x, .2, x + 2.4, .5), '#fff0b0'); }
  a.flat(band(0, 3.2, 16, 3.8), '#868fa2');
  if (open) a.flat(band(0, 2.2, 16, 6), '#12131e');
  if (!(links & 8)) { a.shape(rr(0, 0, 1.8, 8, .2), DARK, { outer: false, lite: band(0, 0, .6, 8) }); }
  if (!(links & 2)) { a.shape(rr(14.2, 0, 1.8, 8, .2), DARK, { outer: false, shade: band(15.4, 0, 16, 8) }); }
};
const ladder = (a) => {
  for (const x of [3, 11.6]) a.flat(band(x, 0, x + 1.4, 16), '#6e5436');
  for (const x of [3, 11.6]) a.flat(band(x, 0, x + .5, 16), '#d4b878');
  for (const y of [2, 6, 10, 14]) { a.flat(band(3, y, 13, y + 1.1), '#a98a4c'); a.flat(band(3, y, 13, y + .35), '#f6e6b4'); a.flat(band(3, y + 1.1, 13, y + 1.5), '#2c2018'); }
};
const floor = (a) => { a.flat(band(0, 0, 16, 4), '#14162a'); a.flat(band(0, .4, 16, 3.4), '#5c6474'); a.flat(band(0, .4, 16, 1), '#a7afbc'); a.flat(band(0, 2.6, 16, 3.4), '#383c58'); };

// The drone: rotors out on arms, a rounded hull with one eye.
const flyBlur = ['#ffffff', '#dfe7f2', '#ffffff', '#c4ccd8'];
const droneFly = (f) => (a) => {
  const span = [5, 3.6, 5, 4.2][f];
  a.tube('M3 2.4L7 6', 1.1, GUN);
  a.tube('M15 2.4L11 6', 1.1, GUN);
  for (const x of [3, 15]) { a.flat(band(x - span, .6, x + span * .5, 1.7), flyBlur[f]); a.flat(rr(x - .7, .6, 1.4, 2.2, .2), '#383c58'); }
  a.shape(rr(4.6, 4.4, 8.8, 5.4, 2), STEEL, { lite: band(5.4, 4.5, 11, 5.6), spec: band(6.2, 4.7, 8.6, 5.1), shade: band(4.6, 8, 13.4, 9.8), lines: ['M5 7.2L13 7.2'] });
  a.shape(rr(11, 4.8, 3, 2.4, .4), DARK, { outer: false });
  a.tube('M9 9.6L9 11', .9, GUN);
};
const droneDock = (a) => {
  a.shape(rr(2, 1, 10, 1.6, .3), STEEL, { lite: band(2.2, 1, 11.8, 1.5) });
  a.tube('M4 2.6L4 4.6', .8, GUN); a.tube('M10 2.6L10 4.6', .8, GUN);
  a.shape(rr(2.2, 3.6, 10, 5, 1.6), STEEL, { lite: band(3, 3.7, 9, 4.7), shade: band(2.2, 7, 12.2, 8.6) });
  a.shape(rr(9.2, 4.4, 3, 2.4, .4), DARK, { outer: false });
  for (const x of [2, 9.4]) a.shape(rr(x, 8.8, 2.6, 2.6, .3), GUN, { lite: band(x, 8.8, x + 1, 11.4) });
};
const eye = (x, y) => (a) => { a.flat(rr(x - .5, y - .5, 3, 3, .5), '#2ca4d8'); a.flat(rr(x, y, 1.6, 1.6, .3), '#a8ecff'); };
const lampOf = (c) => (a) => { a.flat(rr(6.6, 2.7, 2.8, 1.2, .3), c); };
const chargeBar = (n, low) => (a) => {
  for (let i = 0; i < 4; i++) { const x = i * 2.8; a.flat(band(x, 0, x + 2.2, 1), low ? (i === 0 ? '#ff5040' : '#3a1818') : i < n ? '#7cf06a' : '#1a3a20'); }
};

// The tools in the hand.
const WOODH = { base: '#8a6242', shade: '#5c3e30', lite: '#ad8258', spec: '#cca678', ink: '#1c1210' };
const RED = { base: '#c8402c', shade: '#84282c', deep: '#5c1c28', lite: '#ec7458', spec: '#ffd8c8', ink: '#280c10' };
const axe = (a) => {
  a.shape(rr(.4, 5.2, 21, 2.8, .7), WOODH, { lite: band(1, 5.3, 20, 6), shade: band(1, 7.2, 20, 8) });
  for (const x of [1.4, 3.4, 5.4]) a.flat(band(x, 5.2, x + 1, 8), '#262a42');
  a.shape('M18 8.6L18 4C19 2.6 21 1 24 0.4C24.2 3.2 25 7 25.6 9.8L18 9.8Z', RED, { lite: 'M19 3.8C20 2.8 21.6 1.8 23.2 1.4L23.6 4L20 5.4Z', shade: 'M23 4L25.6 9.8L22 9.8Z', spec: 'M20.2 3.2L22.2 2.1L22.2 2.5L20.4 3.6Z' });
  a.shape('M16.4 9.4L27.4 9.4C27.8 10.8 27.8 12 27.4 13.4L16.4 13.4C17 12 17 10.8 16.4 9.4Z', { base: '#dfe7f2', shade: '#868fa2', lite: '#ffffff', spec: '#ffffff', ink: '#14162a' }, { shade: band(16, 12, 28, 13.4) });
};
const drill = (spin) => (a) => {
  // Handle end, body with its orange band, chuck, and the bit; spinning, the bit is three blurred tones.
  a.shape(rr(0, 2.6, 9, 6.6, 1.2), GUN, { lite: band(.6, 2.7, 8.4, 3.6), shade: band(.6, 7.8, 8.4, 9.2) });
  a.flat(ell(1.6, 4.6, .55), spin === null ? '#54ba74' : '#a0f0a0');
  a.shape(rr(3.4, 8.8, 4, 5.6, .6), DARK, { lite: band(3.8, 8.9, 5, 14.2) });
  a.shape(rr(8.6, 1.8, 13, 7.6, .8), STEEL, { lite: band(9, 1.9, 21.2, 2.8), spec: band(10, 2.1, 15, 2.5), shade: band(9, 7.8, 21.2, 9.4) });
  for (let i = 0; i < 4; i++) a.flat(band(10.6 + i * 2.5, 4.2, 11.8 + i * 2.5, 6.8), '#c8703c');
  a.shape(poly(21.4, 2.8, 24.8, 3.8, 24.8, 7.4, 21.4, 8.4), GUN, { lite: band(21.4, 2.8, 24.8, 4.2), shade: band(21.4, 7, 24.8, 8.4) });
  const y0 = 3.6, y1 = 7.6;
  a.shape(poly(24.8, y0, 32.8, 5.6, 24.8, y1), { base: '#dfe7f2', shade: '#868fa2', lite: '#ffffff', spec: '#ffffff', ink: '#14162a' }, { shade: poly(24.8, 5.6, 32.8, 5.6, 24.8, y1) });
  for (let i = 0; i < 3; i++) { const x = 26 + i * 2 + (spin === null ? 0 : (spin % 3) * .66); a.flat(poly(x, 3.9, x + .7, 4.1, x - .2, 7.2, x - .9, 7), '#868fa2', { clip: poly(24.8, y0, 32.8, 5.6, 24.8, y1) }); }
};

Subjects.zfittings = function () {
  for (let n = 0; n < 16; n++) out(`pipe_${n}`, pipe(n));
  out('valve_open', bake({ w: 10, h: 10, draw: valve(true) }));
  out('valve_closed', bake({ w: 10, h: 10, draw: valve(false) }));
  out('socket', bake({ w: 8, h: 8, draw: socket }));
  out('power-socket', bake({ w: 5, h: 5, draw: powerSocket }));
  for (const links of [0, 2, 8, 10]) {
    out(`hatch_closed_${links}`, hatch(links, true));
    out(`hatch_open_${links}`, hatch(links, false));
    out(`platform_${links}`, bake({ w: 16, h: 8, draw: deck(links, false) }));
    out(`platform_open_${links}`, bake({ w: 16, h: 8, draw: deck(links, true) }));
  }
  out('platform', bake({ w: 16, h: 8, draw: deck(10, false) }));
  out('platform_open', bake({ w: 16, h: 8, draw: deck(10, true) }));
  out('ladder', bake({ w: 16, h: 16, tile: true, draw: ladder }));
  out('floor', bake({ w: 16, h: 4, draw: floor }));
  for (let f = 0; f < 4; f++) out(`drone_fly_${f}`, bake({ w: 18, h: 12, draw: droneFly(f) }));
  out('drone_dock', bake({ w: 14, h: 12, draw: droneDock }));
  out('drone_eye', bake({ w: 18, h: 12, draw: eye(12.2, 5.2) }));
  out('drone_dock_eye', bake({ w: 14, h: 12, draw: eye(10.2, 5.2) }));
  out('drone_lamp_work', bake({ w: 18, h: 12, draw: lampOf('#a8ecff') }));
  out('drone_lamp_low', bake({ w: 18, h: 12, draw: lampOf('#ff5040') }));
  out('droneBay_pad', bake({ w: 8, h: 1, draw: (a) => { a.flat(band(0, 0, 8, 1), '#7cf06a'); a.flat(band(1, .2, 7, .6), '#e0ffd0'); } }));
  for (let n = 0; n < 5; n++) out(`droneBay_charge_${n}`, bake({ w: 11, h: 1, draw: chargeBar(n, false) }));
  out('droneBay_charge_low', bake({ w: 11, h: 1, draw: chargeBar(0, true) }));
  out('hero_axe', bake({ w: 28, h: 14, draw: axe }));
  out('hero_drill', bake({ w: 33, h: 15, draw: drill(null) }));
  for (let f = 1; f <= 3; f++) out(`hero_drill_spin${f}`, bake({ w: 33, h: 15, draw: drill(f) }));
};
})();

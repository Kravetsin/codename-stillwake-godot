// What the market sells that is no item: the bag, the belt, the trade station's slot (picked with the market, the market-decrypt ADR) and the canisters' twice the air (heavy-helmet).
(function () {
'use strict';
const { ell, rr, poly } = Anime;
const K = {};
K.band = (x0, y0, x1, y1) => poly(x0, y0, x1, y0, x1, y1, x0, y1);
K.M = {
  STEEL: { base: '#8f97a6', shade: '#6a7183', deep: '#4a5060', lite: '#b4bbc8', spec: '#dfe4ea' },
  GUN: { base: '#596173', shade: '#444a5e', deep: '#2c3044', lite: '#7f879b', spec: '#c4ccd8' },
  DARK: { base: '#2a2d3c', shade: '#1a1c2a', deep: '#12131e', lite: '#40445a', spec: '#a8b4c6' },
};
// Wear, placed by hand. s: its size in the plate's own units.
K.rust = (a, x, y, s) => {
  a.flat(poly(x - s, y - .2 * s, x - .3 * s, y - .7 * s, x + .5 * s, y - .5 * s, x + s, y + .1 * s, x + .4 * s, y + .4 * s, x + .1 * s, y + .75 * s, x - .5 * s, y + .45 * s), '#70482f');
  a.flat(poly(x - .4 * s, y - .1 * s, x + .1 * s, y - .3 * s, x + .45 * s, y + .05 * s, x, y + .3 * s), '#3f2a24');
  a.flat(poly(x + .8 * s, y + .3 * s, x + 1.25 * s, y + .2 * s, x + 1.15 * s, y + .55 * s), '#70482f');
};
K.screw = (a, x, y, r, turn) => {
  a.flat(ell(x, y, r), '#14162a');
  a.flat(ell(x, y, r * .74), '#7d8598');
  a.flat(ell(x - r * .16, y - r * .18, r * .34), '#b0b8c8');
  const c = Math.cos(turn) * r * .62, s = Math.sin(turn) * r * .62;
  a.line(`M${x - c} ${y - s}L${x + c} ${y + s}`, { w: 1.1 });
};
K.rivet = (a, x, y, r) => { a.flat(ell(x, y, r), '#2c3044'); a.flat(ell(x - r * .2, y - r * .22, r * .55), '#8f97a6'); };
K.chip = (a, x, y, l, c) => a.flat(poly(x, y, x + l, y - l * .05, x + l * .84, y + l * .26, x + l * .1, y + l * .3), c || '#969eb0');
const POUCH = { base: '#7c6a4f', shade: '#5c4d3b', deep: '#41372d', lite: '#9c8866', spec: '#c4b088', ink: '#17120f' };
const STRAP = { base: '#4b4033', shade: '#352c25', deep: '#251f1b', lite: '#6b5c49', spec: '#8f7f6a', ink: '#17120f' };
const BRASS = { base: '#a98a4c', shade: '#6e5436', deep: '#4c3a2c', lite: '#d4b878', spec: '#f6e6b4' };
const HANDLE = { base: '#7d8598', shade: '#555c70', spec: '#c5ccd8' };
// The oxygen bottle's blue, as items/canister paints it.
const CAN = { base: '#3f78b0', shade: '#2a5488', deep: '#1c3c68', lite: '#70a4d0', spec: '#c4e0f0' };
/** A standing bottle, its shoulders round, its valve cap above: x, y its top left, w by h its body. */
const bottle = (a, x, y, w, h) => {
  const band = K.band;
  a.shape(rr(x + w * .3, y - 1.6, w * .4, 1.9, .3), K.M.GUN, { lite: band(x + w * .3, y - 1.6, x + w * .7, y - 1.1), shade: band(x + w * .58, y - 1.6, x + w * .7, y + .3) });
  a.shape(`M${x} ${y + 2}Q${x} ${y} ${x + w * .3} ${y}L${x + w * .7} ${y}Q${x + w} ${y} ${x + w} ${y + 2}L${x + w} ${y + h - .8}Q${x + w} ${y + h} ${x + w - .8} ${y + h}L${x + .8} ${y + h}Q${x} ${y + h} ${x} ${y + h - .8}Z`, CAN, {
    lite: band(x, y + .4, x + w * .3, y + h), shade: band(x + w * .72, y, x + w, y + h), deep: band(x, y + h - .9, x + w, y + h), spec: band(x + w * .14, y + 2, x + w * .24, y + h * .6) });
};

const PICTURES = {
  bag(a) {
    a.tube('M5.2 5C5 1.4 11 1.4 10.8 5', 1.1, HANDLE);
    a.shape('M2.6 6L13.4 5.6L14.2 12.4Q14.3 14.4 12.4 14.5L3.7 14.7Q1.9 14.6 1.9 12.6Z', POUCH, {
      shade: 'M11.2 6L14.4 6L14.4 14.7L11.8 14.7Z', deep: 'M1.9 13.1L14.3 12.8L14.3 14.8L1.9 14.8Z', lite: 'M3 9.2L5.6 9.6L5.2 12.6L2.6 12.5Z',
      lines: ['M4 12L12.4 11.7', 'M8.1 9.8L8.2 11.6'] });
    a.flat(poly(10.4, 10.2, 12.8, 10, 12.9, 12, 10.5, 12.2), '#6a5942');
    a.line('M10.3 10.1L12.9 9.9M12.9 9.9L13 12.1M13 12.1L10.5 12.3', { w: .8 });
    a.shape('M2.4 4.8L13.6 4.4L13.8 8.2L8.6 9.7L2.4 8.4Z', STRAP, { lite: 'M2.6 4.9L13.4 4.5L13.4 5.2L2.6 5.6Z', shade: 'M2.4 7.6L8.6 9L13.8 7.4L13.8 8.3L8.6 9.8L2.4 8.5Z', lines: ['M3.2 6.2L12.9 5.8'] });
    a.shape(rr(6.4, 8, 3.2, 2.8, .4), BRASS, { outer: false, shade: 'M6.4 10L9.6 10L9.6 10.8L6.4 10.8Z' });
    a.flat(rr(7.2, 8.7, 1.6, 1.2, .2), '#f0b44c');
    a.flat(poly(7.4, 8.8, 8.3, 8.8, 7.8, 9.3), '#fbe2a8');
    K.rust(a, 9.9, 10.6, .55);
    K.chip(a, 3.2, 5.8, 1.2, '#8f7f6a');
  },
  belt(a) {
    a.shape('M.4 5.6L15.6 5.2L15.6 10.9L.4 11.3Z', POUCH, { shade: 'M.4 9.7L15.6 9.3L15.6 11.1L.4 11.5Z', lite: 'M.4 5.8L15.6 5.4L15.6 6.1L.4 6.5Z', lines: ['M.8 7L4.4 6.9', 'M11.6 6.7L15.2 6.6', 'M.8 9.4L4.4 9.3', 'M11.6 9.1L15.2 9'] });
    for (const [x, y] of [[1.9, 8.2], [3.4, 8.1], [13.8, 7.9]]) { a.flat(ell(x, y, .42), '#1a1510'); a.flat(ell(x + .1, y + .12, .18), '#6a5942'); }
    a.shape(rr(4.8, 3.2, 6.4, 9.8, .7), STRAP, { lite: 'M5.1 3.4L10.9 3.4L10.9 4.1L5.1 4.1Z', shade: 'M9.8 3.4L11.2 3.4L11.2 13L9.8 13Z', deep: 'M4.8 12L11.2 12L11.2 13L4.8 13Z', lines: ['M5.5 11.5L10.4 11.5'] });
    a.flat(rr(5.9, 4.6, 4.2, 6.2, .4), '#14162a');
    a.flat(rr(6.5, 5.2, 3, 5, .25), '#f0b44c');
    a.flat(rr(7.2, 6.2, 1.6, 3, .1), '#14162a');
    a.flat(poly(6.6, 5.3, 8.4, 5.3, 6.6, 6.2), '#fbe2a8');
    K.screw(a, 5.9, 3.9, .4, .6);
    K.screw(a, 10.1, 3.9, .4, 2);
    K.rust(a, 11.2, 12.3, .5);
    K.chip(a, 13.2, 5.7, 1.3, '#9c8866');
    a.line('M12.4 10.2L13.4 10.1', { w: .8 });
  },
  tradeSlots(a) {
    a.shape(poly(3.2, 11, 5.2, 9, 5.2, 14, 3, 15.2), K.M.GUN, { shade: poly(3, 13.6, 5.2, 12.8, 5.2, 14, 3, 15.2) });
    a.shape(poly(12.8, 11, 10.8, 9, 10.8, 14, 13, 15.2), K.M.GUN, { shade: poly(11.6, 10, 12.8, 11, 13, 15.2, 11.6, 14.6) });
    a.shape('M5.6 12.6L10.4 12.6L11 14.9L5 14.9Z', K.M.DARK, { lines: ['M5.8 13.6L10.2 13.6'] });
    a.shape('M8 .7C10.4 2.4 11.6 5 11.6 8L11.6 12.8L4.4 12.8L4.4 8C4.4 5 5.6 2.4 8 .7Z', K.M.STEEL, {
      shade: 'M9.8 1.8L12 3L12 13L9.8 13Z', deep: 'M4.4 12L11.6 12L11.6 12.9L4.4 12.9Z', lite: 'M5.6 5L6.6 3.4L6.8 11.6L5.6 11.6Z', lines: ['M4.6 8.4L11.4 8.4', 'M6 3.2L10 3.2'] });
    a.flat(ell(8, 6, 1.9), '#14162a');
    a.flat(ell(8, 6, 1.4), '#4da3d6');
    a.flat(poly(7, 5.6, 7.9, 4.9, 8.4, 5.1, 7.3, 6.2), '#c8e4f4');
    a.flat(K.band(4.4, 9.4, 11.6, 10.8), '#d99a2c');
    a.flat(poly(9.6, 9.4, 10.6, 9.4, 10.2, 10.8, 9.3, 10.8), '#14162a');
    K.rivet(a, 5.4, 11.7, .32); K.rivet(a, 10.6, 11.7, .32);
    K.rust(a, 4.9, 8.9, .5);
    a.line('M9.2 6.8L9.9 7.5', { w: .8 });
  },
  // Canisters give twice the air (2026-11-03-the-heavy-helmet), as picked: two bottles strapped, their valves joined by a brass tee.
  canister(a) {
    const band = K.band;
    a.shape('M4.4 1.6L11.6 1.6L11.6 2.8L8.6 2.8L8.6 3.6L7.4 3.6L7.4 2.8L4.4 2.8Z', BRASS, { shade: band(4.4, 2.3, 11.6, 2.9), lite: band(4.4, 1.6, 11.6, 1.95) });
    a.shape(rr(7.3, .4, 1.4, 1.4, .3), BRASS, { shade: band(7.3, 1.2, 8.7, 1.8) });
    bottle(a, 1.4, 4.4, 6, 10.6);
    bottle(a, 8.6, 4.4, 6, 10.6);
    a.shape('M.8 8.6L15.2 8.4L15.2 10.8L.8 11Z', STRAP, { lite: band(.8, 8.6, 15.2, 9.1), shade: band(.8, 10.2, 15.2, 11), lines: ['M1.2 9.6L6.8 9.5', 'M9.2 9.5L14.8 9.4'] });
    a.shape(rr(6.6, 8, 2.8, 3.4, .3), BRASS, { shade: band(6.6, 10.6, 9.4, 11.4) });
    a.flat(rr(7.2, 8.7, 1.6, 2, .2), '#14162a');
    a.flat(band(7.6, 8.7, 8, 10.7), BRASS.lite);
    K.rust(a, 13.2, 13.4, .5);
    K.chip(a, 2.2, 6, 1, '#70a4d0');
    a.line('M10.4 12.4L11.2 12.9', { w: .8 });
  },
};

Subjects.upgrades = () => {
  for (const kind of Object.keys(PICTURES)) out('upgrade_' + kind, Anime.bake({ w: 16, h: 16, draw: PICTURES[kind] }));
};
})();

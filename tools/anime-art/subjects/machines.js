// Every machine of the testbed and its running pictures, on the smelter's materials and light. Layout follows the logical picture: ports, plugs and overlays land on it.
(function () {
'use strict';
const { ell, rr, poly, bake } = Anime;
const STEEL = { base: '#a7afbc', shade: '#6a708c', deep: '#474c68', lite: '#d4dae2', spec: '#ffffff', rim: '#86d2e8' };
const GUN = { base: '#5c6474', shade: '#383c58', deep: '#262a42', lite: '#868fa2', spec: '#dfe7f2', rim: '#5fb2d0' };
const DARK = { base: '#2a2d3c', shade: '#1a1c2a', deep: '#12131e', lite: '#40445a', spec: '#a8b4c6', rim: '#4f9cbc' };
const COPPER = { base: '#a8693c', shade: '#6c3a30', deep: '#4c2830', lite: '#cf8c58', spec: '#f4c896', rim: '#d09060' };
const BRASS = { base: '#a98a4c', shade: '#6e5436', deep: '#4c3a2c', lite: '#d4b878', spec: '#f6e6b4', rim: '#d4b878' };
const CREAM = { base: '#d8d2c2', shade: '#8e8a84', deep: '#6a666a', lite: '#f2eee2', spec: '#ffffff', rim: '#d8d2c2' };
const WOOD = { base: '#8a6242', shade: '#5c3e30', deep: '#402a26', lite: '#ad8258', spec: '#cca678', rim: '#ad8258', ink: '#1c1210' };
const NAVY = { base: '#35587a', shade: '#223c5c', deep: '#172a44', lite: '#5a86a8', spec: '#a8d0e4', rim: '#5a86a8' };
const TEAL = { base: '#4b8f96', shade: '#2d606e', deep: '#20465a', lite: '#7cc0c0', spec: '#d0f0ec', rim: '#7cc0c0' };
const GLASS = { base: '#1c3a50', shade: '#142a40', deep: '#0e1c30', lite: '#2f5f78', spec: '#c8ecf8', rim: '#5ab8d8' };
const GREEN = { base: '#2b7a52', shade: '#1a5040', deep: '#123a30', lite: '#54ba74', spec: '#c8f4c0', rim: '#54ba74' };
const CELL = { base: '#244a94', shade: '#1a3470', deep: '#122456', lite: '#3f6cc0', spec: '#a0c4f4', rim: '#3f6cc0' };
const band = (x0, y0, x1, y1) => poly(x0, y0, x1, y0, x1, y1, x0, y1);

/** A panel: its lit top edge, its shaded foot and side, as the smelter's cabinet. */
function box(a, x, y, w, h, m, o) {
  o = o || {};
  const s = o.s ?? Math.min(1.25, w * .12), b = o.b ?? Math.min(1.4, h * .14), l = o.l ?? Math.min(.8, h * .2);
  a.shape(rr(x, y, w, h, o.r ?? .4), m, { outer: o.outer, lite: band(x, y, x + w, y + l), shade: [band(x + w - s, y, x + w, y + h), band(x, y + h - b, x + w, y + h)], spec: o.spec ? band(x + .8, y + .15, x + .8 + w * .3, y + .45) : undefined, lines: o.lines });
}
const inner = (a, x, y, w, h, m, o) => box(a, x, y, w, h, m, { outer: false, ...o });
const well = (a, x, y, w, h, m) => a.shape(band(x, y, x + w, y + h), m || DARK, { outer: false, deep: band(x, y, x + w, y + Math.min(1, h * .25)) });
const rivets = (a, ...pts) => { for (const [x, y] of pts) { a.flat(ell(x, y, .42), '#474c68'); a.flat(ell(x - .1, y - .12, .2), '#d4dae2'); } };
const feet = (a, y, h, ...xs) => { for (const [x, w] of xs) a.shape(rr(x, y, w, h, .25), GUN, { lite: band(x + .2, y, x + w * .45, y + h), shade: band(x + w * .65, y, x + w, y + h) }); };
const slats = (a, x, y, w, h, n, m) => { for (let i = 0; i < n; i++) { const yy = y + i * (h / n); a.flat(rr(x, yy, w, h / n * .55, .1), (m || DARK).base); a.flat(band(x, yy + h / n * .4, x + w, yy + h / n * .55), (m || DARK).lite); } };
const gearPath = (cx, cy, r, n, turn) => { const p = []; for (let i = 0; i < n * 2; i++) { const t = (i / (n * 2)) * Math.PI * 2 + (turn || 0), k = i % 2 ? r * .72 : r; p.push(cx + Math.cos(t) * k, cy + Math.sin(t) * k); } return poly(p); };
function gear(a, cx, cy, r, n, m, turn) {
  a.shape(gearPath(cx, cy, r, n, turn), m, { outer: false, lite: gearPath(cx - r * .12, cy - r * .16, r * .8, n, turn), shade: .35 });
  a.flat(ell(cx, cy, r * .32), m.deep);
  a.flat(ell(cx - r * .08, cy - r * .1, r * .14), m.spec);
}
/** A spoked wheel in a dark ring, its spokes at <turn>. */
function wheel(a, cx, cy, r, m, turn, spokes) {
  a.shape(ell(cx, cy, r), DARK, { lite: .0001 });
  a.flat(ell(cx, cy, r * .78), m.deep);
  for (let i = 0; i < (spokes || 3); i++) {
    const t = turn + (i / (spokes || 3)) * Math.PI * 2, c = Math.cos(t), s = Math.sin(t), w = r * .2;
    a.flat(poly(cx - s * w, cy + c * w, cx + c * r * .74 - s * w * 1.6, cy + s * r * .74 + c * w * 1.6, cx + c * r * .74 + s * w * 1.6, cy + s * r * .74 - c * w * 1.6, cx + s * w, cy - c * w), m.lite);
  }
  a.flat(ell(cx, cy, r * .2), m.base);
  a.flat(ell(cx - r * .05, cy - r * .06, r * .09), m.spec);
}
const hazard = (a, x, y, w, h) => { a.flat(band(x, y, x + w, y + h), '#f0c43c'); for (let k = -h; k < w; k += h * 1.6) a.flat(poly(x + Math.max(0, k), y + h, x + Math.min(w, k + h * .8), y + h, x + Math.min(w, k + h * 1.8), y, x + Math.min(w, k + h), y), '#1a1820', { clip: band(x, y, x + w, y + h) }); };
const led = (a, x, y, c) => { a.flat(ell(x, y, .55), '#14131c'); a.flat(ell(x, y, .36), c); };
const cyl = (a, x, y, w, h, m, o) => a.shape(rr(x, y, w, h, o?.r ?? w * .22), m, { outer: o?.outer, lite: band(x + w * .18, y, x + w * .46, y + h), spec: band(x + w * .26, y + h * .08, x + w * .34, y + h * .7), shade: band(x + w * .7, y, x + w, y + h), deep: band(x, y + h * .92, x + w, y + h), lines: o?.lines });

// ---------- the gear stations by the hero ----------
const furnace = (a) => {
  // The flue, with its cowl and a rust stain.
  cyl(a, 21.5, 1, 3.5, 7, GUN);
  a.shape(rr(20.8, 0, 5, 1.6, .3), DARK, { lite: band(21, 0, 25.5, .4) });
  a.tube('M25 10.5L29 10.5', 1.2, COPPER);
  box(a, 2, 6.5, 28, 23.5, STEEL, { spec: true });
  // A patched plate on the left, streaked with rust; the firebox's mouth; a plate on the right.
  inner(a, 2.5, 7.5, 11, 21.5, { ...STEEL, base: '#979fac', lite: '#c0c7d1' });
  a.flat('M6 16L6.8 16L6.5 23L7 26L6.1 26L5.7 22Z', '#8e6044');
  a.flat('M9.5 12L10.2 12L10 17L9.4 17Z', '#8e6044');
  slats(a, 4, 19.5, 7.5, 5, 3, GUN);
  rivets(a, [3.6, 9], [12.2, 9], [3.6, 27.7], [12.2, 27.7]);
  inner(a, 14, 8, 10, 8, GUN, { lines: ['M14.4 12L23.6 12'] });
  a.shape(rr(15, 9, 5, 1.6, .3), COPPER, { outer: false, lite: band(15, 9, 20, 9.5) });
  inner(a, 25, 8, 4.5, 21, STEEL);
  rivets(a, [27.2, 10], [27.2, 27]);
  a.shape(band(13.4, 16.4, 24.6, 28.6), DARK, { outer: false, deep: band(13.4, 16.4, 24.6, 17.4) });
  for (let i = 0; i < 5; i++) a.flat(band(14.4 + i * 2, 18, 15.4 + i * 2, 28), '#4c2830');
  a.flat(band(13.4, 28, 24.6, 28.6), '#6a708c');
  feet(a, 30, 2, [4, 3.5], [25, 3.5]);
};
const former = (a) => {
  a.tube('M15.5 10L15.5 12', 2.4, GUN);
  wheel(a, 15.5, 5.6, 5.4, GUN, .5);
  box(a, 2, 10.5, 28, 15.5, STEEL, { spec: true });
  well(a, 6, 12.5, 20, 11);
  gear(a, 9.8, 18, 3.8, 8, COPPER, .2);
  gear(a, 21.2, 18, 3.8, 8, COPPER, .6);
  a.flat(band(12.6, 17.3, 18.4, 18.7), '#868fa2');
  inner(a, 12.5, 21, 6, 2.2, GUN);
  rivets(a, [4, 12.8], [4, 23.6], [28, 12.8], [28, 23.6]);
  box(a, 1, 25.5, 30, 2.8, STEEL, { spec: true });
  feet(a, 28, 3.6, [3.4, 3], [25.6, 3]);
};
const crusher = (a) => {
  // The hopper, its striped lip, the ore heaped in it.
  a.shape(poly(5, 3, 23, 3, 19, 11, 9, 11), GUN, { shade: poly(17, 3, 23, 3, 19, 11, 16, 11), lite: band(5, 3, 23, 4) });
  a.shape(rr(3, 2.2, 22, 2.4, .3), DARK, { lite: .0001 });
  hazard(a, 3.4, 2.6, 21.2, 1.6);
  a.flat(poly(9, 2.4, 11, .8, 14, 0.2, 18, .8, 20, 2.4), '#868fa2');
  a.flat(poly(11, 1.4, 14, .6, 15, 1.4), '#c4ccd8');
  box(a, 1, 8.5, 30, 16.5, STEEL, { spec: true });
  well(a, 4, 11, 18.5, 12);
  gear(a, 9.2, 17, 4.6, 8, GUN, .3);
  gear(a, 16.8, 17, 4.6, 8, GUN, .7);
  inner(a, 23.6, 10.5, 6.2, 13, GUN);
  for (let i = 0; i < 4; i++) a.line(`M24.6 ${12 + i * 3}L28.8 ${12 + i * 3}`, { w: 2, c: '#262a42' });
  rivets(a, [2.4, 10.2], [2.4, 23]);
  a.shape(poly(9, 25, 19, 25, 21, 29, 7, 29), GUN, { shade: poly(16, 25, 19, 25, 21, 29, 17, 29) });
  feet(a, 25, 7, [2.2, 3], [26.4, 3]);
};
const assembler = (a) => {
  box(a, 1, 0.6, 30, 3.6, STEEL, { spec: true });
  a.shape(band(2, 4, 4, 18), GUN, { lite: band(2, 4, 2.7, 18) });
  a.shape(band(28, 4, 30, 18), GUN, { shade: band(29.2, 4, 30, 18) });
  well(a, 4, 4, 24, 14);
  // The arm at rest, its crystal in the clamp, the status strip under it.
  a.tube('M7 9.5L10.5 8L14 11', 1.1, GUN);
  gear(a, 7, 9.5, 1.7, 6, GUN);
  a.flat(band(9, 15.4, 22, 16), '#2b7a52');
  box(a, 1, 18, 30, 3, STEEL);
  a.shape(band(2, 21, 30, 30), GUN, { shade: band(28.8, 21, 30, 30) });
  well(a, 4, 22, 24, 7);
  for (const x of [7.5, 16]) { box(a, x, 23.4, 8, 5.2, WOOD, { outer: false }); a.line(`M${x + 4} 23.6L${x + 4} 28.4`, { w: 1.6, c: '#402a26' }); }
  box(a, 1, 29.6, 30, 2, STEEL);
};
const workbench = (a) => {
  // A pegboard of tools, the bench top, a cabinet and a shelf.
  a.shape(band(2, 0.6, 29, 16.4), DARK, { deep: band(2, .6, 29, 1.4) });
  a.shape(rr(1.6, .2, 28, 1.4, .3), GUN, { lite: band(1.8, .2, 29.4, .6) });
  for (const [x, h, c] of [[5, 8, '#868fa2'], [9, 10, '#a8693c'], [12.4, 6.5, '#c4ccd8'], [15.5, 9, '#868fa2'], [19, 8, '#c8503c'], [22, 8, '#c8503c']]) {
    a.flat(band(x, 3, x + 1, 3 + h), c);
    a.flat(rr(x - .7, 2.2, 2.4, 1.8, .3), c);
  }
  a.flat(band(25.2, 2.2, 28, 13), '#262a42');
  box(a, 0.8, 16, 29.2, 3.6, GUN, { spec: true });
  a.shape(rr(11, 16.8, 3, 1.6, .2), COPPER, { outer: false, lite: band(11, 16.8, 14, 17.3) });
  a.shape(rr(2.6, 13, 3.4, 3.2, .4), COPPER, { lite: band(2.8, 13.1, 5.6, 13.8) });
  a.shape(band(2, 19.6, 29, 37), DARK, { deep: band(2, 19.6, 29, 20.6) });
  box(a, 2, 20.4, 12, 16.2, STEEL, { spec: true });
  hazard(a, 2.8, 24, 10.4, 1.2);
  a.flat(rr(6, 28, 4.4, 1, .3), '#474c68');
  for (const y of [25, 30.6, 35.6]) a.flat(band(14.4, y, 28.6, y + .8), '#868fa2');
  box(a, 15.4, 21.2, 4, 3.8, COPPER, { outer: false });
  for (const x of [16, 19, 22.5]) cyl(a, x, 31.4, 2.4, 4.2, STEEL, { outer: false });
  a.shape(rr(1.6, 19.6, 27.8, .8, .1), GUN, { outer: false });
  feet(a, 37, 3, [3, 3.6], [24.4, 3.6]);
};
const arcBench = (a) => {
  // Table, its vise and tool, and the electrode's post.
  box(a, .4, 17.6, 31, 3.4, STEEL, { spec: true });
  a.shape(band(3, 21, 29, 25), GUN, { shade: band(3, 24, 29, 25) });
  inner(a, 11, 21.5, 11, 2.8, DARK);
  a.flat(rr(14, 22.4, 5, .8, .2), '#868fa2');
  for (const x of [2.6, 25.4]) { cyl(a, x, 21, 3.6, 8.5, STEEL); a.shape(rr(x - .8, 29, 5.2, 2.6, .3), GUN, { lite: band(x - .6, 29, x + 4.2, 29.5) }); }
  a.flat(band(6, 26, 25.4, 27), '#6a708c');
  // The vise and what it holds.
  a.shape(rr(10.6, 14.6, 12, 3.2, .3), COPPER, { lite: band(10.8, 14.7, 22.4, 15.3), shade: band(10.6, 16.8, 22.6, 17.8) });
  a.shape(rr(11.4, 9.8, 9.6, 5, .5), BRASS, { lite: band(11.6, 9.9, 20.8, 10.6), shade: band(19.6, 9.8, 21, 14.8) });
  a.flat(rr(13, 11, 5.8, 2.6, .6), '#2a2640');
  a.flat(ell(14.8, 12, .9, .6), '#7a68c8');
  // The post with its arm.
  cyl(a, 26.4, 7, 2.8, 11, COPPER);
  a.shape(rr(25, 15.8, 5.6, 2, .3), COPPER, { lite: band(25.2, 15.9, 30.4, 16.4) });
  a.tube('M27.8 7.2L21.8 7.2', 1.5, COPPER);
  a.shape(rr(25.6, 5.6, 4.4, 2.2, .4), BRASS, { lite: band(25.8, 5.7, 29.8, 6.2) });
};
const vent = (a) => {
  box(a, 1, 1, 14, 14, NAVY, { r: .3, s: .8, b: .8, l: .5 });
  a.shape(band(2.4, 2.4, 13.6, 13.6), DARK, { outer: false });
  for (let i = 0; i < 5; i++) { a.flat(band(2.8, 3 + i * 2.1, 13.2, 4 + i * 2.1), '#5a86a8'); a.flat(band(2.8, 3 + i * 2.1, 13.2, 3.3 + i * 2.1), '#a8d0e4'); }
};
const crate = (a) => {
  box(a, .4, 1, 31.2, 14, WOOD, { r: .3, spec: true });
  for (const y of [5.4, 10]) { a.flat(band(.8, y, 31.2, y + .5), '#402a26'); a.flat(band(.8, y + .5, 31.2, y + .8), '#ad8258'); }
  for (const [x, y] of [[9, 2.6], [20, 7.6], [13, 12]]) a.line(`M${x} ${y}L${x + 3.4} ${y + .2}`, { w: 1.8, c: '#5c3e30' });
  for (const x of [.6, 26.8]) { inner(a, x, 1, 4.6, 14, STEEL, { l: .5 }); rivets(a, [x + 2.3, 3], [x + 2.3, 8], [x + 2.3, 13]); }
  inner(a, 13.8, 5.6, 4.4, 4.6, GUN);
  a.flat(rr(15.2, 7, 1.6, 1.8, .2), '#c4ccd8');
};
const cistern = (a) => {
  a.shape(rr(13.5, .6, 5, 2.4, .3), COPPER, { lite: band(13.7, .7, 18.3, 1.2) });
  cyl(a, 14.6, 2.6, 2.8, 3.4, GUN, { outer: false });
  for (const x of [.4, 28.6]) a.shape(rr(x, 13, 3, 3.6, .3), DARK, { lite: band(x + .2, 13, x + 2.8, 13.6) });
  a.shape(rr(3, 5.4, 26, 18.4, 4.6), STEEL, {
    lite: 'M7 5.4L25 5.4C26.5 5.4 28 6.5 28.6 8L3.4 8C4 6.5 5.5 5.4 7 5.4Z', spec: band(6.6, 6.2, 17, 6.8), shade: 'M3 19L29 19L29 19.2C29 21.8 27 23.8 24.4 23.8L7.6 23.8C5 23.8 3 21.8 3 19.2Z',
    lines: ['M7.8 5.6L7.8 23.6', 'M24.2 5.6L24.2 23.6'] });
  a.flat(band(4, 11, 7.6, 11.6), '#d4dae2'); a.flat(band(24.6, 11, 28, 11.6), '#6a708c');
  // The window: its frame, dark glass; the fill is drawn over it (tank_fill_*).
  a.shape(rr(11.6, 9.4, 8.8, 12.4, .5), DARK, { outer: false, lite: band(11.8, 9.5, 20.2, 10) });
  a.flat(band(13, 11, 19, 21), '#0e1c30');
  a.flat(band(13, 15, 19, 21), '#2c8ca8'); a.flat(band(13, 15, 19, 15.6), '#8ce0ec'); a.flat(band(13.6, 15.6, 14.4, 21), '#58b8cc');
  for (const [x, y] of [[12.1, 10], [19.9, 10], [12.1, 21.2], [19.9, 21.2]]) a.flat(ell(x, y, .35), '#a8693c');
  for (const x of [8.6, 19.6]) a.shape(rr(x, 23.4, 3.8, 3, .2), GUN, { lite: band(x, 23.4, x + 1.4, 26.4) });
  box(a, 1, 26.6, 30, 2.8, STEEL, { spec: true });
};
const growCapsule = (a) => {
  // Cap, tall glass with soil in it, the base under it.
  a.shape(poly(4, 5, 8, 1.4, 24, 1.4, 28, 5, 28, 7, 4, 7), GUN, { lite: poly(8, 1.4, 24, 1.4, 25, 2.4, 7, 2.4), shade: band(4, 5.6, 28, 7) });
  a.shape(rr(12, .4, 8, 1.6, .3), COPPER, { lite: band(12.2, .5, 19.8, .9) });
  a.shape(band(5, 7, 27, 33.6), GLASS, { lite: band(5, 7, 6.6, 33.6), shade: band(23, 7, 27, 33.6), deep: band(5, 7, 27, 8), lines: ['M5.4 8.4L26.6 8.4'] });
  a.flat(poly(21, 14, 25.4, 9.6, 25.4, 11.4, 21, 15.8), '#7cc0d0');
  a.flat(poly(19, 25, 25.4, 18.6, 25.4, 19.8, 19, 26.2), '#7cc0d0');
  a.shape(band(5.4, 30.6, 26.6, 33.6), WOOD, { outer: false, lite: band(5.4, 30.6, 26.6, 31.3) });
  box(a, 3.4, 33.6, 25.2, 1.8, GUN);
  box(a, 2, 35.2, 28, 10.6, STEEL, { spec: true });
  inner(a, 3.6, 37, 5.4, 7, { ...STEEL, base: '#bcc4cf' });
  for (let i = 0; i < 3; i++) a.flat(band(4.2, 38 + i * 1.8, 8.4, 38.6 + i * 1.8), '#6a708c');
  inner(a, 16, 37.4, 11, 5.4, DARK);
  a.flat(band(17.4, 38.8, 25.6, 41.2), '#2b7a52');
  a.flat(rr(21, 39.2, 3, 1.6, .2), '#54ba74');
  feet(a, 45.6, 2.2, [4, 3.4], [24.6, 3.4]);
};
const charger = (a) => {
  a.shape(rr(1.6, 4, 12.8, 25.6, .5), NAVY, { lite: band(1.8, 4.1, 14.2, 4.7), shade: band(13, 4, 14.4, 29.6) });
  a.shape(band(3.4, 5.8, 12.4, 28), DARK, { outer: false, deep: band(3.4, 5.8, 12.4, 6.6) });
  inner(a, 4.4, 7, 7, 3.4, GUN);
  a.flat(band(5.2, 8, 10.6, 9.2), '#12131e');
  a.shape(band(5, 13, 11, 27.6), { ...NAVY, base: '#223c5c', lite: '#2f4e70' }, { outer: false, lite: band(5, 13, 11, 13.7) });
  a.flat(rr(6.6, 25.4, 2.8, .8, .2), '#5a86a8');
  a.shape(rr(1, 29.6, 14, 2, .3), NAVY, { lite: band(1.2, 29.6, 14.8, 30.1) });
};
const burnerGenerator = (a) => {
  cyl(a, 4.6, 2.6, 3.4, 10, GUN);
  a.shape(rr(4, 1.6, 4.6, 1.4, .3), DARK, { lite: band(4.2, 1.6, 8.4, 1.9) });
  // The wheel's axle; the wheel itself turns over it (burner_wheel_*), drawn here at rest.
  wheel(a, 23.5, 6.5, 6, GUN, .4);
  a.tube('M23.5 11L23.5 13', 1.6, GUN);
  box(a, 1, 11.4, 29, 18, STEEL, { spec: true });
  a.shape(band(3.4, 15.4, 14.6, 28), DARK, { outer: false, deep: band(3.4, 15.4, 14.6, 16.4) });
  for (let i = 0; i < 5; i++) a.flat(band(4.4 + i * 2, 17, 5.4 + i * 2, 27), '#4c2830');
  a.flat(band(16, 13, 16.6, 28), '#474c68');
  rivets(a, [2.4, 13], [2.4, 27.8], [17.6, 13], [17.6, 27.8], [28.4, 13], [28.4, 27.8]);
  inner(a, 18.8, 16, 8.8, 8.4, COPPER);
  for (let i = 0; i < 4; i++) a.flat(band(20 + i * 1.8, 17.2, 20.8 + i * 1.8, 23.2), '#4c2830');
  feet(a, 29, 2.8, [4, 3.4], [23, 3.4]);
};
/** A brass terminal: the trade station's, a chart on its screen, a green bar; the automatic one's a lamp where the bar is. */
const terminal = (auto) => (a) => {
  a.shape(band(2.6, 1.6, 29, 26.6), BRASS, { lite: band(2.6, 1.6, 29, 2.3), shade: band(27.8, 1.6, 29, 26.6) });
  a.shape(band(4.4, 3.2, 27.2, 13.8), DARK, { outer: false, deep: band(4.4, 3.2, 27.2, 4) });
  a.flat(poly(5.8, 6, 12, 6, 12, 7, 17, 7, 17, 9, 22, 9, 22, 10, 16, 10, 16, 8, 10, 8, 10, 7, 5.8, 7), '#f0c460');
  if (!auto) a.flat(band(4.4, 13.2, 27.2, 14), '#54ba74');
  inner(a, 4.4, 15, 22.8, 6.6, { ...BRASS, base: '#5c4a30', lite: '#7c6440', shade: '#3e3024' });
  if (auto) { led(a, 7, 13, '#54ba74'); led(a, 25, 13, '#f0c460'); }
  a.flat(rr(6, 23, 3.6, 1, .2), '#f6e6b4');
  a.flat(rr(23, 23, 3.6, 1, .2), '#f6e6b4');
  box(a, 1, 26.6, 30, 3, BRASS, { spec: true });
  a.flat(band(1.6, 29.4, 30.4, 30), '#4c3a2c');
};
const droneBay = (a) => {
  // A mast with a deck a side at each pair of pads; docks are drawn over it.
  cyl(a, 14.2, 1.4, 3.6, 26, COPPER, { r: .4 });
  for (let i = 0; i < 7; i++) a.flat(band(15, 4 + i * 3.2, 17, 4.8 + i * 3.2), '#4c2830');
  a.shape(rr(13, .2, 6, 2, .3), GUN, { lite: band(13.2, .3, 18.8, .7) });
  for (const y of [16.4, 29]) {
    box(a, y > 20 ? 2 : .6, y - .6, y > 20 ? 28 : 30.8, 2, GUN, { l: .4, b: .6 });
    for (const x of [2, 5.6, 9.2, 22, 25.6, 29]) if (y < 20) a.flat(ell(x, y + .4, .3), '#c4ccd8');
  }
  a.tube('M6 19L14 25', .8, GUN);
  a.tube('M26 19L18 25', .8, GUN);
  feet(a, 30.4, 1.4, [3, 4], [25, 4]);
};
const lifepod = (a) => {
  for (const x of [0.6, 4.4]) cyl(a, x, 24, 3.8, 20, TEAL, { r: .5 });
  // The console at its right.
  box(a, 35, 14.8, 9.4, 24, GUN);
  a.shape(band(36, 17, 43.4, 23.6), DARK, { outer: false });
  a.flat(band(37.6, 20, 41, 21), '#54ba74');
  for (const y of [25, 29.4, 33.8]) inner(a, 36.4, y, 6.8, 3.4, STEEL);
  // The pod's shell, its glass door, the striped sill.
  a.shape('M9 42L9 16C9 7 15 1.6 22 1.6C29 1.6 35 7 35 16L35 42Z', STEEL, {
    lite: 'M12 10C14 5 18 2.8 22 2.8C26 2.8 30 5 32 10C29 6.8 25.4 5.6 22 5.6C18.6 5.6 15 6.8 12 10Z', spec: 'M14.6 6.2C16.6 4.2 19 3.4 22 3.4C19 4.4 17 5.4 15.6 6.8Z',
    shade: ['M31.6 12C33 13 35 15 35 16L35 42L32.4 42L32.4 16C32.4 14 32 13 31.6 12Z', band(9, 37, 35, 42)], lines: ['M12.4 9.4L12.4 41'] });
  a.shape(rr(14.4, 7.4, 15.2, 30, .9), GUN, { outer: false, lite: band(14.6, 7.5, 29.4, 8) });
  a.shape(band(15.6, 9, 28.4, 36), GLASS, { outer: false, lite: band(15.6, 9, 18, 36), shade: band(26, 9, 28.4, 36) });
  a.flat(band(16.2, 9.4, 17, 35.6), '#7cc0d0');
  a.flat(poly(21, 18, 27.4, 11.6, 27.4, 13, 21, 19.4), '#7cc0d0');
  a.flat(poly(19, 31, 27.4, 22.6, 27.4, 23.6, 19, 32), '#7cc0d0');
  rivets(a, [11, 15], [33, 15], [11, 26], [33, 26]);
  box(a, 8.4, 39.4, 27.2, 6.4, GUN);
  hazard(a, 10, 41.4, 24, 1.6);
};
const generator = (a) => {
  for (const x of [5, 22]) a.shape(rr(x, .4, 2.4, 2.4, .2), GUN, { lite: band(x, .4, x + 1, 2.8) });
  a.shape(rr(1, 2.4, 28, 26.6, .5), NAVY, { lite: band(1.2, 2.5, 28.8, 3.3), spec: band(3, 2.7, 12, 3.1), shade: [band(27.6, 2.4, 29, 29), band(1, 27.4, 29, 29)] });
  wheel(a, 9.6, 12, 7, { ...NAVY, deep: '#172a44', lite: '#a8d0e4', base: '#5a86a8', spec: '#ffffff' }, .3, 6);
  for (let i = 0; i < 6; i++) { a.flat(band(18.6, 6 + i * 2.2, 26.4, 6.9 + i * 2.2), '#172a44'); a.flat(band(18.6, 6.9 + i * 2.2, 26.4, 7.2 + i * 2.2), '#5a86a8'); }
  a.shape(band(13, 23.4, 26.4, 26), DARK, { outer: false });
  for (const x of [15, 18.6, 22.2]) a.flat(band(x, 24, x + 2, 25.4), '#f0c460');
  feet(a, 29, 1.8, [2, 5], [22.6, 5]);
};
const tank = (a) => {
  a.shape(rr(0, 16.6, 6, 3.6, .4), GUN, { lite: band(.2, 16.7, 5.8, 17.2), shade: band(.2, 19.2, 5.8, 20.2) });
  a.shape(rr(11, .8, 6, 1.8, .3), COPPER, { lite: band(11.2, .9, 16.8, 1.3) });
  a.shape('M6 25L6 7C6 4 8 2.4 11 2.4L19 2.4C22 2.4 24 4 24 7L24 25Z', STEEL, {
    lite: band(8, 2.6, 11.6, 25), spec: band(9, 4, 9.8, 20), shade: [band(21, 2, 24, 25), band(6, 23.4, 24, 25)], lines: ['M6.3 7L23.7 7'] });
  a.shape(rr(11.6, 9.4, 8.8, 12.6, .5), DARK, { outer: false, lite: band(11.8, 9.5, 20.2, 10) });
  a.flat(band(13, 11, 19, 21), '#0e1c30');
  for (const [x, y] of [[12.1, 10], [19.9, 10], [12.1, 21.4], [19.9, 21.4]]) a.flat(ell(x, y, .35), '#a8693c');
  a.flat(rr(21.2, 9, 1.8, 1.2, .2), '#6c3a30');
  box(a, 5, 25, 20, 1.6, STEEL, { l: .4, b: .4 });
  feet(a, 26.6, 3.4, [6.6, 3.6], [19.8, 3.6]);
};
const computer = (a) => {
  // Monitor on its neck, a box of parts beside it, the desk and its drawers.
  a.shape(rr(6.4, 0.6, 26.4, 13.6, .6), GUN, { lite: band(6.6, .7, 32.6, 1.3), shade: [band(31.4, .6, 32.8, 14.2), band(6.4, 13, 32.8, 14.2)] });
  a.shape(band(8.4, 2.2, 31, 12.4), DARK, { outer: false, deep: band(8.4, 2.2, 31, 3) });
  a.flat(band(9.6, 2.8, 30, 12), '#0c1420');
  a.flat(band(17, 14, 22, 16.2), '#383c58');
  a.shape(rr(14, 15.8, 11, 1.2, .2), GUN, { lite: band(14.2, 15.8, 24.8, 16.2) });
  box(a, 34, 8.6, 9.8, 8.2, DARK);
  a.shape(rr(39, 6.4, 4, 3, .3), COPPER, { lite: band(39.2, 6.5, 42.8, 7) });
  a.flat(band(35, 12, 37.4, 12.6), '#868fa2'); a.flat(band(38.4, 12, 40.8, 12.6), '#868fa2');
  box(a, .6, 17, 46.8, 2.4, STEEL, { spec: true });
  a.shape(band(2.6, 19.4, 45.4, 28.6), GUN, { lite: band(2.6, 19.4, 45.4, 19.9), shade: band(2.6, 27.6, 45.4, 28.6) });
  inner(a, 4.4, 20.4, 7.4, 7.4, DARK);
  inner(a, 13, 20.4, 7.6, 7, STEEL);
  inner(a, 21.8, 20.4, 7.6, 7, STEEL);
  for (let i = 0; i < 4; i++) { a.flat(band(31.4, 21 + i * 1.6, 43.4, 21.7 + i * 1.6), '#262a42'); }
  feet(a, 28.6, 2, [4, 4.4], [39.4, 4.4]);
};
const capacitorBank = (a) => {
  for (const x of [2.8, 20]) {
    cyl(a, x, 3, 9.2, 23.2, NAVY, { r: 1 });
    for (const y of [5.4, 18.4]) a.shape(band(x, y, x + 9.2, y + 2), COPPER, { outer: false, lite: band(x, y, x + 9.2, y + .6), shade: band(x + 6.6, y, x + 9.2, y + 2) });
    a.shape(rr(x + 2.6, .4, 1.4, 3, .1), COPPER, { outer: false });
    a.shape(rr(x + 5.6, .4, 1.4, 3, .1), COPPER, { outer: false });
  }
  box(a, 12.6, 6.4, 6.8, 19.8, DARK);
  // StoreLights draws the lit gauge here, by the pixels it was drawn at: six segments 4 × 2 at (14, 8), a step of 3 apart.
  for (let i = 0; i < 6; i++) a.flat(band(14, 8 + i * 3, 18, 10 + i * 3), '#1b2a30');
  box(a, .8, 26, 30.4, 2.6, STEEL, { spec: true });
  feet(a, 28.6, 2.4, [3, 4], [25, 4]);
};
const engine = (a) => {
  // The bell out into space on the left, the body against the wall on the right.
  a.shape(rr(1.2, 7, 3.2, 18.6, .8), DARK, { lite: band(1.4, 7.2, 2.2, 25.4) });
  a.shape('M4 8.4L8 7.4L25 11L25 22L8 25.2L4 24.2Z', GUN, {
    lite: 'M8 8.4L25 12L25 14L8 12Z', spec: 'M10 9.6L20 11.6L20 12L10 10.2Z', shade: 'M4 19L25 19L25 22L8 25.2L4 24.2Z', lines: ['M8 7.6L8 25', 'M15 9.2L15 23.6'] });
  a.tube('M25 16.5L34 16.5', 3.8, GUN);
  a.shape(rr(31, 11.6, 3, 9.8, .3), DARK, { lite: band(31.2, 11.7, 33.8, 12.2) });
  // Fuel line over the top.
  a.tube('M29 2L41 2', 1.6, COPPER);
  a.tube('M29.4 2L29.4 6', 1.6, COPPER);
  a.shape(rr(47.6, .6, 4.4, 4.6, .3), DARK, { lite: band(47.8, .7, 51.8, 1.1) });
  a.shape(rr(60.4, .4, 4.4, 2.6, .3), COPPER, { lite: band(60.6, .5, 64.6, .9) });
  box(a, 34.6, 4.8, 36.4, 22, STEEL, { spec: true, lines: ['M49.5 5L49.5 21.6'] });
  // A gauge; the intake's grille; the hazard band; a base.
  a.shape(rr(36.4, 6.6, 11.6, 13.6, .3), STEEL, { outer: false, shade: band(46.8, 6.6, 48, 20.2) });
  a.shape(ell(42, 12.6, 4), CREAM, { outer: false, shade: .4 });
  a.line('M42 12.6L44.6 10', { w: 2.6, c: '#c8503c' });
  a.flat(ell(42, 12.6, .5), '#262a42');
  a.line('M38.6 12.6L39.4 12.6M42 9.2L42 10M45.4 12.6L44.6 12.6', { w: 1.6, c: '#262a42' });
  rivets(a, [37.6, 18.8], [46.6, 18.8], [37.6, 7.8], [46.6, 7.8]);
  a.shape(band(51, 6.6, 69.4, 19.8), DARK, { outer: false, deep: band(51, 6.6, 69.4, 7.4) });
  for (let i = 0; i < 7; i++) a.flat(band(52, 8 + i * 1.6, 68.4, 8.7 + i * 1.6), '#474c68');
  hazard(a, 35.6, 21.2, 34.4, 2.4);
  a.flat(band(34.6, 20.8, 71, 21.2), '#262a42');
  box(a, 36, 26.8, 34, 2.6, STEEL, { spec: true });
  feet(a, 29.4, 2.4, [38.4, 4.4], [62.6, 4.4]);
};
const radarBase = (a) => {
  a.tube('M16 20L16 25', 1.8, GUN);
  box(a, 11.4, 23.6, 9.2, 4, GUN);
  a.flat(band(12.6, 25, 19.4, 26), '#a8693c');
  box(a, 8, 27.8, 16, 2.4, STEEL, { spec: true });
};
const solarPanel = (a) => {
  a.shape(rr(.4, .6, 47.2, 11.8, .4), DARK, { lite: band(.6, .7, 47.4, 1.1) });
  for (let j = 0; j < 2; j++) for (let i = 0; i < 8; i++) {
    const x = 1.2 + i * 5.7, y = 1.3 + j * 5.4;
    a.shape(band(x, y, x + 5.2, y + 4.9), CELL, { outer: false, edge: false, lite: poly(x, y, x + 5.2, y, x + 5.2, y + .8, x, y + .8), shade: poly(x + 3.8, y + 4.9, x + 5.2, y + 3.6, x + 5.2, y + 4.9) });
    a.flat(poly(x + .6, y + 3.2, x + 3.2, y + .7, x + 3.8, y + .7, x + .6, y + 3.9), CELL.spec);
    a.flat(band(x + 2.5, y, x + 2.7, y + 4.9), '#122456');
  }
  a.shape(rr(21.8, 12.2, 4.4, 3.2, .3), GUN, { lite: band(22, 12.3, 26, 12.8) });
};
const lamp = (a) => {
  a.shape(rr(4.4, .4, 7.2, 1.8, .2), NAVY, { lite: band(4.6, .5, 11.4, .9) });
  a.flat(band(7.2, 2.2, 8.8, 4.6), '#262a42');
  a.shape(rr(5.6, 3.8, 4.8, 1.6, .3), GUN, { lite: band(5.8, 3.9, 10.2, 4.3) });
  a.shape(ell(8, 8.6, 3.7), { base: '#fff0b8', shade: '#f2c466', lite: '#fffbe8', spec: '#ffffff', ink: '#5a3c1c' }, { shade: .7, lite: ell(7.6, 8, 2.2) });
};
const arcLampBody = (lit) => (a) => {
  a.shape(rr(5.4, .6, 5.2, 2, .3), GUN, { lite: band(5.6, .7, 10.4, 1.1) });
  a.shape(rr(3.6, 2.2, 8.8, 11, 2.6), lit ? { base: '#cfeaff', shade: '#68a8e0', lite: '#ffffff', spec: '#ffffff' } : { base: '#6e9098', shade: '#3f6070', lite: '#94b4b8', spec: '#d8ecec' }, { lite: band(5, 2.6, 8, 12.6), shade: band(9.6, 2.2, 12.4, 13.2), lines: ['M3.8 4.6L12.2 4.6', 'M3.8 10.8L12.2 10.8'] });
  a.shape(rr(5, 12.8, 6, 1, .2), GUN, { outer: false });
};
const dropPod = (a) => {
  // The nose, a round porthole, the band; the skirt; the legs under it, standing on the deck.
  for (const [x0, x1] of [[5, 1], [27, 31]]) a.tube(`M${x0} 45L${x1} 55`, 1.2, GUN);
  a.tube('M16 46L16 54', 2.2, GUN);
  for (const x of [1, 31]) a.flat(band(x - 1.6, 54.4, x + 1.6, 56), '#262a42');
  a.flat(band(13.6, 54.4, 18.4, 56), '#262a42');
  a.shape('M4 37L4 18C4 8 10 .8 16 .8C22 .8 28 8 28 18L28 37Z', STEEL, {
    lite: 'M7 13C9 6 13 2 16 2C19 2 23 6 25 13C22 8 19 6 16 6C13 6 10 8 7 13Z', spec: 'M9.6 8C11 5 13.4 3 15.6 2.8C13.8 4 12 6 10.6 8.8Z', shade: ['M25 13C27 15 28 16 28 18L28 37L25.4 37Z', band(4, 33, 28, 37)], lines: ['M8 8L8 37', 'M24 8L24 37'] });
  a.shape(ell(16, 18.8, 8.6, 11), GUN, { outer: false, lite: 'M9 12C11 9 14 7.8 16 7.8C18 7.8 21 9 23 12C20 10 18 9.4 16 9.4C14 9.4 12 10 9 12Z' });
  a.shape(ell(16, 18.8, 7, 9.4), TEAL, { outer: false, shade: band(6, 19.4, 26, 29), deep: band(6, 25.4, 26, 29), lite: poly(9.4, 15, 12, 11, 15, 10, 11.4, 16) });
  a.flat(poly(12, 12.4, 14.4, 10.6, 15, 11, 12.6, 13.2), '#d0f0ec');
  a.shape(band(3.6, 37, 28.4, 39), DARK, { outer: false });
  for (const x of [7, 14, 21, 25]) a.flat(ell(x, 38, .4), '#54ba74');
  a.shape('M2.4 39L29.6 39L31.4 45.4L.6 45.4Z', STEEL, { lite: band(.6, 39, 8, 45.4), spec: band(2, 39.4, 4, 44.6), shade: [band(23, 39, 32, 45.4)], lines: ['M8 39L6.6 45.4', 'M23.8 39L25.4 45.4'] });
  a.shape(band(.6, 45.4, 31.4, 46.8), GUN, { outer: false });
};

// ---------- what runs over them ----------
const GLOW = '#fff4c8';
const flat = (w, h, draw) => bake({ w, h, draw });
function former_work(f) {
  return (a) => {
    wheel(a, 15.5, 5.6, 5.4, GUN, .5 + (f * Math.PI) / 6);
    a.flat(band(6, 12.5, 26, 23.5), '#2a2d3c');
    gear(a, 9.8, 18, 3.8, 8, COPPER, .2 + f * .2);
    gear(a, 21.2, 18, 3.8, 8, COPPER, .6 - f * .2);
    a.flat(band(12.6, 17.3, 18.4, 18.7), '#868fa2');
    const down = [0, 1, 2, 1][f];
    a.flat(band(14.5, 13, 16.5, 19 + down), '#868fa2');
    inner(a, 12.5, 19.4 + down, 6, 2, GUN);
  };
}
function crusher_work(f) {
  return (a) => {
    a.flat(band(4, 11, 22.5, 23), '#2a2d3c');
    gear(a, 9.2, 17, 4.6, 8, GUN, .3 + f * .25);
    gear(a, 16.8, 17, 4.6, 8, GUN, .7 - f * .25);
    const chips = [[8, 1.4], [20, 1], [14, .4], [17, 27.4], [11, 28.4], [13.6, 30]];
    chips.forEach(([x, y], i) => a.flat(ell(x + ((i + f) % 3) - 1, y + ((f * 2 + i) % 4) * .3, .4 + (i % 2) * .2), i % 2 ? '#a7afbc' : '#868fa2'));
  };
}
function assembler_work(f) {
  return (a) => {
    a.flat(band(5, 5, 27, 15), '#2a2d3c');
    gear(a, 7.4, 9.5, 2.2, 6, GUN, f * .5);
    const tipX = 15.5 + [0, 1, 0, -1][f], tipY = 9.5 + [0, -1, -1.6, -.8][f];
    a.tube(`M7.4 9.5L${11 + f * .3} ${6.4 + f * .2}L${tipX} ${tipY}`, 1, { ...GUN, base: '#868fa2' }, { bare: true });
    a.flat(poly(tipX - 1, tipY - 1, tipX + 1, tipY - 1, tipX + .5, tipY + 1, tipX - .5, tipY + 1), '#c4ccd8');
    a.flat(ell(21.5, 11, 2.5), '#6cc4d8');
    a.flat(ell(21.5, 11, 1.8), '#b8ecf4');
    a.flat(ell(21.1, 10.5, .8), '#ffffff');
    a.flat(band(9, 15.4, 22, 16), '#54ba74');
    for (let i = 0; i < 3; i++) a.flat(band(10 + i * 3.6, 16.6, 11.6 + i * 3.6, 17.2), (i + f) % 2 ? '#f0c460' : '#54ba74');
  };
}
function assembler_screen(f) { return (a) => { for (const [x, y, c] of [[20.4, 6.8, '#7cf0a0'], [14, 13.6, '#f0c460']]) { if ((f + (y > 10 ? 1 : 0)) % 2 === 0) a.flat(rr(x, y, 3, .9, .2), c); } }; }
function workbench_work(f) {
  return (a) => {
    const dy = [0, -.4, 0, .4][f];
    a.flat(rr(7, 11.4 + dy, 8, 2.2, .5), '#868fa2');
    a.flat(band(7.4, 11.6 + dy, 14.6, 12.2 + dy), '#c4ccd8');
    a.flat(rr(13.6, 11 + dy, 2, 3, .3), '#c8503c');
    for (let i = 0; i < 3; i++) a.flat(ell(15.8 + ((f + i * 2) % 4) * .6, 15 + i * 1.8 + (f % 2) * .6, .28), i % 2 ? '#ffd060' : '#fff4c8');
  };
}
const HOT = ['#ffd060', '#ff9a36', '#f06a22'];
function fire(f) {
  return (a) => {
    for (let i = 0; i < 5; i++) {
      const x = i * 2 + .1, k = (i + f) % 3, top = 3.6 + ((i * 2 + f) % 3) * .7;
      a.flat(band(x, top, x + 1.3, 11), '#c63a14');
      a.flat(band(x + .15, top + 1.2, x + 1.15, 11), HOT[2 - k]);
      a.flat(band(x + .35, top + 3, x + .95, 11), HOT[0]);
      a.flat(band(x + .5, top + 5, x + .8, 10.4), GLOW);
    }
    a.flat(poly(.4, 4, 2, 1.4 + (f % 2), 3, 3.2, 5, .4 + ((f + 1) % 2), 6.6, 3.4, 8, 1.6 + (f % 3) * .5, 9.6, 4, 9.6, 5, .4, 5), '#f06a22');
    a.flat(poly(1.6, 5, 3, 3, 4.4, 4.2, 5.4, 2.4, 6.8, 4.4, 8, 3.2, 8.6, 5), '#ffd060');
  };
}
const burnerWheel = (f) => (a) => wheel(a, 6.5, 6.5, 6, GUN, .4 + (f * Math.PI) / 6);
const fill = (n) => (a) => {
  if (n === 0) { a.flat(band(21.2, 9, 23, 10.2), '#ff5040'); return; }
  const top = 21 - 2 * n;
  a.flat(band(13, top, 19, 21), '#2ca4d8');
  a.flat(band(13, top, 19, top + Math.min(.8, n)), '#a8ecff');
  a.flat(band(13.6, top + .8, 14.6, 21), '#6cc8f0');
};
/** The radar's dish, seen turning: from edge on at frames 0 and 4 to face on at 2 and 6. */
const dish = (f) => (a) => {
  const k = Math.abs(Math.cos(((f % 8) / 8) * Math.PI * 2 + Math.PI / 2)), w = 2.4 + k * 6.2, cx = 16.4;
  a.tube('M16 13L16 20.4', 1.8, GUN);
  a.shape(ell(cx, 10.6, w, 8.4), STEEL, { lite: ell(cx - w * .2, 8.4, w * .6, 5), shade: .9, spec: ell(cx - w * .3, 5.8, w * .22, 1.4) });
  a.flat(ell(cx, 11, w * .24, 1.4), '#a8693c');
  a.flat(ell(cx - w * .04, 10.8, w * .1, .6), '#f4c896');
};
const mast = (a) => radarBase(a);
const beacon = (a) => { a.flat(ell(18, 24, 1.4), '#ff5040'); a.flat(ell(18, 24, .7), '#ffd0c0'); };
const computerLights = (f) => (a) => {
  for (let i = 0; i < 9; i++) { const on = (i * 5 + f * 3) % 4 !== 0; a.flat(rr(5.4 + (i % 3) * 2, 21.4 + Math.floor(i / 3) * 2, .9, .9, .2), on ? '#54ba74' : '#1a5040'); }
  a.flat(rr(29.8, 13, 1, .6, .1), f ? '#54ba74' : '#f0c460');
};
const computerRadar = (f) => (a) => {
  const CX = 20, CY = 12, c = (r) => `M${CX - r} ${CY}A${r} ${r} 0 0 1 ${CX + r} ${CY}`;
  a.flat(band(9.6, 2.8, 30, 12), '#0e2a20');
  for (const r of [3.4, 6.4, 8.8]) a.line(c(r), { w: 1.4, c: '#2b7a52' });
  const t = Math.PI + ((f % 8) / 8) * Math.PI;
  a.flat(poly(CX, CY, CX + Math.cos(t) * 9, CY + Math.sin(t) * 9, CX + Math.cos(t - .4) * 9, CY + Math.sin(t - .4) * 9), '#54ba74');
  a.flat(band(26, 11, 30, 12), '#a0f0b0');
};
const computerScreen = (f) => (a) => {
  a.flat(band(9.6, 2.8, 30, 12), '#0c2438');
  for (let i = 0; i < 5; i++) { const y = 3.8 + i * 1.6, w = 6 + ((i * 7 + f * 5) % 12); a.flat(band(11, y, 11 + w, y + .8), i % 3 ? '#8ad8f0' : '#e0f6ff'); }
};
const plantStage = (s) => (a) => {
  const foot = 31, top = [26, 20, 12, 7][s], cx = 16, wide = [2.6, 4.6, 6.6, 8.6][s];
  const leaf = (x, y, dir, l, m) => a.shape(poly(x, y, x + dir * l * .5, y - l * .55, x + dir * l, y - l * .4, x + dir * l * .55, y + l * .1), m, { outer: s > 0 ? undefined : false, lite: poly(x, y, x + dir * l * .5, y - l * .55, x + dir * l, y - l * .4) });
  const G1 = { base: '#3f9a3c', shade: '#256a36', lite: '#79c850', spec: '#c8ec84', ink: '#0d2a18' };
  a.tube(`M${cx} ${foot}L${cx} ${top + 1}`, .8, G1, { bare: true });
  const n = [1, 2, 4, 6][s];
  for (let i = 0; i < n; i++) {
    const y = foot - 1.4 - (i * (foot - top - 2)) / Math.max(1, n - .5), l = wide * (1 - i * .08);
    leaf(cx, y, 1, l, G1); leaf(cx, y - .8, -1, l, G1);
  }
  if (s === 3) for (const [x, y] of [[10, 16], [21, 15], [13, 24], [20, 22], [15, 10]]) { a.flat(ell(x, y, .9), '#f0d040'); a.flat(ell(x - .2, y - .2, .35), '#fff4b0'); }
};
const growLight = (a) => { a.flat(band(9, 7, 23, 8), '#ff78c0'); a.flat(band(12, 7.2, 20, 7.7), '#ffd8ec'); };
const podGlass = (a) => {
  a.flat(band(15.6, 9.4, 28.4, 36), '#f4c860');
  a.flat(poly(15.6, 16, 22, 9.4, 28.4, 9.4, 28.4, 10, 15.6, 22.6), '#ffe8a8');
  a.flat(ell(22, 14, 3), '#94662c');
  a.flat(rr(18.6, 17, 6.8, 11, 1), '#94662c');
  a.flat(band(19.6, 28, 21.4, 36), '#94662c'); a.flat(band(22.6, 28, 24.4, 36), '#94662c');
};
const arcAlight = (a) => { a.line('M14.4 14L16.4 13.4L17.6 14.6L20.4 14', { w: 3, c: '#c8b8ff' }); a.line('M14.4 14L16.4 13.4L17.6 14.6L20.4 14', { w: 1.4, c: '#ffffff' }); };
const arcFront = (a) => { a.flat(rr(12, 9.6, 11, 2.8, .4), '#6c3a30'); for (let i = 0; i < 6; i++) a.flat(rr(12.6 + i * 1.75, 10, 1.1, 2, .2), i % 2 ? '#f0a040' : '#ffc060'); };

Subjects.machines = function () {
  const whole = { furnace: [32, 32, furnace], former: [32, 32, former], crusher: [32, 32, crusher], assembler: [32, 32, assembler], workbench: [32, 40, workbench], arcBench: [32, 32, arcBench],
    vent: [16, 16, vent], crate: [32, 16, crate], cistern: [32, 32, cistern], growCapsule: [32, 48, growCapsule], charger: [16, 32, charger], burnerGenerator: [32, 32, burnerGenerator],
    autoTradeStation: [32, 32, terminal(true)], tradeStation: [32, 32, terminal(false)], droneBay: [32, 32, droneBay], lifepod: [48, 48, lifepod], generator: [32, 32, generator], tank: [32, 32, tank],
    computer: [48, 32, computer], capacitorBank: [32, 32, capacitorBank], engine: [72, 32, engine], solarPanel: [48, 16, solarPanel], lamp: [16, 16, lamp], arcLamp: [16, 16, arcLampBody(false)], dropPod: [32, 56, dropPod] };
  for (const [kind, [w, h, draw]] of Object.entries(whole)) out(`machine_${kind}`, bake({ w, h, draw }));
  out('machine_arcLamp_lit', flat(16, 16, arcLampBody(true)));
  out('machine_radarAntenna', flat(32, 32, (a) => { radarBase(a); dish(2)(a); }));
  out('radarAntenna_mast', flat(32, 32, mast));
  out('radarAntenna_beacon', flat(32, 32, beacon));
  for (let f = 0; f < 8; f++) { out(`radarAntenna_dish_${f}`, flat(32, 32, dish(f))); out(`computer_radar_${f}`, flat(48, 32, computerRadar(f))); }
  for (let f = 0; f < 4; f++) {
    out(`former_work_${f}`, flat(32, 32, former_work(f)));
    out(`crusher_work_${f}`, flat(32, 32, crusher_work(f)));
    out(`assembler_work_${f}`, flat(32, 32, assembler_work(f)));
    out(`assembler_screen_${f}`, flat(32, 32, assembler_screen(f)));
    out(`workbench_work_${f}`, flat(32, 40, workbench_work(f)));
    out(`burner_wheel_${f}`, flat(13, 13, burnerWheel(f)));
    out(`computer_screen_${f}`, flat(48, 32, computerScreen(f)));
    out(`growCapsule_plant_${f}`, flat(32, 48, plantStage(f)));
  }
  for (let f = 0; f < 2; f++) out(`computer_lights_${f}`, flat(48, 32, computerLights(f)));
  for (let f = 0; f < 3; f++) out(`fire_${f}`, flat(10, 11, fire(f)));
  for (let n = 0; n < 6; n++) out(`tank_fill_${n}`, flat(32, 32, fill(n)));
  out('growCapsule_work_0', flat(32, 48, growLight));
  out('lifepod_glass', flat(48, 48, podGlass));
  out('arcBench_alight', flat(32, 32, arcAlight));
  out('arcBench_front', flat(32, 32, arcFront));
};
window.AnimeKit = { STEEL, GUN, DARK, COPPER, band, box, inner, cyl, rivets, hazard };
})();

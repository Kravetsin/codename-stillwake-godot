// The mafia's deliveries (2026-11-08-mafia-deliveries), as the user picked them: the parcel a tarp bundle tied with cord and
// sealed in lead (16 × 16 world pixels), the crate a steel box in cargo straps with a beacon on top (24 × 24; the game
// blinks its lens), the seized container ribbed, taped, stamped and sealed (40 × 24). Broken, the game's own cracks and wedges.
(function () {
'use strict';
const { ell, rr, poly, bake } = Anime;
const D = DebrisKit, { band } = D;

const M = {
  TARP: { base: '#6c6a54', shade: '#46453a', deep: '#2f2e28', lite: '#8a876a', spec: '#a6a284', ink: '#12120e' },
  TARP2: { base: '#5c604f', shade: '#3e4236', deep: '#2a2d25', lite: '#7a7e68', spec: '#9a9e86', ink: '#12120e' },
  CORD: { base: '#8c7652', shade: '#5c4c36', deep: '#3c3226', lite: '#ad9670', spec: '#c4b08a', ink: '#16100c' },
  LEAD: { base: '#7c8088', shade: '#4e525c', deep: '#353840', lite: '#a0a4ac', spec: '#d0d4d8', ink: '#0e1018' },
  GREEN: { base: '#4a5446', shade: '#30382e', deep: '#20261f', lite: '#68745f', spec: '#9aa894', ink: '#0c100c' },
  TAPE: { base: '#858278', shade: '#5c5a53', deep: '#3e3d39', lite: '#a4a196', spec: '#c4c0b4', ink: '#121210' },
  BTAPE: { base: '#86663e', shade: '#5c462c', deep: '#3e3022', lite: '#a68452', spec: '#c4a474', ink: '#16100c' },
  WAX: { base: '#74302a', shade: '#4e1f1b', deep: '#361513', lite: '#94463c', spec: '#b86a5c', ink: '#140a08' },
  FILM: { base: '#3a3f3c', shade: '#262a28', deep: '#191c1b', lite: '#555b56', spec: '#c8d0cc', ink: '#0a0c0b' },
  PAPER: { base: '#a29c88', shade: '#6e6a5c', deep: '#4c4a40', lite: '#bab4a0', spec: '#d0cab6', ink: '#141410' },
  RED: { base: '#6e3a2c', shade: '#4a2620', deep: '#331a17', lite: '#8c5040', spec: '#b07a66', ink: '#140c0a' },
  STRAP: { base: '#7a5a2e', shade: '#54401f', deep: '#3a2c18', lite: '#987640', spec: '#b49460', ink: '#16100c' },
  FOAM: { base: '#3c3e44', shade: '#2a2c31', deep: '#1c1d21', lite: '#55585f', spec: '#7a7e86', ink: '#0c0d10' },
  ENAMEL: { base: '#b4b2a8', shade: '#7a7a76', deep: '#55565a', lite: '#cac8be', spec: '#e6e2d6', ink: '#0e1018' },
  NAVY: { base: '#3c4c66', shade: '#2a3648', deep: '#1c2432', lite: '#56688a', spec: '#8a9ab8', ink: '#0c1018' },
  TRUNK: { base: '#3e3a36', shade: '#2a2724', deep: '#1c1a18', lite: '#57524c', spec: '#7a746c', ink: '#0e0c0a' },
};
/** Seizure ink: a muted red, as old stamp ink dries. */
const STAMP = '#8e3a30';
/** A lead seal hung on a wire: the wire from (x0, y0) bending to the seal at (x, y). */
function seal(a, x0, y0, x, y, r) {
  const q = r || 1.1;
  a.tube(`M${x0} ${y0}Q${(x0 + x) / 2 + .8} ${(y0 + y) / 2 - .4} ${x} ${y - q * .8}`, .24, M.LEAD, { spec: false });
  a.shape(ell(x, y, q, q * .92), M.LEAD, { shade: q * .3, spec: ell(x - q * .3, y - q * .35, q * .28, q * .2), lines: [`M${x - q * .5} ${y + .1}Q${x} ${y - q * .5} ${x + q * .5} ${y + .1}`] });
}
/** A round stamp in seizure ink: a ring, an inner ring, marks that are no letters, smudged where it dried thin. */
function stamp(a, x, y, r, clip) {
  a.line(Anime.ell(x, y, r, r * .96), { w: 1.8, c: STAMP });
  a.line(Anime.ell(x, y, r * .62, r * .6), { w: 1.1, c: STAMP });
  a.flat(Anime.star(x, y, r * .4, .3), STAMP, clip ? { clip } : undefined);
  for (const t of [.5, 1.5, 2.6, 3.7, 4.8, 5.7]) a.line(`M${x + Math.cos(t) * r * .7} ${y + Math.sin(t) * r * .7}L${x + Math.cos(t + .28) * r * .86} ${y + Math.sin(t + .28) * r * .86}`, { w: .9, c: STAMP });
}
/** A plate box by its corner and size, its line inside only (debris.js keeps its own unexported). */
const box = (a, x, y, w, h, m, o) => DebrisKit.plate(a, [[x, y], [x + w, y], [x + w, y + h], [x, y + h]], m, { outer: false, ...o });

// A: a bundle of tarp, tied crosswise with cord, a lead seal on the knot.
function tarp(a) {
  const body = 'M2.2 3.2Q8 1.4 13.8 2.6Q15.4 8 14.6 13.4Q8 15 1.8 13.8Q.6 8.4 2.2 3.2Z';
  a.shape(body, M.TARP, {
    lite: 'M0 0L16 0L16 3.6Q8 2.8 0 4.8Z', shade: ['M16 1L13 3.6Q14 8 12.8 16L16 16Z', 'M0 12.6Q8 13.6 16 12.2L16 16L0 16Z'],
    lines: ['M3 4.4Q5 5.6 6.6 7.4', 'M13 4Q11.4 5.4 9.8 7.4', 'M3.2 12.6Q5 11 6.8 9.8', 'M13.2 12.8Q11.4 11 9.8 9.8', 'M4.6 2.8Q4.2 3.9 4.7 5'],
  });
  a.flat('M2.6 10.4L4.6 9.8L5.6 11.4L4.4 13L2.6 12.8Z', M.TARP.shade, { clip: body });
  a.shape(poly(10.2, 3.6, 13.2, 3.3, 13.5, 6, 10.5, 6.2), M.TARP2, { outer: false, lite: band(10, 3, 14, 3.9), shade: band(12.9, 3, 14, 6.4) });
  for (const d of ['M10 4.1L10.6 4.3', 'M10.1 5.1L10.7 5.2', 'M11.2 6.1L11.4 6.6', 'M12.4 6L12.6 6.5']) a.line(d, { w: .9 });
  a.tube('M1.4 8.4Q8 9.3 14.9 8.1', .78, M.CORD, { spec: false });
  a.tube('M8.3 2.2Q8.9 8.4 8 14.5', .78, M.CORD, { spec: false });
  a.tube('M7.8 9.2Q6.7 10.6 6.9 11.9', .55, M.CORD, { spec: false });
  a.shape(ell(8.4, 8.7, 1.25, 1.05), M.CORD, { shade: .35, lines: ['M7.6 8.4L9.2 9'] });
  seal(a, 9.3, 9.3, 11, 12, 1.25);
}

const corner = (a, x, y, u, v, m) => a.shape(poly(x, y, x + u * 3.2, y, x + u * 3.2, y + v * 1.4, x + u * 1.4, y + v * 1.6, x + u * 1.4, y + v * 3.2, x, y + v * 3.2), m, { outer: false, lite: band(x - 2, y - 2, x + 4, y + (v > 0 ? .5 : -1)) });
// D: a steel box in cargo straps with a ratchet buckle, a beacon blinking on top so the stream can be searched. Its panels burst.
function strapped(a, s) {
  const d = D.plate(a, [[1.4, 3], [22.6, 3], [22.6, 22.6], [1.4, 22.6]], D.PLATE, { seed: 44, l: 1 });
  for (const [x, y, w, h] of [[3, 4.8, 8.4, 6.2], [12.6, 4.8, 8.4, 6.2], [3, 14.6, 8.4, 6.4], [12.6, 14.6, 8.4, 6.4]]) {
    a.flat(band(x, y, x + w, y + .4), D.PLATE.deep, { clip: d }); a.flat(band(x, y + h - .4, x + w, y + h), D.PLATE.lite, { clip: d });
    D.rv(a, [x + .8, y + 1.2], [x + w - .8, y + h - 1.2]);
  }
  D.rusted(a, 21.2, 21.2, 1.2, .9, d);
  if (s >= 1) { a.flat(poly(3.6, 16, 7.4, 15.4, 8, 18.6, 4.2, 19), D.PLATE.shade, { clip: d }); a.line('M3.6 16Q5.6 15.2 7.4 15.4', { w: .9 }); }
  if (s >= 2) { a.line('M14 17L16.6 18.6L16 20.4', { w: 1.2 }); a.flat(poly(13.6, 6, 16.4, 5.4, 16, 7.6), D.PLATE.deep, { clip: d }); }
  const hx1 = s >= 2 ? 19.6 : 23.4;
  a.shape(band(.6, 11.6, hx1, 13.6), M.STRAP, { lite: band(0, 11.6, 24, 12.1), shade: band(0, 13.1, 24, 13.6), lines: ['M2 12.6L14 12.6'] });
  if (s >= 2) a.shape(poly(21.4, 11.6, 23.4, 11.6, 23.6, 15.6, 22.2, 16.4), M.STRAP, { shade: band(22.8, 11, 24, 17) });
  if (s >= 1) for (const d2 of ['M19.2 11.8L19.8 13.4', 'M20 11.6L20.4 13.2']) a.line(d2, { w: .8 });
  for (const x of [6.6, 15.4]) a.shape(band(x, 2.4, x + 2, 23.2), M.STRAP, { lite: band(x, 0, x + .5, 24), shade: band(x + 1.5, 0, x + 2, 24), lines: [`M${x + 1} 3.6L${x + 1} 22`] });
  a.shape(rr(14.6, 11, 3.6, 3.2, .4), D.GUN, { lite: band(14.6, 11, 18.2, 11.6), shade: band(17.6, 11, 18.2, 14.2), lines: ['M15.2 12.6L17.6 12.6'] });
  for (const [x, y, u, v] of [[1.6, 3.2, 1, 1], [22.4, 3.2, -1, 1], [1.6, 22.4, 1, -1], [22.4, 22.4, -1, -1]]) corner(a, x, y, u, v, D.GUN);
  a.shape(rr(9.6, .3, 4.8, 3, .8), D.GUN, { lite: band(9.6, .3, 14.4, 1.1), shade: band(13.6, .3, 14.4, 3.3) });
  a.shape(ell(12, 1.6, 1.3, 1), { base: '#5a4a2a', shade: '#3c321e', deep: '#2a2416', lite: '#7a6438', spec: '#e0c890', ink: '#140f08' }, { outer: false });
}

/** Seizure tape from (x0, y0) to (x1, y1), w wide: yellow with black bands, its ends torn across. */
function tape(a, x0, y0, x1, y1, w) {
  const L = Math.hypot(x1 - x0, y1 - y0), ux = (x1 - x0) / L, uy = (y1 - y0) / L, nx = -uy * w / 2, ny = ux * w / 2;
  const at = (t, k) => [x0 + ux * t + nx * k, y0 + uy * t + ny * k];
  const strip = poly(...at(0, 1), ...at(L * .5, 1.05), ...at(L, 1), ...at(L - .5, .2), ...at(L - .1, -.4), ...at(L, -1), ...at(L * .5, -1.05), ...at(0, -1), ...at(.4, -.3), ...at(.1, .4));
  a.shape(strip, { base: '#b8962e', shade: '#8a7024', deep: '#5c4c1c', lite: '#cfae48', spec: '#e2c870', ink: '#16120a' }, { outer: false, shade: .3 });
  for (let t = .6; t < L - .8; t += 2.2) a.flat(poly(...at(t, 1.2), ...at(t + .9, 1.2), ...at(t + .3, -1.2), ...at(t - .6, -1.2)), '#1a1820', { clip: strip });
}

// A: a ribbed freight container, crossed with seizure tape, lead seals on its lock bars, stamped twice. Its doors burst.
function boxBody(a, interior) {
  const d = D.plate(a, [[1, 2], [39, 2], [39, 22.6], [1, 22.6]], D.HULL, { seed: 51, l: 1 });
  if (interior) { a.flat(band(3, 3.6, 37, 21), D.SOOT.deep, { clip: d }); a.flat(band(3, 3.6, 37, 5), D.SOOT.base, { clip: d }); }
  else for (let x = 3; x < 37; x += 3) { a.flat(band(x, 3.6, x + 1.2, 21), D.HULL.lite, { clip: d }); a.flat(band(x + 1.2, 3.6, x + 1.8, 21), D.HULL.shade, { clip: d }); }
  box(a, 1, 2, 38, 1.6, D.GUN, { l: .4, b: .4, e: .4 });
  box(a, 1, 21, 38, 1.6, D.GUN, { l: .4, b: .4, e: .4 });
  for (const [x, y] of [[1, 2], [36, 2], [1, 20.2], [36, 20.2]]) { a.shape(rr(x, y, 3, 2.4, .4), D.GUN, { lite: band(x, y, x + 3, y + .6) }); a.flat(rr(x + .8, y + .6, 1.4, 1.1, .4), D.SOOT.deep); }
  D.rusted(a, 37, 19.6, 1.4, 1, d);
  D.rusted(a, 4.4, 4.6, 1, .8, d);
  return d;
}
function seized(a, s) {
  const d = boxBody(a, false);
  a.flat(band(19.7, 3.6, 20.3, 21), D.HULL.deep, { clip: d });
  if (s >= 1) { a.flat(poly(6, 13, 10, 12, 10.8, 15.6, 6.8, 16.4), D.HULL.deep, { clip: d }); a.line('M6 13Q8 11.8 10 12', { w: .9, c: D.HULL.spec }); }
  if (s >= 2) { a.flat(poly(20.3, 4, 22.6, 4.4, 22.4, 12, 20.3, 14), D.SOOT.deep, { clip: d }); a.line('M27 18L29.4 19.6L29 21', { w: 1.1 }); }
  for (const x of [17.2, 22.8]) { a.tube(`M${x} 3.8L${x} 20.8`, .8, D.GUN); a.shape(rr(x - 1, 11, 2, 3.2, .4), D.GUN, { lite: band(x - 1, 11, x + 1, 11.6), shade: band(x + .4, 11, x + 1, 14.2) }); }
  stamp(a, 30.6, 8.2, 3);
  stamp(a, 8.4, 17, 2.3);
  if (s >= 1) tape(a, 3, 4.6, 27, 15.6, 2); else tape(a, 3, 4.6, 37, 20.2, 2);
  tape(a, 3.4, 20, 37, 4.4, 2);
  if (s >= 1) a.shape(poly(27.2, 14.8, 28.6, 15.8, 27.4, 19.4, 26.2, 18.6), { base: '#b8962e', shade: '#8a7024', deep: '#5c4c1c', lite: '#cfae48', spec: '#e2c870', ink: '#16120a' }, { shade: .3 });
  seal(a, 17.6, 14.4, 18.4, 17.4, 1);
  if (s < 2) seal(a, 23.2, 14.4, 24.2, 17.4, 1);
}

/** A baked picture fitted whole into an item's 16 px icon, drawn at 4 times as every picture is, centred. */
function icon(picture) {
  const c = document.createElement('canvas'), s = 64 / Math.max(picture.width, picture.height), w = picture.width * s, h = picture.height * s;
  c.width = c.height = 64;
  const g = c.getContext('2d');
  g.imageSmoothingQuality = 'high';
  g.drawImage(picture, (64 - w) / 2, (64 - h) / 2, w, h);
  return c;
}

Subjects.deliveries = function () {
  const parcel = bake({ w: 16, h: 16, draw: tarp }), crate = bake({ w: 24, h: 24, draw: (a) => strapped(a, 0) }), seizedBox = bake({ w: 40, h: 24, draw: (a) => seized(a, 0) });
  out('debris/parcel_01', parcel);
  out('debris/deliverycrate_01', crate);
  out('debris/seized_01', seizedBox);
  // Their items' icons, never carried but in creative mode, as every wreck's.
  out('items/parcel', icon(parcel));
  out('items/deliveryCrate', icon(crate));
  out('items/confiscated', icon(seizedBox));
};
})();

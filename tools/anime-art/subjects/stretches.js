// The stretches of space (docs/plans/stretches.md), as the user picked them: the ice belt's blocks, five druses (clear hexagonal
// crystals grown together, the pick «Друза»), the contraband drift's four crates (the case, the drum, the double bottom, the bundle;
// their amber lamps drawn dark: the game lights them), their goods «Опечатанная кассета», and the frost on the hull «Кромка инея» as a
// sheet of cell tiles the game grows and melts.
// After debris.js, whose kit it reads when it runs.
(function () {
'use strict';
const { ell, rr, poly, star, bake } = Anime;
const f2 = (v) => +v.toFixed(2);
const P = (pts) => poly(pts.map(f2));
const path = (pts) => 'M' + pts.map((v, i) => (i && i % 2 === 0 ? 'L' : '') + f2(v) + (i % 2 ? '' : ' ')).join('');

const DIRTY = { base: '#93acb8', shade: '#5b7282', deep: '#3a4a58', lite: '#c4d6de', spec: '#eef4f6', ink: '#0d151d' };
const CLEAR = { base: '#8fb9cc', shade: '#56819c', deep: '#355a74', lite: '#c6e0ea', spec: '#f2f8fa', ink: '#0c1622' };
const CASE = { base: '#3c414b', shade: '#272a33', deep: '#191b22', lite: '#59606c', spec: '#8a92a0', ink: '#0c0d13' };
const LENS = { base: '#6a4a1e', shade: '#4a3216', deep: '#33220e', lite: '#8a6428', spec: '#c8a060', ink: '#16100a' };
const TAPE = { base: '#b8862c', shade: '#86601e', deep: '#5a4016', lite: '#d4a448', spec: '#e8c878', ink: '#16100a' };
const TARP = { base: '#4e5648', shade: '#353b32', deep: '#242822', lite: '#69705f', spec: '#8c927e', ink: '#0d100c' };
const STRAP = { base: '#a0642a', shade: '#70461e', deep: '#4c3016', lite: '#c08040', spec: '#d8a060', ink: '#16100a' };
const RIME = { base: '#c3d1d8', shade: '#8ea3ae', deep: '#6c8290', lite: '#e6eef1', spec: '#f6fafb', ink: '#1a2630' };

/** A hexagonal prism seen from the side: its lit top face, its middle, its shaded foot; pointed ends. Its points: s along it, k of its half-width out. */
function prism(a, x0, y0, x1, y1, hw, M, spec) {
  const L = Math.hypot(x1 - x0, y1 - y0), ux = (x1 - x0) / L, uy = (y1 - y0) / L;
  let nx = uy, ny = -ux;
  if (nx * -0.6 + ny * -0.8 < 0) { nx = -nx; ny = -ny; }
  const e = hw * 0.75, pt = (s, k) => [x0 + ux * s + nx * hw * k, y0 + uy * s + ny * hw * k];
  const A = pt(0, 0), B = pt(e, 1), C = pt(L - e, 1), D = pt(L, 0), E = pt(L - e, -1), F = pt(e, -1);
  const B2 = pt(e * 0.9, 0.36), C2 = pt(L - e * 0.9, 0.36), E2 = pt(L - e * 0.9, -0.36), F2 = pt(e * 0.9, -0.36);
  a.shape(P([...A, ...B, ...C, ...D, ...E, ...F]), M, {
    lite: P([...A, ...B, ...C, ...D, ...C2, ...B2]),
    shade: P([...A, ...F2, ...E2, ...D, ...E, ...F]),
    spec: spec ? P([...pt(e * 1.2, 0.86), ...pt(L * 0.55, 0.86), ...pt(L * 0.5, 0.6), ...pt(e * 1.3, 0.62)]) : undefined,
    lines: [path([...B2, ...C2]), path([...F2, ...E2])],
  });
  return { pt, L };
}

/**
 * A druse (the pick «Друза»): the dirty ice it grew out of, if any, then its crystals back to front, each [x0, y0, x1, y1, half-width,
 * glinting]; its shines a hard white line along a crystal, [which, from, to of its length, k of its half-width out, width]; a star where
 * the light catches a tip.
 */
function druse(d) {
  return (a) => {
    if (d.base) a.shape(P(d.base), DIRTY, { shade: 1, lite: d.baseLite ? P(d.baseLite) : undefined });
    const drawn = d.crystals.map(([x0, y0, x1, y1, hw, spec]) => prism(a, x0, y0, x1, y1, hw, CLEAR, spec));
    for (const [x, y, r] of d.stars) a.flat(star(x, y, r, 0.2), '#f6fbfc');
    for (const [i, s0, s1, k, w] of d.shines) {
      const { pt, L } = drawn[i];
      a.line(path([...pt(L * s0, k), ...pt(L * s1, k)]), { c: '#e8f2f6', w });
    }
  };
}

// The five druses, 30 by 26 or so: as many crystals, as long, as thick and as tilted each its own, some on the dirty ice they grew from.
const DRUSES = [
  // The pick's own: five grown together on a lump of dirty ice, the big one lying low.
  {
    w: 30, h: 26,
    base: [4.6, 19.6, 7, 16.4, 11.4, 15.8, 14, 17.6, 13.2, 20.8, 9.6, 22.6, 6, 22], baseLite: [4.6, 19.6, 7, 16.4, 11.4, 15.8, 8, 18.4],
    crystals: [[12.5, 7, 19.5, 2.2, 2.4], [7.5, 22.5, 21, 17, 3.2], [13.6, 19.6, 17.6, 23.6, 1.3], [22.2, 12.4, 27.4, 15, 1.4], [2, 15.5, 28, 7.5, 5, true]],
    stars: [[21.4, 6.4, 1.4]],
    shines: [[4, 0.2, 0.55, 0.15, 1.6], [4, 0.66, 0.87, 0.15, 1.2], [1, 0.36, 0.72, 0.3, 1]],
  },
  // A fan of three long thin needles from one root, bare.
  {
    w: 30, h: 26,
    crystals: [[5, 22, 13, 2.4, 2], [4, 23.4, 28.4, 15.4, 2.4], [3.4, 22.6, 26.6, 4.6, 3.4, true]],
    stars: [[25.4, 5.6, 1.3]],
    shines: [[2, 0.18, 0.62, 0.2, 1.3], [1, 0.3, 0.7, 0.25, 0.9]],
  },
  // A squat crowd of short thick ones standing up out of a wide crust.
  {
    w: 30, h: 26,
    base: [2, 21, 6, 18.4, 12, 17.8, 20, 18.4, 27, 19.4, 28.6, 22.4, 24, 24.6, 14, 25, 5, 24.4], baseLite: [2, 21, 6, 18.4, 12, 17.8, 9, 20],
    crystals: [[4, 22, 4.6, 12.6, 2], [20, 21, 24.6, 7.6, 3.2], [23.4, 22, 28.4, 15, 2], [6.6, 20.6, 9.4, 5.4, 3.4], [12.6, 21, 14.2, 2.4, 4.2, true]],
    stars: [[14.4, 3.6, 1.5]],
    shines: [[4, 0.22, 0.62, 0.3, 1.5], [3, 0.3, 0.7, 0.3, 1]],
  },
  // One great crystal, steep, a small one either side of its foot; bare.
  {
    w: 28, h: 26,
    crystals: [[13.4, 22.4, 24.6, 17, 1.8], [8.6, 24.4, 19.6, 1.6, 5.4, true], [3.6, 13.6, 10, 18.6, 1.5]],
    stars: [[19, 3.4, 1.6], [5, 14.4, 0.9]],
    shines: [[1, 0.2, 0.5, 0.2, 1.6], [1, 0.6, 0.8, 0.2, 1.2]],
  },
  // Two long ones crossed over a knot of dirty ice, a stub between.
  {
    w: 30, h: 26,
    base: [9.4, 14.4, 13.6, 11.4, 19, 12, 20.8, 15.4, 17.4, 18.6, 11.6, 18], baseLite: [9.4, 14.4, 13.6, 11.4, 19, 12, 13, 14],
    crystals: [[2.4, 5.6, 27.6, 19.4, 2.8], [13.2, 15, 16.2, 24.4, 1.6], [2.4, 21.4, 27.6, 4.6, 3.8, true]],
    stars: [[26, 6, 1.3]],
    shines: [[2, 0.16, 0.5, 0.2, 1.4], [0, 0.6, 0.85, 0.3, 0.9]],
  },
];

// ---------- the contraband drift's crates: the pick's four, each with its amber lamps drawn dark (the game lights them, CrateLamps) ----------
const lens = (a, x, y, r, ring) => {
  const { OLD } = window.DebrisKit;
  if (ring) a.shape(ell(x, y, r * 1.5, r * 1.35), OLD, { shade: r * 0.3 });
  a.shape(ell(x, y, r, r * 0.86), LENS, { outer: !ring, shade: r * 0.35, spec: ell(x - r * 0.35, y - r * 0.35, r * 0.25) });
};
const corners = (a, w, h, s) => {
  const { OLD, band } = window.DebrisKit;
  for (const [x, y, u, v] of [[0.5, 0.5, 1, 1], [w - 0.5, 0.5, -1, 1], [0.5, h - 0.5, 1, -1], [w - 0.5, h - 0.5, -1, -1]])
    a.shape(P([x, y, x + u * s, y, x + u * s, y + v * s * 0.42, x + u * s * 0.42, y + v * s * 0.48, x + u * s * 0.42, y + v * s, x, y + v * s]), OLD, { outer: false, lite: band(x - 2, y - 2, x + 4, y + (v > 0 ? 0.5 : -1)) });
};

/** «Кейс», drawn 24 by 17: handle, ribbed shell, two locks and the lamp between them, the serial filed off. */
function hardCase(a) {
  const { OLD, PLATE, SOOT, band, rv } = window.DebrisKit;
  a.tube('M9 3.4L9 1.4L15 1.4L15 3.4', 1.1, OLD, { spec: false });
  a.shape(rr(1, 3, 22, 13, 2.2), CASE, { lite: band(1, 3, 23, 4.2), shade: [band(1, 14.4, 23, 16), band(21.4, 3, 23, 16)] });
  a.shape(band(1.4, 8.8, 22.6, 9.9), CASE, { outer: false, lite: band(1.4, 8.8, 22.6, 9.2), shade: band(1.4, 9.5, 22.6, 9.9) });
  a.shape(band(1.4, 12.2, 22.6, 13.2), CASE, { outer: false, lite: band(1.4, 12.2, 22.6, 12.6), shade: band(1.4, 12.8, 22.6, 13.2) });
  a.line('M1.4 7.2L22.6 7.2', { w: 1.3 });
  for (const x of [4.6, 17.2]) { a.shape(rr(x, 5, 2.4, 3.6, 0.5), PLATE, { shade: 0.5 }); a.flat(band(x + 0.7, 6.4, x + 1.7, 7.2), SOOT.deep); }
  lens(a, 12, 5.6, 1.05, true);
  a.flat(rr(14.4, 10.2, 6, 1.7, 0.3), '#4a505a');
  a.line('M14.8 10.4L19.6 11.8', { w: 0.8, c: '#8a92a0' }); a.line('M15.2 11.8L20 10.4', { w: 0.8, c: '#8a92a0' }); a.line('M16.4 10.3L17.6 11.9', { w: 0.7, c: '#8a92a0' });
  corners(a, 24, 17, 3);
  rv(a, [3, 11], [21, 11]);
  a.flat(P([5, 14.6, 8, 14.2, 7.4, 15.2]), '#59606c');
}

/** «Бочка», drawn 18 by 24: a sealed drum in brown paint, three hoops, paint knocked to the metal, a rough smear of a mark, three lamps on its rim. */
function drum(a) {
  const { OLD, OX, band, rusted, rv } = window.DebrisKit;
  a.shape(rr(2, 2.6, 14, 19.6, 1.6), OX, { lite: band(3.6, 2, 6.4, 23), shade: band(12.4, 2, 16, 23), deep: 0.8 });
  a.shape(rr(1.6, 1.4, 14.8, 2.6, 0.6), OLD, { lite: band(1.6, 1.4, 16.4, 2), shade: band(1.6, 3.4, 16.4, 4) });
  a.shape(rr(1.6, 20.6, 14.8, 2.6, 0.6), OLD, { lite: band(1.6, 20.6, 16.4, 21.2), shade: band(1.6, 22.6, 16.4, 23.2) });
  for (const y of [7, 14.4]) a.shape(band(1.6, y, 16.4, y + 1.6), OLD, { outer: false, lite: band(2.4, y, 5.6, y + 1.6), shade: band(12.6, y, 16.4, y + 1.6) });
  a.flat(P([10.6, 4.6, 13.4, 4.8, 14.2, 6.6, 11.6, 6.4]), '#6b6e74');
  a.flat(P([3, 17, 5.6, 16.6, 5.2, 19.4, 3.2, 19.6]), '#6b6e74');
  a.flat(P([12.4, 10.2, 14.6, 9.6, 14.8, 12.4, 13, 12.8]), OX.deep);
  a.flat(P([6.2, 9.4, 10.6, 9.2, 10.2, 10.4, 8.8, 13, 7.8, 13.2, 6.6, 10.6]), '#bdb39c');
  a.line('M7 10.2L9.6 10.2', { w: 0.8, c: '#6a2e2a' });
  rusted(a, 13.8, 21.6, 1, 0.7); rusted(a, 3.2, 8, 0.8, 0.6);
  for (const x of [5, 9, 13]) lens(a, x, 7.8, 0.68, false);
  rv(a, [3.2, 15.2], [14.8, 15.2]);
}

/** «Двойное дно», drawn 22 by 18: a plain plank crate like any, but a plank knocked out and a black box inside, its lamp in the gap. */
function doubleBottom(a) {
  const { PLANK, SOOT, band, plate, boards, grain, rv } = window.DebrisKit;
  const d = plate(a, [[0.5, 0.5], [21.5, 0.5], [21.5, 17.5], [0.5, 17.5]], PLANK, { seed: 61, l: 0.8 });
  boards(a, d, 1, 1.4, 20, 15.4, 3, PLANK, [0, 1, 0]);
  grain(a, 'M3 3.6L7.4 3.8', 'M14 4L18 3.8', 'M4 14.6L9 14.8', 'M15 13.8L19 14');
  a.flat(P([6.6, 6.7, 8, 6.4, 9.6, 7, 11, 6.5, 13, 6.9, 15.4, 6.6, 15.8, 11.4, 14, 11.8, 12, 11.3, 10, 11.9, 8, 11.4, 6.4, 11.8]), SOOT.deep);
  a.shape(P([7.4, 7.4, 15, 7.4, 15, 11.4, 7.4, 11.4]), CASE, { outer: false, lite: band(7.4, 7.4, 15, 8) });
  a.flat(band(7.4, 7.4, 8.2, 11.4), CASE.deep);
  lens(a, 11.4, 9.4, 0.95, false);
  rv(a, [13.8, 10.6]);
  a.shape(P([6.4, 6.6, 7.6, 7.2, 6.8, 8.6, 7.4, 10, 6.6, 11.8, 5.6, 11.6, 5.6, 6.8]), PLANK, { outer: false, shade: band(5.6, 10, 7.6, 12) });
  a.shape(P([15.6, 6.6, 16.6, 6.8, 16.6, 11.6, 15.6, 11.8, 16.2, 10, 15.2, 8.6]), PLANK, { outer: false, shade: band(15, 10, 16.6, 12) });
  corners(a, 22, 18, 3);
  rv(a, [1.6, 1.6], [20.4, 1.6], [1.6, 16.4], [20.4, 16.4]);
}

/** «Связка», drawn 24 by 18: bales in dark tarp, strapped in rust-orange, no crate; a beacon box clipped to a strap. */
function bundle(a) {
  const { OLD, band } = window.DebrisKit;
  a.shape(rr(6, 1, 11, 6.4, 2.6), TARP, { lite: band(6, 1, 17, 2.4), shade: 0.9, lines: ['M9 2.4L10 5', 'M14 2L13.4 4.6'] });
  a.shape(rr(0.8, 4.6, 11, 12.6, 3.2), TARP, { lite: P([0.8, 6, 6, 4.6, 8, 5.2, 2, 9]), shade: 1.2, lines: ['M3 8L5 12L4.6 15', 'M8 6.4L9.4 10'] });
  a.shape(rr(10.6, 3.6, 12.6, 13.6, 3.4), TARP, { lite: P([11, 6, 15, 3.6, 18, 4, 12, 9]), shade: 1.3, lines: ['M15.6 6L16.4 10.6L15.4 15', 'M20 7L21 11'] });
  a.shape(band(6, 1, 7.6, 17.4), STRAP, { outer: false, lite: band(6, 1, 6.5, 17.4), shade: band(7.1, 1, 7.6, 17.4) });
  a.shape(band(16.2, 1.4, 17.8, 17.4), STRAP, { outer: false, lite: band(16.2, 1, 16.7, 17.4), shade: band(17.3, 1, 17.8, 17.4) });
  a.shape(band(0.6, 9.4, 23.4, 10.9), STRAP, { outer: false, lite: band(0.6, 9.4, 23.4, 9.8), shade: band(0.6, 10.5, 23.4, 10.9) });
  a.shape(rr(5.6, 8.8, 2.4, 2.6, 0.4), OLD, { shade: 0.4 }); a.shape(rr(15.8, 8.8, 2.4, 2.6, 0.4), OLD, { shade: 0.4 });
  a.shape(rr(10.2, 8.2, 3.8, 3.6, 0.5), CASE, { lite: band(10.2, 8.2, 14, 8.8), shade: band(13.4, 8.2, 14, 11.8) });
  lens(a, 12.1, 10, 0.9, false);
  a.flat(P([1.6, 14.4, 3.4, 15.6, 2.6, 16.4]), TARP.deep); a.flat(P([21, 15, 22.6, 13.4, 22.8, 15.2]), TARP.deep);
}

/** «Опечатанная кассета», a 16 px icon: the case, its tape band and the red seal across it. */
function cassette(a) {
  const { OX, band } = window.DebrisKit;
  a.shape(rr(2, 5, 12, 7.6, 1), CASE, { lite: band(2, 5, 14, 6), shade: [band(2, 11.4, 14, 12.6), band(12.8, 5, 14, 12.6)] });
  a.shape(band(7, 5, 9.2, 12.6), TAPE, { outer: false, lite: band(7, 5, 7.6, 12.6), shade: band(8.6, 5, 9.2, 12.6) });
  a.shape(ell(8.1, 8.8, 1.9, 1.7), OX, { shade: 0.6, spec: ell(7.6, 8.2, 0.5) });
  a.line('M7.4 8.6L8.8 9.2', { w: 1, c: '#311616' });
  a.flat(band(3, 7, 6, 7.5), '#59606c'); a.flat(band(10.2, 10, 13, 10.5), '#59606c');
}

// ---------- frost on the hull: a tile a wall cell, by the sides of it facing space ----------
// The sheet: FROST_SLOTS tiles of 8 px in slots FROST_SLOT wide, 4 px clear round each; slot 2 × (open − 1) + parity, the parity (x + y) % 2
// of the cell, so the needles along any edge alternate. Only the rime's paint, deeper than it ever reaches and with no line: FrostView
// cuts its scalloped inner edge and inks it along the whole hull, so no cell shows a seam. The game reads it as FrostView does: keep both in step.
const FROST_SLOT = 16, FROST_CLEAR = 4, FROST_SLOTS = 30, FROST_PAINTED = 4;
const UP = 1, RIGHT = 2, DOWN = 4, LEFT = 8;
/** A point at s along side `side` of the cell, d in from its edge. */
const sideAt = (side, s, d) => (side === UP ? [s, d] : side === RIGHT ? [8 - d, s] : side === DOWN ? [s, 8 - d] : [d, s]);

function frostTile(a, ox, oy, open, parity) {
  const at = (side, s, d) => { const [x, y] = sideAt(side, s, d); return [ox + x, oy + y]; };
  const { band } = window.DebrisKit;
  for (const side of [UP, RIGHT, DOWN, LEFT]) {
    if (!(open & side)) continue;
    const ends = [-0.6, 8.6].map((s) => [at(side, s, -0.6), at(side, s, FROST_PAINTED)]);
    a.shape(P([...ends[0][0], ...ends[1][0], ...ends[1][1], ...ends[0][1]]), RIME,
      { outer: false, edge: false, shade: 0.7, lite: side === UP ? band(ox - 1, oy - 1, ox + 9, oy + 0.6) : undefined });
  }
  // Needles of rime from the edge in.
  for (const side of [UP, RIGHT, DOWN, LEFT]) {
    if (!(open & side)) continue;
    for (let s = parity ? 2.3 : 0.8; s < 8; s += 3.1) {
      const [x0, y0] = at(side, s, 0.4), [x1, y1] = at(side, s + 0.5, 1.8);
      a.line(path([x0, y0, x1, y1]), { c: '#f2f6f8', w: 1.6 });
    }
  }
}

function frostSheet(a) {
  for (let open = 1; open < 16; open++)
    for (let parity = 0; parity < 2; parity++) frostTile(a, (2 * (open - 1) + parity) * FROST_SLOT + FROST_CLEAR, FROST_CLEAR, open, parity);
}

/** A baked picture fitted whole into an item's 16 px icon, drawn at 4 times as every picture is, centred (deliveries.js's). */
function icon(picture) {
  const c = document.createElement('canvas'), s = 64 / Math.max(picture.width, picture.height), w = picture.width * s, h = picture.height * s;
  c.width = c.height = 64;
  const g = c.getContext('2d');
  g.imageSmoothingQuality = 'high';
  g.drawImage(picture, (64 - w) / 2, (64 - h) / 2, w, h);
  return c;
}

// Each look's pictures, one a variant, the piece's id picking one (DebrisLooks): as many as Content.Looks counts. Each variant draws in
// its own box, w by h, and is fitted into the wreck's 3 by 3 cells (Content.Wreckage), drawn smaller if larger, its pen as heavy as any.
const ICE = DRUSES.map((d) => ({ w: d.w, h: d.h, draw: druse(d) }));
// Their lamps where CrateLamps has them: each from its picture's middle, its box centred in the square.
const CRATES = [{ w: 24, h: 17, draw: hardCase }, { w: 18, h: 24, draw: drum }, { w: 22, h: 18, draw: doubleBottom }, { w: 24, h: 18, draw: bundle }];

/** A variant fitted into the wreck's 24 px square, centred. */
function fitted(v) {
  const u = Math.max(1, v.w / 24, v.h / 24), L = Anime.LINE, pen = { outer: L.outer * u, edge: L.edge * u, panel: L.panel * u };
  return bake({ w: 24, h: 24, units: u, W: pen, shift: [(24 * u - v.w) / 2, (24 * u - v.h) / 2], draw: v.draw });
}

Subjects.stretches = function () {
  for (const [look, item, variants] of [["iceblock", "iceBlock", ICE], ["contrabandcrate", "contrabandCrate", CRATES]]) {
    const pictures = variants.map(fitted);
    pictures.forEach((p, i) => out(`debris/${look}_${String(i + 1).padStart(2, "0")}`, p));
    // Its item's icon, never carried but in creative mode, as every wreck's: its first look.
    out(`items/${item}`, icon(pictures[0]));
  }
  out("items/contraband", bake({ w: 16, h: 16, draw: cassette }));
  out("frost", bake({ w: FROST_SLOTS * FROST_SLOT, h: FROST_SLOT, draw: frostSheet }));
};
})();

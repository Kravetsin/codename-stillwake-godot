// The seals of a hull, by the construction of wall_iron: glass panes in a steel frame, sheets of mica in a seam, hide stretched and laced on a wooden frame; their back walls and their edges.
(function () {
'use strict';
const { ell, poly, bake, canvas } = Anime;
const W = 128, H = 64;
const box = (x, y, w, h) => poly(x, y, x + w, y, x + w, y + h, x, y + h);

// ---- Glass: half see-through panes, solid frames (2026-10-29-clear-glass). The panes' alpha goes on after the finish, so the frame stays whole.
const PANE_ALPHA = 150 / 255;
const PANE = [
  { base: '#7d9c9f', shade: '#62828a', lite: '#b0cccb', spec: '#e4f0ee', ink: '#10202a' },
  { base: '#76969a', shade: '#5c7c85', lite: '#a8c4c4', spec: '#dcecea', ink: '#10202a' },
  { base: '#84a2a3', shade: '#688790', lite: '#b8d2d0', spec: '#e8f2f0', ink: '#10202a' },
];
const FRAME = { base: '#435c64', shade: '#2e434b', lite: '#7c9ca2', spec: '#b8ced0', ink: '#0f1c22' };
const BOLT = { base: '#6e868c', shade: '#34484f', lite: '#a4bcc0', spec: '#dfe9ea', ink: '#0f1c22' };

/** A band of the sheen across a pane, leaning as all glass here leans: starting at x along its foot, w wide. */
const sheen = (px, py, x, w, ph, lean) => poly(px + x, py + ph, px + x + w, py + ph, px + x + w + lean, py, px + x + lean, py);

// Each pane by hand: its tone, two bands of sheen (from, width), dirt along its foot (height at its left, its right), and its flaw.
const GLASS = [
  { c: 0, r: 0, t: 0, s: [[3, 5], [10, 1.4]], dirt: [1.2, .5] },
  { c: 1, r: 0, t: 1, s: [[14, 3]], dirt: [.5, 1.8], flaw: 'star', at: [24, 6] },
  { c: 2, r: 0, t: 2, s: [[1, 2], [6, 6], [14, 1]], dirt: [.8, .8] },
  { c: 3, r: 0, t: 0, s: [[17, 4]], dirt: [1.6, .4], flaw: 'scratch', at: [6, 4] },
  { c: 0, r: 1, t: 2, s: [[8, 2.5], [12, 1]], dirt: [.4, 1.4], flaw: 'chip', at: [0, 0] },
  { c: 1, r: 1, t: 0, s: [[2, 7]], dirt: [1, 1] },
  { c: 2, r: 1, t: 1, s: [[13, 2], [17, 5]], dirt: [1.9, .9], flaw: 'run', at: [7, 3] },
  { c: 3, r: 1, t: 2, s: [[5, 3]], dirt: [.5, .5], flaw: 'star', at: [8, 9] },
  { c: 0, r: 2, t: 1, s: [[16, 6]], dirt: [.9, 1.7], flaw: 'scratch', at: [3, 9] },
  { c: 1, r: 2, t: 2, s: [[4, 1.6], [7, 3.5]], dirt: [1.4, .4] },
  { c: 2, r: 2, t: 0, s: [[11, 5]], dirt: [.5, 1], flaw: 'chip', at: [1, 1] },
  { c: 3, r: 2, t: 1, s: [[1, 4], [18, 1.2]], dirt: [1.1, 1.5] },
  { c: 0, r: 3, t: 0, s: [[6, 2], [10, 5]], dirt: [1.7, .6] },
  { c: 1, r: 3, t: 1, s: [[19, 3]], dirt: [.6, 1.3], flaw: 'run', at: [20, 2] },
  { c: 2, r: 3, t: 2, s: [[2, 5.5]], dirt: [1, .4], flaw: 'scratch', at: [14, 3] },
  { c: 3, r: 3, t: 0, s: [[9, 1.6], [13, 4]], dirt: [.5, 1.9] },
];

function paneFlaw(a, g, x, y) {
  const [dx, dy] = g.at, cx = x + dx, cy = y + dy;
  if (g.flaw === 'star') {
    // Where something small hit it: short rays, none the same, never a full circle.
    for (const [ex, ey, mx, my] of [[-5, -3, -2, -1.6], [4, -3.5, 1.6, -1.2], [6, 1.5, 3, .3], [-3, 4.5, -1.2, 2.4], [1, -5, .7, -2.2]])
      a.line(`M${cx} ${cy}L${cx + mx} ${cy + my}L${cx + ex} ${cy + ey}`, { w: 1.8 });
    a.flat(ell(cx, cy, .55), '#1a2a30');
  } else if (g.flaw === 'scratch') {
    a.line(`M${cx} ${cy}L${cx + 7} ${cy + 1.6}L${cx + 13} ${cy + 1.2}`, { w: 1.4, c: '#d8e8e6' });
    a.line(`M${cx + 3} ${cy + 3}L${cx + 8} ${cy + 3.8}`, { w: 1.2, c: '#d8e8e6' });
  } else if (g.flaw === 'chip') {
    // A corner gone, mended with a dark smear of putty.
    a.flat(poly(x + .6 + dx, y + .6 + dy, x + 6 + dx, y + .6 + dy, x + .6 + dx, y + 4.4 + dy), '#2e434b');
    a.line(`M${x + 6 + dx} ${y + .8 + dy}L${x + 3.4 + dx} ${y + 2.4 + dy}L${x + .8 + dx} ${y + 4.4 + dy}`, { w: 2 });
  } else {
    // One long crack from a frame, as a sheet gives way under a flexing hull.
    a.line(`M${cx} ${y + .8}L${cx - 1.6} ${cy + 4}L${cx - .8} ${cy + 6}L${cx - 3.4} ${y + 13.2}`, { w: 2.2 });
    a.line(`M${cx - 1.6} ${cy + 4}L${cx + 2} ${cy + 5.4}`, { w: 1.6 });
  }
}

const glassPanes = (cols, rows, pw, ph) => { const out = []; for (let r = 0; r < rows; r++) for (let c = 0; c < cols; c++) out.push([c * pw, r * ph]); return out; };

/** The panes of a finished sheet thinned, so the stars show through. */
function thinPanes(c, cols, rows, pw, ph, bar) {
  const x = c.getContext('2d');
  x.save();
  x.globalCompositeOperation = 'destination-out';
  x.fillStyle = `rgba(0,0,0,${1 - PANE_ALPHA})`;
  for (const [px, py] of glassPanes(cols, rows, pw, ph)) x.fillRect((px + bar) * 4, (py + bar) * 4, (pw - bar * 2) * 4, (ph - bar * 2) * 4);
  x.restore();
  return c;
}

const wallGlass = {
  w: W, h: H, tile: true, K: 2,
  draw(a) {
    for (const g of GLASS) {
      const x = g.c * 32, y = g.r * 16, m = PANE[g.t];
      a.shape(box(x, y, 32, 16), m, {
        outer: false, edge: false,
        lite: g.s.map(([from, w]) => sheen(x, y, from, w, 16, 7)),
        shade: poly(x, y + 16 - g.dirt[0], x + 11, y + 16 - g.dirt[0] * .6, x + 22, y + 16 - g.dirt[1] * .7, x + 32, y + 16 - g.dirt[1], x + 32, y + 16, x, y + 16),
      });
      if (g.flaw) paneFlaw(a, g, x, y);
    }
    // The frame over all of it: bars lit on top and left, each pane's glazing a dark pen line.
    for (let r = 0; r < 4; r++) {
      const y = r * 16;
      a.shape(box(-2, y - .85, W + 4, 1.7), FRAME, { outer: false, edge: false, lite: box(-2, y - .85, W + 4, .55), shade: box(-2, y + .3, W + 4, .55) });
    }
    for (let c = 0; c < 4; c++) {
      const x = c * 32;
      a.shape(box(x - .85, -2, 1.7, H + 4), FRAME, { outer: false, edge: false, lite: box(x - .85, -2, .55, H + 4), shade: box(x + .3, -2, .55, H + 4) });
    }
    for (let r = 0; r < 4; r++) for (let c = 0; c < 4; c++) {
      const x = c * 32, y = r * 16;
      a.flat(box(x - 1.4, y - 1.4, 2.8, 2.8), FRAME.base);
      a.shape(ell(x, y, .75), BOLT, { outer: false, edge: false, shade: poly(x - .8, y + .1, x + .8, y + .1, x + .8, y + .8, x - .8, y + .8), spec: ell(x - .2, y - .25, .22) });
      a.line(`M${x + 2} ${y + 1.1}L${x + 30} ${y + 1.1}`, { w: 1.5, c: FRAME.ink });
      a.line(`M${x + 1.1} ${y + 2}L${x + 1.1} ${y + 14}`, { w: 1.3, c: FRAME.ink });
    }
  },
};

const BACK_PANE = [
  { base: '#303c44', shade: '#27323a', lite: '#3b4851', spec: '#3b4851', ink: '#0f161c' },
  { base: '#2c3840', shade: '#242e36', lite: '#37444d', spec: '#37444d', ink: '#0f161c' },
];
const backGlass = {
  w: W, h: H, tile: true, K: 2,
  draw(a) {
    // Four large sheets, quiet: one band of sheen on two of them, and nothing else.
    [[0, 0, 0, 8, 6], [64, 0, 1, 0, 0], [0, 32, 1, 0, 0], [64, 32, 0, 30, 4]].forEach(([x, y, t, from, w]) => {
      a.shape(box(x, y, 64, 32), BACK_PANE[t], { outer: false, edge: false, lite: w ? sheen(x, y, from, w, 32, 12) : undefined, shade: box(x, y + 30.6, 64, 1.4) });
    });
    for (const y of [0, 32]) a.flat(box(-2, y - .8, W + 4, 1.6), '#1a242b');
    for (const x of [0, 64]) a.flat(box(x - .8, -2, 1.6, H + 4), '#1a242b');
    for (const y of [0, 32]) for (const x of [0, 64]) a.flat(ell(x, y, .9), '#10181e');
  },
};

// ---- Mica: books of it split into sheets, each with its cleavage, some bronze in the layers, a peeling flake. By hand: a column holds sheets from the top down.
const MICA = [
  { base: '#b6b4a2', shade: '#8f8c78', lite: '#d6d4c2', spec: '#f4f2e6', ink: '#1a1610' },
  { base: '#aba692', shade: '#868069', lite: '#ccc8b4', spec: '#ece8d8', ink: '#1a1610' },
  { base: '#bfbdac', shade: '#97947f', lite: '#dedccc', spec: '#f8f6ec', ink: '#1a1610' },
];
const BRONZE = '#83653c', BRONZE_DEEP = '#5e482a';
// x, w, then sheets: top, height, tone, cleavage lines (rows into the sheet, from and to in shares of its width), bronze seams (row, thickness, from, to), its peel (a corner: 0 top-left, 1 top-right, 2 foot-left, 3 foot-right, size).
const MICA_COLS = [
  { x: 0, w: 32, sheets: [
    { y: 0, h: 21, t: 1, cleave: [[3, 0, .7], [6, .2, 1], [11, 0, .45], [15, .35, 1]], bronze: [[8, 1.1, 0, 1]], peel: [1, 4.5] },
    { y: 21, h: 17, t: 0, cleave: [[4, .1, 1], [9, 0, .8]], bronze: [] },
    { y: 38, h: 26, t: 2, cleave: [[2, 0, .55], [5, .25, 1], [14, 0, 1], [18, .5, 1], [22, 0, .3]], bronze: [[9, 2.6, 0, 1], [20, .8, .3, .95]], peel: [2, 3.5] },
  ] },
  { x: 32, w: 32, sheets: [
    { y: 0, h: 13, t: 0, cleave: [[3, 0, .9], [8, .3, 1]], bronze: [[1.4, .8, .1, .8]] },
    { y: 13, h: 25, t: 2, cleave: [[3, .2, 1], [7, 0, .6], [13, .1, 1], [20, 0, .75]], bronze: [[15, 1, .15, 1]], peel: [0, 5] },
    { y: 38, h: 12, t: 1, cleave: [[4, 0, 1]], bronze: [[6.5, 2.2, 0, 1]] },
    { y: 50, h: 14, t: 0, cleave: [[3, .3, 1], [8, 0, .7]], bronze: [], peel: [3, 3] },
  ] },
  { x: 64, w: 13, sheets: [
    { y: 0, h: 30, t: 2, cleave: [[5, 0, 1], [11, .2, 1], [19, 0, .7], [25, .3, 1]], bronze: [[15, .9, 0, .85]] },
    { y: 30, h: 34, t: 1, cleave: [[6, 0, 1], [13, .2, 1], [23, 0, .8]], bronze: [[28, 1.4, 0, 1]], peel: [1, 2.6] },
  ] },
  { x: 77, w: 19, sheets: [
    { y: 0, h: 18, t: 0, cleave: [[4, 0, .8], [9, .2, 1], [14, 0, 1]], bronze: [[11.4, 1.3, 0, 1]], peel: [0, 3] },
    { y: 18, h: 28, t: 2, cleave: [[6, .1, 1], [12, 0, .6], [22, .3, 1]], bronze: [[18, .8, .2, 1]] },
    { y: 46, h: 18, t: 1, cleave: [[5, 0, 1], [12, 0, .7]], bronze: [[8, 1, 0, .6]] },
  ] },
  { x: 96, w: 18, sheets: [
    { y: 0, h: 26, t: 1, cleave: [[6, 0, 1], [11, .3, 1], [20, 0, .6]], bronze: [[14, 1.8, 0, 1]], peel: [3, 3.2] },
    { y: 26, h: 22, t: 2, cleave: [[3, 0, .7], [8, .2, 1], [16, 0, 1]], bronze: [] },
    { y: 48, h: 16, t: 0, cleave: [[4, .1, 1], [10, 0, .8]], bronze: [[7, .9, .2, 1]] },
  ] },
  { x: 114, w: 14, sheets: [
    { y: 0, h: 15, t: 2, cleave: [[4, 0, 1], [10, .3, 1]], bronze: [] },
    { y: 15, h: 31, t: 0, cleave: [[5, .1, 1], [12, 0, .7], [19, .2, 1], [26, 0, 1]], bronze: [[9, 1.2, 0, 1]], peel: [1, 2.8] },
    { y: 46, h: 18, t: 1, cleave: [[6, 0, .8], [12, .2, 1]], bronze: [[3, .8, .2, .9]] },
  ] },
];

/** A seam of bronze through a sheet: thin at both ends, as a layer of other mineral lies in a book of it. */
const seamOf = (x, w, y, th, from, to) => {
  const x0 = x + w * from, x1 = x + w * to, m = (x0 + x1) / 2;
  return poly(x0, y + th * .35, x0 + (m - x0) * .3, y, m, y - th * .06, x1 - (x1 - m) * .3, y + th * .1, x1, y + th * .5, x1 - (x1 - m) * .25, y + th, m, y + th * 1.04, x0 + (m - x0) * .3, y + th * .88);
};

/** A corner of a sheet lifting: its underside dark, the flake pale and hard-edged. */
function peelOf(a, s, c, corner, size) {
  const left = corner === 0 || corner === 2, top = corner < 2;
  const cx = left ? c.x : c.x + c.w, cy = top ? s.y : s.y + s.h, dx = left ? 1 : -1, dy = top ? 1 : -1;
  a.flat(poly(cx + dx * .2, cy + dy * .2, cx + dx * size * 1.25, cy + dy * .2, cx + dx * .2, cy + dy * size), '#4a4434');
  a.flat(poly(cx + dx * size * 1.25, cy + dy * .2, cx + dx * size * .7, cy + dy * size * .78, cx + dx * .2, cy + dy * size), '#e0dcc8');
  a.line(`M${cx + dx * size * 1.25} ${cy + dy * .2}L${cx + dx * size * .7} ${cy + dy * size * .78}L${cx + dx * .2} ${cy + dy * size}`, { w: 1.8 });
}

const wallMica = {
  w: W, h: H, tile: true, K: 2,
  draw(a) {
    for (const c of MICA_COLS) for (const s of c.sheets) {
      const m = MICA[s.t];
      // Its shine, hard-edged and leaning, broken by the leaves it crosses: what tells a sheet of mica from a slab of stone.
      const lean = s.h * .55, g0 = c.x + c.w * (.08 + (s.y % 7) * .09), g1 = g0 + c.w * (.18 + (s.h % 5) * .05);
      a.shape(box(c.x, s.y, c.w, s.h), m, {
        outer: false, edge: false,
        lite: [box(c.x, s.y, c.w, .7), poly(g0, s.y + s.h, g1, s.y + s.h, g1 + lean, s.y, g0 + lean, s.y)],
        shade: [box(c.x, s.y + s.h - 1, c.w, 1), box(c.x + c.w - .7, s.y, .7, s.h)],
      });
      for (const [row, th, from, to] of s.bronze) {
        a.flat(seamOf(c.x, c.w, s.y + row, th * .7, from, to), BRONZE);
        if (th > 1.2) a.flat(seamOf(c.x, c.w, s.y + row + th * .2, th * .22, from + .1, to - .15), BRONZE_DEEP);
      }
      // Leaves: a hair of shadow over a hair of light across the sheet, ending where a leaf above it ends; one more below each, whole, as a book splits.
      for (const [row, from, to] of s.cleave) {
        const hair = (x0, x1, y) => { a.flat(box(x0, y, x1 - x0, .24), m.shade); a.flat(box(x0, y + .24, x1 - x0, .22), m.lite); };
        hair(c.x + c.w * from, c.x + c.w * to, s.y + row);
        if (row + 1.3 < s.h - 1) hair(c.x, c.x + c.w, s.y + row + 1.3);
      }
    }
    // The dark gaps between sheets and books, straight, so it tiles.
    for (const c of MICA_COLS) {
      a.flat(box(c.x - .3, -2, .6, H + 4), '#1a1610');
      for (const s of c.sheets) a.flat(box(c.x, s.y - .22, c.w, .44), '#1a1610');
    }
    for (const c of MICA_COLS) for (const s of c.sheets) if (s.peel) peelOf(a, s, c, s.peel[0], s.peel[1]);
  },
};

const BACK_MICA = [
  { base: '#3c3a36', shade: '#32302c', lite: '#46443f', spec: '#46443f', ink: '#121110' },
  { base: '#383632', shade: '#2e2c29', lite: '#42403b', spec: '#42403b', ink: '#121110' },
  { base: '#403e39', shade: '#35332f', lite: '#4a4843', spec: '#4a4843', ink: '#121110' },
];
const BACK_MICA_COLS = [
  { x: 0, w: 64, sheets: [[0, 28, 0, [7, 19], [13, 1.4]], [28, 36, 1, [9, 24], [20, 1.8]]] },
  { x: 64, w: 22, sheets: [[0, 40, 2, [13, 27], null], [40, 24, 0, [10], [17, 1]]] },
  { x: 86, w: 42, sheets: [[0, 20, 1, [8, 14], [4, 1]], [20, 44, 2, [11, 28, 36], [18, 1.8]]] },
];
const backMica = {
  w: W, h: H, tile: true, K: 2,
  draw(a) {
    for (const c of BACK_MICA_COLS) for (const [y, h, t, cleave, seam] of c.sheets) {
      a.shape(box(c.x, y, c.w, h), BACK_MICA[t], { outer: false, edge: false, shade: box(c.x, y + h - 1, c.w, 1), lite: box(c.x, y, c.w, .6) });
      if (seam) a.flat(seamOf(c.x, c.w, y + seam[0], seam[1] * .6, 0, 1), '#2b2620');
      for (const row of cleave) a.flat(box(c.x + (row % 3) * 3, y + row, c.w - (row % 5) * 3, .3), '#2d2b27');
    }
    for (const c of BACK_MICA_COLS) {
      a.flat(box(c.x - .25, -2, .5, H + 4), '#121110');
      for (const [y] of c.sheets) a.flat(box(c.x, y - .2, c.w, .4), '#121110');
    }
  },
};

// ---- Hide: a skin to a frame, its points pulled out by lacing, its edge scalloped between them. Each skin by hand, none square.
const SKIN = [
  { base: '#a68872', shade: '#876a58', deep: '#694f42', lite: '#bfa28a', spec: '#d8c0a8', ink: '#1c1210' },
  { base: '#9e816c', shade: '#806452', deep: '#634a3e', lite: '#b79a84', spec: '#d0b8a2', ink: '#1c1210' },
  { base: '#ad8f78', shade: '#8e705d', deep: '#6f5546', lite: '#c6a990', spec: '#dec6ae', ink: '#1c1210' },
];
const WOOD = { base: '#4c3729', shade: '#33241b', lite: '#684c36', spec: '#7c5e44', ink: '#150d09' };

// Points of the skin in its 32 × 32 panel, clockwise from the top left; its pale middle; its fold lines; its mark.
const HIDES = [
  { c: 0, r: 0, t: 0,
    pts: [[3, 3.2], [9, 2.4], [16, 3.4], [23, 2.5], [29, 3.2], [29.8, 10], [28.8, 17], [29.8, 24], [28.8, 29], [22, 29.8], [15, 28.8], [8, 29.7], [3, 28.8], [2.4, 22], [3.4, 15], [2.5, 9]],
    pale: [[10, 9], [19, 7.6], [24, 12], [22.6, 21], [15, 24], [8.6, 19.6]],
    folds: ['M5 5L9 9', 'M27 5L23 8.6', 'M27 27L22.6 22.4', 'M5.4 26.6L9 22', 'M16 4.4L15.6 8', 'M4.4 15L8 15.4'] },
  { c: 1, r: 0, t: 1,
    pts: [[3.6, 2.6], [11, 3.4], [20, 2.4], [28.6, 3.2], [29.6, 12], [28.8, 21], [29.4, 29.2], [20, 28.8], [12, 29.8], [3.4, 29], [2.6, 20], [3.4, 11]],
    pale: [[9, 10.6], [17, 8], [23.6, 11], [24.4, 19.6], [18, 23.6], [10.4, 22]],
    folds: ['M6 5.6L10 9.4', 'M26.4 6L22.8 9.6', 'M26.6 26.6L23 21.6', 'M6 26.4L10 22.6', 'M29 16L25 16.4'],
    mark: 'scar', at: [8, 14] },
  { c: 2, r: 0, t: 2,
    pts: [[2.8, 3.4], [8, 2.5], [14, 3.4], [20, 2.4], [26, 3.2], [29.6, 2.8], [28.8, 9], [29.8, 16], [28.8, 23], [29.4, 29.4], [23, 28.8], [16, 29.8], [9, 28.8], [2.6, 29.4], [3.4, 22], [2.4, 14], [3.4, 8]],
    pale: [[8, 7.4], [22, 8.4], [25, 15], [20, 24], [11, 24.6], [7, 16]],
    folds: ['M4.6 5L8.4 8', 'M27.6 5L23.4 8.6', 'M27.4 27.4L21.4 23.8', 'M4.8 27L10 24'] },
  { c: 3, r: 0, t: 0,
    pts: [[3.2, 3], [12, 2.4], [22, 3.4], [29.2, 2.6], [28.8, 11], [29.8, 20], [28.8, 29], [18, 29.8], [9, 28.8], [2.8, 29.2], [2.4, 19], [3.4, 10]],
    pale: [[11, 8.6], [21, 10.4], [23.4, 18], [16, 24.4], [9, 19]],
    folds: ['M5.6 5.4L10 9', 'M27 5L22 9.6', 'M26.6 26.6L21.4 21.6', 'M6 26L10 21'],
    mark: 'hole', at: [20, 19] },
  { c: 0, r: 1, t: 2,
    pts: [[2.6, 2.8], [10, 3.4], [17, 2.4], [24, 3.4], [29.4, 2.6], [28.8, 8], [29.8, 15], [28.8, 22], [29.6, 29.4], [21, 28.8], [11, 29.8], [3.2, 29], [2.4, 19], [3.4, 10]],
    pale: [[8.6, 8.4], [18, 7.6], [24, 13.4], [23, 22], [13, 24], [8, 17]],
    folds: ['M5 5L8.6 8', 'M27.4 4.8L23.8 8.6', 'M27 27L22.8 22', 'M5.6 26.8L9.4 21.8'],
    mark: 'bruise', at: [10, 20] },
  { c: 1, r: 1, t: 0,
    pts: [[3.4, 3.4], [9, 2.4], [15, 3.4], [22, 2.5], [28.8, 3.4], [29.8, 12], [28.8, 20], [29.6, 28.8], [20, 29.8], [13, 28.8], [6, 29.8], [2.6, 28.8], [3.4, 20], [2.4, 11]],
    pale: [[10, 8], [20, 8.8], [24.6, 16], [19, 24], [10, 23], [7.4, 15]],
    folds: ['M5.6 5.4L10 8.6', 'M26.6 5.4L21.6 9.4', 'M27 26.8L22 21.8', 'M4.8 26.8L9.6 22.4', 'M15 4.4L15 7.6'] },
  { c: 2, r: 1, t: 1,
    pts: [[3, 2.6], [14, 3.4], [23, 2.4], [29.4, 3.2], [28.8, 14], [29.8, 23], [29, 29.4], [17, 28.8], [8, 29.8], [2.6, 29.2], [3.4, 19], [2.4, 11]],
    pale: [[10, 10], [20, 8], [24, 15], [21, 23], [11, 22]],
    folds: ['M5 4.6L10 9.4', 'M27.4 5L23 8.6', 'M27 27.4L21.8 23', 'M4.8 27L10.4 22'],
    mark: 'patch', at: [17, 6] },
  { c: 3, r: 1, t: 2,
    pts: [[3.4, 3.2], [8, 2.4], [17, 3.4], [25, 2.5], [29, 3.2], [29.8, 9], [28.8, 18], [29.8, 26], [28.8, 29.6], [19, 28.8], [10, 29.8], [3.2, 28.8], [2.4, 21], [3.4, 13], [2.5, 7]],
    pale: [[9, 8.4], [20, 7.4], [24.6, 13], [24, 22], [16, 24.4], [8.4, 20]],
    folds: ['M5 5.4L9 8.4', 'M27.4 5L24 8', 'M27.4 27.6L23.6 22', 'M5 27L9 20.6', 'M28.6 18L25 17.6'] },
];

/** A skin's outline: pulled to its points, sagging in between them. */
function scallop(pts, x, y, cx, cy, sag) {
  let d = `M${x + pts[0][0]} ${y + pts[0][1]}`;
  for (let i = 0; i < pts.length; i++) {
    const p = pts[i], q = pts[(i + 1) % pts.length], mx = (p[0] + q[0]) / 2, my = (p[1] + q[1]) / 2;
    d += `Q${x + mx + (cx - mx) * sag} ${y + my + (cy - my) * sag} ${x + q[0]} ${y + q[1]}`;
  }
  return d + 'Z';
}

function hideMark(a, h, x, y) {
  const [dx, dy] = h.at, cx = x + dx, cy = y + dy;
  if (h.mark === 'scar') {
    // An old tear sewn up: the seam and its stitches.
    a.line(`M${cx} ${cy}L${cx + 5} ${cy + 1.4}L${cx + 11} ${cy + .8}L${cx + 15} ${cy + 2.4}`, { w: 2 });
    for (const [sx, sy] of [[2, .6], [5, 1.4], [8, 1.1], [11, .8], [13.5, 1.8]]) a.line(`M${cx + sx - .2} ${cy + sy - 1.1}L${cx + sx + .2} ${cy + sy + 1.1}`, { w: 1.5 });
  } else if (h.mark === 'hole') {
    // A hole the size of a shot, worn at its lip.
    a.flat(poly(cx - 1.5, cy - .5, cx - .4, cy - 1.5, cx + 1.3, cy - 1, cx + 1.6, cy + .6, cx + .2, cy + 1.5, cx - 1.2, cy + 1.1), '#1c1410');
    a.line(`M${cx + 1.6} ${cy + .6}L${cx + 3.6} ${cy + 1.6}`, { w: 1.5 });
  } else if (h.mark === 'bruise') {
    a.flat(poly(cx - 3, cy - 1.4, cx + .6, cy - 2.2, cx + 3.2, cy - .4, cx + 2.2, cy + 2, cx - 1, cy + 2.4, cx - 3.6, cy + .8), '#8a6c58');
  } else {
    // A square of other skin sewn on over a split.
    a.shape(poly(cx - 3, cy, cx + 3.4, cy - .4, cx + 3.8, cy + 5, cx - 2.8, cy + 5.4), SKIN[1], { outer: false, shade: poly(cx - 2.8, cy + 4.2, cx + 3.8, cy + 3.8, cx + 3.8, cy + 5, cx - 2.8, cy + 5.4) });
    for (const [sx, sy] of [[-3, 1.6], [-2.9, 3.4], [3.6, 1.4], [3.7, 3.2], [-1, -.2], [1.6, -.3]]) a.flat(ell(cx + sx, cy + sy, .28), '#2a1a12');
  }
}

/** A cord from a skin's point to the bar it is tied to, and its knot on the bar. */
function lace(a, x, y, p, panel) {
  const [px, py] = p, toL = px, toR = panel - px, toT = py, toB = panel - py, least = Math.min(toL, toR, toT, toB);
  const ex = least === toL ? 0 : least === toR ? panel : px, ey = least === toT ? 0 : least === toB ? panel : py;
  a.line(`M${x + px} ${y + py}L${x + ex} ${y + ey}`, { w: 2.4, c: '#d0b89a' });
  a.flat(ell(x + ex, y + ey, .55), '#2a1a12');
}

const wallHide = {
  w: W, h: H, tile: true, K: 2,
  draw(a) {
    a.flat(box(-2, -2, W + 4, H + 4), '#2a1e17');
    for (const h of HIDES) {
      const x = h.c * 32, y = h.r * 32, m = SKIN[h.t], d = scallop(h.pts, x, y, 16, 16, .07);
      for (const p of h.pts) lace(a, x, y, p, 32);
      a.shape(d, m, {
        outer: false, edge: false,
        deep: 1.1,
        lite: poly(h.pale.flatMap(([px, py]) => [x + px, y + py])),
      });
      a.line(d, { w: 1.8, c: m.ink });
      for (const f of h.folds) {
        const [, fx0, fy0, fx1, fy1] = f.match(/M(\S+) (\S+)L(\S+) (\S+)/);
        a.line(`M${x + +fx0} ${y + +fy0}L${x + +fx1} ${y + +fy1}`, { w: 1.5, c: m.deep });
      }
      if (h.mark) hideMark(a, h, x, y);
    }
    // The frame: bars of worn wood, lashed where they cross.
    for (const y of [0, 32]) a.shape(box(-2, y - 1.1, W + 4, 2.2), WOOD, { outer: false, edge: false, lite: box(-2, y - 1.1, W + 4, .6), shade: box(-2, y + .4, W + 4, .7) });
    for (const x of [0, 32, 64, 96]) a.shape(box(x - 1.1, -2, 2.2, H + 4), WOOD, { outer: false, edge: false, lite: box(x - 1.1, -2, .6, H + 4), shade: box(x + .4, -2, .7, H + 4) });
    for (const y of [0, 32]) for (const x of [0, 32, 64, 96]) {
      a.flat(poly(x - 1.8, y - 1.5, x + 1.8, y - 1.8, x + 1.9, y + 1.6, x - 1.7, y + 1.8), '#7a6448');
      for (const k of [-1, 0, 1]) a.line(`M${x - 1.8} ${y + k * .9 - .2}L${x + 1.9} ${y + k * .9 + .2}`, { w: 1.3, c: '#2a1c12' });
    }
    // Grain along a few of the bars, none repeating.
    for (const [x0, y0, x1, y1] of [[6, .1, 20, -.1], [40, 32.1, 58, 31.9], [72, -.1, 88, .1], [104, 32, 120, 32.1]]) a.line(`M${x0} ${y0}L${x1} ${y1}`, { w: 1.2, c: WOOD.ink });
    for (const [x0, y0, x1, y1] of [[32, 6, 32.1, 18], [64, 38, 63.9, 56], [96, 8, 96, 24], [0, 40, .1, 52]]) a.line(`M${x0} ${y0}L${x1} ${y1}`, { w: 1.2, c: WOOD.ink });
  },
};

const BACK_SKIN = [
  { base: '#3a322e', shade: '#2f2825', deep: '#272120', lite: '#433a35', spec: '#433a35', ink: '#141010' },
  { base: '#37302c', shade: '#2d2623', deep: '#251f1e', lite: '#403733', spec: '#403733', ink: '#141010' },
];
// Four large skins behind: their points only, and one's old scar.
const BACK_HIDES = [
  { x: 0, y: 0, t: 0, pts: [[3, 2.6], [20, 3.4], [40, 2.4], [61, 3], [62, 16], [61, 29.4], [40, 28.6], [20, 29.6], [2.8, 29], [2, 15]] },
  { x: 64, y: 0, t: 1, pts: [[2.6, 3.2], [24, 2.4], [44, 3.4], [61.4, 2.6], [60.8, 18], [61.6, 29], [36, 28.6], [14, 29.6], [3, 28.8], [2.2, 12]], scar: 'M84 12L92 14L100 13L106 16' },
  { x: 0, y: 32, t: 1, pts: [[3.2, 2.6], [15, 3.4], [33, 2.4], [50, 3.4], [61.4, 2.8], [62, 14], [60.8, 29.4], [30, 28.6], [2.6, 29.2], [2, 18]] },
  { x: 64, y: 32, t: 0, pts: [[2.8, 3], [30, 2.4], [61, 3.2], [61.8, 16], [61, 29], [44, 29.6], [24, 28.6], [3.2, 29.4], [2.2, 20], [3, 9]] },
];
const backHide = {
  w: W, h: H, tile: true, K: 2,
  draw(a) {
    a.flat(box(-2, -2, W + 4, H + 4), '#16100d');
    for (const h of BACK_HIDES) {
      const d = scallop(h.pts, h.x, h.y, 32, 16, .05);
      a.shape(d, BACK_SKIN[h.t], { outer: false, edge: false, shade: .9 });
      for (const [px, py] of h.pts) {
        const ex = px < 4 ? 0 : px > 60 ? 64 : px, ey = py < 4 ? 0 : py > 28 ? 32 : py;
        a.flat(poly(h.x + px - .18, h.y + py, h.x + px + .18, h.y + py, h.x + ex + .18, h.y + ey, h.x + ex - .18, h.y + ey), '#4e4238');
      }
      if (h.scar) a.line(h.scar, { w: 1.3, c: '#241d1b' });
    }
    for (const y of [0, 32]) a.flat(box(-2, y - .9, W + 4, 1.8), '#241a14');
    for (const x of [0, 64]) a.flat(box(x - .9, -2, 1.8, H + 4), '#241a14');
  },
};

// ---- Edges: a wall's ink and rim (AnimeEdge), then what its material does to the rim.
const PX = 32, I = 3, RIM = 3;
/** Rim pieces in the edge's own pixels, painted over only what is already drawn: along a side (from, to), on each open side. */
function brokenRim(c, open, pieces, colour) {
  const x = c.getContext('2d');
  x.save();
  x.globalCompositeOperation = 'source-atop';
  x.fillStyle = colour;
  for (const [a0, a1] of pieces) {
    if (open & 1) x.fillRect(a0, I, a1 - a0, RIM);
    if (open & 4) x.fillRect(a0, PX - I - RIM, a1 - a0, RIM);
    if (open & 8) x.fillRect(I, a0, RIM, a1 - a0);
    if (open & 2) x.fillRect(PX - I - RIM, a0, RIM, a1 - a0);
  }
  x.restore();
  return c;
}

Subjects.shipwalls = function () {
  out('wall_glass', thinPanes(bake(wallGlass), 4, 4, 32, 16, .9));
  out('back_glass', thinPanes(bake(backGlass), 2, 2, 64, 32, .85));
  out('wall_mica', bake(wallMica));
  out('back_mica', bake(backMica));
  out('wall_hide', bake(wallHide));
  out('back_hide', bake(backHide));
  for (let open = 1; open < 16; open++) {
    out(`wall-edge_glass_${open}`, AnimeEdge(open, { lite: '#b4cecd', shade: '#55727a', ink: '#1a2c33' }));
    // Mica's rim flakes: chips of pale sheet, and a gap of dull stone; hide's is its frame, tied at intervals.
    const mica = AnimeEdge(open, { lite: '#b4b09a', shade: '#67634f', ink: '#25221b' });
    brokenRim(mica, open, [[11, 14], [23, 25]], '#e8e4d0');
    brokenRim(mica, open, [[3, 6], [17, 20], [27, 29]], '#7e7a66');
    out(`wall-edge_mica_${open}`, mica);
    const hide = AnimeEdge(open, { lite: '#826a58', shade: '#3e2e24', ink: '#1a120e' });
    brokenRim(hide, open, [[0, 2], [9, 12], [20, 23], [30, 32]], '#1a120e');
    out(`wall-edge_hide_${open}`, hide);
  }
};
})();

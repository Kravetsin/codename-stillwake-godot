// What the ship is fitted with: pipes, valves, sockets, hatches, launch decks, floor and ladder; the drone; the hero's tools.
(function () {
'use strict';
const { ell, rr, poly, bake, canvas } = Anime;
const { STEEL, GUN, DARK, COPPER, PIPE, band, rivets } = window.AnimeKit;

/** The pipe's run, a sheet repeated along a span: riveted seam, a rust patch at a rivet, a collar every sheet. It is 4 px thick, rows 2 to 6 of 8. */
const pipeRun = (a) => {
  const w = 24, y0 = 2, y1 = 6, clip = band(0, y0, w, y1);
  a.flat(clip, PIPE.base);
  a.flat(band(0, y0, w, y0 + 1), PIPE.lite);
  a.flat(band(0, y1 - 1.4, w, y1), PIPE.shade);
  a.flat(band(0, y1 - .5, w, y1), PIPE.deep);
  a.flat(poly(15.1, 4.5, 17.2, 4.3, 17.7, 5, 16.8, 5.95, 16.2, 5.5, 15.4, 5.9, 15.2, 5.2), '#6e5646', { clip });
  a.flat(poly(15.8, 4.7, 16.8, 4.6, 16.9, 5.3, 16, 5.4), '#4f3d36', { clip });
  for (const y of [y0, y1]) a.line(`M-1 ${y}L${w} ${y}`, { w: 3 });
  a.line(`M-1 4.7L${w} 4.7`, { w: 1.1 });
  rivets(a, [3.5, 4], [8, 4], [16.2, 4], [20.5, 4]);
  a.shape(rr(10.6, y0 - .6, 2.8, 5.2, .3), GUN, { lite: band(10.6, y0 - .6, 13.4, y0 + .2), shade: band(10.6, y1 - .5, 13.4, y1 + .6), lines: ['M12 1.6L12 6.4'] });
};
/** A joint's bolted flange, its middle the cell's. */
const flange = (a) => {
  a.shape(ell(4, 4, 3.7), GUN, { shade: .6, lite: ell(3.5, 3.4, 2.6) });
  a.flat(ell(4, 4, 2), PIPE.shade);
  a.flat(ell(3.8, 3.7, 1.2), PIPE.base);
  for (let i = 0; i < 6; i++) rivets(a, [4 + Math.cos(.26 + i * Math.PI / 3) * 2.85, 4 + Math.sin(.26 + i * Math.PI / 3) * 2.85]);
};
/** A pipe by its arms (up 1, right 2, down 4, left 8), one cell, what a build card lays together: the run's body, the flange where a run turns or ends. */
function pipe(open) {
  return bake({ w: 8, h: 8, draw: (a) => {
    const arm = (x0, y0, x1, y1, lit, dark) => { a.flat(band(x0, y0, x1, y1), PIPE.base); a.flat(lit, PIPE.lite); a.flat(dark, PIPE.shade); };
    if (open & 8) arm(0, 2, 4, 6, band(0, 2, 4, 3), band(0, 4.6, 4, 6));
    if (open & 2) arm(4, 2, 8, 6, band(4, 2, 8, 3), band(4, 4.6, 8, 6));
    if (open & 1) arm(2, 0, 6, 4, band(2, 0, 3, 4), band(4.6, 0, 6, 4));
    if (open & 4) arm(2, 4, 6, 8, band(2, 4, 3, 8), band(4.6, 4, 6, 8));
    if (open & 10) for (const y of [2, 6]) a.line(`M${open & 8 ? -1 : 4} ${y}L${open & 2 ? 9 : 4} ${y}`, { w: 3 });
    if (open & 5) for (const x of [2, 6]) a.line(`M${x} ${open & 1 ? -1 : 4}L${x} ${open & 4 ? 9 : 4}`, { w: 3 });
    if (open !== 5 && open !== 10) flange(a);
  } });
}
const valve = (open) => (a) => {
  const m = open ? { base: '#3fae5c', shade: '#256a3c', deep: '#1a4c2c', lite: '#86e08c', spec: '#e0ffe0' } : { base: '#d0483c', shade: '#8a2c30', deep: '#5c1c22', lite: '#f08c74', spec: '#ffe0d0' };
  const turn = open ? .4 : .4 + Math.PI / 4, corners = [];
  for (let i = 0; i < 8; i++) corners.push(5 + Math.cos(Math.PI / 8 + i * Math.PI / 4) * 4.7, 5 + Math.sin(Math.PI / 8 + i * Math.PI / 4) * 4.7);
  a.shape(poly(corners), GUN, { shade: .6, lite: ell(4.4, 4.2, 3) });
  a.shape(ell(5, 5, 3.8), m, { shade: .6, lite: ell(4.6, 4.5, 2.7) });
  a.flat(ell(5, 5, 2.4), '#14151c');
  for (let i = 0; i < 3; i++) {
    const t = turn + (i / 3) * Math.PI * 2, c = Math.cos(t), s = Math.sin(t), w = .5, R = 2.8;
    a.flat(poly(5 - s * w, 5 + c * w, 5 + c * R - s * w, 5 + s * R + c * w, 5 + c * R + s * w, 5 + s * R - c * w, 5 + s * w, 5 - c * w), m.base);
  }
  a.flat(ell(5, 5, 1.1), m.lite);
  a.flat(ell(4.75, 4.7, .45), m.spec);
};
const socket = (a) => { a.shape(ell(4, 4, 3.2), DARK, { outer: true, lite: .0001 }); a.flat(ell(4, 4, 1.6), '#12131e'); a.flat(band(2.6, 3.6, 5.4, 4.4), '#4f9cbc'); };
const powerSocket = (a) => { a.shape(ell(2.5, 2.5, 2), { base: '#868fa2', shade: '#474c68', lite: '#c4ccd8', spec: '#ffffff' }, { shade: .4 }); a.flat(ell(2.5, 2.5, 1), '#12131e'); };

/** A hatch segment by what it joins (right 2, left 8): its frame, the same open and shut; the plate between, only shut, so FineSlid finds exactly it. */
function hatch(links, shut) {
  const left = links & 8 ? 0 : 1, right = links & 2 ? 16 : 15;
  const frame = bake({ w: 16, h: 16, draw: (a) => {
    a.shape(band(0, 0, 16, 1), DARK, { outer: false, lite: band(0, .2, 16, .5) });
    a.shape(band(0, 7, 16, 8), DARK, { outer: false, lite: band(0, 7.2, 16, 7.5) });
    if (!(links & 8)) a.shape(band(0, 0, 1, 8), DARK, { outer: false, lite: band(.2, .2, .5, 7.8) });
    if (!(links & 2)) a.shape(band(15, 0, 16, 8), DARK, { outer: false });
  } });
  const c = canvas(64, 64), x = c.getContext('2d');
  if (shut) x.drawImage(bake({ w: 16, h: 16, draw: (a) => {
    a.shape(band(left, 1, right, 7), STEEL, { outer: false, lite: band(left, 1, right, 1.5), shade: band(left, 5.4, right, 7), lines: [`M${left} 5.4L${right} 5.4`] });
    // Hazard across its leading edge: a plate that shuts on someone says so; the only bright thing on it.
    for (const px of [2, 6, 10, 14]) a.flat(poly(px, 1.7, px + 1.8, 1.7, px + 1.1, 3, px - .7, 3), '#c8a030', { clip: band(left, 1, right, 7) });
    if (!(links & 8)) { a.flat(band(3, 4, 7, 4.9), '#1a1c28'); a.flat(band(3, 4, 7, 4.2), '#0f111b'); }
    if (!(links & 2)) { a.shape(band(10.6, 3.6, 13.8, 5.6), { base: '#c8a030', shade: '#8e7028', deep: '#685428', lite: '#e4c45c', spec: '#f4e49c', ink: '#1e1808' }, { outer: false, lite: band(10.6, 3.6, 13.8, 4), shade: band(10.6, 5, 13.8, 5.6) }); }
  } }), 0, 0);
  x.drawImage(frame, 0, 0);
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

// The tools in the hand.
const WOODH = { base: '#8a6242', shade: '#5c3e30', lite: '#ad8258', spec: '#cca678', ink: '#1c1210' };
const RED = { base: '#c8402c', shade: '#84282c', deep: '#5c1c28', lite: '#ec7458', spec: '#ffd8c8', ink: '#280c10' };
// The fire axe, the user's pick of five drawn references («Пожарный, тяжёлая головка»): its materials, by the reference's colours.
const AXE = {
  wood: {'base': '#a86e42', 'shade': '#7a4a2c', 'deep': '#573420', 'lite': '#c48a58', 'spec': '#d8a878', 'ink': '#1c1210'},
  dark: {'base': '#383943', 'shade': '#26262e', 'deep': '#18181e', 'lite': '#52535e', 'spec': '#7a7c88'},
  red: {'base': '#a2302a', 'shade': '#76221e', 'deep': '#521816', 'lite': '#be4a3a', 'spec': '#e08a74', 'ink': '#280c10'},
  steel: {'base': '#a8adb6', 'shade': '#6c7486', 'deep': '#4a5062', 'lite': '#d0d5dc', 'spec': '#eef0f2', 'ink': '#14162a'},
};
const BLADE = { base: '#b4bcc8', shade: '#6c7486', lite: '#d8dee6', spec: '#f0f2f4', ink: '#14162a' };
const screw = (a, x, y, r = .42) => a.shape(ell(x, y, r), STEEL, { outer: false, edge: false, shade: band(x - r, y + .05, x + r, y + r), lines: [`M${x - r * .7} ${y + r * .3}L${x + r * .7} ${y - r * .3}`] });
// Its parts traced from the reference (`tools/anime-art/trace`): [op, material, path, options], in the picture's pixels.
const FIRE_AXE = [
  ['shape', 'wood', 'M29.03 0.44C29.53 0.4 30.74 0.4 31.21 0.45C31.67 0.5 31.82 0.75 31.82 0.75C31.82 0.75 32.17 4.79 32.45 6.46C32.73 8.14 33.49 10.8 33.49 10.8C33.49 10.8 33.17 11.16 32.9 11.34C32.63 11.53 32.43 11.71 31.89 11.91C31.34 12.11 30.38 12.46 29.63 12.54C28.88 12.63 28.11 12.57 27.39 12.42C26.68 12.28 25.34 11.69 25.34 11.69C25.34 11.69 26.46 9.68 26.81 8.98C27.15 8.28 27.26 7.98 27.42 7.5C27.57 7.02 27.73 6.47 27.73 6.14C27.74 5.8 27.46 5.49 27.46 5.49C27.46 5.49 25.89 5.41 25.51 5.34C25.13 5.27 26.03 5.19 25.2 5.09C24.37 4.99 23.22 4.81 20.54 4.76C17.87 4.7 11.5 4.75 9.15 4.76C6.8 4.77 6.93 4.79 6.44 4.83C5.95 4.87 6.34 4.95 6.21 4.99C6.07 5.02 6.52 5.01 5.62 5.02C4.72 5.02 0.79 4.99 0.79 4.99C0.79 4.99 0.57 4.79 0.53 4.49C0.49 4.19 0.5 3.47 0.55 3.19C0.6 2.91 0.82 2.8 0.82 2.8C0.82 2.8 1.67 2.91 1.95 2.9C2.23 2.89 1.9 2.78 2.49 2.76C3.08 2.73 4.82 2.75 5.48 2.76C6.14 2.76 6.24 2.76 6.44 2.79C6.65 2.83 6.17 2.93 6.73 2.97C7.28 3.01 7.64 3.03 9.76 3.04C11.89 3.05 16.88 3.1 19.46 3.04C22.04 2.98 24.22 2.78 25.23 2.68C26.23 2.59 25.1 2.52 25.49 2.45C25.87 2.39 27.14 2.39 27.56 2.29C27.98 2.19 27.92 2.12 28.02 1.85C28.12 1.58 28.17 0.67 28.17 0.67C28.17 0.67 28.52 0.48 29.03 0.44Z', {}],
  ['shape', 'wood', 'M25.34 2.63C25.34 2.63 25.5 2.84 25.54 3.05C25.58 3.26 25.57 3.61 25.57 3.9C25.57 4.19 25.58 4.57 25.54 4.77C25.49 4.97 25.32 5.13 25.32 5.13C25.32 5.13 23.7 4.81 20.54 4.76C17.38 4.71 6.37 4.84 6.37 4.84C6.37 4.84 6.22 4.83 6.22 4.58C6.23 4.33 6.33 3.63 6.39 3.36C6.46 3.09 6.63 2.96 6.63 2.96C6.63 2.96 17.73 3.07 20.85 3.02C23.97 2.96 25.34 2.63 25.34 2.63Z', {'shade':0.22,'deep':0.088}],
  ['shape', 'dark', 'M27.33 2.32C27.72 2.31 27.7 2.46 27.7 2.46C27.7 2.46 27.79 3.66 27.79 4.04C27.79 4.42 27.76 4.52 27.7 4.75C27.63 4.97 27.39 5.39 27.39 5.39C27.39 5.39 25.41 5.31 25.41 5.31C25.41 5.31 25.57 4.32 25.57 3.85C25.57 3.39 25.4 2.51 25.4 2.51C25.4 2.51 26.95 2.33 27.33 2.32Z', {'shade':0.22,'deep':0.088}],
  ['shape', 'dark', 'M5.3 2.77C5.56 2.78 6.6 2.89 6.6 2.89C6.6 2.89 6.36 3.5 6.3 3.85C6.24 4.2 6.23 4.98 6.23 4.98C6.23 4.98 4.77 4.92 4.77 4.92C4.77 4.92 4.64 4.7 4.63 4.51C4.63 4.32 4.67 4.06 4.74 3.78C4.81 3.5 5.05 2.85 5.05 2.85C5.05 2.85 5.05 2.76 5.3 2.77Z', {'shade':0.22,'deep':0.088}],
  ['shape', 'dark', 'M3.84 2.77C4.09 2.77 5.03 2.86 5.03 2.86C5.03 2.86 4.81 3.51 4.74 3.78C4.68 4.06 4.63 4.33 4.63 4.51C4.63 4.69 4.76 4.86 4.76 4.86C4.76 4.86 4.88 4.94 4.65 4.97C4.43 4.99 3.66 5.01 3.41 4.99C3.16 4.98 3.15 4.88 3.15 4.88C3.15 4.88 3.29 3.67 3.36 3.33C3.42 3 3.55 2.84 3.55 2.84C3.55 2.84 3.6 2.77 3.84 2.77Z', {'shade':0.22,'deep':0.088}],
  ['shape', 'dark', 'M2.41 2.77C2.65 2.76 3.51 2.84 3.51 2.84C3.51 2.84 3.08 4.99 3.08 4.99C3.08 4.99 1.59 4.91 1.59 4.91C1.59 4.91 1.59 4.47 1.66 4.13C1.74 3.8 2.03 2.91 2.03 2.91C2.03 2.91 2.16 2.78 2.41 2.77Z', {'shade':0.22,'deep':0.088}],
  ['shape', 'dark', 'M1.26 3.99C1.43 3.96 1.51 4.02 1.51 4.02C1.51 4.02 1.7 4.17 1.71 4.32C1.72 4.48 1.57 4.94 1.57 4.94C1.57 4.94 0.72 4.98 0.72 4.98C0.72 4.98 0.51 4.22 0.51 4.22C0.51 4.22 1.09 4.03 1.26 3.99Z', {'shade':0.22,'deep':0.088}],
  ['shape', 'dark', 'M1.55 2.82C1.55 2.82 2.05 3.05 2.05 3.05C2.05 3.05 1.64 4.09 1.64 4.09C1.64 4.09 1.51 4.13 1.49 3.92C1.47 3.71 1.55 2.82 1.55 2.82Z', {'shade':0.22,'deep':0.088}],
  ['shape', 'red', 'M28.11 1.05C28.11 1.05 28.17 1.27 28.66 1.31C29.16 1.36 31.09 1.31 31.09 1.31C31.09 1.31 31.58 2.21 31.58 2.21C31.58 2.21 31.95 2.25 31.95 2.25C31.95 2.25 32.19 4.78 32.35 5.85C32.51 6.93 32.91 8.7 32.91 8.7C32.91 8.7 32.67 9.04 32.57 9.12C32.47 9.19 32.3 9.17 32.3 9.17C32.3 9.17 32.23 9.55 32.23 9.55C32.23 9.55 32.42 9.56 31.98 9.68C31.54 9.79 30.38 10.1 29.58 10.23C28.79 10.37 27.2 10.5 27.2 10.5C27.2 10.5 27.05 9.92 27.05 9.92C27.05 9.92 26.26 10 26.26 10C26.26 10 26.86 8.92 27.09 8.37C27.32 7.83 27.52 7.13 27.63 6.72C27.74 6.32 27.77 6.14 27.73 5.92C27.69 5.71 27.39 5.43 27.39 5.43C27.39 5.43 27.76 4.69 27.79 4.16C27.83 3.63 27.6 2.25 27.6 2.25C27.6 2.25 27.93 2.05 28.02 1.85C28.1 1.65 28.11 1.05 28.11 1.05Z', {'shade':0.437,'deep':0.175}],
  ['shape', 'steel', 'M32.85 8.79C32.85 8.79 33.48 10.87 33.48 10.87C33.48 10.87 32.93 11.33 32.62 11.53C32.31 11.72 32.05 11.87 31.63 12.02C31.21 12.18 30.62 12.38 30.1 12.47C29.58 12.56 29.07 12.61 28.5 12.57C27.92 12.53 27.16 12.38 26.64 12.23C26.11 12.09 25.34 11.71 25.34 11.71C25.34 11.71 25.96 10.39 26.24 10.09C26.52 9.79 27.04 9.92 27.04 9.92C27.04 9.92 27.12 10.44 27.12 10.44C27.12 10.44 26.74 10.56 27.25 10.5C27.77 10.45 29.4 10.26 30.22 10.12C31.03 9.97 32.15 9.62 32.15 9.62C32.15 9.62 32.29 9.19 32.29 9.19C32.29 9.19 32.48 9.18 32.57 9.12C32.66 9.05 32.85 8.79 32.85 8.79Z', {'shade':0.283,'deep':0.113}],
  ['shape', 'steel', 'M29.03 0.44C29.53 0.43 30.98 0.44 31.44 0.48C31.9 0.53 31.79 0.7 31.79 0.7C31.79 0.7 31.87 1.19 31.87 1.19C31.87 1.19 31.7 1.31 31.16 1.32C30.62 1.34 29.15 1.37 28.64 1.31C28.13 1.25 28.11 0.98 28.11 0.98C28.11 0.98 28.09 0.8 28.14 0.72C28.19 0.64 28.28 0.56 28.43 0.51C28.58 0.46 28.53 0.44 29.03 0.44Z', {'shade':0.22,'deep':0.088}],
  ['shape', 'steel', 'M0.75 2.82C0.75 2.82 1.47 2.84 1.47 2.84C1.47 2.84 1.49 3.9 1.49 3.9C1.49 3.9 0.56 4.16 0.56 4.16C0.56 4.16 0.5 3.56 0.53 3.33C0.56 3.11 0.75 2.82 0.75 2.82Z', {'shade':0.22,'deep':0.088}],
  ['shape', 'steel', 'M31.38 1.26C31.49 1.25 31.86 1.29 31.86 1.29C31.86 1.29 31.91 2.2 31.91 2.2C31.91 2.2 31.61 2.23 31.61 2.23C31.61 2.23 31.34 1.94 31.28 1.79C31.21 1.65 31.21 1.36 31.21 1.36C31.21 1.36 31.28 1.27 31.38 1.26Z', {'shade':0.22,'deep':0.088}],
  ['shape', 'dark', 'M26.41 2.91C26.41 2.91 26.9 2.98 26.9 2.98C26.9 2.98 26.78 3.55 26.78 3.55C26.78 3.55 26.54 3.58 26.54 3.58C26.54 3.58 26.28 3.31 26.28 3.31C26.28 3.31 26.41 2.91 26.41 2.91Z', {'shade':0.22,'deep':0.088}],
  ['shape', 'steel', 'M26.45 4.23C26.54 4.2 26.67 4.2 26.76 4.24C26.84 4.27 26.92 4.36 26.95 4.44C26.99 4.52 27.02 4.61 26.98 4.7C26.95 4.78 26.85 4.91 26.76 4.95C26.66 5 26.43 4.98 26.43 4.98C26.43 4.98 26.26 4.86 26.22 4.77C26.19 4.68 26.19 4.53 26.23 4.44C26.27 4.35 26.36 4.26 26.45 4.23Z', {'shade':0.22,'deep':0.088}],
  ['line', null, 'M25.65 2.86C25.65 2.86 25.61 3.2 25.6 3.43C25.6 3.66 25.61 4.06 25.62 4.23C25.63 4.4 25.67 4.33 25.67 4.44C25.66 4.55 25.58 4.8 25.58 4.91C25.58 5.02 25.65 5.1 25.65 5.1', { w: 1 }],
  ['line', null, 'M25.44 3.08C25.44 3.08 25.4 2.94 25.39 3.22C25.38 3.49 25.39 4.47 25.38 4.75C25.37 5.02 25.34 4.83 25.31 4.85C25.28 4.87 25.2 4.86 25.2 4.86', { w: 1 }],
  ['line', null, 'M24.99 3.73C24.99 3.73 23.77 3.74 23.46 3.73C23.15 3.73 23.13 3.69 23.13 3.69', { w: 1 }],
];
const axe = (a) => {
  for (const [op, mat, d, o] of FIRE_AXE) {
    if (op === 'shape') a.shape(d, AXE[mat], o);
    else if (op === 'flat') a.flat(d, AXE[mat].base);
    else a.line(d, o);
  }
};
const drill = (spin) => (a) => {
  // The back end with its vents and lamp, the grip with its ribs, trigger and battery; a steel housing, bolted, with a heat band; a toothed chuck; the bit, whose flutes turn when it spins.
  a.shape(rr(0, 2.6, 9, 6.6, 1.2), GUN, { lite: band(.6, 2.7, 8.4, 3.6), shade: band(.6, 7.8, 8.4, 9.2) });
  for (const x of [3.2, 4.5, 5.8, 7.1]) a.flat(poly(x, 4.2, x + .55, 4.2, x + .35, 7, x - .2, 7), '#14172c');
  a.flat(ell(1.6, 4.6, .55), spin === null ? '#54ba74' : '#a0f0a0');
  screw(a, 1.3, 7.6);
  a.shape(poly(8, 9.2, 9.6, 9.2, 9.8, 10, 8.9, 11.2, 8.4, 11), RED, { lite: band(8.2, 9.3, 9.5, 9.8) });
  a.shape(rr(3.4, 8.8, 4, 4.4, .6), DARK, { lite: band(3.8, 8.9, 5, 13) });
  for (const y of [10, 10.9, 11.8]) a.line(`M3.7 ${y}L7.1 ${y}`, { w: .9 });
  a.shape(rr(2.6, 12.4, 5.6, 2.4, .4), GUN, { lite: band(2.9, 12.5, 7.9, 13.1), shade: band(2.9, 14.1, 7.9, 14.8) });
  a.flat(band(3.4, 13.4, 5.6, 13.9), spin === null ? '#54ba74' : '#a0f0a0');
  a.shape(rr(8.6, 1.8, 13, 7.6, .8), STEEL, { lite: band(9, 1.9, 21.2, 2.8), spec: band(10, 2.1, 15, 2.5), shade: band(9, 7.8, 21.2, 9.4), lines: ['M18.9 1.9L18.9 9.3', 'M10.2 1.9L10.2 9.3'] });
  for (const [x, y] of [[9.4, 2.9], [9.4, 8.2], [20.2, 2.9], [20.2, 8.2]]) screw(a, x, y);
  for (let i = 0; i < 3; i++) a.flat(band(11.2 + i * 2.5, 4.1, 12.5 + i * 2.5, 6.9), '#c8703c');
  a.line('M11 3.5L18 3.5', { w: .9 });
  a.flat(poly(13.2, 7.4, 15.8, 7.4, 15.6, 8.1, 13.4, 8.1), '#3a2a22');
  a.shape(poly(21.4, 2.8, 24.8, 3.8, 24.8, 7.4, 21.4, 8.4), GUN, { lite: band(21.4, 2.8, 24.8, 4.2), shade: band(21.4, 7, 24.8, 8.4) });
  for (const x of [22.2, 23, 23.8]) a.line(`M${x} ${3 + (x - 21.4) * .28}L${x} ${8.2 - (x - 21.4) * .28}`, { w: .9 });
  const y0 = 3.6, y1 = 7.6;
  a.shape(poly(24.8, y0, 32.8, 5.6, 24.8, y1), { base: '#dfe7f2', shade: '#868fa2', lite: '#ffffff', spec: '#ffffff', ink: '#14162a' }, { shade: poly(24.8, 5.6, 32.8, 5.6, 24.8, y1) });
  for (let i = 0; i < 3; i++) { const x = 26 + i * 2 + (spin === null ? 0 : (spin % 3) * .66); a.flat(poly(x, 3.9, x + .7, 4.1, x - .2, 7.2, x - .9, 7), '#868fa2', { clip: poly(24.8, y0, 32.8, 5.6, 24.8, y1) }); }
};

// The salvage tools, each the user's pick drawn whole (salvage-tools): the grinder «Заводская», the plasma cutter «Кольца», the spear
// «Труба и лист», the sledgehammer «Пакет плит». Their pick pages' own materials and helpers, where they differ from the ones above.
const DEG = Math.PI / 180, f2 = (n) => +n.toFixed(3);
const RUBBER = { base: '#2e2c2a', shade: '#1d1c1b', deep: '#121212', lite: '#45423e' };
const RED_PAINT = { base: '#a63c2e', shade: '#742a28', deep: '#541e20', lite: '#c8604a', spec: '#e8a890' };
const BRASS_BAND = { base: '#b08c48', shade: '#7c5e30', deep: '#574126', lite: '#d4b470', spec: '#f0dca4' };
const STUD = { base: '#b4bcc8', shade: '#6c7486', deep: '#474c68', lite: '#d8dee6', spec: '#f0f2f4' };
const SLOT = '#14172c', CHIP = '#8e96a2';
// Full saturation only for what works: the running lamp the game's own green, the charge lights' steady green, the arc's violet.
const LAMP_ON = '#a0f0a0', LAMP_OFF = '#3d5a46', CHG = '#4f9a58', ON = '#9dff8a', VIOLET_ON = '#b49aff', VIOLET_OFF = '#2e2844';
/** Points along a circle from a0 to a1, in degrees (+y down: 0 ahead, 90 down, 270 up). */
const arcPts = (cx, cy, r, a0, a1, n) => {
  const p = [];
  for (let i = 0; i <= n; i++) { const t = (a0 + (a1 - a0) * i / n) * DEG; p.push(cx + Math.cos(t) * r, cy + Math.sin(t) * r); }
  return p;
};
const steps = (a0, a1) => Math.max(4, Math.ceil(Math.abs(a1 - a0) / 9));
/** A band of a ring between radii r0 and r1, from a0 to a1 degrees. */
const arcBand = (cx, cy, r0, r1, a0, a1) => { const n = steps(a0, a1); return poly(arcPts(cx, cy, r1, a0, a1, n).concat(arcPts(cx, cy, r0, a1, a0, n))); };
/** An open arc, for a pen line. */
const arcPath = (cx, cy, r, a0, a1) => {
  const p = arcPts(cx, cy, r, a0, a1, steps(a0, a1));
  let s = `M${f2(p[0])} ${f2(p[1])}`;
  for (let i = 2; i < p.length; i += 2) s += `L${f2(p[i])} ${f2(p[i + 1])}`;
  return s;
};
/** A band between two ellipses round one middle, from a0 to a1 degrees: a ring seen near edge-on. */
const ellBand = (cx, cy, rx0, ry0, rx1, ry1, a0, a1) => {
  const n = steps(a0, a1) * 2, p = [];
  for (let i = 0; i <= n; i++) { const t = (a0 + (a1 - a0) * i / n) * DEG; p.push(cx + Math.cos(t) * rx1, cy + Math.sin(t) * ry1); }
  for (let i = n; i >= 0; i--) { const t = (a0 + (a1 - a0) * i / n) * DEG; p.push(cx + Math.cos(t) * rx0, cy + Math.sin(t) * ry0); }
  return poly(p);
};
const stud = (a, x, y, r = .42) => a.shape(ell(x, y, r), STUD, { outer: false, edge: false, shade: band(x - r, y + .05, x + r, y + r), lines: [`M${f2(x - r * .7)} ${f2(y + r * .3)}L${f2(x + r * .7)} ${f2(y - r * .3)}`] });
/** Rust round a seam or stud: a torn patch, its darker heart. */
const rustAt = (a, x, y, s) => {
  a.flat(poly(x - s, y - .2 * s, x - .3 * s, y - .7 * s, x + .5 * s, y - .5 * s, x + s, y + .1 * s, x + .4 * s, y + .45 * s, x - .5 * s, y + .4 * s), '#6e4630');
  a.flat(poly(x - .4 * s, y - .1 * s, x + .1 * s, y - .3 * s, x + .4 * s, y + .05 * s, x, y + .3 * s), '#3f2a24');
};
/** Rust where water sat: a torn patch with a darker heart, kept inside its part. */
const rust = (a, x, y, s, clip) => {
  a.flat(poly(x, y, x + 1.3 * s, y - .25 * s, x + 1.9 * s, y + .4 * s, x + 1.3 * s, y + 1.1 * s, x + .6 * s, y + .8 * s, x - .1 * s, y + .9 * s), '#6e5646', { clip });
  a.flat(poly(x + .5 * s, y + .15 * s, x + 1.3 * s, y + .1 * s, x + 1.3 * s, y + .7 * s, x + .6 * s, y + .6 * s), '#4f3d36', { clip });
};
/** Wire wound round a part: turns across it, copper. */
const wire = (a, x0, x1, y0, y1, step = .55) => {
  a.flat(band(x0, y0, x1, y1), COPPER.shade);
  for (let x = x0; x < x1 - .1; x += step) a.flat(poly(x, y0, x + step * .55, y0, x + step * .8, y1, x + step * .25, y1), COPPER.lite);
};
/** Several pictures of one size laid in order, each finished alone, as the pick page lays a tool's front over it. */
const layered = (w, h, draws, px = 4) => {
  const c = canvas(w * px, h * px), x = c.getContext('2d');
  for (const draw of draws) x.drawImage(bake({ w, h, px, draw }), 0, 0);
  return c;
};

// The grinder «Заводская»: a long motor tube in worn ochre enamel, vents at its back, a paddle under it; a cast head, the wheel under a
// half guard. A state: f null standing, 1..3 the disc's turn while it spins; on, its lamp and charge lights lit.
const DISC = { base: '#57524e', shade: '#37332f', deep: '#24211f', lite: '#79726b', spec: '#a8a098' };
const OCHRE = { base: '#9a7c44', shade: '#6a5432', deep: '#4a3b28', lite: '#bc9e62', spec: '#e0cc98' };
/** A cut-off wheel seen face on: grit, its paper label, the print and the nut turning with it; spinning, the marks smear round. */
const wheel = (a, cx, cy, r, f, o = {}) => {
  const th = f === null ? 0 : f * 40, spin = f !== null;
  a.shape(ell(cx, cy, r), DISC, { shade: .7, lines: [arcPath(cx, cy, r * .84, 35, 145)] });
  for (let k = 0; k < 3; k++) {
    const t = th + 60 + k * 120;
    if (spin) { a.flat(arcBand(cx, cy, r * .72, r * .78, t - 30, t + 50), DISC.lite); a.flat(arcBand(cx, cy, r * .86, r * .9, t + 10, t + 80), DISC.lite); }
    else a.line(`M${f2(cx + Math.cos(t * DEG) * r * .68)} ${f2(cy + Math.sin(t * DEG) * r * .68)}L${f2(cx + Math.cos(t * DEG) * r * .93)} ${f2(cy + Math.sin(t * DEG) * r * .93)}`, { w: .9 });
  }
  a.flat(ell(cx, cy, r * .56), o.label || '#7a6c4e');
  a.flat(arcBand(cx, cy, r * .46, r * .56, 15, 165), o.labelShade || '#5a4e3a');
  for (let k = 0; k < 3; k++) { const t = th + k * 120; a.flat(arcBand(cx, cy, r * .33, r * .49, t, t + (spin ? 80 : 46)), o.print || '#3a2e22'); }
  a.shape(ell(cx, cy, r * .27), STEEL, { outer: false, shade: .25 });
  const hex = [];
  for (let k = 0; k < 6; k++) { const t = (th + 30 + k * 60) * DEG; hex.push(cx + Math.cos(t) * r * .17, cy + Math.sin(t) * r * .17); }
  a.shape(poly(hex), GUN, { outer: false, shade: .12 });
  for (const t of [th + 90, th + 270]) a.flat(ell(cx + Math.cos(t * DEG) * r * .22, cy + Math.sin(t * DEG) * r * .22, .18), '#14172c');
};
/** A guard's lip round the wheel from a0 to a1 degrees, over its rim: lit along the top, rolled at the edge. */
const guard = (a, cx, cy, r, a0, a1, m, o = {}) => {
  const r0 = r - (o.inset ?? .5), r1 = r + (o.out ?? 1);
  a.shape(arcBand(cx, cy, r0, r1, a0, a1), m, { shade: .3, lite: arcBand(cx, cy, r1 - .5, r1, Math.max(a0, 200), Math.min(a1, 340)), lines: [arcPath(cx, cy, r1 - .32, a0 + 3, a1 - 3), ...(o.lines || [])] });
};
const battery = (a, x, y, w, h, on) => {
  a.shape(rr(x, y, w, h, .5), GUN, { lite: band(x + .2, y + .1, x + w - .2, y + .6), shade: band(x, y + h - .7, x + w, y + h), lines: [`M${x + w - 1.4} ${y + .1}L${x + w - 1.4} ${y + h - .1}`] });
  for (let i = 0; i < 3; i++) a.glow(rr(x + .8 + i * 1.3, y + h * .45, .9, .55, .15), on ? ON : CHG);
};
const grinder = (s) => (a) => {
  const cx = 22.6, cy = 7.2, R = 4.6;
  battery(a, 1.4, 11.9, 7, 2.6, s.on);
  a.shape('M3.4 7.2L8 7.2L7.4 9.6C7.2 10.6 7.2 11.4 7.4 12.2L3 12.2C2.8 11 2.8 9.6 3.4 7.2Z', RUBBER, { shade: band(6.4, 7, 8.2, 12.4), lines: ['M3.3 8.6L7.7 8.6', 'M3.1 9.6L7.4 9.6', 'M3 10.6L7.3 10.6', 'M3 11.5L7.4 11.5'] });
  a.shape('M7.8 7.4L12.6 7.6L12.2 8.8L8.4 8.6Z', DARK, { lite: band(8, 7.5, 12.4, 7.95) });
  a.shape('M7.4 7.3L8.4 7.4L8.3 8.3L7.5 8Z', RED_PAINT, { outer: false });
  a.shape(rr(1.2, 2.4, 15.4, 5.4, 2.2), OCHRE, { lite: band(1.6, 2.5, 16.2, 3.4), spec: band(7.2, 2.75, 12.6, 3.05), shade: band(1.2, 6.6, 16.6, 7.8), lines: ['M6.6 2.5L6.6 7.7', 'M13.6 2.5L13.6 7.7'] });
  for (const x of [2.7, 3.7, 4.7, 5.7]) a.flat(poly(x, 3.7, x + .5, 3.7, x + .3, 6.4, x - .2, 6.4), SLOT);
  a.shape(rr(8.2, 4, 4.2, 1.8, .3), DARK, { outer: false, lite: band(8.3, 4.05, 12.3, 4.4) });
  a.line('M8.8 4.95L11.6 4.95', { w: .8, c: '#8a8070' });
  a.flat(poly(14.6, 2.5, 15.9, 2.7, 15.8, 3.6, 15, 3.3), CHIP);
  a.flat(poly(9.4, 7.3, 10.8, 7.2, 10.6, 7.75, 9.6, 7.75), CHIP);
  rustAt(a, 13.4, 6.9, .7);
  a.shape(rr(.3, 3, 1.6, 4.2, .7), DARK, { lite: band(.4, 3.1, 1.8, 3.6) });
  a.glow(ell(1.1, 4.3, .42), s.on ? LAMP_ON : LAMP_OFF, s.on ? { core: ell(1.05, 4.2, .16) } : {});
  a.shape('M16.2 2.3L21.2 1.6C22.6 1.5 23.6 2.4 23.6 3.8L23.6 8.2L16.2 8.2Z', GUN, { lite: band(16.4, 1.7, 23.4, 2.6), shade: band(16.2, 7.2, 23.8, 8.4), lines: ['M17.6 2.2L17.6 8.1', 'M18.8 2.1L18.8 4'] });
  stud(a, 21.6, 2.6);
  wheel(a, cx, cy, R, s.f, { label: '#4e6478', labelShade: '#3a4a5a', print: '#d8d2c2' });
  guard(a, cx, cy, R, 150, 350, STEEL);
  stud(a, cx + Math.cos(205 * DEG) * (R + .3), cy + Math.sin(205 * DEG) * (R + .3), .36);
  rustAt(a, cx + Math.cos(165 * DEG) * (R + .3), cy + Math.sin(165 * DEG) * (R + .3), .45);
};

// The plasma cutter «Кольца»: a short dark body banded in brass, a copper nozzle, and the jet's cage: three coils on a rail, their inner
// faces lit violet while it burns. Each ring's far half is drawn with the body, its near half (front) over it, as the jet will run between.
const pommel = (a, x, y, w, h, on) => {
  a.shape(rr(x, y, w, h, .9), DARK, { lite: band(x + .3, y + .1, x + w - .3, y + .6), shade: band(x, y + h - .7, x + w, y + h), lines: [`M${x + w - 1.4} ${y + .1}L${x + w - 1.4} ${y + h - .1}`] });
  for (let i = 0; i < 3; i++) a.glow(rr(x + .9 + i * 1.3, y + h * .4, .9, .5, .15), on ? ON : CHG);
};
const RINGS = [17.6, 21.8, 26];
const ringBand = (x, a0, a1) => ellBand(x, 4.4, .45, 2.3, 1.1, 3, a0, a1);
const ringGlow = (x, a0, a1) => ellBand(x, 4.4, .45, 2.3, .72, 2.55, a0, a1);
const cutter = (s) => (a) => {
  pommel(a, 1.4, 9.6, 6, 2.2, s.on);
  a.shape('M2.8 6.2L6.8 6.2L6.4 7.8C6.2 8.6 6.2 9.2 6.4 9.8L2.4 9.8C2.3 8.8 2.4 7.6 2.8 6.2Z', RUBBER, { shade: band(5.4, 6.2, 7, 9.8), lines: ['M2.7 7.2L6.5 7.2', 'M2.5 8.1L6.3 8.1', 'M2.4 9L6.3 9'] });
  a.shape('M7 6.4L8.4 6.4L8.6 7.2L7.6 8.2L7.1 7.8Z', DARK, { shade: .3 });
  a.shape(rr(10, 6.2, 1.6, 2.7, .3), GUN, { lite: band(10.1, 6.3, 10.6, 8.8) });
  a.shape(rr(10.8, 7.7, 18.6, 1.2, .4), GUN, { lite: band(11, 7.75, 29.2, 8.1), shade: band(10.8, 8.5, 29.4, 8.9) });
  a.shape(rr(.4, 2.2, 11.4, 4.4, 1.6), DARK, { lite: band(.8, 2.3, 11.4, 3.1), spec: band(3, 2.5, 7, 2.75), shade: band(.4, 5.6, 11.8, 6.6), lines: ['M3.4 2.3L3.4 6.5'] });
  a.shape(rr(8.2, 2, 1.5, 4.8, .3), BRASS_BAND, { lite: band(8.3, 2.05, 9.6, 2.6), shade: band(8.2, 6, 9.7, 6.8) });
  a.glow(ell(1.7, 4, .45), s.on ? VIOLET_ON : VIOLET_OFF, s.on ? { core: ell(1.65, 3.9, .17) } : {});
  a.shape(poly(11.6, 3.3, 13.8, 3.8, 13.8, 5, 11.6, 5.5), COPPER, { lite: poly(11.6, 3.3, 13.8, 3.8, 13.8, 4.1, 11.6, 3.8), shade: poly(11.6, 5, 13.8, 4.7, 13.8, 5, 11.6, 5.5) });
  for (const x of RINGS) {
    a.shape(rr(x - .45, 6.9, .9, 1, .2), GUN, { shade: band(x - .45, 7.5, x + .45, 7.9) });
    a.shape(ringBand(x, -90, 90), COPPER, { shade: ringBand(x, 20, 90) });
    a.flat(ringGlow(x, -80, 80), s.on ? VIOLET_ON : COPPER.shade);
  }
};
const cutterFront = (s) => (a) => {
  for (const x of RINGS) {
    a.shape(ringBand(x, 90, 270), COPPER, { lite: ringBand(x, 200, 270), shade: ringBand(x, 90, 140), lines: [120, 150, 180, 210, 240].map((t) => { const c = Math.cos(t * DEG), sn = Math.sin(t * DEG); return `M${f2(x + c * .5)} ${f2(4.4 + sn * 2.35)}L${f2(x + c * 1.05)} ${f2(4.4 + sn * 2.95)}`; }) });
    if (s.on) a.flat(ringGlow(x, 100, 260), VIOLET_ON);
  }
};

// The spear «Труба и лист»: a hull plate's leaf driven into a flattened tube, bound in copper; its grip in tape, a sleeve for the coil.
const spear = (a) => {
  a.tube('M1.6 5L33.4 5', 2.2, PIPE);
  a.shape(rr(0, 3.5, 2.2, 3, .5), GUN, { lite: band(.2, 3.6, 2, 4.2), shade: band(.2, 5.8, 2, 6.5) });
  a.flat(band(4.6, 3.85, 13.4, 6.15), '#2a2b36');
  for (let x = 4.8; x < 12.9; x += 1.15) { a.flat(poly(x, 3.85, x + .7, 3.85, x + 1, 6.15, x + .3, 6.15), '#3e4152'); a.line(`M${x + .72} 3.9L${x + 1.02} 6.1`, { w: .8 }); }
  a.line('M12.9 6.1C13.3 6.9 13.4 7.6 13 8.3', { w: 1.2 });
  rust(a, 17, 4.3, .8, band(14, 3.9, 22, 6.1));
  a.shape(rr(22, 3.5, 8, 3, .4), GUN, { lite: band(22.2, 3.6, 29.8, 4.2), shade: band(22.2, 5.8, 29.8, 6.5), lines: ['M22.9 3.6L22.9 6.4', 'M29.1 3.6L29.1 6.4'] });
  rivets(a, [23.6, 5], [28.4, 5]);
  a.shape(poly(33, 3.7, 36.3, 3.2, 36.3, 6.8, 33, 6.3), PIPE, { lite: band(33, 3.2, 36.3, 4), shade: band(33, 6, 36.3, 6.8) });
  for (const x of [33.5, 34.3, 35.1]) a.flat(poly(x, 3.3, x + .45, 3.3, x + .75, 6.7, x + .3, 6.7), COPPER.lite);
  a.shape('M36 3.5C38 2.3 40.6 2.4 42.4 3.4L47.8 5L42.4 6.6C40.6 7.6 38 7.7 36 6.5Z', BLADE, {
    lite: 'M36.4 3.6C38.4 2.6 40.6 2.7 42.2 3.6L47 4.9L36.4 4.9Z', shade: 'M36.4 5.1L47 5.1L42.2 6.4C40.6 7.3 38.4 7.4 36.4 6.4Z', lines: ['M36.8 5L46.6 5'] });
  rust(a, 36.2, 4.2, .7, 'M36 3.5C38 2.3 40.6 2.4 42.4 3.4L47.8 5L42.4 6.6C40.6 7.6 38 7.7 36 6.5Z');
  screw(a, 37.4, 5.9, .4);
  a.line('M40.6 3.2L41.6 3.6', { w: .9 });
};

// The sledgehammer «Пакет плит»: five cut plates bolted into a block on a plank, a tube ferrule where they meet; the striking face along the bottom.
const FACE = '#c0c6cf';
const sledgehammer = (a) => {
  a.shape(rr(0, 6.7, 22.4, 2.6, .5), WOODH, { lite: band(.4, 6.8, 22, 7.4), shade: band(.4, 8.6, 22, 9.3), lines: ['M12.4 7.8L17 7.7', 'M14.6 8.5L19.6 8.4'] });
  wire(a, .5, 2, 6.6, 9.4, .45);
  a.flat(band(3, 6.6, 10.2, 9.4), '#23263a');
  for (let x = 3.2; x < 9.9; x += 1.2) { a.flat(poly(x, 6.6, x + .75, 6.6, x + 1.05, 9.4, x + .3, 9.4), '#3d4258'); a.line(`M${x + .78} 6.6L${x + 1.08} 9.4`, { w: .8 }); }
  const tops = [1.3, 1, 1.5, 1.1, 1.4], bots = [14.7, 15.1, 14.8, 15.2, 14.9];
  for (let i = 0; i < 5; i++) {
    const x = 21.4 + i * 2, m = i % 2 ? GUN : STEEL, d = rr(x, tops[i], 2, bots[i] - tops[i], .3);
    a.shape(d, m, { lite: band(x, tops[i], x + 2, tops[i] + .8), shade: band(x + 1.35, tops[i], x + 2, bots[i]), deep: band(x, bots[i] - .6, x + 2, bots[i]) });
    a.flat(band(x, bots[i] - .5, x + 2, bots[i]), FACE, { clip: d });
  }
  rust(a, 23.2, 9.6, .8, band(21.4, 1, 31.4, 15.2)); rust(a, 27.4, 3.4, .6, band(21.4, 1, 31.4, 15.2));
  for (const y of [3.2, 12.8]) {
    a.shape(rr(31.2, y - .9, .8, 1.8, .2), DARK, { lite: band(31.2, y - .9, 32, y - .4) });
    a.shape(rr(20.6, y - .9, .9, 1.8, .2), DARK, { lite: band(20.6, y - .9, 21.5, y - .4) });
  }
  a.shape(rr(18, 6.1, 3.6, 3.8, .5), PIPE, { lite: band(18.2, 6.2, 21.4, 6.9), shade: band(18.2, 9.1, 21.4, 9.9) });
  screw(a, 19.8, 8, .45);
};

// The pickaxe «Шип из шпангоута» (the-pickaxe): a length of a ship's rib in old primer, its lightening hole kept, torch-cut into a spike
// and welded to a pipe haft gripped in rubber; the spike's point down, what meets the stone.
const PRIMER = { base: '#7a5546', shade: '#563b34', deep: '#3c2a2a', lite: '#97705c', spec: '#c09a84' };
const WELD = { base: '#8c919c', shade: '#5c6070', deep: '#3a3e50', lite: '#b4bac4' };
const SPIKE_FACE = '#c6ccd4';
/** A bead of weld: small blobs along a seam. */
const bead = (pts, rx = .55, ry = .45) => pts.map(([x, y]) => ell(x, y, rx, ry)).join('');
const WEB = 'M25.6 3.8L26.8 3.4L27.6 3.9L28.8 3.3L30 3.7L31.4 3.4L31.6 8.6L30.6 12L28.7 17.7L27.8 13.8L26.2 11.4L25.8 10.6Z';
const ribSpike = (a) => {
  a.tube('M1.2 9L26.4 9', 2.4, PIPE);
  a.shape(rr(.3, 7.4, 1.6, 3.2, .4), GUN, { lite: band(.4, 7.5, 1.8, 8), shade: band(.4, 10, 1.8, 10.6) });
  a.shape(rr(2.4, 7.5, 7.4, 3, .9), RUBBER, { lite: band(2.8, 7.6, 9.4, 8.1), shade: band(2.6, 9.8, 9.6, 10.5), lines: ['M4.4 7.6L4.4 10.4', 'M6.6 7.6L6.6 10.4'] });
  a.shape(rr(9.4, 7.3, .9, 3.4, .2), STEEL, { lite: band(9.4, 7.3, 10.3, 7.9), shade: band(9.4, 10, 10.3, 10.7) });
  a.shape(rr(16.4, 7.4, 2, 3.2, .3), GUN, { lite: band(16.5, 7.5, 18.3, 8), shade: band(16.5, 10, 18.3, 10.6) });
  rust(a, 18.2, 8.8, .5, band(18.4, 7.8, 21, 10.2));
  a.shape(WEB, PRIMER, {
    lite: band(25.4, 3, 32, 4.6), shade: 'M30.4 3.6L31.6 3.4L31.6 8.6L30.6 12L28.7 17.7L29.6 12L30.5 8.6Z', deep: 'M28.7 17.7L27.8 13.8L26.2 11.4L27 11.6L28.4 14Z',
    lines: ['M30.4 3.8L30.5 8.6L29.6 12', 'M26.4 11L27.6 13.2'],
  });
  a.cut(ell(28.2, 6.6, 1.1, 1));
  a.line(ell(28.2, 6.6, 1.15, 1.05), { w: 1.3 });
  a.flat(poly(28.7, 17.7, 28.1, 15.4, 29.4, 15.2), SPIKE_FACE, { clip: WEB });
  rust(a, 27, 7.4, .55, WEB);
  a.shape(bead([[26, 7.6], [26.1, 8.3], [26, 9], [26.1, 9.7], [26, 10.4]]), WELD, { outer: false, shade: band(25.4, 9.4, 26.8, 11) });
  a.line('M29.2 3.5L29.5 4.3', { w: .9 });
};

Subjects.zfittings = function () {
  out('pipe_run', bake({ w: 24, h: 8, tile: true, draw: pipeRun }));
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
  out('hero_axe', bake({ w: 34, h: 13, draw: axe }));
  out('hero_drill', bake({ w: 33, h: 15, draw: drill(null) }));
  // Their icons (icons/<picture>), as hero.js draws the hand tools': the same drawing at many pixels a world pixel.
  for (const [name, pic] of [['hero_axe', { w: 34, h: 13, draw: axe }], ['hero_drill', { w: 33, h: 15, draw: drill(null) }]])
    out(`icons/${name}`, bake({ ...pic, px: Math.ceil(Anime.ICON_LONG / pic.w) }));
  for (let f = 1; f <= 3; f++) out(`hero_drill_spin${f}`, bake({ w: 33, h: 15, draw: drill(f) }));
  // The salvage tools: each still, then its running states (the grinder's three turns of the disc, the cutter lit), and its icon.
  const salvage = [
    ['hero_grinder', 29, 15, [grinder({ f: null, on: false })]],
    ['hero_plasmaCutter', 32, 12, [cutter({ on: false }), cutterFront({ on: false })]],
    ['hero_spear', 48, 10, [spear]],
    ['hero_sledgehammer', 32, 16, [sledgehammer]],
    ['hero_pickaxe', 34, 18, [ribSpike]],
  ];
  for (const [name, w, h, draws] of salvage) {
    out(name, layered(w, h, draws));
    out(`icons/${name}`, layered(w, h, draws, Math.ceil(Anime.ICON_LONG / w)));
  }
  for (let f = 1; f <= 3; f++) out(`hero_grinder_spin${f}`, bake({ w: 29, h: 15, draw: grinder({ f, on: true }) }));
  out('hero_plasmaCutter_on', layered(32, 12, [cutter({ on: true }), cutterFront({ on: true })]));
};
})();

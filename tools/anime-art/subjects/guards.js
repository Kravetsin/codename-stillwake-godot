// A vessel's guards' docks, as picked (derelict-enemies): capsule G1 «Зрачок — впадина» on a small room's back wall and stand S1 «Люлька»
// on a big room's floor, ported from the pick pages' draw code (mountG, faceLamp, CAP_G1; STANDS[0]). Asleep: the capsule shut behind its
// smoked iris, its hazard lamp dark (the game blinks its amber pip); the stand without its robot, drawn in front of it later.
// `guard_capsule`, 40 × 48: its dock's 5 × 5 cells and the row over them, the floor at its foot. `guard_stand`, 56 × 64, and
// `guard_stand_front` (yoke and waist clamp, over the robot): its dock's 7 × 8 cells, the robot's middle 3 cells from its left.
(function () {
'use strict';
const { ell, poly } = Anime;

const hex = (v) => '#' + v.map((c) => Math.max(0, Math.min(255, Math.round(c))).toString(16).padStart(2, '0')).join('');
const rgb = (h) => [1, 3, 5].map((i) => parseInt(h.substr(i, 2), 16));
const mix = (a, b, f) => { const p = rgb(a), q = rgb(b); return hex(p.map((v, i) => v + (q[i] - v) * f)); };
const INK = '#0b0e13';
/** The pick pages' palOf: a base, lit a quarter toward white, shaded and deep toward black. */
const pal = (h) => ({ base: h, lite: mix(h, '#ffffff', .26), shade: mix(h, '#000000', .3), deep: mix(h, '#000000', .55), spec: mix(h, '#ffffff', .5), ink: INK });
const STEEL = pal('#737a83'), STEEL_D = pal('#555c66'), DARK = pal('#383d45'), GUNM = pal('#474d56');
const PLATE = pal('#383e47'), COLLAR = pal('#6b737d'), SHELF = pal('#5a616b');
const RUST = '#7a3a22', RUST_D = '#4a2214';

const ring = (cx, cy, r, n, a0) => Array.from({ length: n }, (_, i) => { const a = (a0 || 0) + i / n * Math.PI * 2; return [cx + Math.cos(a) * r, cy + Math.sin(a) * r]; });
const arc = (cx, cy, r, a0, a1, n) => Array.from({ length: n + 1 }, (_, i) => { const a = a0 + (a1 - a0) * i / n; return [cx + Math.cos(a) * r, cy + Math.sin(a) * r]; });
const path = (pts) => poly(pts.flat());
const open = (pts) => pts.map((p, i) => `${i ? 'L' : 'M'}${+p[0].toFixed(3)} ${+p[1].toFixed(3)}`).join('');
const oct = (x, y, r) => { const k = r * .41; return [[x - r, y - k], [x - k, y - r], [x + k, y - r], [x + r, y - k], [x + r, y + k], [x + k, y + r], [x - k, y + r], [x - r, y + k]]; };
const rect = (x, y, w, h) => [[x, y], [x + w, y], [x + w, y + h], [x, y + h]];
/** A blob of wear where water sat: a torn edge and a darker heart. */
const wear = (a, x, y, r) => {
  a.flat(path(ring(x, y, r, 9).map(([px, py], i) => [x + (px - x) * (.7 + .3 * ((i * 7) % 5) / 4), y + (py - y) * .75])), RUST);
  a.flat(ell(x + r * .1, y + r * .12, r * .5, r * .38), RUST_D);
};
/** The part of the circle (cx, cy, r) outside the same circle moved down by `by` (up when negative): a crescent on its top (bottom). */
function crescent(cx, cy, r, by) {
  const t = Math.asin(Math.min(1, Math.abs(by) / 2 / r)), up = by > 0;
  const a0 = up ? Math.PI + t : t, a1 = up ? 2 * Math.PI - t : Math.PI - t;
  return path([...arc(cx, cy, r, a0, a1, 16), ...arc(cx, cy + by, r, a1, a0, 16)]);
}
/** Yellow and black hazard stripes in the box, clipped to it. */
function chevrons(a, x, y, w, h, step) {
  const box = path(rect(x, y, w, h));
  a.flat(box, '#d6a12c');
  for (let s = -h - w; s < w + h; s += step * 2) a.flat(path([[x + s, y + h], [x + s + step, y + h], [x + s + step + h, y], [x + s + h, y]]), '#17181b', { clip: box });
}

/* ---------- capsule G1, in world pixels: its face's middle at (20, 24), 5 px over the floor ---------- */
const GC = [20, 24], GR = 12.8;
/** The hazard lamp in the frame's upper right face, dark asleep: its bezel, its unlit lens, a glint. Its standby pip is the game's. */
function faceLamp(a) {
  const q = -Math.PI / 4, x = GC[0] + Math.cos(q) * 15.6, y = GC[1] + Math.sin(q) * 15.6;
  a.shape(ell(x, y, 1.8), DARK, { shade: .4 });
  a.flat(ell(x, y, 1.4), '#4a515b');
  a.flat(ell(x, y, 1.15), '#2a1a12');
  a.flat(path([...arc(x, y, .95, -2.6, -1.4, 4), ...arc(x - .2, y - .2, .6, -1.4, -2.6, 4)]), 'rgba(255,255,255,.55)');
}
function capsule(a) {
  const [cx, cy] = GC;
  // The eight-sided wall plate and its bolts, the machined collar, the face inside it.
  a.shape(path(oct(cx, cy, 19)), PLATE, { lite: path([[cx - 19, cy - 7.8], [cx - 7.8, cy - 19], [cx + 7.8, cy - 19], [cx + 7.8, cy - 17.6], [cx - 7.2, cy - 17.6], [cx - 17.6, cy - 7.2]]), shade: 1.6, deep: .6 });
  for (const [dx, dy] of [[-17, -7], [17, 7], [-17, 7], [-7, -17], [7, -17], [-7, 17], [7, 17]]) a.flat(path(oct(cx + dx, cy + dy, .45)), '#8a929c');
  a.shape(ell(cx, cy, 14.4), COLLAR, { lite: crescent(cx, cy, 14.4, 1.2), shade: 1.2, deep: .5 });
  a.shape(ell(cx, cy, GR), { base: '#4f5660', lite: '#5d646e', shade: '#3f454e', deep: '#2f343b', spec: '#868e98', ink: INK }, { outer: false });
  for (const r of [10.2, 7.4]) a.line(path(ring(cx, cy, r, 28)), { w: 1.6, c: 'rgba(40,45,52,.55)' });
  for (const [dx, dy] of [[-8.4, -5.4], [8.4, -5.4], [-8.4, 5.4], [8.4, 5.4]]) a.flat(path(oct(cx + dx, cy + dy, .5)), '#6a727c');
  // The recess, by shading only: a shadow along the upper inner edge, a lit lower lip.
  a.flat(crescent(cx, cy, GR, 2.6), '#2a2f36');
  a.flat(crescent(cx, cy, GR, -1.1), '#868e98');
  a.line(path(ring(cx, cy, GR - 1.3, 32)), { w: 1.6, c: 'rgba(28,32,38,.5)' });
  // The cradle shelf the folded spider sits on.
  a.shape(path(rect(cx - 9, cy + 10, 18, 1.6)), SHELF, { lite: path(rect(cx - 9, cy + 10, 18, .5)), shade: .5 });
  // The iris shut: smoked glass blades round a pinhole, turned to their stops.
  const R = GR + .05, h = .6, blades = path(ring(cx, cy, R, 32));
  a.flat(blades, 'rgba(38,50,58,.6)');
  for (let i = 0; i < 8; i++) {
    const q = i / 8 * Math.PI * 2, ca = Math.cos(q), sa = Math.sin(q), tx = cx + ca * h, ty = cy + sa * h, d = Math.sqrt(R * R - h * h) + 1;
    a.line(open([[tx, ty], [tx - sa * d, ty + ca * d]]), { w: 3.6, c: INK });
    a.line(open([[tx + ca * .5, ty + sa * .5], [tx - sa * d + ca * .5, ty + ca * d + sa * .5]]), { w: 2, c: 'rgba(200,220,230,.22)' });
  }
  a.flat(path([[cx - 9.4, cy - 5.6], [cx - 6.8, cy - 9.2], [cx - 5.6, cy - 8.4], [cx - 8.2, cy - 4.8]]), 'rgba(220,236,242,.2)');
  a.line(path(ring(cx, cy, R, 32)), { w: 3.4 });
  // Three status pips under the face, dark asleep; the hazard lamp.
  for (let i = 0; i < 3; i++) a.flat(path(rect(cx - 3.3 + i * 2.2, cy + 15.1, 1.2, .9)), '#2d4a3e');
  faceLamp(a);
  for (const [x0, y0, x1, y1] of [[5.6, -8.6, 8.4, -10.2], [31.4, 8.6, 34, 9.8], [25.6, 16, 28.4, 15.6]]) a.line(open([[x0 + cx - 20, y0 + cy], [x1 + cx - 20, y1 + cy]]), { w: 2, c: 'rgba(205,212,220,.32)' });
  wear(a, cx - 15.6, cy + 6.6, .9);
  wear(a, cx + 12.4, cy - 14.2, .6);
  a.flat(ell(cx, cy + 18.6, 9, 1.6), 'rgba(21,18,15,.4)');
}

/* ---------- stand S1, drawn in cells as the page drew it: the floor at 14, the robot's middle at 31; mapped to world pixels ---------- */
const CX = 31, LEFT = CX - 3, TOP = 6, YB = 14 - .4 - 5.15 - .3, HIP = 14 - .4 - 2.4 - .15;
const X = (x) => (x - LEFT) * 8, Y = (y) => (y - TOP) * 8;
const at = (pts) => pts.map(([x, y]) => [X(x), Y(y)]);
const R8 = (x, y, w, h) => at(rect(x, y, w, h));
const thin = (a, x, y, w, h, m) => a.shape(path(R8(x, y, w, h)), m, { lite: path(R8(x, y, w, Math.min(h, .1))), shade: .4 });
const solid = (a, pts, m) => a.shape(path(at(pts)), m, { shade: 1, deep: .4, lite: path(at(rect(pts[0][0], pts[0][1], 99, .14))) });
const stripes = (a, x, y, w, h, step) => chevrons(a, X(x), Y(y), w * 8, h * 8, step * 8);
const rivet = (a, x, y) => { a.flat(path(R8(x - .06, y - .06, .12, .12)), '#23272d'); a.flat(path(R8(x - .06, y - .06, .06, .04)), '#a3abb4'); };
const cable = (a, x0, y0, x1, y1, w, c) => a.line(open([[X(x0), Y(y0)], [X(x1), Y(y1)]]), { w: w * 8 / .14 / 1.6, c: c || "#16181c" });
function hose(a, p, q, sag, w) {
  const mx = (p[0] + q[0]) / 2, my = (p[1] + q[1]) / 2 + sag;
  a.line(`M${X(p[0])} ${Y(p[1])}Q${X(mx)} ${Y(my)} ${X(q[0])} ${Y(q[1])}`, { w: (w || .1) * 8 / .14 / 1.6, c: "#1b1d21" });
  for (const [x, y] of [p, q]) a.flat(path(R8(x - .07, y - .07, .14, .14)), '#9a6842');
}
function drum(a, c, r) {
  a.shape(ell(X(c[0]), Y(c[1]), r * 8), STEEL_D, { shade: .8, lite: crescent(X(c[0]), Y(c[1]), r * 8, .8) });
  for (let k = .55; k < .95; k += .13) a.line(path(ring(X(c[0]), Y(c[1]), r * 8 * k, 20)), { w: 1.2, c: '#1b1d21' });
  for (let i = 0; i < 3; i++) { const q = 3.6 + i * 2.094; cable(a, c[0], c[1], c[0] + Math.cos(q) * r * .5, c[1] + Math.sin(q) * r * .5, .06, '#a3abb4'); }
  a.flat(ell(X(c[0]), Y(c[1]), r * 8 * .22), DARK.base);
}
function stand(a) {
  const cx = CX;
  a.shape(path(R8(cx - 2.9, 13.6, 6.6, .4)), DARK, { shade: .4 });
  stripes(a, cx - 2.8, 13.66, 4.8, .26, .26);
  hose(a, [cx + 3.5, 13.62], [cx + 3.15, 11.6], .2, .12);
  hose(a, [cx + 3.6, 13.62], [cx + 3.2, 7.4], .3, .1);
  // The mast behind the robot's back, its foot, its service hatch and status panel.
  a.shape(path(R8(cx + 2.3, 6.95, 1, 6.65)), GUNM, { lite: path(R8(cx + 2.3, 6.95, .16, 6.65)), shade: path(R8(cx + 3.1, 6.95, .2, 6.65)), deep: 1 });
  solid(a, [[cx + 1.9, 13.6], [cx + 2.3, 12.6], [cx + 3.3, 12.6], [cx + 3.7, 13.6]], DARK);
  stripes(a, cx + 2.3, 12, 1, .55, .22);
  a.shape(path(R8(cx + 2.42, 10.25, .76, 1.5)), GUNM, { lite: path(R8(cx + 2.42, 10.25, .76, .08)), shade: .4, lines: [path(R8(cx + 2.52, 10.35, .56, 1.3))] });
  for (const [x, y] of [[2.49, 10.32], [3.11, 10.32], [2.49, 11.68], [3.11, 11.68]]) rivet(a, cx + x, y);
  cable(a, cx + 2.98, 10.85, cx + 2.98, 11.15, .07, '#a3abb4');
  thin(a, cx + 2.35, 7.9, .9, 1.2, DARK);
  a.flat(path(R8(cx + 2.47, 8.02, .66, .42)), '#0d2422');
  a.glow(path(R8(cx + 2.53, 8.28, .54 * .35, .08)), '#4fb08a');
  a.flat(path(R8(cx + 2.53, 8.1, .3, .05)), '#24443c');
  ['#1a2a1f', '#3a2d16', '#2a1512'].forEach((c, i) => a.flat(path(R8(cx + 2.51 + i * .22, 8.68, .16, .16)), c));
  wear(a, X(cx + 2.35), Y(12.62), 1.3); wear(a, X(cx + 3.25), Y(9.7), .8);
  a.flat(ell(X(cx + 2.4), Y(7.3), .7), '#5c626a');
  // The console over the robot's head, its drum paying out the cable, its beacon at the tip, dark asleep.
  solid(a, [[cx - 2, 6.7], [cx + 3.3, 6.7], [cx + 3.3, 7.25], [cx - 1.6, 7.25]], GUNM);
  for (let x = cx - 1.5; x < cx + 2.2; x += .8) cable(a, x, 6.78, x + .4, 7.17, .06, '#2a2f37');
  stripes(a, cx - 1.95, 6.75, .5, .45, .14);
  thin(a, cx - 2, 6.4, .5, .3, DARK);
  a.flat(path(R8(cx - 1.92, 6.1, .34, .3)), '#4a3a1c');
  cable(a, cx - 1.92, 6.25, cx - 1.58, 6.25, .04, '#1b1d21');
  thin(a, cx + 1.2, 7.25, 1.05, .55, DARK);
  for (let i = 0; i < 3; i++) a.flat(path(R8(cx + 1.3, 7.35 + i * .12, .4, .054)), '#1e2227');
  drum(a, [cx + .85, 7.62], .36);
  thin(a, cx - .5, 7.25, 1, .3, DARK);
  a.shape(ell(X(cx), Y(7.55), 1.28), GUNM, { shade: .3 });
  cable(a, cx + .6, 7.35, cx + .15, 7.45, .07);
  cable(a, cx - .2, 7.55, cx - .2, YB - .5, .08);
  cable(a, cx + .2, 7.55, cx + .2, YB - .5, .08);
}
/** What holds the robot from in front of it: the yoke's bar and hooks at its shoulders, the waist clamp off the mast, shut. */
function standFront(a) {
  for (const side of [-1, 1]) {
    const x = CX + side * 1.25;
    thin(a, x - .12, YB, .24, 1.05, STEEL);
    thin(a, side < 0 ? x : x - .32, YB + .86, .32, .2, STEEL);
  }
  a.shape(path(R8(CX - 1.35, YB - .32, 2.7, .34)), STEEL, { shade: .5 });
  stripes(a, CX - 1.2, YB - .26, 2.4, .2, .2);
  thin(a, CX - .18, YB - .5, .36, .2, DARK);
  rivet(a, CX - 1.2, YB - .16); rivet(a, CX + 1.2, YB - .16);
  const px = CX + 2.3, py = HIP;
  cable(a, px + .4, py + .9, px - .7, py + .15, .1, '#a9afb5');
  cable(a, px + .4, py + .9, px - .1, py + .55, .16, '#3b4048');
  a.shape(path(R8(px - 1.45, py - .2, 1.45, .4)), STEEL, { shade: .5 });
  stripes(a, px - 1.15, py - .14, .7, .28, .16);
  solid(a, [[px - 1.78, py - .45], [px - 1.4, py - .45], [px - 1.4, py + .45], [px - 1.78, py + .45]], DARK);
  a.shape(ell(X(px), Y(py), 1.6), DARK, { shade: .3 });
}

Subjects.guards = function () {
  out('guard_capsule', Anime.bake({ w: 40, h: 48, draw: capsule }));
  out('guard_stand', Anime.bake({ w: 56, h: 64, draw: stand }));
  out('guard_stand_front', Anime.bake({ w: 56, h: 64, draw: standFront }));
};
})();

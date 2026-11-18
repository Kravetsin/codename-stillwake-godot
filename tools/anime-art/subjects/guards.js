// A vessel's guards' docks, as picked (derelict-enemies): capsule G1 «Зрачок — впадина» on a small room's back wall and stand S1 «Люлька»
// on a big room's floor, ported from the pick pages' draw code (mountG, faceLamp, CAP_G1; STANDS[0]). Asleep: the capsule shut behind its
// smoked iris, its hazard lamp dark (the game blinks its amber pip); the stand without its robot and without what moves.
// `guard_capsule_open` is the capsule awake, its iris open (the game draws the blades opening over it, then the lamp red).
// `guard_capsule`, 40 × 48: its dock's 5 × 5 cells and the row over them, the floor at its foot. `guard_stand`, 56 × 64: its dock's
// 7 × 8 cells, the robot's middle 3 cells from its left. Its moving parts (drum, yoke, hook, waist clamp, beacon) are `creatures/inkStand/*`,
// their pivots in Content/logic/StandInk.cs; the cables from the console to the yoke are the game's lines. Exposes `GuardKit`.
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
/** The capsule; with its iris shut, as asleep, or open (its spider awake: the game draws the iris opening over it). */
function capsule(a, shut) {
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
  if (shut) a.flat(blades, 'rgba(38,50,58,.6)');
  for (let i = 0; i < (shut ? 8 : 0); i++) {
    const q = i / 8 * Math.PI * 2, ca = Math.cos(q), sa = Math.sin(q), tx = cx + ca * h, ty = cy + sa * h, d = Math.sqrt(R * R - h * h) + 1;
    a.line(open([[tx, ty], [tx - sa * d, ty + ca * d]]), { w: 3.6, c: INK });
    a.line(open([[tx + ca * .5, ty + sa * .5], [tx - sa * d + ca * .5, ty + ca * d + sa * .5]]), { w: 2, c: 'rgba(200,220,230,.22)' });
  }
  if (shut) a.flat(path([[cx - 9.4, cy - 5.6], [cx - 6.8, cy - 9.2], [cx - 5.6, cy - 8.4], [cx - 8.2, cy - 4.8]]), 'rgba(220,236,242,.2)');
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
const CX = 31, LEFT = CX - 3, TOP = 6;
/** Drawing in cells, as the pick pages did, mapped to a picture's world pixels: 8 a cell, the cell (ox, oy) at its origin. */
function cells(ox, oy) {
  const X = (x) => (x - ox) * 8, Y = (y) => (y - oy) * 8;
  const at = (pts) => pts.map(([x, y]) => [X(x), Y(y)]);
  const R8 = (x, y, w, h) => at(rect(x, y, w, h));
  const thin = (a, x, y, w, h, m) => a.shape(path(R8(x, y, w, h)), m, { lite: path(R8(x, y, w, Math.min(h, .1))), shade: .4 });
  const solid = (a, pts, m) => a.shape(path(at(pts)), m, { shade: 1, deep: .4, lite: path(at(rect(pts[0][0], pts[0][1], 99, .14))) });
  const stripes = (a, x, y, w, h, step) => chevrons(a, X(x), Y(y), w * 8, h * 8, step * 8);
  const rivet = (a, x, y) => { a.flat(path(R8(x - .06, y - .06, .12, .12)), '#23272d'); a.flat(path(R8(x - .06, y - .06, .06, .04)), '#a3abb4'); };
  const cable = (a, x0, y0, x1, y1, w, c) => a.line(open([[X(x0), Y(y0)], [X(x1), Y(y1)]]), { w: w * 8 / .14 / 1.6, c: c || "#16181c" });
  /** A hose from p to q, sagging by `sag` and bowing sideways by `side`, its couplings copper. */
  function hose(a, p, q, sag, w, side) {
    const mx = (p[0] + q[0]) / 2 + (side || 0), my = (p[1] + q[1]) / 2 + sag;
    a.line(`M${X(p[0])} ${Y(p[1])}Q${X(mx)} ${Y(my)} ${X(q[0])} ${Y(q[1])}`, { w: (w || .1) * 8 / .14 / 1.6, c: "#1b1d21" });
    for (const [x, y] of [p, q]) a.flat(path(R8(x - .07, y - .07, .14, .14)), '#9a6842');
  }
  /** The cable drum, its spokes turned by `ang`. */
  function drum(a, c, r, ang) {
    a.shape(ell(X(c[0]), Y(c[1]), r * 8), STEEL_D, { shade: .8, lite: crescent(X(c[0]), Y(c[1]), r * 8, .8) });
    for (let k = .55; k < .95; k += .13) a.line(path(ring(X(c[0]), Y(c[1]), r * 8 * k, 20)), { w: 1.2, c: '#1b1d21' });
    for (let i = 0; i < 3; i++) { const q = ang + i * 2.094; cable(a, c[0], c[1], c[0] + Math.cos(q) * r * .5, c[1] + Math.sin(q) * r * .5, .06, '#a3abb4'); }
    a.flat(ell(X(c[0]), Y(c[1]), r * 8 * .22), DARK.base);
  }
  return { X, Y, at, R8, thin, solid, stripes, rivet, cable, hose, drum };
}
const { X, Y, R8, thin, solid, stripes, rivet, cable, hose } = cells(LEFT, TOP);
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
  thin(a, cx - .5, 7.25, 1, .3, DARK);
  a.shape(ell(X(cx), Y(7.55), 1.28), GUNM, { shade: .3 });
  cable(a, cx + .6, 7.35, cx + .15, 7.45, .07);
}
/* ---------- the stand's moving parts, each in cells about its pivot: the game turns, lifts and lights them ---------- */
/** The yoke's bar, its pivot the middle of the bar's bottom edge. */
function yoke(a) {
  const k = cells(0, 0);
  a.shape(path(k.R8(-1.35, -.32, 2.7, .34)), STEEL, { shade: .5 });
  k.stripes(a, -1.2, -.26, 2.4, .2, .2);
  k.thin(a, -.18, -.5, .36, .2, DARK);
  k.rivet(a, -1.2, -.16); k.rivet(a, 1.2, -.16);
}
/** The left hook, hanging from its hinge, its foot turned in to the robot: the game mirrors it for the right. */
function hook(a) {
  const k = cells(0, 0);
  k.thin(a, -.12, 0, .24, 1.05, STEEL);
  k.thin(a, 0, .86, .32, .2, STEEL);
}
/** The waist clamp off the mast, shut: its hinge P at the origin. */
function clamp(a) {
  const k = cells(0, 0);
  k.cable(a, .4, .9, -.7, .15, .1, '#a9afb5');
  k.cable(a, .4, .9, -.1, .55, .16, '#3b4048');
  a.shape(path(k.R8(-1.45, -.2, 1.45, .4)), STEEL, { shade: .5 });
  k.stripes(a, -1.15, -.14, .7, .28, .16);
  k.solid(a, [[-1.78, -.45], [-1.4, -.45], [-1.4, .45], [-1.78, .45]], DARK);
  a.shape(ell(0, 0, 1.6), DARK, { shade: .3 });
}
/** The beacon at the console's tip, its housing's top middle at the origin, its lens dark. */
function beacon(a) {
  const k = cells(0, 0);
  k.thin(a, -.25, 0, .5, .3, DARK);
  a.flat(path(k.R8(-.17, -.3, .34, .3)), '#4a3a1c');
  k.cable(a, -.17, -.15, .17, -.15, .04, '#1b1d21');
}

/* ---------- parts posed by the game: each its own picture, its pivot its origin (as warden.js's) ---------- */
const MARGIN = 1.5;
const num = (v) => (Number.isInteger(v) ? String(v) : `${+v.toFixed(3)}f`);
/** A part from its bounds in cells about its pivot; `light`, drawn plain in world pixels (it shines). */
const cellBox = (l, t, r, b, draw, light) => ({ w: (r - l) * 8, h: (b - t) * 8, px: -l * 8, py: -t * 8, light, draw });
/** A part with whole bounds and a whole pivot, as a picture is whole pixels: its drawing keeps its place. */
const snapped = (q) => { const px = Math.ceil(q.px), py = Math.ceil(q.py); return { ...q, px, py, w: Math.ceil(q.w + px - q.px), h: Math.ceil(q.h + py - q.py) }; };
const snapAll = (parts) => Object.fromEntries(Object.entries(parts).map(([n, q]) => [n, snapped(q)]));
/** A part that shines, as it is: plain paths, no water. */
function plain(q) {
  const cv = Anime.canvas((q.w + MARGIN * 2) * 4, (q.h + MARGIN * 2) * 4), c = cv.getContext('2d');
  c.setTransform(4, 0, 0, 4, (q.px + MARGIN) * 4, (q.py + MARGIN) * 4);
  c.lineCap = c.lineJoin = 'round';
  q.draw(c);
  return cv;
}
/** Every part to creatures/<id>/<name>; a part's `after(c)` draws plainly over its finished picture, in world pixels about its pivot. */
function bakeAll(id, parts) {
  for (const [name, q] of Object.entries(parts)) {
    const cv = q.light ? plain(q) : Anime.bake({ w: q.w, h: q.h, margin: MARGIN, shift: [q.px, q.py], draw: q.draw });
    if (q.after) {
      const c = cv.getContext('2d'), s = cv.width / (q.w + MARGIN * 2);
      c.save(); c.setTransform(s, 0, 0, s, (q.px + MARGIN) * s, (q.py + MARGIN) * s); q.after(c); c.restore();
    }
    out(`creatures/${id}/${name}`, cv);
  }
}
/** A PartsDesign field: its parts' sizes and pivots in world pixels, the margin in, as warden.js writes its parts. */
const partsDesign = (field, id, summary, parts) => `    /// <summary>${summary}</summary>
    public static readonly PartsDesign ${field} = new("${id}", new Dictionary<string, Part>
    {
${Object.entries(parts).map(([name, q]) => `        ["${name}"] = new(${q.w + MARGIN * 2}, ${q.h + MARGIN * 2}, ${num(q.px + MARGIN)}, ${num(q.py + MARGIN)}, []),`).join('\n')}
    });`;
/** A generated C# file of CreatureArt fields. */
const generated = (by, body) => `// Generated by tools/anime-art/subjects/${by}: export again, never edit.
using System.Collections.Generic;

namespace Stillwake.Game;

public static partial class CreatureArt
{
${body}
}
`;

window.GuardKit = { pal, mix, INK, STEEL, STEEL_D, DARK, GUNM, RUST, RUST_D, ring, arc, path, open, rect, wear, crescent, chevrons, cells, cellBox, snapAll, bakeAll, partsDesign, generated };

Subjects.guards = function () {
  out('guard_capsule', Anime.bake({ w: 40, h: 48, draw: (a) => capsule(a, true) }));
  out('guard_capsule_open', Anime.bake({ w: 40, h: 48, draw: (a) => capsule(a, false) }));
  out('guard_stand', Anime.bake({ w: 56, h: 64, draw: stand }));
  const parts = snapAll({
    drum: cellBox(-.45, -.45, .45, .45, (a) => cells(0, 0).drum(a, [0, 0], .36, 0)),
    yoke: cellBox(-1.45, -.6, 1.45, .12, yoke),
    hook: cellBox(-.22, -.1, .42, 1.15, hook),
    clamp: cellBox(-1.88, -.55, .55, 1.05, clamp),
    beacon: cellBox(-.35, -.4, .35, .4, beacon),
    beacon_lit: cellBox(-.35, -.4, .35, .4, (c) => {
      c.fillStyle = '#ffae2e'; c.fillRect(-.17 * 8, -.3 * 8, .34 * 8, .3 * 8);
      c.fillStyle = '#1b1d21'; c.fillRect(-.17 * 8, -.17 * 8, .34 * 8, .04 * 8);
    }, true),
  });
  bakeAll('inkStand', parts);
  writeText('../../Content/logic/StandInk.cs', generated('guards.js', partsDesign('InkStand', 'inkStand',
    "The guard stand's moving parts in ink, in <c>art/hd/creatures/inkStand</c>: drum, yoke, the left hook, waist clamp, beacon.", parts)));
};
})();

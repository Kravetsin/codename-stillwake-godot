// The debris net, as picked: «Гамак на стрелах» (sling), two pipe booms with struts and a steel-cable net slung between them
// (docs/decisions/2026-11-03-debris-net.md). Its back stands behind the pieces it holds, its net before them, sagging by load.
// x 0 is the hull's face, x grows into space.
(function () {
'use strict';
const { ell, rr, poly, rng, bake } = Anime;
/** The footprint in world pixels: 2 cells by 6. */
const W = 16, H = 48;
/** What it holds before the rest drift past: Content.Debris.NetHolds (NetShowsTests checks the two agree). */
const HOLDS = 10;
/** World pixels drawn past the footprint on every side: the plates on the hull's face, the ink past the booms' tips. */
const MARGIN = 3;

const PIPE = { base: '#8a919e', shade: '#585d72', deep: '#3c4056', lite: '#b4bbc6', spec: '#e6ebf2' };
const GUN = { base: '#5c6474', shade: '#383c58', deep: '#262a42', lite: '#868fa2', spec: '#dfe7f2' };
const COPPER = { base: '#9a6640', shade: '#643a2c', deep: '#462a28', lite: '#c08458', spec: '#e8bc8c' };
const CABLE = { base: '#666a72', shade: '#42454e', deep: '#2c2e36', lite: '#8a8e98', spec: '#c0c4cc' };
const REBAR = { base: '#5c5652', shade: '#3a3534', deep: '#28242a', lite: '#7e766e', spec: '#a8a098' };

const f2 = (v) => +v.toFixed(2);
const pl = (pts) => 'M' + pts.map((p) => f2(p[0]) + ' ' + f2(p[1])).join('L');
const band = (x0, y0, x1, y1) => poly(x0, y0, x1, y0, x1, y1, x0, y1);
const lerp = (a, b, t) => a + (b - a) * t;
const seg = (fn, n) => { const p = []; for (let i = 0; i <= n; i++) p.push(fn(i / n)); return p; };
const hump = (v) => Math.sin(Math.PI * Math.max(0, Math.min(1, v)));
/** How far the net gives by how many it holds: the pick's seven steps to full (0 to 1.95) spread over ten, so full looks as picked. */
const PICKED = [0, .45, .85, 1.2, 1.5, 1.75, 1.95];
const sagOf = (load) => { const at = (load / HOLDS) * (PICKED.length - 1), i = Math.min(PICKED.length - 2, Math.floor(at)); return lerp(PICKED[i], PICKED[i + 1], at - i); };

function rivet(a, x, y) { a.flat(ell(x, y, .42), '#474c68'); a.flat(ell(x - .1, y - .12, .2), '#c4cad4'); }
/** A plate bolted to the hull: lit top, shaded foot and side. */
function plate(a, x, y, w, h, m) {
  a.shape(rr(x, y, w, h, .4), m, { lite: band(x, y, x + w, y + Math.min(.7, h * .2)), shade: [band(x + w - Math.min(.9, w * .2), y, x + w, y + h), band(x, y + h - Math.min(.9, h * .2), x + w, y + h)] });
}
/** Rust where water sat: a torn edge, a darker heart. The edge alone is scattered. */
function rust(a, x, y, r, seed) {
  const R = rng(seed), pts = [];
  for (let i = 0; i < 9; i++) { const t = (i / 9) * Math.PI * 2, k = r * (.7 + R() * .45); pts.push(x + Math.cos(t) * k * 1.15, y + Math.sin(t) * k * .8); }
  a.flat(poly(pts), '#8a5a3c');
  a.flat(ell(x + r * .1, y + r * .05, r * .55, r * .42), '#7a4428');
  a.flat(ell(x + r * .15, y + r * .1, r * .26, r * .2), '#4c2a1c');
}
/** A few turns of wire round something, across it. */
function wrap(a, x, y, len, ang, n, m) {
  const c = Math.cos(ang), s = Math.sin(ang);
  for (let i = 0; i < n; i++) {
    const o = (i - (n - 1) / 2) * .42, px = x - s * o, py = y + c * o;
    a.tube(`M${f2(px - c * len / 2)} ${f2(py - s * len / 2)}L${f2(px + c * len / 2)} ${f2(py + s * len / 2)}`, .28, m || COPPER, { spec: false });
  }
}

/** The net's surface at sag: u across from the hull to the outer edge, v down from the top boom to the foot one. */
const netAt = (sag) => {
  const L = (v) => [1.3, lerp(4.4, H - 4.4, v)];
  const O = (v) => [W - 1.7 - (.5 + sag * .45) * hump(v), lerp(1.9, H - 1.9, v)];
  return (u, v) => { const l = L(v), o = O(v), k = hump(u) * hump(v); return [lerp(l[0], o[0], u) - k * sag * .5, lerp(l[1], o[1], u) + k * (.5 + sag * .5)]; };
};

/** The booms, their struts and the plates bolting them to the hull: behind what the net holds. */
function back(a) {
  for (const flip of [false, true]) {
    const Y = (y) => f2(flip ? H - y : y);
    plate(a, -1.8, flip ? H - 5.4 : .4, 2.8, 5, GUN);
    rivet(a, -.9, Y(1.6)); rivet(a, -.9, Y(4.2));
    plate(a, -1.8, flip ? H - 11.2 : 7.6, 2.6, 3.6, GUN);
    rivet(a, -.9, Y(9.4));
    a.tube(`M0.4 ${Y(9.4)}L8.6 ${Y(2.2)}`, .9, GUN);
    a.tube(`M0.4 ${Y(2.8)}L${f2(W - 1.4)} ${Y(1.4)}`, 1.4, PIPE);
    a.shape(ell(W - 1.1, flip ? H - 1.4 : 1.4, .95), GUN, { lite: band(W - 2.1, flip ? H - 2.4 : .4, W, flip ? H - 1.9 : .9) });
    rust(a, 8.2, flip ? H - 2.3 : 2.3, .4, flip ? 31 : 32);
    rust(a, .9, flip ? H - 2.8 : 2.8, .45, flip ? 33 : 34);
  }
  for (const y of [H * .35, H * .65]) {
    plate(a, -1.6, y - 1.3, 2.2, 2.6, GUN);
    a.tube(`M0.3 ${f2(y + .6)}Q1.9 ${f2(y + .6)} 1.9 ${f2(y - .7)}`, .55, REBAR);
  }
}

/** The cable net holding <load> pieces: crimped where its cables cross, its edges lashed with copper, shackled at the booms' tips. */
const front = (load) => (a) => {
  const sag = sagOf(load), P = netAt(sag), r = .3, dv = 4.4 / (H - 8);
  const curve = (fn, n) => pl(seg(fn, n));
  for (let c = -r + dv / 2; c < 1; c += dv) {
    const ua = Math.max(0, -c / r), ub = Math.min(1, (1 - c) / r);
    if (ub - ua > .02) a.tube(curve((t) => { const u = lerp(ua, ub, t); return P(u, c + r * u); }, 14), .42, CABLE, { spec: false });
  }
  for (let c = dv / 2; c < 1 + r; c += dv) {
    const ua = Math.max(0, (c - 1) / r), ub = Math.min(1, c / r);
    if (ub - ua > .02) a.tube(curve((t) => { const u = lerp(ua, ub, t); return P(u, c - r * u); }, 14), .42, CABLE, { spec: false });
  }
  // Crimped ferrules where the cables cross.
  for (let c1 = -r + dv / 2; c1 < 1; c1 += dv) for (let c2 = dv / 2; c2 < 1 + r; c2 += dv) {
    const u = (c2 - c1) / (2 * r), v = (c1 + c2) / 2;
    if (u <= .04 || u >= .96 || v <= .02 || v >= .98) continue;
    const [x, y] = P(u, v);
    a.flat(rr(x - .42, y - .34, .84, .68, .2), '#33363e');
    a.flat(band(x - .3, y - .3, x + .16, y - .14), '#9aa0aa');
  }
  // The edge cable, heavier, and the edges lashed to the booms and the hull.
  a.tube(curve((v) => P(1, v), 24), .75, CABLE);
  a.tube(curve((v) => P(0, v), 24), .5, CABLE, { spec: false });
  a.tube(curve((u) => P(u, 0), 10), .5, CABLE, { spec: false });
  a.tube(curve((u) => P(u, 1), 10), .5, CABLE, { spec: false });
  for (const u of [.25, .55, .85]) {
    const t = P(u, 0), b = P(u, 1);
    a.tube(`M${f2(t[0])} ${f2(t[1])}L${f2(t[0] + .2)} ${f2(lerp(2.8, 1.4, u) - .1)}`, .26, COPPER, { spec: false });
    a.tube(`M${f2(b[0])} ${f2(b[1])}L${f2(b[0] + .2)} ${f2(H - lerp(2.8, 1.4, u) + .1)}`, .26, COPPER, { spec: false });
  }
  for (const y of [H * .35, H * .65]) {
    const p = P(0, (y - 4.4) / (H - 8.8));
    a.tube(`M${f2(p[0])} ${f2(p[1])}L1.8 ${f2(y - .6)}`, .28, COPPER, { spec: false });
    wrap(a, 1.6, y - .4, 1.4, 0, 2);
  }
  // Shackles at the boom tips.
  for (const y of [1.6, H - 1.6]) a.tube(ell(W - 1.6, y, .75, .6), .26, GUN, { spec: false });
};

/** Where each piece settles, in the order caught, middle and foot first, then into the gaps: ten in 2 by 6 cells overlap, as junk does. */
const HEAP = [
  { x: 8.4, y: 27.5, rot: .12 },
  { x: 8.2, y: 41.8, rot: -.08 },
  { x: 8.6, y: 17.6, rot: .08 },
  { x: 8.6, y: 9.6, rot: 1.52 },
  { x: 8.8, y: 35.0, rot: -.15 },
  { x: 9.2, y: 22.5, rot: .5 },
  { x: 7.6, y: 31.4, rot: .85 },
  { x: 9.0, y: 13.6, rot: -.35 },
  { x: 7.8, y: 38.4, rot: .3 },
  { x: 9.4, y: 6.2, rot: -1.2 },
];
/** Piece <k>'s place in a net holding <load>: moved with the net's give. */
const placeOf = (k, load) => { const h = HEAP[k], v = h.y / H, sag = sagOf(load); return [h.x - sag * .9 * hump(v), h.y + sag * .35 * hump(v), h.rot]; };

/** A part of <canvas>, its pixels from (x, y), w by h. */
function crop(canvas, x, y, w, h) {
  const c = document.createElement('canvas'); c.width = w; c.height = h;
  c.getContext('2d').drawImage(canvas, x, y, w, h, 0, 0, w, h);
  return c;
}

Subjects.net = function () {
  const px = 4, m = MARGIN * px;
  // One bake of the back: inside the footprint the grid's machine picture, left of it the plates on the hull's face, drawn over the wall.
  const whole = bake({ w: W, h: H, margin: MARGIN, draw: back });
  out('machine_net', crop(whole, m, m, W * px, H * px));
  out('net_mount', crop(whole, 0, m, m, H * px));
  for (let n = 0; n <= HOLDS; n++) out(`net_front_${n}`, bake({ w: W, h: H, margin: MARGIN, draw: front(n) }));
  const places = [];
  for (let n = 0; n <= HOLDS; n++) places.push('        [' + Array.from({ length: n }, (_, k) => '(' + placeOf(k, n).map((v) => v.toFixed(2) + 'f').join(', ') + ')').join(', ') + '],');
  writeText('../../features/debris/logic/NetInk.cs', `// Generated by tools/anime-art/subjects/net.js: export again, never edit.
namespace Stillwake.Game.Features.Debris;

/// <summary>The picked debris net's numbers (sling): its pictures' margin and where the pieces it holds lie.</summary>
public static class NetInk
{
    /// <summary>The world's pixels <c>net_front_*</c> reach past the footprint on every side, and <c>net_mount</c> left of it.</summary>
    public const int Margin = ${MARGIN};

    /// <summary>
    /// Where each piece lies in a net holding as many as the row's index, in the order caught: the middle of its picture in the
    /// world's pixels from the footprint's top-left, and its turn in radians, the net sagging by load.
    /// </summary>
    public static readonly (float X, float Y, float Turn)[][] Places =
    [
${places.join('\n')}
    ];
}
`);
};
})();

// The salvage drone, as picked: «Сетчатая рама» (netter), an open pipe frame round a small core with an eye, nozzles at its
// corners, an air bottle on top and a cargo net slung under it that closes round a piece (docs/decisions/2026-11-03-salvage-drones.md).
// Ported whole from the pick page (salvage-art/salvage.js): its flying pictures by grip, the net's near side over the held piece,
// docked, its eye and lamp, three puffs of air; the item. Writes features/logistics/logic/SalvageInk.cs, the numbers the game moves it by.
(function () {
'use strict';
const { ell, rr, poly, rng, bake } = Anime;

// The machines' materials (machines.js AnimeKit), the hero's air bottle (hero.js), cord for the net.
const STEEL = { base: '#a7afbc', shade: '#6a708c', deep: '#474c68', lite: '#d4dae2', spec: '#ffffff', rim: '#86d2e8' };
const GUN = { base: '#5c6474', shade: '#383c58', deep: '#262a42', lite: '#868fa2', spec: '#dfe7f2', rim: '#5fb2d0' };
const DARK = { base: '#2a2d3c', shade: '#1a1c2a', deep: '#12131e', lite: '#40445a', spec: '#a8b4c6', rim: '#4f9cbc' };
const PIPE = { base: '#7f8796', shade: '#555b70', deep: '#3a3e50', lite: '#a9b1be', spec: '#dfe7f2' };
const BOTTLE = { base: '#2f6fa4', shade: '#1f4a78', deep: '#173a60', lite: '#5a9cca', spec: '#a4d0ea', rim: '#5a9cca' };
const CABLE = { base: '#7a7e86', shade: '#4c4f58', deep: '#34363e', lite: '#9a9ea8', spec: '#c8ccd2' };
const CABLE_FAR = { base: '#44464e', shade: '#30323a', deep: '#24252c', lite: '#54565e', spec: '#6a6c74' };

/** World pixels drawn past every picture's box: nozzles and the net reach past it. */
const MARGIN = 2;

const f2 = (v) => +v.toFixed(2);
const pl = (pts) => 'M' + pts.map((p) => f2(p[0]) + ' ' + f2(p[1])).join('L');
const band = (x0, y0, x1, y1) => poly(x0, y0, x1, y0, x1, y1, x0, y1);
const lerp = (a, b, t) => a + (b - a) * t;
/** A body's polyline: its points in body pixels, moved to (X, Y). */
const L = (X, Y, ...pts) => pl(pts.map((p) => [X + p[0], Y + p[1]]));
const B = (X, Y, x0, y0, x1, y1) => band(X + x0, Y + y0, X + x1, Y + y1);
/** Rust where water sat: a torn edge, a darker heart. The edge alone is scattered. */
function rust(a, x, y, r, seed) {
  const R = rng(seed), pts = [];
  for (let i = 0; i < 9; i++) { const t = (i / 9) * Math.PI * 2, k = r * (.7 + R() * .45); pts.push(x + Math.cos(t) * k * 1.15, y + Math.sin(t) * k * .8); }
  a.flat(poly(pts), '#8a5a3c');
  a.flat(ell(x + r * .1, y + r * .05, r * .55, r * .42), '#7a4428');
  a.flat(ell(x + r * .15, y + r * .1, r * .26, r * .2), '#4c2a1c');
}
/** A nozzle's bell, its mouth at (x, y) facing (dx, dy): a short flare, dark in the mouth. */
function bell(a, x, y, dx, dy, s) {
  s = s || 1;
  const px = -dy, py = dx, len = .85 * s, b = .42 * s, m = .72 * s, bx = x - dx * len, by = y - dy * len;
  a.shape(poly(bx + px * b, by + py * b, x + px * m, y + py * m, x - px * m, y - py * m, bx - px * b, by - py * b), GUN, { outer: false, shade: poly(bx, by, x, y, x - px * m, y - py * m, bx - px * b, by - py * b) });
  const k = .62 * m, d = .3 * s;
  a.flat(poly(x + px * k - dx * d, y + py * k - dy * d, x + px * k, y + py * k, x - px * k, y - py * k, x - px * k - dx * d, y - py * k - dy * d), DARK.deep);
}
/** The status lamp's housing, dark, where the lit lamp lands. */
const lampWell = (a, x, y) => a.shape(rr(x - 1.6, y - .75, 3.2, 1.5, .35), GUN, { lite: band(x - 1.6, y - .75, x + 1.6, y - .4), outer: false });
/** A cord mesh in a sack whose point (u across, v down, both 0..1) is at(u, v): two sets of diagonals, n a side. */
function mesh(a, at, n, w, m) {
  const run = (pts) => { if (pts.length > 1) a.tube(pl(pts), w, m, { spec: false }); };
  for (let i = -n + 1; i < n; i++) {
    const c = i / n, u0 = Math.max(0, c), u1 = Math.min(1, 1 + c), p = [];
    if (u1 - u0 < .06) continue;
    for (let k = 0; k <= 8; k++) { const u = lerp(u0, u1, k / 8); p.push(at(u, u - c)); }
    run(p);
  }
  for (let i = 1; i < 2 * n; i++) {
    const c = i / n, u0 = Math.max(0, c - 1), u1 = Math.min(1, c), p = [];
    if (u1 - u0 < .06) continue;
    for (let k = 0; k <= 8; k++) { const u = lerp(u0, u1, k / 8); p.push(at(u, c - u)); }
    run(p);
  }
}
const edgeOf = (at, fn, n) => { const p = []; for (let k = 0; k <= n; k++) p.push(fn(k / n)); return pl(p.map((q) => at(q[0], q[1]))); };

/**
 * The drone from its parts, in body pixels: its body (drawn the same flying and docked), what it adds flying by layer
 * (over the body, over the held piece), and what docked. Points it gives the game are in its pictures' pixels.
 */
function make(d) {
  const F = d.fly, D = d.dock;
  const at = (o, p) => [o.x + p[0], o.y + p[1]];
  return {
    fly: { w: F.w, h: F.h }, dock: { w: D.w, h: D.h },
    flyA: at(F, d.anchor), flyEye: at(F, d.eye), flyLamp: at(F, d.lamp),
    dockA: at(D, d.anchor), dockEye: at(D, d.eye), dockFeet: D.feet,
    nozzles: d.noz.map((n) => ({ x: F.x + n[0], y: F.y + n[1], dx: n[2], dy: n[3], s: n[4] || 1, g: Math.abs(n[2]) > Math.abs(n[3]) ? (n[2] < 0 ? 'L' : 'R') : (n[3] > 0 ? 'D' : 'U') })),
    /** The held piece's middle in the flying picture, for a piece pw by ph. */
    hold: (pw, ph) => at(F, d.hold(pw, ph)),
    /** The flying picture: body, and over it the net by grip ('open', 'mid', 'shut'). */
    drawFly(a, o) { d.flyParts(a, F.x, F.y, { ...o, layer: 'under' }); d.body(a, F.x, F.y, o); d.flyParts(a, F.x, F.y, { ...o, layer: 'over' }); },
    /** What lies over the held piece: the net's near side. */
    drawFront(a, o) { d.flyParts(a, F.x, F.y, { ...o, layer: 'front' }); },
    drawDock(a) { d.dockParts(a, D.x, D.y, 'under'); d.body(a, D.x, D.y, { dock: true }); d.dockParts(a, D.x, D.y, 'over'); },
  };
}

// ---------------- The netter: an open pipe frame round a small core, a cargo net slung from its foot ----------------
const NET_NOZ = [[-.95, .75, -1, 0], [-.95, 8.65, -1, 0], [13.95, .75, 1, 0], [13.95, 8.65, 1, 0], [.75, 10.4, 0, 1], [12.25, 10.4, 0, 1], [.75, -.95, 0, -1], [12.25, -.95, 0, -1]];
const netOpen = (X, Y) => (u, v) => [lerp(X + .9 + v * 3, X + 12.1 - v * 3, u), Y + 9.5 + v * 5.4 * (.55 + .45 * Math.sin(Math.PI * u))];
const netShut = (X, Y, dx, dy) => (u, v) => {
  const w = 11.4 + 7.6 * Math.sin(Math.PI * Math.min(1, v * 1.2)) - (v > .85 ? (v - .85) * 14 : 0);
  return [X + 6.5 + (u - .5) * w + (dx || 0), Y + 9.5 + v * 14.6 + Math.sin(Math.PI * u) * 1.1 * v + (dy || 0)];
};
const netter = make({
  anchor: [6.5, 4.6], eye: [8, 3.1], lamp: [5.1, 1.95],
  noz: NET_NOZ,
  fly: { w: 21, h: 30, x: 4, y: 2.2 }, dock: { w: 14, h: 14, x: .5, y: 3, feet: 13.4 },
  hold: (pw, ph) => [6.5, 24.6 - ph / 2],
  body(a, X, Y) {
    for (const n of NET_NOZ) bell(a, X + n[0], Y + n[1], n[2], n[3]);
    for (const s of [[[3.8, 3], [1, 1]], [[9.2, 3], [12, 1]], [[3.8, 6.6], [1, 8.4]], [[9.2, 6.6], [12, 8.4]]]) a.tube(L(X, Y, ...s), .7, GUN);
    a.tube(rr(X + .8, Y + .8, 11.4, 7.8, .5), 1.1, PIPE);
    a.shape(rr(X + 2.4, Y - 1.3, 8.2, 2, .95), BOTTLE, { lite: B(X, Y, 3, -1.3, 10, -.8), spec: B(X, Y, 3.6, -1.1, 5.2, -.85), shade: B(X, Y, 2.4, .1, 10.6, .7) });
    for (const sx of [3.6, 8.6]) a.shape(rr(X + sx, Y - 1.5, .8, 2.6, .15), GUN, { outer: false, lite: B(X, Y, sx, -1.5, sx + .3, 1.1) });
    a.shape(rr(X + 3.2, Y + 2, 6.8, 5.4, 2), STEEL, {
      lite: B(X, Y, 3.9, 2, 8.6, 2.9), spec: B(X, Y, 4.4, 2.2, 6, 2.55), shade: B(X, Y, 3.2, 5.8, 10, 7.4), lines: [L(X, Y, [3.6, 5.2], [9.6, 5.2])] });
    a.shape(rr(X + 6.8, Y + 2.7, 2.8, 2.3, .4), DARK, { outer: false });
    lampWell(a, X + 5.1, Y + 1.95);
    for (const [cx, cy] of [[-.2, -.2], [11.3, -.2], [-.2, 7.7], [11.3, 7.7]]) {
      a.shape(rr(X + cx, Y + cy, 1.9, 1.9, .4), GUN, { lite: B(X, Y, cx, cy, cx + 1.9, cy + .5), shade: B(X, Y, cx + 1.3, cy, cx + 1.9, cy + 1.9) });
    }
    rust(a, X + .8, Y + 5.1, .4, 31);
    rust(a, X + 12.2, Y + 3.2, .34, 32);
    a.line(L(X, Y, [4.2, 6.3], [4.9, 6.9]), { w: .9 });
  },
  flyParts(a, X, Y, o) {
    if (o.layer === 'under') return;
    if (o.grip !== 'shut') {
      if (o.layer !== 'over') return;
      const at = netOpen(X, Y);
      mesh(a, at, 3, .32, CABLE);
      a.tube(edgeOf(at, (t) => [0, t], 6), .36, CABLE, { spec: false });
      a.tube(edgeOf(at, (t) => [1, t], 6), .36, CABLE, { spec: false });
      a.tube(edgeOf(at, (t) => [t, 1], 8), .36, CABLE, { spec: false });
      a.tube(edgeOf(at, (t) => [t, 0], 8), .42, CABLE, { spec: false });
      return;
    }
    if (o.layer === 'over') {
      const far = netShut(X, Y, .5, .3);
      mesh(a, far, 5, .3, CABLE_FAR);
      return;
    }
    const at = netShut(X, Y);
    mesh(a, at, 5, .32, CABLE);
    for (const u of [0, 1]) a.tube(edgeOf(at, (t) => [u, t], 10), .38, CABLE, { spec: false });
    a.tube(edgeOf(at, (t) => [t, 1], 10), .38, CABLE, { spec: false });
    a.tube(edgeOf(at, (t) => [t, 0], 8), .48, CABLE, { spec: false });
    for (const [x, y] of [at(0, 0), at(1, 0)]) a.shape(ell(x, y, .55), GUN, { outer: false });
  },
  dockParts(a, X, Y, layer) {
    if (layer !== 'over') return;
    a.shape(rr(X + 2.4, Y + 8.6, 8.2, 1.4, .65), CABLE, { lite: B(X, Y, 2.8, 8.6, 10.2, 9), lines: [L(X, Y, [4, 8.7], [4.6, 9.9]), L(X, Y, [6.2, 8.7], [6.8, 9.9]), L(X, Y, [8.4, 8.7], [9, 9.9])] });
  },
});

/** The eye as the ordinary drone's (zfittings.js eye); the lamp as its lampOf. */
const eye = (x, y, c) => (a) => { a.flat(rr(x - .5, y - .5, 3, 3, .5), c ? c[0] : '#2ca4d8'); a.flat(rr(x, y, 1.6, 1.6, .3), c ? c[1] : '#a8ecff'); };
const lamp = (x, y, c) => (a) => { a.flat(rr(x - 1.4, y - .6, 2.8, 1.2, .3), c); };

/** A puff of air off a nozzle, three shapes; pale greys, no light of its own. */
const PUFF = 6, PUFF_MARGIN = .5;
const puffs = [
  (a) => { a.flat(ell(3, 3.1, 1.7, 1.35), '#c9cdd4'); a.flat(ell(2.6, 2.7, 1.05, .8), '#e6e8ec'); a.flat(ell(3.6, 3.9, 1, .45), '#a1a6b0'); a.flat(ell(4.3, 2.4, .5, .4), '#d6d9de'); },
  (a) => { a.flat(ell(2.3, 3.2, 1.2, 1), '#c9cdd4'); a.flat(ell(3.9, 2.8, 1.3, 1.05), '#cfd2d8'); a.flat(ell(3.6, 2.4, .7, .5), '#eceef1'); a.flat(ell(2.8, 3.9, 1.1, .4), '#a1a6b0'); },
  (a) => { a.flat(ell(2.2, 2.6, .8, .65), '#c4c8cf'); a.flat(ell(3.8, 3.5, 1, .7), '#c9cdd4'); a.flat(ell(3.4, 2.1, .55, .45), '#e2e4e8'); a.flat(ell(4.6, 4.2, .5, .35), '#a7acb5'); a.flat(ell(1.6, 3.6, .45, .3), '#b4b8c0'); },
];

Subjects.salvage = function () {
  const c = netter, fly = (draw) => bake({ w: c.fly.w, h: c.fly.h, margin: MARGIN, draw }), dock = (draw) => bake({ w: c.dock.w, h: c.dock.h, margin: MARGIN, draw });
  // The netter's net is open until it shuts round the piece: its 'mid' pose is the open net, as picked.
  out('salvage_fly_open', fly((a) => c.drawFly(a, { grip: 'open' })));
  out('salvage_fly_shut', fly((a) => c.drawFly(a, { grip: 'shut' })));
  out('salvage_front', fly((a) => c.drawFront(a, { grip: 'shut' })));
  out('salvage_dock', dock((a) => c.drawDock(a)));
  out('salvage_eye', fly(eye(c.flyEye[0], c.flyEye[1])));
  out('salvage_dock_eye', dock(eye(c.dockEye[0], c.dockEye[1])));
  out('salvage_lamp_work', fly(lamp(c.flyLamp[0], c.flyLamp[1], '#a8ecff')));
  out('salvage_lamp_low', fly(lamp(c.flyLamp[0], c.flyLamp[1], '#ff5040')));
  puffs.forEach((draw, i) => out(`salvage_puff_${i}`, bake({ w: PUFF, h: PUFF, margin: PUFF_MARGIN, draw })));
  // The item: docked, its eye lit; flat, its eye dark, as the drone's (items.js drone_empty).
  const item = (colours) => bake({ w: 16, h: 16, light: [-0.6, -0.8], shift: [1, .5], draw: (a) => { c.drawDock(a); eye(c.dockEye[0], c.dockEye[1], colours)(a); } });
  out('items/salvageDrone', item(null));
  out('items/salvageDrone_empty', item(['#1e3442', '#2c4656']));

  const n2 = (v) => v.toFixed(2) + 'f';
  const nozzles = c.nozzles.map((n) => `        (${n2(n.x)}, ${n2(n.y)}, ${n.dx}, ${n.dy}, ${n2(n.s)}, '${n.g}'),`).join('\n');
  writeText('../../features/logistics/logic/SalvageInk.cs', `// Generated by tools/anime-art/subjects/salvage.js: export again, never edit.
namespace Stillwake.Game.Features.Logistics;

/// <summary>
/// The picked salvage drone's numbers (netter): its pictures' size and margin, and its points in them, in the world's pixels from
/// a picture's box's top-left (the margin outside it): <c>salvage_fly_*</c>, <c>salvage_front</c>, <c>salvage_eye</c> and
/// <c>salvage_lamp_*</c> flying; <c>salvage_dock</c> and <c>salvage_dock_eye</c> docked; <c>salvage_puff_*</c>.
/// </summary>
public static class SalvageInk
{
    /// <summary>The world's pixels every picture but a puff reaches past its box on every side.</summary>
    public const int Margin = ${MARGIN};

    /// <summary>The flying pictures' box, and the body's middle the client moves.</summary>
    public const int FlyWidth = ${c.fly.w}, FlyHeight = ${c.fly.h};
    public const float AnchorX = ${n2(c.flyA[0])}, AnchorY = ${n2(c.flyA[1])};

    /// <summary>The middle of its status lamp, flying.</summary>
    public const float LampX = ${n2(c.flyLamp[0])}, LampY = ${n2(c.flyLamp[1])};

    /// <summary>Where a held piece hangs, flying: its middle across, and its foot, the net's bottom.</summary>
    public const float HoldX = ${n2(c.hold(0, 0)[0])}, HoldBottom = ${n2(c.hold(0, 0)[1])};

    /// <summary>The docked picture's box, the body's middle in it and the feet's line, which stands on a pad's arm.</summary>
    public const int DockWidth = ${c.dock.w}, DockHeight = ${c.dock.h};
    public const float DockAnchorX = ${n2(c.dockA[0])}, DockAnchorY = ${n2(c.dockA[1])}, DockFeet = ${n2(c.dockFeet)};

    /// <summary>A puff's picture: its box, and the margin round it.</summary>
    public const float PuffSize = ${n2(PUFF)}, PuffMargin = ${n2(PUFF_MARGIN)};
    public const int Puffs = ${puffs.length};

    /// <summary>
    /// Its nozzles, flying: each mouth's point, the way it blows, its size, and its group by that way (L, R, U, D), which
    /// fires together.
    /// </summary>
    public static readonly (float X, float Y, int Dx, int Dy, float S, char Group)[] Nozzles =
    [
${nozzles}
    ];
}
`);
};
})();

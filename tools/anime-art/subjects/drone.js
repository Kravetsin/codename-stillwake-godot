// The light drone (drone-swarm), the user's pick «Пепел в латах»: an iron core, its body a cloud of soot, four scrap plates riding
// it as armour. The pick page's own pen and paint, its small level (makeAsh's build(0)), drawn straight (no finish), 4 hi-res pixels
// a world pixel as there. drone_core: the core, clean, one picture (it is round), CoreBox world px, its middle at the box's.
// drone_plates: a row per plate, its face at Turns angles then its rusty back (the pick's flipY by -1) at as many, PlateBox world px
// each, its middle at the box's; the game squashes one across its long axis as it turns over. The soot, where it all flies, the
// lens, the field's threads and the glow are the game's (DroneSwarm.cs, LightDrone.cs), since they move or light. items/lightDrone:
// the drone idle, its lamp on.
(function () {
'use strict';
const Turns = 8, CoreBox = 4, PlateBox = 6, K = 4;
const INK = '#16171b', LAMP = [255, 212, 150], EMBER = [255, 138, 58];
const STEEL = ['#8b9199', '#61666e', '#3b3f46'], DARK = ['#5b6067', '#42464c', '#2a2d32'], ORANGE = ['#a36c3b', '#7a512f', '#4c331f'];
const OLIVE = ['#6b745f', '#4e5546', '#30352c'], BRONZE = ['#8d7a5a', '#685a43', '#41382a'], RUSTY = ['#6e5646', '#4f3e33', '#30261f'];
const css = (c, a = 1) => `rgba(${c[0] | 0},${c[1] | 0},${c[2] | 0},${a})`;
const pale = (c, k) => [c[0] + (255 - c[0]) * k, c[1] + (255 - c[1]) * k, c[2] + (255 - c[2]) * k];
function rng(seed) { let s = (seed * 2654435761) >>> 0 || 1; return () => { s ^= s << 13; s >>>= 0; s ^= s >>> 17; s ^= s << 5; s >>>= 0; return s / 4294967296; }; }
function ell(rx, ry, n) { const p = []; for (let i = 0; i < n; i++) { const a = i / n * Math.PI * 2; p.push([Math.cos(a) * rx, Math.sin(a) * ry]); } return p; }
const rot = (x, y, a) => [x * Math.cos(a) - y * Math.sin(a), x * Math.sin(a) + y * Math.cos(a)];
// A polygon's edges sampled densely, so the pen keeps its corners sharp.
function polyPts(v) { const p = []; for (let i = 0; i < v.length; i++) { const a = v[i], b = v[(i + 1) % v.length]; for (const t of [0, .2, .5, .8]) p.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]); } return p; }

// A clean shape: its points as given, the pen's pressure even all round, no gaps.
function smooth(pts) { const s = mk(pts, 1, 0); s.w = s.w.map(() => 1); for (const g of s.segs) g[6] = 1; return s; }
// A hand-drawn shape: jittered points and the pen's pressure per segment.
function mk(pts, seed, jit = 0.6, closed = true) {
  const r = rng(seed);
  const p = pts.map(q => [q[0] + (r() - .5) * jit, q[1] + (r() - .5) * jit]);
  const w = [], ph = r() * 6.28;
  for (let i = 0; i < p.length; i++) { let v = .8 + .4 * Math.sin(i * .7 + ph) + (r() - .5) * .35; if (r() < .035) v = 0; w.push(Math.max(0, v)); }
  const s = { p, w, closed };
  let cx = 0, cy = 0; for (const q of p) { cx += q[0]; cy += q[1]; } s.cx = cx / p.length; s.cy = cy / p.length;
  const n = p.length, segs = [];
  if (closed) for (let i = 0; i < n; i++) { const a = p[(i - 1 + n) % n], b = p[i], d = p[(i + 1) % n]; segs.push([(a[0] + b[0]) / 2, (a[1] + b[1]) / 2, b[0], b[1], (b[0] + d[0]) / 2, (b[1] + d[1]) / 2, w[i]]); }
  else for (let i = 0; i < n - 1; i++) { const a = p[i], b = p[i + 1]; segs.push([a[0], a[1], (a[0] + b[0]) / 2, (a[1] + b[1]) / 2, b[0], b[1], w[i]]); }
  s.segs = segs;
  return s;
}
function trace(c, s) {
  const p = s.p, n = p.length; c.beginPath();
  if (!s.closed) { c.moveTo(p[0][0], p[0][1]); for (let i = 1; i < n; i++) c.lineTo(p[i][0], p[i][1]); return; }
  const g = s.segs; c.moveTo(g[0][0], g[0][1]);
  for (const q of g) c.quadraticCurveTo(q[2], q[3], q[4], q[5]);
  c.closePath();
}
// Hard tone steps lit from straight above: the shade a world-space crescent at the bottom, whichever way the shape turns.
function paint(c, s, ang, col, k1 = 1.6, k2 = 5, grainA = .55) {
  c.save(); c.rotate(ang); trace(c, s); c.clip(); c.rotate(-ang);
  c.fillStyle = col[2]; c.fillRect(-90, -90, 180, 180);
  c.save(); c.translate(0, -k1); c.rotate(ang); c.fillStyle = col[1]; trace(c, s); c.fill(); c.restore();
  c.save(); c.translate(0, -k2); c.rotate(ang); c.fillStyle = col[0]; trace(c, s); c.fill(); c.restore();
  if (c.grain) { c.globalAlpha = grainA; c.fillStyle = c.grain; c.fillRect(-90, -90, 180, 180); }
  c.restore();
}
// A wandering pen line, heavier round the object; a thin lit edge where the outline faces up.
function ink(c, s, ang, lw, col = INK, lit = 'rgba(214,220,226,.5)') {
  const cs = Math.cos(ang), sn = Math.sin(ang);
  c.save(); c.rotate(ang); c.lineCap = 'round'; c.lineJoin = 'round';
  if (lit && s.closed) {
    c.strokeStyle = lit; c.lineWidth = 1;
    for (const g of s.segs) {
      const nx = g[2] - s.cx, ny = g[3] - s.cy, L = Math.hypot(nx, ny) || 1;
      if ((nx * sn + ny * cs) / L < -.42) {
        const ix = -nx / L * 1.4, iy = -ny / L * 1.4;
        c.beginPath(); c.moveTo(g[0] + ix, g[1] + iy); c.quadraticCurveTo(g[2] + ix, g[3] + iy, g[4] + ix, g[5] + iy); c.stroke();
      }
    }
  }
  c.strokeStyle = col;
  for (const g of s.segs) {
    if (g[6] <= .05) continue;
    c.lineWidth = lw * g[6]; c.beginPath(); c.moveTo(g[0], g[1]); c.quadraticCurveTo(g[2], g[3], g[4], g[5]); c.stroke();
  }
  c.restore();
}
function blobAt(r, x, y, sz) {
  const n = 9, p = [];
  for (let i = 0; i < n; i++) { const a = i / n * 6.283 + r() * .4, rr = sz * (.55 + .6 * r()); p.push([x + Math.cos(a) * rr, y + Math.sin(a) * rr * .8]); }
  return { s: mk(p, (r() * 1e6) | 0, .3), x, y };
}
function rust(c, b) {
  c.fillStyle = 'rgba(118,66,36,.85)'; trace(c, b.s); c.fill();
  c.save(); c.translate(b.x, b.y); c.scale(.45, .45); c.translate(-b.x, -b.y);
  c.fillStyle = 'rgba(70,36,20,.9)'; trace(c, b.s); c.fill(); c.restore();
}
function halo(c, x, y, r, rgb, a) {
  if (a <= .005) return;
  c.save(); c.globalCompositeOperation = 'lighter';
  const g = c.createRadialGradient(x, y, 0, x, y, r);
  g.addColorStop(0, css(rgb, a)); g.addColorStop(1, css(rgb, 0));
  c.fillStyle = g; c.beginPath(); c.arc(x, y, r, 0, 6.283); c.fill(); c.restore();
}
function grainOf(c) {
  const g = document.createElement('canvas'); g.width = g.height = 64;
  const x = g.getContext('2d'), r = rng(7);
  for (let i = 0; i < 420; i++) { const d = r() < .62; x.fillStyle = d ? `rgba(0,0,0,${.06 + r() * .16})` : `rgba(255,255,255,${.03 + r() * .07})`; x.fillRect(r() * 64, r() * 64, 1 + (r() < .2), 1); }
  x.strokeStyle = 'rgba(0,0,0,.1)'; x.lineWidth = .8;
  for (let i = 0; i < 4; i++) { x.beginPath(); x.arc(r() * 64, r() * 64, 6 + r() * 14, r() * 6, r() * 6 + 1.6); x.stroke(); }
  return c.createPattern(g, 'repeat');
}
function shardShape(r, sz) {
  const n = 4 + ((r() * 3) | 0), v = [];
  for (let i = 0; i < n; i++) { const a = (i + (r() - .5) * .5) / n * 6.283, rr = sz * (.6 + r() * .45); v.push([Math.cos(a) * rr * 1.3, Math.sin(a) * rr * .8]); }
  return mk(polyPts(v), (r() * 1e6) | 0, .2);
}
function piece(c, x, y, a, sh, col, lw = 1.1, rb = null) {
  c.save(); c.translate(x, y);
  paint(c, sh, a, col, .5, 1.6, .45);
  if (rb) { c.save(); c.rotate(a); trace(c, sh); c.clip(); rust(c, rb); c.restore(); }
  ink(c, sh, a, lw);
  c.restore();
}

// A plate seen turned about its long axis: k = 1 face on, -1 its back (the pick's flipY).
function flipY(sh, k) { return { p: sh.p, segs: sh.segs.map(g => [g[0], g[1] * k, g[2], g[3] * k, g[4], g[5] * k, g[6]]), closed: true, cx: sh.cx, cy: sh.cy * k }; }

// The pick's small ash drone, drawn from its seeds in its order: the core is its swarm's, the plates' phases and paces Meeting.cs's
// PlateSeeds, the soot its dustSet (seed 530), as Meeting.cs draws it.
const ash = (() => {
  const r = rng(501), pal = [STEEL, ORANGE, OLIVE, BRONZE], sh = [];
  for (let i = 0; i < 4; i++) {
    const s = shardShape(r, 5.6 + r() * 1.2);
    s.col = pal[i]; s.rb = r() < .6 ? blobAt(r, (r() - .5) * 4, (r() - .5) * 2, 1.5) : null; s.ph = r() * 6.283; s.rr = r(); sh.push(s);
  }
  const d = rng(530), P = [];
  for (let i = 0; i < 170; i++) P.push({ ph: d() * 6.283, r1: d(), r2: d(), r3: d(), sz: .9 + d() * .9, hot: d() < .07, tn: d() });
  return { sh, P, core: smooth(ell(5.4, 5.4, 48)) };
})();

// The core is alien and clean (drone-meeting): no rust and no cracks, its outline one even line; only its plates are scrap.
function core(c) {
  paint(c, ash.core, 0, DARK, .8, 2.6);
  ink(c, ash.core, 0, 1.7);
}
function plate(c, x, y, a, s, k) {
  const kk = Math.abs(k) < .08 ? (k < 0 ? -.08 : .08) : k;
  piece(c, x, y, a, flipY(s, kk), kk > 0 ? s.col : RUSTY, 1.3, kk > 0 ? s.rb : null);
}

// The pick's idle at <t> seconds, canvas px: plate <i>'s place, turn, side (behind the core below 0) and tumble; mote <i>'s place and side.
function idlePlate(i, t) {
  const s = ash.sh[i], th = i / 4 * 6.283 + t * .35, p = rot(Math.cos(th) * 16, Math.sin(th) * 16 * .45, -.2);
  return [p[0], p[1] + Math.sin(t * 1.2 + i) * 1.2, th * 1.3 + i, Math.sin(th), Math.cos(t * (.3 + .4 * s.rr) + s.ph * 3)];
}
function idleMote(i, t) {
  const p = ash.P[i], o = i % 3, tilt = -.3 + o * 1.05, dir = o % 2 ? -1 : 1, th = p.ph + t * (.8 + .25 * o) * dir;
  const rx = 10 + o * 2.2 + (p.r1 - .5) * 1.6, q = rot(Math.cos(th) * rx, Math.sin(th) * rx * .3, tilt);
  return [q[0], q[1], Math.sin(th)];
}
// The pick's dustPass, idle: two tones and a lit top, the hot motes' embers added.
function soot(c, D, t, front) {
  D.forEach((q, i) => {
    const p = ash.P[i], s = p.sz; if ((q[2] >= 0) !== front) return;
    c.fillStyle = p.tn < .5 ? '#1a1b1f' : '#26282d'; c.fillRect(q[0] - s / 2 - .35, q[1] - s / 2 - .35, s + .7, s + .7);
    c.fillStyle = p.tn < .5 ? '#4c4f55' : '#606369'; c.fillRect(q[0] - s / 2, q[1] - s / 2, s, s);
    if (s > 1.1) { c.fillStyle = 'rgba(170,174,180,.45)'; c.fillRect(q[0] - s / 2, q[1] - s / 2, s, .5); }
  });
  c.save(); c.globalCompositeOperation = 'lighter';
  D.forEach((q, i) => {
    const p = ash.P[i], s = p.sz; if ((q[2] >= 0) !== front || !p.hot) return;
    const h = .45 + .4 * Math.sin(t * 11 + p.ph * 7);
    c.fillStyle = css(pale(EMBER, .25 * h), .95 * h); c.fillRect(q[0] - s / 2, q[1] - s / 2, s, s);
    if (h > .55) { const gr = c.createRadialGradient(q[0], q[1], 0, q[0], q[1], 3.2); gr.addColorStop(0, css(EMBER, .45 * h)); gr.addColorStop(1, css(EMBER, 0)); c.fillStyle = gr; c.fillRect(q[0] - 3.2, q[1] - 3.2, 6.4, 6.4); }
  });
  c.restore();
}

// ---------- the threads body (drone-threads), the pick «Нити»: tools/pick-kit/pages/drone-embers.js, candidate threads ----------
// Its shapes' polylines and its numbers, as the pick drew them, written to the game (DroneInk.cs): the game moves and lights them.
// Pick px: about the drone the kit draws drone_core at DS·DSC = 0.63 px a unit, the game at 0.25 world px; over the world 4 a world
// px (the kit's zoom); on a window's frame a screen px.
function threadsInk() {
  const TAU = Math.PI * 2;
  const arcP = (cx, cy, rx, ry, a0, a1, n = 18) => { const p = []; for (let i = 0; i <= n; i++) { const a = a0 + (a1 - a0) * i / n; p.push([cx + Math.cos(a) * rx, cy + Math.sin(a) * ry]); } return p; };
  const para = (back, half, cv, n) => { const p = []; for (let i = 0; i <= n; i++) { const v = -half + 2 * half * i / n; p.push([-back + v * v / cv, v]); } return p; };
  const rect = (u0, u1, h) => [[u0, -h], [u1, -h], [u1, h], [u0, h], [u0, -h]];
  const lamp = [para(9, 12, 14, 14), para(7.2, 10, 15, 12)];
  const scan = [rect(3, 11, 3.6), rect(10, 19, 2.8), arcP(21.5, 0, 1.3, 4.4, 0, TAU, 14), arcP(-2, 0, 5.5, 5.5, Math.PI / 2, 3 * Math.PI / 2, 12)];
  const fan = [];
  for (let j = 0; j < 7; j++) { const a = (j / 3 - 1) * .8; fan.push([[Math.cos(a) * 6, Math.sin(a) * 6], [Math.cos(a) * 19, Math.sin(a) * 19]]); }
  fan.push(arcP(0, 0, 19, 19, -.8, .8, 14));
  const num = v => { const r = Math.round(v * 1e4) / 1e4; return (Object.is(r, -0) ? 0 : r) + 'f'; };
  const lines = ls => `[\n${ls.map(l => `        [${l.map(p => `${num(p[0])}, ${num(p[1])}`).join(', ')}],`).join('\n')}\n    ]`;
  const cs = `// Generated by tools/anime-art/subjects/drone.js: export again, never edit.
namespace Stillwake.Game.Shared;

/// <summary>
/// The light drone's threads body as the pick «Нити» drew it (drone-threads; drone-embers.js, candidate threads, gauge dots): its
/// shapes' polylines, each a run of x, y pairs, and its numbers, in the pick's px and seconds.
/// </summary>
public static class DroneInk
{
    /// <summary>World px a pick px about the drone: the kit drew the core 0.63 px a unit, the game 0.25 world px.</summary>
    public const float Px = ${num(0.25 / (0.45 * 1.4))};

    /// <summary>World px a pick px over the world, the kit's zoom of 4.</summary>
    public const float WorldPx = 0.25f;

    /// <summary>How many sparks the body has.</summary>
    public const int Sparks = 120;

    /// <summary>The idle orbits: the first's radius, each next's more, a spark's spread, all widened by Wide; how flat; the first's tilt and each next's more.</summary>
    public const float Orbit = 6.3f, OrbitMore = 1.4f, OrbitSpread = 1, OrbitWide = 1.05f, OrbitFlat = 0.3f, OrbitTilt = -0.3f, OrbitTiltMore = 1.05f;

    /// <summary>The orbits' pace, radians a second: the first's, each next's more, all hurried by Hurry; the middle one runs the other way.</summary>
    public const float OrbitPace = 0.8f, OrbitPaceMore = 0.25f, OrbitHurry = 2.2f;

    /// <summary>A shape's share of the sparks; a thought's.</summary>
    public const float Share = 0.8f, ThoughtShare = 0.72f;

    /// <summary>A mode's or a gesture's shape forms and falls apart in this long, s; a spark into it starts late by its share of Lag over Span, out of it by LagOut over SpanOut.</summary>
    public const float Form = 0.6f, Lag = 0.45f, Span = 0.55f, LagOut = 0.4f, SpanOut = 0.6f;

    /// <summary>How fast the sparks flow along a shape, px a second.</summary>
    public const float Flow = 30;

    /// <summary>A gesture's fan stands straight up, swaying this far at this pace.</summary>
    public const float FanSway = 0.22f, FanSwayPace = 3.2f;

    /// <summary>A spark's side; its tail's squares, how far apart, their paleness, the first's alpha and how much each next fades.</summary>
    public const float Spark = 0.95f, TailStep = 1.3f, TailPale = 0.3f, TailAlpha = 0.95f, TailFade = 0.3f;
    public const int Tail = 3;

    /// <summary>A spark's heat: the idle share of bright ones, their middle, swing and pace; the dim ones'; on a shape its middle, swing and pace; in flight its bump; in the lamp its more.</summary>
    public const float BrightShare = 0.5f, Bright = 0.55f, BrightSwing = 0.3f, BrightPace = 11, Dim = 0.2f;
    public const float OnShape = 0.7f, OnShapeSwing = 0.25f, OnShapePace = 13, FlightBump = 0.3f, LampMore = 0.12f;

    /// <summary>An alarm adds this heat at its height, and reddens the hot colour by this much of it.</summary>
    public const float AlarmHeat = 0.55f, AlarmRed = 1.4f;

    /// <summary>The lines' widths and alphas: the orbits; a shape's wide and fine lines; a spark's thread in flight, every Every-th.</summary>
    public const float OrbitLine = 0.4f, OrbitAlpha = 0.12f, ShapeWide = 1.6f, ShapeWideAlpha = 0.06f, ShapeFine = 0.45f, ShapeFineAlpha = 0.3f;
    public const float ThreadLine = 0.35f, ThreadAlpha = 0.07f;
    public const int ThreadEvery = 4;

    /// <summary>The coal: its halo's radius and alpha; its cracks from and to; their width and alpha; its flicker's low, swing and pace.</summary>
    public const float CoalHalo = 8, CoalHaloAlpha = 0.35f, CrackFrom = 0.6f, CrackTo = 2.6f, CrackLine = 0.45f, CrackAlpha = 0.5f;
    public const float Flicker = 0.75f, FlickerSwing = 0.25f, FlickerPace = 7.3f;

    /// <summary>Each crack's turn off the eye at its two ends (the second halved).</summary>
    public static readonly float[] Cracks = [-0.6f, 2.4f, 0.9f, 3.6f, 2.2f, 4.8f];

    /// <summary>The eye: how far ahead of the core's middle, its radius, paleness and alpha; its halo's radius and alpha.</summary>
    public const float EyeAhead = ${num(2.2 * 0.45 * 1.4)}, Eye = 1.2f, EyePale = 0.5f, EyeAlpha = 0.9f, EyeHalo = 4, EyeHaloAlpha = 0.5f;

    /// <summary>The colours, rgb 0..255: the embers', an alarm's red, a spark's dark.</summary>
    public static readonly (int R, int G, int B) Ember = (255, 138, 58), Red = (255, 58, 40), Dark = (38, 39, 43);

    /// <summary>The lamp's mirror behind the core, open toward the aim (x along it).</summary>
    public static readonly float[][] Lamp = ${lines(lamp)};

    /// <summary>The spyglass on the aim: its two draws, the ring at its mouth, the hood behind the core.</summary>
    public static readonly float[][] Scan = ${lines(scan)};

    /// <summary>A gesture's fan: seven rays and an arc about its heading.</summary>
    public static readonly float[][] Fan = ${lines(fan)};

    /// <summary>The spyglass's mouth, where the scan's sparks leave for the gauge.</summary>
    public const float MouthX = 21.5f, MouthY = 0;

    /// <summary>A window's frame, screen px and s: its rows and how far apart (and each square's gap); the most a row holds; a square's dark and glow sides.</summary>
    public const int FrameRows = 2, FrameMost = 1024;
    public const float FrameGap = 3.4f, Square = 1.8f, SquareGlow = 1.6f;

    /// <summary>Its clock: the last square lands at Fly, each flying One; the screen comes over Inside; closing, it goes in Close, the squares home in Back.</summary>
    public const float Fly = 0.75f, One = 0.3f, InsideFrom = 0.8f, InsideTo = 1, Close = 0.15f, Back = 0.75f;

    /// <summary>A landed square's shimmer: its least, its wave's height, how many waves round, their pace, how sharp; its noise.</summary>
    public const float ShimmerLeast = 0.14f, Shimmer = 0.7f, ShimmerWaves = 6, ShimmerPace = 4, ShimmerSharp = 6, ShimmerNoise = 0.1f, ShimmerNoisePace = 0.4f;

    /// <summary>The scan's gauge, pick px over the world: its dots, how far apart, their side, how far over the wreck's top.</summary>
    public const int Dots = 26;
    public const float DotStep = 7, Dot = 3.2f, DotsAbove = 20;

    /// <summary>The gauge coming and going, s, each dot late by its share of Lag over Span (the out's own); a dot's flicker; its heat lit and dark.</summary>
    public const float DotsIn = 0.4f, DotsOut = 0.4f, DotLag = 0.4f, DotSpan = 0.6f, DotLagOut = 0.35f, DotSpanOut = 0.65f;
    public const float DotFlicker = 0.82f, DotFlickerSwing = 0.18f, DotFlickerPace = 9, DotFlickerStep = 2.3f, DotLit = 0.3f, DotLitMore = 0.62f, DotDark = 0.04f;

    /// <summary>A hit: how long it lasts, s; the heat it adds to the lit; the row's shudder, px, and its pace; a burst ember's speed least and more, px a second, its fall.</summary>
    public const float Hit = 0.8f, HitHeat = 0.6f, Shudder = 1.4f, ShudderPace = 60, BurstSpeed = 14, BurstMore = 16, BurstFall = 34;
}
`;
  writeText('../../shared/logic/DroneInk.cs', cs);
}

Subjects.drone = function () {
  threadsInk();
  const cs = CoreBox * K, coreSheet = document.createElement('canvas');
  [coreSheet.width, coreSheet.height] = [cs, cs];
  const cc = coreSheet.getContext('2d');
  cc.grain = grainOf(cc);
  cc.translate(cs / 2, cs / 2); core(cc);
  out('drone_core', coreSheet);

  const side = PlateBox * K, sheet = document.createElement('canvas');
  [sheet.width, sheet.height] = [side * Turns * 2, side * ash.sh.length];
  const c = sheet.getContext('2d');
  c.grain = grainOf(c);
  ash.sh.forEach((s, i) => {
    const far = Math.max(...s.p.map(q => Math.hypot(q[0], q[1])));
    if (far + 1.4 > side / 2) throw new Error(`Plate ${i} reaches ${far.toFixed(1)} px, past its box's ${side / 2}`);
    for (let k = 0; k < Turns; k++) {
      const a = k / Turns * Math.PI * 2;
      plate(c, side * k + side / 2, side * i + side / 2, a, s, 1);
      plate(c, side * (Turns + k) + side / 2, side * i + side / 2, a, s, -1);
    }
  });
  out('drone_plates', sheet);

  // The card: the drone idle facing right, its lamp on, grown as far as its plates let it in the icon's 64 px.
  const t = 2, plates = ash.sh.map((s, i) => [s, idlePlate(i, t)]), D = ash.P.map((p, i) => idleMote(i, t));
  const reach = Math.max(...plates.map(([s, q]) => Math.hypot(q[0], q[1]) + Math.max(...s.p.map(v => Math.hypot(v[0], v[1])))));
  const icon = document.createElement('canvas');
  [icon.width, icon.height] = [64, 64];
  const g = icon.getContext('2d');
  g.grain = grainOf(g);
  g.translate(32, 32); const grow = Math.min(1.25, 31 / reach); g.scale(grow, grow);
  soot(g, D, t, false);
  for (const [s, q] of plates) if (q[3] < 0) plate(g, q[0], q[1], q[2], s, q[4]);
  core(g);
  g.save(); g.translate(2.2, 0); g.scale(.82, .82);
  g.fillStyle = DARK[2]; g.beginPath(); g.ellipse(0, 0, 3.1, 4.4, 0, 0, 6.283); g.fill();
  g.strokeStyle = INK; g.lineWidth = 1.3; g.stroke();
  g.fillStyle = css(pale(LAMP, .3)); g.beginPath(); g.ellipse(.3, 0, 2.1, 3.3, 0, 0, 6.283); g.fill();
  g.fillStyle = css(pale(LAMP, .8)); g.beginPath(); g.ellipse(.8, 0, .9, 1.7, 0, 0, 6.283); g.fill();
  g.restore();
  halo(g, 2.2, 0, 14 * .82, LAMP, .55);
  soot(g, D, t, true);
  for (const [s, q] of plates) if (q[3] >= 0) plate(g, q[0], q[1], q[2], s, q[4]);
  out('items/lightDrone', icon);

  // machine_alienCore: the core as it lies in the wreck before it wakes, its own size, sunk in a heap of its cold soot, the lens dull,
  // an ember or two; 2 cells square, the heap reaching the bottom (MachineFootTests).
  const lying = document.createElement('canvas');
  [lying.width, lying.height] = [64, 64];
  const l = lying.getContext('2d');
  l.grain = grainOf(l);
  const hr = rng(707), heap = [];
  for (let i = 0; i < 90; i++) {
    const x = (hr() * 2 - 1) * 15, top = 7.5 * (1 - (x / 15) ** 2), s = 1.3 + hr() * 1.5;
    heap.push({ x: 32 + x, y: 64 - s / 2 - hr() ** 1.6 * top, s, tn: hr(), hot: hr() < .08, ph: hr() * 6.283 });
  }
  const mote = (p) => {
    l.fillStyle = p.tn < .5 ? '#1a1b1f' : '#26282d'; l.fillRect(p.x - p.s / 2 - .35, p.y - p.s / 2 - .35, p.s + .7, p.s + .7);
    l.fillStyle = p.hot ? css(EMBER, .9) : p.tn < .5 ? '#4c4f55' : '#606369'; l.fillRect(p.x - p.s / 2, p.y - p.s / 2, p.s, p.s);
  };
  heap.filter(p => p.y < 60).forEach(mote);
  l.save(); l.translate(32, 57.5); core(l);
  l.translate(2.2, 0); l.scale(.82, .82);
  l.fillStyle = DARK[2]; l.beginPath(); l.ellipse(0, 0, 3.1, 4.4, 0, 0, 6.283); l.fill();
  l.strokeStyle = INK; l.lineWidth = 1.3; l.stroke();
  l.fillStyle = css(pale([77, 64, 45], .1)); l.beginPath(); l.ellipse(.3, 0, 2.1, 3.3, 0, 0, 6.283); l.fill();
  l.restore();
  heap.filter(p => p.y >= 60).forEach(mote);
  for (const p of heap) if (p.hot) halo(l, p.x, p.y, 3, EMBER, .3);
  out('machine_alienCore', lying);

  // The giant core of the drone's meeting (drone-meeting), the vision page's (drone-vision.js gCore) at its giant size, 2x its 27 px a
  // unit, its pens' caps doubled with it: drawn by the game at any size, never stretched past this. Four pictures on one frame, the core's
  // middle at the frame's: its body with the light from straight above and its ink (drone_giant, never turned); its pits
  // (drone_giant_skin), its dull lens (drone_giant_lens) and that lens lit (drone_giant_lit), all three turned with the lens.
  for (const [name, draw] of [['drone_giant', giantBody], ['drone_giant_skin', giantSkin], ['drone_giant_lens', giantLens], ['drone_giant_lit', giantLit]]) {
    const cv = document.createElement('canvas');
    [cv.width, cv.height] = [GiantBox, GiantBox];
    const g = cv.getContext('2d');
    g.translate(GiantBox / 2, GiantBox / 2); g.scale(GiantR, GiantR);
    draw(g);
    out(name, cv);
  }
};
// The threads' numbers alone, for a run with no browser (they need no canvas).
Subjects.drone.ink = threadsInk;

// ---------- the giant core (drone-vision.js), drawn at GiantR px a unit ----------
const GiantR = 54, GiantBox = 672;
const cap = (x, m) => x / Math.sqrt(1 + (x / m) * (x / m));
const pen = (R, w, m) => cap(w * R, 2 * m) / R;
const PITS = [[-2.9, -2.4, .34], [-1.1, -3.7, .2], [1.5, -3.2, .26], [-3.9, .2, .28], [-.9, 1.4, .17], [2.6, 2.9, .3], [.6, 4.1, .22], [-2.3, 3.5, .19], [-4.2, -1.5, .16], [3.6, -1.9, .18], [-1.7, -.6, .13], [-3.1, 2.1, .12]];
// The giant's watercolour, in device px: pooling with a darker tide line, grain, a few fibres; it tiles.
function paper(g) {
  const N = 320, cv = document.createElement('canvas'); cv.width = cv.height = N;
  const x = cv.getContext('2d'), r = rng(4242), round = (f) => { for (const ox of [-N, 0, N]) for (const oy of [-N, 0, N]) { x.save(); x.translate(ox, oy); f(); x.restore(); } };
  for (let i = 0; i < 22; i++) {
    const b = blobAt(r, r() * N, r() * N, 16 + r() * 46), dark = r() < .6;
    round(() => { trace(x, b.s); x.fillStyle = dark ? 'rgba(0,0,0,.075)' : 'rgba(255,255,255,.035)'; x.fill(); x.strokeStyle = dark ? 'rgba(0,0,0,.12)' : 'rgba(255,255,255,.05)'; x.lineWidth = 1.3; x.stroke(); });
  }
  for (let i = 0; i < 5200; i++) { const d = r() < .62, px = r() * N, py = r() * N; x.fillStyle = d ? `rgba(0,0,0,${(.05 + r() * .14).toFixed(3)})` : `rgba(255,255,255,${(.025 + r() * .06).toFixed(3)})`; x.fillRect(px, py, 1 + (r() < .25), 1); }
  x.lineWidth = .8; x.strokeStyle = 'rgba(0,0,0,.08)';
  for (let i = 0; i < 26; i++) { const px = r() * N, py = r() * N, rad = 8 + r() * 30, a0 = r() * 6.283; round(() => { x.beginPath(); x.arc(px, py, rad, a0, a0 + 1.2); x.stroke(); }); }
  return g.createPattern(cv, 'repeat');
}
function giantBody(c) {
  const R = GiantR;
  c.save(); trace(c, ash.core); c.clip();
  c.fillStyle = DARK[2]; c.fillRect(-9, -9, 18, 18);
  c.save(); c.translate(0, -.8); c.fillStyle = DARK[1]; trace(c, ash.core); c.fill(); c.restore();
  c.save(); c.translate(0, -2.6); c.fillStyle = DARK[0]; trace(c, ash.core); c.fill(); c.restore();
  c.save(); c.setTransform(1, 0, 0, 1, 0, 0); c.globalAlpha = .9; c.fillStyle = paper(c); c.fillRect(0, 0, GiantBox, GiantBox); c.restore();
  c.restore();
  // The pick's gInk: a lit edge where the outline faces up, then the wandering pen.
  const s = ash.core, off = cap(1.4 * R, 2 * 2.6) / R;
  c.save(); c.lineCap = 'round'; c.lineJoin = 'round';
  c.strokeStyle = 'rgba(214,220,226,.5)'; c.lineWidth = pen(R, 1, 1.6);
  for (const g of s.segs) {
    const nx = g[2] - s.cx, ny = g[3] - s.cy, L = Math.hypot(nx, ny) || 1;
    if (ny / L < -.42) { const ix = -nx / L * off, iy = -ny / L * off; c.beginPath(); c.moveTo(g[0] + ix, g[1] + iy); c.quadraticCurveTo(g[2] + ix, g[3] + iy, g[4] + ix, g[5] + iy); c.stroke(); }
  }
  c.strokeStyle = INK; const lw = pen(R, 1.7, 5.5);
  for (const g of s.segs) { if (g[6] <= .05) continue; c.lineWidth = lw * g[6]; c.beginPath(); c.moveTo(g[0], g[1]); c.quadraticCurveTo(g[2], g[3], g[4], g[5]); c.stroke(); }
  c.restore();
}
function giantSkin(c) {
  const R = GiantR;
  c.save(); trace(c, ash.core); c.clip(); c.lineCap = 'round';
  for (const [x, y, r] of PITS) {
    c.fillStyle = 'rgba(14,15,18,.55)'; c.beginPath(); c.ellipse(x, y, r, r * .82, 0, 0, 6.283); c.fill();
    c.strokeStyle = 'rgba(205,210,218,.4)'; c.lineWidth = pen(R, .07, 1.3); c.beginPath(); c.ellipse(x, y + r * .08, r * .86, r * .68, 0, .4, 2.75); c.stroke();
    c.strokeStyle = 'rgba(10,10,12,.6)'; c.lineWidth = pen(R, .09, 1.6); c.beginPath(); c.ellipse(x, y, r * 1.05, r * .86, 0, 3.6, 5.9); c.stroke();
  }
  c.restore();
}
function giantLens(c) {
  const R = GiantR * .82;
  c.save(); c.translate(2.2, 0); c.scale(.82, .82);
  c.fillStyle = DARK[2]; c.beginPath(); c.ellipse(0, 0, 3.1, 4.4, 0, 0, 6.283); c.fill(); c.strokeStyle = INK; c.lineWidth = pen(R, 1.3, 3.8); c.stroke();
  c.fillStyle = css(pale([77, 64, 45], .1)); c.beginPath(); c.ellipse(.3, 0, 2.1, 3.3, 0, 0, 6.283); c.fill();
  c.save(); c.beginPath(); c.ellipse(.3, 0, 2.1, 3.3, 0, 0, 6.283); c.clip();
  c.translate(0, .9); c.fillStyle = css([52, 44, 34], .75); c.beginPath(); c.ellipse(.3, 0, 2.1, 3.3, 0, 0, 6.283); c.fill(); c.restore();
  c.strokeStyle = 'rgba(255,255,255,.16)'; c.lineWidth = pen(R, .16, 2.4); c.beginPath(); c.ellipse(.3, 0, 1.7, 2.8, 0, 3.55, 4.3); c.stroke();
  c.restore();
}
function giantLit(c) {
  const R = GiantR * .82, SIGN = [168, 238, 255];
  c.save(); c.translate(2.2, 0); c.scale(.82, .82);
  c.fillStyle = css(pale(SIGN, .3)); c.beginPath(); c.ellipse(.3, 0, 2.1, 3.3, 0, 0, 6.283); c.fill();
  c.fillStyle = css(pale(SIGN, .8)); c.beginPath(); c.ellipse(.8, 0, .9, 1.7, 0, 0, 6.283); c.fill();
  c.strokeStyle = css(pale(SIGN, .55), .4); c.lineWidth = pen(R, .12, 1.3);
  for (const k of [.72, .46]) { c.beginPath(); c.ellipse(.3, 0, 2.1 * k, 3.3 * k, 0, 0, 6.283); c.stroke(); }
  c.restore();
}
})();

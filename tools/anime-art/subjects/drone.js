// The light drone (drone-swarm), the user's pick «Пепел в латах»: an iron core, its body a cloud of soot, four scrap plates riding
// it as armour. The pick page's own pen and paint, its small level (makeAsh's build(0)), drawn straight (no finish), 4 hi-res pixels
// a world pixel as there. drone_core: the core, clean, one picture (it is round), CoreBox world px, its middle at the box's.
// The sparks, their threads and the glow are the game's (DroneSwarm.cs, LightDrone.cs), since they move or light; the plates are
// gone from the game (drone-threads) and stay only on items/lightDrone, the drone idle, its lamp on, till the card is redrawn.
(function () {
'use strict';
const CoreBox = 4, K = 4;
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

// The pick's small ash drone, drawn from its seeds in its order: the core is its swarm's, the plates' phases and paces the card's,
// the soot its dustSet (seed 530), as Meeting.cs draws it.
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

// ---------- the drone's thoughts and moods (drone-thoughts), the pick «Контур»: tools/pick-kit/pages/drone-moods.js, candidate outline ----------
// Its pictures (PICT) as the pick drew them, a box -1..1 (y down), the alarms as drone-thoughts.js drew them too; its clock, its flows,
// its heat and how each mood moves the body (MOTION), in the pick's px over the world and seconds. Written to DroneInk.cs.
// The pictures of the pick «Контур» (drone-moods.js, candidate outline), each polylines in a box -1..1, y down; the alarms' icon is its meteor.
function thoughtPictures() {
  const TAU = Math.PI * 2;
  const arcP = (cx, cy, rx, ry, a0, a1, n = 18) => { const p = []; for (let i = 0; i <= n; i++) { const a = a0 + (a1 - a0) * i / n; p.push([cx + Math.cos(a) * rx, cy + Math.sin(a) * ry]); } return p; };
  const circ = (cx, cy, r, n = 22) => arcP(cx, cy, r, r, 0, TAU, n);
  const heart = (cx, cy, s, n = 36) => { const p = []; for (let i = 0; i <= n; i++) { const a = i / n * TAU; p.push([cx + s * 16 * Math.pow(Math.sin(a), 3) / 17, cy - s * (13 * Math.cos(a) - 5 * Math.cos(2 * a) - 2 * Math.cos(3 * a) - Math.cos(4 * a)) / 17 - .15 * s]); } return p; };
  const G = {};
  G.cold = [];
  for (let i = 0; i < 3; i++) { const a = Math.PI / 2 + i * Math.PI / 3; G.cold.push([[-Math.cos(a), -Math.sin(a)], [Math.cos(a), Math.sin(a)]]); }
  for (let i = 0; i < 6; i++) { const a = Math.PI / 2 + i * Math.PI / 3, P = [Math.cos(a) * .56, Math.sin(a) * .56], b = q => [P[0] + Math.cos(q) * .32, P[1] + Math.sin(q) * .32]; G.cold.push([b(a - .9), P, b(a + .9)]); }
  const C = [-.42, .42], rock = [];
  [.36, .3, .38, .29, .35, .31, .37].forEach((r, i) => { const a = i / 7 * TAU + .3; rock.push([C[0] + Math.cos(a) * r, C[1] + Math.sin(a) * r]); });
  rock.push(rock[0].slice());
  const d = [.7071, -.7071], n = [.7071, .7071];
  G.meteor = [rock, ...[[-.24, .75], [0, 1.05], [.24, .7]].map(([o, L]) => { const s = [C[0] + d[0] * .44 + n[0] * o, C[1] + d[1] * .44 + n[1] * o]; return [s, [s[0] + d[0] * L, s[1] + d[1] * L]]; })];
  G.lightning = [[[.3, -1], [-.34, .08], [.14, .08], [-.28, 1]], [[-.28, 1], [-.27, .66]], [[-.28, 1], [-.02, .8]]];
  G.heat = [-.55, 0, .55].map((x0, j) => { const p = []; for (let i = 0; i <= 16; i++) { const y = .72 - i / 16 * 1.66; p.push([x0 + .16 * Math.sin((.72 - y) * 5 + j * .6), y]); } return p; });
  G.heat.push([[-.95, .95], [.95, .95]]);
  G.curious = [arcP(0, -.44, .4, .4, Math.PI - .15, TAU + 1.05, 18).concat([[0, .1], [0, .4]]), circ(0, .8, .08, 8)];
  G.bored = [-.6, 0, .6].map(x => circ(x, .2, .16, 10));
  G.love = [heart(0, 0, 1)];
  G.pride = [[[-.9, .6], [-.95, -.45], [-.45, .05], [0, -.72], [.45, .05], [.95, -.45], [.9, .6], [-.9, .6]], [[-.9, .32], [.9, .32]]];
  // The magnetic flare, drawn after the pick in its outline: a horseshoe magnet, poles down and barred, a crackle off each pole and one between.
  G.flare = [[[-.5, .3]].concat(arcP(0, -.3, .5, .5, Math.PI, TAU, 16)).concat([[.5, .3]]),
    [[-.72, .3], [-.28, .3]], [[.28, .3], [.72, .3]],
    [[-.58, .48], [-.76, .62], [-.6, .76], [-.82, .96]], [[.58, .48], [.76, .62], [.6, .76], [.82, .96]], [[0, .5], [-.1, .66], [.1, .8], [0, .98]]];
  return G;
}

function thoughtGlyphs() {
  const G = thoughtPictures();
  const num = v => { const r = Math.round(v * 1e4) / 1e4; return (Object.is(r, -0) ? 0 : r) + 'f'; };
  const lines = ls => `[\n${ls.map(l => `        [${l.map(p => `${num(p[0])}, ${num(p[1])}`).join(', ')}],`).join('\n')}\n    ]`;
  const NAMES = [['Meteor', 'meteor', 'The meteor coming: a rock and three streaks behind it.'], ['Bolt', 'lightning', 'The bolt: a zigzag and its arrowhead.'], ['Flare', 'flare', 'The magnetic flare: a horseshoe magnet, poles down, crackling.'],
    ['Cold', 'cold', 'The cold: a snowflake of six arms.'], ['Heat', 'heat', 'The heat: three wavy rising lines over the ground.'],
    ['Curious', 'curious', 'Curious: a question mark.'], ['Bored', 'bored', 'Bored: three dots.'], ['Love', 'love', 'Love: a heart.'], ['Pride', 'pride', 'Pride: a crown.']];
  return `    /// <summary>
    /// A thought or mood over the drone, the pick «Контур» (drone-moods.js, candidate outline): its picture, box -1..1 y down, stands ThoughtAbove pick px
    /// over the core, ThoughtSize a unit; it forms over ThoughtIn, holds ThoughtHold, falls apart over ThoughtOut, s; its sparks flow along it at AlarmFlow
    /// or MoodFlow, pick px a second. The pick's 0.4, 1.5 and 0.4 s were too quick to make the shape out in play: twice the hold.
    /// </summary>
    public const float ThoughtAbove = 50, ThoughtSize = 22, ThoughtIn = 0.5f, ThoughtHold = 3, ThoughtOut = 0.5f, AlarmFlow = 42, MoodFlow = 18;

    /// <summary>A thought's sparks' heat: an alarm's middle, swing and pace; a mood's, its sparks a step apart; a mood's threads this much of an alarm's.</summary>
    public const float AlarmSpark = 0.75f, AlarmSparkSwing = 0.25f, AlarmSparkPace = 13, MoodSpark = 0.42f, MoodSparkSwing = 0.18f, MoodSparkPace = 4, MoodSparkStep = 1.7f, MoodThread = 0.7f;

    /// <summary>An alarm's red as its picture shows: Low of it and Swing more, Beats a second.</summary>
    public const float AlarmLow = 0.6f, AlarmSwing = 0.4f, AlarmBeats = 2.4f;

    /// <summary>How a mood moves the body (the pick's MOTION), pick px over the world, radians and s: it comes in and goes over MoodIn.</summary>
    public const float MoodIn = 0.5f;

    /// <summary>Curious leans this far toward what it looks at, rises, and tilts its orbits.</summary>
    public const float CuriousLean = 5, CuriousRise = 2, CuriousTilt = 0.45f;

    /// <summary>A find: it hops this high at this pace, rocks, swells, hurries its orbits and glows.</summary>
    public const float FindHop = 7, FindHopPace = 7, FindRock = 0.1f, FindSwell = 0.08f, FindHurry = 1.4f, FindGlow = 0.3f;

    /// <summary>Bored: it sways at a pace, sinks, rocks at a pace, looks about and down, slows its orbits and dims.</summary>
    public const float BoredSway = 5, BoredSwayPace = 0.9f, BoredSink = 6, BoredRock = 0.18f, BoredRockPace = 0.7f, BoredLookPace = 0.6f, BoredLookDown = 1.2f, BoredSlow = 0.65f, BoredDim = 0.3f;

    /// <summary>Love: it presses to the helmet (LoveBehind, LoveOver world px from its middle, HelmetUp over the neck), bobs, tilts back, slows and glows.</summary>
    public const float LoveBehind = 4.5f, LoveOver = 6.25f, HelmetUp = 6.2f, LoveBob = 1.5f, LoveBobPace = 3, LoveTilt = 0.2f, LoveSlow = 0.3f, LoveGlow = 0.2f;

    /// <summary>Pride: it rises, swells, looks up and a little back, hurries its orbits and glows.</summary>
    public const float PrideRise = 9, PrideSwell = 0.14f, PrideLookX = -0.3f, PrideLookY = -1, PrideHurry = 0.3f, PrideGlow = 0.25f;

    /// <summary>
    /// Dimmed, its orbits fade by OrbitDim of it, its coal's halo by HaloDim, its cracks and eye by CoalDim, its eye shrinks by EyeDim, its sparks cool
    /// by SparkDim; a glow adds GlowHalo to the coal's halo.
    /// </summary>
    public const float OrbitDim = 0.5f, HaloDim = 0.5f, CoalDim = 0.7f, EyeDim = 0.5f, SparkDim = 0.6f, GlowHalo = 0.25f;

${NAMES.map(([name, id, says]) => `    /// <summary>${says}</summary>
    public static readonly float[][] Glyph${name} = ${lines(G[id])};`).join('\n\n')}`;
}

// ---------- the threads body (drone-threads), the pick «Нити»: tools/pick-kit/pages/drone-embers.js, candidate threads ----------
// Its shapes' polylines and its numbers, as the pick drew them, written to the game (DroneInk.cs): the game moves and lights them.
// Pick px: about the drone the kit draws drone_core at DS·DSC = 0.63 px a unit, the game at 0.25 world px; over the world 4 a world
// px (the kit's zoom); on a window's frame a screen px.
function threadsInk() {
  const TAU = Math.PI * 2;
  const arcP = (cx, cy, rx, ry, a0, a1, n = 18) => { const p = []; for (let i = 0; i <= n; i++) { const a = a0 + (a1 - a0) * i / n; p.push([cx + Math.cos(a) * rx, cy + Math.sin(a) * ry]); } return p; };
  const para = (back, half, cv, n) => { const p = []; for (let i = 0; i <= n; i++) { const v = -half + 2 * half * i / n; p.push([-back + v * v / cv, v]); } return p; };
  const lamp = [para(9, 12, 14, 14), para(7.2, 10, 15, 12)];
  // The scan's reticle, the pick «Прицел» (tools/pick-kit/pages/scan-form.js, candidate reticle): closed (L 0) and opened (L 1).
  const R = (r, a) => [Math.cos(a) * r, Math.sin(a) * r];
  const lerp = (a, b, k) => a + (b - a) * k;
  const reticle = L => {
    const r = lerp(7.6, 7.1, L), rin = lerp(5.4, 4.3, L);
    return { ring: [arcP(0, 0, r, r, 0, TAU, 26)], ticks: [0, 1, 2, 3].map(q => { const a = q * Math.PI / 2; return [R(rin, a), R(lerp(12.5, 13.5, L), a)]; }) };
  };
  const marks = Array.from({ length: 26 }, (_, j) => { const a = (j + .5) / 26 * TAU, p = R(10.2, a), d = R(1, a + Math.PI / 2); return [[p[0] - d[0] * .22, p[1] - d[1] * .22], [p[0] + d[0] * .22, p[1] + d[1] * .22]]; });
  const closed = reticle(0), open = reticle(1);
  const fan = [];
  for (let j = 0; j < 7; j++) { const a = (j / 3 - 1) * .8; fan.push([[Math.cos(a) * 6, Math.sin(a) * 6], [Math.cos(a) * 19, Math.sin(a) * 19]]); }
  fan.push(arcP(0, 0, 19, 19, -.8, .8, 14));
  const num = v => { const r = Math.round(v * 1e4) / 1e4; return (Object.is(r, -0) ? 0 : r) + 'f'; };
  const lines = ls => `[\n${ls.map(l => `        [${l.map(p => `${num(p[0])}, ${num(p[1])}`).join(', ')}],`).join('\n')}\n    ]`;
  const glyphs = thoughtGlyphs();
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

    /// <summary>A shape's share of the sparks; a thought's (drone-moods.js's <c>share</c>).</summary>
    public const float Share = 0.8f, ThoughtShare = 0.7f;

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

    /// <summary>
    /// The player's colour in the embers, the pick «С облачком» (drone-salts.js, candidate flame): light burns ember below BurnEmber of the way
    /// out of the coal, the ember's Cream by BurnCream, the player's colour by BurnSalt; how far out is pick px from the core, from OutFrom over OutOver.
    /// </summary>
    public const float BurnEmber = 0.25f, BurnCream = 0.45f, BurnSalt = 0.75f, Cream = 0.65f, OutFrom = 2.8f, OutOver = 4.6f;

    /// <summary>
    /// A colour lighter than the ember is paled less, by RoomFall a step of brightness down to RoomLeast, and lit by the root of their ratio
    /// between GainLeast and GainMost, so every colour holds; an icon's run pales by RunPale.
    /// </summary>
    public const float RoomFall = 2.3f, RoomLeast = 0.35f, GainLeast = 0.85f, GainMost = 1.3f, RunPale = 0.6f;

    /// <summary>The colour's cloud round the drone: its radius and alpha; how much brighter and wider the orbits; how much brighter the coal, its halo at most.</summary>
    public const float Cloud = 17, CloudAlpha = 0.22f, OrbitLift = 3, OrbitWiden = 1.4f, CoalLift = 1.3f, CoalHaloMost = 0.8f;

    /// <summary>
    /// A spark on a shape and a gauge's dot are born of the coal over and over, Born times a second, each late by its hash past Later, burning
    /// into the colour over In of each life; an icon's run burns by RunAlong of how far along it is and RunBehind of how far behind its head.
    /// </summary>
    public const float SparkBorn = 0.7f, SparkBornIn = 0.5f, SparkBornLater = 9, DotBorn = 0.5f, DotBornIn = 0.2f, DotBornLater = 21, RunAlong = 1.1f, RunBehind = 0.55f;

    /// <summary>The colours, rgb 0..255: the embers', an alarm's red, a spark's dark.</summary>
    public static readonly (int R, int G, int B) Ember = (255, 138, 58), Red = (255, 58, 40), Dark = (38, 39, 43);

    /// <summary>The lamp's mirror behind the core, open toward the aim (x along it).</summary>
    public static readonly float[][] Lamp = ${lines(lamp)};

    /// <summary>
    /// The scan's reticle on the aim, the pick «Прицел» (scan-form.js, candidate reticle): the dial's Dots marks round the core, a spark
    /// sitting on each, the gauge's dots; they stay as the ring and its four ticks open while the scanner reads, the ticks closing in.
    /// </summary>
    public static readonly float[][] ScanMarks = ${lines(marks)};

    /// <summary>The reticle's ring, closed and opened.</summary>
    public static readonly float[][] ScanRing = ${lines(closed.ring)};
    public static readonly float[][] ScanRingOpen = ${lines(open.ring)};

    /// <summary>The reticle's four ticks, closed and opened.</summary>
    public static readonly float[][] ScanTicks = ${lines(closed.ticks)};
    public static readonly float[][] ScanTicksOpen = ${lines(open.ticks)};

    /// <summary>
    /// The reticle takes every spark: Dots of them on the marks, RingShare of all on the ring, the rest on the ticks; the ring's and the ticks'
    /// flow, px a second along their closed length; where along its mark a spark sits.
    /// </summary>
    public const float RingShare = 0.4f, RingFlow = 12, TickFlow = 8, MarkAt = 0.5f;

    /// <summary>The reticle opens over OpenIn once the scanner reads, and closes over CloseIn, CloseAfter after it stops (its dots home), s.</summary>
    public const float OpenIn = 0.3f, CloseAfter = 0.4f, CloseIn = 0.4f;

    /// <summary>A gesture's fan: seven rays and an arc about its heading.</summary>
    public static readonly float[][] Fan = ${lines(fan)};

    /// <summary>
    /// A drone window's frame, the pick «Уголки» (drone-windows.js, candidate corners), screen px and s: its corners this far out of the
    /// window, each bracket's arms this long; a spark reaches corner k (clockwise from the top left) at Reach + k·ReachLag, the screen
    /// coming in from ScreenFrom over ScreenIn; closing, the screen goes in ScreenOut, the corners home in Home.
    /// </summary>
    public const float CornerOut = 5, Arm = 22, Reach = 0.1f, ReachLag = 0.01f, ScreenFrom = 0.03f, ScreenIn = 0.1f, ScreenOut = 0.08f, Home = 0.12f;

    /// <summary>A shot spark's side and its line back to the eye; a landed corner's halo, its radius and alpha, gone over Landing.</summary>
    public const float Shot = 1.6f, ShotLine = 0.4f, ShotAlpha = 0.2f, LandHalo = 12, LandHaloAlpha = 0.6f, Landing = 0.15f;

    /// <summary>A bracket's wide and fine lines; its corner's glowing square and heat; the sparks on each arm, their pace round it, each corner's later, their heat and how much it fades along.</summary>
    public const float BracketWide = 2.2f, BracketWideAlpha = 0.07f, BracketFine = 0.6f, BracketFineAlpha = 0.45f, CornerSquare = 2.4f, CornerHeat = 0.9f;
    public const int ArmSparks = 3;
    public const float ArmPace = 1.3f, ArmLater = 0.17f, ArmHeat = 0.9f, ArmFade = 0.7f;

    /// <summary>The faint thread from the eye to each landed corner (off unless asked): its width and alpha, a spark creeping along it at Creep, each corner's later, its heat.</summary>
    public const float CornerThread = 0.35f, CornerThreadAlpha = 0.08f, Creep = 0.9f, CreepLater = 0.25f, CreepHeat = 0.55f;

    /// <summary>The scan's gauge, pick px over the world: its dots, how far apart, their side, how far over the wreck's top.</summary>
    public const int Dots = 26;
    public const float DotStep = 7, Dot = 3.2f, DotsAbove = 20;

    /// <summary>Every DotThreadEvery-th dot in flight draws its thread back to where it left.</summary>
    public const int DotThreadEvery = 2;

    /// <summary>The gauge coming and going, s, each dot late by its share of Lag over Span (the out's own); a dot's flicker; its heat lit and dark.</summary>
    public const float DotsIn = 0.4f, DotsOut = 0.4f, DotLag = 0.4f, DotSpan = 0.6f, DotLagOut = 0.35f, DotSpanOut = 0.65f;
    public const float DotFlicker = 0.82f, DotFlickerSwing = 0.18f, DotFlickerPace = 9, DotFlickerStep = 2.3f, DotLit = 0.3f, DotLitMore = 0.62f, DotDark = 0.04f;

    /// <summary>A hit: how long it lasts, s; the heat it adds to the lit; the row's shudder, px, and its pace; how far a burst ember flies, least and more, px over the hit, how far it falls; its heat by what it lost, less the hit's share.</summary>
    public const float Hit = 0.8f, HitHeat = 0.6f, Shudder = 1.4f, ShudderPace = 60, BurstSpeed = 14, BurstMore = 16, BurstFall = 34, BurstHeat = 1.2f;

${glyphs}
}
`;
  writeText('../../shared/logic/DroneInk.cs', cs);
}

// ---------- the thread icons, the pick «Одна нить»: tools/pick-kit/pages/thread-icons.js, candidate oneThread ----------
// The F wheel's modes, the H wheel's gestures and the edge keys' motifs, each a run of polylines in a box -1..1 (y down), and the one
// thread through them all (the pick's joinOne): its points and which a bridge reaches. Written to the game (IconInk.cs); drawn there.
function iconsInk() {
  const TAU = Math.PI * 2;
  const bez = (a, b, d, s) => { const k = 1 - s; return [k * k * a[0] + 2 * k * s * b[0] + s * s * d[0], k * k * a[1] + 2 * k * s * b[1] + s * s * d[1]]; };
  const arc = (cx, cy, r, n = 28, a0 = 0, a1 = TAU) => { const p = []; for (let i = 0; i <= n; i++) { const a = a0 + (a1 - a0) * i / n; p.push([cx + Math.cos(a) * r, cy + Math.sin(a) * r]); } return p; };
  function roundSq(rc) {
    const p = [];
    for (const [cx, cy, a0] of [[1 - rc, -1 + rc, -Math.PI / 2], [1 - rc, 1 - rc, 0], [-1 + rc, 1 - rc, Math.PI / 2], [-1 + rc, -1 + rc, Math.PI]])
      for (let i = 0; i <= 4; i++) { const a = a0 + i / 4 * Math.PI / 2; p.push([cx + Math.cos(a) * rc, cy + Math.sin(a) * rc]); }
    p.push(p[0].slice()); return p;
  }
  function svg(d, n = 8) {
    const tk = d.match(/[MLCQZ]|-?\d*\.?\d+/g), out = []; let cur = null, p = [0, 0], st = [0, 0], i = 0, cmd = '';
    const num = () => +tk[i++];
    while (i < tk.length) {
      if (/[MLCQZ]/.test(tk[i])) cmd = tk[i++];
      if (cmd === 'M') { p = [num(), num()]; st = p; cur = [p]; out.push(cur); cmd = 'L'; }
      else if (cmd === 'L') { p = [num(), num()]; cur.push(p); }
      else if (cmd === 'Q') { const a = [num(), num()], b = [num(), num()]; for (let k = 1; k <= n; k++) cur.push(bez(p, a, b, k / n)); p = b; }
      else if (cmd === 'C') {
        const a = [num(), num()], b = [num(), num()], e = [num(), num()];
        for (let k = 1; k <= n; k++) { const s = k / n, m = 1 - s; cur.push([m * m * m * p[0] + 3 * m * m * s * a[0] + 3 * m * s * s * b[0] + s * s * s * e[0], m * m * m * p[1] + 3 * m * m * s * a[1] + 3 * m * s * s * b[1] + s * s * s * e[1]]); }
        p = e;
      } else if (cmd === 'Z') { cur.push(st.slice()); p = st; }
    }
    return out.map(q => q.map(([x, y]) => [(x - 8) / 8, (y - 8) / 8]));
  }
  const rrD = (x, y, w, h, r) => `M ${x + r} ${y} L ${x + w - r} ${y} Q ${x + w} ${y} ${x + w} ${y + r} L ${x + w} ${y + h - r} Q ${x + w} ${y + h} ${x + w - r} ${y + h} L ${x + r} ${y + h} Q ${x} ${y + h} ${x} ${y + h - r} L ${x} ${y + r} Q ${x} ${y} ${x + r} ${y} Z`;
  const motif = ds => ds.flatMap(d => svg(d));
  // One thread through every part: each next part from the nearer of its ends, the step into it a bridge.
  function joinOne(polys) {
    const left = polys.slice(1).map(p => p.slice()), cur = polys[0].slice(), bridges = [];
    while (left.length) {
      const e = cur[cur.length - 1]; let best = 0, rev = false, bd = 1e9;
      left.forEach((p, i) => { const d0 = Math.hypot(e[0] - p[0][0], e[1] - p[0][1]), d1 = Math.hypot(e[0] - p[p.length - 1][0], e[1] - p[p.length - 1][1]); if (d0 < bd) { bd = d0; best = i; rev = false; } if (d1 < bd) { bd = d1; best = i; rev = true; } });
      let p = left.splice(best, 1)[0]; if (rev) p = p.slice().reverse();
      bridges.push(cur.length);
      for (const q of p) cur.push(q);
    }
    return { one: cur, bridges };
  }
  const THUMB = [[-.5, .85], [-.5, .05], [-.22, -.02], [-.2, -.55], [-.12, -.85], [.02, -.92], [.14, -.82], [.12, -.35], [.1, -.05], [.55, -.05], [.68, .08], [.62, .25], [.7, .38], [.63, .55], [.68, .7], [.55, .85], [-.5, .85]];
  const FIST = [THUMB, [[.1, .25], [.6, .25]], [[.1, .45], [.62, .45]], [[.1, .65], [.6, .65]], [[-.85, 0], [-.62, 0], [-.62, .88], [-.85, .88], [-.85, 0]]];
  const ICONS = [
    ['Lamp', 'The lamp: a lens ringed by rays, its rays one zigzag.', [Array.from({ length: 17 }, (_, k) => { const a = k / 16 * TAU - Math.PI / 2, r = k % 2 ? .52 : .95; return [Math.cos(a) * r, Math.sin(a) * r]; }), arc(0, 0, .24, 16)]],
    ['Scan', 'The scan: a ring a line sweeps round, a trail behind it.', [arc(0, 0, .88, 36), [[0, 0], [Math.cos(-Math.PI / 4) * .88, Math.sin(-Math.PI / 4) * .88]], arc(0, 0, .58, 10, -Math.PI / 4 - 1.1, -Math.PI / 4), arc(0, 0, .16, 12)]],
    ['Yes', 'Yes: a fist, its thumb up.', FIST],
    ['No', 'No: the fist, its thumb down.', FIST.map(p => p.map(([x, y]) => [x, -y]))],
    ['Wave', 'A wave: an open hand, two arcs by it.', [[[-.42, .92], [-.48, .3], [-.86, -.08], [-.78, -.22], [-.4, .02], [-.38, -.68], [-.26, -.8], [-.14, -.68], [-.12, -.16], [-.08, -.86], [.04, -.96], [.17, -.86], [.15, -.16], [.22, -.74], [.34, -.82], [.45, -.7], [.38, -.08], [.48, -.46], [.58, -.52], [.68, -.42], [.56, .3], [.36, .92], [-.42, .92]],
      arc(.1, -.3, .8, 6, -.9, -.1), arc(.1, -.3, .98, 6, -.85, -.15)]],
    ['Air', 'The air key: three gusts.', motif(['M 0.5 5.9 C 4 5.3 8 6.1 11.4 5.5 C 14.3 5.0 14.5 1.4 12.0 1.3 C 10.5 1.25 9.9 2.7 10.9 3.4', 'M 1.6 8.7 C 5.5 8.1 10.5 9.1 15.5 8.3', 'M 0.5 11.5 C 3.8 10.9 7.2 11.7 9.6 11.6 C 12.5 11.5 12.8 15.1 10.4 15.25 C 8.9 15.35 8.3 13.9 9.3 13.2'])],
    ['Build', 'The build key: a wall of bricks.', motif([rrD(0.4, 1.8, 7.1, 3.5, .55), 'M 8.4 1.8 L 14.6 1.8 Q 15.6 1.8 15.6 3.0 L 15.6 4.7 Q 15.6 5.3 15.0 5.3 L 8.95 5.3 Q 8.4 5.3 8.4 4.75 L 8.4 2.35 Q 8.4 1.8 8.95 1.8 Z',
      rrD(0.4, 6.2, 3.1, 3.5, .5), rrD(4.4, 6.25, 7.2, 3.5, .55), rrD(12.5, 6.2, 3.1, 3.5, .5), rrD(0.4, 10.6, 7.1, 3.5, .55),
      'M 8.4 10.75 L 15.05 10.75 Q 15.6 10.75 15.6 11.3 L 15.6 13.75 Q 15.6 14.25 15.05 14.25 L 8.95 14.25 Q 8.4 14.25 8.4 13.7 L 8.4 12.2 L 9.6 10.75 Z'])],
    ['Research', 'The board\'s key: a bulb.', motif(['M 5.3 10.8 C 2.4 9.3 1.8 5.4 3.8 3.0 C 5.8 0.6 10.2 0.6 12.2 3.0 C 14.2 5.4 13.6 9.3 10.7 10.8 L 10.4 12.2 L 5.6 12.2 Z', rrD(5.5, 12.7, 5, 1.6, .3),
      'M 6.7 14.8 L 9.3 14.8 L 8.7 15.8 L 7.3 15.8 Z', 'M 6.5 9.8 L 7 6.6 L 8 8.2 L 9 6.6 L 9.5 9.8'])],
    ['Market', 'The market\'s key: a drop of oil, or a sack.', motif(['M 8 0.8 Q 8.5 2.6 9.7 3.7 Q 14.9 6.5 14.3 11.0 Q 13.5 15.2 8 15.4 Q 2.5 15.2 1.7 11.0 Q 1.1 6.5 6.3 3.7 Q 7.5 2.6 8 0.8 Z', 'M 8 4.6 Q 4.0 7.4 4.6 11.2 Q 5.2 14.0 8 14.4', 'M 8 4.6 Q 12.0 7.4 11.4 11.2 Q 10.8 14.0 8 14.4'])],
    ['Journal', 'The Handbook\'s key: an open book.', motif(['M 7.75 4.6 Q 4.8 2.7 0.6 3.3 L 0.4 13.4 Q 4.6 12.7 7.75 14.7 Z', 'M 8.25 4.6 Q 11.2 2.7 15.4 3.3 L 15.6 13.4 Q 11.4 12.7 8.25 14.7 Z',
      'M 2.6 6.4 Q 4.6 6.0 6.6 6.8', 'M 2.6 9.0 Q 4.6 8.6 6.6 9.4', 'M 9.4 6.8 Q 11.4 6.0 13.4 6.4', 'M 9.4 9.4 Q 11.4 8.6 13.4 9.0'])],
    ['Circle', 'A wheel side\'s rim and the wheel\'s middle.', [arc(0, 0, 1, 48)]],
    ['Square', 'An edge key\'s rim.', [roundSq(.24)]],
    ['Cross', 'The cross over a mode not known yet.', [[[-.42, -.42], [.42, .42]], [[.42, -.42], [-.42, .42]]]],
    ['Alarms', 'The alarms: the meteor coming, as the thought over the drone draws it.', thoughtPictures().meteor],
  ];
  const num = v => { const r = Math.round(v * 1e4) / 1e4; return (Object.is(r, -0) ? 0 : r) + 'f'; };
  const run = l => `[${l.map(p => `${num(p[0])}, ${num(p[1])}`).join(', ')}]`;
  const icons = ICONS.map(([name, says, polys]) => {
    const { one, bridges } = joinOne(polys);
    return `    /// <summary>${says}</summary>
    public static readonly IconLines ${name} = new(
        [
${polys.map(p => `            ${run(p)},`).join('\n')}
        ],
        ${run(one)},
        [${bridges.join(', ')}]);`;
  });
  const cs = `// Generated by tools/anime-art/subjects/drone.js: export again, never edit.
namespace Stillwake.Game.Shared;

/// <summary>
/// The thread icons as the pick «Одна нить» drew them (thread-icons.js, candidate oneThread): each icon's parts as polylines in a box
/// -1..1, y down, each a run of x, y pairs; the one thread through them all; and the points of it a bridge reaches.
/// </summary>
public static class IconInk
{
    /// <summary>A waiting key's pulse, a second.</summary>
    public const float Pulse = 1.2f;

${icons.join('\n\n')}
}
`;
  writeText('../../shared/logic/IconInk.cs', cs);
}

Subjects.drone = function () {
  threadsInk();
  iconsInk();
  const cs = CoreBox * K, coreSheet = document.createElement('canvas');
  [coreSheet.width, coreSheet.height] = [cs, cs];
  const cc = coreSheet.getContext('2d');
  cc.grain = grainOf(cc);
  cc.translate(cs / 2, cs / 2); core(cc);
  out('drone_core', coreSheet);

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
// The threads' numbers and the icons alone, for a run with no browser (they need no canvas).
Subjects.drone.ink = () => { threadsInk(); iconsInk(); };

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

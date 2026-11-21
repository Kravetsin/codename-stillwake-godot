// The game's name on the title, as picked (title-logo, «Пепел», take «Отражение»): CASTOFF in Oswald, pale cold letters with
// grain and a warm edge toward the sun, baked; the times each pixel leaves, as a map; and the dust grains with their drift. The
// game plays the loop (TitleName): the word turns to dust, the dust slows and stops, then comes back the same way. The code is the
// pick page's (tools/pick-kit/pages/title-logo.js), at twice its scale. The font, OFL, is in tools/anime-art/fonts/oswald.
(function () {
'use strict';
const TAU = Math.PI * 2, WORD = 'CASTOFF', S = 2, LW = 1280, LH = 720;
const c01 = v => v < 0 ? 0 : v > 1 ? 1 : v;
const sm = u => { u = c01(u); return u * u * (3 - 2 * u); };
const lerp = (a, b, k) => a + (b - a) * k;
const css = (c, a = 1) => `rgba(${c[0] | 0},${c[1] | 0},${c[2] | 0},${a < .001 ? 0 : a > 1 ? 1 : +a.toFixed(3)})`;
function rng(seed) { let s = (seed * 2654435761) >>> 0 || 1; return () => { s ^= s << 13; s >>>= 0; s ^= s >>> 17; s ^= s << 5; s >>>= 0; return s / 4294967296; }; }
const unit = (x, y) => { const l = Math.hypot(x, y) || 1; return [x / l, y / l]; };
const SUN = [840, 290], WARM = [255, 210, 156];
const X0 = 84, BASE = 250;
// the loop, seconds: whole until D0, dissolving for DIS, apart, gathering from G0 for GAT, whole again to CYCLE
const D0 = 7.5, DIS = 3.5, G0 = 13.5, GAT = 3.5, GE = G0 + GAT, CYCLE = 19.5;
const SOFT = .3, TE_LO = D0 + SOFT + .05, TE_HI = D0 + DIS - .15, PACE = 1;
const MID = (D0 + GE) / 2, EPS = 1.2;
const F = { fam: 'Oswald', upm: 1000, cap: 810, px: 88, track: 9, g: { C: [515, 48, 470], A: [492, 19, 473], S: [473, 39, 448], T: [413, 15, 398], O: [539, 48, 491], F: [391, 63, 376] } };
const L = (() => {
  const size = F.px * F.upm / F.cap, k = size / F.upm, xs = [], ink = [];
  let x = X0 - F.g.C[1] * k;
  for (const ch of WORD) { const g = F.g[ch]; xs.push(x); ink.push([x + g[1] * k, x + g[2] * k]); x += g[0] * k + F.track; }
  const top = BASE - F.px, right = ink[6][1];
  return { xs, ink, top, right, mid: BASE - F.px / 2, font: `${size.toFixed(2)}px "${F.fam}"`, WB: [X0 - 40, top - 40, right - X0 + 80, F.px + 80] };
})();
const XL = L.ink[0][0], WW = L.right - XL, nx = x => c01((x - XL) / WW);

const canvas = (w, h) => { const v = document.createElement('canvas'); v.width = Math.max(1, Math.round(w)); v.height = Math.max(1, Math.round(h)); return { v, g: v.getContext('2d') }; };
const hash = (x, y, s) => { let h = (x * 374761393 + y * 668265263 + s * 1442695041) | 0; h = Math.imul(h ^ (h >>> 13), 1274126177); return ((h ^ (h >>> 16)) >>> 0) / 4294967296; };
function vnoise(x, y, s) {
  const xi = Math.floor(x), yi = Math.floor(y), fx = x - xi, fy = y - yi, u = fx * fx * (3 - 2 * fx), v = fy * fy * (3 - 2 * fy);
  const a = hash(xi, yi, s), b = hash(xi + 1, yi, s), c = hash(xi, yi + 1, s), d = hash(xi + 1, yi + 1, s);
  return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
}
const REACH = [[0, 1], [.15, .75], [.35, .4], [.6, .15], [1, .03]], REACH_R = 900;
function sunMask(g) {
  const r = g.createRadialGradient(SUN[0], SUN[1], 0, SUN[0], SUN[1], REACH_R);
  for (const [u, a] of REACH) r.addColorStop(u, `rgba(0,0,0,${a})`);
  g.globalCompositeOperation = 'destination-in'; g.fillStyle = r; g.fillRect(-100, -100, LW + 200, LH + 200); g.globalCompositeOperation = 'source-over';
}
function tint(src, col, a = 1) { const b = canvas(src.width, src.height); b.g.drawImage(src, 0, 0); b.g.globalCompositeOperation = 'source-in'; b.g.fillStyle = css(col, a); b.g.fillRect(0, 0, b.v.width, b.v.height); b.g.globalCompositeOperation = 'source-over'; return b; }
function edgeOf(src, dx, dy, col, a = 1) { const b = tint(src, col, a); b.g.globalCompositeOperation = 'destination-out'; b.g.drawImage(src, dx, dy); b.g.globalCompositeOperation = 'source-over'; return b; }
function letters(g, s, fill) {
  g.setTransform(s, 0, 0, s, -L.WB[0] * s, -L.WB[1] * s);
  g.font = L.font; g.textAlign = 'left'; g.textBaseline = 'alphabetic'; g.fillStyle = fill;
  for (let i = 0; i < WORD.length; i++) g.fillText(WORD[i], L.xs[i], BASE);
}
// when a point of the word leaves, 0..1 between TE_LO and TE_HI: right first, a little noise
const u = (x, y) => .8 * (1 - nx(x)) + .2 * vnoise(x / 3, y / 3, 7);
const teOf = (x, y) => lerp(TE_LO, TE_HI, c01(u(x, y)));

function grains() {
  const R = rng(37), W = Math.ceil(L.WB[2]), H = Math.ceil(L.WB[3]), b = canvas(W, H);
  letters(b.g, 1, '#fff');
  const ya = Math.floor(L.top - 14 - L.WB[1]), yb = Math.ceil(BASE + 14 - L.WB[1]), im = b.g.getImageData(0, ya, W, yb - ya), list = [];
  for (let y = 0; y < im.height; y++) for (let x = 0; x < im.width; x++) {
    const a = im.data[(y * im.width + x) * 4 + 3]; if (a < 90) continue;
    list.push([L.WB[0] + x + .5, L.WB[1] + ya + y + .5, a / 255]);
  }
  const keep = Math.min(1, 3200 / Math.max(1, list.length)), out = [];
  for (const [x, y, a] of list) {
    if (R() > keep) continue;
    const te = teOf(x, y), size = R(), r = size < .07 ? 1.3 : size < .3 ? .8 : .5;
    const g = { ox: x + (R() - .5) * .8, oy: y + (R() - .5) * .8, td: te - .15 + .1 * R(), a0: a * (.25 + .75 * Math.pow(R(), .8)) * (r > 1 ? .7 : 1), r, glint: false };
    if (R() < .06) { g.glint = true; g.a0 = Math.max(g.a0, .85 * a); }
    g.vx = PACE * (8 + 10 * R()); g.vy = -PACE * (1 + 3 * R()); g.A = .8 + 2 * R(); g.P = 5 + 4 * R();
    out.push(g);
  }
  return out;
}
function grainTex() {
  const N = 96, b = canvas(N, N), im = b.g.createImageData(N, N), r = rng(31);
  const blot = (x, y) => .5 + .25 * Math.sin(TAU * (x * 2 + y) / N + 1.3) * Math.sin(TAU * (y * 3 - x) / N + .4) + .25 * Math.sin(TAU * (x + y * 2) / N * 2 + 2.1);
  for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) {
    const i = (y * N + x) * 4, dark = r() < .55, a = dark ? r() * 26 + blot(x, y) * 22 : r() * 14;
    im.data[i] = im.data[i + 1] = im.data[i + 2] = dark ? 0 : 255; im.data[i + 3] = Math.floor(a);
  }
  b.g.putImageData(im, 0, 0);
  return b.v;
}
function word() {
  const b = canvas(L.WB[2] * S, L.WB[3] * S), g = b.g;
  letters(g, S, '#fff');
  const fill = g.createLinearGradient(0, L.top, 0, BASE); fill.addColorStop(0, css([224, 229, 235])); fill.addColorStop(1, css([174, 184, 197]));
  g.globalCompositeOperation = 'source-in'; g.fillStyle = fill; g.fillRect(L.WB[0], L.WB[1], L.WB[2], L.WB[3]);
  g.globalCompositeOperation = 'source-atop'; g.setTransform(1, 0, 0, 1, 0, 0); g.globalAlpha = .5; g.fillStyle = g.createPattern(grainTex(), 'repeat'); g.fillRect(0, 0, b.v.width, b.v.height);
  g.globalAlpha = 1; g.globalCompositeOperation = 'source-over';
  const l = unit(SUN[0] - (L.right - 60), SUN[1] - L.mid), rim = edgeOf(b.v, -l[0] * 1.6 * S, -l[1] * 1.6 * S, WARM);
  rim.g.setTransform(S, 0, 0, S, -L.WB[0] * S, -L.WB[1] * S); sunMask(rim.g); rim.g.setTransform(1, 0, 0, 1, 0, 0);
  g.drawImage(rim.v, 0, 0);
  return b.v;
}
// the leaving map: red is when each pixel leaves, 0..255 for TE_LO..TE_HI; opaque, so nothing is premultiplied away
function leaving(w, h) {
  const b = canvas(w, h), im = b.g.createImageData(w, h);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const i = (y * w + x) * 4, v = Math.round(255 * c01(u(L.WB[0] + (x + .5) / S, L.WB[1] + (y + .5) / S)));
    im.data[i] = v; im.data[i + 1] = v; im.data[i + 2] = v; im.data[i + 3] = 255;
  }
  b.g.putImageData(im, 0, 0);
  return b.v;
}

Subjects.titlename = async function () {
  const face = new FontFace(F.fam, `url("${ANIME_ROOT}/fonts/oswald/Oswald%5Bwght%5D.ttf")`);
  document.fonts.add(await face.load());
  const w = word();
  out('menu/title_name', w);
  out('menu/title_name_leaving', leaving(w.width, w.height));
  const gs = grains(), f = new Float32Array(gs.length * 10);
  gs.forEach((g, i) => f.set([g.ox, g.oy, g.td, g.a0, g.r, g.glint ? 1 : 0, g.vx, g.vy, g.A, g.P], i * 10));
  let bin = ''; const bytes = new Uint8Array(f.buffer); for (let i = 0; i < bytes.length; i++) bin += String.fromCharCode(bytes[i]);
  const n = (v) => (Number.isInteger(v) ? v.toFixed(1) : String(v));
  writeText('../../menu/logic/TitleNameInk.cs', `// Generated by tools/anime-art/subjects/titlename.js: export again, never edit.
namespace Stillwake.Game.Menu;

/// <summary>
/// The game's name as picked (title-logo, «Пепел», «Отражение»), in the pick page's pixels: its pictures in <c>art/hd/menu</c>
/// (the word, and when each of its pixels leaves), the loop's times and the dust's grains.
/// </summary>
public static class TitleNameInk
{
    /// <summary>The pictures' box in the page's pixels: left, top, width, height; they are drawn at <see cref="Scale"/> times that.</summary>
    public const double BoxX = ${n(L.WB[0])}, BoxY = ${n(L.WB[1])}, BoxW = ${n(L.WB[2])}, BoxH = ${n(L.WB[3])}, Scale = ${n(S)};
    /// <summary>The loop in seconds: whole until <see cref="D0"/>, whole again from <see cref="Ge"/>, <see cref="Cycle"/> long; a pixel fades over <see cref="Soft"/>; the leaving map spans <see cref="TeLo"/>..<see cref="TeHi"/>; the dust turns back at <see cref="Mid"/>, slowing over <see cref="Eps"/>.</summary>
    public const double D0 = ${n(D0)}, Ge = ${n(GE)}, Cycle = ${n(CYCLE)}, Soft = ${n(SOFT)}, TeLo = ${n(TE_LO)}, TeHi = ${n(TE_HI)}, Mid = ${n(MID)}, Eps = ${n(EPS)};
    /// <summary>The sun on the page, how far its warm light reaches, and its falloff as (distance share, light) points.</summary>
    public const double SunX = ${n(SUN[0])}, SunY = ${n(SUN[1])}, ReachR = ${n(REACH_R)};
    public static readonly (double U, double Light)[] Reach = [${REACH.map(([a, b]) => '(' + n(a) + ', ' + n(b) + ')').join(', ')}];
    /// <summary>The dust's cold colour and its warm one near the sun, 0..255.</summary>
    public static readonly int[] Dust = [${[214, 222, 232].join(', ')}], Warm = [${WARM.join(', ')}];
    /// <summary>The grains, ten floats each: home x, y; when it leaves; its alpha; its half size; a glint (1); drift vx, vy; sway amplitude, period.</summary>
    public const string Grains = "${btoa(bin)}";
}
`);
};
})();

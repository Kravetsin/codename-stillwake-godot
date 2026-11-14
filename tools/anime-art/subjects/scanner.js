// The scanner (2026-11-07-the-scanner), the picked crest: a swept fin along the helmet's crown, hugging each helmet's dome, its
// front edge dark glass with three green dots running down it. Worn in the lamp's mount; the dots are drawn again unlit, one at a
// time (hero_*scanner_lit_<n>); where it sits goes to the game (ScannerInk.cs).
(function () {
'use strict';
const { ell, rr, poly, bake, canvas } = Anime;
const band = (x0, y0, x1, y1) => poly(x0, y0, x1, y0, x1, y1, x0, y1);

// The hero's materials (hero.js).
const G = { base: '#3f4656', shade: '#262a44', deep: '#191c32', lite: '#67718a', spec: '#c4d2e4' };
const PK = { base: '#7c8699', shade: '#474e6c', deep: '#2f3350', lite: '#aab4c6', spec: '#ffffff' };
const GL = { base: '#1a2028', shade: '#10141b', deep: '#0a0d12', lite: '#2c3844' };
const UNLIT = '#0e1d16', GREEN = [124, 240, 150];
const LIGHT = [0, -1], Px = 4, MARGIN = 1, W = 7, H = 7;

// The helmets' tops in their pictures' pixels: hero.js's basic shell's two curves, the bell's ellipse and the quilted hood's crown (quilted.js).
function cubic(p, n) { const out = []; for (let i = 0; i <= n; i++) { const t = i / n, u = 1 - t; out.push([u * u * u * p[0] + 3 * u * u * t * p[2] + 3 * u * t * t * p[4] + t * t * t * p[6], u * u * u * p[1] + 3 * u * u * t * p[3] + 3 * u * t * t * p[5] + t * t * t * p[7]]); } return out; }
const BASIC_TOP = [...cubic([1.5, 6, 1.7, 3, 3.8, 1.1, 7, 1], 24), ...cubic([7, 1, 10, .9, 12.3, 2.8, 12.6, 5.8], 24).slice(1)];
function basicTop(x) {
  const p = BASIC_TOP;
  if (x <= p[0][0]) return p[0][1];
  for (let i = 1; i < p.length; i++) if (x <= p[i][0]) { const a = p[i - 1], b = p[i], k = (x - a[0]) / (b[0] - a[0] || 1); return a[1] + (b[1] - a[1]) * k; }
  return p[p.length - 1][1];
}
const heavyTop = (x) => { const d = (x - 6.4) / 6.1; return 5.4 - 6 * Math.sqrt(Math.max(0, 1 - d * d)); };
// Each helmet: its top, and where the crest's picture's top left sits on the helmet's picture (inside its margin).
const HELMETS = {
  helmet: { name: 'hero_scanner', top: basicTop, on: [6.2, -.8] },
  heavyHelmet: { name: 'hero_heavyHelmet_scanner', top: heavyTop, on: [6.2, -2.6] },
  quiltedHelmet: { name: 'hero_quiltedHelmet_scanner', top: (x) => QuiltedKit.top(x), on: [6.2, -3] },
};

const n2 = (v) => +v.toFixed(2);
/** The crest on helmet h: the dome under it, its top edge, the glass's top and height. */
function geom(h) {
  const { top, on } = HELMETS[h];
  const base = (x) => top(x + on[0]) - on[1], ridge = (x) => base(x) - (.5 + 3.3 * Math.pow((x - .2) / 5.6, 1.4));
  const yt = ridge(5.8), hs = base(5.8) - yt - .7;
  return { base, ridge, yt, hs };
}
/** The three dots on the glass, top to bottom: middle and radii. */
const dots = (h) => { const { yt, hs } = geom(h); return [0, 1, 2].map((k) => [5.92, yt + .3 + hs * (k + .5) / 3, .2, .26]); };

function crest(a, h) {
  const { base, ridge, yt, hs } = geom(h), xs = [];
  for (let x = .2; x < 5.8; x += .4) xs.push(x);
  xs.push(5.8);
  let d = `M.2 ${n2(base(.2) + .15)}`;
  for (const x of xs) d += `L${n2(x)} ${n2(ridge(x))}`;
  d += `L6.3 ${n2(yt + .15)}L6.3 ${n2(base(5.8) + .1)}`;
  for (const x of xs.slice().reverse()) d += `L${n2(x)} ${n2(base(x) + .3)}`;
  d += 'Z';
  let seam = '';
  xs.filter((x) => x > .9 && x < 5.4).forEach((x, i) => (seam += `${i ? 'L' : 'M'}${n2(x)} ${n2(ridge(x) + .9)}`));
  let lit = '';
  xs.forEach((x, i) => (lit += `${i ? 'L' : 'M'}${n2(x)} ${n2(ridge(x) - .1)}`));
  for (const x of xs.slice().reverse()) lit += `L${n2(x)} ${n2(ridge(x) + .45)}`;
  a.shape(d, PK, { shade: .35, lite: lit + 'Z', lines: [seam, `M3.4 ${n2(ridge(3.4) + .3)}L3.4 ${n2(base(3.4))}`] });
  let rail = `M.5 ${n2(base(.5) - .2)}`;
  for (const x of xs.filter((x) => x >= .5)) rail += `L${n2(x)} ${n2(base(x) - .3)}`;
  for (const x of xs.filter((x) => x >= .5).reverse()) rail += `L${n2(x)} ${n2(base(x) + .35)}`;
  a.shape(rail + 'Z', G, { outer: false, shade: .2 });
  a.shape(rr(5.55, yt + .3, .75, hs, .25), GL, { outer: false, lite: band(5.55, yt + .3, 6.3, yt + .55) });
  for (const [x, y, rx, ry] of dots(h)) a.flat(ell(x, y, rx, ry), UNLIT);
}

/** One dot lit, drawn straight on a canvas the picture's size and margin: a soft halo ahead of it, then its core. */
function lit(h, k) {
  const c = canvas((W + 2 * MARGIN) * Px, (H + 2 * MARGIN) * Px), x = c.getContext('2d');
  x.setTransform(Px, 0, 0, Px, MARGIN * Px, MARGIN * Px);
  const [cx, cy, rx, ry] = dots(h)[k], g = x.createRadialGradient(cx + .18, cy, 0, cx + .18, cy, 1.2);
  g.addColorStop(0, `rgba(${GREEN},.35)`); g.addColorStop(1, `rgba(${GREEN},0)`);
  x.fillStyle = g; x.fillRect(cx - 1.2, cy - 1.2, 2.6, 2.4);
  x.fillStyle = `rgb(${GREEN})`; x.beginPath(); x.ellipse(cx, cy, rx, ry, 0, 0, Math.PI * 2); x.fill();
  return c;
}

Subjects.scanner = function () {
  for (const h of Object.keys(HELMETS)) {
    out(HELMETS[h].name, bake({ w: W, h: H, margin: MARGIN, light: LIGHT, draw: (a) => crest(a, h) }));
    for (let k = 0; k < 3; k++) out(`${HELMETS[h].name}_lit_${k}`, lit(h, k));
  }
  // The card: the basic helmet's crest fitted to 16 px, its top dot lit.
  const [x0, y0, x1, y1] = [0, 1, 6.6, 5.4], w = x1 - x0, hh = y1 - y0, u = Math.max(w, hh) / 14;
  const top = dots('helmet')[0];
  out('items/scanner', bake({ w: 16, h: 16, light: [-.6, -.8], units: u, shift: [(16 * u - w) / 2 - x0, (16 * u - hh) / 2 - y0],
    W: { outer: .9 * u, edge: .26 * u, panel: .14 * u },
    draw: (a) => { crest(a, 'helmet'); a.glow(ell(top[0], top[1], top[2], top[3]), '#7cf096'); } }));

  const pt = (v) => v.toFixed(2).replace(/\.?0+$/, '') || '0';
  writeText('../../shared/logic/ScannerInk.cs', `// Generated by tools/anime-art/subjects/scanner.js: export again, never edit.
namespace Stillwake.Game.Shared;

/// <summary>The picked scanner's numbers (crest, 2026-11-07-the-scanner): where its picture sits on each helmet.</summary>
public static class ScannerInk
{
    /// <summary>The world's pixels its pictures reach past their box on every side.</summary>
    public const int Margin = ${MARGIN};

    /// <summary>Where the picture's box's top left sits on the basic helmet's picture, in its pixels.</summary>
    public static readonly (double X, double Y) On = (${pt(HELMETS.helmet.on[0])}, ${pt(HELMETS.helmet.on[1])});

    /// <summary>Where it sits on the heavy helmet's picture.</summary>
    public static readonly (double X, double Y) OnHeavy = (${pt(HELMETS.heavyHelmet.on[0])}, ${pt(HELMETS.heavyHelmet.on[1])});

    /// <summary>Where it sits on the quilted helmet's picture.</summary>
    public static readonly (double X, double Y) OnQuilted = (${pt(HELMETS.quiltedHelmet.on[0])}, ${pt(HELMETS.quiltedHelmet.on[1])});
}
`);
};
})();

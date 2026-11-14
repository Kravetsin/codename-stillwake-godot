// The quilted helmet (2026-11-14-quilted-helmet-and-traced-art), as picked: a padded canvas hood of rolls strapped to a welder's
// plate, its glass dark. Traced from an AI-made reference (refs/quilted.png) for its form only; drawn by the engine (traced.js).
// On the hero in the helmets' box and joint (hero.js's cut), facing +x, its glass the visor; its card the trace's own fit.
(function () {
'use strict';
const Margin = 3;
/** Where the hood sits on the head (hero.js's 13 × 12 box), in its pixels: its front edge, its crown and how tall it stands, the last roll a collar on the shoulders as the bell's breastplate. */
const HEAD = { right: 13, top: -0.8, height: 15.6 };

/** The trace's scale and offset onto the head. */
function onHead() {
  const [x0, y0, x1, y1] = TRACED.quilted.fit.box, s = HEAD.height / (y1 - y0 + 1);
  return { s, at: [HEAD.right - (x1 + 1) * s, HEAD.top - y0 * s] };
}

let hull = null;
/** The hood's top in the head's pixels at x: the highest of its outline there, bridged over the dips between rolls, as a crest resting on it. */
function top(x) {
  hull = hull || crown();
  if (x <= hull[0][0]) return hull[0][1];
  for (let i = 1; i < hull.length; i++) if (x <= hull[i][0]) { const [a, b] = [hull[i - 1], hull[i]]; return a[1] + (b[1] - a[1]) * (x - a[0]) / (b[0] - a[0]); }
  return hull[hull.length - 1][1];
}

/** The outline's upper hull, left to right: the topmost pixel of each column, no point kept below a chord between two others. */
function crown() {
  const { s, at } = onHead(), body = Traced.placed('quilted', s, at)[0], k = 20;
  const c = Anime.canvas((13 + 2 * Margin) * k, (12 + 2 * Margin) * k), g = c.getContext('2d');
  g.setTransform(k, 0, 0, k, Margin * k, Margin * k);
  g.fill(new Path2D(body.d));
  const data = g.getImageData(0, 0, c.width, c.height).data, pts = [];
  for (let i = 0; i < c.width; i++) for (let j = 0; j < c.height; j++) if (data[(j * c.width + i) * 4 + 3] > 127) { pts.push([i / k - Margin, j / k - Margin]); break; }
  const hull = [];
  for (const p of pts) {
    while (hull.length >= 2) {
      const [a, b] = [hull[hull.length - 2], hull[hull.length - 1]];
      if ((b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0]) <= 0) hull.pop(); else break;
    }
    hull.push(p);
  }
  return hull;
}

Subjects.quilted = function () {
  const { s, at } = onHead(), ops = Traced.placed('quilted', s, at), u = TRACED.quilted.fit.units;
  const glass = ops.filter((o) => o.mat === 'glass').map((o) => o.d).join('');
  const [shell, visor] = HeroKit.cut(Traced.drawer('quilted', ops, u * s), glass, []);
  out('hero_quiltedHelmet_shell', shell);
  out('hero_quiltedHelmet_visor', visor);
  out('items/quiltedHelmet', Traced.icon('quilted'));
};

window.QuiltedKit = { top };
})();

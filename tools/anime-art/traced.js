// Draws a traced subject (trace.js, README's Traced subjects) by the engine: its ops are in the reference's pixels; the line, the
// paint (the palette's materials) and the finish are the engine's, as for any drawn subject. Loaded after wash.js; TRACED holds
// every traced/<name>.json with its palette's materials (export.js).
(function () {
'use strict';
/** A path's every point scaled by s (or [sx, sy]) and moved by [x, y]: the tracer writes only absolute M, C and Z. */
const move = (d, s, x, y) => {
  const [sx, sy] = Array.isArray(s) ? s : [s, s];
  let k = 0;
  return d.replace(/-?\d*\.?\d+/g, (v) => String(+(k++ % 2 ? +v * sy + y : +v * sx + x).toFixed(3)));
};

/** The ops of TRACED[name] scaled by s (or [sx, sy]) and moved by at, from the reference's pixels into a picture's units. */
function placed(name, s = 1, at = [0, 0]) {
  const t = TRACED[name], m = (d) => move(d, s, at[0], at[1]);
  return t.ops.map((o) => ({ ...o, d: m(o.d), lite: (o.lite || []).map(m) }));
}

/**
 * draw(a) for ops of TRACED[name], each in its palette material. A crescent is traced in the icon's world pixels; k is how many of
 * the ops' units make one (the fit's units for the icon, times the scale for ops placed elsewhere).
 */
function drawer(name, ops, k) {
  const M = TRACED[name].materials;
  return (a) => {
    for (const o of ops) {
      const m = o.mat ? M[o.mat].mat : null;
      if (o.op === 'shape') {
        const opt = {};
        if (o.shade) opt.shade = o.shade * k;
        if (o.deep) opt.deep = o.deep * k;
        if (o.lite.length) opt.lite = o.lite;
        a.shape(o.d, m, opt);
      } else if (o.op === 'flat') a.flat(o.d, m.base);
      else if (o.op === 'line') a.line(o.d, { w: o.w || 1 });
    }
  };
}

/** The item's icon: the trace fitted into 16 logical pixels as the tracer measured it, its pen per world pixel as every icon's. */
function icon(name) {
  const t = TRACED[name], u = t.fit.units, L = Anime.LINE;
  return Anime.bake({ w: 16, h: 16, light: [-0.6, -0.8], units: u, shift: t.fit.shift, W: { outer: L.outer * u, edge: L.edge * u, panel: L.panel * u },
    draw: drawer(name, placed(name), u) });
}

/** A piece's fit box stretched over `box` ([x0, y0, x1, y1], in a picture's units): the scale [sx, sy] and the move, for placed(). */
function into(name, box) {
  const [x0, y0, x1, y1] = TRACED[name].fit.box, s = [(box[2] - box[0]) / (x1 - x0 + 1), (box[3] - box[1]) / (y1 - y0 + 1)];
  return { s, at: [box[0] - x0 * s[0], box[1] - y0 * s[1]] };
}

/** Where a traced piece of headgear sits on the head (hero.js's 13 × 12 box, in its pixels): its fit box head.height tall, its right edge at head.right, its top at head.top. */
function onHead(name, head) {
  const [x0, y0, x1, y1] = TRACED[name].fit.box, s = head.height / (y1 - y0 + 1);
  return { s, at: [head.right - (x1 + 1) * s, head.top - y0 * s] };
}

/** Its top in the head's pixels at x, as a crest or lamp rests on it: the outline's upper hull, bridged over its dips. */
function crown(name, head) {
  let hull = null;
  return (x) => {
    hull = hull || upperHull(name, head);
    if (x <= hull[0][0]) return hull[0][1];
    for (let i = 1; i < hull.length; i++) if (x <= hull[i][0]) { const [a, b] = [hull[i - 1], hull[i]]; return a[1] + (b[1] - a[1]) * (x - a[0]) / (b[0] - a[0]); }
    return hull[hull.length - 1][1];
  };
}

/** The outline's upper hull on the head, left to right: the topmost pixel of each column, no point kept below a chord between two others. */
function upperHull(name, head) {
  const { s, at } = onHead(name, head), body = placed(name, s, at)[0], k = 20, M = 3;
  const c = Anime.canvas((13 + 2 * M) * k, (12 + 2 * M) * k), g = c.getContext('2d');
  g.setTransform(k, 0, 0, k, M * k, M * k);
  g.fill(new Path2D(body.d));
  const data = g.getImageData(0, 0, c.width, c.height).data, pts = [];
  for (let i = 0; i < c.width; i++) for (let j = 0; j < c.height; j++) if (data[(j * c.width + i) * 4 + 3] > 127) { pts.push([i / k - M, j / k - M]); break; }
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

window.Traced = { placed, drawer, icon, into, onHead, crown };
})();

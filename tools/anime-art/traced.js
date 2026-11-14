// Draws a traced subject (trace.js, README's Traced subjects) by the engine: its ops are in the reference's pixels; the line, the
// paint (the palette's materials) and the finish are the engine's, as for any drawn subject. Loaded after wash.js; TRACED holds
// every traced/<name>.json with its palette's materials (export.js).
(function () {
'use strict';
/** A path's every point scaled by s and moved by [x, y]: the tracer writes only absolute M, C and Z. */
const move = (d, s, x, y) => { let k = 0; return d.replace(/-?\d*\.?\d+/g, (v) => String(+(+v * s + (k++ % 2 ? y : x)).toFixed(3))); };

/** The ops of TRACED[name] scaled by s and moved by at, from the reference's pixels into a picture's units. */
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

window.Traced = { placed, drawer, icon };
})();

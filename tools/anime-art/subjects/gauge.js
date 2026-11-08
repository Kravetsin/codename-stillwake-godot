// The suit gauge's lamps, as picked (the ink-signs ADR): hand-cut lamps with an ink hairline and light in hard steps, five shapes in five
// tones (unlit, the air's sky, danger's red, pale for the light running over them, white for what was just lost), 17 by 10 for normal
// slots and 26 by 10 for large, a pixel of margin for the line. Laid one a cell by WristGauge, which cuts the lit one at the fill.
(function () {
'use strict';
const { poly, rng } = Anime;
const TOP = [0, -1], SHAPES = 5;

const rgb = (h) => [parseInt(h.substr(1, 2), 16), parseInt(h.substr(3, 2), 16), parseInt(h.substr(5, 2), 16)];
const hex = (c) => '#' + c.map((v) => Math.max(0, Math.min(255, Math.round(v))).toString(16).padStart(2, '0')).join('');
/** The finish greys paint; a screen's light gets its saturation back first, so the air and the danger stay full. */
const paint = (h) => { const c = rgb(h).map((v) => v / 0.97), l = 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]; return hex(c.map((v) => l + (v - l) / 0.8)); };
const tone = (base, lite, shade, deep) => ({ base: paint(base), lite: paint(lite), shade: paint(shade), deep: paint(deep), spec: '#ffffff' });
const TONES = {
  dark: { base: '#18223c', lite: '#2a3856', shade: '#111a2f', deep: '#0c1325', spec: '#2c3b5a' },
  sky: tone('#38bdf8', '#c4f0ff', '#1d8cc2', '#146893'),
  red: tone('#ef4444', '#ffb8b0', '#b42828', '#841b20'),
  pale: tone('#dcf5ff', '#ffffff', '#b2e2f6', '#8ccbe6'),
  white: { base: '#f3f6f9', lite: '#ffffff', shade: '#cfd8e0', deep: '#aeb9c4', spec: '#ffffff' },
};

/** Lamp v's outline, w wide: a slightly leaning, slightly crooked box with one corner cut. */
function lampPath(v, w) {
  const r = rng(4100 + v * 37), j = (s) => (r() - 0.5) * s, lean = j(1.6), e = w - 0.6;
  const pts = [[0.6 + lean / 2 + j(0.5), 0.7 + j(0.6)], [e + lean / 2 + j(0.5), 0.7 + j(0.6)], [e - lean / 2 + j(0.5), 9.3 + j(0.5)], [0.6 - lean / 2 + j(0.5), 9.3 + j(0.5)]];
  const cut = Math.floor(r() * 4), k = 0.9 + r() * 0.9, made = [];
  pts.forEach((p, i) => {
    if (i !== cut) { made.push(p); return; }
    const a = pts[(i + 3) % 4], b = pts[(i + 1) % 4], la = Math.hypot(a[0] - p[0], a[1] - p[1]), lb = Math.hypot(b[0] - p[0], b[1] - p[1]);
    made.push([p[0] + (a[0] - p[0]) * k / la, p[1] + (a[1] - p[1]) * k / la], [p[0] + (b[0] - p[0]) * k / lb, p[1] + (b[1] - p[1]) * k / lb]);
  });
  return poly(made.flat());
}
const lamp = (v, w, M, lit) => (a) => {
  const d = lampPath(v, w), r = rng(900 + v * 11), s = (r() - 0.5) * 0.8, k = w / 17, R = w + 1;
  if (!lit) { a.shape(d, M, { lite: poly(-1, -1, R, -1, R, 1.7 + s, -1, 1.9 - s), shade: poly(-1, 7.6 - s, R, 7.4 + s, R, 11, -1, 11) }); return; }
  a.shape(d, M, {
    lite: poly(-1, -1, R, -1, R, 2.6 + s, -1, 3 - s), shade: poly(-1, 7 + s, R, 6.6 - s, R, 11, -1, 11),
    deep: poly(-1, 8.7, R, 8.5, R, 11, -1, 11), spec: poly(2.4 * k, 1.5, (7 + s * 2) * k, 1.3, (6.8 + s * 2) * k, 2.1, 2.5 * k, 2.3),
  });
};

Subjects.gauge = function () {
  for (const [name, w] of [['gauge_lamp', 17], ['gauge_lampWide', 26]])
    for (let v = 0; v < SHAPES; v++)
      for (const [t, M] of Object.entries(TONES))
        out(`${name}_${v}_${t}`, Anime.bake({ w, h: 10, margin: 1, light: TOP, draw: lamp(v, w, M, t !== 'dark') }));
};
})();

// The vitals' light, as picked (hud-vitals, look D "light and water"): a wash per tone whose rim is a slightly darker, wandering
// tide line, no frame. A strip 304 by 10 that the game cuts to a piece's length (its start, then its own 8 px end), and the meal's
// thin time light 24 by 3, stretched; two variants each so neighbours differ. The glow under a lit part is the game's, not baked.
(function () {
'use strict';
const { poly, rr } = Anime;
const TOP = [0, -1];

const rgb = (h) => [parseInt(h.substr(1, 2), 16), parseInt(h.substr(3, 2), 16), parseInt(h.substr(5, 2), 16)];
const hex = (c) => '#' + c.map((v) => Math.max(0, Math.min(255, Math.round(v))).toString(16).padStart(2, '0')).join('');
const mix = (a, b, k) => hex(rgb(a).map((v, i) => v + (rgb(b)[i] - v) * k));
/** The finish greys paint; a screen's light gets its saturation back first, so the air and the danger stay full. */
const paint = (h) => { const c = rgb(h).map((v) => v / 0.97), l = 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]; return hex(c.map((v) => l + (v - l) / 0.8)); };
/** A light: its body, its top light, and its rim a little darker than the body. */
const light = (c, lite, shade) => ({ base: paint(c), lite: paint(lite), shade: paint(mix(c, shade, 0.6)), deep: paint(shade) });
/** A light's dark wash, where it is empty: kept as painted. */
const dark = (c, lite, shade) => ({ base: c, lite, shade, deep: shade });
const TONES = {
  sky: light('#38bdf8', '#c4f0ff', '#1d8cc2'),
  red: light('#ff463c', '#ffb3ab', '#c22a22'),
  pale: light('#dcf5ff', '#ffffff', '#b2e2f6'),
  white: light('#f3f6f9', '#ffffff', '#cfd8e0'),
  green: light('#7ee36b', '#d4f8c8', '#4aa83c'),
  paleGreen: light('#dcfbd2', '#ffffff', '#b8eea8'),
  amber: light('#f2b84b', '#fbe2a8', '#b8862a'),
  time: light('#a4aec2', '#dce2ec', '#6e7890'),
  hunger: light('#d2a866', '#f0d4a0', '#a07a40'),
  darkSky: dark('#10283b', '#1a3a55', '#0b1c2b'),
  darkRed: dark('#3a1512', '#55201b', '#260c0a'),
  darkGreen: dark('#13301b', '#1d4628', '#0c2011'),
  darkMeal: dark('#1c2030', '#283048', '#121624'),
};
/** The rim: the band between the piece's edge and i pixels in, a slightly darker tide line the wash wanders over. */
const rim = (w, h, i) => `M-2 -2L${w + 2} -2L${w + 2} ${h + 2}L-2 ${h + 2}Z M${i} ${i}L${i} ${h - i}L${w - i} ${h - i}L${w - i} ${i}Z`;
const water = (w, h, inset, M) => (a) => {
  const r = Math.min(2.6, h / 2 - 0.1, w / 2 - 0.1);
  a.shape(rr(0, 0, w, h, r), M, { outer: false, edge: false, shade: rim(w, h, inset), lite: poly(-2, -2, w + 2, -2, w + 2, Math.min(2.6, h * 0.3), -2, Math.min(3, h * 0.35)) });
};

/** The meals' time lights, drawn thin; every other tone is a row's strip. */
const THIN = ['time', 'hunger', 'darkMeal'];

Subjects.gauge = function () {
  for (const [t, M] of Object.entries(TONES))
    for (let v = 0; v < 2; v++)
      if (THIN.includes(t)) out(`gauge_thin_${t}_${v}`, Anime.bake({ w: 24 + v, h: 3, margin: 2, light: TOP, draw: water(24 + v, 3, 0.7, M) }));
      else out(`gauge_strip_${t}_${v}`, Anime.bake({ w: 304 + v, h: 10, margin: 2, light: TOP, draw: water(304 + v, 10, 1.2, M) }));
};
})();

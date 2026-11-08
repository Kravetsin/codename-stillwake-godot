// The blasting charge (blasting-charge), the user's pick «Труба с серой»: an iron pipe packed with sulfur, screw caps, a short fuse that
// sparks once it is lit. One drawing serves the hand (hero_blastCharge, unlit), its icon (icons/, the fuse lit, drawn larger so the pen
// stays thin) and the flight: the hand's picture turning, the spark's four frames (shot_blastCharge_spark) unlit over it. ShotLooks.cs.
(function () {
'use strict';
const { ell, poly, star, canvas, bake } = Anime;

// Metals cool grey, the cord warm and worn; full colour only for the spark.
const IRON = { base: '#666c78', shade: '#474c58', deep: '#30343e', lite: '#868c98', spec: '#b4bac4', ink: '#0f111b' };
const CAST = { base: '#4c505a', shade: '#353841', deep: '#24262d', lite: '#686d78', spec: '#9aa0aa', ink: '#0d0e14' };
const SULF = { base: '#d8c66c', shade: '#aa9c58', deep: '#7c7244', lite: '#eee2a4', spec: '#f4ecc0', ink: '#29251a' };
const CORD = { base: '#5c5249', shade: '#3e3730', spec: '#7a7066', ink: '#14110e' };
const SPARK = '#f0a040';
const FLICK = [1, .35, .8, .15];

// A frame scaled by k round (cx, cy), in tenths of a world pixel, and paths through it.
const f2 = (v) => +v.toFixed(2);
const frame = (cx, cy, k) => (u, v) => [cx + u * k, cy + v * k];
const pt = (p) => `${f2(p[0])} ${f2(p[1])}`;
const quad = (F, u0, v0, u1, v1) => poly(...F(u0, v0), ...F(u1, v0), ...F(u1, v1), ...F(u0, v1));
const seg = (F, u0, v0, u1, v1) => `M${pt(F(u0, v0))}L${pt(F(u1, v1))}`;
const curve = (F, a, b, c, d) => `M${pt(F(...a))}C${pt(F(...b))} ${pt(F(...c))} ${pt(F(...d))}`;
const dot = (F, u, v, r, k) => { const p = F(u, v); return ell(p[0], p[1], r * k); };

/** The fuse's spark at phase t: a star that breathes, a few flecks thrown off it. */
function spark(a, p, k, t) {
  const f = FLICK[Math.floor(t * 4) % 4], s = Math.max(k, .8), r = (8 + 5 * f) * s;
  a.glow(star(p[0], p[1], r, .22), SPARK, { core: star(p[0], p[1], r * .42, .3) });
  const g = Anime.rng(97 + Math.floor(t * 4));
  for (let i = 0; i < 3; i++) { const an = g() * Math.PI * 2, d = (9 + g() * 8) * s; a.glow(ell(p[0] + Math.cos(an) * d, p[1] + Math.sin(an) * d, 1.6 * s), SPARK); }
}

/** The pipe along +u, its middle at (0, 0), the fuse at +u; mode 'held' unlit, 'icon' lit, 'flight' lit at phase t. */
function pipe(a, F, k, mode, t) {
  const L = 40, T = 15, CT = 19;
  a.shape(quad(F, -L, -T, L, T), IRON, { lite: quad(F, -L, -T, L, -T * .45), shade: quad(F, -L, T * .35, L, T), deep: quad(F, -L, T * .72, L, T), lines: [seg(F, -L + 4, 2, L - 4, 2)] });
  for (const [u0, u1] of [[-L - 13, -L + 4], [L - 4, L + 13]]) {
    a.shape(quad(F, u0, -CT, u1, CT), CAST, { lite: quad(F, u0, -CT, u1, -CT * .5), shade: quad(F, u0, CT * .4, u1, CT), lines: k > .7 ? [seg(F, (u0 + u1) / 2, -CT + 3, (u0 + u1) / 2, CT - 3)] : [] });
  }
  a.shape(quad(F, L + 13, -6, L + 19, 6), CAST, { shade: quad(F, L + 13, 2, L + 19, 6) });
  if (k > .7) {
    // Sulfur squeezed out of the cap's thread, rust grown at the far seam.
    a.flat(dot(F, -L + 5, -7, 3.2, k), SULF.base); a.flat(dot(F, -L + 5.5, 6, 2.6, k), SULF.shade); a.flat(dot(F, -L + 5, -8, 1.2, k), SULF.spec);
    const r = F(L - 10, 10); a.flat(ell(r[0], r[1], 6 * k, 3.4 * k), '#7a4430'); a.flat(ell(r[0] + .6, r[1] + .6, 2.6 * k, 1.5 * k), '#4a281c');
  }
  const tip = [L + 34, -13];
  a.tube(curve(F, [L + 19, 0], [L + 26, 1], [L + 28, -8], tip), 4.2 * k, CORD, { spec: false });
  if (mode !== 'held') spark(a, F(...tip), k, mode === 'icon' ? 0 : t);
}

// The hand's picture: W x H world px, the pipe's middle at MID (tenths), at the pick's scale in the hand. The spark's frames have SPARK_M
// more round it on every side, the pipe where it is in the hand's picture.
const W = 9, H = 4, MID = [38, 20], K = .55, SPARK_M = 2, FRAMES = 4, Px = 4;
const PEN = { outer: Anime.LINE.outer * 10, edge: Anime.LINE.edge * 10, panel: Anime.LINE.panel * 10 };
/** Only what glows of a drawing: the spark, unlit over the turning pipe. */
const sparkOnly = (a) => ({ ...a, shape() {}, flat() {}, line() {}, tube() {} });

Subjects.charge = function () {
  const held = frame(MID[0], MID[1], K);
  out('hero_blastCharge', bake({ w: W, h: H, units: 10, light: [0, -1], W: PEN, draw: (a) => pipe(a, held, K, 'held', 0) }));
  // Its icon as the pick drew it, twice the hand's size against the pen: turned up-right and fitted by ToolIcon.
  out('icons/hero_blastCharge', bake({ w: 16, h: 7, units: 10, light: [0, -1], W: PEN, px: Math.ceil(Anime.ICON_LONG / 16),
    draw: (a) => pipe(a, frame(60, 38, 1), 1, 'icon', 0) }));
  const strip = canvas(FRAMES * (W + 2 * SPARK_M) * Px, (H + 2 * SPARK_M) * Px);
  for (let i = 0; i < FRAMES; i++) {
    const f = bake({ w: W, h: H, margin: SPARK_M, units: 10, W: PEN, draw: (a) => pipe(sparkOnly(a), held, K, 'flight', i / FRAMES) });
    strip.getContext('2d').drawImage(f, i * (W + 2 * SPARK_M) * Px, 0);
  }
  out('shot_blastCharge_spark', strip);
};
})();

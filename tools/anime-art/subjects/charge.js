// The blasting charge (blasting-charge), the user's pick «Труба с серой», redrawn from the pick of five drawn references («Нынешний,
// перерисованный») and shipped raw (refs/blastCharge.png): a threaded iron pipe, a hex cap on each end, a yellow band round its
// middle, a plug in the right cap; the fuse out of the plug is drawn here, and sparks once it is lit. One drawing serves the hand (hero_blastCharge, unlit), its icon (icons/, the fuse lit, drawn larger so the pen
// stays thin) and the flight: the hand's picture turning, the spark's four frames (shot_blastCharge_spark) unlit over it. ShotLooks.cs.
(function () {
'use strict';
const { ell, star, canvas, bake } = Anime;

// The cord warm and worn; full colour only for the spark.
const CORD = { base: '#5c5249', shade: '#3e3730', spec: '#7a7066', ink: '#14110e' };
const SPARK = '#f0a040';
const FLICK = [1, .35, .8, .15];

// A frame scaled by k round (cx, cy), in tenths of a world pixel, and paths through it.
const f2 = (v) => +v.toFixed(2);
const frame = (cx, cy, k) => (u, v) => [cx + u * k, cy + v * k];
const pt = (p) => `${f2(p[0])} ${f2(p[1])}`;
const curve = (F, a, b, c, d) => `M${pt(F(...a))}C${pt(F(...b))} ${pt(F(...c))} ${pt(F(...d))}`;

/** The fuse's spark at phase t: a star that breathes, a few flecks thrown off it. */
function spark(a, p, k, t) {
  const f = FLICK[Math.floor(t * 4) % 4], s = Math.max(k, .8), r = (8 + 5 * f) * s;
  a.glow(star(p[0], p[1], r, .22), SPARK, { core: star(p[0], p[1], r * .42, .3) });
  const g = Anime.rng(97 + Math.floor(t * 4));
  for (let i = 0; i < 3; i++) { const an = g() * Math.PI * 2, d = (9 + g() * 8) * s; a.glow(ell(p[0] + Math.cos(an) * d, p[1] + Math.sin(an) * d, 1.6 * s), SPARK); }
}

/** The pipe along +u, its middle at (0, 0), the fuse at +u; mode 'held' unlit, 'icon' lit, 'flight' lit at phase t. Its picture spans
 * u from -53 (the left cap) to 59 (the plug's end), as the drawn pipe did, centred on v = 0. */
function pipe(a, F, k, mode, t) {
  const [x0, y0, x1, y1] = TRACED.blastCharge.fit.box, s = 112 * k / (x1 - x0 + 1), o = F(-53, 0), { at: [rx, ry], size: [rw, rh] } = TRACED.blastCharge.raw;
  const ax = o[0] - x0 * s, ay = o[1] - (y0 + y1 + 1) / 2 * s;
  a.image(Traced.picture('blastCharge'), ax + rx * s, ay + ry * s, rw * s, rh * s);
  const tip = [74, -13];
  a.tube(curve(F, [59, 0], [66, 1], [68, -8], tip), 4.2 * k, CORD, { spec: false });
  if (mode !== 'held') spark(a, F(...tip), k, mode === 'icon' ? 0 : t);
}

// The hand's picture: W x H world px, the pipe's middle at MID (tenths), at the pick's scale in the hand. The spark's frames have SPARK_M
// more round it on every side, the pipe where it is in the hand's picture.
const W = 9, H = 4, MID = [38, 20], K = .55, SPARK_M = 2, FRAMES = 4, Px = 4;
const PEN = { outer: Anime.LINE.outer * 10, edge: Anime.LINE.edge * 10, panel: Anime.LINE.panel * 10 };
/** Only what glows of a drawing: the spark, unlit over the turning pipe. */
const sparkOnly = (a) => ({ ...a, shape() {}, flat() {}, line() {}, tube() {}, image() {} });

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

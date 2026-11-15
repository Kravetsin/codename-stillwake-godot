// The basic and heavy helmets (2026-11-14-quilted-helmet-and-traced-art), as picked: a worn ochre work helmet, an oval glass in a
// bolted frame, a valve at the back, a rubber neck seal; and the armoured bell, gunmetal and brass, its wide glass behind three
// bars, two air tubes at the back, bolted to a collar on the shoulders. Traced from AI-made references (refs/helmet.png,
// refs/heavyHelmet.png) for their form only, drawn by the engine (traced.js), as the quilted helmet. On the hero in the helmets'
// box and joint (hero.js's cut), facing +x, the glass the visor; each card the trace's own fit.
(function () {
'use strict';
/** Where each sits on the head (hero.js's 13 × 12 box), in its pixels: its front edge, its top and how tall it stands, the seal on the neck, the bell's collar on the shoulders. */
const HEAD = {
  helmet: { right: 13.1, top: 0, height: 13.3 },
  heavyHelmet: { right: 13.9, top: -2, height: 17 },
};
/** The basic helmet's picture for the helmet lamp's card (WornPicture): the worn one shrunk by this into its 13 × 12, its brow where the lamp's On is. */
const CARD = { units: 1.15, shift: [1.65, .3] };

/**
 * The basic helmet's ball turns on its neck ring (helmets-fit-the-suit, BodyFit.cs): the circle its outline fits, about which it turns,
 * in the head's pixels, and how far out its picture is kept under the ring, the outline's ink with it.
 */
const BALL = { x: 6.7, y: 6, r: 6.45 };

/** A helmet's drawing on the head, and its glass; its ops, placed, and the pen's scale. */
function worn(name) {
  const { s, at } = Traced.onHead(name, HEAD[name]), ops = Traced.placed(name, s, at), k = TRACED[name].fit.units * s;
  return { ops, k, draw: Traced.drawer(name, ops, k), glass: ops.filter((o) => o.mat === 'glass').map((o) => o.d).join('') };
}

/** A path's bounds, [x0, y0, x1, y1]: the tracer writes only absolute M, C and Z, so its numbers come in pairs. */
function bounds(d) {
  const n = d.match(/-?\d*\.?\d+/g).map(Number), xs = n.filter((_, i) => i % 2 === 0), ys = n.filter((_, i) => i % 2 === 1);
  return [Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys)];
}

/**
 * The basic helmet in two parts (helmets-fit-the-suit): the ball, its ring's rubber and the ink inside it left out, kept under the
 * ring's top only within its circle, so it turns in place on the ring; and the ring alone, the whole drawing within its outline.
 */
function ringed(basic) {
  const ring = basic.ops.find((o) => o.mat === 'rubber'), [rx0, ry0, rx1, ry1] = bounds(ring.d);
  const inRing = (o) => o === ring || o.op === 'line' && ((b) => b[0] >= rx0 && b[2] <= rx1 && b[1] >= ry0 && b[3] <= ry1)(bounds(o.d));
  const [ball] = HeroKit.cut(Traced.drawer('helmet', basic.ops.filter((o) => !inRing(o)), basic.k), basic.glass, []);
  const { x, y, r } = BALL;
  HeroKit.keep(ball, `M-4 -4L17 -4L17 ${ry0}L-4 ${ry0}Z M${x - r} ${y}A${r} ${r} 0 1 1 ${x + r} ${y}A${r} ${r} 0 1 1 ${x - r} ${y}Z`);
  return { ball, collar: HeroKit.keep(HeroKit.part({ w: 13, h: 12, draw: basic.draw }), ring.d, Anime.LINE.outer) };
}

/** How far the bell's rubber gasket shows below its brass neck ring, in the head's pixels. */
const GASKET = 0.9;

/** A path moved down by dy: the tracer writes only absolute M, C and Z, so its numbers come in pairs. */
const lowered = (d, dy) => {
  let k = 0;
  return d.replace(/-?\d*\.?\d+/g, (v) => String(k++ % 2 ? +(+v + dy).toFixed(3) : v));
};

/** Above this row of the head the bell is its dome, below it its shoulder piece, but for the brass neck ring and the gasket between. */
const DOME = 10.9;

/**
 * The bell in three parts (helmets-fit-the-suit), so its dome nods on a rubber gasket as the basic helmet's ball on its seal: the
 * dome with its brass neck ring and hoses; the shoulder piece, the ring's and the gasket's place left open in it; and the gasket,
 * the ring's own shape and that shape lowered by GASKET, in the hoses' rubber, under the ring and showing below it.
 */
function belled(bell) {
  const brass = bell.ops.filter((o) => o.mat === 'brass'), width = (o) => ((b) => b[2] - b[0])(bounds(o.d));
  const ring = brass.reduce((a, b) => (width(b) > width(a) ? b : a)), low = lowered(ring.d, GASKET), rim = Anime.LINE.outer;
  const top = `M-4 -4L17 -4L17 ${DOME}L-4 ${DOME}Z`, metal = brass.filter((o) => o.op !== 'line').map((o) => o.d);
  const [shell, visor] = HeroKit.cut(bell.draw, bell.glass, []);
  // The ink drawn on the ring, as its clasp's edge, goes with it.
  const [rx0, ry0, rx1, ry1] = bounds(ring.d), inked = bell.ops.filter((o) => o.op === 'line' && ((b) => b[0] >= rx0 && b[2] <= rx1 && b[1] >= ry0 && b[3] <= ry1)(bounds(o.d))).map((o) => o.d);
  HeroKit.within(shell, { keep: [top, ring.d], back: [...metal, ...inked] }, rim);
  const collar = HeroKit.within(HeroKit.part({ w: 13, h: 12, draw: bell.draw }), { keep: ['M-4 -4L17 -4L17 17L-4 17Z'], drop: [top, ring.d, low, ...metal, ...inked] }, rim);
  const seal = { ...ring, mat: 'tube', lite: [], d: ring.d + low };
  return { shell, visor, collar, seal: HeroKit.part({ w: 13, h: 12, draw: Traced.drawer('heavyHelmet', [seal], bell.k) }) };
}

Subjects.helmets = function () {
  const basic = worn('helmet'), [, visor] = HeroKit.cut(basic.draw, basic.glass, []), { ball, collar } = ringed(basic);
  out('hero_helmet_shell', ball);
  out('hero_helmet_collar', collar);
  out('hero_visor', visor);
  out('hero_helmet', Anime.bake({ w: 13, h: 12, light: [0, -1], ...CARD, draw: basic.draw }));
  out('items/helmet', Traced.icon('helmet'));
  const bell = belled(worn('heavyHelmet'));
  out('hero_heavyHelmet_shell', bell.shell);
  out('hero_heavyHelmet_visor', bell.visor);
  out('hero_heavyHelmet_collar', bell.collar);
  out('hero_heavyHelmet_seal', bell.seal);
  out('items/heavyHelmet', Traced.icon('heavyHelmet'));
};

window.HelmetsKit = { top: { helmet: Traced.crown('helmet', HEAD.helmet), heavyHelmet: Traced.crown('heavyHelmet', HEAD.heavyHelmet) } };
})();

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

/** A helmet's drawing on the head, and its glass. */
function worn(name) {
  const { s, at } = Traced.onHead(name, HEAD[name]), ops = Traced.placed(name, s, at);
  return { draw: Traced.drawer(name, ops, TRACED[name].fit.units * s), glass: ops.filter((o) => o.mat === 'glass').map((o) => o.d).join('') };
}

Subjects.helmets = function () {
  const basic = worn('helmet'), [shell, visor] = HeroKit.cut(basic.draw, basic.glass, []);
  out('hero_helmet_shell', shell);
  out('hero_visor', visor);
  out('hero_helmet', Anime.bake({ w: 13, h: 12, light: [0, -1], ...CARD, draw: basic.draw }));
  out('items/helmet', Traced.icon('helmet'));
  const bell = worn('heavyHelmet'), [bellShell, bellVisor] = HeroKit.cut(bell.draw, bell.glass, []);
  out('hero_heavyHelmet_shell', bellShell);
  out('hero_heavyHelmet_visor', bellVisor);
  out('items/heavyHelmet', Traced.icon('heavyHelmet'));
};

window.HelmetsKit = { top: { helmet: Traced.crown('helmet', HEAD.helmet), heavyHelmet: Traced.crown('heavyHelmet', HEAD.heavyHelmet) } };
})();

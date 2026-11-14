// The quilted helmet (2026-11-14-quilted-helmet-and-traced-art), as picked: a padded canvas hood of rolls strapped to a welder's
// plate, its glass dark. Traced from an AI-made reference (refs/quilted.png) for its form only; drawn by the engine (traced.js).
// On the hero in the helmets' box and joint (hero.js's cut), facing +x, its glass the visor; its card the trace's own fit.
(function () {
'use strict';
/** Where the hood sits on the head (hero.js's 13 × 12 box), in its pixels: its front edge, its crown and how tall it stands, the last roll a collar on the shoulders as the bell's breastplate. */
const HEAD = { right: 13, top: -0.8, height: 15.6 };

Subjects.quilted = function () {
  const { s, at } = Traced.onHead('quilted', HEAD), ops = Traced.placed('quilted', s, at), u = TRACED.quilted.fit.units;
  const glass = ops.filter((o) => o.mat === 'glass').map((o) => o.d).join('');
  const [shell, visor] = HeroKit.cut(Traced.drawer('quilted', ops, u * s), glass, []);
  out('hero_quiltedHelmet_shell', shell);
  out('hero_quiltedHelmet_visor', visor);
  out('items/quiltedHelmet', Traced.icon('quilted'));
};

window.QuiltedKit = { top: Traced.crown('quilted', HEAD) };
})();

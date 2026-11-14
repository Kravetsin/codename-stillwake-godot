// The heat suit (2026-11-14-heat-limits), as picked: an aluminised foundry suit of quilted silver rolls, dark cuffs and belt,
// heavy boots and gauntlets, a small dark pack. Traced from an AI-made sheet of its six pieces (refs/heatSuit.png) for their form
// only, drawn by the engine (traced.js). Each piece stretched over its part's box on the basic suit's joints (hero.js), facing +x.
(function () {
'use strict';
/** Where each piece lies in its part's picture, [x0, y0, x1, y1] in its pixels: over the basic suit's outline there. */
const BOX = {
  torso: [.8, -1.4, 11.4, 15.2],
  pack: [-.4, .2, 4.4, 11.8],
  upperArm: [.1, .2, 4.9, 10.6],
  forearm: [.4, .8, 5, 11.4],
  thigh: [.2, -.2, 5.8, 13.4],
  shin: [-.2, -.2, 8, 10.95],
};
const SIZE = { torso: [12, 15], pack: [4, 12], upperArm: [5, 9], forearm: [5, 10], thigh: [6, 12], shin: [9, 11] };
/** Where each part's picture lies on the card's jacket: its joint's place, less the joint (Figure.cs's Joints). */
const STAND = { pack: [-3, 1.5], torso: [0, 0], upperArm: [3.5, .6], forearm: [3.5, 6.3] };

const piece = (name) => 'heatSuit.' + name;
/** A part's drawing in its picture's pixels, moved by `by`; k its units per world pixel (traced.js's drawer), the part's own by default. */
function drawOf(name, by = [0, 0], k = null) {
  const b = BOX[name], { s, at } = Traced.into(piece(name), [b[0] + by[0], b[1] + by[1], b[2] + by[0], b[3] + by[1]]);
  return Traced.drawer(piece(name), Traced.placed(piece(name), s, at), k ?? TRACED[piece(name)].fit.units * (s[0] + s[1]) / 2);
}

Subjects.heatSuit = function () {
  for (const name of Object.keys(BOX)) out(`hero_heatSuit_${name}`, HeroKit.part({ w: SIZE[name][0], h: SIZE[name][1], draw: drawOf(name) }));
  // The card: the jacket as worn, the pack behind and the near arm hanging, fitted into the icon's 16.
  const box = [-3.4, -1.4, 11.4, 17.8], u = (box[3] - box[1]) / 15.4, L = Anime.LINE;
  const draws = ['pack', 'torso', 'upperArm', 'forearm'].map((n) => drawOf(n, STAND[n], u));
  out('items/heatSuit', Anime.bake({ w: 16, h: 16, light: [-0.6, -0.8], units: u, shift: [(16 * u - (box[2] - box[0])) / 2 - box[0], (16 * u - (box[3] - box[1])) / 2 - box[1]],
    W: { outer: L.outer * u, edge: L.edge * u, panel: L.panel * u }, draw: (a) => draws.forEach((d) => d(a)) }));
};
})();

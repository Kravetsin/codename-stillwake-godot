// The heat suit (2026-11-14-heat-limits), as picked: an aluminised foundry suit of quilted silver rolls, dark cuffs and belt,
// heavy boots and gauntlets, a small dark pack. Traced from an AI-made sheet of its six pieces (refs/heatSuit.png) for their form
// only, drawn by the engine (traced.js). Each piece stretched over its part's box on the basic suit's joints (hero.js), facing +x.
(function () {
'use strict';
/** Where each piece lies in its part's picture, [x0, y0, x1, y1] in its pixels: over the basic suit's outline there. */
const BOX = {
  torso: [.8, -1.4, 11.4, 15.2],
  pack: [-1, .2, 4.6, 11.8],
  upperArm: [-.4, .2, 5.4, 10.6],
  forearm: [-.1, .8, 5.4, 11.4],
  thigh: [.2, -.2, 5.8, 13.4],
  shin: [-.2, -.2, 8, 10.95],
};
/**
 * Pieces whose outline is straightened down their box's sides (Traced.straighten), as the user asked on 2026-11-14: the torso with
 * no waist and no flare under the belt, its collar left; the sleeves full, the gauntlet's hand left; the shin under the strap.
 */
const STRAIGHT = {
  torso: { from: .1, k: 1 },
  upperArm: { k: .6 },
  forearm: { to: .55, k: .85 },
  // Taken in under the tracker's strap (hero.js's trackerStrap, 0.8 to 5.2 across, 3.9 to 6.1 down), so it reads strapped round the leg.
  shin: { from: .37, to: .565, ramp: .1, smooth: .5, left: .8, right: null },
};
const SIZE = { torso: [12, 15], pack: [4, 12], upperArm: [5, 9], forearm: [5, 10], thigh: [6, 12], shin: [9, 11] };

const piece = (name) => 'heatSuit.' + name;
/** A part's drawing in its picture's pixels (its card is cut from the hero wearing it: dev/SuitCards.cs). */
function drawOf(name) {
  const box = BOX[name], { s, at } = Traced.into(piece(name), box);
  let ops = Traced.placed(piece(name), s, at);
  if (STRAIGHT[name]) ops = Traced.straighten(ops, box, STRAIGHT[name]);
  return Traced.drawer(piece(name), ops, TRACED[piece(name)].fit.units * (s[0] + s[1]) / 2);
}

Subjects.heatSuit = function () {
  for (const name of Object.keys(BOX)) out(`hero_heatSuit_${name}`, HeroKit.part({ w: SIZE[name][0], h: SIZE[name][1], draw: drawOf(name) }));
};
})();

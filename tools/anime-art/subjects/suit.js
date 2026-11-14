// The basic suit, raw (2026-11-14-raw-basic-suit): a padded ochre suit GPT repainted over the drawn one (refs/suit.input.png, its
// parts at 32 px a world pixel), its own paint cut out (trace.js, "raw") and inked round, no water. The arm and the leg are one
// picture each, bent whole at the elbow, the knee and the ankle by the game (Figure.cs); the torso is the drawn one's box.
(function () {
'use strict';
/** The repaint's pixels a world pixel: the input laid each part out 8 times its hi-res picture. */
const S = 32;
/**
 * Where each part's picture, its margin in, lay on the input, in its pixels, and its box (hero.js's): the arm is the upper arm's box
 * run down over the forearm hung at the elbow, the leg the thigh's over the shin. The input hung the forearm 23 hi-res pixels down
 * and the shin 37, rounded from the rig's 22.76 and 36.56: each picture is raised by the difference, so the hand and the boot are
 * where the forearm's and shin's pictures had them; the shoulder and hip, under the torso, rise as much.
 */
const PARTS = {
  torso: { at: [40, 176], w: 12, h: 15 },
  arm: { at: [680, 164 + (23 - 22.76) * 8], w: 5, h: 16 },
  leg: { at: [1016, 92 + (37 - 36.56) * 8], w: 9, h: 21 },
};
const Margin = 3;

Subjects.suit = function () {
  for (const [name, { at: [x, y], w, h }] of Object.entries(PARTS)) {
    // Shrunk from the repaint by a filter that averages, not by the canvas's default sampling, which skips pixels.
    const draw = Traced.raw('suit.' + name, 1 / S, [-x / S - Margin, -y / S - Margin], { quality: 'high' });
    out('hero_' + name, Anime.bake({ w, h, draw, margin: Margin, light: [0, -1] }));
  }
};
})();

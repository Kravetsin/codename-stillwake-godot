// The catcher's missile (catcher-missile), the user's pick «Самоделка»: a pipe with a welded cone, a hazard band, three sheet fins and a
// crude nozzle, nose along +x. Lit from above; the game flips it when it flies left so its top stays up. Its flame and smoke are the
// game's (ShotLooks.cs). shot_missile: 16 x 6 world px, 2 more round it on every side for the fins.
(function () {
'use strict';
const { ell, poly, bake } = Anime;
const mat = (base, shade, deep, lite) => ({ base, shade, deep, lite });
const IRON = mat('#4a4d55', '#33353b', '#222327', '#6c707a');
const PIPE = mat('#5f5a53', '#433f3a', '#2e2b28', '#7a756c');
const rivet = (a, x, y, r) => { a.flat(ell(x, y, r), '#262a34'); a.flat(ell(x - r * .3, y - r * .35, r * .4), '#8e96a2'); };
const rustAt = (a, x, y, s) => {
  a.flat(poly(x - s, y - .2 * s, x - .3 * s, y - .7 * s, x + .5 * s, y - .5 * s, x + s, y + .1 * s, x + .4 * s, y + .45 * s, x - .5 * s, y + .4 * s), '#6e4630');
  a.flat(poly(x - .4 * s, y - .1 * s, x + .1 * s, y - .3 * s, x + .4 * s, y + .05 * s, x, y + .3 * s), '#3f2a24');
};

function missile(a) {
  a.shape(poly(.6, 1.7, 4.6, 1.9, 2.4, -.9, .1, -.9), IRON, { shade: .4 });
  a.shape(poly(.6, 4.3, 4.6, 4.1, 2.4, 6.9, .1, 6.9), IRON, { shade: .4 });
  a.shape(poly(-1.3, 1.1, .9, 1.9, .9, 4.1, -1.3, 4.9), IRON, { shade: .5, lines: ['M-.6 1.5L-.6 4.5'] });
  const body = 'M.7 1.6L11 1.5L11 4.5L.7 4.4Z', band = 'M8 1.5L10.2 1.5L10.2 4.5L8 4.5Z';
  a.shape(body, PIPE, { shade: .7, deep: 'M0 3.9L12 3.9L12 5L0 5Z', lite: 'M1 1.7L10.8 1.65L10.8 2.1L1 2.15Z', lines: ['M3.4 1.6L3.4 4.4', 'M6.6 1.6L6.5 4.4'] });
  a.flat(band, '#c99a2e', { clip: body });
  for (const x of [7.6, 8.8, 10]) a.flat(poly(x, 1.4, x + .6, 1.4, x + 1.1, 4.6, x + .5, 4.6), '#1d1b19', { clip: band });
  a.shape('M10.9 1.4L14 2.3Q16.2 3 14 3.7L10.9 4.6Z', IRON, { shade: .5, lite: 'M11.2 1.8L13.6 2.4L13.5 2.7L11.2 2.2Z' });
  a.line('M10.9 1.5Q11.2 3 10.9 4.5', { w: 1.1 });
  rivet(a, 2, 2.3, .26); rivet(a, 2, 3.7, .26); rivet(a, 5.2, 2.3, .26);
  rustAt(a, 5, 3.6, .55); rustAt(a, 1.6, 3.9, .4);
}

Subjects.missile = function () {
  out('shot_missile', bake({ w: 16, h: 6, margin: 2, light: [0, -1], draw: missile }));
};
})();

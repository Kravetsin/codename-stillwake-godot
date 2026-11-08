// The credit, as picked (credit-counter): the chip stood upright and cut to its bones, a steel card whose window burns a cold band,
// brass teeth below, so it reads at 12 px beside the gold number. 16 by 16, the finish's; written before every amount of credits.
(function () {
'use strict';
const { ell, poly } = Anime;

/** Wear where water sat: a torn edge and a darker heart, in the metal's own corrosion. */
const wear = (a, x, y, s, edge, heart) => {
  a.flat(poly(x - s, y - .2 * s, x - .3 * s, y - .7 * s, x + .5 * s, y - .5 * s, x + s, y + .1 * s, x + .4 * s, y + .4 * s, x + .1 * s, y + .75 * s, x - .5 * s, y + .45 * s), edge);
  a.flat(poly(x - .4 * s, y - .1 * s, x + .1 * s, y - .3 * s, x + .45 * s, y + .05 * s, x, y + .3 * s), heart);
};

const STEEL = { base: '#9aa2b0', shade: '#6a7183', deep: '#474d5e', lite: '#bfc6d2', spec: '#e6eaf0' };

function chip(a) {
  const body = poly(3.4, 1.4, 10.3, 1.4, 12.6, 3.7, 12.6, 14.6, 3.4, 14.6);
  a.shape(body, STEEL, {
    shade: poly(11.5, 1.4, 12.7, 1.4, 12.7, 14.7, 11.5, 14.7), deep: poly(3.3, 13.8, 12.7, 13.8, 12.7, 14.7, 3.3, 14.7),
    lite: poly(3.3, 1.3, 10.2, 1.3, 10.2, 2.1, 3.3, 2.1), lines: ['M3.4 11.1L12.6 11.1'],
  });
  // The window, sunk, its lower lip lit, burning a cold band; the brass teeth below; the lanyard hole; rust from a corner.
  a.flat(poly(4.3, 5, 11.7, 5, 11.7, 9.6, 4.3, 9.6), '#12161e');
  a.flat(poly(4.3, 9.6, 11.7, 9.6, 11.7, 10.05, 4.3, 10.05), '#c4ccd8');
  a.glow(poly(5, 5.8, 11, 5.8, 11, 8.8, 5, 8.8), '#3fd2e2', { core: poly(5.5, 6.7, 10.5, 6.7, 10.5, 7.5, 5.5, 7.5) });
  for (const x of [4.5, 7.3, 10.1]) {
    a.flat(poly(x, 11.8, x + 1.4, 11.8, x + 1.4, 13.6, x, 13.6), '#b8924e');
    a.flat(poly(x, 11.8, x + 1.4, 11.8, x + 1.4, 12.3, x, 12.3), '#e2c482');
  }
  a.cut(ell(5.3, 3.3, .7));
  a.line(ell(5.3, 3.3, .78), { w: 1 });
  wear(a, 11.8, 10.6, .55, '#70482f', '#3f2a24');
  // The pen swells along the shadow side.
  a.line('M3.42 14.6L12.6 14.6L12.6 3.7', { w: 2.1 });
}

Subjects.credit = function () {
  out('credit', Anime.bake({ w: 16, h: 16, draw: chip }));
};
})();

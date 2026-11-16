// The weather of the first planet (docs/plans/surface-storms.md), as the user picked it on 2026-11-16: the dust storm's wisps of
// blown dust («Пыльная буря», A), soft blots of ochre paint with no ink round them, drifting over the open sky. The rain storm («Косой
// дождь», B) is streaks the game draws itself, so it has no pictures.
(function () {
'use strict';
const { ell, bake } = Anime;

const WISP = { base: '#b09572', shade: '#937a5a', deep: '#76624a', lite: '#c8b08c', spec: '#d8c4a0', ink: '#21180f' };
const SOFT = { outer: 0.5, edge: 0.18, panel: 0.12 };

/** A wisp of dust, 34 by 12: 3 to 5 flat blots strung along it, as the pick page drew them (its seed 40 + k). */
function wisp(k) {
  return (a) => {
    const r = mulberry(40 + k);
    const n = 3 + k;
    for (let i = 0; i < n; i++) a.shape(ell(6 + i * (26 / n) + r() * 3, 6 + r() * 1.5, 6 + r() * 5, 2.2 + r() * 1.6), WISP, { shade: 0.9, edge: false });
  };
}

/** The pick page's mulberry32, so each wisp is the one picked. */
function mulberry(a) {
  return () => {
    a |= 0; a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

Subjects.storms = function () {
  for (let k = 0; k < 3; k++) out(`planet/dust_wisp_${k}`, bake({ w: 34, h: 12, W: SOFT, margin: 2, draw: wisp(k) }));
};
})();

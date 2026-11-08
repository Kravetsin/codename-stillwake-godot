// The cave lurkers' attacks (lurker-attacks), as the user picked them on their page: the capback's gas, «Клубы со спорами» (three
// puffs with an ink line and a spore that shines), and its slime, «Споровая» (a glob, its drops, a splat, a puddle that glows and
// spores rising off it). Each part a sprite the game moves by position, scale, rotation and alpha (LurkerFx). World pixels, y down.
(function () {
'use strict';
const TAU = Math.PI * 2;

Subjects.lurkerattacks = function () {
  const { hide, run, mat, poly, ell, rng, fillP } = CaveHostiles.kit, MARGIN = CaveHostiles.MARGIN;

  /** A lumpy round outline of n bumps. */
  function blob(seed, cx, cy, rx, ry, n, bump) {
    const r = rng(seed), pts = [];
    for (let i = 0; i < n; i++) { const a = (i / n) * TAU + (r() - .5) * .3, k = 1 + (r() - .5) * 2 * (bump || .15); pts.push([cx + Math.cos(a) * rx * k, cy + Math.sin(a) * ry * k]); }
    return hide(pts, r, .3);
  }
  /** A cumulus outline: the edge of a few circles round the origin, scalloped where they meet. */
  function billow(seed, circ) {
    const r = rng(seed), pts = [];
    for (let i = 0; i < 44; i++) {
      const a = (i / 44) * TAU, dx = Math.cos(a), dy = Math.sin(a);
      let best = 0;
      for (const [cx, cy, R] of circ) { const b = cx * dx + cy * dy, c = cx * cx + cy * cy - R * R, q = b * b - c; if (q >= 0) best = Math.max(best, b + Math.sqrt(q)); }
      pts.push([dx * best, dy * best]);
    }
    return hide(pts, r, .25);
  }
  /** A billow's line: an arc inside one of its circles, on its lit side. */
  const arc = (cx, cy, R, a0, a1) => { const p = []; for (let i = 0; i <= 6; i++) { const a = a0 + ((a1 - a0) * i) / 6; p.push([cx + Math.cos(a) * R, cy + Math.sin(a) * R]); } return run(...p); };
  /** A puddle seen from the side: a low lens on the floor, its top a little uneven. */
  function lens(seed, w, h) {
    const r = rng(seed), top = [], n = 9;
    for (let i = 0; i <= n; i++) { const x = -w / 2 + (w * i) / n, e = Math.sin((Math.PI * i) / n); top.push([x, -h * Math.pow(e, .55) * (.8 + r() * .4) + .2]); }
    return hide(top.concat([[w * .32, 1.1], [0, 1.3], [-w * .32, 1.1]]), r, .12);
  }

  // The gas: three puffs of poisoned yellow-green, and a spore.
  const GAS = mat('#a2ac5e', '#838d48', '#646d35', '#bcc47e');
  const puff = (seed, circ, w, h) => ({ w, h, draw(a) {
    const big = circ.slice().sort((p, q) => q[2] - p[2]);
    a.shape(billow(seed, circ), GAS, { shade: 3, deep: 1.2, lite: ell(big[0][0] - big[0][2] * .3, big[0][1] - big[0][2] * .45, big[0][2] * .45, big[0][2] * .25),
      lines: big.slice(1, 3).map(([cx, cy, R]) => arc(cx, cy, R * .82, Math.PI * 1.02, Math.PI * 1.42)) });
  } });
  // The slime: violet mucus with teal blooms, its lit spores.
  const SLIME = mat('#6a6582', '#55516b', '#403d52', '#837f9b'), BLOOM = '#4f7a72', pool = lens(0xa2, 32, 3);
  const parts = {
    gasPuffBig: puff(0x51, [[-5, 2, 7], [3, -1, 8], [9.5, 3, 5.5], [-1, -6.5, 5.5]], 34, 28),
    gasPuff: puff(0x52, [[-4, 1, 6], [3, -2, 6.5], [7.5, 2.5, 4.5]], 28, 22),
    gasPuffSmall: puff(0x53, [[-3, .5, 5], [3, -.5, 5], [0, -4, 4]], 22, 18),
    spore: { w: 4, h: 4, light: true, draw(c) { fillP(c, ell(0, 0, 1.2, 1.2), .22); fillP(c, ell(0, 0, .55, .55), 1); } },
    glob: { w: 9, h: 8, draw(a) { const d = blob(0xa1, 0, 0, 3.4, 3, 7, .14); a.shape(d, SLIME, { shade: 1, deep: .4 }); a.flat(blob(0xa3, .6, .4, 1.6, 1.1, 6, .2), BLOOM, { clip: d }); } },
    globLit: { w: 9, h: 8, light: true, draw(c) { for (const [x, y] of [[-1.2, -.8], [1, .6], [.2, -1.6], [-.4, 1.2]]) fillP(c, ell(x, y, .35, .35)); } },
    drop: { w: 4, h: 4, draw(a) { a.shape(ell(0, 0, 1.1, 1), SLIME, { shade: .5 }); } },
    splat: { w: 22, h: 8, ox: 11, oy: 7, draw(a) {
      a.shape(hide([[-10, 0], [-7, -1.4], [-8.4, -3.6, 'c'], [-4.6, -2.4], [-2.4, -4.8, 'c'], [-.4, -2.6], [2.4, -5.2, 'c'], [3.4, -2.4], [7.8, -3.4, 'c'], [6.6, -1], [10, 0], [0, 1]], rng(0xa4), .2), SLIME, { shade: poly(-11, -1, 11, -1, 11, 2, -11, 2) });
    } },
    pool: { w: 36, h: 7, ox: 18, oy: 4.5, draw(a) { a.shape(pool, SLIME, { shade: poly(-20, -.6, 20, -.6, 20, 3, -20, 3), deep: poly(-20, .6, 20, .6, 20, 3, -20, 3) }); for (const [x, w] of [[-7, 4], [6, 3.4]]) a.flat(ell(x, -1.2, w, .9), BLOOM, { clip: pool }); } },
    poolLit: { w: 36, h: 7, ox: 18, oy: 4.5, light: true, draw(c) { fillP(c, pool, .18); for (const [x, y, s] of [[-11, -1.2, .4], [-7, -1.8, .5], [-2, -1.4, .35], [3, -2, .45], [7, -1.6, .5], [11, -1, .35]]) fillP(c, ell(x, y, s, s * .8), .9); } },
    motes: { w: 30, h: 30, ox: 15, oy: 30, light: true, draw(c) { const r = rng(0xa5); for (let i = 0; i < 16; i++) { const y = -r() * 28, x = (r() - .5) * 24 * (1 - (-y / 40)), s = .22 + r() * .3; fillP(c, ell(x, y, s, s), .5 + r() * .5); } } },
  };

  const num = (v) => String(+v.toFixed(3));
  const rows = [];
  for (const [name, q] of Object.entries(parts)) {
    const ox = q.ox === undefined ? q.w / 2 : q.ox, oy = q.oy === undefined ? q.h / 2 : q.oy;
    out(`creatures/inkLurkerFx/${name}`, CaveHostiles.picture({ ...q, px: ox, py: oy }));
    rows.push(`        ["${name}"] = new(${q.w + MARGIN * 2}, ${q.h + MARGIN * 2}, ${num(ox + MARGIN)}f, ${num(oy + MARGIN)}f, []${q.light ? ', Glow: true' : ''}),`);
  }
  writeText('../../Content/logic/LurkerFxInk.cs', `// Generated by tools/anime-art/subjects/lurkerattacks.js: export again, never edit.
using System.Collections.Generic;

namespace Stillwake.Game;

public static partial class CreatureArt
{
    /// <summary>The capback's gas and slime drawn in ink and watercolour (lurker-attacks), by part: in <c>art/hd/creatures/inkLurkerFx</c>; a glowing one a light mask.</summary>
    public static readonly IReadOnlyDictionary<string, Part> LurkerFx = new Dictionary<string, Part>
    {
${rows.join('\n')}
    };
}
`);
};
})();

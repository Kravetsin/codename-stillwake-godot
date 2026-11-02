// The planet's low growth, every part placed in the logical picture's own pixels: bushes of dead thorn with a few leaves left, boulders of the ground's stone, and bells, hooded bladders on crooked stalks.
// Nothing here is greener or brighter than the ground it stands on. What glows is in `_glow`, on the part that holds it, where the logical glow picture has it: the game lights from there.
(function () {
'use strict';
const { poly, rng, canvas } = Anime;

function kit(a, r) {
  const chaikin = (s0) => { let s = s0; for (let i = 0; i < 2; i++) { const n = [s[0]]; for (let k = 0; k < s.length - 1; k++) { const p = s[k], q = s[k + 1]; n.push([p[0] * .75 + q[0] * .25, p[1] * .75 + q[1] * .25], [p[0] * .25 + q[0] * .75, p[1] * .25 + q[1] * .75]); } n.push(s[s.length - 1]); s = n; } return s; };
  /** A polygon, its edge torn a little. */
  const fill = (pts, col, amp) => {
    const out = [];
    for (let i = 0; i < pts.length; i++) {
      const p = pts[i], q = pts[(i + 1) % pts.length];
      out.push(p);
      if (!amp) continue;
      const L = Math.hypot(q[0] - p[0], q[1] - p[1]), n = Math.floor(L / .8);
      for (let k = 1; k <= n; k++) { const t = k / (n + 1); out.push([p[0] + (q[0] - p[0]) * t + (r() - .5) * amp, p[1] + (q[1] - p[1]) * t + (r() - .5) * amp]); }
    }
    a.flat(poly(out.flat()), col);
  };
  /** A run from width w0 to w1 through the points: stiff bends kept hard, soft ones curved. */
  const run = (pts, w0, w1, col, soft, amp) => {
    const s = soft ? chaikin(pts) : pts.flatMap((p, i) => { if (i === pts.length - 1) return [p]; const q = pts[i + 1]; return [p, [p[0] * .66 + q[0] * .34, p[1] * .66 + q[1] * .34], [p[0] * .34 + q[0] * .66, p[1] * .34 + q[1] * .66]]; });
    const L = [], R = [];
    s.forEach((p, i) => {
      const u = s[Math.max(0, i - 1)], v = s[Math.min(s.length - 1, i + 1)], dx = v[0] - u[0], dy = v[1] - u[1], l = Math.hypot(dx, dy) || 1, t = i / (s.length - 1);
      const q = ((w0 + (w1 - w0) * t) / 2) * (1 + (amp || 0) * (r() - .5));
      L.push([p[0] - (dy / l) * q, p[1] + (dx / l) * q]); R.unshift([p[0] + (dy / l) * q, p[1] - (dx / l) * q]);
    });
    a.flat(poly(L.concat(R).flat()), col);
  };
  const pen = (pts, w, c) => a.line('M' + pts.map((p) => p.map((n) => n.toFixed(2)).join(' ')).join('L'), { w, c });
  return { fill, run, pen };
}

/** The light a picture's parts give: a soft round of the colour, hard shapes over it; at 4x, added over the plant unlit. */
function lit(w, h, halos, colour, draw) {
  const c = canvas(w * 4, h * 4), x = c.getContext('2d');
  x.filter = 'blur(3px)';
  x.fillStyle = colour;
  for (const [px, py, rad] of halos) { x.beginPath(); x.arc(px * 4, py * 4, rad * 4, 0, 7); x.fill(); }
  x.filter = 'none';
  x.scale(4, 4);
  draw(x);
  return c;
}
const shapeOn = (x, pts, col) => { x.fillStyle = col; x.beginPath(); pts.forEach((p, i) => (i ? x.lineTo(p[0], p[1]) : x.moveTo(p[0], p[1]))); x.closePath(); x.fill(); };

/* ---- Thorn: a dead thicket: wire-thin grey wood, hooked spurs, the dark of its own tangle behind; few leaves left, small and hard. The berries are shrivelled knobs on short spurs. ---- */
const WOOD = { thicket: '#1d201b', far: '#2a2a25', base: '#4a443a', lite: '#7a7160', dead: '#1a1714' };
const LEAF = { deep: '#1a2c24', mid: '#2a4235', sick: '#4b4c30', berry: '#3a1a28', husk: '#1d0f15' };
const BUSHES = {
  bush_0: {
    w: 45, h: 23,
    // Patches of its own tangle, torn, the sky showing between them; inside the pixel picture's outline. Its foot stands on two root stocks, a gap between.
    mass: [[[5, 19.6], [7, 15.4], [11.6, 13.4], [17, 14], [20, 18], [18, 20.6], [7, 20.6]], [[21.8, 19.4], [22.4, 12.4], [26, 9], [31.6, 10.4], [34.6, 15], [33, 20.4], [25, 20.6]], [[13, 10.4], [15.6, 5.6], [20, 4.6], [22, 8], [20, 11], [15, 12]]],
    // Behind: thin dead whips crossing each other, reaching the outline.
    far: [[[15, 21], [17.6, 15], [19, 9], [16, 3.4]], [[12, 20], [9.6, 14], [10.6, 8.6], [12.4, 5.2]], [[20, 21], [20, 14], [22.6, 7.4], [25.8, 2]], [[25, 21], [22, 16], [18, 12], [15.6, 11]], [[29, 21], [31, 14], [28, 7], [27, 1.6]], [[23, 18], [27, 11], [31, 6], [30, 3.6]], [[7, 20], [3.6, 14.6], [1.6, 13.4]], [[33, 21], [36, 16], [35.4, 11.4]], [[18, 20], [14, 16], [7, 14.6], [3, 15.6]], [[24, 20], [29, 17], [34.6, 16], [36.6, 14.6]], [[16, 8], [20, 6.4], [24.6, 7.8]], [[9.4, 11.6], [12.6, 7.6], [14, 4.2]]],
    // The stocks and limbs: a limb's way, its width at the foot and at the tip. Two stocks, each splaying out and forking.
    main: [
      [[[13.2, 22], [13.8, 18.4], [11.6, 15], [6.6, 12.8], [1.6, 12.2]], 1.9, .35], [[[13.8, 18.4], [15.2, 13.6], [14.6, 9.4], [17.4, 5], [17, .6]], 1.5, .32],
      [[[27.4, 22], [26.8, 17.6], [23.6, 13], [23.8, 8], [20.8, 3.6], [21.6, 1.6]], 1.9, .32], [[[26.8, 17.6], [30, 13.4], [30.2, 9], [33.2, 5.6]], 1.4, .3],
      [[[27.6, 20], [32, 16.6], [36.4, 12.4]], 1.2, .3], [[[9.4, 22], [8.4, 19.4], [5, 17.6], [1.6, 18.4]], 1.1, .28], [[[32, 22], [34.4, 19.8], [36.4, 17.4]], 1, .26],
    ],
    twigs: [[[6.6, 12.8], [4.6, 10.4], [3.4, 10.6]], [[11.6, 15], [8.6, 10.8], [9, 8.4]], [[14.6, 9.4], [11.8, 6.6], [11.6, 4]], [[17.4, 5], [19.6, 3], [18.8, .8]], [[15.2, 13.6], [19, 11.4], [19.8, 8.6]], [[23.8, 8], [26.4, 5], [27, 1.8]], [[23.6, 13], [19.6, 15.4], [17.8, 19]], [[30.2, 9], [28, 6.4], [29, 4]], [[30, 13.4], [33.8, 11], [33, 8.6]], [[32, 16.6], [33, 19], [36.4, 18.4]], [[5, 17.6], [3.4, 20], [2.2, 19.4]], [[26.8, 17.6], [22.4, 19], [23, 20.4]]],
    // What is left of its leaves: each clump's middle, reach and tone.
    leaves: [[5.6, 11.8, 2.4, 'mid'], [10.4, 9.6, 2.2, 'deep'], [19.2, 2.8, 2, 'sick'], [24.8, 6, 2.6, 'mid'], [31.6, 8, 2.2, 'deep'], [34.4, 15.6, 2, 'sick']],
    berries: [[19, 8], [29, 13], [11, 15], [35, 18], [23, 19]],
    spurs: [[19.6, 9.4], [30, 13.6], [11.6, 15.2], [33, 19], [22.4, 19]],
  },
  // Not its brother: one heavy stock left of the middle, a thin one far right, and between them a dead bough snapped and hanging to the ground.
  bush_1: {
    w: 47, h: 24,
    mass: [[[6, 21], [8, 15.6], [14, 12], [22, 13.4], [24, 18], [22, 21.6], [9, 21.6]], [[29, 21.4], [29.6, 15], [34, 11.6], [38.4, 15], [38, 21.4]], [[13, 9], [16, 4.4], [24, 3.6], [29, 6.4], [26, 10.4], [17, 10.6]]],
    far: [[[18, 22], [13, 15], [9.6, 8], [12, 4.6]], [[22, 22], [24, 14], [27, 7], [30.4, 2.8]], [[9, 22], [5, 17], [1.6, 15]], [[26, 21], [28, 15], [33, 8.6], [32.6, 4.4]], [[36, 22], [38.4, 16], [37.4, 10.4]], [[16, 21], [23, 18], [31, 18.6], [38, 19.6]], [[12, 12], [18, 8], [24, 8.6], [30, 5.4]], [[6, 18], [10, 14.6], [17, 15], [21, 11]], [[30, 13], [36, 13.6], [39.4, 15.6]], [[15, 6], [19, 2], [21, 1.4]]],
    main: [
      [[[20, 23], [19.4, 18.6], [15.8, 14.6], [9.6, 11.8], [7.8, 8.2]], 2.3, .35], [[[19.4, 18.6], [22, 12.6], [20.6, 7], [23.2, 2]], 1.6, .32],
      // The snapped bough: out from the stock, over, and down again, its end trailing on the ground.
      [[[21.6, 13.4], [27, 8.8], [30.6, 10.8], [29.6, 16.4], [27.2, 21.4]], 1.05, .3],
      [[[19.6, 20.4], [13.6, 18], [8.8, 18.4], [1.8, 14]], 1.3, .28],
      [[[33.6, 23], [33, 18], [35.4, 12.8], [33.8, 8], [31.4, 3]], 1.2, .28], [[[33, 18], [37, 15.8], [40.4, 12.4]], .85, .24], [[[33.4, 21], [36, 20.2], [39.6, 17]], .75, .22],
    ],
    twigs: [[[9.6, 11.8], [6.4, 11], [5.4, 9]], [[15.8, 14.6], [13, 9.6], [14, 6.6]], [[8.8, 18.4], [5.4, 20.4], [2.6, 19.8]], [[20.6, 7], [17.4, 5.4], [17, 1.2]], [[22, 12.6], [25, 13], [26, 15]], [[27, 8.8], [28, 5.4], [26.6, 3]], [[30.6, 10.8], [32.6, 13], [32, 14.4]], [[35.4, 12.8], [38, 10.6], [36.4, 9.4]], [[13.6, 18], [14, 20.6], [12, 21.2]], [[19.4, 18.6], [22.4, 19], [24, 21.4]], [[15.8, 14.6], [17.6, 13.8], [18, 13.8]], [[33, 18], [31.6, 15.6], [32, 15]]],
    leaves: [[6.6, 9.6, 2.2, 'sick'], [14.6, 6.8, 2, 'deep'], [24.4, 4, 2.2, 'mid'], [35.4, 10, 2.4, 'mid'], [38, 18.4, 2, 'sick'], [8, 16.6, 2.2, 'deep']],
    berries: [[22, 11], [18, 13], [32, 15], [37, 17], [14, 18], [27, 21]],
    spurs: [[21.6, 12.2], [18, 13.8], [32, 15], [37, 15.8], [14, 18], [27.2, 20.4]],
  },
};

function thornBush(a, k, r, name) {
  const d = BUSHES[name];
  for (const m of d.mass) k.fill(m, WOOD.thicket, 1.3);
  for (const s of d.far) k.run(s, .7, .18, WOOD.far, false, .25);
  const clump = ([cx, cy, reach, tone], under) => {
    // A placed clump, only the darts on it fall by chance: hard narrow leaves, no round mass.
    const col = under ? LEAF.deep : LEAF[tone];
    for (let n = Math.round(reach * (under ? 3.6 : 2.6)); n > 0; n--) {
      const t = r() * 6.28, q = reach * Math.sqrt(r()), x = cx + Math.cos(t) * q * 1.25, y = cy + Math.sin(t) * q * .7, ang = -.9 + r() * 1.8, len = 1 + r() * .8, dx = Math.cos(ang) * len, dy = Math.sin(ang) * len;
      k.fill([[x - dx * .6, y - dy * .6], [x - dy * .18, y + dx * .18], [x + dx * .6, y + dy * .6], [x + dy * .12, y - dx * .12]], col);
    }
  };
  d.leaves.forEach((c) => clump(c, true));
  /** Hooked thorns along a placed limb, either side in turn, their places by chance: the edge of the limb, not a thing placed. */
  const thorns = (pts, every, reach) => {
    let side = r() < .5 ? -1 : 1;
    for (let i = 0; i < pts.length - 1; i++) {
      const [x0, y0] = pts[i], [x1, y1] = pts[i + 1], L = Math.hypot(x1 - x0, y1 - y0), ux = (x1 - x0) / L, uy = (y1 - y0) / L;
      for (let t = every * r(); t < L; t += every * (.7 + r() * .6)) {
        const x = x0 + ux * t, y = y0 + uy * t, far = reach * (.7 + r() * .5), nx = -uy * side, ny = ux * side, bx = ux * far * .55, by = uy * far * .55;
        k.fill([[x - ux * .3, y - uy * .3], [x + nx * far * .7 - bx * .1, y + ny * far * .7 - by * .1], [x + nx * far - bx, y + ny * far - by], [x + nx * far * .5 + ux * .2, y + ny * far * .5 + uy * .2], [x + ux * .32, y + uy * .32]], WOOD.dead);
        side = -side;
      }
    }
  };
  for (const s of d.twigs) { k.run(s, .6, .16, WOOD.dead, false, .2); thorns(s, 2.3, 1.1); }
  for (const [pts, w0, w1] of d.main) { k.run(pts, w0 + .28, w1, WOOD.dead, false, .16); thorns(pts, 2.6, 1.35); }
  for (const [pts, w0, w1] of d.main) k.run(pts.map(([x, y]) => [x - .12, y - .06]), w0 * .6, w1 * .55, WOOD.base, false, .18);
  d.leaves.forEach((c) => clump(c, false));
  // The wood toward the light: a broken streak on the stocks' lower lengths only.
  for (const [pts] of d.main) k.run(pts.slice(0, 3).map(([x, y]) => [x - .3, y - .1]), .3, .08, WOOD.lite, false, .3);
  // The berries: each on its spur, a shrivelled knob; the light of it is in the glow picture.
  d.berries.forEach(([x, y], i) => {
    const [sx, sy] = d.spurs[i];
    k.run([[sx, sy], [(sx + x) / 2 + .3, (sy + y) / 2 - .2], [x, y]], .46, .3, WOOD.dead, true);
    k.fill([[x - .95, y - .1], [x - .6, y - .9], [x + .3, y - 1], [x + 1, y - .3], [x + .8, y + .7], [x, y + 1], [x - .7, y + .7]], LEAF.berry);
    k.fill([[x - .7, y + .1], [x + .1, y + .35], [x + .8, y + .1], [x + .6, y + .7], [x, y + 1], [x - .6, y + .6]], LEAF.husk);
  });
}

function bushGlow(name) {
  const d = BUSHES[name];
  return lit(d.w, d.h, d.berries.map(([x, y]) => [x, y, .95]), 'rgba(226,120,190,.42)', (x) => {
    // The flesh shows only through a split in the skin, a different tear on each.
    d.berries.forEach(([bx, by], i) => {
      const o = i % 2 ? 1 : -1;
      shapeOn(x, [[bx - .55 * o, by - .55], [bx + .1 * o, by - .3], [bx + .6 * o, by + .35], [bx - .05 * o, by + .25]], '#e88cc4');
      shapeOn(x, [[bx - .2 * o, by - .25], [bx + .15 * o, by - .05], [bx + .3 * o, by + .2], [bx - .02 * o, by + .1]], '#ffd4ec');
    });
  });
}

/* ---- Boulders: pieces of the ground's own stone, in large planes: a lit fall, a shadow side, a hard shadow at the foot. Two glints of mineral in a seam are all that shines. ---- */
const STONE = { top: '#5d6068', base: '#494c56', shade: '#34363f', deep: '#1f2028', foot: '#191a21', seam: '#17181f', mineral: '#7e7d6e', lite: '#7c8290' };
const ROCKS = {
  rock_0: {
    w: 35, h: 20, glints: [[23.5, 6.5], [14.5, 11.5]],
    // A broken block: a flat-topped mass cut by one slanting fracture, the outline of the pixel picture's.
    body: [[1.2, 20.4], [1, 12], [2.4, 9.8], [5, 7.2], [7.2, 4.6], [10.6, 2.4], [13.6, .9], [15, 2], [23.6, 2.2], [26.6, 4.4], [29.2, 6.4], [31.4, 8], [32.8, 10.6], [33.6, 13.2], [33.6, 20.4]],
    planes: [
      ['top', [[10.6, 2.4], [13.6, .9], [15, 2], [23.6, 2.2], [26.6, 4.4], [24.4, 5.2], [21.6, 7], [17.4, 6], [14.2, 6.8], [11.6, 5.2], [8.4, 5.2]]],
      ['top', [[27.4, 5.4], [31.4, 8], [32.8, 10.6], [33.2, 12.6], [30, 12], [28, 9]]],
      ['shade', [[1.2, 20.4], [1, 12], [2.4, 9.8], [5, 7.2], [8.6, 8.6], [13, 11.4], [17.6, 13.6], [20.4, 16.6], [24.8, 20.4]]],
      ['deep', [[1.2, 20.4], [1, 15.4], [4.4, 15], [8.2, 16.4], [12.4, 18.4], [14.4, 20.4]]],
      ['shade', [[29, 20.4], [29.6, 16.6], [33.6, 14.8], [33.6, 20.4]]],
    ],
    // Cracks: short, each from an edge of a plane.
    cracks: [[[5, 7.2], [8.6, 8.6], [13, 11.4], [17.6, 13.6]], [[29.6, 16.6], [28.6, 13], [27, 11]], [[8.2, 16.4], [9, 13.8]]],
    seams: [[[21.8, 7.8], [23.5, 6.5], [25.6, 5.4]], [[12.2, 12.6], [14.5, 11.5], [17, 11.9]]],
  },
  rock_1: {
    w: 39, h: 24, glints: [[25.5, 6.5], [16.5, 11.5]],
    // A worn dome, older than the block: split once down its shoulder, a slab slipped off its right.
    body: [[2, 24.4], [2.2, 15.4], [3.4, 12.6], [4.6, 10.2], [6.4, 7.6], [9, 5], [12.6, 3], [15, 2], [22, 2], [25, 3], [29.4, 4.4], [32.2, 6], [34.6, 8.8], [36.4, 11.4], [37, 14.4], [38, 17.4], [37.8, 24.4]],
    planes: [
      ['top', [[12.6, 3], [15, 2], [22, 2], [25, 3], [29.4, 4.4], [27.2, 5.2], [24, 8], [20.6, 7.2], [17.4, 8.2], [14.4, 6.8], [10, 7.4], [9, 5]]],
      ['shade', [[2, 24.4], [2.2, 15.4], [3.4, 12.6], [4.6, 10.2], [8.4, 12], [13, 12.6], [15.6, 15.4], [20.4, 17], [22.2, 20.6], [27.4, 24.4]]],
      ['deep', [[2, 24.4], [2.2, 19.4], [6, 18.6], [10.6, 20.4], [14.4, 21.4], [16, 24.4]]],
      ['top', [[32, 7], [34.6, 8.8], [36.4, 11.4], [36.8, 13.6], [34.4, 12.8], [32.6, 10]]],
      ['shade', [[30.4, 24.4], [31, 19], [33.4, 15.6], [37.2, 15], [38, 17.4], [37.8, 24.4]]],
      ['deep', [[33.6, 24.4], [34.6, 20.4], [37.8, 20], [37.8, 24.4]]],
    ],
    cracks: [[[4.6, 10.2], [8.4, 12], [13, 12.6], [15.6, 15.4]], [[31, 19], [33.4, 15.6], [37, 14.4]], [[10.6, 20.4], [11.6, 17.4]]],
    seams: [[[23.6, 7.8], [25.5, 6.5], [27.8, 5.6]], [[14, 12.8], [16.5, 11.5], [19, 12]]],
  },
};

function boulder(a, k, r, name) {
  const d = ROCKS[name], P = STONE;
  k.fill(d.body, P.base, .35);
  for (const [tone, pts] of d.planes) a.flat(poly(pts.flat()), P[tone], { clip: poly(d.body.flat()) });
  // The foot: a hard dark band, as the ground's blocks have under their masses: the game cuts its lowest two rows, the ground takes them.
  const foot = d.h - 4.3, x0 = d.body[0][0], x1 = d.body[d.body.length - 1][0];
  a.flat(poly(x0, d.h + .6, x0, foot + .4, d.w * .24, foot + 1, d.w * .46, foot + .1, d.w * .72, foot + 1, x1, foot, x1, d.h + .6), P.foot, { clip: poly(d.body.flat()) });
  for (const s of d.cracks) k.pen(s, 1.3, '#0f111b');
  for (const s of d.seams) k.run(s, .3, .14, P.seam, false, .3);
  // The outline only where the mass turns from the light, broken: no sticker's border.
  const n = d.body.length;
  k.pen(d.body.slice(0, Math.ceil(n * .4)), 1.7, '#0f111b');
  k.pen(d.body.slice(Math.ceil(n * .72)), 1.4, '#0f111b');
  // The chips of mineral in the seams, no ring round them.
  for (const [x, y] of d.glints) k.fill([[x - .55, y + .1], [x - .15, y - .45], [x + .5, y - .25], [x + .35, y + .35]], P.mineral);
}

function rockGlow(name) {
  const d = ROCKS[name];
  return lit(d.w, d.h, d.glints.map(([x, y]) => [x, y, .8]), 'rgba(255,236,186,.4)', (x) => {
    for (const [gx, gy] of d.glints) {
      shapeOn(x, [[gx - .5, gy + .1], [gx - .1, gy - .4], [gx + .45, gy - .2], [gx + .3, gy + .3]], '#fff0c4');
    }
  });
}

/* ---- Bells: a hooded bladder hung from a crooked stalk, its mouth torn into three teeth where the pixel bell has them. The body is dull and thick; light comes from inside it, through its veins and out of its mouth. ---- */
const SPECIES = {
  // Papery, a dry hood gone the colour of old bruises on tallow; its mouth split wide.
  bellAmber: { body: '#5b4930', fold: '#3b2e1f', deep: '#1c1510', lite: '#7c6a48', stalk: '#2c2a1c', stalkLite: '#49442c', lightSoft: 'rgba(255,205,120,.5)', light: '#e8b45c', core: '#ffe9b8' },
  // Wet, ribbed, a thing from a shore.
  bellTeal: { body: '#2f4f4c', fold: '#1e3534', deep: '#0f1a1b', lite: '#4a6c66', stalk: '#1e2d29', stalkLite: '#33473f', lightSoft: 'rgba(140,240,220,.5)', light: '#7cd8c4', core: '#d8fff2' },
  // A swollen sac, dark at the neck.
  bellViolet: { body: '#453a55', fold: '#2c2539', deep: '#14111c', lite: '#615474', stalk: '#262432', stalkLite: '#3e3a4c', lightSoft: 'rgba(180,160,255,.5)', light: '#a892e4', core: '#e6dcff' },
};
// Each bell: its stalk's way from the foot up into its neck, the barbed leaves low on it, the hood's outline, its folds, its teeth, its veins (the lit lines of the glow picture) and its mouth. Logical pixels: the hood stands on x 8 to 13, y 5.4 to 9.2, always.
const BELLS = {
  bellAmber_0: { w: 19, h: 19,
    stalk: [[9.5, 18.6], [9.2, 15], [10, 12.6], [9.2, 9.6], [9.5, 6.4]], leaves: [[9.2, 15.6, -2.4, -.8, .7], [9.3, 16.4, 1.9, -.5, .55], [9.8, 13, 1.7, -1.3, .4]],
    hood: [[9.4, 5.5], [10.8, 5.3], [11.9, 5.8], [12.4, 7], [12.8, 8.2], [13.4, 9.4], [12.2, 8.9], [11.4, 9.9], [10.6, 8.9], [9.4, 9.6], [8.9, 8.8], [7.8, 9.2], [8.5, 7.8], [8.7, 6.4]],
    folds: [[[9.7, 5.8], [9.5, 7.4], [9.4, 9.2]], [[11.2, 5.8], [11.6, 7.2], [11.4, 9.4]]], shadow: [[8.5, 7.8], [12.8, 8.2], [13.4, 9.4], [12.2, 8.9], [11.4, 9.9], [10.6, 8.9], [9.4, 9.6], [8.9, 8.8], [7.8, 9.2]],
    veins: [[[10.4, 5.9], [10.3, 7], [10.4, 8.4]], [[9.5, 6.4], [9.4, 7.2], [9, 8.2]], [[11.4, 6.2], [11.9, 7.2], [12.1, 8.2]]], mouth: [[8.9, 8.4], [12.4, 8.4], [11.4, 9.2], [10.6, 8.7], [9.5, 9.1]] },
  // The same plant lodged crooked in a wind: its hood twisted, one tooth gone.
  bellAmber_1: { w: 19, h: 18,
    stalk: [[9.5, 17.6], [9.8, 14.6], [8.8, 12], [9.5, 9.8], [10, 6.4]], leaves: [[9.7, 14.6, -2.4, -.3, .7], [9.7, 15.4, 2.1, -1, .55], [9, 11.6, -1.5, -1.3, .4]],
    hood: [[9.6, 5.6], [11.2, 5.2], [12.4, 6], [12.7, 7.4], [13.3, 8.8], [12, 8.7], [11.2, 9.6], [10.2, 8.6], [8.9, 8.9], [8, 8.4], [8.6, 7.2], [8.8, 6.2]],
    folds: [[[10.8, 5.6], [10.2, 7], [10.2, 8.4]], [[12, 6], [11.8, 7.4], [11.3, 9.1]]], shadow: [[8.6, 7.2], [12.7, 7.4], [13.3, 8.8], [12, 8.7], [11.2, 9.6], [10.2, 8.6], [8.9, 8.9], [8, 8.4]],
    veins: [[[10.6, 5.9], [10.5, 7.2], [10.6, 8.2]], [[9.6, 6.4], [9.4, 7.4], [9.2, 8.1]], [[11.8, 6.4], [12, 7.4], [12.2, 8.2]]], mouth: [[9, 8.1], [12.5, 8], [11.3, 9], [10.2, 8.3]] },
  // Short and fat, its ribs stood out; the stalk a stump.
  bellTeal_0: { w: 19, h: 16,
    stalk: [[9.5, 15.6], [9.8, 13], [9, 10.6], [9.4, 7]], leaves: [[9.6, 13, -2.4, -.8, .7], [9.8, 13.6, 2, -.2, .5]],
    hood: [[9.2, 5.8], [10.6, 5.3], [12, 5.7], [12.8, 7], [13.2, 8.6], [12.4, 9.6], [11.8, 8.7], [11, 9.8], [10.4, 8.7], [9.4, 9.8], [9.1, 8.7], [8.2, 9.4], [8, 7.8], [8.5, 6.5]],
    folds: [[[9.6, 5.8], [9, 7.2], [9, 8.8]], [[10.6, 5.5], [10.6, 7.2], [10.6, 8.8]], [[11.8, 5.8], [12.2, 7.2], [11.9, 8.8]]], shadow: [[8.2, 8], [13, 7.8], [13.2, 8.6], [12.4, 9.6], [11.8, 8.7], [11, 9.8], [10.4, 8.7], [9.4, 9.8], [9.1, 8.7], [8.2, 9.4]],
    veins: [[[10.1, 6], [9.9, 7.2], [9.9, 8.3]], [[11.2, 6], [11.4, 7.2], [11.3, 8.3]]], mouth: [[8.9, 8.3], [12.6, 8.3], [11.8, 8.7], [11, 9.2], [10.4, 8.7], [9.4, 9.2]] },
  // The stump bent right over; a rib split open.
  bellTeal_1: { w: 19, h: 15,
    stalk: [[9.5, 14.6], [9, 12.6], [9.8, 10.4], [10, 7]], leaves: [[9.2, 12.4, -2.1, -1.2, .65], [9.1, 13, 2.3, -.8, .5]],
    hood: [[9.6, 5.4], [11.2, 5.4], [12.6, 6.2], [13.2, 7.6], [12.9, 9], [12, 8.8], [11.2, 9.6], [10.4, 8.8], [9.8, 9.8], [9.1, 8.9], [8, 8.8], [8.2, 7.2], [8.8, 6]],
    folds: [[[10.2, 5.6], [9.6, 7.2], [9.7, 9]], [[11.6, 5.7], [11.6, 7.2], [11.2, 9]]], shadow: [[8.1, 7.8], [13.1, 7.4], [12.9, 9], [12, 8.8], [11.2, 9.6], [10.4, 8.8], [9.8, 9.8], [9.1, 8.9], [8, 8.8]],
    veins: [[[10.8, 5.9], [10.6, 7.2], [10.5, 8.3]], [[12.2, 6.8], [12.4, 7.6], [12.2, 8.3]], [[9.2, 6.8], [8.9, 7.6], [9, 8.2]]], mouth: [[8.8, 8.2], [12.6, 8.2], [11.2, 9], [10.4, 8.5], [9.8, 9.2]] },
  // A bag: hung low and heavy off the neck, its mouth pursed.
  bellViolet_0: { w: 19, h: 16,
    stalk: [[9.5, 15.6], [9.2, 13], [9.8, 10.4], [9.3, 7.4], [9.8, 5.8]], leaves: [[9.3, 13.2, -2.3, -.5, .7], [9.4, 13.8, 2.1, -1, .5]],
    hood: [[10, 5.4], [11, 5.6], [11.6, 6.4], [12.6, 7.2], [13.1, 8.4], [12.4, 9.3], [11.5, 8.9], [10.5, 9.9], [9.5, 8.9], [8.5, 9.3], [8, 8.2], [8.6, 7], [9.4, 6.4]],
    folds: [[[9.6, 6.6], [9, 7.6], [9, 8.8]], [[11.4, 6.6], [12.2, 7.6], [12, 8.9]]], shadow: [[8, 8], [13, 8], [13.1, 8.4], [12.4, 9.3], [11.5, 8.9], [10.5, 9.9], [9.5, 8.9], [8.5, 9.3]],
    veins: [[[10.4, 6], [10.4, 7.2], [10.5, 8.4]], [[9.6, 7], [9.4, 7.6], [9.3, 8.3]], [[11.4, 7], [11.7, 7.6], [11.8, 8.3]]], mouth: [[9.4, 8.4], [11.7, 8.4], [11.2, 8.9], [10.5, 9.3], [9.8, 8.9]] },
  // Tall, thin, overgrown: the bag shrunk, a lobe collapsed in on its left.
  bellViolet_1: { w: 19, h: 20,
    stalk: [[9.5, 19.6], [9.3, 16], [9.9, 12.6], [9, 9.6], [9.8, 5.8]], leaves: [[9.3, 16.6, -2.4, -.9, .7], [9.4, 17.4, 2.1, -.4, .5], [9.7, 13.2, -1.6, -1.2, .4]],
    hood: [[10.2, 5.4], [11.4, 5.6], [12.2, 6.6], [13, 7.8], [13, 9], [12.2, 9.2], [11.4, 8.8], [10.6, 9.8], [9.8, 8.8], [9, 8.7], [8.2, 8.9], [8.8, 7.8], [9.8, 7.2], [9.6, 6.2]],
    folds: [[[9.9, 7.2], [9.5, 8], [9.2, 8.6]], [[11.6, 6.4], [12, 7.6], [11.9, 8.9]]], shadow: [[8.8, 7.9], [13, 7.8], [13, 9], [12.2, 9.2], [11.4, 8.8], [10.6, 9.8], [9.8, 8.8], [9, 8.7], [8.2, 8.9]],
    veins: [[[10.8, 6], [10.8, 7.2], [10.7, 8.2]], [[11.6, 6.8], [12, 7.6], [12.1, 8.3]]], mouth: [[9.6, 8.2], [12.2, 8.3], [11.4, 8.7], [10.6, 9.2], [9.9, 8.7]] },
};
const speciesOf = (name) => SPECIES[name.split('_')[0]];

function bell(a, k, r, name) {
  const d = BELLS[name], S = speciesOf(name);
  // A barbed leaf: stiff, downward at its point, low on the stalk.
  for (const [x, y, dx, dy, wd] of d.leaves) {
    k.fill([[x, y - wd * .4], [x + dx * .5, y + dy * .9 - wd * .3], [x + dx, y + dy + .6], [x + dx * .46, y + dy * .55 + wd * .5], [x, y + wd * .4]], S.stalk);
    k.run([[x, y], [x + dx * .5, y + dy * .8], [x + dx * .9, y + dy + .4]], .16, .06, S.stalkLite, true);
  }
  k.run(d.stalk, 1.1, .7, S.stalk, false, .1);
  k.run(d.stalk.map(([x, y]) => [x - .18, y]), .3, .2, S.stalkLite, false, .1);
  k.fill(d.hood, S.body, .1);
  a.flat(poly(d.shadow.flat()), S.fold, { clip: poly(d.hood.flat()) });
  // A lit edge along the hood's crown on the light's side, thin.
  k.run(d.hood.slice(-3).concat([d.hood[0], d.hood[1]]).map(([x, y]) => [x + .24, y + .22]), .1, .3, S.lite, false, .2);
  for (const f of d.folds) k.run(f, .34, .2, S.deep, true, .2);
  a.flat(poly(d.mouth.flat()), S.deep);
}

function bellGlow(name) {
  const d = BELLS[name], S = speciesOf(name);
  return lit(d.w, d.h, [[10.5, 7.8, 2.2]], S.lightSoft, (x) => {
    x.lineCap = 'round';
    x.strokeStyle = S.light; x.lineWidth = .32;
    for (const v of d.veins) { x.beginPath(); v.forEach((p, i) => (i ? x.lineTo(p[0], p[1]) : x.moveTo(p[0], p[1]))); x.stroke(); }
    shapeOn(x, d.mouth, S.light);
    const m = d.mouth, mx = m.reduce((s, p) => s + p[0], 0) / m.length, my = m.reduce((s, p) => s + p[1], 0) / m.length;
    shapeOn(x, m.map(([px, py]) => [mx + (px - mx) * .5, my + (py - my) * .5]), S.core);
  });
}

Subjects.shrubs = function () {
  for (const name of Object.keys(BUSHES)) {
    const d = BUSHES[name], r = rng(0xb05 + d.w * 7 + d.h);
    out('planet/' + name, Anime.bake({ w: d.w, h: d.h, draw(a) { thornBush(a, kit(a, r), r, name); } }));
    out('planet/' + name + '_glow', bushGlow(name));
  }
  for (const name of Object.keys(ROCKS)) {
    const d = ROCKS[name], r = rng(0x70c + d.w * 7 + d.h);
    out('planet/' + name, Anime.bake({ w: d.w, h: d.h, draw(a) { boulder(a, kit(a, r), r, name); } }));
    out('planet/' + name + '_glow', rockGlow(name));
  }
  for (const name of Object.keys(BELLS)) {
    const d = BELLS[name], r = rng(0xbe1 + name.length * 13 + d.h * 7);
    out('planet/' + name, Anime.bake({ w: d.w, h: d.h, draw(a) { bell(a, kit(a, r), r, name); } }));
    out('planet/' + name + '_glow', bellGlow(name));
  }
};
})();

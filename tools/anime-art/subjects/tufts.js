// The planet's small tufts, picked in 2026-11-02-tufts-in-ink: wet sedge, dead reed, a parasite's stalks, every part placed in the picture's own logical pixels. A tuft of any of them
// has the same size and the same lit tips as its logical picture: no outlines round a plant, flat washes and a rare pen line, as the moss.
(function () {
'use strict';
const { poly, ell, rng, canvas } = Anime;

/** Logical size, and the lit pixels of tuft_N_glow.png. The ground is at h - 1: a foot sinks one pixel. */
const TUFTS = {
  tuft_0: { w: 13, h: 14, tips: [[5, 4]] },
  tuft_1: { w: 17, h: 15, tips: [[5, 5], [9, 5]] },
  tuft_2: { w: 13, h: 11, tips: [] },
};
const POD = '#a8e0cf';

function kit(a, r) {
  const chaikin = (s0) => { let s = s0; for (let i = 0; i < 2; i++) { const n = [s[0]]; for (let k = 0; k < s.length - 1; k++) { const p = s[k], q = s[k + 1]; n.push([p[0] * .75 + q[0] * .25, p[1] * .75 + q[1] * .25], [p[0] * .25 + q[0] * .75, p[1] * .25 + q[1] * .75]); } n.push(s[s.length - 1]); s = n; } return s; };
  /** A polygon, its edge torn a little. */
  const fill = (pts, col, amp) => {
    const out = [];
    for (let i = 0; i < pts.length; i++) {
      const p = pts[i], q = pts[(i + 1) % pts.length];
      out.push(p);
      if (!amp) continue;
      const L = Math.hypot(q[0] - p[0], q[1] - p[1]), n = Math.floor(L / .7);
      for (let k = 1; k <= n; k++) { const t = k / (n + 1); out.push([p[0] + (q[0] - p[0]) * t + (r() - .5) * amp, p[1] + (q[1] - p[1]) * t + (r() - .5) * amp]); }
    }
    a.flat(poly(out.flat()), col);
  };
  /** A run from width w0 to w1 through the points: soft bends curved, stiff ones kept hard. */
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
  const tip = (name) => { for (const [x, y] of TUFTS[name].tips) a.flat(ell(x + .5, y + .5, .55, .6), POD); };
  return { fill, run, pen, tip };
}

/* ---- Осока: wet sedge laid over by one wind, heavy blades dragged flat to the right; only the seed stems still stand. ---- */
// A blade: root x, its top [x, y], where it ends [x, y], width, tone.
const SEDGE = {
  tuft_0: {
    back: [[3.8, 5.6, 7, 9.4, 10, .9, 'shade'], [5, 7.6, 6.4, 11.4, 8.6, .8, 'shade'], [6, 9, 8.4, 12.4, 10.4, .9, 'shade']],
    bent: [[[6.3, 13.6], [6.8, 10], [7.4, 5.4]], [[7.4, 5.4], [9.4, 5.6], [10.8, 7.6]]],
    stems: [[[4.7, 13.7], [4.8, 10.6], [5.3, 7.6], [5.5, 5]]],
    front: [[2.8, 4.8, 8.8, 8.2, 11, 1.15, 'base'], [4.2, 6.6, 8.2, 10, 10.6, 1.1, 'mid'], [5.2, 8.2, 9.6, 11.6, 11.6, 1.05, 'base'], [2.3, 2.8, 10.4, 4.6, 12.2, .85, 'mid']],
    wet: [[4.6, 9.2], [6, 9], [7.8, 10]], mat: [[2.4, 13.8], [3, 12], [5, 11], [7.4, 11.6], [9.6, 12.6], [10.6, 13.8]],
  },
  tuft_1: {
    back: [[3.8, 6, 8.2, 9.8, 10.6, .9, 'shade'], [6, 9.2, 7.6, 13.2, 9.4, .8, 'shade'], [9, 12, 8, 15.2, 10.4, .9, 'shade'], [10, 13.4, 9.6, 16.2, 11.8, .9, 'shade']],
    bent: [[[7.3, 14.6], [7.7, 11], [7.6, 8]], [[7.6, 8], [7.8, 7.2], [8.1, 9.2]], [[11.2, 14.6], [11.7, 11], [12.2, 6.6]], [[12.2, 6.6], [14.2, 7], [15.6, 9]]],
    stems: [[[4.8, 14.7], [4.9, 11.4], [5.2, 8.4], [5.5, 6]], [[9, 14.7], [9, 11.2], [9.3, 8.4], [9.5, 6]]],
    front: [[3.2, 5.4, 9.6, 9, 11.8, 1.15, 'base'], [5.2, 8, 9.4, 11.2, 11.4, 1.1, 'mid'], [7.8, 10.6, 10, 13.6, 11.8, 1.15, 'base'], [9.8, 13, 10.6, 15.6, 12.6, 1.05, 'mid'], [2.6, 3, 11.2, 5.2, 13, .85, 'mid']],
    wet: [[8.6, 10.4], [10.4, 10.2], [12.4, 11.2]], mat: [[2.4, 14.8], [3, 13], [6, 12], [10, 12.2], [13.6, 13.2], [15, 14.8]],
  },
  tuft_2: {
    back: [[3.2, 5.4, 3.6, 9.8, 6, .9, 'shade'], [4.6, 7.4, 2.6, 11.4, 4.8, .8, 'shade'], [6, 9.2, 4.4, 12.2, 6.4, .85, 'shade']],
    bent: [[[5, 10.6], [5.4, 5], [6, .8]], [[6, .8], [8, 1], [9.6, 3]]],
    stems: [],
    front: [[2.8, 4.6, 5.6, 8, 7.8, 1.15, 'base'], [4, 6.2, 5, 9.6, 7.2, 1.1, 'mid'], [5.2, 7.8, 6, 11.2, 8.2, 1.05, 'base'], [2.2, 2.6, 7, 4.4, 9, .85, 'mid']],
    wet: [[3.8, 6.2], [5.4, 6], [7.2, 7.2]], mat: [[2.2, 10.8], [2.8, 9], [5, 8], [7.6, 8.4], [9.4, 9.4], [10.2, 10.8]],
  },
};
function sedge(a, k, r, name, P) {
  const d = SEDGE[name], g = TUFTS[name].h - 1;
  const blade = ([rx, ax, ay, ex, ey, w, tone]) => k.run([[rx, g + .7], [rx + (ax - rx) * .3, (g + ay) / 2 - .4], [ax, ay], [(ax + ex) / 2 + .3, (ay + ey) / 2 - .1], [ex, ey]], w, .12, P[tone], true, .18);
  d.back.forEach(blade);
  for (let i = 0; i < d.bent.length; i += 2) { k.run(d.bent[i], .62, .36, P.shade, true); k.run(d.bent[i + 1], .36, .1, P.shade, true); }
  d.stems.forEach((s, i) => {
    k.run(s, .78, .42, P.stem, true);
    // The seed head: a narrow dark spike under the tip, its awns blown right.
    const [x, y] = TUFTS[name].tips[i], X = x + .5, Y = y + .5;
    k.run([[X - .22, Y + 3.3], [X - .08, Y + 1.8], [X, Y + .3]], .5, 1, P.deep, true);
    for (const [dy, l] of [[2.8, 1.5], [1.9, 1.3], [1, 1]]) k.run([[X, Y + dy], [X + l * .6, Y + dy + .05], [X + l, Y + dy + .55]], .22, .05, P.deep, true);
  });
  k.fill(d.mat, P.shade, .35);
  d.front.forEach(blade);
  k.run(d.wet, .3, .08, P.lite, true);
  k.tip(name);
}

/* ---- Сухостой: long-dead reed: hard jointed stalks, downward barbs at the joints; some snapped and hanging by a fibre. A tip opens a split husk. ---- */
const REED = {
  tuft_0: {
    back: [[[7.2, 13.8], [7.5, 11], [7.2, 8.6], [7.7, 6.8]], [[8.9, 13.8], [9.4, 11.4], [10.8, 8.6]], [[4, 13.8], [2.8, 12], [1.4, 10.8]]],
    hang: [[[7.7, 6.8], [9.5, 7.8], [10.3, 10.2]]],
    main: [[[4.9, 13.8], [4.6, 11], [5.1, 8.2], [5.5, 5.2]], [[3.3, 13.8], [3, 11.4], [3.5, 9.4]]],
    barbs: [[4.6, 11, -1.3, .5], [4.8, 10, 1.2, .7], [5.1, 8.2, -1.3, .7], [5.3, 6.9, 1.2, .8], [7.5, 11, 1.2, .5], [7.2, 8.6, -1.2, .7], [3, 11.4, -1, .6], [9.4, 11.4, 1, .7]],
  },
  tuft_1: {
    back: [[[12, 14.8], [12.4, 11.6], [12, 9.2]], [[2.8, 14.8], [2.4, 12.4], [1.2, 9.8]], [[10.8, 14.8], [11, 13], [10.5, 11]]],
    hang: [[[12, 9.2], [14, 9.8], [15, 12.2]]],
    main: [[[4.8, 14.8], [5.2, 11.8], [4.9, 8.8], [5.5, 6.2]], [[9.2, 14.8], [8.9, 11.6], [9.4, 8.6], [9.5, 6.2]], [[7, 14.8], [6.8, 12.8], [7.2, 11]]],
    barbs: [[5.2, 11.8, 1.3, .5], [4.9, 8.8, -1.3, .7], [5.2, 7.4, 1.1, .8], [8.9, 11.6, -1.2, .5], [9.4, 8.6, 1.3, .7], [9.4, 7.3, -1.1, .8], [12.4, 11.6, 1.3, .6], [2.4, 12.4, -1, .7], [6.8, 12.8, -.9, .5]],
  },
  tuft_2: {
    back: [[[8, 10.8], [8.4, 7.6], [8, 4.8]], [[3, 10.8], [2.6, 8], [1.4, 5.8]], [[10, 10.8], [10.6, 8.6], [11.8, 6.8]]],
    hang: [[[5.3, 1.2], [7.4, 1.5], [8.4, 3.6]]],
    main: [[[5, 10.8], [5.3, 7], [4.9, 3.6], [5.3, 1.2]], [[6.6, 10.8], [6.4, 9], [6.8, 7.4]]],
    barbs: [[5.3, 7, 1.3, .6], [5.1, 5.2, -1.3, .7], [4.9, 3.6, 1.2, .7], [8.4, 7.6, 1.2, .6], [2.6, 8, -1, .7], [10.6, 8.6, 1, .7], [6.4, 9, -1, .5]],
  },
};
function reed(a, k, r, name, P) {
  const d = REED[name];
  const barb = ([x, y, dx, dy]) => k.fill([[x, y - .32], [x + dx, y + dy], [x, y + .28]], P.deep);
  for (const s of d.back) k.run(s, 1.1, .42, P.shade, false, .12);
  for (const s of d.hang) k.run(s, .55, .16, P.shade, false, .2);
  d.barbs.forEach(barb);
  for (const s of d.main) {
    k.run(s, 1.5, .7, P.base, false, .12);
    // The side toward the light, a broken streak; the joints a hard dark step.
    k.run(s.slice(0, 3).map(([x, y]) => [x - .2, y]), .26, .1, P.lite, false);
    s.slice(1, -1).forEach(([x, y], i) => { const o = i % 2 ? 1 : -1; k.fill([[x + o * .72, y - .24], [x - o * .1, y - .08], [x + o * .72, y + .24]], P.deep); });
  }
  // The husk: two splinters split open round each lit tip, or a splintered end where there is none.
  for (const [x, y] of TUFTS[name].tips) {
    const X = x + .5, Y = y + .5;
    k.fill([[X - .4, Y + 1.8], [X - .95, Y + .5], [X - .75, Y - .9], [X - .35, Y + .3]], P.shade);
    k.fill([[X + .4, Y + 1.8], [X + .95, Y + .6], [X + 1, Y - .6], [X + .36, Y + .4]], P.deep);
  }
  k.tip(name);
}

/* ---- Грибница: not grass: stiff thin stalks out of a black wet rot, as a parasite fruits from a corpse: a swollen spindle in line with the stalk, some cut short, some burst. ---- */
// A filament: its points; head: [x, y, angle, length, width] or null. lit: the one whose head holds the tip.
const FUNGUS = {
  tuft_0: {
    rot: [[1.8, 13.8], [2.6, 12.6], [4, 12], [5.8, 11.6], [7.4, 12], [9.2, 12.7], [11, 13.8]],
    back: [{ p: [[7, 13], [7.5, 10], [7.9, 7.2]], h: [8, 6.2, .08, 2.8, 1] }, { p: [[9, 13], [9.6, 10.6], [9.6, 9], [11, 8.4]], h: null }, { p: [[3.6, 13], [3, 10], [2.7, 7.8]], h: [2.6, 6.8, -.08, 2.6, .95] }],
    main: [{ p: [[5.2, 13], [5, 9.8], [5.5, 6.4]], lit: 0 }, { p: [[4, 13], [4.2, 11], [3.9, 9.6]], h: null }, { p: [[6.2, 13], [6.4, 10.8], [6.2, 9]], h: null }],
  },
  tuft_1: {
    rot: [[1.6, 14.8], [2.8, 13.6], [5, 12.9], [7, 13], [9.4, 12.6], [12, 13.2], [14.6, 13.8], [15.6, 14.8]],
    back: [{ p: [[3.4, 14], [2.8, 11], [2.4, 8.6]], h: [2.3, 7.6, -.08, 2.6, .95] }, { p: [[11.4, 14], [12, 10.6], [12.4, 7.6]], h: [12.5, 6.5, .06, 2.8, 1] }, { p: [[13.4, 14], [14.2, 11.4], [14, 10], [15.6, 9.2]], h: null }],
    main: [{ p: [[5, 14], [4.9, 10.4], [5.5, 7.4]], lit: 0 }, { p: [[9.2, 14], [9.5, 10.4], [9.5, 7.4]], lit: 1 }, { p: [[7.2, 14], [7, 11.4], [7.3, 9.4]], h: [7.3, 8.4, .02, 2.6, .95] }],
  },
  tuft_2: {
    rot: [[1.8, 10.8], [2.8, 9.6], [4.6, 9], [6.6, 8.7], [8.6, 9.2], [10, 9.8], [11.4, 10.8]],
    back: [{ p: [[3.4, 10], [3, 7], [3.2, 4.6]], h: [3.2, 3.6, 0, 2.6, .95] }, { p: [[7.4, 10], [7.8, 6.6], [8.2, 3.6]], h: [8.3, 2.6, .06, 2.6, 1] }, { p: [[9.6, 10], [10, 7.6], [10, 6.4], [11.6, 5.6]], h: null }],
    main: [{ p: [[5.4, 10], [5.2, 6.4], [5.6, 3]], burst: true }, { p: [[6.6, 10], [6.7, 8.4], [6.5, 7]], h: null }],
  },
};
function fungus(a, k, r, name, P) {
  const d = FUNGUS[name], tips = TUFTS[name].tips;
  /** A swollen spindle, off-true, thicker low: never a ball. */
  const head = ([x, y, ang, len, wid], col, under) => {
    const c = Math.cos(ang), s = Math.sin(ang), at = (u, v) => [x + s * u * len + c * v * wid, y - c * u * len + s * v * wid];
    k.fill([at(-.56, 0), at(-.34, -.46), at(-.02, -.6), at(.3, -.34), at(.56, .03), at(.26, .34), at(-.08, .62), at(-.36, .4)], col, .05);
    k.fill([at(-.5, .02), at(-.24, .34), at(-.02, .58), at(.2, .28), at(.1, .1), at(-.2, .08)], under);
  };
  const line = (s, w0, w1, col) => k.run(s, w0, w1, col, true, .2);
  for (const f of d.back) { line(f.p, .95, .55, P.shade); if (f.h) head(f.h, P.shade, P.deep); }
  k.fill(d.rot, P.deep, .6);
  for (const f of d.main) {
    line(f.p, 1.05, .62, P.shade);
    line(f.p.map(([x, y]) => [x - .16, y]), .42, .26, P.base);
    if (f.lit !== undefined) {
      const [x, y] = tips[f.lit];
      head([x + .5, y + 2, .05, 3, 1.25], P.base, P.shade);
    } else if (f.burst) {
      const [x, y] = f.p[f.p.length - 1];
      // A burst head: the husk torn into splinters, nothing round left of it.
      k.fill([[x - .4, y + .2], [x - 1.5, y - 2.2], [x - .1, y - .5]], P.deep);
      k.fill([[x - .3, y + .2], [x + .1, y - 2.7], [x + .42, y - .2]], P.base);
      k.fill([[x + .1, y + .2], [x + 1.55, y - 1.9], [x + .55, y + .4]], P.shade);
    } else if (f.h) head(f.h, P.base, P.shade);
  }
  k.tip(name);
}

/** Under the turf (#4a7a38) and the ink acacia's leaves (#2f5a4a): nothing here is greener or brighter than the ground it stands on. */
const PALETTES = {
  sedge: { base: '#33503f', mid: '#27402f', shade: '#1b2f27', deep: '#141d18', stem: '#27392c', lite: '#6c8466' },
  reed: { base: '#463f33', shade: '#2c2721', deep: '#171411', lite: '#736a50' },
  fungus: { base: '#6f725f', shade: '#34382f', deep: '#0f1213', lite: '#a8ad9b' },
};
const DRAW = { sedge, reed, fungus };

/** The picture of one tuft of a candidate, for Anime.bake. */
function picture(candidate, name) {
  const t = TUFTS[name], r = rng(0x7f1 + t.w * 17 + t.h + candidate.length * 91);
  return { w: t.w, h: t.h, draw(a) { DRAW[candidate](a, kit(a, r), r, name, PALETTES[candidate]); } };
}

/** The tips' own light, added over the plant, at 4 x: as the acacia's pods. Where the logical glow picture has it. */
function glow(name) {
  const t = TUFTS[name], c = canvas(t.w * 4, t.h * 4), x = c.getContext('2d');
  x.filter = 'blur(3px)';
  x.fillStyle = 'rgba(150,235,215,.55)';
  for (const [px, py] of t.tips) { x.beginPath(); x.arc(px * 4 + 2, py * 4 + 2, 3.2, 0, 7); x.fill(); }
  x.filter = 'none';
  x.fillStyle = '#d8fff2';
  for (const [px, py] of t.tips) { x.beginPath(); x.arc(px * 4 + 2, py * 4 + 2, 1.5, 0, 7); x.fill(); }
  return c;
}

Subjects.tufts = function () {
  for (const name of Object.keys(TUFTS)) {
    for (const [candidate, kind] of [['sedge', 'tuft'], ['reed', 'tuftReed'], ['fungus', 'tuftFungus']]) out('planet/' + name.replace('tuft', kind), Anime.bake(picture(candidate, name)));
    if (TUFTS[name].tips.length) out('planet/' + name + '_glow', glow(name));
  }
};
})();

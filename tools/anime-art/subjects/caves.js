// A cave's corner: stone as cracked cel blocks (with soil and the ice vein on the same generator), its edges, tops and details; moss; ice crystals.
(function () {
'use strict';
const { ell, poly, rng, bake, canvas } = Anime;

const GROUNDS = {
  stone: { back: ['#181a22', '#1b1d26', '#16181f'], tones: [
    { base: '#565c6a', shade: '#3d424f', lite: '#6c7280', spec: '#868c98', ink: '#0f111b' },
    { base: '#4e5462', shade: '#373c48', lite: '#646a78', spec: '#7c8290', ink: '#0f111b' },
    { base: '#5e6472', shade: '#434855', lite: '#747a88', spec: '#8e94a0', ink: '#0f111b' },
  ], cells: 26 },
  soil: { back: ['#1e171a', '#221a1d', '#1b1417'], tones: [
    { base: '#59443a', shade: '#3e2e2c', lite: '#6e5646', spec: '#866c58', ink: '#140c0e' },
    { base: '#4f3c33', shade: '#372826', lite: '#644c3e', spec: '#7c6250', ink: '#140c0e' },
    { base: '#63493b', shade: '#46322c', lite: '#786050', spec: '#907460', ink: '#140c0e' },
  ], cells: 40,
  // Stones in the earth, each placed: x, y, half width, half height.
  stones: [[14, 9, 2.2, 1.4], [37, 22, 1.4, 1], [58, 7, 1.6, 1.1], [71, 33, 3, 1.9], [96, 14, 1.3, .9], [117, 27, 2, 1.3], [24, 44, 2.6, 1.6], [49, 55, 1.4, 1], [88, 49, 2, 1.2], [108, 58, 1.5, 1]],
  grass: [
    { base: '#4a7a38', shade: '#2f5530', lite: '#6a9a48', spec: '#6a9a48', ink: '#10200f' },
    { base: '#3f6e34', shade: '#2a4d2c', lite: '#5d9042', spec: '#5d9042', ink: '#10200f' },
    { base: '#86804a', shade: '#5c5834', lite: '#a09858', spec: '#a09858', ink: '#1a160c' },
  ] },
};
const VEIN = { base: '#6cb4e4', shade: '#3c74b4', lite: '#b4e4fa', spec: '#ffffff', ink: '#0f1c34' };

/** Points on the sheet's torus, and each one's cell as a polygon: a Voronoi of half-planes, the neighbours' copies round it included so it tiles. */
function cellsOf(W, H, n, seed) {
  const r = rng(seed), pts = [];
  // Jittered grid: blocks of about one size, some merged by leaving a point out.
  const across = Math.round(Math.sqrt((n * W) / H)), down = Math.round(n / across);
  for (let j = 0; j < down; j++) for (let i = 0; i < across; i++) {
    if (r() < .1) continue;
    pts.push([(i + .15 + r() * .7) * W / across, (j + .15 + r() * .7) * H / down]);
  }
  return pts.map((p, k) => {
    let cell = [[p[0] - 40, p[1] - 40], [p[0] + 40, p[1] - 40], [p[0] + 40, p[1] + 40], [p[0] - 40, p[1] + 40]];
    pts.forEach((q0, m) => {
      for (const dx of [-W, 0, W]) for (const dy of [-H, 0, H]) {
        if (m === k && !dx && !dy) continue;
        const q = [q0[0] + dx, q0[1] + dy], mx = (p[0] + q[0]) / 2, my = (p[1] + q[1]) / 2, nx = q[0] - p[0], ny = q[1] - p[1];
        if (nx * nx + ny * ny > 40 * 40 * 4) continue;
        const out = [];
        for (let i = 0; i < cell.length; i++) {
          const a = cell[i], b = cell[(i + 1) % cell.length], da = (a[0] - mx) * nx + (a[1] - my) * ny, db = (b[0] - mx) * nx + (b[1] - my) * ny;
          if (da <= 0) out.push(a);
          if (da * db < 0) { const t = da / (da - db); out.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]); }
        }
        cell = out;
      }
    });
    return cell;
  });
}
const flatPoly = (cell, dx, dy, k) => {
  const cx = cell.reduce((s, p) => s + p[0], 0) / cell.length, cy = cell.reduce((s, p) => s + p[1], 0) / cell.length;
  return poly(cell.flatMap((p) => [cx + (p[0] - cx) * k + dx, cy + (p[1] - cy) * k + dy]));
};

/** A sheet of ground: large masses of close tone, each with a hard shadow along its foot, and only some of the cracks between them drawn, so it reads as one body and not as paving. */
function groundSheet(look, seed, dim) {
  return { w: 128, h: 64, tile: true, K: 2, draw(a) {
    const r = rng(seed ^ 0x13), cells = cellsOf(128, 64, dim ? 34 : look.cells, seed), ink = look.tones[0].ink;
    for (const cell of cells) {
      const t = look.tones[Math.floor(r() * look.tones.length)];
      if (dim) {
        // The wall behind: dark and quiet, so what the hero can stand on reads first. One tone a facet, no lines.
        const k = r();
        a.flat(flatPoly(cell, 0, 0, 1.02), look.back[Math.floor(k * look.back.length)]);
        continue;
      }
      a.shape(flatPoly(cell, 0, 0, 1.03), t, { outer: false, edge: false, shade: 1.1 });
    }
    if (dim) return;
    for (const cell of cells) for (let i = 0; i < cell.length; i++) {
      const p = cell[i], q = cell[(i + 1) % cell.length];
      if (r() < .3) a.line(`M${p[0]} ${p[1]}L${q[0]} ${q[1]}`, { w: 2.4, c: ink });
    }
    for (const [x, y, w, h] of look.stones || []) lump(a, x, y, w, h, GROUNDS.stone.tones[Math.floor(r() * 3)], r);
  } };
}

/** Ice flecks over a wall of stone, in a few clusters, as the vein's sheet draws them. */
function veined(base) {
  return { ...base, draw(a) {
    base.draw(a);
    const r = rng(0x1ce);
    for (const [cx, cy, n] of [[30, 18, 14], [84, 40, 18], [112, 12, 9], [58, 54, 10]])
      for (let i = 0; i < n; i++) {
        const x = cx + (r() - .5) * 18, y = cy + (r() - .5) * 13, s = .9 + r() * 1.3;
        a.shape(poly(x, y - s, x + s * .8, y, x, y + s, x - s * .8, y), VEIN, { outer: false, shade: poly(x, y - s, x + s * .8, y, x, y + s), spec: poly(x - .3, y - s * .5, x, y - s * .7, x, y) });
      }
  } };
}

/** A pebble of the ground: a chipped lump with a shadow crescent. */
const lump = (a, cx, cy, w, h, m, r) => {
  const pts = [];
  for (let i = 0; i < 7; i++) { const t = (i / 7) * Math.PI * 2 + r() * .3, k = .78 + r() * .22; pts.push(cx + Math.cos(t) * w * k, cy + Math.sin(t) * h * k); }
  a.shape(poly(pts), { base: m.base, shade: m.shade, lite: m.lite, spec: m.spec, ink: m.ink }, { shade: .5, lite: poly(cx - w * .6, cy - h * .9, cx + w * .2, cy - h * .9, cx - w * .1, cy - h * .2, cx - w * .8, cy) });
};

/** One of a ground's living-edge columns, 16 × 64: lumps behind the hero on its top half, chips before it below, each halved cell a world of its own so no lump is cut. */
function topOf(look, v, front, half) {
  const r = rng(0x70 + v * 7 + (front ? 13 : 0) + half * 3);
  return bake({ w: 8, h: 32, draw(a) {
    const m = look.tones[Math.floor(r() * look.tones.length)];
    if (front) { if (r() < .35) lump(a, 2 + r() * 4, 17 + r() * .4, .7 + r() * .5, .5 + r() * .2, m, r); }
    else if (!look.grass && r() < .75) lump(a, 3.7 + r() * .6, 15.7 + r() * .3, 1.5 + r() * 2.2, .8 + r() * .7, m, r);
    if (look.grass) turf(a, look.grass, front, r);
  } });
}

/** The living edge of grown ground, one half column of it, its ends level so columns join: behind the hero a band of turf with thick tufts on it; before him turf lying over the block's top, its ragged lip hanging down the soil, and a few short blades. */
function turf(a, grass, front, r) {
  const [lit, deep] = grass, wavy = (y0, spread, flip) => {
    const pts = [];
    for (let k = 0; k <= 8; k++) pts.push(k, y0 + (k === 0 || k === 8 ? 0 : flip * r() * spread));
    return pts;
  };
  const blade = (x, root, h, lean, t) => {
    const tip = Math.min(7.7, Math.max(.3, x + lean)), mid = x + (tip - x) * .35;
    a.shape(`M${x - .42} ${root}Q${mid - .3} ${root - h * .55} ${tip} ${root - h}Q${mid + .32} ${root - h * .5} ${x + .42} ${root}Z`, t, { outer: false, edge: false, shade: `M${x} ${root}Q${mid + .05} ${root - h * .5} ${tip} ${root - h}L${x + .6} ${root}Z` });
  };
  if (front) {
    const lip = wavy(18, 2, 1), under = [];
    for (let i = lip.length - 2; i >= 0; i -= 2) under.push(lip[i], lip[i + 1]);
    a.shape(poly(-.2, 15.7, 8.2, 15.7, ...under), deep, { outer: false, edge: false, lite: poly(-.2, 15.7, 8.2, 15.7, 8.2, 16.9, -.2, 16.9) });
    a.line('M' + lip.join(' ').replace(/(\S+ \S+) /g, '$1L'), { w: 1.8, c: deep.ink });
    for (let i = 0, n = 2 + Math.floor(r() * 3); i < n; i++) blade(.8 + r() * 6.4, 16.4, 1.6 + r() * 2, (r() - .5) * 1.4, grass[Math.floor(r() * 2)]);
    return;
  }
  const crown = wavy(14.9, .9, -1);
  for (let i = 0, n = 6 + Math.floor(r() * 4); i < n; i++) blade(.5 + r() * 7, 15.4, 3 + r() * 4.2, (r() - .5) * 2.2, deep);
  a.shape(poly(...crown, 8.2, 16.4, -.2, 16.4), lit, { outer: false, edge: false, shade: poly(-.2, 15.6, 8.2, 15.6, 8.2, 16.4, -.2, 16.4) });
  for (let i = 0, n = 5 + Math.floor(r() * 4); i < n; i++) blade(.5 + r() * 7, 15.6, 2 + r() * 3.4, (r() - .5) * 1.8, r() < .12 ? grass[2] : lit);
}

/** A ground's details over its cells: pebbles and cracks; features a boulder, a long crack and a heap. */
function detailOf(look, k) {
  const m = look.tones[0], r = rng(0xde + k * 11);
  const side = k < 8 ? 16 : k < 10 ? 32 : 48;
  return bake({ w: side, h: side, draw(a) {
    if (k < 8) {
      if (k % 4 === 2) a.line(`M${3 + r() * 2} ${6 + r() * 3}L${6 + r() * 2} ${8 + r() * 3}L${8 + r() * 2} ${6 + r() * 2}L${12 + r()} ${8 + r() * 3}`, { w: 2.6 });
      else lump(a, 5 + r() * 6, 6 + r() * 6, 1.4 + r() * 1.2, 1 + r() * .9, look.tones[k % look.tones.length], r);
    } else if (k === 8) {
      lump(a, 16, 16, 9, 7.5, look.tones[look.tones.length - 1], r);
      a.line('M12 12L16 15L14 20M16 15L21 13', { w: 2.4 });
    } else if (k === 9) {
      a.line('M4 8L9 12L13 10L19 17L23 16L28 24', { w: 3.6, c: m.ink });
    } else {
      for (let i = 0; i < 9; i++) lump(a, 8 + i * 3.6 + r(), 32 - Math.abs(i - 4) * .7 - r() * 4, 2.6 + r() * 1.4, 1.7 + r(), look.tones[i % look.tones.length], r);
    }
  } });
}

// Moss, the picked «Космы» (2026-11-01-moss-in-ink): matted wet strands, hanging from a ceiling, slumped down a wall, flattened one way on a floor.
// Written in along (al, 0..24 down a face) and off (ac: + into the air, - into the rock), turned to each face by frame(). Every variant has the same
// cross-section at al = 4 and al = 20, where the game cuts a run's pieces, so any joins any; growth stays inside them, a torn end past them.
const mossPicture = (function () {
const frame = (face) => (face === 'top' ? (al, ac) => [al, 4 - ac]
  : face === 'bottom' ? (al, ac) => [al, 20 + ac]
    : face === 'left' ? (al, ac) => [4 - ac, al]
      : (al, ac) => [20 + ac, al]);
const kindOf = (face) => (face === 'top' ? 'floor' : face === 'bottom' ? 'ceil' : 'wall');


/** What it draws with, on a face's frame. */
function kit(a, face, r) {
  const T = frame(face);
  const path = (pts) => poly(pts.flatMap((p) => T(p[0], p[1])));
  const fill = (pts, col) => a.flat(path(pts), col);
  /** A tapering run through hand-placed points. w: width at its root; keep: what is left of it at the tip. */
  const strand = (pts, w, col, keep, amp) => {
    let s = pts;
    for (let i = 0; i < 2; i++) { const n = [s[0]]; for (let k = 0; k < s.length - 1; k++) { const p = s[k], q = s[k + 1]; n.push([p[0] * .75 + q[0] * .25, p[1] * .75 + q[1] * .25], [p[0] * .25 + q[0] * .75, p[1] * .25 + q[1] * .75]); } n.push(s[s.length - 1]); s = n; }
    const L = [], R = [], end = keep === undefined ? 0 : keep;
    s.forEach((p, i) => {
      const u = s[Math.max(0, i - 1)], v = s[Math.min(s.length - 1, i + 1)], dx = v[0] - u[0], dy = v[1] - u[1], l = Math.hypot(dx, dy) || 1, t = i / (s.length - 1);
      const q = (w / 2) * (1 - (1 - end) * t * t) * (1 + (amp || 0) * (r() - .5));
      L.push([p[0] - (dy / l) * q, p[1] + (dx / l) * q]); R.unshift([p[0] + (dy / l) * q, p[1] - (dx / l) * q]);
    });
    a.flat(path(L.concat(R)), col);
  };
  return { fill, strand };
}

// How much there is along a block, by hand, in shares of its height (0 its seam's thickness, 1 its full height, -1 bare rock): a run swells and thins over several blocks.
const WAVE = {
  floor: [
    [[4.7, 0], [5.6, .35], [6.8, .7], [8, .55], [9.4, 1], [10.8, .8], [12, .9], [13.4, .5], [14.8, .6], [16, .3], [17.4, .2], [19.3, 0]],
    [[4.7, 0], [6, -.6], [7.4, -1.2], [8.8, -.6], [10, 0], [11.4, .3], [12.8, .5], [14, .85], [15.2, .7], [16.6, 1], [18, .75], [19.3, 0]],
    [[4.7, 0], [6, .15], [7.6, .1], [9, -.4], [10.6, -1.3], [12, -.8], [13.6, -.1], [15, .25], [16.4, .1], [18, .2], [19.3, 0]],
    [[4.7, 0], [5.4, .6], [6.6, .95], [7.6, .7], [9, .8], [10.2, .5], [11.4, .3], [12.8, .4], [14.2, 0], [15.6, -.5], [17, -.4], [18.2, 0], [19.3, 0]],
  ],
  ceil: [
    [[4.7, 0], [6, .2], [7.2, .1], [8.6, .6], [10, .9], [11.2, 1], [12.6, .7], [14, .85], [15.4, .4], [16.8, .1], [18, .2], [19.3, 0]],
    [[4.7, 0], [5.4, .7], [6.6, 1], [8, .75], [9.4, .5], [10.8, .1], [12.2, -.7], [13.6, -1.3], [15, -.5], [16.4, 0], [18, .3], [19.3, 0]],
    [[4.7, 0], [6.4, -.5], [8, -.2], [9.6, .2], [11, .15], [12.4, -.3], [14, -.9], [15.6, -.4], [17, .1], [18.4, .25], [19.3, 0]],
    [[4.7, 0], [6, .3], [7.4, .2], [8.6, .45], [10, .3], [11.4, .6], [12.6, .8], [14, 1], [15.4, .7], [16.6, .95], [18, .6], [19.3, 0]],
  ],
  wall: [
    [[4.7, 0], [6, .4], [7.4, .8], [8.8, .7], [10.2, 1], [11.6, .6], [13, .2], [14.4, -.5], [15.8, -.3], [17.4, .1], [19.3, 0]],
    [[4.7, 0], [6.2, -.9], [7.8, -.7], [9.2, 0], [10.6, .4], [12, .8], [13.4, .65], [14.8, 1], [16.2, .8], [17.8, .5], [19.3, 0]],
    [[4.7, 0], [6.4, .1], [8, -.4], [9.6, -1.2], [11.2, -.4], [12.8, .1], [14.4, .2], [16, .05], [17.6, .15], [19.3, 0]],
    [[4.7, 0], [5.6, .55], [7, .75], [8.2, .5], [9.6, .2], [11, -.3], [12.4, 0], [13.8, .5], [15.2, .85], [16.8, .7], [18.2, .35], [19.3, 0]],
  ],
};
const CAPS = { floor: [[2.6, 1.7], [1.5, 3], [2, 2.2], [3, 1.5]], ceil: [[2.1, 2.8], [1.4, 2.4], [3.1, 1.6], [2.4, 2.2]], wall: [[2.5, 1.5], [1.6, 2.7], [2.8, 2], [2.2, 1.8]] };
const along = (w, al) => { if (al <= w[0][0]) return w[0][1]; for (let i = 1; i < w.length; i++) if (al <= w[i][0]) { const p = w[i - 1], q = w[i], f = (al - p[0]) / (q[0] - p[0]); return p[1] + (q[1] - p[1]) * f * f * (3 - 2 * f); } return 0; };

/**
 * The mass of a variant along its face, split where the wave leaves bare rock: exact at the seams (off seamHi, into rock seamLo), torn elsewhere.
 * o: seamHi, seamLo, top (height at share 1), deep (reach into rock at most), jag. Returns the height along it, for what lies on it.
 */
function body(k, r, wave, caps, col, o) {
  const cut = (al) => (o.cuts || []).some(([x0, x1]) => al > x0 && al < x1);
  const heightOf = (al) => { if (cut(al)) return 0; const t = along(wave, al); return t >= 0 ? o.seamHi + t * (o.top - o.seamHi) : o.seamHi * (1 + t); };
  const spans = [];
  let cur = null;
  for (let al = 4; al <= 20.001; al += .1) { const on = heightOf(al) > .12; if (on && !cur) cur = [al]; if (!on && cur) { cur.push(al - .1); spans.push(cur); cur = null; } }
  if (cur) { cur.push(20); spans.push(cur); }
  for (const [x0, x1] of spans) {
    const seamA = x0 <= 4.05, seamB = x1 >= 19.95, top = [], under = [];
    const a0 = seamA ? caps[0] : .8, b0 = seamB ? caps[1] : .8;
    top.push([x0 - a0, (o.seamLo * .3)], [x0 - a0 * .68, o.seamHi * .5], [x0 - a0 * .35, o.seamHi * .85]);
    under.push([x0 - a0 * .7, o.seamLo * .55], [x0 - a0 * .3, o.seamLo * .9]);
    for (let al = x0; al < x1; ) {
      const edge = al < 4.72 || al > 19.28, jag = edge ? 0 : o.jag * (r() - .42) * Math.min(1, heightOf(al) * .9 + .3);
      top.push([al, Math.max(.05, heightOf(al) + jag)]);
      under.push([al, edge ? o.seamLo : o.seamLo - (o.deep - (-o.seamLo)) * (.5 + .5 * Math.sin(al * .9 + r() * .4)) * Math.max(0, along(wave, al))]);
      al += edge ? .25 : .32 + r() * .4;
    }
    top.push([x1, seamB ? o.seamHi : .1], [x1 + b0 * .35, o.seamHi * .82], [x1 + b0 * .7, o.seamHi * .45], [x1 + b0, o.seamLo * .3]);
    under.push([x1, o.seamLo], [x1 + b0 * .45, o.seamLo * .75]);
    k.fill(top.concat(under.reverse()), col);
  }
  return heightOf;
}
/** A flat sliver lying along the mass, by hand: from, to, its middle's height off the line, its thickness. */
const sliver = (k, x0, x1, ac, th, col) => { const w = x1 - x0; k.fill([[x0, ac], [x0 + w * .18, ac + th * .35], [x0 + w * .46, ac + th * .5], [x0 + w * .74, ac + th * .3], [x1, ac + th * .05], [x0 + w * .7, ac - th * .42], [x0 + w * .34, ac - th * .5]], col, .1); };


function matted(a, k, r, P, kind, v) {
  const w = WAVE[kind][v], caps = CAPS[kind][v], seamHi = kind === 'ceil' ? .9 : .65;
  // Behind the mat, then in front of it: thin strands, none as long as its neighbour. Where a strand starts is the edge of the mass placed by the wave.
  const h = (al) => { const t = along(w, al); return t >= 0 ? seamHi + t * (kind === 'ceil' ? .9 : .85) : seamHi * (1 + t); };
  if (kind === 'ceil') {
    const pass = (col, reach, step, wid) => {
      for (let al = 4.9; al < 19.2; al += step * (.5 + r())) {
        const t = along(w, al);
        if (t < -.25) continue;
        const L = Math.min(4, h(al) + (reach * (.25 + Math.max(0, t)) * (.55 + r() * .75))), lean = (r() - .5) * 1.2, tip = Math.min(19.5, Math.max(4.6, al + lean));
        k.strand([[al, .5], [al + (tip - al) * .22 + (r() - .5) * .3, L * .42], [al + (tip - al) * .68 + (r() - .5) * .28, L * .74], [tip, L]], wid, col, .14, .2);
      }
    };
    pass(P.shade, 3.1, .8, .42);
    body(k, r, w, caps, P.shade, { seamHi, seamLo: -.6, top: 1.8, deep: .85, jag: .42 });
    pass(P.base, 2.2, 1.25, .36);
  } else if (kind === 'floor') {
    body(k, r, w, caps, P.shade, { seamHi, seamLo: -.6, top: 1.5, deep: .85, jag: .24 });
    for (let al = 4.9; al < 17.8; al += .6 + r() * .9) {
      if (along(w, al) < -.1) continue;
      const end = Math.min(19.4, al + 1.8 + r() * 2.6), H = Math.min(2.5, h((al + end) / 2) * (.75 + r() * .55));
      k.strand([[al, .2], [al + (end - al) * .35, H * .78], [al + (end - al) * .72, H], [end, H * .82]], .38, r() < .55 ? P.base : P.mid, .1, .2);
    }
  } else {
    body(k, r, w, caps, P.shade, { seamHi, seamLo: -.6, top: 1.5, deep: .85, jag: .3 });
    for (let al = 4.9; al < 17.4; al += .7 + r() * 1.1) {
      const t = along(w, al);
      if (t < -.1) continue;
      const end = Math.min(19.3, al + 2.4 + r() * (2.4 + t * 4)), H = Math.min(4, h(al) + .15 + Math.max(0, t) * 1.9 * r());
      k.strand([[al, .25], [al + (end - al) * .2, H * .85], [al + (end - al) * .55, H], [al + (end - al) * .85, H * .74], [end, H * .86]], .38, r() < .55 ? P.base : P.mid, .1, .22);
    }
  }
  // Wet streaks, by hand on the fullest place of each variant.
  const crest = w.reduce((b, p) => (p[1] > b[1] ? p : b), w[0])[0], H = h(crest);
  k.strand([[crest - 1.1, H * .6], [crest - .2, H * .78], [crest + .9, H * .62]], .15, P.lite, .3);
}

return (face, v, palette) => {
  const r = rng(0x305 + v * 31 + face.length * 7 + 606);
  return { w: 24, h: 24, draw(a) { matted(a, kit(a, face, r), r, palette, kindOf(face), v); } };
};
})();
const MOSS = {
  mossGreen: { base: '#67714f', mid: '#535d44', shade: '#434d3b', lite: '#8a8f6a', glow: '#7ca85a', keep: .2 },
  moss: { base: '#527470', mid: '#41605f', shade: '#34504f', lite: '#769c95', glow: '#59c8b0', keep: .4 },
};

/** A soft glow from a picture, as the game's bloom picture: blurred, fading before its edge. */
function glowOf(c, tint, reach, keep) {
  const o = canvas(c.width, c.height), x = o.getContext('2d');
  x.filter = `blur(${reach}px)`;
  x.drawImage(c, 0, 0);
  x.filter = 'none';
  x.globalCompositeOperation = 'source-in';
  x.fillStyle = tint;
  x.fillRect(0, 0, o.width, o.height);
  const f = canvas(c.width, c.height), y = f.getContext('2d');
  y.globalAlpha = keep;
  y.drawImage(o, 0, 0);
  y.globalAlpha = 1;
  // Clear to nothing at the box: cut off there, a bloom draws its rectangle.
  y.globalCompositeOperation = 'destination-in';
  const edge = 8, g = y.createLinearGradient(0, 0, c.width, 0);
  g.addColorStop(0, 'rgba(0,0,0,0)'); g.addColorStop(edge / c.width, '#000'); g.addColorStop(1 - edge / c.width, '#000'); g.addColorStop(1, 'rgba(0,0,0,0)');
  y.fillStyle = g; y.fillRect(0, 0, c.width, c.height);
  const h = y.createLinearGradient(0, 0, 0, c.height);
  h.addColorStop(0, 'rgba(0,0,0,0)'); h.addColorStop(edge / c.height, '#000'); h.addColorStop(1 - edge / c.height, '#000'); h.addColorStop(1, 'rgba(0,0,0,0)');
  y.fillStyle = h; y.fillRect(0, 0, c.width, c.height);
  return f;
}

// Crystals: prisms with facets, fitted to the box the logical picture used, its base row kept where the planting looks for it.
const ICE = { base: '#7cc0f0', shade: '#3a78c4', deep: '#2a5494', lite: '#c0ecff', spec: '#ffffff', ink: '#0c1a38' };
/** A shard from its foot (x, y) up to its tip: width, length, lean; lit face toward the light. */
function shard(a, x, y, w, len, lean, hang) {
  const s = hang ? -1 : 1;
  const tipX = x + lean, tipY = y - s * len, shoulderY = y - s * len * .68;
  const lx = x - w / 2 + lean * .62, rx = x + w / 2 + lean * .62, ridge = x - w * .05 + lean * .7;
  const d = poly(x - w / 2, y, lx, shoulderY, tipX, tipY, rx, shoulderY, x + w / 2, y);
  a.shape(d, ICE, {
    lite: poly(x - w / 2, y, lx, shoulderY, tipX, tipY, ridge, shoulderY + s * len * .08, x - w * .08, y),
    shade: poly(ridge, shoulderY + s * len * .08, tipX, tipY, rx, shoulderY, x + w / 2, y, x + w * .15, y),
    deep: poly(x + w * .28, y, rx - w * .15, shoulderY + s * len * .2, rx, shoulderY, x + w / 2, y),
    spec: poly(lx + w * .14, shoulderY, tipX - w * .08, tipY + s * len * .14, tipX - w * .02, tipY + s * len * .3, lx + w * .24, shoulderY + s * len * .32),
    lines: [`M${lx} ${shoulderY}L${ridge} ${shoulderY + s * len * .08}L${rx} ${shoulderY}`, `M${ridge} ${shoulderY + s * len * .08}L${x - w * .03} ${y}`],
  });
}
const CLUSTERS = [
  [[.5, 1, .95, 0]],
  [[.14, .6, .85, -.55], [.5, .95, 1, .02], [.84, .55, .7, .25]],
  [[.26, .85, 1, -.08], [.66, .6, .78, .35]],
];
function crystal(name, variant, hang) {
  const box = LOGICAL[name];
  const w = box.x1 - box.x0 + 1, h = box.y1 - box.y0 + 1;
  return { w: box.w, h: box.h, draw(a) {
    const foot = hang ? box.y0 : box.y1 + 1;
    const shards = CLUSTERS[variant % CLUSTERS.length].slice().sort((p, q) => q[1] - p[1]);
    for (const [fx, fh, fw, lean] of shards) {
      const width = Math.max(3.4, w * (shards.length === 1 ? .62 : .36) * fw);
      shard(a, box.x0 + fx * w, foot, width, h * fh - .45, lean * h * .5, hang);
    }
  } };
}

Subjects.caves = function () {
  const stone = groundSheet(GROUNDS.stone, 0x5701, false);
  out('wall_stone', bake(stone));
  out('wall_iceVein', bake(veined(stone)));
  out('back_stone', bake(groundSheet(GROUNDS.stone, 0x5702, true)));
  out('back_iceVein', bake(groundSheet(GROUNDS.stone, 0x5702, true)));
  out('wall_soil', bake(groundSheet(GROUNDS.soil, 0x5703, false)));
  out('back_soil', bake(groundSheet(GROUNDS.soil, 0x5704, true)));
  const edges = {
    stone: { lite: '#7c8290', shade: '#2f3244', ink: '#0f111b' }, iceVein: { lite: '#7c8290', shade: '#2f3244', ink: '#0f111b' }, soil: { lite: '#7c6654', shade: '#33242a', ink: '#140c0e' },
  };
  for (const [name, look] of Object.entries(edges)) for (let open = 1; open < 16; open++) out(`wall-edge_${name}_${open}`, AnimeEdge(open, look));
  for (const [name, look] of [['stone', GROUNDS.stone], ['iceVein', GROUNDS.stone], ['soil', GROUNDS.soil]]) {
    for (let v = 0; v < 4; v++) for (const front of [false, true]) for (let half = 0; half < 2; half++) out(`top_${name}_${v}_${front ? 'front' : 'back'}_${half}`, topOf(look, v, front, half));
    for (let k = 0; k < 11; k++) out(`detail_${name}_${k}`, detailOf(look, k));
  }
  for (const [tone, paint] of Object.entries(MOSS)) for (const face of ['top', 'bottom', 'left', 'right']) for (let v = 0; v < 4; v++) {
    const c = bake(mossPicture(face, v, paint));
    out(`caves/${tone}_${face}_${v}`, c);
    out(`caves/${tone}_${face}_${v}_glow`, glowOf(c, paint.glow, 3, paint.keep));
  }
  for (let v = 0; v < 3; v++) {
    for (const [family, hang] of [['crystalIce', false], ['crystalIceHang', true]]) {
      const c = bake(crystal(`${family}_${v}`, v, hang));
      out(`caves/${family}_${v}`, c);
      out(`caves/${family}_${v}_glow`, glowOf(c, '#a0dcff', 6, .8));
    }
  }
};
})();

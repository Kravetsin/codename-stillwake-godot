// What grows and lies in a planet's caves, in ink: crystals as caves.js's ice, dripstone of each band, wet fungi and glow-threads, roots, heat's cracks, geodes, fossils and the spire's cache and lens.
// Every part stands in the logical picture's own pixels, read from it by eye and written here by hand. Cave life is flat washes with a rare pen line, as the moss; it stays dim, and its _glow does the lighting.
(function () {
'use strict';
const { poly, ell, canvas, P } = Anime;
const bake = (w, h, draw) => Anime.bake({ w, h, draw });
const pts = (list) => poly(list.flat());
const pathOf = (list) => 'M' + list.map((p) => p.map((n) => n.toFixed(2)).join(' ')).join('L');

/** A run from width w0 to w1 through the points, soft bends curved, its edge a little uneven: a stalk, a thread, a root. */
function run(a, list, w0, w1, col, soft, amp, seed) {
  let s = list;
  if (soft) for (let i = 0; i < 2; i++) { const n = [s[0]]; for (let k = 0; k < s.length - 1; k++) { const p = s[k], q = s[k + 1]; n.push([p[0] * .75 + q[0] * .25, p[1] * .75 + q[1] * .25], [p[0] * .25 + q[0] * .75, p[1] * .25 + q[1] * .75]); } n.push(s[s.length - 1]); s = n; }
  const L = [], R = [];
  s.forEach((p, i) => {
    const u = s[Math.max(0, i - 1)], v = s[Math.min(s.length - 1, i + 1)], dx = v[0] - u[0], dy = v[1] - u[1], l = Math.hypot(dx, dy) || 1, t = i / (s.length - 1);
    const q = ((w0 + (w1 - w0) * t) / 2) * (1 + (amp || 0) * Math.sin((i + 1) * 2.4 + (seed || 0)));
    L.push([p[0] - (dy / l) * q, p[1] + (dx / l) * q]); R.unshift([p[0] + (dy / l) * q, p[1] - (dx / l) * q]);
  });
  a.flat(pts(L.concat(R)), col);
}

/** A soft bloom of a lit mask, as the game's glow picture: blurred, with the shape itself a little brighter, clear at its box or a bloom draws the rectangle. */
function bloom(c, tint, reach, keep, crisp) {
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
  if (crisp) {
    const t = canvas(c.width, c.height), tx = t.getContext('2d');
    tx.drawImage(c, 0, 0);
    tx.globalCompositeOperation = 'source-in';
    tx.fillStyle = tint;
    tx.fillRect(0, 0, t.width, t.height);
    y.globalAlpha = crisp;
    y.drawImage(t, 0, 0);
  }
  y.globalAlpha = 1;
  y.globalCompositeOperation = 'destination-in';
  const edge = 8, g = y.createLinearGradient(0, 0, c.width, 0);
  g.addColorStop(0, 'rgba(0,0,0,0)'); g.addColorStop(edge / c.width, '#000'); g.addColorStop(1 - edge / c.width, '#000'); g.addColorStop(1, 'rgba(0,0,0,0)');
  y.fillStyle = g; y.fillRect(0, 0, c.width, c.height);
  const h = y.createLinearGradient(0, 0, 0, c.height);
  h.addColorStop(0, 'rgba(0,0,0,0)'); h.addColorStop(edge / c.height, '#000'); h.addColorStop(1 - edge / c.height, '#000'); h.addColorStop(1, 'rgba(0,0,0,0)');
  y.fillStyle = h; y.fillRect(0, 0, c.width, c.height);
  return f;
}

/** A lit mask at 4 x, drawn straight in the picture's logical pixels: path strings, what the drawing lit. */
function mask(w, h, draw) {
  const c = canvas(w * 4, h * 4), x = c.getContext('2d');
  x.setTransform(4, 0, 0, 4, 0, 0);
  x.fillStyle = '#fff';
  x.strokeStyle = '#fff';
  x.lineCap = 'round';
  draw((d) => x.fill(P(d)), (d, w0) => { x.lineWidth = w0; x.stroke(P(d)); });
  return c;
}

/* ---- Crystals: caves.js's prisms, a shard from its foot to its tip, in a crust of dark rock. Rows as the logical picture: the foot on its base row, a hanging one's on row 0. ---- */
const CRYSTAL = {
  Amber: { base: '#d0922e', shade: '#94591a', deep: '#60350e', lite: '#f4c870', spec: '#fff', ink: '#1c1006', crust: { base: '#54402c', shade: '#38291c', lite: '#6e5840', ink: '#1c1006' }, glow: '#ffc070' },
  Ember: { base: '#cc4f1c', shade: '#8c2c10', deep: '#561a0a', lite: '#f09040', spec: '#fff', ink: '#1c0a05', crust: { base: '#4a2a20', shade: '#2f1b16', lite: '#664034', ink: '#1c0a05' }, glow: '#ff7a34' },
  Rose: { base: '#c46478', shade: '#843c52', deep: '#58283c', lite: '#eca4ac', spec: '#fff', ink: '#1e0e16', crust: { base: '#4c3840', shade: '#33262c', lite: '#685058', ink: '#1e0e16' }, glow: '#ff8c90' },
};
/** A shard: foot x, width, tip x, tip y; c: the tip snapped off, a slant of this much; n: pen lines of a flaw, as offsets along it. */
function shard(a, m, x, y, w, tx, ty, o) {
  const hang = ty > y, s = hang ? -1 : 1, len = Math.abs(y - ty), lean = tx - x, shY = y - s * len * ((o && o.sh) || .68);
  const lx = x - w / 2 + lean * .62, rx = x + w / 2 + lean * .62, ridge = x - w * .05 + lean * .7, rY = shY + s * len * .08;
  const snap = o && o.c;
  const top = snap ? [tx - w * .2, ty + s * snap, tx + w * .22, ty + s * snap * .25] : [tx, ty];
  const d = poly(x - w / 2, y, lx, shY, ...top, rx, shY, x + w / 2, y);
  a.shape(d, m, {
    lite: poly(x - w / 2, y, lx, shY, top[0], top[1], ridge, rY, x - w * .08, y),
    shade: poly(ridge, rY, top[top.length - 2], top[top.length - 1], rx, shY, x + w / 2, y, x + w * .15, y),
    deep: poly(x + w * .28, y, rx - w * .15, shY + s * len * .2, rx, shY, x + w / 2, y),
    spec: poly(lx + w * .14, shY, top[0] - w * .08, top[1] + s * len * .14, top[0] - w * .02, top[1] + s * len * .3, lx + w * .24, shY + s * len * .32),
    lines: [`M${lx} ${shY}L${ridge} ${rY}L${rx} ${shY}`, `M${ridge} ${rY}L${x - w * .03} ${y}`].concat((o && o.n || []).map(([f, dx, dy]) => { const cx = x + lean * f, cy = y - s * len * f; return `M${cx - dx} ${cy - dy}L${cx + dx * .2} ${cy + dy * .4}L${cx + dx} ${cy + dy}`; })),
  });
}
const crust = (a, m, list, cracks, line) => {
  const out = [];
  list.forEach((p, i) => {
    const q = list[(i + 1) % list.length], L = Math.hypot(q[0] - p[0], q[1] - p[1]), n = Math.floor(L / .8);
    out.push(p);
    if (p[1] === line && q[1] === line) return;
    for (let k = 1; k < n; k++) { const t = k / n, j = Math.sin(i * 3.1 + k * 2.3) * .34 + (k % 2 ? .16 : -.16); const y = p[1] + (q[1] - p[1]) * t + j; out.push([p[0] + (q[0] - p[0]) * t, line ? Math.min(line, y) : Math.max(0, y)]); }
  });
  a.shape(pts(out), m.crust, { outer: false, edge: false, shade: .5 });
  for (const l of cracks || []) a.line(pathOf(l), { w: 1.2, c: m.ink });
};
// Back to front. Amber's and ember's stand in the same outlines, as their logical pictures do; ember's are hotter, split and snapped.
const CRYSTALS = {
  crystalAmber_0: { w: 22, h: 15, foot: 15, shards: [[15.4, 3, 15.6, 5], [10, 5.6, 9.5, 3, { n: [[.45, .5, .9]] }]], crust: [[8, 15], [8.3, 13.2], [10.4, 12.2], [13, 12.8], [15, 12], [17, 12.8], [16.6, 14.2], [15, 15]], cracks: [[[11, 13.4], [12, 14.2]]] },
  crystalAmber_1: { w: 26, h: 19, foot: 19, shards: [[18.4, 3, 22.6, 11.2, { sh: .6 }], [10.6, 3.8, 11.2, 4.2, { sh: .8 }], [13.6, 6.2, 14.6, 4, { n: [[.5, -.5, .8]] }]], crust: [[9, 19], [9.2, 16.8], [11.2, 15.8], [14.6, 16.2], [17, 16], [19.2, 16.8], [18.4, 18.4], [17, 19]], cracks: [[[12.5, 17], [13.8, 17.8], [13.4, 18.6]]] },
  crystalAmber_2: { w: 24, h: 22, foot: 22, shards: [[18, 2.4, 18.6, 15.6], [7.2, 4.4, 6.6, 5.2, { n: [[.6, .4, .7]] }], [15.6, 4.2, 15.8, 7.6], [10.8, 6.6, 11.4, 4, { n: [[.3, -.6, 1], [.6, .4, .8]] }]], crust: [[5.2, 22], [5, 20], [8, 19.2], [12, 19.6], [15.6, 19], [19.8, 19.4], [19.6, 21], [18, 22]], cracks: [[[8.6, 20.4], [9.8, 21.2]], [[15, 20], [14.4, 21.2]]] },
  crystalEmber_0: { w: 22, h: 15, foot: 15, shards: [[15.4, 3, 15.4, 5, { c: .9 }], [8.8, 3.4, 8.6, 4.6, { sh: .8 }], [11, 4.2, 9.8, 3, { n: [[.3, .5, .7], [.62, -.4, .6]] }]], crust: [[8, 15], [8.6, 13], [11, 12.8], [12.8, 12.2], [15.4, 12.4], [17, 13], [16.2, 14.6], [14.6, 15]], cracks: [[[9.8, 13.6], [10.6, 14.4]], [[14, 13.2], [14.8, 14.2]]] },
  crystalEmber_1: { w: 26, h: 19, foot: 19, shards: [[18.2, 3.2, 22.8, 11, { sh: .55, c: .8 }], [12, 6.8, 11.2, 4, { c: .9, n: [[.3, .7, 1], [.55, -.4, .8]] }], [15, 3.6, 14.8, 4.2, { n: [[.5, .4, .9]] }]], crust: [[9, 19], [9.6, 17], [12, 15.8], [15.4, 16.4], [17.4, 15.8], [19, 17], [18, 18.6], [16, 19]], cracks: [[[11, 17.4], [12, 18.4]], [[15.6, 17], [16.6, 17.8]]] },
  crystalEmber_2: { w: 24, h: 22, foot: 22, shards: [[18, 2.6, 18.4, 15.8, { c: .7 }], [6.8, 3.6, 6.4, 5.4], [15.4, 4.6, 16, 7.4, { n: [[.5, .4, .9]] }], [9, 4, 9.4, 6.4, { c: .7 }], [12, 5.2, 11.4, 4, { n: [[.25, -.5, .9], [.62, .5, .9]] }]], crust: [[5, 22], [5.4, 19.6], [9, 19.4], [12.6, 19], [16, 19.6], [19.8, 19.2], [19.2, 21.2], [17.4, 22]], cracks: [[[7, 20.4], [8.2, 21.4]], [[13, 20.2], [12, 21.2]], [[17.2, 20.4], [18, 21]]] },
  crystalRose_0: { w: 24, h: 15, foot: 15, shards: [[7.8, 3.4, 4.4, 6, { sh: .55 }], [10.6, 6, 10, 3, { n: [[.45, .5, .9]] }]], crust: [[7.2, 15], [7.6, 13], [10, 12.4], [13, 12.2], [15, 13], [14.6, 14.4], [13, 15]], cracks: [[[11.4, 13.4], [12.2, 14.2]]] },
  crystalRose_1: { w: 30, h: 18, foot: 18, shards: [[22.4, 3, 26.8, 10.2, { sh: .6 }], [20.6, 3.8, 24.6, 7, { sh: .62, n: [[.5, .4, .6]] }], [15.4, 7, 16, 3, { n: [[.35, -.5, 1]] }]], crust: [[12, 18], [12.2, 15.6], [15, 15], [19, 15.4], [22.8, 15.2], [23.2, 16.6], [21, 17.6], [19, 18]], cracks: [[[15.6, 16.4], [16.8, 17.2]]] },
  crystalRose_2: { w: 32, h: 21, foot: 21, shards: [[22.4, 3.6, 27, 6, { sh: .62 }], [19.8, 4.6, 19, 4.6, { n: [[.4, .4, .9]] }], [15.4, 7, 15, 3, { n: [[.3, .6, 1], [.6, -.4, .7]] }]], crust: [[12, 21], [12.2, 18.6], [15, 18], [19, 18.4], [23, 18], [24, 19.2], [22.6, 20.6], [21, 21]], cracks: [[[16.6, 19.4], [17.8, 20.2]], [[21, 19], [20.4, 20]]] },
  crystalAmberHang_0: { w: 18, h: 14, foot: 0, m: 'Amber', shards: [[5.6, 3, 5.2, 4.8], [9.5, 5.6, 10.2, 10.9, { sh: .7, n: [[.5, .4, -.8]] }]], crust: [[5.4, 0], [11.8, 0], [11.6, .9], [9.4, 1.4], [7, 1.2], [5.6, 1.3]], cracks: [[[8.2, .6], [9, 1.4]]] },
  crystalAmberHang_1: { w: 24, h: 17, foot: 0, m: 'Amber', shards: [[6.8, 3, 6.2, 5.8], [17, 3.8, 18, 6.8], [11.6, 6.2, 10.6, 13.9, { sh: .72, n: [[.4, -.5, -.9], [.7, .3, -.6]] }]], crust: [[5.6, 0], [18.8, 0], [18.5, .9], [15, 1.3], [11, 1.1], [8, 1.4], [5.8, 1.1]], cracks: [[[10, .5], [11, 1.3]], [[15, .6], [14.4, 1.4]]] },
  crystalAmberHang_2: { w: 32, h: 20, foot: 0, m: 'Amber', shards: [[20.2, 4.2, 27.2, 12.8, { sh: .55, n: [[.5, .5, -.5]] }], [16.2, 7.2, 16.6, 16.9, { sh: .76, n: [[.35, -.6, -1], [.68, .4, -.8]] }]], crust: [[13.2, 0], [22.4, 0], [22.2, 1], [19, 1.3], [16, 1.2], [13.5, 1.4]], cracks: [[[17, .5], [18, 1.2]]] },
};
const crystalM = (name) => CRYSTAL[(/crystal(Amber|Ember|Rose)/.exec(name))[1]];
function crystal(name) {
  const c = CRYSTALS[name], m = crystalM(name);
  return bake(c.w, c.h, (a) => {
    for (const [x, w, tx, ty, o] of c.shards) shard(a, m, x, c.foot, w, tx, ty, o);
    crust(a, m, c.crust, c.cracks, c.foot);
  });
}

/* ---- Dripstone. Rows [y, left, right] by hand, root to tip; every band its own character: cold wet and slim, fluted and leaning, knobbled, cracked and snapped. ---- */
const STONE = [
  { base: '#5f6f80', shade: '#3f4d5e', deep: '#2b3646', lite: '#8296a8', spec: '#fff', ink: '#0e1622' },
  { base: '#666c76', shade: '#464b55', deep: '#30343d', lite: '#868c96', spec: '#fff', ink: '#101218' },
  { base: '#625c72', shade: '#443f52', deep: '#2e2a3a', lite: '#837c94', spec: '#fff', ink: '#120f1a' },
  { base: '#66513f', shade: '#46362b', deep: '#2e231c', lite: '#86705a', spec: '#fff', ink: '#160e09' },
];
const STALACTITE = [
  [
    { rows: [[1, 1.4, 5.7], [2, 2.2, 5.3], [3.4, 2.5, 4.6], [5, 2.9, 4.4], [7, 3.1, 3.9], [9, 3.4, 3.5]], wet: [2, 6], ring: [3.4] },
    { rows: [[1, 1.1, 5.9], [3, 2, 5.2], [6, 2.7, 4.5], [9, 3, 4], [11, 3.2, 3.8], [13, 3.5, 3.6]], wet: [2.4, 8], ring: [4.4] },
    { rows: [[1, 1.1, 7.9], [3, 2.3, 7], [6, 3.3, 5.9], [9, 3.7, 5.2], [12, 4, 4.9], [15, 4.3, 4.7], [16.4, 4.5, 4.55]], wet: [3, 11], ring: [5, 8.6], drop: [4.5, 17.6] },
  ],
  [
    { rows: [[1, 1.3, 5.9], [2.2, 2.3, 5.6], [3.6, 2.5, 5], [5.6, 3.2, 4.8], [7.2, 3.8, 4.5], [9, 4.3, 4.35]], flute: [[3.6, 1.4, 4.1, 5.6], [4.8, 1.4, 4.6, 3.6]] },
    { rows: [[1, 1.2, 5.9], [3, 2.3, 5.4], [5, 2.2, 4.9], [7, 2.9, 4.2], [10, 2.8, 3.9], [13, 2.7, 2.8]], flute: [[4, 1.4, 3.4, 8.4]] },
    { rows: [[1, 1, 7.9], [2.6, 2, 7.4], [5, 3.2, 6.6], [7, 3, 5.6], [10, 3.8, 5.4], [13, 4, 5], [16, 4.4, 4.6], [17, 4.5, 4.55]], flute: [[5, 1.4, 4.5, 9], [3.6, 2.6, 3.8, 7]], spur: [[6.4, 5], [7.4, 6.4], [6, 8.4]] },
  ],
  [
    { rows: [[1, 1.3, 5.8], [2.4, 2, 5.6], [3.6, 2.5, 4.7], [5, 2.2, 4.9], [6.6, 3, 4.2], [9, 3.5, 3.6]], ring: [3.6] },
    { rows: [[1, 1.1, 5.8], [2.6, 1.9, 5.2], [4.4, 2.6, 4.4], [6, 2.1, 4.9], [8, 2.8, 4.4], [9.6, 2.5, 4.4], [11.4, 3.1, 3.8], [13, 3.4, 3.5]], ring: [4.4, 8] },
    { rows: [[1, 1.1, 7.8], [2.6, 2.4, 7.2], [4.6, 3.3, 5.8], [6.4, 2.9, 6.3], [8.4, 3.7, 5.3], [10, 3.3, 5.8], [12, 4, 5.1], [13.6, 3.8, 5.2], [15.4, 4.3, 4.7], [16.6, 4.5, 4.6]], ring: [4.6, 8.4, 12], drop: [4.5, 17.5] },
  ],
  [
    { rows: [[1, 1.1, 5.9], [3, 2, 5.4], [5, 2.6, 4.8], [7, 2.9, 4.4], [7.8, 2.8, 3.6], [8.9, 3.4, 3.5]], crack: [[[3, 2], [4, 3.6], [3.4, 5]]] },
    { rows: [[1, 1.1, 5.9], [3.6, 2.2, 5], [6.4, 2.8, 4.8], [9.4, 3, 4.5], [10.8, 3.1, 4.4], [11.6, 3.5, 4.5], [13, 3.3, 3.4]], crack: [[[4.2, 2], [3.6, 4], [4.2, 6]], [[3.4, 8], [4, 9.4]]] },
    { rows: [[1, 1, 7.9], [3, 2, 7], [6, 3, 6.2], [9, 3.4, 5.6], [12, 3.7, 5.3], [13.6, 3.7, 5.2], [14.4, 4.4, 5.2], [15.2, 3.9, 4.8], [15.6, 4.3, 4.4]], crack: [[[5.4, 1.6], [4.4, 4], [5, 6.4], [4.4, 8.4]], [[3, 3], [3.6, 5]]], drop: [3.8, 17.4] },
  ],
];
const STALAGMITE = [
  [
    { rows: [[0, 3.4, 3.6], [1.6, 3, 4.1], [3.4, 2.7, 4.4], [4.6, 2.5, 4.6], [5.4, 1.7, 5.3], [6, 1.1, 5.9]], wet: [1.4, 4.4], ring: [4.6] },
    { rows: [[0, 3.5, 3.6], [3, 3.1, 4.1], [6, 2.8, 4.5], [8, 2.3, 4.8], [9.2, 1.7, 5.4], [10, 1.1, 5.9]], wet: [2, 7], ring: [6, 8] },
    { rows: [[0, 4.4, 4.6], [4, 4, 5.1], [8, 3.4, 5.6], [11, 2.8, 6.3], [13, 2, 7.1], [14.2, 1.4, 7.6], [15, 1.1, 7.9]], wet: [3, 11], ring: [8.6, 12] },
  ],
  [
    { rows: [[0, 4.1, 4.2], [1.4, 3.5, 4.6], [3, 3.1, 4.5], [4.4, 2.6, 4.8], [5.3, 1.8, 5.5], [6, 1.1, 5.9]], flute: [[3.5, 5.6, 3.8, 2], [4.4, 5.6, 4.2, 3.8]] },
    { rows: [[0, 2.8, 2.9], [2, 2.6, 3.6], [4, 2.7, 4.3], [6.4, 2.4, 4.7], [8.4, 1.8, 5.3], [10, 1.1, 5.9]], flute: [[3.8, 9.6, 3.2, 3]] },
    { rows: [[0, 5, 5.1], [3, 4.5, 5.6], [6, 3.9, 5.7], [8.6, 3.4, 6], [11, 2.7, 6.4], [13, 2, 7.2], [15, 1.1, 7.9]], flute: [[5, 14.6, 5, 4], [3.6, 14.4, 4.2, 9]], spur: [[3, 11], [2.2, 8.8], [3.8, 9]] },
  ],
  [
    { rows: [[0, 3.4, 3.7], [1.4, 3, 4.3], [2.6, 2.7, 4], [3.8, 2.2, 4.9], [5, 2, 5], [6, 1.2, 5.8]], ring: [2.6] },
    { rows: [[0, 3.6, 3.7], [1.8, 3.1, 4.3], [3.4, 3, 4.1], [4.8, 2.4, 4.8], [6.2, 2.7, 4.4], [7.8, 1.9, 5.1], [10, 1.1, 5.9]], ring: [3.4, 6.2] },
    { rows: [[0, 4.3, 4.5], [2.4, 3.9, 5.1], [4.2, 4, 4.9], [6, 3.3, 5.8], [8, 3.6, 5.5], [10, 2.7, 6.3], [12, 2.9, 6.2], [13.6, 1.9, 7.2], [15, 1.1, 7.9]], ring: [4.2, 8, 12] },
  ],
  [
    { rows: [[.2, 3, 3.4], [.9, 2.9, 4.3], [3, 2.5, 4.6], [5, 1.6, 5.4], [6, 1.1, 5.9]], crack: [[[3.4, 1.4], [4, 3], [3.4, 4.4]]] },
    { rows: [[0, 3.9, 4.1], [.9, 3, 4.4], [3.4, 2.7, 4.6], [6, 2.4, 4.9], [8.4, 1.7, 5.4], [10, 1.1, 5.9]], crack: [[[3.6, 1.4], [4.2, 3.4], [3.4, 5.6]], [[4.4, 7], [3.8, 8.6]]] },
    { rows: [[0, 3.6, 3.8], [1.2, 3.5, 5.2], [1.8, 4, 5.4], [5, 3.4, 5.7], [8, 3, 6], [11, 2.5, 6.6], [13.4, 1.7, 7.3], [15, 1.1, 7.9]], crack: [[[4.4, 2], [5, 4.4], [4.2, 6.6], [5, 9]], [[3.4, 9], [4, 11]], [[6, 11], [5.4, 13]]] },
  ],
];
/** One spike of dripstone from its rows: lit on its left, shaded and deep on its right, pen where the table says. */
function spike(a, m, d) {
  const L = d.rows.map(([y, l]) => [l, y]), R = d.rows.map(([y, , r]) => [r, y]);
  const at = (k) => d.rows.map(([y, l, r]) => [l + (r - l) * k, y]);
  const at2 = (k, from, to) => d.rows.filter(([y]) => y >= from && y <= to).map(([y, l, r]) => [l + (r - l) * k, y]);
  if (d.spur) a.shape(pts(d.spur), m, { shade: .4 });
  const lines = [];
  for (const y of d.ring || []) { const row = d.rows.reduce((b, q) => (Math.abs(q[0] - y) < Math.abs(b[0] - y) ? q : b)); lines.push(`M${row[1]} ${y}Q${(row[1] + row[2]) / 2} ${y + .45} ${row[2]} ${y - .05}`); }
  for (const [x0, y0, x1, y1] of d.flute || []) lines.push(`M${x0} ${y0}L${x1} ${y1}`);
  for (const c of d.crack || []) lines.push(pathOf(c));
  a.shape(pts(L.concat(R.slice().reverse())), m, {
    shade: pts(at(.55).concat(R.slice().reverse())),
    deep: pts(at(.82).concat(R.slice().reverse())),
    lite: pts(at(.08).concat(at(.26).reverse())),
    spec: d.wet ? pts(at2(.3, d.wet[0], d.wet[1]).concat(at2(.4, d.wet[0], d.wet[1]).reverse())) : undefined,
    lines,
  });
  if (d.drop) a.shape(ell(d.drop[0], d.drop[1], .42, .72), m, { outer: false, edge: false, shade: ell(d.drop[0] + .2, d.drop[1] + .1, .25, .55) });
}
const DRIP_SIZE = [[6, 9], [6, 13], [8, 18]], MITE_SIZE = [[6, 7], [6, 11], [8, 16]];

/* ---- Wet fungi. No toadstool: a thin, slumping skin on a bare stalk, its rim sagging on one side and running to strands; pits where it rots. Flat washes, a pen line for the gills. ---- */
const FLESH = {
  teal: { base: '#3e5a55', shade: '#293c3a', deep: '#111919', lite: '#688a80', stalk: '#6a6e62', stalkDark: '#30332c', drop: '#7c9c90', glow: '#70ecd0' },
  violet: { base: '#4a4260', shade: '#2e2942', deep: '#14111c', lite: '#766b92', stalk: '#6a6570', stalkDark: '#312e38', drop: '#8d80a8', glow: '#b296ff' },
};
/** A cap's skin: x its middle, y its top, hw half its width, hh its height, lean of its crown, sag of its rim (+ right, - left lower). */
function skin(c) {
  const by = c.y + c.hh, lean = c.lean || 0, sag = c.sag || 0, hw = c.hw, hh = c.hh, x = c.x;
  return [[x - hw, by + Math.max(0, -sag)], [x - hw * .94, c.y + hh * .62], [x - hw * .62 + lean * .4, c.y + hh * .2], [x + lean - hw * .14, c.y], [x + lean + hw * .2, c.y + hh * .03], [x + hw * .66 + lean * .4, c.y + hh * .24], [x + hw * .95, c.y + hh * .64], [x + hw, by + Math.max(0, sag)],
    [x + hw * .72, by - hh * .1 + Math.max(0, sag) * .6], [x + hw * .3, by - hh * .24], [x - hw * .2, by - hh * .28], [x - hw * .68, by - hh * .1 + Math.max(0, -sag) * .6]];
}
function fungusCap(a, F, c) {
  const by = c.y + c.hh, hw = c.hw;
  // The gills: a dark torn skirt under the rim, seen from just below.
  const skirt = [[c.x - hw * .92, by - c.hh * .14 + Math.max(0, -(c.sag || 0))], [c.x - hw * .5, by + .28], [c.x - hw * .1, by + .1], [c.x + hw * .3, by + .34], [c.x + hw * .66, by + .06], [c.x + hw * .92, by - c.hh * .14 + Math.max(0, c.sag || 0)], [c.x + hw * .3, by - c.hh * .4], [c.x - hw * .3, by - c.hh * .4]];
  a.flat(pts(skirt), F.deep);
  for (const [dx, len, w] of c.tat || []) { run(a, [[c.x + dx, by - .2], [c.x + dx + .12, by + len * .75], [c.x + dx - .08, by + len * 1.3]], w || .36, .1, F.shade, true); a.flat(ell(c.x + dx - .08, by + len * 1.3, .17, .26), F.drip || F.lite); }
  const d = skin(c);
  a.flat(pts(d), F.base);
  // Shade on the far side, under the crown; never a highlight dot: one wet streak along the skin.
  a.flat(pts([d[3].map((n, i) => (i ? n + c.hh * .28 : n + hw * .2)), d[4], d[5], d[6], d[7], d[8], d[9], [c.x + hw * .05, by - c.hh * .32]]), F.shade);
  a.flat(pts([[c.x - hw * .78, c.y + c.hh * .62], [c.x - hw * .46, c.y + c.hh * .26], [c.x - hw * .1, c.y + c.hh * .14], [c.x - hw * .36, c.y + c.hh * .4]]), F.lite);
  // Rot runs down the skin in streaks from where it has gone soft; wrinkles from the crown where the skin has fallen in.
  for (const [dx, dy, r] of c.pits || []) run(a, [[c.x + dx, c.y + dy - r * 1.4], [c.x + dx * 1.1, c.y + dy], [c.x + dx * 1.22, c.y + dy + r * 2.6]], r * 1.2, r * .2, F.deep, true);
  if (hw > 3.2) { const cr = c.x + (c.lean || 0); a.line(`M${cr - hw * .1} ${c.y + c.hh * .14}Q${cr - hw * .36} ${c.y + c.hh * .4} ${c.x - hw * .5} ${by - c.hh * .34}`, { w: 1, c: F.deep }); a.line(`M${cr + hw * .16} ${c.y + c.hh * .16}Q${cr + hw * .34} ${c.y + c.hh * .36} ${c.x + hw * .44} ${by - c.hh * .4}`, { w: 1, c: F.deep }); }
  if (hw > 3.2) { const n = hw > 4.8 ? 5 : 3; for (let i = 0; i < n; i++) { const u = (i + .5) / n * 1.6 - .8; a.line(`M${c.x + u * hw} ${by - c.hh * .22}L${c.x + u * hw * 1.04} ${by + .1}`, { w: 1, c: F.deep }); } }
}
/** A bare stalk: flesh with its dark side, a torn collar high on a big one; wet rot at its foot. */
function stalk(a, F, s, w0, w1) {
  run(a, s.p, w0, w1, F.stalkDark, true, .16, s.p[0][0]);
  run(a, s.p.map(([x, y]) => [x - w0 * .16, y]), w0 * .42, w1 * .4, F.stalk, true, .14, s.p[0][1]);
  if (s.collar) { const [x, y] = s.collar; a.flat(pts([[x - 1, y - .1], [x, y - .34], [x + 1, y - .14], [x + .7, y + .7], [x + .3, y + .24], [x - .24, y + .96], [x - .5, y + .24]]), F.stalkDark); }
}
const rot = (a, F, list) => a.flat(pts(list), F.deep);
const GLOWCAPS = {
  // Teal, as tall as the concept's garden: one big one, its small ones crowded off it.
  glowcapTeal_0: { w: 24, h: 21, F: 'teal', parts: [
    { s: { p: [[6.8, 20], [6.5, 17], [7.2, 14.4]] }, w: [.8, .5], c: { x: 7.4, y: 11, hw: 2.8, hh: 2.7, lean: -.3, sag: .5, tat: [[-1.4, 1.2], [1, 1.8]], pits: [[.4, 1.3, .24]] } },
    { s: { p: [[19.5, 20], [19.8, 18], [19.4, 15.8]] }, w: [.8, .5], c: { x: 19.5, y: 13, hw: 2.8, hh: 2.7, lean: .3, sag: -.5, tat: [[-1.2, 1.6], [1.4, .9]] } },
    { s: { p: [[13.2, 20], [13.8, 15.6], [13.5, 12], [14, 9]], collar: [13.7, 11.2] }, w: [1.9, 1.1], c: { x: 13.2, y: 5, hw: 4.8, hh: 4.5, lean: .5, sag: .9, tat: [[-3.6, 1.6], [-1.5, 1], [2.4, 1.4, .4], [4, 2.6, .4]], pits: [[1.6, 1.6, .34], [-1.8, 2.5, .22], [2.8, 2.8, .2]] } },
  ], rot: [[10.6, 20], [11.6, 19], [13.4, 18.8], [15, 19.2], [16, 20]], rot2: [[5.4, 20], [6.6, 19.2], [8, 20]], rot3: [[18.4, 20], [19.6, 19.4], [20.6, 20]] },
  glowcapTeal_1: { w: 28, h: 22, F: 'teal', parts: [
    { s: { p: [[7.4, 21], [7.2, 19.4], [7.5, 18.2]] }, w: [.6, .4], c: { x: 7.2, y: 16.6, hw: 1.6, hh: 1.6, sag: .3, tat: [[.7, .7, .2]] } },
    { s: { p: [[10.5, 21], [10.9, 19.8], [10.6, 18.6]] }, w: [.7, .5], c: { x: 10.6, y: 16, hw: 2.7, hh: 2.6, sag: -.5, lean: .3, tat: [[-1.4, 1], [1.2, .7]] } },
    { s: { p: [[21.5, 21], [21.2, 19.8], [21.8, 18.6]] }, w: [.7, .5], c: { x: 22, y: 16, hw: 2.7, hh: 2.6, sag: .6, lean: -.4, tat: [[1.5, 1.2], [-.8, .7]], pits: [[.6, 1.2, .22]] } },
    { s: { p: [[18.8, 21], [18.3, 16.6], [19.5, 12.6], [19.3, 9]], collar: [19.2, 11] }, w: [1.8, 1.05], c: { x: 19.4, y: 5, hw: 5.5, hh: 4.5, lean: -.8, sag: -.9, tat: [[-4.8, 2.4, .42], [-3, 1.2], [.6, .9], [3.2, 1.6]], pits: [[1.2, 1.9, .36], [3.2, 2.7, .22]] } },
  ], rot: [[16.4, 21], [17.4, 20], [19, 19.8], [20.6, 20.2], [21.4, 21]], rot2: [[6, 21], [7.2, 20.4], [8.4, 21]], rot3: [[9.4, 21], [10.6, 20.4], [11.8, 21]] },
  glowcapTeal_2: { w: 32, h: 22, F: 'teal', parts: [
    { s: { p: [[27.4, 21], [27.7, 19.4], [27.5, 18]] }, w: [.6, .4], c: { x: 27.5, y: 16.6, hw: 1.7, hh: 1.6, sag: -.3, tat: [[-.7, .9, .2]] } },
    { s: { p: [[20.4, 21], [20, 19.6], [20.6, 18.2]] }, w: [.6, .4], c: { x: 20.9, y: 16.8, hw: 1.7, hh: 1.5, sag: .3 } },
    { s: { p: [[6.4, 21], [6.8, 18.8], [6.4, 16.6]] }, w: [.9, .6], c: { x: 6.4, y: 13, hw: 3.6, hh: 3.5, lean: .4, sag: -.7, tat: [[-2.8, 1.7, .34], [.2, 1], [2.3, 1.2]], pits: [[1.2, 1.2, .3]] } },
    { s: { p: [[10.5, 21], [10.2, 20.4], [10.5, 19.6]] }, w: [.7, .5], c: { x: 10.6, y: 17, hw: 2.6, hh: 2.6, lean: -.3, sag: .5, tat: [[1.5, .8]] } },
    { s: { p: [[18.8, 21], [17.9, 16], [18.7, 12.4], [18.4, 9]], collar: [18.4, 12] }, w: [1.8, 1.05], c: { x: 18.4, y: 5, hw: 5.4, hh: 4.5, lean: .8, sag: 1, tat: [[-3.8, 1], [-1.6, 1.5], [1.8, .9], [4.6, 3, .42]], pits: [[-1.6, 1.8, .34], [1.4, 2.8, .24], [3.4, 2.6, .18]] } },
  ], rot: [[16.2, 21], [17.2, 20.2], [18.8, 19.8], [20.4, 20.4], [21.4, 21]], rot2: [[5, 21], [6.4, 20.2], [7.6, 21]], rot3: [[26.2, 21], [27.4, 20.4], [28.6, 21]] },
  // Violet: squatter, drier, the small ones grown into the big one's skirt.
  glowcapViolet_0: { w: 28, h: 19, F: 'violet', parts: [
    { s: { p: [[8.6, 18], [7.8, 15], [7.4, 11.8]] }, w: [.8, .5], c: { x: 7.4, y: 8, hw: 3.6, hh: 3.5, lean: -.4, sag: -.6, tat: [[-2.6, 1.4, .34], [1.8, 1]], pits: [[-.8, 1.5, .26]] } },
    { s: { p: [[19.5, 18], [19.2, 16], [19.6, 13.8]] }, w: [.8, .5], c: { x: 19.5, y: 11, hw: 2.8, hh: 2.7, sag: .6, tat: [[1.5, 1.2], [-1, .7]] } },
    { s: { p: [[14.2, 18], [13.4, 14.4], [13.8, 11.6], [12.6, 9]], collar: [13.6, 11.4] }, w: [1.9, 1.1], c: { x: 12.4, y: 5, hw: 4.8, hh: 4.5, lean: .6, sag: .8, tat: [[3.8, 2.2, .42], [1.5, 1], [-2.6, 1]], pits: [[1.8, 1.4, .34], [-.8, 2.8, .24]] } },
  ], rot: [[11.8, 18], [13, 17.2], [14.6, 17.2], [16, 18]], rot2: [[7.4, 18], [8.6, 17.4], [9.8, 18]], rot3: [[18.4, 18], [19.5, 17.4], [20.6, 18]] },
  glowcapViolet_1: { w: 26, h: 20, F: 'violet', parts: [
    { s: { p: [[5.4, 19], [5.7, 18.4], [5.4, 17.6]] }, w: [.7, .5], c: { x: 5.4, y: 15, hw: 2.7, hh: 2.6, sag: .5, lean: -.3, tat: [[-1.2, .8], [1.3, .6]] } },
    { s: { p: [[11.5, 19], [11.2, 18.4], [11.6, 17.6]] }, w: [.7, .5], c: { x: 11.5, y: 15, hw: 2.7, hh: 2.6, sag: -.5, lean: .4, tat: [[-1.5, .8]], pits: [[.6, 1.2, .22]] } },
    { s: { p: [[20.5, 19], [20.9, 16.6], [20.4, 13.8]] }, w: [.8, .5], c: { x: 20.5, y: 11, hw: 2.7, hh: 2.7, lean: .3, sag: .6, tat: [[1.4, 1.1]] } },
    { s: { p: [[16.8, 19], [17.2, 15], [16.4, 11.8], [16.9, 9]], collar: [16.6, 11.6] }, w: [1.9, 1.1], c: { x: 16.3, y: 5, hw: 5.4, hh: 4.5, lean: -.6, sag: -1, tat: [[-4.8, 2.6, .42], [-1.8, 1], [1.6, 1.3], [4, .8]], pits: [[1.8, 2, .36], [-2.2, 2.8, .2]] } },
  ], rot: [[14.8, 19], [16, 18.2], [17.6, 18.2], [18.8, 19]], rot2: [[4.2, 19], [5.4, 18.5], [6.6, 19]], rot3: [[10.4, 19], [11.5, 18.5], [12.6, 19]] },
  glowcapViolet_2: { w: 32, h: 22, F: 'violet', parts: [
    { s: { p: [[26.4, 21], [26.7, 18], [26.3, 14.8]] }, w: [.8, .5], c: { x: 26.4, y: 12, hw: 2.7, hh: 2.7, lean: .3, sag: -.6, tat: [[-1.3, 1.2], [1.2, .7]] } },
    { s: { p: [[23.5, 21], [23.2, 20.2], [23.5, 19.4]] }, w: [.6, .4], c: { x: 23.5, y: 18, hw: 1.7, hh: 1.5, sag: .3 } },
    { s: { p: [[5.4, 21], [5.7, 20], [5.4, 18.6]] }, w: [.7, .5], c: { x: 5.4, y: 16, hw: 2.7, hh: 2.6, lean: .3, sag: .6, tat: [[1.5, 1]], pits: [[-.7, 1.3, .2]] } },
    { s: { p: [[11.5, 21], [11.2, 20.6], [11.6, 19.6]] }, w: [.7, .5], c: { x: 11.5, y: 17, hw: 2.7, hh: 2.6, lean: -.3, sag: -.5, tat: [[-1.5, .7]] } },
    { s: { p: [[18.2, 21], [17.5, 15.4], [18.6, 12], [18.4, 9]], collar: [18.2, 12] }, w: [1.9, 1.1], c: { x: 18.6, y: 5, hw: 5.4, hh: 4.5, lean: -.5, sag: 1, tat: [[-4, 1.2], [-.8, 1], [2.2, 1.4], [4.8, 2.8, .42]], pits: [[-1.4, 1.5, .34], [1.6, 2.5, .28]] } },
  ], rot: [[15.8, 21], [17, 20.2], [18.8, 20.1], [20.2, 21]], rot2: [[4.2, 21], [5.4, 20.4], [6.6, 21]], rot3: [[25.2, 21], [26.4, 20.5], [27.6, 21]] },
};
const HANGCAPS = {
  // Drooping sacs: a skin hanging off a bare stalk, its low end drawn out into a wet tail.
  hangcap_0: { w: 20, h: 21, parts: [
    { s: { p: [[7.5, 1], [7.7, 3.2], [7.4, 6]] }, w: [.6, .45], c: { x: 7.4, y: 6, hw: 2.7, hh: 2, sag: .4, tat: [[-1, .8]] }, tail: [[7.4, 7.6], [7.6, 9], [7.4, 10]] },
    { s: { p: [[12.7, 1], [12.6, 4.6], [11.9, 8], [11.2, 11.4]], collar: [12.2, 7] }, w: [1.7, 1], c: { x: 11, y: 11, hw: 4.7, hh: 3.8, lean: .4, sag: .8, tat: [[-3.4, 1.2], [3.6, 1.4]], pits: [[-1.4, 1.5, .3], [1.6, 1.2, .22]] }, tail: [[11.2, 14.4], [11.6, 16.4], [11.3, 18]] },
  ], plate: [[10.8, 1], [14.8, 1], [14.2, 1.9], [12.6, 2.2], [11.4, 1.8]] },
  hangcap_1: { w: 28, h: 23, parts: [
    { s: { p: [[9.5, 1], [9.3, 2.6], [9.5, 4.2]] }, w: [.6, .45], c: { x: 9.5, y: 4, hw: 2.7, hh: 2, sag: -.4, tat: [[1.2, .8]] }, tail: [[9.5, 5.6], [9.3, 7], [9.6, 8]] },
    { s: { p: [[20.5, 1], [20.8, 3.8], [20.4, 7.2]] }, w: [.6, .45], c: { x: 20.5, y: 7, hw: 3.6, hh: 2.8, lean: -.3, sag: .6, tat: [[-2.4, 1], [2.6, 1.2]], pits: [[.8, 1.2, .24]] }, tail: [[20.4, 9.4], [20.7, 11], [20.4, 12.4]] },
    { s: { p: [[13.6, 1], [13.9, 5], [14.8, 9], [14.3, 13.4]], collar: [14.4, 8] }, w: [1.7, 1], c: { x: 14, y: 13, hw: 4.7, hh: 3.8, lean: -.5, sag: -.8, tat: [[-3.6, 1.5], [3.2, 1]], pits: [[1.5, 1.5, .3], [-1, 2.2, .2]] }, tail: [[14.2, 16.4], [14.8, 18.2], [14.4, 20]] },
  ], plate: [[11.8, 1], [15.8, 1], [15, 1.9], [13.6, 2.3], [12.4, 1.8]] },
  hangcap_2: { w: 22, h: 22, parts: [
    { s: { p: [[6.5, 1], [6.6, 3], [7.5, 5], [7.2, 7.2]] }, w: [.65, .45], c: { x: 7, y: 7, hw: 3.6, hh: 2.8, lean: .3, sag: .6, tat: [[-2.2, 1.1], [2.4, .9]] }, tail: [[7.2, 9.4], [7.6, 11], [7.3, 12.2]] },
    { s: { p: [[16.5, 1], [16.3, 3.6], [16.6, 7]] }, w: [.6, .45], c: { x: 16.3, y: 7, hw: 2.7, hh: 2.4, sag: -.4, tat: [[1.2, 1]] }, tail: [[16.4, 8.9], [16.6, 10.2], [16.3, 11.4]] },
    { s: { p: [[12.5, 1], [12.3, 5], [12.9, 9], [12.5, 12.4]], collar: [12.5, 6.4] }, w: [1.7, 1], c: { x: 12.5, y: 12, hw: 5.4, hh: 3.8, lean: .6, sag: 1, tat: [[-4, 1.1], [-.6, .7], [4.4, 1.5]], pits: [[-1.6, 1.4, .32], [1.8, 1.8, .26], [-3.2, 2.4, .18]] }, tail: [[12.6, 15.4], [13, 17.2], [12.6, 19]] },
  ], plate: [[10.8, 1], [14.6, 1], [14, 1.8], [12.6, 2.2], [11.4, 1.8]] },
};
function growth(a, F, g) {
  for (const k of ['rot2', 'rot3']) if (g[k]) rot(a, F, g[k]);
  for (const part of g.parts) {
    stalk(a, F, part.s, part.w[0], part.w[1]);
    fungusCap(a, F, part.c);
    if (part.tail) run(a, part.tail, .6, .16, F.shade, true);
  }
  if (g.rot) rot(a, F, g.rot);
  if (g.plate) rot(a, F, g.plate);
}
const growthGlow = (g, F) => bloom(mask(g.w, g.h, (fill) => {
  for (const part of g.parts) {
    fill(pts(skin(part.c)));
    if (part.tail) { const [x, y] = part.tail[part.tail.length - 1]; fill(ell(x, y - .5, .4, .9)); }
  }
}), F.glow, 5, .72, .34);

/* ---- Threads: sticky hanging strands, as a cave gnat's: hair-thin, beaded with wet, each ending in a heavier drop. Beads and drops stand where the logical glow's bright pixels are. ---- */
// Strand: its points, its beads, its end. Back ones first.
const THREADS = {
  threadTeal_0: { w: 16, h: 30, s: [
    { p: [[6.5, 1], [6.5, 4.4], [7.3, 5.5], [6.5, 6.6], [6.6, 9], [7.6, 10.4], [7.6, 15.4], [8.6, 16.6], [8.5, 18.5]], b: [[6.5, 6.5], [7.5, 11.5], [8.5, 16.5]], e: [8.5, 18.5] },
    { p: [[10.8, 1], [10.5, 5.5], [10.6, 9], [9.6, 10], [9.5, 11.5]], b: [[10.5, 5.5], [9.5, 9.5]], e: [9.5, 11.5] },
    { p: [[11.3, 1], [11, 8], [11.6, 10.4], [11.5, 26.5]], b: [[11.5, 7.5], [11.5, 13.5], [11.5, 19.5], [11.5, 25.5]], e: [11.5, 26.5] },
  ] },
  threadTeal_1: { w: 20, h: 26, s: [
    { p: [[6.5, 1], [6.5, 4.4], [7.5, 5.4], [7.5, 8.4], [8.5, 9.4], [8.5, 18.4], [7.5, 19.4], [7.5, 22.5]], b: [[7.5, 6.5], [8.5, 16.5], [7.5, 21.5]], e: [7.5, 22.5] },
    { p: [[9.5, 1], [9.5, 7.5], [9.2, 9.4], [9.5, 11.5]], b: [[9.5, 7.5]], e: [9.5, 11.5] },
    { p: [[13.5, 1], [13.5, 4.4], [12.5, 5.4], [12.6, 9.5], [12.5, 13.5]], b: [[12.5, 5.5], [12.5, 9.5]], e: [12.5, 13.5] },
    { p: [[15.5, 1], [15.5, 6.5], [15.6, 11.5], [16.5, 12.4], [16.5, 16.5]], b: [[15.5, 6.5], [15.5, 11.5]], e: [16.5, 16.5] },
  ] },
  threadTeal_2: { w: 24, h: 30, s: [
    { p: [[8.5, 1], [8.5, 11.5], [8.4, 16.5], [8.5, 18.5]], b: [[8.5, 6.5], [8.5, 11.5], [8.5, 16.5]], e: [8.5, 18.5] },
    { p: [[10.5, 1], [10.5, 9.4], [9.5, 10.4], [9.5, 19], [10.5, 20], [10.5, 24.5]], b: [[10.5, 7.5], [9.5, 13.5], [9.5, 19.5]], e: [10.5, 24.5] },
    { p: [[13.5, 1], [13.5, 11.4], [14.5, 12.4], [14.5, 20.5]], b: [[13.5, 5.5], [13.5, 9.5], [14.5, 13.5], [14.5, 17.5]], e: [14.5, 20.5] },
    { p: [[16.5, 1], [16.5, 9.4], [17.5, 10.4], [17.5, 16.4], [18.3, 17.4], [17.5, 18.4], [17.5, 23.4], [16.5, 24.4], [16.5, 26.5]], b: [[16.5, 6.5], [17.5, 11.5], [17.5, 16.5], [17.5, 21.5]], e: [16.5, 26.5] },
    { p: [[18.5, 1], [18.5, 6.4], [19.5, 7.4], [19.5, 11.4], [20.5, 12.4], [20.5, 23.5]], b: [[19.5, 7.5], [20.5, 13.5], [20.5, 19.5]], e: [20.5, 23.5] },
  ] },
  threadViolet_0: { w: 20, h: 28, s: [
    { p: [[9.5, 1], [9.5, 3.4], [8.5, 4.4], [8.5, 8.4], [7.5, 9.4], [7.5, 18.5]], b: [[8.5, 5.5], [7.5, 9.5], [7.5, 13.5], [7.5, 17.5]], e: [7.5, 18.5] },
    { p: [[11.5, 1], [11.5, 5.4], [10.5, 6.4], [10.5, 9.4], [9.5, 10.4], [9.5, 24.5]], b: [[10.5, 6.5], [9.5, 11.5], [9.5, 16.5], [9.5, 21.5]], e: [9.5, 24.5] },
    { p: [[14.5, 1], [14.4, 13.5], [14.5, 17.4], [15.5, 18.4], [15.5, 20.5]], b: [[14.5, 7.5], [14.5, 13.5], [15.5, 19.5]], e: [15.5, 20.5] },
  ] },
  threadViolet_1: { w: 20, h: 29, s: [
    { p: [[6.5, 1], [6.5, 4.4], [5.5, 5.4], [5.5, 10.5]], b: [[5.5, 5.5], [5.5, 9.5]], e: [5.5, 10.5] },
    { p: [[13.5, 1], [13.5, 15], [12.5, 16.5], [12.5, 18.5], [11.6, 19.6]], b: [[13.5, 7.5], [13.5, 13.5]] },
    { p: [[10.5, 1], [10.5, 11.4], [11.5, 12.4], [11.5, 17.4], [10.6, 18.5], [10.6, 19.6], [11.5, 20.6], [11.5, 23.5]], b: [[10.5, 6.5], [10.5, 11.5], [11.5, 16.5], [10.5, 19.5]], e: [11.5, 23.8], big: true },
    { p: [[15.5, 1], [15.5, 3.4], [16.3, 4.4], [15.5, 5.4], [15.5, 15.5]], b: [[15.5, 5.5], [15.5, 9.5], [15.5, 13.5]], e: [15.5, 15.5] },
  ] },
  threadViolet_2: { w: 24, h: 31, s: [
    { p: [[7.5, 1], [7.5, 11.4], [8.5, 12.4], [8.5, 21.5]], b: [[7.5, 5.5], [7.5, 9.5], [8.5, 13.5], [8.5, 17.5]], e: [8.5, 21.5] },
    { p: [[9.5, 1], [9.5, 9.4], [10.5, 10.4], [10.5, 16.4], [11.3, 17.4], [10.5, 18.4], [10.5, 23.4], [9.5, 24.4], [9.5, 26.5]], b: [[9.5, 6.5], [10.5, 11.5], [10.5, 16.5], [10.5, 21.5]], e: [9.5, 26.5] },
    { p: [[13.5, 1], [13.5, 6.4], [14.5, 7.4], [14.5, 11.4], [15.5, 12.4], [15.5, 26.4], [16.5, 27.5]], b: [[14.5, 7.5], [15.5, 13.5], [15.5, 19.5], [15.5, 25.5]], e: [16.5, 27.5] },
    { p: [[16.5, 1], [16.5, 10.5]], b: [[16.5, 5.5], [16.5, 9.5]], e: [16.5, 10.5] },
    { p: [[19.5, 1], [19.4, 6.5], [19.5, 12.5]], b: [[19.5, 6.5], [19.5, 11.5]], e: [19.5, 12.5] },
  ] },
};
const THREAD_INK = {
  teal: { strand: '#27504b', dark: '#142825', bead: '#5f9486', end: '#7fae9f', mat: '#0f1a18', glow: '#6cecd0' },
  violet: { strand: '#3a3060', dark: '#1a1530', bead: '#75689c', end: '#93879f', mat: '#120f1c', glow: '#b49aff' },
};
function threads(a, T, t) {
  for (const [k, s] of t.s.entries()) {
    run(a, s.p, .58, .36, T.dark, true, .1, k);
    run(a, s.p.map(([x, y]) => [x - .05, y]), .3, .2, T.strand, true);
    for (const [x, y] of s.b) { a.flat(pts([[x, y - .78], [x + .36, y - .1], [x + .24, y + .4], [x, y + .56], [x - .3, y + .36], [x - .34, y - .12]]), T.bead); }
    if (s.e) {
      const [x, y] = s.e, big = s.big ? 1.3 : 1;
      a.flat(pts([[x, y - 1.1], [x + .62 * big, y - .1], [x + .5 * big, y + .6], [x + .1, y + 1.05 * big], [x - .44 * big, y + .66], [x - .62 * big, y - .08]]), T.end);
      a.flat(pts([[x + .1, y - .5], [x + .56 * big, y], [x + .42 * big, y + .6], [x + .1, y + .98 * big], [x + .06, y + .2]]), T.bead);
      // Two hairs off the drop, by the side the air last moved it.
      a.line(`M${x - .4} ${y}L${x - 1.05} ${y + .3}`, { w: .9, c: T.strand });
      a.line(`M${x + .46} ${y - .1}L${x + 1} ${y + .34}`, { w: .9, c: T.strand });
    }
  }
  // A clot of old thread where each one grips the ceiling.
  for (const [k, s] of t.s.entries()) { const x = s.p[0][0], w = .5 + (k % 3) * .16; a.flat(pts([[x - w, 1], [x + w, 1], [x + w * .6, 1.6], [x + .1, 2.2 + (k % 2) * .4], [x - w * .6, 1.6]]), T.mat); }
}
const threadGlow = (t, T) => bloom(mask(t.w, t.h, (fill, stroke) => {
  for (const s of t.s) {
    for (const [x, y] of s.b) fill(ell(x, y - .1, .5, .7));
    if (s.e) fill(ell(s.e[0], s.e[1], s.big ? .9 : .7, .95));
  }
}), T.glow, 3.5, .95, .55);
const threadHaze = (t, T) => bloom(mask(t.w, t.h, (fill, stroke) => { for (const s of t.s) stroke(pathOf(s.p), .55); }), T.glow, 5, .3);
function stacked(c, d) { const x = c.getContext('2d'); x.drawImage(d, 0, 0); return c; }

/* ---- Roots from the surface, drawn wet and split; only a bit of pale new growth at their tips gives light. ---- */
const ROOT = { dark: '#2e2118', base: '#57432f', lite: '#7c6548', tip: '#a9b98a', glow: '#b4f07c' };
const ROOTS = {
  roots_0: { w: 10, h: 23, r: [
    { p: [[9, 1], [9, 5], [8.6, 7], [8.4, 9], [7.6, 10.5], [7.5, 13.9]], w: [1.9, .5], tip: [7.5, 13.2] },
    { p: [[3, 1], [3.2, 3], [4, 5], [4, 7.5], [4.6, 8.4], [3.8, 10], [4.4, 13], [3.8, 16], [4.5, 18], [4.5, 20.9]], w: [1.9, .5], tip: [4.5, 20.2] },
    { p: [[3.8, 15], [3.1, 16.2], [3.5, 17.9]], w: [.6, .3], tip: [3.5, 17.5] },
  ] },
  roots_1: { w: 20, h: 35, r: [
    { p: [[14, 1], [14, 2.8], [13, 3.6], [13.2, 5], [14, 6], [13.6, 8], [12.6, 9.5], [12.5, 13.5], [11.6, 14.6], [11.5, 16.9]], w: [1.9, .5], tip: [11.5, 16.2] },
    { p: [[8, 1], [8, 5.5], [7, 6.5], [8, 7.6], [8, 12.5], [7, 13.5], [7, 16], [5.5, 17.5], [4.6, 20], [4.5, 22], [3.5, 24], [2.6, 27], [2.5, 32.9]], w: [2, .45], tip: [2.5, 32.2] },
    { p: [[3.8, 22.8], [4.5, 24], [4.5, 26.9]], w: [.6, .3], tip: [4.5, 26.2] },
  ] },
  roots_2: { w: 18, h: 37, r: [
    { p: [[13, 1], [13, 4.5], [14, 6], [13.8, 10], [13.6, 16], [14.4, 17.5], [14.5, 20.9]], w: [1.9, .55], tip: [14.5, 20.2] },
    { p: [[13.8, 12.2], [15, 13.2], [15.5, 14.9]], w: [.6, .3], tip: [15.5, 14.3] },
    { p: [[7, 1], [7, 3.5], [6, 4.5], [6, 6.5], [5, 7.5], [5, 15.5], [4.6, 20], [4.8, 23.5], [5.2, 26.4], [6.5, 28], [6.5, 32.9]], w: [2, .5], tip: [6.5, 32.2] },
    { p: [[5, 24], [6.3, 25.4], [6.2, 27]], w: [.55, .3] },
    { p: [[6.4, 30], [5.5, 33], [5.5, 34.9]], w: [.55, .3], tip: [5.5, 34.2] },
  ] },
};
function root(a, r, k) {
  run(a, r.p, r.w[0], r.w[1], ROOT.dark, true, .22, k);
  run(a, r.p.map(([x, y]) => [x - r.w[0] * .14, y]), r.w[0] * .5, r.w[1] * .5, ROOT.base, true, .3, k + 3);
  if (r.w[0] > 1) { const from = Math.floor(r.p.length * .2), head = r.p.slice(from, from + 3).map(([x, y]) => [x - .34, y]); run(a, head, .26, .1, ROOT.lite, true); }
  if (r.tip) a.flat(ell(r.tip[0], r.tip[1], .34, .78), ROOT.tip);
}
const rootsGlow = (t) => bloom(mask(t.w, t.h, (fill) => { for (const r of t.r) if (r.tip) fill(ell(r.tip[0], r.tip[1], .42, .8)); }), ROOT.glow, 3, .7, .55);

/* ---- The heat's cracks, in the rock: a charred seam, burning inside it. What burns may be full. ---- */
const CRACKS = [
  { p: [[4.4, 13], [6.5, 12.2], [8.4, 12.7], [9.6, 11.4], [11.5, 11.4], [12.6, 12.5], [15, 12.3], [16.5, 10.4], [17.4, 12], [19.6, 12.6]], hot: [[6.4, 12.3], [10.4, 11.4], [16.5, 10.9], [18.6, 12.4]], forks: [[[8.4, 12.7], [7.6, 14.4]], [[15, 12.3], [14.4, 13.6]]] },
  { p: [[4.4, 12.4], [6.6, 12.4], [7.6, 13.5], [10.5, 13.5], [12.5, 15], [14.2, 16.6], [14.8, 17.6], [16.5, 14.4], [18.2, 13], [19.6, 12.2]], hot: [[6.4, 12.4], [9.6, 13.5], [14.4, 16.8], [18.6, 12.7]], forks: [[[10.5, 13.5], [11.2, 12.2]], [[16.5, 14.4], [17.4, 15.8]]] },
  { p: [[4.4, 12.6], [6.5, 11.6], [7.6, 10.4], [9.4, 11.4], [11, 12.6], [15, 12.4], [16.4, 11.2], [18, 12.6], [19.6, 12.4]], hot: [[7.6, 10.8], [9.2, 11.3], [12.8, 12.5], [16.5, 11.5]], forks: [[[11, 12.6], [10.2, 13.8]], [[15, 12.4], [15.6, 13.7]]] },
  { p: [[4.4, 12.6], [6, 11.6], [9.5, 11.5], [10.6, 12.5], [12, 11.3], [13.2, 9.4], [14.4, 12.5], [16, 13.6], [16.6, 15.2], [18.4, 12.5], [19.2, 12.2]], hot: [[7.4, 11.5], [12, 11.2], [14.5, 12.6], [18.2, 12.7]], forks: [[[9.5, 11.5], [8.8, 13]], [[13.2, 9.4], [14.2, 8.8]]] },
];
const CHAR = '#150c09', SEAM = '#5a2110', BURN = '#d4561a', WHITE = '#f6bc62';
function crack(a, c) {
  for (const f of c.forks) a.line(pathOf(f), { w: 1.6, c: CHAR });
  run(a, c.p, 1.5, 1.3, CHAR, false, .5, 2);
  run(a, c.p, .95, .85, SEAM, false, .4, 5);
  run(a, c.p, .46, .38, BURN, false, .5, 7);
  for (const [x, y] of c.hot) a.flat(ell(x, y, .5, .28), WHITE);
}
const crackGlow = (c) => bloom(mask(24, 24, (fill, stroke) => { stroke(pathOf(c.p), .85); for (const [x, y] of c.hot) fill(ell(x, y, .7, .5)); }), '#ff7a28', 5, 1, .35);

/* ---- A geode: a split nodule in the rock, its husk grey, crystal teeth round a black hollow. ---- */
const GEODES = [
  { c: [11.8, 11.9], tone: { rind: '#17151d', husk: { base: '#58546a', shade: '#393648', lite: '#7c7890', ink: '#0e0c14' }, tooth: { base: '#6555a0', shade: '#40357a', deep: '#2a2352', lite: '#9d8fd0', ink: '#0d0a1c' }, hollow: '#07060b' },
    rind: [[11, 5.2], [14.6, 5.8], [17.2, 8], [18.6, 11.4], [17.9, 15.2], [15.6, 17.6], [12, 18.8], [8.6, 18], [6.6, 16], [5.2, 12.6], [5.9, 9], [8, 6.4]],
    husk: [[11.4, 6.8], [14.4, 7.4], [16.4, 9.2], [17.3, 11.8], [16.6, 14.8], [14.6, 16.6], [11.8, 17.4], [9, 16.6], [7.4, 14.8], [6.5, 12.2], [7.2, 9.6], [8.9, 7.6]],
    hollow: [[12.5, 9.4], [14.4, 10], [15.2, 12], [14.6, 14], [12.8, 15], [10.8, 14.6], [9.4, 13.6], [9.6, 11.6], [10.6, 10]],
    // Angle in degrees from the hollow's middle (0 east, 90 south), root's reach, tip's reach, width: each by hand, none alike.
    teeth: [[-100, 5, 2.4, 2.2], [-62, 5, 2.9, 2], [-24, 5, 2.5, 2.4], [14, 4.9, 2.2, 1.9], [52, 5, 2.7, 2.3], [92, 5, 2.5, 1.8], [128, 4.9, 2, 2.4], [168, 5, 2.6, 2], [-158, 5, 2.2, 2.3], [-130, 5, 2.9, 1.8]] },
  { c: [12.8, 12.3], tone: { rind: '#17130f', husk: { base: '#5f5a52', shade: '#3d3a34', lite: '#827c70', ink: '#100d09' }, tooth: { base: '#a8762e', shade: '#70491c', deep: '#4b2f12', lite: '#d8ac68', ink: '#170d03' }, hollow: '#080503' },
    rind: [[11.4, 6.2], [15.2, 6.4], [18, 8.8], [19.6, 12], [18.6, 15.4], [16, 17.6], [12.6, 18.8], [9.4, 18], [7.2, 15.8], [6.2, 12.6], [7, 9], [9, 7]],
    husk: [[11.8, 7.5], [15, 7.7], [17, 9.6], [18.3, 12.2], [17.4, 14.8], [15.4, 16.6], [12.6, 17.5], [10, 16.8], [8.2, 15], [7.4, 12.4], [8.1, 9.8], [9.8, 8.1]],
    hollow: [[12, 10.4], [13.4, 11], [15, 10.6], [15.4, 12.4], [14.2, 13.4], [13, 14.6], [11.4, 13.8], [10, 13], [10.6, 11.4]],
    teeth: [[-110, 4.9, 2, 2.6], [-72, 4.8, 2.6, 1.8], [-34, 4.9, 2, 2.8], [8, 4.8, 2.8, 2], [46, 4.7, 2.2, 2.6], [84, 4.7, 2.8, 2], [122, 4.8, 2.2, 2.6], [160, 4.8, 2.9, 1.8], [-160, 4.8, 2, 2.6]] },
];
const toothOf = (g, [deg, r0, r1, w]) => {
  const t = deg * Math.PI / 180, [cx, cy] = g.c, ux = Math.cos(t), uy = Math.sin(t), nx = -uy, ny = ux, sc = 1;
  return [[cx + ux * r0 + nx * w / 2, cy + uy * r0 + ny * w / 2], [cx + ux * (r1 + 1.1) + nx * w * .34, cy + uy * (r1 + 1.1) + ny * w * .34], [cx + ux * r1 * sc, cy + uy * r1 * sc], [cx + ux * (r1 + .9) - nx * w * .28, cy + uy * (r1 + .9) - ny * w * .28], [cx + ux * r0 - nx * w / 2, cy + uy * r0 - ny * w / 2]];
};
function geode(a, g) {
  a.flat(pts(g.rind), g.tone.rind);
  a.shape(pts(g.husk), g.tone.husk, { outer: false, shade: .7 });
  a.flat(pts(g.hollow), g.tone.hollow);
  for (const t of g.teeth) {
    const d = toothOf(g, t);
    a.shape(pts(d), g.tone.tooth, { outer: false, shade: pts([d[0], d[1], d[2]]), lite: pts([d[2], d[3], d[4]]) });
  }
}
const geodeGlow = (g, tint) => bloom(mask(24, 24, (fill) => { for (const t of g.teeth) fill(pts(toothOf(g, t))); }), tint, 6, .75, .25);

/* ---- Fossils, in their block of stone: pale, worn, only half out of it. No glow. ---- */
const BONE = { base: '#6e695b', shade: '#49453b', deep: '#2b2822', lite: '#918b79', ink: '#14120d' };
function ammonite(a) {
  // A worn disc of matrix, chipped; the coil cut into it by hand.
  a.shape(pts([[10, 7.1], [14, 7.1], [16.6, 9], [17.9, 11.6], [17.6, 14.4], [15.4, 16.8], [12.4, 17.9], [9.4, 17.4], [7.5, 15], [7.1, 11.8], [8.1, 8.8]]), BONE, { outer: false, shade: .9 });
  const coil = [], cx = 12.5, cy = 12.5;
  for (let t = 0; t <= 15; t += .35) { const r = .55 + t * .32; coil.push([cx + Math.cos(t + 1) * r, cy + Math.sin(t + 1) * r]); }
  a.line(pathOf(coil), { w: 2, c: BONE.ink });
  for (const [deg, r0, r1] of [[10, 3.4, 4.6], [40, 3.6, 4.8], [70, 3.6, 4.8], [100, 3.8, 4.9], [130, 3.7, 4.9], [165, 3.8, 5], [200, 3.7, 4.8], [235, 3.8, 5], [270, 3.8, 4.9], [305, 3.6, 4.7], [335, 3.5, 4.7], [60, 1.8, 2.8], [150, 1.9, 2.8], [240, 1.8, 2.7], [330, 1.8, 2.7]]) {
    const t = deg * Math.PI / 180;
    a.line(`M${cx + Math.cos(t) * r0} ${cy + Math.sin(t) * r0}L${cx + Math.cos(t + .14) * r1} ${cy + Math.sin(t + .14) * r1}`, { w: 1.1, c: BONE.deep });
  }
  a.flat(pts([[15, 8.8], [16.6, 10], [17, 11.6], [16, 10.8]]), BONE.lite);
}
function ribcage(a) {
  // A spine in an arc, ribs curving down from it, the far ones broken short.
  const spine = [[5.4, 9.2], [7, 7.7], [9.4, 6.8], [12.4, 6.5], [15.4, 6.9], [17.6, 7.9], [19.2, 9.4]];
  for (const [x0, y0, x1, y1, x2, y2] of [[5.8, 8.8, 6.1, 11, 6.7, 12.4], [7.8, 7.4, 7.6, 11, 8.3, 16.8], [11, 6.6, 11, 10, 11.6, 14.8], [14.8, 6.8, 15.1, 10, 15.6, 14.4], [17.6, 8, 18.3, 11, 19.6, 16.8], [9.4, 6.9, 9.2, 8.2, 9.5, 9.6], [13, 6.6, 13.2, 7.6, 13, 9]])
    run(a, [[x0, y0], [x1, y1], [x2, y2]], .78, .32, BONE.base, true, .22, x0);
  run(a, spine, 1.5, 1.2, BONE.shade, true, .22, 1);
  run(a, spine.map(([x, y]) => [x, y - .2]), .68, .56, BONE.lite, true, .3, 2);
  for (const [x, y] of [[8, 7.2], [10.6, 6.6], [13.2, 6.5], [15.8, 7], [17.8, 8]]) a.line(`M${x} ${y - .7}L${x + .1} ${y + .6}`, { w: 1.3, c: BONE.ink });
}
function fish(a) {
  // Something flat and blunt, a plate of its head and a socket; a splinter of bone under it.
  a.shape(pts([[7.1, 11.5], [8.4, 10.2], [9.8, 9.2], [13, 9], [15, 9.8], [17, 11.4], [15.8, 12.4], [14.6, 13], [12.2, 13.9], [9.6, 13.7], [8.2, 12.6]]), BONE, { outer: false, shade: pts([[7.1, 11.5], [8.2, 12.6], [9.6, 13.7], [12.2, 13.9], [14.6, 13], [15.8, 12.4], [17, 11.4], [12, 12.2]]), lines: ['M10.6 9.4L10 12.6L11 13.8', 'M12.2 9.4L12 11.6', 'M8.6 11.4L10 11.4'] });
  a.flat(ell(13.9, 10.5, .55, .42), BONE.ink);
  for (const [x, y] of [[11.2, 12.8], [12.6, 12.6], [13.8, 12.2]]) a.line(`M${x} ${y - .5}L${x + .3} ${y + .4}`, { w: 1, c: BONE.deep });
  run(a, [[8, 15.5], [11, 15.4], [13.5, 15.8], [15.9, 16.6]], .8, .4, BONE.shade, true, .3, 3);
}

/* ---- The spire's ruin: a cold metal cache with its one small lamp, and the dark lens set in the rock. As the zfittings. ---- */
const CACHE = { lid: { base: '#55707a', shade: '#3a4f58', deep: '#26353c', lite: '#7c98a0', spec: '#fff', ink: '#0a1216' }, body: { base: '#415760', shade: '#2d3e46', deep: '#1c2930', lite: '#60797f', ink: '#0a1216' }, iron: { base: '#20292e', shade: '#141a1e', lite: '#39454c', ink: '#070b0d' }, rust: '#5a3a26', lamp: '#4f9c8c' };
function cache(a) {
  // Its flanges behind it, the body, the heavier lid; panes dead along the lid, one small lamp in its lock.
  a.shape(poly(1.1, 9, 3, 9, 3, 12.4, 1.1, 11.8), CACHE.iron, { shade: .3 });
  a.shape(poly(21, 9, 22.9, 9, 22.9, 11.8, 21, 12.4), CACHE.iron, { shade: .3 });
  a.shape(poly(2.1, 8.6, 21.9, 8.6, 21.9, 16, 2.1, 16), CACHE.body, { shade: poly(2.1, 13.7, 21.9, 13.7, 21.9, 16, 2.1, 16), lite: poly(3, 9.2, 5, 9.2, 5, 15.2, 3, 15.2), lines: ['M5.2 9L5.2 15.6', 'M18.4 9.2L18.4 15.6'] });
  a.flat(poly(18.6, 13.6, 20.6, 13.2, 21.2, 14, 20.4, 15.4, 19, 15.6), CACHE.rust);
  a.flat(poly(5.4, 14.8, 7.8, 14.4, 8.8, 15, 7.6, 15.7, 5.6, 15.8), CACHE.rust);
  a.line('M15 10.4L15.8 11.2L15.4 12.4', { w: 1.1 });
  a.shape(poly(3, 3.1, 21, 3.1, 22, 4.6, 22, 8.9, 2, 8.9, 2, 4.6), CACHE.lid, { shade: poly(2, 7, 22, 7, 22, 8.9, 2, 8.9), deep: poly(2, 8.1, 22, 8.1, 22, 8.9, 2, 8.9), lite: poly(3.2, 3.5, 20.8, 3.5, 21.6, 4.5, 2.4, 4.5), lines: ['M2.4 4.7L21.6 4.7'] });
  for (const x of [6.6, 9.4, 13.2, 16]) a.flat(poly(x, 5.3, x + 2, 5.3, x + 2, 6.2, x, 6.2), '#2c4a52');
  a.flat(poly(13.2, 5.3, 15.2, 5.3, 14.4, 6.2, 13.2, 6.2), '#1c2d33');
  a.line('M19 3.4L18.4 5.2L19 6.2', { w: 1 });
  a.shape(poly(9.9, 9.9, 14.1, 9.9, 14.1, 13.1, 9.9, 13.1), CACHE.iron, { outer: false, lite: poly(9.9, 9.9, 14.1, 9.9, 14.1, 10.3, 9.9, 10.3) });
  a.flat(poly(10.9, 11, 13.1, 11, 13.1, 11.9, 10.9, 11.9), CACHE.lamp);
}
const LENS = { glass: { base: '#2a2450', shade: '#1a1636', deep: '#0f0c22', lite: '#453e74', spec: '#fff', ink: '#050410' }, bar: { base: '#5b6670', shade: '#3e4750', lite: '#7c8690', ink: '#0c0f13' }, foot: { base: '#242a33', shade: '#171b22', lite: '#363e4a', ink: '#080a0e' } };
function lens(a) {
  a.shape(poly(7.1, 5, 18, 5, 17.8, 6, 7.2, 6), LENS.bar, { shade: poly(7.1, 5.6, 18, 5.6, 17.8, 6, 7.2, 6), lines: ['M10 5L10 6', 'M15 5L15 6'] });
  a.shape(poly(7.2, 18.1, 18, 18.1, 18, 19, 7.1, 19), LENS.foot, { outer: false });
  const glass = 'M12.5 5.9C15 6.2 17 9 17 12C17 15 15 17.8 12.5 18.1C10 17.8 8 15 8 12C8 9 10 6.2 12.5 5.9Z';
  a.shape(glass, LENS.glass, {
    shade: 'M12.5 5.9C15 6.2 17 9 17 12C17 15 15 17.8 12.5 18.1Z',
    deep: 'M14.8 7.4C16.2 8.8 17 10.4 17 12C17 14 16.2 15.4 14.8 16.6C15.6 14 15.8 10.4 14.8 7.4Z',
    lite: 'M9.2 9.2C9.8 8 10.6 7.2 11.6 6.8C10.8 8.4 10.2 9.8 10 11.2L8.9 11.2C8.9 10.5 9 9.8 9.2 9.2Z',
    lines: ['M10.4 13.6L11.6 14.6L11.2 16'],
  });
}

Subjects.cavelife = function () {
  for (const name of Object.keys(CRYSTALS)) {
    const c = crystal(name);
    out(`caves/${name}`, c);
    out(`caves/${name}_glow`, Bloom(c, crystalM(name).glow));
  }
  STONE.forEach((m, band) => {
    for (let v = 0; v < 3; v++) {
      out(`caves/stalactite_${band}_${v}`, bake(DRIP_SIZE[v][0], DRIP_SIZE[v][1], (a) => spike(a, m, STALACTITE[band][v])));
      out(`caves/stalagmite_${band}_${v}`, bake(MITE_SIZE[v][0], MITE_SIZE[v][1], (a) => spike(a, m, STALAGMITE[band][v])));
    }
  });
  for (const [name, g] of Object.entries(GLOWCAPS)) {
    const F = FLESH[g.F];
    out(`caves/${name}`, bake(g.w, g.h, (a) => growth(a, F, g)));
    out(`caves/${name}_glow`, growthGlow(g, F));
  }
  for (const [name, g] of Object.entries(HANGCAPS)) {
    out(`caves/${name}`, bake(g.w, g.h, (a) => growth(a, FLESH.violet, g)));
    out(`caves/${name}_glow`, growthGlow(g, FLESH.violet));
  }
  for (const [name, t] of Object.entries(THREADS)) {
    const T = THREAD_INK[name.includes('Teal') ? 'teal' : 'violet'];
    out(`caves/${name}`, bake(t.w, t.h, (a) => threads(a, T, t)));
    out(`caves/${name}_glow`, stacked(threadHaze(t, T), threadGlow(t, T)));
  }
  for (const [name, t] of Object.entries(ROOTS)) {
    out(`caves/${name}`, bake(t.w, t.h, (a) => t.r.forEach((r, k) => root(a, r, k))));
    out(`caves/${name}_glow`, rootsGlow(t));
  }
  CRACKS.forEach((c, v) => {
    out(`caves/heatCrack_${v}`, bake(24, 24, (a) => crack(a, c)));
    out(`caves/heatCrack_${v}_glow`, crackGlow(c));
  });
  GEODES.forEach((g, v) => {
    out(`caves/geode_${v}`, bake(24, 24, (a) => geode(a, g)));
    out(`caves/geode_${v}_glow`, geodeGlow(g, v ? '#ffb060' : '#a488ff'));
  });
  out('caves/fossil_0', bake(24, 24, ammonite));
  out('caves/fossil_1', bake(24, 24, ribcage));
  out('caves/fossil_2', bake(24, 24, fish));
  out('caves/spireCache_0', bake(24, 16, cache));
  out('caves/spireCache_0_glow', bloom(mask(24, 16, (fill) => fill(poly(10.9, 11, 13.1, 11, 13.1, 11.9, 10.9, 11.9))), '#64e8cc', 3, 1, .7));
  out('caves/spireLamp_0', bake(24, 24, lens));
};
/** A crystal's whole body as its glow: the same as the ice's. */
function Bloom(c, tint) { return bloom(c, tint, 6, .8); }
})();

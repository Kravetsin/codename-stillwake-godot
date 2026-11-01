// The planet's plants, each part placed. So far the acacia: a dark forked trunk under flat layered plates of leaves, strands hanging from them.
(function () {
'use strict';
const { ell, poly, rng, bake, canvas, LIGHT } = Anime;

const BARK = { base: '#4d3f41', shade: '#33282c', deep: '#221a1e', lite: '#665556', spec: '#665556', ink: '#0f0a0c' };
const BACK = { base: '#1c3833', shade: '#142a27', lite: '#1c3833', spec: '#1c3833', ink: '#091412' };
const MID = { base: '#2f5a4a', shade: '#214337', lite: '#3e6f58', spec: '#3e6f58', ink: '#091412' };
const TOP = { base: '#4a7c60', shade: '#37644f', lite: '#689873', spec: '#689873', ink: '#091412' };

/** A smooth line through the points, about a pixel a step: each step's place, its way along from 0 to 1 and its normal. */
function along(pts) {
  const out = [];
  for (let i = 0; i < pts.length - 1; i++) {
    const p0 = pts[Math.max(0, i - 1)], p1 = pts[i], p2 = pts[i + 1], p3 = pts[Math.min(pts.length - 1, i + 2)];
    const n = Math.max(2, Math.round(Math.hypot(p2[0] - p1[0], p2[1] - p1[1])));
    for (let k = 0; k < n; k++) {
      const t = k / n, f = (a, b, c, d) => .5 * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (3 * b - a - 3 * c + d) * t * t * t);
      out.push([f(p0[0], p1[0], p2[0], p3[0]), f(p0[1], p1[1], p2[1], p3[1])]);
    }
  }
  out.push(pts[pts.length - 1]);
  return out.map((p, i) => {
    const q = out[Math.min(out.length - 1, i + 1)], o = out[Math.max(0, i - 1)], dx = q[0] - o[0], dy = q[1] - o[1], l = Math.hypot(dx, dy) || 1;
    return { x: p[0], y: p[1], t: i / (out.length - 1), nx: -dy / l, ny: dx / l };
  });
}

/** A trunk or a bough along the points, from width w0 to w1: an uneven edge, the side away from the light shaded in a hard step, bark drawn as broken streaks along it. */
function limb(a, pts, w0, w1, r) {
  const c = along(pts), p1 = r() * 6.3, p2 = r() * 6.3;
  const widths = c.map((p, i) => (w0 + (w1 - w0) * p.t) * (1 + .05 * Math.sin(i * .17 + p1) + .03 * Math.sin(i * .41 + p2)));
  const away = c[0].nx * LIGHT[0] + c[0].ny * LIGHT[1] > 0 ? -1 : 1;
  const side = (s, from, to) => c.flatMap((p, i) => [p.x + p.nx * widths[i] * s * (i % 2 ? from : to), p.y + p.ny * widths[i] * s * (i % 2 ? from : to)]);
  const edge = (k) => c.flatMap((p, i) => [p.x + p.nx * widths[i] * k, p.y + p.ny * widths[i] * k]);
  const between = (k0, k1) => { const e = edge(k1), back = []; for (let i = e.length - 2; i >= 0; i -= 2) back.push(e[i], e[i + 1]); return poly(...edge(k0), ...back); };
  const lines = [];
  for (let n = Math.round(c.length * w0 / 26); n > 0; n--) {
    const at = Math.floor(r() * c.length), run = 7 + Math.floor(r() * 18), k = (r() - .5) * .8, path = [];
    for (let i = at; i < Math.min(c.length, at + run); i += 3) path.push(`${(c[i].x + c[i].nx * widths[i] * k + (r() - .5) * .4).toFixed(2)} ${(c[i].y + c[i].ny * widths[i] * k).toFixed(2)}`);
    if (path.length > 1) lines.push('M' + path.join('L'));
  }
  const left = side(-.5, 1, 1), right = side(.5, 1, 1), back = [];
  for (let i = right.length - 2; i >= 0; i -= 2) back.push(right[i], right[i + 1]);
  a.shape(poly(...left, ...back), BARK, {
    edge: false, shade: between(away * .5, away * .08), deep: between(away * .5, away * .33), lite: between(-away * .5, -away * .4), lines,
  });
}

/** The ragged outline of a plate of leaves: a blunt mass, flatter below, heaped above in lumps, its edge in small uneven leaf tips. Random only along the edge of the placed plate. */
function plateEdge(cx, cy, rx, ry, r) {
  const pts = [], n = Math.max(20, Math.round((rx + ry) * 3.2)), p1 = r() * 6.3, p2 = r() * 6.3, blunt = (v) => Math.sign(v) * Math.pow(Math.abs(v), .62);
  for (let k = 0; k < n; k++) {
    const t = (k / n) * Math.PI * 2, below = Math.sin(t) > 0, heap = 1 + .12 * Math.sin(t * 5 + p1) + .1 * Math.sin(t * 11 + p2);
    const tip = r() < .4 ? .4 + r() * .9 : -.1 - r() * .45;
    pts.push(cx + blunt(Math.cos(t)) * (rx * (1 + (heap - 1) * .4) + tip), cy + blunt(Math.sin(t)) * ((below ? ry * .72 : ry) * heap + tip));
  }
  return pts;
}

/** Small heaps of leaves along a plate's top, each lit above and shaded under, so it reads as foliage and not as a band. */
function tufts(a, [cx, cy, rx, ry], m, r) {
  for (let n = Math.round(rx / 4); n > 0; n--) {
    const x = cx + (r() - .5) * rx * 1.75, y = cy - ry * (.05 + r() * .55), w = 2.6 + r() * 3.4, h = 1.5 + r() * 1.3;
    a.shape(poly(plateEdge(x, y, w, h, r)), m, { outer: false, edge: false, shade: poly(x - w - 2, y + h * .2, x + w + 2, y + h * .05, x + w + 2, y + h + 2, x - w - 2, y + h + 2), lite: poly(plateEdge(x + LIGHT[0] * w * .25, y - h * .5, w * .45, h * .35, r)) });
  }
}

/** A leaf, a short sharp dab of tone. */
const leaf = (a, x, y, len, ang, c) => {
  const dx = Math.cos(ang) * len, dy = Math.sin(ang) * len, w = len * .32;
  a.flat(poly(x - dx * .5, y - dy * .5, x - dy / len * w, y + dx / len * w, x + dx * .5, y + dy * .5, x + dy / len * w, y - dx / len * w), c);
};

/** A plate in its tones: a ragged hard shadow under its middle, leaves breaking that line, a few short pen marks in the shadow. */
function plate(a, [cx, cy, rx, ry], m, r) {
  const fall = cy + ry * (.05 + r() * .15), under = [];
  for (let x = cx - rx - 3; x <= cx + rx + 3; x += 2) under.push(x, fall + (r() - .5) * 2.2);
  a.shape(poly(plateEdge(cx, cy, rx, ry, r)), m, { outer: true, edge: false, shade: poly(...under, cx + rx + 3, cy + ry + 4, cx - rx - 3, cy + ry + 4) });
  for (let n = Math.round(rx * .9); n > 0; n--) {
    const x = cx + (r() - .5) * rx * 1.8;
    leaf(a, x, fall + (r() - .3) * 2.4, 1.8 + r() * 1.3, (r() - .5) * 1.6, r() < .5 ? m.base : m.shade);
  }
  for (let n = Math.round(rx / 7); n > 0; n--) {
    const x = cx + (r() - .5) * rx * 1.5, y = fall + 1 + r() * ry * .35;
    a.line(`M${x} ${y}L${x + 1.4 + r() * 2} ${y + (r() - .3) * 1.2}`, { w: 1.1, c: m.ink });
  }
}

/** The lit top of a plate: small, on the side toward the light, its leaves loose over the edge. */
function crown(a, [cx, cy, rx, ry], r) {
  const x0 = cx + LIGHT[0] * rx * .12;
  a.shape(poly(plateEdge(x0, cy, rx, ry, r)), TOP, { outer: false, edge: false, shade: poly(x0 - rx - 3, cy + ry * .25, x0 + rx + 3, cy + ry * .05, x0 + rx + 3, cy + ry + 3, x0 - rx - 3, cy + ry + 3), lite: poly(plateEdge(x0 + LIGHT[0] * rx * .2, cy - ry * .4, rx * .5, ry * .35, r)) });
  for (let n = Math.round(rx * 1.1); n > 0; n--) {
    const t = r() * Math.PI * 2;
    leaf(a, x0 + Math.cos(t) * rx * (1 + r() * .14), cy + Math.sin(t) * ry * (1 + r() * .3), 1.6 + r() * 1.2, r() * 3.1, r() < .4 ? TOP.lite : TOP.base);
  }
}

/** A strand hanging from the canopy: a thin line swaying a little, leaves along it, none at its very end. */
function strand(a, x, y, len, m, r) {
  const sway = (r() - .5) * len * .22, end = x + sway, mid = x + sway * .25, w = .85;
  a.shape(`M${x - w} ${y}Q${mid - w * .6} ${y + len * .55} ${end} ${y + len}Q${mid + w * .6} ${y + len * .55} ${x + w} ${y}Z`, m, { outer: false, edge: false });
  for (let d = 2 + r() * 2; d < len - 1.5; d += 1.9 + r() * 2.4) {
    const k = d / len, px = x + (end - x) * k * k, s = r() < .5 ? -1 : 1;
    leaf(a, px + s * 1, y + d, 1.7 + r() * .9 - k * .5, s * (.9 + r() * .6) + Math.PI / 2, m.base);
  }
}

/** A placed hank of strands: rooted up inside the canopy, with its loose companions either side. */
function hank(a, [x, y, len], m, r) {
  for (const [dx, keep] of [[-4.6 - r() * 1.4, .4 + r() * .4], [4.6 + r() * 1.4, .4 + r() * .4], [-2.2 - r(), .6 + r() * .4], [2.2 + r(), .55 + r() * .4], [0, 1]]) strand(a, x + dx, y - 10, (len + 10) * keep, m, r);
}

const ACACIAS = [
  { w: 185, h: 211, seed: 0xac0,
    trunk: [[94, 212], [93, 180], [95, 150], [95, 118]], width: [15, 6],
    boughs: [
      [[[75, 86], [84, 62], [88, 38]], 4, 1.5], [[[115, 72], [106, 54], [102, 36]], 3.5, 1.5],
      [[[95, 128], [84, 108], [75, 88], [68, 68], [58, 50], [46, 34]], 8, 2.5], [[[96, 128], [107, 105], [113, 85], [118, 65], [125, 48], [136, 32]], 7, 2.5],
    ],
    stub: [[94, 166], [84, 158], [80, 152]],
    back: [[30, 22, 28, 13], [72, 20, 34, 14], [118, 18, 36, 14], [158, 24, 22, 12], [95, 34, 30, 9], [45, 33, 18, 7], [140, 34, 20, 8]],
    mid: [[18, 20, 16, 7], [46, 17, 22, 8], [82, 13, 24, 8], [116, 12, 26, 8], [150, 18, 22, 8], [168, 24, 10, 5], [64, 26, 18, 7], [102, 26, 20, 7], [132, 27, 18, 6], [30, 29, 14, 5]],
    top: [[44, 12, 13, 4], [84, 8, 14, 4], [118, 7, 16, 4], [150, 13, 12, 4], [20, 16, 8, 3], [66, 20, 9, 3], [104, 20, 10, 3]],
    strands: [[12, 30, 14], [22, 33, 18], [34, 34, 12], [42, 36, 22], [54, 38, 10], [63, 38, 20], [76, 42, 24], [86, 44, 14], [96, 44, 22], [108, 42, 16], [120, 40, 10], [128, 38, 22], [142, 36, 20], [152, 34, 14], [162, 34, 22], [172, 28, 12]] },
  { w: 177, h: 166, seed: 0xac1,
    trunk: [[88, 167], [87, 135], [85, 110], [86, 86]], width: [15, 6],
    boughs: [
      [[[66, 72], [70, 52], [72, 34]], 4, 1.5], [[[97, 68], [112, 50], [122, 32]], 3.5, 1.5],
      [[[85, 94], [72, 82], [60, 70], [45, 58], [35, 50]], 7.5, 2.5], [[[87, 94], [95, 75], [99, 60], [103, 36]], 6.5, 2.5],
    ],
    stub: [[87, 128], [97, 121], [101, 114]],
    back: [[30, 22, 27, 12], [72, 20, 34, 13], [115, 20, 32, 12], [135, 26, 13, 8], [50, 34, 30, 9], [28, 58, 22, 14], [26, 74, 17, 9]],
    mid: [[18, 22, 16, 7], [46, 17, 22, 8], [82, 16, 24, 8], [116, 15, 25, 7], [138, 22, 10, 5], [30, 31, 18, 6], [66, 29, 18, 6], [100, 28, 18, 6], [30, 52, 20, 7], [24, 66, 16, 7]],
    top: [[44, 12, 12, 4], [82, 11, 14, 4], [116, 10, 14, 3.5], [18, 17, 8, 3], [30, 47, 11, 3.5], [22, 61, 8, 3]],
    strands: [[8, 28, 16], [18, 34, 22], [34, 38, 10], [46, 38, 8], [62, 36, 14], [78, 36, 10], [92, 36, 8], [106, 34, 12], [120, 34, 14], [130, 32, 18], [140, 28, 18], [14, 68, 18], [24, 76, 14], [36, 68, 12], [44, 58, 10]] },
  { w: 165, h: 184, seed: 0xac2,
    trunk: [[82, 185], [82, 150], [83, 114]], width: [15, 6],
    boughs: [
      [[[76, 112], [72, 90], [70, 64]], 4.5, 1.8], [[[96, 84], [86, 66], [86, 50]], 3.5, 1.5],
      [[[82, 122], [70, 108], [60, 92], [48, 74], [38, 56]], 7.5, 2.5], [[[84, 122], [92, 100], [96, 84], [106, 62], [118, 46]], 6.5, 2.5],
    ],
    stub: [[82, 146], [92, 140], [94, 135]],
    back: [[75, 26, 50, 20], [120, 34, 30, 16], [40, 48, 36, 22], [85, 58, 42, 20], [130, 44, 20, 8], [28, 74, 26, 18], [70, 80, 26, 10]],
    mid: [[72, 14, 24, 8], [105, 20, 26, 8], [38, 25, 22, 8], [135, 32, 16, 6], [18, 38, 14, 6], [60, 36, 26, 9], [100, 40, 26, 8], [30, 54, 22, 8], [72, 58, 24, 8], [110, 56, 14, 5], [22, 74, 18, 7], [55, 76, 20, 7], [82, 76, 10, 5]],
    top: [[72, 9, 13, 3.5], [104, 15, 13, 3.5], [38, 20, 11, 3.5], [135, 27, 8, 3], [60, 31, 12, 3.5], [100, 35, 11, 3], [30, 49, 10, 3], [72, 53, 11, 3], [20, 70, 9, 3], [55, 71, 9, 3]],
    strands: [[6, 74, 16], [14, 82, 18], [26, 88, 12], [36, 84, 10], [48, 84, 16], [62, 84, 10], [78, 82, 12], [88, 70, 14], [98, 62, 14], [108, 60, 12], [118, 54, 10], [128, 48, 12], [138, 42, 14], [146, 36, 10]] },
];

const POD = '#a8e0cf';

function acacia(t, i) {
  const pods = LOGICAL[`acacia_${i}_glow`].pts;
  return { w: t.w, h: t.h, draw(a) {
    const r = rng(t.seed);
    for (const p of t.back) plate(a, p, BACK, r);
    limb(a, t.stub, 3.4, 1.6, r);
    limb(a, t.trunk, t.width[0], t.width[1], r);
    for (const [pts, w0, w1] of t.boughs) limb(a, pts, w0, w1, r);
    t.strands.forEach((s, k) => { if (k % 3 === 0) hank(a, s, BACK, r); });
    for (const p of t.mid) { plate(a, p, MID, r); tufts(a, p, MID, r); }
    t.strands.forEach((s, k) => { if (k % 3) hank(a, s, MID, r); });
    for (const p of t.top) { crown(a, p, r); tufts(a, p, TOP, r); }
    for (let k = 0; k < pods.length; k += 2) {
      const x = pods[k] + .5, y = pods[k + 1] + .5, held = [...t.back, ...t.mid].some(([cx, cy, rx, ry]) => ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 < 1);
      if (!held) strand(a, x, y - 14, 14, MID, r);
      a.flat(ell(x, y, .55, .55), POD);
    }
  } };
}

/** The pods' own light, added over the tree: where the logical glow picture has them, as the game lights from there. */
function podsGlow(t, i) {
  const pods = LOGICAL[`acacia_${i}_glow`].pts, c = canvas(t.w * 4, t.h * 4), x = c.getContext('2d');
  x.filter = 'blur(3px)';
  x.fillStyle = 'rgba(150,235,215,.55)';
  for (let k = 0; k < pods.length; k += 2) { x.beginPath(); x.arc(pods[k] * 4 + 2, pods[k + 1] * 4 + 2, 3.2, 0, 7); x.fill(); }
  x.filter = 'none';
  x.fillStyle = '#d8fff2';
  for (let k = 0; k < pods.length; k += 2) { x.beginPath(); x.arc(pods[k] * 4 + 2, pods[k + 1] * 4 + 2, 1.5, 0, 7); x.fill(); }
  return c;
}

Subjects.flora = function () {
  ACACIAS.forEach((t, i) => {
    out(`planet/acacia_${i}`, bake(acacia(t, i)));
    out(`planet/acacia_${i}_glow`, podsGlow(t, i));
  });
};
})();

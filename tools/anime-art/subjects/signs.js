// The power signs over machines and before power numbers, as picked (the ink-signs ADR): glyphs a screen lights in hard steps,
// drawn straight on the canvas at 4 times their logical pixels, no finish (as bulb.js). The bolt short of power, none, unplugged, broken.
(function () {
'use strict';
const { canvas } = Anime;

/** The logical sizes the game lays them out by: the pixel signs' own. */
const SIZES = { sign_bolt: [7, 9], sign_boltNone: [7, 9], sign_unplugged: [7, 7], sign_broken: [13, 13] };
/** The bolt in the 7 by 9 picture: a heavy zigzag, top right to a point at the bottom left. */
const BOLT = [3.6, .5, 6.5, .5, 4.7, 3.5, 6.4, 3.5, 1.0, 8.6, 2.5, 5.3, .6, 5.3];
/** BOLT scaled by k round its middle, moved to (cx, cy). */
const fit = (pts, cx, cy, k) => pts.map((v, i) => (i % 2 ? (v - 4.55) * k + cy : (v - 3.5) * k + cx));

/** Signed distance to a polygon, negative inside (Inigo Quilez). */
function sdPoly(p) {
  const n = p.length / 2;
  return (x, y) => {
    let d = (x - p[0]) ** 2 + (y - p[1]) ** 2, s = 1;
    for (let i = 0, j = n - 1; i < n; j = i, i++) {
      const vx = p[2 * i], vy = p[2 * i + 1], ex = p[2 * j] - vx, ey = p[2 * j + 1] - vy, wx = x - vx, wy = y - vy;
      const t = Math.max(0, Math.min(1, (wx * ex + wy * ey) / (ex * ex + ey * ey))), bx = wx - ex * t, by = wy - ey * t;
      d = Math.min(d, bx * bx + by * by);
      const c1 = y >= vy, c2 = y < p[2 * j + 1], c3 = ex * wy > ey * wx;
      if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
    }
    return s * Math.sqrt(d);
  };
}
const sdCapsule = (ax, ay, bx, by, r) => (x, y) => {
  const px = x - ax, py = y - ay, ex = bx - ax, ey = by - ay, t = Math.max(0, Math.min(1, (px * ex + py * ey) / (ex * ex + ey * ey || 1)));
  return Math.hypot(px - ex * t, py - ey * t) - r;
};
const sdCircle = (cx, cy, r) => (x, y) => Math.hypot(x - cx, y - cy) - r;
/** A rounded box, centre (cx, cy), half sizes hx, hy, turned by ang. */
const sdBox = (cx, cy, hx, hy, r, ang = 0) => (x, y) => {
  const c = Math.cos(ang), s = Math.sin(ang), dx = x - cx, dy = y - cy, lx = Math.abs(dx * c + dy * s) - hx + r, ly = Math.abs(-dx * s + dy * c) - hy + r;
  return Math.hypot(Math.max(lx, 0), Math.max(ly, 0)) + Math.min(Math.max(lx, ly), 0) - r;
};
const union = (...f) => (x, y) => { let d = Infinity; for (const g of f) d = Math.min(d, g(x, y)); return d; };
const minus = (f, g) => (x, y) => Math.max(f(x, y), -g(x, y));
const ring = (f, t) => (x, y) => Math.abs(f(x, y)) - t;
const rgb = (h) => [parseInt(h.substr(1, 2), 16), parseInt(h.substr(3, 2), 16), parseInt(h.substr(5, 2), 16)];

/**
 * A glyph as a screen lights it: per layer a dim halo one step out, a dark rim, the lit body and a pale core, each a hard step; every
 * fourth bitmap row a scan line. Distances in the bitmap's pixels (4 a logical pixel); 4 by 4 samples a pixel for a clean edge only.
 */
function glyph(name, layers) {
  const [w, h] = SIZES[name], W = w * 4, H = h * 4, c = canvas(W, H), x = c.getContext('2d'), im = x.createImageData(W, H), D = im.data, SS = 4;
  const L = layers.map((l) => ({ ...l, body: rgb(l.body), core: rgb(l.core), rim: rgb(l.rim), halo: rgb(l.halo) }));
  for (let py = 0; py < H; py++) for (let px = 0; px < W; px++) {
    let R = 0, G = 0, B = 0, A = 0;
    for (let sy = 0; sy < SS; sy++) for (let sx = 0; sx < SS; sx++) {
      const lx = (px + (sx + .5) / SS) / 4, ly = (py + (sy + .5) / SS) / 4;
      let r = 0, g = 0, b = 0, a = 0;
      for (const l of L) {
        const d = l.sd(lx, ly) * 4, rimW = l.rimW ?? 2, haloW = l.haloW ?? 2.5;
        let col = null, al = 1;
        if (d < -(l.coreAt ?? 2.2)) col = l.core;
        else if (d < 0) col = l.body;
        else if (d < rimW) { col = l.rim; al = .94; }
        else if (d < rimW + haloW && l.haloA) { col = l.halo; al = l.haloA; }
        if (!col) continue;
        const lit = col === l.body || col === l.core, k = lit && py % 4 === 3 ? .8 : 1;
        r = r * (1 - al) + col[0] * k * al; g = g * (1 - al) + col[1] * k * al; b = b * (1 - al) + col[2] * k * al; a = a * (1 - al) + al;
      }
      R += r; G += g; B += b; A += a;
    }
    const q = (py * W + px) * 4;
    if (A > 0) { D[q] = R / A; D[q + 1] = G / A; D[q + 2] = B / A; D[q + 3] = 255 * A / (SS * SS); }
  }
  x.putImageData(im, 0, 0);
  return c;
}

const bolt = (name, body, core, rim, halo) => glyph(name, [{ sd: sdPoly(fit(BOLT, 3.5, 4.55, .72)), body, core, rim, halo, haloA: .42 }]);
const unplugged = () => glyph('sign_unplugged', [{
  sd: union(sdCapsule(2.75, 1.65, 2.75, 2.35, .34), sdCapsule(4.25, 1.65, 4.25, 2.35, .34), sdBox(3.5, 3.2, 1.45, .8, .35), sdCapsule(3.5, 4.45, 3.5, 4.7, .3), sdCapsule(3.5, 5.4, 3.5, 5.55, .3)),
  body: '#8ea6bc', core: '#dbe8f4', rim: '#0a1016', halo: '#3e5a74', haloA: .4, coreAt: 1.8,
}]);
const broken = () => {
  const dir = -Math.PI / 4, head = minus(sdCircle(8.2, 4.8, 1.65), sdBox(8.2 + Math.cos(dir) * 1.25, 4.8 + Math.sin(dir) * 1.25, 1.05, .52, .05, dir));
  const end = minus(sdCircle(4.0, 9.0, .95), sdCircle(4.0, 9.0, .38));
  return glyph('sign_broken', [
    { sd: ring(sdBox(6.5, 6.5, 4.4, 4.4, 1.1), .55), body: '#ff7c2e', core: '#ffd2ac', rim: '#160a04', halo: '#8a3a12', haloA: .42, coreAt: 1.4 },
    { sd: union(sdCapsule(4.4, 8.6, 7.3, 5.7, .62), head, end), body: '#e6edf3', core: '#ffffff', rim: '#0a0c10', halo: '#000000', haloA: 0, coreAt: 2 },
  ]);
};

Subjects.signs = function () {
  out('sign_bolt', bolt('sign_bolt', '#f5a623', '#fff0c0', '#1c1004', '#8a5410'));
  out('sign_boltNone', bolt('sign_boltNone', '#ec3f36', '#ffd0c4', '#1c0606', '#7c1a18'));
  out('sign_unplugged', unplugged());
  out('sign_broken', broken());
};
})();

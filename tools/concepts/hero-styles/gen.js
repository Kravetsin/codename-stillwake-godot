'use strict';
// Draws one head shape and its hair at any density from outlines in units of the helmet's pixels (13x12, facing +x,
// the neck at (6.5, 12)): each pixel of the result is 1/density of a unit. Shaded in style E: lit from the upper
// front, the outline only where a surface turns from the light, a cool rim on the upper back.
//   gen(density) -> { head, hairs: [{ rows, at }], ... } as pixel rows in the keys of tools/hero-art/parts.js.

const inPoly = (pts, x, y) => {
  let c = false;
  for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
    const [xi, yi] = pts[i], [xj, yj] = pts[j];
    if ((yi > y) !== (yj > y) && x < ((xj - xi) * (y - yi)) / (yj - yi) + xi) c = !c;
  }
  return c;
};
const inEllipse = ([cx, cy, rx, ry], x, y) => ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1;

// The profile, clockwise from the back of the neck: skull, forehead, brow, nose, lips, chin, jaw, the neck's front.
const HEAD = [
  [4.0, 12.2], [3.4, 10.8], [1.6, 9.6], [0.5, 7.6], [0.3, 5.0], [0.9, 2.8], [2.4, 1.1], [4.6, 0.15], [7.4, 0.15],
  [9.6, 0.9], [11.0, 2.3], [11.6, 3.9], [11.75, 4.9], [11.45, 5.45], [12.05, 6.15], [12.75, 7.0], [12.6, 7.35],
  [11.8, 7.55], [11.85, 8.1], [11.55, 8.55], [11.8, 8.95], [11.7, 9.8], [11.25, 10.5], [10.2, 10.95], [8.6, 11.0],
  [7.7, 11.45], [7.5, 12.2],
];
const EAR = [4.4, 6.6, 1.05, 1.45];

// Hair: the parts as outlines (and curls as circles), in the same units.
const HAIRS = {
  // short, combed back and up, the ear clear
  short: { polys: [[[1.4, 7.6], [0.2, 5.0], [0.6, 2.4], [2.2, 0.4], [4.8, -0.9], [8.2, -0.8], [10.6, 0.4], [11.9, 2.2],
    [11.4, 2.9], [10.2, 2.0], [8.4, 2.0], [6.4, 2.7], [5.2, 4.1], [4.0, 5.0], [3.2, 6.6], [2.6, 8.4]]], strands: 0.55 },
  // long, from a side parting down the back past the shoulders
  long: { polys: [[[0.8, 15.6], [-0.6, 12.4], [-0.9, 8.6], [-0.6, 4.6], [0.4, 1.8], [2.4, -0.3], [5.2, -1.1], [8.6, -0.7],
    [11.0, 0.8], [12.1, 3.0], [11.6, 3.6], [10.0, 2.4], [8.0, 2.3], [6.4, 3.4], [5.4, 4.6], [4.4, 5.0], [3.4, 6.2],
    [3.0, 9.0], [3.4, 12.6], [2.8, 15.6]]], strands: 0.8 },
  // a crop of tight curls
  curls: { circles: [[2.0, 6.6, 1.5], [1.0, 4.4, 1.6], [1.8, 2.2, 1.6], [3.6, 0.6, 1.6], [6.0, 0.0, 1.6], [8.4, 0.4, 1.6],
    [10.3, 1.6, 1.5], [3.2, 3.0, 1.4], [5.4, 2.0, 1.4], [7.8, 2.0, 1.2], [3.0, 5.0, 1.0]], curls: true },
  // pulled back into a bun at the crown's back
  bun: { polys: [[[1.6, 8.6], [0.1, 5.4], [0.5, 2.6], [2.4, 0.5], [5.0, -0.5], [8.2, -0.4], [10.6, 0.9], [11.6, 2.6],
    [10.4, 2.3], [8.4, 2.1], [6.4, 2.5], [5.0, 3.6], [3.9, 4.9], [3.2, 6.6], [2.8, 8.6]]],
    circles: [[0.0, 2.4, 2.0]], strands: 0.55 },
};

const LIGHT = (() => { const l = [0.55, -0.85], n = Math.hypot(...l); return [l[0] / n, l[1] / n]; })();

/** Pixel rows of a mask drawn at density d over the box [x0, y0, x1, y1] in units, shaded with ramp [light, own, shade, line]. */
function shade(mask, d, [x0, y0, x1, y1], [Lk, Sk, Dk, Ok], { rim = 'R', strands = 0, curls = false, rimFrom = 2.5, rimTo = 9.5, over = null, line = 1 } = {}) {
  const w = Math.round((x1 - x0) * d), h = Math.round((y1 - y0) * d);
  const at = (i, j) => (i < 0 || j < 0 || i >= w || j >= h ? false : mask(x0 + (i + 0.5) / d, y0 + (j + 0.5) / d));
  const R = Math.max(2, Math.round(1.5 * d));
  const rows = [];
  for (let j = 0; j < h; j++) {
    let row = '';
    for (let i = 0; i < w; i++) {
      if (!at(i, j)) { row += '.'; continue; }
      // the surface's outward normal: the sum of offsets toward the outside nearby
      let nx = 0, ny = 0, edge = false;
      for (let b = -R; b <= R; b++) for (let a = -R; a <= R; a++) {
        if (a * a + b * b > R * R || at(i + a, j + b)) continue;
        nx += a; ny += b;
      }
      // an edge over the head (hair on the brow) is shade, not line
      // line: the outline's thickness in pixels; 2 at half pixels keeps it as thick as the body's
      for (let t = 1; t <= line; t++) for (const [a, b] of [[t, 0], [-t, 0], [0, t], [0, -t]]) if (!at(i + a, j + b) && !(over && over(x0 + (i + a + 0.5) / d, y0 + (j + b + 0.5) / d))) edge = true;
      const m = Math.hypot(nx, ny), dot = m ? (nx * LIGHT[0] + ny * LIGHT[1]) / m : 0, near = m / (R * R);
      const y = y0 + (j + 0.5) / d;
      let k = Sk;
      if (edge && dot < -0.2) k = rim && nx / m < -0.6 && y > rimFrom && y < rimTo ? rim : Ok;
      else if (near > 0.15 && dot < -0.1) k = Dk;
      else if (near > 0.12 && dot > 0.62) k = Lk;
      if (strands && k === Sk) {
        // strands: darker lines combed along the hair, every few pixels, more of them where it turns away
        const s = Math.sin((x0 + (i + 0.5) / d) * 2.2 + (y * 1.3)) + Math.sin(y * 3.1 - (x0 + (i + 0.5) / d));
        if (s > 2 - strands) k = Dk;
      }
      if (curls && (k === Sk || k === Lk)) {
        const u = x0 + (i + 0.5) / d;
        if (Math.sin(u * 3.3) * Math.sin(y * 3.3) > 0.55) k = Lk;
        else if (Math.sin(u * 3.3 + 1.6) * Math.sin(y * 3.3 + 1.6) > 0.6) k = Dk;
      }
      row += k;
    }
    rows.push(row);
  }
  return rows;
}

function gen(d, line = 1) {
  const headMask = (x, y) => y < 12 && (inPoly(HEAD, x, y));
  const head = shade(headMask, d, [0, 0, 13, 12], ['L', 'S', 'D', 'O'], { rim: null, line });
  // the ear, a shaded rim over the head
  const ear = shade((x, y) => inEllipse(EAR, x, y), d, [0, 0, 13, 12], ['S', 'D', 'O', 'O'], { rim: null });
  const headRows = head.map((r, j) => [...r].map((k, i) => (ear[j][i] !== '.' && k !== '.' ? (ear[j][i] === 'O' ? 'D' : ear[j][i] === 'D' ? 'D' : 'S') : k)).join(''));
  const hairs = {};
  for (const [name, h] of Object.entries(HAIRS)) {
    const box = [-2, -2, 14, name === 'long' ? 17 : 12];
    const mask = (x, y) => (h.polys || []).some(p => inPoly(p, x, y)) || (h.circles || []).some(([cx, cy, r]) => (x - cx) ** 2 + (y - cy) ** 2 <= r * r);
    const rows = shade(mask, d, box, ['1', '2', '3', '4'], { strands: h.strands || 0, curls: h.curls, rimFrom: -3, rimTo: 18, over: headMask, line });
    hairs[name] = { rows, at: [box[0] * d, box[1] * d] };
  }
  return { head: headRows, hairs };
}

module.exports = { gen, HEAD, EAR };
if (require.main === module) {
  const g = gen(+process.argv[2] || 1);
  console.log(g.head.join('\n'));
  for (const [n, h] of Object.entries(g.hairs)) console.log('\n' + n + ' at ' + h.at + '\n' + h.rows.join('\n'));
}

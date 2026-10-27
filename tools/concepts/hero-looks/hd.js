'use strict';
// A trial: the bare head drawn twice as fine (each pixel half a pixel of the world). The head and its faces are 26×24 in
// the helmet's frame, turning at the neck, (13, 24), with no neck: the collar hides where it meets the body. Hair is
// bigger than the helmet, 38×40 turning at (19, 30), and the helmet hides it. Keys as tools/hero-art/parts.js: skin
// L S D O, hair 1 2 3 4, eyes e w, mouth m.
const head = [
  '..........................',
  '..........................',
  '..........................',
  '..........................',
  '.........OOOOOOOO.........',
  '.......OOLLLLLLLLOO.......',
  '.....OOLLLLLLLLLLLLOO.....',
  '....OLLLLLLLLLLLLLLSSO....',
  '...OLLLLLLLLLLLLLLSSSSO...',
  '...OLLLLLLLLLLLLSSSSSSO...',
  '...OLLLLLLLLLLSSSSSSSSSO..',
  '...OLLLLLLLLSSSSSSSSSSSO..',
  '...OLLLLLSSSSSSSSSSSSSSO..',
  '...OLLLSSSSSSSSSSSSSSSSO..',
  '...OLSSSSSSDSSSSSSSSSSSSO.',
  '...OSSSSSSDDSSSSSSSSSSSSO.',
  '...OSSSSSSSSSSSSSSSSSSSSO.',
  '...ODSSSSSSSSSSSSSSSSSSO..',
  '....ODSSSSSSSSSSSSSSSSSO..',
  '....ODSSSSSSSSSSSSSSSSO...',
  '.....ODSSSSSSSSSSSSSSO....',
  '......ODDSSSSSSSSSSSO.....',
  '.......OODDSSSSSSSOO......',
  '.........OOOOOOOOO........',
];

const blank = () => Array.from({ length: 24 }, () => '.'.repeat(26));
const paint = (base, marks) => { const rows = base.map(r => [...r]); for (const [x, y, k] of marks) rows[y][x] = k; return rows.map(r => r.join('')); };
const spots = (s, y0) => s.flatMap((row, j) => [...row].map((k, i) => [i, y0 + j, k])).filter(([, , k]) => k !== '.');

// Three-quarter faces: the near eye big, a sliver of the far one at the face's edge, a small nose and mouth.
const faces = [
  // calm
  paint(blank(), spots([
    '................333...3...',
    '................eee.......',
    '................wee...e...',
    '................wye...e...',
    '.....................L....',
    '....................D.....',
    '..........................',
    '...................DD.....',
  ], 11)),
  // stern: brows drawn down, eyes narrowed, a level mouth, stubble along the jaw
  paint(blank(), spots([
    '...............3333..33...',
    '................3eee......',
    '................wee...e...',
    '..........................',
    '.....................L....',
    '....................D.....',
    '..........................',
    '..................DDD.....',
    '..........3.3.3.3.3.......',
    '...........3.3.3.3........',
  ], 11)),
  // cheerful: brows up, eyes wide and bright, freckles, an open smile
  paint(blank(), spots([
    '................333..33...',
    '..........................',
    '................eee...e...',
    '................wye...e...',
    '................wee.......',
    '..............D.D...LD....',
    '...................D......',
    '..................DmmD....',
    '...................mm.....',
  ], 10)),
];

// ---- hair, built from volumes and shaded like light from the upper left
const HW = 38, HH = 40, AT = [6, 6]; // the head's frame inside the hair's picture
const LIGHT = [-0.6, -0.8];
// e: an ellipse [x, y, rx, ry]; c: a capsule [x0, y0, x1, y1, r0, r1]; a cut takes pixels away; a line darkens hair under it.
const e = (x, y, rx, ry) => ({ e: [x, y, rx, ry] });
const c = (x0, y0, x1, y1, r0, r1 = r0) => ({ c: [x0, y0, x1, y1, r0, r1] });
function inside(p, x, y) {
  if (p.e) { const [cx, cy, rx, ry] = p.e; const dx = (x - cx) / rx, dy = (y - cy) / ry; return dx * dx + dy * dy <= 1 ? [dx, dy] : null; }
  const [x0, y0, x1, y1, r0, r1] = p.c, vx = x1 - x0, vy = y1 - y0;
  const t = Math.max(0, Math.min(1, ((x - x0) * vx + (y - y0) * vy) / (vx * vx + vy * vy)));
  const px = x - (x0 + vx * t), py = y - (y0 + vy * t), r = r0 + (r1 - r0) * t, d = Math.hypot(px, py);
  return d <= r ? [px / r, py / r] : null;
}
const FACE = e(27, 24.5, 6.8, 8.6), EAR = e(16.8, 21.2, 1.7, 1.9);
/** Volumes shaded into key rows (1 2 3 4) on a w×h grid; at: where the head lies in it, its skin edged softer, or null. */
function shade({ parts, cuts = [], lines = [], w = HW, h = HH, at = AT }) {
  const HW = w, HH = h;
  const own = Array.from({ length: HH }, () => new Array(HW).fill(-1)), normal = [];
  for (let y = 0; y < HH; y++) for (let x = 0; x < HW; x++) {
    const px = x + 0.5, py = y + 0.5;
    if (cuts.some(k => inside(k, px, py))) continue;
    parts.forEach((p, i) => { const n = inside(p, px, py); if (n) { own[y][x] = i; normal[y * HW + x] = n; } });
  }
  const headAt = (x, y) => { if (!at) return false; const r = head[y - at[1]]; return r ? r[x - at[0]] && r[x - at[0]] !== '.' : false; };
  const rows = [];
  for (let y = 0; y < HH; y++) {
    let row = '';
    for (let x = 0; x < HW; x++) {
      const i = own[y][x];
      if (i < 0) { row += '.'; continue; }
      const near = [[1, 0], [-1, 0], [0, 1], [0, -1]].map(([dx, dy]) => (own[y + dy] && own[y + dy][x + dx] != null ? own[y + dy][x + dx] : -1));
      const edge = near.some(j => j < 0), under = near.some(j => j > i);
      const [nx, ny] = normal[y * HW + x], l = (nx * LIGHT[0] + ny * LIGHT[1]) / (Math.hypot(nx, ny) || 1) * Math.min(1, Math.hypot(nx, ny) * 1.6);
      let k = l > 0.38 ? '1' : l > -0.3 ? '2' : '3';
      if (under) k = '3';
      if (lines.some(ln => inside(ln, x + 0.5, y + 0.5))) k = k === '1' ? '2' : '3';
      if (edge) {
        const out = [[1, 0], [-1, 0], [0, 1], [0, -1]].filter(([dx, dy]) => !(own[y + dy] && own[y + dy][x + dx] >= 0));
        k = out.every(([dx, dy]) => headAt(x + dx, y + dy)) ? '3' : '4';
      }
      row += k;
    }
    rows.push(row);
  }
  return rows;
}

const hair = shade;

const ring = (cx, cy, rx, ry, n, r, from = 0, to = 2 * Math.PI) => Array.from({ length: n }, (_, i) => {
  const a = from + ((to - from) * i) / (n - 1 || 1);
  return e(cx + Math.cos(a) * rx, cy + Math.sin(a) * ry, r, r * 0.9);
});

const hairs = [
  hair({ // tousled, tufts standing up
    parts: [e(18.5, 16.5, 11.6, 9.4), e(10.5, 22, 3.2, 4.2), e(12, 9.5, 4, 3.4), e(18, 7.6, 4.6, 3.8), e(24.5, 9, 4.2, 3.4), e(29, 12.5, 3.6, 3), e(30.5, 15.8, 2.6, 2)],
    cuts: [FACE, EAR],
    lines: [c(15, 11, 21, 16, 0.5), c(22, 11, 25, 15, 0.5), c(11, 14, 14, 19, 0.5)],
  }),
  hair({ // long and straight, a fringe across the brow
    parts: [c(12, 19, 10, 37, 6.4, 5), c(17, 22, 16, 35, 4.2, 3.5), e(18.5, 16, 12.4, 10.6), e(27.5, 15.2, 4.6, 2.8)],
    cuts: [e(27.5, 25.5, 6.4, 8)],
    lines: [c(9, 22, 8, 36, 0.5), c(13, 23, 13, 35, 0.5), c(17, 26, 17, 34, 0.5), c(20, 11, 26, 14, 0.5)],
  }),
  hair({ // curls, a full round cloud
    parts: [e(16.5, 14.5, 12.5, 11.5), ...ring(16.5, 14.5, 13, 12, 15, 3.6, Math.PI * 0.62, Math.PI * 2.15), e(12, 11, 3.4, 3.2), e(19, 9, 3.4, 3.2), e(23.5, 12.5, 3.2, 3), e(14, 18, 3.2, 3), e(9, 16, 3.2, 3)],
    cuts: [e(28.2, 25.8, 7.6, 9.2)],
  }),
  hair({ // a high ponytail, tied at the crown, swinging back
    parts: [e(18.5, 16.8, 11.4, 9.2), c(9, 11, 4, 17, 3.6, 3), c(4, 17, 3.2, 27, 3, 2.4), c(3.2, 27, 5, 33, 2.4, 1.2), e(27.5, 13.2, 4, 2.4)],
    cuts: [FACE, EAR],
    lines: [c(14, 10, 22, 13, 0.5), c(5, 16, 3.5, 28, 0.5)],
  }),
  hair({ // a bun on top, loose strands at the temple
    parts: [e(18.5, 16.8, 11.4, 9.2), e(15, 6.2, 5.2, 4.6), c(28.5, 14, 29.5, 21, 1.2, 0.8), e(27.5, 13.4, 4, 2.4)],
    cuts: [FACE],
    lines: [c(12, 5, 18, 8, 0.5), c(15, 12, 23, 14, 0.5)],
  }),
];

// What stays of the ponytail and the long hair when chains draw what hangs (the live demo).
const crowns = [
  shade({ parts: [e(18.5, 16.8, 11.4, 9.2), e(9.5, 11.5, 2.8, 2.6), e(27.5, 13.2, 4, 2.4)], cuts: [FACE, EAR], lines: [c(14, 10, 22, 13, 0.5)] }),
  shade({ parts: [e(13, 22, 6.5, 6.5), e(18.5, 16, 12.4, 10.6), e(27.5, 15.2, 4.6, 2.8)], cuts: [e(27.5, 25.5, 6.4, 8)], lines: [c(20, 11, 26, 14, 0.5)] }),
];

/** The pictures as images in the drawn ramps, each with the joint it turns at: hd_head, hd_face_0-2, hd_hair_0-4. */
function pictures(A) {
  const keys = { ...A.FIXED };
  'LSDO'.split('').forEach((k, i) => (keys[k] = A.SKINS[A.SKIN_DRAWN][i]));
  '1234'.split('').forEach((k, i) => (keys[k] = A.HAIRS[A.HAIR_DRAWN][i]));
  const image = (rows, joint) => {
    const w = rows[0].length, h = rows.length;
    const img = { w, h, joint, data: new Uint8ClampedArray(w * h * 4) };
    rows.forEach((row, y) => [...row].forEach((k, x) => {
      if (k === '.') return;
      const [hex, a] = keys[k].split('/'), n = parseInt(hex.slice(1), 16);
      img.data.set([n >> 16, (n >> 8) & 255, n & 255, a ? +a : 255], (y * w + x) * 4);
    }));
    return img;
  };
  const out = { hd_head: image(head, [13, 24]) };
  faces.forEach((f, i) => (out['hd_face_' + i] = image(f, [13, 24])));
  hairs.forEach((h, i) => (out['hd_hair_' + i] = image(h, [19, 30])));
  crowns.forEach((h, i) => (out['hd_crown_' + i] = image(h, [19, 30])));
  return out;
}

if (typeof module !== 'undefined') module.exports = { head, faces, hairs, crowns, pictures, shade, e, c };

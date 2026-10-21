// Concepts for the crafted parts and new materials: three variants each, drawn on the resource-icon canvas.
const { Icon, P, OUT, HI } = require('./art.js');

// palettes beyond art.js: light, mid, shade, deep
Object.assign(P, {
  sil:   ['#aebbd6', '#7884a3', '#525c78', '#363d53'],
  glass: ['#effdff', '#c4eff6', '#94d6e3', '#629fb2'],
  cell:  ['#4f7cc9', '#2c4f97', '#1f3a73', '#14264d'],
  rock:  ['#aaa69c', '#837f76', '#5f5c55', '#423f3a'],
  rockC: ['#9aa1a8', '#757c84', '#545a62', '#3a3f46'],
  case:  ['#6e7684', '#4e5562', '#383e49', '#262a32'],
});
const SHEEN = '#dde6f7', GLINT = '#fbfdff';
const IRON = { hi: '#eef2f7', top: '#c9d2de', front: '#8d99aa', frontD: '#66727f', left: '#aab5c4', right: '#56606e' };
const SIL = { hi: '#c9d4ea', top: '#8f9cbb', front: '#5d6884', frontD: '#454e66', left: '#7a87a6', right: '#3b4359' };
const GLS = { hi: '#ffffff', top: '#d6f5fa', front: '#a6e0ea', frontD: '#7cc0cf', left: '#c4eff6', right: '#86c6d4' };

const each = (I, fn) => { for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) if (I.mine(x, y)) fn(x, y); };
const rivet = (I, x, y) => { I.paint(x, y, HI, true); I.paint(x + 1, y + 1, P.dark[3], true); };
const LEDON = ['#d9ffd0', '#7fe36f', '#3fae4a', '#1f6a2c'], LEDOFF = ['#3a4048', '#2a2f36', '#20242a'];

// oblique box, as in the resource icons: faces turned away are skipped; returns pixel -> face
function box(I, o, pal) {
  I.obj(); const { L, W, H, i = 0, ax = 1, ay = 0, bx, by, hz = 1, ox, oy } = o;
  const p = (X, Y, Z) => [ox + X * ax + Y * bx, oy + X * ay + Y * by - Z * hz];
  const b = [p(0, 0, 0), p(L, 0, 0), p(L, W, 0), p(0, W, 0)], t = [p(i, i, H), p(L - i, i, H), p(L - i, W - i, H), p(i, W - i, H)];
  const faces = [['front', [b[0], b[1], t[1], t[0]]], ['right', [b[1], b[2], t[2], t[1]]], ['back', [b[2], b[3], t[3], t[2]]], ['left', [b[3], b[0], t[0], t[3]]], ['top', [t[0], t[1], t[2], t[3]]]];
  const area = q => { let s = 0; for (let k = 0; k < q.length; k++) { const [x0, y0] = q[k], [x1, y1] = q[(k + 1) % q.length]; s += x0 * y1 - x1 * y0; } return s; };
  const sTop = Math.sign(area(faces[4][1])), lab = new Map();
  for (const [n, q] of faces) {
    if (Math.sign(area(q)) !== sTop) continue;
    for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
      let ins = false; const px = x + .5, py = y + .5;
      for (let a = 0, c = q.length - 1; a < q.length; c = a++) { const [xi, yi] = q[a], [xj, yj] = q[c]; if ((yi > py) !== (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi) ins = !ins; }
      if (ins) { I.put(x, y, 'g', pal[n] || pal.front); lab.set(x + ',' + y, n); }
    }
  }
  for (const [k, n] of lab) {
    const [x, y] = k.split(',').map(Number);
    if (n === 'top' && (!lab.has(x + ',' + (y - 1)) || !lab.has((x - 1) + ',' + y))) I.paint(x, y, pal.hi);
    if (n === 'front' && !lab.has(x + ',' + (y + 1))) I.paint(x, y, pal.frontD);
  }
  lab.each = (face, fn) => { for (const [k, n] of lab) if (n === face) { const [x, y] = k.split(',').map(Number); fn(x, y); } };
  return lab;
}

// A pipe from its far end F to its open near end N, radius r; the open end is an ellipse e deep along the axis.
// Shaded as a cylinder lit from the upper left; the hole shows the dark inside and a lit far wall.
function tube(I, F, N, r, e, pal, o = {}) {
  I.obj();
  const ax = N[0] - F[0], ay = N[1] - F[1], L = Math.hypot(ax, ay), ux = ax / L, uy = ay / L;
  let nx = -uy, ny = ux; if (nx + ny > 0) { nx = -nx; ny = -ny; } // the normal facing the light (up-left)
  const band = t => t > 0.78 ? pal[1] : t > 0.25 ? pal[0] : t > -0.35 ? pal[1] : t > -0.75 ? pal[2] : pal[3];
  const lab = new Map();
  for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
    const dx = x + .5 - F[0], dy = y + .5 - F[1], a = dx * ux + dy * uy, s = dx * nx + dy * ny;
    const inBody = a >= 0 && a <= L && Math.abs(s) <= r;
    const inFar = (a / e) ** 2 + (s / r) ** 2 <= 1;
    const da = a - L, inNear = (da / e) ** 2 + (s / r) ** 2 <= 1;
    if (inNear) {
      const ri = r - (o.wall ?? 1.25), ei = e - (o.wallE ?? 0.8);
      const hole = ri > 0 && ei > 0 && (da / ei) ** 2 + (s / ri) ** 2 <= 1;
      let c;
      if (hole) c = (da < -ei * 0.15 && s < 0) || (o.litFar && da < 0) ? '#474e59' : '#23272e';
      else c = s > 0 || da < 0 ? pal[0] : pal[2];
      if (!hole && s > r * 0.55 && da > 0) c = HI;
      I.put(x, y, 'tube', c); lab.set(x + ',' + y, hole ? 'hole' : 'rim');
    } else if (inBody || inFar) { I.put(x, y, 'tube', band(s / r)); lab.set(x + ',' + y, 'body'); }
  }
  lab.axis = { ux, uy, nx, ny, L };
  return lab;
}

// A helix of one-pixel wire from A to B, radius R, T turns, tilted so each turn is an ellipse:
// the back arc dark, the front arc bright and drawn over it, all one object. Returns the curve.
function helix(I, A, B, R, T, tilt, o = {}) {
  const ax = B[0] - A[0], ay = B[1] - A[1], L = Math.hypot(ax, ay), ux = ax / L, uy = ay / L;
  let nx = -uy, ny = ux; if (ny > 0 || (ny === 0 && nx > 0)) { nx = -nx; ny = -ny; }
  const pt = t => { const f = t / (T * 2 * Math.PI), c = Math.cos(t), s = Math.sin(t);
    return [A[0] + ux * (L * f - R * tilt * s) + nx * R * c, A[1] + uy * (L * f - R * tilt * s) + ny * R * c, s, c]; };
  I.obj();
  for (const front of [false, true]) {
    if (front && o.sep) I.obj();
    for (let k = 0; k <= 800; k++) {
      const t = k / 800 * T * 2 * Math.PI, [x, y, z, c] = pt(t); if ((z > 0) !== front) continue;
      I.put(Math.floor(x), Math.floor(y), 'cu', front ? (c > 0.2 ? P.copper[0] : c > -0.7 ? P.copper[1] : P.copper[2]) : P.copper[3]);
    }
  }
  return pt;
}
// a loose end in the current object: pixel-exact segments through the points
const lead = (I, pts, c = P.copper[1]) => { for (let k = 0; k + 1 < pts.length; k++) I.seg(...pts[k], ...pts[k + 1], 'cu', c); };

// ---------- the items ----------
const items = [];
const item = (id, name, what) => { const it = { id, name, what, variants: [] }; items.push(it); return it; };
const v = (it, desc, draw, states) => {
  const mk = d => { const I = new Icon(); d(I); return I; };
  it.variants.push(states ? { desc, icon: mk(s => draw(s, true)), alt: mk(s => draw(s, false)) } : { desc, icon: mk(draw) });
};

// ===== TUBE =====
const tubeIt = item('tube', 'Трубка', 'короткая труба из свальцованного железа');
v(tubeIt, 'По диагонали, открытый торец к нам снизу слева, шов вдоль', I => {
  const f = tube(I, [12.2, 3.8], [4.4, 11.6], 3.1, 1.9, P.iron);
  const { ux, uy, nx, ny, L } = f.axis;
  for (let a = 1.5; a < L - 1; a += 1) I.paint(12.2 + ux * a - nx * 1.6, 3.8 + uy * a - ny * 1.6, P.iron[3]);
});
v(tubeIt, 'Лежит горизонтально, торец слева: видно тёмное нутро', I => {
  const f = tube(I, [13.2, 8.5], [3.8, 8.5], 3.6, 1.7, P.iron);
  for (let x = 6; x <= 12; x++) if (x % 3 !== 2) I.paint(x, 10, P.iron[3]);
});
v(tubeIt, 'Стоит торцом вверх: смотрим в трубу сверху', I => {
  tube(I, [7.6, 13.2], [8.0, 4.2], 4.6, 2.2, P.iron, { wall: 1.4, wallE: 0.9, litFar: false });
  for (let y = 7; y <= 13; y++) I.paint(10, y, P.iron[2]);
});

// ===== FRAME =====
const frame = item('frame', 'Рама', 'сварной квадрат из уголка с косынками');
function frameSq(I, x0, y0, x1, y1, w, m, gus = 3) {
  I.obj();
  for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) if (x < x0 + w || x > x1 - w || y < y0 + w || y > y1 - w) I.put(x, y, m);
  // gussets: a triangle in each inner corner
  for (let k = 0; k < gus; k++) for (let j = 0; j < gus - k; j++) {
    I.put(x0 + w + j, y0 + w + k, m); I.put(x1 - w - j, y0 + w + k, m);
    I.put(x0 + w + j, y1 - w - k, m); I.put(x1 - w - j, y1 - w - k, m);
  }
  I.shade(m);
}
v(frame, 'Квадрат анфас: уголок с тёмной внутренней полкой, косынки в углах', I => {
  frameSq(I, 1, 1, 14, 14, 3, 'iron', 3);
  // the angle's second leg: a darker strip along the inside of each bar
  for (let k = 3; k <= 12; k++) { I.paint(k, 3, P.iron[2]); I.paint(3, k, P.iron[2]); I.paint(k, 12, P.iron[3]); I.paint(12, k, P.iron[3]); }
  for (let k = 1; k <= 13; k++) { I.paint(k, 1, P.iron[0]); I.paint(1, k, P.iron[0]); }
  // gussets lighter, a weld bead where each meets the bars
  for (const [x, y] of [[4, 4], [5, 4], [4, 5], [11, 4], [10, 4], [11, 5], [4, 11], [4, 10], [5, 11], [11, 11], [10, 11], [11, 10]]) I.paint(x, y, P.iron[1]);
  for (const [x, y] of [[5, 5], [10, 5], [5, 10], [10, 10]]) I.paint(x, y, P.iron[0]);
  for (const [x, y] of [[2, 2], [13, 2], [2, 13], [13, 13]]) I.paint(x, y, '#8a929e');
});
v(frame, 'Лежит в ракурсе: видна толщина уголка, косынки в углах', I => {
  const O = [1, 13.5], U = [11, 0], V = [3, -10.5], w = 0.18, g = 0.2;
  const det = U[0] * V[1] - U[1] * V[0];
  const at = (px, py) => { const dx = px - O[0], dy = py - O[1]; return [(dx * V[1] - dy * V[0]) / det, (U[0] * dy - U[1] * dx) / det]; };
  const part = (u, v) => {
    if (u < 0 || v < 0 || u > 1 || v > 1) return null;
    if (u < w || v < w || u > 1 - w || v > 1 - w) return 'bar';
    const a = Math.min(u, 1 - u) - w, b = Math.min(v, 1 - v) - w;
    return a + b < g ? 'gus' : null;
  };
  I.obj();
  for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) if (part(...at(x + .5, y - .5))) I.put(x, y, 'iron', P.iron[3]);
  for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
    const p = part(...at(x + .5, y + .5)); if (!p) continue;
    I.put(x, y, 'iron', p === 'gus' ? '#c3cbd5' : P.iron[1]);
  }
  each(I, (x, y) => { const p = part(...at(x + .5, y + .5)); if (p && (!part(...at(x + .5, y - .5)) || !part(...at(x - .5, y + .5)))) I.paint(x, y, p === 'gus' ? HI : P.iron[0]); });
  each(I, (x, y) => { if (part(...at(x + .5, y + .5)) === 'gus' && part(...at(x + .5, y + 1.5)) === null && I.has(x, y + 1)) I.paint(x, y, P.iron[2]); });
});

v(frame, 'Тёмный уголок, сварные швы на стыках, яркие косынки', I => {
  frameSq(I, 1, 1, 14, 14, 3, 'dark', 0);
  for (let k = 1; k <= 13; k++) { I.paint(k, 1, P.dark[0]); I.paint(1, k, P.dark[0]); }
  for (let k = 3; k <= 12; k++) { I.paint(k, 3, P.dark[2]); I.paint(3, k, P.dark[2]); }
  // diagonal weld seams at the mitred corners
  for (const [x, y] of [[2, 2], [3, 3], [13, 2], [12, 3], [2, 13], [3, 12], [13, 13], [12, 12]]) I.paint(x, y, P.rust[1]);
  for (const [x0, y0, sx, sy] of [[4, 4, 1, 1], [11, 4, -1, 1], [4, 11, 1, -1], [11, 11, -1, -1]]) {
    I.obj(); for (let k = 0; k < 3; k++) for (let j = 0; j < 3 - k; j++) I.put(x0 + sx * j, y0 + sy * k, 'steel');
    I.shade('steel');
  }
});

// ===== COIL =====
const coil = item('coil', 'Катушка', 'медная спираль: пружина или обмотка динамо');
v(coil, 'Лежит: три витка, выводы вниз слева и вверх справа', I => {
  helix(I, [3, 8], [13, 8], 6, 3, 0.45);
  lead(I, [[2, 13], [1, 14], [0, 15]]); lead(I, [[13, 2], [14, 1], [15, 0]], P.copper[0]);
});
v(coil, 'Стоит: пружина из трёх витков, выводы вбок', I => {
  helix(I, [8, 13], [8, 3], 6, 3, 0.45);
  lead(I, [[2, 13], [0, 14]]); lead(I, [[2, 2], [0, 1]], P.copper[0]);
});
v(coil, 'По диагонали, четыре плотных витка, концы торчат вдоль оси', I => {
  helix(I, [4.5, 11.5], [11.5, 4.5], 4.4, 4, 0.25);
  lead(I, [[3, 13], [1, 15]]); lead(I, [[13, 3], [15, 1]], P.copper[0]);
});

// ===== SOLAR CELL =====
const solar = item('solarCell', 'Солнечный элемент', 'тёмно-синяя пластина с серебряными шинами');
function wafer(I, x0, y0, x1, y1, bus, chamfer = true) {
  I.obj(); I.rect(x0, y0, x1, y1, 'cell');
  if (chamfer) for (const [x, y] of [[x0, y0], [x1, y0], [x0, y1], [x1, y1]]) I.erase(x, y);
  each(I, (x, y) => I.paint(x, y, (x + y) % 2 ? P.cell[1] : P.cell[2]));
  each(I, (x, y) => { if ((y - y0) % 3 === 0 && y !== y0) I.paint(x, y, P.cell[3]); });
  for (const bx of bus) for (let y = y0; y <= y1; y++) { I.paint(bx, y, P.steel[0]); I.paint(bx + 1, y, P.steel[2]); }
  for (let x = x0; x <= x1; x++) if (!bus.includes(x) && !bus.includes(x - 1)) I.paint(x, y0, P.cell[0]);
  for (let y = y0; y <= y1; y++) I.paint(x0, y, P.cell[0]);
}
v(solar, 'Пластина анфас: срезанные углы, две серебряные шины, сетка', I => {
  wafer(I, 1, 1, 14, 14, [5, 10]);
  for (const [x, y] of [[3, 12], [4, 11], [12, 4], [12, 3]]) I.paint(x, y, P.cell[0]);
});
v(solar, 'Пластина в ракурсе: видна тонкая кромка, блик наискось', I => {
  const f = box(I, { L: 12.5, W: 10.5, H: 1.3, bx: 0.32, by: -0.66, ox: 0.8, oy: 14 }, { hi: P.cell[0], top: P.cell[2], front: '#8a93a0', frontD: '#646d7a', left: '#aab4c0', right: '#646d7a' });
  f.each('top', (x, y) => { if ((y + 1) % 3 === 0) I.paint(x, y, P.cell[3]); });
  f.each('top', (x, y) => { const u = x - (14 - y) * 0.48; if (Math.abs(u - 4.3) < 0.6 || Math.abs(u - 8.9) < 0.6) I.paint(x, y, P.steel[(y % 2) ? 0 : 1]); });
  f.each('top', (x, y) => { const s = x + y; if (s === 15 || s === 16) if (I.col[I.i(x, y)] !== P.steel[0] && I.col[I.i(x, y)] !== P.steel[1]) I.paint(x, y, P.cell[0]); });
});
v(solar, 'Пластина с паяными ленточками-выводами снизу', I => {
  wafer(I, 1, 1, 14, 11, [5, 10]);
  for (const bx of [5, 10]) { I.obj(); I.rect(bx, 12, bx + 1, 14, 'steel'); I.paint(bx, 12, P.steel[0]); I.paint(bx, 13, P.steel[0]); I.paint(bx, 14, P.steel[1]); I.paint(bx + 1, 12, P.steel[1]); I.paint(bx + 1, 13, P.steel[2]); I.paint(bx + 1, 14, P.steel[2]); }
  for (const [x, y] of [[2, 9], [3, 8], [12, 3], [13, 2]]) I.paint(x, y, P.cell[0], true);
});

// ===== SILICON =====
const sil = item('silicon', 'Кремний', 'очищенный кремний: металлический блеск, серо-синий');
v(sil, 'Кристаллический кусок: грани разного тона, блестящие рёбра', I => {
  I.obj(); I.poly([[2, 9], [5, 3.5], [10, 2], [14, 6], [14.5, 11], [10, 14.5], [4, 14]], 'sil');
  const inside = (q, x, y) => { let s = false; for (let a = 0, c = q.length - 1; a < q.length; c = a++) { const [xi, yi] = q[a], [xj, yj] = q[c]; if ((yi > y) !== (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) s = !s; } return s; };
  const facets = [[[5, 3.5], [10, 2], [14, 6], [8.5, 7]], [[2, 9], [5, 3.5], [8.5, 7], [6.5, 11], [4, 14]], [[14.5, 11], [10, 14.5], [10.5, 11]], [[8.5, 7], [14, 6], [14.5, 11], [10, 14.5], [6.5, 11]]];
  const tones = [P.sil[0], P.sil[1], P.sil[3], P.sil[2]];
  each(I, (x, y) => { const k = facets.findIndex(q => inside(q, x + .5, y + .5)); I.paint(x, y, tones[k < 0 ? 1 : k]); });
  I.pseg(5, 4, 8, 7, SHEEN); I.pseg(9, 7, 13, 6, '#9aa7c4'); I.pseg(7, 11, 9, 13, P.sil[1]);
  I.paint(6, 4, GLINT); I.paint(9, 3, SHEEN);
});

v(sil, 'Слиток: как железо, но серо-синий, с полосой блеска', I => {
  const f = box(I, { L: 13, W: 8, H: 4, i: 1.6, bx: 0.25, by: -0.5, ox: 1, oy: 12.5 }, SIL);
  f.each('top', (x, y) => { const s = x + y * 1.6; if (s > 13.5 && s < 15.5) I.paint(x, y, SHEEN); });
  f.each('front', (x, y) => { if (x === 4 || x === 5) I.paint(x, y, '#6f7b99'); });
});
v(sil, 'Монокристалл-буля: цилиндр с конусом на конце', I => {
  I.obj();
  for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
    const px = x + .5, py = y + .5;
    const r = px < 2 ? 0 : px < 5.5 ? 1.2 + (px - 2) / 3.5 * 3.3 : px < 12.5 ? 4.5 : px < 14.8 ? 4.5 - (px - 12.5) * 1.1 : 0;
    if (Math.abs(py - 8.5) <= r) I.put(x, y, 'sil');
  }
  each(I, (x, y) => { const t = (8.5 - (y + .5)) / 4.5; I.paint(x, y, t > 0.6 ? P.sil[1] : t > 0.15 ? P.sil[0] : t > -0.4 ? P.sil[1] : t > -0.78 ? P.sil[2] : P.sil[3]); });
  for (let x = 4; x <= 13; x++) I.paint(x, 6, SHEEN);
  I.paint(8, 6, GLINT);
  for (let x = 6; x <= 12; x += 2) I.paint(x, 10, P.sil[2]);
});

// ===== GLASS =====
const glass = item('glass', 'Стекло', 'бледно-голубое, прозрачное');
v(glass, 'Лист в ракурсе: сквозь него видны дальние кромки, два блика', I => {
  const f = box(I, { L: 12, W: 11, H: 1.4, bx: 0.3, by: -0.6, ox: 1.2, oy: 13.5 }, GLS);
  f.each('top', (x, y) => I.paint(x, y, P.glass[1]));
  f.each('top', (x, y) => { const s = x - (13.5 - y) * 0.5; if (s > 10 || y >= 11) I.paint(x, y, P.glass[2]); });
  f.each('top', (x, y) => { const s = x + y; if (s === 11 || s === 12 || s === 15) I.paint(x, y, '#ffffff'); });
  f.each('top', (x, y) => { if (x === 2 + Math.round((13.5 - y) * 0.3)) I.paint(x, y, P.glass[0]); });
});
v(glass, 'Стеклянный брусок: светлые торцы, внутренние рёбра просвечивают', I => {
  const f = box(I, { L: 12, W: 7, H: 4.5, bx: 0.3, by: -0.55, ox: 1.2, oy: 13 }, GLS);
  // the hidden back edges seen through the glass
  for (let x = 4; x <= 13; x++) I.paint(x, 7, P.glass[2]);
  for (let y = 7; y <= 10; y++) I.paint(3, y, P.glass[2]);
  for (const [x, y] of [[5, 4], [6, 4], [6, 9], [7, 9], [7, 10], [8, 10], [8, 11]]) I.paint(x, y, '#ffffff');
});
v(glass, 'Стоячее стекло с настоящей прозрачностью: фон виден насквозь', I => {
  I.obj(); I.poly([[3, 1], [13, 1], [13, 15], [3, 15]], 'glass');
  each(I, (x, y) => I.paint(x, y, x === 3 || y === 1 ? P.glass[0] : x === 12 || y === 14 ? P.glass[2] : '#a8dde8'));
  for (const [x, y] of [[9, 3], [8, 4], [7, 5], [6, 6], [10, 3], [9, 4], [5, 7], [11, 8], [10, 9], [9, 10]]) I.paint(x, y, '#ffffff');
  I.alpha = c => (c === '#a8dde8' ? 120 : c === '#ffffff' ? 235 : c === OUT ? 255 : 215);
});

// ===== ASTEROID ROCK =====
const rock = item('asteroidRock', 'Астероидная порода', 'обломок серого камня с вкраплениями руды');
const glints = (I, pts) => { for (const [x, y, k] of pts) { const c = [P.brass[0], P.copper[0], '#9fe6f2'][k]; I.paint(x, y, c); I.paint(x + 1, y + 1, [P.brass[2], P.copper[2], '#4d9fb0'][k]); } };
v(rock, 'Округлый ком, золотистые и медные искры руды', I => {
  I.obj(); I.poly([[1.5, 9], [3, 4.5], [7, 2], [11.5, 3], [14.5, 7], [14, 12], [10, 14.5], [4, 14]], 'rock'); I.shadeRound(7.5, 8, 7, 'rock', [-0.5, 0.15, 0.6]);
  for (const [x, y] of [[6, 9], [7, 9], [10, 6], [4, 6], [9, 12]]) I.tone(x, y, 'rock', 3);
  glints(I, [[5, 5, 0], [10, 9, 1], [4, 11, 0], [11, 4, 0]]);
  I.paint(6, 4, GLINT);
});
v(rock, 'Угловатый обломок с плоскими гранями и прожилкой руды', I => {
  I.obj(); I.poly([[1, 11], [3, 4], [9, 1.5], [14.5, 5], [15, 11], [9, 15], [3, 14.5]], 'rockC');
  each(I, (x, y) => {
    const f = y < 4 + (x - 3) * 0.1 && x < 12 ? 0 : x < 5 && y < 12 ? 1 : x > 11 ? 2 : y > 11 ? 3 : 1;
    I.paint(x, y, P.rockC[f]);
  });
  for (const [x, y] of [[5, 6], [6, 7], [7, 7], [8, 8], [9, 8], [10, 9], [11, 10]]) I.paint(x, y, P.rust[1]);
  for (const [x, y] of [[6, 6], [8, 7], [10, 8]]) I.paint(x, y, P.rust[0]);
  glints(I, [[12, 6, 2], [4, 9, 2]]);
  I.paint(7, 3, GLINT);
});
v(rock, 'Пористый астероид в кратерах, искры руды по краю', I => {
  I.obj(); I.disc(8, 8.2, 6.8, 'rock'); I.erase(1, 8); I.erase(14, 4); I.shadeRound(8, 8, 7, 'rock', [-0.5, 0.15, 0.6]);
  for (const [cx, cy, rr] of [[6, 6, 1.6], [10.5, 10, 1.9], [5, 11, 1.1], [11, 5, 1.0]]) each(I, (x, y) => {
    const dx = x + .5 - cx, dy = y + .5 - cy, d = Math.hypot(dx, dy);
    if (d <= rr) I.paint(x, y, dx + dy < 0 ? P.rock[3] : P.rock[2]);
    else if (d <= rr + 0.9 && dx + dy > 0) I.paint(x, y, P.rock[0]);
  });
  glints(I, [[3, 4, 0], [12, 7, 1], [8, 13, 0]]);
  I.paint(4, 3, GLINT);
});

// ===== BATTERY ===== (variants carry a charged and an empty state)
const bat = item('battery', 'Батарея', 'заряжена / пуста');
v(bat, 'Стоячий элемент: окошко с тремя полосками заряда', (I, on) => {
  I.obj(); I.rect(6, 1, 9, 2, 'steel'); I.shade('steel');
  I.obj(); I.rect(3, 3, 12, 14, 'case'); I.shade('case');
  for (let x = 4; x <= 11; x++) { I.paint(x, 3, P.steel[0]); I.paint(x, 4, P.steel[1]); I.paint(x, 14, P.steel[2]); }
  I.paint(3, 3, P.steel[1]); I.paint(12, 3, P.steel[2]); I.paint(3, 14, P.steel[2]); I.paint(12, 14, P.steel[3]);
  for (let y = 6; y <= 12; y++) for (let x = 5; x <= 10; x++) I.paint(x, y, OUT);
  for (const [k, y] of [[0, 7], [1, 9], [2, 11]]) for (let x = 6; x <= 9; x++) I.paint(x, y, on ? (k === 0 ? LEDON[0] : LEDON[1]) : LEDOFF[0]);
  if (on) for (const y of [7, 9, 11]) I.paint(9, y, LEDON[2]);
}, true);
v(bat, 'Аккумулятор-короб: две клеммы, круглый индикатор', (I, on) => {
  const f = box(I, { L: 13, W: 5, H: 9, bx: 0.3, by: -0.5, ox: 1, oy: 14.5 }, { hi: '#8a93a0', top: '#646d7a', front: '#474e59', frontD: '#30353e', left: '#646d7a', right: '#30353e' });
  f.each('front', (x, y) => { if (y === 8) I.paint(x, y, P.yellow[1]); });
  I.obj(); I.rect(3, 2, 4, 3, 'steel'); I.shade('steel'); I.obj(); I.rect(10, 2, 11, 3, 'copper'); I.shade('copper');
  I.cur = 1; // the lamp is a window in the box
  const lamp = on ? [LEDON[0], LEDON[1], LEDON[1], LEDON[2]] : ['#4a515b', LEDOFF[0], LEDOFF[0], LEDOFF[1]];
  for (let y = 9; y <= 12; y++) for (let x = 8; x <= 11; x++) I.paint(x, y, OUT);
  [[9, 10], [10, 10], [9, 11], [10, 11]].forEach(([x, y], k) => I.paint(x, y, lamp[k]));
}, true);
v(bat, 'Лежачая банка с латунным колпачком и шкалой из четырёх делений', (I, on) => {
  I.obj(); I.rect(13, 6, 14, 10, 'brass'); I.shade('brass');
  I.obj(); I.rect(1, 4, 12, 12, 'paint');
  each(I, (x, y) => I.paint(x, y, y <= 4 ? P.paint[0] : y <= 6 ? P.paint[1] : y <= 10 ? P.paint[2] : P.paint[3]));
  for (let y = 4; y <= 12; y++) { I.paint(1, y, P.steel[1]); I.paint(12, y, P.steel[2]); }
  for (let x = 2; x <= 11; x++) { I.paint(x, 7, OUT); I.paint(x, 9, OUT); }
  for (let k = 0; k < 4; k++) for (const x of [3 + k * 2, 4 + k * 2]) I.paint(x, 8, on ? (x % 2 ? LEDON[1] : LEDON[0]) : LEDOFF[0]);
  for (const x of [2, 11]) I.paint(x, 8, OUT);
}, true);
module.exports = { items };

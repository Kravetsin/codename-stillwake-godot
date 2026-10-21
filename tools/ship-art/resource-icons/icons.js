// Concepts for Stillwake's early resource icons: every item, three or four ways.
const { Icon, P, OUT, HI } = require('./art.js');

// ---------- shared pieces ----------
const IRON = { hi: '#eef2f7', top: '#c9d2de', front: '#8d99aa', frontD: '#66727f', left: '#aab5c4', right: '#56606e' };
const PIG = { hi: '#b3bbc6', top: '#8d96a3', front: '#636b77', frontD: '#474e58', left: '#7a8390', right: '#3c424b' };
const STEEL = { hi: '#f4f7fa', top: '#c3cbd5', front: '#7d8896', frontD: '#5d6774', left: '#a6b0bd', right: '#56606c' };
const HULL = { hi: '#b9cde0', top: '#7f9ab4', front: '#55697e', frontD: '#3f4f60', left: '#6d859c', right: '#3d4c5c' };
const WOOD = { hi: '#ecc48c', top: '#c99055', front: '#a06a37', frontD: '#7b4c25', left: '#e6bd84', right: '#6e4421' };
const PINE = { hi: '#f5dcaa', top: '#ddb071', front: '#b8844a', frontD: '#8c5f30', left: '#f2d49c', right: '#7c5228' };

// a box (tapered by i at its top) projected obliquely; faces turned away are skipped.
// Returns a map "x,y" -> face so details can be painted per face.
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

// a cog: symmetric teeth (tooth centres at phase + k*2pi/n)
function gear(I, cx, cy, rRoot, rOut, n, m, phase = 0, duty = 0.15) {
  I.obj();
  for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
    const dx = x + .5 - cx, dy = y + .5 - cy, r = Math.hypot(dx, dy);
    if (r <= rRoot || (r <= rOut && Math.cos(n * (Math.atan2(dy, dx) - phase)) > duty)) I.put(x, y, m);
  }
}

// a cog with rectangular teeth, flat-shaded
function gearR(I, cx, cy, rB, rO, n, phase, hw, m) {
  I.obj();
  for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
    const dx = x + .5 - cx, dy = y + .5 - cy; let ins = Math.hypot(dx, dy) <= rB;
    for (let k = 0; k < n && !ins; k++) { const a = phase + k * 2 * Math.PI / n, u = dx * Math.cos(a) + dy * Math.sin(a), w = -dx * Math.sin(a) + dy * Math.cos(a); if (u > 0 && u <= rO && Math.abs(w) <= hw) ins = true; }
    if (ins) I.put(x, y, m);
  }
  I.shade(m);
}
const hub = (I, cx, cy, r0, r1, m) => { for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) { const d = Math.hypot(x + .5 - cx, y + .5 - cy); if (d > r0 && d < r1) I.paint(x, y, (x + .5 - cx) + (y + .5 - cy) < 0 ? P[m][2] : P[m][0]); } };

function nut(I, cx, cy, r, m) {
  I.obj(); const pts = []; for (let k = 0; k < 6; k++) pts.push([cx + Math.cos(k * Math.PI / 3) * r, cy + Math.sin(k * Math.PI / 3) * r]);
  I.poly(pts, m); I.shade(m);
}

function loop(I, cx, cy, rx, ry, t, m) {
  for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
    const dx = (x + .5 - cx) / rx, dy = (y + .5 - cy) / ry, d = Math.hypot(dx, dy);
    if (d <= 1 && d >= 1 - t / Math.min(rx, ry)) I.put(x, y, m);
  }
}

const rivet = (I, x, y) => { I.paint(x, y, HI, true); I.paint(x + 1, y + 1, P.dark[3], true); };
const each = (I, fn) => { for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) if (I.mine(x, y)) fn(x, y); };

// a board at 45 degrees with square-cut ends; end grain at the start
function plank45(I, sx, sy, ex, ey, w, m) {
  I.obj(); const dx = ex - sx, dy = ey - sy, l = Math.hypot(dx, dy), nx = -dy / l * w / 2, ny = dx / l * w / 2;
  I.poly([[sx + nx, sy + ny], [ex + nx, ey + ny], [ex - nx, ey - ny], [sx - nx, sy - ny]], m); I.shade(m);
  // grain: a dark line along the middle, broken
  for (let k = 2; k < l - 1; k++) if (k % 4 !== 0) I.tone(sx + dx * k / l, sy + dy * k / l, m, 2);
}

// ---------- the items ----------
const items = [];
const item = (id, name, what) => { const it = { id, name, what, variants: [] }; items.push(it); return it; };
const v = (it, desc, draw) => { const I = new Icon(); draw(I); it.variants.push({ desc, icon: I }); };

// ===== SCRAP =====
const scrap = item('scrap', 'Скрап', 'мелкий металлолом, основной ресурс');
v(scrap, 'Обрезок гнутой трубы с ржавым срезом, рядом гайка', I => {
  I.obj();
  for (let k = 0; k <= 20; k++) { const a = Math.PI + k / 20 * Math.PI / 2; I.disc(12 + Math.cos(a) * 8, 12 + Math.sin(a) * 8, 2.3, 'steel'); }
  I.rect(10, 1, 13, 6, 'steel');
  I.shade('steel');
  for (let k = 2; k <= 18; k++) { const a = Math.PI + k / 20 * Math.PI / 2; I.paint(12 + Math.cos(a) * 9.2, 12 + Math.sin(a) * 9.2, P.steel[0]); }
  for (const [x, y] of [[5, 8], [6, 6], [4, 10]]) I.paint(x, y, P.rust[1]);
  I.obj(); I.ellipse(12, 2.5, 2.3, 1.6, 'rust'); I.shade('rust'); I.paint(11, 2, OUT); I.paint(12, 2, OUT);
  nut(I, 11.4, 12, 2.5, 'rust2'); I.erase(11, 11); I.erase(11, 12);
});
v(scrap, 'Скрученная ржавая полоса и болт поперёк', I => {
  I.obj();
  const pts = []; for (let k = 0; k <= 10; k++) pts.push([2 + k * 1.15, 13 - k * 1.05]);
  for (let k = 0; k < 10; k++) { const [x0, y0] = pts[k], [x1, y1] = pts[k + 1]; I.line(x0, y0, x1, y1, k % 3 === 1 ? 1.2 : 3.2, 'rust'); }
  I.shade('rust');
  for (let k = 0; k < 10; k++) if (k % 3 === 1) { const [x, y] = pts[k]; I.tone(x, y, 'rust', 3); }
  I.obj(); I.line(4.5, 5, 11.5, 12, 1.8, 'steel'); I.shade('steel');
  nut(I, 3.6, 3.8, 2.4, 'dark');
});
v(scrap, 'Горка вперемешку: ржавые обломки, лист, осколок шестерни сверху', I => {
  I.obj(); I.poly([[1, 14.5], [3.5, 9], [7, 6.5], [10, 7], [14.5, 14.5]], 'rust2'); I.shade('rust2');
  I.obj(); I.poly([[2, 14.5], [4, 10.5], [8, 12.5], [7, 14.5]], 'rust'); I.shade('rust');
  I.obj(); I.poly([[9, 14.5], [10, 10], [14.5, 11.5], [14.5, 14.5]], 'paint'); I.shade('paint');
  gear(I, 8.5, 8, 2.6, 4.2, 6, 'steel', 0.25, 0.1);
  each(I, (x, y) => { if (y + .5 > 8.6) I.erase(x, y); }); I.shade('steel'); I.erase(8, 7);
});
v(scrap, 'Горсть крепежа: большой болт, гайка, шайба', I => {
  I.obj(); I.ring(10.8, 10.8, 1.4, 3.6, 'steel'); I.shade('steel');
  I.obj(); I.line(4.5, 4.5, 12.5, 12.5, 2.6, 'iron'); I.shade('iron'); for (let k = 0; k < 4; k++) I.tone(7 + k * 1.7, 8 + k * 1.7, 'iron', 3);
  I.obj(); I.poly([[1.5, 4], [4, 1.5], [7, 4.5], [4.5, 7]], 'dark'); I.shade('dark');
  nut(I, 4.2, 11.6, 2.9, 'rust'); I.erase(4, 11);
});

// ===== SCRAP CHUNK =====
const chunk = item('scrapChunk', 'Ком скрапа', 'сырой ком лома, печь переплавляет в скрап');
function cube(I, bands, noise = true) {
  // a cube in oblique view: front x1..10 y5..13, top rising to the right, right face columns 11..13
  I.obj();
  for (let y = 5; y <= 13; y++) for (let x = 1; x <= 10; x++) I.put(x, y, 'f', P[bands[Math.floor((y - 5) / 2) % bands.length]][1]);
  for (let k = 0; k < 3; k++) for (let x = 2 + k; x <= 11 + k; x++) I.put(x, 4 - k, 't', P[bands[(Math.floor(x / 3) + k) % bands.length]][0]);
  for (let x = 11; x <= 13; x++) for (let y = 4 - (x - 11); y <= 13 - (x - 11); y++) I.put(x, y, 'r', P[bands[Math.floor((y + x - 16) / 2 + 9) % bands.length]][2]);
  // crumple: each layer has a dark lower lip that wanders
  for (let y = 6; y <= 13; y += 2) for (let x = 1; x <= 10; x++) if (noise ? (x * 7 + y) % 5 < 3 : true) I.paint(x, y, P[bands[Math.floor((y - 5) / 2) % bands.length]][2]);
  for (let y = 5; y <= 13; y++) I.paint(1, y, P[bands[Math.floor((y - 5) / 2) % bands.length]][0]);
}
v(chunk, 'Прессованный брикет: слои ржавчины и краски, торчит труба', I => {
  cube(I, ['rust', 'paint', 'rust2', 'rust', 'grime']);
  [[1, 13], [10, 13], [13, 4], [4, 2], [1, 5], [13, 11]].forEach(([x, y]) => I.erase(x, y));
  I.obj(); I.rect(7, 0, 9, 3, 'dark'); I.shade('dark'); I.paint(8, 0, OUT);
});
v(chunk, 'Бесформенный ржавый ком: торчат обрезок трубы, пластина и арматура', I => {
  I.obj(); I.poly([[3, 2], [6, 1.5], [5.5, 6], [2.5, 6]], 'paint'); I.shade('paint');
  I.obj(); I.line(10, 11, 14.5, 14.5, 1.6, 'rust2'); I.shade('rust2');
  I.obj(); I.disc(7, 8.5, 5, 'rust'); I.disc(10.5, 7.5, 3.5, 'rust'); I.disc(5, 10.5, 3.6, 'rust'); I.shadeRound(7.5, 8.5, 6, 'rust');
  for (const [x, y] of [[5, 8], [9, 10], [7, 6], [11, 8], [4, 12]]) I.tone(x, y, 'rust2', 2);
  I.obj(); I.disc(11.5, 4, 2.6, 'dark'); I.shade('dark'); I.erase(11, 3); I.erase(12, 4); I.erase(11, 4); I.erase(12, 3);
});
v(chunk, 'Брикет в обвязке: тёмный металл, жёлтая лента', I => {
  cube(I, ['grime', 'dark', 'rust2', 'dark']);
  [[1, 13], [13, 2], [2, 4]].forEach(([x, y]) => I.erase(x, y));
  for (let y = 5; y <= 13; y++) { I.paint(5, y, P.yellow[1], true); I.paint(6, y, P.yellow[2], true); }
  for (let k = 0; k < 3; k++) { I.paint(6 + k, 4 - k, P.yellow[0], true); I.paint(7 + k, 4 - k, P.yellow[1], true); }
});
v(chunk, 'Смятая бочка: красная краска, вмятины, ржавчина', I => {
  I.obj(); I.poly([[2, 3.5], [13, 2.5], [14.5, 7], [12.5, 9], [14.5, 12.5], [12, 14.5], [3, 14.5], [1.5, 11], [3.5, 8.5], [1.5, 6.5]], 'red'); I.shade('red');
  // the lid seen from above and two bent hoops
  I.obj(); I.ellipse(7.8, 3.5, 5.4, 1.7, 'red'); each(I, (x, y) => I.paint(x, y, (x + y) % 5 === 0 ? P.red[1] : P.red[0])); I.paint(10, 3, OUT); I.paint(10, 4, P.dark[1]);
  I.cur--; // the hoops paint onto the body
  for (let x = 2; x <= 13; x++) { I.paint(x, 7 + (x > 7 && x < 11 ? 1 : 0), P.red[3], true); I.paint(x, 11 + (x < 6 ? 1 : 0), P.red[3], true); }
  for (const [x, y] of [[4, 9], [5, 9], [10, 12], [11, 13], [12, 6], [3, 13], [7, 13]]) I.paint(x, y, P.rust[1], true);
});

// ===== HULL PLATE =====
const hull = item('hullPlate', 'Лист обшивки', 'клёпаный обломок обшивки; печь даёт железо и скрап');
v(hull, 'Квадратный крашеный лист, клёпки по краю, правый верх оторван', I => {
  I.obj(); I.poly([[1, 1], [9, 1], [10.5, 3], [12, 2], [15, 5.5], [15, 15], [1, 15]], 'paint'); I.shade('paint');
  for (let x = 3; x <= 12; x += 3) rivet(I, x, 12);
  for (let y = 3; y <= 9; y += 3) rivet(I, 2.5, y);
  for (let y = 7; y <= 9; y += 3) rivet(I, 12, y);
  for (const [x, y] of [[7, 6], [8, 6], [8, 7], [5, 9]]) I.tone(x, y, 'rust', 1);
});
v(hull, 'Толстый лист в ракурсе: видна кромка, два ряда клёпок', I => {
  const f = box(I, { L: 13, W: 11, H: 2.4, bx: 0.3, by: -0.62, ox: 1, oy: 13.5 }, HULL);
  for (const [x, y] of [[3, 5], [6, 5], [9, 5], [12, 5], [3, 9], [6, 9], [9, 9], [12, 9]]) rivet(I, x, y);
  for (const [x, y] of [[7, 7], [8, 7]]) I.paint(x, y, P.rust[1]);
});
v(hull, 'Лист с жёлто-чёрной разметкой, угол отогнут', I => {
  I.obj(); I.poly([[1, 1], [15, 1], [15, 10], [12, 15], [1, 15]], 'paint'); I.shade('paint');
  for (let y = 2; y <= 4; y++) for (let x = 1; x <= 14; x++) I.paint(x, y, ((x + y) % 4 < 2) ? P.yellow[1] : '#2a2e36');
  for (let x = 3; x <= 12; x += 4) rivet(I, x, 7); for (let x = 3; x <= 7; x += 4) rivet(I, x, 12);
  I.obj(); I.poly([[9.5, 15], [15, 9.5], [15, 15]], 'steel'); I.shade('steel');
});
v(hull, 'Половина листа, разорванная наискось: голый металл по рваному краю', I => {
  I.obj(); I.poly([[1, 1], [14, 1], [12.5, 3.5], [13.5, 5], [10.5, 7], [11, 9], [7.5, 10.5], [8, 12.5], [4.5, 13], [3.5, 15], [1, 15]], 'paint'); I.shade('paint');
  each(I, (x, y) => { if (!I.mine(x + 1, y) || !I.mine(x, y + 1)) if (x + y > 9 && x > 1 && y > 1) I.paint(x, y, P.steel[0]); });
  for (const [x, y] of [[3, 3], [6, 3], [9, 3], [3, 6], [3, 9]]) rivet(I, x, y);
  for (const [x, y] of [[7, 6], [6, 7], [5, 10], [9, 6]]) I.paint(x, y, P.rust[1]);
});

// ===== IRON =====
const iron = item('iron', 'Железо', 'выплавленный металл: слитки');
v(iron, 'Один крупный слиток: светлая верхняя грань, тёмный торец', I => { box(I, { L: 13, W: 8, H: 4, i: 1.6, bx: 0.25, by: -0.5, ox: 1, oy: 12.5 }, IRON); });
v(iron, 'Пирамидка из трёх слитков', I => {
  box(I, { L: 8, W: 5, H: 2.6, i: 1.1, bx: 0.2, by: -0.45, ox: 4, oy: 8.6 }, IRON);
  box(I, { L: 7, W: 5, H: 2.6, i: 1.1, bx: 0.2, by: -0.45, ox: 1, oy: 14 }, IRON);
  box(I, { L: 7, W: 5, H: 2.6, i: 1.1, bx: 0.2, by: -0.45, ox: 8, oy: 14 }, IRON);
});
v(iron, 'Два слитка стопкой, тёмный чугунный тон', I => {
  box(I, { L: 12, W: 7, H: 3, i: 1.3, bx: 0.25, by: -0.45, ox: 1.5, oy: 9.5 }, PIG);
  box(I, { L: 12, W: 7, H: 3, i: 1.3, bx: 0.25, by: -0.45, ox: 1.5, oy: 15 }, PIG);
});
v(iron, 'Слиток по диагонали, как в инвентаре у предметов-инструментов', I => {
  box(I, { L: 12, W: 7, H: 3.2, i: 1.4, ax: 0.71, ay: -0.71, bx: 0.71 * 0.62, by: 0.71 * 0.62, ox: 1.6, oy: 10.6 }, IRON);
});

// ===== PLANKS =====
const planks = item('planks', 'Доски', 'дерево: топливо для печи');
function board(I, x0, y, len, m, nails = []) {
  I.obj(); I.rect(x0, y, x0 + len - 1, y + 2, m); I.shade(m);
  for (let x = x0 + 2; x < x0 + len - 1; x++) if ((x * 5 + y * 3) % 7 < 2) I.tone(x, y + 1, m, 2);
  I.tone(x0, y, m, 0); I.tone(x0, y + 1, 'pine', 0); I.tone(x0, y + 2, m, 2);
  for (const nx of nails) I.paint(x0 + nx, y + 1, P.steel[0]);
}
v(planks, 'Три доски стопкой: торцы слева, концы вразнобой, шляпки гвоздей', I => {
  board(I, 3, 3, 11, 'wood', [2]); board(I, 1, 7, 13, 'wood', [2, 10]); board(I, 2, 11, 13, 'wood', [9]);
});
v(planks, 'Две доски крест-накрест, гвоздь в центре', I => {
  I.obj(); I.line(2.5, 3, 13.5, 13, 3.2, 'pine'); I.shade('pine');
  I.obj(); I.line(2.5, 13, 13.5, 3, 3.2, 'wood'); I.shade('wood');
  for (const [x, y] of [[5, 10], [10, 5]]) I.tone(x, y, 'wood', 2);
  I.paint(8, 8, P.steel[0]); I.paint(9, 9, P.dark[3], true);
});
v(planks, 'Две широкие доски по диагонали, прямой спил, волокна', I => {
  plank45(I, 2.2, 10.2, 10.2, 2.2, 4.2, 'pine');
  plank45(I, 5.8, 13.8, 13.8, 5.8, 4.2, 'wood');
  for (const [x, y] of [[3, 12], [4, 13]]) I.tone(x, y, 'pine', 0);
});
v(planks, 'Три узкие доски по диагонали, с гвоздями', I => {
  plank45(I, 1.6, 8.6, 8.6, 1.6, 3, 'wood');
  plank45(I, 3.6, 12.6, 12.6, 3.6, 3, 'pine');
  plank45(I, 7.4, 14.4, 14.4, 7.4, 3, 'wood');
  for (const [x, y] of [[4, 5], [7, 8], [10, 11]]) I.paint(x, y, P.steel[0], true);
});

// ===== WIRE =====
const wire = item('wire', 'Провод', 'медная проволока');
v(wire, 'Катушка: стальные щёки, медная намотка, хвост провода', I => {
  I.obj(); I.rect(4, 4, 11, 11, 'copper');
  each(I, (x, y) => I.paint(x, y, P.copper[y < 6 ? 0 : y < 9 ? 1 : y < 11 ? 2 : 3]));
  each(I, (x, y) => { if (x % 2 === 1) I.paint(x, y, P.copper[Math.min(3, (y < 6 ? 0 : y < 9 ? 1 : 2) + 1)]); });
  I.obj(); I.rect(2, 2, 3, 13, 'dark'); I.shade('dark'); I.obj(); I.rect(12, 2, 13, 13, 'dark'); I.shade('dark');
  I.obj(); I.seg(11, 12, 12, 14, 'copper', P.copper[1]); I.seg(12, 14, 14, 14, 'copper', P.copper[1]);
});
v(wire, 'Бухта спереди: толстое медное кольцо с витками, конец наружу', I => {
  I.obj(); I.ring(7.5, 7.5, 2.4, 6.6, 'copper'); I.shadeRound(7.5, 7.5, 6.6, 'copper', [-0.35, 0.25, 0.7]);
  each(I, (x, y) => { const d = Math.hypot(x + .5 - 7.5, y + .5 - 7.5); if (Math.abs(d - 4.5) < .45) I.tone(x, y, 'copper', 2); });
  I.obj(); I.seg(12, 11, 14, 14, 'copper', P.copper[0]);
});
v(wire, 'Витки в ракурсе, как лежащая пружина', I => {
  for (let k = 0; k < 4; k++) { I.obj(); loop(I, 4 + k * 2.4, 8, 2.8, 5.8, 1.4, 'copper'); I.shadeRound(4 + k * 2.4, 8, 5, 'copper'); }
});
v(wire, 'Моток, перехваченный красной изолентой', I => {
  I.obj(); I.ring(7.5, 7.5, 2.6, 6.6, 'copper'); I.shadeRound(7.5, 7.5, 6.6, 'copper', [-0.35, 0.25, 0.7]);
  each(I, (x, y) => { const d = Math.hypot(x + .5 - 7.5, y + .5 - 7.5); if (Math.abs(d - 4.6) < .4) I.tone(x, y, 'copper', 2); });
  for (const [x0, y0] of [[2, 6], [11, 6]]) for (let y = y0; y <= y0 + 2; y++) for (let x = x0; x <= x0 + 2; x++) I.paint(x, y, y === y0 ? P.red[0] : P.red[1]);
});

// ===== PLATE =====
const plate = item('plate', 'Пластина', 'штампованная стальная пластина');
v(plate, 'Лист в ракурсе: тонкая кромка, блик по диагонали', I => {
  const f = box(I, { L: 12, W: 11, H: 1.6, bx: 0.3, by: -0.6, ox: 1.2, oy: 13 }, STEEL);
  f.each('top', (x, y) => { const s = x - (13 - y); if (s === 0 || s === 1 || s === 4) I.paint(x, y, HI); });
});
v(plate, 'Стопка из трёх листов', I => {
  for (const [ox, oy] of [[2.6, 7.2], [1.4, 10.6], [2.2, 14]]) box(I, { L: 11.4, W: 7, H: 1.2, bx: 0.3, by: -0.5, ox, oy }, STEEL);
});
v(plate, 'Квадратная пластина с фаской и четырьмя отверстиями', I => {
  I.obj(); I.rect(2, 2, 13, 13, 'steel'); I.shade('steel');
  for (let x = 3; x <= 12; x++) { I.paint(x, 3, P.steel[0]); I.paint(x, 12, P.steel[2]); } for (let y = 3; y <= 12; y++) { I.paint(3, y, P.steel[0]); I.paint(12, y, P.steel[2]); }
  for (let k = 0; k < 4; k++) { I.paint(6 + k, 10 - k, P.steel[0]); I.paint(7 + k, 10 - k, '#c6ced8'); }
  for (const [x, y] of [[4, 4], [11, 4], [4, 11], [11, 11]]) I.erase(x, y);
});
v(plate, 'Профнастил: гофрированный лист, волны светом и тенью', I => {
  I.obj(); I.rect(1, 3, 14, 12, 'steel');
  for (let x = 1; x <= 14; x++) { if (x % 4 === 0) { I.erase(x, 3); I.erase(x, 12); } }
  const t = [HI, P.steel[0], P.steel[1], P.steel[2]];
  each(I, (x, y) => I.paint(x, y, x % 4 === 0 ? P.steel[3] : t[(x + 3) % 4]));
  each(I, (x, y) => { if (y >= 11 && I.col[I.i(x, y)] !== P.steel[3]) I.paint(x, y, P.steel[2]); });
});

// ===== GEAR =====
const gearIt = item('gear', 'Шестерня', 'зубчатое колесо');
v(gearIt, 'Стальная шестерня: 8 прямых зубьев, ступица с отверстием', I => {
  gearR(I, 8, 8, 4.3, 7.2, 8, 0, 1.5, 'steel'); hub(I, 8, 8, 1.6, 2.9, 'steel'); I.hole(8, 8, 1.5);
});
v(gearIt, 'Тяжёлая тёмная шестерня: 6 широких зубьев, большое отверстие', I => {
  gearR(I, 8, 8, 4.6, 7.2, 6, Math.PI / 2, 1.9, 'dark'); hub(I, 8, 8, 2.1, 3.3, 'dark'); I.hole(8, 8, 2.1);
});
v(gearIt, 'Две шестерни в зацеплении: стальная и латунная', I => {
  gearR(I, 11.6, 4.4, 2.1, 3.6, 6, Math.PI / 6, 0.9, 'brass'); I.hole(11.6, 4.4, 0.8);
  gearR(I, 6, 9.6, 3.4, 5.6, 8, 0, 1.2, 'steel'); hub(I, 6, 9.6, 1.2, 2.3, 'steel'); I.hole(6, 9.6, 1.2);
});
v(gearIt, 'Латунная шестерня: 10 зубьев, другой металл', I => {
  gearR(I, 8, 8, 4.8, 7.2, 10, 0, 1.05, 'brass'); hub(I, 8, 8, 1.6, 3, 'brass'); I.hole(8, 8, 1.5);
});

module.exports = { items };

// The arc bench candidates (ADR 0098 slice 3): six benches, 2x1 cells, drawn as the ship's machines are (outlined, 3-4 tones,
// light from the upper left), each a body, its shard alight over it, a front layer laid over a clamped tool, its glow, its
// 16 px icon, and the points its arcs leave from. node paint.js [outDir]
// The user picked "vise" (0099): its body, alight, front and glows are game/art's machine_arcBench, arcBench_*.
const fs = require('fs'), path = require('path');
const P = require(path.join(__dirname, '..', '..', 'homes-art', 'png.js'));
const OUT = process.argv[2] || path.join(__dirname, 'out');

const hx = (h) => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)];
const pal = (...h) => h.map(hx);
const mix = (a, b, t) => a.map((v, i) => Math.round(v + (b[i] - v) * t));
// [ol, dk, sh, base, lt, hi]
const ST = pal('#1a1e25', '#2a2f3a', '#474c57', '#6b7385', '#8f99ad', '#b4bccb'); // ship steel
const BL = pal('#10161d', '#2a3d4f', '#3a536a', '#5f86a8', '#9fbfda', '#c6e9f2'); // ship blue
const CU = pal('#2e1608', '#5c3218', '#8a5228', '#b9783a', '#e3a656', '#f2c886'); // copper
const CER = pal('#262119', '#5a5244', '#8e8470', '#c4b99e', '#ebe2c8', '#f6f0de'); // porcelain
const AL = [[20, 29, 36], [38, 53, 62], [56, 76, 87], [78, 103, 114], [110, 138, 147], [166, 194, 198]]; // the makers' alloy
const VG = pal('#07060c', '#151027', '#241c44', '#372c66', '#5848a0', '#a090e0'); // void glass
const WD = pal('#22140d', '#3e271a', '#5c3a25', '#7c5234', '#a0704a', '#c08c5e'); // salvaged wood
const RB = pal('#120d0c', '#2a1d1b', '#3e2a26', '#553a33', '#6e4d44', '#8a6458'); // leather / rubber
const VC = { core: hx('#fdfaff'), hi: hx('#e4d6ff'), mid: hx('#b493ff'), lo: hx('#7552e0'), dk: hx('#2e1d78') };
const LIGHT = [0.74, 0.62, 1]; // RelicLooks.Light
const OL = { st: ST[0], bl: BL[0], cu: CU[0], cer: CER[0], al: AL[0], vg: VG[0], wd: WD[0], rb: RB[0], gl: VG[0] };

class Spr {
  constructor(w, h) { this.w = w; this.h = h; this.c = new Array(w * h).fill(null); this.m = new Array(w * h).fill(null); this.l = new Array(w * h).fill(null); this.lk = new Float32Array(w * h); }
  in(x, y) { return x >= 0 && y >= 0 && x < this.w && y < this.h; }
  set(x, y, col, mat = 'st') { x = Math.round(x); y = Math.round(y); if (!this.in(x, y) || !col) return; const i = y * this.w + x; this.c[i] = col; this.m[i] = mat; }
  // a pixel of the shard alight: its dark glass in the body, its light over it, unlit
  lit(x, y, col, k = 1, under = VG[2]) { x = Math.round(x); y = Math.round(y); if (!this.in(x, y)) return; const i = y * this.w + x; this.c[i] = under; this.m[i] = 'gl'; this.l[i] = col; this.lk[i] = k; }
  has(x, y) { return this.in(x, y) && this.c[y * this.w + x] !== null; }
  mat(x, y) { return this.in(x, y) ? this.m[y * this.w + x] : null; }
  outline(only) {
    const add = [];
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      if (this.has(x, y)) continue;
      for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) { const m = this.mat(x + dx, y + dy); if (m && m !== 'ol' && OL[m] && (!only || only.includes(m))) { add.push([x, y, OL[m]]); break; } }
    }
    for (const [x, y, c] of add) this.set(x, y, c, 'ol');
  }
  png(layer = 'c') {
    const im = P.create(this.w, this.h);
    for (let i = 0; i < this.w * this.h; i++) { const c = this[layer][i]; if (!c) continue; const o = i * 4; im.data[o] = c[0]; im.data[o + 1] = c[1]; im.data[o + 2] = c[2]; im.data[o + 3] = layer === 'l' ? Math.round(255 * Math.min(1, this.lk[i])) : 255; }
    return im;
  }
}
// a box lit from the upper left: top row lt, left column base+, bottom row dk, right column sh
function box(s, x, y, w, h, p, mat, o = {}) {
  for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) {
    let c = p[3];
    if (j === 0 && !o.notop) c = o.hi ? p[5] : p[4]; else if (j === h - 1 && h > 2) c = p[1]; else if (i === 0 && w > 2) c = p[4]; else if (i === w - 1 && w > 2) c = p[2];
    if (o.flat) c = j === 0 && !o.notop ? p[4] : p[3];
    s.set(x + i, y + j, c, mat);
  }
}
function fill(s, x, y, w, h, c, mat) { for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) s.set(x + i, y + j, c, mat); }
function hline(s, x0, x1, y, c, mat) { for (let x = x0; x <= x1; x++) s.set(x, y, c, mat); }
function vline(s, x, y0, y1, c, mat) { for (let y = y0; y <= y1; y++) s.set(x, y, c, mat); }
function line(x0, y0, x1, y1, f) {
  const dx = Math.abs(x1 - x0), dy = -Math.abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1; let e = dx + dy, k = 0;
  for (;;) { f(x0, y0, k++); if (x0 === x1 && y0 === y1) break; const e2 = 2 * e; if (e2 >= dy) { e += dy; x0 += sx; } if (e2 <= dx) { e += dx; y0 += sy; } }
}
const rivet = (s, x, y, p, mat) => { s.set(x, y, p[5] || p[4], mat); s.set(x + 1, y + 1, p[1], mat); };
// a stack of porcelain discs from y down, n discs, w wide, centred on cx
function insulator(s, cx, y, n, w = 5) {
  for (let k = 0; k < n; k++) { const yy = y + k * 2; for (let i = 0; i < w; i++) s.set(cx - (w >> 1) + i, yy, i === 0 ? CER[4] : i === w - 1 ? CER[2] : CER[3], 'cer'); for (let i = 1; i < w - 1; i++) s.set(cx - (w >> 1) + i, yy + 1, i === 1 ? CER[2] : CER[1], 'cer'); }
}
// a chip of the sky shard: black glass, lit side left, a white crack up its middle; pts its outline from the bottom-left round
function chip(s, pts, crack, o = {}) {
  const xs = pts.map((p) => p[0]), ys = pts.map((p) => p[1]);
  const inP = (x, y) => { let ins = false; for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) { const [xi, yi] = pts[i], [xj, yj] = pts[j]; if ((yi > y) !== (yj > y) && x < ((xj - xi) * (y - yi)) / (yj - yi) + xi) ins = !ins; } return ins; };
  const mid = (Math.min(...xs) + Math.max(...xs)) / 2;
  for (let y = Math.floor(Math.min(...ys)); y <= Math.ceil(Math.max(...ys)); y++) for (let x = Math.floor(Math.min(...xs)); x <= Math.ceil(Math.max(...xs)); x++) {
    if (!inP(x + 0.5, y + 0.5)) continue;
    const edgeL = !inP(x - 0.5, y + 0.5), edgeR = !inP(x + 1.5, y + 0.5);
    s.set(x, y, x + 0.5 < mid ? (edgeL ? VG[4] : VG[3]) : edgeR ? VG[1] : VG[2], 'vg');
  }
  if (o.sheen) for (const [x, y] of o.sheen) s.set(x, y, VG[5], 'vg');
  for (const seg of crack) for (let i = 0; i < seg.length - 1; i++) line(seg[i][0], seg[i][1], seg[i + 1][0], seg[i + 1][1], (x, y) => {
    for (const [dx, dy] of [[1, 0], [-1, 0]]) if (s.mat(x + dx, y + dy) === 'vg' && !s.l[(y + dy) * s.w + x + dx]) s.lit(x + dx, y + dy, mix(VC.lo, VC.dk, 0.2), 0.5, s.c[(y + dy) * s.w + x + dx]);
    s.lit(x, y, VC.hi, 1, VG[3]);
  });
  for (const seg of crack) for (const [x, y] of seg.slice(1, -1)) s.lit(x, y, VC.core, 1, VG[3]);
  if (o.tip) s.lit(o.tip[0], o.tip[1], VC.core, 1, VG[4]);
}

// ---------------------------------------------------------------- the six benches ----------------------------------------------------------------
// Each draws into a 32x32 picture whose bottom 16 rows are its two cells; what rises above reaches into the cells over it,
// drawn only. tool: where the drill's picture's top-left goes; src: where its arcs leave from; light: its own glow.
const W = 32, H = 32;
const ART = path.join(__dirname, '..', '..', '..', 'game', 'art') + '/';
const D = [];

// 1 -- a salvaged ship bench, a copper vise on it with a chip of the shard set upright in its fixed jaw.
D.push({
  id: 'vise', name: 'Тиски с осколком', short: 'корабельный верстак, медные тиски, осколок в губке',
  what: 'Верстак из корабельной стали, как ножки сборщика: столешница с заклёпками, ящик и полка снизу. На столешнице медные тиски: дрель стоит на рукояти, губки держат её за брюхо. В неподвижную губку вставлен острый скол осколка: он тлеет белыми трещинами и бьёт дугой, когда модуль входит в инструмент. Самый «наш» вариант: корабельная вещь, к которой прикрутили находку.',
  tool: [0, 4],
  draw(s, f) {
    box(s, 2, 22, 3, 8, ST, 'st'); box(s, 27, 22, 3, 8, ST, 'st');
    box(s, 1, 30, 5, 2, ST, 'st'); box(s, 26, 30, 5, 2, ST, 'st');
    box(s, 5, 26, 22, 2, [ST[0], ST[1], ST[1], ST[2], ST[3], ST[4]], 'st');
    box(s, 7, 22, 11, 4, [ST[0], ST[1], ST[1], ST[2], ST[3], ST[4]], 'st'); hline(s, 11, 13, 23, ST[4], 'st'); hline(s, 11, 13, 24, ST[1], 'st');
    box(s, 0, 18, 32, 4, ST, 'st', { hi: true }); hline(s, 0, 31, 21, ST[1], 'st');
    for (let x = 3; x < 31; x += 6) s.set(x, 20, ST[1], 'st');
    // a scorched patch on the worktop where the arcs land
    for (const x of [8, 9, 10, 11, 12]) s.set(x, 18, mix(ST[4], VG[2], 0.5), 'st');
    // the vise: its flange bolted down, the fixed jaw block with the chip set into its face, its screw and T-handle to the right
    box(s, 11, 16, 13, 2, CU, 'cu'); s.set(12, 16, CU[5], 'cu'); s.set(22, 16, CU[5], 'cu');
    box(s, 13, 11, 9, 5, CU, 'cu');
    fill(s, 14, 12, 7, 4, CU[1], 'cu');
    chip(s, [[14, 14], [16, 12], [19, 12], [21, 14], [19, 16], [16, 16]], [[[15, 14], [17, 13], [18, 15], [20, 14]]], { sheen: [[16, 12]] });
    hline(s, 22, 27, 13, ST[4], 'st'); hline(s, 22, 27, 14, ST[2], 'st');
    vline(s, 28, 9, 17, CU[4], 'cu'); vline(s, 29, 9, 17, CU[2], 'cu'); box(s, 27, 8, 4, 2, CU, 'cu'); box(s, 27, 17, 4, 2, CU, 'cu');
    s.outline();
    // the front: the jaws' teeth over the drill's belly
    if (f) { for (const x of [13, 15, 17, 19, 21]) { f.set(x, 10, CU[4], 'cu'); f.set(x, 11, CU[3], 'cu'); } for (const x of [14, 16, 18, 20]) f.set(x, 11, CU[2], 'cu'); f.outline(); }
  },
  src: [[17, 13], [20, 14], [15, 14]], core: [17.5, 14], glowR: 3.5, strength: 0.75,
});

// 2 -- an ancient arc makers' anvil-altar of their alloy, rebuilt aboard with ship plates; the shard set in its waist.
D.push({
  id: 'anvil', name: 'Наковальня-алтарь', short: 'древняя наковальня из их сплава, осколок в поясе',
  what: 'Наковальня творцов дуги из бирюзово-серого сплава руин: широкая грань с рогом налево, узкий пояс, тяжёлое основание на фарфоровых изоляторах. Её починили на борту: справа к основанию привинчена корабельная пластина. В поясе за медными обручами стоит осколок, его трещины светят сквозь щель. Инструмент кладут на грань; в момент вставки дуга бьёт из пояса вверх через металл.',
  tool: [1, 7],
  draw(s, f) {
    // horn and face
    for (let i = 0; i < 5; i++) { const t = Math.round(i / 2); fill(s, i, 16 + t, 1, 3 - t, AL[3], 'al'); s.set(i, 16 + t, AL[4], 'al'); }
    box(s, 4, 16, 26, 3, AL, 'al', { hi: true }); hline(s, 4, 29, 18, AL[2], 'al');
    // the face's step to the waist
    box(s, 8, 19, 17, 2, AL, 'al');
    box(s, 11, 21, 11, 5, AL, 'al');
    box(s, 7, 26, 19, 3, AL, 'al', { hi: false });
    // the shard in its slot, behind two copper bands
    fill(s, 14, 21, 5, 5, VG[1], 'vg');
    chip(s, [[14, 26], [14, 22], [16, 20], [18, 22], [18, 26]], [[[16, 25], [17, 23], [16, 21]]], { tip: [16, 20] });
    hline(s, 11, 21, 22, CU[4], 'cu'); hline(s, 11, 21, 25, CU[3], 'cu');
    for (const x of [11, 21]) { s.set(x, 22, CU[2], 'cu'); s.set(x, 25, CU[1], 'cu'); }
    // the ship's plate bolted over a broken corner of the base, and the porcelain feet
    box(s, 21, 25, 8, 4, ST, 'st'); rivet(s, 22, 26, ST, 'st'); rivet(s, 26, 26, ST, 'st');
    insulator(s, 10, 29, 1, 5); s.set(8, 31, CER[2], 'cer'); fill(s, 8, 31, 5, 1, CER[2], 'cer');
    insulator(s, 23, 29, 1, 5); fill(s, 21, 31, 5, 1, CER[2], 'cer');
    // the face's worn spots
    s.set(12, 16, AL[5], 'al'); s.set(20, 16, AL[5], 'al'); s.set(26, 17, AL[1], 'al');
    s.outline();
    if (f) { /* nothing grips it: the tool just lies on the face */ }
  },
  src: [[16, 20], [17, 23], [12, 16], [27, 16]], core: [16.5, 23], glowR: 4, strength: 0.8,
});

// 3 -- a low frame with two porcelain insulator posts, copper horns on them: an arc arches between them over the tool.
D.push({
  id: 'posts', name: 'Рама с изоляторами', short: 'два фарфоровых столба, дуга между рогами над инструментом',
  what: 'Низкая стальная рама с решёткой, медная шина поверх. По краям два столба из фарфоровых изоляторов, как у колыбели осколка, на них медные рога, разведённые внутрь. Скол осколка вставлен в окно рамы спереди, от него по столбам вверх идут медные жилы. Инструмент кладут на медные вилки между столбами. Без дела между рогами изредка проскакивает тонкая дуга; при вставке она падает вниз, в инструмент.',
  tool: [0, 9],
  draw(s, f) {
    box(s, 0, 24, 32, 8, ST, 'st', { hi: true });
    for (let x = 3; x < 12; x += 2) vline(s, x, 26, 29, ST[1], 'st');
    for (let x = 21; x < 30; x += 2) vline(s, x, 26, 29, ST[1], 'st');
    hline(s, 0, 31, 23, CU[4], 'cu'); hline(s, 0, 31, 24, CU[2], 'cu');
    // the chip in its window
    fill(s, 13, 25, 7, 5, VG[0], 'vg');
    chip(s, [[14, 30], [14, 27], [16, 25], [18, 27], [18, 30]], [[[16, 29], [17, 27], [16, 26]]]);
    hline(s, 13, 19, 30, CU[3], 'cu');
    // the forks the tool lies on
    for (const x of [10, 21]) { vline(s, x, 19, 22, CU[3], 'cu'); s.set(x - 1, 19, CU[4], 'cu'); s.set(x + 1, 19, CU[2], 'cu'); }
    // the posts and their horns
    for (const cx of [3, 28]) { insulator(s, cx, 7, 8, 5); vline(s, cx, 6, 6, CU[3], 'cu'); }
    line(3, 6, 5, 2, (x, y) => s.set(x, y, CU[4], 'cu')); line(5, 2, 8, 1, (x, y) => s.set(x, y, CU[3], 'cu'));
    line(28, 6, 26, 2, (x, y) => s.set(x, y, CU[3], 'cu')); line(26, 2, 23, 1, (x, y) => s.set(x, y, CU[2], 'cu'));
    s.outline();
  },
  src: [[8, 1], [23, 1], [16, 25]], core: [16, 27], glowR: 3, strength: 0.6, between: [[8, 1], [23, 1]],
});

// 4 -- a jeweller's bench: a lens on a jointed arm and a lamp of a shard chip on a goose-neck over the work.
D.push({
  id: 'jeweller', name: 'Стол ювелира', short: 'лупа на шарнирах и лампа-осколок на гусиной шее',
  what: 'Тонкая работа: деревянный стол творцов с кожаным фартуком снизу, на нём упор для инструмента. Справа медная гусиная шея, на конце висит скол осколка в воротнике, как в нише дома: лампа, которая светит фиолетово-белым прямо на работу. Слева лупа на шарнирной руке, её стекло перед инструментом. При вставке из острия лампы в инструмент падает одна тонкая точная дуга.',
  tool: [0, 3],
  draw(s, f) {
    // legs, the leather apron under the top, the top with its lip
    box(s, 1, 22, 3, 9, WD, 'wd'); box(s, 28, 22, 3, 9, WD, 'wd'); fill(s, 0, 31, 5, 1, WD[2], 'wd'); fill(s, 27, 31, 5, 1, WD[2], 'wd');
    for (let x = 4; x < 28; x++) { const sag = Math.round(2.4 * Math.sin(Math.PI * (x - 4) / 24)); s.set(x, 23 + sag, RB[4], 'rb'); s.set(x, 24 + sag, RB[2], 'rb'); }
    box(s, 0, 20, 32, 3, WD, 'wd', { hi: true }); hline(s, 0, 31, 22, WD[1], 'wd');
    for (let x = 4; x < 30; x += 7) s.set(x, 21, WD[2], 'wd');
    // the rest the tool stands on: a block with a felt top
    box(s, 13, 17, 8, 3, WD, 'wd'); hline(s, 13, 20, 17, RB[4], 'rb');
    // the goose-neck lamp on the right, its chip hanging over the drill's nose
    box(s, 27, 18, 4, 2, CU, 'cu');
    const neck = [[29, 18], [29, 12], [29, 6], [28, 3], [26, 1], [24, 1]];
    for (let i = 0; i < neck.length - 1; i++) line(neck[i][0], neck[i][1], neck[i + 1][0], neck[i + 1][1], (x, y, k) => s.set(x, y, k % 2 ? CU[4] : CU[3], 'cu'));
    box(s, 20, 0, 6, 2, CU, 'cu', { hi: true });
    chip(s, [[20, 2], [25, 2], [23, 8], [22, 8]], [[[22, 2], [23, 4], [22, 6], [22, 8]]], { tip: [22, 8], sheen: [[20, 2]] });
    // the lens's arm from the left rear
    box(s, 0, 18, 5, 2, ST, 'st');
    line(2, 18, 2, 12, (x, y) => s.set(x, y, ST[4], 'st')); line(2, 12, 5, 9, (x, y) => s.set(x, y, ST[3], 'st'));
    s.set(2, 12, ST[5], 'st');
    s.outline();
    // the lens itself, in front of the tool's stock: a brass ring, the glass a faint sheen
    const lens = f || s;
    const cx = 8.5, cy = 9.5, r = 4;
    for (let y = 3; y < 16; y++) for (let x = 2; x < 16; x++) {
      const d = Math.hypot(x + 0.5 - cx, y + 0.5 - cy);
      if (d <= r && d > r - 1.2) lens.set(x, y, (x + 0.5 - cx) + (y + 0.5 - cy) < 0 ? CU[4] : CU[2], 'cu');
    }
    if (f) { f.set(7, 7, [230, 240, 255], 'x'); f.set(6, 8, [196, 214, 240], 'x'); f.outline(['cu']); }
  },
  glass: { cx: 8.5, cy: 9.5, r: 2.8 },
  src: [[22, 8], [23, 5]], core: [22.5, 5], glowR: 4, strength: 1, lamp: [22.5, 9],
});

// 5 -- two copper hoops seen nearly edge-on round the tool's stock, a shard the keystone of the taller: the arc winds into it.
D.push({
  id: 'hoops', name: 'Кольца-обмотка', short: 'медные кольца вокруг ложа инструмента, осколок-замок',
  what: 'Стальной козёл с медной шиной, над ним два медных кольца, обмотанных витками, стоят на фарфоровых ножках. Инструмент продевают сквозь кольца ложем: задняя половина колец за ним, передняя перед ним, будто его наматывают. Замковый камень большого кольца это скол осколка. При вставке дуга бежит по кольцам и стекает в ложе инструмента, туда, где ляжет катушка.',
  tool: [0, 7],
  draw(s, f) {
    // the trestle
    box(s, 0, 21, 32, 3, ST, 'st', { hi: true }); hline(s, 0, 31, 23, ST[1], 'st');
    for (const [x0, x1] of [[3, 7], [8, 4], [24, 28], [29, 25]]) line(x0, 24, x1, 31, (x, y) => s.set(x, y, ST[3], 'st'));
    hline(s, 4, 7, 28, ST[2], 'st'); hline(s, 24, 28, 28, ST[2], 'st');
    hline(s, 1, 30, 20, CU[3], 'cu');
    // two hoops round the motor, seen nearly edge-on: their back halves here, front halves in the front layer; porcelain feet
    const cy = 11.5, ry = 6.5, rx = 1.8, H0 = [15.5, 20.5];
    for (const cx of H0) {
      for (let y = Math.floor(cy - ry - 1); y <= cy + ry + 1; y++) for (let x = Math.floor(cx - rx - 1); x <= cx + rx + 1; x++) {
        const nx = (x + 0.5 - cx) / rx, ny = (y + 0.5 - cy) / ry, d = nx * nx + ny * ny;
        if (d > 1.05 || d < 0.3) continue;
        const front = x + 0.5 >= cx, col = front ? (y % 2 ? CU[4] : CU[3]) : (y % 2 ? CU[2] : CU[1]);
        if (front && f) f.set(x, y, col, 'hp'); else s.set(x, y, col, 'hp');
      }
      const fx = Math.floor(cx); insulator(s, fx, 18, 1, 3);
    }
    // the bar over them, and the keystone shard on it
    hline(s, 14, 22, 4, CU[4], 'cu'); hline(s, 14, 22, 5, CU[2], 'cu');
    chip(s, [[16, 4], [16, 1], [18, -0.5], [20, 1], [20, 4]], [[[17, 4], [18, 2], [18, 0]]], { tip: [18, 0], sheen: [[16, 1]] });
    box(s, 15, 3, 7, 2, CU, 'cu', { hi: true });
    // a stand for its grip at the rear
    box(s, 2, 18, 8, 2, ST, 'st');
    s.outline();
  },
  src: [[18, 0], [17, 2], [15, 6], [21, 6]], core: [18, 2], glowR: 3, strength: 0.65, ring: true,
});

// 6 -- the shard itself split into a slab: the worktop is black glass with white cracks, on ship trestle legs, copper at its ends.
D.push({
  id: 'slab', name: 'Стеклянная плита', short: 'столешница из расколотого осколка на стальных козлах',
  what: 'Сам осколок, расколотый вдоль в плиту: столешница из чёрного стекла с белыми горящими трещинами, по краям медные оковки, снизу стальные козлы с раскосом. Инструмент лежит прямо на стекле. Без дела трещины тихо дышат светом; при вставке они вспыхивают, и дуги ползут из трещин вверх, в инструмент. Самый яркий и самый «чужой» вариант.',
  tool: [1, 3],
  draw(s, f) {
    // trestle legs and the cross brace
    for (const [x0, x1] of [[4, 2], [7, 9], [24, 22], [27, 29]]) line(x0, 21, x1, 30, (x, y) => s.set(x, y, ST[3], 'st'));
    for (const x of [1, 2, 3, 8, 9, 10, 21, 22, 23, 28, 29, 30]) s.set(x, 31, ST[2], 'st');
    line(7, 26, 24, 26, (x, y) => s.set(x, y, ST[2], 'st')); hline(s, 8, 23, 25, ST[4], 'st');
    box(s, 3, 20, 26, 2, ST, 'st');
    // the slab
    for (let y = 16; y < 20; y++) for (let x = 0; x < 32; x++) s.set(x, y, y === 16 ? VG[4] : y === 19 ? VG[1] : (x < 16 ? VG[3] : VG[2]), 'vg');
    for (const x of [5, 6, 13, 27]) s.set(x, 16, VG[5], 'vg');
    const cracks = [[[3, 19], [6, 17], [9, 18], [13, 16]], [[9, 18], [11, 19]], [[18, 19], [20, 17], [24, 18], [27, 16]], [[20, 17], [21, 16]]];
    for (const seg of cracks) for (let i = 0; i < seg.length - 1; i++) line(seg[i][0], seg[i][1], seg[i + 1][0], seg[i + 1][1], (x, y) => s.lit(x, y, VC.hi, 1, VG[3]));
    for (const [x, y] of [[6, 17], [9, 18], [20, 17], [24, 18]]) s.lit(x, y, VC.core, 1, VG[4]);
    // copper shoes on its ends
    box(s, 0, 15, 3, 6, CU, 'cu'); box(s, 29, 15, 3, 6, CU, 'cu');
    s.outline();
  },
  src: [[6, 17], [9, 18], [13, 16], [20, 17], [24, 18], [27, 16]], core: [16, 18], glowR: 6, strength: 0.9, wide: true,
});

// ---------------------------------------------------------------- icons (16 px, item style) ----------------------------------------------------------------
const ICOL = {
  o: hx('#100c18'), S: ST[4], s: ST[3], t: ST[2], d: ST[1], C: CU[4], c: CU[3], k: CU[2], q: CU[1], W: VC.core, H: VC.hi, M: VC.mid, L: VC.lo,
  1: VG[1], 2: VG[2], 3: VG[3], 4: VG[4], A: AL[4], a: AL[3], b: AL[2], P: CER[4], p: CER[3], r: CER[2], w: WD[4], v: WD[3], u: WD[2], g: [210, 225, 245],
};
const ICONS = {
 "vise": [
  "................",
  "................",
  ".........M......",
  "................",
  "....ooooooo..oo.",
  "...oCCCCCCCo.oCo",
  "...oc3HWH3co.oco",
  "...okk2M2kkoooko",
  "..oqqqqqqqqqoSko",
  "oooooooooooooooo",
  "oSSSSSSSSSSSSSSo",
  "osssssssssssssto",
  "oddddddddddddddo",
  ".oto.otttto.oto.",
  ".oto.oooooo.oto.",
  "ooooo......ooooo"
 ],
 "anvil": [
  "................",
  "................",
  "......M.........",
  "................",
  "ooooooooooooooo.",
  "oAAAAAAAAAAAAAAo",
  ".ooaaaaaaaaaaaao",
  "...oobbbbbbbboo.",
  "....oc3HW3co....",
  "....ob2H2bbo....",
  "....oc2W2cco....",
  "...oaab2baaao...",
  "..obbbbbbbtttto.",
  "..oooooooooooo..",
  "...oPo....oPo...",
  "..ooooo..ooooo.."
 ],
 "posts": [
  "..oCo......oCo..",
  "...oCo....oCo...",
  "...oo..MH..oo...",
  "..oPo.H..W.oPo..",
  "..oro......oro..",
  "..oPo......oPo..",
  "..oro......oro..",
  "..oPo......oPo..",
  "..oro......oro..",
  "..oPo.o..o.oPo..",
  "ooooooCooCoooooo",
  "oCCCCCCCCCCCCCCo",
  "oSSSSo3HW3oSSSSo",
  "osdsdo2H22osdsdo",
  "odddddkkkkdddddo",
  "oooooooooooooooo"
 ],
 "jeweller": [
  "................",
  "......ooooooo...",
  ".....oCCCCCCCo..",
  ".ooo..oo4oo.oCo.",
  "oCCko..oHo..oco.",
  "oCgko..oWo..oco.",
  "okkko...M...oco.",
  ".ooo........oco.",
  "..o..oooooo.oco.",
  "..o..ovvvvo.oco.",
  "oooooooooooooooo",
  "owwwwwwwwwwwwwwo",
  "ouuuuuuuuuuuuuuo",
  ".ovo........ovo.",
  ".ovo........ovo.",
  "ooooo......ooooo"
 ],
 "hoops": [
  "................",
  "......o.........",
  ".....o4o........",
  ".....oHo........",
  "..ooooWoooo.....",
  "..oCCCCCCCCo....",
  "..okCooookCo....",
  "..oCko..oCko....",
  "..okCo..okCo....",
  "..oCko..oCko....",
  "..ookoo.ookoo...",
  "...oPo...oPo....",
  "oooooooooooooooo",
  "oCCCCCCCCCCCCCCo",
  "oSSSSSSSSSSSSSSo",
  "ooso........osoo"
 ],
 "slab": [
  "................",
  "................",
  "......M.........",
  "................",
  "....H.....M.....",
  "................",
  "oooooooooooooooo",
  "oCo4444444444oCo",
  "oco3H33W3H333oco",
  "oko2W2HH2WH22oko",
  "oqo1111111111oqo",
  "ooSSSSSSSSSSSSoo",
  "..oso.oooo.oso..",
  "..osoosssso.oso.",
  ".oso........oso.",
  "ooso........osoo"
 ]
};
function icon(id) {
  const rows = ICONS[id], im = P.create(16, 16);
  rows.forEach((r, y) => { if (r.length > 16) r = r.slice(0, 16); for (let x = 0; x < r.length; x++) { const ch = r[x]; if (ch === '.') continue; const c = ICOL[ch]; if (!c) throw new Error(id + ' bad ' + ch); const o = (y * 16 + x) * 4; im.data[o] = c[0]; im.data[o + 1] = c[1]; im.data[o + 2] = c[2]; im.data[o + 3] = 255; } });
  return im;
}

// the glow round its alight pixels, a cell's margin each side (ArcLampLooks' Glow), in the shard's light
function glowOf(s, M, r, k) {
  const im = P.create(s.w + 2 * M, s.h + 2 * M), acc = new Float32Array(im.w * im.h);
  for (let y = 0; y < s.h; y++) for (let x = 0; x < s.w; x++) { const i = y * s.w + x; if (!s.l[i]) continue; const w0 = s.lk[i];
    for (let v = -M; v <= M; v++) for (let u = -M; u <= M; u++) { const X = x + M + u, Y = y + M + v; if (X < 0 || Y < 0 || X >= im.w || Y >= im.h) continue; const d2 = u * u + v * v; acc[Y * im.w + X] += w0 * Math.exp(-d2 / (2 * r * r)); } }
  let mx = 0; for (const a of acc) mx = Math.max(mx, a);
  for (let i = 0; i < acc.length; i++) { const a = Math.min(1, (acc[i] / mx) * k), o = i * 4; im.data[o] = 180; im.data[o + 1] = 150; im.data[o + 2] = 255; im.data[o + 3] = Math.round(255 * a); }
  return im;
}

function paint() {
  fs.mkdirSync(path.join(OUT), { recursive: true });
  const manifest = { about: 'Arc bench candidates (ADR 0098 slice 3), from paint.js. Pictures 32x32, their two cells the bottom 16 rows (box x 0, y 16, 32 by 16); glow 64x64, a cell of margin round the picture. tool: where the drill held look\'s picture (hero_drill, 33x15) puts its top-left in the picture, its coil mount (4, 4) then lands at tool + (4, 4).', box: { x: 0, y: 16, w: 32, h: 16 }, light: LIGHT, designs: [] };
  for (const d of D) {
    const s = new Spr(W, H), f = new Spr(W, H);
    d.draw(s, f);
    const dir = path.join(OUT, d.id); fs.mkdirSync(path.join(dir, 'items'), { recursive: true });
    P.encode(s.png('c'), path.join(dir, 'machine_arcBench.png'));
    P.encode(s.png('l'), path.join(dir, 'arcBench_alight.png'));
    P.encode(f.png('c'), path.join(dir, 'arcBench_front.png'));
    P.encode(glowOf(s, 16, 4, 0.55), path.join(dir, 'arcBench_glow.png'));
    P.encode(glowOf(s, 16, 7, 1), path.join(dir, 'arcBench_fit_glow.png'));
    P.encode(icon(d.id), path.join(dir, 'items', 'arcBench.png'));
    const [tx, ty] = d.tool;
    // previews at 1x, as the game would lay them: body, its shard alight, the tool, the front over it
    fs.mkdirSync(path.join(dir, 'preview'), { recursive: true });
    const drill = P.decode(ART + 'hero_drill.png'), coil = P.decode(ART + 'module_arc_coil.png');
    for (const st of ['empty', 'drill', 'fitted']) {
      const im = P.create(W, H), put = (src, ox, oy) => { for (let y = 0; y < src.h; y++) for (let x = 0; x < src.w; x++) { const X = ox + x, Y = oy + y; if (X < 0 || Y < 0 || X >= W || Y >= H) continue; const a = src.data[(y * src.w + x) * 4 + 3] / 255; if (!a) continue; for (let c = 0; c < 3; c++) im.data[(Y * W + X) * 4 + c] = Math.round(im.data[(Y * W + X) * 4 + c] * (1 - a) + src.data[(y * src.w + x) * 4 + c] * a); im.data[(Y * W + X) * 4 + 3] = Math.max(im.data[(Y * W + X) * 4 + 3], Math.round(a * 255)); } };
      put(s.png('c'), 0, 0); put(s.png('l'), 0, 0);
      if (st !== 'empty') { put(drill, tx, ty); if (st === 'fitted') put(coil, tx + 4, ty + 4); }
      put(f.png('c'), 0, 0);
      P.encode(im, path.join(dir, 'preview', st + '.png'));
    }
    manifest.designs.push({
      id: d.id, name: d.name, short: d.short, what: d.what,
      files: { body: `${d.id}/machine_arcBench.png`, alight: `${d.id}/arcBench_alight.png`, front: `${d.id}/arcBench_front.png`, glow: `${d.id}/arcBench_glow.png`, fitGlow: `${d.id}/arcBench_fit_glow.png`, icon: `${d.id}/items/arcBench.png` },
      tool: { x: tx, y: ty }, coil: { x: tx + 3, y: ty + 3.5, x2: tx + 11, y2: ty + 6.5 },
      lights: [{ x: d.core[0], y: d.core[1], colour: LIGHT, reach: d.glowR, strength: d.strength }],
      src: d.src, between: d.between || null, lamp: d.lamp || null, ring: !!d.ring, wide: !!d.wide, glass: d.glass || null,
      emitters: {
        idle: (d.between ? [{ kind: 'Arc', x: d.between[0][0], y: d.between[0][1], x2: d.between[1][0], y2: d.between[1][1], rate: 0.02, life: 4, width: 0.6 }] : [])
          .concat(d.src.slice(0, 1).map(([x, y]) => ({ kind: 'Arc', x, y, toAir: true, reach: 4, rate: 0.015, life: 3, width: 0.5 }))),
        fit: d.src.map(([x, y]) => ({ kind: 'Arc', x, y, x2: tx + 7, y2: ty + 5, life: 4, width: 1 })),
      },
    });
  }
  fs.writeFileSync(path.join(OUT, 'manifest.json'), JSON.stringify(manifest, null, 1));
  return manifest;
}
module.exports = { paint, D, OUT };
if (require.main === module) { const m = paint(); console.log('painted', m.designs.map((d) => d.id).join(' ')); }

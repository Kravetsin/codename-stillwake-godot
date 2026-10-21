// The grow capsule, 32x48 (2x3 cells), in three looks. Each look gives:
//   base  — machine_growCapsule.png: the machine at rest, lamp dark (outlined, lit by the game)
//   work  — growCapsule_work_0.png: only the lamp's lit pixels (and its board LED), drawn unlit
//   plant — growCapsule_plant_0..3.png: full-size overlays, cut to the glass, glints kept over them
//   light — where the Glow sits, in pixels of the picture (StationView's Lights)
// Pixels tagged 'over' are the glass's front (glints, straps): plant frames repeat them on top.
const { Pic } = require('./lib.js');
const { MACHINE_OUTLINE } = require('./pal.js');
const { plant } = require('./plants.js');

const K0 = '#10161d', K1 = '#22262c', S2 = '#474c57', S3 = '#5b6170', S4 = '#6b7385', S5 = '#7f848f', S6 = '#8f99ad', S7 = '#9aa0ab', S8 = '#b4bcc8';
const CREAM = '#d8d2c4', CREAM2 = '#a8a092';
const G1 = '#22303c', G2 = '#2a3d4f', G3 = '#3a536a', G4 = '#5f86a8', G5 = '#9fd8ff', G0 = '#1c2832';
const E0 = '#2e1c12', E1 = '#4a2e1e', E2 = '#6a4430', E3 = '#8a5a3a', E4 = '#b07a50';
const PEB = '#aaa69c', PEB2 = '#5f5c55';
const GR0 = '#1c4626', GR1 = '#2c6a3a', GR2 = '#3f8f4f';
const LOFF0 = '#2a1f29', LOFF1 = '#3a2a36', LOFF2 = '#5b3a52';
const LON0 = '#c94a9e', LON1 = '#ff7ad1', LON2 = '#ffd0ef', LON3 = '#fff4fb';
const RUST0 = '#6a4430', RUST1 = '#8a5a3a', RUST2 = '#a86a40';
const WOOD = { a: '#62391a', b: '#8f5b2d', c: '#bd8449', d: '#e2b178' };
const TEAL0 = '#1d4f57', TEAL1 = '#2a7f88', TEAL2 = '#4fb3b5';
const CER = ['#5b6170', '#7f848f', '#9aa0ab', '#b8c0cc', '#dde3ea'];

// soil: a lumpy top of clods lit on their upper left, crumbly darker earth below, the bottom row in shadow
const h2 = (x, y, s) => { let h = (x * 374761393 + y * 668265263 + s * 2246822519) >>> 0; h = ((h ^ (h >>> 13)) * 1274126177) >>> 0; return ((h ^ (h >>> 16)) >>> 0) / 4294967296; };
function soil(p, x0, x1, top, bottom, seed) {
  // the surface, column by column: clods one row up, hollows as they fall
  const lift = []; for (let x = x0; x <= x1; x++) lift.push(h2(x >> 1, 0, seed) < 0.38 ? 1 : 0);
  for (let x = x0; x <= x1; x++) {
    const i = x - x0, t = top - lift[i];
    for (let y = t; y <= bottom; y++) {
      const r = h2(x, y, seed + 1); let c;
      if (y === t) c = lift[i] && (i === 0 || !lift[i - 1]) ? E4 : r < 0.3 ? E4 : E3;
      else if (y === t + 1) c = lift[i] && (i + 1 >= lift.length || !lift[i + 1]) ? E1 : r < 0.25 ? E3 : E2;
      else if (y === bottom) c = r < 0.3 ? E1 : E0;
      else c = r < 0.28 ? E1 : r > 0.9 ? E3 : E2;
      p.px(x, y, c);
    }
  }
}

// a glass column: frame edges, a lit stripe on the left, the right half darker
function glass(p, x0, x1, y0, y1, { stripe = true } = {}) {
  for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
    let c = x < (x0 + x1) / 2 + 1 ? G2 : G1;
    if (x === x0 || x === x1) c = G3;
    else if (x === x1 - 1) c = G2;
    p.px(x, y, c);
  }
  if (stripe) { p.vl(x0 + 1, y0 + 2, y1 - 2, G5, 'over'); p.vl(x0 + 2, y0 + 3, y1 - 4, G4, 'over'); }
}
function glint(p, pts, bright) { for (const [x, y] of pts) p.px(x, y, G4, 'over'); for (const [x, y] of bright || []) p.px(x, y, G5, 'over'); }
const diag = (x, y, n) => Array.from({ length: n }, (_, i) => [x - i, y + i]);

// ---------- A: the game's steel, cleaned up: a cylinder on a plated base, a domed lamp cap ----------
function lookA() {
  const p = new Pic(32, 48);
  // feet
  for (const fx of [4, 23]) { p.rect(fx, 45, fx + 4, 46, S3); p.hl(fx, fx + 4, 45, S5); p.vl(fx + 4, 45, 46, S2); }
  // base
  p.panel(2, 34, 29, 44, [S2, S4, S6]); p.hl(3, 28, 35, S5); p.hl(3, 28, 43, S3);
  for (const y of [37, 39, 41]) { p.hl(5, 12, y, K1); p.hl(5, 12, y + 1, S5); }
  p.px(4, 36, CREAM); p.px(4, 42, CREAM2); p.px(14, 36, CREAM); p.px(14, 42, CREAM2);
  // the control board, recessed
  p.rect(16, 36, 27, 42, K1); p.hl(16, 27, 36, S2); p.vl(16, 36, 42, S2); p.hl(17, 27, 42, S6); p.vl(27, 37, 42, S6);
  p.rect(18, 38, 23, 40, K0); p.hl(19, 21, 38, GR1); p.hl(19, 22, 39, GR0); p.hl(19, 20, 40, GR1);
  p.px(25, 38, LOFF2); p.px(25, 40, CREAM2); p.px(25, 41, S3);
  // the collar the glass stands in
  p.panel(2, 32, 29, 33, [S3, S5, S7]); p.px(4, 32, CREAM); p.px(27, 32, CREAM);
  // glass and soil
  glass(p, 3, 28, 10, 31);
  soil(p, 4, 27, 28, 31, 3); p.px(10, 30, PEB); p.px(11, 30, PEB2); p.px(21, 31, PEB2);
  p.vl(3, 10, 31, G3); p.vl(28, 10, 31, G3);
  glint(p, [...diag(25, 13, 5), ...diag(26, 20, 3)], [[24, 14], [25, 21]]);
  // cap: ring, dome, knob
  p.panel(2, 8, 29, 9, [S3, S5, S7]); p.px(4, 8, CREAM); p.px(27, 8, CREAM);
  p.hl(9, 22, 4, S7); p.hl(7, 24, 5, S6); p.hl(6, 25, 6, S5); p.hl(5, 26, 7, S4);
  p.px(9, 4, S8); p.px(7, 5, S7); p.px(6, 6, S6); p.px(5, 7, S5);
  p.px(22, 4, S5); p.px(24, 5, S3); p.px(25, 6, S3); p.px(26, 7, S2);
  p.hl(10, 13, 6, S4); p.hl(18, 21, 6, S4);
  p.hl(14, 17, 2, S6); p.rect(13, 3, 18, 3, S4); p.px(13, 3, S6); p.px(18, 3, S2);
  // the lamp under the cap, dark
  p.px(6, 10, S2); p.px(25, 10, S2); p.hl(7, 24, 10, LOFF1); p.hl(10, 21, 10, LOFF2); p.hl(9, 22, 11, LOFF0);
  p.outline(MACHINE_OUTLINE);
  const work = new Pic(32, 48);
  work.hl(7, 24, 10, LON0); work.hl(8, 23, 10, LON1); work.hl(11, 20, 10, LON2); work.hl(14, 17, 10, LON3);
  work.hl(9, 22, 11, LON0); work.hl(12, 19, 11, LON1);
  work.px(25, 38, LON1);
  const soilTop = 28;
  return { base: p, work, light: [16, 11], species: 'pea', at: { x: 15, y: soilTop, mask: (x, y) => x >= 6 && x <= 26 && y >= 13 && y <= soilTop } };
}

// ---------- B: knocked together from scrap, as the furnace is: strapped glass, a planter box, a bolted-on lamp ----------
function lookB() {
  const p = new Pic(32, 48);
  // feet: an angle iron, and a block of wood under the other side
  p.rect(3, 45, 7, 46, S3); p.hl(3, 7, 45, S5); p.vl(3, 45, 46, S6);
  p.rect(22, 45, 28, 46, WOOD.c); p.hl(22, 28, 45, WOOD.d); p.hl(23, 28, 46, WOOD.b); p.px(28, 45, WOOD.b); p.px(25, 46, WOOD.a);
  // base: two plates that do not match
  p.panel(2, 34, 15, 44, [S3, S5, S7]);
  for (const [x, y] of [[4, 36], [13, 36], [4, 42], [13, 42]]) p.px(x, y, CREAM);
  p.hl(5, 11, 39, S4); p.hl(5, 11, 40, S6);
  p.panel(16, 34, 29, 44, [S2, S4, S6]); p.vl(16, 34, 44, K1); p.vl(17, 35, 43, S6);
  for (const [x, y, c] of [[18, 35, RUST1], [19, 35, RUST1], [19, 36, RUST0], [27, 43, RUST1], [28, 42, RUST0], [26, 43, RUST0], [8, 43, RUST1], [9, 43, RUST0], [3, 38, RUST1], [3, 39, RUST0]]) p.px(x, y, c);
  // the control box bolted on: a gauge, a toggle, a bulb
  p.panel(19, 36, 28, 42, [S2, S3, S5]);
  p.rect(20, 37, 24, 41, K1); p.rect(21, 37, 23, 41, CREAM); p.rect(20, 38, 24, 40, CREAM); p.px(21, 37, '#f3eee0'); p.px(20, 38, '#f3eee0');
  p.px(23, 41, CREAM2); p.px(24, 40, CREAM2); p.px(22, 39, K1); p.px(23, 38, '#a8452e'); p.px(24, 37, '#a8452e');
  p.px(26, 37, S7); p.px(26, 38, S6); p.px(26, 39, K1); p.px(27, 39, K1);
  p.px(26, 41, LOFF2);
  // the planter box, planks nailed across
  p.rect(3, 29, 28, 33, WOOD.c); p.hl(3, 28, 29, WOOD.d); p.hl(3, 28, 31, WOOD.a); p.hl(4, 28, 33, WOOD.b); p.vl(28, 29, 33, WOOD.b); p.vl(3, 29, 33, WOOD.d);
  for (const [x, y] of [[5, 30], [26, 30], [5, 32], [26, 32], [15, 30]]) p.px(x, y, S6);
  p.px(11, 32, WOOD.b); p.px(12, 32, WOOD.b); p.px(20, 30, WOOD.b);
  // glass tube above it, soil heaped in the box
  glass(p, 4, 27, 9, 28);
  soil(p, 5, 26, 26, 28, 9); p.px(19, 27, PEB); p.px(20, 27, PEB2);
  p.vl(4, 9, 28, G3); p.vl(27, 9, 28, G3);
  glint(p, [...diag(25, 18, 3)], [[25, 18]]);
  // a crack, taped over
  for (const [x, y] of [[22, 10], [23, 11], [23, 12], [24, 13], [25, 14], [24, 15]]) p.px(x, y, G5, 'over');
  p.rect(22, 12, 25, 13, '#c9bfa6', 'over'); p.hl(22, 25, 13, CREAM2, 'over');
  // iron straps round the glass, riveted, their lugs past its edge
  for (const y of [16, 23]) { p.hl(3, 28, y, S6, 'over'); p.hl(3, 28, y + 1, S3, 'over'); p.px(6, y, CREAM, 'over'); p.px(25, y, CREAM, 'over'); p.px(3, y, S7, 'over'); p.px(28, y + 1, S2, 'over'); }
  // the lid, rusting, and the lamp's shade bolted on it
  p.panel(3, 6, 28, 8, [S2, S4, S6]);
  for (const [x, y, c] of [[7, 7, RUST1], [8, 7, RUST0], [9, 7, RUST1], [8, 8, RUST0], [22, 7, RUST1]]) p.px(x, y, c);
  p.px(5, 7, CREAM); p.px(26, 7, CREAM);
  p.hl(10, 21, 5, S3); p.hl(11, 20, 4, S5); p.hl(12, 19, 3, S6); p.hl(14, 17, 2, S7);
  p.px(10, 5, S5); p.px(11, 4, S7); p.px(12, 3, S7); p.px(21, 5, S2); p.px(20, 4, S3); p.px(19, 3, S4); p.px(15, 1, S5); p.px(16, 1, S5);
  // its cable, down the side to the box
  p.hl(21, 23, 4, K1); p.vl(23, 4, 5, K1); p.vl(29, 7, 35, K1); p.px(29, 7, S5);
  for (const y of [12, 20, 27]) p.px(29, y, S5);
  p.px(28, 35, K1); p.px(27, 35, K1);
  // the lamp under the lid, dark
  p.px(7, 9, S2); p.px(24, 9, S2); p.hl(8, 23, 9, LOFF1); p.hl(11, 20, 9, LOFF2); p.hl(10, 21, 10, LOFF0);
  p.outline(MACHINE_OUTLINE);
  const work = new Pic(32, 48);
  work.hl(8, 23, 9, LON0); work.hl(9, 22, 9, LON1); work.hl(12, 19, 9, LON2); work.hl(15, 16, 9, LON3);
  work.hl(10, 21, 10, LON0); work.hl(13, 18, 10, LON1);
  work.px(26, 41, LON1);
  const soilTop = 26;
  return { base: p, work, light: [16, 10], species: 'tomato', at: { x: 15, y: soilTop + 1, mask: (x, y) => x >= 7 && x <= 25 && y >= 12 && y <= soilTop + 1 } };
}

// ---------- C: a lab pod: a glass pill on a pale plinth, an arc of grow LEDs under the dome ----------
function lookC() {
  const p = new Pic(32, 48), [C0, C1, C2, C3, C4] = CER;
  // plinth
  p.rect(6, 45, 25, 46, K1); p.hl(6, 25, 45, S2);
  p.panel(3, 35, 28, 44, [C1, C2, C3]); p.hl(4, 27, 35, C4); p.hl(4, 27, 44, C0);
  p.clear(3, 35); p.clear(28, 35); p.clear(3, 44); p.clear(28, 44);
  p.hl(4, 27, 37, TEAL2); p.hl(4, 27, 38, TEAL1); p.px(3, 37, TEAL2); p.px(28, 38, TEAL0); p.px(3, 38, TEAL1);
  p.rect(6, 40, 15, 42, K0); p.hl(7, 11, 40, TEAL1); p.hl(7, 13, 41, TEAL0); p.hl(7, 9, 42, TEAL1); p.px(14, 40, GR2);
  p.hl(5, 16, 43, C1); p.vl(16, 40, 43, C3);
  for (const [x, c] of [[19, TEAL1], [22, C1], [25, LOFF2]]) { p.px(x, 41, c); p.px(x + 1, 41, c); p.px(x, 42, C1); p.px(x + 1, 42, C1); }
  // the collar
  p.panel(4, 33, 27, 34, [C1, C3, C4]);
  // glass pill: a column under a dome
  glass(p, 5, 26, 9, 32, { stripe: false });
  const dome = [[4, 11, 20], [5, 9, 22], [6, 7, 24], [7, 6, 25], [8, 6, 25]];
  for (const [y, a, b] of dome) for (let x = a; x <= b; x++) p.px(x, y, x === a || x === b ? G3 : x < 16 ? G2 : G1);
  p.px(5, 8, G3); p.px(26, 8, G3);
  // the dome's highlight: an arc, then the stripe down the left
  for (const [x, y] of [[11, 5], [12, 5], [9, 6], [10, 6], [8, 7], [7, 8], [7, 9]]) p.px(x, y, G5, 'over');
  p.vl(6, 10, 29, G5, 'over'); p.vl(7, 11, 27, G4, 'over');
  glint(p, [...diag(24, 14, 4), ...diag(24, 21, 2)], [[23, 15]]);
  // the tray of soil
  soil(p, 6, 25, 28, 31, 5); p.px(13, 30, PEB); p.px(14, 30, PEB2);
  p.hl(6, 25, 31, TEAL1); p.hl(6, 25, 32, TEAL0); p.px(6, 31, TEAL2);
  p.vl(5, 9, 32, G3); p.vl(26, 9, 32, G3);
  // ceramic ribs up the sides to the cap
  p.vl(4, 10, 32, C3); p.vl(27, 10, 32, C1); p.px(4, 9, C4); p.px(27, 9, C2);
  p.hl(12, 19, 3, C3); p.hl(13, 18, 2, C4); p.px(12, 3, C4); p.px(19, 3, C1); p.hl(14, 17, 1, C3);
  p.px(15, 2, TEAL2); p.px(16, 2, TEAL1);
  // the LED arc under the dome, dark
  for (const [x, y] of [[10, 6], [11, 5], [12, 5], [13, 5], [14, 5], [15, 5], [16, 5], [17, 5], [18, 5], [19, 5], [20, 5], [21, 6]]) p.px(x, y, LOFF1);
  for (const x of [12, 14, 16, 18]) p.px(x + 1, 5, LOFF2);
  // the arc sits under the highlight: move the highlight down a row where they meet
  for (const [x, y] of [[9, 6], [8, 7], [7, 8]]) p.px(x, y, G5, 'over');
  p.outline(MACHINE_OUTLINE);
  const work = new Pic(32, 48);
  for (const [x, y] of [[10, 6], [21, 6]]) work.px(x, y, LON0);
  for (let x = 11; x <= 20; x++) work.px(x, 5, x % 2 ? LON1 : LON2);
  for (let x = 12; x <= 19; x++) work.px(x, 6, LON0);
  work.px(25, 41, LON1); work.px(26, 41, LON1);
  const soilTop = 28;
  return { base: p, work, light: [16, 7], species: 'berry', at: { x: 15, y: soilTop, mask: (x, y) => x >= 8 && x <= 24 && y >= 9 && y <= soilTop } };
}

const LOOKS = { A: lookA, B: lookB, C: lookC };
function capsule(look) {
  const c = LOOKS[look]();
  c.plants = [0, 1, 2, 3].map(s => plant(c.species, s, c.base, c.at));
  return c;
}
module.exports = { capsule };

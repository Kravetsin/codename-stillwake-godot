// A placeholder circuit assembler, 32x32, matching its recipe: a frame bench, an arm on a rotor joint, a glass
// magnifier, wire spools, a board. Writes machine_assembler.png (at rest, the arm parked, the screen dark),
// assembler_work_0..3.png (lit: the cabinet's inside with the arm at work and the rotor turned, opaque over it)
// and assembler_screen_0..3.png (unlit: the screen's green trace and the tip's spark, alone).
// Also the item icon solarCell.png (16x16) into <out>/items.
const { encode } = require("./png.js");
const path = require("path");
const out = process.argv[2] || path.join(__dirname, '..', '..', '..', 'game', 'art');
const W = 32, H = 32;
const C = {
  line: 0x1a1e25, back: 0x22262c, deep: 0x10161d,
  hi: 0x9aa0ab, steel: 0x8f99ad, mid: 0x7f848f, mid2: 0x6b7385, dark: 0x5b6170, dark2: 0x474c57,
  cu: 0xb07a50, cu2: 0x8a5a3a, cu3: 0x6a4430, cuHi: 0xc98a55,
  board: 0x1c4626, boardHi: 0x3f8f4f, chip: 0x14171e, gold: 0xf2cf5a,
  glass: 0xc6e9f2, glass2: 0x8fc4d2, glint: 0xf2fbff,
  trace: 0x6fbf73, traceHi: 0xb8f5a8, spark: 0xfff2b0,
};
function canvas(w = W, h = H) { return { w, h, px: new Array(w * h).fill(null) }; }
function set(c, x, y, col) { if (x >= 0 && y >= 0 && x < c.w && y < c.h) c.px[y * c.w + x] = col; }
function rect(c, x0, y0, x1, y1, col) { for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) set(c, x, y, col); }
function box(c, x0, y0, x1, y1, fill) { rect(c, x0, y0, x1, y1, C.line); rect(c, x0 + 1, y0 + 1, x1 - 1, y1 - 1, fill); }
function line(c, x0, y0, x1, y1, col) {
  const n = Math.max(Math.abs(x1 - x0), Math.abs(y1 - y0), 1);
  for (let i = 0; i <= n; i++) set(c, Math.round(x0 + (x1 - x0) * i / n), Math.round(y0 + (y1 - y0) * i / n), col);
}
function save(c, name) {
  const rgba = Buffer.alloc(c.w * c.h * 4);
  c.px.forEach((col, i) => { if (col == null) return; rgba[i * 4] = col >> 16; rgba[i * 4 + 1] = (col >> 8) & 255; rgba[i * 4 + 2] = col & 255; rgba[i * 4 + 3] = 255; });
  encode(`${out}/${name}.png`, c.w, c.h, rgba);
}

// The cabinet's inside, where the arm moves: what a work picture repaints.
const IN = { x0: 5, y0: 5, x1: 26, y1: 17 };
// The rotor joint the arm turns on, and the arm's two lengths.
const J = { x: 8.5, y: 8.5 }, L1 = 6, L2 = 6.5;
// The screen, and the board under the magnifier.
const SCR = { x0: 19, y0: 5, x1: 25, y1: 9 };
const BOARD = { x0: 9, y0: 14, x1: 22, y1: 17 };

/** The arm's elbow for a tip at (tx, ty): two lengths from the joint, the elbow bent up. */
function elbow(tx, ty) {
  const dx = tx - J.x, dy = ty - J.y, d = Math.min(Math.hypot(dx, dy), L1 + L2 - 0.01);
  const a = Math.atan2(dy, dx), b = Math.acos((L1 * L1 + d * d - L2 * L2) / (2 * L1 * d));
  return { x: J.x + Math.cos(a - b) * L1, y: J.y + Math.sin(a - b) * L1 };
}

/** The arm: an upper and a lower length, two pixels thick, a soldering head at the tip. */
function arm(c, tx, ty) {
  const e = elbow(tx, ty);
  for (const [ox, oy, col] of [[0, 1, C.dark2], [0, 0, C.steel]]) {
    line(c, J.x + ox, J.y + oy, e.x + ox, e.y + oy, col);
    line(c, e.x + ox, e.y + oy, tx + ox, ty - 2 + oy, col);
  }
  rect(c, Math.round(e.x) - 1, Math.round(e.y) - 1, Math.round(e.x), Math.round(e.y), C.mid2);
  set(c, Math.round(e.x) - 1, Math.round(e.y) - 1, C.hi);
  // The head: a block and its tip.
  rect(c, Math.round(tx) - 1, Math.round(ty) - 2, Math.round(tx) + 1, Math.round(ty) - 1, C.dark);
  set(c, Math.round(tx), Math.round(ty), C.line);
}

/** The rotor joint: a rim, three spokes turned by `turn` (a third of a turn is a whole period), a hub. */
function rotor(c, turn) {
  for (let y = 4; y <= 13; y++) for (let x = 4; x <= 13; x++) {
    const d = Math.hypot(x - J.x, y - J.y);
    if (d > 3.6) continue;
    set(c, x, y, d > 2.9 ? C.line : d > 2.1 ? C.mid2 : C.back);
  }
  for (let k = 0; k < 3; k++) {
    const a = (turn + k / 3) * 2 * Math.PI;
    for (let r = 0.8; r <= 2.2; r += 0.5) set(c, Math.round(J.x + Math.cos(a) * r), Math.round(J.y + Math.sin(a) * r), C.dark2);
  }
  set(c, Math.floor(J.x), Math.floor(J.y), C.hi);
}

/** The magnifier: a lens in a ring on a stalk from the right post, over the board. */
function magnifier(c) {
  const cx = 21, cy = 12;
  rect(c, 24, 12, 26, 12, C.dark2);
  set(c, 26, 11, C.dark2);
  for (let y = cy - 3; y <= cy + 3; y++) for (let x = cx - 3; x <= cx + 3; x++) {
    const d = Math.hypot(x - cx, y - cy);
    if (d > 3.2) continue;
    set(c, x, y, d > 2.3 ? C.dark2 : d > 1.4 ? C.glass2 : C.glass);
  }
  set(c, cx - 1, cy - 1, C.glint);
  set(c, cx, cy - 2, C.glint);
}

function base(c) {
  // Feet and the legs of the bench, a frame: two posts with their highlights, a socket plate on the left.
  for (const x0 of [1, 26]) {
    box(c, x0, 21, x0 + 4, 30, C.mid);
    rect(c, x0 + 1, 22, x0 + 1, 29, C.steel);
    rect(c, x0 + 3, 22, x0 + 3, 29, C.mid2);
    rect(c, x0, 31, x0 + 4, 31, C.line);
    rect(c, x0 + 1, 31, x0 + 3, 31, C.dark2);
  }
  rect(c, 3, 23, 5, 27, C.dark);
  // The shelf under the bench and two spools of wire on it, copper wound between dark flanges.
  rect(c, 6, 22, 25, 29, C.back);
  box(c, 5, 28, 26, 30, C.dark);
  rect(c, 6, 29, 25, 29, C.mid2);
  for (const x0 of [8, 16]) {
    rect(c, x0, 22, x0, 27, C.dark2);
    rect(c, x0 + 6, 22, x0 + 6, 27, C.dark2);
    for (let y = 23; y <= 26; y++) for (let x = x0 + 1; x <= x0 + 5; x++) set(c, x, y, x === x0 + 1 ? C.cuHi : y % 2 ? C.cu : C.cu2);
    set(c, x0 + 3, 27, C.cu3);
  }
  // The bench top: the frame across the legs.
  box(c, 0, 18, 31, 21, C.mid);
  rect(c, 1, 19, 30, 19, C.steel);
  rect(c, 1, 20, 30, 20, C.mid2);
  for (const x of [2, 29]) set(c, x, 19, C.hi);
  // The cabinet: two posts and a beam, its inside dark.
  rect(c, IN.x0, IN.y0, IN.x1, IN.y1, C.back);
  for (const x0 of [1, 26]) {
    box(c, x0, 0, x0 + 4, 18, C.mid);
    rect(c, x0 + 1, 1, x0 + 1, 17, C.steel);
    rect(c, x0 + 3, 1, x0 + 3, 17, C.mid2);
    for (const y of [6, 12]) set(c, x0 + 2, y, C.hi);
  }
  box(c, 1, 0, 30, 4, C.mid);
  rect(c, 2, 1, 29, 1, C.hi);
  rect(c, 2, 3, 29, 3, C.dark);
  for (const x of [3, 28]) set(c, x, 2, C.hi);
}

/** The inside: the screen dark, the board, the magnifier, the rotor and the arm with its tip at (tx, ty). */
function inside(c, tx, ty, turn) {
  rect(c, IN.x0, IN.y0, IN.x1, IN.y1, C.back);
  // The screen on its bracket, dark.
  box(c, SCR.x0, SCR.y0, SCR.x1, SCR.y1, C.deep);
  rect(c, SCR.x0 + 2, SCR.y0 - 1, SCR.x0 + 2, SCR.y0 - 1, C.dark2);
  // The board on its jig: dark green, a chip, gold pads.
  box(c, BOARD.x0, BOARD.y0, BOARD.x1, BOARD.y1, C.board);
  rect(c, BOARD.x0 + 1, BOARD.y0 + 1, BOARD.x1 - 1, BOARD.y0 + 1, C.boardHi);
  rect(c, 12, 16, 14, 16, C.chip);
  for (const x of [17, 19, 21]) set(c, x, 16, C.gold);
  set(c, 10, 16, C.gold);
  magnifier(c);
  rotor(c, turn);
  // The arm stays inside the cabinet: what would rise into the beam is behind it.
  const a = canvas();
  arm(a, tx, ty);
  for (let y = IN.y0; y <= IN.y1; y++) for (let x = IN.x0; x <= IN.x1; x++) if (a.px[y * W + x] != null) set(c, x, y, a.px[y * W + x]);
}

// The arm parked, and at work: dipping to the board under the glass, pad by pad.
const PARK = [17, 8];
const FRAMES = [[13, 13], [16, 12], [19, 13], [16, 10]];

const rest = canvas();
base(rest);
inside(rest, PARK[0], PARK[1], 0);
save(rest, "machine_assembler");
FRAMES.forEach(([tx, ty], i) => {
  const full = canvas();
  base(full);
  inside(full, tx, ty, i / 4 / 3);
  // Only the inside: the rest of the machine stays as its own picture draws it.
  const c = canvas();
  for (let y = IN.y0; y <= IN.y1; y++) for (let x = IN.x0; x <= IN.x1; x++) set(c, x, y, full.px[y * W + x]);
  save(c, `assembler_work_${i}`);
  // Unlit: the screen's trace scrolling, and a spark where the tip touches the board.
  const g = canvas();
  for (let x = SCR.x0 + 1; x <= SCR.x1 - 1; x++) {
    const k = (x + i) % 5;
    const y = k === 0 ? SCR.y0 + 1 : k === 1 || k === 4 ? SCR.y0 + 2 : SCR.y0 + 3;
    set(g, x, y, k === 0 ? C.traceHi : C.trace);
  }
  set(g, SCR.x1 - 1, SCR.y1 - 1, i % 2 ? C.trace : C.traceHi);
  if (ty >= 13) { set(g, tx, ty + 1, C.spark); set(g, tx + 1, ty + 1, C.gold); }
  save(g, `assembler_screen_${i}`);
});

// The solar cell's icon: a dark blue cell under glass, silver busbars, a glint, a copper tab of wire.
const hex = h => parseInt(h, 16);
function icon(name, rows, pal) {
  const c = canvas(16, 16);
  if (rows.length !== 16) throw new Error(name + " has " + rows.length + " rows");
  rows.forEach((row, y) => {
    if (row.length !== 16) throw new Error(name + " row " + y + " is " + row.length);
    [...row].forEach((ch, x) => { if (ch !== ".") set(c, x, y, hex(pal[ch])); });
  });
  save(c, "items/" + name);
}
icon("solarCell", [
  "................",
  "................",
  "..aaaaaaaaaaaa..",
  "..adbbcbbbcbba..",
  "..abdbcbbbcbba..",
  "..abbdcbbbcbba..",
  "..acccccccccca..",
  "..abbbcdbbcbba..",
  "..abbbcbdbcbba..",
  "..abbbcbbdcbba..",
  "..accccccccdca..",
  "..abbbcbbbcbda..",
  "..aeeeceeeceea..",
  "..aaaaaaaaaaaa..",
  "......aff.......",
  "......aff.......",
], { a: "14171e", b: "2c4f94", c: "aeb8c8", d: "dff2ff", e: "1e3566", f: "c98a55" });

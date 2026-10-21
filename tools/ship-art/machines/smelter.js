// A placeholder electric smelter, 32x32, matching its recipe: a plated box (plates) on a frame, two copper
// coils behind a furnace-like grate, a small control board (the circuit). Writes machine_smelter.png (cold)
// and smelter_work_0..2.png: overlays of only the coils' pixels between the bars, glowing, drawn unlit.
// Also the item icons silicon.png and glass.png (16x16) into <out>/items.
const { encode } = require("./png.js");
const path = require("path");
const out = process.argv[2] || path.join(__dirname, '..', '..', '..', 'game', 'art');
const W = 32, H = 32;
const C = {
  line: 0x1a1e25, back: 0x22262c, deep: 0x10161d,
  hi: 0x9aa0ab, steel: 0x8f99ad, mid: 0x7f848f, mid2: 0x6b7385, dark: 0x5b6170, dark2: 0x474c57,
  cu: 0xb07a50, cu2: 0x8a5a3a, cu3: 0x6a4430, cuHi: 0xc98a55,
  board: 0x1c4626, boardHi: 0x3f8f4f, led: 0x6fbf73, ledOff: 0x2c6a3a, amber: 0xf2cf5a, ceramic: 0xd8d2c4, ceramic2: 0xa8a092,
};
function canvas(w = W, h = H) { return { w, h, px: new Array(w * h).fill(null) }; }
function set(c, x, y, col) { if (x >= 0 && y >= 0 && x < c.w && y < c.h) c.px[y * c.w + x] = col; }
function rect(c, x0, y0, x1, y1, col) { for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) set(c, x, y, col); }
function box(c, x0, y0, x1, y1, fill) { rect(c, x0, y0, x1, y1, C.line); rect(c, x0 + 1, y0 + 1, x1 - 1, y1 - 1, fill); }
function save(c, name) {
  const rgba = Buffer.alloc(c.w * c.h * 4);
  c.px.forEach((col, i) => { if (col == null) return; rgba[i * 4] = col >> 16; rgba[i * 4 + 1] = (col >> 8) & 255; rgba[i * 4 + 2] = col & 255; rgba[i * 4 + 3] = 255; });
  encode(`${out}/${name}.png`, c.w, c.h, rgba);
}

// The grate's window and what is behind it.
const WIN = { x0: 7, y0: 12, x1: 24, y1: 23 };
const COILS = [[9, 14], [17, 22]];
const WINDINGS = [14, 16, 18, 20, 22];
const BARS = [10, 13, 16, 19, 22];
const isBar = x => BARS.includes(x);

/** Every pixel of a coil's windings, with how far along its winding it is (0 at the left end, 1 at the right). */
function windingPixels() {
  const px = [];
  for (const [x0, x1] of COILS)
    for (const y of WINDINGS)
      for (let x = x0; x <= x1; x++) px.push({ x, y, t: (x - x0) / (x1 - x0), coil: x0 });
  return px;
}

function base(c) {
  // Two insulators on top, where its power comes in.
  for (const x of [6, 24]) {
    rect(c, x, 1, x + 1, 3, C.ceramic2);
    rect(c, x, 1, x, 2, C.ceramic);
    rect(c, x - 1, 3, x + 2, 3, C.line);
  }
  // The frame it stands on: a crossbar and two legs with feet.
  box(c, 1, 24, 30, 27, C.mid);
  rect(c, 2, 25, 29, 25, C.steel);
  for (const x of [3, 28]) set(c, x, 26, C.hi);
  for (const x0 of [3, 25]) {
    box(c, x0, 27, x0 + 3, 31, C.dark);
    rect(c, x0 + 1, 28, x0 + 1, 30, C.mid2);
  }
  // The plated box: four plates, seams and rivets.
  box(c, 2, 4, 29, 24, C.mid);
  rect(c, 3, 5, 28, 5, C.hi);
  rect(c, 3, 23, 28, 23, C.dark);
  rect(c, 15, 5, 15, 11, C.dark2);
  rect(c, 3, 11, 28, 11, C.dark2);
  rect(c, 3, 12, 28, 12, C.steel);
  for (const [x, y] of [[4, 7], [13, 7], [17, 7], [4, 21], [27, 21], [4, 14], [27, 14]]) set(c, x, y, C.hi);
  // Vent slots on the left plate.
  for (const y of [7, 9]) rect(c, 6, y, 12, y, C.line);
  // The control board on the right plate: a dark green board, two lights and a dial.
  box(c, 18, 6, 27, 10, C.board);
  rect(c, 19, 7, 26, 7, C.boardHi);
  set(c, 20, 8, C.ledOff); set(c, 22, 8, C.amber);
  rect(c, 24, 8, 25, 9, C.ceramic2); set(c, 24, 8, C.line);
  // The grate's window, the coils behind it, cold, and the bars over them.
  box(c, WIN.x0, WIN.y0, WIN.x1, WIN.y1, C.deep);
  for (const [x0, x1] of COILS) rect(c, x0, 13, x1, 22, C.back);
  for (const { x, y, t } of windingPixels()) set(c, x, y, t < 0.2 ? C.cu3 : t < 0.45 ? C.cuHi : t < 0.8 ? C.cu : C.cu2);
  for (const x of BARS) {
    rect(c, x, WIN.y0 + 1, x, WIN.y1 - 1, C.dark2);
    set(c, x, WIN.y0 + 1, C.mid2);
  }
  // The power socket's plate, left of the window.
  rect(c, 3, 15, 5, 19, C.dark);
}

// Hot colours, from the coolest glow to white heat.
const HEAT = [0x7a2410, 0xc84a18, 0xff7a20, 0xffb050, 0xffe08a];

function work(frame) {
  const c = canvas();
  // The window glows dark red behind the coils, between the bars.
  for (let y = WIN.y0 + 1; y <= WIN.y1 - 1; y++)
    for (let x = WIN.x0 + 1; x <= WIN.x1 - 1; x++) if (!isBar(x)) set(c, x, y, 0x2a1208);
  for (const [x0, x1] of COILS) for (let y = 13; y <= 22; y++) for (let x = x0; x <= x1; x++) if (!isBar(x)) set(c, x, y, 0x4a1a0c);
  // Each winding hottest in its middle; the heat pulses up the coils, a winding a frame.
  for (const { x, y, t, coil } of windingPixels()) {
    if (isBar(x)) continue;
    const row = WINDINGS.indexOf(y);
    const wave = (row + frame + (coil > 10 ? 1 : 0)) % 3 === 0 ? 1 : 0;
    const middle = 1 - Math.abs(t - 0.5) * 2;
    const level = Math.min(HEAT.length - 1, Math.round(1 + middle * 2 + wave));
    set(c, x, y, HEAT[level]);
  }
  return c;
}

const rest = canvas();
base(rest);
save(rest, "machine_smelter");
for (let i = 0; i < 3; i++) save(work(i), `smelter_work_${i}`);

// Item icons, 16x16, in the parts' outline.
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
const A = "14171e";
// Silicon: a grey-blue lump with a crystal's facets and a violet sheen, of the rock it comes from.
icon("silicon", [
  "................",
  "................",
  "................",
  "......aa........",
  ".....abba.......",
  "....abccbaaa....",
  "...abcceccbba...",
  "...abccddcccba..",
  "..abcdddeddcda..",
  "..abddfdddddfa..",
  ".abddfffddffdda.",
  ".adffffgfffffda.",
  ".aggggggggggga..",
  "...aaaaaaaaaaa..",
  "................",
  "................",
], { a: A, b: "dfe4f2", c: "a9b2cc", d: "7a8399", e: "c9b8f0", f: "56607a", g: "3c4458" });
// Glass: a pane, pale and clear, a glint across it.
icon("glass", [
  "................",
  "................",
  "................",
  "...aaaaaaaaaaa..",
  "...abbbbbbbbca..",
  "...abddddedcca..",
  "...abdddeddcca..",
  "...abddeddddca..",
  "...abdedddddca..",
  "...abeddddddca..",
  "...abdddddedca..",
  "...abddddeddca..",
  "...accccccccca..",
  "...aaaaaaaaaaa..",
  "................",
  "................",
], { a: A, b: "f2fbff", c: "8fc4d2", d: "c6e9f2", e: "ffffff" });

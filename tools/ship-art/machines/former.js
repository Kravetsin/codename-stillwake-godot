// A placeholder Former, 32x32, matching its recipe: two frame posts, a rotor flywheel on top, gears driving
// a press head onto a die with a plate on it. Writes machine_former.png (at rest) and former_work_0..3.png:
// full-size overlays of the moving parts (the flywheel, the gears, the head), opaque over what they cover.
const { encode } = require("./png.js");
const path = require("path");
const out = process.argv[2] || path.join(__dirname, '..', '..', '..', 'game', 'art');
const W = 32, H = 32;
const C = {
  line: 0x1a1e25, back: 0x22262c, deep: 0x10161d,
  hi: 0x9aa0ab, steel: 0x8f99ad, mid: 0x7f848f, mid2: 0x6b7385, dark: 0x5b6170, dark2: 0x474c57,
  bronze: 0x9a6232, bronze2: 0x6a4430, rust: 0x5e3a20, plate: 0xa8a092,
};
function canvas() { return { px: new Array(W * H).fill(null) }; }
function clip(c, x, y, col) { if (x >= 7 && x <= 24 && y >= 14 && y <= 21) set(c, x, y, col); }
function set(c, x, y, col) { if (x >= 0 && y >= 0 && x < W && y < H) c.px[y * W + x] = col; }
function rect(c, x0, y0, x1, y1, col) { for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) set(c, x, y, col); }
function box(c, x0, y0, x1, y1, fill) { rect(c, x0, y0, x1, y1, C.line); rect(c, x0 + 1, y0 + 1, x1 - 1, y1 - 1, fill); }
function save(c, name) {
  const rgba = Buffer.alloc(W * H * 4);
  c.px.forEach((col, i) => { if (col == null) return; rgba[i * 4] = col >> 16; rgba[i * 4 + 1] = (col >> 8) & 255; rgba[i * 4 + 2] = col & 255; rgba[i * 4 + 3] = 255; });
  encode(`${out}/${name}.png`, W, H, rgba);
}

// The rotor flywheel: a rim, four spokes turned by `turn` (a quarter turn is a whole period), a hub.
const WX = 15.5, WY = 5.5, WR = 5.6;
function wheel(c, turn) {
  for (let y = 0; y <= 11; y++) for (let x = 9; x <= 22; x++) {
    const d = Math.hypot(x - WX, y - WY);
    if (d > WR + 0.5) continue;
    set(c, x, y, d > WR - 0.6 ? C.line : d > WR - 1.6 ? C.mid2 : C.back);
  }
  for (let k = 0; k < 4; k++) {
    const a = turn * Math.PI / 2 + k * Math.PI / 2;
    for (let r = 1; r < WR - 1.4; r += 0.5) set(c, Math.round(WX + Math.cos(a) * r - 0.01), Math.round(WY + Math.sin(a) * r - 0.01), C.dark2);
  }
  rect(c, 15, 5, 16, 6, C.hi);
}

// A gear: a disc with teeth every 45 degrees, offset by `turn` of a tooth.
function gear(c, cx, cy, turn) {
  for (let y = cy - 3; y <= cy + 3; y++) for (let x = cx - 3; x <= cx + 3; x++) {
    const dx = x - cx, dy = y - cy, d = Math.hypot(dx, dy);
    if (d <= 2.2) clip(c, x, y, d <= 0.9 ? C.rust : C.bronze);
    else if (d <= 3.3) {
      const a = (Math.atan2(dy, dx) / (Math.PI / 4) - turn + 8) % 1;
      if (a < 0.5) clip(c, x, y, C.bronze2);
    }
  }
}

// The press: its back between the posts, the ram from the beam, the head `drop` pixels down, two gears.
function press(c, drop, turn) {
  rect(c, 7, 14, 24, 21, C.back);
  rect(c, 15, 14, 16, 14 + drop, C.mid);
  set(c, 15, 14, C.hi);
  box(c, 12, 15 + drop, 19, 17 + drop, C.steel);
  rect(c, 13, 16 + drop, 18, 16 + drop, C.hi);
  gear(c, 9, 17, turn);
  gear(c, 22, 17, -turn);
}

function base(c) {
  // Feet, the bed and the die with a plate on it.
  rect(c, 3, 30, 6, 31, C.line); rect(c, 25, 30, 28, 31, C.line);
  rect(c, 4, 30, 5, 30, C.dark2); rect(c, 26, 30, 27, 30, C.dark2);
  rect(c, 7, 14, 24, 24, C.back);
  box(c, 1, 25, 30, 29, C.dark);
  rect(c, 2, 26, 29, 26, C.mid2);
  for (const x of [3, 28]) set(c, x, 27, C.hi);
  box(c, 10, 22, 21, 25, C.dark2);
  rect(c, 11, 22, 20, 22, C.plate);
  // The posts.
  for (const x0 of [2, 25]) {
    box(c, x0, 10, x0 + 4, 25, C.mid);
    rect(c, x0 + 1, 11, x0 + 1, 24, C.steel);
    rect(c, x0 + 3, 11, x0 + 3, 24, C.mid2);
    for (const y of [14, 19, 23]) set(c, x0 + 2, y, C.hi);
  }
  // The beam across them, and the flywheel's bracket on it.
  box(c, 2, 9, 29, 13, C.mid);
  rect(c, 3, 10, 28, 10, C.steel);
  rect(c, 3, 12, 28, 12, C.dark);
  for (const x of [4, 27]) set(c, x, 11, C.hi);
  rect(c, 14, 7, 17, 9, C.line);
  rect(c, 15, 8, 16, 9, C.dark2);
}

const frames = [[0, 0], [2, 0.25], [4, 0.5], [2, 0.75]];
const rest = canvas();
base(rest); press(rest, 0, 0); wheel(rest, 0);
save(rest, "machine_former");
frames.forEach(([drop, turn], i) => {
  const c = canvas();
  press(c, drop, turn);
  wheel(c, turn / 2);
  save(c, `former_work_${i}`);
});

// A placeholder capacitor bank, 32x32 (0101), in the ship machines' palette: a plated cabinet on a base and feet holding
// two capacitor cans banded in copper, a socket on top between them, and down its middle a recessed gauge of six
// segments, drawn here unlit. The client lights the segments over it (StoreLights). Writes machine_capacitorBank.png.
const { encode } = require("./png.js");
const path = require("path");
const file = process.argv[2] || path.join(__dirname, '..', '..', '..', 'game', 'art', 'machine_capacitorBank.png');
const W = 32, H = 32;
const C = {
  line: 0x1a1e25, deep: 0x10161d,
  hi: 0x9aa0ab, steel: 0x8f99ad, mid: 0x7f848f, mid2: 0x6b7385, dark: 0x5b6170, dark2: 0x474c57,
  cu: 0xb07a50, cu2: 0x8a5a3a, cuHi: 0xc98a55,
  can: 0x3a536a, canHi: 0x5f86a8, canLo: 0x2a3d4f, glass: 0x1f2b26,
};
const px = new Array(W * H).fill(null);
const set = (x, y, col) => { if (x >= 0 && y >= 0 && x < W && y < H) px[y * W + x] = col; };
const rect = (x0, y0, x1, y1, col) => { for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) set(x, y, col); };
const box = (x0, y0, x1, y1, fill) => { rect(x0, y0, x1, y1, C.line); rect(x0 + 1, y0 + 1, x1 - 1, y1 - 1, fill); };

// Feet and the base plate.
box(3, 28, 7, 31, C.dark2);
box(24, 28, 28, 31, C.dark2);
box(1, 26, 30, 29, C.mid);
rect(2, 27, 29, 27, C.hi);
rect(2, 28, 29, 28, C.dark);

// The cabinet, lit from above and the left.
box(2, 6, 29, 26, C.mid);
rect(3, 7, 28, 7, C.hi);
rect(3, 8, 3, 25, C.steel);
rect(28, 8, 28, 25, C.mid2);
rect(3, 25, 28, 25, C.dark);

// Two cans standing in it, round by their shading, each banded twice in copper, two terminals on its cap.
const shade = [C.canLo, C.can, C.canHi, C.canHi, C.can, C.can, C.canLo];
for (const x0 of [4, 20]) {
  rect(x0, 2, x0 + 8, 24, C.line);
  for (let i = 0; i < shade.length; i++) rect(x0 + 1 + i, 3, x0 + 1 + i, 23, shade[i]);
  rect(x0 + 1, 3, x0 + 7, 3, C.hi);
  rect(x0 + 1, 4, x0 + 7, 4, C.dark);
  for (const y of [9, 19]) {
    rect(x0 + 1, y, x0 + 7, y, C.cu);
    set(x0 + 3, y, C.cuHi);
    set(x0 + 4, y, C.cuHi);
    rect(x0 + 1, y + 1, x0 + 7, y + 1, C.cu2);
  }
  for (const tx of [x0 + 2, x0 + 6]) {
    set(tx, 1, C.line);
    set(tx, 0, null);
    set(tx, 2, C.cu);
    set(tx, 1, C.cuHi);
  }
}

// The socket on top, between the cans, where a cable plugs in.
box(13, 2, 18, 6, C.dark2);
rect(15, 3, 16, 4, C.deep);
rect(14, 3, 14, 5, C.mid2);

// The gauge down the middle: a recess with six dark segments, the lowest first.
box(13, 7, 18, 25, C.deep);
for (let s = 0; s < 6; s++) {
  const y = 23 - 3 * s;
  rect(14, y, 17, y + 1, C.glass);
}

const rgba = Buffer.alloc(W * H * 4);
px.forEach((col, i) => { if (col == null) return; rgba[i * 4] = col >> 16; rgba[i * 4 + 1] = (col >> 8) & 255; rgba[i * 4 + 2] = col & 255; rgba[i * 4 + 3] = 255; });
encode(file, W, H, rgba);

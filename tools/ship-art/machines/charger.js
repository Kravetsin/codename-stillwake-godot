const { encode } = require("./png.js");
// A placeholder charging station, 16x32: a cabinet with a light at the top and a bay for a battery.
const rows = [
  "................",
  "................",
  "................",
  "................",
  "..aaaaaaaaaaaa..",
  "..abbbbbbbbbba..",
  "..abaaaaaaaaba..",
  "..abaccccccaba..",
  "..abaccccccaba..",
  "..abaaaaaaaaba..",
  "..abbbbbbbbbba..",
  "..abaaaaaaaaba..",
  "..abahhggghaba..",
  "..abahhhhhhaba..",
  "..abahhhhhhaba..",
  "..abahhhhhhaba..",
  "..abahhhhhhaba..",
  "..abahhhhhhaba..",
  "..abahhhhhhaba..",
  "..abahhhhhhaba..",
  "..abahhhhhhaba..",
  "..abahhhhhhaba..",
  "..abahhhhhhaba..",
  "..abahhhhhhaba..",
  "..abaaaaaaaaba..",
  "..abbbbbbbbbba..",
  "..abiibbbbiiba..",
  "..abbbbbbbbbba..",
  "..ajjjjjjjjjja..",
  "..aaaaaaaaaaaa..",
  ".aggggggggggggg.",
  ".aaaaaaaaaaaaaa.",
];
const pal = { a: 0x1a1e25, b: 0x3a536a, c: 0x10161d, g: 0x5f86a8, h: 0x2a2f3a, i: 0x9fbfda, j: 0x2a3d4f };
const w = 16, h = rows.length, rgba = Buffer.alloc(w * h * 4);
rows.forEach((r, y) => [...r].forEach((ch, x) => {
  if (ch === ".") return;
  const c = pal[ch], i = (y * w + x) * 4;
  rgba[i] = c >> 16; rgba[i + 1] = (c >> 8) & 255; rgba[i + 2] = c & 255; rgba[i + 3] = 255;
}));
encode(process.argv[2] || require("path").join(__dirname, '..', '..', '..', 'game', 'art', 'machine_charger.png'), w, h, rgba);

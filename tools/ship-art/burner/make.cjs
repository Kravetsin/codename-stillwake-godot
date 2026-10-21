// The burner generator's picture, 32x32 (2x2 cells), in the furnace's scrap palette (ship-art.ts).
// Writes machine_burnerGenerator.png to argv[2] (default game/art) and the wheel frames beside it; a 10x zoom to argv[3] if given.
const fs = require('fs'), zlib = require('zlib');
process.argv[2] = process.argv[2] || require('path').join(__dirname, '..', '..', '..', 'game', 'art', 'machine_burnerGenerator.png');
const W = 32, H = 32;
const g = Array.from({ length: H }, () => Array(W).fill('.'));
const set = (x, y, c) => { if (x >= 0 && x < W && y >= 0 && y < H) g[y][x] = c; };
const rect = (x0, y0, x1, y1, c) => { for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) set(x, y, c); };
const row = (x, y, s) => { [...s].forEach((c, i) => { if (c !== ' ') set(x + i, y, c); }); };

// Body: a squat box of scrap, outlined.
rect(1, 12, 30, 29, 'K');
rect(2, 13, 29, 27, 'M');
// The firebox's face: a corrugated sheet, lit along its top.
for (let x = 2; x <= 15; x++) for (let y = 14; y <= 27; y++) set(x, y, x % 2 ? 'M' : 'D');
rect(2, 13, 15, 13, 'L');
// Soot over the mouth.
row(4, 14, 'ssssssssss'); row(3, 14, 'D'); row(14, 14, 'D');
row(5, 13, 'ss'); row(9, 13, 's');
// The mouth: the furnace's grate, 10x11 at (4, 16), the fire drawn over it while it burns.
rect(3, 15, 14, 27, 'K');
const mouth = ['RRRRRRRRRR', 'RRRRRRRRRR', 'rrrrrrrrrr', 'rrrrrrrrrr', 'gggggggggg',
  'rgrgrgrgrg', 'rgrgrgrgrg', 'rgrgrgrgrg', 'rgrgrgrgrg', 'rgrgrgrgrg', 'rgrgrgrgrg'];
mouth.forEach((s, i) => row(4, 16 + i, s));
// A rusty streak down the sheet beside it.
row(2, 18, 'a'); row(2, 19, 'a'); row(2, 20, 'A');
// The seam where the dynamo's plate is welded on.
for (let y = 13; y <= 28; y++) set(16, y, y % 2 ? 'W' : 'K');
// The dynamo: a hull plate bolted on, a window onto its copper windings.
rect(17, 13, 29, 27, 'I');
rect(29, 13, 29, 27, 'J');
rect(17, 26, 29, 27, 'J');
row(18, 14, 'X'); row(28, 14, 'X'); row(18, 25, 'X'); row(28, 25, 'X');
rect(19, 16, 27, 22, 'K');
for (let x = 20; x <= 26; x++) for (let y = 17; y <= 21; y++) set(x, y, x % 2 ? 'o' : 'O');
row(20, 17, 'pOpOpOp');
row(20, 21, 'OOOOOOO');
// A patch bolted on crooked, rust run from a rivet.
set(28, 15, 'a'); set(28, 16, 'A');
row(18, 15, 'a'); row(18, 16, 'A');
// Its shaft through the windings, out to the plug.
// Base: a rail along the bottom, lit, and the feet.
rect(2, 28, 29, 28, 'D');
for (let x = 3; x <= 28; x += 5) set(x, 28, 'X');
rect(1, 29, 30, 29, 'K');
row(3, 30, 'KLK'); row(3, 31, 'KKK');
row(25, 30, 'KAAAK'); row(25, 31, 'KKKKK');
// The stovepipe, bent, sooty at its mouth.
row(4, 2, 'KKKKK');
row(4, 3, 'KsssK');
row(4, 4, 'KLMDK');
row(4, 5, 'KLMDK');
row(4, 6, 'KaAAK');
row(4, 7, 'KLMDK');
row(4, 8, 'KLMDK');
row(4, 9, 'KLMDK');
row(4, 10, 'KLMDK');
row(4, 11, 'KLMDK');
// The flywheel the fire turns, on a bracket over the dynamo: rim, three spokes, a hub. The view
// spins the same wheel (burner_wheel_*.png) over it while it burns.
rect(21, 9, 25, 12, 'K'); rect(22, 10, 24, 12, 'J');
const WR = 6, cx = 23, cy = 6;
function wheel(turn) {
  const w = [];
  for (let y = -WR; y <= WR; y++) {
    let line = '';
    for (let x = -WR; x <= WR; x++) {
      const r = Math.hypot(x, y);
      let c = '.';
      if (r <= 6.6) c = 'K';
      if (r <= 5.6) c = y < 0 ? 'L' : x + y > 1 ? 'D' : 'M';
      if (r <= 4.6) {
        c = 'g';
        for (let k = 0; k < 3; k++) {
          const t = turn + k * 2 * Math.PI / 3;
          const along = x * Math.cos(t) + y * Math.sin(t), across = -x * Math.sin(t) + y * Math.cos(t);
          if (along > 0 && Math.abs(across) < 1.0) c = across < 0 ? "X" : "L";
        }
      }
      if (r <= 1.5) c = 'K';
      if (r <= 0.5) c = 'X';
      line += c;
    }
    w.push(line);
  }
  return w;
}
const FRAMES = 4;
const wheels = Array.from({ length: FRAMES }, (_, f) => wheel(-Math.PI / 2 + f * (2 * Math.PI / 3) / FRAMES));
wheels[0].forEach((line, i) => row(cx - WR, cy - WR + i, line.split('.').join(' ')));
// The plug's socket on the dynamo's side.

const P = {
  K: 0x1a1e25, M: 0x5b6170, L: 0x7f848f, D: 0x474c57, X: 0x9aa0ab, a: 0x8a5a3a, A: 0x6a4430,
  I: 0x8f99ad, J: 0x6b7385, W: 0xa8a092, R: 0x5a2a18, r: 0x3a1a12, g: 0x22262c, s: 0x10161d,
  o: 0x9a6232, O: 0x5e3a20, p: 0xb87a45,
};
console.log(g.map(r => r.join('')).join('\n'));
console.log(wheels.map(w => w.join('\n')).join('\n\n'));

function png(w, h, at) {
  const raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const [r, gg, b, a] = at(x, y); const o = y * (w * 4 + 1) + 1 + x * 4;
    raw[o] = r; raw[o + 1] = gg; raw[o + 2] = b; raw[o + 3] = a;
  }
  const crcT = Array.from({ length: 256 }, (_, n) => { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c >>> 0; });
  const crc = b => { let c = 0xffffffff; for (const x of b) c = crcT[(c ^ x) & 255] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
  const chunk = (t, d) => { const l = Buffer.alloc(4); l.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t), d]); const c = Buffer.alloc(4); c.writeUInt32BE(crc(td)); return Buffer.concat([l, td, c]); };
  const ih = Buffer.alloc(13); ih.writeUInt32BE(w, 0); ih.writeUInt32BE(h, 4); ih[8] = 8; ih[9] = 6;
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ih), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]);
}
const colour = c => c === '.' ? [0, 0, 0, 0] : [(P[c] >> 16) & 255, (P[c] >> 8) & 255, P[c] & 255, 255];
fs.writeFileSync(process.argv[2], png(W, H, (x, y) => colour(g[y][x])));
wheels.forEach((w, f) => fs.writeFileSync(require('path').join(require('path').dirname(process.argv[2]), 'burner_wheel_' + f + '.png'), png(w[0].length, w.length, (x, y) => colour(w[y][x]))));
const Z = 10;
if (process.argv[3]) fs.writeFileSync(process.argv[3], png(W * Z, H * Z, (x, y) => { const c = g[Math.floor(y / Z)][Math.floor(x / Z)]; return c === '.' ? [40, 44, 52, 255] : colour(c); }));

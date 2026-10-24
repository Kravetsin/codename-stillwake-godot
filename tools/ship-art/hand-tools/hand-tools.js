// The hand tools' 16 x 16 icons (0108), placeholders until art is picked: the wire stripper (0108) and the pipe wrench (0109).
// Writes PNGs with node's zlib, no dependencies. Usage: node hand-tools.js <items art dir>
const fs = require("fs"), zlib = require("zlib"), path = require("path");
const out = process.argv[2];

const colours = { O: "#14171e", s: "#c9ced6", S: "#8a929e", r: "#d84a3a", R: "#8e2a22", y: "#f2c14e", Y: "#9a6a14" };

const icons = {
  stripper: [
    "................",
    "............OO..",
    "...........OssO.",
    "..........OsSsO.",
    ".........OsSSO..",
    "........OsSOO...",
    ".......OSOSO....",
    "......OSSSO.....",
    ".....OrROrRO....",
    "....OrRO.OrRO...",
    "...OrRO...OrRO..",
    "..OrRO.....OrO..",
    ".OrRO.......O...",
    ".OrO............",
    "..O.............",
    "................",
  ],
  wrench: [
    "................",
    "..........OOO...",
    ".........OsssO..",
    "........OsSO.O..",
    "........OsO.....",
    ".......OsSO.....",
    "......OsSO......",
    ".....OsSO.......",
    "....OyyO........",
    "...OyYO.........",
    "..OyYO..........",
    ".OyYO...........",
    ".OYO............",
    "..O.............",
    "................",
    "................",
  ],
};

function png(rows) {
  const w = rows[0].length, h = rows.length, raw = Buffer.alloc((w * 4 + 1) * h);
  rows.forEach((row, y) => [...row].forEach((ch, x) => {
    const c = colours[ch];
    if (!c) return;
    const k = y * (w * 4 + 1) + 1 + x * 4;
    raw[k] = parseInt(c.slice(1, 3), 16); raw[k + 1] = parseInt(c.slice(3, 5), 16); raw[k + 2] = parseInt(c.slice(5, 7), 16); raw[k + 3] = 255;
  }));
  const crcTable = [...Array(256)].map((_, n) => { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c >>> 0; });
  const crc = b => { let c = 0xffffffff; for (const v of b) c = crcTable[(c ^ v) & 255] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
  const chunk = (type, data) => { const len = Buffer.alloc(4); len.writeUInt32BE(data.length); const td = Buffer.concat([Buffer.from(type), data]); const c = Buffer.alloc(4); c.writeUInt32BE(crc(td)); return Buffer.concat([len, td, c]); };
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6;
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk("IHDR", ihdr), chunk("IDAT", zlib.deflateSync(raw)), chunk("IEND", Buffer.alloc(0))]);
}

for (const [name, rows] of Object.entries(icons)) fs.writeFileSync(path.join(out, `${name}.png`), png(rows));

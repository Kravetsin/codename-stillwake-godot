// The screwdriver: three frames of the held sprite (ribs turning) in one strip, and its 16 x 16 icon.
// Writes PNGs with node's zlib, no dependencies. Usage: node screwdriver.js <art dir>
const fs = require("fs"), zlib = require("zlib"), path = require("path");
const out = process.argv[2];

const O = "#14171e", Y1 = "#f2c14e", Y2 = "#d49a24", Y3 = "#9a6a14", RIB = "#6e4a0e";
const F1 = "#9aa3b0", F2 = "#5a6270", S1 = "#f4f7fa", S2 = "#c9ced6", S3 = "#8a929e";

/** The held screwdriver along +x from the grip: handle 7, ferrule 1, shaft 6, tip 1; three rows. */
function held(phase) {
  const px = new Map(), set = (x, y, c) => px.set(`${x},${y}`, [x, y, c]);
  for (let x = 0; x < 7; x++) {
    const rib = (x + phase) % 3 === 0 && x > 0 && x < 6;
    set(x, 0, rib ? Y2 : Y1); set(x, 1, rib ? RIB : Y2); set(x, 2, rib ? RIB : Y3);
  }
  set(7, 0, F1); set(7, 1, F1); set(7, 2, F2);
  for (let x = 8; x < 14; x++) set(x, 1, x === 8 + (phase * 2) % 6 ? S1 : S2);
  set(14, 1, S3);
  return px;
}

/** An outline round every drawn pixel, as all the game's art has, shifted by (dx, dy). */
function framed(px, dx, dy) {
  const has = (x, y) => px.has(`${x},${y}`), pixels = [];
  for (let y = -1; y <= 4; y++) for (let x = -1; x <= 16; x++)
    if (!has(x, y) && (has(x - 1, y) || has(x + 1, y) || has(x, y - 1) || has(x, y + 1))) pixels.push([x + dx, y + dy, O]);
  for (const [x, y, c] of px.values()) pixels.push([x + dx, y + dy, c]);
  return pixels;
}

function png(w, h, pixels) {
  const raw = Buffer.alloc((w * 4 + 1) * h);
  for (const [x, y, c] of pixels) {
    if (x < 0 || y < 0 || x >= w || y >= h) continue;
    const k = y * (w * 4 + 1) + 1 + x * 4;
    raw[k] = parseInt(c.slice(1, 3), 16); raw[k + 1] = parseInt(c.slice(3, 5), 16); raw[k + 2] = parseInt(c.slice(5, 7), 16); raw[k + 3] = 255;
  }
  const crcTable = [...Array(256)].map((_, n) => { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c >>> 0; });
  const crc = b => { let c = 0xffffffff; for (const v of b) c = crcTable[(c ^ v) & 255] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
  const chunk = (type, data) => { const len = Buffer.alloc(4); len.writeUInt32BE(data.length); const td = Buffer.concat([Buffer.from(type), data]); const c = Buffer.alloc(4); c.writeUInt32BE(crc(td)); return Buffer.concat([len, td, c]); };
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6;
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk("IHDR", ihdr), chunk("IDAT", zlib.deflateSync(raw)), chunk("IEND", Buffer.alloc(0))]);
}

// The strip: three frames of 17 x 5 side by side (outline included), the grip at (2, 2) of each.
const fw = 17, fh = 5;
const strip = [];
for (let f = 0; f < 3; f++) for (const [x, y, c] of framed(held(f), 1 + f * fw, 1)) strip.push([x, y, c]);
fs.writeFileSync(path.join(out, "hero_screwdriver.png"), png(fw * 3, fh, strip));

// The icon: the screwdriver at -45 degrees about its middle, sampled nearest, framed again.
const base = held(0), turned = new Map(), a = -Math.PI / 4, cos = Math.cos(a), sin = Math.sin(a);
for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
  const ux = x + 0.5 - 8, uy = y + 0.5 - 8;
  const sx = Math.floor(7.5 + ux * cos + uy * sin), sy = Math.floor(1.5 - ux * sin + uy * cos);
  const p = base.get(`${sx},${sy}`);
  if (p) turned.set(`${x},${y}`, [x, y, p[2]]);
}
const icon = [];
const has = (x, y) => turned.has(`${x},${y}`);
for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++)
  if (!has(x, y) && (has(x - 1, y) || has(x + 1, y) || has(x, y - 1) || has(x, y + 1))) icon.push([x, y, O]);
for (const v of turned.values()) icon.push(v);
fs.writeFileSync(path.join(out, "tool_screwdriver.png"), png(16, 16, icon));
console.log("ok");

// Placeholder art for the ruins (0085): alloy walls, back walls, tops, details; spire cache, relic shrine, spire lamp; items.
// Usage: node tools/planet-art/ruins/make.js [game/art dir, default the repo's] [preview dir, none by default]
const fs = require('fs'), zlib = require('zlib'), path = require('path');
const artDir = process.argv[2] || path.join(__dirname, '..', '..', '..', 'game', 'art'), previewDir = process.argv[3];

function crc32(buf) {
  let c, crc = 0xffffffff;
  for (let n = 0; n < buf.length; n++) {
    c = (crc ^ buf[n]) & 0xff;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    crc = (crc >>> 8) ^ c;
  }
  return (crc ^ 0xffffffff) >>> 0;
}
function chunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const td = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td));
  return Buffer.concat([len, td, crc]);
}
function png(img) {
  const { w, h, px } = img;
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6;
  const raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) { raw[y * (w * 4 + 1)] = 0; px.copy(raw, y * (w * 4 + 1) + 1, y * w * 4, (y + 1) * w * 4); }
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]);
}
function image(w, h) { return { w, h, px: Buffer.alloc(w * h * 4) }; }
function set(img, x, y, c, a = 255) {
  if (x < 0 || y < 0 || x >= img.w || y >= img.h) return;
  const i = (y * img.w + x) * 4; img.px[i] = c[0]; img.px[i + 1] = c[1]; img.px[i + 2] = c[2]; img.px[i + 3] = a;
}
function get(img, x, y) { const i = (y * img.w + x) * 4; return [img.px[i], img.px[i + 1], img.px[i + 2], img.px[i + 3]]; }
function wrapSet(img, x, y, c, a) { set(img, ((x % img.w) + img.w) % img.w, ((y % img.h) + img.h) % img.h, c, a); }
function rect(img, x0, y0, w, h, c, a) { for (let y = y0; y < y0 + h; y++) for (let x = x0; x < x0 + w; x++) set(img, x, y, c, a); }
function hash(x, y, s) { let h = (x * 374761393 + y * 668265263 + s * 2147483647) >>> 0; h = ((h ^ (h >>> 13)) * 1274126177) >>> 0; return (h ^ (h >>> 16)) >>> 0; }
function save(img, rel, scale = 6) {
  const file = path.join(artDir, rel);
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, png(img));
  // A preview scaled up over a dark check, to look at.
  if (!previewDir) return;
  const big = image(img.w * scale, img.h * scale);
  for (let y = 0; y < big.h; y++) for (let x = 0; x < big.w; x++) {
    const [r, g, b, a] = get(img, Math.floor(x / scale), Math.floor(y / scale));
    const bg = ((Math.floor(x / 8) + Math.floor(y / 8)) & 1) ? 40 : 28, k = a / 255;
    set(big, x, y, [Math.round(r * k + bg * (1 - k)), Math.round(g * k + bg * (1 - k)), Math.round(b * k + (bg + 6) * (1 - k))]);
  }
  fs.writeFileSync(path.join(previewDir, rel.replace(/[\/]/g, '_')), png(big));
}

// The alloy's palette: stone's six steps, cooler, a touch of teal, more shine.
const P = {
  outline: [20, 29, 36], dark: [38, 53, 62], shade: [56, 76, 87], base: [78, 103, 114], light: [110, 138, 147], shine: [166, 194, 198],
  glyph: [74, 168, 160], glyphDark: [36, 92, 92],
};
const darker = (c, k) => c.map(v => Math.round(v * k));

// Walls: plates 32 × 8 in courses offset by 16, seams dark, lit along the top, rivets at some ends, engraved glyph lines.
function plates(back) {
  const img = image(128, 64), k = back ? 0.52 : 1;
  const C = Object.fromEntries(Object.entries(P).map(([n, c]) => [n, darker(c, k)]));
  const ph = back ? 16 : 8;
  for (let y = 0; y < 64; y++) for (let x = 0; x < 128; x++) {
    const course = Math.floor(y / ph), off = course % 2 ? 16 : 0, px = (x + off) % 32, py = y % ph;
    const id = Math.floor((x + off) / 32) + course * 7;
    let c = C.base;
    const n = hash(x, y, 3) % 100;
    if (n < 4) c = C.shade; else if (n > 97) c = C.light;
    // Some plates a step darker, worn.
    if (hash(id, 0, 9) % 4 === 0 && c === C.base) c = C.shade;
    if (py === 0) c = back ? C.shade : C.light;
    if (py === 1 && !back && hash(x, y, 5) % 3 === 0) c = C.shine;
    if (px === 0) c = back ? C.shade : C.light;
    if (py === ph - 1) c = C.outline;
    if (px === 31) c = C.dark;
    set(img, x, y, c);
  }
  // Rivets near the ends of some plates.
  for (let course = 0; course < 64 / ph; course++)
    for (let p = 0; p < 4; p++) {
      const off = course % 2 ? 16 : 0, x0 = p * 32 - off, y0 = course * ph;
      if (hash(p, course, 11) % 3 === 0) continue;
      for (const rx of [x0 + 3, x0 + 28]) {
        wrapSet(img, rx, y0 + 3, C.shine); wrapSet(img, rx, y0 + 4, C.dark);
        if (back) { wrapSet(img, rx, y0 + ph - 5, C.shine); wrapSet(img, rx, y0 + ph - 4, C.dark); }
      }
    }
  // Engraved glyph strokes on a few plates: a dark groove with a teal fleck.
  if (!back)
    for (let course = 0; course < 8; course++)
      for (let p = 0; p < 4; p++) {
        if (hash(p, course, 21) % 5 !== 0) continue;
        const off = course % 2 ? 16 : 0, x0 = p * 32 - off + 9, y0 = course * 8 + 3;
        const len = 6 + (hash(p, course, 22) % 8);
        for (let i = 0; i < len; i++) wrapSet(img, x0 + i, y0 + (i % 5 === 2 ? 1 : 0), i % 4 === 1 ? P.glyph : P.glyphDark);
      }
  return img;
}

// The top: metal grit resting on the top edge (back) and a few flecks before it (front), by the top's shape.
function tops() {
  const img = image(128, 64);
  const surface = (col, x) => col < 4 ? 16 : col < 6 ? 32 - x : 16 + x;
  for (let col = 0; col < 8; col++)
    for (let i = 0; i < (col < 4 ? 3 : 2); i++) {
      const x = 2 + (hash(col, i, 31) % 12), y = surface(col, x) - 1, big = hash(col, i, 32) % 3 === 0;
      for (const front of [false, true]) {
        if (front && hash(col, i, 33) % 2) continue;
        const oy = front ? 32 : 0, xx = col * 16 + x;
        set(img, xx, oy + y, front ? P.light : P.shade); set(img, xx + 1, oy + y, P.outline);
        if (big) { set(img, xx, oy + y - 1, P.shine); set(img, xx - 1, oy + y, P.dark); }
      }
    }
  return img;
}

// Details over whole alloy cells: singles in the first row, features of 2, 2 and 3 cells on the second.
function details() {
  const img = image(128, 64);
  const ring = (cx, cy, r, c, gap) => {
    for (let a = 0; a < 360; a += 3) {
      if (gap && a % 90 < 18) continue;
      const rad = a * Math.PI / 180;
      set(img, Math.round(cx + r * Math.cos(rad)), Math.round(cy + r * Math.sin(rad)), c);
    }
  };
  // 0 rivet pair, 1 vent, 2 glyph, 3 bolt plate, 4 scratch, 5 small ring, 6 crack, 7 dent.
  let x = 0;
  for (const r of [4, 11]) { set(img, x + r, 5, P.shine); set(img, x + r, 6, P.dark); set(img, x + r, 10, P.shine); set(img, x + r, 11, P.dark); }
  x = 16; for (let i = 0; i < 4; i++) { rect(img, x + 3, 4 + i * 2, 10, 1, P.outline); rect(img, x + 3, 5 + i * 2, 10, 1, P.light); }
  x = 32; for (const [dx, dy] of [[4, 4], [5, 4], [6, 4], [6, 5], [6, 6], [7, 7], [8, 7], [9, 7], [9, 8], [9, 9], [10, 10], [11, 10]]) set(img, x + dx, dy, P.glyph);
  x = 48; rect(img, x + 4, 4, 8, 8, P.outline); rect(img, x + 5, 5, 6, 6, P.light); rect(img, x + 6, 6, 4, 4, P.base); set(img, x + 7, 7, P.shine);
  x = 64; for (let i = 0; i < 9; i++) set(img, x + 3 + i, 9 - Math.floor(i / 3), P.shine);
  x = 80; ring(x + 8, 8, 3, P.glyphDark); set(img, x + 8, 8, P.glyph);
  x = 96; for (const [dx, dy] of [[6, 2], [6, 3], [7, 4], [7, 5], [8, 6], [8, 7], [9, 8], [8, 9], [8, 10]]) set(img, x + dx, dy, P.outline);
  x = 112; rect(img, x + 5, 6, 6, 4, P.shade); rect(img, x + 5, 6, 6, 1, P.dark); rect(img, x + 5, 9, 6, 1, P.light);
  // 2×2 at (0, 16): an emblem, a ring of glyphs round a lens.
  ring(16, 32, 10, P.outline); ring(16, 32, 9, P.glyphDark, true); ring(16, 32, 5, P.outline);
  rect(img, 14, 30, 4, 4, P.glyph); set(img, 15, 31, P.shine);
  // 2×2 at (32, 16): a panel of glyph lines.
  rect(img, 36, 20, 24, 24, P.outline); rect(img, 37, 21, 22, 22, P.shade);
  for (let row = 0; row < 5; row++) for (let i = 0; i < 18; i++) if (hash(i, row, 41) % 4) set(img, 39 + i, 24 + row * 4, hash(i, row, 42) % 3 ? P.glyphDark : P.glyph);
  // 3×3 at (64, 16): a sun of the makers, rays out of a ring.
  ring(88, 40, 14, P.outline); ring(88, 40, 13, P.light); ring(88, 40, 7, P.outline); ring(88, 40, 6, P.glyphDark);
  for (let a = 0; a < 360; a += 45) {
    const rad = a * Math.PI / 180;
    for (let r = 8; r <= 12; r++) set(img, Math.round(88 + r * Math.cos(rad)), Math.round(40 + r * Math.sin(rad)), P.glyph);
  }
  rect(img, 86, 38, 4, 4, P.glyph); set(img, 87, 39, P.shine);
  return img;
}

// A spire cache (on the floor): an alloy capsule, its lid seam, a small teal lamp on its front.
function cache() {
  const img = image(24, 16), glow = image(24, 16);
  for (let y = 3; y < 16; y++) for (let x = 2; x < 22; x++) {
    const edge = y === 3 || y === 15 || x === 2 || x === 21 || (y === 4 && (x === 3 || x === 20));
    if ((y === 3 && (x < 4 || x > 19))) continue;
    set(img, x, y, edge ? P.outline : y === 4 ? P.shine : y === 8 ? P.dark : y < 8 ? P.light : x < 5 ? P.light : P.base);
  }
  // Feet and handles.
  rect(img, 3, 15, 3, 1, P.outline); rect(img, 18, 15, 3, 1, P.outline);
  rect(img, 1, 9, 1, 3, P.outline); rect(img, 22, 9, 1, 3, P.outline);
  // The lamp.
  rect(img, 10, 10, 4, 3, P.outline); rect(img, 11, 11, 2, 1, [150, 255, 230]);
  for (const [x, y, a] of [[11, 11, 255], [12, 11, 255], [10, 11, 120], [13, 11, 120], [11, 10, 90], [12, 10, 90], [11, 12, 90], [12, 12, 90]]) set(glow, x, y, [120, 255, 220], a);
  // Glyph line on the lid.
  for (let i = 0; i < 10; i++) if (i % 3 !== 2) set(img, 7 + i, 6, P.glyph);
  return [img, glow];
}

// A relic's shrine (in the wall): a niche cut in the alloy round its cell, an arc coil on a plinth, sparks lit violet-blue.
function shrine() {
  const img = image(24, 24), glow = image(24, 24);
  rect(img, 4, 4, 16, 16, P.outline);
  rect(img, 5, 5, 14, 14, darker(P.dark, 0.7));
  rect(img, 5, 5, 14, 1, P.light); rect(img, 5, 5, 1, 14, P.shade);
  // Arch top.
  set(img, 5, 6, P.outline); set(img, 18, 6, P.outline);
  // Plinth.
  rect(img, 7, 16, 10, 3, P.outline); rect(img, 8, 16, 8, 2, P.light); rect(img, 8, 17, 8, 1, P.base);
  // The coil: copper turns round a dark core, upright.
  const copper = [214, 134, 72], copperDark = [140, 80, 42];
  rect(img, 11, 8, 2, 8, P.outline);
  for (let t = 0; t < 4; t++) { rect(img, 9, 8 + t * 2, 6, 1, copper); rect(img, 9, 9 + t * 2, 6, 1, copperDark); }
  // Arcs crawling on it.
  const arc = [180, 170, 255];
  for (const [x, y] of [[8, 7], [9, 6], [10, 7], [15, 9], [16, 8], [16, 12], [15, 13], [8, 12], [7, 11]]) { set(img, x, y, arc); set(glow, x, y, arc, 230); }
  for (const [x, y] of [[12, 6], [11, 6], [13, 7]]) { set(img, x, y, [230, 230, 255]); set(glow, x, y, [210, 210, 255], 255); }
  // Two small lamps at the niche's top corners.
  for (const x of [6, 17]) { set(img, x, 7, [150, 255, 230]); set(glow, x, 7, [120, 255, 220], 220); }
  return [img, glow];
}

// The spire's lamp: a lens set in the tip's alloy, round its cell.
function lamp() {
  const img = image(24, 24), glow = image(24, 24);
  rect(img, 8, 7, 8, 10, P.outline); rect(img, 9, 8, 6, 8, P.dark);
  const lens = [[11, 9], [12, 9], [10, 10], [11, 10], [12, 10], [13, 10], [10, 11], [11, 11], [12, 11], [13, 11], [10, 12], [11, 12], [12, 12], [13, 12], [11, 13], [12, 13], [11, 14], [12, 14]];
  for (const [x, y] of lens) { const core = (x === 11 || x === 12) && y >= 10 && y <= 12; set(img, x, y, core ? [230, 255, 255] : [110, 230, 240]); set(glow, x, y, core ? [220, 255, 255] : [90, 220, 240], core ? 255 : 200); }
  for (const [x, y] of [[11, 8], [12, 8], [9, 11], [14, 11], [11, 15], [12, 15]]) set(glow, x, y, [90, 220, 240], 90);
  return [img, glow];
}

// Item icons, 16 × 16.
function scrapIcon() {
  const img = image(16, 16);
  const shape = ['................', '................', '.....######.....', '....#lllllb#....', '...#lsssssbb#...', '..#lsssgssbbb#..', '..#lssgggssbb#..', '..#lsssgsssbb#..', '...#lssssssb#...', '....#lssssbb#...', '.....#lsssbb#...', '......#llbb#....', '.......####.....', '................', '................', '................'];
  const m = { '#': P.outline, l: P.shine, s: P.base, b: P.shade, g: P.glyph };
  shape.forEach((row, y) => [...row].forEach((ch, x) => m[ch] && set(img, x, y, m[ch])));
  return img;
}
function coilIcon() {
  const img = image(16, 16);
  const shape = ['................', '......a.........', '.....####.a.....', '....#kkkk#......', '...#cccccc#.....', '...#dddddd#..a..', '...#cccccc#.....', '..a#dddddd#.....', '...#cccccc#.....', '...#dddddd#.....', '...#cccccc#a....', '....#kkkk#......', '.....####.......', '......a.........', '................', '................'];
  const m = { '#': P.outline, c: [214, 134, 72], d: [140, 80, 42], k: [40, 44, 56], a: [190, 180, 255] };
  shape.forEach((row, y) => [...row].forEach((ch, x) => m[ch] && set(img, x, y, m[ch])));
  return img;
}

if (previewDir) fs.mkdirSync(previewDir, { recursive: true });
save(plates(false), 'planet/wall_alloy.png');
save(plates(true), 'planet/back_alloy.png');
save(tops(), 'planet/top_alloy.png');
save(details(), 'planet/detail_alloy.png');
for (const [name, make] of [['spireCache', cache], ['relicShrine', shrine], ['spireLamp', lamp]]) {
  const [img, glow] = make();
  save(img, `caves/${name}_0.png`, 10);
  save(glow, `caves/${name}_0_glow.png`, 10);
}
save(scrapIcon(), 'items/alloyScrap.png', 10);
save(coilIcon(), 'items/arcCoil.png', 10);
console.log('done');

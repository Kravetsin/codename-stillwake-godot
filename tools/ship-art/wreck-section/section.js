// The hull section: a chunk of a ship's side torn off, drawn with debris-art/gen.js's own Art (same
// palettes, shading, rivets, seams, tears, outline). node tools/ship-art/wreck-section/section.js [outDir]
// Writes wreck_hullSection.png (48x32) and the item icon hullSection.png (16x16) into outDir; without one, as
// game/art/debris/section_01.png and game/art/items/hullSection.png.
const fs = require('fs'), path = require('path');
const src = fs.readFileSync(path.join(__dirname, '..', 'debris-v1', 'gen.js'), 'utf8');
const lib = src.slice(src.indexOf('// ---------- PNG'), src.indexOf('const pieces ='));
const zlib = require('zlib');
const { Art, M, make, png, hex } = new Function('require', 'process', 'zlib', lib + '; return { Art, M, make, png, hex };')(require, { argv: [] }, zlib);
const out = process.argv[2]; if (out) fs.mkdirSync(out, { recursive: true });
const to = (name, game) => (out ? path.join(out, name) : path.join(path.join(__dirname, '..', '..', '..', 'game', 'art'), game));

const W = 46, H = 30;
const art = make(W, H, 77, (a, o) => {
  // The shape first: painted plates over a dark rib and a steel band, torn off ragged on the right.
  a.poly([[o, o + 2], [o + 4, o], [o + 34, o], [o + 37, o + 3], [o + 35, o + 6], [o + 41, o + 8], [o + 38, o + 12],
    [o + 46, o + 15], [o + 40, o + 19], [o + 43, o + 24], [o + 36, o + 30], [o + 9, o + 30], [o + 6, o + 26], [o, o + 27]], 'paint');
  a.rect(o, o + 19, o + 38, o + 22, 'dark');
  a.rect(o + 1, o + 23, o + 37, o + 29, 'steel');
  a.ring(o + 6.5, o + 9.5, 3.2, 4.6, 'steel'); a.disc(o + 6.5, o + 9.5, 3.2, 'glass');
  a.tear(.18);
  a.shade(.06);
  // Then the details over the shading: seams and rivets, the rib's holes, the porthole's glint.
  for (const x of [13, 26]) { a.seamV(o + x, o + 1, o + 18, 'paint'); for (let y = 3; y < 18; y += 4) { a.rivet(o + x - 2, o + y); a.rivet(o + x + 3, o + y); } }
  a.seamH(o + 14, o + 35, o + 10, 'paint');
  for (let x = 3; x < 37; x += 4) a.rivet(o + x, o + 24);
  a.seamV(o + 18, o + 24, o + 29, 'steel');
  for (let x = 4; x < 36; x += 7) a.hole(o + x, o + 20.5, .8);
  for (const [x, y] of [[5, 8], [6, 8], [5, 9]]) a.paint(o + x, o + y, M.glass[0]);
  for (const [x, y] of [[7, 10], [8, 11], [7, 11], [8, 9]]) a.paint(o + x, o + y, M.glass[3]);
  // A hazard stripe on the steel, and the ship's number half scraped off.
  for (let y = 25; y < 29; y++) for (let x = 25; x < 34; x++) if (((x + y) >> 1) % 2 === 0) a.paint(o + x, o + y, M.yellow[1]);
  for (const [x, y] of [[30, 3], [30, 4], [30, 5], [31, 3], [32, 3], [32, 4], [32, 5], [31, 5], [34, 4]]) a.paint(o + x, o + y, M.paint[3]);
  // Grime, and scorch along the tear.
  a.spots(6, 'grime', 3);
  for (const [x, y] of [[35, 7], [36, 9], [37, 10], [38, 14], [39, 16], [37, 21], [38, 22], [35, 27], [36, 26]]) a.paint(o + x, o + y, M.dark[3]);
  // Cables torn out with it.
  a.wire([[o + 38, o + 18], [o + 42, o + 20], [o + 44, o + 26]], '#d0463a');
  a.wire([[o + 37, o + 16], [o + 43, o + 17], [o + 46, o + 21]], '#e0b030');
});
fs.writeFileSync(to('wreck_hullSection.png', 'debris/section_01.png'), png(art.w, art.h, art.rgba()));

// The icon: the same section drawn small, its porthole, rib and torn end, centred in sixteen pixels.
const icon = new Art(16, 16, 5);
{
  const a = icon, o = 1, t = 3;
  a.poly([[o, t + 1], [o + 1, t], [o + 10, t], [o + 11, t + 2], [o + 13, t + 4], [o + 12, t + 6], [o + 13, t + 8], [o + 11, t + 10], [o + 2, t + 10], [o, t + 9]], 'paint');
  a.rect(o, t + 6, o + 11, t + 6, 'dark');
  a.rect(o, t + 7, o + 11, t + 9, 'steel');
  a.shade(0);
  a.paint(o + 2, t + 2, M.glass[1]); a.paint(o + 3, t + 2, M.glass[0]); a.paint(o + 2, t + 3, M.glass[2]); a.paint(o + 3, t + 3, M.glass[1]);
  a.seamV(o + 6, t + 1, t + 5, 'paint');
  a.paint(o + 4, t + 8, M.steel[3]); a.paint(o + 8, t + 8, M.steel[3]);
  a.paint(o + 9, t + 9, M.yellow[1]); a.paint(o + 10, t + 8, M.yellow[1]);
  a.wire([[o + 12, t + 5], [o + 14, t + 7]], '#d0463a');
}
fs.writeFileSync(to('hullSection.png', 'items/hullSection.png'), png(16, 16, icon.rgba()));
console.log('written', out || 'game/art');

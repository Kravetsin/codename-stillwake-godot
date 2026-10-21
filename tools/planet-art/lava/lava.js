// The lava's tiles for art/planet, after the cave concepts' core (cave-content/strips.js): value noise in 6-pixel cells
// for its patches, thin cracks where a 9-pixel noise crosses its middle, in the concept's colours as its page shows them.
// The lava view draws over these; they are what the tile layers hold. 128 x 64, tileable (noise wrapped on the sheet).
const fs = require('fs');
const path = require('path');
const P = require('../png');
const out = process.argv[2] || path.join(__dirname, '..', '..', '..', 'game', 'art', 'planet'); // node tools/planet-art/lava/lava.js [planetDir]
const W = 128, H = 64;

function hash(x, y, s) {
  let h = Math.imul(x | 0, 374761393) + Math.imul(y | 0, 668265263) + Math.imul(s | 0, 1442695041);
  h = Math.imul(h ^ (h >>> 13), 1274126177); h ^= h >>> 16; return (h >>> 0) / 4294967296;
}
// The concept's value noise, its lattice wrapped on the sheet so the tiles repeat seamlessly.
function vnoise(x, y, cell, s) {
  const nx = Math.round(W / cell), ny = Math.round(H / cell), sx0 = W / nx, sy0 = H / ny;
  const gx = Math.floor(x / sx0), gy = Math.floor(y / sy0), fx = x / sx0 - gx, fy = y / sy0 - gy;
  const ex = fx * fx * (3 - 2 * fx), ey = fy * fy * (3 - 2 * fy);
  const at = (i, j) => hash(((i % nx) + nx) % nx, ((j % ny) + ny) % ny, s);
  const a = at(gx, gy), b = at(gx + 1, gy), c = at(gx, gy + 1), d = at(gx + 1, gy + 1);
  return a + (b - a) * ex + (c - a) * ey + (a - b - c + d) * ex * ey;
}
const hex = (h) => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)];
// As the concept's page shows them, lit and glowing: deep orange, orange, gold patches, pale yellow cracks.
const HOT = { deep: hex('#ff7a34'), mid: hex('#ffa040'), gold: hex('#ffc454'), crack: hex('#ffe680') };
// Its crust, for the back wall no one sees: the concept's own unlit colours.
const CRUST = { deep: hex('#4a120a'), mid: hex('#8a2210'), gold: hex('#d8401a'), crack: hex('#ffb040') };

function sheet(pal) {
  const img = P.create(W, H);
  for (let y = 0; y < H; y++)
    for (let x = 0; x < W; x++) {
      const n = vnoise(x, y, 6, 77), crack = Math.abs(vnoise(x, y, 9, 78) - 0.5) < 0.05;
      const c = crack ? pal.crack : n > 0.6 ? pal.gold : n > 0.4 ? pal.mid : pal.deep, i = (y * W + x) * 4;
      img.data[i] = c[0]; img.data[i + 1] = c[1]; img.data[i + 2] = c[2]; img.data[i + 3] = 255;
    }
  return img;
}

P.encode(sheet(HOT), path.join(out, 'wall_lava.png'));
P.encode(sheet(CRUST), path.join(out, 'back_lava.png'));
// No living edge: the lava view draws its surface, haze and embers.
P.encode(P.create(W, H), path.join(out, 'top_lava.png'));
// Its details, never seen under the lava view: a few gold patches.
const details = P.create(W, H);
for (let y = 0; y < H; y++)
  for (let x = 0; x < W; x++)
    if (vnoise(x, y, 6, 91) > 0.72) { const i = (y * W + x) * 4; details.data.set([...HOT.gold, 255], i); }
P.encode(details, path.join(out, 'detail_lava.png'));
console.log('ok');

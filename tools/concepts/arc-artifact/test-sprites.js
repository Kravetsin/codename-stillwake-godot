const L = require('./lib'); const { DESIGNS, icon } = require('./designs'); const P = require('../../planet-art/png');
const tiles = [];
for (const d of DESIGNS) { const a = d.piece(1).spr, b = d.piece(2).spr, c = icon(d); tiles.push([c, 8], [a, 5], [b, 3]); }
const cellW = 170, cellH = 170, cols = 6, rows = Math.ceil(tiles.length / cols);
const o = P.create(cellW * cols, cellH * rows); for (let i = 0; i < o.w * o.h; i++) { o.data[i*4]=22;o.data[i*4+1]=20;o.data[i*4+2]=30;o.data[i*4+3]=255; }
tiles.forEach(([s, k], n) => { const img = s.png(P), ox = (n % cols) * cellW + 4, oy = Math.floor(n / cols) * cellH + 4;
  for (let y = 0; y < img.h * k; y++) for (let x = 0; x < img.w * k; x++) { const si = (Math.floor(y / k) * img.w + Math.floor(x / k)) * 4; if (!img.data[si + 3]) continue; const di = ((oy + y) * o.w + ox + x) * 4; if (ox + x >= o.w || oy + y >= o.h) continue; img.data.copy(o.data, di, si, si + 4); } });
P.encode(o, 'out/test-sprites.png'); console.log('ok');

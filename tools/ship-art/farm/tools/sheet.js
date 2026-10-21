// node sheet.js out.png scale bgHex file1 file2 ... ; lays images in a row-wrapped sheet
const { read, write } = require('./png.js');
const [, , outF, sc, bg, ...files] = process.argv; const S = +sc;
const ims = files.map(read); const W = 1400; let x = 4, y = 4, rowH = 0; const pos = [];
for (const im of ims) { if (x + im.w * S > W) { x = 4; y += rowH + 6; rowH = 0; } pos.push([x, y]); x += im.w * S + 6; rowH = Math.max(rowH, im.h * S); }
const H = y + rowH + 4; const data = new Uint8Array(W * H * 4); const b = [1, 3, 5].map(i => parseInt(bg.slice(i, i + 2), 16));
for (let i = 0; i < W * H; i++) { data[i * 4] = b[0]; data[i * 4 + 1] = b[1]; data[i * 4 + 2] = b[2]; data[i * 4 + 3] = 255; }
ims.forEach((im, k) => { const [ox, oy] = pos[k]; for (let yy = 0; yy < im.h * S; yy++) for (let xx = 0; xx < im.w * S; xx++) { const s = (((yy / S) | 0) * im.w + ((xx / S) | 0)) * 4, a = im.data[s + 3] / 255; const d = ((oy + yy) * W + ox + xx) * 4; for (let c = 0; c < 3; c++) data[d + c] = Math.round(im.data[s + c] * a + data[d + c] * (1 - a)); } });
write(outF, { w: W, h: H, data });

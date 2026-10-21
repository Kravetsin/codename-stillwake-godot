// node sheet.js out.png scale cols file... : a contact sheet over dark grey
const P = require('../../planet-art/png'); const [, , out, sc, cols, ...files] = process.argv; const k = +sc, nc = +cols;
const imgs = files.map((f) => P.scale(P.decode(f), k)); const cw = Math.max(...imgs.map((i) => i.w)) + 8, chh = [];
const rows = Math.ceil(imgs.length / nc); for (let r = 0; r < rows; r++) chh.push(Math.max(...imgs.slice(r * nc, r * nc + nc).map((i) => i.h)) + 8);
const o = P.create(cw * nc, chh.reduce((a, b) => a + b, 0)); for (let i = 0; i < o.w * o.h; i++) { o.data[i * 4] = 40; o.data[i * 4 + 1] = 40; o.data[i * 4 + 2] = 46; o.data[i * 4 + 3] = 255; }
let y0 = 0; imgs.forEach((im, n) => { const r = Math.floor(n / nc), c = n % nc; if (c === 0 && n) y0 += chh[r - 1]; for (let y = 0; y < im.h; y++) im.data.copy(o.data, ((y0 + 4 + y) * o.w + c * cw + 4) * 4, y * im.w * 4, (y + 1) * im.w * 4); });
P.encode(o, out);

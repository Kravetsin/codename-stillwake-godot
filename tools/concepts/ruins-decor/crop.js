// node crop.js in.png out.png x y w h scale
const P = require('../../planet-art/png'); const [, , i, o, x, y, w, h, k] = process.argv; const s = P.decode(i), c = P.create(+w, +h);
for (let j = 0; j < +h; j++) s.data.copy(c.data, j * +w * 4, ((+y + j) * s.w + +x) * 4, ((+y + j) * s.w + +x + +w) * 4); P.encode(P.scale(c, +k || 1), o);

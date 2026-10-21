// node crop.js in.png out.png x y w h scale : a piece of a picture, enlarged
const { loadPng, img, blit, savePng } = require('./lib.js');
const [, , i, o, x, y, w, h, k] = process.argv; const s = loadPng(i), c = img(+w, +h);
for (let yy = 0; yy < +h; yy++) for (let xx = 0; xx < +w; xx++) { const si = ((+y + yy) * s.w + +x + xx) * 4; c.data.set(s.data.subarray(si, si + 4), (yy * +w + xx) * 4); }
savePng(o, blit(img(+w * +k, +h * +k), c, 0, 0, { scale: +k }));

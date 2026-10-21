// node try.js out.png scale : every look's base and composed frames in a sheet, for checking by eye
const { LOOKS } = require('./tank.js');
const { img, blit, savePng } = require('./lib.js');
const [, , outF, sc] = process.argv; const S = +sc || 6;
const rows = Object.values(LOOKS).map(f => f());
const n = Math.max(...rows.map(r => r.frames.length)) + 1;
const W = n * (32 * S + 8) + 8, H = rows.length * (32 * S + 8) + 8;
const im = img(W, H, '#202430');
rows.forEach((r, j) => {
  const y = 8 + j * (32 * S + 8);
  blit(im, r.base, 8, y, { scale: S });
  r.frames.forEach((f, i) => { blit(im, r.base, 8 + (i + 1) * (32 * S + 8), y, { scale: S }); blit(im, f, 8 + (i + 1) * (32 * S + 8), y, { scale: S }); });
});
savePng(outF, im);

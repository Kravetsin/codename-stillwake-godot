// node try.js out.png scale : every drone look's frames, lights and icons in a sheet, for checking by eye
const { build, LOOKS, W, H } = require('./drones.js');
const { img, blit, savePng } = require('./lib.js');
const [, , outF, sc] = process.argv; const S = +sc || 8;
const rows = Object.keys(LOOKS).map(build);
const cols = 9, cw = 18 * S + 8, ch = 16 * S + 8;
const im = img(cols * cw + 8, rows.length * ch + 8, '#2a2f3a');
rows.forEach((r, j) => {
  const y = 8 + j * ch; let i = 0;
  const put = (p, over) => { blit(im, p, 8 + i * cw, y, { scale: S }); if (over) blit(im, over, 8 + i * cw, y, { scale: S }); i++; };
  r.fly.forEach(f => put(f)); put(r.dock); put(r.fly[0], r.work); put(r.fly[0], r.low); put(r.icon); put(r.empty);
});
savePng(outF, im);

// node trybay.js out.png droneLook : each bay look empty, then full with drones charging at 1..4 lamps, large
const D = require('./drones.js'), B = require('./bays.js'); const { img, blit, savePng, loadPng } = require('./lib.js');
const [, , out, did] = process.argv; const K = 7, dr = D.build(did || 'A');
const ids = ['cur', ...Object.keys(B.LOOKS)];
const im = img(2 * (32 * K + 10) + 10, ids.length * (32 * K + 10) + 10, '#2a2f3a');
const ch = B.charges(), glow = B.padGlow();
ids.forEach((id, j) => {
  const y = 10 + j * (32 * K + 10);
  const base = id === 'cur' ? loadPng(require('path').join(__dirname, '..', '..', '..', '..', 'game', 'art') + '/machine_droneBay.png') : B.build(id).base;
  blit(im, base, 10, y, { scale: K });
  blit(im, base, 20 + 32 * K, y, { scale: K });
  if (id === 'cur') return;
  [0, 1, 2, 3].forEach(i => {
    if (i === 1) return; // one pad empty
    const [px, py] = B.PADS[i], [gx, gy] = B.GLOWS[i], [lx, ly] = B.LAMPS[i], ox = 20 + 32 * K;
    blit(im, dr.dock, ox + px * K, y + py * K, { scale: K });
    if (i !== 3) blit(im, glow, ox + gx * K, y + gy * K, { scale: K });
    blit(im, i === 0 ? ch.low : ch.levels[i + 1], ox + lx * K, y + ly * K, { scale: K });
  });
});
savePng(out, im);

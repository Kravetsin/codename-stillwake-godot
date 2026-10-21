// node big.js out.png A B C : each look's first flight frame, its docked pose and its icon, large, for checking by eye
const { build } = require('./drones.js'); const { img, blit, savePng } = require('./lib.js');
const [, , out, ...ids] = process.argv; const K = 10;
const im = img(18 * K + 14 * K + 16 * K + 40, ids.length * (16 * K + 10) + 10, '#3a3f4a');
ids.forEach((id, j) => { const r = build(id), y = 10 + j * (16 * K + 10); blit(im, r.fly[0], 10, y, { scale: K }); blit(im, r.dock, 20 + 18 * K, y, { scale: K }); blit(im, r.icon, 30 + 32 * K, y, { scale: K }); });
savePng(out, im);

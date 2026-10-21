// quick look: every concept's pieces, closed row then open row, x6, on a dark ground
const path = require('path');
const { img, blit, savePng } = require('./lib.js');
const { CONCEPTS, MASKS } = require('./hatch.js');
const ids = Object.keys(CONCEPTS), K = 6, G = 8;
const W = (84 + 48 + 4) * K, Hh = ids.length * 2 * 12 * K;
const im = img(W, Hh, '#3a3f4a');
ids.forEach((id, i) => [false, true].forEach((open, o) => {
  // each piece alone, then a 3-wide run
  MASKS.forEach((m, k) => blit(im, CONCEPTS[id].piece(open, m, 0), (k * 20) * K, ((i * 2 + o) * 12) * K, { scale: K }));
  [2, 10, 8].forEach((m, k) => blit(im, CONCEPTS[id].piece(open, m, 0), (84 + k * 16) * K, ((i * 2 + o) * 12) * K, { scale: K }));
}));
savePng(path.join(__dirname, '..', 'tmp', 'try.png'), im);

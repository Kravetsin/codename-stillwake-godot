// node icon.js <game items dir> <out dir> — the soil clod's 16x16 item icon, and a preview beside its neighbours at x8
const path = require('path');
const T = path.join(__dirname, '../farm/tools');
const { Pic, img, blit, savePng } = require(path.join(T, 'lib.js'));
const { ITEM_OUTLINE } = require(path.join(T, 'pal.js'));
const [GAME, OUT] = process.argv.slice(2);
// the debris pictures' earth, roots, greens and pebbles
const M = {
  k: '#22150d', m: '#3a2418', n: '#553624', o: '#734b31', p: '#946644', q: '#b5875c',
  1: '#1c4626', 2: '#2c6a3a', 3: '#3f8f4f', 4: '#6fbf73', 5: '#a3dc8c',
  r: '#a88c64', R: '#cdb488', x: '#837f76', X: '#aaa69c',
};
// a lumpy clod torn from a tray, lit from the upper left, a sprout on top, roots trailing from its underside
const rows = [
  '................',
  '.....45..45.....',
  '.....4433443....',
  '......2.32......',
  '........3.......',
  '........2.......',
  '....pqp.3qp.....',
  '..pqqpoo2pooo...',
  '.pqppoqponoonm..',
  '.qpoXxoonopnnmm.',
  '.poooxonnnonmmm.',
  '..onoonnmnnmmk..',
  '..nnmmknnmmkk...',
  '...rmk..rmk.....',
  '..R....r..R.....',
  '................',
];
const p = new Pic(16, 16); p.stamp(0, 0, rows, M);
p.each((x, y) => { if (x === 0 || y === 0 || x === 15 || y === 15) throw new Error(`fill on the border at ${x},${y}`); });
p.outline(ITEM_OUTLINE);
p.save(path.join(OUT, 'soilClod.png'));
const S = 8, line = [p, ...['cableRack', 'tankCluster', 'hullSection', 'soil', 'seed', 'rock'].map(i => Pic.load(path.join(GAME, i + '.png')))];
const out = img(line.length * (16 * S + 8) + 8, 16 * S + 16, '#1a2230');
line.forEach((q, i) => blit(out, q, 8 + i * (16 * S + 8), 8, { scale: S }));
savePng(path.join(OUT, 'preview.png'), out);

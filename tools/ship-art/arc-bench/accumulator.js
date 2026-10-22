// The accumulator's 16 px icons (0099), a placeholder in the items' outlined style till the user picks one: a battery of the
// shard's black glass between copper bands, its core a crack of the shard alight violet-white; empty, the crack gone cold.
// node accumulator.js [itemsDir] writes accumulator.png and accumulator_empty.png (game/art/items by default).
const fs = require('fs'), path = require('path');
const P = require(path.join(__dirname, '..', '..', 'homes-art', 'png.js'));
const OUT = process.argv[2] || path.join(__dirname, '..', '..', '..', 'game', 'art', 'items');

const ROWS = [
  '......oooo....v.',
  '.....oCHHCo.....',
  '..oooooooooooo..',
  '..oCHHHHHHHHco..',
  '..occcccccccco..',
  '..ogGggwgggddo..',
  '..ogGgwWwgggdo..',
  '..ogGgwWWwggdo..',
  '..ogGggwWwggdo..',
  'v.ogGgwWWwggdo..',
  '..ogGgwWwgggdo..',
  '..ogGggwWwggdo..',
  '..ogGgggwgggdo..',
  '..occcccccccco..',
  '..oCHHHHHHHHco..',
  '..oooooooooooo..',
];
const hx = (h) => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)];
const BODY = { o: '#10101a', C: '#b9783a', H: '#e3a656', c: '#8a5228', g: '#241c44', G: '#372c66', d: '#151027' };
const LIT = { ...BODY, W: '#fdfaff', w: '#b493ff', v: '#e4d6ff' };
const COLD = { ...BODY, W: '#3a2d66', w: '#2e2456' };

function paint(colours) {
  const im = P.create(16, 16);
  ROWS.forEach((row, y) => [...row].forEach((ch, x) => {
    if (ch === '.' || !colours[ch]) return;
    const [r, g, b] = hx(colours[ch]), o = (y * 16 + x) * 4;
    im.data[o] = r; im.data[o + 1] = g; im.data[o + 2] = b; im.data[o + 3] = 255;
  }));
  return im;
}

fs.mkdirSync(OUT, { recursive: true });
P.encode(paint(LIT), path.join(OUT, 'accumulator.png'));
P.encode(paint(COLD), path.join(OUT, 'accumulator_empty.png'));
console.log('painted accumulator, accumulator_empty into', OUT);

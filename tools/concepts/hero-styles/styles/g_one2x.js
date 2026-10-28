// G: the same head, hair and faces as F, drawn at half the body's pixel (2x): 26x24 in the helmet's 13x12.
const { gen } = require('../gen.js');
const g = gen(2, +(process.env.LINE || 1));
const blank = () => Array.from({ length: 24 }, () => '.'.repeat(26));
const mark = (spots) => { const r = blank().map(x => [...x]); for (const [x, y, k] of spots) r[y][x] = k; return r.map(x => x.join('')); };
const row = (y, x0, keys) => [...keys].map((k, i) => [x0 + i, y, k]).filter(s => s[2] !== '.');
module.exports = {
  name: 'Одна голова, 2×',
  density: 2,
  fixed: { R: '#8fb4d4', B: '#d98a80' },
  headJoint: [13, 24],
  head: g.head,
  faces: [
    mark([...row(8, 16, '3333'), ...row(10, 18, 'DD'), ...row(11, 18, 'we'), ...row(12, 18, 'we'), [23, 14, 'D'], ...row(17, 20, 'DD')]), // calm
    mark([...row(8, 17, '3333'), ...row(9, 16, '33333'), ...row(11, 18, 'De'), ...row(12, 18, 'we'), [23, 14, 'D'], ...row(17, 19, 'DDD')]), // stern
    mark([[21, 9, 'e'], ...row(10, 18, 'eeee'), ...row(11, 18, 'we'), ...row(12, 18, 'we'), ...row(13, 15, 'BB'), [23, 14, 'D'], ...row(17, 20, 'Dm')]), // soft
  ],
  hairs: ['short', 'long', 'curls', 'bun'].map(n => g.hairs[n]),
  jumpsuit: require('./e_soft.js').jumpsuit,
  examples: [
    { suit: 1, skin: 2, bald: true, face: 0 },
    { suit: 1, skin: 6, hair: 2, hairColour: 0, face: 1 },
    { suit: 2, skin: 0, hair: 1, hairColour: 2, face: 2 },
    { suit: 0, skin: 2, hair: 0, hairColour: 3, face: 0 },
    { suit: 3, skin: 4, hair: 3, hairColour: 0, face: 2 },
    { suit: 0, skin: 2, hair: 0, hairColour: 3, face: 0, turn: -0.35 },
    { suit: 2, skin: 0, face: 2, visor: true },
  ],
};

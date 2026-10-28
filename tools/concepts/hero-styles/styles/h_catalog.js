// H: F's catalog: every hair on one skin and colour, then every face and beard, then skins and colours mixed.
const F = require('./f_one1x.js');
module.exports = {
  ...F,
  name: 'Каталог',
  examples: [
    ...F.hairs.map((_, hair) => ({ suit: 0, skin: 2, hair, hairColour: 1, face: 0 })),
    ...F.faces.map((_, face) => ({ suit: 0, skin: 1, hair: 5, hairColour: 2, face })),
    ...F.beards.map((_, i) => ({ suit: 0, skin: 1, hair: 5, hairColour: 2, face: 0, beard: i + 1 })),
    ...[0, 1, 2, 3, 4, 5, 6].map(skin => ({ suit: 0, skin, hair: 0, hairColour: skin % 6, face: 0 })),
  ],
};

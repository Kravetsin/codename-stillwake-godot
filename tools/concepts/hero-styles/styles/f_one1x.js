// F: the chosen head, as tools/hero-art/parts.js holds it: style E, one head for everyone inside the helmet's 13x12
// outline at the body's pixel (1x), heroes told apart by hair and face as in Starbound.
const A = require('../../../hero-art/parts.js');

module.exports = {
  name: 'Одна голова, 1×',
  density: 1,
  headJoint: A.HEAD_JOINT,
  head: A.head,
  faces: A.faces,
  // hair and beards have their own template: its top left from the head's
  hairs: A.hairs.map(rows => ({ at: [A.HEAD_JOINT[0] - A.HAIR_JOINT[0], A.HEAD_JOINT[1] - A.HAIR_JOINT[1]], rows })),
  beards: A.beards.map(rows => ({ at: [A.HEAD_JOINT[0] - A.HAIR_JOINT[0], A.HEAD_JOINT[1] - A.HAIR_JOINT[1]], rows })),
  jumpsuit: A.jumpsuit,
  examples: [
    { suit: 1, skin: 6, hair: 1, hairColour: 0, face: 0, beard: 1 },
    { suit: 2, skin: 0, hair: 2, hairColour: 2, face: 2 },
    { suit: 0, skin: 2, hair: 0, hairColour: 3, face: 3 },
    { suit: 3, skin: 4, hair: 4, hairColour: 0, face: 4 },
    { suit: 1, skin: 1, hair: 5, hairColour: 1, face: 0, beard: 2 },
    { suit: 0, skin: 3, hair: 3, hairColour: 4, face: 0, turn: -0.35 },
    { suit: 2, skin: 0, face: 2, visor: true },
  ],
};

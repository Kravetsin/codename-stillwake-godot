'use strict';
// The hero without a suit and the face behind the visor: every picture as pixel rows, and the ramps that recolour them.
// Keys: suit l s d o (Suits.ShadesOf(Suits.Drawn): light, own, shade, outline), skin L S D O, hair 1 2 3 4 (light,
// own, shade, outline); every other key keeps its colour. A picture is recoloured by swapping each shade of a drawn
// ramp for the same shade of another, as SuitArt.Of does for suits.

// Suits.ShadesOf(Suits.Drawn); every jumpsuit colour is Suits.ShadesOf of a Suits.All colour.
const SUIT_DRAWN = ['#ebc178', '#e0a030', '#a17323', '#5e4314'];
const SUITS = [0xe8b04a, 0x5fc8c0, 0xa08cf0, 0x8cd06a];

// Skin tones, light to deep: light, own, shade, outline. The pictures are drawn in SKIN_DRAWN.
const SKINS = [
  ['#f8e0cf', '#efc8ae', '#d3a189', '#8c5c4b'],
  ['#f3d0a9', '#e5b68b', '#c68e69', '#7e5039'],
  ['#ecc08e', '#dba570', '#b98052', '#71472a'],
  ['#d9aa7d', '#c38d5f', '#9e6c45', '#5f3b24'],
  ['#c28d5f', '#a87242', '#83542e', '#4b2d17'],
  ['#9e6943', '#835231', '#623a22', '#381e0f'],
  ['#74492f', '#5b3822', '#432716', '#24130a'],
];
const SKIN_DRAWN = 2;

// Hair colours: black, brown, copper, blond, grey, dyed rose. The pictures are drawn in HAIR_DRAWN.
const HAIRS = [
  ['#4c4855', '#302d36', '#1f1d24', '#100f13'],
  ['#7d5636', '#5c3c24', '#412817', '#25170d'],
  ['#d57c42', '#ab562c', '#7e3b1d', '#48210f'],
  ['#f1d78e', '#d6b262', '#aa863e', '#6b5121'],
  ['#e3e3e7', '#b6b8c0', '#888a96', '#4b4d57'],
  ['#ea8db6', '#c46390', '#93466a', '#56253a'],
];
const HAIR_DRAWN = 1;

// Colours no ramp swaps: the gear's greys (the suit's), the eyes, the visor's glass.
const FIXED = {
  K: '#14171d', g: '#262b35', G: '#3d4452', h: '#6a7385', H: '#9aa3ad',
  e: '#16121a', w: '#e9e4de', m: '#7a3b34',
  v: '#1d2836/80', V: '#8fb6d6/210', y: '#cfe6f5/235',
  // a blush
  B: '#d98a80',
  // the cape's wool: light, own, shade, outline
  p: '#c49a68', q: '#9a7148', r: '#6e4f33', t: '#3b2a1b',
  // the cape's lining
  a: '#6f9e98', b: '#4f7a75', c: '#375752', f: '#1f3330',
  // the fur of its collar
  u: '#e2d6bf', x: '#bfae8e', z: '#8a7a60', n: '#4e4436',
};

// The head's slot shares the helmet's template: 13x12, turning at the neck, (6.5, 12), its bottom middle. One head for
// everyone, heroes told apart by hair and face. Every opaque pixel of the head and a face lies inside the helmet's, so
// a suited head shows only through the visor; the eye sits on row 6, the middle of the visor's window (rows 4 to 7).
// Lit from the upper front: the outline only on the shaded back and underside, the lit front left open.
const HEAD_JOINT = [6.5, 12];

const head = [
  '....SSSSS....',
  '..DSLLLLLS...',
  '.DSSLLLLLLS..',
  '.DSSSSLLLLS..',
  'ODSSSSSSSSSS.',
  'ODSSSSSSSSSS.',
  'ODSDDSSSSSSS.',
  'ODDSDSSSSSSSS',
  '.ODDDSSSSSSS.',
  '..ODSSSSSSSS.',
  '...ODDSSSSSO.',
  '....ODOOOOO..',
];

// Faces, in the head's template: eyes (e, w), lips (m) and a blush (B) keep their colours; brows take the hair's ramp,
// so they match the hair; the rest the skin's.
const marks = (w, h, at) => spots => {
  const rows = Array.from({ length: h }, () => [...'.'.repeat(w)]);
  for (const [x, y, keys] of spots) [...keys].forEach((k, i) => (rows[y + at][x + at + i] = k));
  return rows.map(r => r.join(''));
};
const faceOf = marks(13, 12, 0);
const faces = [
  faceOf([[7, 5, '33'], [8, 6, 'we'], [10, 9, 'D']]), // calm
  faceOf([[7, 5, '333'], [8, 6, 'De'], [9, 9, 'DD']]), // stern: a heavy brow, a narrowed eye, a set mouth
  faceOf([[7, 5, '33'], [10, 5, 'e'], [8, 6, 'we'], [7, 7, 'B'], [10, 9, 'Dm']]), // soft: a lash flicked forward, a blush, lips
  faceOf([[8, 4, '33'], [8, 6, 'ee'], [9, 8, 'D'], [10, 9, 'wD']]), // grin: a raised brow, a happy eye, teeth
  faceOf([[7, 5, '33'], [8, 6, 'we'], [7, 7, 'D'], [9, 7, 'D'], [8, 8, 'D'], [10, 9, 'D']]), // freckles
];

// Hair has its own template, bigger than the helmet, which hides it: 16x18, the head's pixel (x, y) its (x + 2, y + 2),
// so it turns at (8.5, 14). Keys 1 lit, 2 own, 3 shade, 4 line where it meets the dark (over the skin its edge is
// shade), R a cool rim of light down the back.
const HAIR_JOINT = [8.5, 14];
const hairs = [
  [ // short, parted, combed up at the front
    '................',
    '.......11111....',
    '.....111111111..',
    '...412222222211.',
    '..4222222222221.',
    '..422322222233..',
    '..42333223......',
    '..423334........',
    '..423...........',
    '..43............',
    '...3............',
  ],
  [ // curls, full and round: flat, scattered lights above and shades below, the edge bumpy
    '.....1..11.1....',
    '...1121112112...',
    '..421222122212..',
    '.42122212221221.',
    '.42322232223222.',
    '.43222322232232.',
    '.423223223323...',
    '.432232.........',
    '.4232...........',
    '..43............',
    '...3............',
  ],
  [ // long, from a side parting down past the shoulders
    '................',
    '......11111.....',
    '....111111111...',
    '...411222222211.',
    '..42222222222221',
    '.422223222222233',
    '.42223322223....',
    '.422233.........',
    '.42223..........',
    '.42233..........',
    '.42233..........',
    '.42233..........',
    '.42333..........',
    '..4233..........',
    '..4233..........',
    '..4334..........',
    '...44...........',
  ],
  [ // pulled back into a bun
    '.11.............',
    '1122...1111.....',
    '42223.11111111..',
    '423334222222211.',
    '.43342222222221.',
    '..42222222222233',
    '..4222322333....',
    '..42333.........',
    '..423...........',
    '..43............',
    '...3............',
  ],
  [ // a ponytail tied at the back
    '................',
    '.......11111....',
    '.....111111111..',
    '...412222222211.',
    '..4222222222221.',
    '.3G2222222222233',
    '422G222322333...',
    '42234333........',
    '4223.43.........',
    '4223..3.........',
    '4233............',
    '.433............',
    '.434............',
    '..4.............',
  ],
  [ // cropped close: the hair's shade hugging the skull, a few lights on top
    '................',
    '................',
    '......23332.....',
    '....23332333....',
    '...4333233323...',
    '...433333333....',
    '..43333333......',
    '..433333........',
    '..433...........',
    '..43............',
  ],
];

// Beards, in the hair's template and ramp, so they take the hair's colour; drawn over the face, only bare, as hair is.
// Spots are given in the head's pixels; the mouth (10, 9) is left open for the face's.
const beardOf = marks(16, 18, 2);
const beards = [
  beardOf([[4, 8, '33'], [4, 9, '3333'], [4, 10, '3333333']]), // short, along the jaw
  beardOf([[5, 7, '3'], [5, 8, '33'], [10, 8, '22'], [4, 9, '32222'], [11, 9, '2'], [4, 10, '32222222'], [5, 11, '3322223'],
    [7, 12, '4334']]), // full, hanging under the chin
];

// The helmet's visor (hero_helmet.png's E F pixels) as glass the face shows through, its glint kept off the eye;
// hero_helmet_shell is the helmet without them.
const visor = [
  '.............',
  '.............',
  '.............',
  '.............',
  '........yvvv.',
  '.......Vvvvv.',
  '.......vvvvv.',
  '........vvvv.',
  '.............',
  '.............',
  '.............',
  '.............',
];

// The jumpsuit: the suit's parts' templates and joints, cloth in the suit's ramp, hands in the skin's, the belt, cuffs and
// shoes in gear. Lit as the head is: no line on the lit front, folds in shade.
const jumpsuit = {
  torso: [ // joint (6, 0)
    '...odllld...',
    '.oddslllll..',
    'odsssslllll.',
    'odsssssllll.',
    'odssdssslls.',
    'odssdssssls.',
    'odsssssssss.',
    'oodssssssss.',
    'ogGGGGHHHHG.',
    'oodsssssdss.',
    'oodssssdsss.',
    '.odssssssss.',
    '.ooddssssdo.',
    '..oooooooo..',
    '............',
  ],
  upperArm: ['.lll.', 'oslll', 'osslo', 'osslo', 'osslo', 'odslo', 'odsso', 'odsso', '.ooo.'], // joint (2.5, 2.5)
  forearm: ['.sls.', 'osslo', 'osslo', 'odsso', 'oGGGo', '.DLS.', 'DSLLS', 'DSSLS', 'ODSSO', '.OOO.'], // joint (2.5, 2.9); a cuff, the fist
  thigh: ['.slll.', 'osslls', 'osslls', 'ossslo', 'ossslo', 'odsslo', 'odsslo', 'odssso', 'odssso', 'oddsso', 'oddsso', '.oooo.'], // joint (3, 2.5)
  shin: ['.oslso', 'osslso', 'osslso', 'osslso', 'odsslo', 'odsslo', 'oGGGGo', '.gHHHg..', 'gGHHHHgg.', 'gGGGGGGHg', 'KgggggggK'], // joint (3, 2.18); a shoe on the boot's row
};

// The cape (a prototype, Cape.cs): its outside and its lining, laid over its grid of cloth, 14x30, across from its
// front edge at the neck (column 0) to its back edge, down from the shoulders to the hem. The outside shows a strip of
// lining down its front edge, where it opens; folds run down from a third of the way, longer toward the back.
const CAPE_W = 14, CAPE_H = 30;
function capeSide(light, own, shade, line, lining) {
  return Array.from({ length: CAPE_H }, (_, r) => [...Array(CAPE_W)].map((_, x) => {
    if (r === 0 || r === CAPE_H - 1 || x === CAPE_W - 1) return line;
    if (lining && x === 0) return 'c';
    if (lining && x === 1) return 'b';
    if (lining && x === 2) return line;
    if (r === CAPE_H - 2) return shade;
    if (r <= 2) return light;
    if ((x === 6 && r >= 9) || (x === 10 && r >= 6) || (x === 8 && r >= 18)) return shade;
    if ((x === 3 || x === 4) && r < 12) return light;
    return own;
  }).join(''));
}
const cape = { outside: capeSide('p', 'q', 'r', 't', true), lining: capeSide('a', 'b', 'c', 'f', false) };
// The hood, down on the back (Cape.cs): a bag of the wool laid over its own small cloth, its opening (row 0) under the
// collar, front edge (column 0) at the neck; lit from above and in front, rounding into shade at its back and its tip.
const hood = [
  'tttttttttt',
  'tpppppqqqt',
  'tppppqqqqt',
  'tppqqqqqrt',
  'tpqqqqqqrt',
  'tqqqqqqrrt',
  '.tqqqrqqrt',
  '.tqqqrqrt.',
  '..trrrrt..',
  '...tttt...',
];
// The fur collar round the neck over the cape's top, back (column 0) to the throat, where it is clasped: tufted on
// top, thickest at the back, thinning toward the clasp. Its top left at (-10.5, -Neck - 1.5) of the figure.
const collar = [
  '..n.nn.n.n.....',
  '.nunuunuunn....',
  'nuuuxuuuxuunn..',
  'nuxuxxuxxuuuun.',
  'nxxzxxzxxzxxxzn',
  '.nzznzznzznnnn.',
];

module.exports = { SUIT_DRAWN, SUITS, SKINS, SKIN_DRAWN, HAIRS, HAIR_DRAWN, FIXED, HEAD_JOINT, HAIR_JOINT, head, faces, hairs, beards, visor, jumpsuit, cape, hood, collar };

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
  // the cape's wool: light, own, shade, outline
  p: '#c49a68', q: '#9a7148', r: '#6e4f33', t: '#3b2a1b',
  // the cape's lining
  a: '#6f9e98', b: '#4f7a75', c: '#375752', f: '#1f3330',
  // the fur of its collar
  u: '#e2d6bf', x: '#bfae8e', z: '#8a7a60', n: '#4e4436',
};

// The head's slot shares the helmet's template: 13x12, turning at the neck, (6.5, 12), its bottom middle. Every
// opaque pixel of the head, a face or hair lies inside the helmet's, so a suited head shows only through the visor.
const HEAD_JOINT = [6.5, 12];

const head = [
  '.............',
  '.............',
  '.....OOOOO...',
  '...OOLLLSSO..',
  '..OLLSSSSSSO.',
  '..OLSSSSSSSO.',
  '..OSSSDSSSSSO',
  '..OSSDDSSSSO.',
  '..OSSSSSSSSO.',
  '...ODSSSSSO..',
  '....OODSSOO..',
  '.....ODSO....',
];

// Faces: eyes (e, w) and a mouth (m) keep their colours, brows take the hair's ramp, the rest the skin's.
const faces = [
  [ // calm
    '.............',
    '.............',
    '.............',
    '.............',
    '........33...',
    '........we...',
    '.............',
    '.............',
    '..........D..',
    '.............',
    '.............',
    '.............',
  ],
  [ // stern: a heavy brow over a narrowed eye, a flat mouth
    '.............',
    '.............',
    '.............',
    '.............',
    '.......3333..',
    '........De...',
    '.............',
    '.............',
    '.........DD..',
    '.............',
    '.............',
    '.............',
  ],
  [ // cheerful: a raised brow, freckles, a grin
    '.............',
    '.............',
    '.............',
    '.............',
    '........3.3..',
    '........we...',
    '.............',
    '.......D.D...',
    '.........Dm..',
    '.............',
    '.............',
    '.............',
  ],
];

const hairs = [
  [ // crop
    '.............',
    '.....4444....',
    '...44111244..',
    '..4112222234.',
    '.4122222334..',
    '.422233......',
    '.4223........',
    '.423.........',
    '..43.........',
    '.............',
    '.............',
    '.............',
  ],
  [ // quiff, swept up and forward
    '....44444....',
    '..441111244..',
    '.41122222224.',
    '.422222222334',
    '.4222223.....',
    '.422233......',
    '.4223........',
    '.423.........',
    '..43.........',
    '.............',
    '.............',
    '.............',
  ],
  [ // ponytail, tied at the nape
    '.............',
    '.....4444....',
    '...44111244..',
    '..4112222234.',
    '.4122222334..',
    '4222233......',
    '4hH3.........',
    '423..........',
    '423..........',
    '.423.........',
    '..43.........',
    '.............',
  ],
  [ // long, down the back
    '.............',
    '....44444....',
    '..441112244..',
    '.41122222334.',
    '412222223....',
    '4222223......',
    '422223.......',
    '422223.......',
    '42223........',
    '.4223........',
    '..423........',
    '...4.........',
  ],
  [ // curls, full
    '....44.44....',
    '..441231234..',
    '.42332332334.',
    '.432312312334',
    '42332332334..',
    '431231231....',
    '42332........',
    '43123........',
    '.4233........',
    '..44.........',
    '.............',
    '.............',
  ],
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

// The jumpsuit: the suit's parts' templates and joints, cloth in the suit's ramp, hands in the skin's, shoes in gear.
const jumpsuit = {
  torso: [ // joint (6, 0)
    '...olllsdo..',
    '.oodddddddo.',
    '.ollddddsdo.',
    '.ollsssssdo.',
    '.olsssssdso.',
    '.olsssssddo.',
    '.olsssssddo.',
    '.olsssssddo.',
    '.oGgGGHGgGo.',
    '.olsssssddo.',
    '.olsssssddo.',
    '.odsssssddo.',
    '.oddsssdddo.',
    '..oooooooo..',
    '............',
  ],
  upperArm: [ // joint (2.5, 2.5)
    '.ooo.',
    'ollso',
    'olsdo',
    'olsdo',
    'olsdo',
    'olsdo',
    'olsdo',
    'olsdo',
    '.ooo.',
  ],
  forearm: [ // joint (2.5, 2.9); the fist where the glove's is
    '.ooo.',
    'olsdo',
    'olsdo',
    'olsdo',
    'olsdo',
    'oddoo',
    '.OSO.',
    'OLSDO',
    'OSSDO',
    '.OOO.',
  ],
  thigh: [ // joint (3, 2.5)
    '.oooo.',
    'ollsdo',
    'olssdo',
    'olssdo',
    'olssdo',
    'olssdo',
    'olssdo',
    'olssdo',
    'olssdo',
    'olssdo',
    'odssdo',
    '.oooo.',
  ],
  shin: [ // joint (3, 2.18); a shoe where the boot is
    '.oooo.',
    'olssdo',
    'olssdo',
    'olssdo',
    'olssdo',
    'oddddo',
    '.gggg..',
    'ghGGGgg..',
    'gGGGGGGg.',
    'KggggggK.',
    '.........',
  ],
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

module.exports = { SUIT_DRAWN, SUITS, SKINS, SKIN_DRAWN, HAIRS, HAIR_DRAWN, FIXED, HEAD_JOINT, head, faces, hairs, visor, jumpsuit, cape, hood, collar };

// The farm's four item icons, 16x16, in three looks. Each is hand-placed ASCII, one char a tone of pal.ICON;
// the outline (#14171e, four-connected) is added after, as on every item icon of the game.
const { Pic } = require('./lib.js');
const { ICON, ITEM_OUTLINE } = require('./pal.js');

const A = {
  // a bean split open by its sprout, a second seed at its foot
  seed: [
    '................',
    '................',
    '.......445......',
    '...54.34432.....',
    '..5443322.......',
    '......3.........',
    '....cdbc........',
    '...cdeedc.......',
    '..cdeeddcb......',
    '..cdeddccb......',
    '..cdddccbb..dc..',
    '..cddccbbb.cddb.',
    '..ccccbbba.cdcb.',
    '...cbbbbaa.bcba.',
    '....bbbaa...ba..',
    '................',
  ],
  // a heap of dark earth, lumpy on top, a pebble in it
  soil: [
    '................',
    '................',
    '................',
    '......po........',
    '....oppon.on....',
    '...oppponnonm...',
    '..opponnnnnnmm..',
    '..ponnrrnmnnmm..',
    '.oonnqrqnnmmmmk.',
    '.onnnnqnnmmmkmk.',
    '.onnmnnmmmmkkmk.',
    '.nnmmnmmmkmkkkk.',
    '..nmmmmmkkkkkk..',
    '................',
    '................',
    '................',
  ],
  // three leaves fanned out of a bundle of stalks tied with twine
  biomass: [
    '................',
    '.......5........',
    '......453....4..',
    '.45...443...443.',
    '.4443.432..4432.',
    '..2443432.4432..',
    '...22432343322..',
    '....23433432....',
    '.....233432.....',
    '.....dcccdc.....',
    '.....cbbbbb.....',
    '.....3232322....',
    '.....3232322....',
    '....33232322....',
    '....54545444....',
    '................',
  ],
  // a round flask, corked, the fuel amber-lime in it
  biofuel: [
    '................',
    '......dcb.......',
    '......cbb.......',
    '.....xzyxx......',
    '......zyx.......',
    '......zyx.......',
    '....xzyyyxx.....',
    '...xzyyyyyxx....',
    '..xzIIJIIIHHx...',
    '..xIHHHHHHGGG...',
    '..xIHJHHHGGGF...',
    '..xHHHHHGGGFF...',
    '...xHGGGGFFF....',
    '....xGGGFFF.....',
    '.....FFFFF......',
    '................',
  ],
};

const B = {
  // a burlap pouch tied with red twine, seeds in its mouth, two spilled
  seed: [
    '................',
    '................',
    '....CdedcC......',
    '....DcdecB......',
    '.....BCCB.......',
    '.....VUUT.......',
    '....CDDCBA......',
    '...CDEDCCBA.....',
    '..CDEDCCBBBA....',
    '..CDDCBCBBBA....',
    '..CDCCBCBBAA....',
    '..CCCBBCBBAA....',
    '..BCBBBBBAAA.de.',
    '...BBBBAAAA.dcb.',
    '..........dcbb..',
    '................',
  ],
  // a burlap sack rolled open, heaped with earth
  soil: [
    '................',
    '................',
    '.....opon.......',
    '...oppnnonm.....',
    '..onponnnmmk....',
    '.DEDDCDDCCCB....',
    '.CDCCBCCBBBA....',
    '..CDDCCBCBA.....',
    '..CDCCCBBBA.....',
    '..DCCBCBBBAA....',
    '..CDCCBCBBAA....',
    '..CCCBBCBBAA....',
    '..BCBBBBBAAA....',
    '...BBBBAAAA.....',
    '................',
    '................',
  ],
  biomass: null, // drawn below: a sheaf of stalks laid on the diagonal
  // a small jerrycan, olive, an amber cap
  biofuel: [
    '................',
    '...hgggh..LM....',
    '...h.h.h..KL....',
    '..HHHHHHHHHF....',
    '..HGGGGGGGGFf...',
    '..HGHFGGHFGFf...',
    '..HGGHFHFGGFf...',
    '..HGGGHMLGGFf...',
    '..HGGGMKLGGFf...',
    '..HGGHFGHFGFf...',
    '..HGHFGGGHFFf...',
    '..HGGGGGGGGFf...',
    '..GFFFFFFFFff...',
    '...fffffffff....',
    '................',
    '................',
  ].map(r => r.slice(0, 16)),
};

const C = {
  // a paper seed packet: a sprout printed over a seed, a teal band
  seed: [
    '................',
    '...SRSRSRSRS....',
    '...SSSSSSSSSQ...',
    '...SRRR4RRRRQ...',
    '...SRR454RRRQ...',
    '...SR45343RRQ...',
    '...SRR232RRRQ...',
    '...SRRR3RRRRQ...',
    '...SRRcdcRRRQ...',
    '...SRcdcbbRRQ...',
    '...SRRbbbRRRQ...',
    '...ZYYYYYYYYX...',
    '...YXXXXXXXXW...',
    '...RQQQQQQQQP...',
    '....PPPPPPPPP...',
    '................',
  ],
  // a teal tub of earth, heaped
  soil: [
    '................',
    '................',
    '.....popo.......',
    '...oppnnonm.....',
    '..opnonnnmmk....',
    '.ZZYYYYYYYYXW...',
    '.YXXXXXXXXXXW...',
    '..YXXXXXXXXW....',
    '..YXSSSSSSXW....',
    '..YXRRRRRQXW....',
    '..YXXXXXXXXW....',
    '...YXXXXXXW.....',
    '...XWWWWWWW.....',
    '................',
    '................',
    '................',
  ],
  // a pressed bale of greens, two steel straps
  biomass: [
    '................',
    '....4...5..4....',
    '...453.454.43...',
    '..44544v4544u...',
    '..54345w3454v...',
    '..43432v2343u...',
    '..34343v3432t...',
    '..43234v4323t...',
    '..32343u3432t...',
    '..43432u2343t...',
    '..34323u3232s...',
    '..23232t2322s...',
    '..12121s1211s...',
    '...........s....',
    '................',
    '................',
  ],
  // a fuel cell: steel caps, a glass window of glowing amber-lime fuel
  biofuel: [
    '................',
    '.....uvvut......',
    '....uwvvuts.....',
    '....hgggggg.....',
    '....yIJIHHx.....',
    '....zHIHHGx.....',
    '....yIH3HGx.....',
    '....yH343Gx.....',
    '....yHH2GGx.....',
    '....yJHGHGx.....',
    '....yHHGGFx.....',
    '....hgggggg.....',
    '....uwvvuts.....',
    '.....tuuts......',
    '................',
    '................',
  ],
};

// B's biomass: five stalks laid bottom-left to top-right, their leaves at the top, a red tie across them
function biomassB() {
  const p = new Pic(16, 16), c = ICON;
  // t: which stalk across the sheaf (x + y - 15), s: how far along it (x - y)
  const tone = { '-3': '4', '-2': '3', '-1': 'H', 0: '3', 1: '4', 2: '2', 3: '2' };
  for (let y = 1; y < 15; y++) for (let x = 1; x < 15; x++) {
    const t = x + y - 15, s = x - y;
    if (s < -10 || s > 9) continue;
    const w = s > 3 ? 4 : 3;
    if (t < -w || t > w) continue;
    let k = tone[Math.max(-3, Math.min(3, t))];
    if (s <= -9) k = t < 0 ? 'I' : 'H';
    else if (s === -4 || s === -3) k = s === -4 ? 'U' : 'T';
    else if (s > 3 && Math.abs(t) === 4) k = t < 0 ? '4' : '2';
    p.px(x, y, c[k]);
  }
  // leaves flaring out of the head
  p.stamp(11, 1, ['.45', '443', '3..'], c); p.stamp(13, 5, ['4', '3'], c); p.stamp(8, 1, ['45', '3.'], c);
  return p;
}

function fromRows(rows, name) {
  if (rows.length !== 16 || rows.some(r => r.length !== 16)) throw new Error(`${name}: rows must be 16x16, got ${rows.map(r => r.length).join(',')}`);
  const p = new Pic(16, 16); p.stamp(0, 0, rows, ICON); return p;
}
function finish(p, name) {
  p.each((x, y) => { if (x === 0 || y === 0 || x === 15 || y === 15) throw new Error(`${name}: fill on the border at ${x},${y}`); });
  return p.outline(ITEM_OUTLINE);
}

const LOOKS = { A, B, C };
const ITEMS = ['seed', 'soil', 'biomass', 'biofuel'];
function icon(look, item) {
  const name = `${look}/${item}`;
  if (look === 'B' && item === 'biomass') return finish(biomassB(), name);
  return finish(fromRows(LOOKS[look][item], name), name);
}
module.exports = { icon, ITEMS };

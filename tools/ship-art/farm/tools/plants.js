// The plant in a grow capsule, four stages: sprout, small, leafy, ripe. Each stage is laid by hand from a stem
// path and leaf and fruit stamps at chosen nodes; (0, 0) is where the stem meets the soil, up is -y.
const { Pic } = require('./lib.js');

const GREEN = { 1: '#1c4626', 2: '#2c6a3a', 3: '#3f8f4f', 4: '#6fbf73', 5: '#a3dc8c' };
const FRUIT = {
  // pea pods: lime, as the biofuel they become
  G: '#4f7a1c', H: '#86ad2c', I: '#bcd84c', J: '#e6f39a',
  // tomatoes
  a: '#6e2412', b: '#b8481c', c: '#e8742c', d: '#ffb36a',
  // berries
  r: '#5e1630', s: '#a8284a', t: '#e04e6a', u: '#ff9aae',
  // a white flower, a yellow heart
  w: '#e8eef4', y: '#f2cf5a',
};
const MAP = { ...GREEN, ...FRUIT };

// leaves point up and right from (0, last row); flipped they point left
const LEAF = {
  s: ['.4', '3.'],
  cot: ['.45', '443', '2..'],
  m: ['..45', '.433', '32..'],
  l: ['...445', '..4433', '.34322', '322...'],
  // a long blade, the tomato's
  t: ['....45', '..443.', '.432..', '32....'],
  // a broad round leaf, the berry bush's
  b: ['..455.', '.44443', '344332', '32221.'],
  bs: ['.45', '443', '32.'],
  // a curled-up tip
  bud: ['.5.', '454', '.3.'],
};
const FRUITS = {
  pod: ['G.', 'JH', 'IH', 'HG', '.G'],
  podS: ['G', 'I', 'H'],
  tom: ['.2.', 'dcb', 'cbb', '.ba'],
  tomS: ['2', 'c', 'b'],
  ber: ['ut.', 'tsut', '.str', '..r.'],
  berS: ['t', 's'],
  flower: ['.w.', 'wyw', '.w.'],
};

function leaf(p, ox, oy, kind, x, y, flip) {
  const rows = LEAF[kind], h = rows.length, w = rows[0].length;
  p.stamp(flip ? ox + x - w + 1 : ox + x, oy + y - h + 1, rows, MAP, 'plant', { flip });
}
function fruit(p, ox, oy, kind, x, y) { p.stamp(ox + x, oy + y, FRUITS[kind], MAP, 'plant'); }
function stem(p, ox, oy, pts, tone = '3', thick = false) {
  for (let i = 1; i < pts.length; i++) {
    p.line(ox + pts[i - 1][0], oy + pts[i - 1][1], ox + pts[i][0], oy + pts[i][1], GREEN[tone], 'plant');
    if (thick) p.line(ox + pts[i - 1][0] + 1, oy + pts[i - 1][1], ox + pts[i][0] + 1, oy + pts[i][1], GREEN[2], 'plant');
  }
}

// each species: four stages, each a list of steps
const SPECIES = {
  pea: [
    [['stem', [[0, 0], [0, -3]]], ['leaf', 'cot', 1, -3], ['leaf', 'cot', -1, -3, true]],
    [['stem', [[0, 0], [0, -6]]], ['leaf', 'm', 1, -2], ['leaf', 'm', -1, -3, true], ['leaf', 's', 1, -5], ['leaf', 's', -1, -6, true], ['leaf', 'bud', -1, -7]],
    [['stem', [[0, 0], [0, -12]], '3', true], ['stem', [[1, -6], [4, -9]]], ['stem', [[0, -8], [-3, -10]]],
      ['leaf', 'l', 2, -2], ['leaf', 'l', -1, -3, true], ['leaf', 'm', 2, -5], ['leaf', 'l', -1, -6, true], ['leaf', 'l', 5, -9], ['leaf', 'm', -4, -10, true],
      ['leaf', 'm', 2, -11], ['leaf', 's', -1, -12, true], ['leaf', 'bud', -1, -14]],
    [['stem', [[0, 0], [0, -14]], '3', true], ['stem', [[1, -6], [5, -10]]], ['stem', [[0, -8], [-4, -11]]], ['stem', [[1, -11], [3, -13]]],
      ['leaf', 'l', 2, -1], ['leaf', 'l', -1, -2, true], ['leaf', 'l', 2, -5], ['leaf', 'l', -1, -6, true], ['leaf', 'l', 6, -10], ['leaf', 'l', -5, -11, true],
      ['leaf', 'm', 3, -13], ['leaf', 'm', -1, -13, true], ['leaf', 'bud', -1, -16], ['leaf', 'm', 4, -8], ['leaf', 'm', -3, -8, true],
      ['fruit', 'pod', 5, -9], ['fruit', 'pod', -6, -9], ['fruit', 'pod', 3, -4], ['fruit', 'podS', -3, -4], ['fruit', 'pod', -2, -12], ['fruit', 'podS', 7, -5],
      ['fruit', 'flower', 2, -16]],
  ],
  tomato: [
    [['stem', [[0, 0], [0, -4]]], ['leaf', 'cot', 1, -4], ['leaf', 'cot', -1, -3, true]],
    [['stem', [[0, 0], [0, -7]]], ['leaf', 'l', 1, -2], ['leaf', 'l', -1, -4, true], ['leaf', 'm', 1, -6], ['leaf', 'bud', -1, -9]],
    [['stem', [[0, 0], [0, -12]], '3', true], ['stem', [[1, -4], [5, -7]]], ['stem', [[0, -8], [-4, -10]]],
      ['leaf', 'l', 2, -1], ['leaf', 'l', -1, -3, true], ['leaf', 'l', 6, -7], ['leaf', 'l', -5, -10, true], ['leaf', 'l', 2, -9], ['leaf', 'm', -1, -11, true],
      ['leaf', 't', 4, -4], ['leaf', 'bud', -1, -14]],
    [['stem', [[0, 0], [0, -14]], '3', true], ['stem', [[1, -4], [6, -8]]], ['stem', [[0, -8], [-5, -11]]], ['stem', [[1, -11], [4, -13]]],
      ['leaf', 'l', 2, -1], ['leaf', 'l', -1, -3, true], ['leaf', 'l', 7, -8], ['leaf', 'l', -6, -11, true], ['leaf', 'l', 5, -13], ['leaf', 'l', -1, -7, true],
      ['leaf', 'm', 2, -10], ['leaf', 'm', -2, -13, true], ['leaf', 'bud', -1, -16],
      ['fruit', 'tom', 4, -7], ['fruit', 'tom', -7, -10], ['fruit', 'tom', 2, -3], ['fruit', 'tom', -5, -5], ['fruit', 'tomS', 7, -4], ['fruit', 'tomS', -2, -10], ['fruit', 'tom', 1, -12]],
  ],
  berry: [
    [['stem', [[0, 0], [0, -2]]], ['leaf', 'bs', 1, -2], ['leaf', 'bs', -1, -2, true]],
    [['stem', [[0, 0], [0, -5]]], ['leaf', 'b', 1, -2], ['leaf', 'b', -1, -3, true], ['leaf', 'bs', 1, -5], ['leaf', 'bud', -1, -7]],
    [['stem', [[0, 0], [0, -11]], '3', true], ['stem', [[1, -4], [4, -7]]], ['stem', [[0, -7], [-3, -9]]],
      ['leaf', 'b', 2, -1], ['leaf', 'b', -1, -2, true], ['leaf', 'b', 5, -6], ['leaf', 'b', -4, -8, true], ['leaf', 'b', 2, -9], ['leaf', 'bs', -1, -11, true],
      ['leaf', 'bud', -1, -13]],
    [['stem', [[0, 0], [0, -13]], '3', true], ['stem', [[1, -4], [5, -8]]], ['stem', [[0, -7], [-4, -10]]], ['stem', [[1, -10], [3, -12]]],
      ['leaf', 'b', 2, -1], ['leaf', 'b', -1, -2, true], ['leaf', 'b', 6, -7], ['leaf', 'b', -5, -9, true], ['leaf', 'b', 3, -11], ['leaf', 'b', -1, -11, true],
      ['leaf', 'bs', 4, -4], ['leaf', 'bud', -1, -15],
      ['fruit', 'ber', 4, -7], ['fruit', 'ber', -7, -8], ['fruit', 'ber', -3, -4], ['fruit', 'berS', 2, -3], ['fruit', 'ber', 0, -13], ['fruit', 'berS', 7, -4]],
  ],
};

// a stage drawn at the capsule's soil point, cut to the glass, the glass's own front pixels (glints, bands) kept over it
function plant(species, stage, base, { x: bx, y: by, mask }) {
  const p = new Pic(base.w, base.h);
  for (const step of SPECIES[species][stage]) {
    if (step[0] === 'stem') stem(p, bx, by, step[1], step[2], step[3]);
    else if (step[0] === 'leaf') leaf(p, bx, by, step[1], step[2], step[3], step[4]);
    else fruit(p, bx, by, step[1], step[2], step[3]);
  }
  p.each((x, y) => { if (!mask(x, y)) p.clear(x, y); });
  p.each((x, y) => { if (base.tagAt(x, y) === 'over') p.px(x, y, base.get(x, y), 'over'); });
  return p;
}
module.exports = { plant, SPECIES };

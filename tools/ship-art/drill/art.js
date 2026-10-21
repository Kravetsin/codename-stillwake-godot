// Drill concepts. Held sprites: the bit along +x, its point at the far end, the pistol grip hanging below
// (+y). Each is drawn by a function of the spin frame k (0 at rest): the bit's flutes run along it, the
// chuck's key rolls over, exposed gears turn half a tooth. Icons: the drill level, bit right, 16x16 slot.
// Coordinates here are before the one-pixel outline; build.js adds it and shifts the points.
const O = '#14171e';

const STEEL = { S: '#f4f7fa', s: '#c9ced6', t: '#8a929e', T: '#5a6270' };
const IRON = { I: '#8a94a2', i: '#5f6874', j: '#454c57', J: '#30353d' };
const TAPE = { K: '#3e4350', k: '#23262e' };
// the coil's copper, as the wire and coil icons have it
const COPPER = { C: '#f7bd84', c: '#d9864a', n: '#a65a2a', N: '#6f3818' };
const RED = { R: '#e25a46', r: '#b8382b', d: '#7e231b' };
const YEL = { Y: '#f2cf5a', y: '#d9a92e', o: '#a87b1c' };
// the harpoon's stock, darkened to walnut
const WALNUT = { W: '#866c55', w: '#624d3c', v: '#423329' };
const RUST = { P: '#c08552', p: '#94603c' };
// the tube icon's blue-grey
const TUBE = { U: '#c9d2de', u: '#97a3b4', x: '#6b7788', X: '#4b5462' };
// the cable to the belt: red, as the placeholder's stub was, one colour on every direction
const CABLE = { L: '#e25a46', l: '#b8382b', m: '#7e231b' };
const LED = { G: '#9dff8a', g: '#3fae4a' };

function grid() {
  const px = new Map();
  const set = (x, y, c) => { if (c === '.') px.delete(`${x},${y}`); else px.set(`${x},${y}`, [x, y, c]); };
  const row = (y, x0, s) => [...s].forEach((c, i) => { if (c !== ' ') set(x0 + i, y, c); });
  const rows = (y0, x0, lines) => lines.forEach((l, i) => row(y0 + i, x0, l));
  return { px, set, row, rows };
}

// Gears seen face on: '#' is metal, shaded from the top left by build-time tone, 'o' the hub's hole, ':'
// a dark window between spokes. A and B are the same gear half a tooth (or an eighth turn) apart.
const COG7 = [[
  '.#.#.#.',
  '#######',
  '.##:##.',
  '###o###',
  '.##:##.',
  '#######',
  '.#.#.#.',
], [
  '..#.#..',
  '.#####.',
  '###:###',
  '.#:o:#.',
  '###:###',
  '.#####.',
  '..#.#..',
]];
const COG5 = [[
  '.#.#.',
  '#####',
  '.#o#.',
  '#####',
  '.#.#.',
], [
  '..#..',
  '.###.',
  '##o##',
  '.###.',
  '..#..',
]];
const WHEEL9 = [[
  '..#.#.#..',
  '.#######.',
  '##::#::##',
  '.#::#::#.',
  '####o####',
  '.#::#::#.',
  '##::#::##',
  '.#######.',
  '..#.#.#..',
], [
  '..#.#.#..',
  '.#######.',
  '###:::###',
  '.#:#:#:#.',
  '##::o::##',
  '.#:#:#:#.',
  '###:::###',
  '.#######.',
  '..#.#.#..',
]];
function stamp(g, x0, y0, art, tone, keep) {
  const n = art.length - 1;
  art.forEach((line, y) => [...line].forEach((ch, x) => {
    if (ch === '.') return;
    if (keep && !keep(x0 + x, y0 + y)) return;
    const v = (x + y) / (2 * n);
    const c = ch === 'o' ? tone[3] : ch === ':' ? tone[4] || tone[3] : v < 0.38 ? tone[0] : v < 0.62 ? tone[1] : tone[2];
    g.set(x0 + x, y0 + y, c);
  }));
}

// A drill bit along a row, x0 to x1, its point at x1: flutes as light, mid and dark steel running along it.
function bit(g, x0, x1, y, k) {
  for (let x = x0; x <= x1; x++) g.set(x, y, x === x1 ? 's' : 'SstT'[((x - x0 - k) % 4 + 4) % 4]);
}
// the chuck's key: rows 0, 1, 2, then round the back
const key = k => [0, 1, 2, -1][k % 4];

const D = {};

// ---------------------------------------------------------------- 1 · the frame drill
D.frame = (() => {
  const pal = { ...STEEL, ...IRON, ...TAPE, ...COPPER, ...RUST, ...RED, ...YEL, ...CABLE };
  const held = k => {
    const g = grid();
    // the frame, rust at its corners, the motor in it: the rotor's ends in steel, the coil's copper between
    g.rows(1, 0, [
      'PIIIIIIIP',
      'IsCcCcCsi',
      'ItcncncTi',
      'ITNnNnNTi',
      'PjjjjjjjP',
    ]);
    // the gearbox, its side open: a dark plate, the big steel gear on the motor's shaft, a brass one on the chuck's
    g.rows(1, 9, ['JJJJJJJJJJJ', 'JJJJJJJJJJJ', 'JJJJJJJJJJJ', 'JJJJJJJJJJJ', 'JJJJJJJJJJJ']);
    stamp(g, 9, 0, COG7[k % 2], ['S', 's', 't', 'J', 'j']);
    stamp(g, 15, 1, COG5[1 - k % 2], ['Y', 'y', 'o', 'J']);
    // the chuck, a cut of tube with its key rolling over, and the bit
    g.rows(2, 20, ['ss', 'tt', 'TT']);
    if (key(k) >= 0) g.set(21, 2 + key(k), 'J');
    bit(g, 22, 27, 3, k);
    // the grip: a taped bar under the frame's rear, raked back; the trigger a red tab
    g.rows(6, 1, [
      ' KkK',
      ' kKk',
      'KkK',
      'kKk',
      'jjJ',
    ]);
    g.set(5, 6, 'R');
    // the cable to the belt, out of the butt
    g.set(0, 11, 'L');
    return g;
  };
  const icon = () => {
    const g = grid();
    g.rows(4, 1, [
      'PIIIIIP',
      'IsCcCsi',
      'ITnNnTi',
      'PjjjjjP',
    ]);
    g.rows(4, 8, ['JJJJ', 'JJJJ', 'JJJJ', 'JJJJ']);
    stamp(g, 7, 3, COG5[0], ['S', 's', 't', 'J']);
    g.set(11, 4, 'Y'); g.set(11, 5, 'y'); g.set(11, 6, 'o');
    g.rows(5, 12, ['s', 'T']);
    g.row(5, 13, 'Ss');
    g.row(6, 13, 'tT');
    g.rows(8, 2, [' KkK', 'KkK', 'kKk', 'jjJ']);
    g.set(6, 8, 'R');
    g.set(1, 12, 'L');
    return g;
  };
  return {
    name: 'Рамный', en: 'Frame drill', pal, held, icon, frames: 4,
    grip: [2.5, 8], hold: 0, chuck: [20, 3.5], tip: [28, 3.5], plug: [0.5, 12],
    about: 'Буквально рецепт: железная рама с ржавыми уголками, в ней мотор — медная обмотка ротора на виду. Вал выходит на редуктор с открытым боком: большая стальная шестерня на вале мотора, малая латунная крутит патрон из обрезка трубы. Короткое сверло. Рукоять из прутка, обмотана изолентой; из торца красный кабель к поясу.',
  };
})();

// ---------------------------------------------------------------- 2 · the tube hammer
D.tube = (() => {
  const pal = { ...STEEL, ...IRON, ...TAPE, ...COPPER, ...RED, ...YEL, ...TUBE, ...CABLE, ...LED };
  const held = k => {
    const g = grid();
    // the body: a length of tube, the motor in it, vent slots cut through to the winding
    g.rows(2, 0, [
      'KUUUUUUUUUUU',
      'Kuuuuuuuuuuu',
      'Kuuuuuuuuuuu',
      'Kxxxxxxxxxxx',
      'KXXXXXXXXXXX',
    ]);
    for (const x of [3, 5, 7, 9]) { g.set(x, 3, 'C'); g.set(x, 4, 'c'); g.set(x, 5, 'N'); }
    // a lamp on the rear cap, lit while it runs
    g.set(0, 3, k ? 'G' : 'g');
    // the gearbox: a red cast cradle, its cover gone, the tops of both gears turning in it
    g.rows(2, 12, ['JJJJJJJJJJ', 'JJJJJJJJJJ', 'JJJJJJJJJJ']);
    stamp(g, 11, 0, COG7[k % 2], ['S', 's', 't', 'J', 'j']);
    stamp(g, 17, 1, COG5[1 - k % 2], ['Y', 'y', 'o', 'J']);
    g.rows(5, 11, [
      'RRRRRRRRRRR',
      'rrrrrrrrrrr',
      ' ddddddddd',
    ]);
    // a rubber collar, the chuck, and a long bit
    g.rows(3, 22, ['kK', 'Kk', 'kK']);
    g.rows(3, 24, ['s', 't', 'T']);
    if (key(k) >= 0) g.set(24, 3 + key(k), 'J');
    bit(g, 25, 30, 4, k);
    // the grip: a frame bent into a loop under the rear of the tube, taped where the fist goes
    g.rows(7, 1, [
      'jIiiiiI',
      'Kk   i',
      'kK   i',
      'Kk   i',
      'jjjjjj',
    ]);
    g.set(5, 8, 'R');
    // the cable to the belt out of the loop's heel
    g.set(1, 12, 'L');
    return g;
  };
  const icon = () => {
    const g = grid();
    g.rows(4, 1, [
      'KUUUUU',
      'KuCuCu',
      'KxcxNx',
      'KXXXXX',
    ]);
    stamp(g, 6, 3, COG5[0], ['S', 's', 't', 'J']);
    g.rows(4, 11, ['Y', 'y']);
    g.rows(6, 6, ['RRRRRR', 'dddddd']);
    g.rows(4, 12, ['k', 'K', 'k']);
    g.rows(5, 13, ['Ss']);
    g.rows(8, 2, ['jIiiI', 'Kk  i', 'kK  i', 'jjjjj']);
    g.set(5, 9, 'R');
    g.set(2, 12, 'L');
    return g;
  };
  return {
    name: 'Перфоратор из трубы', en: 'Tube hammer', pal, held, icon, frames: 4,
    grip: [2, 9.5], hold: 0, chuck: [24, 4.5], tip: [31, 4.5], plug: [1.5, 13],
    about: 'Длинный и деловой: корпус — обрезок трубы, мотор внутри, сквозь прорези просвечивает медь. Спереди красная литая люлька редуктора, крышки нет: сверху торчат и крутятся обе шестерни. Резиновый воротник, патрон, длинный бур, как у перфоратора. Рама согнута в петлю-рукоять; на заднем торце лампочка, горит, пока работает; кабель из пятки рукояти.',
  };
})();

// ---------------------------------------------------------------- 3 · the hand drill with a motor
D.crank = (() => {
  const pal = { ...STEEL, ...IRON, ...TAPE, ...COPPER, ...YEL, ...WALNUT, ...TUBE, ...CABLE };
  const held = k => {
    const g = grid();
    // the frame: a rail from the grip to the chuck, a post up to the wheel's axle
    g.row(7, 1, 'IIIIIIIIIIIIIIII');
    g.row(8, 1, 'jjjjjjjjjjjjjjjj');
    // the big wheel of an old hand drill, spokes and all
    stamp(g, 6, 0, WHEEL9[k % 2], ['s', 't', 'T', 'J', 'J']);
    // the motor where the crank was: the rotor's copper drum between two end plates, on the wheel's axle
    g.rows(1, 0, [
      's    s',
      'sCcCcs',
      'tcncnt',
      'tnNnNt',
      'TNnNnT',
      'T    T',
    ]);
    // the pinion the wheel turns, brass, on the rail
    stamp(g, 14, 5, COG5[1 - k % 2], ['Y', 'y', 'o', 'J']);
    // the chuck: a cut of tube with a brass band, and the bit
    g.rows(6, 18, ['UU', 'xx', 'XX']);
    if (key(k) >= 0) g.set(19, 6 + key(k), 'X');
    g.rows(6, 20, ['y', 'o', 'o']);
    bit(g, 21, 26, 7, k);
    // a walnut grip under the rail's rear, a yellow switch before it
    g.rows(9, 1, [
      'WWWw',
      'WWwv',
      'Wwwv',
      'wwv',
      'kkk',
    ]);
    g.set(5, 9, 'Y');
    // the cable from the motor's back plate
    g.rows(3, -1, ['L', 'l', 'l', 'l', 'm']);
    return g;
  };
  const icon = () => {
    const g = grid();
    stamp(g, 4, 1, COG7[0], ['s', 't', 'T', 'J', 'J']);
    g.rows(2, 1, ['sCcs', 'tnNt', 'TNnT']);
    g.row(8, 2, 'IIIIIIIIIII');
    g.row(9, 2, 'jjjjjjjjjjj');
    g.rows(7, 12, ['U', 'x', 'X']);
    g.rows(8, 13, ['Ss']);
    g.rows(10, 2, ['WWw', 'Wwv', 'wwv', 'kk']);
    g.set(1, 4, 'L'); g.set(1, 5, 'm');
    return g;
  };
  return {
    name: 'Коловорот с мотором', en: 'Motor hand drill', pal, held, icon, frames: 4,
    grip: [2.5, 11], hold: 0, chuck: [18, 7.5], tip: [27, 7.5], plug: [-0.5, 8],
    about: 'Старая ручная дрель-коловорот, где вместо ручки мотор: медный барабан ротора между двух щёчек сидит на оси большого колеса со спицами. Колесо крутит латунную шестерёнку на раме, рама ведёт к патрону из трубы. Рукоять из ореха, как приклад гарпуна; жёлтый тумблер; кабель из задней щёчки мотора. Самый «рукодельный».',
  };
})();

module.exports = { O, D };

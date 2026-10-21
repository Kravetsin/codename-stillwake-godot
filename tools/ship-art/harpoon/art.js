// Harpoon gun concepts. Held sprites: the barrel along +x, the muzzle at the far end, the pistol grip
// hanging below (+y). Rods: tip along +x, the rope eye at the tail. Icons: the gun level, muzzle right.
// Coordinates here are before the one-pixel outline; build.js adds it and shifts the points.
const O = '#14171e';

const STEEL = { S: '#f4f7fa', s: '#c9ced6', t: '#8a929e', T: '#5a6270' };
const IRON = { I: '#8a94a2', i: '#5f6874', j: '#454c57', J: '#30353d' };
const TAPE = { K: '#3e4350', k: '#23262e' };
const RED = { R: '#e25a46', r: '#b8382b', d: '#7e231b' };
const YEL = { Y: '#f2cf5a', y: '#d9a92e', o: '#a87b1c' };
const WOOD = { W: '#c69257', w: '#9a6a3a', v: '#6e4524' };
const RUST = { P: '#c08552', p: '#94603c', q: '#6a4128' };
// the rope: one colour for every direction, as it is one thing in the game
const ROPE = { L: '#ecc67e', l: '#b98e4c', m: '#7c5a2e' };
const DIAL = { E: '#eef2f6' };

function grid() {
  const px = new Map();
  const set = (x, y, c) => px.set(`${x},${y}`, [x, y, c]);
  const row = (y, x0, s) => [...s].forEach((c, i) => { if (c !== '.') set(x0 + i, y, c); });
  const rows = (y0, x0, lines) => lines.forEach((l, i) => row(y0 + i, x0, l));
  return { px, set, row, rows };
}

const D = {};

// ---------------------------------------------------------------- 1 · pneumatic
D.pneumatic = (() => {
  const pal = { ...STEEL, ...IRON, ...TAPE, ...RED, ...YEL, ...ROPE, ...DIAL };
  const held = grid();
  held.rows(0, 0, [
    '.......sEs..............',
    '.iIIi..EdE..............',
    'iLLllj.IIIIi..........Ii',
    'ILJJlmjIiiiijSsSsSsSsSIi',
    'ILJJmmjiiiiijtttttttttiJ',
    'illmmmjjiijjjTTTTTTTTTij',
    '.jjjj.jjjjjJKRRRRRRR.ij.',
    '..T...KkKk.trRrrrrrrd...',
    '.Kk...kKkK.TrYyrrrrrd...',
    '.....KkKk...dYyddddd....',
    '.....kKkK...............',
    '.....jjjJ...............',
  ]);
  const rod = grid();
  rod.rows(0, 0, [
    '.ss............',
    's..sssssssssSSS',
    '.tt.TTTTTTTTTt.',
  ]);
  const icon = grid();
  icon.rows(0, 0, [
    '................',
    '................',
    '................',
    '....sE..........',
    '..I.EdE.....I...',
    '.iLjIIIiSsSsIi..',
    '.LJliiiittttiSS.',
    '.imjjjjjTTTTjT..',
    '..j.KkKjRRRRj...',
    '...KkKtrryrd....',
    '...kK...ddd.....',
    '..Kk............',
    '..jJ............',
    '................',
    '................',
    '................',
  ]);
  return {
    name: 'Пневмогарпун', en: 'Gas harpoon', pal, held, rod, icon,
    grip: [7, 9.5], hold: 0, muzzle: [24, 4.5], reel: [3, 1], seat: [12, 3], eye: [2, 1.5], rodOver: false,
    about: 'Тонкая труба на баллоне со сжатым газом: красный баллон от кислородного резака под стволом, манометр на казённике, катушка сзади, как у спиннинга. Тише и аккуратнее остальных: стержень сидит в стволе, наружу торчит только жало.',
    rodAbout: 'Гладкий стальной стержень, заточенный на конус; хвост расплющен и просверлен под ушко.',
  };
})();

// ---------------------------------------------------------------- 2 · spring
D.spring = (() => {
  const pal = { ...STEEL, ...IRON, ...TAPE, ...WOOD, ...ROPE, ...RUST };
  const held = grid();
  // a plank butt, an open iron frame round a coil spring, a short tube, a line spool under it
  held.rows(3, 0, [
    '......IIIIIIIIIII.......',
    '..WWWWiJJJJJJJJJiSsSsSsI',
    'WWWWWWiJJJJJJJJJittttttI',
    'wwwwwwiJJJJJJJJJiTTTTTTj',
    'v..vwvjjjjjjjjjjj.IiJ...',
    '..vwwv.T.........LLLLl..',
    '..wwv...........LlmlmlL.',
    '.vwwv............LLLLl..',
    '.qqq..............IiJ...',
  ]);
  // the coil, seen side on: strands slanting forward, the gaps between them dark
  for (let x = 7; x <= 15; x++) for (let y = 4; y <= 6; y++) {
    if ((x + y) % 3 === 0) held.set(x, y, 'stT'[y - 4]);
  }
  held.set(16, 5, 'S');
  const rod = grid();
  rod.rows(0, 0, [
    '.PP.................S.',
    'P..ptttttttttttttttsSS',
    '.qq.................T.',
  ]);
  const icon = grid();
  icon.rows(0, 0, [
    '................',
    '................',
    '................',
    '................',
    '................',
    '....IIIIII......',
    '..WWiJsJJiSsS...',
    '.WWWiJtJsitttSS.',
    '.wwwiTJsTiTTTt..',
    '..vwjjjjjj.iJ...',
    '..wwv.T...LLl...',
    '.vwv.....Llml...',
    '.qqq......LLl...',
    '...........iJ...',
    '................',
    '................',
    '................',
  ]);
  return {
    name: 'Пружинник', en: 'Spring harpoon', pal, held, rod, icon,
    grip: [3.5, 10], hold: 0, muzzle: [24, 5.5], reel: [19.5, 10], seat: [6, 4], eye: [2, 1.5], rodOver: false,
    about: 'Пружина от амортизатора в открытой железной раме, приклад из доски, короткая трубка спереди. Взводится, как арбалет: пружину сжимают, стержень вставляют в трубку; витки на виду, видно, сжата ли она. Под трубкой катушка-шпуля с верёвкой.',
    rodAbout: 'Тонкий болт с зазубренным жалом; ушко скручено из медной проволоки прямо на хвосте.',
  };
})();

// ---------------------------------------------------------------- 3 · winch pipe
D.winch = (() => {
  const pal = { ...STEEL, ...IRON, ...TAPE, ...RUST, ...ROPE, ...YEL, ...RED };
  const held = grid();
  held.rows(0, 0, [
    '...................r....',
    '......ILlLlLlLlLlj.T....',
    '......ImlmlmlmlmljT.....',
    '...S...J........J...IS..',
    '.YyIIIIPIIIIsIIIIPIIIj..',
    'rYyIiiippiiitiiiiiipiJ..',
    'rYyIiiiiiqiitiiipiiiiJ..',
    '.yoIjjjjjjjjTjjjjjjjjj..',
    '...SKkKk.T..........JS..',
    '....KkKk.T..............',
    '...KkKk.................',
    '...kKkK.................',
    '...jjjJ.................',
  ]);
  const rod = grid();
  rod.rows(0, 0, [
    'YYy............',
    'Y.yTiTiTiTiTtsS',
    'oyoJjJjJjJjJts.',
  ]);
  const icon = grid();
  icon.rows(0, 0, [
    '................',
    '................',
    '................',
    '...........r....',
    '....ILlLlLjT....',
    '....ImlmlmjT....',
    '..S..J...J..S...',
    '.YIIIPIIsIIPJ...',
    '.YIiiipptiiiJsS.',
    '.oIjjjjjTjjjJt..',
    '..SKkKT.....S...',
    '...KkK.T........',
    '..KkK...........',
    '..kKk...........',
    '..jjJ...........',
    '................',
  ]);
  return {
    name: 'Лебёдка', en: 'Winch pipe', pal, held, rod, icon,
    grip: [5, 10.5], hold: 0, muzzle: [22, 6], reel: [12, 1], seat: [9, 4], eye: [1.5, 1.5], rodOver: false,
    about: 'Обрезок толстой ржавой трубы на хомутах, сверху на ней барабан от лебёдки с ручкой, сзади жёлтая крышка под строительный патрон. Самый грубый и тяжёлый: бьёт патроном, наматывает руками.',
    rodAbout: 'Кусок арматуры с рёбрами, конец сточен на точиле; ушко — приваренная латунная гайка.',
  };
})();

module.exports = { O, D };

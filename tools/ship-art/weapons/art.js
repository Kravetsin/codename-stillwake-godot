// The weapons' pixels. Held sprites: the handle along +x from the grip, the head at the far end, the
// working side below (+y) so it leads an overhead swing turning clockwise. Icons: the same tool on the
// slot's diagonal, handle low-left, head high-right, drawn on an (along, across) grid.
const O = '#14171e';

const STEEL = { S: '#f4f7fa', s: '#c9ced6', t: '#8a929e', T: '#5a6270' };
const PLATE = { S: '#c3ccd6', s: '#939fad', t: '#677280', T: '#4a525e', E: '#eef2f6' };
const TAPE = { K: '#3e4350', k: '#23262e' };
const WOOD = { W: '#c69257', w: '#9a6a3a', v: '#6e4524' };
const RED = { R: '#e25a46', r: '#b8382b', d: '#7e231b' };
const BLUE = { B: '#7f9bb5', b: '#5c7a96', n: '#435b72', N: '#2f4053' };
const YEL = { Y: '#f2cf5a', y: '#d9a92e', o: '#a87b1c', O: '#735210' };
const IRON = { I: '#8a94a2', i: '#5f6874', j: '#454c57', J: '#30353d' };
const RUST = { P: '#c08552', p: '#94603c', q: '#6a4128' };
const COPPER = { C: '#e8a46a', c: '#c47a42', x: '#94552a' };
const HOT = { H: '#fff3c4', G: '#ffe14d', F: '#ff7a2e', g: '#9aa3b0' };

function grid() {
  const px = new Map();
  const set = (x, y, c) => px.set(`${x},${y}`, [x, y, c]);
  const row = (y, x0, s) => [...s].forEach((c, i) => { if (c !== '.') set(x0 + i, y, c); });
  return { px, set, row };
}

// ---------------------------------------------------------------- held
const HELD = {};

HELD.fireAxe = (() => {
  const g = grid(), { set, row } = g;
  // the haft: a swelled end, ash wood, the dark wrap a third up where the fist closes
  set(0, 7, 'v'); set(1, 7, 'v');
  row(5, 0, 'WWWW'); row(6, 0, 'vwww');
  for (let x = 4; x <= 8; x++) { set(x, 5, x % 2 ? 'K' : 'k'); set(x, 6, x % 2 ? 'k' : 'K'); }
  for (let x = 9; x <= 17; x++) { set(x, 5, x % 4 === 0 ? 'w' : 'W'); set(x, 6, x % 3 === 0 ? 'v' : 'w'); }
  // the steel collar, the red head, the pick up and on, the bare blade below to a bright edge
  set(18, 4, 'c'); set(18, 5, 'C'); set(18, 6, 'C'); set(18, 7, 'c');
  for (let y = 3; y <= 8; y++) for (let x = 19; x <= 23; x++) {
    let c = x === 19 ? 'R' : x === 23 ? 'd' : 'r';
    if (y === 3 && x < 22) c = 'R';
    if (y === 8) c = 'd';
    set(x, y, c);
  }
  set(20, 2, 'R'); set(21, 2, 'r'); set(22, 2, 'r'); set(21, 1, 'r'); set(22, 1, 'd'); set(22, 0, 'd');
  row(9, 18, 'ssStttT'); row(10, 17, 'sssstttTT'); row(11, 17, 'SSSSSSSSS');
  return { ...g, pal: { ...WOOD, ...TAPE, C: '#9aa3b0', c: '#5a6270', ...RED, ...STEEL },
    grip: [6.5, 6], edge: [[17.5, 11.5], [21.5, 11.5], [25.5, 11.5]] };
})();

HELD.crowbar = (() => {
  const g = grid(), { set, row } = g, H = 3;
  // a chisel end worn bright, a painted bar taped low for leverage, a gooseneck hook whose claw points back
  set(0, 2, 'S'); set(1, 1, 'S'); set(1, 2, 's'); set(2, 1, 's'); set(2, 2, 't');
  for (let x = 3; x <= 26 + H; x++) { set(x, 1, 'B'); set(x, 2, x % 6 === 4 ? 'n' : 'b'); }
  for (let x = 5; x <= 9; x++) { set(x, 1, x % 2 ? 'K' : 'k'); set(x, 2, x % 2 ? 'k' : 'K'); }
  row(1, 27 + H, 'Bb'); row(2, 27 + H, 'bbn'); row(3, 28 + H, 'bn'); row(4, 29 + H, 'bn'); row(5, 29 + H, 'bt');
  row(6, 29 + H, 'bn'); row(7, 28 + H, 'bnN'); row(8, 26 + H, 'bnN'); row(9, 23 + H, 'Ssst');
  return { ...g, pal: { ...BLUE, ...TAPE, S: '#c3ccd6', s: '#939fad', t: '#677280' },
    grip: [7.5, 2], edge: [[23.5 + H, 9.5], [30.5 + H, 5.5]] };
})();

HELD.sledgehammer = (() => {
  const g = grid(), { set, row } = g;
  // a rubber grip from the end past the fist, a hazard-yellow fibreglass handle, a forged block head
  set(0, 4, 'K'); set(0, 7, 'k');
  for (let x = 0; x <= 10; x++) { set(x, 5, x % 3 === 1 ? 'K' : 'k'); set(x, 6, 'k'); }
  for (let x = 11; x <= 20; x++) { set(x, 5, x >= 19 ? 'y' : 'Y'); set(x, 6, x >= 19 ? 'o' : 'y'); }
  row(0, 22, 'SSss');
  for (let y = 1; y <= 10; y++) row(y, 21, y === 1 ? 'IIiijJ' : y === 10 ? 'jjjjjJ' : y === 3 || y === 8 ? 'Ijjj jJ'.replace(' ', '') : 'Iiiij J'.replace(' ', ''));
  row(11, 22, 'Ssst');
  return { ...g, pal: { ...TAPE, ...YEL, ...IRON, S: '#e6ebf0', s: '#b4bdc8', t: '#8a929e' },
    grip: [7.5, 6], edge: [[22.5, 11.5], [25.5, 11.5]] };
})();

HELD.machete = (() => {
  const g = grid(), { set } = g;
  // a blade cut from hull plate, rust and two rivet holes left in it; a bolt for a pommel, a washer guard
  set(0, 4, 'P'); set(0, 5, 'q');
  for (let x = 1; x <= 6; x++) { set(x, 4, x % 2 ? 'K' : 'k'); set(x, 5, x % 2 ? 'k' : 'K'); }
  [...'SssstT'].forEach((c, i) => set(7, 2 + i, c));
  const span = x => x <= 10 ? [3, 6] : x <= 15 ? [3, 7] : [x - 12, 7];
  for (let x = 8; x <= 19; x++) {
    const [t, b] = span(x);
    for (let y = t; y <= b; y++) set(x, y, y === b ? 'E' : y === t ? 'S' : y === b - 1 ? 't' : 's');
  }
  set(10, 4, 'T'); set(15, 4, 'T');
  set(12, 4, 'P'); set(12, 5, 'p'); set(13, 4, 'p'); set(16, 5, 'p');
  set(14, 3, 'Y'); set(13, 5, 'y');
  return { ...g, pal: { ...PLATE, ...TAPE, ...RUST, Y: '#f2cf5a', y: '#d9a92e' },
    grip: [4, 5], edge: [[9.5, 6.5], [12.5, 7.5], [15.5, 7.5], [19.5, 7.5]] };
})();

HELD.arcBaton = (() => {
  const g = grid(), { set, row } = g;
  // a welder's stinger on a pipe: its cable cut short at the butt, tape a third up, an insulated
  // clamp holding an electrode that glows at the tip
  set(0, 6, 'K'); set(0, 7, 'k'); set(1, 7, 'k'); set(1, 8, 'k'); set(2, 8, 'k'); set(3, 8, 'C');
  set(0, 4, 'S'); set(0, 5, 't');
  for (let x = 1; x <= 6; x++) { set(x, 4, x % 2 ? 'K' : 'k'); set(x, 5, x % 2 ? 'k' : 'K'); }
  row(4, 7, 'SSSs'); row(5, 7, 'tttT');
  row(1, 12, 'KKKk');
  row(2, 11, 'YYYYy');
  for (let y = 3; y <= 6; y++) row(y, 11, 'YyoyO'.slice(0, 4) + 'o');
  row(7, 11, 'ooooO');
  row(3, 16, 'CC'); row(4, 16, 'cc'); row(5, 16, 'cx'); row(6, 16, 'xx');
  row(5, 18, 'gg'); row(6, 19, 'ggF'); row(7, 21, 'FGH');
  return { ...g, pal: { ...TAPE, S: '#c3ccd6', s: '#939fad', t: '#677280', T: '#4a525e', ...YEL, ...COPPER, ...HOT },
    grip: [4, 5], edge: [[23.5, 7.5]] };
})();

// ---------------------------------------------------------------- icons
// u runs along the tool toward its head (x - y), v across it toward the working side (x + y - K).
function icon(K, U, paint, fixes = {}) {
  const px = new Map();
  for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
    const c = paint(x - y - U, x + y - K);
    if (c) px.set(`${x},${y}`, [x, y, c]);
  }
  for (const [k, c] of Object.entries(fixes)) { const [x, y] = k.split(',').map(Number); if (c === '.') px.delete(k); else px.set(k, [x, y, c]); }
  return px;
}
const inR = (a, lo, hi) => a >= lo && a <= hi;

const ICON = {};
ICON.fireAxe = icon(13, 0, (u, v) => {
  if (inR(u, -12, -11) && inR(v, -1, 2)) return v <= 0 ? 'w' : 'v';
  if (inR(u, -12, 2) && inR(v, 0, 1)) return inR(u, -9, -6) ? (v === 0 ? 'K' : 'k') : v === 0 ? 'W' : (u % 3 === 0 ? 'v' : 'w');
  if (inR(u, 3, 3) && inR(v, -1, 2)) return v <= 0 ? 'C' : 'c';
  if (inR(u, 4, 8) && inR(v, -2, 2)) return v === -2 || u === 4 ? 'R' : v === 2 || u === 8 ? 'd' : 'r';
  if (inR(u, 5, 7) && v === -3) return u === 7 ? 'd' : 'r';
  if (u === 7 && v === -4) return 'd';
  if (inR(v, 3, 5) && inR(u, 1 + (5 - v), 10 + (v - 3))) return v === 5 ? 'S' : v === 3 ? 's' : 't';
  return null;
});

ICON.crowbar = icon(14, 0, (u, v) => {
  if (inR(u, -13, -12) && inR(v, 0, 1)) return 'S';
  if (inR(u, -11, 9) && inR(v, 0, 1)) return inR(u, -8, -5) ? (v === 0 ? 'K' : 'k') : v === 0 ? 'B' : 'b';
  // the hook: on past the bar's end, round, and back toward the hand
  if (inR(u, 10, 11) && inR(v, 0, 3)) return v <= 0 ? 'B' : v === 3 ? 'n' : 'b';
  if (inR(u, 8, 9) && inR(v, 3, 4)) return v === 3 ? 'b' : 'n';
  if (inR(u, 5, 7) && inR(v, 4, 4)) return 's';
  if (u === 4 && v === 4) return 'S';
  return null;
});

ICON.sledgehammer = icon(14, 0, (u, v) => {
  if (inR(u, -12, -6) && inR(v, 0, 1)) return v === 0 ? 'K' : 'k';
  if (inR(u, -5, 5) && inR(v, 0, 1)) return v === 0 ? 'Y' : 'y';
  if (inR(u, 6, 10) && inR(v, -4, 5)) {
    if ((u === 6 || u === 10) && (v === -4 || v === 5)) return null;
    if (v === 5) return 'S';
    if (v === -4) return 's';
    return u === 6 ? 'I' : u === 10 ? 'J' : u === 9 ? 'j' : 'i';
  }
  return null;
});

ICON.machete = icon(14, 0, (u, v) => {
  if (u === -12 && inR(v, 0, 1)) return v === 0 ? 'P' : 'q';
  if (inR(u, -11, -7) && inR(v, 0, 1)) return v === 0 ? 'K' : 'k';
  if (u === -6 && inR(v, -1, 2)) return v <= 0 ? 'S' : 't';
  if (inR(u, -5, 11)) {
    const top = u <= 8 ? -1 : u - 9 - 1, bot = u <= -1 ? 2 : 3;
    if (inR(v, top, bot)) return v === bot ? 'E' : v === top ? 'S' : v === bot - 1 ? 't' : 's';
  }
  return null;
}, { '9,6': 'P', '8,8': 'T' });

ICON.arcBaton = icon(14, 0, (u, v) => {
  if (u === -12 && inR(v, 0, 1)) return 'S';
  if (inR(u, -11, -6) && inR(v, 0, 1)) return v === 0 ? 'K' : 'k';
  if (inR(u, -5, -1) && inR(v, 0, 1)) return v === 0 ? 'S' : 't';
  if (inR(u, 0, 4) && inR(v, -1, 2)) return v === -1 ? 'Y' : v === 2 ? 'o' : u === 2 ? 'o' : 'y';
  if (inR(u, 5, 6) && inR(v, -1, 2)) return v <= 0 ? 'C' : 'x';
  return null;
}, {});

function rows(lines) {
  const px = new Map();
  lines.forEach((l, y) => [...l].forEach((c, x) => { if (c !== '.') px.set(`${x},${y}`, [x, y, c]); }));
  return px;
}
ICON.fireAxe = rows([
  '................',
  '................',
  '........d.......',
  '........rrd.....',
  '........Rrrd....',
  '........RrrrdTT.',
  '........CRrdttS.',
  '.......WvcdStS..',
  '......wv..ssS...',
  '.....Ww...sS....',
  '....Kk..........',
  '...Kk...........',
  '..Ww............',
  '.wv.............',
]);
ICON.crowbar = rows([
  '................',
  '............Bb..',
  '...........Bbbn.',
  '..........Bb.bn.',
  '.........Bb..bn.',
  '........Bb..tN..',
  '.......Bb..St...',
  '......Bb..S.....',
  '.....Kk.........',
  '....Kk..........',
  '...Bb...........',
  '..Bb............',
  '.Ss.............',
  '.t..............',
]);
ICON.arcBaton = rows([
  '................',
  '................',
  '................',
  '.........Kk...H.',
  '.........YYCcgG.',
  '........YyyCx...',
  '.......Yyoyx....',
  '.......yyyO.....',
  '......StO.......',
  '.....St.........',
  '....Kk..........',
  '...Kk...........',
  '..Kk............',
  '.St.............',
  '.k..............',
]);
module.exports = { O, HELD, ICON, PAL: { ...STEEL, ...TAPE } };

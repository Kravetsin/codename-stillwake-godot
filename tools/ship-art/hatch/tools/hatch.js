// The hatch concepts: a door laid in a deck. Every piece is one 16x16 cell; joins are left/right, as the
// game's masks have them (Right 2, Left 8): 0 alone, 2 left end, 10 middle, 8 right end.
// The panel is 10 px thick, as the door is 10 px wide; `top` is its first row: 0 lies flush with the
// floor the hero stands on, 3 is centred in the cell as the door is.
const { Pic } = require('./lib.js');
const PNG = require('./png.js');
const ART = require('path').join(__dirname, '..', '..', '..', '..', 'game', 'art') + '/';

const MASKS = [0, 2, 10, 8];
const joins = mask => ({ jl: !!(mask & 8), jr: !!(mask & 2) });

// An open hatch: two rails along the panel's faces, the slid panel's edge at the left end (a lip of
// `lip` columns, its middle ones coloured by `edge(r)`), and catches at the right end, as the door has.
function openPiece({ jl, jr }, top, frame, edge, lip = 3) {
  const p = new Pic(16, 16);
  p.hl(0, 15, top, frame); p.hl(0, 15, top + 9, frame);
  if (!jl) {
    p.vl(0, top, top + 9, frame); p.vl(lip - 1, top, top + 9, frame);
    for (let x = 1; x < lip - 1; x++) for (let r = 1; r <= 8; r++) p.px(x, top + r, edge(r, x));
  }
  if (!jr) { p.px(15, top + 1, frame); p.px(15, top + 8, frame); }
  return p;
}

// ---- A: the door, turned on its side ----
function doorTurned(file, top) {
  const im = PNG.read(ART + file + '.png'), p = new Pic(16, 16), shift = top - 3;
  for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
    const i = (y * 16 + x) * 4; if (im.data[i + 3] < 128) continue;
    const hx = '#' + [0, 1, 2].map(q => im.data[i + q].toString(16).padStart(2, '0')).join('');
    p.px(y, x + shift, hx); // transposed: the door's rows become the hatch's columns, light stays top-left
  }
  return p;
}
const A = {
  id: 'A', piece(open, mask, top) {
    // door masks: Up 1 is the hatch's Left, Down 4 its Right
    const { jl, jr } = joins(mask), dm = (jl ? 1 : 0) | (jr ? 4 : 0);
    return doorTurned(`door_${open ? 'open' : 'closed'}_${dm}`, top);
  },
};

// ---- B: a gunmetal slider, a stripe band along the face you walk on, a grip and a slide arrow ----
const B_ = { F: '#22262e', L: '#8d99aa', M: '#56606e', N: '#474e59', K: '#30353e', Y: '#e8c547', Z: '#2a2f3a' };
const bStripe = (x, r) => (((x + r) & 3) < 2 ? B_.Y : B_.Z);
const B = {
  id: 'B', piece(open, mask, top) {
    const j = joins(mask), t = top;
    if (open) return openPiece(j, t, B_.F, r => (r === 1 ? B_.L : r <= 3 ? bStripe(0, r) : r === 8 ? B_.K : B_.M));
    const p = new Pic(16, 16);
    p.hl(0, 15, t, B_.F); p.hl(0, 15, t + 1, B_.L);
    for (let x = 0; x < 16; x++) for (let r = 2; r <= 3; r++) p.px(x, t + r, bStripe(x, r));
    p.rect(0, t + 4, 15, t + 7, B_.M); p.hl(0, 15, t + 8, B_.K); p.hl(0, 15, t + 9, B_.F);
    if (j.jr) p.vl(15, t + 4, t + 7, B_.N); // a seam between sections
    if (!j.jl) {
      p.vl(0, t, t + 9, B_.F); p.vl(1, t + 1, t + 8, B_.L);
      p.hl(3, 6, t + 5, B_.F); p.hl(3, 6, t + 6, B_.K); p.hl(3, 6, t + 7, B_.L); // the grip, sunk in
    }
    if (!j.jr) {
      p.vl(15, t, t + 9, B_.F); p.vl(14, t + 1, t + 8, B_.K);
      for (const [x, r] of [[12, 4], [13, 4], [10, 5], [11, 5], [12, 5], [13, 5], [10, 6], [11, 6], [12, 6], [13, 6], [12, 7], [13, 7]]) p.px(x, t + r, B_.Y); // a wedge: it slides left
    }
    return p;
  },
};

// ---- C: a heavy steel hatch, a red hand-wheel at its end, a latch bolt and rivets ----
const C_ = { O: '#1a1e25', H: '#c9d2de', L: '#8d99aa', M: '#66727f', N: '#474e59', K: '#30353e', R: '#d05050', r: '#491c1c', P: '#e07a6a' };
const WHEEL = [
  '..RRR..',
  '.R.R.R.',
  'R..R..R',
  'RRRrRRR',
  'R..R..R',
  '.R.R.R.',
  '..RRR..',
];
const C = {
  id: 'C', piece(open, mask, top) {
    const j = joins(mask), t = top;
    if (open) return openPiece(j, t, C_.O, (r, x) => (x === 1 ? (r === 1 ? C_.L : r === 8 ? C_.K : C_.M) : r === 4 || r === 5 ? C_.R : r === 8 ? C_.K : C_.N), 4);
    const p = new Pic(16, 16);
    p.hl(0, 15, t, C_.O); p.hl(0, 15, t + 1, C_.L); p.rect(0, t + 2, 15, t + 7, C_.M); p.hl(0, 15, t + 8, C_.K); p.hl(0, 15, t + 9, C_.O);
    const rivet = (x, r) => { p.px(x, t + r, C_.H); p.px(x + 1, t + r, C_.K); };
    for (const x of [4, 12]) { rivet(x, 2); rivet(x, 7); }
    if (j.jr) { p.vl(15, t + 2, t + 8, C_.K); p.vl(14, t + 2, t + 7, C_.N); }
    if (!j.jl) {
      p.vl(0, t, t + 9, C_.O); p.vl(1, t + 1, t + 8, C_.L); p.vl(2, t + 2, t + 7, C_.N);
      p.rect(3, t + 1, 11, t + 8, C_.M); // the wheel's seat, clear of rivets
      p.hl(3, 11, t + 1, C_.L);
      WHEEL.forEach((row, dy) => [...row].forEach((ch, dx) => {
        if (ch === '.') return;
        const x = 4 + dx, y = t + 1 + dy;
        p.px(x, y, ch === 'R' ? (dy < 3 && dx < 4 ? C_.P : C_.R) : C_.r);
      }));
    }
    if (!j.jr) {
      p.vl(15, t, t + 9, C_.O); p.vl(14, t + 1, t + 8, C_.K); p.vl(13, t + 2, t + 7, C_.N);
      p.rect(11, t + 3, 12, t + 6, C_.K); p.px(11, t + 3, C_.H); p.vl(12, t + 4, t + 6, C_.O); // the latch bolt
    }
    return p;
  },
};

// ---- D: a grating in an orange frame: steel slats you see the room through ----
const D_ = { A: '#503117', B: '#d59765', C: '#c97a3a', D: '#9d5f2d', S: '#8d99aa', T: '#56606e', U: '#3b414c' };
const D = {
  id: 'D', piece(open, mask, top) {
    const j = joins(mask), t = top;
    if (open) return openPiece(j, t, D_.A, r => (r === 1 ? D_.B : r === 8 ? D_.D : r % 2 ? D_.S : D_.T));
    const p = new Pic(16, 16);
    p.hl(0, 15, t, D_.A); p.hl(0, 15, t + 1, D_.B); p.hl(0, 15, t + 8, D_.D); p.hl(0, 15, t + 9, D_.A);
    for (let x = 0; x < 16; x += 2) { p.px(x, t + 2, D_.S); p.vl(x, t + 3, t + 6, D_.T); p.px(x, t + 7, D_.U); }
    if (!j.jl) { p.vl(0, t, t + 9, D_.A); p.vl(1, t + 1, t + 8, D_.B); p.vl(2, t + 2, t + 7, D_.C); }
    if (!j.jr) { p.vl(15, t, t + 9, D_.A); p.vl(14, t + 1, t + 8, D_.D); p.vl(13, t + 2, t + 7, D_.C); }
    return p;
  },
};

// ---- E: hazard stripes all over, in a dark steel frame ----
const E_ = { O: '#1a1e25', L: '#b0b7c5', Y: '#e8c547', y: '#f4dc7a', V: '#b8962e', Z: '#2a2f3a', z: '#474e59', W: '#1f232b', K: '#30353e' };
const eStripe = (x, r) => {
  const yellow = ((x + r) & 3) < 2;
  return r === 1 ? (yellow ? E_.y : E_.z) : r === 8 ? (yellow ? E_.V : E_.W) : yellow ? E_.Y : E_.Z;
};
const E = {
  id: 'E', piece(open, mask, top) {
    const j = joins(mask), t = top;
    if (open) return openPiece(j, t, E_.O, r => (((r >> 1) & 1) ? E_.Y : E_.Z));
    const p = new Pic(16, 16);
    p.hl(0, 15, t, E_.O); p.hl(0, 15, t + 9, E_.O);
    for (let x = 0; x < 16; x++) for (let r = 1; r <= 8; r++) p.px(x, t + r, eStripe(x, r));
    if (!j.jl) { p.vl(0, t, t + 9, E_.O); p.vl(1, t + 1, t + 8, E_.L); }
    if (!j.jr) { p.vl(15, t, t + 9, E_.O); p.vl(14, t + 1, t + 8, E_.K); }
    return p;
  },
};

module.exports = { CONCEPTS: { A, B, C, D, E }, MASKS, ART };

// The crusher candidates: a fed station on power, 4x4 cells (32x32 px), as the Former and the smelter. Rock goes in
// at the hopper on top, is broken between what moves inside, seen through a grate, and leaves by the chute below.
// Each is a rest picture and four work pictures, overlays as the Former's: what moves, opaque over what it covers,
// with dust (part alpha) at the hopper's mouth and gravel falling from the chute.
const { Cv, C, ROCK, HAZARD, RUST } = require('./lib.js');

const CER = [0xd8d2c4, 0xa8a092];
const DUST = 0xc9c3b4;

// A hopper: rows y0..y1, x from l0..r0 at the top narrowing to l1..r1, a hazard-striped rim, rock heaped in it.
function hopper(c, y0, y1, l0, r0, l1, r1) {
  for (let y = y0 + 2; y <= y1; y++) {
    const t = (y - y0 - 2) / Math.max(1, y1 - y0 - 2), l = Math.round(l0 + (l1 - l0) * t), r = Math.round(r0 + (r1 - r0) * t);
    c.rect(l, y, r, y, C.mid);
    c.set(l, y, C.line); c.set(r, y, C.line); c.set(l + 1, y, C.steel); c.set(r - 1, y, C.dark);
  }
  c.rect(l0 - 1, y0, r0 + 1, y0 + 2, C.line);
  for (let x = l0; x <= r0; x++) { c.set(x, y0 + 1, ((x - l0) >> 1) % 2 ? C.line : HAZARD[0]); }
  // the heap of rock over the rim, highest in the middle
  const mid = (l0 + r0) / 2, half = (r0 - l0) / 2 - 2;
  for (let x = l0 + 2; x <= r0 - 2; x++) {
    const h = Math.round(2.2 * (1 - Math.abs(x + 0.5 - mid) / half) + ((x * 5) % 3 === 0 ? 0.6 : 0));
    for (let k = 1; k <= h; k++) c.set(x, y0 - k, k === h ? ROCK[(x % 3) ? 0 : 1] : ROCK[(x * 7) % 3 ? 1 : 2]);
    if (h > 0) c.set(x, y0 - h - 1, C.line);
  }
  c.rect(l0 + 2, y0 + 3, r0 - 3, y0 + 3, C.dark2);
}

// A toothed roller about (cx, cy), turned `turn` of a tooth, inside the box clip.
function roller(c, cx, cy, turn, clip) {
  for (let y = Math.floor(cy - 5); y <= cy + 5; y++) for (let x = Math.floor(cx - 5); x <= cx + 5; x++) {
    if (x < clip[0] || x > clip[2] || y < clip[1] || y > clip[3]) continue;
    const dx = x + 0.5 - cx, dy = y + 0.5 - cy, d = Math.hypot(dx, dy);
    if (d <= 1.1) c.set(x, y, C.hi);
    else if (d <= 3.1) c.set(x, y, dx + dy < -1 ? C.mid : dx + dy > 1.5 ? C.dark2 : C.mid2);
    else if (d <= 4.4) {
      const a = ((Math.atan2(dy, dx) / (Math.PI / 4) - turn) % 1 + 1) % 1;
      if (a < 0.5) c.set(x, y, dx + dy < 0 ? C.steel : C.dark);
    }
  }
}

// Dust at the hopper's mouth and gravel falling from a chute at (x, y), frame f.
function dustAndGravel(c, f, puffs, chute) {
  for (const [k, [x, y]] of puffs.entries()) {
    const ph = (f + k) % 4;
    for (const [dx, dy] of [[0, 0], [1, -1], [-1, -1], [2, -2]].slice(0, ph + 1)) c.set(x + dx * (k % 2 ? 1 : -1), y + dy - ph, DUST, 150 - ph * 25);
  }
  const [gx, gy, len] = chute;
  for (let k = 0; k < 3; k++) {
    const y = gy + ((f + k * 2) % len);
    c.set(gx + (k % 2), y, ROCK[k % 2 ? 1 : 0]);
  }
}

function feet(c, xs, y0) {
  for (const x0 of xs) { c.box(x0, y0, x0 + 3, 31, C.dark); c.rect(x0 + 1, y0 + 1, x0 + 1, 30, C.mid2); }
}

const D = [];

// A: two toothed rollers behind a grate, a motor beside them, its pulley turning.
const A = { win: [5, 11, 21, 22], rollers: [[9.5, 16.5], [16.5, 16.5]], bars: [5, 13, 21] };
function aMoving(c, turn) {
  const [x0, y0, x1, y1] = A.win;
  c.rect(x0 + 1, y0 + 1, x1 - 1, y1 - 1, C.deep);
  roller(c, A.rollers[0][0], A.rollers[0][1], turn, [x0 + 1, y0 + 1, x1 - 1, y1 - 1]);
  roller(c, A.rollers[1][0], A.rollers[1][1], -turn, [x0 + 1, y0 + 1, x1 - 1, y1 - 1]);
  for (const x of A.bars) { if (x === x0 || x === x1) continue; c.rect(x, y0 + 1, x, y1 - 1, C.dark2); c.set(x, y0 + 1, C.mid2); }
  // the pulley's spokes
  const px = 26.5, py = 13.5;
  for (let k = 0; k < 2; k++) { const a = turn * Math.PI / 2 + k * Math.PI / 2; for (const r of [-1.6, -0.8, 0.8, 1.6]) c.set(Math.floor(px + Math.cos(a) * r), Math.floor(py + Math.sin(a) * r), C.dark2); }
  c.set(26, 13, C.hi);
}
D.push({
  id: 'rollers', name: 'Валки',
  what: 'Бункер с жёлто-чёрной кромкой, за решёткой два зубчатых валка навстречу друг другу, справа мотор с ребрами и шкивом на изоляторах, снизу косой лоток',
  rest() {
    const c = new Cv(32, 32);
    feet(c, [2, 25], 26);
    // The chute: a spout under the rollers, leaning right.
    for (let y = 24; y <= 28; y++) { const l = 9 + (y - 24), r = 16 + (y - 24); c.rect(l, y, r, y, C.dark); c.set(l, y, C.line); c.set(r, y, C.line); c.set(l + 1, y, C.mid2); }
    c.rect(13, 29, 21, 29, C.line);
    // The housing.
    c.box(1, 8, 30, 25, C.mid);
    c.rect(2, 9, 29, 9, C.hi); c.rect(2, 24, 29, 24, C.dark);
    c.rect(2, 10, 2, 23, C.steel); c.rect(22, 10, 22, 23, C.dark2);
    for (const [x, y] of [[3, 11], [3, 22], [20, 23], [6, 23]]) c.set(x, y, C.hi);
    c.box(...A.win, C.deep);
    // The motor: a finned can with its pulley, two insulators over it.
    c.box(23, 10, 29, 23, C.dark);
    for (let y = 17; y <= 22; y += 2) c.rect(24, y, 28, y, C.dark2);
    c.rect(24, 11, 28, 11, C.mid2);
    for (let y = 11; y <= 16; y++) for (let x = 24; x <= 29; x++) { const d = Math.hypot(x + 0.5 - 26.5, y + 0.5 - 13.5); if (d <= 2.6) c.set(x, y, d > 1.8 ? C.line : C.mid2); }
    for (const x of [24, 28]) { c.rect(x, 5, x + 1, 7, CER[1]); c.rect(x, 5, x, 6, CER[0]); c.rect(x - 1, 7, x + 2, 7, C.line); }
    hopper(c, 2, 8, 3, 21, 7, 17);
    aMoving(c, 0);
    return c;
  },
  work(f) {
    const c = new Cv(32, 32);
    aMoving(c, f / 4);
    // a stone in the nip, sinking
    c.set(13, 12 + f * 2, ROCK[0]); c.set(13, 13 + f * 2, ROCK[2]);
    dustAndGravel(c, f, [[8, 4], [16, 4]], [18, 29, 3]);
    return c;
  },
});

// B: a jaw crusher, a fixed jaw and a swinging one in a V, its flywheel on top turning the eccentric.
const B = { win: [4, 10, 20, 23] };
function jaws(c, swing) {
  const [x0, y0, x1, y1] = B.win;
  c.rect(x0 + 1, y0 + 1, x1 - 1, y1 - 1, C.deep);
  for (let y = y0 + 1; y <= y1 - 1; y++) {
    const t = (y - y0 - 1) / (y1 - y0 - 2);
    const lf = Math.round(x0 + 1 + 4 * t), rs = Math.round(x1 - 1 - 4 * t - swing * (0.4 + 0.6 * (1 - t)));
    c.rect(x0 + 1, y, lf, y, C.dark); c.set(lf - 1, y, C.mid2); c.set(lf, y, (y % 2) ? C.hi : C.mid2);
    c.rect(rs, y, x1 - 1, y, C.mid2); c.set(rs + 1, y, C.mid); c.set(rs, y, (y % 2) ? C.hi : C.dark);
  }
  c.rect(x1 - 1, y0 + 1, x1 - 1, y1 - 1, C.dark2);
}
function wheel(c, turn) {
  const WX = 25.5, WY = 7.5, R = 5.6;
  for (let y = 1; y <= 14; y++) for (let x = 19; x <= 31; x++) {
    const d = Math.hypot(x + 0.5 - WX, y + 0.5 - WY);
    if (d > R + 0.5) continue;
    c.set(x, y, d > R - 0.6 ? C.line : d > R - 1.6 ? C.mid2 : C.back);
  }
  const a = turn * Math.PI * 2;
  for (let r = 0; r < R - 1.4; r += 0.5) c.set(Math.floor(WX + Math.cos(a) * r), Math.floor(WY + Math.sin(a) * r), C.dark2);
  // the eccentric pin, out on its crank
  c.set(Math.floor(WX + Math.cos(a) * 2.6), Math.floor(WY + Math.sin(a) * 2.6), C.hi);
  c.rect(25, 7, 26, 8, C.hi);
}
D.push({
  id: 'jaw', name: 'Щёки',
  what: 'Щековая: за окном неподвижная щека слева и качающаяся справа, клином вниз; сверху справа маховик с эксцентриком, как у Формовщика; бункер слева, лоток снизу',
  rest() {
    const c = new Cv(32, 32);
    feet(c, [2, 25], 27);
    for (let y = 24; y <= 28; y++) { const l = 8 + (y - 24), r = 13 + (y - 24); c.rect(l, y, r, y, C.dark); c.set(l, y, C.line); c.set(r, y, C.line); c.set(l + 1, y, C.mid2); }
    c.rect(12, 29, 18, 29, C.line);
    c.box(1, 9, 30, 26, C.mid);
    c.rect(2, 10, 29, 10, C.hi); c.rect(2, 25, 29, 25, C.dark);
    c.rect(2, 11, 2, 24, C.steel);
    // the pitman: an arm down from the wheel to the swinging jaw, behind a cover
    c.box(21, 13, 29, 24, C.dark);
    c.rect(22, 14, 28, 14, C.mid2);
    for (const y of [17, 19, 21]) c.rect(23, y, 27, y, C.dark2);
    c.rect(24, 12, 27, 13, C.line);
    c.box(...B.win, C.deep);
    for (const x of [8, 12, 16]) c.set(x, 25, C.hi);
    // insulators, the power in
    for (const x of [21, 28]) { c.rect(x, 22, x, 22, CER[0]); }
    hopper(c, 2, 9, 2, 17, 6, 14);
    jaws(c, 0);
    wheel(c, 0);
    return c;
  },
  work(f) {
    const c = new Cv(32, 32);
    jaws(c, [0, 1, 2, 1][f]);
    wheel(c, f / 4);
    c.set(12, 14 + f * 2, ROCK[0]); c.set(11, 15 + f * 2, ROCK[2]);
    dustAndGravel(c, f, [[7, 5], [13, 5]], [16, 29, 3]);
    return c;
  },
});

// C: a hammer mill: a round drum, its rotor of swinging hammers seen through a porthole grate, a belt to a motor below.
const MX = 15.5, MY = 15.5, MR = 9.6;
function rotor(c, turn) {
  for (let y = 9; y <= 22; y++) for (let x = 9; x <= 22; x++) { const d = Math.hypot(x + 0.5 - MX, y + 0.5 - MY); if (d <= 6.1) c.set(x, y, C.deep); }
  for (let k = 0; k < 4; k++) {
    const a = (turn + k / 4) * Math.PI * 2;
    for (let r = 0; r <= 5.2; r += 0.4) {
      const x = Math.floor(MX + Math.cos(a) * r), y = Math.floor(MY + Math.sin(a) * r);
      c.set(x, y, r > 3.8 ? RUST[1] : C.mid2);
    }
  }
  for (let y = 13; y <= 18; y++) for (let x = 13; x <= 18; x++) { const d = Math.hypot(x + 0.5 - MX, y + 0.5 - MY); if (d <= 1.8) c.set(x, y, d < 0.9 ? C.hi : C.mid); }
  for (const x of [11, 15, 19]) for (let y = 9; y <= 22; y++) { const d = Math.hypot(x + 0.5 - MX, y + 0.5 - MY); if (d <= 6.1 && d > 2) c.set(x, y, C.dark2); }
}
D.push({
  id: 'mill', name: 'Молотковая',
  what: 'Круглый барабан с жёлтой полосой, в иллюминаторе с решёткой крутится ротор с молотками; бункер сверху слева, мотор снизу справа на ремне, лоток снизу',
  rest() {
    const c = new Cv(32, 32);
    feet(c, [3, 24], 27);
    c.box(2, 25, 29, 27, C.mid); c.rect(3, 26, 28, 26, C.steel);
    // The motor and its belt up to the drum's hub.
    c.box(22, 18, 30, 25, C.dark); for (const y of [20, 22]) c.rect(23, y, 29, y, C.dark2); c.rect(23, 19, 29, 19, C.mid2);
    // The drum.
    for (let y = 4; y <= 26; y++) for (let x = 4; x <= 27; x++) {
      const dx = x + 0.5 - MX, dy = y + 0.5 - MY, d = Math.hypot(dx, dy);
      if (d > MR + 0.6) continue;
      c.set(x, y, d > MR - 0.4 ? C.line : d > MR - 1.6 ? (dx + dy < 0 ? C.steel : C.dark) : (dy > 6.5 && dy < 8) ? HAZARD[1] : C.mid);
    }
    for (let y = 8; y <= 23; y++) for (let x = 8; x <= 23; x++) { const d = Math.hypot(x + 0.5 - MX, y + 0.5 - MY); if (d > 6.1 && d <= 7.2) c.set(x, y, C.line); }
    for (const [x, y] of [[8, 9], [22, 9], [8, 22], [22, 22]]) c.set(x, y, C.hi);
    // The hopper into its top left, the chute out of its bottom.
    for (let y = 0; y <= 7; y++) { const l = 2 + Math.round(y * 0.4), r = 11 - Math.round(y * 0.1); c.rect(l, y, r, y, C.mid); c.set(l, y, C.line); c.set(r, y, C.line); c.set(l + 1, y, C.steel); }
    c.rect(1, 0, 12, 1, C.line); for (let x = 2; x <= 11; x++) c.set(x, 0, ((x >> 1) % 2) ? C.line : HAZARD[0]);
    for (const x of [4, 6, 9]) c.set(x, 1, ROCK[1]);
    c.rect(13, 24, 18, 26, C.line); c.rect(14, 24, 17, 25, C.dark2);
    rotor(c, 0);
    return c;
  },
  work(f) {
    const c = new Cv(32, 32);
    rotor(c, f / 16);
    dustAndGravel(c, f, [[5, 2], [9, 2]], [15, 27, 4]);
    return c;
  },
});

module.exports = { D };

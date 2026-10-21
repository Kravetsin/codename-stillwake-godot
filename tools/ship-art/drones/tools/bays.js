// The drone bay, 32x32 (2x2 cells), three looks of a rack where drones sit on four pads, two shelves of two.
// Each look is a base picture (machine_droneBay.png) with its pads empty; the client draws over it, per slot:
//   the docked drone (drone_dock.png, lit like the bay) with its top-left at PADS[i];
//   the pad's charging glow (droneBay_pad.png, unlit) at GLOWS[i] while it charges;
//   its charge in four lamps on the shelf's lip (droneBay_charge_0..4.png, unlit; _low for one lamp amber) at LAMPS[i].
// The slots run as the sim's: top left, top right, bottom left, bottom right. Light from the upper left.
const { Pic } = require('./lib.js');

const OUT = '#1a1e25';
const S = { k: '#10161d', d: '#22262c', e: '#474c57', s: '#5b6170', m: '#6b7385', f: '#7f848f', K: '#8f99ad', D: '#9aa0ab', W: '#c9d2de' };
const R = { L: '#5a2a18', o: '#6a4430', R: '#8a5a3a', Q: '#b07a50', P: '#c98a55' };
const O = { d: '#5e4314', m: '#a17323', b: '#e0a030', h: '#ebc178' };
const HAZ = { y: '#e8be3c', k: '#2a2e36' };
const GREEN = { m: '#7ee36b', h: '#d9ffd0', d: '#3fae4a' }, AMBER = { m: '#f2b84b', h: '#ffe08a' }, SOCKET = '#1d2128';

// The same pads in every look, so the client keeps one table: two shelves, two pads each.
const PADS = [[2, 5], [16, 5], [2, 18], [16, 18]];            // the docked drone's top-left (its canvas is 14x12)
const GLOWS = [[5, 16], [19, 16], [5, 29], [19, 29]];         // the pad's plate, 8x1, under the drone's feet
const LAMPS = [[4, 18], [18, 18], [4, 30], [18, 30]];         // the charge lamps on the shelf's lip, 11x1

const panel = (p, x0, y0, x1, y1, [dark, base, lit]) => p.panel(x0, y0, x1, y1, [dark, base, lit]);
// the lamps' dark sockets on a lip
function sockets(p) { for (const [x, y] of LAMPS) for (let i = 0; i < 4; i++) p.hl(x + i * 3, x + i * 3 + 1, y, SOCKET); }
// a pad: its plate, two copper contacts at its ends
function pad(p, x, y, plate = S.m) { p.hl(x, x + 7, y, plate); p.px(x, y, R.Q); p.px(x + 7, y, R.Q); p.px(x + 1, y, S.K); }

// ======== A: the shelf. A steel rack, two shelves on three posts, a slatted back; a hazard stripe on its header. ========
function lookA() {
  const p = new Pic(32, 32);
  // the back between the posts: dark slats, a plug at the top of each place
  p.rect(3, 4, 28, 28, S.d);
  for (let x = 4; x <= 28; x += 3) p.vl(x, 4, 28, S.k);
  for (const [x, y] of PADS) { p.rect(x + 5, y, x + 7, y + 1, S.e); p.px(x + 6, y + 1, R.Q); p.hl(x + 5, x + 7, y - 1, S.k); }
  // a shadow under each shelf
  p.hl(3, 28, 19, S.k); p.hl(3, 28, 4, S.k);
  // the header: steel with a hazard stripe
  panel(p, 1, 1, 30, 3, [S.e, S.f, S.K]);
  for (let x = 3; x <= 28; x++) p.px(x, 2, (x >> 1) % 2 ? HAZ.y : HAZ.k);
  // posts
  for (const x of [1, 15, 29]) { p.rect(x, 4, x + 1, 30, S.s); p.vl(x, 4, 30, S.K); p.vl(x + 1, 4, 30, S.e); }
  for (const x of [1, 15, 29]) for (const y of [7, 12, 22, 26]) p.px(x, y, S.D);
  // shelves: the pad's row, the plate, the lip with its lamps
  for (const y of [16, 29]) {
    p.hl(3, 28, y, S.f); p.hl(3, 28, y + 1, S.K);
    if (y === 16) { p.hl(1, 30, 18, S.e); p.hl(1, 30, 17, S.K); }
    else p.hl(1, 30, 30, S.e);
  }
  for (const [x, y] of GLOWS) pad(p, x, y);
  sockets(p);
  return p.outline(OUT);
}

// ======== B: the hangar. A steel cabinet in the crew's orange trim, four deep alcoves, a shutter rolled up over each. ========
function lookB() {
  const p = new Pic(32, 32);
  panel(p, 1, 1, 30, 30, [S.e, S.s, S.K]);
  // orange trim down the sides and across the top
  p.hl(2, 29, 2, O.b); p.hl(2, 29, 3, O.m); p.vl(2, 4, 29, O.m); p.vl(29, 4, 29, O.d);
  p.px(2, 2, O.h); p.px(3, 2, O.h);
  for (const [x, y] of PADS) {
    const x0 = x + 1, x1 = x + 12, y0 = y, y1 = y + 10;
    // the alcove: deep, its ceiling in shadow, its left wall lit by the room, its right in shade
    p.rect(x0, y0, x1, y1, S.d);
    p.hl(x0, x1, y0, S.k); p.hl(x0, x1, y0 + 1, S.k);
    p.vl(x0, y0 + 1, y1, S.e); p.vl(x1, y0 + 1, y1, '#1a1e25');
    // the shutter rolled up at its top: ribs
    p.hl(x0, x1, y0, S.f); p.hl(x0 + 1, x1 - 1, y0 + 1, S.e); for (let i = x0 + 1; i < x1; i += 2) p.px(i, y0, S.K);
    // a plug at the back
    p.rect(x + 6, y + 3, x + 7, y + 4, S.e); p.px(x + 6, y + 4, R.Q);
  }
  // the floors of the alcoves are the pads; the lips below them carry the lamps
  for (const y of [16, 29]) { p.hl(3, 28, y, S.f); p.hl(2, 29, y + 1, S.K); }
  p.hl(2, 29, 18, S.e); p.hl(2, 29, 30, S.e);
  // the middle post between the alcoves
  p.vl(15, 4, 29, S.f); p.vl(16, 4, 29, S.e);
  for (const y of [8, 23]) { p.px(15, y, S.D); }
  for (const [x, y] of GLOWS) pad(p, x, y);
  sockets(p);
  return p.outline(OUT);
}

// ======== C: the perch. A mast with an arm either side; drones perch on the arms and on the base, a lamp on top. ========
function lookC() {
  const p = new Pic(32, 32);
  // the mast, a round steel column, a cable up it
  for (let y = 3; y <= 29; y++) { p.px(14, y, S.K); p.px(15, y, S.f); p.px(16, y, S.m); p.px(17, y, S.e); }
  for (let y = 5; y <= 28; y += 4) { p.px(14, y, S.D); p.hl(15, 17, y, S.s); }
  for (let y = 4; y <= 28; y++) p.px(16, y, y % 2 ? R.o : R.L);
  // its cap and the lamp housing
  p.rect(13, 2, 18, 3, S.f); p.hl(13, 18, 2, S.K); p.hl(14, 17, 1, S.e); p.px(15, 1, S.d); p.px(16, 1, S.d);
  // the arms: a beam out to each side under the upper pads, a brace from the mast below it
  for (const [x0, x1] of [[3, 13], [18, 28]]) {
    p.hl(x0, x1, 16, S.f); p.hl(x0, x1, 17, S.K); p.hl(x0, x1, 18, S.e);
    p.px(x0, 17, S.D);
  }
  p.line(13, 23, 8, 19, S.s); p.line(13, 24, 8, 20, S.e);
  p.line(18, 23, 23, 19, S.s); p.line(18, 24, 23, 20, S.e);
  // the base: a wide plate with feet
  p.hl(2, 29, 29, S.f); p.hl(2, 29, 30, S.e); p.hl(3, 28, 28, S.s); p.px(2, 29, S.K);
  p.rect(12, 26, 19, 28, S.s); p.hl(12, 19, 26, S.K); p.vl(19, 26, 28, S.e);
  for (const [x, y] of GLOWS) pad(p, x, y);
  sockets(p);
  return p.outline(OUT);
}

// ---- the overlays, the same for every look ----
// the pad's glow while it charges: its plate lit green
function padGlow() { const p = new Pic(8, 1); p.hl(0, 7, 0, GREEN.m); p.px(0, 0, GREEN.d); p.px(7, 0, GREEN.d); p.px(3, 0, GREEN.h); p.px(4, 0, GREEN.h); return p; }
// the charge: n of four lamps lit green; the low frame one amber
function chargeLamps(n, low) {
  const p = new Pic(11, 1);
  for (let i = 0; i < 4; i++) { const on = i < n; p.hl(i * 3, i * 3 + 1, 0, on ? (low ? AMBER.m : GREEN.m) : SOCKET); if (on) p.px(i * 3, 0, low ? AMBER.h : GREEN.h); }
  return p;
}
const charges = () => ({ levels: [0, 1, 2, 3, 4].map(n => chargeLamps(n, false)), low: chargeLamps(1, true) });

// the lamp on the C perch's mast gives light to the room around it; others none of their own
const LOOKS = {
  A: { name: 'Стеллаж', make: lookA },
  B: { name: 'Ангар', make: lookB },
  C: { name: 'Насест', make: lookC, beacon: [15, 1] },
};
function build(id) { return { base: LOOKS[id].make(), name: LOOKS[id].name, beacon: LOOKS[id].beacon || null }; }
module.exports = { LOOKS, build, PADS, GLOWS, LAMPS, padGlow, charges, GREEN, AMBER };

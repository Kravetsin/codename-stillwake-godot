// The drones, three looks. Each look is a flight picture 18x12 in four rotor frames (drone_fly_0..3), a docked one
// with its rotors still (drone_dock), the status lamp lit sky or amber (drone_light_work / drone_light_low, drawn
// unlit over the body, turned with it), and the item's icons (items/drone, items/drone_empty, 16x16).
// The body's middle is ANCHOR; the clamp the cargo hangs from is CLAMP; the lamp LAMP. Light from the upper left.
const { Pic } = require('./lib.js');

const W = 18, H = 12, ANCHOR = [9, 6];
// the docked pose: folded to sit two to a shelf, its shape within x 2..11, its feet on row 10, the outline under them on row 11
const DW = 14, DH = 12;
const OUT = '#1a1e25', ICON_OUT = '#14171e';
// the stations' steel, darkest first, as the smelter, Former and furnace have it
const S = { k: '#10161d', d: '#22262c', e: '#474c57', s: '#5b6170', m: '#6b7385', f: '#7f848f', K: '#8f99ad', D: '#9aa0ab', W: '#c9d2de', w: '#eef2f7' };
const STEEL = [S.e, S.s, S.m, S.f, S.K, S.D, S.W];
// the crew's orange, as the hero's suit has it
const O = { d: '#5e4314', m: '#a17323', b: '#e0a030', h: '#ebc178', w: '#f6dca6' };
const ORANGE = [O.d, '#7d5a1b', O.m, '#c38a2a', O.b, O.h, O.w];
// hazard yellow, deeper than the crew's orange
const Y = ['#5c4708', '#836410', '#a88218', '#cfa024', '#e8be3c', '#f4d66a', '#fbeaa4'];
// the drones' sky (the bay's pads and zone, the logistics colour)
const SKY = { k: '#0d2333', d: '#1d5f86', m: '#3fa6d8', b: '#7dd3fc', h: '#e3f7ff' };
const AMBER = { d: '#9a6418', m: '#f2b84b', h: '#ffe08a' };
const BLADE = '#e2e8f0', BLUR = '#e2e8f05c', BLUR2 = '#e2e8f030';

// a body shaded as a rounded solid: each pixel's tone from how its face turns to a light at the upper left
function solid(p, inside, x0, y0, x1, y1, ramp, { flat = 0.35, tag } = {}) {
  const cx = (x0 + x1 + 1) / 2, cy = (y0 + y1 + 1) / 2, rx = (x1 - x0 + 1) / 2, ry = (y1 - y0 + 1) / 2;
  const L = [-0.55, -0.7, 0.45], n2 = Math.hypot(...L), l = L.map(v => v / n2);
  for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
    if (!inside(x, y)) continue;
    const u = (x + 0.5 - cx) / rx, v = (y + 0.5 - cy) / ry, z = Math.sqrt(Math.max(0.05, 1 - Math.min(1, u * u * (1 - flat) + v * v * (1 - flat))));
    const n = [u * (1 - flat), v * (1 - flat), z], nn = Math.hypot(...n);
    const b = (n[0] * l[0] + n[1] * l[1] + n[2] * l[2]) / nn;
    const i = Math.max(0, Math.min(ramp.length - 1, Math.round((b + 0.15) / 1.1 * (ramp.length - 1))));
    p.px(x, y, ramp[i], tag);
  }
}
const ellipse = (cx, cy, rx, ry) => (x, y) => ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1;
const box = (x0, y0, x1, y1) => (x, y) => x >= x0 && x <= x1 && y >= y0 && y <= y1;

// a rotor's blade seen edge-on: its length beats as it turns (frame 0 broadside, 2 end-on), over a faint disc
function blade(p, cx, y, frame, half) {
  const k = [1, 0.62, 0.2, 0.62][frame], h = Math.max(0.5, half * k);
  for (let x = Math.floor(cx - half); x <= Math.ceil(cx + half) - 1; x++) p.px(x, y, Math.abs(x + 0.5 - cx) <= half - 1 ? BLUR : BLUR2);
  for (let x = Math.round(cx - h); x < Math.round(cx + h); x++) p.px(x, y, BLADE);
}

// one character a tone, for the hand-drawn rows below
const CH = {
  k: S.k, d: S.d, e: S.e, s: S.s, m: S.m, f: S.f, K: S.K, D: S.D, W: S.W, w: S.w,
  1: SKY.k, 2: SKY.d, 3: SKY.m, 4: SKY.b, 5: SKY.h,
  a: O.d, b: '#7d5a1b', c: O.m, g: '#c38a2a', h: O.b, i: O.h, j: O.w,
  p: Y[0], q: Y[1], r: Y[2], t: Y[3], u: Y[4], v: Y[5], x: Y[6],
  n: '#2a2e36', N: '#3a3f4a', z: '#2a3340', S: '#bf9a2a',
};
const rows = (p, r, y0 = 0) => p.stamp(0, y0, r, CH);

// ======== A: the steel quad. A rounded steel shell with a sky eye, two motor cans on short arms. ========
function lookA() {
  const shape = still => {
    const p = new Pic(W, H);
    rows(p, [
      '..................',
      '..................',
      '.ed............ed.',
      '.DK....ed......Kf.',
      '.KfmmKWWDDKKfmmfm.',
      '.se.fDDKKKfz34.se.',
      '....fKKfffm122....',
      '....smKmKmsssse...',
      '.....eeeeeedd.....',
      '........Kf........',
      '........fs........',
    ]);
    if (still) rows(p, ['.....e......e.....', '....se......se....'], 9);
    if (still) { p.clear(8, 10); p.clear(9, 10); }
    return p;
  };
  const fly = [0, 1, 2, 3].map(f => { const p = shape(false).outline(OUT); blade(p, 2, 1, f, 3); blade(p, 16, 1, (f + 2) % 4, 3); return p; });
  // docked: arms folded down, the motor cans its feet, the blades folded flat over its back
  const dock = new Pic(DW, DH);
  rows(dock, [
    '..............',
    '..............',
    '...WDDDKKff...',
    '....ed.e......',
    '...KWWDDKKf...',
    '..fDDKKKfz34..',
    '..fKKfffm122..',
    '..smKmKmssse..',
    '...eeeeeedd...',
    '..Ke......fe..',
    '..se......se..',
  ]);
  dock.outline(OUT);
  return { fly, dock, dockEye: [[10, 5], [11, 5], [10, 6], [11, 6]], lamp: [[7, 3], [8, 3]], eye: [[12, 5], [13, 5], [12, 6], [13, 6]], clamp: [9, 11], feet: 11 };
}

// ======== B: the bumblebee. A round body in the crew's orange, a dark band, two ducted fans, a big eye. ========
function lookB() {
  const shape = still => {
    const p = new Pic(W, H);
    rows(p, [
      '..................',
      '..................',
      '........ed........',
      '.mDDK..hiih...KDDf',
      '.Kkkf.hijih5.fkkm.',
      '.ffmshiihh134smms.',
      '..ee.ghhhg123.ee..',
      '.....NNnnnnnn.....',
      '......cggccb......',
      '.......bbaa.......',
      '........Kf........',
    ]);
    if (still) { rows(p, ['.....e......e.....', '....se......se....'], 9); p.clear(8, 10); p.clear(9, 10); }
    return p;
  };
  const fan = (p, f) => {
    // the fans' blades go by inside the ducts' mouths, the air over them shimmering
    for (const x0 of [2, 14]) {
      p.hl(x0, x0 + 1, 4, S.k);
      if (f === 0) p.px(x0, 4, S.W); else if (f === 1) p.px(x0 + 1, 4, S.W); else if (f === 2) { p.px(x0, 4, S.e); p.px(x0 + 1, 4, S.e); }
      p.hl(x0 - 1, x0 + 2, 2, f % 2 ? BLUR : BLUR2);
    }
  };
  const fly = [0, 1, 2, 3].map(f => { const p = shape(false).outline(OUT); fan(p, f); return p; });
  // docked: the ducts turned up beside it like folded wings, feet out
  const dock = new Pic(DW, DH);
  rows(dock, [
    '..............',
    '..............',
    '..mK......Kf..',
    '..Kk..hiihkm..',
    '..fkhijih5fs..',
    '..sihiihh134..',
    '...eghhhg123..',
    '...NNnnnnnn...',
    '....cggccb....',
    '...e.bbaa.e...',
    '..se......se..',
  ]);
  dock.outline(OUT);
  return { fly, dock, dockEye: [[9, 5], [10, 5], [10, 6], [9, 4]], lamp: [[8, 2], [9, 2]], eye: [[11, 5], [12, 5], [12, 6], [11, 4]], clamp: [9, 11], feet: 11 };
}

// ======== C: the lifter. A boxy body in hazard yellow, a striped belly, one wide rotor on a mast, a tail with the lamp. ========
function lookC() {
  const shape = still => {
    const p = new Pic(W, H);
    rows(p, [
      '..................',
      '..................',
      '.........e........',
      '.e.......s........',
      '.s....uvvuuuttt...',
      '.m...vvuuutttz34..',
      '.mKKftuttttss123..',
      '..eesrrrrrqqqqq...',
      '.....NrrNNrrNNrN..',
      '......sesssssse...',
      '........Kfs.......',
    ]);
    if (still) { p.clear(8, 10); p.clear(9, 10); p.clear(10, 10); rows(p, ['......ee....ee....', '.....eeeeeeeeee...'], 9); p.clear(6, 9); p.clear(7, 9); p.clear(12, 9); p.clear(13, 9); rows(p, ['......sesssssse...'], 9); rows(p, ['......d......d....', '.....eeeeeeeeee...'], 10); }
    return p;
  };
  const fly = [0, 1, 2, 3].map(f => { const p = shape(false).outline(OUT); blade(p, 9.5, 1, f, 8); return p; });
  // docked: the tail folded up behind it, the blade folded back along its roof, skids down
  const dock = new Pic(DW, DH);
  rows(dock, [
    '..............',
    '..............',
    '..eWDDDKKf....',
    '..s....e......',
    '..m.uvvuutt...',
    '..mvvuuuttz3..',
    '..KtutttsS12..',
    '..frrrrqqqqq..',
    '...NrrNNrrN...',
    '....d....d....',
    '...eeeeeeee...',
  ]);
  dock.outline(OUT);
  return { fly, dock, dockEye: [[10, 5], [11, 5], [11, 6], [10, 6]], lamp: [[1, 3]], eye: [[14, 5], [15, 5], [14, 6], [15, 6]], clamp: [9, 11], feet: 11 };
}

// the lamp lit, as an overlay drawn unlit over the body, and the eye with it
function lights(look) {
  const work = new Pic(W, H), low = new Pic(W, H);
  for (const [x, y] of look.lamp) { work.px(x, y, SKY.b); low.px(x, y, AMBER.m); }
  work.px(look.lamp[0][0], look.lamp[0][1], SKY.h); low.px(look.lamp[0][0], look.lamp[0][1], AMBER.h);
  return { work, low };
}

// the item's icon: the docked drone in a 16x16 square, its outline the items'; empty, its eye dark
function icons(look) {
  const make = dark => {
    const p = new Pic(16, 16);
    look.dock.each((x, y, c, t) => {
      let col = t === 'outline' ? ICON_OUT : c;
      if (dark && look.dockEye.some(([ex, ey]) => ex === x && ey === y)) col = '#1d2836';
      p.px(x + 1, y + 2, col);
    });
    return p;
  };
  return { icon: make(false), empty: make(true) };
}

const LOOKS = {
  A: { name: 'Стальной квадрик', make: lookA },
  B: { name: 'Шмель', make: lookB },
  C: { name: 'Грузчик', make: lookC },
};
function build(id) {
  const look = LOOKS[id].make();
  return { ...look, ...lights(look), ...icons(look), name: LOOKS[id].name, size: [W, H], anchor: ANCHOR };
}
module.exports = { LOOKS, build, W, H, DW, DH, ANCHOR, SKY, AMBER, S, O, OUT };

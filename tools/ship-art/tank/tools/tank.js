// The oxygen tank, 32x32 (2x2 cells), three looks. Each look is a base picture (machine_tank.png) showing the tank
// empty and dark, and fill frames tank_fill_0..N-1 (32x32, only what glows, drawn unlit over the base): frame 0 is
// the empty look (a red mark), the last is full. Light from the upper left; the outline is added last.
const { Pic } = require('./lib.js');

const OUT = '#1a1e25';
// the stations' steel, darkest first, as the smelter, Former and assembler have it
const S = { k: '#10161d', d: '#22262c', e: '#474c57', s: '#5b6170', m: '#6b7385', f: '#7f848f', K: '#8f99ad', D: '#9aa0ab', B: '#a8a092' };
// copper and rust, as the furnace and the smelter's coils have them
const R = { M: '#3a1a12', L: '#5a2a18', o: '#6a4430', R: '#8a5a3a', Q: '#b07a50', P: '#c98a55' };
// the pipes' own steel, so the stub at the port meets the pipe behind it
const PIPE = { lit: '#7b838e', mid: '#56606e', dark: '#434b56', rim: '#2f353d' };
// oxygen, the colour the game gives it everywhere (gauge, pipes overlay, motes)
const O2 = { deep: '#1d5f86', low: '#3fa6d8', mid: '#6fd3ff', hi: '#a6e6ff', top: '#e3f7ff', glint: '#f2fbff' };
const RED = { dark: '#4a1c1c', mid: '#8c2a24', hi: '#e03030', glint: '#ff9a8a' };
const AMBER = { mid: '#f2b84b', hi: '#ffe08a', dark: '#9a6418' };

// a vertical cylinder's tone by column: a lit edge, a highlight band, the face, shade toward the right
function cylTone(t, ramp) { let c = ramp[0][1]; for (const [p, col] of ramp) if (t >= p) c = col; return c; }
const STEEL_RAMP = [[0, S.m], [0.06, S.D], [0.16, S.K], [0.34, S.f], [0.68, S.m], [0.84, S.s], [0.95, S.e]];
function cyl(p, x0, x1, y0, y1, ramp = STEEL_RAMP, tag) {
  for (let x = x0; x <= x1; x++) p.vl(x, y0, y1, cylTone((x - x0) / (x1 - x0), ramp), tag);
}
// a band round the cylinder: the same ramp one step darker, rivets on its lit half
function band(p, x0, x1, y, ramp = STEEL_RAMP, rivetEvery = 4) {
  for (let x = x0; x <= x1; x++) {
    const t = (x - x0) / (x1 - x0), c = cylTone(t, ramp);
    const darker = { [S.D]: S.f, [S.K]: S.m, [S.f]: S.s, [S.m]: S.e, [S.s]: S.e, [S.e]: S.d }[c] || S.e;
    p.px(x, y, darker);
  }
  for (let x = x0 + 2; x <= x1 - 2; x += rivetEvery) p.px(x, y, (x - x0) / (x1 - x0) < 0.6 ? S.D : S.m);
}
// the stub where the pipe comes in: the port is the bottom-left cell, the pipe runs through its middle (rows 23-24)
function portStub(p, x1) {
  for (let x = 0; x <= x1; x++) { p.px(x, 22, PIPE.rim); p.px(x, 23, PIPE.lit); p.px(x, 24, PIPE.dark); p.px(x, 25, PIPE.rim); }
  // a coupling, as the pipes have between pieces
  p.vl(2, 21, 26, PIPE.rim); p.vl(3, 21, 26, PIPE.rim); p.px(3, 22, PIPE.lit); p.px(3, 23, PIPE.lit); p.px(3, 24, PIPE.mid); p.px(3, 25, PIPE.dark);
}
const finish = p => p.outline(OUT);

// ============ A: a steel vessel with a sight glass; liquid oxygen rises in it ============
// Liquid oxygen is pale blue: the glass is backlit, so the liquid glows and lights the room a little.
function lookA() {
  const p = new Pic(32, 32);
  // legs on a ring, as the smelter's
  for (const x0 of [7, 21]) { p.rect(x0, 27, x0 + 3, 30, S.s); p.vl(x0, 27, 30, S.K); p.vl(x0 + 3, 27, 30, S.e); p.px(x0 + 1, 27, S.D); }
  p.panel(5, 25, 26, 26, [S.e, S.f, S.K]); p.px(7, 25, S.B); p.px(24, 25, S.B);
  // body and dome
  cyl(p, 6, 25, 6, 24);
  [[5, 7, 24], [4, 9, 22], [3, 12, 19]].forEach(([y, a, b]) => { for (let x = a; x <= b; x++) p.px(x, y, cylTone((x - 6) / 19, STEEL_RAMP)); });
  p.hl(12, 19, 3, S.D); p.hl(9, 11, 4, S.D); p.px(7, 5, S.D); p.px(8, 5, S.D);
  // the valve on top: stem and a copper handwheel
  p.px(15, 2, S.e); p.px(16, 2, S.d); p.hl(13, 18, 1, R.Q); p.px(13, 1, R.P); p.px(14, 1, R.P); p.px(18, 1, R.o);
  // seams with rivets
  band(p, 6, 25, 7); band(p, 6, 25, 23);
  // the sight glass: a dark frame, copper bolts at its corners, glass 6x10
  p.rect(12, 10, 19, 21, S.d); p.hl(12, 19, 10, S.e); p.vl(12, 10, 21, S.e);
  for (const [x, y] of [[12, 10], [19, 10], [12, 21], [19, 21]]) p.px(x, y, R.Q);
  p.rect(13, 11, 18, 20, S.k);
  // the dark glass catches a little light at its upper left
  p.px(13, 12, '#2a3d4f'); p.px(14, 11, '#2a3d4f'); p.px(13, 13, '#1d2a38');
  // graduation ticks beside the glass, one a fifth
  for (const y of [12, 14, 16, 18]) p.hl(20, 21, y + 1, S.e);
  // the empty lamp, dark until the tank is empty
  p.rect(21, 9, 23, 10, S.d); p.rect(21, 9, 22, 10, RED.dark); p.px(21, 9, '#5e2622');
  portStub(p, 6);
  finish(p);

  const LEVELS = 5, frames = [];
  // frame 0: empty; the lamp lights red
  { const f = new Pic(32, 32); f.rect(21, 9, 22, 10, RED.hi); f.px(21, 9, RED.glint); frames.push(f); }
  for (let lv = 1; lv <= LEVELS; lv++) {
    const f = new Pic(32, 32), h = lv * 2, top = 20 - h + 1; // glass rows 11..20 hold the liquid; row 10 is the cap
    for (let y = top; y <= 20; y++) for (let x = 13; x <= 18; x++) f.px(x, y, x === 13 ? O2.hi : x >= 17 ? O2.low : O2.mid);
    f.hl(13, 18, top, O2.top); f.px(18, top, O2.hi);
    f.hl(14, 17, 20, O2.low); // the bottom deepens
    // bubbles rising
    if (lv >= 2) f.px(15, top + 2, O2.top);
    if (lv >= 3) f.px(17, top + 4, O2.hi);
    if (lv >= 4) f.px(14, top + 6, O2.top);
    // the glass's glint over the liquid, where it reaches
    for (const [x, y] of [[13, 12], [14, 11], [13, 13]]) if (y >= top) f.px(x, y, O2.glint);
    frames.push(f);
  }
  return {
    base: p, frames,
    // light: the glass's middle, pale blue, more as it fills; the red lamp when empty
    lights: frames.map((_, i) => i === 0 ? { at: [22, 10], colour: RED.hi, level: 0.25 } : { at: [16, 16], colour: O2.mid, level: [0, 0.18, 0.24, 0.3, 0.36, 0.42][i] }),
  };
}

// ============ B: two bottles in a frame, a column of lamps between them, one a canister ============
// Four lamps, a canister's worth each: a dark lamp is room for one more canister; with all four lit a canister no
// longer fits. Empty, the bottom lamp shows red.
const PAINT = { dark: '#2a3d4f', mid: '#3a536a', hi: '#5f86a8', top: '#9fbfda' }; // the old machines' blue, kept as the bottles' painted shoulders
const PAINT_RAMP = [[0, PAINT.mid], [0.08, PAINT.top], [0.2, PAINT.hi], [0.42, PAINT.mid], [0.8, PAINT.dark]];
function bottle(p, x0) {
  const x1 = x0 + 8;
  cyl(p, x0, x1, 7, 25);
  // shoulders, painted the oxygen blue, and the neck
  for (let x = x0 + 1; x <= x1 - 1; x++) p.px(x, 5, cylTone((x - x0) / 8, PAINT_RAMP));
  cyl(p, x0, x1, 6, 8, PAINT_RAMP);
  for (let x = x0 + 3; x <= x1 - 3; x++) p.px(x, 4, cylTone((x - x0) / 8, STEEL_RAMP));
  p.rect(x0 + 3, 2, x1 - 3, 3, S.e); p.px(x0 + 3, 2, S.K); p.px(x0 + 3, 3, S.f);
  // the paint worn at the shoulder's edge
  p.px(x1 - 1, 8, S.s); p.px(x0 + 2, 8, S.K);
}
function lookB() {
  const p = new Pic(32, 32);
  // the frame's feet and base plate
  for (const x0 of [4, 24]) { p.rect(x0, 28, x0 + 3, 30, S.s); p.vl(x0, 28, 30, S.K); p.vl(x0 + 3, 28, 30, S.e); }
  p.panel(2, 26, 29, 27, [S.e, S.f, S.K]); for (const x of [4, 10, 21, 27]) p.px(x, 26, S.D);
  bottle(p, 3); bottle(p, 20);
  // the manifold: a pipe from neck to neck through the column's top
  p.hl(9, 22, 2, PIPE.lit); p.hl(9, 22, 3, PIPE.dark); p.px(8, 2, PIPE.mid); p.px(23, 2, PIPE.mid);
  // the lamp column between the bottles: a steel post, a dark window per lamp
  p.panel(12, 4, 19, 25, [S.e, S.f, S.K]);
  p.rect(13, 1, 18, 4, S.f); p.hl(13, 18, 1, S.K); p.px(13, 2, S.K); p.px(18, 2, S.e); p.px(18, 3, S.e); // the valve block on top
  p.hl(14, 17, 1, R.Q); p.px(14, 1, R.P);
  for (let i = 0; i < 4; i++) { const y = 7 + i * 4; p.rect(13, y, 18, y + 3, S.k); p.rect(14, y + 1, 17, y + 2, '#1d2a38'); p.px(14, y + 1, '#2a3d4f'); }
  // straps across the bottles, bolted to the post
  for (const y of [10, 21]) { p.hl(3, 11, y, S.e); p.hl(20, 28, y, S.e); p.hl(4, 9, y, S.s); p.hl(21, 26, y, S.s); p.px(11, y, S.D); p.px(20, y, S.D); }
  portStub(p, 3);
  finish(p);

  const frames = [];
  const lamp = (f, i, c) => { const y = 7 + (3 - i) * 4; f.rect(14, y + 1, 17, y + 2, c.mid); f.hl(14, 16, y + 1, c.hi); f.px(14, y + 1, c.glint || c.top); };
  { const f = new Pic(32, 32); lamp(f, 0, { mid: RED.hi, hi: '#ff6a5a', glint: RED.glint }); frames.push(f); }
  for (let n = 1; n <= 4; n++) { const f = new Pic(32, 32); for (let i = 0; i < n; i++) lamp(f, i, { mid: O2.mid, hi: O2.hi, glint: O2.top }); frames.push(f); }
  return {
    base: p, frames,
    lights: frames.map((_, i) => i === 0 ? { at: [16, 21], colour: RED.hi, level: 0.25 } : { at: [16, 15], colour: O2.mid, level: [0, 0.16, 0.22, 0.28, 0.34][i] }),
  };
}

// ============ C: a tank knocked together from scrap, a pressure gauge on its front ============
// Mismatched plates welded and riveted, a rusted patch, chocks for feet, as the furnace is. The gauge's face is
// backlit: red empty, amber low, pale blue from half; its needle sweeps from the left to the right.
function lookC() {
  const p = new Pic(32, 32);
  // feet: a steel chock and a rusted one, as the furnace stands
  p.rect(5, 28, 9, 30, S.s); p.vl(5, 28, 30, S.D); p.vl(9, 28, 30, S.e);
  p.rect(22, 28, 26, 30, R.o); p.vl(22, 28, 30, R.R); p.vl(26, 28, 30, R.M);
  // the body: three plates, each its own steel
  cyl(p, 4, 27, 6, 27);
  const PLATE2 = [[0, S.s], [0.3, S.m], [0.7, S.s], [0.9, S.e]];
  for (let x = 4; x <= 27; x++) for (let y = 17; y <= 27; y++) if (x <= 15) p.px(x, y, cylTone((x - 4) / 23, [[0, S.m], [0.06, S.K], [0.16, S.m], [0.4, S.s]]));
  // a corrugated sheet patched on at the lower right, as the furnace's side, rust running down from its bolts
  for (let x = 17; x <= 26; x++) for (let y = 18; y <= 25; y++) p.px(x, y, (x - 17) % 2 ? S.e : S.s);
  p.hl(17, 26, 18, S.D); p.vl(17, 18, 25, S.K); p.hl(18, 26, 25, S.d);
  for (const [x, y0, y1] of [[19, 19, 23], [24, 20, 25], [21, 22, 24]]) { p.vl(x, y0, y1, R.R); p.px(x, y1, R.o); }
  p.px(20, 19, R.o); p.px(25, 21, R.o);
  for (const [x, y] of [[18, 19], [26, 19], [18, 24], [26, 24]]) p.px(x, y, S.B);
  // the weld seams: across at row 16, down at x 16
  p.hl(4, 27, 16, S.e); for (let x = 5; x <= 27; x += 2) p.px(x, 16, S.d);
  p.vl(16, 17, 27, S.e);
  for (let y = 18; y <= 26; y += 3) p.px(15, y, S.B);
  // the bottom ring
  p.hl(4, 27, 27, S.e); p.hl(5, 26, 27, S.s);
  // dome: patched, a pipe elbow out of its top to the right
  [[5, 5, 26], [4, 7, 24], [3, 10, 21]].forEach(([y, a, b]) => { for (let x = a; x <= b; x++) p.px(x, y, cylTone((x - 4) / 23, STEEL_RAMP)); });
  p.hl(10, 15, 3, S.D); p.hl(7, 9, 4, S.D);
  // a relief valve: a short stub, its copper cap
  p.rect(21, 2, 22, 4, PIPE.mid); p.vl(21, 2, 4, PIPE.lit); p.hl(20, 23, 1, R.Q); p.px(20, 1, R.P); p.px(23, 1, R.o);
  // a dent, hammered out, in the upper plate
  p.px(19, 9, S.m); p.px(20, 10, S.s); p.px(19, 10, S.K);
  // the hand valve on the dome's left
  p.px(8, 2, R.Q); p.px(9, 2, R.P); p.px(10, 2, R.Q); p.px(9, 1, R.o);
  // the gauge: a copper bezel, a dark face, centre (12, 10), on a bracket welded to the body
  gaugeFace(p, null);
  p.px(12, 16, S.d); p.px(12, 15, S.e);
  portStub(p, 4);
  finish(p);

  const frames = [];
  const FACES = [
    { face: '#c83030', hi: '#ff7a6a', rim: '#7a1e1e' },
    { face: AMBER.mid, hi: AMBER.hi, rim: AMBER.dark },
    { face: '#8fdcff', hi: O2.top, rim: O2.low },
    { face: '#8fdcff', hi: O2.top, rim: O2.low },
    { face: '#8fdcff', hi: O2.top, rim: O2.low },
  ];
  // needle tips round the centre, empty at the lower left to full at the lower right
  const TIPS = [[-3, 2], [-3, -1], [0, -3], [3, -1], [3, 2]];
  for (let i = 0; i < 5; i++) {
    const f = new Pic(32, 32);
    gaugeFace(f, FACES[i]);
    const [dx, dy] = TIPS[i];
    f.line(12, 10, 12 + dx, 10 + dy, '#10161d');
    f.px(12, 10, '#3a1a12');
    frames.push(f);
  }
  const LIGHT = ['#e03030', AMBER.mid, O2.mid, O2.mid, O2.mid];
  return { base: p, frames, lights: frames.map((_, i) => ({ at: [12.5, 10.5], colour: LIGHT[i], level: [0.25, 0.22, 0.24, 0.3, 0.36][i] })) };
}
// the gauge's round face, 7 across inside a copper bezel: with no colours, the base's dark glass
function gaugeFace(p, c) {
  const cx = 12, cy = 10;
  for (let y = cy - 5; y <= cy + 5; y++) for (let x = cx - 5; x <= cx + 5; x++) {
    const d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
    if (!c) {
      if (d2 <= 13) p.px(x, y, '#10161d');
      else if (d2 <= 26) p.px(x, y, (x - cx) + (y - cy) < 0 ? R.P : (x - cx) + (y - cy) > 2 ? R.o : R.Q);
    } else if (d2 <= 13) p.px(x, y, d2 >= 10 ? c.rim : (x - cx) + (y - cy) <= -3 ? c.hi : c.face);
  }
  if (!c) { p.px(cx - 2, cy - 2, '#2a3d4f'); p.px(cx - 1, cy - 3, '#1d2a38'); }
  else { p.px(cx - 2, cy - 2, '#ffffff'); }
}

const LOOKS = { A: lookA, B: lookB, C: lookC };
module.exports = { LOOKS, O2, RED };

// Rooms cut in rock: front cells (rock, alloy, soil), back walls (plaster, alloy, carved rock), painted pixel by pixel
// into the cave-content engine's Pic so its light pass (occlusion, bloom, beam, glints) does the rest.
const E = require('../../planet-art/caves/engine');
const { C, hex, mix, mul, hash, vnoise, baseColor, MP, loadArt } = E;

const WALL = loadArt('planet/wall_alloy.png'), BACK = loadArt('planet/back_alloy.png');
const samp = (img, x, y) => { const o = ((((y % img.h) + img.h) % img.h) * img.w + (((x % img.w) + img.w) % img.w)) * 4; return [img.data[o] / 255, img.data[o + 1] / 255, img.data[o + 2] / 255]; };
const AOL = hex('#141d24'), ASH = hex('#a6c2c6');

class Room {
  constructor(W, H) {
    Object.assign(this, { W, H, PW: W * C, PH: H * C });
    this.F = new Array(W * H).fill('rock'); this.B = new Array(W * H).fill('rock');
    this.noShape = new Set(['alloy']);
  }
  i(x, y) { return y * this.W + x; }
  inb(x, y) { return x >= 0 && y >= 0 && x < this.W && y < this.H; }
  solidCell(x, y) { return !this.inb(x, y) ? true : !!this.F[this.i(x, y)]; }
  // open a rectangle of cells with a back wall material (or a function of the cell)
  carve(x0, y0, w, h, back) { for (let y = y0; y < y0 + h; y++) for (let x = x0; x < x0 + w; x++) if (this.inb(x, y)) { this.F[this.i(x, y)] = null; this.B[this.i(x, y)] = typeof back === 'function' ? back(x, y) : back; } }
  fill(x0, y0, w, h, m) { for (let y = y0; y < y0 + h; y++) for (let x = x0; x < x0 + w; x++) if (this.inb(x, y)) this.F[this.i(x, y)] = m; }
  shapes() {
    const { W, H } = this; this.S = new Array(W * H).fill(0);
    for (let y = 1; y < H - 1; y++) for (let x = 1; x < W - 1; x++) {
      const m = this.F[this.i(x, y)]; if (!m || this.noShape.has(m)) continue;
      const U = !this.solidCell(x, y - 1), D = !this.solidCell(x, y + 1), L = !this.solidCell(x - 1, y), R = !this.solidCell(x + 1, y);
      let s = 0;
      if (U && L && !R && !D) s = 1; else if (U && R && !L && !D) s = 2; else if (D && L && !R && !U) s = 3; else if (D && R && !L && !U) s = 4;
      this.S[this.i(x, y)] = s;
    }
    const inShape = (s, x, y) => s === 1 ? x + y >= 15 : s === 2 ? x <= y : s === 3 ? x >= y : s === 4 ? x + y <= 15 : true;
    const { PW, PH } = this; this.solid = new Uint8Array(PW * PH);
    for (let y = 0; y < PH; y++) for (let x = 0; x < PW; x++) {
      const c = this.i((x / C) | 0, (y / C) | 0); if (this.F[c] && inShape(this.S[c], x % C, y % C)) this.solid[y * PW + x] = 1;
    }
  }
  sp(x, y) { return x < 0 || y < 0 || x >= this.PW || y >= this.PH ? 1 : this.solid[y * this.PW + x]; }
}

// back-wall albedo by material
function backColor(m, x, y) {
  if (m === 'alloy') return mul(samp(BACK, x, y), 0.95);
  if (m === 'plaster') {
    // smoothed stone, washed warm: wide courses, faint trowel marks, a few flecks
    let c = mix(hex('#5c4f45'), hex('#6a5a4c'), vnoise(x, y, 11, 21));
    if (vnoise(x, y, 4, 22) > 0.78) c = mul(c, 1.06);
    if ((y % 24) === 0 && hash(x >> 3, y, 23) < 0.8) c = mul(c, 0.8);
    const h = hash(x, y, 24); if (h < 0.03) c = mul(c, 0.82); else if (h > 0.985) c = mul(c, 1.15);
    return mul(c, 0.62);
  }
  if (m === 'carved') {
    // rock dressed into blocks: the back wall of a cut room
    const bx = (x + ((y >> 3) & 1 ? 8 : 0)) % 16, by = y % 8;
    let c = mul(baseColor('rock', x, y), [0.5, 0.5, 0.56]);
    if (by === 7 || bx === 15) c = mul(c, 0.6); else if (by === 0 || bx === 0) c = mul(c, 1.15);
    return c;
  }
  return mul(baseColor(m === 'deep' ? 'deep' : m === 'soil' ? 'soil' : 'rock', x, y), [0.42, 0.42, 0.5]);
}

function paint(pic) {
  const w = pic.w, { PW, PH } = w;
  for (let y = 0; y < PH; y++) for (let x = 0; x < PW; x++) {
    const i = y * PW + x, cx = (x / C) | 0, cy = (y / C) | 0;
    if (!w.solid[i]) {
      const b = w.B[w.i(cx, cy)];
      if (b === null) { pic.alb.set([0, 0, 0], i * 3); pic.unlit[i] = 1; continue; }
      let c = backColor(b, x, y);
      if (w.sp(x - 1, y - 1) || w.sp(x, y - 1) || w.sp(x - 1, y) || w.sp(x + 1, y)) c = mul(c, 0.55);
      else if (w.sp(x - 2, y - 2) || w.sp(x, y - 2) || w.sp(x - 2, y) || w.sp(x + 2, y)) c = mul(c, 0.78);
      pic.alb.set(c, i * 3); pic.back[i] = 1; continue;
    }
    const m = w.F[w.i(cx, cy)];
    const airU = !w.sp(x, y - 1), airD = !w.sp(x, y + 1), airL = !w.sp(x - 1, y), airR = !w.sp(x + 1, y);
    let c;
    if (m === 'alloy') {
      c = samp(WALL, x, y);
      if (airU || airD || airL || airR) c = AOL;
      else if (!w.sp(x, y - 2)) c = ASH;
      else if (!w.sp(x, y + 2) || !w.sp(x + 2, y)) c = mul(c, 0.75);
    } else {
      const p = MP[m] || MP.rock; c = baseColor(m, x, y);
      if (airU || airD || airL || airR) c = p.ol;
      else if (!w.sp(x, y - 2) || !w.sp(x - 2, y)) c = p.hi;
      else if (!w.sp(x, y + 2) || !w.sp(x + 2, y)) c = mul(c, 0.78);
    }
    pic.alb.set(c, i * 3);
  }
}

module.exports = { Room, paint, backColor, samp, WALL, BACK };

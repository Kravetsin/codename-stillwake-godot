// The machines' canvas, in the Former's and the smelter's manner: flat colours, a dark line round every part,
// light from the upper left. Colours are 0xrrggbb; a pixel may carry an alpha (dust).
const { encode } = require('../part-icons/png.js');

const C = {
  line: 0x1a1e25, back: 0x22262c, deep: 0x10161d,
  hi: 0x9aa0ab, steel: 0x8f99ad, mid: 0x7f848f, mid2: 0x6b7385, dark: 0x5b6170, dark2: 0x474c57,
  weld: 0xbfb49a,
};
// light, mid, shade, deep
const HULL = [0x8fa3b8, 0x6d8197, 0x516276, 0x3a4655];
const RUST = [0xd98c52, 0xa8653a, 0x7a4527, 0x53301c];
const WOOD = [0xc08c5e, 0xa0704a, 0x7c5234, 0x5c3a25];
const ROCK = [0xaaa69c, 0x837f76, 0x5f5c55, 0x423f3a];
const HAZARD = [0xf2cf5a, 0xd9a92e];
const RED = [0xec7158, 0xc9472f, 0x8e2e1f];

class Cv {
  constructor(w, h) { this.w = w; this.h = h; this.px = new Array(w * h).fill(null); this.a = new Array(w * h).fill(255); }
  in(x, y) { return x >= 0 && y >= 0 && x < this.w && y < this.h; }
  set(x, y, c, a = 255) { x = Math.round(x); y = Math.round(y); if (c == null || !this.in(x, y)) return; this.px[y * this.w + x] = c; this.a[y * this.w + x] = a; }
  clear(x, y) { if (this.in(x, y)) this.px[y * this.w + x] = null; }
  get(x, y) { return this.in(x, y) ? this.px[y * this.w + x] : null; }
  rect(x0, y0, x1, y1, c) { for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) this.set(x, y, c); }
  box(x0, y0, x1, y1, fill) { this.rect(x0, y0, x1, y1, C.line); this.rect(x0 + 1, y0 + 1, x1 - 1, y1 - 1, fill); }
  /** A box lit from the upper left in palette p (light, mid, shade, deep). */
  lit(x0, y0, x1, y1, p) {
    this.box(x0, y0, x1, y1, p[1]);
    if (y1 - y0 > 2) this.rect(x0 + 1, y0 + 1, x1 - 1, y0 + 1, p[0]);
    if (y1 - y0 > 3) this.rect(x0 + 1, y1 - 1, x1 - 1, y1 - 1, p[2]);
    if (x1 - x0 > 4 && y1 - y0 > 4) { this.rect(x1 - 1, y0 + 2, x1 - 1, y1 - 2, p[2]); }
  }
  line(x0, y0, x1, y1, c) {
    const dx = Math.abs(x1 - x0), dy = -Math.abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1; let e = dx + dy;
    for (;;) { this.set(x0, y0, c); if (x0 === x1 && y0 === y1) break; const e2 = 2 * e; if (e2 >= dy) { e += dy; x0 += sx; } if (e2 <= dx) { e += dx; y0 += sy; } }
  }
  /** Lines every empty pixel beside a drawn one: for loose things drawn on their own canvas. */
  outline(c = C.line) {
    const add = [];
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      if (this.get(x, y) != null) continue;
      if ([[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => this.get(x + dx, y + dy) != null && this.get(x + dx, y + dy) !== c)) add.push([x, y]);
    }
    for (const [x, y] of add) this.set(x, y, c);
    return this;
  }
  over(o, ox = 0, oy = 0) { for (let y = 0; y < o.h; y++) for (let x = 0; x < o.w; x++) { const i = y * o.w + x; if (o.px[i] != null) this.set(x + ox, y + oy, o.px[i], o.a[i]); } return this; }
  png() {
    const rgba = Buffer.alloc(this.w * this.h * 4);
    this.px.forEach((c, i) => { if (c == null) return; rgba[i * 4] = c >> 16; rgba[i * 4 + 1] = (c >> 8) & 255; rgba[i * 4 + 2] = c & 255; rgba[i * 4 + 3] = this.a[i]; });
    return encode(this.w, this.h, rgba);
  }
}

module.exports = { Cv, C, HULL, RUST, WOOD, ROCK, HAZARD, RED };

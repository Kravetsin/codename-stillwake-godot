// A picture is a grid of hex colours (null for clear). Shapes are placed by hand: ASCII stamps, rects and
// lines, each pixel its tone; light from the upper left, as on all of the game's art. The outline is added last.
const fs = require('fs');
const { encode, read } = require('./png.js');

const hex = c => [parseInt(c.slice(1, 3), 16), parseInt(c.slice(3, 5), 16), parseInt(c.slice(5, 7), 16)];
const toHex = ([r, g, b]) => '#' + [r, g, b].map(v => Math.max(0, Math.min(255, Math.round(v))).toString(16).padStart(2, '0')).join('');

class Pic {
  constructor(w, h) { this.w = w; this.h = h; this.c = new Array(w * h).fill(null); this.tag = new Array(w * h).fill(null); }
  in(x, y) { return x >= 0 && y >= 0 && x < this.w && y < this.h; }
  get(x, y) { return this.in(x, y) ? this.c[y * this.w + x] : null; }
  tagAt(x, y) { return this.in(x, y) ? this.tag[y * this.w + x] : null; }
  px(x, y, col, tag) { if (!this.in(x, y)) return; const i = y * this.w + x; this.c[i] = col; if (tag !== undefined) this.tag[i] = tag; }
  clear(x, y) { if (this.in(x, y)) { this.c[y * this.w + x] = null; this.tag[y * this.w + x] = null; } }
  rect(x0, y0, x1, y1, col, tag) { for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) this.px(x, y, col, tag); }
  hl(x0, x1, y, col, tag) { this.rect(x0, y, x1, y, col, tag); }
  vl(x, y0, y1, col, tag) { this.rect(x, y0, x, y1, col, tag); }
  // ASCII rows: each char looked up in map; ' ' and '.' leave the pixel as it is
  stamp(x0, y0, rows, map, tag, { flip = false } = {}) {
    rows.forEach((r, j) => [...r].forEach((ch, i) => {
      if (ch === ' ' || ch === '.') return;
      const col = map[ch]; if (col === undefined) throw new Error(`no colour for '${ch}'`);
      const x = flip ? x0 + r.length - 1 - i : x0 + i;
      if (col === null) this.clear(x, y0 + j); else this.px(x, y0 + j, col, tag);
    }));
  }
  line(x0, y0, x1, y1, col, tag) {
    const dx = Math.abs(x1 - x0), dy = -Math.abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1; let e = dx + dy;
    for (;;) { this.px(x0, y0, col, tag); if (x0 === x1 && y0 === y1) break; const e2 = 2 * e; if (e2 >= dy) { e += dy; x0 += sx; } if (e2 <= dx) { e += dx; y0 += sy; } }
  }
  // a flat plate: base colour, lit top and left, dark bottom and right
  panel(x0, y0, x1, y1, [dark, base, lit], tag) {
    this.rect(x0, y0, x1, y1, base, tag); this.hl(x0, x1, y0, lit, tag); this.vl(x0, y0, y1, lit, tag);
    this.hl(x0 + 1, x1, y1, dark, tag); this.vl(x1, y0 + 1, y1, dark, tag);
  }
  each(fn) { for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) { const c = this.c[y * this.w + x]; if (c !== null) fn(x, y, c, this.tag[y * this.w + x]); } }
  copy() { const p = new Pic(this.w, this.h); p.c = this.c.slice(); p.tag = this.tag.slice(); return p; }
  // a one-pixel outline round everything drawn, four-connected, as the game's items and machines have it
  outline(col, { diag = false } = {}) {
    const add = [];
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      if (this.get(x, y) !== null) continue;
      const ns = diag ? [[1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [-1, 1], [1, -1], [-1, -1]] : [[1, 0], [-1, 0], [0, 1], [0, -1]];
      if (ns.some(([dx, dy]) => this.get(x + dx, y + dy) !== null)) add.push([x, y]);
    }
    for (const [x, y] of add) this.px(x, y, col, 'outline');
    return this;
  }
  // over another picture: this one's pixels win
  over(other) { const p = this.copy(); other.each((x, y, c, t) => p.px(x, y, c, t)); return p; }
  rgba() { const d = new Uint8Array(this.w * this.h * 4); this.each((x, y, c) => d.set([...hex(c), 255], (y * this.w + x) * 4)); return { w: this.w, h: this.h, data: d }; }
  save(f) { fs.writeFileSync(f, encode(this.w, this.h, this.rgba().data)); }
  static load(f) { const im = read(f); const p = new Pic(im.w, im.h); for (let i = 0; i < im.w * im.h; i++) if (im.data[i * 4 + 3] > 127) p.c[i] = toHex([im.data[i * 4], im.data[i * 4 + 1], im.data[i * 4 + 2]]); return p; }
}

// ---- RGBA images for previews and scenes ----
const img = (w, h, bg) => { const d = new Uint8Array(w * h * 4); if (bg) { const c = hex(bg); for (let i = 0; i < w * h; i++) d.set([...c, 255], i * 4); } return { w, h, data: d }; };
function blit(dst, src, ox, oy, { scale = 1, alpha = 1 } = {}) {
  const s = src.rgba ? src.rgba() : src;
  for (let y = 0; y < s.h * scale; y++) for (let x = 0; x < s.w * scale; x++) {
    const X = ox + x, Y = oy + y; if (X < 0 || Y < 0 || X >= dst.w || Y >= dst.h) continue;
    const si = (((y / scale) | 0) * s.w + ((x / scale) | 0)) * 4, a = (s.data[si + 3] / 255) * alpha; if (!a) continue;
    const di = (Y * dst.w + X) * 4; for (let k = 0; k < 3; k++) dst.data[di + k] = Math.round(s.data[si + k] * a + dst.data[di + k] * (1 - a)); dst.data[di + 3] = 255;
  }
  return dst;
}
// a soft additive light, as the game's Glow casts it, for previews only
function glow(dst, cx, cy, r, col, k) {
  const c = hex(col);
  for (let y = Math.max(0, Math.floor(cy - r)); y < Math.min(dst.h, cy + r); y++) for (let x = Math.max(0, Math.floor(cx - r)); x < Math.min(dst.w, cx + r); x++) {
    const d = Math.sqrt((x + 0.5 - cx) ** 2 + (y + 0.5 - cy) ** 2) / r; if (d >= 1) continue;
    const f = k * (1 - d) * (1 - d), i = (y * dst.w + x) * 4; if (dst.data[i + 3] === 0) continue;
    for (let q = 0; q < 3; q++) dst.data[i + q] = Math.min(255, dst.data[i + q] + c[q] * f * (0.35 + dst.data[i + q] / 255));
  }
  return dst;
}
// darken a picture as an unlit room does, for the "lamp on" preview's contrast
function shade(src, k) { const s = src.rgba ? src.rgba() : src; const d = { w: s.w, h: s.h, data: s.data.slice() }; for (let i = 0; i < d.w * d.h; i++) for (let q = 0; q < 3; q++) d.data[i * 4 + q] *= k; return d; }
const savePng = (f, im) => fs.writeFileSync(f, encode(im.w, im.h, im.data));
const loadPng = f => read(f);

module.exports = { Pic, img, blit, glow, shade, savePng, loadPng, hex, toHex };

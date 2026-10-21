// A 16x16 icon canvas: objects drawn back to front, each shaded lit from the upper left; the outline
// wraps every object and separates an object from the ones drawn over it.
const OUT = '#14171e';
const P = {
  steel:  ['#e4e9ef', '#aab4c0', '#7a8593', '#535c69'],
  iron:   ['#c9d2de', '#97a3b4', '#6b7788', '#4b5462'],
  dark:   ['#8a93a0', '#646d7a', '#474e59', '#30353e'],
  rust:   ['#d98c52', '#a8653a', '#7a4527', '#53301c'],
  rust2:  ['#b47d58', '#8a573c', '#61402c', '#432c1f'],
  copper: ['#f7bd84', '#d9864a', '#a65a2a', '#6f3818'],
  wood:   ['#e2b178', '#bd8449', '#8f5b2d', '#62391a'],
  pine:   ['#efcf98', '#d3a465', '#a6773f', '#724c24'],
  paint:  ['#a3bbd1', '#7893ad', '#58718b', '#3d5268'],
  green:  ['#9fb58a', '#768e62', '#566b46', '#3c4b31'],
  brass:  ['#f1d27a', '#c9a345', '#957626', '#5f4a16'],
  yellow: ['#f2cf5a', '#d9a92e', '#a87b1c', '#735210'],
  red:    ['#ec7158', '#c9472f', '#8e2e1f', '#5e1d14'],
  grime:  ['#a29d8e', '#7c776a', '#5b574d', '#403d36'],
  rope:   ['#e8d4a2', '#c4ab74', '#957f4e', '#665632'],
};
const HI = '#f4f7fa';

class Icon {
  constructor(w = 16, h = 16) { this.w = w; this.h = h; this.mat = Array(w * h).fill(null); this.col = Array(w * h).fill(null); this.ob = Array(w * h).fill(0); this.cur = 0; }
  in(x, y) { return x >= 0 && y >= 0 && x < this.w && y < this.h; }
  i(x, y) { return y * this.w + x; }
  obj() { this.cur++; return this; }
  put(x, y, m, c = null) { x = Math.floor(x); y = Math.floor(y); if (!this.in(x, y)) return; const k = this.i(x, y); this.mat[k] = m; this.col[k] = c; this.ob[k] = this.cur; }
  erase(x, y) { if (!this.in(x, y)) return; const k = this.i(x, y); this.mat[k] = null; this.col[k] = null; this.ob[k] = 0; }
  has(x, y) { return this.in(x, y) && this.mat[this.i(x, y)] != null; }
  mine(x, y) { return this.has(x, y) && this.ob[this.i(x, y)] === this.cur; }
  // paints a drawn pixel of the current object (any object with any=true)
  paint(x, y, c, any = false) { x = Math.floor(x); y = Math.floor(y); if (any ? this.has(x, y) : this.mine(x, y)) this.col[this.i(x, y)] = c; }
  tone(x, y, m, t, any = false) { this.paint(x, y, P[m][t], any); }
  rect(x0, y0, x1, y1, m) { for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) this.put(x, y, m); }
  poly(pts, m) {
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      let inside = false; const px = x + 0.5, py = y + 0.5;
      for (let a = 0, b = pts.length - 1; a < pts.length; b = a++) {
        const [xi, yi] = pts[a], [xj, yj] = pts[b];
        if ((yi > py) !== (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi) inside = !inside;
      }
      if (inside) this.put(x, y, m);
    }
  }
  disc(cx, cy, r, m) { this.ellipse(cx, cy, r, r, m); }
  ellipse(cx, cy, rx, ry, m) { for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) { const dx = (x + .5 - cx) / rx, dy = (y + .5 - cy) / ry; if (dx * dx + dy * dy <= 1) this.put(x, y, m); } }
  ring(cx, cy, r0, r1, m) { for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) { const d = Math.hypot(x + .5 - cx, y + .5 - cy); if (d >= r0 && d <= r1) this.put(x, y, m); } }
  hole(cx, cy, r) { for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) if (Math.hypot(x + .5 - cx, y + .5 - cy) <= r) this.erase(x, y); }
  line(x0, y0, x1, y1, t, m) { const n = Math.ceil(Math.hypot(x1 - x0, y1 - y0) * 3) + 1; for (let k = 0; k <= n; k++) { const x = x0 + (x1 - x0) * k / n, y = y0 + (y1 - y0) * k / n; if (t <= 1) this.put(x, y, m); else this.disc(x, y, t / 2, m); } }
  // pixel-exact segment between pixel coordinates
  seg(x0, y0, x1, y1, m, c = null) { const n = Math.max(Math.abs(x1 - x0), Math.abs(y1 - y0)); for (let k = 0; k <= n; k++) this.put(Math.round(x0 + (x1 - x0) * k / (n || 1)), Math.round(y0 + (y1 - y0) * k / (n || 1)), m, c); }
  pseg(x0, y0, x1, y1, c, any = false) { const n = Math.max(Math.abs(x1 - x0), Math.abs(y1 - y0)); for (let k = 0; k <= n; k++) this.paint(Math.round(x0 + (x1 - x0) * k / (n || 1)), Math.round(y0 + (y1 - y0) * k / (n || 1)), c, any); }
  // rows of characters, a map from character to colour; '.' leaves the pixel alone
  grid(rows, map, x0 = 0, y0 = 0) {
    rows.forEach((row, y) => [...row].forEach((ch, x) => {
      if (ch === '.' || ch === ' ') return;
      if (!(ch in map)) throw new Error('no colour for ' + ch);
      this.put(x0 + x, y0 + y, 'grid', map[ch]);
    }));
  }
  // flat shading of the current object: light on edges facing up or left, dark facing down or right
  shade(m) {
    const p = P[m];
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      if (!this.mine(x, y) || (m && this.mat[this.i(x, y)] !== m)) continue;
      const pp = P[this.mat[this.i(x, y)]] || p; if (!pp) continue;
      const upL = !this.mine(x, y - 1) || !this.mine(x - 1, y), dnR = !this.mine(x, y + 1) || !this.mine(x + 1, y);
      let c = pp[1];
      if (upL && !dnR) c = pp[0]; else if (dnR && !upL) c = pp[2];
      if (!this.mine(x, y + 1) && !this.mine(x + 1, y) && !upL) c = pp[3];
      this.col[this.i(x, y)] = c;
    }
  }
  // round shading about a centre: a gradient from the upper left
  shadeRound(cx, cy, r, m, cut = [-0.45, 0.2, 0.62]) {
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      if (!this.mine(x, y)) continue; const mm = m || this.mat[this.i(x, y)], p = P[mm]; if (!p) continue;
      const v = ((x + .5 - cx) + (y + .5 - cy)) / (r * 1.414);
      this.col[this.i(x, y)] = v < cut[0] ? p[0] : v < cut[1] ? p[1] : v < cut[2] ? p[2] : p[3];
    }
  }
  rgba() {
    const buf = Buffer.alloc(this.w * this.h * 4), hex = c => [1, 3, 5].map(k => parseInt(c.slice(k, k + 2), 16));
    const nb = [[1, 0], [-1, 0], [0, 1], [0, -1]];
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      const k = this.i(x, y); let c = null;
      if (this.mat[k] != null) {
        c = this.col[k] || (P[this.mat[k]] ? P[this.mat[k]][1] : '#ff00ff');
        if (nb.some(([a, b]) => this.has(x + a, y + b) && this.ob[this.i(x + a, y + b)] > this.ob[k])) c = OUT;
      } else if (nb.some(([a, b]) => this.has(x + a, y + b))) c = OUT;
      if (c) { const [r, g, b] = hex(c); buf.set([r, g, b, this.alpha ? this.alpha(c) : 255], k * 4); }
    }
    return buf;
  }
}
module.exports = { Icon, P, OUT, HI };

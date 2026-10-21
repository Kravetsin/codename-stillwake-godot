// Debris sprites for Stillwake, drawn by code: node tools/ship-art/debris-v1/gen.js [outDir, default game/art/debris]
// Every piece is a mask of materials, shaded lit from the upper left, detailed, then outlined like all the game's art.
const fs = require('fs'), zlib = require('zlib'), path = require('path');
const out = process.argv[2] || path.join(path.join(__dirname, '..', '..', '..', 'game', 'art'), 'debris'); fs.mkdirSync(out, { recursive: true });

// ---------- PNG ----------
const CRC = new Int32Array(256).map((_, n) => { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c; });
const crc = b => { let c = -1; for (const x of b) c = CRC[(c ^ x) & 255] ^ (c >>> 8); return (c ^ -1) >>> 0; };
function chunk(t, d) { const l = Buffer.alloc(4); l.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t), d]); const c = Buffer.alloc(4); c.writeUInt32BE(crc(td)); return Buffer.concat([l, td, c]); }
function png(w, h, rgba) {
  const raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) { raw[y * (w * 4 + 1)] = 0; rgba.copy(raw, y * (w * 4 + 1) + 1, y * w * 4, (y + 1) * w * 4); }
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6;
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]);
}
const hex = c => [parseInt(c.slice(1, 3), 16), parseInt(c.slice(3, 5), 16), parseInt(c.slice(5, 7), 16)];

// ---------- rng ----------
function rng(seed) { let s = seed >>> 0 || 1; return () => { s ^= s << 13; s >>>= 0; s ^= s >> 17; s ^= s << 5; s >>>= 0; return s / 4294967296; }; }

// ---------- palettes: light, base, dark, deep ----------
const OUT = '#14171e';
const M = {
  rust:  ['#c08552', '#94603c', '#6a4128', '#4a2c1c'],
  rust2: ['#a8735a', '#7f5644', '#5c3d31', '#3f2a22'],
  grime: ['#9a9a90', '#77776e', '#585850', '#3e3e38'],
  steel: ['#c3ccd6', '#939fad', '#677280', '#4a525e'],
  dark:  ['#7a8390', '#59616d', '#40464f', '#2c3138'],
  paint: ['#7f9bb5', '#5c7a96', '#435b72', '#2f4053'],
  orange:['#e39545', '#c26e2a', '#8f4c1c', '#633212'],
  wood:  ['#d09a5e', '#a8703f', '#7a4e2a', '#55341b'],
  wood2: ['#c08a52', '#966136', '#6c4424', '#4a2c16'],
  glass: ['#cdeefa', '#79bde0', '#4b8fb8', '#2f6a8f'],
  yellow:['#f2cf5a', '#d9a92e', '#a87b1c', '#735210'],
  copper:['#e8a46a', '#c47a42', '#94552a', '#66391a'],
};

class Art {
  constructor(w, h, seed) { this.w = w; this.h = h; this.m = new Array(w * h).fill(null); this.c = new Array(w * h).fill(null); this.r = rng(seed); }
  in(x, y) { return x >= 0 && y >= 0 && x < this.w && y < this.h; }
  put(x, y, mat) { x = Math.round(x); y = Math.round(y); if (this.in(x, y)) this.m[y * this.w + x] = mat; }
  get(x, y) { return this.in(x, y) ? this.m[y * this.w + x] : null; }
  erase(x, y) { x = Math.round(x); y = Math.round(y); if (this.in(x, y)) { this.m[y * this.w + x] = null; this.c[y * this.w + x] = null; } }
  rect(x0, y0, x1, y1, mat) { for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) this.put(x, y, mat); }
  poly(pts, mat) {
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      let inside = false; const px = x + 0.5, py = y + 0.5;
      for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
        const [xi, yi] = pts[i], [xj, yj] = pts[j];
        if ((yi > py) !== (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi) inside = !inside;
      }
      if (inside) this.put(x, y, mat);
    }
  }
  disc(cx, cy, r, mat) { for (let y = Math.floor(cy - r); y <= cy + r; y++) for (let x = Math.floor(cx - r); x <= cx + r; x++) if ((x + .5 - cx) ** 2 + (y + .5 - cy) ** 2 <= r * r) this.put(x, y, mat); }
  ring(cx, cy, r0, r1, mat, a0 = 0, a1 = Math.PI * 2) {
    for (let y = Math.floor(cy - r1); y <= cy + r1; y++) for (let x = Math.floor(cx - r1); x <= cx + r1; x++) {
      const dx = x + .5 - cx, dy = y + .5 - cy, d = Math.hypot(dx, dy); let a = Math.atan2(dy, dx); if (a < 0) a += Math.PI * 2;
      const inArc = a0 <= a1 ? a >= a0 && a <= a1 : a >= a0 || a <= a1;
      if (d >= r0 && d <= r1 && inArc) this.put(x, y, mat);
    }
  }
  line(x0, y0, x1, y1, t, mat) { const n = Math.ceil(Math.hypot(x1 - x0, y1 - y0) * 2) + 1; for (let i = 0; i <= n; i++) { const x = x0 + (x1 - x0) * i / n, y = y0 + (y1 - y0) * i / n; this.disc(x, y, t / 2, mat); } }
  // tears bites out of the edge: a ragged, torn look
  tear(k) { const edge = []; for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) if (this.get(x, y) && [[1,0],[-1,0],[0,1],[0,-1]].some(([a,b]) => !this.get(x+a, y+b))) edge.push([x, y]); for (const [x, y] of edge) if (this.r() < k) this.erase(x, y); }
  // shading: base, light on edges facing up/left, dark facing down/right, speckles
  shade(speck = 0.12) {
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      const mat = this.get(x, y); if (!mat) continue; const p = M[mat];
      const upL = !this.get(x, y - 1) || !this.get(x - 1, y), dnR = !this.get(x, y + 1) || !this.get(x + 1, y);
      let c = p[1];
      if (upL && !dnR) c = p[0]; else if (dnR && !upL) c = p[2];
      else if (this.r() < speck) c = this.r() < .5 ? p[2] : p[0];
      if (dnR && !this.get(x + 1, y + 1) && !upL && this.r() < .4) c = p[3];
      this.c[y * this.w + x] = c;
    }
  }
  paint(x, y, col) { x = Math.round(x); y = Math.round(y); if (this.in(x, y) && this.get(x, y)) this.c[y * this.w + x] = col; }
  rivet(x, y, mat = 'steel') { this.paint(x, y, M[mat][0]); this.paint(x + 1, y + 1, M[mat][3]); }
  seamH(x0, x1, y, mat) { for (let x = x0; x <= x1; x++) { this.paint(x, y, M[mat][3]); this.paint(x, y + 1, M[mat][0]); } }
  seamV(x, y0, y1, mat) { for (let y = y0; y <= y1; y++) { this.paint(x, y, M[mat][3]); this.paint(x + 1, y, M[mat][0]); } }
  spots(n, mat, k = 2) { for (let i = 0; i < n; i++) { const x = this.r() * this.w, y = this.r() * this.h; for (let j = 0; j < k; j++) this.paint(x + Math.round(this.r() * 2 - 1), y + Math.round(this.r() * 2 - 1), M[mat][this.r() < .5 ? 1 : 2]); } }
  hole(cx, cy, r) { for (let y = Math.floor(cy - r - 1); y <= cy + r + 1; y++) for (let x = Math.floor(cx - r - 1); x <= cx + r + 1; x++) { const d = Math.hypot(x + .5 - cx, y + .5 - cy); if (d <= r) this.erase(x, y); else if (d <= r + 1 && this.get(x, y)) this.paint(x, y, (x + y) < cx + cy ? M.dark[3] : M.steel[0]); } }
  wire(pts, col) { for (let i = 0; i < pts.length - 1; i++) { const [x0, y0] = pts[i], [x1, y1] = pts[i + 1]; const n = Math.ceil(Math.hypot(x1 - x0, y1 - y0)) + 1; for (let k = 0; k <= n; k++) { const x = Math.round(x0 + (x1 - x0) * k / n), y = Math.round(y0 + (y1 - y0) * k / n); if (this.in(x, y)) { this.m[y * this.w + x] = this.m[y * this.w + x] || 'wire'; this.c[y * this.w + x] = col; } } } }
  rgba() {
    const has = (x, y) => this.in(x, y) && this.m[y * this.w + x];
    const buf = Buffer.alloc(this.w * this.h * 4);
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      const i = (y * this.w + x) * 4; let col = null;
      if (has(x, y)) col = this.c[y * this.w + x] || M[this.m[y * this.w + x]]?.[1] || '#ff00ff';
      else if (has(x - 1, y) || has(x + 1, y) || has(x, y - 1) || has(x, y + 1)) col = OUT;
      if (col) { const [r, g, b] = hex(col); buf[i] = r; buf[i + 1] = g; buf[i + 2] = b; buf[i + 3] = 255; }
    }
    return buf;
  }
}
// draws on a canvas one bigger each side, so the outline fits
function make(w, h, seed, draw) { const a = new Art(w + 2, h + 2, seed); const o = { a, ox: 1, oy: 1 }; draw(a, 1, 1); return a; }

const pieces = { scrap: [], hull: [], crate: [], pod: [] };
const add = (kind, name, w, h, seed, draw) => pieces[kind].push({ name, art: make(w, h, seed, draw) });

// ===================== SCRAP: rusty, bent, mixed =====================
add('scrap', 'rusty lump', 14, 12, 11, (a, o) => {
  a.disc(o + 5, o + 6, 4.6, 'rust'); a.disc(o + 9, o + 5, 3.8, 'rust2'); a.disc(o + 8, o + 9, 3.2, 'rust'); a.tear(.25); a.shade(.25); a.spots(4, 'grime');
});
add('scrap', 'bent rebar', 18, 10, 12, (a, o) => {
  a.line(o + 1, o + 8, o + 7, o + 5, 2.2, 'rust'); a.line(o + 7, o + 5, o + 11, o + 6, 2.2, 'rust'); a.line(o + 11, o + 6, o + 16, o + 1, 2.2, 'rust'); a.shade(.2);
  for (let x = 2; x < 17; x += 3) a.paint(o + x, o + (x < 8 ? 8 - x * .45 : x < 12 ? 5.6 : 6 - (x - 11) * 1), M.rust[3]);
});
add('scrap', 'gear shard', 16, 16, 13, (a, o) => {
  a.ring(o + 8, o + 8, 2.6, 5.6, 'steel', 0.2, 4.4);
  for (let k = 0; k < 8; k++) { const t = 0.35 + k * 0.52; if (t > 4.3) break; const cx = o + 8 + Math.cos(t) * 6.4, cy = o + 8 + Math.sin(t) * 6.4; a.rect(Math.round(cx - .5), Math.round(cy - .5), Math.round(cx + .5), Math.round(cy + .5), 'steel'); }
  a.shade(.03); a.spots(3, 'rust', 2);
});
add('scrap', 'bent pipe', 18, 14, 14, (a, o) => {
  a.ring(o + 4, o + 13, 6, 9.2, 'copper', 4.7, 6.2); a.line(o + 13, o + 12, o + 17, o + 12, 3.2, 'copper'); a.shade(.08);
  a.spots(3, 'rust', 3); a.rect(o + 3, o + 3, o + 4, o + 5, 'dark'); a.shade(0);
});
add('scrap', 'torn shard', 15, 13, 15, (a, o) => {
  a.poly([[o + 1, o + 3], [o + 9, o + 0], [o + 15, o + 4], [o + 12, o + 9], [o + 14, o + 13], [o + 5, o + 11], [o + 0, o + 8]], 'rust2'); a.tear(.2); a.shade(.2); a.spots(6, 'rust', 3);
});
add('scrap', 'bolt and nut', 16, 9, 16, (a, o) => {
  a.rect(o, o + 1, o + 3, o + 7, 'steel'); a.rect(o + 4, o + 3, o + 12, o + 5, 'grime');
  a.rect(o + 11, o + 1, o + 15, o + 7, 'dark'); a.shade(.02);
  for (let x = 5; x < 11; x += 2) a.paint(o + x, o + 4, M.grime[3]); a.hole(o + 13, o + 4, .8); a.spots(2, 'rust', 2);
});
add('scrap', 'coil spring', 18, 9, 17, (a, o) => {
  for (let k = 0; k < 6; k++) a.ring(o + 2.5 + k * 2.7, o + 4.5, 2.4, 4, k % 2 ? 'steel' : 'grime');
  a.shade(.05);
});
add('scrap', 'half wheel', 16, 10, 18, (a, o) => {
  a.ring(o + 8, o + 9, 0, 7.6, 'dark', Math.PI, Math.PI * 2); a.ring(o + 8, o + 9, 0, 4.2, 'grime', Math.PI, Math.PI * 2); a.shade(.08);
  for (let k = 0; k < 5; k++) { const t = Math.PI + .3 + k * .65; a.paint(o + 8 + Math.cos(t) * 6.2, o + 9 + Math.sin(t) * 6.2, M.dark[3]); }
  a.disc(o + 8, o + 8.5, 1.4, 'steel'); a.shade(0);
});
add('scrap', 'crushed can', 12, 13, 19, (a, o) => {
  a.poly([[o + 1, o + 0], [o + 11, o + 0], [o + 11, o + 4], [o + 8, o + 6], [o + 11, o + 9], [o + 11, o + 13], [o + 1, o + 13], [o + 1, o + 9], [o + 3, o + 6], [o + 1, o + 3]], 'orange'); a.shade(.06);
  a.seamH(o + 1, o + 10, o + 1, 'steel'); a.seamH(o + 1, o + 10, o + 11, 'steel'); a.rect(o + 4, o + 3, o + 7, o + 4, 'steel'); a.shade(0); a.seamH(o + 1, o + 10, o + 1, 'steel'); a.seamH(o + 1, o + 10, o + 11, 'steel');
  for (let y = 5; y < 9; y++) a.paint(o + 6, o + y, M.orange[3]); a.spots(2, 'rust', 2);
});
add('scrap', 'chain', 20, 8, 20, (a, o) => {
  for (let k = 0; k < 4; k++) { a.ring(o + 3 + k * 4.6, o + 4, 1.2, 3, k % 2 ? 'grime' : 'rust2'); }
  a.shade(.05);
});
add('scrap', 'bracket', 14, 14, 21, (a, o) => {
  a.rect(o + 1, o + 1, o + 4, o + 13, 'grime'); a.rect(o + 1, o + 10, o + 13, o + 13, 'grime'); a.shade(.15);
  a.hole(o + 2.8, o + 4, 1); a.hole(o + 10, o + 11.8, 1); a.spots(4, 'rust', 3);
});
add('scrap', 'grate scrap', 16, 12, 22, (a, o) => {
  a.poly([[o + 1, o + 1], [o + 15, o + 2], [o + 14, o + 11], [o + 2, o + 12]], 'dark'); a.tear(.3); a.shade(.05);
  for (let y = 3; y < 12; y += 3) for (let x = 3; x < 15; x += 3) { a.erase(o + x, o + y); a.erase(o + x + 1, o + y); }
  a.spots(4, 'rust', 2);
});

// ===================== HULL: steel and painted plates =====================
const rivets = (a, x0, y0, x1, y1, step = 3) => { for (let x = x0; x <= x1; x += step) { a.rivet(x, y0); a.rivet(x, y1); } for (let y = y0 + step; y < y1; y += step) { a.rivet(x0, y); a.rivet(x1, y); } };
add('hull', 'riveted panel', 16, 16, 31, (a, o) => {
  a.rect(o, o, o + 15, o + 15, 'steel'); a.erase(o + 15, o); a.erase(o + 14, o); a.erase(o + 15, o + 1); a.shade(.05); rivets(a, o + 1, o + 1, o + 13, o + 13, 4);
});
add('hull', 'torn plate', 18, 14, 32, (a, o) => {
  a.poly([[o, o], [o + 13, o], [o + 16, o + 3], [o + 13, o + 6], [o + 18, o + 9], [o + 12, o + 14], [o, o + 14]], 'steel'); a.tear(.25); a.shade(.06);
  a.seamV(o + 6, o + 1, o + 13, 'steel'); for (let y = 2; y < 13; y += 3) a.rivet(o + 4, o + y); a.spots(3, 'grime', 2);
});
add('hull', 'long strip', 24, 7, 33, (a, o) => {
  a.rect(o, o + 1, o + 22, o + 6, 'paint'); a.poly([[o + 21, o + 1], [o + 24, o], [o + 23, o + 7], [o + 21, o + 6]], 'paint'); a.shade(.05);
  for (let x = 2; x < 22; x += 4) a.rivet(o + x, o + 3); a.seamH(o + 1, o + 20, o + 5, 'paint');
});
add('hull', 'porthole', 16, 16, 34, (a, o) => {
  a.rect(o, o, o + 15, o + 15, 'paint'); a.tear(.15); a.shade(.05); a.ring(o + 8, o + 8, 4.2, 6, 'steel'); a.disc(o + 8, o + 8, 4.2, 'glass'); a.shade(.0);
  a.paint(o + 6, o + 6, M.glass[0]); a.paint(o + 7, o + 6, M.glass[0]); a.paint(o + 6, o + 7, M.glass[0]);
  for (const [x, y] of [[9, 7], [10, 8], [9, 9], [10, 10], [8, 9]]) a.paint(o + x, o + y, M.glass[0]);
});
add('hull', 'i-beam', 22, 9, 35, (a, o) => {
  a.rect(o, o, o + 21, o + 1, 'dark'); a.rect(o, o + 7, o + 21, o + 8, 'dark'); a.rect(o, o + 3, o + 21, o + 5, 'dark'); a.rect(o + 2, o + 2, o + 19, o + 6, 'dark');
  a.erase(o + 21, o); a.erase(o + 20, o); a.erase(o + 21, o + 8); a.shade(.05);
  for (let x = 4; x < 20; x += 5) a.hole(o + x, o + 4, .9);
});
add('hull', 'corner piece', 15, 15, 36, (a, o) => {
  a.rect(o, o, o + 14, o + 5, 'steel'); a.rect(o, o, o + 5, o + 14, 'steel'); a.tear(.12); a.shade(.05);
  a.seamH(o + 1, o + 13, o + 2, 'steel'); a.seamV(o + 2, o + 3, o + 13, 'steel'); a.rivet(o + 8, o + 4); a.rivet(o + 12, o + 4); a.rivet(o + 4, o + 9); a.rivet(o + 4, o + 12);
});
add('hull', 'hazard plate', 18, 11, 37, (a, o) => {
  a.poly([[o, o + 1], [o + 17, o], [o + 18, o + 11], [o + 1, o + 10]], 'yellow'); a.tear(.18); a.shade(.04);
  for (let y = 0; y < 13; y++) for (let x = 0; x < 20; x++) if (((x + y) >> 2) % 2 === 0) a.paint(o + x, o + y, (x + y) % 4 === 0 ? '#2c3138' : '#23272e');
});
add('hull', 'curved hull', 20, 12, 38, (a, o) => {
  a.ring(o + 10, o + 22, 15, 21, 'paint', 3.6, 5.85); a.tear(.1); a.shade(.04);
  for (let k = 0; k < 6; k++) { const t = 3.75 + k * .38; a.rivet(o + 10 + Math.cos(t) * 18, o + 22 + Math.sin(t) * 18); }
});
add('hull', 'holed plate', 16, 15, 39, (a, o) => {
  a.rect(o, o, o + 15, o + 14, 'steel'); a.shade(.05); rivets(a, o + 1, o + 1, o + 13, o + 12, 6); a.hole(o + 9, o + 7, 2.6);
  for (const [x, y] of [[12, 4], [12, 10], [6, 9]]) a.paint(o + x, o + y, M.dark[3]);
});
add('hull', 'vent grille', 16, 12, 40, (a, o) => {
  a.rect(o, o, o + 15, o + 11, 'dark'); a.tear(.08); a.shade(.04); for (let y = 2; y < 10; y += 2) a.seamH(o + 2, o + 13, o + y, 'dark'); a.rivet(o + 1, o + 1); a.rivet(o + 13, o + 9);
});
add('hull', 'panel with wires', 16, 18, 41, (a, o) => {
  a.rect(o, o, o + 15, o + 11, 'paint'); a.tear(.2); a.shade(.05); rivets(a, o + 1, o + 1, o + 13, o + 9, 6);
  a.wire([[o + 4, o + 11], [o + 3, o + 14], [o + 5, o + 17]], '#d0463a'); a.wire([[o + 8, o + 11], [o + 9, o + 15], [o + 8, o + 17]], '#e0b030'); a.wire([[o + 11, o + 11], [o + 13, o + 13], [o + 12, o + 16]], '#3a7bd0');
});
add('hull', 'overlapping plates', 20, 14, 42, (a, o) => {
  a.rect(o, o + 3, o + 12, o + 13, 'steel'); a.rect(o + 7, o, o + 19, o + 10, 'paint'); a.tear(.1); a.shade(.05);
  for (let x = 9; x < 19; x += 3) a.rivet(o + x, o + 1); for (let x = 1; x < 7; x += 3) a.rivet(o + x, o + 12);
});

// ===================== CRATES: wood and metal =====================
const planksH = (a, x0, x1, y0, y1) => { for (let y = y0 + 3; y < y1; y += 4) a.seamH(x0 + 1, x1 - 1, y, 'wood'); };
add('crate', 'wooden crate', 16, 14, 51, (a, o) => {
  a.rect(o, o, o + 15, o + 13, 'wood'); a.shade(.08); planksH(a, o, o + 15, o, o + 13);
  a.line(o + 1, o + 1, o + 14, o + 12, 1.4, 'wood2'); a.shade(0); planksH(a, o, o + 15, o, o + 13);
  for (const [x, y] of [[1, 1], [14, 1], [1, 12], [14, 12]]) a.rivet(o + x - (x > 8), o + y - (y > 8), 'dark');
});
add('crate', 'long crate', 24, 10, 52, (a, o) => {
  a.rect(o, o, o + 23, o + 9, 'wood2'); a.shade(.08); a.seamH(o + 1, o + 22, o + 4, 'wood2');
  for (const x of [3, 20]) { a.rect(o + x, o, o + x + 1, o + 9, 'dark'); } a.shade(0); a.seamH(o + 1, o + 22, o + 4, 'wood2');
  a.rect(o + 9, o + 2, o + 14, o + 6, 'yellow'); a.shade(0);
});
add('crate', 'strapped crate', 15, 15, 53, (a, o) => {
  a.rect(o, o, o + 14, o + 14, 'wood'); a.shade(.08); planksH(a, o, o + 14, o, o + 14);
  a.rect(o, o + 3, o + 14, o + 4, 'dark'); a.rect(o, o + 10, o + 14, o + 11, 'dark'); a.shade(0); planksH(a, o, o + 14, o, o + 14);
  a.rivet(o + 7, o + 3, 'steel'); a.rivet(o + 7, o + 10, 'steel');
});
add('crate', 'broken crate', 16, 14, 54, (a, o) => {
  a.poly([[o, o + 2], [o + 9, o], [o + 15, o + 1], [o + 16, o + 14], [o + 0, o + 14]], 'wood'); a.tear(.22); a.shade(.1); planksH(a, o, o + 15, o, o + 13);
  a.erase(o + 11, o + 4); a.erase(o + 12, o + 5); a.erase(o + 12, o + 4); a.erase(o + 13, o + 6); a.line(o + 3, o + 3, o + 8, o + 10, 1, 'wood2'); a.shade(0); planksH(a, o, o + 15, o, o + 13);
});
add('crate', 'small box', 10, 9, 55, (a, o) => {
  a.rect(o, o, o + 9, o + 8, 'wood2'); a.shade(.08); a.seamH(o + 1, o + 8, o + 3, 'wood2'); a.rect(o + 4, o, o + 5, o + 8, 'orange'); a.shade(0); a.seamH(o + 1, o + 8, o + 3, 'wood2');
});
add('crate', 'metal crate', 16, 13, 56, (a, o) => {
  a.rect(o, o, o + 15, o + 12, 'dark'); a.shade(.04); rivets(a, o + 1, o + 1, o + 13, o + 10, 4);
  a.rect(o + 4, o + 4, o + 11, o + 8, 'yellow'); a.shade(0); rivets(a, o + 1, o + 1, o + 13, o + 10, 4); a.paint(o + 6, o + 6, '#23272e'); a.paint(o + 7, o + 6, '#23272e'); a.paint(o + 8, o + 6, '#23272e');
});
add('crate', 'barrel', 12, 16, 57, (a, o) => {
  a.rect(o + 1, o, o + 10, o + 15, 'orange'); a.rect(o, o + 1, o + 11, o + 14, 'orange'); a.shade(.06);
  a.seamH(o, o + 11, o + 4, 'orange'); a.seamH(o, o + 11, o + 10, 'orange'); a.spots(3, 'rust', 2);
});
add('crate', 'tilted crate', 17, 17, 58, (a, o) => {
  const c = [o + 8.5, o + 8.5], s = 6.4, t = 0.4, R = (x, y) => [c[0] + x * Math.cos(t) - y * Math.sin(t), c[1] + x * Math.sin(t) + y * Math.cos(t)];
  a.poly([R(-s, -s), R(s, -s), R(s, s), R(-s, s)], 'wood'); a.shade(.08);
  for (const k of [-2.5, 2.5]) { const [x0, y0] = R(-s, k), [x1, y1] = R(s, k); a.wire([[x0, y0], [x1, y1]], M.wood[3]); }
});

// ===================== SUPPLY PODS =====================
add('pod', 'supply pod', 14, 16, 61, (a, o) => {
  a.poly([[o + 7, o], [o + 13, o + 9], [o + 1, o + 9]], 'yellow'); a.rect(o + 1, o + 9, o + 12, o + 12, 'steel'); a.shade(.04);
  a.disc(o + 7, o + 6, 1.6, 'dark'); a.shade(0); for (const x of [2, 6, 10]) a.rect(o + x, o + 13, o + x + 1, o + 15, 'dark'); a.shade(0);
});
add('pod', 'dented pod', 16, 16, 62, (a, o) => {
  a.disc(o + 8, o + 8, 6.8, 'yellow'); a.ring(o + 8, o + 8, 6.8, 7.6, 'steel'); a.tear(.06); a.shade(.06); a.seamH(o + 2, o + 13, o + 8, 'yellow'); a.disc(o + 8, o + 5, 1.5, 'glass'); a.shade(0);
  a.spots(3, 'grime', 2);
});
add('pod', 'cargo canister', 18, 10, 63, (a, o) => {
  a.rect(o + 2, o, o + 15, o + 9, 'yellow'); a.rect(o, o + 2, o + 17, o + 7, 'steel'); a.rect(o + 2, o, o + 15, o + 9, 'yellow'); a.shade(.04);
  a.seamV(o + 6, o + 1, o + 8, 'yellow'); a.seamV(o + 11, o + 1, o + 8, 'yellow'); a.rect(o + 0, o + 3, o + 1, o + 6, 'dark'); a.rect(o + 16, o + 3, o + 17, o + 6, 'dark'); a.shade(0);
});
add('pod', 'beacon pod', 12, 18, 64, (a, o) => {
  a.rect(o + 2, o + 4, o + 9, o + 15, 'yellow'); a.rect(o + 1, o + 6, o + 10, o + 13, 'yellow'); a.shade(.04);
  a.rect(o + 5, o, o + 6, o + 4, 'steel'); a.disc(o + 5.5, o + 1, 1.3, 'orange'); a.shade(0); a.seamH(o + 2, o + 9, o + 9, 'yellow'); a.rect(o + 3, o + 16, o + 8, o + 17, 'dark'); a.shade(0);
});

// ---------- write ----------
const manifest = {};
for (const [kind, list] of Object.entries(pieces)) {
  manifest[kind] = list.map((p, i) => {
    const file = `${kind}_${String(i + 1).padStart(2, '0')}.png`;
    fs.writeFileSync(path.join(out, file), png(p.art.w, p.art.h, p.art.rgba()));
    return { file, name: p.name, w: p.art.w, h: p.art.h };
  });
}
fs.writeFileSync(path.join(process.argv[2] ? out : __dirname, 'manifest.json'), JSON.stringify(manifest, null, 1));
console.log(Object.entries(manifest).map(([k, v]) => `${k}: ${v.length}`).join(', '));

// The planet's far land, in washes: mesas, a ridge, and near hills with far acacias on them, the same flat-crowned trees as `flora`, lost in the haze. Tiles across.
// Soft and always far, so at twice the logical size, not four times: the game draws it at its logical size whatever its file (PixelArt.Picture).
(function () {
'use strict';
const { canvas, rng } = Anime;
const S = 2, BW = 2048, BH = 384;
const hex = (c) => [parseInt(c.substr(1, 2), 16), parseInt(c.substr(3, 2), 16), parseInt(c.substr(5, 2), 16)];
const mix = (a, b, t) => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];
const clamp = (t) => Math.max(0, Math.min(1, t));
const smooth = (a, b, v) => { const t = clamp((v - a) / (b - a)); return t * t * (3 - 2 * t); };
const h2 = (x, y, s) => { let t = Math.imul(x, 374761393) + Math.imul(y, 668265263) + Math.imul(s, 2147483629); t = Math.imul(t ^ (t >>> 13), 1274126177); return ((t ^ (t >>> 16)) >>> 0) / 4294967296; };
const ease = (t) => t * t * (3 - 2 * t);
/** Noise along x, wrapping at the land's width: what its outline and its washes follow. */
function wave(x, cell, s, oct) {
  let v = 0, a = .5, n = 0;
  for (let k = 0; k < oct; k++) {
    const m = Math.round(BW / cell), X = x / cell, g = Math.floor(X), u = ease(X - g), w = (i) => ((i % m) + m) % m;
    v += a * (h2(w(g), 0, s + k * 17) + (h2(w(g + 1), 0, s + k * 17) - h2(w(g), 0, s + k * 17)) * u);
    n += a; a *= .5; cell /= 2;
  }
  return v / n;
}
/** A land's picture: for each logical point under its outline, its colour; soft-edged by a quarter of a logical pixel. */
function land(top, outline, tone) {
  const w = BW * S, h = (BH - top) * S, c = canvas(w, h), x = c.getContext('2d'), img = x.createImageData(w, h), d = img.data;
  for (let px = 0; px < w; px++) {
    const X = px / S, f = outline(X), wash = (wave(X, 96, 301, 3) - .5) * .05;
    for (let py = Math.max(0, Math.floor((f - top) * S) - 1); py < h; py++) {
      const Y = py / S + top, k = clamp((Y - f) * 2);
      if (k <= 0) continue;
      const col = tone(X, Y, f), i = (py * w + px) * 4;
      d[i] = col[0] * (1 + wash); d[i + 1] = col[1] * (1 + wash); d[i + 2] = col[2] * (1 + wash); d[i + 3] = k * 255;
    }
  }
  x.putImageData(img, 0, 0);
  return c;
}
const far = () => {
  const out = (X) => 168 - wave(X, 128, 7, 4) * 64, base = hex('#9bb7cc'), low = hex('#c6d6d6'), lit = hex('#dce6e2'), dim = hex('#8aa6bc'), haze = hex('#efe6cc');
  return land(96, out, (X, Y, f) => {
    const sl = out((X + 2) % BW) - out((X - 2 + BW) % BW);
    return mix(mix(mix(base, low, clamp((Y - f) / 90)), sl < 0 ? lit : dim, clamp(Math.abs(sl) * .12) * .5 * Math.exp(-(Y - f) / 30)), haze, smooth(150, 240, Y) * .6);
  });
};
const ridge = () => {
  const out = (X) => 196 - wave(X, 64, 13, 3) * 40, base = hex('#7d9fb2'), low = hex('#a8bfc4'), rim = hex('#cddcd8'), haze = hex('#e9e2c8');
  return land(140, out, (X, Y, f) => {
    const sl = out((X + 2) % BW) - out((X - 2 + BW) % BW);
    let col = mix(base, low, clamp((Y - f) / 60));
    if (Y - f < 1.5 && sl < -.6) col = mix(col, rim, .6);
    return mix(col, haze, smooth(170, 250, Y) * .55);
  });
};

/** A crown's ragged flat plate, as `flora`'s: blunt, flatter below, its top heaped in lumps. */
function crown(x, cx, cy, rx, ry, r) {
  const n = Math.max(14, Math.round(rx * 2.6)), p = r() * 6.3;
  x.beginPath();
  for (let k = 0; k < n; k++) {
    const t = (k / n) * Math.PI * 2, s = Math.sin(t), lump = 1 + .16 * Math.sin(t * 5 + p), tip = (r() - .5) * .9, B = (v) => Math.sign(v) * Math.pow(Math.abs(v), .62);
    const X = cx + B(Math.cos(t)) * (rx + tip), Y = cy + B(s) * ((s > 0 ? ry * .62 : ry) * lump + tip * .5);
    k ? x.lineTo(X, Y) : x.moveTo(X, Y);
  }
  x.closePath();
}
const taper = (x, x0, y0, x1, y1, w0, w1) => { x.beginPath(); x.moveTo(x0 - w0 / 2, y0); x.lineTo(x1 - w1 / 2, y1); x.lineTo(x1 + w1 / 2, y1); x.lineTo(x0 + w0 / 2, y0); x.closePath(); x.fill(); };
/** A far acacia, its base at (bx, by), h tall: a forked trunk under two or three flat plates, strands hanging from them. Logical pixels. */
function acacia(x, bx, by, h, r, body, top) {
  const lean = (r() - .5) * h * .14, forkY = by - h * (.42 + r() * .16), forkX = bx + lean, w = Math.max(1, h * .055), arms = 2 + (r() < .35 ? 1 : 0), ends = [];
  x.fillStyle = body;
  taper(x, bx, by + 2, forkX, forkY, w * 1.3, w);
  for (let i = 0; i < arms; i++) {
    const side = arms === 2 ? (i ? 1 : -1) : i - 1, ex = forkX + side * h * (.2 + r() * .16) + lean * .4, ey = by - h * (.78 + r() * .14) + (side === 0 ? -h * .08 : 0);
    taper(x, forkX, forkY, ex, ey, w * .8, w * .35);
    ends.push([ex, ey, h * (.2 + r() * .1)]);
  }
  for (const [ex, ey, rx] of ends) {
    crown(x, ex, ey - .4, rx, Math.max(1.1, rx * .24), r); x.fillStyle = body; x.fill();
    for (let n = Math.round(rx * .55); n > 0; n--) { const sx = ex + (r() - .5) * rx * 1.6; x.fillRect(sx, ey, .4, 1 + r() * h * .07); }
  }
  // The side of each crown toward the sun, a tone up in the haze.
  for (const [ex, ey, rx] of ends) { crown(x, ex + rx * .12, ey - Math.max(.8, rx * .13), rx * .68, Math.max(.6, rx * .1), r); x.fillStyle = top; x.fill(); }
}
const hills = () => {
  const top = 150, w = BW * S, h = (BH - top) * S, out = (X) => 236 - wave(X, 128, 19, 3) * 26, c = canvas(w, h), x = c.getContext('2d'), body = '#5f8684', lit = '#7fa29a', rim = '#86a8a0', r = rng(0x61);
  x.scale(S, S); x.translate(0, -top);
  // Trees first, their feet into the land drawn after; a tree over the sheet's edge is drawn at both ends, so the layer tiles.
  for (let k = 0; k < 24; k++) {
    const tx = 20 + k * 85 + r() * 50, tall = 16 + Math.pow(r(), 2) * 44, seed = Math.floor(r() * 1e9);
    for (const dx of [-BW, 0, BW]) if (tx + dx > -60 && tx + dx < BW + 60) acacia(x, tx + dx, out(tx), tall, rng(seed), body, lit);
  }
  x.setTransform(1, 0, 0, 1, 0, 0);
  x.fillStyle = body;
  for (let px = 0; px < w; px++) { const f = (out(px / S) - top) * S; x.fillRect(px, f, 1, h - f); }
  x.fillStyle = rim;
  for (let px = 0; px < w; px++) { const f = (out(px / S) - top) * S; x.fillRect(px, f, 1, 2); }
  // The haze over all of it, by height, as the pixel layer's.
  const img = x.getImageData(0, 0, w, h), d = img.data, h0 = hex('#e6dfc4'), h1 = hex('#f4e8c8');
  for (let py = 0; py < h; py++) {
    const Y = py / S + top, a = smooth(200, 280, Y) * .5 + .12, b = .18 * smooth(200, 260, Y);
    for (let px = 0; px < w; px++) {
      const i = (py * w + px) * 4;
      if (!d[i + 3]) continue;
      const col = mix(mix([d[i], d[i + 1], d[i + 2]], h0, a), h1, b * .85);
      d[i] = col[0]; d[i + 1] = col[1]; d[i + 2] = col[2];
    }
  }
  x.putImageData(img, 0, 0);
  return c;
};

Subjects.backdrop = function () {
  out('planet/far', far());
  out('planet/ridge', ridge());
  out('planet/hills', hills());
};
})();

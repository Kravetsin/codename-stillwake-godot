// The planet's sky, behind the land composed from the planet's own pictures (features/backdrop) and never brighter than it: a slate wash laid in glazes, thin dragged cloud, a dead ringed giant half in its own shadow, a hard pale sun.
// The sky and the clouds are far layers: PixelArt.Far draws them at their logical size whatever their file, so they are not 4 x (1024 x 768 for 1 x 384, 2 x for the clouds).
(function () {
'use strict';
const { canvas, poly, ell } = Anime;
const hex = (c) => [parseInt(c.substr(1, 2), 16), parseInt(c.substr(3, 2), 16), parseInt(c.substr(5, 2), 16)];
const clamp = (t) => Math.max(0, Math.min(1, t));
const smooth = (a, b, v) => { const t = clamp((v - a) / (b - a)); return t * t * (3 - 2 * t); };
const h2 = (x, y, s) => { let t = Math.imul(x, 374761393) + Math.imul(y, 668265263) + Math.imul(s, 2147483629); t = Math.imul(t ^ (t >>> 13), 1274126177); return ((t ^ (t >>> 16)) >>> 0) / 4294967296; };
/** Smooth noise over the plane, octaves of it, about 0 to 1. */
function noise(x, y, s, oct) {
  let v = 0, a = .5, n = 0;
  for (let k = 0; k < oct; k++) {
    const X = Math.floor(x), Y = Math.floor(y), e = (t) => t * t * (3 - 2 * t), u = e(x - X), w = e(y - Y), q = s + k * 31;
    const A = h2(X, Y, q), B = h2(X + 1, Y, q), C = h2(X, Y + 1, q), D = h2(X + 1, Y + 1, q);
    v += a * (A + (B - A) * u + (C - A) * w + (A - B - C + D) * u * w);
    n += a; a *= .5; x *= 2.03; y *= 2.03;
  }
  return v / n;
}

/* ---- The sky: the pixel one is a pure gradient from blue to cream. This is a graded wash too, a sky is what it is, but slate, and laid in glazes one over another, each drying to a hard wandering edge. Its top row is one flat colour: the game fills above it with that. ---- */
// A logical row and its colour. By the far land's crest (rows 104 to 168) it is darker than the land's #9bb7cc; by the horizon greyer than the land's haze.
const RAMP = [[0, '#687c8e'], [60, '#788c9c'], [110, '#8a9eab'], [160, '#9dadb2'], [210, '#adb5b0'], [255, '#c3c3b2'], [290, '#d3cdb7'], [384, '#d8d1ba']].map(([y, c]) => [y, hex(c)]);
// Each glaze: the logical row its lower edge wanders about, how far it wanders, how much it darkens all above it.
const GLAZES = [[34, 5, .985, 11], [78, 9, .98, 23], [122, 12, .985, 37], [158, 8, .985, 53], [196, 14, .98, 71], [232, 10, .99, 89], [268, 7, .99, 113]];
function sky() {
  const w = 1024, h = 768, c = canvas(w, h), x = c.getContext('2d'), img = x.createImageData(w, h), d = img.data;
  for (let py = 0; py < h; py++) {
    const Y = py / 2;
    let i = 0;
    while (i < RAMP.length - 2 && Y > RAMP[i + 1][0]) i++;
    const t = clamp((Y - RAMP[i][0]) / (RAMP[i + 1][0] - RAMP[i][0])), base = [0, 1, 2].map((k) => RAMP[i][1][k] + (RAMP[i + 1][1][k] - RAMP[i][1][k]) * t);
    // Away from the top, for the flat fill above it to meet it.
    const free = smooth(4, 26, Y);
    for (let px = 0; px < w; px++) {
      const X = px / w * 7;
      let k = 1;
      for (const [row, wander, dark, seed] of GLAZES) {
        const edge = row + (noise(X, 0, seed, 3) - .5) * wander * 2.4 + (noise(X * 6, seed, seed, 2) - .5) * wander * .3;
        if (Y < edge) k *= dark;
        if (Y >= edge - 1.1 && Y < edge) k *= .985;
      }
      const bloom = (noise(X * .9, Y / 46, 5, 3) - .5) * .07 * free, grain = (h2(px, py, 3) - .5) * .018 * free, soak = (noise(X * 5, Y / 7, 9, 2) - .5) * .02 * free;
      const f = k * (1 + bloom + grain + soak), o = (py * w + px) * 4;
      d[o] = base[0] * f; d[o + 1] = base[1] * f; d[o + 2] = base[2] * f; d[o + 3] = 255;
    }
  }
  x.putImageData(img, 0, 0);
  return c;
}

/* ---- The clouds: high, thin, dragged by one wind: a dry brush pulled along, its belly a tone down in a hard step. Banks placed by hand, each logical x, y, half its length, half its depth. Tiles across. ---- */
const BANKS = [
  [130, 22, 170, 5], [395, 14, 62, 2.5], [300, 42, 96, 3.5], [560, 32, 128, 6], [770, 16, 74, 3], [705, 58, 60, 2.5],
  [965, 40, 180, 6.5], [1190, 20, 90, 3], [1085, 74, 86, 3], [1370, 34, 120, 4.5], [1555, 58, 150, 7], [1500, 18, 54, 2.2],
  [1760, 28, 118, 4], [1905, 62, 96, 4], [1980, 18, 66, 2.5], [60, 74, 110, 3.5], [440, 92, 68, 2.2], [820, 108, 104, 3], [1290, 112, 76, 2.4], [1700, 100, 92, 2.6],
];
function clouds() {
  const BW = 2048, BH = 160, S = 2, c = canvas(BW * S, BH * S), x = c.getContext('2d');
  BANKS.forEach(([cx, cy, L, T], b) => {
    const seed = 40 + b * 7, wander = 1.5 + T * .4;
    for (let row = -T * 2 - 1.5; row <= T * 2 + 1; row += .5) {
            for (let dx = -L * 1.15; dx <= L * 1.15; dx += .5) {
        // The wind's lean, and a slow wave along it: the bank is not a lens.
        const u = dx / L, sag = (noise(dx / 70, 1, seed, 2) - .5) * wander * 2 + u * T * .55;
        const vv = (row - sag * .35) / (T * 2), reach = 1 - Math.pow(Math.abs(u), 2.4), body = vv < 0 ? reach - vv * vv * 1.15 : reach - Math.pow(vv, 4) * 1.1;
        const dry = noise(dx / 26, row * .85, seed, 3) * .6 + noise(dx / 6, row * 1.8, seed + 1, 2) * .4;
        if (dry < .72 - body * .4) continue;
        const belly = vv + (noise(dx / 18, 3, seed + 2, 2) - .5) * .5 > .22;
        x.fillStyle = belly ? 'rgba(118,132,146,.4)' : 'rgba(208,210,200,.44)';
        const px = (((cx + dx) % BW) + BW) % BW;
        x.fillRect(px * S, (cy + row) * S, 1, 1);
      }
    }
  });
  return c;
}

/* ---- The giant: a pale ringed world in the haze, its middle and its rings where the pixel one has them, lit from the sun's side of the screen, half of it gone in its own hard shadow. Its pen is a dark slate, not black: it is far. ---- */
const CX = 116, CY = 75, RAD = 51, TILT = 13.4 * Math.PI / 180;
const G = { lite: '#a6b1ae', base: '#92a2a6', band: '#82949c', scar: '#74868f', shade: '#627686', deep: '#536778', ink: '#3c4c5c', ring: '#9eabac', ringDim: '#7a8c97', gapDim: '#5a6d7e' };
/** A point on the ring plane: t round it, at radius share k of the outer ring's (100 x 24). */
const ringAt = (t, k) => { const a = Math.cos(t) * 100 * k, b = Math.sin(t) * 24 * k; return [CX + a * Math.cos(TILT) - b * Math.sin(TILT), CY + a * Math.sin(TILT) + b * Math.cos(TILT)]; };
const arc = (t0, t1, k, n) => Array.from({ length: n + 1 }, (_, i) => ringAt(t0 + (t1 - t0) * i / n, k));
const band = (t0, t1, k0, k1) => { const n = 40; return poly(arc(t0, t1, k0, n).concat(arc(t1, t0, k1, n)).flat()); };

function giant(a) {
  const disc = ell(CX, CY, RAD, RAD);
  const penArc = (t0, t1, k, w) => a.line('M' + arc(t0, t1, k, 40).map((p) => p.map((n) => n.toFixed(2)).join(' ')).join('L'), { w, c: G.ink });
  /** The ring, its back half or its front: two broad flats with the dark gap between, the far side dimmer. */
  const ring = (t0, t1, c) => {
    a.flat(band(t0, t1, .93, 1), G.ringDim);
    a.flat(band(t0, t1, .74, .93), c);
    a.flat(band(t0, t1, .7, .74), G.gapDim);
    a.flat(band(t0, t1, .61, .7), G.ringDim);
  };
  ring(Math.PI, Math.PI * 2, G.ring);
  penArc(Math.PI * 1.04, Math.PI * 1.96, .86, .9);
  // The body; the wash bands lie along the rings' plane, placed, each a torn step of close tone.
  a.flat(disc, G.base);
  const strip = (y0, y1, wob, c) => {
    const top = [], bot = [];
    for (let i = 0; i <= 22; i++) {
      const u = -1.1 + 2.2 * i / 22, X = CX + u * RAD * Math.cos(TILT), Y = CY + u * RAD * Math.sin(TILT);
      const n0 = (noise(u * 3 + 2, y0, 77, 2) - .5) * wob, n1 = (noise(u * 3.4 + 7, y1, 78, 2) - .5) * wob;
      top.push([X + y0 * -Math.sin(TILT), Y + y0 * Math.cos(TILT) + n0]);
      bot.unshift([X + y1 * -Math.sin(TILT), Y + y1 * Math.cos(TILT) + n1]);
    }
    a.flat(poly(top.concat(bot).flat()), c, { clip: disc });
  };
  strip(-56, -31, 5, G.lite);
  strip(-31, -24, 3, G.base);
  strip(-20, -9, 5, G.band);
  strip(-5, 3, 4, G.scar);
  strip(9, 21, 6, G.band);
  strip(28, 37, 5, G.scar);
  strip(40, 58, 4, G.band);
  // Its own shadow, in two hard steps, on the far side from the sun, which hangs to the right of it on the screen, and high.
  const toSun = [.62, -.78], across = [.78, .62];
  /** The part of the disc behind a terminator: the limb's far half, then back along a half ellipse bulging bulge x the radius toward the sun. */
  const dark = (bulge, seed) => {
    const pts = [];
    for (let i = 0; i <= 40; i++) { const t = Math.PI * i / 40, c = Math.cos(t) * RAD * 1.02, q = -Math.sin(t) * RAD * 1.02; pts.push(CX + across[0] * c + toSun[0] * q, CY + across[1] * c + toSun[1] * q); }
    for (let i = 40; i >= 0; i--) { const t = Math.PI * i / 40, c = Math.cos(t) * RAD * 1.02, q = Math.sin(t) * RAD * bulge + (noise(i * .22, 4, seed, 2) - .5) * 3; pts.push(CX + across[0] * c + toSun[0] * q, CY + across[1] * c + toSun[1] * q); }
    return poly(pts);
  };
  a.flat(dark(.12, 91), G.shade, { clip: disc });
  a.flat(dark(-.42, 92), G.deep, { clip: disc });
  // A long storm's scar, a mark that it is a world and old.
  a.flat(poly(CX + 8, CY - 19, CX + 21, CY - 21, CX + 33, CY - 15, CX + 29, CY - 11, CX + 18, CY - 11, CX + 9, CY - 14), G.scar, { clip: disc });
  a.line(`M${CX + 12} ${CY - 15}L${CX + 20} ${CY - 18}L${CX + 28} ${CY - 15}`, { w: .9, c: G.ink });
  // The rings' shadow lying across the body, under the front arc.
  a.flat(band(Math.PI * .08, Math.PI * .92, .55, .63), G.deep, { clip: disc });
  ring(0, Math.PI, G.ring);
  penArc(Math.PI * .02, Math.PI * .4, .86, .9);
  penArc(Math.PI * .55, Math.PI * .98, .86, .9);
  // Where the shadow falls over the back of the ring, a hard break; and the limb's broken pen, only on its lit side.
  a.line(`M${CX + 18} ${CY - 49}L${CX + 34} ${CY - 39}L${CX + 46} ${CY - 22}L${CX + 51} ${CY - 2}L${CX + 49} ${CY + 14}`, { w: 1, c: G.ink });
}

/** A hazed picture: far, its colours sunk into the sky a little, as the pixel one is. */
function hazed(c, alpha) {
  const o = canvas(c.width, c.height), x = o.getContext('2d');
  x.globalAlpha = alpha;
  x.drawImage(c, 0, 0);
  return o;
}

/* ---- The sun: added over the sky, so only its own light counts: a hard pale disc behind thin haze, three torn steps of tone round it and no soft bloom; a short uneven glint across it, as thin as a pen's. ---- */
function sun() {
  const w = 181 * 4, c = canvas(w, w), x = c.getContext('2d'), M = 90.5 * 4;
  const round = (r, tear, col, seed) => {
    x.fillStyle = col;
    x.beginPath();
    for (let i = 0; i < 180; i++) {
      const t = i / 180 * Math.PI * 2, q = r * 4 * (1 + (noise(Math.cos(t) * 5 + 9, Math.sin(t) * 5 + 9, seed, 3) - .5) * tear);
      i ? x.lineTo(M + Math.cos(t) * q, M + Math.sin(t) * q) : x.moveTo(M + Math.cos(t) * q, M + Math.sin(t) * q);
    }
    x.closePath();
    x.fill();
  };
  round(58, .16, 'rgba(255,236,190,.03)', 11);
  round(34, .14, 'rgba(255,238,196,.05)', 12);
  round(18, .12, 'rgba(255,240,204,.08)', 13);
  round(11, .08, 'rgba(255,240,206,.16)', 14);
  // Thin as a pen, long one way, short the other: not a star.
  x.fillStyle = 'rgba(255,240,208,.2)';
  for (const [dx, dy, len, wid] of [[1, 0, 44, .34], [-1, 0, 30, .3], [0, -1, 22, .26], [0, 1, 26, .26]]) {
    x.beginPath(); x.moveTo(M - dy * wid * 4, M + dx * wid * 4); x.lineTo(M + dx * len * 4, M + dy * len * 4); x.lineTo(M + dy * wid * 4, M - dx * wid * 4); x.closePath(); x.fill();
  }
  round(8, .035, '#fff3d8', 15);
  return c;
}

Subjects.heavens = function () {
  out('planet/sky', sky());
  out('planet/clouds', clouds());
  out('planet/giant', hazed(Anime.bake({ w: 232, h: 150, draw: giant }), .8));
  out('planet/sun', sun());
};
})();

// The tablet's key as a frosted lens (the key-lens ADR), as picked: 22 by 22, one picture's pixel to one of the screen, no finish. Dark and lit, at rest and sunk a pixel while held.
(function () {
'use strict';
const { canvas } = Anime;
const clamp = (v) => Math.min(1, Math.max(0, v));
const lerp = (a, b, t) => a + (b - a) * t;
const mix = (a, b, t) => [lerp(a[0], b[0], t), lerp(a[1], b[1], t), lerp(a[2], b[2], t)];

const bake = (shift) => {
  const cells = [], scratched = new Set();
  let seed = 90173;
  const rnd = () => { seed = (seed * 1664525 + 1013904223) >>> 0; return seed / 4294967296; };
  for (let t = 0; t < 7; t++) scratched.add((2 + t) + ',' + (16 + Math.round(t * .4)));
  for (let t = 0; t < 5; t++) scratched.add((14 + t) + ',' + (3 + Math.round(t * .6)));
  for (let j = 0; j < 22; j++) {
    for (let i = 0; i < 22; i++) {
      const x = i + .5 - 11, y = j + .5 - shift, centre = Math.hypot(x, y - 8.5), radial = clamp(Math.hypot(x, y - 9) / 14);
      let m = 0, inside = false;
      if (y < 12.5) { m = clamp(1.4 - Math.abs(centre - 6.3)); inside = centre < 6.3; }
      else if (y < 15.5) { const hw = lerp(4.87, 2.8, (y - 12.5) / 3); m = clamp(1.4 - Math.abs(Math.abs(x) - hw)); inside = Math.abs(x) < hw; }
      else if (y < 19.5 && Math.abs(x) <= 3) m = Math.abs(y - 17.5) < .6 ? .6 : 1;
      else if (y < 20.5 && Math.abs(x) <= 1.5) m = .8;
      cells.push({ i, j, m, inside, radial, grain: rnd() * 2 - 1, scratch: scratched.has(i + ',' + j) ? 1 : 0 });
    }
  }
  return cells;
};

const lens = (k, down) => {
  const made = canvas(22, 22), c = made.getContext('2d'), gain = down ? .8 : 1;
  for (const p of bake(down ? 1 : 0)) {
    const glow = (1 - p.radial) * (p.inside ? 1 : .72);
    let col = mix(mix([38, 43, 52], [52, 58, 69], glow), mix([70, 44, 22], [226, 172, 94], glow), k);
    col = mix(col, mix([20, 22, 28], [62, 36, 17], k), p.m * .72);
    const v = 1 - .28 * p.radial * p.radial, add = p.grain * (2.5 + 6 * k) + p.scratch * (10 + 18 * k);
    let [r, g, b] = col.map((n) => (n * v + add) * gain);
    if (p.j === 0) { if (down) { r *= .45; g *= .45; b *= .45; } else { r += 14; g += 14; b += 16; } }
    if (p.j === 21) { r *= .7; g *= .7; b *= .7; }
    c.fillStyle = `rgb(${Math.max(0, Math.min(255, r | 0))},${Math.max(0, Math.min(255, g | 0))},${Math.max(0, Math.min(255, b | 0))})`;
    c.fillRect(p.i, p.j, 1, 1);
  }
  return made;
};

Subjects.keylens = function () {
  out('key_lens_dark', lens(0, false));
  out('key_lens_lit', lens(1, false));
  out('key_lens_dark_down', lens(0, true));
  out('key_lens_lit_down', lens(1, true));
};
})();

// The suits and helmets painted raw (skins): GPT's paint on a sheet of the rig's parts (a suit's four: torso, pack, arm, leg;
// whole-limbs), in ink and watercolour, shrunk by area to the hd size and laid side by side in traced/suit<Name>.png. Each part goes
// out as it is, its margin included: `out` + the part's name. An arm and a leg are drawn straight, the elbow 8.59 and the knee 11.32
// down their boxes; a sheet with no `out` is kept for later, not drawn. A suit that glows (`lit`) has its lights cut from its own
// paint, by hue: `out` + part + `_lit`, and its pip.
(function () {
'use strict';
const rgb = (hex) => [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16));

/** Whether a pixel's hue lies in [from, to) degrees, the range wrapping past 360; greys never. */
function inHue([r, g, b], [from, to]) {
  const mx = Math.max(r, g, b), d = mx - Math.min(r, g, b);
  if (d < 18 || 0.3 * r + 0.59 * g + 0.11 * b < 50) return false;
  let h = mx === r ? ((g - b) / d) % 6 : mx === g ? (b - r) / d + 2 : (r - g) / d + 4;
  h = (h * 60 + 360) % 360;
  return from < to ? h >= from && h < to : h >= from || h < to;
}

/** The pixels of `c` in `hues`, recoloured from `halo` (dark paint) to `core` (light), each as covered as it is painted. */
function lightOf(c, hues, core, halo) {
  const src = c.getContext('2d').getImageData(0, 0, c.width, c.height), lit = new ImageData(c.width, c.height);
  const [k, h] = [rgb(core), rgb(halo)];
  for (let i = 0; i < src.data.length; i += 4) {
    const p = [src.data[i], src.data[i + 1], src.data[i + 2]];
    if (src.data[i + 3] === 0 || !inHue(p, hues)) continue;
    const t = Math.min(1, (0.3 * p[0] + 0.59 * p[1] + 0.11 * p[2]) / 160);
    for (let j = 0; j < 3; j++) lit.data[i + j] = Math.round(h[j] + (k[j] - h[j]) * t);
    lit.data[i + 3] = src.data[i + 3];
  }
  const out = document.createElement('canvas');
  [out.width, out.height] = [c.width, c.height];
  const x = out.getContext('2d');
  const core1 = document.createElement('canvas');
  [core1.width, core1.height] = [c.width, c.height];
  core1.getContext('2d').putImageData(lit, 0, 0);
  // A soft halo of the same pixels under them, as the code-drawn lights have.
  x.globalAlpha = 0.35; x.filter = 'blur(2px)'; x.drawImage(core1, 0, 0);
  x.globalAlpha = 1; x.filter = 'none'; x.drawImage(core1, 0, 0);
  return out;
}

Subjects.suits = function () {
  for (const [name, t] of Object.entries(TRACED)) {
    if (!t.out || !t.raw) continue;
    const img = Traced.picture(name);
    for (const [part, [x, y, w, h]] of Object.entries(t.parts)) {
      const c = document.createElement('canvas');
      [c.width, c.height] = [w, h];
      c.getContext('2d').drawImage(img, x, y, w, h, 0, 0, w, h);
      out(t.out + part, c);
      const lit = t.lit;
      if (lit?.parts.includes(part)) out(`${t.out}${part}_lit`, lightOf(c, lit.hues, lit.core, lit.halo));
      if (lit?.pip?.[0] === part) out(`${t.out}pip`, lightOf(c, lit.hues, lit.pip[1], lit.pip[1]));
    }
  }
};
})();

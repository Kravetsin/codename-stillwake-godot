// A zoomed contact sheet for checking the pixels by eye: per direction the icon, the held sprite and its
// spin frames, at 8x; the game's drill, harpoon and axe first for scale, held (their icons are, 2026-10-27-tool-icons).
const P = require('../weapons/png.js'), B = require('./build.js'), path = require('path');
const ART = path.join(__dirname, '..', '..', '..', 'game', 'art') + '/';
const S = +(process.argv[2] || 8), gap = 14;
const g = f => P.read(ART + f + '.png');
const rows = [[g('hero_drill'), g('hero_harpoon'), g('hero_axe')],
  ...Object.values(B).map(d => [d.icon, d.heldIm, ...d.spinIms])];
const W = Math.max(...rows.map(r => r.reduce((a, im) => a + im.w * S + gap, gap)));
const H = rows.reduce((a, r) => a + Math.max(...r.map(im => im.h)) * S + gap, gap);
const c = { w: W, h: H, data: Buffer.alloc(W * H * 4) };
for (let i = 0; i < W * H; i++) c.data.set([0x2a, 0x33, 0x44, 255], i * 4);
let y = gap;
for (const r of rows) {
  let x = gap;
  for (const im of r) {
    for (let py = 0; py < im.h * S; py++) for (let px = 0; px < im.w * S; px++) {
      const i = ((py / S | 0) * im.w + (px / S | 0)) * 4, o = ((y + py) * W + x + px) * 4;
      if (im.data[i + 3]) im.data.copy(c.data, o, i, i + 4);
      else if (((px / S | 0) + (py / S | 0)) % 2) c.data.set([0x22, 0x2a, 0x38, 255], o);
    }
    x += im.w * S + gap;
  }
  y += Math.max(...r.map(im => im.h)) * S + gap;
}
P.write(path.join(__dirname, 'preview.png'), c);
for (const [id, d] of Object.entries(B)) console.log(id, d.heldIm.w + 'x' + d.heldIm.h, 'grip', d.grip, 'chuck', d.chuck, 'tip', d.tip, 'plug', d.plug);

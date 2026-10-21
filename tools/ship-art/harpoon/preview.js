// A zoomed contact sheet for checking the pixels by eye: per direction the icon, the held sprite empty
// and loaded, and the rod, at 8x, with the fire axe and its icon for scale.
const P = require('../weapons/png.js'), B = require('./build.js'), path = require('path');
const ART = path.join(__dirname, '..', '..', '..', 'game', 'art') + '/';
const S = 8, gap = 16;
const axe = P.read(ART + 'hero_axe.png'), axeIcon = P.read(ART + 'items/fireAxe.png');
const rows = [[axeIcon, axe], ...Object.values(B).map(d => [d.icon, d.heldIm, d.loadedIm, d.rodIm])];
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
for (const [id, d] of Object.entries(B)) console.log(id, 'held', d.heldIm.w + 'x' + d.heldIm.h, 'loaded', d.loadedIm.w + 'x' + d.loadedIm.h, 'rod', d.rodIm.w + 'x' + d.rodIm.h, 'grip', d.grip, 'muzzle', d.muzzle, 'reel', d.reelAt, 'eye', d.eye);

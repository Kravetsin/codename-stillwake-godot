// One image per direction at 12x: icon, loaded held, rod; for close checking.
const P = require('../weapons/png.js'), B = require('./build.js'), path = require('path');
const S = 12, gap = 12;
for (const [id, d] of Object.entries(B)) {
  const ims = [d.icon, d.loadedIm, d.rodIm];
  const W = ims.reduce((a, im) => a + im.w * S + gap, gap), H = Math.max(...ims.map(im => im.h)) * S + 2 * gap;
  const c = { w: W, h: H, data: Buffer.alloc(W * H * 4) };
  for (let i = 0; i < W * H; i++) c.data.set([0x2a, 0x33, 0x44, 255], i * 4);
  let x = gap;
  for (const im of ims) {
    for (let py = 0; py < im.h * S; py++) for (let px = 0; px < im.w * S; px++) {
      const i = ((py / S | 0) * im.w + (px / S | 0)) * 4, o = ((gap + py) * W + x + px) * 4;
      if (im.data[i + 3]) im.data.copy(c.data, o, i, i + 4);
      else if (((px / S | 0) + (py / S | 0)) % 2) c.data.set([0x22, 0x2a, 0x38, 255], o);
    }
    x += im.w * S + gap;
  }
  P.write(path.join(__dirname, `zoom_${id}.png`), c);
}

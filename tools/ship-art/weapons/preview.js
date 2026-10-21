// Quick look: every held sprite and icon at 8x, grip (cyan) and edge points (orange) dotted.
delete require.cache[require.resolve('./art.js')];
const P = require('./png.js'), W = require('./build.js');
const S = +(process.env.S || 8), ids = Object.keys(W);
const rowH = Math.max(...ids.map(i => Math.max(W[i].held.h, 16))) * S + 16;
const width = 40 * S + 16 * S + 48, height = ids.length * rowH + 16;
const img = { w: width, h: height, data: Buffer.alloc(width * height * 4) };
const put = (x, y, r, g, b, a = 255) => { if (x < 0 || y < 0 || x >= width || y >= height) return; const o = (y * width + x) * 4, k = a / 255; img.data[o] = img.data[o] * (1 - k) + r * k; img.data[o + 1] = img.data[o + 1] * (1 - k) + g * k; img.data[o + 2] = img.data[o + 2] * (1 - k) + b * k; img.data[o + 3] = 255; };
for (let y = 0; y < height; y++) for (let x = 0; x < width; x++) put(x, y, 0x2a, 0x33, 0x40);
const blit = (im, ox, oy) => { for (let y = 0; y < im.h * S; y++) for (let x = 0; x < im.w * S; x++) { const s = ((y / S | 0) * im.w + (x / S | 0)) * 4; if (im.data[s + 3]) put(ox + x, oy + y, im.data[s], im.data[s + 1], im.data[s + 2]); } };
const dot = (cx, cy, r, g, b) => { for (let y = -3; y <= 3; y++) for (let x = -3; x <= 3; x++) if (x * x + y * y <= 9) put(Math.round(cx + x), Math.round(cy + y), r, g, b); };
ids.forEach((id, n) => {
  const w = W[id], oy = 8 + n * rowH;
  blit(w.held, 8, oy);
  dot(8 + w.grip[0] * S, oy + w.grip[1] * S, 0x6f, 0xd3, 0xff);
  for (const e of w.edge) dot(8 + e[0] * S, oy + e[1] * S, 0xff, 0x7a, 0x2e);
  blit(w.icon, 8 + 40 * S, oy);
});
P.write(process.argv[2] || 'preview.png', img);
console.log(ids.map(i => `${i} ${W[i].held.w}x${W[i].held.h} grip ${W[i].grip}`).join('\n'));

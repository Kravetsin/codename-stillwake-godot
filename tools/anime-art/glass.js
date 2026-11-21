// Cuts the painted glass out of the raw helmets' shells (light-drone): their glass was dark paint with white glints, read as
// cracked, and hid the head behind it. The opening is found from the glints (the old visor picture) by its dark-glass and glint
// colours, cleared in the shell, and filled in the visor with the basic helmet's own glass (hero_visor) stretched to fit, so every
// helmet shows the face through the same glass. node tools/anime-art/glass.js [helmet...]; run once on the raw pictures.
'use strict';
const fs = require('fs'), path = require('path');
const { decode, encode } = require('./socket/png.js');
const hd = path.join(__dirname, '..', '..', 'game', 'art', 'hd');
const RAW = ['heatedHelmet', 'hideHelmet', 'boilerHelmet', 'dustHelmet'];

const load = (name) => decode(path.join(hd, name + '.png'));
const at = (im, x, y) => (y * im.w + x) * 4;

/** Whether a shell pixel is the painted glass: deep blue-grey, or a pale glint on it. */
function glassy(px, i) {
  const [r, g, b, a] = [px[i], px[i + 1], px[i + 2], px[i + 3]];
  if (a < 128) return false;
  const luma = 0.3 * r + 0.59 * g + 0.11 * b, sat = Math.max(r, g, b) - Math.min(r, g, b);
  return (luma < 78 && b >= r - 4) || (luma > 150 && sat < 60 && b >= r - 10);
}

/** The opening: the glass flood-filled from the glints, its enclosed holes filled too. */
function opening(shell, glints) {
  const { w, h } = shell, inside = new Uint8Array(w * h), stack = [];
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) if (glints.px[at(glints, x, y) + 3] > 0) stack.push(y * w + x);
  while (stack.length) {
    const p = stack.pop();
    if (inside[p]) continue;
    const x = p % w, y = (p / w) | 0;
    if (!glassy(shell.px, p * 4) && glints.px[p * 4 + 3] === 0) continue;
    inside[p] = 1;
    if (x > 0) stack.push(p - 1); if (x < w - 1) stack.push(p + 1); if (y > 0) stack.push(p - w); if (y < h - 1) stack.push(p + w);
  }
  // Holes: whatever the outside cannot reach round the opening is in it.
  const out = new Uint8Array(w * h);
  for (let x = 0; x < w; x++) stack.push(x, (h - 1) * w + x);
  for (let y = 0; y < h; y++) stack.push(y * w, y * w + w - 1);
  while (stack.length) {
    const p = stack.pop();
    if (out[p] || inside[p]) continue;
    out[p] = 1;
    const x = p % w, y = (p / w) | 0;
    if (x > 0) stack.push(p - 1); if (x < w - 1) stack.push(p + 1); if (y > 0) stack.push(p - w); if (y < h - 1) stack.push(p + w);
  }
  for (let p = 0; p < w * h; p++) if (!out[p]) inside[p] = 1;
  // The glass's pale edge, half clear where it met the rim, goes with it.
  for (let pass = 0; pass < 2; pass++)
    for (const p of inside.map((v, p) => (v ? -1 : p)).filter((p) => p >= 0)) {
      const x = p % w, y = (p / w) | 0, i = p * 4;
      const near = (x > 0 && inside[p - 1]) || (x < w - 1 && inside[p + 1]) || (y > 0 && inside[p - w]) || (y < h - 1 && inside[p + w]);
      const luma = 0.3 * shell.px[i] + 0.59 * shell.px[i + 1] + 0.11 * shell.px[i + 2];
      if (near && shell.px[i + 3] > 0 && luma > 140 && Math.max(shell.px[i], shell.px[i + 1], shell.px[i + 2]) - Math.min(shell.px[i], shell.px[i + 1], shell.px[i + 2]) < 60) inside[p] = 2;
    }
  return inside;
}

function box(mask, w, h) {
  let x0 = w, y0 = h, x1 = -1, y1 = -1;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) if (mask(x, y)) { x0 = Math.min(x0, x); y0 = Math.min(y0, y); x1 = Math.max(x1, x); y1 = Math.max(y1, y); }
  return { x0, y0, x1, y1 };
}

const basic = load('hero_visor');
const from = box((x, y) => basic.px[at(basic, x, y) + 3] > 0, basic.w, basic.h);
let mean = [0, 0, 0, 0], n = 0;
for (let p = 0; p < basic.w * basic.h; p++) if (basic.px[p * 4 + 3] > 100) { for (let k = 0; k < 4; k++) mean[k] += basic.px[p * 4 + k]; n++; }
mean = mean.map((v) => v / n);

for (const helmet of process.argv.slice(2).length ? process.argv.slice(2) : RAW) {
  const shell = load(`hero_${helmet}_shell`), glints = load(`hero_${helmet}_visor`), { w, h } = shell;
  const hole = opening(shell, glints), to = box((x, y) => hole[y * w + x], w, h);
  const visor = { w, h, px: Buffer.alloc(w * h * 4) };
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const p = y * w + x;
    if (!hole[p]) continue;
    shell.px[p * 4 + 3] = 0;
    const u = (x - to.x0 + 0.5) / (to.x1 - to.x0 + 1), v = (y - to.y0 + 0.5) / (to.y1 - to.y0 + 1);
    const bx = Math.min(from.x1, from.x0 + Math.floor(u * (from.x1 - from.x0 + 1))), by = Math.min(from.y1, from.y0 + Math.floor(v * (from.y1 - from.y0 + 1)));
    const i = at(basic, bx, by), got = basic.px[i + 3] > 40;
    for (let k = 0; k < 4; k++) visor.px[p * 4 + k] = got ? basic.px[i + k] : mean[k];
  }
  fs.writeFileSync(path.join(hd, `hero_${helmet}_shell.png`), encode(w, h, shell.px));
  fs.writeFileSync(path.join(hd, `hero_${helmet}_visor.png`), encode(w, h, visor.px));
  console.log(helmet, 'opening', to, 'pixels', hole.reduce((s, v) => s + v, 0));
}

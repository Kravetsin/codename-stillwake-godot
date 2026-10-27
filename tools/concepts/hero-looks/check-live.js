'use strict';
// Runs the live demo without a browser and saves a sheet of its frames, to look at the chains:
//   node check-live.js <out.png> [style] [seconds] [every]
const path = require('path'), vm = require('vm'), fs = require('fs');
const P = require(path.join(__dirname, '..', '..', 'planet-art', 'png.js'));
const ROOT = path.join(__dirname, '..', '..', '..');
const [out, style = 'ponytail', seconds = '6', every = '12'] = process.argv.slice(2);
const PARTS = require(path.join(ROOT, 'tools', 'hero-art', 'parts.js')), HD = require('./hd.js');
const ctx = { PARTS, MAKE: require('./compose.js'), HD, PICTURES: {}, module: { exports: {} }, Math, Uint8ClampedArray, Map, Set, Object, Array, String, Number, JSON, Infinity };
vm.runInNewContext(fs.readFileSync(path.join(__dirname, 'page.js'), 'utf8') + '\n' + fs.readFileSync(path.join(__dirname, 'live.js'), 'utf8') + '\nmodule.exports.live = live;', ctx);
const page = ctx.module.exports;
for (const f of fs.readdirSync(path.join(ROOT, 'game', 'art'))) if (/^(hero_.*|back_scrap|floor)\.png$/.test(f)) page.IMG[f.slice(0, -4)] = P.decode(path.join(ROOT, 'game', 'art', f));
Object.assign(page.IMG, HD.pictures(PARTS));
const demo = page.live(true);
demo.restyle(style);
demo.opts.zoom = 2;
const frames = [];
for (let i = 0; i < +seconds * 60; i++) {
  const state = demo.tick(i / 60);
  if (i % +every === 0) {
    const img = demo.render(state), cw = 80 * 2, ch = 70 * 2;
    const cx = Math.max(0, Math.min(img.w - cw, Math.round(state.feet[0] * 2 - cw / 2)));
    frames.push(page.crop(img, cx, img.h - ch, cw, ch));
  }
}
const cols = 8, fw = frames[0].w, fh = frames[0].h, rows = Math.ceil(frames.length / cols);
const sheet = page.C.blank(fw * cols, fh * rows);
frames.forEach((f, i) => page.C.over(sheet, f, (i % cols) * fw, Math.floor(i / cols) * fh));
P.encode({ w: sheet.w, h: sheet.h, data: Buffer.from(sheet.data.buffer) }, out);

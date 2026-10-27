'use strict';
// Draws one of the page's scenes into a PNG, to look at the art without a browser:
//   node check.js <out.png> <scale> <looks json> [dark] [artDir]
const path = require('path'), vm = require('vm'), fs = require('fs');
const P = require(path.join(__dirname, '..', '..', 'planet-art', 'png.js'));
const ROOT = path.join(__dirname, '..', '..', '..');
const [out, k, json, mode, artArg] = process.argv.slice(2);
const art = artArg ? path.resolve(artArg) : path.join(ROOT, 'game', 'art');
const module_ = { exports: {} };
vm.runInNewContext(fs.readFileSync(path.join(__dirname, 'page.js'), 'utf8'), {
  PARTS: require(path.join(ROOT, 'tools', 'hero-art', 'parts.js')), MAKE: require('./compose.js'), PICTURES: {}, module: module_, Math, Uint8ClampedArray, Map, Object, Array, String, Number, JSON,
});
const page = module_.exports;
for (const f of fs.readdirSync(art)) if (/^hero_.*\.png$/.test(f)) page.IMG[f.slice(0, -4)] = P.decode(path.join(art, f));
for (const n of ['hero_helmet', 'hero_torso', 'hero_pack', 'hero_upperArm', 'hero_forearm', 'hero_thigh', 'hero_shin', 'back_scrap', 'floor'])
  page.IMG[n] = P.decode(path.join(ROOT, 'game', 'art', n + '.png'));
const img = page.C.scale(page.lineup(JSON.parse(json), { dark: mode === 'dark', cw: 36, h: 56 }), +k);
P.encode({ w: img.w, h: img.h, data: Buffer.from(img.data.buffer) }, out);

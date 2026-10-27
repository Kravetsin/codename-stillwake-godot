'use strict';
// Builds the page: node tools/concepts/hero-looks/build.js <out.html> [artDir]
// Inlines the hero's pictures (the new ones from artDir, game/art by default, as tools/hero-art/paint.js writes them),
// the deck's tiles, parts.js, compose.js and page.js into page.html.
const fs = require('fs'), path = require('path');
const ROOT = path.join(__dirname, '..', '..', '..');
const ART = path.join(ROOT, 'game', 'art');
const out = process.argv[2];
if (!out) throw new Error('usage: node build.js <out.html> [artDir]');
const art = process.argv[3] ? path.resolve(process.argv[3]) : ART;

const made = ['hero_head', 'hero_face_0', 'hero_face_1', 'hero_face_2', 'hero_hair_0', 'hero_hair_1', 'hero_hair_2', 'hero_hair_3',
  'hero_hair_4', 'hero_visor', 'hero_helmet_shell', ...['torso', 'upperArm', 'forearm', 'thigh', 'shin'].map(p => 'hero_jumpsuit_' + p)];
const game = ['hero_helmet', 'hero_torso', 'hero_pack', 'hero_upperArm', 'hero_forearm', 'hero_thigh', 'hero_shin', 'back_scrap', 'floor'];
const pictures = {};
for (const [dir, names] of [[art, made], [ART, game]])
  for (const n of names) pictures[n] = 'data:image/png;base64,' + fs.readFileSync(path.join(dir, n + '.png')).toString('base64');

const asModule = (file, name) => `const ${name} = (function () { const module = {}; ${fs.readFileSync(file, 'utf8')}\nreturn module.exports; })();`;
const code = [
  asModule(path.join(ROOT, 'tools', 'hero-art', 'parts.js'), 'PARTS'),
  asModule(path.join(__dirname, 'compose.js'), 'MAKE'),
  `const PICTURES = ${JSON.stringify(pictures)};`,
  fs.readFileSync(path.join(__dirname, 'page.js'), 'utf8'),
].join('\n');
const page = fs.readFileSync(path.join(__dirname, 'page.html'), 'utf8').replace('/*CODE*/', () => code);
fs.writeFileSync(out, page);
console.log(`${path.basename(out)} ${(page.length / 1024).toFixed(0)} KB`);

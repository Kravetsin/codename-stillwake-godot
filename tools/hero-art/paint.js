'use strict';
// Paints the hero without a suit and the face behind the visor into game/art (or <artDir>):
//   node tools/hero-art/paint.js [artDir]
// Reads hero_helmet.png from game/art to cut its visor out. Fails if a head picture leaves the helmet's outline
// or two ramps share a colour, either of which would break the shade swap or show the head through the helmet.
const fs = require('fs'), path = require('path');
const P = require(path.join(__dirname, '..', 'planet-art', 'png.js'));
const A = require('./parts.js');

const ROOT = path.join(__dirname, '..', '..');
const ART = path.join(ROOT, 'game', 'art');
const SIZES = { head: [13, 12], torso: [12, 15], upperArm: [5, 9], forearm: [5, 10], thigh: [6, 12], shin: [9, 11] };

const rgba = s => { const [hex, a] = s.split('/'); const n = parseInt(hex.slice(1), 16); return [n >> 16, (n >> 8) & 255, n & 255, a ? +a : 255]; };
const keys = { ...A.FIXED };
'lsdo'.split('').forEach((k, i) => (keys[k] = A.SUIT_DRAWN[i]));
'LSDO'.split('').forEach((k, i) => (keys[k] = A.SKINS[A.SKIN_DRAWN][i]));
'1234'.split('').forEach((k, i) => (keys[k] = A.HAIRS[A.HAIR_DRAWN][i]));

function picture(rows, [w, h], name) {
  if (rows.length > h || rows.some(r => r.length > w)) throw new Error(`${name}: larger than ${w}x${h}`);
  const img = P.create(w, h);
  rows.forEach((row, y) => [...row].forEach((k, x) => {
    if (k === '.') return;
    if (!(k in keys)) throw new Error(`${name}: no colour for '${k}'`);
    img.data.set(rgba(keys[k]), (y * w + x) * 4);
  }));
  return img;
}

// Every ramp's shades must be unlike each other's and the fixed colours', or a swap would catch the wrong pixel.
const seen = new Map();
const claim = (c, by) => { const k = c.slice(0, 7).toLowerCase(); if (seen.has(k) && seen.get(k) !== by) throw new Error(`${k} in both ${seen.get(k)} and ${by}`); seen.set(k, by); };
Object.entries(A.FIXED).forEach(([k, c]) => claim(c, 'fixed ' + k));
A.SKINS.forEach((r, i) => r.forEach(c => claim(c, 'skin ' + i)));
A.HAIRS.forEach((r, i) => r.forEach(c => claim(c, 'hair ' + i)));
A.SUIT_DRAWN.forEach(c => claim(c, 'suit'));

const out = process.argv[2] ? path.resolve(process.argv[2]) : ART;
const helmet = P.decode(path.join(ART, 'hero_helmet.png'));
const inHelmet = (x, y) => helmet.data[(y * helmet.w + x) * 4 + 3] > 0;
const made = {}, outside = [];
const head = (name, rows) => {
  const img = picture(rows, SIZES.head, name);
  for (let y = 0; y < img.h; y++) for (let x = 0; x < img.w; x++)
    if (img.data[(y * img.w + x) * 4 + 3] && !inHelmet(x, y)) outside.push(`${name} (${x}, ${y})`);
  made[name] = img;
};
head('hero_head', A.head);
A.faces.forEach((f, i) => head(`hero_face_${i}`, f));
A.hairs.forEach((f, i) => head(`hero_hair_${i}`, f));
made.hero_visor = picture(A.visor, SIZES.head, 'hero_visor');
const shell = { w: helmet.w, h: helmet.h, data: Buffer.from(helmet.data) };
for (let i = 0; i < shell.w * shell.h; i++) if (made.hero_visor.data[i * 4 + 3]) shell.data.fill(0, i * 4, i * 4 + 4);
made.hero_helmet_shell = shell;
for (const [part, rows] of Object.entries(A.jumpsuit)) made[`hero_jumpsuit_${part}`] = picture(rows, SIZES[part], part);

fs.mkdirSync(out, { recursive: true });
for (const [name, img] of Object.entries(made)) P.encode(img, path.join(out, name + '.png'));
console.log(`${Object.keys(made).length} pictures into ${path.relative(ROOT, out) || '.'}`);
if (outside.length) { console.error('outside the helmet: ' + outside.join(', ')); process.exitCode = 1; }

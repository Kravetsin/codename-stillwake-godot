'use strict';
// Renders every style in styles/: a lineup on the deck at game scale and close-ups of the heads; with no style named,
// also the strip of all styles, 1x against 2x and the catalog of the chosen head, which build.js inlines.
//   node render.js <outDir> [styleId]
const path = require('path'), fs = require('fs');
const ROOT = path.join(__dirname, '..', '..', '..');
const P = require(ROOT + '/tools/planet-art/png.js');
const PARTS = require(ROOT + '/tools/hero-art/parts.js');
const make = require(ROOT + '/tools/concepts/hero-looks/compose.js');
const ART = ROOT + '/game/art';

const out = path.resolve(process.argv[2] || 'out');
fs.mkdirSync(out, { recursive: true });
const only = process.argv[3];

const game = {};
for (const n of ['hero_helmet', 'hero_helmet_shell', 'hero_visor', 'hero_torso', 'hero_pack', 'hero_upperArm', 'hero_forearm', 'hero_thigh', 'hero_shin', 'back_scrap', 'floor'])
  game[n] = P.decode(path.join(ART, n + '.png'));
const toImg = p => ({ w: p.w, h: p.h, data: new Uint8ClampedArray(p.data) });
for (const k in game) game[k] = toImg(game[k]);

const hex = s => {
  const [c, a] = s.split('/');
  return [parseInt(c.slice(1, 3), 16), parseInt(c.slice(3, 5), 16), parseInt(c.slice(5, 7), 16), a ? +a : 255];
};

/** A style's palette: its drawn ramps' keys and its fixed colours. */
function paletteOf(S) {
  const pal = {};
  'lsdo'.split('').forEach((k, i) => (pal[k] = hex(S.SUIT_DRAWN[i])));
  'LSDO'.split('').forEach((k, i) => (pal[k] = hex(S.SKINS[S.SKIN_DRAWN][i])));
  '1234'.split('').forEach((k, i) => (pal[k] = hex(S.HAIRS[S.HAIR_DRAWN][i])));
  for (const [k, v] of Object.entries({ ...PARTS.FIXED, ...(S.fixed || {}) })) pal[k] = hex(v);
  return pal;
}

function picture(rows, pal, joint) {
  const h = rows.length, w = Math.max(...rows.map(r => r.length));
  const img = { w, h, data: new Uint8ClampedArray(w * h * 4) };
  rows.forEach((r, y) => [...r].forEach((k, x) => {
    if (k === '.' || k === ' ') return;
    if (!pal[k]) throw new Error(`no colour for '${k}' at ${x},${y}`);
    img.data.set(pal[k], (y * w + x) * 4);
  }));
  if (joint) img.joint = joint;
  return img;
}

function styleOf(file) {
  const s = require(file);
  const S = { SUIT_DRAWN: PARTS.SUIT_DRAWN, SUITS: PARTS.SUITS, SKINS: PARTS.SKINS, SKIN_DRAWN: PARTS.SKIN_DRAWN, HAIRS: PARTS.HAIRS, HAIR_DRAWN: PARTS.HAIR_DRAWN, ...s };
  const pal = paletteOf(S);
  const pics = {};
  const j = S.headJoint || PARTS.HEAD_JOINT;
  // A style may draw several head shapes (S.heads), picked by look.head; otherwise its one S.head.
  // density 2: the head's layers drawn at half pixels, under compose's names for them (hd_*).
  const P = S.density === 2 ? 'hd_' : 'hero_';
  (S.heads || [S.head]).forEach((h, i) => (pics[P + 'head_' + i] = picture(h, pal, j)));
  S.faces.forEach((f, i) => (pics[P + 'face_' + i] = picture(f, pal, j)));
  // A hair may be { rows, at: [dx, dy] }: its top left from the head's, for one wider or longer than the head.
  S.hairs.forEach((f, i) => (pics[P + 'hair_' + i] = f.rows ? picture(f.rows, pal, [j[0] - f.at[0], j[1] - f.at[1]]) : picture(f, pal, j)));
  (S.beards || []).forEach((f, i) => (pics[P + 'beard_' + i] = picture(f.rows, pal, [j[0] - f.at[0], j[1] - f.at[1]])));
  if (S.hairsBack) S.hairsBack.forEach((f, i) => (pics['hero_hairback_' + i] = f && picture(f, pal, j)));
  for (const [k, rows] of Object.entries(S.jumpsuit)) pics['hero_jumpsuit_' + k] = picture(rows, pal);
  for (const [k, rows] of Object.entries(S.extra || {})) pics[k] = picture(rows, pal);
  S.get = n => pics[n] || game[n] || (() => { throw new Error('no picture ' + n); })();
  S.C = make(S);
  return S;
}

function deck(C, w, h, floorY) {
  const img = C.blank(w, h, 0x000000);
  const back = game.back_scrap, floor = game.floor;
  for (let y = 0; y < floorY; y += back.h) for (let x = 0; x < w; x += back.w) C.over(img, back, x, y, () => [0.55, 0.55, 0.6]);
  for (let x = 0; x < w; x += floor.w) C.over(img, floor, x, floorY);
  return img;
}

/** The figure's ops for a look in a style: the reference (suited, today's art) or the style's bare hero. */
function opsOf(S, look) {
  const C = S.C;
  const none = { w: 1, h: 1, data: new Uint8ClampedArray(4), joint: [0, 0] };
  // Under a helmet the hair is hidden: it is bigger than the helmet.
  const get = n => (n === 'hero_head' || n === 'hd_head' ? S.get(n + '_' + (look.head || 0)) : (look.visor || look.bald) && /_hair_/.test(n) ? none : S.get(n));
  // visor: suited, the face seen through the helmet's glass
  const ops = C.figure({ ...look, fine: S.density === 2, suited: !!(look.suited || look.visor), closed: !!look.suited && !look.visor }, get);
  // Hair behind the head (a ponytail, long hair), under the torso's front: put it first among the head's layers.
  if (!look.suited && S.hairsBack && S.hairsBack[look.hair || 0]) {
    const img = C.recolour(S.get('hero_hairback_' + (look.hair || 0)), swaps(S, look));
    const iHead = ops.findIndex(o => o.face);
    const ref = ops[iHead];
    ops.splice(ops.findIndex(o => o.joint === C.JOINTS.torso), 0, { ...ref, img, face: false, head: true, joint: S.get('hero_hairback_' + (look.hair || 0)).joint });
  }
  return ops;
}
function swaps(S, look) {
  const l = { suit: 0, skin: S.SKIN_DRAWN, hairColour: S.HAIR_DRAWN, ...look };
  const C = S.C;
  return [[S.SUIT_DRAWN, C.shadesOf(S.SUITS[l.suit])], [S.SKINS[S.SKIN_DRAWN], S.SKINS[l.skin]], [S.HAIRS[S.HAIR_DRAWN], S.HAIRS[l.hairColour]]];
}

// Drawn at zoom z straight away, so a head drawn at half pixels keeps them.
function lineup(S, looks, z = 1, cw = 30, h = 58) {
  const C = S.C, w = cw * looks.length, floorY = h - 5;
  const img = z === 1 ? deck(C, w, h, floorY) : C.scale(deck(C, w, h, floorY), z);
  looks.forEach((look, i) => {
    const feet = [Math.round(i * cw + cw / 2) * z, floorY * z];
    C.draw(img, opsOf(S, look), feet, null, { zoom: z });
  });
  return img;
}

/** Heads alone, larger, on a plain ground: what the character editor would show. */
function heads(S, looks, z = 10, cw = 24, h = 27) {
  const C = S.C, img = C.blank(cw * looks.length * z, h * z, 0x20232b);
  looks.forEach((look, i) => {
    const ops = opsOf(S, look).filter(o => o.face || o.head || o.joint === C.JOINTS.helmet);
    C.draw(img, ops.map(o => ({ ...o, at: [o.at[0], o.at[1] + S.C.Neck] })), [Math.round(i * cw + cw / 2) * z, (h - 3) * z], null, { zoom: z });
  });
  return img;
}

const save = (img, k, file) => {
  const s = k === 1 ? img : make({ ...PARTS }).scale(img, k);
  P.encode({ w: s.w, h: s.h, data: Buffer.from(s.data.buffer) }, file);
};

const dir = path.join(__dirname, 'styles');
// Every style's examples on one deck, in order, a suited hero first for scale.
if (!only) {
  const all = fs.readdirSync(dir).filter(f => /^[a-e]_.*.js$/.test(f)).sort().map(f => styleOf(path.join(dir, f)));
  const cast = [[all[0], { suited: true }], ...all.flatMap(S => S.examples.map(l => [S, l]))];
  const cw = 24, h = 56, floorY = h - 5, img = all[0].C.scale(deck(all[0].C, cw * cast.length, h, floorY), 3);
  cast.forEach(([S, l], i) => S.C.draw(img, opsOf(S, l), [Math.round(i * cw + cw / 2) * 3, floorY * 3], null, { zoom: 3 }));
  save(img, 1, path.join(out, 'all_x3.png'));
}
for (const f of fs.readdirSync(dir).filter(f => f.endsWith('.js')).sort()) {
  const id = f.slice(0, -3);
  if (only && id !== only) continue;
  const S = styleOf(path.join(dir, f));
  const looks = [{ suited: true, suit: S.examples[0].suit || 0 }, ...S.examples];
  save(lineup(S, looks), 1, path.join(out, id + '_x1.png'));
  save(lineup(S, looks, 6), 1, path.join(out, id + '_line.png'));
  save(heads(S, S.examples), 1, path.join(out, id + '_heads.png'));
  console.log('drew', id);
}

// One picture over another, and a column range of one, on the page's ground.
const read = f => P.decode(path.join(out, f + '.png'));
function stack(a, b, file) {
  const im = P.create(Math.max(a.w, b.w), a.h + b.h + 12);
  for (let i = 0; i < im.w * im.h; i++) im.data.set([17, 20, 26, 255], i * 4);
  [[a, 0], [b, a.h + 12]].forEach(([m, y0]) => { for (let y = 0; y < m.h; y++) m.data.copy(im.data, (y + y0) * im.w * 4, y * m.w * 4, (y + 1) * m.w * 4); });
  P.encode(im, path.join(out, file + '.png'));
}
function crop(a, x, y, w, h, file) {
  const im = P.create(w, h);
  for (let j = 0; j < h; j++) a.data.copy(im.data, j * w * 4, ((j + y) * a.w + x) * 4, ((j + y) * a.w + x + w) * 4);
  P.encode(im, path.join(out, file + '.png'));
}
if (!only) {
  stack(read('f_one1x_line'), read('g_one2x_line'), 'fg_line');
  stack(read('f_one1x_heads'), read('g_one2x_heads'), 'fg_heads');
  // the catalog's heads, 240 px each: the hairs, then the faces and beards, then the skins
  const cat = read('h_catalog_heads'), F = require('./styles/f_one1x.js');
  crop(cat, 0, 40, F.hairs.length * 240, 230, 'cat_hairs');
  const faces = F.faces.length + F.beards.length;
  crop(cat, F.hairs.length * 240, 40, faces * 240, 230, 'cat_faces');
  crop(cat, (F.hairs.length + faces) * 240, 40, 7 * 240, 230, 'cat_colours');
}

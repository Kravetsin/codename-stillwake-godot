// Writes the harpoon concepts: icon_<id>.png, held_<id>.png (empty), held_<id>_loaded.png, rod_<id>.png,
// scene_<id>.png, harpoon.json and concepts.html, all into this folder.
const fs = require('fs'), path = require('path');
const P = require('../weapons/png.js'), POSE = require('../weapons/pose.js'), B = require('./build.js');
// [outDir] (default ./out, ignored by git): where the candidates, the json and the concepts page go.
const DIR = process.argv[2] || path.join(__dirname, 'out'), ART = path.join(__dirname, '..', '..', '..', 'game', 'art') + '/';
fs.mkdirSync(DIR, { recursive: true });
const ids = Object.keys(B);
const DEBRIS = { pneumatic: 'crate_03', spring: 'pod_03', winch: 'hull_12' };

// ---------------------------------------------------------------- files
for (const id of ids) {
  const d = B[id];
  P.write(path.join(DIR, `icon_${id}.png`), d.icon);
  P.write(path.join(DIR, `held_${id}.png`), d.heldIm);
  P.write(path.join(DIR, `held_${id}_loaded.png`), d.loadedIm);
  P.write(path.join(DIR, `rod_${id}.png`), d.rodIm);
}

// ---------------------------------------------------------------- hero parts (as ../weapons/gen.js)
const SUIT = 0xe0a030, FAR = 0.7;
const shade = (c, by) => [16, 8, 0].map(s => { const v = (c >> s) & 255; return Math.round(by >= 0 ? v * by : v + (255 - v) * -by); });
const shadesOf = c => [shade(c, -0.35), shade(c, 1), shade(c, 0.72), shade(c, 0.42)];
const farOf = shade(SUIT, FAR), FROM = shadesOf(SUIT), TO = shadesOf((farOf[0] << 16) | (farOf[1] << 8) | farOf[2]);
const PARTS = {};
for (const p of ['helmet', 'torso', 'pack', 'upperArm', 'forearm', 'thigh', 'shin']) {
  const im = P.read(ART + `hero_${p}.png`); PARTS[p] = im;
  const far = { w: im.w, h: im.h, data: Buffer.from(im.data) };
  for (let i = 0; i < far.data.length; i += 4) {
    const k = FROM.findIndex(c => c[0] === far.data[i] && c[1] === far.data[i + 1] && c[2] === far.data[i + 2]);
    if (far.data[i + 3] && k >= 0) [far.data[i], far.data[i + 1], far.data[i + 2]] = TO[k];
  }
  PARTS[p + 'Far'] = far;
}
const FLOOR = P.read(ART + 'floor.png');

// ---------------------------------------------------------------- drawing
function canvas(w, h, bg) {
  const c = { w, h, data: Buffer.alloc(w * h * 4) };
  for (let i = 0; i < w * h; i++) c.data.set([...bg, 255], i * 4);
  return c;
}
function blend(c, x, y, rgb, a) {
  x = Math.floor(x); y = Math.floor(y);
  if (x < 0 || y < 0 || x >= c.w || y >= c.h) return;
  const o = (y * c.w + x) * 4;
  for (let k = 0; k < 3; k++) c.data[o + k] = Math.round(c.data[o + k] * (1 - a) + rgb[k] * a);
}
// a sprite turned about its joint, sampled nearest at screen resolution, as the game's sprites look zoomed in
function drawPart(c, im, joint, at, deg, ox, oy, s) {
  const r = deg * Math.PI / 180, cs = Math.cos(r), sn = Math.sin(r);
  const corners = [[0, 0], [im.w, 0], [0, im.h], [im.w, im.h]].map(([x, y]) => {
    const dx = x - joint[0], dy = y - joint[1];
    return [ox + (at[0] + dx * cs - dy * sn) * s, oy + (at[1] + dx * sn + dy * cs) * s];
  });
  const x0 = Math.floor(Math.min(...corners.map(p => p[0]))), x1 = Math.ceil(Math.max(...corners.map(p => p[0])));
  const y0 = Math.floor(Math.min(...corners.map(p => p[1]))), y1 = Math.ceil(Math.max(...corners.map(p => p[1])));
  for (let py = y0; py <= y1; py++) for (let px = x0; px <= x1; px++) {
    const wx = (px + 0.5 - ox) / s - at[0], wy = (py + 0.5 - oy) / s - at[1];
    const lx = Math.floor(wx * cs + wy * sn + joint[0]), ly = Math.floor(-wx * sn + wy * cs + joint[1]);
    if (lx < 0 || ly < 0 || lx >= im.w || ly >= im.h) continue;
    const i = (ly * im.w + lx) * 4;
    if (im.data[i + 3]) blend(c, px, py, [im.data[i], im.data[i + 1], im.data[i + 2]], im.data[i + 3] / 255);
  }
}
const hex = c => [parseInt(c.slice(1, 3), 16), parseInt(c.slice(3, 5), 16), parseInt(c.slice(5, 7), 16)];
const ROPE = hex('#ecc67e'), ROPE_D = hex('#7c5a2e'), OUT = hex('#14171e');

// the rope: a slack curve from the muzzle to the rod's eye, dashed every few world pixels, one pixel thick
function rope(c, a, b, sag, ox, oy, s) {
  const mid = [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2 + sag];
  const at = t => [(1 - t) ** 2 * a[0] + 2 * (1 - t) * t * mid[0] + t * t * b[0], (1 - t) ** 2 * a[1] + 2 * (1 - t) * t * mid[1] + t * t * b[1]];
  const n = 400, pts = [];
  let len = 0, prev = at(0);
  for (let i = 0; i <= n; i++) { const p = at(i / n); len += Math.hypot(p[0] - prev[0], p[1] - prev[1]); prev = p; pts.push([p, len]); }
  for (const [p, l] of pts) {
    if (l % 4 >= 2.4) continue;
    const X = ox + p[0] * s, Y = oy + p[1] * s, r = s * 0.5;
    for (let dy = -r - 1; dy <= r + 1; dy++) for (let dx = -r - 1; dx <= r + 1; dx++) {
      const d = Math.hypot(dx, dy);
      if (d <= r) blend(c, X + dx, Y + dy, ROPE, 1);
    }
  }
}

function stars(c, seed) {
  let x = seed;
  const rnd = () => (x = (x * 1103515245 + 12345) & 0x7fffffff) / 0x7fffffff;
  for (let i = 0; i < c.w * c.h / 1400; i++) {
    const px = rnd() * c.w | 0, py = rnd() * c.h | 0, b = 0.25 + rnd() * 0.5;
    blend(c, px, py, [200, 215, 235], b); if (rnd() < 0.15) blend(c, px + 1, py, [200, 215, 235], b * 0.6);
  }
}

// ---------------------------------------------------------------- scene
const AIM = -11, REACH = 58, SC = 4;
function scene(id) {
  const d = B[id], im = d.loadedIm, debris = P.read(ART + `debris/${DEBRIS[id]}.png`);
  const look = { grip: d.grip, hold: 0, edge: [d.muzzle, d.reelAt] };
  const sc = POSE.scene(look, { upper: AIM + 4, fore: AIM });
  const [muzzle] = sc.edges, dir = [Math.cos(AIM * Math.PI / 180), Math.sin(AIM * Math.PI / 180)];
  // the rod flew on along the aim and sank a third of its length into the wreck
  const rodLen = d.rodIm.w - 2, tip = [muzzle[0] + dir[0] * REACH, muzzle[1] + dir[1] * REACH];
  const eye = [tip[0] - dir[0] * (d.tip[0] - d.eye[0]), tip[1] - dir[1] * (d.tip[0] - d.eye[0])];
  const bury = rodLen * 0.3, centre = [tip[0] + dir[0] * (debris.w / 2 - bury), tip[1] + dir[1] * (debris.w / 2 - bury)];
  const x0 = -18, y0 = Math.floor(Math.min(-50, centre[1] - debris.h / 2 - 8)), x1 = Math.ceil(centre[0] + debris.w / 2 + 8), y1 = 8;
  const c = canvas((x1 - x0) * SC, (y1 - y0) * SC, [0x07, 0x0a, 0x10]);
  stars(c, id.length * 7919);
  const X = -x0 * SC, Y = -y0 * SC;
  // the deck the hero stands on, ending a little ahead of the feet: the ship's edge
  for (let tx = x0 - 16; tx < 14; tx += 16) drawPart(c, FLOOR, [0, 0], [tx, 0], 0, X, Y, SC);
  rope(c, muzzle, eye, 3, X, Y, SC);
  drawPart(c, d.rodIm, [d.eye[0], d.eye[1]], eye, AIM, X, Y, SC);
  drawPart(c, debris, [debris.w / 2, debris.h / 2], centre, -6, X, Y, SC);
  for (const p of sc.list) {
    const pic = p.part === 'weapon' ? d.heldIm : PARTS[p.part + (p.far ? 'Far' : '')];
    drawPart(c, pic, p.joint || POSE.JOINT[p.part], p.at, p.rot, X, Y, SC);
  }
  return c;
}
for (const id of ids) P.write(path.join(DIR, `scene_${id}.png`), scene(id));

// ---------------------------------------------------------------- data
const r2 = p => p.map(v => Math.round(v * 100) / 100);
const data = ids.map((id, n) => {
  const d = B[id], sh = d.loadedShift, ls = p => r2([p[0] + sh[0], p[1] + sh[1]]);
  return {
    n: n + 1, id, name: d.name, en: d.en, about: d.about, rodAbout: d.rodAbout, debris: DEBRIS[id],
    held: { file: `held_${id}.png`, w: d.heldIm.w, h: d.heldIm.h, grip: r2(d.grip), muzzle: r2(d.muzzle), reel: r2(d.reelAt) },
    loaded: { file: `held_${id}_loaded.png`, w: d.loadedIm.w, h: d.loadedIm.h, grip: ls(d.grip), muzzle: ls(d.muzzle), reel: ls(d.reelAt) },
    rod: { file: `rod_${id}.png`, w: d.rodIm.w, h: d.rodIm.h, eye: r2(d.eye), tip: r2(d.tip) },
    icon: { file: `icon_${id}.png`, w: 16, h: 16 },
    hold: 0,
  };
});
fs.writeFileSync(path.join(DIR, 'harpoon.json'), JSON.stringify({
  about: 'Stillwake harpoon gun, three concept directions. Held sprites: the barrel along +x, the muzzle at the far end, the pistol grip below (+y). Rods: tip along +x, the rope eye at the tail.',
  units: {
    grip: 'pixels of the held PNG from its top-left corner, on pixel edges (7.5 is the middle of pixel 7), as HeldLook.Grip',
    hold: 'radians from the forearm to the barrel, as HeldLook.Hold: 0, the barrel carries the forearm\u2019s line, as a pistol is held',
    muzzle: 'where the rod leaves, same pixels as grip', reel: 'where the rope winds on, same pixels',
    eye: 'the middle of the rod\u2019s eye, in pixels of the rod PNG: where the rope ties on', tip: 'the rod\u2019s point',
    loaded: 'the gun with the rod seated; the same grip, the muzzle and reel shifted by any growth to the left or up (none here)',
  },
  directions: data,
}, null, 2));

// ---------------------------------------------------------------- page
const uri = f => 'data:image/png;base64,' + fs.readFileSync(path.join(DIR, f)).toString('base64');
const IMG = {};
for (const id of ids) for (const f of [`icon_${id}`, `held_${id}`, `held_${id}_loaded`, `rod_${id}`, `scene_${id}`]) IMG[f] = uri(f + '.png');
// A tool has no icon drawn (2026-10-27-tool-icons): the axe's slot shows its held picture.
for (const [f, file] of [['fireAxe', 'hero_axe'], ['wire', 'items/wire'], ['plate', 'items/plate'], ['scrap', 'items/scrap']]) IMG['item_' + f] = 'data:image/png;base64,' + fs.readFileSync(ART + file + '.png').toString('base64');
IMG.hero_axe = 'data:image/png;base64,' + fs.readFileSync(ART + 'hero_axe.png').toString('base64');
const sizes = {};
for (const id of ids) { const s = P.read(path.join(DIR, `scene_${id}.png`)); sizes[id] = [s.w / SC, s.h / SC]; }
const page = fs.readFileSync(path.join(__dirname, 'page.html'), 'utf8')
  .replace('/*DATA*/', () => `var IMG = ${JSON.stringify(IMG)};\nvar DIRS = ${JSON.stringify(data)};\nvar SCENE = ${JSON.stringify(sizes)};\nvar AXE = { w: 28, h: 14, grip: [7.5, 7] };`);
fs.writeFileSync(path.join(DIR, 'concepts.html'), page);
console.log(JSON.stringify(data.map(d => ({ id: d.id, held: d.held, loaded: d.loaded, rod: d.rod })), null, 1));

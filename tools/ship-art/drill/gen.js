// Writes the drill concepts: icon_<id>.png, held_<id>.png (at rest), held_<id>_spin<k>.png, scene_<id>.png,
// drill.json and concepts.html, all into this folder. The pipeline is ../harpoon-art/gen.js's.
const fs = require('fs'), path = require('path');
const P = require('../weapons/png.js'), POSE = require('../weapons/pose.js'), B = require('./build.js');
// [outDir] (default ./out, ignored by git): where the candidates, the json and the concepts page go.
const DIR = process.argv[2] || path.join(__dirname, 'out'), ART = path.join(__dirname, '..', '..', '..', 'game', 'art') + '/';
fs.mkdirSync(DIR, { recursive: true });
const ids = Object.keys(B);
const ROCK = { frame: 'asteroid_01', tube: 'asteroid_03', crank: 'asteroid_02' };

// ---------------------------------------------------------------- files
for (const id of ids) {
  const d = B[id];
  P.write(path.join(DIR, `icon_${id}.png`), d.icon);
  P.write(path.join(DIR, `held_${id}.png`), d.heldIm);
  d.spinIms.forEach((im, i) => P.write(path.join(DIR, `held_${id}_spin${i + 1}.png`), im));
}

// ---------------------------------------------------------------- hero parts (as ../harpoon-art/gen.js)
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
const JETPACK = P.read(ART + 'hero_jetpack.png');

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
function add(c, x, y, rgb, a) {
  x = Math.floor(x); y = Math.floor(y);
  if (x < 0 || y < 0 || x >= c.w || y >= c.h) return;
  const o = (y * c.w + x) * 4;
  for (let k = 0; k < 3; k++) c.data[o + k] = Math.min(255, Math.round(c.data[o + k] + rgb[k] * a));
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
const CABLE = hex('#b8382b'), CABLE_HI = hex('#e25a46'), SPARK = hex('#fff3c4'), GLOW = hex('#ffa040');

// a world-pixel square at a world point, as the game draws a particle
function dot(c, p, rgb, a, ox, oy, s) {
  const X = Math.floor(ox + p[0] * s), Y = Math.floor(oy + p[1] * s);
  for (let dy = 0; dy < s; dy++) for (let dx = 0; dx < s; dx++) blend(c, X + dx, Y + dy, rgb, a);
}
function glow(c, p, rgb, radius, a, ox, oy, s) {
  const X = ox + p[0] * s, Y = oy + p[1] * s, R = radius * s;
  for (let y = Math.floor(Y - R); y <= Y + R; y++) for (let x = Math.floor(X - R); x <= X + R; x++) {
    const d = Math.hypot(x + 0.5 - X, y + 0.5 - Y) / R;
    if (d < 1) add(c, x, y, rgb, a * (1 - d) * (1 - d));
  }
}
// the cable from the drill's plug to the belt: a slack curve, one world pixel thick, its top lit
function cable(c, a, b, sag, ox, oy, s) {
  const mid = [(a[0] + b[0]) / 2, Math.max(a[1], b[1]) + sag];
  const at = t => [(1 - t) ** 2 * a[0] + 2 * (1 - t) * t * mid[0] + t * t * b[0], (1 - t) ** 2 * a[1] + 2 * (1 - t) * t * mid[1] + t * t * b[1]];
  for (let i = 0; i <= 300; i++) {
    const p = at(i / 300), X = ox + p[0] * s, Y = oy + p[1] * s, r = s * 0.5;
    for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++) blend(c, X + dx, Y + dy, dy < -r / 3 ? CABLE_HI : CABLE, 1);
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
const AIM = -9, SC = 4;
function scene(id) {
  const d = B[id], im = d.spinIms[0], rock = P.read(ART + `debris/${ROCK[id]}.png`);
  const look = { grip: d.grip, hold: 0, edge: [d.tip, d.chuck, d.plug] };
  const sc = POSE.scene(look, { upper: AIM + 4, fore: AIM });
  const [tip, , plug] = sc.edges, dir = [Math.cos(AIM * Math.PI / 180), Math.sin(AIM * Math.PI / 180)];
  // the rock's edge along the aim from its middle, so the bit goes a pixel and a half in
  const opaque = (x, y) => { x = Math.floor(x); y = Math.floor(y); return x >= 0 && y >= 0 && x < rock.w && y < rock.h && rock.data[(y * rock.w + x) * 4 + 3] > 0; };
  let edge = 0;
  while (opaque(rock.w / 2 - dir[0] * edge, rock.h / 2 - dir[1] * edge)) edge += 0.25;
  const centre = [tip[0] + dir[0] * (edge - 1.5), tip[1] + dir[1] * (edge - 1.5)];
  const x0 = -16, y0 = Math.floor(Math.min(-48, centre[1] - rock.h / 2 - 6)), x1 = Math.ceil(centre[0] + rock.w / 2 + 6);
  const y1 = Math.ceil(Math.max(4, centre[1] + rock.h / 2 + 6));
  const c = canvas((x1 - x0) * SC, (y1 - y0) * SC, [0x07, 0x0a, 0x10]);
  stars(c, id.length * 7919);
  const X = -x0 * SC, Y = -y0 * SC;
  drawPart(c, rock, [rock.w / 2, rock.h / 2], centre, 0, X, Y, SC);
  // a hero adrift in the band, jetpack on: no deck under it
  for (const p of sc.list) {
    if (p.part === 'weapon') {
      // the belt is where the battery hangs: the cable runs from it to the drill's plug, under the drill
      const neck = -POSE.RIG.neck;
      cable(c, plug, [2.5, neck + 8.5], 5, X, Y, SC);
      drawPart(c, im, p.joint, p.at, p.rot, X, Y, SC);
      continue;
    }
    drawPart(c, PARTS[p.part + (p.far ? 'Far' : '')], p.joint || POSE.JOINT[p.part], p.at, p.rot, X, Y, SC);
    if (p.part === 'pack') drawPart(c, JETPACK, [0, 0], [p.at[0] - 2, p.at[1] - 1], 0, X, Y, SC);
  }
  // sparks off the bit, thrown back toward the hero, as the game's spark: a world pixel, cream, an orange glow
  let seed = 1013 + id.length * 31;
  const rnd = () => (seed = (seed * 1103515245 + 12345) & 0x7fffffff) / 0x7fffffff;
  glow(c, tip, GLOW, 7, 0.55, X, Y, SC);
  const back = Math.atan2(-dir[1], -dir[0]);
  for (let i = 0; i < 13; i++) {
    const a = back + (rnd() - 0.5) * 2.6, r = 1.5 + rnd() * rnd() * 13, fall = r * r * 0.012;
    const p = [tip[0] + Math.cos(a) * r, tip[1] + Math.sin(a) * r + fall];
    glow(c, p, GLOW, 2.2, 0.35, X, Y, SC);
    if (r > 4) for (let t = 1; t <= 2; t++) dot(c, [p[0] - Math.cos(a) * t * 0.9, p[1] - Math.sin(a) * t * 0.9], GLOW, 0.45 / t, X, Y, SC);
    dot(c, p, SPARK, 1, X, Y, SC);
  }
  // a few chips of rock
  for (let i = 0; i < 4; i++) {
    const a = back + (rnd() - 0.5) * 2, r = 3 + rnd() * 8;
    dot(c, [tip[0] + Math.cos(a) * r, tip[1] + Math.sin(a) * r], [0x7d, 0x78, 0x6e], 1, X, Y, SC);
  }
  dot(c, [tip[0] - 0.5, tip[1] - 0.5], SPARK, 1, X, Y, SC);
  return { c, size: [x1 - x0, y1 - y0] };
}
const SIZES = {};
for (const id of ids) { const s = scene(id); P.write(path.join(DIR, `scene_${id}.png`), s.c); SIZES[id] = s.size; }

// ---------------------------------------------------------------- data
const r2 = p => p.map(v => Math.round(v * 100) / 100);
const data = ids.map((id, n) => {
  const d = B[id];
  return {
    n: n + 1, id, name: d.name, en: d.en, about: d.about, rock: ROCK[id],
    held: { file: `held_${id}.png`, w: d.heldIm.w, h: d.heldIm.h, grip: r2(d.grip), chuck: r2(d.chuck), tip: r2(d.tip), plug: r2(d.plug) },
    spin: [`held_${id}.png`, ...d.spinIms.map((_, i) => `held_${id}_spin${i + 1}.png`)],
    icon: { file: `icon_${id}.png`, w: 16, h: 16 },
    hold: 0,
  };
});
fs.writeFileSync(path.join(DIR, 'drill.json'), JSON.stringify({
  about: 'Stillwake drill, three concept directions. Held sprites: the bit along +x, its point at the far end, the pistol grip below (+y). Every spin frame is the same size as the rest picture and swaps in place.',
  units: {
    grip: 'pixels of the held PNG from its top-left corner, on pixel edges (7.5 is the middle of pixel 7), as HeldLook.Grip',
    hold: 'radians from the forearm to the bit, as HeldLook.Hold: 0, the bit carries the forearm\u2019s line, as a pistol is held',
    chuck: 'where the bit leaves the chuck: HeldLook.EdgeFrom', tip: 'the bit\u2019s point: HeldLook.EdgeTo',
    plug: 'where the cable to the belt leaves the drill, same pixels; the scene draws the cable from here to the belt',
    spin: 'frames while it runs, the rest picture first; the game today alternates the rest picture with one Spin picture',
  },
  directions: data,
}, null, 2));

// ---------------------------------------------------------------- page
const b64 = f => 'data:image/png;base64,' + fs.readFileSync(f).toString('base64');
const IMG = {};
for (const d of data) {
  IMG['icon_' + d.id] = b64(path.join(DIR, d.icon.file));
  IMG['scene_' + d.id] = b64(path.join(DIR, `scene_${d.id}.png`));
  d.spin.forEach((f, k) => { IMG[`spin_${d.id}_${k}`] = b64(path.join(DIR, f)); });
}
for (const f of ['fireAxe', 'harpoon', 'battery', 'drill']) IMG['item_' + f] = b64(ART + `items/${f}.png`);
for (const f of ['hero_axe', 'hero_harpoon', 'hero_drill']) IMG[f] = b64(ART + f + '.png');
const GAME = {
  hero_axe: { w: 28, h: 14, grip: [7.5, 7], name: 'топор' },
  hero_harpoon: { w: 26, h: 11, grip: [4.5, 8], name: 'гарпун' },
  hero_drill: { w: 22, h: 13, grip: [7.5, 9], name: 'бур сейчас' },
};
const page = fs.readFileSync(path.join(__dirname, 'page.html'), 'utf8')
  .replace('/*DATA*/', () => `var IMG = ${JSON.stringify(IMG)};\nvar DIRS = ${JSON.stringify(data)};\nvar SCENE = ${JSON.stringify(SIZES)};\nvar GAME = ${JSON.stringify(GAME)};`);
fs.writeFileSync(path.join(DIR, 'concepts.html'), page);
console.log(JSON.stringify(data.map(d => ({ id: d.id, held: d.held })), null, 1), SIZES);

// Writes the weapons: held_<id>.png and icon_<id>.png, weapons.json, sheet.png and melee-weapons.html.
const fs = require('fs'), path = require('path');
const P = require('./png.js'), W = require('./build.js'), POSE = require('./pose.js');
// [outDir] (default ./out, ignored by git): where the candidates, the json and the concepts page go.
const DIR = process.argv[2] || path.join(__dirname, 'out'), ART = path.join(__dirname, '..', '..', '..', 'game', 'art') + '/';
fs.mkdirSync(DIR, { recursive: true });
const TPS = 20;

// ---------------------------------------------------------------- data
const INFO = {
  fireAxe: { name: 'Fire axe', kind: 'Tool · Axe', hold: 90, damage: 2, ticks: 10, arc: 120, effects: [],
    role: 'The all-rounder: two hits a second, the blade biting where it lands.' },
  crowbar: { name: 'Crowbar', kind: 'Tool · Bar', hold: 90, damage: 3, ticks: 16, arc: 110, effects: [],
    role: 'Long and hooked: the furthest reach, a slower swing; the claw pries (later: jammed doors, crates).' },
  sledgehammer: { name: 'Sledgehammer', kind: 'Tool · Hammer', hold: 90, damage: 7, ticks: 28, arc: 140, effects: [],
    role: 'Slow and heavy: a long wind-up, then 7 at once. The most damage a second, if you commit to it.' },
  machete: { name: 'Scrap machete', kind: 'Tool · Blade', hold: 75, damage: 1, ticks: 6, arc: 100, effects: [],
    role: 'Fast and light: a hull-plate offcut ground to an edge. Many quick cuts, the shortest reach.' },
  arcBaton: { name: 'Arc baton', kind: 'Tool · Welder', hold: 90, damage: 1, ticks: 7, arc: 90,
    effects: [{ id: 'fire', name: 'Fire', about: 'sets it burning: +1 damage a second for 3 s' }],
    role: 'A welder\u2019s stinger on a pipe: weak, quick taps that set metal burning. The first carrier of a fire effect.' },
};
const ids = Object.keys(INFO);
const num = n => (Math.round(n * 10) / 10).toFixed(1).replace(/\.0$/, '');

const weapons = ids.map(id => {
  const w = W[id], i = INFO[id];
  return { id, name: i.name, kind: i.kind, role: i.role, held: { file: `held_${id}.png`, w: w.held.w, h: w.held.h },
    icon: { file: `icon_${id}.png`, w: 16, h: 16 }, grip: w.grip, hold: i.hold, edge: w.edge, effects: i.effects,
    damage: i.damage, ticks: i.ticks, arc: i.arc };
});
const axeReach = POSE.reach(weapons[0]);
for (const w of weapons) {
  w.reachPx = Math.round(POSE.reach(w) * 10) / 10;
  w.reach = Math.round(w.reachPx / axeReach * 20) * 100;
  w.seconds = w.ticks / TPS;
  w.dps = num(w.damage / w.seconds);
}

for (const id of ids) { P.write(path.join(DIR, `held_${id}.png`), W[id].held); P.write(path.join(DIR, `icon_${id}.png`), W[id].icon); }

fs.writeFileSync(path.join(DIR, 'weapons.json'), JSON.stringify({
  about: 'Stillwake melee weapons, concept. Held sprites: the handle along +x from the grip, the head at the far end, the working side below (+y).',
  units: {
    grip: 'pixels of the held PNG from its top-left corner, on pixel edges: 7.5 is the middle of pixel 7; the haft\u2019s centre line falls on a whole number',
    hold: 'degrees from the forearm (elbow to fist) to the handle (grip to head); facing +x with y down, the weapon\u2019s screen angle = the forearm\u2019s - hold',
    edge: 'the working part\u2019s points, same pixels as grip; the hit zone is the path they sweep',
    damage: 'hit points a hit takes off a wreck', swingTicks: `ticks between swings, ${TPS} a second`,
    dps: 'damage / (swingTicks / 20), one decimal, half up, as ToolCards.Number',
    reach: 'thousandths of a cell from the shoulder, scaled from reachPx so the fire axe keeps its 2000',
    reachPx: 'farthest an edge point gets from the shoulder in the drawn swing, world pixels (16 a cell)',
    arc: 'degrees back from the line to the pointer, as the Melee table\u2019s Arc',
  },
  fistFromElbow: POSE.RIG.fist,
  weapons: weapons.map(w => ({ id: w.id, name: w.name, kind: w.kind, role: w.role, held: w.held, icon: w.icon,
    grip: w.grip, hold: w.hold, edge: w.edge,
    stats: { damage: w.damage, swingTicks: w.ticks, swingSeconds: w.seconds, dps: +w.dps, reach: w.reach, arc: w.arc, effects: w.effects },
    reachPx: w.reachPx, concept: true })),
}, null, 2));

// ---------------------------------------------------------------- hero parts
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

// ---------------------------------------------------------------- sheet
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
function blit(c, im, ox, oy, s) {
  for (let y = 0; y < im.h * s; y++) for (let x = 0; x < im.w * s; x++) {
    const i = ((y / s | 0) * im.w + (x / s | 0)) * 4;
    if (im.data[i + 3]) blend(c, ox + x, oy + y, [im.data[i], im.data[i + 1], im.data[i + 2]], im.data[i + 3] / 255);
  }
}
// a sprite turned about its joint, sampled nearest, as the game's nearest-filtered sprites look zoomed in
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
function line(c, a, b, rgb, alpha, width = 1) {
  const n = Math.ceil(Math.hypot(b[0] - a[0], b[1] - a[1]) * 2) + 1;
  for (let i = 0; i <= n; i++) {
    const x = a[0] + (b[0] - a[0]) * i / n, y = a[1] + (b[1] - a[1]) * i / n;
    for (let dy = 0; dy < width; dy++) for (let dx = 0; dx < width; dx++) blend(c, x + dx - width / 2, y + dy - width / 2, rgb, alpha);
  }
}
function disc(c, x, y, r, rgb, a = 1) {
  for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++) if (dx * dx + dy * dy <= r * r) blend(c, x + dx, y + dy, rgb, a);
}
const GRIP = [0x6f, 0xd3, 0xff], EDGE = [0xff, 0x7a, 0x2e], RING = [0x14, 0x17, 0x1e];
// one frame for every pose of every weapon: the hero and each weapon's corners, plus a margin
const FRAME = (() => {
  let x0 = -12, y0 = -44, x1 = 10, y1 = 2;
  for (const w of weapons) for (const p of Object.values(POSE.POSES)) {
    const sc = POSE.scene(w, p), d = sc.list.find(d => d.part === 'weapon'), r = d.rot * Math.PI / 180;
    for (const [x, y] of [[0, 0], [w.held.w, 0], [0, w.held.h], [w.held.w, w.held.h]]) {
      const dx = x - w.grip[0], dy = y - w.grip[1];
      const X = d.at[0] + dx * Math.cos(r) - dy * Math.sin(r), Y = d.at[1] + dx * Math.sin(r) + dy * Math.cos(r);
      x0 = Math.min(x0, X); x1 = Math.max(x1, X); y0 = Math.min(y0, Y); y1 = Math.max(y1, Y);
    }
  }
  for (const w of weapons) for (const path of POSE.sweep(w, 60)) for (const [X, Y] of path) { x0 = Math.min(x0, X); x1 = Math.max(x1, X); y0 = Math.min(y0, Y); y1 = Math.max(y1, Y); }
  x0 = Math.floor(x0 - 3); y0 = Math.floor(y0 - 3); x1 = Math.ceil(x1 + 3); y1 = 4;
  return { x0, y0, w: x1 - x0, h: y1 - y0 };
})();
console.log('frame', FRAME);

function drawPose(c, w, poseName, ox, oy, s) {
  const pose = POSE.POSES[poseName], sc = POSE.scene(w, pose), heldIm = W[w.id].held;
  const X = ox - FRAME.x0 * s, Y = oy - FRAME.y0 * s;
  // the floor line, the swept band, then the figure
  line(c, [ox, Y], [ox + FRAME.w * s, Y], [0x25, 0x30, 0x42], 1, 2);
  const paths = POSE.sweep(w, 80);
  for (const path of paths) for (let i = 1; i < path.length; i++)
    line(c, [X + path[i - 1][0] * s, Y + path[i - 1][1] * s], [X + path[i][0] * s, Y + path[i][1] * s], EDGE, 0.16, 2);
  for (const d of sc.list) {
    const im = d.part === 'weapon' ? heldIm : PARTS[d.part + (d.far ? 'Far' : '')];
    drawPart(c, im, d.joint || POSE.JOINT[d.part], d.at, d.rot, X, Y, s);
  }
  disc(c, X + sc.grip[0] * s, Y + sc.grip[1] * s, 4, RING); disc(c, X + sc.grip[0] * s, Y + sc.grip[1] * s, 2, GRIP);
  for (const e of sc.edges) { disc(c, X + e[0] * s, Y + e[1] * s, 4, RING); disc(c, X + e[0] * s, Y + e[1] * s, 2, EDGE); }
}

{
  const HS = 6, PS = 4, gap = 16, poseW = FRAME.w * PS, poseH = FRAME.h * PS;
  const heldW = Math.max(...ids.map(id => W[id].held.w)) * HS;
  const rowH = Math.max(poseH, 16 * HS + 30 + Math.max(...ids.map(id => W[id].held.h)) * HS);
  const width = gap + heldW + gap + 3 * (poseW + gap / 2) + gap / 2, height = gap + ids.length * (rowH + gap);
  const c = canvas(width, height, [0x0b, 0x0e, 0x14]);
  weapons.forEach((w, n) => {
    const oy = gap + n * (rowH + gap), held = W[w.id].held;
    for (let y = oy - 6; y < oy + rowH + 6; y++) for (let x = 6; x < width - 6; x++) blend(c, x, y, [0x11, 0x18, 0x23], 1);
    blit(c, held, gap, oy + 8, HS);
    disc(c, gap + w.grip[0] * HS, oy + 8 + w.grip[1] * HS, 4, RING); disc(c, gap + w.grip[0] * HS, oy + 8 + w.grip[1] * HS, 2, GRIP);
    for (const e of w.edge) { disc(c, gap + e[0] * HS, oy + 8 + e[1] * HS, 4, RING); disc(c, gap + e[0] * HS, oy + 8 + e[1] * HS, 2, EDGE); }
    const iy = oy + 8 + held.h * HS + 22;
    for (let y = iy - 4; y < iy + 16 * HS + 4; y++) for (let x = gap - 4; x < gap + 16 * HS + 4; x++) blend(c, x, y, [0x1e, 0x29, 0x3b], 1);
    blit(c, W[w.id].icon, gap, iy, HS);
    for (let y = iy + 16 * HS - 36; y < iy + 16 * HS; y++) for (let x = gap + 16 * HS + 16; x < gap + 16 * HS + 52; x++) blend(c, x, y, [0x1e, 0x29, 0x3b], 1);
    blit(c, W[w.id].icon, gap + 16 * HS + 18, iy + 16 * HS - 34, 2);
    ['rest', 'raised', 'strike'].forEach((p, k) => {
      const px = gap + heldW + gap + k * (poseW + gap / 2);
      for (let y = oy; y < oy + poseH; y++) for (let x = px; x < px + poseW; x++) blend(c, x, y, [0x07, 0x0a, 0x10], 1);
      drawPose(c, w, p, px, oy, PS);
    });
  });
  P.write(path.join(DIR, 'sheet.png'), c);
}

// ---------------------------------------------------------------- page
const uri = im => 'data:image/png;base64,' + P.encode(im.w, im.h, im.data).toString('base64');
const IMAGES = {};
for (const [k, im] of Object.entries(PARTS)) IMAGES[k] = uri(im);
for (const id of ids) { IMAGES['held_' + id] = uri(W[id].held); IMAGES['icon_' + id] = uri(W[id].icon); }
const page = fs.readFileSync(path.join(__dirname, 'page.html'), 'utf8')
  .replace('/*POSE*/', () => fs.readFileSync(path.join(__dirname, 'pose.js'), 'utf8'))
  .replace('/*DATA*/', () => `var IMAGES = ${JSON.stringify(IMAGES)};\nvar WEAPONS = ${JSON.stringify(weapons)};\nvar FRAME = ${JSON.stringify(FRAME)};`);
fs.writeFileSync(path.join(DIR, 'melee-weapons.html'), page);
console.log(weapons.map(w => `${w.id}: grip ${w.grip} hold ${w.hold} edges ${JSON.stringify(w.edge)} dps ${w.dps} reachPx ${w.reachPx} reach ${w.reach}`).join('\n'));

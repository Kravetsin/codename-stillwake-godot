'use strict';
// The page's scenes, drawn in the browser with compose.js from the pictures build.js inlines (PICTURES, PARTS, MAKE).
const A = PARTS, C = MAKE(PARTS);
const IMG = {};
const get = n => IMG[n];
const $ = id => document.getElementById(id);
const el = (tag, cls, text) => { const e = document.createElement(tag); if (cls) e.className = cls; if (text != null) e.textContent = text; return e; };
const hex = n => '#' + n.toString(16).padStart(6, '0');

function decode(src) {
  return new Promise((ok, fail) => {
    const im = new Image();
    im.onload = () => {
      const c = document.createElement('canvas');
      c.width = im.width; c.height = im.height;
      const g = c.getContext('2d');
      g.drawImage(im, 0, 0);
      ok({ w: im.width, h: im.height, data: g.getImageData(0, 0, im.width, im.height).data });
    };
    im.onerror = fail;
    im.src = src;
  });
}

function canvasOf(img, k) {
  const c = document.createElement('canvas');
  c.width = img.w; c.height = img.h;
  c.getContext('2d').putImageData(new ImageData(new Uint8ClampedArray(img.data), img.w, img.h), 0, 0);
  c.style.width = img.w * k + 'px';
  c.style.height = 'auto';
  if (k > 1) c.className = 'big';
  return c;
}

/** The picture enlarged k times, and under it at 1:1, as the game draws it. */
function pair(img, k, caption) {
  const box = el('div', 'pair');
  box.append(canvasOf(img, k));
  const one = el('div', 'one');
  one.append(canvasOf(img, 1), el('span', null, '1:1' + (caption ? ' · ' + caption : '')));
  box.append(one);
  return box;
}

function crop(img, x0, y0, w, h) {
  const o = C.blank(w, h);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const s = ((y + y0) * img.w + x + x0) * 4;
    if (x + x0 >= 0 && y + y0 >= 0 && x + x0 < img.w && y + y0 < img.h) o.data.set(img.data.subarray(s, s + 4), (y * w + x) * 4);
  }
  return o;
}

// ---- light: the deck lit, or dark with each helmet's lamp
const WARM = [1, 0.9, 0.62], AMBIENT = [0.15, 0.16, 0.21];
function lampAt(x, y, l) {
  const dx = x - l[0], dy = y - l[1], dist = Math.hypot(dx, dy);
  const spill = 0.6 * Math.exp(-dist / 3);
  let beam = 0;
  if (dx > 0) {
    const ang = Math.atan2(Math.abs(dy), dx);
    const cone = ang < 0.35 ? 1 : ang < 0.75 ? (0.75 - ang) / 0.4 : 0;
    beam = cone * 0.9 * Math.max(0, 1 - dist / 70);
  }
  return spill + beam;
}
function darkLight(lamps) {
  return (x, y, op) => {
    if (op && op.face && op.lampLit) return [1, 0.92, 0.76];
    let s = 0;
    for (const l of lamps) s += lampAt(x, y, l);
    return AMBIENT.map((a, k) => a + WARM[k] * s);
  };
}

/** The deck behind the heroes: the ship's back wall and its floor, the floor's top at floorY. */
function deck(w, h, floorY, light) {
  const img = C.blank(w, h, 0x000000);
  const back = get('back_scrap'), floor = get('floor');
  const wall = light || (() => [0.55, 0.55, 0.6]);
  for (let y = 0; y < floorY; y += back.h) for (let x = 0; x < w; x += back.w) C.over(img, back, x, y, wall);
  for (let x = 0; x < w; x += floor.w) C.over(img, floor, x, floorY, light);
  return img;
}

/** Heroes side by side, their feet on the floor: looks, each { ...look, bubble }, lit or in the dark. */
function lineup(looks, { dark = false, cw = 26, h = 54 } = {}) {
  const w = cw * looks.length, floorY = h - 5;
  const feet = looks.map((_, i) => [Math.round(i * cw + cw / 2), floorY]);
  // A bubble's lamp sits on its glass, ahead of the brow.
  const lampOn = l => (l.bubble ? [8, -11] : C.LAMP_ON);
  const lamps = dark ? looks.map((l, i) => (l.suited || l.bubble ? [feet[i][0] + lampOn(l)[0], floorY - C.Neck + lampOn(l)[1]] : null)).filter(Boolean) : [];
  const light = dark ? darkLight(lamps) : null;
  const img = deck(w, h, floorY, light);
  looks.forEach((look, i) => {
    const ops = C.figure(look.bubble ? { ...look, suited: true } : look, get);
    ops.forEach(op => (op.lampLit = look.suited || look.bubble));
    if (look.bubble) ops.push({ img: bubbleGlass(), at: [0, -C.Neck], turn: 0, joint: [6.5 + 4, 12 + 4] });
    C.draw(img, ops, feet[i], light);
  });
  if (dark) for (const l of lamps) bulb(img, l);
  return img;
}

/** The helmet lamp's bulb, drawn unlit (Figure.MakeBulb), centred on the lamp. */
function bulb(img, l) {
  const x0 = Math.round(l[0] - 1.5), y0 = Math.round(l[1] - 1);
  const px = [[255, 230, 158], [255, 230, 158], [255, 230, 158], [140, 122, 102], [255, 230, 158], [140, 122, 102]];
  px.forEach((c, i) => { const x = x0 + (i % 3), y = y0 + ((i / 3) | 0), d = (y * img.w + x) * 4; img.data.set([...c, 255], d); });
}

// The aquarium: a glass bubble round the head, its pixel (4, 4) the helmet's (0, 0), resting on a collar.
let glass;
function bubbleGlass() {
  if (glass) return glass;
  const g = C.blank(21, 21), cx = 10.5, cy = 9.6, r = 9.4;
  const set = (x, y, c, a) => g.data.set([c >> 16, (c >> 8) & 255, c & 255, a], (y * g.w + x) * 4);
  for (let y = 0; y < g.h; y++) for (let x = 0; x < g.w; x++) {
    const d = Math.hypot(x + 0.5 - cx, y + 0.5 - cy), ang = Math.atan2(y + 0.5 - cy, x + 0.5 - cx);
    if (d < r - 1) set(x, y, 0x5c8fb4, 30);
    else if (d < r) set(x, y, 0xa8d4ee, 120);
    if (d >= r - 2.6 && d < r - 1.4 && ang > -2.6 && ang < -1.75) set(x, y, 0xeaf6ff, 200);
    if (d >= r - 2.4 && d < r - 1.4 && ang > 0.35 && ang < 0.6) set(x, y, 0xcfe6f5, 120);
  }
  const collar = ['..KgGHHHHGgK..', 'KghHHHHHHHHhgK'];
  const cols = { K: 0x14171d, g: 0x262b35, G: 0x3d4452, h: 0x6a7385, H: 0x9aa3ad };
  collar.forEach((row, j) => [...row].forEach((k, i) => k !== '.' && set(i + 3, 16 + j, cols[k], 255)));
  return (glass = g);
}

// ---- looks used across the page
const CREW = [
  { suit: 0, skin: 1, hair: 1, hairColour: 1, face: 0 },
  { suit: 1, skin: 4, hair: 2, hairColour: 0, face: 2 },
  { suit: 2, skin: 0, hair: 3, hairColour: 2, face: 1 },
  { suit: 3, skin: 6, hair: 4, hairColour: 4, face: 2 },
  { suit: 0, skin: 3, hair: 4, hairColour: 5, face: 1 },
  { suit: 1, skin: 2, hair: 0, hairColour: 3, face: 0 },
  { suit: 2, skin: 5, hair: 1, hairColour: 1, face: 2 },
  { suit: 3, skin: 1, hair: 2, hairColour: 2, face: 0 },
  { suit: 0, skin: 6, hair: 3, hairColour: 0, face: 2 },
  { suit: 1, skin: 0, hair: 1, hairColour: 4, face: 1 },
  { suit: 2, skin: 3, hair: 0, hairColour: 0, face: 0 },
  { suit: 3, skin: 4, hair: 3, hairColour: 3, face: 1 },
];

/** The trial head, its hair and face, in a cell 40×42 with the neck at (20, 34). */
function fineHead(look) {
  const img = C.blank(40, 42);
  for (const l of C.headLayers({ ...look, fine: true }, get, false)) C.over(img, l.img, 20 - l.joint[0], 34 - l.joint[1]);
  return img;
}

function headOf(look, { glass = true, cw = 15, ch = 14 } = {}) {
  const img = C.blank(cw, ch);
  for (const l of C.headLayers(look, get, glass)) C.over(img, l.img, 1, 1);
  return img;
}

// ---- sections
function hero() {
  const box = $('hero');
  box.append(pair(lineup(TRIAL.map(l => ({ ...l, fine: true })), { cw: 26 }), 5, 'палуба при свете, проба: голова с пикселем вдвое мельче'));
}

const state = { suit: 0, skin: 2, hair: 1, hairColour: 1, face: 0 };
const OPTIONS = { skin: 7, hair: 5, hairColour: 6, face: 3, suit: 4 };
function editor() {
  const chips = {};
  for (const [key, n] of Object.entries(OPTIONS)) {
    const row = $('c-' + key);
    chips[key] = [];
    for (let i = 0; i < n; i++) {
      const b = el('button', 'chip');
      b.type = 'button';
      b.id = `chip-${key}-${i}`;
      b.setAttribute('aria-label', `${key} ${i + 1}`);
      b.addEventListener('click', () => { state[key] = i; render(); });
      row.append(b);
      chips[key].push(b);
    }
  }
  $('shuffle').addEventListener('click', () => {
    for (const [key, n] of Object.entries(OPTIONS)) state[key] = Math.floor(Math.random() * n);
    render();
  });
  function render() {
    for (const [key, list] of Object.entries(chips)) list.forEach((b, i) => {
      b.setAttribute('aria-pressed', String(state[key] === i));
      b.replaceChildren();
      if (key === 'skin' || key === 'hairColour' || key === 'suit') {
        const s = el('span', 'swatch');
        s.style.background = key === 'skin' ? A.SKINS[i][1] : key === 'hairColour' ? A.HAIRS[i][1] : hex(A.SUITS[i]);
        b.append(s);
      } else {
        const c = canvasOf(fineHead({ ...state, [key]: i }), 1);
        c.className = '';
        b.append(c);
      }
    });
    const stages = $('edit-stages');
    stages.replaceChildren(
      pair(lineup([{ ...state, fine: true }], { cw: 30 }), 7, 'без скафандра'),
      pair(lineup([{ ...state, suited: true, fine: true }], { cw: 36, dark: true }), 7, 'в шлеме, темно'),
    );
  }
  render();
}

function grid(g, k, w, h) {
  g.strokeStyle = 'rgba(255,255,255,0.06)';
  g.lineWidth = 1;
  for (let x = 0; x <= w; x++) { g.beginPath(); g.moveTo(x * k + 0.5, 0); g.lineTo(x * k + 0.5, h * k); g.stroke(); }
  for (let y = 0; y <= h; y++) { g.beginPath(); g.moveTo(0, y * k + 0.5); g.lineTo(w * k, y * k + 0.5); g.stroke(); }
}
function edges(g, mask, k, colour, dash) {
  const on = (x, y) => x >= 0 && y >= 0 && x < mask.w && y < mask.h && mask.data[(y * mask.w + x) * 4 + 3] > 0;
  g.strokeStyle = colour; g.lineWidth = 2; g.setLineDash(dash || []);
  g.beginPath();
  for (let y = 0; y < mask.h; y++) for (let x = 0; x < mask.w; x++) {
    if (!on(x, y)) continue;
    if (!on(x, y - 1)) { g.moveTo(x * k, y * k); g.lineTo((x + 1) * k, y * k); }
    if (!on(x, y + 1)) { g.moveTo(x * k, (y + 1) * k); g.lineTo((x + 1) * k, (y + 1) * k); }
    if (!on(x - 1, y)) { g.moveTo(x * k, y * k); g.lineTo(x * k, (y + 1) * k); }
    if (!on(x + 1, y)) { g.moveTo((x + 1) * k, y * k); g.lineTo((x + 1) * k, (y + 1) * k); }
  }
  g.stroke(); g.setLineDash([]);
}
function pivot(g, x, y) {
  g.strokeStyle = '#ffe2a0'; g.lineWidth = 2;
  g.beginPath(); g.moveTo(x - 9, y); g.lineTo(x + 9, y); g.moveTo(x, y - 9); g.lineTo(x, y + 9); g.stroke();
  g.beginPath(); g.arc(x, y, 5, 0, Math.PI * 2); g.stroke();
}
/** A picture drawn k times larger on a grid with its outline marks and pivot, with room round it for the marks. */
function diagram(img, k, joint, marks) {
  const pad = 12, c = document.createElement('canvas');
  c.width = img.w * k + pad * 2; c.height = img.h * k + pad * 2;
  c.style.width = c.width + 'px';
  c.className = 'big';
  c.style.imageRendering = 'auto';
  const g = c.getContext('2d');
  g.translate(pad, pad);
  const src = document.createElement('canvas');
  src.width = img.w; src.height = img.h;
  src.getContext('2d').putImageData(new ImageData(new Uint8ClampedArray(img.data), img.w, img.h), 0, 0);
  g.imageSmoothingEnabled = false;
  g.drawImage(src, 0, 0, img.w * k, img.h * k);
  grid(g, k, img.w, img.h);
  for (const m of marks || []) edges(g, m.mask, k, m.colour, m.dash);
  pivot(g, joint[0] * k, joint[1] * k);
  return c;
}

function templates() {
  const k = 18;
  const head = C.blank(13, 12);
  for (const l of C.headLayers({ hair: 0, face: 0 }, get, false)) C.over(head, l.img, 0, 0);
  const suited = C.blank(13, 12);
  for (const l of C.headLayers({ hair: 0, face: 0, suited: true }, get)) C.over(suited, l.img, 0, 0);
  const marks = [{ mask: get('hero_helmet'), colour: 'rgba(221,227,236,0.75)', dash: [6, 5] }, { mask: get('hero_visor'), colour: '#8fb6d6' }];
  const a = el('div', 'cell'), b = el('div', 'cell');
  a.append(diagram(head, k, A.HEAD_JOINT, marks), el('div', 'label', 'голова + лицо + волосы'));
  b.append(diagram(suited, k, A.HEAD_JOINT, marks.slice(1)), el('div', 'label', '+ корпус шлема + стекло'));
  $('tpl-head').append(a, b);

  const J = C.JOINTS;
  for (const part of ['torso', 'upperArm', 'forearm', 'thigh', 'shin']) {
    const cell = el('div', 'cell');
    const suit = get('hero_' + part), jump = get('hero_jumpsuit_' + part);
    cell.append(diagram(suit, 8, J[part]), diagram(jump, 8, J[part]), el('div', 'label', `${part}\n${suit.w}×${suit.h}`));
    $('tpl-suit').append(cell);
  }

  const ramps = $('ramps');
  const names = ['светлый', 'свой', 'тень', 'контур'];
  const row = (label, ramp, drawn) => {
    const r = el('div', 'ramp');
    r.append(el('div', 'name', label + (drawn ? ' · опорная' : '')));
    ramp.forEach((c, i) => {
      const s = el('div', 'sw', names[i]);
      const col = typeof c === 'string' ? c : hex(c);
      s.style.background = col;
      const n = parseInt(col.slice(1), 16), lum = 0.3 * (n >> 16) + 0.59 * ((n >> 8) & 255) + 0.11 * (n & 255);
      s.style.color = lum > 128 ? '#14171d' : '#dde3ec';
      s.title = col;
      r.append(s);
    });
    ramps.append(r);
  };
  row('скафандр', A.SUIT_DRAWN, true);
  A.SKINS.forEach((r, i) => row(`кожа ${i + 1}`, r, i === A.SKIN_DRAWN));
  A.HAIRS.forEach((r, i) => row(`волосы ${i + 1}`, r, i === A.HAIR_DRAWN));
}

function variants() {
  const heads = (looks, k, labels) => {
    const strip = C.blank(15 * looks.length, 14, 0x1b212b);
    looks.forEach((l, i) => C.over(strip, headOf(l, { glass: false }), i * 15, 0));
    return pair(strip, k, labels);
  };
  $('v-skin').append(heads(A.SKINS.map((_, i) => ({ skin: i, hair: 0, hairColour: 1, face: 0 })), 8, 'кожа 1–7'));
  $('v-hair').append(heads([0, 1, 2, 3, 4].map(i => ({ hair: i, face: 0 })), 8, 'короткая, чуб, хвост, длинные, кудри'));
  $('v-hairColour').append(heads(A.HAIRS.map((_, i) => ({ hair: 3, hairColour: i, skin: 1, face: 0 })), 8, 'чёрные, каштановые, медные, светлые, седые, розовые'));
  $('v-face').append(heads([0, 1, 2].map(i => ({ face: i, hair: 0, skin: [1, 3, 5][i], hairColour: [3, 1, 0][i] })), 12, 'лица 1–3'));
}

function crew() {
  $('crew').append(pair(lineup(CREW.slice(0, 6), { cw: 24 }), 5), pair(lineup(CREW.slice(6), { cw: 24 }), 5));
}

function dark() {
  const looks = [];
  for (let i = 0; i < 4; i++) looks.push({ ...CREW[i], suited: true, closed: true }, { ...CREW[i], suited: true });
  $('dark').append(pair(lineup(looks, { dark: true, cw: 36 }), 3, 'шлем сегодня и со стеклом, по парам'));
  // Close up: the helmet and the bulb, cropped from a dark scene.
  const close = [];
  for (const i of [0, 3, 6]) close.push({ ...CREW[i], suited: true, closed: true }, { ...CREW[i], suited: true });
  const scene = lineup(close, { dark: true, cw: 36, h: 54 });
  const strip = C.blank(close.length * 19, 16);
  close.forEach((_, i) => C.over(strip, crop(scene, i * 36 + 18 - 8, 54 - 5 - Math.ceil(C.Neck) - 13, 19, 16), i * 19, 0));
  $('visor-close').append(pair(strip, 12));
}

function bubble() {
  const looks = [{ ...CREW[1], bubble: true }, { ...CREW[2], bubble: true }, { ...CREW[3], bubble: true }];
  $('bubble').append(pair(lineup(looks, { cw: 30, h: 58 }), 6, 'при свете'), pair(lineup(looks, { cw: 36, h: 58, dark: true }), 6, 'в темноте'));
}

function files() {
  const rows = [
    ['hero_head', '13×12 · (6.5, 12)', 'кожа'],
    ['hero_face_0…2', '13×12 · (6.5, 12)', 'глаза свои; брови: волосы; рот, веснушки: кожа'],
    ['hero_hair_0…4', '13×12 · (6.5, 12)', 'волосы'],
    ['hero_helmet_shell', '13×12 · (6.5, 12)', 'скафандр; hero_helmet без визора'],
    ['hero_visor', '13×12 · (6.5, 12)', 'своя, полупрозрачная'],
    ['hero_jumpsuit_torso', '12×15 · (6, 0)', 'скафандр; вырез: кожа'],
    ['hero_jumpsuit_upperArm', '5×9 · (2.5, 2.5)', 'скафандр'],
    ['hero_jumpsuit_forearm', '5×10 · (2.5, 2.9)', 'скафандр; кисть: кожа'],
    ['hero_jumpsuit_thigh', '6×12 · (3, 2.5)', 'скафандр'],
    ['hero_jumpsuit_shin', '9×11 · (3, 2.18)', 'скафандр; ботинок свой'],
  ];
  for (const [f, s, r] of rows) {
    const tr = el('tr');
    const td = el('td'); td.append(el('code', null, f + '.png'));
    tr.append(td, el('td', 'mono', s), el('td', null, r));
    $('files').append(tr);
  }
}

// ---- the trial: the head twice as fine, on the screen's own pixels
const TRIAL = [
  { suit: 0, skin: 1, hair: 0, hairColour: 1, face: 0 },
  { suit: 1, skin: 5, hair: 2, hairColour: 0, face: 1 },
  { suit: 2, skin: 0, hair: 3, hairColour: 3, face: 2 },
  { suit: 3, skin: 6, hair: 4, hairColour: 4, face: 0 },
  { suit: 0, skin: 3, hair: 1, hairColour: 2, face: 2 },
];
// The camera's zooms (ShipCamera): the farthest shows 192×108 cells, the nearest twice as close.
const ZOOMS = [
  { z: 1.25, label: '1080p · дальний' },
  { z: 2.5, label: '1080p · ближний' },
  { z: 5 / 3, label: '1440p · дальний' },
  { z: 10 / 3, label: '1440p · ближний' },
];

/** A canvas showing img on the screen's own pixels, one image pixel a device pixel. */
function devCanvas(img) {
  const c = canvasOf(img, 1);
  c.style.width = img.w / (window.devicePixelRatio || 1) + 'px';
  return c;
}

/** Each look twice, with today's head and with the fine one, drawn by the camera at zoom z; t animates the head and a slow walk. */
function trialScene(looks, z, smooth, t) {
  const slot = 28, h = Math.round(50 * z), w = Math.round(looks.length * 2 * slot * z);
  const img = C.blank(w, h, 0x1b212b);
  const floor = h - Math.round(4 * z);
  for (let y = floor; y < h; y++) for (let x = 0; x < w; x++) img.data.set([0x3a, 0x40, 0x4c, 255], (y * w + x) * 4);
  const turn = t == null ? 0 : 0.32 * Math.sin(t * 1.3), drift = t == null ? 0 : (t * 2.2) % 6;
  looks.forEach((look, i) => [false, true].forEach((fine, j) => {
    const ops = C.figure({ ...look, fine, turn }, get);
    C.draw(img, ops, [((2 * i + j) * slot + slot / 2 + drift - 3) * z, floor], null, { zoom: z, smooth });
  }));
  return img;
}

function trial() {
  // The heads side by side at one size: today's 13×12, the trial's 26×24.
  const now = C.blank(15 * TRIAL.length, 14, 0x1b212b), fine = C.blank(40 * TRIAL.length, 42, 0x1b212b);
  TRIAL.forEach((l, i) => {
    for (const x of C.headLayers(l, get, false)) C.over(now, x.img, i * 15 + 1, 1);
    for (const x of C.headLayers({ ...l, fine: true }, get, false)) C.over(fine, x.img, i * 40 + 20 - x.joint[0], 34 - x.joint[1]);
  });
  $('trial-heads').append(
    pair(now, 10, 'сейчас: 13×12'),
    pair(fine, 5, 'проба: голова 26×24, волосы 38×40, пиксель вдвое мельче'),
  );
  for (const { z, label } of ZOOMS) {
    const cell = el('div', 'pair');
    cell.append(el('div', 'one', label + ` · ×${z.toFixed(2).replace(/\.?0+$/, '')}`), devCanvas(trialScene(TRIAL, z, false)));
    $('trial-screen').append(cell);
  }

  // Live: the head turning and the body walking slowly, as in play.
  const live = { z: ZOOMS[0].z, smooth: false };
  const zoomRow = $('trial-zoom'), filterRow = $('trial-filter');
  const chip = (row, text, on, pick) => {
    const b = el('button', 'chip', text);
    b.type = 'button';
    b.style.padding = '6px 10px';
    b.addEventListener('click', () => { pick(); sync(); });
    row.append(b);
    return [b, on];
  };
  const chips = [
    ...ZOOMS.map(({ z, label }) => chip(zoomRow, label, () => live.z === z, () => (live.z = z))),
    chip(filterRow, 'резко (как сейчас)', () => !live.smooth, () => (live.smooth = false)),
    chip(filterRow, 'сглажено (mipmaps)', () => live.smooth, () => (live.smooth = true)),
  ];
  chips.forEach(([b], i) => (b.id = 'trial-chip-' + i));
  function sync() { for (const [b, on] of chips) b.setAttribute('aria-pressed', String(on())); }
  sync();
  const stage = $('trial-live'), looks = [TRIAL[1], TRIAL[3]];
  const still = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  let last = -1;
  function frame(ms) {
    if (ms - last > 45) {
      last = ms;
      const img = trialScene(looks, live.z, live.smooth, still ? 1.2 : ms / 1000), big = canvasOf(img, 3 / (window.devicePixelRatio || 1));
      big.className = '';
      stage.replaceChildren(devCanvas(img), el('div', 'one', 'то же втрое крупнее'), big);
    }
    if (!still) requestAnimationFrame(frame);
  }
  requestAnimationFrame(frame);
}

// In Node (check.js) the scenes are drawn without the page.
if (typeof document !== 'undefined')
  Promise.all(Object.entries(PICTURES).map(([n, src]) => decode(src).then(img => (IMG[n] = img)))).then(() => {
    Object.assign(IMG, HD.pictures(A));
    for (const f of [hero, live, trial, editor, templates, variants, crew, dark, bubble, files]) f();
  });
else module.exports = { IMG, C, CREW, lineup, crop, headOf };
